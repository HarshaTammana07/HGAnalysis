USE [BHG_DR];
GO

/*
    P1 Finance metadata exports for Regional Finance 14-table scope.

    Run in Azure SQL BHG_DR, then save the result sets into this folder:
      1. finance_vw_MapAction.csv
      2. finance_vw_MapSrc2Dsn.csv
      3. finance_map_coverage_check.csv

    Scope notes:
      - Regional Finance excludes pats.tbl_FeeSched and pats.tbl_GlobalPayor.
      - ClientDemo is special: active copy loads stg.ClientDemo, then SQL merge
        procedures split rows into pats.tbl_ClientDemo1 and pats.tbl_ClientDemo2.
        The strict final-table query below intentionally still captures the
        final pats ClientDemo1/2 mapping rows, even though they are disabled in
        vw_MapAction, because those rows hold final column split metadata.
*/

DECLARE @FinanceTables TABLE
(
    TableOrder int NOT NULL,
    SchemaName sysname NOT NULL,
    TableName sysname NOT NULL
);

INSERT INTO @FinanceTables (TableOrder, SchemaName, TableName)
VALUES
    (1,  'pats', 'tbl_Bills'),
    (2,  'pats', 'tbl_pbi3PayAuth'),
    (3,  'pats', 'tbl_vw3pBillSub'),
    (4,  'pats', 'tbl_Fmp'),
    (5,  'pats', 'tbl_PayerCltHistory'),
    (6,  'pats', 'tbl_FinancialHardshipApplication'),
    (7,  'pats', 'tbl_3pElig'),
    (8,  'pats', 'tbl_ClaimLineItem'),
    (9,  'pats', 'tbl_ClaimLineItemActivity'),
    (10, 'pats', 'tbl_Claims'),
    (11, 'pats', 'tbl_PayerClient'),
    (12, 'pats', 'tbl_tbldiag10'),
    (13, 'pats', 'tbl_ClientDemo1'),
    (14, 'pats', 'tbl_ClientDemo2');

/* -------------------------------------------------------------------------
   1. vw_MapAction rows for the 14 final Finance destinations.
   ------------------------------------------------------------------------- */

SELECT
    'BHG_DR' AS SourceSystem,
    ft.TableOrder AS FinanceTableOrder,
    ft.SchemaName AS FinanceSchemaName,
    ft.TableName AS FinanceTableName,
    ma.TimeZone,
    ma.SiteCode,
    ma.ActionKey,
    ma.StepKey,
    ma.Enabled,
    ma.ConType,
    ma.ConnectionID,
    ma.ConName,
    ma.ConStr,
    ma.dbName,
    ma.CtrlMethod,
    ma.EnrollCutoff,
    ma.ContractDate,
    ma.ClinicName,
    ma.IsActive,
    ma.IsNewSchema,
    ma.SrcSchema,
    ma.FromTblVw,
    ma.DsnSchema,
    ma.DsnTbl,
    ma.WhereCondition,
    ma.SortOrder,
    ma.ReInitialize,
    ma.SchemaVersion,
    ma.CompKey,
    ma.RowTrax
FROM @FinanceTables ft
INNER JOIN dms.vw_MapAction ma
    ON ma.DsnSchema = ft.SchemaName
   AND LOWER(ma.DsnTbl) = LOWER(ft.TableName)
WHERE
    ma.ConnectionID = 2
    AND ma.ConName = 'SAMMS'
ORDER BY
    ft.TableOrder,
    ma.SiteCode,
    ma.ActionKey,
    ma.StepKey;

/* -------------------------------------------------------------------------
   2. vw_MapSrc2Dsn rows for the same 14 final Finance destinations.

   This joins through distinct ActionKey + StepKey from vw_MapAction so the
   field map is not multiplied by every site.
   ------------------------------------------------------------------------- */

