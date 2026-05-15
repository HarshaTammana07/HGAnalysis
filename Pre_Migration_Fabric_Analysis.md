# BHG ETL → Microsoft Fabric / OneLake: Pre-Migration Analysis
**Purpose:** Complete inventory of all patterns, anomalies, categories, and critical differences  
found across all documentation before beginning Fabric / OneLake medallion migration.  
**Audience:** Data Architect / Senior Data Engineer  
**Date:** April 2026  
**Scope:** All 11 ETL pipelines, 98 destination tables, 80+ source sites

---

## How to Read This Document

This is your **migration baseline** — a map of every critical pattern, difference, risk, and  
category observed in the current C# ETL system. Each section directly answers the question:  
**"What do we need to replicate, rethink, or flatten in Fabric?"**

---

## 1. THE FUNDAMENTAL ARCHITECTURE IN ONE PICTURE

```
ON-PREMISE (80–113 SAMMS SQL Server databases)
    │
    │  one connection per clinic, same table/column names per domain,
    │  different patient data per site
    │
    ▼
BHGTaskRunner.exe (Windows Server, sequential loop per site per table)
    │
    │  SelectConstructor builds SELECT from metadata (dms.tbl_MapSrc2Dsn)
    │  SQLSvrManager fires query against each SAMMS via ADO.NET
    │
    ▼
EITHER:
  A) EF Core row-by-row upsert → straight to pats.* final tables (Azure SQL)
  B) SqlBulkCopy → stg.* staging table → stg.XxxMerge stored proc → pats.* final tables

DESTINATION: Azure SQL (BHG_DR)
    pats.*  → patient/clinical data (the main warehouse)
    ctrl.*  → config/reference data
    stg.*   → staging (transient, truncated before + after each bulk load)
    dms.*   → ETL metadata / column mappings
    tsk.*   → task queue / scheduling
    ayx.*   → analytics / reporting overflows
```

---

## 2. THE TWO LOAD PATTERNS — MOST IMPORTANT DISTINCTION

Everything in this system ultimately uses one of two patterns. **You must migrate both.**

---

### PATTERN A — EF Core Row-by-Row Upsert (the "slow path")

**How it works:**
1. Read source rows into a `DataTable` via ADO.NET.
2. Load the ENTIRE existing slice for this site from Azure into a C# `List<T>` in memory.
3. Loop row-by-row: for each source row, find the match in the List by primary key.
4. Compare `RowChkSum`: if changed → update; if absent → insert; if same → skip.
5. `db.SaveChanges()` once at the end (batch for inserts, individual for updates).

**Used for:** Most tables in the system — enrollment, clients, UA results, orders, assessments, billing, claims, etc.

**Performance problem:** Loading the whole site slice into C# memory is O(N) on memory. The `LINQ.Where` inside the foreach loop is O(N²). For large sites this is the biggest bottleneck in the entire system.

**What to replace it with in Fabric:**
- Delta Lake `MERGE` on `(SiteCode, PrimaryKey)` with condition `target.RowChkSum <> source.RowChkSum`
- No in-memory list loading — Delta engine handles merge set-based in storage

---

### PATTERN B — SqlBulkCopy + Stored Procedure MERGE (the "fast path")

**How it works:**
1. Read source rows into a `DataTable` via ADO.NET.
2. `TRUNCATE` the staging table (`stg.tbl_xxx`).
3. `SqlBulkCopy.WriteToServer(DataTable)` → all rows land in staging in one shot.
4. Execute merge stored procedure(s): `exec stg.XxxMerge` → SQL Server runs `MERGE INTO pats.tbl_xxx`.
5. `TRUNCATE` staging again (clean up).

**Used for:** High-volume tables only:
- DartsSrv (counseling sessions) → 8 year-partitioned merge procs
- Dose + DoseExcuse → `stg.DoseMerge` / `stg.Dose_ExcuseMerge`
- Claims, ClaimLineItem, ClaimLineItemActivity → 3 merge procs
- LiquidLog → `stg.sp_liquidlog_Merge`
- UAResultDetail, LABResultDetail
- ClientDemo → `stg.ClientDemoMerge1` + `stg.ClientDemoMerge2`
- FormsSAMMSClient, FormQA, BillSub

**What to replace it with in Fabric:**
- Bronze: land raw JDBC pull per site (append or overwrite by run)
- Silver: Delta `MERGE` replaces the stored proc MERGE
- The staging table pattern **disappears** — Delta itself becomes the transactional layer
- The 8 DartsSrvMerge procs collapse into one parameterized Delta merge (filter by year)

---

### Critical: Some Tables Have BOTH Paths Available

Several tables have code for EF path AND bulk path. The router (`BHGTaskRunner/Program.cs`) picks one based on site, volume, or flags:

| Table | EF Path | Bulk Path | Why Both? |
|-------|---------|-----------|-----------|
| `pats.tbl_DartsSrv_*` | `SaveDartsSrvs.cs` | `BulkDartsSvc` + `stg.DartsSrvMerge*` | Bulk = daily; EF = backfill/WrkYear |
| `pats.tbl_Dose` | `SaveDoses.cs` | `BulkDartsSvc` + `stg.DoseMerge` | Reload flag switches to EF |
| `pats.tbl_Claims` | `SaveClaims.cs` | `BulkDartsSvc` + `stg.ClaimsMerge` | Volume-based routing |
| `pats.tbl_ClientDemo1/2` | `SaveCleints.cs` | `stg.ClientDemoMerge1/2` | LAB site uses bulk path |
| `pats.tbl_FormQuestionAnswers` | `SaveFormQAData.cs` | `stg.sp_FormQA_Merge` | Site-specific routing |

