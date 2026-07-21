# SAMMS DartsSrv ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `Execute_DartSrv` |
| **Parent Pipeline Object ID** | `12ad712f-3d73-48e9-b4b0-826ab69e388c` |
| **Bronze Child Pipeline** | `dartSRV_Pipeline` (`b2101370-34a9-4695-bf4a-eebdb3ced50b`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 13/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS counseling session records (DartsSrv) from 80+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — all counseling session rows are merged into a single Silver lakehouse table for downstream reporting.

**Single method processed:**

| Method | Description | Silver Target |
|--------|-------------|---------------|
| `DartsSrv` | Counseling / service session records per clinic | `bhg_silver.pats.sl_tbldartsrv` |

**Legacy context:** SAMMS-ETL-DartSvc (`BHGTaskRunner.exe 9`); replaces legacy `BulkDartsSvc` (daily bulk path) and `SaveDartsSrvs` (year-partitioned EF upsert path).

**Important design notes:**

- Bronze uses a **fixed Copy query** with dynamic optional-column handling — not a SQL builder notebook.
- Source table comes from **`meta.taskconfig.SourceTable`** per site (typically `dbo.tblDartsSrv`).
- Silver merge runs as **one notebook on the parent pipeline** (`nb_darts_bronze_to_silver`).
- **2-column merge key:** `_site_code` + `dsID`.
- **RowChkSum gate:** Full column update only when checksum changed; unchanged rows get metadata-only touch.
- Fabric uses a **single Silver table** (legacy used year-partitioned `pats.tbl_DartsSrv_20XX` tables).

---

## 2. Solution Overview

### Business Objective

Extract counseling session records from SAMMS — session dates, staff, billing, signatures, treatment dimensions, and notes — normalize to one schema across all clinics, and publish to Fabric Silver while preserving legacy RowChkSum change detection, RowState placeholder logic, and multi-date incremental lookback.

### End-to-End Data Flow

1. **Extract** per-clinic Copy Data with optional-column probe + dynamic SELECT (Bronze child — one ForEach over active sites).
2. **Transform and merge** Bronze into Silver using one PySpark notebook on the parent: dedupe, RowState, `dsDtStartYear`, Delta MERGE with RowChkSum branches.
3. **Audit** — pipeline run, task queue, and data quality written to control tables.

### Source Systems

- On-premises SAMMS SQL Server databases (one per clinic, ~80+ active Darts sites in taskconfig).
- Primary source object: `tblDartsSrv` (schema from taskconfig, usually `dbo`).
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `Dart` (`br_tblDartSrv`).
- **Silver (final):** `bhg_silver` Lakehouse — `pats.sl_tbldartsrv`.

### Overall Architecture Diagram

```
Execute_DartSrv (PARENT)
|
+- nb_get_darts_taskconfig
+- flt_active_darts_sites
+- nb_darts_audit_start
|
+- Exected_AfterBronz -> dartSRV_Pipeline (BRONZE CHILD)
|     +- fe_each_samms_database_sites (batchCount 5)
|           +- lkp_check_optional_columns_exist
|           +- Dart_Source_to_Bronz
|
+- nb_darts_bronze_to_silver (Silver notebook on parent)
+- nb_darts_audit_finalize_success / _failure
```

**Operational scope:** This documentation covers **Bronze and Silver only**. Silver is the terminal layer for consumers.

---

## 3. Pipeline Flow

### Parent Pipeline (`Execute_DartSrv`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_darts_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for DartsSrv and returns slim JSON for ConfigIds 25 and 26. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

#### Activity 2: Filter Active Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `flt_active_darts_sites` |
| **Activity Type** | Filter |
| **Purpose** | Keeps active Bronze ConfigId **25** rows where `IsActive = 1` and `SiteCode`, `DataBaseName`, and `SourceTable` are populated. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_darts_taskconfig` (Succeeded) |
| **Input** | TaskConfig notebook exit JSON |
| **Output** | Filtered site list for Bronze child |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_darts_audit_start` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `9d0b3480-fa72-4814-ad31-7bbec83a3301` |
| **Purpose** | Initiates audit — creates `PipelineRun` and `TaskQueue` rows for Bronze (~N site tasks) and Silver (1 task). |
| **Execution Sequence** | 3 |
| **Dependencies** | `flt_active_darts_sites` (Succeeded) |
| **Output** | Audit context JSON via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS DartsSrv` |
| `p_pipeline_name` | `Execute_DartSrv` |
| `p_pipeline_path` | `/pipelines/Execute_DartSrv` |
| `p_triggered_by` | `Fabric` |

**Note:** The audit notebook reads etlconfig rows for BR, SL, and GL. GL task rows are created at audit start even when Gold publish is out of operational scope.

---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Exected_AfterBronz` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `dartSRV_Pipeline` |
| **Purpose** | Per-site optional-column probe + Copy to Bronze for all active clinics. |
| **Execution Sequence** | 4 |
| **Dependencies** | `nb_darts_audit_start` (Succeeded) |
| **Output** | Bronze child completion status |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 15) |
| `p_sites` | `@activity('flt_active_darts_sites').output.value` |
| `p_audit_context_json` | Audit start notebook exit |
| `waitOnCompletion` | `true` |

