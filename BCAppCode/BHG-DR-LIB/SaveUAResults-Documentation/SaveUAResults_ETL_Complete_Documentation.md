
UA Results / Lab Results ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 5 — Samms-LAB (primary) / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract Urinalysis (UA) and
Laboratory result data from local SAMMS SQL Server databases at each clinic and load them
into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What UA results, lab results, UA result detail, and UA schedule data are and why they exist
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and BHGTaskRunner dispatches them under Schedule 5
- How all four methods in SaveUAResults.cs work in detail
- The unique reInit / reload reload mechanisms in SaveLABResults and SaveUAResults
- The rolling date-window scope logic used for Azure pre-load
- How RowChkSum change detection operates — and where it is effectively defeated
- The bulk-path replacement for SaveUAResultDetail in production
- All known anomalies, bugs, and dead code

There are four methods in SaveUAResults.cs spanning four Azure destination tables:

pats.tbl_LABresult:         SaveLABResults
pats.tbl_UAresults:         SaveUAResults
pats.tbl_UAresultDetail:    SaveUAResultDetail  (bypassed in production — see Section 9)
pats.tbl_UASched:           SaveUASched
________________________________________

2. High-Level Business Summary

What is UA (Urinalysis) data?

Urinalysis is a core clinical monitoring tool in Medication-Assisted Treatment (MAT) programs.
Patients are scheduled for regular urine drug screenings as a condition of their treatment.
The UA pipeline covers three types of records:

pats.tbl_UAresults — UA Result Header
This table captures the header record for each UA test event. It records the patient link
(UarLngCltId), the test schedule link (UarSchedId), the result and drop date and times, the
created and updated audit fields, a counselor note (UaNote), location, the scheduled date,
nurse note, signature information (UaSig/UaSigDt/UaSigUser), the UA type, program code,
a database-level note (UaDbnotes), the lab reference key, a flag for EtG testing, an
evaluation narrative (UAEval), and a Base64-encoded result document (UaBase64). A PHC-specific
SiteId override (105) is applied for the PHC site.

pats.tbl_UAresultDetail — UA Result Line Items
This table captures the per-substance line items within each UA test event. Each row
records the test record ID (UardRecId), the individual substance result (UardResult),
whether it was a prescribed substance (UardRx bool), the detail narrative (UaDetail),
full notes (UardFullNote), a laboratory key (UardKey), and a substance note (UardNote).
The last three fields are conditionally mapped only if the column exists in the source
DataTable — a defensive pattern unique to this method.

pats.tbl_UASched — UA Schedule Records
This table mirrors the UA scheduling records from SAMMS. It captures the scheduled
collection date and time (UasDt, UasDtAdded), collection details (UasCollectedBy,
UasCollectedDate, UasManifestDate), the UA panel type and panel, priority, status
(UasStat, UasStatDt, UasStatUser), EtG flag (UasEtg as bool from 0/1 int), the ticket
print date, and old migration identifiers. RowState is derived directly from the source
patient ID: if UasLngCltId < 0 (a legacy/null patient placeholder), RowState is set to
false (inactive). A table-existence pre-check runs before this method is called.

What is Lab (LAB) data?

pats.tbl_LABresult — External Laboratory Results
This table captures results from external laboratory tests ordered through SAMMS — distinct
from in-clinic urine screens. It records the lab result date (LabrResultDt), drop date
(LabrDropDt), creation and update audit fields, lot number, order ID, a supplementary
report text, a Base64-encoded PDF of the lab result document (LabBase64), the lab vendor
name (LabName), and a counselor note (Labnote). PHC gets a hardcoded SiteId of 105.

Why this data is important
- UA result data is the primary evidence record for patient compliance with treatment
  conditions — missing or incorrect results directly affect treatment decisions
- UA schedule data drives the scheduling module and compliance reporting
- UA result detail provides the per-substance breakdown required for clinical documentation
  and regulatory reporting
- Lab results support physician-ordered tests (bloodwork, toxicology panels) for full
  clinical documentation

Load type
SaveLABResults and SaveUAResults use a pre-load/RowChkSum-guarded upsert pattern with
optional reInit/reload flags that control the scope of the Azure pre-load. SaveUAResultDetail
was originally an EF Core method but is bypassed in production — both its task routes call
BulkDartsSrvLoader instead. SaveUASched uses a standard EF Core two-phase upsert with a
table-existence pre-check, RowChkSum stored, and RowState derived from the patient ID.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=5                     Main ETL orchestrator for Samms-LAB (UA / Lab)
BHGTaskRunner.exe arg=3                     Catch-all schedule — also processes UA/Lab tasks
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)              Column list + RowChkSum expression for SELECT build
dms.tbl_MapAction (Azure DB)               Maps TaskName to source table/view per task
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
BulkDartsSvc.cs / BulkDartsSrvLoader       Used in production for UAResultDetail and LabResultDetail
SaveUAResults.cs (BHG-DR-LIB)             4 methods for UA / Lab data (SaveUAResultDetail bypassed)
Models/TblLabresult.cs                      EF entity → pats.tbl_LABresult
Models/TblUaresults.cs                      EF entity → pats.tbl_UAresults
Models/TblUaresultDetail.cs                 EF entity → pats.tbl_UAresultDetail (bypassed)
Models/TblUasched.cs                        EF entity → pats.tbl_UASched
pats.tbl_LABresult (Azure BHG_DR)          Final destination for external lab results
pats.tbl_UAresults (Azure BHG_DR)          Final destination for UA result headers
pats.tbl_UAresultDetail (Azure BHG_DR)     Final destination for UA result line items (bulk path)
pats.tbl_UASched (Azure BHG_DR)            Final destination for UA schedule records
tsk.tbl_RowTrax (Azure DB)                 Audit log — source vs destination row counts
________________________________________

