
Orders ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract prescription order
records from local SAMMS SQL Server databases at each clinic and load them into the central
Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What Orders data is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up, splits by year, and drives the pipeline
- How the year-partitioned table architecture works
- How each SaveOrders year method upserts data
- What the source columns look like and their key fields
- What the destination tables look like and all their columns
- How RowChkSum drives change detection
- How RowState and Active together track soft-deleted / inactive orders
- What key behavioral differences exist between SaveOrders method generations
- How RowTrax audit tracking works
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What are Orders?

In SAMMS, a prescription Order record represents a physician's medication order for a
patient enrolled in a MAT (Medication-Assisted Treatment) program. Each order specifies:
- What medication and dose is authorized (Dose, Dose2, MedType)
- When it is effective and when it expires (EffectiveDate, ExpirationDate)
- Which days of the week the patient may receive doses (Sunday–Saturday booleans)
- Whether split dosing applies (SplitFirst, Intervals, Weeknum)
- The prescribing doctor and their signatures (SigDr, SigMid, DtSig)
- Whether the order is currently active (Active flag)
- Who activated or deactivated it and when (ActbyDate, DeActbyDate)

Every dose dispensed to a patient must trace back to an active order. Orders form the
prescription authority foundation for all dose records.

The Orders pipeline manages 14 destination tables — one generic table and 13 year-
partitioned tables — organized by the year of the order's OrderDate:

pats.tbl_Orders        — Generic/current table (SaveOrders method; not called daily)
pats.tbl_Orders2016    — Orders with OrderDate in 2016
pats.tbl_Orders2017    — Orders with OrderDate in 2017
pats.tbl_Orders2018    — Orders with OrderDate in 2018
pats.tbl_Orders2019    — Orders with OrderDate in 2019
pats.tbl_Orders2020    — Orders with OrderDate in 2020
pats.tbl_Orders2021    — Orders with OrderDate in 2021
pats.tbl_Orders2022    — Orders with OrderDate in 2022
pats.tbl_Orders2023    — Orders with OrderDate in 2023
pats.tbl_Orders2024    — Orders with OrderDate in 2024
pats.tbl_Orders2025    — Orders with OrderDate in 2025
pats.tbl_Orders2026    — Orders with OrderDate in 2026
pats.tbl_Orders2027    — Orders with OrderDate in 2027
pats.tbl_Orders2028    — Orders with OrderDate in 2028

A unified view pats.vw_Orders UNIONs all year tables and is used for RowTrax counts
and downstream reporting queries.

Why it is important

The Orders dataset is the prescription and dispensing authorization backbone of the BHG
data warehouse. It enables:
- Validating that every dose has a corresponding active order on the date dispensed
- Reporting on order changes, dose increases/decreases over a patient's treatment history
- Tracking prescriber signature compliance (DrSig, MidSig, Noted timestamps)
- Supporting regulatory audits of controlled substance prescribing records
- Identifying orders marked blind (Blind flag) for double-blind protocol tracking
- Analytics on prescription renewal patterns and patient retention across clinics

Load type

All sites use the EF Core path exclusively. There is no Bulk path (BulkDartsSrvLoader)
for Orders. Every clinic's order data goes through the SaveOrders20XX EF Core upsert
methods, year by year.

The generic SaveOrders method (no year suffix) maps to pats.tbl_Orders but is NOT
called by BHGTaskRunner in the daily run. It exists as a historical predecessor and
potential backfill method. Only SaveOrders2016 through SaveOrders2028 are called.
________________________________________

3. Systems Involved

System / File                           Role
-----------                             ----
tsk.tbl_Schedule (Azure DB)             Configuration — defines schedules and their run times
Scheduler.exe                           Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=11                Main ETL orchestrator for SAMMS-ETL-Orders
dms.vw_MapAction (Azure DB)             Maps pats.tbl_Orders destination to SAMMS-ETL-Orders TaskName
dms.tbl_MapSrc2Dsn (Azure DB)           Column metadata — defines SELECT columns per ActionKey
SelectConstructor.cs                    Assembles SELECT statement from metadata
SQLSvrManager.cs                        Fires SELECT against the clinic SAMMS SQL Server
SaveOrders.cs (BHG-DR-LIB)            EF Core upsert — SaveOrders + SaveOrders2016–2028
ctrl.tbl_LocationCons (Azure DB)        Connection strings for each clinic's SAMMS SQL Server
pats.tbl_Orders (Azure)                 Generic orders table (predecessor; not active in daily run)
pats.tbl_Orders2016–2028 (Azure)        Year-partitioned destination tables (one per calendar year)
pats.vw_Orders (Azure)                  View unioning all year tables — used for RowTrax and reporting
tsk.tbl_RowTrax (Azure)                 Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How Orders Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily and populates the task queue. It does NOT move data —
it only creates tasks.

What the Scheduler does for Orders (SAMMS-ETL-Orders)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For Orders, there is a row with:
    Name        = 'SAMMS-ETL-Orders'
    ActionKey   = 11
    ScheduleId  = (orders schedule ID)

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'SAMMS-ETL-Orders'
    SiteCode = 'All'
    Status   = 17

Step 4 — Insert child task rows (one per clinic)
The Scheduler uses dms.vw_MapAction with a CASE expression that assigns TaskNames:

    when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Orders' → 'SAMMS-ETL-Orders'

Note: pats.tbl_Orders is the anchor mapping in dms.vw_MapAction. There is a SINGLE
child task per clinic (not 13 tasks for each year table). BHGTaskRunner fetches all
years in one SELECT and internally routes them to the right year method. The Scheduler
only creates one task row per clinic.

Note also: pats.tbl_Orders is explicitly excluded from all timezone-based Regional
ETL schedules (Eastern P1/P2, Central P1/P2, Mountain P1/P2, Pacific P1/P2). It belongs
exclusively to SAMMS-ETL-Orders.

This produces child task rows for each active clinic:
    TaskName = 'pats.tbl_orders'
    SiteCode = 'B01', 'VBRA', etc.

Step 5 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1

Step 6 — Clean up
    delete from tsk.tbl_Tasks2
    where RunAt <= DateAdd(m, -3, GetDate()) or RowState = 26

Task queue structure after Scheduler runs:

tsk.tbl_Tasks2 will contain:
    ParentTaskId = NULL
        TaskName = 'SAMMS-ETL-Orders'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'pats.tbl_orders'
        SiteCode = 'B01'
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_orders'
        SiteCode = 'VBRA'
        Status   = 17

    ... (one row per active clinic — not per year table)
________________________________________

5. BHGTaskRunner — How Orders Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 11 to process only the SAMMS-ETL-Orders schedule.

Command:   BHGTaskRunner.exe 11

Step 1 — Filter task queue for SAMMS-ETL-Orders
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"                    // PHC uses a separate runner
        && x.Status == 17                      // ready to run
        && x.TaskName == "SAMMS-ETL-Orders"
        && x.RunAt < DateTime.Now)

Step 2 — Mark parent task as running
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)

