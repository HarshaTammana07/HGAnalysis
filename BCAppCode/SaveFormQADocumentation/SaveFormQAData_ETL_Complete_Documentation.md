
Forms ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract clinical form data
(Form Question Answers, Answer Signatures, E&M Forms, Comprehensive Assessment Forms) from
local SAMMS SQL Server databases at each clinic and load them into the central Azure SQL
data warehouse (BHG_DR).

The goal of this document is to explain:
- What Forms data is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How the two load paths work: SaveFormQAData (EF Core) vs Bulk path
- How the source SQL is built dynamically for each form type
- How TblForms2Process controls which forms are loaded and how
- What the source tables look like and their key columns
- What the destination tables look like and all their columns
- How RowState tracks soft-deleted / active records
- What happens when errors occur
________________________________________

2. High-Level Business Summary

What are Forms?

SAMMS stores structured clinical form data gathered during patient encounters. Forms are
organized around a flexible template system: each Form is an instance of a FormTemplate,
and each FormTemplate defines a set of Questions. Patient responses are stored as Answer rows
linking Form + Question to a typed value.

The Forms ETL pipeline actually manages five related destination tables that together capture
the full picture of clinical documentation:

1. dbo.Form / Question / Answer (SAMMS)     → pats.tbl_dbo_FormQuestionAnswers (Azure)
   One row per answered question per form instance. Captures every Q&A pair from every
   clinical form completed by a patient (assessments, treatment plans, counseling notes, etc.).

2. dbo.AnswerSignature (SAMMS)              → pats.tbl_dbo_FormAnswerSignatures (Azure)
   One row per form instance. Captures the date each type of clinical signature was applied
   (counselor, doctor, patient, provider, supervisor, medical provider, staff, etc.).

3. dbo.EandMForm / EandMFormMDM (SAMMS)     → pats.tbl_eandmformmdm (Azure)
   One row per E&M (Evaluation & Management) form with its Medical Decision Making section.
   Tracks the medical provider's clinical decision making documentation.

4. dbo.EandMForm / EandMFormPregnancy (SAMMS) → pats.tbl_eandmformpregnancy (Azure)
   One row per E&M form with pregnancy-specific section filled in. Captures prenatal and
   obstetric data for pregnant patients on MAT programs.

5. dbo.ComprehensiveAssessmentForm (SAMMS)  → pats.tbl_comprehensiveassessmentform (Azure)
   One row per comprehensive intake/assessment form. Contains 100+ fields covering patient
   demographics, social history, trauma history, family history, employment, education,
   legal history, and recovery support information.

Why it is important

The Forms dataset is the clinical intake and outcomes backbone of the BHG data warehouse.
It enables:
- Tracking completion rates for required clinical forms across all clinics
- Reporting on signature compliance (counselor, medical, patient co-signatures)
- Feeding BAM (Behavioral Analysis Metrics) calculations via pats.BAMMerge
- Compliance audits for regulatory requirements on form completion
- Clinical outcomes research using Comprehensive Assessment demographic data

Load type

Two paths exist for FormQuestionAnswers depending on site code:

EF Core path (SaveFormQuestionAnswers) — used for: all sites EXCEPT the high-volume list below
  Row-by-row upsert. Loads all Azure rows for the site into memory, soft-resets RowState
  based on date window and TblForms2Process configuration, then upserts from the SAMMS DataTable.

Bulk path (SqlBulkCopy + stg.sp_FormQA_Merge) — used for high-volume sites:
  B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, DRD-SF, V12, B35, B25, V9, FW, LO, B42
  SqlBulkCopy into stg.tbl_FormQA, then stored procedures stg.sp_FormQA_Merge and
  pats.BAMMerge to MERGE into the final table.

Note: SaveAnswerSignatures, SaveEMFormMDM, SaveEMFormPregnancy, and
SaveComprehensiveAssessmentForm always use the EF Core path regardless of site code.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and their run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=6                     Main ETL orchestrator for Samms-Forms
ctrl.tbl_Forms2Process (Azure DB)           Configuration — which forms to process per load, date filter rules
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic's SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)              Column mapping metadata (used for ComprehensiveAssessmentForm)
SelectConstructor.cs                        Assembles SELECT for ComprehensiveAssessmentForm
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SaveFormQAData.cs (BHG-DR-LIB)             EF Core upsert methods for all five form tables
BulkDartsSvc.cs / inline bulk code          SqlBulkCopy + stg.sp_FormQA_Merge for high-volume sites
pats.tbl_dbo_FormQuestionAnswers (Azure)    Final destination for form Q&A data
pats.tbl_dbo_FormAnswerSignatures (Azure)   Final destination for form signature data
pats.tbl_eandmformmdm (Azure)              Final destination for E&M MDM form data
pats.tbl_eandmformpregnancy (Azure)        Final destination for E&M Pregnancy form data
pats.tbl_comprehensiveassessmentform (Azure) Final destination for comprehensive assessment data
stg.tbl_FormQA (Azure)                     Staging table for bulk path
tsk.tbl_RowTrax (Azure)                    Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How Forms Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily and populates the task queue. It does NOT move data —
it only creates tasks.

What the Scheduler does for Forms (Samms-Forms)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For Forms, there is a row with:
    Name        = 'Samms-Forms'
    ActionKey   = 6
    ScheduleId  = 6

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'Samms-Forms'
    SiteCode = 'All'
    Status   = 17

Step 4 — Insert child task rows (one per clinic per table)
The Scheduler uses dms.vw_MapAction to identify which destination tables belong to
the Samms-Forms schedule:

    when ma.DsnSchema + '.' + ma.DsnTbl in (
        'pats.tbl_dbo_FormAnswerSignatures',
        'pats.tbl_dbo_FormQuestionAnswers'
    ) Then 'Samms-Forms'

This produces child task rows for each active clinic:
    TaskName = 'pats.tbl_dbo_FormQuestionAnswers'
    SiteCode = 'B01', 'VBRA', etc.

    TaskName = 'pats.tbl_dbo_FormAnswerSignatures'
    SiteCode = 'B01', 'VBRA', etc.

Note: SaveEMFormMDM, SaveEMFormPregnancy, and SaveComprehensiveAssessmentForm are handled
as separate child task entries with their own TaskName values dispatched in the same
BHGTaskRunner run.

Step 5 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1

Step 6 — Clean up
    delete from tsk.tbl_Tasks2
    where RunAt <= DateAdd(m, -3, GetDate()) or RowState = 26

Task queue structure after Scheduler runs:

tsk.tbl_Tasks2 will contain:
    ParentTaskId = NULL
        TaskName = 'Samms-Forms'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'pats.tbl_dbo_FormQuestionAnswers'
        SiteCode = 'B01'
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_dbo_FormAnswerSignatures'
        SiteCode = 'B01'
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How Forms Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 6 to process only the Samms-Forms schedule.

Command:   BHGTaskRunner.exe 6

Step 1 — Filter task queue for Samms-Forms
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"           // PHC uses a separate runner
        && x.Status == 17             // ready to run
        && x.TaskName == "Samms-Forms"
        && x.RunAt < DateTime.Now)

Step 2 — Mark parent task as running
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)

Step 4 — For each child task, dispatch by TaskName
BHGTaskRunner uses a switch/case on the task's TaskName to determine which path to take.
The DaysBack variable controls how far back the date window reaches (typically -10 to -15).
For Forms, formDaysBack = DaysBack - 15 (pushing the window further back to catch late entries).

