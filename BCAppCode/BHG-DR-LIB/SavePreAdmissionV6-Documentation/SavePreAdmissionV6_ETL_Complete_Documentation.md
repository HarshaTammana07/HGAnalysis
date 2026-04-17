
Pre-Admission ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 8 — SAMMS-ETL-INV (primary) / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract pre-admission intake
records and referral source records from local SAMMS SQL Server databases at each clinic and
load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What pre-admission and referral source data are and why they exist
- What systems and files are involved from start to finish
- How BHGTaskRunner dispatches and pre-validates these tasks
- The three-layer pre-check in BHGTaskRunner before SavePreAdmissionV6 is called
- The hardcoded source SELECT (not metadata-driven) and how boolean fields are transformed
- How SavePreAdmissionV6 builds a fresh object per row and performs composite-key lookup
- The disabled RowChkSum guard and its consequence
- The column-order dependency in RowState and EtllastModAt stamping
- How SavePreAdminReferrals handles dual data types for boolean fields
- The extended 500-day lookback for referral source records
- All known anomalies, bugs, and design notes

There are two methods in SavePreAdmissionV6.cs spanning two Azure destination tables:

ayx.tbl_preadmission_v6:          SavePreAdmissionV6     (pre-admission intake records)
pats.tbl_PreAdmissionReferralSource: SavePreAdminReferrals  (referral source detail records)
________________________________________

2. High-Level Business Summary

What is pre-admission data?

A pre-admission intake is the first structured interaction with a prospective patient before
they are formally admitted into a treatment program. Captured via the SAMMS form
SF_PatientPreAdmission (V6 schema), the record collects the patient's pre-admission date,
registration mode (phone/walk-in/by appointment), referral source, program requested, current
clinical screening answers (opiate program membership, pain management, legal prescriptions,
ongoing medical conditions, suicidal/homicidal thoughts within 72 hours, recent penal release,
special accommodation needs), reason for seeking treatment, patient demographics, immediate
assessment triage notes, medical provider contacts, and a signed patient date.

Because the source stores most screening answers as bit integers (0/1), BHGTaskRunner
converts them to human-readable strings ('Yes'/'No'/'Unknown') using CASE expressions in the
hardcoded SELECT before the data reaches the C# method. This means the EF model properties
for these fields are VARCHAR, not bool.

pats.tbl_PreAdmissionReferralSource — Referral Source Detail Records
This table captures the detailed referral source information attached to a pre-admission
record. It records primary and secondary referral sources, referral source notes, referral
organization and contact name/ID, enrollment link, program, readmit flag, delete flag,
and narrative fields for why the patient left BHG treatment and why they are returning. It
was added to the pipeline on 9-12-2025 and uses a separate table existence pre-check.

Why this data is important
- Pre-admission data is the earliest clinical record for a patient — it captures initial
  screening answers that inform triage and admission decisions
- Suicidal and homicidal thought screening fields (within 72 hours) are safety-critical —
  accurate ETL is required for risk tracking
- The referral source data supports marketing attribution, outreach effectiveness reporting,
  and readmission tracking
- The `IsDeleted` flag in the source drives `RowState` in Azure — records deleted in SAMMS
  are soft-deleted in the warehouse

Load type
Both methods use an EF Core two-phase upsert (updates via change tracking, inserts via
AddRange). SavePreAdmissionV6 uses a composite key (PreAdmissionid + Clientid) but has its
RowChkSum guard commented out — every found record is always fully overwritten. SavePreAdminReferrals
uses a single key (Id) and has no RowChkSum at all — also fully overwrites on every run.
________________________________________

3. Systems Involved

System / File                                Role
-----------                                  ----
tsk.tbl_Schedule (Azure DB)                  Configuration — defines schedules and run times
Scheduler.exe                                Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=8                      Main ETL orchestrator for SAMMS-ETL-INV
BHGTaskRunner.exe arg=3                      Catch-all schedule — also processes these tasks
ctrl.tbl_LocationCons (Azure DB)             Connection strings for each clinic SAMMS SQL Server
SQLSvrManager.cs                             Fires SELECT and sys.tables probes against SAMMS
SavePreAdmissionV6.cs (BHG-DR-LIB)          2 methods for pre-admission and referral data
Models/TblPreAdmissionV6.cs                  EF entity → ayx.tbl_preadmission_v6
Models/TblPreAdmissionReferralSource.cs      EF entity → pats.tbl_PreAdmissionReferralSource
ayx.tbl_preadmission_v6 (Azure BHG_DR)     Final destination for pre-admission V6 records
pats.tbl_PreAdmissionReferralSource (Azure BHG_DR)  Final destination for referral source records
tsk.tbl_RowTrax (Azure DB)                  Audit log — not used by either method
________________________________________

4. Scheduler — How Pre-Admission Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task (schedule depends on task configuration)

Step 3 — Insert child tasks per clinic
For each active clinic, child tasks are inserted:
    TaskName = 'ayx.tbl_preadmission_v6'           SiteCode = 'B01A'
    TaskName = 'pats.tbl_preadmissionreferralsource' SiteCode = 'B01A'
    ... (one row per table per clinic)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How Pre-Admission Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner processes these tasks as part of the SAMMS-ETL-INV schedule (arg=8) or the
catch-all schedule (arg=3). The inner switch dispatches by TaskName.

CASE: ayx.tbl_preadmission_v6

Three-layer pre-validation before any data is loaded:

LAYER 1 — Table existence check:
    SrcDt = sm.GetTableData(st.FromTblVw,
        "select name from sys.tables t where upper(name) = 'SF_PatientPreAdmission'",
        st.ConStr)
    if (SrcDt.Rows.Count != 1) → skip: rCodes.ExceptMsg = "Table does not exists or SAMMS Version 5."
    Only clinics with a SF_PatientPreAdmission table in their SAMMS database proceed.

