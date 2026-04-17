
3rd Party Eligibility & Insurance Operations ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract third-party insurance
eligibility, provider setup, claim notes, and accounts receivable (AR) notes from local SAMMS
SQL Server databases at each clinic and load them into the central Azure SQL data warehouse
(BHG_DR).

The goal of this document is to explain:
- What 3rd Party Eligibility data is and why it exists
- What the four save methods in Save3pElig.cs do and how they differ
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How SelectConstructor builds the source SQL
- How SQLSvrManager executes against the clinic databases
- What the source tables look like and all their columns
- What the destination tables look like and all their columns
- How change detection works using RowChkSum
- How RowState tracks soft-deleted / active records
- How the batching optimization works in ClaimNote and ARnote
- How RowTrax audit tracking works
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What is Third-Party Eligibility (3pElig)?

A 3pElig record in SAMMS represents an insurance eligibility check event for a patient at a
BHG clinic. Each time a clinic verifies whether a patient's insurance is currently active and
what coverage is in effect, SAMMS creates an eligibility record that captures the result of
that verification — the payer name, eligibility date, response status, electronic status, and
the file path to any returned eligibility document.

The Save3pElig.cs pipeline manages four related tables that together form the complete third-
party billing support dataset:

1. dbo.tbl3pElig (SAMMS)          → Azure tbl_3pElig (or equivalent)
   One row per eligibility check event. Records who checked eligibility, when, against which
   payer, and what the result was.

2. dbo.tbl3psetup (SAMMS)         → Azure Tbl3psetup
   One row per payer/billing configuration per clinic. Stores provider credentials, NPI
   numbers, tax IDs, SFTP credentials, and Medicaid identifiers used when submitting claims
   to a payer on behalf of this clinic.

3. dbo.tbl3pClaimNote (SAMMS)     → Azure Tbl3pClaimNote
   One row per note attached to a claim. Captures workflow notes, tickler reminders, and
   status annotations added by billing staff during the claims lifecycle.

4. dbo.tbl3pArnote (SAMMS)        → Azure Tbl3pArnote
   One row per accounts receivable (AR) note. Records denial follow-up activity, payment
   appeals, and AR workflow annotations linked to specific claim line items.

Why it is important

This dataset forms the billing operations backbone of the BHG data warehouse. It enables:
- Tracking insurance eligibility verification history per patient per clinic
- Centralized access to provider billing configuration (NPI, Tax ID, SFTP) per site
- Claims workflow visibility through claim notes and ticklers
- AR follow-up tracking and denial management through AR notes

Load type

All four methods use the EF Core upsert path. There is no bulk (SqlBulkCopy) path for these
tables. Each method uses one of two structural patterns:

Pattern A — RowState soft-delete with checksum guard (Save3pElig):
  Loads existing Azure rows into memory, resets all to RowState=false, then re-activates each
  row found in source. Only maps data columns when RowChkSum changed.

Pattern B — Dynamic column switch with batched insert/update (Save3pSetup, Save3pClaimNote,
  Save3pArnote):
  Loops through DataTable columns using a switch statement, builds a new model object per row,
  then matches by primary key to decide INSERT vs UPDATE. New rows are collected in a list and
  bulk-added with AddRange after all updates are committed.
________________________________________

3. Systems Involved

System / File                        Role
-----------                          ----
tsk.tbl_Schedule (Azure DB)          Configuration — defines schedules and their run times
Scheduler.exe                        Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=8              Main ETL orchestrator for SAMMS-ETL-INV (Insurance/Billing)
dms.tbl_MapSrc2Dsn (Azure DB)        Metadata — defines which columns to SELECT for each ActionKey
SelectConstructor.cs                 Assembles SELECT statement from metadata
SQLSvrManager.cs                     Fires SELECT against the clinic SAMMS SQL Server
Save3pElig.cs / SaveData             EF Core upsert class — all four 3p methods live here
ctrl.tbl_LocationCons (Azure)        Connection strings for each clinic's SAMMS SQL Server
tsk.tbl_RowTrax (Azure)              Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How 3p Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily (typically overnight or early morning) and populates the task
queue for all ETL pipelines. It does NOT move data — it only creates tasks.

What the Scheduler does for SAMMS-ETL-INV (Insurance/Billing)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Tasks from previous days that were never picked up are marked as expired (RowState=26).

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For the Insurance/Billing schedule:
    Name        = 'SAMMS-ETL-INV'
    ActionKey   = 8
    ScheduleId  = 8
    NextRunTime = (calculated next run datetime)

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'SAMMS-ETL-INV'
    SiteCode = 'All'
    Status   = 17
    WorkDate = today
    RunAt    = NextRunTime

Step 4 — Insert child task rows (one per clinic per table)
Using a cross join of dms.vw_MapAction and tsk.tbl_Tasks2, the Scheduler inserts child
task rows for each clinic and each destination table (including the 3p tables):

    insert into tsk.tbl_Tasks2(ParentTaskId, TaskName, ...)
    select t.TaskId,
           ma.DsnSchema + '.' + ma.DsnTbl,  -- e.g. 'ins.tbl_3pElig'
           ma.ActionKey,                     -- = 8
           ma.StepKey,
           ma.SiteCode                       -- = 'B01', 'VBRA', etc.
    from dms.vw_MapAction ma
    cross join tsk.tbl_Tasks2 t
    where ma.Enabled = 1
      and ma.IsActive = 1
      and case when ma.DsnSchema + '.' + ma.DsnTbl in
               ('ins.tbl_3pelig', 'ins.tbl_3psetup', 'ins.tbl_3pclaimnote', 'ins.tbl_3parnote')
               then 'SAMMS-ETL-INV' end = t.TaskName

This produces approximately 80+ child rows per table (one per active clinic per table type).

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
        TaskName = 'SAMMS-ETL-INV'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'ins.tbl_3pelig'
        SiteCode = 'VBRA'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'ins.tbl_3psetup'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'ins.tbl_3pclaimnote'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'ins.tbl_3parnote'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How 3p Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 8 to process the SAMMS-ETL-INV schedule, which
includes Claims AND all four 3p Insurance tables.

Command:   BHGTaskRunner.exe 8

Step 1 — Filter task queue for Schedule 8
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"           // PHC uses a separate runner
        && x.Status == 17             // ready to run
        && x.TaskName == "SAMMS-ETL-INV"
        && x.RunAt < DateTime.Now)    // time has passed

Step 2 — Mark parent task as running
For each parent task found:
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)

Step 4 — For each child task (each clinic + table), get the column mapping
    db.WorkToDo.Where(x => x.Enabled
                        && x.ActionKey == st.ActionKey        // = 8
                        && x.ActionStepKey == st.ActionStepKey)

Returns the list of column mappings from dms.tbl_MapSrc2Dsn for ActionKey=8.

Step 5 — Build the SELECT statement
SelectConstructor.GetSLT() assembles the SELECT field list by:
- Reading all enabled column mappings for the specific ActionKey/StepKey
- Building a CHECKSUM(...) expression across all mapped columns to produce RowChkSum
- Replacing placeholder tokens (@SiteCode, @WorkDate, @Samms)

Step 6 — Build the WHERE clause
The WHERE clause filters by date using DaysBack (varies by table — eligibility and claim
notes typically use a short rolling window):

    strCmd += " where " + strWhere + " " + st.SortOrder;

Step 7 — Execute SELECT against SAMMS
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);

Returns a DataTable with all recently touched rows from this clinic for the specific table.

