# P1 Forms module ETLConfig setup
# Run this in a Fabric Spark notebook attached to the Bronze lakehouse.
# ConfigId 97 = Bronze, 98 = Silver

from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, LongType, StringType, StructField, StructType
from delta.tables import DeltaTable


etlconfig_table = "bhg_bronze.meta.etlconfig"

created_by = "Harsha"
environment_name = "DEV"
trigger_type = "SCHEDULE"
trigger_frequency = "DAILY"
triggered_by = "Fabric"
config_prefix = "SAMMS P1 Forms"


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
        97,
        f"{config_prefix} Bronze Pipeline",
        "pl_execute_forms",
        "/pipelines/pl_execute_forms",
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
        98,
        f"{config_prefix} Silver Pipeline",
        "pl_execute_forms",
        "/pipelines/pl_execute_forms",
        "/notebooks/nb_p1_forms_bronze_to_silver",
        "SAMMS",
        "TABLE",
        "SL",
        environment_name,
        "P1_FORMS_BRONZE",
        2,
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
WHERE ConfigId IN (97, 98)
ORDER BY ConfigId
""").show(truncate=False)
