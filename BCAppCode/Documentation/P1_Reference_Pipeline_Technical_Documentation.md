# Microsoft Fabric — Pipeline Documentation — SAMMS P1 Reference ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS P1 Reference ETL Pipeline |
| **Pipeline ID** | `pl_execute_reference` (`936a06e1-7a7b-4a9a-b04f-0ba9ccd0a0cc`) |
| **Bronze Child Pipeline ID** | `pl_reference` (`27f7d826-722f-4eef-a260-17ae96a47fec`) |
| **Silver Child Pipeline ID** | `pl_reference_bronz_to_silver` (`c7cd6646-0b16-4b18-901b-e080e8d91629`) |
| **Version** | v1.0 |
| **Author** | [Name] |
| **Department** | Developer |
| **Created Date** | 14/07/2026 |
| **Last Updated** | 14/07/2026 |
| **Status** | Draft |
| **Environment** | Dev |

---

## 1. Document Control

### Version History

| Version | Date | Author | Change Summary | Approved By |
|---------|------|--------|----------------|-------------|
| v1.0 | 14/07/2026 | [Name] | Initial draft — generated from SAMMS P1 Reference Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS P1 Reference ETL pipeline migrates and modernizes the legacy Regional ETL Phase 1 reference-data process (`BHGTaskRunner.exe 2`) within Microsoft Fabric. It extracts clinic-wide configuration and lookup data from 115+ per-clinic SAMMS SQL Server databases and loads it into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

Unlike the legacy C# `Save*` services — where extraction and upsert logic were tightly coupled per table — the Fabric implementation separates orchestration into a parent pipeline, a Bronze child (nine parallel method loops), a Silver child (nine parallel merge notebooks), and a reusable audit framework. This improves maintainability, scalability, and operational monitoring while preserving existing business logic: `RowChkSum` change detection, method-specific Silver merge rules, per-site success markers, and **partial success** when one reference table fails at one clinic.

The pipeline processes **nine reference methods** across all active clinic sites registered in `meta.taskconfig` (ConfigId 88).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds downstream billing codes, clinic settings, services, custom Q&A, and pre-admission reference data |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | Nine Copy activities per method inside Bronze child — `cp_*_to_bronze` (one per site per method) |
| **Copy Job Object ID** | Embedded in child pipeline `pl_reference` |
| **Job Mode** | Batch (Bronze); mostly full-table extracts; one incremental method (`SavePreAdminReferrals`) |
| **Write Behavior** | Bronze: Append (tagged by `IngestRunId`); Silver: Delta MERGE per method |
| **Enable Staging** | No (Bronze Copy) |
| **Table Option** | Silver tables pre-created or created on first merge run |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy and Lookup default); notebook retry per activity policy |

### Data Flow

Source (per-clinic SAMMS SQL Server) → Bronze child (`pl_reference`) → Bronze Lakehouse → Silver child (`pl_reference_bronz_to_silver`) → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | Nine reference tables (clinic, codes, services, custom Q&A, etc.) |
| **Bronze** | `pl_reference` — 9 parallel Filter + ForEach + Lookup + Copy | Table-existence gate; site success markers; `batchCount = 3` |
| **Silver** | `pl_reference_bronz_to_silver` — 9 parallel notebooks | Method-specific Delta MERGE; RowChkSum / RowState rules |
| **Destination** | Fabric Silver Lakehouse | Schemas `ctrl`, `pats`, `ayx` — terminal layer for consumers |

### Parent Pipeline Activity Sequence

