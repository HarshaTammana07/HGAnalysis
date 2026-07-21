# Notes taskconfig setup
# Run this in a Fabric PySpark notebook attached to the Bronze lakehouse.
#
# This follows the same JSON-driven pattern used in:
#   BCAppCode/Framework/etlconfigandtaskconfigsqls
#
# Configs already created:
#   34 = SAMMS Notes Bronze Pipeline
#   35 = SAMMS Notes Silver Pipeline
#   36 = SAMMS Notes Gold Pipeline (retired/disabled for Notes; Silver is now final)
#
# Fixed template/task rows:
#   143 = 3pArnote Bronze template        -> deactivated after site rows are generated
#   144 = 3pClaimNote Bronze template     -> deactivated after site rows are generated
#   145 = 3pArnote Silver
#   146 = 3pClaimNote Silver
#   147 = 3pArnote Gold        -> retained only as inactive historical config
#   148 = 3pClaimNote Gold     -> retained only as inactive historical config
#
# Generated Bronze site rows:
#   143000 + row_number = 3pArnote Bronze - <SiteCode>
#   144000 + row_number = 3pClaimNote Bronze - <SiteCode>

import json
from pyspark.sql import functions as F
from pyspark.sql.types import *
from pyspark.sql.window import Window
from delta.tables import DeltaTable

taskconfig_table = "bhg_bronze.meta.taskconfig"

try:
    created_by
except NameError:
    created_by = "Harsha"

try:
    notes_lookback_days
except NameError:
    notes_lookback_days = 15

arnote_bronze_request_body = json.dumps({
    "full_table": "bhg_bronze.Notes.br_tbl3pArnote",
    "ingest_column": "IngestRunId",
    "site_column": "SiteCode",
    "database_column": "SourceDatabase",
    "dq_keys": ["SiteCode", "arnID"]
})

claimnote_bronze_request_body = json.dumps({
    "full_table": "bhg_bronze.Notes.br_tbl3pClaimNote",
    "ingest_column": "IngestRunId",
    "site_column": "SiteCode",
    "database_column": "SourceDatabase",
    "dq_keys": ["SiteCode", "tpcnTPCID"]
})

arnote_silver_request_body = json.dumps({
    "full_table": "bhg_silver.pats.tbl_3pARNOTE",
    "dq_keys": ["SiteCode", "arnID"]
})

claimnote_silver_request_body = json.dumps({
    "full_table": "bhg_silver.pats.tbl_3pClaimNote",
    "dq_keys": ["SiteCode", "tpcnTPCID"]
})

