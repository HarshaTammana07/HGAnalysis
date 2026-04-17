
Comprehensive Assessment (CA) ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 6 — Samms-Forms
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract Comprehensive Assessment (CA) records from local SAMMS SQL Server databases at each clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What each CA form type is and why it exists clinically
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and how BHGTaskRunner dispatches them
- How all eight Save methods (SaveMNCA, SaveMNCALOC, SaveVACA, SaveVACASummary, SaveNewAdmissionAssessment, SaveNewAdmissionAssessmentASAMDimension6, SaveNewPeriodicReassessment, Savenewperiodicreassessmentcounselorreview) work in detail
- What the source tables look like and their key columns
- What the destination tables look like
- The shared EF Core two-phase upsert pattern used across all methods
- The table-existence guard unique to these methods
- All known anomalies and code defects

There are eight methods in SaveCA.cs, spanning three clinical form domains:
- Minnesota Comprehensive Assessment (MN CA): SaveMNCA, SaveMNCALOC
- VA Comprehensive Assessment (VA CA): SaveVACA, SaveVACASummary
- New Admission Assessment (NAA): SaveNewAdmissionAssessment, SaveNewAdmissionAssessmentASAMDimension6
- New Periodic Reassessment (NPR): SaveNewPeriodicReassessment, Savenewperiodicreassessmentcounselorreview
________________________________________

2. High-Level Business Summary

What is Comprehensive Assessment data?

Comprehensive Assessments are standardised clinical intake and periodic review forms completed by clinicians in SAMMS at key milestones during a patient's treatment journey. They capture multidimensional patient data including referral history, level-of-care determination, ASAM placement criteria, social determinants of health, patient engagement indicators, periodic treatment pathway changes, and multi-signature clinical sign-off.

Minnesota Comprehensive Assessment (MN CA)
The MN CA is a two-table form. The header (dbo.MNComprehensiveAssessment) holds the assessment header identifying client, date, referral, insurance, version, and audit fields. The Level of Care sub-form (dbo.MNComprehensiveAssessmentlevelofcare) holds the detailed level-of-care determination content including opioid risk counseling checklist, ASAM level recommendations (1, 2.1, 2.5, 3.1, 3.3, 3.5, 3.7, 4), opioid treatment services, withdrawal management, NALOC, accessibility barrier flags, and placement variance reasoning.

VA Comprehensive Assessment (VA CA)
The VA CA is a two-table form used at Veterans Affairs-affiliated clinics. The header (dbo.VAComprehensiveAssessment) holds minimal identification and audit data — it is a thin header record. The summary (dbo.vacomprehensiveassessmentsummary) holds the clinical summary content including treatment recommendation code, opioid treatment services, withdrawal management, ASAM-recommended level, level-of-care variance, and clinical summary text.

New Admission Assessment (NAA)
The New Admission Assessment is a two-table form. The header (dbo.NewAdmissionassessment) stores client, form version, audit, and IsDeleted status. The ASAM Dimension 6 sub-form (dbo.NewAdmissionassessmentASAMDimension6) is the most data-rich form in this file, capturing 12 readiness-to-change questions (Readiness Q1–Q12), stage of change, social determinants of health barriers (transportation, food/housing, childcare, financial, employment, healthcare, social supports, language), ASAM level recommendations (Level 1, 1.5, 1.7, 2.1, 2.5, 2.7, 3.1, 3.5, 3.7, 4, BIO, NonBIO, COE), multi-reason variance flags, treatment preferences, four-party signature block (patient, supervisor, counselor, provider), and clinical summary narrative.

New Periodic Reassessment (NPR)
The NPR is a two-table form. The header (dbo.newperiodicreassessment) stores client, date, pathway, completion location, audit, and version. The counselor review (dbo.newperiodicreassessmentcounselorreview) captures the periodic reassessment clinical findings including ASAM level reassessment, COPE phase, MAT pathway (Induction/Stabilization/Maintenance), BAM-derived risk/use/protective scores, placement variance reasons, patient/counselor/supervisor/provider signature block, and whether the review requires physician review (RR flag).

Why this data is important
- Drives level-of-care placement and variance documentation required by accreditation bodies
- Supports payer-required ASAM criteria documentation for prior authorization and audits
- Feeds periodic compliance reporting for treatment plan reviews at mandated intervals
- Provides multidimensional patient outcome and readiness tracking across the treatment continuum

Load type
All eight methods use EF Core upsert only. No SqlBulkCopy staging path exists for any CA method. Every method performs a unique table-existence pre-check against SAMMS sys.tables before proceeding. If the table does not exist in the clinic's SAMMS database, the entire method invocation is skipped.
________________________________________

3. Systems Involved

System / File                                               Role
-----------                                                 ----
tsk.tbl_Schedule (Azure DB)                                 Configuration — defines schedules and run times
Scheduler.exe                                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=6                                     Main ETL orchestrator for Samms-Forms
ctrl.tbl_LocationCons (Azure DB)                            Connection strings for each clinic SAMMS SQL Server
dms.vw_MapAction (Azure DB)                                 Maps destination tables to schedule TaskNames
dms.tbl_MapSrc2Dsn (Azure DB)                              Column list + RowChkSum expression for SELECT build
SQLSvrManager.cs                                            Fires SELECT against the clinic SAMMS SQL Server
SaveCA.cs (BHG-DR-LIB)                                     All 8 CA EF Core upsert methods
Models/TblMNComprehensiveAssessment.cs                      EF entity → pats.tbl_MNComprehensiveAssessment
Models/TblMNComprehensiveAssessmentLevelOfCare.cs           EF entity → pats.tbl_MNComprehensiveAssessmentLevelOfCare
Models/TblVAComprehensiveAssessment.cs                      EF entity → pats.tbl_VAComprehensiveAssessment
Models/TblVAComprehensiveAssessmentSummary.cs               EF entity → pats.tbl_VAComprehensiveAssessmentSummary
Models/TblNewAdmissionAssessment.cs                         EF entity → pats.tbl_NewAdmissionAssessment
Models/TblNewAdmissionAssessmentASAMDimension6.cs           EF entity → pats.tbl_NewAdmissionAssessmentASAMDimension6
Models/TblNewPeriodicReassessment.cs                        EF entity → pats.tbl_NewPeriodicReassessment
Models/TblNewPeriodicReassessmentCounselorReview.cs         EF entity → pats.tbl_NewPeriodicReassessmentCounselorReview
pats schema (Azure BHG_DR)                                  All 8 destination tables
tsk.tbl_RowTrax (Azure DB)                                  Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How CA Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For Samms-Forms, it inserts one parent task:
    TaskName = 'Samms-Forms'
    SiteCode = 'All'
    Status   = 17

Step 3 — Insert child tasks (one per clinic per CA table type)
For the 8 CA destination tables, child task rows are inserted:

    pats.tbl_mncomprehensiveassessment                  (one per clinic)
    pats.tbl_mncomprehensiveassessmentlevelofcare        (one per clinic)
    pats.tbl_vacomprehensiveassessment                   (one per clinic)
    pats.tbl_vacomprehensiveassessmentsummary            (one per clinic)
    pats.tbl_newadmissionassessment                      (one per clinic)
    pats.tbl_newadmissionassessmentasamdimension6        (one per clinic)
    pats.tbl_newperiodicreassessment                     (one per clinic)
    pats.tbl_newperiodicreassessmentcounselorreview      (one per clinic)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How CA Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 6:
    BHGTaskRunner.exe 6

Step 1 — Filter queue
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"
        && x.Status == 17
        && x.TaskName == "Samms-Forms"
        && x.RunAt < DateTime.Now).ToList()

