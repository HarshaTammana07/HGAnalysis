# P1 Forms module Gold-only ETLConfig + TaskConfig setup
# Run this in a Fabric Spark notebook attached to the Bronze lakehouse.
#
# Existing P1 Forms:
#   ConfigId 97 = Bronze
#   ConfigId 98 = Silver
#
# This file adds only Gold:
#   ConfigId 99 = Gold
#   TaskConfigId 6796-6804 = one Gold row per P1 Forms method
#
# Gold is inserted inactive by default.
# Silver remains the terminal audited layer unless the pipeline is explicitly wired
# to run optional Gold from these taskconfig rows.

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, LongType, StringType, StructField, StructType


etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"

created_by = "Harsha"
environment_name = "DEV"
trigger_type = "SCHEDULE"
trigger_frequency = "DAILY"
triggered_by = "Fabric"
config_prefix = "SAMMS P1 Forms"

gold_config_id = 99
gold_start_task_config_id = 6796
gold_end_task_config_id = 6804

# Keep Gold disabled by default. Change to 1 only when the optional Gold
# publish flow is intentionally enabled.
gold_etl_is_active = 0
gold_task_is_active = 0

silver_lakehouse = "bhg_silver"
gold_warehouse = "bhg_gold"


forms_gold_tables = [
    {
        "display_name": "Comprehensive Assessment Form",
        "method": "SaveComprehensiveAssessmentForm",
        "schema": "pats",
        "table": "tbl_ComprehensiveAssessmentForm",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 1,
    },
    {
        "display_name": "E&M Form Pregnancy",
        "method": "SaveEMFormPregnancy",
        "schema": "pats",
        "table": "tbl_EandMFormPregnancy",
        "dq_keys": ["SiteCode", "EandMFormId"],
        "execution_order": 2,
    },
    {
        "display_name": "E&M Form MDM",
        "method": "SaveEMFormMDM",
        "schema": "pats",
        "table": "tbl_EandMFormMDM",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 3,
    },
    {
        "display_name": "SF Data Forms",
        "method": "SaveDataForms",
        "schema": "pats",
        "table": "tbl_SF_DataForms",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 4,
    },
    {
        "display_name": "SMS Text Consent Form",
        "method": "SaveSMSTextConsentForm",
        "schema": "pats",
        "table": "tbl_SMSTextConsentForm",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 5,
    },
    {
        "display_name": "Consent To Marketing",
        "method": "SaveConsenttoMarketing",
        "schema": "pats",
        "table": "tbl_ConsenttoMarketing",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 6,
    },
    {
        "display_name": "Take Home Agreement And Diversion Control",
        "method": "SaveTakeHomeAgreementandDiversionControl",
        "schema": "pats",
        "table": "tbl_TakeHomeAgreementandDiversionControl",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 7,
    },
    {
        "display_name": "Take Home Risk Assessment",
        "method": "SaveTakeHomeRiskAssessment",
        "schema": "pats",
        "table": "tbl_TakeHomeRiskAssessment",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 8,
    },
    {
        "display_name": "New Discharge Transfer Plan Form",
        "method": "SaveNewDischargeTransferPlanForm",
        "schema": "pats",
        "table": "tbl_NewDischargeTransferPlanForm",
        "dq_keys": ["SiteCode", "Id"],
        "execution_order": 9,
    },
]


def compact_json(payload):
    return json.dumps(payload, separators=(",", ":"))


# ---------------------------------------------------------------------------
# 1. Upsert Gold ETLConfig row
# ---------------------------------------------------------------------------

etl_schema = StructType([
    StructField("ConfigId", LongType(), True),
    StructField("ConfigName", StringType(), True),
    StructField("PipelineName", StringType(), True),
    StructField("PipelinePath", StringType(), True),
    StructField("TransformationModule", StringType(), True),
    StructField("SourceSystem", StringType(), True),
    StructField("SourceType", StringType(), True),
    StructField("TargetName", StringType(), True),
    StructField("EnvironmentName", StringType(), True),
    StructField("PipelineDependency", StringType(), True),
    StructField("ExecutionSequence", IntegerType(), True),
    StructField("TriggerType", StringType(), True),
    StructField("TriggerFrequency", StringType(), True),
    StructField("TriggeredBy", StringType(), True),
    StructField("IsActive", IntegerType(), True),
    StructField("ConnectionConfig", StringType(), True),
    StructField("CreatedBy", StringType(), True),
    StructField("ModifiedBy", StringType(), True),
])

etl_rows = [
    (
        gold_config_id,
        f"{config_prefix} Gold Pipeline",
        "pl_execute_forms",
        "/pipelines/pl_execute_forms",
        "Optional Gold Publish",
        "WAREHOUSE",
        "TABLE",
        "GL",
        environment_name,
        "P1_FORMS_SILVER",
        3,
        trigger_type,
        trigger_frequency,
        triggered_by,
        gold_etl_is_active,
        "{}",
        created_by,
        created_by,
    )
]

