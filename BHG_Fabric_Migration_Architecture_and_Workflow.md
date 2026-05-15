# BHG ETL → Microsoft Fabric Migration — Complete Architecture & Workflow
**Migration Target:** Microsoft Fabric (OneLake + Medallion Architecture)  
**Source System:** BCAppCode — BHG Recovery Data Pipeline (BHGTaskRunner / BHG-DR-LIB)  
**Architecture:** Bronze → Silver → Gold (Delta Lake, no Power BI reports)  
**Date:** 2026-04-22

---

## PART 1 — What Are We Migrating and Why?

### The Current System (What Exists Today)
BHG Recovery runs **80+ addiction treatment clinics** across the USA. Every clinic has its own
local SAMMS SQL Server database. A nightly C# ETL system (`BHGTaskRunner.exe`) extracts data
from all clinics and loads it into one central Azure SQL database (`BHG_DR`).

### The Problem with the Current System

```
CURRENT BOTTLENECKS:
═══════════════════
1. Sequential execution  → 80+ sites processed ONE AT A TIME per pipeline
                           (80 sites × 3 min avg = 4 hours per schedule run)

2. O(N²) memory problem  → EF Core loads ENTIRE Azure site slice into C# RAM,
                           then does LINQ.Where inside foreach = N × N comparisons
                           (50,000 rows = 2.5 billion comparisons)

3. Shared staging tables → stg.tbl_dartssrv used by ALL sites, forces serialisation
                           (cannot parallelise without data corruption)

4. C# exe deployment     → every table change requires recompile + copy .exe to server
                           (no git, no CI/CD, manual file copy)

5. No lineage/audit      → no data quality layer, no Bronze history,
                           raw data not preserved, only final Azure state survives

6. Year-partitioned hack → DartsSrv/Orders split into 10–14 separate physical tables
                           to manage Azure SQL query performance
                           (pats.tbl_DartsSrv_2019, _2020, _2021 … all separate tables)
```

### What Microsoft Fabric Gives Us

```
FABRIC SOLUTIONS:
═════════════════
1. True parallelism      → ForEach activity: 80 sites process SIMULTANEOUSLY
                           (80 parallel notebooks = ~60 min → ~12 min)

2. Delta MERGE engine    → SET-based MERGE in Spark, no in-memory C# list loading
                           (handles 200M+ rows efficiently)

3. No shared staging     → Delta partitioned by SiteCode — concurrent writes are safe
                           (each site writes to its own partition)

4. Notebook pipelines    → PySpark notebooks in Fabric, version-controlled in git,
                           parameterised, no .exe deployment needed

5. Full data lineage     → Bronze = raw history forever preserved
                           Silver = cleansed, merged, deduplicated
                           Gold   = aggregations, KPIs, analytics-ready

6. Single Delta tables   → DartsSrv 2014–2028 in ONE partitioned Delta table
                           partition columns: SiteCode, Year
                           (no more tbl_DartsSrv_2019, _2020, _2021 split)
```

---

## PART 2 — The Target Architecture (All Fabric Components)

### 2.1 — The 3 OneLake Layers (Medallion)

```
OneLake Storage
│
├── Bronze Layer   (raw, append-only, never modified)
│   Path: onelake/<workspace>/bronze.Lakehouse/Tables/
│   What goes here: every row extracted from every SAMMS source, with extraction timestamp
│   Format: Delta Lake (Parquet under the hood)
│   Who writes here: Extraction notebooks only
│   Who reads here: Silver transformation notebooks
│
├── Silver Layer   (cleaned, merged, deduplicated — one row per business key)
│   Path: onelake/<workspace>/silver.Lakehouse/Tables/
│   What goes here: MERGE result — current state of all tables across all sites
│   Format: Delta Lake, partitioned by SiteCode (and Year for large tables)
│   Who writes here: Transformation (MERGE) notebooks
│   Who reads here: Gold aggregation notebooks, downstream consumers
│
└── Gold Layer     (aggregations, KPIs, analytics-ready — replaces AzureAgent)
    Path: onelake/<workspace>/gold.Lakehouse/Tables/
    What goes here: Computed tables (BAM buckets, counselor KPIs, treatment plans)
    Format: Delta Lake (or Fabric Warehouse tables for SQL access)
    Who writes here: Aggregation notebooks / Fabric Data Pipelines
    Who reads here: Applications, APIs, reporting tools
```

### 2.2 — The Fabric Components (Replacing Each C# Executable)

