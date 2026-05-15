# Bulk Table Runtime — Single Day Results
**Run date:** 2026-04-22  
**Source:** `tsk.tbl_Tasks2` (Status = 19, child tasks only)  
**Scope:** One ETL night — 116 site runs per table (where applicable)

---

## Formatted Table

| # | Table Name | Load Pattern | Sites | Total Rows Fetched (night) | Total Duration (sum of all site runs) | Avg / Site | Min | Max (slowest site) | Slowest site | Avg row count / run | Max row count / run |
|---|------------|-------------|------:|---------------------------:|----------------------------------------|:-----------|:----|:------------------:|--------------|--------------------:|--------------------:|
| 1 | `pats.tbl_DartsSrv` | Bulk — DartsSrvMerge x8 | 116 | 4,702,119 | 03:49:27 | 00:01:58 | 00:00:05 | 00:08:49 | LO | 40,535 | 201,298 |
| 2 | `pats.tbl_Dose` | Bulk (EF for 4 sites) | 116 | 5,896,635 | 01:38:38 | 00:00:51 | 00:00:00 | 00:15:18 | MP | 50,833 | 181,701 |
| 3 | `pats.tbl_ClaimLineItemActivity` | Bulk | 116 | 49,388,393 | 01:21:29 | 00:00:42 | 00:00:00 | 00:04:32 | DRD-KVC | 425,762 | 2,747,119 |
| 4 | `pats.tbl_Claims` | Bulk (EF for 4 sites) | 116 | 9,033,285 | 01:06:37 | 00:00:34 | 00:00:00 | 00:04:45 | VWBY | 77,873 | 389,705 |
| 5 | `pats.tbl_ClaimLineItem` | Bulk | 116 | 38,901,475 | 01:00:07 | 00:00:31 | 00:00:00 | 00:03:11 | B42 | 335,357 | 2,417,949 |
| 6 | `pats.tbl_dbo_FormQuestionAnswers` | Bulk (18 sites) / EF (rest) | 116 | 248,883 | 00:56:23 | 00:00:29 | 00:00:00 | 00:04:45 | VWBY | 2,145 | 20,525 |
| 7 | `pats.tbl_vw3pBillSub` | Bulk | 116 | 662,272 | 00:10:15 | 00:00:05 | 00:00:00 | 00:00:48 | TTCA | 5,709 | 123,449 |
| 8 | `pats.tbl_Dose_Excuse` | EF (Bulk infra exists) | 116 | 451,232 | 00:09:15 | 00:00:04 | 00:00:00 | 00:01:16 | V6 | 3,889 | 58,222 |
| 9 | `pats.tbl_FormsSAMMSClient` | Bulk | 1 | 5,279,676 | 00:05:47 | 00:05:47 | 00:05:47 | 00:05:47 | Global | 5,279,676 | 5,279,676 |
| 10 | `pats.tbl_LiquidLog` | Bulk (reload) / EF (incr) | 116 | 833,427 | 00:01:50 | 00:00:00 | 00:00:00 | 00:00:06 | **Tie: DRD-KVC, DRD-NOLA** | 7,184 | 33,183 |
| 11 | `pats.tbl_UAResultDetail` | Bulk | 116 | 498,399 | 00:00:31 | 00:00:00 | 00:00:00 | 00:00:02 | B35A | 4,296 | 14,124 |
| 12 | `pats.tbl_LabResultDetail` | Bulk | 116 | 556,532 | 00:00:26 | 00:00:00 | 00:00:00 | 00:00:05 | RE | 4,797 | 88,853 |

---

## Column reference

