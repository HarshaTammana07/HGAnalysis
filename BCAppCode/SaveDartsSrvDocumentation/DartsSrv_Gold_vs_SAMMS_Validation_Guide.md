# DartsSrv Gold Validation Guide

This document explains how to validate the Fabric DartsSrv Gold table against the correct source system, and why validating against `BHG_DR.dbo.vw_DartsSrv` can produce misleading results.

## Correct Validation Source

The Fabric DartsSrv pipeline reads directly from each live SAMMS site database:

```sql
[SAMMS-Ahoskie].dbo.tblDartsSrv
[SAMMS-ChesapeakeSouthV5].dbo.tblDartsSrv
...
```

The Fabric Gold table is:

```sql
[bhg_gold].[pats].[gd_darts_srv]
```

So the correct primary validation is:

```text
Live SAMMS dbo.tblDartsSrv
vs
Fabric Gold pats.gd_darts_srv
```

Do not use `BHG_DR.dbo.vw_DartsSrv` as the primary validation source for this pipeline.

## Why BHG_DR Is Not The Correct Primary Comparison

`BHG_DR` is a downstream/reporting copy. It is not the same source snapshot that Fabric reads.

Main reasons `BHG_DR` can mismatch Fabric:

- Fabric reads live SAMMS site databases directly.
- `BHG_DR` is refreshed separately and may be older than the Fabric run.
- `BHG_DR` uses `vw_DartsSrv`, while Fabric reads `dbo.tblDartsSrv`.
- The view can include separate transformation/filter logic.
- SAMMS is live and can receive new/updated rows after either system runs.
- Fabric `LastModAt` is the Fabric update timestamp from Silver, not a source extraction timestamp.

Example mismatch pattern:

```text
BHG_DR last refreshed last night
Fabric pulled live SAMMS this morning
Tester queries live SAMMS later in the day
```

All three can produce different counts even when the Fabric pipeline is correct.

## DartsSrv Source Filter

Use the same filter as the Fabric pipeline and old C# logic.

Default lookback:

```text
15 days
```

For a run date of `2026-06-09`, the default start date is:

```sql
DECLARE @StartDate date = '2026-05-25';
```

The Darts filter is:

```sql
WHERE dsClt IS NOT NULL
  AND (
        CONVERT(date, dsDtStart) >= @StartDate
     OR CONVERT(date, dsDtAdded) >= @StartDate
     OR CONVERT(date, dsUpdate) >= @StartDate
     OR CONVERT(date, dsBilled) >= @StartDate
     OR CONVERT(date, dsSigDate) >= @StartDate
     OR dsClt <= 0
  )
```

Important notes:

- There is no upper-bound date in the original logic.
- Future `dsDtStart` rows are included.
- `dsClt <= 0` bypasses the date window and can bring old rows into the result.

## Step 1: Identify Site Database

Use `AllsiteCodesAndDatabses.txt` or Fabric `site_mapping`.

Example:

```text
SiteCode: B42D
Database: SAMMS-ChesapeakeSouthV5
```

Optional Fabric check:

```sql
SELECT
    SiteCode,
    ClinicName,
    DataBaseName,
    ETLName,
    MethodName,
    IsActive
FROM bhg_bronze.dbo.site_mapping
WHERE SiteCode = 'B42D'
  AND ETLName = 'Darts'
  AND MethodName = 'DartsSrv';
```

## Step 2: Count From Live SAMMS Source

Run this against the SAMMS/source SQL Server.

Replace:

- `B42D`
- `SAMMS-ChesapeakeSouthV5`
- `@StartDate`