**Migration implication:** In Fabric there is only one path — Delta MERGE. The dual-path complexity dissolves. But you need parity testing against both C# paths during validation.

---

## 3. YEAR-PARTITIONED TABLE PATTERN — HIGH RISK

Three domains use year-partitioned tables. Each year is a separate physical table in Azure SQL today. This is the **hardest design decision** for Fabric.

### DartsSrv (counseling sessions) — 11 tables
```
pats.tbl_DartsSrv_2014B4   (2008–2014 combined)
pats.tbl_DartsSrv_2015
pats.tbl_DartsSrv_2016
...
pats.tbl_DartsSrv_2023
pats.tbl_DartsSrv_2024
pats.tbl_DartsSrv_2025 / 2026 / 2027 / 2028  (future-proofed)
```
C# has 10 separate methods (`SaveDartSrv2014` … `SaveDartSrv2023`) — one per EF DbSet.  
Bulk path calls 8 stored procs in sequence after one staging load.

### Orders (prescriptions) — 13 tables
```
pats.tbl_Orders   (base / pre-2016)
pats.tbl_Orders_2016 through pats.tbl_Orders_2028
```
Year split happens **in C# memory**: one query returns all orders for a site, then `SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == year)` splits them before calling per-year methods.

### Dose — NOT year-partitioned
Dose is a single table (`pats.tbl_Dose`) with reload-flag support.

### What this means for Fabric / OneLake

| Option | Trade-off |
|--------|-----------|
| **Keep year as partition column** in one Silver Delta table (recommended) | One table, partition pruning replaces physical split; simpler |
| **Replicate physical year tables as separate Delta tables** | Preserves existing consumer SQL but multiplies objects |
| **Hybrid: one Silver table, year-bucketed Gold views** | Most flexible — Silver unified, Gold preserves old query patterns |

**Recommendation:** Collapse year tables into one Silver Delta table per domain, with `year(ServiceDate)` or `year(OrderDate)` as a **partition column**. Delta partition pruning gives the same query performance as separate tables. The 10 duplicate C# methods and 8 merge procs collapse into one parameterized merge.

---

## 4. CHANGE DETECTION PATTERNS — THREE DISTINCT BEHAVIORS

How the system decides whether to write a row.

### Pattern 1: RowChkSum (used by ~85% of tables)
```sql
CHECKSUM([col1], [col2], ...) AS RowChkSum
```
Computed at source during SELECT (via `SelectConstructor.GetSLT()`).  
Stored in destination alongside the row.  
On next run: if `target.RowChkSum == source.RowChkSum` → skip entirely.

**Fabric migration:** Replicate exactly. Compute `CHECKSUM(...)` in the JDBC SELECT or recompute in PySpark using `hash()` on the same columns. Keep `RowChkSum` as a column in Silver. Use it as the `whenMatchedUpdate` condition in Delta MERGE.

### Pattern 2: ActionKey=3 — Full Delete + Reload (NO checksum)
```csharp
if (st.ActionKey == 3) { ChkSumEnabled = false; }
```
For these tables, NO `RowChkSum` is added to the SELECT. The load strategy is: **TRUNCATE destination, INSERT all rows**. No incremental logic.

Tables in this group are reference/lookup tables (code lists, dropdown values) that are small enough to reload fully every run.

**Fabric migration:** These tables in Silver simply become `OVERWRITE` mode Delta writes — no merge needed. Still idempotent.

### Pattern 3: Reload Flag (specific tables)
Some tables support a boolean `Reload` or `ReInitialize` flag on the task record:
- `SaveDoses`: `reload=true` → DELETE all rows for the site before re-inserting
- `SaveOrders`: `ReInitialize=true` → triggers full reload
- `SaveBills`: `yearly=true` → changes lookback scope

**Fabric migration:** These become pipeline parameters. Pass `reload_mode` as a pipeline parameter per site. In Bronze, tag rows with `_load_mode = 'reload' | 'incremental'`. In Silver, branch on this before MERGE.

---

## 5. LOOKBACK WINDOW PATTERNS — MUST REPLICATE EXACTLY

The ETL does NOT pull all data on every run. It uses a **date lookback window** to limit source queries. Different tables and different situations use different windows.

### Standard lookback: -15 days
Used by most tables. Controlled by `DaysBack` variable in `BHGTaskRunner.Program.cs`.

### DartsSrv special lookback (three tiers)
```csharp
int offsetvalue = -15;
if (WorkDate.DayOfWeek == DayOfWeek.Friday && WorkDate.Month == WorkDate.AddDays(1).Month)
    offsetvalue = -90;
if (WorkDate.Date == DateTime.Parse("1/24/2025"))
    offsetvalue = -200;
DateTime DartsDate = WorkDate.AddDays(offsetvalue);
```
- **Normal run**: -15 days
- **Last Friday of month**: -90 days
- **Special override dates** (hardcoded like `1/24/2025`): -200 days

**DartsSrv multi-column WHERE clause:**
```sql
WHERE dsClt IS NOT NULL
  AND (
    convert(date, dsDtStart)  >= DartsDate OR
    convert(date, dsDtAdded)  >= DartsDate OR
    convert(date, dsUpdate)   >= DartsDate OR
    convert(date, dsBilled)   >= DartsDate OR
    convert(date, dsSigDate)  >= DartsDate OR
    dsClt <= 0
  )
```
Five date columns checked — because a session can be billed, signed, or updated on a different day than it started. All five columns act as "activity indicators."

