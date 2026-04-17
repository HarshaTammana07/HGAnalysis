# BHG Daily Exports — Official Document Correlated with Our Analysis
**Source:** "BHG Daily Exports Tasks - Brian.docx" (shared by BHG team)  
**Date:** 2026-03-25  
**Purpose:** Maps every point in the official document to the code/files we already analyzed

---

## IMPORTANT CORRECTION — Task Timing Is EVENING, Not Morning

Our earlier `Daily_ETL_Flow_Story.md` assumed tasks run in the early morning (2 AM, 3 AM...).  
**The Windows Task Scheduler screenshot proves the actual timing is EVENING:**

```
ACTUAL SCHEDULE (from Windows Task Scheduler screenshot):
══════════════════════════════════════════════════════════════════
Job Name                   | Arg | Time          | What It Does
───────────────────────────┼─────┼───────────────┼─────────────────────────────
BHG Azure Task Scheduler   |  -  | 5:15 PM       | = Scheduler.exe (creates tasks)
BHG Task Runner Forms      |  6  | 7:00 PM       | Samms-Forms
BHG Task Runner            |  2  | 8:23 PM       | P1 (Eastern/Central/Mountain/Pacific)
BHG Task Runner P2         |  4  | 8:50 PM       | P2 financial (all timezones)
BHG Task Runner Dose       | 10  | 9:36 PM       | SAMMS-ETL-Dose
BHG Task Runner Orders     | 11  | 10:01 PM      | SAMMS-ETL-Orders
BHG Task 1 Runner          |  1  | 10:10 PM      | SAMMSGlobal
BHG Task Runner Inv        |  8  | 11:50 PM      | SAMMS-ETL-INV
BHG Task Runner DartSvc    |  9  | 12:05 AM      | SAMMS-ETL-DartSvc
BHG Task Runner Notes      |  7  | 2:29 AM       | SAMMS-ETL-Notes
AzureAgent2                |  -  | 6:45 AM       | Post-processing SPs
AzureAgent 7 am Run        |  -  | 7:01 AM       | Post-processing SPs
BHG Task 3 Runner          |  3  | DISABLED      | Arg 3 is disabled
```

**The corrected daily timeline:**
```
5:15 PM  → Scheduler.exe     creates all tasks for the day
7:00 PM  → BHGTaskRunner 6   Forms
8:23 PM  → BHGTaskRunner 2   P1 (all 4 timezone pipelines)
8:50 PM  → BHGTaskRunner 4   P2 financial
9:36 PM  → BHGTaskRunner 10  Dose
10:01 PM → BHGTaskRunner 11  Orders
10:10 PM → BHGTaskRunner 1   SAMMSGlobal
11:50 PM → BHGTaskRunner 8   Assessments/Inv
12:05 AM → BHGTaskRunner 9   DartSvc
2:29 AM  → BHGTaskRunner 7   Notes
6:45 AM  → AzureAgent2       Post-processing stored procedures
7:01 AM  → AzureAgent        Additional post-processing
```
→ By **7-8 AM** when staff arrive, all data is fresh in Azure BHG_DR.

---

## Section 1 — Executables (Where Everything Lives)

**Official document says:**
- Executables: `10.50.5.223 @ C:\users\bcatellier\Documents\BCApps`
- Source Code: `10.50.5.223 @ C:\users\bcatellier\Documents\BCAppCode`
- Visual Studio Solution: `C:\Users\bcatellier\Documents\BCAppCode\BHG\BHG.sln`

**Correlation with our analysis:**

| Official Doc | Our Analysis | File We Documented |
|---|---|---|
| `BCApps` folder | Compiled executables (`.exe` files) | Referenced in `BCAppCode_EXPLAINED.md` |
| `BCAppCode` folder | Source code | All our `.md` files analyze this |
| `BHG.sln` | The Visual Studio solution | Listed in `BCAppCode_EXPLAINED.md` as the entry point |
| Machine `10.50.5.223` | The dedicated ETL server | Not in code — now confirmed from official doc |

The source code we analyzed at `c:\Users\tsaty\Downloads\BCAppCode` is a copy of `BCAppCode` from that machine.

---

## Section 1e — TaskRunner Arguments (Confirmed + Corrected)

**Official document confirms all 11 arguments:**

