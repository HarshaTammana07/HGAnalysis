# SAMMS Form Question Answers ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_execute_form_questionanswers` |
| **Parent Pipeline Object ID** | `ea25fed4-2f4b-4c1e-9a62-e504364580e2` |
| **Bronze Child Pipeline** | `pl_forms_samms_to_lakehouse` (`d7caff4a-595e-4826-a75e-75919ed5d30f`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 13/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS Form Question-and-Answer data from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — all normalized Q&A rows land in a single Silver table for downstream reporting and analytics.

**Single method processed:**

| Method | Description | Silver Target |
|--------|-------------|---------------|
| `FormQuestionAnswers` | All form Q&A rows per clinic (base Form model + ~75 custom form types) | `bhg_silver.pats.tbl_dbo_FormQuestionAnswers` |

**Legacy context:** Samms-Forms (`BHGTaskRunner.exe 6`); replaces legacy `SaveFormQuestionAnswers` C# service.

**Important design notes:**

- Bronze extraction uses a **dynamic SQL builder** (`nb_forms_build_site_sql`) — not a fixed Copy query per table.
- Form catalog is read from **`bhg_silver.ctrl.Forms2Process`** (not from SAMMS at runtime).
- Silver merge runs as **one notebook on the parent pipeline** (no Silver child pipeline).

---

## 2. Solution Overview

### Business Objective

Extract clinical form question-and-answer records from SAMMS — admission assessments, treatment plans, custom forms, and base Form/Question/Answer data — normalize them to a single schema, and publish to Fabric Silver while preserving legacy behavior: 7-column composite keys, RowState pre-reset, Treatment Plan (`TP-*`) rules, and compound `IsDeleted` logic.

### End-to-End Data Flow

1. **Extract** per-clinic dynamic UNION SQL via SQL builder notebook + Copy Data (Bronze child — one ForEach over ~115 sites).
2. **Transform and merge** Bronze into Silver using one PySpark notebook on the parent: RowState pre-pass + Delta MERGE.
3. **Audit and notify** — pipeline run, task queue, data quality, and failure alerts written to control tables.

### Source Systems

- On-premises SAMMS SQL Server databases (one per clinic, ~115 active sites).
- Base `Form` model + up to ~75 custom form tables per clinic (Forms2Process-driven).
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `Form` (`br_tblFormQA`, `br_formqa_site_success`).
- **Silver (final):** `bhg_silver` Lakehouse — `pats.tbl_dbo_FormQuestionAnswers`.
- **Control dependency:** `bhg_silver.ctrl.Forms2Process` (read by SQL builder and Silver pre-pass).

Gold ConfigId 30 may exist in control tables but is **not executed**. Silver is the terminal layer.

### Overall Architecture Diagram

```
pl_execute_form_questionanswers (PARENT)
|
+- nb_get_FormQuestionAnswers_taskconfig
+- flt_active_formquestionanswers_sites
+- nb_formqa_audit_start
|
+- Invoke_legacy_Execute_AfterBronz -> pl_forms_samms_to_lakehouse (BRONZE CHILD)
|     +- fe_each_samms_sites (batchCount 5)
|           +- lkp_check_form_table
|           +- lkp_get_existing_tables / lkp_get_existing_columns
|           +- nb_forms_build_site_sql
|           +- cp_forms_to_bronze
|           +- mk_formqa_site_success
|
+- set_formqa_bronze_results
+- nb_forms_bronze_to_silver (Silver notebook on parent)
+- set_formqa_silver_results
+- if_formqa_silver_result_success
|     +- TRUE  -> nb_formqa_audit_finalize_clean_success
|     +- FALSE -> nb_formqa_audit_finalize_failure -> nb_notify_failed
```

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_execute_form_questionanswers`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_FormQuestionAnswers_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for FormQA configuration and returns slim JSON for active sites. Avoids Fabric Lookup 4 MB limit. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

#### Activity 2: Filter Active Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `flt_active_formquestionanswers_sites` |
| **Activity Type** | Filter |
| **Purpose** | Keeps active Bronze ConfigId **28** rows where `IsActive = 1` and `SiteCode` + `DataBaseName` are populated. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_FormQuestionAnswers_taskconfig` (Succeeded) |
| **Input** | `@json(activity('nb_get_FormQuestionAnswers_taskconfig').output.result.exitValue)` |
| **Output** | Filtered site list for Bronze child |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_formqa_audit_start` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` |
| **Purpose** | Initiates audit — creates `PipelineRun` and `TaskQueue` rows for Bronze (~115 site tasks) and Silver (1 task). Terminal layer **SL**. |
| **Execution Sequence** | 3 |
| **Dependencies** | `flt_active_formquestionanswers_sites` (Succeeded) |
| **Output** | Audit context JSON via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS FormQuestionAnswers` |
| `p_pipeline_name` | `Execute_Form_QuestionAnswer` |
| `p_pipeline_path` | `/pipelines/Execute_Form_QuestionAnswer` |
| `p_triggered_by` | `Fabric` |
| `p_active_target_layers_json` | `["BR","SL"]` |
| `p_terminal_target_name` | `SL` |

---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Invoke_legacy_Execute_AfterBronz` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_forms_samms_to_lakehouse` |
| **Purpose** | Per-site dynamic SQL build + Copy to Bronze for all active clinics. |
| **Execution Sequence** | 4 |
| **Dependencies** | `nb_formqa_audit_start` (Succeeded) |
| **Output** | Child `pipelineReturnValue` with `FormQuestionAnswers` Bronze status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 30) |
| `p_sites` | `@activity('flt_active_formquestionanswers_sites').output.value` |
| `p_reload` | `@pipeline().parameters.p_reload` |
| `p_audit_context_json` | Audit start notebook exit |
| `waitOnCompletion` | `true` |

---

#### Activity 5: Capture Bronze Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_formqa_bronze_results` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Bronze child return JSON in `v_bronze_method_results_json`. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Invoke_legacy_Execute_AfterBronz` (**Completed**) |
| **Output** | Pipeline variable `v_bronze_method_results_json` |

**JSON shape:**

- `FormQuestionAnswers.status`: `SUCCESS` or `FAILED`
- `failed_stage`: `BR` when failed
- `error_message`: detail when failed

---

#### Activity 6: Silver Merge (on Parent)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_forms_bronze_to_silver` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `564ca691-a179-49f2-ad2f-3a389c1983f4` |
| **Purpose** | RowState pre-pass + Delta MERGE from Bronze into final Silver table. |
| **Execution Sequence** | 6 |
| **Dependencies** | `set_formqa_bronze_results` (Succeeded) |
| **Input** | Bronze `br_tblFormQA`, Forms2Process, site list |
| **Output** | Notebook exit JSON with row counts and per-site results |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | Default 30 |
| `p_reload` | `false` (true -> lookback 2010-01-01) |
| `p_sites_json` | Filtered site list |

---

#### Activity 7: Capture Silver Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_formqa_silver_results` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Silver notebook exit JSON in `v_silver_method_results_json`, or builds FAILED/SKIPPED JSON if notebook did not succeed. |
| **Execution Sequence** | 7 |
| **Dependencies** | `nb_forms_bronze_to_silver` (Succeeded, Failed, or Skipped) |
| **Output** | Pipeline variable `v_silver_method_results_json` |

---

#### Activity 8: Audit Finalize (Conditional)

| Field | Value |
|-------|-------|
| **Activity Name** | `if_formqa_silver_result_success` |
| **Activity Type** | IfCondition |
| **Purpose** | Routes to success or failure audit finalize based on silver result JSON. |
| **Execution Sequence** | 8 |
| **Dependencies** | `set_formqa_silver_results` (Succeeded) |
| **Condition** | Silver JSON does not contain `"status":"FAILED"`, `"status":"SKIPPED"`, or `"status":"ERROR"` |

**If TRUE — Activity 8a: Success Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_formqa_audit_finalize_clean_success` |
| **Activity Type** | Notebook |
| **Purpose** | Marks tasks SUCCESS; writes DataQuality rows. |
| **Configuration** | `p_mode = FINALIZE_SUCCESS`, `p_terminal_target_name = SL` |

**If FALSE — Activity 8b: Failure Audit + Notify**

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_formqa_audit_finalize_failure` | Then `nb_notify_failed` if finalize fails |
| **Activity Type** | Notebook |
| **Purpose** | Finalize failure; notify runs on finalize failure path. |
| **Configuration** | `p_mode = FINALIZE_FAILURE`, `p_status = FAILED` |

---

### Bronze Child Pipeline (`pl_forms_samms_to_lakehouse`) — Per-Site Pattern

Single **ForEach** over all sites (`fe_each_samms_sites`, `batchCount = 5`).

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `lkp_check_form_table` | Lookup | Verify clinic has `Form` table; skip site if absent |
| 2 | `if_form_table_exists` | IfCondition | Gate downstream activities |
| 3 | `lkp_get_existing_tables` | Lookup | List all tables in clinic database |
| 4 | `lkp_get_existing_columns` | Lookup | List columns for optional tables (e.g. `tblTP17REVIEW`) |
| 5 | `nb_forms_build_site_sql` | Notebook | Build UNION ALL SQL from Forms2Process |
| 6 | `cp_forms_to_bronze` | Copy | Execute generated SQL against SAMMS -> Append Bronze |
| 7 | `mk_formqa_site_success` | Copy | Write site success marker (retry = 3) |

#### Bronze Aggregate Activity

| Field | Value |
|-------|-------|
| **Activity Name** | `set_child_bronze_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | After ForEach **Completes**, builds `FormQuestionAnswers` status on `pipelineReturnValue`. |
| **Output** | `pipelineReturnValue['v_bronze_method_results_json']` |

---

## 4. Source Details

| Field | Value |
|-------|-------|
| **Source System** | SAMMS On-Premises SQL Server (per clinic) |
| **Connection (high level)** | Fabric linked service via on-premises data gateway |
| **Connection ID** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Active Sites** | ~115 clinic databases |
| **Data Format** | Tabular SQL — dynamic UNION ALL per site |

### Source Objects (per clinic)

| Object Type | Examples | Notes |
|-------------|----------|-------|
| Required gate table | `Form` | Site skipped if absent |
| Base model | `FormTemplate`, `Question`, `Answer` | Core Q&A extraction |
| Pre-admission joins | `SF_PatientPreAdmission`, `SF_DataForms` | Compound `IsDeleted` logic |
| Custom form tables | ~75 patterns from Forms2Process | Only unioned if table exists at clinic |
| Optional columns | e.g. `tblTP17REVIEW.tprReviewFrequency` | Omitted from SQL if column absent |

### Load Strategy

| Setting | Value |
|---------|-------|
| **Load Type** | Incremental — default **30-day** lookback (`p_lookback_days`) |
| **Full Reload** | `p_reload = true` -> lookback date **2010-01-01** in SQL builder and Silver pre-pass |
| **SQL Generation** | Per site via `nb_forms_build_site_sql` reading `bhg_silver.ctrl.Forms2Process` |
| **Date Filter Rules** | Per form: `DateFilterEnabled` in Forms2Process; `CreatedOn`/`UpdatedOn` vs lookback |
| **Watermark Column** | Not used — date logic embedded in generated SQL |

---

## 5. Destination Details

| Field | Value |
|-------|-------|
| **Destination Type** | Fabric Lakehouse (Bronze and Silver) |
| **Bronze Lakehouse** | `bhg_bronze` (Artifact ID `77d24027-6a1c-43a8-a998-1a14dd3c0d52`) |
| **Silver Lakehouse (final)** | `bhg_silver` |
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Bronze Destination

| Schema | Table | Write Mode |
|--------|-------|------------|
| `Form` | `br_tblFormQA` | Append (tagged by `_ingest_run_id`) |
| `Form` | `br_formqa_site_success` | Append (per-site success markers) |

### Silver Destination (Final Layer)

| Schema | Table | Write Mode | Merge Key |
|--------|-------|------------|-----------|
| `pats` | `tbl_dbo_FormQuestionAnswers` | Delta MERGE | 7-column composite key (see below) |

### Silver MERGE Key (7 columns)

`SiteCode` + `FormName` + `FormId` + `ClientId` + `PreAdmissionId` + `QuestionId` + `QuestionOrderId`

### Silver Final Columns

`SiteCode`, `FormName`, `FormId`, `ClientId`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`, `PreAdmissionId`, `IsDeleted`, `IsChildForm`, `QuestionId`, `QuestionOrderId`, `QuestionText`, `OptionId`, `AnswerValue`, `RowState`, `LastModAt`

### Bronze Metadata (not carried to Silver)

| Column | Purpose |
|--------|---------|
| `_site_code` | Maps to `SiteCode` |
| `_source_database` | Extraction audit |
| `_ingest_run_id` | Run filter |
| `_extracted_at` | Within-run deduplication |
| `_lookback_date` | SQL builder context |

---

## 6. Notebook Documentation

### `nb_get_FormQuestionAnswers_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig; returns slim JSON for pipeline runtime |
| **Input** | `meta.taskconfig` (ConfigIds 28, 29) |
| **Output** | JSON array via notebook exit |
| **Error Handling** | Fails pipeline if TaskConfig unreadable |

---

### `nb_formqa_audit_start` / `_finalize_clean_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` |
| **Purpose** | Audit lifecycle — start runs, finalize success or failure |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_audit_context_json`, `p_ingest_run_id`, `p_sites_json`, `p_silver_method_results_json`, `p_active_target_layers_json`, `p_terminal_target_name`, `p_status`, `p_error_message` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |
| **Business Logic** | Terminal layer SL; ~115 Bronze + 1 Silver task per run |

---

### `nb_forms_build_site_sql` (Bronze child)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `b25a2e2d-d7d0-4cdc-bd41-1b1f7f7253bd` |
| **Purpose** | Generates per-site UNION ALL SQL from Forms2Process |
| **Parameters** | `p_site_code`, `p_source_database`, `p_ingest_run_id`, `p_lookback_days`, `p_reload`, existing tables/columns JSON |
| **Input** | `bhg_silver.ctrl.Forms2Process`, clinic metadata lookups |
| **Output** | SQL string via notebook exit -> consumed by Copy activity |
| **Business Logic** | Unions base Form model + custom forms; date filters; compound IsDeleted; metadata columns |
| **Error Handling** | Skips missing tables/columns silently for that clinic |

---

### `nb_forms_bronze_to_silver` (Parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `564ca691-a179-49f2-ad2f-3a389c1983f4` |
| **Purpose** | Bronze -> Silver: pre-pass RowState reset + Delta MERGE |
| **Parameters** | `p_ingest_run_id`, `p_lookback_days`, `p_reload`, `p_sites_json` |
| **Input Tables** | `Form.br_tblFormQA`, `Form.br_formqa_site_success`, `ctrl.Forms2Process` |
| **Output Table** | `pats.tbl_dbo_FormQuestionAnswers` (final) |
| **Merge/Upsert Logic** | 7-key MERGE; full update on match (no RowChkSum gate) |
| **Error Handling** | SKIPPED if no successful Bronze sites; SUCCESS with zero counts if markers exist but no rows |

**Transformation highlights:**

- Deduplicate on 7-column key (latest `_extracted_at`)
- Normalize `FormId` to UPPER case
- Default null keys (`ClientId` -> 0, `PreAdmissionId` -> -1, `QuestionId` -> 0)
- RowState pre-pass per Forms2Process rules; `TP-*` under Treatment Plan entry
- RowState from `IsDeleted` and negative `ClientId`

---

### `nb_notify_failed`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `77c87686-120d-486b-9146-6a794d794e38` |
| **Purpose** | Failure notification on audit finalize failure path |
| **Parameters** | `Pipeline_Name`, `Status`, `Config_Name`, `Source_System`, `Target_Name`, `Environment_Name`, `Run_Id`, `Error_Msg` |

---

## 7. Copy Activity Documentation

| Field | Value |
|-------|-------|
| **Activity Name** | `cp_forms_to_bronze` |
| **Source** | SAMMS SQL Server — dynamic SQL from `nb_forms_build_site_sql` |
| **Destination** | `bhg_bronze.Form.br_tblFormQA` |
| **Mapping** | Generated UNION ALL query — not a fixed column map |
| **Partitioning** | N/A |
| **Incremental Logic** | Date filters in generated SQL; `p_reload` resets to 2010-01-01 |
| **Retry Configuration** | 0 (Copy default); site marker `mk_formqa_site_success` retry = 3 |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

### Site Success Marker Copy (`mk_formqa_site_success`)

| Field | Value |
|-------|-------|
| **Destination** | `Form.br_formqa_site_success` |
| **Purpose** | Records site completed Bronze even when zero Q&A rows extracted |
| **Columns** | `Method`, `SiteCode`, `SourceDatabase`, `IngestRunId`, `MarkedAt` |

---

## 8. PySpark Transformations

### Data Cleansing (Bronze -> Silver)

- Filter Bronze to current `_ingest_run_id`.
- Determine successful sites from Bronze rows and/or site success markers.
- Deduplicate within run on 7-column business key.
- Align to Silver target schema; drop Bronze metadata columns.

### Business Rules Implemented (Silver)

| Rule | Description |
|------|-------------|
| 7-column merge key | Must include `QuestionOrderId` |
| FormId normalization | UPPER case |
| RowState pre-pass | Unconditional reset for non-date-filtered forms; date-gated for others |
| TP-* forms | Treated under "Treatment Plan" Forms2Process entry |
| RowState on row | `IsDeleted` or negative `ClientId` -> RowState 0 |
| Full update on match | No RowChkSum gate — always refresh matched Q&A fields |
| Reload mode | `p_reload` resets lookback to 2010-01-01 in pre-pass and builder |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **Pre-pass UPDATE** | RowState reset for sites in current run before MERGE |
| **MERGE — Matched** | Full column update |
| **MERGE — Not Matched** | INSERT new 7-key combination |

### Performance Optimizations

- Single ForEach with `batchCount = 5` limits gateway concurrency.
- SQL builder skips non-existent tables/columns per clinic.
- One Silver notebook processes all sites in a single MERGE per run.

### Error Handling

- Per-site isolation in Bronze ForEach — one clinic failure does not stop others.
- Lookup failure on Form table check handled via `handle_lkp_check_form_table_failure` Wait activity.
- Site marker failure handled via `handle_mk_formqa_site_success_failure`.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters (`pl_execute_form_questionanswers`)

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | `manual-run` | Tags Bronze rows; filters Silver |
| `p_lookback_days` | int | 30 | Incremental window (unless reload) |
| `p_reload` | bool | false | When true, extract from 2010-01-01 |
| `p_sites` | array | [] | Site list (normally from Filter, not manual) |

### Parent Pipeline Variables

| Variable | Set By | Used By |
|----------|--------|---------|
| `v_bronze_method_results_json` | `set_formqa_bronze_results` | Silver context, audit, notify |
| `v_silver_method_results_json` | `set_formqa_silver_results` | Audit finalize, notify |

### Bronze Child Parameters (`pl_forms_samms_to_lakehouse`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Metadata on Bronze rows |
| `p_lookback_days` | int | SQL builder lookback |
| `p_sites` | array | ForEach site list |
| `p_reload` | bool | Full reload flag |
| `p_audit_context_json` | string | Audit correlation |

### Child Return Values (`pipelineReturnValue`)

| Child | Return Key | Set By |
|-------|------------|--------|
| `pl_forms_samms_to_lakehouse` | `v_bronze_method_results_json` | `set_child_bronze_method_results` |

---

## 10. Dependencies

### Activity Execution Order (Parent)

```
nb_get_FormQuestionAnswers_taskconfig
  -> flt_active_formquestionanswers_sites
  -> nb_formqa_audit_start
  -> Invoke_legacy_Execute_AfterBronz (pl_forms_samms_to_lakehouse)
  -> set_formqa_bronze_results
  -> nb_forms_bronze_to_silver
  -> set_formqa_silver_results
  -> if_formqa_silver_result_success
      +- TRUE  -> nb_formqa_audit_finalize_clean_success
      +- FALSE -> nb_formqa_audit_finalize_failure -> nb_notify_failed
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
| No `Form` table at clinic | Site skipped — no Copy |
| Bronze site failures | Tracked in site_results; partial Bronze JSON |
| No successful Bronze sites | Silver exits SKIPPED |
| Silver FAILED/SKIPPED/ERROR | Audit failure branch + notify |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 28 | BR | Bronze extraction |
| 29 | SL | Silver merge |
| 30 | GL | Configured — **not active**; SL is terminal |

---

## 11. Validation

### Source Validation

- `lkp_check_form_table` gates sites without Form table.
- SQL builder only unions tables/columns that exist at clinic.

### Row Count Validation

- Audit `DataQuality` records Bronze vs Silver counts.
- Silver exit JSON: `rows_read`, `rows_inserted`, `rows_updated`, `rows_skipped`, `site_results`.

### Duplicate Checks

- Within-run deduplication on 7-column key before MERGE.
- `DuplicateCount` in `meta.dataquality`.

### Business Validations

| Validation | Detail |
|------------|--------|
| Merge key | All 7 columns required — especially `QuestionOrderId` |
| TP-* forms | Treatment Plan date filter rules from Forms2Process |
| Site success markers | Bronze success with zero rows still valid |
| Forms2Process coverage | Missing form types at clinic handled gracefully |

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Per-site failure in ForEach; partial Bronze JSON |
| No Form table | Site skipped | Expected for some clinics |
| SQL builder error | Site fails | Logged; other sites continue |
| Silver MERGE failure | Pipeline fails at SL | Audit finalize failure + notify |
| Forms2Process stale/missing | Wrong/missing form types | Ensure Forms2Process ETL runs first |

### Retry Logic

- Most activities: retry = 0.
- `mk_formqa_site_success`: retry = 3.

### Recovery Steps

1. Review `v_silver_method_results_json` and Silver `site_results`.
2. Query `meta.taskaudit` and `meta.dataquality` for ingest run ID.
3. Fix gateway, Forms2Process, or clinic-specific SQL issues.
4. Re-run pipeline — new `_ingest_run_id` prevents duplicate merges.
5. Delta time travel on Silver if bad merge confirmed.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric run history — parent and Bronze child.
- `meta.pipelinerun` — status per layer (BR, SL).

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%FormQuestionAnswers%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%FormQuestionAnswers%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (28, 29)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| Site skipped entirely | `lkp_check_form_table` — no Form table |
| Site in marker, zero Bronze rows | `p_lookback_days`, `p_reload` — no forms in window |
| Silver SKIPPED | No successful Bronze sites — `br_formqa_site_success` |
| Missing form type | Forms2Process entry; table absent at clinic |
| TP-* RowState wrong | Forms2Process "Treatment Plan" date filter |
| Duplicate Silver rows | All 7 merge key columns used |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Forms2Process current** | `bhg_silver.ctrl.Forms2Process` loaded before Bronze |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **Parameter validation** | `p_lookback_days` (default 30), `p_reload` (false for daily) |
| **TaskConfig active** | ConfigId 28 rows active for target sites |
| **Gateway capacity** | ~115 sites, batchCount 5 concurrent copies |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor Succeeded or expected Failed |
| **Site completion** | `br_formqa_site_success` rows vs active sites |
| **Source-to-target counts** | `meta.taskaudit` and `meta.dataquality` |
| **Silver table validation** | Query `tbl_dbo_FormQuestionAnswers` for site coverage |
| **RowState pre-pass** | Verify RowState reset for sites in current run |
| **Log review** | Silver notebook `site_results` JSON |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%FormQuestionAnswers%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%FormQuestionAnswers%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (28, 29)
  AND PipelineRunId = '<pipeline_run_id>';

-- Site success markers
SELECT *
FROM Form.br_formqa_site_success
WHERE IngestRunId = '<pipeline_run_id>';
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_execute_form_questionanswers` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — `fe_each_samms_sites` ForEach canvas**

*[Insert screenshot]*

**3. SQL builder notebook — `nb_forms_build_site_sql` activity configuration**

*[Insert screenshot]*

**4. TaskConfig notebook — `nb_get_FormQuestionAnswers_taskconfig`**

*[Insert screenshot]*

**5. Audit start notebook — `nb_formqa_audit_start` parameters**

*[Insert screenshot]*

**6. Copy activity — `cp_forms_to_bronze` dynamic source SQL**

*[Insert screenshot]*

**7. Silver notebook on parent — `nb_forms_bronze_to_silver`**

*[Insert screenshot]*

**8. Silver notebook — RowState pre-pass and Delta MERGE logic**

*[Insert screenshot]*

**9. IfCondition — audit finalize success / failure branches**

*[Insert screenshot]*

**10. Forms2Process control table — `bhg_silver.ctrl.Forms2Process`**

*[Insert screenshot]*

**11. Pipeline monitoring — successful execution**

*[Insert screenshot]*

**12. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS Form Question Answers ETL | v1.0*
