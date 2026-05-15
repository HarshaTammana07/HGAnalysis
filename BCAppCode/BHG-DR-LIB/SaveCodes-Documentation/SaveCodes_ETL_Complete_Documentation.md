
Codes ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 6 — Samms-Forms / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the end-to-end process used to extract clinical service and
program code reference records from local SAMMS SQL Server databases at each clinic and
load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What codes data is and why it is critical to the warehouse
- The two overloads of SaveCodes and their key differences
- Which overload BHGTaskRunner calls and which is legacy/unused
- How BHGTaskRunner dispatches this task
- The AllNewRows fast path and effective RowChkSum guard shared by both overloads
- The difference in commit strategy: `UpdateRange` (overload 1) vs. EF change tracking
  with no explicit `UpdateRange` (overload 2)
- The PHC SiteId hardcode present only in overload 1
- All known anomalies, bugs, and design notes

There are two overloads in SaveCodes.cs, both targeting the same Azure table:

pats.tbl_Codes:   SaveCodes(tbl, sc, PYear, db)  — Overload 1 (legacy, returns bool)
pats.tbl_Codes:   SaveCodes(tbl, sc, db)          — Overload 2 (active, returns RCodes)
________________________________________

2. High-Level Business Summary

What are codes?

`pats.tbl_Codes` holds the clinic-specific service and program code lookup table — the master
list of codes that drive billing, UA scheduling, counseling documentation, and discharge
classification in SAMMS. Each code (`CdeId`, `CdeDesc`) belongs to a group (`CdeGroup`) and
carries configuration flags such as:
- `CdeBillable` / `CdeBillableResidential` — whether the service generates a bill
- `CdeUA` — whether the code schedules a UA test
- `CdeDrugfree` — drug-free program indicator
- `CdeLiquid` / `Suboxoneprog` — medication modality flags
- `CdeDischargeType` — discharge classification
- `CdeSigRequired` — whether a patient signature is required
- `ReqAuth` — whether prior authorisation is required
- `OBAT` — office-based addiction treatment indicator
- Financial: `CdeIntAmt`, `DuiAmt`, `DuiHourRate`, `DefRate`, `WeeklyFee`

Because `pats.tbl_Codes` is referenced by downstream ETL (e.g., billing lookups, counseling
session validation), it needs to stay current with each clinic's configured code list.

Load type
Both overloads use the same EF Core upsert pattern: full site slice pre-load, AllNewRows
fast path, effective RowChkSum guard, ~30-field column switch, new rows added to the
in-memory list and then committed. The commit strategy differs (see Section 6).
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule / tsk.tbl_Tasks (Azure)  Task queue — ActionKey drives schedule selection
BHGTaskRunner.exe 6 / 3                   Samms-Forms / catch-all (verify in tsk.tbl_Tasks)
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)            Column list + RowChkSum expression
SelectConstructor.cs                       Builds `strFlds` / `strWhere` (rolling lookback)
SQLSvrManager.cs                          Executes SELECT against SAMMS SQL Server
SaveCodes.cs (BHG-DR-LIB)               2 overloads — both target pats.tbl_Codes
Models/TblCodes.cs                        EF entity → pats.tbl_Codes
pats.tbl_Codes (Azure BHG_DR)           Final destination for clinic code reference data
tsk.tbl_RowTrax (Azure DB)               Audit log — RowTrax block present but empty
________________________________________

4. The Two SaveCodes Overloads — Key Differences

Aspect                    Overload 1 (Legacy)                    Overload 2 (Active — BHGTaskRunner calls this)
------                    -------------------                    ----------------------------------------------
Signature                 SaveCodes(tbl, sc, bool PYear, db)     SaveCodes(tbl, sc, db)
Return type               bool                                   Models.RCodes
Called by BHGTaskRunner   No                                     Yes — sd.SaveCodes(SrcDt, st.SiteCode, null)
PYear parameter           Present (bool) — NEVER used            Absent
RowsIns / RowsUpd         Never incremented                      Incremented correctly
Commit strategy           db.TblCodes.UpdateRange(codes) +       db.SaveChanges() only — EF change tracking
                          db.SaveChanges()                       handles updates; no explicit UpdateRange
New row insert             codes.Add(c) — then UpdateRange        codes.Add(c) — then SaveChanges via tracking
PHC SiteId hardcode       Yes — if SiteCode=="PHC" → SiteId=105  No — uses raw source value for all sites
Error handling            res=false; Console.WriteLine only       res.IsResult=false; ExceptMsg populated
BulkMerge (commented out) No                                     Yes — commented-out Z.BulkOperations.BulkMerge
Column switch             Identical ~30 cases                    Identical ~30 cases
RowChkSum guard           Identical: (NewRow) || (rcs != c.RowChkSum)   Identical
________________________________________

5. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 831

