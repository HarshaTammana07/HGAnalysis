
Appointments ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract appointment
and appointment attendance records from local SAMMS SQL Server databases at each
clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What Appointment data is and why it exists
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How the two load paths work: SaveAppointments and SaveAppointmentAttend
- How the source SQL is built dynamically for each table type
- What the source tables look like and their key columns
- What the destination tables look like and all their columns
- How RowState tracks soft-deleted / active attendance records
- What happens when errors occur
- Known code anomalies and quirks
________________________________________

2. High-Level Business Summary

What is Appointment data?

SAMMS maintains a clinical appointment and scheduling system for each clinic.
Appointments represent scheduled calendar events — individual counseling sessions,
group therapy, medical consultations, intake visits, and any other time-slot
reservations made for patients or staff.

The Appointments ETL pipeline manages two related destination tables:

1. dbo.tblAppointment (SAMMS)   → pats.tbl_Appointments (Azure)
   One row per calendar appointment. Captures when the appointment starts and ends,
   what type it is, who the resource is (staff), whether it is a recurring event,
   whether it is a drop-in or a scheduled visit, how many participants it allows,
   and any linked SalesForce or third-party sync identifiers.

2. dbo.tblAppointmentAttend (SAMMS) → pats.tbl_AppointmentAttend (Azure)
   One row per patient enrolled in an appointment. A single appointment (from
   pats.tbl_Appointments) can have many attendees. This table captures which patient
   (aacltid) is linked to which appointment (aaaptID), the date they were enrolled
   (aaDTENROLLED), and the date they were removed from the appointment (aaDTREMOVED).

Why it is important

The Appointments dataset enables:
- Tracking patient attendance and no-show patterns across all clinics
- Reporting on counseling session compliance for regulatory and payer requirements
- Feeding billing workflows — scheduled visits drive service codes and claims
- Supporting intake scheduling analytics and capacity planning
- Identifying patients removed from appointments (discharge indicators via aaDTREMOVED)
- SalesForce and third-party CRM integration via SalesForceId and sync flags

Load type

Both methods use EF Core upsert only — no bulk path exists for Appointments or
AppointmentAttend. Unlike Dose or DartsSrv which have a SqlBulkCopy staging path
for high-volume sites, all clinics use the same row-by-row EF Core path here.

SaveAppointments (pats.tbl_Appointments):
  Composite key: SiteCode + UniqueId
  Azure scope: all rows WHERE SiteCode = sc (no date filter applied in method)
  RowState: NOT maintained — no RowState field on TblAppointments
  RowChkSum: stored but no skip guard — all existing rows always overwritten

SaveAppointmentAttend (pats.tbl_AppointmentAttend):
  Composite key: SiteCode + AAId
  Azure scope: all rows WHERE SiteCode = sc (no date filter applied in method)
  RowState: maintained — true by default, false when aacltid < 0 (deleted patient sentinel)
  RowChkSum: stored but no skip guard — all existing rows always overwritten
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Configuration — defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks2 daily
BHGTaskRunner.exe arg=6                     Main ETL orchestrator for Samms-Forms
ctrl.tbl_LocationCons (Azure DB)            Connection strings for each clinic SAMMS SQL Server
dms.vw_MapAction (Azure DB)                 Maps destination tables to schedule TaskNames
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SaveAppointments.cs (BHG-DR-LIB)           EF Core upsert methods — SaveAppointments and
                                            SaveAppointmentAttend
pats.tbl_Appointments (Azure)              Final destination for appointment calendar records
pats.tbl_AppointmentAttend (Azure)         Final destination for appointment attendance records
tsk.tbl_RowTrax (Azure)                    Audit log — source vs destination row counts per run
________________________________________

4. Scheduler — How Appointment Tasks Are Created

File: BCAppCode/Scheduler/Program.cs

The Scheduler.exe runs once daily and populates the task queue. It does NOT move data —
it only creates tasks.

What the Scheduler does for Appointments (Samms-Forms)

Step 1 — Reset stuck tasks
    update tsk.tbl_Tasks2 set Status = 17 where Status = 18

Any task left in "running" state (Status=18) from a previous failed run is reset to
"ready" (Status=17) so it can be retried.

Step 2 — Expire old tasks
    update tsk.vwTaskList set RowState = 26
    where Status = 17 and WorkDate < today and WhereCondition = '1 = 1'

Step 3 — Insert the parent task row
The Scheduler reads tsk.tbl_Schedule where Enabled=1. For SAMMS-Forms, there is a row
with:
    Name        = 'Samms-Forms'
    ActionKey   = 6
    ScheduleId  = (forms schedule ID)

It inserts one parent task row into tsk.tbl_Tasks2:
    TaskName = 'Samms-Forms'
    SiteCode = 'All'
    Status   = 17

Step 4 — Insert child task rows (one per clinic per table)
The Scheduler uses dms.vw_MapAction to enumerate all active clinic/table combinations
under ActionKey 6. For appointments the relevant mappings are:

    when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Appointments'       → 'Samms-Forms'
    when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_AppointmentAttend'  → 'Samms-Forms'

