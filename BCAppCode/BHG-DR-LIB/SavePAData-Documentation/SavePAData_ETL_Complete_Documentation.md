
Pre-Admission (PA) Data ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 6 — Samms-Forms
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract Pre-Admission (PA)
assessment data from local SAMMS SQL Server databases at each clinic and load it into the
central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What Pre-Admission (PA) data is and why it exists in the BHG clinical workflow
- What systems and files are involved from start to finish
- How the Scheduler creates tasks and BHGTaskRunner dispatches them under Schedule 6
- How all 10 methods in SavePAData.cs each work in detail
- Why SavePADimension1 through SavePADimension6 share one documented pattern with
  per-dimension clinical field differences called out explicitly
- How RowChkSum change detection and RowState soft-delete work (and where they are absent)
- All known anomalies, bugs, and behavioral differences across the 10 methods

There are 10 methods in SavePAData.cs spanning 8 Azure destination tables and 1 reference table:

pats.tbl_PA:                        SavePA
pats.tbl_FinancialHardshipApplication: SaveFinancialHardshipApplication
pats.tbl_PACounselorReview:         SavePACounselorReview
pats.tbl_PADimension1:              SavePADimension1
pats.tbl_PADimension2:              SavePADimension2
pats.tbl_PADimension3:              SavePADimension3
pats.tbl_PADimension4:              SavePADimension4
pats.tbl_PADimension5:              SavePADimension5
pats.tbl_PADimension6:              SavePADimension6
ctrl.tbl_DropDownListItems:         SavedropDownListItems
________________________________________

2. High-Level Business Summary

What is Pre-Admission (PA) data?

Pre-Admission data represents the clinical intake and periodic reassessment process that
patients undergo at BHG clinics before and during treatment. A PA record (pats.tbl_PA) is
the header form — it records the patient, the date, the treatment pathway, and the
version of the form used. Attached to each PA header are six ASAM-dimension sub-forms
(Dimensions 1 through 6), a counselor review form, and a financial hardship application.
Together these tables capture the complete clinical picture that determines treatment level,
medication dosing pathway, and care plan for each patient at each reassessment point.

pats.tbl_PA — PA Form Header
This is the parent record for every pre-admission or periodic reassessment event. It links
the patient (ClientId), the pre-admission record (PreAdmissionId), the data form (DataFormId),
the date of assessment, the current treatment pathway and pathway phase, and the completion
location. The IsDeleted flag is stored as an integer (0=active, 1=deleted) — this is
different from the bool-based RowState used in the Dimension sub-tables. Unlike the Dimension
methods, SavePA does not use RowChkSum, RowState, or LastModAt — it is a simpler upsert
using SiteCode + Id as the composite key.

pats.tbl_FinancialHardshipApplication — Sliding Scale Fee Application
This table captures whether a patient has applied for and been approved for a reduced
treatment fee under a financial hardship program. It stores household income data across
multiple income categories (gross wages, social security, alimony, self-employment, rent),
identification and income documentation flags, patient and staff signatures with dates,
the approved pay class and approver, effective and expiration dates, application status,
and factual narrative. A known field-mapping bug means FHAPatientSignatureDate is never
populated (see Section 19).

pats.tbl_PACounselorReview — Periodic Reassessment Counselor Summary
This table captures the counselor's clinical summary at each reassessment, including the
treatment modality recommended (early intervention, outpatient, intensive outpatient,
residential, MAT subtypes — OTS, OBOT, OTP, OBAT, induction/stabilization/maintenance,
COPE phases), the date the review was completed, USE score, risk score, protective score,
patient and counselor and supervisor signatures with dates, and the clinical summary narrative.
The composite key is SiteCode + PeriodicReassessmentId — the same key used by all six
Dimension tables.

pats.tbl_PADimension1 through pats.tbl_PADimension6 — ASAM Dimension Sub-Tables
These six tables map one-to-one to the six ASAM criteria dimensions used in substance use
disorder assessment:
  Dimension 1 — Acute Intoxication and Withdrawal Potential (substance use status, UDS, overdose)
  Dimension 2 — Biomedical Conditions and Complications (physical health, HIV/Hepatitis risk)
  Dimension 3 — Emotional, Behavioral, or Cognitive Conditions (mental health symptom inventory)
  Dimension 4 — Readiness to Change (motivation, treatment satisfaction, relapse taper plans)
  Dimension 5 — Relapse, Continued Use, or Continued Problem Potential (triggers, legal, employment)
  Dimension 6 — Recovery / Living Environment (housing, family support, social connections)
Each table shares the same composite key (SiteCode + PeriodicReassessmentId) and the same
EF Core upsert pattern. All six methods store an integer ASAM severity rating
(DimensionNASAMRating) that feeds into level-of-care determination.

ctrl.tbl_DropDownListItems — Clinical Reference Lookup Values
This table mirrors the clinic-specific dropdown list values from SAMMS — the reference
values used to populate coded fields (ethnicity codes, status codes, program codes, etc.)
in other tables. SavedropDownListItems uses a selective field-level update pattern
(checking each field before writing) rather than the bulk-copy pattern used by other methods.

Why this data is important
- pats.tbl_PA links reassessment events to specific patients and treatment episodes
- The six Dimension tables power level-of-care determination and clinical dashboards
- pats.tbl_PACounselorReview provides the counselor narrative used in compliance audits
- Dimension 3's suicidal ideation fields (WishedDead, KillingYourself) feed safety alerting
- Financial hardship records feed billing and payor classification workflows
- ctrl.tbl_DropDownListItems keeps the Azure reference tables in sync with per-clinic
  SAMMS configuration so coded values decode correctly in reports
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=6                     Main ETL orchestrator for Schedule 6 (Samms-Forms)
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)              Column list + RowChkSum expression for SELECT build
dms.tbl_MapAction (Azure DB)               Maps TaskName to FromTblVw (source table/view name)
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SavePAData.cs (BHG-DR-LIB)                10 EF Core upsert methods for PA/reassessment data
Models/TblPA.cs                            EF entity → pats.tbl_PA
Models/TblFinancialHardshipApplication.cs  EF entity → pats.tbl_FinancialHardshipApplication
Models/TblPACounselorReview.cs             EF entity → pats.tbl_PACounselorReview
Models/TblPADimension1.cs … TblPADimension6.cs  EF entities → pats.tbl_PADimension1–6
Models/TblDropDownListItems.cs             EF entity → ctrl.tbl_DropDownListItems
pats.tbl_PA (Azure BHG_DR)                Final destination for PA header records
pats.tbl_FinancialHardshipApplication      Final destination for hardship applications
pats.tbl_PACounselorReview                 Final destination for counselor review forms
pats.tbl_PADimension1–6 (Azure BHG_DR)   Final destination for ASAM dimension data
ctrl.tbl_DropDownListItems (Azure BHG_DR) Final destination for clinical reference values
________________________________________

