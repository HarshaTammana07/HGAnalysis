# BHG ETL System — Complete Architecture & Workflow
**System:** BCAppCode — BHG Recovery Data Pipeline  
**Server:** 10.50.5.223 (machine where everything runs)  
**Exe Path:** `C:\users\bcatellier\Documents\BCApps`  
**Source Path:** `C:\users\bcatellier\Documents\BCAppCode`  
**Date:** 2026-03-25

---

## PART 1 — What Is This System?

BHG Recovery operates **80+ addiction treatment clinics** across the USA. Every clinic runs a local patient management software called **SAMMS** on its own SQL Server database. Each clinic's SAMMS database is completely separate — they don't talk to each other.

**The Problem:**  
The leadership team cannot see all clinics together. Each clinic's data is stuck in its own database. You cannot run a single report across all clinics.

**The Solution — This ETL System:**  
Every evening, this system **automatically** extracts data from all 80+ clinic SAMMS databases and loads it into one single central Azure SQL database called **BHG_DR**. By morning, every clinic's data is in one place — ready for reports, dashboards, and analytics.

```
BEFORE ETL:
  Clinic B01 SAMMS DB → only B01 sees its own data
  Clinic B02 SAMMS DB → only B02 sees its own data
  Clinic B03 SAMMS DB → only B03 sees its own data
  ...80+ more silos...

AFTER ETL (every morning):
  Azure BHG_DR → EVERYONE sees ALL clinics together
                  ✔ Patient enrollments from all sites
                  ✔ Drug test results from all sites
                  ✔ Counseling services from all sites
                  ✔ Billing/claims from all sites
                  ✔ Medication doses from all sites
                  ✔ All historical data back to 2014+
```

---

## PART 2 — The System Architecture (All Components)

### 2.1 — The 5 Executables (Programs That Run)

```
C:\users\bcatellier\Documents\BCApps\
├── Scheduler.exe          ← "The Planner"         runs once at 5:15 PM
├── BHGTaskRunner.exe      ← "The Worker"           runs 11 times (7PM–2:29AM)
├── AzureAgent.exe         ← "The Post-Processor"   runs at 6:24AM, 6:45AM, 7:00AM
├── PHC\PHC.exe            ← "The PHC Worker"       separate for PHC clinics
└── ETLMgr.exe             ← "The Monitor"          desktop app (manual use)
```

### 2.2 — The 2 Databases

```
SOURCE (80+ separate databases):                DESTINATION (1 central database):
════════════════════════════════                ═════════════════════════════════
Each clinic has its own SQL Server              Azure SQL Server:
on-premise or Netalytics cloud:                 bhgazuresql01.database.windows.net
                                                Database: BHG_DR
  SAMMS-ColoradoSpringsV5                       
  SAMMS-NashvilleV5                             Schemas inside BHG_DR:
  SAMMS-LawrenceV5                               pats.*  → patient/clinical data
  SAMMS-ArkansasV5                               ctrl.*  → control/config tables
  SAMMSGLOBAL                                    dms.*   → ETL metadata/mappings
  Methasoft_BHG_*                                tsk.*   → task scheduling/logging
  AdvancedMD_*                                   stg.*   → staging tables
  ... 80+ more ...                               ayx.*   → analytics/reporting
                                                 pba.*   → performance-based analytics
```

### 2.3 — The Code Projects (Visual Studio Solution: BHG.sln)

```
BCAppCode\
├── BHG\
│   ├── Scheduler\          ← creates daily task list
│   ├── AzureAgent\         ← post-ETL aggregations
│   └── bhg.TestCode\       ← developer sandbox (not production)
├── BHGTaskRunner\          ← main ETL worker
├── BHG-DR-LIB\             ← shared library (reused by all projects)
│   ├── Models\             ← EF Core entity classes (one per destination table)
│   ├── SelectConstructor.cs← builds SQL SELECT statements from metadata
│   ├── SQLSvrManager.cs    ← executes raw SQL on any database
│   ├── SaveData.cs         ← EF Core upsert logic (partial class)
│   ├── Save*.cs            ← individual save methods per table
│   └── BulkDartsSvc.cs     ← SqlBulkCopy bulk loading
├── PHC\                    ← separate ETL for PHC clinics
└── ETLMgr\                 ← WinForms monitoring desktop app
```

