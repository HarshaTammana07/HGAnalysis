# SAMMS P1 Forms ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_execute_forms` |
| **Parent Pipeline Object ID** | `a288b29f-c5ae-4994-b131-2dfb0af137e8` |
| **Bronze Child Pipeline** | `pl_p1_forms` (`b934fec7-7208-4f3a-a621-b9bd414aed1f`) |
| **Silver Child Pipeline** | `pl_p1_forms_bronze_to_silver` (`4c0bd0aa-4771-4471-9789-2b126c788a31`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 29/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS P1 clinical form records from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — nine form tables are published to the Silver lakehouse for downstream reporting and analytics.

**Nine form methods processed:**

| Method | Description | Silver Target |
|--------|-------------|---------------|
| `SaveComprehensiveAssessmentForm` | Comprehensive assessment intake form | `bhg_silver.pats.tbl_ComprehensiveAssessmentForm` |
| `SaveEMFormPregnancy` | E&M pregnancy form (joined extract) | `bhg_silver.pats.tbl_EandMFormPregnancy` |
| `SaveEMFormMDM` | E&M medical decision-making form | `bhg_silver.pats.tbl_EandMFormMDM` |
| `SaveDataForms` | SF data forms master | `bhg_silver.pats.tbl_SF_DataForms` |
| `SaveSMSTextConsentForm` | SMS text consent form | `bhg_silver.pats.tbl_SMSTextConsentForm` |
| `SaveConsenttoMarketing` | Marketing consent form | `bhg_silver.pats.tbl_ConsenttoMarketing` |
| `SaveTakeHomeAgreementandDiversionControl` | Take-home agreement and diversion control | `bhg_silver.pats.tbl_TakeHomeAgreementandDiversionControl` |
| `SaveTakeHomeRiskAssessment` | Take-home risk assessment | `bhg_silver.pats.tbl_TakeHomeRiskAssessment` |
| `SaveNewDischargeTransferPlanForm` | Discharge / transfer plan form | `bhg_silver.pats.tbl_NewDischargeTransferPlanForm` |

**Legacy context:** Part of Regional ETL P1 (`BHGTaskRunner.exe 2`); replaces legacy C# Save* form methods in `SaveFormQAData` / P1 forms path.

**Important design notes:**

- Bronze uses **fixed Copy SQL per method** — not a dynamic SQL builder (unlike FormQuestionAnswers / FormAnswerSignatures).
- Bronze site success is **inferred from each method's Bronze table** using TaskConfig `RequestBody.full_table`, `ingest_column`, and `site_column` — no separate site-marker table.
- Silver runs as **nine parallel notebooks** inside child pipeline `pl_p1_forms_bronze_to_silver`.
- Optional Gold publish (`nb_p1_forms_optional_gold_publish`, ConfigId 99) exists on the success path but is **out of active workflow scope** — Silver is the terminal layer for consumers.

---

## 2. Solution Overview

### Business Objective

Extract, normalize, and merge nine SAMMS clinical form types from per-clinic databases into Fabric Silver while preserving legacy C# upsert behavior: method-specific merge keys, RowState / IsDeleted handling, incremental lookback where configured, and per-method partial success.

### End-to-End Data Flow

1. **Extract** raw form data via Copy Data activities (Bronze child — 9 parallel method loops × ~115 sites).
2. **Transform and merge** Bronze into Silver using nine parallel PySpark notebooks with method-specific Delta MERGE rules (Silver child).
3. **Audit** — pipeline run, task queue, and data quality written to control tables; optional Gold publish when ConfigId 99 tasks are active.

### Source Systems

- On-premises SAMMS SQL Server databases (one database per clinic, ~115 active sites).
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `P1Forms` (append by `IngestRunId`).
- **Silver (final):** `bhg_silver` Lakehouse — schema `pats` (nine form tables).

### Overall Architecture Diagram

```
pl_execute_forms (PARENT)
|
+- nb_get_p1_forms_taskconfig
+- flt_active_p1_forms_sites
+- nb_p1_forms_audit_start
|
+- Executed_AfterBronz -> pl_p1_forms (BRONZE CHILD)
|     +- 9 x (Filter -> ForEach sites -> Lookup -> If -> Copy)
|
+- set_bronze_method_results_from_child
+- Executed_AfterSilver -> pl_p1_forms_bronze_to_silver (SILVER CHILD)
|     +- 9 parallel Silver MERGE notebooks
|
+- set_silver_method_results_from_child
+- if_all_forms_methods_success
|     +- TRUE  -> flt_active_p1_forms_gold -> nb_p1_forms_optional_gold_publish (optional)
|     |          -> nb_p1_forms_audit_finalize_success
|     +- FALSE -> nb_p1_forms_audit_finalize_failure
+- nb_p1_forms_notify_failed (Inactive)
```

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_execute_forms`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_p1_forms_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for P1 Forms methods and returns a slim JSON array. Avoids Fabric Lookup 4 MB output limit. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_config_ids_json` | `[97, 99]` |
| `p_methods_json` | Nine Save* form methods (see Section 1) |
| `p_only_active` | `true` |
| `p_require_site` | `false` |
| `p_require_database` | `false` |
| `p_require_source_table` | `true` |

---

#### Activity 2: Filter Active Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `flt_active_p1_forms_sites` |
| **Activity Type** | Filter |
| **Purpose** | Keeps active Bronze ConfigId **97** rows for the nine form methods with populated `SiteCode`, `DataBaseName`, and `SourceTable`. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_p1_forms_taskconfig` (Succeeded) |
| **Input** | `@json(activity('nb_get_p1_forms_taskconfig').output.result.exitValue)` |
| **Output** | Filtered JSON array — authoritative site/method list for this run |

---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_forms_audit_start` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `dc7a5867-14f5-455a-a0f8-ff5c036346b9` |
| **Purpose** | Initiates audit logging — creates `PipelineRun` and `TaskQueue` rows for Bronze (~1,035 site×method tasks) and Silver (9 method tasks). |
| **Execution Sequence** | 3 |
| **Dependencies** | `flt_active_p1_forms_sites` (Succeeded) |
| **Output** | Audit context JSON via notebook exit |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS P1 Forms` |
| `p_pipeline_name` | `pl_execute_forms` |
| `p_pipeline_path` | `/pipelines/pl_execute_forms` |
| `p_triggered_by` | `Fabric` |

---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterBronz` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_p1_forms` |
| **Purpose** | Extracts form data from SAMMS clinic databases and lands it in Bronze lakehouse tables. Nine methods run in parallel. |
| **Execution Sequence** | 4 |
| **Dependencies** | `nb_p1_forms_audit_start` (Succeeded) |
| **Output** | Child `pipelineReturnValue` with per-method Bronze status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@if(empty, pipeline().RunId, p_ingest_run_id)` |
| `p_work_date` | `@pipeline().parameters.p_work_date` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 15) |
| `p_sites` | `@activity('flt_active_p1_forms_sites').output.value` |
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
| **Output** | Pipeline variable `v_bronze_method_results_json` |

