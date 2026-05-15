
Check-In ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Regional ETL P1/P2 — BHGTaskRunner.exe 2 / 4
________________________________________

1. Document Purpose

This document explains the end-to-end process used to extract patient check-in records from
local SAMMS SQL Server databases at each clinic and load them into the central Azure SQL data
warehouse (BHG_DR).

The goal of this document is to explain:
- What check-in data is and why it exists in the warehouse
- How BHGTaskRunner dispatches this task including the ciQUEUETIME column existence probe
- The partial Azure pre-load scoped by `CiDate >= workdate OR CiId >= ciid`
- Why AllNewRows can never be true (LINQ always returns a list, never null)
- The effective RowChkSum guard
- The MinutesWaited computed field
- The per-row `db.TblCheckIn.Add()` with a single trailing `SaveChanges()`
- The commented-out `UpdateRange` and its consequence for the update path
- All known anomalies, bugs, and design notes

There is one method in SaveCheckIn.cs:

pats.tbl_CheckIn:   SaveCheckIn   (patient dispensing check-in records)
________________________________________

2. High-Level Business Summary

What is check-in data?

A check-in record in SAMMS captures each patient's arrival and service event at the clinic
dispensing window. It records when the patient checked in (`CiDate`, `CiTime`), when they
were served (`CiServeddtm`), how long they waited (`MinutesWaited` — computed by the ETL,
not stored in SAMMS), what queue they were in (`CiQueue`), the dose amounts and counts
(`CiAmt`, `CiDoses`), whether they were on hold (`CiHold`), the serving staff member
(`CiServedStaff`), and a link to the patient (`CiCltid`, `CicltName`, `Cicltm4id`).

Check-in records are critical for:
- Wait-time analysis and operational reporting
- Patient compliance monitoring (did the patient receive their dose on a given day)
- Audit trails for medication dispensing events
- Queue management analytics

Load type
EF Core upsert with a date-and-id-bounded partial Azure pre-load. RowChkSum is used as an
effective change detection guard — field mappings fire only when the checksum changes or for
new rows. Per-row `db.TblCheckIn.Add()` for inserts, tracked-entity updates via EF change
tracking, single `db.SaveChanges()` after the loop. No `AddRange` pattern.
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule / tsk.tbl_Tasks (Azure)  Task queue — ActionKey 2/4 Regional ETL P1/P2
BHGTaskRunner.exe 2 / 4                   ETL orchestrator — Regional ETL P1 / P2
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)            Column list + RowChkSum expression (ciQUEUETIME conditional)
SelectConstructor.cs                       Builds `strFlds` / `strWhere` (rolling lookback)
SQLSvrManager.cs                          Executes column-existence probe and data SELECT
SaveCheckIn.cs (BHG-DR-LIB)              1 method — SaveCheckIn
Models/TblCheckIn.cs                      EF entity → pats.tbl_CheckIn
pats.tbl_CheckIn (Azure BHG_DR)          Final destination
tsk.tbl_RowTrax (Azure DB)               Audit log — RowTrax block present but empty
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 573

CASE: pats.tbl_checkin

Column existence probe — ciQUEUETIME:
    cols = sm.GetTableData("Cols",
        "select name, column_id from sys.all_columns c
         where c.object_id = (select object_id from sys.tables where upper(name) = 'TBLCHECKIN')
         and upper(name) = 'CIQUEUETIME'",
        st.ConStr).Rows.Count
    if (cols == 0) → strCmd = strCmd.Replace(", [ciQUEUETIME]", "").Replace(" ciQUEUETIME", "")

If the clinic's SAMMS `tblCheckin` does not have a `ciQUEUETIME` column (older SAMMS
versions), it is stripped from the SELECT before the query executes. This prevents a
"Invalid column name" error on legacy schemas.

Inside `SaveCheckIn`, a second guard is present:
    if (tbl.Columns.Contains("ciQUEUETIME")) { ... map ciQUEUETIME ... }
This provides a defence-in-depth check even if the column slips through the SELECT.