### 2.4 — The Control Tables (The "Brain" of the System)

All ETL configuration lives in **Azure BHG_DR** database tables:

```
CONTROL TABLES IN AZURE BHG_DR:
════════════════════════════════════════════════════════════════════════
ctrl.tbl_Locations       → All 80+ clinic sites (SiteCode, ClinicName)
ctrl.tbl_LocationCons    → Maps sites to source DB names + ActionKeys
ctrl.tbl_Forms2Process   → Maps SAMMS form tables + signature columns
dms.tbl_MapAction        → Maps (ActionKey+StepKey) to source/dest tables
dms.tbl_MapSrc2Dsn       → Maps individual columns for each table
tsk.tbl_Schedule         → The 17 named pipelines/schedules
tsk.tbl_Tasks2           → Daily task log (parent + child tasks)
tsk.tbl_ErrorLog         → AzureAgent error/activity log
```

---

## PART 3 — The 17 Pipelines (What Gets Loaded)

The system has **17 named pipelines** stored in `tsk.tbl_Schedule`. Each pipeline covers a specific set of tables and/or clinics:

```
TIMEZONE-BASED PIPELINES (split by clinic location):
═══════════════════════════════════════════════════════
Pipeline Name        | Arg | Type         | What It Loads
─────────────────────┼─────┼──────────────┼──────────────────────────────────
Eastern ETL P1       |  2  | EST Non-Fin  | Enrollment, Clients, UA Results,
Central ETL P1       |  2  | CST Non-Fin  | Codes, Clinic, Assessments,
Mountain ETL P1      |  2  | MST Non-Fin  | Pre-Admission, etc. (non-billing)
Pacific ETL P1       |  2  | PST Non-Fin  |
                     |     |              |
Eastern ETL P2       |  4  | EST Fin      | Claims, Bills, Check-In,
Central ETL P2       |  4  | CST Fin      | Claim Line Items, Fee Schedules,
Mountain ETL P2      |  4  | MST Fin      | Payor/Client, E&M Forms
Pacific ETL P2       |  4  | PST Fin      | (billing/financial tables)

SPECIALTY PIPELINES (topic-specific, all sites):
═══════════════════════════════════════════════════════
Pipeline Name        | Arg | What It Loads
─────────────────────┼─────┼──────────────────────────────────────────────
SAMMSGlobal          |  1  | Global codes, users, consents, clinic settings
Samms-LAB            |  5  | LAB site only: ClientDemo1, ClientDemo2
Samms-Forms          |  6  | Form Q&A data, form answer signatures
SAMMS-ETL-Notes      |  7  | AR notes, claim notes
SAMMS-ETL-INV        |  8  | Inventory, bottles, assessments (ASAM), appointments
SAMMS-ETL-DartSvc    |  9  | Counseling service records (year-partitioned)
SAMMS-ETL-Dose       | 10  | Medication doses + dose excuses
SAMMS-ETL-Orders     | 11  | Medication orders
PHC ETL              | PHC | PHC-specific clinics (runs via separate PHC.exe)
```

---

## PART 4 — The ActionKey / ActionStepKey System

Every table loaded by the ETL has an `ActionKey` and a `StepKey`. These two numbers together tell the system:
1. Which **column list** to SELECT from the source
2. How to **handle checksum** (insert/update/skip vs full reload)

```
ActionKey | What It Groups         | Checksum?
──────────┼────────────────────────┼──────────
1         | Standard SAMMS tables  | YES (upsert)
2         | Global/shared tables   | YES (upsert)
3         | Bulk reference tables  | NO  (full reload — delete + re-insert)
4         | Financial/billing      | YES (upsert)
5         | PHC/LAB claims variant | YES (upsert)
6         | Another group          | YES (upsert)
7         | Methasoft schema sites | YES (upsert)
```

**How the lookup works:**

