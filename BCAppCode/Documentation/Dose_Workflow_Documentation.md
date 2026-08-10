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
| **Last Updated** | 29/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS medication dose and dose-excuse records from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — dose and dose-excuse rows are published to the Silver lakehouse for downstream reporting and analytics.

**Two methods processed:**

| Method | Description | Silver Target |
|--------|-------------|---------------|
| `Dose` | Medication dose administration records | `bhg_silver.pats.tbl_dose` |
| `DoseExcuse` | Dose excuse / exception records | `bhg_silver.pats.tbl_dose_excuse` |

**Legacy context:** SAMMS-ETL-Dose (`BHGTaskRunner.exe 10`); replaces legacy `SaveDoses` / `SaveDoseExcuse` C# services and `BulkDose` staging path for Fabric.

**Important design notes:**

- Bronze child runs **two parallel ForEach loops** — one per method (`batchCount = 10`).
- Silver merge runs as **two parallel notebooks on the parent** (`doses_excuse_bronze_to_silver`, `dose_bronze_to_silver`) — not a separate Silver child pipeline.
- **RowChkSum-gated Delta MERGE** with legacy RowState pre-reset rules on both methods.
- Dose Bronze uses **legacy date-window logic** (not a simple 15-day lookback) — special 1-month vs 6-month rules by site.
- Gold Copy activities exist in the pipeline definition but are **Inactive**. Silver is the terminal layer.

---

## 2. Solution Overview

### Business Objective

Extract medication dose and dose-excuse data from SAMMS, normalize to Silver schema, and merge into Fabric while preserving legacy behavior: RowChkSum change detection, RowState soft-reset, void/negative-client inactive rules, and per-method partial success when one method or site fails.

### End-to-End Data Flow

1. **Extract** dose and dose-excuse data via Copy Data activities (Bronze child — 2 parallel ForEach loops × ~115 sites).
2. **Transform and merge** Bronze into Silver using two parallel PySpark notebooks on the parent.
3. **Audit and notify** — pipeline run, task queue, data quality, and failure alerts written to control tables.

### Source Systems

- On-premises SAMMS SQL Server databases (one per clinic, ~115 active sites).
- Source tables: `dbo.tblDOSE`, `dbo.tblDOSE_Excuse`.
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `Dose` (`br_tblDose`, `br_tblDoseExcuse`).
- **Silver (final):** `bhg_silver` Lakehouse — `pats.tbl_dose`, `pats.tbl_dose_excuse`.

### Overall Architecture Diagram

```
pl_dose (PARENT)
|
+- nb_get_taskconfig
+- fliter_Active_Sitecodes
+- control_audit_dose
|
+- Src_to_Brz1 -> pl_dose_src_brz (BRONZE CHILD)
|     +- flt_child_doseexcuse_sites -> fe_samms_doseexcuse -> Dose_excuse_src_to_brz
|     +- flt_Child_dose_Sites       -> fe_samms_dose       -> Dose_src_to_brz
|     +- set_child_bronze_method_result
|
+- set_bronze_method_results_from_child
+- doses_excuse_bronze_to_silver  } parallel on parent
+- dose_bronze_to_silver          }
+- Set_dose_method_results
+- If Condition1
|     +- TRUE  -> control_audit_dose_Sucess
|     +- FALSE -> control_audit_dose_Failure
+- nb_dose_failure_notification (on IfCondition Failed/Skipped)
```