```sql
DECLARE @StartDate date = '2026-05-25';

SELECT
    'B42D' AS SiteCode,
    COUNT(*) AS samms_count,
    COUNT(DISTINCT dsID) AS samms_distinct_dsid,
    MIN(dsDtStart) AS min_dsdtstart,
    MAX(dsDtStart) AS max_dsdtstart
FROM [SAMMS-ChesapeakeSouthV5].dbo.tblDartsSrv
WHERE dsClt IS NOT NULL
  AND (
        CONVERT(date, dsDtStart) >= @StartDate
     OR CONVERT(date, dsDtAdded) >= @StartDate
     OR CONVERT(date, dsUpdate) >= @StartDate
     OR CONVERT(date, dsBilled) >= @StartDate
     OR CONVERT(date, dsSigDate) >= @StartDate
     OR dsClt <= 0
  );
```

## Step 3: Count From Fabric Gold

Run this against the Fabric Gold Warehouse or SQL endpoint.

Replace:

- `B42D`
- `@StartDate`

```sql
DECLARE @StartDate date = '2026-05-25';

SELECT
    COUNT(*) AS fabric_count,
    COUNT(DISTINCT DsId) AS fabric_distinct_dsid,
    MIN(DsDtStart) AS min_dsdtstart,
    MAX(DsDtStart) AS max_dsdtstart
FROM [bhg_gold].[pats].[gd_darts_srv]
WHERE SiteCode = 'B42D'
  AND DsClt IS NOT NULL
  AND (
        CONVERT(date, DsDtStart) >= @StartDate
     OR CONVERT(date, DsDtAdded) >= @StartDate
     OR CONVERT(date, DsUpdate) >= @StartDate
     OR CONVERT(date, DsBilled) >= @StartDate
     OR CONVERT(date, DsSigDate) >= @StartDate
     OR DsClt <= 0
  );
```

## Step 4: Compare Results

Compare these values:

```text
samms_distinct_dsid
fabric_distinct_dsid
```

Use distinct `dsID`/`DsId` as the main row coverage check.

Example from testing:

```text
SiteCode: B42D
SAMMS distinct dsID: 25,868
Fabric distinct DsId: 25,807
Difference: 61 rows
Difference percent: 0.24%
```

Another example:

```text
SiteCode: AHK
SAMMS distinct dsID: 3,746
Fabric distinct DsId: 3,722
Difference: 24 rows
Difference percent: 0.64%
```

These small differences are consistent with timing if SAMMS changed after Fabric extracted the site.

## Step 5: Check Fabric Extraction Time

Use Bronze only for the current extraction timing. Bronze is append-only, so do not compare total Bronze row counts across all time.

Run this in Fabric:

```sql
SELECT
    MIN(_extracted_at) AS fabric_extract_start,
    MAX(_extracted_at) AS fabric_extract_end,
    COUNT(DISTINCT dsID) AS bronze_distinct_dsid
FROM bhg_bronze.dbo.br_tblDartSrv
WHERE _site_code = 'B42D'
  AND CAST(_extracted_at AS date) = '2026-06-09';
```

Use `fabric_extract_end` in the next SAMMS query.

## Step 6: Check Rows Added Or Changed After Fabric Extract

Run this against SAMMS.

Replace `@FabricExtractEnd` with the value from Step 5.

```sql
DECLARE @StartDate date = '2026-05-25';
DECLARE @FabricExtractEnd datetime = '2026-06-09 09:00:00';

SELECT
    COUNT(*) AS samms_rows_after_fabric_extract,
    COUNT(DISTINCT dsID) AS samms_distinct_dsid_after_fabric_extract
FROM [SAMMS-ChesapeakeSouthV5].dbo.tblDartsSrv
WHERE dsClt IS NOT NULL
  AND (
        CONVERT(date, dsDtStart) >= @StartDate
     OR CONVERT(date, dsDtAdded) >= @StartDate
     OR CONVERT(date, dsUpdate) >= @StartDate
     OR CONVERT(date, dsBilled) >= @StartDate
     OR CONVERT(date, dsSigDate) >= @StartDate
     OR dsClt <= 0
  )
  AND (
        dsDtAdded > @FabricExtractEnd
     OR dsUpdate > @FabricExtractEnd
     OR dsBilled > @FabricExtractEnd
     OR dsSigDate > @FabricExtractEnd
  );
```