4. Scheduler — How PA Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Step 2 — Insert parent task for Samms-Forms schedule
    TaskName = 'Samms-Forms', SiteCode = 'All', Status = 17

Step 3 — Insert child tasks per clinic
For each active clinic configured for PA data, child tasks are inserted with the
relevant destination table names as the TaskName, for example:
    TaskName = 'pats.tbl_pa'                          SiteCode = 'B01A'
    TaskName = 'pats.tbl_financialhardshipapplication' SiteCode = 'B01A'
    TaskName = 'pats.tbl_pacounselorreview'            SiteCode = 'B01A'
    TaskName = 'pats.tbl_padimension1'                 SiteCode = 'B01A'
    TaskName = 'pats.tbl_padimension2'                 SiteCode = 'B01A'
    TaskName = 'pats.tbl_padimension3'                 SiteCode = 'B01A'
    TaskName = 'pats.tbl_padimension4'                 SiteCode = 'B01A'
    TaskName = 'pats.tbl_padimension5'                 SiteCode = 'B01A'
    TaskName = 'pats.tbl_padimension6'                 SiteCode = 'B01A'
    TaskName = 'ctrl.tbl_drodownlistitems'             SiteCode = 'B01A'
    (Note: "ctrl.tbl_drodownlistitems" is a typo in the task config — "drodown" instead
    of "dropdown". The case label in BHGTaskRunner matches the same typo.)

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1
________________________________________

5. BHGTaskRunner — How PA Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 6:
    BHGTaskRunner.exe 6

Step 1 — Filter queue by Samms-Forms task name
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"
        && x.Status == 17
        && x.TaskName == "Samms-Forms"
        && x.RunAt < DateTime.Now).ToList()

Step 2 — Mark parent task as running (Status=18)

Step 3 — Load and order child tasks, loop one per clinic

Step 4 — Build base SELECT
    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
    strWhere = st.WhereCondition
                 .Replace("@SiteCode", "'" + st.SiteCode + "'")
                 .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack) + "'")

Step 5 — Dispatch by TaskName

CASE: pats.tbl_pa
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SavePA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

CASE: pats.tbl_financialhardshipapplication
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveFinancialHardshipApplication(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

CASE: pats.tbl_pacounselorreview
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SavePACounselorReview(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

CASE: pats.tbl_padimension1
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SavePADimension1(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

CASE: pats.tbl_padimension2  →  sd.SavePADimension2(...)
CASE: pats.tbl_padimension3  →  sd.SavePADimension3(...)
CASE: pats.tbl_padimension4  →  sd.SavePADimension4(...)
CASE: pats.tbl_padimension5  →  sd.SavePADimension5(...)
CASE: pats.tbl_padimension6  →  sd.SavePADimension6(...)
    (All follow the same strCmd + strWhere + SortOrder → GetTableData → SavePADimensionN pattern)

CASE: ctrl.tbl_drodownlistitems
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SavedropDownListItems(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

Step 6 — Mark child task Status=20 (complete)
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the full SELECT assembled from SelectConstructor + WhereCondition, and
returns a DataTable. Connection strings are from ctrl.tbl_LocationCons. The source table
or view name (st.FromTblVw) is from dms.tbl_MapAction per task — for PA data tasks it
is typically dbo.PA, dbo.PACounselorReview, dbo.PADimension1 through dbo.PADimension6,
dbo.FinancialHardshipApplication, and dbo.DropDownListItems (or clinic-specific equivalents
as configured in dms.tbl_MapAction).

The WHERE clause applied at source is determined by st.WhereCondition from the task
metadata row, which typically filters by ModifiedOn >= @WorkDate or equivalent to limit
each daily pull to recently changed records.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

All source tables are in the SAMMS database for each clinic. The specific table or view
name per task is held in dms.tbl_MapAction.FromTblVw. Representative source table names:

    dbo.PA                          → pats.tbl_PA
    dbo.FinancialHardshipApplication → pats.tbl_FinancialHardshipApplication
    dbo.PACounselorReview           → pats.tbl_PACounselorReview
    dbo.PADimension1                → pats.tbl_PADimension1
    dbo.PADimension2                → pats.tbl_PADimension2
    dbo.PADimension3                → pats.tbl_PADimension3
    dbo.PADimension4                → pats.tbl_PADimension4
    dbo.PADimension5                → pats.tbl_PADimension5
    dbo.PADimension6                → pats.tbl_PADimension6
    dbo.DropDownListItems           → ctrl.tbl_DropDownListItems

The column list and RowChkSum expression for each table are assembled by SelectConstructor
from dms.tbl_MapSrc2Dsn filtered by the ActionKey for Samms-Forms (ActionKey=6).
________________________________________

8. Shared EF Core Upsert Pattern (SavePADimension1–6 and SaveFinancialHardshipApplication
   and SavePACounselorReview)

All methods in SavePAData.cs except SavePA and SavedropDownListItems follow this identical
structural pattern:

Step 1 — Initialise RCodes and DbContext
    rc.IsResult = true
    rc.RowsProcessed = tbl.Rows.Count
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load entire site slice from Azure into memory
    List<TblPADimensionN> dbl = db.TblPADimensionNs.Where(x => x.SiteCode == sc).ToList()

Step 3 — Initialise staging list for new records
    List<TblPADimensionN> NewItems = new List<TblPADimensionN>()

Step 4 — For each DataRow in the source DataTable:
    a) Build a new EF model instance
    b) In the "periodicreassessmentid" (or "id" for FHA) case — stamp SiteCode, RowState=true,
       LastModAt=runat simultaneously with parsing the key field
    c) Iterate all DataColumns with a switch on c.ColumnName.ToLower() to map each field
    d) Lookup existing record: dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId)
    e) If not found → rc.RowsIns++, add to NewItems list
    f) If found → rc.RowsUpd++, copy every field from the new model to the existing tracked entity

Step 5 — Two-phase commit
    db.SaveChanges()         (commits all updates)
    if (NewItems.Count > 0)
    {
        db.TblPADimensionNs.AddRange(NewItems)
        db.SaveChanges()     (commits all inserts)
    }

Step 6 — Catch any exception, set rc.IsResult=false, log e.Message and e.InnerException.Message

Key characteristics:
- No RowChkSum guard on updates — every row that exists in Azure is unconditionally
  overwritten with source values regardless of whether anything changed
