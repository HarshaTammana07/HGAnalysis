# Microsoft Fabric — Pipeline Documentation — SAMMS P1 Forms ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS P1 Forms ETL Pipeline |
| **Pipeline ID** | `pl_execute_forms` (`a288b29f-c5ae-4994-b131-2dfb0af137e8`) |
| **Bronze Child Pipeline ID** | `pl_p1_forms` (`b934fec7-7208-4f3a-a621-b9bd414aed1f`) |
| **Silver Child Pipeline ID** | `pl_p1_forms_bronze_to_silver` (`4c0bd0aa-4771-4471-9789-2b126c788a31`) |
| **Version** | v1.0 |
| **Author** | [Name] |
| **Department** | Developer |
| **Created Date** | 29/07/2026 |
| **Last Updated** | 29/07/2026 |
| **Status** | Draft |
| **Environment** | Dev |

---

## 1. Document Control

### Version History

| Version | Date | Author | Change Summary | Approved By |
|---------|------|--------|----------------|-------------|
| v1.0 | 29/07/2026 | [Name] | Initial draft — generated from SAMMS P1 Forms Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS P1 Forms ETL pipeline migrates and modernizes the legacy Regional ETL Phase 1 clinical-forms process (`BHGTaskRunner.exe 2`) within Microsoft Fabric. It extracts nine SAMMS form types from 115+ per-clinic SQL Server databases and loads them into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

Unlike the legacy C# `Save*` form services in `SaveFormQAData` — where extraction and upsert logic were tightly coupled per table — the Fabric implementation separates orchestration into a parent pipeline, a Bronze child (nine parallel method loops), a Silver child (nine parallel merge notebooks), and a reusable audit framework. This improves maintainability, scalability, and operational monitoring while preserving existing business logic: method-specific merge keys, RowState / IsDeleted handling, incremental lookback where configured, and **partial success** when one form type fails at one clinic.

The pipeline processes **nine form methods** across all active clinic sites registered in `meta.taskconfig` (ConfigId 97).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds clinical intake, E&M, consent, take-home, and discharge form reporting |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | Nine Copy activities per method inside Bronze child — `cp_*_to_bronze` (one per site per method) |
| **Copy Job Object ID** | Embedded in child pipeline `pl_p1_forms` |
| **Job Mode** | Batch (Bronze); mix of full-table and 15-day incremental extracts |
| **Write Behavior** | Bronze: Append (tagged by `IngestRunId`); Silver: Delta MERGE per method |
| **Enable Staging** | No (Bronze Copy) |
| **Table Option** | Silver tables pre-created or created on first merge run |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy and Lookup default); notebook retry per activity policy |

### Data Flow

Source (per-clinic SAMMS SQL Server) → Bronze child (`pl_p1_forms`) → Bronze Lakehouse → Silver child (`pl_p1_forms_bronze_to_silver`) → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | Nine clinical form tables (assessment, E&M, consent, take-home, discharge, etc.) |
| **Bronze** | `pl_p1_forms` — 9 parallel Filter + ForEach + Lookup + Copy | Table-existence gate; success inferred from Bronze rows; `batchCount = 3` |
| **Silver** | `pl_p1_forms_bronze_to_silver` — 9 parallel notebooks | Method-specific Delta MERGE; TaskConfig-driven keys |
| **Destination** | Fabric Silver Lakehouse | Schema `pats` — nine form tables; terminal layer for consumers |

### Parent Pipeline Activity Sequence

```
nb_get_p1_forms_taskconfig
  → flt_active_p1_forms_sites
  → nb_p1_forms_audit_start
  → Executed_AfterBronz (pl_p1_forms)
  → set_bronze_method_results_from_child
  → Executed_AfterSilver (pl_p1_forms_bronze_to_silver)
  → set_silver_method_results_from_child
  → if_all_forms_methods_success
      → flt_active_p1_forms_gold → nb_p1_forms_optional_gold_publish (optional)
      → nb_p1_forms_audit_finalize_success / _failure
  → nb_p1_forms_notify_failed (Inactive)
```

---

## 4. Source System

### Connection Details

| Field | Value |
|-------|-------|
| **Source Type** | SQL Server (SAMMS — one database per clinic) |
| **Server / Host** | On-premises via Fabric data gateway |
| **Database Name** | Per clinic — from TaskConfig `DataBaseName` |
| **Connection ID (Fabric)** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Authentication** | Fabric linked service / gateway |

