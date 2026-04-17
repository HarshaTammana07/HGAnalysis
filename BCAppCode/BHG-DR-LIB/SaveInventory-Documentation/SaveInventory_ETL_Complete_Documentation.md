
Inventory & Orientation ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract medication bottle
inventory, liquid dispensing logs, inventory type definitions, and patient orientation
checklists from local SAMMS SQL Server databases at each clinic and load them into the central
Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What inventory data is and why it exists
- What the four save methods in SaveInventory.cs do and how they differ
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How SelectConstructor builds the source SQL
- How SQLSvrManager executes against the clinic databases
- What the source tables look like and all their columns
- What the destination tables look like and all their columns
- How change detection works using RowChkSum (and where it is absent)
- How RowState tracks active records
- How the batched insert optimization works
- What special-case field guards exist in each method
- How RowTrax audit tracking works
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What is Inventory data?

BHG clinics dispense controlled substances (primarily methadone and buprenorphine) as
Medication-Assisted Treatment (MAT). The inventory system in SAMMS tracks every bottle of
medication received, every unit dispensed, and the configuration of each medication type.
The SaveInventory.cs pipeline captures this medication lifecycle data and centralizes it in
the BHG_DR data warehouse.

The file manages four related tables that together form the complete inventory and patient
onboarding dataset:

1. dbo.tblBottle (SAMMS)                      → Azure TblBottle
   One row per physical medication bottle received at the clinic. Tracks the DEA number,
   lot number, initial volume, bottle type, expiration date, and whether the bottle has
   been closed (depleted). Each bottle is the source of inventory deductions as doses
   are dispensed.

2. dbo.tblLiquidLog (SAMMS)                   → Azure TblLiquidLog
   One row per liquid medication movement event (dispensing, adjustments, pre-packs,
   reconciliation entries). Links each movement to a bottle (BtlId), a dose (DoseId),
   a beaker (BkrId), and a pump. This is the granular transaction log of all liquid
   medication activity at the clinic.

3. dbo.tblInvType (SAMMS)                     → Azure TblInvtype
   One row per medication type configuration. Defines the drug name, form (liquid vs film),
   NDC code, J-code, units-per-bottle, dilution factor, medication class, and display
   names used across SAMMS. This is the reference/lookup table for all inventory items.

4. dbo.tblOrientationChecklist (SAMMS)        → Azure TblOrientationChecklistNew
   One row per patient orientation checklist completion event. Captures the patient's
   acknowledgement of clinic rules, policies, and regulatory disclosures at admission.
   Each checklist row is a signed record of what the patient was oriented to.

Why it is important

This dataset supports:
- DEA compliance — complete audit trail of all controlled substance receipts and dispensing
- Medication reconciliation — bottle-level traceability from receipt through depletion
- Operational reporting — daily liquid log activity feeds dispensing analytics dashboards
- Inventory type reference — canonical drug definitions used across dose, order, and billing
  pipelines for CPT codes and J-codes
- Regulatory compliance — orientation checklist records prove patients were informed of
  policies as required by SAMSA/OTP accreditation standards

Load type

All four methods use the EF Core upsert path exclusively. There is no Bulk / SqlBulkCopy
path for these tables. All methods use the same structural pattern:

Pattern: Dynamic column switch + batched insert
  Loops through DataTable columns using a switch statement to build a new model object per
  row, then matches by primary key to decide INSERT vs UPDATE. New rows are staged in a
  list and inserted with db.AddRange() after all updates are committed in a single batch.

Key behavioral difference — change detection:
  SaveBottles, SaveLiquidlog: RowChkSum comparison guards updates (only changed rows written)
  SaveInvTypes: RowChkSum is NOT mapped in the switch — inv.RowChkSum stays 0, so the
               comparison always triggers a full update for every existing row on every run
  SaveOrientationCheckList: No RowChkSum at all — always updates every matched row
________________________________________

3. Systems Involved

System / File                        Role
-----------                          ----
tsk.tbl_Schedule (Azure DB)          Configuration — defines schedules and their run times
Scheduler.exe                        Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=8              Main ETL orchestrator for SAMMS-ETL-INV (Inventory)
dms.tbl_MapSrc2Dsn (Azure DB)        Metadata — defines which columns to SELECT for each ActionKey
SelectConstructor.cs                 Assembles SELECT statement from metadata
SQLSvrManager.cs                     Fires SELECT against the clinic SAMMS SQL Server
SaveInventory.cs / SaveData          EF Core upsert class — all four inventory methods live here
ctrl.tbl_LocationCons (Azure)        Connection strings for each clinic's SAMMS SQL Server
tsk.tbl_RowTrax (Azure)              Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How Inventory Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily (typically overnight or early morning) and populates the
task queue for all ETL pipelines. It does NOT move data — it only creates tasks.

What the Scheduler does for SAMMS-ETL-INV (Inventory)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Tasks from previous days that were never picked up are marked as expired (RowState=26).

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For the Inventory/Insurance schedule:
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
task rows for each clinic and each destination table:

    insert into tsk.tbl_Tasks2(ParentTaskId, TaskName, ...)
    select t.TaskId,
           ma.DsnSchema + '.' + ma.DsnTbl,  -- e.g. 'inv.tbl_bottle'
           ma.ActionKey,                     -- = 8
           ma.StepKey,
           ma.SiteCode                       -- = 'B01', 'VBRA', etc.
    from dms.vw_MapAction ma
    cross join tsk.tbl_Tasks2 t
    where ma.Enabled = 1
      and ma.IsActive = 1
      and case when ma.DsnSchema + '.' + ma.DsnTbl in
               ('inv.tbl_bottle', 'inv.tbl_liquidlog', 'inv.tbl_invtype',
                'pat.tbl_orientationchecklistnew')
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
        TaskName = 'inv.tbl_bottle'
        SiteCode = 'VBRA'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'inv.tbl_liquidlog'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'inv.tbl_invtype'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pat.tbl_orientationchecklistnew'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How Inventory Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 8 to process the SAMMS-ETL-INV schedule, which
includes Claims, 3p Insurance tables, AND all four Inventory tables.

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
  (for tables that use RowChkSum)
- Replacing placeholder tokens (@SiteCode, @WorkDate, @Samms)

Step 6 — Build the WHERE clause
The WHERE clause filters by date using DaysBack (varies by table). For LiquidLog a
lookback window (e.g. -15 days) is typical. For Bottles and InvTypes, broader or no date
filter may be used.

    strCmd += " where " + strWhere + " " + st.SortOrder;

Step 7 — Execute SELECT against SAMMS
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);

Returns a DataTable with all matching rows from this clinic for the specific table.

Step 8 — Route to the appropriate Save method
The routing is performed by SelectConstructor or directly in BHGTaskRunner based on TaskName:

    case "inv.tbl_bottle" (or equivalent):
        rCodes = sd.SaveBottles(SrcDt, st.SiteCode, WorkDate, true/false, null)

    case "inv.tbl_liquidlog":
        rCodes = sd.SaveLiquidlog(SrcDt, st.SiteCode, WorkDate, false, null)

    case "inv.tbl_invtype":
        rCodes = sd.SaveInvTypes(SrcDt, st.SiteCode, WorkDate, false, null)

    case "pat.tbl_orientationchecklistnew":
        rCodes = sd.SaveOrientationCheckList(SrcDt, st.SiteCode, WorkDate, null)
        // Note: SaveOrientationCheckList signature has NO yearly parameter

All four methods are EF Core upsert paths. There is no Bulk / SqlBulkCopy path.

Step 9 — RowTrax audit (if enabled for this task)
If st.RowTrax == true:

    Source count:  SrcDt.Rows.Count  (rows returned from SAMMS)

    Destination count:
        Select count(1) from [destination table]
        where SiteCode = 'VBRA'   (and RowState = 1 where applicable)

    These counts are saved to tsk.tbl_RowTrax.

Step 10 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. SelectConstructor — How the SELECT Is Built

File: BCAppCode/BHG-DR-LIB/SelectConstructor.cs

SelectConstructor.GetSLT() is called for every child task. It reads the column metadata from
dms.tbl_MapSrc2Dsn and assembles the SELECT field list with CHECKSUM() for change detection.

For Bottles, the SELECT looks conceptually like this:

    Select
        SiteCode = 'VBRA',
        bottleid,
        deanum,
        lotnumber,
        dtreceived,
        liquid,
        bottletype,
        initialamount,
        dtclosed,
        blnclosed,
        struser,
        white,
        specgrav,
        weight,
        color,
        invgroup,
        brid,
        siteid,
        manufacturer,
        expdate,
        RowChkSum = CHECKSUM(bottleid, deanum, lotnumber, dtreceived, liquid, bottletype,
                             initialamount, dtclosed, blnclosed, struser, white, specgrav,
                             weight, color, invgroup, brid, siteid, manufacturer, expdate)
    from dbo.tblBottle
    where dtreceived >= DATEADD(day, -90, GETDATE())  -- (example; actual filter varies)

