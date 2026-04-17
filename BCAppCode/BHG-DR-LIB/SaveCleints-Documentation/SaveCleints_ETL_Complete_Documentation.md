
Client Demographic ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 2 / 4 — Regional ETL P1 / P2
________________________________________

NOTE ON FILE NAME: The source file is named SaveCleints.cs — "Cleints" is a typo for
"Clients" that has been preserved as-is in the codebase. This document refers to the
file by its actual name throughout.
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract core patient
demographic records from local SAMMS SQL Server databases at each clinic and load them
into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What client demographic data (Demo1 and Demo2) is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and BHGTaskRunner dispatches them
- How the four methods in SaveCleints.cs each work in detail
- The three distinct load paths: EF Core (SaveClientDemo1var), EF Core legacy
  (SaveClientDemo1), EF Core secondary (SaveClientDemo2), and the fingerprint-only
  path (SaveClientDemo3)
- The separate bulk copy path (stg.clientdemo) that also loads to TblClientDemo1/2
- How RowChkSum change detection and RowState soft-delete work in these methods
- The special B50x multi-site handling in SaveClientDemo1var
- The NetAlystic site exclusion in SaveClientDemo1
- All known anomalies, bugs, and dead code

There are four methods in SaveCleints.cs spanning two destination tables:

pats.tbl_ClientDemo1:  SaveClientDemo1var (primary, called daily), SaveClientDemo1 (legacy)
pats.tbl_ClientDemo2:  SaveClientDemo2 (primary, called daily), SaveClientDemo3 (fingerprint-only; disabled in production)
________________________________________

2. High-Level Business Summary

What is Client Demographic data?

Client demographic data is the foundational patient record in the BHG data warehouse.
Every patient enrolled in a BHG clinic has a record in both pats.tbl_ClientDemo1 and
pats.tbl_ClientDemo2. These two tables together capture the complete patient profile
that drives all downstream analytics, compliance reporting, billing validation, and
clinical workflows.

pats.tbl_ClientDemo1 — Identity and Demographics
This table captures the core identity and demographic profile of each patient: full
name (first, middle, last, suffix), date of birth, gender, Social Security Number,
contact information (address, phone, email), physical descriptors (height, weight,
hair, eye), race, language, county, marital status, employment status, employer,
income, education level, pregnancy status (Preg flag + EDC date), and the SAMMS
client ID linking back to the source system. The RowChkSum computed at source guards
which records are updated — only patients whose demographics have changed since the
last ETL run receive a field-level update.

pats.tbl_ClientDemo2 — Operational and Clinical Status
This table captures the patient's current treatment operational state: assigned
counselor, treatment status, program code, billing frequency, medication bottling
arrangement, dosing schedule days (DOW1/DOW2), next and last billing dates, next
treatment plan date, enrollment date, next physician TB test date, Medicaid flag,
insurance code, ethnicity, RIN (state registry identifier), UA schedule flags
(weekly, bi-weekly), AMSID (state ASAM ID), DDAP ID, Salesforce CRM linkage fields,
risk level, emergency contact (911 name/phone/relation), fingerprint biometric data
(two finger images as byte arrays), holiday pickup flag, and credit/back-fee balances.
The RowState flag marks whether the patient is currently active in the site's daily
census.

Why this data is important
- pats.tbl_ClientDemo1 is the master patient identity table in BHG_DR — nearly every
  other table joins to it via SiteCode + ClientId
- pats.tbl_ClientDemo2 drives census reporting, billing validation, and daily
  operational dashboards
- The RowState flag in Demo2 determines which patients appear in active census reports
- Salesforce CRM synchronization relies on the IsSalesForceSync / SalesForceId fields
  in Demo2 for outreach coordination
- Fingerprint biometric data in Demo2 supports patient identity verification at
  dispensing

Load type
The primary production paths (SaveClientDemo1var and SaveClientDemo2) use EF Core
upsert with RowChkSum change detection. A separate bulk copy path (stg.clientdemo,
handled by BulkDartsSrvLoader — not in SaveCleints.cs) also writes to Demo1/Demo2
for specific sites configured to use the stg.clientdemo task. There is no unified
consolidation between the EF Core path and the bulk path within this file.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=2                     Main ETL orchestrator for Regional ETL P1 (timezone-split)
BHGTaskRunner.exe arg=4                     Regional ETL P2 (additional client data wave)
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)              Column list + RowChkSum expression for SELECT build
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
BulkDartsSvc.cs                            Handles stg.clientdemo bulk copy path (separate from SaveCleints.cs)
SaveCleints.cs (BHG-DR-LIB)               4 EF Core upsert methods for client demographics
Models/TblClientDemo1.cs                    EF entity → pats.tbl_ClientDemo1
Models/TblClientDemo2.cs                    EF entity → pats.tbl_ClientDemo2
pats.tbl_ClientDemo1 (Azure BHG_DR)       Final destination for patient identity/demographics
pats.tbl_ClientDemo2 (Azure BHG_DR)       Final destination for patient operational/clinical status
tsk.tbl_RowTrax (Azure DB)                 Audit log — source vs destination row counts per run (Demo1 only)
________________________________________

4. Scheduler — How Client Demo Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent tasks for each timezone P1 schedule
The Scheduler creates four timezone-specific P1 parent tasks:
    TaskName = 'Eastern ETL P1',   SiteCode = 'All', Status = 17
    TaskName = 'Central ETL P1',   SiteCode = 'All', Status = 17
    TaskName = 'Mountain ETL P1',  SiteCode = 'All', Status = 17
    TaskName = 'Pacific ETL P1',   SiteCode = 'All', Status = 17

Step 3 — Insert child tasks per clinic
For each active clinic, child tasks are inserted:
    TaskName = 'pats.tbl_clientdemo1'    SiteCode = 'B01A'
    TaskName = 'pats.tbl_clientdemo2'    SiteCode = 'B01A'
    ... (one row per table per clinic, one of the above TaskNames)

Some clinics are configured with:
    TaskName = 'stg.clientdemo'          SiteCode = 'B01A'
    (bulk path — handled by BulkDartsSrvLoader, not SaveCleints.cs)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How Client Demo Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 2 (P1) or 4 (P2):
    BHGTaskRunner.exe 2

Step 1 — Filter queue by timezone P1 or P2 schedule names
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"
        && x.Status == 17
        && x.RunAt < DateTime.Now
        && (x.TaskName == "Central ETL P1"   ||
            x.TaskName == "Eastern ETL P1"   ||
            x.TaskName == "Mountain ETL P1"  ||
            x.TaskName == "Pacific ETL P1")).ToList()

Step 2 — Mark parent task as running (Status=18)

Step 3 — Load and order child tasks, loop one per clinic

Step 4 — Build base SELECT
    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
    strWhere = st.WhereCondition
                 .Replace("@SiteCode", "'" + st.SiteCode + "'")
                 .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")

Step 5 — Dispatch by TaskName

CASE: pats.tbl_clientdemo1
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveClientDemo1var(SrcDt, st.SiteCode, st.ActionKey, null)
    (RowTrax audit runs if st.RowTrax = true — see Section 16)

CASE: pats.tbl_clientdemo2
    if (st.ActionKey == 3)
    {
        // sd.SaveClientDemo3(SrcDt, st.SiteCode) ← COMMENTED OUT
        // No data movement occurs for ActionKey=3 in production
    }
    else
    {
        strCmd += " Where " + strWhere + " " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = sd.SaveClientDemo2(SrcDt, st.SiteCode, st.ActionKey, null)
    }

