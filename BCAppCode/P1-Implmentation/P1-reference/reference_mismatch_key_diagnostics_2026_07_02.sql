/*
P1 Reference mismatch diagnostics for work date 2026-07-02.

Use this only for the four mismatching tables:
- pats.tbl_Codes
- pats.tbl_SERVICES
- pats.tbl_PreadmissionReferralSource
- ayx.tbl_PreAdmission_V6

Run Section 1 in the BHG_DR SQL database.
Run Sections 2 and 3 from Fabric SQL where bhg_silver and bhg_bronze are visible.
*/

/* --------------------------------------------------------------------------
   Section 1: BHG_DR key inventory
   -------------------------------------------------------------------------- */

WITH Sites (SiteCode) AS (
    SELECT 'B12B' UNION ALL
    SELECT 'B24' UNION ALL
    SELECT 'B25' UNION ALL
    SELECT 'B26' UNION ALL
    SELECT 'B28'
)
SELECT
    'BHG_DR' AS SystemName,
    'pats.tbl_Codes' AS TableName,
    c.SiteCode,
    CAST(c.cdeID AS varchar(100)) AS Key1,
    CAST(NULL AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(c.RowChkSum AS varchar(100)) AS RowChkSum,
    CAST(NULL AS varchar(20)) AS ActiveFlag
FROM [pats].[tbl_Codes] c
JOIN Sites s ON s.SiteCode = c.SiteCode

UNION ALL

SELECT
    'BHG_DR' AS SystemName,
    'pats.tbl_SERVICES' AS TableName,
    v.SiteCode,
    CAST(v.sID AS varchar(100)) AS Key1,
    CAST(NULL AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(v.RowChkSum AS varchar(100)) AS RowChkSum,
    CAST(v.IsActive AS varchar(20)) AS ActiveFlag
FROM [pats].[tbl_SERVICES] v
JOIN Sites s ON s.SiteCode = v.SiteCode

UNION ALL

SELECT
    'BHG_DR' AS SystemName,
    'pats.tbl_PreadmissionReferralSource' AS TableName,
    r.SiteCode,
    CAST(r.Id AS varchar(100)) AS Key1,
    CAST(NULL AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(NULL AS varchar(100)) AS RowChkSum,
    CAST(NULL AS varchar(20)) AS ActiveFlag
FROM [pats].[tbl_PreadmissionReferralSource] r
JOIN Sites s ON s.SiteCode = r.SiteCode

UNION ALL

SELECT
    'BHG_DR' AS SystemName,
    'ayx.tbl_PreAdmission_V6' AS TableName,
    p.SiteCode,
    CAST(p.PreAdmissionid AS varchar(100)) AS Key1,
    CAST(p.Clientid AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(p.RowChkSum AS varchar(100)) AS RowChkSum,
    CAST(p.RowState AS varchar(20)) AS ActiveFlag
FROM [ayx].[tbl_PreAdmission_V6] p
JOIN Sites s ON s.SiteCode = p.SiteCode
ORDER BY TableName, SiteCode, Key1, Key2;

/* --------------------------------------------------------------------------
   Section 2: Fabric Silver key inventory
   Run from bhg_silver SQL endpoint. Do not prefix table names with bhg_silver.
   -------------------------------------------------------------------------- */

WITH Sites (SiteCode) AS (
    SELECT 'B12B' UNION ALL
    SELECT 'B24' UNION ALL
    SELECT 'B25' UNION ALL
    SELECT 'B26' UNION ALL
    SELECT 'B28'
)
SELECT
    'FABRIC_SILVER' AS SystemName,
    'pats.tbl_Codes' AS TableName,
    c.SiteCode,
    CAST(c.cdeID AS varchar(100)) AS Key1,
    CAST(NULL AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(c.RowChkSum AS varchar(100)) AS RowChkSum,
    CAST(NULL AS varchar(20)) AS ActiveFlag
FROM [pats].[tbl_codes] c
JOIN Sites s ON s.SiteCode = c.SiteCode

UNION ALL

SELECT
    'FABRIC_SILVER' AS SystemName,
    'pats.tbl_SERVICES' AS TableName,
    v.SiteCode,
    CAST(v.sID AS varchar(100)) AS Key1,
    CAST(NULL AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(v.RowChkSum AS varchar(100)) AS RowChkSum,
    CAST(v.IsActive AS varchar(20)) AS ActiveFlag
FROM [pats].[tbl_services] v
JOIN Sites s ON s.SiteCode = v.SiteCode

UNION ALL

SELECT
    'FABRIC_SILVER' AS SystemName,
    'pats.tbl_PreadmissionReferralSource' AS TableName,
    r.SiteCode,
    CAST(r.Id AS varchar(100)) AS Key1,
    CAST(NULL AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(NULL AS varchar(100)) AS RowChkSum,
    CAST(NULL AS varchar(20)) AS ActiveFlag
FROM [pats].[tbl_preadmissionreferralsource] r
JOIN Sites s ON s.SiteCode = r.SiteCode

UNION ALL

SELECT
    'FABRIC_SILVER' AS SystemName,
    'ayx.tbl_PreAdmission_V6' AS TableName,
    p.SiteCode,
    CAST(p.PreAdmissionid AS varchar(100)) AS Key1,
    CAST(p.Clientid AS varchar(100)) AS Key2,
    CAST(NULL AS varchar(100)) AS Key3,
    CAST(p.RowChkSum AS varchar(100)) AS RowChkSum,
    CAST(p.RowState AS varchar(20)) AS ActiveFlag
FROM [ayx].[tbl_preadmission_v6] p
JOIN Sites s ON s.SiteCode = p.SiteCode
ORDER BY TableName, SiteCode, Key1, Key2;

/* --------------------------------------------------------------------------
   Section 3: Latest Bronze row counts by table/site/ingest run
   -------------------------------------------------------------------------- */

WITH Sites (SiteCode) AS (
    SELECT 'B12B' UNION ALL
    SELECT 'B24' UNION ALL
    SELECT 'B25' UNION ALL
    SELECT 'B26' UNION ALL
    SELECT 'B28'
),
BronzeCounts AS (
    SELECT
        'pats.tbl_Codes' AS TableName,
        b.SiteCode,
        b.IngestRunId,
        COUNT_BIG(*) AS [RowCount],
        MAX(b.ExtractedAt) AS MaxExtractedAt
    FROM [bhg_bronze].[P1Reference].[br_samms_codes] b
    JOIN Sites s ON s.SiteCode = b.SiteCode
    GROUP BY b.SiteCode, b.IngestRunId

    UNION ALL

    SELECT
        'pats.tbl_SERVICES' AS TableName,
        b.SiteCode,
        b.IngestRunId,
        COUNT_BIG(*) AS [RowCount],
        MAX(b.ExtractedAt) AS MaxExtractedAt
    FROM [bhg_bronze].[P1Reference].[br_samms_services] b
    JOIN Sites s ON s.SiteCode = b.SiteCode
    GROUP BY b.SiteCode, b.IngestRunId

    UNION ALL

    SELECT
        'pats.tbl_PreadmissionReferralSource' AS TableName,
        b.SiteCode,
        b.IngestRunId,
        COUNT_BIG(*) AS [RowCount],
        MAX(b.ExtractedAt) AS MaxExtractedAt
    FROM [bhg_bronze].[P1Reference].[br_samms_preadmission_referral_source] b
    JOIN Sites s ON s.SiteCode = b.SiteCode
    GROUP BY b.SiteCode, b.IngestRunId

    UNION ALL

    SELECT
        'ayx.tbl_PreAdmission_V6' AS TableName,
        b.SiteCode,
        b.IngestRunId,
        COUNT_BIG(*) AS [RowCount],
        MAX(b.ExtractedAt) AS MaxExtractedAt
    FROM [bhg_bronze].[P1Reference].[br_samms_pre_admission_v6] b
    JOIN Sites s ON s.SiteCode = b.SiteCode
    GROUP BY b.SiteCode, b.IngestRunId
),
Ranked AS (
    SELECT
        *,
        ROW_NUMBER() OVER (
            PARTITION BY TableName, SiteCode
            ORDER BY MaxExtractedAt DESC, IngestRunId DESC
        ) AS rn
    FROM BronzeCounts
)
SELECT
    TableName,
    SiteCode,
    IngestRunId,
    [RowCount],
    MaxExtractedAt
FROM Ranked
WHERE rn = 1
ORDER BY TableName, SiteCode;

/* --------------------------------------------------------------------------
   Section 4: Services active/inactive check in Fabric Silver
   -------------------------------------------------------------------------- */

WITH Sites (SiteCode) AS (
    SELECT 'B12B' UNION ALL
    SELECT 'B24' UNION ALL
    SELECT 'B25' UNION ALL
    SELECT 'B26' UNION ALL
    SELECT 'B28'
)
SELECT
    v.SiteCode,
    COUNT_BIG(*) AS [TotalRows],
    SUM(CASE WHEN v.IsActive = 1 THEN 1 ELSE 0 END) AS [ActiveRows],
    SUM(CASE WHEN v.IsActive = 0 OR v.IsActive IS NULL THEN 1 ELSE 0 END) AS [InactiveRows]
FROM [pats].[tbl_services] v
JOIN Sites s ON s.SiteCode = v.SiteCode
GROUP BY v.SiteCode
ORDER BY v.SiteCode;
