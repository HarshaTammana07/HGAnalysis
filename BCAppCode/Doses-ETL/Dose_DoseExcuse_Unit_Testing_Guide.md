# Dose / DoseExcuse Unit Testing Guide

This document is for validating Fabric Silver Dose tables against BHG_DR.

## Scope

Validate these two tables:

| # | Method | BHG_DR Table | Fabric Silver Table |
| --- | --- | --- | --- |
| 1 | `Dose` | `pats.tbl_DOSE` | `bhg_silver.pats.tbl_dose` |
| 2 | `DoseExcuse` | `pats.tbl_DOSE_Excuse` | `bhg_silver.pats.tbl_dose_excuse` |

Default test sites used during validation:

```sql
('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
```

If a site has no rows in either system, it may not appear in grouped count results.

Use the pipeline run date as `WorkDate`.

Example:

```text
WorkDate = 2026-07-17
LookbackDays = 15
```

## Expected Result Summary

Schema validation should confirm:

- `tbl_DOSE` has 31 columns and Fabric `tbl_dose` has the same business columns.
- `tbl_DOSE_Excuse` has 10 columns and Fabric `tbl_dose_excuse` has the same business columns.
- Column names match logically. Case differences are acceptable, for example `DoseID` vs `DoseId`.
- Fabric `datetime2` is compatible with BHG_DR `datetime`.
- Fabric `varchar(8000)` is acceptable for Silver even when BHG_DR has smaller `varchar` lengths, as long as no truncation occurs.
- Fabric nullable differences are acceptable unless explicit Delta constraints are required later.

Known datatype notes:

- BHG_DR `dosesig` is `ntext`; Fabric `Dosesig` is `varchar(8000)`.
- BHG_DR `strEXCUSED` is `ntext`; Fabric `StrExcused` is `varchar(8000)`.
- BHG_DR `DoseSigImg` is `varbinary(max)`; Fabric `DoseSigImg` is `varbinary(8000)`.

## Important Logic Notes

### DoseExcuse

DoseExcuse source extraction is effectively full table for the selected site.

Legacy behavior:

1. Load all current source rows for the site.
2. Reset existing target rows for that site to `RowState = 0`.
3. Set rows returned from source back to `RowState = 1`.
4. Match by `SiteCode + ExID`.

BHG_DR may contain historical inactive rows from older legacy runs. If Fabric Silver was rebuilt from current source only, total counts may be lower because historical inactive rows are not present yet.

### Dose

Dose does not use a simple 15-day source window. Legacy/Fabric normal logic is:

- Special sites `V10A`, `CBCO`, `V21`, `V10`: `dtDate >= WorkDate - 1 month`
- All other sites: `dtDate >= WorkDate - 6 months`
- `dtDate <= WorkDate + 2 days`
- `CltID IS NOT NULL`
- year guard based on `WorkDate - LookbackDays - 1 year`

For Dose, do not compare full table counts between BHG_DR and Fabric Silver. BHG_DR has years of historical rows. Compare the same source/window logic instead.

Dose inactive rows are expected when:

```text
blVoid = 1 AND dtVoid = 1
OR CltID < 0 AND CltID <> -111
```

## Schema Validation - BHG_DR

Run in BHG_DR:

```sql
SELECT
    'BHG_DR' AS SourceSystem,
    s.name AS SchemaName,
    t.name AS TableName,
    c.name AS ColumnName,
    c.column_id AS OrdinalPosition,
    ty.name AS DataType,
    CASE
        WHEN ty.name IN ('varchar', 'char', 'nvarchar', 'nchar') AND c.max_length = -1 THEN -1
        WHEN ty.name IN ('nvarchar', 'nchar') THEN c.max_length / 2
        ELSE c.max_length
    END AS MaxLength,
    c.precision AS NumericPrecision,
    c.scale AS NumericScale,
    CASE WHEN c.is_nullable = 1 THEN 'YES' ELSE 'NO' END AS IsNullable
FROM sys.tables t
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
INNER JOIN sys.columns c
    ON t.object_id = c.object_id
INNER JOIN sys.types ty
    ON c.user_type_id = ty.user_type_id
WHERE s.name = 'pats'
  AND t.name IN ('tbl_DOSE', 'tbl_DOSE_Excuse')
ORDER BY t.name, c.column_id;
```

