# Microsoft Fabric — Pipeline Documentation — SAMMS DartsSrv ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS DartsSrv ETL Pipeline |
| **Pipeline ID** | `Execute_DartSrv` (`12ad712f-3d73-48e9-b4b0-826ab69e388c`) |
| **Bronze Child Pipeline ID** | `dartSRV_Pipeline` (`b2101370-34a9-4695-bf4a-eebdb3ced50b`) |
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
| v1.0 | 14/07/2026 | [Name] | Initial draft — generated from SAMMS DartsSrv Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS DartsSrv ETL pipeline migrates and modernizes the legacy counseling-session ETL process (`BHGTaskRunner.exe 9` / SAMMS-ETL-DartSvc) within Microsoft Fabric. It extracts counseling and service session records from 80+ per-clinic SAMMS SQL Server databases and loads them into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

Unlike the legacy C# services — where `BulkDartsSvc` bulk-loaded staging tables and `SaveDartsSrvs` upserted into year-partitioned Azure tables (`pats.tbl_DartsSrv_20XX`) — the Fabric implementation separates orchestration, extraction, transformation, and auditing into reusable, configuration-driven components. This improves maintainability, scalability, and operational monitoring while preserving existing business logic: five-date incremental lookback, `RowChkSum` change detection, placeholder-client `RowState` handling, and optional-column compatibility across SAMMS versions.

The pipeline processes one method (`DartsSrv`) across all active clinic sites registered in `meta.taskconfig` (ConfigId 25).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds downstream billing, clinical counseling, and session reporting |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | `Dart_Source_to_Bronz` — one Copy activity per active site inside Bronze child ForEach |
| **Copy Job Object ID** | Embedded in child pipeline `dartSRV_Pipeline` |
| **Job Mode** | Batch (Bronze); Incremental via `p_lookback_days` (default 15) and five-date OR filter |
| **Write Behavior** | Bronze: Append (tagged by `_ingest_run_id`); Silver: Delta MERGE with RowChkSum branches |
| **Enable Staging** | No (Bronze Copy); N/A (Silver notebook) |
| **Table Option** | Silver table auto-created on first run if not present |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy and Lookup default); notebook retry per activity policy |

### Data Flow

Source (per-clinic SAMMS SQL Server) → Bronze child pipeline (`dartSRV_Pipeline`) → Bronze Lakehouse → Bronze-to-Silver notebook on parent (`nb_darts_bronze_to_silver`) → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | `tblDartsSrv` (from TaskConfig `SourceTable`, typically `dbo.tblDartsSrv`) |
| **Bronze** | `dartSRV_Pipeline` — ForEach + Lookup + Copy | Optional-column probe; dynamic SELECT; Append to `Dart.br_tblDartSrv`; `batchCount = 5` |
| **Silver** | `nb_darts_bronze_to_silver` notebook | Dedupe, `RowState`, `dsDtStartYear`; Delta MERGE with RowChkSum gate |
| **Destination** | Fabric Silver Lakehouse | `bhg_silver.pats.sl_tbldartsrv` — terminal layer for consumers |

### Parent Pipeline Activity Sequence

```
nb_get_darts_taskconfig
  → flt_active_darts_sites
  → nb_darts_audit_start
  → Exected_AfterBronz (dartSRV_Pipeline)
  → nb_darts_bronze_to_silver
  → nb_darts_audit_finalize_success / _failure
```

---

## 4. Source System

### Connection Details

| Field | Value |
|-------|-------|
| **Source Type** | SQL Server (SAMMS — one database per clinic) |
| **Server / Host** | On-premises via Fabric data gateway |
| **Database Name** | Per clinic — from TaskConfig `DataBaseName` (e.g. `SAMMS-Ahoskie`) |
| **Connection ID (Fabric)** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Authentication** | Fabric linked service / gateway |

### Source Tables

The pipeline processes one source table per active clinic site. Row counts, sizes, and primary key status are environment-specific and should be confirmed against the live source.

| Source Table | Bronze Table | Row Count | Size | Has PK? |
|--------------|--------------|-----------|------|---------|
| `dbo.tblDartsSrv` (typical) | `Dart.br_tblDartSrv` | [Per site / aggregate] | [MB/GB] | [Yes/No] |

**Active sites:** One TaskConfig row per clinic (ConfigId 25). Site count is defined in `meta.taskconfig` where `IsActive = 1`.

### Optional Columns (schema-varying by clinic)

Before Copy, `lkp_check_optional_columns_exist` probes the source table. Missing columns are replaced with NULL stubs in the Copy SELECT:

| Column | When Missing |
|--------|--------------|
| `ServiceType` | `CAST(NULL AS varchar(100))` |
| `dsTelehealthSession` | `CAST(NULL AS bit)` |
| `HoldId` | `CAST(NULL AS int)` |
| `upsize_ts` | `CAST(NULL AS varbinary(8))` |

