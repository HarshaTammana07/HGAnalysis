# ETL pipeline runtime (single day)

**Run date:** 2026-01-22

**Source:** `tsk.tbl_Tasks2` — parent tasks (`ParentTaskId IS NULL`). Start = `RunAt`; end = `RunAt + Duration`; wall-clock = span across regional parents for P1/P2 (4 rows each).

**Note:** `PipelineStart` and `PipelineEnd` are time-of-day strings on the run date. If the end is **earlier** in the clock than the start (e.g. P1: `00:34:36` after `20:01:00`), the process crossed **midnight**; `WallClockDuration` is the correct elapsed time (e.g. 4h 33m 36s), not a mis-sorted time.

---

## Results (2026-01-22)


| RunDate    | Arg | PipelineName      | RegionalTasks | PipelineStart | PipelineEnd | WallClockDuration | Status    |
| ---------- | --- | ----------------- | ------------- | ------------- | ----------- | ----------------- | --------- |
| 2026-01-22 | 1   | SAMMSGlobal       | 1             | 20:00:00      | 21:52:17    | 01:52:17          | Completed |
| 2026-01-22 | 2   | Regional ETL P1   | 4             | 20:01:00      | 00:34:36*   | 04:33:36          | Completed |
| 2026-01-22 | 4   | Regional ETL P2   | 4             | 20:01:00      | 22:50:20    | 02:49:20          | Completed |
| 2026-01-22 | 6   | Samms-Forms       | 1             | 20:01:30      | 07:27:33*   | 11:26:03          | Completed |
| 2026-01-22 | 7   | SAMMS-ETL-Notes   | 1             | 20:05:00      | 21:15:17    | 01:10:17          | Completed |
| 2026-01-22 | 8   | SAMMS-ETL-INV     | 1             | 20:06:00      | 23:05:16    | 02:59:16          | Completed |
| 2026-01-22 | 9   | SAMMS-ETL-DartSvc | 1             | 20:07:00      | 23:51:31    | 03:44:31          | Completed |
| 2026-01-22 | 10  | SAMMS-ETL-Dose    | 1             | 20:08:00      | 22:29:19    | 02:21:19          | Completed |
| 2026-01-22 | 11  | SAMMS-ETL-Orders  | 1             | 20:09:00      | 01:59:36*   | 05:50:36          | Completed |


 *End time is on the **next calendar day** after `RunDate` (after midnight).*

**Missing from this extract:** Arg 5 (Samms-LAB) had no row for 2026-01-22 in the data you pasted — re-run the query in BHG_DR to confirm whether LAB did not run that day or was filtered out.

---

## SQL query

Set `@RunDate` to the day you want. Use a literal date or `CAST(GETDATE() - 1 AS DATE)` for “yesterday.”

