1. Final target architecture 

Use this shape: 

 
 
This is the right replacement for the old Scheduler → TaskRunner → SQLSvrManager → SaveDartsSrv/BulkDartsSrv flow, because the old system tightly mixes orchestration, extraction, and persistence, while the Fabric target separates them cleanly. 

For on-premises access, Fabric Data Factory supports using an on-premises data gateway to connect local data sources to cloud destinations, and Microsoft explicitly documents that as the standard route for pipelines.  

 
2. What we are building 

You asked for the full end-to-end migration. Build these items. 

Workspace and environments 

Use 3 workspaces eventually: 

 
 
 
 
Core Fabric items 

Create these items: 

 
 
3. The most important design choices 

Bronze 

Bronze is incremental append-only raw landing: 

all relevant source columns  

RowChkSum  

_site_code  

_ingest_run_id  

_extracted_at  

Silver 

Silver is the current curated DART truth: 

one current row per (SiteCode, DsId)  

merge-based insert/update  

partitioned by service_year  

Gold 

Gold is reporting-friendly: 

current serving view/table  

daily aggregates  

optional year views if legacy downstream wants them  

Incremental strategy 

Do not require source CDC/Change Tracking as your first version. 

Use: 

initial historical load once  

later runs use watermark + overlap window  

merge on (SiteCode, DsId)  

update only when RowChkSum changes  

This is the most practical route across 80 sites. 

 

4. Build order from zero to production 

Phase A — Foundation 

Step A1 — Create the workspace 

If not already created: 

create dartserviceworkspace-dev  

Step A2 — Create the Lakehouse 

Create: 

lh_dartservice 

Lakehouse is the right central store here because Fabric Lakehouse uses Delta Lake as the default table format and Delta integrates best across Fabric services.  

Step A3 — Create notebooks 

Create: 

nb_dart_00_setup  

nb_dart_01_merge_silver  

nb_dart_02_build_gold  

nb_dart_03_optimize  

nb_dart_99_audit_helpers  

Pin lh_dartservice as the default lakehouse for each notebook. Fabric notebooks can be attached to lakehouses and use the default lakehouse for reads/writes.  

 

Phase B — Metadata and control tables 

Run this in nb_dart_00_setup. 

# nb_dart_00_setup 
spark.sql(""" 
CREATE TABLE IF NOT EXISTS meta_sites ( 
    site_code STRING, 
    site_name STRING, 
    is_active BOOLEAN, 
    server_name STRING, 
    database_name STRING, 
    auth_type STRING, 
    gateway_connection_name STRING, 
    secret_scope STRING, 
    secret_user_key STRING, 
    secret_password_key STRING, 
    timezone_code STRING, 
    lookback_days INT, 
    overlap_hours INT, 
    notes_enabled BOOLEAN, 
    service_type_enabled BOOLEAN, 
    created_at TIMESTAMP, 
    updated_at TIMESTAMP 
) USING DELTA 
""") 
 
spark.sql(""" 
CREATE TABLE IF NOT EXISTS meta_pipeline_run ( 
    run_id STRING, 
    pipeline_name STRING, 
    load_type STRING, 
    work_date DATE, 
    start_time TIMESTAMP, 
    end_time TIMESTAMP, 
    status STRING, 
    trigger_type STRING, 
    error_message STRING 
) USING DELTA 
""") 
 
spark.sql(""" 
CREATE TABLE IF NOT EXISTS meta_site_run ( 
    run_id STRING, 
    site_code STRING, 
    stage_name STRING, 
    start_time TIMESTAMP, 
    end_time TIMESTAMP, 
    status STRING, 
    rows_extracted BIGINT, 
    rows_inserted BIGINT, 
    rows_updated BIGINT, 
    rows_unchanged BIGINT, 
    rows_rejected BIGINT, 
    max_activity_ts TIMESTAMP, 
    error_message STRING 
) USING DELTA 
""") 
 
spark.sql(""" 
CREATE TABLE IF NOT EXISTS meta_watermark ( 
    site_code STRING, 
    table_name STRING, 
    last_successful_watermark TIMESTAMP, 
    last_run_id STRING, 
    updated_at TIMESTAMP 
) USING DELTA 
""") 

Insert seed rows for test sites: 

spark.sql(""" 
INSERT INTO meta_sites VALUES 
('B01','Clinic B01',true,'server1','SAMMS_B01','sql','gw_b01',NULL,NULL,NULL,'EST',15,24,true,true,current_timestamp(),current_timestamp()), 
('B02','Clinic B02',true,'server2','SAMMS_B02','sql','gw_b02',NULL,NULL,NULL,'EST',15,24,true,true,current_timestamp(),current_timestamp()) 
""") 

 

Phase C — Bronze table 

Still in nb_dart_00_setup, create Bronze. 

