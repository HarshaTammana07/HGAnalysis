
Doses ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract medication dose
administration records and dose excuse records from local SAMMS SQL Server databases at
each clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What Dose data is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How the two load paths work: SaveDoses (EF Core) vs Bulk path (SqlBulkCopy + stg.DoseMerge)
- Which sites use which path and why
- How the source SQL is built dynamically for each dose table type
- What the source tables look like and their key columns
- What the destination tables look like and all their columns
- How RowState tracks soft-deleted / active dose records
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What is Dose data?

SAMMS tracks every medication dispensing event at each clinic. In a Medication-Assisted
Treatment (MAT) program, patients receive daily doses of medications such as methadone,
buprenorphine, or naltrexone. Each dispensing event is recorded as a dose record.

The Dose ETL pipeline manages two related destination tables:

1. dbo.tblDose (SAMMS)          → pats.tbl_Dose (Azure)
   One row per dose administration event. Captures which patient received which medication
   on which date, how much was dispensed, who administered it, whether it was voided,
   whether it was a take-home (bulk/prepack), the electronic signature image, and
   exception/override details.

2. dbo.tblDoseExcuse (SAMMS)    → pats.tbl_Dose_Excuse (Azure)
   One row per excused absence from dosing. When a patient misses a scheduled dose and
   the clinic formally excuses that absence, a record is created here. Captures the
   excuse date, timestamp, and which staff member processed the excuse.

Why it is important

The Dose dataset is the core daily operations record for MAT programs. It enables:
- Tracking medication dispensing volumes across all clinics in real time
- Identifying patients with missed doses or voided administrations
- Supporting compliance reporting for state regulatory bodies (dose logs, take-home counts)
- Feeding billing systems — dose records often trigger claims
- Clinical analytics on adherence, take-home eligibility, and treatment continuity
- Audit trails for controlled substance dispensing (DEA/state requirements)

Load type

Two paths exist for pats.tbl_Dose depending on site code:

EF Core path (SaveDoses) — used for: V10A, CBCO, V21, V10
  Row-by-row upsert. Loads all Azure rows for the site into memory, soft-resets RowState
  based on the date window, then upserts from the SAMMS DataTable. Passes reload flag
  to optionally hard-delete first.

Bulk path (SqlBulkCopy + stg.DoseMerge) — used for: all sites EXCEPT V10A, CBCO, V21, V10
  SqlBulkCopy into stg.tbl_dose (staging), then stored procedure stg.DoseMerge to
  MERGE into pats.tbl_Dose. For reload: hard-deletes pats.tbl_Dose for the site first,
  then full SELECT → stg.tbl_dose → DoseMerge.

pats.tbl_Dose_Excuse (all sites):
  SaveDoseExcuse — always uses EF Core row-by-row upsert. No bulk path exists for excuses.
  The commented-out bulk path line in BHGTaskRunner confirms this was considered but not
  implemented.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and their run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=10                    Main ETL orchestrator for SAMMS-ETL-Dose
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic's SAMMS SQL Server
dms.vw_MapAction (Azure DB)                 Maps destination tables to schedule TaskNames
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SaveDoses.cs (BHG-DR-LIB)                  EF Core upsert methods — SaveDoses and SaveDoseExcuse
BulkDartsSvc.cs (BHG-DR-LIB)              SqlBulkCopy + stg.DoseMerge for bulk dose sites
stg.tbl_dose (Azure)                        Staging table for bulk dose path
stg.DoseMerge (Azure stored proc)          MERGE from stg.tbl_dose into pats.tbl_Dose
stg.Dose_ExcuseMerge (Azure stored proc)   MERGE for dose excuse — referenced but not used
                                            in current code path (SaveDoseExcuse runs instead)
pats.tbl_Dose (Azure)                       Final destination for dose administration records
pats.tbl_Dose_Excuse (Azure)               Final destination for dose excuse records
tsk.tbl_RowTrax (Azure)                    Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How Dose Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily and populates the task queue. It does NOT move data —
it only creates tasks.

What the Scheduler does for Doses (SAMMS-ETL-Dose)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For Doses, there is a row with:
    Name        = 'SAMMS-ETL-Dose'
    ActionKey   = 10
    ScheduleId  = (dose schedule ID)

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'SAMMS-ETL-Dose'
    SiteCode = 'All'
    Status   = 17

Step 4 — Insert child task rows (one per clinic per table)
The Scheduler uses dms.vw_MapAction with a CASE expression that assigns TaskNames:

    when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Dose'        → 'SAMMS-ETL-Dose'
    when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Dose_Excuse' → 'SAMMS-ETL-Dose'

Both tbl_Dose and tbl_Dose_Excuse are explicitly excluded from all Regional P1/P2 and
timezone-based schedules (Eastern, Central, Mountain, Pacific). They belong exclusively
to SAMMS-ETL-Dose.

This produces child task rows for each active clinic:
    TaskName = 'pats.tbl_Dose'
    SiteCode = 'B01', 'VBRA', etc.

    TaskName = 'pats.tbl_dose_excuse'
    SiteCode = 'B01', 'VBRA', etc.

Step 5 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1

Step 6 — Clean up
    delete from tsk.tbl_Tasks2
    where RunAt <= DateAdd(m, -3, GetDate()) or RowState = 26

Task queue structure after Scheduler runs:

tsk.tbl_Tasks2 will contain:
    ParentTaskId = NULL
        TaskName = 'SAMMS-ETL-Dose'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'pats.tbl_Dose'
        SiteCode = 'B01'
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_dose_excuse'
        SiteCode = 'B01'
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How Doses Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 10 to process only the SAMMS-ETL-Dose schedule.

Command:   BHGTaskRunner.exe 10

Step 1 — Filter task queue for SAMMS-ETL-Dose
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"                // PHC uses a separate runner
        && x.Status == 17                  // ready to run
        && x.TaskName == "SAMMS-ETL-Dose"
        && x.RunAt < DateTime.Now)