- RowState is set to true on every processed row (no soft-delete logic driven by the source)
- LastModAt is set to DateTime.Now (runat) for every processed row
- The wrkdt parameter accepted by all methods is never used inside any method's logic
- Composite key for Dimension tables and CounselorReview: SiteCode + PeriodicReassessmentId
- Composite key for FinancialHardshipApplication: SiteCode + Id
________________________________________

9. SavePA — PA Form Header Load (pats.tbl_PA)

Source: dbo.PA (or clinic-specific equivalent)
Destination: pats.tbl_PA
Composite key: SiteCode + Id

SavePA differs from all other methods in this file in several important ways.

Structural differences:
- An outer guard `if (tbl.Rows.Count > 0)` wraps the entire logic including the try/catch.
  If the source DataTable is empty the method returns immediately with the default RCodes
  (IsResult=true, RowsProcessed=0) without any logging or error.
- No RowChkSum is stored on this table.
- No RowState is set (the table uses IsDeleted as an int — see below).
- No LastModAt is set.
- SiteCode is read from the source data column "sitecode" and trimmed, not injected from sc.
  However the Azure lookup also uses sc.Trim() vs pa.SiteCode.Trim() for consistency.
- Numeric fields (Id, DataFormId, PreAdmissionId, ClientId, CompletedAt) are parsed with
  double.Parse rather than int.Parse. The EF model stores these as double/double? — consistent
  with the method but unusual compared to other PA sub-tables that use int.

Column mapping:

    Source column         EF property            Type / notes
    ---------             -----------            -----
    sitecode              SiteCode               string — trimmed from column, overridden by sc.Trim()
    id                    Id                     double — primary key portion
    dataformid            DataFormId             double
    preadmissionid        PreAdmissionId         double? — guarded: length > 0
    clientid              ClientId               double? — guarded: length > 0
    date                  Date                   DateTime? — guarded: length > 6
    currentpathway        CurrentPathway         string
    currentpathwayphase   CurrentPathwayPhase    string
    completedat           CompletedAt            double? — guarded: length > 0
    completedatothers     CompletedAtOthers      string — trimmed: length > 0
    isdeleted             IsDeleted              int — bool.Parse then stored as 1 (true) or 0 (false)
    createdby             CreatedBy              string
    createdon             CreatedOn              DateTime — WEAK GUARD: length > 0 (not > 6; risk of DateTime.Parse failure on short strings)
    modifiedby            ModifiedBy             string
    modifiedon            ModifiedOn             DateTime? — guarded: length > 6
    version               Version                string — TRUNCATED to 2 chars if length > 2

Version truncation logic:
    if (pa.Version.Length > 2) { pa.Version = pa.Version.Substring(0, 2); }
This silently discards version strings longer than 2 characters.

IsDeleted mapping:
    if (bool.Parse(r[c.ColumnName].ToString()) == true) { pa.IsDeleted = 1; }
    else { pa.IsDeleted = 0; }
Note: unlike FHA which uses bool? IsDeleted and drives RowState off it, SavePA uses an
int column and does not set any RowState or active/inactive indicator beyond this IsDeleted int.

Update logic:
    tblPA.ClientId = pa.ClientId
    tblPA.CompletedAt = pa.CompletedAt
    tblPA.CompletedAtOthers = pa.CompletedAtOthers
    tblPA.CreatedBy = pa.CreatedBy
    tblPA.CreatedOn = pa.CreatedOn
    tblPA.CurrentPathway = pa.CurrentPathway
    tblPA.CurrentPathwayPhase = pa.CurrentPathwayPhase
    tblPA.DataFormId = pa.DataFormId
    tblPA.Date = pa.Date
    tblPA.IsDeleted = pa.IsDeleted
    tblPA.ModifiedBy = pa.ModifiedBy
    tblPA.ModifiedOn = pa.ModifiedOn
    tblPA.PreAdmissionId = pa.PreAdmissionId
    tblPA.Version = pa.Version
________________________________________

10. SaveFinancialHardshipApplication — Financial Hardship Application Load
    (pats.tbl_FinancialHardshipApplication)

Source: dbo.FinancialHardshipApplication (or clinic-specific equivalent)
Destination: pats.tbl_FinancialHardshipApplication
Composite key: SiteCode + Id

This method follows the shared EF Core upsert pattern (Section 8). The primary key field
is "id" (not "periodicreassessmentid" as in the Dimension tables). When the "id" case is
hit it also stamps SiteCode=sc, RowState=true, LastModAt=runat.

Column mapping (abbreviated — 44 fields mapped):

    Source column                EF property                     Type
    ---------                    -----------                     -----
    id                           Id                              int + SiteCode/RowState/LastModAt stamp
    rowchksum                    RowChkSum                       int?
    dataformid                   DataFormId                      int?
    preadmissionid               PreAdmissionId                  int?
    cltid                        CltId                           int?
    createdon                    CreatedOn                       DateTime? — guarded: length > 6
    createdby                    CreatedBy                       string
    modifiedon                   ModifiedOn                      DateTime? — guarded: length > 6
    modifiedby                   ModifiedBy                      string
    isdeleted                    IsDeleted (bool?) + RowState    bool? — if true → RowState=false
    isidentification             IsIdentification                bool?
    isincome                     IsIncome                        bool?
    txtincomeidentification      txtIncomeIdentification         string
    fhapatientsignature          FHAPatientSignature             string
    fhapatientsignaturedate      ExpirationDate (BUG)            DateTime? — see Section 19
    fhapatientsignatureby        FHAPatientSignatureBy           string
    txtannualhouseholdincome     txtAnnualHouseholdIncome        string
    emergencyname                EmergencyName                   string
    emergencyrelation            EmergencyRelation               string
    emergencyphone               EmergencyPhone                  string
    txtauigross1/2/3             txtAUIGross1/2/3                double?
    txtauisocial1/2/3            txtAUISocial1/2/3               double?
    txtauialimony1/2/3           txtAUIAlimony1/2/3              double?
    txtauiself1/2/3              txtAUISelf1/2/3                 double?
    txtauirent1/2/3              txtAUIRent1/2/3                 double?
    version                      Version                         string
    iscurrentlyuninsured         IscurrentlyUninsured            bool?
    statusofapplication          StatusofApplication             string
    facts                        Facts                           string
    payclassapproved             PayClassApproved                string
    approvedby                   ApprovedBy                      string
    effectivedate                EffectiveDate                   DateTime? — guarded: length > 6
    expirationdate               ExpirationDate                  DateTime? — guarded: length > 6

IsDeleted / RowState interaction:
    If IsDeleted is true → RowState is set to false (soft-delete)
    If IsDeleted is false or empty → RowState stays true (set during "id" case)

See Section 19 for the critical FHAPatientSignatureDate / ExpirationDate mapping bug.
________________________________________

