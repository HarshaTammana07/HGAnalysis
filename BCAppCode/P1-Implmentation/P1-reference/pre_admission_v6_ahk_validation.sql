/*
P1 Reference validation queries for AHK
Work date context: 2026-06-25

Tables covered:
1. ctrl.tbl_Clinic
2. ctrl.tbl_3PSETUP
3. pats.tbl_Codes
4. pats.tbl_SERVICES
5. ctrl.tbl_DroDownListItems
6. pats.tbl_CustomAnswers
7. pats.tbl_CustomQuestions
8. ayx.tbl_PreAdmission_V6
9. pats.tbl_PreadmissionReferralSource

Important:
These P1 Reference tables are validated as full AHK tables. For SavePreAdmissionV6,
the old C# hardcoded SQL uses only this quality filter:
    LEN(pp.CreatedOn) > 0
    AND pp.ClientAddress NOT LIKE '%test data%'
It does not use a date lookback filter.

Run the BHG_DR section against old BHG_DR.
Run the Fabric Silver section against the Fabric SQL endpoint.
If your Fabric SQL endpoint is already connected directly to bhg_silver, replace
bhg_silver.<schema>.<table> with <schema>.<table>.
*/

/*==============================================================================
1. BHG_DR / SQL Server - row counts and distinct key counts
==============================================================================*/

DECLARE @SiteCode varchar(25) = 'AHK';
DECLARE @WorkDate date = '2026-06-25';

SELECT 'BHG_DR' AS SystemName, @WorkDate AS WorkDateContext, 'ctrl.tbl_Clinic' AS TableName,
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', PKEY)) AS DistinctKeyCount
FROM ctrl.tbl_Clinic
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'ctrl.tbl_3PSETUP',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', pID)) AS DistinctKeyCount
FROM ctrl.tbl_3PSETUP
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'pats.tbl_Codes',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', cdeID)) AS DistinctKeyCount
FROM pats.tbl_Codes
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'pats.tbl_SERVICES',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', sID)) AS DistinctKeyCount
FROM pats.tbl_SERVICES
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'ctrl.tbl_DroDownListItems',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM ctrl.tbl_DroDownListItems
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'pats.tbl_CustomAnswers',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
FROM pats.tbl_CustomAnswers
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'pats.tbl_CustomQuestions',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', cID)) AS DistinctKeyCount
FROM pats.tbl_CustomQuestions
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'ayx.tbl_PreAdmission_V6',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid)) AS DistinctKeyCount
FROM ayx.tbl_PreAdmission_V6
WHERE SiteCode = @SiteCode
UNION ALL
SELECT 'BHG_DR', @WorkDate, 'pats.tbl_PreadmissionReferralSource',
       COUNT_BIG(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM pats.tbl_PreadmissionReferralSource
WHERE SiteCode = @SiteCode
ORDER BY TableName;

/*==============================================================================
2. BHG_DR / SQL Server - duplicate key checks
Expected result: zero rows.
==============================================================================*/

SELECT 'ctrl.tbl_Clinic' AS TableName, CONCAT(SiteCode, '|', PKEY) AS KeyValue, COUNT_BIG(*) AS [RowCount]
FROM ctrl.tbl_Clinic
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, PKEY
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'ctrl.tbl_3PSETUP', CONCAT(SiteCode, '|', pID), COUNT_BIG(*) AS [RowCount]
FROM ctrl.tbl_3PSETUP
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, pID
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'pats.tbl_Codes', CONCAT(SiteCode, '|', cdeID), COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_Codes
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, cdeID
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'pats.tbl_SERVICES', CONCAT(SiteCode, '|', sID), COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_SERVICES
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, sID
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'ctrl.tbl_DroDownListItems', CONCAT(SiteCode, '|', Id), COUNT_BIG(*) AS [RowCount]
FROM ctrl.tbl_DroDownListItems
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, Id
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'pats.tbl_CustomAnswers', CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID), COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_CustomAnswers
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, caID, caQID, caCLTID
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'pats.tbl_CustomQuestions', CONCAT(SiteCode, '|', cID), COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_CustomQuestions
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, cID
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'ayx.tbl_PreAdmission_V6', CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid), COUNT_BIG(*) AS [RowCount]
FROM ayx.tbl_PreAdmission_V6
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, PreAdmissionid, Clientid
HAVING COUNT_BIG(*) > 1
UNION ALL
SELECT 'pats.tbl_PreadmissionReferralSource', CONCAT(SiteCode, '|', Id), COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_PreadmissionReferralSource
WHERE SiteCode = @SiteCode
GROUP BY SiteCode, Id
HAVING COUNT_BIG(*) > 1
ORDER BY TableName, [RowCount] DESC, KeyValue;

