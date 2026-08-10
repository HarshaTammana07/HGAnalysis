from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"

bronze_request_body = """{
  "full_table": "bhg_bronze.Dart.br_tblDartSrv",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["SiteCode", "dsID"]
}"""

silver_request_body = """{
  "full_table": "bhg_silver.pats.tbl_dartsrv",
  "source_full_table": "bhg_bronze.Dart.br_tblDartSrv",
  "dq_keys": ["SiteCode", "dsID"]
}"""

gold_request_body = """{
  "full_table": "tbl_dartsrv",
  "source_full_table": "bhg_silver.pats.tbl_dartsrv",
  "dq_keys": ["SiteCode", "dsID"]
}"""

target = DeltaTable.forName(spark, taskconfig_table)

print("Rows before update:")
display(
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(25, 26, 27))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        "SourceTable",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "RequestBody",
        "IsActive"
    )
    .orderBy("ConfigId", "TaskConfigId")
)

# Bronze: keep the existing Bronze table name, but update metadata to match the
# PascalCase Bronze columns and add dq_keys for Bronze data quality.
target.update(
    condition="ConfigId = 25",
    set={
        "TargetSchema": F.lit("Dart"),
        "TargetTable": F.lit("br_tblDartSrv"),
        "TargetPath": F.lit("/lakehouse/bhg_bronze/Tables/Dart/br_tblDartSrv"),
        "SourceTable": F.lit("dbo.tblDartsSrv"),
        "WatermarkColumn": F.lit("dsDtStart,dsDtAdded,dsUpdate,dsBilled,dsSigDate,dsClt<=0"),
        "RequestBody": F.lit(bronze_request_body),
        "ModifiedAt": F.current_timestamp(),
        "ModifiedBy": F.lit("Harsha")
    }
)

# Silver: final Silver table is now pats.tbl_dartsrv, no sl_ prefix.
target.update(
    condition="ConfigId = 26",
    set={
        "TaskName": F.lit("DartsSrv Silver"),
        "SourceTable": F.lit("bhg_bronze.Dart.br_tblDartSrv"),
        "LoadType": F.lit("MERGE"),
        "IsIncremental": F.lit(1),
        "WatermarkColumn": F.lit("IngestRunId"),
        "TargetSchema": F.lit("pats"),
        "TargetTable": F.lit("tbl_dartsrv"),
        "TargetPath": F.lit("/lakehouse/bhg_silver/Tables/pats/tbl_dartsrv"),
        "ExecutionOrder": F.lit(2),
        "RequestBody": F.lit(silver_request_body),
        "ModifiedAt": F.current_timestamp(),
        "ModifiedBy": F.lit("Harsha")
    }
)

# Gold: metadata stays available independently, but table name also becomes
# pats.tbl_dartsrv, no gd_ prefix. This does not activate/deactivate Gold.
target.update(
    condition="ConfigId = 27",
    set={
        "TaskName": F.lit("DartsSrv Gold"),
        "SourceTable": F.lit("bhg_silver.pats.tbl_dartsrv"),
        "TargetSchema": F.lit("pats"),
        "TargetTable": F.lit("tbl_dartsrv"),
        "TargetPath": F.lit("/warehouse/bhg_gold/pats/tbl_dartsrv"),
        "ExecutionOrder": F.lit(3),
        "RequestBody": F.lit(gold_request_body),
        "ModifiedAt": F.current_timestamp(),
        "ModifiedBy": F.lit("Harsha")
    }
)

print("Rows after update:")
display(
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(25, 26, 27))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        "SourceTable",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "WatermarkColumn",
        "LookbackDays",
        "IsActive",
        "RequestBody",
        "ModifiedAt",
        "ModifiedBy"
    )
    .orderBy("ConfigId", "TaskConfigId")
)
