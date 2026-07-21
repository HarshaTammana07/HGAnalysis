# P1 Reference Silver TaskConfig name/path updater
# Run in a Fabric Spark notebook.
# Scope: ConfigId = 89 only.

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, StringType, StructField, StructType


taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 89
modified_by = "Harsha"


silver_rows = [
    {
        "Method": "SaveClinic",
        "TaskName": "P1 Reference Clinic Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_clinic",
        "TargetSchema": "ctrl",
        "TargetTable": "tbl_Clinic",
        "TargetPath": "bhg_silver.ctrl.tbl_Clinic",
        "WatermarkColumn": "SiteCode,PKEY",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.ctrl.tbl_Clinic", "dq_keys": ["SiteCode", "PKEY"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 1,
    },
    {
        "Method": "Save3pSetup",
        "TaskName": "P1 Reference 3P Setup Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_3p_setup",
        "TargetSchema": "ctrl",
        "TargetTable": "tbl_3PSETUP",
        "TargetPath": "bhg_silver.ctrl.tbl_3PSETUP",
        "WatermarkColumn": "SiteCode,pID",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.ctrl.tbl_3PSETUP", "dq_keys": ["SiteCode", "pID"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 2,
    },
    {
        "Method": "SaveCodes",
        "TaskName": "P1 Reference Codes Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_codes",
        "TargetSchema": "pats",
        "TargetTable": "tbl_Codes",
        "TargetPath": "bhg_silver.pats.tbl_Codes",
        "WatermarkColumn": "SiteCode,cdeID",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.pats.tbl_Codes", "dq_keys": ["SiteCode", "cdeID"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 3,
    },
    {
        "Method": "SaveServices",
        "TaskName": "P1 Reference Services Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_services",
        "TargetSchema": "pats",
        "TargetTable": "tbl_SERVICES",
        "TargetPath": "bhg_silver.pats.tbl_SERVICES",
        "WatermarkColumn": "SiteCode,sID",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.pats.tbl_SERVICES", "dq_keys": ["SiteCode", "sID"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 4,
    },
    {
        "Method": "SavedropDownListItems",
        "TaskName": "P1 Reference Dropdown List Items Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_dropdown_list_items",
        "TargetSchema": "ctrl",
        "TargetTable": "tbl_DroDownListItems",
        "TargetPath": "bhg_silver.ctrl.tbl_DroDownListItems",
        "WatermarkColumn": "SiteCode,Id",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.ctrl.tbl_DroDownListItems", "dq_keys": ["SiteCode", "Id"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 5,
    },
    {
        "Method": "SaveCustomAnswers",
        "TaskName": "P1 Reference Custom Answers Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_custom_answers",
        "TargetSchema": "pats",
        "TargetTable": "tbl_CustomAnswers",
        "TargetPath": "bhg_silver.pats.tbl_CustomAnswers",
        "WatermarkColumn": "SiteCode,caID,caQID,caCLTID",
        "RequestBody": json.dumps(
            {
                "full_table": "bhg_silver.pats.tbl_CustomAnswers",
                "dq_keys": ["SiteCode", "caID", "caQID", "caCLTID"],
            },
            separators=(",", ":"),
        ),
        "ExecutionOrder": 6,
    },
    {
        "Method": "SaveCustomQuestions",
        "TaskName": "P1 Reference Custom Questions Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_custom_questions",
        "TargetSchema": "pats",
        "TargetTable": "tbl_CustomQuestions",
        "TargetPath": "bhg_silver.pats.tbl_CustomQuestions",
        "WatermarkColumn": "SiteCode,cID",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.pats.tbl_CustomQuestions", "dq_keys": ["SiteCode", "cID"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 7,
    },
    {
        "Method": "SavePreAdmissionV6",
        "TaskName": "P1 Reference PreAdmission V6 Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_pre_admission_v6",
        "TargetSchema": "ayx",
        "TargetTable": "tbl_PreAdmission_V6",
        "TargetPath": "bhg_silver.ayx.tbl_PreAdmission_V6",
        "WatermarkColumn": "SiteCode,PreAdmissionid,Clientid",
        "RequestBody": json.dumps(
            {
                "full_table": "bhg_silver.ayx.tbl_PreAdmission_V6",
                "dq_keys": ["SiteCode", "PreAdmissionid", "Clientid"],
            },
            separators=(",", ":"),
        ),
        "ExecutionOrder": 8,
    },
    {
        "Method": "SavePreAdminReferrals",
        "TaskName": "P1 Reference Preadmission Referral Source Silver",
        "SourceTable": "bhg_bronze.P1Reference.br_samms_preadmission_referral_source",
        "TargetSchema": "pats",
        "TargetTable": "tbl_PreadmissionReferralSource",
        "TargetPath": "bhg_silver.pats.tbl_PreadmissionReferralSource",
        "WatermarkColumn": "SiteCode,Id",
        "RequestBody": json.dumps(
            {"full_table": "bhg_silver.pats.tbl_PreadmissionReferralSource", "dq_keys": ["SiteCode", "Id"]},
            separators=(",", ":"),
        ),
        "ExecutionOrder": 9,
    },
]


