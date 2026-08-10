from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, StringType, StructField, StructType

taskconfig_table = "bhg_bronze.meta.taskconfig"
updated_by = "Harsha"

updates = [
    (
        18,
        "Orders",
        """{
          "full_table": "bhg_bronze.order.brz_Orders",
          "ingest_column": "PipelineRunId",
          "site_column": "SiteCode",
          "database_column": "DataBaseName",
          "dq_keys": ["SiteCode", "OrderNum", "cltID"]
        }"""
    ),
    (
        19,
        "Orders",
        """{
          "full_table": "bhg_silver.pats.slv_Orders",
          "ingest_column": "PipelineRunId",
          "site_column": "SiteCode",
          "database_column": "DataBaseName",
          "dq_keys": ["SiteCode", "OrderNum", "cltID"]
        }"""
    ),
    (
        20,
        "Orders",
        """{
          "full_table": "bhg_gold.pats.Orders",
          "ingest_column": "PipelineRunId",
          "site_column": "SiteCode",
          "database_column": "DataBaseName",
          "dq_keys": ["SiteCode", "OrderNum", "cltID"]
        }"""
    )
]

schema = StructType([
    StructField("ConfigId", IntegerType(), False),
    StructField("Method", StringType(), False),
    StructField("RequestBody", StringType(), False),
])

updates_df = spark.createDataFrame(updates, schema)

DeltaTable.forName(spark, taskconfig_table).alias("t").merge(
    updates_df.alias("s"),
    "t.ConfigId = s.ConfigId"
).whenMatchedUpdate(set={
    "Method": "s.Method",
    "RequestBody": "s.RequestBody",
    "ModifiedAt": "current_timestamp()",
    "ModifiedBy": f"'{updated_by}'"
}).execute()

display(
    spark.table(taskconfig_table)
    .where(F.col("ConfigId").isin(18, 19, 20))
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        "SourceTable",
        "TargetSchema",
        "TargetTable",
        "SiteCode",
        "DataBaseName",
        "IsActive",
        "RequestBody",
        "ModifiedAt",
        "ModifiedBy",
    )
    .orderBy("ConfigId", "TaskConfigId")
)
