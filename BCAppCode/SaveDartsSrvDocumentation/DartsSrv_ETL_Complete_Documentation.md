
DartsSrv ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract counseling session records
(DartsSrv) from local SAMMS SQL Server databases at each clinic and load them into the central
Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What DartsSrv data is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How SelectConstructor builds the source SQL
- How SQLSvrManager executes against the clinic databases
- How BulkDartsSvc performs the bulk load (primary path)
- How SaveDartsSrvs performs the EF Core upsert (historical/backfill path)
- What the source table looks like and all its columns
- What the staging table looks like and all its columns
- What all 11 destination year-tables look like and all their columns
- How change detection works using RowChkSum
- How the dynamic date lookback window is determined
- How RowTrax audit tracking works
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What is DartsSrv?

DARTS stands for Drug Abuse Reporting Tool Services. A DartsSrv record is created every time a
patient has a counseling session with a clinical staff member at a BHG clinic.

Each row represents one counseling service event. It captures who the patient was, who the
counselor was, when the session happened, what type of service was delivered, how many units
(hours) were provided, billing and signature details, and clinical dimension scores.

Why it is important

DartsSrv is the highest-volume dataset in the entire BHG data warehouse. With 80+ clinics each
generating thousands of sessions per year going back to 2008, the total dataset spans tens of
millions of rows across all years. This data is the primary source for clinical outcome reporting,
billing reconciliation, and regulatory compliance.

Why the data is split by year

A single DartsSrv table for all years would be impractical to query. To keep performance
acceptable, the Azure destination is split into separate tables by year:
pats.tbl_DartsSrv_2014B4, pats.tbl_DartsSrv_2015, ... pats.tbl_DartsSrv_2024.
Each year table is independently indexed and partitioned. This means a report querying only 2023
data never touches 2022 data.

Volume estimate
80 clinics x 5,000 sessions/year/clinic x 10 years = approximately 4 million rows across all tables.

Load type

This is an incremental upsert load with change detection.
- It is not a full refresh (source data is not reloaded from scratch each run).
- On each run, only records changed since the lookback window are fetched from source.
- Only rows where the RowChkSum has changed are written to Azure.
- Rows already in Azure with no changes are left untouched.
________________________________________

3. Systems Involved

System / File                    Role
-----------                      ----
tsk.tbl_Schedule (Azure DB)      Configuration — defines schedules and their run times
Scheduler.exe                    Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=9          Main ETL orchestrator for DartsSrv
dms.tbl_MapSrc2Dsn (Azure DB)    Metadata — defines which columns to SELECT for each ActionKey
SelectConstructor.cs             Assembles SELECT statement from metadata
SQLSvrManager.cs                 Fires SELECT against the clinic SAMMS SQL Server
BulkDartsSvc.cs                  SqlBulkCopy into staging + executes merge stored procedures
SaveDartsSrvs.cs                 EF Core row-by-row upsert (historical backfill path)
stg.tbl_DartsSrv (Azure)         Staging table — temporary landing zone before merge
stg.DartsSrvMerge (SP)           Stored procedure that merges staging into final year tables
pats.tbl_DartsSrv_20XX (Azure)   Final destination tables — one per year, permanent store
ctrl.tbl_LocationCons (Azure)    Connection strings for each clinic's SAMMS SQL Server
tsk.tbl_RowTrax (Azure)          Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How DartsSrv Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily (typically overnight or early morning) and populates the task
queue for all ETL pipelines. It does NOT move data — it only creates tasks.

What the Scheduler does for DartsSrv

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task that was left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Tasks from previous days that were never picked up are marked as expired (RowState=26).

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For DartsSrv, there is a row in
tsk.tbl_Schedule with:
    Name = 'SAMMS-ETL-DartSvc'
    ActionKey = 9
    NextRunTime = (calculated next run datetime)
    ScheduleId = 9

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'SAMMS-ETL-DartSvc'
    SiteCode = 'All'
    Status   = 17
    WorkDate = today
    RunAt    = NextRunTime

Step 4 — Insert the child task rows (one per clinic per table)
Using a cross join of dms.vw_MapAction and tsk.tbl_Tasks2, the Scheduler inserts one child
task row per clinic for DartsSrv:

    insert into tsk.tbl_Tasks2(ParentTaskId, TaskName, ...)
    select t.TaskId,
           ma.DsnSchema + '.' + ma.DsnTbl,  -- = 'pats.tbl_DartsSrv'
           ...
           ma.ActionKey,                     -- = 9
           ma.StepKey,
           ma.SiteCode                       -- = 'B01', 'B02', etc.
    from dms.vw_MapAction ma
    cross join tsk.tbl_Tasks2 t
    where ma.Enabled = 1
      and ma.IsActive = 1
      and case when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_DartsSrv'
               then 'SAMMS-ETL-DartSvc' end = t.TaskName

This produces approximately 80+ child task rows, one per active clinic, all under the
parent task SAMMS-ETL-DartSvc.

Step 5 — Advance the schedule
    update tsk.tbl_Schedule
    set NextRunTime = DATEADD(d, 1, NextRunTime)
    where Enabled = 1

Step 6 — Clean up
    delete from tsk.tbl_Tasks2
    where RunAt <= DateAdd(m, -3, GetDate()) or RowState = 26

Tasks older than 3 months or expired tasks are deleted.

Task queue structure after Scheduler runs

tsk.tbl_Tasks2 will contain:
    ParentTaskId = NULL
        TaskName = 'SAMMS-ETL-DartSvc'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'pats.tbl_DartsSrv'
        SiteCode = 'B01'
        ActionKey = 9
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_DartsSrv'
        SiteCode = 'B02'
        ActionKey = 9
        Status   = 17

    ... (one row per active clinic)
________________________________________

5. BHGTaskRunner — How DartsSrv Is Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 9 to process only the DartsSrv schedule.

Command:   BHGTaskRunner.exe 9

Step 1 — Filter task queue for Schedule 9
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"          // PHC uses a separate runner
        && x.Status == 17            // ready to run
        && x.TaskName == "SAMMS-ETL-DartSvc"
        && x.RunAt < DateTime.Now)   // time has passed

Step 2 — Mark parent task as running
For each parent task found:
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)

