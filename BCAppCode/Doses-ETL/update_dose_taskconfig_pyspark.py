# Dose TaskConfig updater — Silver + Gold full_table / TargetSchema fixes
# Run in a Fabric Spark notebook attached to bhg_bronze.
#
# Fixes audit notebook reading wrong Gold table (bhg_gold.pats.tbl_dose) caused by
# RequestBody full_table pointing at Silver paths.
#
# Scope:
#   ConfigId 8 / SL — Dose + DoseExcuse pipeline rows (non site-level)
#   ConfigId 9 / GL — Dose + DoseExcuse pipeline rows
# Active and inactive rows are updated so toggled-off rows do not keep stale metadata.
#
# Target table: bhg_bronze.meta.taskconfig

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import StringType

# ---------------------------------------------------------------------------
# Configuration
# ---------------------------------------------------------------------------

taskconfig_table = "bhg_bronze.meta.taskconfig"
modified_by = "FabricMigration"

bronze_config_id = 7
silver_config_id = 8
gold_config_id = 9

# Match deployed notebook / warehouse object names
DOSE_BRONZE_FULL_TABLE = "bhg_bronze.Dose.br_tblDose"
DOSE_EXCUSE_BRONZE_FULL_TABLE = "bhg_bronze.Dose.br_tblDoseExcuse"
DOSE_SILVER_FULL_TABLE = "bhg_silver.pats.tbl_dose"
DOSE_EXCUSE_SILVER_FULL_TABLE = "bhg_silver.pats.tbl_dose_excuse"
DOSE_GOLD_FULL_TABLE = "bhg_gold.pats.tblDOSE"
DOSE_EXCUSE_GOLD_FULL_TABLE = "bhg_gold.pats.DOSEExcuse"

DOSE_SILVER_TARGET_TABLE = "tbl_dose"
DOSE_EXCUSE_SILVER_TARGET_TABLE = "tbl_dose_excuse"
DOSE_GOLD_TARGET_TABLE = "tblDOSE"
DOSE_EXCUSE_GOLD_TARGET_TABLE = "DOSEExcuse"

GOLD_TARGET_SCHEMA = "bhg_gold.pats"
SILVER_TARGET_SCHEMA = "bhg_silver.pats"

DOSE_DQ_KEYS = ["SiteCode", "DoseId"]
DOSE_EXCUSE_DQ_KEYS = ["SiteCode", "ExId"]

# Known TaskConfigIds from DEV (also match by ConfigId + Method)
KNOWN_TASK_CONFIG_IDS = {
    ("SL", "Dose"): 384,
    ("GL", "Dose"): 94387,
    ("SL", "DoseExcuse"): 94385,
    ("GL", "DoseExcuse"): 94386,
}


def method_key(method):
    return str(method or "").replace(" ", "").lower()


def build_request_body(full_table, dq_keys):
    return json.dumps(
        {
            "full_table": full_table,
            "dq_keys": dq_keys,
        },
        separators=(",", ":"),
    )


def build_bronze_request_body(full_table, dq_keys):
    return json.dumps(
        {
            "full_table": full_table,
            "ingest_column": "IngestRunId",
            "site_column": "SiteCode",
            "database_column": "SourceDatabase",
            "dq_keys": dq_keys,
        },
        separators=(",", ":"),
    )


def expected_update(layer, method):
    layer = str(layer).upper()
    method = str(method)
    mk = method_key(method)

    if layer == "SL" and mk == "dose":
        return {
            "TargetSchema": SILVER_TARGET_SCHEMA,
            "TargetTable": DOSE_SILVER_TARGET_TABLE,
            "RequestBody": build_request_body(DOSE_SILVER_FULL_TABLE, DOSE_DQ_KEYS),
        }
    if layer == "SL" and mk == "doseexcuse":
        return {
            "TargetSchema": SILVER_TARGET_SCHEMA,
            "TargetTable": DOSE_EXCUSE_SILVER_TARGET_TABLE,
            "RequestBody": build_request_body(DOSE_EXCUSE_SILVER_FULL_TABLE, DOSE_EXCUSE_DQ_KEYS),
        }
    if layer == "GL" and mk == "dose":
        return {
            "SourceTable": DOSE_SILVER_FULL_TABLE,
            "TargetSchema": GOLD_TARGET_SCHEMA,
            "TargetTable": DOSE_GOLD_TARGET_TABLE,
            "RequestBody": build_request_body(DOSE_GOLD_FULL_TABLE, DOSE_DQ_KEYS),
        }
    if layer == "GL" and mk == "doseexcuse":
        return {
            "SourceTable": DOSE_EXCUSE_SILVER_FULL_TABLE,
            "TargetSchema": GOLD_TARGET_SCHEMA,
            "TargetTable": DOSE_EXCUSE_GOLD_TARGET_TABLE,
            "RequestBody": build_request_body(DOSE_EXCUSE_GOLD_FULL_TABLE, DOSE_EXCUSE_DQ_KEYS),
        }
    return None