```
Child task row has: ActionKey=1, ActionStepKey=9
        │
        ▼
db.WorkToDo.Where(ActionKey=1 AND ActionStepKey=9)
        │
        ▼
Returns rows from dms.tbl_MapSrc2Dsn:
  FieldKey=1  FieldName=uarID        DsnFieldName=uarID       PrimaryKey=1
  FieldKey=2  FieldName=@SiteCode    DsnFieldName=SiteCode    (injected literal)
  FieldKey=3  FieldName=uarResultDt  DsnFieldName=uarResultDt
  FieldKey=4  FieldName=uarDropDt    ...
  ...
        │
        ▼
SelectConstructor builds:
  SELECT [uarID], 'B01' SiteCode, [uarResultDt], [uarDropDt], ...,
         CHECKSUM([uarID], [uarResultDt], ...) RowChkSum
  FROM dbo.tblUAResult
  WHERE convert(date, uarResultDt) >= '3/10/2026'
```

---

## PART 5 — The Complete Daily Timeline

```
════════════════════════════════════════════════════════════════════════
                    COMPLETE DAILY ETL TIMELINE
════════════════════════════════════════════════════════════════════════

 5:15 PM ┌──────────────────────────────────────────────────────────┐
         │  BHG Azure Task Scheduler = Scheduler.exe               │
         │  Runs for ~10 seconds                                    │
         │                                                          │
         │  1. Reads 17 rows from tsk.tbl_Schedule                 │
         │  2. Creates 17 parent task rows in tsk.tbl_Tasks2       │
         │  3. Cross-joins dms.vw_MapAction × parent tasks         │
         │     → creates thousands of child task rows              │
         │     (one per site per table per pipeline)               │
         │  4. Applies skip rules (marks RowState=26 for tasks     │
         │     not valid for certain sites)                        │
         │  5. Updates NextRunTime in tsk.tbl_Schedule +1 day      │
         │  DONE. Closes. Does NOT start BHGTaskRunner.            │
         └──────────────────────────────────────────────────────────┘
              │
              ▼
 7:00 PM  BHG Task Runner Forms  → BHGTaskRunner.exe 6
              Loads: pats.tbl_dbo_FormQuestionAnswers
                     pats.tbl_dbo_FormAnswerSignatures
              How: Loops ctrl.tbl_Forms2Process → UNIONs all form SELECTs
              Duration: ~30 minutes

 8:23 PM  BHG Task Runner  → BHGTaskRunner.exe 2
              Loads: All P1 tables for EST + CST + MST + PST clinics
              Tables: Enrollment, ClientDemo1/2, UAResults, UAResultDetail,
                      Codes, Clinic, Consents, Users, PayerClient, PreAdmission,
                      COWS, Assessments, Services, DartsSrv (P1 only), and more
              Duration: ~4-5 hours (biggest run — 800+ child tasks)

 8:50 PM  BHG Task Runner P2  → BHGTaskRunner.exe 4
              Loads: All P2 financial tables for EST + CST + MST + PST
              Tables: Claims, ClaimLineItem, ClaimLineItemActivity, Bills,
                      CheckIn, GlobalPayor, FeeSchedules, 3pElig, EandMForms
              Duration: ~1-2 hours

 9:36 PM  BHG Task Runner Dose  → BHGTaskRunner.exe 10
              Loads: pats.tbl_Dose, pats.tbl_Dose_Excuse
              How: EF Core upsert by (SiteCode, dtDate, PatientID)
              Duration: ~20-30 minutes

10:01 PM  BHG Task Runner Orders  → BHGTaskRunner.exe 11
              Loads: pats.tbl_Orders_2019 through pats.tbl_Orders_2023
              How: Year-partitioned tables, upserted by OrderDate year
              Duration: ~15-20 minutes

10:10 PM  BHG Task 1 Runner  → BHGTaskRunner.exe 1
              Loads: SAMMSGlobal — global reference data from SAMMSGLOBAL DB
              Tables: GlobalConsents, GlobalUser, GlobalUserSite, GlobalPayer,
                      FormsSAMMSClient, BriefAddictionMonitor, ClinicalOpiateWithdrawalScale
              Duration: ~2 hours

11:50 PM  BHG Task Runner Inv  → BHGTaskRunner.exe 8
              Loads: Inventory, bottles, liquid log, appointments,
                     ALL ASAM assessment tables (Admission, ReAssessment,
                     Dimension 1-6, Periodic Reassessment, PADimensions 1-6)
              Duration: ~1 hour

12:05 AM  BHG Task Runner DartSvc  → BHGTaskRunner.exe 9
              Loads: pats.tbl_DartsSrv_2014 through pats.tbl_DartsSrv_2023
              How: Year-partitioned tables (one table per year)
                   EITHER EF Core upsert OR SqlBulkCopy → stg.DartsSrvMerge SP
              Duration: ~30-60 minutes

 2:29 AM  BHG Task Runner Notes  → BHGTaskRunner.exe 7
              Loads: pats.tbl_3parnote, pats.tbl_3pclaimnote
              How: EF Core upsert by (SiteCode, NoteId)
              Duration: ~15-20 minutes

════════════════════════════════════════════════════════════════════════
  ETL LOADS COMPLETE BY ~3-4 AM
════════════════════════════════════════════════════════════════════════

 6:24 AM  AzureAgent.exe (6:24 AM window)
              → TRUNCATE + reload pats.tbl_vw_ZeroDollarDenials
              → TRUNCATE + reload pats.tbl_vw_SignatureReportSAMMSForms
              → EXEC pats.Populate_BAM_Bucketed
              → TRUNCATE + reload pats.tbl_ServicesMissingSigCode

 6:45 AM  AzureAgent.exe (6:45 AM window)
              → INSERT pba.tbl_vw_CounselorSupervision_KPISite
              → INSERT pba.tbl_vw_CounselorSupervision_KPICounselor
              → TRUNCATE + reload pats.tbl_vw_Treatment_Plan

 7:00 AM  AzureAgent.exe (7:00 AM window)
              → EXEC pats.SP_CounselingStateReq   (trending counseling)
              → EXEC pats.SP_MedInvMerge           (medication inventory merge)

 7:01 AM  AzureAgent 7 am Run (separate Task Scheduler entry)
              → Additional AzureAgent run with 7 AM specific jobs

════════════════════════════════════════════════════════════════════════
  ALL FRESH DATA READY BY ~7-8 AM
  Staff arrive. Reports show today's data across all 80+ clinics.
════════════════════════════════════════════════════════════════════════
```

