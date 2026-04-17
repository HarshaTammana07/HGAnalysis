# ActionKey & ActionStepKey — Complete Reference
**File:** `BCAppCode` System — Metadata-Driven ETL Engine  
**Date:** 2026-03-25

---

## 1. What Are They? (Simple Explanation)

`ActionKey` and `ActionStepKey` are a **two-part address** used by the ETL engine to answer one question:

> **"Which columns should I SELECT from this SAMMS source table when loading data into Azure BHG_DR?"**

Think of it like a filing cabinet:

```
Filing Cabinet  =  dms.vw_MapSrc2Dsn  (metadata table in Azure BHG_DR)
  │
  ├── Drawer #1  (ActionKey = 1)   ← "Standard per-site SAMMS tables"
  │     ├── Folder #9  (StepKey = 9)   → tblUAResult → pats.tbl_UAResults
  │     ├── Folder #24 (StepKey = 24)  → SF_PatientPreAdmission → ayx.tbl_PreAdmission_V6
  │     ├── Folder #83 (StepKey = 83)  → Tbldiag10 → pats.tbl_TblDiag10
  │     └── ... (up to StepKey 85+)
  │
  ├── Drawer #2  (ActionKey = 2)   ← "Global / shared tables"
  │     ├── Folder #7  (StepKey = 7)   → tblFORMSSAMMSCLIENT → pats.tbl_FormsSAMMSClient
  │     ├── Folder #9  (StepKey = 9)   → BAM source → pats.tbl_BriefAddictionMonitor
  │     └── Folder #13 (StepKey = 13)  → LiquidLog → pats.tbl_LiquidLog
  │
  ├── Drawer #3  (ActionKey = 3)   ← "Bulk reload tables — NO checksum"
  ├── Drawer #4  (ActionKey = 4)   ← "Financial / billing tables"
  ├── Drawer #5  (ActionKey = 5)   ← "PHC / LAB claims variant"
  ├── Drawer #6  (ActionKey = 6)   ← "Separate group"
  └── Drawer #7  (ActionKey = 7)   ← "Methasoft schema clinics"
```

Inside **each folder** is a list of column mapping rows — one row per column — that tells the ETL:
- What the column is named in the SAMMS source
- What to call it in the Azure BHG_DR destination
- What data type it is
- Whether it should be included in the `RowChkSum` checksum calculation

---

## 2. Where Do They Come From?

### In the Database (Azure BHG_DR)

Two tables store the mappings:

#### `dms.tbl_MapAction` — One row per table per site
This table defines **what to extract** for each ETL step:

| Column | Purpose |
|---|---|
| `ActionKey` | The pipeline/group number |
| `StepKey` | The specific table number within that group |
| `SiteCode` | Which clinic site this applies to |
| `SrcSchema` | Source schema in SAMMS (usually `dbo`) |
| `FromTblVw` | Source table/view name in SAMMS |
| `DsnSchema` | Destination schema in Azure BHG_DR |
| `DsnTbl` | Destination table name in Azure BHG_DR |
| `WhereCondition` | The WHERE clause filter for this table |
| `SortOrder` | ORDER BY clause |
| `Enabled` | Whether this mapping is active |
| `IsActive` | Whether the site is active |
| `ReInitialize` | Whether to do a full reload |
| `ConStr` | Connection string to the source SAMMS database |

#### `dms.tbl_MapSrc2Dsn` — One row per **column** per table
This table defines **which columns** to include in the SELECT:

| Column | Purpose |
|---|---|
| `ActionKey` | Links to the parent table in `tbl_MapAction` |
| `ActionStepKey` | Links to the specific step in `tbl_MapAction` |
| `FieldKey` | Column order number |
| `FieldName` | Column name in the SAMMS source |
| `DsnFieldName` | Column alias in the Azure BHG_DR destination |
| `FieldType` | SQL data type (varchar, int, datetime, etc.) |
| `PrimaryKey` | Whether this column is part of the primary key |
| `Enabled` | Whether to include this column |
| `PHC_Enabled` | Whether to include for PHC sites specifically |
| `Nullable` | Whether the column allows nulls |

### In the Code (C# / BHGTaskRunner)

Each child task record in `tsk.tbl_Tasks2` has `ActionKey` and `ActionStepKey` columns. When BHGTaskRunner picks up a task to run, it uses those two numbers to look up the column mappings:

```csharp
// Load the column mapping rows for this specific table
List<VwMapSrc2Dsn> tdwork = db.WorkToDo
    .Where(x => x.Enabled
             && x.ActionKey     == st.ActionKey      // e.g. 1
             && x.ActionStepKey == st.ActionStepKey) // e.g. 9
    .ToList();

// Build the SELECT column list from those rows
strFlds = sc.GetSLT(tdwork, ChkSumEnabled, st.IsNewSchema.Value, st.FromTblVw, st.SiteCode);

// Assemble the full query
strCmd = "Select " + strFlds + " from dbo.tblUAResult";
```

---

## 3. Why Two Keys Instead of One?

Because **one key alone is not specific enough**.

- `ActionKey` alone just says: "this is a standard SAMMS table" — but there are 85+ tables in that group
- `ActionStepKey` alone is just a number with no context (step 9 means different things in different groups)
- Together `(ActionKey=1, ActionStepKey=9)` **uniquely identifies** exactly one table's column mapping

**Street address analogy:**
- `ActionKey` = the street name
- `ActionStepKey` = the house number
- You need BOTH to find the exact house

---

## 4. All 7 ActionKeys — What They Cover

There are **7 ActionKeys** in the system (not 5 — confirmed from SQL files and source code):

| ActionKey | Group Name | What It Covers | Checksum? | Notes |
|---|---|---|---|---|
| **1** | Standard SAMMS | Every standard per-site table: enrollment, clients, UA results, codes, clinic, assessments, notes, etc. | YES | StepKeys go from 1 up to 85+ |
| **2** | Global / Shared | Tables sourced from the shared SAMMSGLOBAL database: Forms, BAM, LiquidLog | YES | StepKeys include 7, 9, 13 |
| **3** | Bulk / Reference | Reference/lookup tables that need full reload — no change detection | **NO** | Checksum is disabled for this key |
| **4** | Financial / Billing | Claims, bills, eligibility: tbl_Claims, tbl_ClaimLineItem, tbl_3pElig | YES | Covers P2 financial pipeline |
| **5** | PHC / LAB Claims | PHC and LAB-specific claims variant | YES | Used in `SelectConstructor.SyncRDB2` |
| **6** | Separate Group | Another ETL category (referenced in commented-out code) | YES | Exact scope in DB only |
| **7** | Methasoft Schema | Clinics using the Methasoft (ms) schema format | YES | Referenced in `ctrl.tbl_LocationCons` inserts |

### The Most Critical Rule — ActionKey 3 Disables Checksum

This one line in `BHGTaskRunner/Program.cs` is very important:

```csharp
if (st.ActionKey == 3) { ChkSumEnabled = false; } else { ChkSumEnabled = true; }
```

- For `ActionKey = 3`: No `RowChkSum` column is added to the SELECT. The ETL does a **full delete + reload** of the data (no insert/update/skip logic).
- For all other ActionKeys: `CHECKSUM(...)` is appended to the SELECT as `RowChkSum`, enabling row-level change detection.

---

## 5. Known ActionStepKey Mappings (From Code + SQL Files)

The full list of StepKeys lives in the **Azure BHG_DR database** (`dms.tbl_MapAction`), not in C# code. But the SQL scripts in the repo reveal these confirmed mappings:

### ActionKey = 1 (Standard SAMMS tables)

