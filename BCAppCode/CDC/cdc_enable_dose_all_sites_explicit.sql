/*
================================================================================
  BHG — Enable CDC on Dose + DoseExcuse (explicit site list — all clinics)
================================================================================

  Purpose   : Enable SQL Server CDC on dbo.tblDOSE and dbo.tblDOSE_Excuse
              for every clinic in the BHG site registry.

  Source    : BCAppCode/Framework/AllsiteCodesAndDatabses.txt
              (115 sites as of file export)

  Tables    : dbo.tblDOSE          -> cdc.dbo_tblDOSE_CT
              dbo.tblDOSE_Excuse   -> cdc.dbo_tblDOSE_Excuse_CT

  Pilot only? Use instead: cdc_enable_dose_pilot_3sites.sql (AHK, B12B, CBCO)

  Prerequisites:
    - SQL Server Agent running
    - sysadmin or db_owner on each target database
    - Source tables must have a primary key

  Safe to re-run: skips DBs/tables already tracked by CDC.

  Steps:
    1. Run STEP 1 preview
    2. Run STEP 2 enable
    3. Run STEP 3 verification summary
    4. Run STEP 4 verification — failures only (empty = success)
================================================================================
*/

SET NOCOUNT ON;

/*
================================================================================
  Site registry — explicit database names (from Framework folder)
================================================================================
*/

DECLARE @Databases TABLE (
    SiteCode     NVARCHAR(16)  NOT NULL,
    DatabaseName NVARCHAR(128) NOT NULL PRIMARY KEY
);

