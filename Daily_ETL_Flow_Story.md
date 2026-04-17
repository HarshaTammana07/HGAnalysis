# The Complete Daily ETL Flow — Explained Like a Story
**System:** BCAppCode — BHG Recovery ETL Pipeline  
**Date:** 2026-03-25

---

## The Big Picture First

The whole system has **one purpose**:

> Every night, collect patient data from 80+ clinic databases (SAMMS) scattered across the USA and load it all into one central Azure database (BHG_DR) so reports and analytics can run on it.

There are **3 characters** in this story:

```
CHARACTER 1: Scheduler.exe      → "The Planner"    (runs once, ~1 minute)
CHARACTER 2: BHGTaskRunner.exe  → "The Worker"     (runs 11 times, hours)
CHARACTER 3: tsk.tbl_Tasks2     → "The Whiteboard" (shared to-do list in the DB)
```

They don't talk to each other directly. The only thing they share is **the whiteboard** (the database table).

---

## PHASE 1 — Before Anything Runs (End of Previous Day)

Imagine you are at 11:59 PM. The day is ending.

The whiteboard (`tsk.tbl_Tasks2`) has yesterday's completed tasks. They are old, done, and will be cleaned up soon. There are **no new tasks yet** for today.

```
tsk.tbl_Tasks2 at 11:59 PM:
══════════════════════════════════════════
TaskId | TaskName            | Status | WorkDate
───────┼─────────────────────┼────────┼──────────
10001  | Eastern ETL P1      | DONE   | 3/24/2026  ← yesterday's parent
10002  | pats.tbl_Enrollment | DONE   | 3/24/2026  ← yesterday's child
10003  | pats.tbl_UAResults  | DONE   | 3/24/2026  ← yesterday's child
...thousands more...                                  all from yesterday
```

---

## PHASE 2 — 1:00 AM — Scheduler.exe Runs (The Planner)

At exactly 1:00 AM, an external Windows Task Scheduler fires up `Scheduler.exe`.  
This program runs for about **5-10 seconds** and does ONE thing: **writes today's to-do list on the whiteboard**.

### What Scheduler does inside:

**Step A** — It reads `tsk.tbl_Schedule` to find out what pipelines need to run today:

```
tsk.tbl_Schedule (the pipeline definitions):
═══════════════════════════════════════════════════════
ScheduleId | Name              | ActionKey | NextRunTime
───────────┼───────────────────┼───────────┼────────────
1          | SAMMSGlobal       | 1         | 3/25/2026 2:00 AM
2          | Eastern ETL P1    | 1         | 3/25/2026 3:00 AM
3          | Central ETL P1    | 1         | 3/25/2026 3:00 AM
4          | Mountain ETL P1   | 1         | 3/25/2026 3:00 AM
5          | Pacific ETL P1    | 1         | 3/25/2026 3:00 AM
6          | Eastern ETL P2    | 4         | 3/25/2026 5:00 AM
7          | Central ETL P2    | 4         | 3/25/2026 5:00 AM
8          | Mountain ETL P2   | 4         | 3/25/2026 5:00 AM
9          | Pacific ETL P2    | 4         | 3/25/2026 5:00 AM
10         | Samms-LAB         | 1         | 3/25/2026 6:00 AM
11         | Samms-Forms       | 2         | 3/25/2026 6:30 AM
12         | SAMMS-ETL-Notes   | 1         | 3/25/2026 7:00 AM
13         | SAMMS-ETL-INV     | 2         | 3/25/2026 7:30 AM
14         | SAMMS-ETL-DartSvc | 1         | 3/25/2026 8:30 AM
15         | SAMMS-ETL-Dose    | 1         | 3/25/2026 9:00 AM
16         | SAMMS-ETL-Orders  | 1         | 3/25/2026 9:30 AM
17         | PHC ETL           | 1         | 3/25/2026 3:00 AM
...17 rows total...
```

**Step B** — It creates a **parent task row** for each schedule.  
Think of this as the "header" for each pipeline:

```
tsk.tbl_Tasks2 AFTER STEP B:
══════════════════════════════════════════════════════════════════
TaskId | TaskName           | Status  | WorkDate   | ParentTaskId
───────┼────────────────────┼─────────┼────────────┼─────────────
20001  | SAMMSGlobal        | PENDING | 3/25/2026  | NULL  ← parent
20002  | Eastern ETL P1     | PENDING | 3/25/2026  | NULL  ← parent
20003  | Central ETL P1     | PENDING | 3/25/2026  | NULL  ← parent
20004  | Mountain ETL P1    | PENDING | 3/25/2026  | NULL  ← parent
20005  | Pacific ETL P1     | PENDING | 3/25/2026  | NULL  ← parent
...17 parent rows total...
```