```
CURRENT SYSTEM                          FABRIC EQUIVALENT
══════════════════════════════════      ══════════════════════════════════════════
Scheduler.exe (5:15 PM)              →  Fabric Data Pipeline: pl_scheduler
                                         Activity: Lookup active sites
                                         Activity: ForEach — create pipeline runs
                                         Trigger: Daily scheduled at 5:15 PM

BHGTaskRunner.exe 1–11               →  Fabric Data Pipelines (one per schedule):
                                         pl_global         (arg=1  SAMMSGlobal)
                                         pl_regional_p1    (arg=2  Regional P1)
                                         pl_regional_p2    (arg=4  Regional P2)
                                         pl_lab            (arg=5  Samms-LAB)
                                         pl_forms          (arg=6  Samms-Forms)
                                         pl_notes          (arg=7  SAMMS-ETL-Notes)
                                         pl_inventory      (arg=8  SAMMS-ETL-INV)
                                         pl_dartssrv       (arg=9  SAMMS-ETL-DartSvc)
                                         pl_dose           (arg=10 SAMMS-ETL-Dose)
                                         pl_orders         (arg=11 SAMMS-ETL-Orders)

AzureAgent.exe (6:24/6:45/7:00 AM)  →  Fabric Data Pipeline: pl_post_processing
                                         Notebooks for each aggregation job
                                         Trigger: Daily at 6:24 AM, 6:45 AM, 7:00 AM

PHC.exe                              →  Same pipelines, PHC_Enabled parameter flag
                                         PHC sites included as parameter in ForEach

ETLMgr.exe (monitoring desktop)     →  Fabric Pipeline Run History UI
                                         + Gold table: meta.tbl_pipeline_run_log
                                         + Optional: simple monitoring notebook
```

### 2.3 — The Fabric Notebooks (Replacing BHG-DR-LIB)

```
Fabric Notebooks (PySpark):
│
├── nb_extract_base.py           ← replaces SQLSvrManager.GetTableData()
│   Purpose: JDBC connect to SAMMS source, SELECT with date filter, return DataFrame
│   Parameters: site_code, conn_str, source_table, where_clause, work_date, lookback_days
│
├── nb_merge_base.py             ← replaces EF Core SaveData upsert pattern
│   Purpose: Delta MERGE into Silver on (SiteCode, PrimaryKey) with RowChkSum check
│   Parameters: site_code, source_df, silver_table_name, key_cols, chksum_enabled
│
├── nb_bulk_merge_base.py        ← replaces BulkDartsSvc + stg.SP MERGE pattern
│   Purpose: Append to Bronze, then Delta MERGE into Silver (set-based, no staging)
│   Parameters: site_code, source_df, silver_table_name, key_cols
│
├── nb_checksum.py               ← replaces SelectConstructor RowChkSum logic
│   Purpose: Compute xxhash64 or SHA checksum across all payload columns per row
│   Parameters: df, exclude_cols (timestamp cols not included in chksum)
│
├── nb_metadata.py               ← replaces dms.tbl_MapSrc2Dsn lookup
│   Purpose: Load column mappings and table configs from meta.tbl_map_config
│   Parameters: action_key, action_step_key
│
├── nb_rowtrax.py                ← replaces SaveRowTrax()
│   Purpose: Log source count vs silver count per site per table per run
│   Parameters: site_code, run_date, table_name, source_count, silver_count
│
└── Per-table notebooks (one per destination table):
    nb_dartssrv.py, nb_dose.py, nb_orders.py, nb_claims.py ...
    Each calls nb_extract_base + nb_merge_base with table-specific params
```

### 2.4 — The Metadata / Control Tables (Replacing Azure BHG_DR control schema)

```
CURRENT CONTROL TABLES (Azure BHG_DR)     FABRIC EQUIVALENT (meta.Lakehouse)
══════════════════════════════════════     ══════════════════════════════════════
ctrl.tbl_Locations                      →  meta.dim_sites
ctrl.tbl_LocationCons                   →  meta.dim_site_connections
                                            (conn strings stored in Key Vault)
dms.tbl_MapAction                       →  meta.tbl_map_action
dms.tbl_MapSrc2Dsn                      →  meta.tbl_map_columns
tsk.tbl_Schedule                        →  Fabric Pipeline schedules + trigger config
tsk.tbl_Tasks2                          →  meta.tbl_pipeline_run_log (Gold)
tsk.tbl_ErrorLog                        →  meta.tbl_error_log (Gold)
ctrl.tbl_Forms2Process                  →  meta.tbl_forms_config
stg.* (staging tables)                  →  NOT NEEDED — Delta replaces staging
```

---

## PART 3 — The 11 Fabric Pipelines (What Gets Loaded)

Each pipeline maps directly to one `BHGTaskRunner.exe` argument.