| Column | Meaning for this single night (2026-04-22) |
|--------|--------------------------------|
| **Sites** | How many child tasks (sites) completed for that table. |
| **Total rows fetched** | `SUM(RowCount)` for all 116 site runs = total rows read from source that night. |
| **Total duration (sum of all site runs)** | Cumulative time if every site’s run is added. On this architecture sites run **one after another** within a regional task, so this is a good proxy for **end-to-end sequential work** for the table. |
| **Avg / site** | Average time per one site. |
| **Min** | Fastest single-site run. |
| **Max** | Slowest single-site run — the **bottleneck** for parallel Fabric (wall-clock is roughly the max, not the sum, when sites run in parallel). |
| **Slowest site** | Site with **Max** duration. If two sites share the same max, the join returns two rows (hence the duplicate `LiquidLog` in raw output) — **tie** in the table above. |
| **Avg row count / run** | Average of `RowCount` per child task. |
| **Max row count / run** | Largest `RowCount` in any single run that night. |

---

## What stands out (2026-04-22)

- **DartsSrv** is the heaviest single night: ~3.8 h cumulative time across 116 sites; slowest site **LO** = 8:49, ~4.7M rows across all sites.
- **Dose** second: ~1.6 h cumulative; slowest **MP** = 15:18.
- **Claims / Claim lines / line activity** each ~1+ h cumulative; millions of rows per run on large sites.
- **FormsSAMMSClient** = **1** site run, **Global** = SAMMSGLOBAL, not 116.
- **LiquidLog** duplicate row in the export = **two** sites (DRD-KVC, DRD-NOLA) with the **same** max duration 00:00:06 — the query can list both; the formatted table uses one row with a tie note.

---

## How to fix duplicate rows in the output (SQL)

If two sites have the same **max** duration, the join to `SlowestSitePerTable` returns two rows. Options:

1. **Concatenate (Azure SQL, SQL Server 2022+ or compatible):** in the final `SELECT` use
   `STRING_AGG` on `SlowestSite` grouped by `RunDate` + the rest of the summary columns, or
2. **Pick one site:** use `ROW_NUMBER() OVER (PARTITION BY s.TaskName ORDER BY s.SiteCode)` and
   `WHERE rn = 1` on the join to `SingleNight` so only the alphabetically first tied site is shown, or
3. **Accept two rows** for that table and read both site codes.

---

## The query used (single day — @RunDate)

`Duration` and `RowCount` are T-SQL reserved / problematic as aliases; `[RowCnt]` and bracketed names avoid errors.