# Paste/update content from BCAppCode/Framework/AllsiteCodesAndDatabses.txt here.
notes_sites_json = r'''
[
  {"site_code": "AHK", "source_database": "SAMMS-Ahoskie"},
  {"site_code": "B12B", "source_database": "SAMMS-ColoradoSpringsV5"},
  {"site_code": "B24", "source_database": "SAMMS-PaintsvilleV5"},
  {"site_code": "B25", "source_database": "SAMMS-PikevilleV5"},
  {"site_code": "B26", "source_database": "SAMMS-HazardV5"},
  {"site_code": "B27", "source_database": "SAMMS-SavannahV6"},
  {"site_code": "B28", "source_database": "SAMMS-WestPlainesV5"},
  {"site_code": "B29", "source_database": "SAMMS-PoplarBluffV5"},
  {"site_code": "B30", "source_database": "SAMMS-KCNv5"},
  {"site_code": "B31", "source_database": "SAMMS-DyersburgV5"},
  {"site_code": "B33", "source_database": "SAMMS-Paducah"},
  {"site_code": "B34", "source_database": "SAMMS-CorbinV5"},
  {"site_code": "B35", "source_database": "SAMMS-LexingtonV5"},
  {"site_code": "B35A", "source_database": "SAMMS-BereaV5"},
  {"site_code": "B36", "source_database": "SAMMS-AshevilleV5"},
  {"site_code": "B37", "source_database": "SAMMS-ClydeV5"},
  {"site_code": "B38", "source_database": "SAMMS-SpartanburgV5"},
  {"site_code": "B39", "source_database": "SAMMS-AikenV5"},
  {"site_code": "B41", "source_database": "SAMMS-ChesapeakeV5"},
  {"site_code": "B42", "source_database": "SAMMS-VirginiaBeachV5"},
  {"site_code": "B42A", "source_database": "SAMMS-NewportNewsV5"},
  {"site_code": "B42B", "source_database": "SAMMS-FranklinV5"},
  {"site_code": "B42C", "source_database": "SAMMS-GlenAllenV5"},
  {"site_code": "B42D", "source_database": "SAMMS-ChesapeakeSouthV5"},
  {"site_code": "B44", "source_database": "SAMMS-AlbanyV5"},
  {"site_code": "B45", "source_database": "SAMMS-TiftonV5"},
  {"site_code": "B46", "source_database": "SAMMS-WashingtonDCv5"},
  {"site_code": "B47", "source_database": "SAMMS-MobileV5"},
  {"site_code": "B48", "source_database": "SAMMS-TuscaloosaV5"},
  {"site_code": "B51", "source_database": "SAMMS-NorthLittleRockV6"},
  {"site_code": "B52", "source_database": "SAMMS-JacksonGAV5"},
  {"site_code": "B54", "source_database": "SAMMS-GadsdenV5"},
  {"site_code": "B55", "source_database": "SAMMS-ShoalsV5"},
  {"site_code": "B57", "source_database": "SAMMS-Pawtucket"},
  {"site_code": "B57A", "source_database": "SAMMS-Johnston"},
  {"site_code": "B57B", "source_database": "SAMMS-Middletown"},
  {"site_code": "B57C", "source_database": "SAMMS-Providence"},
  {"site_code": "B57D", "source_database": "SAMMS-Westerly"},
  {"site_code": "B66A", "source_database": "SAMMS-Bremen"},
  {"site_code": "B72", "source_database": "SAMMS-Mobile-OBOT"},
  {"site_code": "B73", "source_database": "SAMMS-Montgomery"},
  {"site_code": "B75", "source_database": "SAMMS-LawrenceV6"},
  {"site_code": "B76", "source_database": "SAMMS-Huntsville-OBOT"},
  {"site_code": "BAT", "source_database": "SAMMS-Batesville"},
  {"site_code": "BG", "source_database": "SAMMS-BowlingGreen"},
  {"site_code": "BOI", "source_database": "SAMMS-Boise"},
  {"site_code": "CBCO", "source_database": "SAMMS-CoeurdAleneV6"},
  {"site_code": "CON", "source_database": "SAMMS-Conway"},
  {"site_code": "D07", "source_database": "SAMMS-KnoxvilleV6"},
  {"site_code": "D08", "source_database": "SAMMS-MadisonV6"},
  {"site_code": "D09", "source_database": "SAMMS-MurfreesboroV6"},
  {"site_code": "DA", "source_database": "SAMMS-Davenport"},
  {"site_code": "DM", "source_database": "SAMMS-DesMoines"},
  {"site_code": "DRD-CO", "source_database": "SAMMS-ColumbiaV5"},
  {"site_code": "DRD-KC", "source_database": "SAMMS-KCv5"},
  {"site_code": "DRD-KVB", "source_database": "SAMMS-KVBv5"},
  {"site_code": "DRD-KVC", "source_database": "SAMMS-KVCv5"},
  {"site_code": "DRD-NOLA", "source_database": "SAMMS-NOLAv5"},
  {"site_code": "DRD-SF", "source_database": "SAMMS-SFv5"},
  {"site_code": "ELC", "source_database": "SAMMS-ElizabethCity"},
  {"site_code": "ET", "source_database": "SAMMS-Elizabethtown"},
  {"site_code": "FAY", "source_database": "SAMMS-Fayetteville"},
  {"site_code": "FR", "source_database": "SAMMS-Frankfort"},
  {"site_code": "FS", "source_database": "SAMMS-FortSmith"},
  {"site_code": "FW", "source_database": "SAMMS-FortWayne"},
  {"site_code": "GAL", "source_database": "SAMMS-Gaylord"},
  {"site_code": "HGT", "source_database": "SAMMS-Hagerstown"},
  {"site_code": "HNT", "source_database": "SAMMS-Huntsville"},
  {"site_code": "HS", "source_database": "SAMMS-HotSprings"},
  {"site_code": "JON", "source_database": "SAMMS-Jonesboro"},
  {"site_code": "LAN", "source_database": "SAMMS-Lansing"},
  {"site_code": "LO", "source_database": "SAMMS-Louisville"},
  {"site_code": "LV1", "source_database": "SAMMS-Cheyenne"},
  {"site_code": "LV2", "source_database": "SAMMS-DesertInn"},
  {"site_code": "LV3", "source_database": "SAMMS-McDaniel"},
  {"site_code": "MNRE", "source_database": "SAMMS-Monroe"},
  {"site_code": "MP", "source_database": "SAMMS-MtPleasant"},
  {"site_code": "MRD", "source_database": "SAMMS-Meridian"},
  {"site_code": "NC", "source_database": "SAMMS-NorthCharleston"},
  {"site_code": "NLR", "source_database": "SAMMS-NLROBOT"},
  {"site_code": "PH", "source_database": "SAMMS-Phoenix"},
  {"site_code": "RE", "source_database": "SAMMS-Reno"},
  {"site_code": "RMD", "source_database": "SAMMS-Richmond"},
  {"site_code": "SFN", "source_database": "SAMMS-SFNv5"},
  {"site_code": "SHP", "source_database": "SAMMS-Shreveport"},
  {"site_code": "STN", "source_database": "SAMMS-Staunton"},
  {"site_code": "STVN", "source_database": "SAMMS-Stevenson"},
  {"site_code": "TE", "source_database": "SAMMS-Tempe"},
  {"site_code": "TEX", "source_database": "SAMMS-Texarkana"},
  {"site_code": "TTCA", "source_database": "SAMMS-BessemerV5"},
  {"site_code": "TTCB", "source_database": "SAMMS-CullmanV5"},
  {"site_code": "TTCC", "source_database": "SAMMS-GrandBay"},
  {"site_code": "TU", "source_database": "SAMMS-Tucson"},
  {"site_code": "V1", "source_database": "SAMMS-VCPHCS-I-MemphisV5"},
  {"site_code": "V10", "source_database": "SAMMS-BoulderV5"},
  {"site_code": "V10A", "source_database": "SAMMS-FortCollinsV5"},
  {"site_code": "V11", "source_database": "SAMMS-VCPHCS-XI-NorthDenverV5"},
  {"site_code": "V12", "source_database": "SAMMS-VCPHCS-XII-DowntownDenverV5"},
  {"site_code": "V12A", "source_database": "SAMMS-CentennialV5"},
  {"site_code": "V14", "source_database": "SAMMS-VCPHCS-XIV-BridgewayV5"},
  {"site_code": "V15", "source_database": "SAMMS-JoplinV5"},
  {"site_code": "V17", "source_database": "SAMMS-ColumbiaTNv5"},
  {"site_code": "V19", "source_database": "SAMMS-JacksonV5"},
  {"site_code": "V20", "source_database": "SAMMS-ParisV5"},
  {"site_code": "V21", "source_database": "SAMMS-RaleighV5"},
  {"site_code": "V5", "source_database": "SAMMS-NONTCv5"},
  {"site_code": "V5B", "source_database": "SAMMS-HoumaV6"},
  {"site_code": "V6", "source_database": "SAMMS-LCv5"},
  {"site_code": "V8", "source_database": "SAMMS-VCPHCS-VIII-MemphisV5"},
  {"site_code": "V9", "source_database": "SAMMS-NashvilleV5"},
  {"site_code": "VBRA", "source_database": "SAMMS-BrainerdV6"},
  {"site_code": "VBRP", "source_database": "SAMMS-BrooklynParkV6"},
  {"site_code": "VMIN", "source_database": "SAMMS-MinneapolisV6"},
  {"site_code": "VWBY", "source_database": "SAMMS-WoodburyV6"},
  {"site_code": "WIL", "source_database": "SAMMS-Wilson"}
]
'''