### Source Tables

| # | Method | Source Table | Bronze Table | Row Count | Size | Has PK? |
|---|--------|--------------|--------------|-----------|------|---------|
| 1 | `SaveComprehensiveAssessmentForm` | `dbo.ComprehensiveAssessmentForm` | `br_samms_comprehensive_assessment_form` | [Per site] | [MB/GB] | [Yes/No] |
| 2 | `SaveEMFormPregnancy` | `dbo.EandMFormPregnancy` (+ joins) | `br_samms_eandm_form_pregnancy` | [Per site] | [MB/GB] | [Yes/No] |
| 3 | `SaveEMFormMDM` | `dbo.EandMForm` | `br_samms_eandm_form_mdm` | [Per site] | [MB/GB] | [Yes/No] |
| 4 | `SaveDataForms` | `dbo.SF_DataForms` | `br_samms_sf_data_forms` | [Per site] | [MB/GB] | [Yes/No] |
| 5 | `SaveSMSTextConsentForm` | `dbo.SMSTextConsentForm` | `br_samms_sms_text_consent_form` | [Per site] | [MB/GB] | [Yes/No] |
| 6 | `SaveConsenttoMarketing` | `dbo.consenttomarketing` | `br_samms_consent_to_marketing` | [Per site] | [MB/GB] | [Yes/No] |
| 7 | `SaveTakeHomeAgreementandDiversionControl` | `dbo.takehomeagreementanddiversioncontrol` | `br_samms_take_home_agreement_diversion_control` | [Per site] | [MB/GB] | [Yes/No] |
| 8 | `SaveTakeHomeRiskAssessment` | `dbo.TakeHomeRiskAssessment` | `br_samms_take_home_risk_assessment` | [Per site] | [MB/GB] | [Yes/No] |
| 9 | `SaveNewDischargeTransferPlanForm` | `dbo.newdischargetransferplanform` | `br_samms_new_discharge_transfer_plan_form` | [Per site] | [MB/GB] | [Yes/No] |

**Active sites:** ~115 clinics — one TaskConfig row per site per method (ConfigId 97). Typical Bronze task volume: ~1,035 site × method tasks per run.

### Load Strategy

| Method | Load Type | Incremental Logic |
|--------|-----------|-------------------|
| `SaveComprehensiveAssessmentForm` | Full | `WHERE 1 = 1` — full table extract |
| `SaveEMFormPregnancy` | Full | Joined extract (requires `EandMForm`, `EandMFormPregnancy`, `SF_PatientPreAdmission`) |
| `SaveEMFormMDM` | Full | Full extract from `EandMForm` |
| `SaveDataForms` | Incremental | **15-day lookback** (`p_lookback_days`, default 15) |
| `SaveSMSTextConsentForm` | Full | Full extract |
| `SaveConsenttoMarketing` | Incremental | **15-day lookback** |
| `SaveTakeHomeAgreementandDiversionControl` | Incremental | **15-day lookback** |
| `SaveTakeHomeRiskAssessment` | Full | Full extract |
| `SaveNewDischargeTransferPlanForm` | Incremental | **15-day lookback** |

**Source table gate:** Bronze Lookup verifies table(s) exist before Copy. Missing tables are skipped per site. **EM Pregnancy** requires all three tables: `EandMForm`, `EandMFormPregnancy`, `SF_PatientPreAdmission`.

**RowChkSum:** `CHECKSUM(*)` computed in Bronze Copy SELECT where applicable (e.g. Comprehensive Assessment uses `c.*` with checksum).

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `P1Forms` (Bronze); `pats` (Silver) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (method-specific keys) |

### Source-to-Target Mapping

