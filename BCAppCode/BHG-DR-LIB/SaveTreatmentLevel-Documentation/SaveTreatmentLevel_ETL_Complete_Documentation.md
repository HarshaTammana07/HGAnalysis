
Treatment Level ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 6 — Samms-Forms / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the end-to-end process used to extract patient treatment level records
from local SAMMS SQL Server databases at each clinic and load them into the central Azure SQL
data warehouse (BHG_DR).

The goal of this document is to explain:
- What treatment level data is and why it exists in the warehouse
- How BHGTaskRunner dispatches this task using a standard rolling SELECT
- How SaveTreatmentLevel works: pre-load, per-field try/catch, RowChkSum read in the
  sitecode case, composite key lookup, explicit field-copy update, two-phase commit
- Why RowState is never set and defaults throughout
- Why the `wrkdt` parameter is unused
- The weak date guard (`> 5` instead of `> 6`) on `recordon`
- All known anomalies, bugs, and design notes

There is one method in SaveTreatmentLevel.cs:

pats.tbl_TreatmentLevel:   SaveTreatmentLevel   (per-patient treatment level records)
________________________________________

2. High-Level Business Summary

What is treatment level data?

`pats.tbl_TreatmentLevel` stores per-patient treatment level classification records as
assigned by clinical staff. Each row records a level of care designation (e.g., level of
service intensity), the patient it applies to (`CltId`), the staff member who recorded it
(`UserID`), and the date it was recorded (`RecordOn`). Treatment level tracking supports
clinical documentation, regulatory reporting, and level-of-care transition reporting across
the warehouse.

Load type
EF Core two-phase upsert. Full site slice pre-load (no pre-pass RowState reset). Per-field
try/catch wrapping each column assignment. Lookup by `SiteCode + ID`. Explicit field copy on
update. No RowChkSum guard — all fields unconditionally overwritten. Single `SaveChanges()`
for updates followed by `AddRange + SaveChanges()` for new rows.
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule / tsk.tbl_Tasks (Azure)  Task queue — ActionKey drives schedule selection
BHGTaskRunner.exe 6 / 3                   Samms-Forms / catch-all (verify in tsk.tbl_Tasks)
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)            Column list + RowChkSum expression for SELECT
SelectConstructor.cs                       Builds `strFlds` / `strWhere` (rolling lookback)
SQLSvrManager.cs                          Executes SELECT against SAMMS SQL Server
SaveTreatmentLevel.cs (BHG-DR-LIB)       1 method — SaveTreatmentLevel
Models/TblTreatmentLevel.cs               EF entity → pats.tbl_TreatmentLevel
pats.tbl_TreatmentLevel (Azure BHG_DR)   Final destination
tsk.tbl_RowTrax (Azure DB)               Audit log — no RowTrax block present for this case
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 3105

CASE: pats.tbl_treatmentlevel

Standard metadata-driven SELECT with rolling WHERE:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