Standard WHERE applied after probe:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

Call:
    rCodes = sd.SaveCheckIn(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)
    rCodes.RowsProcessed = SrcDt.Rows.Count

RowTrax block: EMPTY — no audit logged.
________________________________________

5. Source Table — SAMMS SQL Server (dbo)

The source is typically `dbo.tblCheckin` or a view over it.
Columns read directly by name (not via a column switch):

Field (hardcoded key)    Maps to EF property     Type        Notes
--------------------     -------------------     ----        -----
ciid                     CiId                    int         Read directly: r["ciID"] and tbl.Rows[0]["ciid"]
RowChkSum                RowChkSum               int         Read directly: r["RowChkSum"]
ciDate                   CiDate                  DateTime    Read directly: r["ciDate"] — no null guard
ciCLTID                  CiCltid                 string      Read directly: r["ciCLTID"]
ciTime                   CiTime                  DateTime    Read directly: r["ciTime"] — no null guard
ciServeddtm              CiServeddtm             DateTime?   Guarded: length > 7
cicltName                CicltName               string      Read directly
ciHOLD / ciHold          CiHold                  bool?       Guarded: ciHOLD length > 0; parse uses ciHold (mixed case)
cicltm4id                Cicltm4id               string      Read directly
ciCode                   CiCode                  string      Read directly
ciQueue                  CiQueue                 string      Read directly
ciServedStaff            CiServedStaff           string      Read directly
ciAmt                    CiAmt                   int?        Guarded: length > 0
ciDoses                  CiDoses                 int?        Guarded: length > 0
ciqueuetime              ciQUEUETIME             DateTime?   Guarded: column contains check + length > 6

Computed field (not in source):
    MinutesWaited        MinutesWaited           decimal?    ETL-computed: ciServeddtm - CiTime, truncated to 4 chars

Note: this method uses direct hardcoded field access (`r["fieldname"]`) rather than a
column switch pattern. All field names are case-sensitive string literals.
________________________________________

6. SaveCheckIn — Check-In Load (pats.tbl_CheckIn)

Source: `dbo.tblCheckin` via `st.FromTblVw` + `strWhere` rolling filter
Destination: pats.tbl_CheckIn
Key: `CiId` (within site-scoped pre-load)
Parameters: `tbl`, `sc`, `workdate`, `db`

Early exit when no source rows:
    if (tbl.Rows.Count > 0) { ... }
The entire method body is skipped if the source DataTable is empty.

Anchor row read (before loop):
    int ciid = int.Parse(tbl.Rows[0]["ciid"].ToString())
The `CiId` of the FIRST source row is extracted before the loop and used to scope the
Azure pre-load. This anchors the lookback window to the lowest ID in the current extract.

Partial Azure pre-load:
    chkIns = db.TblCheckIn.Where(x => x.SiteCode == sc
        && (x.CiDate.Date >= workdate.Date || x.CiId >= ciid)
        ).ToList()
Unlike most other Save methods that load the full site slice, SaveCheckIn loads only records
that are either dated on or after `workdate` OR have a `CiId >= ciid`. This is a performance
optimisation — it avoids loading years of historical check-in data.

IMPORTANT: This creates a gap risk. If a source record from within the rolling window has a
`CiId` lower than `tbl.Rows[0]["ciid"]` but a recent `CiDate`, it may still be found via
the date clause. But a record with an old `CiDate` and a high `CiId` (e.g., a corrected
record) might not be in the pre-load window (see Anomaly 4).

AllNewRows flag — DEAD CODE (see Anomaly 1):
    if (chkIns == null) { AllNewRows = true; }
`LINQ .Where().ToList()` always returns a list (empty or populated), never null.
`AllNewRows` is always false. The entire `AllNewRows` fast path is unreachable.

Per-row processing:
    int id  = int.Parse(r["ciID"].ToString())        ← no null guard
    int rcs = int.Parse(r["RowChkSum"].ToString())   ← no null guard
    DateTime dt = DateTime.Parse(r["ciDate"].ToString())  ← no null guard (see Anomaly 3)