Step 8 — Route to the appropriate Save method
The routing is performed by SelectConstructor or directly in BHGTaskRunner based on TaskName:

    case "ins.tbl_3pelig" (or equivalent):
        rCodes = sd.Save3pElig(SrcDt, st.SiteCode, WorkDate.AddDays(DaysBack), true/false, null)

    case "ins.tbl_3psetup":
        rCodes = sd.Save3pSetup(SrcDt, st.SiteCode, WorkDate, false, null)

    case "ins.tbl_3pclaimnote":
        rCodes = sd.Save3pClaimNote(SrcDt, st.SiteCode, WorkDate, false, null)

    case "ins.tbl_3parnote":
        rCodes = sd.Save3pArnote(SrcDt, st.SiteCode, WorkDate, false, null)

All four methods are EF Core upsert paths. There is no Bulk / SqlBulkCopy path for these
tables.

Step 9 — RowTrax audit (if enabled for this task)
If st.RowTrax == true:

    Source count:  SrcDt.Rows.Count  (rows returned from SAMMS)

    Destination count:
        Select count(1) from [destination table]
        where SiteCode = 'VBRA' and RowState = 1   (where applicable)

    These counts are saved to tsk.tbl_RowTrax.

Step 10 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. SelectConstructor — How the SELECT Is Built

File: BCAppCode/BHG-DR-LIB/SelectConstructor.cs

SelectConstructor.GetSLT() is called for every child task. It reads the column metadata from
dms.tbl_MapSrc2Dsn (exposed through db.WorkToDo) and assembles the SELECT field list.

For 3pElig tables, the SELECT looks conceptually like this (example for tbl3pElig):

    Select
        SiteCode = 'VBRA',
        eid,
        eclt,
        epayer,
        edate,
        estaff,
        epost,
        eresponse,
        estatus,
        eformat,
        filepath,
        eelecstatus,
        estaffstatus,
        estaffnote,
        escan,
        eorigid,
        pyeligcheck,
        RowChkSum = CHECKSUM(eid, eclt, epayer, edate, estaff, epost, eresponse, estatus,
                             eformat, filepath, eelecstatus, estaffstatus, estaffnote,
                             escan, eorigid, pyeligcheck)
    from dbo.tbl3pElig
    where edate >= DATEADD(day, -14, GETDATE())   -- (example lookback)

The CHECKSUM() expression across all mapped columns produces the RowChkSum used for change
detection. SelectConstructor also routes the table name to the correct Save* method when
called via the backfill path.
________________________________________

7. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL Server,
executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'VBRA'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_VBRA;User Id=...;Password=...;'

The DataTable returned contains all rows from the source table for this clinic that match the
WHERE clause. This DataTable is passed directly into the appropriate Save3p* method.
________________________________________

8. Source Tables — SAMMS SQL Server (dbo)

All four source tables live in the clinic's local SAMMS SQL Server database under the dbo
schema.

________________________________________
8a. dbo.tbl3pElig — Eligibility Check Records

Primary Key: eid (unique per clinic)

Column Name         Type            Description
-----------         ----            -----------
eid                 int             Unique eligibility check ID within this clinic
eclt                int             Patient/client ID this eligibility check belongs to
epayer              varchar(?)      Name of the insurance payer verified
edate               datetime        Date the eligibility check was performed
estaff              varchar(?)      Username of the staff member who performed the check
epost               varchar(?)      Post status or batch reference
eresponse           varchar(?)      Raw or summarized response received from the payer
estatus             varchar(?)      Result status (e.g. 'Active', 'Inactive', 'Error')
eformat             varchar(?)      Format of the eligibility document (e.g. '271', 'PDF')
filepath            varchar(?)      File system or storage path to the returned elig document
eelecstatus         varchar(?)      Electronic transaction status code
estaffstatus        varchar(?)      Staff-assigned review status of the eligibility result
estaffnote          varchar(?)      Free-text note added by staff reviewing the elig result
escan               varchar(?)      Scan reference or indicator
eorigid             int (nullable)  Original/parent eligibility ID for re-check or resubmit
pyeligcheck         datetime (null) Date of prior year eligibility check (conditional parse)
SiteCode            varchar(25)     Added by ETL — clinic identifier (e.g. 'VBRA')
RowChkSum           int             Computed by ETL: CHECKSUM() across all mapped columns

Note: RowChkSum is NOT a column in dbo.tbl3pElig. It is computed during SELECT by
SelectConstructor using SQL Server's CHECKSUM() function.
________________________________________

8b. dbo.tbl3psetup — Third-Party Payer/Provider Setup

Primary Key: 3pid or pid (mapped to _pId in EF model, unique per clinic per payer config)

Column Name         Type            Description
-----------         ----            -----------
3pid / pid          int             Unique payer setup ID within this clinic
SiteCode            varchar(25)     Added by ETL — clinic identifier
Clinic              varchar(?)      Clinic display name for this billing configuration
Address             varchar(?)      Clinic billing address
City                varchar(?)      Clinic billing city
State               varchar(?)      Clinic billing state
Zip                 varchar(?)      Clinic billing zip code
Npi                 varchar(?)      Clinic NPI (National Provider Identifier) number
TaxId               varchar(?)      Federal tax ID used for billing
Medicaid            varchar(?)      Medicaid provider ID
Drlname             varchar(?)      Prescribing doctor last name
Drfname             varchar(?)      Prescribing doctor first name
Drnpi               varchar(?)      Prescribing doctor NPI
ProviderAddress     varchar(?)      Rendering provider address
ProviderCity        varchar(?)      Rendering provider city
ProviderName        varchar(?)      Rendering provider full name
ProviderPhone       varchar(?)      Rendering provider phone
ProviderState       varchar(?)      Rendering provider state
ProviderZip         varchar(?)      Rendering provider zip
SiteId              int (nullable)  SAMMS internal site numeric ID (-1 if blank)
Clia                varchar(?)      CLIA (Clinical Laboratory Improvement Amendments) number
StrDbnotes          varchar(?)      Administrative/database notes
ProviderDesc        varchar(?)      Description of the rendering provider type
BlHasPreloader      bool (nullable) Whether this payer config uses a preloader (false if blank)
IndividualNpi       bool (nullable) Whether individual NPI is used instead of group NPI
Taxonomy            varchar(?)      Provider taxonomy code
Sftpun              varchar(?)      SFTP username for electronic claim submission
Sftppw              varchar(?)      SFTP password for electronic claim submission
RowChkSum           int             Computed by ETL: CHECKSUM() across all mapped columns

Note: The column name for the primary key is "3pid" or "pid" depending on the SAMMS version.
The switch case handles both: case "3pid": and case "pid": both map to psetup._pId.
________________________________________

8c. dbo.tbl3pClaimNote — Claim Notes

Primary Key: tpcntpcid (links each note to its parent claim, unique per clinic)

Column Name                 Type            Description
-----------                 ----            -----------
tpcn                        int             Unique claim note ID within this clinic
tpcntpcid                   int             Parent claim ID (links to tbl3pClaim.tpcID)
tpcndtmadded                datetime (null) Date/time the note was added
tpcnstradded                varchar(?)      Username who added the note
tpcnstrnote                 varchar(?)      Note text content
tpcnstrtype                 varchar(?)      Note type/category code
tpcndttickler               datetime (null) Tickler (follow-up reminder) date
tpcndtticklerremoved        varchar(?)      Date the tickler was removed (stored as string)
tpcnstrticklerremovednote   varchar(?)      Reason note for removing the tickler
tpcnstrticklerremoveduser   varchar(?)      Username who removed the tickler
tpcnstrticklertype          varchar(?)      Type of tickler created
globalbatchid               int (nullable)  Batch processing ID for grouped operations
SiteCode                    varchar(25)     Added by ETL — clinic identifier
RowChkSum                   int             Computed by ETL: CHECKSUM() across all mapped columns

