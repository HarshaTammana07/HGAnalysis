# Microsoft Fabric — Pipeline Documentation — SAMMS Form Answer Signatures ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS Form Answer Signatures ETL Pipeline |
| **Pipeline ID** | `pl_execute_pipeline_answersignature` (`3d7e0bbc-a3b8-40ae-b933-fa9188990c94`) |
| **Bronze Child Pipeline ID** | `pl_answersig_samms_to_lakehouse` (`cf9e89d0-9574-439e-85b9-c022327ed022`) |
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
| v1.0 | 14/07/2026 | [Name] | Initial draft — generated from SAMMS Form Answer Signatures Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS Form Answer Signatures ETL pipeline migrates and modernizes the legacy form signature-date ETL process (`BHGTaskRunner.exe 6` / Samms-Forms — `SaveAnswerSignatures` / `SaveFormQAData` path) within Microsoft Fabric. It extracts when each clinical form was signed by each role (completed-by, counselor, doctor, medical provider, patient, provider, requestor, staff, supervisor) from 115+ per-clinic SAMMS SQL Server databases and loads them into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

Unlike the legacy C# `SaveAnswerSignatures` service — where dynamic SQL generation and EF Core upsert were coupled per site — the Fabric implementation separates Bronze extraction (dedicated child pipeline with per-site SQL builder + Copy) from Silver merge (one notebook on the parent). Form catalog configuration is read from **`bhg_silver.ctrl.Forms2Process`** (not from SAMMS at runtime). This improves maintainability and operational monitoring while preserving legacy behavior: 4-column composite keys, RowState pre-reset, compound `IsDeleted` logic, sentinel unsigned dates (`1/1/1900`), and the LAB-site `StaffSignatureDate` null rule.

The pipeline processes **one method** (`FormAnswerSignatures`) across all active clinic sites registered in `meta.taskconfig` (ConfigId 31).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds clinical form signature compliance and reporting |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | `cp_answersig_to_bronze` — one dynamic SQL Copy per site in Bronze child ForEach |
| **Copy Job Object ID** | Embedded in child pipeline `pl_answersig_samms_to_lakehouse` |
| **Job Mode** | Batch (Bronze); incremental metadata via `p_lookback_days` (default 30); full reload via `p_reload` |
| **Write Behavior** | Bronze: Append (tagged by `_ingest_run_id`); Silver: Delta MERGE (full update on match — no RowChkSum gate) |
| **Enable Staging** | No (Bronze Copy) |
| **Table Option** | Silver table created or normalized on first run |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy/Lookup default); SQL builder notebook retry = 3 |

### Data Flow

Source (per-clinic SAMMS SQL Server — dynamic UNION ALL) → Bronze child (`pl_answersig_samms_to_lakehouse`) → Bronze Lakehouse → Silver notebook on parent → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | Base Form/AnswerSignature model + Forms2Process custom forms |
| **Control** | `bhg_silver.ctrl.Forms2Process` | Form catalog, date-filter rules, custom SQL patterns |
| **Bronze** | `pl_answersig_samms_to_lakehouse` — ForEach + SQL builder + Copy | Per-site dynamic UNION ALL; `batchCount = 5` |
| **Silver** | `nb_answersig_bronze_to_silver` (parent) | RowState pre-pass + single Delta MERGE for all sites |
| **Destination** | Fabric Silver Lakehouse | `pats.sl_tblFormAnswerSignatures` — terminal layer |

### Parent Pipeline Activity Sequence

```
nb_get_FormAnswerSignatures_taskconfig
  → flt_active_formanswersignatures_sites
  → nb_formanswersig_audit_start
  → Invoke_legacy_Executed_AfterBronz (pl_answersig_samms_to_lakehouse)
  → nb_answersig_bronze_to_silver
  → nb_formanswersig_audit_finalize_success / _failure
```