Normal lookup path (always used — AllNewRows never true):
    chkin = chkIns.Where(x => x.CiId == id).FirstOrDefault()
    Not found: NewRow = true; new TblCheckIn { SiteCode, CiId, RowChkSum, CiDate }; RowsIns++
    Found:     RowsUpd++

RowChkSum guard — EFFECTIVE:
    if ((chkin.RowChkSum != rcs) || (NewRow))
    {
        // all field mappings
    }
This is a genuine change detection guard. Found records where the checksum has not changed
are skipped — only RowsUpd is incremented but no fields are updated. For new records
(NewRow == true), the guard always fires.

MinutesWaited — ETL-computed field:
    if (r["ciServeddtm"].ToString().Length > 7)
    {
        DateTime sdt = DateTime.Parse(r["ciServeddtm"].ToString())
        chkin.CiServeddtm = sdt
        TimeSpan tsWait = sdt.Subtract(chkin.CiTime)
        int lx = tsWait.TotalMinutes.ToString().Length < 4 ? tsWait.TotalMinutes.ToString().Length : 4
        chkin.MinutesWaited = decimal.Parse(tsWait.TotalMinutes.ToString().Substring(0, lx))
    }
Wait time is calculated as `CiServeddtm - CiTime`. The result in total minutes is truncated
to at most 4 characters (max representable = "9999" = 9999 minutes ≈ 7 days). Negative wait
times (if CiServeddtm < CiTime due to data entry error) produce a negative `TotalMinutes`
string starting with '-', which would be 5+ characters — the substring truncation would yield
a negative decimal. Also: `Substring(0, lx)` where lx=4 on a 3-character negative string
like "-99" would cap to 3 — the sign is preserved (see Anomaly 5).

ciHOLD / ciHold key mismatch (see Anomaly 2):
    if (r["ciHOLD"].ToString().Length > 0)
    {
        chkin.CiHold = bool.Parse(r["ciHold"].ToString());
    }
The null guard reads `r["ciHOLD"]` (uppercase HOLD) but the parse reads `r["ciHold"]`
(mixed case Hold). In SQL Server DataTables, column names are case-insensitive on retrieval,
so this typically works. But it is an inconsistency that could fail if the DataTable enforces
case-sensitive column lookup.

Per-row insert (not AddRange):
    if (NewRow || AllNewRows)   ← AllNewRows always false — only NewRow matters
    {
        NewRow = false;
        db.TblCheckIn.Add(chkin);   ← individual Add, not AddRange
    }
New records are added to the DbContext individually inside the loop.

UpdateRange commented out:
    //db.TblCheckIn.UpdateRange(chkins);
For updated records, no explicit `UpdateRange` or `Update` is called. Instead, EF Core's
change tracking on the pre-loaded `chkIns` list handles updates — since `chkIns` objects
are tracked entities from the DbContext query, modifying their properties directly is
enough for `SaveChanges()` to generate the UPDATE statements. This pattern works correctly.

Single commit:
    db.SaveChanges();
One transaction after the full loop — covers all inserts and all tracked-entity updates.
________________________________________

7. Change Detection — RowChkSum Behaviour

Unlike many other Save methods where RowChkSum is disabled or bypassed, `SaveCheckIn` has
a genuinely effective RowChkSum guard:

    if ((chkin.RowChkSum != rcs) || (NewRow))

For existing records:
    - Checksum unchanged → only RowsUpd incremented; no fields updated; no DB write for this row
    - Checksum changed → all field mappings run; LastModAt = DateTime.Now; DB UPDATE generated

For new records (NewRow == true):
    - Guard always fires regardless of checksum value

RowChkSum is stored on the new-record constructor and updated in the field block:
    Constructor: RowChkSum = rcs
    Field block: (RowChkSum stored at construction; not re-assigned in the field block)
Actually — for existing records, RowChkSum is NOT explicitly updated in the field mapping
block. EF change tracking will not update RowChkSum on existing records unless it is explicitly
assigned. Since only the initial `chkin.RowChkSum = rcs` at construction (for new records)
stores RowChkSum, and no assignment occurs in the update block, existing records' RowChkSum
in Azure is NEVER updated (see Anomaly 6).
________________________________________