### Load Strategy

| Field | Value |
|-------|-------|
| **Load Type** | Incremental — default 15-day lookback via pipeline parameter `p_lookback_days` |
| **Watermark Column** | N/A — multi-date OR filter used instead of single watermark |
| **Delta Filter Logic** | Rows where `dsClt IS NOT NULL` AND any of: `dsDtStart`, `dsDtAdded`, `dsUpdate`, `DSbilled`, or `dsSigDate` within lookback window, **OR** `dsClt <= 0` (placeholder rows always included) |
| **RowChkSum** | `CHECKSUM(...)` computed at source over core business columns during Bronze Copy |

**Legacy note:** C# ETL used dynamic lookback (-15 / -90 / -200 days). Fabric uses `p_lookback_days` unless extended by scheduler logic.

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `Dart` (Bronze); `pats` (Silver) |
| **Destination Table** | `br_tblDartSrv` (Bronze); `sl_tbldartsrv` (Silver — **final**) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (`_site_code` + `dsID` key; RowChkSum-gated update) |

### Source-to-Target Mapping

| Source Table | Bronze Table | Silver Table (Final) |
|--------------|--------------|----------------------|
| `dbo.tblDartsSrv` (per clinic) | `bhg_bronze.Dart.br_tblDartSrv` | `bhg_silver.pats.sl_tbldartsrv` |

### Key Column Mappings / Transformations

| Source Column | Source Value | Dest Column | Dest Value | Notes |
|---------------|--------------|-------------|------------|-------|
| Clinic site code (injected) | `AHK` | `_site_code` | `AHK` | Added in Copy SELECT |
| `dsID` | Session ID | `dsID` | Same | Part of 2-column merge key |
| `dsClt` | Negative client ID | `RowState` | `0` | Placeholder client |
| `dsClt` | Non-negative | `RowState` | `1` | Active session |
| `dsDtStart` | Session date | `dsDtStartYear` | e.g. `2026` | Replaces legacy year-table routing |
| `RowChkSum` | CHECKSUM at source | `RowChkSum` | Same | Gates full vs metadata-only Silver update |
| N/A | Run timestamp | `silver_created_at` | Current timestamp | INSERT only |
| N/A | Run timestamp | `silver_updated_at` | Current timestamp | Refreshed on data update |
| N/A | Run timestamp | `last_seen_at` | Current timestamp | Refreshed on every merge match |

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [e.g. confirm against live SAMMS — large text/signature columns] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | [List column names — e.g. `dstxtNote`, `dsRTBNOTE`, signature image blobs] |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

Each active clinic site is registered as a row in `bhg_bronze.meta.taskconfig` (ConfigId 25). Silver has one TaskConfig row (ConfigId 26).

| Field | Bronze (ConfigId 25) | Silver (ConfigId 26) |
|-------|----------------------|----------------------|
| **TaskName / endpoint_name** | Per site — e.g. `DartsSrv Bronze` | `DartsSrv Silver` |
| **Method** | `DartsSrv` | `DartsSrv` |
| **source_schema** | `dbo` (typical) | N/A |
| **source_table / SourceTable** | `dbo.tblDartsSrv` (typical) | N/A |
| **DataBaseName** | e.g. `SAMMS-Ahoskie` | N/A |
| **SiteCode** | e.g. `AHK` | N/A |
| **destination_schema** | `Dart` | `pats` |
| **destination_table / TargetTable** | `br_tblDartSrv` | `sl_tbldartsrv` |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | ConfigName |
|----------|------------|------------|
| 25 | BR | SAMMS DartsSrv Bronze Pipeline |
| 26 | SL | SAMMS DartsSrv Silver Pipeline |

Audit prefix: **`SAMMS DartsSrv`**.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 9` (SAMMS-ETL-DartSvc) |

### Notebook / Pipeline Entry Point

`nb_get_darts_taskconfig` → `flt_active_darts_sites` → `nb_darts_audit_start` → `Exected_AfterBronz` (`dartSRV_Pipeline`) → `nb_darts_bronze_to_silver` → `nb_darts_audit_finalize_success` / `_failure`

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_darts_taskconfig` | `6e7b4814-5818-4715-9275-f6ad72743221` | Retrieves active TaskConfig metadata; returns slim JSON to pipeline |
| `nb_darts_audit_start` / `_finalize_success` / `_failure` | `9d0b3480-fa72-4814-ad31-7bbec83a3301` | Audit lifecycle (START_LAYER_RUNS / FINALIZE_SUCCESS / FINALIZE_FAILURE) |
| `nb_darts_bronze_to_silver` | `89769158-4ba9-4e86-9b6d-ff22852dddd5` | Bronze → Silver dedupe, derive columns, Delta MERGE |

