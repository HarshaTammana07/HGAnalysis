# Microsoft Fabric — Pipeline Documentation — SAMMS Notes ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS Notes ETL Pipeline |
| **Pipeline ID** | `pl_execute_notes` (`189b74e5-ef11-4ee4-afb8-4a9b8a576f30`) |
| **Bronze Child Pipeline ID** | `pl_note_saams_to_lakehouse` (`61f2955b-68c9-4a3b-8da7-186a0ce8e23e`) |
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
| v1.0 | 14/07/2026 | [Name] | Initial draft — generated from SAMMS Notes Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS Notes ETL pipeline migrates and modernizes the legacy clinical and billing notes ETL process (`BHGTaskRunner.exe 7` / SAMMS-ETL-Notes) within Microsoft Fabric. It extracts **AR Notes** and **Claim Notes** from 115+ per-clinic SAMMS SQL Server databases and loads them into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

Unlike the legacy C# save operations — where extraction and upsert were coupled per note type — the Fabric implementation separates Bronze extraction (dedicated child pipeline with parallel Copy loops) from Silver merge (two notebooks on the parent). This improves maintainability and operational monitoring while preserving incremental lookback logic, `RowChkSum` change detection, and **partial success** when one note type or site fails.

The pipeline processes **two methods** (`3pArnote`, `3pClaimNote`) across all active clinic sites registered in `meta.taskconfig` (ConfigId 34).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds clinical and billing note reporting |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | `cp_arnote_to_bronze` / `cp_claimnote_to_bronze` — one Copy per site per method in Bronze child |
| **Copy Job Object ID** | Embedded in child pipeline `pl_note_saams_to_lakehouse` |
| **Job Mode** | Batch (Bronze); Incremental via `p_work_date` + `p_lookback_days` (default 15) |
| **Write Behavior** | Bronze: Append (tagged by `_ingest_run_id`); Silver: Delta MERGE with RowChkSum gate |
| **Enable Staging** | No (Bronze Copy) |
| **Table Option** | Silver tables created or normalized on first run |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy and Lookup default); notebook retry per activity policy |

### Data Flow

Source (per-clinic SAMMS SQL Server) → Bronze child (`pl_note_saams_to_lakehouse`) → Bronze Lakehouse → two Silver notebooks on parent → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | `tbl3pARNOTE`, `tbl3pClaimNote` |
| **Bronze** | `pl_note_saams_to_lakehouse` — 2 parallel Filter + ForEach + Lookup + Copy | Incremental extract; optional `globalBatchId`; `batchCount = 5` |
| **Silver** | `nb_3parnote_bronze_to_silver` + `nb_3pclaimnote_bronze_to_silver` (parent) | Parallel Delta MERGE; TaskConfig-driven table names |
| **Destination** | Fabric Silver Lakehouse | `pats.tbl_3pARNOTE`, `pats.tbl_3pClaimNote` — terminal layer |

### Parent Pipeline Activity Sequence

