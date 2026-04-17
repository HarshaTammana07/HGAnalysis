# Eastern ETL P1 — Complete End-to-End Journey
**Pipeline:** Eastern ETL P1  
**Triggered by:** `BHGTaskRunner.exe 2`  
**Example table traced:** `pats.tbl_ENROLLMENT` for site `B01`  
**Date:** 2026-03-25

---

## Who is "Eastern ETL P1"?

Eastern ETL P1 handles **non-financial, clinical data** (enrollments, client demographics, UA results, codes, clinic config, etc.) for all clinics in the **Eastern time zone (EST)**. It is triggered by running:

```
BHGTaskRunner.exe 2
```

Argument `2` picks up all four timezone P1 pipelines (`Eastern ETL P1`, `Central ETL P1`, `Mountain ETL P1`, `Pacific ETL P1`). This document traces EST only.

---

## Complete Flow Diagram

```
DAILY TRIGGER (Windows Scheduler / Azure Automation)
        │
        ▼
┌─────────────────────────────────────────────────────┐
│  SCHEDULER.EXE  (runs ~3 AM)                        │
│  Reads tsk.tbl_Schedule                             │
│  ┌──────────────────────────────────────────────┐   │
│  │  INSERT parent task: "Eastern ETL P1"        │   │
│  │  Status = 17 (Pending)                       │   │
│  │                                              │   │
│  │  INSERT child tasks from dms.vw_MapAction    │   │
│  │  One row per (EST site × P1 table)           │   │
│  │  e.g. B01 × pats.tbl_Enrollment → TaskId X  │   │
│  │       B01 × pats.tbl_ClientDemo1 → TaskId Y  │   │
│  │       B03 × pats.tbl_Enrollment → TaskId Z  │   │
│  │       ... ~600 child tasks total             │   │
│  │                                              │   │
│  │  SKIP RULES: mark invalid combos RowState=26 │   │
│  │  DELETE old/skipped tasks                    │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
        │
        ▼  (4:00 AM EST)
┌─────────────────────────────────────────────────────┐
│  BHGTaskRunner.exe 2  (Eastern ETL P1)              │
│  ┌──────────────────────────────────────────────┐   │
│  │  Load all pending tasks into RAM             │   │
│  │  Filter: TaskName = "Eastern ETL P1"         │   │
│  │                                              │   │
│  │  FOR EACH parent task:                       │   │
│  │    Status → 18 (Running)                     │   │
│  │    FOR EACH child task (ordered by table):   │   │
│  │    ┌────────────────────────────────────┐    │   │
│  │    │  Status → 18 (Running)             │    │   │
│  │    │                                    │    │   │
│  │    │  STEP A: Load column mappings      │    │   │
│  │    │  FROM dms.vw_MapSrc2Dsn            │    │   │
│  │    │  WHERE ActionKey=1, StepKey=4      │    │   │
│  │    │                                    │    │   │
│  │    │  STEP B: Build SELECT query        │    │   │
│  │    │  SelectConstructor.GetSLT()        │    │   │
│  │    │  → [col1], [col2], ...,            │    │   │
│  │    │     CHECKSUM(...) RowChkSum        │    │   │
│  │    │                                    │    │   │
│  │    │  STEP C: Apply date filter         │    │   │
│  │    │  WHERE LastModAt >= WorkDate-15    │    │   │
│  │    │                                    │    │   │
│  │    │  STEP D: Execute on SAMMS source   │    │   │
│  │    │  SQLSvrManager.GetTableData()      │    │   │
│  │    │  → DataTable (342 rows)            │    │   │
│  │    │                                    │    │   │
│  │    │  STEP E: switch(TaskName)          │    │   │
│  │    │  → sd.SaveEnrollment(DataTable)    │    │   │
│  │    │                                    │    │   │
│  │    │  STEP F: SaveEnrollment upsert     │    │   │
│  │    │  FOR EACH row:                     │    │   │
│  │    │    lookup by SiteCode+EnrollId     │    │   │
│  │    │    if NEW → INSERT                 │    │   │
│  │    │    if CHANGED checksum → UPDATE    │    │   │
│  │    │    if SAME checksum → SKIP         │    │   │
│  │    │  db.SaveChanges()                  │    │   │
│  │    │                                    │    │   │
│  │    │  STEP G: Update task status        │    │   │
│  │    │  Status → 19 (Done)                │    │   │
│  │    │  RowCount, Duration, RowsIns/Upd   │    │   │
│  │    └────────────────────────────────────┘    │   │
│  │    Repeat for next child task...             │   │
│  │                                              │   │
│  │  Parent task Status → 19 (Done)              │   │
│  └──────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
        │
        ▼
 Azure BHG_DR database now has today's data
 from all Eastern timezone SAMMS clinics
 in pats.tbl_Enrollment, pats.tbl_ClientDemo1, etc.
```