### Bronze Child Pipeline Activities (not notebooks)

| Activity | Type | Purpose |
|----------|------|---------|
| `fe_each_samms_database_sites` | ForEach | Iterates active sites (`batchCount = 5`) |
| `lkp_check_optional_columns_exist` | Lookup | Probes optional columns on source table |
| `Dart_Source_to_Bronz` | Copy | Dynamic SELECT → Append Bronze |

| Field | Value |
|-------|-------|
| **Method / Function Name** | `DartsSrv` |
| **Language** | PySpark (Silver notebook); SQL (Bronze Copy expression) |
| **Error Strategy** | Per-site isolation in Bronze ForEach; Silver fails if zero Bronze rows for run |
| **Retry Attempts** | 0 (Copy/Lookup); per notebook activity policy |
| **Failure Notification** | Logged to audit framework (`PipelineRun` / `TaskQueue` / `TaskAudit`); Teams notification notebooks exist but are **Inactive** |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Metadata tagging** — `_site_code`, `_source_database`, `_ingest_run_id`, `_extracted_at`, lookback date columns appended at Bronze extraction
- **Optional-column stubbing** — missing SAMMS columns replaced with typed NULLs so Copy succeeds across schema versions
- **Within-run deduplication** — `_site_code` + `dsID`; latest `_extracted_at` wins before Silver MERGE
- **RowState derivation** — `dsClt >= 0` → active (1); negative → placeholder (0)
- **Year column** — `dsDtStartYear` from `dsDtStart` replaces legacy year-partitioned target tables
- **Change detection** — `RowChkSum` comparison at Silver MERGE: full update when changed; metadata-only touch when unchanged
- **Upsert processing** — INSERT new sessions; UPDATE changed sessions; touch `last_seen_at` on unchanged sessions still in lookback window
- **Schema evolution** — auto-add `upsize_ts` to Silver if table predates column

### Silver MERGE Branches

| Branch | Condition | Action |
|--------|-----------|--------|
| Matched — data changed | `RowChkSum` differs or either side NULL | Full column update |
| Matched — unchanged | `RowChkSum` equal | Update `last_seen_at` and ingest metadata only |
| Not matched | New `_site_code` + `dsID` | INSERT with `silver_created_at` |

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

Counseling session records include patient identifiers, clinical notes, staff names, billing data, and signature fields — indicating the dataset **likely contains PHI**. This should be formally confirmed by the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Counseling / clinical session data |
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

The audit framework's `DataQuality` table captures the following validation metrics automatically after each successful finalize:

- Row count captured and compared (`RowCount` metric) for ConfigIds 25 and 26
- Duplicate records identified (`DuplicateCount` metric)
- Null values quantified for loaded columns (`NullCount` metric)
- Overall `ValidationStatus` recorded as PASS or FAIL
- Audit log (`TaskAudit`, `PipelineRun`, `TaskQueue`) populated after each run
- Silver notebook raises exception if zero Bronze rows exist for the ingest run

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Actual row count from source — per run, per site] |
| **Destination Row Count** | [Bronze append count vs Silver merge count for ingest run] |
| **Load Duration** | [e.g. 45 mins] |
| **Throughput** | [e.g. rows/min] |

---

## 10. DevOps & Source Control

### Repository Details

| Field | Value |
|-------|-------|
| **Azure DevOps Org** | [Organisation name] |
| **Repository Name** | [e.g. fabric-pipelines] |
| **Feature Branch** | [e.g. feature/samms-dartsrv-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure or confirmed bad data post-run |
| **Rollback Steps** | [Step-by-step — e.g. Delta time travel on `sl_tbldartsrv`; re-run with corrected `p_lookback_days`; fix failed site TaskConfig] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze is append-only — historical Bronze snapshots accumulate by `_ingest_run_id` | Silver holds merged current state; use ingest run filter for run-scoped validation | |
| 002 | Audit finalize in exported pipeline JSON depends on Gold publish step | Rewire finalize to depend on `nb_darts_bronze_to_silver` for Silver-only operation | [DD/MM/YYYY] |
| 003 | Audit start requires active GL row in `meta.etlconfig` even when Gold is out of scope | Keep GL config active or refactor audit notebook | [DD/MM/YYYY] |
| 004 | Legacy dynamic lookback (-15 / -90 / -200) not built into Fabric pipeline | Pass appropriate `p_lookback_days` from scheduler on month-end / special dates | |
| 005 | No per-site Bronze success marker — zero-row site is silent at site level | Compare per-site Bronze counts in post-run validation | |

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

*Microsoft Fabric | Pipeline Documentation | SAMMS DartsSrv ETL | Generated from Technical Design*