spark.sql(""" 

CREATE TABLE IF NOT EXISTS bronze_dartssrv_raw ( 

    DsId BIGINT, 

    DsClt BIGINT, 

    DsTxtSrv STRING, 

    DsDtStart TIMESTAMP, 

    DsDtEnd TIMESTAMP, 

    DsDtAdded TIMESTAMP, 

    DsUpdate TIMESTAMP, 

    DsBilled TIMESTAMP, 

    DsSigDate TIMESTAMP, 

    DsProgram STRING, 

    DsGroupnum STRING, 

    DsError STRING, 

    DsTxtType STRING, 

    DsdblUnits DOUBLE, 

    DsNoteId BIGINT, 

    DstxtStaff STRING, 

    DsUpdatestaff STRING, 

    DsInvalidatedOn TIMESTAMP, 

    DsTxtHiv STRING, 

    DsDartsGroup BIGINT, 

    RepOldSrv DECIMAL(18,2), 

    DsSigUser STRING, 

    DsSigUserCosign STRING, 

    DsSigcltdate TIMESTAMP, 

    DsAptid BIGINT, 

    Dsuncharted BOOLEAN, 

    DsTxDim1 BIGINT, 

    DsTxDim2 BIGINT, 

    DsTxDim3 BIGINT, 

    DsTxDim4 BIGINT, 

    DsTxDim5 BIGINT, 

    DsTxDim6 BIGINT, 

    DsDiag STRING, 

    DsArea STRING, 

    DsGroupDefaultNote BOOLEAN, 

    DsGroupEnd TIMESTAMP, 

    DsGroupIdentity BIGINT, 

    DsGroupStart TIMESTAMP, 

    DsDiag10 STRING, 

    SiteId BIGINT, 

    DsDbnotes STRING, 

    Mg DOUBLE, 

    RowChkSum BIGINT, 

    _site_code STRING, 

    _ingest_run_id STRING, 

    _extracted_at TIMESTAMP, 

    _source_query_start_ts TIMESTAMP, 

    _source_query_end_ts TIMESTAMP 

) USING DELTA 

PARTITIONED BY (_site_code) 

""")                
 
This keeps Bronze raw and audit-friendly.                  
 
Phase D — Silver table 

Create Silver in nb_dart_00_setup. 

spark.sql(""" 

CREATE TABLE IF NOT EXISTS silver_dartssrv ( 

    SiteCode STRING, 

    DsId BIGINT, 

    DsClt BIGINT, 

    DsTxtSrv STRING, 

    DsDtStart TIMESTAMP, 

    DsDtEnd TIMESTAMP, 

    DsDtAdded TIMESTAMP, 

    DsUpdate TIMESTAMP, 

    DsBilled TIMESTAMP, 

    DsSigDate TIMESTAMP, 

    DsProgram STRING, 

    DsGroupnum STRING, 

    DsError STRING, 

    DsTxtType STRING, 

    DsdblUnits DOUBLE, 

    DsNoteId BIGINT, 

    DstxtStaff STRING, 

    DsUpdatestaff STRING, 

    DsInvalidatedOn TIMESTAMP, 

    DsTxtHiv STRING, 

    DsDartsGroup BIGINT, 

    RepOldSrv DECIMAL(18,2), 

    DsSigUser STRING, 

    DsSigUserCosign STRING, 

    DsSigcltdate TIMESTAMP, 

    DsAptid BIGINT, 

    Dsuncharted BOOLEAN, 

    DsTxDim1 BIGINT, 

    DsTxDim2 BIGINT, 

    DsTxDim3 BIGINT, 

    DsTxDim4 BIGINT, 

    DsTxDim5 BIGINT, 

    DsTxDim6 BIGINT, 

    DsDiag STRING, 

    DsArea STRING, 

    DsGroupDefaultNote BOOLEAN, 

    DsGroupEnd TIMESTAMP, 

    DsGroupIdentity BIGINT, 

    DsGroupStart TIMESTAMP, 

    DsDiag10 STRING, 

    SiteId BIGINT, 

    DsDbnotes STRING, 

    Mg DOUBLE, 

    RowChkSum BIGINT, 

    service_year INT, 

    first_ingest_run_id STRING, 

    last_ingest_run_id STRING, 

    last_merged_at TIMESTAMP 

) USING DELTA 

PARTITIONED BY (service_year) 

""") 
 

Why partition by service_year? Because the old DART flow is naturally year-oriented and the legacy destination is year-split. A unified Silver table partitioned by year gives you the performance benefit without keeping separate physical year tables as the main model. 
 
Phase E — Gold tables 

Create Gold. 

spark.sql(""" 

CREATE TABLE IF NOT EXISTS gold_dartssrv_current 

USING DELTA 

AS 

SELECT * FROM silver_dartssrv WHERE 1 = 0 

""") 

 

spark.sql(""" 

CREATE TABLE IF NOT EXISTS gold_dartssrv_daily ( 

    SiteCode STRING, 

    service_date DATE, 

    session_count BIGINT, 

    unique_clients BIGINT, 

    total_units DOUBLE, 

    load_run_id STRING, 

    built_at TIMESTAMP 

) USING DELTA 

""") 
 
 
5. How extraction works 

