# Fabric Control And Audit Reusable Implementation Guide

This document captures the current control/audit approach used for the DartsSrv Fabric pipeline and how to reuse the same pattern for other C# / SAMMS / database migration modules.

The old approach used several audit notebook activities and one control config for the whole Darts flow. The current approach is simpler and faster:

- One reusable notebook: `nb_control_audit_writer`
- Three layer-level configs: Bronze, Silver, Gold
- One start audit notebook activity before the ETL work
- One success finalizer after the Gold publish
- One failure finalizer on the failed/skipped path
- Dynamic site list from `bhg_bronze.dbo.site_mapping`
- Versioned Gold publish instead of truncating the active Gold table

The notebook code is maintained separately here:

[nb_control_audit_writer_complete.md](./nb_control_audit_writer_complete.md)

Use that file as the source of truth for notebook cells. This guide explains how the notebook is wired, what it writes, and how to validate the tables.

## Current Darts Flow

The Darts parent pipeline currently runs in this order:

```text
lkp_darts_site_mapping
-> flt_active_darts_sites
-> nb_darts_audit_start
-> Exected_AfterBronz
-> nb_darts_bronze_to_silver
-> Prepare_DartsSrv_Load_Table
-> Copy_darts_silver_to_gold_load
-> Publish_DartsSrv_Versioned_Gold
-> nb_darts_audit_finalize_success
```

Failure path:

```text
Publish_DartsSrv_Versioned_Gold failed/skipped
-> nb_darts_audit_finalize_failure
```

Because all normal upstream activities feed into the Gold publish chain, the failure finalizer can run when Bronze, Silver, prepare, copy, or publish fails. It receives a combined Fabric activity error message from the parent pipeline.

## Control And Audit Tables Used

The reusable notebook writes to the existing Bronze control/audit tables:

```text
bhg_bronze.meta.etlconfig
bhg_bronze.meta.taskconfig
bhg_bronze.meta.pipelinerun
bhg_bronze.meta.taskqueue
bhg_bronze.meta.taskaudit
bhg_bronze.meta.siteaudit
bhg_bronze.meta.dataquality
```

Table purpose:

| Table | Purpose |
|---|---|
| `etlconfig` | One row per ETL layer/config. For Darts we use one row each for Bronze, Silver, and Gold. |
| `taskconfig` | One row per executable task/table under each config. |
| `pipelinerun` | One row per layer execution for a pipeline run. |
| `taskqueue` | Runtime task row created at the beginning of the run and finalized later. |
| `taskaudit` | Final task result with status, counts, duration, and error message. |
| `siteaudit` | Site-level Darts audit, aggregated from Bronze rows for the current pipeline run. |
| `dataquality` | Silver and Gold DQ counts, including row count, null count, duplicate count, and status. |

## Current Darts Config Rows

For Darts we now use three configs, one per layer:

| Layer | TargetName | ConfigId | ConfigName | TaskConfigId | TaskName | TargetTable |
|---|---:|---:|---|---:|---|---|
| Bronze | `BR` | `25` | `SAMMS DartsSrv Bronze Pipeline` | `55` | `DartsSrv Bronze` | `br_tblDartSrv` |
| Silver | `SL` | `26` | `SAMMS DartsSrv Silver Pipeline` | `56` | `DartsSrv Silver` | `sl_tbldartsrv` |
| Gold | `GL` | `27` | `SAMMS DartsSrv Gold Pipeline` | `57` | `DartsSrv Gold` | `gd_darts_srv` |

The start notebook looks up active configs using:

```sql
ConfigName LIKE 'SAMMS DartsSrv%'
AND TargetName IN ('BR', 'SL', 'GL')
AND IsActive = 1
```

Then it looks up active `taskconfig` rows for those three `ConfigId` values.

Verify the config rows:

