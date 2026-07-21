# SAMMS Notes ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_execute_notes` |
| **Parent Pipeline Object ID** | `189b74e5-ef11-4ee4-afb8-4a9b8a576f30` |
| **Bronze Child Pipeline** | `pl_note_saams_to_lakehouse` (`61f2955b-68c9-4a3b-8da7-186a0ce8e23e`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 10/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS clinical and billing notes from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — AR Notes and Claim Notes are published to the Silver lakehouse for downstream reporting and analytics.

**Two note methods processed:**

| Method | Description | Source Table |
|--------|-------------|--------------|
| `3pArnote` | Accounts receivable / AR notes | `tbl3pARNOTE` |
| `3pClaimNote` | Claim notes | `tbl3pClaimNote` |

**Legacy context:** SAMMS-ETL-Notes (`BHGTaskRunner.exe 7`).

**Important design note:** Silver merge runs as **two notebooks on the parent pipeline** (not a separate Silver child pipeline). Bronze extraction uses a dedicated child pipeline because of ~230 parallel Copy activities across all sites.

---

## 2. Solution Overview

### Business Objective

Extract incremental AR Notes and Claim Notes from SAMMS clinic databases, apply legacy change-detection rules (RowChkSum), and publish normalized note records to Fabric Silver for clinical and billing reporting.

### End-to-End Data Flow

1. **Extract** incremental SQL Server note data via Copy Data activities (Bronze child — 2 parallel method loops x ~115 sites).
2. **Transform and merge** Bronze data into Silver using two parallel PySpark notebooks on the parent with Delta MERGE and RowChkSum-gated updates.
3. **Audit and notify** — pipeline run, task queue, data quality, and failure alerts written to control tables.

### Source Systems

- On-premises SAMMS SQL Server databases (one database per clinic, ~115 active sites).
- Connection via Fabric on-premises data gateway.
- Tables: `tbl3pARNOTE`, `tbl3pClaimNote`.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `Notes` (append by `_ingest_run_id`).
- **Silver (final):** `bhg_silver` Lakehouse — schema `pats` (`tbl_3pARNOTE`, `tbl_3pClaimNote`).

Gold publish activities exist in the pipeline definition but are **INACTIVE**. Silver is the terminal layer.

### Overall Architecture Diagram

```
pl_execute_notes (PARENT)
|
+- nb_get_notes_taskconfig
+- flt_active_notes_sites
+- nb_notes_audit_start
|
+- Executed_AfterBronz -> pl_note_saams_to_lakehouse (BRONZE CHILD)
|     +- 2 x (Filter -> ForEach sites -> Lookup globalBatchId -> Copy)
|
+- set_bronze_method_results_from_child
+- nb_3parnote_bronze_to_silver  } parallel on parent
+- nb_3pclaimnote_bronze_to_silver }
+- set_notes_method_results
+- if_all_notes_methods_success
|     +- TRUE  -> nb_notes_audit_finalize_success
|     +- FALSE -> nb_notes_audit_finalize_failure
+- nb_notes_notify_failed (on IfCondition Failed/Skipped)
```

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_execute_notes`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_notes_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for Notes methods and returns slim JSON. Replaces inactive Lookup (`lkp_notes_taskconfig`) to avoid Fabric Lookup 4 MB limit. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_config_ids_json` | `[34,35]` |
| `p_methods_json` | `["3pArnote","3pClaimNote"]` |
| `p_only_active` | `true` |
| `p_require_site` | `false` |
| `p_require_database` | `false` |
| `p_require_source_table` | `false` |

---

#### Activity 2: Filter Active Note Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `flt_active_notes_sites` |
| **Activity Type** | Filter |
| **Purpose** | Keeps active Bronze ConfigId 34 rows for `3pArnote` and `3pClaimNote` with populated `SiteCode`, `DataBaseName`, and `SourceTable`. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_notes_taskconfig` (Succeeded) |
| **Input** | `@json(activity('nb_get_notes_taskconfig').output.result.exitValue)` |
| **Output** | Filtered JSON array — site/method list for Bronze child |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_notes_audit_start` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `9d0b3480-fa72-4814-ad31-7bbec83a3301` |
| **Purpose** | Initiates audit logging — creates `PipelineRun` and `TaskQueue` rows for Bronze (~230 site x method tasks) and Silver (2 method tasks). |
| **Execution Sequence** | 3 |
| **Dependencies** | `flt_active_notes_sites` (Succeeded) |
| **Input** | Active ETL config (ConfigId 34, 35), TaskConfig rows |
| **Output** | Audit context JSON via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS Notes` |
| `p_pipeline_name` | `Execute_Notes` |
| `p_pipeline_path` | `/pipelines/Execute_Notes` |
| `p_triggered_by` | `Fabric` |

---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterBronz` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_note_saams_to_lakehouse` |
| **Purpose** | Incremental extract of AR Notes and Claim Notes from SAMMS into Bronze. Two methods run in parallel. |
| **Execution Sequence** | 4 |
| **Dependencies** | `nb_notes_audit_start` (Succeeded) |
| **Input** | Filtered site list, ingest run ID, work date, lookback days |
| **Output** | Child `pipelineReturnValue` with per-method Bronze status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_work_date` | `@pipeline().parameters.p_work_date` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 15) |
| `p_sites` | `@activity('flt_active_notes_sites').output.value` |
| `waitOnCompletion` | `true` |

