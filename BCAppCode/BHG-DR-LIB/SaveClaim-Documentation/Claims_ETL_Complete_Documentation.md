
Claims ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract insurance claim records
(Claims, ClaimLineItems, ClaimLineItemActivity) from local SAMMS SQL Server databases at each
clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What Claims data is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How the two load paths work: SaveClaims (EF Core) vs BulkDartsSrvLoader (Bulk)
- How SelectConstructor builds the source SQL
- How SQLSvrManager executes against the clinic databases
- What the source tables look like and all their columns
- What the destination tables look like and all their columns
- How change detection works using RowChkSum
- How RowState tracks soft-deleted / active records
- How CleanupDeletedData reconciles deleted source records
- How RowTrax audit tracking works
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What are Claims?

A Claim record in SAMMS represents a single insurance billing claim submitted (or to be submitted)
to a payer (insurance company, Medicaid, Medicare) for services rendered to a patient at a BHG
clinic. The claim contains all the information from a CMS-1500 paper claim form — patient
demographics, insurer details, diagnosis codes, dates of service, billing amounts, and signature
fields.

The Claims pipeline actually manages three related tables that together form the full billing
picture:

1. tbl3pClaim (dbo in SAMMS)       → pats.tbl_Claims (Azure)
   One row per claim. Header-level billing record. Contains all CMS-1500 form fields.

2. tbl3pClaimLineItem (dbo)        → pats.tbl_ClaimLineItem (Azure)
   One row per service line within a claim. Each claim may have multiple line items
   representing individual CPT procedures billed.

3. tbl3pClaimLineItemActivity (dbo) → pats.tbl_ClaimLineItemActivity (Azure)
   One row per payment/adjustment activity against a line item. Tracks EOB (Explanation
   of Benefits) postings: amounts paid, adjustments, denials, copays, deductibles.

Why it is important

The Claims dataset is the financial backbone of the BHG data warehouse. It enables:
- Billing reconciliation between what was submitted to payers and what was paid
- Denial rate analysis by payer, CPT code, clinic, and counselor
- Revenue cycle management reporting across all clinics
- Compliance with payer contract terms

Load type

Two paths exist depending on site code:

EF Core path (SaveClaims) — used for: VBRA, VMIN, VWBY, VBRP
  Row-by-row upsert. Loads all Azure rows for the site into memory, compares RowChkSum
  per row, writes only changed or new rows.

Bulk path (BulkDartsSrvLoader) — used for: all other clinics
  SqlBulkCopy into a staging table, then stored procedures MERGE into the final table.
  This is the same engine used for DartsSrv, Dose, Orders, and ClaimLineItem/Activity.

Note: ClaimLineItem and ClaimLineItemActivity always use the Bulk path regardless of site code.
________________________________________

3. Systems Involved

System / File                        Role
-----------                          ----
tsk.tbl_Schedule (Azure DB)          Configuration — defines schedules and their run times
Scheduler.exe                        Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=8              Main ETL orchestrator for SAMMS-ETL-INV (Claims)
dms.tbl_MapSrc2Dsn (Azure DB)        Metadata — defines which columns to SELECT for each ActionKey
SelectConstructor.cs                 Assembles SELECT statement from metadata
SQLSvrManager.cs                     Fires SELECT against the clinic SAMMS SQL Server
SaveClaims.cs / SaveData             EF Core upsert for claims header (VBRA/VMIN/VWBY/VBRP only)
BulkDartsSvc.cs                      SqlBulkCopy + merge stored procedure path (all other sites)
pats.tbl_Claims (Azure)              Final destination for claim headers
pats.tbl_ClaimLineItem (Azure)       Final destination for claim line items
pats.tbl_ClaimLineItemActivity (Azure) Final destination for payment/adjustment activity
ctrl.tbl_LocationCons (Azure)        Connection strings for each clinic's SAMMS SQL Server
tsk.tbl_RowTrax (Azure)              Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How Claims Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily (typically overnight or early morning) and populates the task
queue for all ETL pipelines. It does NOT move data — it only creates tasks.

What the Scheduler does for Claims (SAMMS-ETL-INV)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Tasks from previous days that were never picked up are marked as expired (RowState=26).

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For Claims, there is a row with:
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
           ma.DsnSchema + '.' + ma.DsnTbl,  -- e.g. 'pats.tbl_claims'
           ma.ActionKey,                     -- = 8
           ma.StepKey,
           ma.SiteCode                       -- = 'B01', 'VBRA', etc.
    from dms.vw_MapAction ma
    cross join tsk.tbl_Tasks2 t
    where ma.Enabled = 1
      and ma.IsActive = 1
      and case when ma.DsnSchema + '.' + ma.DsnTbl in
               ('pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity')
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
        TaskName = 'pats.tbl_claims'
        SiteCode = 'VBRA'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_claims'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_claimlineitem'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_claimlineitemactivity'
        SiteCode = 'B01'
        ActionKey = 8
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How Claims Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 8 to process only the SAMMS-ETL-INV schedule.

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
- Reading all enabled column mappings for ActionKey=8
- Building a CHECKSUM(...) expression across all mapped columns to produce RowChkSum
- Replacing placeholder tokens (@SiteCode, @WorkDate, @Samms)

Step 6 — Build the WHERE clause
The WHERE clause filters by date using DaysBack (typically -14 for claims):

    strCmd += " where " + strWhere + " " + st.SortOrder;

strWhere typically filters on tpcCreatedDate or tpcClaimBatchID range.

Step 7 — Execute SELECT against SAMMS
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);

Returns a DataTable with all recently touched claim rows from this clinic.

Step 8 — Route to EF Core or Bulk path based on site code

For pats.tbl_claims:

    if site is VBRA, VMIN, VWBY, or VBRP:
        rCodes = sd.SaveClaims(SrcDt, st.SiteCode, WorkDate.AddDays(DaysBack), true, null)
        // EF Core row-by-row upsert
    else:
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claims", st.SiteCode, WorkDate, db)
        // SqlBulkCopy + merge stored procedure

For pats.tbl_claimlineitem (all sites → Bulk only):
    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claimlineitem", st.SiteCode, WorkDate, null)