For InvTypes: Note that RowChkSum is produced by CHECKSUM() in the SELECT (from
SelectConstructor) but is NOT mapped in SaveInvTypes' switch statement. The EF model
object's RowChkSum property remains 0. The result is that the RowChkSum comparison
(dbtyp.RowChkSum != inv.RowChkSum) will always be true for any row that has ever had a
RowChkSum written to Azure — causing a full column update on every run for every row.

For OrientationChecklist: No RowChkSum column is used. SelectConstructor may or may not
include a CHECKSUM() expression; the Save method does not read or compare it.
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
WHERE clause. This DataTable is passed directly into the appropriate SaveInventory method.
________________________________________

8. Source Tables — SAMMS SQL Server (dbo)

All four source tables live in the clinic's local SAMMS SQL Server database under the dbo
schema.

________________________________________
8a. dbo.tblBottle — Medication Bottle Inventory

Primary Key: bottleId (unique per clinic)

Column Name     Type                Description
-----------     ----                -----------
bottleid        int                 Unique bottle ID within this clinic
deanum          varchar(?)          DEA registration number for this bottle/shipment
lotnumber       varchar(?)          Manufacturer lot number printed on the bottle
dtreceived      datetime            Date the bottle was received at the clinic
liquid          bool (nullable)     True = liquid methadone; false/null = solid/film
bottletype      varchar(?)          Bottle type label (e.g. '40mg', '1000mL')
initialamount   int                 Initial unit count or volume in this bottle
dtclosed        datetime (nullable) Date the bottle was closed/depleted (conditional parse)
blnclosed       bool                Whether the bottle has been closed (no remaining inventory)
struser         varchar(?)          Username who received/entered the bottle
white           bool (nullable)     Color indicator — white bottle flag
specgrav        decimal (nullable)  Specific gravity measurement for liquid bottles
weight          decimal (nullable)  Weight measurement for the bottle
color           varchar(?)          Bottle color description
invgroup        varchar(?)          Inventory group/category this bottle belongs to
brid            int (nullable)      Linked batch receipt ID
siteid          int (nullable)      SAMMS internal numeric site ID
manufacturer    varchar(?)          Manufacturer name for this medication batch
expdate         datetime (nullable) Bottle expiration date (conditional parse)
SiteCode        varchar(25)         Added by ETL — clinic identifier (e.g. 'VBRA')
RowChkSum       int                 Computed by ETL: CHECKSUM() across all mapped columns
________________________________________

8b. dbo.tblLiquidLog — Liquid Medication Dispensing Log

Primary Key: liqid (unique per clinic)

Column Name         Type                Description
-----------         ----                -----------
liqid               int                 Unique liquid log entry ID within this clinic
pump                Int16 (nullable)    Pump number used for this dispensing event
doseid              int (nullable)      Linked dose record ID (references tblDose)
btlid               int (nullable)      Source bottle ID (references tblBottle)
bkrid               int (nullable)      Beaker ID used for this measurement
amt                 decimal (nullable)  Amount dispensed (in mL or units); see overflow guard
dtm                 datetime (nullable) Date/time of this liquid log event
desc                varchar(?)          Description or transaction type code
staff               varchar(?)          Staff member who performed the action
bllogonly           bool (nullable)     True = log-only entry (no physical dispense occurred)
blprepack           bool (nullable)     True = pre-packaged dose (prepared in advance)
memonew             varchar(?)          New/current memo note for this entry
memo                varchar(?)          Legacy memo field
dtrti               datetime (nullable) Date/time of return-to-inventory event
acknowledgedate     varchar(?)          Date staff acknowledged this entry (stored as string)
acknowledgeuser     varchar(?)          Username who acknowledged this entry
regionaldate        datetime (nullable) Date reviewed by regional staff
regionaluser        varchar(?)          Regional staff username who reviewed
complainceuser      varchar(?)          Compliance officer username (note: field name is misspelled
                                        in SAMMS source — 'complaince' not 'compliance')
compliancedate      datetime (nullable) Date compliance review occurred
invgroup            varchar(?)          Inventory group/category for this transaction
siteid              int (nullable)      SAMMS internal numeric site ID
SiteCode            varchar(25)         Added by ETL — clinic identifier
RowChkSum           int                 Computed by ETL: CHECKSUM() across all mapped columns

Special guard — amt overflow value:
    if (amt == "690122921") → lg.Amt = 0
This guard catches a known corrupted or sentinel value (690122921) that appears in some
SAMMS databases where a liquid amount field overflowed or was set to an invalid placeholder.
Rather than rejecting the row or propagating the invalid number, the ETL substitutes 0.
________________________________________

8c. dbo.tblInvType — Inventory Type Definitions

Primary Key: invid (unique per clinic; 0 used as sentinel for blank)

Column Name     Type                Description
-----------     ----                -----------
invid           int                 Unique inventory type ID (0 if blank in source)
invname         varchar(?)          Full medication name (e.g. 'Methadone 40mg Oral Conc.')
invliquid       bool (nullable)     True = this type is a liquid formulation
invunit         int (nullable)      Units per bottle for this medication type
invtotal        int (nullable)      Total unit count stored (calculated field)
invdivision     decimal (nullable)  Dilution or concentration factor (e.g. 10.0 = 10mL/dose)
defaultmed      bool (nullable)     True = this is the default medication for new patients
displayname     varchar(?)          Short display name shown in SAMMS UI
invmedclass     varchar(?)          Medication class code (e.g. 'OPI', 'BUP')
isfilm          bool (nullable)     True = buprenorphine film formulation (not liquid)
type            varchar(?)          Inventory type category string
hasbeaker       bool (nullable)     True = this medication type uses a beaker for dispensing
invactual       varchar(?)          Actual/physical inventory description
invlabelname    varchar(?)          Label name printed on dispensing records
invndc          varchar(?)          NDC (National Drug Code) for this medication
invjcode        varchar(?)          J-code (HCPCS billing code) for insurance claims
SiteCode        varchar(25)         Added by ETL — clinic identifier
RowChkSum       int                 NOT mapped in switch — always stays 0 in the EF object
                                    (see Section 11 for full behavioral explanation)

Note: RowChkSum is not included as a switch case in SaveInvTypes. The RowChkSum on the
EF object (inv.RowChkSum) will always be 0. The comparison dbtyp.RowChkSum != inv.RowChkSum
will always be true for any row where Azure has a non-zero stored RowChkSum. This means
SaveInvTypes effectively performs a full update of all columns on every matching row every
run, regardless of whether data changed.
________________________________________

8d. dbo.tblOrientationChecklist — Patient Orientation Checklists

Primary Key: checklistid or id (unique per clinic; 0 used as sentinel for blank)

Column Name                         Type                Description
-----------                         ----                -----------
checklistid / id                    int                 Unique checklist ID (two source aliases)
preadmissionid                      int (nullable)      Pre-admission record ID (linked to intake)
clientid                            int (nullable)      Patient/client ID
dataformid                          int (nullable)      Data form definition ID
patientcomplaints                   bool (nullable)     Patient rights & complaint procedure reviewed
accesstoemergency                   bool (nullable)     Emergency services access information reviewed
codeofethics                        bool (nullable)     Code of ethics reviewed
confidentialitypolicy               bool (nullable)     Confidentiality/HIPAA policy reviewed
methods                             bool (nullable)     Treatment methods explained
explanationoffiancialobligations    bool (nullable)     Financial obligations explained (note: source
                                                        field name contains typo 'fianc' not 'financ')
