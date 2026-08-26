Enabling Change Data Capture (CDC) 

Across Multiple Databases and Specific Tables 

1. Overview 

This document describes the SQL script used to enable Change Data Capture (CDC) on a specific, common set of tables across multiple SQL Server databases. Instead of enabling CDC on all tables in a database, this script targets only the tables named in a defined list, and applies that same list consistently to every database in scope. 

CDC allows downstream systems (e.g., ETL pipelines, Delta Lake ingestion, reporting layers) to capture insert, update, and delete operations on source tables without requiring custom triggers. 

2. Prerequisites 

SQL Server Agent must be running — CDC relies on Agent jobs for the capture and cleanup processes. 

The executing account must have sysadmin or db_owner privileges on each target database. 

CDC must be supported by the SQL Server edition in use (Enterprise, Standard 2016 SP1+, or Azure SQL Managed Instance). 

The list of database names and the list of table names should be confirmed and approved before execution. 

3. Scope Definition 

Update the following two lists before running the script: 

Databases: the set of databases CDC should be enabled on. 

Tables: the common table names (identical across all listed databases) that should be tracked. 

Example values used in this script: 

Databases — Database1, Database2, Database3 

Tables — Orders, Customers, Invoices 

4. Script: Enable CDC on Specific Tables Across Multiple Databases 

This script loops through each database in scope, enables CDC at the database level if not already enabled, then loops through the specified table list and enables CDC only on those tables (skipping any table already tracked). 

DECLARE @DatabaseName NVARCHAR(128) 
DECLARE @FullSQL NVARCHAR(MAX) 
 
-- List the databases to enable CDC on 
DECLARE db_cursor CURSOR FOR 
SELECT name FROM sys.databases 
WHERE name IN ('Database1', 'Database2', 'Database3')   -- <-- update this list 
 
OPEN db_cursor 
FETCH NEXT FROM db_cursor INTO @DatabaseName 
 
WHILE @@FETCH_STATUS = 0 
BEGIN 
    SET @FullSQL = ' 
    USE [' + @DatabaseName + ']; 
 
    -- Step 1: Enable CDC at database level if not already enabled 
    IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = ''' + @DatabaseName + ''' AND is_cdc_enabled = 1) 
        EXEC sys.sp_cdc_enable_db; 
 
    -- Step 2: Enable CDC only on specific tables 
    DECLARE @schema NVARCHAR(128), @table NVARCHAR(128) 
 
    DECLARE tbl_cur CURSOR FOR 
        SELECT s.name, t.name 
        FROM sys.tables t 
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id 
        WHERE t.is_ms_shipped = 0 
        AND t.is_tracked_by_cdc = 0 
        AND t.name IN (''Orders'', ''Customers'', ''Invoices'')   -- <-- update your specific table list here 
 
    OPEN tbl_cur 
    FETCH NEXT FROM tbl_cur INTO @schema, @table 
 
    WHILE @@FETCH_STATUS = 0 
    BEGIN 
        BEGIN TRY 
            EXEC sys.sp_cdc_enable_table 
                @source_schema = @schema, 
                @source_name = @table, 
                @role_name = NULL, 
                @supports_net_changes = 1 
            PRINT ''CDC enabled on: ' + @DatabaseName + '.'' + @schema + ''.'' + @table 
        END TRY 
        BEGIN CATCH 
            PRINT ''Failed for table: ' + @DatabaseName + '.'' + @schema + ''.'' + @table + '' - '' + ERROR_MESSAGE() 
        END CATCH 
 
        FETCH NEXT FROM tbl_cur INTO @schema, @table 
    END 
 
    CLOSE tbl_cur 
    DEALLOCATE tbl_cur 
    ' 
 
    EXEC sp_executesql @FullSQL 
 
    FETCH NEXT FROM db_cursor INTO @DatabaseName 
END 
 
CLOSE db_cursor 
DEALLOCATE db_cursor 

5. Verification Script 

Run the following after execution to confirm CDC status for the target tables across all databases in scope: 

DECLARE @DatabaseName NVARCHAR(128) 
DECLARE @SQL NVARCHAR(MAX) = '' 
 
SELECT @SQL = @SQL + ' 
SELECT ''' + name + ''' AS DatabaseName, s.name AS SchemaName, t.name AS TableName, t.is_tracked_by_cdc 
FROM [' + name + '].sys.tables t 
INNER JOIN [' + name + '].sys.schemas s ON t.schema_id = s.schema_id 
WHERE t.name IN (''Orders'', ''Customers'', ''Invoices'') 
UNION ALL ' 
FROM sys.databases 
WHERE name IN ('Database1', 'Database2', 'Database3') 
 
SET @SQL = LEFT(@SQL, LEN(@SQL) - LEN('UNION ALL')) 
EXEC sp_executesql @SQL 

6. Rollback / Disable CDC 

If CDC needs to be reverted for any table or database, use the following pattern: 

-- Disable CDC for a specific table 
EXEC sys.sp_cdc_disable_table 
    @source_schema = 'dbo', 
    @source_name   = 'Orders', 
    @capture_instance = 'all'; 
 
-- Disable CDC at database level (only after all tables are disabled) 
EXEC sys.sp_cdc_disable_db; 

7. Notes and Considerations 

The script skips tables where is_tracked_by_cdc = 1, so it is safe to re-run without duplicating CDC setup. 

@role_name is set to NULL, allowing all database users access to CDC change tables. Set this to a specific database role to restrict access in production environments. 

Errors on individual tables (e.g., missing primary key) are caught and printed per table without stopping the overall script. 

Table name matching is case-sensitive or insensitive depending on the database collation setting. 

CDC introduces additional storage and I/O overhead due to change tables and the capture job; monitor capture job latency after enabling. 

 