For pats.tbl_claimlineitemactivity (all sites → Bulk only):
    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claimlineitemactivity", st.SiteCode, WorkDate, null)

Step 9 — RowTrax audit (if enabled for this task)
If st.RowTrax == true:

    Source count:  SrcDt.Rows.Count  (rows returned from SAMMS)

    Destination count:
        Select count(1) from pats.tbl_claims
        where SiteCode = 'VBRA' and RowState = 1

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

For Claims (ActionKey=8), the SELECT looks conceptually like this:

    Select
        SiteCode = 'VBRA',
        tpcID,
        tpccltID,
        tpcStrStatus,
        tpcStrPayer,
        tpcDtmAdded,
        tpcStrAdded,
        tpcClaimBatchID,
        tpcStrPrimary,
        tpcCreatedDate,
        tpcEncounter,
        tpcREBILLREASON,
        tpcStrWeek,
        tpcWKSTART,
        tpcPayerCIN,
        tpcSrvType,
        tpcClaimType,
        SiteID,
        tpcDBnotes,
        tpcReferring,
        f1id, f2name, f3dob, f3sex,
        f4insname,
        f5add, f5city, f5phone, f5state, f5zip,
        f6insrel,
        f7insadd, f7inscity, f7insphone, f7insstate, f7inszip,
        f8stat,
        f9othinsdob, f9othinsemp, f9othinsname, f9othinsnumber, f9othinsplan, f9othinssex,
        f10auto, f10employ, f10local, f10oth,
        f11insanother, f11insdob, f11insemploy, f11insnumber, f11insplan, f11inssex,
        f12sig, f12sigdate,
        f13inssig,
        f14date, f15firstdate,
        f16dateunableend, f16dateunablestart,
        f17refername, f17refernpi,
        f18datehospend, f18datehospstart,
        f19local,
        f20outsidelab,
        f21diag1, f21diag2, f21diag3, f21diag4,
        f22medresub,
        f23priorauth,
        f25taxid,
        f26account,
        f27assign,
        f28totalcharge, f29amtpaid,
        f30balancedue,
        f31date, f31phys,
        f32a, f32b, f32line1, f32line2, f32line3, f32line4,
        f33a, f33b, f33line1, f33line2, f33line3, f33line4, f33phone,
        RowChkSum = CHECKSUM(tpcID, tpccltID, tpcStrStatus, tpcStrPayer, ...)
    from dbo.tbl3pClaim

The CHECKSUM() expression across all mapped columns produces the RowChkSum used for change
detection.

SelectConstructor also routes tbl_claims to SaveClaims on the backfill path:

    case "tbl_claims":
        rcodes = sd.SaveClaims(SrcDt, st.SiteCode, wrkdate, true, null)
________________________________________

7. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL Server,
executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'VBRA'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_VBRA;User Id=...;Password=...;'

The DataTable returned contains all rows from dbo.tbl3pClaim for this clinic that match the
WHERE clause. This DataTable is passed directly into SaveClaims or BulkDartsSrvLoader.
________________________________________

8. Source Tables — SAMMS SQL Server (dbo)

All three source tables live in the clinic's local SAMMS SQL Server database under the dbo schema.

________________________________________
8a. dbo.tbl3pClaim — Claim Headers

Primary Key: tpcID (unique per clinic)

Column Name             Type            Description
-----------             ----            -----------
tpcID                   int             Unique claim ID within this clinic
tpccltID                int             Patient/client ID linked to this claim
SiteCode                varchar(25)     Added by ETL — clinic identifier (e.g. 'VBRA')
tpcStrStatus            varchar(100)    Current claim status (e.g. 'Submitted', 'Paid', 'Denied')
tpcStrPayer             varchar(100)    Payer name (insurance company)
tpcDtmAdded             datetime        Date/time the claim record was created in SAMMS
tpcStrAdded             varchar(100)    Username who created the claim
tpcClaimBatchID         int             Batch ID this claim belongs to (tbl3pClaimBatch)
tpcStrPrimary           varchar(100)    Primary payer identifier
tpcCreatedDate          datetime        Claim creation date (may differ from DtmAdded)
tpcEncounter            varchar(10)     Encounter/visit reference
tpcREBILLREASON         varchar(500)    Reason if claim is being rebilled
tpcStrWeek              varchar(25)     Week identifier for weekly billing cycles
tpcWKSTART              date            Start date of the billing week
tpcPayerCIN             varchar(100)    Payer client identification number (CIN)
tpcSrvType              varchar(50)     Service type billed
tpcClaimType            int             Claim type code (e.g. professional, institutional)
SiteID                  int             SAMMS internal site numeric ID
tpcDBnotes              varchar(1000)   Administrative/database notes
tpcReferring            varchar(10)     Referring provider code