---

## PHASE 1 — One-Time Setup (Database Tables, not daily)

This is configuration that already lives in the Azure BHG_DR database. It does not change every day. It is the "blueprint" the whole system reads from.

### `tsk.tbl_Schedule` — has one row for Eastern ETL P1

| ScheduleId | Name | Enabled | NextRunTime | ActionKey |
|---|---|---|---|---|
| (some id) | Eastern ETL P1 | 1 | 2026-03-25 04:00 | 2 |

### `dms.vw_MapAction` — one row per (site × table) combination

For every EST clinic (e.g., B01, B03, B05...) and every P1-assigned table (Enrollment, ClientDemo1, UAResults, Codes, etc.) there is one row:

| SiteCode | TimeZone | DsnSchema | DsnTbl | SrcSchema | FromTblVw | ActionKey | StepKey | WhereCondition | ConStr |
|---|---|---|---|---|---|---|---|---|---|
| B01 | EST | pats | tbl_Enrollment | dbo | tblEnrollment | 1 | 4 | `LastModAt >= '@WorkDate'` | `Data Source=B01-SQL;...` |

### `dms.vw_MapSrc2Dsn` — one row per column within each (ActionKey, StepKey) combo

For Enrollment (ActionKey=1, StepKey=4):

| FieldKey | FieldName (Source) | DsnFieldName (Destination) | FieldType | PrimaryKey |
|---|---|---|---|---|
| 1 | cltId | CltId | int | NULL |
| 2 | EnrollId | Id | int | 1 |
| 3 | Program | Program | varchar | NULL |
| 4 | EnrollDate | EnrollDate | datetime | NULL |
| ... | ... | ... | ... | ... |

This is the **column-by-column mapping**: source column name → destination column name.

---

## PHASE 2 — Scheduler.exe Runs (Each Morning, ~3 AM)

`Scheduler.exe` runs once per day. It fires a single SQL block against Azure BHG_DR. Here is what it does step by step.

### Step 2-A: Reset stuck tasks from yesterday

```sql
UPDATE tsk.tbl_Tasks2 SET Status = 17 WHERE Status = 18
```

Status 18 = "Running". If anything is still "Running" from yesterday, it was stuck — reset to 17 (Pending).

### Step 2-B: Insert the parent task for Eastern ETL P1

```sql
INSERT INTO tsk.tbl_Tasks2
    (TaskName, RunAt, ActionKey, Status, RowState, SiteCode, WorkDate, ...)
SELECT
    'Eastern ETL P1',
    '2026-03-25 04:00',
    2,
    17,    -- Pending
    24,    -- Active
    'All',
    '2026-03-25', ...
FROM tsk.tbl_Schedule
WHERE Enabled = 1
```

Result: one parent task row, `Status = 17`, `ParentTaskId = NULL`.

### Step 2-C: Insert all child tasks (one per site × table)

