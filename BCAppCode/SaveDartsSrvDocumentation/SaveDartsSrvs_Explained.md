# SaveDartsSrvs.cs — Complete Explanation & All the Dots Connected

---

## What Is DartsSrv? (The Data)

**"DartsSrv"** = **"DARTS Service"** — these are **counseling session records**.

Every time a patient at a clinic has a counseling session with a staff member, SAMMS creates one row in the `tblDartsSrv` table. This is one of the highest-volume tables in the system — each clinic can have tens of thousands of these records going back to 2014.

---

## Why Is the File So Long? (1,691 Lines)

Because DartsSrv is **split into separate tables by year** in Azure BHG_DR.  
There is **one method per year**, and each method is ~160 lines:

| Method | Lines | Writes To (Azure BHG_DR) |
|---|---|---|
| `SaveDartSrv2014()` | 12–174 | `pats.tbl_DartsSrv_2014B4` |
| `SaveDartSrv2015()` | 175–337 | `pats.tbl_DartsSrv_2015` |
| `SaveDartSrv2016()` | 338–500 | `pats.tbl_DartsSrv_2016` |
| `SaveDartSrv2017()` | 501–667 | `pats.tbl_DartsSrv_2017` |
| `SaveDartSrv2018()` | 668–834 | `pats.tbl_DartsSrv_2018` |
| `SaveDartSrv2019()` | 835–1002 | `pats.tbl_DartsSrv_2019` |
| `SaveDartSrv2020()` | 1003–1185 | `pats.tbl_DartsSrv_2020` |
| `SaveDartSrv2021()` | 1186–1360 | `pats.tbl_DartsSrv_2021` |
| `SaveDartSrv2022()` | 1361–1524 | `pats.tbl_DartsSrv_2022` |
| `SaveDartSrv2023()` | 1525–1691 | `pats.tbl_DartsSrv_2023` |

> All 10 methods contain the **same logic** — only the EF Core model class and Azure table name differ.

---

## The Complete Dot-to-Dot Connection

```
SAMMS (Source)                    BHG-DR-LIB (Library)                Azure BHG_DR (Destination)
══════════════════════════════════════════════════════════════════════════════════════════════════

Clinic B01 SQL Server             BHGTaskRunner\Program.cs             pats.tbl_DartsSrv_2022
  dbo.tblDartsSrv                                                      ─────────────────────
  ───────────────         STEP 1: case "pats.tbl_dartssrv":            dsID (PK)
  dsID      → int         Check if ServiceType column exists           SiteCode (PK)
  dsClt     → int         in source. If not → strip from SELECT.       dsClt
  dsTxtSrv  → varchar     Build WHERE with dynamic lookback:           dsTxtSrv
  dsDtStart → datetime       -15 days normally                         dsDtStart
  dsDtEnd   → datetime       -90 days on Fridays (month-end)           dsDtEnd
  dsdblUnits→ double         -200 days on special date                 dsdblUnits
  DsDim1-6  → bit                                                      DsDim1..6
  dsSigDate → datetime    STEP 2: Execute SELECT on SAMMS              dsSigDate
  dsUpdate  → datetime    sm.GetTableData("tblDartsSrv",               dsUpdate
  DsBilled  → datetime       strCmd,                                   DsBilled
  ...50+ cols...             st.ConStr) ← B01's connection string      ...50+ cols...
                          → Returns DataTable (SrcDt)
                                                                        Model class:
                          STEP 3: Call BulkDartsSrvLoader()            TblDartsSrv.cs
                          bldr.BulkDartsSrvLoader(                     [Table("tbl_DartsSrv_2014B4")]
                              SrcDt,                                   [Table("tbl_DartsSrv_2022")]
                              "stg.tbl_dartssrv",   ← staging          etc.
                              "B01",
                              DartsDate,
                              null)
                                │
                                ▼
                          SqlBulkCopy.WriteToServer()
                          → Bulk inserts ALL rows into
                            stg.tbl_dartssrv (staging table)
                                │
                                ▼
                          sm.ExeSqlCmd("exec stg.DartsSrvMerge22")
                          → Stored procedure runs MERGE:
                            WHEN MATCHED AND changed → UPDATE
                            WHEN NOT MATCHED        → INSERT
                            into pats.tbl_DartsSrv_2022
```

---

## Inside `SaveDartSrv2022()` — The EF Core Path (Step by Step)

For some sites, the EF Core upsert method (`SaveDartSrv20XX`) is called directly instead of the bulk loader. Here is exactly what happens:

### Step 1 — Load All Existing Azure Rows for This Site

```csharp
List<TblDartsSrv_2022> darts = db.TblDartsSrv2022
    .Where(x => x.SiteCode == "B01")
    .ToList();

// If Azure has zero rows for this site → flag AllNewRows=true
if (darts.Count == 0) { AllNewRows = true; }
```

### Step 2 — Loop Through Every Row from SAMMS

```csharp
foreach (DataRow r in tbl.Rows)
{
    int dsID        = int.Parse(r["dsid"].ToString());
    int newChkSum   = int.Parse(r["RowChkSum"].ToString());

    if (AllNewRows)
    {
        // Brand new site — create object, skip lookup
        dart = new TblDartsSrv_2022();
        dart.SiteCode = "B01";
        dart.DsId = dsID;
        NewRow = true;
    }
    else
    {
        // Try to find existing Azure row by PK (SiteCode + DsId)
        dart = darts.Where(x => x.DsId == dsID).FirstOrDefault();
        if (dart == null) { dart = new TblDartsSrv_2022(); NewRow = true; }
    }
```

### Step 3 — Only Write If Checksum Changed or It's a New Row

```csharp
    if ((dart.RowChkSum != newChkSum) || NewRow)
    {
        // Map EVERY column from DataRow → C# object
        dart.RowChkSum = newChkSum;
        dart.DsClt     = int.Parse(r["DsClt"].ToString());
        dart.DsDim1    = bool.Parse(r["DsDim1"].ToString());
        dart.DsTxtSrv  = r["DsTxtSrv"].ToString();
        dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString());
        // ... 50+ columns mapped one by one ...
        dart.LastModAt = DateTime.Now;

        if (NewRow) { DSNew.Add(dart); }  // collect new rows separately
    }
    // If checksum matches → do NOTHING (no unnecessary write)
}
```

### Step 4 & 5 — Commit Changes

```csharp
db.SaveChanges();                // saves all modified existing rows (UPDATEs)

if (DSNew.Count > 0)
{
    db.TblDartsSrv2022.AddRange(DSNew);
}
db.SaveChanges();                // saves all new rows (INSERTs)
```

---

## The Special WHERE Clause — Dynamic Lookback Logic

One of the most important things in `BHGTaskRunner` for DartsSrv is this lookback logic:

```csharp
int offsetvalue = -15;  // default: look back 15 days

// On Fridays at end of month → look back 90 days
if (st.WorkDate.Value.DayOfWeek == DayOfWeek.Friday)
{
    if (st.WorkDate.Value.Month == st.WorkDate.Value.AddDays(1).Month)
        offsetvalue = -90;

    // Special case: Jan 24 2025 → look back 200 days (one-time manual fix)
    if (st.WorkDate.Value.Date == DateTime.Parse("1/24/2025"))
        offsetvalue = -200;
}
```

The WHERE clause catches **any record touched recently across multiple date fields**:

```sql
WHERE dsClt IS NOT NULL
  AND (
    convert(date, dsdtstart) >= '3/10/2026'    -- session start date
    OR convert(date, dsDtAdded) >= '3/10/2026' -- when record was added
    OR convert(date, dsUpdate) >= '3/10/2026'  -- when record was last updated
    OR convert(date, dsBilled) >= '3/10/2026'  -- when it was billed
    OR convert(date, dsSigDate) >= '3/10/2026' -- when it was signed
    OR dsClt <= 0                              -- placeholder/invalid records
  )
```

**Why multiple date fields?**  
A counseling session might have been done weeks ago but **billed today** or **signed today** — any of those changes means the row needs to be picked up and synced.

---

## The Full File-to-File Connection Map