Step 5 — FormQuestionAnswers path (case "pats.tbl_dbo_formquestionanswers")

  Step 5a — Check if the Form table exists at this clinic
      sm.GetTableData(..., "select name from sys.tables t where name = 'Form'", st.ConStr)
      If 0 rows returned: skip this site (Forms not deployed here)

  Step 5b — Compute formDaysBack and wrkdt
      int formDaysBack = DaysBack - 15
      DateTime wrkdt = st.WorkDate.Value.AddDays(formDaysBack).Date
      If st.Reload == true: wrkdt = 1/1/2010  (full history reload)

  Step 5c — Build the source SQL
  The main query joins Form → FormTemplate → Question → Answer → SF_PatientPreAdmission
  → SF_DataForms to get Q&A rows with IsDeleted logic:

      select SiteCode, FormName, FormId, PreAdmissionId, ClientId, QuestionId,
             QuestionOrderId, QuestionText, OptionId, AnswerValue,
             CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted
      from (
          select SiteCode = 'B01',
                 ft.FormName,
                 f.id as FormId,
                 f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy,
                 f.PreAdmissionId,
                 IsDeleted = case when isnull(f.IsDeleted,0)=0 and pa.IsDeleted<>1
                                  and isnull(pa.DataFormId,0)>=0
                                  and isnull(d.IsDeleted,0)=0 then 0 else 1 end,
                 QuestionId = isnull(q.Id, 0),
                 QuestionOrderId = q.QuestionOrderId,
                 q.QuestionText,
                 a.OptionId,
                 AnswerValue = a.Value,
                 AnswerSeq = a.Id
          from dbo.Form f
          left join FormTemplate ft on (f.FormTemplateId = ft.Id)
          left join Question q on (ft.Id = q.FormTemplateId)
          left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId)
          inner join SF_PatientPreAdmission pa on (f.PreAdmissionId = pa.ID)
          left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)
          where a.Value is not null
            and (f.CreatedOn >= wrkdt or isnull(f.UpdatedOn, f.CreatedOn) >= wrkdt)
          union
          -- same join but where q.Id is null (forms with no questions mapped)
          ...
      ) x

  Step 5d — Append extra form sources via TblForms2Process
  BHGTaskRunner loads ctrl.tbl_Forms2Process (only Enabled and RowState=1 rows):
      List<TblForms2Process> xForms = db.TblForms2Process.Where(x => x.Enabled && x.RowState).ToList()

  For each xForm that has a non-null TableName, it checks if that table exists in SAMMS:
      sm.GetTableData(..., "select name from sys.tables t where name = '" + xf.TableName + "'", ...)

  If the table exists, the query is extended with a UNION to append rows from that table.
  Special handling exists for:

  tblORDERREQ (Level Justification forms):
      Builds FormID as '9-1-<abs(cltID)>-<ReqNum>-1'
      Maps Status='Approved' orders, extracting DrSigDt → ProviderSignatureSignatureDate
                                               sigCoordinatorDt → SupervisorSignatureSignatureDate
      Date-filtered by DateAdded / statusDate if xf.DateFilterEnabled

  tblTP17REVIEW (Treatment Plan Review):
      Builds FormID as '8-1-<abs(tprCLTID)>-<tpRID>-<tprTPID>'
      FormName mapped as 'TP-' + tprType (normalized to "Treatment Plan" in save method)
      Maps tprDRSIGDate → ProviderSignatureSignatureDate
           tprCOUNSSIGDate → StaffSignatureDate
           tprSUPERSIGDate → SupervisorSignatureSignatureDate

  All other special tables (default case):
      FormID = '<Prefix>-<ClientId>-<PreAdmissionId>-<Id>'
      Joined to SF_PatientPreAdmission and SF_DataForms for IsDeleted logic
      Date-filtered by CreatedOn / UpdatedOn if DateFilterEnabled

  Step 5e — Execute the SELECT
      SrcDt = sm.GetTableData(st.FromTblVw, "select distinct * from (" + strCmd + ") z", st.ConStr)

  Step 5f — Route to Bulk or EF Core based on site code

  High-volume sites (B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, DRD-SF, V12,
                     B35, B25, V9, FW, LO, B42) → Bulk path:
      sqlm.ExeSqlCmd("Truncate Table [stg].[tbl_FormQA]", ...)
      SqlBulkCopy.WriteToServer(SrcDt) → stg.tbl_FormQA
      sqlm.ExecStrPro("stg.sp_FormQA_Merge", "@sitecode", st.SiteCode, ...)
      sqlm.ExecStrPro("pats.BAMMerge", "@sitecode", st.SiteCode, ...)

  All other sites → EF Core path:
      rCodes = sd.SaveFormQuestionAnswers(SrcDt, st.SiteCode,
                   st.WorkDate.Value.AddDays(formDaysBack), xForms, null)
      // After EF upsert, also runs BAMMerge:
      sm.ExecStrPro("pats.BAMMerge", "@sitecode", st.SiteCode, ...)

Step 6 — AnswerSignatures path (case "pats.tbl_dbo_formanswersignatures")

  Step 6a — Check if answersignature table exists at this clinic
      sm.GetTableData(..., "select name from sys.tables t where name = 'answersignature'", st.ConStr)
      If 0 rows: skip this site

  Step 6b — Compute formDaysBack
      int formDaysBack = DaysBack - 15
      DateTime wrkdt = st.WorkDate.Value.AddDays(formDaysBack).Date
      Special override: if WorkDate == 2/2/2024, wrkdt = 1/1/2010 (full reload event)

  Step 6c — Build the source SQL
  Pivots signature dates from dbo.AnswerSignature by querying the most recent non-null
  DateTime for each named DateField per FormId:

      select distinct SiteCode, FormName, FormId, ClientId,
             CreatedOn, UpdatedOn, IsDeleted,
             CompletedBySignatureSignatureDate = (select top 1 case when Sign is null
                 then '1/1/1900' else [DateTime] end from AnswerSignature
                 where FormId = x.FormId and DateField = 'CompletedBySignatureSignatureDate'
                 order by [DateTime] desc),
             CounselorSignatureSignatureDate = ...,
             DoctorSignatureSignatureDate = ...,
             MedicalProviderSignatureSignatureDate = ...,
             PatientSignatureDate = ...,
             ProviderSignatureSignatureDate = ...,
             RequestorSignatureDate = ...,
             StaffSignatureDate = ...,
             SupervisorSignatureSignatureDate = ...
      from (
          -- same Form / FormTemplate / SF_PatientPreAdmission join as FormQA but without Answer
          -- includes rows where f.IsDeleted = 1
      ) x

  The same TblForms2Process extra sources (tblORDERREQ, tblTP17REVIEW, etc.) are appended
  as UNIONs with explicit NULL or mapped signature date expressions.

  Step 6d — Execute SELECT and call SaveAnswerSignatures
      SrcDt = sm.GetTableData(...)
      rCodes = sd.SaveAnswerSignatures(SrcDt, st.SiteCode,
                   st.WorkDate.Value.AddDays(formDaysBack), xForms, null)

