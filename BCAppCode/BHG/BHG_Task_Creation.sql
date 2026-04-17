--Data Source=na-bhgpst01.netalytics-cloud.com,51433;Persist Security Info=True;User ID=bhg;Password=3pen-lone-Eagle4;Encrypt=False;Initial Catalog=Methasoft_CBH_DesertInn;
--select * from pats.tbl_DartsSrv_2021 where dsID = 1333885 order by SiteCode
--update pats.tbl_DartsSrv_2021 set RowState = 0 where dsID = 1333885 and SiteCode = 'B24'
--update tsk.tbl_Tasks2 set Status = 17 where WorkDate = '12/30/2022' and TaskId in (470296, 470295, 470297, 470298)
--update tsk.tbl_Tasks2 set Status = 17 where WorkDate = '12/30/2022' and ParentTaskId in (470296, 470295, 470297, 470298) and TaskName = 'pats.tbl_UAResults' and Status = 20
--select * from tsk.tbl_Tasks2 where ParentTaskId in (454344, 454343, 454345, 454346) and Status in (17, 18,19,20) and TaskName = 'pats.tbl_Dose'
--select top 1000 * from pats.tbl_BriefAddictionMonitor where SiteCode = 'TTCB'
--select * from tsk.tbl_Tasks2 where TaskName = 'pats.tbl_FormsSAMMSClient'
/*
The instance of entity type 'TblDboFormQuestionAnswers' cannot be tracked because another instance with the key value '{SiteCode: D07, FormName: HITS Tool for Intimate Partner Violence Screening, FormId: 1e19433d-4488-4998-bd6d-6db59fb6b610, ClientId: 7864, QuestionId: 2830, QuestionOrderId: 0}' is already being tracked. When attaching existing entities, ensure that only one entity instance with a given key value is attached.     
insert into tsk.tbl_Tasks2 (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, Duration, OnCompletion, OnError, LastModAt, LastModBy, ParentTaskId, SiteCode, WorkDate) 
values ('pats.tbl_BriefAddictionMonitor', '2022-07-05 20:01:00.000', 2, 9, 17, 24, 0, 0, 0, GetDate(), 'Brian.Catellier', 135120, 'Global', '05/25/2022'),
       ('pats.tbl_BriefAddictionMonitor', '2022-07-05 20:01:00.000', 2, 9, 17, 24, 0, 0, 0, GetDate(), 'Brian.Catellier', 135120, 'Global', '12/10/2019'),
       ('pats.tbl_BriefAddictionMonitor', '2022-07-05 20:01:00.000', 2, 9, 17, 24, 0, 0, 0, GetDate(), 'Brian.Catellier', 135120, 'Global', '3/16/2020'),
       ('pats.tbl_BriefAddictionMonitor', '2022-07-05 20:01:00.000', 2, 9, 17, 24, 0, 0, 0, GetDate(), 'Brian.Catellier', 135120, 'Global', '9/10/2019')
*/
--update tsk.tbl_Tasks2 set ParentTaskId = null where TaskId = 22821
/*
declare @ErrDate date = '11/9/2021';
insert into tsk.tbl_Tasks2 (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError)
select 'Error ReLoad', RunAt, ActionKey, 0, 17, 24, GetDate(), LastModBy, 'Errors', WorkDate, 0, 0, 0 from tsk.tbl_Tasks2 where WorkDate = @ErrDate and Status in (17, 18, 20)
declare @ErrDate date = '11/9/2021';
update tsk.tbl_Tasks2 set ParentTaskId = (select TaskId from tsk.tbl_Tasks2 where WorkDate = @ErrDate and ParentTaskId is null and Status = 17 and TaskId = 22821), Status = 17
 where WorkDate = @ErrDate and Status in (17, 20) and TaskId <> 22821
select * from tsk.vwTaskList where WorkDate = @ErrDate

--update tsk.tbl_Schedule set Enabled = 0 where ScheduleId = 12
update tsk.tbl_Schedule set NextRunTime = '10/28/2021 20:00:00' where Enabled = 1
update tsk.tbl_Schedule set Enabled = 1 where ScheduleId = 5
update dms.tbl_MapAction set Enabled = 1 where ActionKey = 4
update dms.tbl_MapAction set Enabled = 0 where Dsn
select * from ctrl.tbl_Connections
select * from dms.tbl_MapAction where Enabled = 1

select * from tsk.tbl_Schedule where Enabled = 1
insert into tsk.tbl_Schedule (ScheduleId, Enabled, Name, ActionKey, TriggerKey, NextRunTime, LastRunTime, LastModAt, LastModBy, RowState) 
  values (17, 1, 'B12B DartsSrv', 0, 1, '2022-1-1 20:00:00.000', '2022-1-1 20:00:00.000', GetDate(), 'Brian.Catellier', 24);


select * from tsk.tbl_Tasks2 where convert(date,RunAt) >= convert(date,GetDate()-1) and Status in (17,20) and RowState = 24
select * from tsk.vw_Schedule where Enabled = 1
insert into tsk.tbl_Tasks2 (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError)
--values ('PHC ETL', '2/5/2022 16:04:00', 0, 0, 17, 24, GetDate(), 'Brian.Catellier', 'ALL', '2/5/2022', '0', 0, 0)
select Name, NextRunTime, ActionKey, 0, 17, 24, GetDate(), 'Brian.Catellier', Case when scheduleid = 18 then 'PHC' else 'All' end, Convert(date, NextRunTime), '0', 0, 0 
  from tsk.tbl_Schedule where Enabled = 1 --and ActionKey <> 2

insert into tsk.tbl_Tasks2 (ParentTaskId, TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError)
select t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, t.RunAt,  ma.ActionKey, ma.StepKey, 17, 24, GetDate(), 'Brian.Catellier', ma.SiteCode, t.WorkDate, '0', 0, 0
  from dms.vw_MapAction ma cross join tsk.tbl_Tasks2 t
 where ma.Enabled = 1 and ma.IsActive = 1 and ConnectionID <> 3 
   --and ma.DsnTbl = 'tbl_FormsSAMMSClient'
   and ma.SiteCode = 'B27' -- 'PHC' --
   --and ma.ActionKey = 1 and ma.StepKey = 21
   --and ma.WhereCondition <> '1 = 1'
   --and ma.ActionKey = 4 --and ma.SiteCode in ('D08', 'D09') --
   and t.Status = 17 and t.WorkDate = '9/22/2022' and --	convert(date,Getdate()) and 
   case when ma.SiteCode = 'PHC' then 'PHC ETL'
        when ma.TimeZone = 'EST' then 'Estern ETL' 
        when ma.TimeZone = 'CST' then 'Central ETL' 
		when ma.TimeZone = 'MST' then 'Mountain ETL' 
		when ma.TimeZone = 'PST' then 'Pacific ETL'
        else 'SAMMSGlobal' end = t.TaskName
order by ma.ActionKey, ma.DsnTbl, ma.SiteCode

update tsk.tbl_Schedule set NextRunTime = DATEADD(d,1,NextRunTime), LastRunTime = dateadd(d,1,LastRunTime) where Enabled = 1 --and ScheduleId <> 2
--update tsk.tbl_Schedule set NextRunTime = DATEADD(d,-1,NextRunTime), LastRunTime = dateadd(d,-1,LastRunTime) where Enabled = 1 --and ScheduleId <> 2

update tsk.tbl_Tasks2 set RowState = 26 where TaskName in ('pats.tbl_BriefAddictionMonitor', 'pats.tbl_clinicalopiatewithdrawalscale', 'pats.tbl_FormsSAMMSClient') and SiteCode = 'PHC' and Status = 17 and RowState = 24
update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_PayerClient' and Status = 17 and RowState = 24 and SiteCode = 'LAB' and ActionKey = 1 and ActionStepKey = 6
delete from tsk.tbl_Tasks2 where RunAt <= DateAdd(m, -3, convert(date,GetDate())) or RowState = 26

--insert into tsk.tbl_Tasks2 (ParentTaskId, TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError)
--select top 1 t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, t.RunAt,  t.ActionKey, ma.StepKey, 17, 24, GetDate(), 'Brian.Catellier', ma.SiteCode, t.WorkDate, '0', 0, 0
-- from dms.vw_MapAction ma inner join tsk.tbl_Tasks2 t on (ma.ActionKey = t.ActionKey and t.WorkDate = convert(date,getdate()))
-- where ma.DsnTbl = 'tbl_Dose' and ma.SiteCode = 'B44'
--update tsk.tbl_Schedule set NextRunTime = DATEADD(d,2,NextRunTime), LastRunTime = dateadd(d,1,LastRunTime) where Enabled = 1 
*/
--update tsk.tbl_Schedule set NextRunTime = DATEADD(d,2,NextRunTime), LastRunTime = dateadd(d,1,LastRunTime) where Enabled = 1 and ActionKey = 1
--truncate table tsk.tbl_Tasks2
--update tsk.tbl_Tasks2 set RunAt = Dateadd(d, -1, RunAt) 
--select * from dms.tbl_MapAction where Enabled = 1
--select * from dms.vw_MapSrc2Dsn where Enabled = 1 and ActionKey = 6 and ActionStepKey = 2
/*
Alter view tsk.vwTaskList as
select t.*, ma.ConStr, ma.WhereCondition, ma.SortOrder, ma.IsNewSchema, ma.SrcSchema, ma.FromTblVw
  from tsk.tbl_Tasks2 t left join dms.vw_MapAction ma 
    on (t.ActionKey = ma.ActionKey and t.ActionStepKey = ma.StepKey and t.TaskName = ma.DsnSchema + '.' + ma.DsnTbl and t.SiteCode = ma.SiteCode)
 where t.Status = 17
--order by t.WorkDate, t.RunAt, t.ActionKey, t.ActionStepKey, t.SiteCode
*/
--select SiteCode, Count(1) CNT from pats.tbl_ClaimLineItem where RowState = 1 group by SiteCode
--select * from pats.tbl_ClaimLineItemActivity where SiteCode = 'V8' and RowState = 1