8. Anomalies, Bugs, and Known Defects

ANOMALY 1 — AllNewRows is dead code.

File: SaveCheckIn.cs, line 29
    if (chkIns == null) { AllNewRows = true; }
`LINQ .Where().ToList()` never returns null. `AllNewRows` is always false. The fast path at
lines 36–46 is unreachable.

ANOMALY 2 — ciHOLD / ciHold key mismatch.

File: SaveCheckIn.cs, lines 83–86
    if (r["ciHOLD"].ToString().Length > 0)      ← guard reads uppercase HOLD
    { chkin.CiHold = bool.Parse(r["ciHold"].ToString()); }  ← parse reads mixed case Hold

Functionally equivalent in most ADO.NET DataTables (case-insensitive lookup), but the
inconsistency is a latent risk if the DataTable is constructed with a case-sensitive comparer.

ANOMALY 3 — Core fields parsed without null guards.

File: SaveCheckIn.cs, lines 33–35 and line 80
    int id  = int.Parse(r["ciID"].ToString())         ← no length guard
    int rcs = int.Parse(r["RowChkSum"].ToString())    ← no length guard
    DateTime dt = DateTime.Parse(r["ciDate"].ToString())  ← no length guard
    chkin.CiDate = DateTime.Parse(r["ciDate"].ToString()) ← no length guard (line 80)
    chkin.CiTime = DateTime.Parse(r["ciTime"].ToString()) ← no length guard

Any NULL or empty value in `ciID`, `RowChkSum`, `ciDate`, or `ciTime` from the source will
throw a `FormatException`, aborting the entire site's check-in load for the run.

ANOMALY 4 — Partial pre-load creates a lookup gap.

File: SaveCheckIn.cs, lines 26–28
    db.TblCheckIn.Where(x => x.SiteCode == sc
        && (x.CiDate.Date >= workdate.Date || x.CiId >= ciid))
The pre-load uses `tbl.Rows[0]["ciid"]` (the first source row's CiId) as the lower ID bound.
Records in the current source extract with IDs lower than the first row's ID but recent dates
are handled by the date clause. However, if the source extract is sorted by date descending
and the first row has a high CiId, older records with lower IDs but within the date range
may not be found in `chkIns`, triggering incorrect inserts of already-existing rows.

ANOMALY 5 — MinutesWaited computed with string-length truncation.

File: SaveCheckIn.cs, lines 76–78
    int lx = tsWait.TotalMinutes.ToString().Length < 4
              ? tsWait.TotalMinutes.ToString().Length : 4
    chkin.MinutesWaited = decimal.Parse(tsWait.TotalMinutes.ToString().Substring(0, lx))

a) Negative wait times: If CiServeddtm < CiTime (data entry error), TotalMinutes is negative.
   The string starts with '-' (e.g., "-30"). Length > 4 means lx=4, Substring(0,4) = "-30."
   (if fractional) or "-300" — decimal.Parse succeeds with a negative value stored in Azure.
b) Values >= 10000 minutes (~7 days): lx=4 means only the first 4 digits are parsed, silently
   truncating the wait time (e.g., 14400 minutes stored as 1440).

ANOMALY 6 — RowChkSum never updated for existing records.

File: SaveCheckIn.cs — field mapping block (lines 70–105)
RowChkSum is assigned in the new-record constructor but is NOT assigned inside the field
mapping block. When an existing record's checksum changes and the field block fires, the
updated `rcs` value is NOT written back to `chkin.RowChkSum`. EF change tracking will not
update RowChkSum since the property was never re-assigned. This means on the next ETL run,
the Azure RowChkSum for this record still holds the OLD value, causing the guard to fire
again even if source data is truly unchanged.

ANOMALY 7 — RowTrax block present but empty.

File: BHGTaskRunner/Program.cs, lines 583–588
No source-vs-destination row count audit is ever logged.

