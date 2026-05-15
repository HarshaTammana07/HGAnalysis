
FMP ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Regional ETL (typical) — verify ActionKey in tsk.tbl_Tasks / dms metadata
________________________________________

1. Document Purpose

This document explains the end-to-end process used to extract Financial Management Plan (FMP)
records — patient-level fee or payment-plan style data tied to a client and date range — from
local SAMMS SQL Server databases at each clinic and load them into the central Azure SQL data
warehouse (BHG_DR).

The goal of this document is to explain:
- What FMP data represents in the warehouse
- How BHGTaskRunner dispatches `SaveFmp`
- The full pre-pass RowState deactivation pattern before the main loop
- The column switch, lookup key, explicit field-copy update pattern, and two-phase commit
- Why the `dtWrk` parameter is unused
- Why `RowsUpd` is never incremented
- Why the `rowstate` and `lastmodat` switch cases ignore actual source values
- All known anomalies, bugs, and design notes

There is one method in SaveFmp.cs:

pats.tbl_Fmp:   SaveFmp   (FMP / patient financial plan records)
________________________________________

2. High-Level Business Summary

What is FMP?

The `pats.tbl_Fmp` table stores per-patient financial management plan (or equivalent SAMMS
workflow) records: client linkage (`FmpLngClt`), plan start and projected/actual end dates,
interest or intensity rate (`FmpIntRate`), textual reason and description, audit fields for
who added or ended the plan and when, end narrative (`FmPendtext` — note EF property spelling),
and an at-risk classification string (`AtriskType`). `RowState` marks whether the plan row is
currently active in the warehouse after ETL.

The pre-pass deactivation pattern is intentional: every existing Azure row for the site that
was previously active (`RowState == true`) is marked inactive before processing the source
batch. Any FMP row returned from SAMMS in the current extract is then re-activated and fully
refreshed. Any FMP row that no longer appears in the extract remains inactive — this is a
full-reconciliation soft-delete model keyed on presence in the rolling source window.

Load type
EF Core upsert with a full pre-pass RowState reset (all active → inactive), per-row fresh
object construction, lookup by `FmpId` within the site slice, explicit field copy on update,
no RowChkSum — all mapped fields are overwritten on every match. Two-phase commit:
`SaveChanges()` for updates, then `AddRange` + `SaveChanges()` for inserts.
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule / tsk.tbl_Tasks (Azure)   Task queue — ActionKey determines which schedule
BHGTaskRunner.exe                          ETL orchestrator (args 2/4 typical for this switch block)
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)            Column list + RowChkSum for standard SELECT build
SelectConstructor.cs                       Builds `strFlds` / `strWhere` (rolling lookback)
SQLSvrManager.cs                          Executes SELECT against SAMMS
SaveFmp.cs (BHG-DR-LIB)                  1 method — SaveFmp
Models/TblFmp.cs                          EF entity → pats.tbl_Fmp (composite key SiteCode + FmpId)
pats.tbl_Fmp (Azure BHG_DR)              Final destination
tsk.tbl_RowTrax (Azure DB)               Audit log — RowTrax block empty for this task
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 1055

CASE: pats.tbl_fmp

This case sits in the same inner `switch` block as `pats.tbl_enrollment`, `pats.tbl_feesched`,
and other patient-centric loads (~lines 1005–1066). In most BHG deployments these tasks are
queued under **Regional ETL P1 or P2** (BHGTaskRunner args **2** or **4**), but the exact
schedule is determined by `tsk.tbl_Tasks.ActionKey` and Scheduler configuration — always
verify in your environment.

Standard metadata-driven SELECT:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

Call:
    rCodes = sd.SaveFmp(SrcDt, st.SiteCode, st.WorkDate.Value.Date, null)
    rCodes.RowsProcessed = SrcDt.Rows.Count

The third parameter is `st.WorkDate.Value.Date` — passed as `dtWrk` but **never referenced**
inside `SaveFmp` (see Anomaly 1).

RowTrax block: EMPTY — no audit logged.
________________________________________

5. Source Table — SAMMS SQL Server (dbo)

The concrete source view/table name comes from `st.FromTblVw` in the task metadata
(commonly a `dbo.vw_*` or base table exposing FMP columns). Typical columns consumed by
`SaveFmp` (names from the switch, all lower-cased for matching):

Field (switch name)     Maps to EF property      Notes
-------------------     -------------------      -----
sitecode                (ignored for value)      SiteCode forced from `sc` parameter
fmpid                   FmpId                    int — lookup key
fmplngclt               FmpLngClt                int? — guarded length > 1
fmpdtstart              FmpDtStart               DateTime? — length > 6
fmpdtprojend            FmpDtProjEnd             DateTime? — length > 6
fmpdtend                FmpDtEnd                 DateTime? — length > 6
fmpintrate              FmpIntRate               int? — length > 1
fmpstrreason            FmpStrReason             string
fmpstrdesc              FmpStrDesc               string
fmpdtadded              FmpDtAdded               DateTime? — length > 6
fmpstruseradded         FmpStrUserAdded          string
fmpdtended              FmpDtEnded               DateTime? — length > 6
fmpstruserended         FmpStrUserEnded          string
fmpendtext              FmPendtext               string (EF property spelling)
atrisktype              AtriskType               string
rowstate                RowState                 bool — **NOT read from source** (see Anomaly 5)
lastmodat               LastModAt                DateTime — **NOT read from source** (see Anomaly 6)

