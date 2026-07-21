# DartsSrv TaskConfig RequestBody PascalCase updater
# Run in a Fabric Spark notebook attached to bhg_bronze.
#
# Updates audit-column references after Bronze/Silver notebook migration:
#   _ingest_run_id      -> IngestRunId
#   _site_code          -> SiteCode
#   _source_database    -> SourceDatabase
#
# Scope:
#   ConfigId 25 / BR  - all site-level Bronze rows (RequestBody metadata columns)
#   ConfigId 26 / SL  - Silver row (dq_keys + WatermarkColumn)
#   ConfigId 27 / GL  - Gold row (dq_keys if present)
#
# Target table: bhg_bronze.meta.taskconfig_custom_load only (custom-load pipeline).

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import StringType


# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

taskconfig_table = "bhg_bronze.meta.taskconfig_custom_load"

modified_by = "Harsha"

bronze_config_id = 25
silver_config_id = 26
gold_config_id = 27

bronze_full_table = "bhg_bronze.Dart.br_tblDartSrv"
silver_full_table = "bhg_silver.pats.sl_tbldartsrv"
gold_table_name = "gd_darts_srv"

silver_dq_keys = ["SiteCode", "dsID"]
gold_dq_keys = ["SiteCode", "DsId"]
silver_watermark_column = "SiteCode,dsID"

ingest_column = "IngestRunId"
site_column = "SiteCode"
database_column = "SourceDatabase"

column_renames = {
    "_ingest_run_id": "IngestRunId",
    "_site_code": "SiteCode",
    "_source_database": "SourceDatabase",
    "_extracted_at": "ExtractedAt",
    "_source_query_start_date": "SourceQueryStartDate",
    "_source_query_end_date": "SourceQueryEndDate",
    "silver_created_at": "CreatedInstanceAt",
    "silver_updated_at": "UpdatedInstanceAt",
    "last_seen_at": "LastSeenAt",
}


def rename_token(value):
    if value is None:
        return None
    text = str(value).strip()
    if text in column_renames:
        return column_renames[text]
    return text


def rename_csv(value):
    if value is None:
        return None
    parts = [rename_token(part.strip()) for part in str(value).split(",")]
    return ",".join(part for part in parts if part)


def rename_dq_keys(keys):
    if keys is None:
        return None
    if isinstance(keys, str):
        keys = [part.strip() for part in keys.split(",") if part.strip()]
    return [rename_token(key) for key in keys]


def build_bronze_request_body(existing_body):
    body = {}
    if existing_body:
        try:
            body = json.loads(existing_body)
        except Exception:
            body = {}

    body["full_table"] = body.get("full_table") or bronze_full_table
    body["ingest_column"] = ingest_column
    body["site_column"] = site_column
    body["database_column"] = database_column

    if "dq_keys" in body and body["dq_keys"]:
        body["dq_keys"] = rename_dq_keys(body["dq_keys"])

    return json.dumps(body, separators=(",", ":"))


def build_silver_request_body(existing_body):
    body = {}
    if existing_body:
        try:
            body = json.loads(existing_body)
        except Exception:
            body = {}

    body["full_table"] = body.get("full_table") or silver_full_table
    body["dq_keys"] = silver_dq_keys
    return json.dumps(body, separators=(",", ":"))


def build_gold_request_body(existing_body):
    body = {}
    if existing_body:
        try:
            body = json.loads(existing_body)
        except Exception:
            body = {}

    body["full_table"] = body.get("full_table") or gold_table_name
    body["dq_keys"] = gold_dq_keys
    return json.dumps(body, separators=(",", ":"))


@F.udf(StringType())
def update_request_body_udf(config_id, request_body):
    config_id = int(config_id)

    if config_id == bronze_config_id:
        return build_bronze_request_body(request_body)
    if config_id == silver_config_id:
        return build_silver_request_body(request_body)
    if config_id == gold_config_id:
        return build_gold_request_body(request_body)

    return request_body


# ---------------------------------------------------------------------------
# Preview current rows
# ---------------------------------------------------------------------------

target_config_ids = [bronze_config_id, silver_config_id, gold_config_id]

before_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(target_config_ids))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "SiteCode",
        "WatermarkColumn",
        "RequestBody",
        "IsActive",
    )
    .orderBy("ConfigId", "TaskConfigId")
)

print(f"TaskConfig table: {taskconfig_table}")
print(f"Rows to inspect/update: {before_df.count()}")
display(before_df)


# ---------------------------------------------------------------------------
# Build update dataset
# ---------------------------------------------------------------------------

updates_df = (
    before_df
    .withColumn(
        "NewRequestBody",
        update_request_body_udf(F.col("ConfigId"), F.col("RequestBody")),
    )
    .withColumn(
        "NewWatermarkColumn",
        F.when(
            F.col("ConfigId") == F.lit(silver_config_id),
            F.lit(silver_watermark_column),
        ).otherwise(
            F.when(
                F.col("WatermarkColumn").isNull(),
                F.lit(None).cast("string"),
            ).otherwise(
                F.udf(rename_csv, StringType())(F.col("WatermarkColumn"))
            )
        ),
    )
    .where(
        (F.col("RequestBody") != F.col("NewRequestBody"))
        | (
            F.col("NewWatermarkColumn").isNotNull()
            & (
                F.col("WatermarkColumn").isNull()
                | (F.col("WatermarkColumn") != F.col("NewWatermarkColumn"))
            )
        )
    )
)