Step 4 — For each child task (each clinic), get column mappings and build SELECT

SelectConstructor assembles the SELECT statement from dms.tbl_MapSrc2Dsn metadata
for ActionKey=11. This produces a SELECT across all columns of the source order table,
including the CHECKSUM(...) expression that produces the RowChkSum column.

Step 5 — Apply the WHERE clause from task metadata
    strCmd += " Where " + st.WhereCondition

Unlike most other ETL pipelines where BHGTaskRunner builds strWhere dynamically from
DaysBack and date arithmetic, the Orders pipeline uses st.WhereCondition directly from
the task row. This means the date filter is stored in the task configuration, not
computed at runtime. The typical condition filters on active status or date range to
control how far back the SELECT reaches.

Step 6 — Execute SELECT against SAMMS
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

This returns a single DataTable containing ALL orders for this clinic that match the
WHERE condition — potentially spanning multiple calendar years.

Step 7 — Year-split and sequential dispatch

Unlike all other ETL pipelines that call one method per task, Orders dispatches to
13 different year-specific methods in a single pass. The DataTable returned in Step 6
is split by the OrderDate year of each row before being passed to each method.

    bool x = true    // failure gate — stops processing if any year fails

    For each year 2016 through 2028:
        cnt = SrcDt.AsEnumerable()
                   .Where(row => row.Field<DateTime>("OrderDate").Year == YYYY)
                   .Count()

        if cnt > 0 AND x == true:
            yearDt = SrcDt.AsEnumerable()
                          .Where(row => row.Field<DateTime>("OrderDate").Year == YYYY)
                          .CopyToDataTable()

            x = sd.SaveOrders20YY(yearDt, st.SiteCode, DateTime.Parse("12/31/20YY"), null)

The years are processed in order: 2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023,
2024, 2025, 2026, 2027, 2028.

CRITICAL — Failure gate behavior:
If any year's method returns false (an exception occurred), the variable x becomes false
and ALL subsequent years are skipped. The failure is propagated as rCodes.IsResult = false.

For example: if SaveOrders2021 throws an exception:
- SaveOrders2022 through SaveOrders2028 are NOT called
- The run is marked as failed
- Data for 2022-2028 is not updated until the next retry

Important notes on year boundaries:
- For years 2026, 2027, and 2028, the wrkdt parameter passed is hardcoded to
  DateTime.Parse("12/31/2025") — the same date. This appears to be an oversight where
  the future-year methods received outdated date parameters. The wrkdt parameter is not
  actively used in filtering within those methods (it is not applied to the WHERE clause
  since that is done before the split), so this does not affect data correctness.

Step 8 — Return results
    rCodes.IsResult = x         (true only if ALL year methods succeeded)
    rCodes.RowsProcessed = SrcDt.Rows.Count  (total rows from SAMMS across all years)

Step 9 — RowTrax audit (if st.RowTrax = true AND st.ActionKey == 1)

    Source count:
        select count(1) from <SrcSchema>.<FromTblVw>
        where OrderDate is not null and cltID > 0

    Destination count:
        Select tblcnt = count(1) from pats.vw_Orders
        where SiteCode = '<sc>'

    Note: The destination uses pats.vw_Orders (the UNION view across all year tables),
    not any individual year table. The source count only includes rows where CltId > 0
    — negative client IDs are excluded from the count.
    Also: RowTrax is only written when ActionKey == 1. Orders tasks have ActionKey = 11
    in the typical setup, so RowTrax may not be written on every run.

Step 10 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. SelectConstructor — How the SELECT Is Built

File: BCAppCode/BHG-DR-LIB/SelectConstructor.cs

SelectConstructor.GetSLT() is called for each child task. It reads column metadata from
dms.tbl_MapSrc2Dsn (ActionKey=11) and assembles the SELECT field list.

The SELECT conceptually includes all columns from the source order table:

    Select
        SiteCode = 'B01',
        OrderNum,
        cltid,
        medtype,
        dateadded,
        orderdate,
        doctor,
        effectivedate,
        expirationdate,
        dose,
        dose2,
        changeby,
        intervals,
        sunday, monday, tuesday, wednesday, thursday, friday, saturday,
        sunday2, monday2, tuesday2, wednesday2, thursday2, friday2, saturday2,
        notes,
        active,
        type,
        stype,
        weeknum,
        splitfirst,
        blind,
        o_user,
        cltM4id,
        newdose,
        pckcode,
        rxhistid,
        ex,
        actbydate,
        actbyuser,
        white,
        repoldorder,
        sigdr,
        dtsig,
        aws,
        blsched,
        blverbal,
        color,
        deactbydate,
        deactbyuser,
        ordertypev5,
        sigentered,
        signoted,
        signoteddt,
        dtmid,
        sigmid,
        overapprove,
        overapprovedt,
        sigentereddt,
        sigdrimg,
        SigMidImg,
        SigNotedImg,
        RowChkSum = CHECKSUM(OrderNum, cltid, medtype, dateadded, orderdate, doctor,
                             effectivedate, expirationdate, dose, dose2, changeby,
                             intervals, sunday, monday, ... saturday2,
                             notes, active, type, stype, weeknum, splitfirst, blind,
                             o_user, cltM4id, newdose, pckcode, rxhistid, ex,
                             actbydate, actbyuser, white, repoldorder, sigdr, dtsig,
                             aws, blsched, blverbal, color, deactbydate, deactbyuser,
                             ordertypev5, sigentered, signoted, signoteddt, dtmid, sigmid,
                             overapprove, overapprovedt, sigentereddt, sigdrimg,
                             SigMidImg, SigNotedImg)
    from <source view or table>

Note: The exact CHECKSUM expression spans all mapped columns. Only columns included in
the CHECKSUM expression are considered for change detection. RowChkSum is computed at
the SAMMS SQL Server — the ETL receives it pre-computed in the DataTable.
________________________________________

7. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'B01'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_B01;User Id=...;Password=...;'

The source table or view name (st.FromTblVw) and schema (st.SrcSchema) come from the
task metadata in dms.vw_MapAction. For Orders, this typically points to a view or table
such as dbo.tblOrder or dbo.vwOrder in the clinic's SAMMS database.

The returned DataTable contains ALL years of order data that match the WHERE condition.
BHGTaskRunner then partitions this single DataTable by OrderDate year before dispatching
each year-specific slice to the corresponding Save method.
________________________________________

8. Source Table — SAMMS SQL Server (dbo)

The source lives in the clinic's local SAMMS SQL Server database under the dbo schema.
The exact table or view name is stored in the task metadata (st.FromTblVw).

dbo.tblOrder (or site-specific view) — Prescription Order Records

Primary Key: OrderNum (unique per clinic, scoped by CltId)