Note: RowChkSum is NOT stored in the source table. It is computed by SelectConstructor.

Load scope: Azure rows loaded with TpcnDtmAdded >= '1/1/2023' (hard-coded cutoff).
________________________________________

8d. dbo.tbl3pArnote — Accounts Receivable Notes

Primary Key: arnid (unique per clinic)

Column Name             Type            Description
-----------             ----            -----------
arnid                   int             Unique AR note ID within this clinic
arnliid                 int (nullable)  Parent claim line item ID (links to tbl3pClaimLineItem)
arndate                 datetime (null) Date the AR note was created
arndtremoved            datetime (null) Date the AR note was removed/resolved
arnnote                 varchar(?)      Free-text AR note content
arnuser                 varchar(?)      Username who created the AR note
arnstrremovedreason     varchar(?)      Reason text for removing/resolving the AR note
arnstrremoveduser       varchar(?)      Username who removed/resolved the AR note
bid                     int (nullable)  Linked billing batch or remittance ID
arndbnotes              varchar(?)      Administrative/database notes
globalbatchid           int (nullable)  Batch processing ID for grouped operations
SiteCode                varchar(25)     Added by ETL — clinic identifier
RowChkSum               int             Computed by ETL: CHECKSUM() across all mapped columns

Note: RowChkSum is NOT stored in the source table. It is computed by SelectConstructor.

Load scope: Azure rows loaded with ArnDate >= wrkdt.AddDays(-10) (rolling 10-day window).
________________________________________

9. Save3pElig — EF Core Path (Pattern A: RowState Soft-Delete)

File: BCAppCode/BHG-DR-LIB/Save3pElig.cs
Class: SaveData (partial class)
Method: Save3pElig()

Method signature:
    public RCodes Save3pElig(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (year boundary for load scope)
        bool yearly,             // controls load scope (both branches currently identical)
        BHG_DRContext db)        // EF context (created if null)

This method uses Pattern A: soft-delete reset before looping, RowState tracks active records.

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count }
    (Note: RowsIns and RowsUpd are NOT tracked in this method — only RowsProcessed)

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load existing Azure rows for this site scoped by year
Both the yearly=true and yearly=false branches execute the same query:

    Eligs = db.Tbl3pElig
        .Where(x => x.EDate.Value.Year >= wrkdt.Year && x.SiteCode == sc)
        .ToList()

This loads all eligibility records for this site where the eligibility date is in the current
year or later. The yearly parameter is accepted but does not currently change behavior — both
branches produce the same result. This is a placeholder for future divergence.

Step 4 — Soft-reset all loaded Azure rows to RowState = false
    foreach (Tbl3pElig el in Eligs)
    {
        el.RowState = false;
    }

This marks every Azure row in scope as "inactive." The assumption is that any row not
re-activated by the SAMMS data in this run is no longer active at the source.

Step 5 — Loop through every row from the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 6 — Extract the primary key and new checksum from source
    int eid = int.Parse(r["eid"].ToString())
    int rcs = int.Parse(r["RowChkSum"].ToString())

Step 7 — Find or create the Azure object
    pe = Eligs.Where(x => x.EId == eid).FirstOrDefault()

    if (pe == null):
        pe = new Tbl3pElig {
            SiteCode  = sc,
            EId       = eid,
            RowState  = true,
            RowChkSum = 0      // force checksum mismatch so columns are always mapped on insert
        }
        Eligs.Add(pe)          // add to in-memory list (prevents re-inserting same row)
        db.Tbl3pElig.Add(pe)   // register with EF for INSERT

Step 8 — Compare checksums. Map all columns only if changed or new.

    if (pe.RowChkSum != rcs):
        pe.RowChkSum    = rcs
        pe.LastModAt    = DateTime.Now
        pe.RowState     = true
        pe.EClt         = int.Parse(r["eclt"])
        pe.EPayer       = r["epayer"]
        pe.EDate        = DateTime.Parse(r["edate"])
        pe.EStaff       = r["estaff"]
        pe.EPost        = r["epost"]
        pe.EResponse    = r["eresponse"]
        pe.EStatus      = r["estatus"]
        pe.EFormat      = r["eformat"]
        pe.Filepath     = r["filepath"]
        pe.EElecstatus  = r["eelecstatus"]
        pe.EstaffStatus = r["estaffstatus"]
        pe.EstaffNote   = r["estaffnote"]
        pe.EScan        = r["escan"]

        Conditional integer (only mapped if source is non-empty):
        if (r["eorigid"].ToString().Length > 0):
            pe.EOrigid = int.Parse(r["eorigid"])

        Conditional datetime (only parsed if source string length > 6):
        if (r["pyeligcheck"].ToString().Length > 6):
            pe.Pyeligcheck = DateTime.Parse(r["pyeligcheck"])

    else (RowChkSum unchanged — row not modified):
        pe.RowState = true    // still mark as active; no data column writes

Step 9 — Commit all changes in one batch
    db.SaveChanges()
    // EF Core generates UPDATE for modified objects + INSERT for newly added objects

Why RowState matters here:
At the start of the method, all rows for the current year are set to RowState=false (Step 4).
As the loop processes SAMMS rows, each found or new row is set back to RowState=true.
After SaveChanges(), any eligibility record that existed in Azure but was NOT in the SAMMS
data set remains RowState=false — indicating it was deleted or is no longer active in the
source. This is the soft-delete mechanism for eligibility records.

Key behavioral note on eorigid and pyeligcheck:
These two columns use conditional parsing to handle NULL or empty source values safely:
- eorigid: if the source value is empty string, the property is left at its existing value
  (not overwritten with zero or null). This preserves the original ID linkage.
- pyeligcheck: the length > 6 guard prevents attempting to parse empty or very short strings
  as DateTime, which would throw an exception.
________________________________________

10. Save3pSetup — EF Core Path (Pattern B: Dynamic Switch, Per-Row Commit)

File: BCAppCode/BHG-DR-LIB/Save3pElig.cs
Class: SaveData (partial class)
Method: Save3pSetup()

Method signature:
    public RCodes Save3pSetup(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (not used for scoping in this method)
        bool yearly,             // accepted but not used for branching
        BHG_DRContext db)        // EF context (created if null)

This method uses Pattern B: dynamic column switch to build objects, per-row db.SaveChanges().
It does NOT use RowState soft-delete — setup records are either inserted or updated.

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes {
        IsResult       = true,
        RowsProcessed  = tbl.Rows.Count,
        RowsIns        = 0,
        RowsUpd        = 0
    }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load ALL existing Azure rows for this site (no date filter)
    tblSetup = db.Tbl3psetup.Where(x => x.SiteCode == sc).ToList()

This loads the complete payer/provider configuration set for this clinic. Setup records are
relatively small in volume (typically one row per active payer per clinic) so full load is
appropriate.

Step 4 — Capture execution timestamp
    DateTime execDT = DateTime.Now;

This timestamp is used for LastModAt on all rows in this batch, ensuring all rows in a single
run share the same audit timestamp.

Step 5 — Loop through every row from the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 6 — Build a new model object by iterating all DataTable columns
    Models.Tbl3psetup psetup = new Models.Tbl3psetup();
    foreach (DataColumn c in tbl.Columns)
    {
        switch (c.ColumnName.ToLower()) { ... }
    }

This dynamic approach allows the method to handle any set of columns returned by the SELECT
without hardcoding positional column access. The switch maps source column names (lowercased)
to the corresponding EF model properties.