Step 2 — Mark parent task as running
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)

Step 4 — For each child task, dispatch by TaskName

Step 5 — pats.tbl_dose path (case "pats.tbl_dose")

  The dose case has two completely different branches depending on SiteCode.

  Step 5a — Build the base SELECT
  The base SELECT (strCmd) is assembled from the task metadata (st.FromTblVw is the
  source view or table name, st.SrcSchema is the source schema). The WHERE clause
  is built inline depending on the branch.

  Step 5b — Branch: EF Core sites (V10A, CBCO, V21, V10)

    Normal run:
        strWhere = "(Year(dtDate) >= <WorkDate+DaysBack-1yr>.Year
                    or Year(dtMedDate) >= <WorkDate+DaysBack-1yr>.Year)
                    and dtDate <= '<WorkDate+2 days>'
                    and CltId is not null
                    and dtDate >= '<WorkDate-1 month>'"
        strCmd += " Where " + strWhere + " " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = sd.SaveDoses(SrcDt, st.SiteCode,
                     st.WorkDate.Value.AddDays(DaysBack), st.Reload.Value, null)

    Reload run (st.Reload = true):
        strCmd += " Where CltID is not null and dtMedDate is not null " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = sd.SaveDoses(SrcDt, st.SiteCode,
                     st.WorkDate.Value.AddDays(DaysBack), st.Reload.Value, null)

    Note: For these EF Core sites, the reload DELETE (if any) happens INSIDE SaveDoses
    when reload=true — NOT in BHGTaskRunner. The SaveDoses method executes:
        Delete from pats.tbl_Dose where SiteCode = '<sc>'
    before the EF processing begins.

  Step 5c — Branch: Bulk sites (all sites except V10A, CBCO, V21, V10)

    Normal run:
        strWhere = "(Year(dtDate) >= <WorkDate+DaysBack-1yr>.Year
                    or Year(dtMedDate) >= <WorkDate+DaysBack-1yr>.Year)
                    and dtDate <= '<WorkDate+2 days>'
                    and CltId is not null
                    and dtDate >= '<WorkDate-6 months>'"
        strCmd += " Where " + strWhere + " " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_dose", st.SiteCode,
                     st.WorkDate.Value.AddDays(DaysBack), null)

    Reload run (st.Reload = true):
        BHGTaskRunner executes the hard-delete directly here (before BulkDartsSrvLoader):
            delete from [pats].[tbl_dose] where SiteCode = '<sc>'
        strCmd += " Where CltID is not null and dtMedDate is not null " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_dose", st.SiteCode,
                     st.WorkDate.Value.AddDays(DaysBack), null)

  Key difference in WHERE clause between EF Core and Bulk sites:
    EF Core sites (V10A etc.): lookback = WorkDate - 1 month
    Bulk sites: lookback       = WorkDate - 6 months
  The bulk sites use a wider 6-month window because the bulk merge stored procedure is
  more efficient at handling larger volumes than the EF row-by-row path.

  Step 5d — RowTrax (if st.RowTrax = true and SiteCode != "PHC")
      Source count = count(*) from SAMMS where CltID is not null and dtMedDate is not null
      Dest count   = count(*) from pats.tbl_dose where SiteCode = sc and RowState = 1

Step 6 — pats.tbl_dose_excuse path (case "pats.tbl_dose_excuse")

  Step 6a — Build and execute SELECT
      strCmd += " Where " + strWhere + " " + st.SortOrder
      SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

  Note: The WhereCondition for dose excuse tasks in the task metadata controls strWhere.
  The dose excuse case does NOT have a bulk path. The commented-out line confirms this:
      //rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_dose_excuse", st.SiteCode, null)

  Step 6b — Call SaveDoseExcuse
      rCodes.IsResult = sd.SaveDoseExcuse(SrcDt, st.SiteCode, null)
      rCodes.RowsProcessed = SrcDt.Rows.Count

  Note: Because SaveDoseExcuse returns a plain bool (not RCodes), BHGTaskRunner manually
  assigns the results: IsResult from the return value, RowsProcessed from SrcDt.Rows.Count.

  Step 6c — RowTrax (if st.RowTrax = true and SiteCode != "PHC")
      Source count = count(*) from SAMMS where CltID > 0
      Dest count   = count(*) from pats.tbl_dose_excuse where SiteCode = sc and RowState = 1

Step 7 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'B01'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_B01;User Id=...;Password=...;'

The source view/table name (st.FromTblVw) and schema (st.SrcSchema) are stored in the
task metadata row from dms.vw_MapAction. For dose records the source is typically a
view like vwDose or the table dbo.tblDose, depending on the clinic's SAMMS version.

The DataTable returned is passed directly into SaveDoses, SaveDoseExcuse, or
BulkDartsSrvLoader.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

All source tables live in the clinic's local SAMMS SQL Server database under the dbo
schema. The exact table or view name is stored in dms.tbl_MapSrc2Dsn via st.FromTblVw.

________________________________________
7a. dbo.tblDose (or vwDose) — Dose Administration Records

Primary Key: DoseId

