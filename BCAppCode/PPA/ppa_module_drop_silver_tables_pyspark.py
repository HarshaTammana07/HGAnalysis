# PPA — drop Silver table for explicit TARGET_SCHEMA reload.
# Run in a Fabric PySpark notebook attached to the Silver lakehouse (bhg_silver).
#
# Why DROP (not DELETE):
#   Recreates Delta table with fixed 498-column PascalCase TARGET_SCHEMA from the
#   silver notebook instead of legacy mixed-case column names.
#
# Bronze tables are NOT touched.

from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 77
method_name = "SFPatientPreAdmission"

PPA_SILVER_TABLES = [
    "bhg_silver.pats.tbl_SF_PatientPreAdmission",
    "bhg_silver.pats.SFPatientPreAdmission",
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
    .where(F.col("Method") == method_name)
    .where(F.col("IsActive") == 1)
    .select("TaskConfigId", "Method", "TargetPath", "TargetTable")
    .collect()
)

if config_rows:
    tables_to_drop = list(dict.fromkeys(row["TargetPath"] for row in config_rows if row["TargetPath"]))
    print(f"Using Silver TargetPath from TaskConfig (ConfigId={silver_config_id}).")
else:
    tables_to_drop = PPA_SILVER_TABLES
    print(f"TaskConfig returned no rows; using built-in list of {len(tables_to_drop)} table(s).")

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
after_status = []
for full_name in tables_to_drop:
    exists = table_exists(full_name)
    after_status.append((full_name, exists))
    print(f"{full_name}: exists={exists}")

still_present = [name for name, exists in after_status if exists]
if still_present:
    raise Exception(f"These tables still exist after DROP: {still_present}")

print(
    f"\nDone. Dropped {len(dropped)} PPA silver table(s); "
    f"{len(missing)} were already absent."
)
print("Next: deploy updated nb_ppa_sl_bronze_to_silver notebook, then run pl_ppa Silver step.")
