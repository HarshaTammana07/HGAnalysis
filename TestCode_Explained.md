# bhg.TestCode/Program.cs — Complete Explanation
**File:** `BCAppCode/BHG/bhg.TestCode/Program.cs`  
**Lines:** 239  
**Date:** 2026-03-25

---

## What Is This File?

This is a **developer sandbox / manual testing tool**. It is NOT part of the production ETL pipeline. It is a throwaway console application that a developer (Brian Catellier) used to:

1. **Test individual ETL steps manually** without running the full BHGTaskRunner pipeline
2. **Debug specific table extractions** by hardcoding a single task instead of pulling from the database
3. **Run one-off data backfills** for specific sites and year ranges
4. **Experiment with new logic** before adding it to BHGTaskRunner

> Think of it as a developer's "scratch pad" — a place to run a single piece of the ETL engine in isolation, point it at one specific site/table/date, and see what happens.

---

## Why This Is Valuable for Understanding the Full Flow

This file is actually **the clearest possible demonstration of the ETL engine** because:
- It uses the **exact same helpers** as BHGTaskRunner (`SelectConstructor`, `SQLSvrManager`, `SaveData`, `BulkDartsSvc`, `BHG_DRContext`)
- It skips the task queue entirely — no `tsk.tbl_Tasks2` involved
- You can see exactly what a single ETL step looks like **stripped of all the orchestration noise**
- All the commented-out blocks show the history of what scenarios were tested

---

## The Setup Block (Lines 1–24)

```csharp
// Initialize the SAME helpers used by BHGTaskRunner
BHG_DR_LIB.SelectConstructor sc  = new BHG_DR_LIB.SelectConstructor();  // builds SELECT queries
BHG_DR_LIB.SQLSvrManager     sm  = new BHG_DR_LIB.SQLSvrManager();       // runs SQL on any DB
BHG_DR_LIB.SaveData          sd  = new BHG_DR_LIB.SaveData();            // upsert logic
BHG_DR_LIB.BulkDartsSvc      bldr = new BHG_DR_LIB.BulkDartsSvc();       // bulk/staging loader
BHG_DR_LIB.Models.BHG_DRContext db = new BHG_DR_LIB.Models.BHG_DRContext(); // EF Core → Azure BHG_DR

DataTable SrcDt = new DataTable();   // will hold source data from SAMMS
string strFlds = "";                 // will hold the SELECT column list
string strCmd;                       // will hold the full SQL query
int RowsIns = 0;
int RowsUpd = 0;
```

These are identical to the first lines of `BHGTaskRunner/Program.cs`. Same objects, same purpose.

---

## Commented-Out Test Blocks — History of What Was Tested

### Test Block 1 — ZeroDollarDenials (Lines 26–32, commented out)

```csharp
// strCmd = "truncate table pats.tbl_vw_ZeroDollarDenials; " +
//          " insert into pats.tbl_vw_ZeroDollarDenials(...) " +
//          " SELECT ... FROM [pats].[vw_ZeroDollarDenials] ";
// sm.ExeSqlCmd(strCmd, sm.ConnectionString);
```

**What this tested:** A one-time population of a zero-dollar denials reporting table by running a TRUNCATE + INSERT...SELECT entirely within Azure BHG_DR. No SAMMS source involved — just an internal Azure SQL refresh.

**Why commented out:** Either it worked and was moved to AzureAgent/a stored procedure, or it was a one-time fix that is no longer needed.

---

### Test Block 2 — BAMMerge SP Test (Lines 35–46, commented out)

```csharp
// DataTable tblr = sm.ExecStrPro("pats.BAMMerge", "@sitecode", "B12B", sm.ConnectionString);
// foreach(DataRow r in tblr.Rows)
// {
//     if (r[0].ToString() == "UPDATE") { RowsUpd++; }
//     else { RowsIns++; }
// }
```