Step 4 — For each child task (each clinic), get the column mapping
    db.WorkToDo.Where(x => x.Enabled
                        && x.ActionKey == st.ActionKey        // = 9
                        && x.ActionStepKey == st.ActionStepKey)

This returns the list of column mappings from dms.tbl_MapSrc2Dsn for ActionKey=9.

Step 5 — Build the SELECT statement
SelectConstructor.GetSLT() assembles the SELECT field list by:
- Reading all enabled column mappings for ActionKey=9
- Building a CHECKSUM(...) expression across all mapped columns to produce RowChkSum
- Replacing placeholder tokens (@SiteCode, @WorkDate, @Samms)

Step 6 — DartsSrv-specific: Check if ServiceType column exists
Not all SAMMS versions have the ServiceType column. BHGTaskRunner checks:

    select name from sys.all_columns
    where object_id = (select object_id from sys.all_objects
                       where upper(name) = 'TBLDARTSSRV')
    and name = 'ServiceType'

If the result has zero rows, ServiceType is stripped from the SELECT:
    strCmd = strCmd.Replace(", [ServiceType] ServiceType", "")
                   .Replace(", [ServiceType]", "")

Step 7 — Build the WHERE clause with dynamic date lookback

The lookback window is calculated based on the WorkDate day of week:

    int offsetvalue = -15     // default: look back 15 days

    if (WorkDate is Friday)
    {
        if (WorkDate.Month == WorkDate.AddDays(1).Month)
            offsetvalue = -90      // last Friday of month: 90 days
        if (WorkDate == 1/24/2025)
            offsetvalue = -200     // one-time special override
    }

    DateTime DartsDate = WorkDate.AddDays(offsetvalue)

The WHERE clause checks FIVE date columns to ensure any recently touched record is captured:

    Where dsClt is not null
    and (
        convert(date, dsdtstart) >= 'DartsDate'     -- session start date
        or convert(date, dsDtAdded) >= 'DartsDate'  -- record added date
        or convert(date, dsUpdate) >= 'DartsDate'   -- record last updated
        or convert(date, dsBilled) >= 'DartsDate'   -- billing date
        or convert(date, dsSigDate) >= 'DartsDate'  -- signature date
        or dsClt <= 0                               -- catch placeholder records
    )
    order by 1, 2

Why five date columns?
A counseling session might have been completed weeks ago but signed or billed today. Checking
only the start date would miss those updated records. All five fields are checked so any row
that was touched within the lookback window is picked up.

Step 8 — Execute SELECT against SAMMS clinic
    SrcDt = sm.GetTableData(
        st.FromTblVw,     -- source table = 'tblDartsSrv'
        strCmd,           -- full SELECT...WHERE statement
        st.ConStr)        -- SAMMS connection string for this clinic

Returns a DataTable with all recently touched DartsSrv rows from this clinic.

Step 9 — Call BulkDartsSrvLoader (primary path)
    rCodes = bldr.BulkDartsSrvLoader(
        SrcDt,                               -- rows from SAMMS
        "stg.tbl_dartssrv",                  -- staging destination
        st.SiteCode,                         -- e.g. "B01"
        DartsDate,                           -- lookback date
        null)

Step 10 — RowTrax audit (if enabled for this task)
If the task has RowTrax enabled (st.RowTrax == true):

    Source count:
        select count(1) from dbo.tblDartsSrv
        where dsClt > 0
        and (dsDtAdded > '12/31/2018' or dsDtStart = '12/31/2018')

    Destination count:
        select count(1) from pats.vw_DartsSrv
        where SiteCode = 'B01'

    These counts are saved to tsk.tbl_RowTrax for audit purposes.

Step 11 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. SelectConstructor — How the SELECT Is Built

File: BCAppCode/BHG-DR-LIB/SelectConstructor.cs

SelectConstructor.GetSLT() is called for every child task. It reads the column metadata from
dms.tbl_MapSrc2Dsn (exposed through db.WorkToDo) and assembles the SELECT field list.

For DartsSrv (ActionKey=9), the SELECT looks like this conceptually:

    Select
        SiteCode = 'B01',
        dsID,
        dsClt,
        dsDIM1,
        dsDIM2,
        dsDIM3,
        dsDIM4,
        dsDIM5,
        dsDIM6,
        dsTxtSrv,
        dsDtStart,
        dsDtEnd,
        dsTxtType,
        dsdblUnits,
        dsNoteID,
        dsDtAdded,
        dstxtStaff,
        dstxtNote,
        dsRTBNOTE,
        DSbilled,
        dsGROUPNUM,
        dsPROGRAM,
        dsUpdate,
        dsUPDATEStaff,
        dsInvalidatedOn,
        dsError,
        dsTxtHIV,
        dsDartsGroup,
        repOldSrv,
        dsSignature,
        dsSigDate,
        dssigdateCOSIGN,
        dssignatureCOSIGN,
        dsSigUser,
        dsSigUserCosign,
        dsSIGCLT,
        dsSIGCLTDATE,
        dsSIGCLTUSER,
        dsAPTID,
        dsuncharted,
        dsTxDim1 ... dsTxDim6,
        dsDIAG,
        dsArea,
        dsGroupDefaultNote,
        dsGroupEnd,
        dsGroupIdentity,
        dsGroupStart,
        dsDIAG10,
        SiteID,
        dsDBnotes,
        dsSigCltImg,
        dsSignatureCoSignImg,
        dsSignatureIMG,
        MG,
        [ServiceType],   -- stripped if column does not exist in this clinic's SAMMS
        RowChkSum = CHECKSUM(dsID, dsClt, dsDIM1, dsDIM2, ... all 50+ columns ...)
    from dbo.tblDartsSrv

The CHECKSUM() function at the end calculates a hash across all mapped columns. This becomes the
RowChkSum value that is compared during change detection.

SelectConstructor also handles the historical EF Core path (backfill by year)

