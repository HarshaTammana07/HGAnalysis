# P1 Reference Unit Testing Guide

This document is for validating P1 Reference Fabric Silver tables against BHG_DR.

## Scope

Validate these 9 P1 Reference tables:

| # | Method | BHG_DR Table | Fabric Silver Table |
| --- | --- | --- | --- |
| 1 | `SaveClinic` | `ctrl.tbl_Clinic` | `ctrl.tbl_clinic` |
| 2 | `Save3pSetup` | `ctrl.tbl_3PSETUP` | `ctrl.tbl_3psetup` |
| 3 | `SaveCodes` | `pats.tbl_Codes` | `pats.tbl_codes` |
| 4 | `SaveServices` | `pats.tbl_SERVICES` | `pats.tbl_services` |
| 5 | `SavedropDownListItems` | `ctrl.tbl_DroDownListItems` | `ctrl.tbl_drodownlistitems` |
| 6 | `SaveCustomAnswers` | `pats.tbl_CustomAnswers` | `pats.tbl_customanswers` |
| 7 | `SaveCustomQuestions` | `pats.tbl_CustomQuestions` | `pats.tbl_customquestions` |
| 8 | `SavePreAdmissionV6` | `ayx.tbl_PreAdmission_V6` | `ayx.tbl_preadmission_v6` |
| 9 | `SavePreAdminReferrals` | `pats.tbl_PreadmissionReferralSource` | `pats.tbl_preadmissionreferralsource` |

Test sites used:

```sql
('AHK', 'B12B', 'B24', 'B25', 'B26')
```

Current test run date:

```text
WorkDate = 2026-07-21
```

P1 Reference is mostly full reference/dimension extraction. For count testing, use the same site list in BHG_DR and Fabric Silver.

## Expected Schema Result

Schema validation should confirm:

- Column names match for the expected BHG_DR business columns.
- Column order matches for the expected BHG_DR business columns.
- No expected BHG_DR columns are missing in Fabric Silver.
- Datatypes are compatible after Fabric/Spark normalization:
  - BHG_DR `varchar` / `nvarchar` / `ntext` = Fabric `string`
  - BHG_DR `datetime` = Fabric `timestamp`
  - BHG_DR `bit` = Fabric `boolean`
  - BHG_DR `int` = Fabric `int`
  - BHG_DR `bigint` = Fabric `bigint`

Nullable differences are acceptable unless explicit Delta constraints are later required. Fabric Silver may show nullable `YES` where BHG_DR shows `NO`.

Known schema note:

- `ctrl.tbl_Clinic` has additional Fabric compatibility/mapped columns in Silver. For this table, testers should validate that all expected BHG_DR columns exist and types are compatible. Do not treat extra Fabric Silver columns as a failure unless the final target contract changes.

## Schema Validation Query - BHG_DR

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
WHERE (
        s.name = 'ctrl'
        AND t.name IN ('tbl_Clinic', 'tbl_3PSETUP', 'tbl_DroDownListItems')
      )
   OR (
        s.name = 'pats'
        AND t.name IN (
            'tbl_Codes',
            'tbl_SERVICES',
            'tbl_CustomAnswers',
            'tbl_CustomQuestions',
            'tbl_PreadmissionReferralSource'
        )
      )
   OR (
        s.name = 'ayx'
        AND t.name = 'tbl_PreAdmission_V6'
      )
ORDER BY s.name, t.name, c.column_id;
```

## Schema Validation Query - Fabric Silver

Run in Fabric Silver SQL endpoint.

Use lowercase table names in Fabric Silver:

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
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (
        TABLE_SCHEMA = 'ctrl'
        AND LOWER(TABLE_NAME) IN ('tbl_clinic', 'tbl_3psetup', 'tbl_drodownlistitems')
      )
   OR (
        TABLE_SCHEMA = 'pats'
        AND LOWER(TABLE_NAME) IN (
            'tbl_codes',
            'tbl_services',
            'tbl_customanswers',
            'tbl_customquestions',
            'tbl_preadmissionreferralsource'
        )
      )
   OR (
        TABLE_SCHEMA = 'ayx'
        AND LOWER(TABLE_NAME) = 'tbl_preadmission_v6'
      )
ORDER BY TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION;
```

