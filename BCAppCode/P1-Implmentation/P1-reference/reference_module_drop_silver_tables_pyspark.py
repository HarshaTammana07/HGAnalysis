# P1 Reference — drop all 9 Silver tables for PascalCase reload.
# Run in a Fabric PySpark notebook attached to the Silver lakehouse (bhg_silver).
#
# Why DROP (not DELETE):
#   Existing silver tables may still have legacy camelCase column names.
#   DROP removes table + schema so the silver notebooks recreate Delta tables
#   with the new PascalCase TARGET_COLUMNS on the next Bronze → Silver run.
#
# Bronze tables are NOT touched.

from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 89

# Fallback if taskconfig is unavailable — matches taskcomfigrefrencecurrentrows.txt Silver rows.
REFERENCE_SILVER_TABLES = [
    "bhg_silver.ctrl.tbl_Clinic",
    "bhg_silver.ctrl.tbl_3PSETUP",
    "bhg_silver.pats.tbl_Codes",
    "bhg_silver.pats.tbl_SERVICES",
    "bhg_silver.ctrl.tbl_DroDownListItems",
    "bhg_silver.pats.tbl_CustomAnswers",
    "bhg_silver.pats.tbl_CustomQuestions",
    "bhg_silver.ayx.tbl_PreAdmission_V6",
    "bhg_silver.pats.tbl_PreadmissionReferralSource",
]


def table_exists(full_name):
    try:
        spark.table(full_name)
        return True
    except Exception:
        return False


config_rows = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == silver_config_id)
    .select("TaskConfigId", "Method", "TargetPath", "TargetTable")
    .orderBy("ExecutionOrder", "TaskConfigId")
    .collect()
)

if len(config_rows) == 9:
    tables_to_drop = [row["TargetPath"] for row in config_rows]
    print("Using 9 Silver TargetPath values from TaskConfig (ConfigId=89).")
else:
    tables_to_drop = REFERENCE_SILVER_TABLES
    print(
        f"TaskConfig returned {len(config_rows)} Silver rows; "
        f"using built-in list of {len(tables_to_drop)} tables."
    )

print("\n--- BEFORE: table existence ---")
before_status = []
for full_name in tables_to_drop:
    exists = table_exists(full_name)
    row_count = None
    if exists:
        row_count = spark.table(full_name).count()
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
after_status = []
for full_name in tables_to_drop:
    exists = table_exists(full_name)
    after_status.append((full_name, exists))
    print(f"{full_name}: exists={exists}")

still_present = [name for name, exists in after_status if exists]
if still_present:
    raise Exception(f"These tables still exist after DROP: {still_present}")

display(
    spark.createDataFrame(
        [
            (full_name, exists, rows)
            for (full_name, exists, rows) in before_status
        ],
        ["full_table", "existed_before", "row_count_before"],
    )
)

print(
    f"\nDone. Dropped {len(dropped)} reference silver table(s); "
    f"{len(missing)} were already absent."
)
print("Next: run pl_p1_reference Silver child pipeline to reload from Bronze.")