```
FABRIC PIPELINES:
═════════════════════════════════════════════════════════════════════════
Pipeline Name         | Original Arg | Tables Loaded
──────────────────────┼──────────────┼────────────────────────────────────
pl_global             |  arg=1       | GlobalUser, GlobalPayer, Consents,
                      |              | FormsSAMMSClient, BAM, Services,
                      |              | Devices, ClinicalOpiateWithdrawalScale
──────────────────────┼──────────────┼────────────────────────────────────
pl_regional_p1        |  arg=2       | Enrollment, ClientDemo1/2, UAResults,
                      |              | UAResultDetail, Codes, Clinic,
                      |              | Consents, Users, CustomQuestions,
                      |              | CustomAnswers, PreAdmission
──────────────────────┼──────────────┼────────────────────────────────────
pl_regional_p2        |  arg=4       | Claims, ClaimLineItem,
                      |              | ClaimLineItemActivity, Bills, CheckIn,
                      |              | GlobalPayor, FeeSchedules, 3pElig,
                      |              | EandMForms, PayerClient, AuthBills
──────────────────────┼──────────────┼────────────────────────────────────
pl_lab                |  arg=5       | LabResult, UAResult, UAResultDetail,
                      |              | LABResultDetail, UASched
──────────────────────┼──────────────┼────────────────────────────────────
pl_forms              |  arg=6       | FormQuestionAnswers, AnswerSignatures,
                      |              | EandMFormMDM, EandMFormPregnancy,
                      |              | ComprehensiveAssessmentForm, FormQA
──────────────────────┼──────────────┼────────────────────────────────────
pl_notes              |  arg=7       | 3pArnote, 3pClaimNote
──────────────────────┼──────────────┼────────────────────────────────────
pl_inventory          |  arg=8       | Bottle, LiquidLog, InvTypes,
                      |              | OrientationChecklist, Appointments,
                      |              | AppointmentAttend, AdmissionAssessment
                      |              | (all 9 dimensions), ReAssessment
                      |              | (all 10 sub-tables), PA + PADimension1-6,
                      |              | PACounselorReview, COWS_V6, MNCA,
                      |              | VACA, VACASummary, BAMForm, BAMScore,
                      |              | FinancialHardshipApplication, FMP
──────────────────────┼──────────────┼────────────────────────────────────
pl_dartssrv           |  arg=9       | silver.dartssrv (single Delta table,
                      |              | replaces 10 year-partitioned tables)
──────────────────────┼──────────────┼────────────────────────────────────
pl_dose               |  arg=10      | silver.dose, silver.dose_excuse
──────────────────────┼──────────────┼────────────────────────────────────
pl_orders             |  arg=11      | silver.orders (single Delta table,
                      |              | replaces 14 year-partitioned tables)
──────────────────────┼──────────────┼────────────────────────────────────
pl_post_processing    |  AzureAgent  | gold.zero_dollar_denials,
                      |              | gold.signature_report,
                      |              | gold.bam_bucketed,
                      |              | gold.counselor_kpi_site,
                      |              | gold.counselor_kpi_counselor,
                      |              | gold.treatment_plan,
                      |              | gold.counseling_state_req,
                      |              | gold.med_inv_merge
```

---

## PART 4 — How RowChkSum Works in Fabric (Replacing SelectConstructor)

In the current system, `SelectConstructor.cs` builds a SQL `CHECKSUM()` expression
dynamically from `dms.tbl_MapSrc2Dsn` columns and injects it into the SELECT query.

In Fabric, this moves to PySpark using `xxhash64` (faster than SQL CHECKSUM, deterministic):

```python
# nb_checksum.py — equivalent of SelectConstructor RowChkSum logic
from pyspark.sql import functions as F

def add_row_checksum(df, exclude_cols=None):
    """
    Compute a row-level checksum across all payload columns.
    Replaces: CHECKSUM(col1, col2, col3, ...) AS RowChkSum in SQL SELECT.
    Excludes: timestamp cols (LastModAt, RunAt), binary cols (varbinary),
              the RowChkSum column itself.
    """
    if exclude_cols is None:
        exclude_cols = []

    # Columns to exclude from checksum (same logic as SelectConstructor)
    always_exclude = ['LastModAt', 'RunAt', 'RowChkSum', 'upsize_ts']
    skip_cols = set(always_exclude + exclude_cols)

    payload_cols = [c for c in df.columns if c not in skip_cols]

    # Cast all to string and concatenate, then hash (matches SQL CHECKSUM behaviour)
    concat_expr = F.concat_ws('|', *[F.coalesce(F.col(c).cast('string'), F.lit('')) 
                                      for c in payload_cols])
    return df.withColumn('RowChkSum', F.xxhash64(concat_expr))
```

---

## PART 5 — The Two Write Strategies in Fabric

Exactly mirrors the current EF Core vs SqlBulkCopy split, but implemented in PySpark.

### STRATEGY 1 — Delta MERGE (replaces EF Core Row-by-Row Upsert)

Used for: All tables currently using `Save*.cs` EF Core methods (~85+ tables)

```python
# nb_merge_base.py — replaces every SaveXxx() EF Core method
from delta.tables import DeltaTable

def silver_merge(spark, source_df, silver_table, key_cols, chksum_enabled=True):
    """
    Replaces: EF Core foreach → LINQ.Where → INSERT/UPDATE/SKIP pattern.
    SET-BASED in Spark engine — no in-memory list loading, no O(N²) loop.
    """
    target = DeltaTable.forName(spark, silver_table)

    # Build join condition on composite primary key (e.g. SiteCode + EnrollmentId)
    join_condition = " AND ".join([f"target.{k} = source.{k}" for k in key_cols])

    if chksum_enabled:
        merge_op = (
            target.alias("target")
            .merge(source_df.alias("source"), join_condition)
            .whenMatchedUpdate(
                condition="target.RowChkSum <> source.RowChkSum",   # only if changed
                set={col: f"source.{col}" for col in source_df.columns}
            )
            .whenNotMatchedInsertAll()                               # new rows
        )
    else:
        # ActionKey=3 equivalent — no checksum, always overwrite
        merge_op = (
            target.alias("target")
            .merge(source_df.alias("source"), join_condition)
            .whenMatchedUpdateAll()
            .whenNotMatchedInsertAll()
        )

    merge_op.execute()
```

**Advantage over EF Core:**
No C# `List<T>` loaded into RAM. No O(N²) LINQ loop.
Spark Delta engine runs this as a single distributed set operation.