---

#### Activity 6: Silver Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterSilver` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_p1_forms_bronze_to_silver` |
| **Purpose** | Runs nine parallel Silver notebooks — Delta MERGE from Bronze into final Silver tables. |
| **Execution Sequence** | 6 |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |
| **Output** | Child `pipelineReturnValue` with per-method Silver status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | Pipeline RunId or override |
| `p_bronze_method_results_json` | `@variables('v_bronze_method_results_json')` |
| `p_sites_json` | `@string(activity('flt_active_p1_forms_sites').output.value)` |

---

#### Activity 7: Capture Silver Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_silver_method_results_from_child` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Silver child return JSON in parent variable `v_silver_method_results_json`. |
| **Execution Sequence** | 7 |
| **Dependencies** | `Executed_AfterSilver` (Succeeded) |
| **Output** | Pipeline variable `v_silver_method_results_json` |

---

#### Activity 8: Audit Finalize (Conditional)

| Field | Value |
|-------|-------|
| **Activity Name** | `if_all_forms_methods_success` |
| **Activity Type** | IfCondition |
| **Purpose** | Routes to success (optional Gold + audit finalize) or failure audit based on bronze and silver method result JSON. |
| **Execution Sequence** | 8 |
| **Dependencies** | `set_silver_method_results_from_child` (Succeeded) |
| **Condition** | Neither `v_bronze_method_results_json` nor `v_silver_method_results_json` contains `FAILED` |