LAYER 2 — Schema version check:
    if (st.SchemaVersion == "V5") → skip: same message as above
    Clinics running SAMMS V5 are excluded — this method targets V6 schema only.

LAYER 3 — Column existence check (clientaddress column):
    tblCols = sm.GetTableData("Cols",
        "select name, column_id from sys.all_columns where object_id = (select object_id
        from sys.tables where upper(name) = 'SF_PatientPreAdmission')",
        st.ConStr)
    if lstCols.Where(x => x.ColName.ToLower() == "clientaddress") == null
        → rCodes.IsResult = false; rCodes.ExceptMsg = "Column ClientAddress does not exists."
    Ensures the V6 schema is complete before proceeding.

All three layers must pass. Only then is the data SELECT executed.

Hardcoded SELECT (not metadata-driven):
BHGTaskRunner builds the SELECT in code — it is not generated by SelectConstructor from
dms.tbl_MapSrc2Dsn. The query joins:
    SF_PatientPreAdmission PP
    left join SF_Program pg       on pp.ProgramID = pg.id
    left join tblCodes tc         on pp.ReferralSourceID = tc.cdeID
    left join tblCodes tc2        on pp.SammsProgramID = tc2.cdeID
    left join tblClient clt       on pp.PatientID = clt.cltID

Boolean-to-string CASE transformations in SELECT (bit → 'Yes'/'No'):
    IsCurrentlyInOpiateProgram, IsPatientAtPainManagementClinic, IsHavingLegalPrescription,
    IsAnyLegalPrescriptionForPain, IsAnyOngoingMedicalCondition, IsSuicidalThoughtWithin72Hours,
    IsHavingPlanForHowToCommitSuicide, IsHomicidalThoughtWithin72Hours, IsRecentlyReleasedFromPenal,
    IsSpecialAccommodationRequired, IsPatientAdmitted, BringIDProof, BringInsuranceCard,
    ClinicInfo, CurrntlyRecevingTreatmentForCondition, IsAnyPrescriptionForPain, IsInsurance,
    isOverTheCounterMedications, PlanOfSuicide, PlanOnSpendingTimeAtClinic
    All: 1='Yes', 0='No', else cast as varchar

    AreYouCurrentlyPregnant (special — reversed convention):
        0='Yes', 1='No', 2='Unknown'
    Note: 0 maps to 'Yes' (pregnant) and 1 maps to 'No' — reversed from the standard
    bit convention. See Anomaly 3.

    RegistrationMode (integer → text):
        0='Phone', 1='Walk-In', 2='By Appointment', else cast as varchar

RowChkSum built in SELECT:
    CHECKSUM(pp.id, pp.PatientID, pp.LastUpdatedBy, pp.LastUpdateOn, pp.PatientSignatureDate,
             pp.DateofRelease, pp.Version, pp.IsDeleted, clt.cltM4ID)

WHERE clause applied after hardcoded SELECT:
    strCmd += " Where " + strWhere + " " + st.SortOrder
    Standard rolling lookback (DaysBack=-15) from task WhereCondition.

Call:
    rCodes = sd.SavePreAdmissionV6(SrcDt, st.SiteCode, null)
No RowTrax logging for this case.

CASE: pats.tbl_preadmissionreferralsource (added 9-12-2025)

Table existence pre-check (single layer):
    RefSrctbl = sm.GetTableData("RefSrc",
        "select object_id from sys.all_objects where upper(name) = '" + st.FromTblVw.ToUpper() + "'",
        st.ConStr)
    if RefSrctbl.Rows.Count != 1 → skip: rCodes.IsResult = true

Extended lookback:
    int mydaysback = DaysBack - 500     (DaysBack=-15, so mydaysback=-515 ≈ 17 months)
    strCmd += " Where " + strWhere.Replace("@WorkDate", WorkDate.AddDays(-515))

Call (only if SrcDt.Rows.Count > 0):
    rCodes = sd.SavePreAdminReferrals(SrcDt, st.SiteCode, WorkDate.AddDays(-515), null)
No RowTrax logging for this case.
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

For SavePreAdmissionV6, SQLSvrManager is used THREE times per site before the main fetch:
    1. Table existence probe (SF_PatientPreAdmission in sys.tables)
    2. Column metadata probe (sys.all_columns for SF_PatientPreAdmission)
    3. Main data SELECT (the full hardcoded JOIN query)

For SavePreAdminReferrals, it is used TWICE:
    1. Table existence probe (sys.all_objects for the source view/table)
    2. Main data SELECT

Source names:
    dbo.SF_PatientPreAdmission    → ayx.tbl_preadmission_v6  (V6 schema, joined via BHGTaskRunner SELECT)
    dbo.[ReferralSourceView]      → pats.tbl_preadmissionreferralsource  (via st.FromTblVw)
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

7a. dbo.SF_PatientPreAdmission — Pre-Admission Intake Records (V6 schema)

