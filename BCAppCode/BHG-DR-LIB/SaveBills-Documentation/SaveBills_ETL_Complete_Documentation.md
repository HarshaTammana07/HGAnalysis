
Billing / Auth-Bill ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 8 — SAMMS-ETL-INV (primary) / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract billing transaction
records and insurance billing view records from local SAMMS SQL Server databases at each
clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What billing (Bills) and auth-bill (AuthBills) data are and why they exist
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and BHGTaskRunner dispatches them under Schedule 8
- How all three methods in SaveBills.cs work in detail
- The unique year-based source WHERE clause used for pats.tbl_bills (vs the standard rolling date)
- The Reload flag that expands DaysBack to -728250 for full-history reloads
- The partial pre-pass RowState reset pattern in SaveBills
- The full pre-pass RowState reset pattern in SaveAuthBills
- RowState derivation from BillCltid and DsClt (negative = inactive)
- The PHC SiteId hardcode (105) in both SaveBills and SaveAuthBills
- The two SaveBills overloads — primary (DataTable) and legacy (string/standalone)
- All known anomalies, bugs, and dead code

There are three methods in SaveBills.cs spanning two Azure destination tables:

pats.tbl_Bills:     SaveBills(DataTable)    (primary daily path — called from BHGTaskRunner)
pats.tbl_Bills:     SaveBills(string)       (legacy standalone path — NOT called from BHGTaskRunner)
pats.tbl_vw3pbill:  SaveAuthBills           (insurance billing view — new records broken, see Section 10)
________________________________________

2. High-Level Business Summary

What is billing (Bills) data?

The billing table is the core financial transaction record in the SAMMS system. Each row
represents a single billing event for a patient visit — capturing the charge amount (BillBill),
payment received (BillPay), adjustment amount (BillAdjust), payment type (BillPaytype), the
linked patient (BillCltid), the bill date, receipt number, service link (BillServId), appointment
link (BillAptId), original date (BillOrgdt), deposit flag (BlnDeposit), FIFO allocation and
balance fields (Fifoallocated, Fifobalance), cost center, staff user, and a narrative reason
field (BillReason — free text, truncated to 2498 characters). BillSiteId is numeric and is
hardcoded to 105 for the PHC site.

RowState tracks whether the record is considered active. It is driven by BillCltid: any
record with BillCltid <= 0 is marked RowState=false (inactive/null patient). This supports
partial pre-pass soft-delete — records within the date window that are not returned in the
source pull are deactivated.

pats.tbl_vw3pbill — Insurance Billing View Records
This table holds a flattened join of DartsSrv service data with payer and patient demographic
fields, structured for insurance billing reporting. It mirrors much of the same layout as
pats.tbl_vw3pBillSub (see SaveAuths.cs) but uses a simpler single-key lookup (DsId) rather
than the multi-component key. RowState is derived from DsClt: a negative client ID marks the
record as inactive. PHC site gets SiteId=105 hardcoded.

Why this data is important
- Billing records are the primary financial audit trail per patient per service date
- FIFO allocation fields (Fifoallocated, Fifobalance) support the practice management
  workflow for tracking payment application and outstanding balances
- The year-based source window ensures that current-year billing history is always refreshed,
  not just the last 15 days — critical for late payment postings and adjustments
- The auth-bill view (pats.tbl_vw3pbill) feeds insurance billing reports and dashboards

Load type
SaveBills (DataTable) uses a partial pre-pass RowState reset (reset RowState=true → false
within the date window, committed immediately), followed by a RowChkSum-guarded EF Core upsert
with direct field assignment. SaveBills (string) is a legacy standalone method with parallel
async data loading and a dynamic column switch. SaveAuthBills uses a full pre-pass RowState
reset and per-row object construction with a 35-field column switch, but its new-record insert
path is broken — new records are added to an in-memory list only and never persisted.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=8                     Main ETL orchestrator for SAMMS-ETL-INV
BHGTaskRunner.exe arg=3                     Catch-all schedule — also processes these tasks
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)              Column list + RowChkSum expression for SELECT build
dms.tbl_MapAction (Azure DB)               Maps TaskName to source table/view per task
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SaveBills.cs (BHG-DR-LIB)                  3 methods — SaveBills x2, SaveAuthBills
Models/TblBills.cs                          EF entity → pats.tbl_Bills
Models/TblVw3pbill.cs                       EF entity → pats.tbl_vw3pbill
pats.tbl_Bills (Azure BHG_DR)             Final destination for billing transactions
pats.tbl_vw3pbill (Azure BHG_DR)          Final destination for insurance billing view records
tsk.tbl_RowTrax (Azure DB)                 Audit log — source vs destination row counts
________________________________________

4. Scheduler — How Billing Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task for SAMMS-ETL-INV schedule
    TaskName = 'SAMMS-ETL-INV'
    SiteCode = 'All'
    Status   = 17

Step 3 — Insert child tasks per clinic
For each active clinic, child tasks are inserted:
    TaskName = 'pats.tbl_bills'       SiteCode = 'B01A'
    TaskName = 'pats.tbl_vw3pbill'    SiteCode = 'B01A'
    ... (one row per table per clinic)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How Billing Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 8 for the SAMMS-ETL-INV schedule:
    BHGTaskRunner.exe 8

Step 1 — Filter queue by SAMMS-ETL-INV task name
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"
        && x.Status == 17
        && x.TaskName == "SAMMS-ETL-INV"
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

CASE: pats.tbl_bills
    //strCmd += " Where " + strWhere + " " + st.SortOrder    ← STANDARD WHERE IS COMMENTED OUT

    Reload flag — controls BillDaysBack:
        int BillDaysBack = DaysBack   (default -15)
        if (st.Reload.HasValue && st.Reload.Value == true)
        {
            BillDaysBack = -728250    ← 2000 years — effectively all-time full reload
        }

    CUSTOM WHERE (year-based, not standard rolling date):
        strCmd += " where year(billDate) >= " + WorkDate.AddDays(BillDaysBack).Year
               + " and billdate <= '" + WorkDate.AddDays(12).ToShortDateString() + "'"
               + " " + st.SortOrder
    This fetches all bills from the start of the year corresponding to (WorkDate + BillDaysBack)
    up to 12 days into the future. For a normal run (DaysBack=-15), this covers current year
    bills up to 12 days ahead — not just the last 15 days.

    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    if (SrcDt.Rows.Count > 0)
    {
        rCodes = sd.SaveBills(SrcDt, st.SiteCode, st.WorkDate.Value.Date, BillDaysBack, null)
    }
    else
    {
        rCodes.IsResult = true; rCodes.RowsProcessed = 0;   ← no-op if no source rows
    }
    RowTrax block: EMPTY — no RowTrax logging fires for pats.tbl_bills

