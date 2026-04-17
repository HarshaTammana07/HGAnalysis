
Payer-Client ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 8 — SAMMS-ETL-INV (primary) / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract payer-client insurance
enrollment and history data from local SAMMS SQL Server databases at each clinic and load them
into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What payer-client and payer-client history data are and why they exist
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and BHGTaskRunner dispatches them under Schedule 8
- How all three methods in SavePayorClient.cs work in detail
- The dual-source-view routing in BHGTaskRunner (active vs. inactive records)
- The Reload flag mechanism that controls the source WHERE clause for SavePayerClient
- How RowChkSum change detection behaves — and why it is completely disabled for SavePayerClient
- The soft-delete mechanism implemented in RemovePayerClients
- The silent update loss bug in SavePayerCltHistory
- All known anomalies, bugs, and dead code

There are three methods in SavePayorClient.cs spanning two Azure destination tables:

pats.tbl_PayerClient:     SavePayerClient       (active record upsert)
pats.tbl_PayerClient:     RemovePayerClients    (soft-delete path for inactive records)
pats.tbl_PayerCltHistory: SavePayerCltHistory   (payer change audit history)
________________________________________

2. High-Level Business Summary

What is payer-client data?

In a Medication-Assisted Treatment (MAT) and behavioral health clinic environment, every
patient may have one or more insurance payers attached to them at any point in time. The
payer-client relationship is the enrollment record that links a patient (client) to a specific
insurance payer — capturing the payer identity, policy details, coverage dates, eligibility
check results, subscriber information, and active/inactive status.

pats.tbl_PayerClient — Patient Payer Enrollment Records
This table holds one row per payer-per-patient-per-site. It records the payer ID (PyId),
the linked client ID (PyCltid), the payer type and payer company ID (PyPayertype, PyPayerid),
the subscriber ID (PySubsid), group number (PyGroup), authorization number (PyAuth), coverage
start and projected end dates (PyStart, PyEnd, PyProjectedEnd), the add date and add user
(PyAddDate, PyAddUser), address/demographic fields on the payer's subscriber
(Pyadd, Pycity, Pystate, Pyzip, PyPhone, Pyfirst, Pylast, PyDob, Pysame), co-insurance and
co-pay amounts (Pycoins, Pycopay), deductible and deductible-remaining amounts (Pyded,
Pydeduct, Pydeductleft), the eligibility check date and user (PyEligCheck, PyEligUser),
type-specific coverage codes (Pyfront, Pymmt, Pyout, PyBack, Pybupe), a temporary save field
(TempSavePayer), a basic number (PyBasicNum), a category code (PyCategory), HMO provider
(PyHmoprovider), local office (PyLocalOffice), and database notes (PyDbnotes). The active
flag (PyActive) indicates whether the enrollment is currently valid.

pats.tbl_PayerCltHistory — Payer Change Audit History
This table records a time-stamped audit trail every time a payer-client record is modified in
SAMMS. Each row captures the history record ID (PchId), the linked payer ID (PyId), the change
description (PyChange), the date and time of the change (PyDtm), the user who made the change
(PyUser), and a note (PyNote). It serves as an insurance enrollment change log.

Why this data is important
- Accurate payer-client enrollment records drive billing — incorrect payer assignments result
  in claim rejections or wrong-payer billing
- The active flag (PyActive) and coverage dates control which payer is submitted for each visit
- Eligibility check fields support real-time insurance verification workflows
- The history table provides a compliance-ready audit trail of all payer changes per patient
- The inactive path (RemovePayerClients) ensures that payers terminated in SAMMS are reflected
  as inactive in Azure, preventing stale billing against closed coverages

Load type
SavePayerClient uses an EF Core two-phase upsert with a lookup key of PyId + PyCltid. Change
detection is functionally disabled (see Section 12). RemovePayerClients uses an EF Core
soft-delete path — it sets PyActive=false for records that appear in the inactive source view.
SavePayerCltHistory uses an EF Core upsert with a fixed field list (no dynamic column switch),
but its update path contains a critical bug where UpdateRange is commented out, causing updates
to existing history records to be silently lost.
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
SavePayorClient.cs (BHG-DR-LIB)            3 methods for payer-client and payer history data
Models/TblPayerClient.cs                    EF entity → pats.tbl_PayerClient
Models/TblPayerCltHistory.cs               EF entity → pats.tbl_PayerCltHistory
pats.tbl_PayerClient (Azure BHG_DR)        Final destination for payer-client enrollment records
pats.tbl_PayerCltHistory (Azure BHG_DR)   Final destination for payer change history records
tsk.tbl_RowTrax (Azure DB)                 Audit log — source vs destination row counts
________________________________________