**If TRUE — Success path:**

| Activity | Purpose |
|----------|---------|
| `flt_active_p1_forms_gold` | Filters ConfigId **99** active Gold tasks |
| `nb_p1_forms_optional_gold_publish` | Optional Gold watermark merge (`c1e2d16d-7945-4d2f-8cee-dced07b2e1e4`) |
| `nb_p1_forms_audit_finalize_success` | Marks tasks SUCCESS; writes DataQuality rows |

**If FALSE — Failure path:**

| Activity | Purpose |
|----------|---------|
| `nb_p1_forms_audit_finalize_failure` | Partial finalize — failed methods marked FAILED |

---

#### Activity 9: Failure Notification (Inactive)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_forms_notify_failed` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `77c87686-120d-486b-9146-6a794d794e38` |
| **State** | **Inactive** |
| **Purpose** | Would send per-method BR/SL failure alert when IfCondition fails or is skipped. |

---

### Bronze Child Pipeline (`pl_p1_forms`) — Per-Method Pattern (×9)

Each of the nine form methods follows the same pattern. All nine ForEach blocks start in parallel. A final SetVariable aggregates status after all nine **Complete**.

| Method | Filter Activity | ForEach Activity | Lookup | If | Copy Activity | Bronze Table |
|--------|----------------|------------------|--------|-----|---------------|--------------|
| `SaveComprehensiveAssessmentForm` | `flt_child_comprehensive_assessment_form_sites` | `fe_each_samms_site_comprehensive_assessment_form` | `lkp_check_comprehensive_assessment_form` | `if_comprehensive_assessment_form_exists` | `cp_comprehensive_assessment_form_to_bronze` | `br_samms_comprehensive_assessment_form` |
| `SaveEMFormPregnancy` | `flt_child_eandm_form_pregnancy_sites` | `fe_each_samms_site_eandm_form_pregnancy` | `lkp_check_eandm_form_pregnancy` | `if_eandm_form_pregnancy_exists` | `cp_eandm_form_pregnancy_to_bronze` | `br_samms_eandm_form_pregnancy` |
| `SaveEMFormMDM` | `flt_child_eandm_form_mdm_sites` | `fe_each_samms_site_eandm_form_mdm` | `lkp_check_eandm_form_mdm` | `if_eandm_form_mdm_exists` | `cp_eandm_form_mdm_to_bronze` | `br_samms_eandm_form_mdm` |
| `SaveDataForms` | `flt_child_sf_data_forms_sites` | `fe_each_samms_site_sf_data_forms` | `lkp_check_sf_data_forms` | `if_sf_data_forms_exists` | `cp_sf_data_forms_to_bronze` | `br_samms_sf_data_forms` |
| `SaveSMSTextConsentForm` | `flt_child_sms_text_consent_form_sites` | `fe_each_samms_site_sms_text_consent_form` | `lkp_check_sms_text_consent_form` | `if_sms_text_consent_form_exists` | `cp_sms_text_consent_form_to_bronze` | `br_samms_sms_text_consent_form` |
| `SaveConsenttoMarketing` | `flt_child_consent_to_marketing_sites` | `fe_each_samms_site_consent_to_marketing` | `lkp_check_consent_to_marketing` | `if_consent_to_marketing_exists` | `cp_consent_to_marketing_to_bronze` | `br_samms_consent_to_marketing` |
| `SaveTakeHomeAgreementandDiversionControl` | `flt_child_take_home_agreement_diversion_control_sites` | `fe_each_samms_site_takehome_agree_div_ctrl` | `lkp_check_take_home_agreement_diversion_control` | `if_take_home_agreement_diversion_control_exists` | `cp_take_home_agreement_diversion_control_to_bronze` | `br_samms_take_home_agreement_diversion_control` |
| `SaveTakeHomeRiskAssessment` | `flt_child_take_home_risk_assessment_sites` | `fe_each_samms_site_take_home_risk_assessment` | `lkp_check_take_home_risk_assessment` | `if_take_home_risk_assessment_exists` | `cp_take_home_risk_assessment_to_bronze` | `br_samms_take_home_risk_assessment` |
| `SaveNewDischargeTransferPlanForm` | `flt_child_new_discharge_transfer_plan_form_sites` | `fe_each_samms_site_new_discharge_transfer_plan_form` | `lkp_check_new_discharge_transfer_plan_form` | `if_new_discharge_transfer_plan_form_exists` | `cp_new_discharge_transfer_plan_form_to_bronze` | `br_samms_new_discharge_transfer_plan_form` |