CASE: pats.tbl_vw3pbill
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveAuthBills(SrcDt, st.SiteCode, st.WorkDate.Value.Date, false, null)
    RowTrax audit fires if st.RowTrax = true

Step 6 — RowTrax audit (pats.tbl_vw3pbill only — pats.tbl_bills block is empty)

Step 7 — Mark child task Status=20 (complete)

Schedule 3 note:
BHGTaskRunner.exe 3 is the catch-all schedule — its task filter includes all tasks that are
not P1, P2, or SAMMSGlobal. If SAMMS-ETL-INV child tasks are queued when arg=3 runs, they
are also processed by the same inner switch. In practice, the dedicated arg=8 run handles
these tasks first.
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the full SELECT, and returns a DataTable. Connection strings are from
ctrl.tbl_LocationCons. Typical SAMMS source names:
    dbo.tblBills (or clinic view)      → pats.tbl_Bills
    dbo.vw3pBill (or clinic view)      → pats.tbl_vw3pbill
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

7a. dbo.tblBills — Billing Transaction Records

Key columns:
    billid            int         Unique billing record ID (primary key component)
    billcltid         int         Patient/client ID (0 or negative = inactive/null patient)
    RowChkSum         int         CHECKSUM() computed over key billing fields
    billdate          datetime    Bill date and time
    billbill          decimal     Charge amount
    billpay           decimal     Payment amount received
    billpaytype       varchar     Payment type code
    billadjust        decimal     Adjustment amount
    billreason        varchar     Narrative reason for adjustment (free text — may exceed 2500 chars)
    billreceiptnum    int         Receipt number
    struser           varchar     Staff user who created the bill
    blnDeposit        bool        Is this a deposit payment?
    billadjustid      int         Linked adjustment record ID
    Fifoallocated     bool        FIFO payment allocation flag
    Fifobalance       decimal     Remaining FIFO balance
    Costcenter        varchar     Cost center code
    BillAptId         int         Linked appointment record ID
    BillOrgdt         datetime    Original bill date
    BillServId        int         Linked service record ID
    BillSiteId        int         Site numeric ID (PHC hardcoded to 105)
    billguestid       int         Guest patient ID

7b. dbo.vw3pBill — Insurance Billing View (flattened)

Key columns (32 mapped fields):
    dsid              int         DartsSrv service record ID (primary lookup key)
    dsclt             int         Patient/client ID (negative = inactive → RowState=false)
    descript          varchar     Service description
    billdatecriteria  datetime    Billing date criteria
    paydefaultsubmit  varchar     Default submission payer
    scruberror        varchar     Scrub error message
    dstxtsrv          varchar     Service text
    dsdtstart         datetime    Service start date
    dsdtend           datetime    Service end date
    dstxttype         varchar     Service type text
    dsdblunits        double      Service units
    billunits         double      Billed units (NO null guard — see Anomaly 6)
    dstxtstaff        varchar     Staff member text
    npi               varchar     Provider NPI number
    dsbilled          datetime    Date billed/submitted
    pypayerid         varchar     Payer ID
    pysubsid          varchar     Subscriber ID
    pygroup           varchar     Group number
    cptcode           varchar     CPT procedure code
    modifier          varchar     CPT modifier
    charge            double      Charge amount
    tpaauthcode       varchar     Authorization code
    clientname        varchar     Patient name
    cltdob            datetime    Patient date of birth
    cltgender         varchar     Patient gender
    cltadd1           varchar     Patient address
    cltcity           varchar     Patient city
    cltstate          varchar     Patient state
    cltzip            varchar     Patient ZIP
    cltphone          varchar     Patient phone
    cltmarry          varchar     Patient marital status
    cltm4id           varchar     Patient Medicaid ID
    dsdiag            varchar     Diagnosis code
    dspos             varchar     Place of service
    ndc               varchar     NDC drug code
    mg                double      Medication mg amount
    siteid            int         Site numeric ID (PHC hardcoded to 105)
    dsarea            varchar     Service area
    payclass          varchar     Payer class
    sitecode          varchar     Site code (overwritten from source — see Anomaly 7)
________________________________________

8. SaveBills (DataTable overload) — Primary Daily Billing Load (pats.tbl_Bills)

Source: dbo.tblBills (or clinic-specific view)
Destination: pats.tbl_Bills
Key: SiteCode + BillId
Parameters: tbl, sc, wrkdt, DaysBack, db

Short-circuit: If tbl.Rows.Count == 0, method returns immediately without entering the
try block — rcodes.RowsProcessed is 0, IsResult remains true.

wrkdt normalization: wrkdt = DateTime.Parse(wrkdt.ToShortDateString()) — strips the time
component to midnight, ensuring date comparisons are day-aligned.

Azure pre-load with year-based date window:
    bills = db.TblBills.Where(x =>
        x.SiteCode == sc
        && x.BillDate.Value.Year >= wrkdt.AddDays(DaysBack).Year
        && x.BillDate <= wrkdt.AddDays(15)).ToList()
Lower bound: year of (wrkdt + DaysBack). For a normal run (DaysBack=-15), this is the
current year. For a full reload (DaysBack=-728250), this is year 1 (all-time).
Upper bound: wrkdt + 15 days (slightly ahead of today).
This is a year-start-to-near-future window — it loads all records from the beginning of
the relevant year, not just a rolling 15-day window.

Pre-pass partial RowState reset:
    foreach (TblBills b in bills)
    {
        if (b.RowState) { b.RowState = false; b.LastModAt = DateTime.Now; }
    }
    db.SaveChanges()    ← committed immediately before the main loop
