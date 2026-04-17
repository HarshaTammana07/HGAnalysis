
BAM Form / BAM Score / Diagnosis-10 ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract Brief Addiction Monitor (BAM) form records, BAM score rows, and ICD-10 diagnosis records from local SAMMS SQL Server databases at each clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What BAM form, BAM score, and Diagnosis-10 data are and why they exist
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How all three load methods (SaveBamForm, SaveBamScore, SaveTblDiags) work
- How the source SQL is built dynamically for each table type
- What the source tables look like and their key columns
- What the destination tables look like and all their columns
- How the shared EF Core two-phase upsert pattern works
- What happens when errors occur
- All known anomalies and quirks in the code

IMPORTANT NAMING NOTE: Two different BAM pipelines exist in this codebase.
This file (SaveBAM.cs) implements three per-clinic EF Core upsert methods: SaveBamForm,
SaveBamScore, and SaveTblDiags — loaded under BHGTaskRunner.exe 6 (Samms-Forms) into
pats.tbl_BamForm, pats.tbl_BamScore, and pats.tbl_TblDiag10 respectively.
A separate method called SaveBAM (all caps) in SaveGlobal.cs loads the global
pats.tbl_briefaddictionmonitor table under BHGTaskRunner.exe 1 (SAMMSGlobal).
That pipeline is documented in SaveGlobal-Documentation. Do not confuse the two.
________________________________________

2. High-Level Business Summary

What is BAM Form data?

The Brief Addiction Monitor (BAM) is a standardised clinical outcomes tool used in
Medication-Assisted Treatment (MAT) programs to track patient recovery progress over
time. The BAM questionnaire consists of 17 items across three subscales: substance use,
risk factors, and protective factors. Clinicians administer the BAM at scheduled
intervals and the completed form is recorded in SAMMS.

The BAM Form ETL (SaveBamForm) captures the header-level record for each BAM
administration — who completed it, when, how (clinician interview vs self-report vs
phone), the 17 individual question responses with both numeric scores and free-text
answers, three computed subscale score text fields, staff signature details, creation
and modification audit fields, IsDeleted status, and version.

What is BAM Score data?

SaveBamScore captures auxiliary scoring rows associated with BAM or similar outcomes
workflows. Each row links a ClientId and optional tprID (third-party record reference)
with a Description label and a Score value. These are scored summary or subscale rows
that complement the detailed BAM Form record.

What is Diagnosis-10 data?

SaveTblDiags captures per-patient ICD-10 clinical diagnosis records from SAMMS.
Each row records a diagnosis code (dgDIAG10), description, the associated client,
diagnosing staff member, date, whether it is primary, diagnosis type, notes, enrollment
linkage, and an optional end date. Although grouped in SaveBAM.cs for historical reasons,
this data is diagnosis management data — not BAM questionnaire data.

Why this data is important

The BAM Form dataset:
- Supports SAMMS-required outcomes reporting at regular treatment intervals
- Feeds patient recovery progress dashboards and analytics
- Enables tracking of risk vs protective factor trends over time
- Provides evidence for treatment plan adjustments and regulatory compliance

The Diagnosis-10 dataset:
- Drives billing and coding workflows (ICD-10 codes on claims)
- Supports clinical documentation and patient-level diagnosis history
- Enables payer-required diagnosis validation for claim submission

Load type

All three methods use EF Core upsert only — no SqlBulkCopy staging path exists.
Each method pre-loads all existing Azure rows for the SiteCode, builds new entity
objects from the source DataTable via a dynamic column switch, looks up the existing
record by composite key, and either updates in-place or stages for insert.
No RowChkSum change detection is used — every existing row is always fully overwritten.
No RowState soft-delete is used in any of the three methods.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=6                     Main ETL orchestrator for Samms-Forms
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic SAMMS SQL Server
dms.vw_MapAction (Azure DB)                 Maps destination tables to schedule TaskNames
dms.tbl_MapSrc2Dsn (Azure DB)              Column list + RowChkSum expression for SELECT build
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SaveBAM.cs (BHG-DR-LIB)                    EF Core upsert: SaveBamForm, SaveBamScore, SaveTblDiags
Models/TblBamForm.cs                        EF entity mapped to pats.tbl_BamForm
Models/TblBamScore.cs                       EF entity mapped to pats.tbl_BamScore
Models/TblDiag10.cs                         EF entity mapped to pats.tbl_TblDiag10
pats.tbl_BamForm (Azure)                    Final destination for BAM form instance records
pats.tbl_BamScore (Azure)                   Final destination for BAM score rows
pats.tbl_TblDiag10 (Azure)                 Final destination for ICD-10 diagnosis records
tsk.tbl_RowTrax (Azure)                    Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How BAM and Diagnosis Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily and populates the task queue. It does NOT move data —
it only creates tasks.

What the Scheduler does for BAM Form / BAM Score / Diagnosis-10

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For Samms-Forms, there is a row:
    Name        = 'Samms-Forms'
    ActionKey   = 6
    ScheduleId  = (forms schedule ID)

It inserts one parent task row into tsk.tbl_Tasks:
    TaskName = 'Samms-Forms'
    SiteCode = 'All'
    Status   = 17

Step 3 — Insert child task rows (one per clinic per table)
The Scheduler uses dms.vw_MapAction to determine which destination tables belong to the
Samms-Forms schedule. For the three BAM/Diagnosis tables:

    when DsnSchema + '.' + DsnTbl = 'pats.tbl_bamform'     → TaskName = 'pats.tbl_bamform'
    when DsnSchema + '.' + DsnTbl = 'pats.tbl_bamscore'    → TaskName = 'pats.tbl_bamscore'
    when DsnSchema + '.' + DsnTbl = 'pats.tbl_tbldiag10'   → TaskName = 'pats.tbl_tbldiag10'

This produces child task rows for each active clinic:
    TaskName = 'pats.tbl_bamform'
    SiteCode = 'B01A', 'VBRA', etc.

    TaskName = 'pats.tbl_bamscore'
    SiteCode = 'B01A', 'VBRA', etc.

    TaskName = 'pats.tbl_tbldiag10'
    SiteCode = 'B01A', 'VBRA', etc.