For production, I recommend this extraction design: 

Recommended production extraction 

Use Fabric Pipeline Copy activity with an on-premises data gateway to pull from each site’s SQL Server into Bronze. Microsoft’s current guidance for on-premises data access in Fabric Data Factory is to use the gateway pattern.  

Why I recommend this: 

more supported for on-prem connectivity  

easier ops  

easier connection management  

easier source-side authentication standardization  

What query should Copy activity run? 

Use a parameterized incremental query per site. 

Initial load query 

For first load: 

SELECT 

    dsID       AS DsId, 

    dsClt      AS DsClt, 

    dsTxtSrv   AS DsTxtSrv, 

    dsDtStart  AS DsDtStart, 

    dsDtEnd    AS DsDtEnd, 

    dsDtAdded  AS DsDtAdded, 

    dsUpdate   AS DsUpdate, 

    dsBilled   AS DsBilled, 

    dsSigDate  AS DsSigDate, 

    dsProgram  AS DsProgram, 

    dsGroupnum AS DsGroupnum, 

    dsError    AS DsError, 

    dsTxtType  AS DsTxtType, 

    dsdblUnits AS DsdblUnits, 

    dsNoteId   AS DsNoteId, 

    dstxtStaff AS DstxtStaff, 

    dsUpdatestaff AS DsUpdatestaff, 

    dsInvalidatedOn AS DsInvalidatedOn, 

    dsTxtHiv   AS DsTxtHiv, 

    dsDartsGroup AS DsDartsGroup, 

    repOldSrv  AS RepOldSrv, 

    dsSigUser  AS DsSigUser, 

    dsSigUserCosign AS DsSigUserCosign, 

    dsSigcltdate AS DsSigcltdate, 

    dsAptid    AS DsAptid, 

    dsuncharted AS Dsuncharted, 

    dsTxDim1, dsTxDim2, dsTxDim3, dsTxDim4, dsTxDim5, dsTxDim6, 

    dsDiag, dsArea, 

    dsGroupDefaultNote, 

    dsGroupEnd, 

    dsGroupIdentity, 

    dsGroupStart, 

    dsDiag10, 

    siteId AS SiteId, 

    dsDBnotes AS DsDbnotes, 

    mg AS Mg, 

    CHECKSUM( 

        dsID, dsClt, dsTxtSrv, dsDtStart, dsDtEnd, dsDtAdded, dsUpdate, 

        dsBilled, dsSigDate, dsProgram, dsGroupnum, dsError, dsTxtType, 

        dsdblUnits, dsNoteId, dstxtStaff, dsUpdatestaff, dsInvalidatedOn, 

        dsTxtHiv, dsDartsGroup, repOldSrv, dsSigUser, dsSigUserCosign, 

        dsSigcltdate, dsAptid, dsuncharted, dsTxDim1, dsTxDim2, dsTxDim3, 

        dsTxDim4, dsTxDim5, dsTxDim6, dsDiag, dsArea, dsGroupDefaultNote, 

        dsGroupEnd, dsGroupIdentity, dsGroupStart, dsDiag10, siteId, dsDBnotes, mg 

    ) AS RowChkSum 

FROM dbo.tblDartsSrv 

WHERE dsDtStart >= '@initial_start' 

  AND dsDtStart <  '@initial_end' 
 
 
Incremental query 

For later runs, use watermark + overlap: 

 
SELECT 

    dsID       AS DsId, 

    dsClt      AS DsClt, 

    dsTxtSrv   AS DsTxtSrv, 

    dsDtStart  AS DsDtStart, 

    dsDtEnd    AS DsDtEnd, 

    dsDtAdded  AS DsDtAdded, 

    dsUpdate   AS DsUpdate, 

    dsBilled   AS DsBilled, 

    dsSigDate  AS DsSigDate, 

    dsProgram  AS DsProgram, 

    dsGroupnum AS DsGroupnum, 

    dsError    AS DsError, 

    dsTxtType  AS DsTxtType, 

    dsdblUnits AS DsdblUnits, 

    dsNoteId   AS DsNoteId, 

    dstxtStaff AS DstxtStaff, 

    dsUpdatestaff AS DsUpdatestaff, 

    dsInvalidatedOn AS DsInvalidatedOn, 

    dsTxtHiv   AS DsTxtHiv, 

    dsDartsGroup AS DsDartsGroup, 

    repOldSrv  AS RepOldSrv, 

    dsSigUser  AS DsSigUser, 

    dsSigUserCosign AS DsSigUserCosign, 

    dsSigcltdate AS DsSigcltdate, 

    dsAptid    AS DsAptid, 

    dsuncharted AS Dsuncharted, 

    dsTxDim1, dsTxDim2, dsTxDim3, dsTxDim4, dsTxDim5, dsTxDim6, 

    dsDiag, dsArea, 

    dsGroupDefaultNote, 

    dsGroupEnd, 

    dsGroupIdentity, 

    dsGroupStart, 

    dsDiag10, 

    siteId AS SiteId, 

    dsDBnotes AS DsDbnotes, 

    mg AS Mg, 

    CHECKSUM( 

        dsID, dsClt, dsTxtSrv, dsDtStart, dsDtEnd, dsDtAdded, dsUpdate, 

        dsBilled, dsSigDate, dsProgram, dsGroupnum, dsError, dsTxtType, 

        dsdblUnits, dsNoteId, dstxtStaff, dsUpdatestaff, dsInvalidatedOn, 

        dsTxtHiv, dsDartsGroup, repOldSrv, dsSigUser, dsSigUserCosign, 

        dsSigcltdate, dsAptid, dsuncharted, dsTxDim1, dsTxDim2, dsTxDim3, 

        dsTxDim4, dsTxDim5, dsTxDim6, dsDiag, dsArea, dsGroupDefaultNote, 

        dsGroupEnd, dsGroupIdentity, dsGroupStart, dsDiag10, siteId, dsDBnotes, mg 

    ) AS RowChkSum 