Column Name         Type            Description
-----------         ----            -----------
OrderNum            int             Unique order number within this clinic
cltid               int             Patient/client ID this order is for
medtype             varchar(50)     Medication type code (e.g. 'M' for methadone, 'B' for buprenorphine)
dateadded           datetime        Date/time the order was entered into SAMMS
orderdate           datetime        The clinical order date — used as the year-partitioning key
doctor              varchar(50)     Name/ID of the prescribing doctor
effectivedate       datetime        Date the order becomes effective (patient may start receiving doses)
expirationdate      datetime        Date the order expires (doses cannot be dispensed after this)
dose                decimal         Primary dose amount (in mg)
dose2               decimal         Secondary/split dose amount (in mg; used with SplitFirst)
changeby            int             User ID who last changed this order
intervals           smallint        Dosing interval code (e.g. daily, every other day)
sunday–saturday     bit             Take-home schedule flags for each day of the week (primary week)
sunday2–saturday2   bit             Take-home schedule flags for each day (secondary/alternate week)
notes               varchar(1000)   Free-text notes on this order
active              bit             Whether this order is currently active (1=active, 0=inactive)
type                varchar(50)     Order type code
stype               varchar(50)     Order sub-type code
weeknum             int             Week number within a multi-week take-home schedule
splitfirst          bit             True = first dose of the day is split (partial morning dose)
blind               bit             True = blind study / double-blind protocol order
o_user              varchar(100)    Username who entered/owns this order
cltM4id             varchar(50)     Patient's M4 system ID (alternate ID cross-reference)
newdose             int             New dose amount if this order represents a dose change (nullable)
pckcode             varchar(50)     Package/container code for dispensing
rxhistid            varchar(50)     Prescription history ID (links to pharmacy Rx system)
ex                  bit             Exception flag (nullable)
actbydate           datetime        Date/time this order was activated (nullable)
actbyuser           varchar(100)    Username who activated this order
white               bit             White card / override flag (nullable)
repoldorder         decimal         Order number being replaced (if this is a replacement order, nullable)
sigdr               ntext           Doctor's written/electronic signature text
dtsig               datetime        Date/time doctor signed the order (nullable)
aws                 bit             Automatic weekly schedule flag (nullable)
blsched             bit             Blank schedule flag (nullable)
blverbal            bit             Verbal order flag (nullable)
color               varchar(50)     Order display color code
deactbydate         datetime        Date/time this order was deactivated (nullable)
deactbyuser         varchar(100)    Username who deactivated this order
ordertypev5         varchar(50)     Order type code for SAMMS V5 schema compatibility
sigentered          ntext           Signature of person who entered the order
signoted            ntext           Signature of person who noted/witnessed the order
signoteddt          datetime        Date/time the order was noted/witnessed (nullable)
dtmid               datetime        Date/time of mid-level provider signature (nullable)
                                    Note: the sentinel value '1900-01-01 00:00:00.000' is explicitly
                                    excluded from storage — stored as NULL if this sentinel is received
sigmid              ntext           Mid-level provider signature text
overapprove         varchar(50)     Over-approval code (supervisor override reason)
overapprovedt       varchar(50)     Over-approval date (stored as varchar, not datetime)
sigentereddt        datetime        Date/time the entered-by signature was applied (nullable)
sigdrimg            varbinary       Doctor's signature image — stored as ASCII bytes
SigMidImg           varbinary       Mid-level provider signature image — stored as ASCII bytes
SigNotedImg         varbinary       Noted-by signature image — stored as ASCII bytes
rowchksum           int             SQL CHECKSUM() across all mapped columns (computed during SELECT)

Note on the three signature images:
sigdrimg, SigMidImg, and SigNotedImg are transmitted from SAMMS as string representations
and stored as byte arrays in Azure using System.Text.Encoding.ASCII.GetBytes(). They are
always stored when present in the source DataTable schema (may be schema-optional in
SaveOrders2024+).
________________________________________

9. SaveOrders Methods — The EF Core Upsert Family

File: BCAppCode/BHG-DR-LIB/SaveOrders.cs
Class: SaveData (partial class)

All 14 methods (SaveOrders and SaveOrders2016 through SaveOrders2028) share the same
fundamental EF Core upsert pattern. They differ primarily in:
- Which Azure destination table they write to (TblOrders vs TblOrders2016 etc.)
- The checksum gate condition
- Length guards on date and numeric fields
- CltId filtering behavior
- Schema-optional column guards
- Notes field truncation

All methods return bool (true = success, false = exception caught).

Method signatures (all identical in shape):
    public bool SaveOrders(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
    public bool SaveOrders2016(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
    public bool SaveOrders2017(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
    ... (same for 2018 through 2028)

Parameters:
    tbl     DataTable — rows from SAMMS for this clinic, already filtered by year by BHGTaskRunner
    sc      string    — SiteCode (e.g. "B01", "VBRA")
    wrkdt   DateTime  — work date (passed as 12/31/YYYY for year methods; not used in the WHERE
                        clause since that was applied before the year split)
    db      DbContext — EF context (created internally if null)

________________________________________
9a. Common Logic Across All Methods

Step 1 — Guard: if tbl.Rows.Count == 0, return true immediately

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Peek at the first OrderNum from the DataTable
    int onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString())
This is an initialization step; onum is overwritten per-row in the loop.

Step 4 — Load all existing Azure rows for this site and year table
    List<TblOrders20YY> orders = db.TblOrders20YY
        .Where(x => x.SiteCode == sc)
        .OrderBy(o => o.OrderNum)
        .ToList()

ALL rows for this SiteCode are loaded regardless of year or date — the year scope is
enforced by the table name itself (each method targets only one year table).

Note: the commented-out year filter
    //&& x.Orderdate.Value.Year == 20YY
was removed. The full table for the site is always loaded.

Step 5 — Set AllNewRows flag
    if (orders.Count == 0) { AllNewRows = true; }
This skips all lookups and treats every incoming row as a new insert.

Step 6 — Pre-pass: reset ALL existing rows (RowState and Active)
    foreach (TblOrders20YY ord in orders):
        ord.RowState = false
        ord.Active   = false

Both flags are soft-reset to false/false before the upsert loop. All existing orders
for this clinic in this year table are treated as potentially inactive until re-confirmed
by the SAMMS data. No date filter is applied — the full table is reset every run.

This is the most aggressive pre-pass of all ETL pipelines — it resets ALL existing rows
regardless of date, not just those within a lookback window.

Step 7 — Process each source row
    foreach (DataRow r in tbl.Rows):
        onum   = int.Parse(r["OrderNum"].ToString())
        cltid  = int.Parse(r["cltid"].ToString())
        rcs    = int.Parse(r["rowchksum"].ToString())

Step 8 — Construct or locate the order object

    If AllNewRows == true:
        o = new TblOrders20YY {
            SiteCode = sc,
            CltId    = cltid,
            OrderNum = onum,
            RowChkSum = rcs
        }
        NewRow = true

    Else:
        o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault()

        if o == null (not found in Azure):
            o = new TblOrders20YY {
                SiteCode = sc,
                CltId    = cltid,
                OrderNum = onum,
                RowChkSum = rcs
            }
            NewRow = true

        else (found):
            proceed to checksum check

The composite lookup key is: OrderNum + CltId (within the already SiteCode-filtered list).

Step 9 — Write all fields (if gate condition is met — see Section 9b)

    o.RowState  = true
    o.LastModAt = DateTime.Now
    o.RowChkSum = int.Parse(r["RowChkSum"].ToString())
    (all columns mapped — see Column Mapping table in Section 9c)

    if (NewRow || AllNewRows):
        NewRow = false
        ords.Add(o)    // stage for batch insert

Step 10 — Checksum unchanged path (existing rows only, gate not met)

    o.RowState  = true
    o.LastModAt = DateTime.Now

The row is re-activated and timestamp refreshed, but NO data fields are updated. This
re-sets the pre-pass RowState=false back to true for orders that exist unchanged.

Step 11 — Two-commit write
    db.SaveChanges()               // commits pre-pass resets + updates for existing rows
    if (ords.Count > 0):
        db.TblOrders20YY.AddRange(ords)
        db.SaveChanges()           // batch insert of all new rows

________________________________________
9b. Checksum Gate — Differences Between Method Generations

The gate condition controls when a full field update runs vs. just RowState refresh.
The condition varies meaningfully across method generations:

SaveOrders (generic, pats.tbl_Orders):
    if ((NewRow) || (rcs != o.RowChkSum) || (rcs < 0))

    Extra condition: rcs < 0 — if the SQL CHECKSUM() produces a negative value (which
    is valid output from CHECKSUM()), the row is ALWAYS fully updated regardless of
    whether the stored checksum matches. This is a safety net for negative checksums.

SaveOrders2016, 2017, 2018, 2019, 2020, 2021, 2022, 2023:
    if ((NewRow) || (rcs != o.RowChkSum))

    Standard gate: update only if new row or checksum changed.

SaveOrders2024, 2025, 2026, 2027, 2028:
    if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))

    IMPORTANT: The last condition (rcs == o.RowChkSum) is always true when matched,
    because if rcs == o.RowChkSum the previous condition (rcs != o.RowChkSum) is false,
    so the OR chain reaches this condition and evaluates to true.
    In effect: SaveOrders2024+ ALWAYS does a full field update for every matched row,
    every run. The checksum comparison is effectively bypassed for these years.
    This was likely intentional to ensure late-arriving signature images and order
    status changes are always captured for recent orders.