This produces child task rows for each active clinic:
    TaskName = 'pats.tbl_Appointments'
    SiteCode = 'B01', 'VBRA', etc.

    TaskName = 'pats.tbl_AppointmentAttend'
    SiteCode = 'B01', 'VBRA', etc.

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
        TaskName = 'pats.tbl_Appointments'
        SiteCode = 'B01'
        Status   = 17

    ParentTaskId = (same parent)
        TaskName = 'pats.tbl_AppointmentAttend'
        SiteCode = 'B01'
        Status   = 17

    ... (one row per active clinic per table type)
________________________________________

5. BHGTaskRunner — How Appointments Are Orchestrated

File: BCAppCode/BHGTaskRunner/Program.cs

BHGTaskRunner.exe is run with argument 6 to process the Samms-Forms schedule, which
covers all assessment forms, appointments, and related clinical forms tables.

Command:   BHGTaskRunner.exe 6

Step 1 — Filter task queue for Samms-Forms
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"             // PHC uses a separate runner
        && x.Status == 17               // ready to run
        && x.TaskName == "Samms-Forms"
        && x.RunAt < DateTime.Now)

Step 2 — Mark parent task as running
    ptask.Status = 18  (running)

Step 3 — Load child tasks for this parent
    Tasks.Where(x => x.ParentTaskId == pt.TaskId)
         .OrderBy(o => o.TaskName)
         .ThenBy(b => b.SiteCode)
         .ThenBy(d => d.FromTblVw)

Step 4 — For each child task, build the SELECT statement
    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
                .Replace("@SiteCode", "'" + st.SiteCode + "'")
                .Replace("@Samms", "'SAMMS'")
    strCmd  = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw
    strWhere = st.WhereCondition (from dms.tbl_MapAction, with @SiteCode and @WorkDate substituted)
    DaysBack = -15