## Column Count Validation - Fabric Silver

Run in Fabric Silver SQL endpoint:

```sql
SELECT
    TABLE_SCHEMA AS SchemaName,
    TABLE_NAME AS TableName,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE (
        TABLE_SCHEMA = 'ctrl'
        AND LOWER(TABLE_NAME) IN ('tbl_clinic', 'tbl_3psetup', 'tbl_drodownlistitems')
      )
   OR (
        TABLE_SCHEMA = 'pats'
        AND LOWER(TABLE_NAME) IN (
            'tbl_codes',
            'tbl_services',
            'tbl_customanswers',
            'tbl_customquestions',
            'tbl_preadmissionreferralsource'
        )
      )
   OR (
        TABLE_SCHEMA = 'ayx'
        AND LOWER(TABLE_NAME) = 'tbl_preadmission_v6'
      )
GROUP BY TABLE_SCHEMA, TABLE_NAME
ORDER BY TABLE_SCHEMA, TABLE_NAME;
```

Expected Fabric Silver counts from the current implementation:

| Fabric Silver Table | Expected Column Count |
| --- | ---: |
| `ayx.tbl_preadmission_v6` | 57 |
| `ctrl.tbl_3psetup` | 30 |
| `ctrl.tbl_clinic` | 285 |
| `ctrl.tbl_drodownlistitems` | 5 |
| `pats.tbl_codes` | 39 |
| `pats.tbl_customanswers` | 8 |
| `pats.tbl_customquestions` | 6 |
| `pats.tbl_preadmissionreferralsource` | 29 |
| `pats.tbl_services` | 18 |

## Count Validation - All Tables

Run both sections using the same site list.

### BHG_DR

Run in BHG_DR:

```sql
SELECT 'ctrl.tbl_Clinic' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', PKEY)) AS DistinctKeyCount
FROM [ctrl].[tbl_Clinic]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'ctrl.tbl_3PSETUP' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', pID)) AS DistinctKeyCount
FROM [ctrl].[tbl_3PSETUP]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_Codes' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', cdeID)) AS DistinctKeyCount
FROM [pats].[tbl_Codes]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_SERVICES' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', sID)) AS DistinctKeyCount
FROM [pats].[tbl_SERVICES]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'ctrl.tbl_DroDownListItems' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM [ctrl].[tbl_DroDownListItems]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_CustomAnswers' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
FROM [pats].[tbl_CustomAnswers]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_CustomQuestions' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', cID)) AS DistinctKeyCount
FROM [pats].[tbl_CustomQuestions]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'ayx.tbl_PreAdmission_V6' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid)) AS DistinctKeyCount
FROM [ayx].[tbl_PreAdmission_V6]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_PreadmissionReferralSource' AS TableName, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM [pats].[tbl_PreadmissionReferralSource]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26');
```

### Fabric Silver

Run in Fabric Silver SQL endpoint.

Fabric table names are shown lowercase here:

```sql
SELECT 'ctrl.tbl_clinic' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', PKEY)) AS DistinctKeyCount
FROM [ctrl].[tbl_clinic]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'ctrl.tbl_3psetup' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', pID)) AS DistinctKeyCount
FROM [ctrl].[tbl_3psetup]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_codes' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', cdeID)) AS DistinctKeyCount
FROM [pats].[tbl_codes]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_services' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', sID)) AS DistinctKeyCount
FROM [pats].[tbl_services]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'ctrl.tbl_drodownlistitems' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM [ctrl].[tbl_drodownlistitems]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_customanswers' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
FROM [pats].[tbl_customanswers]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_customquestions' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', cID)) AS DistinctKeyCount
FROM [pats].[tbl_customquestions]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'ayx.tbl_preadmission_v6' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid)) AS DistinctKeyCount
FROM [ayx].[tbl_preadmission_v6]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'pats.tbl_preadmissionreferralsource' AS TableName, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM [pats].[tbl_preadmissionreferralsource]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26');
```