________________________________________
9c. Column Mapping Table (All Fields)

The following fields are mapped identically across all Save methods (with guards noted).

Always parsed / always written:

Source Column       Destination Field   Type            Guard / Notes
-----------         ----------------    ----            -----
rowchksum           RowChkSum           int             Parsed from r["RowChkSum"] (note: key case)
medtype             MedType             varchar(50)     Always
dateadded           DateAdded           datetime        Always parsed — no length guard
orderdate           Orderdate           datetime        Always parsed — no length guard
doctor              Doctor              varchar(50)     Always
notes               Notes               varchar(1000)   Always; SaveOrders2024+ truncates to 999
                                                        chars if length > 1000
active              Active              bool            Always parsed via bool.Parse()
type                Type                varchar(50)     Always
stype               Stype               varchar(50)     Always
weeknum             Weeknum             int             Always parsed
splitfirst          SplitFirst          bool            Always parsed
blind               Blind               bool            Always parsed
o_user              OUser               varchar(100)    Always (column name has underscore in source)
cltM4id             CltM4id             varchar(50)     Always
pckcode             Pckcode             varchar(50)     Always
rxhistid            RxhistId            varchar(50)     Always
actbyuser           ActByUser           varchar(100)    Always
color               Color               varchar(50)     Always
deactbyuser         DeActbyUser         varchar(100)    Always
ordertypev5         OrderTypev5         varchar(50)     Always
overapprove         OverApprove         varchar(50)     Always
overapprovedt       OverapproveDt       varchar(50)     Always (stored as varchar despite "dt" name)
sigdr               SigDr               ntext           Always in 2016–2023; SCHEMA GUARD in 2024+
                                                        tbl.Columns.Contains("sigdr") check added
sigentered          Sigentered          ntext           Always in 2016–2023; SCHEMA GUARD in 2024+
                                                        tbl.Columns.Contains("sigentered")
signoted            Signoted            ntext           Always in 2016–2023; SCHEMA GUARD in 2024+
                                                        tbl.Columns.Contains("signoted")
sigmid              SigMid              ntext           Always in 2016–2023; SCHEMA GUARD in 2024+
                                                        tbl.Columns.Contains("sigmid")

Required dates (no length guard in 2016–2018, length guard added in 2019+):

Source Column       Destination Field   Guard in 2016-2018   Guard in 2019-2023
effectivedate       EffectiveDate       Always parsed        length > 7 before DateTime.Parse
expirationdate      ExpirationDate      Always parsed        length > 7 before DateTime.Parse
dose                Dose                Always parsed        length > 0 before decimal.Parse
dose2               Dose2               Always parsed        length > 0 before decimal.Parse

All days-of-week (always parsed in all methods):

sunday / sunday2    Sunday / Sunday2    bool — bool.Parse()
monday / monday2    Monday / Monday2    bool — bool.Parse()
tuesday / tuesday2  Tuesday / Tuesday2  bool — bool.Parse()
wednesday/wed2      Wednesday/Wed2      bool — bool.Parse()
thursday/thu2       Thursday / Thursday2 bool — bool.Parse()
friday / friday2    Friday / Friday2    bool — bool.Parse()
saturday/sat2       Saturday / Saturday2 bool — bool.Parse()

Nullable fields with length guards:

Source Column       Destination Field   Guard
changeby            Changeby            int — always parsed (no guard)
intervals           Intervals           Int16 — always parsed (no guard)
newdose             Newdose             int — length > 0 before int.Parse
ex                  Ex                  bool — length > 0 before bool.Parse
actbydate           ActbyDate           datetime — length > 0 before DateTime.Parse
white               White               bool — length > 0 before bool.Parse
repoldorder         RepOldOrder         decimal — length > 0 before decimal.Parse
dtsig               DtSig               datetime — length > 0 before DateTime.Parse
aws                 Aws                 bool — length > 0 before bool.Parse
blsched             BlSched             bool — length > 0 before bool.Parse
blverbal            BlVerbal            bool — length > 0 before bool.Parse
deactbydate         DeActbyDate         datetime — length > 0 before DateTime.Parse
signoteddt          SigNoteddt          datetime — length > 0 before DateTime.Parse
sigentereddt        Sigentereddt        datetime — length > 0 before DateTime.Parse
                                        Note: In SaveOrders2024+, wrapped in SCHEMA GUARD:
                                        tbl.Columns.Contains("sogentereddt") — this has a TYPO
                                        ("sogentereddt" not "sigentereddt"). The guard will
                                        never match, so sigentereddt is never stored in 2024+.

dtmid               Dtmid               datetime — TWO guards:
                                        1. length > 0 before any processing
                                        2. string != "1900-01-01 00:00:00.000"
                                        If the value equals that sentinel, Dtmid is NOT stored
                                        (left as whatever was in the existing record or null).
                                        Only truly non-sentinel, non-empty values are stored.