4. Scheduler — How Payer-Client Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task for SAMMS-ETL-INV schedule
    TaskName = 'SAMMS-ETL-INV'
    SiteCode = 'All'
    Status   = 17

Step 3 — Insert child tasks per clinic
For each active clinic, child tasks are inserted:
    TaskName = 'pats.tbl_payerclient'       SiteCode = 'B01A'    (active payer records)
    TaskName = 'pats.tbl_payerclient'       SiteCode = 'B01A'    (inactive payer records — FromTblVw = vw_PayerClt_INACTIVE)
    TaskName = 'pats.tbl_payerclthistory'   SiteCode = 'B01A'
    ... (one row per table per clinic)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How Payer-Client Tasks Are Orchestrated

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

CASE: pats.tbl_payerclient
    Reload flag check:
        if (st.Reload.HasValue && st.Reload.Value == true)
        {
            // Skip WHERE condition — full site reload, no date filter
        }
        else
        {
            strCmd += " Where " + st.WhereCondition
                          .Replace("@WorkDate", "'" + WorkDate.AddDays(DaysBack) + "'")
                          .Replace("@SiteCode", "'" + st.SiteCode + "'")
        }
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)

    Source view routing:
        if (st.FromTblVw == "vw_PayerClt_INACTIVE")
        {
            → rCodes = sd.RemovePayerClients(SrcDt, st.SiteCode, WorkDate.AddDays(DaysBack), true, null)
            → Soft-delete path — sets PyActive=false for matched records
            → RowTrax NOT triggered for this path
        }
        else
        {
            → rCodes = sd.SavePayerClient(SrcDt, st.SiteCode, WorkDate.AddDays(DaysBack), true, null)
            → Upsert path — inserts new or updates existing payer-client records
            → RowTrax audit triggered if st.RowTrax = true
        }

CASE: pats.tbl_payerclthistory
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SavePayerCltHistory(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null)
    → No Reload flag handling
    → No RowTrax audit

Step 6 — RowTrax audit (if st.RowTrax = true, pats.tbl_payerclient active path only)
    Source count: SELECT count(1) FROM SrcSchema.FromTblVw
    Azure count:  SELECT count(1) FROM pats.tbl_payerclient WHERE SiteCode = 'B01A'

Step 7 — Mark child task Status=20 (complete)

Schedule 3 note:
BHGTaskRunner.exe 3 is the catch-all schedule. Its task filter includes ALL tasks that are
not P1, P2, or SAMMSGlobal — this includes SAMMS-ETL-INV child tasks. So if these task names
appear in the queue when arg=3 is run, they will also be processed by the same inner switch.
In practice, the dedicated arg=8 run handles the SAMMS-ETL-INV parent and its children first.
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the full SELECT, and returns a DataTable. Connection strings are from
ctrl.tbl_LocationCons. The source view name (st.FromTblVw) comes from dms.tbl_MapAction
per task. Typical SAMMS source names:
    vw_PayerClt               → pats.tbl_PayerClient  (active payer-client upsert path)
    vw_PayerClt_INACTIVE      → pats.tbl_PayerClient  (inactive soft-delete path)
    vw_PayerCltHistory        → pats.tbl_PayerCltHistory
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

7a. dbo.tblPayerClient / vw_PayerClt — Payer-Client Enrollment Records