**Step C** — For EACH parent, it creates thousands of **child task rows** — one per clinic per table.  
These come from cross-joining `dms.vw_MapAction` (which knows all site+table combinations) with the parent tasks:

```sql
-- Scheduler SQL (simplified):
INSERT INTO tsk.tbl_Tasks2 (ParentTaskId, TaskName, ActionKey, ActionStepKey, SiteCode ...)
SELECT t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, ma.ActionKey, ma.StepKey, ma.SiteCode ...
FROM dms.vw_MapAction ma
CROSS JOIN tsk.tbl_Tasks2 t
WHERE ma.Enabled = 1 AND ma.IsActive = 1
  AND (schedule routing CASE statement matches t.TaskName)
```

Result:

```
tsk.tbl_Tasks2 AFTER STEP C (showing children of "Eastern ETL P1"):
══════════════════════════════════════════════════════════════════════════════
TaskId | TaskName               | SiteCode | Status  | ParentTaskId
───────┼────────────────────────┼──────────┼─────────┼─────────────
30001  | pats.tbl_Enrollment    | B01      | PENDING | 20002
30002  | pats.tbl_Enrollment    | B02      | PENDING | 20002
30003  | pats.tbl_Enrollment    | B03      | PENDING | 20002
30004  | pats.tbl_UAResults     | B01      | PENDING | 20002
30005  | pats.tbl_UAResults     | B02      | PENDING | 20002
30006  | pats.tbl_ClientDemo1   | B01      | PENDING | 20002
30007  | pats.tbl_ClientDemo2   | B01      | PENDING | 20002
...hundreds more for Eastern ETL P1 alone...
...thousands more for all 17 pipelines combined...
```

**Step D** — Scheduler applies **skip rules** — marks certain tasks as skipped (RowState=26) because they are not applicable to certain sites:

```
Skip rules applied:
  pats.tbl_PayerClient  → skip for LAB sites
  pats.tbl_Cows_V6      → skip for PHC sites
  ayx.tbl_PreAdmission_V6 → skip for PHC and LAB
  pats.tbl_Appointments → skip for LAB
  ... and more
```

**Scheduler.exe finishes and closes.** It does nothing else. It doesn't run BHGTaskRunner. It just wrote the to-do list and left.

---

## PHASE 3 — 2:00 AM — BHGTaskRunner.exe 1 Runs (SAMMSGlobal)

At 2:00 AM, Windows Task Scheduler fires `BHGTaskRunner.exe 1`.

The `1` is the argument. Inside the code:

```csharp
switch (args[0])  // args[0] = "1"
{
    case "1":
        // Find parent tasks named "SAMMSGlobal"
        pTasks = db.VwTaskList
            .Where(x => x.TaskName == "SAMMSGlobal" && x.Status == 17)
            .ToList();
        break;
}
```

It finds the parent task `SAMMSGlobal` (TaskId=20001) and ALL its children.

### Then it loops through every child task one by one:

```
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Processing child task: pats.tbl_Enrollment, SiteCode = B01
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

STEP 1 — Mark task as "RUNNING" (Status = 18) in tsk.tbl_Tasks2

STEP 2 — Look up column mapping from dms.vw_MapSrc2Dsn
         using ActionKey + ActionStepKey from the task row
         → Gets list of columns: [enrollID, SiteCode, enrollDate, ...]

STEP 3 — Build the SELECT query using those columns:
         SELECT [enrollID], 'B01' SiteCode, [enrollDate], ...
                CHECKSUM([enrollID], [enrollDate], ...) RowChkSum
         FROM dbo.tblEnrollment
         WHERE LastModAt >= '3/10/2026'   ← 15 days lookback
         ORDER BY 1

STEP 4 — Connect to B01's SAMMS database (ConStr from the task row)
         Run the query against SAMMS-B01
         Get back a DataTable with e.g. 42 rows of source data

STEP 5 — Route to the right Save method:
         switch(TaskName) → case "pats.tbl_enrollment":
           → calls sd.SaveEnrollment(SrcDt, "B01", ...)

STEP 6 — SaveEnrollment loops through 42 rows one by one:
         Row 1: enrollID=100 → exists in Azure BHG_DR? NO  → INSERT
         Row 2: enrollID=101 → exists? YES, checksum same? YES → SKIP
         Row 3: enrollID=102 → exists? YES, checksum different? → UPDATE
         ...
         db.SaveChanges()  ← commit all inserts/updates to Azure BHG_DR

STEP 7 — Mark task as "DONE" (Status = 19)
         Record row count and duration in tsk.tbl_Tasks2
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
→ Move to next child task... repeat for B02, B03, B04...
→ Then next table... pats.tbl_UAResults for B01, B02, B03...
→ Hundreds of child tasks processed one by one
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
```