---

#### Activity 5: Capture Bronze Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_bronze_method_results_from_child` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Bronze child return JSON in parent variable `v_bronze_method_results_json`. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Executed_AfterBronz` (**Completed**) |
| **Input** | Child `pipelineReturnValue['v_bronze_method_results_json']` |
| **Output** | Pipeline variable `v_bronze_method_results_json` |

**Why SetVariable:** Child return values must be assigned to a parent variable before Silver notebooks, audit, and notification can reference them.

---

#### Activity 6: Silver MERGE — AR Notes (parallel with Activity 7)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_3parnote_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `4057574c-6a08-4c3e-8584-9ead64ee8608` |
| **Purpose** | Reads Bronze AR Notes for current ingest run; dedup; Delta MERGE into `tbl_3pARNOTE`. |
| **Execution Sequence** | 6 (parallel with Activity 7) |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |
| **Input** | Bronze table, TaskConfig JSON, bronze method results |
| **Output** | Notebook exit JSON with row counts and per-site results |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_bronze_succeeded` | Bronze `3pArnote` status = SUCCESS |
| `p_bronze_method_results_json` | `@variables('v_bronze_method_results_json')` |
| `p_sites_json` | Filtered site list |
| `p_taskconfig_json` | TaskConfig notebook exit |
| `p_method` | `3pArnote` |
| `p_bronze_config_id` | `34` |
| `p_silver_config_id` | `35` |

---

#### Activity 7: Silver MERGE — Claim Notes (parallel with Activity 6)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_3pclaimnote_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `ecb28154-151c-46c5-8c67-99940dd9d570` |
| **Purpose** | Reads Bronze Claim Notes for current ingest run; dedup; Delta MERGE into `tbl_3pClaimNote`. |
| **Execution Sequence** | 7 (parallel with Activity 6) |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |
| **Input** | Same pattern as Activity 6 |
| **Output** | Notebook exit JSON |

**Configuration details:** Same as Activity 6 except `p_method = 3pClaimNote` and `p_bronze_succeeded` checks ClaimNote status.

---

#### Activity 8: Capture Silver Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_notes_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | Combines both Silver notebook exit JSON into `v_silver_method_results_json`. |
| **Execution Sequence** | 8 |
| **Dependencies** | Both Silver notebooks (**Succeeded**, **Failed**, or **Skipped**) |
| **Input** | Exit JSON from `nb_3parnote_bronze_to_silver` and `nb_3pclaimnote_bronze_to_silver` |
| **Output** | Pipeline variable `v_silver_method_results_json` |

---

#### Activity 9: Audit Finalize (Conditional)

| Field | Value |
|-------|-------|
| **Activity Name** | `if_all_notes_methods_success` |
| **Activity Type** | IfCondition |
| **Purpose** | Routes to success or failure audit finalize based on bronze and silver method result JSON. |
| **Execution Sequence** | 9 |
| **Dependencies** | `set_notes_method_results` (Succeeded) |
| **Condition** | Neither result JSON contains `FAILED`, `ERROR`, or `SKIPPED` |

**If TRUE — Activity 9a: Success Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_notes_audit_finalize_success` |
| **Activity Type** | Notebook |
| **Purpose** | Marks all tasks SUCCESS; writes DataQuality rows for Bronze and Silver. |
| **Configuration** | `p_mode = FINALIZE_SUCCESS` |

