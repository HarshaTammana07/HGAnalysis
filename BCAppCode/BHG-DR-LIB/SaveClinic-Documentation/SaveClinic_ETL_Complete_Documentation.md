
Clinic Configuration ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 1 — SAMMSGlobal
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract clinic configuration
records from local SAMMS SQL Server databases at each clinic and load them into the central
Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What clinic configuration data is and why it exists in the warehouse
- What systems and files are involved from start to finish
- How BHGTaskRunner dispatches the SaveClinic task
- How SaveClinic.cs works — the pkey lookup, the ~150-field column switch, and the
  single-commit insert/update pattern
- The Lab site column exclusion
- The absence of RowChkSum — all fields are unconditionally overwritten on every run
- The AllNewRows dead code flag
- All known anomalies, bugs, and design notes

There is one method in SaveClinic.cs:

ctrl.tbl_Clinic:   SaveClinic   (clinic system configuration)
________________________________________

2. High-Level Business Summary

What is clinic configuration data?

Every SAMMS clinic maintains a single row in its local `tblClinic` table that holds the entire
system configuration for that site — over 150 boolean, integer, decimal, and string flags that
control SAMMS application behaviour. These include medication dispensing settings
(DoseWarn, DoseStop, Bottles, FastDose, SignBeforeDose), inventory controls (NumInventory,
EnableInventory4and5, OtherInvType), UA testing rules (SchedUA, UAMonthly, UAOnVisit,
AutoImportUA), billing configuration (BillHold, BillDirection, Combine3payfees, DoseCharge,
AuthBasedOnProgram), security policies (PasswordEnforce, PasswordLength, PinSigs, CoSign,
ClientSecurity), printing and label settings (Zebra, LandscapeLabel, PrintDoseAmt,
NewBottleLabels), calendar and scheduling flags (FiveDayCalendarWeek, CalendarStartTime,
Blockaptcalhold), integration credentials (EligPw, EligUn, ToxAcct, LabAcct, ToxProvider),
and many others.

This clinic configuration row is replicated to the Azure warehouse so that enterprise
reporting and analytics can reference per-site settings without querying each SAMMS SQL
Server directly.

Load type
SaveClinic uses an EF Core upsert. Existing records are found by `Pkey`, updated in memory,
and all writes are committed in a single `db.SaveChanges()` call at the end of the loop.
New records are added to the DbContext inside the loop and committed at the same time.
There is no RowChkSum — every field is overwritten unconditionally on every run.
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule (Azure DB)               Defines schedules and run times
Scheduler.exe                             Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=1                   ETL orchestrator — SAMMSGlobal (primary)
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)            Column list for ctrl.tbl_clinic task
SQLSvrManager.cs                          Fires SELECT against the clinic SAMMS SQL Server
SaveClinic.cs (BHG-DR-LIB)               1 method — SaveClinic
Models/TblClinic.cs                       EF entity → ctrl.tbl_Clinic
ctrl.tbl_Clinic (Azure BHG_DR)           Final destination for clinic configuration
tsk.tbl_RowTrax (Azure DB)               Audit log — present but empty for this task
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 249

CASE: ctrl.tbl_clinic    (comment in code: "//What's up with this?")

Lab site column exclusion:
    if (st.SiteCode == "Lab")
    {
        strCmd = strCmd.Replace(", PullPicsFromDB", "");
    }
The Lab site does not store patient photos in the SAMMS database, so the PullPicsFromDB
column is stripped from the SELECT before it is executed. All other sites receive the
full column list.

Standard date-scoped SELECT:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

Call:
    rCodes = sd.SaveClinic(SrcDt, st.SiteCode, null)
    rCodes.RowsProcessed = SrcDt.Rows.Count    ← explicitly re-set from source count

RowTrax block: EMPTY — no audit logged.

Note: The commented-out code at line 104 shows that `ctrl.tbl_clinic` was historically
considered for a "Lasttbl skip" optimisation (forcing a new DbContext per site) — this
was never activated, so SaveClinic shares the default context lifecycle.
________________________________________

5. Source Table — SAMMS SQL Server (dbo)

dbo.tblClinic — Clinic System Configuration

This is typically a single-row table per SAMMS database (one row per physical clinic).
Key columns relevant to the switch (selected subset — full list is ~150 fields):