@F.udf(StringType())
def build_request_body_udf(layer, method):
    spec = expected_update(layer, method)
    return spec["RequestBody"] if spec else None


@F.udf(StringType())
def build_target_schema_udf(layer, method):
    spec = expected_update(layer, method)
    return spec["TargetSchema"] if spec else None


@F.udf(StringType())
def build_target_table_udf(layer, method):
    spec = expected_update(layer, method)
    return spec["TargetTable"] if spec else None


@F.udf(StringType())
def build_source_table_udf(layer, method):
    spec = expected_update(layer, method)
    return spec.get("SourceTable") if spec else None


# ---------------------------------------------------------------------------
# Preview candidate rows
# ---------------------------------------------------------------------------

base_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(silver_config_id, gold_config_id))
    .where(F.col("Method").isin("Dose", "DoseExcuse"))
    .where(F.col("SiteCode").isNull())
)

print(f"TaskConfig table: {taskconfig_table}")
print(f"Dose/DoseExcuse SL+GL pipeline rows: {base_df.count()}")
display(
    base_df.select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        "SourceTable",
        "TargetSchema",
        "TargetTable",
        "RequestBody",
    ).orderBy("ConfigId", "Method")
)

layer_col = (
    F.when(F.col("ConfigId") == silver_config_id, F.lit("SL"))
    .when(F.col("ConfigId") == gold_config_id, F.lit("GL"))
)

updates_df = (
    base_df
    .withColumn("Layer", layer_col)
    .withColumn("NewSourceTable", build_source_table_udf(F.col("Layer"), F.col("Method")))
    .withColumn("NewTargetSchema", build_target_schema_udf(F.col("Layer"), F.col("Method")))
    .withColumn("NewTargetTable", build_target_table_udf(F.col("Layer"), F.col("Method")))
    .withColumn("NewRequestBody", build_request_body_udf(F.col("Layer"), F.col("Method")))
    .where(F.col("NewRequestBody").isNotNull())
    .where(
        (F.coalesce(F.col("TargetSchema"), F.lit("")) != F.col("NewTargetSchema"))
        | (F.coalesce(F.col("TargetTable"), F.lit("")) != F.col("NewTargetTable"))
        | (F.coalesce(F.col("RequestBody"), F.lit("")) != F.col("NewRequestBody"))
        | (F.col("NewSourceTable").isNotNull() & (F.coalesce(F.col("SourceTable"), F.lit("")) != F.col("NewSourceTable")))
    )
)

change_count = updates_df.count()
print(f"Rows needing update: {change_count}")

if change_count == 0:
    print("No TaskConfig changes required.")
else:
    display(
        updates_df.select(
            "TaskConfigId",
            "ConfigId",
            "Method",
            "SourceTable",
            "NewSourceTable",
            "TargetSchema",
            "NewTargetSchema",
            "TargetTable",
            "NewTargetTable",
            "RequestBody",
            "NewRequestBody",
        ).orderBy("ConfigId", "Method")
    )

# ---------------------------------------------------------------------------
# Apply MERGE
# ---------------------------------------------------------------------------

APPLY_CHANGES = True  # set False to preview only
target_columns = set(spark.table(taskconfig_table).columns)

if APPLY_CHANGES and change_count > 0:
    staging_df = updates_df.select(
        "TaskConfigId",
        F.col("NewSourceTable").alias("SourceTable"),
        F.col("NewTargetSchema").alias("TargetSchema"),
        F.col("NewTargetTable").alias("TargetTable"),
        F.col("NewRequestBody").alias("RequestBody"),
    )

    update_set = {
        "TargetSchema": "source.TargetSchema",
        "TargetTable": "source.TargetTable",
        "RequestBody": "source.RequestBody",
        "ModifiedBy": f"'{modified_by}'",
    }
    if "SourceTable" in target_columns:
        update_set["SourceTable"] = "CASE WHEN source.SourceTable IS NOT NULL THEN source.SourceTable ELSE target.SourceTable END"
    if "ModifiedAt" in target_columns:
        update_set["ModifiedAt"] = "current_timestamp()"

    (
        DeltaTable.forName(spark, taskconfig_table)
        .alias("target")
        .merge(
            staging_df.alias("source"),
            "target.TaskConfigId = source.TaskConfigId",
        )
        .whenMatchedUpdate(set=update_set)
        .execute()
    )
    print(f"Updated {change_count} taskconfig row(s).")