```sql
INSERT INTO tsk.tbl_Tasks2
    (ParentTaskId, TaskName, ActionKey, ActionStepKey, SiteCode, Status, RowState, WorkDate, ...)
SELECT
    t.TaskId,                              -- links child to parent
    ma.DsnSchema + '.' + ma.DsnTbl,        -- e.g. 'pats.tbl_Enrollment'
    ma.ActionKey,
    ma.StepKey,
    ma.SiteCode,                           -- e.g. 'B01'
    17, 24, t.WorkDate, ...
FROM dms.vw_MapAction ma
CROSS JOIN tsk.tbl_Tasks2 t
WHERE ma.Enabled = 1
  AND ma.IsActive = 1
  AND t.Status = 17
  AND t.WorkDate = '2026-03-25'
  AND (
        CASE
          WHEN ma.TimeZone = 'EST'
               AND ma.DsnSchema + '.' + ma.DsnTbl NOT IN (
                   'pats.tbl_claims', 'pats.tbl_claimlineitem',
                   'pats.tbl_Bills', 'pats.tbl_CheckIn', ...  -- financial tables go to P2
               )
          THEN 'Eastern ETL P1'
        END
      ) = t.TaskName
ORDER BY ma.ActionKey, ma.DsnTbl, ma.SiteCode
```

For 20 EST sites × 30 P1 tables = **~600 child task rows** inserted, all with:
- `ParentTaskId` → pointing to the Eastern ETL P1 parent row
- `Status = 17` (Pending)
- `RowState = 24` (Active)
- `TaskName = 'pats.tbl_Enrollment'` (the destination table name)
- `SiteCode = 'B01'`

### Step 2-D: Apply skip rules

Some site/table combinations are invalid and get soft-deleted:

```sql
-- V6 PreAdmission not available at PHC or LAB
UPDATE tsk.tbl_Tasks2 SET RowState = 26
WHERE TaskName = 'ayx.tbl_PreAdmission_V6'
  AND SiteCode IN ('PHC', 'LAB')
  AND Status = 17 AND RowState = 24;

-- LAB site doesn't use Appointments
UPDATE tsk.tbl_Tasks2 SET RowState = 26
WHERE TaskName = 'pats.tbl_Appointments'
  AND SiteCode = 'LAB' AND RowState = 24;

-- ... 10 more skip rules ...
```

### Step 2-E: Delete old and skipped tasks

```sql
DELETE FROM tsk.tbl_Tasks2
WHERE RunAt <= DATEADD(m, -3, CONVERT(DATE, GETDATE()))
   OR RowState = 26;
```

**Result after Phase 2:** `tsk.tbl_Tasks2` has fresh, clean task rows ready for tonight's run. Each row knows exactly: what table to load, for which site, from which source, linked to its parent.

---

## PHASE 3 — External Trigger Fires BHGTaskRunner.exe

Windows Task Scheduler (or Azure Automation) runs at **4:00 AM EST**:

```
BHGTaskRunner.exe 2
```

---

## PHASE 4 — BHGTaskRunner Starts Up

```csharp
// Initialize all helpers
SelectConstructor sc  = new SelectConstructor();   // builds SELECT field lists
SQLSvrManager     sm  = new SQLSvrManager();        // executes raw SQL against any DB
BHG_DRContext     db  = new BHG_DRContext();        // EF Core → Azure BHG_DR
SaveData          sd  = new SaveData();             // upsert logic (insert/update/skip)

// Load ALL pending tasks from Azure BHG_DR into RAM
List<VwTaskListMap> Tasks = db.VwTaskList
    .Where(x => x.SiteCode != "PHC"
             && x.Status == 17
             && x.RunAt < DateTime.Now)
    .ToList();

// Filter PARENT tasks for arg "2" = all P1 timezone pipelines
pTasks = db.VwTaskList.Where(x =>
    x.Status == 17 &&
    x.RunAt < DateTime.Now &&
    (x.TaskName == "Eastern ETL P1"  ||
     x.TaskName == "Central ETL P1"  ||
     x.TaskName == "Mountain ETL P1" ||
     x.TaskName == "Pacific ETL P1")
).ToList();
```