**If FALSE — Activity 9b: Failure Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_notes_audit_finalize_failure` |
| **Activity Type** | Notebook |
| **Purpose** | Per-method partial finalize; raises exception so Fabric marks pipeline Failed. |
| **Configuration** | `p_mode = FINALIZE_FAILURE` |

---

#### Activity 10: Failure Notification (Alternative Path)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_notes_notify_failed` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `77c87686-120d-486b-9146-6a794d794e38` |
| **Purpose** | Sends failure alert with per-method BR/SL error detail. |
| **Execution Sequence** | 10 (alternative path) |
| **Dependencies** | `if_all_notes_methods_success` (Failed or Skipped) |

**Note:** `nb_notes_notify_success` exists in pipeline definition but is **INACTIVE**. Only failure notification is active.

---

### Bronze Child Pipeline (`pl_note_saams_to_lakehouse`) — Two Parallel Method Loops

| Method | Filter | ForEach | Lookup | Copy | Bronze Table |
|--------|--------|---------|--------|------|--------------|
| `3pArnote` | `flt_child_arnote_sites` | `fe_each_samms_site_arnote` | `lkp_check_arnote_globalbatchid_exists` | `cp_arnote_to_bronze` | `Notes.br_tbl3pArnote` |
| `3pClaimNote` | `flt_child_claimnote_sites` | `fe_each_samms_site_claimnote` | `lkp_check_claimnote_globalbatchid_exists` | `cp_claimnote_to_bronze` | `Notes.br_tbl3pClaimNote` |

#### Bronze Activity Pattern (per method, per site)

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `flt_child_*_sites` | Filter | Filter parent `p_sites` to one method |
| 2 | `fe_each_samms_site_*` | ForEach | Iterate sites — `isSequential: false`, `batchCount: 5` |
| 3 | `lkp_check_*_globalbatchid_exists` | Lookup | Detect whether `globalBatchId` column exists at clinic |
| 4 | `cp_*_to_bronze` | Copy | Incremental SELECT with metadata + RowChkSum -> Append to Bronze |

#### Bronze Aggregate Activity

| Field | Value |
|-------|-------|
| **Activity Name** | `set_child_bronze_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | After both ForEach loops **Complete**, builds per-method status JSON on `pipelineReturnValue`. |
| **Dependencies** | Both ForEach activities (Completed) |
| **Output** | `pipelineReturnValue['v_bronze_method_results_json']` |

---

## 4. Source Details

| Field | Value |
|-------|-------|
| **Source System** | SAMMS On-Premises SQL Server (per clinic) |
| **Connection (high level)** | Fabric linked service via on-premises data gateway |
| **Connection ID** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Active Sites** | ~115 clinic databases |
| **Data Format** | Tabular SQL data |

### Source Tables

| Method | Source Table | Load Type | Incremental Logic |
|--------|--------------|-----------|-------------------|
| `3pArnote` | `tbl3pARNOTE` | Incremental | `(arnDATE >= 2023-01-01 AND arnDATE >= lookback_date) OR (arnDtRemoved >= lookback_date)` |
| `3pClaimNote` | `tbl3pClaimNote` | Incremental | `(tpcnDtmAdded >= 2023-01-01 AND tpcnDtmAdded >= lookback_date) OR (tpcnDtTickler >= lookback_date)` |

### Incremental Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `p_lookback_days` | 15 | Days before `p_work_date` for extract window |
| `p_work_date` | Run date | Business anchor for lookback calculation |
| Minimum date floor | 2023-01-01 | Notes before this date excluded from date filters |

### Optional Column Handling

| Column | Behavior |
|--------|----------|
| `globalBatchId` | Included in Copy SQL and RowChkSum when column exists at clinic; NULL placeholder when absent (Lookup pre-check) |

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
| `Notes` | `br_tbl3pArnote` | Append (tagged by `_ingest_run_id`) |
| `Notes` | `br_tbl3pClaimNote` | Append |

### Silver Destination (Final Layer)

| Schema | Table | Write Mode | Merge Key |
|--------|-------|------------|-----------|
| `pats` | `tbl_3pARNOTE` | Delta MERGE | `SiteCode` + `arnID` |
| `pats` | `tbl_3pClaimNote` | Delta MERGE | `SiteCode` + `tpcnTPCID` |

**Important:** ClaimNote merge key is `tpcnTPCID`, **not** `tpcn`.

### Bronze Metadata Columns (not carried to Silver)

| Column | Purpose |
|--------|---------|
| `_site_code` | Maps to `SiteCode` in Silver |
| `_source_database` | Extraction audit |
| `_ingest_run_id` | Run filter |
| `_extracted_at` | Within-run deduplication |
| `_source_query_date_anchor` | Lookback context |

---

## 6. Notebook Documentation

### `nb_get_notes_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig for ConfigIds 34 and 35; returns slim JSON |
| **Parameters** | `p_config_ids_json`, `p_methods_json`, `p_only_active`, `p_require_site`, `p_require_database`, `p_require_source_table` |
| **Input** | `meta.taskconfig` |
| **Output** | JSON array via notebook exit |
| **Business Logic** | Filters to active rows for `3pArnote` and `3pClaimNote` |
| **Error Handling** | Fails pipeline if TaskConfig unreadable |

