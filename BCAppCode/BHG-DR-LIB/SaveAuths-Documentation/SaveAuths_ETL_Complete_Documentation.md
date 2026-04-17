
Authorization / Bill Submission ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 8 — SAMMS-ETL-INV (primary) / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract prior authorization
records and billing submission records from local SAMMS SQL Server databases at each clinic
and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What prior authorization and billing submission data are and why they exist
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and BHGTaskRunner dispatches them under Schedule 8
- How both methods in SaveAuths.cs work in detail
- The full pre-pass RowState reset pattern used by both methods
- How RowChkSum change detection operates in SaveAuths
- The production bypass of SaveAuthBillsub — replaced by BulkDartsSrvLoader
- The complex lookup key and mid-loop SaveChanges call in SaveAuthBillsub
- All known anomalies, bugs, and dead code

There are two methods in SaveAuths.cs spanning two Azure destination tables:

pats.tbl_pbi3payauth:   SaveAuths         (active in production — EF Core upsert)
pats.tbl_vw3pBillSub:   SaveAuthBillsub   (bypassed in production — BulkDartsSrvLoader used)
________________________________________

2. High-Level Business Summary

What is authorization (Auth) data?

Prior authorization (PA) is a requirement imposed by insurance payers before certain services
can be billed. In SAMMS-based treatment programs, authorization records link a patient to an
insurance payer and capture the payer's approval details — authorization code, effective and
termination dates, service type, units approved, request and response form references, and
a service description. These records are essential for verifying that a billed service was
pre-approved by the payer before submission.

pats.tbl_pbi3payauth — Prior Authorization Records
This table captures one row per authorization record per patient per payer per site. It records
the authorization ID (TpaId), the patient ID (TpaCltid), the payer name (TpaPayer), the
authorization description (TpaDesc), the effective date from two possible source columns
(TpEffdate / TpaEffDate), the termination date (TpaTermDate), the staff member who obtained
the authorization (TpaStaff), the authorization date (Tpadt), the authorization code
(TpaAuthCode), the file paths to the request and confirmation forms (TpAuthpath, TpConfirmpath),
the failure reason (TpFail), the request and response form references (TpRequestForm,
TpResponseForm), the approved service description (TpServ — truncated to 299 characters if
longer), the term date (TpTermDate), authorized units (TpUnits), the approved service list
(TpServapproved), a note (TpNote), and the authorization type (TpType). RowState tracks
whether the record is currently active — set to true for records present in the latest source
pull, false for records not returned (soft-delete via pre-pass reset).

What is bill submission (BillSub) data?

The billing submission view (`pats.tbl_vw3pBillSub`) holds a flattened, ready-to-submit
billing record joining DartsSrv service data with payer, patient demographic, and claim
detail fields. It is used as a pre-billing staging area — capturing the CPT code, modifier,
payer subscriber and group details, charge amount, service date range, NPI, diagnosis, and
patient demographics — effectively all fields required to build a CMS-1500 or EDI 837 claim.
This method was originally an EF Core path; production now routes to BulkDartsSrvLoader.

Why this data is important
- Authorization records are the gatekeeper for insurance billing — billing without a valid
  PA code results in claim denial
- The RowState pre-pass reset ensures that authorization records not present in the latest
  SAMMS pull are deactivated in Azure, preventing submission of expired or revoked auths
- The BillSub table supports reporting and BI dashboards that require a denormalized view
  of service + payer + demographic data
- The bulk path for BillSub delivers significantly better throughput than the EF Core path
  for the volume of billing records processed daily

Load type
SaveAuths uses an EF Core upsert with a full pre-pass RowState reset, RowChkSum-guarded
field updates, and a single terminal SaveChanges. SaveAuthBillsub is an EF Core method with
a complex composite lookup key and mid-loop SaveChanges calls, but it is bypassed in production
— BulkDartsSrvLoader handles `pats.tbl_vw3pbillsub` tasks via staging table and stored
procedure merge.
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
BulkDartsSvc.cs / BulkDartsSrvLoader       Production path for pats.tbl_vw3pbillsub (bypasses SaveAuthBillsub)
SaveAuths.cs (BHG-DR-LIB)                  2 methods — SaveAuths (active), SaveAuthBillsub (bypassed)
Models/TblPbi3Payauth.cs                    EF entity → pats.tbl_pbi3payauth
Models/Tblvw3pBillSub.cs                    EF entity → pats.tbl_vw3pBillSub (bypassed)
pats.tbl_pbi3payauth (Azure BHG_DR)        Final destination for prior authorization records
pats.tbl_vw3pBillSub (Azure BHG_DR)       Final destination for billing submission records (bulk path)
stg.tbl_vw3pbillsub (Azure BHG_DR)        Staging table for BulkDartsSrvLoader (BillSub path)
tsk.tbl_RowTrax (Azure DB)                 Audit log — source vs destination row counts
________________________________________