DOSING / MEDICATION
    pkey              int         Primary key (external lookup key — read directly, not via switch)
    dosewarn          varchar     Dose warning threshold
    dosestop          varchar     Dose stop threshold
    bottles           bit         Bottle dispensing mode
    tabs              bit         Tablet dispensing mode
    liquid            bit         Liquid dispensing mode
    fastdose          bit         Fast dose mode
    signbeforedose    bit         Require signature before dose
    dosecharge        bit         Charge for dose
    order2confirm     int         Orders requiring 2 confirmations
    orderconfirm      int         Order confirmation setting
    nursesig          bit         Require nurse signature
    pinsigs           bit         PIN-based signatures
    pinbeforesig      bit         PIN required before signature
    siglcd            bit         LCD signature pad enabled

UA / TESTING
    schedua           bit         Scheduled UA testing
    uamonthly         bit         Monthly UA required
    uaonvisit         bit         UA on visit
    autoimportua      bit         Auto-import UA results
    udspanelrequired  bit         UDS panel required
    advancedtesting   bit         Advanced testing mode
    beakercolors      bit         Beaker colour coding

INVENTORY
    numinventory      int         Number of inventory items
    enableinventory4and5 bit      Enable inventory types 4 and 5
    otherinvtype      varchar     Other inventory type name
    bottleweight      decimal     Bottle weight for diversion checks
    spgravity         decimal     Specific gravity threshold
    spgravityclear    decimal     Specific gravity clear threshold
    diversion_padding int         Diversion padding days
    diversiontype     varchar     Diversion type setting

BILLING / INSURANCE
    billhold          int         Billing hold threshold
    billdirection     varchar     Billing direction setting
    combine3payfees   bit         Combine 3rd party payer fees
    authbasedonprogram bit        Authorisation based on program
    tpautomation      int         Third-party automation mode
    usecostcenter     bit         Use cost centre in billing
    isihc             bit         IHC billing mode
    enableautospsammsbilling bit  Auto SP SAMMS billing

SECURITY / ACCESS
    passwordenforce   bit         Enforce password policy
    passwordlength    int         Minimum password length
    clientsecurity    bit         Client security mode
    cosign            bit         Co-sign required
    forcecheckin      bit         Force check-in
    autocheckin       bit         Auto check-in
    isbhg             bit         BHG-owned clinic flag
    isrnp             bit         RNP clinic flag
    issh              bit         SH clinic flag
    multitenant       bit         Multi-tenant mode

CALENDAR / SCHEDULING
    fivedaycalendarweek  bit      Five-day calendar week
    calendarstarttime    int      Calendar start time offset
    blockaptcalhold      bit      Block appointment calendar holds
    openonsunday         bit      Open on Sunday

PRINTING / LABELS
    zebra              bit        Zebra printer mode
    landscapelabel     bit        Landscape label printing
    printdoseamt       bit        Print dose amount on label
    newbottlelabels    bit        New bottle labels format
    smallreceipts      bit        Small receipts mode
    smalltox           bit        Small toxicology label

INTEGRATION
    toxprovider        varchar    Toxicology provider name
    toxacct            varchar    Toxicology account number (note: read as r["ToxACCT"])
    labacct            varchar    Lab account number
    eligpw             varchar    Eligibility password
    eligun             varchar    Eligibility username
    scanpath           varchar    Document scan path
    reportdir          varchar    Report directory path
    reportserver       varchar    Report server URL
    wordpath           varchar    Word template path
    sigimgpath         varchar    Signature image file path
    sigimguri          varchar    Signature image URI
    uapath             varchar    UA document path
    iispath            varchar    IIS integration path
    doctemplatepath    varchar    Document template path
    hnpurl             varchar    H&P URL
    intakepacketurl    varchar    Intake packet URL

DATES
    lastupdated        datetime   Last configuration update (no null guard — see Anomaly 3)
    datesigstart       datetime   Electronic signature start date (no null guard — see Anomaly 3)
________________________________________

6. SaveClinic — Clinic Configuration Load (ctrl.tbl_Clinic)

Source: dbo.tblClinic (or clinic view via st.FromTblVw)
Destination: ctrl.tbl_Clinic
Key: Pkey (extracted directly from row outside the switch)
Parameters: tbl, sc, db
No wrkdt parameter.

Azure pre-load:
    clinics = db.TblClinic.Where(x => x.SiteCode == sc).ToList()
Full site slice — typically 0 or 1 records per clinic.

AllNewRows flag:
    if (clinics.Count == 0) { AllNewRows = true; }
This flag is set but NEVER READ anywhere in the method — it is dead code (see Anomaly 1).