rulesforinvoluntarydetox            bool (nullable)     Involuntary detox rules reviewed
firesafety                          bool (nullable)     Fire safety procedures reviewed
programrulesonpatientparking        bool (nullable)     Patient parking rules reviewed
policyonrestraint                   bool (nullable)     Restraint policy reviewed
policyontobaccoproducts             bool (nullable)     Tobacco policy reviewed
policyonillicit                     bool (nullable)     Illicit drug policy reviewed
policyonweapons                     bool (nullable)     Weapons policy reviewed
knowledgeofnames                    bool (nullable)     Staff names/contact information shared
programrules                        bool (nullable)     General program rules reviewed
aidshivprevention                   bool (nullable)     AIDS/HIV prevention information reviewed
hepatitisprevention                 bool (nullable)     Hepatitis prevention information reviewed
purposeandprocess                   bool (nullable)     Treatment purpose and process explained
individualtreatmentplan             bool (nullable)     Individual treatment plan discussed
policyregardingurinedrugscreens     bool (nullable)     Urine drug screen policy reviewed
dischargetransitioncriteria         bool (nullable)     Discharge/transition criteria reviewed
naturalprogression                  bool (nullable)     Natural progression of disease reviewed
createdby                           varchar(?)          Username who created this checklist
createdon                           datetime (nullable) Date/time checklist was created
modifiedby                          varchar(?)          Username who last modified this checklist
modifiedon                          datetime (nullable) Date/time of last modification
isdeleted                           bool                Soft-delete flag from source ("1"=true, else false)
version / versionx                  varchar(?)          Checklist version or form version string
staffsignature                      varchar(?)          Staff signature field
staffsignaturedate                  datetime (nullable) Date of staff signature
staffsignatureby                    varchar(?)          Username of staff who signed
patientsignature                    varchar(?)          Patient signature field
patientsignaturedate                datetime (nullable) Date of patient signature
patientsignatureby                  varchar(?)          Username of patient who signed
SiteCode                            varchar(25)         Added by ETL — clinic identifier
________________________________________

9. SaveBottles — EF Core Path

File: BCAppCode/BHG-DR-LIB/SaveInventory.cs
Class: SaveData (partial class)
Method: SaveBottles()

Method signature:
    public RCodes SaveBottles(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted but not used for scoping)
        bool yearly,             // accepted but not used for branching
        BHG_DRContext db)        // EF context (created if null)

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count }
    Note: RowsIns and RowsUpd start at 0 (default). RowsProcessed is set to total rows.

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load ALL existing Azure rows for this site (no date filter)
    bottles = db.TblBottle.Where(x => x.SiteCode == sc).ToList()

All bottle records ever loaded for this clinic are pulled into memory. This is appropriate
because bottles are long-lived records (a bottle may be active for weeks or months) and
the full history is needed to detect changes or confirm existing rows.

Step 4 — Initialize new-row staging list
    List<TblBottle> newbottles = new List<TblBottle>();

New rows are staged here and inserted after the update pass completes.

Step 5 — Loop through every row from the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 6 — Build a new model object by iterating all DataTable columns
    Models.TblBottle btl = new Models.TblBottle();
    foreach (DataColumn c in tbl.Columns)
    {
        switch (c.ColumnName.ToLower()) { ... }
    }

Column mapping switch cases and special handling:

    "sitecode"      → btl.SiteCode = sc; btl.RowState = true; btl.LastModAt = DateTime.Now
    "rowchksum"     → btl.RowChkSum = int.Parse(value)
    "bottleid"      → btl.BottleId = int.Parse(value)
    "deanum"        → btl.Deanum (string, direct)
    "lotnumber"     → btl.LotNumber (string, direct)
    "dtreceived"    → btl.DtReceived = DateTime.Parse(value)  [always parsed — no guard]
    "liquid"        → btl.Liquid = bool.Parse(value)  [conditional: only if length > 0]
    "bottletype"    → btl.BottleType (string, direct)
    "initialamount" → btl.InitialAmount = int.Parse(value)
    "dtclosed"      → btl.DtClosed = DateTime.Parse(value)   [conditional: length > 6]
    "blnclosed"     → btl.BlnClosed = bool.Parse(value)  [always parsed — no guard]
    "struser"       → btl.StrUser (string, direct)
    "white"         → btl.White = bool.Parse(value)       [conditional: length > 0]
    "specgrav"      → btl.SpecGrav = decimal.Parse(value) [conditional: length > 0]
    "weight"        → btl.Weight = decimal.Parse(value)   [conditional: length > 0]
    "color"         → btl.Color (string, direct)
    "invgroup"      → btl.InvGroup (string, direct)
    "brid"          → btl.BrId = int.Parse(value)         [conditional: length > 0]
    "siteid"        → btl.SiteId = int.Parse(value)       [conditional: length > 0]
    "manufacturer"  → btl.Manufacturer (string, direct)
    "expdate"       → btl.ExpDate = DateTime.Parse(value) [conditional: length > 6]

Step 7 — Match against existing Azure row by BottleId
    Models.TblBottle dbBottle = bottles.FirstOrDefault(x => x.BottleId == btl.BottleId);

Step 8 — INSERT or UPDATE based on match and RowChkSum

    if (dbBottle == null):
        rc.RowsIns += 1
        newbottles.Add(btl)      // staged for bulk insert

    else if (dbBottle.RowChkSum != btl.RowChkSum):
        rc.RowsUpd += 1
        // Copy all fields from new object to existing tracked EF object
        dbBottle.BlnClosed    = btl.BlnClosed
        dbBottle.BottleType   = btl.BottleType
        dbBottle.BrId         = btl.BrId
        dbBottle.Color        = btl.Color
        dbBottle.Deanum       = btl.Deanum
        dbBottle.DtClosed     = btl.DtClosed
        dbBottle.DtReceived   = btl.DtReceived
        dbBottle.ExpDate      = btl.ExpDate
        dbBottle.InitialAmount= btl.InitialAmount
        dbBottle.InvGroup     = btl.InvGroup
        dbBottle.LastModAt    = btl.LastModAt
        dbBottle.Liquid       = btl.Liquid
        dbBottle.LotNumber    = btl.LotNumber
        dbBottle.Manufacturer = btl.Manufacturer
        dbBottle.RowChkSum    = btl.RowChkSum
        dbBottle.RowState     = btl.RowState
        dbBottle.SiteId       = btl.SiteId
        dbBottle.SpecGrav     = btl.SpecGrav
        dbBottle.StrUser      = btl.StrUser
        dbBottle.Weight       = btl.Weight
        dbBottle.White        = btl.White

    else (RowChkSum matches):
        // No action — bottle not modified; row is not touched in Azure

Step 9 — Commit all updates in one batch
    db.SaveChanges()

Step 10 — Insert all new rows using AddRange
    if (newbottles.Count > 0):
        db.TblBottle.AddRange(newbottles)
        db.SaveChanges()
________________________________________

10. SaveLiquidlog — EF Core Path

File: BCAppCode/BHG-DR-LIB/SaveInventory.cs
Class: SaveData (partial class)
Method: SaveLiquidlog()

Method signature:
    public RCodes SaveLiquidlog(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted; see scoping note below)
        bool yearly,             // accepted but not used for branching
        BHG_DRContext db)        // EF context (created if null)

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load existing Azure rows for this site
    liquidLogs = db.TblLiquidLog
        .Where(x => x.SiteCode == sc
            //&& x.Dtm >= wrkdt.AddDays(-15)
        )
        .ToList()

The date filter (.Dtm >= wrkdt.AddDays(-15)) is currently COMMENTED OUT. This means all
liquid log records ever loaded for this clinic are pulled into memory on every run.
This is a significant load for high-volume clinics. The commented line suggests a -15 day
rolling window was previously used or is being evaluated. Without the filter, the in-memory
list can grow to tens of thousands of rows for active clinics.

Step 4 — Initialize new-row staging list
    List<TblLiquidLog> newlogs = new List<TblLiquidLog>();

Step 5 — Loop through every row from the SAMMS DataTable

Step 6 — Build a new model object by iterating all DataTable columns

Column mapping switch cases and special handling:

    "sitecode"          → lg.SiteCode = sc; lg.LastModAt = DateTime.Now; lg.RowState = true
    "liqid"             → lg.LiqId = int.Parse(value)
    "pump"              → lg.Pump = Int16.Parse(value.Trim())   [conditional: trim.Length > 0]
    "doseid"            → lg.DoseId = int.Parse(value.Trim())   [conditional: trim.Length > 0]
    "btlid"             → lg.BtlId = int.Parse(value.Trim())    [conditional: trim.Length > 0]
    "bkrid"             → lg.BkrId = int.Parse(value.Trim())    [conditional: trim.Length > 0]
    "amt"               → SPECIAL — see overflow guard below
    "dtm"               → lg.Dtm = DateTime.Parse(value)        [conditional: length > 6]
    "desc"              → lg.Desc = value.Trim()
    "staff"             → lg.Staff (string, direct)
    "bllogonly"         → lg.BlLogOnly = bool.Parse(value.Trim()) [conditional: trim.Length > 0]
    "blprepack"         → lg.BlPrepack = bool.Parse(value.Trim()) [conditional: trim.Length > 0]
    "memonew"           → lg.Memonew = value.Trim()
    "memo"              → lg.Memo = value.Trim()
    "dtrti"             → lg.DtRti = DateTime.Parse(value)      [conditional: length > 6]
    "acknowledgedate"   → lg.AcknowledgeDate (string, direct)
    "acknowledgeuser"   → lg.AcknowledgeUser (string, direct)
    "regionaldate"      → lg.RegionalDate = DateTime.Parse(value) [conditional: length > 6]
    "regionaluser"      → lg.RegionalUser (string, direct)
    "complainceuser"    → lg.ComplainceUser (string, direct)    [note: field is misspelled in source]
    "compliancedate"    → lg.ComplianceDate = DateTime.Parse(value) [conditional: length > 6]
    "invgroup"          → lg.Invgroup (string, direct)
    "siteid"            → lg.SiteId = int.Parse(value.Trim())   [conditional: trim.Length > 0]
    "rowchksum"         → lg.RowChkSum = int.Parse(value)

