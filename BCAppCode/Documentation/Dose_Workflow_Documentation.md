# SAMMS Dose ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_dose` |
| **Parent Pipeline Object ID** | `a3401580-ada4-49c7-8efe-55a94295a020` |
| **Bronze Child Pipeline** | `pl_dose_src_brz` (`b3b79e02-d56b-4f2a-b68e-289793c8d8d5`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 14/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS medication dose records and dose excuse records from clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — dose and dose-excuse rows are published to the Silver lakehouse for downstream reporting.

**Two methods processed:**

| Method | Description | Source Table | Silver Target |
|--------|-------------|--------------|---------------|
| `Dose` | Medication dose / dispensing records | `dbo.tblDOSE` | `bhg_silver.pats.tbl_dose` |
| `DoseExcuse` | Dose excuse records | `dbo.tblDOSE_Excuse` | `bhg_silver.pats.tbl_dose_excuse` |

**Legacy context:** SAMMS-ETL-Dose (`BHGTaskRunner.exe 10`); replaces legacy `BulkDartsSvc` dose bulk path and related Save* dose processing.

**Important design notes:**

- Bronze child runs **two parallel ForEach loops** (Dose + DoseExcuse), each with `batchCount = 10`.
- Silver merge runs as **two notebooks on the parent pipeline** in parallel (`doses_excuse_bronze_to_silver`, `dose_bronze_to_silver`).
- Bronze uses **IF EXISTS table gate** in Copy SQL — site skips gracefully when table missing.
- **Per-method status JSON** flows Bronze child → parent variable → Silver notebooks → audit finalize (same pattern as Notes and P1 Reference).
- DoseExcuse Silver applies **RowState pre-reset** (all rows set to 0 before merge); Dose Silver merges on **RowChkSum** with update gate.

---

## 2. Solution Overview

### Business Objective

Extract medication dose and dose-excuse data from SAMMS clinic databases, preserve legacy RowChkSum change detection and RowState rules, and publish normalized records to Fabric Silver for medication tracking, compliance, and billing support.

### End-to-End Data Flow

1. **Extract** per-clinic Copy Data for `tblDOSE` and `tblDOSE_Excuse` (Bronze child — two parallel ForEach loops over active sites).
2. **Transform and merge** Bronze into Silver using two parallel PySpark notebooks on the parent: RowState pre-reset + Delta MERGE (DoseExcuse); RowChkSum-gated MERGE (Dose).
3. **Audit** — pipeline run, task queue, and data quality written to control tables; per-method partial success supported.

### Source Systems

- On-premises SAMMS SQL Server databases (one per clinic; active sites in TaskConfig ConfigId 7).
- Tables: `dbo.tblDOSE`, `dbo.tblDOSE_Excuse`.
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `Dose` (`br_tblDose`, `br_tblDoseExcuse`).
- **Silver (final):** `bhg_silver` Lakehouse — `pats.tbl_dose`, `pats.tbl_dose_excuse`.

### Overall Architecture Diagram

```
pl_dose (PARENT)
|
+- nb_get_taskconfigs
+- fliter_Active_Sitecodes
+- control_audit_dose
|
+- Src_to_Brz -> pl_dose_src_brz (BRONZE CHILD)
|     +- flt_child_doseexcuse_sites -> fe_samms_doseexcuse (batchCount 10)
|     |     +- Dose_excuse_src_to_brz
|     +- flt_Child_dose_Sites -> fe_samms_dose (batchCount 10)
|           +- Dose_src_to_brz
|     +- set_child_bronze_method_result (pipelineReturnValue)
|
+- set_bronze_method_results_from_child
+- doses_excuse_bronze_to_silver  } parallel on parent
+- dose_bronze_to_silver          }
+- Set_dose_method_results
+- If Condition1
|     +- TRUE  -> control_audit_dose_Sucess
|     +- FALSE -> control_audit_dose_Failure
+- nb_dose_failure_notification (Inactive)
```

**Operational scope:** This documentation covers **Bronze and Silver only**. Silver is the terminal layer for consumers. Gold publish notebooks exist in the pipeline definition but are **out of scope** for this document.

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_dose`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_taskconfigs` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for Dose ConfigIds 7 and 8; returns slim JSON for both methods. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_config_ids_json` | `[7,8]` |
| `p_methods_json` | `["DoseExcuse","Dose"]` |
| `p_only_active` | `true` |
| `p_require_site` | `false` |
| `p_require_database` | `false` |
| `p_require_source_table` | `false` |

---

#### Activity 2: Filter Active Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `fliter_Active_Sitecodes` |
| **Activity Type** | Filter |
| **Purpose** | Keeps active Bronze ConfigId **7** rows where `TaskName` is `Bronze DoseExcuse` or `Bronze Dose`, `IsActive = 1`, and `SiteCode` + `DataBaseName` are populated. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_taskconfigs` (Succeeded) |
| **Output** | Filtered site/method list for Bronze child |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `control_audit_dose` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `2ba7000b-89f6-4e40-ac7f-7787792e2ee8` |
| **Purpose** | Initiates audit — creates `PipelineRun` and `TaskQueue` rows for Bronze (per site x method) and Silver (2 method tasks). |
| **Execution Sequence** | 3 |
| **Dependencies** | `fliter_Active_Sitecodes` (Succeeded) |
| **Output** | Audit context JSON via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS Dose` |
| `p_pipeline_name` | `pl_dose` |
| `p_pipeline_path` | `/pipelines/pl_dose` |
| `p_triggered_by` | `Fabric` |

---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Src_to_Brz` |
| **Activity Type** | Invoke Pipeline |
| **Child Pipeline** | `pl_dose_src_brz` |
| **Purpose** | Parallel Dose + DoseExcuse extraction to Bronze for all active sites. |
| **Execution Sequence** | 4 |
| **Dependencies** | `control_audit_dose` (Succeeded) |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 15) |
| `p_work_date` | Business work date (passed to child — used in Dose Copy WHERE) |
| `p_sites` | `@activity('fliter_Active_Sitecodes').output.value` |
| `p_audit_context_json` | Audit start notebook exit |
| `waitOnCompletion` | `true` |

