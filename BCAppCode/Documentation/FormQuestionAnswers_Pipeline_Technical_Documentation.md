# Microsoft Fabric — Pipeline Documentation — SAMMS Form Question Answers ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS Form Question Answers ETL Pipeline |
| **Pipeline ID** | `pl_execute_form_questionanswers` (`ea25fed4-2f4b-4c1e-9a62-e504364580e2`) |
| **Bronze Child Pipeline ID** | `pl_forms_samms_to_lakehouse` (`d7caff4a-595e-4826-a75e-75919ed5d30f`) |
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
| v1.0 | 14/07/2026 | [Name] | Initial draft — generated from SAMMS Form Question Answers Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS Form Question Answers ETL pipeline migrates and modernizes the legacy clinical forms ETL process (`BHGTaskRunner.exe 6` / Samms-Forms) within Microsoft Fabric. It extracts patient form question-and-answer records from 115+ per-clinic SAMMS SQL Server databases — covering the base `Form`/`Question`/`Answer` model plus up to ~75 custom form types per clinic — and loads them into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

Unlike the legacy C# `SaveFormQuestionAnswers` service — where dynamic SQL generation and EF Core upsert were coupled per site — the Fabric implementation separates Bronze extraction (dedicated child pipeline with per-site SQL builder + Copy) from Silver merge (one notebook on the parent). Form catalog configuration is read from **`bhg_silver.ctrl.Forms2Process`** (not from SAMMS at runtime), matching how legacy C# loaded `f2p` from Azure. This improves maintainability and operational monitoring while preserving legacy behavior: 7-column composite keys, RowState pre-reset, Treatment Plan (`TP-*`) rules, and compound `IsDeleted` logic.

The pipeline processes **one method** (`FormQuestionAnswers`) across all active clinic sites registered in `meta.taskconfig` (ConfigId 28).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds clinical assessment, treatment plan, and form reporting |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | `cp_forms_to_bronze` — one dynamic SQL Copy per site in Bronze child ForEach |
| **Copy Job Object ID** | Embedded in child pipeline `pl_forms_samms_to_lakehouse` |
| **Job Mode** | Batch (Bronze); Incremental via `p_lookback_days` (default 30); full reload via `p_reload` |
| **Write Behavior** | Bronze: Append (tagged by `_ingest_run_id`); Silver: Delta MERGE (full update on match — no RowChkSum gate) |
| **Enable Staging** | No (Bronze Copy) |
| **Table Option** | Silver table created or normalized on first run |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy/Lookup default); site marker `mk_formqa_site_success` retry = 3 |

### Data Flow

Source (per-clinic SAMMS SQL Server — dynamic UNION ALL) → Bronze child (`pl_forms_samms_to_lakehouse`) → Bronze Lakehouse → Silver notebook on parent → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | Base Form model + ~75 Forms2Process-driven custom tables |
| **Control** | `bhg_silver.ctrl.Forms2Process` | Form catalog, date-filter rules, custom SQL patterns |
| **Bronze** | `pl_forms_samms_to_lakehouse` — ForEach + SQL builder + Copy | Per-site dynamic UNION ALL; `batchCount = 5` |
| **Silver** | `nb_forms_bronze_to_silver` (parent) | RowState pre-pass + single Delta MERGE for all sites |
| **Destination** | Fabric Silver Lakehouse | `pats.tbl_dbo_FormQuestionAnswers` — terminal layer |

### Parent Pipeline Activity Sequence

