# SaveDartsSrv — Flow Diagrams

---

## EXISTING FLOW (Legacy C# / Schedule 9)

```
Windows Task Scheduler
        |
        | runs overnight / daily
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task : SAMMS-ETL-DartSvc  (Status = 17)
        |-- insert child tasks : pats.tbl_dartssrv x 80 clinics (Status = 17)
        |-- advance NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2  (Azure BHG_DR — task queue)
        |
        | when RunAt time arrives
        V
BHGTaskRunner.exe  arg = 9
        |
        |-- filter tasks : TaskName = 'SAMMS-ETL-DartSvc', SiteCode != 'PHC'
        |-- mark parent task Status = 18 (running)
        |
        |   ┌─────────────────────────────────────────────────────────┐
        |   │  LOOP — for each child task (one per clinic, ~80 sites) │
        |   └─────────────────────────────────────────────────────────┘
        |
        |-- get column list from dms.tbl_MapSrc2Dsn  (ActionKey = 9)
        |-- SelectConstructor.GetSLT()
        |       → builds SELECT field list
        |       → appends CHECKSUM(...) AS RowChkSum
        |-- check if ServiceType column exists in SAMMS
        |       → strip from SELECT if absent (version guard)
        |-- calculate DartsDate
        |       → WorkDate - 15 days  (normal)
        |       → WorkDate - 90 days  (last Friday of month)
        |       → WorkDate - 200 days (special override dates)
        |-- build WHERE clause
        |       WHERE dsClt IS NOT NULL
        |         AND ( convert(date, dsDtStart)  >= DartsDate
        |            OR convert(date, dsDtAdded)  >= DartsDate
        |            OR convert(date, dsUpdate)   >= DartsDate
        |            OR convert(date, dsBilled)   >= DartsDate
        |            OR convert(date, dsSigDate)  >= DartsDate
        |            OR dsClt <= 0 )
        |
        V
SQLSvrManager.GetTableData()
        |
        | executes SELECT against clinic SAMMS SQL Server
        | connection string from ctrl.tbl_LocationCons (this SiteCode)
        |
        V
DataTable  (in memory — rows from dbo.tblDartsSrv)
        |
        V
        ├─────────────────────────────────────────────────────────────────┐
        │  PATH A — PRIMARY (daily incremental)                           │
        │                                                                 │
        V                                                                 │
BulkDartsSvc.BulkDartsSrvLoader()                                        │
        |                                                                 │
        |-- TRUNCATE stg.tbl_DartsSrv                                    │
        |-- SqlBulkCopy.WriteToServer(DataTable)                         │
        |       → all rows bulk-inserted into stg.tbl_DartsSrv          │
        |                                                                 │
        |-- exec stg.DartsSrvMerge    → MERGE pats.tbl_DartsSrv_2014B4  │
        |-- exec stg.DartsSrvMerge22  → MERGE pats.tbl_DartsSrv_2022    │
        |-- exec stg.DartsSrvMerge23  → MERGE pats.tbl_DartsSrv_2023    │
        |-- exec stg.DartsSrvMerge24  → MERGE pats.tbl_DartsSrv_2024    │
        |-- exec stg.DartsSrvMerge25  → MERGE pats.tbl_DartsSrv_2025    │
        |       Inside each MERGE stored procedure:                      │
        |           WHEN MATCHED AND RowChkSum changed → UPDATE          │
        |           WHEN NOT MATCHED                   → INSERT          │
        |           WHEN MATCHED AND same checksum     → skip            │
        |-- TRUNCATE stg.tbl_DartsSrv  (cleanup)                        │
        |                                                                 │
        └─────────────────────────────────────────────────────────────────┘
        │  PATH B — EF CORE (historical backfill / SelectConstructor)
        │
        V
SaveDartsSrvs.SaveDartSrv20XX()
        |
        |-- load ALL Azure rows for this SiteCode into app memory
        |       db.TblDartsSrv20XX.Where(x => x.SiteCode == sc).ToList()
        |
        |-- if zero Azure rows → set AllNewRows = true
        |
        |   for each row in DataTable:
        |       if AllNewRows  → create new EF object
        |       else           → lookup existing object by DsId in List<T>
        |                            (O(N) LINQ scan per row)
        |
        |       if RowChkSum changed OR new row:
        |           map all 50+ columns one by one
        |           if new → add to DSNew list
        |
        |-- db.SaveChanges()         → commit UPDATEs
        |-- db.AddRange(DSNew)       → queue INSERTs
        |-- db.SaveChanges()         → commit INSERTs
        |
        V
pats.tbl_DartsSrv_20XX  (Azure SQL BHG_DR — FINAL DESTINATION)
        |
        V
tsk.tbl_RowTrax  ← row counts written (source vs destination)
        |
        V
BHGTaskRunner marks task Status = 20 (complete)
        |
        |   ┌─────────────────────────────────────────────────────────┐
        |   │  END OF LOOP — repeat for next clinic                   │
        |   └─────────────────────────────────────────────────────────┘
        |
        V
No structured alerts — operator checks logs manually
```

---

## RECOMMENDED FLOW (Microsoft Fabric)