Step 4 — Advance the schedule
    update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime) where Enabled = 1

Task queue structure after Scheduler runs:

tsk.tbl_Tasks will contain:
    ParentTaskId = NULL
        TaskName = 'Samms-Forms'
        SiteCode = 'All'
        Status   = 17  (ready)

    ParentTaskId = (above row's TaskId)
        TaskName = 'pats.tbl_bamform'
        SiteCode = 'B01A'
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_bamscore'
        SiteCode = 'B01A'
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_tbldiag10'
        SiteCode = 'B01A'
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How BAM Tasks Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 6 to process only the Samms-Forms schedule.

Command:   BHGTaskRunner.exe 6

Step 1 — Filter task queue for Samms-Forms
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"                // PHC uses a separate runner
        && x.Status == 17                  // ready to run
        && x.TaskName == "Samms-Forms"
        && x.RunAt < DateTime.Now)

Step 2 — Mark parent task as running
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)

Step 4 — For each child task, dispatch by TaskName (st.TaskName)

Step 5 — Build base SELECT
The base SELECT (strCmd) is assembled using SelectConstructor.GetSLT():

    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw

The DaysBack constant is -15. WhereCondition from the task metadata is applied:
    strWhere = st.WhereCondition
                 .Replace("@SiteCode",  "'" + st.SiteCode + "'")
                 .Replace("@WorkDate",  "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
                 .Replace("@Samms",     "'SAMMS'")

Step 6 — pats.tbl_bamform path (case "pats.tbl_bamform")
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveBamForm(SrcDt, st.SiteCode, null)

Step 7 — pats.tbl_bamscore path (case "pats.tbl_bamscore")
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveBamScore(SrcDt, st.SiteCode, null)

Step 8 — pats.tbl_tbldiag10 path (case "pats.tbl_tbldiag10")
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveTblDiags(SrcDt, st.SiteCode, null)

Step 9 — RowTrax (if st.RowTrax = true and SiteCode != "PHC")
    Source count = count(*) from SAMMS using task FromTblVw
    Dest count   = count(*) from target Azure table where SiteCode = sc

Step 10 — Mark task complete
    task.Status = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'B01A'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_B01;...'

The source view/table name (st.FromTblVw) and schema (st.SrcSchema) come from the task
metadata row in dms.vw_MapAction. For BAM and Diagnosis records the source objects in
SAMMS follow the standard dbo.tbl* naming convention.

The DataTable returned is passed directly into SaveBamForm, SaveBamScore, or SaveTblDiags.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

All source tables live in the clinic's local SAMMS SQL Server under the dbo schema.
The exact table or view name is stored per task in dms.tbl_MapAction via st.FromTblVw.
________________________________________

7a. dbo.tblBamForm — BAM Form Instance Records

Primary Key: Id (form instance ID within this SAMMS clinic)

Column Name             Type            Description
-----------             ----            -----------
Id                      int             Unique BAM form instance ID within this clinic
SiteCode                int/varchar     Source site identifier
PreAdmissionId          int             Linked pre-admission record ID (nullable)
ClientId                int             Patient/client ID (nullable)
DataFormId              int             Form template ID (nullable)
BAMDate                 datetime        Date the BAM was administered (nullable)
InterviewerID           varchar         ID of the interviewer / clinician
ClinicianInterview      bit             True = administered as clinician interview
SelfReport              bit             True = patient completed self-report
Phone                   bit             True = administered via telephone
TimeStarted             datetime        Time the form session began (nullable)
InstructionsQ1          int             Numeric response to item 1 (nullable)
InstructionsQ1Txt       varchar         Free-text response to item 1
InstructionsQ2          int             Numeric response to item 2 (nullable)
InstructionsQ2Txt       varchar         Free-text response to item 2
InstructionsQ3          int             Numeric response to item 3 (nullable)
InstructionsQ3Txt       varchar         Free-text response to item 3
InstructionsQ4          int             Numeric response to item 4 (nullable)
InstructionsQ4Txt       varchar         Free-text response to item 4
InstructionsQ5          int             Numeric response to item 5 (nullable)
InstructionsQ5Txt       varchar         Free-text response to item 5
InstructionsQ6          int             Numeric response to item 6 (nullable)
InstructionsQ6Txt       varchar         Free-text response to item 6
InstructionsQ7A         int             Sub-item A of question 7 (nullable)
InstructionsQ7B         int             Sub-item B of question 7 (nullable)
InstructionsQ7C         int             Sub-item C of question 7 (nullable)
InstructionsQ7D         int             Sub-item D of question 7 (nullable)
InstructionsQ7E         int             Sub-item E of question 7 (nullable)
InstructionsQ7F         int             Sub-item F of question 7 (nullable)
InstructionsQ7G         int             Sub-item G of question 7 (nullable)
InstructionsQ8          int             Numeric response to item 8 (nullable)
InstructionsQ8Txt       varchar         Free-text response to item 8
InstructionsQ9          int             Numeric response to item 9 (nullable)
InstructionsQ9Txt       varchar         Free-text response to item 9
InstructionsQ10         int             Numeric response to item 10 (nullable)
InstructionsQ10Txt      varchar         Free-text response to item 10
InstructionsQ11         int             Numeric response to item 11 (nullable)
InstructionsQ11Txt      varchar         Free-text response to item 11
InstructionsQ12         int             Numeric response to item 12 (nullable)
InstructionsQ12Txt      varchar         Free-text response to item 12
InstructionsQ13         int             Numeric response to item 13 (nullable)
InstructionsQ13Txt      varchar         Free-text response to item 13
InstructionsQ14         int             Numeric response to item 14 (nullable)
InstructionsQ14Txt      varchar         Free-text response to item 14
InstructionsQ15         int             Numeric response to item 15 (nullable)
InstructionsQ15Txt      varchar         Free-text response to item 15
InstructionsQ16         int             Numeric response to item 16 (nullable)
InstructionsQ16Txt      varchar         Free-text response to item 16
InstructionsQ17         int             Numeric response to item 17 (nullable)
InstructionsQ17Txt      varchar         Free-text response to item 17
TimeFinished            datetime        Time the form session ended (nullable)
SubscaleScoreTxt1       varchar         Computed subscale score text (subscale 1)
SubscaleScoreTxt2       varchar         Computed subscale score text (subscale 2)
SubscaleScoreTxt3       varchar         Computed subscale score text (subscale 3)
StaffSignature          varchar         Staff signature value
StaffSignatureBy        varchar         Username of signing staff member
StaffSignatureDate      datetime        Date staff signed (nullable)
CreatedBy               varchar         User who created this form instance
CreatedOn               datetime        Timestamp of form creation (nullable)
ModifiedBy              varchar         User who last modified this form instance
ModifiedOn              datetime        Timestamp of last modification (nullable)
IsDeleted               bit             True = this form instance has been deleted in SAMMS
Version                 varchar         Form version identifier
________________________________________

7b. dbo.tblBamScore — BAM Score Rows

Primary Key: Id (score row ID within this SAMMS clinic)

Column Name     Type        Description
-----------     ----        -----------
Id              int         Unique score row ID
SiteCode        int/varchar Source site identifier
ClientId        int         Patient/client ID (nullable)
tprID           int         Third-party record reference ID (nullable)
Description     varchar     Score label / subscale name (only mapped if length > 6)
Score           varchar     Score value (stored as string)
________________________________________

7c. dbo.tblDiag10 — ICD-10 Diagnosis Records

Primary Key: dgID (diagnosis record ID within this SAMMS clinic)

Column Name             Type        Description
-----------             ----        -----------
dgID                    int         Unique diagnosis record ID
SiteCode                int/varchar Source site identifier
dgCLTID                 int         Patient/client ID (nullable)
dgDIAG                  varchar     Diagnosis code (non-ICD10 legacy field, nullable)
dgDATE                  datetime    Date diagnosis was entered (nullable, length > 6 guard)
dgDESC                  varchar     Diagnosis description text (nullable)
dgSTAFF                 varchar     Staff who entered the diagnosis (nullable)
dgdt                    datetime    Diagnosis timestamp/date variant (nullable, length > 6 guard)
dgPRIMARY               bit         True = this is the primary diagnosis (nullable)
dgDIAG10                varchar     ICD-10 diagnosis code (nullable)
dgDIAG10Description     varchar     ICD-10 code description (nullable)
dgNote                  varchar     Free-text diagnosis note
dgType                  varchar     Diagnosis type code (nullable)
EnrollmentId            int         Linked enrollment/admission ID (nullable)
dgEndDate               datetime    Date the diagnosis was resolved/ended (nullable, length > 8 guard)
________________________________________

8. SaveBamForm — EF Core Upsert (All Sites)

File: BCAppCode/BHG-DR-LIB/SaveBAM.cs
Class: SaveData (partial class)
Method: SaveBamForm()

Method signature:
    public RCodes SaveBamForm(
        DataTable tbl,          // rows from SAMMS — one per BAM form instance
        string sc,              // SiteCode e.g. "B01A"
        BHG_DRContext db)       // EF context (created if null)

Returns: RCodes — IsResult, RowsIns, RowsUpd, ExceptMsg, ExceptInnerMsg

EF Core upsert logic — step by step:

Step 1 — Create EF context and initialise RCodes
    res.IsResult = true
    if (db == null) { db = new BHG_DRContext(); }
    DateTime RunDT = DateTime.Now  (NOTE: declared but never used — dead code)

Step 2 — Load all existing Azure rows for this site
    List<TblBamForm> BamForms = db.TblBamForms.Where(x => x.SiteCode == sc).ToList()

All existing pats.tbl_BamForm rows for this SiteCode are loaded into memory. There is no
date filter here — the entire site's history is loaded.

Step 3 — Initialise new-rows collection
    List<TblBamForm> NewBFs = new List<TblBamForm>()

Step 4 — Loop through every row in the SAMMS DataTable
    foreach (DataRow r in tbl.Rows)

Step 5 — Build entity object from columns
A new TblBamForm object bf is created and all columns are mapped via switch:

    Column mapping:
    Source Column       Destination Field       Guard / Transformation
    sitecode            bf.SiteCode = sc        Always uses parameter sc (not source value)
    id                  bf.Id                   int.Parse — always
    preadmissionid      bf.PreAdmissionId       int — only if length > 0
    clientid            bf.ClientId             int — only if length > 0
    dataformid          bf.DataFormId           int — only if length > 0
    bamdate             bf.BAMDate              DateTime — only if length > 6
    interviewerid       bf.InterviewerID        Always (string)
    clinicianinterview  bf.ClinicianInterview   bool — only if length > 0
    selfreport          bf.SelfReport           bool — only if length > 0
    phone               bf.Phone                bool — only if length > 0
    timestarted         bf.TimeStarted          DateTime — only if length > 6
    instructionsq1      bf.InstructionsQ1       int — only if length > 0
    instructionsq1txt   bf.InstructionsQ1Txt    Always (string)
    instructionsq2      bf.InstructionsQ2       int — only if length > 0
    instructionsq2txt   bf.InstructionsQ2Txt    Always (string)
    instructionsq3      bf.InstructionsQ3       int — only if length > 0
    instructionsq3txt   bf.InstructionsQ3Txt    Always (string)
    instructionsq4      bf.InstructionsQ4       int — only if length > 0
    instructionsq4txt   bf.InstructionsQ4Txt    Always (string)
    instructionsq5      bf.InstructionsQ5       int — only if length > 0
    instructionsq5txt   bf.InstructionsQ5Txt    Always (string)
    instructionsq6      bf.InstructionsQ6       int — only if length > 0
    instructionsq6txt   bf.InstructionsQ6Txt    Always (string)
    instructionsq7a     bf.InstructionsQ7A      int — only if length > 0
    instructionsq7b     bf.InstructionsQ7B      int — only if length > 0
    instructionsq7c     bf.InstructionsQ7C      int — only if length > 0
    instructionsq7d     bf.InstructionsQ7D      int — only if length > 0
    instructionsq7e     bf.InstructionsQ7E      int — only if length > 0
    instructionsq7f     bf.InstructionsQ7F      int — only if length > 0
    instructionsq7g     bf.InstructionsQ7G      int — only if length > 0
    instructionsq8      bf.InstructionsQ8       int — only if length > 0
    instructionsq8txt   bf.InstructionsQ8Txt    Always (string)
    instructionsq9      bf.InstructionsQ9       int — only if length > 0
    instructionsq9txt   bf.InstructionsQ9Txt    Always (string)
    instructionsq10     bf.InstructionsQ10      int — only if length > 0
    instructionsq10txt  bf.InstructionsQ10Txt   Always (string)
    instructionsq11     bf.InstructionsQ11      int — only if length > 0
    instructionsq11txt  bf.InstructionsQ11Txt   Always (string)
    instructionsq12     bf.InstructionsQ12      int — only if length > 0
    instructionsq12txt  bf.InstructionsQ12Txt   Always (string)
    instructionsq13     bf.InstructionsQ13      int — only if length > 0
    instructionsq13txt  bf.InstructionsQ13Txt   Always (string)
    instructionsq14     bf.InstructionsQ14      int — only if length > 0
    instructionsq14txt  bf.InstructionsQ14Txt   Always (string)
    instructionsq15     bf.InstructionsQ15      int — only if length > 0
    instructionsq15txt  bf.InstructionsQ15Txt   Always (string)
    instructionsq16     bf.InstructionsQ16      int — only if length > 0
    instructionsq16txt  bf.InstructionsQ16Txt   Always (string)
    instructionsq17     bf.InstructionsQ17      int — only if length > 0
    instructionsq17txt  bf.InstructionsQ17Txt   Always (string)
    timefinished        bf.TimeFinished         DateTime — only if length > 6
    subscalescoretxt1   bf.SubscaleScoreTxt1    Always (string)
    subscalescoretxt2   bf.SubscaleScoreTxt2    Always (string)
    subscalescoretxt3   bf.SubscaleScoreTxt3    Always (string)
    staffsignature      bf.StaffSignature       Always (string)
    staffsignatureby    bf.StaffSignatureBy     Always (string)
    staffsignaturedate  bf.StaffSignatureDate   DateTime — only if length > 0
                                                NOTE: uses length > 0 not length > 6 — unlike
                                                all other DateTime fields in this method
    createdby           bf.CreatedBy            Always (string)
    createdon           bf.CreatedOn            DateTime — only if length > 0
    modifiedby          bf.ModifiedBy           Always (string)
    modifiedon          bf.ModifiedOn           DateTime — only if length > 0
    isdeleted           bf.IsDeleted            bool — only if length > 0
    version             bf.Version              Always (string)

Step 6 — Lookup existing record
    Models.TblBamForm dbbf = BamForms
        .FirstOrDefault(x => x.SiteCode == bf.SiteCode && x.Id == bf.Id)

Step 7 — Insert or Update
    if (dbbf == null):
        NewBFs.Add(bf)
        res.RowsIns += 1
    else:
        res.RowsUpd += 1
        (copy every mapped field from bf to dbbf — explicit property assignment block)

The update block explicitly reassigns every mapped property to the existing tracked
entity: BAMDate, ClientId, ClinicianInterview, CreatedBy, CreatedOn, DataFormId,
InstructionsQ1…Q17 (all numeric and text variants), InterviewerID, IsDeleted,
ModifiedBy, ModifiedOn, Phone, PreAdmissionId, SelfReport, StaffSignature,
StaffSignatureBy, StaffSignatureDate, SubscaleScoreTxt1..3, TimeFinished, TimeStarted, Version.

Step 8 — First commit — updates to existing rows
    db.SaveChanges()

Step 9 — Second commit — insert new rows
    if (NewBFs.Count > 0):
        db.TblBamForms.AddRange(NewBFs)
        db.SaveChanges()
________________________________________

9. SaveBamScore — EF Core Upsert (All Sites)

File: BCAppCode/BHG-DR-LIB/SaveBAM.cs
Class: SaveData (partial class)
Method: SaveBamScore()

Method signature:
    public RCodes SaveBamScore(
        DataTable tbl,          // rows from SAMMS — one per BAM score row
        string sc,              // SiteCode
        BHG_DRContext db)       // EF context (created if null)

Returns: RCodes — IsResult, RowsIns, RowsUpd, ExceptMsg, ExceptInnerMsg

EF Core upsert logic — step by step:

Step 1 — Create EF context and initialise
    if (db == null) { db = new BHG_DRContext(); }
    DateTime RunDT = DateTime.Now  (NOTE: declared but never used — dead code)

Step 2 — Load all existing Azure rows for this site
    List<TblBamScore> BamScores = db.TblBamScores.Where(x => x.SiteCode == sc).ToList()

Step 3 — Initialise new-rows collection
    List<TblBamScore> NewBSs = new List<TblBamScore>()

Step 4 — Loop through every row in the SAMMS DataTable, build entity via switch:

    Source Column   Destination Field   Guard / Transformation
    sitecode        bs.SiteCode = sc    Always uses parameter sc
    id              bs.Id               int.Parse — always
    clientid        bs.ClientId         int — only if length > 0
    tprid           bs.tprID            int — only if length > 0
    description     bs.Description      Only if length > 6
                                        NOTE: uses length > 6 — unlike most string fields
                                        which are unconditional. Short descriptions (1-6
                                        chars) will never be stored or updated in Azure.
    score           bs.Score            Only if length > 0 (string stored as-is)

Step 5 — Lookup existing record
    Models.TblBamScore dbbs = BamScores
        .FirstOrDefault(x => x.SiteCode == bs.SiteCode && x.Id == bs.Id)

Step 6 — Insert or Update
    if (dbbs == null):
        NewBSs.Add(bs)
        res.RowsIns += 1
    else:
        res.RowsUpd += 1
        dbbs.ClientId    = bs.ClientId
        dbbs.tprID       = bs.tprID
        dbbs.Description = bs.Description
        dbbs.Score       = bs.Score

Step 7 — Two-phase commit
    db.SaveChanges()
    if (NewBSs.Count > 0):
        db.TblBamScores.AddRange(NewBSs)
        db.SaveChanges()
________________________________________

10. SaveTblDiags — EF Core Upsert (All Sites)

File: BCAppCode/BHG-DR-LIB/SaveBAM.cs
Class: SaveData (partial class)
Method: SaveTblDiags()

Method signature:
    public RCodes SaveTblDiags(
        DataTable tbl,          // rows from SAMMS — one per ICD-10 diagnosis row
        string sc,              // SiteCode
        BHG_DRContext db)       // EF context (created if null)

Returns: RCodes — IsResult, RowsIns, RowsUpd, ExceptMsg, ExceptInnerMsg

EF Core upsert logic — step by step:

Step 1 — Create EF context and initialise
    if (db == null) { db = new BHG_DRContext(); }
    DateTime RunDT = DateTime.Now  (NOTE: declared but never used — dead code)

Step 2 — Load all existing Azure rows for this site
    List<TblDiag10> TDs = db.TblDiag10s.Where(x => x.SiteCode == sc).ToList()

Step 3 — Initialise new-rows collection
    List<TblDiag10> NewTDs = new List<TblDiag10>()

Step 4 — Loop through every row in the SAMMS DataTable, build entity via switch:

    Source Column           Destination Field       Guard / Transformation
    sitecode                td.SiteCode = sc        Always uses parameter sc
    dgid                    td.dgID                 int.Parse — always
    dgcltid                 td.dgCLTID              int — only if length > 0
    dgdiag                  td.dgDIAG               Only if length > 0 (string)
    dgdate                  td.dgDATE               DateTime — only if length > 6
    dgdesc                  td.dgDESC               Only if length > 0 (string)
    dgstaff                 td.dgSTAFF              Only if length > 0 (string)
    dgdt                    td.dgdt                 DateTime — only if length > 6
    dgprimary               td.dgPRIMARY            bool — only if length > 0
    dgdiag10                td.dgDIAG10             Only if length > 0 (string)
    dgdiag10description     td.dgDIAG10Description  Only if length > 0 (string)
    dgnote                  td.dgNote               Always (string — no guard)
    dgtype                  td.dgType               Only if length > 0 (string)
    enrollmentid            td.EnrollmentId         int — only if length > 0
    dgenddate               td.dgEndDate            DateTime — only if length > 8
                                                    NOTE: uses length > 8, stricter than
                                                    dgdate/dgdt which use length > 6

Step 5 — Lookup existing record
    Models.TblDiag10 dbtd = TDs
        .FirstOrDefault(x => x.SiteCode == td.SiteCode && x.dgID == td.dgID)

Step 6 — Insert or Update
    if (dbtd == null):
        NewTDs.Add(td)
        res.RowsIns += 1
    else:
        res.RowsUpd += 1
        (copy all clinical fields: dgCLTID, dgDIAG, dgDESC, dgDATE, dgSTAFF, dgdt,
         dgPRIMARY, dgDIAG10, dgDIAG10Description, dgNote, dgType, EnrollmentId, dgEndDate)

Step 7 — Two-phase commit
    db.SaveChanges()
    if (NewTDs.Count > 0):
        db.TblDiag10s.AddRange(NewTDs)
        db.SaveChanges()
________________________________________

11. Destination Tables — Azure BHG_DR (pats schema)
________________________________________

11a. pats.tbl_BamForm

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_BamForm
EF Model: BHG-DR-LIB/Models/TblBamForm.cs

Primary Key: SiteCode + Id (composite)

C# Property (EF)        SQL Column              Type                Notes
----------------        ---------------         ----                -----
SiteCode                SiteCode                varchar             Clinic identifier
Id                      Id                      int                 BAM form instance ID from SAMMS
PreAdmissionId          PreAdmissionId          int (nullable)      Pre-admission linkage
ClientId                ClientId                int (nullable)      Patient / client ID
DataFormId              DataFormId              int (nullable)      Form template ID
BAMDate                 BAMDate                 datetime (nullable) Date BAM was administered
InterviewerID           InterviewerID           varchar             Interviewer identifier
ClinicianInterview      ClinicianInterview      bit (nullable)      Clinician-administered flag
SelfReport              SelfReport              bit (nullable)      Self-report flag
Phone                   Phone                   bit (nullable)      Phone administration flag
TimeStarted             TimeStarted             datetime (nullable) Session start time
InstructionsQ1          InstructionsQ1          int (nullable)      Item 1 numeric response
InstructionsQ1Txt       InstructionsQ1Txt       varchar             Item 1 text response
InstructionsQ2          InstructionsQ2          int (nullable)      Item 2 numeric response
InstructionsQ2Txt       InstructionsQ2Txt       varchar             Item 2 text response
InstructionsQ3          InstructionsQ3          int (nullable)      Item 3 numeric response
InstructionsQ3Txt       InstructionsQ3Txt       varchar             Item 3 text response
InstructionsQ4          InstructionsQ4          int (nullable)      Item 4 numeric response
InstructionsQ4Txt       InstructionsQ4Txt       varchar             Item 4 text response
InstructionsQ5          InstructionsQ5          int (nullable)      Item 5 numeric response
InstructionsQ5Txt       InstructionsQ5Txt       varchar             Item 5 text response
InstructionsQ6          InstructionsQ6          int (nullable)      Item 6 numeric response
InstructionsQ6Txt       InstructionsQ6Txt       varchar             Item 6 text response
InstructionsQ7A         InstructionsQ7A         int (nullable)      Q7 sub-item A
InstructionsQ7B         InstructionsQ7B         int (nullable)      Q7 sub-item B
InstructionsQ7C         InstructionsQ7C         int (nullable)      Q7 sub-item C
InstructionsQ7D         InstructionsQ7D         int (nullable)      Q7 sub-item D
InstructionsQ7E         InstructionsQ7E         int (nullable)      Q7 sub-item E
InstructionsQ7F         InstructionsQ7F         int (nullable)      Q7 sub-item F
InstructionsQ7G         InstructionsQ7G         int (nullable)      Q7 sub-item G
InstructionsQ8          InstructionsQ8          int (nullable)      Item 8 numeric response
InstructionsQ8Txt       InstructionsQ8Txt       varchar             Item 8 text response
InstructionsQ9          InstructionsQ9          int (nullable)      Item 9 numeric response
InstructionsQ9Txt       InstructionsQ9Txt       varchar             Item 9 text response
InstructionsQ10         InstructionsQ10         int (nullable)      Item 10 numeric response
InstructionsQ10Txt      InstructionsQ10Txt      varchar             Item 10 text response
InstructionsQ11         InstructionsQ11         int (nullable)      Item 11 numeric response
InstructionsQ11Txt      InstructionsQ11Txt      varchar             Item 11 text response
InstructionsQ12         InstructionsQ12         int (nullable)      Item 12 numeric response
InstructionsQ12Txt      InstructionsQ12Txt      varchar             Item 12 text response
InstructionsQ13         InstructionsQ13         int (nullable)      Item 13 numeric response
InstructionsQ13Txt      InstructionsQ13Txt      varchar             Item 13 text response
InstructionsQ14         InstructionsQ14         int (nullable)      Item 14 numeric response
InstructionsQ14Txt      InstructionsQ14Txt      varchar             Item 14 text response
InstructionsQ15         InstructionsQ15         int (nullable)      Item 15 numeric response
InstructionsQ15Txt      InstructionsQ15Txt      varchar             Item 15 text response
InstructionsQ16         InstructionsQ16         int (nullable)      Item 16 numeric response
InstructionsQ16Txt      InstructionsQ16Txt      varchar             Item 16 text response
InstructionsQ17         InstructionsQ17         int (nullable)      Item 17 numeric response
InstructionsQ17Txt      InstructionsQ17Txt      varchar             Item 17 text response
TimeFinished            TimeFinished            datetime (nullable) Session end time
SubscaleScoreTxt1       SubscaleScoreTxt1       varchar             Subscale 1 computed score text
SubscaleScoreTxt2       SubscaleScoreTxt2       varchar             Subscale 2 computed score text
SubscaleScoreTxt3       SubscaleScoreTxt3       varchar             Subscale 3 computed score text
StaffSignature          StaffSignature          varchar             Staff signature value
StaffSignatureBy        StaffSignatureBy        varchar             Signing staff username
StaffSignatureDate      StaffSignatureDate      datetime (nullable) Staff signature date
CreatedBy               CreatedBy               varchar             User who created this record
CreatedOn               CreatedOn               datetime (nullable) Creation timestamp
ModifiedBy              ModifiedBy              varchar             User who last modified record
ModifiedOn              ModifiedOn              datetime (nullable) Last modification timestamp
IsDeleted               IsDeleted               bit (nullable)      True = deleted in SAMMS
Version                 Version                 varchar             Form template version identifier
________________________________________

11b. pats.tbl_BamScore

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_BamScore
EF Model: BHG-DR-LIB/Models/TblBamScore.cs

Primary Key: SiteCode + Id (composite)

C# Property (EF)    SQL Column      Type            Notes
----------------    ---------------  ----            -----
SiteCode            SiteCode        varchar         Clinic identifier
Id                  Id              int             Score row ID from SAMMS
ClientId            ClientId        int (nullable)  Patient / client ID
tprID               tprID           int (nullable)  Third-party record reference ID
Description         Description     varchar         Score label / subscale name
Score               Score           varchar         Score value (stored as string)
________________________________________

11c. pats.tbl_TblDiag10

Location: Azure SQL Database BHG_DR
Schema  : pats
Table   : tbl_TblDiag10
EF Model: BHG-DR-LIB/Models/TblDiag10.cs

Primary Key: SiteCode + dgID (composite)

C# Property (EF)        SQL Column              Type                Notes
----------------        ---------------         ----                -----
SiteCode                SiteCode                varchar             Clinic identifier
dgID                    dgID                    int                 Diagnosis record ID from SAMMS
dgCLTID                 dgCLTID                 int (nullable)      Patient / client ID
dgDIAG                  dgDIAG                  varchar             Legacy diagnosis code
dgDATE                  dgDATE                  datetime (nullable) Date of diagnosis entry
dgDESC                  dgDESC                  varchar             Diagnosis description text
dgSTAFF                 dgSTAFF                 varchar             Diagnosing staff identifier
dgdt                    dgdt                    datetime (nullable) Alternative diagnosis timestamp
dgPRIMARY               dgPRIMARY               bit (nullable)      True = primary diagnosis
dgDIAG10                dgDIAG10                varchar             ICD-10 diagnosis code
dgDIAG10Description     dgDIAG10Description     varchar             ICD-10 code full description
dgNote                  dgNote                  varchar             Free-text clinical note
dgType                  dgType                  varchar             Diagnosis type / category code
EnrollmentId            EnrollmentId            int (nullable)      Linked enrollment / admission ID
dgEndDate               dgEndDate               datetime (nullable) Date diagnosis was resolved
________________________________________

12. Change Detection

None of the three methods in SaveBAM.cs implement RowChkSum-based change detection.

Value   Method
-----   ------
Not used        SaveBamForm    — no RowChkSum field; every existing row is fully re-mapped on every run
Not used        SaveBamScore   — same; always overwrites all fields
Not used        SaveTblDiags   — same; always overwrites all fields

This is an always-update pattern. Every row returned from SAMMS that matches an existing
Azure record by composite key will have all its fields overwritten unconditionally.
________________________________________

13. RowState — Soft Delete Tracking

None of the three methods in SaveBAM.cs use a RowState soft-delete mechanism.

There is no pre-pass that sets existing rows to inactive before processing.
Records that no longer appear in the SAMMS source extract are not marked as deleted.
Only SaveBamForm maps an IsDeleted field from source — this reflects the SAMMS
soft-delete flag directly, but is not equivalent to the ETL RowState pattern used in
SaveDoses or SaveGlobal methods.

If a BAM form or diagnosis record is removed from the SAMMS extract due to the task
WhereCondition date scope, it will simply not be touched in the current run. It will
remain in Azure with its last-seen field values and will not be marked inactive.
________________________________________

14. Load Design Summary

Load type: Incremental upsert — no RowChkSum, no RowState, no pre-pass reset

Per run behavior for all three methods:

  Source query: controlled by st.WhereCondition + st.SortOrder from task metadata
  1. Load ALL existing Azure rows for this SiteCode into memory
  2. For each SAMMS source row:
       - Build entity object via dynamic column switch
       - Match by composite key (SiteCode + Id or SiteCode + dgID)
       - Not found  → stage in new-rows list; RowsIns++
       - Found      → overwrite all mapped fields on existing entity; RowsUpd++
  3. db.SaveChanges() — commit updates to existing rows
  4. if new-rows list > 0: AddRange + db.SaveChanges() — batch insert new rows

No reload flag or hard-delete path exists for any of these three methods.
No staging table or stored procedure MERGE is used — pure EF Core only.
________________________________________

15. Error Handling and Recovery

All three methods use the same error handling pattern:

    try
    {
        // pre-load + loop + SaveChanges() ...
    }
    catch (Exception e)
    {
        res.IsResult = false
        Console.WriteLine(e.Message)
        res.ExceptMsg = e.Message
        if (e.InnerException != null)
        {
            Console.WriteLine(e.InnerException.Message)
            res.ExceptInnerMsg = e.InnerException.Message
        }
    }

If an EF Core exception occurs during SaveChanges:
- The current batch for that clinic is not committed
- RCodes.IsResult = false is returned to BHGTaskRunner
- BHGTaskRunner logs the task error

Recovery behavior:
If a task fails, the Scheduler's daily reset restores it to Status=17 (ready):
    update tsk.tbl_Tasks set Status = 17 where Status = 18

A failed BAM or Diagnosis run for a clinic will automatically be retried the next day.
________________________________________

16. RowTrax — Audit and Row Count Tracking

For tasks where st.RowTrax = true (and SiteCode != "PHC"), BHGTaskRunner writes a
RowTrax entry after each run. The source count comes from a count(*) against the
SAMMS source using the task WhereCondition. The destination count comes from a
count(*) against the Azure target table for that SiteCode.

These counts are stored in tsk.tbl_RowTrax and are used for:
- Monitoring whether the ETL is writing expected volumes per clinic
- Detecting clinics where BAM or diagnosis data has dropped unexpectedly
- Historical trend analysis for data completeness auditing
________________________________________

17. Key Design Notes and Gotchas

RunDT declared but never used (all three methods):
    DateTime RunDT = DateTime.Now
This variable is declared at the top of each method but is never referenced anywhere
in the method body. It is dead code and can be safely removed in a refactor.

SaveBamForm — StaffSignatureDate uses weaker DateTime guard:
All other DateTime fields in SaveBamForm use length > 6 before DateTime.Parse.
StaffSignatureDate uses length > 0. A single non-empty character that is not a valid
date string will pass the guard and throw a DateTime.Parse exception. This is an
inconsistency compared to the defensive length > 6 pattern used elsewhere.

SaveBamScore — Description uses length > 6 guard:
The Description field (a string) uses length > 6 before assigning. This means
descriptions of 1–6 characters are never stored or updated in Azure. Short description
values that exist in SAMMS will silently be skipped. This is inconsistent with most
string fields in the codebase which are assigned unconditionally.

SaveTblDiags — dgEndDate uses length > 8 guard:
dgEndDate requires length > 8 before DateTime.Parse, unlike dgDATE and dgdt which use
length > 6. This means dgEndDate values with 7–8 character strings (e.g. M/D/YYYY
format) will not be parsed and will remain null in Azure.

No bulk path exists for any of these three methods:
Unlike Dose records (which have a SqlBulkCopy path for most sites), BAM Form, BAM Score,
and Diagnosis-10 records always use EF Core row-by-row upsert for all clinics.

SaveTblDiags is not BAM questionnaire data:
Despite living in SaveBAM.cs, SaveTblDiags loads ICD-10 clinical diagnosis records.
The grouping is a historical code organization decision, not a data domain decision.

No relationship enforced between SaveBamForm and SaveBamScore in ETL:
The ETL loads each table independently per task. There is no cross-check or FK
validation between pats.tbl_BamForm (form header) and pats.tbl_BamScore (score rows)
during the EF Core upsert. Referential integrity, if required, must be handled at
the database constraint level.

Naming disambiguation with SaveGlobal.SaveBAM:
This file (SaveBAM.cs) and its methods (SaveBamForm, SaveBamScore, SaveTblDiags) are
entirely separate from the SaveBAM method in SaveGlobal.cs, which loads
pats.tbl_briefaddictionmonitor under BHGTaskRunner.exe 1 (SAMMSGlobal). See the
BAMMergeGbl stored procedure context in SaveGlobal documentation for that pipeline.
________________________________________

18. End-to-End Flow Diagram

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17)
        |-- insert parent task: Samms-Forms (Status=17)
        |-- insert child tasks per clinic:
        |       pats.tbl_bamform    x 80+ clinics
        |       pats.tbl_bamscore   x 80+ clinics
        |       pats.tbl_tbldiag10  x 80+ clinics
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
        |-- for each child task (one per clinic per table type):
        |
        |   Build base strCmd from SelectConstructor (ActionKey=6)
        |   strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
        |   strCmd += " Where " + strWhere + " " + st.SortOrder
        |
        |======================================================
        |  BRANCH A: TaskName = pats.tbl_bamform
        |======================================================
        |
        |   SQLSvrManager.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |   (executes against clinic SAMMS SQL Server → DataTable SrcDt)
        |           |
        |           V
        |   SaveBamForm(SrcDt, st.SiteCode, null)
        |           |
        |     |-- load ALL Azure TblBamForm rows WHERE SiteCode = sc
        |     |-- loop SAMMS rows:
        |     |       build TblBamForm object via column switch
        |     |       SiteCode + Id lookup in memory
        |     |       not found → NewBFs.Add(bf)         RowsIns++
        |     |       found     → overwrite all fields    RowsUpd++
        |     |                   (no RowChkSum check — always updates)
        |     |-- db.SaveChanges()        (updates to existing rows)
        |     |-- AddRange(NewBFs)
        |     |-- db.SaveChanges()        (batch insert new rows)
        |           |
        |           V
        |       pats.tbl_BamForm (Azure BHG_DR)
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != PHC)
        |       source count = count from SAMMS source
        |       dest count   = count from pats.tbl_BamForm where SiteCode = sc
        |       → tsk.tbl_RowTrax
        |
        |======================================================
        |  BRANCH B: TaskName = pats.tbl_bamscore
        |======================================================
        |
        |   SQLSvrManager.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |           |
        |           V
        |   SaveBamScore(SrcDt, st.SiteCode, null)
        |           |
        |     |-- load ALL Azure TblBamScore rows WHERE SiteCode = sc
        |     |-- loop SAMMS rows:
        |     |       build TblBamScore via column switch
        |     |       SiteCode + Id lookup
        |     |       not found → NewBSs.Add(bs)         RowsIns++
        |     |       found     → overwrite ClientId, tprID, Description, Score
        |     |-- db.SaveChanges()
        |     |-- AddRange(NewBSs) + db.SaveChanges()
        |           |
        |           V
        |       pats.tbl_BamScore (Azure BHG_DR)
        |
        |-- RowTrax audit if enabled
        |
        |======================================================
        |  BRANCH C: TaskName = pats.tbl_tbldiag10
        |======================================================
        |
        |   SQLSvrManager.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |           |
        |           V
        |   SaveTblDiags(SrcDt, st.SiteCode, null)
        |           |
        |     |-- load ALL Azure TblDiag10 rows WHERE SiteCode = sc
        |     |-- loop SAMMS rows:
        |     |       build TblDiag10 via column switch
        |     |       SiteCode + dgID lookup
        |     |       not found → NewTDs.Add(td)          RowsIns++
        |     |       found     → overwrite all diag fields RowsUpd++
        |     |                   (no RowChkSum check)
        |     |-- db.SaveChanges()
        |     |-- AddRange(NewTDs) + db.SaveChanges()
        |           |
        |           V
        |       pats.tbl_TblDiag10 (Azure BHG_DR)
        |
        |-- RowTrax audit if enabled
        |
        V
