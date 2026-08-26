# P1 Finance module ETLConfig setup
# Run this in a Fabric Spark notebook attached to the Bronze lakehouse.
# ConfigId 46 = Bronze, 47 = Silver, 48 = Gold

from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, LongType, StringType, StructField, StructType
from delta.tables import DeltaTable


etlconfig_table = "bhg_bronze.meta.etlconfig"

# Naming standard: DEV_<first two letters of developer name>, uppercase.
created_by = "DEV_HA"
finance_config_ids = (46, 47, 48)
environment_name = "DEV"
trigger_type = "SCHEDULE"
trigger_frequency = "DAILY"
triggered_by = "Fabric"
config_prefix = "SAMMS P1 Finance"


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
        46,
        f"{config_prefix} Bronze Pipeline",
        "pl_p1_finance",
        "/pipelines/pl_p1_finance",
        None,
        "SAMMS",
        "DATA BASE",
        "BR",
        environment_name,
        None,
        1,
        trigger_type,
        trigger_frequency,
        triggered_by,
        1,
        "{}",
        created_by,
        created_by,
    ),
    (
        47,
        f"{config_prefix} Silver Pipeline",
        "nb_p1_finance_bronze_to_silver",
        "/notebooks/nb_p1_finance_bronze_to_silver",
        "/notebooks/nb_p1_finance_bronze_to_silver",
        "SAMMS",
        "TABLE",
        "SL",
        environment_name,
        "P1_FINANCE_BRONZE",
        2,
        trigger_type,
        trigger_frequency,
        triggered_by,
        1,
        "{}",
        created_by,
        created_by,
    ),
    (
        48,
        f"{config_prefix} Gold Pipeline",
        "Execute_P1_Finance",
        "/pipelines/Execute_P1_Finance",
        "Versioned Gold Publish",
        "SAMMS",
        "TABLE",
        "GL",
        environment_name,
        "P1_FINANCE_SILVER",
        3,
        trigger_type,
        trigger_frequency,
        triggered_by,
        1,
        "{}",
        created_by,
        created_by,
    ),
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

# Merge preserves CreatedBy on matched rows; normalize audit columns for finance configs.
finance_config_filter = ", ".join(str(config_id) for config_id in finance_config_ids)
spark.sql(f"""
UPDATE {etlconfig_table}
SET CreatedBy = '{created_by}', ModifiedBy = '{created_by}'
WHERE ConfigId IN ({finance_config_filter})
""")

spark.sql("""
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
    TriggerType,
    TriggerFrequency,
    TriggeredBy,
    IsActive,
    ConnectionConfig,
    CreatedAt,
    CreatedBy,
    ModifiedAt,
    ModifiedBy
FROM bhg_bronze.meta.etlconfig
WHERE ConfigId IN (46, 47, 48)
ORDER BY ConfigId
""").show(truncate=False)