### Other notable lookbacks
| Table | Lookback Logic |
|-------|---------------|
| `tbl_Bills` | `DaysBack` parameter + `yearly` flag for full-year loads |
| `tbl_Dose` | Year-based or reload-based |
| `tbl_ClaimStatus` | 12-month rolling window |
| `tbl_FormsSAMMSClient` | `firsthalf` flag splits large loads into two batches |
| `tbl_PreAdmissionV6` | Schema version check gates entire run |

**Fabric migration:** Replace hardcoded date logic with a **lookback config table** in metadata (`meta.tbl_lookback_config`). Store `standard_days = -15`, `month_end_days = -90`, `override_dates` as rows. Pipeline reads this at runtime. Override dates must move from C# code into the config table or a pipeline parameter.

---

## 6. SCHEMA VERSION GUARD PATTERN — CRITICAL FOR 80+ SITES

Different SAMMS databases are on different versions. The current system guards against this in code. You must replicate these guards in Fabric.

### Column existence check (ServiceType example)
```csharp
DataTable tblDartcols = sm.GetTableData("tcols",
    "select name from sys.all_columns where object_id = " +
    "(select object_id from sys.all_objects where upper(name) = 'TBLDARTSSRV') " +
    "and name = 'ServiceType'", st.ConStr);
if (tblDartcols.Rows.Count == 0)
    strCmd = strCmd.Replace(", [ServiceType] ServiceType", "").Replace(", [ServiceType]", "");
```
Before running the Darts SELECT against each clinic, the system checks whether `ServiceType` column exists. If not → strips it from the query.

### Table existence check (PreAdmission V6)
```
SavePreAdmissionV6: checks for SF_PatientPreAdmission table.
If not present → site is still on V5 schema → skip entirely.
```

### Site-specific column exclusions
```csharp
// SaveClinic.cs — Lab site
if (st.SiteCode == "Lab")
    strCmd = strCmd.Replace("[PullPicsFromDB]", "null PullPicsFromDB");

// Save3pArnote.cs — LAB site strips globalBatchId
if (st.SiteCode == "Lab")
    strCmd = strCmd.Replace(", [globalBatchId] globalBatchId", "");
```

### Known site exceptions across all docs
| SiteCode | Exception |
|----------|-----------|
| `PHC` | Excluded from BHGTaskRunner Schedule 9; uses separate PHC.exe runner; PHC-specific Save* variants in `/PHC` folder |
| `Lab` | Multiple column strips (PullPicsFromDB, globalBatchId); skipped for SaveEnrollment |
| `B28`, `B42A` | Site-specific column exclusions in SaveClinic |
| `V10A`, `CBCO`, `V21`, `V10` | Custom Dose lookback logic |

**Fabric migration:** Build a **site feature flag table** (`meta.dim_site_features`):
- `has_service_type` (bool)
- `has_sf_preadmission` (bool)
- `has_pull_pics_from_db` (bool)
- `schema_version` (V5 / V6 / Methasoft)
- `runner` (BHG / PHC)

Notebooks read this table at runtime and conditionally include/exclude columns in the JDBC SELECT. Never hardcode site-specific logic in notebook code.

---

## 7. PHC vs BHG — SEPARATE PIPELINE PATTERN

PHC clinics run through `PHC/PHC.exe`, not `BHGTaskRunner.exe`. There are **PHC-specific versions** of several key files:

| File | BHG-DR-LIB Version | PHC/ Version |
|------|-------------------|--------------|
| `BulkDartsSvc.cs` | Standard bulk loader | PHC variant |
| `SaveBills.cs` | Standard billing | PHC billing rules |
| `SaveFormQAData.cs` | Standard forms | PHC forms variant |
| `SavePreAdmissionV6.cs` | Standard pre-admission | PHC pre-admission |
| `SaveUAResults.cs` | Standard UA results | PHC UA results |

The `dms.tbl_MapSrc2Dsn` table has a `PHC_Enabled` column per field — when `SiteCode == "PHC"`, `SelectConstructor` filters column mappings to `PHC_Enabled = true` only.

**Fabric migration:** PHC becomes its own pipeline branch in Fabric. Same notebooks, different parameters and feature flags. `PHC_Enabled` column from `dms.tbl_MapSrc2Dsn` migrates into `meta.map_column.phc_enabled`. A single parameterized notebook handles both BHG and PHC runs based on a `runner_type` parameter.

---

## 8. METADATA-DRIVEN COLUMN MAPPING — THE ENGINE BEHIND EVERYTHING

The system does NOT hardcode SELECT column lists in C# code. It uses **two Azure SQL tables** as the source of truth for what to SELECT:

### `dms.tbl_MapAction` — one row per (site × table)
Defines: source schema, source table/view name, destination schema/table, WHERE condition, sort order, whether enabled, connection string.

### `dms.tbl_MapSrc2Dsn` — one row per column per table
Defines: source column name, destination column alias, data type, whether it's a primary key, whether it's included in `CHECKSUM()`, whether it applies to PHC.

**`SelectConstructor.GetSLT()`** assembles the SELECT dynamically:
```
SELECT [col1] alias1, [col2] alias2, ..., 'B01' SiteCode,
       CHECKSUM([col1], [col2], ...) RowChkSum
FROM dbo.tblXxx
```

**Fabric migration:** This metadata system is one of the best things in the current architecture. **Replicate it in OneLake.**
- Mirror `dms.tbl_MapAction` → `meta.dim_pipeline_table` in Lakehouse
- Mirror `dms.tbl_MapSrc2Dsn` → `meta.dim_column_map` in Lakehouse
- Notebooks read these metadata tables at runtime to build JDBC SELECT strings
- No recompilation needed to add/change columns — just update the metadata table

---