**What this tested:** Manually calling the `pats.BAMMerge` stored procedure for site `B12B` and counting how many rows it inserted vs updated. Used to verify the BAM score aggregation SP was working correctly.

**Why commented out:** SP was verified working, test no longer needed.

---

### Test Block 3 — DartsSrv Config (Lines 48–62, commented out)

```csharp
// VwTaskListMap st = new VwTaskListMap {
//     ActionKey = 1, ActionStepKey = 4,
//     SiteCode = "B12B",
//     WorkDate = DateTime.Parse("1/1/2020"),
//     FromTblVw = "tblDartsSrv",
//     WhereCondition = "convert(date,DsDtStart) = @WorkDate",
//     SrcSchema = "dbo",
//     TaskName = "pats.tbl_FormsSAMMSClient",
//     ConStr = @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;...SAMMS-ColoradoSpringsV5;"
// };
```

**What this tested:** A DartsSrv extraction for site B12B (Colorado Springs clinic) using ActionKey=1/StepKey=4. The developer was testing whether the DartsSrv column mapping and extraction worked for that specific site.

---

### Test Block 4 — PreAdmission V6 Config (Lines 80–90, commented out)

```csharp
// VwTaskListMap st = new VwTaskListMap {
//     ActionKey = 1, ActionStepKey = 24,
//     SiteCode = "V9", SchemaVersion = "V6",
//     WorkDate = DateTime.Parse("3/28/2023"),
//     FromTblVw = "SF_PatientPreAdmission",
//     WhereCondition = "len(pp.CreatedOn) > 0 and pp.ClientAddress not like '%test data%'",
//     SrcSchema = "dbo",
//     TaskName = "tbl_PreAdmission_V6",
//     ConStr = @"Data Source=BHGDALLSQL05\...SAMMS-NashvilleV5;"
// };
```

**What this tested:** PreAdmission V6 extraction for site V9 (Nashville). The WHERE condition `not like '%test data%'` shows the developer was trying to filter out test/dummy records entered during SAMMS setup. Date 3/28/2023 suggests this was run in late March 2023.

---

## The ACTIVE Test Configuration (Lines 51–79) — What Actually Runs

The one block that is **NOT commented out** is:

```csharp
BHG_DR_LIB.Models.VwTaskListMap st = new BHG_DR_LIB.Models.VwTaskListMap
{
    ActionKey     = 2,
    ActionStepKey = 7,
    SiteCode      = "Global",
    WorkDate      = DateTime.Parse("1/1/2020"),
    FromTblVw     = "tblFORMSSAMMSCLIENT",
    WhereCondition = "Year(convert(date, tpcCreatedDate)) = Year(@WorkDate)",
    SrcSchema     = "dbo",
    IsNewSchema   = false,
    TaskName      = "pats.tbl_FormsSAMMSClient",
    SortOrder     = "Order by 1, 2",
    ConStr        = @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;...SAMMSGLOBAL;"
};
```

**What this says, field by field:**

| Field | Value | What It Means |
|---|---|---|
| `ActionKey` | 2 | Forms mapping group |
| `ActionStepKey` | 7 | Step 7 within that group = FormsSAMMSClient |
| `SiteCode` | `"Global"` | All sites / no site filter |
| `WorkDate` | `1/1/2020` | Historical backfill starting from Jan 1 2020 |
| `FromTblVw` | `tblFORMSSAMMSCLIENT` | Source table name in SAMMS |
| `WhereCondition` | `Year(convert(date, tpcCreatedDate)) = Year(@WorkDate)` | Only 2020 records |
| `SrcSchema` | `dbo` | Standard SAMMS schema |
| `IsNewSchema` | `false` | Old SAMMS schema version |
| `TaskName` | `pats.tbl_FormsSAMMSClient` | Destination table in Azure BHG_DR |
| `ConStr` | `...SAMMSGLOBAL;` | Points to the SAMMSGLOBAL shared database on the Dallas SQL server |

