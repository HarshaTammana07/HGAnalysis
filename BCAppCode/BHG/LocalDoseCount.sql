declare @WorkDate date = '12/22/2022';
use [SAMMS-FortCollinsV5]
--select convert(date, dtDate) dtDate, Count(1) from dbo.tblDOSE where dtDate >= '10/1/2022' group by convert(date, dtDate) order by 1

select len(ExceptionReason) er, * from dbo.tblDOSE where year(dtDate) = 2022 and len(ExceptionReason) > 200