Interpretation:

```text
If this count is close to the difference between SAMMS and Fabric, the mismatch is timing.
```

## Step 7: Check Silver Seen Today

This proves whether Fabric actually pulled the site rows in the latest run.

Run this in Fabric:

```sql
DECLARE @StartDate date = '2026-05-25';

SELECT
    COUNT(*) AS silver_count,
    COUNT(DISTINCT dsID) AS silver_distinct_dsid,
    COUNT(DISTINCT CASE WHEN CAST(last_seen_at AS date) = '2026-06-09' THEN dsID END) AS seen_today,
    COUNT(DISTINCT CASE WHEN CAST(silver_updated_at AS date) = '2026-06-09' THEN dsID END) AS changed_today,
    MIN(dsDtStart) AS min_dsdtstart,
    MAX(dsDtStart) AS max_dsdtstart
FROM bhg_silver.pats.sl_tbldartsrv
WHERE _site_code = 'B42D'
  AND dsClt IS NOT NULL
  AND (
        CONVERT(date, dsDtStart) >= @StartDate
     OR CONVERT(date, dsDtAdded) >= @StartDate
     OR CONVERT(date, dsUpdate) >= @StartDate
     OR CONVERT(date, dsBilled) >= @StartDate
     OR CONVERT(date, dsSigDate) >= @StartDate
     OR dsClt <= 0
  );
```

Interpretation:

```text
seen_today = rows extracted/seen by Fabric in today's run
changed_today = rows inserted or updated in Fabric because RowChkSum changed
```

Do not use `changed_today` as total source coverage. It only means new or changed rows.

## Step 8: Drill Into Missing IDs

If the count difference is not explained by timing, compare IDs.

SAMMS source ID query:

```sql
DECLARE @StartDate date = '2026-05-25';

SELECT DISTINCT
    dsID
FROM [SAMMS-ChesapeakeSouthV5].dbo.tblDartsSrv
WHERE dsClt IS NOT NULL
  AND (
        CONVERT(date, dsDtStart) >= @StartDate
     OR CONVERT(date, dsDtAdded) >= @StartDate
     OR CONVERT(date, dsUpdate) >= @StartDate
     OR CONVERT(date, dsBilled) >= @StartDate
     OR CONVERT(date, dsSigDate) >= @StartDate
     OR dsClt <= 0
  )
ORDER BY dsID;
```

Fabric Gold ID query:

```sql
DECLARE @StartDate date = '2026-05-25';

SELECT DISTINCT
    DsId
FROM [bhg_gold].[pats].[gd_darts_srv]
WHERE SiteCode = 'B42D'
  AND DsClt IS NOT NULL
  AND (
        CONVERT(date, DsDtStart) >= @StartDate
     OR CONVERT(date, DsDtAdded) >= @StartDate
     OR CONVERT(date, DsUpdate) >= @StartDate
     OR CONVERT(date, DsBilled) >= @StartDate
     OR CONVERT(date, DsSigDate) >= @StartDate
     OR DsClt <= 0
  )
ORDER BY DsId;
```

If testers load the SAMMS ID list into a Fabric validation table, use this pattern:

```sql
SELECT
    s.DsId
FROM dbo.validation_samms_darts_ids s
LEFT JOIN [bhg_gold].[pats].[gd_darts_srv] g
    ON g.SiteCode = s.SiteCode
   AND g.DsId = s.DsId
WHERE s.SiteCode = 'B42D'
  AND g.DsId IS NULL;
```

Reverse check:

```sql
SELECT
    g.DsId
FROM [bhg_gold].[pats].[gd_darts_srv] g
LEFT JOIN dbo.validation_samms_darts_ids s
    ON s.SiteCode = g.SiteCode
   AND s.DsId = g.DsId
WHERE g.SiteCode = 'B42D'
  AND s.DsId IS NULL;
```

## Step 9: Check Future-Date Impact

DartsSrv can contain future service dates. The original logic includes them because it only checks `>= @StartDate`.