**Inactive (not in active BR+SL flow):** `dose_sl_to_gl`, `dose_excuse_sl_to_gl` (Gold Copy).

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_dose`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for Dose/DoseExcuse configuration and returns slim JSON. Avoids Fabric Lookup 4 MB limit. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_config_ids_json` | `[7, 8]` |
| `p_methods_json` | `["DoseExcuse", "Dose"]` |
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
| **Purpose** | Keeps active Bronze ConfigId **7** rows where `TaskName` is `Bronze Dose` or `Bronze DoseExcuse`, with populated `SiteCode` and `DataBaseName`. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_taskconfig` (Succeeded) |
| **Input** | `@json(activity('nb_get_taskconfig').output.result.exitValue)` |
| **Output** | Filtered site list for Bronze child |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `control_audit_dose` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `2ba7000b-89f6-4e40-ac7f-7787792e2ee8` |
| **Purpose** | Initiates audit — creates `PipelineRun` and `TaskQueue` rows for Bronze (~230 site tasks) and Silver (2 method tasks). |
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
| **Activity Name** | `Src_to_Brz1` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_dose_src_brz` |
| **Purpose** | Extracts dose and dose-excuse data from SAMMS clinic databases into Bronze tables. Two methods run in parallel ForEach loops. |
| **Execution Sequence** | 4 |
| **Dependencies** | `control_audit_dose` (Succeeded) |
| **Output** | Child `pipelineReturnValue` with per-method Bronze status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_sites` | `@activity('fliter_Active_Sitecodes').output.value` |
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 15) |
| `p_audit_context_json` | Audit start notebook exit |
| `p_work_date` | Current date (Central Standard Time) |
| `waitOnCompletion` | `true` |

---

#### Activity 5: Capture Bronze Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_bronze_method_results_from_child` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Bronze child return JSON in parent variable `v_bronze_method_results_json`. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Src_to_Brz1` (**Completed**) |
| **Output** | Pipeline variable `v_bronze_method_results_json` |

**JSON shape per method:**

- `Dose` / `DoseExcuse`.status`: `SUCCESS` or `FAILED`
- `failed_stage`: `BR` when failed
- `error_message`: detail when failed

---

#### Activity 6: Silver Merge — DoseExcuse (on Parent)

| Field | Value |
|-------|-------|
| **Activity Name** | `doses_excuse_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `72d50d83-99ab-4d6b-981d-939486313012` |
| **Purpose** | RowState pre-reset + RowChkSum-gated Delta MERGE for DoseExcuse. |
| **Execution Sequence** | 6 (parallel with Activity 7) |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |
| **Input** | `Dose.br_tblDoseExcuse`, TaskConfig metadata |
| **Output** | Notebook exit JSON with row counts and per-site results |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_bronze_succeeded` | DoseExcuse Bronze status from `v_bronze_method_results_json` |
| `p_bronze_method_results_json` | `@variables('v_bronze_method_results_json')` |
| `p_sites_json` | Filtered site list |
| `p_taskconfig_json` | TaskConfig notebook exit |
| `p_method` | `DoseExcuse` |
| `p_bronze_config_id` | `7` |
| `p_silver_config_id` | `8` |

---

#### Activity 7: Silver Merge — Dose (on Parent)

| Field | Value |
|-------|-------|
| **Activity Name** | `dose_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `658d5662-6be6-4ef7-b3e2-a68e52c4ecf8` |
| **Purpose** | RowState pre-reset + RowChkSum-gated Delta MERGE for Dose with legacy date-window RowState rules. |
| **Execution Sequence** | 7 (parallel with Activity 6) |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |
| **Input** | `Dose.br_tblDose`, TaskConfig metadata |
| **Output** | Notebook exit JSON with row counts and per-site results |

**Additional parameters vs DoseExcuse:**

| Parameter | Value |
|-----------|-------|
| `p_method` | `Dose` |
| `p_work_date` | Current date (Central Standard Time) |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` |

---

#### Activity 8: Capture Silver Results

| Field | Value |
|-------|-------|
| **Activity Name** | `Set_dose_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | Concatenates both Silver notebook exit JSON into `v_silver_method_result_json`. |
| **Execution Sequence** | 8 |
| **Dependencies** | Both Silver notebooks (Succeeded) |
| **Output** | Pipeline variable `v_silver_method_result_json` |

---

#### Activity 9: Audit Finalize (Conditional)

| Field | Value |
|-------|-------|
| **Activity Name** | `If Condition1` |
| **Activity Type** | IfCondition |
| **Purpose** | Routes to success or failure audit finalize based on bronze and silver method result JSON. |
| **Execution Sequence** | 9 |
| **Dependencies** | `Set_dose_method_results` (Succeeded) |
| **Condition** | Neither `v_bronze_method_results_json` nor `v_silver_method_result_json` contains `FAILED`, `ERROR`, or `SKIPPED` |

**If TRUE — Activity 9a: Success Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `control_audit_dose_Sucess` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `2ba7000b-89f6-4e40-ac7f-7787792e2ee8` |
| **Purpose** | Marks tasks SUCCESS; writes DataQuality rows. |
| **Configuration** | `p_mode = FINALIZE_SUCCESS`, `p_status = SUCCESS` |

**If FALSE — Activity 9b: Failure Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `control_audit_dose_Failure` |
| **Activity Type** | Notebook |
| **Purpose** | Partial finalize — failed methods marked FAILED. |
| **Configuration** | `p_mode = FINALIZE_FAILURE`, `p_status = FAILED` |

---

#### Activity 10: Failure Notification

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_dose_failure_notification` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `77c87686-120d-486b-9146-6a794d794e38` |
| **Purpose** | Sends failure alert with bronze/silver JSON detail when IfCondition fails or is skipped. |
| **Execution Sequence** | 10 (alternative path) |
| **Dependencies** | `If Condition1` (Failed or Skipped) |

