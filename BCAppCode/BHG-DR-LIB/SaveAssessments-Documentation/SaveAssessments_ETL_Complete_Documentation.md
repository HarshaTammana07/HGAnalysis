
Clinical Assessments ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract clinical assessment and
re-assessment form data from local SAMMS SQL Server databases at each clinic and load them into
the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What clinical Assessment and Re-Assessment data is and why it exists
- What all 19 save methods in SaveAssessments.cs do and how they relate to each other
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How SelectConstructor builds the source SQL
- How SQLSvrManager executes against the clinic databases
- What the source tables look like and all their columns
- What the destination tables look like and all their columns
- How the standard always-update pattern works across 17 methods
- How RowChkSum and RowState work in the two Substance Use History methods
- How the batching optimization works across all methods
- How RowTrax audit tracking works
- What happens when errors occur
- All known anomalies and quirks in the code
________________________________________

2. High-Level Business Summary

What is Clinical Assessment data?

An Assessment in SAMMS represents a comprehensive multi-dimensional clinical evaluation of
a patient at the time of admission to a BHG clinic. BHG uses the ASAM (American Society of
Addiction Medicine) Patient Placement Criteria, which organizes patient evaluation across six
dimensions. Each dimension assesses a different aspect of the patient's condition and need for
care:

    Dimension 1 — Acute Intoxication and Withdrawal Potential
    Dimension 2 — Biomedical Conditions and Complications
    Dimension 3 — Emotional, Behavioral, and Cognitive Conditions
    Dimension 4 — Readiness to Change
    Dimension 5 — Relapse, Continued Use, or Continued Problem Potential
    Dimension 6 — Recovery/Living Environment

A Re-Assessment is the same structured evaluation performed periodically for a patient already
in treatment, across occupational, family, legal, mental health, physical health, substance use,
social, and treatment satisfaction domains.

The SaveAssessments.cs pipeline manages 19 related tables that together form the complete
clinical assessment dataset:

GROUP A — Admission Assessment (8 tables)

1.  SAMMS source form          → Azure TblAdmissionAssessment
    Root header record. Links the assessment to a patient (ClientId), pre-admission record
    (PreAdmissionId), and the form template (DataFormId). Captures who created and modified
    the form, when, and whether it has been deleted.

2.  SAMMS source form          → Azure TblAdmissionAssessmentSummary
    Clinical summary section. Records the ASAM level-of-care recommendation, clinical narrative,
    opioid treatment and withdrawal management selections, variance explanation, and four
    signature sets: Staff, Provider, Patient, and Supervisor.

3.  SAMMS source form          → Azure TblAdmissionAssessmentDimensionFour
    ASAM Dimension 4: Readiness to Change. Records SOCRATES (Stages of Change Readiness and
    Treatment Eagerness Scale) questionnaire responses and computed pre-contemplation,
    contemplation, and action scale scores.

4.  SAMMS source form          → Azure TblAdmissionAssessmentDimensionOneDisorder
    ASAM Dimension 1: Substance disorder diagnoses, prior treatment history (medically assisted
    withdrawal, inpatient rehab, intensive outpatient, outpatient), prior MAT history, drug
    procurement methods, and the overall dimension score.

5.  SAMMS source form          → Azure TblAdmissionAssessmentDimensionTwo
    ASAM Dimension 2: Biomedical conditions. 30+ medical condition flags (Asthma, Cancer,
    COPD, Diabetes, Heart Disease, HIV, Hepatitis A/B/C/D, Tuberculosis, etc.), allergies,
    tobacco use, and free-text diagnosis/problem fields.

6.  SAMMS source form          → Azure TblAdmissionAssessmentDimensionThree
    ASAM Dimension 3: Mental health conditions. Flags for Agoraphobia, Anxiety, Bipolar
    Disorder, Depression, and other emotional/behavioral diagnoses. Includes dimension score
    and free-text problem description.

7.  SAMMS source form          → Azure TblAdmissionAssessmentDimensionFiveSubstanceUse
    ASAM Dimension 5: Relapse risk and recovery environment factors. Overdose history,
    physical/mental health impact, legal involvement (arrests, probation, court cases, child
    custody), and financial stress indicators.

8.  SAMMS source form          → Azure TblAdmissionAssessmentDimensionSix
    ASAM Dimension 6: Recovery/living environment. Housing stability, financial security,
    employment, peer support, drug culture in neighborhood, and social support systems.

GROUP B — Re-Assessment (9 tables)

9.  SAMMS source form          → Azure TblReAssessment
    Root header record for periodic re-assessments. Mirrors the structure of the Admission
    Assessment header, adding TimeInTreatment to capture how long the patient has been enrolled.

10. SAMMS source form          → Azure TblReAssessmentOccupational
    Occupational/vocational domain. Current employment status, student status, and whether
    the patient has found new employment since the last assessment.

11. SAMMS source form          → Azure TblReAssessmentFamily
    Family and social support domain. Housing stability, financial capacity, child custody
    status, DFS/child protective services involvement, and domestic safety.

12. SAMMS source form          → Azure TblReAssessmentLegal
    Legal domain. Drug court involvement, probation/parole, open warrants, court fines,
    open criminal cases, and recent arrest history.

13. SAMMS source form          → Azure TblReAssessmentMentalHealth
    Mental health domain. Whether the patient has a psychiatrist, recent hospitalization for
    mental health reasons, and whether mental health has changed since last assessment.

14. SAMMS source form          → Azure TblReAssessmentPhysicalHealth
    Physical health domain. HIV/Hepatitis C test results (checkbox groups for positive/
    negative/NA), healthcare provider access, IV drug use, unsafe sex behavior, ER/911
    use, and physical health change since last assessment.

15. SAMMS source form          → Azure TblReAssessmentSubstanceUse
    Substance use domain. Overdose history since last assessment and tobacco/vaping use.

16. SAMMS source form          → Azure TblReAssessmentSocial
    Social support domain. Whether the patient has sober friends/family, a support network,
    and awareness of peer support groups.

17. SAMMS source form          → Azure TblReAssessmentTreatment
    Treatment satisfaction domain. Treatment satisfaction score, tapering intentions, and
    free-text narrative about what the patient has learned and still needs.

GROUP C — Substance Use History (2 tables)

18. SAMMS source table         → Azure TblAssessmentSubstanceUseHistories
    Substance use history rows attached to periodic re-assessment forms. One row per
    substance reported by the patient (type, route, amount, frequency, age of first use,
    withdrawal history). Uses RowChkSum for change detection.

19. SAMMS source table         → Azure TblAdmissionAssessmentSubstanceUseHistory
    Substance use history rows attached to the initial admission assessment form. Identical
    structure to the re-assessment version but targets a different Azure table. Also uses
    RowChkSum and includes additional per-field try/catch blocks for date resilience.

Why it is important

The Assessments dataset provides the clinical foundation of the BHG data warehouse. It enables:
- Tracking of ASAM-structured patient evaluations at admission and through treatment
- Identifying clinical risk factors (medical, mental health, legal, social) per patient
- Monitoring readiness-to-change scores and treatment engagement over time
- Linking substance use history to assessment episodes for clinical analysis
- Compliance with regulatory reporting requirements for OTP (Opioid Treatment Program) oversight

Load type

All 19 methods use the EF Core upsert path. There is no bulk (SqlBulkCopy) path for these
tables. The 19 methods fall into two structural patterns:

Pattern A — Standard always-update (methods 1–17, Groups A and B):
  Loads all existing Azure rows for the site into memory, iterates source DataTable, builds
  a new model object per row via dynamic column switch, matches by Id (single-column PK),
  then inserts new rows via AddRange batch or updates all fields on existing rows.
  No RowChkSum guard — every existing row is overwritten unconditionally.
  No RowState soft-delete — rows are never logically deleted by this pipeline.

Pattern B — RowChkSum with RowState (methods 18–19, Group C):
  Same structural approach as Pattern A but with RowChkSum stored per row and RowState
  used to flag logically invalid records. The composite key (SiteCode + Id) is used for
  matching. RowChkSum is written on every update but no skip guard is implemented, so
  column writes still occur on every run. RowState is set to false when CltId < 0.
________________________________________

3. Systems Involved

System / File                        Role
-----------                          ----
tsk.tbl_Schedule (Azure DB)          Configuration — defines schedules and their run times
Scheduler.exe                        Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=6              Main ETL orchestrator for SAMMS-Forms (Assessments)
dms.tbl_MapSrc2Dsn (Azure DB)        Metadata — defines which columns to SELECT for ActionKey=6
SelectConstructor.cs                 Assembles SELECT statement from metadata
SQLSvrManager.cs                     Fires SELECT against the clinic SAMMS SQL Server
SaveAssessments.cs / SaveData        EF Core upsert class — all 19 assessment methods live here
ctrl.tbl_LocationCons (Azure)        Connection strings for each clinic's SAMMS SQL Server
tsk.tbl_RowTrax (Azure)              Audit log — source vs destination row counts per run
BHG_DRContext                        EF Core DbContext — maps all 19 model classes to Azure tables
________________________________________

4. Scheduler — How Assessment Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily (typically overnight or early morning) and populates the task
queue for all ETL pipelines. It does NOT move data — it only creates tasks.

What the Scheduler does for SAMMS-Forms (Assessments)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Tasks from previous days that were never picked up are marked as expired (RowState=26).

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For the Assessment/Forms schedule:
    Name        = 'SAMMS-Forms'
    ActionKey   = 6
    ScheduleId  = 6
    NextRunTime = (calculated next run datetime)

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'SAMMS-Forms'
    SiteCode = 'All'
    Status   = 17
    WorkDate = today
    RunAt    = NextRunTime

Step 4 — Insert child task rows (one per clinic per table)
Using a cross join of dms.vw_MapAction and tsk.tbl_Tasks2, the Scheduler inserts child
task rows for each clinic and each destination assessment table:

    insert into tsk.tbl_Tasks2(ParentTaskId, TaskName, ...)
    select t.TaskId,
           ma.DsnSchema + '.' + ma.DsnTbl,  -- e.g. 'forms.tblAdmissionAssessment'
           ma.ActionKey,                     -- = 6
           ma.StepKey,
           ma.SiteCode                       -- = 'B01', 'VBRA', etc.
    from dms.vw_MapAction ma
    cross join tsk.tbl_Tasks2 t
    where ma.Enabled = 1
      and ma.IsActive = 1
      and case when ma.ActionKey = 6
               then 'SAMMS-Forms' end = t.TaskName

This produces 80+ child rows per table (one per active clinic per assessment table type).

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
        TaskName = 'SAMMS-Forms'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'forms.tblAdmissionAssessment'
        SiteCode = 'VBRA'
        ActionKey = 6
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'forms.tblAdmissionAssessmentSummary'
        SiteCode = 'VBRA'
        ActionKey = 6
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'forms.tblAdmissionAssessmentDimensionFour'
        SiteCode = 'B01'
        ActionKey = 6
        Status   = 17

    ... (one row per active clinic per assessment table type, ~19 tables x 80+ clinics)
________________________________________

5. BHGTaskRunner — How Assessment Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 6 to process the SAMMS-Forms schedule, which
includes all 19 assessment and re-assessment tables.

Command:   BHGTaskRunner.exe 6

Step 1 — Filter task queue for Schedule 6
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"           // PHC uses a separate runner
        && x.Status == 17             // ready to run
        && x.TaskName == "SAMMS-Forms"
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
                        && x.ActionKey == st.ActionKey        // = 6
                        && x.ActionStepKey == st.ActionStepKey)

Returns the list of column mappings from dms.tbl_MapSrc2Dsn for ActionKey=6.

Step 5 — Build the SELECT statement
SelectConstructor.GetSLT() assembles the SELECT field list by:
- Reading all enabled column mappings for the specific ActionKey/StepKey
- Building a CHECKSUM(...) expression across all mapped columns to produce RowChkSum
- Replacing placeholder tokens (@SiteCode, @WorkDate, @Samms)

Note: For Groups A and B (the always-update methods), the RowChkSum column is included
in the SELECT by SelectConstructor for structural consistency, but the methods do not use
it to skip writes. For Group C (Substance Use History), RowChkSum is actively read and
stored by the methods.

Step 6 — Build the WHERE clause
    strCmd += " where " + strWhere + " " + st.SortOrder;

The WHERE clause filters records by date, using DaysBack as configured per task. Assessment
forms typically use a moderate rolling window to capture recently created or modified forms.

Step 7 — Execute SELECT against SAMMS
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);

Returns a DataTable with all recently touched rows from this clinic for the specific table.