4. Scheduler — How UA / Lab Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task for Samms-LAB schedule
    TaskName = 'Samms-LAB'
    SiteCode = 'All'
    Status   = 17

Step 3 — Insert child tasks per clinic
For each active clinic, child tasks are inserted:
    TaskName = 'pats.tbl_labresult'        SiteCode = 'B01A'
    TaskName = 'pats.tbl_uaresults'        SiteCode = 'B01A'
    TaskName = 'pats.tbl_uaresultdetail'   SiteCode = 'B01A'
    TaskName = 'pats.tbl_labresultdetail'  SiteCode = 'B01A'
    TaskName = 'pats.tbl_uasched'          SiteCode = 'B01A'
    ... (one row per table per clinic)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How UA / Lab Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 5 for the dedicated lab schedule:
    BHGTaskRunner.exe 5

Step 1 — Filter queue by Samms-LAB task name
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"
        && x.Status == 17
        && x.TaskName == "Samms-LAB"
        && x.RunAt < DateTime.Now).ToList()

Step 2 — Mark parent task as running (Status=18)

Step 3 — Load and order child tasks, loop one per clinic

Step 4 — Build base SELECT
    DaysBack = -15
    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
    strWhere = st.WhereCondition
                 .Replace("@SiteCode", "'" + st.SiteCode + "'")
                 .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")

Step 5 — Dispatch by TaskName

CASE: pats.tbl_labresult
    if (st.SiteCode.ToUpper() != "LAB")
    {
        strCmd += " Where " + strWhere + " " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = sd.SaveLABResults(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null)
        → reInit always passed as true — full site pre-load path always used
        → "LAB" site code skipped (returns IsResult=true with no data movement)
    }

CASE: pats.tbl_uaresults
    reload = st.Reload.HasValue && st.Reload.Value   (from task metadata)
    if (reload)
    {
        strCmd += " Where uarresultdt is not null " + st.SortOrder
        → reload uses broader WHERE — all records with a result date
    }
    else
    {
        strCmd += " Where " + strWhere + " " + st.SortOrder
        → normal daily run uses standard rolling lookback
    }
    if (st.SiteCode.ToLower() == "lab")
    {
        strCmd = strCmd.Replace(", [LabName] LabName", "").Replace(", [LabName]", "")
        strCmd = strCmd.Replace(", [UAEval] UAEval", "").Replace(", [UAEval]", "")
        → "lab" site strips LabName and UAEval columns from SELECT
    }
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveUAResults(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), reload, null)

CASE: pats.tbl_uaresultdetail
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_uaresultdetail", st.SiteCode, st.WorkDate.Value.Date, null)
    → SaveUAResultDetail(EF Core) IS COMMENTED OUT — BulkDartsSrvLoader used instead

CASE: pats.tbl_labresultdetail
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_labresultdetail", st.SiteCode, st.WorkDate.Value.Date, null)
    → SaveUAResultDetail(EF Core) IS COMMENTED OUT — BulkDartsSrvLoader used instead

CASE: pats.tbl_uasched
    TABLE EXISTENCE PRE-CHECK:
    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblUASched'", st.ConStr)
    if (SrcDt.Rows.Count == 1)
    {
        strCmd = strCmd.Replace("Select ", "Select distinct ")    ← forced DISTINCT
        strCmd += " Where " + strWhere + " " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = sd.SaveUASched(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)
    }
    else { rCodes.ExceptMsg = "Table does not exists." }

Step 6 — RowTrax audit (if st.RowTrax = true and SiteCode != "PHC")

Step 7 — Mark child task Status=20 (complete)

Schedule 3 note:
BHGTaskRunner.exe 3 is the catch-all schedule. Its task filter includes ALL tasks that are
not P1, P2, or SAMMSGlobal — this includes Samms-LAB child tasks. So if these task names
appear in the queue when arg=3 is run, they will also be processed by the same inner switch.
In practice, the dedicated arg=5 run handles the Samms-LAB parent and its children first.
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the full SELECT, and returns a DataTable. Connection strings are from
ctrl.tbl_LocationCons. The source table or view name (st.FromTblVw) comes from
dms.tbl_MapAction per task. Typical SAMMS source names:
    dbo.tblLABResult (or clinic view)     → pats.tbl_LABresult
    dbo.tblUAResult  (or clinic view)     → pats.tbl_UAresults
    dbo.tblUAResultDetail                 → pats.tbl_UAresultDetail (bulk path)
    dbo.tblUASched                        → pats.tbl_UASched

For pats.tbl_uasched, BHGTaskRunner fires an extra sys.tables probe before the main SELECT
to confirm the source table exists in the clinic's SAMMS database.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

7a. dbo.tblLABResult — External Lab Result Records

Key columns:
    LABrID           int        Unique lab result ID (primary key)
    lablngCltId      int        Patient/client ID
    RowChkSum        int        CHECKSUM() computed over key result fields
    LABrresultdt     datetime   Result date and time
    LABrDropDt       datetime   Drop/collection date
    LABrCreatedDt    datetime   Record creation date
    LABrCreatedBy    varchar    Created by user
    LABrUpdatedBy    varchar    Last updated by user
    LABrUpdatedDt    datetime   Last updated date
    LabNote          varchar    Counselor note
    RepOldLab        int        Replacement for old lab record ID
    OldClient        varchar    Old client identifier (migration)
    LABrLotno        varchar    Lot number of test kit
    LabOrderId       varchar    External lab order ID
    SupplementaryReport varchar  Supplementary text report
    SiteId           int        Site numeric ID (PHC hardcoded to 105)
    LabBase64        varchar    Base64-encoded PDF of lab result
    LabName          varchar    Lab vendor name
    LABrSchedId      int        Linked schedule record ID