CMS-1500 Form Fields (all varchar(50) unless noted):
f1id            Field 1  — Payer/insurance plan identifier
f2name          Field 2  — Patient name
f3dob           Field 3  — Patient date of birth
f3sex           Field 3  — Patient sex (varchar(10))
f4insname       Field 4  — Insured's name
f5add           Field 5  — Patient address line 1
f5city          Field 5  — Patient city
f5phone         Field 5  — Patient phone
f5state         Field 5  — Patient state
f5zip           Field 5  — Patient zip code
f6insrel        Field 6  — Patient relationship to insured
f7insadd        Field 7  — Insured's address line 1
f7inscity       Field 7  — Insured's city
f7insphone      Field 7  — Insured's phone
f7insstate      Field 7  — Insured's state
f7inszip        Field 7  — Insured's zip
f8stat          Field 8  — Patient status
f9othinsdob     Field 9  — Other insured's date of birth
f9othinsemp     Field 9  — Other insured's employer
f9othinsname    Field 9  — Other insured's name
f9othinsnumber  Field 9  — Other insured's policy number
f9othinsplan    Field 9  — Other insured's plan name
f9othinssex     Field 9  — Other insured's sex
f10auto         Field 10 — Auto accident indicator
f10employ       Field 10 — Employment indicator
f10local        Field 10 — Other accident/local indicator
f10oth          Field 10 — Other accident indicator
f11insanother   Field 11 — Is there another health plan?
f11insdob       Field 11 — Insured's date of birth
f11insemploy    Field 11 — Insured's employer name
f11insnumber    Field 11 — Insured's policy number
f11insplan      Field 11 — Insurance plan name
f11inssex       Field 11 — Insured's sex
f12sig          Field 12 — Patient signature
f12sigdate      Field 12 — Date of patient signature
f13inssig       Field 13 — Insured's authorization signature
f14date         Field 14 — Date of current illness/injury/pregnancy
f15firstdate    Field 15 — First date of similar illness
f16dateunableend   Field 16 — Date unable to work end
f16dateunablestart Field 16 — Date unable to work start
f17refername    Field 17 — Referring/ordering provider name
f17refernpi     Field 17 — Referring provider NPI number
f18datehospend  Field 18 — Hospitalization dates end
f18datehospstart Field 18 — Hospitalization dates start
f19local        Field 19 — Local use field
f20outsidelab   Field 20 — Outside lab indicator
f21diag1        Field 21 — Diagnosis code 1 (ICD-10)
f21diag2        Field 21 — Diagnosis code 2
f21diag3        Field 21 — Diagnosis code 3
f21diag4        Field 21 — Diagnosis code 4
f22medresub     Field 22 — Medicaid resubmission code
f23priorauth    Field 23 — Prior authorization number
f25taxid        Field 25 — Federal tax ID number
f26account      Field 26 — Patient account number
f27assign       Field 27 — Accept assignment indicator
f28totalcharge  Field 28 — Total charge amount
f29amtpaid      Field 29 — Amount paid
f30balancedue   Field 30 — Balance due
f31date         Field 31 — Signature date
f31phys         Field 31 — Physician signature
f32a            Field 32 — Service facility NPI (part a)
f32b            Field 32 — Service facility taxonomy (part b)
f32line1        Field 32 — Service facility name
f32line2        Field 32 — Service facility address line 1
f32line3        Field 32 — Service facility address line 2
f32line4        Field 32 — Service facility city/state/zip
f33a            Field 33 — Billing provider NPI (part a)
f33b            Field 33 — Billing provider taxonomy (part b)
f33line1        Field 33 — Billing provider name
f33line2        Field 33 — Billing provider address
f33line3        Field 33 — Billing provider city/state
f33line4        Field 33 — Billing provider zip
f33phone        Field 33 — Billing provider phone
RowChkSum       int      — Computed by ETL: CHECKSUM() across all mapped columns

Note: RowChkSum is NOT a column in tbl3pClaim. It is computed during SELECT by
SelectConstructor using SQL Server's CHECKSUM() function.
________________________________________

8b. dbo.tbl3pClaimLineItem — Claim Line Items

Primary Key: tpcliID (unique per clinic)

Column Name         Type            Description
-----------         ----            -----------
tpcliID             int             Unique line item ID within this clinic
SiteCode            varchar(25)     Added by ETL — clinic identifier
tpcliTPCID          int             Parent claim ID (links to tbl3pClaim.tpcID)
tpcliDtmService     datetime        Date of service for this line item
tpcliTxtService     varchar(?)      Service description text
tpcliIntUnits       int             Number of service units
tpcliDtmAdded       datetime        Date line item was added
tpcliAmtCharge      decimal         Charged amount for this line item
tpcliStrAdded       varchar(?)      Username who added the line item
tpcliStrCPT         varchar(?)      CPT (procedure) code billed
tpcliStrModifier    varchar(?)      CPT modifier codes
tpcliStrNDC         varchar(?)      NDC (drug) code if applicable
tpcliStrPOS         varchar(?)      Place of service code
tpcliIntDx1         int             Diagnosis pointer 1 (references f21diag1-4)
tpcliIntDx2         int             Diagnosis pointer 2
tpcliIntDx3         int             Diagnosis pointer 3
tpcliIntDx4         int             Diagnosis pointer 4
tpcliDiagnosis      varchar(?)      Diagnosis code text
tpcliDSID           int             Linked DartsSrv session ID (dsID in tblDartsSrv)
tpcliPayerClaimID   varchar(?)      Payer-assigned claim ID
tpcliProviderID     varchar(?)      Rendering provider ID
tpcliUnitfee        decimal         Fee per unit
tpcliVoid           bit             Void indicator
tpcliVoidDT         date            Date line item was voided
tpcliVoidUser       varchar(?)      Username who voided the line item
tpcliDtmServiceTo   datetime        Service end date (if a date range)
tpcliIntMG          int             Milligrams (for MAT/methadone programs)
tpcliDBnotes        varchar(?)      Administrative/database notes
RowChkSum           int             Computed by ETL: CHECKSUM() across all mapped columns
________________________________________

8c. dbo.tbl3pClaimLineItemActivity — Payment/Adjustment Activity

Primary Key: liaID (unique per clinic)

Column Name     Type            Description
-----------     ----            -----------
liaID           int             Unique activity ID within this clinic
SiteCode        varchar(25)     Added by ETL — clinic identifier
liaTPCLIID      int             Parent line item ID (links to tbl3pClaimLineItem.tpcliID)
liaDtm          datetime        Date/time of this payment or adjustment activity
liaStrUser      varchar(?)      User/system that posted the activity
laiPaidIns      decimal         Amount paid by insurance
laiContAdj      decimal         Contractual adjustment amount
laiGenAdj       decimal         General adjustment amount
laiCopay        decimal         Copay amount collected from patient
laiDeduc        decimal         Deductible applied
laiClient       decimal         Patient responsibility amount
liaBitNoteOnly  bit             Flag for note-only (non-financial) activity
liaStrDesc      varchar(?)      Activity description text
tprbID          int             Linked payment remittance batch ID
liaPending      bit             Pending/unposted indicator
liaAmt          decimal         Total activity amount
liaStrText      varchar(?)      Free text note on the activity
liaAdjReason    varchar(?)      Adjustment reason code
laiCoins        decimal         Coinsurance amount
liaAction1      varchar(?)      Action code 1 (EOB processing)
liaAction2      varchar(?)      Action code 2
liaAdjContract  varchar(?)      Contractual adjustment code
liaAdjGeneral   varchar(?)      General adjustment code
liaANSI1        varchar(?)      ANSI/CARC reason code 1
liaANSI2        varchar(?)      ANSI/CARC reason code 2
liaANSIMod1     varchar(?)      ANSI modifier code 1
liaANSIMod2     varchar(?)      ANSI modifier code 2
billID          int             Billing batch/run ID
liaDBnotes      varchar(?)      Administrative/database notes
RowChkSum       int             Computed by ETL: CHECKSUM() across all mapped columns
________________________________________

