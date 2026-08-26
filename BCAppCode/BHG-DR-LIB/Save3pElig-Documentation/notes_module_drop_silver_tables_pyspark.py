# Notes — drop Silver tables for PascalCase reload.
# Run in a Fabric PySpark notebook attached to the Silver lakehouse (bhg_silver).
#
# Bronze and Gold are NOT touched.

from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 35

NOTES_SILVER_TABLES = [
    "bhg_silver.pats.tbl_3pARNOTE",
    "bhg_silver.pats.tbl_3pClaimNote",
    "bhg_silver.pats.sl_tbl_3pARNOTE",
    "bhg_silver.pats.sl_tbl_3pClaimNote",
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
    .select("TaskConfigId", "Method", "TargetPath")
    .collect()
)

if config_rows:
    tables_to_drop = list(dict.fromkeys(row["TargetPath"] for row in config_rows if row["TargetPath"]))
    print(f"Using Silver TargetPath from TaskConfig (ConfigId={silver_config_id}).")
else:
    tables_to_drop = NOTES_SILVER_TABLES[:2]
    print("TaskConfig empty; using built-in silver table list.")

print("\n--- BEFORE ---")
before = []
for full_name in tables_to_drop:
    exists = table_exists(full_name)
    rows = spark.table(full_name).count() if exists else None
    before.append((full_name, exists, rows))
    print(f"{full_name}: exists={exists}, rows={rows}")

print("\n--- DROP ---")
dropped = []
for full_name in tables_to_drop:
    if not table_exists(full_name):
        print(f"SKIP: {full_name}")
        continue
    spark.sql(f"DROP TABLE IF EXISTS {full_name}")
    dropped.append(full_name)
    print(f"DROPPED: {full_name}")

still = [name for name, exists, _ in before if table_exists(name)]
if still:
    raise Exception(f"Tables still present after DROP: {still}")

print(f"\nDone. Dropped {len(dropped)} table(s).")
print("Next: deploy nb_3parnote + nb_3pclaimnote notebooks, run Notes Silver pipeline.")
