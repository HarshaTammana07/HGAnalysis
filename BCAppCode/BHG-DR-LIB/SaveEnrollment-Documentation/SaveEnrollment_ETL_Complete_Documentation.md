
Enrollment ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Regional ETL P1/P2 — BHGTaskRunner.exe 2 / 4
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract patient enrollment
(admit/discharge) records from local SAMMS SQL Server databases at each clinic and load them
into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What enrollment data is and why it is critical to the warehouse
- What systems and files are involved from start to finish
- How BHGTaskRunner dispatches the SaveEnrollment task including two pre-checks:
  `vw_Enrollment.Modality` column detection and `TreatmentLevel` column detection
- How SaveEnrollment.cs works — the conditional pre-pass RowState reset, the RowChkSum
  bypass introduced on 6/26/2023, the AllNewRows fast path, the column switch, the
  PHC SiteId hardcode, the UpdateRange + AddRange single-commit pattern
- The large block of per-site EnrollDate correction code that was disabled
- The `akey` (ActionKey) parameter that is received but not used
- All known anomalies, bugs, and design notes

There is one method in SaveEnrollment.cs:

pats.tbl_Enrollment:   SaveEnrollment   (patient enrollment / admit-discharge records)
________________________________________

2. High-Level Business Summary

What is enrollment data?

An enrollment record in SAMMS captures the formal admission of a patient into a treatment
program and, when applicable, their discharge. It is the central record in the patient journey
timeline — virtually every other clinical record (doses, assessments, counseling sessions,
lab results, billing) links back to an enrollment. Key data points include:
- `EnrollDate` and `DischargeDate` — start and end of the treatment episode
- `Program` — treatment program name
- `Counselor`, `Physician` — assigned clinical staff
- Substance use fields: primary, secondary, and tertiary problem and frequency codes
- Sociodemographic fields at admit/discharge: employment status, living situation,
  education, baby information
- `DischargeReasonCode` / `DischargeReasonText` — why the patient left treatment
- `RowState` — whether the enrollment is currently active in the warehouse

The conditional pre-pass: when the Azure site slice already has rows (`AllNewRows == false`),
all existing enrollment records are set `RowState = false` before processing. Then only
records present in the current source extract are re-activated. This means records that
no longer appear in the rolling-lookback window stay inactive after the run.

Why RowChkSum is bypassed: a comment in the code reads `// Changed 6/26/2023`. The original
conditional `(eroll.RowChkSum == myrcs) || ((eroll.RowChkSum != myrcs) || (NewRow))` was
replaced with `if (true)`, permanently forcing all field mappings on every row. The
RowChkSum is still stored but no longer guards updates.

Load type
EF Core upsert with a conditional pre-pass RowState reset (when Azure rows exist),
`UpdateRange` for all in-memory updates + `AddRange` for new rows, and a single
`SaveChanges()` committing everything together.
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule / tsk.tbl_Tasks (Azure)  Task queue — ActionKey 2/4 for Regional ETL P1/P2
BHGTaskRunner.exe 2 / 4                   ETL orchestrator — Regional ETL P1 / P2
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)            Column list + RowChkSum expression for enrollment SELECT
SelectConstructor.cs                       Builds `strFlds` / `strWhere` (rolling lookback)
SQLSvrManager.cs                          Executes column-existence probes and data SELECT
SaveEnrollment.cs (BHG-DR-LIB)           1 method — SaveEnrollment
Models/TblEnrollment.cs                   EF entity → pats.tbl_Enrollment
pats.tbl_Enrollment (Azure BHG_DR)       Final destination for enrollment records
tsk.tbl_RowTrax (Azure DB)               Audit log — ACTIVE for enrollment (one of few tasks that logs RowTrax)
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 1005

CASE: pats.tbl_enrollment

Lab site exclusion:
    if (st.SiteCode != "Lab")
The Lab site code is completely skipped for enrollment data.

Column existence pre-check 1 — Modality (only if source is vw_Enrollment):
    if (st.FromTblVw == "vw_Enrollment")
    {
        tblcols = sm.GetTableData("tcols",
            "select name, column_id from sys.all_columns where object_id =
            (select object_id from sys.all_views where upper(name) = 'VW_ENROLLMENT')
            and name = 'Modality'",
            st.ConStr)
        if (tblcols.Rows.Count > 0 || st.SiteCode == "CBNC")
        {
            strCmd = "Select " + strFlds + ", Modality from " + st.SrcSchema + "." + st.FromTblVw
            Console.WriteLine("Modality included in Site " + st.SiteCode)
        }
    }