---

### Bronze Child Pipeline (`pl_dose_src_brz`) — Two Parallel ForEach Loops

| Method | Filter Activity | ForEach Activity | Copy Activity | Bronze Table |
|--------|----------------|------------------|---------------|--------------|
| `DoseExcuse` | `flt_child_doseexcuse_sites` | `fe_samms_doseexcuse` | `Dose_excuse_src_to_brz` | `Dose.br_tblDoseExcuse` |
| `Dose` | `flt_Child_dose_Sites` | `fe_samms_dose` | `Dose_src_to_brz` | `Dose.br_tblDose` |

#### Bronze Activity Pattern (per method, per site)

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `flt_child_*` | Filter | Split sites by `Method` (`Dose` or `DoseExcuse`) |
| 2 | `fe_samms_*` | ForEach | Iterate sites — `isSequential: false`, `batchCount: 10` |
| 3 | `*_src_to_brz` | Copy | IF EXISTS table check + SELECT → Append to Bronze |

**No separate Lookup/IfCondition:** table existence is embedded in the Copy SQL (`IF EXISTS ... BEGIN ... END`).

#### Bronze Aggregate Activity

| Field | Value |
|-------|-------|
| **Activity Name** | `set_child_bronze_method_result` |
| **Activity Type** | SetVariable |
| **Purpose** | After both ForEach loops **Complete**, builds per-method status JSON on `pipelineReturnValue`. |
| **Dependencies** | `fe_samms_dose` and `fe_samms_doseexcuse` (Completed) |
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
| `DoseExcuse` | `dbo.tblDOSE_Excuse` | Full (when table exists) | Full table SELECT; skipped if table absent |
| `Dose` | `dbo.tblDOSE` | Incremental (legacy window) | Complex date rules — see below |

### Dose Bronze Date-Window Logic (legacy parity)

| Rule | Detail |
|------|--------|
| Year guard | `YEAR(dtDate)` or `YEAR(dtMedDate)` >= year of (`p_work_date` − `p_lookback_days` − 1 year) |
| Upper bound | `dtDate <= p_work_date + 2 days` |
| Client filter | `CltId IS NOT NULL` |
| Special sites (`V10A`, `CBCO`, `V21`, `V10`) | `dtDate >= p_work_date − 1 month` |
| All other sites | `dtDate >= p_work_date − 6 months` |

**Note:** Dose does **not** use a simple 15-day lookback at source — `p_lookback_days` feeds the year-guard and Silver RowState reset date only.

### DoseExcuse Bronze Logic

- Full extract from `tblDOSE_Excuse` when table exists.
- `RowChkSum = CHECKSUM(ExId, CltID, DtEx, Dtstamp, StrUser)`.
- Dummy NULL row appended via `UNION ALL` (legacy Copy pattern).

### Dose Bronze Logic

- `RowChkSum = CHECKSUM(...)` over dose business columns (excludes `Dosesig`, `DoseSigImg` from checksum).
- Dummy NULL row appended via `UNION ALL`.

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
| `Dose` | `br_tblDose` | Append (tagged by `IngestRunId`) |
| `Dose` | `br_tblDoseExcuse` | Append (tagged by `IngestRunId`) |

### Silver Destination (Final Layer)

| Schema | Table | Merge Key | Write Mode |
|--------|-------|-----------|------------|
| `pats` | `tbl_dose` | `SiteCode` + `DoseId` | Delta MERGE |
| `pats` | `tbl_dose_excuse` | `SiteCode` + `ExId` | Delta MERGE |

### Silver Final Columns