---

### STRATEGY 2 — Bronze Append + Silver Delta MERGE (replaces SqlBulkCopy + SP MERGE)

Used for: High-volume tables (DartsSrv, Dose, Claims, LiquidLog, UAResultDetail, etc.)

```python
# nb_bulk_merge_base.py — replaces BulkDartsSvc + stg.DartsSrvMerge SP pattern
from delta.tables import DeltaTable

def bronze_append_silver_merge(spark, source_df, bronze_table, silver_table,
                                key_cols, site_code, run_ts):
    """
    Step 1: Append raw rows to Bronze (permanent history — never truncated).
            Replaces: SqlBulkCopy.WriteToServer() into stg.tbl_dartssrv

    Step 2: Delta MERGE into Silver (set-based change detection).
            Replaces: EXEC stg.DartsSrvMerge (T-SQL MERGE stored procedure)

    No staging table needed. No TRUNCATE needed.
    Delta Lake handles concurrent writes safely — multiple sites can run in parallel.
    """

    # STEP 1: Write raw source rows to Bronze (append, never overwrite)
    (
        source_df
        .withColumn('_bronze_loaded_at', F.lit(run_ts))
        .withColumn('_source_site', F.lit(site_code))
        .write
        .format('delta')
        .mode('append')
        .option('mergeSchema', 'true')
        .partitionBy('SiteCode')
        .saveAsTable(bronze_table)
    )

    # STEP 2: MERGE latest Bronze rows into Silver
    latest_bronze = spark.read.table(bronze_table).filter(
        f"SiteCode = '{site_code}' AND _bronze_loaded_at = '{run_ts}'"
    )

    target = DeltaTable.forName(spark, silver_table)
    join_condition = " AND ".join([f"t.{k} = s.{k}" for k in key_cols])

    (
        target.alias("t")
        .merge(latest_bronze.alias("s"), join_condition)
        .whenMatchedUpdate(
            condition="t.RowChkSum <> s.RowChkSum",
            set={col: f"s.{col}" for col in latest_bronze.columns 
                 if not col.startswith('_bronze')}
        )
        .whenNotMatchedInsertAll()
        .execute()
    )
```

**Key difference from current system:**
- No `TRUNCATE stg.tbl_dartssrv` before load.
- No shared staging table — each site writes to its own partition in Bronze.
- Bronze is permanent historical record (current system discards staging after MERGE).
- Multiple sites can execute this in parallel without conflict.

---

## PART 6 — The Complete Daily Fabric Timeline

```
════════════════════════════════════════════════════════════════════════
                 FABRIC DAILY ETL TIMELINE (TARGET STATE)
════════════════════════════════════════════════════════════════════════

 5:15 PM ┌──────────────────────────────────────────────────────────────┐
         │  pl_scheduler (Fabric Data Pipeline — scheduled trigger)    │
         │  Duration: ~30 seconds                                       │
         │                                                              │
         │  1. Lookup: read meta.dim_sites WHERE IsActive = 1          │
         │  2. Lookup: read meta.tbl_map_action for all enabled tables  │
         │  3. Write run manifest to meta.tbl_pipeline_run_log         │
         │     (one row per pipeline, Status = 'Pending')              │
         │  DONE. Downstream pipelines triggered by schedule.          │
         └──────────────────────────────────────────────────────────────┘
              │
              ▼
 7:00 PM  pl_forms  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_forms: extract FormQuestionAnswers, AnswerSignatures
                → Bronze append → Silver MERGE
              Duration: ~8–10 minutes (was ~30 min sequential)

 8:23 PM  pl_regional_p1  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_enrollment, nb_clientdemo, nb_uaresults ...
                → Bronze append → Silver MERGE for each table
              Duration: ~30–45 minutes (was ~4–5 hours sequential)

 8:50 PM  pl_regional_p2  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_claims, nb_bills, nb_checkin ...
                → Bronze append → Silver MERGE for each table
              Duration: ~10–15 minutes (was ~1–2 hours sequential)

 9:36 PM  pl_dose  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_dose, nb_dose_excuse
                → Bronze append → Silver MERGE
              Duration: ~5 minutes (was ~20–30 min sequential)

10:01 PM  pl_orders  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_orders: extract all years, write to single silver.orders
                  partitioned by (SiteCode, Year)
                → No year-routing in Python — Delta partition handles it
              Duration: ~4 minutes (was ~15–20 min sequential)

10:10 PM  pl_global  (Fabric Data Pipeline — parallel execution)
              Source: SAMMSGLOBAL DB only (not per-clinic loop)
              → nb_global: GlobalUser, GlobalPayer, Consents, BAM ...
              Duration: ~15 minutes (was ~2 hours)

11:50 PM  pl_inventory  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_inventory, nb_assessments, nb_appointments ...
                → All ASAM Dimension tables, PA tables, ReAssessment tables
              Duration: ~10 minutes (was ~1 hour sequential)

12:05 AM  pl_dartssrv  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_dartssrv: extract with dynamic lookback
                  (-15 / -90 / -200 days, 5-column date filter)
                → Bronze append → Silver MERGE into single silver.dartssrv
                  partitioned by (SiteCode, Year(dsDtStart))
              Duration: ~8 minutes (was ~30–60 min sequential)

 2:29 AM  pl_notes  (Fabric Data Pipeline — parallel execution)
              ForEach site (parallel=TRUE, batchCount=20):
                → nb_notes: 3pArnote (64M rows), 3pClaimNote (14M rows)
              Duration: ~6 minutes (was ~15–20 min sequential)

════════════════════════════════════════════════════════════════════════
  ETL LOADS COMPLETE BY ~3-4 AM  (same window, but most pipelines
  run dramatically faster — total parallel time ≈ 30–40 minutes
  vs current ~8 hours of sequential wall-clock time)
════════════════════════════════════════════════════════════════════════

 6:24 AM  pl_post_processing  (Fabric Data Pipeline — replaces AzureAgent)
              → nb_zero_dollar_denials     (was TRUNCATE + reload)
              → nb_signature_report        (was TRUNCATE + reload)
              → nb_bam_bucketed            (was EXEC pats.Populate_BAM_Bucketed)
              → nb_services_missing_sig    (was TRUNCATE + reload)

 6:45 AM  pl_post_processing  (second window)
              → nb_counselor_kpi_site      (was INSERT pba.tbl_...)
              → nb_counselor_kpi_counselor (was INSERT pba.tbl_...)
              → nb_treatment_plan          (was TRUNCATE + reload)

 7:00 AM  pl_post_processing  (third window)
              → nb_counseling_state_req    (was EXEC pats.SP_CounselingStateReq)
              → nb_med_inv_merge           (was EXEC pats.SP_MedInvMerge)

════════════════════════════════════════════════════════════════════════
  ALL FRESH DATA READY BY ~7-8 AM (same business SLA preserved)
════════════════════════════════════════════════════════════════════════
```