FROM dbo.tblDartsSrv 

WHERE 

    dsUpdate   >= '@window_start' 

 OR dsDtAdded  >= '@window_start' 

 OR dsBilled   >= '@window_start' 

 OR dsSigDate  >= '@window_start' 

 OR dsDtStart  >= '@window_start' 

 OR dsDtEnd    >= '@window_start' 
 
 
This mirrors the legacy DART behavior much more closely than a naive single-column watermark, because the old DART logic used multi-date lookback behavior for extraction and was not true CDC-based. 

 
6. How the pipeline should be built 

Create this main pipeline: 

pl_dart_incremental 

Pipeline parameters 

Create parameters: 

p_run_id  

p_work_date  

p_load_type  

p_site_code_override nullable  

p_initial_start nullable  

p_initial_end nullable  

Pipeline flow 

Activity 1 — Set run_id 

Use pipeline expression or pass externally. 

Activity 2 — Insert row into meta_pipeline_run 

Use Script activity or Notebook activity. 

Activity 3 — Lookup active sites 

Query meta_sites: 

active only  

if override provided, just that site  

Activity 4 — ForEach site 

Use controlled concurrency, for example 8–12 to start. 

Inside each site branch: 

4.1 Get watermark 

Read meta_watermark for that site. 

4.2 Copy source → Bronze staging file/table 

Use gateway-backed Copy activity from the site SQL Server query into Lakehouse Bronze landing. 

4.3 Notebook: merge Bronze → Silver 

Call nb_dart_01_merge_silver 

4.4 Update site audit row 

Write counts/status to meta_site_run 

4.5 On failure branch 

Update meta_site_run as failed and raise pipeline alert path 

Activity 5 — Notebook: build Gold 

Call nb_dart_02_build_gold 

Activity 6 — Notebook: optimize Silver/Gold 

Call nb_dart_03_optimize 

Activity 7 — Mark pipeline success 

Update meta_pipeline_run 

This is the Fabric equivalent of old scheduler-driven parent/child site tasks, but cleaner and easier to monitor. Fabric Pipelines support scheduling and orchestration for these ETL scenarios.  

 
7. Silver merge notebook — full PySpark 
Here is the main notebook. 
# nb_dart_01_merge_silver 

 

from pyspark.sql import functions as F 

from delta.tables import DeltaTable 

import uuid 

from datetime import datetime, timedelta 

 

# ---------- PARAMETERS ---------- 

run_id = dbutils.widgets.get("run_id") if "dbutils" in globals() else None 

site_code = dbutils.widgets.get("site_code") if "dbutils" in globals() else None 

load_type = dbutils.widgets.get("load_type") if "dbutils" in globals() else "INCREMENTAL" 

 

if not run_id: 

    run_id = f"RUN_{datetime.utcnow().strftime('%Y%m%d%H%M%S')}_{uuid.uuid4().hex[:8]}" 

 

if not site_code: 

    raise ValueError("site_code parameter is required") 

 

stage_name = "MERGE_SILVER" 

 

# ---------- START SITE AUDIT ---------- 

spark.sql(f""" 

INSERT INTO meta_site_run 

SELECT 

    '{run_id}' as run_id, 

    '{site_code}' as site_code, 

    '{stage_name}' as stage_name, 

    current_timestamp() as start_time, 

    cast(null as timestamp) as end_time, 

    'RUNNING' as status, 

    cast(null as bigint) as rows_extracted, 

    cast(null as bigint) as rows_inserted, 

    cast(null as bigint) as rows_updated, 

    cast(null as bigint) as rows_unchanged, 

    cast(null as bigint) as rows_rejected, 

    cast(null as timestamp) as max_activity_ts, 

    cast(null as string) as error_message 

""") 

 

# ---------- LOAD BRONZE FOR CURRENT SITE/RUN ---------- 

bronze_df = ( 

    spark.table("bronze_dartssrv_raw") 

    .filter(F.col("_site_code") == site_code) 

    .filter(F.col("_ingest_run_id") == run_id) 

) 

 