If `vw_Enrollment` contains a `Modality` column (or the site is CBNC), the SELECT is rebuilt
to append `Modality`. For all other source views/tables, the standard `strFlds` SELECT is used.

Column existence pre-check 2 — TreatmentLevel (applies to any FromTblVw):
    tblNRollcols = sm.GetTableData("tcols",
        "select name, column_id from sys.all_columns where object_id =
        (select object_id from sys.all_objects where upper(name) = '{FromTblVw}')
        and name = 'TreatmentLevel'",
        st.ConStr)
    if (tblNRollcols.Rows.Count == 0)
    {
        strCmd = strCmd.Replace(", [TreatmentLevel] TreatmentLevel", "").Replace(", [TreatmentLevel]", "")
    }
If the source view/table does not have a `TreatmentLevel` column, it is stripped from the
SELECT to avoid a column-not-found error.

Standard WHERE applied after all column adjustments:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

Call:
    rCodes = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null)

RowTrax — ACTIVE (one of the few tasks that actually logs audit counts):
    if (st.RowTrax.HasValue && st.RowTrax.Value)
    {
        aztblcnt = count(*) from pats.tbl_Enrollment where SiteCode = sc and RowState = 1
        sd.SaveRowTrax(sc, WorkDate, "pats.tbl_Enrollment", SrcDt.Rows.Count, aztblcnt, null)
    }
Source row count vs. Azure active row count is logged after each site's load.
________________________________________

5. Source Table — SAMMS SQL Server (dbo)

The source is typically `dbo.vw_Enrollment` or an equivalent base table/view.
Key columns consumed by the column switch (lower-cased for matching):

Field (switch name)       Maps to EF property          Notes
-------------------       -------------------          -----
(id — read directly)      Id                           int — read outside switch as r["id"]
cltid                     CltId                        int? — guarded length > 0
program                   Program                      string
enrolldate                EnrollDate                   DateTime? — guarded Trim().Length > 7
dischargedate             DischargeDate                DateTime? — guarded Trim().Length > 7; explicitly null if short
enrollreasoncode          EnrollReasonCode             string
enrollreasontext          EnrollReasonText             string
dischargereasoncode       DischargeReasonCode          string
dischargereasontext       DischargeReasonText          string
strstaff                  StrStaff                     string
transfer                  Transfer                     bool? — guarded length > 0
parentenrollid            ParentEnrollId               int? — guarded length > 0
nodarts_enroll            NoDartsEnroll                bool? — guarded length > 0
nodarts_discharge         NoDartsDischarge             bool? — guarded length > 0
repoldenroll              RepOldEnroll                 decimal? — guarded length > 0
dasareason                Dasareason                   string
dtlastcontact             DtLastContact                DateTime? — guarded length > 7
strarrests                StrArrests                   string
strbaby                   StrBaby                      string
strbabydf                 StrBabyDf                    string
streduc                   StrEduc                      string
strempstat                StrEmpStat                   string
strliving                 StrLiving                    string
strnilf                   StrNilf                      string
strprifreq                StrPriFreq                   string
strpriprob                StrPriProb                   string
strsecfreq                StrSecFreq                   string
strsecprob                StrSecProb                   string
strselfhelp               StrSelfHelp                  string
strselfhelpdet            StrSelfHelpDet               string
strsuppint                StrSuppInt                   string
strterfreq                StrTerFreq                   string
strterprob                StrTerProb                   string
strschooljobtraining      StrSchoolJobTraining         string
counselor                 Counselor                    string
dischargesubreasoncode    DischargeSubReasonCode       string
enrollsubreasoncode       EnrollSubReasonCode          string
ondemand                  OnDemand                     bool? — guarded length > 0
physician                 Physician                    string
siteid                    SiteId                       int? — PHC hardcoded to 105; else int.Parse
module                    Module                       string
modulenote                Modulenote                   string
dischargeincome           DischargeIncome              string
intakeincome              IntakeIncome                 string
strdbnotes                StrDbnotes                   string
deleterecord              Deleterecord                 string
upsizets                  (FULLY COMMENTED OUT)        byte[] mapping disabled (see Anomaly 4)
modality                  Modality                     string — added only if column detected
treatmentlevel            TreatmentLevel               string — added only if column detected

RowChkSum read directly (outside switch):
    myrcs = int.Parse(r["RowChkSum"].ToString()) if tbl.Columns.Contains("rowchksum")