When the table destination is 'tbl_dartssrv' and a WrkYear is provided, SelectConstructor
routes to the appropriate SaveDartsSrvs method:

    case "tbl_dartssrv":
        switch (st.WrkYear)
        {
            case "2014": sd.SaveDartSrv2014(SrcDt, SiteCode, ActionKey, 1/1/2014, db)
            case "2015": sd.SaveDartSrv2015(...)
            case "2016": sd.SaveDartSrv2016(...)
            case "2017": sd.SaveDartSrv2017(...)
            case "2018": sd.SaveDartSrv2018(...)
            case "2019": sd.SaveDartSrv2019(...)
            case "2020": sd.SaveDartSrv2020(...)
            case "2021": sd.SaveDartSrv2021(...)
            case "2022": sd.SaveDartSrv2022(...)
        }
________________________________________

7. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL Server,
executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row in ctrl.tbl_LocationCons contains:
        SiteCode   = 'B01'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_B01;User Id=...;Password=...;'

The DataTable returned by GetTableData contains all rows from dbo.tblDartsSrv for this clinic
that match the dynamic WHERE clause. This DataTable is passed directly into BulkDartsSrvLoader
or SaveDartsSrvs.
________________________________________

8. Source Table — dbo.tblDartsSrv (SAMMS SQL Server)

Location: Each clinic's local SAMMS SQL Server database
Schema  : dbo
Table   : tblDartsSrv

Primary Key: dsID (unique per clinic — not globally unique across clinics)

All columns pulled by the ETL:

Column Name             Type            Description
-----------             ----            -----------
dsID                    int             Unique session ID within this clinic
dsClt                   int             Client/patient ID
SiteCode                varchar(25)     Added by ETL — clinic identifier (e.g. 'B01')
dsDIM1                  bit             Clinical dimension 1 flag
dsDIM2                  bit             Clinical dimension 2 flag
dsDIM3                  bit             Clinical dimension 3 flag
dsDIM4                  bit             Clinical dimension 4 flag
dsDIM5                  bit             Clinical dimension 5 flag
dsDIM6                  bit             Clinical dimension 6 flag
dsTxtSrv                varchar(100)    Service type description text
dsDtStart               datetime        Session start date/time
dsDtEnd                 datetime        Session end date/time
dsTxtType               varchar(50)     Service type code
dsdblUnits              float           Units (hours/time) of service
dsNoteID                int             Linked note record ID
dsDtAdded               datetime        Date record was added to SAMMS
dstxtStaff              varchar(100)    Staff member providing the service
dstxtNote               ntext           Full text of counseling note
dsRTBNOTE               ntext           RTB (Return To Base) note text
DSbilled                datetime        Date the session was billed
dsGROUPNUM              varchar(50)     Group session number
dsPROGRAM               varchar(50)     Program type
dsUpdate                datetime        Date record was last updated
dsUPDATEStaff           varchar(50)     Staff who last updated the record
dsInvalidatedOn         datetime        Date record was invalidated/voided
dsError                 varchar(4000)   Error message if record has issues
dsTxtHIV                varchar(50)     HIV program flag
dsDartsGroup            int             DARTS group session identifier
repOldSrv               numeric(18,0)   Old service type code (legacy mapping)
dsSignature             ntext           Counselor signature (text/base64)
dsSigDate               datetime        Date counselor signed the record
dssigdateCOSIGN         datetime        Date co-signer signed the record
dssignatureCOSIGN       ntext           Co-signer signature text
dsSigUser               varchar(50)     Username of signing counselor
dsSigUserCosign         varchar(50)     Username of co-signing counselor
dsSIGCLT                ntext           Client signature text
dsSIGCLTDATE            datetime        Date client signed the record
dsSIGCLTUSER            varchar(50)     Username associated with client signature
dsAPTID                 int             Linked appointment ID
dsuncharted             bit             Flag for uncharted sessions
dsTxDim1                int             Treatment dimension score 1
dsTxDim2                int             Treatment dimension score 2
dsTxDim3                int             Treatment dimension score 3
dsTxDim4                int             Treatment dimension score 4
dsTxDim5                int             Treatment dimension score 5
dsTxDim6                int             Treatment dimension score 6
dsDIAG                  varchar(100)    Primary diagnosis code (ICD-9 style)
dsArea                  varchar(100)    Treatment area/program area
dsGroupDefaultNote      bit             Flag for group session default note
dsGroupEnd              datetime        Group session end date/time
dsGroupIdentity         int             Group session identity key
dsGroupStart            datetime        Group session start date/time
dsDIAG10                varchar(100)    Primary diagnosis code (ICD-10)
SiteID                  int             SAMMS internal site numeric ID
dsDBnotes               varchar(250)    Database administrative notes
dsSigCltImg             varbinary       Client signature image bytes
dsSignatureCoSignImg    varbinary       Co-signer signature image bytes
dsSignatureIMG          varbinary       Counselor signature image bytes
MG                      float           Milligrams (used for MAT programs)
ServiceType             varchar(?)      Service type (optional — stripped if column absent)
RowChkSum               int             Computed by ETL: CHECKSUM() across all columns

Note on RowChkSum:
RowChkSum is NOT a column in dbo.tblDartsSrv. It is computed during the SELECT by the
SelectConstructor using SQL Server's CHECKSUM() function across all 50+ mapped columns. It is
used by both the staging merge stored procedures and the EF Core upsert to detect changed rows.
________________________________________

9. Staging Table — stg.tbl_DartsSrv (Azure BHG_DR)

Location: Azure SQL Database BHG_DR
Schema  : stg
Table   : tbl_DartsSrv
EF Model: BHG-DR-LIB/Models/TblDartsSrvStg.cs

Primary Key: dsID + SiteCode (composite)

The staging table is a temporary landing zone. It exists only to hold data during a bulk load.

Its schema is identical to the destination year-tables. Every column present in the final
pats.tbl_DartsSrv_YYYY tables also exists in stg.tbl_DartsSrv.