4. Scheduler — How Authorization Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task for SAMMS-ETL-INV schedule
    TaskName = 'SAMMS-ETL-INV'
    SiteCode = 'All'
    Status   = 17

Step 3 — Insert child tasks per clinic
For each active clinic, child tasks are inserted:
    TaskName = 'pats.tbl_pbi3payauth'    SiteCode = 'B01A'
    TaskName = 'pats.tbl_vw3pbillsub'   SiteCode = 'B01A'
    ... (one row per table per clinic)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How Authorization Tasks Are Orchestrated

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

CASE: pats.tbl_pbi3payauth
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveAuths(SrcDt, st.SiteCode, null)
    rCodes.RowsProcessed = SrcDt.Rows.Count
    RowTrax block: present but EMPTY — no RowTrax logging fires for this task
    Note: SaveAuths has no wrkdt parameter — date scoping is handled entirely
    at source via the WHERE applied here before GetTableData.

CASE: pats.tbl_vw3pbillsub
    SELECT modifications before fetch:
        strCmd = strCmd.Replace("Select ", "Select distinct ")
        strCmd = strCmd.Replace("[CptMod] CptMod",    "isnull([CptMod], ':(') CptMod")
        strCmd = strCmd.Replace("[pySUBSID] pySUBSID", "isnull([pySUBSID], ':(') pySUBSID")
        strCmd = strCmd.Replace("[charge] charge",     "isnull(charge, 0) charge")
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    → rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_vw3pbillsub", SiteCode, WorkDate.AddDays(DaysBack), db)
    → sd.SaveAuthBillsub(...) IS COMMENTED OUT — BulkDartsSrvLoader used instead
    RowTrax audit: fires if st.RowTrax = true

Step 6 — RowTrax audit (pats.tbl_vw3pbillsub only — pats.tbl_pbi3payauth block is empty)

Step 7 — Mark child task Status=20 (complete)

Schedule 3 note:
BHGTaskRunner.exe 3 is the catch-all schedule. Its task filter includes ALL tasks that are
not P1, P2, or SAMMSGlobal — this includes SAMMS-ETL-INV child tasks. So if these task names
appear in the queue when arg=3 is run, they will also be processed by the same inner switch.
In practice, the dedicated arg=8 run handles SAMMS-ETL-INV tasks first.
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the full SELECT, and returns a DataTable. Connection strings are from
ctrl.tbl_LocationCons. Typical SAMMS source names:
    dbo.tblPayAuth (or clinic view)    → pats.tbl_pbi3payauth
    dbo.vw3pBillSub (or clinic view)   → pats.tbl_vw3pBillSub (bulk path)

For pats.tbl_vw3pbillsub, BHGTaskRunner also applies four SELECT-level transformations
(distinct, isnull wrapping for CptMod, pySUBSID, charge) before calling GetTableData.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

7a. dbo.tblPayAuth — Prior Authorization Records

Key columns:
    tpaid             int        Unique authorization record ID (primary key)
    tpacltid          int        Patient/client ID
    RowChkSum         int        CHECKSUM() computed over key authorization fields
    tpaPayer          varchar    Payer company name
    tpadesc           varchar    Authorization description
    tpeffdate         datetime   Effective date (alternate field — no 'a' prefix)
    tpaeffdate        datetime   Effective date (standard field — with 'a' prefix)
    tpatermdate       datetime   Termination/expiry date
    tpastaff          varchar    Staff member who obtained the authorization
    tpadt             datetime   Authorization date and time
    tpaauthcode       varchar    Authorization code from payer
    tpauthpath        varchar    File path to authorization request document
    tpconfirmpath     varchar    File path to authorization confirmation document
    tpfail            varchar    Failure reason (if auth was rejected)
    tprequestform     varchar    Request form reference
    tpresponseform    varchar    Response form reference
    tpserv            varchar    Approved service description (may exceed 300 chars — truncated)
    tptermdate        datetime   Service termination date (separate from tpatermdate)
    tpunits           int        Number of authorized units
    tpservapproved    varchar    Approved services list
    tpnote            varchar    Authorization note
    tptype            varchar    Authorization type code
    tpacompkey        varchar    (NOT MAPPED — commented out)
    tpabigkey         varchar    (NOT MAPPED — commented out)
    proggroup         varchar    (NOT MAPPED — commented out)
    payergroup        varchar    (NOT MAPPED — commented out)
    payertype         varchar    (NOT MAPPED — commented out)