```sql
DECLARE @RunDate DATE = '2026-04-22';
-- DECLARE @RunDate DATE = CAST(GETDATE() - 1 AS DATE);

WITH SingleNight AS (
    SELECT
        TaskName,
        SiteCode,
        Duration,
        [RowCnt]      = [RowCount],
        [DurationSec] = DATEDIFF(SECOND, '00:00:00', TRY_CAST([Duration] AS TIME))
    FROM tsk.tbl_Tasks2
    WHERE
        [Status]          = 19
        AND [ParentTaskId] IS NOT NULL
        AND [WorkDate]    = @RunDate
        AND [Duration]    IS NOT NULL
        AND [Duration]    <> ''
        AND [Duration]    <> '0'
        AND LOWER([TaskName]) IN (
            'pats.tbl_dartssrv',
            'pats.tbl_dose',
            'pats.tbl_dose_excuse',
            'pats.tbl_claims',
            'pats.tbl_claimlineitem',
            'pats.tbl_claimlineitemactivity',
            'pats.tbl_clientdemo1',
            'pats.tbl_clientdemo2',
            'pats.tbl_formssammsclient',
            'pats.tbl_liquidlog',
            'pats.tbl_uaresultdetail',
            'pats.tbl_labresultdetail',
            'pats.tbl_vw3pbillsub',
            'pats.tbl_dbo_formquestionanswers'
        )
),

Summary AS (
    SELECT
        TaskName,
        [SitesRan]     = COUNT(1),
        [TotalDurSec]  = SUM([DurationSec]),
        [AvgDurSec]    = AVG(CAST([DurationSec] AS FLOAT)),
        [MinDurSec]    = MIN([DurationSec]),
        [MaxDurSec]    = MAX([DurationSec]),
        [TotalRowCnt]  = SUM(CAST([RowCnt] AS BIGINT)),
        [AvgRowCnt]    = AVG(CAST([RowCnt] AS FLOAT)),
        [MaxRowCnt]    = MAX(CAST([RowCnt] AS BIGINT))
    FROM SingleNight
    GROUP BY TaskName
),

SlowestSitePerTable AS (
    SELECT
        s.TaskName,
        [SlowestSite] = s.SiteCode
    FROM SingleNight s
    INNER JOIN Summary agg
        ON  s.TaskName     = agg.TaskName
        AND s.[DurationSec] = agg.MaxDurSec
)

SELECT
    [RunDate]     = @RunDate,
    [TaskName]    = s.TaskName,
    [LoadPattern] = CASE LOWER(s.TaskName)
        WHEN 'pats.tbl_dartssrv'                 THEN 'Bulk — DartsSrvMerge x8'
        WHEN 'pats.tbl_dose'                      THEN 'Bulk (EF for 4 sites)'
        WHEN 'pats.tbl_dose_excuse'               THEN 'EF (Bulk infra exists)'
        WHEN 'pats.tbl_claims'                    THEN 'Bulk (EF for 4 sites)'
        WHEN 'pats.tbl_claimlineitem'             THEN 'Bulk'
        WHEN 'pats.tbl_claimlineitemactivity'     THEN 'Bulk'
        WHEN 'pats.tbl_clientdemo1'               THEN 'Bulk'
        WHEN 'pats.tbl_clientdemo2'               THEN 'Bulk'
        WHEN 'pats.tbl_formssammsclient'          THEN 'Bulk'
        WHEN 'pats.tbl_liquidlog'                 THEN 'Bulk (reload) / EF (incr)'
        WHEN 'pats.tbl_uaresultdetail'            THEN 'Bulk'
        WHEN 'pats.tbl_labresultdetail'           THEN 'Bulk'
        WHEN 'pats.tbl_vw3pbillsub'              THEN 'Bulk'
        WHEN 'pats.tbl_dbo_formquestionanswers'  THEN 'Bulk (18 sites) / EF (rest)'
    END,
    s.[SitesRan],
    s.[TotalRowCnt],

    -- Durations in HH:MM:SS (total can exceed 24:00:00; do not use style 108 alone on TIME)
    [TotalDuration] = RIGHT('0' + CAST(s.[TotalDurSec] / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((s.[TotalDurSec] % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(s.[TotalDurSec] % 60 AS VARCHAR), 2),
    [AvgDuration]   = RIGHT('0' + CAST(CAST(s.[AvgDurSec] AS INT) / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((CAST(s.[AvgDurSec] AS INT) % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(CAST(s.[AvgDurSec] AS INT) % 60 AS VARCHAR), 2),
    [MinDuration]   = RIGHT('0' + CAST(s.[MinDurSec] / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((s.[MinDurSec] % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(s.[MinDurSec] % 60 AS VARCHAR), 2),
    [MaxDuration]   = RIGHT('0' + CAST(s.[MaxDurSec] / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((s.[MaxDurSec] % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(s.[MaxDurSec] % 60 AS VARCHAR), 2),

    ss.[SlowestSite],
    [AvgRowCount]   = s.[AvgRowCnt],
    [MaxRowCount]   = s.[MaxRowCnt]
FROM Summary s
LEFT JOIN SlowestSitePerTable ss ON s.TaskName = ss.TaskName
ORDER BY s.[TotalDurSec] DESC;
```

> **Note on duration formatting:** `CONVERT` with style 108 (time only) is wrong for **Total** duration when the sum of seconds is **over 24 hours** because time-of-day rolls over. The `RIGHT('0' + ...)` pattern above is safe for any total.

> **Tie for slowest site:** The `LEFT JOIN SlowestSitePerTable` can produce **two rows** for one `TaskName` (e.g. LiquidLog). Deduplicate in the client or use `STRING_AGG` / one row per `TaskName` with `ROW_NUMBER` as in the “fix duplicate rows” section above.

---

*Generated for BHG_DR ETL and Fabric wall-clock analysis.*