Column mapping switch cases:

    "sitecode"          → psetup.SiteCode = sc; psetup.LastModAt = execDT
    "3pid" / "pid"      → psetup._pId = int.Parse(value)
    "clinic"            → psetup.Clinic
    "address"           → psetup.Address
    "state"             → psetup.State
    "zip"               → psetup.Zip
    "npi"               → psetup.Npi
    "taxid"             → psetup.TaxId
    "medicaid"          → psetup.Medicaid
    "city"              → psetup.City
    "drlname"           → psetup.Drlname
    "drfname"           → psetup.Drfname
    "drnpi"             → psetup.Drnpi
    "provideraddress"   → psetup.ProviderAddress
    "providercity"      → psetup.ProviderCity
    "providername"      → psetup.ProviderName
    "providerphone"     → psetup.ProviderPhone
    "providerstate"     → psetup.ProviderState
    "providerzip"       → psetup.ProviderZip
    "siteid"            → psetup.SiteId = int.Parse(value) OR -1 if blank
    "clia"              → psetup.Clia
    "strdbnotes"        → psetup.StrDbnotes
    "providerdesc"      → psetup.ProviderDesc
    "blhaspreloader"    → psetup.BlHasPreloader = bool.Parse(value) OR false if blank
    "individualnpi"     → psetup.IndividualNpi = bool.Parse(value) OR false if blank
    "taxonomy"          → psetup.Taxonomy
    "sftpun"            → psetup.Sftpun
    "sftppw"            → psetup.Sftppw
    "rowchksum"         → psetup.RowChkSum = int.Parse(value)

Special handling for SiteId:
    if (value.Length == 0) → SiteId = -1    (sentinel: no site ID available)
    else                   → SiteId = int.Parse(value)

Special handling for BlHasPreloader and IndividualNpi (boolean fields):
    if (value.Length == 0)      → property = false
    else                        → property = bool.Parse(value)
    if (property == null)       → property = false   (additional null guard)

Step 7 — Match against existing Azure row by primary key
    Models.Tbl3psetup dbSetup = tblSetup.FirstOrDefault(x => x._pId == psetup._pId);

Step 8 — INSERT or UPDATE based on match result

    if (dbSetup == null):
        rc.RowsIns += 1
        db.Tbl3psetup.Add(psetup)     // register new record with EF

    else if (dbSetup.RowChkSum != psetup.RowChkSum):
        rc.RowsUpd += 1
        // Copy all fields from new object to existing tracked object
        dbSetup.RowChkSum       = psetup.RowChkSum
        dbSetup.Address         = psetup.Address
        dbSetup.BlHasPreloader  = psetup.BlHasPreloader
        dbSetup.City            = psetup.City
        dbSetup.Clia            = psetup.Clia
        dbSetup.Clinic          = psetup.Clinic
        dbSetup.Drfname         = psetup.Drfname
        dbSetup.Drlname         = psetup.Drlname
        dbSetup.Drnpi           = psetup.Drnpi
        dbSetup.IndividualNpi   = psetup.IndividualNpi
        dbSetup.LastModAt       = psetup.LastModAt
        dbSetup.Medicaid        = psetup.Medicaid
        dbSetup.Npi             = psetup.Npi
        dbSetup.ProviderAddress = psetup.ProviderAddress
        dbSetup.ProviderCity    = psetup.ProviderCity
        dbSetup.ProviderDesc    = psetup.ProviderDesc
        dbSetup.ProviderName    = psetup.ProviderName
        dbSetup.ProviderPhone   = psetup.ProviderPhone
        dbSetup.ProviderState   = psetup.ProviderState
        dbSetup.ProviderZip     = psetup.ProviderZip
        dbSetup.Sftppw          = psetup.Sftppw
        dbSetup.Sftpun          = psetup.Sftpun
        dbSetup.SiteId          = psetup.SiteId
        dbSetup.State           = psetup.State
        dbSetup.StrDbnotes      = psetup.StrDbnotes
        dbSetup.TaxId           = psetup.TaxId
        dbSetup.Taxonomy        = psetup.Taxonomy
        dbSetup.Zip             = psetup.Zip

    else (RowChkSum unchanged):
        // No action — setup record not modified; row is not touched in Azure

Step 9 — Commit after EVERY row (per-row SaveChanges)
    db.SaveChanges()   // called inside the foreach loop

This is the key behavioral difference from Save3pElig, Save3pClaimNote, and Save3pArnote.
Save3pSetup commits after each individual row. This ensures that a failure on row N does not
roll back inserts/updates from rows 1 through N-1. Setup records are low-volume (small number
of payer configs per clinic), so per-row commit overhead is acceptable.
________________________________________

11. Save3pClaimNote — EF Core Path (Pattern B: Dynamic Switch, Batched Insert)

File: BCAppCode/BHG-DR-LIB/Save3pElig.cs
Class: SaveData (partial class)
Method: Save3pClaimNote()

Method signature:
    public RCodes Save3pClaimNote(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (not used for scoping in this method)
        bool yearly,             // accepted but not used for branching
        BHG_DRContext db)        // EF context (created if null)

This method uses Pattern B with a batching optimization: new rows are collected in a separate
list (newCNs) and inserted using db.AddRange() after all updates are committed, rather than
adding them to the EF context during the main loop.

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes {
        IsResult       = true,
        RowsProcessed  = tbl.Rows.Count,
        RowsIns        = 0,
        RowsUpd        = 0
    }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Initialize new-row staging list
    List<Tbl3pClaimNote> newCNs = new List<Tbl3pClaimNote>();

New rows are NOT added to the EF context during the loop. They are staged here and added
after the update pass completes.

Step 4 — Load existing Azure rows scoped to 2023+
    tblCNs = db.Tbl3pClaimNote
        .Where(x => x.SiteCode == sc && x.TpcnDtmAdded >= DateTime.Parse("1/1/2023"))
        .ToList()

Note: The date cutoff is hard-coded to January 1, 2023. This limits the in-memory working
set to notes added in 2023 or later, which is the active monitoring window. Older claim notes
are not re-loaded or re-evaluated on each run.

Step 5 — Capture execution timestamp
    DateTime execDT = DateTime.Now;

Step 6 — Loop through every row from the SAMMS DataTable

Step 7 — Build a new model object by iterating all DataTable columns
    Models.Tbl3pClaimNote claimNote = new Models.Tbl3pClaimNote();
    foreach (DataColumn c in tbl.Columns)
    {
        switch (c.ColumnName.ToLower()) { ... }
    }

Column mapping switch cases:

    "sitecode"                    → claimNote.SiteCode = sc;
                                    claimNote.LastModAt = execDT;
                                    claimNote.RowState = true
    "tpcn"                        → claimNote.Tpcn = int.Parse(value)
    "tpcntpcid"                   → claimNote.TpcnTpcid = int.Parse(value)
    "tpcndtmadded"                → claimNote.TpcnDtmAdded = DateTime.Parse(value)
                                    (conditional: only if value.Length > 6)
    "tpcnstradded"                → claimNote.TpcnStrAdded
    "tpcnstrnote"                 → claimNote.TpcnStrNote
    "tpcnstrtype"                 → claimNote.TpcnStrType
    "tpcndttickler"               → claimNote.TpcnDtTickler = DateTime.Parse(value)
                                    (conditional: only if value.Length > 6)
    "tpcndtticklerremoved"        → claimNote.TpcnDtTicklerRemoved (stored as string)
    "tpcnstrticklerremovednote"   → claimNote.TpcnStrTicklerRemovedNote
    "tpcnstrticklerremoveduser"   → claimNote.TpcnStrTicklerRemovedUser
    "tpcnstrticklertype"          → claimNote.TpcnStrTicklerType
    "globalbatchid"               → claimNote.GlobalBatchId = int.Parse(value)
                                    (conditional: only if value.Length > 0)
    "rowchksum"                   → claimNote.RowChkSum = int.Parse(value)