This is a **manual backfill test** for `pats.tbl_FormsSAMMSClient` using the SAMMSGLOBAL source database (the global shared forms DB, not a site-specific one).

---

## The Column Mapping and SELECT Build (Lines 92–110)

```csharp
bool ChkSumEnabled = true;

// Step 1: Load column mappings from Azure BHG_DR metadata
List<VwMapSrc2Dsn> tdwork = db.WorkToDo
    .Where(x => x.Enabled
             && x.ActionKey      == st.ActionKey       // 2
             && x.ActionStepKey  == st.ActionStepKey)  // 7
    .ToList();

// Step 2: PHC special filter — not applicable here (SiteCode = "Global")
if (st.SiteCode == "PHC") { tdwork = tdwork.Where(x => x.PHC_Enabled).ToList(); }

// Step 3: Disable checksum if ActionKey = 3 (not this case)
if (st.ActionKey == 3) { ChkSumEnabled = false; } else { ChkSumEnabled = true; }

// Step 4: Build the SELECT field list using SelectConstructor
strFlds = sc.GetSLT(tdwork, ChkSumEnabled, st.IsNewSchema.Value, st.FromTblVw, st.SiteCode)
            .Replace("@SiteCode", "'Global'")
            .Replace("@Samms", "'SAMMS'");

// Step 5: Special override for FormsSAMMSClient — replace hardcoded 'Global'
// with a dynamic lookup of the clinic prefix from tblSites
if (st.TaskName.ToLower() == "pats.tbl_formssammsclient")
{
    strFlds = strFlds.Replace("'Global'", 
        "isnull((select Prefix from dbo.tblSites where sID = fscsite), 'Global')");
}

// Step 6: Assemble final SELECT query
strCmd = "Select " + strFlds + " from dbo.tblFORMSSAMMSCLIENT";

// Step 7: Replace date placeholder (WorkDate - 14 days)
string strWhere = st.WhereCondition
    .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(-14).ToShortDateString() + "'");
// Result: "Year(convert(date, tpcCreatedDate)) = Year('12/18/2019')"
// = records from year 2019 (since 1/1/2020 - 14 days = 2019)
```

This is **identical to what BHGTaskRunner does** for every child task. The developer copy-pasted the same logic here for testing.

---

## The Three Active Test Scenarios (Lines 112–235)

### Scenario 1 — DartsSrv Multi-Year Backfill (Lines 112–130)

```csharp
if ((st.ActionKey == 1) && (st.ActionStepKey == 4))
{
    // Extract 2019 DartsSrv records and save to year-2019 table
    string sstrCmd = strCmd + " Where Year(dsdtstart) = 2019 order by 1, 2";
    SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
    _ = sd.SaveDartSrv2019(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2019"), null);

    // Extract 2020 DartsSrv records
    sstrCmd = strCmd + " Where Year(dsdtstart) = 2020 order by 1, 2";
    SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
    _ = sd.SaveDartSrv2020(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2020"), null);

    // Extract 2021 DartsSrv records
    sstrCmd = strCmd + " Where Year(dsdtstart) = 2021 order by 1, 2";
    SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
    _ = sd.SaveDartSrv2021(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2021"), null);

    // Extract 2022 DartsSrv records
    sstrCmd = strCmd + " Where Year(dsdtstart) = 2022 order by 1, 2";
    SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
    _ = sd.SaveDartSrv2022(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2022"), null);

    // Extract 2023 DartsSrv records
    sstrCmd = strCmd + " Where Year(dsdtstart) = 2023 order by 1, 2";
    SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
    _ = sd.SaveDartSrv2023(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2023"), null);
}
```

**This scenario is NOT active** (ActionKey=2/StepKey=7 is set above, not 1/4).

**What it would do if activated:** A **5-year historical backfill** of DartsSrv (counseling service records) for years 2019–2023. It loops through 5 years, extracts each year's data from SAMMS, and writes to the corresponding year-partitioned table in Azure BHG_DR.

