# Optional Gold Layer Control Guide

## Purpose

Some ETLs use Silver as the final Fabric table and keep Gold only as an optional warehouse copy for reporting/backward compatibility.

The goal is to let users enable or disable the Gold copy from control tables, without manually activating/deactivating Gold activities in the Fabric pipeline UI.

## Main Rule

Gold should be controlled by `etlconfig` / `taskconfig` `IsActive`.

If Gold is active, run the Gold copy/publish step.

If Gold is inactive, skip Gold cleanly.

Bronze and Silver should continue to run normally.

## Single-Table ETL Pattern

This is good for ETLs like:

- FormQuestionAnswers
- FormAnswerSignatures
- DartsSrv, if only one Gold table is involved

Flow:

```text
nb_get_<etl>_taskconfig
   |
filter active Bronze/Silver site rows
   |
Bronze
   |
Silver
   |
filter active Gold task rows
   |
if Gold active
      Prepare Gold table
      Copy Silver to Gold
      Validate/Publish Gold
else
      do nothing
   |
Audit finalize based on BR/SL
```

The Gold filter should reuse the first taskconfig notebook output. Do not call the taskconfig notebook again.

Example filter input:

```text
@json(activity('nb_get_FormQuestionAnswers_taskconfig').output.result.exitValue)
```

Example FormQuestionAnswers Gold filter condition:

```text
@and(equals(item().ConfigId, 30), equals(item().IsActive, 1))
```

Example If Condition:

```text
@greater(activity('flt_active_formqa_gold').output.FilteredItemsCount, 0)
```

If `FilteredItemsCount` is `0`, the Gold activities are skipped.

## Multi-Method ETL Pattern

For ETLs with many methods/tables, do not create `3 * method_count` activities.

For example, P1 Reference may have 8, 9, or more methods. Instead of:

```text
Prepare Gold x 9
Copy Silver to Gold x 9
Validate Gold x 9
```

Use:

```text
filter active Gold task rows
   |
if active Gold task count > 0
      run one reusable Gold publish notebook
else
      do nothing
```

The notebook can loop through active Gold taskconfig rows and process each method/table dynamically.

## Reusable Gold Notebook Responsibilities

A reusable optional Gold notebook can:

1. Read the active Gold task rows passed from the parent pipeline or directly from `taskconfig`.
2. For each active Gold row:
   - read `SourceTable` as the Silver source table
   - read `TargetSchema` + `TargetTable` as the Gold target table
   - truncate/overwrite the Gold target
   - copy Silver rows into Gold
   - validate row count
3. If no Gold rows are active, exit cleanly:

```json
{"status":"SKIPPED","message":"No active Gold tasks"}
```

For Fabric Warehouse writes from Spark, use the warehouse connector pattern:

```python
from com.microsoft.spark.fabric.Constants import Constants

(
    df.write
    .option(Constants.WorkspaceId, gold_workspace_id)
    .option(Constants.DatawarehouseId, gold_warehouse_id)
    .mode("overwrite")
    .synapsesql("bhg_gold.pats.TargetTable")
)
```

## Control Table Setup

Even when Gold is optional, keep the Gold config/task rows.

Example single-table setup:

```text
Bronze ConfigId = 28
Silver ConfigId = 29
Gold   ConfigId = 30
```

Gold taskconfig row should contain:

```text
ConfigId      = Gold config id
IsActive      = 1 or 0
SourceTable   = Silver table
TargetSchema  = Gold schema
TargetTable   = Gold table
RequestBody   = Gold DQ/table metadata if needed
```

Client/user controls Gold by updating:

```text
IsActive = 1 -> run Gold
IsActive = 0 -> skip Gold
```

## Audit Behavior

For current BR/SL-final ETLs, audit should remain based on Bronze and Silver only.

Gold is only a copy of Silver, so Gold audit is not required unless the framework requirement changes.

Recommended:

```text
p_active_target_layers_json = ["BR", "SL"]
p_terminal_target_name = "SL"
```

Gold being active or inactive should not change BR/SL audit finalization.

## Why Not Manually Disable Activities?