7b. dbo.vw3pBillSub — Billing Submission View (flattened)

Key columns (35 mapped fields):
    dsid              int        DartsSrv service record ID
    dsclt             int        Patient/client ID
    descript          varchar    Service description
    billdatecriteria  datetime   Billing date criteria
    paydefaultsubmit  varchar    Default submission payer
    scruberror        varchar    Scrub error message
    dstxtsrv          varchar    Service text
    dsdtstart         datetime   Service start date
    dsdtend           datetime   Service end date
    dstxttype         varchar    Service type text
    dsdblunits        double     Service units (double)
    billunits         double     Billed units
    dstxtstaff        varchar    Staff member text
    npi               varchar    Provider NPI number
    dsbilled          datetime   Date submitted/billed
    pypayerid         varchar    Payer ID
    pysubsid          varchar    Subscriber ID
    pygroup           varchar    Group number
    cptcode           varchar    CPT procedure code; also builds CptMod = cptcode:modifier
    modifier          varchar    CPT modifier
    charge            double     Charge amount (isnull → 0 if null)
    tpaauthcode       varchar    Authorization code
    clientname        varchar    Patient name
    cltdob            datetime   Patient date of birth
    cltgender         varchar    Patient gender
    cltadd1           varchar    Patient address
    cltcity           varchar    Patient city
    cltState          varchar    Patient state
    cltzip            varchar    Patient ZIP
    cltphone          varchar    Patient phone
    cltmarry          varchar    Patient marital status
    cltm4id           varchar    Patient Medicaid ID
    dsdiag            varchar    Diagnosis code
    dspos             varchar    Place of service
    ndc               varchar    NDC drug code
    mg                double     Medication mg amount
    siteid            int        Site numeric ID
    dsarea            varchar    Service area
    payclass          varchar    Payer class
    rowchksum         int        CHECKSUM()
    CptMod            varchar    Computed: cptcode + ':' + modifier (isnull-wrapped)
    pySUBSID          varchar    Subscriber ID (isnull-wrapped in SELECT)
________________________________________

8. SaveAuths — Prior Authorization Load (pats.tbl_pbi3payauth)

Source: dbo.tblPayAuth (or clinic-specific view)
Destination: pats.tbl_pbi3payauth
Key: SiteCode + TpaId
Parameters: tbl, sc, db
Note: No wrkdt parameter — date scoping is handled at source level only.

Azure pre-load:
    auths = db.Pbi3Payauths.Where(x => x.SiteCode == sc).ToList()
Full site slice — all existing authorization records for the clinic.

Pre-pass RowState reset:
    if (auths.Count == 0) { AllNewRows = true; }
    else
    {
        foreach (TblPbi3Payauth a in auths) { a.RowState = false; }
    }
Before any source row is processed, every existing Azure record for the site is set to
RowState=false. This is a full pre-pass soft-delete: records that are NOT present in the
source pull will remain RowState=false after the loop (effectively deactivated). Records
that ARE present get their RowState set back to true during field mapping.

AllNewRows flag: If auths.Count == 0, all incoming rows go straight to new record creation
without a lookup — valid for first-load scenarios.

Lookup key: TpaId (single key — not composite)
    auth = auths.Where(x => x.TpaId == inttpaID).FirstOrDefault()
No SiteCode filter in the lookup since the pre-loaded list is already site-scoped.

RowChkSum guard:
    if (NewRow || (rcs != auth.RowChkSum))
    {
        // full field mapping + re-activate
        auth.RowState = true
        auth.LastModAt = RunDT
        auth.RowChkSum = rcs
        ... (all fields mapped)
        res.RowsUpd += 1
    }
    else
    {
        // checksum unchanged — re-activate only
        auth.RowState = true
        auth.LastModAt = RunDT
    }
