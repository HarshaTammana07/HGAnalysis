# nb_p1_reference_optional_gold_publish

Use this notebook for the optional P1 Reference Gold publish step.

The parent pipeline passes only active Gold `taskconfig` rows into `p_gold_tasks_json`. If no Gold rows are active, the parent `If Condition` should skip this notebook.

## Cell 1

```python
from datetime import datetime
import json

from pyspark.sql import functions as F

try:
    p_gold_tasks_json
except NameError:
    p_gold_tasks_json = "[]"

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = None

try:
    p_pipeline_run_id
except NameError:
    p_pipeline_run_id = None

try:
    p_config_name_prefix
except NameError:
    p_config_name_prefix = "SAMMS P1 Reference"

gold_workspace_id = "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
gold_warehouse_id = "d29ef036-8c2c-40b0-a8e0-3279f9a906e7"
gold_warehouse_name = "bhg_gold"

def parse_json(value, default):
    if value is None:
        return default
    if isinstance(value, (list, dict)):
        return value
    text = str(value).strip()
    if not text:
        return default
    return json.loads(text)

def row_value(row, *names):
    for name in names:
        if isinstance(row, dict) and name in row:
            return row.get(name)
    return None

def exit_json(payload):
    result = json.dumps(payload, default=str)
    print(result)
    try:
        mssparkutils.notebook.exit(result)
    except NameError:
        pass

gold_tasks = parse_json(p_gold_tasks_json, [])

if isinstance(gold_tasks, dict):
    gold_tasks = gold_tasks.get("value") or gold_tasks.get("Value") or []

if not isinstance(gold_tasks, list):
    raise Exception(f"p_gold_tasks_json must be a JSON array. Received: {type(gold_tasks).__name__}")

active_gold_tasks = [
    task for task in gold_tasks
    if str(row_value(task, "IsActive", "isActive", "is_active", "isactive") or "0") in ("1", "True", "true")
]

if not active_gold_tasks:
    exit_json({
        "status": "SKIPPED",
        "message": "No active Gold taskconfig rows were passed.",
        "config_name_prefix": p_config_name_prefix,
        "pipeline_run_id": p_pipeline_run_id
    })

from com.microsoft.spark.fabric.Constants import Constants

results = []

for task in active_gold_tasks:
    task_config_id = row_value(task, "TaskConfigId", "task_config_id")
    method = row_value(task, "Method", "method")
    source_table = row_value(task, "SourceTable", "source_table")
    target_schema = row_value(task, "TargetSchema", "target_schema")
    target_table = row_value(task, "TargetTable", "target_table")

    if not source_table:
        raise Exception(f"Gold task {task_config_id} / {method} is missing SourceTable.")
    if not target_schema or not target_table:
        raise Exception(f"Gold task {task_config_id} / {method} is missing TargetSchema or TargetTable.")

    source_table = str(source_table).strip()
    target_schema = str(target_schema).strip()
    target_table = str(target_table).strip()
    gold_table = f"{gold_warehouse_name}.{target_schema}.{target_table}"

    print(f"Publishing Gold for {method}: {source_table} -> {gold_table}")

    source_df = spark.table(source_table)
    source_count = source_df.count()

    (
        source_df.write
        .option(Constants.WorkspaceId, gold_workspace_id)
        .option(Constants.DatawarehouseId, gold_warehouse_id)
        .mode("overwrite")
        .synapsesql(gold_table)
    )

    gold_df = (
        spark.read
        .option(Constants.WorkspaceId, gold_workspace_id)
        .option(Constants.DatawarehouseId, gold_warehouse_id)
        .synapsesql(gold_table)
    )
    gold_count = gold_df.count()

    if source_count != gold_count:
        raise Exception(
            f"Gold validation failed for {method}: source count {source_count}, gold count {gold_count}."
        )

    results.append({
        "task_config_id": task_config_id,
        "method": method,
        "source_table": source_table,
        "gold_table": gold_table,
        "rows_written": int(gold_count),
        "status": "SUCCESS"
    })

exit_json({
    "status": "SUCCESS",
    "config_name_prefix": p_config_name_prefix,
    "pipeline_run_id": p_pipeline_run_id,
    "ingest_run_id": p_ingest_run_id,
    "gold_task_count": len(active_gold_tasks),
    "results": results
})
```

## Parent Pipeline Parameters

The parent activity `nb_p1_reference_optional_gold_publish` should pass:

```text
p_gold_tasks_json   = @string(activity('flt_active_p1_reference_gold').output.value)
p_ingest_run_id     = current ingest run id expression
p_pipeline_run_id   = @pipeline().RunId
p_config_name_prefix = SAMMS P1 Reference
```

## What This Does

- Reads only active Gold task rows from the parent filter.
- Uses each row's `SourceTable` as the Silver source.
- Uses `TargetSchema` and `TargetTable` as the Gold Warehouse destination.
- Overwrites each active Gold table.
- Validates source row count equals Gold row count.
- Exits with a JSON result for pipeline debugging.