9. SaveClaims — The EF Core Path (VBRA, VMIN, VWBY, VBRP Only)

File: BCAppCode/BHG-DR-LIB/SaveClaims.cs
Class: SaveData (partial class)
Method: SaveClaims()

Method signature:
    public RCodes SaveClaims(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (used for yearly vs daily load scope)
        bool yearly,             // true = load full year, false = load by date
        BHG_DRContext db)        // EF context (created if null)

This path is ONLY used for site codes: VBRA, VMIN, VWBY, VBRP.
All other sites use BulkDartsSrvLoader (see Section 10).

EF Core upsert logic — step by step:

Step 1 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load existing Azure rows for this site
Two loading modes based on the yearly flag:

    if (yearly == true):
        claims = db.TblClaims
            .Where(x => x.SiteCode == sc)
            .ToList()
        // Loads ALL claims for this site regardless of year
        // Then soft-resets: marks all rows in the current year's data as RowState=false
        foreach (TblClaims c in claims)
        {
            if (c.TpcCreatedDate.Value.Year == wrkdt.Year)
                c.RowState = false;
        }

    if (yearly == false):
        claims = db.TblClaims
            .Where(x => x.SiteCode == sc)
            .ToList()
        // Loads all claims for this site

In practice, SaveClaims is called with yearly=true from BHGTaskRunner (daily run).

Step 3 — Detect if this is a first-time load (all new)
    if (claims.Count == 0) { AllNewRows = true; }

If Azure has zero rows for this site, skip all lookups and create new objects directly.
This saves time on initial load.

Step 4 — Loop through every row from the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 5 — Get the claim ID and new checksum from source
    int inttpcID = int.Parse(r["tpcid"].ToString())
    int rcs = int.Parse(r["rowchksum"].ToString())

Step 6 — Find or create the Azure object

    if (AllNewRows):
        clm = new TblClaims { SiteCode = sc, TpcId = inttpcID, RowChkSum = rcs }
        NewRow = true
        res.RowsIns += 1

    else:
        clm = claims.Where(x => x.TpcId == inttpcID).FirstOrDefault()
        if (clm == null):
            clm = new TblClaims { SiteCode = sc, TpcId = inttpcID, RowChkSum = rcs }
            NewRow = true
            res.RowsIns += 1
        else:
            res.RowsUpd += 1

Step 7 — Compare checksums. Map all columns only if changed or new.

    if (NewRow || rcs != clm.RowChkSum):
        clm.LastModAt   = RunDT (DateTime.Now)
        clm.RowState    = true
        clm.RowChkSum   = rcs
        clm.TpccltId    = int.Parse(r["tpccltid"])
        clm.TpcStrStatus = r["tpcstrstatus"]
        clm.TpcStrPayer  = r["tpcstrpayer"]
        clm.TpcDtmAdded  = DateTime.Parse(r["tpcDtmAdded"])  // only if length > 7
        clm.TpcStrAdded  = r["tpcstrAdded"]
        ... (all F1–F33 CMS-1500 fields) ...
        clm.TpcCreatedDate = DateTime.Parse(r["tpcCreatedDate"])
        clm.TpcEncounter   = r["tpcEncounter"]
        clm.TpcWkstart     = DateTime.Parse(r["tpcwkstart"]).Date
        clm.TpcPayerCin    = r["tpcpayercin"]
        clm.TpcSrvType     = r["tpcsrvtype"]
        clm.TpcClaimType   = int.Parse(r["tpcClaimtype"])
        clm.SiteId         = int.Parse(r["SiteID"])
        clm.TpcDbnotes     = r["tpcdbnotes"]
        clm.TpcReferring   = r["tpcreferring"]

        if (NewRow || AllNewRows):
            db.TblClaims.Add(clm)
            NewRow = false

    else (RowChkSum unchanged — row not modified):
        clm.RowState  = true    // still mark as active
        clm.LastModAt = RunDT

Step 8 — Commit all changes in one batch
    db.SaveChanges()
    // EF Core generates UPDATE for modified objects + INSERT for newly added objects

Why RowState matters here:
At the start of the method, all rows for the current year are set to RowState=false (Step 2).
As the loop processes SAMMS rows, each found or new row is set back to RowState=true.
After SaveChanges(), any claim that existed in Azure but was NOT in the SAMMS data set
remains RowState=false — indicating it was deleted or is no longer active in the source.
This is the soft-delete mechanism for claims.
________________________________________

10. BulkDartsSrvLoader — The Bulk Path (All Other Sites)

File: BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs
Method: BulkDartsSrvLoader()

For all sites other than VBRA/VMIN/VWBY/VBRP, and for ClaimLineItem and
ClaimLineItemActivity on all sites, the bulk path is used.

The bulk path for claims mirrors the DartsSrv bulk path exactly:

Step 1 — Guard: if tbl.Rows.Count == 0, return immediately

Step 2 — TRUNCATE the staging table
    sm.ExeSqlCmd("Truncate Table stg.tbl_claims", sm.ConnectionString)

Step 3 — Map columns for bulk copy
    foreach (DataColumn c in tbl.Columns)
        bc.ColumnMappings.Add(c.ColumnName, c.ColumnName)

Step 4 — SqlBulkCopy.WriteToServer(tbl)
    All rows bulk-inserted into stg.tbl_claims in a single network operation.

Step 5 — Execute the merge stored procedure(s)
    exec stg.ClaimsMerge   (or equivalent)
    → MERGE pats.tbl_Claims AS tgt
      USING stg.tbl_Claims AS src ON (tgt.tpcID = src.tpcID AND tgt.SiteCode = src.SiteCode)
      WHEN MATCHED AND tgt.RowChkSum <> src.RowChkSum THEN UPDATE SET ...
      WHEN NOT MATCHED BY TARGET THEN INSERT ...

