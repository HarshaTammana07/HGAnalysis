/*
P1 Reference five-site count validation
Work date context: 2026-07-02

Sites:
  B12B, B24, B25, B26, B28

How to use:
1. Run section 1 in the old BHG_DR SQL database.
2. Run section 2 in the Fabric SQL endpoint that can see bhg_silver.
3. Export both result grids and compare by SiteCode + TableName.

If your Fabric SQL endpoint is already connected directly to bhg_silver,
replace [bhg_silver].[schema].[table] with [schema].[table].
*/

/* ============================================================
1. BHG_DR / SQL Server - row counts and distinct key counts
============================================================ */

DECLARE @WorkDate date = '2026-07-02';

WITH Counts (
    TableOrder,
    SystemName,
    WorkDateContext,
    SiteCode,
    TableName,
    [RowCount],
    DistinctKeyCount
) AS (
    SELECT 1 AS TableOrder, 'BHG_DR' AS SystemName, @WorkDate AS WorkDateContext,
           SiteCode, 'ctrl.tbl_Clinic' AS TableName,
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', PKEY)) AS DistinctKeyCount
    FROM [ctrl].[tbl_Clinic]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 2, 'BHG_DR', @WorkDate, SiteCode, 'ctrl.tbl_3PSETUP',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', pID)) AS DistinctKeyCount
    FROM [ctrl].[tbl_3PSETUP]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 3, 'BHG_DR', @WorkDate, SiteCode, 'pats.tbl_Codes',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', cdeID)) AS DistinctKeyCount
    FROM [pats].[tbl_Codes]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 4, 'BHG_DR', @WorkDate, SiteCode, 'pats.tbl_SERVICES',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', sID)) AS DistinctKeyCount
    FROM [pats].[tbl_SERVICES]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 5, 'BHG_DR', @WorkDate, SiteCode, 'ctrl.tbl_DroDownListItems',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
    FROM [ctrl].[tbl_DroDownListItems]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 6, 'BHG_DR', @WorkDate, SiteCode, 'pats.tbl_CustomAnswers',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
    FROM [pats].[tbl_CustomAnswers]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 7, 'BHG_DR', @WorkDate, SiteCode, 'pats.tbl_CustomQuestions',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', cID)) AS DistinctKeyCount
    FROM [pats].[tbl_CustomQuestions]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 8, 'BHG_DR', @WorkDate, SiteCode, 'ayx.tbl_PreAdmission_V6',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid)) AS DistinctKeyCount
    FROM [ayx].[tbl_PreAdmission_V6]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 9, 'BHG_DR', @WorkDate, SiteCode, 'pats.tbl_PreadmissionReferralSource',
           COUNT_BIG(*) AS [RowCount],
           COUNT_BIG(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
    FROM [pats].[tbl_PreadmissionReferralSource]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode
)
SELECT
    SystemName,
    WorkDateContext,
    SiteCode,
    TableName,
    [RowCount],
    DistinctKeyCount,
    [RowCount] - DistinctKeyCount AS DuplicateKeyCount
FROM Counts
ORDER BY SiteCode, TableOrder;


/* ============================================================
2. Fabric bhg_silver SQL endpoint - row counts and distinct key counts
============================================================ */