BHGTaskRunner marks child task Status=20 (complete)
        |
        V
BHGTaskRunner marks parent task Status=20 (all children done)
________________________________________

19. File Reference Map

File Path                                           Purpose
---------                                           -------
BCAppCode/Scheduler/Program.cs                      Creates daily task queue — inserts Samms-Forms tasks
BCAppCode/BHGTaskRunner/Program.cs                  Main ETL driver (arg=6 → Samms-Forms pipeline)
                                                    Cases: pats.tbl_bamform ~line 494
                                                           pats.tbl_bamscore ~line 499
                                                           pats.tbl_tbldiag10 ~line 504
BCAppCode/BHG-DR-LIB/SaveBAM.cs                    EF Core upsert: SaveBamForm, SaveBamScore, SaveTblDiags
BCAppCode/BHG-DR-LIB/SaveGlobal.cs                 Contains SaveBAM (all caps) — different pipeline
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs              ADO.NET wrapper — executes SQL against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblBamForm.cs          EF Model → pats.tbl_BamForm
BCAppCode/BHG-DR-LIB/Models/TblBamScore.cs         EF Model → pats.tbl_BamScore
BCAppCode/BHG-DR-LIB/Models/TblDiag10.cs           EF Model → pats.tbl_TblDiag10
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs       EF DbContext — registers TblBamForms, TblBamScores, TblDiag10s
________________________________________