Key columns:
    pyid               int        Unique payer-client record ID (primary key component)
    pycltid            int        Patient/client ID (primary key component; may be negative for legacy)
    RowChkSum          int        CHECKSUM() computed over key payer fields
    pyadd              varchar    Payer subscriber address line 1
    pypayerid          varchar    Payer company/insurance ID
    pypayertype        varchar    Payer type classification
    pysubsid           varchar    Subscriber ID
    pygroup            varchar    Group number
    pyauth             varchar    Authorization number
    pystart            datetime   Coverage start date
    pyend              datetime   Coverage end date (nullable)
    pyactive           bool       Active coverage flag
    pycity             varchar    Subscriber city
    pydob              datetime   Subscriber date of birth
    pyfirst            varchar    Subscriber first name
    pylast             varchar    Subscriber last name
    pyphone            varchar    Subscriber phone number
    pysame             bool       Same as patient address flag
    pystate            varchar    Subscriber state
    pyzip              varchar    Subscriber ZIP code
    pyadddate          datetime   Date payer record was added (also aliased as payadddate)
    pyadduser          varchar    User who added the payer record
    pyback             varchar    Back coverage code
    pybupe             varchar    Buprenorphine coverage code
    pycoins            decimal    Co-insurance amount
    pycopay            decimal    Co-payment amount
    pyded              varchar    Deductible description
    pydeduct           decimal    Deductible amount
    pydeductleft       decimal    Remaining deductible amount
    pyeligcheck        datetime   Last eligibility check date
    pyeliguser         varchar    User who performed eligibility check
    pyfront            varchar    Front coverage code
    pymmt              varchar    MMT coverage code
    pyout              varchar    Out-of-pocket code
    pyprojectedend     datetime   Projected end of coverage (nullable)
    tempsavepayer      varchar    Temporary save field
    pybasicnum         varchar    Basic number
    pycategory         varchar    Category code
    pyhmoprovider      varchar    HMO provider code
    pylocaloffice      varchar    Local office code
    pydbnotes          varchar    Database-level notes

7b. dbo.tblPayerCltHistory / vw_PayerCltHistory — Payer Change History Records

Key columns:
    pchid              int        Unique history record ID (primary key)
    pyid               int        Linked payer-client record ID
    pychange           varchar    Description of the change made
    pydtm              datetime   Date and time of the change (no NULL guard — see Anomaly 6)
    pyuser             varchar    User who made the change
    pynote             varchar    Change note or additional context
________________________________________

8. SavePayerClient — Active Payer-Client Upsert (pats.tbl_PayerClient)

Source: vw_PayerClt (or clinic-specific view)
Destination: pats.tbl_PayerClient
Composite key: SiteCode + PyId + Math.Abs(PyCltid)
Parameters: tbl, sc, wrkdt, yearly, db

Azure pre-load:
    if (yearly)
    {
        payerClients = db.TblPayerClient.Where(x => x.SiteCode == sc).ToList()
    }
    else
    {
        payerClients = db.TblPayerClient.Where(x => x.SiteCode == sc).ToList()
    }
Both branches have IDENTICAL queries. The yearly flag has absolutely no effect on pre-load
scope (see Anomaly 1).

AllNewRows flag: If payerClients.Count == 0 (site has no existing records in Azure),
AllNewRows is set to true. Every incoming row is treated as new — the lookup step is skipped.
This is valid for first-load scenarios but is never triggered on subsequent daily runs unless
the Azure table is empty for that site.

PyCltid pre-read:
    int cltid = 0;
    if (r["pycltid"].ToString().Length > 0) { cltid = int.Parse(r["PyCltid"].ToString()); }
The client ID is parsed before the column loop and stored in a local variable. The column
loop's "pycltid" case then writes this pre-read value: pc.PyCltid = cltid.

Lookup key: PyId + Math.Abs(PyCltid)
    pc = payerClients.Where(x => x.PyId == pyid && Math.Abs(x.PyCltid) == Math.Abs(cltid))
                     .FirstOrDefault()
Math.Abs is applied to both sides. This allows matching regardless of whether the stored or
incoming client ID carries a negative sign (used in SAMMS for legacy/inactive patient IDs).

RowChkSum change detection — COMPLETELY DISABLED:
    if (1 == 1)
    //if ((pc.RowChkSum == myrcs) || (pc.RowChkSum != myrcs) || (NewRow))
The guard condition is hardcoded to always true. The real RowChkSum comparison is commented
out (and even if restored, the commented form is a tautology — it covers all cases). This
means the column loop and all 37 field mappings execute for every row on every run, regardless
of whether any data changed (see Anomaly 2).

PyStart new-row gap:
    //PyStart = pystart,    ← COMMENTED OUT in new row constructor
PyStart is mapped in the column switch loop but is NOT set when constructing a new TblPayerClient
object. New records will have a NULL PyStart until the switch loop runs immediately after the
constructor and maps it via the "pystart" case (see Anomaly 3).

Commit sequence:
    foreach row:
        if (NewRow || AllNewRows) → PCNew.Add(pc)
        else → db.TblPayerClient.Update(pc)    ← individual Update per updated row
    db.SaveChanges()           ← commits all individual updates
    if (PCNew.Count > 0)
    {
        db.TblPayerClient.AddRange(PCNew)
        db.SaveChanges()       ← commits all new inserts in one batch
    }

