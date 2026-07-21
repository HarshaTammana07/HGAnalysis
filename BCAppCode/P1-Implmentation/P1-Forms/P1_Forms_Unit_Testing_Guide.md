# P1 Forms Unit Testing Guide

This document is for validating P1 Forms Fabric Silver tables against BHG_DR.

## Scope

Validate these 9 P1 Forms tables:

| # | Method | BHG_DR / Fabric Silver Table |
| --- | --- | --- |
| 1 | `SaveComprehensiveAssessmentForm` | `tbl_ComprehensiveAssessmentForm` |
| 2 | `SaveEMFormPregnancy` | `tbl_EandMFormPregnancy` |
| 3 | `SaveEMFormMDM` | `tbl_EandMFormMDM` |
| 4 | `SaveDataForms` | `tbl_SF_DataForms` |
| 5 | `SaveSMSTextConsentForm` | `tbl_SMSTextConsentForm` |
| 6 | `SaveConsenttoMarketing` | `tbl_ConsenttoMarketing` |
| 7 | `SaveTakeHomeAgreementandDiversionControl` | `tbl_TakeHomeAgreementandDiversionControl` |
| 8 | `SaveTakeHomeRiskAssessment` | `tbl_TakeHomeRiskAssessment` |
| 9 | `SaveNewDischargeTransferPlanForm` | `tbl_NewDischargeTransferPlanForm` |

Test sites used:

```sql
('AHK', 'B12B', 'B24', 'B25', 'B26')
```

Current test run date:

```text
WorkDate = 2026-07-20
LookbackDays = 15
StartDate = 2026-07-05
```

If testing another run date, calculate:

```text
StartDate = WorkDate - LookbackDays
```

## Expected Schema Result

Schema validation already confirmed:

- Column counts match for all 9 tables.
- Column names match.
- Column order matches.
- No missing Fabric columns.
- No extra Fabric columns.
- Datatypes are compatible after Fabric/Spark normalization:
  - BHG_DR `varchar` / `nvarchar` = Fabric `string`
  - BHG_DR `datetime` = Fabric `timestamp`
  - BHG_DR `bit` = Fabric `boolean`
  - BHG_DR `int` = Fabric `int`

Nullable differences are acceptable unless explicit Delta constraints are later required. Fabric Silver may show nullable `YES` where BHG_DR shows `NO`.

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
WHERE s.name = 'pats'
  AND t.name IN (
      'tbl_ComprehensiveAssessmentForm',
      'tbl_EandMFormPregnancy',
      'tbl_EandMFormMDM',
      'tbl_SF_DataForms',
      'tbl_SMSTextConsentForm',
      'tbl_ConsenttoMarketing',
      'tbl_TakeHomeAgreementandDiversionControl',
      'tbl_TakeHomeRiskAssessment',
      'tbl_NewDischargeTransferPlanForm'
  )
ORDER BY t.name, c.column_id;
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
WHERE TABLE_SCHEMA = 'pats'
  AND LOWER(TABLE_NAME) IN (
      'tbl_comprehensiveassessmentform',
      'tbl_eandmformpregnancy',
      'tbl_eandmformmdm',
      'tbl_sf_dataforms',
      'tbl_smstextconsentform',
      'tbl_consenttomarketing',
      'tbl_takehomeagreementanddiversioncontrol',
      'tbl_takehomeriskassessment',
      'tbl_newdischargetransferplanform'
  )
ORDER BY TABLE_NAME, ORDINAL_POSITION;
```

## Column Count Validation - Fabric Silver

Run in Fabric Silver SQL endpoint:

```sql
SELECT
    TABLE_SCHEMA AS SchemaName,
    TABLE_NAME AS TableName,
    COUNT(*) AS ColumnCount
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'pats'
  AND LOWER(TABLE_NAME) IN (
      'tbl_comprehensiveassessmentform',
      'tbl_eandmformpregnancy',
      'tbl_eandmformmdm',
      'tbl_sf_dataforms',
      'tbl_smstextconsentform',
      'tbl_consenttomarketing',
      'tbl_takehomeagreementanddiversioncontrol',
      'tbl_takehomeriskassessment',
      'tbl_newdischargetransferplanform'
  )
