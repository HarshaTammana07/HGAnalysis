# P1 Forms module TaskConfig setup
# Run this in a Fabric Spark notebook attached to the Bronze lakehouse.
#
# ID layout:
#   For each Forms table:
#     1 inactive generic Bronze row, 115 active Bronze site rows, then 1 Silver row.
#   Start TaskConfigId: 5743
#   End TaskConfigId:   6795
#
# Forms is Bronze + Silver only. No Gold TaskConfig rows are created here.

import json
import re

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, LongType, StringType, StructField, StructType


etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"

created_by = "Harsha"
start_task_config_id = 5743

bronze_config_id = 97
silver_config_id = 98

# Reuse the already-approved SAMMS site universe from P1 Reference TaskConfig.
reference_bronze_config_id = 88
expected_site_count = 115

bronze_schema = "P1Forms"
bronze_lakehouse = "bhg_bronze"
silver_lakehouse = "bhg_silver"

ingest_column = "IngestRunId"
site_column = "SiteCode"
database_column = "SourceDatabase"


forms_tables = [
    {
        "display_name": "Comprehensive Assessment Form",
        "method": "SaveComprehensiveAssessmentForm",
        "source_table": "dbo.ComprehensiveAssessmentForm",
        "bronze_table": "br_samms_comprehensive_assessment_form",
        "silver_schema": "pats",
        "silver_table": "tbl_ComprehensiveAssessmentForm",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "E&M Form Pregnancy",
        "method": "SaveEMFormPregnancy",
        "source_table": "dbo.EandMFormPregnancy",
        "bronze_table": "br_samms_eandm_form_pregnancy",
        "silver_schema": "pats",
        "silver_table": "tbl_EandMFormPregnancy",
        "dq_keys": ["SiteCode", "EandMFormId"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "E&M Form MDM",
        "method": "SaveEMFormMDM",
        "source_table": "dbo.EandMForm",
        "bronze_table": "br_samms_eandm_form_mdm",
        "silver_schema": "pats",
        "silver_table": "tbl_EandMFormMDM",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "SF Data Forms",
        "method": "SaveDataForms",
        "source_table": "dbo.SF_DataForms",
        "bronze_table": "br_samms_sf_data_forms",
        "silver_schema": "pats",
        "silver_table": "tbl_SF_DataForms",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 1,
        "lookback_days": 15,
    },
    {
        "display_name": "SMS Text Consent Form",
        "method": "SaveSMSTextConsentForm",
        "source_table": "dbo.SMSTextConsentForm",
        "bronze_table": "br_samms_sms_text_consent_form",
        "silver_schema": "pats",
        "silver_table": "tbl_SMSTextConsentForm",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Consent To Marketing",
        "method": "SaveConsenttoMarketing",
        "source_table": "dbo.consenttomarketing",
        "bronze_table": "br_samms_consent_to_marketing",
        "silver_schema": "pats",
        "silver_table": "tbl_ConsenttoMarketing",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 1,
        "lookback_days": 15,
    },
    {
        "display_name": "Take Home Agreement And Diversion Control",
        "method": "SaveTakeHomeAgreementandDiversionControl",
        "source_table": "dbo.takehomeagreementanddiversioncontrol",
        "bronze_table": "br_samms_take_home_agreement_diversion_control",
        "silver_schema": "pats",
        "silver_table": "tbl_TakeHomeAgreementandDiversionControl",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 1,
        "lookback_days": 15,
    },
    {
        "display_name": "Take Home Risk Assessment",
        "method": "SaveTakeHomeRiskAssessment",
        "source_table": "dbo.TakeHomeRiskAssessment",
        "bronze_table": "br_samms_take_home_risk_assessment",
        "silver_schema": "pats",
        "silver_table": "tbl_TakeHomeRiskAssessment",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "New Discharge Transfer Plan Form",
        "method": "SaveNewDischargeTransferPlanForm",
        "source_table": "dbo.newdischargetransferplanform",
        "bronze_table": "br_samms_new_discharge_transfer_plan_form",
        "silver_schema": "pats",
        "silver_table": "tbl_NewDischargeTransferPlanForm",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 1,
        "lookback_days": 15,
    },
]


def full_table(lakehouse, schema_name, table_name):
    return f"{lakehouse}.{schema_name}.{table_name}"


def request_body(payload):
    return json.dumps(payload, separators=(",", ":"))


def site_name_from_database(source_database):
    site_name = re.sub(r"^SAMMS-", "", source_database or "")
    site_name = re.sub(r"(?i)V[0-9]+$", "", site_name)
    return site_name.strip()


def make_task(
    task_config_id,
    config_id,
    task_name,
    method,
    auth_type,
    source_table,
    load_type,
    is_incremental,
    watermark_column,
    lookback_days,
    target_schema,
    target_table,
    target_path,
    execution_order,
    request_payload,
    dependency_task_config_id,
    site_code=None,
    database_name=None,
    site_name=None,
    is_active=1,
):
    return {
        "TaskConfigId": task_config_id,
        "ConfigId": config_id,
        "TaskName": task_name,
        "Endpoint": None,
        "Method": method,
        "AuthType": auth_type,
        "SourceTable": source_table,
        "PaginationEnabled": 0,
        "PaginationParam": None,
        "LoadType": load_type,
        "IsIncremental": is_incremental,
        "WatermarkColumn": watermark_column,
        "LookbackDays": lookback_days,
        "TargetSchema": target_schema,
        "TargetTable": target_table,
        "TargetPath": target_path,
        "ExecutionOrder": execution_order,
        "RetryCount": 0,
        "TimeoutSeconds": 43200,
        "RequestBody": request_body(request_payload),
        "DependencyTaskConfigId": dependency_task_config_id,
        "SiteCode": site_code,
        "DataBaseName": database_name,
        "SiteName": site_name,
        "IsActive": is_active,
        "CreatedBy": created_by,
        "ModifiedBy": created_by,
    }


site_rows = (
    spark.table(taskconfig_table)
    .where(
        (F.col("ConfigId") == reference_bronze_config_id)
        & F.col("SiteCode").isNotNull()
        & F.col("DataBaseName").isNotNull()
    )
    .select(
        F.col("SiteCode").alias("site_code"),
        F.col("DataBaseName").alias("source_database"),
    )
    .dropDuplicates()
    .orderBy("site_code")
    .collect()
)

samms_sites = [
    {"site_code": row.site_code, "source_database": row.source_database}
    for row in site_rows
]

if len(samms_sites) != expected_site_count:
    raise Exception(
        f"Expected {expected_site_count} SAMMS sites from Reference TaskConfig "
        f"ConfigId={reference_bronze_config_id}, found {len(samms_sites)}."
    )

site_codes = [site["site_code"] for site in samms_sites]
if len(site_codes) != len(set(site_codes)):
    raise Exception("Duplicate site_code values found in Reference TaskConfig site list.")


task_rows = []
task_id = start_task_config_id

for table_order, item in enumerate(forms_tables, start=1):
    bronze_full_table = full_table(bronze_lakehouse, bronze_schema, item["bronze_table"])
    silver_full_table = full_table(silver_lakehouse, item["silver_schema"], item["silver_table"])
    dq_watermark_column = ",".join(item["dq_keys"])
    bronze_load_type = "INCREMENTAL" if item["is_incremental"] else "FULL"

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=bronze_config_id,
            task_name=f"P1 Forms {item['display_name']} Bronze",
            method=item["method"],
            auth_type="SQLServer",
            source_table=item["source_table"],
            load_type=bronze_load_type,
            is_incremental=item["is_incremental"],
            watermark_column=dq_watermark_column,
            lookback_days=item["lookback_days"],
            target_schema=bronze_schema,
            target_table=item["bronze_table"],
            target_path=bronze_full_table,
            execution_order=table_order,
            request_payload={
                "full_table": bronze_full_table,
                "ingest_column": ingest_column,
                "site_column": site_column,
                "database_column": database_column,
                "dq_keys": item["dq_keys"],
            },
            dependency_task_config_id=None,
            is_active=0,
        )
    )
    task_id += 1

    for site in samms_sites:
        task_rows.append(
            make_task(
                task_config_id=task_id,
                config_id=bronze_config_id,
                task_name=f"P1 Forms {item['display_name']} Bronze - {site['site_code']}",
                method=item["method"],
                auth_type="SQLServer",
                source_table=item["source_table"],
                load_type=bronze_load_type,
                is_incremental=item["is_incremental"],
                watermark_column=dq_watermark_column,
                lookback_days=item["lookback_days"],
                target_schema=bronze_schema,
                target_table=item["bronze_table"],
                target_path=bronze_full_table,
                execution_order=table_order,
                request_payload={
                    "full_table": bronze_full_table,
                    "ingest_column": ingest_column,
                    "site_column": site_column,
                    "database_column": database_column,
                    "dq_keys": item["dq_keys"],
                },
                dependency_task_config_id=None,
                site_code=site["site_code"],
                database_name=site["source_database"],
                site_name=site_name_from_database(site["source_database"]),
            )
        )
        task_id += 1

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=silver_config_id,
            task_name=f"P1 Forms {item['display_name']} Silver",
            method=item["method"],
            auth_type="Lakehouse",
            source_table=bronze_full_table,
            load_type="MERGE",
            is_incremental=item["is_incremental"],
            watermark_column=dq_watermark_column,
            lookback_days=item["lookback_days"],
            target_schema=item["silver_schema"],
            target_table=item["silver_table"],
            target_path=silver_full_table,
            execution_order=table_order,
            request_payload={
                "full_table": silver_full_table,
                "dq_keys": item["dq_keys"],
            },
            dependency_task_config_id=None,
        )
    )
    task_id += 1