Column Name         Type            Description
-----------         ----            -----------
DoseId              bigint          Unique dose event ID within this clinic
CltId               int             Patient/client ID (may be negative for deletions)
dtMedDate           datetime        The medication date — the scheduled dispensing date
dtDate              datetime        The actual administration date (may differ from dtMedDate)
dtGiven             datetime        Date/time the dose was physically given (nullable)
dtPrep              datetime        Date/time the dose was prepared (nullable)
dose                int             Dose amount dispensed (in mg or units)
GuestId             int             Guest dosing record ID (nullable — for take-homes given off-site)
strUser             varchar         Username of staff who dispensed the dose
strVoidReason       varchar         Reason for voiding this dose record (if voided)
Bottletype          varchar         Type of dispensing bottle used
blVoid              bit             True if this dose record was voided
blException         bit             True if this dose record has an exception/override
blBulk              bit             True if this was dispensed as a bulk take-home
blPrepack           bit             True if this was a pre-packaged take-home dose
dtVoid              bit             Void status flag — NOTE: stored as bool despite "dt" prefix
ordernum            int             Linked prescription order number (nullable)
ExceptionReason     varchar         Reason for exception if blException = true
Exceptiontype       varchar         Type of exception (e.g. late dose, missed dose)
Manualauthuser      varchar         Username who manually authorized an override
manualauthdtm       datetime        Date/time of manual authorization (nullable)
Dosenote            varchar         Free-text note on this dose event
Dosesig             varchar         Staff signature string (text form)
dosesigimg          varbinary       Electronic signature image — stored as ASCII bytes in Azure
InventoryGroup      varchar         Inventory/medication group (not in all SAMMS schemas)
siteid              int             Clinic site ID (for multi-site SAMMS instances)
rowchksum           int             SQL CHECKSUM of the row — used for change detection
________________________________________

7b. dbo.tblDoseExcuse — Dose Excuse Records

Primary Key: ExId

Column Name     Type            Description
-----------     ----            -----------
ExId            int             Unique excuse ID within this clinic
CltId           int             Patient/client ID
DtEx            datetime        Date of the excused absence
Dtstamp         datetime        Timestamp when the excuse was recorded in the system
StrUser         varchar         Username of staff who recorded the excuse
rowchksum       int             SQL CHECKSUM of the row — used for change detection
________________________________________

8. SaveDoses — EF Core Path (Sites: V10A, CBCO, V21, V10)

File: BCAppCode/BHG-DR-LIB/SaveDoses.cs
Class: SaveData (partial class)
Method: SaveDoses()

Method signature:
    public RCodes SaveDoses(
        DataTable tbl,          // rows from SAMMS — one per dose event
        string sc,              // SiteCode e.g. "V10A"
        DateTime dtWrk,         // work date window boundary = WorkDate + DaysBack
        bool reload,            // if true: DELETE all rows for SiteCode first
        BHG_DRContext db)       // EF context (created if null)

EF Core upsert logic — step by step:

Step 1 — Hard Reload (if reload = true)
    SQLSvrManager sm = new SQLSvrManager()
    sm.ExeSqlCmd("Delete from pats.tbl_Dose where SiteCode = '" + sc + "'", sm.ConnectionString)

This wipes all existing Azure dose rows for the site before any EF processing. The
remaining steps then treat everything as brand new rows (AllNewRows = true effectively).

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load all existing Azure rows for this site
    List<TblDose> doses = db.TblDose.Where(x => x.SiteCode == sc).ToList()

All rows currently in pats.tbl_Dose for this SiteCode are loaded into memory. The date
filter commented out in the source (DtDate.Year >= dtWrk.Year) was never activated.

Step 4 — Set AllNewRows flag
    if (doses.Count == 0) { AllNewRows = true; }

Step 5 — Pre-pass: soft-reset in-window rows
    foreach (TblDose d in doses)
        if (d.DtDate >= dtWrk.Date) { d.RowState = false; }

Only rows whose DtDate falls on or after the work date boundary are soft-reset. Historical
doses with DtDate older than the lookback window are left untouched. No SaveChanges is
called here — resets are deferred to the end.

Step 6 — Loop through every row in the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 7 — Parse key fields (always)
    long intDoseId  = long.Parse(r["DoseId"].ToString())
    int intcltid    = int.Parse(r["cltid"].ToString())
    int rcs         = int.Parse(r["rowchksum"].ToString())
    DateTime meddt  = DateTime.Parse(r["dtmeddate"].ToString())

These four fields are parsed before any branch logic. dtMedDate is always set at
object construction — there is no conditional guard for it.

Step 8 — Construct or locate the dose object

    If AllNewRows == true:
        dose = new TblDose {
            SiteCode = sc,
            DoseId = intDoseId,
            CltId = intcltid,
            RowChkSum = 0,         // intentionally 0 so checksum gate always fires
            DtMedDate = meddt
        }
        res.RowsIns += 1
        NewRow = true

    Else:
        dose = doses.Where(x => x.DoseId == intDoseId).FirstOrDefault()
        if dose == null:
            dose = new TblDose {
                SiteCode = sc,
                DoseId = intDoseId,
                CltId = intcltid,
                RowChkSum = 0,
                RowState = true,
                DtMedDate = meddt
            }
            NewRow = true
            res.RowsIns += 1
        else:
            res.RowsUpd += 1

Step 9 — Full field mapping (if NewRow OR checksum changed: rcs != dose.RowChkSum)

    dose.RowState    = true
    dose.LastModAt   = RunDT
    dose.RowChkSum   = rcs

Column mapping table:

    Source Column       Destination Field       Guard / Transformation
    dtdate              DtDate                  DateTime — only if length > 6
    guestid             GuestId                 int — only if length > 0
    dose                Dose                    int — only if length > 0
    struser             StrUser                 Always
    strvoidreason       StrVoidReason           Always
    bottletype          Bottletype              Always
    Blvoid              BlVoid                  bool — only if length > 0
    blexception         BlException             bool — only if length > 0
    ordernum            Ordernum                int — only if length > 0
    exceptionreason     ExceptionReason         Always
    blbulk              BlBulk                  bool — only if length > 0
    blprepack           BlPrepack               bool — only if length > 0
    dtvoid              DtVoid                  bool — only if length > 0
                                                NOTE: despite the "dt" prefix, this is
                                                a bool flag (void status), not a DateTime
    dtgiven             Dtgiven                 DateTime — only if length > 6
    dtprep              Dtprep                  DateTime — only if length > 0
                                                NOTE: uses length > 0 (not > 6) — unlike
                                                all other DateTime fields in this method
    ppstaff             Ppstaff                 Always
    exceptiontype       Exceptiontype           Always
    Manualauthuser      Manualauthuser          Always
    manualauthdtm       Manualauthdtm           DateTime — only if length > 6
    dosenote            Dosenote                Always
    dosesig             Dosesig                 Always
    inventorygroup      InventoryGroup          Only if column exists in source DataTable:
                                                tbl.Columns.Contains("InventoryGroup")
    siteid              SiteId                  int — only if length > 0
                                                EXCEPTION: if SiteCode == "PHC" → hardcoded 105
    dosesigimg          DoseSigImg              byte[] — always:
                                                System.Text.Encoding.ASCII.GetBytes(r["dosesigimg"])

    Void check (applied after field mapping):
        if (dose.BlVoid == true && dose.DtVoid == true) { dose.RowState = false; }