---

## PART 6 — Deep Dive: What Happens Inside BHGTaskRunner

When `BHGTaskRunner.exe 2` fires at 8:23 PM, here is EXACTLY what happens step by step:

```
STEP 1 — Initialize helpers
═══════════════════════════
SelectConstructor sc  → builds SELECT column lists from metadata
SQLSvrManager sm      → executes SQL on any database (source or Azure)
BulkDartsSvc bldr     → handles SqlBulkCopy bulk loads
BHG_DRContext db      → EF Core connection to Azure BHG_DR
SaveData sd           → contains all upsert Save___() methods


STEP 2 — Load all pending tasks for arg "2"
════════════════════════════════════════════
pTasks = db.VwTaskList
    .Where(x =>
        x.SiteCode != "PHC"       ← PHC handled separately
        && x.Status == 17          ← Status 17 = Pending
        && x.RunAt < DateTime.Now  ← scheduled time has passed
        && (x.TaskName == "Eastern ETL P1"  ||
            x.TaskName == "Central ETL P1"  ||
            x.TaskName == "Mountain ETL P1" ||
            x.TaskName == "Pacific ETL P1"))
    .ToList();
// Returns e.g. 4 parent tasks


STEP 3 — Loop through parent tasks
═══════════════════════════════════
foreach (parent task in pTasks):   // e.g., "Eastern ETL P1"

    Mark parent Status = 18 (Running)
    
    Get ALL child tasks for this parent:
    Tasks.Where(x => x.ParentTaskId == parent.TaskId)
    // Returns hundreds of child tasks:
    // pats.tbl_Enrollment B01, pats.tbl_Enrollment B02,
    // pats.tbl_UAResults B01, pats.tbl_UAResults B02...


STEP 4 — Loop through child tasks
═══════════════════════════════════
foreach (child task in childTasks):  // e.g., "pats.tbl_Enrollment, B01"

    4a. Mark child Status = 18 (Running)

    4b. Load column mapping:
        tdwork = db.WorkToDo
            .Where(ActionKey == st.ActionKey 
                && ActionStepKey == st.ActionStepKey)
            .ToList();
        // Returns list of column rows from dms.tbl_MapSrc2Dsn

    4c. PHC filter (if site is PHC, only use PHC_Enabled columns):
        if (st.SiteCode == "PHC")
            tdwork = tdwork.Where(x => x.PHC_Enabled).ToList();

    4d. Decide checksum mode:
        if (ActionKey == 3) ChkSumEnabled = false;
        else ChkSumEnabled = true;

    4e. Build SELECT column list using SelectConstructor:
        strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, ...)
            .Replace("@SiteCode", "'B01'")
            .Replace("@Samms", "'SAMMS'");

    4f. Build full SQL query:
        strCmd = "SELECT " + strFlds +
                 " FROM dbo.tblEnrollment" +
                 " WHERE LastModAt >= '3/10/2026'" +   ← 15 days back
                 " ORDER BY 1";

    4g. Execute query against SAMMS source (B01's database):
        SrcDt = sm.GetTableData("tblEnrollment", strCmd, st.ConStr);
        // st.ConStr = "Data Source=B01-SQL;Initial Catalog=SAMMS-B01;..."

    4h. Route to the right Save method:
        switch (st.TaskName.ToLower())
        {
            case "pats.tbl_enrollment":
                rCodes = sd.SaveEnrollment(SrcDt, "B01", st.ActionKey, null);
                break;
            case "pats.tbl_uaresults":
                rCodes = sd.SaveUAResults(SrcDt, "B01", WorkDate, false, null);
                break;
            // ...131 cases total...
        }

    4i. Inside SaveEnrollment() — EF Core Upsert:
        List<TblEnrollment> existing = db.TblEnrollment
            .Where(x => x.SiteCode == "B01").ToList();
        
        foreach (DataRow r in SrcDt.Rows):
            int id = int.Parse(r["enrollID"].ToString());
            int newChkSum = int.Parse(r["RowChkSum"].ToString());
            TblEnrollment existingRow = existing.Where(x => x.Id == id).FirstOrDefault();
            
            if (existingRow == null):
                INSERT new row          → RowsIns++
            elif existingRow.RowChkSum != newChkSum:
                UPDATE existing row     → RowsUpd++
            else:
                SKIP (no change)        → no action
        
        db.SaveChanges();  ← commit all inserts/updates in one batch

    4j. Update child task status:
        task.Status = 19 (Done) or 20 (Error)
        task.RowCount = SrcDt.Rows.Count
        task.Duration = "00:00:18"
        task.ErrorMessage = "" (or error text if failed)

→ Move to next child task. Repeat 4a–4j hundreds of times.

Mark parent Status = 19 (Done)
→ Move to next parent task.
```

