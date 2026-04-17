# SaveDartsSrvs_Fabric.py — Flow Diagrams (ASCII)

This matches the **structure and steps described inside** `SaveDartsSrvs_Fabric.py`: architecture notes, DDL for Lakehouse tables, pipeline **`pl_dart_incremental`**, and notebooks **`nb_dart_00_setup`** through **`nb_dart_03_optimize`**.  
*(The file mixes markdown-style documentation with embedded SQL/Python — these flows follow that content, not a separate repo.)*

---

## 1. End-to-end: Fabric target (everything in one tree)

```
Fabric schedule / manual trigger
        |
        V
pl_dart_incremental  (or pl_dart_initial_load for first-time backfill)
        |
        |-- parameters: p_run_id, p_work_date, p_load_type,
        |               p_site_code_override (optional),
        |               p_initial_start / p_initial_end (initial load only)
        |
        V
Activity 1 — Set / receive run_id
        |
        V
Activity 2 — INSERT meta_pipeline_run
        |       (run_id, pipeline_name, load_type, work_date,
        |        start_time, status = RUNNING, ...)
        |
        V
Activity 3 — Lookup active sites
        |       FROM meta_sites WHERE is_active = true
        |       if p_site_code_override → single site only
        |
        V
Activity 4 — ForEach site  (concurrency ~ 8–12 to start)
        |
        |   ┌─────────────────────────────────────────────────────────────┐
        |   │  PER SITE (repeated for each SiteCode)                      │
        |   └─────────────────────────────────────────────────────────────┘
        |
        |-- 4.1 Read meta_watermark
        |       → last_successful_watermark for site + tblDartsSrv
        |
        |-- 4.2 Build window for Copy query
        |       INCREMENTAL: @window_start = watermark - overlap_hours
        |                    (multi-column OR on dsUpdate, dsDtAdded,
        |                     dsBilled, dsSigDate, dsDtStart, dsDtEnd)
        |       INITIAL:     dsDtStart between @initial_start and @initial_end
        |
        |-- 4.3 Copy activity (gateway → SAMMS SQL Server)
        |       → SELECT ... CHECKSUM(...) AS RowChkSum
        |       → FROM dbo.tblDartsSrv
        |       → append into bronze_dartssrv_raw
        |       → add audit cols: _site_code, _ingest_run_id, _extracted_at,
        |                         _source_query_start_ts, _source_query_end_ts
        |
        |-- 4.4 Notebook nb_dart_01_merge_silver
        |       (widgets: run_id, site_code, load_type)
        |       → see Section 3 below
        |
        |-- 4.5 On success: meta_site_run already updated inside notebook
        |-- 4.6 On failure branch:
        |       UPDATE meta_site_run status = FAILED, error_message
        |       → pipeline alert / retry path
        |
        V
Activity 5 — Notebook nb_dart_02_build_gold
        |       → refresh gold_dartssrv_current
        |       → rebuild gold_dartssrv_daily aggregates
        |
        V
Activity 6 — Notebook nb_dart_03_optimize
        |       → OPTIMIZE silver_dartssrv (recent service_year)
        |       → OPTIMIZE gold_dartssrv_current
        |       → OPTIMIZE gold_dartssrv_daily
        |
        V
Activity 7 — UPDATE meta_pipeline_run
        |       end_time, status = SUCCESS (or FAILED)
        |
        V
Optional — Alerts (pipeline failure, Real-Time hub, Power Automate)
        Monitoring hub + meta_* tables for ops
```

---

## 2. One-time / rare: nb_dart_00_setup (Lakehouse DDL)

```
Open notebook nb_dart_00_setup  (lakehouse lh_dartservice attached)
        |
        V
CREATE meta_sites
        |   site_code, server_name, database_name, gateway_connection_name,
        |   secret_scope / keys, lookback_days, overlap_hours,
        |   notes_enabled, service_type_enabled, ...
        V
CREATE meta_pipeline_run
CREATE meta_site_run
CREATE meta_watermark
        |
        V
CREATE bronze_dartssrv_raw  (Delta, PARTITIONED BY _site_code)
        |   all DartsSrv columns + RowChkSum
        |   audit: _site_code, _ingest_run_id, _extracted_at,
        |           _source_query_start_ts, _source_query_end_ts
        V
CREATE silver_dartssrv  (Delta, PARTITIONED BY service_year)
        |   curated columns + SiteCode + RowChkSum
        |   service_year, first_ingest_run_id, last_ingest_run_id, last_merged_at
        V
CREATE gold_dartssrv_current   (empty shell from Silver WHERE 1=0)
CREATE gold_dartssrv_daily     (aggregates schema)
        |
        V
Seed INSERT into meta_sites (test clinics)
        |
        V
Ready for pl_dart_initial_load / pl_dart_incremental
```

---

## 3. Per-site: nb_dart_01_merge_silver (Silver merge notebook)