Note: Updated records are submitted via individual db.TblPayerClient.Update(pc) calls inside
the loop (one per row), not via UpdateRange. New records are batched via AddRange. This is a
mixed pattern — updates are individually tracked, inserts are batched.

LastModAt is set unconditionally: pc.LastModAt = DateTime.Now

Column mapping (SavePayerClient — 37 fields via switch):

    Source column          EF property           Type / notes
    ---------              -----------           -----
    (pre-read)             PyId                  int — r["pyid"]
    (pre-read)             RowChkSum             int — r["RowChkSum"]
    (pre-read)             PyCltid               int — r["pycltid"] (0 if empty)
    (pre-read)             Pyadd                 string — r["pyadd"]
    rowchksum              RowChkSum             int
    pypayerid              PyPayerid             string
    pypayertype            PyPayertype           string
    pysubsid               PySubsid              string
    pygroup                PyGroup               string
    pyauth                 PyAuth                string
    pystart                PyStart               DateTime? — length > 7 guard
    pyend                  PyEnd                 DateTime? — length > 7 guard; null if short
    pycltid                PyCltid               int — reads pre-computed cltid variable
    pyactive               PyActive              bool? — bool.Parse; length > 0 guard
    pyadd                  Pyadd                 string
    pycity                 Pycity                string
    pydob                  PyDob                 DateTime? — length > 7 guard
    pyfirst                Pyfirst               string
    pylast                 Pylast                string
    pyphone                PyPhone               string
    pysame                 Pysame                bool? — bool.Parse; length > 0 guard
    pystate                Pystate               string
    pyzip                  Pyzip                 string
    pyadddate / payadddate PyAddDate             DateTime? — length > 7 guard; null if short
                                                  (dual case labels — handles both column names)
    pyadduser              PyAddUser             string
    pyback                 PyBack                string
    pybupe                 Pybupe                string
    pycoins                Pycoins               decimal? — decimal.Parse; length > 0 guard
    pycopay                Pycopay               decimal? — decimal.Parse; length > 0 guard
    pyded                  Pyded                 string
    pydeduct               Pydeduct              decimal? — decimal.Parse; length > 0 guard
    pydeductleft           Pydeductleft          decimal? — decimal.Parse; length > 0 guard
    pyeligcheck            PyEligCheck           DateTime? — length > 7 guard
    pyeliguser             PyEligUser            string
    pyfront                Pyfront               string
    pymmt                  Pymmt                 string
    pyout                  Pyout                 string
    pyprojectedend         PyProjectedEnd        DateTime? — length > 7 guard; null if short
    tempsavepayer          TempSavePayer         string
    pybasicnum             PyBasicNum            string
    pycategory             PyCategory            string
    pyhmoprovider          PyHmoprovider         string
    pylocaloffice          PyLocalOffice         string
    pydbnotes              PyDbnotes             string
________________________________________

9. RemovePayerClients — Soft-Delete for Inactive Payers (pats.tbl_PayerClient)

Source: vw_PayerClt_INACTIVE (clinic-specific view of terminated/inactive payer records)
Destination: pats.tbl_PayerClient
Composite key: SiteCode + PyId + Math.Abs(PyCltid)
Parameters: tbl, sc, wrkdt, yearly, db

This method is called when BHGTaskRunner detects the source view is vw_PayerClt_INACTIVE.
Rather than inserting or updating records, it marks existing payer-client records as inactive.

Azure pre-load:
    pcs = db.TblPayerClient.Where(x => x.SiteCode == sc).ToList()
Full site slice — all existing payer records for the clinic are loaded into memory.

PyAddDate parsing (local variable only):
    DateTime dtadd = DateTime.Today;
    if (r["PyAddDate"].ToString().Length > 7) { dtadd = DateTime.Parse(r["PyAddDate"].ToString()); }
The dtadd variable is computed but never used in the lookup or update logic. The commented-out
filter in the lookup (x.PyAddDate.Value.Date == dtadd.Date) was removed, leaving dtadd as dead
code (see Anomaly 5).

Lookup key: PyId + Math.Abs(PyCltid)
    pc = pcs.FirstOrDefault(x => x.PyId == id && Math.Abs(x.PyCltid) == Math.Abs(cltid))
Consistent with SavePayerClient — Math.Abs applied to both sides.

Soft-delete action:
    if (pc != null)
    {
        pc.PyActive = false
        pc.LastModAt = DateTime.Now
        db.TblPayerClient.Update(pc)
    }
Only PyActive and LastModAt are written. All other fields on the record are left unchanged.
If no matching record is found (pc == null), no action is taken — the row is silently skipped.