CASE: pats.tbl_codes

Standard metadata-driven SELECT with rolling WHERE:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

Call — always uses Overload 2:
    rCodes = sd.SaveCodes(SrcDt, st.SiteCode, null)

Note: BHGTaskRunner passes only two meaningful arguments (SrcDt and SiteCode; db=null).
The overload with `PYear` (Overload 1) is NOT called from BHGTaskRunner and is legacy code.

RowTrax block: EMPTY — no audit logged.
Scheduling context: `pats.tbl_codes` sits adjacent to `pats.tbl_customquestions` and
`pats.tbl_cows_v6` — runs under **Schedule 6 (Samms-Forms)** or **Schedule 3 (catch-all)**.
________________________________________

6. Source Table — SAMMS SQL Server (dbo)

The source is typically `dbo.tblCodes` or a view over it (`st.FromTblVw`).
Columns consumed by the shared column switch (lower-cased for matching, ~30 fields):

Field (switch name)        Maps to EF property          Type       Notes
-------------------        -------------------          ----       -----
cdegroup                   CdeGroup                     string
cdedesc                    CdeDesc                      string
cdebillable                CdeBillable                  bool?      length > 0 guard
cdeua                      CdeUa                        bool?      length > 0 guard
cdeintamt                  CdeIntAmt                    int?       length > 0 guard
cdeliquid                  CdeLiquid                    bool?      length > 0 guard; uses r["cdeliquid"] (lowercase)
cdestaffcode               CdeStaffcode                 bool?      length > 0 guard; uses r["cdeSTAFFCODE"]
cdefund                    CdeFund                      string
cdemodality                CdeModality                  string
cdedrugfree                CdeDrugfree                  bool?      length > 0 guard; uses r["cdeDRUGFREE"]
cdeprovider                CdeProvider                  string
cdesitenum                 CdeSiteNum                   string
rowguid                    Rowguid                      Guid?      length > 0 guard; Guid.Parse
cdebillableresidential     CdeBillableResidential       bool?      length > 0 guard
cdeservicesetting          CdeServiceSetting            string
cdedischargetype           CdeDischargeType             string
cdesigrequired             CdeSigRequired               bool?      length > 0 guard
cderesidential             CdeResidential               bool?      length > 0 guard
cdeallowoverlap            CdeAllowOverlap              bool?      length > 0 guard
duiamt                     DuiAmt                       decimal?   length > 0 guard
duihourrate                DuiHourRate                  decimal?   length > 0 guard
bldefault                  BlDefault                    bool?      length > 0 guard
weeklyfee                  WeeklyFee                    int?       length > 0 guard
musthavebilling            MustHaveBilling              bool?      length > 0 guard
suboxoneprog               Suboxoneprog                 bool?      length > 0 guard
cdeinsurance               CdeInsurance                 bool?      length > 0 guard
defrate                    DefRate                      decimal?   length > 0 guard
siteid                     SiteId                       int?       OL1: PHC→105 hardcode; OL2: raw source
cdelblcolor                Cdelblcolor                  string
cde3pdonotbill             Cde3pdonotbill               bool?      length > 0 guard
cde3pPOSoverride           Cde3pPosoverride             string
isprescreening             IsPrescreening               bool?      length > 0 guard
obat                       Obat                         bool?      length > 0 guard
reqauth                    ReqAuth                      bool?      length > 0 guard
IntakeProg                 IntakeProg                   bool?      length > 0 guard; case label uses original casing

Note: `cdeID` and `RowChkSum` are read OUTSIDE the switch directly:
    int cid = int.Parse(r["cdeID"].ToString())
    int rcs = int.Parse(r["RowChkSum"].ToString())
Neither has a null/length guard.
________________________________________

7. SaveCodes Shared Logic (Both Overloads)

Azure pre-load:
    codes = db.TblCodes.Where(x => x.SiteCode == sc).ToList()
Full site slice for the clinic.

AllNewRows fast path:
    if (codes.Count == 0) { AllNewRows = true; }
When no Azure rows exist for this site, every source row takes the new-record path directly.

Per-row processing:
    cid = int.Parse(r["cdeID"].ToString())      ← no null guard
    rcs = int.Parse(r["RowChkSum"].ToString())   ← no null guard

AllNewRows path:
    c = new TblCodes { SiteCode = sc, CdeId = cid, RowChkSum = rcs }; NewRow = true

Normal lookup (codes exist): c = codes.Where(x => x.CdeId == cid).FirstOrDefault()
    Not found: new TblCodes { SiteCode, CdeId, RowChkSum }; NewRow = true
    Found:     use existing tracked entity

RowChkSum guard (effective in both overloads):
    if ((NewRow) || (rcs != c.RowChkSum))
    {
        c.LastModAt = RunDT;
        foreach (DataColumn m in tbl.Columns) { switch ... }
    }