INSERT INTO @Databases (SiteCode, DatabaseName) VALUES
    (N'AHK',    N'SAMMS-Ahoskie'),
    (N'B12B',   N'SAMMS-ColoradoSpringsV5'),
    (N'B24',    N'SAMMS-PaintsvilleV5'),
    (N'B25',    N'SAMMS-PikevilleV5'),
    (N'B26',    N'SAMMS-HazardV5'),
    (N'B27',    N'SAMMS-SavannahV6'),
    (N'B28',    N'SAMMS-WestPlainesV5'),
    (N'B29',    N'SAMMS-PoplarBluffV5'),
    (N'B30',    N'SAMMS-KCNv5'),
    (N'B31',    N'SAMMS-DyersburgV5'),
    (N'B33',    N'SAMMS-Paducah'),
    (N'B34',    N'SAMMS-CorbinV5'),
    (N'B35',    N'SAMMS-LexingtonV5'),
    (N'B35A',   N'SAMMS-BereaV5'),
    (N'B36',    N'SAMMS-AshevilleV5'),
    (N'B37',    N'SAMMS-ClydeV5'),
    (N'B38',    N'SAMMS-SpartanburgV5'),
    (N'B39',    N'SAMMS-AikenV5'),
    (N'B41',    N'SAMMS-ChesapeakeV5'),
    (N'B42',    N'SAMMS-VirginiaBeachV5'),
    (N'B42A',   N'SAMMS-NewportNewsV5'),
    (N'B42B',   N'SAMMS-FranklinV5'),
    (N'B42C',   N'SAMMS-GlenAllenV5'),
    (N'B42D',   N'SAMMS-ChesapeakeSouthV5'),
    (N'B44',    N'SAMMS-AlbanyV5'),
    (N'B45',    N'SAMMS-TiftonV5'),
    (N'B46',    N'SAMMS-WashingtonDCv5'),
    (N'B47',    N'SAMMS-MobileV5'),
    (N'B48',    N'SAMMS-TuscaloosaV5'),
    (N'B51',    N'SAMMS-NorthLittleRockV6'),
    (N'B52',    N'SAMMS-JacksonGAV5'),
    (N'B54',    N'SAMMS-GadsdenV5'),
    (N'B55',    N'SAMMS-ShoalsV5'),
    (N'B57',    N'SAMMS-Pawtucket'),
    (N'B57A',   N'SAMMS-Johnston'),
    (N'B57B',   N'SAMMS-Middletown'),
    (N'B57C',   N'SAMMS-Providence'),
    (N'B57D',   N'SAMMS-Westerly'),
    (N'B66A',   N'SAMMS-Bremen'),
    (N'B72',    N'SAMMS-Mobile-OBOT'),
    (N'B73',    N'SAMMS-Montgomery'),
    (N'B75',    N'SAMMS-LawrenceV6'),
    (N'B76',    N'SAMMS-Huntsville-OBOT'),
    (N'BAT',    N'SAMMS-Batesville'),
    (N'BG',     N'SAMMS-BowlingGreen'),
    (N'BOI',    N'SAMMS-Boise'),
    (N'CBCO',   N'SAMMS-CoeurdAleneV6'),
    (N'CON',    N'SAMMS-Conway'),
    (N'D07',    N'SAMMS-KnoxvilleV6'),
    (N'D08',    N'SAMMS-MadisonV6'),
    (N'D09',    N'SAMMS-MurfreesboroV6'),
    (N'DA',     N'SAMMS-Davenport'),
    (N'DM',     N'SAMMS-DesMoines'),
    (N'DRD-CO', N'SAMMS-ColumbiaV5'),
    (N'DRD-KC', N'SAMMS-KCv5'),
    (N'DRD-KVB',N'SAMMS-KVBv5'),
    (N'DRD-KVC',N'SAMMS-KVCv5'),
    (N'DRD-NOLA',N'SAMMS-NOLAv5'),
    (N'DRD-SF', N'SAMMS-SFv5'),
    (N'ELC',    N'SAMMS-ElizabethCity'),
    (N'ET',     N'SAMMS-Elizabethtown'),
    (N'FAY',    N'SAMMS-Fayetteville'),
    (N'FR',     N'SAMMS-Frankfort'),
    (N'FS',     N'SAMMS-FortSmith'),
    (N'FW',     N'SAMMS-FortWayne'),
    (N'GAL',    N'SAMMS-Gaylord'),
    (N'HGT',    N'SAMMS-Hagerstown'),
    (N'HNT',    N'SAMMS-Huntsville'),
    (N'HS',     N'SAMMS-HotSprings'),
    (N'JON',    N'SAMMS-Jonesboro'),
    (N'LAN',    N'SAMMS-Lansing'),
    (N'LO',     N'SAMMS-Louisville'),
    (N'LV1',    N'SAMMS-Cheyenne'),
    (N'LV2',    N'SAMMS-DesertInn'),
    (N'LV3',    N'SAMMS-McDaniel'),
    (N'MNRE',   N'SAMMS-Monroe'),
    (N'MP',     N'SAMMS-MtPleasant'),
    (N'MRD',    N'SAMMS-Meridian'),
    (N'NC',     N'SAMMS-NorthCharleston'),
    (N'NLR',    N'SAMMS-NLROBOT'),
    (N'PH',     N'SAMMS-Phoenix'),
    (N'RE',     N'SAMMS-Reno'),
    (N'RMD',    N'SAMMS-Richmond'),
    (N'SFN',    N'SAMMS-SFNv5'),
    (N'SHP',    N'SAMMS-Shreveport'),
    (N'STN',    N'SAMMS-Staunton'),
    (N'STVN',   N'SAMMS-Stevenson'),
    (N'TE',     N'SAMMS-Tempe'),
    (N'TEX',    N'SAMMS-Texarkana'),
    (N'TTCA',   N'SAMMS-BessemerV5'),
    (N'TTCB',   N'SAMMS-CullmanV5'),
    (N'TTCC',   N'SAMMS-GrandBay'),
    (N'TU',     N'SAMMS-Tucson'),
    (N'V1',     N'SAMMS-VCPHCS-I-MemphisV5'),
    (N'V10',    N'SAMMS-BoulderV5'),
    (N'V10A',   N'SAMMS-FortCollinsV5'),
    (N'V11',    N'SAMMS-VCPHCS-XI-NorthDenverV5'),
    (N'V12',    N'SAMMS-VCPHCS-XII-DowntownDenverV5'),
    (N'V12A',   N'SAMMS-CentennialV5'),
    (N'V14',    N'SAMMS-VCPHCS-XIV-BridgewayV5'),
    (N'V15',    N'SAMMS-JoplinV5'),
    (N'V17',    N'SAMMS-ColumbiaTNv5'),
    (N'V19',    N'SAMMS-JacksonV5'),
    (N'V20',    N'SAMMS-ParisV5'),
    (N'V21',    N'SAMMS-RaleighV5'),
    (N'V5',     N'SAMMS-NONTCv5'),
    (N'V5B',    N'SAMMS-HoumaV6'),
    (N'V6',     N'SAMMS-LCv5'),
    (N'V8',     N'SAMMS-VCPHCS-VIII-MemphisV5'),
    (N'V9',     N'SAMMS-NashvilleV5'),
    (N'VBRA',   N'SAMMS-BrainerdV6'),
    (N'VBRP',   N'SAMMS-BrooklynParkV6'),
    (N'VMIN',   N'SAMMS-MinneapolisV6'),
    (N'VWBY',   N'SAMMS-WoodburyV6'),
    (N'WIL',    N'SAMMS-Wilson');