| Source Table | Bronze Table | Silver Table (Final) |
|--------------|--------------|----------------------|
| `dbo.ComprehensiveAssessmentForm` | `P1Forms.br_samms_comprehensive_assessment_form` | `bhg_silver.pats.tbl_ComprehensiveAssessmentForm` |
| `dbo.EandMFormPregnancy` | `P1Forms.br_samms_eandm_form_pregnancy` | `bhg_silver.pats.tbl_EandMFormPregnancy` |
| `dbo.EandMForm` | `P1Forms.br_samms_eandm_form_mdm` | `bhg_silver.pats.tbl_EandMFormMDM` |
| `dbo.SF_DataForms` | `P1Forms.br_samms_sf_data_forms` | `bhg_silver.pats.tbl_SF_DataForms` |
| `dbo.SMSTextConsentForm` | `P1Forms.br_samms_sms_text_consent_form` | `bhg_silver.pats.tbl_SMSTextConsentForm` |
| `dbo.consenttomarketing` | `P1Forms.br_samms_consent_to_marketing` | `bhg_silver.pats.tbl_ConsenttoMarketing` |
| `dbo.takehomeagreementanddiversioncontrol` | `P1Forms.br_samms_take_home_agreement_diversion_control` | `bhg_silver.pats.tbl_TakeHomeAgreementandDiversionControl` |
| `dbo.TakeHomeRiskAssessment` | `P1Forms.br_samms_take_home_risk_assessment` | `bhg_silver.pats.tbl_TakeHomeRiskAssessment` |
| `dbo.newdischargetransferplanform` | `P1Forms.br_samms_new_discharge_transfer_plan_form` | `bhg_silver.pats.tbl_NewDischargeTransferPlanForm` |

**Bronze site success:** Inferred from each method's Bronze table using TaskConfig `RequestBody.full_table`, `ingest_column` (`IngestRunId`), and `site_column` (`SiteCode`) — no separate marker table.

### Key Column Mappings / Silver Merge Keys

| Method | Merge Key (Silver) | Notable Rule |
|--------|-------------------|--------------|
| `SaveComprehensiveAssessmentForm` | `SiteCode` + `Id` | `RowState` from `IsDeleted`; `RowChkSum` from Bronze |
| `SaveEMFormPregnancy` | `SiteCode` + `EandMFormId` | Match on **`EandMFormId`**, not `Id` |
| `SaveEMFormMDM` | `SiteCode` + `Id` | Full update on all matched rows (legacy parity) |
| `SaveDataForms` | `SiteCode` + `Id` | Incremental scope from Bronze lookback |
| `SaveSMSTextConsentForm` | `SiteCode` + `Id` | Full extract merge |
| `SaveConsenttoMarketing` | `SiteCode` + `Id` | Incremental scope |
| `SaveTakeHomeAgreementandDiversionControl` | `SiteCode` + `Id` | Incremental scope |
| `SaveTakeHomeRiskAssessment` | `SiteCode` + `Id` | Full extract merge |
| `SaveNewDischargeTransferPlanForm` | `SiteCode` + `Id` | Incremental scope |

| Bronze Metadata | Silver Handling |
|-----------------|-----------------|
| `SiteCode`, `SourceDatabase`, `IngestRunId`, `ExtractedAt`, lookback dates | Used for filtering/dedup; not carried to final reporting columns |
| `RowChkSum`, `LastModAt`, `RowState` | Transformed per method legacy rules |

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [Confirm per table — Comprehensive Assessment has many columns] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | [List column names if applicable] |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

Each active clinic site × method is registered in `bhg_bronze.meta.taskconfig` (ConfigId 97). Silver has one TaskConfig row per method (ConfigId 98).

| Field | Bronze (ConfigId 97) | Silver (ConfigId 98) |
|-------|----------------------|----------------------|
| **Method** | One of nine `Save*` form methods | Same method name |
| **SiteCode** | e.g. `AHK` | N/A (method-level task) |
| **DataBaseName** | e.g. `SAMMS-Ahoskie` | N/A |
| **SourceTable** | e.g. `dbo.ComprehensiveAssessmentForm` | N/A |
| **destination_schema** | `P1Forms` | `pats` |
| **destination_table** | `br_samms_*` | `tbl_*` Silver table name |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 97 | BR | Bronze extraction |
| 98 | SL | Silver merge |
| 99 | GL | Optional Gold watermark publish — out of active consumer scope |

Audit prefix: **`SAMMS P1 Forms`**.