Per-row lookup (OUTSIDE the column switch):
    int pkey = int.Parse(r["pkey"].ToString())
    c = clinics.Where(x => x.Pkey == pkey).FirstOrDefault()
    if (c == null)
    {
        c = new TblClinic { Pkey = pkey, SiteCode = sc }
        NewRow = true
        rCodes.RowsIns += 1
    }
    else { rCodes.RowsUpd += 1 }

The `pkey` is the only lookup field. `SiteCode` is assigned at construction time for new
records only — it is NOT re-stamped from the source row on the existing-record path.

Column switch (~150 fields):
Fields are mapped from the DataTable row to the EF entity. Boolean fields (the majority)
use `bool.Parse(...)` guarded by `length > 0`. Integer fields use `int.Parse(...)` guarded
by `length > 0`. Decimal fields use `decimal.Parse(...)` guarded by `length > 0`.
String fields are assigned directly without a guard.

No RowChkSum case is present in the switch. There is no change detection — every field
of every found record is overwritten on every ETL run regardless of whether data changed.

NewRow commit (inside loop):
    if (NewRow)
    {
        db.TblClinic.Add(c)
        NewRow = false
    }
New records are added to the EF DbContext immediately inside the loop (per-row Add),
but the actual INSERT does not happen until `db.SaveChanges()` is called after the loop.

Single-commit:
    db.SaveChanges()    ← called ONCE after the outer foreach loop
All updates AND inserts for the entire DataTable are committed in one transaction.

No RowState, no RowSate, no IsActive, no soft-delete of any kind.

Error handling:
    catch (Exception e)
    {
        rCodes.ExceptMsg = e.Message.ToString()
        if (e.InnerException != null) { rCodes.ExceptInnerMsg = e.InnerException.Message.ToString() }
    }
Correct pattern — null check on e.InnerException before accessing .Message.
________________________________________

7. Anomalies, Bugs, and Known Defects

ANOMALY 1 — AllNewRows is dead code.

File: SaveClinic.cs, lines 19–22
    bool AllNewRows = false;
    if (clinics.Count == 0) { AllNewRows = true; }
    
`AllNewRows` is set but never read or used anywhere in the method. The same dead-code
pattern exists in `SaveClientDemo1`, `SaveClientDemo2`, and `SaveClientDemo3` in
SaveCleints.cs. No data impact, but the flag adds confusion.

ANOMALY 2 — No RowChkSum guard — full overwrite on every run.

File: SaveClinic.cs — no checksum case in the column switch
The `ctrl.tbl_clinic` task has no RowChkSum expression. On every ETL run, all ~150 fields
of every found clinic record are unconditionally overwritten. For a configuration table that
rarely changes, this is a performance waste but not a data correctness issue. It does mean
that LastUpdated in Azure reflects when the ETL last ran, not when the clinic admin changed
a setting.

ANOMALY 3 — DateTime fields parsed without null or length guard.

File: SaveClinic.cs, lines 71 and 137
    case "lastupdated":  c.LastUpdated = DateTime.Parse(r["lastupdated"].ToString());
    case "datesigstart": c.DateSigStart = DateTime.Parse(r["datesigstart"].ToString());
    
Neither field has a `length > 0` guard before `DateTime.Parse`. If the source field is
NULL or empty, this will throw a `FormatException`, aborting the entire site's load.
The standard safe pattern used elsewhere in the codebase is:
    if (r["field"].ToString().Length > 6) { c.Field = DateTime.Parse(...); }

ANOMALY 4 — toxtixnum parsed without null guard.

File: SaveClinic.cs, line 278
    case "toxtixnum":  c.Toxtixnum = int.Parse(r["Toxtixnum"].ToString());
    
Unlike every other integer field in this method, `toxtixnum` has no `length > 0` guard
before `int.Parse`. An empty or null source value will throw a `FormatException`.

ANOMALY 5 — SiteCode not re-stamped on existing record path.

File: SaveClinic.cs, lines 29–36
SiteCode is assigned only in the new-record constructor block:
    c = new TblClinic { Pkey = pkey, SiteCode = sc }
On the existing-record (update) path, SiteCode is never re-assigned. There is no `sitecode`
case in the column switch either. This means if a clinic record is moved between sites, the
SiteCode would never be corrected by the ETL.

ANOMALY 6 — RowTrax block present but empty.

File: BHGTaskRunner/Program.cs, lines 258–263
    if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
    {
        if (st.RowTrax.Value) { }    ← EMPTY
    }
No source-vs-destination row count audit is ever logged for this task.

