/*
================================================================================
  BHG — Enable CDC on Dose + DoseExcuse (ALL SAMMS clinic databases)
================================================================================

  Purpose   : Enable SQL Server CDC on dbo.tblDOSE and dbo.tblDOSE_Excuse
              across every online SAMMS clinic database on this instance.

  Tables    : dbo.tblDOSE          -> cdc.dbo_tblDOSE_CT
              dbo.tblDOSE_Excuse   -> cdc.dbo_tblDOSE_Excuse_CT

  Database scope:
              All databases where name LIKE 'SAMMS-%' and state = ONLINE.
              Review STEP 1 preview output before running STEP 2.

  WARNING   : Full rollout (~115+ databases × 2 tables). Prefer pilot first:
              see cdc_enable_dose_pilot_3sites.sql (AHK, B12B, CBCO).

  Prerequisites:
    - SQL Server Agent running
    - sysadmin or db_owner on each target database
    - Source tables must have a primary key

  Safe to re-run: skips DBs/tables already tracked by CDC.

  Rollback (per table):
    EXEC sys.sp_cdc_disable_table @source_schema=N'dbo', @source_name=N'tblDOSE', @capture_instance=N'all';
    EXEC sys.sp_cdc_disable_table @source_schema=N'dbo', @source_name=N'tblDOSE_Excuse', @capture_instance=N'all';
================================================================================
*/

SET NOCOUNT ON;

/*
================================================================================
  STEP 1 — PREVIEW (run first; no changes)
  Confirm the database list before enabling CDC.
================================================================================
*/

SELECT
    d.name AS DatabaseName,
    d.state_desc,
    d.is_cdc_enabled AS CdcEnabledOnDatabase
FROM sys.databases d
WHERE d.name LIKE N'SAMMS-%'
  AND d.state_desc = N'ONLINE'
  AND d.database_id > 4
ORDER BY d.name;

/*
  Optional exclusions — add to STEP 2 WHERE clause if any DB should be skipped, e.g.:
    AND d.name NOT IN (N'SAMMS-SomeTemplate', N'SAMMS-Lab')
*/

/*
================================================================================
  STEP 2 — ENABLE CDC (run after preview is approved)
================================================================================
*/

DECLARE @DatabaseName NVARCHAR(128);
DECLARE @FullSQL NVARCHAR(MAX);
DECLARE @DbCount INT = 0;

DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT d.name
FROM sys.databases d
WHERE d.name LIKE N'SAMMS-%'
  AND d.state_desc = N'ONLINE'
  AND d.database_id > 4
  /* AND d.name NOT IN (N'SAMMS-ExampleExclude') */
ORDER BY d.name;

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
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
        PRINT ''CDC enabled at database level: ' + @DatabaseName + N''';
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

            PRINT ''CDC enabled: ' + @DatabaseName + N'.'' + @schema + N''.'' + @table;
        END TRY
        BEGIN CATCH
            PRINT ''FAILED: ' + @DatabaseName + N'.'' + @schema + N''.'' + @table + N'' - '' + ERROR_MESSAGE();
        END CATCH;

        FETCH NEXT FROM tbl_cur INTO @schema, @table;
    END

    CLOSE tbl_cur;
    DEALLOCATE tbl_cur;
    ';

    EXEC sys.sp_executesql @FullSQL;

    FETCH NEXT FROM db_cursor INTO @DatabaseName;
END

CLOSE db_cursor;
DEALLOCATE db_cursor;

PRINT '';
PRINT '=== ENABLE SCRIPT COMPLETE ===';
PRINT 'Databases processed: ' + CAST(@DbCount AS NVARCHAR(20));
PRINT 'Review FAILED lines above. Run STEP 3 verification next.';
PRINT '';

/*
================================================================================
  STEP 3 — VERIFICATION (summary)
================================================================================
*/

DECLARE @SummarySQL NVARCHAR(MAX) = N'';

SELECT @SummarySQL = @SummarySQL + N'
SELECT
    ''' + name + N''' AS DatabaseName,
    MAX(CASE WHEN t.name = N''tblDOSE'' THEN t.is_tracked_by_cdc END) AS tblDOSE_Enabled,
    MAX(CASE WHEN t.name = N''tblDOSE_Excuse'' THEN t.is_tracked_by_cdc END) AS tblDOSE_Excuse_Enabled
FROM [' + name + N'].sys.tables t
INNER JOIN [' + name + N'].sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = N''dbo''
  AND t.name IN (N''tblDOSE'', N''tblDOSE_Excuse'')
UNION ALL'
FROM sys.databases
WHERE name LIKE N'SAMMS-%'
  AND state_desc = N'ONLINE'
  AND database_id > 4;

IF LEN(@SummarySQL) > 0
BEGIN
    SET @SummarySQL = LEFT(@SummarySQL, LEN(@SummarySQL) - LEN(N'UNION ALL'));
    SET @SummarySQL = N'
SELECT
    DatabaseName,
    tblDOSE_Enabled,
    tblDOSE_Excuse_Enabled,
    CASE
        WHEN tblDOSE_Enabled = 1 AND tblDOSE_Excuse_Enabled = 1 THEN N''OK''
        ELSE N''CHECK''
    END AS Status
FROM (
' + @SummarySQL + N'
) v
ORDER BY DatabaseName;';
    EXEC sys.sp_executesql @SummarySQL;
END

/*
================================================================================
  STEP 4 — VERIFICATION (detail — databases missing either table)
================================================================================
*/

DECLARE @DetailSQL NVARCHAR(MAX) = N'';

SELECT @DetailSQL = @DetailSQL + N'
SELECT
    ''' + name + N''' AS DatabaseName,
    ct.capture_instance,
    t.name AS TableName,
    t.is_tracked_by_cdc
FROM [' + name + N'].sys.tables t
INNER JOIN [' + name + N'].sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN [' + name + N'].cdc.change_tables ct ON ct.source_object_id = t.object_id
WHERE s.name = N''dbo''
  AND t.name IN (N''tblDOSE'', N''tblDOSE_Excuse'')
UNION ALL'
FROM sys.databases
WHERE name LIKE N'SAMMS-%'
  AND state_desc = N'ONLINE'
  AND database_id > 4;

IF LEN(@DetailSQL) > 0
BEGIN
    SET @DetailSQL = LEFT(@DetailSQL, LEN(@DetailSQL) - LEN(N'UNION ALL'));
    SET @DetailSQL = N'
SELECT *
FROM (
' + @DetailSQL + N'
) v
WHERE ISNULL(v.is_tracked_by_cdc, 0) = 0
ORDER BY v.DatabaseName, v.TableName;';
    EXEC sys.sp_executesql @DetailSQL;
END

/*
  Expected when fully enabled (per database):
    dbo_tblDOSE         -> cdc.dbo_tblDOSE_CT
    dbo_tblDOSE_Excuse  -> cdc.dbo_tblDOSE_Excuse_CT

  STEP 4 returns only rows still NOT enabled (empty = success).
*/