---

## PART 7 — Deep Dive: What Happens Inside pl_dartssrv

This is the exact equivalent of the `BHGTaskRunner.exe 9` deep dive.
When `pl_dartssrv` fires at 12:05 AM, here is EXACTLY what happens:

```
STEP 1 — Pipeline initialises
══════════════════════════════
Fabric Pipeline variables:
  work_date = today's date
  schedule_name = 'pl_dartssrv'

STEP 2 — Get active sites
══════════════════════════
Lookup activity: SELECT SiteCode, ConnStr, IsNewSchema
                 FROM meta.dim_site_connections
                 WHERE IsActive = 1 AND ScheduleName = 'pl_dartssrv'
Result: list of 80+ site rows


STEP 3 — Compute dynamic lookback
═══════════════════════════════════
  if work_date is a Friday AND not month-end:
      lookback_days = -15    ← normal daily run
  elif work_date is month-end Friday:
      lookback_days = -90    ← broader look for month-end
  elif work_date == specific special date (e.g. 2025-01-24):
      lookback_days = -200   ← special catch-up run
  else:
      lookback_days = -15

  cutoff_date = work_date + lookback_days


STEP 4 — ForEach site (PARALLEL — 20 sites at a time)
═══════════════════════════════════════════════════════
For each site (e.g. Site B01):

    Notebook: nb_dartssrv
    Parameters: site_code='B01', conn_str='<from Key Vault>', 
                cutoff_date='2026-04-07', work_date='2026-04-22'

    4a. Check schema: does ServiceType column exist in source?
        cols = spark.read.jdbc(conn_str, 
            "SELECT name FROM sys.all_columns WHERE object_id = 
             OBJECT_ID('dbo.tblDartsSrv') AND name = 'ServiceType'")
        has_service_type = cols.count() > 0

    4b. Build SELECT with 5-column date filter:
        where_clause = f"""
            dsClt IS NOT NULL AND (
                CONVERT(date,dsDtStart) >= '{cutoff_date}'
                OR CONVERT(date,dsDtAdded) >= '{cutoff_date}'
                OR CONVERT(date,dsUpdate)  >= '{cutoff_date}'
                OR CONVERT(date,dsBilled)  >= '{cutoff_date}'
                OR CONVERT(date,dsSigDate) >= '{cutoff_date}'
                OR dsClt <= 0
            )
        """
        if not has_service_type:
            select_cols = [c for c in all_cols if c != 'ServiceType']
        else:
            select_cols = all_cols

    4c. Extract from SAMMS source via JDBC:
        source_df = spark.read.jdbc(
            url=conn_str,
            table=f"(SELECT {','.join(select_cols)}, 
                     'B01' AS SiteCode,
                     XXHASH64(...) AS RowChkSum
                     FROM dbo.tblDartsSrv
                     WHERE {where_clause}) t",
            properties=jdbc_props
        )

    4d. Add checksum:
        source_df = add_row_checksum(source_df, exclude_cols=['LastModAt'])

    4e. Write to Bronze (append — permanent history):
        source_df.write.format('delta').mode('append')
            .partitionBy('SiteCode')
            .saveAsTable('bronze.dartssrv')

    4f. MERGE into Silver (replaces 8 stg.DartsSrvMerge procs):
        silver_merge(spark, source_df,
                     silver_table='silver.dartssrv',
                     key_cols=['SiteCode', 'DsId'])
        # Silver table is partitioned by (SiteCode, Year(dsDtStart))
        # Replaces 10 separate pats.tbl_DartsSrv_20XX tables

    4g. Log row counts:
        log_rowtrax(site_code='B01', table='silver.dartssrv',
                    source_count=source_df.count(),
                    silver_count=silver_count_for_site)

    4h. Update run log:
        meta.tbl_pipeline_run_log Status = 'Completed' or 'Failed'

→ While B01 is on step 4d, B02 is on step 4b, B03 is on step 4a ...
  (All 20 batch sites run truly in parallel)

→ Next batch of 20 sites begins when current batch completes
```