11. SavePACounselorReview — Periodic Reassessment Counselor Review Load
    (pats.tbl_PACounselorReview)

Source: dbo.PACounselorReview (or clinic-specific equivalent)
Destination: pats.tbl_PACounselorReview
Composite key: SiteCode + PeriodicReassessmentId

This method follows the shared EF Core upsert pattern (Section 8). The composite key
is SiteCode + PeriodicReassessmentId (same as all six Dimension tables).

Column mapping (36 fields mapped):

    Source column              EF property                   Type
    ---------                  -----------                   -----
    periodicreassessmentid     PeriodicReassessmentId        int + SiteCode/RowState/LastModAt stamp
    rowchksum                  RowChkSum                     int?
    preadmissionid             PreAdmissionId                int?
    earlyintervention          EarlyIntervention             bool?
    outpatienttreatment        OutpatientTreatment           bool?
    intensiveoutpatient        IntensiveOutpatient           bool?
    partialhospitalization     PartialHospitalization        bool?
    residentialinpatient       ResidentialInpatient          bool?
    medmanagedintensiveinpatient MedManagedIntensiveInpatient bool?
    ots                        OTS                           bool?
    obot                       OBOT                          bool?
    otp                        OTP                           bool?
    obat                       OBAT                          bool?
    withdrawalmanagement       WithdrawalManagement          bool?
    copephase1                 CopePhase1                    bool?
    copephase2                 CopePhase2                    bool?
    copephase3                 CopePhase3                    bool?
    induction                  Induction                     bool?
    stabilization              Stabilization                 bool?
    maintenance                Maintenance                   bool?
    datecompleted              DateCompleted                 DateTime? — guarded: length > 6
    usescore                   UseScore                      int?
    riskscore                  RiskScore                     int?
    protectivescore            ProtectiveScore               int?
    clinicalsummary            ClinicalSummary               string
    patientsignature           PatientSignature              string
    patientsignatureby         PatientSignatureBy            string
    patientsignaturedate       PatientSignatureDate          DateTime? — guarded: length > 6
    counselorsignature         CounselorSignature            string
    counselorsignatureby       CounselorSignatureBy          string
    counselorsignaturedate     CounselorSignatureDate        DateTime? — guarded: length > 6
    supervisorsignature        SupervisorSignature           string
    supervisorsignatureby      SupervisorSignatureBy         string
    supervisorsignaturedate    SupervisorSignatureDate       DateTime? — guarded: length > 6

Note: CopePhase1, CopePhase2, CopePhase3 are correctly mapped here (source → same-named EF
property). This is in contrast to the CopePhase1/2 swap bug documented in
SaveCleints-Documentation/SaveCleints_ETL_Complete_Documentation.md for another method.
________________________________________

12. SavePADimension1 — ASAM Dimension 1: Acute Intoxication / Withdrawal (pats.tbl_PADimension1)

Source: dbo.PADimension1 (or clinic-specific equivalent)
Destination: pats.tbl_PADimension1
Composite key: SiteCode + PeriodicReassessmentId

This method follows the shared EF Core upsert pattern (Section 8).

Clinical scope: Dimension 1 assesses acute substance use and withdrawal potential. It records
the patient's last UDS date and result, illegal substance use, overdose history, Narcan
availability, craving level and rating, and the overall ASAM Dimension 1 severity rating.
The UAEval text field captures the counselor's written evaluation of urinalysis results.

Column mapping:

    Source column           EF property               Type / notes
    ---------               -----------               -----
    periodicreassessmentid  PeriodicReassessmentId    int + SiteCode/RowState/LastModAt stamp
    rowchksum               RowChkSum                 int?
    preadmissionid          PreAdmissionId            int?
    lastuds                 LastUDS                   string
    udsresult               UDSResult                 string
    illegalsubstances       PreAdmissionId (BUG)      int? — CRITICAL BUG: writes to wrong property
    illegalsubstancesbox    IllegalSubstancesBox      string
    overdose                PreAdmissionId (BUG)      int? — CRITICAL BUG: writes to wrong property
    overdosebox             OverdoseBox               string
    narcanavailable         NarcanAvailable           int?
    cravings                Cravings                  int?
    cravingrating           CravingRating             int?
    dimension1asamrating    Dimension1ASAMRating      int?
    uaeval                  UAEval                    string

See Section 19 for the critical PreAdmissionId triple-overwrite bug caused by the
"illegalsubstances" and "overdose" case labels incorrectly targeting xtm.PreAdmissionId.
________________________________________

13. SavePADimension2 — ASAM Dimension 2: Biomedical Conditions (pats.tbl_PADimension2)

Source: dbo.PADimension2
Destination: pats.tbl_PADimension2
Composite key: SiteCode + PeriodicReassessmentId

Follows the shared EF Core upsert pattern (Section 8).

Clinical scope: Dimension 2 assesses the patient's current physical health status and
medical complications related to substance use. It captures whether physical health changed
since last visit, 911 calls, worsening medical conditions, primary care provider engagement,
HIV/Hepatitis risk behaviors (unprotected sex, drug injection, sharing equipment), tobacco/
nicotine use, and the overall ASAM Dimension 2 severity rating.

Column mapping:

    Source column                    EF property                     Type
    ---------                        -----------                     -----
    periodicreassessmentid           PeriodicReassessmentId          int + stamp
    rowchksum                        RowChkSum                       int?
    preadmissionid                   PreAdmissionId                  int?
    physicalhealthchange             PhysicalHealthChange            int?
    called911                        Called911                       int?
    called911box                     Called911Box                    string
    worseningmedicalcondition        WorseningMedicalCondition       int?
    worseningmedicalconditionbox     WorseningMedicalConditionBox    string
    primarycareprovider              PrimaryCareProvider             int?
    primarycareproviderbox           PrimaryCareProviderBox          string
    unprotectedsex                   UnprotectedSex                  bool?
    druginjection                    DrugInjection                   bool?
    sharingdrug                      SharingDrug                     bool?
    hivhepatits                      HIVHepatits                     int?
    hivhepatitisbox                  HIVHepatitisBox                 string
    tobacconicotine                  TobaccoNicotine                 int?
    tobacconicotinefrequency         TobaccoNicotineFrequency        string
    discontinuetobacconicotine       DiscontinueTobaccoNicotine      int?
    dimension2asamrating             Dimension2ASAMRating            int?

Note: The source field name `hivhepatits` contains a typo (missing second 'i' — should be
"hivhepatitis"). This typo is preserved in both the switch case label and the EF property
name (HIVHepatits). The HIVHepatitisBox string (which has correct spelling) is a separate field.
________________________________________

14. SavePADimension3 — ASAM Dimension 3: Emotional / Behavioral / Cognitive Conditions
    (pats.tbl_PADimension3)

