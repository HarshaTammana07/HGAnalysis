--update tsk.tbl_Tasks2 set RunAt = '12/11/2022 10:10:10 am' where ParentTaskId = 434289 --WorkDate >= '10/5/2022' and status in (17, 18, 20) and TaskName =  'pats.tbl_Dose'
--delete from tsk.tbl_Tasks2 where ParentTaskId in (399146, 401097, 401098, 401099, 401100, 401101, 401102, 401103, 401104, 401105) or TaskId in (399146, 401097, 401098, 401099, 401100, 401101, 401102, 401103, 401104, 401105)
select * from tsk.tbl_Tasks2 where (TaskId = 444233 or ParentTaskId = 444233) and Status in (17, 18)
--update tsk.tbl_Tasks2 set RunAt = '12/18/2022 7:03:00 AM' where TaskId = 444233
select * from tsk.vwTaskList where ActionKey = 7
select * from tsk.tbl_Tasks2 where WorkDate >= '12/21/2022' and status in (17, 18, 20) --and SiteCode = 'B75'--and TaskName = 'pats.tbl_UAResults' --and (ParentTaskId = 434289 or TaskId = 434289) 
order by ParentTaskId, WorkDate 
--update tsk.tbl_Tasks2 set status = 17 where Status = 20 and WorkDate = '1/1/2022' and TaskId in (444233)
--update tsk.tbl_Tasks2 set status = 17 where Status = 20 and WorkDate = '1/1/2022' and ParentTaskId in (438245, 438246)

select * from ctrl.tbl_Locations where SiteCode = 'B75'
select * from ctrl.tbl_LocationCons where SiteCode = 'B42'
select * from dms.vw_MapAction where SiteCode = 'B42' and ActionKey = 1
select * from dms.vw_MapSrc2Dsn where ActionKey = 1 and ActionStepKey = 9
--update dms.tbl_MapAction set WhereCondition = --'uardRecID in (select uarID from tblUAResult where convert(date, uarResultDt) = convert(date, @WorkDate) or convert(date, uarDropDt) >= convert(date, @WorkDate))' where ActionKey = 1 and StepKey = 10
--'convert(date, uarResultDt) = convert(date, @WorkDate) or convert(date, uarDropDt) >= convert(date, @WorkDate)' where ActionKey = 1 and StepKey = 9
--delete from ctrl.tbl_LocationCons where SiteCode = 'B75' and ActionKey = 7





select distinct FormName from pats.tbl_dbo_FormAnswerSignatures order by 1
select x.TaskId, x.WorkDate, x.TaskName, (Select count(o.TaskId) from tsk.tbl_Tasks2 o where o.ParentTaskId = x.TaskId and o.Status = 17) TaskRemaining, sum(x.[RowCount]) Completed
  from tsk.tbl_Tasks2 x where x.Status in (17, 18) and x.ParentTaskId is null group by x.TaskId, x.WorkDate, x.TaskName
order by x.TaskId
--update tsk.tbl_Tasks2 set status = 17 where TaskId = 434289
select * from tsk.tbl_Tasks2 where SiteCode = 'PHC' and WorkDate >= '2/15/2023'
select * from tsk.tbl_Schedule where Enabled = 1
--update tsk.tbl_Tasks2 set Status = 17 where TaskId = 401096
--insert into tsk.tbl_Tasks2 (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError) 
--values ('Results test', '2022-12-17 10:03:00.000', 1, 0, 17, 24, GetDate(), 'BCatellier', '', '2022-1-01', '', 0, 0)
select Name, NextRunTime, ActionKey, 0, 17, 24, GetDate(), 'Brian.Catellier', Case when scheduleid = 18 then 'PHC' else 'All' end, Convert(date, '11/27/2022'), '0', 0, 0 
       from tsk.tbl_Schedule where Enabled = 1;
/*
insert into tsk.tbl_Tasks2(ParentTaskId, TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError) 
select t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, t.RunAt,  ma.ActionKey, ma.StepKey, 17, 24, GetDate(), 'Brian.Catellier', ma.SiteCode, t.WorkDate, '0', 0, 0 
  from dms.vw_MapAction ma cross join tsk.tbl_Tasks2 t 
 where ma.Enabled = 1 and ma.IsActive = 1 and ConnectionID <> 3 and ma.ActionKey = 1 and ma.StepKey in (9, 10)
   and t.Status = 17 and t.TaskName = 'Results test' /*and t.WorkDate = convert(date, '1/1/2022') and 
  case when ma.SiteCode = 'PHC' then 'PHC ETL' 
       when ma.TimeZone = 'EST' then 'Estern ETL' 
       when ma.TimeZone = 'CST' then 'Central ETL' 
       when ma.TimeZone = 'MST' then 'Mountain ETL' 
       when ma.TimeZone = 'PST' then 'Pacific ETL' 
       when t.TaskName = 'Results test' then 'Results test'
       else 'SAMMSGlobal' end = t.TaskName */
order by ma.ActionKey, ma.DsnTbl, ma.SiteCode;
*/
--update tsk.tbl_Tasks2 set RowState = 26 where TaskName in ('pats.tbl_BriefAddictionMonitor', 'pats.tbl_clinicalopiatewithdrawalscale', 'pats.tbl_FormsSAMMSClient') and SiteCode = 'PHC' and Status = 17 and RowState = 24;
--update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_PayerClient' and Status = 17 and RowState = 24 and SiteCode = 'LAB' and ActionKey = 1 and ActionStepKey = 6;
--delete from tsk.tbl_Tasks2 where RunAt <= DateAdd(m, -3, convert(date, GetDate())) or RowState = 26;
--update tsk.tbl_Tasks2 set status = 17 where TaskId in (select distinct ParentTaskId from tsk.tbl_Tasks2 where Status = 20 and WorkDate = '12/5/2022')
--update tsk.tbl_Tasks2 set status = 17 where status = 20 and WorkDate = '12/5/2022'
--alter table pats.tbl_orders_2022 alter column Notes varchar(MAX)
select * from ctrl.tbl_Locations where ClinicName like 'P%'
select * from pats.tbl_dbo_FormQuestionAnswers where SiteCode = 'B24'
select distinct a.SiteCode, a.SiteID, l.ClinicName, l.SiteCode, l.sID 
/*delete*/ from pats.tbl_DartsSrv_2022 a inner join ctrl.tbl_Locations l on a.SiteCode = l.SiteCode
where a.SiteID <> l.sID order by 1