Step 5 — pats.tbl_Appointments path (case "pats.tbl_appointments")

    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveAppointments(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

    There is no Reload branch, no bulk path, and no RowTrax block for pats.tbl_Appointments
    in the current code. The WhereCondition and SortOrder come entirely from the task metadata
    stored in dms.tbl_MapAction for ActionKey=6 / DsnTbl='tbl_Appointments'.

Step 6 — pats.tbl_appointmentattend path (case "pats.tbl_appointmentattend")

    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveAppointmentAttend(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)

    Same single path as appointments. No bulk, no Reload, no RowTrax block.

Step 7 — Mark task complete
    task.Status   = 20     (completed)
    task.Duration = elapsed seconds
    task.RowCount = rows processed
    task.RowsIns  = rCodes.RowsIns
    task.RowsUpd  = rCodes.RowsUpd
    If rCodes.IsResult = false, task.ErrorMessage = rCodes.ExceptMsg
________________________________________

6. SQLSvrManager — Executing Against SAMMS

File: BCAppCode/BHG-DR-LIB/SQLSvrManager.cs

SQLSvrManager.GetTableData() opens an ADO.NET SqlConnection to the clinic's SAMMS SQL
Server, executes the assembled SELECT statement, and returns the result as a DataTable.

Connection string source: ctrl.tbl_LocationCons in Azure BHG_DR
    Each row contains:
        SiteCode   = 'B01'
        ConStr     = 'Server=clinic-sql01;Database=SAMMS_B01;User Id=...;Password=...;'

The source view/table name (st.FromTblVw) and schema (st.SrcSchema) are stored in the
task metadata row from dms.vw_MapAction. For appointments the source is typically the
SAMMS table dbo.tblAppointment (or its view), and for attendance dbo.tblAppointmentAttend,
with the exact name controlled by dms.tbl_MapAction.FromTblVw.

The DataTable returned is passed directly into SaveAppointments or SaveAppointmentAttend.
________________________________________

7. Source Tables — SAMMS SQL Server (dbo)

All source tables live in the clinic's local SAMMS SQL Server database under the dbo
schema. The exact table or view name is stored in dms.tbl_MapAction via st.FromTblVw.
Column lists are built by SelectConstructor reading dms.tbl_MapSrc2Dsn for ActionKey=6.

________________________________________
7a. dbo.tblAppointmentAttend — Appointment Attendance Records

Primary Key: aaID

Column Name         Type        Description
-----------         ----        -----------
aaID                int         Attendance record ID — primary identifier
aaaptID             int         Foreign key → appointment (links to tblAppointment.UniqueId)
aacltid             int         Client/patient ID. Negative value = deleted/voided record
                                (SAMMS soft-delete sentinel — triggers RowState=false in Azure)
aaDTENROLLED        date        Date the patient was enrolled/added to the appointment
aaDTREMOVED         date        Date the patient was removed from the appointment (discharge,
                                cancellation, or transfer). NULL if still enrolled.
SiteCode            varchar     Clinic site code — injected by SelectConstructor via @SiteCode
RowChkSum           int         CHECKSUM() over source row — computed by SelectConstructor

Note: The column name prefix 'aa' is the SAMMS naming convention for AppointmentAttend
table fields. All physical column names in the source retain this prefix.

________________________________________
7b. dbo.tblAppointment — Appointment Calendar Records

Primary Key: UniqueId

Column Name             Type        Description
-----------             ----        -----------
UniqueId                int         Appointment unique identifier — primary key
Type                    int         Appointment type code (maps to appointment category)
StartDate               datetime    Appointment start date/time
EndDate                 datetime    Appointment end date/time
AllDay                  bit/bool    Whether this is an all-day event
Subject                 varchar     Appointment subject/title
Location                varchar     Location where appointment takes place
Description             varchar     Free-text description of the appointment
Status                  int         Appointment status code
Label                   int         Label/colour code for calendar display
ResourceId              int         Primary resource (staff/room) ID
ResourceIds             varchar     Comma-separated list of additional resource IDs
ReminderInfo            varchar     Reminder configuration XML or text
RecurrenceInfo          varchar     Recurrence rule XML or text (for repeating appointments)
precentcomplete         int         Percent complete — NOTE: SAMMS source column name has a
                                    typo ('precentcomplete' missing the letter 'r'). Maps to
                                    PercentComplete property in Azure.
GroupName               int         Group identifier code
CustomField1            varchar     Custom extensible field 1
Attendees               bit/bool    Whether the appointment has attendees
Service                 varchar     Clinical service code
ServiceModifier         varchar     Service modifier code
TxtNote                 varchar     Free-text note attached to appointment
Area                    varchar     Clinic area or room designation
IntakeAppointmentMissed bit/bool    Flag indicating a missed intake appointment
SalesForceId            varchar     SalesForce CRM record ID (for CRM-linked appointments)
IsSalesForceSync        int         SalesForce sync status flag
IsThirdPartySync        int         Third-party system sync status flag
AppointmentType         varchar     Appointment type text label
IsDropIn                bit/bool    Whether this is a drop-in (walk-in) appointment
IsSchedule              bit/bool    Whether this is a scheduled (pre-booked) appointment
NoofParticipants        int         Maximum number of participants allowed
GroupTimeAllowed        int         Time allotted for group session (minutes)
GracePeriod             int         Grace period in minutes for late arrivals
SiteCode                varchar     Clinic site code — injected via @SiteCode
RowChkSum               int         CHECKSUM() over source row — computed by SelectConstructor
LastModAt               datetime    Timestamp — injected from SAMMS but overridden by runat
                                    (see LastModAt case in switch)
________________________________________

8. Destination Tables — Azure SQL BHG_DR

________________________________________
8a. pats.tbl_AppointmentAttend

Primary Key: SiteCode + aaID (composite, as confirmed in CREATE TABLE DDL)

Column Name     Type            Source Column       Notes
-----------     ----            -------------       -----
SiteCode        varchar(25)     @SiteCode           Injected; never NULL
RowState        bit             (computed)          true always; false when aacltid < 0
RowChkSum       int             RowChkSum / aaid    Read in "sitecode" case (eager) and
                                                    again in "rowchksum" case (final).
                                                    Second read overwrites first.
LastModAt       datetime        (runat)             Set to DateTime.Now at run start
AAId            int             aaid                Attendance record ID
aaaptID         int             aaaptid             Parent appointment ID (FK)
aacltid         int             aacltid             Client ID. Negative triggers RowState=false.
aaDTENROLLED   date            aadtenrolled        Enrollment date; skipped if length <= 5
aaDTREMOVED    date            aadtremoved         Removal date; skipped if length <= 5

________________________________________
8b. pats.tbl_Appointments

Primary Key: SiteCode + UniqueId (composite)

Column Name             Type            Source Column           Notes
-----------             ----            -------------           -----
SiteCode                varchar         @SiteCode               Injected
RowChkSum               int             rowchksum / sitecode    Set in both "sitecode" and
                                                                "rowchksum" cases
LastModAt               datetime        (runat)                 Set in "sitecode" case and
                                                                redundantly re-set in
                                                                "lastmodat" case; always runat
UniqueId                int             uniqueid                Defaults to -1 if empty
                                                                (not 0; unique to this method)
Type                    int             type                    Conditional parse; null if empty
StartDate               datetime        startdate               length > 5 guard; null if short
EndDate                 datetime        enddate                 length > 5 guard; null if short
AllDay                  bool            allday                  bool.Parse; null if empty
Subject                 varchar         subject                 Direct string assign
Location                varchar         location                Direct string assign
Description             varchar         description             Direct string assign
Status                  int             status                  Conditional parse
Label                   int             label                   Conditional parse
ResourceId              int             resourceid              Conditional parse
ResourceIds             varchar         resourceids             Direct string assign
ReminderInfo            varchar         reminderinfo            Direct string assign
RecurrenceInfo          varchar         recurrenceinfo          Direct string assign
PercentComplete         int             precentcomplete         ⚠ Source column 'precentcomplete'
                                                                is a SAMMS typo (missing 'r').
                                                                Maps to correctly-spelled property.
GroupName               int             groupname               Conditional parse
CustomField1            varchar         customfield1            Direct string assign
Attendees               bool            attendees               bool.Parse; null if empty
Service                 varchar         service                 Direct string assign
ServiceModifier         varchar         servicemodifier         Direct string assign
TxtNote                 varchar         txtnote                 Direct string assign
Area                    varchar         area                    Direct string assign
IntakeAppointmentMissed bool            intakeappointmentmissed bool.Parse; null if empty
SalesForceId            varchar         salesforceid            Direct string assign
IsSalesForceSync        int             issalesforcesync        Conditional parse
IsThirdPartySync        int             isthirdpartysync        Conditional parse
AppointmentType         varchar         appointmenttype         Direct string assign
IsDropIn                bool            isdropin                bool.Parse; null if empty
IsSchedule              bool            isschedule              bool.Parse; null if empty
NoofParticipants        int             noofparticipants        Conditional parse
GroupTimeAllowed        int             grouptimeallowed        Conditional parse
GracePeriod             int             graceperiod             Conditional parse

Note: pats.tbl_Appointments does NOT have a RowState column. There is no soft-delete
tracking for the appointment calendar records — only for attendance records.
________________________________________

9. EF Core Upsert Pattern — Common to Both Methods

Both SaveAppointments and SaveAppointmentAttend follow the same structural pattern:

Step 1 — Initialize RCodes
    rc.IsResult        = true
    rc.RowsProcessed   = tbl.Rows.Count

Step 2 — Open or reuse BHG_DRContext
    if (db == null) { db = new Models.BHG_DRContext(); }

Step 3 — Load all Azure rows for this site into memory
    SaveAppointmentAttend:
        dbAA = db.TblAppointmentAttends.Where(x => x.SiteCode == sc).ToList()
    SaveAppointments:
        dbapts = db.TblAppointments.Where(x => x.SiteCode == sc).ToList()

Step 4 — Initialize new-record staging list
    SaveAppointmentAttend:  xnAA = new List<TblAppointmentAttend>()
    SaveAppointments:       napts = new List<TblAppointments>()

Step 5 — Iterate over each DataRow from the SAMMS DataTable

    Step 5a — Create a fresh model instance per row
    Step 5b — Iterate over each DataColumn with a per-field try/catch (see Section 11)
    Step 5c — Switch on c.ColumnName.ToLower() to map fields
    Step 5d — Look up existing Azure record using composite key
        SaveAppointmentAttend:
            xapt = dbAA.FirstOrDefault(x => x.SiteCode == xappt.SiteCode && x.AAId == xappt.AAId)
        SaveAppointments:
            xapt = dbapts.FirstOrDefault(x => x.SiteCode == xappt.SiteCode && x.UniqueId == xappt.UniqueId)

    Step 5e — Upsert decision
        If null  → new record: add to staging list, increment rc.RowsIns
        If found → existing record: overwrite all mapped fields, increment rc.RowsUpd

Step 6 — First SaveChanges (commits all updates)
    db.SaveChanges()
    This flushes all field-level assignments made to existing tracked EF entities.

Step 7 — Insert new records (AddRange + second SaveChanges)
    If staging list has entries:
        db.TblAppointmentAttends.AddRange(xnAA) / db.TblAppointments.AddRange(napts)
        db.SaveChanges()
    This batch-inserts all new rows in a single round trip.

Step 8 — Outer exception handling
    If any unhandled exception escapes the loop:
        rc.IsResult     = false
        rc.ExceptMsg    = e.Message
        rc.ExceptInnerMsg = e.InnerException.Message (if present)
________________________________________

10. Method-Specific Load Logic

________________________________________
10a. SaveAppointmentAttend — Detailed Column Mapping

Method signature:
    public Models.RCodes SaveAppointmentAttend(DataTable tbl, string sc, DateTime wrkdt,
                                               Models.BHG_DRContext db)

EF entity: Models.TblAppointmentAttend
DbSet:      db.TblAppointmentAttends
Lookup key: composite SiteCode + AAId

"sitecode" case — infrastructure block (runs first due to column ordering):
    xappt.SiteCode   = dr[c.ColumnName].ToString()   // actual value from DataTable
    xappt.LastModAt  = runat                          // fixed run timestamp
    xappt.RowChkSum  = int.Parse(dr["RowChkSum"].ToString())  // eager read of RowChkSum
                                                               // by column name, not c.ColumnName
    xappt.RowState   = true                           // default: active record

    Note: RowChkSum is read here eagerly (before the "rowchksum" case fires) to ensure it
    is populated even if column ordering puts RowChkSum after sitecode in the DataTable.

"aaid" case:
    xappt.AAId = int.Parse(...)   — conditional (length > 0 guard)

"aaaptid" case:
    xappt.aaaptID = int.Parse(...) — conditional, sets the parent appointment FK

"aacltid" case:
    xappt.aacltid = int.Parse(...)   — conditional
    if (xappt.aacltid < 0) { xappt.RowState = false; }
    Negative client IDs are SAMMS's sentinel value for a deleted or invalid patient.
    This is the only condition under which RowState can become false for attendance records.

"rowchksum" case:
    xappt.RowChkSum = int.Parse(dr[c.ColumnName].ToString())
    This overwrites the value already set in "sitecode". Since both read the same column
    value, the result is identical. This dual-read is harmless but redundant.

"aadtenrolled" case:
    xappt.aaDTENROLLED = DateTime.Parse(...)  — conditional (length > 5 guard)

"aadtremoved" case:
    xappt.aaDTREMOVED = DateTime.Parse(...)   — conditional (length > 5 guard)
    A populated aaDTREMOVED means the patient was removed from this appointment slot.
    The row remains in Azure (soft data retention) but RowState may be false if aacltid < 0.

Update path (existing record found):
    All mapped fields are copied from the incoming model to the EF-tracked entity.
    No RowChkSum skip guard — the update always executes regardless of whether the
    checksum changed. Every matching row is overwritten on every run.

Fields written on update:
    aaaptID, aacltid, aaDTENROLLED, aaDTREMOVED, RowChkSum, LastModAt, RowState

________________________________________
10b. SaveAppointments — Detailed Column Mapping

Method signature:
    public Models.RCodes SaveAppointments(DataTable tbl, string sc, DateTime wrkdt,
                                          Models.BHG_DRContext db)

EF entity: Models.TblAppointments
DbSet:      db.TblAppointments
Lookup key: composite SiteCode + UniqueId

"sitecode" case — infrastructure block:
    xappt.SiteCode   = dr[c.ColumnName].ToString()
    xappt.LastModAt  = runat
    xappt.RowChkSum  = int.Parse(dr["RowChkSum"].ToString())   // eager RowChkSum read
    Note: No RowState assignment here — TblAppointments has no RowState column.

"uniqueid" case:
    if (dr[c.ColumnName].ToString().Length > 0)
        xappt.UniqueId = int.Parse(...)
    else
        xappt.UniqueId = -1    // ⚠ Default is -1, not 0. Unique to this method.
    An empty UniqueId means the source row has no valid primary key. The -1 sentinel
    ensures it is not confused with ID=0 records.

"lastmodat" case:
    xappt.LastModAt = runat
    This is redundant — the same assignment is already made in the "sitecode" case.
    Result is identical. Harmless.

"rowchksum" case:
    xappt.RowChkSum = int.Parse(...)
    Overwrites the value set in "sitecode". Harmless, same value.

Date fields — "startdate" and "enddate":
    Both use length > 5 guard before DateTime.Parse.
    A value with length <= 5 (e.g. empty or very short string) is skipped; the property
    remains null.

Boolean fields — "allday", "attendees", "intakeappointmentmissed", "isdropin", "isschedule":
    All use bool.Parse() with a length > 0 guard.

"precentcomplete" case:
    xappt.PercentComplete = int.Parse(...)   (conditional)
    ⚠ The source column name is 'precentcomplete' — this is a SAMMS database column name
    typo (the word 'percent' is missing the letter 'r'). The EF property is correctly
    spelled as PercentComplete. The case statement preserves the SAMMS typo verbatim to
    match what is actually in the DataTable column header.

"groupname" case:
    xappt.GroupName = int.Parse(...)
    Despite the name sounding like a text field, GroupName is stored as an int code.

SalesForce and third-party sync fields:
    SalesForceId    — varchar, direct string assign
    IsSalesForceSync  — int code (not bool)
    IsThirdPartySync  — int code (not bool)
    These fields support integration with SalesForce CRM and other external platforms.
    The int flag pattern (rather than bool) allows multiple sync states beyond on/off.

Update path (existing record found):
    All 26+ mapped fields are copied from the incoming model to the EF-tracked entity.
    No RowChkSum skip guard — the update always executes, every row is overwritten.
    RowState is not touched (column does not exist on TblAppointments).

Fields written on update (all 26):
    RowChkSum, LastModAt, Type, StartDate, EndDate, AllDay, Subject, Location,
    Description, Status, Label, ResourceId, ResourceIds, ReminderInfo, RecurrenceInfo,
    PercentComplete, GroupName, CustomField1, Attendees, Service, ServiceModifier,
    TxtNote, Area, IntakeAppointmentMissed, SalesForceId, IsSalesForceSync,
    IsThirdPartySync, AppointmentType, IsDropIn, IsSchedule, NoofParticipants,
    GroupTimeAllowed, GracePeriod
________________________________________

11. Change Detection (RowChkSum)

Both methods use RowChkSum but neither uses it as a skip guard.

How RowChkSum is computed:
    SelectConstructor builds a CHECKSUM(...) expression over all enabled source columns
    for ActionKey=6 / ActionStepKey=(step for the appointment table). This computed value
    is added to the SELECT as the column "RowChkSum". BHGTaskRunner passes ChkSumEnabled=true
    for ActionKey=6 (only ActionKey=3 disables checksum; see Program.cs line 118).

How RowChkSum is stored:

    SaveAppointmentAttend:
        Read #1 in "sitecode" case: xappt.RowChkSum = int.Parse(dr["RowChkSum"].ToString())
        Read #2 in "rowchksum" case: xappt.RowChkSum = int.Parse(dr[c.ColumnName].ToString())
        Both reads produce the same value. The second overwrites the first.

    SaveAppointments:
        Read #1 in "sitecode" case: xappt.RowChkSum = int.Parse(dr["RowChkSum"].ToString())
        Read #2 in "rowchksum" case: xappt.RowChkSum = int.Parse(dr[c.ColumnName].ToString())
        Same dual-read pattern.

What is NOT done:
    Neither method compares the incoming RowChkSum against the existing Azure RowChkSum.
    There is no code such as:
        if (xapt.RowChkSum == xappt.RowChkSum) { continue; }
    As a result, every matching row found in Azure is overwritten on every ETL run,
    even if the source data has not changed since the last run.

Implication:
    RowChkSum is stored in Azure as an auditing column only. It could be used in future
    to implement a skip guard to reduce unnecessary writes, but currently it is not.
________________________________________

12. RowState — Soft Delete

SaveAppointmentAttend implements RowState. SaveAppointments does not.

SaveAppointmentAttend RowState behavior:

    Default: RowState = true is set unconditionally in the "sitecode" case every run.
    This resets the flag to active for every row arriving from SAMMS.

    Override: in the "aacltid" case —
        if (xappt.aacltid < 0) { xappt.RowState = false; }

    SAMMS uses a negative client ID to signal that a patient record has been deleted or
    voided. When SAMMS marks a patient as deleted, the related appointment attendance rows
    carry a negative aacltid value. The ETL captures this and stores RowState=false in
    Azure, logically deleting the attendance record without physically removing it.

    This RowState is stored on update:
        xapt.RowState = xappt.RowState
    So even an existing Azure row can have its RowState flipped from true to false if the
    patient's aacltid becomes negative in SAMMS.

SaveAppointments RowState behavior:

    TblAppointments has no RowState column. There is no soft-delete mechanism for
    the appointment calendar records. If an appointment is cancelled or deleted in SAMMS,
    the Azure record will simply be updated (overwritten) with whatever state the source
    row carries. There is no way for the ETL to mark a pats.tbl_Appointments row as
    logically deleted via RowState.
________________________________________

13. Load Scoping — Azure Query Window

Both methods load all Azure rows for the site into memory without any date filter:

    SaveAppointmentAttend:
        db.TblAppointmentAttends.Where(x => x.SiteCode == sc).ToList()

    SaveAppointments:
        db.TblAppointments.Where(x => x.SiteCode == sc).ToList()

This means the in-memory lookup list contains every row ever loaded for that site.
For sites with large historical appointment volumes, this can result in a sizeable
in-memory footprint before the per-row loop begins.

The wrkdt parameter (WorkDate) is accepted by both method signatures but is NOT used
inside either method to filter the Azure load or the upsert logic. The WhereCondition
in the SAMMS SELECT (from dms.tbl_MapAction) controls which source rows are extracted —
not the method itself.
________________________________________

14. Error Handling

Both methods use two tiers of error handling:

Tier 1 — Per-field try/catch (inner loop)

    foreach (DataColumn c in tbl.Columns)
    {
        try
        {
            switch (c.ColumnName.ToLower())
            { ... }
        }
        catch (Exception e)
        {
            Console.WriteLine(c.ColumnName.ToString() + " : " + dr[c.ColumnName].ToString());
        }
    }

    If a single field fails to parse (e.g. a date field containing an unexpected format,
    or an int field containing a blank string that slips past the length guard), the
    exception is caught silently. The field is skipped, the value remains at its C# default
    (null or 0), and processing continues with the next column. The failed column name and
    its raw value are written to Console (stdout/log).

    This makes both methods fault-tolerant at the field level. A bad value in one column
    cannot prevent the rest of the row from being processed.

Tier 2 — Method-level try/catch (outer block)

    Wraps the entire load operation (Azure query, foreach row loop, SaveChanges calls).
    If an exception propagates out of the inner loop or from SaveChanges itself:
        rc.IsResult       = false
        rc.ExceptMsg      = e.Message
        rc.ExceptInnerMsg = e.InnerException.Message  (if InnerException is present)

    The rc object is returned to BHGTaskRunner, which records the error in:
        task.ErrorMessage = rCodes.ExceptMsg
        task.Status = 20 (completed with error)
________________________________________

15. Known Anomalies and Code Quirks

1. RowChkSum dual-read (both methods)
   RowChkSum is set twice per row — once in the "sitecode" case via dr["RowChkSum"] (by
   column name directly) and once in the "rowchksum" case via dr[c.ColumnName]. Both
   reads return the same integer value. The second write is redundant. This is a harmless
   pattern repeated across several ETL methods in this codebase, likely originating from
   copy-paste.

2. SAMMS column name typo: 'precentcomplete' (SaveAppointments only)
   The SAMMS source database column for percent completion is named 'precentcomplete' —
   missing the letter 'r' in 'percent'. The case statement in SaveAppointments reads this
   literally: case "precentcomplete". The EF model property is correctly named
   PercentComplete. This typo must be preserved in the case statement as long as the SAMMS
   source column is not renamed.

3. UniqueId defaults to -1 when empty (SaveAppointments only)
   Most EF methods default missing int primary key values to 0. SaveAppointments sets
   UniqueId = -1 when the source value is empty. The -1 sentinel is unusual and could
   theoretically match another -1 record in the Azure load set (from a prior run where
   another empty source row produced UniqueId=-1). In practice, appointments with no
   UniqueId are not valid and would likely cause a lookup collision rather than an insert.

4. Redundant LastModAt assignment (SaveAppointments only)
   "lastmodat" case sets xappt.LastModAt = runat. The same assignment is already made
   in the "sitecode" case. The result is always runat (DateTime.Now at run start).
   The second assignment is harmless but unnecessary.

5. No RowState on TblAppointments
   Unlike AppointmentAttend, the appointment calendar table has no soft-delete mechanism.
   Deleted or cancelled appointments in SAMMS are not distinguishable in Azure via RowState.

6. No RowChkSum skip guard (both methods)
   Despite calculating and storing RowChkSum, neither method uses it to skip unchanged
   rows. All existing Azure rows are overwritten on every run. This is consistent with
   the "always-update" pattern used across Group A methods in SaveAssessments.cs.
________________________________________

16. Flow Diagrams

________________________________________
16a. SaveAppointmentAttend — Row Processing Flow

SAMMS DataTable row arrives
         |
         v
Create new TblAppointmentAttend instance
         |
         v
Iterate over each DataColumn (per-field try/catch wraps switch)
         |
    +---------+-----------+-----------+-----------+-----------+-----------+
    |         |           |           |           |           |           |
"sitecode" "aaid"    "aaaptid"   "aacltid"   "rowchksum" "aadten-  "aadtre-
SiteCode=  AAId=      aaaptID=    aacltid=    RowChkSum=  rolled"   moved"
dr value   int.Parse  int.Parse   int.Parse   int.Parse   DateTime  DateTime
LastModAt= (if len>0) (if len>0)  (if len>0)  (overwrites (if len>5)(if len>5)
runat                             if <0:      sitecode
RowChkSum=                        RowState=   value)
dr["Row-                          false
ChkSum"]
RowState=
true
    |
    v
Lookup in dbAA:
  FirstOrDefault(SiteCode == sc && AAId == AAId)
         |
    +----+----+
    |         |
  null      found
    |         |
  RowsIns++ RowsUpd++
  Add to    Overwrite all fields:
  xnAA      aaaptID, aacltid,
  list      aaDTENROLLED,
            aaDTREMOVED,
            RowChkSum,
            LastModAt,
            RowState
         |
         v
(after all rows)
db.SaveChanges()    — commits updates
if xnAA.Count > 0:
  db.TblAppointmentAttends.AddRange(xnAA)
  db.SaveChanges()  — batch inserts new records

________________________________________
16b. SaveAppointments — Row Processing Flow

SAMMS DataTable row arrives
         |
         v
Create new TblAppointments instance
         |
         v
Iterate over each DataColumn (per-field try/catch wraps switch)
         |
    +----------+----------+----------+----------+----------+-----------+
    |          |          |          |          |          |           |
"sitecode" "uniqueid"  "lastmodat" "rowchksum" "type"   "startdate"  ... (24 more)
SiteCode=  UniqueId=   LastModAt=  RowChkSum=  Type=    StartDate=
dr value   int.Parse   runat       int.Parse   int      DateTime
LastModAt= if empty:   (redundant) (overwrites (if      (if len>5)
runat      -1 sentinel             sitecode)   len>0)
RowChkSum=
dr["Row-
ChkSum"]
(No RowState)
         |
         v
Lookup in dbapts:
  FirstOrDefault(SiteCode == sc && UniqueId == UniqueId)
         |
    +----+----+
    |         |
  null      found
    |         |
  RowsIns++ RowsUpd++
  Add to    Overwrite all 26+ fields
  napts     (RowChkSum, LastModAt,
  list       Type, StartDate, EndDate,
             AllDay, Subject, Location,
             Description, Status, Label,
             ResourceId, ResourceIds,
             ReminderInfo, RecurrenceInfo,
             PercentComplete, GroupName,
             CustomField1, Attendees,
             Service, ServiceModifier,
             TxtNote, Area,
             IntakeAppointmentMissed,
             SalesForceId,
             IsSalesForceSync,
             IsThirdPartySync,
             AppointmentType, IsDropIn,
             IsSchedule, NoofParticipants,
             GroupTimeAllowed, GracePeriod)
         |
         v
(after all rows)
db.SaveChanges()    — commits updates
if napts.Count > 0:
  db.TblAppointments.AddRange(napts)
  db.SaveChanges()  — batch inserts new records

________________________________________
16c. End-to-End Pipeline (both tables, one clinic)

Scheduler.exe (daily)
    Inserts: parent task 'Samms-Forms' (Status=17)
    Inserts: child task 'pats.tbl_Appointments' / SiteCode='B01' (Status=17)
    Inserts: child task 'pats.tbl_AppointmentAttend' / SiteCode='B01' (Status=17)
         |
         v
BHGTaskRunner.exe 6
    Picks up parent 'Samms-Forms' (Status 17 → 18)
    Loops child tasks ordered by TaskName, SiteCode
         |
         v
    For 'pats.tbl_appointments' / B01:
        Build SELECT from dms.tbl_MapSrc2Dsn (ActionKey=6, step for tbl_Appointments)
        Add WhereCondition from dms.tbl_MapAction
        sm.GetTableData(FromTblVw, strCmd, ConStr) → DataTable
        sd.SaveAppointments(DataTable, 'B01', WorkDate, null)
             |
             v
        EF Core upsert → pats.tbl_Appointments
        Return RCodes → task.Status=20, RowsIns/Upd recorded
         |
         v
    For 'pats.tbl_appointmentattend' / B01:
        Same SELECT build
        sm.GetTableData(FromTblVw, strCmd, ConStr) → DataTable
        sd.SaveAppointmentAttend(DataTable, 'B01', WorkDate, null)
             |
             v
        EF Core upsert → pats.tbl_AppointmentAttend
        Return RCodes → task.Status=20, RowsIns/Upd recorded
         |
         v
    Repeat for next SiteCode
         |
         v
    All child tasks complete → parent task Status=20
________________________________________

17. File Reference Map

File Path                                               Purpose
---------                                               -------
BCAppCode/Scheduler/Program.cs                          Creates daily task queue — inserts Samms-Forms tasks
                                                        including pats.tbl_Appointments and
                                                        pats.tbl_AppointmentAttend child rows per clinic
BCAppCode/BHGTaskRunner/Program.cs                      Main ETL driver (arg=6 → Samms-Forms pipeline)
                                                        Contains case "pats.tbl_appointments" and
                                                        case "pats.tbl_appointmentattend" dispatch blocks
                                                        Builds WHERE clause from dms.tbl_MapAction metadata
BCAppCode/BHG-DR-LIB/SaveAppointments.cs               EF Core upsert — SaveAppointments + SaveAppointmentAttend
BCAppCode/BHG-DR-LIB/SelectConstructor.cs              Builds SELECT + CHECKSUM() expression from
                                                        dms.tbl_MapSrc2Dsn (ActionKey=6)
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                  ADO.NET wrapper — executes SQL against clinic SAMMS
BCAppCode/BHG-DR-LIB/Models/TblAppointments.cs         EF Model → pats.tbl_Appointments
BCAppCode/BHG-DR-LIB/Models/TblAppointmentAttend.cs    EF Model → pats.tbl_AppointmentAttend
BCAppCode/BHG-DR-LIB/BHG_DRContext.cs                  EF DbContext — registers TblAppointments and
                                                        TblAppointmentAttends DbSets
________________________________________

18. Quick Reference Summary

What triggers Appointment ETL?        Scheduler.exe creates tasks, BHGTaskRunner.exe 6 processes them
TaskName in scheduler?                Samms-Forms
Source tables in SAMMS?               dbo.tblAppointment (or clinic view via FromTblVw)
                                      dbo.tblAppointmentAttend
Destination tables in Azure?          pats.tbl_Appointments
                                      pats.tbl_AppointmentAttend
EF Core or Bulk path?                 Both methods use EF Core only — no bulk/staging path
Schedule number?                      6 — BHGTaskRunner.exe 6
ActionKey in dms.tbl_MapSrc2Dsn?      6
Primary key — tbl_Appointments?       SiteCode + UniqueId (composite)
Primary key — tbl_AppointmentAttend?  SiteCode + AAId (composite)
Change detection?                     RowChkSum is stored in both methods but NO skip guard —
                                      all existing rows are always overwritten unconditionally
What is RowState?                     Active/inactive bit flag — only on tbl_AppointmentAttend
                                      tbl_Appointments has NO RowState column
How is RowState set?                  AppointmentAttend: RowState=true by default;
                                      RowState=false when aacltid < 0 (deleted patient sentinel)
Azure load scope (both methods)?      SiteCode == sc only — full site load, no date filter applied
                                      inside either method
wrkdt parameter used?                 Accepted by both methods but not used inside either
Dual RowChkSum read?                  Yes — both methods set RowChkSum in "sitecode" case and
                                      again in "rowchksum" case. Second overwrites first; harmless.
UniqueId default when empty?          -1 (SaveAppointments only — not 0 like most other methods)
Known SAMMS column name typo?         'precentcomplete' → PercentComplete (missing 'r' in 'percent')
                                      SaveAppointments only — case statement preserves typo verbatim
Redundant field assignment?           SaveAppointments "lastmodat" case re-sets LastModAt = runat;
                                      same assignment already made in "sitecode" case
Per-row vs batch commit?              Both methods: updates committed first (SaveChanges), then
                                      new rows batch-inserted (AddRange + SaveChanges)
Per-field error handling?             Yes — both methods wrap the inner column switch in try/catch;
                                      failed fields logged to Console, row processing continues
RowTrax audit?                        Not present for Appointments in current BHGTaskRunner code
Reload / hard-delete path?            Not present — no st.Reload branch for either appointments table
PHC handled here?                     No — PHC excluded by x.SiteCode != "PHC" filter
                                      in BHGTaskRunner parent task query
SalesForce integration?               SaveAppointments: SalesForceId (varchar), IsSalesForceSync (int),
                                      IsThirdPartySync (int) — stored for CRM sync tracking
Recurrence support?                   RecurrenceInfo (varchar) stores recurrence rule XML from SAMMS
                                      for repeating appointment series
________________________________________

Documentation generated from source: BHG-DR-LIB\SaveAppointments.cs (350 lines, 2 methods)
Generated: April 2026
Parent Schedule: Samms-Forms (Schedule 6 — BHGTaskRunner.exe 6)