**Note:** Gold publish activities (`Prepare_FormAnswerSignatures_Versioned_Gold_Table`, `Copy_silver_to_gold_version`, `Publish_FormAnswerSignatures_Versioned_Gold`) and notification notebooks exist in the pipeline definition but are **out of active documentation scope**. Silver is the operational terminal layer for consumers.

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

### Source Gate Table

| Check | Behavior |
|-------|----------|
| `AnswerSignature` table | Required — site skipped if `sys.tables` probe returns 0 |

### Source Objects (per clinic)

| Object Type | Examples | Notes |
|-------------|----------|-------|
| Gate table | `AnswerSignature` | Must exist or site is skipped |
| Base model | `Form`, `FormTemplate`, `SF_PatientPreAdmission`, `SF_DataForms` | UNION 1 (active forms) + UNION 2 (deleted forms) |
| Signature subquery | `AnswerSignature` | Nine `DateField` values per role |
| Custom forms | `tblORDERREQ`, `tblTP17REVIEW`, Forms2Process default | Only if table exists at clinic |

**Active sites:** ~115 clinics — one TaskConfig row per site (ConfigId 31). Typical Bronze task volume: ~115 site tasks per run.

### Nine Signature Date Columns (source)

| Silver Column | AnswerSignature DateField |
|---------------|---------------------------|
| `CompletedBySignatureSignatureDate` | `CompletedBySignatureSignatureDate` |
| `CounselorSignatureSignatureDate` | `CounselorSignatureSignatureDate` or `CounselorSignatureDate` |
| `DoctorSignatureSignatureDate` | `DoctorSignatureSignatureDate` |
| `MedicalProviderSignatureSignatureDate` | `MedicalProviderSignatureSignatureDate` |
| `PatientSignatureDate` | `PatientSignatureDate` |
| `ProviderSignatureSignatureDate` | `ProviderSignatureSignatureDate` |
| `RequestorSignatureDate` | `RequestorSignatureDate` |
| `StaffSignatureDate` | `StaffSignatureDate` |
| `SupervisorSignatureSignatureDate` | `SupervisorSignatureSignatureDate` |

**Sentinel rule:** If `Sign IS NULL` in the `AnswerSignature` subquery → return `1/1/1900` (form exists, role not signed).

### Load Strategy

| Field | Value |
|-------|-------|
| **Load Type** | Incremental metadata — default **30-day** lookback for Forms2Process custom UNIONs and Silver pre-pass |
| **Full Reload** | `p_reload = true` → lookback date **2010-01-01** |
| **Base Form UNION** | Legacy behavior — base active-form query has **no date filter** (pulls all forms) |
| **Custom form UNIONs** | Respect `DateFilterEnabled` in Forms2Process vs lookback date |
| **SQL Generation** | Per site via `nb_answersig_build_site_sql` |
| **Watermark Column** | N/A — date logic embedded in generated SQL |

Bronze extraction uses a **dynamic SQL builder** — not a fixed Copy query. For each site, the builder reads clinic metadata and generates a **UNION ALL** string executed by the Copy activity.

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `Forms` (Bronze); `pats` (Silver) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (full update on match) |

### Source-to-Target Mapping

| Source Concept | Bronze Table | Silver Table (Final) |
|----------------|--------------|----------------------|
| All form signature rows (UNION ALL) | `bhg_bronze.Forms.br_tblFormAnswerSig` | `bhg_silver.pats.sl_tblFormAnswerSignatures` |
| Form catalog (read-only) | N/A | `bhg_silver.ctrl.Forms2Process` |

### Key Column Mappings / Silver Merge Key

**Merge key (4 columns):**

`SiteCode` + `FormName` + `FormId` + `ClientId`