Source: dbo.PADimension3
Destination: pats.tbl_PADimension3
Composite key: SiteCode + PeriodicReassessmentId

Follows the shared EF Core upsert pattern (Section 8).

Clinical scope: Dimension 3 is the largest of the six dimension tables. It captures a
comprehensive mental health symptom inventory — whether mental health has changed, recent
hospitalization, and 35 individual boolean symptom flags covering: agitation, decreased
pleasure, anxiety, lack of interest, confusion, panic attacks, brain fog, numbness, insomnia,
trouble falling/waking, headaches, stomach issues, fatigue, restlessness, tearfulness,
increased/decreased appetite, feeling empty, irritability, anger, guilt/shame, mood swings,
decreased self-control, nightmares, decreased/increased energy, lack of focus, hallucinations,
isolation, obsessive worrying thoughts, lack of motivation, forgetfulness, nervousness,
persistent sadness, and disorganized confused thoughts. It also captures suicidal ideation via
WishedDead (int) and KillingYourself (string narrative), plus the overall ASAM Dimension 3
severity rating.

Key fields (abbreviated — 42 fields total):

    mentalhealthchange / mentalhealthhospitalized / worseningmentalhealth   int? (+ box strings)
    agitation / decreasedpleasure / anxiety / lackofinterest / confusion    bool? (35 symptom flags)
    wisheddead                                                               int?
    killingyourself                                                          string
    dimension3asamrating                                                     int?

The WishedDead and KillingYourself fields feed safety alerting workflows and are among the
most clinically sensitive fields in the entire BHG_DR data warehouse.
________________________________________

15. SavePADimension4 — ASAM Dimension 4: Readiness to Change (pats.tbl_PADimension4)

Source: dbo.PADimension4
Destination: pats.tbl_PADimension4
Composite key: SiteCode + PeriodicReassessmentId

Follows the shared EF Core upsert pattern (Section 8).

Clinical scope: Dimension 4 assesses the patient's motivation and readiness to engage with
and complete treatment. It captures the counselor's free-text assessment of the patient's
motivation for change, treatment satisfaction (with narrative), whether the patient wants
to eventually discontinue medication (and on what timeline — 3 to 6 months), and four SNAP
attributes (Strengths, Needs, Abilities, PreferredForTreatment). The overall ASAM Dimension
4 severity rating is stored as an integer.

Column mapping:

    Source column             EF property                 Type
    ---------                 -----------                 -----
    periodicreassessmentid    PeriodicReassessmentId      int + stamp
    rowchksum                 RowChkSum                   int?
    preadmissionid            PreAdmissionId              int?
    motivationforchange       MotivationforChange         string
    treatmentsatisfaction     TreatmentSatisfaction       int?
    treatmentsatisfactionbox  TreatmentSatisfactionBox    string — trimmed
    eventuallydiscontinuing   EventuallyDiscontinuing     int?
    discontinuing3to6months   Discontinuing3to6Months     int?
    strengths                 Strengths                   string — trimmed, guarded: length > 0
    needs                     Needs                       string — trimmed, guarded: length > 0
    abilities                 Abilities                   string — trimmed, guarded: length > 0
    preferedfortreatment      PreferedforTreatment        string — trimmed, guarded: length > 0
    dimension4asamrating      Dimension4ASAMRating        int?

Note: "preferedfortreatment" contains a typo in the source column name (missing 'd' — should
be "preferred"). This typo is preserved in both the switch case and EF property name.
________________________________________

16. SavePADimension5 — ASAM Dimension 5: Relapse / Continued Use Potential (pats.tbl_PADimension5)

Source: dbo.PADimension5
Destination: pats.tbl_PADimension5
Composite key: SiteCode + PeriodicReassessmentId

Follows the shared EF Core upsert pattern (Section 8).

Clinical scope: Dimension 5 assesses the patient's risk of relapse and their social / legal
/ economic circumstances that may contribute to continued substance use. It records the
patient's identified triggers and coping strategies (free text), whether the patient
intends to continue using (with narrative), employment status and part/full-time indicator,
recent arrests, changes in legal status (with narrative), financial trouble, and the overall
ASAM Dimension 5 severity rating.

Column mapping:

    Source column             EF property               Type
    ---------                 -----------               -----
    periodicreassessmentid    PeriodicReassessmentId    int + stamp
    rowchksum                 RowChkSum                 int?
    preadmissionid            PreAdmissionId            int?
    triggers                  Triggers                  string — trimmed, guarded: length > 0
    copingstrategies          CopingStrategies          string — trimmed, guarded: length > 0
    continueusing             ContinueUsing             int?
    continueusingbox          ContinueUsingBox          string — trimmed, guarded: length > 0
    employmentstatus          EmploymentStatus          int?
    employmentstatusother     EmploymentStatusOther     string — trimmed, guarded: length > 0
    partfulltime              PartFullTime              int?
    arrested                  Arrested                  int?
    changeinlegalstatus       ChangeinLegalStatus       int?
    changeinlegalstatusbox    ChangeinLegalStatusBox    string — trimmed, guarded: length > 0
    financialtrouble          FinancialTrouble          int?
    dimension5asamrating      Dimension5ASAMRating      int?
________________________________________

17. SavePADimension6 — ASAM Dimension 6: Recovery / Living Environment (pats.tbl_PADimension6)

Source: dbo.PADimension6
Destination: pats.tbl_PADimension6
Composite key: SiteCode + PeriodicReassessmentId

Follows the shared EF Core upsert pattern (Section 8).

Clinical scope: Dimension 6 assesses the patient's living situation and social support
environment. It records housing type via nine boolean flags (LivesAlone, HouseApartment,
LiveKids, Shelter, LivesPartnerSpouse, SoberLivingHome, LivesFamily, Unhoused, LivesFriends,
Other), living environment stability, exploitation risk, threats, children and custody
information (with child/family services open case flag), quality of friends/family support,
adequacy of money, whether family/friends are in recovery, current peer support connections,
and barriers to recovery. The overall ASAM Dimension 6 severity rating is stored as an integer.

