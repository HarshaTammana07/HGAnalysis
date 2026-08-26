/*
================================================================================
  BHG — Enable CDC on Dose + DoseExcuse (3-site pilot)
================================================================================

  Purpose   : Enable SQL Server CDC for Fabric Dose ETL pilot (same sites as
              LiquidLog CDC: AHK, B12B, CBCO).

  Tables    : dbo.tblDOSE          -> cdc.dbo_tblDOSE_CT
              dbo.tblDOSE_Excuse   -> cdc.dbo_tblDOSE_Excuse_CT

  Databases : SAMMS-Ahoskie
              SAMMS-ColoradoSpringsV5
              SAMMS-CoeurdAleneV6

  Prerequisites:
    - SQL Server Agent running (CDC capture + cleanup jobs)
    - Executing login: sysadmin or db_owner on each database
    - Source tables must have a primary key

  Safe to re-run: skips databases/tables already tracked by CDC.

  After execution: run the VERIFICATION section at the bottom and send output
  to the Fabric team.

  Rollback (if needed):
    EXEC sys.sp_cdc_disable_table @source_schema=N'dbo', @source_name=N'tblDOSE', @capture_instance=N'all';
    EXEC sys.sp_cdc_disable_table @source_schema=N'dbo', @source_name=N'tblDOSE_Excuse', @capture_instance=N'all';
    -- Only after ALL CDC tables disabled on that DB:
    -- EXEC sys.sp_cdc_disable_db;
================================================================================
*/

SET NOCOUNT ON;

DECLARE @DatabaseName NVARCHAR(128);
DECLARE @FullSQL NVARCHAR(MAX);

DECLARE db_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT name
FROM sys.databases
WHERE name IN (
    N'SAMMS-Ahoskie',
    N'SAMMS-ColoradoSpringsV5',
    N'SAMMS-CoeurdAleneV6'
);

OPEN db_cursor;
FETCH NEXT FROM db_cursor INTO @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
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
    ELSE
    BEGIN
        PRINT ''CDC already enabled at database level: ' + @DatabaseName + N''';
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

            PRINT ''CDC enabled on table: ' + @DatabaseName + N'.'' + @schema + N''.'' + @table;
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
PRINT '=== ENABLE SCRIPT COMPLETE — run verification below ===';
PRINT '';

/*
================================================================================
  VERIFICATION — run after enable script
================================================================================
*/

DECLARE @SQL NVARCHAR(MAX) = N'';

SELECT @SQL = @SQL + N'
SELECT
    ''' + name + N''' AS DatabaseName,
    ct.capture_instance,
    s.name AS SchemaName,
    t.name AS TableName,
    t.is_tracked_by_cdc
FROM [' + name + N'].sys.tables t
INNER JOIN [' + name + N'].sys.schemas s ON t.schema_id = s.schema_id
LEFT JOIN [' + name + N'].cdc.change_tables ct ON ct.source_object_id = t.object_id
WHERE t.name IN (N''tblDOSE'', N''tblDOSE_Excuse'')
UNION ALL'
FROM sys.databases
WHERE name IN (
    N'SAMMS-Ahoskie',
    N'SAMMS-ColoradoSpringsV5',
    N'SAMMS-CoeurdAleneV6'
);

IF LEN(@SQL) > 0
BEGIN
    SET @SQL = LEFT(@SQL, LEN(@SQL) - LEN(N'UNION ALL'));
    SET @SQL = @SQL + N' ORDER BY DatabaseName, TableName;';
    EXEC sys.sp_executesql @SQL;
END

/*
  Expected capture instances:
    dbo_tblDOSE         -> cdc.dbo_tblDOSE_CT
    dbo_tblDOSE_Excuse  -> cdc.dbo_tblDOSE_Excuse_CT

  Optional — confirm CDC jobs exist (run per database):
    USE [SAMMS-Ahoskie];
    SELECT name, enabled
    FROM msdb.dbo.sysjobs
    WHERE name LIKE N'cdc.%';
*/