Call:
    rCodes = sd.SaveTreatmentLevel(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

The third argument is `st.WorkDate.Value.AddDays(DaysBack)` — passed as `wrkdt` but
**never used** inside the method (see Anomaly 1).

No RowTrax block — this case does not log source vs. destination row counts.
No special pre-checks, column probes, or site exclusions.

Scheduling context: `pats.tbl_treatmentlevel` sits in the main inner `switch` block
alongside assessment form tasks (`pats.tbl_comprehensiveassessmentform`,
`pats.tbl_admissionassessmentsubstanceusehistory`) and is expected to run under
**Schedule 6 (Samms-Forms)** or **Schedule 3 (catch-all)**. Verify TaskName in
`tsk.tbl_Tasks` for your environment.
________________________________________

5. Source Table — SAMMS SQL Server (dbo)

The source table/view name comes from `st.FromTblVw` in the task metadata.
Columns consumed by the switch (lower-cased):

Field (switch name)    Maps to EF property    Type      Notes
-------------------    -------------------    ----      -----
sitecode               SiteCode               string    Also sets LastModAt and reads RowChkSum
id                     ID                     int       Lookup key (part of composite)
treatmentlevel         TreatmentLevel         string    Level of care description
userid                 UserID                 int?      Guarded length > 0
cltid                  CltId                  int?      Guarded length > 0
recordon               RecordOn               DateTime? Guarded length > 5 (see Anomaly 3)

RowChkSum is read directly in the `sitecode` case:
    xtl.RowChkSum = int.Parse(dr["RowChkSum"].ToString())
It is NOT read via its own switch case. If `sitecode` appears before `rowchksum` in the
column order (and no `rowchksum` case exists), the checksum is always populated from the
sitecode case regardless of column order.
________________________________________

6. SaveTreatmentLevel — Treatment Level Load (pats.tbl_TreatmentLevel)

Source: `st.FromTblVw` (SAMMS) with `strWhere` rolling filter
Destination: pats.tbl_TreatmentLevel
Composite key: SiteCode + ID
Parameters: `tbl`, `sc`, `wrkdt`, `db`

Runtime stamp:
    DateTime runat = DateTime.Now;
Used as `LastModAt` on every write (both new and update paths). `runat` is set once at method
start — all rows in the same run share the same `LastModAt` timestamp.

Azure pre-load:
    treatmentLevels = db.TblTreatmentLevels.Where(x => x.SiteCode == sc).ToList()
Full site slice. No pre-pass RowState reset — existing records are not deactivated before
the loop. Records that no longer appear in the source extract are not touched.

Per-row fresh object construction via 6-case column switch with per-field try/catch:

Each individual column assignment is wrapped in its own try/catch:
    try { switch(c.ColumnName.ToLower()) { ... } }
    catch (Exception e) { Console.WriteLine(c.ColumnName + " : " + dr[c.ColumnName]); }

This means a parse failure on any single field is silently swallowed and logged only to
Console — the row continues processing with the failed field left at its default value.
The outer catch (lines 96–104) is the only mechanism that returns an error to the caller.

sitecode case (multi-assignment):
    xtl.SiteCode = dr[c.ColumnName].ToString();
    xtl.LastModAt = runat;
    xtl.RowChkSum = int.Parse(dr["RowChkSum"].ToString());
RowChkSum is parsed here via a hardcoded `dr["RowChkSum"]` reference — not via the column
iteration variable `c`. If the source DataTable does not contain a "RowChkSum" column, this
will throw a `KeyNotFoundException` caught by the per-field try/catch — RowChkSum will remain
0 (default) and processing continues.

id:
    xtl.ID = int.Parse(dr[c.ColumnName].ToString())

treatmentlevel:
    xtl.TreatmentLevel = dr[c.ColumnName].ToString()

userid:
    int.Parse guarded by length > 0

cltid:
    int.Parse guarded by length > 0

recordon:
    DateTime.Parse guarded by length > 5 (see Anomaly 3)

No `rowstate` case exists — `xtl.RowState` is never set in the switch. For new records,
`RowState` will be whatever the EF model default is (likely `false` or `null`). For existing
records, `RowState` is explicitly copied in the update block as `dbxtl.RowState = xtl.RowState`
— which copies the unset default, effectively clearing RowState for every updated record
(see Anomaly 2).

Lookup: SiteCode + ID
    dbxtl = treatmentLevels.FirstOrDefault(x => x.SiteCode == xtl.SiteCode && x.ID == xtl.ID)

Insert path (dbxtl == null):
    rc.RowsIns += 1
    ntls.Add(xtl)

Update path (dbxtl != null):
    rc.RowsUpd += 1
    Explicit field copy: CltId, LastModAt, RecordOn, RowChkSum, RowState, TreatmentLevel, UserID
    No RowChkSum guard — update runs unconditionally.

Two-phase commit:
    db.SaveChanges()                              ← commits all updates
    if (ntls.Count > 0) → db.TblTreatmentLevels.AddRange(ntls) → db.SaveChanges()

Error handling (outer):
    Correct null check: `if (e.InnerException != null)` before reading `.Message`.
________________________________________

7. Change Detection — RowChkSum Behaviour

RowChkSum is populated in the `sitecode` case and stored in both new and updated records
(`dbxtl.RowChkSum = xtl.RowChkSum`). However, there is no comparison guard around the update
block:
    // if (dbxtl.RowChkSum != xtl.RowChkSum) — MISSING
    {
        dbxtl.CltId = ...
        ...
    }
All 7 update fields are overwritten unconditionally on every run for every matched record.
RowChkSum is stored as an audit value only.
________________________________________

8. Anomalies, Bugs, and Known Defects

ANOMALY 1 — `wrkdt` parameter is never used.

File: SaveTreatmentLevel.cs, method signature line 13
    public Models.RCodes SaveTreatmentLevel(DataTable tbl, string sc, DateTime wrkdt, ...)
`wrkdt` receives `st.WorkDate.Value.AddDays(DaysBack)` from BHGTaskRunner but is never
referenced inside the method. Date scoping is handled source-side via `strWhere` in the SELECT.
Dead parameter — same pattern seen in `SaveFmp`, `SavePAData`, and others.

ANOMALY 2 — RowState never set; update path copies default value, clearing RowState.

File: SaveTreatmentLevel.cs — no `rowstate` case in switch; line 84 in update block
    dbxtl.RowState = xtl.RowState;

`xtl` is a freshly constructed `TblTreatmentLevel` object. Since no switch case sets
`xtl.RowState`, it retains the EF model default (typically `false` or `null`). The update
path then copies this default to `dbxtl.RowState`, overwriting whatever value the record
previously had in Azure with false/null on every update. New records inserted via `ntls`
also carry the unset default. This means `RowState` is effectively always reset to the
default on every ETL run — it can never reflect a meaningful active/inactive state.

ANOMALY 3 — DateTime guard uses `> 5` instead of standard `> 6`.

File: SaveTreatmentLevel.cs, line 59
    if (dr[c.ColumnName].ToString().Length > 5)
    { xtl.RecordOn = DateTime.Parse(dr[c.ColumnName].ToString()); }

The standard guard used across the codebase is `> 6`. Using `> 5` means a 6-character string
(which could be a short invalid date fragment) would be passed to `DateTime.Parse`, risking
a `FormatException`. The per-field try/catch would catch this and log to Console, but `RecordOn`
would silently remain null.

ANOMALY 4 — RowChkSum read via hardcoded key in sitecode case.

File: SaveTreatmentLevel.cs, line 38
    xtl.RowChkSum = int.Parse(dr["RowChkSum"].ToString());

This is inside the `sitecode` switch case and uses a literal string key `"RowChkSum"` rather
than the column iteration variable `c`. Two consequences:
a) If the source DataTable has no "RowChkSum" column, `KeyNotFoundException` is caught by the
   per-field try/catch, `RowChkSum` defaults to 0, and no error is raised to the caller.