Step 7 — E&M Form MDM path (case "pats.tbl_eandmformmdm")

  Builds a hand-written SELECT (not SelectConstructor):
      SELECT SiteCode = 'B01', a.Id, a.PreAdmissionId, a.ClientId, a.DataFormId,
             a.CreatedBy, a.CreatedOn, a.ModifiedBy, a.ModifiedOn, a.FormDate,
             a.ServiceId, a.Context, a.[Version],
             c.MEdicalProviderSignatureDate, c.MEdicalProviderSignatureBy,
             IsDeleted = case when a.Isdeleted=1 or b.IsDeleted=1 then 1 else 0 end
      FROM dbo.EandMForm a
      left join dbo.EandMFormMDM c on (a.ID = c.EandMFormID)
      inner join SF_PatientPreAdmission b on (a.PreAdmissionID = b.ID)

  Note: The WHERE clause (filtering by date) is commented out — loads all records every run.
      rCodes = sd.SaveEMFormMDM(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

Step 8 — E&M Form Pregnancy path (case "pats.tbl_eandmformpregnancy")

  Builds a hand-written SELECT using c.* (all EandMFormPregnancy columns):
      SELECT SiteCode = 'B01', a.ClientId, a.DataFormId, a.CreatedBy, a.CreatedOn,
             a.ModifiedBy, a.ModifiedOn, a.FormDate, a.ServiceId, a.Context, a.[Version],
             c.*,
             IsDeleted = case when a.Isdeleted=1 or b.IsDeleted=1 then 1 else 0 end
      FROM dbo.EandMForm a
      inner join dbo.EandMFormPregnancy c on (a.ID = c.EandMFormID)
      inner join SF_PatientPreAdmission b on (a.PreAdmissionID = b.ID)

  Note: WHERE clause also commented out — loads all records every run.
      rCodes = sd.SaveEMFormPregnancy(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

Step 9 — Comprehensive Assessment Form path (case "pats.tbl_comprehensiveassessmentform")

  Uses the standard SelectConstructor path with DaysBack WHERE clause:
      strCmd += " Where " + strWhere + " " + st.SortOrder
      SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
      rCodes = sd.SaveComprehensiveAssessmentForm(SrcDt, st.SiteCode,
                   st.WorkDate.Value.AddDays(DaysBack), null)

Step 10 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. ctrl.tbl_Forms2Process — Form Processing Configuration

This control table (Azure BHG_DR, ctrl schema) is loaded at the start of each Forms run
and drives how the FormQuestionAnswers and AnswerSignatures pipelines behave.

Key columns:
Column          Description
------          -----------
FormName        Form name as it appears in SAMMS FormTemplate (e.g. 'Treatment Plan',
                'Intake Assessment', 'Counseling Session Note')
TableName       Special table name if this form has its own SAMMS table (e.g. 'tblORDERREQ',
                'tblTP17REVIEW'). NULL for standard Form/Answer-based forms.
Prefix          Prefix character used to build FormId for special-table forms (e.g. '9', '8')
DateFilterEnabled  If true: only Azure rows within the current date window are soft-reset
                   to RowState=0 during the pre-pass. If false: ALL rows for this form
                   are reset regardless of date.
CreatedOn       Column name in the special source table for created date (for dynamic SQL)
ModifiedOn      Column name in the special source table for modified date (for dynamic SQL)
Enabled         If false: skip this form entirely
RowState        If false: skip this form entirely

How it is used in SaveFormQuestionAnswers:

  Pre-pass (soft-delete prep):
  Before processing SAMMS rows, the method loops through all existing Azure rows for the
  site. For each Azure row, it looks up its FormName (normalizing "TP-*" → "Treatment Plan")
  in xForms (TblForms2Process):

  If the form IS in xForms AND DateFilterEnabled = true:
      Mark RowState = 0 only if (CreatedOn >= wrkdt OR UpdatedOn >= wrkdt) AND RowState was 1
      This means: only rows that were recently updated get temporarily deactivated before
      the fresh SAMMS data re-activates them (accurate incremental refresh for active forms).

  If the form IS in xForms AND DateFilterEnabled = false:
      Mark RowState = 0 unconditionally (full replacement for this form type)

  If the form is NOT in xForms:
      Mark RowState = 0 if date conditions match (default behavior for unregistered forms)

The same logic runs in SaveAnswerSignatures before the upsert loop.
________________________________________

7. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL Server,
executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'B01'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_B01;User Id=...;Password=...;'

The DataTable returned is passed directly into the appropriate Save* method or bulk loader.

For the Forms pipeline, the connection is also used for existence checks:
    sm.GetTableData(..., "select name from sys.tables t where name = 'Form'", st.ConStr)
    sm.GetTableData(..., "select name from sys.tables t where name = 'answersignature'", st.ConStr)

These guard queries ensure the code silently skips clinics that have not yet deployed
the specific SAMMS tables required for forms.
________________________________________

8. Source Tables — SAMMS SQL Server (dbo)

All source tables live in the clinic's local SAMMS SQL Server database under the dbo schema.

________________________________________
8a. dbo.Form — Form Instances

Primary Key: Id

Column Name         Type            Description
-----------         ----            -----------
Id                  int             Unique form instance ID within this clinic
FormTemplateId      int             Links to FormTemplate (defines the form type)
ClientId            int             Patient/client ID
PreAdmissionId      int             Links to SF_PatientPreAdmission (encounter context)
CreatedOn           datetime        Date/time the form was created
CreatedBy           varchar         Username who created the form
UpdatedOn           datetime        Date/time the form was last updated (nullable)
UpdatedBy           varchar         Username who last updated the form (nullable)
IsDeleted           bit             Soft-delete flag (1 = deleted in SAMMS)
________________________________________

8b. dbo.FormTemplate — Form Type Definitions

Primary Key: Id

Column Name     Description
-----------     -----------
Id              Unique template ID
FormName        Display name of the form (e.g. 'Counseling Session Note', 'Intake Assessment')
                This becomes the FormName in the Azure destination table.
________________________________________

8c. dbo.Question — Questions in a Form Template

Column Name         Description
-----------         -----------
Id                  Unique question ID
FormTemplateId      Links to FormTemplate
QuestionOrderId     Display order of the question within the form
QuestionText        Text of the question as shown to the user
________________________________________

8d. dbo.Answer — Patient Answers to Questions

Column Name     Description
-----------     -----------
Id              Unique answer ID
FormId          Links to Form (the specific form instance)
QuestionId      Links to Question
OptionId        ID of the selected option (for dropdown/checkbox questions)
Value           The answer value (text, number, or selected option text)
________________________________________

8e. dbo.AnswerSignature — Signature Records

Column Name     Description
-----------     -----------
FormId          Links to Form
DateField       Named signature slot (e.g. 'CounselorSignatureSignatureDate')
DateTime        Date/time the signature was applied
Sign            Signature data (null if unsigned)
________________________________________

8f. dbo.EandMForm — E&M Form Header

Column Name             Description
-----------             -----------
ID                      Unique E&M form ID
ClientId                Patient ID
PreAdmissionId          Links to SF_PatientPreAdmission
DataFormId              Links to SF_DataForms
CreatedOn               Creation date
CreatedBy               Created by username
ModifiedOn              Last modification date (nullable)
ModifiedBy              Last modified by username
FormDate                Clinical date of the form (may differ from CreatedOn)
ServiceId               Linked clinical service/session ID
Context                 Free-text context note
Version                 Form version string
Isdeleted               Soft-delete flag
________________________________________

8g. dbo.EandMFormMDM — E&M Medical Decision Making Section

Column Name                         Description
-----------                         -----------
EandMFormID                         Links to EandMForm.ID
MEdicalProviderSignatureDate        Date the medical provider signed the MDM section
MEdicalProviderSignatureBy          Username of the signing medical provider
________________________________________

8h. dbo.EandMFormPregnancy — E&M Pregnancy Section

Contains all columns from EandMFormMDM plus 30+ pregnancy-specific clinical fields:
EandMFormID, Ddltrimester, DoseTxt, MgTxt, DoseStabilityTxt, SignsTxt, Bleeding,
Contraction, NauseaVomiting, PregnancyOtherTxt, MedicationsTxt, PrenatalVitaminsTxt,
AllergiesTxt, ChangesInRoutineTxt, UdsradioBtn, SmokerRadioBtn, IllicitDrugTxt,
NoOfPregnanciesTxt, DeliveriesTxt, DateOfLastOb, NameofObtxt, PregnancyCommentsTxt,
Wttxt, GravidaTxt, ParaTxt, Provider, PrenatalCare, ReviewedandAcknowledged,
NapregnancyGrid

________________________________________
8i. dbo.SF_PatientPreAdmission — Encounter/Admission Context

Used for IsDeleted logic join in all form queries.
Column Name     Description
-----------     -----------
ID              Pre-admission record ID (= form.PreAdmissionId)
PatientID       Patient/client ID
DataFormId      Links to SF_DataForms (negative or null = inactive)
IsDeleted       1 = this admission/encounter is deleted
________________________________________

8j. dbo.SF_DataForms — Data Form Registry

Column Name     Description
-----------     -----------
Id              Data form ID
PatientId       Patient ID
FormName        Name of the data form
IsDeleted       1 = this data form record is deleted
________________________________________

9. SaveFormQuestionAnswers — The EF Core Path

File: BCAppCode/BHG-DR-LIB/SaveFormQAData.cs
Class: SaveData (partial class)
Method: SaveFormQuestionAnswers()

Method signature:
    public RCodes SaveFormQuestionAnswers(
        DataTable tbl,                   // rows from SAMMS for this clinic
        string sc,                       // SiteCode e.g. "B01"
        DateTime wrkdt,                  // work date window boundary
        List<TblForms2Process> f2p,      // form processing config loaded from ctrl.tbl_Forms2Process
        BHG_DRContext db)                // EF context (created if null)

EF Core upsert logic — step by step:

Step 1 — Create EF context if not passed
    if (db == null) { db = new BHG_DRContext(); }

Step 2 — Load all existing Azure rows for this site
    List<TblDboFormQuestionAnswers> fqas =
        db.TblDboFormQuestionAnswers.Where(x => x.SiteCode == sc).ToList()

Step 3 — Pre-pass: soft-reset RowState on existing Azure rows
For each existing Azure row (d):

  Normalize FormName: if d.FormName.StartsWith("TP-") → treat as "Treatment Plan"
  Look up formname in f2p (TblForms2Process):

  Case A — form IS in f2p AND DateFilterEnabled = true:
      if (d.UpdatedOn.HasValue):
          if ((d.CreatedOn.Date >= wrkdt.Date OR d.UpdatedOn.Date >= wrkdt.Date)
              AND d.RowState == 1):
              d.RowState = 0
      else:
          if (d.CreatedOn.Date >= wrkdt.Date AND d.RowState == 1):
              d.RowState = 0

  Case B — form IS in f2p AND DateFilterEnabled = false:
      d.RowState = 0  (unconditional reset for all rows of this form type)

  Case C — form NOT in f2p:
      same date-based reset logic as Case A

Note: db.SaveChanges() is NOT called here — the soft-reset is queued in EF's change
tracker and committed together with the upsert writes at the end of the method.

Step 4 — Collect new rows in a staging list
    List<TblDboFormQuestionAnswers> newfqas = new List<>()

Step 5 — Loop through every row in the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 6 — Map all columns from the DataRow to a new EF object
    switch (c.ColumnName.ToLower()):

    "sitecode"        → fqa.SiteCode = sc; fqa.LastModAt = DateTime.Now
    "formname"        → fqa.FormName = r[c]
    "formid"          → fqa.FormId = r[c].ToUpper()
    "clientid"        → fqa.ClientId = int.Parse(r[c]);
                        if ClientId < 0: RowState = 0 (soft-deleted); else RowState = 1
    "createdon"       → fqa.CreatedOn = DateTime.Parse(r[c])  (if length > 0)
    "createdby"       → fqa.CreatedBy = r[c]
    "updatedon"       → fqa.UpdatedOn = DateTime.Parse(r[c])
    "updatedby"       → fqa.UpdatedBy = r[c]
    "preadmissionid"  → fqa.PreAdmissionId = int.Parse(r[c]);  default = -1 if empty
    "isdeleted"       → if "1": fqa.IsDeleted = true, fqa.RowState = 0
                        if "0": fqa.RowState = 1
    "ischildform"     → fqa.IsChildForm = r[c] == "1"
    "questionid"      → fqa.QuestionId = int.Parse(r[c])
    "questionorderid" → fqa.QuestionOrderId = int.Parse(r[c])
    "questiontext"    → fqa.QuestionText = r[c]
    "optionid"        → fqa.OptionId = r[c]
    "answervalue"     → fqa.AnswerValue = r[c]

Step 7 — Lookup in the in-memory Azure list
    TblDboFormQuestionAnswers qax = fqas.Where(x =>
        x.SiteCode == fqa.SiteCode
        && x.FormName == fqa.FormName
        && x.FormId.ToUpper() == fqa.FormId.ToUpper()
        && x.ClientId == fqa.ClientId
        && x.PreAdmissionId == fqa.PreAdmissionId
        && x.QuestionId == fqa.QuestionId
        && x.QuestionOrderId == fqa.QuestionOrderId).FirstOrDefault()

Step 8 — Insert or Update
    if qax == null:
        newfqas.Add(fqa)     // add to insert list
        rCodes.RowsIns += 1
    else:
        qax.PreAdmissionId = fqa.PreAdmissionId
        qax.CreatedBy      = fqa.CreatedBy
        qax.CreatedOn      = fqa.CreatedOn
        qax.UpdatedBy      = fqa.UpdatedBy
        qax.UpdatedOn      = fqa.UpdatedOn
        qax.AnswerValue    = fqa.AnswerValue
        qax.OptionId       = fqa.OptionId
        qax.LastModAt      = fqa.LastModAt
        qax.RowState       = fqa.RowState
        rCodes.RowsUpd += 1

Step 9 — Commit updates and inserts in two batches
    db.SaveChanges()            // flushes updates + soft-reset changes
    if (newfqas.Count > 0):
        db.TblDboFormQuestionAnswers.AddRange(newfqas)
        db.SaveChanges()        // flushes new inserts

Important note on RowState:
- ClientId < 0 signals a soft-delete in SAMMS — RowState is set to 0 during column mapping
- IsDeleted = "1" also sets RowState = 0
- The pre-pass soft-reset means Azure rows in the current date window start as RowState=0
  and are only re-activated if the same composite key appears in today's SAMMS fetch
- A record that existed in Azure but was not present in today's SAMMS data (and was in the
  date window) remains RowState=0 after the run — this is the soft-delete mechanism
________________________________________

10. SaveAnswerSignatures — EF Core for Signature Data

File: BCAppCode/BHG-DR-LIB/SaveFormQAData.cs
Method: SaveAnswerSignatures()

Method signature:
    public RCodes SaveAnswerSignatures(
        DataTable tbl,
        string sc,
        DateTime wrkdt,
        List<TblForms2Process> f2p,
        BHG_DRContext db)

The same pre-pass soft-reset logic as SaveFormQuestionAnswers runs against existing
TblDboFormAnswerSignatures rows for this site.

Match key (lookup in memory):
    x.SiteCode == a.SiteCode
    && x.FormName == a.FormName
    && x.FormId.ToUpper() == a.FormId.ToUpper()
    && x.ClientId == a.ClientId

Column mapping:

"sitecode"                                → a.SiteCode = r[c]; a.RowState = 1; a.LastModAt = DateTime.Now
"formname"                                → a.FormName = r[c]
"formid"                                  → a.FormId = r[c].ToUpper()
"clientid"                                → a.ClientId = Math.Abs(int.Parse(r[c]));
                                            if original < 0: a.RowState = 0
"createdon"                               → a.CreatedOn (length > 6 guard)
"updatedon"                               → a.UpdatedOn (length > 6 guard)
"completedbysignaturesignaturedate"       → a.CompletedBySignatureSignatureDate
"counselorsignaturesignaturedate"         → a.CounselorSignatureSignatureDate
"doctorsignaturesignaturedate"            → a.DoctorSignatureSignatureDate
"medicalprovidersignaturesignaturedate"   → a.MedicalProviderSignatureSignatureDate
"patientsignaturedate"                    → a.PatientSignatureDate
"providersignaturesignaturedate"          → a.ProviderSignatureSignatureDate
"requestorsignaturedate"                  → a.RequestorSignatureDate
"staffsignaturedate"                      → a.StaffSignatureDate
"supervisorsignaturesignaturedate"        → a.SupervisorSignatureSignatureDate
"rowchksum"                               → a.RowChkSum = int.Parse(r[c])
"isdeleted"                               → if "1": a.RowState = 0
                                            if "0" and ClientId < 0: a.RowState = 0
                                            if "0" and ClientId >= 0: a.RowState = 1

Note on RowChkSum in signatures:
Unlike FormQuestionAnswers, the AnswerSignatures method does have a RowChkSum column.
However, in the update branch (when dbAns != null) the RowChkSum comparison is commented out:
    //if (dbAns.RowChkSum != a.RowChkSum)
All matched rows are always updated unconditionally. RowChkSum is stored but not used
as a gating condition in this method.

Commit pattern:
    db.SaveChanges()            // updates
    db.TblDboFormAnswerSignatures.AddRange(newAns)
    db.SaveChanges()            // inserts
________________________________________

11. SaveEMFormMDM — EF Core for E&M MDM

File: BCAppCode/BHG-DR-LIB/SaveFormQAData.cs
Method: SaveEMFormMDM()

Method signature:
    public RCodes SaveEMFormMDM(
        DataTable tbl,
        string sc,
        DateTime wrkdt,       // passed but not used for pre-filtering
        BHG_DRContext db)

No RowState pre-pass — this method loads and upserts without soft-resetting existing rows.

Step 1 — Load all existing Azure rows for this site
    List<TblEandMformMdm> EMs = db.TblEandMformMdm.Where(x => x.SiteCode == sc).ToList()

Step 2 — Loop source rows, map columns
    switch (c.ColumnName.ToLower()):
    "sitecode"                       → nEM.SiteCode
    "id"                             → nEM.Id
    "preadmissionid"                 → nEM.PreAdmissionId
    "clientid"                       → nEM.ClientId
    "dataformid"                     → nEM.DataFormId
    "createdon"                      → nEM.CreatedOn (length > 6 guard)
    "createdby"                      → nEM.CreatedBy
    "modifiedon"                     → nEM.ModifiedOn (length > 6 guard)
    "modifiedby"                     → nEM.ModifiedBy
    "isdeleted"                      → nEM.Isdeleted = r[c] == "1"
    "formdate"                       → nEM.FormDate (length > 6 guard)
    "serviceid"                      → nEM.ServiceId
    "context"                        → nEM.Context
    "version"                        → nEM.Version
    "medicalprovidersignaturedate"   → nEM.MedicalProviderSignatureDate (length > 6 guard)
    "medicalprovidersignatureby"     → nEM.MedicalProviderSignatureBy

Match key (lookup in memory):
    EMs.FirstOrDefault(x => x.Id == nEM.Id)

On match: update all fields. On no match: add to NewEMs insert list.
________________________________________

12. SaveEMFormPregnancy — EF Core for E&M Pregnancy

File: BCAppCode/BHG-DR-LIB/SaveFormQAData.cs
Method: SaveEMFormPregnancy()

Method signature:
    public RCodes SaveEMFormPregnancy(
        DataTable tbl,
        string sc,
        DateTime wrkdt,
        BHG_DRContext db)

Match key:
    EMs.FirstOrDefault(x => x.EandMformId == nEM.EandMformId)

Columns mapped (subset of pregnancy-specific fields beyond the EandMForm base):

"eandmformid"           → nEM.EandMformId
"ddltrimester"          → nEM.Ddltrimester (int, nullable)
"dosetxt"               → nEM.DoseTxt
"mgtxt"                 → nEM.MgTxt
"dosestabilitytxt"      → nEM.DoseStabilityTxt
"signstxt"              → nEM.SignsTxt
"bleeding"              → nEM.Bleeding (bool, nullable)
"contraction"           → nEM.Contraction (bool, nullable)
"nauseavomiting"        → nEM.NauseaVomiting (bool, nullable)
"pregnancyothertxt"     → nEM.PregnancyOtherTxt
"medicationstxt"        → nEM.MedicationsTxt
"prenatalvitiamstxt"    → nEM.PrenatalVitaminsTxt   (note: source column has typo "vitiams")
"allergiestxt"          → nEM.AllergiesTxt
"changesinroutine"      → nEM.ChangesInRoutineTxt
"udsradiobtn"           → nEM.UdsradioBtn (int)
"smokerradiobtn"        → nEM.SmokerRadioBtn (int)
"illicitdrugtxt"        → nEM.IllicitDrugTxt
"noofpregnanciestxt"    → nEM.NoOfPregnanciesTxt
"deliveriestxt"         → nEM.DeliveriesTxt
"dateoflastob"          → nEM.DateOfLastOb (length > 6 guard)
"nameofobtxt"           → nEM.NameofObtxt
"pregnancycommentstxt"  → nEM.PregnancyCommentsTxt
"wttxt"                 → nEM.Wttxt
"gravidatxt"            → nEM.GravidaTxt
"paratxt"               → nEM.ParaTxt
"provider"              → nEM.Provider
"prenatalcare"          → nEM.PrenatalCare (bool, nullable)
"reviewedandacknowledged" → nEM.ReviewedandAcknowledged (bool, nullable)
"napregnancygrid"       → nEM.NapregnancyGrid (bool, nullable)

Plus the EandMForm base fields inherited from the source SELECT (SiteCode, ClientId,
DataFormId, CreatedOn, CreatedBy, ModifiedOn, ModifiedBy, FormDate, ServiceId,
Context, Version, Isdeleted, PreAdmissionId, LastModAt set to runDate).
________________________________________

13. SaveComprehensiveAssessmentForm — EF Core for Comprehensive Assessment

File: BCAppCode/BHG-DR-LIB/SaveFormQAData.cs
Method: SaveComprehensiveAssessmentForm()

Method signature:
    public RCodes SaveComprehensiveAssessmentForm(
        DataTable tbl,
        string sc,
        DateTime wrkdt,
        BHG_DRContext db)

Match key:
    forms.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id)