Column Name             SQL Column Name         Type            Notes
-----------             ---------------         ----            -----
DsId                    dsID                    int             PK (with SiteCode)
DsClt                   dsClt                   int
SiteCode                SiteCode                varchar(25)     PK (with dsID)
RowChkSum               RowChkSum               int             Change detection hash
DsDim1 - DsDim6         dsDIM1 - dsDIM6         bit (nullable)  Clinical dimension flags
DsTxtSrv                dsTxtSrv                varchar(100)
DsDtStart               dsDtStart               datetime
DsDtEnd                 dsDtEnd                 datetime
DsTxtType               dsTxtType               varchar(50)
DsdblUnits              dsdblUnits              float
DsNoteId                dsNoteID                int
DsDtAdded               dsDtAdded               datetime
DstxtStaff              dstxtStaff              varchar(100)
DstxtNote               dstxtNote               ntext
DsRtbnote               dsRTBNOTE               ntext
Dsbilled                DSbilled                datetime
DsGroupnum              dsGROUPNUM              varchar(50)
DsProgram               dsPROGRAM               varchar(50)
DsUpdate                dsUpdate                datetime
DsUpdatestaff           dsUPDATEStaff           varchar(50)
DsInvalidatedOn         dsInvalidatedOn         datetime
DsError                 dsError                 varchar(4000)
DsTxtHiv                dsTxtHIV                varchar(50)
DsDartsGroup            dsDartsGroup            int
RepOldSrv               repOldSrv               numeric(18,0)
DsSignature             dsSignature             ntext
DsSigDate               dsSigDate               datetime
DssigdateCosign         dssigdateCOSIGN         datetime
DssignatureCosign       dssignatureCOSIGN       ntext
DsSigUser               dsSigUser               varchar(50)
DsSigUserCosign         dsSigUserCosign         varchar(50)
DsSigclt                dsSIGCLT                ntext
DsSigcltdate            dsSIGCLTDATE            datetime
DsSigcltuser            dsSIGCLTUSER            varchar(50)
DsAptid                 dsAPTID                 int
Dsuncharted             dsuncharted             bit
DsTxDim1 - DsTxDim6     dsTxDim1 - dsTxDim6     int             Treatment dimension scores
DsDiag                  dsDIAG                  varchar(100)
DsArea                  dsArea                  varchar(100)
DsGroupDefaultNote      dsGroupDefaultNote      bit
DsGroupEnd              dsGroupEnd              datetime
DsGroupIdentity         dsGroupIdentity         int
DsGroupStart            dsGroupStart            datetime
DsDiag10                dsDIAG10                varchar(100)
SiteId                  SiteID                  int
DsDbnotes               dsDBnotes               varchar(250)
DsSigCltImg             dsSigCltImg             varbinary(max)
DsSignatureCoSignImg    dsSignatureCoSignImg    varbinary(max)
DsSignatureImg          dsSignatureIMG          varbinary(max)
Mg                      MG                      float
LastModAt               LastModAt               datetime

Staging lifecycle per run:
    1. TRUNCATE stg.tbl_DartsSrv                    (empty it before load)
    2. SqlBulkCopy.WriteToServer(DataTable)          (bulk insert all rows from SAMMS)
    3. exec stg.DartsSrvMerge                        (merge into pats.tbl_DartsSrv_2014B4)
    4. exec stg.DartsSrvMerge22                      (merge into pats.tbl_DartsSrv_2022)
    5. exec stg.DartsSrvMerge23                      (merge into pats.tbl_DartsSrv_2023)
    6. exec stg.DartsSrvMerge24                      (merge into pats.tbl_DartsSrv_2024)
    7. exec stg.DartsSrvMerge25                      (merge into pats.tbl_DartsSrv_2025)
    8. exec stg.DartsSrvMerge26                      (merge into pats.tbl_DartsSrv_2026)
    9. exec stg.DartsSrvMerge27                      (merge into pats.tbl_DartsSrv_2027)
   10. exec stg.DartsSrvMerge28                      (merge into pats.tbl_DartsSrv_2028)
   11. TRUNCATE stg.tbl_DartsSrv                     (clean up after merge)
________________________________________

10. BulkDartsSvc — The Primary Load Path

File: BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs
Class: BulkDartsSvc
Method: BulkDartsSrvLoader()

Signature:
    public RCodes BulkDartsSrvLoader(
        DataTable tbl,           // rows from SAMMS for this clinic
        string dsnSchTbl,        // = "stg.tbl_dartssrv"
        string sitecode,         // = "B01"
        DateTime wrkDt,          // lookback date
        BHG_DRContext db)        // EF context (can be null)

This is the HIGH-VOLUME path used for all daily incremental loads on all standard BHG clinics.

Step-by-step behavior:

1. Guard check
   if (tbl.Rows.Count == 0) { return immediately — nothing to load }

2. TRUNCATE the staging table
   sm.ExeSqlCmd("Truncate Table stg.tbl_dartssrv", sm.ConnectionString)
   Wipes any leftover data from previous run.

3. Map columns for bulk copy
   foreach (DataColumn c in tbl.Columns)
       bc.ColumnMappings.Add(c.ColumnName, c.ColumnName)
   Column names from DataTable are mapped directly to staging table columns by name.

4. SqlBulkCopy.WriteToServer(tbl)
   All rows from the DataTable are bulk-inserted into stg.tbl_dartssrv in a single operation.
   BulkCopyTimeout = 99999 seconds (essentially unlimited).

5. Execute merge stored procedures (all 8)
   case "stg.tbl_dartssrv":
       exec stg.DartsSrvMerge      (rows up to 2021 → pats.tbl_DartsSrv_2014B4 + older years)
       exec stg.DartsSrvMerge22    (2022 sessions   → pats.tbl_DartsSrv_2022)
       exec stg.DartsSrvMerge23    (2023 sessions   → pats.tbl_DartsSrv_2023)
       exec stg.DartsSrvMerge24    (2024 sessions   → pats.tbl_DartsSrv_2024)
       exec stg.DartsSrvMerge25    (2025 sessions   → pats.tbl_DartsSrv_2025)
       exec stg.DartsSrvMerge26    (2026 sessions   → pats.tbl_DartsSrv_2026)
       exec stg.DartsSrvMerge27    (2027 sessions   → pats.tbl_DartsSrv_2027)
       exec stg.DartsSrvMerge28    (2028 sessions   → pats.tbl_DartsSrv_2028)