Amt overflow guard (special case):
    if (value.Trim().Length > 0):
        if (value.Trim() == "690122921"):
            lg.Amt = 0                        // sentinel/corrupted value → substitute 0
        else:
            lg.Amt = decimal.Parse(value)

The value "690122921" is a known bad data sentinel that appears in some SAMMS clinic
databases. Its origin is likely an integer overflow or a default value set by a legacy
SAMMS module. Substituting 0 prevents a decimal overflow exception and avoids loading
a nonsensical amount into the warehouse.

Step 7 — Match against existing Azure row by LiqId
    Models.TblLiquidLog dbll = liquidLogs.FirstOrDefault(x => x.LiqId == lg.LiqId);

Step 8 — INSERT or UPDATE based on match and RowChkSum

    if (dbll == null):
        newlogs.Add(lg)         // staged for bulk insert
        rc.RowsIns += 1

    else if (dbll.RowChkSum != lg.RowChkSum):
        rc.RowsUpd += 1
        dbll.AcknowledgeDate = lg.AcknowledgeDate
        dbll.AcknowledgeUser = lg.AcknowledgeUser
        dbll.Amt             = lg.Amt
        dbll.BkrId           = lg.BkrId
        dbll.BlLogOnly       = lg.BlLogOnly
        dbll.BlPrepack       = lg.BlPrepack
        dbll.BtlId           = lg.BtlId
        dbll.ComplainceUser  = lg.ComplainceUser
        dbll.ComplianceDate  = lg.ComplianceDate
        dbll.Desc            = lg.Desc
        dbll.DoseId          = lg.DoseId
        dbll.Dtm             = lg.Dtm
        dbll.DtRti           = lg.DtRti
        dbll.Invgroup        = lg.Invgroup
        dbll.LastModAt       = lg.LastModAt
        dbll.Memo            = lg.Memo
        dbll.Memonew         = lg.Memonew
        dbll.Pump            = lg.Pump
        dbll.RegionalDate    = lg.RegionalDate
        dbll.RegionalUser    = lg.RegionalUser
        dbll.RowChkSum       = lg.RowChkSum
        dbll.RowState        = lg.RowState
        dbll.SiteId          = lg.SiteId
        dbll.Staff           = lg.Staff

    else (RowChkSum matches):
        // No action — log entry not modified

Step 9 — Commit all updates in one batch
    db.SaveChanges()

Step 10 — Insert all new rows using AddRange
    if (newlogs.Count > 0):
        db.TblLiquidLog.AddRange(newlogs)
        db.SaveChanges()

Note on the Staff field: Staff is mapped during the switch but is NOT included in the
update block (Step 8). If a Staff value changes on an existing log record, it will NOT be
updated in Azure even if RowChkSum differs. This is a column omission in the update block.
________________________________________

11. SaveInvTypes — EF Core Path (No Effective RowChkSum Guard)

File: BCAppCode/BHG-DR-LIB/SaveInventory.cs
Class: SaveData (partial class)
Method: SaveInvTypes()

Method signature:
    public RCodes SaveInvTypes(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted but not used for scoping)
        bool yearly,             // accepted but not used for branching
        BHG_DRContext db)        // EF context (created if null)

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load ALL existing Azure rows for this site (no date filter)
    invtypes = db.TblInvtype.Where(x => x.SiteCode == sc).ToList()

Inventory type definitions are global-per-clinic (typically 3-10 rows). Full load is fast.

Step 4 — Initialize new-row staging list
    List<TblInvtype> newinv = new List<TblInvtype>();

Step 5 — Loop through every row from the SAMMS DataTable

Step 6 — Build a new model object by iterating all DataTable columns

Column mapping switch cases and special handling:

    "sitecode"      → inv.SiteCode = sc; inv.RowState = true; inv.LastModAt = DateTime.Now
    "invid"         → inv.Invid = int.Parse(value) OR 0 if blank  [conditional with 0 fallback]
    "invname"       → inv.InvName (string, direct)
    "invliquid"     → inv.InvLiquid = bool.Parse(value)    [conditional: length > 0]
    "invunit"       → inv.InvUnit = int.Parse(value)       [conditional: length > 0]
    "invtotal"      → inv.InvTotal = int.Parse(value)      [conditional: length > 0]
    "invdivision"   → inv.InvDivision = decimal.Parse(value) [conditional: length > 0]
    "defaultmed"    → inv.DefaultMed = bool.Parse(value)   [conditional: length > 0]
    "displayname"   → inv.DisplayName (string, direct)
    "invmedclass"   → inv.InvMedclass (string, direct)
    "isfilm"        → inv.IsFilm = bool.Parse(value)       [conditional: length > 0]
    "type"          → inv.Type (string, direct)
    "hasbeaker"     → inv.HasBeaker = bool.Parse(value)    [conditional: length > 0]
    "invactual"     → inv.InvActual (string, direct)
    "invlabelname"  → inv.InvLabelName (string, direct)
    "invndc"        → inv.InvNdc (string, direct)
    "invjcode"      → inv.InvJcode (string, direct)

CRITICAL NOTE — RowChkSum is NOT in the switch:
    There is NO "rowchksum" case in SaveInvTypes' switch statement.
    When the loop builds the inv object, inv.RowChkSum remains at the C# default value of 0.

    The update guard then evaluates:
        if (dbtyp.RowChkSum != inv.RowChkSum)    →    if (storedValue != 0)

    Any existing Azure row where RowChkSum was previously stored as a non-zero value will
    always trigger the update block. This means SaveInvTypes performs a full column update
    for every existing inventory type row on every run.

    Practical effect: Because there are only 3-10 inventory type rows per clinic, this is
    not a performance concern. But it does mean LastModAt will be updated on every run,
    and EF will generate UPDATE statements even when no data actually changed.

Step 7 — Match against existing Azure row by Invid
    Models.TblInvtype dbtyp = invtypes.FirstOrDefault(x => x.Invid == inv.Invid);

Step 8 — INSERT or UPDATE (RowChkSum guard always fires for existing rows)

    if (dbtyp == null):
        newinv.Add(inv)         // staged for bulk insert
        rc.RowsIns += 1

    else if (dbtyp.RowChkSum != inv.RowChkSum):    // always true for non-zero stored values
        rc.RowsUpd += 1
        dbtyp.DefaultMed   = inv.DefaultMed
        dbtyp.DisplayName  = inv.DisplayName
        dbtyp.HasBeaker    = inv.HasBeaker
        dbtyp.InvActual    = inv.InvActual
        dbtyp.InvDivision  = inv.InvDivision
        dbtyp.InvJcode     = inv.InvJcode
        dbtyp.InvLabelName = inv.InvLabelName
        dbtyp.InvLiquid    = inv.InvLiquid
        dbtyp.InvMedclass  = inv.InvMedclass
        dbtyp.InvName      = inv.InvName
        dbtyp.InvNdc       = inv.InvNdc
        dbtyp.InvTotal     = inv.InvTotal
        dbtyp.InvUnit      = inv.InvUnit
        dbtyp.IsFilm       = inv.IsFilm
        dbtyp.LastModAt    = inv.LastModAt
        dbtyp.RowChkSum    = inv.RowChkSum    // writes 0 to Azure on every update
        dbtyp.RowState     = inv.RowState
        dbtyp.Type         = inv.Type

    else (RowChkSum == 0 in both — only if Azure also stored 0):
        // No action

Step 9 — Commit all updates in one batch
    db.SaveChanges()

Step 10 — Insert all new rows using AddRange
    if (newinv.Count > 0):
        db.TblInvtype.AddRange(newinv)
        db.SaveChanges()
________________________________________

12. SaveOrientationCheckList — EF Core Path (No RowChkSum, Always Updates)

File: BCAppCode/BHG-DR-LIB/SaveInventory.cs
Class: SaveData (partial class)
Method: SaveOrientationCheckList()

Method signature (note: NO yearly parameter — unique among the four methods in this file):
    public RCodes SaveOrientationCheckList(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted but not used for scoping)
        BHG_DRContext db)        // EF context (created if null)