--select * from tsk.tbl_Tasks where WorkDate = '10/28/2021'
--select * from tsk.tbl_Tasks2 where ParentTaskId in (select taskId from tsk.tbl_Tasks2 where ParentTaskId is null and WorkDate = @wrkdt) order by WorkDate, ActionKey, TaskName, SiteCode
--delete from tsk.tbl_Tasks2 where WorkDate = '10/28/2021' and TaskName in ('pats.tbl_Dose', 'pats.tbl_Enrollment')
--update tsk.vwTaskList set RowState = 26 where Status = 17 and ActionKey = 2 and ActionStepKey = 6
--select getdate() dbDate, DateAdd(HH, -5, GetDate()) mDate
--delete from tsk.tbl_Tasks2 where WorkDate <= GetDate()-31
--select * from tsk.tbl_Tasks2 where (TaskId = 444233 or ParentTaskId = 444233) /*and TaskName = 'pats.tbl_UAResults'*/ and Status in (17, 18) --<> 19 and RowState = 24
--update tsk.tbl_Tasks2 set status = 17 where /*(ParentTaskId = 444233 and TaskName = 'pats.tbl_UAResults' and RowState = 24) or*/ TaskId in (468301, 468300)
--update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'ayx.tbl_PreAdmission_V6' and SiteCode = 'PHC' and RowState = 24
--update tsk.vwTaskList set RowState = 26 where TaskName = 'ayx.tbl_PreAdmission_V6' and tsk.vwTaskList.SchemaVersion = 'V5' and RowState = 24; 
--update tsk.tbl_Tasks2 set status = 17 where TaskId = 541833 TaskName = 'ayx.tbl_PreAdmission_V6'
declare @wrkdt date = '2/6/2022';
--'10/3/2021';
select @wrkdt = convert(date,GetDate()-2);
--select * from tsk.tbl_Tasks2 where RowState = 24 and status = 18 --and ParentTaskId is null
select * from tsk.tbl_Tasks2 where RowState = 24 and status in (17, 18, 19) and ParentTaskId is null and WorkDate > @wrkdt --'12/5/2021' and TaskName <> 'Error ReLoad' 
  or (Status in (17, 18) and RowState = 24 and ParentTaskId is null)
