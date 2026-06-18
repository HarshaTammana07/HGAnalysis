# Notes Dynamic Control/Audit Reference

This document explains the dynamic control/audit pattern used for the Notes Fabric pipeline.

The implementation is in:

```text
nb_notes_control_audit_writer.md
```

The same notebook is attached to three parent pipeline activities:

```text
nb_notes_audit_start
nb_notes_audit_finalize_success
nb_notes_audit_finalize_failure
```

## Goal

The audit notebook should not hardcode Notes table names, method names, or DQ keys.

Instead, it reads runtime behavior from:

```text
bhg_bronze.meta.etlconfig
bhg_bronze.meta.taskconfig
```

The same pattern can be reused for an ETL with 2 methods or 50+ methods, as long as the control table rows are configured correctly.

## What Stays Constant

These are environment-level constants and are intentionally kept inside the notebook:

```python
gold_workspace_id = "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
gold_warehouse_id = "d29ef036-8c2c-40b0-a8e0-3279f9a906e7"
gold_warehouse_prefix = "bhg_gold.pats"

etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"
pipelinerun_table = "bhg_bronze.meta.pipelinerun"
taskqueue_table = "bhg_bronze.meta.taskqueue"
taskaudit_table = "bhg_bronze.meta.taskaudit"
dataquality_table = "bhg_bronze.meta.dataquality"
```

These values identify the shared audit framework and the Gold Warehouse connection.

## How The Notebook Knows It Is Notes

The notebook does not identify Notes by hardcoded table names.

It uses this pipeline/notebook parameter:

```python
p_config_name_prefix = "SAMMS Notes"
```

At audit start, the notebook reads active BR/SL/GL config rows:

```sql
SELECT *
FROM bhg_bronze.meta.etlconfig
WHERE IsActive = 1
  AND ConfigName LIKE 'SAMMS Notes%'
  AND TargetName IN ('BR', 'SL', 'GL')
```

For Notes, this should find three rows:

```text
SAMMS Notes Bronze Pipeline
SAMMS Notes Silver Pipeline
SAMMS Notes Gold Pipeline
```

Then it reads all active `taskconfig` rows for those three `ConfigId` values.

## Required ETL Config Rows

Notes uses one config row per layer:

```text
BR = Bronze
SL = Silver
GL = Gold
```

Example:

```text
ConfigId  TargetName  ConfigName
34        BR          SAMMS Notes Bronze Pipeline
35        SL          SAMMS Notes Silver Pipeline
36        GL          SAMMS Notes Gold Pipeline
```

The exact IDs can differ by environment, but the notebook expects one active row for each target:

```text
BR
SL
GL
```

## Required Task Config Rows

Each method/table needs taskconfig rows.

For Notes we currently have:

```text
3pArnote
3pClaimNote
```

Bronze is site-level, so Bronze normally has one task per active site per method.

Silver and Gold are method/table-level, so they normally have one task per method/table.

Example with 2 methods and 115 sites:

```text
Bronze taskqueue rows = 2 methods x 115 sites = 230
Silver taskqueue rows = 2
Gold taskqueue rows = 2
```

For 57 methods and 115 sites:

```text
Bronze taskqueue rows = 57 methods x 115 sites = 6555
Silver taskqueue rows = 57
Gold taskqueue rows = 57
```

The notebook can handle this shape, but final audit/DQ runtime will scale with the number of method tables.

## RequestBody Contract

The dynamic behavior comes from `bhg_bronze.meta.taskconfig.RequestBody`.

### Bronze RequestBody

Bronze tasks must include the table and the columns used for run/site/database counting:

```json
{
  "full_table": "bhg_bronze.Notes.br_tbl3pArnote",
  "ingest_column": "_ingest_run_id",
  "site_column": "_site_code",
  "database_column": "_source_database"
}
```

For claim notes:

```json
{
  "full_table": "bhg_bronze.Notes.br_tbl3pClaimNote",
  "ingest_column": "_ingest_run_id",
  "site_column": "_site_code",
  "database_column": "_source_database"
}
```

### Silver RequestBody

Silver tasks must include the table and DQ key columns:

```json
{
  "full_table": "bhg_silver.pats.sl_tbl_3pARNOTE",
  "dq_keys": ["_site_code", "arnID"]
}
```

For claim notes:

```json
{
  "full_table": "bhg_silver.pats.sl_tbl_3pClaimNote",
  "dq_keys": ["_site_code", "tpcnTPCID"]
}
```

### Gold RequestBody

Gold tasks must include the Gold table and DQ key columns:

```json
{
  "full_table": "gd_3p_arnote",
  "dq_keys": ["SiteCode", "arnID"]
}
```

For claim notes:

```json
{
  "full_table": "gd_3p_claim_note",
  "dq_keys": ["SiteCode", "tpcnTPCID"]
}
```

Gold `full_table` can be the short table name because the notebook uses:

```python
gold_warehouse_prefix = "bhg_gold.pats"
```

So this:

```text
gd_3p_arnote
```

is read as:

```text
bhg_gold.pats.gd_3p_arnote
```

## Audit Start Flow

Mode:

```python
p_mode = "START_LAYER_RUNS"
```

The notebook:

1. Reads active Notes BR/SL/GL rows from `etlconfig`.
2. Reads all active related rows from `taskconfig`.
3. Creates one `pipelinerun` row per layer:

```text
BR = RUNNING
SL = RUNNING
GL = RUNNING
```

4. Creates one `taskqueue` row per active task.
5. Returns an audit context JSON to the parent pipeline.

The returned JSON includes each task's:

```text
run_id
config_id
task_config_id
task_id
target_name
method
site_code
data_base_name
full_table
dq_keys
ingest_column
site_column
database_column
```

That JSON is passed to the final success/failure audit activity.

## Success Finalize Flow

Mode:

```python
p_mode = "FINALIZE_NOTES_SUCCESS"
```

The notebook:

1. Reads BR/SL/GL from `p_audit_context_json`.
2. Updates Bronze taskqueue rows to `SUCCESS`.
3. Writes Bronze taskaudit rows.
4. Runs Silver DQ and writes `dataquality`.
5. Runs Gold DQ and writes `dataquality`.
6. Updates Silver taskqueue rows to `SUCCESS`.
7. Writes Silver taskaudit rows.
8. Updates Gold taskqueue rows to `SUCCESS`.
9. Writes Gold taskaudit rows.
10. Updates all three pipelinerun rows to `SUCCESS`.

## Bronze Site Count Optimization

Bronze is site-level, so without optimization the notebook would count one site at a time.

That would be expensive:

```text
57 methods x 115 sites = 6555 count queries
```

The current notebook avoids that.

For each Bronze method/table, it does one grouped count:

```python
df = spark.table(full_table).where(F.col(ingest_column) == F.lit(p_ingest_run_id))

counts_df = (
    df.groupBy(site_column, database_column)
      .count()
)
```

Then it stores the results in memory as a lookup:

```text
(method, table, site, database) -> row_count
```

The output still writes one taskaudit row per site task, but the count is read from the grouped result instead of running another Spark count.

So the cost becomes closer to:

```text
57 grouped Bronze count queries
```

instead of:

```text
6555 individual Bronze count queries
```

## Silver And Gold Counts

Silver and Gold are method/table-level.

For Silver taskaudit:

```text
RowsRead    = total grouped Bronze rows for the matching method
RowsWritten = current row count of the Silver table
```

For Gold taskaudit:

```text
RowsRead    = current row count of the matching Silver table
RowsWritten = current row count of the Gold Warehouse table
```

## Data Quality Flow

DQ runs for Silver and Gold using `dq_keys` from `taskconfig.RequestBody`.