schema = StructType(
    [
        StructField("Method", StringType(), False),
        StructField("TaskName", StringType(), False),
        StructField("SourceTable", StringType(), False),
        StructField("TargetSchema", StringType(), False),
        StructField("TargetTable", StringType(), False),
        StructField("TargetPath", StringType(), False),
        StructField("WatermarkColumn", StringType(), False),
        StructField("RequestBody", StringType(), False),
        StructField("ExecutionOrder", IntegerType(), False),
    ]
)

updates_df = spark.createDataFrame(silver_rows, schema)
methods = [row["Method"] for row in silver_rows]

existing_df = (
    spark.table(taskconfig_table)
    .where((F.col("ConfigId") == F.lit(silver_config_id)) & F.col("Method").isin(methods))
    .select("TaskConfigId", "ConfigId", "Method", "TargetSchema", "TargetTable", "TargetPath", "RequestBody")
)

existing_count = existing_df.count()
if existing_count != len(silver_rows):
    display(existing_df.orderBy("Method"))
    raise Exception(
        f"Expected {len(silver_rows)} existing P1 Reference Silver task rows for ConfigId={silver_config_id}, "
        f"found {existing_count}. Check TaskConfig before updating."
    )

target_columns = set(spark.table(taskconfig_table).columns)
candidate_updates = {
    "TaskName": "source.TaskName",
    "AuthType": "'Lakehouse'",
    "SourceTable": "source.SourceTable",
    "LoadType": "'MERGE'",
    "WatermarkColumn": "source.WatermarkColumn",
    "TargetSchema": "source.TargetSchema",
    "TargetTable": "source.TargetTable",
    "TargetPath": "source.TargetPath",
    "RequestBody": "source.RequestBody",
    "ExecutionOrder": "source.ExecutionOrder",
    "SiteCode": "NULL",
    "DataBaseName": "NULL",
    "SiteName": "NULL",
    "IsActive": "1",
    "ModifiedBy": f"'{modified_by}'",
}

update_set = {col_name: expr for col_name, expr in candidate_updates.items() if col_name in target_columns}

DeltaTable.forName(spark, taskconfig_table).alias("target").merge(
    updates_df.alias("source"),
    f"target.ConfigId = {silver_config_id} AND target.Method = source.Method",
).whenMatchedUpdate(set=update_set).execute()

updated_df = (
    spark.table(taskconfig_table)
    .where((F.col("ConfigId") == F.lit(silver_config_id)) & F.col("Method").isin(methods))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        "AuthType",
        "SourceTable",
        "LoadType",
        "WatermarkColumn",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "RequestBody",
        "ExecutionOrder",
        "IsActive",
    )
    .orderBy("ExecutionOrder", "TaskConfigId")
)

display(updated_df)

print(f"Updated {len(silver_rows)} P1 Reference Silver TaskConfig rows for ConfigId={silver_config_id}.")