Signature images (always stored in 2016–2023; schema-optional in 2024+):

Source Column       Destination Field   Type        Transformation
sigdrimg            SigDrImg            byte[]      Encoding.ASCII.GetBytes(r["sigdrimg"].ToString())
SigMidImg           SigMidImg           byte[]      Encoding.ASCII.GetBytes(r["SigMidImg"].ToString())
SigNotedImg         SigNotedImg         byte[]      Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString())

In SaveOrders2024+:
    if tbl.Columns.Contains("sigdrimg"):   store SigDrImg
    if tbl.Columns.Contains("sigmidimg"):  store SigMidImg
    if tbl.Columns.Contains("signotedimg"):store SigNotedImg

These schema guards allow the same method to run against older SAMMS schemas that may
not expose the signature image columns in their source views.

________________________________________
9d. CltId Filter — 2024+ Only

In SaveOrders2024 through SaveOrders2028, an additional outer guard wraps the entire
per-row processing block:

    if (cltid > 0):
        (all lookup, construction, and mapping logic)
    // if cltid <= 0: row is silently skipped

This filters out rows where CltId is zero or negative at the BHGTaskRunner level before
they ever reach the lookup or construction logic. In earlier year methods (2016–2023),
negative CltIds are processed normally and may result in matching or inserting records
with negative CltId values in Azure.
________________________________________

10. Destination Tables — Azure BHG_DR (pats schema)

All 14 destination tables share the same column structure. They differ only in their
table names and the year of order data they contain.

________________________________________
10a. pats.tbl_Orders (generic table — historical predecessor)

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_Orders
EF Model: BHG-DR-LIB/Models/TblOrders.cs
Mapped  : [Table("tbl_Orders", Schema = "pats")]

Primary Key: SiteCode + OrderNum + CltId (3-column composite)

________________________________________
10b. pats.tbl_Orders2016 through pats.tbl_Orders2028 (year-partitioned tables)

Each year table has an identical column structure:

C# Property (EF)    SQL Column              Type                Notes
----------------    ---------------         ----                -----
SiteCode            SiteCode                varchar(25)         PK Part 1 — clinic code
OrderNum            OrderNum                int                 PK Part 2 — order number
CltId               cltID                   int                 PK Part 3 — patient ID
RowChkSum           RowChkSum               int                 Change detection hash (SQL CHECKSUM)
RowState            RowState                bit (nullable)      true=active, false=soft-deleted
LastModAt           LastModAt               datetime            ETL last write timestamp
MedType             medType                 varchar(50)         Medication type code
DateAdded           DateAdded               datetime (nullable) Order entry date
Orderdate           Orderdate               datetime (nullable) Clinical order date (year partition key)
Doctor              Doctor                  varchar(50)         Prescribing doctor name/ID
EffectiveDate       EffectiveDate           datetime (nullable) Order effective date
ExpirationDate      ExpirationDate          datetime (nullable) Order expiration date
Dose                Dose                    decimal (nullable)  Primary dose in mg
Dose2               Dose2                   decimal (nullable)  Secondary/split dose in mg
Changeby            Changeby                int (nullable)      User ID of last changer
Intervals           Intervals               smallint (nullable) Dosing interval code
Sunday              Sunday                  bit (nullable)      Take-home: Sunday primary
Monday              Monday                  bit (nullable)      Take-home: Monday primary
Tuesday             Tuesday                 bit (nullable)      Take-home: Tuesday primary
Wednesday           Wednesday               bit (nullable)      Take-home: Wednesday primary
Thursday            Thursday                bit (nullable)      Take-home: Thursday primary
Friday              Friday                  bit (nullable)      Take-home: Friday primary
Saturday            Saturday                bit (nullable)      Take-home: Saturday primary
Sunday2             Sunday2                 bit (nullable)      Take-home: Sunday alternate
Monday2             Monday2                 bit (nullable)      Take-home: Monday alternate
Tuesday2            Tuesday2                bit (nullable)      Take-home: Tuesday alternate
Wednesday2          Wednesday2              bit (nullable)      Take-home: Wednesday alternate
Thursday2           Thursday2               bit (nullable)      Take-home: Thursday alternate
Friday2             Friday2                 bit (nullable)      Take-home: Friday alternate
Saturday2           Saturday2               bit (nullable)      Take-home: Saturday alternate
Notes               Notes                   varchar(1000)       Free-text notes (truncated to 999 in 2024+)
Active              Active                  bit (nullable)      SAMMS active flag (mirrors source)
Type                Type                    varchar(50)         Order type code
Stype               Stype                   varchar(50)         Order sub-type code
Weeknum             Weeknum                 int (nullable)      Week number in multi-week schedule
SplitFirst          splitFIRST              bit (nullable)      Split dose on first dispensing
Blind               BLIND                   bit (nullable)      Double-blind protocol flag
OUser               o_User                  varchar(100)        Order entry username
CltM4id             cltM4ID                 varchar(50)         Patient M4 cross-reference ID
Newdose             newdose                 int (nullable)      New dose amount if dose change
Pckcode             pckcode                 varchar(50)         Dispensing package code
RxhistId            rxhistID                varchar(50)         Prescription history cross-reference
Ex                  EX                      bit (nullable)      Exception flag
ActbyDate           ActbyDATE               datetime (nullable) Activation date
ActByUser           ActByUser               varchar(100)        Activation username
White               white                   bit (nullable)      White card / override flag
RepOldOrder         repOldOrder             decimal (nullable)  Replaced order number reference
SigDr               sigDr                   ntext               Doctor's signature text
DtSig               dtSig                   datetime (nullable) Doctor signature date
Aws                 aws                     bit (nullable)      Automatic weekly schedule flag
BlSched             blSched                 bit (nullable)      Blank schedule flag
BlVerbal            blVerbal                bit (nullable)      Verbal order flag
Color               Color                   varchar(50)         Display color code
DeActbyDate         DeActbyDate             datetime (nullable) Deactivation date
DeActbyUser         DeActbyUser             varchar(100)        Deactivation username
OrderTypev5         OrderTypev5             varchar(50)         V5-compatible order type code
Sigentered          sigentered              ntext               Entry-by signature text
Signoted            signoted                ntext               Noted-by signature text
SigNoteddt          sigNOTEDDT              datetime (nullable) Noted-by signature date
Dtmid               DTMID                   datetime (nullable) Mid-level provider signature date
                                                                (excludes 1900-01-01 sentinel)
SigMid              sigMID                  ntext               Mid-level provider signature text
OverApprove         OverApprove             varchar(50)         Over-approval reason code
OverapproveDt       OverapproveDT           varchar(50)         Over-approval date (varchar, not datetime)
Sigentereddt        sigentereddt            datetime (nullable) Entry-by signature timestamp
SigDrImg            sigDrImg                varbinary           Doctor signature image (ASCII bytes)
SigMidImg           sigMidImg               varbinary           Mid-level signature image (ASCII bytes)
SigNotedImg         sigNotedImg             varbinary           Noted-by signature image (ASCII bytes)