```python
display(spark.sql("""
SELECT ConfigId, ConfigName, PipelineName, SourceSystem, SourceType,
       TargetName, EnvironmentName, ExecutionSequence, IsActive, CreatedAt, CreatedBy
FROM bhg_bronze.meta.etlconfig
WHERE ConfigId IN (25, 26, 27)
ORDER BY ConfigId
"""))

display(spark.sql("""
SELECT TaskConfigId, ConfigId, TaskName, SourceTable, LoadType,
       LookbackDays, TargetSchema, TargetTable, ExecutionOrder,
       IsActive, CreatedAt, CreatedBy
FROM bhg_bronze.meta.taskconfig
WHERE TaskConfigId IN (55, 56, 57)
ORDER BY TaskConfigId
"""))
```

## Site Mapping

Darts sites now come from:

```text
bhg_bronze.dbo.site_mapping
```

Expected columns:

```text
SiteCode
ClinicName
DataBaseName
ETLName
MethodName
IsActive
```

The parent pipeline uses:

```text
ETLName = 'Darts'
MethodName = 'DartsSrv'
IsActive = 1
DataBaseName is not null
```

The resulting rows are passed directly to the child pipeline as `p_sites`.

Example site object:

```json
{
  "SiteCode": "AHK",
  "ClinicName": "Ahoskie",
  "DataBaseName": "SAMMS-Ahoskie",
  "ETLName": "Darts",
  "MethodName": "DartsSrv",
  "IsActive": 1
}
```

The child pipeline uses:

```text
item().SiteCode
item().DataBaseName
```

The Darts source schema/table are fixed in the child:

```text
dbo.tblDartsSrv
```

## Reusable Notebook

All three audit activities point to the same notebook:

```text
nb_control_audit_writer
```

Notebook code:

[nb_control_audit_writer_complete.md](./nb_control_audit_writer_complete.md)

The notebook behavior is controlled by `p_mode`.

Active modes used by the optimized Darts parent:

| Mode | Used By | Purpose |
|---|---|---|
| `START_LAYER_RUNS` | `nb_darts_audit_start` | Creates BR/SL/GL `pipelinerun` and `taskqueue` rows and returns audit context JSON. |
| `FINALIZE_DARTS_SUCCESS` | `nb_darts_audit_finalize_success` | Writes siteaudit, Silver DQ, Gold DQ, taskaudit, and marks all BR/SL/GL runs success. |
| `FINALIZE_DARTS_FAILURE` | `nb_darts_audit_finalize_failure` | Writes failure audit rows, stores the Fabric error message, updates statuses, then raises an exception so the pipeline remains failed. |

Older modes still exist in the notebook for reference, but the optimized Darts parent does not use them in the normal path:

```text
WRITE_SITE_AUDIT_BULK_FROM_BRONZE
WRITE_DATAQUALITY
FINISH_TASK_SUCCESS
FINISH_TASK_FAILURE
```

## Notebook Constants

The current notebook is reusable as a pattern, but Cell 1 still has Darts-specific constants:

```python
etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"
pipelinerun_table = "bhg_bronze.meta.pipelinerun"
taskqueue_table = "bhg_bronze.meta.taskqueue"
taskaudit_table = "bhg_bronze.meta.taskaudit"
dataquality_table = "bhg_bronze.meta.dataquality"
siteaudit_table = "bhg_bronze.meta.siteaudit"

bronze_table = "bhg_bronze.Dart.br_tblDartSrv"
silver_table = "bhg_silver.pats.sl_tbldartsrv"
gold_workspace_id = "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
gold_warehouse_id = "d29ef036-8c2c-40b0-a8e0-3279f9a906e7"
gold_warehouse_prefix = "bhg_gold.pats"
```

For another module, keep the same notebook pattern but change or parameterize these values.

Gold is a Warehouse table, so the notebook reads it with the Fabric Spark connector:

```python
from com.microsoft.spark.fabric.Constants import Constants

spark.read \
    .option(Constants.WorkspaceId, gold_workspace_id) \
    .option(Constants.DatawarehouseId, gold_warehouse_id) \
    .synapsesql(f"{gold_warehouse_prefix}.{table_name}")
```