expected_task_count = len(forms_tables) * (len(samms_sites) + 2)
if len(task_rows) != expected_task_count:
    raise Exception(f"Expected {expected_task_count} taskconfig rows, built {len(task_rows)}")

end_task_config_id = start_task_config_id + expected_task_count - 1
incoming_task_config_ids = [row["TaskConfigId"] for row in task_rows]

if incoming_task_config_ids[0] != 5743 or incoming_task_config_ids[-1] != 6795:
    raise Exception(
        f"Unexpected TaskConfigId range: {incoming_task_config_ids[0]}-{incoming_task_config_ids[-1]}"
    )

seen_methods = {row["Method"] for row in task_rows}
expected_methods = {item["method"] for item in forms_tables}
if seen_methods != expected_methods:
    raise Exception(f"Method mismatch: {sorted(seen_methods)} != {sorted(expected_methods)}")

for row in task_rows:
    parsed_request_body = json.loads(row["RequestBody"])
    expected_dq_keys = row["WatermarkColumn"].split(",")

    if not row["Method"]:
        raise Exception(f"Missing Method for TaskConfigId={row['TaskConfigId']}")

    if not parsed_request_body.get("full_table"):
        raise Exception(f"Missing full_table in RequestBody for TaskConfigId={row['TaskConfigId']}")

    if parsed_request_body.get("dq_keys") != expected_dq_keys:
        raise Exception(
            f"dq_keys and WatermarkColumn do not match for TaskConfigId={row['TaskConfigId']}: "
            f"{parsed_request_body.get('dq_keys')} != {expected_dq_keys}"
        )

    if row["ConfigId"] == bronze_config_id:
        for column_name in ["ingest_column", "site_column", "database_column"]:
            if not parsed_request_body.get(column_name):
                raise Exception(f"Missing {column_name} in Bronze RequestBody for TaskConfigId={row['TaskConfigId']}")
        if row["SiteCode"] and not row["DataBaseName"]:
            raise Exception(f"Bronze row missing SiteCode/DataBaseName for TaskConfigId={row['TaskConfigId']}")