Step 2 — Mark parent running (Status=18), loop child tasks

Step 3 — Build base SELECT via SelectConstructor
    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
    strWhere = st.WhereCondition
                 .Replace("@SiteCode", "'" + st.SiteCode + "'")
                 .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")

DaysBack constant = -15 (15-day rolling lookback)

Step 4 — TABLE EXISTENCE PRE-CHECK (all 8 CA tasks)
Unlike most ETL methods in this codebase, every CA task in BHGTaskRunner performs a
table-existence check against the clinic's SAMMS sys.tables BEFORE executing the full
SELECT and calling the Save method.

Pattern (representative example for SaveMNCA):
    SrcDt = sm.GetTableData(st.FromTblVw,
        "select name from sys.tables t where name = 'MNComprehensiveAssessment'",
        st.ConStr)
    if (SrcDt.Rows.Count == 1)
    {
        strCmd += " Where " + strWhere + " " + st.SortOrder
        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        rCodes = sd.SaveMNCA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)
    }
    else
    {
        rCodes.IsResult = false
        rCodes.ExceptMsg = "Table does not exists."
    }

WHAT THIS MEANS: If a clinic's SAMMS database does not have the CA source table,
the ETL marks the task as failed with the message "Table does not exists." without
actually calling the Save method. This is by design — these form types were added
incrementally across clinics. Not all 80+ clinics will have all CA tables.

Step 5 — Dispatch by TaskName to Save method (details in sections 8–15 below)

Step 6 — RowTrax audit (if st.RowTrax = true and SiteCode != "PHC")

Step 7 — Mark task Status=20 (complete)
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager fires two ADO.NET queries per CA task:
1. A sys.tables probe: "select name from sys.tables t where name = '{TableName}'"
2. If the table exists: the full SELECT built from SelectConstructor + WhereCondition

Connection strings are sourced from ctrl.tbl_LocationCons (one row per clinic).
The source table name (st.FromTblVw) comes from dms.tbl_MapAction for each task.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

All source tables live in the clinic's SAMMS SQL Server under the dbo schema.
These tables were added to SAMMS incrementally — not all clinics will have all of them.
The table-existence check in BHGTaskRunner handles clinics where a table is absent.
________________________________________

7a. dbo.MNComprehensiveAssessment — MN CA Header

Primary Key: Id

Column Name             Type            Description
-----------             ----            -----------
Id                      int             Unique form instance ID within this clinic
SiteCode                varchar         Clinic identifier
PreAdmissionId          int             Linked pre-admission / enrollment ID (nullable)
DataFormId              int             Form template ID (nullable)
ClientId                int             Patient / client ID (nullable)
TodayDate               datetime        Date the assessment was completed (nullable, length > 0 guard)
ReferradBy              int             Referral source code (nullable)
ReferradByOther         varchar         Free-text referral source if "Other" selected
ReferralReason          int             Referral reason code (nullable)
ReferralReasonOther     varchar         Free-text referral reason if "Other"
InsuranceId             varchar         Insurance ID string
CreatedBy               varchar         User who created the record
CreatedOn               datetime        Creation timestamp (nullable, length > 6 guard)
ModifiedBy              varchar         User who last modified the record
ModifiedOn              datetime        Last modification timestamp (nullable, length > 6 guard)
Version                 varchar         Form template version
IsDeleted               bit             True = deleted in SAMMS (nullable)
________________________________________

7b. dbo.MNComprehensiveAssessmentlevelofcare — MN CA Level of Care Sub-form

Primary Key: MNComprehensiveAssessmentFormId (FK to dbo.MNComprehensiveAssessment.Id)

Column Name                         Type        Description
-----------                         ----        -----------
MNComprehensiveAssessmentFormId     int         FK to parent MN CA form Id
SiteCode                            varchar     Clinic identifier
PreAdmissionId                      int         Linked pre-admission ID (nullable)
SymptomsUrgentlyAddressed           int         Code — urgency of symptom treatment needed (nullable)
SymptomsUrgentlyAddressedExplain    varchar     Free-text explanation for urgency
RisksofOpioid                       bit         Opioid risk counseling provided (nullable)
TreatmentOptions                    bit         Treatment options discussed (nullable)
RisksofrecognitionOpioidOverdose    bit         Overdose recognition counseling (nullable)
AvailabilityAdministration          bit         Naloxone admin counseling (nullable)
Other                               bit         Other counseling flag (nullable)
OtherTxt                            varchar     Free-text for other counseling
LevelofCareRecommendation1          bit         ASAM Level 1 recommended (nullable)
LevelofCareRecommendation21         bit         ASAM Level 2.1 recommended (nullable)
LevelofCareRecommendation25         bit         ASAM Level 2.5 recommended (nullable)
LevelofCareRecommendation31         bit         ASAM Level 3.1 recommended (nullable)
LevelofCareRecommendation33         bit         ASAM Level 3.3 recommended (nullable)
LevelofCareRecommendation35         bit         ASAM Level 3.5 recommended (nullable)
LevelofCareRecommendation37         bit         ASAM Level 3.7 recommended (nullable)
LevelofCareRecommendation4          bit         ASAM Level 4 recommended (nullable)
OpioidTreatmentServices             int         OTP services code (nullable)
WithdrawalManagement                int         Withdrawal management code (nullable)
ASAMRecommendation                  int         Overall ASAM recommendation code (nullable)
NALOC                               bit         Not at recommended LOC flag (nullable)
LOCNotAvailable                     bit         LOC not available flag (nullable)
ClinicianJudgment                   bit         Clinician judgment variance flag (nullable)
Patientpreference                   bit         Patient preference variance flag (nullable)
PatientWaitingForLOC                bit         Patient on waitlist flag (nullable)
RecommendedLOCAvailable             bit         Recommended LOC is available flag (nullable)
Geographicaccessibility             bit         Geographic barrier flag (nullable)
Familycaregiverresponsibilities     bit         Caregiver responsibility barrier (nullable)
EmploymentResponsibilities          bit         Employment responsibility barrier (nullable)
Courttreatmentrequirements          bit         Court-mandated treatment flag (nullable)
Lackofphysicalaccess                bit         Lack of physical access flag (nullable)
Languageaccessibility               bit         Language barrier flag (nullable)
LOCIsAvailable                      bit         LOC is available override (nullable)
LOCIsAvailableReason                varchar     Reason LOC is available (nullable)
Patientisineligible                 bit         Patient ineligible for recommended LOC (nullable)
PatientisineligibleReason           varchar     Reason patient is ineligible (nullable)
AdditionalComments                  varchar     Additional clinician notes (nullable)
LOCOther                            bit         Other LOC variance reason (nullable)
OtherReason                         varchar     Free-text other reason (nullable)
________________________________________

7c. dbo.VAComprehensiveAssessment — VA CA Header

Primary Key: Id

Column Name     Type        Description
-----------     ----        -----------
Id              int         Unique form instance ID
SiteCode        varchar     Clinic identifier
PreAdmissionId  int         Linked pre-admission ID (nullable)
DataFormId      int         Form template ID (nullable)
ClientId        int         Patient / client ID (nullable)
CreatedBy       varchar     User who created the record
CreatedOn       datetime    Creation timestamp (nullable, length > 6 guard)
ModifiedBy      varchar     User who last modified
ModifiedOn      datetime    Last modification timestamp (nullable, length > 6 guard)
IsDeleted       bit         True = deleted in SAMMS (nullable)