## 9. THE 7 ACTION KEYS — LOAD BEHAVIOR CLASSIFICATION

ActionKey is the most important classification dimension in the current system. It directly controls checksum behavior and load strategy.

| ActionKey | Group | Tables | Checksum / Load Strategy | Fabric Equivalent |
|-----------|-------|--------|--------------------------|-------------------|
| **1** | Standard SAMMS | Every per-site table (enrollment, clients, UA, codes, clinic, assessments…) | YES — RowChkSum, incremental upsert | Delta MERGE with RowChkSum condition |
| **2** | Global / Shared | FormsSAMMSClient, BAM, LiquidLog from SAMMSGLOBAL DB | YES — RowChkSum, incremental upsert | Delta MERGE with RowChkSum condition |
| **3** | Bulk / Reference | Lookup/reference tables (code lists, dropdown values) | **NO checksum** — full delete + reload | Delta OVERWRITE (no merge needed) |
| **4** | Financial / Billing | Claims, bills, eligibility | YES — RowChkSum, incremental upsert | Delta MERGE with RowChkSum condition |
| **5** | PHC / LAB Claims | PHC and LAB site claims variant | YES — RowChkSum, incremental upsert | Delta MERGE, PHC branch |
| **6** | Separate Group | (scope in DB only) | YES | Delta MERGE |
| **7** | Methasoft Schema | Clinics using `ms.` schema (not `dbo.`) | YES | Delta MERGE, schema-aware JDBC query |

**Critical:** ActionKey=3 tables must be identified before migration and given a separate Bronze-to-Silver strategy (OVERWRITE, not MERGE).

---

## 10. THE 11 PIPELINES — COMPLETE BREAKDOWN

### Pipeline timing and load order (must preserve dependencies)

| Time | Arg | Pipeline Name | Key Tables |
|------|-----|---------------|-----------|
| 5:15 PM | — | Scheduler.exe | Creates task queue only |
| 7:00 PM | 6 | Samms-Forms | FormQuestionAnswers, FormAnswerSignatures |
| 8:23 PM | 2 | Eastern/Central/Mountain/Pacific P1 | Enrollment, ClientDemo1/2, UAResults, Codes, Clinic, Assessments, PreAdmission |
| 8:50 PM | 4 | Eastern/Central/Mountain/Pacific P2 | Claims, Bills, CheckIn, ClaimLineItem, ClaimLineItemActivity, FeeSchedules, PayerClient |
| 9:36 PM | 10 | SAMMS-ETL-Dose | Dose, DoseExcuse |
| 10:01 PM | 11 | SAMMS-ETL-Orders | Orders (year-partitioned) |
| 10:10 PM | 1 | SAMMSGlobal | Users, Consents, GlobalDevices, GlobalPayer, FormsSAMMSClient |
| 11:50 PM | 8 | SAMMS-ETL-Inv | Bottles, LiquidLog, LabResults, All Assessments (Admission/ReAssessment/Dimensions 1-6), Appointments |
| 12:05 AM | 9 | SAMMS-ETL-DartSvc | DartsSrv (year-partitioned, largest table) |
| 2:29 AM | 7 | SAMMS-ETL-Notes | 3pARNote, 3pClaimNote |
| 6:24–7:01 AM | — | AzureAgent.exe | Post-load aggregations: BAM scores, KPIs, inventory merge, counseling state requirements |

### P1 vs P2 split logic (must replicate)
P1 = non-financial clinical data. P2 = financial/billing data.  
Both run under `arg=2` and `arg=4` respectively.  
**The split is timezone-sensitive** — some tables flip between P1 and P2 depending on the site's timezone (EST/CST/MST/PST). This is hardcoded in the Scheduler CASE statement.

| Table | EST | CST | MST | PST |
|-------|-----|-----|-----|-----|
| `tbl_enrollment` | P1 | P2 | P2 | P2 |
| `tbl_checkin` | P2 | P2 | P1 | P1 |
| `tbl_bills` | P2 | P2 | P1 | P1 |
| `tbl_payerclthistory` | P2 | P1 | P2 | P2 |

**Fabric migration:** Move this timezone assignment logic into `meta.dim_site` or `meta.dim_pipeline_table`. Each site has a `timezone` column. Each table has a `p1_timezones` and `p2_timezones` array. This replaces the Scheduler CASE statement entirely.

---

## 11. DATA DOMAIN CATEGORIES — ALL 98 TABLES GROUPED

Grouping by business domain — this is how you should organize your **Fabric Lakehouse folders**, **Silver schemas**, and eventually **Gold semantic models**.

### Domain 1: Patient Identity & Demographics (HIGH SENSITIVITY — PII)
| Table | Pipeline | Method |
|-------|----------|--------|
| `pats.tbl_ClientDemo1` | P1 / LAB | `SaveClientDemo1var` |
| `pats.tbl_ClientDemo2` | P1 | `SaveClientDemo2` |
| `pats.tbl_Enrollment` | P1 / P2 | `SaveEnrollment` |
| `ayx.tbl_PreAdmission_V6` | P1 | `SavePreAdmissionV6` |
| `pats.tbl_PreAdmission_Referrals` | P1 | `SavePreAdminReferrals` |
| `pats.tbl_TreatmentLevel` | P2 | `SaveTreatmentLevel` |
| `pats.tbl_CheckIn` | P2 | `SaveCheckIn` |
| `pats.tbl_FMP` | P1 | `SaveFmp` |

**Fabric note:** PII columns (SSN, DOB, address) must have **column-level security** in Gold. Consider tokenizing SSN in Silver. These tables feed the most consumer reports.