Step 10 — Checksum unchanged path (existing rows only)

    dose.RowState = true
    if (dose.BlVoid == true && dose.DtVoid == true)    { dose.RowState = false; }
    if ((dose.CltId < 0) && (dose.CltId != -111))      { dose.RowState = false; }

Note on CltId = -111:
A negative CltId normally signals that a patient record was soft-deleted in SAMMS.
The value -111 is a known sentinel representing a special administrative record.
It is explicitly exempt — it remains RowState = true even though CltId is negative.
All other negative CltId values result in RowState = false.

Step 11 — Queue new rows
    if (NewRow || AllNewRows):
        NewRow = false
        if (dose.BlVoid == true && dose.DtVoid == true) { dose.RowState = false; }
        if ((dose.CltId < 0) && (dose.CltId != -111))  { dose.RowState = false; }
        newdoses.Add(dose)

Step 12 — Commit in two batches
    db.SaveChanges()                // flushes pre-pass resets + field updates for existing rows
    if (newdoses.Count > 0):
        db.TblDose.AddRange(newdoses)
        db.SaveChanges()            // batch insert of all new rows
________________________________________

9. Bulk Path — All Other Sites (BulkDartsSrvLoader + stg.DoseMerge)

File: BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs
Method: BulkDartsSrvLoader()

For all sites except V10A, CBCO, V21, V10, the dose data takes the bulk path.

Step 1 — TRUNCATE the staging table
    sm.ExeSqlCmd("Truncate Table stg.tbl_dose", sm.ConnectionString)

The staging table is always truncated before loading new data. This is a full replace
of the staging area — no incremental logic in staging.

Step 2 — Map columns for bulk copy
    foreach (DataColumn c in tbl.Columns)
        bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName))
    bc.DestinationTableName = "stg.tbl_dose"
    bc.BulkCopyTimeout = 99999

Step 3 — SqlBulkCopy.WriteToServer(tbl)
    All rows from SAMMS inserted into stg.tbl_dose in a single operation.

Step 4 — Execute merge stored procedure
    sm.ExeSqlCmd("exec stg.DoseMerge '" + sitecode + "'", sm.ConnectionString)

stg.DoseMerge is a site-specific MERGE:
    MERGE pats.tbl_Dose AS tgt
    USING stg.tbl_dose AS src ON (DoseId = src.DoseId AND tgt.SiteCode = sitecode)
    WHEN MATCHED AND src.rowchksum <> tgt.RowChkSum THEN UPDATE SET ...
    WHEN NOT MATCHED BY TARGET THEN INSERT ...
    (rows in target but not in source within the date window get RowState = 0 via
    a separate UPDATE step inside the stored procedure)

Note: Unlike SaveDoses (EF Core path) where every committed match fires RowState logic
in C#, the bulk path delegates all matching, change detection, and RowState management
to the SQL Server stored procedure stg.DoseMerge.

Reload via bulk path:
BHGTaskRunner executes the hard-delete before calling BulkDartsSrvLoader:
    delete from [pats].[tbl_dose] where SiteCode = '<sc>'
The SELECT then uses WHERE CltID is not null and dtMedDate is not null (full history).
BulkDartsSrvLoader proceeds as normal: TRUNCATE → BulkCopy → DoseMerge.
________________________________________

10. SaveDoseExcuse — EF Core Upsert (All Sites)

File: BCAppCode/BHG-DR-LIB/SaveDoses.cs
Class: SaveData (partial class)
Method: SaveDoseExcuse()

Method signature:
    public bool SaveDoseExcuse(
        DataTable tbl,          // rows from SAMMS — one per excuse record
        string sc,              // SiteCode
        BHG_DRContext db)       // EF context (created if null)

Returns: bool — true = success, false = exception caught (written to console)

EF Core upsert logic — step by step:

Step 1 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load all existing Azure rows for this site
    List<TblDoseExcuse> doses = db.TblDoseExcuse.Where(x => x.SiteCode == sc).ToList()

Step 3 — Set AllNewRows flag
    if (doses.Count == 0) { AllNewRows = true; }

Step 4 — Pre-pass: full reset (ALL rows for site)
    foreach (TblDoseExcuse d in doses)
        d.RowState = false

Unlike SaveDoses, there is no date window filter here. Every existing excuse row for the
clinic is soft-reset regardless of age. This means the next fetch must return ALL active
excuse records — any not returned will remain RowState = false (soft-deleted).

Step 5 — Parse key fields
    int intExId    = int.Parse(r["ExId"].ToString())
    int intcltid   = int.Parse(r["cltid"].ToString())
    int rcs        = int.Parse(r["rowchksum"].ToString())

Step 6 — Construct or locate the excuse object

    If AllNewRows == true:
        dose = new TblDoseExcuse {
            SiteCode = sc,
            ExId = intExId,
            CltId = intcltid,
            RowChkSum = rcs      // stored at actual source value (not 0 like SaveDoses)
        }
        NewRow = true

    Else:
        dose = doses.Where(x => x.ExId == intExId).FirstOrDefault()
        if dose == null:
            dose = new TblDoseExcuse {
                SiteCode = sc,
                ExId = intExId,
                CltId = intcltid,
                RowChkSum = rcs
            }
            NewRow = true