```
nb_get_p1_reference_taskconfig
  → flt_active_p1_reference_sites
  → nb_p1_reference_audit_start
  → Executed_AfterBronz (pl_reference)
  → set_bronze_method_results_from_child
  → Executed_AfterSilver (pl_reference_bronz_to_silver)
  → set_silver_method_results_from_child
  → if_all_reference_methods_success
      → nb_p1_reference_audit_finalize_success / _failure
  → nb_p1_reference_notify_failed (on IfCondition Failed/Skipped)
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

The pipeline processes nine configured source tables. Row counts, sizes, and primary key status are environment-specific and should be confirmed against the live source.

| # | Method | Source Table | Bronze Table | Row Count | Size | Has PK? |
|---|--------|--------------|--------------|-----------|------|---------|
| 1 | `SaveClinic` | `dbo.tblClinic` | `br_samms_clinic` | [Per site] | [MB/GB] | [Yes/No] |
| 2 | `Save3pSetup` | `dbo.tbl3PSETUP` | `br_samms_3p_setup` | [Per site] | [MB/GB] | [Yes/No] |
| 3 | `SaveCodes` | `dbo.tblCodes` | `br_samms_codes` | [Per site] | [MB/GB] | [Yes/No] |
| 4 | `SaveServices` | `dbo.tblSERVICES` | `br_samms_services` | [Per site] | [MB/GB] | [Yes/No] |
| 5 | `SavedropDownListItems` | `dbo.DroDownListItems` | `br_samms_dropdown_list_items` | [Per site] | [MB/GB] | [Yes/No] |
| 6 | `SaveCustomAnswers` | `dbo.tblCUSTOMANSWERS` | `br_samms_custom_answers` | [Per site] | [MB/GB] | [Yes/No] |
| 7 | `SaveCustomQuestions` | `dbo.tblCUSTOMQUESTIONS` | `br_samms_custom_questions` | [Per site] | [MB/GB] | [Yes/No] |
| 8 | `SavePreAdmissionV6` | `dbo.SF_PatientPreAdmission` | `br_samms_pre_admission_v6` | [Per site] | [MB/GB] | [Yes/No] |
| 9 | `SavePreAdminReferrals` | `dbo.SF_PatientPreadmissionReferralSource` | `br_samms_preadmission_referral_source` | [Per site] | [MB/GB] | [Yes/No] |

**Active sites:** ~115 clinics — one TaskConfig row per site per method (ConfigId 88).

### Load Strategy

| Method | Load Type | Delta Filter Logic |
|--------|-----------|-------------------|
| `SaveClinic` through `SaveCustomQuestions`, `SavePreAdmissionV6` | Full extract | Full table SELECT when source table exists |
| `SavePreAdminReferrals` | Incremental | **515-day lookback** — `LookbackDate = DATEADD(day, -(p_lookback_days + 500), GETDATE())` |
| All applicable methods | RowChkSum | `CHECKSUM(...)` computed in Bronze Copy SELECT where legacy used checksum |

**Source table gate:** Bronze Lookup verifies table exists before Copy. Missing tables are skipped per site (some clinics on different SAMMS versions).

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `P1Reference` (Bronze); `ctrl`, `pats`, `ayx` (Silver) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (method-specific keys and rules) |

### Source-to-Target Mapping

| Source Table | Bronze Table | Silver Table (Final) |
|--------------|--------------|----------------------|
| `dbo.tblClinic` | `P1Reference.br_samms_clinic` | `bhg_silver.ctrl.tbl_Clinic` |
| `dbo.tbl3PSETUP` | `P1Reference.br_samms_3p_setup` | `bhg_silver.ctrl.tbl_3PSETUP` |
| `dbo.tblCodes` | `P1Reference.br_samms_codes` | `bhg_silver.pats.tbl_Codes` |
| `dbo.tblSERVICES` | `P1Reference.br_samms_services` | `bhg_silver.pats.tbl_SERVICES` |
| `dbo.DroDownListItems` | `P1Reference.br_samms_dropdown_list_items` | `bhg_silver.ctrl.tbl_DroDownListItems` |
| `dbo.tblCUSTOMANSWERS` | `P1Reference.br_samms_custom_answers` | `bhg_silver.pats.tbl_CustomAnswers` |
| `dbo.tblCUSTOMQUESTIONS` | `P1Reference.br_samms_custom_questions` | `bhg_silver.pats.tbl_CustomQuestions` |
| `dbo.SF_PatientPreAdmission` | `P1Reference.br_samms_pre_admission_v6` | `bhg_silver.ayx.tbl_PreAdmission_V6` |
| `dbo.SF_PatientPreadmissionReferralSource` | `P1Reference.br_samms_preadmission_referral_source` | `bhg_silver.pats.tbl_PreadmissionReferralSource` |

**Control / audit Bronze table:** `P1Reference.br_p1_reference_site_success` (per-site Bronze success markers).

### Key Column Mappings / Silver Merge Keys

| Method | Merge Key (Silver) | Notable Rule |
|--------|-------------------|--------------|
| `SaveClinic` | `SiteCode` + `PKEY` | Full upsert |
| `Save3pSetup` | TaskConfig-defined | RowChkSum gate |
| `SaveCodes` | TaskConfig-defined | RowChkSum gate |
| `SaveServices` | TaskConfig-defined | Pre-reset `IsActive=false`; RowChkSum gate; scoped insert rules |
| `SavedropDownListItems` | TaskConfig-defined | Simple match upsert |
| `SaveCustomAnswers` | `SiteCode` + `caID` + `caQID` + `caCLTID` | Pre-reset `RowSate=0`; derive from `caCLTID` |
| `SaveCustomQuestions` | TaskConfig-defined | Same RowSate pattern as CustomAnswers |
| `SavePreAdmissionV6` | TaskConfig-defined | `RowState` from `IsDeleted` |
| `SavePreAdminReferrals` | TaskConfig-defined | Type casts for flag columns |

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [Confirm per table against live SAMMS — custom answer text may be large] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | [List column names if applicable] |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

Each active clinic site × method is registered in `bhg_bronze.meta.taskconfig` (ConfigId 88). Silver has one TaskConfig row per method (ConfigId 89).

| Field | Bronze (ConfigId 88) | Silver (ConfigId 89) |
|-------|----------------------|----------------------|
| **Method** | One of nine `Save*` methods | Same method name |
| **SiteCode** | e.g. `AHK` | N/A (method-level task) |
| **DataBaseName** | e.g. `SAMMS-Ahoskie` | N/A |
| **SourceTable** | e.g. `dbo.tblClinic` | N/A |
| **destination_schema / TargetSchema** | `P1Reference` | `ctrl` / `pats` / `ayx` |
| **destination_table / TargetTable** | `br_samms_*` | Silver table name |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 88 | BR | Bronze extraction |
| 89 | SL | Silver merge |

Audit prefix: **`SAMMS P1 Reference`**.

**Note:** Gold ConfigId 90 may exist in control tables but is **not executed** by this pipeline.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 2` (Regional ETL P1) |