```
nb_get_notes_taskconfig
  → flt_active_notes_sites
  → nb_notes_audit_start
  → Executed_AfterBronz (pl_note_saams_to_lakehouse)
  → set_bronze_method_results_from_child
  → nb_3parnote_bronze_to_silver  } parallel on parent
  → nb_3pclaimnote_bronze_to_silver }
  → set_notes_method_results
  → if_all_notes_methods_success
      → nb_notes_audit_finalize_success / _failure
  → nb_notes_notify_failed (on IfCondition Failed/Skipped)
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
| 1 | `3pArnote` | `tbl3pARNOTE` | `Notes.br_tbl3pArnote` | [Per site] | [MB/GB] | [Yes/No] |
| 2 | `3pClaimNote` | `tbl3pClaimNote` | `Notes.br_tbl3pClaimNote` | [Per site] | [MB/GB] | [Yes/No] |

**Active sites:** ~115 clinics — one TaskConfig row per site per method (ConfigId 34). Typical Bronze task volume: ~230 site × method tasks per run.

### Optional Column Handling

| Column | Behavior |
|--------|----------|
| `globalBatchId` | Lookup pre-check; included in Copy and RowChkSum when present; NULL stub when absent |

### Load Strategy

| Field | Value |
|-------|-------|
| **Load Type** | Incremental — lookback from `p_work_date` minus `p_lookback_days` (default 15) |
| **Minimum date floor** | `2023-01-01` — notes before this date excluded from date filters |
| **Watermark Column** | N/A — multi-column date OR logic per method |
| **RowChkSum** | `CHECKSUM(...)` computed at source in Bronze Copy; adapts when `globalBatchId` missing |

**Incremental WHERE logic:**

| Method | Rows included when |
|--------|-------------------|
| `3pArnote` | `arnDATE` on/after lookback (and on/after 2023-01-01), **or** `arnDtRemoved` on/after lookback |
| `3pClaimNote` | `tpcnDtmAdded` on/after lookback (and on/after 2023-01-01), **or** `tpcnDtTickler` on/after lookback |

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `Notes` (Bronze); `pats` (Silver) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (RowChkSum-gated update) |

### Source-to-Target Mapping

| Source Table | Bronze Table | Silver Table (Final) |
|--------------|--------------|----------------------|
| `tbl3pARNOTE` | `bhg_bronze.Notes.br_tbl3pArnote` | `bhg_silver.pats.tbl_3pARNOTE` |
| `tbl3pClaimNote` | `bhg_bronze.Notes.br_tbl3pClaimNote` | `bhg_silver.pats.tbl_3pClaimNote` |

### Key Column Mappings / Silver Merge Keys

| Method | Merge Key | Notable Rule |
|--------|-----------|--------------|
| `3pArnote` | `SiteCode` + `arnID` | RowChkSum gate — update only when checksum changed |
| `3pClaimNote` | `SiteCode` + `tpcnTPCID` | Match on **`tpcnTPCID`**, not `tpcn` (legacy parity) |

| Transformation | Detail |
|----------------|--------|
| `_site_code` → `SiteCode` | Renamed at Silver |
| `RowState` | Set `true` for all rows returned in incremental extract |
| `LastModAt` | Current timestamp on merge |
| Bronze metadata | `_ingest_run_id`, `_extracted_at`, etc. — not carried to Silver |

### Silver Final Columns (reporting-ready)

**AR Note:** `SiteCode`, `arnID`, `arnLIID`, `arnNOTE`, `arnUSER`, `arnDATE`, `arnDtRemoved`, `arnStrRemovedReason`, `arnStrRemovedUser`, `bid`, `arnDBnotes`, `globalBatchId`, `RowChkSum`, `LastModAt`, `RowState`

**Claim Note:** `SiteCode`, `tpcn`, `tpcnTPCID`, `tpcnDtmAdded`, `tpcnStrAdded`, `tpcnStrNote`, `tpcnStrType`, `tpcnDtTickler`, `tpcnDtTicklerRemoved`, `tpcnStrTicklerRemovedNote`, `tpcnStrTicklerRemovedUser`, `tpcnStrTicklerType`, `globalBatchId`, `RowChkSum`, `LastModAt`, `RowState`

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [Confirm — note text columns may be large] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | [e.g. `arnNOTE`, `tpcnStrNote`, `arnDBnotes`] |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

| Field | Bronze (ConfigId 34) | Silver (ConfigId 35) |
|-------|----------------------|----------------------|
| **Method** | `3pArnote` or `3pClaimNote` | Same method name |
| **SiteCode** | Per clinic | N/A (method-level Silver task) |
| **DataBaseName** | Per clinic | N/A |
| **SourceTable** | e.g. `tbl3pARNOTE` | N/A |
| **destination_schema** | `Notes` | `pats` |
| **destination_table** | `br_tbl3pArnote` / `br_tbl3pClaimNote` | `tbl_3pARNOTE` / `tbl_3pClaimNote` |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 34 | BR | Bronze extraction |
| 35 | SL | Silver merge |

Audit prefix: **`SAMMS Notes`**.

**Note:** Gold publish activities exist in the pipeline definition but are **Inactive**. Silver is the terminal layer.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 7` (SAMMS-ETL-Notes) |

### Notebook / Pipeline Entry Point