Uses RowChkSum for change detection (unlike most other form methods):
    ca.RowChkSum = int.Parse(dr["rowchksum"].ToString())   // read in "sitecode" case
    ca.RowState = true   // default; set to IsDeleted value when isdeleted column is processed

Column categories:

Identity / System:
    "sitecode"      → ca.SiteCode, ca.LastModAt = runDate, ca.RowChkSum, ca.RowState = true
    "id"            → ca.Id
    "preadmissionid" → ca.PreAdmissionId
    "clientid"      → ca.ClientId
    "clientm4id"    → ca.ClientM4Id
    "clientname"    → ca.ClientName
    "dataformid"    → ca.DataFormId
    "createdon"     → ca.CreatedOn
    "createdby"     → ca.CreatedBy
    "modifiedon"    → ca.ModifiedOn
    "modifiedby"    → ca.ModifiedBy
    "version"       → ca.Version
    "isdeleted"     → ca.IsDeleted = ca.RowState = bool.Parse(r[c])

Demographics:
    "ddlgender"                 → ca.DDLGender (int)
    "ddltermsofgender"          → ca.DDLTermsofGender (int)
    "ddlsexualorientation"      → ca.DDLSexualOrientation (int)
    "ddlrelationshipstatus"     → ca.DDLRelationshipStatus (int)
    "ddlpreferredlanguage"      → ca.DDLPreferredLanguage (int)
    "hispanic"                  → ca.Hispanic (bool)
    "nonhispanic"               → ca.NonHispanic (bool)
    "raceamericalindian"        → ca.RaceAmericanIndian (bool)
    "raceasian"                 → ca.RaceAsian (bool)
    "raceblack"                 → ca.RaceBlack (bool)
    "racenativehawaiian"        → ca.RaceNativeHawaiian (bool)
    "raceother"                 → ca.RaceOther (bool)
    "raceothertxt"              → ca.RaceOtherTxt
    "racetwoormore"             → ca.RaceTwoorMore (bool)
    "racewhite"                 → ca.RaceWhite (bool)
    "islgbt"                    → ca.IsLGBT (bool)
    "ispertainingbeinglgbt"     → ca.IsPertainingBeingLGBT (bool)
    "thosewhoarenotcisgender"   → ca.ThoseWhoAreNotcisgender
    "supportivesexualorientaion" → ca.SupportiveSexualOrientaion
    "notsupportivesexualorientaion" → ca.NotSupportiveSexualOrientaion
    "ismakeyouuncomfortable"    → ca.IsMakeYouUncomfortable (bool)
    "culturalpreferencesforyourtreatment" → ca.CulturalPreferencesForYourTreatment (bool)

