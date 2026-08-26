# P1 Finance — drop all 14 Silver tables for PascalCase reload.
# Run in a Fabric PySpark notebook attached to the Silver lakehouse (bhg_silver).
#
# Why DROP (not DELETE):
#   Existing silver tables may still have legacy camelCase column names.
#   DROP removes table + schema so the silver notebooks recreate Delta tables
#   with the new PascalCase TARGET_SCHEMA on the next Bronze → Silver run.
#
# Bronze tables are NOT touched.

from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 47
silver_schema = "pats"
silver_catalog = "bhg_silver"

# Fallback if taskconfig is unavailable — matches Taskconfigcurrentrows.txt Silver rows.
FINANCE_SILVER_TABLES = [
    "bhg_silver.pats.tbl_Bills",
    "bhg_silver.pats.tbl_pbi3PayAuth",
    "bhg_silver.pats.tbl_vw3pBillSub",
    "bhg_silver.pats.tbl_Fmp",
    "bhg_silver.pats.tbl_PayerCltHistory",
    "bhg_silver.pats.tbl_FinancialHardshipApplication",
    "bhg_silver.pats.tbl_3pElig",
    "bhg_silver.pats.tbl_ClaimLineItem",
    "bhg_silver.pats.tbl_ClaimLineItemActivity",
    "bhg_silver.pats.tbl_Claims",
    "bhg_silver.pats.tbl_PayerClient",
    "bhg_silver.pats.tbl_tbldiag10",
    "bhg_silver.pats.tbl_ClientDemo1",
    "bhg_silver.pats.tbl_ClientDemo2",
]


def table_exists(full_name):
    try:
        spark.table(full_name)
        return True
    except Exception:
        return False


# Prefer TargetPath from Silver TaskConfig rows (ConfigId 47).
config_rows = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == silver_config_id)
    .select("TaskConfigId", "Method", "TargetPath", "TargetTable")
    .orderBy("ExecutionOrder", "TaskConfigId")
    .collect()
)

if len(config_rows) == 14:
    tables_to_drop = [row["TargetPath"] for row in config_rows]
    print("Using 14 Silver TargetPath values from TaskConfig (ConfigId=47).")
else:
    tables_to_drop = FINANCE_SILVER_TABLES
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
    f"\nDone. Dropped {len(dropped)} finance silver table(s); "
    f"{len(missing)} were already absent."
)
print("Next: run pl_p1_finance (or Silver child pipeline) to reload from Bronze.")