b) RowChkSum is always read when `sitecode` is processed, regardless of whether a dedicated
   `rowchksum` case would also fire — there is no `rowchksum` case in this switch.

ANOMALY 5 — Per-field try/catch silently swallows parse errors.

File: SaveTreatmentLevel.cs, lines 31–69
Every column assignment is individually wrapped in a try/catch that only writes to Console
and discards the exception. A failed `int.Parse` on `userid` or `cltid` (empty or non-numeric
value), or a `DateTime.Parse` failure on `recordon`, will leave the affected field at its
default and continue processing as if nothing happened. No indicator appears in `rc.ExceptMsg`.
This makes diagnosing field-level data quality issues very difficult without reading Console
output.

ANOMALY 6 — No RowChkSum guard — unconditional full overwrite on update.

File: SaveTreatmentLevel.cs, lines 80–87
RowChkSum is stored in Azure but never compared before writing. Every matched record receives
a full field copy on every run.

ANOMALY 7 — No RowTrax logging.

File: BHGTaskRunner/Program.cs — no RowTrax block after line 3108
Unlike `pats.tbl_enrollment` (which actively logs RowTrax), `pats.tbl_treatmentlevel` has no
source vs. destination row count audit.
________________________________________

9. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe → tsk.tbl_Tasks (TaskName per SiteCode, pats.tbl_treatmentlevel)
        |
        V
BHGTaskRunner.exe 6 (Samms-Forms) or 3 (catch-all)
        |
        |  strCmd += " Where " + strWhere + " " + st.SortOrder
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveTreatmentLevel(SrcDt, SiteCode, WorkDate.AddDays(DaysBack), null)
        |                                         [wrkdt — unused]
        |
        |  runat = DateTime.Now
        |  treatmentLevels = all pats.tbl_TreatmentLevel rows for SiteCode
        |  (no pre-pass RowState reset)
        |
        |  FOREACH source row:
        |      Build new TblTreatmentLevel via 6-case switch
        |      Per-field try/catch — parse failures silently logged to Console
        |      sitecode case also reads RowChkSum via hardcoded dr["RowChkSum"]
        |      Lookup: SiteCode + ID
        |      Not found → ntls.Add(xtl); rc.RowsIns++
        |      Found     → explicit field copy (RowState copied from unset default);
        |                   rc.RowsUpd++
        |
        |  db.SaveChanges()                   ← updates committed
        |  AddRange(ntls) → db.SaveChanges()  ← new rows inserted
        |
        V
pats.tbl_TreatmentLevel (Azure BHG_DR)
RowTrax: none
BHGTaskRunner marks task Status=20 (complete)
________________________________________

10. File Reference Map

File Path                                                       Purpose
---------                                                       -------
BCAppCode/BHG-DR-LIB/SaveTreatmentLevel.cs                     SaveTreatmentLevel method (110 lines)
BCAppCode/BHGTaskRunner/Program.cs                              case "pats.tbl_treatmentlevel" ~line 3105
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                      strFlds / strWhere for SELECT build
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                          Executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblTreatmentLevel.cs               EF model → pats.tbl_TreatmentLevel
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                   DbSet<TblTreatmentLevels>
________________________________________

11. Quick Reference Summary

Method                  Load path    Key              RowChkSum guard    RowState    Schedule
------                  ---------    ---              ---------------    --------    --------
SaveTreatmentLevel      EF Core      SiteCode + ID    None               Never set   6 / 3

Parameters: `wrkdt` unused.
RowState always default (false/null) for all new and updated records.
Per-field try/catch silently swallows parse errors — no indication in RCodes.
RowChkSum read via hardcoded key inside sitecode case, not via its own switch case.
Date guard `> 5` (weaker than standard `> 6`).