## Parent Pipeline Parameters

The optimized Darts parent only needs this user-facing parameter:

```text
p_lookback_days int default 15
```

The parent uses `@pipeline().RunId` as the ingest run id. That value tags Bronze rows and is passed into the audit finalizers.

The site list is not a parent parameter now. It is read dynamically from `site_mapping`.

## Audit Activity Wiring

### 1. `nb_darts_audit_start`

Attach this activity to the reusable notebook `nb_control_audit_writer`.

Parameters:

| Parameter | Value |
|---|---|
| `p_mode` | `START_LAYER_RUNS` |
| `p_config_name_prefix` | `SAMMS DartsSrv` |
| `p_pipeline_name` | `Execute_DartSrv` |
| `p_pipeline_path` | `/pipelines/Execute_DartSrv` |
| `p_triggered_by` | `Fabric` |

Expected notebook output:

```json
{
  "BR": {
    "run_id": 202606041633453404,
    "config_id": 25,
    "config_name": "SAMMS DartsSrv Bronze Pipeline",
    "target_name": "BR",
    "task_config_id": 55,
    "task_id": 202606041633453405,
    "task_name": "DartsSrv Bronze",
    "target_table": "br_tblDartSrv"
  },
  "SL": {
    "run_id": 202606041633453504,
    "config_id": 26,
    "config_name": "SAMMS DartsSrv Silver Pipeline",
    "target_name": "SL",
    "task_config_id": 56,
    "task_id": 202606041633453505,
    "task_name": "DartsSrv Silver",
    "target_table": "sl_tbldartsrv"
  },
  "GL": {
    "run_id": 202606041633453604,
    "config_id": 27,
    "config_name": "SAMMS DartsSrv Gold Pipeline",
    "target_name": "GL",
    "task_config_id": 57,
    "task_id": 202606041633453605,
    "task_name": "DartsSrv Gold",
    "target_table": "gd_darts_srv"
  }
}
```

### 2. Child Bronze Pipeline `Exected_AfterBronz`

The parent passes:

| Child Parameter | Value |
|---|---|
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` |
| `p_sites` | `@activity('flt_active_darts_sites').output.value` |
| `p_audit_context_json` | `@activity('nb_darts_audit_start').output.result.exitValue` |

`p_audit_context_json` is included for consistency and future use. The current child copy path mainly needs `p_ingest_run_id`, `p_lookback_days`, and `p_sites`.

### 3. `nb_darts_audit_finalize_success`

Attach this activity to the same reusable notebook `nb_control_audit_writer`.

Dependency:

```text
Publish_DartsSrv_Versioned_Gold Succeeded
```

Parameters:

| Parameter | Value |
|---|---|
| `p_mode` | `FINALIZE_DARTS_SUCCESS` |
| `p_audit_context_json` | `@activity('nb_darts_audit_start').output.result.exitValue` |
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_sites_json` | `@string(activity('flt_active_darts_sites').output.value)` |
| `p_status` | `SUCCESS` |

This one notebook run handles all success auditing:

- `siteaudit` for all active Darts sites
- Silver `dataquality`
- Gold `dataquality`
- BR/SL/GL `taskqueue` final status
- BR/SL/GL `taskaudit`
- BR/SL/GL `pipelinerun` final status

This avoids running one notebook per site. That matters because Darts can have many active sites.

### 4. `nb_darts_audit_finalize_failure`

Attach this activity to the same reusable notebook `nb_control_audit_writer`.

Dependency:

```text
Publish_DartsSrv_Versioned_Gold Failed or Skipped
```

Parameters:

| Parameter | Value |
|---|---|
| `p_mode` | `FINALIZE_DARTS_FAILURE` |
| `p_failed_target_name` | `ALL` |
| `p_failure_activity` | `Darts_Bronze_Silver_Gold_Finalizer` |
| `p_audit_context_json` | `@activity('nb_darts_audit_start').output.result.exitValue` |
| `p_ingest_run_id` | `@pipeline().RunId` |
| `p_sites_json` | `@string(activity('flt_active_darts_sites').output.value)` |
| `p_error_message` | Combined Fabric activity error expression |
| `p_status` | `FAILED` |

Current combined error expression:

```text
@string(coalesce(
  activity('Exected_AfterBronz').error,
  activity('nb_darts_bronze_to_silver').error,
  activity('Prepare_DartsSrv_Load_Table').error,
  activity('Copy_darts_silver_to_gold_load').error,
  activity('Publish_DartsSrv_Versioned_Gold').error,
  'Darts pipeline failed, but no activity error object was available.'
))
```

The failure notebook intentionally raises an exception after writing audit rows. That keeps the Fabric pipeline status as failed while still recording the audit details.

## Gold Versioned Publish

Darts no longer uses a direct truncate of the active Gold table.

Current Gold flow:

```text
Prepare_DartsSrv_Load_Table
-> Copy_darts_silver_to_gold_load
-> Publish_DartsSrv_Versioned_Gold
```

The idea:

1. Keep the current reporting Gold table available while the load is running.
2. Load the refreshed data into a load/version table.
3. Publish/switch only after the load succeeds.
4. If the load fails, the active Gold table stays untouched.

This matches the recommended approach for full overwrite/reporting tables.

## What The Success Finalizer Writes

On success, `FINALIZE_DARTS_SUCCESS`:

1. Reads current Bronze rows for this run:

```text
bhg_bronze.Dart.br_tblDartSrv
where _ingest_run_id = @pipeline().RunId
```

2. Groups by:

```text
_site_code
_source_database
```

3. Joins to the active site list passed from the parent.

4. Writes one `siteaudit` row per active site:

```text
ETLName = Darts
MethodName = DartsSrv
PipelineRunId = BR run_id from audit context
RowCount = Bronze rows copied for that site/run
RowsInserted = Bronze rows copied for that site/run
Status = SUCCESS
```

5. Reads Silver and Gold and writes `dataquality`.

Silver DQ keys:

```text
_site_code
dsID
```

Gold DQ keys:

```text
SiteCode
DsId
```

6. Finalizes BR/SL/GL `taskqueue`, `taskaudit`, and `pipelinerun`.

## What The Failure Finalizer Writes

On failure, `FINALIZE_DARTS_FAILURE`:

1. Reads the audit context from `nb_darts_audit_start`.
2. Reads the current run's Bronze rows if any exist.
3. Writes failure `siteaudit`.
4. Updates BR/SL/GL `taskqueue` and `pipelinerun`.
5. Inserts BR/SL/GL `taskaudit`.
6. Stores the passed `p_error_message`.
7. Raises an exception so Fabric keeps the pipeline failed.

Current limitation:

With the optimized design, we do not run an audit notebook after each individual site copy. That keeps performance reasonable. Because of that, a Bronze failure can be captured as a pipeline/layer failure, but the notebook cannot always identify the exact failed site unless the child pipeline passes that detail or writes per-site failure metadata.

Also, failures before `nb_darts_audit_start` cannot use the normal audit context because the BR/SL/GL runtime rows do not exist yet.

## Validation After A Successful Run

Use these in a Lakehouse notebook:

```python
display(spark.sql("""
SELECT RunId, ConfigId, ConfigName, TargetName, Status,
       TotalTasks, SuccessTasks, FailedTasks, SkippedTasks,
       StartTime, EndTime, CreatedAt
FROM bhg_bronze.meta.pipelinerun
WHERE ConfigId IN (25, 26, 27)
ORDER BY CreatedAt DESC
LIMIT 15
"""))
```

Expected:

```text
BR, SL, GL rows are SUCCESS
SuccessTasks = 1
FailedTasks = 0
SkippedTasks = 0
```

Task queue:

```python
display(spark.sql("""
SELECT TaskId, RunId, ConfigId, TaskConfigId, TaskName,
       TargetTable, Status, StartTime, EndTime, ErrorMessage, CreatedAt
FROM bhg_bronze.meta.taskqueue
WHERE TaskConfigId IN (55, 56, 57)
ORDER BY CreatedAt DESC
LIMIT 15
"""))
```

Task audit:

```python
display(spark.sql("""
SELECT AuditId, TaskId, RunId, ConfigId, TaskConfigId, TaskName,
       TableName, StepName, Status, LogLevel,
       RowsRead, RowsWritten, RowsFailed,
       ErrorMessage, StartTime, EndTime, DurationSeconds, CreatedAt
FROM bhg_bronze.meta.taskaudit
WHERE TaskConfigId IN (55, 56, 57)
ORDER BY CreatedAt DESC
LIMIT 15
"""))
```

Site audit:

```python
display(spark.sql("""
SELECT PipelineRunId, ETLName, MethodName, SiteCode, DataBaseName,
       RowCount, RowsInserted, RowsUpdated, ReloadFlag,
       Status, ErrorMessage, CreatedDate
FROM bhg_bronze.meta.siteaudit
WHERE ETLName = 'Darts'
  AND MethodName = 'DartsSrv'
ORDER BY CreatedDate DESC
LIMIT 50
"""))
```

Data quality:

```python
display(spark.sql("""
SELECT DqId, RunId, ConfigId, TaskConfigId, TableName,
       RowCount, NullCount, DuplicateCount, ValidationStatus, CreatedAt
FROM bhg_bronze.meta.dataquality
WHERE TaskConfigId IN (56, 57)
ORDER BY CreatedAt DESC
LIMIT 20
"""))
```

## Validation After A Failure Test

After intentionally failing one activity, check:

```python
display(spark.sql("""
SELECT RunId, ConfigId, ConfigName, TargetName, Status,
       SuccessTasks, FailedTasks, SkippedTasks, EndTime
FROM bhg_bronze.meta.pipelinerun
WHERE ConfigId IN (25, 26, 27)
ORDER BY CreatedAt DESC
LIMIT 15
"""))

display(spark.sql("""
SELECT TaskId, RunId, ConfigId, TaskConfigId, TaskName,
       Status, ErrorMessage, EndTime
FROM bhg_bronze.meta.taskqueue
WHERE TaskConfigId IN (55, 56, 57)
ORDER BY CreatedAt DESC
LIMIT 15
"""))

display(spark.sql("""
SELECT AuditId, RunId, TaskConfigId, TaskName, Status,
       LogLevel, RowsRead, RowsWritten, RowsFailed,
       ErrorMessage, CreatedAt
FROM bhg_bronze.meta.taskaudit
WHERE TaskConfigId IN (55, 56, 57)
ORDER BY CreatedAt DESC
LIMIT 15
"""))

display(spark.sql("""
SELECT PipelineRunId, SiteCode, DataBaseName, RowCount,
       Status, ErrorMessage, CreatedDate
FROM bhg_bronze.meta.siteaudit
WHERE ETLName = 'Darts'
  AND MethodName = 'DartsSrv'
ORDER BY CreatedDate DESC
LIMIT 50
"""))
```

Expected:

```text
At least one layer has Status = FAILED.
ErrorMessage contains the Fabric activity error object/string passed from the parent.
The pipeline itself still shows failed, because the failure finalizer raises an exception after auditing.
```

## Troubleshooting

### DML Not Supported

Error:

```text
Data Manipulation Language (DML) statements are not supported for this table type
```

Cause:

You are trying to insert/update Lakehouse Delta tables from the SQL endpoint.

Fix:

Use a Lakehouse notebook with PySpark:

```python
spark.sql("INSERT INTO bhg_bronze.meta.taskconfig ...")
```

### Missing Task Config

Error:

```text
No active taskconfig row found for ConfigIds ..., found ...
```