This method has no RowChkSum comparison at all. Every matched row is always updated.
This is the correct design for orientation checklists — they are relatively low volume
(one per patient admission) and must always reflect current source state including any
amendments or corrections made to the record.

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load ALL existing Azure rows for this site (no date filter)
    OCLs = db.TblOrientationChecklistNew.Where(x => x.SiteCode == sc).ToList()

Step 4 — Initialize new-row staging list
    List<TblOrientationChecklistNew> newOCL = new List<TblOrientationChecklistNew>();

Step 5 — Loop through every row from the SAMMS DataTable

Step 6 — Build a new model object by iterating all DataTable columns

Column mapping switch cases and special handling:

    "sitecode"                          → ocl.SiteCode = sc; ocl.LastModEtl = DateTime.Now
                                          (Note: uses LastModEtl — not LastModAt)
    "checklistid" / "id"                → ocl.CheckListId = int.Parse(value) OR 0 if blank
    "preadmissionid"                    → ocl.PreAdmissionId = int.Parse(value) if length > 0
                                          else ocl.CheckListId = 0  ← LIKELY BUG: should be
                                          ocl.PreAdmissionId = 0, not CheckListId = 0
    "clientid"                          → ocl.ClientId = int.Parse(value) [conditional: length > 0]
    "dataformid"                        → ocl.DataFormId = int.Parse(value) [conditional: length > 0]
    "patientcomplaints"                 → ocl.PatientComplaints = bool.Parse(value) [cond.]
    "accesstoemergency"                 → ocl.AccesstoEmergency = bool.Parse(value) [cond.]
    "codeofethics"                      → ocl.CodeofEthics = bool.Parse(value) [cond.]
    "confidentialitypolicy"             → ocl.ConfidentialityPolicy = bool.Parse(value) [cond.]
    "methods"                           → ocl.Methods = bool.Parse(value) [cond.]
    "explanationoffiancialobligations"  → ocl.ExplanationofFiancialObligations [cond.] (note: typo)
    "rulesforinvoluntarydetox"          → ocl.RulesforInvoluntaryDetox [cond.]
    "firesafety"                        → ocl.FireSafety [cond.]
    "programrulesonpatientparking"      → ocl.ProgramRulesonPatientParking [cond.]
    "policyonrestraint"                 → ocl.PolicyonRestraint [cond.]
    "policyontobaccoproducts"           → ocl.PolicyonTobaccoProducts [cond.]
    "policyonillicit"                   → ocl.PolicyonIllicit [cond.]
    "policyonweapons"                   → ocl.PolicyonWeapons [cond.]
    "knowledgeofnames"                  → ocl.KnowledgeofNames [cond.]
    "programrules"                      → ocl.ProgramRules [cond.]
    "aidshivprevention"                 → ocl.Aidshivprevention [cond.]
    "hepatitisprevention"               → ocl.HepatitisPrevention [cond.]
    "purposeandprocess"                 → ocl.PurposeandProcess [cond.]
    "individualtreatmentplan"           → ocl.IndividualTreatmentPlan [cond.]
    "policyregardingurinedrugscreens"   → ocl.PolicyRegardingUrineDrug [cond.]  (note: long source name
                                          maps to shorter EF property)
    "dischargetransitioncriteria"       → ocl.DischargeTransitionCriteria [cond.]
    "naturalprogression"                → ocl.NaturalProgression [cond.]
    "createdby"                         → ocl.CreatedBy [conditional: length > 0]
    "createdon"                         → ocl.CreatedOn = DateTime.Parse(value) [cond: length > 0]
    "modifiedby"                        → ocl.ModifiedBy [conditional: length > 0]
    "modifiedon"                        → ocl.ModifiedOn = DateTime.Parse(value) [cond: length > 0]
    "isdeleted"                         → ocl.IsDeleted = (value == "1")  [non-bool source: "1"=true]
    "version" / "versionx"             → ocl.Version [conditional: length > 0] (two source aliases)
    "staffsignature"                    → ocl.StaffSignature (string, always set)
    "staffsignaturedate"                → ocl.StaffSignatureDate = DateTime.Parse(value) [cond.]
    "staffsignatureby"                  → ocl.StaffSignatureBy [conditional: length > 0]
    "patientsignature"                  → ocl.PatientSignature (string, always set)
    "patientsignaturedate"              → ocl.PatientSignatureDate = DateTime.Parse(value) [cond.]
    "patientsignatureby"                → ocl.PatientSignatureBy [conditional: length > 0]

Step 7 — Match against existing Azure row by CheckListId
    Models.TblOrientationChecklistNew dbocl =
        OCLs.FirstOrDefault(x => x.CheckListId == ocl.CheckListId);

Step 8 — INSERT or UNCONDITIONAL UPDATE (no RowChkSum comparison)

    if (dbocl == null):
        newOCL.Add(ocl)         // staged for bulk insert
        rc.RowsIns += 1

    else:                       // ALWAYS update — no RowChkSum guard
        rc.RowsUpd += 1
        dbocl.PreAdmissionId                    = ocl.PreAdmissionId
        dbocl.ClientId                          = ocl.ClientId
        dbocl.DataFormId                        = ocl.DataFormId
        dbocl.PatientComplaints                 = ocl.PatientComplaints
        dbocl.AccesstoEmergency                 = ocl.AccesstoEmergency
        dbocl.CodeofEthics                      = ocl.CodeofEthics
        dbocl.ConfidentialityPolicy             = ocl.ConfidentialityPolicy
        dbocl.Methods                           = ocl.Methods
        dbocl.ExplanationofFiancialObligations  = ocl.ExplanationofFiancialObligations
        dbocl.RulesforInvoluntaryDetox          = ocl.RulesforInvoluntaryDetox
        dbocl.FireSafety                        = ocl.FireSafety
        dbocl.ProgramRulesonPatientParking      = ocl.ProgramRulesonPatientParking
        dbocl.PolicyonRestraint                 = ocl.PolicyonRestraint
        dbocl.PolicyonTobaccoProducts           = ocl.PolicyonTobaccoProducts
        dbocl.PolicyonIllicit                   = ocl.PolicyonIllicit
        dbocl.PolicyonWeapons                   = ocl.PolicyonWeapons
        dbocl.KnowledgeofNames                  = ocl.KnowledgeofNames
        dbocl.ProgramRules                      = ocl.ProgramRules
        dbocl.Aidshivprevention                 = ocl.Aidshivprevention
        dbocl.HepatitisPrevention               = ocl.HepatitisPrevention
        dbocl.PurposeandProcess                 = ocl.PurposeandProcess
        dbocl.IndividualTreatmentPlan           = ocl.IndividualTreatmentPlan
        dbocl.PolicyRegardingUrineDrug          = ocl.PolicyRegardingUrineDrug
        dbocl.DischargeTransitionCriteria       = ocl.DischargeTransitionCriteria
        dbocl.NaturalProgression                = ocl.NaturalProgression
        dbocl.CreatedBy                         = ocl.CreatedBy
        dbocl.CreatedOn                         = ocl.CreatedOn
        dbocl.ModifiedBy                        = ocl.ModifiedBy
        dbocl.ModifiedOn                        = ocl.ModifiedOn
        dbocl.IsDeleted                         = ocl.IsDeleted
        dbocl.Version                           = ocl.Version
        dbocl.StaffSignatureDate                = ocl.StaffSignatureDate
        dbocl.StaffSignatureBy                  = ocl.StaffSignatureBy
        dbocl.PatientSignatureDate              = ocl.PatientSignatureDate
        dbocl.PatientSignatureBy                = ocl.PatientSignatureBy
        // Note: StaffSignature and PatientSignature fields (the raw signature values) are
        // NOT included in the update block — only the date and by fields are updated.
        // This is likely intentional: signature fields are write-once.

Step 9 — Commit all updates in one batch
    db.SaveChanges()

Step 10 — Insert all new rows using AddRange
    if (newOCL.Count > 0):
        db.TblOrientationChecklistNew.AddRange(newOCL)
        db.SaveChanges()
________________________________________

13. Destination Tables — Azure BHG_DR

13a. Azure TblBottle (EF Model: TblBottle)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.TblBottle in BHG_DRContext