Each stored procedure internally executes a SQL MERGE statement such as:
    MERGE pats.tbl_DartsSrv_2022 AS tgt
    USING stg.tbl_DartsSrv AS src ON (tgt.dsID = src.dsID AND tgt.SiteCode = src.SiteCode)
    WHEN MATCHED AND tgt.RowChkSum <> src.RowChkSum THEN
        UPDATE SET col1 = src.col1, col2 = src.col2, ...  (update changed rows)
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (dsID, SiteCode, dsClt, ...)                (insert new rows)

6. Final TRUNCATE
   sm.ExeSqlCmd("Truncate Table stg.tbl_dartssrv", sm.ConnectionString)
   Cleans the staging table after the merge is complete.

7. Return RCodes
   RCodes.IsResult = true/false (success/failure)
   RCodes.RowsProcessed = sum of rows affected by all merge stored procedures
   RCodes.ExceptMsg = exception message if error occurred

Why BulkDartsSvc is faster than EF Core
- SqlBulkCopy sends all rows in a single network batch to SQL Server's bulk insert engine.
- No row-by-row INSERT or UPDATE is issued from C# code.
- The MERGE logic runs inside SQL Server itself — no round-trips for each row.
- A clinic with 5,000 rows processes in seconds; EF Core would take minutes.
________________________________________

11. SaveDartsSrvs — The EF Core Path (Backfill and Historical Load)

File: BCAppCode/BHG-DR-LIB/SaveDartsSrvs.cs
Class: SaveData (partial class)

This file contains 10 public methods, one for each destination year table.
All 10 methods are identical in logic. Only the EF Core model class and Azure table name differ.

Method               Writes To                    EF Model Class
------               ---------                    --------------
SaveDartSrv2014()    pats.tbl_DartsSrv_2014B4     TblDartsSrv
SaveDartSrv2015()    pats.tbl_DartsSrv_2015       TblDartsSrv_2015
SaveDartSrv2016()    pats.tbl_DartsSrv_2016       TblDartsSrv_2016
SaveDartSrv2017()    pats.tbl_DartsSrv_2017       TblDartsSrv_2017
SaveDartSrv2018()    pats.tbl_DartsSrv_2018       TblDartsSrv_2018
SaveDartSrv2019()    pats.tbl_DartsSrv_2019       TblDartsSrv_2019
SaveDartSrv2020()    pats.tbl_DartsSrv_2020       TblDartsSrv_2020
SaveDartSrv2021()    pats.tbl_DartsSrv_2021       TblDartsSrv_2021
SaveDartSrv2022()    pats.tbl_DartsSrv_2022       TblDartsSrv_2022
SaveDartSrv2023()    pats.tbl_DartsSrv_2023       TblDartsSrv_2023

Method signature (same for all 10):
    public bool SaveDartSrv2022(
        DataTable tbl,           // rows from SAMMS
        string sc,               // SiteCode e.g. "B01"
        long akey,               // ActionKey (passed from task)
        DateTime wrkdt,          // work date
        BHG_DRContext db)        // EF context (created if null)

EF Core upsert logic — step by step:

Step 1 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load all existing Azure rows for this site
    List<TblDartsSrv_2022> darts = db.TblDartsSrv2022
        .Where(x => x.SiteCode == sc)
        .ToList()

This loads the entire Azure dataset for this clinic's 2022 sessions into memory.

Step 3 — Detect if this is a first-time load (all new)
    if (darts.Count == 0) { AllNewRows = true; }

If Azure has zero rows for this site in this year table, skip all lookups
and create new objects directly. This saves time on initial load.

Step 4 — Loop through every row from SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 5 — Get the new checksum from source
    int newChkSum = int.Parse(r["RowChkSum"].ToString())
    int dsID = int.Parse(r["dsid"].ToString())

Step 6 — Find or create the Azure object
    if (AllNewRows)
    {
        dart = new TblDartsSrv_2022()
        dart.SiteCode = sc
        dart.DsId = dsID
        NewRow = true
    }
    else
    {
        dart = darts.Where(x => x.DsId == dsID).FirstOrDefault()
        if (dart == null)
        {
            dart = new TblDartsSrv_2022()   // new row not yet in Azure
            NewRow = true
        }
    }

Step 7 — Compare checksums and map columns only if changed
    if ((dart.RowChkSum != newChkSum) || NewRow)
    {
        dart.RowChkSum   = newChkSum
        dart.DsClt       = int.Parse(r["DsClt"].ToString())
        dart.DsDim1      = bool.Parse(r["DsDim1"].ToString())
        dart.DsTxtSrv    = r["DsTxtSrv"].ToString()
        dart.DsDtStart   = DateTime.Parse(r["DsDtStart"].ToString())
        // ... all 50+ columns mapped one by one ...
        dart.LastModAt   = DateTime.Now

        // Optional note/signature columns (present only in some ActionKey variants)
        if (tbl.Columns.Contains("dstxtnote"))
        {
            dart.DstxtNote = r["DstxtNote"].ToString()
            dart.DsSignature = r["DsSignature"].ToString()
            // ... signature image bytes ...
        }

        if (NewRow) { DSNew.Add(dart) }  // collect new rows separately
        NewRow = false
    }
    // If checksum matches: skip this row entirely — no write needed

Step 8 — Commit updates (modified existing rows)
    db.SaveChanges()       // EF Core generates UPDATE statements for all modified objects

Step 9 — Commit inserts (new rows)
    if (DSNew.Count > 0)
    {
        db.TblDartsSrv2022.AddRange(DSNew)
    }
    db.SaveChanges()       // EF Core generates INSERT statements for all new objects

When is SaveDartsSrvs used instead of BulkDartsSvc?

- During historical/yearly backfill operations via SelectConstructor
- When developer manually calls specific year methods via bhg.TestCode
- Never during the normal daily Schedule 9 run (daily uses BulkDartsSrvLoader exclusively)

The EF Core path is slower because it:
- Loads all existing Azure rows for the site into memory
- Loops row by row in C#
- Issues individual INSERT/UPDATE SQL statements

But it offers:
- Precise per-row control over what gets written
- No dependency on stored procedures
- Easier to debug individual row failures
________________________________________

12. Destination Tables — All Year Tables (Azure BHG_DR)

Location: Azure SQL Database BHG_DR
Schema  : pats

All year tables have IDENTICAL column schemas. They differ only in table name and the year
range of data they contain.