________________________________________

6. SaveEnrollment — Enrollment Load (pats.tbl_Enrollment)

Source: `st.FromTblVw` (typically `vw_Enrollment`) + `strWhere` rolling filter
Destination: pats.tbl_Enrollment
Key: `Id` (enrollment ID — read directly as `r["id"]` before the column switch)
Parameters: `tbl`, `sc`, `akey`, `db`

Azure pre-load:
    erolls = db.TblEnrollment.Where(x => x.SiteCode == sc).ToList()
Full site slice for the clinic.

AllNewRows fast path:
    if (erolls.Count == 0) { AllNewRows = true; }
When no Azure rows exist for the site, the pre-pass is skipped and every source row takes
the new-record path without a lookup.

Conditional pre-pass RowState deactivation (only when erolls.Count > 0):
    foreach (TblEnrollment e in erolls)
    {
        e.RowState = false;
    }
Note: `LastModAt` is NOT stamped in the pre-pass (contrast with `SaveFmp` which does stamp it).
This deactivation is committed together with all other changes in the single `db.SaveChanges()`.

Per-row RowChkSum extraction (before switch):
    enid = int.Parse(r["id"].ToString())
    if (tbl.Columns.Contains("rowchksum")) { myrcs = int.Parse(r["RowChkSum"].ToString()); }

AllNewRows path (no Azure rows exist):
    NewRow = true
    eroll = new TblEnrollment { SiteCode = sc, Id = enid, RowChkSum = myrcs }
    res.RowsIns += 1

Normal lookup path (erolls exist):
    eroll = erolls.Where(x => x.Id == enid).FirstOrDefault()
    Found:     res.RowsUpd += 1
    Not found: NewRow = true; new TblEnrollment { SiteCode, Id, RowChkSum }; res.RowsIns += 1

RowChkSum guard — BYPASSED (changed 6/26/2023):
    // Original: (eroll.RowChkSum == myrcs) || ((eroll.RowChkSum != myrcs) || (NewRow))
    if (true)     ← permanent bypass
    {
        eroll.RowState = true;
        eroll.RowChkSum = int.Parse(r["RowChkSum"].ToString());
        eroll.LastModAt = DateTime.Now;
        foreach (DataColumn c in r.Table.Columns) { switch ... }
    }
All field mappings fire unconditionally. RowState is always set to `true` here. `LastModAt`
is `DateTime.Now` (unlike `SaveFmp` which uses `DateTime.Today`).

PHC SiteId hardcode (inside switch):
    case "siteid":
        if (eroll.SiteCode == "PHC") { eroll.SiteId = 105; }
        else { if (length > 0) { eroll.SiteId = int.Parse(...); } }
PHC is always assigned SiteId=105 regardless of the source value.

New row tracking:
    if (NewRow || AllNewRows)
    {
        NewRow = false;
        erolls.Add(eroll);     ← adds to the in-memory list (for AllNewRows subsequent lookups)
        ERNew.Add(eroll);      ← batches for AddRange
    }

Single-commit pattern (no interim SaveChanges):
    db.TblEnrollment.UpdateRange(erolls);   ← marks ALL in-memory erolls as Modified
    if (ERNew.Count > 0) { db.TblEnrollment.AddRange(ERNew); }
    db.SaveChanges();                        ← single transaction for everything

Important: `UpdateRange(erolls)` is called on the entire pre-loaded site slice, which now
includes records that were deactivated in the pre-pass AND records updated in the loop AND
new records added to `erolls`. This means EVERY row in the Azure site slice is written back
to the database on every ETL run (see Anomaly 3).

Error handling — ExceptMsg includes entity state:
    res.ExceptMsg = e.Message + " " + eroll.SiteCode.ToString() + " " + eroll.CltId.ToString()
Unlike other methods, the catch block appends the last processed `eroll.SiteCode` and
`eroll.CltId` to the error message. This aids debugging but risks a secondary
`NullReferenceException` if `eroll` is null or `CltId` is null at the time of the exception
(see Anomaly 6).
________________________________________

7. Change Detection — RowChkSum Behaviour

Prior to 6/26/2023, the intent was:
- If checksum matches AND it is not a new row: skip field mapping (only RowState + LastModAt
  would have been updated in the commented-out else block)
- If checksum differs OR new row: run full column switch