7b. dbo.tblUAResult — UA Result Header Records

Key columns:
    uarID            int        Unique UA result ID (primary key)
    uarLngCltID      int        Patient/client ID (composite key component)
    RowChkSum        int        CHECKSUM() computed over key UA fields
    uarresultdt      datetime   Result date
    uarDropDt        datetime   Drop/collection date
    uarCreatedDt     datetime   Record creation date
    uarCreatedBy     varchar    Created by user
    uarUpdatedBy     varchar    Last updated by user
    uarUpdatedDt     datetime   Last updated date
    uarSchedID       int        Linked UA schedule record ID
    cpID             int        Counselor/provider ID
    uaNOTE           varchar    Counselor note
    oldnum           int        Old record number (migration)
    OldClient        varchar    Old client identifier (migration)
    repOldUAr        decimal    Replacement for old UA record
    uarLabKey        varchar    Lab reference key
    uaType           varchar    UA type code
    SiteID           int        Site numeric ID (PHC hardcoded to 105)
    uaDBnotes        varchar    Database-level notes
    uaNurseNote      varchar    Nurse notes
    uaSig            varchar    Signature text
    uaSigDt          datetime   Signature date
    uaSigUser        varchar    Signing user
    location_        varchar    Location field (note underscore suffix in source)
    scheduledDate    datetime   Scheduled test date
    uaBase64         varchar    Base64-encoded UA result document
    UAProgram        varchar    Program code
    LabName          varchar    Lab vendor name (stripped for "lab" site code)
    uaeval           varchar    Counselor evaluation narrative (stripped for "lab" site code)

7c. dbo.tblUAResultDetail — UA Result Line Items

Key columns:
    uardid       int        Unique detail record ID (primary key)
    uardRecId    int        Parent UA result record ID
    RowChkSum    int        CHECKSUM()
    uardResult   varchar    Substance test result value
    uardRX       bit        Prescribed substance flag (bool)
    uaDetail     varchar    Detail narrative
    uardFullNote varchar    Full note (conditionally present — column existence checked)
    uardkey      varchar    Lab key (conditionally present — column existence checked;
                            note: source column "usardkey" contains typo — "usar" not "uard")
    uardNote     varchar    Substance note (conditionally present — column existence checked)

7d. dbo.tblUASched — UA Schedule Records

Key columns:
    uasID              int        Unique schedule record ID
    uasLngCltId        int        Patient/client ID (negative = legacy/null patient → RowState=false)
    RowChkSum          int        CHECKSUM()
    uasDt              datetime   Scheduled collection date
    uasDtAdded         datetime   Date schedule record was created
    uasStat            varchar    Status code
    uasStatDt          datetime   Status date
    uasStatUser        varchar    User who set status
    lngCpano           int        Provider reference
    uasNote            varchar    Schedule note
    oldNum             varchar    Old number (migration)
    OldClient          varchar    Old client (migration)
    repOldUas          decimal    Replacement for old UAS record
    uasCollectedBy     varchar    User who collected specimen
    uasCollectedDate   datetime   Actual collection date
    uasManifestDate    datetime   Lab manifest date
    uasPanel           varchar    Test panel name
    uasPanelOther      varchar    Free-text panel description
    uasType            varchar    Schedule type
    uasEtg             int/bool   EtG test flag (source int 0/1 → Azure bool)
    uapriority         varchar    Priority code
    uasticketprintdate datetime   Ticket print date
________________________________________

8. SaveLABResults — External Lab Results Load (pats.tbl_LABresult)

Source: dbo.tblLABResult (or clinic-specific view)
Destination: pats.tbl_LABresult
Composite key: SiteCode + LabrId + LablngCltId (for lookup); SiteCode + LabrId (for new row creation)
Parameters: tbl, sc, wrkdt, reInit (bool), db

The reInit flag controls the scope of the Azure pre-load:
    if (reInit = true)  → v = db.TblLabresult.Where(x => x.SiteCode == sc).ToList()
                          Full site slice — all records for this clinic
    if (reInit = false) → v = db.TblLabresult.Where(x => x.SiteCode == sc
                              && (x.LabrResultDt.Value.Date >= wrkdt.Date
                                  || x.LabrDropDt >= wrkdt.Date
                                  || x.LabrCreatedDt >= wrkdt.Date)).ToList()
                          Rolling window — only records touched since wrkdt across 3 date columns

Important: BHGTaskRunner always passes reInit=true (hardcoded). The rolling date-window
path (reInit=false) exists in code but is never triggered from production. It is effectively
dead code in the current dispatch configuration.

AllNewRows flag: If the pre-loaded list v is empty (v.Count == 0), AllNewRows is set to
true. This bypasses the lookup step and goes straight to new row creation for every incoming
row — an optimisation for first-load scenarios.

RowChkSum guard: The column loop and field mapping only execute if:
    NewRow == true  OR  rcs != vr.RowChkSum
This is the standard change-detection gate. If the row exists and the checksum matches,
no field-level update occurs (only LastModAt is updated on the existing entity in the
non-AllNewRows path for existing records — but even then, LastModAt only gets updated on
the RowChkSum mismatch path).

PHC site handling:
    case "siteid":
        if (vr.SiteCode == "PHC") { vr.SiteId = 105; }
        else { parse from source }
This hardcodes SiteId=105 for the PHC site regardless of the source value.

Commit sequence:
    db.TblLabresult.UpdateRange(v)   ← marks all pre-loaded entities as modified
    db.SaveChanges()
    if (vsNew.Count > 0)
    {
        db.TblLabresult.AddRange(vsNew)
        db.SaveChanges()
    }