Primary Key: SiteCode + BottleId (composite)

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar(25)         PK Part 1 — clinic code
BottleId            BottleId            int                 PK Part 2 — bottle ID
RowChkSum           RowChkSum           int                 Change detection hash
LastModAt           LastModAt           datetime            ETL last write timestamp
RowState            RowState            bit (nullable)      true=active
Deanum              Deanum              varchar(?)          DEA number
LotNumber           LotNumber           varchar(?)          Manufacturer lot number
DtReceived          DtReceived          datetime            Date received
Liquid              Liquid              bit (nullable)      Liquid formulation flag
BottleType          BottleType          varchar(?)          Bottle type label
InitialAmount       InitialAmount       int                 Initial inventory count/volume
DtClosed            DtClosed            datetime (nullable) Date closed/depleted
BlnClosed           BlnClosed           bool                Closed flag
StrUser             StrUser             varchar(?)          Username who entered bottle
White               White               bit (nullable)      White bottle indicator
SpecGrav            SpecGrav            decimal (nullable)  Specific gravity
Weight              Weight              decimal (nullable)  Weight measurement
Color               Color               varchar(?)          Color description
InvGroup            InvGroup            varchar(?)          Inventory group category
BrId                BrId                int (nullable)      Batch receipt ID
SiteId              SiteId              int (nullable)      SAMMS site numeric ID
Manufacturer        Manufacturer        varchar(?)          Manufacturer name
ExpDate             ExpDate             datetime (nullable) Expiration date
________________________________________

13b. Azure TblLiquidLog (EF Model: TblLiquidLog)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.TblLiquidLog in BHG_DRContext

Primary Key: SiteCode + LiqId (composite)

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar(25)         Clinic code
LiqId               LiqId               int                 PK — unique log entry ID
RowChkSum           RowChkSum           int                 Change detection hash
LastModAt           LastModAt           datetime            ETL last write timestamp
RowState            RowState            bit (nullable)      true=active
Pump                Pump                Int16 (nullable)    Pump number
DoseId              DoseId              int (nullable)      Linked dose ID
BtlId               BtlId               int (nullable)      Linked bottle ID
BkrId               BkrId               int (nullable)      Beaker ID
Amt                 Amt                 decimal (nullable)  Amount dispensed (0 if overflow guard)
Dtm                 Dtm                 datetime (nullable) Event date/time
Desc                Desc                varchar(?)          Transaction description
Staff               Staff               varchar(?)          Staff member (not updated on existing rows)
BlLogOnly           BlLogOnly           bit (nullable)      Log-only flag
BlPrepack           BlPrepack           bit (nullable)      Pre-pack flag
Memonew             Memonew             varchar(?)          Current memo
Memo                Memo                varchar(?)          Legacy memo
DtRti               DtRti               datetime (nullable) Return-to-inventory date/time
AcknowledgeDate     AcknowledgeDate     varchar(?)          Acknowledgement date (string)
AcknowledgeUser     AcknowledgeUser     varchar(?)          Acknowledging user
RegionalDate        RegionalDate        datetime (nullable) Regional review date
RegionalUser        RegionalUser        varchar(?)          Regional reviewer
ComplainceUser      ComplainceUser      varchar(?)          Compliance user (misspelled — matches source)
ComplianceDate      ComplianceDate      datetime (nullable) Compliance review date
Invgroup            Invgroup            varchar(?)          Inventory group
SiteId              SiteId              int (nullable)      SAMMS site numeric ID
________________________________________

13c. Azure TblInvtype (EF Model: TblInvtype)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.TblInvtype in BHG_DRContext

Primary Key: SiteCode + Invid (composite; Invid=0 for blank source values)

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar(25)         Clinic code
Invid               Invid               int                 PK — inventory type ID (0 if blank)
RowChkSum           RowChkSum           int                 Written as 0 on every update (see note)
LastModAt           LastModAt           datetime            ETL last write timestamp (updated every run)
RowState            RowState            bit (nullable)      true=active
InvName             InvName             varchar(?)          Full medication name
InvLiquid           InvLiquid           bit (nullable)      Liquid formulation flag
InvUnit             InvUnit             int (nullable)      Units per bottle
InvTotal            InvTotal            int (nullable)      Total inventory count
InvDivision         InvDivision         decimal (nullable)  Dilution/concentration factor
DefaultMed          DefaultMed          bit (nullable)      Default medication flag
DisplayName         DisplayName         varchar(?)          Short display name
InvMedclass         InvMedclass         varchar(?)          Medication class code
IsFilm              IsFilm              bit (nullable)      Film formulation flag
Type                Type                varchar(?)          Type category string
HasBeaker           HasBeaker           bit (nullable)      Uses beaker flag
InvActual           InvActual           varchar(?)          Physical inventory description
InvLabelName        InvLabelName        varchar(?)          Label name
InvNdc              InvNdc              varchar(?)          NDC code
InvJcode            InvJcode            varchar(?)          J-code (HCPCS billing code)
________________________________________

13d. Azure TblOrientationChecklistNew (EF Model: TblOrientationChecklistNew)

Location: Azure SQL Database BHG_DR
EF Model: referenced as db.TblOrientationChecklistNew in BHG_DRContext

Primary Key: SiteCode + CheckListId (composite; CheckListId=0 for blank source values)

C# Property (EF)                        SQL Column                          Type
----------------                        ---------------                     ----
SiteCode                                SiteCode                            varchar(25)
CheckListId                             CheckListId                         int
LastModEtl                              LastModEtl                          datetime    (note: different from LastModAt)
PreAdmissionId                          PreAdmissionId                      int (nullable)
ClientId                                ClientId                            int (nullable)
DataFormId                              DataFormId                          int (nullable)
PatientComplaints                       PatientComplaints                   bit (nullable)
AccesstoEmergency                       AccesstoEmergency                   bit (nullable)
CodeofEthics                            CodeofEthics                        bit (nullable)
ConfidentialityPolicy                   ConfidentialityPolicy               bit (nullable)
Methods                                 Methods                             bit (nullable)
ExplanationofFiancialObligations        ExplanationofFiancialObligations    bit (nullable)  [typo preserved]
RulesforInvoluntaryDetox                RulesforInvoluntaryDetox            bit (nullable)
FireSafety                              FireSafety                          bit (nullable)
ProgramRulesonPatientParking            ProgramRulesonPatientParking        bit (nullable)
PolicyonRestraint                       PolicyonRestraint                   bit (nullable)
PolicyonTobaccoProducts                 PolicyonTobaccoProducts             bit (nullable)
PolicyonIllicit                         PolicyonIllicit                     bit (nullable)
PolicyonWeapons                         PolicyonWeapons                     bit (nullable)
KnowledgeofNames                        KnowledgeofNames                    bit (nullable)
ProgramRules                            ProgramRules                        bit (nullable)
Aidshivprevention                       Aidshivprevention                   bit (nullable)
HepatitisPrevention                     HepatitisPrevention                 bit (nullable)
PurposeandProcess                       PurposeandProcess                   bit (nullable)
IndividualTreatmentPlan                 IndividualTreatmentPlan             bit (nullable)
PolicyRegardingUrineDrug                PolicyRegardingUrineDrug            bit (nullable)
DischargeTransitionCriteria             DischargeTransitionCriteria         bit (nullable)
NaturalProgression                      NaturalProgression                  bit (nullable)
CreatedBy                               CreatedBy                           varchar(?)
CreatedOn                               CreatedOn                           datetime (nullable)
ModifiedBy                              ModifiedBy                          varchar(?)
ModifiedOn                              ModifiedOn                          datetime (nullable)
IsDeleted                               IsDeleted                           bit         [derived: "1"=true]
Version                                 Version                             varchar(?)  [source aliases: version, versionx]
StaffSignature                          StaffSignature                      varchar(?)  [not updated on existing rows]
StaffSignatureDate                      StaffSignatureDate                  datetime (nullable)
StaffSignatureBy                        StaffSignatureBy                    varchar(?)
PatientSignature                        PatientSignature                    varchar(?)  [not updated on existing rows]
PatientSignatureDate                    PatientSignatureDate                datetime (nullable)
PatientSignatureBy                      PatientSignatureBy                  varchar(?)
________________________________________

14. Change Detection — RowChkSum (and Absence Thereof)

The RowChkSum column is the standard efficiency mechanism for ETL updates in this system.
Its presence and behavior differs across the four methods in this file:

Method                      RowChkSum Used?     Behavior
------                      ---------------     --------
SaveBottles                 YES                 Proper guard: update only if checksum changed
SaveLiquidlog               YES                 Proper guard: update only if checksum changed
SaveInvTypes                PARTIAL             RowChkSum NOT mapped in switch; always writes 0
                                                to inv.RowChkSum; comparison always fires for
                                                existing rows with stored non-zero checksums
SaveOrientationCheckList    NO                  No RowChkSum field; every matched row updated

How RowChkSum is computed (at source, during SELECT by SelectConstructor):

For SaveBottles example:
    RowChkSum = CHECKSUM(
        bottleid, deanum, lotnumber, dtreceived, liquid, bottletype, initialamount,
        dtclosed, blnclosed, struser, white, specgrav, weight, color, invgroup,
        brid, siteid, manufacturer, expdate
    )