Column mapping (abbreviated — 35 fields):

    Source column                    EF property                       Type
    ---------                        -----------                       -----
    periodicreassessmentid           PeriodicReassessmentId            int + stamp
    rowchksum                        RowChkSum                         int?
    preadmissionid                   PreAdmissionId                    int?
    currentlylivingother             CurrentlyLivingOther              string — trimmed
    environmentstability             EnvironmentStability              int?
    environmentstabilitybox          EnvironmentStabilityBox           string — trimmed
    safefromexploitation             SafefromExploitation              int?
    safefromexploitationbox          SafefromExploitationBox           string — trimmed
    threats / threatsbox             Threats (int?) / ThreatsBox       int? / string
    children / childrenage           Children / ChildrenAge            int?
    childrenagebox                   ChildrenAgeBox                    string — trimmed
    childrenlegalcustody             ChildrenLegalCustody              int?
    childfamilyservicesopencases     ChildFamilyServicesOpenCases      int?
    friendsfamilysupport             FriendsFamilySupport              int?
    enoughmoney                      EnoughMoney                       int?
    familyfriendsinrecovery          FamilyFriendsinRecovery           int?
    currentlyconnectedsupport        CurrentlyConnectedSupport         int?
    currentlyconnectedsupportbox     CurrentlyConnectedSupportBox      string — trimmed
    barriers / barriersbox           Barriers (int?) / BarriersBox     int? / string
    dimension6asamrating             Dimension6ASAMRating              int?
    livesalone / houseapartment /    LivesAlone / HouseApartment /     bool? (housing flags)
    livekids / shelter / ...         LiveKids / Shelter / ...
________________________________________

18. SavedropDownListItems — Clinical Reference Lookup Values (ctrl.tbl_DropDownListItems)

Source: dbo.DropDownListItems (or clinic-specific equivalent)
Destination: ctrl.tbl_DropDownListItems
Composite key: SiteCode + Id

SavedropDownListItems differs from all other methods in this file in two important ways:
(1) it uses selective field-level update logic rather than bulk property copy, and (2) it
has no RowChkSum, RowState, or LastModAt fields.

The outer guard `if (tbl.Rows.Count > 0)` wraps all logic including the try/catch (same
pattern as SavePA). If the source DataTable is empty the method returns immediately.

Column mapping (5 fields):

    Source column       EF property          Type / notes
    ---------           -----------          -----
    sitecode            SiteCode             string — read from source column
    id                  Id                   int
    dropdownlistitem    DropDownListItem      string
    dropdownlistid      DropDownListId        int? — default 0 if empty
    ddapcode            ddapcode             string — trimmed

Update logic (selective — only writes changed fields):
    if (dbx.DropDownListItem != itm.DropDownListItem) { dbx.DropDownListItem = itm.DropDownListItem; }
    if (dbx.DropDownListId != itm.DropDownListId)     { dbx.DropDownListId = itm.DropDownListId; }
    if (dbx.ddapcode.Trim() != itm.ddapcode)          { dbx.ddapcode = itm.ddapcode; }

This is the only method in the file that checks field values before writing. All other
methods copy every field unconditionally on every update pass.

Note on TaskName typo: The BHGTaskRunner case label for this method is
`ctrl.tbl_drodownlistitems` — "drodown" is missing the 'p' from "dropdown". This typo
appears consistently in the Scheduler task configuration and BHGTaskRunner dispatch case,
so the system functions correctly despite the misspelling.
________________________________________

19. Change Detection — RowChkSum Behavior

RowChkSum is present in most methods as a column that is read from source and stored in
Azure, but it does NOT guard update decisions in any method in SavePAData.cs.

In the Dimension tables, SavePACounselorReview, and SaveFinancialHardshipApplication:
    RowChkSum is stored via the "rowchksum" case label.
    It is copied to the existing record on every update pass.
    There is no comparison: if (dbxtm.RowChkSum != xtm.RowChkSum) — that conditional
    is completely absent. Every row is unconditionally overwritten on every daily run.

In SavePA and SavedropDownListItems:
    RowChkSum is not present in the source mapping for these tables at all.
    SavedropDownListItems uses selective field comparison (see Section 18) — but this
    is a different mechanism from RowChkSum and it has no RowChkSum column.

Practical consequence: The ETL re-writes every PA and Dimension record for every site
on every daily run, even if nothing has changed. This is higher I/O than necessary but
produces correct results as long as source data is accurate.
________________________________________

20. Scoping / Data Windowing

The scope of data extracted from SAMMS source for each task run is governed by the
WhereCondition stored in the task metadata row in tsk.tbl_Tasks. This is appended to
the SELECT statement before GetTableData() executes:
    strCmd += " Where " + strWhere + " " + st.SortOrder

The wrkdt (WorkDate) parameter is passed to each method by BHGTaskRunner as
st.WorkDate.Value.AddDays(DaysBack), but it is NOT used inside any of the 10 Save methods
in this file. The date filtering is entirely handled at the SQL query level in SAMMS via
the WhereCondition, not inside the C# method. This means the DataTable arriving at each
method always contains the pre-filtered result set as determined by the task metadata.

The EF Core load-to-memory step (db.TblPADimensionNs.Where(x => x.SiteCode == sc).ToList())
loads the full site slice from Azure with no date filter — all existing records for the
clinic are in memory during the upsert loop. This is the standard approach across the
BHG-DR-LIB methods and ensures correct insert-vs-update detection.
________________________________________

21. Error Handling

All 10 methods use a standard try/catch(Exception e) block:
    rc.IsResult = false
    rc.ExceptMsg = e.Message
    Console.WriteLine(e.Message)
    if (e.InnerException != null)
    {
        rc.ExceptInnerMsg = e.InnerException.Message
        Console.WriteLine(e.InnerException.Message)
    }

SavePA and SavedropDownListItems have the try/catch inside an outer
`if (tbl.Rows.Count > 0)` block — so an empty DataTable skips the try/catch entirely
and returns the default RCodes without flagging an error, which is correct behaviour.

All methods properly null-check e.InnerException before accessing it — no
NullReferenceException risk in the error handler (unlike SaveCleints.cs which has a
defect in this area).

If a DbContext is not provided (db == null), each method creates a new BHG_DRContext().
This allows the method to be called from unit tests or one-off harnesses without
an injected context.
________________________________________

22. Anomalies, Bugs, and Known Defects

ANOMALY 1 — CRITICAL BUG: SavePADimension1 — "illegalsubstances" and "overdose" cases
write to PreAdmissionId instead of their own fields.

File: SavePAData.cs, lines ~658–674
    case "illegalsubstances":
        if (r[c.ColumnName].ToString().Length > 0)
        {
            xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());  // BUG — should be xtm.IllegalSubstances
        }
        break;
    case "overdose":
        if (r[c.ColumnName].ToString().Length > 0)
        {
            xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());  // BUG — should be xtm.Overdose
        }
        break;

Impact:
- xtm.IllegalSubstances is NEVER populated from source — it is always left at default
  (null or 0) in pats.tbl_PADimension1.
- xtm.Overdose is NEVER populated from source — same result.
- xtm.PreAdmissionId is potentially overwritten up to three times per row (by the
  "preadmissionid" case, then by "illegalsubstances", then by "overdose"). Whichever
  column appears last in the DataTable determines the final value of PreAdmissionId —
  which is likely corrupted with an illegal-substances or overdose integer value.