CASE: stg.clientdemo (bulk path — NOT SaveCleints.cs)
    A hardcoded SELECT from dbo.tblClient is assembled directly in BHGTaskRunner.
    SqlBulkCopy via BulkDartsSrvLoader into stg.clientdemo staging table.
    Not documented here — see BulkDartsSvc.cs.

Step 6 — RowTrax audit (Demo1 only, if st.RowTrax = true and SiteCode != "PHC")

Step 7 — Mark child task Status=20 (complete)

ActionKey behavior:
The `actionkey` parameter passed to SaveClientDemo1var and SaveClientDemo2 is
`st.ActionKey` from the task metadata row. When ActionKey=1 is stored in the task
metadata, the Save method performs a full pre-pass RowState=0 reset on all existing
records for the site before processing incoming data. This is a reload/resync trigger.
For normal daily runs (ActionKey=2), no pre-pass reset occurs — only records with
changed checksums are updated and RowState is set to 1 for all seen records.
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the full SELECT assembled from SelectConstructor + WhereCondition, and
returns a DataTable. Connection strings are from ctrl.tbl_LocationCons. The source
table or view name (st.FromTblVw) is from dms.tbl_MapAction per task — for client
demo tasks it is typically dbo.tblClient or a clinic-specific client view.

For Demo1 RowTrax, BHGTaskRunner fires an additional count(*) query against the
SAMMS source after the load:
    "Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw
    (with extra filter if FromTblVw == "vw_Clients": where AddressType = 2)
    (with extra filter if FromTblVw == "ClientDemo": where SiteCode = '{sc}' and Patient_UID is not null)
________________________________________

7. Source Table — SAMMS SQL Server (dbo)

Source: dbo.tblClient (or clinic-specific view — st.FromTblVw per task)

The SelectConstructor assembles the SELECT column list and RowChkSum expression from
dms.tbl_MapSrc2Dsn. The RowChkSum for tbl_clientdemo1 covers identity/demographic
fields; the RowChkSum for tbl_clientdemo2 covers operational/status fields.

Key source columns (SAMMS naming convention, abbreviated):

Identity/Demo columns (Demo1 scope)
    cltID           → ClientId      Unique client identifier within this clinic
    cltM4ID         → ClientM4id   M4 (national registry) patient ID
    cltFName        → FirstName     Patient first name
    cltMI           → MiddleName    Patient middle name/initial
    cltLName        → LastName      Patient last name
    cltSuffix       → Suffix        Name suffix (Jr, Sr, III, etc.)
    cltDOB          → Dob           Date of birth
    cltGender       → Gender        Gender code
    cltSSN          → Ssn           Social Security Number
    cltemail        → Email         Email address
    cltSize         → Size          Clothing size (int)
    cltADD1         → Address1      Street address line 1
    cltADD2         → Address2      Street address line 2
    cltCity         → City          City
    cltState        → State         State code
    cltzip          → Zip           ZIP code
    cltPhone        → Phone         Primary phone number
    cltpreg         → Preg          Pregnancy flag (bool)
    cltPregEDC      → PregEdc       Estimated delivery date
    cltMARRY        → Marital       Marital status code
    cltEmpStatus    → EmpStatus     Employment status code
    cltEmployer     → Employer      Employer name
    cltWorkPh       → WorkPhone     Work phone number
    cltIncome       → Income        Income category
    cltEducation    → Education     Education level code
    cltHair         → Hair          Hair color code
    cltEye          → Eye           Eye color code
    cltH            → Height        Height
    cltW            → Weight        Weight
    cltRace         → Race          Race code
    cltLANG         → Language      Primary language code
    cltCounty       → County        County of residence
    RowChkSum       → RowChkSum     CHECKSUM() computed over identity/demo columns

Operational/Status columns (Demo2 scope)
    cltCounselor    → Counselor     Assigned counselor
    cltStatus       → Status        Treatment status code
    cltProg         → Prog          Program code
    cltdtadded      → DateAdded     Enrollment/admit date
    cltAmount       → Amount        Fee amount
    cltFreq         → Freq          Billing frequency
    cltDOW1         → Dow1          Dosing day-of-week 1
    cltDOW2         → Dow2          Dosing day-of-week 2
    cltNextBill     → NextBill      Next billing date
    cltLastBill     → LastBill      Last billing date
    dtNextTP        → NextTp        Next treatment plan date
    dtPhysTB        → PhysTb        Next physician TB test date
    cltBOTTLES      → Bottles       Number of take-home bottles (Int16)
    Monthly         → Monthly       Monthly billing flag (bool)
    cltPICPATH      → Picpath       Patient photo file path
    cltRIN          → Rin           State registry ID (RIN)
    cltETH          → Eth           Ethnicity code
    cltMedicaid     → Medicaid      Medicaid coverage flag (bool)
    cltENROLLdt     → EnrollDate    Enrollment date
    cltBULK         → Bulk          Bulk take-home flag (bool)
    cltSTAND        → Stand         Standing order flag (bool)
    cltSPECIAL      → Special       Special notes code
    cltdtLastUA     → DtLastUa      Date of last urine analysis
    Amsid           → Amsid         State ASAM ID
    cltNOCENSUS     → Nocensus      Exclude from census flag (bool)
    cltCHANGEUSER   → Changeuser    Last user to modify record
    repOldClt       → RepOldClient  Replacement old client ID (decimal)
    dtuaweekly      → Uaweekly      Next weekly UA date
    cltOptIn        → OptIn         Opt-in consent flag (bool)
    cltcredit       → Credit        Credit balance (int)
    cltCONTTXDT     → Conttxdt      Continued treatment date
    cltINS          → Ins           Insurance code
    cltRISK         → Risk          Risk level code
    clt3pBack       → Clt3pBack     Insurance card back image path
    clt3pfront      → Clt3pfront    Insurance card front image path
    cltBIWEEKUA     → BiWeeklyUa    Bi-weekly UA schedule flag (bool)
    cltnursenote    → NurseNotes    Nurse notes text
    cltPANEL        → Panel         Panel/group assignment
    cltPAYDAY       → Payday        Payday day indicator
    cltFingerPrint1 → FingerPrint1  Fingerprint 1 biometric image (byte[])
    cltFingerPrint2 → FingerPrint2  Fingerprint 2 biometric image (byte[])
    clt911Name      → Clt911Name    Emergency contact name
    clt911PH        → Clt911Ph      Emergency contact phone
    clt911Relation  → Clt911Relation Emergency contact relationship
    salesForceId    → SalesForceId  Salesforce CRM record ID
    isSalesForceSync → IsSalesForceSync Salesforce sync status (int)
    cltHolidayPickup → HolidayPickup Holiday pickup flag (bool)
    ddapid          → Ddapid        DDAP state agency ID (long)
    provclt         → ProvClient    Provider client ID (long)
    provcltID       → ProvClientId  Provider client reference ID (long)
    cltBackFee      → BackFee       Back fee balance (decimal)
    cltREMARKS      → Remarks       General remarks / notes
    RowChkSum       → RowChkSum     CHECKSUM() computed over operational/status columns
________________________________________

8. SaveClientDemo1var — EF Core Upsert with RowChkSum and RowState (Primary Path)

File: BCAppCode/BHG-DR-LIB/SaveCleints.cs
Class: SaveData (partial class)
Method: SaveClientDemo1var()

Method signature:
    public RCodes SaveClientDemo1var(
        DataTable tbl,          // SAMMS source rows — one per patient
        string sc,              // SiteCode e.g. "B01A"
        int actionkey,          // st.ActionKey from task; 1 = reload reset, 2 = normal
        BHG_DRContext db)       // EF context (created if null)

Returns: RCodes — IsResult, RowsProcessed, RowsIns, RowsUpd, ExceptMsg, ExceptInnerMsg

EF Core upsert logic — step by step:

Step 1 — Create EF context and capture run time
    DateTime starttime = DateTime.Now
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load existing Azure rows (special B50x handling)

    if (sc.StartsWith("B50")):
        clients = db.TblClientDemo1.Where(x => x.SiteCode.StartsWith("B50")).ToList()
        // Loads ALL B50x records (B50A, B50B, B50C, ...) into a single in-memory list.
        // Then resets RowState=0 ONLY for the specific sc being processed:
        foreach (var s in clients)
        {
            if (s.SiteCode == sc) { s.RowState = 0; }
        }
    else:
        clients = db.TblClientDemo1.Where(x => x.SiteCode == sc).ToList()
        if (actionkey == 1):
            foreach (var s in clients) { s.RowState = 0; }   // full pre-pass reset

B50x EXPLANATION: The B50x prefix represents a group of affiliated clinics that
share patient populations. Loading ALL B50* rows into memory ensures that cross-site
patient lookups do not miss records belonging to a patient whose SiteCode entry in
Azure differs from the current site being processed. Only the current SiteCode rows
get the RowState=0 pre-pass reset.

ActionKey=1 EXPLANATION: When ActionKey=1 is set in the task metadata (typically a
manual reload trigger), every existing Demo1 record for this site is pre-set to
RowState=0 (inactive). As each SAMMS row is processed, only records that are seen
in the current extract get RowState=1 restored. Records that no longer appear in
SAMMS (e.g. discharged patients outside the query window) remain RowState=0.

Step 3 — Loop through every SAMMS row

    For each DataRow r in tbl.Rows:

    Step 3a — Extract key fields
        int cid = int.Parse(r["ClientID"].ToString())
        int rcs = int.Parse(r["RowChkSum"].ToString())

    Step 3b — Lookup existing record
        clt = clients.Where(x => x.ClientId == cid && x.SiteCode == sc).FirstOrDefault()

    Step 3c — New row path (not found in Azure)
        if (clt == null):
            clt = new TblClientDemo1
            {
                SiteCode = sc,
                ClientId = cid,
                RowChkSum = 0,       // deliberately 0 so the checksum check below is always true
                LastModAt = starttime,
                RowState = 1
            }
            db.TblClientDemo1.Add(clt)
            db.SaveChanges()         // PER-ROW COMMIT for new records
            res.RowsIns++

    Step 3d — RowChkSum check (applied to both new and existing rows)
        if (clt.RowChkSum != rcs):   // also true for new rows (RowChkSum=0 != rcs)
            clt.LastModAt = starttime
            clt.RowState  = 1
            clt.RowChkSum = rcs
            clt.SiteCode  = sc
            // Field mapping via inner column switch:

            Source Column   Destination         Guard / Transformation
            clientm4id      clt.ClientM4id      != "''" (single-quote guard)
            firstname       clt.FirstName       != "''" (single-quote guard)
            middlename      clt.MiddleName      != "''" (single-quote guard)
            lastname        clt.LastName        != "''" (single-quote guard)
            suffix          clt.Suffix          != "''" (single-quote guard)
            dob             clt.Dob             Replace("'","").Trim() length > 7, then DateTime.Parse
            gender          clt.Gender          != "''" (single-quote guard)
            ssn             clt.Ssn             Always (string)
            email           clt.Email           Always (string)
            size            clt.Size            int — only if length > 0
            address1        clt.Address1        Always — with .Trim()
            address2        clt.Address2        Always — with .Trim()
            city            clt.City            Always — with .Trim()
            state           clt.State           Always (string)
            zip             clt.Zip             Always (string)
            phone           clt.Phone           BUG: if length > 24: Substring(24, 0) → always empty string
                                                else: assigned as-is
                                                NOTE: Substring(24, 0) always returns empty — see anomalies
            preg            clt.Preg            bool — only if length > 0
            pregedc         clt.PregEdc         DateTime — only if length > 7
            marital         clt.Marital         Always (string)
            empstatus       clt.EmpStatus       Always (string)
            employer        clt.Employer        Always (string)
            workphone       clt.WorkPhone       Always (string)
            income          clt.Income          Always (string)
            education       clt.Education       Always (string)
            hair            clt.Hair            Always (string)
            eye             clt.Eye             Always (string)
            height          clt.Height          Always (string)
            weight          clt.Weight          Always (string)
            race            clt.Race            Always (string)
            language        clt.Language        Always (string)
            county          clt.County          Always (string)

            res.RowsUpd++

    Step 3e — RowChkSum unchanged path (existing row, no data change)
        else:
            clt.RowState = 1             // restore active status
            // clt.LastModAt = DateTime.Now  ← COMMENTED OUT
            res.RowsUpd++

Step 4 — Batch commit (existing row updates)
    db.SaveChanges()
    res.RowsUpd -= res.RowsIns      // correct RowsUpd: removes count of new rows
                                     // (new rows were double-counted in RowsUpd)
________________________________________

9. SaveClientDemo1 — EF Core Legacy Path (Not Called from BHGTaskRunner Daily)

File: BCAppCode/BHG-DR-LIB/SaveCleints.cs
Class: SaveData (partial class)
Method: SaveClientDemo1()

Method signature:
    public RCodes SaveClientDemo1(DataTable tbl, string sc, BHG_DRContext db)

Returns: RCodes (RowsIns/RowsUpd not incremented)

STATUS: This method is NOT called for the pats.tbl_clientdemo1 case in BHGTaskRunner.
The production daily path calls SaveClientDemo1var. SaveClientDemo1 appears to be an
earlier implementation that was superseded. It may be called from test code or one-off
scripts. Its logic differs from SaveClientDemo1var in important ways.

Key differences from SaveClientDemo1var:
- No actionkey parameter — no conditional pre-pass reset mechanism
- No B50x special handling
- AllNewRows flag: set if clients == null. However, a LINQ .Where().ToList() never
  returns null — it always returns an empty list. So AllNewRows is never set to true
  in practice (dead code path).
- Column mapping uses hardcoded direct field access (r["ColumnName"]) instead of
  iterating DataColumns with a switch
- NetAlystic exclusion: sites GB, SOS, and PAWTUCKET skip name, DOB, SSN, address,
  and contact field updates — only City, State, Zip, EmpStatus, Race, County,
  Marital, ClientM4ID, Gender, Preg, and Email are updated for those sites
- Phone field: always calls r["Phone"].ToString().Substring(24, 0) — always empty
  string (same bug as SaveClientDemo1var — see anomalies)
- No RowState management: RowState is never set in this method
- clt.LastModAt = DateTime.Now is set for all changed/new rows (not starttime)
- New rows are added to db.TblClientDemo1.Add(clt) and added to the clients list
  in memory; single db.SaveChanges() at end (NOT per-row commit)
- No correction of RowsUpd vs RowsIns at end

NetAlystic site exclusion (GB, SOS, PAWTUCKET):
These sites use the NetAlystic system (not standard SAMMS), which has a different
schema for patient demographics. The column names or data format for identity fields
differ. The code skips updating: FirstName, MiddleName, LastName, Suffix, DOB, SSN,
Address1, Address2, Phone, PregEDC, Employer, WorkPhone, Income, Education, Hair,
Eye, Height, Weight, Language for these three site codes.
________________________________________

10. SaveClientDemo2 — EF Core Upsert with RowChkSum and RowState (Primary Path)

File: BCAppCode/BHG-DR-LIB/SaveCleints.cs
Class: SaveData (partial class)
Method: SaveClientDemo2()

Method signature:
    public RCodes SaveClientDemo2(DataTable tbl, string sc, int actionkey, BHG_DRContext db)

Returns: RCodes — IsResult, RowsProcessed, ExceptMsg, ExceptInnerMsg
(RowsIns/RowsUpd not separately tracked)