### Domain 2: Counseling Services — HIGHEST VOLUME
| Table | Pipeline | Method |
|-------|----------|--------|
| `pats.tbl_DartsSrv_2014B4` through `_2024` | 9 (DartSvc) | `BulkDartsSrvLoader` / `SaveDartSrv20XX` |

**Fabric note:** Primary candidate for medallion optimization. Silver = one partitioned Delta table replacing 11 physical tables.

### Domain 3: Medication / Dosing
| Table | Pipeline | Method |
|-------|----------|--------|
| `pats.tbl_Dose` | 10 (Dose) | `SaveDoses` |
| `pats.tbl_Dose_Excuse` | 10 (Dose) | `SaveDoseExcuse` |
| `pats.tbl_Bottle` | 8 (Inv) | `SaveBottles` |
| `pats.tbl_LiquidLog` | 8 (Inv) | `SaveLiquidlog` |

**Fabric note:** Dose is near-daily per patient. Very high row count. Reload flag must survive as pipeline parameter.

### Domain 4: Medication Orders (Year-Partitioned)
| Table | Pipeline | Method |
|-------|----------|--------|
| `pats.tbl_Orders` through `pats.tbl_Orders_2028` | 11 (Orders) | `SaveOrders20XX` (13 methods) |

**Fabric note:** Year split happens in C# memory today. In Fabric this collapses to one Silver table partitioned by `year(OrderDate)`.

### Domain 5: Billing & Financial
| Table | Pipeline | Method |
|-------|----------|--------|
| `pats.tbl_Bills` | P2 | `SaveBills` |
| `pats.tbl_AuthBills` | P2 | `SaveAuthBills` |
| `pats.tbl_Claims` | P2 | `SaveClaims` |
| `pats.tbl_ClaimLineItem` | P2 | `SaveClaimLineItem` |
| `pats.tbl_ClaimLineItemActivity` | P2 | `SaveClaimLineItemActivity` |
| `pats.tbl_3pElig` | P2 | `Save3pElig` |
| `pats.tbl_3pArnote` | 7 (Notes) | `Save3pArnote` |
| `pats.tbl_3pClaimNote` | 7 (Notes) | `Save3pClaimNote` |
| `pats.tbl_PayerClient` | P2 | `SavePayerClient` |
| `pats.tbl_PayerCltHistory` | P2 | `SavePayerCltHistory` |
| `pats.tbl_AuthBillSub` | P2 | `SaveAuthBillsub` |

**Fabric note:** Claims uses bulk path (SqlBulkCopy) today for high-volume sites. Delta MERGE handles this. `RemovePayerClients` method marks rows inactive — this is a **soft delete** pattern, not a hard delete. Silver must preserve this.

### Domain 6: Laboratory & Drug Testing
| Table | Pipeline | Method |
|-------|----------|--------|
| `pats.tbl_UAResult` | P1 / LAB | `SaveUAResults` |
| `pats.tbl_UAResultDetail` | P2 | `SaveUAResultDetail` |
| `pats.tbl_UASched` | P1 | `SaveUASched` |
| `pats.tbl_LabResult` | P1 / 8 | `SaveLABResults` |
| `pats.tbl_LabResultDetail` | 8 | bulk path |

### Domain 7: Clinical Assessments (Largest set — 20+ tables)
| Table Group | Pipeline | Notes |
|-------------|----------|-------|
| AdmissionAssessment + all 6 ASAM Dimensions | 8 (Inv) | Header + 6 child tables |
| ReAssessment + all sub-sections (Occ/Family/Legal/Mental/Physical/Substance/Social/Treatment) | 8 | Header + 8 child tables |
| PADimension1–6 + PA + PACounselorReview | 8 | Pre-Admission assessment |
| MN/VA state-specific assessment forms | 8 | State-specific |
| NewAdmissionAssessment + NewPeriodicReassessment | 8 | Newer SAMMS schema |
| BAMForm + BAMScore | 1 / Global | Brief Addiction Monitor |
| COWS (original + V6) | 1 / 8 | Two schema versions |

**Fabric note:** Assessment tables are the most complex domain — 20+ tables with 1:many hierarchies (header → dimension 1 → dimension 2 → … → dimension 6). Gold layer will need to denormalize these for reporting.

### Domain 8: Digital Forms (Schema-Flexible)
| Table | Pipeline | Notes |
|-------|----------|-------|
| `pats.tbl_dbo_FormQuestionAnswers` | 6 (Forms) | Dynamic schema — varies by form type |
| `pats.tbl_dbo_FormAnswerSignatures` | 6 | 6 signature types per form |
| `pats.tbl_FormsSAMMSClient` | Global | Form-to-client linkage |
| `pats.tbl_EandMFormMDM` | P2 | E&M decision making form |
| `pats.tbl_EandMFormPregnancy` | P2 | Pregnancy E&M form |
| `pats.tbl_ComprehensiveAssessmentForm` | P1 | Comprehensive form |

**Fabric note:** FormQuestionAnswers is the most schema-flexible domain. `ctrl.tbl_Forms2Process` defines which forms to process. In Fabric, this config table must be replicated in metadata.

### Domain 9: Global Reference / Configuration
| Table | Pipeline | Notes |
|-------|----------|-------|
| `ctrl.tbl_User` | Global | SAMMS staff per clinic |
| `ctrl.tbl_UserSites` | Global | Staff-to-site assignments |
| `ctrl.tbl_Clinic` | Global | 150+ config flags per site |
| `pats.tbl_Codes` | Global | Service/billing code lookup |
| `pats.tbl_Services` | P1 | Clinical service type master |
| `pats.tbl_GlobalPayor` | Global / P2 | Insurance payer master |
| `pats.tbl_FeeSched` | Global / P2 | Billable rates per payer |
| `ctrl.tbl_InvType` | 8 | Medication/inventory types |
| `pats.tbl_GlobalUser` | Global | SAMMS user master |