```
FILE                            ROLE                            CONNECTS TO
════════════════════════════════════════════════════════════════════════════════

dbo.tblDartsSrv (SAMMS)
  └── Source table               One row per counseling session   Read by SQLSvrManager
         │
         │  Column list built by:
         ▼
SelectConstructor.cs
  └── Builds SELECT list         Reads dms.tbl_MapSrc2Dsn        Columns: dsID, dsClt, dsTxtSrv...
  └── Adds CHECKSUM(...)         ActionKey=9                      Produces: strCmd
         │
         │  strCmd (SELECT statement)
         ▼
SQLSvrManager.cs
  └── .GetTableData()            Fires SELECT against SAMMS       Uses st.ConStr (clinic connection)
  └── Returns DataTable (SrcDt)  from ctrl.tbl_LocationCons       80+ different clinic servers
         │
         │  DataTable with raw rows
         ▼
BHGTaskRunner\Program.cs
  └── case "pats.tbl_dartssrv":  Routes to write path
       ├── Builds WHERE clause    Dynamic date lookback (-15/-90/-200 days)
       ├── Checks ServiceType     Column existence guard
       │
       ├── PATH A: BulkDartsSrvLoader()   ← most sites (bulk)
       │       │
       │       ▼
       │   BulkDartsSvc.cs
       │   └── SqlBulkCopy           Bulk inserts into stg.tbl_dartssrv
       │   └── exec stg.DartsSrvMerge22    Stored Procedure MERGE
       │       └── MERGE into pats.tbl_DartsSrv_2022
       │
       └── PATH B: SaveDartSrv202X()      ← some sites (EF Core)
               │
               ▼
           SaveDartsSrvs.cs
           └── 10 methods (2014–2023)
           └── SaveDartSrv2022()
                   │
                   │  Uses EF Core model:
                   ▼
               TblDartsSrv.cs
               [Table("tbl_DartsSrv_2022", Schema = "pats")]
                   │
                   ▼
               db.SaveChanges()
                   │
                   ▼
               pats.tbl_DartsSrv_2022  (Azure BHG_DR — final destination)
```

---

## Why Year-Partitioned Tables?

DartsSrv is the **largest dataset** in the system. Having one table for all years would be tens of millions of rows and extremely slow to query. Splitting by year means:

- Queries for "2022 counseling sessions" hit only `pats.tbl_DartsSrv_2022` — small and fast
- Each year table is independently indexed
- Reports for a specific year never scan other years' data
- New year = new table, with no changes to old ones

**Volume estimate:**  
80+ clinics × ~5,000 sessions/year/clinic × 10 years = **~4 million rows** across all year tables.

---

## All 11 ETL Schedules — Where DartsSrv Fits

`BHGTaskRunner.exe` accepts a single command-line argument (1–11) to determine which pipeline to run. Here is the complete map directly from `BHGTaskRunner\Program.cs` lines 30–73:

| Arg | TaskName Filter | What It Processes | SaveDartsSrvs.cs Used? |
|---|---|---|---|
| `1` | `SAMMSGlobal` | Global SAMMS reference data — clinic setup, codes, payers | No |
| `2` | `Central/Eastern/Mountain/Pacific ETL P1` | Main patient/clinical data **Phase 1** — 4 time-zones run in parallel | No |
| `3` | Everything **except** P1, P2, SAMMSGlobal | Catch-all for miscellaneous non-phased tasks | No |
| `4` | `Central/Eastern/Mountain/Pacific ETL P2` | Main patient/clinical data **Phase 2** — runs after P1 | No |
| `5` | `Samms-LAB` | Laboratory test results | No |
| `6` | `Samms-Forms` | Patient assessment forms and questionnaires | No |
| `7` | `SAMMS-ETL-Notes` | Clinical counselor notes | No |
| `8` | `SAMMS-ETL-INV` | Insurance/inventory data | No |
| **`9`** | **`SAMMS-ETL-DartSvc`** | **Counseling session records (DartsSrv)** | **YES — this is it** |
| `10` | `SAMMS-ETL-Dose` | Medication/dose administration records | No |
| `11` | `SAMMS-ETL-Orders` | Prescription orders | No |

> **`SaveDartsSrvs.cs` is only ever invoked as part of Schedule 9.**

---

## How Schedule 9 Attaches to SaveDartsSrvs.cs (Step by Step)

```
Windows Task Scheduler / Server
     │
     │  Runs:
     ▼
BHGTaskRunner.exe  9
     │
     │  args[0] = "9"  →  switch case "9":
     │
     ▼
db.VwTaskList.Where(x => x.TaskName == "SAMMS-ETL-DartSvc"
                      && x.Status == 17
                      && x.RunAt < DateTime.Now)
     │
     │  Returns parent tasks (one "SAMMS-ETL-DartSvc" per time zone or batch)
     ▼
foreach (pt in pTasks)       ← loop over parent tasks
     │
     │  For each parent, load its child tasks:
     ▼
Tasks.Where(x => x.ParentTaskId == pt.TaskId)
     │
     │  Each child task has:
     │    TaskName  = "pats.tbl_dartssrv"
     │    ActionKey = 9
     │    SiteCode  = "B01" (one row per clinic)
     │    WorkDate  = today
     ▼
foreach (st in child tasks)  ← one iteration per clinic
     │
     │  Build SELECT from dms.tbl_MapSrc2Dsn (ActionKey=9)
     │  Build WHERE with dynamic date lookback (-15/-90/-200 days)
     │  Fire SELECT against SAMMS clinic SQL Server
     │
     ▼
case "pats.tbl_dartssrv":
     │
     ├──► PATH A (normal/daily):
     │    bldr.BulkDartsSrvLoader()
     │         → SqlBulkCopy into stg.tbl_dartssrv
     │         → exec stg.DartsSrvMerge22 (stored procedure)
     │         → MERGE into pats.tbl_DartsSrv_2022
     │
     └──► PATH B (initial load / SelectConstructor path):
          SelectConstructor.cs → case "tbl_dartssrv":
               switch (st.WrkYear)
               {
                   case "2014": sd.SaveDartSrv2014(...)
                   case "2015": sd.SaveDartSrv2015(...)
                   ...
                   case "2022": sd.SaveDartSrv2022(...)  ← SaveDartsSrvs.cs
               }
```