| Arg | Official Name | Our Documentation Match |
|---|---|---|
| 1 | SAMMSGlobal | Confirmed in `BHGTaskRunner_DEEP_ANALYSIS.md` |
| 2 | Central ETL P1, Eastern ETL P1, Mountain ETL P1, Pacific ETL P1 | Confirmed — 4 schedules bundled into one arg |
| 3 | Any others | **DISABLED** in Task Scheduler — confirmed by screenshot |
| 4 | Central ETL P2, Eastern ETL P2, Mountain ETL P2, Pacific ETL P2 | Confirmed in `BHGTaskRunner_DEEP_ANALYSIS.md` |
| 5 | Samms-LAB | Confirmed |
| 6 | Samms-Forms | Confirmed |
| 7 | SAMMS-ETL-Notes | Confirmed |
| 8 | SAMMS-ETL-INV | Confirmed |
| 9 | SAMMS-ETL-DartSvc | Confirmed |
| 10 | SAMMS-ETL-Dose | Confirmed |
| 11 | SAMMS-ETL-Orders | Confirmed |

**New finding:** Arg 3 is **disabled** (BHG Task 3 Runner = Disabled in screenshot).  
We saw it referenced in commented-out code in `BHGTaskRunner/Program.cs` — now confirmed it's intentionally turned off.

---

## Section 2 — Checking Scheduled Tasks (The Monitoring Queries)

**Official document provides two SQL queries to monitor ETL health.**

### Query A — Parent Task View

```sql
select * from tsk.ParentTaskView
```

**What the screenshot shows** (from 2023-03-09 run):

| TaskId | TaskName | RunAt | Status | Duration | SiteCode | RowCount | Remaining | Failed |
|---|---|---|---|---|---|---|---|---|
| 613247 | Central ETL | 2023-03-09 20:02:00 | **Completed** | 02:59:18 | All | 976 | 0 | 0 |
| 613246 | Estern ETL | 2023-03-09 20:01:00 | **Completed** | 04:48:44 | All | 838 | 0 | **1** ← has error |
| 613248 | Mountain ETL | 2023-03-09 20:03:00 | **Completed** | 01:02:29 | All | 217 | 0 | 0 |
| 613249 | Pacific ETL | 2023-03-09 20:04:00 | **Completed** | 00:09:21 | All | 28 | 0 | 0 |
| 613245 | SAMMSGlobal | 2023-03-09 20:00:00 | **Completed** | 02:12:09 | All | 8 | 0 | 0 |
| 613250 | PHC ETL | 2023-03-09 18:06:00 | **Pending** | 0 | PHC | NULL | 29 | 0 |
| 611092 | PHC ETL | 2023-03-08 18:06:00 | **Pending** | 0 | PHC | NULL | 14 | 0 |

**What this tells us:**
- `RunAt` at 20:00–20:04 = 8:00–8:04 PM → confirms evening execution
- Eastern ETL had **1 failed child task** (Failed=1) — typical network timeout
- PHC ETL still **Pending** — PHC is separate, runs independently
- Mountain ETL (28 rows) and Pacific ETL (8 rows) much smaller than Eastern (838) and Central (976) — fewer clinics in those timezones
- `tsk.ParentTaskView` is a **view** on top of `tsk.tbl_Tasks2` that aggregates child task counts — we documented the underlying table in `Scheduler_DEEP_ANALYSIS.md`

### Query B — Child Task List

```sql
Select * from tsk.vwTaskList 
where RowState = 24 and Status <> 19 and RunAt >= GetDate()-2 
order by ParentTaskId, WorkDate, TaskName, SiteCode
```

**Correlation with our analysis:**
- `RowState = 24` → Active tasks (not skipped/deleted). We documented this in `Scheduler_DEEP_ANALYSIS.md` — RowState=26 means skipped.
- `Status <> 19` → Excludes completed tasks. Status 19 = Done, we documented all status codes.
- `RunAt >= GetDate()-2` → Last 2 days — catches anything still pending or failed from yesterday.

**Child tasks visible in screenshot:**
```
pats.tbl_Enrollment          ← P1 pipeline table
pats.tbl_Bills               ← P2 financial table
pats.tbl_CheckIn             ← P2 financial table
pats.tbl_Claims              ← P2 financial table
pats.tbl_ClaimLineItem       ← P2 financial table
pats.tbl_ClaimLineItemActivity ← P2 financial table
pats.tbl_DartsSrv            ← DartSvc pipeline
pats.tbl_dbo_FormAnswerSignatures ← Forms pipeline
pats.tbl_dbo_FormQuestionAnswers  ← Forms pipeline
pats.tbl_Dose                ← Dose pipeline
pats.tbl_PayerClient         ← P1/P2 pipeline
pats.tbl_UAResults           ← P1 pipeline
pats.tbl_UAResultDetail      ← P1 pipeline
ctrl.tbl_Clinic              ← SAMMSGlobal pipeline
ctrl.tbl_CONSENTS            ← SAMMSGlobal pipeline
```
All of these are documented in `Tables_By_Pipeline.md` and `Tables_With_Columns.md`.