Only records where RowState is currently true are reset. This is a partial pre-pass —
records already false are left unchanged. Unlike SaveAuths (which resets all records),
this method resets only active records within the loaded date window.

Important: db.SaveChanges() is called immediately after the pre-pass — BEFORE the main
processing loop. This means the soft-delete of existing active records is committed to the
database before any new data is processed.

AllNewRows is permanently disabled:
    //if (bills == null) { AllNewRows = true; }    ← COMMENTED OUT
AllNewRows is always false. Every source row goes through the lookup path.

Pre-read per row: billid, bcltid (length > 0 guard), myrcs (RowChkSum).

Lookup key: SiteCode + BillId
    bill = bills.FirstOrDefault(x => x.SiteCode == sc && x.BillId == billid)
Note: BillCltid was removed from the lookup key in June 2023:
    //&& x.BillCltid == bcltid  //Removed 20230621
Bills now match solely on BillId within the site.

RowChkSum null guard:
    if (bill.RowChkSum == null) { bill.RowChkSum = 0; }
Applied before the guard check — prevents NullReferenceException on nullable RowChkSum.

RowChkSum guard:
    if ((bill.RowChkSum != myrcs) || (NewRow))
    {
        // RowState derivation from bcltid
        if (bcltid <= 0) { bill.RowState = false; } else { bill.RowState = true; }
        bill.RowChkSum = myrcs
        // field mapping (inner try/catch)
        bill.LastModAt = DateTime.Now
        // new-row handling or Update
    }
    else
    {
        // checksum unchanged — still update LastModAt and RowState
        bill.LastModAt = DateTime.Now
        if (bcltid <= 0) { bill.RowState = false; } else { bill.RowState = true; }
        rcodes.RowsUpd += 1
    }
RowState is re-derived from bcltid on BOTH the change-detected path and the checksum-match
path. Even unchanged bills get their RowState refreshed and LastModAt updated.

Inner try/catch around field mapping:
    try
    {
        // direct field assignment for 18 fields
    }
    catch (Exception e)
    {
        foreach (DataColumn g in tbl.Columns)
        {
            Console.WriteLine(g.ColumnName + ": " + r[g.ColumnName].ToString())
        }
    }
On any field parsing exception, all column name/value pairs are dumped to Console.
The exception is swallowed — processing continues for the row. This diagnostic approach
prevents a single bad row from aborting the site's entire load.

PHC SiteId hardcode: if (bill.SiteCode == "PHC") { bill.BillSiteId = 105; }

BillReason truncation guard:
    bill.BillReason = r["billreason"].ToString().Trim()
    if (bill.BillReason.Length > 2500) { bill.BillReason = bill.BillReason.Substring(0, 2498); }
Guard is > 2500 but Substring target is 2498 — off-by-two (see Anomaly 2).

Double database check for new rows:
    if (NewRow || AllNewRows)
    {
        Models.TblBills bl = db.TblBills.FirstOrDefault(x =>
            x.SiteCode == bill.SiteCode && x.BillId == bill.BillId)
        if (bl == null) { db.TblBills.Add(bill); }
        //else { db.TblBills.Update(bill); }    ← commented out
        NewRow = false
        rcodes.RowsIns += 1
    }
Before adding a new record, a live database query checks whether the record already exists
in Azure (not just in the in-memory bills list). This prevents duplicate key violations for
records that exist in Azure but fall outside the pre-loaded date window. If found in Azure,
the bill is NOT updated (commented out) — only inserted if truly absent.

Updated rows:
    else { db.TblBills.Update(bill); rcodes.RowsUpd += 1; }

NewBills dead code block:
    if (NewBills.Count > 0)    ← NewBills is always empty (NewBills.Add(bill) commented out)
    {
        foreach (TblBills nb in NewBills)
        {
            TblBills dbBill = db.TblBills.FirstOrDefault(...)
            if (dbBill == null) { db.TblBills.Add(nb); }
            else { db.TblBills.Update(nb); }
        }
        db.SaveChanges()
    }
NewBills.Add(bill) is commented out at line 141. NewBills is always empty. This entire
block including the live DB queries is dead code (see Anomaly 3).

Commit sequence:
    db.SaveChanges()    ← immediately after pre-pass reset
    ... main loop (Add / Update per row) ...
    db.SaveChanges()    ← commits all main loop changes
    [dead code SaveChanges if NewBills.Count > 0]

Field mapping (SaveBills DataTable — 18 fields, direct assignment not switch-based):

    Source field        EF property         Type / notes
    ---------           -----------         -----
    (pre-read)          BillId              int — r["billid"]
    (pre-read)          BillCltid           int — r["billcltid"], 0 if empty
    (pre-read)          RowChkSum           int — r["RowChkSum"]
    (derived)           RowState            bool — bcltid <= 0 → false, else true
    billcltid           BillCltid           int — length > 0 guard
    billguestid         BillGuestId         int — length > 0 guard
    billdate            BillDate            DateTime? — length > 7 guard
    billbill            BillBill            decimal? — length > 0 guard
    billpay             BillPay             decimal? — length > 0 guard
    billpaytype         BillPaytype         string
    billadjust          BillAdjust          decimal? — length > 0 guard
    billreason          BillReason          string — Trim() + truncate to 2498 if > 2500
    billreceiptnum      BillReceiptNum      int? — length > 0 guard
    struser             StrUser             string
    blnDeposit          BlnDeposit          bool? — bool.Parse; length > 0 guard
    billadjustid        BillAdjustid        int? — length > 0 guard
    Fifoallocated       Fifoallocated       bool? — bool.Parse; length > 0 guard
    Fifobalance         Fifobalance         decimal? — length > 0 guard
    Costcenter          Costcenter          string
    BillAptId           BillAptId           int? — length > 0 guard
    BillOrgdt           BillOrgdt           DateTime? — length > 0 guard (WEAK — not > 7)
    BillServId          BillServId          int? — length > 0 guard
    BillSiteId          BillSiteId          int? — length > 0 guard; PHC override 105
________________________________________

9. SaveBills (string overload) — Legacy Standalone Billing Load