Notes:
- The 3-column composite PK (SiteCode + OrderNum + CltId) is defined via [Key] attributes
  on all three properties in the EF model.
- OverapproveDt is stored as varchar(50) despite having "Dt" in the name. This is
  intentional — the source value is not always a valid date.
- All ntext fields (SigDr, SigMid, Sigentered, Signoted) store full text signatures.
  These can be large. In 2024+ they are schema-optional (Columns.Contains() guards).
________________________________________

11. Change Detection — RowChkSum and the Active Flag

RowChkSum is computed at the SAMMS SQL Server by SelectConstructor using SQL Server's
CHECKSUM() function across all mapped columns. It is returned as a column in the
DataTable alongside the actual data.

How it is used in SaveOrders2016–2023:
    if (NewRow || rcs != o.RowChkSum):
        → full field update (all ~50 fields written)
    else:
        → RowState = true, LastModAt refreshed only

Unchanged orders (checksum match, existing row) get only RowState and LastModAt updated.
This prevents unnecessary database writes for the vast majority of historical orders
that have not changed since the last run.

How it is used in SaveOrders (generic):
    if (NewRow || rcs != o.RowChkSum || rcs < 0):
        → full update always when checksum is negative (SQL CHECKSUM can return negatives)

How it is used in SaveOrders2024–2028:
    if (NewRow || rcs != o.RowChkSum || o.RowChkSum < 0 || rcs == o.RowChkSum):
        → always fires (see Section 9b for explanation)
    Effectively: every matched row is fully updated on every run for 2024+ years.

The Active flag is a separate independent mechanism:
- Active comes from the source SAMMS column of the same name (bool)
- It reflects whether the order is currently active in the clinical system
- Pre-pass sets Active = false for all rows; the upsert restores it from the source value
- An order with Active = true but RowState = false indicates a conflict state that
  should not occur under normal operation (source data sends Active, ETL manages RowState)
________________________________________

12. RowState and Active — Dual Soft-Delete Tracking

Both RowState and Active are managed on every run. They serve related but distinct roles.

RowState (ETL-managed):
Value       Meaning
-----       -------
true        Row exists in the SAMMS data returned this run — order confirmed present
false       Row was in Azure but NOT in today's SAMMS data — order may be deactivated,
            deleted, or outside the source query window

Active (SAMMS-sourced):
Value       Meaning
-----       -------
true        Order is currently active in SAMMS — patient may receive doses against it
false       Order has been deactivated, cancelled, or expired in SAMMS

Pre-pass behavior:
Both are set to false/false for all existing rows before the upsert loop begins.
This is a full reset — not date-windowed like SaveDoses or form-config-driven like
SaveFormQuestionAnswers.

After the upsert loop:
- Orders returned from SAMMS: RowState = true, Active = whatever SAMMS says (true/false)
- Orders NOT returned from SAMMS: remain RowState = false, Active = false (from pre-pass)

An order appearing in SAMMS with Active=false means the prescriber deactivated it — it
is returned from the source but stored with Active=false. RowState remains true because
the record still exists.

An order NOT appearing in SAMMS at all means it fell outside the query's WHERE condition —
RowState = false indicates this ETL run could not confirm its current status.

RowTrax destination count uses pats.vw_Orders with no RowState filter — it counts all
rows across all year tables for the site, not just active ones.
________________________________________

13. pats.vw_Orders — The Cross-Year Union View

pats.vw_Orders is a database view in Azure BHG_DR that UNIONs all year tables:

    SELECT * FROM pats.tbl_Orders2016
    UNION ALL SELECT * FROM pats.tbl_Orders2017
    UNION ALL SELECT * FROM pats.tbl_Orders2018
    ...
    UNION ALL SELECT * FROM pats.tbl_Orders2028

This view is used for:
- RowTrax destination count: count(1) from pats.vw_Orders where SiteCode = sc
- Downstream reporting and analytical queries that span all years
- Joins to dose records to validate that each dose has a corresponding order

The generic pats.tbl_Orders table is NOT included in pats.vw_Orders. Only year tables
2016–2028 are unioned.
________________________________________

14. Load Design Summary

Load type: Full-site upsert with complete pre-pass reset, RowChkSum change detection,
           year-partitioned tables, sequential year dispatch with failure gate

Per run behavior for all year tables:

  1. Single SELECT from SAMMS fetches ALL years at once (using task WhereCondition)
  2. DataTable split by OrderDate.Year — each year creates a separate DataTable slice
  3. For each year 2016–2028 (stopping at first failure):
     a. Load ALL Azure rows for SiteCode + year table into memory
     b. Pre-pass: set ALL rows to RowState=false, Active=false (full reset — no date filter)
     c. For each SAMMS row in this year's slice:
        - Lookup by OrderNum + CltId in the in-memory list
        - Found, checksum unchanged (2016-2023 only): RowState=true, LastModAt refresh
        - Found, checksum changed (or 2024+ unconditional): full field update, RowState=true
        - Not found: new row constructed, all fields written, staged for batch insert
     d. db.SaveChanges() — commits pre-pass resets + existing row updates
     e. db.TblOrders20YY.AddRange(newRows) + db.SaveChanges() — batch insert new rows
  4. Overall success = true only if ALL processed years returned true

Per-order identity:
Each order row is identified by the 3-column composite key (SiteCode + OrderNum + CltId).
OrderNum is unique within a clinic but a patient can have multiple orders with the same
OrderNum across different clinics — SiteCode disambiguates. CltId is also in the key
because in some SAMMS versions the same OrderNum can appear for different patients.

Year partition logic:
Orders are routed to the correct year table based on their OrderDate (the clinical order
date), not DateAdded. An order entered on 1/1/2023 with an OrderDate of 12/15/2022 would
be stored in pats.tbl_Orders2022.
________________________________________

15. Error Handling and Recovery

All SaveOrders methods use identical error handling:

    try
    {
        // pre-pass + upsert loop + SaveChanges() ...
    }
    catch (Exception e)
    {
        res = false
        Console.WriteLine(e.Message)
        Console.WriteLine(e.InnerException.Message)
    }

Note: In SaveOrders2024, the inner exception check includes a null guard:
    if (e.InnerException.Message != null):
        Console.WriteLine(e.InnerException.Message)

In all earlier methods, e.InnerException.Message is accessed without a null check,
which can itself throw a NullReferenceException if InnerException is null.

All errors are written to console only — the method returns false to BHGTaskRunner.

BHGTaskRunner failure gate behavior:
If any year method returns false:
    x = false
    All subsequent year methods are skipped (not called)
    rCodes.IsResult = false
    Task is marked as failed

Example failure scenario:
    SaveOrders2020 throws an exception → x = false
    SaveOrders2021 through SaveOrders2028 are NOT called
    Data for 2021-2028 is not updated for this clinic this run
    Task retries on next day's Scheduler reset