# -------------------------------------------------------------------------
# 1. Seed the six Notes template/layer tasks.
# -------------------------------------------------------------------------
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
    StructField("ModifiedBy", StringType(), True)
])

task_rows = [
    (
        143, 34, "3pArnote Bronze", None, "3pArnote", "SQLServer", "tbl3pARNOTE",
        0, None, "INCREMENTAL", 1, "arnDATE,arnDtRemoved", notes_lookback_days,
        "Notes", "br_tbl3pArnote", "/lakehouse/bhg_bronze/Notes/br_tbl3pArnote",
        1, 0, 43200, arnote_bronze_request_body, None, None, None, None, 1, created_by, created_by
    ),
    (
        144, 34, "3pClaimNote Bronze", None, "3pClaimNote", "SQLServer", "tbl3pClaimNote",
        0, None, "INCREMENTAL", 1, "tpcnDtmAdded,tpcnDtTickler", notes_lookback_days,
        "Notes", "br_tbl3pClaimNote", "/lakehouse/bhg_bronze/Notes/br_tbl3pClaimNote",
        2, 0, 43200, claimnote_bronze_request_body, None, None, None, None, 1, created_by, created_by
    ),
    (
        145, 35, "3pArnote Silver", None, "3pArnote", "Lakehouse", "bhg_bronze.Notes.br_tbl3pArnote",
        0, None, "MERGE", 1, "IngestRunId", notes_lookback_days,
        "pats", "tbl_3pARNOTE", "/lakehouse/bhg_silver/pats/tbl_3pARNOTE",
        1, 0, 43200, arnote_silver_request_body, None, None, None, None, 1, created_by, created_by
    ),
    (
        146, 35, "3pClaimNote Silver", None, "3pClaimNote", "Lakehouse", "bhg_bronze.Notes.br_tbl3pClaimNote",
        0, None, "MERGE", 1, "IngestRunId", notes_lookback_days,
        "pats", "tbl_3pClaimNote", "/lakehouse/bhg_silver/pats/tbl_3pClaimNote",
        2, 0, 43200, claimnote_silver_request_body, None, None, None, None, 1, created_by, created_by
    ),
    (
        147, 36, "3pArnote Gold", None, "3pArnote", "Warehouse", "bhg_silver.pats.tbl_3pARNOTE",
        0, None, "VERSIONED_FULL_OVERWRITE", 0, None, None,
        "pats", "gd_3p_arnote", "bhg_gold.pats.gd_3p_arnote",
        1, 0, 43200, None, None, None, None, None, 0, created_by, created_by
    ),
    (
        148, 36, "3pClaimNote Gold", None, "3pClaimNote", "Warehouse", "bhg_silver.pats.tbl_3pClaimNote",
        0, None, "VERSIONED_FULL_OVERWRITE", 0, None, None,
        "pats", "gd_3p_claim_note", "bhg_gold.pats.gd_3p_claim_note",
        2, 0, 43200, None, None, None, None, None, 0, created_by, created_by
    )
]