On a RowChkSum match: only RowState and LastModAt are written — no field updates. This
correctly preserves unchanged data while ensuring the record is re-activated after the
pre-pass reset. On a RowChkSum mismatch: all fields are updated and the record is re-activated.

Date normalization:
All DateTime fields use .Replace('-', '/') before DateTime.Parse:
    auth.TpaEffDate  = DateTime.Parse(r["tpaeffdate"].ToString().Replace('-', '/'))
    auth.TpaTermDate = DateTime.Parse(r["tpatermdate"].ToString().Replace('-', '/'))
    auth.Tpadt       = DateTime.Parse(r["tpadt"].ToString().Replace('-', '/'))
    auth.TpTermDate  = DateTime.Parse(r["tptermdate"].ToString().Replace('-', '/'))
This normalizes ISO-format date strings (YYYY-MM-DD) that DateTime.Parse may otherwise
reject on some .NET locale configurations.

TpEffdate vs TpaEffDate — two separate effective date fields:
    case "tpeffdate":  → auth.TpEffdate (without 'a') — length > 6, no dash replacement
    case "tpaeffdate": → auth.TpaEffDate (with 'a') — length > 6, with dash replacement
Both map to different EF properties. They represent the same conceptual date stored in two
separate columns in different SAMMS schema versions.

TpServ truncation guard:
    auth.TpServ = r["tpserv"].ToString().Trim()
    if (auth.TpServ.Length > 300) { auth.TpServ = auth.TpServ.Substring(0, 299); }
The service description is capped at 299 characters. The guard condition checks > 300 but
Substring uses index 0 to 299 (299 characters), leaving a 1-character gap — any string of
exactly 300 characters is not truncated, but a string of 301+ is truncated to 299.

New record add pattern:
    if (NewRow || AllNewRows)
    {
        NewRow = false
        db.Pbi3Payauths.Add(auth)    ← per-row Add (not AddRange)
        //db.SaveChanges()           ← was originally per-row commit — now commented out
    }
New rows are added to the context per-row via Add(), but SaveChanges() is only called once
at the end of the full loop. The commented-out SaveChanges() shows this was originally a
per-row commit pattern that was optimised to a single terminal commit.

Terminal commit:
    db.SaveChanges()
One SaveChanges() covers all updates (to tracked entities via the pre-pass and field
assignments) and all new inserts (via Add()). This is a single-phase commit — all changes
are committed together.

Note: Unlike most other BHG-DR-LIB methods, SaveAuths does NOT use a two-phase commit
(updates first, then AddRange for new rows). All changes are in one SaveChanges call.

Column mapping (SaveAuths — 22 fields, direct assignment not switch-based):

    Source column       EF property       Type / notes
    ---------           -----------       -----
    (pre-read)          TpaId             int — r["tpaid"]
    (pre-read)          RowChkSum         int — r["rowchksum"]
    (always)            RowState          bool — true (re-activated by guard block)
    (always)            LastModAt         DateTime — RunDT
    (always)            RowChkSum         int — rcs (set in guard block)
    tpacltid            TpaCltid          int — direct parse
    tpaPayer            TpaPayer          string
    tpadesc             TpaDesc           string
    tpeffdate           TpEffdate         DateTime? — length > 6 guard; NO dash replace
    tpaeffdate          TpaEffDate        DateTime? — length > 6 guard; dash replace
    tpatermdate         TpaTermDate       DateTime? — length > 6 guard; dash replace
    tpastaff            TpaStaff          string
    tpadt               Tpadt             DateTime? — length > 6 guard; dash replace
    tpaauthcode         TpaAuthCode       string
    tpauthpath          TpAuthpath        string
    tpconfirmpath       TpConfirmpath     string
    tpfail              TpFail            string
    tprequestform       TpRequestForm     string
    tpresponseform      TpResponseForm    string
    tpserv              TpServ            string — Trim() + truncate to 299 if > 300
    tptermdate          TpTermDate        DateTime? — length > 6 guard; dash replace
    tpunits             TpUnits           int? — length > 0 guard
    tpservapproved      TpServapproved    string — Trim()
    tpnote              TpNote            string
    tptype              TpType            string
    tpacompkey          (NOT MAPPED)      commented out
    tpabigkey           (NOT MAPPED)      commented out
    proggroup           (NOT MAPPED)      commented out
    payergroup          (NOT MAPPED)      commented out
    payertype           (NOT MAPPED)      commented out