etl_df = (
    spark.createDataFrame(etl_rows, etl_schema)
    .withColumn("CreatedAt", F.current_timestamp())
    .withColumn("ModifiedAt", F.current_timestamp())
)

etl_cols = [field.name for field in etl_df.schema.fields]
etl_update = {
    column_name: f"source.{column_name}"
    for column_name in etl_cols
    if column_name not in ["ConfigId", "CreatedAt", "CreatedBy"]
}
etl_insert = {column_name: f"source.{column_name}" for column_name in etl_cols}

DeltaTable.forName(spark, etlconfig_table).alias("target") \
    .merge(etl_df.alias("source"), "target.ConfigId = source.ConfigId") \
    .whenMatchedUpdate(set=etl_update) \
    .whenNotMatchedInsert(values=etl_insert) \
    .execute()


# ---------------------------------------------------------------------------
# 2. Build one inactive Gold TaskConfig row per method
# ---------------------------------------------------------------------------

task_rows = []
for index, item in enumerate(forms_gold_tables):
    task_config_id = gold_start_task_config_id + index
    silver_full_table = f"{silver_lakehouse}.{item['schema']}.{item['table']}"
    gold_full_table = f"{gold_warehouse}.{item['schema']}.{item['table']}"
    watermark_column = ",".join(item["dq_keys"])

    task_rows.append({
        "TaskConfigId": task_config_id,
        "ConfigId": gold_config_id,
        "TaskName": f"P1 Forms {item['display_name']} Gold",
        "Endpoint": None,
        "Method": item["method"],
        "AuthType": "Warehouse",
        "SourceTable": silver_full_table,
        "PaginationEnabled": 0,
        "PaginationParam": None,
        "LoadType": "VERSIONED_FULL_OVERWRITE",
        "IsIncremental": 0,
        "WatermarkColumn": watermark_column,
        "LookbackDays": None,
        "TargetSchema": item["schema"],
        "TargetTable": item["table"],
        "TargetPath": gold_full_table,
        "ExecutionOrder": item["execution_order"],
        "RetryCount": 0,
        "TimeoutSeconds": 43200,
        "RequestBody": compact_json({
            "full_table": gold_full_table,
            "dq_keys": item["dq_keys"],
        }),
        "DependencyTaskConfigId": None,
        "SiteCode": None,
        "DataBaseName": None,
        "SiteName": None,
        "IsActive": gold_task_is_active,
        "CreatedBy": created_by,
        "ModifiedBy": created_by,
    })

if len(task_rows) != 9:
    raise Exception(f"Expected 9 P1 Forms Gold taskconfig rows, built {len(task_rows)}")

incoming_task_config_ids = [row["TaskConfigId"] for row in task_rows]
if incoming_task_config_ids[0] != gold_start_task_config_id or incoming_task_config_ids[-1] != gold_end_task_config_id:
    raise Exception(
        f"Unexpected Gold TaskConfigId range: "
        f"{incoming_task_config_ids[0]}-{incoming_task_config_ids[-1]}"
    )

seen_methods = {row["Method"] for row in task_rows}
expected_methods = {item["method"] for item in forms_gold_tables}
if seen_methods != expected_methods:
    raise Exception(f"Method mismatch: {sorted(seen_methods)} != {sorted(expected_methods)}")

for row in task_rows:
    parsed_request_body = json.loads(row["RequestBody"])
    expected_dq_keys = row["WatermarkColumn"].split(",")

    if not row["Method"]:
        raise Exception(f"Missing Method for TaskConfigId={row['TaskConfigId']}")
    if not row["SourceTable"] or not row["SourceTable"].startswith("bhg_silver."):
        raise Exception(f"Invalid SourceTable for TaskConfigId={row['TaskConfigId']}: {row['SourceTable']}")
    if row["TargetTable"].startswith("gd_") or row["TargetTable"].startswith("sl_"):
        raise Exception(f"Gold TargetTable must match final Silver table name, not gd_/sl_: {row['TargetTable']}")
    if parsed_request_body.get("full_table") != row["TargetPath"]:
        raise Exception(f"RequestBody full_table does not match TargetPath for TaskConfigId={row['TaskConfigId']}")
    if parsed_request_body.get("dq_keys") != expected_dq_keys:
        raise Exception(
            f"dq_keys and WatermarkColumn do not match for TaskConfigId={row['TaskConfigId']}: "
            f"{parsed_request_body.get('dq_keys')} != {expected_dq_keys}"
        )

existing_gold_config = (
    spark.table(etlconfig_table)
    .where(F.col("ConfigId") == gold_config_id)
    .select("ConfigId")
    .count()
)
if existing_gold_config == 0:
    raise Exception(f"Gold etlconfig ConfigId={gold_config_id} was not inserted.")