Key columns:
    id                              int        Unique pre-admission record ID (mapped as PreAdmissionid)
    PatientID                       int        Patient/client ID (mapped as Clientid)
    cltM4ID                         varchar    Medicaid ID (joined from tblClient)
    CreatedON                       datetime   Record creation date
    Createdby                       varchar    Created by user
    PreAdmissionDate                datetime   Pre-admission interview date
    RegistrationModeID              int        Registration mode code (0/1/2 → text via CASE)
    ReferralSourceID                int        Referral source code (joined to tblCodes.cdeDesc)
    PrimaryReferralSourceNote       varchar    Free-text referral note
    ProgramID                       int        Program ID (joined to SF_Program.Description)
    InsuranceType                   varchar    Insurance type
    IntakeProgram                   varchar    Intake program name
    IntakeProgramDate               datetime   Intake program date
    IsCurrentlyInOpiateProgram      bit        Converted to 'Yes'/'No' by CASE
    IsPatientAtPainManagementClinic bit        Converted to 'Yes'/'No' by CASE
    IsHavingLegalPrescription       bit        Converted to 'Yes'/'No' by CASE
    IsAnyLegalPrescriptionForPain   bit        Converted to 'Yes'/'No' by CASE
    IsAnyOngoingMedicalCondition    bit        Converted to 'Yes'/'No' by CASE
    IsSuicidalThoughtWithin72Hours  bit        Converted to 'Yes'/'No' by CASE
    IsHavingPlanForHowToCommitSuicide bit      Converted to 'Yes'/'No' by CASE
    IsHomicidalThoughtWithin72Hours bit        Converted to 'Yes'/'No' by CASE
    IsRecentlyReleasedFromPenal     bit        Converted to 'Yes'/'No' by CASE
    IsSpecialAccommodationRequired  bit        Converted to 'Yes'/'No' by CASE
    ReasonSeekingTreatment          varchar    Free-text reason
    AccomodationNeeded              varchar    Accommodation description (note: typo in column/field)
    ClientAddress                   varchar    Patient address (V6 schema marker column)
    Comments                        varchar    General comments
    IsPatientAdmitted               bit        Converted to 'Yes'/'No'
    AreYouCurrentlyPregnant         int        0='Yes', 1='No', 2='Unknown' (reversed convention)
    BringIDProof                    bit        'Yes'/'No'
    BringInsuranceCard              bit        'Yes'/'No'
    ClinicInfo                      bit        'Yes'/'No'
    CurrntlyRecevingTreatmentForCondition bit  'Yes'/'No' (note: typo in column name)
    IsAnyPrescriptionForPain        bit        'Yes'/'No'
    IsInsurance                     bit        'Yes'/'No'
    isOverTheCounterMedications     bit        'Yes'/'No'
    ImmediateAssessment             varchar    Immediate assessment triage note
    ImmediateAssessment911          varchar    Emergency (911) triage note
    MedicalConditionsProviderName1  varchar    Primary medical provider name
    MedicalConditionsProviderPhone1 varchar    Primary medical provider phone
    MedicalConditionsProviderName2  varchar    Secondary medical provider name
    MedicalConditionsProviderPhone2 varchar    Secondary medical provider phone
    PlanOfSuicide                   bit        'Yes'/'No'
    PlanOnSpendingTimeAtClinic      bit        'Yes'/'No'
    SammsProgramID                  int        Joined to tblCodes.cdeDesc as SAMMSProgram
    OfficeUseWhy                    varchar    Internal office reason field
    OngoingMedicalConditionsWha     varchar    Ongoing conditions detail
    PreAdd_Address                  varchar    Pre-admission address (separate from ClientAddress)
    LastUpdatedBy                   varchar    Last updated by user
    LastUpdateOn                    datetime   Last update timestamp
    PatientSignatureDate            datetime   Patient signature date
    DateofRelease                   datetime   Date of release
    Version                         varchar    Form version
    IsDeleted                       bit        Soft-delete flag — drives RowState in Azure

7b. dbo.[ReferralSourceView] — Referral Source Detail Records

Key columns (column list from st.FromTblVw configuration):
    id                   int        Unique referral source record ID (primary key)
    preadmissionid       int        Linked pre-admission record ID
    clientid             int        Patient/client ID
    dataformid           int        Linked data form ID
    primaryreferralsource varchar   Primary referral source code/name
    secondaryreferralsource varchar  Secondary referral source code/name
    referralsourcenote   varchar    Referral source note
    createdby            varchar    Created by user
    createdon            datetime   Record creation date
    lastupdatedby        varchar    Last updated by user
    lastupdateon         datetime   Last update timestamp
    enrollmentid         int        Linked enrollment ID
    program              varchar    Program name
    isdeleted            bit/int    Delete flag (dual-type — see Section 10)
    referralorganization varchar    Referral organization name
    referralname         varchar    Referral contact name
    accountnotinlist     bit/int    Flag for account not in lookup list (dual-type)
    contactnotinlist     bit/int    Flag for contact not in lookup list (dual-type)
    whylefttreatmentofbhg varchar   Narrative: why patient left BHG
    whycomingbacktobhg   varchar    Narrative: why patient is returning to BHG
    mostwanttododifferently varchar Narrative: what patient wants to do differently
    organization         varchar    Organization name
    name                 varchar    Contact name
    email                varchar    Contact email
    phone                varchar    Contact phone
    ispatientreadmit     bit/int    Readmit flag (dual-type)
    referralorganizationid varchar  Organization lookup ID
    referralnameid       varchar    Contact lookup ID
    sitecode             varchar    Site code
________________________________________

8. SavePreAdmissionV6 — Pre-Admission Intake Load (ayx.tbl_preadmission_v6)

Source: dbo.SF_PatientPreAdmission (via hardcoded BHGTaskRunner JOIN query)
Destination: ayx.tbl_preadmission_v6
Composite key: SiteCode (implicit via pre-load) + PreAdmissionid + Clientid
Parameters: tbl, sc, db
Note: No wrkdt parameter — date scoping handled entirely at source via strWhere.

Azure pre-load:
    PreAds = db.TblPreAdmissionV6.Where(x => x.SiteCode == sc).ToList()