```
nb_get_FormQuestionAnswers_taskconfig
  → flt_active_formquestionanswers_sites
  → nb_formqa_audit_start
  → Invoke_legacy_Execute_AfterBronz (pl_forms_samms_to_lakehouse)
  → set_formqa_bronze_results
  → nb_forms_bronze_to_silver
  → set_formqa_silver_results
  → if_formqa_silver_result_success
      → nb_formqa_audit_finalize_clean_success / _failure
      → nb_notify_failed (on failure path)
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

### Source Objects (per clinic)

| Object Type | Examples | Notes |
|-------------|----------|-------|
| Required gate table | `Form` | Site skipped if absent |
| Base model | `FormTemplate`, `Question`, `Answer` | Core Q&A extraction |
| Pre-admission joins | `SF_PatientPreAdmission`, `SF_DataForms` | Compound `IsDeleted` logic |
| Custom form tables | ~75 patterns from Forms2Process | Unioned only if table exists at clinic |
| Optional columns | e.g. `tblTP17REVIEW.tprReviewFrequency` | Omitted from SQL if column absent |

**Active sites:** ~115 clinics — one TaskConfig row per site (ConfigId 28). Typical Bronze task volume: ~115 site tasks per run.

### Load Strategy

| Field | Value |
|-------|-------|
| **Load Type** | Incremental — default **30-day** lookback (`p_lookback_days`) |
| **Full Reload** | `p_reload = true` → lookback date **2010-01-01** in SQL builder and Silver pre-pass |
| **SQL Generation** | Per site via `nb_forms_build_site_sql` reading `bhg_silver.ctrl.Forms2Process` |
| **Date Filter Rules** | Per form: `DateFilterEnabled` in Forms2Process; `CreatedOn`/`UpdatedOn` vs lookback |
| **Watermark Column** | N/A — date logic embedded in generated SQL |

Bronze extraction is **not a fixed SELECT per table**. For each site, the SQL builder reads clinic metadata (existing tables/columns from Lookups) and generates a large **UNION ALL** string executed by the Copy activity.

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `Form` (Bronze); `pats` (Silver) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (full update on match) |

### Source-to-Target Mapping

| Source Concept | Bronze Table | Silver Table (Final) |
|----------------|--------------|----------------------|
| All form Q&A (UNION ALL) | `bhg_bronze.Form.br_tblFormQA` | `bhg_silver.pats.tbl_dbo_FormQuestionAnswers` |
| Site success marker | `bhg_bronze.Form.br_formqa_site_success` | N/A (Bronze audit only) |
| Form catalog (read-only) | N/A | `bhg_silver.ctrl.Forms2Process` |

### Key Column Mappings / Silver Merge Key

**Merge key (7 columns):**

`SiteCode` + `FormName` + `FormId` + `ClientId` + `PreAdmissionId` + `QuestionId` + `QuestionOrderId`

| Transformation | Detail |
|----------------|--------|
| `_site_code` → `SiteCode` | Renamed at Silver |
| `FormId` | Normalized to UPPER case |
| Null key defaults | `ClientId` → 0, `PreAdmissionId` → -1, `QuestionId` → 0 |
| `RowState` | From `IsDeleted` and negative `ClientId`; pre-pass reset before MERGE |
| `LastModAt` | Current timestamp on merge |
| Bronze metadata | `_ingest_run_id`, `_extracted_at`, etc. — not carried to Silver |

### Silver Final Columns (reporting-ready)

`SiteCode`, `FormName`, `FormId`, `ClientId`, `CreatedOn`, `CreatedBy`, `UpdatedOn`, `UpdatedBy`, `PreAdmissionId`, `IsDeleted`, `IsChildForm`, `QuestionId`, `QuestionOrderId`, `QuestionText`, `OptionId`, `AnswerValue`, `RowState`, `LastModAt`

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [Confirm — `QuestionText` and `AnswerValue` may be large] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | [e.g. `QuestionText`, `AnswerValue`] |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

| Field | Bronze (ConfigId 28) | Silver (ConfigId 29) |
|-------|----------------------|----------------------|
| **Method** | `FormQuestionAnswers` | `FormQuestionAnswers` |
| **SiteCode** | Per clinic | N/A (method-level Silver task) |
| **DataBaseName** | Per clinic | N/A |
| **destination_schema** | `Form` | `pats` |
| **destination_table** | `br_tblFormQA` | `tbl_dbo_FormQuestionAnswers` |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 28 | BR | Bronze extraction |
| 29 | SL | Silver merge |
| 30 | GL | Gold publish — **inactive / not executed** |

Audit prefix: **`SAMMS FormQuestionAnswers`**.

**Note:** Gold publish (ConfigId 30) may exist in control tables but is **not executed**. Silver is the terminal layer.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 6` (Samms-Forms) |

### Notebook / Pipeline Entry Point