Step 6 — TRUNCATE staging table (cleanup)
    sm.ExeSqlCmd("Truncate Table stg.tbl_claims", sm.ConnectionString)

Step 7 — Return RCodes
    RCodes.IsResult        = true/false
    RCodes.RowsProcessed   = rows from DataTable
    RCodes.ExceptMsg       = exception if any

The same flow applies for stg.tbl_claimlineitem and stg.tbl_claimlineitemactivity.
________________________________________

11. SaveClaimLineItem — EF Core Path for Line Items

File: BCAppCode/BHG-DR-LIB/SaveClaims.cs
Method: SaveClaimLineItem()

Method signature:
    public RCodes SaveClaimLineItem(
        DataTable tbl,       // rows from SAMMS
        string sc,           // SiteCode
        DateTime wrkdt,      // work date
        BHG_DRContext db)    // EF context (created if null)

Note: In BHGTaskRunner, ClaimLineItem always goes through BulkDartsSrvLoader, not this
method. SaveClaimLineItem is available for use via SelectConstructor or manual backfill only.

Load scoping logic:

    if (wrkdt.Date == 1/1/wrkdt.Year):
        // Yearly load: load all line items for this site + year, reset RowState=false
        items = db.TblClaimLineItem
            .Where(x => x.SiteCode == sc && x.TpcliDtmAdded.Value.Year == wrkdt.Year)
            .ToList()
        foreach item: item.RowState = false

    else:
        // Date-specific load: load line items added on this exact date
        items = db.TblClaimLineItem
            .Where(x => x.SiteCode == sc && x.TpcliDtmAdded.Value.Date == wrkdt.Date)
            .ToList()

Upsert logic: same pattern as SaveClaims — AllNewRows shortcut, RowChkSum comparison,
column mapping only when changed, db.SaveChanges() at end.

Key column mapped: tpcliDSID (links the line item back to the DartsSrv session record).
This is the join between billing and clinical data in the warehouse.
________________________________________

12. SaveClaimLineItemActivity — EF Core Path for Activity

File: BCAppCode/BHG-DR-LIB/SaveClaims.cs
Method: SaveClaimLineItemActivity()

Method signature:
    public RCodes SaveClaimLineItemActivity(
        DataTable tbl,       // rows from SAMMS
        string sc,           // SiteCode
        DateTime wrkdt,      // work date
        BHG_DRContext db)    // EF context (created if null)

Note: In BHGTaskRunner, ClaimLineItemActivity always goes through BulkDartsSrvLoader.
SaveClaimLineItemActivity is available for SelectConstructor / manual backfill use only.

Load scoping logic:

    if (wrkdt.Date == 1/1/wrkdt.Year):
        // Yearly: load all activity for this site + year, reset RowState=false
        clia = db.TblClaimLineItemActivity
            .Where(x => x.SiteCode == sc && x.LiaDtm.Value.Year == wrkdt.Year)
            .ToList()

    else:
        // Date: load activity where liaDtm matches wrkdt
        clia = db.TblClaimLineItemActivity
            .Where(x => x.SiteCode == sc && x.LiaDtm.Value.Date == wrkdt.Date)
            .ToList()

Upsert logic: same AllNewRows / RowChkSum pattern. PK is liaID + SiteCode.
________________________________________

13. CleanupDeletedData — Soft Delete Reconciliation

File: BCAppCode/BHG-DR-LIB/SaveClaims.cs
Method: CleanupDeletedData()

Method signature:
    public bool CleanupDeletedData(
        DataTable tbl,       // ALL active IDs from SAMMS (not just recently changed)
        string sc,           // SiteCode
        string tblName,      // "claims", "claimlineitem", or "claimlineitemactivity"
        BHG_DRContext db)

This method performs a full reconciliation between what is currently active in SAMMS and
what exists in Azure. It marks rows that are no longer in the source as RowState=false
(soft-deleted).

Step-by-step for tblName = "claims":

Step 1 — Load all Azure rows for this site
    claims = db.TblClaims.Where(x => x.SiteCode == sc).ToList()

Step 2 — Set ALL Azure rows to RowState = false (assume all deleted)
    foreach (TblClaims c in claims) { c.RowState = false; }

Step 3 — Re-activate rows that still exist in source
    foreach (DataRow r in tbl.Rows):
        id = int.Parse(r["tpcid"])
        TblClaims c = claims.Where(x => x.TpcId == id).FirstOrDefault()
        if (c != null) { c.RowState = true; }
        // c == null means record exists in source but not yet in Azure — will be
        // picked up by SaveClaims on the next regular run

Step 4 — Commit
    db.SaveChanges()
    // Rows that were in Azure but not in SAMMS remain RowState=false (soft-deleted)

The same logic applies for "claimlineitem" (key: TpcliId + TpcliDsid + TpcliTpcid)
and "claimlineitemactivity" (key: LiaId + LiaTpcliid).

When is this called?
CleanupDeletedData is called explicitly when running claim cleanup operations,
typically via the commented-out RunClaimCleanup() call in SelectConstructor:
    //x = RunClaimCleanup(st.SiteCode, "claims");
________________________________________

14. Destination Tables — Azure BHG_DR (pats schema)

14a. pats.tbl_Claims

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_Claims
EF Model: BHG-DR-LIB/Models/TblClaims.cs
Mapped  : [Table("tbl_Claims", Schema = "pats")]

Primary Key: SiteCode + tpcID (composite)