NOTE: The VA CA header is a thin record. No form-specific clinical content is stored
in the header table itself. All clinical content lives in the Summary sub-form.
________________________________________

7d. dbo.vacomprehensiveassessmentsummary — VA CA Summary Sub-form

Primary Key: Id

Column Name                     Type        Description
-----------                     ----        -----------
Id                              int         Unique summary row ID
SiteCode                        varchar     Clinic identifier
PreAdmissionId                  int         Linked pre-admission ID (nullable)
VAComprehensiveAssessmentId     int         FK to parent VA CA form Id (nullable)
DDLRecommendation               int         Dropdown recommendation code (nullable)
OpioidTreatmentServices         int         OTP services code (nullable)
WithdrawalManagement            int         Withdrawal management code (nullable)
ClinicalSummary                 varchar     Free-text clinical narrative
ASAMRecommendationForLevel      int         ASAM recommended level code (nullable)
LevelOfCareAtVariance           varchar     Variance description if placed at non-recommended level (nullable)
SummaryComments                 varchar     Additional summary comments
________________________________________

7e. dbo.NewAdmissionassessment — New Admission Assessment Header

Primary Key: Id

Column Name     Type        Description
-----------     ----        -----------
Id              int         Unique form instance ID
SiteCode        varchar     Clinic identifier
PreAdmissionId  int         Linked pre-admission ID (nullable)
DataFormId      int         Form template ID (nullable)
ClientId        int         Patient / client ID (nullable)
CreatedBy       varchar     User who created the record
CreatedOn       datetime    Creation timestamp (nullable, length > 6 guard)
ModifiedBy      varchar     User who last modified
ModifiedOn      datetime    Last modification timestamp (nullable, length > 6 guard)
IsDeleted       bit         True = deleted in SAMMS (nullable)
Version         varchar     Form template version (nullable, with length > 0 guard)

NOTE: Like the VA CA header, the NAA header is a thin record. The clinical content
lives entirely in the ASAM Dimension 6 sub-form.
________________________________________

7f. dbo.NewAdmissionassessmentASAMDimension6 — NAA ASAM Dimension 6 Sub-form

Primary Key: NewAdmissionAssessmentFormId (FK to dbo.NewAdmissionassessment.Id)

Column Name                         Type        Description
-----------                         ----        -----------
NewAdmissionAssessmentFormId        int         FK to parent NAA form Id
SiteCode                            varchar     Clinic identifier
PreAdmissionId                      int         Linked pre-admission ID (nullable)
ReadinessQuestion1..12              int         12 readiness-to-change scale items (nullable each)
StageOfChange                       varchar     Stage of change text label
AdditionalComments                  varchar     Additional comments (nullable)
TreatmentPreferences                varchar     Patient treatment preferences
HasTreatmentPreferences             bit         True = patient expressed preferences (nullable)
WillingToAttendRecommendedCare      bit         Patient willingness flag (nullable)
ReasonNotWillingToAttend            varchar     Reason unwilling (nullable)
ReasonWillNotAdmitReason            varchar     Reason facility will not admit (nullable)
ReasonPatientIneligibleReason       varchar     Reason patient ineligible (nullable)
ReasonOtherReason                   varchar     Other reason text
ClinicalSummary                     varchar     Clinical summary narrative
TransportationChallenges            bit         Social determinant — transportation (nullable)
FoodHousingInsecurity               bit         Social determinant — food/housing (nullable)
ChildcareResponsibilities           bit         Social determinant — childcare (nullable)
FinancialInsecurity                 bit         Social determinant — financial (nullable)
LackEmploymentOpportunities         bit         Social determinant — employment (nullable)
LackJobSecurity                     bit         Social determinant — job security (nullable)
LackHealthcareCoverage              bit         Social determinant — healthcare (nullable)
LackSocialSupports                  bit         Social determinant — social support (nullable)
LanguageBarriers                    bit         Social determinant — language (nullable)
Level1                              bit         ASAM Level 1 recommended (nullable)
Level1_5                            bit         ASAM Level 1.5 recommended (nullable)
Level1_7                            bit         ASAM Level 1.7 (medically managed) (nullable)
Level2_1                            bit         ASAM Level 2.1 recommended (nullable)
Level2_5                            bit         ASAM Level 2.5 recommended (nullable)
Level2_7                            bit         ASAM Level 2.7 recommended (nullable)
Level3_1                            bit         ASAM Level 3.1 recommended (nullable)
Level3_5                            bit         ASAM Level 3.5 recommended (nullable)
Level3_7                            bit         ASAM Level 3.7 recommended (nullable)
Level4                              bit         ASAM Level 4 recommended (nullable)
NonBIO                              bit         Non-biomedical pathway (nullable)
BIO                                 bit         Biomedical pathway (nullable)
COE                                 bit         COE (Center of Excellence) pathway (nullable)
ReasonNotAligned..ReasonOther       bit         Multiple variance reason flags (nullable each)
PatientSignature                    varchar     Patient signature value (nullable)
PatientSignatureBy                  varchar     Username of signing patient rep (nullable)
PatientSignatureDate                datetime    Patient signature date (nullable, length > 0 guard)
SupervisorSignature                 varchar     Supervisor signature value (nullable)
SupervisorSignatureBy               varchar     Supervisor signing username (nullable)
SupervisorSignatureDate             datetime    Supervisor signature date (nullable, length > 0 guard)
CounselorSignature                  varchar     Counselor signature value (nullable)
CounselorSignatureBy                varchar     Counselor signing username (nullable)
CounselorSignatureDate              datetime    Counselor signature date (nullable, length > 0 guard)
ProviderSignature                   varchar     Provider / physician signature value (nullable)
ProviderSignatureBy                 varchar     Provider signing username (nullable)
ProviderSignatureDate               datetime    Provider signature date (nullable, length > 0 guard)
SuperviosorSignNA                   bit         Supervisor signature N/A flag (nullable)
                                                NOTE: This is a typo in the source column name
                                                ("Superviosor" instead of "Supervisor")
________________________________________

7g. dbo.newperiodicreassessment — New Periodic Reassessment Header

Primary Key: Id

Column Name         Type        Description
-----------         ----        -----------
Id                  int         Unique form instance ID
SiteCode            varchar     Clinic identifier
PreAdmissionId      int         Linked pre-admission ID (nullable)
DataFormId          int         Form template ID (nullable)
ClientId            int         Patient / client ID (nullable)
Date                datetime    Date of the periodic reassessment (nullable, length > 6 guard)
CurrentPathway      varchar     Current treatment pathway name (nullable)
CompletedAt         int         Location where assessment was completed code (nullable)
CompletedAtOthers   varchar     Free-text completion location if "Other" (nullable)
IsDeleted           bit         True = deleted in SAMMS (nullable)
CreatedBy           varchar     User who created (nullable, length > 0 guard)
CreatedOn           datetime    Creation timestamp (nullable, length > 6 guard)
ModifiedBy          varchar     User who last modified (nullable, length > 0 guard)
ModifiedOn          datetime    Last modification timestamp (nullable, length > 6 guard)
Version             varchar     Form template version (nullable, length > 0 guard)
________________________________________

7h. dbo.newperiodicreassessmentcounselorreview — NPR Counselor Review Sub-form

Primary Key: NewPeriodicReassessmentId (FK to dbo.newperiodicreassessment.Id)