After 6/26/2023, `if (true)` permanently bypasses all checksum logic. Every row in every run
receives a full column switch pass and RowState=true + LastModAt=DateTime.Now. The RowChkSum
is still stored (`eroll.RowChkSum = int.Parse(r["RowChkSum"].ToString())`) for reference but
is never compared.
________________________________________

8. Commented-Out Per-Site EnrollDate / DischargeDate Correction Code

Lines 242–462 contain a large `#region Hide old code` block with a site-specific switch
statement that was used to correct historically corrupt or misconfigured enrollment dates
across ~25 named sites (B24, B30, B33, B35A–B55, DRD-SF, NOLA, SFN, TTCA, TTCB, V10A,
V12, V12A). Examples:
- B33: `EnrollDate == "1913-06-18"` corrected to `"2013-06-18"` (century transposition)
- NOLA: `EnrollDate == "6002-07-08"` corrected to `"2006-07-08"` (digit transposition)
- B35A: dates before 2018-06-01 nulled out (site went live on that date)
- DRD-SF: specific `DischargeDate` swapped between two dates (apparent data entry correction)

This entire block is commented out. It represents a significant body of historical data
quality remediation work — if re-enabled, it would silently alter source dates on 25+ sites.
________________________________________

9. Anomalies, Bugs, and Known Defects

ANOMALY 1 — RowChkSum guard permanently bypassed via `if (true)`.

File: SaveEnrollment.cs, line 76
    // Changed 6/26/2023
    if (true)
The original change detection was replaced with an unconditional pass. RowChkSum is still
stored but no longer prevents unnecessary field overwrites. On every run, every row from the
source extract gets a full column mapping applied, `RowState=true`, and `LastModAt=DateTime.Now`.

ANOMALY 2 — AllNewRows is set but creates an asymmetric code path.

File: SaveEnrollment.cs, lines 25–56
When `AllNewRows == true`, every source row takes the new-record path without looking up
`erolls` (which is empty). New records are still added to `erolls` during the loop
(line 467), which means later rows within the same batch could find a previously added row
if `AllNewRows` is true but duplicates exist in the source. In practice `AllNewRows` is only
ever true for a brand-new site, making this a low-risk edge case.

ANOMALY 3 — `UpdateRange(erolls)` writes the entire site slice back on every run.

File: SaveEnrollment.cs, line 478
    db.TblEnrollment.UpdateRange(erolls);
`UpdateRange` marks ALL objects in `erolls` as `EntityState.Modified`, including:
- Rows that were deactivated in the pre-pass and not refreshed from source
- Rows that were found and updated normally
- Any row in the site slice that was not touched at all

This causes EF Core to generate `UPDATE` statements for every enrollment record for the
site on every ETL run, regardless of whether any field changed. For sites with large
historical enrollment histories, this is a substantial and unnecessary write load.

ANOMALY 4 — `upsizets` mapping permanently commented out.

File: SaveEnrollment.cs, lines 228–233
    case "upsizets":
        // if (...) { eroll.UpsizeTs = Encoding.ASCII.GetBytes(r["UpsizeTs"].ToString()); }
        break;
The timestamp/binary migration field is never mapped. Its case label is present but the
body is fully commented out.

ANOMALY 5 — `akey` (ActionKey) parameter received but never used.

File: SaveEnrollment.cs, method signature line 11
    public Models.RCodes SaveEnrollment(DataTable tbl, string sc, long akey, Models.BHG_DRContext db)
`akey` is passed from BHGTaskRunner as `st.ActionKey` but is never referenced in the method
body. It was presumably intended to allow different behaviour for ActionKey 2 vs. 4 (Regional
P1 vs. P2) but was never implemented.

ANOMALY 6 — ExceptMsg appends entity fields that may throw a secondary NullReferenceException.

File: SaveEnrollment.cs, line 488
    res.ExceptMsg = e.Message + " " + eroll.SiteCode.ToString() + " " + eroll.CltId.ToString()
If the original exception occurred before `eroll` was properly initialised, or before
`eroll.CltId` was set (it is `int?`), `eroll.CltId.ToString()` on a null `int?` would not
throw (returns empty string). However, if `eroll` itself is null, a `NullReferenceException`
would be thrown inside the catch block, masking the original exception.

ANOMALY 7 — Pre-pass does not stamp LastModAt on deactivated rows.

File: SaveEnrollment.cs, lines 36–40
    foreach (TblEnrollment e in erolls)
    {
        e.RowState = false;
    }