---

## When Exactly Is SaveDartsSrvs.cs Called vs. BulkDartsSrvLoader?

| Scenario | Method Called | File |
|---|---|---|
| **Daily incremental sync** (normal run) | `BulkDartsSrvLoader()` | `BulkDartsSvc.cs` |
| **Historical / year-specific backfill** via `SelectConstructor` | `sd.SaveDartSrv202X()` | `SaveDartsSrvs.cs` |
| **Developer test / manual one-off** via `bhg.TestCode` | `sd.SaveDartSrv2019-2023()` directly | `SaveDartsSrvs.cs` |

The **daily ETL (Schedule 9)** primarily uses `BulkDartsSrvLoader` for speed. `SaveDartsSrvs.cs` is the **historical EF Core path** — used when loading a specific year's data for the first time, or when `SelectConstructor` routes by `WrkYear`.

---

## Which Pipeline Triggers This?

DartsSrv is processed under **Schedule 9 — SAMMS-ETL-DartSvc**:

```
BHGTaskRunner.exe 9
     │
     ▼
tsk.tbl_Schedule (ScheduleId = 9, Name = "SAMMS-ETL-DartSvc")
     │
     ▼
tsk.tbl_Tasks2 (ParentTask: "SAMMS-ETL-DartSvc")
     └── Child tasks: one per site (B01, B02, B03... × 80+ clinics)
             TaskName = "pats.tbl_dartssrv"
             ActionKey = 9
             SiteCode  = "B01"
             WorkDate  = today
```

---

## Summary — One Sentence Per File

| File | What It Does for DartsSrv |
|---|---|
| `dbo.tblDartsSrv` (SAMMS) | Source — one row per counseling session at each clinic |
| `dms.tbl_MapSrc2Dsn` (Azure DB) | Defines which 50+ columns to SELECT from source |
| `SelectConstructor.cs` | Assembles the SELECT column list + `CHECKSUM(...)` |
| `SQLSvrManager.cs` | Fires the SELECT against the clinic's SAMMS SQL Server |
| `BHGTaskRunner\Program.cs` | Routes to write path, builds dynamic WHERE with date lookback |
| `BulkDartsSvc.cs` | Bulk-inserts into `stg.tbl_dartssrv` staging table |
| `stg.DartsSrvMerge` (Stored Proc) | MERGE from staging into `pats.tbl_DartsSrv_202X` |
| `SaveDartsSrvs.cs` | EF Core path — 10 methods (2014–2023), each upserts into its year-table |
| `TblDartsSrv.cs` (Model) | C# class mapped to `pats.tbl_DartsSrv_2014B4` (one per year) |
| `pats.tbl_DartsSrv_202X` (Azure) | Final destination — all counseling sessions from all clinics by year |

---

## Key Design Decisions Explained

| Decision | Why |
|---|---|
| Year-partitioned tables | DartsSrv is the highest-volume table; year split keeps each table fast |
| `RowChkSum` checksum | Avoids re-writing unchanged rows — huge performance gain at 80+ sites |
| Multiple date fields in WHERE | A session can be touched (billed, signed, updated) on a different day than it started |
| -90 day lookback on Fridays | Month-end billing corrections often update old records — need wider window |
| `AllNewRows` flag | Skips slow row-by-row lookup if Azure has no data yet for a site (first-time load) |
| `ServiceType` column guard | Not all SAMMS versions have this column — code adapts dynamically rather than failing |
| Bulk path vs EF Core path | SqlBulkCopy is faster for large sites; EF Core is more precise for smaller/special sites |