C# Property (EF)        SQL Column Name         Type                Notes
----------------        ---------------         ----                -----
SiteCode                SiteCode                varchar(25)         PK Part 1 — clinic code
TpcId                   tpcID                   int                 PK Part 2 — claim ID
RowChkSum               RowChkSum               int                 Change detection hash
LastModAt               LastModAt               datetime            ETL last write timestamp
TpccltId                tpccltID                int (nullable)      Patient ID
TpcStrStatus            tpcStrStatus            varchar(100)        Claim status
TpcStrPayer             tpcStrPayer             varchar(100)        Payer name
TpcDtmAdded             tpcDtmAdded             datetime (nullable) Date claim created
TpcStrAdded             tpcStrAdded             varchar(100)        Created-by username
TpcClaimBatchId         tpcClaimBatchID         int (nullable)      Batch ID
TpcStrPrimary           tpcStrPrimary           varchar(100)        Primary payer ID
TpcCreatedDate          tpcCreatedDate          datetime (nullable) Claim date
TpcEncounter            tpcEncounter            varchar(10)         Encounter reference
TpcRebillreason         tpcREBILLREASON         varchar(500)        Rebill reason
TpcStrWeek              tpcStrWeek              varchar(25)         Billing week
TpcWkstart              tpcWKSTART              date (nullable)     Billing week start
TpcPayerCin             tpcPayerCIN             varchar(100)        Payer CIN
TpcSrvType              tpcSrvType              varchar(50)         Service type
TpcClaimType            tpcClaimType            int (nullable)      Claim type code
SiteId                  SiteID                  int (nullable)      SAMMS internal site ID
TpcDbnotes              tpcDBnotes              varchar(1000)       Admin notes
TpcReferring            tpcReferring            varchar(10)         Referring provider
RowState                RowState                bit (nullable)      true=active, false=soft-deleted
F10oth                  f10oth                  varchar(50)         CMS-1500 Field 10 other
F10auto                 f10auto                 varchar(50)         CMS-1500 Field 10 auto
F10employ               f10employ               varchar(50)         CMS-1500 Field 10 employment
F10local                f10local                varchar(50)         CMS-1500 Field 10 local
F11insanother           f11insanother           varchar(50)         Field 11 another insurance
F11insdob               f11insdob               varchar(50)         Field 11 insured DOB
F11insemploy            f11insemploy            varchar(50)         Field 11 insured employer
F11insnumber            f11insnumber            varchar(50)         Field 11 policy number
F11insplan              f11insplan              varchar(50)         Field 11 insurance plan
F11inssex               f11inssex               varchar(50)         Field 11 insured sex
F12sig                  f12sig                  varchar(50)         Field 12 patient signature
F12sigdate              f12sigdate              varchar(50)         Field 12 signature date
F13inssig               f13inssig               varchar(50)         Field 13 insured auth sig
F14date                 f14date                 varchar(50)         Field 14 illness/injury date
F15firstdate            f15firstdate            varchar(50)         Field 15 first similar illness
F16dateunableend        f16dateunableend        varchar(50)         Field 16 unable to work end
F16dateunablestart      f16dateunablestart      varchar(50)         Field 16 unable to work start
F17refername            f17refername            varchar(50)         Field 17 referring name
F17refernpi             f17refernpi             varchar(50)         Field 17 referring NPI
F18datehospend          f18datehospend          varchar(50)         Field 18 hosp end
F18datehospstart        f18datehospstart        varchar(50)         Field 18 hosp start
F19local                f19local                varchar(50)         Field 19 local use
F1id                    f1id                    varchar(50)         Field 1 payer ID
F20outsidelab           f20outsidelab           varchar(50)         Field 20 outside lab
F21diag1                f21diag1                varchar(50)         Field 21 diagnosis 1
F21diag2                f21diag2                varchar(50)         Field 21 diagnosis 2
F21diag3                f21diag3                varchar(50)         Field 21 diagnosis 3
F21diag4                f21diag4                varchar(50)         Field 21 diagnosis 4
F22medresub             f22medresub             varchar(50)         Field 22 Medicaid resubmission
F23priorauth            f23priorauth            varchar(50)         Field 23 prior auth
F25taxid                f25taxid                varchar(50)         Field 25 tax ID
F26account              f26account              varchar(50)         Field 26 patient account
F27assign               f27assign               varchar(50)         Field 27 assignment
F28totalcharge          f28totalcharge          varchar(50)         Field 28 total charge
F29amtpaid              f29amtpaid              varchar(50)         Field 29 amount paid
F2name                  f2name                  varchar(50)         Field 2 patient name
F30balancedue           f30balancedue           varchar(50)         Field 30 balance due
F31date                 f31date                 varchar(50)         Field 31 signature date
F31phys                 f31phys                 varchar(50)         Field 31 physician sig
F32a                    f32a                    varchar(50)         Field 32 facility NPI
F32b                    f32b                    varchar(50)         Field 32 facility taxonomy
F32line1                f32line1                varchar(50)         Field 32 facility name
F32line2                f32line2                varchar(50)         Field 32 facility addr 1
F32line3                f32line3                varchar(50)         Field 32 facility addr 2
F32line4                f32line4                varchar(50)         Field 32 facility city/st/zip
F33a                    f33a                    varchar(50)         Field 33 billing NPI
F33b                    f33b                    varchar(50)         Field 33 billing taxonomy
F33line1                f33line1                varchar(50)         Field 33 billing name
F33line2                f33line2                varchar(50)         Field 33 billing addr
F33line3                f33line3                varchar(50)         Field 33 billing city/st
F33line4                f33line4                varchar(50)         Field 33 billing zip
F33phone                f33phone                varchar(50)         Field 33 billing phone
F3dob                   f3dob                   varchar(50)         Field 3 patient DOB
F3sex                   f3sex                   varchar(10)         Field 3 patient sex
F4insname               f4insname               varchar(50)         Field 4 insured name
F5add                   f5add                   varchar(50)         Field 5 patient address
F5city                  f5city                  varchar(50)         Field 5 patient city
F5phone                 f5phone                 varchar(50)         Field 5 patient phone
F5state                 f5state                 varchar(50)         Field 5 patient state
F5zip                   f5zip                   varchar(50)         Field 5 patient zip
F6insrel                f6insrel                varchar(50)         Field 6 insured relation
F7insadd                f7insadd                varchar(50)         Field 7 insured address
F7inscity               f7inscity               varchar(50)         Field 7 insured city
F7insphone              f7insphone              varchar(50)         Field 7 insured phone
F7insstate              f7insstate              varchar(50)         Field 7 insured state
F7inszip                f7inszip                varchar(50)         Field 7 insured zip
F8stat                  f8stat                  varchar(50)         Field 8 patient status
F9othinsdob             f9othinsdob             varchar(50)         Field 9 other ins DOB
F9othinsemp             f9othinsemp             varchar(50)         Field 9 other ins employer
F9othinsname            f9othinsname            varchar(50)         Field 9 other ins name
F9othinsnumber          f9othinsnumber          varchar(50)         Field 9 other policy number
F9othinsplan            f9othinsplan            varchar(50)         Field 9 other ins plan
F9othinssex             f9othinssex             varchar(50)         Field 9 other ins sex
________________________________________