Source: SrcCmd / SrcCon (caller-supplied SQL and connection string)
Destination: pats.tbl_Bills
Key: BillId (site-scoped via pre-load)
Parameters: SrcCmd, SrcCon, sc, wrkdt, yearly

NOT CALLED FROM BHGTaskRunner. This overload is a standalone, self-contained method
likely used from bhg.TestCode or for manual backfill runs. It creates its own
BHG_DRContext and SQLSvrManager internally.

Parallel async loading:
    Task stask = Task.Run(() => { SrcDt = ssm.GetTableData("Bills", SrcCmd, SrcCon); })
    Task ztask = Task.Run(() => { bills = db.TblBills.Where(...).ToList(); })
Source data fetch and Azure pre-load run concurrently. stask.Wait() is called AFTER
ztask.Wait(), so both complete before the main loop.

yearly flag — FUNCTIONAL (unlike most other yearly parameters in the codebase):
    yearly=true:  full site slice — all bills for site
    yearly=false: rolling 2-month window — BillDate >= wrkdt.AddMonths(-1) AND BillDate <= wrkdt.AddDays(31)

AllNewRows: Set if bills == null. However Task.Run().ToList() never returns null —
AllNewRows is dead code in practice.

Uses dynamic column switch (unlike the DataTable overload's direct assignment):
Field mapping via switch on c.ColumnName.ToLower() covering the same 18+ fields.
No BillReason length guard in this overload's switch — still applies same 2500/2498 truncation.
No RowState logic — this overload does not manage RowState.
No PHC SiteId handling.

Unconditional Update bug:
    db.TblBills.Update(bill)    ← called OUTSIDE the RowChkSum guard, for EVERY row
At line 369, after the guard block, db.TblBills.Update(bill) is called unconditionally.
For new rows that were just staged via db.TblBills.Add(bill), calling Update immediately
after may cause EF Core to issue both an INSERT and an UPDATE for the same entity, or
to override the Add tracking state. This is an EF Core anti-pattern (see Anomaly 5).

No LastModAt in the column switch — the "lastmodat" case sets bill.LastModAt = DateTime.Now.
This case fires within the RowChkSum guard block only.

Commit: Single db.SaveChanges() at end of loop.
________________________________________

10. SaveAuthBills — Insurance Billing View Load (pats.tbl_vw3pbill)

Source: dbo.vw3pBill (or clinic-specific view)
Destination: pats.tbl_vw3pbill
Key: DsId (single key — not composite)
Parameters: tbl, sc, wrkdt, yearly, db
Note: wrkdt and yearly are accepted but never used inside the method.

Azure pre-load:
    pbills = db.TblVw3pbill.Where(x => x.SiteCode == sc).ToList()
Full site slice — all existing billing view records for the clinic.

Full pre-pass RowState reset:
    foreach (var pb in pbills) { if ((bool)pb.RowState) { pb.RowState = false; } }
    //db.SaveChanges()    ← COMMENTED OUT — reset NOT committed until terminal SaveChanges
All currently-active records are set RowState=false. Unlike SaveBills (DataTable), no
intermediate SaveChanges is called — the reset and all subsequent changes are committed
together at the end.

Per-row object construction:
For each source row, a NEW TblVw3pbill object pb is created and populated via a 32-field
column switch. Object is built fresh and THEN looked up — reverse of the standard pattern.

RowState derivation from DsClt:
    case "dsclt":
        pb.DsClt = int.Parse(dr[c.ColumnName].ToString())
        if (pb.DsClt < 0) { pb.RowState = false; } else { pb.RowState = true; }
Negative client ID marks the record as inactive. No null/empty guard on this parse — an
empty DsClt would throw FormatException.

PHC SiteId handling:
    case "siteid":
        if (pb.SiteCode == "PHC") { pb.SiteId = 105; }
        else { if (length > 0) { pb.SiteId = int.Parse(...); } }
This references pb.SiteCode — but at the time the "siteid" case fires, pb.SiteCode may or
may not have been set yet (depends on column order in the DataTable). If "sitecode" column
appears after "siteid" in the schema, the PHC check will use the default empty string and
the override will never fire (see Anomaly 7).

Lookup key: DsId only
    pbx = pbills.FirstOrDefault(x => x.DsId == pb.DsId)

Found path (pbx != null):
    35 fields explicitly copied from pb to pbx
    pbx.LastModAt = DateTime.Now
    pbx.RowState = pb.RowState    ← RowState from DsClt derivation
    rcode.RowsUpd++
Note: PyPayerid, PySubsid, and PyGroup ARE updated here (unlike SaveAuthBillsub which
excludes them from updates because they are part of the lookup key).

CRITICAL BUG — New records never inserted:
    else
    {
        pb.LastModAt = DateTime.Now
        //newpbs.Add(pb)    ← COMMENTED OUT
        pbills.Add(pb)     ← adds to in-memory list only
        rcode.RowsIns++
    }
New records are added to the local pbills List<TblVw3pbill> (in-memory) but NOT to the
EF Core DbContext. The AddRange block at the end is also commented out:
    //if (newpbs.Count > 0) { db.TblVw3pbill.AddRange(newpbs); db.SaveChanges(); }
The single db.SaveChanges() at line 623 has no new inserts to commit. New records reported
in rcode.RowsIns are never actually written to Azure (see Anomaly 4).

Commit: Single db.SaveChanges() at end — commits pre-pass resets and all field updates
to tracked entities. New inserts are silently discarded.

Column mapping (SaveAuthBills — 32 fields via switch):

    Source column       EF property         Type / notes
    ---------           -----------         -----
    sitecode            SiteCode            string — from source (may differ from sc parameter!)
    descript            Descript            string
    billdatecriteria    Billdatecriteria    DateTime? — length > 6 guard
    paydefaultsubmit    PayDefaultsubmit    string
    scruberror          ScrubError          string
    dsid                DsId                int — length > 0 guard
    dsclt               DsClt               int + RowState derivation (no empty guard)
    dstxtsrv            DsTxtSrv            string
    dsdtstart           DsDtStart           DateTime? — length > 6 guard
    dsdtend             DsDtEnd             DateTime? — length > 6 guard
    dstxttype           DsTxtType           string
    dsdblunits          DsdblUnits          double? — length > 0 guard
    billunits           BillUnits           double — NO guard (FormatException on empty)
    dstxtstaff          DstxtStaff          string
    npi                 Npi                 string
    dsbilled            Dsbilled            DateTime? — length > 6 guard
    pypayerid           PyPayerid           string
    pysubsid            PySubsid            string
    pygroup             PyGroup             string
    cptcode             Cptcode             string
    charge              Charge              double? — length > 0 guard
    tpaauthcode         TpaAuthCode         string
    clientname          Clientname          string
    cltdob              CltDob              DateTime? — length > 6 guard
    cltgender           CltGender           string
    cltadd1             CltAdd1             string
    cltcity             CltCity             string
    cltstate            CltState            string
    cltzip              Cltzip              string
    cltphone            CltPhone            string
    cltmarry            CltMarry            string
    cltm4id             CltM4id             string
    dsdiag              Dsdiag              string
    modifier            Modifier            string
    dspos               DsPos               string
    ndc                 Ndc                 string
    mg                  Mg                  double? — length > 0 guard
    siteid              SiteId              int? — PHC override 105; length > 0 guard
    dsarea              Dsarea              string
    payclass            Payclass            string
________________________________________

11. Shared EF Core Patterns

BHG_DRContext:
    if (db == null) { db = new Models.BHG_DRContext(); }
SaveBills (DataTable) and SaveAuthBills accept an optional db context. If null, a new
context is instantiated. SaveBills (string) always creates its own internal context.

RCodes return:
    rcodes.IsResult = true initially
    rcodes.RowsProcessed = tbl.Rows.Count (DataTable overload)
    rcodes.RowsIns incremented per new row
    rcodes.RowsUpd incremented per updated row (or checksum-unchanged row in DataTable overload)
On exception: rcodes.IsResult = false, rcodes.ExceptMsg populated, InnerException captured.

Error handling (SaveBills DataTable and SaveAuthBills):
    catch (Exception e)
    {
        rcodes.IsResult = false
        rcodes.ExceptMsg = e.Message
        if (e.InnerException != null) { rcodes.ExceptInnerMsg = e.InnerException.Message; }
    }
Note: No Console.WriteLine in the outer catch for these two methods — only the inner
per-field try/catch in SaveBills (DataTable) writes to Console.

Error handling (SaveBills string):
    catch (Exception e)
    {
        rcodes.IsResult = false
        rcodes.ExceptMsg = e.Message
        if (e.InnerException != null) { rcodes.ExceptInnerMsg = e.InnerException.Message; }
    }
Same pattern, no Console output.
________________________________________

12. Change Detection — RowChkSum Behaviour

Method                    RowChkSum present    Guard used    Effective behaviour
------                    -----------------    ----------    -------------------
SaveBills (DataTable)     Yes                  Yes           Effective. On mismatch: 18 fields mapped directly + RowState re-derived from bcltid. On match: only LastModAt and RowState refreshed. Null RowChkSum treated as 0.
SaveBills (string)        Yes                  Yes           Effective. Dynamic column switch only runs on mismatch or NewRow. Unconditional Update call after guard is an anti-pattern.
SaveAuthBills             No                   N/A           No RowChkSum — fresh object built for every row, then lookup performed. Every found record is always fully updated. New records are silently lost.
________________________________________

13. Scoping / Data Windowing

Source-side scoping:

SaveBills (DataTable):
    CUSTOM year-based WHERE (not standard strWhere):
    year(billDate) >= [year of WorkDate+DaysBack] AND billdate <= [WorkDate+12]
    For normal run (DaysBack=-15): fetches all bills from start of current year to 12 days ahead
    For reload (DaysBack=-728250): fetches all bills from year 1 (all-time)
    Note: Standard strWhere is commented out in BHGTaskRunner for this case

SaveAuthBills:
    Standard strWhere rolling 15-day lookback applied in BHGTaskRunner

SaveBills (string):
    SrcCmd is passed directly by caller — scoping is determined by the caller

Azure-side scoping:

SaveBills (DataTable):
    Year-window: BillDate.Year >= wrkdt.AddDays(DaysBack).Year AND BillDate <= wrkdt+15
    This loads the same year-to-date range as the source query, ensuring pre-load and
    source data are aligned.

SaveBills (string):
    yearly=true:  full site slice
    yearly=false: wrkdt.AddMonths(-1) to wrkdt.AddDays(31) — 2-month rolling window

SaveAuthBills:
    Full site slice — all records for site regardless of date
________________________________________

14. Error Handling

SaveBills (DataTable) has a two-layer error handling strategy:

    Outer catch: catches EF Core exceptions, SaveChanges failures, and context errors.
        rcodes.IsResult = false; rcodes.ExceptMsg = e.Message; ExceptInnerMsg if present.

    Inner catch (around per-field mapping only):
        On any field parsing error, dumps ALL column names and values to Console.WriteLine.
        Exception is SWALLOWED — processing continues for the row with partially mapped data.
        This means a bad row does not abort the site — but the partial update may corrupt
        the Azure record with default/zero values for fields that failed to parse.

SaveBills (string) and SaveAuthBills:
    Single outer catch only — no inner per-field protection.
________________________________________

15. Anomalies, Bugs, and Known Defects

ANOMALY 1 — SaveBills (DataTable): Standard strWhere commented out — year-based WHERE used instead.

File: BHGTaskRunner/Program.cs, line 518
    //strCmd += " Where " + strWhere + " " + st.SortOrder    ← COMMENTED OUT

The standard rolling-date WHERE (DaysBack=-15) is not applied for bills. Instead, a custom
year-based WHERE fetches all bills from the start of the year. This is intentional design
(late payment postings need current-year history) but is undocumented in the task metadata
and means the strWhere/WhereCondition configuration for this task has no effect.

ANOMALY 2 — SaveBills (DataTable and string): BillReason truncation guard is off-by-two.

File: SaveBills.cs, lines 103–104 and 313–314
    if (bill.BillReason.Length > 2500) { bill.BillReason = bill.BillReason.Substring(0, 2498); }

The guard triggers at > 2500 but truncates to 2498 characters. A string of exactly 2500
characters passes through untouched. A string of 2501+ is truncated to 2498. If the target
column is VARCHAR(2500), a 2500-character value would still fit. If VARCHAR(2499), the
guard should be > 2499. The two-character gap (2500 vs 2498) likely indicates the intended
target is 2499 characters, but neither the guard nor the truncation length is consistent.

ANOMALY 3 — SaveBills (DataTable): NewBills list is dead code.

File: SaveBills.cs, lines 141, 165–184
    //NewBills.Add(bill)    ← line 141, COMMENTED OUT

NewBills.Add() is commented out, so NewBills is always empty. The entire block at lines
165–184 (which loops NewBills, queries the database per record, and calls Add or Update)
is dead code. It contains live database queries that will never execute.

ANOMALY 4 — SaveAuthBills: New records never inserted into Azure.

File: SaveBills.cs, lines 617–621
    //newpbs.Add(pb)    ← COMMENTED OUT
    pbills.Add(pb)     ← in-memory list only — no EF context attachment

    //if (newpbs.Count > 0) { db.TblVw3pbill.AddRange(newpbs); db.SaveChanges(); }  ← COMMENTED OUT

New records reported in rcode.RowsIns are never written to the database. The pats.tbl_vw3pbill
table only grows via the update path (existing records found by DsId). Any service record
that does not already exist in Azure will never be inserted. For clinics with new DsId
values not yet in Azure, data is permanently silently dropped.

ANOMALY 5 — SaveBills (string): Unconditional db.Update() called for all rows including new ones.

File: SaveBills.cs, line 369
    db.TblBills.Update(bill)    ← called OUTSIDE the RowChkSum guard, unconditionally

After the guard block, db.TblBills.Update(bill) is called for every row regardless of
whether the RowChkSum guard fired or not. For new rows already staged via db.TblBills.Add(),
calling Update() on the same entity may change EF Core's entity state from Added to Modified,
preventing the INSERT from being generated — effectively blocking new row inserts.

ANOMALY 6 — SaveAuthBills: billunits has no null/empty guard.

File: SaveBills.cs, line 466
    case "billunits":
        pb.BillUnits = double.Parse(dr[c.ColumnName].ToString())

No length check before double.Parse. If any source row has an empty billunits value,
FormatException is thrown and the entire site's SaveAuthBills run fails. All fields parsed
in the same row (and all subsequent rows) are lost.

ANOMALY 7 — SaveAuthBills: SiteCode may be overwritten from source column; PHC check is column-order dependent.

File: SaveBills.cs, lines 413–415 and 552–553
    case "sitecode":
        pb.SiteCode = dr[c.ColumnName].ToString()

If the source view includes a "sitecode" column, pb.SiteCode is set to the source value —
potentially overriding the sc parameter. More critically, the PHC check in the "siteid" case:
    if (pb.SiteCode == "PHC") { pb.SiteId = 105; }
relies on pb.SiteCode already being set. If the DataTable column order places "siteid" before
"sitecode", pb.SiteCode will be empty when the PHC check fires, and the override will never
trigger for the PHC site.

ANOMALY 8 — SaveBills (DataTable): RowTrax block is present but empty.

File: BHGTaskRunner/Program.cs, lines 542–547
    if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
    {
        if (st.RowTrax.Value) { }    ← EMPTY
    }

No RowTrax audit is ever logged for pats.tbl_bills, even if RowTrax is configured as true.

ANOMALY 9 — SaveBills (DataTable): BillOrgdt uses weak length > 0 guard instead of > 7.

File: SaveBills.cs, line 114
    if (r["BillOrgdt"].ToString().Length > 0) { bill.BillOrgdt = DateTime.Parse(r["BillOrgdt"].ToString()); }

All other DateTime fields in this method use length > 7. BillOrgdt uses length > 0 — a
single non-empty character passes the check and DateTime.Parse will throw a FormatException,
caught by the inner try/catch with column dump to Console.

ANOMALY 10 — SaveAuthBills: wrkdt and yearly parameters accepted but unused.

File: SaveBills.cs, line 385
    public RCodes SaveAuthBills(DataTable tbl, string sc, DateTime wrkdt, bool yearly, ...)

Neither wrkdt nor yearly is referenced inside the method. Azure-side scoping always loads
the full site slice regardless of these values. BHGTaskRunner always passes yearly=false.
________________________________________

16. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-INV (Status=17, SiteCode='All')
        |-- insert child tasks per clinic:
        |       pats.tbl_bills           SiteCode='B01A'
        |       pats.tbl_vw3pbill        SiteCode='B01A'
        |       ... (repeated for each active clinic)
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 8
        |
        |-- filter: TaskName='SAMMS-ETL-INV', SiteCode!='PHC', Status=17
        |-- mark parent task Status=18 (running)
        |
        |-- for each child task (one per clinic per task type):
        |
        |   Build strCmd via SelectConstructor (DaysBack=-15)
        |   strCmd = "Select " + strFlds + " from " + SrcSchema + "." + FromTblVw
        |
        |======================================================
        |  BRANCH A: pats.tbl_bills
        |======================================================
        |   Standard strWhere SKIPPED (commented out)
        |   if Reload=true: BillDaysBack = -728250 (all-time)
        |   else: BillDaysBack = -15 (current year)
        |   CUSTOM WHERE: year(billDate) >= [year] AND billdate <= [WorkDate+12]
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   if (SrcDt.Rows.Count == 0): skip — return IsResult=true
        |   → sd.SaveBills(SrcDt, SiteCode, WorkDate, BillDaysBack, null)
        |       Normalize wrkdt to midnight
        |       Pre-load: year-window from pats.tbl_Bills
        |       Pre-pass: set RowState=true → false for all in-window records
        |       db.SaveChanges() ← commits pre-pass BEFORE main loop
        |       Loop rows:
        |           Pre-read billid, bcltid, myrcs
        |           Lookup by SiteCode + BillId
        |           RowChkSum guard:
        |               mismatch/new: map 18 fields (inner try/catch), derive RowState from bcltid
        |               match: update LastModAt + RowState only
        |           New rows: double DB check before Add
        |           Updated rows: db.Update(bill)
        |       db.SaveChanges() ← commits main loop changes
        |       → pats.tbl_Bills (Azure BHG_DR)
        |       → RowTrax block EMPTY — no audit logged
        |
        |======================================================
        |  BRANCH B: pats.tbl_vw3pbill
        |======================================================
        |   strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   → sd.SaveAuthBills(SrcDt, SiteCode, WorkDate, false, null)
        |       Pre-load: full site slice from pats.tbl_vw3pbill
        |       Pre-pass: set RowState=true → false for all active records
        |       Loop rows:
        |           Build fresh TblVw3pbill object via 32-field switch
        |           Derive RowState from DsClt (negative → false)
        |           Lookup by DsId
        |           Found: copy 35 fields to existing entity → RowsUpd++
        |           Not found: add to pbills list only (NOT to db context) → RowsIns++ (false count)
        |       db.SaveChanges() ← commits updates + pre-pass; new inserts silently dropped
        |       → pats.tbl_vw3pbill (Azure BHG_DR) — updates only; new records NEVER inserted
        |       → RowTrax audit if st.RowTrax = true
        |
        |-- mark child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)

        [Azure BHG_DR — final state after run]
        pats.tbl_Bills       — bills upserted (year-window); absent active records soft-deleted
        pats.tbl_vw3pbill    — existing records updated; new records silently lost