order by Status, WorkDate desc, TaskName
--select * from tsk.tbl_Tasks2 where WorkDate = '12/18/2021' and Status <> 19 and RowState = 24 order by ParentTaskId, LastModAt desc
--select * from tsk.tbl_Tasks2 where ActionKey = 6 and Status = 19 and WorkDate = '2/12/2023' and SiteCode <> 'PHC' and TaskName = 'pats.tbl_ClientDemo1'
--select * from pats.tbl_ClientDemo1 where SiteCode like 'B50%' and RowState = 0 order by ClientID
--update pats.tbl_ClientDemo1 set RowState = 0 where SiteCode = 'B50A' and LastModAt < '2/13/2023 12:00:00'
select * from tsk.vwTaskList where RowState = 24 and status = 17 and --WorkDate > '8/3/2022' and
  ParentTaskId in (select TaskId from tsk.tbl_Tasks2 where RowState = 24 and status in (17, 18) and ParentTaskId is null) --and SiteCode = 'PHC'
  or (Status = 20 and WorkDate >= @wrkdt) 
  --and TaskName = 'pats.tbl_ClientDemo1'
order by ParentTaskId, WorkDate, TaskName, SiteCode
--select top 3 * from tsk.tbl_Tasks2 where SiteCode = 'LAB' and TaskName = 'pats.tbl_PayerClient' order by WorkDate desc
--select * from tsk.tbl_Tasks2 where Status in (19, 20) and WorkDate >= @wrkdt /*and SiteCode <> 'PHC'*/ and TaskName in ('pats.tbl_dbo_FormAnswerSignatures', 'pats.tbl_dbo_FormQuestionAnswers') and [RowCount] > 0
--update tsk.tbl_Tasks2 set Status = 17, WorkDate = '3/11/2022' where TaskId in (196929) --or TaskId = 56012 or TaskId = 56013
--update tsk.tbl_Tasks2 set Status = 17 where WorkDate = '2/23/2022' and Status = 19 and ParentTaskId is null
--select * from ctrl.tbl_XRef r where ParentXRef = 23 --16 -- 
--r.Code = 'RowState'
--delete from [pats].[tbl_ClaimLineItem] where SiteCode = 'B24' and RowState = 0 ;
--delete from [pats].[tbl_ClaimLineItemActivity] where RowState = 0;
--delete from [pats].[tbl_Claims] where SiteCode = 'B24' and RowState = 0;
--update tsk.tbl_Tasks2 set status = 19, RowState = 24 where TaskName = '' in (8393)
--select * from pats.tbl_ClientDemo1 where SiteCode = 'Johnston'
--select * from ctrl.tbl_Locations
--delete from tsk.tbl_Tasks2 where TaskId in (522672)