WITH FinanceActionSteps AS
(
    SELECT DISTINCT
        ft.TableOrder,
        ft.SchemaName,
        ft.TableName,
        ma.ActionKey,
        ma.StepKey
    FROM @FinanceTables ft
    INNER JOIN dms.vw_MapAction ma
        ON ma.DsnSchema = ft.SchemaName
       AND LOWER(ma.DsnTbl) = LOWER(ft.TableName)
    WHERE
        ma.ConnectionID = 2
        AND ma.ConName = 'SAMMS'
)
SELECT
    'BHG_DR' AS SourceSystem,
    fas.TableOrder AS FinanceTableOrder,
    fas.SchemaName AS FinanceSchemaName,
    fas.TableName AS FinanceTableName,
    msd.ActionKey,
    msd.ActionStepKey,
    msd.FieldKey,
    msd.FieldName,
    msd.PHC_Enabled,
    msd.Enabled,
    msd.PrimaryKey,
    msd.FieldType,
    msd.FieldLength,
    msd.FieldPrecision,
    msd.FieldScale,
    msd.DsnFieldName,
    msd.Nullable,
    msd.[Default],
    msd.FormatConvert,
    msd.CompKey
FROM FinanceActionSteps fas
INNER JOIN dms.vw_MapSrc2Dsn msd
    ON msd.ActionKey = fas.ActionKey
   AND msd.ActionStepKey = fas.StepKey
ORDER BY
    fas.TableOrder,
    msd.ActionKey,
    msd.ActionStepKey,
    msd.FieldKey;

/* -------------------------------------------------------------------------
   3. Coverage check by final destination.
   ------------------------------------------------------------------------- */

SELECT
    ft.TableOrder,
    ft.SchemaName,
    ft.TableName,
    COUNT(ma.CompKey) AS MapActionRows,
    COUNT(DISTINCT ma.SiteCode) AS DistinctSites,
    COUNT(DISTINCT CONCAT(ma.ActionKey, '-', ma.StepKey)) AS DistinctActionSteps,
    SUM(CASE WHEN ma.Enabled = 1 THEN 1 ELSE 0 END) AS EnabledRows,
    SUM(CASE WHEN ma.Enabled = 0 THEN 1 ELSE 0 END) AS DisabledRows,
    MIN(ma.ActionKey) AS MinActionKey,
    MAX(ma.ActionKey) AS MaxActionKey,
    MIN(ma.StepKey) AS MinStepKey,
    MAX(ma.StepKey) AS MaxStepKey
FROM @FinanceTables ft
LEFT JOIN dms.vw_MapAction ma
    ON ma.DsnSchema = ft.SchemaName
   AND LOWER(ma.DsnTbl) = LOWER(ft.TableName)
   AND ma.ConnectionID = 2
   AND ma.ConName = 'SAMMS'
GROUP BY
    ft.TableOrder,
    ft.SchemaName,
    ft.TableName
ORDER BY
    ft.TableOrder;

/* -------------------------------------------------------------------------
   4. Active copy path for ClientDemo.

   Use this alongside the final-table metadata above. It is one active SAMMS
   copy into stg.ClientDemo; final split happens later through
   stg.ClientDemoMerge1 and stg.ClientDemoMerge2.
   ------------------------------------------------------------------------- */

SELECT
    'BHG_DR' AS SourceSystem,
    'pats.tbl_ClientDemo1 + pats.tbl_ClientDemo2' AS FinalFinanceDestinations,
    ma.TimeZone,
    ma.SiteCode,
    ma.ActionKey,
    ma.StepKey,
    ma.Enabled,
    ma.ConType,
    ma.ConnectionID,
    ma.ConName,
    ma.ConStr,
    ma.dbName,
    ma.CtrlMethod,
    ma.EnrollCutoff,
    ma.ContractDate,
    ma.ClinicName,
    ma.IsActive,
    ma.IsNewSchema,
    ma.SrcSchema,
    ma.FromTblVw,
    ma.DsnSchema,
    ma.DsnTbl,
    ma.WhereCondition,
    ma.SortOrder,
    ma.ReInitialize,
    ma.SchemaVersion,
    ma.CompKey,
    ma.RowTrax
FROM dms.vw_MapAction ma
WHERE
    ma.ConnectionID = 2
    AND ma.ConName = 'SAMMS'
    AND ma.Enabled = 1
    AND ma.DsnSchema = 'stg'
    AND LOWER(ma.DsnTbl) = 'clientdemo'
ORDER BY
    ma.SiteCode,
    ma.ActionKey,
    ma.StepKey;