Step 8 — Match against existing Azure row by TpcnTpcid (claim ID)
    Models.Tbl3pClaimNote dbclaimNote =
        tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid);

The lookup key is TpcnTpcid (the parent claim ID), not Tpcn (the note ID itself). This means
the match finds the note associated with a given claim, not a specific note row.

Step 9 — INSERT or UPDATE based on match result

    if (dbclaimNote == null):
        rc.RowsIns += 1
        newCNs.Add(claimNote)      // staged for bulk insert, NOT added to EF context yet
        // Note: db.Tbl3pClaimNote.Add(claimNote) is commented out in favor of AddRange pattern

    else if (dbclaimNote.RowChkSum != claimNote.RowChkSum):
        rc.RowsUpd += 1
        dbclaimNote.RowChkSum                 = claimNote.RowChkSum
        dbclaimNote.RowState                  = claimNote.RowState
        dbclaimNote.GlobalBatchId             = claimNote.GlobalBatchId
        dbclaimNote.LastModAt                 = claimNote.LastModAt
        dbclaimNote.TpcnDtmAdded              = claimNote.TpcnDtmAdded
        dbclaimNote.TpcnDtTickler             = claimNote.TpcnDtTickler
        dbclaimNote.TpcnDtTicklerRemoved      = claimNote.TpcnDtTicklerRemoved
        dbclaimNote.TpcnStrAdded              = claimNote.TpcnStrAdded
        dbclaimNote.TpcnStrNote               = claimNote.TpcnStrNote
        dbclaimNote.TpcnStrTicklerRemovedNote = claimNote.TpcnStrTicklerRemovedNote
        dbclaimNote.TpcnStrTicklerRemovedUser = claimNote.TpcnStrTicklerRemovedUser
        dbclaimNote.TpcnStrTicklerType        = claimNote.TpcnStrTicklerType
        dbclaimNote.TpcnStrType               = claimNote.TpcnStrType
        dbclaimNote.TpcnTpcid                 = claimNote.TpcnTpcid
        // Note: per-row db.SaveChanges() is commented out; batch commit is used instead

    else (RowChkSum unchanged):
        // No action — claim note not modified

Step 10 — Commit all updates in one batch
    db.SaveChanges()
    // All update-tracked EF objects committed in one round trip

Step 11 — Insert all new rows using AddRange
    if (newCNs.Count > 0):
        db.Tbl3pClaimNote.AddRange(newCNs)
        db.SaveChanges()

The two-phase commit (updates first, then inserts) ensures that update conflicts do not
interfere with new row inserts. AddRange generates a single batched INSERT statement, which
is more efficient than individual Add() calls for high-volume note inserts.
________________________________________

12. Save3pArnote — EF Core Path (Pattern B: Dynamic Switch, Batched Insert)

File: BCAppCode/BHG-DR-LIB/Save3pElig.cs
Class: SaveData (partial class)
Method: Save3pArnote()

Method signature:
    public RCodes Save3pArnote(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date — used for rolling 10-day window scope
        bool yearly,             // accepted but not used for branching
        BHG_DRContext db)        // EF context (created if null)

This method uses the same Pattern B + batched insert approach as Save3pClaimNote, but scopes
the Azure load using a rolling 10-day lookback from wrkdt rather than a hard-coded year.

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes {
        IsResult       = true,
        RowsProcessed  = tbl.Rows.Count,
        RowsIns        = 0,
        RowsUpd        = 0
    }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Initialize new-row staging list
    List<Tbl3pArnote> newARs = new List<Tbl3pArnote>();

Step 4 — Load existing Azure rows scoped to rolling 10-day window
    tblARs = db.Tbl3pArnote
        .Where(x => x.SiteCode == sc && x.ArnDate >= wrkdt.AddDays(-10))
        .ToList()

The rolling window is dynamic (based on wrkdt), unlike Save3pClaimNote's hard-coded 2023
cutoff. The commented-out alternative (DateTime.Parse("1/1/2023")) shows that the method
previously used the same fixed cutoff before being changed to the rolling window.

The 10-day window limits memory consumption while ensuring that any AR note created or
modified in the past 10 days is available for checksum comparison and update.

Step 5 — Capture execution timestamp
    DateTime execDT = DateTime.Now;

Step 6 — Loop through every row from the SAMMS DataTable

Step 7 — Build a new model object by iterating all DataTable columns
    Models.Tbl3pArnote ar = new Models.Tbl3pArnote();
    foreach (DataColumn c in tbl.Columns)
    {
        switch (c.ColumnName.ToLower()) { ... }
    }

Column mapping switch cases:

    "sitecode"              → ar.SiteCode = sc;
                              ar.LastModAt = execDT;
                              ar.RowState = true
    "arnid"                 → ar.ArnId = int.Parse(value)
    "arnliid"               → ar.ArnLiid = int.Parse(value)
                              (conditional: only if value.Length > 0)
    "arndate"               → ar.ArnDate = DateTime.Parse(value)
                              (conditional: only if value.Length > 6)
    "arndtremoved"          → ar.ArnDtRemoved = DateTime.Parse(value)
                              (conditional: only if value.Length > 6)
    "arnnote"               → ar.ArnNote
    "arnuser"               → ar.ArnUser
    "arnstrremovedreason"   → ar.ArnStrRemovedReason
    "arnstrremoveduser"     → ar.ArnStrRemovedUser
    "bid"                   → ar.Bid = int.Parse(value)
                              (conditional: only if value.Length > 0)
    "arndbnotes"            → ar.ArnDbnotes
    "globalbatchid"         → ar.GlobalBatchId = int.Parse(value)
                              (conditional: only if value.Length > 0)
    "rowchksum"             → ar.RowChkSum = int.Parse(value)

Step 8 — Match against existing Azure row by ArnId
    Models.Tbl3pArnote dbar = tblARs.FirstOrDefault(x => x.ArnId == ar.ArnId);

The lookup key is ArnId — the unique AR note identifier within this clinic.

Step 9 — INSERT or UPDATE based on match result

    if (dbar == null):
        rc.RowsIns += 1
        newARs.Add(ar)         // staged for bulk insert

    else if (dbar.RowChkSum != ar.RowChkSum):
        rc.RowsUpd += 1
        dbar.RowChkSum            = ar.RowChkSum
        dbar.RowState             = ar.RowState
        dbar.GlobalBatchId        = ar.GlobalBatchId
        dbar.LastModAt            = ar.LastModAt
        dbar.ArnDate              = ar.ArnDate
        dbar.ArnDbnotes           = ar.ArnDbnotes
        dbar.ArnDtRemoved         = ar.ArnDtRemoved
        dbar.ArnLiid              = ar.ArnLiid
        dbar.ArnNote              = ar.ArnNote
        dbar.ArnStrRemovedReason  = ar.ArnStrRemovedReason
        dbar.ArnStrRemovedUser    = ar.ArnStrRemovedUser
        dbar.ArnUser              = ar.ArnUser
        dbar.Bid                  = ar.Bid
        // Note: per-row db.SaveChanges() is commented out; batch commit is used

    else (RowChkSum unchanged):
        // No action — AR note not modified

Step 10 — Commit all updates in one batch
    db.SaveChanges()

