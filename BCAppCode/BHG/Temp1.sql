CREATE TABLE [pats].[tbl_AppointmentAttend] (
	[SiteCode] [varchar](25) NOT NULL,
	[RowState] [bit] NOT NULL,
	[RowChkSum] [int] NULL,
	[LastModAt] [datetime] NULL,
    [aaID]         INT  NOT NULL,
    [aaaptID]      INT  NULL,
    [aacltid]      INT  NULL,
    [aaDTENROLLED] DATE NULL,
    [aaDTREMOVED]  DATE NULL
 CONSTRAINT [PK_AppointmentAttend] PRIMARY KEY CLUSTERED 
(
	[SiteCode] ASC,
	[aaID] ASC
));










select [Region], [sitecode], [statecode], [ClinicName], [PatientID], [clientid], [first], [last]
     , [formid], [formname], [tcode], [true14enrolldate], [Duration], [Counselor], [proggroup], [Program], [Payor]
     , [finClass], [formdate], [DayspastTP], [ClientSignDate], [CounselorSignDate], [DrSignDate], [SuprvSignDate], [NextTPdue]
     , [TPInterval], [TP_Late], [TP_DueSoon] 
  from pats.vw_Treatment_Plan order by 1, 2, 3, clientid, Program, formdate




declare @dt date = getdate()-1;
insert into [pats].[tbl_Vw_TrendingCounseling_StateReq] 
       (ActiveDate, [Location], ProgGroup, cltID, PatientID, FinClass, PayorType, PatientCount, CodeLevel, StateCode
       , LOS, [Sessions], Unit, Frequency, IndividualMin, GroupMin, BHGStrict, Individual, Groups, LastTPDate, TP_Late
       , GroupUnits, IndividualUnits, UpToDate, UpToDateTrue) Exec pats.SP_CounselingStateReq @dt

declare @dt date = getdate()-1;
Exec pats.SP_CounselingStateReq @dt

/*
select *, DiffCnt = AzureCnt - SammsCnt from tsk.tbl_RowTrax where tblName = 'pats.tbl_Dose' and RCDate >= '5/18/2025' order by SiteCode

select SiteCode, dtMedDate, count(1) from pats.tbl_Dose where SiteCode = 'V21' and CltID > 0  group by SiteCode, dtMedDate
order by 1, 2 desc

select SiteCode, count(1) from pats.tbl_Dose where RowState = 1 group by SiteCode, DoseID having Count(DoseID) > 1

select * from tsk.Tbl_Tasks2 where TaskName = 'pats.tbl_LiquidLog' and WorkDate = '3/15/2025'

select siteCode, count(1) xRows from pats.tbl_3pARNOTE group by SiteCode order by 1
--select * from pats.tbl_3pARNOTE where SiteCode = 'B24' order by arnDATE 
--delete from pats.tbl_3pARNOTE where arnDate < '1/1/2023'
--update tsk.tbl_Tasks2 set status = 20 where TaskId = 2413893 or ParentTaskId = 2413893
--WorkDate = '11/12/2024' and status = 17
select top 10 TaskId, TaskName, Status, LastModAt, SiteCode, [RowCount], RowsIns, RowsUpd, Duration, ErrorMessage
from tsk.tbl_Tasks2 where WorkDate >= '11/25/2024' --and Status = 19 and TaskName in ('pats.tbl_3pArnote', 'pats.tbl_3pClaimNote')
order by LastModAt desc
--update dms.tbl_MapAction set WhereCondition = 'dtm >= @WorkDate' where ActionKey = 1 and StepKey = 38

select * from pats.tbl_Bottle where SiteCode = 'V21'
select SiteCode, Count(1) from pats.tbl_Bottle group by SiteCode
select SiteCode, Count(1) from pats.tbl_LiquidLog group by SiteCode
select * from ctrl.tbl_InvType where SiteCode = 'B25'
select * from dms.tbl_MapSrc2Dsn where ActionKey = 1 and ActionStepKey = 40
select * from ctrl.tbl_Forms2Process where Enabled = 1
--update ctrl.tbl_Forms2Process set [DateFilterEnabled] = 1 where FormProcessId = 47
--update ctrl.tbl_Forms2Process set Enabled = 0 where DateFilterEnabled = 0
--order by [RowCount] desc
/*
select * from tsk.tbl_Schedule where enabled = 1
insert into tsk.tbl_Schedule (ScheduleId, Enabled, RowState, LastModBy, LastModAt, Name, TriggerKey, ActionKey, NextRunTime, LastRunTime)
values (25, 1, 24, 'Brian.Catellier', GetDate(), 'SAMMS-ETL-DartSvc', 1, 1, '2025-6-4 20:07:00.000', '2025-6-4 20:07:00.000'),
(26, 1, 24, 'Brian.Catellier', GetDate(), 'SAMMS-ETL-Dose', 1, 1, '2025-6-4 20:08:00.000', '2025-6-4 20:08:00.000')

truncate table pats.tbl_ServicesMissingSigCode;
Insert into pats.tbl_ServicesMissingSigCode ([patientid], [clientId], [firstname], [LastName], [Status], [SiteCode] 
 , [SiteID], [prog], [counselor], [dsTxtSrv], [dstxtStaff], [dsTxtType], [dsDtAdded], [dsDtStart], [dsDtEnd] 
 , [dsdblUnits], [dsUpdate], [dsUPDATEStaff], [dstxtNote], [dsdbnotes], [dsGROUPNUM], [dsArea] 
 , [dsID], [dsSigDate], [dsbilled], [StaffName], [StaffActive]) 
SELECT [patientid], [clientId], [firstname], [LastName], [Status], [SiteCode], [SiteID], [prog]
     , [counselor], [dsTxtSrv], [dstxtStaff], [dsTxtType], [dsDtAdded], [dsDtStart], [dsDtEnd] 
     , [dsdblUnits], [dsUpdate], [dsUPDATEStaff], [dstxtNote], [dsdbnotes], [dsGROUPNUM], [dsArea] 
     , [dsID], [dsSigDate], [dsbilled], [StaffName], [StaffActive] FROM [pats].[ServicesMissingSigCode] 


select WhereCondition from dms.tbl_MapAction where DsnTbl = 'tbl_AdmissionAssessment'
update dms.tbl_MapAction set WhereCondition = '(CreatedOn > = @workDate or ModifiedOn >= @WorkDate' where DsnTbl = 'Tbl_AdmissionAssessment'
--update tsk.tbl_Tasks2 set WorkDate = '12/31/2019' where status = 17 and TaskName = 'pats.tbl_Dose'
select * from pats.tbl_AdmissionAssessment
select * from pats.tbl_ReAssessment
*/

Exec pats.SP_CounselingStateReq
*/