Result: `pTasks` = 4 parent task objects (one per timezone). We trace Eastern ETL P1.

---

## PHASE 5 — Outer Loop: Iterate Parent Tasks

```csharp
foreach (var pt in pTasks
    .Where(x => x.ParentTaskId == null)
    .OrderBy(z => z.WorkDate)
    .ThenBy(o => o.RunAt))
{
    // Mark parent task as Running
    TblTasks ptask = db.TblTasks.Where(x => x.TaskId == pt.TaskId).First();
    ptask.Status = 18;   // 18 = Running
    db.SaveChanges();
```

The **"Eastern ETL P1"** parent task is now `Status = 18`.

---

## PHASE 6 — Inner Loop: Iterate Child Tasks

```csharp
foreach (var st in Tasks
    .Where(x => x.ParentTaskId == pt.TaskId)
    .OrderBy(o => o.TaskName)       // alphabetical table order
    .ThenBy(b => b.SiteCode)        // then by site code
    .ThenBy(d => d.FromTblVw))      // then by source view name
```

We hit the child task: **`pats.tbl_Enrollment` for site `B01`**.

The `st` object (from `vw_TaskListMap`) contains:

| Field | Value | Meaning |
|---|---|---|
| `st.TaskId` | 88234 | unique task ID |
| `st.TaskName` | `pats.tbl_Enrollment` | destination table |
| `st.SiteCode` | `B01` | clinic site code |
| `st.ActionKey` | 1 | mapping group ID |
| `st.ActionStepKey` | 4 | mapping sub-step ID |
| `st.SrcSchema` | `dbo` | source schema |
| `st.FromTblVw` | `tblEnrollment` | source table/view name |
| `st.WhereCondition` | `LastModAt >= '@WorkDate'` | filter template |
| `st.WorkDate` | `2026-03-25` | today's work date |
| `st.ConStr` | `Data Source=B01-SQL;Initial Catalog=SAMMS_B01;...` | source DB connection string |
| `st.IsNewSchema` | `false` | schema version flag |

```csharp
// Mark child task as Running
task.Status    = 18;
task.RunAt     = DateTime.Now;
task.ErrorMessage = "";
db.SaveChanges();
```

---

## PHASE 7 — Load Column Mappings from `dms.vw_MapSrc2Dsn`

```csharp
List<VwMapSrc2Dsn> tdwork = db.WorkToDo
    .Where(x => x.Enabled
             && x.ActionKey      == st.ActionKey       // 1
             && x.ActionStepKey  == st.ActionStepKey)  // 4
    .ToList();
```

Returns ~30 column-mapping rows for Enrollment — one row per field. Each row says:
- Source field `cltId` → Destination field `CltId`, type `int`
- Source field `EnrollId` → Destination field `Id`, type `int` (PrimaryKey = 1)
- Source field `Program` → Destination field `Program`, type `varchar`
- etc.

**Enable RowChkSum** (ActionKey != 3, so checksums are active):
```csharp
ChkSumEnabled = true;
```

---

## PHASE 8 — Build the SELECT Query (SelectConstructor.GetSLT)

```csharp
strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, "tblEnrollment", "B01")
            .Replace("@SiteCode", "'B01'")
            .Replace("@Samms", "'SAMMS'");
```

Inside `GetSLT()`, it loops every column row in `tdwork`:

1. **Always skip** `RowChkSum` from the field list (it will be recomputed).
2. **Skip schema-gated fields** — for old-schema sites, newer columns like `ReqAuth` are omitted so the query doesn't fail on old SAMMS versions.
3. **Build SELECT list**: `[cltId], [EnrollId] Id, [Program], [EnrollDate], ...`
4. **Build CHECKSUM list**: all fields except `ntext`, `varbinary`, `timestamp` types.
5. **Append computed checksum**: `CHECKSUM([cltId], [EnrollId], ...) RowChkSum`