Table Name                     Year Coverage      EF Model File
----------                     -------------      -------------
pats.tbl_DartsSrv_2014B4       2008 – 2014        TblDartsSrv.cs
pats.tbl_DartsSrv_2015         2015               TblDartsSrv_2015.cs
pats.tbl_DartsSrv_2016         2016               TblDartsSrv_2016.cs
pats.tbl_DartsSrv_2017         2017               TblDartsSrv_2017.cs
pats.tbl_DartsSrv_2018         2018               TblDartsSrv_2018.cs
pats.tbl_DartsSrv_2019         2019               TblDartsSrv_2019.cs
pats.tbl_DartsSrv_2020         2020               TblDartsSrv_2020.cs
pats.tbl_DartsSrv_2021         2021               TblDartsSrv_2021.cs
pats.tbl_DartsSrv_2022         2022               TblDartsSrv_2022.cs
pats.tbl_DartsSrv_2023         2023               TblDartsSrv_2023.cs
pats.tbl_DartsSrv_2024         2024               TblDartsSrv_2024.cs

Primary Key for all tables: dsID + SiteCode (composite — dsID is unique within a clinic only)

Complete column listing for all destination tables:

Column Name (C# Property)    SQL Column Name         Type                Notes
-------------------------    ---------------         ----                -----
DsId                         dsID                    int                 PK Part 1
DsClt                        dsClt                   int                 Patient/client ID
SiteCode                     SiteCode                varchar(25)         PK Part 2 — clinic code
RowChkSum                    RowChkSum               int                 Change detection hash
DsDim1                       dsDIM1                  bit (nullable)      Clinical dimension flag 1
DsDim2                       dsDIM2                  bit (nullable)      Clinical dimension flag 2
DsDim3                       dsDIM3                  bit (nullable)      Clinical dimension flag 3
DsDim4                       dsDIM4                  bit (nullable)      Clinical dimension flag 4
DsDim5                       dsDIM5                  bit (nullable)      Clinical dimension flag 5
DsDim6                       dsDIM6                  bit (nullable)      Clinical dimension flag 6
DsTxtSrv                     dsTxtSrv                varchar(100)        Service description text
DsDtStart                    dsDtStart               datetime            Session start date/time
DsDtEnd                      dsDtEnd                 datetime            Session end date/time
DsTxtType                    dsTxtType               varchar(50)         Service type code
DsdblUnits                   dsdblUnits              float               Units of service (hours)
DsNoteId                     dsNoteID                int (nullable)      Linked note record ID
DsDtAdded                    dsDtAdded               datetime            Record created date
DstxtStaff                   dstxtStaff              varchar(100)        Delivering staff name
DstxtNote                    dstxtNote               ntext               Full counseling note text
DsRtbnote                    dsRTBNOTE               ntext               RTB note text
Dsbilled                     DSbilled                datetime            Billing date
DsGroupnum                   dsGROUPNUM              varchar(50)         Group session number
DsProgram                    dsPROGRAM               varchar(50)         Program type
DsUpdate                     dsUpdate                datetime            Last update date
DsUpdatestaff                dsUPDATEStaff           varchar(50)         Last updating staff
DsInvalidatedOn              dsInvalidatedOn         datetime            Void/invalidation date
DsError                      dsError                 varchar(4000)       ETL or record error msg
DsTxtHiv                     dsTxtHIV                varchar(50)         HIV program identifier
DsDartsGroup                 dsDartsGroup            int (nullable)      Group session key
RepOldSrv                    repOldSrv               numeric(18,0)       Legacy service code
DsSignature                  dsSignature             ntext               Counselor signature
DsSigDate                    dsSigDate               datetime            Counselor sign date
DssigdateCosign              dssigdateCOSIGN         datetime            Co-signer sign date
DssignatureCosign            dssignatureCOSIGN       ntext               Co-signer signature
DsSigUser                    dsSigUser               varchar(50)         Signing counselor username
DsSigUserCosign              dsSigUserCosign         varchar(50)         Co-signing username
DsSigclt                     dsSIGCLT                ntext               Client signature
DsSigcltdate                 dsSIGCLTDATE            datetime            Client sign date
DsSigcltuser                 dsSIGCLTUSER            varchar(50)         Client signature username
DsAptid                      dsAPTID                 int (nullable)      Linked appointment ID
Dsuncharted                  dsuncharted             bit (nullable)      Uncharted session flag
DsTxDim1                     dsTxDim1                int (nullable)      Treatment dimension 1
DsTxDim2                     dsTxDim2                int (nullable)      Treatment dimension 2
DsTxDim3                     dsTxDim3                int (nullable)      Treatment dimension 3
DsTxDim4                     dsTxDim4                int (nullable)      Treatment dimension 4
DsTxDim5                     dsTxDim5                int (nullable)      Treatment dimension 5
DsTxDim6                     dsTxDim6                int (nullable)      Treatment dimension 6
DsDiag                       dsDIAG                  varchar(100)        ICD-9 diagnosis code
DsArea                       dsArea                  varchar(100)        Treatment area
DsGroupDefaultNote           dsGroupDefaultNote      bit (nullable)      Group default note flag
DsGroupEnd                   dsGroupEnd              datetime            Group session end
DsGroupIdentity              dsGroupIdentity         int (nullable)      Group identity ID
DsGroupStart                 dsGroupStart            datetime            Group session start
DsDiag10                     dsDIAG10                varchar(100)        ICD-10 diagnosis code
SiteId                       SiteID                  int (nullable)      SAMMS internal site ID
DsDbnotes                    dsDBnotes               varchar(250)        Admin/DB notes
DsSigCltImg                  dsSigCltImg             varbinary(max)      Client signature image
DsSignatureCoSignImg         dsSignatureCoSignImg    varbinary(max)      Co-signer signature image
DsSignatureImg               dsSignatureIMG          varbinary(max)      Counselor signature image
Mg                           MG                      float               Milligrams (MAT programs)
LastModAt                    LastModAt               datetime            ETL last write timestamp
________________________________________

13. Change Detection — RowChkSum

The RowChkSum column is the heart of this ETL's efficiency.

How it is computed (at source, during SELECT):

    RowChkSum = CHECKSUM(
        dsID, dsClt, dsDIM1, dsDIM2, dsDIM3, dsDIM4, dsDIM5, dsDIM6,
        dsTxtSrv, dsDtStart, dsDtEnd, dsTxtType, dsdblUnits, dsNoteID,
        dsDtAdded, dstxtStaff, DSbilled, dsGROUPNUM, dsPROGRAM,
        dsUpdate, dsUPDATEStaff, dsInvalidatedOn, dsError, dsTxtHIV,
        dsDartsGroup, repOldSrv, dsSigDate, dssigdateCOSIGN,
        dsSigUser, dsSigUserCosign, dsSIGCLTDATE, dsAPTID,
        dsuncharted, dsTxDim1 ... dsTxDim6,
        dsDIAG, dsArea, dsGroupDefaultNote, dsGroupEnd,
        dsGroupIdentity, dsGroupStart, dsDIAG10, SiteID, dsDBnotes, MG
    )

How it is used:

Bulk path (stg merge stored procedures):
    WHEN MATCHED AND tgt.RowChkSum <> src.RowChkSum THEN UPDATE ...
    Only matched rows where the hash changed are updated.

EF Core path (SaveDartsSrvs methods):
    if (dart.RowChkSum != newChkSum) || NewRow) { ... map all columns ... }
    Only rows where the hash changed (or is new) get their columns mapped and written.