Employment / Education:
    "ddlemploymentstatus"       → ca.DDLEmploymentStatus (int)
    "ddlcurrentjob"             → ca.DDLCurrentJob (int)
    "howlonghadcurrentjob"      → ca.HowLongHadCurrentJob (int)
    "affectedyouremployment"    → ca.AffectedYourEmployment (bool)
    "istrainingactivities"      → ca.IsTrainingActivities (bool)
    "isemploymentsituation"     → ca.IsEmploymentSituation (bool)
    "ddlhighestgradecompleted"  → ca.DDLHighestGradeCompleted (int)
    "ddlwhatkindofschoolattend" → ca.DDLWhatKindOfSchoolAttend (int)
    "havehighschooldiploma"     → ca.HaveHighSchoolDiploma (bool)
    "ishighschooldiplomaged"    → ca.IsHighSchoolDiplomaGED (bool)
    "isheldbackschool"          → ca.IsHeldBackSchool (bool)
    "ismainstreamclasses"       → ca.IsMainstreamClasses (bool)
    "isunderstandenglish"       → ca.IsUnderstandEnglish (bool)
    "isreadwriteeffectively"    → ca.IsReadWriteEffectively (bool)
    "isfulltimestudent"         → ca.IsFullTimeStudent (bool)
    "isparttimestudent"         → ca.IsPartTimeStudent (bool)

Family History (substance use):
    "familystruggledwithdrugalcoholproblems" → ca.FamilyStruggledWithDrugAlcoholProblems (bool)
    "ddlactivesubstanceusers"   → ca.DDLActiveSubstanceUsers (int)
    "ddllivewithyou"            → ca.DDLLiveWithYou (int)
    "ddlinfluencedrugs"         → ca.DDLInfluenceDrugs (int)
    "checkfather" / "ddlcheckfather" + all maternal/paternal family member check + DDL columns
    (35+ individual family member bool/int columns)

Social History / Support:
    "ishaveanychildren"         → ca.IsHaveAnyChildren (bool)
    "iscloserelationship"       → ca.IsCloseRelationship (bool)
    "iscounttosupportyou"       → ca.IsCountToSupportYou (bool)
    "isfriendsrecovery"         → ca.IsFriendsRecovery (bool)
    "ispeersupportmeetings"     → ca.IsPeerSupportMeetings (bool)
    "checkeveryone" / "checkimmediatefamily" / "checkfriends" / various support check columns
    "findsupportyourselfinrecoveryother" → ca.FindSupportYourselfInRecoveryOther (bool)
    "substancesaffectedyourlife" → ca.SubstancesAffectedYourLife
    "socialhistoryproblemswithother" → ca.SocialHistoryProblemsWithOther (bool)

Trauma History:
    "experiencedanytraumaabuseneglect"      → ca.ExperiencedAnytraumaAbuseNeglect (bool)
    "currentlyexperiencingabusenglectexploitation" → ca.CurrentlyExperiencingAbuseNglectExploitation (bool)
    "isabuseneglectgrowingup"               → ca.IsAbuseNeglectGrowingUp (bool)
    "isfeelingtraumatized"                  → ca.IsFeelingTraumatized (bool)
    "anydifficultycopingwithtrauma"         → ca.AnyDifficultyCopingWithTrauma (int)
    "physicalabuse"                         → ca.PhysicalAbuse (bool)
    "verbalabuse"                           → ca.VerbalAbuse (bool)
    "sexualabuse"                           → ca.SexualAbuse (bool)
    "neglect"                               → ca.Neglect (bool)
    "captivity"                             → ca.Captivity (bool)
    "laborexploitation"                     → ca.LaborExploitation (bool)
    "sexualexploitation"                    → ca.SexualExploitation (bool)
    "traumaother"                           → ca.TraumaOther (bool)
    "traumarelatedtorace"                   → ca.TraumaRelatedtoRace (bool)
    "neglecttraumarelatedyourrace"          → ca.NeglectTraumaRelatedYourRace (bool)
    "physicalabuseviolencecaptivityother"   → ca.PhysicalAbuseViolenceCaptivityOther (bool)
    "sexualabuseassaultsexualexploitation"  → ca.SexualAbuseAssaultSexualExploitation (bool)
    "verbalemotionalfinancialabuse"         → ca.VerbalEmotionalFinancialAbuse (bool)

Legal / Military:
    "isincarcerated"            → ca.IsIncarcerated (bool)
    "isarrested"                → ca.IsArrested (bool)
    "isopencourtcases"          → ca.IsOpenCourtCases (bool)
    "isopenwarrants"            → ca.IsOpenWarrants (bool)
    "probationorparole"         → ca.ProbationorParole (bool)
    "isdrugtreatmentcourt"      → ca.IsDrugTreatmentCourt (bool)
    "iscourtfines"              → ca.IsCourtFines (bool)
    "iscourtorderedchildsupportpayments" → ca.IsCourtOrderedChildSupportPayments (bool)
    "ischildsupportpayments"    → ca.IsChildSupportPayments (bool)
    "iscareoffamilymembers"     → ca.IsCareOfFamilyMembers (bool)
    "isarmedforces"             → ca.IsArmedForces (bool)
    "isdeployoverseas"          → ca.IsDeployOverseas (bool)
    "isveteransadministration"  → ca.IsVeteransAdministration (bool)
    "ddlwhatbranch"             → ca.DDLWhatBranch (int)
    "ddlwhatbranchtype"         → ca.DDLWhatBranchType (int)
    "ddltypedischarge"          → ca.DDLTypeDischarge (int)

Other clinical:
    "haveyoueverreceivedservices"   → ca.HaveYouEverReceivedServices (int)
    "alwaysfollowssafersexpracices" → ca.AlwaysFollowsSaferSexPracices (bool)
    "issafersexpractices"           → ca.IsSaferSexPractices (bool)
    "obsevationofothers"            → ca.ObsevationofOthers (bool)
    "checkpersonalexperience"       → ca.CheckPersonalExperience (bool)
    "checktalkitthrough" / "checkverballyexplainittome" / "checkvisuallyshowme" / "checktactilelyhandson"
    (learning style preference check columns)

Each column parse is wrapped in a try/catch that prints the column name to Console on
failure — allowing the run to continue even if an individual field cannot be parsed.
________________________________________

14. Destination Tables — Azure BHG_DR (pats schema)

________________________________________
14a. pats.tbl_dbo_FormQuestionAnswers

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_dbo_FormQuestionAnswers
EF Model: BHG-DR-LIB/Models/TblDboFormQuestionAnswers.cs
Mapped  : [Table("tbl_dbo_FormQuestionAnswers", Schema = "pats")]

Composite Primary Key: SiteCode + FormName + FormId + ClientId + PreAdmissionId + QuestionId + QuestionOrderId

C# Property (EF)    SQL Column          Type                Notes
----------------    ---------------     ----                -----
SiteCode            SiteCode            varchar             Clinic identifier
FormName            FormName            varchar             Name of the form (e.g. 'Treatment Plan')
FormId              FormId              varchar(100)        Unique form instance ID (stored upper-case)
ClientId            ClientId            int                 Patient ID
PreAdmissionId      PreAdmissionId      int (nullable)      Pre-admission context (default -1 if empty)
QuestionId          QuestionId          int (nullable)      Question ID (0 if form has no questions)
QuestionOrderId     QuestionOrderId     int (nullable)      Display order of question
QuestionText        QuestionText        varchar (nullable)  Text of the question
OptionId            OptionId            varchar (nullable)  Selected option ID (for choice questions)
AnswerValue         AnswerValue         varchar (nullable)  The patient's answer text
CreatedOn           CreatedOn           datetime (nullable) Date the form was created
CreatedBy           CreatedBy           varchar (nullable)  Username who created the form
UpdatedOn           UpdatedOn           datetime (nullable) Date the form was last updated
UpdatedBy           UpdatedBy           varchar (nullable)  Username who last updated the form
IsDeleted           IsDeleted           bit (nullable)      true if deleted in source
IsChildForm         IsChildForm         bit (nullable)      true if this is a child (sub) form
RowState            RowState            int (nullable)      0=soft-deleted, 1=active
LastModAt           LastModAt           datetime (nullable) ETL last write timestamp
________________________________________

14b. pats.tbl_dbo_FormAnswerSignatures

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_dbo_FormAnswerSignatures
EF Model: BHG-DR-LIB/Models/TblDboFormAnswerSignatures.cs
Mapped  : [Table("tbl_dbo_FormAnswerSignatures", Schema = "pats")]

Composite Primary Key: SiteCode + FormName + FormId + ClientId