--select * from tsk.tbl_Tasks2 where Status <> 19 and (ParentTaskId = 127842 or TaskId = 127842) --TaskName = 'Forms Oct'
--select fscdate, fscsite, sum(cnt) cnts from stg.tbl_FormsCounts where fscdate >= '10/01/2021' group by fscdate, fscsite
--select * from pats.tbl_FormsSAMMSClient where SiteCode = 'B39' and fscDATE = '10/4/2021'
--select * from ctrl.tbl_Locations
/*
select * from tsk.vw_Schedule 
select * from ctrl.vw_LocationCons where TimeZone = 'EST'
update tsk.tbl_Schedule set NextRunTime = DATEADD(d,1,NextRunTime), LastRunTime = dateadd(d,1,LastRunTime) where ScheduleId in (14, 15, 16)
insert into tsk.tbl_Tasks2 (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError)
select Name, NextRunTime, ActionKey, 0, 17, 24, GetDate(), 'Brian.Catellier', 'All', Convert(date, NextRunTime), '0', 0, 0 from tsk.tbl_Schedule where ScheduleId in (14, 15, 16)

declare @wrkdt date;
select @wrkdt = convert(date,'11/18/2021');
select t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, t.RunAt,  ma.ActionKey, ma.StepKey, 17, 24, GetDate(), 'Brian.Catellier', ma.SiteCode, t.WorkDate, '0', 0, 0
  from dms.vw_MapAction ma inner join tsk.tbl_Tasks2 t on (t.WorkDate = convert(date,@wrkdt))
 where Enabled = 1 and IsActive = 1 and t.Status = 17 --and ma.ActionKey in (select ActionKey from tsk.tbl_Schedule where Enabled = 1)
   and ma.TimeZone = case when t.TaskName = 'Estern ETL' then 'EST' when t.TaskName = 'Central ETL' then 'CST' when t.TaskName = 'Mountain ETL' then 'MST' else 'PST' end
   --and ma.DsnTbl <> 'tbl_Dose'
order by 1, ma.DsnTbl, ma.SiteCode
*/
--update tsk.vwTaskList set RowState = 26 where TaskId = 542067 and SiteCode = 'PHC'
--delete from tsk.tbl_Tasks2 where TaskId in (56906, 56967, 57028, 57089, 57150, 57211, 57272, 57333, 57394, 57455, 57516, 57577, 56601, 56662, 56723, 56784, 56845)