#### Bronze Activity Pattern (per method, per site)

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `flt_child_*_sites` | Filter | Filter parent `p_sites` to one method |
| 2 | `fe_each_samms_site_*` | ForEach | Iterate sites — `isSequential: false`, `batchCount: 3` |
| 3 | `lkp_check_*` | Lookup | Verify source table(s) exist in clinic SAMMS database |
| 4 | `if_*_exists` | IfCondition | Run Copy only when gate passes |
| 5 | `cp_*_to_bronze` | Copy | Dynamic SELECT with metadata columns → Append to Bronze |

**Special lookup gates:**

| Method | Lookup requirement |
|--------|-------------------|
| `SaveComprehensiveAssessmentForm` | `ComprehensiveAssessmentForm` table exists |
| `SaveEMFormPregnancy` | All three: `EandMForm`, `EandMFormPregnancy`, `SF_PatientPreAdmission` |
| Other methods | Respective source table exists |

#### Bronze Aggregate Activity

| Field | Value |
|-------|-------|
| **Activity Name** | `set_child_bronze_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | After all nine ForEach loops **Complete**, builds per-method status JSON on `pipelineReturnValue`. |
| **Dependencies** | All nine ForEach activities (Completed) |
| **Output** | `pipelineReturnValue['v_bronze_method_results_json']` |

---

### Silver Child Pipeline (`pl_p1_forms_bronze_to_silver`) — Nine Parallel Notebooks

| Notebook Activity | Object ID | Method | Silver Target |
|-------------------|-----------|--------|---------------|
| `nb_forms_sl_comp_assess` | `1fef87e4-feaf-4d6e-8e12-e41260c938e0` | `SaveComprehensiveAssessmentForm` | `pats.tbl_ComprehensiveAssessmentForm` |
| `nb_forms_sl_em_preg` | `f2631915-54e0-4407-a3f6-b709218989da` | `SaveEMFormPregnancy` | `pats.tbl_EandMFormPregnancy` |
| `nb_forms_sl_em_mdm` | `56d1c4d5-ff78-454a-a014-e197daef7c78` | `SaveEMFormMDM` | `pats.tbl_EandMFormMDM` |
| `nb_forms_sl_data_forms` | `505f0328-c497-4495-88dc-4e7467621e19` | `SaveDataForms` | `pats.tbl_SF_DataForms` |
| `nb_forms_sl_sms_consent` | `f9ec192e-2b82-46e7-8dee-430df050695b` | `SaveSMSTextConsentForm` | `pats.tbl_SMSTextConsentForm` |
| `nb_forms_sl_marketing` | `826419c4-0686-425b-af28-c58f44d5f69d` | `SaveConsenttoMarketing` | `pats.tbl_ConsenttoMarketing` |
| `nb_forms_sl_takehome_agree` | `d14ddddb-62b3-4baf-9312-adc1141bf1f0` | `SaveTakeHomeAgreementandDiversionControl` | `pats.tbl_TakeHomeAgreementandDiversionControl` |
| `nb_forms_sl_takehome_risk` | `86916924-cd42-4553-9637-37301f0adbd2` | `SaveTakeHomeRiskAssessment` | `pats.tbl_TakeHomeRiskAssessment` |
| `nb_forms_sl_discharge_plan` | `aa8969a2-27c2-4334-a512-d5ed4e62d641` | `SaveNewDischargeTransferPlanForm` | `pats.tbl_NewDischargeTransferPlanForm` |

**Common Silver notebook parameters:** `p_ingest_run_id`, `p_bronze_method_results_json`, `p_sites_json`, `p_method_name`

#### Silver Aggregate Activity

| Field | Value |
|-------|-------|
| **Activity Name** | `set_silver_method_results_return` |
| **Activity Type** | SetVariable |
| **Purpose** | After all nine notebooks **Complete**, concatenates exit JSON into `pipelineReturnValue['v_silver_method_results_json']`. |
| **Dependencies** | All nine Silver notebooks (Completed) |

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

| # | Method | Source Table | Load Type | Incremental Logic |
|---|--------|--------------|-----------|-------------------|
| 1 | `SaveComprehensiveAssessmentForm` | `dbo.ComprehensiveAssessmentForm` | Full | `WHERE 1 = 1` — full table extract |
| 2 | `SaveEMFormPregnancy` | `dbo.EandMFormPregnancy` (+ joins) | Full | Joined extract via EandMForm / PreAdmission |
| 3 | `SaveEMFormMDM` | `dbo.EandMForm` | Full | Full extract with legacy column mapping |
| 4 | `SaveDataForms` | `dbo.SF_DataForms` | Incremental | **15-day lookback** on date columns |
| 5 | `SaveSMSTextConsentForm` | `dbo.SMSTextConsentForm` | Full | Full extract |
| 6 | `SaveConsenttoMarketing` | `dbo.consenttomarketing` | Incremental | **15-day lookback** |
| 7 | `SaveTakeHomeAgreementandDiversionControl` | `dbo.takehomeagreementanddiversioncontrol` | Incremental | **15-day lookback** |
| 8 | `SaveTakeHomeRiskAssessment` | `dbo.TakeHomeRiskAssessment` | Full | Full extract |
| 9 | `SaveNewDischargeTransferPlanForm` | `dbo.newdischargetransferplanform` | Incremental | **15-day lookback** |

**Source table existence check:** Bronze Lookup verifies table exists before Copy. Missing tables are skipped gracefully (some clinics on different SAMMS versions).

---

## 5. Destination Details

| Field | Value |
|-------|-------|
| **Destination Type** | Fabric Lakehouse (Bronze and Silver) |
| **Bronze Lakehouse** | `bhg_bronze` (Artifact ID `77d24027-6a1c-43a8-a998-1a14dd3c0d52`) |
| **Silver Lakehouse (final)** | `bhg_silver` (Artifact ID `dd09d8b6-d862-4954-a0b2-fcf7372c6595`) |
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Bronze Destination

| Schema | Pattern | Write Mode |
|--------|---------|------------|
| `P1Forms` | `br_samms_*` (one table per method) | Append (tagged by `IngestRunId`) |

### Silver Destination (Final Layer)

| Schema | Table | Merge Key |
|--------|-------|-----------|
| `pats` | `tbl_ComprehensiveAssessmentForm` | `SiteCode` + `Id` |
| `pats` | `tbl_EandMFormPregnancy` | `SiteCode` + `EandMFormId` |
| `pats` | `tbl_EandMFormMDM` | `SiteCode` + `Id` |
| `pats` | `tbl_SF_DataForms` | `SiteCode` + `Id` |
| `pats` | `tbl_SMSTextConsentForm` | `SiteCode` + `Id` |
| `pats` | `tbl_ConsenttoMarketing` | `SiteCode` + `Id` |
| `pats` | `tbl_TakeHomeAgreementandDiversionControl` | `SiteCode` + `Id` |
| `pats` | `tbl_TakeHomeRiskAssessment` | `SiteCode` + `Id` |
| `pats` | `tbl_NewDischargeTransferPlanForm` | `SiteCode` + `Id` |

### Bronze Metadata Columns (added in every Copy)

| Column | Purpose |
|--------|---------|
| `SiteCode` | Clinic identifier |
| `SourceDatabase` | SAMMS database name |
| `IngestRunId` | Pipeline run filter |
| `ExtractedAt` | Extraction timestamp |
| `SourceQueryStartDate` / `SourceQueryEndDate` | Query window audit |
| `LookbackDate` | Effective lookback date used |
| `RowChkSum` | `CHECKSUM(*)` at source (where applicable) |
| `LastModAt` | Load timestamp |
| `RowState` | Legacy active/deleted flag (method-specific mapping) |

Bronze metadata columns are dropped or transformed at Silver — not carried as-is to final reporting columns.

---

## 6. Notebook Documentation

### `nb_get_p1_forms_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig for ConfigIds 97 and 99; returns slim JSON |
| **Input** | `meta.taskconfig` |
| **Output** | JSON array via notebook exit |

