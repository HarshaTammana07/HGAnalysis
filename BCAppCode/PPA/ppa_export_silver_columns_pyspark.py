# PPA — export current Silver table column list (schema snapshot).
# Run in a Fabric PySpark notebook attached to the Silver lakehouse (bhg_silver).
#
# Use this before building a full PascalCase silver TARGET_SCHEMA map.
# Paste tab-separated output into a .txt file and share for schema work.

from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 77
method_name = "SFPatientPreAdmission"

# Fallbacks if TaskConfig lookup fails (notebook default vs TaskConfig TargetPath).
SILVER_TABLE_CANDIDATES = [
    "bhg_silver.pats.tbl_SF_PatientPreAdmission",
    "bhg_silver.pats.SFPatientPreAdmission",
]


def table_exists(full_name):
    try:
        spark.table(full_name)
        return True
    except Exception:
        return False


def resolve_silver_table():
    rows = (
        spark.table(taskconfig_table)
        .where(F.col("ConfigId") == silver_config_id)
        .where(F.col("Method") == method_name)
        .where(F.col("IsActive") == 1)
        .select("TaskConfigId", "TargetPath", "TargetTable", "RequestBody")
        .limit(1)
        .collect()
    )
    if rows:
        target_path = rows[0]["TargetPath"]
        if target_path and table_exists(target_path):
            print(f"Using Silver TargetPath from TaskConfig (ConfigId={silver_config_id}): {target_path}")
            return target_path

    for candidate in SILVER_TABLE_CANDIDATES:
        if table_exists(candidate):
            print(f"Using fallback silver table: {candidate}")
            return candidate

    raise Exception(
        "PPA silver table not found. Expected one of: "
        + ", ".join(SILVER_TABLE_CANDIDATES)
    )


silver_table = resolve_silver_table()
silver_df = spark.table(silver_table)
row_count = silver_df.count()

schema_rows = []
for idx, field in enumerate(silver_df.schema.fields, start=1):
    schema_rows.append(
        {
            "ordinal_position": idx,
            "column_name": field.name,
            "spark_data_type": field.dataType.simpleString(),
            "nullable": field.nullable,
        }
    )

schema_df = spark.createDataFrame(schema_rows).orderBy("ordinal_position")

print(f"\n--- PPA Silver schema: {silver_table} ---")
print(f"Column count: {len(schema_rows)}")
print(f"Row count: {row_count}")
display(schema_df)

# Tab-separated block — copy/paste into ppa_silver_columns_current.txt
header = "OrdinalPosition\tColumnName\tSparkDataType\tNullable"
lines = [header]
for row in schema_df.collect():
    lines.append(
        f"{row['ordinal_position']}\t{row['column_name']}\t{row['spark_data_type']}\t{row['nullable']}"
    )

print("\n--- COPY BELOW (tab-separated) ---")
print("\n".join(lines))

# Optional: column-name-only list for quick diff
print("\n--- COLUMN NAMES ONLY (one per line) ---")
for row in schema_df.collect():
    print(row["column_name"])