---

#### Activity 5: Capture Bronze Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_bronze_method_results_from_child` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores child `pipelineReturnValue` (`v_bronze_method_results_json`) in parent variable for Silver notebooks and audit. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Src_to_Brz` (**Completed** — not only Succeeded) |

---

#### Activity 6: Silver Merge (Two Parallel Notebooks on Parent)

| Field | Value |
|-------|-------|
| **Activity Name** | `doses_excuse_bronze_to_silver` / `dose_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Purpose** | Bronze → Silver Delta MERGE per method. |
| **Execution Sequence** | 6 (parallel) |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |

| Notebook | Object ID | Silver Target |
|----------|-----------|---------------|
| `doses_excuse_bronze_to_silver` | `72d50d83-99ab-4d6b-981d-939486313012` | `pats.tbl_dose_excuse` |
| `dose_bronze_to_silver` | `658d5662-6be6-4ef7-b3e2-a68e52c4ecf8` | `pats.tbl_dose` |

**Shared parameters:** `p_ingest_run_id`, `p_bronze_succeeded`, `p_bronze_method_results_json`, `p_sites_json`, `p_taskconfig_json`, `p_method`, `p_bronze_config_id` (7), `p_silver_config_id` (8).

---

#### Activity 7: Aggregate Silver Method Results

| Field | Value |
|-------|-------|
| **Activity Name** | `Set_dose_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | Builds `v_silver_method_result_json` from both Silver notebook exit payloads. |
| **Execution Sequence** | 7 |
| **Dependencies** | Both Silver notebooks (**Succeeded**, **Failed**, or **Skipped**) |

**Pipeline JSON note:** This activity also depends on Gold publish notebooks in the exported definition. For Silver-only operation, wire it to depend on Silver notebook completion only.

---

#### Activity 8: Audit Finalize