________________________________________

9. SaveAuthBillsub — Bill Submission Load (pats.tbl_vw3pBillSub)

Source: dbo.vw3pBillSub (or clinic-specific view)
Destination: pats.tbl_vw3pBillSub
Parameters: tbl, sc, WrkDate, Reload, db

PRODUCTION STATUS: This method is NOT called in production. The BHGTaskRunner case for
pats.tbl_vw3pbillsub has the SaveAuthBillsub call commented out:

    case "pats.tbl_vw3pbillsub":
        // rCodes = sd.SaveAuthBillsub(SrcDt, st.SiteCode, st.WorkDate.Value.Date, false, null);   ← COMMENTED OUT
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_vw3pbillsub", ...)                       ← ACTIVE

BulkDartsSrvLoader uses SqlBulkCopy to push data into stg.tbl_vw3pbillsub, from which a
stored procedure merges the data into the final pats.tbl_vw3pBillSub destination table.
SaveAuthBillsub remains in the codebase but is dead code from a production standpoint.

The EF Core method logic (if it were called):

Azure pre-load:
    BSubs = db.Tblvw3pBillSub.Where(x => x.SiteCode == sc).ToList()
Full site slice loaded.

Pre-pass RowState reset:
    foreach (var r in BSubs) { r.RowState = false; }
    //db.SaveChanges();    ← COMMENTED OUT — reset not committed until end
All existing records are set to RowState=false before the loop. Unlike SaveAuths, the
SaveChanges after the pre-pass is commented out — the reset is only committed at the end.

Per-row construction:
For each source row, a NEW Tblvw3pBillSub object is created and populated via a 35-field
column switch. The object is NOT looked up first — it is built fresh, then looked up.

Complex composite lookup key:
    dbs = BSubs.FirstOrDefault(x =>
        x.SiteCode == bs.SiteCode
        && x.DsId == bs.DsId
        && x.PyPayerid == bs.PyPayerid
        && x.PySubsid == bs.PySubsid
        && x.PyGroup.Trim() == bs.PyGroup.Trim()
        && x.CptMod == bs.CptMod
        && x.Charge == bs.Charge)
Six-component key: SiteCode + DsId + PyPayerid + PySubsid + PyGroup + CptMod + Charge.
Note: Charge (a double) is part of the lookup key — floating-point equality comparison.

Not-found path (dbs == null):
    db.SaveChanges()    ← MID-LOOP SaveChanges (commits all pending changes up to this point)
    dbsx = BSubs.Where(x => SiteCode + DsId + DsClt + Modifier + RowState==false).ToList()
    if (dbsx.Count == 0)
    {
        BSubsNew.Add(bs)
        res.RowsIns += 1
    }
    else
    {
        try
        {
            foreach (var x in dbsx) { db.Tblvw3pBillSub.Remove(x); }
            //db.SaveChanges()    ← COMMENTED OUT
        }
        catch (Exception e)
        {
            // all logging commented out — silent failure
        }
        //BSubsNew.Add(bs)        ← COMMENTED OUT — new record NOT added after remove
        //res.RowsIns += 1        ← COMMENTED OUT
    }
When the primary key lookup fails, a mid-loop SaveChanges is called, then a secondary lookup
by (SiteCode + DsId + DsClt + Modifier + RowState=false) is used to find stale records to
remove. If stale records are found, they are removed via db.Remove() but the SaveChanges
is commented out and the replacement record is also commented out — so the remove is staged
but never committed, and no new record takes its place (see Anomaly 5).

Found path (dbs != null):
    res.RowsUpd += 1
    dbs.Billdatecriteria = bs.Billdatecriteria
    dbs.BillUnits = bs.BillUnits
    //dbs.Charge = bs.Charge       ← NOT updated (part of lookup key)
    ... (30 more fields copied from bs to dbs)
    //dbs.PyGroup = bs.PyGroup     ← NOT updated (part of lookup key)
    //dbs.PyPayerid = bs.PyPayerid ← NOT updated (part of lookup key)
    //dbs.PySubsid = bs.PySubsid   ← NOT updated (part of lookup key)
    dbs.RowState = true
    //db.SaveChanges()             ← COMMENTED OUT (per-row commit removed)