---

### `nb_p1_forms_audit_start` / `_finalize_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `dc7a5867-14f5-455a-a0f8-ff5c036346b9` |
| **Purpose** | Audit lifecycle for P1 Forms |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_audit_context_json`, `p_ingest_run_id`, `p_sites_json`, `p_bronze_method_results_json`, `p_silver_method_results_json`, `p_status`, `p_error_message` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

---

### Silver Notebooks (×9)

| Field | Value |
|-------|-------|
| **Purpose** | Bronze → Silver Delta MERGE per form method |
| **Input** | Bronze table for method + `IngestRunId`; TaskConfig metadata via `resolve_forms_silver_metadata()` |
| **Business Logic** | Filter to successful Bronze sites; align schema; deduplicate on match keys; full update on match |
| **Merge/Upsert Logic** | TaskConfig `dq_keys`; null-safe `<=>` match; `whenMatchedUpdate` + `whenNotMatchedInsert` |
| **Error Handling** | Per-method isolation — SKIPPED when all Bronze sites failed; FAILED returns traceback in exit JSON |
| **Exit JSON** | `status`, `rows_read`, `rows_inserted`, `rows_updated`, `rows_skipped`, `site_results` |

**Method-specific legacy notes:**

| Method | Notable Silver behavior |
|--------|---------------------------|
| `SaveComprehensiveAssessmentForm` | `RowState` from `IsDeleted`; `RowChkSum` from Bronze |
| `SaveEMFormPregnancy` | Joined pregnancy extract; match on `EandMFormId` |
| `SaveEMFormMDM` | Updates all matched rows by `SiteCode` + `Id` (legacy parity) |
| Incremental methods | Scope filtered by lookback window in Bronze Copy SQL |

---

### `nb_p1_forms_optional_gold_publish` (optional — out of workflow scope)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `c1e2d16d-7945-4d2f-8cee-dced07b2e1e4` |
| **Purpose** | Watermark-based Gold merge when ConfigId 99 tasks are active |
| **Scope** | Not part of Silver-terminal consumer flow documented here |

---

## 7. Copy Activity Documentation

Each Bronze Copy activity follows the same incremental/full pattern per method.

| Field | Value |
|-------|-------|
| **Source** | SAMMS SQL Server (clinic database via gateway) |
| **Destination** | `bhg_bronze.P1Forms.br_samms_*` |
| **Mapping** | Explicit column map per method + metadata columns |
| **Partitioning** | N/A |
| **Incremental Logic** | 15-day lookback for incremental methods; full extract for others |
| **Retry Configuration** | 0 (pipeline default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

---

## 8. PySpark Transformations

### Data Cleansing (Bronze → Silver)

- Filter Bronze to current `IngestRunId`.
- Determine successful sites from Bronze rows (TaskConfig-driven `site_column` / `ingest_column`).
- Deduplicate on method match keys before MERGE.
- Align to Silver target schema via TaskConfig metadata.

### Business Rules Implemented (Silver)

| Rule | Description |
|------|-------------|
| Match keys from TaskConfig | `dq_keys` must match Silver MERGE keys |
| Full update on match | No RowChkSum gate in generic merge — always refresh matched columns |
| Bronze method failure | Silver SKIPPED for that method when all sites failed at BR |
| Zero Bronze rows | Silver SUCCESS with zero counts when no data in lookback window |
| Per-site results | `site_results` in exit JSON for audit and troubleshooting |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **CREATE TABLE** | First run when Silver table does not exist |
| **MERGE — Matched** | Full column update (excluding insert-only columns if configured) |
| **MERGE — Not Matched** | INSERT new key combination |

### Performance Optimizations

- Nine parallel Bronze ForEach loops with `batchCount = 3` limits gateway concurrency per method.
- Nine parallel Silver notebooks — one MERGE per method per run.
- Lookup gate skips Copy for clinics without required tables.

### Error Handling

- Per-site isolation in Bronze ForEach — one clinic failure does not stop other sites in the same method.
- Per-method isolation in Silver — one form type failing does not block the other eight.
- Partial success propagated via bronze/silver method JSON to audit finalize.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters (`pl_execute_forms`)

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | (empty → RunId) | Tags Bronze rows; filters Silver |
| `p_work_date` | string | `2026-07-20` | Work date for scheduling context |
| `p_lookback_days` | int | 15 | Incremental window for applicable methods |

### Parent Pipeline Variables

| Variable | Set By | Used By |
|----------|--------|---------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver child, audit, notify |
| `v_silver_method_results_json` | `set_silver_method_results_from_child` | Audit finalize |

### Bronze Child Parameters (`pl_p1_forms`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Bronze metadata |
| `p_work_date` | string | Scheduling context |
| `p_lookback_days` | int | Incremental Copy SQL |
| `p_sites` | array | ForEach site list |

### Silver Child Parameters (`pl_p1_forms_bronze_to_silver`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Bronze row filter |
| `p_bronze_method_results_json` | string | Skip logic when method failed at BR |
| `p_sites_json` | string | Site scope for Silver |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 97 | BR | Bronze extraction |
| 98 | SL | Silver merge |
| 99 | GL | Optional Gold publish |

Audit prefix: **`SAMMS P1 Forms`**.

---

## 10. Dependencies

### Activity Execution Order (Parent — Silver Terminal)

```
nb_get_p1_forms_taskconfig
  -> flt_active_p1_forms_sites
  -> nb_p1_forms_audit_start
  -> Executed_AfterBronz (pl_p1_forms)
  -> set_bronze_method_results_from_child
  -> Executed_AfterSilver (pl_p1_forms_bronze_to_silver)
  -> set_silver_method_results_from_child
  -> if_all_forms_methods_success
      -> TRUE: optional Gold publish -> nb_p1_forms_audit_finalize_success
      -> FALSE: nb_p1_forms_audit_finalize_failure