For each configured table, the notebook calculates:

```text
RowCount
NullCount
DuplicateCount
ValidationStatus
```

`ValidationStatus` is:

```text
SUCCESS when NullCount = 0 and DuplicateCount = 0
FAILED otherwise
```

This writes to:

```text
bhg_bronze.meta.dataquality
```

Important runtime note:

DQ still runs per Silver/Gold method table. For many methods, this can be one of the slower parts of final audit.

## Failure Finalize Flow

Mode:

```python
p_mode = "FINALIZE_NOTES_FAILURE"
```

The parent pipeline passes:

```text
p_failed_target_name
p_failure_activity
p_error_message
```

The notebook:

1. Finds BR/SL/GL audit context.
2. Marks the failed layer as `FAILED`.
3. Marks earlier layers as `SUCCESS` if they completed before the failure.
4. Marks later layers as `SKIPPED`.
5. Writes taskaudit rows.
6. Updates pipelinerun and taskqueue statuses.
7. Raises an exception at the end so the parent pipeline remains failed.

This keeps audit tables updated while preserving the pipeline failure status.

## Reuse Rules For Another ETL

To reuse this pattern for another ETL:

1. Create BR/SL/GL rows in `etlconfig`.
2. Use a unique `ConfigName` prefix, for example:

```text
SAMMS SomeOtherETL
```

3. Pass that value into:

```python
p_config_name_prefix
```

4. Create active `taskconfig` rows for each method/table.
5. Populate `Method`.
6. Populate `RequestBody`.
7. Attach the same style of audit notebook to:

```text
audit start
audit finalize success
audit finalize failure
```

The notebook should not need method-specific table hardcoding.

## Validation Queries

Check pipeline run rows:

```sql
SELECT *
FROM bhg_bronze.meta.pipelinerun
WHERE ConfigName LIKE 'SAMMS Notes%'
ORDER BY CreatedAt DESC;
```

Check task queue rows:

```sql
SELECT
    ConfigId,
    TaskName,
    Method,
    SiteCode,
    DataBaseName,
    Status,
    StartTime,
    EndTime
FROM bhg_bronze.meta.taskqueue
WHERE ConfigId IN (34, 35, 36)
ORDER BY CreatedAt DESC;
```

Check task audit by method/site:

```sql
SELECT
    ConfigId,
    TaskConfigId,
    TaskName,
    TableName,
    SiteCode,
    DataBaseName,
    Status,
    RowsRead,
    RowsWritten,
    RowsFailed,
    ErrorMessage,
    StartTime,
    EndTime,
    DurationSeconds
FROM bhg_bronze.meta.taskaudit
WHERE ConfigId IN (34, 35, 36)
ORDER BY CreatedAt DESC;
```

Check DQ:

```sql
SELECT
    ConfigId,
    TaskConfigId,
    TableName,
    RowCount,
    NullCount,
    DuplicateCount,
    ValidationStatus,
    CreatedAt
FROM bhg_bronze.meta.dataquality
WHERE ConfigId IN (35, 36)
ORDER BY CreatedAt DESC;
```

## Performance Notes

The Bronze site count is optimized with grouped counts.

However, total runtime still depends on:

```text
number of methods
number of Silver tables
number of Gold tables
DQ cost per table
Gold Warehouse read latency
Spark notebook startup time
Delta write concurrency
```

If final audit becomes slow for 50+ methods, optimize next in this order:

1. Make DQ optional by pipeline parameter.
2. Combine row count and null count in one aggregation.
3. Avoid duplicate checks where the table already has guaranteed keys.
4. Pass copy activity row counts into audit instead of recounting Silver/Gold.
5. Split very large ETLs into method groups if needed.

## Current Notes Methods

Current Notes methods:

```text
3pArnote
3pClaimNote
```

Current active test can be scaled from selected sites to all active sites by updating `taskconfig.IsActive`.