WITH Counts (
    TableOrder,
    SystemName,
    WorkDateContext,
    SiteCode,
    TableName,
    [RowCount],
    DistinctKeyCount
) AS (
    SELECT 1 AS TableOrder, 'bhg_silver' AS SystemName, CAST('2026-07-02' AS date) AS WorkDateContext,
           SiteCode, 'ctrl.tbl_Clinic' AS TableName,
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', PKEY)) AS DistinctKeyCount
    FROM [ctrl].[tbl_clinic]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 2, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'ctrl.tbl_3PSETUP',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', pID)) AS DistinctKeyCount
    FROM [ctrl].[tbl_3psetup]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 3, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'pats.tbl_Codes',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', cdeID)) AS DistinctKeyCount
    FROM [pats].[tbl_codes]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 4, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'pats.tbl_SERVICES',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', sID)) AS DistinctKeyCount
    FROM [pats].[tbl_services]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 5, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'ctrl.tbl_DroDownListItems',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
    FROM [ctrl].[tbl_drodownlistitems]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 6, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'pats.tbl_CustomAnswers',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
    FROM [pats].[tbl_customanswers]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 7, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'pats.tbl_CustomQuestions',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', cID)) AS DistinctKeyCount
    FROM [pats].[tbl_customquestions]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 8, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'ayx.tbl_PreAdmission_V6',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid)) AS DistinctKeyCount
    FROM [ayx].[tbl_preadmission_v6]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode

    UNION ALL
    SELECT 9, 'bhg_silver', CAST('2026-07-02' AS date), SiteCode, 'pats.tbl_PreadmissionReferralSource',
           COUNT(*) AS [RowCount],
           COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
    FROM [pats].[tbl_preadmissionreferralsource]
    WHERE SiteCode IN ('B12B', 'B24', 'B25', 'B26', 'B28')
    GROUP BY SiteCode
)
SELECT
    SystemName,
    WorkDateContext,
    SiteCode,
    TableName,
    [RowCount],
    DistinctKeyCount,
    [RowCount] - DistinctKeyCount AS DuplicateKeyCount
FROM Counts
ORDER BY SiteCode, TableOrder;




SAAMS against Silver


DECLARE @WorkDate date = '2026-07-02';
DECLARE @ReferralLookbackStart date = DATEADD(day, -515, @WorkDate);