________________________________________

17. File Reference Map

File Path                                                     Purpose
---------                                                     -------
BCAppCode/BHG-DR-LIB/SaveBills.cs                            All 3 methods (644 lines)
BCAppCode/BHGTaskRunner/Program.cs                            Schedule 8 dispatch
                                                              pats.tbl_bills       ~line 517
                                                              pats.tbl_vw3pbill    ~line 3067
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                    Builds SELECT column list and RowChkSum expression
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                        ADO.NET wrapper — executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblBills.cs                      EF Model → pats.tbl_Bills
BCAppCode/BHG-DR-LIB/Models/TblVw3pbill.cs                   EF Model → pats.tbl_vw3pbill
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                 EF DbContext — DbSet registrations
BCAppCode/Scheduler/Program.cs                                Task creation for SAMMS-ETL-INV schedule
________________________________________

18. Quick Reference Summary

Method                   Load path     Key                   RowChkSum guard       RowState         Schedule
------                   ---------     ---                   ---------------       --------         --------
SaveBills (DataTable)    EF Core       SiteCode + BillId     Yes (effective)       Yes (bcltid<=0)  8 / 3
SaveBills (string)       EF Core       BillId                Yes (effective)       No               Not in BHGTaskRunner
SaveAuthBills            EF Core       DsId                  None (fresh build)    Yes (DsClt<0)    8 / 3