/*
================================================================================
  STEP 1 — PREVIEW (no changes)
================================================================================
*/

SELECT
    d.SiteCode,
    d.DatabaseName,
    CASE WHEN sd.name IS NULL THEN N'MISSING ON INSTANCE' ELSE sd.state_desc END AS SqlServerState,
    ISNULL(sd.is_cdc_enabled, 0) AS CdcEnabledOnDatabase
FROM @Databases d
LEFT JOIN sys.databases sd ON sd.name = d.DatabaseName
ORDER BY d.SiteCode;

/*
================================================================================
  STEP 2 — ENABLE CDC
================================================================================
*/

DECLARE @SiteCode NVARCHAR(16);
DECLARE @DatabaseName NVARCHAR(128);
DECLARE @FullSQL NVARCHAR(MAX);
DECLARE @DbCount INT = 0;

DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT SiteCode, DatabaseName
FROM @Databases
ORDER BY SiteCode;

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @SiteCode, @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = @DatabaseName)
    BEGIN
        PRINT N'SKIPPED (not on instance): ' + @SiteCode + N' -> ' + @DatabaseName;
        FETCH NEXT FROM db_cursor INTO @SiteCode, @DatabaseName;
        CONTINUE;
    END

    SET @DbCount += 1;

    SET @FullSQL = N'
    USE [' + @DatabaseName + N'];

    IF NOT EXISTS (
        SELECT 1
        FROM sys.databases
        WHERE name = N''' + @DatabaseName + N'''
          AND is_cdc_enabled = 1
    )
    BEGIN
        EXEC sys.sp_cdc_enable_db;
        PRINT ''CDC enabled at database level: ' + @SiteCode + N' / ' + @DatabaseName + N''';
    END

    DECLARE @schema NVARCHAR(128);
    DECLARE @table NVARCHAR(128);

    DECLARE tbl_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT s.name, t.name
        FROM sys.tables t
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE t.is_ms_shipped = 0
          AND t.is_tracked_by_cdc = 0
          AND s.name = N''dbo''
          AND t.name IN (N''tblDOSE'', N''tblDOSE_Excuse'');

    OPEN tbl_cur;
    FETCH NEXT FROM tbl_cur INTO @schema, @table;

    WHILE @@FETCH_STATUS = 0
    BEGIN
        BEGIN TRY
            EXEC sys.sp_cdc_enable_table
                @source_schema = @schema,
                @source_name = @table,
                @role_name = NULL,
                @supports_net_changes = 0;

            PRINT ''CDC enabled: ' + @SiteCode + N' / ' + @DatabaseName + N'.'' + @schema + N''.'' + @table;
        END TRY
        BEGIN CATCH
            PRINT ''FAILED: ' + @SiteCode + N' / ' + @DatabaseName + N'.'' + @schema + N''.'' + @table + N'' - '' + ERROR_MESSAGE();
        END CATCH;

        FETCH NEXT FROM tbl_cur INTO @schema, @table;
    END

    CLOSE tbl_cur;
    DEALLOCATE tbl_cur;
    ';

    EXEC sys.sp_executesql @FullSQL;

    FETCH NEXT FROM db_cursor INTO @SiteCode, @DatabaseName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;

PRINT '';
PRINT N'=== ENABLE SCRIPT COMPLETE ===';
PRINT N'Databases found and processed on this instance: ' + CAST(@DbCount AS NVARCHAR(20));
PRINT N'Expected site count from Framework list: 115';
PRINT N'Run STEP 3 and STEP 4 verification next.';
PRINT '';