Step 7 — Full field mapping (if NewRow OR rcs != dose.RowChkSum)

    dose.RowState  = true
    dose.LastModAt = RunDT
    dose.CltId     = intcltid   // explicitly re-assigned — even on updates
    if DtEx length > 6:
        dose.DtEx = DateTime.Parse(r["DtEx"].ToString())
    if Dtstamp length > 6:
        dose.Dtstamp = DateTime.Parse(r["Dtstamp"].ToString())
    dose.StrUser = r["StrUser"].ToString()

Step 8 — Checksum unchanged path (existing rows only)
    dose.RowState  = true
    dose.LastModAt = RunDT

Step 9 — Queue new row
    if (NewRow || AllNewRows):
        NewRow = false
        db.TblDoseExcuse.Add(dose)   // inline add — NOT batched into a list

Step 10 — Single commit
    db.SaveChanges()    // commits everything: resets + updates + inserts in one call

No void or negative-CltId checks exist in SaveDoseExcuse. Excuse records are never
soft-deleted based on field values — only the pre-pass reset mechanism can mark them
inactive (RowState = false) if they do not reappear in the current SAMMS data.
________________________________________

11. Destination Tables — Azure BHG_DR (pats schema)

________________________________________
11a. pats.tbl_Dose

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_Dose
EF Model: BHG-DR-LIB/Models/TblDose.cs

Primary Key: SiteCode + DoseId (composite)

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar             Clinic identifier
DoseId              DoseId              bigint              Unique dose event ID from source
CltId               CltId               int                 Patient/client ID
DtMedDate           DtMedDate           datetime            Scheduled medication date (always populated)
DtDate              DtDate              datetime (nullable) Actual administration date
Dtgiven             Dtgiven             datetime (nullable) Physical dispensing timestamp
Dtprep              Dtprep              datetime (nullable) Preparation timestamp
Dose                Dose                int (nullable)      Dose amount in mg or units
GuestId             GuestId             int (nullable)      Guest dosing record ID
StrUser             StrUser             varchar             Staff who dispensed
StrVoidReason       StrVoidReason       varchar             Void reason if applicable
Bottletype          Bottletype          varchar             Dispensing bottle type
BlVoid              BlVoid              bit (nullable)      True = this dose was voided
BlException         BlException         bit (nullable)      True = exception/override on this dose
BlBulk              BlBulk              bit (nullable)      True = bulk take-home dispensing
BlPrepack           BlPrepack           bit (nullable)      True = pre-packaged take-home
DtVoid              DtVoid              bit (nullable)      Void status flag (bool despite "dt" name)
Ordernum            Ordernum            int (nullable)      Linked prescription order number
ExceptionReason     ExceptionReason     varchar             Exception reason text
Exceptiontype       Exceptiontype       varchar             Exception type code
Manualauthuser      Manualauthuser      varchar             Manual override authorizer username
Manualauthdtm       Manualauthdtm       datetime (nullable) Manual authorization timestamp
Dosenote            Dosenote            varchar             Free-text note on dose event
Dosesig             Dosesig             varchar             Staff signature text
DoseSigImg          DoseSigImg          varbinary           Electronic signature as ASCII bytes
InventoryGroup      InventoryGroup      varchar             Medication/inventory group (schema-optional)
SiteId              SiteId              int (nullable)      Clinic site ID (PHC hardcoded to 105)
RowChkSum           RowChkSum           int (nullable)      Source SQL CHECKSUM for change detection
RowState            RowState            bit                 true = active, false = soft-deleted
LastModAt           LastModAt           datetime (nullable) ETL last write timestamp

Note on RowState type:
RowState in TblDose is a bool (bit), not an int. This differs from many other ETL
destination tables (e.g. FormQuestionAnswers uses int 0/1). The EF model confirms:
    public bool RowState { get; set; }
________________________________________

11b. pats.tbl_Dose_Excuse

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_Dose_Excuse
EF Model: BHG-DR-LIB/Models/TblDoseExcuse.cs

Primary Key: SiteCode + ExId (composite)

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar             Clinic identifier
ExId                ExId                int                 Unique excuse ID from source
CltId               CltId               int (nullable)      Patient/client ID
DtEx                DtEx                datetime (nullable) Date of the excused absence
Dtstamp             Dtstamp             datetime (nullable) Timestamp when excuse was recorded
StrUser             StrUser             varchar             Staff who recorded the excuse
StrExcused          StrExcused          varchar             Excuse type or reason code
RowChkSum           RowChkSum           int                 Source SQL CHECKSUM (non-nullable here)
RowState            RowState            bit                 true = active, false = soft-deleted
LastModAt           LastModAt           datetime            ETL last write timestamp (non-nullable)

Note: StrExcused is present in the model but does not appear in the SaveDoseExcuse
column mapping in the C# code. It must be populated by the source query selecting it
by name if present, or by the stg.Dose_ExcuseMerge stored procedure in the bulk path.
________________________________________

12. RowState — Soft Delete Tracking

RowState is the active/inactive flag for both pats.tbl_Dose and pats.tbl_Dose_Excuse.
In both cases it is a bool (bit) — not an int like FormQuestionAnswers uses.

Value   Meaning
-----   -------
true    Row is active — exists in current SAMMS data within the processing window
false   Row has been soft-deleted — was in Azure but not returned from SAMMS, or was
        voided (BlVoid + DtVoid both true), or CltId is negative (not -111)

How RowState flows in SaveDoses (EF Core):
1. Pre-pass: existing rows with DtDate >= dtWrk are set to RowState = false
2. Upsert (new or checksum changed): RowState set to true, then void-check may override
3. Upsert (checksum unchanged): RowState set to true, then void-check and CltId check may override
4. After loop: remaining false rows (in the pre-pass window but not seen in SAMMS) stay false
5. New rows: RowState set based on void-check and CltId check before inserting