Step 11 — Insert all new rows using AddRange
    if (newARs.Count > 0):
        db.Tbl3pArnote.AddRange(newARs)
        db.SaveChanges()
________________________________________

13. Destination Tables — Azure BHG_DR

13a. Azure Tbl3pElig (EF Model: Tbl3pElig)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.Tbl3pElig in BHG_DRContext

Primary Key: SiteCode + EId (composite — EId is unique per clinic)

C# Property (EF)    SQL Column Name     Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar(25)         PK Part 1 — clinic code
EId                 eid                 int                 PK Part 2 — eligibility check ID
RowChkSum           RowChkSum           int                 Change detection hash
LastModAt           LastModAt           datetime            ETL last write timestamp
RowState            RowState            bit (nullable)      true=active, false=soft-deleted
EClt                eclt                int (nullable)      Patient/client ID
EPayer              epayer              varchar(?)          Payer name
EDate               edate               datetime (nullable) Date of eligibility check
EStaff              estaff              varchar(?)          Staff member who performed check
EPost               epost               varchar(?)          Post status or batch reference
EResponse           eresponse           varchar(?)          Response received from payer
EStatus             estatus             varchar(?)          Result status
EFormat             eformat             varchar(?)          Document format code
Filepath            filepath            varchar(?)          Path to returned elig document
EElecstatus         eelecstatus         varchar(?)          Electronic transaction status
EstaffStatus        estaffstatus        varchar(?)          Staff review status
EstaffNote          estaffnote          varchar(?)          Staff free-text note
EScan               escan               varchar(?)          Scan reference
EOrigid             eorigid             int (nullable)      Original eligibility ID (re-check)
Pyeligcheck         pyeligcheck         datetime (nullable) Prior year eligibility check date
________________________________________

13b. Azure Tbl3psetup (EF Model: Tbl3psetup)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.Tbl3psetup in BHG_DRContext

Primary Key: SiteCode + _pId (composite)

C# Property (EF)    SQL Column Name     Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar(25)         PK Part 1 — clinic code
_pId                _pId / 3pid         int                 PK Part 2 — payer setup ID
RowChkSum           RowChkSum           int                 Change detection hash
LastModAt           LastModAt           datetime            ETL last write timestamp
Clinic              Clinic              varchar(?)          Clinic display name
Address             Address             varchar(?)          Clinic billing address
City                City                varchar(?)          City
State               State               varchar(?)          State
Zip                 Zip                 varchar(?)          Zip code
Npi                 Npi                 varchar(?)          Clinic NPI number
TaxId               TaxId               varchar(?)          Federal tax ID
Medicaid            Medicaid            varchar(?)          Medicaid provider ID
Drlname             Drlname             varchar(?)          Doctor last name
Drfname             Drfname             varchar(?)          Doctor first name
Drnpi               Drnpi               varchar(?)          Doctor NPI
ProviderAddress     ProviderAddress     varchar(?)          Provider address
ProviderCity        ProviderCity        varchar(?)          Provider city
ProviderName        ProviderName        varchar(?)          Provider name
ProviderPhone       ProviderPhone       varchar(?)          Provider phone
ProviderState       ProviderState       varchar(?)          Provider state
ProviderZip         ProviderZip         varchar(?)          Provider zip
SiteId              SiteId              int (nullable)      SAMMS internal site ID (-1=blank)
Clia                Clia                varchar(?)          CLIA lab certification number
StrDbnotes          StrDbnotes          varchar(?)          Administrative notes
ProviderDesc        ProviderDesc        varchar(?)          Provider type description
BlHasPreloader      BlHasPreloader      bit (nullable)      Preloader flag (false if blank)
IndividualNpi       IndividualNpi       bit (nullable)      Use individual NPI flag
Taxonomy            Taxonomy            varchar(?)          Provider taxonomy code
Sftpun              Sftpun              varchar(?)          SFTP username
Sftppw              Sftppw              varchar(?)          SFTP password
________________________________________

13c. Azure Tbl3pClaimNote (EF Model: Tbl3pClaimNote)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.Tbl3pClaimNote in BHG_DRContext

Primary Key: SiteCode + TpcnTpcid (the parent claim ID is the lookup key)

C# Property (EF)            SQL Column Name             Type                Notes
----------------            ---------------             ----                -----
SiteCode                    SiteCode                    varchar(25)         Clinic code
Tpcn                        tpcn                        int                 Note row ID
TpcnTpcid                   TpcnTpcid                   int                 Parent claim ID (PK lookup key)
RowChkSum                   RowChkSum                   int                 Change detection hash
LastModAt                   LastModAt                   datetime            ETL last write timestamp
RowState                    RowState                    bit (nullable)      true=active
GlobalBatchId               GlobalBatchId               int (nullable)      Batch processing ID
TpcnDtmAdded                TpcnDtmAdded                datetime (nullable) Date note was added
TpcnStrAdded                TpcnStrAdded                varchar(?)          Username who added note
TpcnStrNote                 TpcnStrNote                 varchar(?)          Note text
TpcnStrType                 TpcnStrType                 varchar(?)          Note type/category
TpcnDtTickler               TpcnDtTickler               datetime (nullable) Tickler reminder date
TpcnDtTicklerRemoved        TpcnDtTicklerRemoved        varchar(?)          Date tickler removed
TpcnStrTicklerRemovedNote   TpcnStrTicklerRemovedNote   varchar(?)          Tickler removal reason
TpcnStrTicklerRemovedUser   TpcnStrTicklerRemovedUser   varchar(?)          Who removed tickler
TpcnStrTicklerType          TpcnStrTicklerType          varchar(?)          Tickler type
________________________________________

13d. Azure Tbl3pArnote (EF Model: Tbl3pArnote)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.Tbl3pArnote in BHG_DRContext

Primary Key: SiteCode + ArnId (composite)

C# Property (EF)    SQL Column Name     Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar(25)         Clinic code
ArnId               ArnId               int                 PK — unique AR note ID
RowChkSum           RowChkSum           int                 Change detection hash
LastModAt           LastModAt           datetime            ETL last write timestamp
RowState            RowState            bit (nullable)      true=active
GlobalBatchId       GlobalBatchId       int (nullable)      Batch processing ID
ArnLiid             ArnLiid             int (nullable)      Parent line item ID
ArnDate             ArnDate             datetime (nullable) Date AR note was created
ArnDtRemoved        ArnDtRemoved        datetime (nullable) Date AR note was resolved
ArnNote             ArnNote             varchar(?)          AR note text
ArnUser             ArnUser             varchar(?)          Username who created note
ArnStrRemovedReason ArnStrRemovedReason varchar(?)          Reason for removal
ArnStrRemovedUser   ArnStrRemovedUser   varchar(?)          Who removed the note
Bid                 Bid                 int (nullable)      Linked billing/remittance batch ID
ArnDbnotes          ArnDbnotes          varchar(?)          Administrative notes
________________________________________

14. Change Detection — RowChkSum

The RowChkSum column is the efficiency mechanism for this ETL.

How it is computed (at source, during SELECT by SelectConstructor):

    RowChkSum = CHECKSUM(
        <all mapped columns for this ActionKey/StepKey>
    )

For tbl3pElig example:
    RowChkSum = CHECKSUM(
        eid, eclt, epayer, edate, estaff, epost, eresponse, estatus,
        eformat, filepath, eelecstatus, estaffstatus, estaffnote,
        escan, eorigid, pyeligcheck
    )

How it is used across the four methods:

Save3pElig:
    if (pe.RowChkSum != rcs) { ... map all columns ... }
    Rows with matching checksums have only RowState=true set. Data columns are untouched.