**Fabric note:** Reference data — small tables, infrequently changing. In Fabric these are great candidates for Delta OVERWRITE (ActionKey=3 pattern) or scheduled daily refresh.

### Domain 10: Custom Clinic Forms
| Table | Pipeline | Notes |
|-------|----------|-------|
| `pats.tbl_CustomQuestions` | P1 | Clinic-defined question definitions |
| `pats.tbl_CustomAnswers` | P1 | Answers to clinic custom questions |

**Fabric note:** Custom schemas — question definitions vary per clinic. These are part of the "dynamic schema" challenge category.

### Domain 11: Appointments
| Table | Pipeline | Notes |
|-------|----------|-------|
| `pats.tbl_Appointments` | 8 (Inv) | Appointment master |
| `pats.tbl_AppointmentAttend` | 8 / P1 | Appointment outcomes |

### Domain 12: Audit & Control (internal — may NOT need to migrate)
| Table | Pipeline | Notes |
|-------|----------|-------|
| `tsk.tbl_RowTrax` | All | Row count audit per run per site |
| `tsk.tbl_Tasks2` | Scheduler | Task queue — replaced by Fabric Pipelines |
| `tsk.tbl_Schedule` | Scheduler | Schedule config — replaced by Fabric triggers |
| `tsk.tbl_ErrorLog` | AzureAgent | Error log — replaced by Fabric monitoring |

**Fabric note:** `tsk.*` tables are the task queue — they become Fabric Pipeline run history + `meta.pipeline_site_run` audit Delta table. `tsk.tbl_RowTrax` (source vs destination row counts per site per run) is valuable — replicate it as an audit table in the Lakehouse.

---

## 12. POST-LOAD AGGREGATIONS (AzureAgent) — SEPARATE CONCERN

AzureAgent.exe runs after ETL completes (6:24–7:01 AM). It runs stored procedures that compute derived data inside Azure SQL. These are NOT ETL loads — they are post-load transforms.

| Stored Proc | What It Does | Schedule |
|-------------|-------------|----------|
| `pats.Populate_BAM_Bucketed` | Buckets BAM assessment scores for reporting | 6:24 AM |
| `pats.SP_CounselingStateReq` | Computes counseling state requirements / compliance | 7:00 AM |
| `pats.SP_MedInvMerge` | Merges medication inventory across sites | 7:00 AM |
| `pats.BAMMerge` | Computes BAM scores from raw FormQuestionAnswers | Per-site after forms load |
| `pats.BAMMergeGbl` | Global BAM score aggregation | After global forms load |

**Fabric migration:** These become **Gold layer notebooks** or **Fabric Pipelines with Spark notebook activities** that run after all Silver writes complete. They are the only legitimate "Gold" transforms in the current system. In the new world:
- Silver = parity with current Azure SQL final tables
- Gold = what AzureAgent.exe computes today + any new aggregations/denormalizations

---

## 13. KNOWN BUGS & ANOMALIES — DO NOT REPLICATE IN FABRIC

These are documented issues in the current system. **Do not carry them forward.**

| # | Location | Anomaly | Impact |
|---|----------|---------|--------|
| 1 | `SaveCodes.cs` OL1 | `UpdateRange(codes)` writes entire site slice unconditionally — no RowChkSum guard | Unnecessary writes every run even for unchanged codes |
| 2 | `SaveCodes.cs` OL1 | New codes for a site are silently dropped (not inserted) | Critical data loss for new code additions |
| 3 | `SaveEnrollment.cs` | Large per-site date correction switch (25+ sites) commented out | If re-enabled, silently alters source dates |
| 4 | `SavePreAdmissionV6.cs` | Boolean → 'Yes'/'No' string conversion for many fields | Schema-level type mismatch that must be handled in Silver |
| 5 | `SaveClinic.cs` | Comment in code: `"//What's up with this?"` — `ctrl.tbl_clinic` skip optimization never implemented | No real impact but indicates incomplete design |
| 6 | `SaveGlobal.cs` (FeeSched) | `IsActive` set to false then zeroed before re-map — RowChkSum guard non-functional (always 0 during pre-deactivation) | Every fee schedule row always re-written even unchanged |
| 7 | `SaveDartsSrvs.cs` | O(N²) `LINQ.Where` inside foreach — `darts.Where(x => x.DsId == ds).FirstOrDefault()` in loop | Performance bottleneck for large sites on EF path |
| 8 | `pats.MergeServicesMissingSigCode` | Commented out in AzureAgent — not running | Services records with missing sig codes never fixed |
| 9 | `BulkDartsSvc.cs` | Giant commented-out EF path block (lines 22–238) | Dead code — can be removed; no impact |
| 10 | `SaveClinic.cs` line 104 | Commented-out `Lasttbl skip` (force new DbContext per site) | Potential EF context caching issue not addressed |

---

## 14. THE SAMMS SOURCE SCHEMA VARIATIONS — GATEWAY / NETWORK CHALLENGE

The source is not one database — it is **80–113 separate databases**, each potentially on a different SAMMS version.

### Source schema variants documented