ANOMALY 7 — Developer comment in production code.

File: BHGTaskRunner/Program.cs, line 249
    case "ctrl.tbl_clinic":  //What's up with this?
A developer question about the routing of this case is embedded in production code.
This suggests the placement of clinic configuration data under the `ctrl.` schema
(rather than a `pats.` or `ref.` schema) was considered unusual at the time of writing.
________________________________________

8. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe
        |
        |-- insert parent task (SAMMSGlobal)
        |-- insert child tasks per clinic:
        |       ctrl.tbl_clinic    SiteCode='B01A'
        |       ctrl.tbl_clinic    SiteCode='B01B'
        |       ...
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        V
BHGTaskRunner.exe 1 (SAMMSGlobal)
        |
        |  if (SiteCode == "Lab") → strip PullPicsFromDB column from SELECT
        |  strCmd += " Where " + strWhere + " " + st.SortOrder
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveClinic(SrcDt, SiteCode, null)
        |
        |  Pre-load full site slice from ctrl.tbl_Clinic
        |  (AllNewRows flag set if none found — DEAD CODE, never used)
        |
        |  For each source row:
        |      pkey = int.Parse(r["pkey"])
        |      Lookup: clinics.Where(x => x.Pkey == pkey).FirstOrDefault()
        |      Found:     rCodes.RowsUpd += 1
        |      Not found: new TblClinic { Pkey, SiteCode }; db.TblClinic.Add(c); rCodes.RowsIns += 1
        |      Map ~150 columns via switch (no RowChkSum — full overwrite)
        |
        |  db.SaveChanges()   ← single commit for all rows
        |
        V
ctrl.tbl_Clinic (Azure BHG_DR)
        |
        V
rCodes.RowsProcessed = SrcDt.Rows.Count
RowTrax EMPTY — no audit logged
BHGTaskRunner marks task Status=20 (complete)
________________________________________

9. File Reference Map

File Path                                              Purpose
---------                                              -------
BCAppCode/BHG-DR-LIB/SaveClinic.cs                    Single method SaveClinic (840 lines, ~150-field switch)
BCAppCode/BHGTaskRunner/Program.cs                     Dispatch — case "ctrl.tbl_clinic" ~line 249
BCAppCode/BHG-DR-LIB/SelectConstructor.cs             Builds SELECT column list from dms.tbl_MapSrc2Dsn
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                 Executes SELECT against SAMMS SQL Server
BCAppCode/BHG-DR-LIB/Models/TblClinic.cs              EF Model → ctrl.tbl_Clinic
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs          EF DbContext — DbSet<TblClinic> registration
________________________________________

10. Quick Reference Summary

Method          Load path    Key     RowChkSum    RowState    Schedule
------          ---------    ---     ---------    --------    --------
SaveClinic      EF Core      Pkey    None         None        1 (SAMMSGlobal)

Fields: ~150 configuration flags across bool, int, decimal, varchar, datetime types.
No soft-delete. No change detection. Single SaveChanges() after full loop.
Lab site: PullPicsFromDB column stripped from SELECT before data fetch.

Critical bugs:
1. lastupdated and datesigstart — DateTime.Parse without null/length guard — FormatException risk
2. toxtixnum — int.Parse without null guard — FormatException risk
3. AllNewRows flag set but never read — dead code
4. RowTrax block empty — no audit logging
5. SiteCode not re-stamped on existing record update path




SaveClinic.cs
Method: SaveClinic
Field	Value
Name	SaveClinic
Module	Clinic system configuration
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode
Source table	dbo.tblClinic; column list from dms.tbl_MapSrc2Dsn (ActionKey 1)
Target DB	Azure SQL — BHG_DR
Target table	ctrl.tbl_Clinic
Load type	EF Core upsert — full site slice pre-load; lookup by Pkey; no RowChkSum — all ~150 fields unconditionally overwritten; new records added to DbContext inside loop; single SaveChanges() after loop; Lab site strips PullPicsFromDB from SELECT
Load type column	No RowChkSum, no RowState, no soft-delete; AllNewRows flag set but never read (dead code)
Frequency	Daily
Schedule	Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal)
Parent	SAMMSGlobal
Downstream	ctrl.tbl_Clinic → enterprise reporting referencing per-site configuration flags
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveClinic(SrcDt, st.SiteCode, null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveClinic.cs
Status	Active
Folder	BHG-DR-LIB\SaveClinic.cs; detail in SaveClinic-Documentation\SaveClinic_ETL_Complete_Documentation.md