- The update block correctly assigns dbxtm.IllegalSubstances = xtm.IllegalSubstances and
  dbxtm.Overdose = xtm.Overdose — but since both are always null/0 these assignments
  just write null/0 every run.

ANOMALY 2 — BUG: SaveFinancialHardshipApplication — "fhapatientsignaturedate" case maps
to xfha.ExpirationDate instead of xfha.FHAPatientSignatureDate.

File: SavePAData.cs, lines ~114–118
    case "fhapatientsignaturedate":
        if (r[c.ColumnName].ToString().Length > 6)
        {
            xfha.ExpirationDate = DateTime.Parse(r[c.ColumnName].ToString());  // BUG — should be xfha.FHAPatientSignatureDate
        }
        break;

Impact:
- xfha.FHAPatientSignatureDate is NEVER populated — it stays null in Azure.
- xfha.ExpirationDate is set by two separate case labels ("fhapatientsignaturedate" and
  "expirationdate"). If both columns are present in the DataTable, whichever comes last
  overwrites the other. This means either the patient signature date is stored in the
  expiration date field, or the true expiration date overwrites the patient signature date —
  depending on DataTable column order. Both outcomes produce incorrect data.
- The update block at line 278 includes `dbxfha.FHAPatientSignatureDate = xfha.FHAPatientSignatureDate`
  which perpetually writes null, and `dbxfha.ExpirationDate = xfha.ExpirationDate` which
  receives the wrong value.

ANOMALY 3 — SavePA: weak guard on "createdon" field uses length > 0 instead of length > 6.

File: SavePAData.cs, line ~2016
    case "createdon":
        if (r[c.ColumnName].ToString().Length > 0)
        {
            pa.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
        }
        break;

Impact: Any non-empty but non-parseable short string (e.g. "N/A", "0", a single character)
will cause DateTime.Parse to throw a FormatException at runtime. The standard guard used
throughout the rest of the codebase is length > 6. This inconsistency means SavePA is more
susceptible to runtime exceptions on malformed date data than the Dimension methods.

ANOMALY 4 — SavePA: Version field is silently truncated to 2 characters.

File: SavePAData.cs, lines ~2032–2035
    if (pa.Version.Length > 2) { pa.Version = pa.Version.Substring(0, 2); }

Impact: Any version string longer than 2 characters is truncated without any warning,
logging, or audit. This is intentional per design but undocumented and could silently
discard version detail if the SAMMS source starts returning longer version strings.

ANOMALY 5 — SavePA: uses double.Parse for integer key fields (Id, DataFormId, etc.)

The EF model TblPA stores Id and related fields as double, and SavePA parses them with
double.Parse. This is consistent internally but unusual relative to every other PA method
which uses int for similar ID fields. It is not a bug but is an inconsistency to be
aware of when joining pats.tbl_PA to other tables on these ID fields.

ANOMALY 6 — SavedropDownListItems: TaskName configuration typo "drodownlistitems".

The BHGTaskRunner case label is `ctrl.tbl_drodownlistitems` — "drodown" is missing the
letter 'p'. This matches the Scheduler task configuration. The system works correctly
because both the insertion (Scheduler) and the dispatch (BHGTaskRunner case) use the
same misspelled string. However any new tooling or configuration that uses the correct
spelling "dropdown" will not match and the task will not be dispatched.

ANOMALY 7 — wrkdt parameter is accepted but unused by all 10 methods.

All 10 methods accept a DateTime wrkdt parameter but none use it in their internal logic.
The date scoping is entirely handled at the SQL source level via WhereCondition. This is
consistent with similar methods elsewhere in BHG-DR-LIB (e.g. SaveCA.cs) but the unused
parameter adds interface noise and could be misleading to future maintainers.

ANOMALY 8 — Dimension tables: no RowChkSum guard — unconditional full overwrite on update.

All six Dimension methods and SavePACounselorReview store the RowChkSum value received from
source but never compare it to the existing Azure value before writing. Every row in every
site is fully overwritten on every daily run. This contrasts with SaveClientDemo1var in
SaveCleints.cs which does guard updates with a RowChkSum comparison. The result is correct
but generates more SQL UPDATE traffic than necessary.
________________________________________

23. End-to-End Flow Diagram

