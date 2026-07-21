# SAMMS P1 Reference ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_execute_reference` |
| **Parent Pipeline Object ID** | `936a06e1-7a7b-4a9a-b04f-0ba9ccd0a0cc` |
| **Bronze Child Pipeline** | `pl_reference` (`27f7d826-722f-4eef-a260-17ae96a47fec`) |
| **Silver Child Pipeline** | `pl_reference_bronz_to_silver` (`c7cd6646-0b16-4b18-901b-e080e8d91629`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 10/07/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS P1 Reference / lookup data from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — nine reference tables are published to the Silver lakehouse for downstream reporting and analytics.

**Nine reference methods processed:**

| Method | Description |
|--------|-------------|
| `SaveClinic` | Clinic settings and configuration |
| `Save3pSetup` | Third-party setup records |
| `SaveCodes` | Billing / service codes |
| `SaveServices` | Service definitions |
| `SavedropDownListItems` | Dropdown list values |
| `SaveCustomAnswers` | Custom question answers |
| `SaveCustomQuestions` | Custom question definitions |
| `SavePreAdmissionV6` | Pre-admission V6 forms |
| `SavePreAdminReferrals` | Pre-admission referral source |

**Legacy context:** Part of Regional ETL P1 (`BHGTaskRunner.exe 2`).

---

## 2. Solution Overview

### Business Objective

Ingest, cleanse, and standardize clinic-wide reference and configuration data from SAMMS into Fabric. This data supports billing codes, clinic settings, custom Q&A, services, dropdown lists, and pre-admission intake reference tables used across the enterprise data platform.

### End-to-End Data Flow

1. **Extract** raw SQL Server reference data via Copy Data activities (Bronze child — 9 parallel method loops × ~115 sites).
2. **Transform and merge** Bronze data into Silver using nine parallel PySpark notebooks with method-specific Delta MERGE rules (Silver child).
3. **Audit and notify** — pipeline run, task queue, data quality, and failure alerts written to control tables.

### Source Systems

- On-premises SAMMS SQL Server databases (one database per clinic, ~115 active sites).
- Connection via Fabric on-premises data gateway.

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `P1Reference` (append by `IngestRunId`).
- **Silver (final):** `bhg_silver` Lakehouse — schemas `ctrl`, `pats`, `ayx`.

### Overall Architecture Diagram

```
pl_execute_reference (PARENT)
|
+- nb_get_p1_reference_taskconfig
+- flt_active_p1_reference_sites
+- nb_p1_reference_audit_start
|
+- Executed_AfterBronz -> pl_reference (BRONZE CHILD)
|     +- 9 x (Filter -> ForEach sites -> Lookup -> If -> Copy -> site marker)
|
+- set_bronze_method_results_from_child
+- Executed_AfterSilver -> pl_reference_bronz_to_silver (SILVER CHILD)
|     +- 9 parallel Silver MERGE notebooks
|
+- set_silver_method_results_from_child
+- if_all_reference_methods_success
|     +- TRUE  -> nb_p1_reference_audit_finalize_success
|     +- FALSE -> nb_p1_reference_audit_finalize_failure
+- nb_p1_reference_notify_failed (on IfCondition Failed/Skipped)
```

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_execute_reference`)

---

#### Activity 1: Load Task Configuration

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_p1_reference_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `6e7b4814-5818-4715-9275-f6ad72743221` |
| **Purpose** | Reads `meta.taskconfig` for P1 Reference methods and returns a slim JSON array to drive pipeline execution. Avoids Fabric Lookup 4 MB output limit. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |
| **Input** | `bhg_bronze.meta.taskconfig` |
| **Output** | JSON array via notebook exit — `TaskConfigId`, `ConfigId`, `TaskName`, `Method`, `SourceTable`, `SiteCode`, `DataBaseName`, `TargetTable`, `IsActive` |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_config_ids_json` | `[88]` |
| `p_methods_json` | `["SaveClinic","Save3pSetup","SaveCodes","SaveServices","SavedropDownListItems","SaveCustomAnswers","SaveCustomQuestions","SavePreAdmissionV6","SavePreAdminReferrals"]` |
| `p_only_active` | `true` |
| `p_require_site` | `true` |
| `p_require_database` | `true` |
| `p_require_source_table` | `true` |


---

#### Activity 2: Filter Active Reference Sites

| Field | Value |
|-------|-------|
| **Activity Name** | `flt_active_p1_reference_sites` |
| **Activity Type** | Filter |
| **Purpose** | Keeps only active Bronze ConfigId 88 rows for the nine reference methods with populated `SiteCode`, `DataBaseName`, and `SourceTable`. |
| **Execution Sequence** | 2 |
| **Dependencies** | `nb_get_p1_reference_taskconfig` (Succeeded) |
| **Input** | `@json(activity('nb_get_p1_reference_taskconfig').output.result.exitValue)` |
| **Output** | Filtered JSON array — authoritative site/method list for this run |


---

#### Activity 3: Start Pipeline Audit

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_reference_audit_start` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `32503e1c-b4a8-4f36-8bfa-988562250c2f` |
| **Purpose** | Initiates audit logging — creates `PipelineRun` and `TaskQueue` rows for Bronze (~1,035 site×method tasks) and Silver (9 method tasks). |
| **Execution Sequence** | 3 |
| **Dependencies** | `flt_active_p1_reference_sites` (Succeeded) |
| **Input** | Active ETL config (ConfigId 88, 89), TaskConfig rows |
| **Output** | Audit context JSON via notebook exit (`run_id`, task metadata) |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS P1 Reference` |
| `p_pipeline_name` | `pl_execute_reference` |
| `p_pipeline_path` | `/pipelines/pl_execute_reference` |
| `p_triggered_by` | `Fabric` |


---

#### Activity 4: Bronze Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterBronz` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_reference` |
| **Purpose** | Extracts reference data from SAMMS clinic databases and lands it in Bronze lakehouse tables. Nine methods run in parallel. |
| **Execution Sequence** | 4 |
| **Dependencies** | `nb_p1_reference_audit_start` (Succeeded) |
| **Input** | Filtered site list, ingest run ID, work date, lookback days |
| **Output** | Child `pipelineReturnValue` with per-method Bronze status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | `@if(empty, pipeline().RunId, p_ingest_run_id)` |
| `p_work_date` | `@pipeline().parameters.p_work_date` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (default 15) |
| `p_sites` | `@activity('flt_active_p1_reference_sites').output.value` |
| `waitOnCompletion` | `true` |


---

#### Activity 5: Capture Bronze Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_bronze_method_results_from_child` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Bronze child return JSON in parent variable `v_bronze_method_results_json` for Silver, audit, and notification. |
| **Execution Sequence** | 5 |
| **Dependencies** | `Executed_AfterBronz` (**Completed** — not Succeeded only) |
| **Input** | Child `pipelineReturnValue['v_bronze_method_results_json']` |
| **Output** | Pipeline variable `v_bronze_method_results_json` |

**Why SetVariable:** Fabric child return values are not directly referenceable in later parent activities without assignment to a pipeline variable.


---

#### Activity 6: Silver Orchestration (Invoke Child Pipeline)

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterSilver` |
| **Activity Type** | Invoke Pipeline (Execute Pipeline) |
| **Child Pipeline** | `pl_reference_bronz_to_silver` |
| **Purpose** | Runs nine parallel Silver notebooks — Delta MERGE from Bronze into final Silver tables. |
| **Execution Sequence** | 6 |
| **Dependencies** | `set_bronze_method_results_from_child` (Succeeded) |
| **Input** | Ingest run ID, bronze method results JSON, sites JSON |
| **Output** | Child `pipelineReturnValue` with per-method Silver status JSON |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_ingest_run_id` | Pipeline RunId or override |
| `p_bronze_method_results_json` | `@variables('v_bronze_method_results_json')` |
| `p_sites_json` | `@string(activity('flt_active_p1_reference_sites').output.value)` |


---

#### Activity 7: Capture Silver Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_silver_method_results_from_child` |
| **Activity Type** | SetVariable |
| **Purpose** | Stores Silver child return JSON in parent variable `v_silver_method_results_json`. |
| **Execution Sequence** | 7 |
| **Dependencies** | `Executed_AfterSilver` (**Completed**) |
| **Input** | Child `pipelineReturnValue['v_silver_method_results_json']` |
| **Output** | Pipeline variable `v_silver_method_results_json` |


---

#### Activity 8: Audit Finalize (Conditional)

| Field | Value |
|-------|-------|
| **Activity Name** | `if_all_reference_methods_success` |
| **Activity Type** | IfCondition |
| **Purpose** | Routes to success or failure audit finalize based on bronze and silver method result JSON. |
| **Execution Sequence** | 8 |
| **Dependencies** | `set_silver_method_results_from_child` (Succeeded) |
| **Condition** | Neither `v_bronze_method_results_json` nor `v_silver_method_results_json` contains `FAILED`, `ERROR`, or `SKIPPED` |

**If TRUE — Activity 8a: Success Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_reference_audit_finalize_success` |
| **Activity Type** | Notebook |
| **Purpose** | Marks all tasks SUCCESS; writes DataQuality rows for Bronze and Silver. |
| **Configuration** | `p_mode = FINALIZE_SUCCESS`, `p_status = SUCCESS` |

**If FALSE — Activity 8b: Failure Audit**

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_reference_audit_finalize_failure` |
| **Activity Type** | Notebook |
| **Purpose** | Per-method partial finalize — successful methods marked SUCCESS + DQ; failed methods marked FAILED. Raises exception so Fabric marks pipeline Failed. |
| **Configuration** | `p_mode = FINALIZE_FAILURE`, `p_status = FAILED` |


---

#### Activity 9: Failure Notification (Alternative Path)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_reference_notify_failed` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `77c87686-120d-486b-9146-6a794d794e38` |
| **Purpose** | Sends failure alert with per-method BR/SL error detail when finalize path fails or is skipped. |
| **Execution Sequence** | 9 (alternative path) |
| **Dependencies** | `if_all_reference_methods_success` (Failed or Skipped) |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `Pipeline_Name` | `P1 Reference ETL` |
| `Status` | `Failed` |
| `Config_Name` | `SAMMS P1 Reference` |
| `Source_System` | `SAMMS` |
| `Target_Name` | `ALL` |
| `Environment_Name` | `DEV` |
| `Run_id` | `@pipeline().RunId` |


---

### Bronze Child Pipeline (`pl_reference`) — Per-Method Pattern (×9)

Each of the nine reference methods follows the same pattern. All nine ForEach blocks start in parallel. A final SetVariable aggregates status after all nine **Complete**.

| Method | Filter Activity | ForEach Activity | Lookup | If | Copy Activity | Bronze Table |
|--------|----------------|------------------|--------|-----|---------------|--------------|
| `SaveClinic` | `flt_child_clinic_sites` | `fe_each_samms_site_clinic` | `lkp_check_clinic` | `if_clinic_exists` | `cp_clinic_to_bronze` | `br_samms_clinic` |
| `Save3pSetup` | `flt_child_3p_setup_sites` | `fe_each_samms_site_3p_setup` | `lkp_check_3p_setup` | `if_3p_setup_exists` | `cp_3p_setup_to_bronze` | `br_samms_3p_setup` |
| `SaveCodes` | `flt_child_codes_sites` | `fe_each_samms_site_codes` | `lkp_check_codes` | `if_codes_exists` | `cp_codes_to_bronze` | `br_samms_codes` |
| `SaveServices` | `flt_child_services_sites` | `fe_each_samms_site_services` | `lkp_check_services` | `if_services_exists` | `cp_services_to_bronze` | `br_samms_services` |
| `SavedropDownListItems` | `flt_child_dropdown_list_items_sites` | `fe_each_samms_site_dropdown_list_items` | `lkp_check_dropdown_list_items` | `if_dropdown_list_items_exists` | `cp_dropdown_list_items_to_bronze` | `br_samms_dropdown_list_items` |
| `SaveCustomAnswers` | `flt_child_custom_answers_sites` | `fe_each_samms_site_custom_answers` | `lkp_check_custom_answers` | `if_custom_answers_exists` | `cp_custom_answers_to_bronze` | `br_samms_custom_answers` |
| `SaveCustomQuestions` | `flt_child_custom_questions_sites` | `fe_each_samms_site_custom_questions` | `lkp_check_custom_questions` | `if_custom_questions_exists` | `cp_custom_questions_to_bronze` | `br_samms_custom_questions` |
| `SavePreAdmissionV6` | `flt_child_pre_admission_v6_sites` | `fe_each_samms_site_pre_admission_v6` | `lkp_check_pre_admission_v6` | `if_pre_admission_v6_exists` | `cp_pre_admission_v6_to_bronze` | `br_samms_pre_admission_v6` |
| `SavePreAdminReferrals` | `flt_child_preadmission_referral_source_sites` | `fe_each_samms_site_preadmission_referral_source` | `lkp_check_preadmission_referral_source` | `if_preadmission_referral_source_exists` | `cp_preadmission_referral_source_to_bronze` | `br_samms_preadmission_referral_source` |

#### Bronze Activity Pattern (per method, per site)

| Step | Activity | Type | Purpose |
|------|----------|------|---------|
| 1 | `flt_child_*_sites` | Filter | Filter parent `p_sites` to one method |
| 2 | `fe_each_samms_site_*` | ForEach | Iterate sites — `isSequential: false`, `batchCount: 3` |
| 3 | `lkp_check_*` | Lookup | Verify source table exists in clinic SAMMS database |
| 4 | `if_*_exists` | IfCondition | Run Copy only when table exists |
| 5 | `cp_*_to_bronze` | Copy | Dynamic SELECT with metadata columns → Append to Bronze |
| 6 | Site success marker | Copy | Write row to `br_p1_reference_site_success` |

#### Bronze Aggregate Activity

| Field | Value |
|-------|-------|
| **Activity Name** | `set_child_bronze_method_results` |
| **Activity Type** | SetVariable |
| **Purpose** | After all nine ForEach loops **Complete**, builds per-method status JSON on `pipelineReturnValue`. |
| **Dependencies** | All nine ForEach activities (Completed) |
| **Output** | `pipelineReturnValue['v_bronze_method_results_json']` |


---

### Silver Child Pipeline (`pl_reference_bronz_to_silver`) — Nine Parallel Notebooks

| Notebook | Object ID | Method | Silver Target |
|----------|-----------|--------|---------------|
| `nb_p1_reference_sl_save_clinic` | `12ef735e-a19d-43db-8d6f-f76b62228859` | `SaveClinic` | `bhg_silver.ctrl.tbl_Clinic` |
| `nb_p1_reference_sl_save_3p_setup` | `1405a051-c662-4b1f-9078-1d21af91033f` | `Save3pSetup` | `bhg_silver.ctrl.tbl_3PSETUP` |
| `nb_p1_reference_sl_save_codes` | `2c9662e0-db8e-4f56-af65-099ebb7abbc9` | `SaveCodes` | `bhg_silver.pats.tbl_Codes` |
| `nb_p1_reference_sl_save_services` | `7ee00480-e57c-4055-ade8-581460bdaf8f` | `SaveServices` | `bhg_silver.pats.tbl_SERVICES` |
| `nb_p1_reference_sl_save_dropdown_list_items` | `b00aa878-ae47-4ae6-b623-bd827ac55f5f` | `SavedropDownListItems` | `bhg_silver.ctrl.tbl_DroDownListItems` |
| `nb_p1_reference_sl_save_custom_answers` | `744818c1-b78a-424f-b4a6-eeef89bfd226` | `SaveCustomAnswers` | `bhg_silver.pats.tbl_CustomAnswers` |
| `nb_p1_reference_sl_save_custom_questions` | `4f4ef829-feb7-4ced-8357-0deed45b6b30` | `SaveCustomQuestions` | `bhg_silver.pats.tbl_CustomQuestions` |
| `nb_p1_reference_sl_save_pre_admission_v6` | `222b75bf-57f3-4c8a-98d0-d45aa274425e` | `SavePreAdmissionV6` | `bhg_silver.ayx.tbl_PreAdmission_V6` |
| `nb_p1_reference_sl_save_preadmin_referrals` | `b17be974-02b7-4fac-8f4f-15fdb25974d1` | `SavePreAdminReferrals` | `bhg_silver.pats.tbl_PreadmissionReferralSource` |

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
| 1 | `SaveClinic` | `dbo.tblClinic` | Full | `WHERE 1 = 1` — full table extract |
| 2 | `Save3pSetup` | `dbo.tbl3PSETUP` | Full | Full extract; `RowChkSum = CHECKSUM(...)` computed in SELECT |
| 3 | `SaveCodes` | `dbo.tblCodes` | Full | Full extract; `RowChkSum` computed in SELECT |
| 4 | `SaveServices` | `dbo.tblSERVICES` | Full | Full extract; `RowChkSum` computed in SELECT |
| 5 | `SavedropDownListItems` | `dbo.DroDownListItems` | Full | Full extract; `RowChkSum` computed in SELECT |
| 6 | `SaveCustomAnswers` | `dbo.tblCUSTOMANSWERS` | Full | Full extract; `RowChkSum` computed in SELECT |
| 7 | `SaveCustomQuestions` | `dbo.tblCUSTOMQUESTIONS` | Full | Full extract; `RowChkSum` computed in SELECT |
| 8 | `SavePreAdmissionV6` | `dbo.SF_PatientPreAdmission` | Full | Full extract with joins to client table |
| 9 | `SavePreAdminReferrals` | `dbo.SF_PatientPreadmissionReferralSource` | Incremental | **515-day lookback** — `LookbackDate = DATEADD(day, -(p_lookback_days + 500), GETDATE())` |

**Source table existence check:** Bronze Lookup verifies table exists before Copy. Missing tables are skipped gracefully (some clinics on different SAMMS versions).


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
| `P1Reference` | `br_samms_clinic` | Append (tagged by `IngestRunId`) |
| `P1Reference` | `br_samms_3p_setup` | Append |
| `P1Reference` | `br_samms_codes` | Append |
| `P1Reference` | `br_samms_services` | Append |
| `P1Reference` | `br_samms_dropdown_list_items` | Append |
| `P1Reference` | `br_samms_custom_answers` | Append |
| `P1Reference` | `br_samms_custom_questions` | Append |
| `P1Reference` | `br_samms_pre_admission_v6` | Append |
| `P1Reference` | `br_samms_preadmission_referral_source` | Append |
| `P1Reference` | `br_p1_reference_site_success` | Append (site success markers) |

### Silver Destination (Final Layer)

| Schema | Table | Write Mode |
|--------|-------|------------|
| `ctrl` | `tbl_Clinic` | Delta MERGE |
| `ctrl` | `tbl_3PSETUP` | Delta MERGE |
| `pats` | `tbl_Codes` | Delta MERGE |
| `pats` | `tbl_SERVICES` | Delta MERGE |
| `ctrl` | `tbl_DroDownListItems` | Delta MERGE |
| `pats` | `tbl_CustomAnswers` | Delta MERGE |
| `pats` | `tbl_CustomQuestions` | Delta MERGE |
| `ayx` | `tbl_PreAdmission_V6` | Delta MERGE |
| `pats` | `tbl_PreadmissionReferralSource` | Delta MERGE |

**Note:** Gold ConfigId 90 may exist in control tables but is **not executed** by this pipeline. Silver is the terminal layer.


---

## 6. Notebook Documentation

### `nb_get_p1_reference_taskconfig`

| Field | Value |
|-------|-------|
| **Purpose** | Reads TaskConfig and returns slim JSON for pipeline runtime |
| **Parameters** | `p_config_ids_json`, `p_methods_json`, `p_only_active`, `p_require_site`, `p_require_database`, `p_require_source_table` |
| **Input** | `meta.taskconfig` |
| **Output** | JSON array via notebook exit |
| **Business Logic** | Filters to ConfigId 88, nine methods, active rows with required fields |
| **Error Handling** | Fails pipeline if TaskConfig unreadable |

---

### `nb_p1_reference_audit_start` / `_finalize_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `32503e1c-b4a8-4f36-8bfa-988562250c2f` |
| **Purpose** | Audit lifecycle — start runs, finalize success or per-method failure |
| **Parameters** | `p_mode`, `p_config_name_prefix`, `p_pipeline_name`, `p_pipeline_path`, `p_triggered_by`, `p_audit_context_json`, `p_ingest_run_id`, `p_sites_json`, `p_bronze_method_results_json`, `p_silver_method_results_json`, `p_status` |
| **Input Tables** | `meta.etlconfig`, `meta.taskconfig`, `meta.pipelinerun`, `meta.taskqueue` |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |
| **Business Logic** | START: creates ~1,035 Bronze + 9 Silver task rows. FINALIZE_SUCCESS: marks all SUCCESS + DQ. FINALIZE_FAILURE: per-method partial finalize; raises exception. |
| **Error Handling** | Failure finalize raises exception to mark pipeline Failed in Fabric monitor |

---

### Silver Notebooks (`nb_p1_reference_sl_save_*`) — Shared Pattern

| Field | Value |
|-------|-------|
| **Purpose** | Bronze → Silver Delta MERGE for one reference method |
| **Parameters** | `p_ingest_run_id`, `p_bronze_method_results_json`, `p_sites_json` |
| **Input Tables** | `bhg_bronze.P1Reference.br_samms_*` (filtered by `IngestRunId`) |
| **Output Tables** | `bhg_silver.{schema}.tbl_*` (final) |
| **Business Logic** | Load bronze → skip if method failed at bronze → align schema → method-specific pre-reset → Delta MERGE |
| **Merge/Upsert Logic** | Method-specific match keys; RowChkSum gate where legacy C# used checksum |
| **Error Handling** | Per-method isolation — one method failing does not block other eight |
| **Performance** | Shared `merge_to_silver` helper; Delta MERGE |

**Method-specific rules:**

| Method | Match Key | RowChkSum Gate | Notable Behavior |
|--------|-----------|----------------|------------------|
| `SaveClinic` | `SiteCode + PKEY` | No | Full upsert |
| `Save3pSetup` | `SiteCode + pID` | Yes | Update only when checksum changed |
| `SaveCodes` | `SiteCode + cdeID` | Yes | Update only when checksum changed |
| `SaveServices` | `SiteCode + sID` | Yes | Pre-reset `IsActive`; legacy insert scope rule |
| `SavedropDownListItems` | `SiteCode + Id` | No | Simple match upsert |
| `SaveCustomAnswers` | `SiteCode + caID + caQID + caCLTID` | Yes | Pre-reset `RowSate`; **caQID required in key** |
| `SaveCustomQuestions` | `SiteCode + cID` | Yes | Pre-reset `RowSate` |
| `SavePreAdmissionV6` | `SiteCode + PreAdmissionid + Clientid` | No | Updates matched rows every run |
| `SavePreAdminReferrals` | `SiteCode + Id` | No | Simple match upsert |


---

### `nb_p1_reference_notify_failed`

| Field | Value |
|-------|-------|
| **Purpose** | Sends failure notification on pipeline finalize failure |
| **Parameters** | `Pipeline_Name`, `Status`, `Config_Name`, `Source_System`, `Target_Name`, `Environment_Name`, `Run_id`, error detail |
| **Business Logic** | Parses bronze/silver method JSON; constructs operator-facing alert with failed method names and stages (BR/SL) |

---

## 7. Copy Activity Documentation

Each Bronze Copy activity follows the same pattern. Example: `cp_clinic_to_bronze`.

| Field | Value |
|-------|-------|
| **Source** | SAMMS SQL Server (clinic database via gateway) |
| **Destination** | `bhg_bronze.P1Reference.br_samms_*` |
| **Mapping** | Dynamic SQL — metadata columns prepended + source business columns |
| **Partitioning** | N/A |
| **Incremental Logic** | Full extract for 8 methods; `SavePreAdminReferrals` uses 515-day lookback filter in WHERE clause |
| **Retry Configuration** | 0 (pipeline default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |

### Metadata Columns Added in Every Bronze Copy

| Column | Purpose |
|--------|---------|
| `SiteCode` | Clinic identifier |
| `SourceDatabase` | SAMMS database name |
| `IngestRunId` | Pipeline run correlation |
| `ExtractedAt` | Extraction timestamp |
| `SourceQueryStartDate` | Lookback window start |
| `SourceQueryEndDate` | Lookback window end |
| `LookbackDate` | Effective lookback date |

### Site Success Marker Copy

After successful Copy, a marker row is written to `br_p1_reference_site_success` with `Method`, `SiteCode`, `SourceDatabase`, `IngestRunId`, `MarkedAt`.


---

## 8. PySpark Transformations

### Data Cleansing (Bronze → Silver)

- Filter Bronze rows to current `IngestRunId` only.
- Deduplicate within run if needed (keep latest `ExtractedAt`).
- Align column names and types to Silver target schema.
- Default null keys where legacy C# did (method-specific).

### Business Rules Implemented (Silver)

| Rule | Methods | Description |
|------|---------|-------------|
| RowChkSum gate | 3pSetup, Codes, Services, CustomAnswers, CustomQuestions | UPDATE matched rows only when source `RowChkSum` differs from Silver |
| IsActive pre-reset | Services | Set `IsActive = false` for site before merge; reactivate matches |
| RowSate pre-reset | CustomAnswers, CustomQuestions | Set `RowSate = 0` for site before merge |
| Legacy insert scope | Services | Do not insert brand-new `sID` when site already has service rows |
| RowState from IsDeleted | PreAdmissionV6 | Derive `RowState` from source delete flag |
| Composite key | CustomAnswers | Match key must include `caQID` alongside `caID` and `caCLTID` |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **MERGE — Matched** | Update all columns (full refresh or RowChkSum-gated per method) |
| **MERGE — Not Matched** | INSERT new business key |
| **Pre-pass UPDATE** | RowSate / IsActive reset before merge (CustomAnswers, CustomQuestions, Services) |

### Performance Optimizations

- Nine Bronze ForEach loops run in parallel (one per method).
- Site batching: `batchCount = 3` limits concurrent gateway connections.
- Nine Silver notebooks run in parallel with no cross-notebook dependency.
- Shared `merge_to_silver` helper reduces code duplication.

### Error Handling

- Per-method isolation in both Bronze ForEach and Silver notebooks.
- Bronze child returns partial status JSON even when some methods fail (`dependsOn: Completed`).
- Silver skips methods where Bronze status is FAILED.
- Audit finalize handles partial success per method.


---

## 9. Parameters and Variables

### Parent Pipeline Parameters (`pl_execute_reference`)

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | (empty → `pipeline().RunId`) | Tags Bronze rows; filters Silver; audit correlation |
| `p_work_date` | string | Run date | Business date metadata on Bronze rows |
| `p_lookback_days` | int | 15 | Bronze query metadata; PreAdminReferrals adds 500 days |

### Parent Pipeline Variables

| Variable | Set By | Used By |
|----------|--------|---------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver child, audit finalize, notify |
| `v_silver_method_results_json` | `set_silver_method_results_from_child` | Audit finalize, notify |

### Bronze Child Parameters (`pl_reference`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Metadata column on every Bronze row |
| `p_work_date` | string | Business date |
| `p_lookback_days` | int | Lookback metadata (515-day for PreAdminReferrals) |
| `p_sites` | array | Filtered TaskConfig site list from parent |

### Silver Child Parameters (`pl_reference_bronz_to_silver`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Filter Bronze to current run |
| `p_bronze_method_results_json` | string | Skip failed Bronze methods |
| `p_sites_json` | string | Active site list for per-site result reporting |

### Child Return Values (`pipelineReturnValue`)

| Child | Return Key | Set By |
|-------|------------|--------|
| `pl_reference` | `v_bronze_method_results_json` | `set_child_bronze_method_results` |
| `pl_reference_bronz_to_silver` | `v_silver_method_results_json` | `set_silver_method_results_return` |

### Pipeline Built-in Variables

| Variable | Usage |
|----------|-------|
| `@pipeline().Pipeline` | Pipeline name in audit |
| `@pipeline().RunId` | Default ingest run ID |
| `@pipeline().TriggerTime` | Notification timestamp |

---

## 10. Dependencies

### Activity Execution Order (Parent)

```
nb_get_p1_reference_taskconfig
  → flt_active_p1_reference_sites
  → nb_p1_reference_audit_start
  → Executed_AfterBronz (pl_reference)
  → set_bronze_method_results_from_child
  → Executed_AfterSilver (pl_reference_bronz_to_silver)
  → set_silver_method_results_from_child
  → if_all_reference_methods_success
      +- TRUE  -> nb_p1_reference_audit_finalize_success
      +- FALSE -> nb_p1_reference_audit_finalize_failure
  → nb_p1_reference_notify_failed (if IfCondition Failed/Skipped)
```

### Notebook Dependencies

| Notebook | Depends On |
|----------|------------|
| Silver notebooks | Bronze completion for same method; reads `p_bronze_method_results_json` |
| Audit finalize | Both bronze and silver result JSON variables |
| Notify | IfCondition failure (typically finalize_failure raised exception) |

### Table Dependencies

| Table | Dependency |
|-------|------------|
| `meta.taskconfig` | Must have active ConfigId 88 (Bronze) and 89 (Silver) rows |
| `meta.etlconfig` | ConfigId 88, 89 must be active |
| `br_p1_reference_site_success` | Written during Bronze; consulted during Silver |
| Silver target tables | Auto-created on first run if not present |

### External Dependencies

- On-premises SAMMS SQL Server availability via data gateway.
- Fabric workspace and lakehouses online (`bhg_bronze`, `bhg_silver`).

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| TaskConfig `IsActive = 0` | Site/method excluded at Filter step |
| Source table missing at clinic | Bronze Lookup returns 0 → Copy skipped for that site |
| Bronze method FAILED | Silver notebook exits SKIPPED for that method |
| Any method FAILED/ERROR/SKIPPED in result JSON | Audit routes to FINALIZE_FAILURE branch |

---

## 11. Validation

### Source Validation

- Bronze Lookup verifies source table exists before Copy.
- TaskConfig filter ensures `SiteCode`, `DataBaseName`, `SourceTable` are populated.

### Row Count Validation

- Audit `DataQuality` table records source vs target row counts per method after finalize.
- Silver notebook exit JSON includes `rows_read`, `rows_inserted`, `rows_updated`, `rows_skipped`.

### Duplicate Checks

- `DuplicateCount` metric written to `meta.dataquality` during Silver processing.
- CustomAnswers match key must include `caQID` to prevent incorrect merges.

### Null Checks

- `NullCount` metric written to `meta.dataquality` for primary key columns.

### Business Validations

| Validation | Detail |
|------------|--------|
| Match key coverage | TaskConfig `dq_keys` must match Silver MERGE keys |
| CustomAnswers key | Must be `SiteCode + caID + caQID + caCLTID` |
| RowChkSum presence | Required for methods that gate updates on checksum |
| Site success markers | Bronze sites with zero rows still marked successful via marker table |

### Data Quality Checks

- `ValidationStatus` recorded as PASS or FAIL per method in `meta.dataquality`.
- Per-method JSON status (`SUCCESS` / `FAILED` / `SKIPPED`) verified in pipeline variables.

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS connection failure | Site Copy fails | Site skipped; method may partial-fail; status in bronze JSON |
| Source table missing | Copy skipped for site | Lookup gate; site not in success marker |
| Silver MERGE failure | Method fails at SL | Other 8 methods continue; audit partial finalize |
| TaskConfig empty | No sites to process | Filter returns empty; pipeline may succeed with zero work |
| Audit finalize failure | Pipeline marked Failed | Notify notebook sends alert |

### Retry Logic

- Pipeline activities: retry = 0 (default).
- Site success marker Copy: retry = 3.

### Exception Handling

- Silver notebooks: per-method try/except — logs traceback, returns FAILED status in exit JSON.
- Audit finalize failure: raises exception after writing audit rows so Fabric monitor shows Failed.

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
3. Fix root cause (gateway, missing table, schema drift).
4. Re-run pipeline — records tagged with new `IngestRunId` prevent duplicate merges.
5. If bad Silver merge confirmed, use Delta time travel on affected table before re-run.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric pipeline run history — activity-level status for parent and both child pipelines.
- `meta.pipelinerun` — `Status`, `RestartFlag`, `AttemptNumber` per layer (BR, SL).

### Notebook Monitoring

- Silver notebook exit JSON — per-method `rows_read`, `rows_inserted`, `rows_updated`, `site_results`.
- Audit notebook output — task queue IDs and finalize status.

### Execution History

- `meta.taskqueue` — ~1,035 Bronze tasks + 9 Silver tasks per run.
- `meta.taskaudit` — detailed step logs with row counts.

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%P1 Reference%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%P1 Reference%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (88, 89)` AND ingest run filter |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| No sites in Bronze | TaskConfig ConfigId 88 active? TaskConfig notebook output? |
| One method fails all sites | Bronze ForEach for that method — gateway connectivity |
| Silver SKIPPED | `v_bronze_method_results_json` — method FAILED at BR |
| Row count mismatch | `meta.dataquality` for method and ingest run |
| CustomAnswers duplicates | Match key includes `caQID`? |
| Audit FAILED but data looks fine | Result JSON contains `SKIPPED` string? |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | On-prem SAMMS SQL databases accessible via data gateway |
| **Required access/permissions** | Gateway service account has read access to all clinic databases |
| **Environment readiness** | Fabric workspace online; `bhg_bronze` and `bhg_silver` lakehouses available |
| **Parameter validation** | `p_lookback_days`, `p_work_date`, `p_ingest_run_id` (if override) set correctly |
| **Dependency validation** | `meta.etlconfig` ConfigId 88, 89 active; `meta.taskconfig` Bronze rows active for target sites |
| **Required datasets availability** | Silver target tables exist or auto-create is enabled |
| **Previous execution status** | No conflicting re-run with same `p_ingest_run_id` override |
| **Gateway capacity** | 9 parallel Bronze methods × batchCount 3 — confirm gateway can handle concurrent connections |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor shows Succeeded (or Failed with expected partial failure) |
| **Activity completion status** | All parent activities Completed; review any Failed ForEach sites in Bronze child |
| **Source-to-target record count** | Compare `meta.taskaudit` rows read vs written per method |
| **Data quality validation** | Review `meta.dataquality` — `DuplicateCount`, `NullCount`, `ValidationStatus` |
| **Target table validation** | Query Silver tables for expected site coverage; verify RowSate/IsActive flags on applicable methods |
| **Log review** | Check `meta.taskaudit` for SKIPPED methods; review Silver notebook exit JSON |
| **Error verification** | If Failed — review `nb_p1_reference_notify_failed` alert and finalize audit output |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%P1 Reference%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%P1 Reference%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (88, 89)
  AND PipelineRunId = '<pipeline_run_id>';
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_execute_reference` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — 9 parallel ForEach blocks**

*[Insert screenshot]*

**3. Silver child pipeline — 9 parallel notebooks**

*[Insert screenshot]*

**4. TaskConfig notebook activity configuration**

*[Insert screenshot]*

**5. Audit start notebook parameters**

*[Insert screenshot]*

**6. Copy activity — source connection and dynamic SQL**

*[Insert screenshot]*

**7. Copy activity — Bronze destination mapping**

*[Insert screenshot]*

**8. Silver notebook — Delta MERGE logic**

*[Insert screenshot]*

**9. Silver notebook — RowChkSum gate or pre-reset logic**

*[Insert screenshot]*

**10. IfCondition — audit finalize branches**

*[Insert screenshot]*

**11. Pipeline monitoring — successful execution**

*[Insert screenshot]*

**12. Validation results — TaskQueue / TaskAudit / DataQuality query output**

*[Insert screenshot]*

---

*Microsoft Fabric | Developer Workflow Documentation | SAMMS P1 Reference ETL | v1.0*