EF Core upsert logic — step by step:

Step 1 — Create EF context
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load existing Azure rows
    List<TblClientDemo2> clients = db.TblClientDemo2.Where(x => x.SiteCode == sc).ToList()

Step 3 — Pre-pass reset (actionkey == 1 only)
    if (actionkey == 1):
        foreach (var s in clients) { s.RowState = 0; }

Note: The AllNewRows check occurs AFTER the pre-pass (line 389), but clients is
loaded before it (line 381). If clients is empty, AllNewRows should be set. However,
LINQ .ToList() never returns null — so AllNewRows is never set to true (dead code,
same as SaveClientDemo1).

Step 4 — Loop through each SAMMS row

    Step 4a — Extract key fields
        int intClient = int.Parse(r["ClientID"].ToString())
        int myrcs     = int.Parse(r["RowChkSum"].ToString())

    Step 4b — Lookup or create entity
        clt = clients.Where(x => x.ClientId == intClient).FirstOrDefault()
        if not found:
            NewRow = true
            clt = new TblClientDemo2 { SiteCode=sc, ClientId=intClient, RowChkSum=myrcs, RowState=1 }

    Step 4c — Null-safe checksum init
        if (clt.RowChkSum == null) { clt.RowChkSum = myrcs; }

    Step 4d — RowChkSum check
        if ((clt.RowChkSum != myrcs) || (NewRow)):
            // map all fields via switch (see column map below)
            clt.LastModAt = DateTime.Now
            clt.RowState  = 1
            if (NewRow || AllNewRows):
                db.TblClientDemo2.Add(clt)
                NewRow = false
        else:
            clt.RowState  = 1
            clt.LastModAt = DateTime.Now

Column mapping (SaveClientDemo2):

    Source Column       Destination         Guard / Transformation
    rowchksum           clt.RowChkSum       int.Parse — always
    counselor           clt.Counselor       Always (string)
    status              clt.Status          Always (string)
    prog                clt.Prog            Always (string)
    dateadded           clt.DateAdded       DateTime — only if length > 8
    amount              clt.Amount          Always (string)
    freq                clt.Freq            Always (string)
    dow1                clt.Dow1            Always (string)
    dow2                clt.Dow2            Always (string)
    nextbill            clt.NextBill        DateTime — only if length > 8
    lastbill            clt.LastBill        DateTime — only if length > 8
    nexttp              clt.NextTp          DateTime — only if length > 8
    phystb              clt.PhysTb          DateTime — only if length > 8
    bottles             clt.Bottles         Int16 — only if length > 0
    monthly             clt.Monthly         bool — only if length > 0
    picpath             clt.Picpath         Always (string)
    rin                 clt.Rin             Always (string)
    eth                 clt.Eth             Always (string)
    medicaid            clt.Medicaid        bool — only if length > 0
    enrolldate          clt.EnrollDate      DateTime — only if length > 8
    bulk                clt.Bulk            bool — only if length > 0
    stand               clt.Stand           bool — only if length > 0
    special             clt.Special         Always (string)
    dtlastua            clt.DtLastUa        Always (string — stored as string, not DateTime)
    amsid               clt.Amsid           Always (string)
    nocensus            clt.Nocensus        bool — only if length > 0
    changeuser          clt.Changeuser      Always (string)
    repoldclient        clt.RepOldClient    decimal — only if length > 0
    uaweekly            clt.Uaweekly        DateTime — only if length > 8
    optin               clt.OptIn           bool — only if length > 0
    credit              clt.Credit          int — only if length > 0
    conttxdt            clt.Conttxdt        DateTime — only if length > 8
    ins                 clt.Ins             Always (string)
    risk                clt.Risk            Always (string)
    clt3pback           clt.Clt3pBack       Always (string)
    clt3pfront          clt.Clt3pfront      Always (string)
    biweeklyua          clt.BiWeeklyUa      bool — only if length > 0
                                            NOTE: switch case reads r["biweeklyua"] (lowercase)
                                            but the DataRow access uses r["biweeklyua"] too —
                                            matched correctly
    nursenotes          clt.NurseNotes      Always (string)
    panel               clt.Panel           Always (string)
    payday              clt.Payday          Always (string)
    fingerprint1        clt.FingerPrint1    byte[] — only if r["FingerPrint1"] != DBNull.Value
    fingerprint2        clt.FingerPrint2    byte[] — only if r["FingerPrint2"] != DBNull.Value
    clt911name          clt.Clt911Name      Always (string)
    clt911ph            clt.Clt911Ph        Always (string)
    clt911relation      clt.Clt911Relation  Always (string)
    salesforceid        clt.SalesForceId    Always (string)
    issalesforcesync    clt.IsSalesForceSync int — only if length > 0
    holidaypickup       clt.HolidayPickup   bool — only if length > 0
    ddapid              clt.Ddapid          long — only if length > 0
    provclient          clt.ProvClient      long — only if length > 0
    provclientid        clt.ProvClientId    long — only if length > 0
    backfee             clt.BackFee         decimal — only if length > 0
    remarks             clt.Remarks         Always (string)

Step 5 — Single commit
    db.SaveChanges()

CRITICAL ANOMALY — Error handler NullReferenceException risk:
    catch (Exception e)
    {
        res.IsResult = false;
        res.ExceptMsg = e.Message;
        res.ExceptInnerMsg = e.InnerException.Message;  ← NO null check!
        ...
    }
If the exception does not have an InnerException, this line throws a
NullReferenceException inside the catch block, hiding the original exception from
the RCodes return value. This is a defect — compare to SaveClientDemo1var which
correctly checks `if (e.InnerException != null)` before accessing it.
________________________________________

11. SaveClientDemo3 — Fingerprint-Only Path (Disabled in Production)

File: BCAppCode/BHG-DR-LIB/SaveCleints.cs
Class: SaveData (partial class)
Method: SaveClientDemo3()

Method signature:
    public bool SaveClientDemo3(DataTable tbl, string sc)    // returns bool, not RCodes

STATUS: Effectively disabled. The call in BHGTaskRunner is commented out:
    case "pats.tbl_clientdemo2":
        if (st.ActionKey == 3)
        {
            //x = SaveClientDemo3(SrcDt, st.SiteCode);  ← COMMENTED OUT
        }

So SaveClientDemo3 is never called in the daily production pipeline.

What SaveClientDemo3 does (if called):
- Opens its own `using (var db = new BHG_DRContext())` — ignores any passed context
- Loads TblClientDemo2 for the SiteCode
- Sets AllNewRows if clients == null (dead code — never true)
- Loops SAMMS rows: creates new entity or looks up by ClientId
- The RowChkSum check block is entirely commented out
- Only the fingerprint block executes:
    if (r.ItemArray.Contains("FingerPrint1")):
        if (r["FingerPrint1"] != DBNull.Value): clt.FingerPrint1 = (byte[])r["FingerPrint1"]
        if (r["FingerPrint2"] != DBNull.Value): clt.FingerPrint2 = (byte[])r["FingerPrint2"]
- Adds new rows to db.TblClientDemo2.Add(clt) if NewRow
- db.SaveChanges() at end
- Returns bool (true = success, false = exception)

ANOMALY — Error handler in SaveClientDemo3 has the same NullReferenceException risk:
    Console.WriteLine(e.InnerException.Message)  ← no null check on InnerException

Use case: SaveClientDemo3 was likely written for a specific fingerprint backfill or
migration scenario where only biometric data needed to be loaded without touching
the operational fields. It writes to TblClientDemo2 (same as SaveClientDemo2), but
with all field logic commented out. Any future use must re-enable the commented-out
RowChkSum guard and field mapping to be meaningful.
________________________________________

