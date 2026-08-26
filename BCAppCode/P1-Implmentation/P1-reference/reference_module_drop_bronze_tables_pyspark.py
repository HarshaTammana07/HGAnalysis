# P1 Reference — drop all 9 Bronze tables for PascalCase reload.
# Run in a Fabric PySpark notebook attached to the Bronze lakehouse (bhg_bronze).
#
# Why DROP (not DELETE):
#   Existing bronze tables may still have legacy camelCase column names.
#   DROP removes table + schema so the bronze Copy pipeline recreates Delta tables
#   with PascalCase sink mappings on the next run.
#
# Silver/Gold tables are NOT touched.

from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
bronze_config_id = 88

REFERENCE_BRONZE_TABLES = [
    "bhg_bronze.P1Reference.br_samms_clinic",
    "bhg_bronze.P1Reference.br_samms_3p_setup",
    "bhg_bronze.P1Reference.br_samms_codes",
    "bhg_bronze.P1Reference.br_samms_services",
    "bhg_bronze.P1Reference.br_samms_dropdown_list_items",
    "bhg_bronze.P1Reference.br_samms_custom_answers",
    "bhg_bronze.P1Reference.br_samms_custom_questions",
    "bhg_bronze.P1Reference.br_samms_pre_admission_v6",
    "bhg_bronze.P1Reference.br_samms_preadmission_referral_source",
]


def table_exists(full_name):
    try:
        spark.table(full_name)
        return True
    except Exception:
        return False


config_rows = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == bronze_config_id)
    .where(F.col("SiteCode").isNull())
    .select("TaskConfigId", "Method", "TargetPath", "TargetTable")
    .orderBy("ExecutionOrder", "TaskConfigId")
    .collect()
)

if len(config_rows) == 9:
    tables_to_drop = [row["TargetPath"] for row in config_rows]
    print("Using 9 Bronze TargetPath values from TaskConfig template rows (ConfigId=88).")
else:
    tables_to_drop = REFERENCE_BRONZE_TABLES
    print(
        f"TaskConfig returned {len(config_rows)} Bronze template rows; "
        f"using built-in list of {len(tables_to_drop)} tables."
    )

print("\n--- BEFORE: table existence ---")
before_status = []
for full_name in tables_to_drop:
    exists = table_exists(full_name)
    row_count = spark.table(full_name).count() if exists else None
    before_status.append((full_name, exists, row_count))
    print(f"{full_name}: exists={exists}, rows={row_count}")

display(
    spark.createDataFrame(before_status, ["full_table", "exists_before", "row_count_before"])
)

print("\n--- Dropping tables ---")
dropped = []
missing = []
errors = []

for full_name in tables_to_drop:
    if not table_exists(full_name):
        missing.append(full_name)
        print(f"SKIP (not found): {full_name}")
        continue
    try:
        spark.sql(f"DROP TABLE IF EXISTS {full_name}")
        dropped.append(full_name)
        print(f"DROPPED: {full_name}")
    except Exception as exc:
        errors.append((full_name, str(exc)))
        print(f"ERROR: {full_name} -> {exc}")

if errors:
    raise Exception(f"Failed to drop {len(errors)} table(s): {errors}")

print("\n--- AFTER: confirm all gone ---")
still_present = []
for full_name in tables_to_drop:
    exists = table_exists(full_name)
    print(f"{full_name}: exists={exists}")
    if exists:
        still_present.append(full_name)

if still_present:
    raise Exception(f"These tables still exist after DROP: {still_present}")

print(
    f"\nDone. Dropped {len(dropped)} reference bronze table(s); "
    f"{len(missing)} were already absent."
)
print("Next: deploy updated pl_p1_reference bronze child JSON, run bronze TaskConfig script, reload Bronze.")