How it is used (SaveBottles and SaveLiquidlog):
    if (dbBottle.RowChkSum != btl.RowChkSum) { ... map all columns ... }
    Rows with matching checksums: no columns written (no EF dirty tracking).

What this means in practice:
- A clinic with 500 liquid log entries but only 5 updated today generates 5 column-level
  updates. The other 495 rows pass through FirstOrDefault() but generate no SQL.
- SaveInvTypes: typically 3-10 rows per clinic, so the overhead of always updating is
  negligible. All rows get LastModAt refreshed on every run.
- SaveOrientationCheckList: checklists are low volume per clinic; always-update is by design
  to ensure signed records always reflect current source state.
________________________________________

15. RowState — Active Record Tracking

RowState is a bit column (nullable) used as an active/inactive flag.

Value       Meaning
-----       -------
true (1)    Row is active — written by this ETL run
false (0)   Row may be stale — soft-deleted flag not actively managed here
NULL        Row has never been written with a RowState value

How RowState is managed across the four methods:

SaveBottles:
    RowState = true is set when building the btl object (via the "sitecode" switch case)
    and is copied to existing rows on update. There is no soft-delete reset cycle.
    Rows not seen in a given run simply retain their last RowState.

SaveLiquidlog:
    RowState = true is set when building the lg object. Same behavior as SaveBottles.
    No soft-delete cycle.

SaveInvTypes:
    RowState = true is set when building the inv object. Written on every update (since
    RowChkSum guard always fires). Same as above — no soft-delete cycle.

SaveOrientationCheckList:
    RowState is NOT set or read in SaveOrientationCheckList.
    The IsDeleted column serves the equivalent purpose — it comes directly from the source
    record's deleted flag and is always written on update.

Note: Unlike SaveClaims or Save3pElig, none of the methods in SaveInventory.cs implement
the soft-delete pattern (pre-loop RowState=false reset, then re-activate per row found).
RowState in these tables is a simple "was this row last touched by ETL" indicator rather
than a true active/deleted marker derived by comparison.
________________________________________

16. Load Scoping Summary

Each method scopes its Azure load differently:

Method                      Azure Load Scope                        Notes
------                      ----------------                        -----
SaveBottles                 SiteCode == sc (all-time full load)     Appropriate for long-lived bottle records
SaveLiquidlog               SiteCode == sc (all-time full load)     Date filter commented out; may be
                                                                    memory-intensive for high-volume sites
SaveInvTypes                SiteCode == sc (all-time full load)     OK — 3-10 rows per clinic
SaveOrientationCheckList    SiteCode == sc (all-time full load)     OK — one row per patient admission

The commented-out date filter in SaveLiquidlog:
    //&& x.Dtm >= wrkdt.AddDays(-15)
If re-enabled, this would limit the Azure working set to the most recent 15 days of liquid
log entries. This would reduce memory pressure but would also prevent updates to liquid log
entries older than 15 days (since they would not be found in the in-memory list, and would
instead be treated as new inserts, creating duplicates). Any re-activation of this filter
would need to be aligned with the WHERE clause in the source SELECT to prevent duplicate
inserts.
________________________________________

17. Load Design Summary

Load type: Incremental upsert with checksum-based change detection (Bottles, LiquidLog)
           Full replace every run (InvTypes), Always-update (OrientationCheckList)

Per-run behavior:

SaveBottles (Azure TblBottle):
    1. Load all Azure rows for this site (no date filter)
    2. For each SAMMS row (dynamic column switch):
       - Build btl object; RowState=true always set
       - Not found in Azure → stage in newbottles; RowsIns++
       - RowChkSum differs → UPDATE all 21 fields; RowsUpd++
       - RowChkSum matches → skip (no write)
    3. db.SaveChanges() — commit all updates in one batch
    4. db.TblBottle.AddRange(newbottles); db.SaveChanges() — insert new rows

SaveLiquidlog (Azure TblLiquidLog):
    1. Load all Azure rows for this site (date filter commented out)
    2. For each SAMMS row (dynamic column switch):
       - Build lg object; overflow guard on amt; RowState=true always set
       - Not found in Azure → stage in newlogs; RowsIns++
       - RowChkSum differs → UPDATE 22 fields (Staff not in update block); RowsUpd++
       - RowChkSum matches → skip (no write)
    3. db.SaveChanges() — batch commit
    4. db.TblLiquidLog.AddRange(newlogs); db.SaveChanges()

SaveInvTypes (Azure TblInvtype):
    1. Load all Azure rows for this site (no date filter)
    2. For each SAMMS row (dynamic column switch):
       - Build inv object; RowState=true; inv.RowChkSum stays 0 (not in switch)
       - Not found in Azure → stage in newinv; RowsIns++
       - RowChkSum != 0 (always true for non-zero Azure values) → UPDATE all fields; RowsUpd++
    3. db.SaveChanges() — batch commit
    4. db.TblInvtype.AddRange(newinv); db.SaveChanges()

SaveOrientationCheckList (Azure TblOrientationChecklistNew):
    1. Load all Azure rows for this site (no date filter)
    2. For each SAMMS row (dynamic column switch):
       - Build ocl object; IsDeleted from "1" string; version/versionx aliases
       - Not found in Azure → stage in newOCL; RowsIns++
       - Found → ALWAYS UPDATE 31 fields; RowsUpd++ (no RowChkSum guard)
       - StaffSignature and PatientSignature not updated (write-once pattern)
    3. db.SaveChanges() — batch commit
    4. db.TblOrientationChecklistNew.AddRange(newOCL); db.SaveChanges()

Per-record identity:
    SaveBottles              → SiteCode + BottleId
    SaveLiquidlog            → SiteCode + LiqId
    SaveInvTypes             → SiteCode + Invid (0 for blank source)
    SaveOrientationCheckList → SiteCode + CheckListId (0 for blank source)
________________________________________

18. Known Field Anomalies and Behavioral Notes

1. SaveLiquidlog — amt overflow guard (690122921):
   If source amt value equals "690122921", the ETL substitutes 0.
   This is a known bad data sentinel from a legacy SAMMS bug. Without this guard, decimal
   parsing would succeed (it is a valid integer) but the value would be stored as a
   meaningless large number in dispensing records.

2. SaveLiquidlog — Staff field not updated:
   The "staff" case maps Staff during the object-build phase but Staff is not included in
   the dbll update block. If a liquid log entry's staff field changes, that change is
   silently not propagated to Azure on subsequent runs (only new rows get the Staff value).

3. SaveLiquidlog — ComplainceUser (misspelled field):
   The source SAMMS column is named "complainceuser" (misspelled) and the EF property is
   "ComplainceUser" to match. The Azure column carries the same typo. This is intentional
   to preserve the mapping — do not rename without updating both SAMMS ETL and Azure schema.

4. SaveInvTypes — RowChkSum always 0:
   No "rowchksum" case exists in SaveInvTypes' switch. inv.RowChkSum stays at C# default
   (0). The RowChkSum written to Azure after each update is also 0. This means all
   inventory type rows will be updated on every ETL run.

5. SaveOrientationCheckList — no yearly parameter:
   SaveOrientationCheckList is the only method in this file that does not accept a `yearly`
   bool parameter. This means it cannot be called from SelectConstructor pathways that pass
   the yearly flag. It can only be called from code that explicitly omits the parameter.

6. SaveOrientationCheckList — preadmissionid else branch:
   When preadmissionid is blank, the else branch sets ocl.CheckListId = 0 instead of
   ocl.PreAdmissionId = 0. This may overwrite a correctly parsed CheckListId with 0 if
   the "preadmissionid" column happens to be processed AFTER "checklistid" in column order.
   Whether this actually causes problems depends on DataTable column iteration order and
   whether blank preadmissionid values are common.

7. SaveOrientationCheckList — StaffSignature / PatientSignature not in update block:
   The raw signature fields (StaffSignature, PatientSignature) are mapped during object
   build (always set, no length guard) but are NOT included in the dbocl update block.
   Existing rows retain their original signature field values. Only new rows get the
   signature field written. This appears intentional — treating signatures as write-once.

8. SaveOrientationCheckList — uses LastModEtl, not LastModAt:
   All other methods in SaveInventory.cs set LastModAt. SaveOrientationCheckList sets
   LastModEtl. These are different columns in the Azure model. Queries filtering by
   LastModAt will not see orientation checklist timestamps; queries must use LastModEtl.

9. SaveOrientationCheckList — ExplanationofFiancialObligations (source typo):
   The source SAMMS column name contains a typo ("Fianc" instead of "Financ"). This typo
   is preserved in both the switch case and the EF property name to maintain the mapping.