--update tsk.tbl_Tasks2 set ParentTaskId = 82679 where TaskId >= 83223

--select * from pats.tbl_DartsSrv_2022 --where dsID = 44550 order by SiteCode, dsID 
--select * from pats.tbl_DartsSrv_2021 where convert(date,dsDtStart) > '12/31/2021' -- SiteCode = 'B37' and LastModAt >= '1/19/2022'
/*
select * from 
--update 
tsk.tbl_Tasks2 set ParentTaskId = 106001, Status = 17
where WorkDate = '1/1/2022' and TaskName = 'pats.tbl_DartsSrv' --or TaskName = 'DartsSrvs B12B')--and Status = 19

--select * from tsk.tbl_Tasks2 where TaskName = 'pats.tbl_Bills' and Status = 17 and SiteCode = 'B12B'
--update tsk.tbl_Tasks2 set Status = 17 where TaskId = 106004
select * from ctrl.tbl_Connections

select * from ctrl.vw_LocationCons where ConnectionID = 4 --SiteCode like 'CB%'
where SiteCode not in (select SiteCode from ctrl.tbl_Locations)
--select * from ctrl.tbl_LocationCons where SiteCode = 'GB'

select * from ctrl.tbl_Locations where SiteCode like 'B75%'
where SiteCode not in (select SiteCode from ctrl.vw_LocationCons)
--update ctrl.tbl_Locations set ConnectionID = 5 where SiteCode = 'CBLO'

insert into ctrl.tbl_LocationCons (SiteCode, EffectiveDate, ConnectionID, dbName, ActionKey, SchemaVersion)
Values ('B75', '2021-11-09', 6, 'Methasoft_BHG_Lawrence', 7, 'ms'), 
('STC', '2021-10-29', 5, 'Methasoft_StauntonTx', 7, 'ms')
*/
--Alter table pats.tbl_FeeSched alter column CPTCODE varchar(100);