Commit: db.SaveChanges() is called once after the full loop.

wrkdt parameter: Accepted but never used inside the method.
yearly parameter: Accepted but never used inside the method. BHGTaskRunner passes true.
RowTrax: Not triggered for the inactive path — BHGTaskRunner explicitly checks:
    if ((st.RowTrax.HasValue) && (st.FromTblVw != "vw_PayerClt_INACTIVE"))
________________________________________

10. SavePayerCltHistory — Payer Change History Upsert (pats.tbl_PayerCltHistory)

Source: vw_PayerCltHistory (or clinic-specific history view)
Destination: pats.tbl_PayerCltHistory
Key: SiteCode + PchId
Parameters: tbl, sc, wrkdt, yearly, db

Azure pre-load:
    PCHs = db.TblPayerCltHistory.Where(x => x.SiteCode == sc).ToList()
Full site slice — all existing history records for the clinic.

Unlike SavePayerClient, SavePayerCltHistory does NOT use a dynamic column switch. Both new
and updated records are assigned via a fixed, direct field list.

New record path (pch == null):
    pch = new Models.TblPayerCltHistory()
    pch.SiteCode = sc
    pch.PchId    = pchid
    pch.PyId     = int.Parse(r["pyid"].ToString())
    pch.PyChange = r["pychange"].ToString()
    pch.PyDtm    = DateTime.Parse(r["pydtm"].ToString())    ← NO NULL GUARD (see Anomaly 6)
    pch.PyUser   = r["pyuser"].ToString()
    pch.PyNote   = r["pynote"].ToString()
    PCHNew.Add(pch)
    rc.RowsIns += 1

Update record path (pch != null):
    pch.PyId     = int.Parse(r["pyid"].ToString())
    pch.PyChange = r["pychange"].ToString()
    pch.PyDtm    = DateTime.Parse(r["pydtm"].ToString())    ← NO NULL GUARD
    pch.PyUser   = r["pyuser"].ToString()
    pch.PyNote   = r["pynote"].ToString()
    PCHUpd.Add(pch)
    rc.RowsUpd += 1

CRITICAL BUG — Updates silently lost:
    if (PCHUpd.Count > 0)
    {
        //db.TblPayerCltHistory.UpdateRange(PCHUpd)    ← COMMENTED OUT
        db.SaveChanges()                                ← SaveChanges with nothing tracked
    }
UpdateRange is commented out. EF Core has no tracked changes for the updated entities since
they were loaded via .ToList() and modified in memory but never explicitly attached to the
change tracker for update. db.SaveChanges() executes with zero pending changes. All updates
to existing history records are silently discarded (see Anomaly 7).

New record commit (correctly implemented):
    if (PCHNew.Count > 0)
    {
        db.TblPayerCltHistory.AddRange(PCHNew)    ← correctly staged
        db.SaveChanges()                          ← correctly committed
    }
Only insertions work. Updates do not.

wrkdt parameter: Accepted but never used inside the method.
yearly parameter: Accepted but never used. BHGTaskRunner passes false.
No RowChkSum, no RowState, no LastModAt on this table.
________________________________________

11. Shared EF Core Patterns

BHG_DRContext:
    if (db == null) { db = new Models.BHG_DRContext(); }
All three methods accept an optional db context. If null, a new context is instantiated.
This allows the caller (BHGTaskRunner) to pass null for normal operation or inject a context
for testing.

RCodes return:
    rc.IsResult = true initially
    rc.RowsProcessed = tbl.Rows.Count
    rc.RowsIns  incremented on each new row
    rc.RowsUpd  incremented on each updated row
On exception: rc.IsResult = false, rc.ExceptMsg populated, InnerException captured if present.

Error handling (all three methods):
    catch (Exception e)
    {
        rc.IsResult = false
        rc.ExceptMsg = e.Message
        Console.WriteLine(e.Message)
        if (e.InnerException != null)
        {
            rc.ExceptInnerMsg = e.InnerException.Message
            Console.WriteLine(e.InnerException.Message)
        }
    }
The InnerException is stored in the separate rc.ExceptInnerMsg field — this is the correct
pattern consistent with most other BHG-DR-LIB methods.
________________________________________

12. Change Detection — RowChkSum Behaviour