---

### `nb_notes_audit_start` / `_finalize_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `9d0b3480-fa72-4814-ad31-7bbec83a3301` |
| **Purpose** | Audit lifecycle — start runs, finalize success or per-method failure |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_pipeline_name`, `p_pipeline_path`, `p_triggered_by`, `p_audit_context_json`, `p_ingest_run_id`, `p_bronze_method_results_json`, `p_silver_method_results_json` |
| **Input Tables** | `meta.etlconfig`, `meta.taskconfig`, `meta.pipelinerun`, `meta.taskqueue` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |
| **Business Logic** | Dynamic — reads method names and dq_keys from TaskConfig RequestBody |
| **Error Handling** | Failure finalize raises exception to mark pipeline Failed |

---

### `nb_3parnote_bronze_to_silver` / `nb_3pclaimnote_bronze_to_silver`

| Field | Value |
|-------|-------|
| **Purpose** | Bronze -> Silver Delta MERGE for one note method |
| **Parameters** | `p_ingest_run_id`, `p_bronze_succeeded`, `p_bronze_method_results_json`, `p_sites_json`, `p_taskconfig_json`, `p_method`, `p_bronze_config_id`, `p_silver_config_id` |
| **Input Tables** | `bhg_bronze.Notes.br_tbl3pArnote` or `br_tbl3pClaimNote` |
| **Output Tables** | `bhg_silver.pats.tbl_3pARNOTE` or `tbl_3pClaimNote` |
| **Business Logic** | Resolve tables from TaskConfig -> read Bronze -> skip if bronze failed -> dedup -> transform -> Delta MERGE |
| **Merge/Upsert Logic** | RowChkSum-gated update; insert new keys; `RowState = true` for returned rows |
| **Error Handling** | Per-method isolation — one note type failing does not block the other |
| **Performance** | TaskConfig-driven table resolution; Delta MERGE |

**Method-specific rules:**

| Method | Match Key | RowChkSum Gate | Notable Behavior |
|--------|-----------|----------------|------------------|
| `3pArnote` | `SiteCode` + `arnID` | Yes | Update only when checksum changed |
| `3pClaimNote` | `SiteCode` + `tpcnTPCID` | Yes | Match on `tpcnTPCID`, not `tpcn` |

---

### `nb_notes_notify_failed`

| Field | Value |
|-------|-------|
| **Purpose** | Sends failure notification on pipeline finalize failure |
| **Parameters** | `Pipeline_Name`, `Status`, `Config_Name`, `Source_System`, `Target_Name`, `Environment_Name`, `Run_id`, error detail |
| **Business Logic** | Parses bronze/silver method JSON; alerts with failed method names and stages (BR/SL) |

---

## 7. Copy Activity Documentation

Each Bronze Copy activity follows the same incremental pattern.

| Field | Value |
|-------|-------|
| **Source** | SAMMS SQL Server (clinic database via gateway) |
| **Destination** | `bhg_bronze.Notes.br_tbl3pArnote` or `br_tbl3pClaimNote` |
| **Mapping** | Dynamic SQL — metadata columns + source business columns + `RowChkSum = CHECKSUM(...)` |
| **Partitioning** | N/A |
| **Incremental Logic** | Date-based lookback with 2023-01-01 floor (see Section 4) |
| **Retry Configuration** | 0 (pipeline default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

### Metadata Columns Added in Every Bronze Copy

| Column | Purpose |
|--------|---------|
| `_site_code` | Clinic identifier |
| `_source_database` | SAMMS database name |
| `_ingest_run_id` | Pipeline run correlation |
| `_extracted_at` | Extraction timestamp |
| `_source_query_date_anchor` | Work date / lookback anchor |

### RowChkSum at Source

`RowChkSum` is computed in the Bronze Copy SQL on the SAMMS server using the same column set as legacy ETL. When `globalBatchId` is absent, NULL is used in the checksum expression.

---

## 8. PySpark Transformations

### Data Cleansing (Bronze -> Silver)

- Filter Bronze rows to current `_ingest_run_id` only.
- Deduplicate within run on business key, keeping latest `_extracted_at`.
- Rename `_site_code` to `SiteCode`.
- Set `LastModAt` to current timestamp.

### Business Rules Implemented (Silver)

| Rule | Description |
|------|-------------|
| RowChkSum gate | UPDATE matched rows only when source `RowChkSum` differs from Silver |
| Insert new keys | `whenNotMatchedInsert` for rows not in Silver target |
| RowState | All rows returned in extract marked active (`RowState = true`); no pre-reset sweep |
| ClaimNote match key | Must use `SiteCode` + `tpcnTPCID`, not `tpcn` |
| Dynamic globalBatchId | Null when column absent at clinic; included in checksum when present |
| TaskConfig table names | Silver resolves Bronze/Silver table names at runtime |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **MERGE — Matched + RowChkSum changed** | Update all business columns |
| **MERGE — Matched + RowChkSum unchanged** | Skip update (legacy behavior) |
| **MERGE — Not Matched** | INSERT new business key |

### Performance Optimizations

- Two Bronze ForEach loops run in parallel (ARNote + ClaimNote).
- Site batching: `batchCount = 5` limits concurrent gateway connections.
- Two Silver notebooks run in parallel on parent with no cross-notebook dependency.

### Error Handling

- Per-method isolation in Bronze ForEach and Silver notebooks.
- Bronze child returns partial status JSON even when one method fails.
- Silver exits SKIPPED when Bronze failed for that method; SUCCESS with zero counts when Bronze succeeded but no rows in window.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters (`pl_execute_notes`)

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | `test-run-001` (override) / `pipeline().RunId` at runtime | Tags Bronze rows; filters Silver |
| `p_work_date` | string | Business date (e.g. `2026-06-29`) | Anchor for lookback date calculation |
| `p_lookback_days` | int | 15 | Incremental extract window |

### Parent Pipeline Variables

| Variable | Set By | Used By |
|----------|--------|---------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver notebooks, audit finalize, notify |
| `v_silver_method_results_json` | `set_notes_method_results` | Audit finalize, notify |

### Bronze Child Parameters (`pl_note_saams_to_lakehouse`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Metadata column on every Bronze row |
| `p_work_date` | string | Lookback anchor |
| `p_lookback_days` | int | Incremental window (default 15) |
| `p_sites` | array | Filtered TaskConfig site list from parent |

### Child Return Values (`pipelineReturnValue`)

| Child | Return Key | Set By |
|-------|------------|--------|
| `pl_note_saams_to_lakehouse` | `v_bronze_method_results_json` | `set_child_bronze_method_results` |

### Pipeline Built-in Variables

| Variable | Usage |
|----------|-------|
| `@pipeline().Pipeline` | Pipeline name in audit |
| `@pipeline().RunId` | Ingest run ID |
| `@pipeline().TriggerTime` | Notification timestamp |

---

## 10. Dependencies

### Activity Execution Order (Parent)

```
nb_get_notes_taskconfig
  -> flt_active_notes_sites
  -> nb_notes_audit_start
  -> Executed_AfterBronz (pl_note_saams_to_lakehouse)
  -> set_bronze_method_results_from_child
  -> nb_3parnote_bronze_to_silver + nb_3pclaimnote_bronze_to_silver (parallel)
  -> set_notes_method_results
  -> if_all_notes_methods_success
      +- TRUE  -> nb_notes_audit_finalize_success
      +- FALSE -> nb_notes_audit_finalize_failure
  -> nb_notes_notify_failed (if IfCondition Failed/Skipped)