**Why this exists:** When a clinic first joins BHG or when DartsSrv data needs to be re-initialized from scratch, the daily ETL only pulls 15 days back. This test code was used to backfill all historical years at once manually.

---

### Scenario 2 — PreAdmission V6 Extraction with Column Check (Lines 131–185)

```csharp
if ((st.ActionKey == 1) && (st.ActionStepKey == 24))
{
    // STEP 1: Check if SF_PatientPreAdmission table EXISTS in the source
    SrcDt = sm.GetTableData(st.FromTblVw, 
        "select name from sys.tables t where upper(name) = 'SF_PatientPreAdmission'", 
        st.ConStr);

    // Only proceed if the table exists AND site is NOT on V5 schema
    if ((SrcDt.Rows.Count == 1) && (st.SchemaVersion != "V5"))
    {
        // STEP 2: Get the list of columns in SF_PatientPreAdmission
        DataTable tblCols = sm.GetTableData("Cols", 
            "select name, column_id from sys.all_columns c where c.object_id = ...", 
            st.ConStr);

        // STEP 3: Check if 'clientaddress' column exists (V6 schema indicator)
        if (lstCols.Where(x => x.ColName.ToLower() == "clientaddress").FirstOrDefault() != null)
        {
            // STEP 4: Build fully CUSTOM SELECT (NOT from dms.vw_MapSrc2Dsn)
            // This query manually maps all columns with boolean → 'Yes'/'No' conversions
            strCmd = "select SiteCode = '" + st.SiteCode + "', " +
                     "pp.id as PreAdmissionid, pp.PatientID as Clientid, clt.cltM4ID, " +
                     "pp.CreatedON, pp.Createdby, pp.PreAdmissionDate, " +
                     // RegistrationModeID (0/1/2) → 'Phone'/'Walk-In'/'By Appointment'
                     "RegistrationMode = Case when pp.RegistrationModeID = 0 then 'Phone' " +
                                            "when pp.RegistrationModeID = 1 then 'Walk-In' " +
                                            "when pp.RegistrationModeID = 2 then 'By Appointment' " +
                                            "else Cast(pp.RegistrationModeID as varchar) end, " +
                     // All boolean flags (1/0) → 'Yes'/'No'
                     "IsCurrentlyInOpiateProgram = Case when pp.IsCurrentlyInOpiateProgram = 1 then 'Yes' " +
                                                       "when pp.IsCurrentlyInOpiateProgram = 0 then 'No' " +
                                                       "else cast(pp.IsCurrentlyInOpiateProgram as varchar) end, " +
                     // ... (20+ more boolean conversions) ...
                     // CHECKSUM computed over key fields
                     "RowChkSum = CHECKSUM(pp.id, pp.PatientID, pp.LastUpdatedBy, " +
                                         "pp.LastUpdateOn, pp.PatientSignatureDate, " +
                                         "pp.DateofRelease, pp.Version, pp.IsDeleted, clt.cltM4ID) " +
                     "from SF_PatientPreAdmission PP " +
                     "left join [dbo].[SF_Program] pg on pp.ProgramID = pg.id " +
                     "left join [dbo].[tblCodes] tc on pp.ReferralSourceID = tc.cdeID " +
                     "left join [dbo].[tblCodes] tc2 on pp.SammsProgramID = tc2.cdeID " +
                     "left join dbo.tblClient clt on (pp.PatientID = clt.cltID) ";

            strCmd += " Where " + strWhere + " " + st.SortOrder;
            SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);

            // STEP 5: Save to ayx.tbl_PreAdmission_V6
            BHG_DR_LIB.Models.RCodes rCodes = sd.SavePreAdmissionV6(SrcDt, st.SiteCode, null);
        }
    }
}
```

**This scenario is NOT active** (ActionKey=2/StepKey=7 is set, not 1/24).