---

## PART 7 — The Two Write Strategies

Not all tables use the same write method. There are two paths:

### PATH 1 — EF Core Upsert (Insert / Update / Skip)
Used for: Most tables (enrollment, clients, UA results, claims, etc.)

```
Source DataTable
    │
    ▼
foreach row:
    Check RowChkSum against existing Azure row
    ├── Row doesn't exist → INSERT
    ├── Checksum different → UPDATE
    └── Checksum same → SKIP (no database write needed)
    │
    ▼
db.SaveChanges()   ← one commit for all rows
```

**Advantage:** Efficient — only writes rows that actually changed.  
**Used for:** ~100+ tables via individual `Save___.cs` methods.

### PATH 2 — SqlBulkCopy + Staging + Stored Procedure
Used for: High-volume tables (DartsSrv, FormsSAMMSClient, FormQA)

```
Source DataTable (thousands of rows)
    │
    ▼
SqlBulkCopy.WriteToServer()
    → bulk inserts into stg.tbl_dartssrv (staging table)
    │
    ▼
sm.ExeSqlCmd("exec stg.DartsSrvMerge")
    → stored procedure runs MERGE statement:
       WHEN MATCHED AND changed → UPDATE in pats.tbl_DartsSrv_202X
       WHEN NOT MATCHED → INSERT into pats.tbl_DartsSrv_202X
```

**Advantage:** Much faster for large datasets (bulk insert + single SP call).  
**Used for:** DartsSrv (10,000s of rows per site), Forms data.

---

## PART 8 — AzureAgent: The Post-Processing Layer