Save3pSetup:
    if (dbSetup.RowChkSum != psetup.RowChkSum) { ... copy all fields ... }
    Rows with matching checksums are skipped entirely (no RowState update, no db.SaveChanges).

Save3pClaimNote:
    if (dbclaimNote.RowChkSum != claimNote.RowChkSum) { ... copy all fields ... }
    Rows with matching checksums are skipped entirely.

Save3pArnote:
    if (dbar.RowChkSum != ar.RowChkSum) { ... copy all fields ... }
    Rows with matching checksums are skipped entirely.

What this means in practice:
- A clinic that ran 200 eligibility checks this month but only 5 changed since yesterday
  generates 5 column-level updates. The other 195 rows are processed through the loop but
  no data columns change.
- This keeps the ETL fast even for sites with large historical eligibility volumes.
________________________________________

15. RowState — Soft Delete Tracking

RowState is a bit column (nullable) used as an active/inactive flag.

Value       Meaning
-----       -------
true (1)    Row is active — exists in current SAMMS data
false (0)   Row has been soft-deleted — existed in Azure but is no longer in SAMMS
NULL        Row has never been touched by RowState logic

Which methods use RowState:

Save3pElig:
    YES — uses full soft-delete cycle:
    - At load start: all year-scoped rows set to RowState=false (reset)
    - After each SAMMS row processed: RowState set back to true
    - Rows that appear in Azure but not in today's SAMMS fetch remain false (soft-deleted)

Save3pClaimNote:
    YES — RowState is always set to true on insert and on update.
    There is no soft-delete reset cycle. New rows are RowState=true; updated rows
    receive RowState=true from the source object.

Save3pArnote:
    YES — RowState is always set to true on insert and on update.
    Same behavior as Save3pClaimNote — no soft-delete reset cycle.

Save3pSetup:
    NO — RowState is NOT set in Save3pSetup. Setup configuration records are not
    managed with a soft-delete lifecycle in this method.

Usage in downstream queries:
    Select count(1) from [destination table]
    where SiteCode = 'VBRA' and RowState = 1
    — used by RowTrax to count active destination rows (where applicable)
________________________________________

16. Load Scoping Comparison

Each method scopes its Azure load differently. This is critical to understand because it
determines which rows are available for checksum comparison during each run:

Method              Azure Load Scope                        Strategy
------              ----------------                        --------
Save3pElig          EDate.Year >= wrkdt.Year                Year-to-date and future
                    AND SiteCode == sc
                    (yearly parameter does not change this)

Save3pSetup         SiteCode == sc only                     All-time, full site load
                    (no date filter)

Save3pClaimNote     SiteCode == sc                          Fixed year cutoff (2023+)
                    AND TpcnDtmAdded >= '1/1/2023'

Save3pArnote        SiteCode == sc                          Rolling 10-day window
                    AND ArnDate >= wrkdt.AddDays(-10)

Why this matters:
- A row that falls outside the Azure load scope cannot be found by FirstOrDefault(), so it
  will be treated as a new INSERT even if it already exists in Azure with the same data.
- Save3pSetup is the safest (full load) but is acceptable because setup records are low volume.
- Save3pArnote's rolling 10-day window means AR notes older than 10 days will be re-inserted
  if they appear in the SAMMS source data again. The WHERE clause on the source SELECT should
  align with this 10-day window to prevent phantom duplicates.
________________________________________

17. Load Design Summary

Load type: Incremental upsert with checksum-based change detection

Per-run behavior by method:

Save3pElig (pats/ins.tbl_3pElig):
    1. Load all Azure rows for this site where EDate year >= current year
    2. Soft-reset all loaded rows to RowState=false
    3. For each SAMMS row:
       - RowChkSum matches existing → set RowState=true only; no data writes
       - RowChkSum differs → update all columns, RowState=true, LastModAt
       - Not found in Azure → INSERT new row with RowChkSum=0 to force column map
    4. db.SaveChanges() — single batch commit

Save3pSetup (ins.tbl_3psetup):
    1. Load all Azure rows for this site (no date filter)
    2. For each SAMMS row (dynamic column switch):
       - Build psetup object from DataTable columns
       - Not found in Azure → INSERT; RowsIns++
       - RowChkSum differs → UPDATE all fields; RowsUpd++
       - RowChkSum matches → skip (no write)
    3. db.SaveChanges() after EVERY row (per-row commit inside loop)

Save3pClaimNote (ins.tbl_3pclaimnote):
    1. Load Azure rows for this site with TpcnDtmAdded >= 1/1/2023
    2. For each SAMMS row (dynamic column switch):
       - Build claimNote object from DataTable columns
       - Not found → stage in newCNs list; RowsIns++
       - RowChkSum differs → UPDATE all fields; RowsUpd++
       - RowChkSum matches → skip
    3. db.SaveChanges() — commit all updates in one batch
    4. If newCNs.Count > 0: db.Tbl3pClaimNote.AddRange(newCNs); db.SaveChanges()

Save3pArnote (ins.tbl_3parnote):
    1. Load Azure rows for this site with ArnDate >= wrkdt.AddDays(-10)
    2. For each SAMMS row (dynamic column switch):
       - Build ar object from DataTable columns
       - Not found → stage in newARs list; RowsIns++
       - RowChkSum differs → UPDATE all fields; RowsUpd++
       - RowChkSum matches → skip
    3. db.SaveChanges() — commit all updates in one batch
    4. If newARs.Count > 0: db.Tbl3pArnote.AddRange(newARs); db.SaveChanges()