No `rowchksum` case exists in SaveFmp — change detection is absent.
________________________________________

6. SaveFmp — FMP Load (pats.tbl_Fmp)

Source: `st.FromTblVw` (SAMMS) + `strWhere` rolling filter from SelectConstructor
Destination: pats.tbl_Fmp
Azure composite key (EF): SiteCode + FmpId
Lookup within method: `FmpId` only (safe because `fmps` is pre-filtered by `SiteCode == sc`)
Parameters: `tbl`, `sc`, `dtWrk`, `db`

Timestamp constant:
    DateTime LastMod = DateTime.Today;
Every `LastModAt` written by this method uses **midnight today** (`DateTime.Today`), not
`DateTime.Now` and not the source `lastmodat` column (see Anomaly 6).

Azure pre-load:
    fmps = db.TblFmp.Where(x => x.SiteCode == sc).ToList()
Full site slice for the clinic.

Pre-pass — full RowState deactivation of all currently-active rows:
    foreach (var f in fmps)
    {
        if (f.RowState == true)
        {
            f.RowState = false;
            f.LastModAt = LastMod;
        }
    }
No `SaveChanges()` here — deactivation is committed together with the rest at the first
post-loop `db.SaveChanges()`.

Per-row fresh object + 14-case column switch:

sitecode:
    fmp.SiteCode = sc;           ← uses task SiteCode parameter, NOT `r["sitecode"]`
    fmp.LastModAt = LastMod;
    fmp.RowState = true;

fmpid:
    fmp.FmpId = int.Parse(...)

Numeric / date fields:
    Length guards: `> 1` for ints (`fmplngclt`, `fmpintrate`), `> 6` for all DateTime fields.

Strings:
    Direct `ToString()` assignment.

rowstate:
    fmp.RowState = true;         ← ignores actual source value (see Anomaly 5)

lastmodat:
    fmp.LastModAt = LastMod;     ← ignores actual source value; duplicates sitecode case (see Anomaly 6)

Lookup:
    dbF = fmps.FirstOrDefault(x => x.FmpId == fmp.FmpId)

Insert path (`dbF == null`):
    nFmps.Add(fmp);
    res.RowsIns += 1;

Update path (`dbF != null`):
    Explicit copy of all 14 business fields + LastModAt + RowState from `fmp` to `dbF`.
    **res.RowsUpd is never incremented** (see Anomaly 2).

No RowChkSum — every field copy runs unconditionally on every update.

Two-phase commit:
    db.SaveChanges();                 ← commits pre-pass deactivations + all updates
    if (nFmps.Count > 0)
    {
        db.TblFmp.AddRange(nFmps);
        db.SaveChanges();            ← inserts new rows
    }

Error handling:
    Correct null check: `if (e.InnerException != null)` before reading `.Message`.
________________________________________

7. Change Detection — RowChkSum Behaviour

RowChkSum is **not implemented** in SaveFmp. There is no `rowchksum` case in the switch and no
comparison before the explicit field-copy update block. Combined with the pre-pass that
deactivates every previously-active row, every ETL run performs a full reconciliation:
inactive-by-default for all prior actives, then re-activate only rows present in the
current source extract.
________________________________________

8. Anomalies, Bugs, and Known Defects

ANOMALY 1 — `dtWrk` parameter is never used.

File: SaveFmp.cs, method signature line 11
    public Models.RCodes SaveFmp(DataTable tbl, string sc, DateTime dtWrk, Models.BHG_DRContext db)

`dtWrk` is passed from BHGTaskRunner as `st.WorkDate.Value.Date` but is never referenced in the
method body. Date scoping is entirely source-side via `strWhere` in the SELECT. The parameter
is dead API surface — possibly a vestige of an earlier design or copy-paste from another Save
method.

ANOMALY 2 — `RowsUpd` never incremented.

File: SaveFmp.cs, lines 117–139
Only `res.RowsIns += 1` is executed on the insert path. The update path copies all fields but
never executes `res.RowsUpd += 1`. Downstream monitoring that relies on `RowsUpd` will always
show `0` for FMP updates.

ANOMALY 3 — `LastMod` uses `DateTime.Today` instead of `DateTime.Now`.

File: SaveFmp.cs, line 21
    DateTime LastMod = DateTime.Today;

All `LastModAt` stamps (pre-pass, sitecode case, lastmodat case, and copied to `dbF` on update)
resolve to **midnight local time on the calendar date the ETL runs**, not the actual wall-clock
time of the load. This loses sub-day precision and differs from the `DateTime.Now` pattern used
in most other Save methods.

ANOMALY 4 — `rowstate` switch case ignores source data.