Note: UpdateRange(v) is called on the full pre-loaded list, not just records that were
actually modified. EF Core will generate UPDATE statements for all tracked entities whose
property values differ from the database snapshot. This is broader than necessary but
correct in result.

Column mapping (SaveLABResults — 17 fields mapped via switch):

    Source column          EF property           Type / notes
    ---------              -----------           -----
    (key, pre-read)        LabrId                int — read from r["LABrID"] before column loop
    (key, pre-read)        LablngCltId           int — read from r["lablngCltId"] before loop
    (key, pre-read)        RowChkSum             int — read from r["RowChkSum"] before loop
    (key, pre-read)        LabrResultDt          DateTime — read from r["LABrresultdt"] before loop
    labrresultdt           LabrResultDt          DateTime — re-parsed in loop with length > 6 guard
    labrlngcltid / lablngcltid  (COMMENTED OUT) — LablngCltId never mapped in the column loop
    labrschedid            LabrSchedId           int?
    labrdropdt             LabrDropDt            DateTime? — length > 6 guard
    labresultdt            LabrResultDt          DateTime — length > 0 guard (WEAK — different from labrresultdt)
    labrcreatedby          LabrCreatedBy         string
    labrcreateddt          LabrCreatedDt         DateTime? — length > 6 guard
    labnote                Labnote               string
    repoldlab              RepOldLab             int?
    oldclient              OldClient             string (uses r["oldClient"] hardcoded casing)
    labrlotno              LabrLotno             string
    laborderid             LabOrderId            string
    labrupdateby           LabrUpdatedBy         string
    labrupdatedt           LabrUpdatedDt         DateTime? — length > 6 guard
    supplementaryreport    SupplementaryReport   string
    siteid                 SiteId                int? — PHC override to 105
    labbase64              LabBase64             string
    labname                LabName               string
________________________________________

9. SaveUAResults — UA Result Header Load (pats.tbl_UAresults)

Source: dbo.tblUAResult (or clinic-specific view)
Destination: pats.tbl_UAresults
Composite key: SiteCode + UarId + UarLngCltId
Parameters: tbl, sc, wrkdt, reInit (bool, called "reload" in BHGTaskRunner), db

The reInit flag controls the Azure pre-load scope:
    if (reInit = true)  → v = db.TblUaresults.Where(x => x.SiteCode == sc).ToList()
                          Full site slice
    if (reInit = false) → v = db.TblUaresults.Where(x => x.SiteCode == sc
                              && (x.UarResultDt.Date >= wrkdt.Date.AddMonths(-3)
                                  || x.UarDropDt >= wrkdt.Date.AddMonths(-3)
                                  || x.UarCreatedDt >= wrkdt.Date.AddMonths(-3)
                                  || x.LastModAt >= wrkdt.AddMonths(-3).Date)).ToList()
                          3-month rolling window across 4 date columns (including LastModAt)

The "reload" flag in BHGTaskRunner (st.Reload from task metadata) maps directly to reInit.
Unlike SaveLABResults where reInit is hardcoded to true, here it comes from the task's
Reload configuration field — so the rolling window path CAN be used in production for
non-reload runs.

RowChkSum behaviour — CRITICAL ANOMALY:
On the update path (existing record found), before entering the column loop, the code
explicitly sets:
    vr.RowChkSum = 0
Then the guard condition checks:
    if (NewRow || rcs != vr.RowChkSum)  ← rcs (from source) is always != 0, so this is ALWAYS true
This means every found row is treated as if its checksum changed, and all fields are
always overwritten — the RowChkSum guard is fully defeated for update rows. New rows
do not have this issue since they go through the NewRow=true path.

PHC site handling: Same pattern as SaveLABResults — SiteId hardcoded to 105 for PHC.

"lab" site code handling: BHGTaskRunner strips LabName and UAEval from the source SELECT
for the "lab" site code, so those columns will not appear in the DataTable. The switch
cases for those fields are never hit for that site.

Commit sequence:
    db.TblUaresults.UpdateRange(v)
    db.SaveChanges()
    if (vsNew.Count > 0) { db.TblUaresults.AddRange(vsNew); db.SaveChanges(); }

Column mapping (SaveUAResults — 26 fields mapped via switch):

    Source column        EF property        Type / notes
    ---------            -----------        -----
    (pre-read)           UarId              int — r["uarID"]
    (pre-read)           RowChkSum          int — r["RowChkSum"]
    (pre-read)           UarLngCltId        int — r["uarLngCltID"]
    (pre-read)           UarResultDt        DateTime — r["uarresultdt"]
    uarresultdt          UarResultDt        DateTime — re-parsed, length > 6 guard
    uarlngcltid          UarLngCltId        int — uses r["uarLngCltID"] hardcoded; default 0 if empty
    uarschedid           UarSchedId         int? — uses r["uarSchedID"]
    uardropdt            UarDropDt          DateTime? — uses r["uarDropDt"], length > 6
    createdby            UarCreatedBy       string — uses r["uarCreatedBy"] (case mismatch: column "createdby" → r["uarCreatedBy"])
    uarcreateddt         UarCreatedDt       DateTime? — uses r["uarCreatedDt"], length > 6
    cpid                 CpId               int? — uses r["cpID"]
    uanote               UaNote             string — uses r["uaNOTE"]
    oldnum               Oldnum             int? — uses r["oldnum"]
    oldclient            OldClient          string — uses r["oldClient"]
    repolduar            RepOldUar          decimal? — uses r["repOldUAr"]
    uarlabkey            UarLabKey          string — uses r["uarLabKey"]
    uarupdateby          UarUpdatedBy       string — uses r["uarUpdatedBy"]
    uarupdatedt          UarUpdatedDt       DateTime? — uses r["uarUpdatedDt"], length > 6
    uatype               UaType             string — uses r["uaType"]
    siteid               SiteId             int? — PHC override 105; uses r["SiteID"]
    uadbnotes            UaDbnotes          string — uses r["uaDBnotes"]
    uanursenote          UaNurseNote        string — uses r["uaNurseNote"]
    uasig                UaSig              string — uses r["uaSig"]
    uasigdt              UaSigDt            DateTime? — uses r["uaSigDt"], length > 6
    uasiguser            UaSigUser          string — uses r["uaSigUser"]
    location_            Location           string — uses r["location_"] (source column has trailing underscore)
    scheduleddate        ScheduledDate      DateTime? — uses r["scheduledDate"], length > 6
    uabase64             UaBase64           string — uses r["uaBase64"]
    uaprogram            Uaprogram          string — uses r["UAProgram"]
    labname              LabName            string
    uaeval               UAEval             string