Method                  RowChkSum present    Guard used    Effective behaviour
------                  -----------------    ----------    -------------------
SavePayerClient         Yes                  Disabled      if (1==1) always true — every row always fully written. RowChkSum is stored in Azure but never compared before updating.
RemovePayerClients      N/A                  N/A           Soft-delete only — writes PyActive=false and LastModAt. No RowChkSum comparison.
SavePayerCltHistory     No                   N/A           No RowChkSum in source or destination. Full overwrite on every run (for new rows only — update path is broken).
________________________________________

13. Scoping / Data Windowing

Source-side scoping for SavePayerClient (applied before GetTableData is called):
    Normal run:   strCmd += WHERE strWhere (rolls @WorkDate back by DaysBack=-15)
    Reload=true:  WHERE skipped — full source table returned
The Reload flag is read from task metadata (st.Reload) and controls whether the source query
returns a date-limited or full result set. This is the only effective scoping control for this
method since Azure-side pre-load always loads the full site slice regardless of the yearly flag.

Source-side scoping for RemovePayerClients:
    Same Reload flag logic applies — BHGTaskRunner applies the same WHERE handling before
    calling RemovePayerClients as it does for SavePayerClient.

Source-side scoping for SavePayerCltHistory:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    No Reload flag — always uses rolling WHERE with DaysBack=-15.

Azure-side scoping (controls which existing records are loaded into memory for comparison):
    SavePayerClient:     Full site slice — payerClients = Where SiteCode == sc
    RemovePayerClients:  Full site slice — pcs = Where SiteCode == sc
    SavePayerCltHistory: Full site slice — PCHs = Where SiteCode == sc

All three methods load the complete Azure site slice into memory. For clinics with large
payer-client histories, this can result in significant memory pressure in the process.
________________________________________

14. Error Handling

All three methods (SavePayerClient, RemovePayerClients, SavePayerCltHistory) use identical
error handling:

    catch (Exception e)
    {
        rc.IsResult = false
        rc.ExceptMsg = e.Message
        Console.WriteLine(e.Message)
        if (e.InnerException != null)
        {
            rc.ExceptInnerMsg = e.InnerException.Message
            Console.WriteLine(e.InnerException.Message)
        }
    }

This is the standard BHG-DR-LIB error pattern — InnerException is correctly stored in the
separate ExceptInnerMsg field (not concatenated into ExceptMsg). A SavePayerCltHistory failure
caused by DateTime.Parse on a null pydtm value will propagate here, setting IsResult=false and
populating ExceptMsg with the parse exception message.
________________________________________

15. Anomalies, Bugs, and Known Defects

ANOMALY 1 — SavePayerClient: yearly flag has no effect.

File: SavePayorClient.cs, lines 25–38
    if (yearly)
    {
        payerClients = db.TblPayerClient.Where(x => x.SiteCode == sc).ToList()
    }
    else
    {
        payerClients = db.TblPayerClient.Where(x => x.SiteCode == sc).ToList()
    }

Both branches have identical LINQ predicates. The yearly flag was presumably intended to
scope the Azure pre-load differently (e.g., by year or by date range) but was never
implemented. Commented-out filters (x.Pcid >= 1 and x.PyAddDate == null || x.PyAddDate ==
wrkdt) suggest the scope logic was planned but removed. BHGTaskRunner always passes
yearly=true, making this dead-branch code.

ANOMALY 2 — SavePayerClient: RowChkSum change detection completely disabled.

File: SavePayorClient.cs, lines 92–93
    if (1 == 1)
    //if ((pc.RowChkSum == myrcs) || (pc.RowChkSum != myrcs) || (NewRow))

The live condition is a hardcoded tautology. The commented form is also a tautology
(covers all three cases — equal, not equal, or new row). Every incoming row always passes the
guard and all 37 field mappings always execute. This generates UPDATE statements for every
existing record on every daily run regardless of whether any data actually changed, increasing
unnecessary database write traffic to pats.tbl_PayerClient.

ANOMALY 3 — SavePayerClient: PyStart not set in new row constructor.

File: SavePayorClient.cs, lines 54–63
    pc = new Models.TblPayerClient
    {
        PyId = pyid,
        SiteCode = sc,
        RowChkSum = myrcs,
        PyCltid = cltid,
        //PyStart = pystart,    ← COMMENTED OUT
        Pyadd = pyadd
    };

PyStart is not assigned during object construction. It is populated immediately after via the
column switch loop (case "pystart"), so the field is correctly populated for complete source
rows. However, if the source DataTable does not include a "pystart" column, the new record
will have a NULL PyStart. The commented-out pre-read variable pystart (line 45–46) is also
dead code and can never be used.