File: SaveFmp.cs, lines 108–110
    case "rowstate":
        fmp.RowState = true;
        break;

If the source DataTable contains a `rowstate` column with `false` / `0` / inactive semantics,
it is **never read**. The value is always forced to `true` whenever that column appears in the
column iteration order. The only way a row ends inactive is via the pre-pass (not present in
source extract) — not via an explicit inactive flag from SAMMS on an included row.

ANOMALY 5 — `lastmodat` switch case ignores source and duplicates `sitecode`.

File: SaveFmp.cs, lines 111–113
    case "lastmodat":
        fmp.LastModAt = LastMod;
        break;

This assigns the same `DateTime.Today` constant already assigned in the `sitecode` case. It does
not parse or honour a source `lastmodat` timestamp from SAMMS.

ANOMALY 6 — EF property name typo frozen: `FmPendtext`.

File: SaveFmp.cs, line 103 / Models/TblFmp.cs
The end-text field maps to `FmPendtext` (capital P) — a historical typo in the EF model that
matches the Azure column mapping. All references must use this spelling.

ANOMALY 7 — RowTrax block present but empty.

File: BHGTaskRunner/Program.cs, lines 1060–1065
No source-vs-destination row count audit is ever logged for this task.

ANOMALY 8 — No RowChkSum guard.

Every matched row receives a full field overwrite on every run regardless of whether data
changed. For high-churn sites this increases write volume but does not corrupt data.
________________________________________

9. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe → tsk.tbl_Tasks (TaskName = pats.tbl_fmp, per SiteCode)
        |
        V
BHGTaskRunner (typical: arg 2 or 4 — Regional ETL; verify ActionKey in your DB)
        |
        |  strCmd built from SelectConstructor metadata + strWhere (rolling lookback)
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveFmp(SrcDt, SiteCode, WorkDate.Date, null)
        |
        |  fmps = all pats.tbl_Fmp rows for SiteCode
        |
        |  PRE-PASS: foreach f in fmps where RowState==true → RowState=false, LastModAt=Today
        |
        |  FOREACH source row:
        |      Build new TblFmp via switch (SiteCode=sc from parameter, not row)
        |      Lookup: FmpId match within fmps
        |      Not found → nFmps.Add, RowsIns++
        |      Found     → explicit field copy (RowsUpd NOT incremented)
        |
        |  db.SaveChanges()           ← deactivations + updates
        |  AddRange(nFmps) + SaveChanges()   ← inserts
        |
        V
pats.tbl_Fmp (Azure BHG_DR)
RowTrax EMPTY
________________________________________

10. File Reference Map

File Path                                              Purpose
---------                                              -------
BCAppCode/BHG-DR-LIB/SaveFmp.cs                       SaveFmp method (161 lines)
BCAppCode/BHGTaskRunner/Program.cs                     case "pats.tbl_fmp" ~line 1055
BCAppCode/BHG-DR-LIB/SelectConstructor.cs             strFlds / strWhere for standard SELECT
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                 Executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblFmp.cs                  EF model → pats.tbl_Fmp
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs           DbSet<TblFmp>
________________________________________

11. Quick Reference Summary

Method    Load path    Key (within site)    RowChkSum    Pre-pass RowState reset    Schedule
------    ---------    -----------------    ---------    ------------------------    --------
SaveFmp   EF Core      FmpId                None         Yes (all active → false)    2/4 typical

Parameters: `dtWrk` unused. `LastModAt` = `DateTime.Today` (midnight), not `DateTime.Now`.
`rowstate` / `lastmodat` cases do not read source values. `RowsUpd` never incremented.
Property typo: `FmPendtext`.


Method: SaveFmp
Field	Value
Name	SaveFmp
Module	Financial Management Plan (FMP) records
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName + SiteCode
Source table	st.FromTblVw (FMP view/table); column list + WHERE from dms.tbl_MapSrc2Dsn via SelectConstructor
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_Fmp
Load type	EF Core upsert — full site slice pre-load; full pre-pass RowState deactivation (all active → false, LastModAt = DateTime.Today); lookup by FmpId; no RowChkSum — all 14 mapped fields unconditionally overwritten on update; new rows batched in nFmps; two-phase commit
Load type column	No RowChkSum; RowState (bool) re-activated in sitecode case — rowstate case ignores source value, always sets true; LastModAt = DateTime.Today (midnight) throughout, not DateTime.Now; lastmodat case ignores source value; RowsUpd never incremented
Frequency	Daily
Schedule	Regional ETL P1/P2 — BHGTaskRunner.exe 2 or 4 (verify ActionKey in tsk.tbl_Tasks)
Parent	Regional ETL
Downstream	pats.tbl_Fmp → patient financial plan reporting; at-risk classification
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveFmp(SrcDt, st.SiteCode, st.WorkDate.Value.Date, null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveFmp.cs
Status	Active
Folder	BHG-DR-LIB\SaveFmp.cs; detail in SaveFmp-Documentation\SaveFmp_ETL_Complete_Documentation.md