Source WHERE behaviour:
    pats.tbl_bills:    CUSTOM — year(billDate) >= [year] AND billdate <= [WorkDate+12]
    pats.tbl_vw3pbill: STANDARD — WhereCondition with DaysBack=-15

Critical bugs:
1. SaveAuthBills — new records added to in-memory list only — never inserted into Azure;
   rcode.RowsIns reports false counts
2. SaveBills (string) — unconditional db.Update() after RowChkSum guard may prevent new
   row inserts by overriding EF Core entity state from Added to Modified
3. SaveAuthBills — billunits has no empty guard — FormatException aborts entire site load
4. SaveBills (DataTable) — RowTrax block is empty — no billing record audit logged
5. SaveBills (DataTable) — NewBills dead code block contains live DB queries that never execute
6. SaveBills (DataTable and string) — BillReason truncation is off-by-two (guard > 2500, truncates to 2498)
7. SaveBills (DataTable) — BillOrgdt uses weak length > 0 guard (not > 7) — single-char value throws DateTime.Parse exception, caught by inner catch with full column dump



Here are the metadata tables for both files:

SaveAuths.cs
Method 1 — SaveAuths

Field	Value
Name	SaveAuths
Module	Prior authorization records load
Layer	Target load / EF Core upsert with full pre-pass RowState reset
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr where TaskName = pats.tbl_pbi3payauth
Source table	dbo.tblPayAuth (or clinic view); column list + RowChkSum from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_pbi3payauth
Load type	EF Core upsert — pre-pass sets ALL existing records RowState=false before loop; lookup key: SiteCode + TpaId; RowChkSum guards field updates; on match: only RowState=true + LastModAt written; new rows via per-row db.Add(); single terminal db.SaveChanges()
Load type column	RowChkSum — effective change detection; RowState (bool); no wrkdt parameter — date scoping is source-side only; date fields use .Replace('-','/') normalization; TpServ truncated to 299 chars if > 300 (off-by-one); 5 fields permanently commented out (TpaCompKey, TpaBigKey, ProgGroup, PayerGroup, PayerType)
Frequency	Daily
Schedule	Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV)
Parent	SAMMS-ETL-INV
Downstream	pats.tbl_pbi3payauth → insurance billing authorization validation; claim submission pre-checks
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAuths(SrcDt, st.SiteCode, null)
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveAuths.cs
Status	Active
Folder	BHG-DR-LIB\SaveAuths.cs; detail in SaveAuths-Documentation\SaveAuths_ETL_Complete_Documentation.md
Known anomalies	5 fields permanently commented out (never mapped to Azure); TpServ truncation off-by-one (> 300 check, truncates to 299); tpeffdate missing dash-replace unlike tpaeffdate; RowTrax block present but empty — no audit logged
Method 2 — SaveAuthBillsub

