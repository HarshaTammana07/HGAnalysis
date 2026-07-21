# SAMMS Form Answer Signatures ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_execute_pipeline_answersignature` |
| **Parent Pipeline Object ID** | `3d7e0bbc-a3b8-40ae-b933-fa9188990c94` |
| **Bronze Child Pipeline** | `pl_answersig_samms_to_lakehouse` (`cf9e89d0-9574-439e-85b9-c022327ed022`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 13/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS Form Answer Signature date records from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — one row per form instance with nine role-based signature dates is published to the Silver lakehouse for downstream reporting.

**Single method processed:**

| Method | Description | Silver Target |
|--------|-------------|---------------|
| `FormAnswerSignatures` | Form signature dates per role (patient, counselor, doctor, etc.) | `bhg_silver.pats.sl_tblFormAnswerSignatures` |

**Legacy context:** Samms-Forms (`BHGTaskRunner.exe 6`); replaces legacy `SaveAnswerSignatures` / `SaveFormQAData` path for signature dates.

**Important design notes:**

- Bronze uses a **dynamic SQL builder** (`nb_answersig_build_site_sql`) — not a fixed Copy query.
- Source gate table is **`answersignature`** (not `Form` alone).
- Form catalog read from **`bhg_silver.ctrl.Forms2Process`** at runtime.
- Silver merge runs as **one notebook on the parent pipeline** (`nb_answersig_bronze_to_silver`).
- **4-column merge key:** `SiteCode` + `FormName` + `FormId` + `ClientId`.

---

## 2. Solution Overview

### Business Objective

Extract when each clinical form was signed by each role (completed-by, counselor, doctor, medical provider, patient, provider, requestor, staff, supervisor) from SAMMS, normalize to one row per form instance, and publish signature date columns to Fabric Silver while preserving legacy RowState pre-reset and Forms2Process-driven custom form UNION logic.

### End-to-End Data Flow

1. **Extract** per-clinic dynamic UNION SQL via SQL builder notebook + Copy Data (Bronze child — one ForEach over ~115 sites).
2. **Transform and merge** Bronze into Silver using one PySpark notebook on the parent: RowState pre-pass + Delta MERGE.
3. **Audit** — pipeline run, task queue, and data quality written to control tables.

### Source Systems

- On-premises SAMMS SQL Server databases (one per clinic, ~115 active sites).
- Base `Form` / `FormTemplate` / `AnswerSignature` model plus custom form tables from Forms2Process (`tblORDERREQ`, `tblTP17REVIEW`, default cases).
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `Forms` (`br_tblFormAnswerSig`).
- **Silver (final):** `bhg_silver` Lakehouse — `pats.sl_tblFormAnswerSignatures`.
- **Control dependency:** `bhg_silver.ctrl.Forms2Process`.

### Overall Architecture Diagram

```
pl_execute_pipeline_answersignature (PARENT)
|
+- nb_get_FormAnswerSignatures_taskconfig
+- flt_active_formanswersignatures_sites
+- nb_formanswersig_audit_start
|
+- Invoke_legacy_Executed_AfterBronz -> pl_answersig_samms_to_lakehouse (BRONZE CHILD)
|     +- fe_each_samms_sites (batchCount 5)
|           +- lkp_check_answersig_table
|           +- lkp_get_existing_tables / lkp_get_existing_columns
|           +- nb_answersig_build_site_sql
|           +- cp_answersig_to_bronze
|
+- nb_answersig_bronze_to_silver (Silver notebook on parent)
+- nb_formanswersig_audit_finalize_success / _failure
```

**Operational scope:** This documentation covers **Bronze and Silver only**. Silver is the terminal layer for consumers.

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_execute_pipeline_answersignature`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_FormAnswerSignatures_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for FormAnswerSignatures and returns slim JSON. Replaces inactive Lookup `lkp_formanswersignatures_taskconfig`. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

#### Activity 2: Filter Active Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `flt_active_formanswersignatures_sites` |
| **Activity Type** | Filter |
| **Purpose** | Keeps active Bronze ConfigId **31** rows where `IsActive = 1` and `SiteCode` + `DataBaseName` are populated. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_FormAnswerSignatures_taskconfig` (Succeeded) |
| **Input** | TaskConfig notebook exit JSON |
| **Output** | Filtered site list for Bronze child |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_formanswersig_audit_start` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` |
| **Purpose** | Initiates audit — creates `PipelineRun` and `TaskQueue` rows for Bronze (~115 site tasks) and Silver (1 task). |
| **Execution Sequence** | 3 |
| **Dependencies** | `flt_active_formanswersignatures_sites` (Succeeded) |
| **Output** | Audit context JSON via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS FormAnswerSignatures` |
| `p_pipeline_name` | `Execute_Pipeline_AnswerSignature` |
| `p_pipeline_path` | `/pipelines/Execute_Pipeline_AnswerSignature` |
| `p_triggered_by` | `Fabric` |

---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Invoke_legacy_Executed_AfterBronz` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_answersig_samms_to_lakehouse` |
| **Purpose** | Per-site dynamic SQL build + Copy to Bronze for all active clinics. |
| **Execution Sequence** | 4 |
| **Dependencies** | `nb_formanswersig_audit_start` (Succeeded) |
| **Output** | Bronze child completion status |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 30) |
| `p_sites` | `@activity('flt_active_formanswersignatures_sites').output.value` |
| `p_reload` | `@pipeline().parameters.p_reload` |
| `p_audit_context_json` | Audit start notebook exit |
| `waitOnCompletion` | `true` |

---

#### Activity 5: Silver Merge (on Parent)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_answersig_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `4cb06dc3-4954-4119-85aa-9e590ac3dc4c` |
| **Purpose** | RowState pre-pass + Delta MERGE from Bronze into final Silver table. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Invoke_legacy_Executed_AfterBronz` (Succeeded) |
| **Input** | `Forms.br_tblFormAnswerSig`, `ctrl.Forms2Process` |
| **Output** | Silver table updated; notebook exit with processing counts |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | Default 30 |
| `p_reload` | `false` (true -> lookback 2010-01-01 for pre-pass) |

**Note:** A copy of this notebook exists inside the Bronze child pipeline but is **Inactive**. Silver runs only on the parent.

---

#### Activity 6: Audit Finalize

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_formanswersig_audit_finalize_success` / `nb_formanswersig_audit_finalize_failure` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` |
| **Purpose** | Finalize audit after Silver completes — marks tasks SUCCESS or FAILED; writes DataQuality. |
| **Execution Sequence** | 6 |
| **Dependencies** | Silver notebook outcome |

**Success path:** `p_mode = FINALIZE_SUCCESS`, `p_status = SUCCESS`

**Failure path:** `p_mode = FINALIZE_FAILURE`, `p_status = FAILED`

**Notification:** `nb_notify_success` and `nb_notify_failed` exist in the pipeline definition but are **Inactive**.

---

### Bronze Child Pipeline (`pl_answersig_samms_to_lakehouse`) — Per-Site Pattern

Single **ForEach** (`fe_each_samms_sites`, `batchCount = 5`).

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `lkp_check_answersig_table` | Lookup | Verify clinic has `answersignature` table |
| 2 | `if_answersig_table_exists` | IfCondition | Gate downstream when table exists |
| 3 | `lkp_get_existing_tables` | Lookup | List all tables in clinic database |
| 4 | `lkp_get_existing_columns` | Lookup | List columns for optional tables |
| 5 | `nb_answersig_build_site_sql` | Notebook | Build UNION ALL SQL from Forms2Process (retry = 3) |
| 6 | `cp_answersig_to_bronze` | Copy | Execute generated SQL -> Append to `Forms.br_tblFormAnswerSig` |

**Inactive in Bronze child:** `sv_set_answersig_sql` (SetVariable), `nb_answersig_bronze_to_silver` (duplicate Silver notebook).

---

## 4. Source Details

| Field | Value |
|-------|-------|
| **Source System** | SAMMS On-Premises SQL Server (per clinic) |
| **Connection (high level)** | Fabric linked service via on-premises data gateway |
| **Connection ID** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Active Sites** | ~115 clinic databases |
| **Data Format** | Tabular SQL — dynamic UNION ALL per site |

### Source Gate Table

| Check | Behavior |
|-------|----------|
| `answersignature` table | Required — site skipped if `sys.tables` probe returns 0 |

### Source Objects (per clinic)

| Object Type | Examples | Notes |
|-------------|----------|-------|
| Gate table | `AnswerSignature` | Must exist or site is skipped |
| Base model | `Form`, `FormTemplate`, `SF_PatientPreAdmission`, `SF_DataForms` | UNION 1 (active forms) + UNION 2 (deleted forms) |
| Signature subquery | `AnswerSignature` | Nine `DateField` values per role |
| Custom forms | `tblORDERREQ`, `tblTP17REVIEW`, Forms2Process default | Only if table exists at clinic |

### Load Strategy

| Setting | Value |
|---------|-------|
| **Load Type** | Incremental metadata — default **30-day** lookback (`p_lookback_days`) for Forms2Process custom UNIONs and Silver pre-pass |
| **Full Reload** | `p_reload = true` -> lookback date **2010-01-01** |
| **Base Form UNION** | Legacy behavior — base active-form query has **no date filter** (pulls all forms) |
| **Custom form UNIONs** | Respect `DateFilterEnabled` in Forms2Process vs lookback date |
| **SQL Generation** | Per site via `nb_answersig_build_site_sql` |

### Nine Signature Date Columns (source)

| Column | AnswerSignature DateField |
|--------|---------------------------|
| `CompletedBySignatureSignatureDate` | `CompletedBySignatureSignatureDate` |
| `CounselorSignatureSignatureDate` | `CounselorSignatureSignatureDate` or `CounselorSignatureDate` |
| `DoctorSignatureSignatureDate` | `DoctorSignatureSignatureDate` |
| `MedicalProviderSignatureSignatureDate` | `MedicalProviderSignatureSignatureDate` |
| `PatientSignatureDate` | `PatientSignatureDate` |
| `ProviderSignatureSignatureDate` | `ProviderSignatureSignatureDate` |
| `RequestorSignatureDate` | `RequestorSignatureDate` |
| `StaffSignatureDate` | `StaffSignatureDate` |
| `SupervisorSignatureSignatureDate` | `SupervisorSignatureSignatureDate` |

**Sentinel rule:** If `Sign IS NULL` in `AnswerSignature` subquery -> return `1/1/1900` (form exists, not signed).

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
| `Forms` | `br_tblFormAnswerSig` | Append (tagged by `_ingest_run_id`) |

### Silver Destination (Final Layer)

| Schema | Table | Write Mode | Merge Key |
|--------|-------|------------|-----------|
| `pats` | `sl_tblFormAnswerSignatures` | Delta MERGE | 4-column key (see below) |

### Silver MERGE Key (4 columns)

`SiteCode` + `FormName` + `FormId` + `ClientId`

(`FormId` normalized to UPPER case; `ClientId` stored as absolute value — negative signal carried in `RowState`.)

### Silver Final Columns

`SiteCode`, `FormName`, `FormId`, `ClientId`, `CreatedOn`, `UpdatedOn`, `CompletedBySignatureSignatureDate`, `CounselorSignatureSignatureDate`, `DoctorSignatureSignatureDate`, `MedicalProviderSignatureSignatureDate`, `PatientSignatureDate`, `ProviderSignatureSignatureDate`, `RequestorSignatureDate`, `StaffSignatureDate`, `SupervisorSignatureSignatureDate`, `RowChkSum`, `RowState`, `LastModAt`

### Bronze Metadata (not carried to Silver)

| Column | Purpose |
|--------|---------|
| `_site_code` | Maps to `SiteCode` |
| `_source_database` | Extraction audit |
| `_ingest_run_id` | Run filter |
| `_extracted_at` | Within-run deduplication |
| `_lookback_date` | SQL builder / pre-pass context |

---

## 6. Notebook Documentation

### `nb_get_FormAnswerSignatures_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig for ConfigIds 31 and 32; returns slim JSON |
| **Input** | `meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

### `nb_formanswersig_audit_start` / `_finalize_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` |
| **Purpose** | Audit lifecycle for FormAnswerSignatures |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_audit_context_json`, `p_ingest_run_id`, `p_sites_json`, `p_status`, `p_error_message` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

---

### `nb_answersig_build_site_sql` (Bronze child)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `673e26ce-cd7a-4b79-83d3-5b5cf7a76c79` |
| **Purpose** | Generates per-site UNION ALL SQL from Forms2Process |
| **Parameters** | `p_site_code`, `p_source_database`, `p_ingest_run_id`, `p_lookback_days`, `p_reload`, `p_existing_tables_json`, `p_existing_columns_json` |
| **Input** | `bhg_silver.ctrl.Forms2Process`, clinic metadata lookups |
| **Output** | SQL string via notebook exit -> Copy activity |
| **Business Logic** | UNION 1 (active forms) + UNION 2 (deleted) + custom form blocks; compound `IsDeleted`; no DISTINCT wrapper (legacy parity) |
| **Retry** | 3 (notebook policy) |

**Custom form switch cases:** `tblORDERREQ`, `tblTP17REVIEW`, default Forms2Process entries.

---

### `nb_answersig_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `4cb06dc3-4954-4119-85aa-9e590ac3dc4c` |
| **Purpose** | Bronze -> Silver: pre-pass RowState reset + Delta MERGE |
| **Parameters** | `p_ingest_run_id`, `p_lookback_days`, `p_reload` |
| **Input Tables** | `Forms.br_tblFormAnswerSig`, `ctrl.Forms2Process` |
| **Output Table** | `pats.sl_tblFormAnswerSignatures` (final) |
| **Merge/Upsert Logic** | 4-key MERGE; full update on match (RowChkSum gate not applied — legacy parity) |

**Transformation highlights:**

- Deduplicate within run on 4-column key (latest `_extracted_at`)
- `FormId` -> UPPER; `ClientId` -> `Math.Abs` (negative original -> `RowState = 0`)
- RowState pre-pass per Forms2Process date-filter rules
- `IsDeleted` or negative source `ClientId` -> `RowState = 0`
- **LAB site rule:** `StaffSignatureDate` set to null (legacy hardcoded skip)
- `LastModAt` set to current timestamp on merge

---

## 7. Copy Activity Documentation

| Field | Value |
|-------|-------|
| **Activity Name** | `cp_answersig_to_bronze` |
| **Source** | SAMMS SQL Server — dynamic SQL from `nb_answersig_build_site_sql` |
| **Destination** | `bhg_bronze.Forms.br_tblFormAnswerSig` |
| **Mapping** | Generated UNION ALL query |
| **Partitioning** | N/A |
| **Incremental Logic** | Custom form UNIONs use lookback; base Form UNION has no date filter |
| **Retry Configuration** | 0 (Copy default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

---

## 8. PySpark Transformations

### Data Cleansing (Bronze -> Silver)

- Filter Bronze to current `_ingest_run_id`.
- Deduplicate on 4-column business key within run.
- Normalize `FormId` to UPPER case.
- Store `ClientId` as absolute value.

### Business Rules Implemented (Silver)

| Rule | Description |
|------|-------------|
| 4-column merge key | `SiteCode` + `FormName` + `FormId` + `ClientId` |
| RowState pre-pass | Unconditional reset for non-date-filtered forms; date-gated for others |
| Full update on match | No RowChkSum gate — always refresh signature dates (legacy parity) |
| Compound IsDeleted | From Form + pre-admission + data-form joins in source SQL |
| LAB site | `StaffSignatureDate` forced null |
| Sentinel dates | `1/1/1900` from source means unsigned role |
| Reload mode | `p_reload` resets pre-pass window to 2010-01-01 |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **Pre-pass UPDATE** | RowState reset for sites/forms in current run |
| **MERGE — Matched** | Full column update including all 9 signature dates |
| **MERGE — Not Matched** | INSERT new 4-key combination |

### Performance Optimizations

- Single ForEach with `batchCount = 5` limits gateway concurrency.
- SQL builder skips missing tables/columns per clinic.
- One Silver notebook MERGE per run for all sites.

### Error Handling

- Per-site isolation in Bronze ForEach.
- Site skipped when `answersignature` table absent.
- SQL builder retry = 3 on transient failures.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | `manual-run` | Tags Bronze rows; filters Silver |
| `p_lookback_days` | int | 30 | Pre-pass and custom form date filters |
| `p_reload` | bool | false | When true, lookback 2010-01-01 |
| `p_sites` | array | [] | Normally from Filter, not manual |
| `p_audit_context_json` | string | (from audit start) | Passed to Bronze child |

### Bronze Child Parameters

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Bronze metadata |
| `p_lookback_days` | int | SQL builder |
| `p_sites` | array | ForEach site list |
| `p_reload` | bool | Full reload flag |
| `p_audit_context_json` | string | Audit correlation |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 31 | BR | Bronze extraction |
| 32 | SL | Silver merge |

Audit prefix: **`SAMMS FormAnswerSignatures`**.

---

## 10. Dependencies

### Activity Execution Order (Parent — Silver Terminal)

```
nb_get_FormAnswerSignatures_taskconfig
  -> flt_active_formanswersignatures_sites
  -> nb_formanswersig_audit_start
  -> Invoke_legacy_Executed_AfterBronz (pl_answersig_samms_to_lakehouse)
  -> nb_answersig_bronze_to_silver
  -> nb_formanswersig_audit_finalize_success / _failure
```

### External Dependencies

| Dependency | Requirement |
|------------|-------------|
| `bhg_silver.ctrl.Forms2Process` | Must be current before Bronze runs |
| On-premises gateway | SAMMS SQL Server reachable |
| Fabric lakehouses | `bhg_bronze`, `bhg_silver` online |

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| No `answersignature` table | Site skipped — no Copy |
| Bronze child fails | Silver does not run (depends on Succeeded) |
| Silver fails | Audit finalize failure path |

### Inactive Activities (not in active BR+SL flow)

| Activity | State | Notes |
|----------|-------|-------|
| `lkp_formanswersignatures_taskconfig` | Inactive | Replaced by notebook |
| `nb_answersig_bronze_to_silver` (in child) | Inactive | Silver runs on parent only |
| `sv_set_answersig_sql` | Inactive | Copy reads notebook exit directly |
| `nb_notify_success` / `nb_notify_failed` | Inactive | Notifications not active |

---

## 11. Validation

### Source Validation

- `answersignature` table Lookup gate before extraction.
- SQL builder validates table/column existence per clinic.

### Row Count Validation

- Audit `DataQuality` records Bronze vs Silver counts after finalize.
- Compare Bronze rows for `_ingest_run_id` vs Silver merge counts.

### Business Validations

| Validation | Detail |
|------------|--------|
| Merge key | 4 columns — not 6 like FormQuestionAnswers |
| ClientId | Must be non-null in Bronze; stored as absolute in Silver |
| Signature sentinel | `1/1/1900` = unsigned role |
| LAB site | StaffSignatureDate null |
| Forms2Process | Custom forms only unioned when table exists |

### Data Quality Checks

- `DuplicateCount`, `NullCount` in `meta.dataquality`
- `ValidationStatus` PASS/FAIL per method

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Other sites continue |
| No AnswerSignature table | Site skipped | Expected for some clinics |
| SQL builder failure | Site fails | Retry = 3 on notebook |
| Silver MERGE failure | Pipeline fails at SL | Audit finalize failure |
| Forms2Process stale | Wrong/missing custom forms | Refresh Forms2Process first |

### Retry Logic

- SQL builder notebook: retry = 3.
- Most other activities: retry = 0.

### Recovery Steps

1. Query `meta.taskaudit` for failed site/method detail.
2. Fix gateway, Forms2Process, or clinic-specific SQL issue.
3. Re-run pipeline with new `RunId`.
4. Use Delta time travel on Silver if bad merge confirmed.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric run history — parent and Bronze child activity status.
- `meta.pipelinerun` — BR and SL layer status.

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%FormAnswerSignatures%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%FormAnswerSignatures%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (31, 32)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| Site skipped | `answersignature` table missing at clinic |
| Zero Bronze rows | Base query returns no forms; custom form date filter |
| Silver row count low | Compare per-site Bronze counts |
| Wrong signature dates | AnswerSignature subquery DateField mapping |
| Duplicate Silver rows | 4-key merge — verify FormId UPPER and ClientId abs |
| LAB StaffSignatureDate | Expected null per legacy rule |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Forms2Process current** | `bhg_silver.ctrl.Forms2Process` loaded |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **Parameter validation** | `p_lookback_days` (30), `p_reload` (false for daily) |
| **TaskConfig active** | ConfigId 31 rows active for target sites |
| **Gateway capacity** | ~115 sites, batchCount 5 |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor Succeeded |
| **Bronze completion** | Rows in `Forms.br_tblFormAnswerSig` for ingest run |
| **Silver merge** | Row counts in `pats.sl_tblFormAnswerSignatures` |
| **Signature date sanity** | Spot-check sentinel `1/1/1900` vs real dates |
| **RowState pre-pass** | Verify reset applied for sites in run |
| **Audit tables** | TaskQueue, TaskAudit, DataQuality populated |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%FormAnswerSignatures%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%FormAnswerSignatures%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (31, 32)
  AND PipelineRunId = '<pipeline_run_id>';

-- Bronze row count for run
SELECT _site_code, COUNT(*) AS row_count
FROM Forms.br_tblFormAnswerSig
WHERE _ingest_run_id = '<pipeline_run_id>'
GROUP BY _site_code
ORDER BY _site_code;
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_execute_pipeline_answersignature` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — `fe_each_samms_sites` ForEach canvas**

*[Insert screenshot]*

**3. AnswerSignature table Lookup — `lkp_check_answersig_table`**

*[Insert screenshot]*

**4. SQL builder notebook — `nb_answersig_build_site_sql` activity configuration**

*[Insert screenshot]*

**5. TaskConfig notebook — `nb_get_FormAnswerSignatures_taskconfig`**

*[Insert screenshot]*

**6. Audit start notebook — `nb_formanswersig_audit_start` parameters**

*[Insert screenshot]*

**7. Copy activity — `cp_answersig_to_bronze` dynamic source SQL**

*[Insert screenshot]*

**8. Silver notebook on parent — `nb_answersig_bronze_to_silver`**

*[Insert screenshot]*

**9. Silver notebook — RowState pre-pass and Delta MERGE logic**

*[Insert screenshot]*

**10. Forms2Process control table — `bhg_silver.ctrl.Forms2Process`**

*[Insert screenshot]*

**11. Pipeline monitoring — successful execution**

*[Insert screenshot]*

**12. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS Form Answer Signatures ETL | v1.0*