________________________________________

10. SaveUAResultDetail — UA Result Line Items (pats.tbl_UAresultDetail)

Source: dbo.tblUAResultDetail (or clinic-specific equivalent)
Destination: pats.tbl_UAresultDetail
Key: SiteCode + UardId

PRODUCTION STATUS: This method is NOT called in production. Both task routes that would
invoke it have the SaveUAResultDetail call commented out:

    case "pats.tbl_uaresultdetail":
        // rCodes = sd.SaveUAResultDetail(SrcDt, st.SiteCode, st.WorkDate.Value, null);   ← COMMENTED OUT
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_uaresultdetail", ...)           ← ACTIVE

    case "pats.tbl_labresultdetail":
        // rCodes = sd.SaveUAResultDetail(SrcDt, st.SiteCode, st.WorkDate.Value, null);   ← COMMENTED OUT
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_labresultdetail", ...)           ← ACTIVE

BulkDartsSrvLoader (in BulkDartsSvc.cs) uses SqlBulkCopy to push data into staging tables
(stg.tbl_uaresultdetail, stg.tbl_labresultdetail), from which stored procedures merge the
data into the final destination tables.

The EF Core method SaveUAResultDetail remains in the codebase but is bypassed. Its logic:
- AllNewRows flag: if rds.Count == 0, all incoming rows treated as new
- RowChkSum guard: column loop only runs if NewRow or rcs != rd.RowChkSum
- Conditional column existence checks: uses tbl.Columns.Contains("uardfullnote"),
  tbl.Columns.Contains("usardkey"), tbl.Columns.Contains("uardnote") before mapping
  — defensive pattern to handle clinics that may not have all columns
- Note: "usardkey" column existence check contains a typo ("usard" instead of "uard")
  but the actual field read uses r["uardkey"] correctly

Fields mapped by the EF Core method (if it were called):
    uardid        UardId       int — primary key
    uardRecId     UardRecId    int — parent result record ID
    RowChkSum     RowChkSum    int
    uardResult    UardResult   string
    uardRX        UardRx       bool? — bool.Parse
    uaDetail      UaDetail     string
    uardFullNote  UardFullNote string — conditional
    uardkey       UardKey      string — conditional (existence check has typo "usardkey")
    uardNote      UardNote     string — conditional
________________________________________

11. SaveUASched — UA Schedule Load (pats.tbl_UASched)

Source: dbo.tblUASched (or clinic-specific view)
Destination: pats.tbl_UASched
Composite key: SiteCode + UasId + UasLngCltId
Parameters: tbl, sc, wrkdt, db

SaveUASched follows the standard two-phase EF Core upsert pattern used across BHG-DR-LIB
with one unique addition: BHGTaskRunner injects `Select distinct` into the source query
before fetching data, deduplicating the result set at the SAMMS SQL Server level before
any data reaches the C# method.

Azure pre-load:
    Scheds = db.TblUasched.Where(x => x.SiteCode == sc).ToList()
    Full site slice — all existing UA schedule records for the clinic.

RowState derivation — from UasLngCltId value:
    case "uaslngcltid":
        if (length > 0) { uas.UasLngCltId = parsed value; } else { uas.UasLngCltId = -1; }
        if (uas.UasLngCltId < 0) { uas.RowState = false; } else { uas.RowState = true; }
A negative or absent patient ID marks the schedule record as inactive (RowState=false).
This is a data-driven soft-delete mechanism rather than an explicit IsDeleted flag.

UasEtg mapping — int to bool conversion:
    case "uasetg":
        if (value == "0") { uas.UasEtg = false; } else { uas.UasEtg = true; }
Any non-zero non-empty string (including "1", "2", "true") sets UasEtg=true.

LastModAt and SiteCode stamped in "sitecode" case:
    uas.SiteCode = sc;
    uas.LastModAt = DateTime.Now;

RowChkSum stored but NOT used as an update guard — every existing record is fully
overwritten on every run.

Commit sequence:
    db.SaveChanges()             ← commits all updates
    if (NewSchds.Count > 0)
    {
        db.TblUasched.AddRange(NewSchds)
        db.SaveChanges()
    }

Note: Unlike SaveLABResults and SaveUAResults, SaveUASched does NOT call UpdateRange()
on the pre-loaded list — it relies on EF Core change tracking to detect modified entities.