Full site slice — all existing pre-admission records for the clinic.
No pre-pass RowState reset — existing records are not deactivated before the loop.

Per-row fresh object construction:
For each source row, a NEW TblPreAdmissionV6 object is created, then ALL fields are mapped
via a 54-field column switch. The lookup happens AFTER the full object is populated.

RowState and EtllastModAt stamped in sitecode case:
    case "sitecode":
        pa.SiteCode = r[c.ColumnName].ToString()
        pa.RowState = true
        pa.EtllastModAt = DateTime.Now

RowState override by isdeleted case:
    case "isdeleted":
        if (r[c.ColumnName].ToString() == "1") { pa.RowState = false; }
        else { pa.RowState = true; }

Column-order dependency: If the DataTable presents "isdeleted" BEFORE "sitecode" in the
column sequence, the "sitecode" case will fire last and always reset RowState=true, making
the isdeleted override ineffective. If "sitecode" fires first (most likely since it's the
first column in the hardcoded SELECT), the isdeleted override correctly sets the final
RowState value (see Anomaly 2).

Lookup key: PreAdmissionid + Clientid
    xpa = PreAds.Where(x => x.PreAdmissionid == pa.PreAdmissionid
                          && x.Clientid == pa.Clientid).FirstOrDefault()

RowChkSum guard — DISABLED:
    //if (xpa.RowChkSum != pa.RowChkSum)
    {
        xpa.AccomodationNeeded = pa.AccomodationNeeded
        xpa.AreYouCurrentlyPregnant = pa.AreYouCurrentlyPregnant
        ... (52 fields)
    }
The guard is commented out. All 52 fields are always fully overwritten for every found
record on every run, regardless of whether any data changed (see Anomaly 1).

Update path (52 fields explicitly copied — major fields listed):
AccomodationNeeded, AreYouCurrentlyPregnant, BringIdproof, BringInsuranceCard, ClientAddress,
ClinicInfo, Comments, Createdby, CreatedOn, CurrntlyRecevingTreatmentForCondition, DateofRelease,
EtllastModAt, ImmediateAssessment, ImmediateAssessment911, InsuranceType, IntakeProgram,
IntakeProgramDate, IsAnyLegalPrescriptionForPain, IsAnyOngoingMedicalCondition,
IsAnyPrescriptionForPain, IsCurrentlyInOpiateProgram, IsHavingLegalPrescription,
IsHavingPlanForHowToCommitSuicide, IsHomicidalThoughtWithin72Hours, IsInsurance,
IsOverTheCounterMedications, IsPatientAdmitted, IsPatientAtPainManagementClinic,
IsRecentlyReleasedFromPenal, IsSpecialAccommodationRequired, IsSuicidalThoughtWithin72Hours,
LastUpdatedBy, LastUpdateOn, MedicalConditionsProviderName1/2, MedicalConditionsProviderPhone1/2,
OfficeUseWhy, OngoingMedicalConditionsWha, PatientSignatureDate, PlanOfSuicide,
PlanOnSpendingTimeAtClinic, PreAddAddress, PreAdmissionDate, PrimaryReferralSourceNote,
Program, ReasonSeekingTreatment, ReferralSourcedesc, RegistrationMode, RowState, RowChkSum,
Sammsprogram, Version, cltM4ID

New record path:
    NewPAs.Add(pa)
    rCodes.RowsIns++
New records are batched in NewPAs list.

Commit sequence:
    db.SaveChanges()               ← commits all update field assignments (EF Core tracking)
    if (NewPAs.Count > 0)
    {
        db.TblPreAdmissionV6.AddRange(NewPAs)
        db.SaveChanges()           ← commits all new inserts
    }