GROUP BY TABLE_SCHEMA, TABLE_NAME
ORDER BY TABLE_NAME;
```

Expected counts:

| Table | Expected Column Count |
| --- | ---: |
| `tbl_ComprehensiveAssessmentForm` | 153 |
| `tbl_ConsenttoMarketing` | 18 |
| `tbl_EandMFormMDM` | 16 |
| `tbl_EandMFormPregnancy` | 43 |
| `tbl_NewDischargeTransferPlanForm` | 20 |
| `tbl_SF_DataForms` | 14 |
| `tbl_SMSTextConsentForm` | 18 |
| `tbl_TakeHomeAgreementandDiversionControl` | 22 |
| `tbl_TakeHomeRiskAssessment` | 20 |

## Count Validation - Full / `WHERE 1 = 1` Tables

These tables use full/no additional source filter logic for the selected sites:

- `tbl_ComprehensiveAssessmentForm`
- `tbl_EandMFormPregnancy`
- `tbl_EandMFormMDM`
- `tbl_SMSTextConsentForm`
- `tbl_TakeHomeRiskAssessment`

### BHG_DR

Run in BHG_DR:

```sql
SELECT 'tbl_ComprehensiveAssessmentForm' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_ComprehensiveAssessmentForm]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_EandMFormPregnancy' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_EandMFormPregnancy]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_EandMFormMDM' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_EandMFormMDM]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_SMSTextConsentForm' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_SMSTextConsentForm]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_TakeHomeRiskAssessment' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_TakeHomeRiskAssessment]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26');
```

### Fabric Silver

Run in Fabric Silver SQL endpoint.

Fabric table names are shown lowercase here:

```sql
SELECT 'tbl_comprehensiveassessmentform' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_comprehensiveassessmentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_eandmformpregnancy' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_eandmformpregnancy]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_eandmformmdm' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_eandmformmdm]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_smstextconsentform' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_smstextconsentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')

UNION ALL
SELECT 'tbl_takehomeriskassessment' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_takehomeriskassessment]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26');
```

Expected result: BHG_DR and Fabric Silver counts should match for each table.

## Count Validation - Lookback Tables

These 4 tables use lookback date logic:

- `tbl_SF_DataForms`
- `tbl_ConsenttoMarketing`
- `tbl_TakeHomeAgreementandDiversionControl`
- `tbl_NewDischargeTransferPlanForm`

For the July 20, 2026 test run:

```sql
DECLARE @StartDate date = '2026-07-05';
```

### BHG_DR

Run in BHG_DR:

```sql
DECLARE @StartDate date = '2026-07-05';

SELECT 'tbl_SF_DataForms' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_SF_DataForms]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(LastUpdatedOn AS date) >= @StartDate
  )

UNION ALL
SELECT 'tbl_ConsenttoMarketing' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_ConsenttoMarketing]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )

UNION ALL
SELECT 'tbl_TakeHomeAgreementandDiversionControl' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_TakeHomeAgreementandDiversionControl]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )

UNION ALL
SELECT 'tbl_NewDischargeTransferPlanForm' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_NewDischargeTransferPlanForm]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  );
```

### Fabric Silver

Run in Fabric Silver SQL endpoint.

Fabric table names are shown lowercase here:

```sql
DECLARE @StartDate date = '2026-07-05';

SELECT 'tbl_sf_dataforms' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_sf_dataforms]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(LastUpdatedOn AS date) >= @StartDate
  )

UNION ALL
SELECT 'tbl_consenttomarketing' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_consenttomarketing]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )

UNION ALL
SELECT 'tbl_takehomeagreementanddiversioncontrol' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_takehomeagreementanddiversioncontrol]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )

UNION ALL
SELECT 'tbl_newdischargetransferplanform' AS TableName, COUNT(*) AS RowCount
FROM [pats].[tbl_newdischargetransferplanform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  );
```

Expected result: BHG_DR and Fabric Silver counts should match for each lookback table when both are tested for the same sites and same `StartDate`.

## Per-Site Count Validation

Use this when total counts do not match and the tester needs to locate which site differs.

Example for `tbl_SF_DataForms`.

### BHG_DR

```sql
DECLARE @StartDate date = '2026-07-05';

SELECT SiteCode, COUNT(*) AS RowCount
FROM [pats].[tbl_SF_DataForms]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(LastUpdatedOn AS date) >= @StartDate
  )
GROUP BY SiteCode
ORDER BY SiteCode;
```

### Fabric Silver

```sql
DECLARE @StartDate date = '2026-07-05';

SELECT SiteCode, COUNT(*) AS RowCount
FROM [pats].[tbl_sf_dataforms]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(LastUpdatedOn AS date) >= @StartDate
  )
GROUP BY SiteCode
ORDER BY SiteCode;
```

## Business Key Validation

Use this to check duplicate business keys in Fabric Silver.

Most tables use:

```text
SiteCode + Id
```

Pregnancy uses:

```text
SiteCode + EandMFormId
```

### Example: `SiteCode + Id`

```sql
SELECT SiteCode, Id, COUNT(*) AS DuplicateCount
FROM [pats].[tbl_comprehensiveassessmentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, Id
HAVING COUNT(*) > 1;
```

### Example: Pregnancy `SiteCode + EandMFormId`

```sql
SELECT SiteCode, EandMFormId, COUNT(*) AS DuplicateCount
FROM [pats].[tbl_eandmformpregnancy]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EandMFormId
HAVING COUNT(*) > 1;
```

Expected result: no duplicate rows.

## Tester Notes

- Always use the same site list in BHG_DR and Fabric Silver.
- Always use the same `StartDate` for the 4 lookback tables.
- Full tables should be tested by site only.
- Lookback tables should be tested by site plus the correct date condition.
- If BHG_DR is refreshed at a different time than Fabric, small count differences can happen for active/live source data.
- For schema checks, nullable differences are acceptable unless the project explicitly requires Delta constraints.