| StepKey | Source Table (`FromTblVw`) | Destination Table | Notes |
|---|---|---|---|
| 6 | `vw_PayerClt` (or similar) | `pats.tbl_PayerClient` | Skipped for LAB |
| 9 | `tblUAResult` | `pats.tbl_UAResults` | UA drug test results |
| 10 | `tblUAResultDetail` | `pats.tbl_UAResultDetail` | UA test detail lines |
| 23 | `SF_COWS` | `pats.tbl_Cows_V6` | Clinical Opiate Withdrawal Scale (V6 schema only) |
| 24 | `SF_PatientPreAdmission` | `ayx.tbl_PreAdmission_V6` | Pre-admission form (V6 schema only) |
| 34 | (AR notes source) | `pats.tbl_3parnote` | AR/account receivable notes |
| 35 | (claim notes source) | `pats.tbl_3pclaimnote` | Claim-level notes |
| 44 | `ReAssessment` | `pats.Tbl_ReAssessment` | Patient re-assessment |
| 56 | `ReAssessmentSocial` | `pats.tbl_ReAssessmentSocial` | Social dimension of re-assessment |
| 57 | `ReAssessmentTreatment` | `pats.tbl_ReAssessmentTreatment` | Treatment dimension of re-assessment |
| 61 | `tblTreatmentLevel` | `pats.tbl_TreatmentLevel` | Treatment level/LOC |
| 62 | `admissionassessmentsubstanceusehistory` | `pats.tbl_AdmissionAssessmentSubstanceuseHistory` | Substance use history |
| 65 | `PACounselorReview` | `pats.tbl_PACounselorReview` | Counselor review for periodic assessment |
| 69 | `PADimension4` | `pats.tbl_PADimension4` | PA dimension 4 |
| 70 | `PADimension5` | `pats.tbl_PADimension5` | PA dimension 5 |
| 71 | `PADimension6` | `pats.tbl_PADimension6` | PA dimension 6 |
| 79 | (ASAM assessment) | `pats.tbl_NewAdmissionAssessmentASAMDimension6` | ASAM dimension 6 |
| 83 | `Tbldiag10` | `pats.tbl_TblDiag10` | ICD-10 diagnosis codes |
| 84 | `NewPeriodicReassessment` | `pats.tbl_NewPeriodicReassessment` | New periodic re-assessment |
| 85 | `NewPeriodicReassessmentCounselorReview` | `pats.tbl_NewPeriodicReassessmentCounselorReview` | Counselor review for new periodic reassessment |

Note: StepKeys 1–8, 11–22, 25–33, 36–43, 45–55, 58–60, 63–64, 66–68, 72–78, 80–82 also exist but are defined only in the database, not visible from the code files alone.

### ActionKey = 2 (Global / Shared tables)

| StepKey | Source Table (`FromTblVw`) | Destination Table | Notes |
|---|---|---|---|
| 7 | `tblFORMSSAMMSCLIENT` | `pats.tbl_FormsSAMMSClient` | SAMMS form completion records from SAMMSGLOBAL |
| 9 | (BAM source) | `pats.tbl_BriefAddictionMonitor` | Brief Addiction Monitor scores |
| 13 | `LiquidLog` | `pats.tbl_LiquidLog` | Liquid methadone log entries |

### ActionKey = 4 (Financial / Billing)

| StepKey | Source Table (`FromTblVw`) | Destination Table | Notes |
|---|---|---|---|
| 1 | `tbl3pClaim` | `pats.tbl_Claims` | Third-party claims (billing) |

---

## 6. How the Flow Works — Step by Step

### Step 1: Scheduler.exe Creates Tasks

Every day, `Scheduler.exe` inserts **child task rows** into `tsk.tbl_Tasks2` by cross-joining `dms.vw_MapAction` with parent tasks. Each child task row gets `ActionKey` and `ActionStepKey` copied from `dms.vw_MapAction`:

```sql
-- Child tasks inherit ActionKey and StepKey from dms.vw_MapAction
INSERT INTO tsk.tbl_Tasks2 (ParentTaskId, TaskName, ActionKey, ActionStepKey, ...)
SELECT t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, ma.ActionKey, ma.StepKey, ...
FROM dms.vw_MapAction ma 
CROSS JOIN tsk.tbl_Tasks2 t
WHERE ma.Enabled = 1 AND ma.IsActive = 1 ...
```

### Step 2: BHGTaskRunner.exe Picks Up Tasks

BHGTaskRunner reads the child tasks from `tsk.tbl_Tasks2` and for each one:

```
Child task row has:
  ActionKey     = 1
  ActionStepKey = 9
  TaskName      = "pats.tbl_UAResults"
  SiteCode      = "B01"
  ConStr        = "Data Source=B01-SQL;..."
  FromTblVw     = "tblUAResult"
  WhereCondition = "convert(date,uarResultDt) >= @WorkDate"
```

### Step 3: Column Mapping Lookup

```csharp
// Use ActionKey=1 and ActionStepKey=9 to find all column rows
List<VwMapSrc2Dsn> tdwork = db.WorkToDo
    .Where(x => x.Enabled
             && x.ActionKey == 1         // ← ActionKey
             && x.ActionStepKey == 9)    // ← ActionStepKey
    .ToList();
// Returns: [{uarID, int, PK=1}, {SiteCode→@SiteCode, varchar}, {uarResultDt, datetime}, ...]
```