### Notebook / Pipeline Entry Point

`nb_get_p1_reference_taskconfig` → `flt_active_p1_reference_sites` → `nb_p1_reference_audit_start` → `Executed_AfterBronz` (`pl_reference`) → `set_bronze_method_results_from_child` → `Executed_AfterSilver` (`pl_reference_bronz_to_silver`) → `set_silver_method_results_from_child` → `if_all_reference_methods_success` → audit finalize / notify

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_p1_reference_taskconfig` | `6e7b4814-5818-4715-9275-f6ad72743221` | Slim TaskConfig JSON for pipeline runtime |
| `nb_p1_reference_audit_start` / `_finalize_success` / `_finalize_failure` | `32503e1c-b4a8-4f36-8bfa-988562250c2f` | Audit lifecycle with per-method partial finalize |
| `nb_p1_reference_sl_save_clinic` | `12ef735e-a19d-43db-8d6f-f76b62228859` | Silver MERGE — `SaveClinic` |
| `nb_p1_reference_sl_save_3p_setup` | `1405a051-c662-4b1f-9078-1d21af91033f` | Silver MERGE — `Save3pSetup` |
| `nb_p1_reference_sl_save_codes` | `2c9662e0-db8e-4f56-af65-099ebb7abbc9` | Silver MERGE — `SaveCodes` |
| `nb_p1_reference_sl_save_services` | `7ee00480-e57c-4055-ade8-581460bdaf8f` | Silver MERGE — `SaveServices` |
| `nb_p1_reference_sl_save_dropdown_list_items` | `b00aa878-ae47-4ae6-b623-bd827ac55f5f` | Silver MERGE — `SavedropDownListItems` |
| `nb_p1_reference_sl_save_custom_answers` | `744818c1-b78a-424f-b4a6-eeef89bfd226` | Silver MERGE — `SaveCustomAnswers` |
| `nb_p1_reference_sl_save_custom_questions` | `4f4ef829-feb7-4ced-8357-0deed45b6b30` | Silver MERGE — `SaveCustomQuestions` |
| `nb_p1_reference_sl_save_pre_admission_v6` | `222b75bf-57f3-4c8a-98d0-d45aa274425e` | Silver MERGE — `SavePreAdmissionV6` |
| `nb_p1_reference_sl_save_preadmin_referrals` | `b17be974-02b7-4fac-8f4f-15fdb25974d1` | Silver MERGE — `SavePreAdminReferrals` |
| `nb_p1_reference_notify_failed` | `77c87686-120d-486b-9146-6a794d794e38` | Failure notification (runs on finalize path failure) |

### Bronze Child Pipeline Activities (not notebooks)

| Pattern | Type | Purpose |
|---------|------|---------|
| `flt_child_*_sites` | Filter | One filter per method |
| `fe_each_samms_site_*` | ForEach | Per-site Copy (`batchCount = 3`) |
| `lkp_check_*` | Lookup | Source table existence |
| `if_*_exists` | IfCondition | Gate Copy |
| `cp_*_to_bronze` | Copy | SAMMS → Bronze Append |
| Site success marker | Copy | Write to `br_p1_reference_site_success` |
| `set_child_bronze_method_results` | SetVariable | Return per-method JSON to parent |

| Field | Value |
|-------|-------|
| **Method / Function Names** | `SaveClinic`, `Save3pSetup`, `SaveCodes`, `SaveServices`, `SavedropDownListItems`, `SaveCustomAnswers`, `SaveCustomQuestions`, `SavePreAdmissionV6`, `SavePreAdminReferrals` |
| **Language** | PySpark (Silver notebooks); SQL (Bronze Copy) |
| **Error Strategy** | Per-site and per-method isolation; partial success via method result JSON; audit finalize per method |
| **Retry Attempts** | 0 (Copy/Lookup); per notebook activity policy |
| **Failure Notification** | `nb_p1_reference_notify_failed` on IfCondition Failed/Skipped |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Metadata tagging at Bronze** — `SiteCode`, `SourceDatabase`, `IngestRunId`, `ExtractedAt`, lookback date columns
- **Table-existence gate** — skip Copy when source table missing at clinic
- **Site success markers** — Bronze records which site/method combinations succeeded for Silver gating
- **Within-run Bronze filter** — Silver reads rows for current `IngestRunId` only
- **RowChkSum change detection** — update only when checksum differs (where applicable)
- **IsActive / RowState pre-reset** — `SaveServices`, `SaveCustomAnswers`, `SaveCustomQuestions`, `SavePreAdmissionV6`
- **Scoped insert rules** — e.g. `SaveServices` does not insert new `sID` if site already has service rows
- **Per-method status JSON** — Bronze child → parent variable → Silver child → parent variable → audit

### Parent Pipeline Variables

| Variable | Set by | Consumed by |
|----------|--------|-------------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver child, audit finalize, notify |
| `v_silver_method_results_json` | `set_silver_method_results_from_child` | Audit finalize, notify |

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

Reference data includes clinic configuration, billing codes, and custom Q&A — some tables may contain **PII/PHI** (pre-admission forms, custom answers tied to clients). Confirm classification with the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Clinic reference / configuration; potential client-linked custom Q&A |
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

The audit framework's `DataQuality` table captures the following after finalize (full or partial success):

- Row count captured and compared (`RowCount` metric) per method for ConfigIds 88 and 89
- Duplicate records identified (`DuplicateCount` metric)
- Null values quantified (`NullCount` metric)
- Overall `ValidationStatus` recorded as PASS or FAIL
- Audit log (`TaskAudit`, `PipelineRun`, `TaskQueue`) populated after each run
- Per-method bronze/silver JSON checked for `FAILED`, `ERROR`, or `SKIPPED` before success finalize
- Site success markers in `br_p1_reference_site_success` align with Bronze Copy outcomes

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Per method, per site — ~115 sites × 9 methods at Bronze] |
| **Destination Row Count** | [Bronze append vs Silver merge counts per method] |
| **Load Duration** | [e.g. 2–4 hours for full P1 Reference run] |
| **Throughput** | [e.g. rows/min per method] |

---

## 10. DevOps & Source Control

### Repository Details

| Field | Value |
|-------|-------|
| **Azure DevOps Org** | [Organisation name] |
| **Repository Name** | [e.g. fabric-pipelines] |
| **Feature Branch** | [e.g. feature/samms-p1-reference-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure for a method; confirmed bad reference data post-run |
| **Rollback Steps** | [e.g. Delta time travel on affected Silver table; re-run failed method only; fix TaskConfig match keys] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze is append-only — rows accumulate by `IngestRunId` | Silver holds merged current state; filter Bronze by ingest run for validation | |
| 002 | One clinic missing a source table skips that site only | Expected for SAMMS version differences; monitor per-method bronze JSON | |
| 003 | `SaveCustomAnswers` merge key must include `caQID` | Verify TaskConfig match keys — not `SiteCode + caID + caCLTID` alone | |
| 004 | IfCondition uses string contains for FAILED/SKIPPED | A method name or message containing those tokens could false-trigger — rare | |
| 005 | Nine parallel Bronze methods × batchCount 3 stresses gateway | Monitor gateway capacity during peak runs | |
| 006 | TaskConfig notebook required due to Lookup 4 MB limit | Do not replace with Lookup activity for full TaskConfig rows | |

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

*Microsoft Fabric | Pipeline Documentation | SAMMS P1 Reference ETL | Generated from Technical Design*