Manual activity activation/deactivation works, but it requires editing the pipeline each time.

Control-table driven Gold skipping is better because:

- client can enable/disable by data change
- no pipeline structure change is needed
- Silver remains the stable final table
- multi-method ETLs stay clean
- optional Gold behavior is consistent across ETLs

## Recommended Design

For one-table ETLs:

```text
Use Filter + If Condition around the three Gold activities.
```

For many-table/many-method ETLs:

```text
Use Filter + If Condition around one reusable optional Gold notebook.
```

In both cases, use the taskconfig output from the first active taskconfig notebook.

## P1 Reference Example Placement

For `pl_execute_reference`, optional Gold should run only after Bronze and Silver both complete successfully.

Important Fabric limitation: do not place an `If Condition` inside another `If Condition`. Fabric validation rejects nested If activities. Use a top-level Gold If after the Bronze/Silver success check.

Recommended parent flow:

```text
nb_get_p1_reference_taskconfig
   |
flt_active_p1_reference_sites
   |
nb_p1_reference_audit_start
   |
Executed_AfterBronz
   |
set_bronze_method_results_from_child
   |
Executed_AfterSilver
   |
set_silver_method_results_from_child
   |
if_all_reference_methods_success
   |-----------------------------|
   | True                        | False
   v                             v
flt_active_p1_reference_gold     nb_p1_reference_audit_finalize_failure
   |                             |
if_p1_reference_gold_active      nb_p1_reference_notify_failed
   |----------------------|
   | True                 | False
   v                      v
nb_p1_reference_optional_        nb_p1_reference_audit_finalize_success
gold_publish
   |
nb_p1_reference_audit_finalize_success
```

The Gold filter should be connected from the `True` output of `if_all_reference_methods_success`, but the `if_p1_reference_gold_active` activity itself should be top-level, not nested inside the previous If.

Reason:

- if Bronze/Silver fails, optional Gold should not run
- if Bronze/Silver succeeds, check whether any Gold tasks are active
- if active Gold count is greater than zero, run one reusable Gold notebook
- if active Gold count is zero, skip Gold and finalize success
- no Gold notebook startup time is spent when Gold has zero active rows

P1 Reference Gold filter input:

```text
@json(activity('nb_get_p1_reference_taskconfig').output.result.exitValue)
```

Example Gold filter condition, assuming Gold `ConfigId = 90`:

```text
@and(equals(item().ConfigId, 90), equals(item().IsActive, 1))
```

Gold If Condition expression:

```text
@greater(activity('flt_active_p1_reference_gold').output.FilteredItemsCount, 0)
```

The reusable Gold notebook should receive the filtered active Gold task rows, not all task rows.

Gold notebook parameter:

```text
p_gold_tasks_json = @string(activity('flt_active_p1_reference_gold').output.value)
```

Expected behavior:

```text
Gold active > 0:
  success check -> Gold filter -> Gold If True -> Gold notebook -> success audit

Gold active = 0:
  success check -> Gold filter -> Gold If False -> success audit directly

Bronze/Silver failed:
  success check False -> failure audit -> failed notification
```

## P1 Reference Gold TaskConfig Correction Example

For P1 Reference, Gold `ConfigId = 90` should point to the same final table names as Silver, not the older `gd_*` names.

Example Silver-to-Gold mapping:

| Method | SourceTable | TargetSchema | TargetTable |
| --- | --- | --- | --- |
| SaveClinic | `bhg_silver.ctrl.tbl_Clinic` | `ctrl` | `tbl_Clinic` |
| Save3pSetup | `bhg_silver.ctrl.tbl_3PSETUP` | `ctrl` | `tbl_3PSETUP` |
| SaveCodes | `bhg_silver.pats.tbl_Codes` | `pats` | `tbl_Codes` |
| SaveServices | `bhg_silver.pats.tbl_SERVICES` | `pats` | `tbl_SERVICES` |
| SavedropDownListItems | `bhg_silver.ctrl.tbl_DroDownListItems` | `ctrl` | `tbl_DroDownListItems` |
| SaveCustomAnswers | `bhg_silver.pats.tbl_CustomAnswers` | `pats` | `tbl_CustomAnswers` |
| SaveCustomQuestions | `bhg_silver.pats.tbl_CustomQuestions` | `pats` | `tbl_CustomQuestions` |
| SavePreAdmissionV6 | `bhg_silver.ayx.tbl_PreAdmission_V6` | `ayx` | `tbl_PreAdmission_V6` |
| SavePreAdminReferrals | `bhg_silver.pats.tbl_PreadmissionReferralSource` | `pats` | `tbl_PreadmissionReferralSource` |