---

#### Activity 5: Silver Merge (on Parent)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_darts_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `89769158-4ba9-4e86-9b6d-ff22852dddd5` |
| **Purpose** | Deduplicate Bronze run rows, derive RowState and year, Delta MERGE into final Silver table with RowChkSum branches. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Exected_AfterBronz` (Succeeded) |
| **Input** | `Dart.br_tblDartSrv` |
| **Output** | Silver table updated; notebook exit with processing counts |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |

---

#### Activity 6: Audit Finalize

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_darts_audit_finalize_success` / `nb_darts_audit_finalize_failure` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `9d0b3480-fa72-4814-ad31-7bbec83a3301` |
| **Purpose** | Finalize audit after Silver completes — marks tasks SUCCESS or FAILED; writes DataQuality. |
| **Execution Sequence** | 6 |
| **Dependencies** | Silver notebook outcome |

**Success path:** `p_mode = FINALIZE_SUCCESS`, `p_status = SUCCESS`

**Failure path:** `p_mode = FINALIZE_FAILURE`, `p_status = FAILED`

**Notification:** `nb_notify_success` and `nb_notify_failed` exist in the pipeline definition but are **Inactive**.

**Pipeline JSON note:** Audit finalize activities are currently wired to depend on Gold publish in the exported definition. For BR+SL-only operation, finalize should depend on `nb_darts_bronze_to_silver` instead.

---

### Bronze Child Pipeline (`dartSRV_Pipeline`) — Per-Site Pattern

Single **ForEach** (`fe_each_samms_database_sites`, `batchCount = 5`).

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `lkp_check_optional_columns_exist` | Lookup | Probe optional columns on source table |
| 2 | `Dart_Source_to_Bronz` | Copy | Dynamic SELECT with optional columns -> Append to `Dart.br_tblDartSrv` |

---

## 4. Source Details

| Field | Value |
|-------|-------|
| **Source System** | SAMMS On-Premises SQL Server (per clinic) |
| **Connection (high level)** | Fabric linked service via on-premises data gateway |
| **Connection ID** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Active Sites** | One taskconfig row per active clinic (ConfigId 25) |
| **Data Format** | Tabular SQL — fixed SELECT with conditional optional columns |

### Source Object (per clinic)

| Field | Value |
|-------|-------|
| **Table** | From `taskconfig.SourceTable` (typically `dbo.tblDartsSrv`) |
| **Database** | From `taskconfig.DataBaseName` (e.g., `SAMMS-Ahoskie`) |
| **Method** | `DartsSrv` |

### Optional Column Probe

Before Copy, `lkp_check_optional_columns_exist` checks whether these columns exist on the site source table:

| Column | When Missing |
|--------|--------------|
| `ServiceType` | `CAST(NULL AS varchar(100)) AS ServiceType` |
| `dsTelehealthSession` | `CAST(NULL AS bit) AS dsTelehealthSession` |
| `HoldId` | `CAST(NULL AS int) AS HoldId` |
| `upsize_ts` | `CAST(NULL AS varbinary(8)) AS upsize_ts` |

### Load Strategy

| Setting | Value |
|---------|-------|
| **Load Type** | Incremental — default **15-day** lookback (`p_lookback_days`) |
| **Legacy parity** | Legacy C# used dynamic lookback (-15 days, -90 on month-end Fridays, -200 on special dates); Fabric uses pipeline parameter |
| **Row filter** | `dsClt IS NOT NULL` required |
| **Date OR logic** | Any of five date columns within lookback, OR `dsClt <= 0` (placeholder rows always included) |

### Five-Date Incremental WHERE Clause

Records are extracted when **any** of these conditions is true:

| Condition | Column |
|-----------|--------|
| Session start in window | `dsDtStart` |
| Date added in window | `dsDtAdded` |
| Last update in window | `dsUpdate` |
| Billing date in window | `DSbilled` |
| Signature date in window | `dsSigDate` |
| Placeholder client | `dsClt <= 0` (always pulled regardless of dates) |

### RowChkSum at Source

`CHECKSUM()` is computed at extraction over core business columns (dimensions, dates, billing, signatures, optional columns). This fingerprint drives Silver merge update vs metadata-only branches.

### Core Source Columns Extracted

`dsID`, `dsClt`, `dsDIM1`–`dsDIM6`, `dsTxtSrv`, `dsDtStart`, `dsDtEnd`, `dsTxtType`, `dsdblUnits`, `dsNoteID`, `dsDtAdded`, `dstxtStaff`, `dstxtNote`, `dsRTBNOTE`, `DSbilled`, `dsGROUPNUM`, `dsPROGRAM`, `dsUpdate`, `dsUPDATEStaff`, `dsInvalidatedOn`, `dsError`, `dsTxtHIV`, `dsDartsGroup`, `repOldSrv`, signature fields (`dsSignature`, `dsSigDate`, cosign fields, client sign fields), `dsAPTID`, `dsuncharted`, `dsTxDim1`–`dsTxDim6`, `dsDIAG`, `dsArea`, group session fields, `dsDIAG10`, `SiteID`, `dsDBnotes`, signature image blobs, `MG`, plus optional columns above.

---

## 5. Destination Details

| Field | Value |
|-------|-------|
| **Destination Type** | Fabric Lakehouse (Bronze and Silver) |
| **Bronze Lakehouse** | `bhg_bronze` (Artifact ID `77d24027-6a1c-43a8-a998-1a14dd3c0d52`) |
| **Silver Lakehouse (final)** | `bhg_silver` (Artifact ID `dd09d8b6-d862-4954-a0b2-fcf7372c6595`) |
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Bronze Destination

| Schema | Table | Write Mode |
|--------|-------|------------|
| `Dart` | `br_tblDartSrv` | Append (tagged by `_ingest_run_id`) |

### Silver Destination (Final Layer)

| Schema | Table | Write Mode | Merge Key |
|--------|-------|------------|-----------|
| `pats` | `sl_tbldartsrv` | Delta MERGE | 2-column key (see below) |

### Silver MERGE Key (2 columns)

`_site_code` + `dsID`

### Silver-Derived Columns

| Column | Derivation |
|--------|------------|
| `dsDtStartYear` | `year(dsDtStart)` — year partition helper |
| `RowState` | `1` when `dsClt >= 0`; `0` when `dsClt < 0` (placeholder) |
| `silver_created_at` | Set on INSERT only; never overwritten |
| `silver_updated_at` | Refreshed on every data update |
| `last_seen_at` | Refreshed on every merge match (data or metadata branch) |

### Bronze Metadata (carried to Silver)