When checksum is unchanged on a found record, the column switch is skipped. Only new rows
and changed rows receive field mappings. `RunDT = DateTime.Now` is set once per method call —
all updated rows share the same `LastModAt` timestamp for the run.

New row tracking:
    if (NewRow || AllNewRows)
    {
        codes.Add(c);   ← adds to in-memory list
        NewRow = false;
    }
________________________________________

8. Commit Strategy Differences

Overload 1 (Legacy — SaveCodes with PYear):
    db.TblCodes.UpdateRange(codes);
    db.SaveChanges();
`UpdateRange` explicitly marks ALL records in `codes` as `EntityState.Modified`, including
records where RowChkSum was unchanged and the column switch was skipped. This causes EF to
generate UPDATE statements for EVERY code record in the site slice, even unchanged ones —
a significant unnecessary write load (see Anomaly 3).

Overload 2 (Active — SaveCodes with RCodes):
    db.SaveChanges();
No explicit `UpdateRange`. Updates rely on EF change tracking: since `codes` was populated
from `db.TblCodes.Where(...)`, the entities are tracked. Modifying a property on a tracked
entity automatically marks it as Modified — so only records that actually had a field
assignment inside the column switch block generate UPDATE statements. Unchanged records
(where the RowChkSum guard skipped the switch) remain `Unchanged` and are not written.
This is the correct and more efficient pattern.

However, new records added via `codes.Add(c)` are NOT added to the DbContext in overload 2
— only to the in-memory list. EF has no knowledge of them. There is no `db.TblCodes.Add(c)`
or `db.TblCodes.AddRange(...)` call for new records in overload 2 (see Anomaly 4).
Overload 1 is the same — `UpdateRange` on `codes` which now includes new objects, but
`UpdateRange` marks them as Modified (not Added), which would cause EF to issue UPDATE
statements for records that don't exist yet — likely throwing a DbUpdateConcurrencyException
or silently doing nothing depending on database settings (see Anomaly 5).
________________________________________

9. Anomalies, Bugs, and Known Defects

ANOMALY 1 — `PYear` parameter in Overload 1 is never used.

File: SaveCodes.cs, line 10
    public bool SaveCodes(DataTable tbl, string sc, bool PYear, Models.BHG_DRContext db)
`PYear` is accepted but never referenced in the method body. Its original intent (prior year
filtering?) was never implemented. The overload itself is not called from BHGTaskRunner.

ANOMALY 2 — Core fields (`cdeID`, `RowChkSum`) parsed without null guards.

File: SaveCodes.cs, lines 25–26 (OL1) and 212–213 (OL2)
    int cid = int.Parse(r["cdeID"].ToString())
    int rcs = int.Parse(r["RowChkSum"].ToString())
No length or null guard before `int.Parse`. A NULL or empty source value throws a
`FormatException`, aborting the entire site's codes load for the run.

ANOMALY 3 — Overload 1: `UpdateRange(codes)` writes entire site slice unconditionally.

File: SaveCodes.cs, line 177
    db.TblCodes.UpdateRange(codes);
`UpdateRange` marks every entity in `codes` as `EntityState.Modified` regardless of whether
any field changed. Even records skipped by the RowChkSum guard receive an UPDATE SQL
statement. On a site with hundreds of codes, this is a large and unnecessary write.

ANOMALY 4 — Overload 2: New records never registered with DbContext.

File: SaveCodes.cs, lines 360–364 (OL2)
    codes.Add(c);   ← adds to in-memory list only
New `TblCodes` objects are added to the `codes` list but never to the `db.TblCodes` DbSet
via `db.Add(c)` or `db.AddRange(...)`. When `db.SaveChanges()` is called, EF has no
knowledge of these new objects — they are not inserted. New codes for a site are silently
dropped. This is a critical data loss bug for new sites or newly added codes.

ANOMALY 5 — Overload 1: `UpdateRange` on objects that include un-tracked new records.

File: SaveCodes.cs, line 177
When `AllNewRows == true` or `NewRow` was set, new `TblCodes` objects (created with
`new Models.TblCodes { ... }`) are added to `codes` and then included in `UpdateRange(codes)`.
`UpdateRange` on a detached/new object marks it as `EntityState.Modified`, not `EntityState.Added`.
EF will attempt UPDATE statements for records that do not exist in the database. Depending on
EF Core version and concurrency settings, this either fails silently (0 rows affected) or
throws a `DbUpdateConcurrencyException`. New codes are effectively never inserted in Overload 1.

ANOMALY 6 — `IntakeProg` case label uses original casing.

File: SaveCodes.cs, line 165 (OL1) and line 354 (OL2)
    case "IntakeProg":