`nb_get_FormQuestionAnswers_taskconfig` → `flt_active_formquestionanswers_sites` → `nb_formqa_audit_start` → Bronze child → `set_formqa_bronze_results` → `nb_forms_bronze_to_silver` → `set_formqa_silver_results` → audit finalize / notify

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_FormQuestionAnswers_taskconfig` | `6e7b4814-5818-4715-9275-f6ad72743221` | Slim TaskConfig JSON (ConfigIds 28, 29) |
| `nb_formqa_audit_start` / `_finalize_clean_success` / `_finalize_failure` | `8c44b574-f4dc-498d-80bb-ff1ee87837f3` | Audit lifecycle; terminal layer SL |
| `nb_forms_build_site_sql` | `b25a2e2d-d7d0-4cdc-bd41-1b1f7f7253bd` | Per-site UNION ALL SQL from Forms2Process (Bronze child) |
| `nb_forms_bronze_to_silver` | `564ca691-a179-49f2-ad2f-3a389c1983f4` | RowState pre-pass + Delta MERGE (parent) |
| `nb_notify_failed` | `77c87686-120d-486b-9146-6a794d794e38` | Failure notification |

### Bronze Child Pipeline Activities (not notebooks)

| Pattern | Type | Purpose |
|---------|------|---------|
| `fe_each_samms_sites` | ForEach | Per-site extract (`batchCount = 5`) |
| `lkp_check_form_table` | Lookup | Gate — skip site if no `Form` table |
| `lkp_get_existing_tables` / `_columns` | Lookup | Clinic schema discovery for SQL builder |
| `cp_forms_to_bronze` | Copy | Execute generated SQL → Bronze Append |
| `mk_formqa_site_success` | Copy | Per-site success marker (retry = 3) |
| `set_child_bronze_method_results` | SetVariable | Return method JSON to parent |

| Field | Value |
|-------|-------|
| **Method / Function Names** | `FormQuestionAnswers` |
| **Language** | PySpark (notebooks); SQL (dynamic Copy) |
| **Error Strategy** | Per-site isolation in Bronze ForEach; Silver SKIPPED when no successful Bronze sites |
| **Retry Attempts** | 0 (Copy/Lookup); site marker retry = 3 |
| **Failure Notification** | `nb_notify_failed` on audit failure path |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Dynamic SQL builder** — unions base Form model + custom forms; skips missing tables/columns per clinic
- **Date filters** — per Forms2Process `DateFilterEnabled`; `p_reload` resets to 2010-01-01
- **Compound IsDeleted** — legacy joins to pre-admission and data-form tables in generated SQL
- **Within-run deduplication** — 7-column key + latest `_extracted_at` before Silver MERGE
- **RowState pre-pass** — unconditional reset for non-date-filtered forms; date-gated for others; `TP-*` under Treatment Plan entry
- **Full update on match** — no RowChkSum gate (always refresh matched Q&A fields)
- **Site success marker** — distinguishes "copy succeeded, zero rows" from "copy failed"

### Parent Pipeline Parameters and Variables

| Parameter / Variable | Default / Set by | Purpose |
|----------------------|------------------|---------|
| `p_lookback_days` | 30 | Incremental window |
| `p_reload` | false | Full reload from 2010-01-01 |
| `p_ingest_run_id` | pipeline RunId | Bronze row tag; Silver filter |
| `v_bronze_method_results_json` | `set_formqa_bronze_results` | Silver context, audit, notify |
| `v_silver_method_results_json` | `set_formqa_silver_results` | Audit finalize, notify |

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

Clinical form Q&A may contain **PHI**. Confirm classification with the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Clinical form responses |
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

- Row count captured and compared (`RowCount` metric) for ConfigIds 28 and 29
- Duplicate records identified (`DuplicateCount` metric) — verify all 7 merge key columns
- Null values quantified (`NullCount` metric)
- Overall `ValidationStatus` PASS or FAIL
- Audit log populated after each run (~115 Bronze + 1 Silver task)
- Site in `br_formqa_site_success` with zero Bronze rows → Silver SUCCESS with zero counts
- `TP-*` forms validated under Treatment Plan Forms2Process rules
- `Forms2Process` current before Bronze run

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Per site, per run — varies by form volume] |
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
| **Feature Branch** | [e.g. feature/samms-formqa-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure or confirmed bad form Q&A data |
| **Rollback Steps** | [e.g. Delta time travel on `tbl_dbo_FormQuestionAnswers`; re-run with corrected `p_lookback_days` or `p_reload`] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze append-only — rows accumulate by `_ingest_run_id` | Silver holds merged state; filter Bronze by ingest run for validation | |
| 002 | Per-clinic schema variance — not all form tables exist | SQL builder skips missing tables/columns silently | |
| 003 | Site skipped when no `Form` table | Expected — clinic not on SAMMS forms model | |
| 004 | Gold publish (ConfigId 30) inactive | Silver is operational terminal layer | |
| 005 | No RowChkSum gate on Silver MERGE | Full update on match — legacy parity for FormQA | |
| 006 | `Forms2Process` must be maintained in Silver | Update control table before adding new form types | |
| 007 | Dynamic SQL + gateway — long-running sites | Monitor Copy duration; `batchCount = 5` limits concurrency | |

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

*Microsoft Fabric | Pipeline Documentation | SAMMS Form Question Answers ETL | Generated from Technical Design*