## Schema Validation - Fabric Silver

Run in Fabric Silver SQL endpoint:

```sql
SELECT
    'FABRIC_SILVER' AS SourceSystem,
    TABLE_SCHEMA AS SchemaName,
    TABLE_NAME AS TableName,
    COLUMN_NAME AS ColumnName,
    ORDINAL_POSITION AS OrdinalPosition,
    DATA_TYPE AS DataType,
    CHARACTER_MAXIMUM_LENGTH AS MaxLength,
    NUMERIC_PRECISION AS NumericPrecision,
    NUMERIC_SCALE AS NumericScale,
    IS_NULLABLE AS IsNullable
FROM bhg_silver.INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'pats'
  AND TABLE_NAME IN ('tbl_dose', 'tbl_dose_excuse')
ORDER BY TABLE_NAME, ORDINAL_POSITION;
```

## Column Count Validation - BHG_DR

Run in BHG_DR:

```sql
SELECT
    s.name AS SchemaName,
    t.name AS TableName,
    COUNT(*) AS ColumnCount
FROM sys.tables t
INNER JOIN sys.schemas s
    ON t.schema_id = s.schema_id
INNER JOIN sys.columns c
    ON t.object_id = c.object_id
WHERE s.name = 'pats'
  AND t.name IN ('tbl_DOSE', 'tbl_DOSE_Excuse')
GROUP BY s.name, t.name
ORDER BY t.name;
```

Expected:

| BHG_DR Table | Expected Column Count |
| --- | ---: |
| `pats.tbl_DOSE` | 31 |
| `pats.tbl_DOSE_Excuse` | 10 |

## Column Count Validation - Fabric Silver

Run in Fabric Silver SQL endpoint:

```sql
SELECT
    TABLE_SCHEMA AS SchemaName,
    TABLE_NAME AS TableName,
    COUNT(*) AS ColumnCount
FROM bhg_silver.INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'pats'
  AND TABLE_NAME IN ('tbl_dose', 'tbl_dose_excuse')
GROUP BY TABLE_SCHEMA, TABLE_NAME
ORDER BY TABLE_NAME;
```

Expected:

| Fabric Silver Table | Expected Column Count |
| --- | ---: |
| `pats.tbl_dose` | 31 |
| `pats.tbl_dose_excuse` | 10 |

## DoseExcuse Count Validation - BHG_DR

Run in BHG_DR:

```sql
SELECT
    SiteCode,
    COUNT(*) AS bhg_dr_count,
    COUNT(DISTINCT ExID) AS bhg_dr_distinct_exid,
    SUM(CASE WHEN RowState = 1 THEN 1 ELSE 0 END) AS bhg_dr_active_count,
    SUM(CASE WHEN RowState = 0 THEN 1 ELSE 0 END) AS bhg_dr_inactive_count
FROM pats.tbl_DOSE_Excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode
ORDER BY SiteCode;
```

## DoseExcuse Count Validation - Fabric Silver

Run in Fabric Silver SQL endpoint:

```sql
SELECT
    SiteCode,
    COUNT(*) AS fabric_count,
    COUNT(DISTINCT ExId) AS fabric_distinct_exid,
    SUM(CASE WHEN RowState = 1 THEN 1 ELSE 0 END) AS fabric_active_count,
    SUM(CASE WHEN RowState = 0 THEN 1 ELSE 0 END) AS fabric_inactive_count
FROM bhg_silver.pats.tbl_dose_excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode
ORDER BY SiteCode;
```

### DoseExcuse Interpretation

- Active counts should match or be very close.
- Total counts may differ when BHG_DR has historical inactive rows that Fabric Silver has not backfilled.
- If BHG_DR has inactive rows and Fabric has zero inactive rows, confirm whether those rows are historical inactive records.

Optional BHG_DR inactive breakdown:

```sql
SELECT
    SiteCode,
    RowState,
    COUNT(*) AS row_count
FROM pats.tbl_DOSE_Excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, RowState
ORDER BY SiteCode, RowState;
```

Optional Fabric inactive breakdown:

```sql
SELECT
    SiteCode,
    RowState,
    COUNT(*) AS row_count
FROM bhg_silver.pats.tbl_dose_excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, RowState
ORDER BY SiteCode, RowState;
```

## Dose Window Count Validation - BHG_DR

Run in BHG_DR.

Set `@WorkDate` to the Fabric pipeline work date:

```sql
DECLARE @WorkDate date = '2026-07-17';
DECLARE @LookbackDays int = 15;

SELECT
    SiteCode,
    COUNT(*) AS bhg_dr_window_count,
    COUNT(DISTINCT DoseID) AS bhg_dr_window_distinct_doseid,
    SUM(CASE WHEN RowState = 1 THEN 1 ELSE 0 END) AS bhg_dr_window_active_count,
    SUM(CASE WHEN RowState = 0 THEN 1 ELSE 0 END) AS bhg_dr_window_inactive_count
FROM pats.tbl_DOSE
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  AND (
        YEAR(dtDate) >= YEAR(DATEADD(year, -1, DATEADD(day, -@LookbackDays, @WorkDate)))
     OR YEAR(dtMedDate) >= YEAR(DATEADD(year, -1, DATEADD(day, -@LookbackDays, @WorkDate)))
  )
  AND dtDate <= DATEADD(day, 2, @WorkDate)
  AND CltID IS NOT NULL
  AND (
        (SiteCode IN ('V10A', 'CBCO', 'V21', 'V10')
         AND dtDate >= DATEADD(month, -1, @WorkDate))
     OR (SiteCode NOT IN ('V10A', 'CBCO', 'V21', 'V10')
         AND dtDate >= DATEADD(month, -6, @WorkDate))
  )
GROUP BY SiteCode
ORDER BY SiteCode;
```

## Dose Window Count Validation - Fabric Silver

Run in Fabric Silver SQL endpoint.

Set `@WorkDate` to the Fabric pipeline work date:

```sql
DECLARE @WorkDate date = '2026-07-17';
DECLARE @LookbackDays int = 15;

SELECT
    SiteCode,
    COUNT(*) AS fabric_window_count,
    COUNT(DISTINCT DoseId) AS fabric_window_distinct_doseid,
    SUM(CASE WHEN RowState = 1 THEN 1 ELSE 0 END) AS fabric_window_active_count,
    SUM(CASE WHEN RowState = 0 THEN 1 ELSE 0 END) AS fabric_window_inactive_count
FROM bhg_silver.pats.tbl_dose
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  AND (
        YEAR(DtDate) >= YEAR(DATEADD(year, -1, DATEADD(day, -@LookbackDays, @WorkDate)))
     OR YEAR(DtMedDate) >= YEAR(DATEADD(year, -1, DATEADD(day, -@LookbackDays, @WorkDate)))
  )
  AND DtDate <= DATEADD(day, 2, @WorkDate)
  AND CltId IS NOT NULL
  AND (
        (SiteCode IN ('V10A', 'CBCO', 'V21', 'V10')
         AND DtDate >= DATEADD(month, -1, @WorkDate))
     OR (SiteCode NOT IN ('V10A', 'CBCO', 'V21', 'V10')
         AND DtDate >= DATEADD(month, -6, @WorkDate))
  )
GROUP BY SiteCode
ORDER BY SiteCode;
```

### Dose Interpretation

- Do not compare full BHG_DR Dose counts to Fabric Silver counts.
- Compare the same Dose window logic shown above.
- Small count differences are acceptable when BHG_DR and Fabric were run at different times because Dose data changes frequently.
- `COUNT(*)` and `COUNT(DISTINCT DoseID/DoseId)` should generally be equal per site. If not, investigate duplicate keys.

## Dose Inactive Count Validation

Use this query to prove why Fabric rows are inactive.

Run in Fabric Silver SQL endpoint:

```sql
SELECT
    SiteCode,
    COUNT(*) AS inactive_count,
    SUM(CASE WHEN BlVoid = 1 AND DtVoid = 1 THEN 1 ELSE 0 END) AS void_inactive_count,
    SUM(CASE WHEN CltId < 0 AND CltId <> -111 THEN 1 ELSE 0 END) AS negative_client_inactive_count,
    SUM(CASE
            WHEN NOT (BlVoid = 1 AND DtVoid = 1)
             AND NOT (CltId < 0 AND CltId <> -111)
            THEN 1 ELSE 0
        END) AS reset_not_reactivated_count
FROM bhg_silver.pats.tbl_dose
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  AND RowState = 0
GROUP BY SiteCode
ORDER BY SiteCode;
```

Expected:

- `void_inactive_count` explains rows made inactive by `BlVoid = 1 AND DtVoid = 1`.
- `negative_client_inactive_count` explains rows made inactive by negative client IDs.
- `reset_not_reactivated_count` should be reviewed if it is greater than zero.

Equivalent BHG_DR breakdown:

```sql
SELECT
    SiteCode,
    COUNT(*) AS inactive_count,
    SUM(CASE WHEN blVoid = 1 AND dtVoid = 1 THEN 1 ELSE 0 END) AS void_inactive_count,
    SUM(CASE WHEN CltID < 0 AND CltID <> -111 THEN 1 ELSE 0 END) AS negative_client_inactive_count,
    SUM(CASE
            WHEN NOT (blVoid = 1 AND dtVoid = 1)
             AND NOT (CltID < 0 AND CltID <> -111)
            THEN 1 ELSE 0
        END) AS reset_not_reactivated_count
FROM pats.tbl_DOSE
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  AND RowState = 0
GROUP BY SiteCode
ORDER BY SiteCode;
```

## Duplicate Key Checks

Run in BHG_DR:

```sql
SELECT SiteCode, DoseID, COUNT(*) AS duplicate_count
FROM pats.tbl_DOSE
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, DoseID
HAVING COUNT(*) > 1
ORDER BY SiteCode, DoseID;

SELECT SiteCode, ExID, COUNT(*) AS duplicate_count
FROM pats.tbl_DOSE_Excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, ExID
HAVING COUNT(*) > 1
ORDER BY SiteCode, ExID;
```

Run in Fabric Silver:

```sql
SELECT SiteCode, DoseId, COUNT(*) AS duplicate_count
FROM bhg_silver.pats.tbl_dose
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, DoseId
HAVING COUNT(*) > 1
ORDER BY SiteCode, DoseId;

SELECT SiteCode, ExId, COUNT(*) AS duplicate_count
FROM bhg_silver.pats.tbl_dose_excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, ExId
HAVING COUNT(*) > 1
ORDER BY SiteCode, ExId;
```

Expected:

- No duplicate rows should be returned.

## Optional Audit Validation

Run in Fabric SQL endpoint or Lakehouse SQL where `meta` tables are available:

```sql
SELECT
    ConfigId,
    TaskConfigId,
    TaskName,
    TableName,
    SiteCode,
    Status,
    RowsRead,
    RowsWritten,
    RowsFailed,
    ErrorMessage,
    StartTime,
    EndTime,
    PipelineRunId
FROM bhg_bronze.meta.taskaudit
WHERE ConfigId IN (7, 8)
  AND CAST(CreatedAt AS date) = CAST(GETDATE() AS date)
ORDER BY CreatedAt DESC, ConfigId, TaskConfigId;
```

Expected:

- Bronze tasks for active site/method rows should log per site.
- Silver tasks should log per method.
- If one site fails in Bronze, other successful sites should continue to Silver.
- Failed Bronze site should be recorded as failed/skipped through the method/site result handling.

## Tester Sign-Off Criteria

Pass criteria:

- Column count and business columns match for both tables.
- No duplicate business keys in Fabric Silver.
- DoseExcuse active counts match or are explainably close.
- Dose window counts match or are explainably close.
- Fabric Dose inactive rows are explained by legacy business rules, especially `BlVoid = 1 AND DtVoid = 1`.

Known acceptable differences:

- Fabric `datetime2` vs BHG_DR `datetime`.
- Fabric `varchar(8000)` vs exact BHG_DR string lengths.
- Fabric nullable `YES` where BHG_DR has `NO`.
- Total BHG_DR Dose counts are much larger because BHG_DR contains historical rows.
- BHG_DR DoseExcuse may have historical inactive rows not present in Fabric unless those rows are backfilled.