C# Property (EF)                        SQL Column                              Type
----------------                        ---------------                         ----
SiteCode                                SiteCode                                varchar
FormName                                FormName                                varchar
FormId                                  FormId                                  varchar (upper-case)
ClientId                                ClientId                                int
CreatedOn                               CreatedOn                               datetime (nullable)
UpdatedOn                               UpdatedOn                               datetime (nullable)
CompletedBySignatureSignatureDate       CompletedBySignatureSignatureDate       datetime (nullable)
CounselorSignatureSignatureDate         CounselorSignatureSignatureDate         datetime (nullable)
DoctorSignatureSignatureDate            DoctorSignatureSignatureDate            datetime (nullable)
MedicalProviderSignatureSignatureDate   MedicalProviderSignatureSignatureDate   datetime (nullable)
PatientSignatureDate                    PatientSignatureDate                    datetime (nullable)
ProviderSignatureSignatureDate          ProviderSignatureSignatureDate          datetime (nullable)
RequestorSignatureDate                  RequestorSignatureDate                  datetime (nullable)
StaffSignatureDate                      StaffSignatureDate                      datetime (nullable)
SupervisorSignatureSignatureDate        SupervisorSignatureSignatureDate        datetime (nullable)
RowChkSum                               RowChkSum                               int (nullable)
RowState                                RowState                                int (nullable)      0=soft-deleted, 1=active
LastModAt                               LastModAt                               datetime (nullable)

Note on '1/1/1900' sentinel dates:
When the source AnswerSignature table has a row for a given DateField but the Sign column
is null (unsigned), the ETL stores '1900-01-01' as the signature date rather than null.
This distinguishes "form was submitted but not yet signed" from "form was never submitted".
________________________________________

14c. pats.tbl_eandmformmdm

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_eandmformmdm
EF Model: BHG-DR-LIB/Models/TblEandMformMdm.cs

Primary Key: Id (unique per clinic)

C# Property (EF)                SQL Column                          Type
----------------                ---------------                     ----
SiteCode                        SiteCode                            varchar
Id                              Id                                  int
PreAdmissionId                  PreAdmissionId                      int
ClientId                        ClientId                            int (nullable)
DataFormId                      DataFormId                          int (nullable)
CreatedOn                       CreatedOn                           datetime (nullable)
CreatedBy                       CreatedBy                           varchar
ModifiedOn                      ModifiedOn                          datetime (nullable)
ModifiedBy                      ModifiedBy                          varchar
Isdeleted                       Isdeleted                           bool (nullable)
FormDate                        FormDate                            datetime (nullable)
ServiceId                       ServiceId                           int (nullable)
Context                         Context                             varchar
Version                         Version                             varchar
MedicalProviderSignatureDate    MedicalProviderSignatureDate        datetime (nullable)
MedicalProviderSignatureBy      MedicalProviderSignatureBy          varchar
________________________________________

14d. pats.tbl_eandmformpregnancy

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_eandmformpregnancy
EF Model: BHG-DR-LIB/Models/TblEandMformPregnancy.cs

Primary Key: EandMformId

Contains all columns from tbl_eandmformmdm base structure plus:

C# Property (EF)            SQL Column                  Type
----------------             ---------------             ----
EandMformId                  EandMformId                 int
LastModAt                    LastModAt                   datetime (nullable)
Ddltrimester                 Ddltrimester                int (nullable)
DoseTxt                      DoseTxt                     varchar
MgTxt                        MgTxt                       varchar
DoseStabilityTxt             DoseStabilityTxt            varchar
SignsTxt                     SignsTxt                    varchar
Bleeding                     Bleeding                    bool (nullable)
Contraction                  Contraction                 bool (nullable)
NauseaVomiting               NauseaVomiting              bool (nullable)
PregnancyOtherTxt            PregnancyOtherTxt           varchar
MedicationsTxt               MedicationsTxt              varchar
PrenatalVitaminsTxt          PrenatalVitaminsTxt         varchar
AllergiesTxt                 AllergiesTxt                varchar
ChangesInRoutineTxt          ChangesInRoutineTxt         varchar
UdsradioBtn                  UdsradioBtn                 int (nullable)
SmokerRadioBtn               SmokerRadioBtn              int (nullable)
IllicitDrugTxt               IllicitDrugTxt              varchar
NoOfPregnanciesTxt           NoOfPregnanciesTxt          varchar
DeliveriesTxt                DeliveriesTxt               varchar
DateOfLastOb                 DateOfLastOb                datetime (nullable)
NameofObtxt                  NameofObtxt                 varchar
PregnancyCommentsTxt         PregnancyCommentsTxt        varchar
Wttxt                        Wttxt                       varchar
GravidaTxt                   GravidaTxt                  varchar
ParaTxt                      ParaTxt                     varchar
Provider                     Provider                    varchar
PrenatalCare                 PrenatalCare                bool (nullable)
ReviewedandAcknowledged      ReviewedandAcknowledged     bool (nullable)
NapregnancyGrid              NapregnancyGrid             bool (nullable)
________________________________________

14e. pats.tbl_comprehensiveassessmentform

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_comprehensiveassessmentform
EF Model: BHG-DR-LIB/Models/TblComprehensiveAssessmentForm.cs

Composite Primary Key: SiteCode + Id

Uses RowChkSum for change detection.
Contains 100+ columns across all the domain categories described in Section 13.

Key system columns:
C# Property     SQL Column      Type                Notes
-----------     ----------      ----                -----
SiteCode        SiteCode        varchar             Clinic identifier
Id              Id              int                 Record ID from source
RowChkSum       RowChkSum       int (nullable)      Change detection hash (SQL CHECKSUM)
RowState        RowState        bool (nullable)     true=active, false=soft-deleted
LastModAt       LastModAt       datetime (nullable) ETL last write timestamp
IsDeleted       IsDeleted       bool (nullable)     Mirrors IsDeleted from source (= RowState)
________________________________________

15. RowState — Soft Delete Tracking

RowState is the active/inactive flag used by SaveFormQuestionAnswers and SaveAnswerSignatures.

Value   Meaning
-----   -------
1       Row is active — exists in current SAMMS data
0       Row has been soft-deleted — existed in Azure but is no longer in SAMMS or was deleted
        (could be because: IsDeleted=1 in source, ClientId<0, or not seen in current date window)

For ComprehensiveAssessmentForm, RowState is a bool (true/false) and directly mirrors
the IsDeleted field: RowState = IsDeleted = bool.Parse(r[c]).

RowState in FormQuestionAnswers — how it flows:
1. Pre-pass: existing Azure rows in the current date window are set to RowState=0
2. Column mapping: if ClientId<0 or IsDeleted=1, the incoming row is also RowState=0
3. Upsert: when an existing row is found, qax.RowState = fqa.RowState is applied
4. New rows: RowState is set during column mapping based on ClientId and IsDeleted
5. Rows that were RowState=0 after the pre-pass and never matched by a SAMMS row
   remain RowState=0 — this is the implicit soft-delete for removed forms
________________________________________

16. Bulk Path — High-Volume Sites

For high-volume sites (B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, DRD-SF, V12,
B35, B25, V9, FW, LO, B42), FormQuestionAnswers uses SqlBulkCopy instead of EF Core.

This path is triggered in BHGTaskRunner (not in SaveFormQAData.cs directly).

Step 1 — TRUNCATE the staging table
    sqlm.ExeSqlCmd("Truncate Table [stg].[tbl_FormQA]", sqlm.ConnectionString)

Step 2 — Map columns for bulk copy
    foreach (DataColumn c in SrcDt.Columns)
        bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName))
    bc.DestinationTableName = "[stg].[tbl_FormQA]"
    bc.BulkCopyTimeout = 99999

Step 3 — SqlBulkCopy.WriteToServer(SrcDt)
    All rows inserted into stg.tbl_FormQA in a single operation.

Step 4 — Execute merge stored procedures
    stg.sp_FormQA_Merge @sitecode = 'NC'
    → MERGE pats.tbl_dbo_FormQuestionAnswers AS tgt
      USING stg.tbl_FormQA AS src ON (composite key match)
      WHEN MATCHED ... THEN UPDATE SET ...
      WHEN NOT MATCHED BY TARGET THEN INSERT ...

    pats.BAMMerge @sitecode = 'NC'
    → Post-merge procedure that recalculates BAM (Behavioral Analysis Metrics)
      aggregate values for this site from the newly loaded form Q&A data.

Note: The BAMMerge stored procedure is also called after the EF Core path for non-bulk sites.
This ensures BAM metrics are kept current regardless of which load path was used.
________________________________________

17. Load Design Summary

Load type: Incremental upsert with date-window RowState soft-delete

Per run behavior for pats.tbl_dbo_FormQuestionAnswers (EF Core sites):

  1. Load ALL Azure rows for this site into memory
  2. Pre-pass: soft-reset RowState=0 for rows in the current date window
     (controlled by TblForms2Process.DateFilterEnabled per form type)
  3. For each SAMMS row:
     - Match by composite key → update fields + set RowState=1
     - No match → insert new row with RowState set from ClientId/IsDeleted
  4. db.SaveChanges() — batch commit of all updates + soft-resets
  5. db.TblDboFormQuestionAnswers.AddRange(newRows) + db.SaveChanges() — batch insert
  6. pats.BAMMerge — update BAM aggregates for this site

Per run behavior for pats.tbl_dbo_FormQuestionAnswers (Bulk sites):

  1. TRUNCATE stg.tbl_FormQA
  2. SqlBulkCopy → all SAMMS rows into stg.tbl_FormQA
  3. stg.sp_FormQA_Merge → MERGE into pats.tbl_dbo_FormQuestionAnswers
  4. pats.BAMMerge → update BAM aggregates

Per run behavior for pats.tbl_dbo_FormAnswerSignatures (all sites — EF Core only):
  Same pre-pass soft-reset + upsert pattern as FormQuestionAnswers.
  Match key: SiteCode + FormName + FormId + ClientId.
  Always updates all signature date fields on match (RowChkSum comparison commented out).