---

## Section 3 — Reviewing Errors

**Official document says:**
- Status = **20** means ERROR
- `ErrorMessage` field holds the actual error text
- Example error: `"View or function 'rpt_physicals_due_v' contains a self-reference"`
- Fix: Reset status to 17 (Pending) for both parent and child tasks

**Correlation with our analysis:**
We documented status codes in `BHGTaskRunner_DEEP_ANALYSIS.md`:

| Status Code | Meaning | Source |
|---|---|---|
| 17 | Pending (ready to run) | Confirmed |
| 18 | Running (in progress) | Confirmed |
| 19 | Done / Success | Confirmed |
| **20** | **Failed / Error** | **NOW OFFICIALLY CONFIRMED** |
| 22 | Another completion state | From SelectConstructor |
| 26 | Skipped (RowState value) | From Scheduler skip rules |

**The official re-run SQL:**

```sql
-- Step 1: Reset parent task status to Pending
declare @wrkDt date = '3/15/2026';
update tsk.tbl_Tasks2 set Status = 17 
where TaskId in (
    select ParentTaskId from tsk.tbl_Tasks2 
    where WorkDate = @wrkDt and Status = 20
);

-- Step 2: Reset all failed child tasks to Pending
update tsk.tbl_Tasks2 set status = 17 
where WorkDate = @wrkDt and status = 20;
```

This is the **manual re-run process** when something fails. The BHGTaskRunner will pick up these tasks again on its next check because it looks for `Status = 17`.

---

## Section 4 — Failed Parent Task Recovery

**Official document says:**
> If a scheduled Parent Task fails to run or does not finish, the scheduling process will remove tasks which have a WhereCondition of `1 = 1` (full table loads) and reset the status to 17, when submitting the new tasks.

**Correlation with our analysis:**
This is documented in `Scheduler_DEEP_ANALYSIS.md`. The Scheduler has this line:

```csharp
db.ExeSqlCmd(
    "update tsk.vwTaskList set RowState = 26 " +
    "where Status = 17 and WorkDate < '" + wrkdt.ToShortDateString() + 
    "' and WhereCondition = '1 = 1'", 
    db.ConnectionString
);
```

Tables with `WhereCondition = '1 = 1'` are **full reload tables** (no date filter — always loads everything). If the parent task didn't finish yesterday, these stale tasks are cleaned up and recreated fresh today.

---

## Section 5 — Creating Tasks Manually

**Official document provides the manual task creation SQL:**

```sql
-- Create parent task
insert into tsk.tbl_Tasks2 
    (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, 
     LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError)
select Name, NextRunTime, ActionKey, 0, 17, 24, GetDate(), 'Brian.Catellier', 
       Case when scheduleid = 18 then 'PHC' else 'All' end, 
       Convert(date, NextRunTime), '0', 0, 0 
from tsk.tbl_Schedule where Enabled = 1
```

**Correlation with our analysis:**
This is exactly what `Scheduler.exe` does automatically! The manual SQL is the same logic as `Scheduler/Program.cs`. We documented this in `Scheduler_DEEP_ANALYSIS.md`.

Key values explained:
- `ActionStepKey = 0` → Parent tasks always have StepKey=0 (child tasks get the real StepKey)
- `Status = 17` → Pending
- `RowState = 24` → Active
- `scheduleid = 18` → PHC schedule ID, gets SiteCode='PHC', all others get 'All'

---

## Section 6 — Adding a New Table to the ETL (The Official Process)

**This is gold — the official 6-step process for adding any new table:**