Column mapping (SaveUASched — 22 fields via switch):

    Source column         EF property            Type / notes
    ---------             -----------            -----
    sitecode              SiteCode + LastModAt   string + DateTime.Now — stamped in this case
    uasid                 UasId                  int
    uaslngcltid           UasLngCltId + RowState int + bool — RowState derived from sign
    uasdt                 UasDt                  DateTime? — length > 6
    uasdtadded            UasDtAdded             DateTime? — length > 6
    uasstat               UasStat                string
    uasstatdt             UasStatDt              DateTime? — length > 6
    uasstatuser           UasStatUser            string
    lngcpano              LngCpano               int?
    uasnote               UasNote                string
    oldnum                OldNum                 string
    oldclient             OldClient              string
    repolduas             RepOldUas              decimal?
    uascollectedby        UasCollectedBy         string
    uascollecteddate      UasCollectedDate       DateTime? — length > 6
    uasmanifestdate       UasManifestDate        DateTime? — length > 6
    uaspanel              UasPanel               string
    uaspanelother         UasPanelOther          string
    uastype               UasType                string
    uasetg                UasEtg                 bool? — int 0/1 converted to bool
    uapriority            Uapriority             string
    uasticketprintdate    Uasticketprintdate     DateTime? — length > 6
    rowchksum             RowChkSum              int
________________________________________

12. Change Detection — RowChkSum Behaviour

Method         RowChkSum present    Guard used    Effective behaviour
------         -----------------    ----------    -------------------
SaveLABResults Yes                  Yes           Only maps fields if NewRow OR rcs != vr.RowChkSum. Effective change detection for new rows and genuinely changed records.
SaveUAResults  Yes                  Defeated      On update, vr.RowChkSum is set to 0 before the guard check — so rcs != 0 is always true. Every found row is treated as changed regardless of actual checksum. Effective change detection is disabled for updates.
SaveUAResultDetail Yes              Yes           Bypassed in production — EF Core method not called.
SaveUASched    Yes (stored)         No            RowChkSum is stored in Azure but not compared before writing. Every existing record is always fully overwritten.
________________________________________

13. Scoping / Data Windowing

Source-side scoping (applied before GetTableData is called):
All tasks use WhereCondition from the task metadata row. DaysBack=-15 gives a 15-day
rolling lookback at source. For reload/reInit runs, the WHERE clause is broadened:
    SaveUAResults reload: "Where uarresultdt is not null" — all records with a result date
    SaveLABResults reInit=true: still uses strWhere (WhereCondition), no override in BHGTaskRunner

Azure-side scoping (controls which existing records are loaded into memory for comparison):
    SaveLABResults (reInit=true):  Full site slice — all records
    SaveLABResults (reInit=false): Rolling window — LabrResultDt/LabrDropDt/LabrCreatedDt >= wrkdt
    SaveUAResults (reload=true):   Full site slice — all records
    SaveUAResults (reload=false):  3-month rolling window across UarResultDt/UarDropDt/UarCreatedDt/LastModAt
    SaveUAResultDetail:            Full site slice — all records (bypassed in production)
    SaveUASched:                   Full site slice — all records

The 3-month rolling window in SaveUAResults (reload=false) is the widest rolling window
in the BHG-DR-LIB codebase. This is intentional — UA results may have delayed processing
and late-arriving updates that arrive well after the original result date.
________________________________________

14. Error Handling

SaveLABResults and SaveUAResults:
    res.IsResult = false
    res.ExceptMsg = e.Message
    Console.WriteLine(e.Message)
    if (e.InnerException != null)
    {
        res.ExceptMsg += "    " + e.InnerException.Message   ← appended to ExceptMsg (not ExceptInnerMsg)
        Console.WriteLine(e.InnerException.Message)
    }
Note: InnerException message is concatenated into ExceptMsg rather than stored separately
in ExceptInnerMsg — this differs from the pattern in most other BHG-DR-LIB methods.

SaveUAResultDetail:
    res.IsResult = false
    res.ExceptMsg = e.Message
    if (e.InnerException != null) { res.ExceptInnerMsg = e.InnerException.Message; }
    (InnerException stored in ExceptInnerMsg — correct pattern)

SaveUASched:
    res.IsResult = false
    res.ExceptMsg = e.Message
    Console.WriteLine(e.Message)
    Inner exception block is COMMENTED OUT:
    // if (e.InnerException != null) { Console.WriteLine(e.InnerException.ToString()); }
    Inner exception information is NEVER captured or logged for SaveUASched failures.
________________________________________

15. Anomalies, Bugs, and Known Defects

ANOMALY 1 — SaveLABResults: reInit=true is hardcoded in BHGTaskRunner — rolling window path is dead.

File: BHGTaskRunner/Program.cs, line ~1414
    rCodes = sd.SaveLABResults(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null)

The fourth argument is always `true`. The rolling date-window code path inside SaveLABResults
(reInit=false) cannot be reached from any production dispatch. The reInit parameter and the
rolling window logic serve no practical purpose in the current codebase. If a date-scoped
pre-load was intended for performance, it requires a BHGTaskRunner code change to pass
`false` (or derive it from st.Reload).

ANOMALY 2 — SaveUAResults: RowChkSum guard defeated for all update rows.

File: SaveUAResults.cs, line ~258
    else { res.RowsUpd += 1; vr.LastModAt = RunDT; vr.RowChkSum = 0; NewRow = false; }

Setting `vr.RowChkSum = 0` on the found record before the guard `if (NewRow || rcs != vr.RowChkSum)`
means `rcs` (always a positive CHECKSUM value from SAMMS) will always be != 0, so the guard
always passes for existing rows. All 26 field mappings execute for every existing row on every
run, regardless of whether any data actually changed. This increases UPDATE traffic unnecessarily
and also means RowChkSum in Azure does not reflect the actual last-changed checksum — it
reflects the most recent source value (always overwritten).

ANOMALY 3 — SaveLABResults: Dual case labels for result date with different guards.

File: SaveUAResults.cs, lines ~89–112
    case "labrresultdt":  if (length > 6) { ... }   ← standard guard
    case "labresultdt":   if (length > 0) { ... }   ← weak guard (note: missing 'r' in 'labr')