### Step 4: SelectConstructor Builds the SELECT

`SelectConstructor.GetSLT()` loops through all the column rows and builds:

```sql
SELECT [uarID], '@SiteCode' SiteCode, [uarResultDt], [uarDropDt], ...
       CHECKSUM([uarID], [uarResultDt], ...) RowChkSum
FROM dbo.tblUAResult
WHERE convert(date,uarResultDt) >= '3/10/2026'
ORDER BY 1
```

### Step 5: Execute and Save

The query runs against the SAMMS source database, results come back as a `DataTable`, then the appropriate `Save___()` method writes them to Azure BHG_DR using EF Core upsert (insert/update/skip based on `RowChkSum`).

---

## 7. Complete ASCII Flow Diagram

```
Azure BHG_DR Database
─────────────────────────────────────────────────────
dms.tbl_MapAction                dms.tbl_MapSrc2Dsn
  ActionKey  StepKey  FromTblVw    ActionKey  ActionStepKey  FieldName
  ─────────  ───────  ──────────   ─────────  ─────────────  ─────────
  1          9        tblUAResult  1          9              uarID
  1          9        tblUAResult  1          9              SiteCode
  1          9        tblUAResult  1          9              uarResultDt
  1          10       tblUARDetail 1          10             uardRecID
  2          7        tblFORMSCLT  2          7              fscsid
  ...                              ...
─────────────────────────────────────────────────────
             │                          │
             │ cross-joined at runtime  │ looked up at runtime
             ▼                          ▼
    Scheduler.exe                BHGTaskRunner.exe
    ─────────────                ─────────────────
    Inserts child tasks          Reads child tasks
    into tsk.tbl_Tasks2          from tsk.tbl_Tasks2
    with ActionKey+StepKey       uses ActionKey+StepKey
    copied from MapAction        to look up column list
             │                          │
             ▼                          ▼
    tsk.tbl_Tasks2               SelectConstructor.GetSLT()
    ─────────────────            ─────────────────────────
    TaskName = pats.tbl_UAResults  builds:
    ActionKey = 1                  SELECT [uarID], [SiteCode],
    ActionStepKey = 9              [uarResultDt], ...,
    SiteCode = "B01"               CHECKSUM(...) RowChkSum
    ConStr = "Data Source=..."     FROM dbo.tblUAResult
    FromTblVw = "tblUAResult"      WHERE ...
    WhereCondition = "..."
             │                          │
             └──────────────────────────┘
                                        │
                                        ▼
                               SQLSvrManager.GetTableData()
                               ─────────────────────────────
                               Runs query against SAMMS B01
                               Returns DataTable (source rows)
                                        │
                                        ▼
                               switch(st.TaskName.ToLower())
                               ─────────────────────────────
                               case "pats.tbl_uaresults":
                                 sd.SaveUAResults(SrcDt, ...)
                                        │
                                        ▼
                               EF Core Upsert
                               ─────────────
                               foreach row in SrcDt:
                                 existing = db.TblUAResults
                                   .Where(x => x.SiteCode == "B01"
                                            && x.UarId == rowId)
                                   .FirstOrDefault()
                                 if null → INSERT
                                 elif RowChkSum differs → UPDATE
                                 else → SKIP (no change)
                               db.SaveChanges()
```

---

## 8. Special Rules and Edge Cases

### Rule 1 — PHC Sites Filter Differently

For PHC sites, only columns where `PHC_Enabled = true` are included:

```csharp
if (st.SiteCode == "PHC") 
{ 
    tdwork = tdwork.Where(x => x.PHC_Enabled).ToList(); 
}
```

This lets PHC have a slightly different column list than regular SAMMS sites for the same step.

### Rule 2 — Schema Version Affects Which Columns Are Skipped

`SelectConstructor.GetSLT()` skips specific columns based on the source table name and `IsNewSchema` flag:

| Source Table | Columns Skipped on Old Schema |
|---|---|
| `tblCodes` | `reqauth`, `obat`, `isprescreening`, `cde3pposoverride` |
| `tblUAResult` | `location_`, `location`, `scheduleddate`, `uabase64`, `uaprogram` |
| `tblUAResultDetail` | `uardfullnote`, `uardkey`, `uardnote` |
| `tblClinic` | `blasterwide`, `pumpcalibrate`, `checkvisitingpatient`, and others |