________________________________________

19. Error Handling and Recovery

All four methods share the same try/catch error handling pattern:

    try
    {
        // ... full EF Core loop + SaveChanges() calls ...
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

All four methods use two db.SaveChanges() calls:

First call (inside the foreach loop scope, after the full row iteration):
    db.SaveChanges() — commits all in-memory EF update tracking
    If this fails, no updates from this run are written.

Second call (outside the loop, AddRange for new rows):
    db.AddRange(newList); db.SaveChanges()
    If the first SaveChanges() succeeded but the second fails:
    - All updates are already committed
    - New rows (newbottles / newlogs / newinv / newOCL) are lost for this run
    - They will be re-created as new inserts on the next run since they will still not
      be found in the Azure load

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

A failed inventory run for a clinic will automatically be retried the next day.
________________________________________

20. RowTrax — Audit and Row Count Tracking

Table: tsk.tbl_RowTrax (Azure BHG_DR)

After each successful load for a clinic (if st.RowTrax == true):

    sd.SaveRowTrax(
        st.SiteCode,           -- e.g. "VBRA"
        st.WorkDate,           -- today
        st.TaskName,           -- e.g. "inv.tbl_bottle"
        SrcDt.Rows.Count,      -- rows returned from SAMMS this run
        destCount,             -- count in Azure
        null)

Destination count query (run against Azure):
    Select count(1) from [destination table]
    where SiteCode = 'VBRA'   (and RowState = 1 where applicable)

Note: The source count is the DataTable row count from the incremental SAMMS fetch.
For tables without a date filter (Bottles, InvTypes) the source DataTable may represent the
full historical set for the clinic, making the source count a total count, not a delta count.

The stored RowTrax records allow analysts to monitor inventory record volumes over time and
detect clinics where data loads are lagging or diverging from expected counts.
________________________________________

21. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-INV (Status=17)
        |-- insert child tasks per clinic:
        |       inv.tbl_bottle x 80 clinics
        |       inv.tbl_liquidlog x 80 clinics
        |       inv.tbl_invtype x 80 clinics
        |       pat.tbl_orientationchecklistnew x 80 clinics
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
        |       SelectConstructor.GetSLT() → builds SELECT + CHECKSUM()
        |       build WHERE clause (date range filter, varies by table)
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
        |---[TaskName = inv.tbl_bottle]
        |           |
        |           V
        |   SaveBottles() [EF CORE — RowChkSum guard]
        |           |
        |   load ALL Azure rows for site (no date filter)
        |   for each row (dynamic switch):
        |     build btl object; RowState=true
        |     match by BottleId
        |     UPDATE if RowChkSum changed (21 fields)
        |     stage new rows in newbottles
        |   db.SaveChanges() [update batch]
        |   AddRange(newbottles); db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure TblBottle
        |
        |---[TaskName = inv.tbl_liquidlog]
        |           |
        |           V
        |   SaveLiquidlog() [EF CORE — RowChkSum guard]
        |           |
        |   load ALL Azure rows for site (date filter commented out)
        |   for each row (dynamic switch):
        |     build lg object; amt overflow guard (690122921 → 0)
        |     match by LiqId
        |     UPDATE if RowChkSum changed (22 fields, excl. Staff)
        |     stage new rows in newlogs
        |   db.SaveChanges() [update batch]
        |   AddRange(newlogs); db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure TblLiquidLog
        |
        |---[TaskName = inv.tbl_invtype]
        |           |
        |           V
        |   SaveInvTypes() [EF CORE — RowChkSum always 0, always updates]
        |           |
        |   load ALL Azure rows for site (no date filter)
        |   for each row (dynamic switch):
        |     build inv object; RowChkSum stays 0 (not in switch)
        |     match by Invid
        |     UPDATE always fires (stored RowChkSum != 0 always true)
        |     stage new rows in newinv
        |   db.SaveChanges() [update batch]
        |   AddRange(newinv); db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure TblInvtype
        |
        |---[TaskName = pat.tbl_orientationchecklistnew]
        |           |
        |           V
        |   SaveOrientationCheckList() [EF CORE — no RowChkSum, always updates]
        |           |
        |   load ALL Azure rows for site (no date filter)
        |   for each row (dynamic switch):
        |     build ocl object; IsDeleted from "1" string; version/versionx aliases
        |     match by CheckListId
        |     UPDATE always (no RowChkSum guard); 31 fields written
        |     signature fields NOT updated on existing rows (write-once)
        |     stage new rows in newOCL
        |   db.SaveChanges() [update batch]
        |   AddRange(newOCL); db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure TblOrientationChecklistNew
        |
        V
RowTrax audit saved to tsk.tbl_RowTrax
        |
        V
BHGTaskRunner marks task Status=20 (complete)
________________________________________

22. File Reference Map

File Path                                               Purpose
---------                                               -------
BCAppCode/Scheduler/Program.cs                          Creates daily task queue for all ETL pipelines
BCAppCode/BHGTaskRunner/Program.cs                      Main ETL driver (arg=8 → Inventory pipeline)
BCAppCode/BHG-DR-LIB/SelectConstructor.cs               Builds SELECT + CHECKSUM() from metadata
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                   ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/SaveInventory.cs                   EF Core upsert — SaveBottles, SaveLiquidlog,
                                                         SaveInvTypes, SaveOrientationCheckList
BCAppCode/BHG-DR-LIB/Models/TblBottle.cs                EF Model → Azure TblBottle
BCAppCode/BHG-DR-LIB/Models/TblLiquidLog.cs             EF Model → Azure TblLiquidLog
BCAppCode/BHG-DR-LIB/Models/TblInvtype.cs               EF Model → Azure TblInvtype
BCAppCode/BHG-DR-LIB/Models/TblOrientationChecklistNew.cs EF Model → Azure TblOrientationChecklistNew
BCAppCode/BHG-DR-LIB/BHG_DRContext.cs                   EF DbContext — db.TblBottle, db.TblLiquidLog,
                                                         db.TblInvtype, db.TblOrientationChecklistNew
________________________________________

23. Quick Reference Summary

What triggers Inventory ETL?        Scheduler.exe creates tasks, BHGTaskRunner.exe 8 processes them
TaskName in scheduler?              SAMMS-ETL-INV (same schedule as Claims and 3p Insurance)
Source tables in SAMMS?             dbo.tblBottle, dbo.tblLiquidLog, dbo.tblInvType,
                                    dbo.tblOrientationChecklist
Destination tables in Azure?        TblBottle, TblLiquidLog, TblInvtype, TblOrientationChecklistNew
Primary key (Bottles)?              SiteCode + BottleId (composite)
Primary key (LiquidLog)?            SiteCode + LiqId (composite)
Primary key (InvTypes)?             SiteCode + Invid (composite; 0 if blank)
Primary key (OrientationCheckList)? SiteCode + CheckListId (composite; 0 if blank)
EF Core or Bulk path?               All four methods use EF Core only — no bulk path
How is change detected (Bottles)?   RowChkSum = CHECKSUM() — proper guard, updates only when changed
How is change detected (LiquidLog)? RowChkSum = CHECKSUM() — proper guard, updates only when changed
How is change detected (InvTypes)?  RowChkSum NOT mapped in switch; always 0; all rows updated every run
How is change detected (OCL)?       No RowChkSum — always updates every matched row (no guard)
What is RowState?                   Active flag — true set on write; no soft-delete reset cycle in this file
Azure load scope?                   All four methods: full site load (no date filter)
                                    Exception: LiquidLog had date filter commented out (wrkdt.AddDays(-15))
Batched insert optimization?        All four methods: new rows staged in list, AddRange() used after update batch
amt overflow guard?                 SaveLiquidlog: if amt == "690122921" → 0 (known bad sentinel)
Staff field not updated?            SaveLiquidlog: Staff mapped during build but missing from update block
ComplainceUser typo?                SaveLiquidlog: source/Azure both misspelled — do not rename
OrientationCheckList signature?     StaffSignature and PatientSignature are write-once — not in update block
OrientationCheckList LastModAt?     Uses LastModEtl instead of LastModAt — different column from all others
yearly parameter?                   SaveOrientationCheckList does NOT accept yearly; all others do
IsDeleted in OCL?                   Derived from string comparison: "1"=true, else false (not bool.Parse)
version/versionx?                   Both source column name aliases map to the same Version property
RowTrax audit?                      Source DataTable row count vs count of Azure rows per site
Error recovery?                     Scheduler resets failed tasks to Status=17 on next daily run
PHC handled here?                   No — PHC uses its own runner (PHC/Program.cs)