`LastModAt` is not updated during the pre-pass deactivation. If a record is deactivated
(drops out of the rolling source window), its `LastModAt` in Azure will reflect the last time
it was actively updated — not the run when it was marked inactive. This differs from the
`SaveFmp` behaviour which does stamp `LastModAt` in its pre-pass.

ANOMALY 8 — Date guard uses `> 7` instead of the standard `> 6`.

File: SaveEnrollment.cs, lines 92–93 and 96–97
    if (r["EnrollDate"].ToString().Trim().Length > 7)
    if (r["DischargeDate"].ToString().Trim().Length > 7)
Most other Save methods use `> 6` for DateTime guard thresholds. Using `> 7` means a valid
7-character date string (e.g., "1/1/200" — unlikely but possible) would be skipped.
Functionally this is not impactful in practice but is an inconsistency.
________________________________________

10. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe → tsk.tbl_Tasks (TaskName = pats.tbl_enrollment, per SiteCode)
        |
        V
BHGTaskRunner.exe 2 or 4 (Regional ETL P1 / P2)
        |
        |  if SiteCode == "Lab" → skip entirely
        |
        |  if FromTblVw == "vw_Enrollment":
        |      Probe sys.all_columns for "Modality" column
        |      If found (or CBNC): strCmd += ", Modality"
        |
        |  Probe sys.all_objects for "TreatmentLevel" column
        |      If not found: strip TreatmentLevel from strCmd
        |
        |  strCmd += " Where " + strWhere + " " + st.SortOrder
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveEnrollment(SrcDt, SiteCode, ActionKey, null)
        |
        |  erolls = all pats.tbl_Enrollment rows for SiteCode
        |
        |  If erolls.Count == 0 → AllNewRows = true (skip pre-pass)
        |  Else → pre-pass: all e.RowState = false
        |
        |  FOREACH source row:
        |      enid = int.Parse(r["id"])
        |      myrcs = r["RowChkSum"] if column exists
        |      Lookup: erolls.Where(x => x.Id == enid).FirstOrDefault()
        |      Found:     res.RowsUpd++
        |      Not found: new TblEnrollment { SiteCode, Id, RowChkSum }; res.RowsIns++
        |      if (true) [RowChkSum bypass]:
        |          eroll.RowState = true; eroll.RowChkSum = myrcs; eroll.LastModAt = DateTime.Now
        |          ~44-field column switch
        |          PHC: SiteId = 105 hardcoded
        |      if NewRow/AllNewRows: erolls.Add(eroll); ERNew.Add(eroll)
        |
        |  db.TblEnrollment.UpdateRange(erolls)    ← marks ENTIRE site slice as Modified
        |  db.TblEnrollment.AddRange(ERNew)         ← stages new records
        |  db.SaveChanges()                         ← single transaction
        |
        V
pats.tbl_Enrollment (Azure BHG_DR)
        |
        V
RowTrax ACTIVE → sd.SaveRowTrax(SiteCode, WorkDate, TaskName, srcCnt, azCnt, null)
BHGTaskRunner marks task Status=20 (complete)
________________________________________

11. File Reference Map

File Path                                                  Purpose
---------                                                  -------
BCAppCode/BHG-DR-LIB/SaveEnrollment.cs                    SaveEnrollment method (500 lines)
BCAppCode/BHGTaskRunner/Program.cs                         case "pats.tbl_enrollment" ~line 1005
                                                           Modality + TreatmentLevel column probes
                                                           Lab exclusion; RowTrax logging
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                 strFlds / strWhere for SELECT build
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                     Executes column probes and data SELECT
BCAppCode/BHG-DR-LIB/Models/TblEnrollment.cs              EF Model → pats.tbl_Enrollment
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs              DbSet<TblEnrollment>
________________________________________

12. Quick Reference Summary

Method            Load path    Key    RowChkSum guard    Pre-pass RowState    RowTrax    Schedule
------            ---------    ---    ---------------    ----------------    -------    --------
SaveEnrollment    EF Core      Id     Bypassed (if true) Conditional (if     ACTIVE     2/4 (Regional
                                      since 6/26/2023)   erolls.Count > 0)             ETL P1/P2)

Key behaviours:
- Lab site skipped entirely
- Modality added to SELECT dynamically if column exists in vw_Enrollment (or site is CBNC)
- TreatmentLevel stripped from SELECT if column not present in source object
- RowChkSum stored but never compared — all rows fully overwritten
- UpdateRange(erolls) writes entire site slice back on every run
- PHC SiteId hardcoded to 105
- akey parameter received but never used
- Large per-site date correction switch (25+ sites) is commented out