bronze_count = bronze_df.count() 

 

if bronze_count == 0: 

    spark.sql(f""" 

    UPDATE meta_site_run 

    SET end_time = current_timestamp(), 

        status = 'SUCCESS', 

        rows_extracted = 0, 

        rows_inserted = 0, 

        rows_updated = 0, 

        rows_unchanged = 0, 

        rows_rejected = 0 

    WHERE run_id = '{run_id}' 

      AND site_code = '{site_code}' 

      AND stage_name = '{stage_name}' 

      AND status = 'RUNNING' 

    """) 

    raise SystemExit("No rows found for this site/run") 

 

# ---------- STANDARDIZE ---------- 

curated_df = ( 

    bronze_df 

    .withColumn("SiteCode", F.col("_site_code")) 

    .withColumn("service_year", F.year("DsDtStart")) 

    .withColumn("first_ingest_run_id", F.col("_ingest_run_id")) 

    .withColumn("last_ingest_run_id", F.col("_ingest_run_id")) 

    .withColumn("last_merged_at", F.current_timestamp()) 

    .select( 

        "SiteCode","DsId","DsClt","DsTxtSrv","DsDtStart","DsDtEnd","DsDtAdded","DsUpdate", 

        "DsBilled","DsSigDate","DsProgram","DsGroupnum","DsError","DsTxtType","DsdblUnits", 

        "DsNoteId","DstxtStaff","DsUpdatestaff","DsInvalidatedOn","DsTxtHiv","DsDartsGroup", 

        "RepOldSrv","DsSigUser","DsSigUserCosign","DsSigcltdate","DsAptid","Dsuncharted", 

        "DsTxDim1","DsTxDim2","DsTxDim3","DsTxDim4","DsTxDim5","DsTxDim6","DsDiag","DsArea", 

        "DsGroupDefaultNote","DsGroupEnd","DsGroupIdentity","DsGroupStart","DsDiag10","SiteId", 

        "DsDbnotes","Mg","RowChkSum","service_year","first_ingest_run_id","last_ingest_run_id", 

        "last_merged_at" 

    ) 

) 

 

# ---------- DEDUP WITHIN RUN ---------- 

# Keep the latest row by DsUpdate, then _extracted_at if multiple rows for same key 

window_cols = ["SiteCode", "DsId"] 

dedup_df = ( 

    curated_df 

    .withColumn("_rn", F.row_number().over( 

        Window.partitionBy(*window_cols).orderBy(F.col("DsUpdate").desc_nulls_last(), F.col("last_merged_at").desc()) 

    )) 

    .filter(F.col("_rn") == 1) 

    .drop("_rn") 

) 

 

dedup_df.createOrReplaceTempView("src_dart_batch") 

 

# ---------- PRE-MERGE COUNTS ---------- 

silver_exists = spark.catalog.tableExists("silver_dartssrv") 

if not silver_exists: 

    raise ValueError("silver_dartssrv table does not exist") 

 

silver_dt = DeltaTable.forName(spark, "silver_dartssrv") 

 

# Changed rows estimate 

silver_df = spark.table("silver_dartssrv").filter(F.col("SiteCode") == site_code) 

 

match_df = ( 

    dedup_df.alias("s") 

    .join(silver_df.alias("t"), 

          on=[F.col("s.SiteCode")==F.col("t.SiteCode"), F.col("s.DsId")==F.col("t.DsId")], 

          how="left") 

) 

 

insert_count = match_df.filter(F.col("t.DsId").isNull()).count() 

update_count = match_df.filter((F.col("t.DsId").isNotNull()) & (F.col("s.RowChkSum") != F.col("t.RowChkSum"))).count() 

unchanged_count = match_df.filter((F.col("t.DsId").isNotNull()) & (F.col("s.RowChkSum") == F.col("t.RowChkSum"))).count() 

 

# ---------- MERGE ---------- 