```
Notebook start — nb_dart_01_merge_silver
        |
        |-- Read widgets: run_id, site_code, load_type (= INCREMENTAL default)
        |-- if run_id missing → generate RUN_YYYYMMDDhhmmss_<uuid8>
        |-- if site_code missing → raise ValueError
        |
        |-- stage_name = "MERGE_SILVER"
        |
        V
INSERT meta_site_run
        |   run_id, site_code, stage_name, start_time, status = RUNNING
        |   (counts null until end)
        |
        V
Load Bronze slice for this site + run
        |   spark.table("bronze_dartssrv_raw")
        |     .filter(_site_code == site_code)
        |     .filter(_ingest_run_id == run_id)
        |
        V
bronze_count = count()
        |
        |-- if bronze_count == 0:
        |       UPDATE meta_site_run → SUCCESS, all row counts 0
        |       SystemExit("No rows found for this site/run")
        |
        V
STANDARDIZE — curated_df
        |   SiteCode      ← _site_code
        |   service_year  ← year(DsDtStart)
        |   first_ingest_run_id / last_ingest_run_id ← _ingest_run_id
        |   last_merged_at ← current_timestamp()
        |   select(all business columns + RowChkSum + service_year + ...)
        |
        V
DEDUP within batch — dedup_df
        |   Window.partitionBy(SiteCode, DsId)
        |   orderBy DsUpdate DESC NULLS LAST, last_merged_at DESC
        |   row_number() = 1
        |
        V
createOrReplaceTempView("src_dart_batch")
        |
        V
PRE-MERGE COUNTS (estimate vs existing Silver for this SiteCode)
        |   join dedup_df (s) left join silver_dartssrv (t)
        |       on SiteCode + DsId
        |   insert_count   = t.DsId IS NULL
        |   update_count   = matched AND s.RowChkSum != t.RowChkSum
        |   unchanged_count = matched AND s.RowChkSum == t.RowChkSum
        |
        V
DeltaTable.forName("silver_dartssrv").merge(...)
        |   ON  t.SiteCode = s.SiteCode AND t.DsId = s.DsId
        |
        |   WHEN MATCHED AND t.RowChkSum <> s.RowChkSum
        |       → UPDATE all mapped columns + service_year +
        |                 last_ingest_run_id + last_merged_at
        |
        |   WHEN NOT MATCHED
        |       → INSERT full row incl. first_ingest_run_id
        |
        |   (same RowChkSum → no write)
        |
        V
UPDATE meta_watermark  (MERGE INTO)
        |   max_activity_ts = MAX( GREATEST(
        |       DsUpdate, DsDtAdded, DsBilled, DsSigDate, DsDtStart, DsDtEnd ))
        |   over bronze_df for this batch
        |   → site_code, table_name = 'tblDartsSrv',
        |      last_successful_watermark, last_run_id, updated_at
        |
        V
UPDATE meta_site_run
        |   end_time, status = SUCCESS
        |   rows_extracted  = bronze_count
        |   rows_inserted   = insert_count
        |   rows_updated    = update_count
        |   rows_unchanged  = unchanged_count
        |   rows_rejected   = 0
        |   max_activity_ts = max_activity_ts (or NULL)
        |   WHERE run_id + site_code + stage MERGE_SILVER + RUNNING
        |
        V
End notebook (failure path should UPDATE meta_site_run FAILED + error)
```

---

## 4. Gold: nb_dart_02_build_gold

```
nb_dart_02_build_gold
        |
        |-- widget: run_id (or auto-generate)
        |
        V
silver_df = spark.table("silver_dartssrv")
        |
        |-- gold_dartssrv_current
        |       silver_df.write Delta mode OVERWRITE → saveAsTable(...)
        |
        V
gold_dartssrv_daily
        |   service_date = to_date(DsDtStart)
        |   groupBy SiteCode, service_date
        |   agg: session_count, unique_clients (countDistinct DsClt), total_units
        |   load_run_id, built_at
        |
        V
daily_df.write Delta OVERWRITE → gold_dartssrv_daily
```

---

## 5. Maintenance: nb_dart_03_optimize

```
nb_dart_03_optimize
        |
        |-- spark.conf: parquet vorder default = false  (write-heavy preference)
        |
        V
OPTIMIZE silver_dartssrv
        |   WHERE service_year >= year(current_date()) - 2
        |
        V
OPTIMIZE gold_dartssrv_current
        |
        V
OPTIMIZE gold_dartssrv_daily
```

---

## 6. Extraction query shape (what Copy activity runs)

**Initial load (from file section 5):**

```
SELECT  dsID AS DsId, ... , CHECKSUM(...) AS RowChkSum
FROM dbo.tblDartsSrv
WHERE dsDtStart >= @initial_start
  AND dsDtStart <  @initial_end
```

**Incremental (watermark + overlap — from file section 5):**

```
SELECT  ... , CHECKSUM(...) AS RowChkSum
FROM dbo.tblDartsSrv
WHERE dsUpdate   >= @window_start
   OR dsDtAdded  >= @window_start
   OR dsBilled   >= @window_start
   OR dsSigDate  >= @window_start
   OR dsDtStart  >= @window_start
   OR dsDtEnd    >= @window_start
```

*(Aligns with legacy multi-date “activity” logic; not SQL Server CDC in v1 of this doc.)*

---

## 7. Quick map: file section → artifact

| Section in `SaveDartsSrvs_Fabric.py` | What it is |
|--------------------------------------|------------|
| Phases A–E / §4 | `nb_dart_00_setup` DDL (`meta_*`, Bronze, Silver, Gold) |
| §5 | Parameterized Copy queries (initial vs incremental) |
| §6 | `pl_dart_incremental` activity list |
| §7 / `# nb_dart_01_merge_silver` | Full merge notebook body |
| §8 | `nb_dart_02_build_gold` |
| §9 | `nb_dart_03_optimize` |
| §10+ | Initial-load pipeline, alerts, security, sprint order |

---

## 8. Bronze vs Silver vs Gold (one line each)

```
SAMMS dbo.tblDartsSrv
        →  Copy + gateway  →  bronze_dartssrv_raw   (append, partitioned by _site_code)
        →  nb_dart_01      →  silver_dartssrv      (MERGE key SiteCode+DsId, RowChkSum gate, partitioned by service_year)
        →  nb_dart_02      →  gold_dartssrv_*      (current snapshot + daily aggregates)
```

---

*For legacy C# vs Fabric comparison at a higher level, see `SaveDartsSrv_Flow_Diagrams.md` and `SaveDartsSrv_Legacy_vs_Fabric_Workflow.md`.*
