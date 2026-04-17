# DartsSrv (DART) in BHG vs PHC — Complete Deep Dive

---

## Your Analysis — Verified ✓

Your findings are **100% correct**. Here is the code evidence for each point:

| Your Claim | Evidence in Code |
|---|---|
| PHC has its own dedicated executable | `PHC/Program.cs` is a separate project from `BHGTaskRunner/Program.cs` |
| `BulkDartsSvc.cs` exists in both | `BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs` AND `BCAppCode/PHC/BulkDartsSvc.cs` |
| `PHC_Enabled` flag controls column inclusion | `PHC/Program.cs` line 82: `if (st.SiteCode == "PHC") { tdwork = tdwork.Where(x => x.PHC_Enabled).ToList(); }` |
| DartsSrv not on PHC skip list | `PHC/Program.cs` line 526: `case "pats.tbl_dartssrv":` exists and is active (not commented out) |
| Same destination Azure tables | Both call `BulkDartsSrvLoader(... "stg.tbl_dartssrv" ...)` → same `stg.DartsSrvMerge` → same `pats.tbl_DartsSrv_20XX` |

---

## My Additional Findings — What You Didn't Mention

### Finding 1 — BHGTaskRunner Explicitly Excludes PHC

`BHGTaskRunner/Program.cs` line 21–26:
```csharp
// BHGTaskRunner ALWAYS excludes PHC from its task queue
List<VwTaskListMap> Tasks = db.VwTaskList
    .Where(x => x.SiteCode != "PHC"   // ← hard exclusion
             && x.Status == 17
             && x.RunAt < DateTime.Now)
    .ToList();
```
PHC is **never processed by `BHGTaskRunner.exe`**. It runs through `PHC/Program.cs` exclusively.

---

### Finding 2 — Different Lookback Windows for DartsSrv

This is a **real behavioral difference** between the two systems:

**BHG (`BHGTaskRunner/Program.cs` lines 866–892):**
```csharp
int offsetvalue = -15;  // default

if (st.WorkDate.Value.DayOfWeek == DayOfWeek.Friday)
{
    if (st.WorkDate.Value.Month == st.WorkDate.Value.AddDays(1).Month)
        offsetvalue = -90;   // month-end Friday → look back 90 days
    if (st.WorkDate.Value.Date == DateTime.Parse("1/24/2025"))
        offsetvalue = -200;  // special override → 200 days
}

// WHERE uses DYNAMIC offset (-15 / -90 / -200)
// Also requires: dsClt is not null
```

**PHC (`PHC/Program.cs` lines 527–533):**
```csharp
// FIXED -14 days. No dynamic logic. No dsClt filter.
strCmd += " Where convert(date,dsdtstart) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
    + "' or convert(date,dsDtAdded) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
    + "' or convert(date,dsUpdate) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
    + "' or convert(date,dsBilled) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
    + "' or convert(date,dsSigDate) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
    + "' or dsClt <= 0 order by 1, 2";
```

| | BHG | PHC |
|---|---|---|
| Normal lookback | -15 days | -14 days (hardcoded) |
| Month-end Friday | -90 days | No special logic |
| Special date override | -200 days | No special logic |
| `dsClt is not null` guard | Yes | No (only `dsClt <= 0` catch) |

---

### Finding 3 — ServiceType Column Guard (BHG Only)

**BHG** (`BHGTaskRunner/Program.cs` lines 880–884):
```csharp
// Check if older SAMMS versions have the ServiceType column
DataTable tblDartcols = sm.GetTableData("tcols",
    "select name, column_id from sys.all_columns c where c.object_id = " +
    "(select object_id from sys.all_objects where upper(name) = 'TBLDARTSSRV') and name = 'ServiceType'",
    st.ConStr);

if ((tblDartcols.Rows.Count == 0))
{
    strCmd = strCmd.Replace(", [ServiceType] ServiceType", "").Replace(", [ServiceType]", "");
}
```

**PHC** (`PHC/Program.cs`): **No such check**. PHC assumes `ServiceType` always exists. This means PHC clinics must all be on a SAMMS version that includes this column.

---

### Finding 4 — Stored Procedure Coverage (Bigger Difference Than Expected)