( 

    silver_dt.alias("t") 

    .merge( 

        dedup_df.alias("s"), 

        "t.SiteCode = s.SiteCode AND t.DsId = s.DsId" 

    ) 

    .whenMatchedUpdate( 

        condition="t.RowChkSum <> s.RowChkSum", 

        set={ 

            "DsClt": "s.DsClt", 

            "DsTxtSrv": "s.DsTxtSrv", 

            "DsDtStart": "s.DsDtStart", 

            "DsDtEnd": "s.DsDtEnd", 

            "DsDtAdded": "s.DsDtAdded", 

            "DsUpdate": "s.DsUpdate", 

            "DsBilled": "s.DsBilled", 

            "DsSigDate": "s.DsSigDate", 

            "DsProgram": "s.DsProgram", 

            "DsGroupnum": "s.DsGroupnum", 

            "DsError": "s.DsError", 

            "DsTxtType": "s.DsTxtType", 

            "DsdblUnits": "s.DsdblUnits", 

            "DsNoteId": "s.DsNoteId", 

            "DstxtStaff": "s.DstxtStaff", 

            "DsUpdatestaff": "s.DsUpdatestaff", 

            "DsInvalidatedOn": "s.DsInvalidatedOn", 

            "DsTxtHiv": "s.DsTxtHiv", 

            "DsDartsGroup": "s.DsDartsGroup", 

            "RepOldSrv": "s.RepOldSrv", 

            "DsSigUser": "s.DsSigUser", 

            "DsSigUserCosign": "s.DsSigUserCosign", 

            "DsSigcltdate": "s.DsSigcltdate", 

            "DsAptid": "s.DsAptid", 

            "Dsuncharted": "s.Dsuncharted", 

            "DsTxDim1": "s.DsTxDim1", 

            "DsTxDim2": "s.DsTxDim2", 

            "DsTxDim3": "s.DsTxDim3", 

            "DsTxDim4": "s.DsTxDim4", 

            "DsTxDim5": "s.DsTxDim5", 

            "DsTxDim6": "s.DsTxDim6", 

            "DsDiag": "s.DsDiag", 

            "DsArea": "s.DsArea", 

            "DsGroupDefaultNote": "s.DsGroupDefaultNote", 

            "DsGroupEnd": "s.DsGroupEnd", 

            "DsGroupIdentity": "s.DsGroupIdentity", 

            "DsGroupStart": "s.DsGroupStart", 

            "DsDiag10": "s.DsDiag10", 

            "SiteId": "s.SiteId", 

            "DsDbnotes": "s.DsDbnotes", 

            "Mg": "s.Mg", 

            "RowChkSum": "s.RowChkSum", 

            "service_year": "s.service_year", 

            "last_ingest_run_id": "s.last_ingest_run_id", 

            "last_merged_at": "s.last_merged_at" 

        } 

    ) 

    .whenNotMatchedInsert(values={ 

        "SiteCode": "s.SiteCode", 

        "DsId": "s.DsId", 

        "DsClt": "s.DsClt", 

        "DsTxtSrv": "s.DsTxtSrv", 

        "DsDtStart": "s.DsDtStart", 

        "DsDtEnd": "s.DsDtEnd", 

        "DsDtAdded": "s.DsDtAdded", 

        "DsUpdate": "s.DsUpdate", 

        "DsBilled": "s.DsBilled", 

        "DsSigDate": "s.DsSigDate", 

        "DsProgram": "s.DsProgram", 

        "DsGroupnum": "s.DsGroupnum", 

        "DsError": "s.DsError", 

        "DsTxtType": "s.DsTxtType", 

        "DsdblUnits": "s.DsdblUnits", 

        "DsNoteId": "s.DsNoteId", 

        "DstxtStaff": "s.DstxtStaff", 

        "DsUpdatestaff": "s.DsUpdatestaff", 

        "DsInvalidatedOn": "s.DsInvalidatedOn", 

        "DsTxtHiv": "s.DsTxtHiv", 

        "DsDartsGroup": "s.DsDartsGroup", 

        "RepOldSrv": "s.RepOldSrv", 

        "DsSigUser": "s.DsSigUser", 

        "DsSigUserCosign": "s.DsSigUserCosign", 

        "DsSigcltdate": "s.DsSigcltdate", 

        "DsAptid": "s.DsAptid", 

        "Dsuncharted": "s.Dsuncharted", 

        "DsTxDim1": "s.DsTxDim1", 

        "DsTxDim2": "s.DsTxDim2", 

        "DsTxDim3": "s.DsTxDim3", 

        "DsTxDim4": "s.DsTxDim4", 

        "DsTxDim5": "s.DsTxDim5", 

        "DsTxDim6": "s.DsTxDim6", 

        "DsDiag": "s.DsDiag", 

        "DsArea": "s.DsArea", 

        "DsGroupDefaultNote": "s.DsGroupDefaultNote", 

        "DsGroupEnd": "s.DsGroupEnd", 

        "DsGroupIdentity": "s.DsGroupIdentity", 

        "DsGroupStart": "s.DsGroupStart", 

        "DsDiag10": "s.DsDiag10", 

        "SiteId": "s.SiteId", 

        "DsDbnotes": "s.DsDbnotes", 

        "Mg": "s.Mg", 

        "RowChkSum": "s.RowChkSum", 

        "service_year": "s.service_year", 

        "first_ingest_run_id": "s.first_ingest_run_id", 

        "last_ingest_run_id": "s.last_ingest_run_id", 

        "last_merged_at": "s.last_merged_at" 

    }) 

    .execute() 

) 

 

# ---------- UPDATE WATERMARK ---------- 

max_activity_ts = bronze_df.select( 

    F.greatest("DsUpdate","DsDtAdded","DsBilled","DsSigDate","DsDtStart","DsDtEnd").alias("mx") 

).agg(F.max("mx").alias("max_ts")).collect()[0]["max_ts"] 

 