Check:

```python
display(spark.sql("""
SELECT TaskConfigId, ConfigId, TaskName, TargetTable, IsActive, CreatedAt
FROM bhg_bronze.meta.taskconfig
WHERE ConfigId IN (25, 26, 27)
   OR TaskConfigId IN (55, 56, 57)
ORDER BY ConfigId, TaskConfigId
"""))
```

The start notebook expects exactly three active `taskconfig` rows for the three active Darts `etlconfig` rows.

### Missing ETL Config

Error:

```text
Expected 3 active etlconfig rows for prefix SAMMS DartsSrv, found ...
```

Check:

```python
display(spark.sql("""
SELECT ConfigId, ConfigName, TargetName, ExecutionSequence, IsActive
FROM bhg_bronze.meta.etlconfig
WHERE ConfigName LIKE 'SAMMS DartsSrv%'
ORDER BY ConfigId
"""))
```

Expected active target names:

```text
BR
SL
GL
```

### Notebook Output Is Null

The child and finalizer activities need:

```text
@activity('nb_darts_audit_start').output.result.exitValue
```

If this is null, confirm:

1. `nb_darts_audit_start` is attached to `nb_control_audit_writer`.
2. `p_mode = START_LAYER_RUNS`.
3. The notebook has both cells from [nb_control_audit_writer_complete.md](./nb_control_audit_writer_complete.md).
4. The notebook completed successfully.

### Failure Finalizer Writes Audit But Pipeline Still Fails

This is expected. `FINALIZE_DARTS_FAILURE` writes audit rows and then raises an exception intentionally. That keeps the pipeline run failed instead of hiding the original ETL failure.

## Reusing This Pattern For Other Modules

For another module, follow this checklist:

1. Decide whether the module needs layer-level audit rows like Darts.
2. Create three active `etlconfig` rows if the module has Bronze, Silver, and Gold stages.
3. Use `TargetName` values `BR`, `SL`, and `GL`.
4. Create one active `taskconfig` row per layer/task.
5. Add or verify source site rows in `site_mapping` if the module is site-based.
6. Add a start notebook activity before ETL work.
7. Add one success finalizer after the last publish/load activity.
8. Add one failure finalizer from the last publish/load activity with `Failed` and `Skipped` dependency conditions.
9. Pass the start notebook `exitValue` into the child/finalizer as `p_audit_context_json`.
10. Pass the parent pipeline run id as `p_ingest_run_id`.
11. Update or parameterize notebook table constants for that module.

Recommended generic naming:

| Layer | ConfigName Pattern | TargetName | TaskName Pattern |
|---|---|---|---|
| Bronze | `<Module> Bronze Pipeline` | `BR` | `<Module> Bronze` |
| Silver | `<Module> Silver Pipeline` | `SL` | `<Module> Silver` |
| Gold | `<Module> Gold Pipeline` | `GL` | `<Module> Gold` |

For Darts the prefix is:

```text
SAMMS DartsSrv
```

For another module, the start notebook can use a different prefix:

```text
p_config_name_prefix = SAMMS ClientDemo
```

But the current notebook still has Darts table names and Darts DQ keys. Change those before reusing it for a different table.

## Recommended Future Improvement

To make `nb_control_audit_writer` fully generic across all modules, move these Darts-specific values from notebook constants into notebook parameters:

```text
p_bronze_table
p_silver_table
p_gold_workspace_id
p_gold_warehouse_id
p_gold_warehouse_prefix
p_gold_table
p_site_column
p_source_site_column
p_bronze_ingest_column
p_silver_key_columns_json
p_gold_key_columns_json
p_etl_name
p_method_name
```

That would let one notebook support Darts, Forms, Clients, Payers, and other modules without copying notebook code.

For now, Darts is correctly optimized with:

```text
1 start notebook
1 success finalizer notebook
1 failure finalizer notebook
```

No per-site audit notebook is required in the success path.