Charge, PyGroup, PyPayerid, and PySubsid are excluded from update (they form part of the
lookup key). All other fields are updated. RowState is forced to true.

Terminal commit:
    db.SaveChanges()
    if (BSubsNew.Count > 0) { db.Tblvw3pBillSub.AddRange(BSubsNew); db.SaveChanges(); }

WrkDate and Reload parameters: Accepted but never used inside the method.
________________________________________

10. Shared EF Core Patterns

BHG_DRContext:
    if (db == null) { db = new Models.BHG_DRContext(); }
Both methods accept an optional db context. If null, a new context is instantiated.

RCodes return:
    res.IsResult = true initially
    res.RowsProcessed = tbl.Rows.Count (SaveAuthBillsub only — SaveAuths does not set this)
    res.RowsIns incremented on each new record
    res.RowsUpd incremented on each updated record (SaveAuths also increments on change-detected rows)
On exception: res.IsResult = false, res.ExceptMsg populated, InnerException captured if present.

Error handling (both methods):
    catch (Exception e)
    {
        res.IsResult = false
        res.ExceptMsg = e.Message
        Console.WriteLine(e.Message)
        if (e.InnerException != null)
        {
            Console.WriteLine(e.InnerException.Message)
            res.ExceptInnerMsg = e.InnerException.Message
        }
    }
Standard BHG-DR-LIB pattern — InnerException stored in ExceptInnerMsg (correct).
________________________________________

11. Change Detection — RowChkSum Behaviour

Method            RowChkSum present    Guard used    Effective behaviour
------            -----------------    ----------    -------------------
SaveAuths         Yes                  Yes           Effective change detection. On match: only RowState + LastModAt updated. On mismatch: all 22 fields mapped. Pre-pass ensures absent records are soft-deleted.
SaveAuthBillsub   Yes (stored)         No            No RowChkSum guard — the column switch builds a fresh object for every row regardless. Bypassed in production.
________________________________________

12. Scoping / Data Windowing

Source-side scoping:
    SaveAuths:       strCmd += " Where " + strWhere — rolling 15-day lookback (@WorkDate DaysBack=-15)
    SaveAuthBillsub: Same rolling WHERE + "Select distinct" + isnull() wrapping applied by BHGTaskRunner

Azure-side scoping:
    SaveAuths:       Full site slice — all authorization records for the site
    SaveAuthBillsub: Full site slice — all bill submission records for the site

Note: SaveAuths has no wrkdt parameter. The rolling date window only affects which SOURCE
records are fetched — Azure always loads the full site slice. This means SaveAuths compares
all existing Azure records against a 15-day source window. Records outside the 15-day window
that previously existed in Azure remain RowState=false after the run (soft-deleted from the
daily perspective).
________________________________________

13. Error Handling

SaveAuths:
    Standard outer try/catch.
    res.IsResult = false on exception.
    ExceptMsg = e.Message.
    ExceptInnerMsg = e.InnerException.Message (if present).
    No inner try/catch for individual row or field processing.

SaveAuthBillsub:
    Outer try/catch follows the same pattern.
    Additionally contains an INNER try/catch around the Remove loop:
        catch (Exception e)
        {
            // Console.WriteLine(...) ← COMMENTED OUT
            // _ = sm.ExeSqlCmd(...)  ← COMMENTED OUT (direct SQL delete fallback)
        }
    The inner catch silently swallows any exception during the Remove loop — nothing is
    logged or rethrown. The direct SQL delete fallback (sm.ExeSqlCmd) is also commented out.
    A Remove failure is completely invisible.
________________________________________

14. Anomalies, Bugs, and Known Defects

ANOMALY 1 — SaveAuths: Five fields permanently commented out of mapping.

File: SaveAuths.cs, lines 101–105
    //auth.TpaCompKey = r["tpacompkey"].ToString();
    //auth.TpaBigKey  = r["tpabigkey"].ToString();
    //auth.ProgGroup  = r["proggroup"].ToString();
    //auth.PayerGroup = r["payergroup"].ToString();
    //auth.PayerType  = r["payertype"].ToString();