/*==============================================================================
3. BHG_DR / SQL Server - status flag distributions
==============================================================================*/

SELECT 'pats.tbl_SERVICES' AS TableName, 'IsActive' AS StatusColumn,
       CAST(IsActive AS varchar(30)) AS StatusValue, COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_SERVICES
WHERE SiteCode = @SiteCode
GROUP BY IsActive
UNION ALL
SELECT 'pats.tbl_CustomAnswers', 'RowSate',
       CAST(RowSate AS varchar(30)) AS StatusValue, COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_CustomAnswers
WHERE SiteCode = @SiteCode
GROUP BY RowSate
UNION ALL
SELECT 'pats.tbl_CustomQuestions', 'RowSate',
       CAST(RowSate AS varchar(30)) AS StatusValue, COUNT_BIG(*) AS [RowCount]
FROM pats.tbl_CustomQuestions
WHERE SiteCode = @SiteCode
GROUP BY RowSate
UNION ALL
SELECT 'ayx.tbl_PreAdmission_V6', 'RowState',
       CAST(RowState AS varchar(30)) AS StatusValue, COUNT_BIG(*) AS [RowCount]
FROM ayx.tbl_PreAdmission_V6
WHERE SiteCode = @SiteCode
GROUP BY RowState
ORDER BY TableName, StatusColumn, StatusValue;

/*==============================================================================
4. BHG_DR / SQL Server - sample rows
==============================================================================*/

SELECT TOP (25) 'ctrl.tbl_Clinic' AS TableName, SiteCode, PKEY AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4
FROM ctrl.tbl_Clinic
WHERE SiteCode = @SiteCode
ORDER BY PKEY DESC;

SELECT TOP (25) 'ctrl.tbl_3PSETUP' AS TableName, SiteCode, pID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       RowChkSum, LastModAt
FROM ctrl.tbl_3PSETUP
WHERE SiteCode = @SiteCode
ORDER BY pID DESC;

SELECT TOP (25) 'pats.tbl_Codes' AS TableName, SiteCode, cdeID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       cdeCode, RowChkSum, LastModAt
FROM pats.tbl_Codes
WHERE SiteCode = @SiteCode
ORDER BY cdeID DESC;

SELECT TOP (25) 'pats.tbl_SERVICES' AS TableName, SiteCode, sID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       sDesc, RowChkSum, IsActive, LastModAt
FROM pats.tbl_SERVICES
WHERE SiteCode = @SiteCode
ORDER BY sID DESC;

SELECT TOP (25) 'ctrl.tbl_DroDownListItems' AS TableName, SiteCode, Id AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       DropDownListItem, DropDownListId, ddapcode
FROM ctrl.tbl_DroDownListItems
WHERE SiteCode = @SiteCode
ORDER BY Id DESC;

SELECT TOP (25) 'pats.tbl_CustomAnswers' AS TableName, SiteCode, caID AS Key1, caQID AS Key2, caCLTID AS Key3, NULL AS Key4,
       RowSate, RowCheckSum, LastModAt
FROM pats.tbl_CustomAnswers
WHERE SiteCode = @SiteCode
ORDER BY caID DESC, caQID DESC, caCLTID DESC;

SELECT TOP (25) 'pats.tbl_CustomQuestions' AS TableName, SiteCode, cID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       RowSate, RowCheckSum, LastModAt
FROM pats.tbl_CustomQuestions
WHERE SiteCode = @SiteCode
ORDER BY cID DESC;

SELECT TOP (25) 'ayx.tbl_PreAdmission_V6' AS TableName, SiteCode, PreAdmissionid AS Key1, Clientid AS Key2, NULL AS Key3, NULL AS Key4,
       cltM4ID, CreatedON, PreAdmissionDate, LastUpdateOn, RowChkSum, RowState, ETLLastModAt
FROM ayx.tbl_PreAdmission_V6
WHERE SiteCode = @SiteCode
ORDER BY COALESCE(LastUpdateOn, CreatedON) DESC, PreAdmissionid DESC, Clientid DESC;

SELECT TOP (25) 'pats.tbl_PreadmissionReferralSource' AS TableName, SiteCode, Id AS Key1, PreAdmissionId AS Key2, ClientId AS Key3, NULL AS Key4,
       IsDeleted, CreatedDate, ModifiedDate
FROM pats.tbl_PreadmissionReferralSource
WHERE SiteCode = @SiteCode
ORDER BY COALESCE(ModifiedDate, CreatedDate) DESC, Id DESC;

/*==============================================================================
5. Fabric Silver - row counts and distinct key counts
==============================================================================*/

