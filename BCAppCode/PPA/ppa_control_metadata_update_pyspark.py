from delta.tables import DeltaTable
from pyspark.sql import functions as F

etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"

bronze_request_body = """{
  "full_table": "bhg_bronze.PatientPreAdmission.br_SF_PatientPreAdmission",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["SiteCode", "ID"]
}"""

silver_request_body = """{
  "full_table": "bhg_silver.pats.tbl_SF_PatientPreAdmission",
  "dq_keys": ["SiteCode", "ID"]
}"""

# PPA now audits/runs BR and SL only. Silver is the final controlled table.
spark.sql(f"""
UPDATE {etlconfig_table}
SET
    IsActive = CASE WHEN ConfigId = 78 THEN 0 ELSE IsActive END,
    ModifiedAt = current_timestamp(),
    ModifiedBy = 'Harsha'
WHERE ConfigId IN (76, 77, 78)
""")

# Add BR DQ keys and normalize Bronze metadata.
spark.sql(f"""
UPDATE {taskconfig_table}
SET
    TargetSchema = 'PatientPreAdmission',
    TargetTable = 'br_SF_PatientPreAdmission',
    TargetPath = 'bhg_bronze.PatientPreAdmission.br_SF_PatientPreAdmission',
    RequestBody = '{bronze_request_body}',
    ModifiedAt = current_timestamp(),
    ModifiedBy = 'Harsha'
WHERE ConfigId = 76
  AND Method = 'SFPatientPreAdmission'
""")

# Silver final table uses the BHG_DR destination name, without sl_ prefix.
spark.sql(f"""
UPDATE {taskconfig_table}
SET
    SourceTable = 'bhg_bronze.PatientPreAdmission.br_SF_PatientPreAdmission',
    LoadType = 'MERGE',
    IsIncremental = 1,
    TargetSchema = 'pats',
    TargetTable = 'tbl_SF_PatientPreAdmission',
    TargetPath = 'bhg_silver.pats.tbl_SF_PatientPreAdmission',
    RequestBody = '{silver_request_body}',
    IsActive = 1,
    ModifiedAt = current_timestamp(),
    ModifiedBy = 'Harsha'
WHERE ConfigId = 77
  AND Method = 'SFPatientPreAdmission'
""")

# Keep the Gold metadata row but inactive. The parent pipeline no longer runs Gold.
spark.sql(f"""
UPDATE {taskconfig_table}
SET
    IsActive = 0,
    ModifiedAt = current_timestamp(),
    ModifiedBy = 'Harsha'
WHERE ConfigId = 78
  AND Method = 'SFPatientPreAdmission'
""")

display(
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(76, 77, 78))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        "SourceTable",
        "LoadType",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "IsActive",
        "RequestBody",
        "ModifiedAt",
        "ModifiedBy"
    )
    .orderBy("ConfigId", "TaskConfigId")
)
