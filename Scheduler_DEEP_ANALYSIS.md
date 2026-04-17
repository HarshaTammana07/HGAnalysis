# Scheduler — Program.cs Deep Analysis

> **File:** `BCAppCode\Scheduler\Program.cs`  
> **Size:** 88 lines of C# — but fires **14 SQL statements** in one shot  
> **Purpose:** Runs once per day. Prepares the entire ETL to-do list for all pipelines. Nothing moves data — it only writes instructions into the database that BHGTaskRunner later reads and executes.

---

## Table of Contents

1. [What This File Is & What It Does](#1-what-this-file-is--what-it-does)
2. [The Two Objects It Creates](#2-the-two-objects-it-creates)
3. [The 14 SQL Statements — One by One](#3-the-14-sql-statements--one-by-one)
   - [Statement 1 — Reset Stuck Tasks](#statement-1--reset-stuck-tasks-line-14)
   - [Statement 2 — Archive Stale Old Tasks](#statement-2--archive-stale-old-tasks-line-15)
   - [Statement 3 — Create Parent Tasks](#statement-3--create-parent-tasks-lines-17-19)
   - [Statement 4 — Create Child Tasks (the big one)](#statement-4--create-child-tasks-lines-20-59)
   - [Statement 5 — Bump the Schedule Forward](#statement-5--bump-the-schedule-forward-line-60)
   - [Statements 6–13 — Skip Rules (Site/Table Exclusions)](#statements-613--skip-rules)
   - [Statement 14 — Delete Old & Skipped Tasks](#statement-14--delete-old--skipped-tasks-line-80)
4. [The Batch Assignment Logic — CASE Statement Decoded](#4-the-batch-assignment-logic--case-statement-decoded)
5. [How Parent and Child Tasks Connect](#5-how-parent-and-child-tasks-connect)
6. [All Skip Rules Explained](#6-all-skip-rules-explained)
7. [The P1 vs P2 Split — Which Tables Go Where](#7-the-p1-vs-p2-split--which-tables-go-where)
8. [What the Database Looks Like After Scheduler Runs](#8-what-the-database-looks-like-after-scheduler-runs)
9. [Execution Flow Summary](#9-execution-flow-summary)
10. [Key Design Observations](#10-key-design-observations)

---

## 1. What This File Is & What It Does

The Scheduler is the **starting gun** for every day's ETL. It does not move any data at all — it only writes rows into `tsk.tbl_Tasks2` that tell BHGTaskRunner what to do later.

Think of it this way:

```
Scheduler.exe          BHGTaskRunner.exe (×11)
─────────────          ───────────────────────
Writes to-do list  →   Reads to-do list and does the work
(runs once/day)        (runs 11 times/day with different args)
```

The entire program is **one `Main()` method** that:
1. Sets today's date
2. Creates a SQL connection helper
3. Runs 2 immediate SQL commands
4. Builds one giant SQL string containing 12 more SQL statements
5. Fires that giant string in a single `ExeSqlCmd()` call
6. Exits

Total runtime: probably a few seconds. It is not a long-running process.

---

## 2. The Two Objects It Creates

```csharp
DateTime wrkdt = DateTime.Today;
BHG_DR_LIB.SQLSvrManager db = new BHG_DR_LIB.SQLSvrManager();
```

| Variable | What it is |
|----------|-----------|
| `wrkdt` | Today's date — used as the `WorkDate` for all tasks created today |
| `db` | SQLSvrManager — the ADO.NET helper from BHG-DR-LIB. Uses its built-in connection string to connect to `BHG_DR` on Azure SQL |

No EF Core context. No SaveData. Just raw SQL execution via `db.ExeSqlCmd()`.

---

## 3. The 14 SQL Statements — One by One

### Statement 1 — Reset Stuck Tasks (line 14)

```sql
UPDATE tsk.tbl_Tasks2
SET Status = 17
WHERE Status = 18
```

**What it does:** Finds any task that is still marked as `Status = 18` (Running) and resets it back to `Status = 17` (Pending).

**Why this is needed:** If BHGTaskRunner crashed mid-run yesterday, or the server was rebooted while a task was processing, that task would be left stuck at Status=18 forever. Nothing would ever pick it up again because BHGTaskRunner only looks for Status=17.

This reset at the start of each day is the **crash recovery mechanism**. It ensures every stuck task gets retried today.

> **Note:** This resets ALL running tasks with no filter on date or batch. So if somehow two runners are running simultaneously and one is mid-process, this could incorrectly reset a legitimately running task. There is no lock or concurrency protection here.

---

### Statement 2 — Archive Stale Old Tasks (line 15)

```sql
UPDATE tsk.vwTaskList
SET RowState = 26
WHERE Status = 17
  AND WorkDate < '[today]'
  AND WhereCondition = '1 = 1'
```

**What it does:** Finds any tasks from **previous days** that are still Pending (Status=17) and have a `WhereCondition = '1 = 1'` (a catch-all/wildcard condition), and marks them as Skipped (RowState=26).

**Why:** These are tasks from prior days that were never picked up — perhaps the runner didn't run, or they were generated for a day that passed. Since the WHERE condition is `1 = 1` (no real filter), keeping them as pending would mean BHGTaskRunner might re-run them against stale data. Marking them skipped prevents that.

The `RowState = 26` means "do not process" — BHGTaskRunner's query filters to `RowState = 24` (Active) only.

---

### Statement 3 — Create Parent Tasks (lines 17–19)

```sql
INSERT INTO tsk.tbl_Tasks2
    (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState,
     LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError, Reload)
SELECT
    Name,
    NextRunTime,
    ActionKey,
    0,              -- ActionStepKey = 0 for parent tasks
    17,             -- Status = Pending
    24,             -- RowState = Active
    GetDate(),
    'Brian.Catellier',
    CASE WHEN scheduleid = 18 THEN 'PHC' ELSE 'All' END,
    CONVERT(date, NextRunTime),
    '0',
    0, 0, 0
FROM tsk.tbl_Schedule
WHERE Enabled = 1
```

**What it does:** Reads every enabled entry in `tsk.tbl_Schedule` and creates one **Parent Task row** per schedule entry.

**Key fields:**
| Field | Value | Meaning |
|-------|-------|---------|
| `TaskName` | e.g. `"Eastern ETL P1"` | The batch group name — matches the pipeline arg |
| `RunAt` | `NextRunTime` from schedule | When BHGTaskRunner should pick this up |
| `ActionStepKey` | `0` | Parent tasks always have 0 — only children have real step keys |
| `Status` | `17` | Pending — ready to be picked up |
| `RowState` | `24` | Active |
| `SiteCode` | `'PHC'` if scheduleId=18, else `'All'` | PHC has its own separate schedule entry (#18) |
| `WorkDate` | Same as `RunAt` date | The date this batch is working on |
| `ParentTaskId` | NULL (not in INSERT) | These ARE the parents — no parent above them |

**What `tsk.tbl_Schedule` contains:** One row per named ETL batch (e.g., Eastern ETL P1, Eastern ETL P2, SAMMSGlobal, SAMMS-ETL-Dose, etc.) with the time of day each batch should run.

---

### Statement 4 — Create Child Tasks (lines 20–59)

This is the **most important and complex** statement. It creates one child task row for every combination of (table × site).

```sql
INSERT INTO tsk.tbl_Tasks2
    (ParentTaskId, TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState,
     LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError, Reload)
SELECT
    t.TaskId,                              -- links child to its parent
    ma.DsnSchema + '.' + ma.DsnTbl,        -- e.g. 'pats.tbl_Dose'
    t.RunAt,                               -- same run time as parent
    ma.ActionKey,
    ma.StepKey,
    17,                                    -- Pending
    24,                                    -- Active
    GetDate(),
    'Brian.Catellier',
    ma.SiteCode,                           -- e.g. 'NYC01', 'CHI02'
    t.WorkDate,
    '0', 0, 0, 0
FROM dms.vw_MapAction ma
CROSS JOIN tsk.tbl_Tasks2 t
WHERE ma.Enabled = 1
  AND ma.IsActive = 1
  AND ma.ConnectionID <> 3                 -- exclude a specific connection type
  AND t.Status = 17
  AND t.WorkDate = CONVERT(date, '[today]')
  AND [CASE statement = t.TaskName]        -- match child to correct parent batch
ORDER BY ma.ActionKey, ma.DsnTbl, ma.SiteCode
```

**How the CROSS JOIN works:**
- `dms.vw_MapAction` has one row per (table × site) combination — this is the ETL mapping config
- `tsk.tbl_Tasks2` at this point contains the parent tasks just inserted in Statement 3
- The CROSS JOIN produces every possible combination of (mapping row × parent task row)
- The `WHERE` clause + `CASE = t.TaskName` filters it down to only valid matches — each child row only links to the one parent whose batch name matches

**What `dms.vw_MapAction` contains:** The complete ETL configuration — for every site (SiteCode) and every destination table (DsnSchema.DsnTbl), it knows:
- Which source table/view to pull from
- Which connection to use
- Which timezone the site is in
- ActionKey and StepKey (links to column mapping in `dms.vw_MapSrc2Dsn`)

**`ConnectionID <> 3`:** Explicitly excludes one connection type from getting child tasks. Connection 3 is likely a special/test connection or a connection type that is not ETL-able.

---

### Statement 5 — Bump the Schedule Forward (line 60)

```sql
UPDATE tsk.tbl_Schedule
SET NextRunTime = DATEADD(d, 1, NextRunTime),
    LastRunTime = DATEADD(d, 1, LastRunTime)
WHERE Enabled = 1
```

**What it does:** Adds 1 day to both `NextRunTime` and `LastRunTime` for every enabled schedule entry.

**Why:** This ensures the next time Scheduler runs (tomorrow), it picks up tomorrow's date as the run time. The schedule is self-advancing — no manual date updates needed.

**Important:** This runs as part of the same big SQL string as Statement 4, so it fires right after children are created. If the Scheduler crashes before this runs, tomorrow's Scheduler would re-insert today's tasks with today's time (which would likely be in the past and get immediately picked up or archived by Statement 2).

---

### Statements 6–13 — Skip Rules

These are 8 UPDATE statements that mark specific task rows as `RowState = 26` (Skipped). They run immediately after child tasks are created, so they mark rows that should NOT be processed.

See Section 6 for the full breakdown of each skip rule.

---

### Statement 14 — Delete Old & Skipped Tasks (line 80)

```sql
DELETE FROM tsk.tbl_Tasks2
WHERE RunAt <= DATEADD(m, -3, CONVERT(date, GetDate()))
   OR RowState = 26
```

**What it does:** Two things in one DELETE:

1. **Age-out old tasks:** Any task with `RunAt` older than 3 months is deleted — keeps the task table from growing forever
2. **Clean up skipped tasks:** Any task just marked as RowState=26 (by statements 6–13 above) is immediately deleted — no point keeping them

**Why delete instead of just leaving RowState=26?** The task table needs to stay clean. Accumulating months of skipped tasks would make queries slower and ETLMgr monitoring harder to read.

---

## 4. The Batch Assignment Logic — CASE Statement Decoded

The CASE statement in Statement 4 is the **core routing logic** of the entire ETL system. It decides which parent batch each child task belongs to. Written out clearly:

```
PHC site (any table)                                    → 'PHC ETL'
LAB site + ClientDemo1 or ClientDemo2                   → 'Samms-LAB'
FormAnswerSignatures or FormQuestionAnswers (any site)  → 'Samms-Forms'
3parnote or 3pclaimnote (any site)                     → 'SAMMS-ETL-Notes'

These specific tables (any site):
  Bottle, LiquidLog, InvType, OrientationChecklist,
  LabResult, LabResultDetail, Appointments,
  AdmissionAssessment, AdmissionAssessmentSummary,
  ReAssessment, (all ReAssessment sub-dimensions),
  (all AdmissionAssessment sub-dimensions)             → 'SAMMS-ETL-Inv'

DartsSrv (any site)                                    → 'SAMMS-ETL-DartSvc'
Dose (any site)                                        → 'SAMMS-ETL-Dose'
Dose_Excuse (any site)                                 → 'SAMMS-ETL-Dose'
Orders (any site)                                      → 'SAMMS-ETL-Orders'

EST timezone + NOT in financial list                   → 'Eastern ETL P1'
EST timezone + IN financial list                       → 'Eastern ETL P2'
CST timezone + NOT in financial list                   → 'Central ETL P1'
CST timezone + IN financial list                       → 'Central ETL P2'
MST timezone + NOT in financial list                   → 'Mountain ETL P1'
MST timezone + IN financial list                       → 'Mountain ETL P2'
PST timezone + NOT in financial list                   → 'Pacific ETL P1'
PST timezone + IN financial list                       → 'Pacific ETL P2'

Everything else                                        → 'SAMMSGlobal'
```

**The CASE evaluates top-to-bottom.** This means:
- PHC site always wins — even if a table would match a timezone batch
- Table-type-specific batches (Forms, Notes, INV, Dose, Orders) always win over timezone batches
- Timezone batches (Eastern P1/P2, etc.) only apply to tables that don't match anything above
- `SAMMSGlobal` is the catch-all for anything not explicitly routed

**The matching mechanism:**
The CASE result is compared against `t.TaskName` (the parent task's name). So a child row is only inserted if the CASE result exactly matches a parent task that exists in the table. This means if a parent task row doesn't exist for a given batch name, those children simply won't be inserted.

---

## 5. How Parent and Child Tasks Connect

After Statements 3 and 4 run, `tsk.tbl_Tasks2` contains:

```
tsk.tbl_Tasks2 AFTER SCHEDULER RUNS:

TaskId  ParentTaskId  TaskName                SiteCode  Status  RowState
──────  ────────────  ──────────────────────  ────────  ──────  ────────
  100   NULL          Eastern ETL P1          All       17      24      ← Parent
  101   NULL          Eastern ETL P2          All       17      24      ← Parent
  102   NULL          SAMMS-ETL-Dose          All       17      24      ← Parent
  103   NULL          SAMMS-ETL-Orders        All       17      24      ← Parent
  ...   NULL          (more parents)

  201   100           pats.tbl_ClientDemo1    NYC01     17      24      ← Child of 100
  202   100           pats.tbl_ClientDemo1    NYC02     17      24      ← Child of 100
  203   100           pats.tbl_Appointments   NYC01     17      24      ← Child of 100
  204   101           pats.tbl_claims         NYC01     17      24      ← Child of 101
  205   101           pats.tbl_Bills          NYC01     17      24      ← Child of 101
  206   102           pats.tbl_Dose           NYC01     17      24      ← Child of 102
  207   102           pats.tbl_Dose           NYC02     17      24      ← Child of 102
  ...
```

**The link:** Child rows have `ParentTaskId` set to the parent's `TaskId`. BHGTaskRunner uses this to loop: find the parent → find all children with that ParentTaskId → process each child.

---

## 6. All Skip Rules Explained

After creating all child tasks, the Scheduler immediately marks certain combinations as Skipped (RowState=26). Here is every skip rule:

| # | SQL Statement | What Gets Skipped | Why |
|---|--------------|------------------|-----|
| 1 | Line 61–63 | `pats.tbl_BriefAddictionMonitor` for PHC site | PHC's BAM data is pulled via a hardcoded PHCSQLVM connection directly in BHGTaskRunner — cannot be driven by standard task mapping |
| 2 | Line 61–63 | `pats.tbl_clinicalopiatewithdrawalscale` for PHC site | Same reason — PHC COWS uses hardcoded PHCSQLVM connection |
| 3 | Line 61–63 | `pats.tbl_vw3pBillSub` for PHC site | 3rd party billing submission not supported for PHC |
| 4 | Line 64 | `pats.tbl_PayerClient` for LAB site where ActionKey=1 AND ActionStepKey=6 | This specific mapping step is not valid for LAB site — LAB uses a different payer schema variant |
| 5 | Line 65 | `pats.tbl_Cows_V6` for PHC site where ActionKey=1 AND ActionStepKey=23 | PHC does not have the SF_COWS (SalesForce COWS V6) table |
| 6 | Line 66 | `ayx.tbl_PreAdmission_V6` for PHC and LAB sites | Both PHC and LAB use an older pre-admission schema — V6 table doesn't exist there |
| 7 | Line 67 | `pats.tbl_EandMFormMDM` for PHC and LAB | E&M Medical Decision Making form not used at PHC or LAB |
| 8 | Line 68 | `pats.tbl_EandMFormPregnancy` for PHC and LAB | E&M Pregnancy form not supported at PHC or LAB |
| 9 | Line 69 | `pats.tbl_Appointments` for LAB site | LAB site does not use the standard appointments table |
| 10 | Line 70 | `ayx.tbl_PreAdmission_V6` for any site where `SchemaVersion = 'V5'` | V5 SAMMS sites use a completely different pre-admission structure — the V6 table doesn't exist |
| 11 | Line 71 | `pats.Tbl_OrientationChecklistNew` for LAB site | LAB does not use the new orientation checklist format |
| 12 | Lines 72–79 | **30+ assessment tables** for LAB site | LAB uses a completely different assessment structure — none of the standard ASAM/ReAssessment tables exist there |

**The 30+ LAB assessment tables skipped:**
```
AdmissionAssessment, AdmissionAssessmentSummary,
AdmissionAssessmentDimensionOneDisorder, AdmissionAssessmentDimensionFiveSubstanceUse,
AdmissionAssessmentDimensionTwo, AdmissionAssessmentDimensionFour,
AdmissionAssessmentDimensionThree, AdmissionAssessmentDimensionSix,
AdmissionAssessmentSubstanceuseHistory, AssessmentSubstanceuseHistory,
ReAssessment, ReAssessmentOccupational, ReAssessmentFamily, ReAssessmentLegal,
ReAssessmentMentalHealth, ReAssessmentPhysicalHealth, ReAssessmentSubstanceUse,
ReAssessmentSocial, ReAssessmentTreatment,
PADimension1 through PADimension6, AppointmentAttend, PA,
TreatmentLevel, FinancialHardshipApplication, PACounselorReview,
ComprehensiveAssessmentForm, BAMForm, BAMScore, TblDiag10
```

**Why skip instead of not inserting?** The child tasks were already inserted by Statement 4 (because the mapping in `dms.vw_MapAction` still includes these site+table combinations). The skip rules are a post-creation filter — it's easier to insert everything from the mapping and then mark exceptions, rather than encoding all these exclusions into the complex CASE logic of Statement 4.

---

## 7. The P1 vs P2 Split — Which Tables Go Where

The P1/P2 split is the most important routing decision. For every timezone (EST, CST, MST, PST):

**P1 (Phase 1 — runs first) = NON-financial patient data**

The P1 exclusion list (= what goes to P2 instead) is:
```
pats.tbl_claims
pats.tbl_claimlineitem
pats.tbl_claimlineitemactivity
pats.tbl_dose_excuse
pats.tbl_uaresultdetail
pats.tbl_GlobalPayor
pats.tbl_PayerClient
pats.tbl_feesched
pats.tbl_CheckIn
pats.tbl_EandMFormPregnancy
pats.tbl_EandMFormMDM
pats.tbl_Bills
pats.tbl_payerclthistory
pats.tbl_treatmentlevel
pats.tbl_Orders
```
> Also: `DartsSrv` is excluded from P1/P2 entirely (it has its own dedicated batch)

So **P1 gets everything else** — demographics, assessments, enrollment, lab results, codes, config tables, etc.

**P2 (Phase 2 — runs after P1) = financial/billing data**

P2 gets exactly the tables listed above.

**Slight timezone variations:**
- **CST** P1 also excludes `pats.tbl_Enrollment` (goes to P2 for Central)
- **MST** P1 also excludes `pats.tbl_payerclthistory` and `pats.tbl_EandMFormMDM`
- **PST** same as MST

These variations reflect that certain tables behave differently across regions.

**Why this split matters:**
- P1 must complete before P2 can run correctly (billing data references patient/enrollment data)
- Separating them lets ops team confirm patient data is loaded correctly before financial data follows
- If P1 has errors, P2 can be held until they're resolved

---

## 8. What the Database Looks Like After Scheduler Runs

**Before Scheduler:**
```
tsk.tbl_Schedule:
  Name="Eastern ETL P1", NextRunTime="2026-03-25 06:00", Enabled=1
  Name="Eastern ETL P2", NextRunTime="2026-03-25 09:00", Enabled=1
  Name="SAMMS-ETL-Dose", NextRunTime="2026-03-25 07:00", Enabled=1
  ...

tsk.tbl_Tasks2: (only old completed rows from prior days)
```

**After Scheduler:**
```
tsk.tbl_Schedule:
  Name="Eastern ETL P1", NextRunTime="2026-03-26 06:00"  ← bumped +1 day
  Name="Eastern ETL P2", NextRunTime="2026-03-26 09:00"  ← bumped +1 day
  ...

tsk.tbl_Tasks2:
  ── PARENT rows (one per schedule entry) ──────────────────────────
  TaskId=100, TaskName="Eastern ETL P1",   WorkDate=3/25, Status=17, RunAt=06:00
  TaskId=101, TaskName="Eastern ETL P2",   WorkDate=3/25, Status=17, RunAt=09:00
  TaskId=102, TaskName="SAMMS-ETL-Dose",   WorkDate=3/25, Status=17, RunAt=07:00
  TaskId=103, TaskName="SAMMSGlobal",      WorkDate=3/25, Status=17, RunAt=05:00
  ...

  ── CHILD rows (one per table×site, linked to parents) ────────────
  TaskId=201, ParentTaskId=100, TaskName="pats.tbl_ClientDemo1", SiteCode="NYC01", Status=17
  TaskId=202, ParentTaskId=100, TaskName="pats.tbl_ClientDemo1", SiteCode="NYC02", Status=17
  TaskId=203, ParentTaskId=100, TaskName="pats.tbl_Appointments", SiteCode="NYC01", Status=17
  TaskId=204, ParentTaskId=101, TaskName="pats.tbl_claims",       SiteCode="NYC01", Status=17
  TaskId=205, ParentTaskId=102, TaskName="pats.tbl_Dose",         SiteCode="NYC01", Status=17
  TaskId=206, ParentTaskId=102, TaskName="pats.tbl_Dose",         SiteCode="NYC02", Status=17
  ... (potentially thousands of rows)
```

**How many child task rows are created per day?**
Roughly: `number of enabled mappings in dms.vw_MapAction × number of schedule entries` (before skip rules reduce it). With dozens of tables and dozens of sites, this could easily be **thousands of child task rows** per day.

---

## 9. Execution Flow Summary

Here is the exact sequence of what happens when `Scheduler.exe` runs:

```
[START]
    │
    ├─ SET wrkdt = today's date
    ├─ CREATE SQLSvrManager (connects to BHG_DR)
    │
    ├─ SQL #1:  UPDATE Status=18 → Status=17    (reset stuck running tasks)
    ├─ SQL #2:  UPDATE old pending tasks → RowState=26  (archive stale tasks)
    │
    ├─ [Build one big SQL string with 12 more statements]
    │
    ├─ SQL #3:  INSERT parent task rows from tsk.tbl_Schedule
    ├─ SQL #4:  INSERT child task rows from dms.vw_MapAction × tbl_Tasks2
    │             (CASE statement assigns each child to the right batch)
    ├─ SQL #5:  UPDATE tbl_Schedule NextRunTime += 1 day
    │
    ├─ SQL #6:  SKIP PHC BAM, COWS, BillSub
    ├─ SQL #7:  SKIP LAB PayerClient step 6
    ├─ SQL #8:  SKIP PHC COWS_V6 step 23
    ├─ SQL #9:  SKIP PreAdmission_V6 for PHC + LAB
    ├─ SQL #10: SKIP EandMFormMDM for PHC + LAB
    ├─ SQL #11: SKIP EandMFormPregnancy for PHC + LAB
    ├─ SQL #12: SKIP Appointments for LAB
    ├─ SQL #13: SKIP PreAdmission_V6 for V5-schema sites
    │           SKIP OrientationChecklistNew for LAB
    │           SKIP 30+ assessment tables for LAB
    │
    ├─ SQL #14: DELETE tasks older than 3 months OR RowState=26
    │
    └─ [EXIT — entire run takes a few seconds]
```

After the Scheduler exits, the database is fully prepped. BHGTaskRunner invocations throughout the day pick up their respective batches as each batch's `RunAt` time arrives.

---

## 10. Key Design Observations

### The Whole Program is One SQL String
Only 2 lines of C# logic exist (`DateTime wrkdt = DateTime.Today` and the `SQLSvrManager` creation). Everything else is string concatenation building SQL. There is no C# branching, no loops, no error handling — just SQL.

### If Scheduler Fails, the Entire Day is Lost
There is no try/catch anywhere. If any of the 14 SQL statements throws an error (network timeout, SQL syntax error, constraint violation), the C# exception will propagate and the process will crash. Depending on where it crashes:
- If before Statement 5 (schedule bump) — scheduler can be re-run safely today
- If after Statement 5 (schedule already bumped to tomorrow) — re-running would create duplicate tasks for today OR need manual schedule correction

### Statement 4 Uses CROSS JOIN — This Could Be Expensive
The `CROSS JOIN` between `dms.vw_MapAction` and `tsk.tbl_Tasks2` produces every possible combination. The WHERE clause filters it down, but the intermediate result could be large if either table has many rows. This is a design that works fine at current scale but could become slow as the number of sites or tables grows.

### `LastModBy = 'Brian.Catellier'` is Hardcoded
The name of a specific developer is hardcoded as the `LastModBy` value for every task row created. This is fine for tracking but not a great practice — it should be a system/service account name or the machine/process name.

### Skip Rules Are Maintenance Risk
The skip rules in Statements 6–13 encode business knowledge about which sites support which tables. If a new site is added that has the same limitations as LAB, someone must remember to add it to each skip rule. There is no configuration table for this — it's all hardcoded SQL in this file.

### PHC Has Its Own Schedule Entry (ScheduleId = 18)
PHC is handled differently: it gets `SiteCode = 'PHC'` on its parent task instead of `'All'`. BHGTaskRunner explicitly excludes PHC (`x.SiteCode != "PHC"`) — so PHC's parent task is meant to be picked up by a separate PHC runner (the code in the `PHC/` folder).

### `ConnectionID <> 3` Is Unexplained
The WHERE clause filters out mappings with `ConnectionID = 3`. There is no comment explaining what connection type 3 is. It could be a test connection, a disabled connection type, or a special non-ETL-able connection. This is implicit business knowledge embedded in a number.

### The Order Within Statement 4 Matters
The `ORDER BY ma.ActionKey, ma.DsnTbl, ma.SiteCode` at the end of the INSERT statement controls the physical insertion order of child tasks. BHGTaskRunner processes children in `ORDER BY TaskName, SiteCode, FromTblVw` — so the insertion order doesn't directly affect execution order, but it does affect the TaskId sequence which can be useful for debugging.

---

*Analysis based on static code review of Scheduler/Program.cs (88 lines). March 2026.*