task_df = (
    spark.createDataFrame(task_rows, task_schema)
    .withColumn("CreatedAt", F.current_timestamp())
    .withColumn("ModifiedAt", F.current_timestamp())
)

task_cols_for_merge = [f.name for f in task_df.schema.fields]
task_update = {c: f"s.{c}" for c in task_cols_for_merge if c not in ["TaskConfigId", "CreatedAt", "CreatedBy"]}
task_insert = {c: f"s.{c}" for c in task_cols_for_merge}

DeltaTable.forName(spark, taskconfig_table).alias("t") \
    .merge(task_df.alias("s"), "t.TaskConfigId = s.TaskConfigId") \
    .whenMatchedUpdate(set=task_update) \
    .whenNotMatchedInsert(values=task_insert) \
    .execute()

# -------------------------------------------------------------------------
# 2. Generate per-site Bronze taskconfig rows from JSON.
# -------------------------------------------------------------------------
task_cols = spark.table(taskconfig_table).columns

notes_sites = json.loads(notes_sites_json)

notes_sites_df = (
    spark.createDataFrame(
        [(x["site_code"], x["source_database"]) for x in notes_sites],
        ["SiteCode", "DataBaseName"]
    )
    .withColumn(
        "SiteName",
        F.trim(
            F.regexp_replace(
                F.regexp_replace(F.col("DataBaseName"), "^SAMMS-", ""),
                "(?i)V[0-9]+$",
                ""
            )
        )
    )
    .withColumn("rn", F.row_number().over(Window.orderBy("SiteCode")))
)