`AzureAgent.exe` is a **continuously running** process. It checks the clock every loop and fires specific SQL when the clock matches a target window:

```
AzureAgent.exe running loop:
    │
    ├── Clock = 2:24-2:26 AM?
    │   └── TRUNCATE + reload ayx.tbl_Transactions
    │       (financial transaction summary from ayx.vw_Transactions)
    │
    ├── Clock = 6:24-6:26 AM?
    │   ├── TRUNCATE + reload pats.tbl_vw_ZeroDollarDenials
    │   ├── TRUNCATE + reload pats.tbl_vw_SignatureReportSAMMSForms
    │   ├── EXEC pats.Populate_BAM_Bucketed
    │   └── TRUNCATE + reload pats.tbl_ServicesMissingSigCode
    │
    ├── Clock = 6:45-6:48 AM?
    │   ├── INSERT pba.tbl_vw_CounselorSupervision_KPISite
    │   ├── INSERT pba.tbl_vw_CounselorSupervision_KPICounselor
    │   └── TRUNCATE + reload pats.tbl_vw_Treatment_Plan
    │
    └── Clock = 7:00-7:10 AM?
        ├── EXEC pats.SP_CounselingStateReq  → trending counseling data
        └── EXEC pats.SP_MedInvMerge         → medication inventory merge
```

**Important:** AzureAgent does NOT extract from SAMMS. It only works **inside Azure BHG_DR**, computing derived/aggregated tables from data already loaded by BHGTaskRunner.

---

## PART 9 — ETLMgr: The Monitoring Desktop App

`ETLMgr.exe` is a **Windows Forms desktop application** that connects to Azure BHG_DR and shows task status in a grid. The team uses it to monitor the ETL.

```
ETLMgr Screen shows:
═══════════════════════════════════════════════════════════════════════
TaskId | TaskName      | RunAt    | Status     | Duration | RowCount |
       |               |          |            |          | Remaining|
───────┼───────────────┼──────────┼────────────┼──────────┼──────────┤
613247 | Central ETL   | 8:02 PM  | Completed  | 02:59:18 | 0        |
613246 | Eastern ETL   | 8:01 PM  | Completed  | 04:48:44 | 0        |  ← 1 Failed
613248 | Mountain ETL  | 8:03 PM  | Completed  | 01:02:29 | 0        |
613249 | Pacific ETL   | 8:04 PM  | Completed  | 00:09:21 | 0        |
613245 | SAMMSGlobal   | 10:00 PM | Completed  | 02:12:09 | 0        |
613250 | PHC ETL       | 6:06 PM  | Pending    | 0        | 29       |
═══════════════════════════════════════════════════════════════════════

Status codes:
  17 = Pending    (waiting to run)
  18 = Processing (currently running)
  19 = Completed  (success)
  20 = Error      (failed — check ErrorMessage column)
```

**SQL the monitor runs internally:**
```sql
SELECT TaskId, TaskName, RunAt,
    TaskStatus = CASE
        WHEN Status=17 THEN 'Pending'
        WHEN Status=18 THEN 'Processing'
        WHEN Status=19 THEN 'Completed'
        WHEN Status=20 THEN 'Error'
    END,
    Duration, SiteCode, WorkDate, RowCount,
    Remaining = (SELECT COUNT(1) FROM tsk.tbl_Tasks2 
                 WHERE ParentTaskId = o.TaskId AND Status = 17),
    Failed    = (SELECT COUNT(1) FROM tsk.tbl_Tasks2 
                 WHERE ParentTaskId = o.TaskId AND Status = 20)
FROM tsk.tbl_Tasks2 o
WHERE RowState = 24 AND Status IN (17,18,19) AND ParentTaskId IS NULL
ORDER BY LastModAt DESC
```

---

## PART 10 — Error Handling & Recovery

### When a child task fails (Status = 20):

```
Cause: Network timeout, SAMMS view has self-reference, duplicate key, etc.
Example error: "View or function 'rpt_physicals_due_v' contains a self-reference"

How to fix:
  declare @wrkDt date = '3/25/2026';
  -- Reset failed child tasks to Pending
  UPDATE tsk.tbl_Tasks2 SET Status = 17
  WHERE WorkDate = @wrkDt AND Status = 20;
  -- Reset parent task to Pending so BHGTaskRunner picks it up again
  UPDATE tsk.tbl_Tasks2 SET Status = 17
  WHERE TaskId IN (SELECT ParentTaskId FROM tsk.tbl_Tasks2 
                   WHERE WorkDate = @wrkDt AND Status = 20);
```