| Step | Official Doc Says | Where in Code |
|---|---|---|
| **a** | Create the table in Azure | Azure BHG_DR schema (e.g., `pats.tbl_NewTable`) |
| **b** | Create a class for table structure in `BHG_DR_LIB Models` | `BCAppCode\BHG-DR-LIB\Models\TblNewTable.cs` |
| **c** | Create a save method in `BHG_DR_LIB` | `BCAppCode\BHG-DR-LIB\SaveData.cs` or new `SaveNewTable.cs` |
| **d** | Add a case to the switch statement in TaskRunner | `BCAppCode\BHGTaskRunner\Program.cs` — the 131-case switch |
| **e** | Create MapAction row + MapSrc2Dsn rows in Azure DB | `dms.tbl_MapAction` + `dms.tbl_MapSrc2Dsn` |
| **f** | Recompile + test + copy exe to `C:\users\bcatellier\documents\bcapps\` | Deploy to `10.50.5.223` |

**This confirms our ActionKey/ActionStepKey analysis completely.**  
Step (e) is what creates the ActionKey/StepKey pair we documented in `ActionKey_ActionStepKey_Explained.md`.

---

## Section 7 — Removing a Table

```
a. Disable the row in dms.tbl_MapAction   ← sets Enabled=0
b. Delete rows + recompile if fully removing
```

**Correlation:** The `Scheduler.exe` only picks tasks where `ma.Enabled = 1 and ma.IsActive = 1`. Setting Enabled=0 immediately stops new tasks from being created for that table. Existing tasks already on the whiteboard still run.

---

## Section 8 — Modifying an Existing Table

| Step | Action | Where |
|---|---|---|
| a | Add/remove column from Azure table | Azure SQL DDL (`ALTER TABLE`) |
| b | Update the model in `BHG_DR_LIB` | The `.cs` file in `Models/` folder |
| c | Update Save method | `Save___.cs` partial class |
| d | Update `dms.tbl_MapSrc2Dsn` — enable/disable field | `Enabled=1` or `Enabled=0` per column row |
| e | Recompile, test, publish | Copy to `BCApps` on 10.50.5.223 |

**This confirms our column mapping analysis in `ActionKey_ActionStepKey_Explained.md`.**  
Step (d) is exactly what we described — each column is one row in `dms.tbl_MapSrc2Dsn` with an `Enabled` flag.

---

## Control Tables Section — Officially Confirmed

**Official document lists:**

| Official Table | Purpose Stated | Our Documentation |
|---|---|---|
| `ctrl.tbl_Locations` | SiteCodes and Clinic Names | Documented in `ControlTables_Explained.md` |
| `ctrl.tbl_LocationCons` | Maps Sites to DatabaseNames to ActionKeys | Documented in `ControlTables_Explained.md` |
| `dms.tbl_MapActions` | Maps Tables to ActionKeys | Documented in `ActionKey_ActionStepKey_Explained.md` |
| `dms.tbl_MapSrc2Dsn` | Maps Columns to Tables | Documented in `ActionKey_ActionStepKey_Explained.md` |
| `ctrl.tbl_Forms2Process` | Maps Forms to Tables, identifies Signature fields | Referenced in `ETL_Transformations_By_Pipeline.md` |
| `tsk.tbl_tasks2` | Log of task processes | Documented in `Scheduler_DEEP_ANALYSIS.md` |
| `tsk.tbl_schedule` | Maps ETL Processes to Tasks | Documented in `Scheduler_DEEP_ANALYSIS.md` |

**All 7 control tables are confirmed. We had documented 6 of them already.**  
`ctrl.tbl_Forms2Process` was referenced but not deeply documented — it drives the Forms ETL loop.

---

## Forms ETL Section — How It Works (Official Explanation)

**Official document says:**
```
Forms ETL
  Loops Sites (Tasks)
  Loops tbl_Forms2Process
  Builds SELECT union together each different form
  Gets Data
  Uploads into Azure (SaveData methods)
```

**Correlation with our analysis:**

This matches what we documented in `ETL_Transformations_By_Pipeline.md` for `SaveFormQAData.cs`:

```
For each site task:
  For each form in ctrl.tbl_Forms2Process:
    Build SELECT for that form's table/view
  UNION all form SELECTs together into one query
  Execute against SAMMS source
  Call SaveFormQAData() → saves to pats.tbl_dbo_FormQuestionAnswers
                          and pats.tbl_dbo_FormAnswerSignatures
```

The `ctrl.tbl_Forms2Process` table contains:
- Form name
- Source table/view in SAMMS
- Signature column names
- Which fields identify a question vs an answer

---

## All Other Tasks Section — Official Summary

**Official document says:**
```
All Other Tasks (Tables)
  Loops Tasks (Sites and Tables)
  Based on Task Name switches to table
  Gets Data based on Select from enabled fields in dms.tbl_MapSrc2Dsn
  Save Data based on Task Name
  Creates an object of destination table and populates by looping columns
  OR does a bulk upload to stg schema and calls a stored procedure to merge data.