/*
================================================================================
  STEP 3 — VERIFICATION (summary by site)
  Re-declare @Databases if running this section alone in a new session.
================================================================================
*/

DECLARE @DatabasesVerify TABLE (
    SiteCode     NVARCHAR(16)  NOT NULL,
    DatabaseName NVARCHAR(128) NOT NULL PRIMARY KEY
);

INSERT INTO @DatabasesVerify (SiteCode, DatabaseName)
SELECT SiteCode, DatabaseName FROM @Databases;

DECLARE @SummarySQL NVARCHAR(MAX) = N'';

SELECT @SummarySQL = @SummarySQL + N'
SELECT
    ''' + v.SiteCode + N''' AS SiteCode,
    ''' + v.DatabaseName + N''' AS DatabaseName,
    MAX(CASE WHEN t.name = N''tblDOSE'' THEN t.is_tracked_by_cdc END) AS tblDOSE_Enabled,
    MAX(CASE WHEN t.name = N''tblDOSE_Excuse'' THEN t.is_tracked_by_cdc END) AS tblDOSE_Excuse_Enabled
FROM [' + v.DatabaseName + N'].sys.tables t
INNER JOIN [' + v.DatabaseName + N'].sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = N''dbo''
  AND t.name IN (N''tblDOSE'', N''tblDOSE_Excuse'')
UNION ALL'
FROM @DatabasesVerify v
INNER JOIN sys.databases sd ON sd.name = v.DatabaseName;

IF LEN(@SummarySQL) > 0
BEGIN
    SET @SummarySQL = LEFT(@SummarySQL, LEN(@SummarySQL) - LEN(N'UNION ALL'));
    SET @SummarySQL = N'
SELECT
    SiteCode,
    DatabaseName,
    tblDOSE_Enabled,
    tblDOSE_Excuse_Enabled,
    CASE
        WHEN tblDOSE_Enabled = 1 AND tblDOSE_Excuse_Enabled = 1 THEN N''OK''
        ELSE N''CHECK''
    END AS Status
FROM (
' + @SummarySQL + N'
) x
ORDER BY SiteCode;';
    EXEC sys.sp_executesql @SummarySQL;
END

/*
================================================================================
  STEP 4 — VERIFICATION (failures only)
================================================================================
*/

DECLARE @DetailSQL NVARCHAR(MAX) = N'';

SELECT @DetailSQL = @DetailSQL + N'
SELECT
    ''' + v.SiteCode + N''' AS SiteCode,
    ''' + v.DatabaseName + N''' AS DatabaseName,
    ct.capture_instance,
    t.name AS TableName,
    t.is_tracked_by_cdc
FROM [' + v.DatabaseName + N'].sys.tables t
INNER JOIN [' + v.DatabaseName + N'].sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN [' + v.DatabaseName + N'].cdc.change_tables ct ON ct.source_object_id = t.object_id
WHERE s.name = N''dbo''
  AND t.name IN (N''tblDOSE'', N''tblDOSE_Excuse'')
UNION ALL'
FROM @DatabasesVerify v
INNER JOIN sys.databases sd ON sd.name = v.DatabaseName;

IF LEN(@DetailSQL) > 0
BEGIN
    SET @DetailSQL = LEFT(@DetailSQL, LEN(@DetailSQL) - LEN(N'UNION ALL'));
    SET @DetailSQL = N'
SELECT *
FROM (
' + @DetailSQL + N'
) y
WHERE ISNULL(y.is_tracked_by_cdc, 0) = 0
ORDER BY y.SiteCode, y.TableName;';
    EXEC sys.sp_executesql @DetailSQL;
END

/*
  Expected capture instances (per database):
    dbo_tblDOSE         -> cdc.dbo_tblDOSE_CT
    dbo_tblDOSE_Excuse  -> cdc.dbo_tblDOSE_Excuse_CT

  STEP 4 empty result = all listed sites have both tables CDC-enabled.

  Rollback (per table):
    EXEC sys.sp_cdc_disable_table @source_schema=N'dbo', @source_name=N'tblDOSE', @capture_instance=N'all';
    EXEC sys.sp_cdc_disable_table @source_schema=N'dbo', @source_name=N'tblDOSE_Excuse', @capture_instance=N'all';
*/