Column mapping (SavePreAdmissionV6 — 54 fields via switch):

    Source column                       EF property                            Type / notes
    ---------                           -----------                            -----
    sitecode                            SiteCode + RowState + EtllastModAt     string + bool=true + DateTime.Now (stamped here)
    preadmissionid                      PreAdmissionid                         int — direct parse
    clientid                            Clientid                               int — direct parse
    cltm4id                             cltM4ID                                string
    createdon                           CreatedOn                              DateTime? — length > 6 guard
    createdby                           Createdby                              string
    preadmissiondate                    PreAdmissionDate                       DateTime? — length > 6 guard
    registrationmode                    RegistrationMode                       string (already converted to text by CASE)
    referralsourcedesc                  ReferralSourcedesc                     string (joined tblCodes.cdeDesc)
    primaryreferralsourcenote           PrimaryReferralSourceNote              string
    program                             Program                                string (joined SF_Program.Description)
    insurancetype                       InsuranceType                          string
    intakeprogram                       IntakeProgram                          string
    intakeprogramdate                   IntakeProgramDate                      DateTime? — length > 6 guard
    iscurrentlyinopiateprogram          IsCurrentlyInOpiateProgram             string ('Yes'/'No' from CASE)
    ispatientatpainmanagementclinic     IsPatientAtPainManagementClinic        string
    ishavinglegalprescription           IsHavingLegalPrescription              string
    isanylegalprescriptionforpain       IsAnyLegalPrescriptionForPain          string
    isanyongoingmedicalcondition        IsAnyOngoingMedicalCondition           string
    issuicidalthoughtwithin72hours      IsSuicidalThoughtWithin72Hours         string
    ishavingplanforhowtocommitsuicide   IsHavingPlanForHowToCommitSuicide      string
    ishomicidalthoughtwithin72hours     IsHomicidalThoughtWithin72Hours        string
    isrecentlyreleasedfrompenal         IsRecentlyReleasedFromPenal            string
    isspecialaccommodationrequired      IsSpecialAccommodationRequired         string
    reasonseekingtreatment              ReasonSeekingTreatment                 string
    accomodationneeded                  AccomodationNeeded                     string (typo in column and property)
    clientaddress                       ClientAddress                          string
    comments                            Comments                               string
    ispatientadmitted                   IsPatientAdmitted                      string
    areyoucurrentlypregnant             AreYouCurrentlyPregnant                string ('Yes'/'No'/'Unknown' — reversed CASE)
    bringidproof                        BringIdproof                           string
    bringinsurancecard                  BringInsuranceCard                     string
    clinicinfo                          ClinicInfo                             string
    currntlyrecevingtreatmentforcondition CurrntlyRecevingTreatmentForCondition string (typo in both)
    isanyprescriptionforpain            IsAnyPrescriptionForPain               string
    isinsurance                         IsInsurance                            string
    isoverthecountermedications         IsOverTheCounterMedications            string
    immediateassessment                 ImmediateAssessment                    string
    immediateassessment911              ImmediateAssessment911                 string
    medicalconditionsprovidername1      MedicalConditionsProviderName1         string
    medicalconditionsproviderphone1     MedicalConditionsProviderPhone1        string
    medicalconditionsprovidername2      MedicalConditionsProviderName2         string
    medicalconditionsproviderphone2     MedicalConditionsProviderPhone2        string
    planofsuicide                       PlanOfSuicide                          string
    planonspendingtimeatclinic          PlanOnSpendingTimeAtClinic             string
    sammsprogram                        Sammsprogram                           string (joined tblCodes.cdeDesc)
    officeusewhy                        OfficeUseWhy                           string
    ongoingmedicalconditionswha         OngoingMedicalConditionsWha            string
    preaddaddress                       PreAddAddress                          string (source: pp.PreAdd_Address)
    lastupdatedby                       LastUpdatedBy                          string
    lastupdateon                        LastUpdateOn                           DateTime? — length > 6 guard
    patientsignaturedate                PatientSignatureDate                   DateTime? — length > 6 guard
    dateofrelease                       DateofRelease                          DateTime? — length > 6 guard
    version                             Version                                string
    isdeleted                           RowState                               bool — "1"=false, else=true
    rowchksum                           RowChkSum                              int
________________________________________

9. SavePreAdminReferrals — Referral Source Load (pats.tbl_PreAdmissionReferralSource)

Source: dbo.[ReferralSourceView] (or configured source via st.FromTblVw)
Destination: pats.tbl_PreAdmissionReferralSource
Key: SiteCode (via pre-load) + Id
Parameters: tbl, sc, wrkdt, db
Note: wrkdt is passed in from BHGTaskRunner but not used inside the method.

Azure pre-load:
    dbtbl = db.TblPreAdmissionReferralSource.Where(x => x.SiteCode == sc).ToList()
Full site slice — all existing referral source records for the clinic.

Per-row fresh object construction:
A NEW TblPreAdmissionReferralSource object prs is built via a 33-field column switch,
then looked up by Id.