if max_activity_ts is not None: 

    spark.sql(f""" 

    MERGE INTO meta_watermark t 

    USING ( 

        SELECT 

            '{site_code}' as site_code, 

            'tblDartsSrv' as table_name, 

            TIMESTAMP('{max_activity_ts}') as last_successful_watermark, 

            '{run_id}' as last_run_id, 

            current_timestamp() as updated_at 

    ) s 

    ON t.site_code = s.site_code AND t.table_name = s.table_name 

    WHEN MATCHED THEN UPDATE SET 

        t.last_successful_watermark = s.last_successful_watermark, 

        t.last_run_id = s.last_run_id, 

        t.updated_at = s.updated_at 

    WHEN NOT MATCHED THEN INSERT * 

    """) 

 

# ---------- COMPLETE SITE AUDIT ---------- 

spark.sql(f""" 

UPDATE meta_site_run 

SET end_time = current_timestamp(), 

    status = 'SUCCESS', 

    rows_extracted = {bronze_count}, 

    rows_inserted = {insert_count}, 

    rows_updated = {update_count}, 

    rows_unchanged = {unchanged_count}, 

    rows_rejected = 0, 

    max_activity_ts = {f"TIMESTAMP('{max_activity_ts}')" if max_activity_ts else "NULL"} 

WHERE run_id = '{run_id}' 

  AND site_code = '{site_code}' 

  AND stage_name = '{stage_name}' 

  AND status = 'RUNNING' 

""") 
 
Add this import at the top if needed: 
from pyspark.sql.window import Window 
 
8. Gold build notebook 
 
# nb_dart_02_build_gold 

 

from pyspark.sql import functions as F 

 

run_id = dbutils.widgets.get("run_id") if "dbutils" in globals() else f"RUN_{datetime.utcnow().strftime('%Y%m%d%H%M%S')}" 

 

silver_df = spark.table("silver_dartssrv") 

 

# Gold current 

silver_df.write.format("delta").mode("overwrite").option("overwriteSchema","true").saveAsTable("gold_dartssrv_current") 

 

# Gold daily aggregate 

daily_df = ( 

    silver_df 

    .withColumn("service_date", F.to_date("DsDtStart")) 

    .groupBy("SiteCode", "service_date") 

    .agg( 

        F.count("*").alias("session_count"), 

        F.countDistinct("DsClt").alias("unique_clients"), 

        F.sum("DsdblUnits").alias("total_units") 

    ) 

    .withColumn("load_run_id", F.lit(run_id)) 

    .withColumn("built_at", F.current_timestamp()) 

) 

 

daily_df.write.format("delta").mode("overwrite").option("overwriteSchema","true").saveAsTable("gold_dartssrv_daily") 
 
9. Optimization notebook 

Fabric’s current Delta guidance says V-Order is disabled by default in new workspaces to favor write-heavy data engineering workloads, and table maintenance should be done with OPTIMIZE, optionally with VORDER/ZORDER when appropriate.  

For this pipeline, keep write performance first. 

# nb_dart_03_optimize 

 

spark.conf.set("spark.sql.parquet.vorder.default", "false") 

 

spark.sql(""" 

OPTIMIZE silver_dartssrv WHERE service_year >= year(current_date()) - 2 

""") 

 

spark.sql(""" 

OPTIMIZE gold_dartssrv_current 

""") 

 

spark.sql(""" 

OPTIMIZE gold_dartssrv_daily 

""") 
 
 
For read-heavy Gold later, you can consider VORDER/ZORDER on targeted tables.  
 
10. Initial load vs incremental load 

Initial load pipeline 

Create: 

pl_dart_initial_load 

Use it for: 

one site first  

one date range first  

then widen  

Recommended rollout: 

one site, one year  

one site, multiple years  

five sites  

all sites  

Do not start with all 80 sites at full history on day one. 

Incremental pipeline 

Create: 

pl_dart_incremental 

Daily steps: 

get active sites  

get watermark  

compute window_start = watermark - overlap  

copy query to Bronze  

merge to Silver  

update watermark  

build Gold  

optimize  

 

11. Scheduling 

Fabric Pipelines support scheduled and event-based execution. For your DART daily pipeline, use a schedule trigger.  

Recommended schedule: 

initial version: once daily after clinic systems settle  

later: hourly or every few hours if business wants fresher DART  

Important operational note: Fabric notebooks and schedules run under the identity of the user who created or updated them, so make sure the right service owner creates/updates the notebook schedules and pipeline wiring.  

Recommended cadence: 

pl_dart_incremental: daily at 1 AM local control time  

pl_dart_retry_failed: 2 AM and 4 AM or manual  

pl_dart_initial_load: manual only  

 

12. Alerts and failure handling 

Fabric now has multiple alerting patterns: 

pipeline run alerts  

Monitoring hub failure visibility  

Real-Time hub / Activator-based event alerts that can send email, Teams, run a pipeline, or kick off Power Automate.  

What I recommend 

Level 1 — Pipeline alerts 

Configure pipeline-run failure alerts for: 

pipeline failed  

critical activity failed  

Level 2 — Real-Time hub / event alert 