| Field | Value |
|-------|-------|
| **Activity Name** | `If Condition1` → `control_audit_dose_Sucess` / `control_audit_dose_Failure` |
| **Activity Type** | IfCondition + Notebook |
| **Notebook Object ID** | `2ba7000b-89f6-4e40-ac7f-7787792e2ee8` |
| **Purpose** | Finalize audit when no FAILED/ERROR/SKIPPED in bronze or silver method JSON. |
| **Execution Sequence** | 8 |

**Success path:** `p_mode = FINALIZE_SUCCESS`, passes `p_bronze_method_results_json` and `p_silver_method_results_json`

**Failure path:** `p_mode = FINALIZE_FAILURE`

**Notification:** `nb_dose_failure_notification` is **Inactive**.

---

### Bronze Child Pipeline (`pl_dose_src_brz`) — Per-Method Pattern

Two **parallel** Filter + ForEach blocks; final SetVariable waits for both ForEach activities to **Complete**.

| Method | Filter | ForEach | Copy | Bronze Table |
|--------|--------|---------|------|--------------|
| `DoseExcuse` | `flt_child_doseexcuse_sites` | `fe_samms_doseexcuse` | `Dose_excuse_src_to_brz` | `Dose.br_tblDoseExcuse` |
| `Dose` | `flt_Child_dose_Sites` | `fe_samms_dose` | `Dose_src_to_brz` | `Dose.br_tblDose` |

**ForEach settings:** `isSequential: false`, `batchCount: 10`

**Child return:** `set_child_bronze_method_result` sets `pipelineReturnValue.v_bronze_method_results_json` with per-method SUCCESS/FAILED status.

---

## 4. Source Details

| Field | Value |
|-------|-------|
| **Source System** | SAMMS On-Premises SQL Server (per clinic) |
| **Connection ID** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Active Sites** | TaskConfig ConfigId 7 — one row per site per method |
| **Data Format** | Tabular SQL — IF EXISTS gated SELECT per site |

### Source Gate Tables

| Method | Table Check | Behavior when missing |
|--------|-------------|----------------------|
| `DoseExcuse` | `dbo.tblDOSE_Excuse` | Copy returns no rows (IF EXISTS wrapper) |
| `Dose` | `dbo.tblDOSE` | Copy returns no rows (IF EXISTS wrapper) |

### Load Strategy — DoseExcuse

| Setting | Value |
|---------|-------|
| **Load Type** | Full extract per run (no date filter in Copy SQL) |
| **RowChkSum** | CHECKSUM over `ExId`, `CltID`, `DtEx`, `Dtstamp`, `StrUser` |

### Load Strategy — Dose

| Setting | Value |
|---------|-------|
| **Load Type** | Incremental by business work date |
| **Date floor** | `dtMedDate >= 1/1/2020` |
| **Work date filter** | `dtMedDate <= p_work_date` OR `dtDate <= p_work_date` |
| **Metadata** | `SourceQueryStartDate` from `p_lookback_days` (metadata only — not applied in WHERE) |
| **RowChkSum** | CHECKSUM over core dose business columns (excludes `Dosesig`, `DoseSigImg` from checksum list) |

### Dose Source Columns (core)

`DoseId`, `CltId`, `DtMedDate`, `GuestId`, `DtDate`, `Dose`, `StrUser`, `BlVoid`, `StrVoidReason`, `BlException`, `Bottletype`, `Ordernum`, `ExceptionReason`, `BlBulk`, `BlPrepack`, `Dtgiven`, `Dtprep`, `DtVoid`, `Ppstaff`, `Exceptiontype`, `Manualauthdtm`, `Manualauthuser`, `Dosenote`, `Dosesig`, `InventoryGroup`, `SiteId`, `DoseSigImg`

### DoseExcuse Source Columns

`ExId`, `CltID`, `DtEx`, `Dtstamp`, `StrUser`, `StrExcused`

---

## 5. Destination Details