def build_notes_bronze_tasks(template_task_config_id, generated_id_base, task_name_prefix, execution_order):
    template_df = (
        spark.table(taskconfig_table)
        .where(f"TaskConfigId = {template_task_config_id} AND ConfigId = 34")
        .limit(1)
    )

    if template_df.count() != 1:
        raise Exception(f"Template Bronze taskconfig row TaskConfigId={template_task_config_id}, ConfigId=34 was not found.")

    site_df = notes_sites_df.withColumn("TaskConfigId", F.lit(generated_id_base) + F.col("rn"))
    joined_df = site_df.alias("s").crossJoin(template_df.alias("t"))

    return joined_df.select([
        F.col("s.TaskConfigId").cast("long").alias("TaskConfigId") if c == "TaskConfigId" else
        F.lit(34).cast("long").alias("ConfigId") if c == "ConfigId" else
        F.concat(F.lit(task_name_prefix), F.col("s.SiteCode")).alias("TaskName") if c == "TaskName" else
        F.col("s.SiteCode").alias("SiteCode") if c == "SiteCode" else
        F.col("s.DataBaseName").alias("DataBaseName") if c == "DataBaseName" else
        F.col("s.SiteName").alias("SiteName") if c == "SiteName" else
        F.lit(None).cast("long").alias("DependencyTaskConfigId") if c == "DependencyTaskConfigId" else
        F.lit(execution_order).cast("int").alias("ExecutionOrder") if c == "ExecutionOrder" else
        F.lit(1).cast("int").alias("IsActive") if c == "IsActive" else
        F.current_timestamp().alias("CreatedAt") if c == "CreatedAt" else
        F.current_timestamp().alias("ModifiedAt") if c == "ModifiedAt" else
        F.lit(created_by).alias("CreatedBy") if c == "CreatedBy" else
        F.lit(created_by).alias("ModifiedBy") if c == "ModifiedBy" else
        F.col(f"t.{c}").alias(c)
        for c in task_cols
    ])

arnote_bronze_task_df = build_notes_bronze_tasks(
    template_task_config_id=143,
    generated_id_base=143000,
    task_name_prefix="3pArnote Bronze - ",
    execution_order=1
)

claimnote_bronze_task_df = build_notes_bronze_tasks(
    template_task_config_id=144,
    generated_id_base=144000,
    task_name_prefix="3pClaimNote Bronze - ",
    execution_order=2
)

notes_bronze_task_df = arnote_bronze_task_df.unionByName(claimnote_bronze_task_df)

# Deactivate old generic Bronze template rows. Site-level rows are active instead.
DeltaTable.forName(spark, taskconfig_table).update(
    condition="TaskConfigId IN (143, 144) AND ConfigId = 34",
    set={
        "IsActive": F.lit(0),
        "ModifiedAt": F.current_timestamp(),
        "ModifiedBy": F.lit(created_by)
    }
)

# Keep Silver layer tasks independent for now. Gold rows are retained inactive only.
DeltaTable.forName(spark, taskconfig_table).update(
    condition="TaskConfigId IN (145, 146)",
    set={
        "DependencyTaskConfigId": F.lit(None).cast("long"),
        "ModifiedAt": F.current_timestamp(),
        "ModifiedBy": F.lit(created_by)
    }
)

DeltaTable.forName(spark, taskconfig_table).update(
    condition="TaskConfigId IN (147, 148)",
    set={
        "IsActive": F.lit(0),
        "DependencyTaskConfigId": F.lit(None).cast("long"),
        "ModifiedAt": F.current_timestamp(),
        "ModifiedBy": F.lit(created_by)
    }
)

# Remove existing generated Notes Bronze site rows for the same ConfigId + SiteCode,
# then insert clean. This keeps reruns idempotent.
generated_sites_df = notes_bronze_task_df.select("ConfigId", "SiteCode").distinct()

DeltaTable.forName(spark, taskconfig_table).alias("t") \
    .merge(
        generated_sites_df.alias("s"),
        "t.ConfigId = s.ConfigId AND t.SiteCode = s.SiteCode"
    ) \
    .whenMatchedDelete() \
    .execute()

notes_bronze_task_df.write.format("delta").mode("append").saveAsTable(taskconfig_table)

# -------------------------------------------------------------------------
# 3. Verification
# -------------------------------------------------------------------------
display(spark.sql("""
SELECT TaskConfigId, ConfigId, TaskName, Method, SourceTable,
       SiteCode, DataBaseName, SiteName,
       DependencyTaskConfigId, ExecutionOrder, IsActive,
       TargetSchema, TargetTable, LoadType, LookbackDays,
       CreatedAt, CreatedBy, ModifiedAt, ModifiedBy
FROM bhg_bronze.meta.taskconfig
WHERE ConfigId IN (34, 35, 36)
ORDER BY ConfigId, SiteCode, ExecutionOrder, TaskConfigId
"""))