change_count = updates_df.count()
print(f"Rows needing update: {change_count}")

if change_count == 0:
    print("No TaskConfig changes required. RequestBody already uses PascalCase audit columns.")
else:
    display(
        updates_df.select(
            "TaskConfigId",
            "ConfigId",
            "TaskName",
            "SiteCode",
            "WatermarkColumn",
            "NewWatermarkColumn",
            "RequestBody",
            "NewRequestBody",
        ).orderBy("ConfigId", "TaskConfigId")
    )


# ---------------------------------------------------------------------------
# Apply merge
# ---------------------------------------------------------------------------

if change_count > 0:
    target_columns = set(spark.table(taskconfig_table).columns)

    update_set = {
        "RequestBody": "source.NewRequestBody",
        "ModifiedBy": f"'{modified_by}'",
    }

    if "WatermarkColumn" in target_columns:
        update_set["WatermarkColumn"] = "source.NewWatermarkColumn"

    if "ModifiedAt" in target_columns:
        update_set["ModifiedAt"] = "current_timestamp()"

    DeltaTable.forName(spark, taskconfig_table).alias("target").merge(
        updates_df.alias("source"),
        "target.TaskConfigId = source.TaskConfigId",
    ).whenMatchedUpdate(set=update_set).execute()

    print(f"Updated {change_count} TaskConfig row(s).")


# ---------------------------------------------------------------------------
# Verification
# ---------------------------------------------------------------------------

after_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(target_config_ids))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "SiteCode",
        "WatermarkColumn",
        "RequestBody",
        "ModifiedBy",
        "ModifiedAt",
        "IsActive",
    )
    .orderBy("ConfigId", "TaskConfigId")
)

display(after_df)

# Spot-check Bronze template / one site row / Silver / Gold
display(
    spark.sql(
        f"""
        SELECT TaskConfigId, ConfigId, TaskName, WatermarkColumn, RequestBody
        FROM {taskconfig_table}
        WHERE ConfigId = {bronze_config_id}
        ORDER BY TaskConfigId
        LIMIT 5
        """
    )
)

display(
    spark.sql(
        f"""
        SELECT TaskConfigId, ConfigId, TaskName, WatermarkColumn, RequestBody
        FROM {taskconfig_table}
        WHERE ConfigId IN ({silver_config_id}, {gold_config_id})
        ORDER BY ConfigId
        """
    )
)

print("Expected Bronze RequestBody keys:")
print(
    json.dumps(
        {
            "full_table": bronze_full_table,
            "ingest_column": ingest_column,
            "site_column": site_column,
            "database_column": database_column,
        },
        indent=2,
    )
)
print("Expected Silver RequestBody keys:")
print(
    json.dumps(
        {"full_table": silver_full_table, "dq_keys": silver_dq_keys},
        indent=2,
    )
)
print("Expected Gold RequestBody keys:")
print(json.dumps({"full_table": gold_table_name, "dq_keys": gold_dq_keys}, indent=2))





# DartsSrv Silver TaskConfig table name fix
# Run in a Fabric Spark notebook attached to bhg_bronze.
#
# Scope: ConfigId = 26 only in bhg_bronze.meta.taskconfig_custom_load
# Fix:   sl_tblDartSrv -> sl_tbldartsrv (TargetTable, TargetPath, RequestBody)

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F


taskconfig_table = "bhg_bronze.meta.taskconfig_custom_load"
silver_config_id = 26
modified_by = "Harsha"

silver_target_schema = "pats"
silver_target_table = "sl_tbldartsrv"
silver_target_path = f"/lakehouse/bhg_silver/{silver_target_schema}/{silver_target_table}"
silver_full_table = f"bhg_silver.{silver_target_schema}.{silver_target_table}"
silver_watermark_column = "SiteCode,dsID"

request_body = json.dumps(
    {
        "full_table": silver_full_table,
        "dq_keys": ["SiteCode", "dsID"],
    },
    separators=(",", ":"),
)

before_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == F.lit(silver_config_id))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "WatermarkColumn",
        "RequestBody",
        "IsActive",
    )
)

row_count = before_df.count()
if row_count != 1:
    display(before_df)
    raise Exception(
        f"Expected exactly 1 Silver taskconfig row for ConfigId={silver_config_id}, found {row_count}."
    )

print("Before update:")
display(before_df)

target_columns = set(spark.table(taskconfig_table).columns)

update_set = {
    "TargetTable": F.lit(silver_target_table),
    "TargetPath": F.lit(silver_target_path),
    "RequestBody": F.lit(request_body),
    "WatermarkColumn": F.lit(silver_watermark_column),
    "ModifiedBy": F.lit(modified_by),
}

if "ModifiedAt" in target_columns:
    update_set["ModifiedAt"] = F.current_timestamp()

DeltaTable.forName(spark, taskconfig_table).update(
    condition=f"ConfigId = {silver_config_id}",
    set=update_set,
)

after_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == F.lit(silver_config_id))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "WatermarkColumn",
        "RequestBody",
        "ModifiedBy",
        "ModifiedAt",
    )
)

print("After update:")
display(after_df)

print(f"Updated ConfigId={silver_config_id} to TargetTable={silver_target_table}")