--select * from dms.vw_MapAction where ActionKey = 1 and DsnTbl = 'tbl_Orders'
--select * from tsk.tbl_Tasks2 where TaskName = 'pats.tbl_FormsSAMMSClient'
--update dms.tbl_MapAction set Enabled = 1 where ActionKey = 2 and StepKey = 7
--update ctrl.tbl_LocationCons set ConnectionID = 5 where SiteCode ='CBNC'
--select * from pats.tbl_Orders_2022
--select * from pats.tbl_FormsSAMMSClient where LastModAt >= '1/25/2022' --fscDATE = '11/2/2021'
--update ctrl.tbl_LocationCons set dbName = 'AdvancedMD - Arkansas' where ConnectionID = 4 and SiteCode <> 'B59A'
--update tsk.tbl_Tasks2 set SiteCode = 'PHC' where TaskId = 128959
--select * from tsk.tbl_Tasks2 where TaskId = 128959 or ParentTaskId = 128959
--select * from tsk.vwTaskList where TaskId = 128959 or ParentTaskId = 128959
--delete from tsk.tbl_Tasks2 where TaskId >= 128988 and SiteCode = 'PHC'
--update tsk.tbl_Tasks2 set WorkDate = '1/1/2021' where TaskName = 'pats.tbl_DartsSrv' and Status = 17 and convert(date,RunAt) ='2022-03-11'
--update tsk.tbl_Tasks2 set WorkDate = '2/26/2022' where SiteCode = 'DRD-KVC' and Status = 17 and TaskName = 'pats.tbl_DartsSrv'
--update tsk.tbl_Tasks2 set WorkDate = '3/1/2022' where SiteCode = 'TTCB' and Status = 17 and TaskName = 'pats.tbl_DartsSrv'