Field	Value
Name	SaveAuthBillsub
Module	Billing submission view load — EF Core path (bypassed in production)
Layer	Target load / EF Core upsert — bypassed; BulkDartsSrvLoader used instead
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr where TaskName = pats.tbl_vw3pbillsub
Source table	dbo.vw3pBillSub (or clinic view)
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_vw3pBillSub (EF Core path — bypassed); stg.tbl_vw3pbillsub → pats.tbl_vw3pBillSub (bulk path — active)
Load type	BYPASSED — sd.SaveAuthBillsub(...) commented out in BHGTaskRunner; BulkDartsSrvLoader → stg.tbl_vw3pbillsub → stored procedure MERGE is the active path; EF Core method has 35-field switch, complex 6-component lookup key (SiteCode+DsId+PyPayerid+PySubsid+PyGroup+CptMod+Charge), mid-loop db.SaveChanges() on not-found path
Load type column	No RowChkSum guard (fresh object built per row); RowState (bool); pre-pass resets all to RowState=false; WrkDate and Reload parameters accepted but unused
Frequency	Daily (bulk path active)
Schedule	Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV)
Parent	SAMMS-ETL-INV
Downstream	pats.tbl_vw3pBillSub → pre-billing staging; insurance claim generation
Connection / method	Active: bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_vw3pbillsub", ...). Bypassed: sd.SaveAuthBillsub(...) commented out
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via bulk/staging
Owner	BHGTaskRunner / BHG-DR-LIB\SaveAuths.cs
Status	EF Core method bypassed — dead code in production
Folder	BHG-DR-LIB\SaveAuths.cs; detail in SaveAuths-Documentation\SaveAuths_ETL_Complete_Documentation.md
Known anomalies	EF Core method bypassed — dead code; stale record replacement path broken (SaveChanges + BSubsNew.Add both commented out); mid-loop db.SaveChanges() anti-pattern; WrkDate and Reload parameters unused
SaveBills.cs
Method 1 — SaveBills (DataTable overload)