Recovery behavior:
The Scheduler resets failed tasks to Status=17 (ready) each day:
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

The next day's run retries ALL years from 2016 forward. There is no partial recovery
or year-specific retry — the entire clinic's orders run is retried from the beginning.
________________________________________

16. RowTrax — Audit and Row Count Tracking

Table: tsk.tbl_RowTrax (Azure BHG_DR)

After each successful orders run (if st.RowTrax = true AND st.ActionKey == 1):

    sd.SaveRowTrax(
        st.SiteCode,                          -- e.g. "B01"
        st.WorkDate.Value.Date,               -- today
        st.TaskName,                          -- "pats.tbl_orders"
        sourceCount,                          -- count from SAMMS
        destCount,                            -- count from pats.vw_Orders
        null)

Source count query (run against SAMMS):
    select count(1) from <SrcSchema>.<FromTblVw>
    where OrderDate is not null and cltID > 0

Destination count query (run against Azure):
    Select tblcnt = count(1) from pats.vw_Orders
    Where SiteCode = '<sc>'

Note on source count:
- Filter: OrderDate is not null and cltID > 0
- Negative/zero CltIds excluded from the count even though they may have been
  fetched in the DataTable (older year methods may process negative CltIds)
- This gives a count of "valid active orders" rather than raw row count

Note on destination count:
- Uses pats.vw_Orders — counts across ALL year tables for the site
- No RowState or Active filter — counts all rows regardless of status
- This means the destination count grows as historical orders accumulate

Note on ActionKey condition:
RowTrax is written only when st.ActionKey == 1. Orders tasks typically have ActionKey = 11.
If the task was created with ActionKey = 1 (e.g. via a manual or alternate task config),
RowTrax will be written. Otherwise it is silently skipped.
________________________________________

17. Key Design Notes and Gotchas

Year-partitioned tables — rationale:
Order records span the entire history of each patient's treatment. A clinic with 10
years of active patients may have hundreds of thousands of order records across all
years. Partitioning by year keeps each table smaller and allows year-specific queries
to scan only the relevant partition. pats.vw_Orders reunifies them for cross-year joins.

SaveOrders2024+ always updates every matched row:
The gate condition `(rcs == o.RowChkSum)` at the end of the OR chain is always true
when the previous `(rcs != o.RowChkSum)` is false — making the entire condition always
evaluate to true for matched existing rows. Every run fully updates all matched orders
for years 2024 and later, regardless of whether any data changed. This was intentional
to ensure recent orders always have their status and signature fields current.

Single fetch, multi-year dispatch:
The entire order history matching the WHERE condition is fetched in ONE DataTable from
SAMMS, then split in memory by OrderDate.Year. This is more efficient than running one
SQL query per year table, but means the DataTable can be large for clinics with long
order histories.

Failure gate stops subsequent years:
If 2019 orders fail, 2020-2028 are not processed. The failure is visible in the task
error status. The next day's retry will attempt all years again from 2016.

Both RowState AND Active are reset in pre-pass:
Unlike most other ETL tables that only reset RowState, SaveOrders also resets Active.
This means Active accurately reflects the current SAMMS status on every run. An order
that becomes inactive in SAMMS will have Active=false in Azure after the next run.

dtmid sentinel exclusion:
The value "1900-01-01 00:00:00.000" is a SAMMS sentinel meaning "not yet signed by
mid-level provider." This value is never stored in Dtmid — instead Dtmid remains null
(or its previously stored value for an unchanged row). This prevents sentinel dates
from appearing as if the mid-level provider signed in 1900.

overapprovedt stored as varchar:
Despite having "dt" in its name, OverapproveDt is stored as varchar(50) in both the
source and destination. The value from SAMMS is not guaranteed to be parseable as a
DateTime, so it is stored as a string.

sigentereddt typo in SaveOrders2024+:
The column check uses tbl.Columns.Contains("sogentereddt") — "so" instead of "si".
This will never match, so Sigentereddt is never updated in 2024+ year methods. This
is a known bug — the typo prevents the signature entered timestamp from being stored
for orders dated 2024 and later.

Schema-optional signature fields in 2024+:
sigdr, sigentered, signoted, sigmid, sigdrimg, sigmidimg, signotedimg are all wrapped
in Columns.Contains() guards in SaveOrders2024+. If the clinic's SAMMS source view
does not expose these columns (e.g. older schema), they are simply skipped rather than
throwing an exception. This makes the method compatible with a wider range of SAMMS
versions.

Notes field truncation in 2024+:
    if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
Earlier methods (2016-2023) do not truncate — if a notes field exceeds 1000 characters,
an EF Core exception would occur on SaveChanges for those methods.

Order vs date added partitioning:
BHGTaskRunner splits by the OrderDate column (the clinical order date), not by
DateAdded. This is important: an order that was entered late (DateAdded in 2024) but
has an OrderDate of 2023 will be stored in pats.tbl_Orders2023, not 2024. The year
table always reflects the clinical date of the prescription.

pats.tbl_Orders is not called daily:
The generic SaveOrders method (pats.tbl_Orders) is not called in the BHGTaskRunner
loop. Only SaveOrders2016 through SaveOrders2028 are dispatched. pats.tbl_Orders
likely exists as the original pre-partitioning table before year tables were introduced.
________________________________________

18. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-Orders (Status=17)
        |-- insert child tasks: one row per active clinic
        |       TaskName = 'pats.tbl_orders'
        |       SiteCode = 'B01', 'VBRA', etc.
        |       (single task per clinic — not per year)
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2 (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 11
        |
        |-- filter: TaskName = 'SAMMS-ETL-Orders', SiteCode != 'PHC', Status=17
        |-- mark parent task Status=18 (running)
        |
        |-- for each child task (one per clinic):
        |
        |   SelectConstructor builds SELECT + CHECKSUM() from dms.tbl_MapSrc2Dsn
        |   strCmd += " Where " + st.WhereCondition  (from task metadata)
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |   executes SELECT against clinic SAMMS SQL Server
        |   connection string from ctrl.tbl_LocationCons
        |          |
        |          V
        |   DataTable: ALL years of matching orders in one result set
        |          |
        |          |-- Count rows where OrderDate.Year == 2016 → if > 0:
        |          |       SaveOrders2016(yearSlice2016, SiteCode, 12/31/2016, null)
        |          |       → load pats.tbl_Orders2016 for site into memory
        |          |       → pre-pass: ALL rows → RowState=false, Active=false
        |          |       → loop: lookup by OrderNum+CltId
        |          |           found, checksum changed → full update, RowState=true
        |          |           found, checksum same → RowState=true, LastModAt refresh
        |          |           not found → new row, staged in ords list
        |          |       → SaveChanges() (updates+resets)
        |          |       → AddRange(ords) + SaveChanges() (inserts)
        |          |       → returns bool x
        |          |
        |          |-- if x, Count rows where OrderDate.Year == 2017 → if > 0:
        |          |       SaveOrders2017(yearSlice2017, SiteCode, 12/31/2017, null)
        |          |       → same pattern → pats.tbl_Orders2017
        |          |
        |          |-- if x, ... repeat for 2018, 2019, 2020, 2021, 2022, 2023 ...
        |          |
        |          |-- if x, Count rows where OrderDate.Year == 2024 → if > 0:
        |          |       SaveOrders2024(yearSlice2024, SiteCode, 12/31/2024, null)
        |          |       → cltid > 0 filter (skip non-positive CltIds)
        |          |       → checksum gate ALWAYS fires → full update every matched row
        |          |       → schema-optional guards for sig fields
        |          |       → pats.tbl_Orders2024
        |          |
        |          |-- if x, ... repeat for 2025, 2026, 2027, 2028 ...
        |          |
        |          V
        |   rCodes.IsResult = x (false if any year method threw)
        |   rCodes.RowsProcessed = SrcDt.Rows.Count (total across all years)
        |          |
        |          |-- RowTrax (if st.RowTrax=true AND st.ActionKey==1):
        |          |   source = count(SAMMS where OrderDate not null and cltID > 0)
        |          |   dest   = count(pats.vw_Orders where SiteCode = sc)
        |          |   → tsk.tbl_RowTrax
        |          |
        |          V
        |   BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)