---

## PART 8 — The Year-Partition Problem — Solved

The current system has 10 physical tables for DartsSrv and 14 for Orders.
This was a workaround for Azure SQL query performance.

In Fabric, a single Delta table with partition columns replaces all of them:

```
CURRENT SYSTEM (Azure SQL):                 FABRIC (Silver Delta Lake):
══════════════════════════════              ══════════════════════════════════
pats.tbl_DartsSrv_2014B4  (0 rows)     ┐   silver.dartssrv
pats.tbl_DartsSrv_2015    (0 rows)     │   ├── partition SiteCode=B01/
pats.tbl_DartsSrv_2016    (0 rows)     │   │   ├── partition Year=2014/
pats.tbl_DartsSrv_2017    (0 rows)     │   │   ├── partition Year=2015/
pats.tbl_DartsSrv_2018    (0 rows)     │   │   ├── ...
pats.tbl_DartsSrv_2019    (7.9M rows)  │   │   └── partition Year=2023/
pats.tbl_DartsSrv_2020    (12.2M rows) ├── │   (total ~80M rows, one table)
pats.tbl_DartsSrv_2021    (28.6M rows) │   ├── partition SiteCode=B02/
pats.tbl_DartsSrv_2022    (14.2M rows) │   │   └── ...
pats.tbl_DartsSrv_2023    (17.4M rows) ┘   └── ...

pats.tbl_Orders            (122K rows)  ┐   silver.orders
pats.tbl_Orders_2016       (230K rows)  │   ├── partition SiteCode=B01/
pats.tbl_Orders_2017       (224K rows)  │   │   ├── partition Year=2016/
...                                     │   │   └── partition Year=2026/
pats.tbl_Orders_2026       (111K rows)  ┘   └── ...
pats.tbl_Orders_2027       (0 rows)
pats.tbl_Orders_2028       (0 rows)
```

**Query performance is maintained** by Delta partition pruning — querying
`WHERE SiteCode = 'B01' AND Year = 2022` only reads the relevant partition files.
No need to maintain separate physical tables.

---

## PART 9 — Connection String Security (Replacing ConStr in Control Tables)

The current system stores SAMMS connection strings in `ctrl.tbl_LocationCons.ConStr` 
as plain text in Azure SQL.

In Fabric, all credentials move to **Azure Key Vault**:

```
CURRENT:
  ctrl.tbl_LocationCons → ConStr = "Data Source=B01-SQL;Initial Catalog=SAMMS-B01;
                                    User ID=bhguser;Password=plaintext123"

FABRIC:
  meta.dim_site_connections → ConnStrSecretName = "samms-b01-connstr"
                                      │
                                      ▼
                             Azure Key Vault
                             Secret: "samms-b01-connstr"
                             Value:  "jdbc:sqlserver://B01-SQL;database=SAMMS-B01;
                                      user=bhguser;password=<secure>"

  Fabric notebooks retrieve at runtime:
    from azure.keyvault.secrets import SecretClient
    conn_str = secret_client.get_secret(site['ConnStrSecretName']).value
```

For on-premise SAMMS databases, connectivity is through:
- **On-premises Data Gateway** registered in Fabric workspace, OR
- **VPN/ExpressRoute** to allow Spark JDBC to reach the clinic SQL Servers directly

---

## PART 10 — Error Handling & Recovery in Fabric

### When a notebook fails for one site:

```
Current system:  child task Status = 20 (Error)
                 Fix: UPDATE tsk.tbl_Tasks2 SET Status = 17 WHERE ...

Fabric equivalent:
  ForEach activity setting: continueOnError = TRUE
  → Failed site writes error to meta.tbl_error_log
  → Other 79 sites continue processing
  → Pipeline overall marks as 'PartialSuccess'

  Rerun a single site:
    Trigger notebook manually with site_code parameter
    OR re-trigger the full pipeline (idempotent — Delta MERGE is safe to re-run)
```

### Idempotency — a major improvement over the current system:

```
Current system:  Re-running a completed task can cause duplicate rows
                 (EF Core may insert again if task state was reset)

Fabric Delta MERGE:  100% idempotent
                     Re-running the same pipeline for the same date
                     → same rows are detected by RowChkSum
                     → already-loaded rows = no change (MATCHED, same checksum)
                     → only genuinely new/changed rows are written
                     Safe to re-run as many times as needed
```

### Monitoring:

```sql
-- View pipeline run summary (Gold table)
SELECT PipelineName, SiteCode, RunDate, Status, SourceCount, SilverCount,
       DurationSeconds, ErrorMessage
FROM meta.tbl_pipeline_run_log
WHERE RunDate = CURRENT_DATE()
ORDER BY PipelineName, SiteCode

-- Find failed sites
SELECT SiteCode, PipelineName, ErrorMessage
FROM meta.tbl_pipeline_run_log
WHERE RunDate = CURRENT_DATE() AND Status = 'Failed'
```