| Field | Value |
|-------|-------|
| **Bronze Lakehouse** | `bhg_bronze` (Artifact ID `77d24027-6a1c-43a8-a998-1a14dd3c0d52`) |
| **Silver Lakehouse (final)** | `bhg_silver` (Artifact ID `dd09d8b6-d862-4954-a0b2-fcf7372c6595`) |
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Bronze Destination

| Schema | Table | Write Mode |
|--------|-------|------------|
| `Dose` | `br_tblDose` | Append (`IngestRunId` metadata) |
| `Dose` | `br_tblDoseExcuse` | Append (`IngestRunId` metadata) |

### Silver Destination (Final Layer)

| Method | Schema | Table | Write Mode | Merge Key |
|--------|--------|-------|------------|-----------|
| `DoseExcuse` | `pats` | `tbl_dose_excuse` | Delta MERGE | `SiteCode` + `ExID` |
| `Dose` | `pats` | `tbl_dose` | Delta MERGE | `RowChkSum` (notebook config) |

### Silver-Derived Columns (DoseExcuse)

| Column | Rule |
|--------|------|
| `LastModAt` | Current timestamp (Asia/Kolkata formatted) |
| `RowState` | `1` when `cltID = -111` or `cltID > 0`; else `0` |
| Pre-merge reset | All Silver rows `RowState = 0` before MERGE |

### Bronze Metadata

| Column | Purpose |
|--------|---------|
| `SiteCode` | Clinic site code |
| `SourceDatabase` | Extraction audit |
| `IngestRunId` | Run correlation |
| `ExtractedAt` | Extraction timestamp |
| `SourceQueryStartDate` | Dose only — lookback metadata |

---

## 6. Notebook Documentation

### `nb_get_taskconfigs`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig for ConfigIds 7 and 8 |
| **Output** | Slim JSON array via notebook exit |

---

### `control_audit_dose` / `_Sucess` / `_Failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `2ba7000b-89f6-4e40-ac7f-7787792e2ee8` |
| **Purpose** | Audit lifecycle for Dose (BR + SL layers) |
| **Modes** | `START_LAYER_RUNS`, `FINALIZE_SUCCESS`, `FINALIZE_FAILURE` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

Supports per-method bronze/silver result JSON for partial finalize (same pattern as Notes).

---

### `doses_excuse_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `72d50d83-99ab-4d6b-981d-939486313012` |
| **Input** | `Dose.br_tblDoseExcuse` |
| **Output** | `pats.tbl_dose_excuse` |

**Processing steps:**

1. Read full Bronze table; map columns (`ExId` → `ExID`, etc.)
2. Derive `LastModAt`, `RowState`
3. Dedupe on `SiteCode` + `ExID`
4. If Silver missing → overwrite initial load
5. Else → **RowState pre-reset** (`UPDATE SET RowState = 0` all rows)
6. Delta MERGE: matched update when `RowChkSum` differs; insert new keys
7. Exit JSON with method status and row counts

---

### `dose_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `658d5662-6be6-4ef7-b3e2-a68e52c4ecf8` |
| **Input** | `Dose.br_tblDose` |
| **Output** | `pats.tbl_dose` |

**Processing steps:**

1. Read full Bronze table
2. If Silver missing → overwrite initial load
3. Else → Delta MERGE on `RowChkSum` key with `whenMatchedUpdate` only when checksum differs
4. Exit JSON with method status and row counts

---

## 7. Copy Activity Documentation

### `Dose_excuse_src_to_brz`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblDOSE_Excuse` — IF EXISTS gated full SELECT |
| **Destination** | `bhg_bronze.Dose.br_tblDoseExcuse` |
| **Write Mode** | Append |
| **Timeout** | `0.12:00:00` |
| **Retry** | 0 |

---

### `Dose_src_to_brz`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblDOSE` — IF EXISTS + work-date WHERE |
| **Destination** | `bhg_bronze.Dose.br_tblDose` |
| **Write Mode** | Append |
| **Incremental Logic** | `dtMedDate >= 2020-01-01` AND (`dtMedDate` or `dtDate` <= `p_work_date`) |
| **Timeout** | `0.12:00:00` |
| **Retry** | 0 |