| Schema Type | Description | Sites |
|-------------|-------------|-------|
| Standard `dbo.` | Most BHG clinics — SAMMS on-premise or Netalytics cloud | ~90% |
| `ms.` Methasoft | Clinics using Methasoft schema format (ActionKey=7) | Small subset |
| SAMMSGLOBAL | A shared global database (not per-clinic) — BAM, LiquidLog, FormsSAMMSClient | 1 central DB |
| AdvancedMD | Referenced in architecture docs as a source variant | Subset |
| PHC | PHC clinics — completely separate runner, separate Save* files | All PHC sites |
| LAB | Lab site — special column exclusions, separate demographics pipeline (arg=5) | 1 site |

### Connection string management
Today: `ctrl.tbl_LocationCons` in Azure SQL stores one connection string per site.  
**Fabric migration:** Connection strings (containing credentials) must NOT be in Delta tables.  
Use **Azure Key Vault** — one secret per site. `meta.dim_site` stores the Key Vault secret name, not the connection string. Notebooks resolve the secret at runtime via Key Vault SDK.

### On-premise connectivity
All SAMMS databases are on-premise (VPN / Netalytics). Today: direct ADO.NET from the ETL server inside the network.  
**Fabric:** Requires **On-premises Data Gateway** (or Fabric Gateway) for JDBC access from Spark. This is the single biggest infrastructure challenge. Plan capacity: 80+ concurrent JDBC connections at concurrency cap (recommend 8–20 parallel).

---

## 15. THE CHECKSUM COLUMN — IMPLEMENTATION DETAIL

### How it's computed today (in SAMMS via SQL SELECT)
```sql
CHECKSUM([col1], [col2], [col3], ...) AS RowChkSum
```
SQL Server `CHECKSUM()` is computed at the source database during the SELECT. The `SelectConstructor` builds the expression from `dms.tbl_MapSrc2Dsn` (columns flagged for checksum inclusion).

### Problem with Fabric
PySpark does not have a native `CHECKSUM()` function. Options:
1. **Keep computing at source:** Keep the `CHECKSUM(...)` expression in the JDBC SELECT query — SQL Server computes it at the SAMMS side. This is the simplest path. The value arrives in the DataFrame pre-computed. ✅ Recommended.
2. **Recompute in Spark:** Use `hash()` or `sha2(concat_ws(...))` over the same columns. Produces a different value than SQL Server CHECKSUM. Requires a one-time re-baseline of all `RowChkSum` values in the existing destination tables.
3. **Compare individual columns instead:** The Delta MERGE condition becomes column-by-column equality instead of a single checksum. Simpler semantically but more complex SQL.

**Recommendation:** Keep computing `CHECKSUM(...)` in the JDBC SELECT (Option 1). The existing Silver checksum values remain valid. No re-baseline needed.

---

## 16. STORED PROCEDURES — WHAT REPLACES THEM IN FABRIC

All `stg.*` stored procedures are staging-to-final MERGE operations. In Fabric these disappear because Delta MERGE is native.

| Current SP | What Replaces It in Fabric |
|------------|---------------------------|
| `stg.DartsSrvMerge` … `stg.DartsSrvMerge28` | One Delta `MERGE` on `(SiteCode, DsId)` with `year(DsDtStart)` partition filter |
| `stg.DoseMerge` | Delta `MERGE` on `(SiteCode, DoseId)` |
| `stg.ClaimsMerge` | Delta `MERGE` on `(SiteCode, ClaimId)` |
| `stg.ClientDemoMerge1` + `ClientDemoMerge2` | Two Delta MERGEs (ClientDemo1, ClientDemo2) |
| `stg.sp_liquidlog_Merge` | Delta `MERGE` with `@sitecode` parameter |
| `stg.FormsSAMMSMerge` / `FormsSAMMSMergePHC` | Delta MERGE, PHC-aware parameter |
| `stg.sp_FormQA_Merge` | Delta MERGE |
| All other `stg.*` | Delta MERGE per table |

`pats.*` post-load SPs (`BAMMerge`, `SP_CounselingStateReq`, `SP_MedInvMerge`) become **Gold notebook activities** after all Silver writes succeed.

---

## 17. MIGRATION RISK REGISTER