**BHG `BulkDartsSvc.cs` (lines 291–299):**
```csharp
case "stg.tbl_dartssrv":
    rst.RowsProcessed  = sm.ExeSqlCmd("exec stg.DartsSrvMerge",   sm.ConnectionString); // 2014-2021
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge22", sm.ConnectionString); // 2022
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge23", sm.ConnectionString); // 2023
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge24", sm.ConnectionString); // 2024
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge25", sm.ConnectionString); // 2025
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge26", sm.ConnectionString); // 2026
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge27", sm.ConnectionString); // 2027
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge28", sm.ConnectionString); // 2028
    break;
```

**PHC `BulkDartsSvc.cs` (lines 287–292):**
```csharp
case "stg.tbl_dartssrv":
    rst.RowsProcessed  = sm.ExeSqlCmd("exec stg.DartsSrvMerge",   sm.ConnectionString); // 2014-2021
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge22", sm.ConnectionString); // 2022
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge23", sm.ConnectionString); // 2023
    rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge24", sm.ConnectionString); // 2024
    break;
```

| | BHG | PHC |
|---|---|---|
| Merge SPs called | 8 (Merge + 22–28) | 4 (Merge + 22–24) |
| Covers through year | 2028 | 2024 |
| Future-proofed | Yes | Needs update as years pass |

**This is a maintenance risk for PHC.** When sessions dated 2025+ come in for PHC clinics, `stg.DartsSrvMerge25` will not be called and those rows will silently be left in the staging table without landing in the final year table.

---

### Finding 5 — PHC SelectConstructor Stops at 2022 (EF Core Path)

**BHG `SelectConstructor.cs` (lines 513–549):** EF Core backfill covers **2014–2022**
```csharp
case "tbl_dartssrv":
    switch (st.WrkYear)
    {
        case "2014": x = sd.SaveDartSrv2014(...)
        case "2015": x = sd.SaveDartSrv2015(...)
        ...
        case "2022": x = sd.SaveDartSrv2022(...)  // BHG goes to 2022
    }
```

**PHC `SelectConstructor.cs` (lines 507–541):** EF Core backfill also covers **2014–2022**, but **no 2023 case**:
```csharp
case "tbl_dartssrv":
    switch (st.WrkYear)
    {
        case "2014": x = sd.SaveDartSrv2014(...)
        ...
        case "2022": x = sd.SaveDartSrv2022(...)  // PHC stops here — no 2023
    }
```
PHC's EF Core historical backfill path has **no 2023 support** for DartsSrv.

---

### Finding 6 — PHC Has a Batched DartsSrv Audit Query

Both systems call `SaveRowTrax()` after loading DartsSrv to compare source vs destination row counts. But the audit source count query is **identical**:
```csharp
// Both BHG and PHC use same audit query
sm.GetTableData("tbllcl",
    "select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw
    + " where dsClt > 0 and (dsDtAdded > '12/31/2018' or dsDtStart = '12/31/2018')",
    st.ConStr)
```
The cutoff date `12/31/2018` is hardcoded in both — meaning rows added before 2019 are excluded from the audit count in both pipelines.

---

### Finding 7 — PHC Has Site-Grouping Logic in YearlyAuditData

PHC `SelectConstructor.cs` has a special batching system for DartsSrv historical audits — it groups sites by prefix (lines 273–302):
```csharp
if (tblname == "tbl_dartssrv")
{
    switch (sites.ToLower())
    {
        case "b1": // All sites starting with "B" (e.g., B01, B02...)
        case "b2": // All non-B, non-V sites
        case "b3": // All sites starting with "V" (e.g., V10, VMIN...)
        default:   // Specific site
    }
}
```
This batching logic **does not exist in BHG's SelectConstructor**. PHC added it specifically to handle year-by-year historical audits across site groups.

---

## Complete Side-by-Side Comparison

| Feature | BHG (`BHGTaskRunner`) | PHC (`PHC/Program.cs`) |
|---|---|---|
| **Executable** | `BHGTaskRunner.exe 9` | `PHC/Program.exe` (arg 1/2/3) |
| **Task filter** | `SiteCode != "PHC"` | PHC-specific tasks (includes PHC-flagged sites) |
| **PHC_Enabled filter** | Yes (line 117) | Yes (line 82) |
| **DartsSrv active** | Yes | Yes |
| **Lookback window** | Dynamic: -15 / -90 / -200 days | Fixed: -14 days always |
| **`dsClt is not null` guard** | Yes | No |
| **ServiceType column check** | Yes — dynamic strip if missing | No — assumes column exists |
| **Merge SPs called** | 8 (through 2028) | 4 (through 2024) |
| **EF Core backfill years** | 2014–2022 | 2014–2022 (no 2023) |
| **YearlyAudit site grouping** | No | Yes (b1/b2/b3 groups) |
| **Same staging table** | `stg.tbl_dartssrv` | `stg.tbl_dartssrv` (same) |
| **Same destination tables** | `pats.tbl_DartsSrv_20XX` | `pats.tbl_DartsSrv_20XX` (same) |