12. Destination Tables — Azure BHG_DR (pats schema)
________________________________________

12a. pats.tbl_ClientDemo1

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_ClientDemo1
EF Model: BHG-DR-LIB/Models/TblClientDemo1.cs

Primary Key: SiteCode + ClientId (composite)

C# Property (EF)    SQL Column      Type                Notes
----------------    ---------------  ----                -----
SiteCode            SiteCode        varchar             Clinic identifier
ClientId            ClientId        int                 SAMMS client ID (cltID)
ClientM4id          ClientM4id      varchar             M4 national registry patient ID
FirstName           FirstName       varchar             Patient first name
MiddleName          MiddleName      varchar             Patient middle name/initial
LastName            LastName        varchar             Patient last name
Suffix              Suffix          varchar             Name suffix
Dob                 Dob             datetime (nullable) Date of birth
Gender              Gender          varchar             Gender code
Ssn                 Ssn             varchar             Social Security Number (stored as string)
Email               Email           varchar             Email address
Size                Size            int (nullable)      Clothing size
Address1            Address1        varchar             Street address line 1
Address2            Address2        varchar             Street address line 2
City                City            varchar             City
State               State           varchar             State code
Zip                 Zip             varchar             ZIP code
Phone               Phone           varchar             Primary phone (ALWAYS EMPTY — see anomaly)
Preg                Preg            bit (nullable)      Pregnancy flag
PregEdc             PregEdc         datetime (nullable) Estimated delivery date
Marital             Marital         varchar             Marital status code
EmpStatus           EmpStatus       varchar             Employment status code
Employer            Employer        varchar             Employer name
WorkPhone           WorkPhone       varchar             Work phone number
Income              Income          varchar             Income category
Education           Education       varchar             Education level
Hair                Hair            varchar             Hair color code
Eye                 Eye             Eye color code      Eye color code
Height              Height          varchar             Height
Weight              Weight          varchar             Weight
Race                Race            varchar             Race code
Language            Language        varchar             Primary language code
County              County          varchar             County of residence
RowChkSum           RowChkSum       int (nullable)      Source SQL CHECKSUM for change detection
RowState            RowState        int                 1 = active, 0 = inactive/soft-deleted
LastModAt           LastModAt       datetime (nullable) ETL last write timestamp
________________________________________

12b. pats.tbl_ClientDemo2

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_ClientDemo2
EF Model: BHG-DR-LIB/Models/TblClientDemo2.cs

Primary Key: SiteCode + ClientId (composite)

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------      ----                -----
SiteCode            SiteCode            varchar             Clinic identifier
ClientId            ClientId            int                 SAMMS client ID
Counselor           Counselor           varchar             Assigned counselor name/code
Status              Status              varchar             Treatment status code
Prog                Prog                varchar             Program code
DateAdded           DateAdded           datetime (nullable) Enrollment/admit date
Amount              Amount              varchar             Fee amount (stored as string)
Freq                Freq                varchar             Billing frequency code
Dow1                Dow1                varchar             Dosing day-of-week 1
Dow2                Dow2                varchar             Dosing day-of-week 2
NextBill            NextBill            datetime (nullable) Next billing date
LastBill            LastBill            datetime (nullable) Last billing date
NextTp              NextTp              datetime (nullable) Next treatment plan date
PhysTb              PhysTb              datetime (nullable) Next physician TB test date
Bottles             Bottles             smallint (Int16)    Take-home bottle count
Monthly             Monthly             bit (nullable)      Monthly billing flag
Picpath             Picpath             varchar             Patient photo file path
Rin                 Rin                 varchar             State registry ID
Eth                 Eth                 varchar             Ethnicity code
Medicaid            Medicaid            bit (nullable)      Medicaid coverage flag
EnrollDate          EnrollDate          datetime (nullable) Enrollment date
Bulk                Bulk                bit (nullable)      Bulk take-home flag
Stand               Stand               bit (nullable)      Standing order flag
Special             Special             varchar             Special notes
DtLastUa            DtLastUa            varchar             Last UA date (string, not datetime)
Amsid               Amsid               varchar             State ASAM ID
Nocensus            Nocensus            bit (nullable)      Exclude from census flag
Changeuser          Changeuser          varchar             Last modified user
RepOldClient        RepOldClient        decimal (nullable)  Replaced old client ID
Uaweekly            Uaweekly            datetime (nullable) Next weekly UA date
OptIn               OptIn               bit (nullable)      Opt-in consent flag
Credit              Credit              int (nullable)      Credit balance
Conttxdt            Conttxdt            datetime (nullable) Continued treatment date
Ins                 Ins                 varchar             Insurance code
Risk                Risk                varchar             Risk level
Clt3pBack           Clt3pBack           varchar             Insurance card back image path
Clt3pfront          Clt3pfront          varchar             Insurance card front image path
BiWeeklyUa          BiWeeklyUa          bit (nullable)      Bi-weekly UA flag
NurseNotes          NurseNotes          varchar             Nurse notes
Panel               Panel               varchar             Panel/group assignment
Payday              Payday              varchar             Payday indicator
FingerPrint1        FingerPrint1        varbinary           Fingerprint 1 biometric image (byte[])
FingerPrint2        FingerPrint2        varbinary           Fingerprint 2 biometric image (byte[])
Clt911Name          Clt911Name          varchar             Emergency contact name
Clt911Ph            Clt911Ph            varchar             Emergency contact phone
Clt911Relation      Clt911Relation      varchar             Emergency contact relationship
SalesForceId        SalesForceId        varchar             Salesforce CRM record ID
IsSalesForceSync    IsSalesForceSync    int (nullable)      Salesforce sync status
HolidayPickup       HolidayPickup       bit (nullable)      Holiday pickup flag
Ddapid              Ddapid              bigint (nullable)   DDAP state agency ID
ProvClient          ProvClient          bigint (nullable)   Provider client ID
ProvClientId        ProvClientId        bigint (nullable)   Provider client reference ID
BackFee             BackFee             decimal (nullable)  Back fee balance
Remarks             Remarks             varchar             General remarks
RowChkSum           RowChkSum           int (nullable)      Source SQL CHECKSUM
RowState            RowState            int                 1 = active, 0 = inactive
LastModAt           LastModAt           datetime (nullable) ETL last write timestamp
________________________________________

13. RowChkSum — Change Detection

Both primary production methods (SaveClientDemo1var and SaveClientDemo2) use
RowChkSum-based change detection. RowChkSum is a SQL Server CHECKSUM() value computed
at source over a defined set of columns and retrieved alongside the data row.

For Demo1: covers identity/demographic columns
For Demo2: covers operational/status columns

Behavior:
- New row (not found in Azure): RowChkSum set to 0, then immediately checked → always triggers field mapping
- Existing row, checksum changed: all fields overwritten, LastModAt updated
- Existing row, checksum unchanged: only RowState=1 restored; fields not re-mapped;
  LastModAt NOT updated in SaveClientDemo1var (commented out); LastModAt IS updated in SaveClientDemo2

In SaveClientDemo1var, RowChkSum is initialised to 0 for new rows specifically so
that the `clt.RowChkSum != rcs` condition is guaranteed to be true, forcing the field
mapping block to execute for all new records.
________________________________________

14. RowState — Active / Inactive Tracking

RowState in both Demo1 and Demo2 is an int (not a bool):
    1 = active (patient is in current SAMMS extract for this site)
    0 = inactive (patient was not seen in SAMMS extract; possibly discharged or outside window)

How RowState flows in SaveClientDemo1var:

With ActionKey=1 (reload):
1. Pre-pass: ALL existing records for this site set to RowState=0
2. Each SAMMS row processed: RowState=1 restored
3. Records not in current SAMMS extract remain RowState=0 → soft-deleted

Without ActionKey=1 (normal daily run):
1. No pre-pass reset
2. Each SAMMS row that triggers a checksum change: RowState=1 set
3. Each SAMMS row with unchanged checksum: RowState=1 set
4. Records not in current extract: RowState unchanged (whatever it was before)

For B50x sites (regardless of actionkey):
1. Pre-pass: only records matching the current SiteCode (not all B50*) reset to RowState=0
2. Normal processing follows for returned SAMMS rows

How RowState flows in SaveClientDemo2:

Same pattern as SaveClientDemo1var with ActionKey parameter.
Both checksum-changed and checksum-unchanged paths set RowState=1.
________________________________________

15. Load Design Summary

Load type: Incremental upsert with RowChkSum change detection and RowState tracking

Per run behavior for pats.tbl_ClientDemo1 (SaveClientDemo1var):

  Source query: controlled by st.WhereCondition + DaysBack + st.SortOrder
  1. Load existing Azure Demo1 rows (B50x: all B50* records; others: this SiteCode only)
  2. If ActionKey=1: pre-pass sets all site records to RowState=0
     If B50x: pre-pass sets only current SiteCode rows to RowState=0
  3. For each SAMMS source row:
     - Parse ClientId + RowChkSum
     - Lookup by SiteCode + ClientId
     - Not found → insert inline (per-row db.SaveChanges), RowChkSum=0 to force field map
     - Found, checksum changed → update all fields, RowState=1, RowChkSum updated
     - Found, checksum unchanged → RowState=1 only, no field update
  4. db.SaveChanges() — commit all remaining updates
  5. Correct RowsUpd = RowsUpd - RowsIns

Per run behavior for pats.tbl_ClientDemo2 (SaveClientDemo2):

  Same pattern as above but:
  - No B50x special handling
  - AllNewRows is dead code (LINQ never returns null)
  - New rows are added via db.TblClientDemo2.Add(clt) inside the loop
  - Single db.SaveChanges() at end covers all inserts and updates
________________________________________

16. Error Handling and Recovery

SaveClientDemo1var:
    try { ... }
    catch (Exception e)
    {
        res.IsResult = false
        res.ExceptMsg = e.Message
        Console.WriteLine(e.Message)
        if (e.InnerException != null)    // correctly null-checked
        {
            res.ExceptInnerMsg = e.InnerException.Message
            Console.WriteLine(e.InnerException.Message)
        }
    }

SaveClientDemo2 (DEFECTIVE):
    catch (Exception e)
    {
        res.IsResult = false;
        res.ExceptMsg = e.Message;
        res.ExceptInnerMsg = e.InnerException.Message;  ← NO null check
        Console.WriteLine(e.Message);
        Console.WriteLine(e.InnerException.Message);    ← NO null check
    }
If InnerException is null, this catch block itself throws a NullReferenceException,
overwriting the original error and corrupting the RCodes state.

SaveClientDemo3 (DEFECTIVE — same pattern as SaveClientDemo2):
    catch (Exception e)
    {
        res = false;
        Console.WriteLine(e.Message);
        Console.WriteLine(e.InnerException.Message);    ← NO null check
    }

Recovery:
The Scheduler's daily reset restores failed tasks:
    update tsk.tbl_Tasks set Status = 17 where Status = 18
Failed Demo1 or Demo2 tasks are retried the next day.
________________________________________

17. RowTrax — Audit and Row Count Tracking

RowTrax is enabled for pats.tbl_clientdemo1 tasks (if st.RowTrax = true and
SiteCode != "PHC"). BHGTaskRunner fires after SaveClientDemo1var:

Source count:
    count(1) from {st.SrcSchema}.{st.FromTblVw}
    + additional filter if FromTblVw == "vw_Clients":   where AddressType = 2
    + additional filter if FromTblVw == "ClientDemo":   where SiteCode = '{sc}' and Patient_UID is not null

Destination count:
    count(1) from pats.tbl_clientdemo1 where SiteCode = '{sc}' and RowState = 1

Stored in tsk.tbl_RowTrax. RowTrax is not configured for pats.tbl_clientdemo2 tasks
in the BHGTaskRunner code.
________________________________________

18. Key Design Notes and Gotchas

Phone field always stores empty string (SaveClientDemo1var AND SaveClientDemo1):
    case "phone":
        if (r[c.ColumnName].ToString().Length > 24)
        {
            clt.Phone = r[c.ColumnName].ToString().Substring(24, 0);  // BUG
        }
        else
        {
            clt.Phone = r[c.ColumnName].ToString();
        }

The Substring(startIndex, length) call uses length=0. String.Substring(24, 0) always
returns an empty string regardless of what the phone number actually is. This means
any patient whose phone number is longer than 24 characters will have an empty Phone
field stored in Azure. In practice, valid phone numbers (e.g. "(555) 123-4567 ext 789")
can exceed 24 characters. The correct call would be Substring(0, 24) to take the first
24 characters, or just assign the raw string without truncation.
In SaveClientDemo1 (legacy), Phone is always assigned as:
    clt.Phone = r["Phone"].ToString().Substring(24, 0)
...without the length check — so it is ALWAYS empty regardless of length.

AllNewRows is dead code (SaveClientDemo1, SaveClientDemo2, SaveClientDemo3):
The check `if (clients == null) { AllNewRows = true; }` is placed after a LINQ
.Where().ToList() call. EF Core's ToList() always returns a List<T>, never null —
even for an empty result set it returns an empty list. AllNewRows can never be set
to true, so the AllNewRows-based code path for pre-populating new rows is unreachable
dead code in all three affected methods.

Per-row commit for new rows in SaveClientDemo1var:
New rows are inserted and committed immediately:
    db.TblClientDemo1.Add(clt)
    db.SaveChanges()    ← inside the foreach loop
This is a per-row commit pattern — one database transaction per new client. For
large initial loads (hundreds of new patients in one run), this can be significantly
slower than batching inserts. SaveClientDemo2 does NOT have this pattern — it adds
all new rows during the loop and commits once at the end.

RowsUpd correction at end of SaveClientDemo1var:
    res.RowsUpd -= res.RowsIns
Because new rows trigger the checksum check branch (RowChkSum=0 != rcs always),
RowsUpd is incremented for new rows as well as genuine updates. The subtraction at
the end corrects for this double-count. SaveClientDemo2 does not implement this
correction.

LastModAt commented out in SaveClientDemo1var (checksum-unchanged path):
    else
    {
        clt.RowState = 1;
        //clt.LastModAt = DateTime.Now;   ← COMMENTED OUT
        res.RowsUpd++;
    }
When a record's data has not changed (same checksum), the LastModAt timestamp is not
updated. Only RowState is refreshed. This means the LastModAt field in Azure shows
the last time data actually changed, not the last time the ETL verified the record.
The SaveClientDemo2 checksum-unchanged path DOES update LastModAt.

B50x shared-prefix site handling (SaveClientDemo1var):
B50x clinic codes (B50A, B50B, etc.) represent affiliated clinics that may have
patients appearing in multiple sites. Loading ALL B50* records into memory prevents
duplicate insert errors when a patient exists under a slightly different SiteCode.
The RowState=0 pre-pass is scoped to only the specific SiteCode being processed,
not all B50* sites, to avoid marking records from sibling clinics as inactive.

NetAlystic site exclusion (SaveClientDemo1 legacy only):
Sites GB, SOS, and PAWTUCKET use the NetAlystic EMR system. Their source data
schema differs from SAMMS. SaveClientDemo1 explicitly excludes name, DOB, SSN, and
address field updates for these sites, mapping only the fields that NetAlystic
provides in a compatible format. SaveClientDemo1var does NOT have this exclusion
and uses the same column switch for all sites.