SELECT 'bhg_silver' AS SystemName, CAST('2026-06-25' AS date) AS WorkDateContext, 'ctrl.tbl_Clinic' AS TableName,
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', PKEY)) AS DistinctKeyCount
FROM bhg_silver.ctrl.tbl_Clinic
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'ctrl.tbl_3PSETUP',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', pID)) AS DistinctKeyCount
FROM bhg_silver.ctrl.tbl_3PSETUP
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'pats.tbl_Codes',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', cdeID)) AS DistinctKeyCount
FROM bhg_silver.pats.tbl_Codes
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'pats.tbl_SERVICES',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', sID)) AS DistinctKeyCount
FROM bhg_silver.pats.tbl_SERVICES
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'ctrl.tbl_DroDownListItems',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM bhg_silver.ctrl.tbl_DroDownListItems
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'pats.tbl_CustomAnswers',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID)) AS DistinctKeyCount
FROM bhg_silver.pats.tbl_CustomAnswers
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'pats.tbl_CustomQuestions',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', cID)) AS DistinctKeyCount
FROM bhg_silver.pats.tbl_CustomQuestions
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'ayx.tbl_PreAdmission_V6',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid)) AS DistinctKeyCount
FROM bhg_silver.ayx.tbl_PreAdmission_V6
WHERE SiteCode = 'AHK'
UNION ALL
SELECT 'bhg_silver', CAST('2026-06-25' AS date), 'pats.tbl_PreadmissionReferralSource',
       COUNT(*) AS [RowCount],
       COUNT(DISTINCT CONCAT(SiteCode, '|', Id)) AS DistinctKeyCount
FROM bhg_silver.pats.tbl_PreadmissionReferralSource
WHERE SiteCode = 'AHK'
ORDER BY TableName;

/*==============================================================================
6. Fabric Silver - duplicate key checks
Expected result: zero rows.
==============================================================================*/

SELECT 'ctrl.tbl_Clinic' AS TableName, CONCAT(SiteCode, '|', PKEY) AS KeyValue, COUNT(*) AS [RowCount]
FROM bhg_silver.ctrl.tbl_Clinic
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, PKEY
HAVING COUNT(*) > 1
UNION ALL
SELECT 'ctrl.tbl_3PSETUP', CONCAT(SiteCode, '|', pID), COUNT(*) AS [RowCount]
FROM bhg_silver.ctrl.tbl_3PSETUP
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, pID
HAVING COUNT(*) > 1
UNION ALL
SELECT 'pats.tbl_Codes', CONCAT(SiteCode, '|', cdeID), COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_Codes
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, cdeID
HAVING COUNT(*) > 1
UNION ALL
SELECT 'pats.tbl_SERVICES', CONCAT(SiteCode, '|', sID), COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_SERVICES
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, sID
HAVING COUNT(*) > 1
UNION ALL
SELECT 'ctrl.tbl_DroDownListItems', CONCAT(SiteCode, '|', Id), COUNT(*) AS [RowCount]
FROM bhg_silver.ctrl.tbl_DroDownListItems
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, Id
HAVING COUNT(*) > 1
UNION ALL
SELECT 'pats.tbl_CustomAnswers', CONCAT(SiteCode, '|', caID, '|', caQID, '|', caCLTID), COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_CustomAnswers
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, caID, caQID, caCLTID
HAVING COUNT(*) > 1
UNION ALL
SELECT 'pats.tbl_CustomQuestions', CONCAT(SiteCode, '|', cID), COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_CustomQuestions
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, cID
HAVING COUNT(*) > 1
UNION ALL
SELECT 'ayx.tbl_PreAdmission_V6', CONCAT(SiteCode, '|', PreAdmissionid, '|', Clientid), COUNT(*) AS [RowCount]
FROM bhg_silver.ayx.tbl_PreAdmission_V6
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, PreAdmissionid, Clientid
HAVING COUNT(*) > 1
UNION ALL
SELECT 'pats.tbl_PreadmissionReferralSource', CONCAT(SiteCode, '|', Id), COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_PreadmissionReferralSource
WHERE SiteCode = 'AHK'
GROUP BY SiteCode, Id
HAVING COUNT(*) > 1
ORDER BY TableName, [RowCount] DESC, KeyValue;

/*==============================================================================
7. Fabric Silver - status flag distributions
==============================================================================*/

SELECT 'pats.tbl_SERVICES' AS TableName, 'IsActive' AS StatusColumn,
       CAST(IsActive AS varchar(30)) AS StatusValue, COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_SERVICES
WHERE SiteCode = 'AHK'
GROUP BY IsActive
UNION ALL
SELECT 'pats.tbl_CustomAnswers', 'RowSate',
       CAST(RowSate AS varchar(30)) AS StatusValue, COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_CustomAnswers