SCHEDULE 6 — Samms-Forms (BHGTaskRunner.exe 6)

    [Scheduler.exe]
        │
        ├── Reset stuck tasks (Status 18 → 17)
        ├── Insert parent task:  TaskName='Samms-Forms'  Status=17
        └── Insert child tasks per clinic:
                pats.tbl_pa                             SiteCode='B01A'
                pats.tbl_financialhardshipapplication   SiteCode='B01A'
                pats.tbl_pacounselorreview              SiteCode='B01A'
                pats.tbl_padimension1                   SiteCode='B01A'
                pats.tbl_padimension2                   SiteCode='B01A'
                pats.tbl_padimension3                   SiteCode='B01A'
                pats.tbl_padimension4                   SiteCode='B01A'
                pats.tbl_padimension5                   SiteCode='B01A'
                pats.tbl_padimension6                   SiteCode='B01A'
                ctrl.tbl_drodownlistitems               SiteCode='B01A'
                ... (repeated for each active clinic)
        └── Advance schedule NextRunTime += 1 day

    [BHGTaskRunner.exe 6]
        │
        ├── Query VwTaskList: TaskName='Samms-Forms', Status=17, RunAt < Now
        ├── Mark parent task Status=18 (running)
        │
        └── For each child task (one clinic at a time):
                │
                ├── Read task metadata from tsk.vw_TaskListMap:
                │       st.FromTblVw     (source table/view name from dms.tbl_MapAction)
                │       st.WhereCondition (date/site filter for source SELECT)
                │       st.ConStr        (SAMMS connection string from ctrl.tbl_LocationCons)
                │       st.SiteCode      (clinic code e.g. 'B01A')
                │       st.WorkDate      (run date)
                │
                ├── Build SELECT via SelectConstructor:
                │       strFlds = sc.GetSLT(...)      (column list + RowChkSum expression from dms.tbl_MapSrc2Dsn)
                │       strCmd = "Select " + strFlds + " from " + SrcSchema + "." + FromTblVw
                │       strCmd += " Where " + WhereCondition + " " + SortOrder
                │
                ├── Execute against SAMMS:
                │       SrcDt = sm.GetTableData(FromTblVw, strCmd, ConStr)
                │       → ADO.NET SqlConnection to clinic SQL Server
                │       → Returns DataTable with source rows
                │
                ├── Dispatch by TaskName:
                │
                │   CASE pats.tbl_pa
                │       → sd.SavePA(SrcDt, SiteCode, WorkDate, null)
                │       → EF upsert (SiteCode+Id key, no RowChkSum/RowState/LastModAt)
                │       → Azure: pats.tbl_PA
                │
                │   CASE pats.tbl_financialhardshipapplication
                │       → sd.SaveFinancialHardshipApplication(SrcDt, SiteCode, WorkDate, null)
                │       → EF upsert (SiteCode+Id key, RowState+LastModAt, IsDeleted→RowState)
                │       → Azure: pats.tbl_FinancialHardshipApplication
                │
                │   CASE pats.tbl_pacounselorreview
                │       → sd.SavePACounselorReview(SrcDt, SiteCode, WorkDate, null)
                │       → EF upsert (SiteCode+PeriodicReassessmentId key, RowState+LastModAt)
                │       → Azure: pats.tbl_PACounselorReview
                │
                │   CASE pats.tbl_padimension1
                │       → sd.SavePADimension1(SrcDt, SiteCode, WorkDate, null)
                │       → EF upsert (SiteCode+PeriodicReassessmentId key, RowState+LastModAt)
                │       → Azure: pats.tbl_PADimension1
                │       → WARNING: IllegalSubstances and Overdose never populated (Bug — Section 22)
                │
                │   CASE pats.tbl_padimension2 → sd.SavePADimension2 → pats.tbl_PADimension2
                │   CASE pats.tbl_padimension3 → sd.SavePADimension3 → pats.tbl_PADimension3
                │   CASE pats.tbl_padimension4 → sd.SavePADimension4 → pats.tbl_PADimension4
                │   CASE pats.tbl_padimension5 → sd.SavePADimension5 → pats.tbl_PADimension5
                │   CASE pats.tbl_padimension6 → sd.SavePADimension6 → pats.tbl_PADimension6
                │
                │   CASE ctrl.tbl_drodownlistitems
                │       → sd.SavedropDownListItems(SrcDt, SiteCode, WorkDate, null)
                │       → EF selective-field update (no RowChkSum/RowState/LastModAt)
                │       → Azure: ctrl.tbl_DropDownListItems
                │
                └── Mark child task Status=20 (complete)

    [Azure BHG_DR — final state after run]
        pats.tbl_PA                         — PA header records updated
        pats.tbl_FinancialHardshipApplication — hardship applications updated
        pats.tbl_PACounselorReview          — counselor review forms updated
        pats.tbl_PADimension1               — ASAM Dim 1 updated (IllegalSubstances/Overdose = NULL — BUG)
        pats.tbl_PADimension2               — ASAM Dim 2 updated
        pats.tbl_PADimension3               — ASAM Dim 3 updated (suicidal ideation fields updated)
        pats.tbl_PADimension4               — ASAM Dim 4 updated
        pats.tbl_PADimension5               — ASAM Dim 5 updated
        pats.tbl_PADimension6               — ASAM Dim 6 updated
        ctrl.tbl_DropDownListItems          — reference lookup values updated
________________________________________

24. File Reference Map

File                                            Role
----                                            ----
BCAppCode/BHG-DR-LIB/SavePAData.cs             10 EF Core upsert methods (this file)
BCAppCode/BHGTaskRunner/Program.cs              Schedule 6 dispatch — lines ~3126–3289
BCAppCode/BHG-DR-LIB/SelectConstructor.cs      Builds SELECT column list and RowChkSum expression
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs          ADO.NET wrapper — executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblPA.cs           EF model for pats.tbl_PA
BCAppCode/BHG-DR-LIB/Models/TblFinancialHardshipApplication.cs  EF model
BCAppCode/BHG-DR-LIB/Models/TblPACounselorReview.cs            EF model
BCAppCode/BHG-DR-LIB/Models/TblPADimension1.cs … TblPADimension6.cs  EF models
BCAppCode/BHG-DR-LIB/Models/TblDropDownListItems.cs            EF model
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs   DbContext — DbSet registrations for all PA tables
BCAppCode/Scheduler/Program.cs                 Task creation for Samms-Forms schedule
________________________________________

25. Quick Reference Summary

Method                              Destination                           Key                            RowChkSum  RowState  Schedule
------                              -----------                           ---                            ---------  --------  --------
SavePA                              pats.tbl_PA                           SiteCode + Id                  No         No        6
SaveFinancialHardshipApplication    pats.tbl_FinancialHardshipApplication  SiteCode + Id                  Stored     Yes       6
SavePACounselorReview               pats.tbl_PACounselorReview            SiteCode + PeriodicReassId     Stored     Yes       6
SavePADimension1                    pats.tbl_PADimension1                 SiteCode + PeriodicReassId     Stored     Yes       6
SavePADimension2                    pats.tbl_PADimension2                 SiteCode + PeriodicReassId     Stored     Yes       6
SavePADimension3                    pats.tbl_PADimension3                 SiteCode + PeriodicReassId     Stored     Yes       6
SavePADimension4                    pats.tbl_PADimension4                 SiteCode + PeriodicReassId     Stored     Yes       6
SavePADimension5                    pats.tbl_PADimension5                 SiteCode + PeriodicReassId     Stored     Yes       6
SavePADimension6                    pats.tbl_PADimension6                 SiteCode + PeriodicReassId     Stored     Yes       6
SavedropDownListItems               ctrl.tbl_DropDownListItems            SiteCode + Id                  No         No        6

RowChkSum note: All methods that store RowChkSum do NOT use it to guard updates.
Every existing row is unconditionally overwritten on each daily run.

Known Bugs (production impact):
1. SavePADimension1 — IllegalSubstances and Overdose are NEVER populated in Azure
   (both case labels incorrectly target xtm.PreAdmissionId — Section 22 Anomaly 1)
2. SaveFinancialHardshipApplication — FHAPatientSignatureDate is NEVER populated;
   ExpirationDate receives both its own value and the patient signature date depending
   on DataTable column order (Section 22 Anomaly 2)
3. SavePA — "createdon" weak guard (length > 0) risks DateTime.Parse exception on
   short malformed date strings (Section 22 Anomaly 3)

Design notes:
- SavePA and SavedropDownListItems use `if (tbl.Rows.Count > 0)` outer guard — silent
  early return on empty DataTable with no error logged
- All 10 methods accept wrkdt but none use it — date scoping is entirely at source SELECT level
- The task config typo "ctrl.tbl_drodownlistitems" is consistently used in both Scheduler
  and BHGTaskRunner — changing it to the correct spelling would break task dispatch
- SavedropDownListItems is the only method in the file that uses selective field-level
  comparison before writing (no RowChkSum, no RowState — unique pattern in this file)