How RowState flows in SaveDoseExcuse (EF Core):
1. Pre-pass: ALL rows (no date filter) set to RowState = false
2. Upsert (new or checksum changed): RowState set to true
3. Upsert (checksum unchanged): RowState set to true
4. Any excuse not returned from SAMMS stays false — soft-deleted

How RowState is managed in the Bulk path (stg.DoseMerge):
The stored procedure handles all RowState logic in T-SQL:
- Rows matched and updated get RowState set based on void/cltid logic within the SP
- Rows in staging but not in Azure get inserted
- Rows in Azure (within the window) but not in staging get RowState = false
________________________________________

13. Load Design Summary

Load type: Incremental upsert with date-window RowState soft-delete

Per run behavior for pats.tbl_Dose (EF Core path — V10A, CBCO, V21, V10):

  Source query window: ~1 month lookback + current year filter
  1. If reload=true: DELETE all Azure rows for site, then full SELECT from SAMMS
  2. Load ALL Azure rows for this site into memory
  3. Pre-pass: soft-reset RowState=false for rows where DtDate >= dtWrk
  4. For each SAMMS row:
     - Match by DoseId → found: check checksum
       - Changed or new: update all fields, set RowState=true, apply void/CltId checks
       - Unchanged: RowState=true, apply void/CltId checks
     - Not found → create new row
  5. db.SaveChanges() — commit pre-pass resets + existing row updates
  6. db.TblDose.AddRange(newdoses) + db.SaveChanges() — batch insert new rows

Per run behavior for pats.tbl_Dose (Bulk path — all other sites):

  Source query window: ~6 months lookback + current year filter
  1. If reload=true: DELETE all Azure rows for site in BHGTaskRunner, then full SELECT
  2. TRUNCATE stg.tbl_dose
  3. SqlBulkCopy → all SAMMS rows into stg.tbl_dose
  4. stg.DoseMerge @sitecode → MERGE into pats.tbl_Dose

Per run behavior for pats.tbl_Dose_Excuse (all sites — EF Core only):

  Source query window: controlled by task WhereCondition
  1. Load ALL Azure excuse rows for site into memory
  2. Pre-pass: ALL rows set to RowState = false (no date filter)
  3. For each SAMMS row:
     - Match by ExId → found: check checksum
       - Changed: update all fields, RowState = true
       - Unchanged: RowState = true, LastModAt refreshed
     - Not found → add inline via db.TblDoseExcuse.Add()
  4. db.SaveChanges() — single commit covers everything
________________________________________

14. Error Handling and Recovery

SaveDoses error handling:

    try
    {
        // pre-pass + upsert loop + SaveChanges() ...
    }
    catch (Exception e)
    {
        res.IsResult = false
        res.ExceptMsg = e.Message
        if (e.InnerException != null)
            res.ExceptInnerMsg = e.InnerException.Message
    }

If an EF Core exception occurs:
- The entire batch for that site is rolled back (SaveChanges not committed)
- RCodes.IsResult = false is returned to BHGTaskRunner
- The task is marked with the error message

SaveDoseExcuse error handling:

    try { ... }
    catch (Exception e)
    {
        res = false
        Console.WriteLine(e.Message)
        if (e.InnerException != null)
            Console.WriteLine(e.InnerException.Message)
    }

Note: SaveDoseExcuse errors are written to console only — not captured in a structured
return object. BHGTaskRunner receives only the bool false return value.

Bulk path error handling (BulkDartsSrvLoader):

    try
    {
        bc.WriteToServer(tbl)
        sm.ExeSqlCmd("exec stg.DoseMerge '" + sitecode + "'", ...)
    }
    catch (Exception ex)
    {
        rst.IsResult = false
        Console.WriteLine(ex.Message)
        rst.ExceptMsg = ex.Message
        rst.ExceptInnerMsg = ex.InnerException.Message  (if not null)
    }
    finally
    {
        bc.Close()
    }

If bulk copy fails: stg.tbl_dose may be partially populated or truncated — a manual
run with reload=true may be needed to restore pats.tbl_Dose to a consistent state.

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

A failed Dose run for a clinic will automatically be retried the next day.
________________________________________

15. RowTrax — Audit and Row Count Tracking

For Dose tasks where st.RowTrax = true (and SiteCode != "PHC"), BHGTaskRunner writes
a RowTrax entry after each run:

For pats.tbl_Dose:
    sd.SaveRowTrax(
        st.SiteCode,
        st.WorkDate.Value.Date,
        st.TaskName,              // 'pats.tbl_Dose'
        sourceCount,              // count(*) from SAMMS where CltID not null and dtMedDate not null
        destCount,                // count(*) from pats.tbl_dose where SiteCode = sc and RowState = 1
        null)

For pats.tbl_Dose_Excuse:
    sd.SaveRowTrax(
        st.SiteCode,
        st.WorkDate.Value.Date,
        st.TaskName,              // 'pats.tbl_dose_excuse'
        sourceCount,              // count(*) from SAMMS where CltID > 0
        destCount,                // count(*) from pats.tbl_dose_excuse where SiteCode = sc and RowState = 1
        null)

These counts are stored in tsk.tbl_RowTrax and are used for:
- Monitoring whether the ETL is writing expected volumes
- Detecting sites where dose data has dropped unexpectedly
- Historical trend analysis for data completeness auditing

Note on source count difference:
- Dose source count filter: CltID is not null and dtMedDate is not null (quality guard)
- Excuse source count filter: CltID > 0 (only positive client IDs)
________________________________________

16. Key Design Notes and Gotchas