Dual data type handling for boolean-like fields:
Three fields in this method use runtime type checking to handle both SQL BIT (mapped as
C# System.Boolean in DataRow) and SQL INT/VARCHAR representations across different clinic
schema versions:

    isdeleted:
        if (GetType() == "System.Boolean")  → bool.Parse → 1 or 0
        else if (length > 0)                → int.Parse
        else                                → default 0

    accountnotinlist:
        if (GetType() == "System.Boolean")  → bool.Parse → 1 or 0
        else if (trim.length > 0)           → int.Parse

    contactnotinlist:
        Same pattern as accountnotinlist

    ispatientreadmit:
        if (GetType() == "System.Boolean")  → bool.Parse directly (stores as bool)
        else if value == "1"                → true
        else                               → false
        Note: stores as bool (not int) unlike the other three flags.

This dual-type approach handles clinic databases where the column is defined as BIT vs INT.

Lookup key: Id only
    dprs = dbtbl.FirstOrDefault(x => x.Id == prs.Id)

No RowChkSum — every found record is always fully updated (25 fields copied explicitly).

Update path (25 fields explicitly copied to dprs):
AccountNotInList, ClientId, ContactNotInList, CreatedBy, CreatedOn, DataFormId, Email,
EnrollmentId, IsDeleted, IsPatientReadmit, LastUpdatedBy, LastUpdateOn,
MostWantToDoDifferently, Name, Organization, Phone, PreAdmissionId, PrimaryReferralSource,
Program, ReferralName, ReferralNameId, ReferralOrganization, ReferralOrganizationId,
ReferralSourceNote, SecondaryReferralSource, WhyComingBackToBhg, WhyLeftTreatmentOfBhg

NOT copied in update path (key fields preserved):
SiteCode, Id — expected, as they are the identity key

New record path:
    newPRS.Add(prs)
    rCodes.RowsIns += 1

Commit sequence:
    db.SaveChanges()               ← commits all updates via EF Core tracking
    if (newPRS.Count > 0)
    {
        db.TblPreAdmissionReferralSource.AddRange(newPRS)
        db.SaveChanges()
    }

wrkdt: Accepted but never used inside the method. Passed from BHGTaskRunner as
WorkDate.AddDays(-515) to document the lookback period, but SavePreAdminReferrals ignores it.

Column mapping (SavePreAdminReferrals — 33 fields via switch):

    Source column             EF property               Type / notes
    ---------                 -----------               -----
    sitecode                  SiteCode                  string
    id                        Id                        int — direct parse
    preadmissionid            PreAdmissionId            int? — trim length > 0 guard
    clientid                  ClientId                  int? — trim length > 0 guard
    dataformid                DataFormId                int? — trim length > 0 guard
    primaryreferralsource     PrimaryReferralSource     string
    secondaryreferralsource   SecondaryReferralSource   string
    referralsourcenote        ReferralSourceNote        string
    createdby                 CreatedBy                 string
    createdon                 CreatedOn                 DateTime? — length > 6 guard
    lastupdatedby             LastUpdatedBy             string
    lastupdateon              LastUpdateOn              DateTime? — length > 6 guard
    enrollmentid              EnrollmentId              int? — trim length > 0 guard
    program                   Program                   string
    isdeleted                 IsDeleted                 int — dual-type (bool→1/0 or int.Parse or 0)
    referralorganization      ReferralOrganization      string
    referralname              ReferralName              string
    accountnotinlist          AccountNotInList          int? — dual-type (bool→1/0 or int.Parse)
    contactnotinlist          ContactNotInList          int? — dual-type (bool→1/0 or int.Parse)
    whylefttreatmentofbhg     WhyLeftTreatmentOfBhg     string
    whycomingbacktobhg        WhyComingBackToBhg        string
    mostwanttododifferently   MostWantToDoDifferently   string
    organization              Organization              string
    name                      Name                      string
    email                     Email                     string
    phone                     Phone                     string
    ispatientreadmit          IsPatientReadmit          bool? — dual-type (bool or "1"=true else false)
    referralorganizationid    ReferralOrganizationId    string
    referralnameid            ReferralNameId            string
________________________________________

10. Change Detection — RowChkSum Behaviour

Method                   RowChkSum present    Guard used    Effective behaviour
------                   -----------------    ----------    -------------------
SavePreAdmissionV6       Yes (stored)         Disabled      Guard commented out — all 52 fields always overwritten on every run for every found record. RowChkSum stored in Azure but never compared.
SavePreAdminReferrals    No                   N/A           No RowChkSum. 25 fields always overwritten for every found record.
________________________________________

11. Scoping / Data Windowing

Source-side scoping:

SavePreAdmissionV6:
    Standard strWhere rolling lookback (DaysBack=-15) applied after the hardcoded SELECT.
    The commented-out test WHERE suggests an older filter:
    //" where pp.CreatedOn > '' and pp.ClientAddress not like '%test data%' order by..."
    The current production WHERE uses the standard @WorkDate condition from task metadata.

SavePreAdminReferrals:
    Extended lookback: DaysBack - 500 = -515 (approximately 17 months).
    This is much wider than the standard -15 day window, reflecting that referral source
    records may be associated with pre-admission records created long before the current run.

Azure-side scoping:
    Both methods: Full site slice — all records for the site.
________________________________________

12. Error Handling

Both methods use the same pattern:
    catch (Exception e)
    {
        rCodes.IsResult = false
        rCodes.ExceptMsg = e.Message
        if (e.InnerException != null)
        {
            rCodes.ExceptInnerMsg = e.InnerException.Message
        }
    }
Standard BHG-DR-LIB pattern. No Console.WriteLine. No inner per-field try/catch.
A parse failure on any field (e.g., empty preadmissionid, empty clientid) will abort
the entire site's load for that task.
________________________________________

13. Anomalies, Bugs, and Known Defects

ANOMALY 1 — SavePreAdmissionV6: RowChkSum guard commented out — all fields always overwritten.

File: SavePreAdmissionV6.cs, lines 238–294
    //if (xpa.RowChkSum != pa.RowChkSum)
    {
        xpa.AccomodationNeeded = pa.AccomodationNeeded
        ... (52 fields)
    }

The RowChkSum comparison is permanently commented out. Every existing pre-admission record
is fully rewritten on every daily run. This generates UPDATE statements for all 52 fields
on every found record regardless of actual changes, increasing unnecessary database write
traffic. RowChkSum is still stored in Azure but serves no functional purpose.

ANOMALY 2 — SavePreAdmissionV6: RowState depends on column order (sitecode vs isdeleted).

File: SavePreAdmissionV6.cs, lines 32–36 and 214–223
    case "sitecode": pa.RowState = true      ← set to true when sitecode column is processed
    case "isdeleted": pa.RowState = true/false  ← may override to false

If the DataTable column order places "sitecode" AFTER "isdeleted", the sitecode case will
fire last and always reset RowState to true, neutralising the isdeleted logic. The hardcoded
SELECT in BHGTaskRunner places SiteCode first (line 190), so in practice the correct order
applies — but this is fragile. Any change to the SELECT column order could silently break
the soft-delete mechanism.

ANOMALY 3 — SavePreAdmissionV6: AreYouCurrentlyPregnant uses reversed CASE convention.

File: BHGTaskRunner/Program.cs, line 205
    AreYouCurrentlyPregnant = Case
        when pp.AreYouCurrentlyPregnant = 0 then 'Yes'
        when pp.AreYouCurrentlyPregnant = 1 then 'No'
        when pp.AreYouCurrentlyPregnant = 2 then 'Unknown'

Standard SAMMS boolean convention is 1=true/Yes and 0=false/No. This field reverses that:
0='Yes' (currently pregnant) and 1='No' (not pregnant). This may be intentional SAMMS
clinical design (0=no response/unknown pregnancy status vs 1=confirmed not pregnant) or it
may be a developer error that has become permanent. Azure will store 'Yes' for patients
where the source bit is 0.

ANOMALY 4 — SavePreAdmissionV6: Source SELECT is entirely hardcoded — not metadata-driven.

File: BHGTaskRunner/Program.cs, lines 190–219
The SELECT query is built as a multi-line string literal in BHGTaskRunner. It is not
generated by SelectConstructor from dms.tbl_MapSrc2Dsn. This means:
- Column changes in SF_PatientPreAdmission require a BHGTaskRunner code change
- The column list cannot be managed through the metadata control tables
- RowChkSum is computed inline in the SELECT (CHECKSUM expression hardcoded)
- Any new columns added to SF_PatientPreAdmission are invisible to the ETL until
  the hardcoded SELECT is manually updated

ANOMALY 5 — SavePreAdmissionV6: Three spelling/naming typos preserved in both source and target.

File: SavePreAdmissionV6.cs / BHGTaskRunner/Program.cs
1. "AccomodationNeeded" — missing second 'm' in "Accommodation" (should be "AccommodationNeeded").
   Present in both the source column name and the EF property.
2. "CurrntlyRecevingTreatmentForCondition" — double typo: "Currntly" (missing 'e') and
   "Receving" (missing 'i'). Present in both source and EF property.
3. "preaddaddress" — maps to pp.PreAdd_Address in source but stored as PreAddAddress in Azure.
   The underscore is stripped. This is a naming inconsistency, not a functional bug.
These typos are frozen in the SAMMS schema and the EF model — correcting them would
require schema migration on both sides.

ANOMALY 6 — SavePreAdminReferrals: wrkdt parameter accepted but unused.

File: SavePreAdmissionV6.cs, line 318
    public RCodes SavePreAdminReferrals(DataTable tbl, string sc, DateTime wrkdt, ...)

wrkdt is never referenced in the method body. Azure pre-load always loads the full site
slice regardless of this value. BHGTaskRunner passes WorkDate.AddDays(-515), suggesting
the intent was to use wrkdt for Azure-side scoping (a rolling 17-month window), but it
was never implemented.

ANOMALY 7 — SavePreAdminReferrals: MostWantToDoDifferently not updated in found-path.

File: SavePreAdmissionV6.cs, lines 510–537
Reviewing the update path, MostWantToDoDifferently IS included at line 522. No omission.
[Cross-check passed — this field is correctly mapped in both paths.]

ANOMALY 8 — SavePreAdmissionV6: No RowTrax audit — no source/destination count logged.

BHGTaskRunner does not include a RowTrax block after the SavePreAdmissionV6 call.
No source-vs-destination count is recorded for ayx.tbl_preadmission_v6.
Similarly, SavePreAdminReferrals has no RowTrax block.
________________________________________

14. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily)
        V