```sql
DECLARE @RunDate DATE = '2026-01-22';

WITH ParentTasks AS (
    SELECT
        TaskId,
        TaskName,
        RunAt,
        Status,
        [PipelineName] = CASE
            WHEN TaskName = 'SAMMSGlobal'                              THEN 'SAMMSGlobal'
            WHEN TaskName IN ('Eastern ETL P1','Central ETL P1',
                              'Mountain ETL P1','Pacific ETL P1')      THEN 'Regional ETL P1'
            WHEN TaskName IN ('Eastern ETL P2','Central ETL P2',
                              'Mountain ETL P2','Pacific ETL P2')      THEN 'Regional ETL P2'
            WHEN TaskName = 'Samms-LAB'                                THEN 'Samms-LAB'
            WHEN TaskName = 'Samms-Forms'                              THEN 'Samms-Forms'
            WHEN TaskName = 'SAMMS-ETL-Notes'                         THEN 'SAMMS-ETL-Notes'
            WHEN TaskName = 'SAMMS-ETL-INV'                           THEN 'SAMMS-ETL-INV'
            WHEN TaskName = 'SAMMS-ETL-DartSvc'                       THEN 'SAMMS-ETL-DartSvc'
            WHEN TaskName = 'SAMMS-ETL-Dose'                          THEN 'SAMMS-ETL-Dose'
            WHEN TaskName = 'SAMMS-ETL-Orders'                        THEN 'SAMMS-ETL-Orders'
            ELSE NULL
        END,
        [Arg] = CASE
            WHEN TaskName = 'SAMMSGlobal'                              THEN 1
            WHEN TaskName IN ('Eastern ETL P1','Central ETL P1',
                              'Mountain ETL P1','Pacific ETL P1')      THEN 2
            WHEN TaskName IN ('Eastern ETL P2','Central ETL P2',
                              'Mountain ETL P2','Pacific ETL P2')      THEN 4
            WHEN TaskName = 'Samms-LAB'                                THEN 5
            WHEN TaskName = 'Samms-Forms'                              THEN 6
            WHEN TaskName = 'SAMMS-ETL-Notes'                         THEN 7
            WHEN TaskName = 'SAMMS-ETL-INV'                           THEN 8
            WHEN TaskName = 'SAMMS-ETL-DartSvc'                       THEN 9
            WHEN TaskName = 'SAMMS-ETL-Dose'                          THEN 10
            WHEN TaskName = 'SAMMS-ETL-Orders'                        THEN 11
            ELSE NULL
        END,
        [EndTime] = DATEADD(
                        SECOND,
                        DATEDIFF(SECOND, '00:00:00', TRY_CAST(Duration AS TIME)),
                        RunAt
                    )
    FROM tsk.tbl_Tasks2
    WHERE
        ParentTaskId IS NULL
        AND WorkDate  = @RunDate
        AND Status    IN (18, 19, 20)
        AND Duration  IS NOT NULL
        AND Duration  <> ''
        AND Duration  <> '0'
)

SELECT
    [RunDate]           = @RunDate,
    [Arg],
    [PipelineName],
    [RegionalTasks]     = COUNT(1),
    [PipelineStart]     = CONVERT(VARCHAR(8), MIN(RunAt),   108),
    [PipelineEnd]       = CONVERT(VARCHAR(8), MAX(EndTime),  108),
    [WallClockDuration] = RIGHT('0' + CAST(DATEDIFF(SECOND, MIN(RunAt), MAX(EndTime)) / 3600 AS VARCHAR), 2) + ':' +
                          RIGHT('0' + CAST((DATEDIFF(SECOND, MIN(RunAt), MAX(EndTime)) % 3600) / 60 AS VARCHAR), 2) + ':' +
                          RIGHT('0' + CAST(DATEDIFF(SECOND, MIN(RunAt), MAX(EndTime)) % 60 AS VARCHAR), 2),
    [Status]            = CASE
                              WHEN MAX(Status) = 20 THEN 'Has Errors'
                              WHEN MAX(Status) = 18 THEN 'Still Running'
                              ELSE 'Completed'
                          END
FROM ParentTasks
WHERE PipelineName IS NOT NULL
GROUP BY PipelineName, Arg
ORDER BY [Arg];
```

**Column reference**


| Output column                   | Meaning                                                                                                           |
| ------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| `Arg`                           | `BHGTaskRunner.exe` schedule (1–11).                                                                              |
| `RegionalTasks`                 | Count of parent rows: `4` for P1/P2 (four time zones), `1` for single-pipeline names.                             |
| `PipelineStart` / `PipelineEnd` | `HH:MM:SS` only; next-day end times still show as time-of-day (e.g. `00:34` = just after midnight on 2026-01-23). |
| `WallClockDuration`             | `MAX(EndTime) - MIN(RunAt)` in `HH:MM:SS`, including spans past 24 hours.                                         |


If you need **end datetime** as a real `DATETIME2` (so “next day” is obvious in one column), the expression is `MAX(EndTime)` without stripping to time-only.