**DoseExcuse:** `SiteCode`, `RowChkSum`, `RowState`, `ExId`, `CltID`, `DtEx`, `StrExcused`, `Dtstamp`, `StrUser`, `LastModAt`

**Dose:** `SiteCode`, `RowState`, `RowChkSum`, `LastModAt`, `DoseId`, `CltId`, `DtMedDate`, `GuestId`, `DtDate`, `Dose`, `StrUser`, `BlVoid`, `StrVoidReason`, `BlException`, `Bottletype`, `Ordernum`, `ExceptionReason`, `BlBulk`, `BlPrepack`, `Dtgiven`, `Dtprep`, `DtVoid`, `Ppstaff`, `Exceptiontype`, `Manualauthdtm`, `Manualauthuser`, `Dosenote`, `Dosesig`, `InventoryGroup`, `SiteId`, `DoseSigImg`

### Bronze Metadata (not all carried to Silver)

| Column | Purpose |
|--------|---------|
| `SiteCode` | Clinic identifier |
| `SourceDatabase` | SAMMS database name |
| `IngestRunId` | Run filter |
| `ExtractedAt` | Within-run deduplication |
| `SourceQueryStartDate` | Dose only — query window audit |

---

## 6. Notebook Documentation

### `nb_get_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig for ConfigIds 7 and 8; returns slim JSON |
| **Input** | `meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

### `control_audit_dose` / `_Sucess` / `_Failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `2ba7000b-89f6-4e40-ac7f-7787792e2ee8` |
| **Purpose** | Audit lifecycle for Dose pipeline |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_audit_context_json`, `p_ingest_run_id`, `p_sites_json`, `p_bronze_method_results_json`, `p_silver_method_results_json`, `p_status` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

---

### `doses_excuse_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `72d50d83-99ab-4d6b-981d-939486313012` |
| **Purpose** | Bronze → Silver: site-wide RowState reset + RowChkSum MERGE |
| **Input Tables** | `Dose.br_tblDoseExcuse` |
| **Output Table** | `pats.tbl_dose_excuse` (final) |
| **Merge/Upsert Logic** | `SiteCode` + `ExId`; RowChkSum branches |
| **Error Handling** | SKIPPED when no Bronze rows for ingest run |

**Transformation highlights:**

- Deduplicate on `SiteCode` + `ExId` (latest `ExtractedAt`)
- Pre-pass: reset `RowState = false` for all rows at site
- Matched + checksum changed → full update
- Matched + checksum same → update `RowState = true`, `LastModAt`
- Source rows get `RowState = true`

---

### `dose_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `658d5662-6be6-4ef7-b3e2-a68e52c4ecf8` |
| **Purpose** | Bronze → Silver: date-gated RowState reset + RowChkSum MERGE |
| **Input Tables** | `Dose.br_tblDose` |
| **Output Table** | `pats.tbl_dose` (final) |
| **Merge/Upsert Logic** | `SiteCode` + `DoseId`; RowChkSum branches |
| **Error Handling** | SKIPPED when no Bronze rows; SUCCESS with zero business rows when Bronze succeeded |

**Transformation highlights:**

- Deduplicate on `SiteCode` + `DoseId` (latest `ExtractedAt`)
- Pre-pass: reset `RowState = false` where `DtDate >= rowstate_reset_date` (`p_work_date − p_lookback_days`)
- RowState inactive when `(BlVoid AND DtVoid)` OR `(CltId < 0 AND CltId <> -111)`
- RowChkSum changed → full update; same checksum → RowState/LastModAt only
- Final guard update for void/negative-client rows

---

### `nb_dose_failure_notification`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `77c87686-120d-486b-9146-6a794d794e38` |
| **Purpose** | Failure notification on audit finalize path failure |
| **Parameters** | `Pipeline_Name`, `Status`, `Config_Name`, `Error_Msg`, bronze/silver JSON |

---

## 7. Copy Activity Documentation