SaveClientDemo3 has all field mapping commented out:
The entire RowChkSum check + field assignment block is commented out in
SaveClientDemo3. The method was probably written for a targeted fingerprint backfill
operation and was subsequently disabled. Its only active logic is the fingerprint
byte-array copy. It should not be called with the expectation of populating Demo2
operational fields.

Duplicate task with stg.clientdemo (Bulk path):
Some clinics have their client demo task configured as `stg.clientdemo` instead of
`pats.tbl_clientdemo1`. This routes through BulkDartsSrvLoader (BulkDartsSvc.cs) —
a SqlBulkCopy path into a staging table followed by a merge stored procedure.
These clinics do NOT call SaveClientDemo1var or SaveClientDemo2. The stg.clientdemo
case in BHGTaskRunner constructs a full hardcoded SELECT from dbo.tblClient. There
is no flag in the data model to distinguish which path a given site uses — it is
controlled purely by which TaskName value is configured in tsk.tbl_Tasks.

File name typo:
SaveCleints.cs — "Cleints" is a transposition of "Clients". The file name is fixed
in the codebase. All references to this file should use the typo-preserved name.
________________________________________

19. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, timezone-staggered)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert 4 parent tasks (one per timezone):
        |       Eastern ETL P1  / Central ETL P1 / Mountain ETL P1 / Pacific ETL P1
        |-- insert child tasks per clinic:
        |       pats.tbl_clientdemo1   x 80+ clinics (EF Core path)
        |       pats.tbl_clientdemo2   x 80+ clinics (EF Core path)
        |       stg.clientdemo         x selected clinics (Bulk path — not SaveCleints.cs)
        |-- advance NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        | (when RunAt time arrives for each timezone)
        V
BHGTaskRunner.exe 2
        |
        |-- filter: TaskName in (Central/Eastern/Mountain/Pacific ETL P1)
        |            SiteCode != 'PHC', Status=17
        |-- mark parent task Status=18 (running)
        |
        |-- for each child task (one per clinic per table type):
        |
        |   Build strCmd via SelectConstructor + WhereCondition
        |
        |======================================================
        |  BRANCH A: TaskName = pats.tbl_clientdemo1
        |======================================================
        |   strCmd += " Where " + strWhere + " " + st.SortOrder
        |   SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |           |
        |           V
        |   SaveClientDemo1var(SrcDt, st.SiteCode, st.ActionKey, null)
        |           |
        |     |-- Load TblClientDemo1 (B50x: all B50* / others: this SiteCode)
        |     |-- If ActionKey=1: pre-pass RowState=0 for all site rows
        |     |   If B50x: pre-pass RowState=0 only for this SiteCode
        |     |-- Loop SAMMS rows:
        |     |       Parse ClientId + RowChkSum
        |     |       Lookup by SiteCode + ClientId
        |     |       not found → Add + SaveChanges() (per-row commit); RowChkSum=0
        |     |       checksum changed → map all fields, RowState=1, RowChkSum updated
        |     |       checksum unchanged → RowState=1 only
        |     |-- db.SaveChanges() (batch commit all updates)
        |     |-- res.RowsUpd -= res.RowsIns (correct double-count)
        |           |
        |           V
        |       pats.tbl_ClientDemo1 (Azure BHG_DR)
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != PHC):
        |       source count = count from SAMMS source view
        |       dest count   = count from pats.tbl_clientdemo1 where SiteCode=sc and RowState=1
        |       → tsk.tbl_RowTrax
        |
        |======================================================
        |  BRANCH B: TaskName = pats.tbl_clientdemo2
        |======================================================
        |   if (st.ActionKey == 3): → SKIPPED (SaveClientDemo3 call commented out)
        |   else:
        |   strCmd += " Where " + strWhere + " " + st.SortOrder
        |   SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |           |
        |           V
        |   SaveClientDemo2(SrcDt, st.SiteCode, st.ActionKey, null)
        |           |
        |     |-- Load TblClientDemo2 where SiteCode = sc
        |     |-- If ActionKey=1: pre-pass RowState=0 for all site rows
        |     |-- Loop SAMMS rows:
        |     |       Lookup by ClientId
        |     |       not found → new entity, RowState=1 (staged for AddRange)
        |     |       checksum changed → map all fields via switch, RowState=1
        |     |       checksum unchanged → RowState=1, LastModAt updated
        |     |-- db.SaveChanges() (single commit)
        |           |
        |           V
        |       pats.tbl_ClientDemo2 (Azure BHG_DR)
        |
        |======================================================
        |  BRANCH C: TaskName = stg.clientdemo (Bulk path — not SaveCleints.cs)
        |======================================================
        |   Hardcoded SELECT from dbo.tblClient
        |   → BulkDartsSrvLoader → stg.clientdemo staging table → stg.ClientDemoMerge SP
        |   (documented separately in BulkDartsSvc.cs documentation)
        |
        V
BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)
________________________________________

20. File Reference Map

File Path                                           Purpose
---------                                           -------
BCAppCode/Scheduler/Program.cs                      Creates daily task queue — P1/P2 timezone parent tasks
BCAppCode/BHGTaskRunner/Program.cs                  Main ETL driver (arg=2/4)
                                                    pats.tbl_clientdemo1 case ~line 675
                                                    pats.tbl_clientdemo2 case ~line 706
                                                    stg.clientdemo case   ~line 657
BCAppCode/BHG-DR-LIB/SaveCleints.cs                SaveClientDemo1var, SaveClientDemo1, SaveClientDemo2, SaveClientDemo3
BCAppCode/BHG-DR-LIB/BulkDartsSvc.cs              BulkDartsSrvLoader — handles stg.clientdemo bulk copy path
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs              ADO.NET wrapper — executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblClientDemo1.cs      EF Model → pats.tbl_ClientDemo1
BCAppCode/BHG-DR-LIB/Models/TblClientDemo2.cs      EF Model → pats.tbl_ClientDemo2
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs       EF DbContext — TblClientDemo1, TblClientDemo2 DbSets
________________________________________

21. Quick Reference Summary

What triggers Client Demo ETL?          Scheduler creates tasks, BHGTaskRunner.exe 2 (P1) processes them
Schedule?                                Schedule 2 — Regional ETL P1 (4 timezone parent tasks)
                                         Also Schedule 4 — Regional ETL P2 for selected tasks
Methods in SaveCleints.cs?              4 total:
                                         SaveClientDemo1var (primary daily — Demo1)
                                         SaveClientDemo1 (legacy — Demo1, not in daily pipeline)
                                         SaveClientDemo2 (primary daily — Demo2)
                                         SaveClientDemo3 (disabled — Demo2, fingerprint only)
Source table in SAMMS?                  dbo.tblClient (or clinic view via st.FromTblVw)
Destination tables?                     pats.tbl_ClientDemo1 (identity/demographics)
                                         pats.tbl_ClientDemo2 (operational/clinical status)
EF Core or Bulk?                        EF Core for pats.tbl_clientdemo1 and pats.tbl_clientdemo2 tasks
                                         Bulk copy for stg.clientdemo tasks (BulkDartsSvc.cs)
Primary keys?                            Both tables: SiteCode + ClientId (composite)
Change detection?                        RowChkSum — yes in SaveClientDemo1var and SaveClientDemo2
RowState?                                int: 1=active, 0=inactive. Pre-pass reset when ActionKey=1
RowState type?                           int (not bool — different from SaveDoses where RowState is bool)
B50x special handling?                   Yes — SaveClientDemo1var loads all B50* records in memory
                                         to handle cross-site patient lookup