This takes **30-60 minutes** depending on how many sites and rows.

---

## PHASE 4 — 3:00 AM — BHGTaskRunner.exe 2 Runs (All P1 Pipelines)

At 3:00 AM, Windows Task Scheduler fires `BHGTaskRunner.exe 2`.

```csharp
case "2":
    // picks up Eastern ETL P1, Central ETL P1, Mountain ETL P1, Pacific ETL P1
    pTasks = db.VwTaskList
        .Where(x => x.Status == 17 &&
                   (x.TaskName == "Eastern ETL P1" ||
                    x.TaskName == "Central ETL P1"  ||
                    x.TaskName == "Mountain ETL P1"  ||
                    x.TaskName == "Pacific ETL P1"))
        .ToList();
```

It picks up ALL FOUR timezone P1 pipelines at once and processes all their children:
- Enrollment, clients, UA results, clinic data, codes, pre-admissions, assessments
- For every EST, CST, MST, and PST clinic

This is the **biggest run** — hundreds of clinics × many tables. Takes **60-90 minutes**.

---

## PHASE 5 — Morning — Args 4 through 11

The same pattern continues. Each time, Windows Task Scheduler fires BHGTaskRunner with the next argument:

```
5:00 AM → BHGTaskRunner.exe 4   → All P2 financial (claims, bills, check-in)
~6:00 AM → BHGTaskRunner.exe 5  → Samms-LAB
~6:30 AM → BHGTaskRunner.exe 6  → Samms-Forms (form Q&A)
~7:00 AM → BHGTaskRunner.exe 7  → Notes (AR notes, claim notes)
~7:30 AM → BHGTaskRunner.exe 8  → Inv/Assessments (inventory, ASAM assessments)
~8:30 AM → BHGTaskRunner.exe 9  → DartSvc (counseling services)
~9:00 AM → BHGTaskRunner.exe 10 → Dose (medication doses)
~9:30 AM → BHGTaskRunner.exe 11 → Orders (medication orders)
```

Each run:
1. Reads only ITS assigned pending tasks from the whiteboard
2. Processes them (extract from SAMMS → transform → load to Azure BHG_DR)
3. Marks them done

---

## PHASE 6 — Morning — AzureAgent.exe Runs Post-Processing

After the main ETL loads are done, `AzureAgent.exe` runs at specific wall-clock times and executes **stored procedures** inside Azure BHG_DR to aggregate and summarize the freshly loaded data:

```
~6:45 AM → exec pats.BAMMerge          ← compute BAM addiction monitor scores
           exec pats.CalcKPIs           ← calculate clinic KPIs
           exec stg.DartsSrvMerge       ← merge staging → final DartsSrv tables
           exec stg.DartsSrvMerge22     ← merge for 2022
           exec stg.DartsSrvMerge23     ← merge for 2023
           ... and more stored procedures
```

This is NOT extracting from SAMMS — it's purely internal Azure BHG_DR work, computing derived/summary data from what the ETL already loaded.

---

## PHASE 7 — End of Day — Whiteboard Final Status

By mid-morning, the whiteboard looks like this:

```
tsk.tbl_Tasks2 at 10:00 AM:
══════════════════════════════════════════════════════════════════
TaskId | TaskName               | Status | RowCount | Duration
───────┼────────────────────────┼────────┼──────────┼──────────
20001  | SAMMSGlobal            | DONE   | 0        | 00:45:22  ← parent
20002  | Eastern ETL P1         | DONE   | 0        | 01:22:10  ← parent
30001  | pats.tbl_Enrollment B01| DONE   | 42       | 00:00:18  ← child done
30002  | pats.tbl_Enrollment B02| DONE   | 38       | 00:00:15  ← child done
30003  | pats.tbl_Enrollment B03| FAILED | 0        | 00:00:03  ← child failed
30004  | pats.tbl_UAResults B01 | DONE   | 156      | 00:00:31  ← child done
...thousands more rows...
```

The `ETLMgr` desktop app shows this grid so the team can monitor what succeeded, what failed, how many rows each task processed.

---

## The Complete Timeline — One Day at a Glance