All other case labels use fully lowercase strings consistent with `c.ColumnName.ToLower()`.
`"IntakeProg"` retains mixed case. `c.ColumnName.ToLower()` would produce `"intakeprog"` —
which does NOT match `"IntakeProg"`. This case label is **unreachable** — `IntakeProg` is
never mapped in either overload.

ANOMALY 7 — `cdeliquid` inconsistent key casing.

File: SaveCodes.cs, lines 74–76 (OL1) and 267–269 (OL2)
    case "cdeliquid":
        if (r["cdeLiquid"].ToString().Length > 0) { c.CdeLiquid = bool.Parse(r["cdeliquid"].ToString()); }
The guard reads `r["cdeLiquid"]` (mixed case) but the parse reads `r["cdeliquid"]` (lowercase).
In standard ADO.NET DataTables, column access is case-insensitive, so this works in practice,
but is an inconsistency that could fail in case-sensitive contexts.

ANOMALY 8 — Overload 2: Commented-out BulkMerge code.

File: SaveCodes.cs, lines 366–370
    //db.TblCodes.BulkMerge(codes, options => { ... });
A `Z.BulkOperations.BulkMerge` call was drafted but commented out, suggesting a prior
attempt to optimise this path with a bulk upsert. The commented-out code uses
`x.SiteCode, x.CdeId, x.RowChkSum, x.LastModAt` as the merge key expression, which does
not include all necessary key fields for a safe upsert.

ANOMALY 9 — RowTrax block present but empty.

File: BHGTaskRunner/Program.cs, lines 836–841
No source-vs-destination row count audit is ever logged.

ANOMALY 10 — Overload 1 returns bool; errors only go to Console.

File: SaveCodes.cs, lines 181–189
    catch (Exception e)
    {
        res = false;
        Console.WriteLine(e.Message)
        ...Console.WriteLine(e.InnerException.Message)
    }
Error details are only written to Console — `res` is set to `false` but no message is stored
in any returned object. BHGTaskRunner has no way to capture or log the failure details for
Overload 1 beyond the return value being false. This overload is not called by BHGTaskRunner,
so this is academic but noted.
________________________________________

10. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe → tsk.tbl_Tasks (TaskName = pats.tbl_codes, per SiteCode)
        |
        V
BHGTaskRunner.exe 6 (Samms-Forms) or 3 (catch-all)
        |
        |  strCmd += " Where " + strWhere + " " + st.SortOrder
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveCodes(SrcDt, SiteCode, null)   ← Overload 2 (RCodes version)
        |
        |  RunDT = DateTime.Now
        |  codes = all pats.tbl_Codes rows for SiteCode
        |  AllNewRows = (codes.Count == 0)
        |
        |  FOREACH source row:
        |      cid = r["cdeID"]; rcs = r["RowChkSum"]   (no null guards)
        |      AllNewRows → new TblCodes { SiteCode, CdeId, RowChkSum }; RowsIns++
        |      Else lookup by CdeId:
        |          Not found → new TblCodes; RowsIns++
        |          Found     → RowsUpd++
        |      if (NewRow OR rcs != c.RowChkSum):
        |          c.LastModAt = RunDT
        |          ~30-field column switch (IntakeProg case unreachable)
        |      if (NewRow OR AllNewRows):
        |          codes.Add(c)   ← in-memory only — NOT added to DbContext
        |
        |  db.SaveChanges()
        |      ← EF updates only tracked changed entities (correct)
        |      ← NEW records in codes list are NOT inserted (bug — never registered with DbContext)
        |
        V
pats.tbl_Codes (Azure BHG_DR)  — updates only; inserts silently dropped
RowTrax: empty
________________________________________

11. File Reference Map

File Path                                                   Purpose
---------                                                   -------
BCAppCode/BHG-DR-LIB/SaveCodes.cs                          Two overloads (391 lines)
BCAppCode/BHGTaskRunner/Program.cs                          case "pats.tbl_codes" ~line 831
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                  strFlds / strWhere for SELECT
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                      Executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblCodes.cs                    EF model → pats.tbl_Codes
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs               DbSet<TblCodes>
________________________________________

12. Quick Reference Summary

Overload              Called by         Key     RowChkSum guard    New row insert     Schedule
--------              ---------         ---     ---------------    ---------------    --------
SaveCodes(tbl,sc,     BHGTaskRunner     CdeId   Effective          BROKEN — codes     6 / 3
  db)  [RCodes]       (active)                                     .Add() only, never
                                                                   db.Add()
SaveCodes(tbl,sc,     Not called from   CdeId   Effective          BROKEN —           (legacy)
  PYear,db)  [bool]   BHGTaskRunner               UpdateRange on
                      (legacy)                   new detached
                                                 objects

PHC SiteId=105 hardcode: Overload 1 only.
`IntakeProg` case label: unreachable in both overloads (mixed-case label never matches ToLower() input).
RowChkSum guard works — unchanged records skip the column switch and are not written in Overload 2.