Per-record identity:
Save3pElig      → SiteCode + EId
Save3pSetup     → SiteCode + _pId
Save3pClaimNote → SiteCode + TpcnTpcid  (parent claim ID, not the note's own ID)
Save3pArnote    → SiteCode + ArnId
________________________________________

18. Error Handling and Recovery

All four methods share the same try/catch error handling pattern:

    try
    {
        // ... full EF Core loop + SaveChanges() ...
    }
    catch (Exception e)
    {
        rc.IsResult = false
        rc.ExceptMsg = e.Message
        if (e.InnerException != null)
        {
            rc.ExceptInnerMsg = e.InnerException.Message
        }
    }
    return rc;

If an EF Core exception occurs:
- rc.IsResult is set to false
- The exception message is captured in rc.ExceptMsg
- The inner exception (if present) is captured in rc.ExceptInnerMsg
- The method returns normally; it does NOT re-throw

Commit behavior on failure:

Save3pElig:
    Single db.SaveChanges() at end. If exception occurs before it, no rows are written.
    If exception occurs during SaveChanges(), the entire batch is rolled back.

Save3pSetup:
    db.SaveChanges() inside the loop (per row). Rows committed before the failure are
    permanently written. Only the row that caused the exception and any rows after it
    are not saved. This is intentional — it allows partial success for setup records.

Save3pClaimNote:
    Two db.SaveChanges() calls. If the first (updates) succeeds but the second (inserts)
    fails, all updates are already committed. The new rows in newCNs are lost for this run
    but will be picked up as new inserts on the next run.

Save3pArnote:
    Same two-phase pattern as Save3pClaimNote. Same partial-commit behavior.

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

A failed 3p run for a clinic will automatically be retried the next day.
________________________________________

19. RowTrax — Audit and Row Count Tracking

Table: tsk.tbl_RowTrax (Azure BHG_DR)

After each successful load for a clinic (if st.RowTrax == true):

    sd.SaveRowTrax(
        st.SiteCode,           -- e.g. "VBRA"
        st.WorkDate,           -- today
        st.TaskName,           -- e.g. "ins.tbl_3pelig"
        SrcDt.Rows.Count,      -- rows returned from SAMMS this run
        destCount,             -- count in Azure
        null)

Destination count query (run against Azure):
    Select count(1) from [destination table]
    where SiteCode = 'VBRA' and RowState = 1   (where RowState is used)

Note: The source count is the DataTable row count from the incremental SAMMS fetch (not a
full source table count). The destination count reflects only active rows where RowState = 1.

The stored RowTrax records allow analysts to monitor eligibility and billing operations
record counts over time and detect clinics where insurance processing data may be lagging or
diverging from the source.
________________________________________

20. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-INV (Status=17)
        |-- insert child tasks per clinic:
        |       ins.tbl_3pelig x 80 clinics
        |       ins.tbl_3psetup x 80 clinics
        |       ins.tbl_3pclaimnote x 80 clinics
        |       ins.tbl_3parnote x 80 clinics
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2 (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 8
        |
        |-- filter: TaskName = 'SAMMS-ETL-INV', SiteCode != 'PHC'
        |-- for each parent task: mark ptask.Status = 18 (running)
        |
        |-- for each child task (one per clinic per table):
        |       get column mappings from dms.tbl_MapSrc2Dsn
        |       SelectConstructor.GetSLT() → builds SELECT field list + CHECKSUM()
        |       build WHERE clause (date range filter)
        |
        V
SQLSvrManager.GetTableData()
        |
        | executes SELECT against clinic SQL Server
        | connection from ctrl.tbl_LocationCons for this SiteCode
        |
        V
DataTable (in memory — rows from SAMMS)
        |
        |---[TaskName = ins.tbl_3pelig]
        |           |
        |           V
        |   Save3pElig() [EF CORE — Pattern A]
        |           |
        |   load Azure rows (year >= wrkdt.Year)
        |   reset all to RowState=false
        |   for each row:
        |     compare RowChkSum
        |     map 14 columns if changed
        |     conditional: eorigid, pyeligcheck
        |     RowState = true
        |   db.SaveChanges() [single batch]
        |           |
        |           V
        |   Azure Tbl3pElig
        |
        |---[TaskName = ins.tbl_3psetup]
        |           |
        |           V
        |   Save3pSetup() [EF CORE — Pattern B, per-row commit]
        |           |
        |   load ALL Azure rows for site
        |   for each row:
        |     build psetup object via switch
        |     match by _pId
        |     INSERT or UPDATE if RowChkSum changed
        |     db.SaveChanges() [per row]
        |           |
        |           V
        |   Azure Tbl3psetup
        |
        |---[TaskName = ins.tbl_3pclaimnote]
        |           |
        |           V
        |   Save3pClaimNote() [EF CORE — Pattern B, batched insert]
        |           |
        |   load Azure rows (TpcnDtmAdded >= 1/1/2023)
        |   for each row:
        |     build claimNote via switch
        |     match by TpcnTpcid
        |     UPDATE if RowChkSum changed
        |     stage new rows in newCNs
        |   db.SaveChanges() [update batch]
        |   db.AddRange(newCNs); db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure Tbl3pClaimNote
        |
        |---[TaskName = ins.tbl_3parnote]
        |           |
        |           V
        |   Save3pArnote() [EF CORE — Pattern B, batched insert]
        |           |
        |   load Azure rows (ArnDate >= wrkdt.AddDays(-10))
        |   for each row:
        |     build ar via switch
        |     match by ArnId
        |     UPDATE if RowChkSum changed
        |     stage new rows in newARs
        |   db.SaveChanges() [update batch]
        |   db.AddRange(newARs); db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure Tbl3pArnote
        |
        V
RowTrax audit saved to tsk.tbl_RowTrax
        |
        V
BHGTaskRunner marks task Status=20 (complete)
________________________________________

21. File Reference Map

File Path                                       Purpose
---------                                       -------
BCAppCode/Scheduler/Program.cs                  Creates daily task queue for all ETL pipelines
BCAppCode/BHGTaskRunner/Program.cs              Main ETL driver (arg=8 → Insurance/3p pipeline)
BCAppCode/BHG-DR-LIB/SelectConstructor.cs       Builds SELECT + CHECKSUM() from metadata
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs           ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/Save3pElig.cs             EF Core upsert — Save3pElig, Save3pSetup,
                                                 Save3pClaimNote, Save3pArnote
BCAppCode/BHG-DR-LIB/Models/Tbl3pElig.cs       EF Model → Azure Tbl3pElig
BCAppCode/BHG-DR-LIB/Models/Tbl3psetup.cs      EF Model → Azure Tbl3psetup
BCAppCode/BHG-DR-LIB/Models/Tbl3pClaimNote.cs  EF Model → Azure Tbl3pClaimNote
BCAppCode/BHG-DR-LIB/Models/Tbl3pArnote.cs     EF Model → Azure Tbl3pArnote
BCAppCode/BHG-DR-LIB/BHG_DRContext.cs          EF DbContext — db.Tbl3pElig, db.Tbl3psetup,
                                                 db.Tbl3pClaimNote, db.Tbl3pArnote
________________________________________

22. Quick Reference Summary

What triggers 3p ETL?           Scheduler.exe creates tasks, BHGTaskRunner.exe 8 processes them
TaskName in scheduler?          SAMMS-ETL-INV (same schedule as Claims)
Source tables in SAMMS?         dbo.tbl3pElig, dbo.tbl3psetup, dbo.tbl3pClaimNote, dbo.tbl3pArnote
Destination tables in Azure?    Tbl3pElig, Tbl3psetup, Tbl3pClaimNote, Tbl3pArnote
Primary key (3pElig)?           SiteCode + EId (composite)
Primary key (3pSetup)?          SiteCode + _pId (composite)
Primary key (3pClaimNote)?      SiteCode + TpcnTpcid (parent claim ID, not note ID)
Primary key (3pArnote)?         SiteCode + ArnId (composite)
EF Core or Bulk path?           All four methods use EF Core only — no bulk/staging path
How is change detected?         RowChkSum = CHECKSUM() across all mapped columns at source
What is RowState?               Soft-delete flag — true=active, false=deleted/inactive
Which methods use RowState?     Save3pElig (full soft-delete cycle), Save3pClaimNote and
                                Save3pArnote (RowState=true always set on write)
How are deletes handled?        Save3pElig: soft-reset before loop, re-activate per row found
                                Save3pSetup/ClaimNote/Arnote: no soft-delete — rows persist
Azure load scope?               3pElig: year >= wrkdt.Year | 3pSetup: full site (no date filter)
                                3pClaimNote: fixed 2023+ | 3pArnote: rolling 10-day window
Per-row vs batch commit?        Save3pSetup: per-row SaveChanges()
                                All others: batch SaveChanges() at end of loop
Batched insert optimization?    Save3pClaimNote and Save3pArnote: new rows staged in list,
                                AddRange() used for bulk insert after update batch commits
Conditional field parsing?      Save3pElig: eorigid (non-empty guard), pyeligcheck (length > 6)
                                Save3pArnote: arnliid, bid, globalbatchid (non-empty guards)
                                Save3pSetup: siteid (-1 sentinel), BlHasPreloader/IndividualNpi
                                (bool.Parse with false fallback)
RowTrax audit?                  Source DataTable row count vs count(RowState=1) in Azure
Error recovery?                 Scheduler resets failed tasks to Status=17 on next daily run
PHC handled here?               No — PHC uses its own runner (PHC/Program.cs) with separate logic
yearly parameter behavior?      Accepted by all four methods; only Save3pElig reads it, and
                                both branches currently execute the same query (placeholder)