---

## PART 11 — Adding / Removing / Modifying Tables (New Process)

### To ADD a new table to the Fabric ETL:

```
Step 1: CREATE Silver Delta table
        In Fabric Lakehouse:
        CREATE TABLE silver.tbl_new_table (
            SiteCode     STRING,
            NewId        INT,
            ...columns...,
            RowChkSum    BIGINT,
            LastModAt    TIMESTAMP
        )
        USING DELTA
        PARTITIONED BY (SiteCode);

Step 2: CREATE Bronze Delta table (raw history)
        CREATE TABLE bronze.new_table (
            ...same columns...,
            _bronze_loaded_at TIMESTAMP,
            _source_site      STRING
        )
        USING DELTA
        PARTITIONED BY (SiteCode);

Step 3: CREATE extraction notebook
        nb_new_table.py
        - Call nb_extract_base with source table name + WHERE clause
        - Call add_row_checksum
        - Call bronze_append_silver_merge with key_cols

Step 4: ADD metadata rows in meta.tbl_map_action and meta.tbl_map_columns
        INSERT INTO meta.tbl_map_action VALUES (...)
        INSERT INTO meta.tbl_map_columns VALUES (...)
        (No recompile needed — notebooks read metadata at runtime)

Step 5: ADD notebook call to the relevant pipeline's ForEach activity
        pl_inventory → add nb_new_table to the ForEach body

Step 6: TEST with one site, then promote to all sites
        Trigger pl_inventory with site_code = 'B01' only first
```

### To REMOVE a table:
```python
# Soft disable (recommended):
UPDATE meta.tbl_map_action SET Enabled = False WHERE TableName = 'new_table'
# No recompile, takes effect on next pipeline run
```

### To MODIFY a table (add/remove column):
```
1. ALTER TABLE silver.tbl_name ADD COLUMN new_col STRING  (Delta schema evolution)
2. UPDATE meta.tbl_map_columns — add/disable the column row
3. Update nb_new_table.py to include/exclude column
4. No recompile, no exe copy — notebook runs from OneLake automatically
```

---

## PART 12 — The Complete Fabric Architecture Diagram

```
════════════════════════════════════════════════════════════════════════════
                    BHG ETL — MICROSOFT FABRIC ARCHITECTURE
════════════════════════════════════════════════════════════════════════════

  MICROSOFT FABRIC WORKSPACE
  ┌──────────────────────────────────────────────────────────────────────┐
  │                                                                      │
  │  FABRIC DATA PIPELINES (Orchestration)                               │
  │  ┌────────────┐ ┌────────────┐ ┌────────────┐ ┌──────────────────┐  │
  │  │pl_scheduler│ │pl_dartssrv │ │pl_regional │ │pl_post_process   │  │
  │  │ 5:15 PM    │ │ 12:05 AM   │ │ _p1/_p2    │ │ 6:24/6:45/7AM    │  │
  │  │ ~30 secs   │ │ ~8 min     │ │ ~45 min    │ │ Post-ETL aggs    │  │
  │  └─────┬──────┘ └─────┬──────┘ └─────┬──────┘ └────────┬─────────┘  │
  │        │              │              │                  │            │
  │        └──────────────┴──────────────┴──────────────────┘            │
  │                              │                                       │
  │                   ForEach Site (parallel=TRUE, batchCount=20)        │
  │                              │                                       │
  │  FABRIC NOTEBOOKS (PySpark — run inside ForEach)                     │
  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │
  │  │nb_extract    │ │nb_merge_base │ │nb_checksum   │                 │
  │  │_base.py      │ │.py           │ │.py           │                 │
  │  │(JDBC read)   │ │(Delta MERGE) │ │(xxhash64)    │                 │
  │  └──────────────┘ └──────────────┘ └──────────────┘                 │
  │  ┌──────────────┐ ┌──────────────┐ ┌──────────────┐                 │
  │  │nb_dartssrv   │ │nb_dose.py    │ │nb_orders.py  │  ... 85+ more   │
  │  │.py           │ │              │ │              │                 │
  │  └──────────────┘ └──────────────┘ └──────────────┘                 │
  │                                                                      │
  │  ONELAKE STORAGE (Delta Lake — 3 layers)                             │
  │  ┌──────────────────────────────────────────────────────────────┐    │
  │  │  BRONZE  (raw, append-only, permanent history)               │    │
  │  │  bronze.dartssrv  bronze.dose  bronze.orders  bronze.claims  │    │
  │  │  Partition: SiteCode   Format: Delta Lake                    │    │
  │  └──────────────────────────────────────────────────────────────┘    │
  │  ┌──────────────────────────────────────────────────────────────┐    │
  │  │  SILVER  (current state, merged, RowChkSum deduplicated)     │    │
  │  │  silver.dartssrv  silver.dose  silver.orders  silver.claims  │    │
  │  │  silver.enrollment  silver.clientdemo  silver.uaresults ...  │    │
  │  │  Partition: SiteCode (+ Year for large tables)               │    │
  │  └──────────────────────────────────────────────────────────────┘    │
  │  ┌──────────────────────────────────────────────────────────────┐    │
  │  │  GOLD  (aggregations, KPIs, analytics-ready)                 │    │
  │  │  gold.counselor_kpi  gold.treatment_plan  gold.bam_bucketed  │    │
  │  │  meta.tbl_pipeline_run_log  meta.tbl_error_log               │    │
  │  └──────────────────────────────────────────────────────────────┘    │
  │                                                                      │
  │  METADATA / CONFIG LAYER                                             │
  │  ┌──────────────────────────────────────────────────────────────┐    │
  │  │  meta.dim_sites          (replaces ctrl.tbl_Locations)       │    │
  │  │  meta.dim_site_connections (conn secret names → Key Vault)   │    │
  │  │  meta.tbl_map_action     (replaces dms.tbl_MapAction)        │    │
  │  │  meta.tbl_map_columns    (replaces dms.tbl_MapSrc2Dsn)       │    │
  │  │  meta.tbl_forms_config   (replaces ctrl.tbl_Forms2Process)   │    │
  │  └──────────────────────────────────────────────────────────────┘    │
  └──────────────────────────────────────────────────────────────────────┘
           │                                        │
           │ reads secrets                          │ JDBC reads
           ▼                                        ▼
  ┌──────────────────┐              ┌────────────────────────────────────┐
  │  Azure Key Vault │              │  80+ SAMMS SOURCE DATABASES        │
  │  samms-b01-conn  │              │  (On-premise SQL Servers)          │
  │  samms-b02-conn  │              │  via On-premises Data Gateway      │
  │  ...             │              │  OR VPN/ExpressRoute               │
  └──────────────────┘              │                                    │
                                    │  SAMMS-ColoradoSpringsV5           │
                                    │  SAMMS-NashvilleV5                 │
                                    │  SAMMSGLOBAL                       │
                                    │  ... 80+ more ...                  │
                                    └────────────────────────────────────┘
```