**What it demonstrates:** The PreAdmission V6 table requires special handling:
1. The table `SF_PatientPreAdmission` might not exist in older SAMMS versions → existence check first
2. The column `clientaddress` is only in newer V6 schemas → column check second
3. The SELECT query is **fully handcrafted** — NOT driven by `dms.vw_MapSrc2Dsn` like other tables
4. All 20+ boolean fields (0/1) get converted to human-readable `'Yes'`/`'No'` strings
5. Multiple JOIN tables needed: `SF_Program`, `tblCodes` (twice), `tblClient`

This is the most **complex transformation** in the codebase — this test code is where the developer worked it out before copying into `BHGTaskRunner/Program.cs`.

---

### Scenario 3 — FormsSAMMSClient Bulk Load (Lines 186–222) ← THE ACTIVE ONE

```csharp
if ((st.ActionKey == 2) && (st.ActionStepKey == 7))  // ← matches the active config above
{
    // Custom WHERE — forms from 2020 onward, excluding test sites (25, 38, 99, 100, 106, 115, 118)
    strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 38, 99, 100, 106, 115, 118) ";
    strCmd += strWhere + " order by 1, 2";

    // Extract from SAMMSGLOBAL source
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);

    // Load using BulkDartsSvc (SqlBulkCopy path, not EF Core)
    _ = bldr.BulkDartsSrvLoader(SrcDt, st.TaskName, st.SiteCode, st.WorkDate.Value, null);
}
```

**This IS the active scenario.**

**What it does:**
1. Extracts ALL FormsSAMMSClient records from `SAMMSGLOBAL` where `fscDATE > 12/31/2019`
2. Excludes known test/special sites by fscsite IDs: 25, 38, 99, 100, 106, 115, 118
3. Uses `BulkDartsSvc.BulkDartsSrvLoader()` — the SqlBulkCopy path
4. This means: bulk inserts into staging table → then calls `stg.FormsSAMMSMerge` stored procedure → merges into `pats.tbl_FormsSAMMSClient`

This was a **historical backfill of all SAMMS form completion records from 2020 onward** from the global SAMMS database.

The commented-out loop block (lines 206–221) shows an earlier attempt that ran **day by day** from a start date to end date — much slower. The active version pulls everything at once, which is faster but requires more memory.

---

### Scenario 4 — 3pElig Multi-Year Backfill (Lines 223–235)

```csharp
if ((st.ActionKey == 4) && (st.ActionStepKey == 1))
{
    // Backfill eligibility checks for years 2020, 2021, 2022, 2023
    DateTime dtWorkDate = DateTime.Parse("1/1/2020");

    // Year 2020
    string sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2020 " + st.SortOrder;
    SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
    _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate, true, null);

    // Year 2021
    sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2021 " + st.SortOrder;
    _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate.AddYears(1), true, null);  // ← BUG: reuses same SrcDt!

    // Year 2022
    sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2022 " + st.SortOrder;
    _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate.AddYears(2), true, null);  // ← BUG: reuses same SrcDt!

    // Year 2023
    sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2023 " + st.SortOrder;
    _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate.AddYears(3), true, null);  // ← BUG: reuses same SrcDt!
}
```

**This scenario is NOT active** (ActionKey=2/StepKey=7 is set, not 4/1).

**What it would do:** A 4-year historical backfill of `pats.tbl_3pElig` (third-party eligibility check records).

**Bug spotted:** For years 2021, 2022, and 2023, the code builds a new `sstrCmd` but **never runs it** — it calls `GetTableData` only for 2020 and reuses the same `SrcDt` for all years. This means years 2021–2023 would get 2020's data saved to them. This is clearly a test/quick-and-dirty script that was never production-ready. It shows why this code lives in TestCode and not in BHGTaskRunner.

---

## How This File Connects to the Full ETL Flow