Column Name                             Type        Description
-----------                             ----        -----------
NewPeriodicReassessmentId               int         FK to parent NPR form Id
SiteCode                                varchar     Clinic identifier
PreAdmissionId                          int         Linked pre-admission ID (nullable)
Level1, Level1_5, Level1_7              bit         ASAM level recommendations (nullable each)
Level2_1, Level2_5, Level2_7            bit         ASAM level 2.x recommendations (nullable each)
Level3_1, Level3_5, Level3_7            bit         ASAM level 3.x recommendations (nullable each)
Level4, BIO, NonBIO, COE               bit         Additional pathway flags (nullable each)
ReasonNotAligned..ReasonOther          bit         Placement variance reason flags (nullable each)
ReasonWillNotAdmitReason                varchar     Free-text reason clinic won't admit (nullable)
ReasonPatientIneligibleReason           varchar     Free-text patient ineligibility reason (nullable)
ReasonOtherReason                       varchar     Free-text other reason (nullable)
CopePhase1                              bit         COPE Phase 1 pathway flag (nullable)
CopePhase2                              bit         COPE Phase 2 pathway flag (nullable)
CopePhase3                              bit         COPE Phase 3 pathway flag (nullable)
Induction                               bit         MAT induction phase flag (nullable)
Stabilization                           bit         MAT stabilization phase flag (nullable)
Maintenance                             bit         MAT maintenance phase flag (nullable)
ClinicalSummary                         varchar     Free-text clinical summary narrative
DateCompleted                           datetime    Date the counselor review was completed (nullable, length > 6)
UseScore                                int         BAM substance use subscale score (nullable)
RiskScore                               int         BAM risk factor subscale score (nullable)
ProtectiveScore                         int         BAM protective factor subscale score (nullable)
PatientSignature                        varchar     Patient signature value (nullable)
PatientSignatureBy                      varchar     Patient signing username (nullable)
PatientSignatureDate                    datetime    Patient signature date (nullable, length > 6 guard)
CounselorSignature                      varchar     Counselor signature value (nullable)
CounselorSignatureBy                    varchar     Counselor signing username (nullable)
CounselorSignatureDate                  datetime    Counselor signature date (nullable, length > 0 guard)
ProviderSignature                       varchar     Provider signature value (nullable)
ProviderSignatureBy                     varchar     Provider signing username (nullable)
ProviderSignatureDate                   datetime    Provider signature date (nullable, length > 6 guard)
SupervisorSignature                     varchar     Supervisor signature value (nullable)
SupervisorSignatureBy                   varchar     Supervisor signing username (nullable)
SupervisorSignatureDate                 datetime    Supervisor signature date (nullable, length > 6 guard)
RR                                      bit         Requires physician review flag (nullable)
________________________________________

8. SaveMNCA — EF Core Upsert (Minnesota CA Header)

File: BCAppCode/BHG-DR-LIB/SaveCA.cs
Class: SaveData (partial class)
Method: SaveMNCA()

Method signature:
    public RCodes SaveMNCA(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)

Returns: RCodes — IsResult, RowsProcessed, RowsIns, ExceptMsg, ExceptInnerMsg

EF Core upsert logic — step by step:

Step 1 — Guard: only proceed if tbl.Rows.Count > 0
    if (tbl.Rows.Count > 0) { ... }
    (If SAMMS returned no rows, method returns immediately with initial RCodes — no DB calls)

Step 2 — Create EF context, capture run time
    DateTime runat = DateTime.Now
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Load all existing Azure rows for this site
    List<TblMNComprehensiveAssessment> dbList =
        db.TblMNComprehensiveAssessments.Where(x => x.SiteCode == sc).ToList()

Step 4 — For each SAMMS row, build entity via column switch:

    Source Column       Destination Field       Guard / Transformation
    sitecode            ca.SiteCode = sc        Uses parameter sc; also sets LastModAt = runat
    id                  ca.Id                   int.Parse — always
    preadmissionid      ca.PreAdmissionId       int — only if length > 0
    dataformid          ca.DataFormId           int — only if length > 0
    clientid            ca.ClientId             int — only if length > 0
    todaydate           ca.TodayDate            DateTime — only if length > 0
                                                NOTE: uses length > 0, not > 6 — weaker guard
    referradby          ca.ReferradBy           int — only if length > 0
    referradbyother     ca.ReferradByOther      Always (string)
    referralreason      ca.ReferralReason       int — only if length > 0
    referralreasonother ca.ReferralReasonOther  Always (string)
    insuranceid         ca.InsuranceId          Always (string)
    createdby           ca.CreatedBy            Always (string)
    createdon           ca.CreatedOn            DateTime — only if length > 6
    modifiedby          ca.ModifiedBy           Always (string)
    modifiedon          ca.ModifiedOn           DateTime — only if length > 6
    version             ca.Version              Always (string)
    isdeleted           ca.IsDeleted            bool — only if length > 0

Step 5 — Lookup existing record
    TblMNComprehensiveAssessment dbca = dbList
        .FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id)

Step 6 — Insert or Update
    if (dbca == null):
        rc.RowsIns += 1
        NewItems.Add(ca)
    else:
        rc.RowsUpd += 1 (implicit — not explicitly counted)
        (copy all fields: ClientId, CreatedBy, CreatedOn, DataFormId, InsuranceId,
         IsDeleted, LastModAt, ModifiedBy, ModifiedOn, PreAdmissionId, ReferradBy,
         ReferradByOther, ReferralReason, ReferralReasonOther, TodayDate, Version)

Step 7 — Two-phase commit
    db.SaveChanges()
    if (NewItems.Count > 0):
        db.TblMNComprehensiveAssessments.AddRange(NewItems)
        db.SaveChanges()
________________________________________

9. SaveMNCALOC — EF Core Upsert (MN CA Level of Care)

Method: SaveMNCALOC()
Composite key: SiteCode + MNComprehensiveAssessmentFormId

The MN CA Level of Care method uses the same guard, load, switch, and two-phase
commit pattern as SaveMNCA. Key differences:
- Lookup key is MNComprehensiveAssessmentFormId (not Id)
- Column set is the full LOC sub-form column list (see Section 7b)
- All bit fields use bool.Parse with length > 0 guard
- All int fields use int.Parse with length > 0 guard
- Two string fields (SymptomsUrgentlyAddressedExplain, OtherTxt) are always assigned
- Three string fields (AdditionalComments, LOCIsAvailableReason, PatientisineligibleReason, OtherReason) use length > 0 guard
- LastModAt IS set (runat assigned in "sitecode" case)
________________________________________

10. SaveVACA — EF Core Upsert (VA CA Header)

Method: SaveVACA()
Composite key: SiteCode + Id

Minimal column set — header-only record. Fields: SiteCode, Id, PreAdmissionId,
DataFormId, ClientId, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn, IsDeleted.
LastModAt IS set in the "sitecode" case.

Update block copies: ClientId, CreatedBy, CreatedOn, DataFormId, IsDeleted,
LastModAt, ModifiedBy, ModifiedOn, PreAdmissionId.

NOTE: No Version field in the VA CA header unlike MN CA and NAA headers.
________________________________________

11. SaveVACASummary — EF Core Upsert (VA CA Summary)

Method: SaveVACASummary()
Composite key: SiteCode + Id

Column set: SiteCode, Id, PreAdmissionId, VAComprehensiveAssessmentId,
DDLRecommendation, OpioidTreatmentServices, WithdrawalManagement, ClinicalSummary,
ASAMRecommendationForLevel, LevelOfCareAtVariance, SummaryComments.

ANOMALY — LastModAt is commented out:
    case "sitecode":
        ca.SiteCode = sc;
        //ca.LastModAt = runat;   ← COMMENTED OUT
        break;