**Note:** Optional Gold publish (`nb_p1_forms_optional_gold_publish`, ConfigId 99) runs on the success path when Gold tasks are active. **Silver remains the operational terminal layer** for downstream consumers documented here.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 2` (Regional ETL P1) |

### Notebook / Pipeline Entry Point

`nb_get_p1_forms_taskconfig` → `flt_active_p1_forms_sites` → `nb_p1_forms_audit_start` → `Executed_AfterBronz` → `set_bronze_method_results_from_child` → `Executed_AfterSilver` → `set_silver_method_results_from_child` → `if_all_forms_methods_success` → audit finalize

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_p1_forms_taskconfig` | `6e7b4814-5818-4715-9275-f6ad72743221` | Slim TaskConfig JSON (ConfigIds 97, 99) |
| `nb_p1_forms_audit_start` / `_finalize_success` / `_finalize_failure` | `dc7a5867-14f5-455a-a0f8-ff5c036346b9` | Audit lifecycle with per-method partial finalize |
| `nb_forms_sl_comp_assess` | `1fef87e4-feaf-4d6e-8e12-e41260c938e0` | Silver MERGE — `SaveComprehensiveAssessmentForm` |
| `nb_forms_sl_em_preg` | `f2631915-54e0-4407-a3f6-b709218989da` | Silver MERGE — `SaveEMFormPregnancy` |
| `nb_forms_sl_em_mdm` | `56d1c4d5-ff78-454a-a014-e197daef7c78` | Silver MERGE — `SaveEMFormMDM` |
| `nb_forms_sl_data_forms` | `505f0328-c497-4495-88dc-4e7467621e19` | Silver MERGE — `SaveDataForms` |
| `nb_forms_sl_sms_consent` | `f9ec192e-2b82-46e7-8dee-430df050695b` | Silver MERGE — `SaveSMSTextConsentForm` |
| `nb_forms_sl_marketing` | `826419c4-0686-425b-af28-c58f44d5f69d` | Silver MERGE — `SaveConsenttoMarketing` |
| `nb_forms_sl_takehome_agree` | `d14ddddb-62b3-4baf-9312-adc1141bf1f0` | Silver MERGE — `SaveTakeHomeAgreementandDiversionControl` |
| `nb_forms_sl_takehome_risk` | `86916924-cd42-4553-9637-37301f0adbd2` | Silver MERGE — `SaveTakeHomeRiskAssessment` |
| `nb_forms_sl_discharge_plan` | `aa8969a2-27c2-4334-a512-d5ed4e62d641` | Silver MERGE — `SaveNewDischargeTransferPlanForm` |
| `nb_p1_forms_optional_gold_publish` | `c1e2d16d-7945-4d2f-8cee-dced07b2e1e4` | Optional Gold watermark merge (ConfigId 99) |
| `nb_p1_forms_notify_failed` | `77c87686-120d-486b-9146-6a794d794e38` | Failure notification — **Inactive** |

### Bronze Child Pipeline Activities (not notebooks)

| Pattern | Type | Purpose |
|---------|------|---------|
| `flt_child_*_sites` | Filter | One filter per method |
| `fe_each_samms_site_*` | ForEach | Per-site Copy (`batchCount = 3`) |
| `lkp_check_*` | Lookup | Source table existence |
| `if_*_exists` | IfCondition | Gate Copy |
| `cp_*_to_bronze` | Copy | SAMMS → Bronze Append |
| `set_child_bronze_method_results` | SetVariable | Return per-method JSON to parent |

| Field | Value |
|-------|-------|
| **Method / Function Names** | Nine `Save*` form methods (see Section 4) |
| **Language** | PySpark (Silver notebooks); SQL (Bronze Copy) |
| **Error Strategy** | Per-site and per-method isolation; partial success via method result JSON |
| **Retry Attempts** | 0 (Copy/Lookup); per notebook activity policy |
| **Failure Notification** | `nb_p1_forms_notify_failed` exists but **Inactive** |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Metadata tagging at Bronze** — `SiteCode`, `SourceDatabase`, `IngestRunId`, `ExtractedAt`, lookback date columns
- **Table-existence gate** — skip Copy when source table missing at clinic
- **Bronze site success inference** — Silver reads successful sites from Bronze rows via TaskConfig `ingest_column` / `site_column`
- **Within-run Bronze filter** — Silver reads rows for current `IngestRunId` only
- **Schema alignment** — `resolve_forms_silver_metadata()` + `align_to_target()` per method
- **Full update on match** — generic `merge_to_silver()` uses null-safe `<=>` keys; no RowChkSum gate in shared merge
- **Per-method status JSON** — Bronze child → parent variable → Silver child → parent variable → audit

### Parent Pipeline Parameters and Variables

| Parameter / Variable | Default / Set by | Purpose |
|----------------------|------------------|---------|
| `p_lookback_days` | 15 | Incremental Copy SQL for applicable methods |
| `p_work_date` | `2026-07-20` | Scheduling context |
| `p_ingest_run_id` | pipeline RunId | Bronze row tag; Silver filter |
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver child, audit finalize |
| `v_silver_method_results_json` | `set_silver_method_results_from_child` | Audit finalize |