Final `strFlds`:
```sql
[cltId],
[EnrollId] Id,
[Program],
[EnrollDate],
[EnrollReasonCode],
[DischargeReasonCode],
[DischargeDate],
[Counselor],
[Status],
[StrStaff],
[Transfer],
[NoDartsEnroll],
[Dasareason],
[DtLastContact],
...
CHECKSUM([cltId], [EnrollId], [Program], [EnrollDate], ...) RowChkSum
```

---

## PHASE 9 — Apply the Date Window Filter

```csharp
int DaysBack = -15;

string strWhere = st.WhereCondition
    .Replace("@WorkDate",
             st.WorkDate.Value.AddDays(DaysBack).ToShortDateString());

// WhereCondition template = "LastModAt >= '@WorkDate'"
// WorkDate = 2026-03-25
// WorkDate + (-15 days) = 2026-03-10
// Result: "LastModAt >= '2026-03-10'"
```

The full SELECT query assembled:

```sql
SELECT
    [cltId],
    [EnrollId] Id,
    [Program],
    [EnrollDate],
    [EnrollReasonCode],
    [DischargeReasonCode],
    [DischargeDate],
    [Counselor],
    [Status],
    ...
    CHECKSUM([cltId], [EnrollId], [Program], [EnrollDate], ...) RowChkSum
FROM dbo.tblEnrollment
WHERE LastModAt >= '2026-03-10'
ORDER BY [EnrollId]
```

> **Why -15 days?**  
> The lookback window catches any row modified in the last 15 days — not just today.  
> This handles missed runs, retries, and late-arriving data without having to reload everything.

---

## PHASE 10 — Execute Query Against SAMMS Source Database

```csharp
SrcDt = sm.GetTableData("tblEnrollment", strCmd, st.ConStr);
```

`SQLSvrManager.GetTableData()` opens a connection to **B01's local SAMMS SQL Server** using `st.ConStr`, fires the SELECT query, and returns a `DataTable` with all matching rows into memory.

Say it returns **342 rows** — all enrollment records modified in the last 15 days at clinic B01.

Console output:
```
B01  pats.tbl_Enrollment  Rows = 342    04:03:17 AM
```

---

## PHASE 11 — Route to the Correct Save Method (switch statement)

```csharp
switch (st.TaskName.ToLower())
{
    case "pats.tbl_enrollment":
        strCmd += " Where " + strWhere + " " + st.SortOrder;
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
        rCodes = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null);
        break;
    // ... ~98 other cases for other tables ...
}
```

The `switch` has ~98 cases — one per destination table. Each case:
- May add table-specific adjustments to the query (e.g., extra WHERE clauses)
- Calls the specific `Save___()` method in `SaveData.cs`

---

## PHASE 12 — SaveEnrollment: The Upsert Loop (EF Core)

Inside `SaveData.SaveEnrollment(SrcDt, "B01", 1, null)`:

```csharp
BHG_DRContext db = new BHG_DRContext();   // connect to Azure BHG_DR
int rowsInserted = 0;
int rowsUpdated  = 0;

foreach (DataRow r in SrcDt.Rows)
{
    // 1. Parse the primary key and new checksum from the source row
    int enrollId   = int.Parse(r["Id"].ToString());
    int newChkSum  = int.Parse(r["RowChkSum"].ToString());

    // 2. Look up existing record in Azure BHG_DR by SiteCode + primary key
    TblEnrollment existing = db.TblEnrollment
        .Where(x => x.SiteCode == "B01" && x.Id == enrollId)
        .FirstOrDefault();

    if (existing == null)
    {
        // ─── 3A. INSERT ─── Record does not exist in Azure yet
        TblEnrollment newRec = new TblEnrollment
        {
            SiteCode            = "B01",
            Id                  = enrollId,
            CltId               = int.Parse(r["CltId"].ToString()),
            Program             = r["Program"].ToString(),
            EnrollDate          = DateTime.Parse(r["EnrollDate"].ToString()),
            EnrollReasonCode    = r["EnrollReasonCode"].ToString(),
            DischargeReasonCode = r["DischargeReasonCode"] == DBNull.Value
                                  ? null : r["DischargeReasonCode"].ToString(),
            DischargeDate       = r["DischargeDate"] == DBNull.Value
                                  ? (DateTime?)null
                                  : DateTime.Parse(r["DischargeDate"].ToString()),
            Counselor           = r["Counselor"].ToString(),
            Status              = r["Status"].ToString(),
            // ... all other fields ...
            RowChkSum  = newChkSum,
            LastModAt  = DateTime.Now,
            RowState   = 24
        };
        db.TblEnrollment.Add(newRec);
        rowsInserted++;
    }
    else if (existing.RowChkSum != newChkSum)
    {
        // ─── 3B. UPDATE ─── Record exists BUT data has changed (checksum differs)
        existing.CltId               = int.Parse(r["CltId"].ToString());
        existing.Program             = r["Program"].ToString();
        existing.EnrollDate          = DateTime.Parse(r["EnrollDate"].ToString());
        existing.DischargeReasonCode = r["DischargeReasonCode"] == DBNull.Value
                                       ? null : r["DischargeReasonCode"].ToString();
        existing.DischargeDate       = r["DischargeDate"] == DBNull.Value
                                       ? (DateTime?)null
                                       : DateTime.Parse(r["DischargeDate"].ToString());
        existing.Counselor           = r["Counselor"].ToString();
        // ... all other fields ...
        existing.RowChkSum  = newChkSum;   // update the checksum too
        existing.LastModAt  = DateTime.Now;
        rowsUpdated++;
    }
    // ─── 3C. SKIP ─── Checksum is the same → data unchanged → do nothing
}

// 4. Commit ALL inserts and updates to Azure BHG_DR in one batch
db.SaveChanges();
```

### Why the checksum matters

| Scenario | RowChkSum comparison | Action taken | Cost |
|---|---|---|---|
| Brand new enrollment | No existing record | INSERT | Write |
| Counselor changed on enrollment | Old chk ≠ New chk | UPDATE all fields | Write |
| Enrollment unchanged | Old chk = New chk | SKIP | Nothing |

If 342 rows come from SAMMS but only 62 actually changed, **280 rows are skipped** — no SQL writes, no EF tracking, just a fast checksum comparison. This makes the ETL efficient even for large tables.

---

## PHASE 13 — Return Results & Update Task Status

Back in `BHGTaskRunner`, after `SaveEnrollment` returns:

```csharp
// rCodes returned from SaveEnrollment:
// rCodes.IsResult      = true   (success)
// rCodes.RowsIns       = 15     (15 new enrollment records inserted)
// rCodes.RowsUpd       = 47     (47 records updated)
// rCodes.RowsProcessed = 342    (342 rows read from source)

task.Status        = 19;          // 19 = Success  (20 = Failed)
task.Duration      = "00:00:08";  // 8 seconds
task.RowCount      = 342;
task.RowsIns       = 15;
task.RowsUpd       = 47;
task.ErrorMessage  = "";
db.SaveChanges();
```

The child task row in `tsk.tbl_Tasks2` now reads:
- Status **19** (Done/Success)
- 342 rows processed, 15 inserted, 47 updated, 280 skipped
- Took 8 seconds

---

## PHASE 14 — Repeat for Every Other Table and Site

The inner loop continues, moving on to the next child task:

```
pats.tbl_Enrollment   for B01   ✓ Done (342 rows, 8s)
pats.tbl_Enrollment   for B03   → same process, different ConStr
pats.tbl_Enrollment   for B05   → same
pats.tbl_Enrollment   for B12   → same
... all EST sites done for Enrollment ...

pats.tbl_ClientDemo1  for B01   → same process
pats.tbl_ClientDemo1  for B03   → same
...

pats.tbl_ClientDemo2  for B01   → same
...

pats.tbl_Codes        for B01   → same
...

pats.tbl_UAResults    for B01   → same
...

pats.tbl_UASched      for B01   → same
...

ctrl.tbl_Clinic       for B01   → same
...

pats.tbl_Services     for B01   → same
...
(continues for all ~30 P1 tables × ~20 EST sites = ~600 child tasks)
```