ANOMALY 8 — Anchor row CiId read without empty DataTable guard.

File: SaveCheckIn.cs, line 25
    int ciid = int.Parse(tbl.Rows[0]["ciid"].ToString())
This is inside `if (tbl.Rows.Count > 0)` — so the DataTable is guaranteed non-empty before
this line. However, `tbl.Rows[0]["ciid"]` could still be null/empty for the first row, which
would throw a FormatException outside the try/catch (the try starts at line 20, so it would
be caught). But the check `if (tbl.Rows.Count > 0)` protects against IndexOutOfRange.
________________________________________

9. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe → tsk.tbl_Tasks (TaskName = pats.tbl_checkin, per SiteCode)
        |
        V
BHGTaskRunner.exe 2 or 4 (Regional ETL P1 / P2)
        |
        |  Probe sys.all_columns for 'CIQUEUETIME' in tblCheckin
        |      Not found → strip [ciQUEUETIME] from strCmd
        |
        |  strCmd += " Where " + strWhere + " " + st.SortOrder
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveCheckIn(SrcDt, SiteCode, WorkDate.AddDays(DaysBack), null)
        |
        |  if (tbl.Rows.Count == 0) → return immediately
        |
        |  ciid = tbl.Rows[0]["ciid"]   ← anchor row ID
        |
        |  chkIns = TblCheckIn where SiteCode==sc
        |                          AND (CiDate >= workdate OR CiId >= ciid)
        |  (AllNewRows always false — LINQ never returns null)
        |
        |  FOREACH source row:
        |      id = r["ciID"]; rcs = r["RowChkSum"]; dt = r["ciDate"]
        |      Lookup: chkIns.Where(x => x.CiId == id).FirstOrDefault()
        |      Not found → new TblCheckIn{SiteCode, CiId, RowChkSum, CiDate}; RowsIns++; db.Add(chkin)
        |      Found     → RowsUpd++
        |      if (RowChkSum changed OR NewRow):
        |          Map: CiCltid, CiTime, CiServeddtm, MinutesWaited (computed),
        |               CiDate, LastModAt, CicltName, CiHold, Cicltm4id,
        |               CiCode, CiQueue, CiServedStaff, CiAmt, CiDoses, ciQUEUETIME (if col exists)
        |          NOTE: RowChkSum NOT re-assigned for existing records
        |
        |  db.SaveChanges()   ← single transaction (inserts via Add + EF-tracked updates)
        |
        V
pats.tbl_CheckIn (Azure BHG_DR)
RowTrax: empty
________________________________________

10. File Reference Map

File Path                                                  Purpose
---------                                                  -------
BCAppCode/BHG-DR-LIB/SaveCheckIn.cs                       SaveCheckIn method (133 lines)
BCAppCode/BHGTaskRunner/Program.cs                         case "pats.tbl_checkin" ~line 573
                                                           ciQUEUETIME column probe
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                 strFlds / strWhere for SELECT
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                     Column probe + data SELECT
BCAppCode/BHG-DR-LIB/Models/TblCheckIn.cs                 EF model → pats.tbl_CheckIn
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs              DbSet<TblCheckIn>
________________________________________

11. Quick Reference Summary

Method         Load path    Key     RowChkSum guard    Pre-load scope                  Schedule
------         ---------    ---     ---------------    ---------------                 --------
SaveCheckIn    EF Core      CiId    Effective          Partial: CiDate >= workdate      2 / 4
                                                       OR CiId >= first-row CiId       (Regional P1/P2)

Key behaviours:
- ciQUEUETIME stripped from SELECT if column absent in source schema
- Partial pre-load by date + anchor CiId (performance opt; gap risk)
- AllNewRows dead code — LINQ never returns null
- RowChkSum guard active — skips unchanged records
- MinutesWaited computed in ETL (not a SAMMS field); truncated to 4-char string
- RowChkSum NOT updated for existing records — guard re-fires every run for changed data
- Single SaveChanges() commit — EF change tracking handles updates; db.Add() for inserts