| Field | Value |
|-------|-------|
| **Activity Names** | `Dose_excuse_src_to_brz`, `Dose_src_to_brz` |
| **Source** | SAMMS SQL Server — dynamic SQL with IF EXISTS gate |
| **Destination** | `bhg_bronze.Dose.br_tblDoseExcuse` / `br_tblDose` |
| **Mapping** | Auto translator with type conversion |
| **Partitioning** | N/A |
| **Incremental Logic** | Dose: legacy date window; DoseExcuse: full table |
| **Retry Configuration** | 0 (Copy default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

---

## 8. PySpark Transformations

### Data Cleansing (Bronze → Silver)

- Filter Bronze to current `IngestRunId`.
- Determine successful sites from Bronze row presence per `SiteCode`.
- Deduplicate within run on business key + latest `ExtractedAt`.
- TaskConfig-driven table resolution via `resolve_taskconfig_table()`.

### Business Rules Implemented (Silver)

| Rule | Method | Description |
|------|--------|-------------|
| RowChkSum gate | Both | Update full row only when checksum changed |
| RowState pre-reset | Both | Soft-reset before MERGE |
| Date-gated reset | Dose | Reset only rows with `DtDate >= work_date − lookback_days` |
| Site-wide reset | DoseExcuse | Reset all rows for site before MERGE |
| Void inactive | Dose | `BlVoid = true AND DtVoid = true` → `RowState = false` |
| Negative client | Dose | `CltId < 0 AND CltId <> -111` → `RowState = false` |
| Partial success | Both | One method failing does not block the other at Bronze ForEach level |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **Pre-pass UPDATE** | RowState reset before MERGE |
| **MERGE — Matched (checksum changed)** | Full column update |
| **MERGE — Matched (checksum same)** | RowState + LastModAt only |
| **MERGE — Not Matched** | INSERT new key |

### Performance Optimizations

- Two parallel Bronze ForEach loops with `batchCount = 10`.
- Two parallel Silver notebooks on parent.
- IF EXISTS in Copy SQL avoids separate Lookup activities.

### Error Handling

- Per-site isolation in Bronze ForEach.
- Per-method isolation in Silver — SKIPPED when Bronze method failed or zero rows.
- Method JSON propagated to audit finalize and notification.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_lookback_days` | int | 15 | Year guard + Silver RowState reset date |
| `p_ingest_run_id` | string | `manual-run` | Bronze row tag; Silver filter |
| `p_sites` | array | [] | Normally from Filter, not manual |

### Parent Pipeline Variables

| Variable | Set By | Used By |
|----------|--------|---------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver notebooks, audit, notify |
| `v_silver_method_result_json` | `Set_dose_method_results` | Audit finalize, notify |

### Bronze Child Parameters

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_sites` | array | ForEach site list |
| `p_ingest_run_id` | string | Bronze metadata |
| `p_lookback_days` | int | Dose Copy SQL year guard |
| `p_work_date` | string | Dose Copy date window |
| `p_audit_context_json` | string | Audit correlation |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 7 | BR | Bronze extraction |
| 8 | SL | Silver merge |
| 9 | GL | Gold publish — **Inactive** in pipeline |

Audit prefix: **`SAMMS Dose`**.

---

## 10. Dependencies

### Activity Execution Order (Parent — Silver Terminal)

```
nb_get_taskconfig
  -> fliter_Active_Sitecodes
  -> control_audit_dose
  -> Src_to_Brz1 (pl_dose_src_brz)
  -> set_bronze_method_results_from_child
  -> doses_excuse_bronze_to_silver + dose_bronze_to_silver (parallel)
  -> Set_dose_method_results
  -> If Condition1
      -> TRUE  -> control_audit_dose_Sucess
      -> FALSE -> control_audit_dose_Failure
  -> nb_dose_failure_notification (Failed/Skipped)
```

### External Dependencies

| Dependency | Requirement |
|------------|-------------|
| On-premises gateway | SAMMS SQL Server reachable |
| Fabric lakehouses | `bhg_bronze`, `bhg_silver` online |
| TaskConfig | ConfigId 7 Bronze rows active for target sites |

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| `tblDOSE_Excuse` / `tblDOSE` absent | Copy returns no rows for that site |
| Bronze method ForEach fails | Method status FAILED in bronze JSON |
| Silver SKIPPED | No Bronze rows for method in ingest run |
| All methods succeed | Audit finalize success |

### Inactive Activities

| Activity | State | Notes |
|----------|-------|-------|
| `dose_sl_to_gl` | Inactive | Silver → Gold Copy for Dose |
| `dose_excuse_sl_to_gl` | Inactive | Silver → Gold Copy for DoseExcuse |

---

## 11. Validation

### Source Validation

- IF EXISTS gate in Copy SQL before extraction.
- `CltId IS NOT NULL` filter on Dose source.

### Row Count Validation

- Audit `DataQuality` records Bronze vs Silver counts after finalize.
- **Dose:** do not compare full table counts — BHG_DR has years of history; compare same date window.
- **DoseExcuse:** full-table per site at source; BHG_DR may have extra historical inactive rows.

### Business Validations

| Validation | Detail |
|------------|--------|
| Dose merge key | `SiteCode` + `DoseId` |
| DoseExcuse merge key | `SiteCode` + `ExId` |
| RowChkSum gate | Updates only when checksum changed |
| Void rows | `BlVoid AND DtVoid` → inactive |
| Negative client | `CltId < 0 AND CltId <> -111` → inactive |
| Special sites | 1-month vs 6-month window verified |

### Data Quality Checks

- `DuplicateCount`, `NullCount` in `meta.dataquality`
- `ValidationStatus` PASS/FAIL per method
- Test sites: `AHK`, `B42D`, `CBCO`, `HS`, `TTCC` (unit testing guide)

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Other sites continue; method may partial-fail |
| Source table missing | Zero rows for site | IF EXISTS — no error |
| Silver MERGE failure | Method fails at SL | Other method may succeed; audit partial finalize |
| IfCondition string match | Rare false trigger | Error text containing FAILED/SKIPPED tokens |

### Retry Logic

- Pipeline activities: retry = 0 (default).

### Recovery Steps

1. Identify failed method/stage from `v_bronze_method_results_json` or `v_silver_method_result_json`.
2. Query `meta.taskaudit` and `meta.dataquality` for the ingest run ID.
3. Fix root cause (gateway, schema drift, date window).
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
| `meta.taskqueue` | `TaskName LIKE '%Dose%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%Dose%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (7, 8)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| DoseExcuse zero rows | `tblDOSE_Excuse` exists at clinic? |
| Dose row count low | Date window — special site vs 6-month rule |
| Silver SKIPPED | Bronze JSON — method FAILED or zero rows |
| Row count vs BHG_DR mismatch | Dose: compare window logic, not full table |
| Duplicate Silver rows | Verify merge keys (`DoseId` / `ExId`) |
| Notification fired | `nb_dose_failure_notification` on IfCondition failure |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **Parameter validation** | `p_lookback_days` (15), `p_ingest_run_id` (if override) |
| **TaskConfig active** | ConfigId 7 rows active — `Bronze Dose` and `Bronze DoseExcuse` per site |
| **Gateway capacity** | 2 parallel ForEach × batchCount 10 |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor Succeeded (or Failed with expected partial failure) |
| **Bronze completion** | Rows in `Dose.br_tblDose` and `br_tblDoseExcuse` for ingest run |
| **Silver merge** | Row counts in `pats.tbl_dose` and `tbl_dose_excuse` |
| **RowState sanity** | Void and negative-client rows inactive on Dose |
| **Audit tables** | TaskQueue, TaskAudit, DataQuality populated |
| **Per-method JSON** | No unexpected FAILED in result variables |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%Dose%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%Dose%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (7, 8)
  AND PipelineRunId = '<pipeline_run_id>';

-- Bronze row count for Dose
SELECT SiteCode, COUNT(*) AS row_count
FROM Dose.br_tblDose
WHERE IngestRunId = '<pipeline_run_id>'
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_dose` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — parallel ForEach loops**

*[Insert screenshot]*

**3. Dose Copy activity — IF EXISTS + date window SQL**

*[Insert screenshot]*

**4. TaskConfig notebook — `nb_get_taskconfig`**

*[Insert screenshot]*

**5. Audit start notebook — `control_audit_dose` parameters**

*[Insert screenshot]*

**6. Silver notebook — `dose_bronze_to_silver` RowChkSum MERGE**

*[Insert screenshot]*

**7. Silver notebook — `doses_excuse_bronze_to_silver` RowState pre-reset**

*[Insert screenshot]*

**8. IfCondition — audit finalize branches**

*[Insert screenshot]*

**9. Pipeline monitoring — successful execution**

*[Insert screenshot]*

**10. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

**11. Unit test comparison — Fabric Silver vs BHG_DR (test sites)**

*[Insert screenshot]*

**12. Failure notification — `nb_dose_failure_notification` (optional)**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS Dose ETL | v1.0*