Per run behavior for pats.tbl_eandmformmdm and pats.tbl_eandmformpregnancy:
  No pre-pass. Load all Azure rows for site. Upsert without RowState soft-reset.
  Note: No WHERE clause in source SQL — loads ALL records every run.

Per run behavior for pats.tbl_comprehensiveassessmentform:
  Uses SelectConstructor with DaysBack WHERE clause.
  RowChkSum comparison not used for gating — all matched rows are updated.
  RowState = IsDeleted from source.
________________________________________

18. Error Handling and Recovery

SaveFormQuestionAnswers / SaveAnswerSignatures error handling:

    try
    {
        // ... pre-pass + upsert loop + SaveChanges() ...
    }
    catch (Exception e)
    {
        rCodes.IsResult = false
        rCodes.ExceptMsg = e.Message
        rCodes.ExceptInnerMsg = e.InnerException.Message  (if InnerException != null)
    }

If an EF Core exception occurs:
- The entire batch for that site is rolled back (db.SaveChanges() not committed)
- RCodes.IsResult = false
- The caller (BHGTaskRunner) records the error and sets the task status accordingly

SaveComprehensiveAssessmentForm adds per-row error resilience:
Each column's switch case is wrapped in its own try/catch:
    try { switch (c.ColumnName.ToLower()) { ... } }
    catch (Exception e) { Console.WriteLine(c.ColumnName.ToString()); }

This means a parse failure on a single field of a single row does not abort the entire
site's load — only that column for that row is skipped and logged to console.

Bulk path error handling:

    try
    {
        bc.WriteToServer(SrcDt)
        sqlm.ExecStrPro("stg.sp_FormQA_Merge", ...)
        sqlm.ExecStrPro("pats.BAMMerge", ...)
    }
    catch (Exception e)
    {
        rCodes.IsResult = false
        rCodes.ExceptMsg = e.Message
    }

If bulk copy fails: staging table may be left with partial data — manual truncate may
be needed before the next run.

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

A failed Forms run for a clinic will automatically be retried the next day.
________________________________________

19. RowTrax — Audit and Row Count Tracking

For certain Forms task types, BHGTaskRunner writes a RowTrax entry after a successful run:

    sd.SaveRowTrax(
        st.SiteCode,
        st.WorkDate.Value.Date,
        st.TaskName,
        sourceCount,     // SrcDt.Rows.Count (rows fetched from SAMMS)
        destCount,       // count(1) from pats.tbl_... where SiteCode = '...'
        null)

Source count: number of rows returned from the SAMMS dynamic SELECT
Destination count: total rows in Azure for this site

These counts are stored in tsk.tbl_RowTrax and are used for:
- Monitoring whether the ETL is writing expected volumes
- Detecting clinics where form data is unexpectedly zero
- Historical trend analysis for data completeness auditing

Note: RowTrax is not enabled for all Forms sub-tasks — the st.RowTrax column controls
this per task mapping row.
________________________________________

20. Key Design Notes and Gotchas

FormId uniqueness:
FormId is stored as a varchar and is always uppercased before use in lookups:
    fqa.FormId = r[c.ColumnName].ToString().ToUpper()
    x.FormId.ToUpper() == fqa.FormId.ToUpper()
This ensures case-insensitive matching since SAMMS and SAMMS upgrade paths may produce
different-cased GUIDs or composite keys.

Treatment Plan name normalization:
Any FormName starting with "TP-" (e.g. "TP-Initial", "TP-Quarterly") is normalized to
"Treatment Plan" before looking up in TblForms2Process:
    if (d.FormName.StartsWith("TP-")) formname = "Treatment Plan"
This ensures all TP variants share the same form processing configuration.

"prenatalvitiamstxt" typo:
The source SAMMS column name has a known typo ("vitiams" instead of "vitamins").
The ETL maps this as-is: case "prenatalvitiamstxt" → nEM.PrenatalVitaminsTxt
The destination EF property uses the correct spelling. Do not change the case label.

RowState integer vs boolean:
- FormQuestionAnswers and AnswerSignatures: RowState is int (0 or 1)
- ComprehensiveAssessmentForm: RowState is bool (true/false), and it directly mirrors IsDeleted
  (a form marked IsDeleted=true has RowState=false/deleted)

BAMMerge always runs:
Both the EF Core path and the Bulk path always call pats.BAMMerge after saving form Q&A data.
BAMMerge recalculates behavioral analysis aggregate metrics for the site from the full
content of pats.tbl_dbo_FormQuestionAnswers. It is NOT called for the other form tables
(AnswerSignatures, EMForm, ComprehensiveAssessment).

Reload override:
If a task row has st.Reload = true, the work date for FormQuestionAnswers is forced to
1/1/2010, bypassing the rolling date window and loading the clinic's complete form history.
This is used for initial loads of newly onboarded clinics or full-history corrections.

EandMForm WHERE clause is inactive:
The WHERE clause that would filter EandMForm records by date was commented out in a code
update. Both SaveEMFormMDM and SaveEMFormPregnancy load ALL records for the site every run.
This is intentional for forms that are infrequent but critical (medical provider sign-offs).
________________________________________

21. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: Samms-Forms (Status=17)
        |-- insert child tasks per clinic:
        |       pats.tbl_dbo_FormQuestionAnswers  x 80+ clinics
        |       pats.tbl_dbo_FormAnswerSignatures x 80+ clinics
        |       pats.tbl_eandmformmdm             x applicable clinics
        |       pats.tbl_eandmformpregnancy        x applicable clinics
        |       pats.tbl_comprehensiveassessmentform x applicable clinics
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks2 (Azure BHG_DR)
        |
        | (when RunAt time arrives)
        V