The `IsNewSchema` flag per site in `dms.vw_MapAction` controls this — newer SAMMS schema versions have more columns.

### Rule 3 — ActionKey Passed to Save Methods

Some `Save___()` methods receive `ActionKey` as a parameter because the same table gets loaded differently based on which pipeline group is calling it:

```csharp
// ActionKey tells SaveClientDemo1 whether to use EF Core path 1 or path 2
rCodes = sd.SaveClientDemo1var(SrcDt, st.SiteCode, st.ActionKey, null);

// ActionKey also differentiates SaveClientDemo2
rCodes = sd.SaveClientDemo2(SrcDt, st.SiteCode, st.ActionKey, null);

// ActionKey differentiates SaveEnrollment behavior
rCodes = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null);
```

### Rule 4 — Skip Rules in Scheduler Override Active Mappings

Even if `ActionKey + StepKey` exists and is enabled in `dms.tbl_MapAction`, the Scheduler can mark specific tasks as skipped (`RowState = 26`) after creation:

| Task Skipped | Condition |
|---|---|
| `pats.tbl_PayerClient` | SiteCode = LAB, ActionKey = 1, ActionStepKey = 6 |
| `pats.tbl_Cows_V6` | SiteCode = PHC, ActionKey = 1, ActionStepKey = 23 |
| `ayx.tbl_PreAdmission_V6` | SiteCode = PHC or LAB |
| `pats.tbl_EandMFormMDM` | SiteCode = PHC or LAB |
| `pats.tbl_EandMFormPregnancy` | SiteCode = PHC or LAB |
| `pats.tbl_Appointments` | SiteCode = LAB |
| `ayx.tbl_PreAdmission_V6` | SchemaVersion = 'V5' (old schema sites) |

---

## 9. How to Query the Full Map from the Database

Since the complete list lives in Azure BHG_DR, not in code, the SQL to get everything at once is:

```sql
-- Full ActionKey + StepKey map (all enabled steps)
SELECT 
    ma.ActionKey,
    ma.StepKey           AS ActionStepKey,
    ma.SiteCode,
    ma.SrcSchema + '.' + ma.FromTblVw  AS SourceTable,
    ma.DsnSchema + '.' + ma.DsnTbl     AS DestinationTable,
    ma.WhereCondition,
    ma.SortOrder,
    ma.IsNewSchema,
    ma.ReInitialize
FROM dms.tbl_MapAction ma
WHERE ma.Enabled = 1
ORDER BY ma.ActionKey, ma.StepKey, ma.SiteCode;
```

```sql
-- Count of StepKeys per ActionKey
SELECT ActionKey, COUNT(DISTINCT StepKey) AS StepCount
FROM dms.tbl_MapAction
WHERE Enabled = 1
GROUP BY ActionKey
ORDER BY ActionKey;
```

```sql
-- Full column list for a specific (ActionKey, StepKey)
SELECT ActionKey, ActionStepKey, FieldKey, FieldName, DsnFieldName, 
       FieldType, PrimaryKey, Enabled, PHC_Enabled
FROM dms.tbl_MapSrc2Dsn
WHERE Enabled = 1
  AND ActionKey = 1
  AND ActionStepKey = 9
ORDER BY FieldKey;
```

---

## 10. Summary — Why These Keys Exist

| Purpose | Explanation |
|---|---|
| **Metadata-driven design** | Adding a new table to the ETL does NOT require changing C# code — just insert rows into `dms.tbl_MapAction` and `dms.tbl_MapSrc2Dsn` with the right ActionKey + StepKey |
| **Column reuse** | Multiple sites can use the same `(ActionKey, StepKey)` mapping — they all get the same SELECT columns, just pointed at different source databases |
| **Schema version handling** | The same `(ActionKey, StepKey)` works for both old and new SAMMS schemas because `SelectConstructor` skips schema-specific columns dynamically |
| **Change detection** | `ActionKey = 3` disables checksum for tables that need full reload — all others use `RowChkSum` for efficient insert/update/skip |
| **PHC isolation** | `PHC_Enabled` column in `dms.tbl_MapSrc2Dsn` lets PHC sites see a subset of columns without needing separate step entries |
| **Pipeline routing** | `ActionKey` in `tsk.tbl_Schedule` links a schedule name (e.g., "Eastern ETL P1") to its set of `dms.tbl_MapAction` rows — everything flows from that |