elif not APPLY_CHANGES:
    print("APPLY_CHANGES=False — preview only, no rows written.")

# ---------------------------------------------------------------------------
# Post-check
# ---------------------------------------------------------------------------

after_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(silver_config_id, gold_config_id))
    .where(F.col("Method").isin("Dose", "DoseExcuse"))
    .where(F.col("SiteCode").isNull())
    .select(
        "TaskConfigId",
        "ConfigId",
        "Method",
        "SourceTable",
        "TargetSchema",
        "TargetTable",
        "RequestBody",
    )
    .orderBy("ConfigId", "Method")
)

print("Post-update rows:")
display(after_df)

print("\nExpected RequestBody values:")
print(f"  Dose SL:       {build_request_body(DOSE_SILVER_FULL_TABLE, DOSE_DQ_KEYS)}")
print(f"  DoseExcuse SL: {build_request_body(DOSE_EXCUSE_SILVER_FULL_TABLE, DOSE_EXCUSE_DQ_KEYS)}")
print(f"  Dose GL:       {build_request_body(DOSE_GOLD_FULL_TABLE, DOSE_DQ_KEYS)}")
print(f"  DoseExcuse GL: {build_request_body(DOSE_EXCUSE_GOLD_FULL_TABLE, DOSE_EXCUSE_DQ_KEYS)}")

# ---------------------------------------------------------------------------
# Bronze RequestBody DQ-key update
# ---------------------------------------------------------------------------

dose_bronze_request_body = build_bronze_request_body(DOSE_BRONZE_FULL_TABLE, DOSE_DQ_KEYS)
dose_excuse_bronze_request_body = build_bronze_request_body(DOSE_EXCUSE_BRONZE_FULL_TABLE, DOSE_EXCUSE_DQ_KEYS)

bronze_updates_df = (
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == bronze_config_id)
    .where(F.col("Method").isin("Dose", "DoseExcuse"))
    .withColumn(
        "NewRequestBody",
        F.when(F.lower(F.col("Method")) == F.lit("dose"), F.lit(dose_bronze_request_body))
        .when(F.lower(F.col("Method")) == F.lit("doseexcuse"), F.lit(dose_excuse_bronze_request_body))
    )
    .where(F.col("NewRequestBody").isNotNull())
    .where(F.coalesce(F.col("RequestBody"), F.lit("")) != F.col("NewRequestBody"))
)

bronze_change_count = bronze_updates_df.count()
print(f"\nBronze rows needing RequestBody update: {bronze_change_count}")

if APPLY_CHANGES and bronze_change_count > 0:
    bronze_staging_df = bronze_updates_df.select(
        "TaskConfigId",
        F.col("NewRequestBody").alias("RequestBody"),
    )

    bronze_update_set = {
        "RequestBody": "source.RequestBody",
        "ModifiedBy": f"'{modified_by}'",
    }
    if "ModifiedAt" in target_columns:
        bronze_update_set["ModifiedAt"] = "current_timestamp()"

    (
        DeltaTable.forName(spark, taskconfig_table)
        .alias("target")
        .merge(
            bronze_staging_df.alias("source"),
            "target.TaskConfigId = source.TaskConfigId",
        )
        .whenMatchedUpdate(set=bronze_update_set)
        .execute()
    )
    print(f"Updated {bronze_change_count} Bronze taskconfig row(s).")
elif not APPLY_CHANGES:
    print("APPLY_CHANGES=False — Bronze preview only, no rows written.")

display(
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == bronze_config_id)
    .where(F.col("Method").isin("Dose", "DoseExcuse"))
    .select("TaskConfigId", "ConfigId", "Method", "SiteCode", "DataBaseName", "RequestBody", "ModifiedAt", "ModifiedBy")
    .orderBy("Method", "SiteCode", "TaskConfigId")
)
