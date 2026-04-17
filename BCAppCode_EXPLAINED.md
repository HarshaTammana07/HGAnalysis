# BHG Data Repository — Complete System Documentation

> **What is this?**  
> This codebase is a **multi-site ETL (Extract, Transform, Load) data integration platform** built in C# / .NET for **BHG Recovery** (Behavioral Health Group), an organization operating **opioid treatment / substance-use-disorder clinics** across the United States. Its job is to collect clinical and operational data from every individual clinic's local database and consolidate it into a single central Azure SQL database for reporting, analytics, and KPI tracking.

---

## Table of Contents

1. [Big Picture — Why Does This Exist?](#1-big-picture)
2. [High-Level Architecture](#2-high-level-architecture)
3. [Project-by-Project Breakdown](#3-project-by-project-breakdown)
4. [The Database — BHG_DR](#4-the-database--bhg_dr)
5. [The ETL Flow — Step by Step](#5-the-etl-flow--step-by-step)
6. [Task Lifecycle & Status Codes](#6-task-lifecycle--status-codes)
7. [ETL Batch Groups Explained](#7-etl-batch-groups-explained)
8. [The Two Write Strategies](#8-the-two-write-strategies)
9. [How Projects Connect to Each Other](#9-how-projects-connect-to-each-other)
10. [Key Data Entities (What Data is Moved)](#10-key-data-entities)
11. [AzureAgent — The Scheduled SQL Job](#11-azureagent--the-scheduled-sql-job)
12. [ETLMgr — The Monitoring Desktop App](#12-etlmgr--the-monitoring-desktop-app)
13. [Critical Issues to Know](#13-critical-issues-to-know)
14. [Glossary](#14-glossary)

---

## 1. Big Picture

### The Problem This Solves

BHG operates **many OTP (Opioid Treatment Program) clinics** spread across multiple US time zones. Each clinic runs a local **SAMMS** (Substance Abuse Management & Monitoring System) SQL Server database. These are separate, isolated databases — one per site (or per small group of sites).

Leadership, analysts, and compliance teams need answers to questions like:
- How many patients received doses across ALL clinics today?
- Which counselors are behind on treatment plans?
- What are the billing/claims numbers across the whole network?

None of these questions can be answered from a single site's database. You need **all the data in one place**.

### The Solution

This codebase pulls data from every site's local SAMMS database and loads it into a **central Azure SQL warehouse** called `BHG_DR`. From there, reporting tools (like Alteryx, referenced in schema prefix `ayx`) can query everything in one place.

```
Multiple SAMMS Site DBs  ──ETL──►  BHG_DR (Azure SQL)  ──►  Reports / Analytics
(1 per clinic)                      (Central warehouse)
```

---

## 2. High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        OPERATIONS ENVIRONMENT                        │
│                                                                      │
│  ┌───────────────┐     ┌───────────────┐     ┌──────────────────┐   │
│  │   Scheduler   │     │ BHGTaskRunner │     │   AzureAgent     │   │
│  │  (Console EXE)│     │ (Console EXE) │     │  (Console EXE)   │   │
│  │               │     │               │     │                  │   │
│  │  Runs once    │     │  Runs as a    │     │  Runs on a timer │   │
│  │  per day to   │     │  worker loop  │     │  doing SQL batch │   │
│  │  seed tasks   │     │  pulling &    │     │  refresh jobs    │   │
│  │  into SQL     │     │  saving data  │     │  (2 AM, 6 AM...) │   │
│  └──────┬────────┘     └──────┬────────┘     └────────┬─────────┘   │
│         │                     │                        │             │
└─────────┼─────────────────────┼────────────────────────┼─────────────┘
          │                     │                        │
          ▼                     ▼                        ▼
┌─────────────────────────────────────────────────────────────────────┐
│                   Azure SQL — BHG_DR  (Central DB)                  │
│                                                                      │
│  tsk.*  (Tasks/Schedule)    pats.*  (Patient/Clinical data)          │
│  dms.*  (Mapping config)    stg.*   (Staging tables)                 │
│  ayx.*  (Analytics views)   pba.*   (KPI / Performance)              │
│  ctrl.* (Clinic control)                                             │
└─────────────────────────────────────────────────────────────────────┘
          ▲
          │  (per-site ETL pulls)
          │
┌─────────┴───────────────────────────────────────────────────────────┐
│          Source: SAMMS Site Databases (one per clinic)               │
│   Site A (EST) │ Site B (CST) │ Site C (MST) │ Site D (PST) │ ...   │
└─────────────────────────────────────────────────────────────────────┘
```

**Monitoring:**

```
┌──────────────────┐
│     ETLMgr       │  ←── WinForms desktop app for ops team
│  (Desktop App)   │      to watch task status in real-time
└──────────────────┘
         │
         ▼
    BHG_DR  tsk.tbl_Tasks2
```

---

## 3. Project-by-Project Breakdown

### 3.1 `BHG-DR-LIB` — The Shared Library (Heart of the System)

**What it is:** A `.NET Core 3.1` class library referenced by ALL other projects. It contains all reusable logic.

**Key files inside:**

| File | What it does |
|------|-------------|
| `Models/BHG_DRContext.cs` | The EF Core `DbContext` — declares all tables/views as C# classes. This is the "map" of the entire database. |
| `Models/*.cs` (~151 files) | One C# class per database table or view entity |
| `SQLSvrManager.cs` | Raw ADO.NET helper: connects to SQL Server, runs queries, fills DataTables |
| `SelectConstructor.cs` | Builds dynamic `SELECT` column lists from mapping metadata — decides which columns to pull from each source site |
| `BulkDartsSvc.cs` | High-speed bulk loading using `SqlBulkCopy` into staging tables, then runs stored procedure merges |
| `Save*.cs` (many files) | Partial class `SaveData` — one file per destination table, does EF Core row-by-row upserts |

**Think of this library as:** The engine. All other executables are just "drivers" that call into this engine.

---

### 3.2 `BHGTaskRunner` — The Main ETL Worker

**What it is:** A `.NET Core 3.1` console application. This is the main workhorse that actually moves data.

**What it does:**
1. Reads the task queue from `tsk.VwTaskList` in the central DB
2. Finds tasks with `Status = 17` (Pending) and `RunAt < NOW`
3. For each parent task → loops through all child tasks
4. For each child task → pulls data from the site's source DB → saves to central DB
5. Updates task status as it goes (17 → 18 → 19 or 20)

**Command-line args (which batch to run):**

| Arg | Batch Name | What it processes |
|-----|-----------|-------------------|
| `1` | SAMMSGlobal | Global/shared tables (all sites, basic data) |
| `2` | Eastern/Central/Mountain/Pacific ETL **P1** | Phase 1 — non-financial tables by timezone |
| `3` | Special bundles | Forms, Notes, Inventory, etc. |
| `4` | Eastern/Central/Mountain/Pacific ETL **P2** | Phase 2 — financial/billing tables by timezone |
| `5` | Samms-LAB | LAB site demographic tables |
| `6` | Samms-Forms | Form answers & signatures |
| `7` | SAMMS-ETL-Notes | Clinical notes |
| `8` | SAMMS-ETL-INV | Inventory, assessments, lab results |
| `9` | SAMMS-ETL-DartSvc | DART services records |
| `10` | SAMMS-ETL-Dose | Dosing records |
| `11` | SAMMS-ETL-Orders | Orders (partitioned by year) |

---

### 3.3 `Scheduler` — The Daily Task Seeder

**What it is:** A `.NET Core 3.1` console application that runs once per day (likely via Windows Task Scheduler or Azure).

**What it does (in order):**

1. **Resets stuck tasks** — any task still showing `Status = 18` (Processing) gets reset to `17` (Pending), meaning it will be retried
2. **Archives old tasks** — tasks from more than 3 months ago get deleted
3. **Creates parent tasks** — reads `tsk.tbl_Schedule` for all enabled schedules and inserts a parent task row for today
4. **Creates child tasks** — reads `dms.vw_MapAction` (the mapping config) and creates one child task per table per site
5. **Assigns each child to a batch group** — using timezone + table name logic to assign the right ETL batch name (Eastern P1, Dose, Orders, etc.)
6. **Marks disabled combos** — certain site+table combinations that aren't supported get `RowState = 26` (Skipped)
7. **Bumps the schedule** — adds 1 day to `NextRunTime` in `tbl_Schedule`

**Key concept:** The Scheduler doesn't move data. It just prepares the to-do list for BHGTaskRunner.

---

### 3.4 `ETLMgr` — Operations Monitoring Desktop App

**What it is:** A `.NET 8.0` Windows Forms desktop application.

**What it does:** Shows a live grid of ETL tasks from `tsk.tbl_Tasks2` for any selected work date. Operators can see:
- Task name & site
- Status (Pending / Processing / Completed / Error)
- Duration
- Row count processed
- How many child tasks remain / failed

**Who uses it:** The operations/data team to monitor daily ETL runs.

---

### 3.5 `AzureAgent` — Timed SQL Batch Runner

**What it is:** A `.NET Core 3.1` console application that runs continuously and triggers SQL commands at specific times of day.

**Schedule:**

| Time | What it does |
|------|-------------|
| **2:24 AM** | Truncates and rebuilds `ayx.tbl_Transactions` from `ayx.vw_Transactions` |
| **6:24 AM** | Refreshes billing-related derived tables (Zero Dollar Denials, etc.) |
| **6:45 AM** | Refreshes counselor KPI tables: `pba.tbl_vw_CounselorSupervision_KPISite` and `pba.tbl_vw_CounselorSupervision_KPICounselor` |

All execution steps are logged to `tsk.tbl_ErrorLog`.

---

### 3.6 `BHG\AzureAgent`, `BHG\bhg.TestCode`, `BHG\BHG` — The Solution Folder

**`BHG.sln`** — The Visual Studio solution file that ties together:
- BHG-DR-LIB
- BHGTaskRunner
- Scheduler
- AzureAgent
- bhg.TestCode

**`bhg.TestCode`** — Developer scratch pad / integration test. Constructs fake task list entries and runs `SelectConstructor` + save operations against a dev SQL host.

**`BHG\*.sql`** — Ad hoc SQL scripts used during development: adding new tables, testing form structures, checking data quality, creating task definitions.

**`BHG\BHG` (.NET 4.7.2)** — Legacy/stub project. No meaningful code. Appears to be an old project that was never removed.

---

### 3.7 `PHC` — Diverged Code Fork (Handle with Care)

**What it is:** A copy/snapshot of the main BHG-DR-LIB + BHGTaskRunner code, but with **no `.csproj` file** — it cannot be compiled as-is.

**Why it's confusing:** The folder is named "PHC" (a specific clinic site code), but the code inside filters OUT PHC sites (`SiteCode != "PHC"`). The PHC-specific ETL actually runs separately.

**Risk:** This is diverged code. If BHG-DR-LIB gets updated, PHC won't automatically get those changes. This is a maintenance hazard.

---

## 4. The Database — BHG_DR

**Server:** `bhgazuresql01.database.windows.net`  
**Database:** `BHG_DR`

### Schema Map

| Schema | Purpose | Example tables |
|--------|---------|---------------|
| `tsk` | Task orchestration | `tbl_Tasks2`, `tbl_Schedule`, `vwTaskList`, `tbl_ErrorLog` |
| `dms` | Data mapping config | `vw_MapAction`, `vw_MapSrc2Dsn` |
| `pats` | Patient clinical data | `tbl_Dose`, `tbl_ClientDemo1/2`, `tbl_Orders`, `tbl_claims`, `tbl_DartsSrv`, `tbl_labresult`, `tbl_Appointments`, etc. |
| `stg` | Staging (temp) tables | Bulk-loaded before merge procs run |
| `ayx` | Alteryx analytics | `tbl_Transactions`, `tbl_PreAdmission_V6`, `vw_Transactions` |
| `pba` | Performance / KPI | `tbl_vw_CounselorSupervision_KPISite/KPICounselor` |
| `ctrl` | Clinic control data | `Tbl_InvType`, clinic configuration |

### Task Tables (the orchestration engine)

```
tsk.tbl_Schedule
  └── defines WHAT runs (name, schedule time, timezone)
      Scheduler reads this to create tasks each day

tsk.tbl_Tasks2
  ├── Parent Task Row (ParentTaskId = NULL)
  │     e.g. "Eastern ETL P1" — the batch container
  └── Child Task Rows (ParentTaskId = <parentId>)
        e.g. "pats.tbl_Dose" for site "NYC01"
             "pats.tbl_Dose" for site "NYC02"
             ...etc

tsk.vwTaskList (view)
  └── joins tbl_Tasks2 with mapping metadata for BHGTaskRunner to read

dms.vw_MapAction
  └── defines WHICH tables to pull from WHICH sites
      Used by Scheduler to generate child tasks

dms.vw_MapSrc2Dsn (WorkToDo in code)
  └── defines column-level mapping from source to destination
      Used by SelectConstructor to build SELECT queries
```

---

## 5. The ETL Flow — Step by Step

Here is the complete journey of data from a clinic to the central database:

```
DAY START
    │
    ▼
[1] SCHEDULER runs (once per day)
    │
    ├── Resets any stuck "running" tasks back to "pending"
    ├── Reads tsk.tbl_Schedule → creates Parent Task rows in tbl_Tasks2
    ├── Reads dms.vw_MapAction → creates Child Task rows (1 per site per table)
    ├── Assigns each child to a batch (Eastern P1, Dose, Orders, etc.)
    ├── Marks unsupported site+table combos as Skipped (RowState=26)
    └── Bumps schedule NextRunTime by 1 day
    │
    ▼
[2] BHGTaskRunner runs (with arg e.g. "2" for Eastern P1)
    │
    ├── Queries VwTaskList WHERE Status=17 AND RunAt < NOW AND TaskName="Eastern ETL P1"
    │
    ├── FOR EACH Parent Task:
    │     Mark parent Status = 18 (Running)
    │     │
    │     └── FOR EACH Child Task under this parent:
    │           Mark child Status = 18 (Running)
    │           │
    │           ├── Load mapping from dms.WorkToDo (VwMapSrc2Dsn)
    │           │     → What columns exist? Any checksums? Site-specific overrides?
    │           │
    │           ├── SelectConstructor.GetSLT()
    │           │     → Build the SELECT query string dynamically
    │           │
    │           ├── SQLSvrManager.GetTableData(query, site.ConStr)
    │           │     → Execute SELECT against the SITE's local SAMMS DB
    │           │     → Returns DataTable with rows
    │           │
    │           ├── SWITCH on TaskName:
    │           │     "pats.tbl_Dose"        → BulkDartsSvc.BulkLoader + stg.DoseMerge
    │           │     "pats.tbl_claims"      → BulkDartsSvc + stg.ClaimsMerge
    │           │     "pats.tbl_DartsSrv"    → BulkDartsSvc + stg.DartsSrvMerge
    │           │     "pats.tbl_ClientDemo1" → SaveData.SaveClientDemo1() (EF upsert)
    │           │     "pats.tbl_Orders"      → BulkDartsSvc + year-partitioned merge
    │           │     ... (many more cases)
    │           │
    │           └── Update child Task: Status=19 (Done) or Status=20 (Error)
    │                                  Duration, RowCount, ErrorMessage
    │
    └── Update Parent Task: Status=19 (Done) or Status=20 (Error)
    │
    ▼
[3] AzureAgent runs (on wall-clock triggers)
    │
    ├── 2:24 AM → Rebuild ayx.tbl_Transactions from view
    ├── 6:24 AM → Refresh billing derived tables
    └── 6:45 AM → Refresh counselor KPI snapshot tables
    │
    ▼
[4] Reports / Analytics tools query BHG_DR
    └── Alteryx, Power BI, SSRS, etc. read final data
```

---

## 6. Task Lifecycle & Status Codes

```
         ┌─────────────────────────────────────────────────────┐
         │                    tsk.tbl_Tasks2                    │
         └─────────────────────────────────────────────────────┘

Status codes:
  17 = PENDING   ← Scheduler creates tasks here; TaskRunner picks these up
  18 = RUNNING   ← TaskRunner sets this when it starts working on a task
  19 = COMPLETE  ← TaskRunner sets this when done successfully
  20 = ERROR     ← TaskRunner sets this when something went wrong

RowState codes:
  24 = ACTIVE    ← Normal, should be processed
  26 = SKIPPED   ← Scheduler marked this as not applicable (disabled combo)

Task hierarchy:
  Parent Task (ParentTaskId IS NULL)
    └── Child Task 1 (ParentTaskId = parent.TaskId)
    └── Child Task 2
    └── Child Task 3
    └── ...

Example:
  Parent: TaskName="Eastern ETL P1", SiteCode="All", Status=17
    Child: TaskName="pats.tbl_ClientDemo1", SiteCode="NYC01", Status=17
    Child: TaskName="pats.tbl_ClientDemo1", SiteCode="NYC02", Status=17
    Child: TaskName="pats.tbl_Dose", SiteCode="NYC01", Status=17
    ...
```

---

## 7. ETL Batch Groups Explained

The Scheduler assigns every child task to a named **batch group**. BHGTaskRunner is invoked with the batch number as a command-line argument. This allows multiple parallel runners to work on different batches simultaneously.

| Arg | Batch Name | Tables included | Why separate? |
|-----|-----------|-----------------|---------------|
| 1 | **SAMMSGlobal** | Shared/global tables | Not site-specific |
| 2 | **[TZ] ETL P1** (Eastern/Central/Mountain/Pacific) | Non-financial patient data | P1 = lighter data, runs first |
| 3 | **Special bundles** | Forms, Notes, Inventory, Assessments | Grouped by data type |
| 4 | **[TZ] ETL P2** | Financial: claims, billing, payer, check-in | P2 = financial data runs after P1 |
| 5 | **Samms-LAB** | LAB site demographics only | LAB has different schema |
| 6 | **Samms-Forms** | FormAnswerSignatures, FormQuestionAnswers | High volume, separate batch |
| 7 | **SAMMS-ETL-Notes** | 3rd-party billing notes | Separate due to volume |
| 8 | **SAMMS-ETL-INV** | Bottles, lab results, assessments, appointments | Inventory + clinical assessments |
| 9 | **SAMMS-ETL-DartSvc** | DART services | Large table, dedicated batch |
| 10 | **SAMMS-ETL-Dose** | Dosing records (tbl_Dose + tbl_Dose_Excuse) | Very high volume |
| 11 | **SAMMS-ETL-Orders** | Orders (year-partitioned) | Largest table, dedicated batch |

**P1 vs P2 pattern:** Within a timezone batch, **P1** handles patient/clinical data first. **P2** handles financial/billing data which depends on P1 completing. This ordering matters for data integrity.

---

## 8. The Two Write Strategies

When data arrives from a source site, it needs to be saved to the central DB. There are two different methods:

### Strategy A: EF Core Row-by-Row Upsert (for lower-volume tables)

```
Source DataTable rows
    │
    ▼
SaveData.SaveXxx() method (e.g. SaveClientDemo1.cs)
    │
    ├── For each row in DataTable:
    │     ├── Check if record already exists (by primary key)
    │     ├── If YES + RowChkSum changed → UPDATE the entity
    │     ├── If YES + RowChkSum same → SKIP (no change)
    │     └── If NO → INSERT new entity
    │
    └── db.SaveChanges()  ← EF Core commits all changes

Used for: Demographics, configuration tables, assessments, smaller patient tables
Advantage: Change detection via RowChkSum avoids unnecessary writes
```

### Strategy B: SqlBulkCopy + Staging Merge (for high-volume tables)

```
Source DataTable rows
    │
    ▼
BulkDartsSvc.BulkDartsSrvLoader()
    │
    ├── SqlBulkCopy → dumps ALL rows into stg.* staging table
    │     (very fast — bypasses EF, direct bulk insert)
    │
    └── EXEC stg.XxxMerge  ← stored procedure in SQL Server
          Stored proc handles:
          ├── INSERT rows that don't exist in target
          ├── UPDATE rows where data changed
          └── (optionally) DELETE rows no longer in source

Used for: Dose, Claims, DartsSrv, Orders, ClaimLineItems
Advantage: Much faster for thousands of rows at a time
```

---

## 9. How Projects Connect to Each Other

```
                     ┌─────────────────┐
                     │   BHG-DR-LIB    │  ← Shared library
                     │                 │
                     │  • BHG_DRContext│
                     │  • SQLSvrManager│
                     │  • SelectConstr │
                     │  • BulkDartsSvc │
                     │  • SaveData.*   │
                     └────────┬────────┘
                              │  (Project Reference — all others use this)
              ┌───────────────┼────────────────┬────────────────┐
              │               │                │                │
              ▼               ▼                ▼                ▼
    ┌──────────────┐  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐
    │  Scheduler   │  │BHGTaskRunner │  │  AzureAgent  │  │bhg.TestCode  │
    │              │  │              │  │              │  │              │
    │Uses only:    │  │Uses all of:  │  │Uses only:    │  │Uses:         │
    │SQLSvrManager │  │BHG_DRContext │  │SQLSvrManager │  │SelectConstr  │
    │              │  │SQLSvrManager │  │              │  │SaveData      │
    │              │  │SelectConstr  │  │              │  │SQLSvrManager │
    │              │  │BulkDartsSvc  │  │              │  │              │
    │              │  │SaveData      │  │              │  │              │
    └──────────────┘  └──────────────┘  └──────────────┘  └──────────────┘

    ETLMgr (WinForms) ── does NOT use BHG-DR-LIB ──► connects directly via SqlConnection
```

**BHG.sln** is the Visual Studio solution that holds all these projects together:
- `BHG-DR-LIB` (library)
- `BHGTaskRunner` (console exe)
- `Scheduler` (console exe)
- `AzureAgent` (console exe)
- `bhg.TestCode` (console exe)

`ETLMgr` has its **own separate solution** (`ETLMgr.sln`) because it targets `.NET 8.0-windows` (WinForms) vs the others targeting `.NET Core 3.1`.

---

## 10. Key Data Entities

These are the main types of clinical/operational data being moved:

### Patient & Clinical
| Table | What it stores |
|-------|---------------|
| `pats.tbl_ClientDemo1/2` | Patient demographic information |
| `pats.tbl_Dose` | Medication dosing records (methadone/buprenorphine) |
| `pats.tbl_Dose_Excuse` | Missed dose excuses |
| `pats.tbl_Appointments` | Patient appointment scheduling |
| `pats.tbl_labresult` / `tbl_labresultdetail` | Drug screening / UA (urinalysis) results |
| `pats.tbl_CheckIn` | Patient check-in records |
| `pats.tbl_uaresultdetail` | Urinalysis result details |
| `pats.tbl_orientationchecklistnew` | Orientation checklist completion |

### Assessments & Forms
| Table | What it stores |
|-------|---------------|
| `pats.Tbl_AdmissionAssessment` | New patient intake assessments |
| `pats.Tbl_ReAssessment` | Ongoing re-assessments |
| `pats.tbl_BriefAddictionMonitor` / `BAMForm` / `BAMScore` | BAM (Brief Addiction Monitor) clinical tool |
| `pats.tbl_clinicalopiatewithdrawalscale` / `tbl_Cows_V6` | COWS withdrawal assessment |
| `pats.tbl_dbo_FormQuestionAnswers` | All clinical form question/answer data |
| `pats.tbl_dbo_FormAnswerSignatures` | Form signatures |
| `pats.tbl_PA*` | Pre-admission dimension tables |
| `ayx.tbl_PreAdmission_V6` | Pre-admission in the newer V6 schema format |

### Billing & Financial
| Table | What it stores |
|-------|---------------|
| `pats.tbl_claims` | Insurance claims |
| `pats.tbl_claimlineitem` | Individual claim line items |
| `pats.tbl_claimlineitemactivity` | Claim activity history |
| `pats.tbl_Bills` | Billing records |
| `pats.tbl_DartsSrv` | DART (Drug Abuse Reporting Tool) services billed |
| `pats.tbl_feesched` | Fee schedules by payer |
| `pats.tbl_GlobalPayor` | Payer/insurance master |
| `pats.tbl_PayerClient` | Patient-to-payer assignments |
| `pats.tbl_payerclthistory` | History of payer changes |

### Orders (Year-Partitioned)
Due to volume, orders are split into yearly tables:
`pats.tbl_Orders`, `pats.tbl_Orders2020`, `pats.tbl_Orders2021`, ..., `pats.tbl_Orders2028`

### Inventory & Clinic Control
| Table | What it stores |
|-------|---------------|
| `pats.tbl_Bottle` | Medication bottle tracking |
| `pats.tbl_LiquidLog` | Liquid medication logs |
| `ctrl.Tbl_InvType` | Inventory type definitions |

### Analytics & KPI (BHG_DR Internal)
| Table | What it stores |
|-------|---------------|
| `ayx.tbl_Transactions` | Denormalized transaction summary (rebuilt nightly at 2 AM) |
| `pba.tbl_vw_CounselorSupervision_KPISite` | Per-site counselor supervision KPIs |
| `pba.tbl_vw_CounselorSupervision_KPICounselor` | Per-counselor KPIs |

---

## 11. AzureAgent — The Scheduled SQL Job

The AzureAgent is a **workaround** for Azure SQL's limitation that you can't schedule T-SQL jobs natively without SQL Server Agent (which isn't available on Azure SQL Basic/Standard tiers). So this console app:
- Runs 24/7 in the background
- Checks `DateTime.Now` on every execution
- Fires SQL commands only within specific minute windows

**Execution schedule:**

```
2:24–2:26 AM ──► Rebuild ayx.tbl_Transactions
                  (truncate + insert from ayx.vw_Transactions)

6:24–6:26 AM ──► Refresh billing-derived tables
                  (zero dollar denials, E&M forms data)

6:45–6:50 AM ──► Refresh counselor KPI snapshots
                  pba.tbl_vw_CounselorSupervision_KPISite
                  pba.tbl_vw_CounselorSupervision_KPICounselor
```

All steps write success/failure to `tsk.tbl_ErrorLog`.

**Risk:** If the AzureAgent process crashes or the VM restarts, the time windows are missed and the refresh doesn't happen. There is no retry/catch-up logic.

---

## 12. ETLMgr — The Monitoring Desktop App

A Windows Forms application (`net8.0-windows`) for the operations team.

**What the UI shows:**
- Date picker to select a work date
- Grid with parent tasks showing:
  - Task name & site
  - Status: Pending / Processing / Completed / Error
  - Duration
  - Row count
  - Remaining child tasks (still pending)
  - Failed child tasks (in error state)
  - Last modified time

**Connects directly** to `BHG_DR` via `SqlConnection` (not through BHG-DR-LIB). This was likely done because it's a separate solution targeting .NET 8 (WinForms).

---

## 13. Critical Issues to Know

### 🔴 SECURITY — Hardcoded Credentials in Source Code

Multiple files contain **live Azure SQL credentials directly in the code**:
- `BHG_DRContext.cs` — `OnConfiguring` method
- `SQLSvrManager.cs` — `ConnectionString` field
- `ETLMgrForm.cs` — constructor

**This is a critical security risk.** These credentials are now in source control history.

**What should be done:**
1. **Rotate the passwords immediately** in Azure AD / SQL Server
2. Move connection strings to `appsettings.json` + `Azure Key Vault` or environment variables
3. Use `.gitignore` to prevent secrets from re-entering source control

---

### 🟡 TECH DEBT — Outdated Framework

`BHGTaskRunner`, `Scheduler`, `AzureAgent`, and `BHG-DR-LIB` all target **`netcoreapp3.1`** — which reached **end-of-life in December 2022**. This means:
- No more security patches from Microsoft
- Some NuGet packages may drop support
- Should be migrated to `.NET 8` (LTS, supported until 2026)

---

### 🟡 CODE DUPLICATION — PHC Folder

The `PHC/` folder is an uncompilable copy of core library code. If `BHG-DR-LIB` is updated:
- `PHC/` does NOT get those updates automatically
- This leads to bugs where PHC-related logic differs from the main path
- Should be deleted or properly integrated into the main solution

---

### 🟡 FRAGILE SCHEDULING — AzureAgent Time Windows

The AzureAgent fires in narrow 2-minute windows. If the process isn't running (crash, restart, deployment), the batch is **silently skipped** with no alerting and no retry.

---

### 🔵 DESIGN NOTE — Metadata-Driven but With a Big `switch`

The system is designed to be **metadata-driven** (column mappings in `dms.vw_MapSrc2Dsn`, task configs in `dms.vw_MapAction`). However, `BHGTaskRunner/Program.cs` contains a **huge `switch` statement on task names** (300+ lines) for specialized logic. Adding a new table type requires code changes to this switch, not just database configuration.

---

## 14. Glossary

| Term | Meaning |
|------|---------|
| **BHG** | Behavioral Health Group — the organization running opioid treatment clinics |
| **SAMMS** | Substance Abuse Management & Monitoring System — the clinical software each site runs locally |
| **ETL** | Extract, Transform, Load — the process of copying data from source to destination |
| **OTP** | Opioid Treatment Program — a federally-licensed clinic providing methadone/buprenorphine treatment |
| **BHG_DR** | BHG Data Repository — the central Azure SQL database that holds consolidated data |
| **EF Core** | Entity Framework Core — Microsoft's ORM library that lets C# code work with SQL tables as objects |
| **SqlBulkCopy** | .NET API for very fast bulk row insertion into SQL Server |
| **stg.*** | Staging schema — temporary tables used as a landing zone before merge stored procedures run |
| **RowChkSum** | A checksum column computed from row data used to detect if a row has changed since last load |
| **ActionKey / ActionStepKey** | IDs that link a task to its column mapping configuration in `dms.vw_MapSrc2Dsn` |
| **P1 / P2** | Phase 1 / Phase 2 within a timezone batch — P1 = general data, P2 = financial data |
| **PHC** | A specific clinic site code (appears to be a separate partner/franchise) |
| **LAB** | Another specific site code with some schema differences |
| **DART** | Drug Abuse Reporting Tool — the billing/service tracking system |
| **BAM** | Brief Addiction Monitor — a validated clinical assessment tool |
| **COWS** | Clinical Opiate Withdrawal Scale — a clinical assessment for withdrawal severity |
| **KPI** | Key Performance Indicator — metrics tracked in the `pba.*` schema |
| **WinForms** | Windows Forms — Microsoft's desktop UI framework used by ETLMgr |
| **ayx** | Schema prefix for Alteryx — a data analytics tool that reads from BHG_DR |
| **pba** | Schema prefix for "performance-based analytics" or similar |
| **tsk** | Schema prefix for "task" — holds orchestration/scheduling tables |
| **dms** | Schema prefix for "data mapping service" — holds ETL configuration |

---

*Document generated: March 2026. Based on static code analysis of BCAppCode.*