conflicting_taskconfig_df = (
    spark.table(taskconfig_table)
    .where(F.col("TaskConfigId").between(gold_start_task_config_id, gold_end_task_config_id))
    .where(F.col("ConfigId") != gold_config_id)
)

if conflicting_taskconfig_df.count() > 0:
    display(
        conflicting_taskconfig_df.select(
            "TaskConfigId",
            "ConfigId",
            "TaskName",
            "TargetSchema",
            "TargetTable",
            "IsActive",
        ).orderBy("TaskConfigId")
    )
    raise Exception(
        f"TaskConfigId range {gold_start_task_config_id}-{gold_end_task_config_id} "
        f"is already used outside ConfigId {gold_config_id}."
    )


task_schema = StructType([
    StructField("TaskConfigId", LongType(), True),
    StructField("ConfigId", LongType(), True),
    StructField("TaskName", StringType(), True),
    StructField("Endpoint", StringType(), True),
    StructField("Method", StringType(), True),
    StructField("AuthType", StringType(), True),
    StructField("SourceTable", StringType(), True),
    StructField("PaginationEnabled", IntegerType(), True),
    StructField("PaginationParam", StringType(), True),
    StructField("LoadType", StringType(), True),
    StructField("IsIncremental", IntegerType(), True),
    StructField("WatermarkColumn", StringType(), True),
    StructField("LookbackDays", IntegerType(), True),
    StructField("TargetSchema", StringType(), True),
    StructField("TargetTable", StringType(), True),
    StructField("TargetPath", StringType(), True),
    StructField("ExecutionOrder", IntegerType(), True),
    StructField("RetryCount", IntegerType(), True),
    StructField("TimeoutSeconds", IntegerType(), True),
    StructField("RequestBody", StringType(), True),
    StructField("DependencyTaskConfigId", LongType(), True),
    StructField("SiteCode", StringType(), True),
    StructField("DataBaseName", StringType(), True),
    StructField("SiteName", StringType(), True),
    StructField("IsActive", IntegerType(), True),
    StructField("CreatedBy", StringType(), True),
    StructField("ModifiedBy", StringType(), True),
])

task_df = (
    spark.createDataFrame(task_rows, task_schema)
    .withColumn("CreatedAt", F.current_timestamp())
    .withColumn("ModifiedAt", F.current_timestamp())
)

task_cols = [field.name for field in task_df.schema.fields]
task_update = {
    column_name: f"source.{column_name}"
    for column_name in task_cols
    if column_name not in ["TaskConfigId", "CreatedAt", "CreatedBy"]
}
task_insert = {column_name: f"source.{column_name}" for column_name in task_cols}

DeltaTable.forName(spark, taskconfig_table).alias("target") \
    .merge(task_df.alias("source"), "target.TaskConfigId = source.TaskConfigId") \
    .whenMatchedUpdate(set=task_update) \
    .whenNotMatchedInsert(values=task_insert) \
    .execute()


# ---------------------------------------------------------------------------
# 3. Verification output
# ---------------------------------------------------------------------------

display(spark.sql(f"""
SELECT
    ConfigId,
    ConfigName,
    PipelineName,
    PipelinePath,
    TransformationModule,
    SourceSystem,
    SourceType,
    TargetName,
    EnvironmentName,
    PipelineDependency,
    ExecutionSequence,
    IsActive,
    CreatedAt,
    CreatedBy,
    ModifiedAt,
    ModifiedBy
FROM {etlconfig_table}
WHERE ConfigId = {gold_config_id}
ORDER BY ConfigId
"""))

display(spark.sql(f"""
SELECT
    ConfigId,
    MIN(TaskConfigId) AS MinTaskConfigId,
    MAX(TaskConfigId) AS MaxTaskConfigId,
    COUNT(*) AS TaskCount,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveTaskCount
FROM {taskconfig_table}
WHERE ConfigId = {gold_config_id}
  AND TaskConfigId BETWEEN {gold_start_task_config_id} AND {gold_end_task_config_id}
GROUP BY ConfigId
"""))

display(spark.sql(f"""
SELECT
    TaskConfigId,
    ConfigId,
    TaskName,
    Method,
    AuthType,
    SourceTable,
    LoadType,
    IsIncremental,
    WatermarkColumn,
    LookbackDays,
    TargetSchema,
    TargetTable,
    TargetPath,
    SiteCode,
    DataBaseName,
    DependencyTaskConfigId,
    ExecutionOrder,
    IsActive,
    RequestBody,
    ModifiedAt,
    ModifiedBy
FROM {taskconfig_table}
WHERE ConfigId = {gold_config_id}
  AND TaskConfigId BETWEEN {gold_start_task_config_id} AND {gold_end_task_config_id}
ORDER BY TaskConfigId
"""))