BHGTaskRunner will pick up those Status=17 tasks on its next check cycle.

### When a parent task never ran (full day missed):

Scheduler.exe has a built-in cleanup:  
On the next day's run, it removes all stale tasks where `WhereCondition = '1 = 1'` (full reload tables) and recreates them fresh. Tasks with date-filtered WHERE conditions stay and can be re-run.

### Monitoring queries (from official doc):
```sql
-- See parent task summary (ParentTaskView)
SELECT * FROM tsk.ParentTaskView

-- See all pending/failed child tasks from last 2 days
SELECT * FROM tsk.vwTaskList
WHERE RowState = 24 AND Status <> 19 AND RunAt >= GetDate()-2
ORDER BY ParentTaskId, WorkDate, TaskName, SiteCode
```

---

## PART 11 — Adding / Removing / Modifying Tables (Official Process)

### To ADD a new table to the ETL:

```
Step 1: CREATE the table in Azure BHG_DR
        CREATE TABLE pats.tbl_NewTable (...)

Step 2: CREATE C# model class
        BCAppCode\BHG-DR-LIB\Models\TblNewTable.cs
        (EF Core entity with [Table], [Key], [Column] attributes)

Step 3: CREATE Save method
        BCAppCode\BHG-DR-LIB\SaveNewTable.cs
        (EF Core upsert: foreach row → INSERT or UPDATE or SKIP)

Step 4: ADD case to BHGTaskRunner switch statement
        BCAppCode\BHGTaskRunner\Program.cs
        case "pats.tbl_newtable":
            rCodes = sd.SaveNewTable(SrcDt, st.SiteCode, null);
            break;

Step 5: INSERT metadata rows in Azure BHG_DR
        -- Map the table to an ActionKey + StepKey
        INSERT INTO dms.tbl_MapAction
        (ActionKey, StepKey, Enabled, SrcSchema, DsnSchema, FromTblVw, DsnTbl, WhereCondition...)
        VALUES (1, 86, 1, 'dbo', 'pats', 'tblNewSource', 'tbl_NewTable', '...')
        
        -- Map each column
        INSERT INTO dms.tbl_MapSrc2Dsn
        (ActionKey, ActionStepKey, FieldKey, FieldName, Enabled, FieldType...)
        VALUES (1, 86, 1, 'NewColumn', 1, 'varchar', 100...)

Step 6: RECOMPILE, TEST, COPY EXE
        Rebuild solution → copy BHGTaskRunner.exe to:
        C:\users\bcatellier\Documents\BCApps\
```

### To REMOVE a table:
```sql
-- Soft disable (recommended):
UPDATE dms.tbl_MapAction SET Enabled = 0 WHERE ActionKey = 1 AND StepKey = 86;
-- Hard remove: delete rows + recompile
```

### To MODIFY a table (add/remove column):
```
1. ALTER TABLE in Azure BHG_DR
2. Update Model class (.cs)
3. Update Save method (.cs)
4. UPDATE dms.tbl_MapSrc2Dsn SET Enabled = 1/0 WHERE ...
5. Recompile + copy exe
```

---

## PART 12 — The Complete Architecture Diagram