WHERE SiteCode = 'AHK'
GROUP BY RowSate
UNION ALL
SELECT 'pats.tbl_CustomQuestions', 'RowSate',
       CAST(RowSate AS varchar(30)) AS StatusValue, COUNT(*) AS [RowCount]
FROM bhg_silver.pats.tbl_CustomQuestions
WHERE SiteCode = 'AHK'
GROUP BY RowSate
UNION ALL
SELECT 'ayx.tbl_PreAdmission_V6', 'RowState',
       CAST(RowState AS varchar(30)) AS StatusValue, COUNT(*) AS [RowCount]
FROM bhg_silver.ayx.tbl_PreAdmission_V6
WHERE SiteCode = 'AHK'
GROUP BY RowState
ORDER BY TableName, StatusColumn, StatusValue;

/*==============================================================================
8. Fabric Silver - sample rows
==============================================================================*/

SELECT TOP (25) 'ctrl.tbl_Clinic' AS TableName, SiteCode, PKEY AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4
FROM bhg_silver.ctrl.tbl_Clinic
WHERE SiteCode = 'AHK'
ORDER BY PKEY DESC;

SELECT TOP (25) 'ctrl.tbl_3PSETUP' AS TableName, SiteCode, pID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       RowChkSum, LastModAt
FROM bhg_silver.ctrl.tbl_3PSETUP
WHERE SiteCode = 'AHK'
ORDER BY pID DESC;

SELECT TOP (25) 'pats.tbl_Codes' AS TableName, SiteCode, cdeID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       cdeCode, RowChkSum, LastModAt
FROM bhg_silver.pats.tbl_Codes
WHERE SiteCode = 'AHK'
ORDER BY cdeID DESC;

SELECT TOP (25) 'pats.tbl_SERVICES' AS TableName, SiteCode, sID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       sDesc, RowChkSum, IsActive, LastModAt
FROM bhg_silver.pats.tbl_SERVICES
WHERE SiteCode = 'AHK'
ORDER BY sID DESC;

SELECT TOP (25) 'ctrl.tbl_DroDownListItems' AS TableName, SiteCode, Id AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       DropDownListItem, DropDownListId, ddapcode
FROM bhg_silver.ctrl.tbl_DroDownListItems
WHERE SiteCode = 'AHK'
ORDER BY Id DESC;

SELECT TOP (25) 'pats.tbl_CustomAnswers' AS TableName, SiteCode, caID AS Key1, caQID AS Key2, caCLTID AS Key3, NULL AS Key4,
       RowSate, RowCheckSum, LastModAt
FROM bhg_silver.pats.tbl_CustomAnswers
WHERE SiteCode = 'AHK'
ORDER BY caID DESC, caQID DESC, caCLTID DESC;

SELECT TOP (25) 'pats.tbl_CustomQuestions' AS TableName, SiteCode, cID AS Key1, NULL AS Key2, NULL AS Key3, NULL AS Key4,
       RowSate, RowCheckSum, LastModAt
FROM bhg_silver.pats.tbl_CustomQuestions
WHERE SiteCode = 'AHK'
ORDER BY cID DESC;

SELECT TOP (25) 'ayx.tbl_PreAdmission_V6' AS TableName, SiteCode, PreAdmissionid AS Key1, Clientid AS Key2, NULL AS Key3, NULL AS Key4,
       cltM4ID, CreatedON, PreAdmissionDate, LastUpdateOn, RowChkSum, RowState, ETLLastModAt
FROM bhg_silver.ayx.tbl_PreAdmission_V6
WHERE SiteCode = 'AHK'
ORDER BY COALESCE(LastUpdateOn, CreatedON) DESC, PreAdmissionid DESC, Clientid DESC;

SELECT TOP (25) 'pats.tbl_PreadmissionReferralSource' AS TableName, SiteCode, Id AS Key1, PreAdmissionId AS Key2, ClientId AS Key3, NULL AS Key4,
       IsDeleted, CreatedDate, ModifiedDate
FROM bhg_silver.pats.tbl_PreadmissionReferralSource
WHERE SiteCode = 'AHK'
ORDER BY COALESCE(ModifiedDate, CreatedDate) DESC, Id DESC;

/*==============================================================================
9. Expected checks
==============================================================================*/

/*
Expected:
1. For each table, BHG_DR [RowCount] should match bhg_silver [RowCount] for SiteCode = AHK.
2. For each table, DistinctKeyCount should equal [RowCount].
3. Duplicate key checks should return zero rows.
4. Status flag distributions should match for:
   - pats.tbl_SERVICES.IsActive
   - pats.tbl_CustomAnswers.RowSate
   - pats.tbl_CustomQuestions.RowSate
   - ayx.tbl_PreAdmission_V6.RowState
5. LastModAt and ETLLastModAt timestamps may differ because Fabric writes the current pipeline run timestamp.
*/