---

## 8. PySpark Transformations

### DoseExcuse (Bronze → Silver)

- Column alias normalization (`ExId` → `ExID`, `StrExcused` → `strEXCUSED`)
- `LastModAt` timestamp derivation
- `RowState` from `cltID` (-111 and positive clients active)
- Dedupe on `SiteCode` + `ExID`
- Unconditional RowState pre-reset before merge
- RowChkSum-gated update on match

### Dose (Bronze → Silver)

- Full Bronze read (not filtered to current ingest run in notebook)
- MERGE match key configured as `RowChkSum` in notebook
- Update all columns when checksum differs; insert when not matched

### Performance

- Two parallel Bronze ForEach loops (`batchCount = 10`)
- Two parallel Silver notebooks on parent
- Per-method failure isolation via method result JSON

---

## 9. Parameters and Variables

### Parent Pipeline Parameters

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_lookback_days` | int | 15 | Metadata on Dose Bronze Copy |
| `p_ingest_run_id` | string | `manual-run` | Run tagging |
| `p_sites` | array | [] | Normally from filter |

### Parent Pipeline Variables

| Variable | Set by | Consumed by |
|----------|--------|-------------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver notebooks, audit finalize |
| `v_silver_method_result_json` | `Set_dose_method_results` | IfCondition audit finalize |

### Bronze Child Parameters

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_sites` | array | Site list split by method filter |
| `p_ingest_run_id` | string | Bronze metadata |
| `p_lookback_days` | int | Dose SourceQueryStartDate |
| `p_work_date` | string | Dose Copy WHERE ceiling |
| `p_audit_context_json` | string | Audit correlation |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 7 | BR | Bronze extraction (per site x method) |
| 8 | SL | Silver merge (one task per method) |

Audit prefix: **`SAMMS Dose`**.

Task names in filter: `Bronze Dose`, `Bronze DoseExcuse`.

---

## 10. Dependencies

### Activity Execution Order (Parent — Silver Terminal)

```
nb_get_taskconfigs
  -> fliter_Active_Sitecodes
  -> control_audit_dose
  -> Src_to_Brz (pl_dose_src_brz)
  -> set_bronze_method_results_from_child
  -> doses_excuse_bronze_to_silver  } parallel
  -> dose_bronze_to_silver           }
  -> Set_dose_method_results
  -> If Condition1 -> control_audit_dose_Sucess / _Failure
```

### External Dependencies

| Dependency | Requirement |
|------------|-------------|
| On-premises gateway | SAMMS SQL Server reachable per clinic |
| Fabric lakehouses | `bhg_bronze`, `bhg_silver` online |
| `meta.taskconfig` | Active ConfigId 7 rows for both methods |
| `meta.etlconfig` | Active rows for `SAMMS Dose%` prefix |

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| Source table missing | IF EXISTS → zero rows; ForEach still succeeds |
| One method Bronze fails | Method JSON marks FAILED; Silver may skip that method |
| Either method FAILED/SKIPPED in JSON | Audit finalize failure path |
| Both methods SUCCESS | Audit finalize success |

### Inactive / Out-of-Scope Activities

| Activity | State | Notes |
|----------|-------|-------|
| `doseExcuse_silver_to_gold` | Active in JSON | Gold — out of doc scope |
| `dose_silver_to_gold` | Active in JSON | Gold — out of doc scope |
| `nb_dose_failure_notification` | Inactive | Teams notification not active |

---

## 11. Validation

### Source Validation

- IF EXISTS table gate in Copy SQL
- TaskConfig requires `SiteCode` and `DataBaseName` for Bronze filter

### Row Count Validation

- Audit `DataQuality` per method after finalize
- Silver notebook exit JSON includes `rows_read`

### Business Validations