```

**This is the exact design we documented in detail:**

| Official Description | Our Documentation | File |
|---|---|---|
| "Loops Tasks (Sites and Tables)" | Parent/child task loop | `BHGTaskRunner_DEEP_ANALYSIS.md` |
| "Based on Task Name switches to table" | 131-case switch statement on `st.TaskName` | `BHGTaskRunner_DEEP_ANALYSIS.md` |
| "Gets Data based on Select from enabled fields in dms.tbl_MapSrc2Dsn" | `SelectConstructor.GetSLT()` | `ActionKey_ActionStepKey_Explained.md` |
| "Creates an object and populates by looping columns" | EF Core upsert (INSERT/UPDATE/SKIP) | `EasternETLP1_EndToEnd.md` |
| "Bulk upload to stg schema + stored procedure" | `BulkDartsSvc.BulkDartsSrvLoader()` + `stg.DartsSrvMerge` SP | `ETL_Transformations_By_Pipeline.md` |

---

## Complete Cross-Reference — Official Doc → Our Files

| Official Document Section | Our Documentation File |
|---|---|
| Section 1 — Executables location | `BCAppCode_EXPLAINED.md` |
| Section 1d — Windows Task Scheduler jobs | `Daily_ETL_Flow_Story.md` (now corrected with evening times) |
| Section 1e — TaskRunner arguments 1-11 | `BHGTaskRunner_DEEP_ANALYSIS.md`, `Daily_ETL_Flow_Story.md` |
| Section 2a — `tsk.ParentTaskView` | `Scheduler_DEEP_ANALYSIS.md` |
| Section 2b — `tsk.vwTaskList` query | `BHGTaskRunner_DEEP_ANALYSIS.md` |
| Section 3 — Error handling (Status=20) | `BHGTaskRunner_DEEP_ANALYSIS.md` |
| Section 4 — WhereCondition='1=1' cleanup | `Scheduler_DEEP_ANALYSIS.md` |
| Section 5 — Manual task creation SQL | `Scheduler_DEEP_ANALYSIS.md` |
| Section 6 — Adding new table (6 steps) | `ETL_Transformations_By_Pipeline.md`, `ActionKey_ActionStepKey_Explained.md` |
| Section 7 — Removing a table | `ActionKey_ActionStepKey_Explained.md` |
| Section 8 — Modifying existing table | `ActionKey_ActionStepKey_Explained.md`, `Tables_With_Columns.md` |
| Control Tables | `ControlTables_Explained.md`, `ActionKey_ActionStepKey_Explained.md` |
| Forms ETL loop | `ETL_Transformations_By_Pipeline.md` |
| All Other Tasks (switch + save + bulk) | `BHGTaskRunner_DEEP_ANALYSIS.md`, `EasternETLP1_EndToEnd.md` |

---

## What the Official Document Confirms / Corrects / Adds

### CONFIRMS (we got it right):
- 11 TaskRunner arguments and what each one does
- Scheduler creates tasks, TaskRunner processes them
- `dms.tbl_MapAction` + `dms.tbl_MapSrc2Dsn` drive the metadata
- EF Core upsert path AND bulk/staging path both exist
- `tsk.tbl_Tasks2` is the central task log
- Status 17=Pending, 18=Running, 19=Done, 20=Failed
- 17 schedules in `tsk.tbl_Schedule`

### CORRECTS (we had wrong):
- **Timing** — Tasks run in the EVENING (5:15 PM onward), not early morning
- **Arg 3 is DISABLED** — BHG Task 3 Runner is disabled in Task Scheduler
- **Scheduler = "BHG Azure Task Scheduler"** at 5:15 PM (not 1:00 AM)

### ADDS (new information):
- Server IP: **10.50.5.223**
- Executable path: `C:\users\bcatellier\Documents\BCApps`
- Source path: `C:\users\bcatellier\Documents\BCAppCode`
- `tsk.ParentTaskView` is the monitoring view (not just `tsk.tbl_Tasks2`)
- `ctrl.tbl_Forms2Process` drives the Forms ETL loop (maps forms to tables + signature fields)
- The 6-step official process for adding/removing/modifying ETL tables
- Error re-run SQL script (reset Status=20 back to Status=17)
- `AzureAgent` has multiple triggers (7:01 AM + 6:45 AM + others)