These five fields exist in the source SAMMS schema and in the EF model but are never mapped.
Their Azure values are NULL for all records on all runs. If these fields are needed for
reporting or downstream processing, the mapping must be restored.

ANOMALY 2 — SaveAuths: TpServ truncation guard uses inconsistent lengths.

File: SaveAuths.cs, lines 90–93
    auth.TpServ = auth.TpServ.Substring(0, 299)   ← triggered when length > 300

The check is `> 300` but the Substring length is 299. A string of exactly 300 characters
passes the check untouched. A string of 301 characters is truncated to 299. The target
column's max length is presumably 300 — a 301-character value would still fail a column
constraint if the column is CHAR(300). The off-by-one should be `> 299` or Substring to 300.

ANOMALY 3 — SaveAuths: RowTrax block is present but empty.

File: BHGTaskRunner/Program.cs, lines 1334–1339
    if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
    {
        if (st.RowTrax.Value)
        {
            // EMPTY — no SaveRowTrax call
        }
    }

The RowTrax audit block exists in the case statement but contains no SaveRowTrax call.
No source-vs-destination row count audit is ever logged for pats.tbl_pbi3payauth, even
if RowTrax is configured as true for the task.

ANOMALY 4 — SaveAuths: TpEffdate and TpaEffDate — different date parsing for same conceptual field.

File: SaveAuths.cs, lines 68–73
    case "tpeffdate":  DateTime.Parse(r["tpeffdate"].ToString())                              ← no dash replace
    case "tpaeffdate": DateTime.Parse(r["tpaeffdate"].ToString().Replace('-', '/'))           ← dash replace

The two effective date fields use different normalization. If the source column named
"tpeffdate" contains a dash-separated ISO date (YYYY-MM-DD), it will fail to parse on
locale configurations where '-' is not a valid date separator. Only the 'tpaeffdate'
column gets the dash-to-slash replacement.

ANOMALY 5 — SaveAuthBillsub: Record replacement path is partially commented out — stale records removed but replacement not added.

File: SaveAuths.cs, lines 341–356
    foreach (var x in dbsx) { db.Tblvw3pBillSub.Remove(x); }
    //db.SaveChanges()        ← COMMENTED OUT
    //BSubsNew.Add(bs)        ← COMMENTED OUT
    //res.RowsIns += 1        ← COMMENTED OUT

When a new record cannot match the primary key but matches a stale (RowState=false) record
by the secondary key (DsId + DsClt + Modifier), the stale records are staged for removal
but the replacement record is never added and SaveChanges for the removal is also commented
out. The net effect is neither the old record is deleted nor the new one is inserted — the
incoming data is silently discarded. Since this method is bypassed in production, the
practical impact is zero, but the replacement logic is broken.

ANOMALY 6 — SaveAuthBillsub: Mid-loop db.SaveChanges() on not-found path.

File: SaveAuths.cs, line 330
    db.SaveChanges()   ← called inside the foreach loop when primary lookup fails

A SaveChanges() inside a processing loop is a performance anti-pattern — it creates a
separate database round-trip for every row that fails the primary lookup. For clinics with
many non-matching rows, this could cause significant latency. Since this method is bypassed
in production, there is no current impact.

ANOMALY 7 — SaveAuthBillsub: WrkDate and Reload parameters accepted but unused.

File: SaveAuths.cs, line 136
    public RCodes SaveAuthBillsub(DataTable tbl, string sc, DateTime WrkDate, bool Reload, ...)

Neither WrkDate nor Reload is referenced anywhere inside the method body. They were likely
intended for Azure-side scoping (e.g., a rolling date window for pre-load) but were never
implemented.

ANOMALY 8 — SaveAuthBillsub: EF Core method bypassed in production — BulkDartsSrvLoader used instead.

BHGTaskRunner case "pats.tbl_vw3pbillsub" routes to BulkDartsSrvLoader. SaveAuthBillsub
is dead code in production. Any fix or enhancement to SaveAuthBillsub in this file will
have no effect until the BHGTaskRunner dispatch is updated to call it.
________________________________________

15. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-ETL-INV (Status=17, SiteCode='All')
        |-- insert child tasks per clinic:
        |       pats.tbl_pbi3payauth    SiteCode='B01A'
        |       pats.tbl_vw3pbillsub    SiteCode='B01A'
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
        |  BRANCH A: pats.tbl_pbi3payauth
        |======================================================
        |   strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   → sd.SaveAuths(SrcDt, SiteCode, null)
        |       Load full site slice from pats.tbl_pbi3payauth
        |       Pre-pass: set ALL existing records RowState=false
        |       Loop source rows:
        |           if AllNewRows: build new record directly
        |           else: lookup by TpaId
        |               if not found: build new record
        |               if found and RowChkSum changed: map all 22 fields, set RowState=true
        |               if found and RowChkSum same: set RowState=true + LastModAt only
        |       db.SaveChanges() — single commit (updates + new adds)
        |       → pats.tbl_pbi3payauth (Azure BHG_DR)
        |       → Records absent from source remain RowState=false (soft-deleted)
        |       → RowTrax block present but EMPTY — no audit logged
        |
        |======================================================
        |  BRANCH B: pats.tbl_vw3pbillsub
        |======================================================
        |   SELECT modifications: distinct + isnull(CptMod) + isnull(pySUBSID) + isnull(charge,0)
        |   strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   → sd.SaveAuthBillsub(...) IS COMMENTED OUT
        |   → bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_vw3pbillsub", SiteCode, WorkDate, db)
        |       SqlBulkCopy into stg.tbl_vw3pbillsub staging table
        |       Stored procedure MERGE into pats.tbl_vw3pBillSub
        |       → pats.tbl_vw3pBillSub (Azure BHG_DR) — via bulk/staging path
        |       → RowTrax audit if st.RowTrax = true
        |
        |-- mark child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)

        [Azure BHG_DR — final state after run]
        pats.tbl_pbi3payauth  — auth records upserted; absent records RowState=false
        pats.tbl_vw3pBillSub  — bill sub records updated via bulk/staging merge
________________________________________

16. File Reference Map

File Path                                                     Purpose
---------                                                     -------
BCAppCode/BHG-DR-LIB/SaveAuths.cs                            Both methods (427 lines)
BCAppCode/BHGTaskRunner/Program.cs                            Schedule 8 dispatch
                                                              pats.tbl_pbi3payauth   ~line 1329
                                                              pats.tbl_vw3pbillsub   ~line 3081
BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs                         BulkDartsSrvLoader — production path for BillSub
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                    Builds SELECT column list and RowChkSum expression
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                        ADO.NET wrapper — executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblPbi3Payauth.cs               EF Model → pats.tbl_pbi3payauth
BCAppCode/BHG-DR-LIB/Models/Tblvw3pBillSub.cs               EF Model → pats.tbl_vw3pBillSub (bypassed)
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                 EF DbContext — DbSet registrations
BCAppCode/Scheduler/Program.cs                                Task creation for SAMMS-ETL-INV schedule
________________________________________

17. Quick Reference Summary

Method              Load path       Key                                           RowChkSum guard      RowState       Schedule
------              ---------       ---                                           ---------------      --------       --------
SaveAuths           EF Core         SiteCode + TpaId                              Yes (effective)      Yes (bool)     8 / 3
SaveAuthBillsub     BYPASSED        SiteCode+DsId+PyPayerid+PySubsid+CptMod+Charge  None (builds fresh) Yes (bool)     8 / 3

Production bill sub path:
    pats.tbl_vw3pbillsub → BulkDartsSrvLoader → stg.tbl_vw3pbillsub → MERGE → pats.tbl_vw3pBillSub

Critical bugs:
1. SaveAuths — RowTrax block is present but empty — no audit logging for auth records
2. SaveAuths — 5 fields permanently commented out (TpaCompKey, TpaBigKey, ProgGroup,
   PayerGroup, PayerType) — these are NULL in Azure for all records
3. SaveAuths — TpServ truncation guard is off-by-one (checks > 300, truncates to 299)
4. SaveAuths — tpeffdate (no 'a') lacks dash-to-slash normalization unlike tpaeffdate
5. SaveAuthBillsub — bypassed in production; EF Core method is dead code
6. SaveAuthBillsub — stale record removal path is broken (SaveChanges and BSubsNew.Add
   both commented out — stale records staged for delete but not committed, replacement not added)
7. SaveAuthBillsub — mid-loop db.SaveChanges() on not-found path is a performance anti-pattern



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
Known anomalies	EF Core method bypassed — dead code; stale record replacement path broken (Save