Step 8 — Route to the appropriate Save method
The routing is performed by SelectConstructor or directly in BHGTaskRunner based on TaskName:

    case "forms.tbladmissionassessment":
        rCodes = sd.SaveAdmissionAssessment(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentsummary":
        rCodes = sd.SaveAdmissionAssessmentSummary(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentdimensionfour":
        rCodes = sd.SaveAdmissionAssessmentDimensionfour(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentdimensiononedisorder":
        rCodes = sd.SaveAdmissionAssessmentDimensionOneDisorder(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentdimensiontwo":
        rCodes = sd.SaveAdmissionAssessmentDimensionTwo(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentdimensionthree":
        rCodes = sd.SaveAdmissionAssessmentDimensionThree(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentdimensionfivesubstanceuse":
        rCodes = sd.SaveAdmissionAssessmentDimensionFiveSubstanceUse(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentdimensionsix":
        rCodes = sd.SaveAdmissionAssessmentDimensionSix(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessment":
        rCodes = sd.SaveReAssessment(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentoccupational":
        rCodes = sd.SaveReAssessmentOccupational(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentfamily":
        rCodes = sd.SaveReAssessmentFamily(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentlegal":
        rCodes = sd.SaveReAssessmentLegal(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentmentalhealth":
        rCodes = sd.SaveReAssessmentMentalHealth(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentphysicalhealth":
        rCodes = sd.SaveReAssessmentPhysicalHealth(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentsubstanceuse":
        rCodes = sd.SaveReAssessmentSubstanceUse(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmentsocial":
        rCodes = sd.SaveReAssessmentSocial(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblreassessmenttreatment":
        rCodes = sd.SaveReAssessmentTreatment(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tblassessmentsubstanceusehistory":
        rCodes = sd.SaveAssessmentSubstanceuseHistory(SrcDt, st.SiteCode, WorkDate, null)

    case "forms.tbladmissionassessmentsubstanceusehistory":
        rCodes = sd.SaveAdmissionAssessmentSubstanceuseHistory(SrcDt, st.SiteCode, WorkDate, null)

All 19 methods use the EF Core upsert path. There is no Bulk / SqlBulkCopy path for these tables.

Step 9 — RowTrax audit (if enabled for this task)
If st.RowTrax == true:

    Source count:  SrcDt.Rows.Count  (rows returned from SAMMS)

    Destination count:
        Select count(1) from [destination table]
        where SiteCode = 'VBRA'

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

For assessment tables, the SELECT looks conceptually like this (example for AdmissionAssessment):

    Select
        SiteCode = 'VBRA',
        Id,
        PreAdmissionId,
        DataFormId,
        ClientId,
        CreatedBy,
        CreatedOn,
        ModifiedBy,
        ModifiedOn,
        IsDeleted,
        Version,
        RowChkSum = CHECKSUM(Id, PreAdmissionId, DataFormId, ClientId, CreatedBy,
                             CreatedOn, ModifiedBy, ModifiedOn, IsDeleted, Version)
    from dbo.[AdmissionAssessment source table]
    where ModifiedOn >= DATEADD(day, -30, GETDATE())   -- (example lookback)

The CHECKSUM() expression is computed at source for structural consistency. For the 17
always-update methods (Groups A and B), it is not used to skip writes. For Group C
(Substance Use History), it is read by the method and stored in the RowChkSum column.

SelectConstructor also routes the table name to the correct Save* method when called via
the backfill path in bhg.TestCode or manual reruns.
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
WHERE clause. This DataTable is passed directly into the appropriate Save* method.
________________________________________

8. Source Tables — SAMMS SQL Server

All source assessment tables live in the clinic's local SAMMS SQL Server database. The column
names listed below are the actual SAMMS column names as they appear in the DataTable columns
iterated by the switch statements in each Save method. SiteCode and RowChkSum are added by
the ETL SELECT, not stored in the SAMMS source tables.
________________________________________

8a. Admission Assessment Header Source

Primary Key: Id (unique per clinic)

Column Name             Type                Description
-----------             ----                -----------
Id                      int                 Unique assessment ID within this clinic
PreAdmissionId          int                 Linked pre-admission record ID
DataFormId              int                 Form template ID used for this assessment
ClientId                int                 Patient/client ID
CreatedBy               varchar             Username who created the form
CreatedOn               datetime (null)     Date/time the form was created
ModifiedBy              varchar             Username who last modified the form
ModifiedOn              datetime (null)     Date/time the form was last modified
IsDeleted               bool                Whether this form has been logically deleted in SAMMS
Version                 varchar             Form version identifier
SiteCode                varchar(25)         Added by ETL — clinic identifier
RowChkSum               int                 Computed by ETL: CHECKSUM() across all mapped columns
________________________________________

8b. Admission Assessment Summary Source

Primary Key: Id

Column Name                                     Type                Description
-----------                                     ----                -----------
Id                                              int                 Unique summary ID
PreAdmissionId                                  int                 Linked pre-admission record
AdmissionAssessmentId                           int                 Parent assessment ID
Ddlrecommendation                               int                 ASAM level-of-care recommendation code
OpioidTreatmentServices                         int (null)          Opioid treatment services code
WithdrawalManagement                            int (null)          Withdrawal management code
ClinicalSummary                                 varchar             Clinical narrative text
AsamrecommendationForLevel                      int (null)          ASAM recommended care level
LevelOfCareAtVariance                           varchar             Level of care variance explanation
SummaryComments                                 varchar             Summary narrative comments
AdmissionAssessmentStaffSignature               varchar             Staff signature
AdmissionAssessmentStaffSignatureBy             varchar             Staff signatory name
AdmissionAssessmentStaffSignatureDate           datetime (null)     Staff signature date
AdmissionAssessmentProviderSignature            varchar             Provider signature
AdmissionAssessmentProviderSignatureBy          varchar             Provider signatory name
AdmissionAssessmentProviderSignatureDate        datetime (null)     Provider signature date
AdmissionAssessmentPatientSignature             varchar             Patient signature
AdmissionAssessmentPatientSignatureBy           varchar             Patient signatory name
AdmissionAssessmentPatientSignatureDate         datetime (null)     Patient signature date
AdmissionAssessmentSupervisorSignature          varchar             Supervisor signature
AdmissionAssessmentSupervisorSignatureBy        varchar             Supervisor signatory name
AdmissionAssessmentSupervisorSignatureDate      datetime (null)     Supervisor signature date
________________________________________

8c. Admission Assessment Dimension Four (Readiness to Change) Source

Primary Key: Id

Column Name                     Type        Description
-----------                     ----        -----------
Id                              int         Unique dimension 4 record ID
PreAdmissionId                  int         Linked pre-admission record
AdmissionAssessmentId           int         Parent assessment ID
IdontThinkUseDrugsTooMuch       int (0 def) SOCRATES item — "I don't think I use drugs too much"
TryingTtoDrinklessThanUsed      int (null)  SOCRATES item — "Trying to drink less than I used to"
IenjoyMyDrinking                int (null)  SOCRATES item — "I enjoy my drinking"
StageOfChange                   varchar     Computed stage of change description
IshouldCutDownOnMyDrinking      int (null)  SOCRATES item
WasteOfTimeToThinkAboutMyDrinking int (null) SOCRATES item
RecentlyChangedMyDrinking       int (null)  SOCRATES item
AnyoneCanTalkAboutWanting       int (null)  SOCRATES item
ThinkAboutDrinkingLessAlcohol   int (null)  SOCRATES item
MyDrinkingUse                   int (null)  SOCRATES item
NoNeedForMeToThinkAbout         int (null)  SOCRATES item
ActuallyChangingMyDrinking      int (null)  SOCRATES item
DrinkingLessAlcohol             int (null)  SOCRATES item
PrecontemplationScale           int (null)  Computed pre-contemplation score
ContemplationScale              int (null)  Computed contemplation score
ActionScale                     int (null)  Computed action scale score
DdldimensionFourScore           int (null)  Overall dimension 4 score
StatusofChange                  int (null)  Status of change code
Comments4                       varchar     Dimension 4 free-text comments
Dimension4Problems              varchar     Dimension 4 problems narrative
________________________________________

8d. Admission Assessment Dimension One Disorder (Acute Intoxication) Source

Primary Key: Id

Column Name                                     Type        Description
-----------                                     ----        -----------
Id                                              int         Unique dim 1 record ID
PreAdmissionId                                  int (null)  Linked pre-admission record
AdmissionAssessmentId                           int (null)  Parent assessment ID
OpioidDisorderPresent                           int (null)  Opioid use disorder flag
AlcoholDisorderPresent                          int (null)  Alcohol use disorder flag
SedativeDisorderPresent                         int (null)  Sedative use disorder flag
StimulantDisorderPresent                        int (null)  Stimulant use disorder flag
CannabisDisorderPresent                         int (null)  Cannabis use disorder flag
HallucinogenDisorderPresent                     int (null)  Hallucinogen use disorder flag
InhalantDisorderPresent / inhakantdisorderpresent int (null) Inhalant use disorder flag
PhencyclidineDisorderPresent                    int (null)  Phencyclidine (PCP) disorder flag
MedicallyAssistedWithdrawal                     int (null)  Prior MAW count
MedicallyAssistedWithdrawalHowManyTimes         int (null)  Number of prior MAW episodes
MedicallyAssistedWithdrawalRecentTimes          int (null)  Recent MAW times
InpatientRehabilitation                         int (null)  Prior inpatient rehab count
InpatientRehabilitationHowManyTimes             int (null)  Number of prior inpatient stays
InpatientRehabilitationSuccessfullyComplete     int (null)  Whether successfully completed
InpatientRehabilitationRecentTimes              int (null)  Recent inpatient times
IntensiveOutpatientTreatments                   int (null)  Prior IOP count
IntensiveOutpatientHowManyTimes                 int (null)  Number of prior IOP episodes
IntensiveOutpatientSuccessfullyComplete         int (null)  Whether IOP successfully completed
IntensiveOutpatientRecentTimes                  int (null)  Recent IOP times
OutpatientTreatment                             int (null)  Prior outpatient treatment count
OutpatientTreatmentHowManyTimes                 int (null)  Number of outpatient episodes
OutpatientTreatmentSuccessfullyComplete         int (null)  Whether outpatient completed
OutpatientTreatmentRecentTimes                  int (null)  Recent outpatient times
DdlmedicallyAssistedWithdrawal                  int (null)  Dropdown code for MAW
DdlinpatientRehabilitation                      int (null)  Dropdown code for inpatient
DdlintensiveOutpatient                          int (null)  Dropdown code for IOP
DdloutpatientTreatment                          int (null)  Dropdown code for outpatient
ChkMedicallyAssistedWithdrawal                  bool (null) Checkbox — has had MAW
ChkInpatientRehabilitation                      bool (null) Checkbox — has had inpatient
ChkIntensiveOutpatientTreatments                bool (null) Checkbox — has had IOP
ChkOutpatientTreatment                          bool (null) Checkbox — has had outpatient
ChkPreviousMat                                  bool (null) Checkbox — has had prior MAT
PreviousMat                                     int (null)  Prior MAT indicator
PreviousMatmethadone                            bool (null) Prior MAT was methadone
PreviousMatbuprenorphine                        bool (null) Prior MAT was buprenorphine
PreviousMatnaltrexone                           bool (null) Prior MAT was naltrexone
PreviousMatwhatWasYourDose                      varchar     Dose description text
PreviousMatwasItHelpful                         int (null)  Whether prior MAT was helpful
HowLongDidYouTakeIt                             int (null)  Duration of prior MAT
DdlhowLongDidYouTakeIt                          int (null)  Dropdown for duration code
LongestPeriodOfSobriety                         int (null)  Longest sobriety period (numeric)
DdllongestPeriodOfSobrietyFromAllSubstances     int (null)  Dropdown code for sobriety period
HowDoYouProcureTheDrug                          int (null)  Procurement method code
BuyOnTheStreet                                  bool (null) Procurement — buy on street
FreeFromFamily                                  bool (null) Procurement — free from family
PrescriptionFromHealthcareProvider              bool (null) Procurement — prescription
SellingUseOwnSupply                             bool (null) Procurement — sell/use own supply
Theft                                           bool (null) Procurement — theft
DdldimensionOneScore                            int (null)  Overall dimension 1 score
SubstanceUseHistoryComments                     varchar     Substance history comments
Comments                                        varchar     General dimension 1 comments
________________________________________

8e. Admission Assessment Dimension Two (Biomedical) Source

Primary Key: Id

Column Name             Type        Description
-----------             ----        -----------
Id                      int         Unique dim 2 record ID
PreAdmissionId          int (null)  Linked pre-admission record
AdmissionAssessmentId   int (null)  Parent assessment ID
Allergies               varchar     Known drug/food allergies (free text)
Asthma                  bool (null) Medical condition flag — Asthma
Blindness               bool (null) Medical condition flag — Blindness
Cancer                  bool (null) Medical condition flag — Cancer
ChronicPain             bool (null) Medical condition flag — Chronic Pain
Copdemphysema           bool (null) Medical condition flag — COPD/Emphysema
Deafness                bool (null) Medical condition flag — Deafness
Diabetes                bool (null) Medical condition flag — Diabetes
EpilepsySeizures        bool (null) Medical condition flag — Epilepsy/Seizures
Gerd                    bool (null) Medical condition flag — GERD
HearingLoss             bool (null) Medical condition flag — Hearing Loss
HeartDisease            bool (null) Medical condition flag — Heart Disease
HepatitisA              bool (null) Infectious disease — Hepatitis A
HepatitisB              bool (null) Infectious disease — Hepatitis B
HepatitisC              bool (null) Infectious disease — Hepatitis C
HepatitisD              bool (null) Infectious disease — Hepatitis D
HighBloodPressure       bool (null) Medical condition — High Blood Pressure
HighCholesterol         bool (null) Medical condition — High Cholesterol
Hiv                     bool (null) Infectious disease — HIV
LiverDisease            bool (null) Medical condition — Liver Disease
Other                   bool (null) Other condition flag
OtherTxt                varchar     Description of other condition
PoorVision              bool (null) Medical condition — Poor Vision
PrimaryCarePractitioner int (null)  Has a primary care practitioner code
RenalKidneyDisease      bool (null) Medical condition — Renal/Kidney Disease
Tuberculosis            bool (null) Infectious disease — Tuberculosis
DoYouHaveAnyConcerns    int (null)  Any health concerns code
DoYouUseTobacco         int (null)  Tobacco use code
DdldimensionTwoScore    int (null)  Overall dimension 2 score
DiagnosedComment2       varchar     Diagnosed conditions comment
Dimension2Problems      varchar     Dimension 2 problems narrative
Comments2               varchar     Dimension 2 free-text comments
________________________________________

8f. Admission Assessment Dimension Three (Mental Health) Source

Primary Key: Id

Column Name                 Type        Description
-----------                 ----        -----------
Id                          int         Unique dim 3 record ID
PreAdmissionId              int (null)  Linked pre-admission record
AdmissionAssessmentId       int (null)  Parent assessment ID
Agoraphobia                 bool (null) Mental health flag — Agoraphobia
Anxiety                     bool (null) Mental health flag — Anxiety
BipolarDisorder             bool (null) Mental health flag — Bipolar Disorder
Depression                  bool (null) Mental health flag — Depression
(additional mental health condition flags per SAMMS form version)
DdldimensionThreeScore      int (null)  Overall dimension 3 score
Comments3                   varchar     Dimension 3 free-text comments
Dimension3Problems          varchar     Dimension 3 problems narrative
________________________________________

8g. Admission Assessment Dimension Five (Relapse Risk) Source

Primary Key: Id

Column Name                         Type        Description
-----------                         ----        -----------
Id                                  int         Unique dim 5 record ID
PreAdmissionId                      int (null)  Linked pre-admission record
AdmissionAssessmentId               int (null)  Parent assessment ID
HadAnOverdose                       int (null)  History of overdose code
YourPhysicalHealthWorse             int (null)  Physical health worsened by use code
YourPhysicalMentalWorse             int (null)  Mental health worsened by use code
HaveYouCalled911                    int (null)  Called 911 for overdose code
SubstanceUseJeopardized             int (null)  Substance use jeopardized relationships code
CausedProblemsAtYourJob             int (null)  Caused job problems code
HavingAnyFinancialTroubles          int (null)  Financial troubles code
DoesYourTemperTend                  int (null)  Temper/aggression code
HaveYouEverBeenArrested             int (null)  Prior arrest code
RiskOfBeingArrested                 int (null)  Current arrest risk code
OpenOrPendingCourtCases             int (null)  Open court cases code
AreYouOnProbation                   int (null)  Probation status code
LegalCustodyOfYourChildren          int (null)  Child custody status code
AnyOpenCasesWitHlocalDepartment     int (null)  Open DFS/child services cases code
ChildrenLiveInYourHome              int (null)  Children in home code
Comments                            varchar     Dimension 5 free-text comments
DimensionFiveComments               varchar     Additional dimension 5 comments
Dimension5Problems                  varchar     Dimension 5 problems narrative
________________________________________

8h. Admission Assessment Dimension Six (Recovery Environment) Source

Primary Key: Id

Column Name                                 Type        Description
-----------                                 ----        -----------
Id                                          int         Unique dim 6 record ID
PreAdmissionId                              int (null)  Linked pre-admission record
AdmissionAssessmentId                       int (null)  Parent assessment ID
AreYouBehindOnYourRent                      int (null)  Behind on rent code
AreYouBehindOnYourUtility                   int (null)  Behind on utilities code
AnyPeerSupport                              int (null)  Has peer support code
DoYouHaveEnoughMoney                        int (null)  Sufficient income code
DoYouHaveJob                                int (null)  Has employment code
DoYouHaveSourceOfIncome                     int (null)  Has income source code
DrugSellingCommonInYourNeighborhood         int (null)  Drug activity in neighborhood code
FamilyMembersWhoAreInRecovery               int (null)  Family in recovery code
DdldimensionSixScore                        int (null)  Overall dimension 6 score
DimensionSixComments                        varchar     Dimension 6 narrative comments
Dimension6Problems                          varchar     Dimension 6 problems narrative
Comments                                    varchar     Free-text comments
________________________________________

8i. Re-Assessment Header Source

Primary Key: Id

Column Name         Type                Description
-----------         ----                -----------
Id                  int                 Unique re-assessment ID
PreAdmissionId      int                 Linked pre-admission record
DataFormId          int                 Form template ID
ClientId            int                 Patient/client ID
CreatedBy           varchar             Username who created the form
CreatedOn           datetime (null)     Date/time created (length > 6 guard)
ModifiedBy          varchar             Username who last modified
ModifiedOn          datetime (null)     Date/time last modified (length > 6 guard)
IsDeleted           bool                Logically deleted in SAMMS (false if empty)
Version             varchar             Form version
TimeInTreatment     int (null)          Duration in treatment at time of re-assessment
________________________________________

8j. Re-Assessment Occupational Source

Primary Key: Id

Column Name                                 Type        Description
-----------                                 ----        -----------
Id                                          int         Unique record ID
PreAdmissionId                              int (null)  Linked pre-admission record
ReassessmentId                              int (null)  Parent re-assessment ID
WhatIsYourCurrentEmploymentStatus           int (null)  Employment status code
AreYouCurrentlyAfulltimeStudent             int (null)  Full-time student code
AreYouCurrentlyAparttimeStudent             int (null)  Part-time student code
HaveYouFoundAparttimeOrFulltimeJob          int (null)  Found new job since last assessment
CommentsOccupational                        varchar     Occupational comments
________________________________________

8k. Re-Assessment Family Source

Primary Key: Id

Column Name                                             Type        Description
-----------                                             ----        -----------
Id                                                      int         Unique record ID
PreAdmissionId                                          int (null)  Linked pre-admission record
ReassessmentId                                          int (null)  Parent re-assessment ID
AreYouSafeFromPhysicalOrSexualAbuseInYourHome           int (null)  Domestic safety code
DoYouHaveAnyOpenCasesWithYourLocalDepartment            int (null)  Open DFS cases code
DoYouHaveEnoughMoney                                    int (null)  Financial sufficiency code
DoYouHaveLegalCustodyOfYourChildren                     int (null)  Child custody code
DoYouHaveStableHousingOfYourOwn                         int (null)  Housing stability code
CommentsFamily                                          varchar     Family domain comments
________________________________________

8l. Re-Assessment Legal Source

Primary Key: Id

Column Name                                 Type        Description
-----------                                 ----        -----------
Id                                          int         Unique record ID
PreAdmissionId                              int (null)  Linked pre-admission record
ReassessmentId                              int (null)  Parent re-assessment ID
AreYouInvolvedWithAdrugTreatmentCourt       int (null)  Drug treatment court involvement code
AreYouOnProbationOrPayrole                  int (null)  Probation/parole code
DoYouHaveAnyOpenCriminalCases               int (null)  Open criminal cases code
DoYouHaveAnyOpenOrPendingCourtCases         int (null)  Open/pending court cases code
DoYouHaveAnyOpenWarrants                    int (null)  Open warrants code
DoYouOweMoneyForCourtFinesOrFees            int (null)  Court fines owed code
HaveYouBeenArrested                         int (null)  Recent arrest code
CommentsLegal                               varchar     Legal domain comments
________________________________________

8m. Re-Assessment Mental Health Source

Primary Key: Id

Column Name                                             Type        Description
-----------                                             ----        -----------
Id                                                      int         Unique record ID
PreAdmissionId                                          int (null)  Linked pre-admission record
ReAssessmentId                                          int (null)  Parent re-assessment ID
DoYouHaveApsychiatrist                                  int (null)  Has psychiatrist code
HaveYouBeenHospitalizedForMentalHealthReasons           int (null)  Recent MH hospitalization code
HowHasYourMentalHealthChanged                           int (null)  MH change since last assessment
________________________________________

8n. Re-Assessment Physical Health Source

Primary Key: Id

Column Name                                 Type        Description
-----------                                 ----        -----------
Id                                          int         Unique record ID
PreAdmissionId                              int (null)  Linked pre-admission record
ReassessmentId                              int (null)  Parent re-assessment ID
ChkboxHepatitisCnegative                    bool (null) Hepatitis C test result — Negative
ChkboxHepatitisCpostive                     bool (null) Hepatitis C test result — Positive
ChkboxHivnegative                           bool (null) HIV test result — Negative
ChkboxHivpostive                            bool (null) HIV test result — Positive
ChkboxNa                                    bool (null) Testing — Not Applicable
DoYouHaveAprimaryCarePractitionerOrClinic   int (null)  Has primary care provider code
HaveYouBeenTestedForHivandHepatitisC        int (null)  Recent HIV/HCV testing code
HaveYouCalled911OrBeeniItheEmergencyRoom    int (null)  ER/911 use code
HaveYouHadAnyUnsafeSex                      int (null)  Unsafe sex code
HaveYouInjectedDrugs                        int (null)  IV drug use code
HowHasYourPhysicalHealthChanged             int (null)  Physical health change code
IfYouWereHepatitisCpositive                 int (null)  If HCV positive — action taken code
IfYouWereHivpositive                        int (null)  If HIV positive — action taken code
CommentsPhysicalHealth                      varchar     Physical health comments
________________________________________

8o. Re-Assessment Substance Use Source

Primary Key: Id

Column Name                         Type        Description
-----------                         ----        -----------
Id                                  int         Unique record ID
PreAdmissionId                      int (null)  Linked pre-admission record
ReAssessmentId                      int (null)  Parent re-assessment ID
HaveYouHadAnOverdose                int (null)  Overdose since last assessment code
DoYouUseTobaccoOrVapeNicotine       int (null)  Tobacco/vaping use code
CommentsSubstanceUse                varchar     Substance use domain comments
________________________________________

8p. Re-Assessment Social Source

Primary Key: Id

Column Name                                                     Type        Description
-----------                                                     ----        -----------
Id                                                              int         Unique record ID
PreAdmissionId                                                  int (null)  Linked pre-admission record
ReassessmentId                                                  int (null)  Parent re-assessment ID
DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink                 int (null)  Sober social network code
DoYouHaveFriendsAndFamilyWhoYouCanCountOnToSupportYou           int (null)  Support network code
DoYouKnowOfAnyPeerSupport                                       int (null)  Aware of peer support code
CommentsSocial                                                  varchar     Social domain comments
________________________________________

8q. Re-Assessment Treatment Source

Primary Key: Id

Column Name                                 Type        Description
-----------                                 ----        -----------
Id                                          int         Unique record ID
PreAdmissionId                              int (null)  Linked pre-admission record
ReassessmentId                              int (null)  Parent re-assessment ID
ClientId                                    int (null)  Patient/client ID
AreYouSatisfiedWith                         int (null)  Treatment satisfaction code
DoYouPlanOnTaperingOff                      int (null)  Tapering plan code
IsEventuallyTaperingOff                     int (null)  Long-term taper goal code
WhatHaveYouLearnedAboutWhatYouPrefer        varchar     Free-text — learning narrative
WhatNeedsDoYouHaveThatWeCanHelpYou          varchar     Free-text — unmet needs narrative
________________________________________

8r. Assessment Substance Use History Source

Primary Key: Id (per clinic — composite SiteCode + Id in Azure)

Column Name             Type                Description
-----------             ----                -----------
Id                      int                 Unique substance use history row ID
PreAdmissionId          int                 Linked pre-admission record (0 if blank)
AssessmentFormId        int                 Linked periodic assessment form ID (0 if blank)
CltId                   int                 Patient/client ID (0 if blank; negative = invalid)
CreatedOn               datetime (null)     Date record was created (length > 6 guard)
DateOfLastUse           datetime (null)     Date of last substance use (length > 6 guard)
TxEpisode               varchar             Treatment episode identifier
SubstanceType           varchar             Type category of substance
Substance               varchar             Specific substance name
Route                   varchar             Route of administration
Amount                  varchar             Amount used
FrequencyOfLastUse      varchar             Frequency of use at last use
PeakUse                 varchar             Peak use amount/frequency
AgeOfFirstUse           varchar             Age at first use of this substance
ListSymptoms            varchar             Symptoms reported
Notes                   varchar             Free-text notes
MasterID                int (null)          Master record ID if applicable
DateOfReported          datetime (null)     Date this row was reported (length > 6 guard)
Withdrawal              bool (null)         Withdrawal symptoms present
SiteCode                varchar(25)         Added by ETL — clinic identifier
RowChkSum               int                 Computed by ETL: CHECKSUM() across mapped columns
________________________________________

8s. Admission Assessment Substance Use History Source

Same column structure as 8r (Assessment Substance Use History) with these differences:
- AssessmentFormId is NOT present in this version
- DateOfLastUse and DateOfReported have additional try/catch error handling on parse
- CreatedOn also has try/catch error handling (unique to this method)

All other columns match the structure in 8r.
________________________________________

9. Standard EF Core Upsert Pattern (Groups A and B — 17 Methods)

File: BCAppCode/BHG-DR-LIB/SaveAssessments.cs
Class: SaveData (partial class)

This section describes the common structural pattern shared by all 17 methods in Group A
(Admission Assessment) and Group B (ReAssessment). Each method has the same skeleton.
Method-specific column mapping details are documented in sections 10 through 26.

Method signature:
    public RCodes Save<MethodName>(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted but NOT used inside any of these methods)
        BHG_DRContext db)        // EF context (created internally if null)

Note: wrkdt is accepted for interface consistency with the SelectConstructor routing pattern
but is not used within the method body by any of the 17 Group A/B methods.

EF Core upsert logic — step by step (same for all 17 methods):

Step 1 — Initialize RCodes
    rc = new RCodes {
        IsResult      = true,
        RowsProcessed = tbl.Rows.Count
    }
    (RowsIns and RowsUpd are also tracked and incremented per row)

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Capture execution timestamp
    DateTime runat = DateTime.Now;

This timestamp is used for LastModAt on all rows in this batch. All rows in a single method
call share the same audit timestamp, regardless of when they are individually processed.

Step 4 — Load ALL existing Azure rows for this site (no date filter)
    dbList = db.Tbl<Xxx>.Where(x => x.SiteCode == sc).ToList();

The entire existing Azure dataset for this clinic and table is pulled into memory. No date
filter is applied. This is the working set against which source rows are matched.

Step 5 — Initialize new-row staging list
    xnList = new List<Tbl<Xxx>>();

New rows are staged here during the loop and inserted with AddRange after updates commit.

Step 6 — Loop through every row from the SAMMS DataTable
    foreach (DataRow dr in tbl.Rows)

Step 7 — Build a new model object by iterating all DataTable columns
    Tbl<Xxx> xa = new Tbl<Xxx>();
    foreach (DataColumn c in tbl.Columns)
    {
        switch (c.ColumnName.ToLower()) { ... }
    }

    The "sitecode" case always:
        xa.SiteCode  = sc;       // force from parameter, not source value
        xa.LastModAt = runat;    // ETL timestamp set on every row

    Integer fields with 0 default:
        xa.Id = 0;
        if (dr[c.ColumnName].ToString().Length > 0)
            xa.Id = int.Parse(dr[c.ColumnName].ToString());

    Integer fields with null default:
        if (dr[c.ColumnName].ToString().Length > 0)
            xa.FieldName = int.Parse(dr[c.ColumnName].ToString());

    Boolean fields:
        if (dr[c.ColumnName].ToString().Length > 0)
            xa.FieldName = bool.Parse(dr[c.ColumnName].ToString());

    DateTime fields:
        if (dr[c.ColumnName].ToString().Length > 6)
            xa.FieldName = DateTime.Parse(dr[c.ColumnName].ToString());

    String fields:
        xa.FieldName = dr[c.ColumnName].ToString();    // or .Trim() where applicable

Step 8 — Match against existing Azure row by primary key
    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);

    The lookup key is Id only (single-column). SiteCode is not part of the match expression
    in Groups A and B. (Group C uses composite SiteCode + Id — see sections 27–28.)

Step 9 — INSERT or UPDATE based on match result

    if (xdb == null):
        rc.RowsIns += 1
        xnList.Add(xa)           // staged for batch insert

    else:
        rc.RowsUpd += 1
        xdb.Field1 = xa.Field1   // copy every field individually
        xdb.Field2 = xa.Field2
        ... (all mapped fields)
        xdb.LastModAt = xa.LastModAt

    There is NO RowChkSum guard in any Group A/B method. Every existing row is
    unconditionally overwritten on every run, regardless of whether the data changed.

Step 10 — Commit all updates in one batch
    db.SaveChanges()
    // All update-tracked EF objects committed in one round trip

Step 11 — Insert all new rows using AddRange
    if (xnList.Count > 0):
        db.Tbl<Xxx>.AddRange(xnList)
        db.SaveChanges()

The two-phase commit (updates first, then inserts) ensures that update conflicts do not
interfere with new row inserts. AddRange generates a batched INSERT rather than individual
Add() calls.
________________________________________

10. SaveAdmissionAssessment — Admission Assessment Header

EF Core method: SaveAdmissionAssessment()
Target table: TblAdmissionAssessment
Source: SAMMS admission assessment header form

Business purpose:
Loads the root header record for a patient's admission assessment. This is the anchor record
to which all eight ASAM dimension sub-tables (Summary, Dim1–6) link via AdmissionAssessmentId.
Captures the administrative metadata: who created and last modified the assessment form, when,
which form template was used, which patient and pre-admission record it belongs to, whether it
has been deleted in SAMMS, and the form version.

Column mapping (switch cases):

    "sitecode"          → aa.SiteCode = sc; aa.LastModAt = runat
    "id"                → aa.Id = 0; then int.Parse if non-empty
    "preadmissionid"    → aa.PreAdmissionId = 0; then int.Parse if non-empty
    "dataformid"        → aa.DataFormId = 0; then int.Parse if non-empty
    "clientid"          → aa.ClientId = 0; then int.Parse if non-empty
    "createdby"         → aa.CreatedBy = value (direct string)
    "createdon"         → aa.CreatedOn = DateTime.Parse if length > 6
    "modifiedby"        → aa.ModifiedBy = value (direct string)
    "modifiedon"        → aa.ModifiedOn = DateTime.Parse if length > 6
    "isdeleted"         → aa.IsDeleted = false (default); bool.Parse if non-empty
    "version"           → aa.Version = value (direct string)

Special handling — IsDeleted:
    aa.IsDeleted = false;
    if (dr[c.ColumnName].ToString().Length > 0)
    {
        aa.IsDeleted = bool.Parse(dr[c.ColumnName].ToString());
    }
The default of false ensures that an empty source cell does not throw a parse exception
and the record is treated as not deleted. This mirrors the same pattern in SaveReAssessment.

PK lookup: laas.FirstOrDefault(x => x.Id == aa.Id)

Fields copied on update: PreAdmissionId, DataFormId, ClientId, CreatedBy, CreatedOn,
ModifiedBy, ModifiedOn, IsDeleted, Version, LastModAt
________________________________________

11. SaveAdmissionAssessmentSummary — Clinical Summary and Signatures

EF Core method: SaveAdmissionAssessmentSummary()
Target table: TblAdmissionAssessmentSummary
Source: SAMMS admission assessment summary section

Business purpose:
Loads the clinical summary section of the admission assessment. Contains the ASAM level-of-
care recommendation (Ddlrecommendation), clinical narrative text, opioid treatment and
withdrawal management selections, variance explanation, summary comments, and four complete
signature sets (Staff, Provider, Patient, Supervisor — each with signature image, signatory
name, and date).

Column mapping (switch cases):

    "sitecode"                                      → aa.SiteCode = sc; aa.LastModAt = runat
    "id"                                            → aa.Id = 0; int.Parse if non-empty
    "preadmissionid"                                → aa.PreAdmissionId = 0; int.Parse if non-empty
    "admissionassessmentid"                         → aa.AdmissionAssessmentId = 0; int.Parse if non-empty
    "ddlrecommendation"                             → aa.Ddlrecommendation = 0; int.Parse if non-empty
    "opioidtreatmentservices"                       → int.Parse if non-empty
    "withdrawalmanagement"                          → int.Parse if non-empty
    "clinicalsummary"                               → string, .Trim()
    "asamrecommendationforlevel"                    → int.Parse if non-empty
    "levelofcareatvariance"                         → string, .Trim()
    "summarycomments"                               → string, .Trim()
    "admissionassessmentstaffsignature"             → string, .Trim()
    "admissionassessmentstaffsignatureby"           → string, .Trim()
    "admissionassessmentstaffsignaturedate"         → ⚠ BUG: maps to PatientSignatureDate, not StaffSignatureDate
    "admissionassessmentprovidersignature"          → string, .Trim()
    "admissionassessmentprovidersignatureby"        → string, .Trim()
    "admissionassessmentprovidersignaturedate"      → DateTime.Parse if length > 0
    "admissionassessmentpatientsignature"           → string, .Trim()
    "admissionassessmentpatientsignatureby"         → string, .Trim()
    "admissionassessmentpatientsignaturedate"       → DateTime.Parse if length > 0
    "admissionassessmentsupervisorsignature"        → string, .Trim()
    "admissionassessmentsupervisorsignatureby"      → string, .Trim()
    "admissionassessmentsupervisorsignaturedate"    → DateTime.Parse if length > 0

KNOWN BUG — Staff Signature Date:
The case "admissionassessmentstaffsignaturedate" maps to:
    aa.AdmissionAssessmentPatientSignatureDate = DateTime.Parse(...)
instead of:
    aa.AdmissionAssessmentStaffSignatureDate = DateTime.Parse(...)

This is a copy-paste error. As a result:
- AdmissionAssessmentStaffSignatureDate (Azure column) is NEVER populated by this method.
- AdmissionAssessmentPatientSignatureDate (Azure column) may be overwritten with the staff
  date if the staff date case processes after the legitimate patient date case in the column
  iteration order.

PK lookup: laas.FirstOrDefault(x => x.Id == aa.Id)

Note: StaffSignatureDate uses length > 0 guard (not > 6 like the other date fields in
the file) — consistent with the Provider/Patient/Supervisor date guards in this method,
though the property it actually writes to is wrong due to the bug above.
________________________________________

12. SaveAdmissionAssessmentDimensionfour — ASAM Dimension 4: Readiness to Change

EF Core method: SaveAdmissionAssessmentDimensionfour()
Target table: TblAdmissionAssessmentDimensionFour
Source: SAMMS Dimension 4 form section

Business purpose:
Loads ASAM Dimension 4 — Readiness to Change. Stores the patient's responses to SOCRATES
questionnaire items about their attitudes toward drinking/drug use (e.g. "I don't think I use
drugs too much", "I enjoy my drinking", "I should cut down on my drinking") and the resulting
computed scores: PrecontemplationScale, ContemplationScale, and ActionScale. Also stores the
overall DdldimensionFourScore, the StageOfChange text description, and StatusofChange code.

Column mapping (switch cases):

    "sitecode"                          → aa.SiteCode = sc; aa.LastModAt = runat
    "id"                                → aa.Id = 0; int.Parse if non-empty
    "preadmissionid"                    → aa.PreAdmissionId = 0; int.Parse if non-empty
    "admissionassessmentid"             → aa.AdmissionAssessmentId = 0; int.Parse if non-empty
    "idontthinkusedrugstoomuch"         → aa.IdontThinkUseDrugsTooMuch = 0; int.Parse if non-empty
    "tryingttodrinklessthanused"        → int.Parse if non-empty
    "ienjoymydrinking"                  → int.Parse if non-empty
    "stageofchange"                     → string, .Trim()
    "ishouldcutdownonmydrinking"        → int.Parse if non-empty
    "wasteoftimetothinkaboutmydrinking" → int.Parse if non-empty
    "recentlychangedmydrinking"         → int.Parse if non-empty
    "anyonecantalkaboutwanting"         → int.Parse if non-empty
    "thinkaboutdrinkinglessalcohol"     → int.Parse if non-empty
    "mydrinkinguse"                     → int.Parse if non-empty
    "noneedformetothinkabout"           → int.Parse if non-empty
    "actuallychangingmydrinking"        → int.Parse if non-empty
    "drinkinglessalcohol"               → int.Parse if non-empty
    "precontemplationscale"             → int.Parse if non-empty
    "contemplationscale"                → int.Parse if non-empty
    "actionscale"                       → int.Parse if non-empty
    "ddldimensionfourscore"             → int.Parse if non-empty
    "statusofchange"                    → int.Parse if non-empty
    "comments4"                         → string, .Trim()
    "dimension4problems"                → string, .Trim()

Note: IdontThinkUseDrugsTooMuch uses a 0 default (explicit = 0 before conditional parse)
whereas all other int fields in this method are nullable (conditional-only parse). This
is a minor inconsistency in the default-handling approach.

PK lookup: laas.FirstOrDefault(x => x.Id == aa.Id)
________________________________________

13. SaveAdmissionAssessmentDimensionOneDisorder — ASAM Dimension 1: Substance Disorders

EF Core method: SaveAdmissionAssessmentDimensionOneDisorder()
Target table: TblAdmissionAssessmentDimensionOneDisorder
Source: SAMMS Dimension 1 form section

Business purpose:
Loads ASAM Dimension 1 — Acute Intoxication and Withdrawal Potential. This is the largest
method in the file (~55 fields). It stores substance use disorder diagnoses for eight substance
categories, complete prior treatment history across four modalities (medically assisted
withdrawal, inpatient rehabilitation, intensive outpatient, outpatient treatment) with count,
frequency, and completion data for each, prior MAT (Medication Assisted Treatment) history
including which medications were used and how helpful they were, drug procurement methods,
and the overall dimension score.

Column mapping (switch cases — key groups):

SUBSTANCE DISORDER FLAGS (int? — conditional parse only):
    "opioiddisorderpresent"             → xa.OpioidDisorderPresent
    "alcoholdisorderpresent"            → xa.AlcoholDisorderPresent
    "sedativedisorderpresent"           → xa.SedativeDisorderPresent
    "stimulantdisorderpresent"          → xa.StimulantDisorderPresent
    "cannabisdisorderpresent"           → xa.CannabisDisorderPresent
    "hallucinogendisorderpresent"       → xa.HallucinogenDisorderPresent
    "inhakantdisorderpresent"           → xa.InhalantDisorderPresent
    "phencyclidinedisorderpresent"      → xa.PhencyclidineDisorderPresent

TREATMENT HISTORY (int? per modality with count/completion/recent sub-fields):
    "medicallyassistedwithdrawal"                       → MedicallyAssistedWithdrawal
    "medicallyassistedwithdrawalhowmanytimes"            → MedicallyAssistedWithdrawalHowManyTimes
    "medicallyassistedwithdrawalrecenttimes"             → MedicallyAssistedWithdrawalRecentTimes
    "ddlmedicallyassistedwithdrawal"                    → DdlmedicallyAssistedWithdrawal
    "chkmedicallyassistedwithdrawal"                    → ChkMedicallyAssistedWithdrawal (bool.Parse)
    "inpatientrehabilitation"                           → InpatientRehabilitation
    "inpatientrehabilitationhowmanytimes"               → InpatientRehabilitationHowManyTimes
    "inpatientrehabilitationsuccessfullycomplete"       → InpatientRehabilitationSuccessfullyComplete
    "inpatientrehabilitationrecenttimes"                → InpatientRehabilitationRecentTimes
    "ddlinpatientrehabilitation"                        → DdlinpatientRehabilitation
    "chkinpatientrehabilitation"                        → ChkInpatientRehabilitation (bool.Parse)
    "intensiveoutpatienttreatments"                     → IntensiveOutpatientTreatments
    "intensiveoutpatienthowmanytimes"                   → IntensiveOutpatientHowManyTimes
    "intensiveoutpatientsuccessfullycomplete"           → IntensiveOutpatientSuccessfullyComplete
    "intensiveoutpatientrecenttimes"                    → IntensiveOutpatientRecentTimes
    "ddlintensiveoutpatient"                            → DdlintensiveOutpatient
    "chkintensiveoutpatienttreatments"                  → ChkIntensiveOutpatientTreatments (bool.Parse)
    "outpatienttreatment"                               → OutpatientTreatment
    "outpatienttreatmenthowmanytimes"                   → OutpatientTreatmentHowManyTimes
    "outpatienttreatmentsuccessfullycomplete"           → OutpatientTreatmentSuccessfullyComplete
    "outpatienttreatmentrecenttimes"                    → OutpatientTreatmentRecentTimes
    "ddloutpatienttreatment"                            → DdloutpatientTreatment
    "chkoutpatienttreatment"                            → ChkOutpatientTreatment (bool.Parse)

MAT HISTORY:
    "previousmat"                                       → PreviousMat (int?)
    "previousmatmethadone"                              → PreviousMatmethadone (bool.Parse)
    "previousmatbuprenorphine"                          → PreviousMatbuprenorphine (bool.Parse)
    "previousmatnaltrexone"                             → PreviousMatnaltrexone (bool.Parse)
    "previousmatwhatwasyourdose"                        → PreviousMatwhatWasYourDose (string direct)
    "previousmatwasithelpful"                           → PreviousMatwasItHelpful (int?)
    "howlongdidyoutakeit"                               → HowLongDidYouTakeIt (int?)
    "ddlhowlongdidyoutakeit"                            → DdlhowLongDidYouTakeIt (int?)
    "longestperiodofsobriety"                           → LongestPeriodOfSobriety (int?)
    "ddllongestperiodofsobrietyfromallsubstances"       → DdllongestPeriodOfSobrietyFromAllSubstances
    "chkpreviousmat"                                    → ChkPreviousMat (bool.Parse)

DRUG PROCUREMENT:
    "howdoyouprocurethedrug"                            → HowDoYouProcureTheDrug (int?)
    "buyonthestreet"                                    → BuyOnTheStreet (bool.Parse)
    "freefromfamily"                                    → FreeFromFamily (bool.Parse)
    "prescriptionfromhealthcareprovider"                → PrescriptionFromHealthcareProvider (bool.Parse)
    "sellinguseownsupply"                               → SellingUseOwnSupply (bool.Parse)
    "theft"                                             → Theft (bool.Parse)

SCORES / COMMENTS:
    "ddldimensiononescore"                              → DdldimensionOneScore (int?)
    "substanceusehistorycomments"                       → SubstanceUseHistoryComments (string direct)
    "comments"                                          → Comments (string direct)

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

14. SaveAdmissionAssessmentDimensionTwo — ASAM Dimension 2: Biomedical Conditions

EF Core method: SaveAdmissionAssessmentDimensionTwo()
Target table: TblAdmissionAssessmentDimensionTwo
Source: SAMMS Dimension 2 form section

Business purpose:
Loads ASAM Dimension 2 — Biomedical Conditions and Complications. Records the patient's
medical history including 23+ chronic condition flags, four infectious disease flags (HIV,
Hepatitis A/B/C/D, Tuberculosis), sensory impairments, and other health indicators. Also
captures allergies as free text, tobacco use, the overall dimension score, and free-text
diagnosis and problem description fields.

Column mapping (switch cases):

    "sitecode"                  → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                        → xa.Id = 0; int.Parse if non-empty
    "admissionassessmentid"     → int.Parse if non-empty
    "preadmissionid"            → int.Parse if non-empty
    "allergies"                 → string direct
    "asthma"                    → bool.Parse if non-empty
    "blindness"                 → bool.Parse if non-empty
    "cancer"                    → bool.Parse if non-empty
    "chronicpain"               → bool.Parse if non-empty
    "copdemphysema"             → bool.Parse if non-empty
    "deafness"                  → bool.Parse if non-empty
    "diabetes"                  → bool.Parse if non-empty
    "epilepsyseizures"          → bool.Parse if non-empty
    "gerd"                      → bool.Parse if non-empty
    "hearingloss"               → bool.Parse if non-empty
    "heartdisease"              → bool.Parse if non-empty
    "hepatitisa"                → bool.Parse if non-empty
    "hepatitisb"                → bool.Parse if non-empty
    "hepatitisc"                → bool.Parse if non-empty
    "hepatitisd"                → bool.Parse if non-empty
    "highbloodpressure"         → bool.Parse if non-empty
    "highcholesterol"           → bool.Parse if non-empty
    "hiv"                       → bool.Parse if non-empty
    "liverdisease"              → bool.Parse if non-empty
    "other"                     → bool.Parse if non-empty
    "othertxt"                  → string direct
    "poorvision"                → bool.Parse if non-empty
    "primarycarepractitioner"   → int.Parse if non-empty
    "renalkidneydisease"        → bool.Parse if non-empty
    "tuberculosis"              → bool.Parse if non-empty
    "doyouhaveanyconcerns"      → int.Parse if non-empty
    "doyouusetobacco"           → int.Parse if non-empty
    "ddldimensiontwoscore"      → int.Parse if non-empty
    "diagnosedcomment2"         → string direct
    "dimension2problems"        → string direct
    "comments2"                 → string direct

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

15. SaveAdmissionAssessmentDimensionThree — ASAM Dimension 3: Mental Health Conditions

EF Core method: SaveAdmissionAssessmentDimensionThree()
Target table: TblAdmissionAssessmentDimensionThree
Source: SAMMS Dimension 3 form section

Business purpose:
Loads ASAM Dimension 3 — Emotional, Behavioral, and Cognitive Conditions. Stores the
patient's mental health disorder flags, an overall dimension score, a free-text problem
description, and comments. The flags include Agoraphobia, Anxiety, Bipolar Disorder,
Depression, and other clinically relevant diagnoses present at the time of admission.

Column mapping (switch cases):

    "sitecode"                  → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                        → xa.Id = 0; int.Parse if non-empty
    "admissionassessmentid"     → int.Parse if non-empty
    "preadmissionid"            → int.Parse if non-empty
    "agoraphobia"               → bool.Parse if non-empty
    "anxiety"                   → bool.Parse if non-empty
    "bipolardisorder"           → bool.Parse if non-empty
    "depression"                → bool.Parse if non-empty
    (additional mental health condition fields per SAMMS form version)
    "ddldimensionthreescore"    → int.Parse if non-empty
    "comments3"                 → string direct
    (dimension3problems field   → string direct)

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

16. SaveAdmissionAssessmentDimensionFiveSubstanceUse — ASAM Dimension 5: Relapse Risk

EF Core method: SaveAdmissionAssessmentDimensionFiveSubstanceUse()
Target table: TblAdmissionAssessmentDimensionFiveSubstanceUse
Source: SAMMS Dimension 5 form section

Business purpose:
Loads ASAM Dimension 5 — Relapse, Continued Use, or Continued Problem Potential. Captures
risk factors that may jeopardize recovery: overdose history, whether substance use has
worsened physical and mental health, legal involvement (arrests, probation, open court
cases, child custody and DFS cases), employment impact, financial stress, and family
composition factors (children in home). Also stores dimension comments and problem narrative.

Column mapping (switch cases):

    "sitecode"                          → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                → xa.Id = 0; int.Parse if non-empty
    "admissionassessmentid"             → int.Parse if non-empty
    "preadmissionid"                    → int.Parse if non-empty
    "hadanoverdose"                     → int.Parse if non-empty
    "yourphysicalhealthworse"           → int.Parse if non-empty
    "yourphysicalmetalworse"            → int.Parse if non-empty (note: 'metal' typo preserved from source)
    "haveyoucalled911"                  → int.Parse if non-empty
    "substanceusejeopardized"           → int.Parse if non-empty
    "causedproblemsatyourjob"           → int.Parse if non-empty
    "havinganyfinancialtroubles"        → int.Parse if non-empty
    "doesyourtempertend"                → int.Parse if non-empty
    "haveyoueverbeenarrested"           → int.Parse if non-empty
    "riskofbeingarrested"               → int.Parse if non-empty
    "openorpendingcourtcases"           → int.Parse if non-empty
    "areyouonprobation"                 → int.Parse if non-empty
    "legalcustodyofyourchildren"        → int.Parse if non-empty
    "anyopencaseswithlocaldepartment"   → int.Parse if non-empty
    "childrenliveinyourhome"            → int.Parse if non-empty
    "comments"                          → string direct
    "dimensionfivecomments"             → string direct
    "dimension5problems"                → string direct

PK lookup: dbaad5.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

17. SaveAdmissionAssessmentDimensionSix — ASAM Dimension 6: Recovery Environment

EF Core method: SaveAdmissionAssessmentDimensionSix()
Target table: TblAdmissionAssessmentDimensionSix
Source: SAMMS Dimension 6 form section

Business purpose:
Loads ASAM Dimension 6 — Recovery/Living Environment. Captures factors in the patient's
environment that support or threaten recovery: housing stability (behind on rent/utilities),
financial security, employment status, income source, drug-selling culture in the neighborhood,
family recovery connections, peer support access, and the overall dimension score. Also stores
dimension comments and problem narrative.

Column mapping (switch cases):

    "sitecode"                                  → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                        → xa.Id = 0; int.Parse if non-empty
    "admissionassessmentid"                     → int.Parse if non-empty
    "preadmissionid"                            → int.Parse if non-empty
    "anypeersupport"                            → int.Parse if non-empty
    "areyoubehindonyourrent"                    → int.Parse if non-empty
    "areyoubehindonyourutility"                 → int.Parse if non-empty
    "comments"                                  → string direct
    "ddldimensionsixscore"                      → int.Parse if non-empty
    "dimension6problems"                        → string direct
    "dimensionsixcomments"                      → string direct
    "doyouhaveenoughmoney"                      → int.Parse if non-empty
    "doyouhavejob"                              → int.Parse if non-empty
    "doyouhavesourceofincome"                   → int.Parse if non-empty
    "drugsellingcommoninyourneighborhood"        → int.Parse if non-empty
    "familymemberswhoareinrecovery"             → int.Parse if non-empty

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

18. SaveReAssessment — Re-Assessment Header

EF Core method: SaveReAssessment()
Target table: TblReAssessment
Source: SAMMS re-assessment header form

Business purpose:
Loads the root header record for a periodic re-assessment. Structurally mirrors the
SaveAdmissionAssessment header pattern with the addition of TimeInTreatment — a numeric
field capturing how long the patient has been enrolled in treatment at the time of this
re-assessment. All eight re-assessment domain sub-tables link back to this record via
their ReassessmentId foreign key.

Column mapping (switch cases):

    "sitecode"          → ra.SiteCode = sc; ra.LastModAt = runat
    "id"                → ra.Id = 0; int.Parse if non-empty
    "preadmissionid"    → ra.PreAdmissionId = 0; int.Parse if non-empty
    "dataformid"        → ra.DataFormId = 0; int.Parse if non-empty
    "clientid"          → ra.ClientId = 0; int.Parse if non-empty
    "createdby"         → string direct
    "createdon"         → DateTime.Parse if length > 6
    "modifiedby"        → string direct
    "modifiedon"        → DateTime.Parse if length > 6
    "isdeleted"         → false default; bool.Parse if non-empty
    "version"           → string direct
    "timeintreatment"   → int.Parse if non-empty

Special handling — IsDeleted: identical to SaveAdmissionAssessment (false default).

PK lookup: ldbras.FirstOrDefault(x => x.Id == ra.Id)
________________________________________

19. SaveReAssessmentOccupational — Re-Assessment: Occupational Domain

EF Core method: SaveReAssessmentOccupational()
Target table: TblReAssessmentOccupational
Source: SAMMS re-assessment occupational section

Business purpose:
Loads the occupational domain of the re-assessment. Captures the patient's current employment
status, whether they are enrolled as a student (full-time or part-time), and whether they
have found new work since the last assessment. This domain tracks economic self-sufficiency
progress across treatment episodes.

Column mapping (switch cases):

    "sitecode"                                  → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                        → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                            → int.Parse if non-empty
    "reassessmentid"                            → int.Parse if non-empty
    "areyoucurrentlyafulltimestudent"           → int.Parse if non-empty
    "areyoucurrentlyaparttimestudent"           → int.Parse if non-empty
    "haveyoufoundaparttimeorfulltimejob"        → int.Parse if non-empty
    "whatisyourcurrentemploymentstatus"         → int.Parse if non-empty
    "commentsoccupational"                      → string direct

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

20. SaveReAssessmentFamily — Re-Assessment: Family Domain

EF Core method: SaveReAssessmentFamily()
Target table: TblReAssessmentFamily
Source: SAMMS re-assessment family section

Business purpose:
Loads the family and social support domain of the re-assessment. Captures housing stability,
financial capacity, legal custody of children, open DFS/child services cases, and domestic
safety. These questions track whether the patient's family environment supports or threatens
sustained recovery between assessments.

Column mapping (switch cases):

    "sitecode"                                                  → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                                        → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                                            → int.Parse if non-empty
    "reassessmentid"                                            → int.Parse if non-empty
    "areyousafefromphysicalorsexualabuseinyourhome"             → int.Parse if non-empty
    "doyouhaveanyopencaseswithyourlocaldepartment"              → int.Parse if non-empty
    "doyouhaveenoughmoney"                                      → int.Parse if non-empty
    "doyouhavelegalcustodyofyourchildren"                       → int.Parse if non-empty
    "doyouhavestablehousingofyourown"                           → int.Parse if non-empty
    "commentsfamily"                                            → string direct

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

21. SaveReAssessmentLegal — Re-Assessment: Legal Domain

EF Core method: SaveReAssessmentLegal()
Target table: TblReAssessmentLegal
Source: SAMMS re-assessment legal section

Business purpose:
Loads the legal domain of the re-assessment. Tracks current legal entanglements that may
affect treatment compliance and recovery: drug treatment court involvement, probation/parole
status, open warrants, court fines, open criminal cases, pending court cases, and whether the
patient has been arrested since the last assessment.

Column mapping (switch cases):

    "sitecode"                                      → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                            → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                                → int.Parse if non-empty
    "reassessmentid"                                → int.Parse if non-empty
    "areyouinvolvedwithadrugtreatmentcourt"         → int.Parse if non-empty
    "areyouonprobationorpayrole"                    → int.Parse if non-empty
    "commentslegal"                                 → string direct
    "doyouhaveanyopencriminalcases"                 → int.Parse if non-empty
    "doyouhaveanyopenorpendingcourtcases"           → int.Parse if non-empty
    "doyouhaveanyopenwarrants"                      → int.Parse if non-empty
    "doyouowemoneyforcourtfinesorfees"              → int.Parse if non-empty
    "haveyoubeenarrested"                           → int.Parse if non-empty

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

22. SaveReAssessmentMentalHealth — Re-Assessment: Mental Health Domain

EF Core method: SaveReAssessmentMentalHealth()
Target table: TblReAssessmentMentalHealth
Source: SAMMS re-assessment mental health section

Business purpose:
Loads the mental health domain of the re-assessment. This is one of the smallest methods in
the file — only three clinical fields beyond the key fields. Tracks whether the patient has
a psychiatrist, whether they have been hospitalized for mental health reasons since the last
assessment, and a coded answer to how their mental health has changed overall.

Column mapping (switch cases):

    "sitecode"                                              → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                                    → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                                        → int.Parse if non-empty
    "reassessmentid"                                        → xa.ReAssessmentId (note: EF property is ReAssessmentId)
    "doyouhaveapsychiatrist"                                → int.Parse if non-empty
    "haveyoubeenhospitalizedformentalhealthreasons"         → int.Parse if non-empty
    "howhasyourmentalhealthchanged"                         → int.Parse if non-empty

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

23. SaveReAssessmentPhysicalHealth — Re-Assessment: Physical Health Domain

EF Core method: SaveReAssessmentPhysicalHealth()
Target table: TblReAssessmentPhysicalHealth
Source: SAMMS re-assessment physical health section

Business purpose:
Loads the physical health domain of the re-assessment. Captures HIV and Hepatitis C test
results using checkbox groups (positive/negative/NA), whether the patient has a primary care
provider, recent HIV/HCV testing, IV drug use since last assessment, unsafe sexual behavior,
ER or 911 use, and how physical health has changed overall. Includes conditional action
questions if the patient tested positive for HCV or HIV.

Column mapping (switch cases):

    "sitecode"                                          → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                                → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                                    → int.Parse if non-empty
    "reassessmentid"                                    → int.Parse if non-empty
    "chkboxhepatitiscnegative"                          → bool.Parse if non-empty
    "chkboxhepatitiscpostive"                           → bool.Parse if non-empty
    "chkboxhivnegative"                                 → bool.Parse if non-empty
    "chkboxhivpostive"                                  → bool.Parse if non-empty
    "chkboxna"                                          → bool.Parse if non-empty
    "commentsphysicalhealth"                            → string direct
    "doyouhaveaprimarycarepractitionerorclinic"         → int.Parse if non-empty
    "haveyoubeentestedforhivandhepatitisc"              → int.Parse if non-empty
    "haveyoucalled911orbeeniitheemergencyroom"          → int.Parse if non-empty
    "haveyouhadanyunsafesex"                            → int.Parse if non-empty
    "haveyouinjecteddrugs"                              → int.Parse if non-empty
    "howhasyourphysicalhealthchanged"                   → int.Parse if non-empty
    "ifyouwerehepatitiscpositive"                       → int.Parse if non-empty
    "ifyouwerehivpositive"                              → int.Parse if non-empty

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

24. SaveReAssessmentSubstanceUse — Re-Assessment: Substance Use Domain

EF Core method: SaveReAssessmentSubstanceUse()
Target table: TblReAssessmentSubstanceUse
Source: SAMMS re-assessment substance use section

Business purpose:
Loads the substance use domain of the re-assessment. This is a minimal domain with two
clinical questions: whether the patient has had an overdose since the last assessment, and
whether they are using tobacco or vaping nicotine. The domain is linked to the parent
re-assessment via ReAssessmentId.

Column mapping (switch cases):

    "sitecode"                          → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                    → int.Parse if non-empty
    "reassessmentid"                    → xa.ReAssessmentId (EF property name)
    "commentssubstanceuse"              → string direct
    "doyouusetobaccoorvapenicotine"     → int.Parse if non-empty
    "haveyouhadanoverdose"              → int.Parse if non-empty

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

25. SaveReAssessmentSocial — Re-Assessment: Social Domain

EF Core method: SaveReAssessmentSocial()
Target table: TblReAssessmentSocial
Source: SAMMS re-assessment social section

Business purpose:
Loads the social support domain of the re-assessment. Captures three key social recovery
factors: whether the patient has friends or family who do not drink or use drugs, whether
they have a support network they can rely on, and whether they are aware of peer support
resources in their area.

Column mapping (switch cases):

    "sitecode"                                                          → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                                                → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                                                    → int.Parse if non-empty
    "reassessmentid"                                                    → int.Parse if non-empty
    "commentssocial"                                                    → string direct
    "doyouhaveanyfriendsrorfamilymemberswhodontdrink"                   → int.Parse if non-empty
    "doyouhavefriendsandfamilywhoyoucancountontosupportyou"             → int.Parse if non-empty
    "doyouknowofanypeersupport"                                         → int.Parse if non-empty

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

26. SaveReAssessmentTreatment — Re-Assessment: Treatment Satisfaction Domain

EF Core method: SaveReAssessmentTreatment()
Target table: TblReAssessmentTreatment
Source: SAMMS re-assessment treatment section

Business purpose:
Loads the treatment satisfaction and goals domain of the re-assessment. Captures the
patient's satisfaction with their current treatment, whether they plan to taper off
medication, whether tapering is a long-term goal, and two free-text narrative fields:
what they have learned about their preferences during treatment and what needs the clinic
can still help them with.

Column mapping (switch cases):

    "sitecode"                              → xa.SiteCode = sc; xa.LastModAt = runat
    "id"                                    → xa.Id = 0; int.Parse if non-empty
    "preadmissionid"                        → int.Parse if non-empty
    "reassessmentid"                        → int.Parse if non-empty
    "clientid"                              → int.Parse if non-empty
    "areyousatisfiedwith"                   → int.Parse if non-empty
    "doyouplanontaperingoff"                → int.Parse if non-empty
    "iseventuallytaperingoff"               → int.Parse if non-empty
    "whathaveyoulearnedaboutwhatyouprefer"  → string direct
    "whatneedsdoyouhavethatwecanhelpyou"    → string direct

PK lookup: dbList.FirstOrDefault(x => x.Id == xa.Id)
________________________________________

27. SaveAssessmentSubstanceuseHistory — Assessment Substance Use History (RowChkSum)

File: BCAppCode/BHG-DR-LIB/SaveAssessments.cs
Class: SaveData (partial class)
Method: SaveAssessmentSubstanceuseHistory()

Method signature:
    public RCodes SaveAssessmentSubstanceuseHistory(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted but NOT used inside this method)
        BHG_DRContext db)        // EF context (created internally if null)

Target table: TblAssessmentSubstanceUseHistories (accessed as db.TblAssessmentSubstanceUseHistories)

Business purpose:
Loads substance use history detail rows attached to periodic re-assessment forms. Each row
represents one substance reported by the patient during a re-assessment episode (type, route,
amount, frequency, age of first use, withdrawal symptoms, etc.). This table allows analysts to
track substance use patterns over time across re-assessment intervals. This is one of only two
methods in the file that uses RowChkSum and RowState.

EF Core upsert logic — step by step:

Step 1 — Initialize RCodes
    rc = new RCodes {
        IsResult      = true,
        RowsProcessed = tbl.Rows.Count
    }

Step 2 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 3 — Capture execution timestamp
    DateTime runat = DateTime.Now;

Step 4 — Load ALL existing Azure rows for this site (no date filter)
    ldbras = db.TblAssessmentSubstanceUseHistories
        .Where(x => x.SiteCode == sc)
        .ToList();

The full site dataset is loaded. No date filter is applied.

Step 5 — Initialize new-row staging list
    List<TblAssessmentSubstanceUseHistory> xnras = new List<...>();

Step 6 — Loop through every row from the SAMMS DataTable
    foreach (DataRow dr in tbl.Rows)

Step 7 — Build a new model object via column switch, including RowChkSum and RowState

The "sitecode" case reads RowChkSum and sets RowState in one branch:

    case "sitecode":
        ra.SiteCode   = sc;
        ra.LastModAt  = runat;
        ra.RowChkSum  = int.Parse(dr["RowChkSum"].ToString());
        ra.RowState   = true;
        break;

This is an unconventional pattern — RowChkSum is not read through the column iteration
of the switch (i.e. there is no case "rowchksum") but is instead accessed directly by
column name (dr["RowChkSum"]) inside the "sitecode" case. This means RowChkSum is always
set at the same time SiteCode is processed, before any other columns are evaluated.

The "cltid" case applies the only RowState conditional logic in this method:

    case "cltid":
        ra.CltId = 0;
        if (dr[c.ColumnName].ToString().Length > 0)
        {
            ra.CltId = int.Parse(dr[c.ColumnName].ToString());
        }
        if (ra.CltId < 0) { ra.RowState = false; }
        break;

A negative CltId (e.g. -1) is a SAMMS sentinel meaning the patient record is logically
invalid or deleted. When CltId < 0, RowState is set to false, marking this substance use
row as inactive regardless of what RowChkSum says.

Full column mapping:

    "sitecode"          → SiteCode = sc; LastModAt = runat; RowChkSum = int.Parse(dr["RowChkSum"]); RowState = true
    "id"                → Id = 0; int.Parse if non-empty
    "preadmissionid"    → PreAdmissionId = 0; int.Parse if non-empty
    "assessmentformid"  → AssessmentFormId = 0; int.Parse if non-empty
    "cltid"             → CltId = 0; int.Parse if non-empty; if < 0 → RowState = false
    "createdon"         → DateTime.Parse if length > 6
    "dateoflastuse"     → DateTime.Parse if length > 6
    "txepisode"         → string direct
    "substancetype"     → string direct
    "substance"         → string direct
    "route"             → string direct
    "amount"            → string direct
    "frequencyoflastuse"→ string direct
    "peakuse"           → string direct
    "ageoffirstuse"     → string direct
    "listsymptoms"      → string direct
    "notes"             → string direct
    "masterid"          → int.Parse if non-empty
    "dateofreported"    → DateTime.Parse if length > 6
    "withdrawal"        → bool.Parse if non-empty

Step 8 — Match against existing Azure row by COMPOSITE key (SiteCode + Id)
    dbra = ldbras.FirstOrDefault(x => x.SiteCode == ra.SiteCode && x.Id == ra.Id);

This is a critical difference from all Group A/B methods. The lookup uses both SiteCode and
Id to prevent cross-clinic collisions. SiteCode == ra.SiteCode (which equals sc, since it is
forced from the parameter) and Id == ra.Id.

Step 9 — INSERT or UPDATE based on match result

    if (dbra == null):
        rc.RowsIns += 1
        xnras.Add(ra)                     // staged for batch insert

    else:
        rc.RowsUpd += 1
        dbra.PreAdmissionId   = ra.PreAdmissionId
        dbra.CltId            = ra.CltId
        dbra.AssessmentFormId = ra.AssessmentFormId
        dbra.AgeOfFirstUse    = ra.AgeOfFirstUse
        dbra.CreatedOn        = ra.CreatedOn
        dbra.Amount           = ra.Amount
        dbra.DateOfLastUse    = ra.DateOfLastUse
        dbra.DateOfReported   = ra.DateOfReported
        dbra.FrequencyOfLastUse = ra.FrequencyOfLastUse
        dbra.LastModAt        = ra.LastModAt
        dbra.ListSymptoms     = ra.ListSymptoms
        dbra.MasterID         = ra.MasterID
        dbra.Notes            = ra.Notes
        dbra.PeakUse          = ra.PeakUse
        dbra.Route            = ra.Route
        dbra.RowChkSum        = ra.RowChkSum    // always written
        dbra.RowState         = ra.RowState     // always written (may be false if CltId < 0)
        dbra.Substance        = ra.Substance
        dbra.SubstanceType    = ra.SubstanceType
        dbra.TxEpisode        = ra.TxEpisode
        dbra.Withdrawal       = ra.Withdrawal

Note: There is NO RowChkSum skip guard in this method. The pattern
    if (dbra.RowChkSum != ra.RowChkSum) { ...update fields... }
is NOT used. RowChkSum is stored in the database on every update, but no field writes
are skipped when the checksum is unchanged. Every existing row is unconditionally updated.

Step 10 — Commit all updates in one batch
    db.SaveChanges()

Step 11 — Insert all new rows using AddRange
    if (xnras.Count > 0):
        db.TblAssessmentSubstanceUseHistories.AddRange(xnras)
        db.SaveChanges()
________________________________________

28. SaveAdmissionAssessmentSubstanceuseHistory — Admission Assessment Substance Use History

File: BCAppCode/BHG-DR-LIB/SaveAssessments.cs
Class: SaveData (partial class)
Method: SaveAdmissionAssessmentSubstanceuseHistory()

Method signature:
    public RCodes SaveAdmissionAssessmentSubstanceuseHistory(
        DataTable tbl,           // rows from SAMMS for this clinic
        string sc,               // SiteCode e.g. "VBRA"
        DateTime wrkdt,          // work date (accepted but NOT used inside this method)
        BHG_DRContext db)        // EF context (created internally if null)

Target table: TblAdmissionAssessmentSubstanceUseHistory (accessed as db.TblAdmissionAssessmentSubstanceUseHistory)

Business purpose:
Loads substance use history detail rows attached to the initial admission assessment form.
Functionally equivalent to SaveAssessmentSubstanceuseHistory (section 27) but targets a
different Azure table and applies additional date parsing resilience. Each row records one
substance the patient disclosed at admission, enabling clinical review of the patient's
complete substance use history at the point of entry into treatment.

This method shares all structural patterns with method 27 (RowChkSum in "sitecode" case,
RowState=false when CltId < 0, composite PK lookup, no RowChkSum skip guard, batched insert).

Key differences from SaveAssessmentSubstanceuseHistory:

1. Target table is different:
       db.TblAdmissionAssessmentSubstanceUseHistory  (not TblAssessmentSubstanceUseHistories)

2. AssessmentFormId is NOT present in this method's column mapping.
   The AssessmentFormId case does not appear in this method's switch statement.

3. Three date fields have per-field try/catch error handling (unique in the entire file):

    case "createdon":
        if (dr[c.ColumnName].ToString().Length > 6)
        {
            try
            {
                ra.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("CreatedOn: " + dr[c.ColumnName].ToString());
            }
        }
        break;

    case "dateoflastuse":
        same try/catch pattern with Console.WriteLine("Dateoflastuse: ...")

    case "dateofreported":
        if (dr[c.ColumnName].ToString().Trim().Length > 6)   // .Trim() added here
        {
            try
            {
                ra.DateOfReported = DateTime.Parse(dr[c.ColumnName].ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("DateofReported: " + dr[c.ColumnName].ToString());
            }
        }
        break;

    These inner try/catch blocks prevent a malformed date string from aborting the entire
    row or method. When a date fails to parse, the field is left null and the raw value
    is printed to console for diagnostic purposes. The outer try/catch is NOT triggered.
    This is the only place in SaveAssessments.cs where individual field errors are
    silently swallowed (with console logging) rather than propagated.

4. DateOfReported uses .Trim() before the length check:
        dr[c.ColumnName].ToString().Trim().Length > 6
   The sibling method 27 uses:
        dr[c.ColumnName].ToString().Length > 6 (no trim)
   This means a value like "  " (whitespace only) would pass the length > 6 check in
   method 27 and throw a parse exception, but is safely rejected in method 28 by the trim.

Full column mapping (same as method 27 except where noted):

    "sitecode"          → SiteCode = sc; LastModAt = runat; RowChkSum = int.Parse(dr["RowChkSum"]); RowState = true
    "id"                → Id = 0; int.Parse if non-empty
    "preadmissionid"    → PreAdmissionId = 0; int.Parse if non-empty
    "cltid"             → CltId = 0; int.Parse if non-empty; if < 0 → RowState = false
    "createdon"         → DateTime.Parse if length > 6 — WITH try/catch + Console.WriteLine
    "dateoflastuse"     → DateTime.Parse if length > 6 — WITH try/catch + Console.WriteLine
    "txepisode"         → string direct
    "substancetype"     → string direct
    "substance"         → string direct
    "route"             → string direct
    "amount"            → string direct
    "frequencyoflastuse"→ string direct
    "peakuse"           → string direct
    "ageoffirstuse"     → string direct
    "listsymptoms"      → string direct
    "notes"             → string direct
    "dateofreported"    → .Trim() + length > 6 — WITH try/catch + Console.WriteLine
    "withdrawal"        → bool.Parse if non-empty

PK lookup (composite — same as method 27):
    ldbras.FirstOrDefault(x => x.SiteCode == ra.SiteCode && x.Id == ra.Id)

RowChkSum and RowState: same pattern as method 27 — stored on every update, no skip guard,
RowState=false when CltId < 0.
________________________________________

29. Destination Tables — Azure BHG_DR

All 19 destination tables reside in the Azure SQL Database BHG_DR. Each is accessed via
BHG_DRContext (EF Core DbContext). The C# property names listed are the EF model property
names used in the Save methods' update blocks.
________________________________________

29a. TblAdmissionAssessment

EF Model: db.TblAdmissionAssessment
Primary Key: Id (SiteCode stored but not part of PK lookup in this method)

C# Property (EF)        SQL Column          Type                Notes
----------------        ----------          ----                -----
SiteCode                SiteCode            varchar(25)         Clinic identifier
Id                      Id                  int                 Assessment ID
LastModAt               LastModAt           datetime            ETL last write timestamp
PreAdmissionId          PreAdmissionId      int                 Linked pre-admission record
DataFormId              DataFormId          int                 Form template ID
ClientId                ClientId            int                 Patient/client ID
CreatedBy               CreatedBy           varchar             Form creator username
CreatedOn               CreatedOn           datetime (null)     Form creation date
ModifiedBy              ModifiedBy          varchar             Last modifier username
ModifiedOn              ModifiedOn          datetime (null)     Last modification date
IsDeleted               IsDeleted           bit                 Logical delete flag
Version                 Version             varchar             Form version
________________________________________

29b. TblAdmissionAssessmentSummary

EF Model: db.TblAdmissionAssessmentSummary
Primary Key: Id

C# Property (EF)                                    Type                Notes
----------------                                    ----                -----
SiteCode                                            varchar(25)         Clinic identifier
Id                                                  int                 Summary record ID
LastModAt                                           datetime            ETL timestamp
PreAdmissionId                                      int                 Linked pre-admission record
AdmissionAssessmentId                               int                 Parent assessment ID
Ddlrecommendation                                   int                 ASAM care level recommendation code
OpioidTreatmentServices                             int (null)          Opioid treatment services code
WithdrawalManagement                                int (null)          Withdrawal management code
ClinicalSummary                                     varchar             Clinical narrative
AsamrecommendationForLevel                          int (null)          ASAM level code
LevelOfCareAtVariance                               varchar             Level of care variance
SummaryComments                                     varchar             Summary comments
AdmissionAssessmentStaffSignature                   varchar             Staff signature
AdmissionAssessmentStaffSignatureBy                 varchar             Staff signatory name
AdmissionAssessmentStaffSignatureDate               datetime (null)     ⚠ NEVER WRITTEN (bug in mapping)
AdmissionAssessmentProviderSignature                varchar             Provider signature
AdmissionAssessmentProviderSignatureBy              varchar             Provider signatory name
AdmissionAssessmentProviderSignatureDate            datetime (null)     Provider signature date
AdmissionAssessmentPatientSignature                 varchar             Patient signature
AdmissionAssessmentPatientSignatureBy               varchar             Patient signatory name
AdmissionAssessmentPatientSignatureDate             datetime (null)     Patient date OR staff date (see bug note)
AdmissionAssessmentSupervisorSignature              varchar             Supervisor signature
AdmissionAssessmentSupervisorSignatureBy            varchar             Supervisor signatory name
AdmissionAssessmentSupervisorSignatureDate          datetime (null)     Supervisor signature date
________________________________________

29c. TblAdmissionAssessmentDimensionFour

EF Model: db.TblAdmissionAssessmentDimensionFour — Primary Key: Id
Key columns: Id, PreAdmissionId, AdmissionAssessmentId, SiteCode, LastModAt
Clinical data: IdontThinkUseDrugsTooMuch, TryingTtoDrinklessThanUsed, IenjoyMyDrinking,
StageOfChange, IshouldCutDownOnMyDrinking, WasteOfTimeToThinkAboutMyDrinking,
RecentlyChangedMyDrinking, AnyoneCanTalkAboutWanting, ThinkAboutDrinkingLessAlcohol,
MyDrinkingUse, NoNeedForMeToThinkAbout, ActuallyChangingMyDrinking, DrinkingLessAlcohol,
PrecontemplationScale, ContemplationScale, ActionScale, DdldimensionFourScore, StatusofChange,
Comments4, Dimension4Problems
________________________________________

29d. TblAdmissionAssessmentDimensionOneDisorder

EF Model: db.TblAdmissionAssessmentDimensionOneDisorder — Primary Key: Id
Key columns: Id, PreAdmissionId, AdmissionAssessmentId, SiteCode, LastModAt
Clinical data: All substance disorder flags (8 substances), complete treatment history
(4 modalities x count/completion/recent fields), all DDL coded equivalents, all checkbox
fields, full MAT history, drug procurement flags, scores, and comment fields.
See section 13 for complete column list.
________________________________________

29e. TblAdmissionAssessmentDimensionTwo

EF Model: db.TblAdmissionAssessmentDimensionTwo — Primary Key: Id
Key columns: Id, PreAdmissionId, AdmissionAssessmentId, SiteCode, LastModAt
Clinical data: Allergies, 23 medical condition bool flags, 4 infectious disease flags,
DoYouUseTobacco, DoYouHaveAnyConcerns, PrimaryCarePractitioner, DdldimensionTwoScore,
DiagnosedComment2, Dimension2Problems, Comments2
________________________________________

29f. TblAdmissionAssessmentDimensionThree

EF Model: db.TblAdmissionAssessmentDimensionThree — Primary Key: Id
Key columns: Id, PreAdmissionId, AdmissionAssessmentId, SiteCode, LastModAt
Clinical data: Mental health condition bool flags (Agoraphobia, Anxiety, BipolarDisorder,
Depression, and others), DdldimensionThreeScore, Comments3, Dimension3Problems
________________________________________

29g. TblAdmissionAssessmentDimensionFiveSubstanceUse

EF Model: db.TblAdmissionAssessmentDimensionFiveSubstanceUse — Primary Key: Id
Key columns: Id, PreAdmissionId, AdmissionAssessmentId, SiteCode, LastModAt
Clinical data: HadAnOverdose, YourPhysicalHealthWorse, YourPhysicalMentalWorse,
HaveYouCalled911, SubstanceUseJeopardized, CausedProblemsAtYourJob, HavingAnyFinancialTroubles,
DoesYourTemperTend, HaveYouEverBeenArrested, RiskOfBeingArrested, OpenOrPendingCourtCases,
AreYouOnProbation, LegalCustodyOfYourChildren, AnyOpenCasesWitHlocalDepartment,
ChildrenLiveInYourHome, Comments, DimensionFiveComments, Dimension5Problems
________________________________________

29h. TblAdmissionAssessmentDimensionSix

EF Model: db.TblAdmissionAssessmentDimensionSix — Primary Key: Id
Key columns: Id, PreAdmissionId, AdmissionAssessmentId, SiteCode, LastModAt
Clinical data: AreYouBehindOnYourRent, AreYouBehindOnYourUtility, AnyPeerSupport,
DoYouHaveEnoughMoney, DoYouHaveJob, DoYouHaveSourceOfIncome,
DrugSellingCommonInYourNeighborhood, FamilyMembersWhoAreInRecovery,
DdldimensionSixScore, DimensionSixComments, Dimension6Problems, Comments
________________________________________

29i. TblReAssessment

EF Model: db.TblReAssessment — Primary Key: Id
Key columns: Id, PreAdmissionId, DataFormId, ClientId, SiteCode, LastModAt
Clinical data: CreatedBy, CreatedOn, ModifiedBy, ModifiedOn, IsDeleted, Version, TimeInTreatment
________________________________________

29j. TblReAssessmentOccupational

EF Model: db.TblReAssessmentOccupational — Primary Key: Id
Key columns: Id, PreAdmissionId, ReassessmentId, SiteCode, LastModAt
Clinical data: WhatIsYourCurrentEmploymentStatus, AreYouCurrentlyAfulltimeStudent,
AreYouCurrentlyAparttimeStudent, HaveYouFoundAparttimeOrFulltimeJob, CommentsOccupational
________________________________________

29k. TblReAssessmentFamily

EF Model: db.TblReAssessmentFamily — Primary Key: Id
Key columns: Id, PreAdmissionId, ReassessmentId, SiteCode, LastModAt
Clinical data: AreYouSafeFromPhysicalOrSexualAbuseInYourHome,
DoYouHaveAnyOpenCasesWithYourLocalDepartment, DoYouHaveEnoughMoney,
DoYouHaveLegalCustodyOfYourChildren, DoYouHaveStableHousingOfYourOwn, CommentsFamily
________________________________________

29l. TblReAssessmentLegal

EF Model: db.TblReAssessmentLegal — Primary Key: Id
Key columns: Id, PreAdmissionId, ReassessmentId, SiteCode, LastModAt
Clinical data: AreYouInvolvedWithAdrugTreatmentCourt, AreYouOnProbationOrPayrole,
DoYouHaveAnyOpenCriminalCases, DoYouHaveAnyOpenOrPendingCourtCases, DoYouHaveAnyOpenWarrants,
DoYouOweMoneyForCourtFinesOrFees, HaveYouBeenArrested, CommentsLegal
________________________________________

29m. TblReAssessmentMentalHealth

EF Model: db.TblReAssessmentMentalHealth — Primary Key: Id
Key columns: Id, PreAdmissionId, ReAssessmentId, SiteCode, LastModAt
Clinical data: DoYouHaveApsychiatrist, HaveYouBeenHospitalizedForMentalHealthReasons,
HowHasYourMentalHealthChanged
________________________________________

29n. TblReAssessmentPhysicalHealth

EF Model: db.TblReAssessmentPhysicalHealth — Primary Key: Id
Key columns: Id, PreAdmissionId, ReassessmentId, SiteCode, LastModAt
Clinical data: ChkboxHepatitisCnegative, ChkboxHepatitisCpostive, ChkboxHivnegative,
ChkboxHivpostive, ChkboxNa, DoYouHaveAprimaryCarePractitionerOrClinic,
HaveYouBeenTestedForHivandHepatitisC, HaveYouCalled911OrBeeniItheEmergencyRoom,
HaveYouHadAnyUnsafeSex, HaveYouInjectedDrugs, HowHasYourPhysicalHealthChanged,
IfYouWereHepatitisCpositive, IfYouWereHivpositive, CommentsPhysicalHealth
________________________________________

29o. TblReAssessmentSubstanceUse

EF Model: db.TblReAssessmentSubstanceUse — Primary Key: Id
Key columns: Id, PreAdmissionId, ReAssessmentId, SiteCode, LastModAt
Clinical data: HaveYouHadAnOverdose, DoYouUseTobaccoOrVapeNicotine, CommentsSubstanceUse
________________________________________

29p. TblReAssessmentSocial

EF Model: db.TblReAssessmentSocial — Primary Key: Id
Key columns: Id, PreAdmissionId, ReassessmentId, SiteCode, LastModAt
Clinical data: DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink,
DoYouHaveFriendsAndFamilyWhoYouCanCountOnToSupportYou, DoYouKnowOfAnyPeerSupport,
CommentsSocial
________________________________________

29q. TblReAssessmentTreatment

EF Model: db.TblReAssessmentTreatment — Primary Key: Id
Key columns: Id, PreAdmissionId, ReassessmentId, ClientId, SiteCode, LastModAt
Clinical data: AreYouSatisfiedWith, DoYouPlanOnTaperingOff, IsEventuallyTaperingOff,
WhatHaveYouLearnedAboutWhatYouPrefer, WhatNeedsDoYouHaveThatWeCanHelpYou
________________________________________

29r. TblAssessmentSubstanceUseHistories

EF Model: db.TblAssessmentSubstanceUseHistories
Primary Key: SiteCode + Id (composite — matched as SiteCode == sc && Id == ra.Id)

C# Property (EF)        SQL Column          Type                Notes
----------------        ----------          ----                -----
SiteCode                SiteCode            varchar(25)         PK Part 1 — clinic code
Id                      Id                  int                 PK Part 2 — substance row ID
RowChkSum               RowChkSum           int                 Checksum stored per row
RowState                RowState            bit (null)          true=active; false=invalid (CltId < 0)
LastModAt               LastModAt           datetime            ETL last write timestamp
PreAdmissionId          PreAdmissionId      int                 Linked pre-admission record
AssessmentFormId        AssessmentFormId    int                 Linked assessment form ID
CltId                   CltId               int                 Patient/client ID (negative = invalid)
CreatedOn               CreatedOn           datetime (null)     Record creation date
DateOfLastUse           DateOfLastUse       datetime (null)     Date of last use of this substance
TxEpisode               TxEpisode           varchar             Treatment episode identifier
SubstanceType           SubstanceType       varchar             Type category of substance
Substance               Substance           varchar             Specific substance name
Route                   Route               varchar             Route of administration
Amount                  Amount              varchar             Amount used
FrequencyOfLastUse      FrequencyOfLastUse  varchar             Frequency of use
PeakUse                 PeakUse             varchar             Peak use description
AgeOfFirstUse           AgeOfFirstUse       varchar             Age at first use
ListSymptoms            ListSymptoms        varchar             Reported symptoms
Notes                   Notes               varchar             Free-text notes
MasterID                MasterID            int (null)          Master record link
DateOfReported          DateOfReported      datetime (null)     Date this row was reported
Withdrawal              Withdrawal          bit (null)          Withdrawal symptoms present
________________________________________

29s. TblAdmissionAssessmentSubstanceUseHistory

EF Model: db.TblAdmissionAssessmentSubstanceUseHistory
Primary Key: SiteCode + Id (composite — same as 29r)

C# Property (EF)        SQL Column          Type                Notes
----------------        ----------          ----                -----
SiteCode                SiteCode            varchar(25)         PK Part 1
Id                      Id                  int                 PK Part 2
RowChkSum               RowChkSum           int                 Checksum stored per row
RowState                RowState            bit (null)          true=active; false=invalid (CltId < 0)
LastModAt               LastModAt           datetime            ETL timestamp
PreAdmissionId          PreAdmissionId      int                 Linked pre-admission record
CltId                   CltId               int                 Patient/client ID
CreatedOn               CreatedOn           datetime (null)     With try/catch date parsing
DateOfLastUse           DateOfLastUse       datetime (null)     With try/catch date parsing
TxEpisode               TxEpisode           varchar             Treatment episode identifier
SubstanceType           SubstanceType       varchar             Substance type category
Substance               Substance           varchar             Specific substance name
Route                   Route               varchar             Route of administration
Amount                  Amount              varchar             Amount used
FrequencyOfLastUse      FrequencyOfLastUse  varchar             Frequency of use
PeakUse                 PeakUse             varchar             Peak use description
AgeOfFirstUse           AgeOfFirstUse       varchar             Age at first use
ListSymptoms            ListSymptoms        varchar             Symptoms reported
Notes                   Notes               varchar             Free-text notes
DateOfReported          DateOfReported      datetime (null)     With .Trim() + try/catch parsing
Withdrawal              Withdrawal          bit (null)          Withdrawal symptoms present

Note: AssessmentFormId is NOT present in this table — it exists only in TblAssessmentSubstanceUseHistories (29r).
________________________________________

30. Change Detection — RowChkSum

RowChkSum is the mechanism used by SelectConstructor to detect whether a row's data has
changed since the last ETL run.

How it is computed (at source, during SELECT by SelectConstructor):

    RowChkSum = CHECKSUM(
        <all mapped columns for this ActionKey/StepKey>
    )

Example for AdmissionAssessment:
    RowChkSum = CHECKSUM(
        Id, PreAdmissionId, DataFormId, ClientId, CreatedBy,
        CreatedOn, ModifiedBy, ModifiedOn, IsDeleted, Version
    )

How RowChkSum is used across the 19 methods:

Groups A and B (methods 1–17, SaveAdmissionAssessment through SaveReAssessmentTreatment):
    RowChkSum column IS present in the SELECT and in the DataTable returned.
    However, none of these 17 methods read or compare RowChkSum at all.
    The "rowchksum" case does not appear in any of their switch statements.
    Every existing row is overwritten unconditionally on every run.
    RowChkSum data is in the source DataTable but is completely ignored.

Group C Method 18 (SaveAssessmentSubstanceuseHistory):
    RowChkSum is read in the "sitecode" case via dr["RowChkSum"].ToString() and stored:
        ra.RowChkSum = int.Parse(dr["RowChkSum"].ToString())
    RowChkSum is written on every update (dbra.RowChkSum = ra.RowChkSum).
    But there is NO skip guard: if (dbra.RowChkSum != ra.RowChkSum) is NOT used.
    Checksum is stored but does not prevent field writes when unchanged.

Group C Method 19 (SaveAdmissionAssessmentSubstanceuseHistory):
    Same behavior as method 18. RowChkSum is stored but no skip guard is implemented.

What this means in practice:
- All 19 methods perform full field updates on every run for every matched row.
- For Groups A/B (17 methods), LastModAt is always updated to the current run timestamp
  even when no clinical data has changed.
- For Group C (2 methods), RowChkSum is persisted so downstream queries can detect when
  a row last changed, but it does not reduce ETL write volume in this implementation.
________________________________________

31. RowState — Active/Inactive Flag

RowState is a bit column (nullable) used as an active/inactive flag in Group C methods.

Value       Meaning
-----       -------
true (1)    Row is active — valid patient record
false (0)   Row is flagged inactive — CltId is negative (invalid/deleted patient in SAMMS)
NULL        Row has not been touched by RowState logic (Groups A/B rows never set RowState)

Which methods use RowState:

SaveAssessmentSubstanceuseHistory (method 18):
    YES — RowState is set true by default in the "sitecode" case.
    RowState is set false in the "cltid" case if ra.CltId < 0.
    RowState is always written on update: dbra.RowState = ra.RowState
    There is no full soft-delete reset cycle (unlike Save3pElig). RowState is simply
    derived per-row from the CltId value rather than by comparing to a prior full fetch.

SaveAdmissionAssessmentSubstanceuseHistory (method 19):
    YES — identical behavior to method 18.

All Group A/B methods (methods 1–17):
    NO — RowState is not mapped, not set, and not written in any of these 17 methods.
    Azure rows for these tables have no RowState lifecycle managed by this pipeline.

Usage in downstream queries:
    Select count(1) from TblAssessmentSubstanceUseHistories
    where SiteCode = 'VBRA' and RowState = 1
    — counts only active (valid) substance use history rows, excluding CltId < 0 rows.
________________________________________

32. Load Scoping Comparison

All 19 methods scope their Azure load to SiteCode = sc with no date filter applied.
This means every method loads the complete historical dataset for the clinic from Azure
into memory before processing.

Method                                              Azure Load Scope        Strategy
------                                              ----------------        --------
SaveAdmissionAssessment (1)                         SiteCode == sc          All-time full site load
SaveAdmissionAssessmentSummary (2)                  SiteCode == sc          All-time full site load
SaveAdmissionAssessmentDimensionfour (3)            SiteCode == sc          All-time full site load
SaveAdmissionAssessmentDimensionOneDisorder (4)     SiteCode == sc          All-time full site load
SaveAdmissionAssessmentDimensionTwo (5)             SiteCode == sc          All-time full site load
SaveAdmissionAssessmentDimensionThree (6)           SiteCode == sc          All-time full site load
SaveAdmissionAssessmentDimensionFiveSubstanceUse(7) SiteCode == sc          All-time full site load
SaveAdmissionAssessmentDimensionSix (8)             SiteCode == sc          All-time full site load
SaveReAssessment (9)                                SiteCode == sc          All-time full site load
SaveReAssessmentOccupational (10)                   SiteCode == sc          All-time full site load
SaveReAssessmentFamily (11)                         SiteCode == sc          All-time full site load
SaveReAssessmentLegal (12)                          SiteCode == sc          All-time full site load
SaveReAssessmentMentalHealth (13)                   SiteCode == sc          All-time full site load
SaveReAssessmentPhysicalHealth (14)                 SiteCode == sc          All-time full site load
SaveReAssessmentSubstanceUse (15)                   SiteCode == sc          All-time full site load
SaveReAssessmentSocial (16)                         SiteCode == sc          All-time full site load
SaveReAssessmentTreatment (17)                      SiteCode == sc          All-time full site load
SaveAssessmentSubstanceuseHistory (18)              SiteCode == sc          All-time full site load
SaveAdmissionAssessmentSubstanceuseHistory (19)     SiteCode == sc          All-time full site load

Why this matters:
- Since no date filter limits the in-memory load, every existing Azure row for the clinic
  is available for FirstOrDefault() matching on every run.
- Any row returned by the SAMMS SELECT that doesn't have a match in the Azure in-memory
  list is inserted as new. There is no phantom re-insert risk from a date-limited window
  (unlike Save3pArnote's 10-day window approach).
- For large clinics with many years of assessment data, the full-load strategy consumes
  more memory than a date-windowed approach, but ensures correctness.
________________________________________

33. Load Design Summary

Load type: Incremental upsert — always-update (Groups A/B) and RowChkSum-stored (Group C)

Per-run behavior for Groups A and B (methods 1–17):

    1. Load all Azure rows for this site (no date filter) into memory list
    2. Initialize new-row staging list
    3. Capture runat = DateTime.Now
    4. For each SAMMS source row:
       - Build model object via dynamic column switch
       - Force SiteCode = sc; LastModAt = runat
       - Find match: list.FirstOrDefault(x => x.Id == model.Id)
       - Not found → stage for batch insert; RowsIns++
       - Found → overwrite ALL fields unconditionally; RowsUpd++
         (no RowChkSum guard — every existing row is always updated)
    5. db.SaveChanges()         commit all updates in one batch
    6. if new rows: AddRange + db.SaveChanges()    batch insert

Per-run behavior for Group C (methods 18–19):

    1. Load all Azure rows for this site (no date filter) into memory list
    2. Initialize new-row staging list
    3. Capture runat = DateTime.Now
    4. For each SAMMS source row:
       - Build model object via dynamic column switch
       - "sitecode" case: force SiteCode = sc; LastModAt = runat;
                          RowChkSum = int.Parse(dr["RowChkSum"]); RowState = true
       - "cltid" case: if CltId < 0 → RowState = false
       - Find match: list.FirstOrDefault(x => x.SiteCode == sc && x.Id == model.Id)
       - Not found → stage for batch insert; RowsIns++
       - Found → overwrite ALL fields including RowChkSum + RowState; RowsUpd++
         (RowChkSum stored but no skip guard — still always updates)
    5. db.SaveChanges()         commit all updates
    6. if new rows: AddRange + db.SaveChanges()    batch insert

Per-record identity:
Methods 1–17 (Groups A/B)       → Id only
Methods 18–19 (Group C)         → SiteCode + Id (composite)
________________________________________

34. Error Handling and Recovery

All 19 methods share the same outer try/catch error handling pattern:

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

If an EF Core or parsing exception occurs:
- rc.IsResult is set to false
- The exception message is captured in rc.ExceptMsg
- The inner exception (if present) is captured in rc.ExceptInnerMsg
- The method returns normally — it does NOT re-throw
- Any rows committed before the exception (via the update-phase SaveChanges) remain written

Commit behavior on failure:

Methods 1–17 (Groups A/B):
    Two db.SaveChanges() calls (one for updates, one for inserts).
    If the first (updates) succeeds but the second (inserts) fails, all updates are
    committed and the new rows in xnList are lost for this run. They will be picked up
    as new inserts on the next run.
    If the exception occurs before any SaveChanges(), no rows are written.

Methods 18–19 (Group C):
    Same two-phase pattern. Same partial-commit behavior applies.

Special case — per-field date exceptions in method 19 (SaveAdmissionAssessmentSubstanceuseHistory):
    The three date fields (CreatedOn, DateOfLastUse, DateOfReported) have their own inner
    try/catch. These inner exceptions do NOT set rc.IsResult = false. The field is left
    null, a Console.WriteLine diagnostic message is emitted, and processing continues for
    the row and the method. Only exceptions from EF Core SaveChanges() or other code
    outside the inner try/catch blocks reach the outer catch and set IsResult = false.

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

A failed assessment run for a clinic will automatically be retried the next day.
________________________________________

35. RowTrax — Audit and Row Count Tracking

Table: tsk.tbl_RowTrax (Azure BHG_DR)

After each successful load for a clinic (if st.RowTrax == true):

    sd.SaveRowTrax(
        st.SiteCode,           -- e.g. "VBRA"
        st.WorkDate,           -- today
        st.TaskName,           -- e.g. "forms.tblAdmissionAssessment"
        SrcDt.Rows.Count,      -- rows returned from SAMMS this run
        destCount,             -- count in Azure
        null)

Destination count query (run against Azure):
    Select count(1) from [destination table]
    where SiteCode = 'VBRA'

Note: The source count is the DataTable row count from the incremental SAMMS fetch (not the
full source table count). The destination count reflects all rows for the clinic without any
RowState filter (since most assessment tables do not use RowState).

For Group C tables (methods 18–19), RowState = 1 filtering may optionally be applied to
the destination count to exclude invalid (CltId < 0) rows from the audit comparison.

The stored RowTrax records allow analysts to monitor assessment form volume over time and
detect clinics where assessment data may be lagging, miscounting, or diverging from source.
________________________________________

36. Known Anomalies and Quirks

Anomaly 1 — Staff Signature Date mapped to Patient Signature Date property (method 11):
    In SaveAdmissionAssessmentSummary, case "admissionassessmentstaffsignaturedate":
        aa.AdmissionAssessmentPatientSignatureDate = DateTime.Parse(...)
    should be:
        aa.AdmissionAssessmentStaffSignatureDate = DateTime.Parse(...)
    Effect: AdmissionAssessmentStaffSignatureDate in Azure is never populated.
    AdmissionAssessmentPatientSignatureDate may be overwritten by the staff date
    value from source.

Anomaly 2 — RowChkSum stored but no skip guard in Group C (methods 18–19):
    The pattern if (dbra.RowChkSum != ra.RowChkSum) { ...update fields... } is not
    implemented. RowChkSum is written to the database every run, but it does not
    prevent field writes when the data has not changed. Every row is always updated.

Anomaly 3 — No RowChkSum usage at all in Groups A and B (methods 1–17):
    17 of 19 methods have no change detection mechanism. Every daily run rewrites all
    fields and updates LastModAt for all existing rows at the clinic, even if no clinical
    data changed since the previous run. This increases Azure write load unnecessarily
    for high-volume assessment tables.

Anomaly 4 — Single-column PK lookup in Groups A/B vs composite in Group C:
    Methods 1–17: list.FirstOrDefault(x => x.Id == model.Id)
    Methods 18–19: list.FirstOrDefault(x => x.SiteCode == sc && x.Id == model.Id)
    If two clinics were to share the same Id value (which should not happen in practice
    since each clinic's DB is separate and SiteCode scopes the Azure load), Group A/B
    methods could theoretically update the wrong row. Group C avoids this risk.

Anomaly 5 — wrkdt parameter unused across all 19 methods:
    All 19 methods accept DateTime wrkdt but none use it in the method body. The
    parameter exists for interface consistency with SelectConstructor's routing pattern.

Anomaly 6 — Method name capitalization inconsistency:
    SaveAdmissionAssessmentDimensionfour uses lowercase 'f' in "four".
    All other Dimension methods (DimensionOneDisorder, DimensionTwo, DimensionThree,
    DimensionFiveSubstanceUse, DimensionSix) use uppercase first letters.

Anomaly 7 — yourphysicalmetalworse typo preserved in case statement (method 16):
    The source column name contains a typo: "yourphysicalmetalworse" instead of
    "yourphysicalmentalworse". The case statement matches the source typo:
        case "yourphysicalmetalworse": xa.YourPhysicalMentalWorse = ...
    The EF property name is correct (YourPhysicalMentalWorse) but the source
    column name typo must be preserved to match the DataTable column name from SAMMS.

Anomaly 8 — DateOfReported uses .Trim() only in method 19:
    SaveAdmissionAssessmentSubstanceuseHistory:
        if (dr[c.ColumnName].ToString().Trim().Length > 6)
    SaveAssessmentSubstanceuseHistory:
        if (dr[c.ColumnName].ToString().Length > 6)
    A whitespace-only string passes the guard in method 18 and may cause a parse exception
    caught by the outer catch. In method 19 it is safely filtered by .Trim().

Anomaly 9 — AdmissionAssessmentSupervisorSignatureDate date guard uses length > 0 (not > 6):
    In SaveAdmissionAssessmentSummary, the Supervisor signature date case uses:
        if (dr[c.ColumnName].ToString().Length > 0)
    rather than the standard length > 6 guard used throughout the file for DateTime fields.
    A value of length 1–6 (e.g. a truncated date string) would pass the guard and throw
    a DateTime.Parse exception. This is inconsistent with the defensive pattern used
    elsewhere.
________________________________________

37. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: SAMMS-Forms (Status=17)
        |-- insert child tasks per clinic:
        |       19 assessment table task types x 80+ clinics
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2 (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 6
        |
        |-- filter: TaskName = 'SAMMS-Forms', SiteCode != 'PHC'
        |-- for each parent task: mark ptask.Status = 18 (running)
        |
        |-- for each child task (one per clinic per table):
        |       get column mappings from dms.tbl_MapSrc2Dsn (ActionKey=6)
        |       SelectConstructor.GetSLT() → builds SELECT field list + CHECKSUM()
        |       build WHERE clause (date range filter)
        |
        V
SQLSvrManager.GetTableData()
        |
        | executes SELECT against clinic SAMMS SQL Server
        | connection from ctrl.tbl_LocationCons for this SiteCode
        |
        V
DataTable (in memory — rows from SAMMS)
        |
        |---[GROUP A — ADMISSION ASSESSMENT]
        |
        |---[TaskName = forms.tbladmissionassessment]
        |           |
        |           V
        |   SaveAdmissionAssessment() [EF CORE — Pattern A, always-update]
        |           |
        |   load ALL Azure TblAdmissionAssessment WHERE SiteCode = sc
        |   for each source row:
        |     build model via switch (isdeleted defaults false)
        |     match by Id
        |     new → stage insert
        |     found → overwrite all fields (no RowChkSum guard)
        |   db.SaveChanges() [update batch]
        |   if new: AddRange + db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure TblAdmissionAssessment
        |
        |---[TaskName = forms.tbladmissionassessmentsummary]
        |           |
        |           V
        |   SaveAdmissionAssessmentSummary() [EF CORE — Pattern A, always-update]
        |           |
        |   load ALL Azure TblAdmissionAssessmentSummary WHERE SiteCode = sc
        |   for each source row:
        |     build model via switch (⚠ staff date bug)
        |     match by Id → overwrite all fields
        |   db.SaveChanges(); AddRange if new
        |           V
        |   Azure TblAdmissionAssessmentSummary
        |
        |---[TaskName = forms.tbladmissionassessmentdimensionfour ... dimensionsix]
        |           |
        |           V
        |   SaveAdmissionAssessmentDimensionXxx() x 6 methods [Pattern A]
        |           |
        |   each: full site load → loop → match by Id → always overwrite
        |          db.SaveChanges(); AddRange if new
        |           V
        |   Azure TblAdmissionAssessmentDimension{Four,OneDisorder,Two,Three,Five,Six}
        |
        |
        |---[GROUP B — REASSESSMENT]
        |
        |---[TaskName = forms.tblreassessment]
        |           |
        |           V
        |   SaveReAssessment() [EF CORE — Pattern A, always-update]
        |           |
        |   load ALL Azure TblReAssessment WHERE SiteCode = sc
        |   for each source row:
        |     build model via switch (isdeleted defaults false; adds TimeInTreatment)
        |     match by Id → overwrite all fields
        |   db.SaveChanges(); AddRange if new
        |           V
        |   Azure TblReAssessment
        |
        |---[TaskName = forms.tblreassessmentoccupational ... tblreassessmenttreatment]
        |           |
        |           V
        |   SaveReAssessmentXxx() x 8 methods [Pattern A]
        |           |
        |   each: full site load → loop → match by Id → always overwrite
        |          db.SaveChanges(); AddRange if new
        |           V
        |   Azure TblReAssessment{Occupational,Family,Legal,MentalHealth,
        |                         PhysicalHealth,SubstanceUse,Social,Treatment}
        |
        |
        |---[GROUP C — SUBSTANCE USE HISTORY]
        |
        |---[TaskName = forms.tblassessmentsubstanceusehistory]
        |           |
        |           V
        |   SaveAssessmentSubstanceuseHistory() [EF CORE — Pattern B, RowChkSum]
        |           |
        |   load ALL Azure TblAssessmentSubstanceUseHistories WHERE SiteCode = sc
        |   for each source row:
        |     build model via switch
        |     "sitecode" case: RowChkSum = int.Parse(dr["RowChkSum"])
        |                      RowState = true
        |     "cltid" case: if CltId < 0 → RowState = false
        |     match by SiteCode + Id (composite)
        |     new → stage insert
        |     found → overwrite ALL fields incl. RowChkSum + RowState
        |              (no skip guard — always updates)
        |   db.SaveChanges() [update batch]
        |   if new: AddRange + db.SaveChanges() [insert batch]
        |           |
        |           V
        |   Azure TblAssessmentSubstanceUseHistories
        |
        |---[TaskName = forms.tbladmissionassessmentsubstanceusehistory]
        |           |
        |           V
        |   SaveAdmissionAssessmentSubstanceuseHistory() [EF CORE — Pattern B]
        |           |
        |   same as above PLUS:
        |     CreatedOn, DateOfLastUse, DateOfReported have per-field try/catch
        |     DateOfReported uses .Trim() before length check
        |     AssessmentFormId is NOT mapped in this method
        |           |
        |           V
        |   Azure TblAdmissionAssessmentSubstanceUseHistory
        |
        V
RowTrax audit saved to tsk.tbl_RowTrax
        |
        V
BHGTaskRunner marks task Status=20 (complete)
________________________________________

38. File Reference Map

File Path                                               Purpose
---------                                               -------
BCAppCode/Scheduler/Program.cs                          Creates daily task queue for all ETL pipelines
BCAppCode/BHGTaskRunner/Program.cs                      Main ETL driver (arg=6 → SAMMS-Forms pipeline)
BCAppCode/BHG-DR-LIB/SelectConstructor.cs               Builds SELECT + CHECKSUM() from dms.tbl_MapSrc2Dsn
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                   ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/SaveAssessments.cs                 EF Core upsert — all 19 assessment/reassessment methods
BCAppCode/BHG-DR-LIB/Models/TblAdmissionAssessment.cs   EF Model → Azure TblAdmissionAssessment
BCAppCode/BHG-DR-LIB/Models/TblAdmissionAssessmentSummary.cs  EF Model → Azure TblAdmissionAssessmentSummary
BCAppCode/BHG-DR-LIB/Models/TblAdmission...DimensionFour.cs etc.  EF Models → Dimension tables (1 file each)
BCAppCode/BHG-DR-LIB/Models/TblReAssessment.cs          EF Model → Azure TblReAssessment
BCAppCode/BHG-DR-LIB/Models/TblReAssessment*.cs         EF Models → ReAssessment domain tables (1 per domain)
BCAppCode/BHG-DR-LIB/Models/TblAssessmentSubstanceUseHistory.cs    EF Model → TblAssessmentSubstanceUseHistories
BCAppCode/BHG-DR-LIB/Models/TblAdmissionAssessmentSubstanceUseHistory.cs  EF Model → Admission SU History
BCAppCode/BHG-DR-LIB/BHG_DRContext.cs                   EF DbContext — all 19 assessment table registrations
________________________________________

39. Quick Reference Summary

What triggers Assessment ETL?         Scheduler.exe creates tasks, BHGTaskRunner.exe 6 processes them
TaskName in scheduler?                SAMMS-Forms
Total save methods in file?           19 (8 Admission Assessment, 9 ReAssessment, 2 Substance Use History)
EF Core or Bulk path?                 All 19 methods use EF Core only — no bulk/staging path
Schedule number?                      6 — BHGTaskRunner.exe 6
ActionKey in dms.tbl_MapSrc2Dsn?      6
Primary key (Groups A/B, 17 methods)? Id only (single column)
Primary key (Group C, 2 methods)?     SiteCode + Id (composite — matches prevent cross-clinic collision)
Azure load scope (all 19 methods)?    SiteCode == sc only — all-time full site load (no date filter)
Change detection in Groups A/B?       None — every row is always overwritten unconditionally
Change detection in Group C?          RowChkSum is stored but NO skip guard — still always overwrites
What is RowState?                     Active/inactive flag — only used in Group C methods 18 and 19
How is RowState set in Group C?       RowState = true on all rows; RowState = false if CltId < 0
Which methods use IsDeleted?          SaveAdmissionAssessment (method 1) and SaveReAssessment (method 9)
                                      Both default IsDeleted = false if source cell is empty
How is runat (LastModAt) set?         Captured once at method entry (DateTime.Now); shared by all rows
Per-row vs batch commit?              All 19 methods use batch SaveChanges() — updates first, then inserts
Batched insert optimization?          All 19 methods use AddRange() for inserts after update batch commits
wrkdt parameter used?                 Accepted by all 19 methods but used by NONE of them
Known bug?                            SaveAdmissionAssessmentSummary: StaffSignatureDate maps to
                                      PatientSignatureDate property (staff date never stored)
Date parsing guards?                  Most date fields: length > 6 before DateTime.Parse
                                      Method 19 (AdmissionAssessmentSubstanceuseHistory): adds try/catch
                                      and .Trim() for DateOfReported; also try/catch on CreatedOn, DateOfLastUse
RowTrax audit?                        Source DataTable row count vs count(SiteCode) in Azure
Error recovery?                       Scheduler resets failed tasks to Status=17 on next daily run
PHC handled here?                     No — PHC uses its own runner (PHC/Program.cs) with separate logic
Smallest method by fields?            SaveReAssessmentMentalHealth (3 clinical fields) and
                                      SaveReAssessmentSubstanceUse (2 clinical fields)
Largest method by fields?             SaveAdmissionAssessmentDimensionOneDisorder (~55 fields)
________________________________________

Documentation generated from source: BHG-DR-LIB\SaveAssessments.cs (3240 lines, 19 methods)
Generated: April 2026
Parent Schedule: SAMMS-Forms (Schedule 6 — BHGTaskRunner.exe 6)