15. Change Detection — RowChkSum

The RowChkSum column is the efficiency mechanism for this ETL.

How it is computed (at source, during SELECT by SelectConstructor):

    RowChkSum = CHECKSUM(
        tpcID, tpccltID, tpcStrStatus, tpcStrPayer, tpcDtmAdded, tpcStrAdded,
        tpcClaimBatchID, tpcStrPrimary, tpcCreatedDate, tpcEncounter,
        tpcREBILLREASON, tpcStrWeek, tpcWKSTART, tpcPayerCIN, tpcSrvType,
        tpcClaimType, SiteID, tpcDBnotes, tpcReferring,
        f1id, f2name, f3dob, f3sex, f4insname,
        f5add, f5city, f5phone, f5state, f5zip,
        f6insrel, f7insadd, f7inscity, f7insphone, f7insstate, f7inszip,
        f8stat, f9othinsdob, f9othinsemp, f9othinsname, f9othinsnumber,
        f9othinsplan, f9othinssex, f10auto, f10employ, f10local, f10oth,
        f11insanother, f11insdob, f11insemploy, f11insnumber, f11insplan, f11inssex,
        f12sig, f12sigdate, f13inssig, f14date, f15firstdate,
        f16dateunableend, f16dateunablestart, f17refername, f17refernpi,
        f18datehospend, f18datehospstart, f19local, f20outsidelab,
        f21diag1, f21diag2, f21diag3, f21diag4, f22medresub, f23priorauth,
        f25taxid, f26account, f27assign, f28totalcharge, f29amtpaid, f30balancedue,
        f31date, f31phys, f32a, f32b, f32line1, f32line2, f32line3, f32line4,
        f33a, f33b, f33line1, f33line2, f33line3, f33line4, f33phone
    )

How it is used:

EF Core path (SaveClaims):
    if (rcs != clm.RowChkSum || NewRow) { ... map all columns ... }
    Rows with matching checksums have their RowState and LastModAt updated but all
    data columns are left untouched — no database writes for unchanged data.

Bulk path (merge stored procedure):
    WHEN MATCHED AND tgt.RowChkSum <> src.RowChkSum THEN UPDATE SET ...
    WHEN NOT MATCHED BY TARGET THEN INSERT ...

What this means in practice:
- A clinic with 10,000 claims but only 30 updated today generates 30 column-level updates.
- The other 9,970 rows are processed through the loop/merge but no data columns change.
- This keeps the ETL fast even for sites with large historical claim volumes.
________________________________________

16. RowState — Soft Delete Tracking

RowState is a bit column (nullable) that acts as an active/inactive flag.

Value       Meaning
-----       -------
true (1)    Row is active — exists in current SAMMS data
false (0)   Row has been soft-deleted — existed in Azure but is no longer in SAMMS
NULL        Row has never been touched by cleanup logic

How RowState is managed:

During SaveClaims (normal daily run):
- At load start: rows for the current year are set to RowState=false (reset)
- After each SAMMS row is processed: RowState is set back to true
- Rows that appear in Azure but not in today's SAMMS fetch remain false

During CleanupDeletedData (reconciliation run):
- ALL Azure rows for the site are set to RowState=false
- Only rows found in the full SAMMS ID list are set back to true
- Rows remaining false are confirmed-deleted from source

Usage in queries:
    Select count(1) from pats.tbl_Claims
    where SiteCode = 'VBRA' and RowState = 1
    — used by RowTrax to count active destination rows

RowState = 1 is the standard filter for "active claims" in all downstream reporting views.
________________________________________

17. Load Design Summary

Load type: Incremental upsert with checksum-based change detection + soft-delete reconciliation

Per run behavior for pats.tbl_claims:

    EF Core path (VBRA/VMIN/VWBY/VBRP):
        1. Load all Azure rows for this site into memory
        2. Soft-reset RowState=false for current year rows
        3. For each SAMMS row:
           - RowChkSum matches existing → set RowState=true, LastModAt, no data write
           - RowChkSum differs → update all columns, RowState=true, LastModAt
           - Not found in Azure → INSERT new row
        4. db.SaveChanges() — single batch commit

    Bulk path (all other sites):
        1. TRUNCATE stg.tbl_claims
        2. SqlBulkCopy → all SAMMS rows into stg.tbl_claims
        3. exec stg.ClaimsMerge → MERGE into pats.tbl_Claims
           - RowChkSum match → no action
           - RowChkSum changed → UPDATE
           - Not in target → INSERT
        4. TRUNCATE stg.tbl_claims (cleanup)

Per-claim identity:
Each claim row is identified by the composite key (tpcID + SiteCode).
tpcID is only unique within a single clinic. The ETL-added SiteCode column makes the
key globally unique across the entire warehouse.

Three-table hierarchy:
    pats.tbl_Claims (header)
        → pats.tbl_ClaimLineItem (line items, joined by tpcliTPCID = tpcID)
            → pats.tbl_ClaimLineItemActivity (payments, joined by liaTPCLIID = tpcliID)
________________________________________

18. Error Handling and Recovery

SaveClaims error handling:

    try
    {
        // ... full EF Core loop + SaveChanges() ...
    }
    catch (Exception e)
    {
        res.IsResult = false
        res.ExceptMsg = e.Message
        res.ExceptInnerMsg = e.InnerException.Message
        Console.WriteLine(e.Message)
        Console.WriteLine(e.InnerException.Message)
    }

If an EF Core exception occurs:
- The entire batch for that site is rolled back (db.SaveChanges() not committed)
- RCodes.IsResult = false
- The caller (BHGTaskRunner) records the error in task.ErrorMessage
- The task status is set to Status=19 (error) or 20 (complete with error)