| Column | Purpose |
|--------|---------|
| `_site_code` | Clinic site code |
| `_source_database` | Extraction audit |
| `_ingest_run_id` | Run filter |
| `_extracted_at` | Within-run deduplication |
| `_source_query_start_date` | Lookback window start |
| `_source_query_end_date` | Lookback window end |

---

## 6. Notebook Documentation

### `nb_get_darts_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig for DartsSrv ConfigIds; returns slim JSON |
| **Input** | `meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

### `nb_darts_audit_start` / `_finalize_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `9d0b3480-fa72-4814-ad31-7bbec83a3301` |
| **Purpose** | Audit lifecycle for DartsSrv (BR + SL layers) |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_audit_context_json`, `p_ingest_run_id`, `p_sites_json`, `p_status`, `p_error_message` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

---

### `nb_darts_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `89769158-4ba9-4e86-9b6d-ff22852dddd5` |
| **Purpose** | Bronze -> Silver: dedupe, derive columns, Delta MERGE with RowChkSum branches |
| **Parameters** | `p_ingest_run_id` |
| **Input Tables** | `Dart.br_tblDartSrv` |
| **Output Table** | `pats.sl_tbldartsrv` (final) |

**Cell 1 — Prepare source:**

- Filter Bronze to current `_ingest_run_id`.
- Fail if zero rows (Copy or child pipeline issue).
- Deduplicate on `_site_code` + `dsID` (latest `_extracted_at` wins).
- Drop rows where `_site_code` or `dsID` is null.
- Add `dsDtStartYear`, `silver_updated_at`, `last_seen_at`, `RowState`.
- Create Silver table on first run (overwrite mode + `silver_created_at`).
- Auto-add `upsize_ts` column to existing Silver if missing.

**Cell 2 — Delta MERGE:**

| Branch | Condition | Action |
|--------|-----------|--------|
| Matched — data changed | `RowChkSum` differs or either side NULL | Full column update + timestamps |
| Matched — unchanged | `RowChkSum` equal | Metadata-only update (`last_seen_at`, ingest metadata) |
| Not matched | New `_site_code` + `dsID` | INSERT full row + `silver_created_at` |

**Merge condition:** `tgt._site_code = src._site_code AND tgt.dsID = src.dsID`

---

## 7. Copy Activity Documentation