The LastModAt timestamp is never set for VA CA Summary records. The runat variable
is declared but the assignment is disabled. This means LastModAt remains null (or its
existing value) for all VA CA Summary rows — the ETL audit timestamp field does not
function in this method.

Update block copies: ASAMRecommendationForLevel, ClinicalSummary, DDLRecommendation,
LevelOfCareAtVariance, OpioidTreatmentServices, PreAdmissionId, SummaryComments,
VAComprehensiveAssessmentId, WithdrawalManagement.
________________________________________

12. SaveNewAdmissionAssessment — EF Core Upsert (NAA Header)

Method: SaveNewAdmissionAssessment()
Composite key: SiteCode + Id

Minimal column set (header only): SiteCode, Id, PreAdmissionId, DataFormId,
ClientId, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn, IsDeleted, Version.

Version field uses length > 0 guard here (unlike SaveMNCA where Version is always assigned).
LastModAt IS set.

Update block: ClientId, CreatedBy, CreatedOn, DataFormId, IsDeleted, LastModAt,
ModifiedBy, ModifiedOn, PreAdmissionId, Version.
________________________________________

13. SaveNewAdmissionAssessmentASAMDimension6 — EF Core Upsert (NAA ASAM Dim 6)

Method: SaveNewAdmissionAssessmentASAMDimension6()
Composite key: SiteCode + NewAdmissionAssessmentFormId

This is the most data-rich method in SaveCA.cs. Full column mapping:

    Source Column                   Destination Field                   Guard
    sitecode                        ca.SiteCode = sc; LastModAt = runat  Always
    newadmissionassessmentformid    ca.NewAdmissionAssessmentFormId      int.Parse always
    preadmissionid                  ca.PreAdmissionId                    int — length > 0
    readinessquestion1..12          ca.ReadinessQuestion1..12            int — length > 0 (each)
    stageofchange                   ca.StageOfChange                     Always (string)
    additionalcomments              ca.AdditionalComments                length > 0
    treatmentpreferences            ca.TreatmentPreferences              Always (string)
    reasonnotwillingtoattend        ca.ReasonNotWillingToAttend          length > 0
    reasonwillnotadmitreason        ca.ReasonWillNotAdmitReason          length > 0
    reasonpatientineligiblereason   ca.ReasonPatientIneligibleReason     length > 0
    reasonotherreason               ca.ReasonOtherReason                 Always (string)
    clinicalsummary                 ca.ClinicalSummary                   Always (string)
    hastreatmentpreferences         ca.HasTreatmentPreferences           bool — length > 0
    willingtoattendrecommendedcare  ca.WillingToAttendRecommendedCare    bool — length > 0
    transportationchallenges        ca.TransportationChallenges          bool — length > 0
    foodhousinginsecurity           ca.FoodHousingInsecurity             bool — length > 0
    childcareresponsibilities       ca.ChildcareResponsibilities         bool — length > 0
    financialinsecurity             ca.FinancialInsecurity               bool — length > 0
    lackemploymentopportunities     ca.LackEmploymentOpportunities       bool — length > 0
    lackjobsecurity                 ca.LackJobSecurity                   bool — length > 0
    lackhealthcarecoverage          ca.LackHealthcareCoverage            bool — length > 0
    lacksocialsupports              ca.LackSocialSupports                bool — length > 0
    languagebarriers                ca.LanguageBarriers                  bool — length > 0
    level1                          ca.Level1                            bool — length > 0
    level1_5                        ca.Level1_5                          bool — length > 0
    level1_7                        ca.Level1_7                          bool — length > 0
    level2_1                        ca.Level2_1                          bool — length > 0
    level2_5                        ca.Level2_5                          bool — length > 0
    level2_7                        ca.Level2_7                          bool — length > 0
    level3_1                        ca.Level3_1                          bool — length > 0
    level3_5                        ca.Level3_5                          bool — length > 0
    level3_7                        ca.Level3_7                          bool — length > 0
    nonbio                          ca.NonBIO                            bool — length > 0
    bio                             ca.BIO                               bool — length > 0
    level4                          ca.Level4                            bool — length > 0
    coe                             ca.COE                               bool — length > 0
    reasonnotaligned                ca.ReasonNotAligned                  bool — length > 0
    reasonnotavailable              ca.ReasonNotAvailable                bool — length > 0
    reasonclinicianjudgment         ca.ReasonClinicianJudgment           bool — length > 0
    reasonpatientpreference         ca.ReasonPatientPreference           bool — length > 0
    reasononwaitinglist             ca.ReasonOnWaitingList               bool — length > 0
    reasonlackspayment              ca.ReasonLacksPayment                bool — length > 0
    reasongeographicaccess          ca.ReasonGeographicAccess            bool — length > 0
    reasoncaregiverresponsibilities ca.ReasonCaregiverResponsibilities   bool — length > 0
    reasonemploymentresponsibilities ca.ReasonEmploymentResponsibilities  bool — length > 0
    reasoncourtrequirements         ca.ReasonCourtRequirements           bool — length > 0
    reasontransportationchallenges  ca.ReasonTransportationChallenges    bool — length > 0
    reasonlanguageaccessibility     ca.ReasonLanguageAccessibility       bool — length > 0
    reasonwillnotadmit              ca.ReasonWillNotAdmit                bool — length > 0
    reasonpatientineligible         ca.ReasonPatientIneligible           bool — length > 0
    reasonother                     ca.ReasonOther                       bool — length > 0
    patientsignature                ca.PatientSignature                  length > 0
    patientsignatureby              ca.PatientSignatureBy                length > 0
    patientsignaturedate            ca.PatientSignatureDate              DateTime — length > 0
                                                                         (WEAK guard — see anomalies)
    supervisorsignature             ca.SupervisorSignature               length > 0
    supervisorsignatureby           ca.SupervisorSignatureBy             length > 0
    supervisorsignaturedate         ca.SupervisorSignatureDate           DateTime — length > 0 (WEAK)
    counselorsignature              ca.CounselorSignature                length > 0
    counselorsignatureby            ca.CounselorSignatureBy              length > 0
    counselorsignaturedate          ca.CounselorSignatureDate            DateTime — length > 0 (WEAK)
    providersignature               ca.ProviderSignature                 length > 0
    providersignatureby             ca.ProviderSignatureBy               length > 0
    providersignaturedate           ca.ProviderSignatureDate             DateTime — length > 0 (WEAK)
    superviosorsignna               ca.SuperviosorSignNA                 bool — length > 0
________________________________________

14. SaveNewPeriodicReassessment — EF Core Upsert (NPR Header)

Method: SaveNewPeriodicReassessment()
Composite key: SiteCode + Id

ANOMALY — LastModAt is commented out (same as SaveVACASummary):
    case "sitecode":
        ca.SiteCode = sc;
        //ca.LastModAt = runat;   ← COMMENTED OUT

LastModAt is never set for NPR header records.

Column set: SiteCode, Id, PreAdmissionId, DataFormId, ClientId, Date, CurrentPathway,
CompletedAt, CompletedAtOthers, IsDeleted, CreatedBy, CreatedOn, ModifiedBy, ModifiedOn, Version.

Note: CreatedBy and ModifiedBy in this method use length > 0 guard (unlike SaveMNCA
which assigns them unconditionally). This means empty strings are not stored.

Update block copies: PreAdmissionId, ClientId, CompletedAt, CompletedAtOthers,
CreatedBy, CreatedOn, CurrentPathway, DataFormId, Date, IsDeleted, ModifiedBy,
ModifiedOn, Version.
________________________________________

15. Savenewperiodicreassessmentcounselorreview — EF Core Upsert (NPR Counselor Review)