Field	Value
Name	SaveBills (DataTable)
Module	Billing transaction load — primary daily path
Layer	Target load / EF Core upsert with partial pre-pass RowState reset
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr where TaskName = pats.tbl_bills
Source table	dbo.tblBills (or clinic view); column list + RowChkSum from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_Bills
Load type	EF Core upsert — year-window Azure pre-load; partial pre-pass resets RowState=true → false then commits before main loop; RowChkSum guards field updates; 18-field direct assignment (not switch); inner try/catch dumps all columns on parse error; double DB check before new row Add; BillCltid <= 0 → RowState=false; PHC BillSiteId=105
Load type column	RowChkSum — effective; RowState (bool) derived from BillCltid; BillReason truncated to 2498 if > 2500 (off-by-two); BillOrgdt weak guard (length > 0); standard strWhere commented out — year-based custom WHERE used instead
Frequency	Daily
Schedule	Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV)
Parent	SAMMS-ETL-INV
Downstream	pats.tbl_Bills → financial audit trail; FIFO payment allocation; billing reports
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveBills(SrcDt, st.SiteCode, WorkDate, BillDaysBack, null)
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveBills.cs
Status	Active
Folder	BHG-DR-LIB\SaveBills.cs; detail in SaveBills-Documentation\SaveBills_ETL_Complete_Documentation.md
Known anomalies	RowTrax block empty — no audit logged; NewBills list dead code (Add commented out, block with live DB queries never runs); BillOrgdt weak guard (> 0 not > 7); BillReason truncation off-by-two; AllNewRows permanently disabled (commented out)
Method 2 — SaveBills (string overload)

Field	Value
Name	SaveBills (string)
Module	Billing transaction load — legacy standalone path
Layer	Target load / EF Core upsert with parallel async data fetch
Source system	SAMMS (SQL Server — caller-supplied connection)
Source DB	Caller-supplied SrcCon
Source table	Caller-supplied via SrcCmd
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_Bills
Load type	EF Core upsert — parallel Task.Run for source fetch and Azure pre-load; yearly flag functional (true=full site, false=2-month window); RowChkSum-guarded dynamic column switch; unconditional db.Update() called after guard for ALL rows including new ones (anti-pattern); single terminal db.SaveChanges()
Load type column	RowChkSum — effective; no RowState logic; no PHC handling; BillReason same 2500/2498 truncation; AllNewRows dead code (.ToList() never null)
Frequency	Not in BHGTaskRunner — legacy / manual / test use only
Schedule	Not scheduled — standalone call
Parent	N/A
Downstream	pats.tbl_Bills — same target as primary overload
Connection / method	Self-contained — creates own BHG_DRContext and SQLSvrManager internally
Server / DB / API	Source: caller-supplied SrcCon. Target: Azure BHG_DR via internal BHG_DRContext
Owner	BHG-DR-LIB\SaveBills.cs
Status	Legacy — not in active daily pipeline
Folder	BHG-DR-LIB\SaveBills.cs; detail in SaveBills-Documentation\SaveBills_ETL_Complete_Documentation.md
Known anomalies	Unconditional db.Update() after RowChkSum guard may prevent inserts by overriding EF Core entity state Added → Modified; AllNewRows dead code; no RowState; no PHC handling
Method 3 — SaveAuthBills

Field	Value
Name	SaveAuthBills
Module	Insurance billing view load
Layer	Target load / EF Core upsert (updates only — new inserts broken)
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr where TaskName = pats.tbl_vw3pbill
Source table	dbo.vw3pBill (or clinic view); column list from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_vw3pbill
Load type	EF Core upsert — full pre-pass RowState=false reset; fresh object built per row via 32-field switch; lookup by DsId; found: 35 fields copied to existing entity; not found: added to in-memory list only — never inserted to DB; single terminal db.SaveChanges(); PHC SiteId=105; RowState derived from DsClt < 0
Load type column	No RowChkSum; RowState (bool) from DsClt; wrkdt and yearly parameters unused; billunits has no null guard (FormatException aborts site); SiteCode overwritten from source column (column-order risk for PHC check)
Frequency	Daily
Schedule	Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV)
Parent	SAMMS-ETL-INV
Downstream	pats.tbl_vw3pbill → insurance billing reports and dashboards
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAuthBills(SrcDt, st.SiteCode, WorkDate, false, null)
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveBills.cs
Status	Active (updates only — new records silently dropped)
Folder	BHG-DR-LIB\SaveBills.cs; detail in SaveBills-Documentation\SaveBills_ETL_Complete_Documentation.md
Known anomalies	New records never inserted — newpbs.Add(pb) and AddRange both commented out; rcode.RowsIns reports false counts; billunits no empty guard — FormatException aborts entire site; SiteCode source override may break PHC SiteId=105 check depending on column order; wrkdt and yearly unused