| Method | Validation |
|--------|------------|
| DoseExcuse | Merge key `SiteCode` + `ExID`; RowState pre-reset then re-activate matches |
| DoseExcuse | `cltID = -111` treated as active (legacy rule) |
| Dose | RowChkSum gate on update |
| Dose | Work-date ceiling on source extract |

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Other sites continue in ForEach |
| Missing dose table | Zero rows for site | IF EXISTS — no error |
| Bronze method ForEach fails | Method JSON = FAILED | Silver may skip; audit partial finalize |
| Silver MERGE failure | Method JSON = FAILED | Audit finalize failure |
| Child return missing | Synthetic FAILED JSON in SetVariable fallback | |

### Retry Logic

- Copy activities: retry = 0
- Notebook activities: retry = 0 (default)

### Recovery Steps

1. Query `meta.taskaudit` for failed site/method.
2. Fix gateway, TaskConfig, or clinic connectivity.
3. Adjust `p_work_date` if Dose extract window wrong.
4. Re-run pipeline with new `RunId`.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric run history — parent and Bronze child
- `meta.pipelinerun` — BR and SL layer status per method

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%Dose%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%Dose%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (7, 8)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| No sites in bronze | ConfigId 7 active? Filter TaskName Bronze Dose/DoseExcuse? |
| DoseExcuse empty | `tblDOSE_Excuse` missing at clinic |
| Dose row count low | `p_work_date` ceiling; `dtMedDate` / `dtDate` filter |
| One method FAILED | `v_bronze_method_results_json` per-method status |
| Silver SKIPPED | Bronze method not SUCCESS |
| RowState all zero post-run | DoseExcuse pre-reset — verify merge matched rows |
| Audit partial success | Per-method JSON in finalize notebooks |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **TaskConfig active** | ConfigId 7 rows for `Bronze Dose` and `Bronze DoseExcuse` |
| **Work date** | `p_work_date` set correctly on child invoke |
| **ETL config** | Active `SAMMS Dose%` rows in `meta.etlconfig` |
| **Gateway capacity** | Active sites x 2 methods x batchCount 10 |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline status** | Fabric monitor Succeeded through Silver |
| **Bronze rows** | Counts in `Dose.br_tblDose` and `Dose.br_tblDoseExcuse` |
| **Silver merge** | `pats.tbl_dose` and `pats.tbl_dose_excuse` |
| **Method JSON** | Both methods SUCCESS in bronze and silver result variables |
| **Audit tables** | TaskQueue, TaskAudit, DataQuality populated |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%Dose%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (7, 8)
  AND PipelineRunId = '<pipeline_run_id>';

-- Bronze Dose row count by site
SELECT SiteCode, COUNT(*) AS row_count
FROM Dose.br_tblDose
GROUP BY SiteCode
ORDER BY SiteCode;

-- Bronze DoseExcuse row count by site
SELECT SiteCode, COUNT(*) AS row_count
FROM Dose.br_tblDoseExcuse
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_dose` parent canvas**

*[Insert screenshot]*

**2. Bronze child — parallel Dose and DoseExcuse ForEach loops**

*[Insert screenshot]*

**3. TaskConfig notebook — `nb_get_taskconfigs` parameters**

*[Insert screenshot]*

**4. Audit start — `control_audit_dose`**

*[Insert screenshot]*

**5. Copy activity — `Dose_src_to_brz` source SQL**

*[Insert screenshot]*

**6. Copy activity — `Dose_excuse_src_to_brz` source SQL**

*[Insert screenshot]*

**7. Silver notebook — `doses_excuse_bronze_to_silver` (RowState pre-reset + MERGE)**

*[Insert screenshot]*

**8. Silver notebook — `dose_bronze_to_silver`**

*[Insert screenshot]*

**9. SetVariable — `set_bronze_method_results_from_child`**

*[Insert screenshot]*

**10. IfCondition audit finalize — `If Condition1`**

*[Insert screenshot]*

**11. Bronze tables — `br_tblDose` / `br_tblDoseExcuse` sample rows**

*[Insert screenshot]*

**12. Validation — TaskQueue / DataQuality query output**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS Dose ETL | v1.0*