What this means in practice:
- A clinic that had 10,000 sessions but only 50 updated today will generate 50 UPDATEs.
- The other 9,950 rows are bulk-loaded into staging, the merge sees matching checksums,
  and those rows are left untouched in the final year table.
- This makes the ETL fast even for large clinics.
________________________________________

14. Load Design Summary

Load type: Incremental upsert with checksum-based change detection

Per run behavior:
1. Only records modified within the lookback window are fetched from SAMMS
2. All fetched records are bulk-loaded into the staging table
3. Merge stored procedures compare RowChkSum values:
   - Row exists in Azure with same checksum   → no action (skip)
   - Row exists in Azure with different checksum → UPDATE
   - Row does not exist in Azure              → INSERT
4. Staging table is truncated after merge

Per-session identity:
Each DartsSrv row is identified by the composite key (dsID + SiteCode).
dsID is only unique within a single clinic. The SiteCode column (added by the ETL,
not present in SAMMS) makes the key globally unique across the entire warehouse.

Date window behavior:

Scenario                         Lookback Applied
--------                         ----------------
Normal weekday run               -15 days from WorkDate
Last Friday of any month         -90 days from WorkDate
Special date override (1/24/25)  -200 days from WorkDate

The wider windows on certain dates ensure that billing corrections and late signatures
from previous months are captured and not missed by the daily incremental logic.
________________________________________

15. Error Handling and Recovery

BulkDartsSvc error handling:

    try
    {
        bc.WriteToServer(tbl)
        // ... exec merge stored procedures ...
    }
    catch (Exception ex)
    {
        rst.IsResult = false
        Console.WriteLine(ex.Message)
        rst.ExceptMsg = ex.Message
        rst.ExceptInnerMsg = ex.InnerException.Message
    }
    finally
    {
        bc.Close()
    }

If the bulk copy fails:
- The exception message is captured in RCodes.ExceptMsg
- RCodes.IsResult is set to false
- The caller (BHGTaskRunner) records the error in task.ErrorMessage
- The task status is set to Status=19 (error) or 20 (complete with error)
- The staging table is left un-truncated (manual cleanup may be required)

SaveDartsSrvs error handling:

    try
    {
        // ... all EF Core upsert logic ...
        db.SaveChanges()
    }
    catch (Exception e)
    {
        res = false
        Console.WriteLine(e.Message)
    }
    return res

If an EF Core exception occurs, the entire batch for that site/year is rolled back.
The method returns false. The caller records the error.

Recovery behavior:

If a task fails, the Scheduler's daily reset restores it to Status=17 (ready) on the next run:
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

This means a failed DartsSrv run for a clinic will automatically be retried the next day.
________________________________________

16. RowTrax — Audit and Row Count Tracking

Table: tsk.tbl_RowTrax (Azure BHG_DR)

After each successful DartsSrv load for a clinic, if the task has RowTrax enabled:

    sd.SaveRowTrax(
        st.SiteCode,           -- e.g. "B01"
        st.WorkDate,           -- today
        st.TaskName,           -- "pats.tbl_dartssrv"
        sourceCount,           -- count from SAMMS
        destCount,             -- count in Azure
        null)

Source count query (run against SAMMS):
    select count(1)
    from dbo.tblDartsSrv
    where dsClt > 0
    and (dsDtAdded > '12/31/2018' or dsDtStart = '12/31/2018')

Note: The hardcoded cutoff '12/31/2018' means records added before 2019 are excluded from
the source count. This is intentional — only active modern records are compared.

Destination count query (run against Azure):
    select count(1)
    from pats.vw_DartsSrv
    where SiteCode = 'B01'

pats.vw_DartsSrv is a view that UNIONS all year tables together for easy querying.

The stored RowTrax record allows analysts to compare source vs destination row counts
over time and identify clinics where data may be drifting or falling behind.
________________________________________

17. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-DartSvc (Status=17)
        |-- insert child tasks: pats.tbl_DartsSrv x 80 clinics (Status=17)
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2 (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 9
        |
        |-- filter: TaskName = 'SAMMS-ETL-DartSvc', SiteCode != 'PHC'
        |-- for each parent task:
        |       mark ptask.Status = 18 (running)
        |
        |-- for each child task (one per clinic):
        |       get column mappings from dms.tbl_MapSrc2Dsn (ActionKey=9)
        |       SelectConstructor.GetSLT() → builds SELECT field list + CHECKSUM()
        |       check if ServiceType column exists in SAMMS → strip if absent
        |       calculate DartsDate (WorkDate - 15/90/200 days)
        |       build WHERE clause across 5 date columns
        |
        V
SQLSvrManager.GetTableData()
        |
        | executes full SELECT against clinic SQL Server
        | connection from ctrl.tbl_LocationCons for this SiteCode
        |
        V
DataTable (in memory — rows from SAMMS dbo.tblDartsSrv)
        |
        V
BulkDartsSvc.BulkDartsSrvLoader()  [PRIMARY — daily path]
        |
        |-- TRUNCATE stg.tbl_DartsSrv
        |-- SqlBulkCopy.WriteToServer(DataTable)
        |       → all rows bulk-inserted into stg.tbl_DartsSrv
        |
        |-- exec stg.DartsSrvMerge    → MERGE into pats.tbl_DartsSrv_2014B4
        |-- exec stg.DartsSrvMerge22  → MERGE into pats.tbl_DartsSrv_2022
        |-- exec stg.DartsSrvMerge23  → MERGE into pats.tbl_DartsSrv_2023
        |-- exec stg.DartsSrvMerge24  → MERGE into pats.tbl_DartsSrv_2024
        |-- exec stg.DartsSrvMerge25  → MERGE into pats.tbl_DartsSrv_2025
        |-- exec stg.DartsSrvMerge26  → MERGE into pats.tbl_DartsSrv_2026
        |-- exec stg.DartsSrvMerge27  → MERGE into pats.tbl_DartsSrv_2027
        |-- exec stg.DartsSrvMerge28  → MERGE into pats.tbl_DartsSrv_2028
        |
        |       Inside each MERGE:
        |           WHEN MATCHED AND RowChkSum changed → UPDATE
        |           WHEN NOT MATCHED               → INSERT
        |           WHEN MATCHED AND same checksum  → (nothing)
        |
        |-- TRUNCATE stg.tbl_DartsSrv (cleanup)
        |
        V
pats.tbl_DartsSrv_20XX (Azure BHG_DR)  [FINAL DESTINATION]
        |
        |-- rows updated or inserted for this clinic + this year
        |
        V
RowTrax audit saved to tsk.tbl_RowTrax
        |
        V
BHGTaskRunner marks task Status=20 (complete)

  ---- OR (historical backfill / year-specific path) ----

SelectConstructor (when WrkYear is specified)
        |
        | case "tbl_dartssrv": switch (WrkYear)
        V
SaveDartsSrvs.SaveDartSrv202X()  [EF CORE PATH — backfill only]
        |
        |-- load all Azure rows for this SiteCode into memory
        |-- for each SAMMS row:
        |       if AllNewRows: create new EF object
        |       else: lookup existing EF object by dsID
        |       if RowChkSum changed or new row:
        |           map all 50+ columns
        |           add to DSNew list or modify existing object
        |-- db.SaveChanges()         → commits UPDATEs
        |-- db.AddRange(DSNew)       → queues INSERTs
        |-- db.SaveChanges()         → commits INSERTs
        |
        V
pats.tbl_DartsSrv_20XX (Azure BHG_DR)  [FINAL DESTINATION]
________________________________________

18. File Reference Map

File Path                                   Purpose
---------                                   -------
BCAppCode/Scheduler/Program.cs              Creates daily task queue for all ETL pipelines
BCAppCode/BHGTaskRunner/Program.cs          Main ETL driver (arg=9 → DartsSrv pipeline)
BCAppCode/BHG-DR-LIB/SelectConstructor.cs  Builds SELECT + CHECKSUM() from metadata
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs      ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs       SqlBulkCopy + merge SPs (daily path)
BCAppCode/BHG-DR-LIB/SaveDartsSrvs.cs      EF Core upsert — 10 year methods (backfill path)
BCAppCode/BHG-DR-LIB/SaveDartsSrv.cs       Original single-table EF Core method (legacy)
BCAppCode/BHG-DR-LIB/SaveDartsSrvs-old.cs  Previous version of SaveDartsSrvs (reference only)
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv.cs              EF Model → pats.tbl_DartsSrv_2014B4
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2015.cs         EF Model → pats.tbl_DartsSrv_2015
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2016.cs         EF Model → pats.tbl_DartsSrv_2016
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2017.cs         EF Model → pats.tbl_DartsSrv_2017
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2018.cs         EF Model → pats.tbl_DartsSrv_2018
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2019.cs         EF Model → pats.tbl_DartsSrv_2019
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2020.cs         EF Model → pats.tbl_DartsSrv_2020
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2021.cs         EF Model → pats.tbl_DartsSrv_2021
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2022.cs         EF Model → pats.tbl_DartsSrv_2022
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2023.cs         EF Model → pats.tbl_DartsSrv_2023
BCAppCode/BHG-DR-LIB/Models/TblDartsSrv_2024.cs         EF Model → pats.tbl_DartsSrv_2024
BCAppCode/BHG-DR-LIB/Models/TblDartsSrvStg.cs           EF Model → stg.tbl_DartsSrv (staging)
BCAppCode/BHG/bhg.TestCode/Program.cs       Developer test harness — calls Save methods directly
________________________________________

19. Quick Reference Summary

What triggers DartsSrv?     Scheduler.exe creates tasks, BHGTaskRunner.exe 9 processes them
Source system?              SAMMS SQL Server at each clinic — dbo.tblDartsSrv
How many clinics?           80+ active clinics (one child task per clinic per run)
Primary load method?        BulkDartsSvc — SqlBulkCopy + stg.DartsSrvMergeXX stored procedures
Backfill load method?       SaveDartsSrvs — EF Core row-by-row upsert per year method
How is change detected?     RowChkSum = CHECKSUM() across all 50+ columns
Normal lookback window?     -15 days from WorkDate
Month-end Friday window?    -90 days from WorkDate
Special override window?    -200 days (1/24/2025 only)
Why 5 date fields in WHERE? Session may be billed or signed days after it occurred
Why year-partitioned?       Volume (~4M rows total) — partitioning keeps queries fast
Staging table?              stg.tbl_DartsSrv — temporary, truncated before and after each load
Destination tables?         pats.tbl_DartsSrv_2014B4 through pats.tbl_DartsSrv_2024
Primary key?                dsID + SiteCode (composite — dsID is not globally unique)
Audit logging?              tsk.tbl_RowTrax — source count vs destination count per site
Error recovery?             Scheduler resets failed tasks to Status=17 on next daily run
PHC handled here?           No — PHC uses its own runner (PHC/Program.cs)