Two-path design for tbl_Dose:
The EF Core path (SaveDoses) exists only for four sites: V10A, CBCO, V21, V10. All other
80+ sites use the Bulk path. This likely reflects a historical transition — these four sites
were either too small for bulk to matter, have schema differences, or were the original
implementation before BulkDartsSrvLoader was generalized.

Lookback window difference between paths:
- EF Core path (V10A, CBCO, V21, V10): 1-month lookback
- Bulk path (all others): 6-month lookback
The bulk path pulls a wider window to ensure the DoseMerge stored procedure has enough
source data to correctly identify inactive records (RowState = false) for recent months.

DtVoid is a bool, not a DateTime:
The field name begins with "dt" which throughout SAMMS conventions normally signals a
DateTime. However, DtVoid in TblDose is declared as bool? (bit). Despite the name, it
stores a void status flag. The void-check condition reads:
    if (dose.BlVoid == true && dose.DtVoid == true) { dose.RowState = false; }
Both BlVoid AND DtVoid must be true for a dose to be soft-deleted.

dtprep uses length > 0 instead of > 6:
All other DateTime fields in SaveDoses use a length > 6 guard before parsing. The dtprep
field uses length > 0. Any non-empty string — even a single character — will attempt to
parse as a DateTime and may throw an exception. This is a potential source of parse errors
for malformed dtprep values.

CltId = -111 is a protected sentinel:
A negative CltId normally signals that a record should be soft-deleted (RowState = false).
The value -111 is explicitly exempt in SaveDoses:
    if ((dose.CltId < 0) && (dose.CltId != -111)) { dose.RowState = false; }
This means -111 records stay active. The -111 sentinel represents a known special-purpose
administrative record type in SAMMS.

InventoryGroup column guard:
The InventoryGroup field is only mapped if the column exists in the source DataTable:
    if (r.Table.Columns.Contains("InventoryGroup")) { ... }
This allows SaveDoses to run against multiple SAMMS schema versions where older deployments
may not have the InventoryGroup column in their dose view or table.

PHC SiteId hardcoded to 105:
When SiteCode == "PHC", the SiteId field is always written as 105 regardless of what the
source row contains. This hardcoding is specific to the PHC clinic's site identity in the
Azure database.

DoseSigImg stored as ASCII bytes:
The electronic signature image is transmitted from SAMMS as a string representation and
stored in Azure as a byte array via ASCII encoding:
    dose.DoseSigImg = System.Text.Encoding.ASCII.GetBytes(r["dosesigimg"].ToString())
This always runs — there is no length guard. An empty string produces an empty byte array.

SaveDoseExcuse uses single commit:
Unlike SaveDoses which uses two commits (one for updates, one for inserts), SaveDoseExcuse
commits everything in a single db.SaveChanges() at the end. Pre-pass resets, field updates,
and new row inserts are all flushed together.

SaveDoseExcuse CltId re-assignment:
When an existing excuse row matches on ExId and the checksum has changed, CltId is
explicitly re-assigned from the source:
    dose.CltId = intcltid
This means patient ID corrections in SAMMS propagate to existing Azure rows on the next
checksum-changing update.

Dose excuse has no bulk path:
The comment in BHGTaskRunner confirms a bulk path for excuse was considered:
    //rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_dose_excuse", st.SiteCode, null)
But it was never activated. SaveDoseExcuse is used for all sites.

stg.Dose_ExcuseMerge exists but is not called:
BulkDartsSvc.cs contains a case for "stg.tbl_dose_excuse" that would call
stg.Dose_ExcuseMerge. This stored procedure exists on the Azure side but the
BHGTaskRunner code path never routes dose excuse records through BulkDartsSrvLoader.
________________________________________

17. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-Dose (Status=17)
        |-- insert child tasks per clinic:
        |       pats.tbl_Dose        x 80+ clinics
        |       pats.tbl_dose_excuse x 80+ clinics
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2 (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 10
        |
        |-- filter: TaskName = 'SAMMS-ETL-Dose', SiteCode != 'PHC', Status=17
        |-- mark parent task Status=18 (running)
        |
        |-- for each child task (one per clinic per table type):
        |
        |======================================================
        |  BRANCH A: TaskName = pats.tbl_dose
        |======================================================
        |
        |   Build base strCmd from st.FromTblVw / st.SrcSchema
        |
        |   Is SiteCode in (V10A, CBCO, V21, V10)?
        |          |
        |       YES (EF Core path)              NO (Bulk path)
        |          |                                 |
        |          |-- st.Reload = true?             |-- st.Reload = true?
        |          |   YES: strCmd += full WHERE     |   YES: DELETE pats.tbl_dose for site
        |          |        SaveDoses(reload=true)   |        strCmd += full WHERE
        |          |        (SaveDoses deletes        |        → BulkDartsSrvLoader
        |          |         internally first)       |   NO: strCmd += 6-month window WHERE
        |          |   NO: strCmd += 1-month WHERE   |        → BulkDartsSrvLoader
        |          |        SaveDoses(reload=false)  |
        |          |                                 |
        |          V                                 V
        |   SQLSvrManager.GetTableData()      SQLSvrManager.GetTableData()
        |   (SAMMS source → DataTable)        (SAMMS source → DataTable)
        |          |                                 |
        |          V                                 V
        |   SaveDoses()                       BulkDartsSrvLoader("stg.tbl_dose")
        |     |                                 |
        |     |-- hard delete if reload         |-- TRUNCATE stg.tbl_dose
        |     |-- load all Azure doses           |-- SqlBulkCopy → stg.tbl_dose
        |     |-- pre-pass: reset               |-- exec stg.DoseMerge 'sitecode'
        |     |   DtDate >= dtWrk → false           MERGE pats.tbl_Dose:
        |     |-- loop SAMMS rows:                     MATCHED + chksum diff → UPDATE
        |     |   DoseId lookup:                       NOT MATCHED BY TARGET → INSERT
        |     |   found  → checksum check              in-window missing → RowState=false
        |     |     changed → full update
        |     |     same   → RowState=true,
        |     |               void/CltId checks
        |     |   not found → new row
        |     |-- SaveChanges() (updates+resets)
        |     |-- AddRange(new) + SaveChanges()
        |          |
        |          +--------------------+
        |                               |
        |                               V
        |                   pats.tbl_Dose (Azure BHG_DR)
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != PHC)
        |       source count = count from SAMMS where CltID not null and dtMedDate not null
        |       dest count   = count from pats.tbl_dose where SiteCode = sc and RowState = 1
        |       → tsk.tbl_RowTrax
        |
        |======================================================
        |  BRANCH B: TaskName = pats.tbl_dose_excuse
        |======================================================
        |
        |   Build strCmd from st.FromTblVw (using task WhereCondition as strWhere)
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |   (SAMMS source → DataTable)
        |          |
        |          V
        |   SaveDoseExcuse()
        |     |
        |     |-- load all Azure excuse rows for site
        |     |-- pre-pass: ALL rows → RowState = false (no date filter)
        |     |-- loop SAMMS rows:
        |     |   ExId lookup:
        |     |   found  → checksum check
        |     |     changed → update DtEx, Dtstamp, StrUser, CltId; RowState=true
        |     |     same   → RowState=true, LastModAt refreshed
        |     |   not found → new row (Add inline)
        |     |-- db.SaveChanges()  (single commit — all changes)
        |          |
        |          V
        |   pats.tbl_Dose_Excuse (Azure BHG_DR)
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != PHC)
        |       source count = count from SAMMS where CltID > 0
        |       dest count   = count from pats.tbl_dose_excuse where SiteCode = sc and RowState = 1
        |       → tsk.tbl_RowTrax
        |
        V
BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)
________________________________________

18. File Reference Map

File Path                                           Purpose
---------                                           -------
BCAppCode/Scheduler/Program.cs                      Creates daily task queue — inserts SAMMS-ETL-Dose tasks
BCAppCode/BHGTaskRunner/Program.cs                  Main ETL driver (arg=10 → Dose pipeline)
                                                    Contains WHERE clause builders for both dose tables
                                                    Routes to EF Core or Bulk based on SiteCode
                                                    Contains hard-delete logic for bulk reload path
BCAppCode/BHG-DR-LIB/SaveDoses.cs                  EF Core upsert — SaveDoses + SaveDoseExcuse
BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs              SqlBulkCopy loader — BulkDartsSrvLoader
                                                    Routes stg.tbl_dose → exec stg.DoseMerge
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs              ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblDose.cs             EF Model → pats.tbl_Dose
BCAppCode/BHG-DR-LIB/Models/TblDoseExcuse.cs       EF Model → pats.tbl_Dose_Excuse
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs       EF DbContext — registers TblDose + TblDoseExcuse
________________________________________

19. Quick Reference Summary

What triggers Dose ETL?              Scheduler.exe creates tasks, BHGTaskRunner.exe 10 processes them
TaskName in scheduler?               SAMMS-ETL-Dose
Source tables in SAMMS?              dbo.tblDose (or site-specific view), dbo.tblDoseExcuse
Destination tables in Azure?         pats.tbl_Dose
                                     pats.tbl_Dose_Excuse
EF Core path applies to?             Dose: V10A, CBCO, V21, V10 only
                                     DoseExcuse: all sites
Bulk path applies to?                Dose only — all sites except V10A, CBCO, V21, V10
Staging table?                       stg.tbl_dose (bulk dose path only)
Merge stored procedure?              stg.DoseMerge @sitecode (bulk dose path)
                                     stg.Dose_ExcuseMerge (exists but not called — SaveDoseExcuse used instead)
Primary key — tbl_Dose?              SiteCode + DoseId
Primary key — tbl_Dose_Excuse?       SiteCode + ExId
How is change detected?              RowChkSum — full field update only when checksum differs or row is new
What is RowState?                    bool (bit) — true=active, false=soft-deleted
                                     Note: int in some other tables but bool here
How are soft-deletes handled?        tbl_Dose: pre-pass resets rows with DtDate >= dtWrk to false;
                                       SAMMS rows re-activate matched records;
                                       void check (BlVoid+DtVoid) and negative CltId also force false
                                     tbl_Dose_Excuse: pre-pass resets ALL rows to false;
                                       all returned SAMMS rows re-activate to true
Void logic?                          BlVoid = true AND DtVoid = true → RowState = false
                                     (only in SaveDoses — no equivalent in SaveDoseExcuse)
CltId = -111 sentinel?               Yes — exempt from negative-CltId soft-delete rule in SaveDoses
Reload override?                     st.Reload = true forces full history SELECT (WHERE CltID not null)
                                     EF Core path: SaveDoses deletes Azure rows internally
                                     Bulk path: BHGTaskRunner deletes Azure rows before BulkDartsSrvLoader
Lookback window — EF Core sites?     ~1 month (WorkDate - 1 month for dtDate lower bound)
Lookback window — Bulk sites?        ~6 months (WorkDate - 6 months for dtDate lower bound)
Both windows also apply year filter: Year(dtDate) >= (WorkDate+DaysBack - 1yr).Year
PHC handled here?                    No — PHC uses PHC/Program.cs and is excluded by
                                     x.SiteCode != "PHC" filter in BHGTaskRunner
DoseSigImg?                          Always stored as ASCII byte array — System.Text.Encoding.ASCII.GetBytes()
InventoryGroup?                      Optional column — only mapped if present in source DataTable
SiteId for PHC?                      Hardcoded to 105 when SiteCode == "PHC"
RowTrax audit?                       Source count vs active destination count per site
Error recovery?                      Scheduler resets failed tasks to Status=17 on next daily run
SaveDoseExcuse return type?          bool — not RCodes; BHGTaskRunner manually sets RowsProcessed
________________________________________