Method: Savenewperiodicreassessmentcounselorreview()
Composite key: SiteCode + NewPeriodicReassessmentId

ANOMALY — LastModAt is commented out:
    case "sitecode":
        ca.SiteCode = sc;
        //ca.LastModAt = runat;   ← COMMENTED OUT

CRITICAL BUG — CopePhase1 and CopePhase2 are swapped:
    case "copephase1":
        ca.CopePhase2 = bool.Parse(...)   ← stores SAMMS copephase1 into CopePhase2
        break;
    case "copephase2":
        ca.CopePhase1 = bool.Parse(...)   ← stores SAMMS copephase2 into CopePhase1
        break;
    case "copephase3":
        ca.CopePhase3 = bool.Parse(...)   ← Phase 3 is correctly mapped

This is a confirmed column-name-to-property swap bug. In Azure, the CopePhase1 and
CopePhase2 fields contain the opposite values from what SAMMS recorded. Phase 3 is
unaffected. Every row processed by this method has Phase 1 and Phase 2 silently reversed.

ANOMALY — CounselorSignatureDate uses length > 0 guard (not > 6):
All other signature date fields in this method use length > 6. CounselorSignatureDate
uses length > 0, meaning a 1–6 character string that is not a valid date will pass
the guard and throw a DateTime.Parse exception.

Full column set (key fields):
NewPeriodicReassessmentId, SiteCode, PreAdmissionId, Level 1–4 and sub-level flags,
BIO/NonBIO/COE, Reason flags, CopePhase1..3 (see swap bug above), Induction,
Stabilization, Maintenance, ClinicalSummary, DateCompleted, UseScore, RiskScore,
ProtectiveScore, PatientSignature/By/Date, CounselorSignature/By/Date,
ProviderSignature/By/Date, SupervisorSignature/By/Date, RR.
________________________________________

16. Destination Tables — Azure BHG_DR (pats schema)

All 8 destination tables follow the composite primary key pattern (SiteCode + form-specific Id).
________________________________________

16a. pats.tbl_MNComprehensiveAssessment
EF Model: TblMNComprehensiveAssessment
Primary Key: SiteCode + Id (composite)
Key clinical columns: PreAdmissionId, ClientId, TodayDate, ReferradBy, ReferralReason, InsuranceId, Version, IsDeleted, LastModAt
________________________________________

16b. pats.tbl_MNComprehensiveAssessmentLevelOfCare
EF Model: TblMNComprehensiveAssessmentLevelOfCare
Primary Key: SiteCode + MNComprehensiveAssessmentFormId (composite)
Key clinical columns: SymptomsUrgentlyAddressed, all LOC recommendation bits, ASAMRecommendation, NALOC, LOCNotAvailable, all accessibility barrier bits, LastModAt
________________________________________

16c. pats.tbl_VAComprehensiveAssessment
EF Model: TblVAComprehensiveAssessment
Primary Key: SiteCode + Id (composite)
Key clinical columns: PreAdmissionId, ClientId, DataFormId, CreatedOn, ModifiedOn, IsDeleted, LastModAt
________________________________________

16d. pats.tbl_VAComprehensiveAssessmentSummary
EF Model: TblVAComprehensiveAssessmentSummary
Primary Key: SiteCode + Id (composite)
Key clinical columns: VAComprehensiveAssessmentId, DDLRecommendation, ASAMRecommendationForLevel, LevelOfCareAtVariance, ClinicalSummary, SummaryComments
NOTE: LastModAt is never populated by ETL (commented out in source code)
________________________________________

16e. pats.tbl_NewAdmissionAssessment
EF Model: TblNewAdmissionAssessment
Primary Key: SiteCode + Id (composite)
Key clinical columns: PreAdmissionId, ClientId, DataFormId, Version, IsDeleted, LastModAt
________________________________________

16f. pats.tbl_NewAdmissionAssessmentASAMDimension6
EF Model: TblNewAdmissionAssessmentASAMDimension6
Primary Key: SiteCode + NewAdmissionAssessmentFormId (composite)
Key clinical columns: ReadinessQuestion1–12, StageOfChange, all Level flags, all SDOH barrier flags, all Reason flags, four-party signature block, ClinicalSummary, LastModAt
NOTE: All four signature date fields use the weaker length > 0 DateTime guard
________________________________________

16g. pats.tbl_NewPeriodicReassessment
EF Model: TblNewPeriodicReassessment
Primary Key: SiteCode + Id (composite)
Key clinical columns: PreAdmissionId, ClientId, Date, CurrentPathway, CompletedAt, Version, IsDeleted
NOTE: LastModAt is never populated by ETL (commented out in source code)
________________________________________

16h. pats.tbl_NewPeriodicReassessmentCounselorReview
EF Model: TblNewPeriodicReassessmentCounselorReview
Primary Key: SiteCode + NewPeriodicReassessmentId (composite)
Key clinical columns: Level flags, ASAM placement, CopePhase1–3 (Phase 1 and 2 are swapped!), MAT phase, BAM scores (UseScore/RiskScore/ProtectiveScore), four-party signature block, RR, ClinicalSummary
NOTE: LastModAt never populated; CopePhase1/2 bug present
________________________________________

17. Change Detection

None of the eight methods implement RowChkSum-based change detection.

All eight methods use an always-update pattern — every existing record that matches
the composite key will have all its mapped fields overwritten on every run, regardless
of whether the data has changed since the last ETL execution.
________________________________________

18. RowState — Soft Delete Tracking

None of the eight methods implement RowState soft-delete tracking.

There is no pre-pass to mark existing records inactive. Records that disappear from
the SAMMS source extract (due to the task WhereCondition date scope or deletion from
SAMMS) are not marked inactive in Azure. They remain with their last-seen field values.

The IsDeleted field (present in most header tables) propagates the SAMMS soft-delete
flag directly — this is not an ETL-managed RowState pattern but rather a data field.
________________________________________

19. Load Design Summary

Load type: Incremental upsert — no RowChkSum, no RowState, no pre-pass reset

Unique feature: Table-existence pre-check before any data movement.

Per run behavior for all 8 methods:

  Pre-check: sys.tables probe — if table missing in SAMMS → mark task failed, skip
  Source query: st.WhereCondition + DaysBack(-15) + st.SortOrder
  1. Guard: if tbl.Rows.Count == 0, return immediately (no DB calls)
  2. Load ALL existing Azure rows for this SiteCode into memory
  3. For each SAMMS source row:
       - Build entity via dynamic column switch (all columns iterated)
       - Match by composite key in memory
       - Not found  → stage in NewItems list; RowsIns++
       - Found      → overwrite all mapped fields on existing entity (no checksum check)
  4. db.SaveChanges() — commit all updates
  5. if NewItems.Count > 0: AddRange + db.SaveChanges() — batch insert
________________________________________

20. Error Handling and Recovery

All eight methods use the same error handling pattern:

    try
    {
        if (tbl.Rows.Count > 0)
        {
            // load + loop + SaveChanges() ...
        }
    }
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
    return rc

If an EF Core exception occurs:
- The current batch for that clinic is not committed
- RCodes.IsResult = false is returned to BHGTaskRunner
- BHGTaskRunner logs the task error in the task row

There are two failure paths for CA tasks:
1. Table-existence check failure: BHGTaskRunner sets IsResult=false, ExceptMsg="Table does not exists." before calling the Save method. The Save method is never invoked.
2. Save method EF Core exception: caught inside the Save method, returned as IsResult=false.