---

## PHASE 15 — Parent Task Marked Complete

Once all ~600 child tasks are processed:

```csharp
ptask.Status   = 19;           // Parent → Success
ptask.Duration = "01:42:17";   // Total runtime: 1 hour 42 minutes
db.SaveChanges();
```

The Eastern ETL P1 run for today is **complete**.

---

## One Row's Full Journey — Summary Table

| # | Stage | What happens | System / Location |
|---|---|---|---|
| 1 | Data entry | Nurse saves a patient enrollment in SAMMS | B01 local SAMMS SQL Server (`dbo.tblEnrollment`) |
| 2 | Scheduler | Inserts a child task for B01 × Enrollment | Azure BHG_DR (`tsk.tbl_Tasks2`) |
| 3 | BHGTaskRunner loads metadata | Reads column mappings for ActionKey=1, StepKey=4 | Azure BHG_DR (`dms.vw_MapSrc2Dsn`) |
| 4 | Build SELECT | `SelectConstructor` assembles SELECT with CHECKSUM | Memory (C# string) |
| 5 | Apply date filter | `WHERE LastModAt >= '2026-03-10'` (WorkDate − 15 days) | Memory (C# string) |
| 6 | Execute query | SQL runs against B01's SAMMS database | B01 local SQL Server |
| 7 | Data in RAM | 342 rows loaded into a `DataTable` in memory | BHGTaskRunner process RAM |
| 8 | Switch routing | `case "pats.tbl_enrollment":` → calls `SaveEnrollment()` | BHGTaskRunner `Program.cs` |
| 9 | Upsert loop | Each row: lookup in Azure → INSERT / UPDATE / SKIP via checksum | Azure BHG_DR (`pats.tbl_Enrollment`) |
| 10 | Commit | `db.SaveChanges()` flushes all changes to Azure SQL | Azure BHG_DR |
| 11 | Status update | Task marked Status=19, RowCount=342, RowsIns=15, RowsUpd=47 | Azure BHG_DR (`tsk.tbl_Tasks2`) |
| 12 | Next task | Loop moves to pats.tbl_Enrollment for B03 | Repeats from step 3 |

---

## Status Code Reference

| Code | Meaning |
|---|---|
| 17 | Pending — created by Scheduler, waiting to run |
| 18 | Running — currently being processed |
| 19 | Success — completed without errors |
| 20 | Failed — completed with errors |
| 22 | (Legacy/other use) |
| 24 | RowState: Active |
| 26 | RowState: Skipped / soft-deleted |

---

## Key Files Involved

| File | Role |
|---|---|
| `Scheduler/Program.cs` | Creates all tasks in `tsk.tbl_Tasks2` each morning |
| `BHGTaskRunner/Program.cs` | Main ETL loop — reads tasks, queries sources, routes to save methods |
| `BHG-DR-LIB/SelectConstructor.cs` | Builds the SELECT field list dynamically from column mapping metadata |
| `BHG-DR-LIB/SQLSvrManager.cs` | Executes raw SQL queries against any database connection string |
| `BHG-DR-LIB/SaveData.cs` | Contains all Save___() upsert methods (one per destination table) |
| `BHG-DR-LIB/Models/BHG_DRContext.cs` | EF Core DbContext — maps C# classes to Azure BHG_DR tables |
| `dms.vw_MapAction` | Runtime config: which sites, tables, source objects, and connection strings |
| `dms.vw_MapSrc2Dsn` | Runtime config: column-level field mappings per (ActionKey, StepKey) |
| `tsk.tbl_Tasks2` | The task queue — every ETL job's status, timing, and row counts live here |