ActionKey=1 meaning?                     Triggers full pre-pass RowState=0 reset before processing
NetAlystic exclusion?                    SaveClientDemo1 (legacy only) skips identity fields for GB, SOS, PAWTUCKET
Phone field bug?                         YES — Substring(24, 0) always returns empty string
                                         Long phone numbers stored as empty in pats.tbl_ClientDemo1
AllNewRows dead code?                    YES — in SaveClientDemo1, SaveClientDemo2, SaveClientDemo3
                                         LINQ .ToList() never returns null; AllNewRows never true
Per-row commit?                          SaveClientDemo1var only — new rows committed one-at-a-time
                                         (performance concern for large initial loads)
Demo2 error handler bug?                 SaveClientDemo2 + SaveClientDemo3 — no null check on
                                         e.InnerException; can throw NullReferenceException in catch block
SaveClientDemo3 status?                  Disabled — call is commented out in BHGTaskRunner
                                         Only fingerprints are loaded; all other field mapping commented out
RowTrax audit?                           Demo1 only — source count vs active (RowState=1) dest count
                                         Supports special count filters for vw_Clients and ClientDemo views
File name note?                          "SaveCleints.cs" is a deliberate typo in the original codebase
                                         ("Cleints" vs "Clients"); preserved as-is
________________________________________

Documentation generated from source: BHG-DR-LIB\SaveCleints.cs (736 lines, 4 methods).
Parent Schedule: Regional ETL P1 / P2 (Schedules 2 and 4 — BHGTaskRunner.exe 2/4)
Destination tables: pats.tbl_ClientDemo1 (identity), pats.tbl_ClientDemo2 (operational).


Method 1 — SaveClientDemo1var

Field	Value
Name	SaveClientDemo1var
Module	Client demographic identity load — primary daily path
Layer	Target load / EF Core upsert with per-row commit for new records
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr where TaskName = pats.tbl_clientdemo1
Source table	dbo.tblClient (or clinic view via st.FromTblVw); column list + RowChkSum from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_ClientDemo1
Load type	EF Core upsert — dynamic column switch; lookup key: SiteCode + ClientId (composite); RowChkSum guards updates; per-row db.SaveChanges() for new records; B50x cross-site patient pre-load; ActionKey=1 triggers full RowState=0 pre-pass reset
Load type column	RowChkSum — effective change detection; RowState (int 1=active, 0=inactive); Phone uses Substring(24, 0) — always returns empty string (bug); LastModAt not updated on checksum-unchanged path
Frequency	Daily
Schedule	Schedule 2 — BHGTaskRunner.exe 2 (Regional ETL P1)
Parent	Central / Eastern / Mountain / Pacific ETL P1
Downstream	pats.tbl_ClientDemo1 → patient identity master; claim generation; reporting
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveClientDemo1var(SrcDt, st.SiteCode, st.WorkDate, st.ActionKey, null)
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveCleints.cs
Status	Active — primary daily path
Folder	BHG-DR-LIB\SaveCleints.cs; detail in SaveCleints-Documentation\SaveCleints_ETL_Complete_Documentation.md
Known anomalies	Phone field: Substring(24, 0) always stores empty string; LastModAt not updated when RowChkSum unchanged; per-row commit for new rows (performance risk on large loads); B50x loads all B50* records into memory
Method 2 — SaveClientDemo1

Field	Value
Name	SaveClientDemo1
Module	Client demographic identity load — legacy path
Layer	Target load / EF Core upsert (not in active daily pipeline)
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr
Source table	dbo.tblClient (or clinic view via st.FromTblVw)
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_ClientDemo1
Load type	EF Core upsert — dynamic column switch; lookup key: SiteCode + ClientId; RowChkSum guards updates; NetAlystic site exclusion skips identity fields for GB, SOS, PAWTUCKET sites; AllNewRows flag present but dead code
Load type column	RowChkSum — effective change detection; RowState (int); Phone uses same Substring(24, 0) bug — always empty; AllNewRows never true (LINQ .ToList() never returns null)
Frequency	Not in active daily pipeline — legacy method
Schedule	Schedule 2 — BHGTaskRunner.exe 2 (Regional ETL P1) — if configured
Parent	Central / Eastern / Mountain / Pacific ETL P1
Downstream	pats.tbl_ClientDemo1 → patient identity master
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: called only if routed explicitly
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHG-DR-LIB\SaveCleints.cs
Status	Legacy — not in primary daily dispatch
Folder	BHG-DR-LIB\SaveCleints.cs; detail in SaveCleints-Documentation\SaveCleints_ETL_Complete_Documentation.md
Known anomalies	Phone field: Substring(24, 0) always stores empty string; AllNewRows is dead code; NetAlystic exclusion skips identity fields for specific sites
Method 3 — SaveClientDemo2

Field	Value
Name	SaveClientDemo2
Module	Client operational / clinical status load — primary daily path
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr where TaskName = pats.tbl_clientdemo2
Source table	dbo.tblClient (or clinic view via st.FromTblVw); column list + RowChkSum from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_ClientDemo2
Load type	EF Core upsert — dynamic column switch; lookup key: SiteCode + ClientId; RowChkSum guards updates; new rows collected in list, committed via AddRange; single db.SaveChanges() for updates; AllNewRows dead code
Load type column	RowChkSum — effective change detection; RowState (int 1=active, 0=inactive); AllNewRows never true (LINQ .ToList() never returns null)
Frequency	Daily
Schedule	Schedule 2 / 4 — BHGTaskRunner.exe 2/4 (Regional ETL P1 / P2)
Parent	Central / Eastern / Mountain / Pacific ETL P1 / P2
Downstream	pats.tbl_ClientDemo2 → operational/clinical status; treatment status reporting; billing eligibility
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveClientDemo2(SrcDt, st.SiteCode, st.WorkDate, null)
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveCleints.cs
Status	Active — primary daily path
Folder	BHG-DR-LIB\SaveCleints.cs; detail in SaveCleints-Documentation\SaveCleints_ETL_Complete_Documentation.md
Known anomalies	AllNewRows is dead code; e.InnerException accessed without null check in catch block — throws NullReferenceException if InnerException is null, masking original error
Method 4 — SaveClientDemo3

Field	Value
Name	SaveClientDemo3
Module	Client fingerprint / biometric reference load
Layer	Target load / EF Core upsert — disabled in production
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.ConStr
Source table	dbo.tblClient (or clinic view)
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_ClientDemo2
Load type	EF Core upsert — fingerprint fields only; all non-fingerprint field mappings commented out; AllNewRows dead code; call to this method is commented out in BHGTaskRunner
Load type column	RowChkSum present in source but mappings commented out; only fingerprint/biometric columns mapped; AllNewRows never true; e.InnerException accessed without null check
Frequency	Not in production — disabled
Schedule	Was Schedule 2 — BHGTaskRunner.exe 2 — call commented out
Parent	Central / Eastern / Mountain / Pacific ETL P1
Downstream	None in production — method is effectively dead code
Connection / method	sd.SaveClientDemo3(...) call is commented out in BHGTaskRunner/Program.cs
Server / DB / API	Source: st.ConStr (clinic SAMMS). Target: Azure BHG_DR via BHG_DRContext
Owner	BHG-DR-LIB\SaveCleints.cs
Status	Disabled — call commented out in BHGTaskRunner; field mappings also commented out internally
Folder	BHG-DR-LIB\SaveCleints.cs; detail in SaveCleints-Documentation\SaveCleints_ETL_Complete_Documentation.md
Known anomalies	Entire method disabled in production; all field mappings except fingerprint are commented out; AllNewRows is dead code; e.InnerException null check missing in catch block