Expected result: BHG_DR and Fabric Silver row counts should match for each table when both are tested for the same site list.

## Per-Site Count Validation

Use this when total counts do not match and the tester needs to locate which site differs.

Example for `pats.tbl_CustomAnswers`.

### BHG_DR

```sql
SELECT SiteCode, COUNT_BIG(*) AS RowCount, COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
FROM [pats].[tbl_CustomAnswers]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY SiteCode;
```

### Fabric Silver

```sql
SELECT SiteCode, COUNT(*) AS RowCount, COUNT(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
FROM [pats].[tbl_customanswers]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY SiteCode;
```

## Business Key Validation

Use these checks to confirm there are no duplicate business keys in Fabric Silver.

```sql
SELECT 'ctrl.tbl_clinic' AS TableName, SiteCode, PKEY AS KeyValue, COUNT(*) AS DuplicateCount
FROM [ctrl].[tbl_clinic]
GROUP BY SiteCode, PKEY
HAVING COUNT(*) > 1

UNION ALL
SELECT 'ctrl.tbl_3psetup', SiteCode, pID, COUNT(*)
FROM [ctrl].[tbl_3psetup]
GROUP BY SiteCode, pID
HAVING COUNT(*) > 1

UNION ALL
SELECT 'pats.tbl_codes', SiteCode, cdeID, COUNT(*)
FROM [pats].[tbl_codes]
GROUP BY SiteCode, cdeID
HAVING COUNT(*) > 1

UNION ALL
SELECT 'pats.tbl_services', SiteCode, sID, COUNT(*)
FROM [pats].[tbl_services]
GROUP BY SiteCode, sID
HAVING COUNT(*) > 1

UNION ALL
SELECT 'ctrl.tbl_drodownlistitems', SiteCode, Id, COUNT(*)
FROM [ctrl].[tbl_drodownlistitems]
GROUP BY SiteCode, Id
HAVING COUNT(*) > 1

UNION ALL
SELECT 'pats.tbl_customquestions', SiteCode, cID, COUNT(*)
FROM [pats].[tbl_customquestions]
GROUP BY SiteCode, cID
HAVING COUNT(*) > 1

UNION ALL
SELECT 'pats.tbl_preadmissionreferralsource', SiteCode, Id, COUNT(*)
FROM [pats].[tbl_preadmissionreferralsource]
GROUP BY SiteCode, Id
HAVING COUNT(*) > 1;
```

Custom Answers and PreAdmission V6 use composite keys:

```sql
SELECT SiteCode, caID, caQID, caCLTID, COUNT(*) AS DuplicateCount
FROM [pats].[tbl_customanswers]
GROUP BY SiteCode, caID, caQID, caCLTID
HAVING COUNT(*) > 1;

SELECT SiteCode, PreAdmissionid, Clientid, COUNT(*) AS DuplicateCount
FROM [ayx].[tbl_preadmission_v6]
GROUP BY SiteCode, PreAdmissionid, Clientid
HAVING COUNT(*) > 1;
```

Expected result: no duplicate rows.

## Tester Notes

- Always use the same site list in BHG_DR and Fabric Silver.
- Fabric Silver table names are lowercase in these examples.
- Most P1 Reference tables are full reference/dimension loads for selected active sites.
- Count mismatches should be checked per site first.
- `tbl_Clinic` has extra Fabric Silver compatibility columns; validate required BHG_DR columns and row counts.
- If BHG_DR and Fabric are refreshed at different times, small differences can happen for live source data.
- For schema checks, nullable differences are acceptable unless the project explicitly requires Delta constraints.