---

## PART 13 — Performance Comparison: Current vs Fabric

| Metric | Current System | Microsoft Fabric | Improvement |
|--------|---------------|-----------------|-------------|
| Site parallelism | 4 regional processes | 20+ parallel notebooks | 5–20× |
| DartsSrv (80 sites) | ~30–60 min sequential | ~8 min parallel | ~6× faster |
| Regional P1 (800+ tasks) | ~4–5 hours | ~30–45 min | ~7× faster |
| Total nightly wall-clock | ~8 hours | ~40–60 minutes | ~8× faster |
| Year-partitioned tables | 24 separate tables | 2 Delta tables | Simpler |
| Staging tables | 13 shared stg.* tables | 0 (Delta replaces all) | Eliminated |
| Data history / lineage | Only current state | Bronze = full history | New capability |
| Schema changes | Recompile + copy exe | Edit notebook + metadata | Minutes vs hours |
| Re-run safety | Risk of duplicates | Fully idempotent | Safer |
| Connection string security | Plain text in Azure SQL | Azure Key Vault | Secure |

---

## PART 14 — Quick Reference Summary

| Question | Answer |
|---|---|
| What is the target system? | Microsoft Fabric — OneLake with Bronze/Silver/Gold Delta layers |
| How many Fabric pipelines? | 11 (one per BHGTaskRunner arg) + 1 post-processing |
| What replaces BHGTaskRunner? | Fabric Data Pipelines with PySpark Notebooks |
| What replaces EF Core upsert? | Delta MERGE on (SiteCode, PrimaryKey) with RowChkSum check |
| What replaces SqlBulkCopy + SP? | Bronze append + Silver Delta MERGE (no staging table) |
| What replaces stg.* staging tables? | Nothing — Delta handles concurrent writes, no staging needed |
| What replaces AzureAgent? | pl_post_processing pipeline with aggregation notebooks |
| What replaces year-partitioned tables? | Single Delta table partitioned by (SiteCode, Year) |
| What replaces ctrl.tbl_LocationCons? | meta.dim_site_connections + Azure Key Vault secrets |
| How are sites parallelised? | Fabric Pipeline ForEach with parallel=TRUE, batchCount=20 |
| How does RowChkSum work in Fabric? | PySpark xxhash64 over all payload columns |
| Can pipelines be safely re-run? | Yes — Delta MERGE is fully idempotent |
| What is Bronze layer? | Raw append-only history — every extracted row, forever |
| What is Silver layer? | Current state — one row per business key, post-MERGE |
| What is Gold layer? | Aggregations and KPIs (replaces AzureAgent computations) |
| How to add a new table? | 6 steps: create Delta table → Bronze → notebook → metadata → pipeline → test |
| How to modify a column? | Delta schema evolution + update meta.tbl_map_columns — no recompile |
| Where do secrets live? | Azure Key Vault — not in any database table |

---

*Migration design based on full analysis of BCAppCode codebase:*
*BHGTaskRunner/Program.cs, BHG-DR-LIB/BulkDartsSvc.cs, BHG-DR-LIB/Save*.cs,*
*BHG_DR_LIB_All_Save_Services.csv, Pre_Migration_Fabric_Analysis.md,*
*EF_vs_BulkCopy_Complete_Reference.md, Sequential_vs_Parallel_Execution_Analysis.md*