---

## 8. Security & Compliance

### Access & Permissions

| Field | Value |
|-------|-------|
| **Source DB Permission** | Per-clinic SAMMS access via on-premises gateway |
| **Authentication Method** | Fabric connection |
| **Credential Storage** | |
| **Workspace Access** | BHG PLATFORM CORE DEV |

### Data Classification

Clinical form data may contain **PHI**. Confirm classification with the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Clinical form records |
| **Contains PHI / PII** | |
| **HIPAA Applicable** | |
| **Masking in Non-Prod** | |
| **Compliance Standards** | |

---

## 9. Testing & Validation

### Test Results

| Environment | Test Date | Tested By | Row Count Match | Result |
|-------------|-----------|-----------|-----------------|--------|
| Dev | [DD/MM/YYYY] | [Name] | [Yes/No] | [Pass/Fail] |
| UAT | [DD/MM/YYYY] | [Name] | [Yes/No] | [Pass/Fail] |
| Prod (dry run) | [DD/MM/YYYY] | [Name] | [Yes/No] | [Pass/Fail] |

### Validation Checklist

- Row count captured and compared (`RowCount` metric) for ConfigIds 97 and 98
- Schema parity validated against BHG_DR for all nine Silver tables (see `P1_Forms_Unit_Testing_Guide.md`)
- Duplicate records identified — verify `SiteCode` + `Id` (or `EandMFormId` for pregnancy)
- Null values quantified on merge key columns
- Overall `ValidationStatus` PASS or FAIL per method
- Audit log populated after each run (~1,035 Bronze + 9 Silver tasks)
- Per-method bronze/silver JSON checked for `FAILED` or `SKIPPED`
- Test sites: `AHK`, `B12B`, `B24`, `B25`, `B26` (unit testing guide)

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Per method, per run] |
| **Destination Row Count** | [Bronze append vs Silver merge] |
| **Load Duration** | [e.g. 90–180 mins] |
| **Throughput** | [e.g. rows/min] |

---

## 10. DevOps & Source Control

### Repository Details

| Field | Value |
|-------|-------|
| **Azure DevOps Org** | [Organisation name] |
| **Repository Name** | [e.g. fabric-pipelines] |
| **Feature Branch** | [e.g. feature/samms-p1-forms-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure or confirmed bad form data for one method |
| **Rollback Steps** | [e.g. Delta time travel on affected `pats.tbl_*` table; re-run with corrected parameters] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze append-only — rows accumulate by `IngestRunId` | Silver holds merged state; filter Bronze by ingest run for validation | |
| 002 | Per-clinic schema variance — not all form tables exist | Lookup gate skips missing tables silently | |
| 003 | EM Pregnancy requires three source tables | Site skipped if any of three missing | |
| 004 | `SaveEMFormPregnancy` merge key differs | Must use `EandMFormId`, not `Id` | |
| 005 | Optional Gold (ConfigId 99) on success path | Silver is operational terminal layer for consumers | |
| 006 | Notifications inactive | Monitor via Fabric run history and audit tables | |
| 007 | IfCondition string contains check for FAILED | Rare false trigger if error text contains `FAILED` | |
| 008 | No separate Bronze site-marker table | Success inferred from Bronze row presence per TaskConfig | |

---

## 12. Sign-Off & Approvals

### Development

| Role | Full Name | Signature | Date |
|------|-----------|-----------|------|
| Developer / Author | [Name] | ______________________ | [DD/MM/YYYY] |
| Technical Lead | [Name] | ______________________ | [DD/MM/YYYY] |
| Code Reviewer | [Name] | ______________________ | [DD/MM/YYYY] |

### UAT

| Role | Full Name | Signature | Date |
|------|-----------|-----------|------|
| QA / Test Lead | [Name] | ______________________ | [DD/MM/YYYY] |
| Business Owner | [Name] | ______________________ | [DD/MM/YYYY] |

### Production Approval

| Role | Full Name | Signature | Date |
|------|-----------|-----------|------|
| Data Architect | [Name] | ______________________ | [DD/MM/YYYY] |
| Security Officer | [Name] | ______________________ | [DD/MM/YYYY] |
| Project Manager | [Name] | ______________________ | [DD/MM/YYYY] |

---

*Microsoft Fabric | Pipeline Documentation | SAMMS P1 Forms ETL | Generated from Technical Design*