required_config_ids = [bronze_config_id, silver_config_id]
existing_config_ids = {
    row.ConfigId
    for row in (
        spark.table(etlconfig_table)
        .where(F.col("ConfigId").isin(required_config_ids))
        .select("ConfigId")
        .collect()
    )
}

missing_config_ids = sorted(set(required_config_ids) - existing_config_ids)
if missing_config_ids:
    raise Exception(f"Missing etlconfig rows for ConfigId(s): {missing_config_ids}. Run ETLConfig setup first.")


conflicting_taskconfig_df = (
    spark.table(taskconfig_table)
    .where(F.col("TaskConfigId").between(start_task_config_id, end_task_config_id))
    .where(~F.col("ConfigId").isin(required_config_ids))
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
        f"TaskConfigId range {start_task_config_id}-{end_task_config_id} is already used outside "
        f"ConfigIds {required_config_ids}. Choose a new start_task_config_id before running this setup."
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


display(spark.sql(f"""
SELECT
    ConfigId,
    TargetTable,
    MIN(TaskConfigId) AS MinTaskConfigId,
    MAX(TaskConfigId) AS MaxTaskConfigId,
    COUNT(*) AS TaskCount,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveTaskCount,
    SUM(CASE WHEN SiteCode IS NOT NULL THEN 1 ELSE 0 END) AS SiteTaskCount
FROM {taskconfig_table}
WHERE TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
GROUP BY ConfigId, TargetTable
ORDER BY MinTaskConfigId
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
    SiteName,
    DependencyTaskConfigId,
    ExecutionOrder,
    IsActive
FROM {taskconfig_table}
WHERE TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
ORDER BY TaskConfigId
LIMIT 60
"""))