```

### External Dependencies

| Dependency | Requirement |
|------------|-------------|
| On-premises gateway | SAMMS SQL Server reachable |
| Fabric lakehouses | `bhg_bronze`, `bhg_silver` online |
| TaskConfig | ConfigId 97 Bronze rows active for target sites |
| P1 Reference site universe | Forms TaskConfig seeded from P1 Reference site list (~115 sites) |

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| Source table missing | Site skipped — no Copy |
| Bronze method ForEach fails | Method status FAILED in bronze JSON; Silver may SKIPPED |
| Silver notebook fails | Method status FAILED in silver JSON; audit finalize failure path |
| All methods succeed | Optional Gold publish then audit finalize success |

### Inactive Activities

| Activity | State | Notes |
|----------|-------|-------|
| `nb_p1_forms_notify_failed` | Inactive | Notifications not active |

---

## 11. Validation

### Source Validation

- Per-method Lookup gate before extraction.
- EM Pregnancy requires three-table existence check.

### Row Count Validation

- Audit `DataQuality` records Bronze vs Silver counts after finalize.
- Compare Bronze rows for `IngestRunId` vs Silver merge counts per method.

### Business Validations

| Validation | Detail |
|------------|--------|
| Merge key | `SaveEMFormPregnancy` uses `SiteCode` + `EandMFormId` — not `Id` |
| Duplicate check | All other methods use `SiteCode` + `Id` |
| Schema parity | Column counts/names/order validated against BHG_DR (see unit testing guide) |
| Incremental scope | DataForms, Marketing, TakeHome Agreement, Discharge Plan respect 15-day lookback |

### Data Quality Checks

- `DuplicateCount`, `NullCount` in `meta.dataquality`
- `ValidationStatus` PASS/FAIL per method
- Silver notebook exit JSON per-method `rows_read` / `rows_inserted` / `rows_updated`

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Other sites continue; method may partial-fail |
| Source table missing | Copy skipped for site | Lookup gate |
| Silver MERGE failure | Method fails at SL | Other 8 methods continue; audit partial finalize |
| TaskConfig empty | No sites to process | Filter returns empty |
| Audit finalize failure | Pipeline marked Failed | Review method JSON for FAILED entries |

### Retry Logic

- Pipeline activities: retry = 0 (default).

### Recovery Steps

1. Identify failed method/stage from `v_bronze_method_results_json` or `v_silver_method_results_json`.
2. Query `meta.taskaudit` and `meta.dataquality` for the ingest run ID.
3. Fix root cause (gateway, missing table, schema drift).
4. Re-run pipeline with new `RunId`.
5. If bad Silver merge confirmed, use Delta time travel on affected table before re-run.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric run history — parent and both child pipeline activity status.
- `meta.pipelinerun` — BR and SL layer status.

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%P1 Forms%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%P1 Forms%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (97, 98)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| No sites in Bronze | TaskConfig ConfigId 97 active? TaskConfig notebook output? |
| One method fails all sites | Bronze ForEach for that method — gateway connectivity |
| Silver SKIPPED | `v_bronze_method_results_json` — method FAILED at BR |
| Row count mismatch | `meta.dataquality` for method and ingest run |
| Pregnancy rows missing | All three source tables exist at clinic? |
| Duplicate Silver rows | Verify correct merge key (`EandMFormId` vs `Id`) |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **Parameter validation** | `p_lookback_days` (15), `p_work_date`, `p_ingest_run_id` (if override) |
| **TaskConfig active** | ConfigId 97 rows active for target sites (~115 × 9 methods) |
| **Gateway capacity** | 9 parallel Bronze methods × batchCount 3 |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor Succeeded (or Failed with expected partial failure) |
| **Bronze completion** | Rows in each `P1Forms.br_samms_*` table for ingest run |
| **Silver merge** | Row counts in all nine `pats.tbl_*` Silver tables |
| **Audit tables** | TaskQueue, TaskAudit, DataQuality populated |
| **Per-method JSON** | No unexpected FAILED in bronze/silver result variables |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%P1 Forms%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%P1 Forms%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (97, 98)
  AND PipelineRunId = '<pipeline_run_id>';

-- Bronze row count for one method
SELECT SiteCode, COUNT(*) AS row_count
FROM P1Forms.br_samms_comprehensive_assessment_form
WHERE IngestRunId = '<pipeline_run_id>'
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_execute_forms` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — 9 parallel ForEach blocks**

*[Insert screenshot]*

**3. Silver child pipeline — 9 parallel notebooks**

*[Insert screenshot]*

**4. TaskConfig notebook activity configuration**

*[Insert screenshot]*

**5. Audit start notebook parameters**

*[Insert screenshot]*

**6. Copy activity — source connection and dynamic SQL (Comprehensive Assessment example)**

*[Insert screenshot]*

**7. Copy activity — Bronze destination mapping**

*[Insert screenshot]*

**8. Silver notebook — Delta MERGE logic**

*[Insert screenshot]*

**9. IfCondition — audit finalize and optional Gold branches**

*[Insert screenshot]*

**10. Pipeline monitoring — successful execution**

*[Insert screenshot]*

**11. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

**12. Unit test comparison — Fabric Silver vs BHG_DR row counts (test sites)**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS P1 Forms ETL | v1.0*