Fabric:

```sql
SELECT
    COUNT(*) AS future_count,
    COUNT(DISTINCT DsId) AS future_distinct_dsid,
    MIN(DsDtStart) AS min_future_dsdtstart,
    MAX(DsDtStart) AS max_future_dsdtstart
FROM [bhg_gold].[pats].[gd_darts_srv]
WHERE SiteCode = 'B42D'
  AND DsDtStart > CAST(GETDATE() AS date);
```

SAMMS:

```sql
SELECT
    COUNT(*) AS future_count,
    COUNT(DISTINCT dsID) AS future_distinct_dsid,
    MIN(dsDtStart) AS min_future_dsdtstart,
    MAX(dsDtStart) AS max_future_dsdtstart
FROM [SAMMS-ChesapeakeSouthV5].dbo.tblDartsSrv
WHERE dsDtStart > CAST(GETDATE() AS date);
```

## Step 10: Check dsClt <= 0 Impact

The `dsClt <= 0` condition can pull historical placeholder rows.

Fabric:

```sql
SELECT
    COUNT(*) AS negative_dsclt_count,
    COUNT(DISTINCT DsId) AS negative_dsclt_distinct_dsid
FROM [bhg_gold].[pats].[gd_darts_srv]
WHERE SiteCode = 'B42D'
  AND DsClt <= 0;
```

SAMMS:

```sql
SELECT
    COUNT(*) AS negative_dsclt_count,
    COUNT(DISTINCT dsID) AS negative_dsclt_distinct_dsid
FROM [SAMMS-ChesapeakeSouthV5].dbo.tblDartsSrv
WHERE dsClt <= 0;
```

## BHG_DR Query For Reference Only

This can be used as an informational check, but not as the primary pass/fail validation.

```sql
DECLARE @StartDate date = '2026-05-25';

SELECT
    COUNT(*) AS bhg_dr_count,
    COUNT(DISTINCT dsID) AS bhg_dr_distinct_dsid,
    MIN(dsDtStart) AS min_dsdtstart,
    MAX(dsDtStart) AS max_dsdtstart
FROM dbo.vw_DartsSrv
WHERE SiteCode = 'B42D'
  AND dsClt IS NOT NULL
  AND (
        CONVERT(date, dsDtStart) >= @StartDate
     OR CONVERT(date, dsDtAdded) >= @StartDate
     OR CONVERT(date, dsUpdate) >= @StartDate
     OR CONVERT(date, DSbilled) >= @StartDate
     OR CONVERT(date, dsSigDate) >= @StartDate
     OR dsClt <= 0
  );
```

Use this result only to understand downstream drift.

Do not fail Fabric validation only because `BHG_DR` count differs.

## Pass/Fail Guidance

Pass conditions:

- Fabric Gold count is equal or close to live SAMMS count using the same Darts filter.
- Difference is explained by SAMMS rows added/updated after Fabric extraction.
- `seen_today` in Silver is close to the Fabric Gold count for the same site/filter.
- Date range and distinct ID coverage are aligned.

Investigate conditions:

- Fabric is missing rows that existed in SAMMS before `fabric_extract_end`.
- Bronze has the rows, but Silver or Gold does not.
- `seen_today` is much lower than expected.
- Missing IDs are not explained by timing.

## Summary For Testers

Use this validation order:

```text
1. Get site database name.
2. Run SAMMS tblDartsSrv count with Darts filter.
3. Run Fabric Gold count with the same Darts filter.
4. Compare distinct dsID/DsId.
5. Check Fabric extraction time.
6. Check SAMMS rows added/changed after extraction.
7. Use BHG_DR only as reference, not pass/fail.
```

Final conclusion:

```text
The correct validation source for DartsSrv Fabric Gold is live SAMMS dbo.tblDartsSrv.
BHG_DR.dbo.vw_DartsSrv is a downstream copy and may not match due to refresh timing, view logic, and different source snapshot timing.
```