| Risk | Severity | Notes |
|------|----------|-------|
| On-prem JDBC gateway capacity for 80+ sites in parallel | HIGH | Gateway throughput, VPN bandwidth, and SQL Server connection limits all need sizing |
| RowChkSum value parity during transition | HIGH | If recomputed differently in Spark, every row appears "changed" on first run |
| Year-partitioned table consumers | HIGH | Existing Power BI / SQL queries reference `pats.tbl_DartsSrv_2022` by name; need views or migration |
| PHC pipeline separation | MEDIUM | PHC uses different files; needs its own Fabric pipeline branch from day 1 |
| DartsSrv multi-column date OR lookback | MEDIUM | Five date columns OR'd in WHERE — must replicate exactly or over-pull |
| ActionKey=3 (no checksum) tables inadvertently given MERGE | MEDIUM | Reference tables need OVERWRITE strategy, not MERGE |
| Schema version guard per site (ServiceType, V5 vs V6) | MEDIUM | Must build site feature flag table before first notebook runs |
| Timezone-based P1/P2 split | MEDIUM | Hardcoded in Scheduler CASE; must be data-driven in Fabric |
| AzureAgent post-load SPs timing | MEDIUM | BAMMerge, SP_CounselingStateReq must run AFTER all Silver writes, not during |
| EF Core anomalies carried forward | LOW | Don't replicate bugs (SaveCodes silent drop, FeeSched always-rewrite) |
| Commented-out override dates (hardcoded C# dates) | LOW | Move to config table; `1/24/2025` style fixes must survive as data |
| `SaveDartsSrvs-old.cs` still in repo | LOW | Dead code; confirms old approach — delete from reference, not from migration scope |

---

## 18. RECOMMENDED FABRIC MEDALLION LAYER DESIGN

Based on all patterns above:

### Bronze Layer — Raw Landing
- One Delta table per source domain (not per site)
- Columns: all source columns + `_site_code`, `_run_id`, `_extracted_at`, `_source_table`, `_load_mode`
- Append-only or overwrite-per-run (configurable per table)
- No business transformation — raw values as extracted from SAMMS
- Partition by `_site_code` and `_extracted_date`

### Silver Layer — Conformed, Merged, Audited
- One Delta table per business domain (DartsSrv, Orders, Enrollment, etc.)
- Year-partitioned tables collapse: DartsSrv → one Silver table partitioned by `year(ServiceDate)`
- Delta MERGE on `(SiteCode, PrimaryKey)` with `RowChkSum` condition
- Add audit columns: `_silver_updated_at`, `_run_id`, `_rows_merged`, `_rows_inserted`
- `RowChkSum` retained as a column
- PHC rows co-exist with BHG rows (differentiated by `SiteCode`)
- Append `meta.pipeline_site_run` after each successful site write

### Gold Layer — Reporting Ready
- Today: AzureAgent computed columns (BAM scores, counseling state requirements, KPI tables)
- Future: Denormalized assessment hierarchies, cross-domain joined tables
- No direct JDBC source access — reads from Silver only
- If Azure SQL consumers need backward compatibility: use **Gold views** that mirror old table names (`pats.tbl_DartsSrv_2022` → `gold.vw_DartsSrv` filtered to `year = 2022`)

### Metadata Layer
- `meta.dim_site` — all 80–113 sites, timezone, runner_type, feature flags
- `meta.dim_column_map` — mirrors `dms.tbl_MapSrc2Dsn`
- `meta.dim_pipeline_table` — mirrors `dms.tbl_MapAction`
- `meta.tbl_lookback_config` — replaces hardcoded DaysBack values
- `meta.pipeline_site_run` — per-site run audit (replaces `tsk.tbl_RowTrax`)
- `meta.pipeline_run` — per-run summary

---

## 19. SUMMARY TABLE — EVERY KEY PATTERN AT A GLANCE

| # | Pattern / Category | Current Implementation | Fabric Replacement |
|---|-------------------|----------------------|-------------------|
| 1 | EF Core row-by-row upsert | `foreach` + `LINQ.Where` + `SaveChanges()` | Delta `MERGE` with RowChkSum condition |
| 2 | SqlBulkCopy + staging MERGE | `SqlBulkCopy` → `stg.tbl_xxx` → `stg.XxxMerge` SP | JDBC DataFrame → Bronze Delta → Silver Delta MERGE |
| 3 | Year-partitioned tables | 11 physical DartsSrv tables, 13 Orders tables | One Silver Delta table, partition by year column |
| 4 | RowChkSum change detection | `CHECKSUM(...)` in SELECT, compare at C# layer | Keep `CHECKSUM(...)` in JDBC SELECT; use in MERGE condition |
| 5 | Full reload (ActionKey=3) | TRUNCATE + INSERT, no checksum | Delta OVERWRITE mode |
| 6 | Reload flag | `reload=true` → DELETE + reinsert | Pipeline parameter → OVERWRITE or targeted DELETE |
| 7 | Dynamic lookback window | `-15` / `-90` / `-200` days, hardcoded in C# | `meta.tbl_lookback_config` table, read at runtime |
| 8 | Schema version guards | `sys.all_columns` check per site before SELECT | `meta.dim_site_features` table, notebook reads at runtime |
| 9 | Metadata-driven column mapping | `dms.tbl_MapSrc2Dsn` → `SelectConstructor` | `meta.dim_column_map` → JDBC SELECT builder in PySpark |
| 10 | PHC vs BHG split | Separate `PHC.exe`, separate Save* files in `/PHC` | Pipeline parameter `runner_type = PHC | BHG`, same notebooks |
| 11 | 7 ActionKeys | Controls checksum/reload behavior | `meta.dim_pipeline_table.action_key` → branch in notebook |
| 12 | Timezone P1/P2 split | Hardcoded in Scheduler CASE | `meta.dim_site.timezone` + `meta.dim_pipeline_table.timezones` |
| 13 | 80+ site fan-out | Sequential loop in BHGTaskRunner | ForEach parallel activity in Fabric Pipeline, concurrency cap |
| 14 | Post-load aggregations | AzureAgent.exe stored procs | Gold notebooks chained after Silver writes |
| 15 | Connection string management | `ctrl.tbl_LocationCons` in Azure SQL | Azure Key Vault secrets, `meta.dim_site.kv_secret_name` |
| 16 | Row count auditing | `tsk.tbl_RowTrax` | `meta.pipeline_site_run` Delta audit table |
| 17 | Dead code / old versions | `SaveDartsSrvs-old.cs`, `SaveGlobal-old.cs`, `SaveGlobalorg.cs` | Do not migrate; confirm current versions before implementing |
| 18 | Soft delete (PayerClient) | `RemovePayerClients()` marks rows inactive | Delta MERGE with `whenMatchedUpdate(IsActive=false)` |
| 19 | Inline SQL transforms | Inline UPDATE/DELETE strings in C# | Move to Silver notebook logic or Gold notebook |
| 20 | Multi-path routing (131 switch cases) | `BHGTaskRunner/Program.cs` 3,375 lines | Metadata-driven routing table — no switch statement |

---

*Document generated from deep analysis of all BHG-DR-LIB, BHGTaskRunner, PHC, Scheduler, and  
AzureAgent source code and all Save*-Documentation markdown files in the BCAppCode workspace.*
