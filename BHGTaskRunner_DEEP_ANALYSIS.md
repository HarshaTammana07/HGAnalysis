# BHGTaskRunner — Program.cs Deep Analysis

> **File:** `BCAppCode\BHGTaskRunner\Program.cs`  
> **Size:** 3,375 lines — one single `Main()` method  
> **Purpose:** The core ETL engine that moves clinical data from every BHG clinic's local SAMMS database into the central Azure SQL warehouse (`BHG_DR`)

---

## Table of Contents

1. [What This File Is & What It Does](#1-what-this-file-is--what-it-does)
2. [The Tools It Uses (Initialization)](#2-the-tools-it-uses-initialization)
3. [Step 1 — Deciding Which Batch to Run (args switch)](#3-step-1--deciding-which-batch-to-run)
4. [Step 2 — The Outer Loop (Parent Tasks)](#4-step-2--the-outer-loop-parent-tasks)
5. [Step 3 — The Inner Loop (Child Tasks — Per Site Per Table)](#5-step-3--the-inner-loop-child-tasks)
6. [Step 4 — Building the SELECT Query](#6-step-4--building-the-select-query)
7. [The Giant Switch — Every Table Case Explained](#7-the-giant-switch--every-table-case-explained)
   - [Control/Configuration Tables](#71-controlconfiguration-tables-ctrl)
   - [Patient Clinical Tables](#72-patient-clinical-tables-pats)
   - [Assessment Tables](#73-assessment-tables-pats)
   - [Billing & Financial Tables](#74-billing--financial-tables-pats)
   - [Forms & Documents](#75-forms--documents-pats)
   - [Special / Complex Cases](#76-special--complex-cases)
8. [Step 5 — Error Handling & Task Closeout](#8-step-5--error-handling--task-closeout)
9. [Step 6 — Continuous Worker Pattern](#9-step-6--continuous-worker-pattern)
10. [Special Site-Specific Behaviors](#10-special-site-specific-behaviors)
11. [Date Window Logic](#11-date-window-logic)
12. [RowTrax — Row Count Auditing](#12-rowtrax--row-count-auditing)
13. [The Reload Flag](#13-the-reload-flag)
14. [Complete Switch Case Reference Table](#14-complete-switch-case-reference-table)
15. [Code Smells & Technical Notes](#15-code-smells--technical-notes)

---

## 1. What This File Is & What It Does

`Program.cs` is **the entire BHGTaskRunner executable in one method**. It has no classes, no helper methods, no abstraction — just one enormous `static void Main()` that is 3,375 lines long.

**In plain English:** It reads a to-do list of ETL tasks from the database, then for each task, it:
1. Connects to a specific clinic's local SAMMS SQL Server
2. Runs a SELECT query to get data
3. Saves that data into the central Azure SQL warehouse

It does this for **~70 different clinical data tables**, each with its own specialized logic, site-specific overrides, column existence checks, and date filtering rules.

```
BHGTaskRunner.exe [arg]
       │
       ├─ Reads task queue from tsk.VwTaskList (central DB)
       ├─ Loops through parent tasks
       │    └─ Loops through child tasks (1 per site per table)
       │         ├─ Builds SELECT query
       │         ├─ Executes against site's local SAMMS DB
       │         └─ switch(TaskName) → save to BHG_DR
       └─ Repeats until no more pending tasks in batch
```

---

## 2. The Tools It Uses (Initialization)

At the top of `Main()`, six objects are created:

```
SelectConstructor sc   → builds dynamic SELECT column lists from mapping metadata
SQLSvrManager sm       → raw ADO.NET helper: runs queries, fills DataTables
BulkDartsSvc bldr      → SqlBulkCopy + staging merge stored procs (high-volume tables)
BHG_DRContext db       → EF Core DbContext: access to task queue and mapping tables
SaveData sd            → EF Core row-by-row upserts (lower-volume tables)
bool ChkSumEnabled     → whether to include RowChkSum in SELECT (change detection)
```

**Two task lists are loaded upfront:**
- `Tasks` = ALL pending child tasks (Status=17, not PHC, RunAt < now) — used inside the inner loop
- `pTasks` = filtered parent tasks based on the command-line argument

---

## 3. Step 1 — Deciding Which Batch to Run

The program is launched with a single numeric argument (e.g., `BHGTaskRunner.exe 2`). The `switch` on `args[0]` filters which parent tasks to process:

```
No arg  → ALL pending tasks (no filter)
  "1"  → SAMMSGlobal only
  "2"  → Eastern/Central/Mountain/Pacific ETL P1 (all 4 timezones, Phase 1)
  "3"  → Everything EXCEPT P1, P2, and SAMMSGlobal (Forms, Notes, INV, etc.)
  "4"  → Eastern/Central/Mountain/Pacific ETL P2 (all 4 timezones, Phase 2)
  "5"  → Samms-LAB only
  "6"  → Samms-Forms only
  "7"  → SAMMS-ETL-Notes only
  "8"  → SAMMS-ETL-INV only
  "9"  → SAMMS-ETL-DartSvc only
 "10"  → SAMMS-ETL-Dose only
 "11"  → SAMMS-ETL-Orders only
```

All filters also enforce:
- `SiteCode != "PHC"` — PHC runs separately (different EXE/process)
- `Status == 17` — only Pending tasks
- `RunAt < DateTime.Now` — only tasks whose scheduled time has passed

---

## 4. Step 2 — The Outer Loop (Parent Tasks)

```csharp
foreach (var pt in pTasks
    .Where(x => x.ParentTaskId == null)   // only PARENT tasks (not children)
    .OrderBy(z => z.WorkDate)             // oldest work date first
    .ThenBy(o => o.RunAt))                // then by scheduled run time
```

For each parent task:
1. **Mark parent Running:** `ptask.Status = 18`
2. **Record start time** for duration tracking
3. **Loop through all child tasks** that belong to this parent

The parent task row represents a named batch (e.g., "Eastern ETL P1") and acts as a container/progress tracker for all the individual table+site child tasks beneath it.

---

## 5. Step 3 — The Inner Loop (Child Tasks)

```csharp
foreach (var st in Tasks
    .Where(x => x.ParentTaskId == pt.TaskId)  // children of THIS parent
    .OrderBy(o => o.TaskName)                  // order by table name
    .ThenBy(b => b.SiteCode)                   // then by site
    .ThenBy(d => d.FromTblVw))                 // then by source view/table
```

Each child task (`st`) represents one specific combination of **destination table + site code**. For example:
- `TaskName = "pats.tbl_Dose"`, `SiteCode = "NYC01"` → pull dosing records from NYC01's SAMMS DB

For each child task, the code:
1. Marks the child task as Running (`Status = 18`)
2. Creates a new `RCodes` object (result tracking)
3. Loads column mapping from `db.WorkToDo` (filtered by `ActionKey` + `ActionStepKey`)
4. Builds the SELECT query
5. Runs the giant `switch` on `st.TaskName.ToLower()`
6. On completion or error, writes results back to the task row

---

## 6. Step 4 — Building the SELECT Query

Before the switch runs, two things are built:

**A. The column list (`strFlds`):**
```csharp
strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
    .Replace("@SiteCode", "'" + st.SiteCode + "'")
    .Replace("@Samms", "'SAMMS'");
```
- `SelectConstructor.GetSLT()` reads the `dms.VwMapSrc2Dsn` mapping (loaded as `tdwork`) and builds the SELECT column list
- If `ChkSumEnabled = true` (ActionKey != 3), it also includes a `RowChkSum` computed column for change detection
- Site-specific literal values (`@SiteCode`, `@Samms`) are substituted in

**B. The base query string (`strCmd`):**
```csharp
strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw;
```
A `WHERE` clause is then typically appended inside each switch case using:
```csharp
string strWhere = st.WhereCondition
    .Replace("@SiteCode", ...)
    .Replace("@WorkDate", ...)    // WorkDate - 15 days by default
    .Replace("@Samms", ...)
```
> `DaysBack = -15` is the default lookback window — most tables pull data modified within the last 15 days

---

## 7. The Giant Switch — Every Table Case Explained

The heart of the program is a `switch (st.TaskName.ToLower())` with approximately **70 distinct cases**. Every case follows the same pattern:
1. Optionally modify `strCmd` (add WHERE, fix column names, handle site quirks)
2. Execute: `SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`
3. Save: `rCodes = sd.SaveXxx(...)` or `rCodes = bldr.BulkDartsSrvLoader(...)`

### 7.1 Control/Configuration Tables (`ctrl.*`)

| Case | Source View/Table | Save Method | Notes |
|------|------------------|-------------|-------|
| `ctrl.tbl_3psetup` | 3rd party billing setup | `Save3pSetup()` | Simple EF upsert |
| `ctrl.tbl_claimstatus` | Claim status codes | `SaveClaimStatus()` | WHERE: created >= 12 months ago |
| `ctrl.tbl_clinic` | Clinic master data | `SaveClinic()` | LAB site: removes `PullPicsFromDB` column |
| `ctrl.tbl_consents` | Consent types | `SaveGlobalConsents()` or `SaveGlobalConsentsPhc()` | PHC uses different save method |
| `ctrl.tbl_globaldevices` | Global devices | `SaveGlobalDevices()` | Standard EF upsert |
| `ctrl.tbl_invtype` | Inventory types | `SaveInvTypes()` | Standard EF upsert |
| `ctrl.tbl_user` | Users/staff | `SaveGlobalUser()` | Standard EF upsert |
| `ctrl.tbl_usersites` | User-site assignments | `SaveGlobalUserSite()` | Standard EF upsert |
| `ctrl.tbl_drodownlistitems` | Dropdown list items | `SavedropDownListItems()` | Config/reference data |

---

### 7.2 Patient Clinical Tables (`pats.*`)

| Case | What It Is | Save Method | Special Handling |
|------|-----------|-------------|-----------------|
| `pats.tbl_clientdemo1` | Patient demographics part 1 | `SaveClientDemo1var()` | RowTrax audit: counts source vs central rows |
| `pats.tbl_clientdemo2` | Patient demographics part 2 | `SaveClientDemo2()` | ActionKey=3 is skipped (commented-out legacy path) |
| `stg.clientdemo` | Full client demographics (bulk path) | `BulkDartsSrvLoader()` → `stg.clientdemo` | Hardcoded SELECT query (all demographic fields + RowChkSum) |
| `pats.tbl_codes` | Reference code table | `SaveCodes()` | Standard EF upsert |
| `pats.tbl_customquestions` | Custom clinical questions | `SaveCustomQuestions()` | Standard EF upsert |
| `pats.tbl_customanswers` | Answers to custom questions | `SaveCustomAnswers()` | Standard EF upsert |
| `pats.tbl_enrollment` | Patient program enrollment | `SaveEnrollment()` | Checks if `Modality` column exists (adds it if so); checks `TreatmentLevel` column; LAB site skipped entirely |
| `pats.tbl_checkin` | Patient check-in records | `SaveCheckIn()` | Checks if `ciQUEUETIME` column exists before including it |
| `pats.tbl_appointments` | Patient appointments | `SaveAppointments()` | Standard EF upsert |
| `pats.tbl_appointmentattend` | Appointment attendance | `SaveAppointmentAttend()` | Standard EF upsert |
| `pats.tbl_uasched` | UA screening schedule | `SaveUASched()` | Checks if `tblUASched` table exists first; uses DISTINCT |
| `pats.tbl_uaresults` | Urinalysis results | `SaveUAResults()` | LAB site: removes `LabName` and `UAEval` columns; Reload mode: no date filter |
| `pats.tbl_uaresultdetail` | UA result details | `BulkDartsSrvLoader()` | Bulk path; high volume |
| `pats.tbl_labresult` | Lab results | `SaveLABResults()` | LAB site skipped (LAB IS the lab — data flows differently) |
| `pats.tbl_labresultdetail` | Lab result details | `BulkDartsSrvLoader()` | Bulk path |
| `pats.tbl_bottle` | Medication bottles | `SaveBottles()` | LAB site: removes `ExpDate` column |
| `pats.tbl_liquidlog` | Liquid medication log | `SaveLiquidlog()` OR `BulkDartsSrvLoader()` | Dual path: EF for normal runs; BulkCopy+`stg.sp_liquidlog_Merge` for Reload |
| `pats.tbl_services` | Clinical services | `SaveServices()` | Only saves if rows returned |
| `pats.tbl_fmp` | Financial/miscellaneous payments | `SaveFmp()` | Standard EF upsert |
| `pats.tbl_tbldiag10` | ICD-10 diagnosis codes | `SaveTblDiags()` | Standard EF upsert |

---

### 7.3 Assessment Tables (`pats.*`)

All assessment tables follow the same simple pattern: append WHERE clause → pull → SaveXxx(). They all use EF Core upserts.

| Case | Clinical Assessment |
|------|-------------------|
| `pats.tbl_admissionassessment` | New patient intake assessment (ASAM) |
| `pats.tbl_admissionassessmentsummary` | Summary of admission assessment |
| `pats.tbl_admissionassessmentdimensionfour` | ASAM Dimension 4 — Readiness for Change |
| `pats.tbl_admissionassessmentdimensiononedisorder` | ASAM Dimension 1 — Substance Use |
| `pats.tbl_admissionassessmentdimensionfivesubstanceuse` | ASAM Dimension 5 — Relapse Potential |
| `pats.tbl_admissionassessmentdimensiontwo` | ASAM Dimension 2 — Biomedical |
| `pats.tbl_admissionassessmentdimensionthree` | ASAM Dimension 3 — Emotional/Behavioral |
| `pats.tbl_admissionassessmentdimensionsix` | ASAM Dimension 6 — Living Environment |
| `pats.tbl_admissionassessmentsubstanceusehistory` | Substance use history on admission |
| `pats.tbl_assessmentsubstanceusehistory` | Ongoing substance use history |
| `pats.tbl_reassessment` | Periodic re-assessment (main) |
| `pats.tbl_reassessmentoccupational` | Re-assessment — Employment/Education |
| `pats.tbl_reassessmentfamily` | Re-assessment — Family/Social |
| `pats.tbl_reassessmentlegal` | Re-assessment — Legal Status |
| `pats.tbl_reassessmentmentalhealth` | Re-assessment — Mental Health |
| `pats.tbl_reassessmentsubstanceuse` | Re-assessment — Substance Use |
| `pats.tbl_reassessmentphysicalhealth` | Re-assessment — Physical Health |
| `pats.tbl_reassessmentsocial` | Re-assessment — Social Environment |
| `pats.tbl_reassessmenttreatment` | Re-assessment — Treatment Plan |
| `pats.tbl_bamform` | BAM (Brief Addiction Monitor) form |
| `pats.tbl_bamscore` | BAM calculated scores |
| `pats.tbl_pa` | Pre-Admission main record |
| `pats.tbl_padimension1` through `pats.tbl_padimension6` | Pre-Admission ASAM dimensions 1-6 |
| `pats.tbl_pacounselorreview` | Pre-Admission counselor review |
| `pats.tbl_comprehensiveassessmentform` | Comprehensive Assessment Form |
| `pats.tbl_financialhardshipapplication` | Financial Hardship Application |
| `pats.tbl_orientationchecklistnew` | New patient orientation checklist |
| `pats.tbl_treatmentlevel` | Patient treatment level classification |
| `pats.tbl_mncomprehensiveassessment` | MN state: Comprehensive Assessment |
| `pats.tbl_mncomprehensiveassessmentlevelofcare` | MN state: Level of Care determination |
| `pats.tbl_vacomprehensiveassessment` | VA (Virginia/Veteran?) Comprehensive Assessment |
| `pats.tbl_vacomprehensiveassessmentsummary` | VA Comprehensive Assessment Summary |
| `pats.tbl_newadmissionassessment` | New-format Admission Assessment |
| `pats.tbl_newadmissionassessmentasamdimension6` | New Admission: ASAM Dimension 6 |
| `pats.tbl_newperiodicreassessment` | New-format Periodic Re-Assessment |
| `pats.tbl_newperiodicreassessmentcounselorreview` | New Periodic Re-Assessment: Counselor Review |
| `pats.tbl_preadmissionreferralsource` | Pre-Admission referral source (added 9/12/2025) |

> **Pattern for "table existence check" cases:** Several newer assessment tables are only present in clinics running a newer SAMMS version. These cases first run `SELECT name FROM sys.tables WHERE name = '...'` against the source DB. If 0 rows return, the table doesn't exist there — skip gracefully and set `rCodes.IsResult = true` (success with 0 rows).

---

### 7.4 Billing & Financial Tables (`pats.*`)

| Case | What It Is | Write Strategy | Special Notes |
|------|-----------|---------------|---------------|
| `pats.tbl_claims` | Insurance claims | EF (`SaveClaims`) for VBRA/VMIN/VWBY/VBRP sites; `BulkDartsSrvLoader` for all others | Split by site code — a deliberate test/exception list |
| `pats.tbl_claimlineitem` | Claim line items | `BulkDartsSrvLoader` → `stg.tbl_claimlineitem` | Always bulk path; high volume |
| `pats.tbl_claimlineitemactivity` | Claim line item activity | `BulkDartsSrvLoader` → `stg.tbl_claimlineitemactivity` | Always bulk path |
| `pats.tbl_dartssrv` | DART services billed | `BulkDartsSrvLoader` → `stg.tbl_dartssrv` | **See DartsSrv special logic below** |
| `pats.tbl_dose` | Medication dosing records | EF (`SaveDoses`) for 4 specific sites; `BulkDartsSrvLoader` for all others | **See Dose special logic below** |
| `pats.tbl_dose_excuse` | Missed dose excuses | `SaveDoseExcuse()` (EF) | Standard; RowTrax enabled |
| `pats.tbl_bills` | Billing records | `SaveBills()` (EF) | Date filter: from beginning of WorkDate year, up to WorkDate+12 days |
| `pats.tbl_feesched` | Fee schedules by payer | `SaveFeeSchedules()` (EF) | Standard EF |
| `pats.tbl_globalpayor` | Payer/insurance master | `SaveGlobalPayer()` (EF) | Standard EF |
| `pats.tbl_payerclient` | Patient-to-payer assignments | `SavePayerClient()` or `RemovePayerClients()` (EF) | If `FromTblVw = "vw_PayerClt_INACTIVE"`, removes records instead |
| `pats.tbl_payerclthistory` | History of payer changes | `SavePayerCltHistory()` (EF) | Standard EF |
| `pats.tbl_pbi3payauth` | Payer authorizations | `SaveAuths()` (EF) | Standard EF |
| `pats.tbl_3pelig` | 3rd-party eligibility | `Save3pElig()` (EF) | Standard EF |
| `pats.tbl_3parnote` | 3rd-party AR notes | `Save3pArnote()` (EF) | LAB site: removes `globalBatchId` column |
| `pats.tbl_3pclaimnote` | 3rd-party claim notes | `Save3pClaimNote()` (EF) | LAB site: removes `globalBatchId` column |
| `pats.tbl_vw3pbillsub` | Billing submission | `BulkDartsSrvLoader` | Modifies query: adds DISTINCT, replaces NULLs |
| `pats.tbl_eandmformmdm` | E&M Medical Decision Making form | `SaveEMFormMDM()` (EF) | Hardcoded multi-join query (EandMForm + EandMFormMDM + SF_PatientPreAdmission) |
| `pats.tbl_eandmformpregnancy` | E&M Pregnancy form | `SaveEMFormPregnancy()` (EF) | Hardcoded multi-join query |

---

### 7.5 Forms & Documents

| Case | What It Is | Write Strategy | Notes |
|------|-----------|---------------|-------|
| `pats.tbl_formssammsclient` | SAMMS forms client metadata | `BulkDartsSrvLoader` | PHC uses hardcoded `PHCSQLVM` connection; excludes test sites (25, 38, 99, 100, 106, 115, 118); non-PHC reads from BHGDALLSQL05 |
| `pats.tbl_dbo_formquestionanswers` | All clinical form Q&A | EF (`SaveFormQuestionAnswers`) or BulkCopy (`stg.sp_FormQA_Merge`) | **Most complex case — see below** |
| `pats.tbl_dbo_formanswersignatures` | Form signatures | EF | Checks if `answersignature` table exists first |

---

### 7.6 Special / Complex Cases

#### `pats.tbl_orders` — Year-Partitioned Orders (lines 1168–1290)

Orders are spread across **13 year-partitioned tables** (2016–2028). The code:
1. Pulls all orders with one big query (no date filter — all years)
2. Splits the returned DataTable by year using LINQ:
   ```csharp
   SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2016)
   ```
3. Calls a separate Save method for each year: `SaveOrders2016()`, `SaveOrders2017()`, ..., `SaveOrders2028()`
4. Each SaveOrders call returns a boolean success flag; if any year fails, it stops processing subsequent years

**Why:** SQL Server partitioned tables for performance — splitting billions of order records across yearly tables avoids massive table scans.

---

#### `pats.tbl_dartssrv` — Dynamic Lookback Window (lines 865–907)

Normally DartsSrv uses 15 days back. But on **Fridays that end a month**, the lookback extends to **90 days** to catch any late-billed services from the prior month. There's also a one-off hardcoded date override for `1/24/2025` which extends it to 200 days.

```
Normal:               DaysBack = -15
Friday end-of-month:  DaysBack = -90
Special 1/24/2025:    DaysBack = -200
```

Also checks if `ServiceType` column exists in source (schema version detection) before including it.

---

#### `pats.tbl_dose` — Dual Write Path + Reload (lines 908–960)

**EF path** (for 4 specific sites: V10A, CBCO, V21, V10):
- Uses `SaveDoses()` EF upsert
- Reload flag supported

**Bulk path** (all other sites):
- Normal: BulkDartsSrvLoader with 6-month lookback
- Reload mode: **first deletes all dose records for that site** from the central DB, then bulk loads everything

**Date filter logic:**
```
Year(dtDate) >= [WorkDate - 1 year]
OR Year(dtMedDate) >= [WorkDate - 1 year]
AND dtDate <= WorkDate + 2 days
AND dtDate >= WorkDate - 6 months
```
This catches both recently dispensed doses AND retroactively entered records.

---

#### `ayx.tbl_preadmission_v6` — Schema Version + Column Detection (lines 173–248)

Before loading, this case:
1. Checks if table `SF_PatientPreAdmission` exists in source DB (`sys.tables` query)
2. Checks if `SchemaVersion != "V5"` (V5 sites use the older PreAdmission model)
3. Checks if the `ClientAddress` column exists (some sites are on an older V6 that lacks it)

Only if all three checks pass does it run a **hardcoded 30+ column SELECT** with calculated CASE fields (converting 0/1 integers to 'Yes'/'No' strings for a dozen boolean fields like `IsCurrentlyInOpiateProgram`, `IsPatientAtPainManagementClinic`, etc.)

---

#### `pats.tbl_cows_v6` — COWS (Clinical Opiate Withdrawal Scale) V6 (lines 738–830)

COWS uses the `SF_COWS` table (SalesForce-based newer schema). The code:
1. Checks if `SF_COWS` table exists
2. Introspects column names from `sys.all_columns` to detect which version of COWS schema the site has
3. Builds SELECT dynamically using `ISNULL(c.NewColumnName, c.OldColumnName)` patterns for 10+ columns that were renamed between versions (e.g., `Sweat` → `Sweating`, `Restless` → `Restlessness`, `Yawn` → `Yawning`, etc.)
4. Left-joins to `DroDownListItems` 11 times for human-readable descriptions of each severity score

---

#### `pats.tbl_dbo_formquestionanswers` — The Most Complex Case (lines 1463–2195)

This is the single most complex case in the file (~700 lines). It handles **all clinical form question/answer data** from the SalesForce-based Forms module.

**Phase 1: Check if Form table exists**
- Queries `sys.tables WHERE name = 'Form'`
- If it doesn't exist → skip with message "No Form table"

**Phase 2: Build base query**
A complex query joining `Form`, `FormTemplate`, `Question`, `Answer`, and `SF_PatientPreAdmission`. It handles:
- `IsDeleted` logic: a form is deleted if the form itself, the pre-admission record, OR the data form is deleted
- `QuestionOrderId` computation using `ROW_NUMBER() OVER(PARTITION BY ...)`
- Two UNION branches: one for forms with answers, one for form records with no answer rows

**Phase 3: Dynamic additional form tables (from `TblForms2Process`)**
The code reads `db.TblForms2Process` — a configuration table of additional form types. For each enabled form:
- Checks if the form's physical SQL table exists at the site
- Generates a UNION SELECT customized per table type

Currently active form types handled:
- `adversechildhood` — Adverse Childhood Experiences
- `financialhardshipapplication` — Financial Hardship Application
- `tbltp17review` — Treatment Plan (with 3-4 question rows per plan: Type, Phase, Next Due, Review Frequency)
- `tblorderreq` — Level Justification (approved orders only, excludes test data)
- `insurancebenefitverification` — Insurance Benefit Verification
- `referralform` — Referral Form
- `sf_understandingoftreatment` — Understanding of Treatment
- `sf_patientpreadmission` — Pre-Admission summary as a form
- `newperiodicreassessment` — New Periodic Re-Assessment as a form

**Phase 4: Execute and save**
- For certain sites (B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, DRD-SF, V12, B35, B25, V9, FW, LO, B42):
  - `BulkCopy → stg.tbl_FormQA` + `stg.sp_FormQA_Merge` proc + `pats.BAMMerge` proc
- For all other sites:
  - `sd.SaveFormQuestionAnswers()` EF upsert + `pats.BAMMerge` proc

> **Note:** There is a large `#region Dead Code` block (lines 1496–1906) containing 20+ commented-out UNION branches for older form types that were handled inline before the dynamic `TblForms2Process` approach was introduced. This represents the evolution of the system — originally every form type was hardcoded here.

---

#### `pats.tbl_briefaddictionmonitor` — PHC Special Connection (lines 549–572)

PHC site uses a **hardcoded connection string** to `PHCSQLVM\SAMMSGLOBAL` instead of the task's `st.ConStr`. This is because PHC's SAMMS database is on a different server and not stored in the standard connection mapping.

The date filter is also non-standard: it uses a 30-day lookback and specifically excludes clinic sites 25 and 100.

After saving, it calls `pats.BAMMergeGbl` stored procedure to propagate BAM data globally.

---

#### `pats.tbl_clinicalopiatewithdrawalscale` — Also PHC Special (lines 719–737)

Same pattern as BAM above — PHC uses the hardcoded `PHCSQLVM` connection.

---

#### `pats.tbl_bills` — Full Reload Support (lines 517–548)

Normal run: pulls bills where `year(billDate) >= current year AND billDate <= WorkDate + 12 days`.

**Reload mode:** DaysBack is set to `-728250` (approximately 2,000 years back — effectively "all time"), which combined with the year filter means it pulls every bill ever recorded for that site.

---

## 8. Step 5 — Error Handling & Task Closeout

Every `switch` case is wrapped in a `try/catch`:
```csharp
try {
    switch (st.TaskName.ToLower()) { ... }
}
catch (Exception e) {
    rCodes.ExceptMsg = e.Message;
    if (e.InnerException != null) {
        rCodes.ExceptInnerMsg = e.InnerException.Message;
    }
}
```

**After the switch (whether success or error):**
```csharp
task.RowCount     = rCodes.RowsProcessed;
task.RowsIns      = rCodes.RowsIns;
task.RowsUpd      = rCodes.RowsUpd;
task.ErrorMessage = rCodes.ExceptMsg + "     " + rCodes.ExceptInnerMsg;
// Truncate at 500 chars
task.Status = rCodes.IsResult ? 19 : 20;   // 19 = Done, 20 = Error
task.Duration = HH:MM:SS formatted elapsed time;
task.LastModAt = DateTime.Now;
// Update running parent duration too
db.SaveChanges();   // ← EF commits this to tsk.tbl_Tasks2
```

**Key behavior:** If an exception is thrown, the task gets `Status = 20` (Error) but the outer loop **continues** to the next child task. The system does not abort — one failed table does not stop the rest of the ETL.

---

## 9. Step 6 — Continuous Worker Pattern

At the end of each parent task completing, the task lists are **refreshed from the database**:

```csharp
ptask.Status = 19;  // Mark parent complete
db.SaveChanges();

// Re-query task lists
Tasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
pTasks = db.VwTaskList.Where(x => ... && x.Status == 17 ...).ToList();
```

This means the outer `foreach` loop on `pTasks` will process any **newly pending parent tasks** that appeared while earlier ones were running. The program keeps working through tasks until none remain — it doesn't stop after the initially loaded list.

**In effect, BHGTaskRunner acts as a continuous worker**, not a one-shot batch. It will run until the task queue for its batch is exhausted.

---

## 10. Special Site-Specific Behaviors

| Site Code | Table | Special Behavior |
|-----------|-------|-----------------|
| `PHC` | All | Excluded from this runner (PHC runs separately) |
| `PHC` | `pats.tbl_briefaddictionmonitor` | Uses hardcoded `PHCSQLVM` connection |
| `PHC` | `pats.tbl_clinicalopiatewithdrawalscale` | Uses hardcoded `PHCSQLVM` connection |
| `PHC` | `pats.tbl_formssammsclient` | Uses hardcoded `PHCSQLVM` connection; filters to `fscsite = 1` |
| `PHC` | `ctrl.tbl_consents` | Uses `SaveGlobalConsentsPhc()` instead of `SaveGlobalConsents()` |
| `LAB` | `pats.tbl_3parnote` | Removes `globalBatchId` column from SELECT |
| `LAB` | `pats.tbl_3pclaimnote` | Removes `globalBatchId` column from SELECT |
| `LAB` | `ctrl.tbl_clinic` | Removes `PullPicsFromDB` column |
| `LAB` | `pats.tbl_bottle` | Removes `ExpDate` column |
| `LAB` | `pats.tbl_enrollment` | Skipped entirely |
| `LAB` | `pats.tbl_labresult` | Skipped entirely (LAB is the source, not destination for lab results) |
| `LAB` | `pats.tbl_uaresults` | Removes `LabName` and `UAEval` columns |
| `CBNC` | `pats.tbl_enrollment` | Modality always included |
| `V10A`, `CBCO`, `V21`, `V10` | `pats.tbl_dose` | Uses EF save path instead of BulkCopy |
| `VBRA`, `VMIN`, `VWBY`, `VBRP` | `pats.tbl_claims` | Uses EF `SaveClaims()` instead of BulkCopy |
| B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, etc. | `pats.tbl_dbo_formquestionanswers` | Uses BulkCopy path to `stg.tbl_FormQA` |

---

## 11. Date Window Logic

| Context | DaysBack | What it means |
|---------|---------|---------------|
| **Default** | -15 | Pull records modified in the last 15 days |
| **DartsSrv normal** | -15 | 15 days |
| **DartsSrv Friday end-of-month** | -90 | 90 days (month-end cleanup) |
| **DartsSrv special 1/24/2025** | -200 | One-time override |
| **Dose (non-reload)** | -6 months | 6 months of dosing history |
| **Dose year filter** | -1 year for Year() check | Catches records from prior year still being updated |
| **Bills (normal)** | current year start | From January 1 of current year |
| **Bills (reload)** | -728250 days | All time |
| **FormQuestionAnswers** | DaysBack - 15 = -30 | 30 days for forms |
| **FormQuestionAnswers (reload)** | 1/1/2010 | From program's beginning |
| **PreAdmissionReferralSource** | DaysBack - 500 = -515 | Extra deep lookback for referrals |

The `@WorkDate` placeholder in `st.WhereCondition` is always replaced with `WorkDate.AddDays(DaysBack)`.

---

## 12. RowTrax — Row Count Auditing

Several cases include optional `RowTrax` (Row Tracking) logic. When `st.RowTrax.Value == true`, the code:
1. Queries the **source DB** for total row count: `SELECT COUNT(1) FROM source.table WHERE ...`
2. Queries the **central DB** for current count: `SELECT COUNT(1) FROM pats.tblXxx WHERE SiteCode = '...' AND RowState = 1`
3. Saves both numbers via `sd.SaveRowTrax()`

This creates an audit trail showing whether the source and central DB are in sync. Useful for:
- Detecting data drift
- Validating ETL completeness
- Compliance/QA reporting

Tables with RowTrax enabled: `tbl_Dose`, `tbl_DartsSrv`, `tbl_claims`, `tbl_claimlineitem`, `tbl_claimlineitemactivity`, `tbl_Orders`, `tbl_ClientDemo1`, `tbl_Enrollment`, `tbl_PayerClient`, `tbl_UASched`, `tbl_dose_excuse`, and others.

---

## 13. The Reload Flag

`st.Reload` is a nullable boolean on each child task that indicates a full reload instead of incremental.

| Table | Reload Behavior |
|-------|----------------|
| `pats.tbl_dose` (non-V10A sites) | DELETE all site rows from central DB first, then bulk load everything |
| `pats.tbl_dose` (V10A etc.) | No date filter — loads all non-null records |
| `pats.tbl_bills` | Sets DaysBack to -728,250 (all history) |
| `pats.tbl_liquidlog` | Uses BulkCopy instead of EF; no date filter |
| `pats.tbl_uaresults` | No date filter — loads all records |
| `pats.tbl_payerclient` | Skips WHERE clause entirely |
| `pats.tbl_dbo_formquestionanswers` | WorkDate set to `1/1/2010` (all forms since program start) |

---

## 14. Complete Switch Case Reference Table

| # | Task Name (switch case) | Write Strategy | Table Type |
|---|------------------------|---------------|-----------|
| 1 | `pats.tbl_3parnote` | EF | 3rd Party Notes |
| 2 | `pats.tbl_3pclaimnote` | EF | 3rd Party Notes |
| 3 | `ctrl.tbl_3psetup` | EF | Config |
| 4 | `ctrl.tbl_claimstatus` | EF | Config |
| 5 | `ayx.tbl_preadmission_v6` | EF | Pre-Admission |
| 6 | `ctrl.tbl_clinic` | EF | Config |
| 7 | `ctrl.tbl_consents` | EF | Config |
| 8 | `ctrl.tbl_globaldevices` | EF | Config |
| 9 | `ctrl.tbl_user` | EF | Config |
| 10 | `ctrl.tbl_usersites` | EF | Config |
| 11 | `pats.tbl_3pelig` | EF | Billing |
| 12 | `pats.tbl_admissionassessment` | EF | Assessment |
| 13 | `pats.tbl_admissionassessmentsummary` | EF | Assessment |
| 14 | `pats.tbl_admissionassessmentdimensionfour` | EF | Assessment |
| 15 | `pats.tbl_admissionassessmentdimensiononedisorder` | EF | Assessment |
| 16 | `pats.tbl_admissionassessmentdimensionfivesubstanceuse` | EF | Assessment |
| 17 | `pats.tbl_admissionassessmentdimensiontwo` | EF | Assessment |
| 18 | `pats.tbl_admissionassessmentdimensionthree` | EF | Assessment |
| 19 | `pats.tbl_admissionassessmentdimensionsix` | EF | Assessment |
| 20 | `pats.tbl_reassessment` | EF | Assessment |
| 21 | `pats.tbl_reassessmentoccupational` | EF | Assessment |
| 22 | `pats.tbl_reassessmentfamily` | EF | Assessment |
| 23 | `pats.tbl_reassessmentlegal` | EF | Assessment |
| 24 | `pats.tbl_reassessmentmentalhealth` | EF | Assessment |
| 25 | `pats.tbl_reassessmentsubstanceuse` | EF | Assessment |
| 26 | `pats.tbl_reassessmentphysicalhealth` | EF | Assessment |
| 27 | `pats.tbl_reassessmentsocial` | EF | Assessment |
| 28 | `pats.tbl_reassessmenttreatment` | EF | Assessment |
| 29 | `pats.tbl_appointments` | EF | Clinical |
| 30 | `pats.tbl_appointmentattend` | EF | Clinical |
| 31 | `pats.tbl_orientationchecklistnew` | EF | Clinical |
| 32 | `ctrl.tbl_invtype` | EF | Config |
| 33 | `pats.tbl_liquidlog` | EF or BulkCopy | Inventory |
| 34 | `pats.tbl_bamform` | EF | Assessment |
| 35 | `pats.tbl_bamscore` | EF | Assessment |
| 36 | `pats.tbl_tbldiag10` | EF | Clinical |
| 37 | `pats.tbl_bottle` | EF | Inventory |
| 38 | `pats.tbl_bills` | EF | Billing |
| 39 | `pats.tbl_briefaddictionmonitor` | EF + EXEC BAMMergeGbl | Assessment |
| 40 | `pats.tbl_checkin` | EF | Clinical |
| 41 | `pats.tbl_claims` | EF or BulkCopy | Billing |
| 42 | `pats.tbl_claimlineitem` | BulkCopy | Billing |
| 43 | `pats.tbl_claimlineitemactivity` | BulkCopy | Billing |
| 44 | `stg.clientdemo` | BulkCopy | Demographics |
| 45 | `pats.tbl_clientdemo1` | EF | Demographics |
| 46 | `pats.tbl_clientdemo2` | EF | Demographics |
| 47 | `pats.tbl_clinicalopiatewithdrawalscale` | EF | Assessment |
| 48 | `pats.tbl_cows_v6` | EF | Assessment |
| 49 | `pats.tbl_codes` | EF | Config |
| 50 | `pats.tbl_customquestions` | EF | Config |
| 51 | `pats.tbl_customanswers` | EF | Clinical |
| 52 | `pats.tbl_dartssrv` | BulkCopy | Billing/Services |
| 53 | `pats.tbl_dose` | EF or BulkCopy | Clinical (dosing) |
| 54 | `pats.tbl_dose_excuse` | EF | Clinical |
| 55 | `pats.tbl_eandmformmdm` | EF | Clinical Forms |
| 56 | `pats.tbl_eandmformpregnancy` | EF | Clinical Forms |
| 57 | `pats.tbl_enrollment` | EF | Clinical |
| 58 | `pats.tbl_feesched` | EF | Billing |
| 59 | `pats.tbl_fmp` | EF | Billing |
| 60 | `pats.tbl_formssammsclient` | BulkCopy | Forms |
| 61 | `pats.tbl_globalpayor` | EF | Billing |
| 62 | `pats.tbl_orders` | EF (per-year) | Orders |
| 63 | `pats.tbl_payerclient` | EF | Billing |
| 64 | `pats.tbl_pbi3payauth` | EF | Billing |
| 65 | `pats.tbl_preadmissionreferralsource` | EF | Pre-Admission |
| 66 | `pats.tbl_services` | EF | Clinical |
| 67 | `pats.tbl_uasched` | EF | Clinical |
| 68 | `pats.tbl_labresultdetail` | BulkCopy | Clinical |
| 69 | `pats.tbl_labresult` | EF | Clinical |
| 70 | `pats.tbl_uaresultdetail` | BulkCopy | Clinical |
| 71 | `pats.tbl_uaresults` | EF | Clinical |
| 72 | `pats.tbl_dbo_formquestionanswers` | EF or BulkCopy | Forms (most complex) |
| 73 | `pats.tbl_dbo_formanswersignatures` | EF | Forms |
| 74 | `pats.tbl_vw3pbillsub` | BulkCopy | Billing |
| 75 | `pats.tbl_payerclthistory` | EF | Billing |
| 76 | `pats.tbl_treatmentlevel` | EF | Clinical |
| 77 | `pats.tbl_admissionassessmentsubstanceusehistory` | EF | Assessment |
| 78 | `pats.tbl_assessmentsubstanceusehistory` | EF | Assessment |
| 79 | `pats.tbl_comprehensiveassessmentform` | EF | Assessment |
| 80 | `pats.tbl_pa` | EF | Pre-Admission |
| 81 | `pats.tbl_mncomprehensiveassessment` | EF | Assessment (MN) |
| 82 | `pats.tbl_mncomprehensiveassessmentlevelofcare` | EF | Assessment (MN) |
| 83 | `pats.tbl_vacomprehensiveassessment` | EF | Assessment (VA) |
| 84 | `pats.tbl_vacomprehensiveassessmentsummary` | EF | Assessment (VA) |
| 85 | `pats.tbl_newadmissionassessment` | EF | Assessment (new format) |
| 86 | `pats.tbl_newadmissionassessmentasamdimension6` | EF | Assessment |
| 87 | `pats.tbl_newperiodicreassessment` | EF | Assessment (new format) |
| 88 | `pats.tbl_newperiodicreassessmentcounselorreview` | EF | Assessment |
| 89 | `pats.tbl_financialhardshipapplication` | EF | Clinical |
| 90 | `pats.tbl_pacounselorreview` | EF | Pre-Admission |
| 91 | `pats.tbl_padimension1` | EF | Pre-Admission |
| 92 | `pats.tbl_padimension2` | EF | Pre-Admission |
| 93 | `pats.tbl_padimension3` | EF | Pre-Admission |
| 94 | `pats.tbl_padimension4` | EF | Pre-Admission |
| 95 | `pats.tbl_padimension5` | EF | Pre-Admission |
| 96 | `pats.tbl_padimension6` | EF | Pre-Admission |
| 97 | `ctrl.tbl_drodownlistitems` | EF | Config |

---

## 15. Code Smells & Technical Notes

### One Method Does Everything
The entire 3,375-line program is a single `Main()` method. No classes, no functions, no separation of concerns. This makes it:
- Very hard to test individual cases in isolation
- Impossible to add parallel processing per table
- Difficult to onboard new developers

### Massive Code Duplication
Approximately 90% of the switch cases share identical patterns:
```csharp
strCmd += " Where " + strWhere + " " + st.SortOrder;
SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
rCodes = sd.SaveXxx(SrcDt, st.SiteCode, ...);
```
This could be extracted into a generic method and driven purely by configuration.

### Large Dead Code Region
Lines 1496–1906 contain a `#region Dead Code` with 400+ lines of commented-out UNION branches that were replaced by the `TblForms2Process` configuration approach. This should be deleted.

### Hardcoded Connection Strings
`PHCSQLVM` and `BHGDALLSQL05\SQL2016SAMMS` are hardcoded directly in this file. If those servers change, this file must be recompiled.

### Task List Loaded Twice
Both `Tasks` (all children) and `pTasks` (filtered parents) are loaded at the start. `Tasks` contains ALL pending children for the entire day, but the inner loop only processes children of the current parent. This means hundreds of unrelated child tasks sit in memory throughout the run.

### The Switch is the Only Extension Point
To add ETL for a new table, a developer must:
1. Add a new case to the `switch` statement in this file
2. Add a new `SaveXxx()` method to one of the `Save*.cs` partial class files
3. Configure the table in `dms.vw_MapAction` and `dms.vw_MapSrc2Dsn`

There is no plugin model — all logic is compiled in.

### Re-querying pTasks at End
The task list is re-queried from the database at the end of each parent task completion. This means the `switch` on `args[0]` is duplicated **a second time** at lines 3323–3370 (identical to lines 30–73). This is a copy-paste artifact that should be extracted into a method.

---

*Analysis based on static code review of BHGTaskRunner/Program.cs (3,375 lines). March 2026.*


How the 11 Pipelines Actually Run
One EXE, one arg = one pipeline:

BHGTaskRunner.exe 1   ← runs SAMMSGlobal
BHGTaskRunner.exe 2   ← runs Eastern/Central/Mountain/Pacific ETL P1
BHGTaskRunner.exe 3   ← runs Forms, Notes, INV, etc.
BHGTaskRunner.exe 4   ← runs ETL P2 (financial tables)
BHGTaskRunner.exe 5   ← runs Samms-LAB
...and so on up to 11
Each call only processes its assigned batch and stops when that queue is empty. So to run all 11, the EXE must be launched 11 separate times each day.

Who Actually Triggers Them?
The code itself does not auto-trigger the next pipeline. That triggering must come from outside — most likely:

Windows Task Scheduler on the server — 11 separate scheduled tasks, each running the EXE with a different argument at different times of day
An Azure automation job or pipeline
Manual invocation by the ops team
The codebase does not show whatever triggers these 11 calls — that configuration lives outside of the code (in Windows Task Scheduler or Azure, not in any .cs file here).

What the Scheduler Does (Once Per Day)
The Scheduler.exe runs once per day and seeds all tasks for all 11 pipelines into tsk.tbl_Tasks2 at once. After that, it bumps the NextRunTime by 1 day and exits.

Scheduler.exe (once/day)
    └── Creates parent + child task rows for ALL 11 pipelines
         Status = 17 (Pending), RunAt = scheduled time
BHGTaskRunner.exe 1   ← picks up arg-1 tasks when their RunAt time passes
BHGTaskRunner.exe 2   ← picks up arg-2 tasks when their RunAt time passes
...
BHGTaskRunner.exe 11  ← picks up arg-11 tasks when their RunAt time passes
Are They Run in a Specific Order Every Day?
Yes, and the order matters:

Step 1:  Arg 1  — SAMMSGlobal first (shared/global tables, no timezone dependency)
Step 2:  Arg 2  — ETL P1 (patient data, by timezone — runs after clinics open)
Step 3:  Arg 3  — Special bundles (Forms, Notes, INV — independent)
Step 4:  Arg 4  — ETL P2 (financial/billing — MUST run after P1, depends on patient data)
Step 5+: Args 5–11 — Specialized tables (Dose, Orders, DartsSrv, etc.)
P1 before P2 is critical — billing data depends on patient/enrollment data being loaded first. Running P2 before P1 would cause foreign key or data integrity issues.

Does the Code Know What Runs When?
Not really. The code just processes whatever tasks have Status = 17 and RunAt < NOW for its assigned batch. The timing is controlled entirely by:

NextRunTime in tsk.tbl_Schedule (set by Scheduler)
Whatever external scheduler invokes the EXE at the right time
So the answer to your question: Yes, all 11 pipelines are intended to run every day, but it requires 11 separate invocations of the same EXE with different arguments, triggered externally at staggered times throughout the day.



The file breaks down every single thing the 3,375-line Program.cs does:

Section	What You'll Learn
Initialization	The 6 tools created at startup and what each one does
Batch selection	How the command-line arg (1–11) controls which tables get processed
Outer loop	How parent tasks work as containers/progress trackers
Inner loop	How each child task (one table + one site) is executed
SELECT query building	How SelectConstructor + column mapping metadata builds the SQL dynamically
The giant switch (all 97 cases)	Every table case explained: what it pulls, how it saves, any special logic
Special complex cases	Deep dives into pats.tbl_orders (year partitioning), pats.tbl_dose (dual write paths), pats.tbl_dartssrv (dynamic lookback), pats.tbl_cows_v6 (dynamic column detection), pats.tbl_dbo_formquestionanswers (700-line mega case)
Error handling	How exceptions are caught, logged, and the loop continues
Continuous worker pattern	Why it re-queries the task list after each parent completes
Site-specific overrides	Every PHC/LAB/VBRA/etc. special behavior in one table
Date windows	All the different lookback periods and when they apply
RowTrax auditing	How row counts are compared between source and central DB
Reload flag	What full reload mode does for each table
Complete reference table	All 97 switch cases in one lookup table
Code issues	Honest notes on tech debt and what should be refactored