ANOMALY 4 — SavePayerClient: pycltid case does not read from source column.

File: SavePayorClient.cs, lines 125–129
    case "pycltid":
        //if (r["PyCltid"].ToString().Length > 0)
        //{ pc.PyCltid = int.Parse(r["PyCltid"].ToString()); }
        pc.PyCltid = cltid;    ← reads pre-computed variable, not r[dc.ColumnName]
        break;

The direct parse from the DataRow is commented out. The case always writes the pre-read cltid
value. This is consistent and correct as long as the pre-read (lines 47–51) captures the right
value, but it means the column switch does not serve as the authoritative source for this field.

ANOMALY 5 — RemovePayerClients: dtadd variable computed but never used.

File: SavePayorClient.cs, lines 284–291
    DateTime dtadd = DateTime.Today;
    if (r["PyAddDate"].ToString().Length > 7)
    {
        dtadd = DateTime.Parse(r["PyAddDate"].ToString());
    }
    Models.TblPayerClient pc = pcs.FirstOrDefault(x => x.PyId == id
        && Math.Abs(x.PyCltid) == Math.Abs(cltid)
        //&& x.PyAddDate.Value.Date == dtadd.Date    ← COMMENTED OUT
        );

The dtadd variable is always computed but is never used in the lookup because the date
filter is commented out. This means the soft-delete lookup matches only on PyId + PyCltid.
If two records with the same PyId and PyCltid existed at different add dates, both would be
matched by the lookup and only the first (FirstOrDefault) would be deactivated.

ANOMALY 6 — SavePayerCltHistory: PyDtm DateTime.Parse has no null guard.

File: SavePayorClient.cs, lines 340, 350
    pch.PyDtm = DateTime.Parse(r["pydtm"].ToString())

Both the new record path and the update path call DateTime.Parse directly on the pydtm field
with no length check. If any source row has a null or empty pydtm value, DateTime.Parse will
throw a FormatException, causing the entire method to fail for that site and leaving all
subsequent rows unprocessed. This is particularly risky for history records where the change
timestamp may not always be populated.

ANOMALY 7 — SavePayerCltHistory: UpdateRange commented out — all updates silently lost.

File: SavePayorClient.cs, lines 357–361
    if (PCHUpd.Count > 0)
    {
        //db.TblPayerCltHistory.UpdateRange(PCHUpd);    ← COMMENTED OUT
        db.SaveChanges();
    }

db.TblPayerCltHistory.UpdateRange(PCHUpd) is commented out. The entities in PCHUpd are
loaded via .Where().ToList() and are tracked by EF Core's change tracker only if they were
loaded by the same context instance with default tracking enabled. In this code, db is
either injected (passed in) or newly created — in either case, the .Where().ToList() on
line 326 does attach the entities to the context and EF Core will detect property changes.
However, the fact that UpdateRange is explicitly commented out suggests this was a deliberate
disabling. The practical result is that EF Core may or may not detect the in-memory changes
depending on tracking state, but the intent to update is clearly suppressed. rc.RowsUpd is
still incremented, giving a false indication that updates occurred.
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
        |       pats.tbl_payerclient (FromTblVw=vw_PayerClt)           SiteCode='B01A'
        |       pats.tbl_payerclient (FromTblVw=vw_PayerClt_INACTIVE)  SiteCode='B01A'
        |       pats.tbl_payerclthistory                                SiteCode='B01A'
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
        |  BRANCH A: pats.tbl_payerclient (active path — vw_PayerClt)
        |======================================================
        |   if (Reload=true): skip WHERE → full source result set
        |   else: strCmd += " Where " + WhereCondition (@WorkDate, @SiteCode replaced)
        |   SrcDt = sm.GetTableData(vw_PayerClt, strCmd, ConStr)
        |   → sd.SavePayerClient(SrcDt, SiteCode, WorkDate.AddDays(-15), yearly=true, null)
        |       Load full site slice from pats.tbl_PayerClient (yearly flag no effect)
        |       Loop rows → lookup by PyId + Math.Abs(PyCltid)
        |       RowChkSum guard: ALWAYS passes (if 1==1) — all 37 fields written every run
        |       Individual db.Update(pc) per updated row → db.SaveChanges()
        |       db.AddRange(PCNew) → db.SaveChanges()
        |       → pats.tbl_PayerClient (Azure BHG_DR)
        |       → RowTrax audit if st.RowTrax = true
        |
        |======================================================
        |  BRANCH B: pats.tbl_payerclient (inactive path — vw_PayerClt_INACTIVE)
        |======================================================
        |   Same WHERE logic as Branch A (Reload flag applies)
        |   SrcDt = sm.GetTableData(vw_PayerClt_INACTIVE, strCmd, ConStr)
        |   → sd.RemovePayerClients(SrcDt, SiteCode, WorkDate.AddDays(-15), yearly=true, null)
        |       Load full site slice from pats.tbl_PayerClient
        |       Loop rows → lookup by PyId + Math.Abs(PyCltid)
        |       If found: set PyActive=false, LastModAt=DateTime.Now, db.Update(pc)
        |       If not found: silently skip
        |       db.SaveChanges()
        |       → pats.tbl_PayerClient (Azure BHG_DR) — PyActive set to false
        |       → RowTrax NOT triggered for this path
        |
        |======================================================
        |  BRANCH C: pats.tbl_payerclthistory
        |======================================================
        |   strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   → sd.SavePayerCltHistory(SrcDt, SiteCode, WorkDate.AddDays(-15), yearly=false, null)
        |       Load full site slice from pats.tbl_PayerCltHistory
        |       Loop rows → lookup by PchId
        |       New: assign fields directly → PCHNew.Add(pch)
        |       Update: assign fields directly → PCHUpd.Add(pch) → [UpdateRange COMMENTED OUT]
        |       PCHUpd: db.SaveChanges() with no tracked changes → updates silently lost
        |       PCHNew: db.AddRange(PCHNew) → db.SaveChanges() → correctly inserted
        |       → pats.tbl_PayerCltHistory (Azure BHG_DR) — inserts only
        |       → No RowTrax audit
        |
        |-- mark child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)

        [Azure BHG_DR — final state after run]
        pats.tbl_PayerClient     — active payer enrollments upserted (all fields rewritten every run)
        pats.tbl_PayerClient     — inactive payer enrollments soft-deleted (PyActive=false)
        pats.tbl_PayerCltHistory — new history records inserted; existing updates silently lost