20. Quick Reference Summary

What triggers BAM / Diagnosis ETL?      Scheduler.exe creates tasks, BHGTaskRunner.exe 6 processes them
TaskName in scheduler?                   Samms-Forms (parent); pats.tbl_bamform / pats.tbl_bamscore /
                                         pats.tbl_tbldiag10 (child tasks)
Source tables in SAMMS?                  dbo.tblBamForm, dbo.tblBamScore, dbo.tblDiag10
                                         (exact object from dms.tbl_MapAction st.FromTblVw)
Destination tables in Azure?             pats.tbl_BamForm
                                         pats.tbl_BamScore
                                         pats.tbl_TblDiag10
EF Core or Bulk path?                    All three methods — EF Core only. No bulk path.
Staging table?                           None
Merge stored procedure?                  None
Primary key — tbl_BamForm?               SiteCode + Id (composite)
Primary key — tbl_BamScore?              SiteCode + Id (composite)
Primary key — tbl_TblDiag10?             SiteCode + dgID (composite)
How is change detected?                  No RowChkSum — every existing row always fully overwritten
What is RowState?                        Not used in any of these three methods
How are soft-deletes handled?            Not handled — records not in source extract remain in Azure
                                         untouched with last-seen values
IsDeleted field?                         Only in SaveBamForm — maps source isdeleted flag directly
Reload override?                         No reload flag; no hard-delete path in any method
Lookback window?                         Controlled by st.WhereCondition from task metadata (DaysBack = -15)
PHC handled here?                        No — PHC excluded by x.SiteCode != "PHC" filter
RowTrax audit?                           Source count vs destination count per site (where enabled)
Error recovery?                          Scheduler resets failed tasks to Status=17 on next daily run
Dead code in all three methods?          DateTime RunDT = DateTime.Now — declared but never used
Known date guard inconsistency?          SaveBamForm: StaffSignatureDate uses length > 0 vs length > 6
                                         SaveTblDiags: dgEndDate uses length > 8 vs length > 6
Known string guard inconsistency?        SaveBamScore: Description uses length > 6 — short descriptions lost
Naming caution?                          SaveBAM.cs / SaveBamForm ≠ SaveGlobal.cs / SaveBAM
                                         SaveBAM (Global) → TblBriefAddictionMonitor (Schedule 1)
                                         SaveBamForm (this file) → TblBamForm (Schedule 6)
________________________________________

Documentation generated from source: BHG-DR-LIB\SaveBAM.cs (551 lines, 3 methods).
Parent Schedule: Samms-Forms (Schedule 6 — BHGTaskRunner.exe 6)
Related global BAM pipeline: BHG-DR-LIB\SaveGlobal.cs method SaveBAM — see SaveGlobal-Documentation.