| Transformation | Detail |
|----------------|--------|
| `_site_code` → `SiteCode` | Renamed at Silver |
| `FormId` | Normalized to UPPER case |
| `ClientId` | Stored as absolute value; negative source → `RowState = 0` |
| `RowState` | From `IsDeleted` and negative source `ClientId`; pre-pass reset before MERGE |
| `StaffSignatureDate` | Forced **null** at LAB site (legacy hardcoded rule) |
| `LastModAt` | Current timestamp on merge |
| Bronze metadata | `_ingest_run_id`, `_extracted_at`, etc. — not carried to Silver |

### Silver Final Columns (reporting-ready)

`SiteCode`, `FormName`, `FormId`, `ClientId`, `CreatedOn`, `UpdatedOn`, `CompletedBySignatureSignatureDate`, `CounselorSignatureSignatureDate`, `DoctorSignatureSignatureDate`, `MedicalProviderSignatureSignatureDate`, `PatientSignatureDate`, `ProviderSignatureSignatureDate`, `RequestorSignatureDate`, `StaffSignatureDate`, `SupervisorSignatureSignatureDate`, `RowChkSum`, `RowState`, `LastModAt`

**Note:** `RowChkSum` exists on the Silver entity for legacy parity but is **not populated from source** (legacy C# SELECT never included `CHECKSUM(...)`) and is **not used as a merge gate**.

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [Confirm — signature date columns are fixed-width datetime] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | N/A for typical signature rowset |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

| Field | Bronze (ConfigId 31) | Silver (ConfigId 32) |
|-------|----------------------|----------------------|
| **Method** | `FormAnswerSignatures` | `FormAnswerSignatures` |
| **SiteCode** | Per clinic | N/A (method-level Silver task) |
| **DataBaseName** | Per clinic | N/A |
| **destination_schema** | `Forms` | `pats` |
| **destination_table** | `br_tblFormAnswerSig` | `sl_tblFormAnswerSignatures` |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 31 | BR | Bronze extraction |
| 32 | SL | Silver merge |
| 33 | GL | Gold publish — **out of active documentation scope** |

Audit prefix: **`SAMMS FormAnswerSignatures`**.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 6` (Samms-Forms) |

### Notebook / Pipeline Entry Point

`nb_get_FormAnswerSignatures_taskconfig` → `flt_active_formanswersignatures_sites` → `nb_formanswersig_audit_start` → Bronze child → `nb_answersig_bronze_to_silver` → audit finalize

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_FormAnswerSignatures_taskconfig` | `6e7b4814-5818-4715-9275-f6ad72743221` | Slim TaskConfig JSON (ConfigIds 31, 32) |
| `nb_formanswersig_audit_start` / `_finalize_success` / `_finalize_failure` | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` | Audit lifecycle |
| `nb_answersig_build_site_sql` | `673e26ce-cd7a-4b79-83d3-5b5cf7a76c79` | Per-site UNION ALL SQL from Forms2Process (Bronze child) |
| `nb_answersig_bronze_to_silver` | `4cb06dc3-4954-4119-85aa-9e590ac3dc4c` | RowState pre-pass + Delta MERGE (parent) |
| `nb_notify_success` / `nb_notify_failed` | `77c87686-120d-486b-9146-6a794d794e38` | **Inactive** — notifications not active |

### Bronze Child Pipeline Activities (not notebooks)

| Pattern | Type | Purpose |
|---------|------|---------|
| `fe_each_samms_sites` | ForEach | Per-site extract (`batchCount = 5`) |
| `lkp_check_answersig_table` | Lookup | Gate — skip site if no `AnswerSignature` table |
| `lkp_get_existing_tables` / `_columns` | Lookup | Clinic schema discovery for SQL builder |
| `cp_answersig_to_bronze` | Copy | Execute generated SQL → Bronze Append |
| `sv_set_answersig_sql` | SetVariable | **Inactive** — Copy reads notebook exit directly |
| `nb_answersig_bronze_to_silver` (in child) | Notebook | **Inactive** — Silver runs on parent only |

| Field | Value |
|-------|-------|
| **Method / Function Names** | `FormAnswerSignatures` |
| **Language** | PySpark (notebooks); SQL (dynamic Copy) |
| **Error Strategy** | Per-site isolation in Bronze ForEach; Silver does not run if Bronze child fails |
| **Retry Attempts** | 0 (Copy/Lookup); SQL builder retry = 3 |
| **Failure Notification** | `nb_notify_failed` exists but **Inactive** |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Dynamic SQL builder** — UNION 1 (active forms) + UNION 2 (deleted) + custom form blocks; no DISTINCT wrapper (legacy parity)
- **Compound IsDeleted** — Form + pre-admission + data-form joins in generated SQL
- **Within-run deduplication** — 4-column key + latest `_extracted_at` before Silver MERGE
- **RowState pre-pass** — unconditional reset for non-date-filtered forms; date-gated for others per Forms2Process
- **Full update on match** — no RowChkSum gate; always refresh all 9 signature date columns
- **LAB site rule** — `StaffSignatureDate` set to null
- **Sentinel dates** — `1/1/1900` from source means unsigned role

### Parent Pipeline Parameters

| Parameter | Default | Purpose |
|-----------|---------|---------|
| `p_lookback_days` | 30 | Custom form date filters and Silver pre-pass |
| `p_reload` | false | Full reload from 2010-01-01 |
| `p_ingest_run_id` | pipeline RunId | Bronze row tag; Silver filter |
| `p_audit_context_json` | (from audit start) | Passed to Bronze child |

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

Form signature dates may relate to **PHI** (patient signature timestamps). Confirm classification with the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Clinical form signature metadata |
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

- Row count captured and compared (`RowCount` metric) for ConfigIds 31 and 32
- Duplicate records identified (`DuplicateCount` metric) — verify 4-column merge key
- Null values quantified (`NullCount` metric)
- Overall `ValidationStatus` PASS or FAIL
- Audit log populated after each run (~115 Bronze + 1 Silver task)
- Merge key validated: `SiteCode` + `FormName` + `FormId` (UPPER) + `ClientId` (absolute)
- Sentinel dates: `1/1/1900` = unsigned role
- LAB site: `StaffSignatureDate` null
- `Forms2Process` current before Bronze run

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Per site, per run — one row per form instance] |
| **Destination Row Count** | [Bronze append vs Silver merge] |
| **Load Duration** | [e.g. 90–180 mins — dynamic SQL per site] |
| **Throughput** | [e.g. rows/min] |

---

## 10. DevOps & Source Control

### Repository Details

| Field | Value |
|-------|-------|
| **Azure DevOps Org** | [Organisation name] |
| **Repository Name** | [e.g. fabric-pipelines] |
| **Feature Branch** | [e.g. feature/samms-formanswersig-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure or confirmed bad signature data |
| **Rollback Steps** | [e.g. Delta time travel on `sl_tblFormAnswerSignatures`; re-run with corrected `p_lookback_days` or `p_reload`] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze append-only — rows accumulate by `_ingest_run_id` | Silver holds merged state; filter Bronze by ingest run for validation | |
| 002 | Per-clinic schema variance — not all custom form tables exist | SQL builder skips missing tables/columns silently | |
| 003 | Site skipped when no `AnswerSignature` table | Expected for some clinics | |
| 004 | Gold publish (ConfigId 33) out of active scope | Silver is operational terminal layer | |
| 005 | `RowChkSum` not populated from source | Full update on match — legacy parity | |
| 006 | Base Form UNION has no date filter | Pulls all active forms every run — legacy behavior | |
| 007 | Notifications inactive | Monitor via Fabric run history and audit tables | |
| 008 | 4-key merge differs from FormQuestionAnswers (7-key) | Do not reuse FormQA merge logic | |

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

*Microsoft Fabric | Pipeline Documentation | SAMMS Form Answer Signatures ETL | Generated from Technical Design*