________________________________________

19. File Reference Map

File Path                                           Purpose
---------                                           -------
BCAppCode/Scheduler/Program.cs                      Creates daily task queue — inserts SAMMS-ETL-Orders tasks
                                                    Maps pats.tbl_Orders → SAMMS-ETL-Orders TaskName
BCAppCode/BHGTaskRunner/Program.cs                  Main ETL driver (arg=11 → Orders pipeline)
                                                    Contains year-split logic (2016-2028)
                                                    Contains WhereCondition-based WHERE clause
                                                    Contains RowTrax call with pats.vw_Orders count
BCAppCode/BHG-DR-LIB/SaveOrders.cs                 EF Core upsert — 14 methods:
                                                    SaveOrders (generic, not called daily)
                                                    SaveOrders2016 through SaveOrders2028
BCAppCode/BHG-DR-LIB/SelectConstructor.cs          Builds SELECT + CHECKSUM() from metadata
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs              ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblOrders.cs           EF Model → pats.tbl_Orders (generic)
BCAppCode/BHG-DR-LIB/Models/TblOrders2016.cs       EF Model → pats.tbl_Orders2016
BCAppCode/BHG-DR-LIB/Models/TblOrders2017.cs       EF Model → pats.tbl_Orders2017
BCAppCode/BHG-DR-LIB/Models/TblOrders2018.cs       EF Model → pats.tbl_Orders2018
BCAppCode/BHG-DR-LIB/Models/TblOrders2019.cs       EF Model → pats.tbl_Orders2019
BCAppCode/BHG-DR-LIB/Models/TblOrders2020.cs       EF Model → pats.tbl_Orders2020
BCAppCode/BHG-DR-LIB/Models/TblOrders2021.cs       EF Model → pats.tbl_Orders2021
BCAppCode/BHG-DR-LIB/Models/TblOrders2022.cs       EF Model → pats.tbl_Orders2022
BCAppCode/BHG-DR-LIB/Models/TblOrders2023.cs       EF Model → pats.tbl_Orders2023
BCAppCode/BHG-DR-LIB/Models/TblOrders2024.cs       EF Model → pats.tbl_Orders2024
BCAppCode/BHG-DR-LIB/Models/TblOrders2025.cs       EF Model → pats.tbl_Orders2025
BCAppCode/BHG-DR-LIB/Models/TblOrders2026.cs       EF Model → pats.tbl_Orders2026
BCAppCode/BHG-DR-LIB/Models/TblOrders2027.cs       EF Model → pats.tbl_Orders2027
BCAppCode/BHG-DR-LIB/Models/TblOrders2028.cs       EF Model → pats.tbl_Orders2028
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs       EF DbContext — registers all TblOrders20YY tables
________________________________________

20. Quick Reference Summary

What triggers Orders ETL?            Scheduler.exe creates tasks, BHGTaskRunner.exe 11 processes them
TaskName in scheduler?               SAMMS-ETL-Orders
Tasks per clinic?                    ONE task per clinic (not one per year) — BHGTaskRunner splits internally
Source table in SAMMS?               dbo.tblOrder or site-specific view (st.FromTblVw from task metadata)
Destination tables in Azure?         pats.tbl_Orders (generic — not used in daily run)
                                     pats.tbl_Orders2016 through pats.tbl_Orders2028
Union view?                          pats.vw_Orders — UNIONs all year tables, used for RowTrax + reporting
Primary key?                         SiteCode + OrderNum + CltId (3-column composite, all years)
How is change detected?              RowChkSum = CHECKSUM() across all mapped columns
                                     2016-2023: update only when checksum differs or new row
                                     2024+: always update every matched row (gate always true)
                                     SaveOrders (generic): also updates when rcs < 0
What is RowState?                    bit (nullable) — true=active/present in source, false=not in source
What is Active?                      bit (nullable) — mirrors SAMMS active flag; source-controlled
Both reset in pre-pass?              Yes — both RowState=false AND Active=false for all existing rows
Pre-pass date filter?                None — ALL existing rows for the site are reset every run
CltId filter in 2024+?               Yes — rows with cltid <= 0 are skipped (not processed or inserted)
How are new rows inserted?           Batched — collected in ords list → AddRange + SaveChanges
Failure gate behavior?               If any year method fails, all subsequent years are skipped
Year selection logic?                Split by OrderDate.Year — NOT DateAdded.Year
                                     An order dated 2022 goes to pats.tbl_Orders2022 regardless of entry date
dtmid sentinel?                      "1900-01-01 00:00:00.000" excluded — stored as null if received
sigentereddt bug in 2024+?           tbl.Columns.Contains("sogentereddt") typo — never matches; sigentereddt
                                     is NOT stored for orders in years 2024 and later
Notes truncation in 2024+?           Notes > 1000 chars truncated to 999 chars (earlier methods do not truncate)
Schema-optional columns in 2024+?    sigdr, sigentered, signoted, sigmid, sig*Img fields all have Columns.Contains guards
Signature images stored as?          byte[] via System.Text.Encoding.ASCII.GetBytes(stringValue)
overapprovedt type?                  varchar(50) — NOT a datetime despite the "dt" name
Error recovery?                      Scheduler resets failed tasks to Status=17 on next daily run
RowTrax written when?                ActionKey == 1 (Orders tasks use ActionKey=11, so may not write RowTrax)
RowTrax source count filter?         OrderDate is not null and cltID > 0
RowTrax dest count uses?             pats.vw_Orders — no RowState filter, counts all rows
PHC handled here?                    No — PHC uses PHC/Program.cs and is excluded by SiteCode != "PHC"
SaveOrders (generic) called daily?   No — only SaveOrders2016 through SaveOrders2028 are called by BHGTaskRunner
________________________________________