WITH Counts AS (
    SELECT 'B12B' AS SiteCode, 'ctrl.tbl_3PSETUP' AS TableName, COUNT(*) AS [RowCount] FROM [SAMMS-ColoradoSpringsV5].[dbo].[tbl3PSETUP]
    UNION ALL SELECT 'B12B', 'pats.tbl_Codes', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[tblCodes]
    UNION ALL SELECT 'B12B', 'pats.tbl_SERVICES', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[tblSERVICES]
    UNION ALL SELECT 'B12B', 'ctrl.tbl_DroDownListItems', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[DroDownListItems]
    UNION ALL SELECT 'B12B', 'pats.tbl_CustomAnswers', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[tblCUSTOMANSWERS]
    UNION ALL SELECT 'B12B', 'pats.tbl_CustomQuestions', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[tblCUSTOMQUESTIONS]
    UNION ALL SELECT 'B12B', 'ayx.tbl_PreAdmission_V6', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[SF_PatientPreAdmission] pp WHERE LEN(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE '%test data%'
    UNION ALL SELECT 'B12B', 'pats.tbl_PreadmissionReferralSource', COUNT(*) FROM [SAMMS-ColoradoSpringsV5].[dbo].[SF_PatientPreadmissionReferralSource] WHERE ISNULL(LastUpdateOn, CreatedOn) >= @ReferralLookbackStart

    UNION ALL SELECT 'B24', 'ctrl.tbl_3PSETUP', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[tbl3PSETUP]
    UNION ALL SELECT 'B24', 'pats.tbl_Codes', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[tblCodes]
    UNION ALL SELECT 'B24', 'pats.tbl_SERVICES', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[tblSERVICES]
    UNION ALL SELECT 'B24', 'ctrl.tbl_DroDownListItems', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[DroDownListItems]
    UNION ALL SELECT 'B24', 'pats.tbl_CustomAnswers', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[tblCUSTOMANSWERS]
    UNION ALL SELECT 'B24', 'pats.tbl_CustomQuestions', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[tblCUSTOMQUESTIONS]
    UNION ALL SELECT 'B24', 'ayx.tbl_PreAdmission_V6', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[SF_PatientPreAdmission] pp WHERE LEN(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE '%test data%'
    UNION ALL SELECT 'B24', 'pats.tbl_PreadmissionReferralSource', COUNT(*) FROM [SAMMS-PaintsvilleV5].[dbo].[SF_PatientPreadmissionReferralSource] WHERE ISNULL(LastUpdateOn, CreatedOn) >= @ReferralLookbackStart

    UNION ALL SELECT 'B25', 'ctrl.tbl_3PSETUP', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[tbl3PSETUP]
    UNION ALL SELECT 'B25', 'pats.tbl_Codes', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[tblCodes]
    UNION ALL SELECT 'B25', 'pats.tbl_SERVICES', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[tblSERVICES]
    UNION ALL SELECT 'B25', 'ctrl.tbl_DroDownListItems', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[DroDownListItems]
    UNION ALL SELECT 'B25', 'pats.tbl_CustomAnswers', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[tblCUSTOMANSWERS]
    UNION ALL SELECT 'B25', 'pats.tbl_CustomQuestions', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[tblCUSTOMQUESTIONS]
    UNION ALL SELECT 'B25', 'ayx.tbl_PreAdmission_V6', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[SF_PatientPreAdmission] pp WHERE LEN(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE '%test data%'
    UNION ALL SELECT 'B25', 'pats.tbl_PreadmissionReferralSource', COUNT(*) FROM [SAMMS-PikevilleV5].[dbo].[SF_PatientPreadmissionReferralSource] WHERE ISNULL(LastUpdateOn, CreatedOn) >= @ReferralLookbackStart

    UNION ALL SELECT 'B26', 'ctrl.tbl_3PSETUP', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[tbl3PSETUP]
    UNION ALL SELECT 'B26', 'pats.tbl_Codes', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[tblCodes]
    UNION ALL SELECT 'B26', 'pats.tbl_SERVICES', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[tblSERVICES]
    UNION ALL SELECT 'B26', 'ctrl.tbl_DroDownListItems', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[DroDownListItems]
    UNION ALL SELECT 'B26', 'pats.tbl_CustomAnswers', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[tblCUSTOMANSWERS]
    UNION ALL SELECT 'B26', 'pats.tbl_CustomQuestions', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[tblCUSTOMQUESTIONS]
    UNION ALL SELECT 'B26', 'ayx.tbl_PreAdmission_V6', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[SF_PatientPreAdmission] pp WHERE LEN(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE '%test data%'
    UNION ALL SELECT 'B26', 'pats.tbl_PreadmissionReferralSource', COUNT(*) FROM [SAMMS-HazardV5].[dbo].[SF_PatientPreadmissionReferralSource] WHERE ISNULL(LastUpdateOn, CreatedOn) >= @ReferralLookbackStart

    UNION ALL SELECT 'B28', 'ctrl.tbl_3PSETUP', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[tbl3PSETUP]
    UNION ALL SELECT 'B28', 'pats.tbl_Codes', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[tblCodes]
    UNION ALL SELECT 'B28', 'pats.tbl_SERVICES', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[tblSERVICES]
    UNION ALL SELECT 'B28', 'ctrl.tbl_DroDownListItems', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[DroDownListItems]
    UNION ALL SELECT 'B28', 'pats.tbl_CustomAnswers', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[tblCUSTOMANSWERS]
    UNION ALL SELECT 'B28', 'pats.tbl_CustomQuestions', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[tblCUSTOMQUESTIONS]
    UNION ALL SELECT 'B28', 'ayx.tbl_PreAdmission_V6', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[SF_PatientPreAdmission] pp WHERE LEN(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE '%test data%'
    UNION ALL SELECT 'B28', 'pats.tbl_PreadmissionReferralSource', COUNT(*) FROM [SAMMS-WestPlainesV5].[dbo].[SF_PatientPreadmissionReferralSource] WHERE ISNULL(LastUpdateOn, CreatedOn) >= @ReferralLookbackStart
)
SELECT 'SAMMS_SOURCE' AS SystemName, @WorkDate AS WorkDateContext, SiteCode, TableName, [RowCount]
FROM Counts
ORDER BY SiteCode, TableName;