BulkDartsSrvLoader error handling (bulk path):

    try
    {
        bc.WriteToServer(tbl)
        // ... exec merge stored procedures ...
    }
    catch (Exception ex)
    {
        rst.IsResult = false
        rst.ExceptMsg = ex.Message
        rst.ExceptInnerMsg = ex.InnerException.Message
    }
    finally
    {
        bc.Close()
    }

If the bulk copy fails, the staging table is left un-truncated (manual cleanup may be needed).

SaveClaimLineItemActivity error handling adds an extra null-guard:
    if (e.InnerException != null)
    { Console.WriteLine(e.InnerException.Message); }
    Console.WriteLine(li.SiteCode + ", " + li.TpcliId.ToString())
    // Logs the specific row that caused the failure

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

A failed claims run for a clinic will automatically be retried the next day.
________________________________________

19. RowTrax — Audit and Row Count Tracking

Table: tsk.tbl_RowTrax (Azure BHG_DR)

After each successful claims load for a clinic (if st.RowTrax == true):

    sd.SaveRowTrax(
        st.SiteCode,           -- e.g. "VBRA"
        st.WorkDate,           -- today
        st.TaskName,           -- "pats.tbl_claims"
        SrcDt.Rows.Count,      -- rows returned from SAMMS this run
        destCount,             -- count in Azure
        null)

Destination count query (run against Azure):
    Select count(1) from pats.tbl_claims
    where SiteCode = 'VBRA' and RowState = 1

Note: Unlike DartsSrv, the claims source count uses the DataTable row count from the
incremental fetch (not a full source table count). The destination count includes only
active rows (RowState = 1).

The same RowTrax pattern applies for pats.tbl_claimlineitem and
pats.tbl_claimlineitemactivity.

The stored RowTrax records allow analysts to monitor claim counts over time and detect
clinics where billing data may be falling behind or diverging from the source.
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
        |       pats.tbl_claims x 80 clinics
        |       pats.tbl_claimlineitem x 80 clinics
        |       pats.tbl_claimlineitemactivity x 80 clinics
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
        |       get column mappings from dms.tbl_MapSrc2Dsn (ActionKey=8)
        |       SelectConstructor.GetSLT() → builds SELECT field list + CHECKSUM()
        |       build WHERE clause (date range filter)
        |
        V
SQLSvrManager.GetTableData()
        |
        | executes full SELECT against clinic SQL Server
        | connection from ctrl.tbl_LocationCons for this SiteCode
        |
        V
DataTable (in memory — rows from SAMMS)
        |
        |---[if TaskName = pats.tbl_claims]
        |
        |   [SiteCode is VBRA / VMIN / VWBY / VBRP?]
        |          YES                             NO
        |           |                               |
        |           V                               V
        |   SaveClaims() [EF CORE]         BulkDartsSrvLoader() [BULK]
        |           |                               |
        |   load all Azure rows             TRUNCATE stg.tbl_claims
        |   reset RowState=false            SqlBulkCopy → stg.tbl_claims
        |   for each row:                   exec stg.ClaimsMerge
        |     compare RowChkSum               MERGE into pats.tbl_Claims
        |     map columns if changed             RowChkSum diff → UPDATE
        |     RowState = true                    not in target → INSERT
        |   db.SaveChanges()               TRUNCATE stg.tbl_claims
        |           |                               |
        |           V                               V
        |   pats.tbl_Claims (Azure BHG_DR)  [FINAL DESTINATION]
        |
        |---[if TaskName = pats.tbl_claimlineitem]
        |       BulkDartsSrvLoader → stg.tbl_claimlineitem → merge → pats.tbl_ClaimLineItem
        |
        |---[if TaskName = pats.tbl_claimlineitemactivity]
        |       BulkDartsSrvLoader → stg.tbl_claimlineitemactivity → merge → pats.tbl_ClaimLineItemActivity
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
BCAppCode/BHGTaskRunner/Program.cs              Main ETL driver (arg=8 → Claims pipeline)
BCAppCode/BHG-DR-LIB/SelectConstructor.cs       Builds SELECT + CHECKSUM() from metadata
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs           ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/SaveClaims.cs             EF Core upsert — SaveClaims, SaveClaimLineItem,
                                                 SaveClaimLineItemActivity, CleanupDeletedData
BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs            SqlBulkCopy + merge SPs (bulk path for all sites)
BCAppCode/BHG-DR-LIB/Models/TblClaims.cs        EF Model → pats.tbl_Claims
BCAppCode/PHC/SelectConstructor.cs               PHC runner — also routes tbl_claims to SaveClaims
BCAppCode/PHC/Program.cs                         PHC ETL driver — handles VBRA site claims specially
________________________________________

22. Quick Reference Summary

What triggers Claims ETL?       Scheduler.exe creates tasks, BHGTaskRunner.exe 8 processes them
TaskName in scheduler?          SAMMS-ETL-INV
Source tables in SAMMS?         dbo.tbl3pClaim, dbo.tbl3pClaimLineItem, dbo.tbl3pClaimLineItemActivity
Destination tables in Azure?    pats.tbl_Claims, pats.tbl_ClaimLineItem, pats.tbl_ClaimLineItemActivity
Primary key (Claims)?           SiteCode + tpcID (composite)
EF Core path applies to?        VBRA, VMIN, VWBY, VBRP site codes only (SaveClaims)
Bulk path applies to?           All other site codes + all ClaimLineItem/Activity loads
How is change detected?         RowChkSum = CHECKSUM() across all claim header fields
What is RowState?               Soft-delete flag — true=active, false=deleted/inactive
How are deletes handled?        SaveClaims resets current-year RowState=false at start, then
                                re-activates each row found in source; CleanupDeletedData does
                                full reconciliation
Staging tables?                 stg.tbl_claims, stg.tbl_claimlineitem, stg.tbl_claimlineitemactivity
What are the F# fields?         CMS-1500 insurance claim form fields (standard paper billing form)
ClaimLineItem link to DartsSrv? tpcliDSID = dsID in pats.tbl_DartsSrv — joins billing to sessions
RowTrax audit?                  Source DataTable row count vs count(RowState=1) in Azure
Error recovery?                 Scheduler resets failed tasks to Status=17 on next daily run
PHC handled here?               No — PHC uses its own runner (PHC/Program.cs) but also calls SaveClaims