Recovery: The Scheduler's daily reset restores failed tasks to Status=17:
    update tsk.tbl_Tasks set Status = 17 where Status = 18
A failed CA run for a clinic will automatically be retried the next day.
________________________________________

21. RowTrax — Audit and Row Count Tracking

For tasks where st.RowTrax = true (and SiteCode != "PHC"), BHGTaskRunner logs:
- Source row count: count(*) against the SAMMS source using task WhereCondition
- Destination row count: count(*) against the Azure target table for that SiteCode

Stored in tsk.tbl_RowTrax for compliance and data completeness monitoring.
________________________________________

22. Key Design Notes and Gotchas

Table-existence check (all 8 methods):
BHGTaskRunner uses "select name from sys.tables t where name = '{TableName}'" against
the clinic's SAMMS SQL Server before each CA task. If the clinic does not have the
form table, the entire task is skipped with "Table does not exists." (note: typo in
the code — the period is "exists." rather than the grammatically correct "exist.").
This design was intentional — CA form tables were added to SAMMS incrementally.

LastModAt commented out (SaveVACASummary, SaveNewPeriodicReassessment,
Savenewperiodicreassessmentcounselorreview):
In three of the eight methods, the `ca.LastModAt = runat` assignment is commented out
in the "sitecode" case. These are the only methods in SaveCA.cs where the ETL audit
timestamp is not populated. The runat variable is still declared (DateTime runat =
DateTime.Now) but its value is never used — it is dead code in these three methods.

CopePhase1 / CopePhase2 swap in Savenewperiodicreassessmentcounselorreview:
The source column "copephase1" is stored in ca.CopePhase2, and "copephase2" is stored
in ca.CopePhase1. This is a confirmed property-assignment transposition bug. Every
periodic reassessment counselor review row in Azure has CopePhase1 and CopePhase2
values reversed relative to what was entered in SAMMS. Phase 3 is correctly mapped.
Only a targeted data correction (update pats.tbl_NewPeriodicReassessmentCounselorReview
swapping CopePhase1 and CopePhase2 values) can remediate existing Azure data.

Signature date guards — length > 0 vs length > 6:
SaveNewAdmissionAssessmentASAMDimension6: all four signature dates use length > 0.
Savenewperiodicreassessmentcounselorreview: CounselorSignatureDate uses length > 0;
PatientSignatureDate, ProviderSignatureDate, SupervisorSignatureDate use length > 6.
A single non-empty character that is not a valid date will pass length > 0 and throw a
DateTime.Parse exception at runtime. The defensive standard in this codebase is > 6.

TodayDate in SaveMNCA uses length > 0 (not > 6):
The MN CA header's assessment date field (TodayDate) uses the weaker length > 0 guard.

Duplicate case labels in BHGTaskRunner:
Two CA tasks have duplicate case labels in the BHGTaskRunner switch:
    "pats.tbl_vacomprehensiveassessmentsummary" AND "pats.pats.tbl_vacomprehensiveassessmentsummary"
    "pats.tbl_newadmissionassessment"           AND "pats.pats.tbl_newadmissionassessment"
The "pats.pats." prefix is a double-schema prefix that appears to be a data entry error
in the task configuration. Both case labels route to the same Save method, so if a task
row exists with the malformed double-prefix TaskName it is still handled correctly.

SuperviosorSignNA field name typo (SaveNewAdmissionAssessmentASAMDimension6):
The source column name in SAMMS is "superviosorsignna" (with "iosor" instead of "isor").
The C# model property is SuperviosorSignNA (preserving the typo). This must remain
consistent between SAMMS, the C# model, and Azure — any "correction" to the spelling
in one place without changing the others will break the mapping silently.

No RowsUpd counter:
The RowsIns counter is explicitly incremented. The RowsUpd (update count) is not
incremented in any of the eight methods. BHGTaskRunner will see RowsIns but no update
count. This is consistent with SaveBAM.cs behaviour.
________________________________________

23. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: Samms-Forms (Status=17, SiteCode='All')
        |-- insert child tasks per clinic (8 task types x 80+ clinics):
        |       pats.tbl_mncomprehensiveassessment
        |       pats.tbl_mncomprehensiveassessmentlevelofcare
        |       pats.tbl_vacomprehensiveassessment
        |       pats.tbl_vacomprehensiveassessmentsummary
        |       pats.tbl_newadmissionassessment
        |       pats.tbl_newadmissionassessmentasamdimension6
        |       pats.tbl_newperiodicreassessment
        |       pats.tbl_newperiodicreassessmentcounselorreview
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 6
        |
        |-- filter: TaskName = 'Samms-Forms', SiteCode != 'PHC', Status=17
        |-- mark parent task Status=18 (running)
        |
        |-- for each child task (one per clinic per CA table type):
        |
        |   Build strCmd via SelectConstructor (ActionKey=6, DaysBack=-15)
        |   strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
        |
        |   ======= TABLE EXISTENCE PRE-CHECK (ALL 8 TASKS) =======
        |   sm.GetTableData(st.FromTblVw,
        |       "select name from sys.tables t where name = '{SourceTableName}'",
        |       st.ConStr)
        |   if (SrcDt.Rows.Count != 1):
        |       → rCodes.IsResult = false, ExceptMsg = "Table does not exists."
        |       → skip to RowTrax + mark task complete
        |
        |   if table exists:
        |       strCmd += " Where " + strWhere + " " + st.SortOrder
        |       SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        |======================================================
        |  BRANCH A: pats.tbl_mncomprehensiveassessment
        |======================================================
        |   → sd.SaveMNCA(SrcDt, st.SiteCode, WorkDate.AddDays(-15), null)
        |       Load all TblMNComprehensiveAssessment where SiteCode = sc
        |       Loop rows → build entity via switch → match by SiteCode + Id
        |       not found → NewItems.Add    found → overwrite all fields
        |       SaveChanges() → AddRange(NewItems) → SaveChanges()
        |       → pats.tbl_MNComprehensiveAssessment (Azure)
        |
        |======================================================
        |  BRANCH B: pats.tbl_mncomprehensiveassessmentlevelofcare
        |======================================================
        |   → sd.SaveMNCALOC(...)
        |       Lookup by SiteCode + MNComprehensiveAssessmentFormId
        |       → pats.tbl_MNComprehensiveAssessmentLevelOfCare (Azure)
        |
        |======================================================
        |  BRANCH C: pats.tbl_vacomprehensiveassessment
        |======================================================
        |   → sd.SaveVACA(...)
        |       Lookup by SiteCode + Id
        |       → pats.tbl_VAComprehensiveAssessment (Azure)
        |
        |======================================================
        |  BRANCH D: pats.tbl_vacomprehensiveassessmentsummary
        |======================================================
        |   → sd.SaveVACASummary(...)
        |       Lookup by SiteCode + Id
        |       LastModAt NOT set (commented out)
        |       → pats.tbl_VAComprehensiveAssessmentSummary (Azure)
        |
        |======================================================
        |  BRANCH E: pats.tbl_newadmissionassessment
        |======================================================
        |   → sd.SaveNewAdmissionAssessment(...)
        |       Lookup by SiteCode + Id
        |       → pats.tbl_NewAdmissionAssessment (Azure)
        |
        |======================================================
        |  BRANCH F: pats.tbl_newadmissionassessmentasamdimension6
        |======================================================
        |   → sd.SaveNewAdmissionAssessmentASAMDimension6(...)
        |       Lookup by SiteCode + NewAdmissionAssessmentFormId
        |       All 4 signature date fields: length > 0 guard (weaker)
        |       → pats.tbl_NewAdmissionAssessmentASAMDimension6 (Azure)
        |
        |======================================================
        |  BRANCH G: pats.tbl_newperiodicreassessment
        |======================================================
        |   → sd.SaveNewPeriodicReassessment(...)
        |       Lookup by SiteCode + Id
        |       LastModAt NOT set (commented out)
        |       → pats.tbl_NewPeriodicReassessment (Azure)
        |
        |======================================================
        |  BRANCH H: pats.tbl_newperiodicreassessmentcounselorreview
        |======================================================
        |   → sd.Savenewperiodicreassessmentcounselorreview(...)
        |       Lookup by SiteCode + NewPeriodicReassessmentId
        |       LastModAt NOT set (commented out)
        |       CopePhase1 ↔ CopePhase2 SWAPPED (bug)
        |       CounselorSignatureDate: length > 0 (weaker guard)
        |       → pats.tbl_NewPeriodicReassessmentCounselorReview (Azure)
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != PHC)
        |       source count = count from SAMMS source view
        |       dest count   = count from Azure target table where SiteCode = sc
        |       → tsk.tbl_RowTrax
        |
        V
BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)
________________________________________

24. File Reference Map

File Path                                                       Purpose
---------                                                       -------
BCAppCode/Scheduler/Program.cs                                  Creates daily task queue — inserts Samms-Forms tasks
BCAppCode/BHGTaskRunner/Program.cs                              Main ETL driver (arg=6 → Samms-Forms)
                                                                Cases: pats.tbl_mncomprehensiveassessment            ~line 3131
                                                                        pats.tbl_mncomprehensiveassessmentlevelofcare ~line 3145
                                                                        pats.tbl_vacomprehensiveassessment            ~line 3159
                                                                        pats.tbl_vacomprehensiveassessmentsummary     ~line 3173
                                                                        pats.tbl_newadmissionassessment               ~line 3188
                                                                        pats.tbl_newadmissionassessmentasamdimension6 ~line 3203
                                                                        pats.tbl_newperiodicreassessment              ~line 3217
                                                                        pats.tbl_newperiodicreassessmentcounselorreview ~line 3231
BCAppCode/BHG-DR-LIB/SaveCA.cs                                  All 8 EF Core upsert methods (2015 lines)
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                          ADO.NET wrapper — sys.tables probe + full SELECT
BCAppCode/BHG-DR-LIB/Models/TblMNComprehensiveAssessment.cs    EF Model → pats.tbl_MNComprehensiveAssessment
BCAppCode/BHG-DR-LIB/Models/TblMNComprehensiveAssessmentLevelOfCare.cs  EF Model
BCAppCode/BHG-DR-LIB/Models/TblVAComprehensiveAssessment.cs    EF Model → pats.tbl_VAComprehensiveAssessment
BCAppCode/BHG-DR-LIB/Models/TblVAComprehensiveAssessmentSummary.cs      EF Model
BCAppCode/BHG-DR-LIB/Models/TblNewAdmissionAssessment.cs       EF Model → pats.tbl_NewAdmissionAssessment
BCAppCode/BHG-DR-LIB/Models/TblNewAdmissionAssessmentASAMDimension6.cs  EF Model
BCAppCode/BHG-DR-LIB/Models/TblNewPeriodicReassessment.cs      EF Model → pats.tbl_NewPeriodicReassessment
BCAppCode/BHG-DR-LIB/Models/TblNewPeriodicReassessmentCounselorReview.cs EF Model
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                   EF DbContext — registers all 8 CA DbSets
________________________________________

25. Quick Reference Summary

What triggers CA ETL?                    Scheduler.exe creates tasks, BHGTaskRunner.exe 6 processes them
Schedule?                                Schedule 6 — Samms-Forms
TaskName in scheduler (parent)?          Samms-Forms
TaskNames (children)?                    pats.tbl_mncomprehensiveassessment
                                         pats.tbl_mncomprehensiveassessmentlevelofcare
                                         pats.tbl_vacomprehensiveassessment
                                         pats.tbl_vacomprehensiveassessmentsummary
                                         pats.tbl_newadmissionassessment
                                         pats.tbl_newadmissionassessmentasamdimension6
                                         pats.tbl_newperiodicreassessment
                                         pats.tbl_newperiodicreassessmentcounselorreview
How many methods?                        8 (SaveMNCA, SaveMNCALOC, SaveVACA, SaveVACASummary,
                                         SaveNewAdmissionAssessment, SaveNewAdmissionAssessmentASAMDimension6,
                                         SaveNewPeriodicReassessment, Savenewperiodicreassessmentcounselorreview)
Source tables in SAMMS?                  dbo.MNComprehensiveAssessment
                                         dbo.MNComprehensiveAssessmentlevelofcare
                                         dbo.VAComprehensiveAssessment
                                         dbo.vacomprehensiveassessmentsummary
                                         dbo.NewAdmissionassessment
                                         dbo.NewAdmissionassessmentASAMDimension6
                                         dbo.newperiodicreassessment
                                         dbo.newperiodicreassessmentcounselorreview
Table existence guard?                   YES — all 8 tasks check sys.tables before proceeding
                                         If table absent: task fails with "Table does not exists."
EF Core or Bulk?                         All 8 — EF Core only. No bulk path.
Staging table?                           None
Primary keys?                            MN CA header: SiteCode + Id
                                         MN CA LOC: SiteCode + MNComprehensiveAssessmentFormId
                                         VA CA header: SiteCode + Id
                                         VA CA summary: SiteCode + Id
                                         NAA header: SiteCode + Id
                                         NAA ASAM Dim 6: SiteCode + NewAdmissionAssessmentFormId
                                         NPR header: SiteCode + Id
                                         NPR counselor review: SiteCode + NewPeriodicReassessmentId
Change detection (RowChkSum)?            Not used in any method — always-update pattern
RowState soft-delete?                    Not used in any method
IsDeleted field?                         Present in MN CA, VA CA, NAA headers (propagates SAMMS flag)
Lookback window?                         DaysBack = -15 days via st.WhereCondition
PHC handled here?                        No — PHC excluded from Samms-Forms filter
RowTrax audit?                           Source count vs destination count per site (where enabled)
Error recovery?                          Scheduler resets failed tasks to Status=17 next daily run
Which methods have LastModAt missing?    SaveVACASummary, SaveNewPeriodicReassessment,
                                         Savenewperiodicreassessmentcounselorreview (commented out)
CopePhase bug?                           YES — in Savenewperiodicreassessmentcounselorreview:
                                         SAMMS copephase1 → Azure CopePhase2 (SWAPPED)
                                         SAMMS copephase2 → Azure CopePhase1 (SWAPPED)
                                         CopePhase3 is correctly mapped
Signature date guard inconsistency?      SaveNewAdmissionAssessmentASAMDimension6: all 4 dates use length > 0
                                         Savenewperiodicreassessmentcounselorreview: CounselorSignatureDate uses length > 0
Duplicate TaskName in BHGTaskRunner?     YES: "pats.pats.tbl_vacomprehensiveassessmentsummary" and
                                         "pats.pats.tbl_newadmissionassessment" — double-prefix variants
                                         that route to the same Save method
Column name typo in SAMMS?              "superviosorsignna" (should be "supervisorsignna") — preserved in model
________________________________________

Documentation generated from source: BHG-DR-LIB\SaveCA.cs (2015 lines, 8 methods).
Parent Schedule: Samms-Forms (Schedule 6 — BHGTaskRunner.exe 6)
Clinical domains: Minnesota CA, VA CA, New Admission Assessment, New Periodic Reassessment.