--delete from pats.tbl_BriefAddictionMonitor where [date] >= '10/1/2021'
/*
An error occurred while updating the entries. See the inner exception for details.     Violation of PRIMARY KEY constraint 'PK_BreifAddictionMonitor'. 
Cannot insert duplicate key in object 'pats.tbl_BriefAddictionMonitor'. 
The duplicate key value is (B42C, 63286).  The statement has been terminated.
*/
--alter table pats.tbl_ClientDemo2 add RowState int;
--select * from dms.vw_MapAction where FromTblVw = 'tblClient'
--select SiteCode, count(1) from pats.tbl_ClientDemo1 where RowState = 1 group by SiteCode order by 1
--select SiteCode, count(1) from pats.tbl_ClientDemo2 where RowState = 1 group by SiteCode
--update pats.tbl_ClientDemo1 set RowState = 1 where SiteCode in ('Westerly', 'Middletown', 'MTCGA', 'MTCLA', 'MTCMP', 'Pawtucket', 'Providence', 'SoS', 'STC') and RowState = 0
--select * from tsk.tbl_Tasks2 where --TaskName = 'pats.tbl_PayerClient' and Status = 17 and SiteCode = 'CBLV2'
--select * from pats.tbl_ClientDemo1 where LastModAt >= '3/15/2022' order by SiteCode
/*
update tsk.tbl_Tasks2 set status = 17 where (TaskName = 'pats.tbl_ClientDemo1' or TaskName = 'pats.tbl_ClientDemo2') and WorkDate = '3/15/2022' and ActionKey = 1
update tsk.tbl_Tasks2 set WorkDate = '3/7/2022' where TaskId = 7052 --and WorkDate = '3/27/2022'
--TaskId in (201429)

An error occurred while updating the entries. See the inner exception for details. B42D 1979     Violation of PRIMARY KEY constraint 'PK_Enrollment'. Cannot insert duplicate key in object 'pats.tbl_Enrollment'. The duplicate key value is (5230, B42D).
select * from pats.tbl_Enrollment where SiteCode = 'B42D' and  ID in (5230, 1979)
*/
--select * from tsk.tbl_Tasks2 where siteCode = 'VBRA' or SiteCode = 'VMIN' order by TaskName, SiteCode
--update tsk.tbl_Tasks2 set RowState = 26 where ParentTaskId = 9005 and TaskId >=10507 
--select * from tsk.tbl_Tasks2 where Status = 19 and WorkDate = '4/19/2022'
--update tsk.tbl_Tasks2 set WorkDate = '2/22/2022' where TaskName = 'pats.tbl_DartsSrv' and Status = 17 and SiteCode = 'V15'
--select top 10 * from pats.tbl_FormsSAMMSClient where fscsid = 2094521
--update tsk.tbl_Tasks2 set Status = 17 where Status = 18 
--alter table pats.tbl_ClientDemo2 alter column Counselor varchar(100) 
--select * from tsk.vwTaskList where ParentTaskId = 127842 order by TaskName, SiteCode
--delete from tsk.tbl_Tasks2 where WorkDate = '7/5/2022' and LastModAt >= '7/6/2022' and ParentTaskId is not null and Status = 17
--update tsk.vwTaskList set RowState = 26 where Status = 17 and WorkDate < '7/6/2022' and WhereCondition = '1 = 1'
--update tsk.tbl_Tasks2 set workDate = '7/10/2022' where TaskName = 'pats.tbl_DartsSrv' and Status = 17
--update tsk.tbl_Tasks2 set status = 17 where TaskId in (225007, 224899)
--select * from pats.tbl_Bills where SiteCode = 'Lab' and LastModAt >= '7/29/2022' order by billDate desc
--select * from tsk.tbl_Tasks2 where TaskName = 'pats.tbl_Bills' and WorkDate = '8/27/2022'
--select * from ctrl.tbl_LocationCons where SiteCode = 'STC' -- dbName like 'SAMMS%'
--select * from tsk.vwTaskList where SiteCode = 'STC' and Status = 17
--update dms.tbl_MapAction set WhereCondition = 'address_type_id = 106' where ActionKey = 7 and StepKey = 5
--select * from pats.tbl_DartsSrv_2022 where SiteCode = 'V5' and (DSbilled = '3/31/2022' or dsSigDate = '3/31/2022' or convert(date,dsDtAdded) = '3/31/2022' or dsDtStart = '3/31/2022' or dssigdateCOSIGN = '3/31/2022')
--select * from tsk.tbl_Tasks2 where Status = 17 and SiteCode = 'TTCA' and TaskName = 'pats.tbl_PayerClient'
--update tsk.tbl_Tasks2 set Status = 17 where TaskId in (561709, 561710, 561711, 561712, 561713, 561714, 561715, 561716, 561717, 559895, 559894)  --248933, 
--select * from tsk.tbl_Tasks2 where TaskName = 'pats.tbl_ClientDemo1' and WorkDate = '2/22/2023'
--update tsk.tbl_Tasks2 set workDate = '1/1/2000' where TaskName = 'pats.tbl_dbo_FormAnswerSignatures' and status = 17
--update tsk.tbl_Tasks2 set Status = 17 where status = 20 and TaskName in ('pats.tbl_dbo_FormAnswerSignatures', 'pats.tbl_dbo_FormQuestionAnswers') and WorkDate >= '10/31/2022'
--update tsk.tbl_Tasks2 set status = 17 where TaskId in (583065, 581214)
--in (select distinct ParentTaskId from tsk.tbl_Tasks2 where WorkDate >= '10/31/2022' and status = 17)
--dtMedDate > '12/31/2019' and (dtMedDate <= @WorkDate or dtDate <= @WorkDate) or manualauthdtm <= @workDate or [DTgiven] <= @WorkDate or [DTprep] <= @WorkDate)
--An error occurred while updating the entries. See the inner exception for details.     Violation of PRIMARY KEY constraint 'PK_tblClientDemo2'. Cannot insert duplicate key in object 'pats.tbl_ClientDemo2'. The duplicate key value is (Lab, 245476).
--select * from pats.tbl_ClientDemo2 where clientID = 245476
--update tsk.tbl_Tasks2 set Status = 17 where Status = 20 and ParentTaskId in (540706) --and TaskName = 'ayx.tbl_PreAdmission_V6'
--update tsk.tbl_Tasks2 set status = 17 where TaskId in (540706)

--alter table pats.tbl_Orders_2023 alter column Notes varchar(2000)
--alter table pats.tbl_Cows_v6 alter column CompletedBy Varchar(1000) 
--alter table dms.tbl_MapAction add RowTrax bit
--select * from pats.tbl_Cows_V6