```

### Notebook Dependencies

| Notebook | Depends On |
|----------|------------|
| Silver notebooks | Bronze completion; reads `p_bronze_method_results_json` and `p_bronze_succeeded` |
| Audit finalize | Both bronze and silver result JSON variables |
| Notify | IfCondition failure |

### Table Dependencies

| Table | Dependency |
|-------|------------|
| `meta.taskconfig` | Active ConfigId 34 (Bronze) and 35 (Silver) rows |
| `meta.etlconfig` | ConfigId 34, 35 must be active |
| Silver target tables | Auto-created on first run if not present |

### External Dependencies

- On-premises SAMMS SQL Server availability via data gateway.
- Fabric workspace and lakehouses online (`bhg_bronze`, `bhg_silver`).

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| TaskConfig `IsActive = 0` | Site/method excluded at Filter step |
| Bronze method FAILED | Silver notebook exits SKIPPED for that method |
| Bronze succeeded, zero rows | Silver exits SUCCESS with zero counts |
| Any method FAILED/ERROR/SKIPPED in result JSON | Audit routes to FINALIZE_FAILURE branch |

### Inactive Activities (not in active flow)

| Activity | State | Notes |
|----------|-------|-------|
| `lkp_notes_taskconfig` | Inactive | Replaced by notebook |
| `Prepare_*_Gold_Table`, Copy Silver to Gold, Publish Gold | Inactive | Silver is terminal layer |
| `nb_notes_notify_success` | Inactive | Only failure notify active |

---

## 11. Validation

### Source Validation

- TaskConfig filter ensures `SiteCode`, `DataBaseName`, `SourceTable` populated for Bronze site tasks.
- `globalBatchId` Lookup prevents hard failure on older SAMMS versions.

### Row Count Validation

- Audit `DataQuality` table records source vs target row counts per method after finalize.
- Silver notebook exit JSON includes `rows_read`, `rows_inserted`, `rows_updated`, `rows_skipped`.

### Duplicate Checks

- Within-run deduplication on business key before MERGE.
- `DuplicateCount` metric written to `meta.dataquality`.

### Null Checks

- `NullCount` metric written to `meta.dataquality` for key columns.

### Business Validations

| Validation | Detail |
|------------|--------|
| ARNote merge key | `SiteCode` + `arnID` |
| ClaimNote merge key | `SiteCode` + `tpcnTPCID` (not `tpcn`) |
| RowChkSum gate | Updates skipped when checksum unchanged — expected behavior |
| Date floor | Notes before 2023-01-01 excluded from incremental filters |
| RowState | All extracted rows marked active in Silver |

### Data Quality Checks

- `ValidationStatus` recorded as PASS or FAIL per method in `meta.dataquality`.
- Per-method JSON status verified in pipeline variables.

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS connection failure | Site Copy fails | Method may partial-fail; status in bronze JSON |
| Missing `globalBatchId` column | None — handled dynamically | Lookup nulls column; checksum adapts |
| Silver MERGE failure | Method fails at SL | Other method continues; audit partial finalize |
| Bronze method FAILED | Silver SKIPPED | Expected — no stale merge |
| Audit finalize failure | Pipeline marked Failed | Notify notebook sends alert |

### Retry Logic

- Pipeline activities: retry = 0 (default).

### Exception Handling

- Silver notebooks: per-method try/except — returns FAILED status in exit JSON.
- Audit finalize failure: raises exception after writing audit rows.

### Logging

| Location | Content |
|----------|---------|
| `meta.pipelinerun` | Run status, layer, timestamps |
| `meta.taskqueue` | Per-site (Bronze) and per-method (Silver) task status |
| `meta.taskaudit` | Step-level logs, row counts |
| `meta.dataquality` | Validation metrics |
| Fabric pipeline monitor | Activity-level success/failure |

### Recovery Steps

1. Identify failed method/stage from `v_bronze_method_results_json` or `v_silver_method_results_json`.
2. Query `meta.taskaudit` and `meta.dataquality` for the ingest run ID.
3. Fix root cause (gateway, date window, merge key).
4. Re-run pipeline — records tagged with new `_ingest_run_id` prevent duplicate merges.
5. If bad Silver merge confirmed, use Delta time travel on affected table before re-run.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric pipeline run history — parent and Bronze child activity status.
- `meta.pipelinerun` — `Status`, `RestartFlag`, `AttemptNumber` per layer (BR, SL).

### Notebook Monitoring

- Silver notebook exit JSON — per-method row counts and `site_results`.
- Audit notebook output — task queue IDs and finalize status.

### Execution History

- `meta.taskqueue` — ~230 Bronze tasks + 2 Silver tasks per run.
- `meta.taskaudit` — detailed step logs with row counts.

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%Notes%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%Notes%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (34, 35)` AND ingest run filter |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| No sites in Bronze | TaskConfig ConfigId 34 active? TaskConfig notebook output? |
| One note type fails all sites | Bronze ForEach for that method — gateway connectivity |
| Silver SKIPPED | `v_bronze_method_results_json` — method FAILED at BR |
| Silver SUCCESS but zero rows | `p_work_date`, `p_lookback_days`; notes before 2023-01-01 floor |
| Updates not appearing in Silver | RowChkSum unchanged — expected (no update when data identical) |
| ClaimNote duplicates | Match key must be `SiteCode` + `tpcnTPCID` |
| Audit FAILED but data looks fine | Result JSON contains `SKIPPED` string? |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | On-prem SAMMS SQL databases accessible via data gateway |
| **Required access/permissions** | Gateway service account has read access to all clinic databases |
| **Environment readiness** | Fabric workspace online; `bhg_bronze` and `bhg_silver` lakehouses available |
| **Parameter validation** | `p_work_date`, `p_lookback_days` (default 15), `p_ingest_run_id` if override |
| **Dependency validation** | `meta.etlconfig` ConfigId 34, 35 active; TaskConfig Bronze rows active |
| **Required datasets availability** | Silver target tables exist or auto-create enabled |
| **Previous execution status** | No conflicting re-run with same ingest run override |
| **Gateway capacity** | 2 parallel Bronze methods x batchCount 5 — confirm gateway concurrency |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor shows Succeeded (or Failed with expected partial failure) |
| **Activity completion status** | Both Silver notebooks Completed; review Failed ForEach sites in Bronze child |
| **Source-to-target record count** | Compare `meta.taskaudit` rows read vs written per method |
| **Data quality validation** | Review `meta.dataquality` — `DuplicateCount`, `NullCount`, `ValidationStatus` |
| **Target table validation** | Query Silver `tbl_3pARNOTE` and `tbl_3pClaimNote` for expected site coverage |
| **Log review** | Check for SKIPPED methods; review Silver notebook exit JSON |
| **Error verification** | If Failed — review `nb_notes_notify_failed` alert and finalize audit output |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%Notes%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%Notes%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (34, 35)
  AND PipelineRunId = '<pipeline_run_id>';
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_execute_notes` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — 2 parallel ForEach blocks (ARNote + ClaimNote)**

*[Insert screenshot]*

**3. Silver notebooks on parent — `nb_3parnote_bronze_to_silver` and `nb_3pclaimnote_bronze_to_silver`**

*[Insert screenshot]*

**4. TaskConfig notebook activity configuration**

*[Insert screenshot]*

**5. Audit start notebook parameters**

*[Insert screenshot]*

**6. Copy activity — ARNote incremental source SQL**

*[Insert screenshot]*

**7. Copy activity — ClaimNote incremental source SQL**

*[Insert screenshot]*

**8. globalBatchId Lookup activity configuration**

*[Insert screenshot]*

**9. Silver notebook — Delta MERGE / RowChkSum gate logic**

*[Insert screenshot]*

**10. IfCondition — audit finalize branches**

*[Insert screenshot]*

**11. Pipeline monitoring — successful execution**

*[Insert screenshot]*

**12. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS Notes ETL | v1.0*