Example PySpark update for `ConfigId = 90`, activating three Gold methods for testing:

```python
from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import StructType, StructField, StringType
import json

taskconfig_table = "bhg_bronze.meta.taskconfig"

gold_updates = [
    ("SaveClinic", "bhg_silver.ctrl.tbl_Clinic", "ctrl", "tbl_Clinic"),
    ("Save3pSetup", "bhg_silver.ctrl.tbl_3PSETUP", "ctrl", "tbl_3PSETUP"),
    ("SaveCodes", "bhg_silver.pats.tbl_Codes", "pats", "tbl_Codes"),
    ("SaveServices", "bhg_silver.pats.tbl_SERVICES", "pats", "tbl_SERVICES"),
    ("SavedropDownListItems", "bhg_silver.ctrl.tbl_DroDownListItems", "ctrl", "tbl_DroDownListItems"),
    ("SaveCustomAnswers", "bhg_silver.pats.tbl_CustomAnswers", "pats", "tbl_CustomAnswers"),
    ("SaveCustomQuestions", "bhg_silver.pats.tbl_CustomQuestions", "pats", "tbl_CustomQuestions"),
    ("SavePreAdmissionV6", "bhg_silver.ayx.tbl_PreAdmission_V6", "ayx", "tbl_PreAdmission_V6"),
    ("SavePreAdminReferrals", "bhg_silver.pats.tbl_PreadmissionReferralSource", "pats", "tbl_PreadmissionReferralSource"),
]

active_methods = {"SaveClinic", "Save3pSetup", "SaveCodes"}

schema = StructType([
    StructField("Method", StringType(), False),
    StructField("SourceTable", StringType(), False),
    StructField("TargetSchema", StringType(), False),
    StructField("TargetTable", StringType(), False),
    StructField("TargetPath", StringType(), False),
    StructField("RequestBody", StringType(), False),
    StructField("IsActive", StringType(), False),
])

rows = []
for method, source_table, target_schema, target_table in gold_updates:
    request_body = json.dumps(
        {"full_table": f"bhg_gold.{target_schema}.{target_table}"},
        separators=(",", ":")
    )

    rows.append((
        method,
        source_table,
        target_schema,
        target_table,
        f"bhg_gold.{target_schema}.{target_table}",
        request_body,
        "1" if method in active_methods else "0"
    ))

updates_df = spark.createDataFrame(rows, schema)

DeltaTable.forName(spark, taskconfig_table).alias("t") \
    .merge(
        updates_df.alias("s"),
        "t.ConfigId = 90 AND lower(t.Method) = lower(s.Method)"
    ) \
    .whenMatchedUpdate(set={
        "SourceTable": "s.SourceTable",
        "TargetSchema": "s.TargetSchema",
        "TargetTable": "s.TargetTable",
        "TargetPath": "s.TargetPath",
        "RequestBody": "s.RequestBody",
        "IsActive": "CAST(s.IsActive AS INT)",
        "ModifiedAt": "current_timestamp()",
        "ModifiedBy": "'Harsha'"
    }) \
    .execute()

display(
    spark.table(taskconfig_table)
    .where(F.col("ConfigId") == 90)
    .select(
        "TaskConfigId",
        "ConfigId",
        "Method",
        "SourceTable",
        "TargetSchema",
        "TargetTable",
        "TargetPath",
        "LoadType",
        "IsActive",
        "RequestBody",
        "ModifiedAt",
        "ModifiedBy"
    )
    .orderBy("Method")
)
```