`nb_get_notes_taskconfig` → `flt_active_notes_sites` → `nb_notes_audit_start` → `Executed_AfterBronz` → `set_bronze_method_results_from_child` → Silver notebooks → `set_notes_method_results` → audit finalize / notify

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_notes_taskconfig` | `6e7b4814-5818-4715-9275-f6ad72743221` | Slim TaskConfig JSON (replaces inactive Lookup) |
| `nb_notes_audit_start` / `_finalize_success` / `_finalize_failure` | `9d0b3480-fa72-4814-ad31-7bbec83a3301` | Dynamic audit lifecycle; reads dq keys from TaskConfig |
| `nb_3parnote_bronze_to_silver` | `4057574c-6a08-4c3e-8584-9ead64ee8608` | Silver MERGE — AR Notes |
| `nb_3pclaimnote_bronze_to_silver` | `ecb28154-151c-46c5-8c67-99940dd9d570` | Silver MERGE — Claim Notes |
| `nb_notes_notify_failed` | `77c87686-120d-486b-9146-6a794d794e38` | Failure notification (active) |

### Bronze Child Pipeline Activities (not notebooks)

| Pattern | Type | Purpose |
|---------|------|---------|
| `flt_child_arnote_sites` / `flt_child_claimnote_sites` | Filter | Split sites by method |
| `fe_each_samms_site_arnote` / `_claimnote` | ForEach | Per-site Copy (`batchCount = 5`) |
| `lkp_check_*_globalbatchid_exists` | Lookup | Optional column probe |
| `cp_arnote_to_bronze` / `cp_claimnote_to_bronze` | Copy | Incremental SAMMS → Bronze Append |
| `set_child_bronze_method_results` | SetVariable | Return per-method JSON to parent |

| Field | Value |
|-------|-------|
| **Method / Function Names** | `3pArnote`, `3pClaimNote` |
| **Language** | PySpark (Silver notebooks); SQL (Bronze Copy) |
| **Error Strategy** | Per-site and per-method isolation; partial success via method JSON; Silver SKIPPED when Bronze method failed |
| **Retry Attempts** | 0 (Copy/Lookup); per notebook activity policy |
| **Failure Notification** | `nb_notes_notify_failed` active; `nb_notes_notify_success` **Inactive** |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Incremental extract** — date lookback with 2023-01-01 floor; removed/tickler date columns included
- **Optional `globalBatchId`** — dynamic Copy SQL and checksum adaptation
- **Within-run deduplication** — business key + latest `_extracted_at` before Silver MERGE
- **TaskConfig-driven table resolution** — Silver notebooks resolve Bronze/Silver table names at runtime
- **RowChkSum gate** — update Silver only when checksum changed; insert new keys
- **RowState** — active (`true`) for all rows in current incremental batch
- **Per-method status JSON** — Bronze child → parent variable → Silver → parent variable → audit

### Parent Pipeline Variables

| Variable | Set by | Consumed by |
|----------|--------|-------------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver notebooks, audit, notify |
| `v_silver_method_results_json` | `set_notes_method_results` | Audit finalize, notify |

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

Clinical and billing note text may contain **PHI**. Confirm classification with the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Clinical / billing notes |
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

- Row count captured and compared (`RowCount` metric) for ConfigIds 34 and 35
- Duplicate records identified (`DuplicateCount` metric)
- Null values quantified (`NullCount` metric)
- Overall `ValidationStatus` PASS or FAIL
- Audit log populated after each run
- Per-method bronze/silver JSON checked for `FAILED`, `ERROR`, or `SKIPPED`
- ClaimNote merge validated on `tpcnTPCID` not `tpcn`
- Zero-row Silver SUCCESS accepted when Bronze succeeded but no notes in lookback window

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Per method, per run] |
| **Destination Row Count** | [Bronze append vs Silver merge] |
| **Load Duration** | [e.g. 60–90 mins] |
| **Throughput** | [e.g. rows/min] |

---

## 10. DevOps & Source Control

### Repository Details

| Field | Value |
|-------|-------|
| **Azure DevOps Org** | [Organisation name] |
| **Repository Name** | [e.g. fabric-pipelines] |
| **Feature Branch** | [e.g. feature/samms-notes-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure or confirmed bad note data |
| **Rollback Steps** | [e.g. Delta time travel on `tbl_3pARNOTE` or `tbl_3pClaimNote`; re-run with corrected `p_work_date`] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze append-only — rows accumulate by `_ingest_run_id` | Silver holds merged state; filter Bronze by ingest run for validation | |
| 002 | `lkp_notes_taskconfig` Lookup inactive — notebook required | Do not re-enable Lookup (4 MB limit) | |
| 003 | Gold publish activities inactive | Silver is operational terminal layer | |
| 004 | RowChkSum unchanged → no Silver update | Expected legacy behavior — not a defect | |
| 005 | ClaimNote must merge on `tpcnTPCID` | Verify TaskConfig and notebook match key | |
| 006 | IfCondition string contains check for FAILED/SKIPPED | Rare false trigger if error text contains those tokens | |

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

*Microsoft Fabric | Pipeline Documentation | SAMMS Notes ETL | Generated from Technical Design*