---

## The Full Flow — Both Systems Together

```
                    ┌─────────────────────────────────────────────────┐
                    │            SAMMS SQL Server Databases            │
                    │         dbo.tblDartsSrv (counseling records)     │
                    └─────────────┬───────────────────────┬───────────┘
                                  │                       │
             SiteCode != "PHC"    │                       │  PHC clinics
                                  │                       │
               ┌──────────────────▼──────┐   ┌───────────▼──────────┐
               │  BHGTaskRunner.exe 9    │   │   PHC/Program.exe    │
               │  (SAMMS-ETL-DartSvc)   │   │   (Schedule 1/2/3)   │
               │                        │   │                       │
               │  WHERE lookback:        │   │  WHERE lookback:      │
               │  -15 / -90 / -200 days  │   │  FIXED -14 days       │
               │  + ServiceType guard    │   │  No ServiceType check │
               └──────────┬─────────────┘   └──────────┬────────────┘
                          │                             │
              BHG-DR-LIB/BulkDartsSvc.cs         PHC/BulkDartsSvc.cs
                          │                             │
                          │ SqlBulkCopy                 │ SqlBulkCopy
                          │                             │
                          └──────────────┬──────────────┘
                                         │
                                         ▼
                              stg.tbl_dartssrv  (Azure BHG_DR)
                              ─────────────────────────────────
                                         │
                    BHG calls 8 SPs:     │     PHC calls 4 SPs:
                    DartsSrvMerge        │     DartsSrvMerge
                    DartsSrvMerge22      │     DartsSrvMerge22
                    DartsSrvMerge23      │     DartsSrvMerge23
                    DartsSrvMerge24      │     DartsSrvMerge24
                    DartsSrvMerge25      │     (stops here)
                    DartsSrvMerge26      │
                    DartsSrvMerge27      │
                    DartsSrvMerge28      │
                                         │
                                         ▼
                 pats.tbl_DartsSrv        ← years 2014 and before
                 pats.tbl_DartsSrv_2015
                 pats.tbl_DartsSrv_2016
                 pats.tbl_DartsSrv_2017
                 pats.tbl_DartsSrv_2018
                 pats.tbl_DartsSrv_2019
                 pats.tbl_DartsSrv_2020
                 pats.tbl_DartsSrv_2021
                 pats.tbl_DartsSrv_2022
                 pats.tbl_DartsSrv_2023
                 pats.tbl_DartsSrv_2024
                 (all clinics — BHG and PHC — land here together)
```

---

## Key Risks and Observations

### Risk 1 — PHC Merge SP Coverage Gap
PHC's `BulkDartsSvc.cs` only calls merge SPs through 2024. Any DartsSrv session with `dsDtStart` in 2025+ for a PHC clinic will bulk-load into the staging table but **no merge SP will fire to move it to the final year table**. It will be silently truncated at cleanup.

### Risk 2 — PHC Fixed Lookback May Miss Records
BHG expands to -90 days on month-end Fridays to catch billing corrections on old sessions. PHC always uses -14 days. PHC billing staff can correct sessions older than 14 days and the next ETL run **will not pick those corrections up**.

### Risk 3 — ServiceType Assumption in PHC
PHC does not check if `ServiceType` exists before building the SELECT. If any PHC site is ever on an older SAMMS version that lacks this column, the ETL will fail with a SQL error instead of gracefully stripping the column.

### Design Insight — Shared Destination, Independent Pipelines
Despite the two separate pipelines, **both write to the exact same Azure BHG_DR tables**. The `SiteCode` column on every row (e.g., `SiteCode = 'B01'` or `SiteCode = 'PHC'`) is what separates BHG data from PHC data in the shared warehouse. Reports can therefore query all clinics from a single table.