```
Fabric Schedule  /  Manual trigger
        |
        | parameters: WorkDate, RunId, Environment
        V
Fabric Pipeline  —  pl_dartssrv_daily
        |
        |-- read meta.dim_site
        |       → active sites, JDBC endpoint, Key Vault secret ref
        |       → flags: ct_enabled, notes_enabled, service_type_enabled
        |-- read meta.map_column  (ActionKey = 9)
        |       → column list, types  (mirrors dms.tbl_MapSrc2Dsn)
        |-- read meta.lookback_override
        |       → special date overrides (replaces hard-coded C# dates)
        |-- write meta.pipeline_run
        |       → RunId, WorkDate, start time, status = RUNNING
        |
        |   ┌──────────────────────────────────────────────────────────────┐
        |   │  ForEach SiteCode  — parallel, max concurrency = N (e.g. 20) │
        |   └──────────────────────────────────────────────────────────────┘
        |
        V
Notebook Activity  —  nb_dartssrv_ingest_merge
  (one invocation per site, parameters: SiteCode, RunId, WorkDate)
        |
        |── STAGE 1 — EXTRACT (Bronze)
        |       |
        |       |-- resolve JDBC URL + credentials from Key Vault
        |       |-- calculate lookback window
        |       |       → -15 days  (normal)
        |       |       → -90 days  (last Friday of month)
        |       |       → override  (from meta.lookback_override table)
        |       |
        |       |-- if ct_enabled for this site:
        |       |       query CHANGETABLE with last_change_version
        |       |       (from meta.site_cdc_cursor)
        |       |   else:
        |       |       build WHERE across 5 date columns (same as legacy)
        |       |       add overlap window for late-arriving records
        |       |
        |       |-- build SELECT from meta.map_column
        |       |       → same column list as SelectConstructor
        |       |       → include CHECKSUM(...) AS RowChkSum
        |       |       → strip ServiceType if not flagged for this site
        |       |       → add notes columns if notes_enabled = true
        |       |
        |       |-- spark.read.jdbc()  → SAMMS SQL Server
        |       |
        |       V
        |   Bronze Delta table  —  bronze_dartssrv_raw
        |       |   append batch with audit columns:
        |       |       _ingest_run_id, _site_code, _extracted_at,
        |       |       _lookback_days, _source_row_count
        |       |
        |
        |── STAGE 2 — TRANSFORM + MERGE (Silver)
        |       |
        |       |-- add SiteCode column  (F.lit(site_code))
        |       |-- add LastModAt column (F.current_timestamp())
        |       |-- cast all types       (no string-length guards needed)
        |       |
        |       |-- split DataFrame by year
        |       |       filter(year(DsDtStart) == 2022) → year_df_2022
        |       |       filter(year(DsDtStart) == 2023) → year_df_2023
        |       |       ... (2014 → current year)
        |       |
        |       |   for each year slice:
        |       |       |
        |       |       V
        |       |   DeltaTable.merge()  on Silver
        |       |       condition : target.SiteCode = source.SiteCode
        |       |                   AND target.DsId = source.DsId
        |       |       |
        |       |       |-- WHEN MATCHED AND
        |       |       |       target.RowChkSum <> source.RowChkSum
        |       |       |       → UPDATE all 50+ columns
        |       |       |
        |       |       |-- WHEN NOT MATCHED
        |       |       |       → INSERT all columns
        |       |       |
        |       |       |-- WHEN MATCHED AND same RowChkSum
        |       |               → skip  (no write, no cost)
        |       |
        |       V
        |   Silver Delta tables — partitioned by year
        |       silver_dartssrv_2014 ... silver_dartssrv_2025
        |       (mirrors pats.tbl_DartsSrv_20XX semantics)
        |
        |── STAGE 3 — CHECKPOINT + LOG
        |       |
        |       |-- update meta.site_cdc_cursor
        |       |       → last_change_version / watermark_datetime
        |       |       → only updated after successful Silver commit
        |       |
        |       |-- write meta.pipeline_site_run
        |       |       → RunId, SiteCode, stage, status = SUCCESS
        |       |       → rows_bronze, rows_merged, duration_ms
        |       |
        |
        |   ┌──────────────────────────────────────────────────────────────┐
        |   │  END ForEach — repeat for next site                          │
        |   └──────────────────────────────────────────────────────────────┘
        |
        V
Fabric Pipeline  — aggregate status
        |
        |── All sites SUCCESS ?
        |       |
        |       |-- Yes → optional success digest email
        |       |           (row totals, duration, sites processed)
        |       |
        |       |-- Partial / Failure →
        |               |
        |               V
        |           Logic App / Power Automate
        |               |
        |               |-- email alert with:
        |               |       Pipeline name
        |               |       Failed site(s)
        |               |       Failed stage
        |               |       Error message
        |               |       RunId + timestamp (UTC)
        |               |       Link to Fabric Monitor
        |               |
        |               V
        |           pl_dartssrv_retry_failed  (child pipeline)
        |               |
        |               |-- query meta.pipeline_site_run
        |               |       WHERE status = 'FAILED' AND run_id = RunId
        |               |-- ForEach failed SiteCode only
        |               |-- re-invoke same notebook
        |               |       (successful sites not touched)
        |
        V
Gold Delta / Warehouse  (optional — analytics layer)
        |
        |-- views or materialized tables over Silver
        |-- Power BI / reporting consumption
        |
        V
meta.pipeline_run  ← final status, end time, total duration written
```

---

## KEY DIFFERENCES AT A GLANCE

```
                  LEGACY                       FABRIC
                  ──────                       ──────
Trigger           Windows Task Scheduler       Fabric Pipeline schedule
Parallelism       ~Sequential per site         ForEach — N sites in parallel
Source read       ADO.NET DataTable            Spark JDBC DataFrame
Change detection  RowChkSum in C# foreach      RowChkSum in Delta MERGE predicate
CDC               None — calendar lookback     SQL CT (preferred) + watermarks
Year routing      10 duplicate EF methods      1 function + year filter
Persist           EF SaveChanges / Bulk SP     Delta MERGE — ACID, time travel
Failed site retry Rerun entire job             Retry failed sites only
Logging           RowTrax + limited logs       meta Delta tables (full audit)
Alerting          Manual / none                Logic App → email on failure
Scalability       Recompile to add year/site   Update metadata table
```