Scheduler.exe
        |
        |-- insert parent task (SAMMS-ETL-INV or equivalent)
        |-- insert child tasks per clinic:
        |       ayx.tbl_preadmission_v6             SiteCode='B01A'
        |       pats.tbl_preadmissionreferralsource  SiteCode='B01A'
        |       ... (repeated for each active clinic)
        |
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        V
BHGTaskRunner.exe 8 (or 3)
        |
        |======================================================
        |  BRANCH A: ayx.tbl_preadmission_v6
        |======================================================
        |   LAYER 1: sys.tables probe for SF_PatientPreAdmission
        |       if not found OR SchemaVersion=='V5': skip
        |   LAYER 2: sys.all_columns probe for 'clientaddress' column
        |       if not found: IsResult=false, error message, skip
        |   LAYER 3: Pass — build hardcoded multi-join SELECT
        |       CASE expressions convert bit→'Yes'/'No'
        |       RowChkSum = CHECKSUM(pp.id, pp.PatientID, LastUpdatedBy, ...)
        |       strCmd += " Where " + strWhere + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   → sd.SavePreAdmissionV6(SrcDt, SiteCode, null)
        |       Pre-load full site slice from ayx.tbl_preadmission_v6
        |       Loop rows:
        |           Build fresh TblPreAdmissionV6 via 54-field switch
        |           RowState=true + EtllastModAt stamped in 'sitecode' case
        |           RowState overridden by 'isdeleted' case ("1"→false)
        |           Lookup by PreAdmissionid + Clientid
        |           Found: copy all 52 fields (no RowChkSum guard — always overwrites)
        |           Not found: NewPAs.Add(pa)
        |       db.SaveChanges() → AddRange(NewPAs) → db.SaveChanges()
        |       → ayx.tbl_preadmission_v6 (Azure BHG_DR)
        |       No RowTrax audit
        |
        |======================================================
        |  BRANCH B: pats.tbl_preadmissionreferralsource
        |======================================================
        |   sys.all_objects probe for source table existence
        |       if not found: skip (IsResult=true)
        |   DaysBack-500 lookback (≈-515 days)
        |   strCmd += " Where " + strWhere(@WorkDate=-515) + " " + SortOrder
        |   SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
        |   if SrcDt.Rows.Count == 0: skip (IsResult=true)
        |   → sd.SavePreAdminReferrals(SrcDt, SiteCode, WorkDate.AddDays(-515), null)
        |       Pre-load full site slice from pats.tbl_PreAdmissionReferralSource
        |       Loop rows:
        |           Build fresh TblPreAdmissionReferralSource via 33-field switch
        |           Dual-type handling for isdeleted, accountnotinlist,
        |           contactnotinlist, ispatientreadmit
        |           Lookup by Id
        |           Found: copy 25 fields (always overwrites — no RowChkSum)
        |           Not found: newPRS.Add(prs)
        |       db.SaveChanges() → AddRange(newPRS) → db.SaveChanges()
        |       → pats.tbl_PreAdmissionReferralSource (Azure BHG_DR)
        |       No RowTrax audit
        |
        V
BHGTaskRunner marks task Status=20 (complete)

        [Azure BHG_DR — final state after run]
        ayx.tbl_preadmission_v6                — pre-admission records upserted (all fields rewritten)
        pats.tbl_PreAdmissionReferralSource    — referral source records upserted (all fields rewritten)