```
PRODUCTION FLOW (BHGTaskRunner):
═══════════════════════════════
tsk.tbl_Tasks2 (task queue)
        │  read parent + child tasks
        ▼
foreach(parent task) → foreach(child task)
        │  each child task has: ActionKey, ActionStepKey, SiteCode, ConStr, etc.
        ▼
Load column mappings from dms.vw_MapSrc2Dsn
        │
        ▼
SelectConstructor.GetSLT() → build SELECT
        │
        ▼
SQLSvrManager.GetTableData() → query SAMMS
        │
        ▼
switch(TaskName) → call Save___() method
        │
        ▼
EF Core upsert OR BulkDartsSvc → Azure BHG_DR


TEST CODE FLOW (bhg.TestCode):
═══════════════════════════════
(No task queue — task is hardcoded manually)
VwTaskListMap st = new VwTaskListMap { ActionKey=2, StepKey=7, ... }
        │  manual object construction
        ▼
Load column mappings from dms.vw_MapSrc2Dsn  ← same step
        │
        ▼
SelectConstructor.GetSLT() → build SELECT     ← same step
        │
        ▼
SQLSvrManager.GetTableData() → query SAMMS    ← same step
        │
        ▼
if/else blocks (instead of switch)            ← simplified routing
  → Save___() OR BulkDartsSvc.BulkDartsSrvLoader()  ← same save methods
        │
        ▼
Azure BHG_DR updated                          ← same destination
```

The **only difference** between TestCode and BHGTaskRunner is:
- TestCode: task details are **hardcoded** directly in C# (you edit the file to change what runs)
- BHGTaskRunner: task details come **from the database** (`tsk.tbl_Tasks2` / `dms.vw_MapAction`)

---

## Summary

| Aspect | Detail |
|---|---|
| **Purpose** | Developer sandbox — manual testing and historical backfills |
| **Status** | NOT a production component — never deployed as a scheduled job |
| **Active test** | FormsSAMMSClient bulk backfill (ActionKey=2, StepKey=7) for SAMMSGLOBAL |
| **Commented tests** | DartsSrv backfill (2019–2023), PreAdmission V6 column-detection extraction, 3pElig multi-year backfill, BAMMerge SP test, ZeroDollarDenials refresh |
| **What it proves** | The ETL engine (SelectConstructor + SQLSvrManager + SaveData + BulkDartsSvc) is completely reusable — you can invoke any single ETL step just by manually constructing a `VwTaskListMap` object |
| **Known bug** | Scenario 4 (3pElig backfill) reuses the 2020 DataTable for years 2021–2023 instead of re-querying — never was production-ready |
| **Key insight** | This file is the BEST place to understand the ETL engine in isolation because it removes all orchestration complexity and shows exactly what one ETL step looks like |

---

## If You Wanted to Use This to Test Any Table

Here is how you would modify the active config block to test, say, `pats.tbl_Enrollment` for site B01:

```csharp
BHG_DR_LIB.Models.VwTaskListMap st = new BHG_DR_LIB.Models.VwTaskListMap
{
    ActionKey      = 1,       // ← look up from dms.vw_MapAction for Enrollment
    ActionStepKey  = 4,       // ← look up from dms.vw_MapAction for Enrollment
    SiteCode       = "B01",   // ← the clinic you want to test
    WorkDate       = DateTime.Parse("3/25/2026"),
    FromTblVw      = "tblEnrollment",       // ← SAMMS source table name
    WhereCondition = "LastModAt >= '@WorkDate'",
    SrcSchema      = "dbo",
    IsNewSchema    = false,
    TaskName       = "pats.tbl_Enrollment",  // ← Azure BHG_DR destination
    SortOrder      = "Order by 1",
    ConStr         = @"Data Source=B01-SQL;Initial Catalog=SAMMS_B01;User ID=sa;Password=***;"
};
// Then add the if block:
// SrcDt = sm.GetTableData(st.FromTblVw, strCmd + " Where " + strWhere, st.ConStr);
// var result = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null);
// Console.WriteLine($"Inserted: {result.RowsIns}, Updated: {result.RowsUpd}");
```