```
════════════════════════════════════════════════════════════════════════
                    BHG ETL SYSTEM ARCHITECTURE
════════════════════════════════════════════════════════════════════════

  SERVER: 10.50.5.223
  ┌─────────────────────────────────────────────────────────────────┐
  │  Windows Task Scheduler                                         │
  │  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐   │
  │  │ Scheduler.exe│  │BHGTaskRunner │  │  AzureAgent.exe    │   │
  │  │  5:15 PM     │  │  .exe 1-11   │  │ 6:24/6:45/7:00 AM  │   │
  │  │  ~10 seconds │  │  7PM - 2:29AM│  │  Post-processing   │   │
  │  └──────┬───────┘  └──────┬───────┘  └────────┬───────────┘   │
  │         │                 │                    │               │
  │  ┌──────▼─────────────────▼────────────────────▼──────────┐   │
  │  │              BHG_DR_LIB (Shared Library)                │   │
  │  │  SelectConstructor  SQLSvrManager  SaveData  BulkSvc    │   │
  │  └──────────────────────────────────────────────────────────┘   │
  │                                                                 │
  │  ETLMgr.exe  ← Monitoring desktop app (manual use)             │
  └─────────────────────────────────────────────────────────────────┘
           │                    │                    │
           │ reads config       │ reads/writes       │ writes
           ▼                    ▼                    ▼
  ┌──────────────────────────────────────────────────────────────┐
  │              AZURE SQL SERVER                                 │
  │         bhgazuresql01.database.windows.net                   │
  │              Database: BHG_DR                                │
  │                                                              │
  │  CONTROL/METADATA TABLES:    ETL DATA TABLES:               │
  │  tsk.tbl_Schedule            pats.*  (patient/clinical)      │
  │  tsk.tbl_Tasks2   ←──────→  ctrl.*  (config/reference)      │
  │  tsk.tbl_ErrorLog            ayx.*   (analytics prep)        │
  │  ctrl.tbl_Locations          stg.*   (staging/bulk)          │
  │  ctrl.tbl_LocationCons       pba.*   (performance analytics) │
  │  ctrl.tbl_Forms2Process                                      │
  │  dms.tbl_MapAction                                           │
  │  dms.tbl_MapSrc2Dsn                                          │
  └──────────────────────────────────────────────────────────────┘
           ▲
           │ ETL extracts from SAMMS sources
           │
  ┌────────┴─────────────────────────────────────────────────────┐
  │              80+ SAMMS SOURCE DATABASES                       │
  │                                                              │
  │  On-premise SQL Servers (clinic machines):                   │
  │  SAMMS-ColoradoSpringsV5   SAMMS-NashvilleV5                 │
  │  SAMMS-LawrenceV5          SAMMS-ArkansasV5                  │
  │  ... 70+ more SAMMS databases ...                            │
  │                                                              │
  │  Netalytics cloud (Methasoft schema):                        │
  │  Methasoft_BHG_Lawrence    Methasoft_CBH_DesertInn           │
  │  ... more Methasoft databases ...                            │
  │                                                              │
  │  Shared global database:                                     │
  │  SAMMSGLOBAL  (forms, global codes, global users)            │
  └──────────────────────────────────────────────────────────────┘
```

---

## PART 13 — Quick Reference Summary

| Question | Answer |
|---|---|
| What is BHG ETL? | Nightly system that copies patient data from 80+ clinic DBs to one Azure DB |
| Where does it run? | Server 10.50.5.223, exe at `C:\users\bcatellier\Documents\BCApps` |
| How many pipelines? | 17 named schedules in `tsk.tbl_Schedule` |
| How many TaskRunner calls? | 11 (one per argument, same executable) |
| When does Scheduler run? | 5:15 PM every day (~10 seconds) |
| When does ETL run? | 7:00 PM to ~3:00 AM |
| When is data ready? | By 7-8 AM when staff arrive |
| What is arg 3? | Disabled in Task Scheduler |
| How are columns defined? | In `dms.tbl_MapSrc2Dsn` — one row per column |
| How are tables defined? | In `dms.tbl_MapAction` — one row per table per site |
| What is ActionKey? | Groups tables into ETL categories (1=standard, 3=bulk/no-checksum, etc.) |
| What is ActionStepKey? | Identifies the specific table within an ActionKey group |
| How does upsert work? | RowChkSum — if checksum differs → update, if new → insert, if same → skip |
| What is BulkDartsSvc? | SqlBulkCopy path for high-volume tables → stg schema → SP merge |
| What does AzureAgent do? | Runs inside Azure only — computes KPIs, treatment plans, zero-dollar denials |
| How to re-run failed tasks? | Set Status=17 for failed tasks + their parent, BHGTaskRunner picks them up |
| How to add a new table? | 6 steps: create table → model → save method → switch case → metadata rows → recompile |
| Status 17 means? | Pending |
| Status 18 means? | Running |
| Status 19 means? | Completed/Success |
| Status 20 means? | Failed/Error |