Two separate case labels map to LabrResultDt. "labresultdt" (without the 'r') uses a weak
length > 0 guard, risking DateTime.Parse exceptions on short malformed strings. If a source
DataTable column named "labresultdt" (without 'r') exists, it overwrites the value written
by "labrresultdt" with a potentially unsafe parse.

ANOMALY 4 — SaveLABResults: LablngCltId is never mapped inside the column loop.

File: SaveUAResults.cs, lines ~95–100
    case "labrlngcltid":
    case "lablngcltid":
        // if (r[c.ColumnName].ToString().Length > 0)
        // { vr.LablngCltId = int.Parse(r[c.ColumnName].ToString()); }
        // else { vr.LablngCltId = 0; }
        break;

The mapping is entirely commented out. LablngCltId is set in the new-row constructor
(from the pre-read value before the loop) but is NEVER updated in the column loop for
existing rows. If the patient ID on a lab result changes (patient reassignment), Azure
will never reflect that change.

ANOMALY 5 — SaveUAResultDetail: EF Core method bypassed in production — BulkDartsSrvLoader used instead.

Both `pats.tbl_uaresultdetail` and `pats.tbl_labresultdetail` cases in BHGTaskRunner have
the SaveUAResultDetail call commented out and replaced with BulkDartsSrvLoader. The EF Core
method in SaveUAResults.cs is dead code from a production standpoint. Any fix or enhancement
to SaveUAResultDetail in this file will have no effect until the BHGTaskRunner dispatch is
updated to call it.

ANOMALY 6 — SaveUAResultDetail: "usardkey" typo in column existence check.

File: SaveUAResults.cs, line ~468
    if (tbl.Columns.Contains("usardkey"))   ← "usar" instead of "uard"
    {
        rd.UardKey = r["uardkey"].ToString()   ← correct column name for reading
    }

The existence check looks for "usardkey" (wrong) but reads from "uardkey" (correct). If
the source DataTable has a column named "uardkey" (correct), the Contains check will return
false and the field will never be mapped. Since this method is bypassed in production, the
practical impact is zero — but the bug exists.

ANOMALY 7 — SaveUASched: Inner exception handling commented out.

File: SaveUAResults.cs, lines ~690–693
    // if (e.InnerException != null)
    // {
    //     Console.WriteLine(e.InnerException.ToString());
    // }

If SaveUASched throws an exception with an InnerException (e.g. a SQL constraint violation
with detail), the inner message is silently discarded. Only the top-level exception message
is captured. This makes diagnosing certain failure types difficult.

ANOMALY 8 — SaveUASched: "Select distinct" injection may produce unexpected results.

BHGTaskRunner modifies the base SELECT before calling SaveUASched:
    strCmd = strCmd.Replace("Select ", "Select distinct ")

The DISTINCT operates over the full projected column set (including all numeric IDs). If
any two source rows differ in any column other than a projected column, they will not be
deduplicated and both will appear in the result. This is likely harmless for most data
but means the distinct behaviour depends on which columns SelectConstructor includes.

ANOMALY 9 — SaveUAResults: "createdby" case reads from r["uarCreatedBy"] not r[c.ColumnName].

File: SaveUAResults.cs, line ~288
    case "createdby":
        vr.UarCreatedBy = r["uarCreatedBy"].ToString()

The case label matches column name "createdby" (all lowercase) but reads the value using
the hardcoded key r["uarCreatedBy"] (mixed case). If the DataTable column is named
"uarCreatedBy" in the source, this works correctly. If it is named "createdby" (matching
the case label), the key read r["uarCreatedBy"] will throw a KeyNotFoundException. The
consistency of this mapping depends on the source column naming convention from SAMMS.
________________________________________

16. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: Samms-LAB (Status=17, SiteCode='All')
        |-- insert child tasks per clinic:
        |       pats.tbl_labresult           SiteCode='B01A'
        |       pats.tbl_uaresults           SiteCode='B01A'
        |       pats.tbl_uaresultdetail      SiteCode='B01A'
        |       pats.tbl_labresultdetail     SiteCode='B01A'
        |       pats.tbl_uasched             SiteCode='B01A'
        |       ... (repeated for each active clinic)
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 5
        |
        |-- filter: TaskName='Samms-LAB', SiteCode!='PHC', Status=17
        |-- mark parent task Status=18 (running)
        |
        |-- for each child task (one per clinic per task type):
        |
        |   Build strCmd via SelectConstructor (DaysBack=-15)
        |   strCmd = "Select " + strFlds + " from " + SrcSchema + "." + FromTblVw
        |
        |======================================================
        |  BRANCH A: pats.tbl_labresult
        |======================================================
        |   Skip if SiteCode == "LAB" (returns IsResult=true, no data movement)
        |   strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   → sd.SaveLABResults(SrcDt, SiteCode, WorkDate.AddDays(-15), reInit=true, null)
        |       Load full site slice from pats.tbl_LABresult (reInit=true always)
        |       Loop rows → check AllNewRows or lookup by LabrId+LablngCltId
        |       RowChkSum guard: skip field loop if checksum unchanged
        |       UpdateRange(v) → SaveChanges() → AddRange(vsNew) → SaveChanges()
        |       → pats.tbl_LABresult (Azure BHG_DR)
        |
        |======================================================
        |  BRANCH B: pats.tbl_uaresults
        |======================================================
        |   reload = st.Reload (from task metadata)
        |   if reload: WHERE = "uarresultdt is not null" (full reload)
        |   else: WHERE = strWhere (rolling 15-day lookback)
        |   if SiteCode=="lab": strip [LabName] and [UAEval] from SELECT
        |   SrcDt = sm.GetTableData(...)
        |   → sd.SaveUAResults(SrcDt, SiteCode, WorkDate.AddDays(-15), reload, null)
        |       Load full site slice OR 3-month window from pats.tbl_UAresults
        |       Loop rows → set RowChkSum=0 on found records (defeats guard for updates)
        |       RowChkSum guard: always true for existing rows (due to RowChkSum=0 reset)
        |       UpdateRange(v) → SaveChanges() → AddRange(vsNew) → SaveChanges()
        |       → pats.tbl_UAresults (Azure BHG_DR)
        |
        |======================================================
        |  BRANCH C: pats.tbl_uaresultdetail  (and pats.tbl_labresultdetail)
        |======================================================
        |   strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(...)
        |   → sd.SaveUAResultDetail(...) IS COMMENTED OUT
        |   → bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_uaresultdetail", SiteCode, WorkDate, null)
        |       SqlBulkCopy into stg.tbl_uaresultdetail staging table
        |       Stored procedure MERGE into pats.tbl_UAresultDetail
        |       → pats.tbl_UAresultDetail (Azure BHG_DR) — via bulk/staging path
        |
        |======================================================
        |  BRANCH D: pats.tbl_uasched
        |======================================================
        |   sys.tables probe: "select name from sys.tables where name='tblUASched'"
        |   if absent: rCodes.ExceptMsg = "Table does not exists." — skip
        |   if present:
        |       strCmd = strCmd.Replace("Select ", "Select distinct ")
        |       strCmd += " Where " + strWhere + " " + SortOrder
        |       SrcDt = sm.GetTableData(...)
        |       → sd.SaveUASched(SrcDt, SiteCode, WorkDate.AddDays(-15), null)
        |           Load full site slice from pats.tbl_UASched
        |           Loop rows → build entity via switch
        |           RowState derived from UasLngCltId sign
        |           Lookup by SiteCode + UasId + UasLngCltId
        |           SaveChanges() → AddRange(NewSchds) → SaveChanges()
        |           → pats.tbl_UASched (Azure BHG_DR)
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != PHC)
        |
        V
BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)

        [Azure BHG_DR — final state after run]
        pats.tbl_LABresult         — external lab results updated
        pats.tbl_UAresults         — UA result headers updated (RowChkSum guard defeated)
        pats.tbl_UAresultDetail    — UA result line items updated via bulk/staging merge
        pats.tbl_UASched           — UA schedule records updated
________________________________________

17. File Reference Map

File Path                                                     Purpose
---------                                                     -------
BCAppCode/BHG-DR-LIB/SaveUAResults.cs                        All 4 UA/Lab methods (699 lines)
BCAppCode/BHGTaskRunner/Program.cs                            Schedule 5 dispatch
                                                              pats.tbl_labresult      ~line 1409
                                                              pats.tbl_uaresults      ~line 1433
                                                              pats.tbl_uaresultdetail ~line 1419
                                                              pats.tbl_labresultdetail ~line 1396
                                                              pats.tbl_uasched        ~line 1375
BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs                         BulkDartsSrvLoader — handles detail bulk path
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                    Builds SELECT column list and RowChkSum expression
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                        ADO.NET wrapper — sys.tables probe + full SELECT
BCAppCode/BHG-DR-LIB/Models/TblLabresult.cs                  EF Model → pats.tbl_LABresult
BCAppCode/BHG-DR-LIB/Models/TblUaresults.cs                  EF Model → pats.tbl_UAresults
BCAppCode/BHG-DR-LIB/Models/TblUaresultDetail.cs             EF Model → pats.tbl_UAresultDetail
BCAppCode/BHG-DR-LIB/Models/TblUasched.cs                    EF Model → pats.tbl_UASched
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                 EF DbContext — DbSet registrations
BCAppCode/Scheduler/Program.cs                                Task creation for Samms-LAB schedule
________________________________________

18. Quick Reference Summary

Method                Load path        Key                               RowChkSum guard    RowState    Schedule
------                ---------        ---                               ---------------    --------    --------
SaveLABResults        EF Core          SiteCode + LabrId + LablngCltId   Yes (effective)    No          5 / 3
SaveUAResults         EF Core          SiteCode + UarId + UarLngCltId    Defeated (=0 bug)  No          5 / 3
SaveUAResultDetail    BYPASSED         SiteCode + UardId                 Yes (but bypassed) No          5 / 3
SaveUASched           EF Core          SiteCode + UasId + UasLngCltId    Stored, not used   Yes         5 / 3

Production detail paths:
    pats.tbl_uaresultdetail  → BulkDartsSrvLoader → stg.tbl_uaresultdetail → MERGE → pats.tbl_UAresultDetail
    pats.tbl_labresultdetail → BulkDartsSrvLoader → stg.tbl_labresultdetail → MERGE → pats.tbl_UAresultDetail

Critical bugs:
1. SaveUAResults — RowChkSum set to 0 before guard check defeats change detection for all
   update rows — every existing UA result is fully overwritten on every daily run
2. SaveLABResults — reInit hardcoded to true in BHGTaskRunner — rolling date window path
   can never be reached from production dispatch
3. SaveLABResults — dual case labels "labrresultdt" (safe guard) and "labresultdt"
   (weak guard) both map to LabrResultDt — weak guard risks DateTime.Parse exceptions
4. SaveLABResults — LablngCltId column mapping commented out — patient ID changes on
   existing lab records are never captured in Azure
5. SaveUASched — inner exception handling commented out — constraint violation details lost
6. SaveUAResultDetail — "usardkey" column existence check has typo — UardKey never mapped
   even if the correct column exists (moot in production since method is bypassed)