________________________________________

15. File Reference Map

File Path                                                          Purpose
---------                                                          -------
BCAppCode/BHG-DR-LIB/SavePreAdmissionV6.cs                        Both methods (560 lines)
BCAppCode/BHGTaskRunner/Program.cs                                 Dispatch + pre-validation
                                                                   ayx.tbl_preadmission_v6           ~line 173
                                                                   pats.tbl_preadmissionreferralsource ~line 1341
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                             sys.tables/all_objects probes + SELECT
BCAppCode/BHG-DR-LIB/Models/TblPreAdmissionV6.cs                  EF Model → ayx.tbl_preadmission_v6
BCAppCode/BHG-DR-LIB/Models/TblPreAdmissionReferralSource.cs      EF Model → pats.tbl_PreAdmissionReferralSource
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                      EF DbContext — DbSet registrations
BCAppCode/Scheduler/Program.cs                                     Task creation
________________________________________

16. Quick Reference Summary

Method                   Load path    Key                                    RowChkSum guard    RowState            Schedule
------                   ---------    ---                                    ---------------    --------            --------
SavePreAdmissionV6       EF Core      SiteCode + PreAdmissionid + Clientid   Disabled           Yes (isdeleted "1") 8 / 3
SavePreAdminReferrals    EF Core      SiteCode + Id                          None               None (IsDeleted int) 8 / 3

Pre-call validation:
    SavePreAdmissionV6:    3-layer check (table existence + V5 skip + clientaddress column)
    SavePreAdminReferrals: 1-layer check (source object existence)

Source data approach:
    SavePreAdmissionV6:    Hardcoded multi-join SELECT with CASE bit→string conversions
    SavePreAdminReferrals: Standard metadata-driven SELECT via SelectConstructor

Lookback window:
    SavePreAdmissionV6:    Standard DaysBack=-15 (rolling 15-day)
    SavePreAdminReferrals: DaysBack-500 = -515 (rolling ~17-month)

Critical bugs:
1. SavePreAdmissionV6 — RowChkSum guard commented out — all 52 fields rewritten every run;
   unnecessary UPDATE traffic for every existing pre-admission record
2. SavePreAdmissionV6 — RowState column-order dependency — isdeleted override only works
   correctly if sitecode column appears before isdeleted in the DataTable
3. AreYouCurrentlyPregnant CASE reversal — 0='Yes', 1='No' (reversed from standard convention)
4. Hardcoded SELECT — new source columns invisible until BHGTaskRunner code is manually updated
5. wrkdt parameter in SavePreAdminReferrals is accepted but never used — Azure always loads
   full site slice regardless of the intended 17-month window



SavePreAdmissionV6.cs — Method Metadata Tables
Method: SavePreAdmissionV6
Field	Value
Name	SavePreAdmissionV6
Module	Pre-admission intake records (V6 schema)
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode
Source table	dbo.SF_PatientPreAdmission (joined to dbo.SF_Program, dbo.tblCodes, dbo.tblClient); SELECT is hardcoded in BHGTaskRunner — not metadata-driven via dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	ayx.tbl_preadmission_v6
Load type	EF Core upsert — full site slice pre-load; no pre-pass RowState reset; composite key lookup (PreAdmissionid + Clientid); RowChkSum guard DISABLED (commented out) — all 54 fields overwritten on every run; two-phase commit
Load type column	RowChkSum stored but guard commented out; RowState (bool) derived from isdeleted case; column-order dependency between sitecode and isdeleted cases; EtllastModAt stamped in sitecode case
Frequency	Daily
Schedule	Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) / Schedule 3 (catch-all)
Parent	SAMMS-ETL-INV
Downstream	ayx.tbl_preadmission_v6 → pre-admission intake reporting; safety screening; triage decisioning
Connection / method	Source: hardcoded JOIN SELECT built in BHGTaskRunner. Target: sd.SavePreAdmissionV6(SrcDt, st.SiteCode, null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SavePreAdmissionV6.cs
Status	Active — gated by 3-layer pre-check (table existence + SchemaVersion != V5 + ClientAddress column check)
Folder	BHG-DR-LIB\SavePreAdmissionV6.cs; detail in SavePreAdmissionV6-Documentation\SavePreAdmissionV6_ETL_Complete_Documentation.md
Method: SavePreAdminReferrals
Field	Value
Name	SavePreAdminReferrals
Module	Pre-admission referral source detail records
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode
Source table	dbo.[ReferralSourceView] (via st.FromTblVw); column list from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_PreAdmissionReferralSource
Load type	EF Core upsert — full site slice pre-load; no pre-pass RowState reset; single key lookup (Id); no RowChkSum at all — all fields overwritten on every run; dual data-type handling for boolean fields (GetType() == "System.Boolean"); two-phase commit; called only when SrcDt.Rows.Count > 0
Load type column	No RowChkSum; RowState (bool) derived from isdeleted case with dual type handling; LastModAt stamped in sitecode case
Frequency	Daily
Schedule	Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) / Schedule 3 (catch-all)
Parent	SAMMS-ETL-INV
Downstream	pats.tbl_PreAdmissionReferralSource → referral source reporting; marketing attribution; readmission tracking
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr) with extended lookback (~515 days). Target: sd.SavePreAdminReferrals(SrcDt, st.SiteCode, WorkDate.AddDays(-515), null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SavePreAdmissionV6.cs
Status	Active — gated by single-layer table existence pre-check (sys.all_objects); added 9-12-2025
Folder	BHG-DR-LIB\SavePreAdmissionV6.cs; detail in SavePreAdmissionV6-Documentation\SavePreAdmissionV6_ETL_Complete_Documentation.md