```
11:59 PM  ┌─────────────────────────────────────────────────────────┐
          │  Whiteboard has yesterday's completed tasks             │
          │  No new tasks for today yet                             │
          └─────────────────────────────────────────────────────────┘
               │
               ▼
01:00 AM  ┌─────────────────────────────────────────────────────────┐
          │  Scheduler.exe  (~10 seconds)                           │
          │  ✔ Reads 17 rows from tsk.tbl_Schedule                  │
          │  ✔ Creates 17 parent tasks on the whiteboard            │
          │  ✔ Creates thousands of child tasks on the whiteboard   │
          │  ✔ Applies skip rules (marks invalid tasks RowState=26) │
          │  ✔ Closes. Done. Doesn't touch BHGTaskRunner.           │
          └─────────────────────────────────────────────────────────┘
               │
               ▼
02:00 AM  BHGTaskRunner.exe 1   → SAMMSGlobal              (~45 min)
               │  reads only SAMMSGlobal parent + children
               │  processes: global codes, users, consents, etc.
               ▼
03:00 AM  BHGTaskRunner.exe 2   → All 4 P1 pipelines       (~90 min)
               │  reads Eastern/Central/Mountain/Pacific ETL P1
               │  processes: enrollment, clients, UA results, etc.
               ▼
05:00 AM  BHGTaskRunner.exe 4   → All 4 P2 pipelines       (~60 min)
               │  reads Eastern/Central/Mountain/Pacific ETL P2
               │  processes: claims, bills, check-in, etc.
               ▼
06:00 AM  BHGTaskRunner.exe 5   → Samms-LAB                (~20 min)
               ▼
06:30 AM  BHGTaskRunner.exe 6   → Samms-Forms              (~30 min)
               ▼
07:00 AM  BHGTaskRunner.exe 7   → Notes                    (~15 min)
               ▼
07:30 AM  BHGTaskRunner.exe 8   → Inv/Assessments          (~60 min)
               ▼
08:30 AM  BHGTaskRunner.exe 9   → DartSvc                  (~30 min)
               ▼
09:00 AM  BHGTaskRunner.exe 10  → Dose                     (~20 min)
               ▼
09:30 AM  BHGTaskRunner.exe 11  → Orders                   (~15 min)
               │
               ▼
~10:00 AM ┌─────────────────────────────────────────────────────────┐
          │  All fresh data is now in Azure BHG_DR                  │
          │  Reports and dashboards show today's patient data       │
          │  ETLMgr desktop app shows task results for review       │
          └─────────────────────────────────────────────────────────┘
               │
               ▼
11:59 PM  Cycle repeats tomorrow...
```

---

## Key Facts — Quick Reference

| Item | Detail |
|---|---|
| **Scheduler.exe** | Runs ONCE per day (~10 seconds). Creates all tasks. Does NOT run BHGTaskRunner. |
| **BHGTaskRunner.exe** | ONE executable, called 11 times with different arguments throughout the morning |
| **17 Schedules** | Data rows in `tsk.tbl_Schedule` — not 17 programs. One Scheduler reads all 17. |
| **11 Arguments** | Each argument = a group of schedules processed together (e.g., arg 2 = all 4 P1 timezones) |
| **tsk.tbl_Tasks2** | The shared whiteboard — only way Scheduler and BHGTaskRunner communicate |
| **SAMMS databases** | 80+ local clinic databases, each on its own SQL Server |
| **Azure BHG_DR** | The single central destination database all data flows into |
| **AzureAgent.exe** | Runs after ETL loads to compute aggregates/scores via stored procedures |
| **ETLMgr.exe** | Desktop monitoring app — shows task status grid from tsk.tbl_Tasks2 |

---

## The Restaurant Analogy

| Role | Program | What it does |
|---|---|---|
| **Kitchen Manager** (arrives at opening) | `Scheduler.exe` | Writes the day's order tickets — runs ONCE |
| **Chefs** (work in shifts all day) | `BHGTaskRunner.exe 1-11` | Each chef picks up their assigned tickets and cooks — 11 separate shifts |
| **The ticket board** | `tsk.tbl_Tasks2` | Where all pending/done orders are tracked |
| **Post-service cleanup** | `AzureAgent.exe` | After all cooking is done, prepares summary reports |
| **Manager watching** | `ETLMgr.exe` | Supervisor watching the ticket board in real time |

> The manager (Scheduler) does NOT cook.  
> The chefs (BHGTaskRunner) do NOT write tickets.  
> The external timetable (Windows Task Scheduler) decides when each person starts their shift.  
> They only communicate through the ticket board (the database table).

---

## One Sentence Summary

> **Scheduler.exe** writes a to-do list once at night → **BHGTaskRunner.exe** (the same program called 11 times with different arguments) reads that list throughout the morning and copies data from 80+ clinic SAMMS databases into Azure BHG_DR → **AzureAgent.exe** runs stored procedures to compute summaries → by mid-morning, all reports have fresh patient data.