________________________________________

17. File Reference Map

File Path                                                         Purpose
---------                                                         -------
BCAppCode/BHG-DR-LIB/SavePayorClient.cs                          All 3 payer-client methods (383 lines)
BCAppCode/BHGTaskRunner/Program.cs                                Schedule 8 dispatch
                                                                  pats.tbl_payerclient    ~line 1291
                                                                  pats.tbl_payerclthistory ~line 3100
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                        Builds SELECT column list and RowChkSum expression
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                            ADO.NET wrapper — executes source SELECT
BCAppCode/BHG-DR-LIB/Models/TblPayerClient.cs                    EF Model → pats.tbl_PayerClient
BCAppCode/BHG-DR-LIB/Models/TblPayerCltHistory.cs               EF Model → pats.tbl_PayerCltHistory
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                     EF DbContext — DbSet registrations
BCAppCode/Scheduler/Program.cs                                    Task creation for SAMMS-ETL-INV schedule
________________________________________

18. Quick Reference Summary

Method                  Load path    Key                                RowChkSum guard    RowState    Schedule
------                  ---------    ---                                ---------------    --------    --------
SavePayerClient         EF Core      SiteCode + PyId + Abs(PyCltid)    Disabled (1==1)    No          8 / 3
RemovePayerClients      EF Core      SiteCode + PyId + Abs(PyCltid)    N/A (soft-delete)  No          8 / 3
SavePayerCltHistory     EF Core      SiteCode + PchId                  None               No          8 / 3

Routing split on source view:
    vw_PayerClt          → SavePayerClient      (upsert active records)
    vw_PayerClt_INACTIVE → RemovePayerClients   (soft-delete inactive records)
Both share the same TaskName = 'pats.tbl_payerclient' — the dispatch branch is decided by
st.FromTblVw at runtime.

Critical bugs:
1. SavePayerClient — RowChkSum guard replaced with if (1==1) — every record fully rewritten
   on every daily run regardless of whether data changed
2. SavePayerCltHistory — UpdateRange commented out — all updates to existing history records
   are silently lost; only new insertions succeed
3. SavePayerCltHistory — PyDtm has no null/length guard — any row with empty pydtm causes
   a DateTime.Parse FormatException and aborts the entire site's history load
4. SavePayerClient — yearly flag has no effect — both branches have identical WHERE queries
5. RemovePayerClients — dtadd variable is computed from PyAddDate but never used in the
   lookup — the date-scoped deactivation filter is permanently disabled
