declare @wrkdt date = '2/13/2024';
set @wrkdt = GetDate()-1;
select t1.PTaskName, t1.TaskName, TS_Yesterday = t1.TS, TS_2Days = t2.TS, TS_3Days = t3.TS, TS_4Days = t4.TS, TS_5Days = t5.TS
     , TS_Avg = (t1.TS + t2.TS + t3.TS + t4.TS + t5.TS)/5
  From (select PTaskName = pt.TaskName, t.TaskName
     --, [Hours] = sum(convert(int,SUBSTRING(t.Duration, 1, 2)))
	 --, [Minutes] = sum(convert(int,SUBSTRING(t.Duration, 4, 2)))
	 --, [Seconds] = sum(convert(int,SUBSTRING(t.Duration, 7, 2)))
	 , TS = sum(convert(int,SUBSTRING(t.Duration, 7, 2)) + (convert(int,SUBSTRING(t.Duration, 4, 2))*60) + (convert(int,SUBSTRING(t.Duration, 1, 2)) * 60 * 60))
  from tsk.tbl_Tasks2 t inner join tsk.tbl_Tasks2 pt on (t.ParentTaskId = pt.TaskId)
 where t.WorkDate = @wrkdt and t.ParentTaskId is not null
group by pt.TaskName, t.ParentTaskId, t.TaskName) t1
left join 
(select PTaskName = pt.TaskName, t.TaskName
     --, [Hours] = sum(convert(int,SUBSTRING(t.Duration, 1, 2)))
	 --, [Minutes] = sum(convert(int,SUBSTRING(t.Duration, 4, 2)))
	 --, [Seconds] = sum(convert(int,SUBSTRING(t.Duration, 7, 2)))
	 , TS = sum(convert(int,SUBSTRING(t.Duration, 7, 2)) + (convert(int,SUBSTRING(t.Duration, 4, 2))*60) + (convert(int,SUBSTRING(t.Duration, 1, 2)) * 60 * 60))
  from tsk.tbl_Tasks2 t inner join tsk.tbl_Tasks2 pt on (t.ParentTaskId = pt.TaskId)
 where t.WorkDate = DateAdd(d,-1,@wrkdt) and t.ParentTaskId is not null
group by pt.TaskName, t.ParentTaskId, t.TaskName) t2 on (t1.PTaskName = t2.PTaskName and t1.TaskName = t2.TaskName)
left join 
(select PTaskName = pt.TaskName, t.TaskName
     --, [Hours] = sum(convert(int,SUBSTRING(t.Duration, 1, 2)))
	 --, [Minutes] = sum(convert(int,SUBSTRING(t.Duration, 4, 2)))
	 --, [Seconds] = sum(convert(int,SUBSTRING(t.Duration, 7, 2)))
	 , TS = sum(convert(int,SUBSTRING(t.Duration, 7, 2)) + (convert(int,SUBSTRING(t.Duration, 4, 2))*60) + (convert(int,SUBSTRING(t.Duration, 1, 2)) * 60 * 60))
  from tsk.tbl_Tasks2 t inner join tsk.tbl_Tasks2 pt on (t.ParentTaskId = pt.TaskId)
 where t.WorkDate = DateAdd(d,-2,@wrkdt) and t.ParentTaskId is not null
group by pt.TaskName, t.ParentTaskId, t.TaskName) t3 on (t1.PTaskName = t3.PTaskName and t1.TaskName = t3.TaskName)
left join 
(select PTaskName = pt.TaskName, t.TaskName
     --, [Hours] = sum(convert(int,SUBSTRING(t.Duration, 1, 2)))
	 --, [Minutes] = sum(convert(int,SUBSTRING(t.Duration, 4, 2)))
	 --, [Seconds] = sum(convert(int,SUBSTRING(t.Duration, 7, 2)))
	 , TS = sum(convert(int,SUBSTRING(t.Duration, 7, 2)) + (convert(int,SUBSTRING(t.Duration, 4, 2))*60) + (convert(int,SUBSTRING(t.Duration, 1, 2)) * 60 * 60))
  from tsk.tbl_Tasks2 t inner join tsk.tbl_Tasks2 pt on (t.ParentTaskId = pt.TaskId)
 where t.WorkDate = DateAdd(d,-3,@wrkdt) and t.ParentTaskId is not null
group by pt.TaskName, t.ParentTaskId, t.TaskName) t4 on (t1.PTaskName = t4.PTaskName and t1.TaskName = t4.TaskName)
left join 
(select PTaskName = pt.TaskName, t.TaskName
     --, [Hours] = sum(convert(int,SUBSTRING(t.Duration, 1, 2)))
	 --, [Minutes] = sum(convert(int,SUBSTRING(t.Duration, 4, 2)))
	 --, [Seconds] = sum(convert(int,SUBSTRING(t.Duration, 7, 2)))
	 , TS = sum(convert(int,SUBSTRING(t.Duration, 7, 2)) + (convert(int,SUBSTRING(t.Duration, 4, 2))*60) + (convert(int,SUBSTRING(t.Duration, 1, 2)) * 60 * 60))
  from tsk.tbl_Tasks2 t inner join tsk.tbl_Tasks2 pt on (t.ParentTaskId = pt.TaskId)
 where t.WorkDate = DateAdd(d,-4,@wrkdt) and t.ParentTaskId is not null
group by pt.TaskName, t.ParentTaskId, t.TaskName) t5 on (t1.PTaskName = t5.PTaskName and t1.TaskName = t5.TaskName)
--where t1.TaskName = 'pats.tbl_dbo_FormAnswerSignatures'
order by t1.TS desc