| Field | Value |
|-------|-------|
| **Activity Name** | `Dart_Source_to_Bronz` |
| **Source** | SAMMS SQL Server — dynamic expression-built SELECT |
| **Destination** | `bhg_bronze.Dart.br_tblDartSrv` |
| **Mapping** | Tabular translator with type conversion |
| **Partitioning** | N/A |
| **Incremental Logic** | Five-date OR lookback + `dsClt <= 0` always |
| **Retry Configuration** | 0 (Copy default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

**Pre-step:** `lkp_check_optional_columns_exist` must succeed before Copy runs.

---

## 8. PySpark Transformations

### Data Cleansing (Bronze -> Silver)

- Filter Bronze to current `_ingest_run_id`.
- Deduplicate on 2-column business key within run.
- Drop null-key rows.
- Skip blank/invalid column names before MERGE (defensive guard).

### Business Rules Implemented (Silver)

| Rule | Description |
|------|-------------|
| 2-column merge key | `_site_code` + `dsID` |
| RowChkSum gate | Full update only when checksum changed |
| Metadata-only touch | Same checksum -> update `last_seen_at` and ingest metadata only |
| RowState | `dsClt >= 0` -> active (1); negative client -> placeholder (0) |
| Year column | `dsDtStartYear` from session start date |
| Created timestamp | `silver_created_at` preserved on updates |
| Schema evolution | Auto-add `upsize_ts` if Silver table predates column |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **MERGE — Matched (changed)** | RowChkSum differs — full update |
| **MERGE — Matched (same)** | RowChkSum equal — metadata-only update |
| **MERGE — Not Matched** | INSERT new session record |

### Performance Optimizations

- Single ForEach with `batchCount = 5` limits gateway concurrency.
- Optional-column probe avoids Copy failures on older SAMMS schemas.
- Metadata-only merge branch avoids rewriting unchanged session data.
- Single Silver table replaces legacy multi-year table fan-out.

### Error Handling

- Per-site isolation in Bronze ForEach.
- Zero Bronze rows for run raises exception in Silver notebook.
- Blank column name guard prevents empty MERGE key failures.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_lookback_days` | int | 15 | Source date filter window |

**Runtime values:**

| Value | Source |
|-------|--------|
| `p_ingest_run_id` | `@pipeline().RunId` (passed to child and Silver notebook) |
| `p_sites` | From `flt_active_darts_sites` filter output |
| `p_audit_context_json` | From `nb_darts_audit_start` exit |

### Bronze Child Parameters

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | `manual-run` | Bronze metadata |
| `p_lookback_days` | int | 15 | Source WHERE clause |
| `p_sites` | array | [] | ForEach site list |
| `p_audit_context_json` | string | — | Audit correlation |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 25 | BR | Bronze extraction (one taskconfig row per site) |
| 26 | SL | Silver merge (one taskconfig row) |

Audit prefix: **`SAMMS DartsSrv`**.

Config names:

| ConfigId | ConfigName |
|----------|------------|
| 25 | SAMMS DartsSrv Bronze Pipeline |
| 26 | SAMMS DartsSrv Silver Pipeline |

---

## 10. Dependencies

### Activity Execution Order (Parent — Silver Terminal)

```
nb_get_darts_taskconfig
  -> flt_active_darts_sites
  -> nb_darts_audit_start
  -> Exected_AfterBronz (dartSRV_Pipeline)
  -> nb_darts_bronze_to_silver
  -> nb_darts_audit_finalize_success / _failure
```

### External Dependencies

| Dependency | Requirement |
|------------|-------------|
| On-premises gateway | SAMMS SQL Server reachable per clinic |
| Fabric lakehouses | `bhg_bronze`, `bhg_silver` online |
| `meta.taskconfig` | Active ConfigId 25 rows with `SiteCode`, `DataBaseName`, `SourceTable` |
| `meta.etlconfig` | Active rows for `SAMMS DartsSrv%` prefix (audit notebook expects BR + SL + GL config rows) |

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| Optional column missing | Copy uses CAST NULL stub — site still processes |
| Bronze child fails | Silver does not run (depends on Succeeded) |
| Zero Bronze rows for run | Silver notebook fails with explicit exception |
| Silver fails | Audit finalize failure path |

### Inactive / Out-of-Scope Activities (not in active BR+SL flow)

| Activity | State | Notes |
|----------|-------|-------|
| `Prepare_DartsSrv_Versioned_Gold_Table` | Active in JSON | Gold — out of doc scope |
| `Copy_darts_silver_to_gold_version` | Active in JSON | Gold — out of doc scope |
| `Publish_DartsSrv_Versioned_Gold` | Active in JSON | Gold — out of doc scope |
| `nb_notify_success` | Inactive | Teams notification not active |
| `nb_notify_failed` | Inactive | Teams notification not active |

---

## 11. Validation

### Source Validation

- Taskconfig requires `SourceTable` populated per site.
- Optional-column Lookup validates schema before Copy.

### Row Count Validation

- Audit `DataQuality` records Bronze vs Silver counts after finalize.
- Compare Bronze rows for `_ingest_run_id` vs Silver merge counts.

### Business Validations

| Validation | Detail |
|------------|--------|
| Merge key | `_site_code` + `dsID` — unique per counseling session |
| RowChkSum | Must match source CHECKSUM for metadata-only branch |
| RowState | Negative `dsClt` -> placeholder (0) |
| Placeholder pull | `dsClt <= 0` always in source WHERE |
| dsClt NOT NULL | Required in source filter |

### Data Quality Checks

- `DuplicateCount`, `NullCount` in `meta.dataquality`
- `ValidationStatus` PASS/FAIL per method
- Silver notebook raises if Bronze count = 0 for run

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Other sites continue in ForEach |
| Missing source table | Copy fails for site | Investigate taskconfig `SourceTable` |
| Zero Bronze rows total | Silver notebook fails | Check lookback window and site activity |
| Silver MERGE failure | Pipeline fails at SL | Audit finalize failure |
| Blank column in Bronze | MERGE guard skips column | Logged in notebook |

### Retry Logic

- Copy and Lookup activities: retry = 0.
- Notebook activities: retry = 0 (default policy).

### Recovery Steps

1. Query `meta.taskaudit` for failed site/method detail.
2. Fix gateway, taskconfig, or clinic-specific connectivity.
3. Adjust `p_lookback_days` if window too narrow.
4. Re-run pipeline with new `RunId`.
5. Use Delta time travel on Silver if bad merge confirmed.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric run history — parent and Bronze child activity status.
- `meta.pipelinerun` — BR and SL layer status.

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%DartsSrv%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%DartsSrv%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (25, 26)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| Site Copy fails | Gateway, database name, `SourceTable` in taskconfig |
| Zero Bronze rows | Lookback too narrow; no recent session activity |
| Silver row count low | Compare per-site Bronze counts for ingest run |
| All rows metadata-only | Expected when RowChkSum unchanged within lookback |
| RowState = 0 rows | Negative `dsClt` placeholder sessions |
| Missing optional columns | Lookup stub values — verify clinic SAMMS version |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **Parameter validation** | `p_lookback_days` (15 default for daily) |
| **TaskConfig active** | ConfigId 25 rows active with `SiteCode`, `DataBaseName`, `SourceTable` |
| **ETL config** | Active `SAMMS DartsSrv%` rows in `meta.etlconfig` |
| **Gateway capacity** | Active sites x batchCount 5 concurrent copies |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor Succeeded through Silver |
| **Bronze completion** | Rows in `Dart.br_tblDartSrv` for ingest run |
| **Silver merge** | Row counts in `pats.sl_tbldartsrv` |
| **RowChkSum branches** | Mix of full updates vs metadata-only expected |
| **RowState** | Placeholder rows (`RowState = 0`) present where expected |
| **Audit tables** | TaskQueue, TaskAudit, DataQuality populated |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%DartsSrv%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%DartsSrv%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (25, 26)
  AND PipelineRunId = '<pipeline_run_id>';

-- Bronze row count for run
SELECT _site_code, COUNT(*) AS row_count
FROM Dart.br_tblDartSrv
WHERE _ingest_run_id = '<pipeline_run_id>'
GROUP BY _site_code
ORDER BY _site_code;

-- Silver merge sanity — sessions updated this run
SELECT _site_code, COUNT(*) AS rows_touched
FROM pats.sl_tbldartsrv
WHERE _ingest_run_id = '<pipeline_run_id>'
GROUP BY _site_code
ORDER BY _site_code;
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `Execute_DartSrv` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — `fe_each_samms_database_sites` ForEach canvas**

*[Insert screenshot]*

**3. Optional columns Lookup — `lkp_check_optional_columns_exist`**

*[Insert screenshot]*

**4. TaskConfig notebook — `nb_get_darts_taskconfig`**

*[Insert screenshot]*

**5. Audit start notebook — `nb_darts_audit_start` parameters**

*[Insert screenshot]*

**6. Copy activity — `Dart_Source_to_Bronz` dynamic source SQL**

*[Insert screenshot]*

**7. Silver notebook on parent — `nb_darts_bronze_to_silver`**

*[Insert screenshot]*

**8. Silver notebook — RowChkSum MERGE branches (Cell 2)**

*[Insert screenshot]*

**9. Bronze table — `bhg_bronze.Dart.br_tblDartSrv` schema/sample rows**

*[Insert screenshot]*

**10. Silver table — `bhg_silver.pats.sl_tbldartsrv` schema/sample rows**

*[Insert screenshot]*

**11. Pipeline monitoring — successful execution through Silver**

*[Insert screenshot]*

**12. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS DartsSrv ETL | v1.0*