BHGTaskRunner.exe 6
        |
        |-- filter: TaskName = 'Samms-Forms', SiteCode != 'PHC', Status=17
        |-- mark parent task Status=18 (running)
        |-- load ctrl.tbl_Forms2Process → xForms (form config list)
        |
        |-- for each child task (one per clinic per table type):
        |
        |========================================================
        |  BRANCH A: TaskName = pats.tbl_dbo_formquestionanswers
        |========================================================
        |
        |   Check if dbo.Form table exists at this clinic
        |   (select name from sys.tables where name = 'Form')
        |          |
        |       [not found] → skip clinic, rCodes.ExceptMsg = "No Form table."
        |          |
        |       [found]
        |          |
        |          |-- compute formDaysBack = DaysBack - 15
        |          |-- build strCmd:
        |          |     SELECT from dbo.Form
        |          |       left join FormTemplate
        |          |       left join Question
        |          |       left join Answer
        |          |       inner join SF_PatientPreAdmission
        |          |       left join SF_DataForms
        |          |     WHERE CreatedOn >= wrkdt OR UpdatedOn >= wrkdt
        |          |
        |          |-- for each xForm in TblForms2Process with TableName != null:
        |          |     check if special table exists (tblORDERREQ, tblTP17REVIEW, etc.)
        |          |     if exists: UNION extra rows into strCmd
        |          |       tblORDERREQ  → Level Justification forms
        |          |       tblTP17REVIEW → Treatment Plan Review forms
        |          |       other tables → generic prefix-based FormID construction
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |          | executes SELECT against clinic SAMMS SQL Server
        |          | connection string from ctrl.tbl_LocationCons
        |          V
        |   DataTable (rows from SAMMS — all form Q&A for this site in window)
        |          |
        |          |--[SiteCode in high-volume list?]
        |          |   B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24,
        |          |   DRD-SF, V12, B35, B25, V9, FW, LO, B42
        |          |
        |          |       YES (Bulk path)           NO (EF Core path)
        |          |           |                          |
        |          |           V                          V
        |          |   TRUNCATE stg.tbl_FormQA    SaveFormQuestionAnswers()
        |          |   SqlBulkCopy → stg.tbl_FormQA      |
        |          |   stg.sp_FormQA_Merge          Step 1: load ALL Azure rows
        |          |     MERGE into                       for SiteCode into memory
        |          |     pats.tbl_dbo_             Step 2: pre-pass RowState soft-reset
        |          |     FormQuestionAnswers              per TblForms2Process rules:
        |          |       match → UPDATE                  DateFilterEnabled=true →
        |          |       new   → INSERT                   reset rows in date window
        |          |           |                          DateFilterEnabled=false →
        |          |           |                           reset ALL rows for form
        |          |           |                  Step 3: loop SAMMS rows:
        |          |           |                           match composite key
        |          |           |                           found  → update fields,
        |          |           |                                     RowState=1
        |          |           |                           not found → add to insert list
        |          |           |                  Step 4: db.SaveChanges() (updates)
        |          |           |                  Step 5: AddRange(new) + SaveChanges()
        |          |           |                          |
        |          |           V                          V
        |          |   pats.tbl_dbo_FormQuestionAnswers (Azure BHG_DR)
        |          |           |                          |
        |          |           +----------+---------------+
        |          |                      |
        |          |                      V
        |          |           pats.BAMMerge @sitecode
        |          |           (recalculate BAM metrics for this site)
        |
        |========================================================
        |  BRANCH B: TaskName = pats.tbl_dbo_formanswersignatures
        |========================================================
        |
        |   Check if dbo.answersignature table exists at this clinic
        |          |
        |       [not found] → skip clinic
        |          |
        |       [found]
        |          |
        |          |-- compute formDaysBack = DaysBack - 15
        |          |   (special override: if WorkDate=2/2/2024 → wrkdt=1/1/2010)
        |          |-- build strCmd:
        |          |     SELECT from dbo.Form + FormTemplate + SF_PatientPreAdmission
        |          |     pivot 9 signature dates via correlated subqueries on AnswerSignature:
        |          |       CompletedBySignatureSignatureDate
        |          |       CounselorSignatureSignatureDate
        |          |       DoctorSignatureSignatureDate
        |          |       MedicalProviderSignatureSignatureDate
        |          |       PatientSignatureDate
        |          |       ProviderSignatureSignatureDate
        |          |       RequestorSignatureDate
        |          |       StaffSignatureDate
        |          |       SupervisorSignatureSignatureDate
        |          |     UNION extra sources from TblForms2Process
        |          |       (tblORDERREQ, tblTP17REVIEW — with mapped sig dates)
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |          V
        |   DataTable (one row per form instance per site)
        |          |
        |          V
        |   SaveAnswerSignatures()
        |          |
        |   Step 1: load ALL Azure signature rows for SiteCode
        |   Step 2: pre-pass RowState soft-reset (same TblForms2Process logic)
        |   Step 3: loop SAMMS rows, map 9 signature date columns + RowChkSum
        |   Step 4: lookup by SiteCode + FormName + FormId + ClientId
        |             found     → update all sig dates + RowState (always, no checksum gate)
        |             not found → add to insert list
        |   Step 5: db.SaveChanges() + AddRange(new) + db.SaveChanges()
        |          |
        |          V
        |   pats.tbl_dbo_FormAnswerSignatures (Azure BHG_DR)
        |
        |========================================================
        |  BRANCH C: TaskName = pats.tbl_eandmformmdm
        |========================================================
        |
        |   Build hand-written SELECT:
        |     FROM dbo.EandMForm a
        |     left join dbo.EandMFormMDM c on (a.ID = c.EandMFormID)
        |     inner join SF_PatientPreAdmission b on (a.PreAdmissionID = b.ID)
        |     [WHERE clause is currently commented out — loads all records]
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |          V
        |   DataTable
        |          V
        |   SaveEMFormMDM()
        |     load Azure rows for site into memory
        |     loop source rows → map 16 columns
        |     lookup by Id
        |       found     → update all fields
        |       not found → add to insert list
        |     db.SaveChanges() + AddRange(new) + db.SaveChanges()
        |          |
        |          V
        |   pats.tbl_eandmformmdm (Azure BHG_DR)
        |
        |========================================================
        |  BRANCH D: TaskName = pats.tbl_eandmformpregnancy
        |========================================================
        |
        |   Build hand-written SELECT (c.* — all pregnancy columns):
        |     FROM dbo.EandMForm a
        |     inner join dbo.EandMFormPregnancy c on (a.ID = c.EandMFormID)
        |     inner join SF_PatientPreAdmission b on (a.PreAdmissionID = b.ID)
        |     [WHERE clause commented out — loads all records]
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |          V
        |   DataTable
        |          V
        |   SaveEMFormPregnancy()
        |     load Azure rows for site into memory
        |     loop source rows → map 35+ pregnancy-specific columns
        |     lookup by EandMformId
        |       found     → update all fields
        |       not found → add to insert list
        |     db.SaveChanges() + AddRange(new) + db.SaveChanges()
        |          |
        |          V
        |   pats.tbl_eandmformpregnancy (Azure BHG_DR)
        |
        |========================================================
        |  BRANCH E: TaskName = pats.tbl_comprehensiveassessmentform
        |========================================================
        |
        |   SelectConstructor builds SELECT from dms.tbl_MapSrc2Dsn metadata
        |   strCmd += " Where " + strWhere + " " + st.SortOrder
        |          |
        |          V
        |   SQLSvrManager.GetTableData()
        |          V
        |   DataTable
        |          V
        |   SaveComprehensiveAssessmentForm()
        |     load Azure rows for site into memory
        |     loop source rows → map 100+ columns (wrapped per-column try/catch)
        |     lookup by SiteCode + Id
        |       found     → update all fields (RowChkSum stored but not used as gate)
        |       not found → add to insert list
        |     db.SaveChanges() + AddRange(new) + db.SaveChanges()
        |          |
        |          V
        |   pats.tbl_comprehensiveassessmentform (Azure BHG_DR)
        |
        V
RowTrax audit (if st.RowTrax = true)
        |   saved to tsk.tbl_RowTrax
        |   source count  = SrcDt.Rows.Count
        |   dest count    = count(1) from destination table where SiteCode = sc
        V
BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)
________________________________________

22. File Reference Map

File Path                                           Purpose
---------                                           -------
BCAppCode/Scheduler/Program.cs                      Creates daily task queue — inserts Samms-Forms tasks
BCAppCode/BHGTaskRunner/Program.cs                  Main ETL driver (arg=6 → Forms pipeline)
                                                    Contains dynamic SQL builders for all 5 form branches
                                                    Contains bulk path inline code for high-volume sites
BCAppCode/BHG-DR-LIB/SaveFormQAData.cs             EF Core upsert — all 5 Save* methods
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs              ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/SelectConstructor.cs          Builds SELECT for ComprehensiveAssessmentForm path
BCAppCode/BHG-DR-LIB/Models/TblDboFormQuestionAnswers.cs    EF Model → pats.tbl_dbo_FormQuestionAnswers
BCAppCode/BHG-DR-LIB/Models/TblDboFormAnswerSignatures.cs   EF Model → pats.tbl_dbo_FormAnswerSignatures
BCAppCode/BHG-DR-LIB/Models/TblEandMformMdm.cs     EF Model → pats.tbl_eandmformmdm
BCAppCode/BHG-DR-LIB/Models/TblEandMformPregnancy.cs EF Model → pats.tbl_eandmformpregnancy
BCAppCode/BHG-DR-LIB/Models/TblComprehensiveAssessmentForm.cs EF Model → pats.tbl_comprehensiveassessmentform
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs       EF DbContext — registers all form tables + ctrl.tbl_Forms2Process
BCAppCode/PHC/SaveFormQAData.cs                     PHC variant of SaveFormQuestionAnswers and SaveAnswerSignatures
                                                    (slightly different signature — uses bool yearly instead of xForms list)
BCAppCode/PHC/Program.cs                            PHC ETL driver — calls PHC variant of form save methods
________________________________________

23. Quick Reference Summary

What triggers Forms ETL?            Scheduler.exe creates tasks, BHGTaskRunner.exe 6 processes them
TaskName in scheduler?              Samms-Forms
Source tables in SAMMS?             dbo.Form, dbo.FormTemplate, dbo.Question, dbo.Answer,
                                    dbo.AnswerSignature, dbo.EandMForm, dbo.EandMFormMDM,
                                    dbo.EandMFormPregnancy, dbo.ComprehensiveAssessmentForm
                                    + special tables via TblForms2Process (tblORDERREQ, tblTP17REVIEW, etc.)
Destination tables in Azure?        pats.tbl_dbo_FormQuestionAnswers
                                    pats.tbl_dbo_FormAnswerSignatures
                                    pats.tbl_eandmformmdm
                                    pats.tbl_eandmformpregnancy
                                    pats.tbl_comprehensiveassessmentform
EF Core path applies to?            All sites for FormQA EXCEPT high-volume list below
                                    All sites for AnswerSignatures, EMForm, ComprehensiveAssessment
Bulk path applies to?               FormQA only — B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24,
                                    DRD-SF, V12, B35, B25, V9, FW, LO, B42
Staging table?                      stg.tbl_FormQA (bulk path only)
Merge stored procedures?            stg.sp_FormQA_Merge + pats.BAMMerge (both paths call BAMMerge)
Primary key — FormQuestionAnswers?  SiteCode + FormName + FormId + ClientId + PreAdmissionId +
                                    QuestionId + QuestionOrderId (7-column composite)
Primary key — AnswerSignatures?     SiteCode + FormName + FormId + ClientId (4-column composite)
Primary key — EMFormMDM?            Id
Primary key — EMFormPregnancy?      EandMformId
Primary key — ComprehensiveAssmt?   SiteCode + Id
How is change detected?             FormQuestionAnswers: no checksum — all matched rows updated
                                    AnswerSignatures: RowChkSum stored but comparison commented out
                                    ComprehensiveAssessment: RowChkSum read but not used as gate
What is RowState?                   FormQA/Signatures: int 0=soft-deleted, 1=active
                                    ComprehensiveAssessment: bool false=deleted, true=active
How are soft-deletes handled?       Pre-pass resets RowState=0 for rows in the date window;
                                    SAMMS rows re-activate them to RowState=1;
                                    rows not seen in current fetch remain RowState=0
What is TblForms2Process?           ctrl.tbl_Forms2Process — configuration table controlling
                                    which forms are processed and whether date filtering applies
What is BAMMerge?                   pats.BAMMerge — stored procedure that recalculates Behavioral
                                    Analysis Metrics aggregates after FormQuestionAnswers is updated
formDaysBack vs DaysBack?           formDaysBack = DaysBack - 15 (pushed 15 days further back
                                    than the standard lookback to catch late form entries)
Reload override?                    st.Reload = true forces wrkdt = 1/1/2010 (full history reload)
Table existence guard?              ETL checks if dbo.Form / dbo.answersignature exist before
                                    querying — silently skips clinics without these tables
RowTrax audit?                      Source DataTable row count vs total destination count per site
Error recovery?                     Scheduler resets failed tasks to Status=17 on next daily run
Treatment Plan normalization?       FormNames starting with "TP-" are treated as "Treatment Plan"
                                    when looking up in TblForms2Process
EandMForm date filter?              WHERE clause is commented out — loads ALL E&M records each run
PHC handled here?                   No — PHC uses PHC/Program.cs + PHC/SaveFormQAData.cs (slight
                                    signature difference: uses yearly bool instead of xForms list)
________________________________________