Create a Fabric workspace item event alert: 

condition: DART pipeline failed  

action: send email  

action: send Teams  

optional: run retry pipeline or Power Automate flow  

Fabric’s current event alerts explicitly support email, Teams, running Fabric items, and Power Automate workflows.  

Level 3 — Metadata-driven alerts 

Use meta_site_run to trigger abnormal-row alerts: 

site extracted 0 rows unexpectedly  

inserted/updated count drops below threshold  

repeated site failures  

That can be implemented with: 

a small audit notebook  

then a Power Automate call or event alert  

 

13. Monitoring and operations 

Use three things together: 

Monitoring hub 

Monitoring hub shows current and historical Fabric activity, run status, diagnostics, and failure investigation, and also exposes schedule failures.  

Pipeline run history 

Use for activity-level pipeline failures. 

Lakehouse audit tables 

Use: 

meta_pipeline_run  

meta_site_run  

meta_watermark  

These are your strongest ops layer because they let you answer: 

which site failed  

how many rows changed  

what was the watermark  

what was the last successful run  

This is exactly the kind of observability the old system lacked. 

 

14. Security and secrets 

Do not hardcode source credentials in notebooks. 

Fabric notebooks support secret access using NotebookUtils / credentials utilities with Azure Key Vault. Microsoft’s current docs explicitly recommend using notebook utilities to work with secrets.  

If you need notebook-side secrets, pattern it like this: 

from notebookutils import mssparkutils  # or notebookutils, depending on runtime import path 
 
# example placeholder 
# user = notebookutils.credentials.getSecret("https://<your-kv>.vault.azure.net/", "sql-user") 
# password = notebookutils.credentials.getSecret("https://<your-kv>.vault.azure.net/", "sql-password") 

For on-prem pipeline Copy activity, prefer managed Fabric connections via gateway where possible. 

 

15. CI/CD and promotion 

When you move beyond proof of concept: 

connect notebooks and pipelines to Git  

use deployment pipelines for dev → test → prod promotion  

Fabric’s current docs support notebooks with Git/deployment pipelines and provide deployment pipelines as the production lifecycle mechanism.  

 

16. What replaces what from the old system 

Here is the migration map. 

OLD SYSTEM                                  NEW FABRIC SYSTEM 
--------------------------------------------------------------------------- 
Scheduler.exe                               Fabric Pipeline schedule trigger 
tsk.tbl_Tasks2 parent/child task rows       meta_pipeline_run + meta_site_run 
ctrl.tbl_LocationCons                       meta_sites + Fabric connections/gateway 
SQLSvrManager.GetTableData()                Pipeline Copy activity from source query 
SelectConstructor                           parameterized SQL query templates 
SaveDartsSrvs row-by-row EF logic           Delta MERGE in nb_dart_01_merge_silver 
BulkDartsSrv staging merge                  Bronze->Silver merge notebook 
Azure SQL year-split tables                 unified silver_dartssrv partitioned by year 
manual task monitoring                      Monitoring hub + alerts + meta_* audit tables 

The old system’s DART schedule separation, bulk path, and year-based behavior all still matter conceptually, but they become simpler in Fabric. 

 

17. Exact first implementation order I want you to follow 

Do it in this exact order. 

Sprint 1 

Create workspace/lakehouse  

Create metadata tables  

Create Bronze/Silver/Gold empty tables  

Create initial-load pipeline for one site and one year  

Run first load  

Run Silver merge  

Validate counts against source  

Sprint 2 

Add watermark logic  

Create incremental pipeline  

Add audit updates  

Add pipeline failure alert  

Add site retry pipeline  

Sprint 3 

Expand to 5 sites  

Tune concurrency  

Add Gold daily metrics  

Add Monitoring hub operational process  

Add deployment pipeline  

Sprint 4 

Roll to all sites  

Add advanced row-count anomaly alerts  

Add optimize/maintenance notebook  

Cut over downstream consumers  

 

18. What you should not do 

Do not: 

recreate 80 separate Bronze tables  

recreate 80 separate Silver tables  

recreate year-specific tables as your primary design  

do full reloads every day  

update every matched row in Silver if checksum is available  

hardcode credentials in notebooks  

let one site failure kill the entire day without retry support  

 

19. Final recommendation 

For BHG DART, the production-ready Fabric implementation should be: 

Lakehouse-centered  

Bronze raw incremental append  

Silver unified Delta table partitioned by service_year  

Gold reporting tables/views  

Fabric Pipeline orchestration with schedule trigger  

on-premises gateway-backed source extraction  

notebook-driven Delta MERGE using (SiteCode, DsId) and RowChkSum  

metadata tables for runs, sites, and watermarks  

Monitoring hub + pipeline alerts + Real-Time hub/Power Automate notifications  

deployment pipelines for promotion  

That gives you the Fabric-native replacement for the old DART pipeline while fixing the biggest legacy problems: long runtime, weak operational visibility, manual recovery, and tightly coupled C# code paths. 

                                                                                                                                                                           