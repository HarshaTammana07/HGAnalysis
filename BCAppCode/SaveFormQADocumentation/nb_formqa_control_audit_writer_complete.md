# nb_formqa_control_audit_writer

Paste these two cells into the dedicated `nb_formqa_control_audit_writer` notebook.

This version follows the new FormQuestionAnswers control model:

- Bronze uses one active `taskconfig` row per site for `ConfigId = 28`.
- Silver uses one active `taskconfig` row for `ConfigId = 29`.
- Gold uses one active `taskconfig` row for `ConfigId = 30`.
- `siteaudit` is no longer written.
- Per-site Bronze results are written directly into `taskaudit` using `SiteCode`, `DataBaseName`, and `SiteName`.

## Cell 1

```python
from datetime import datetime
import json
import random
import time
from pyspark.sql import functions as F
from pyspark.sql.types import *
from delta.tables import DeltaTable

try:
    p_mode
except NameError:
    p_mode = "START_LAYER_RUNS"

try:
    p_config_name_prefix
except NameError:
    p_config_name_prefix = "SAMMS FormQuestionAnswers"

try:
    p_pipeline_name
except NameError:
    p_pipeline_name = "Execute_Form_QuestionAnswer"

try:
    p_pipeline_path
except NameError:
    p_pipeline_path = "/pipelines/Execute_Form_QuestionAnswer"

try:
    p_triggered_by
except NameError:
    p_triggered_by = "Fabric"

try:
    p_error_message
except NameError:
    p_error_message = None

try:
    p_audit_context_json
except NameError:
    p_audit_context_json = "{}"

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = None

try:
    p_sites_json
except NameError:
    p_sites_json = "[]"

try:
    p_rows_read
except NameError:
    p_rows_read = 0

try:
    p_rows_written
except NameError:
    p_rows_written = 0

try:
    p_rows_failed
except NameError:
    p_rows_failed = 0

try:
    p_row_count
except NameError:
    p_row_count = 0

try:
    p_null_count
except NameError:
    p_null_count = 0

try:
    p_duplicate_count
except NameError:
    p_duplicate_count = 0

try:
    p_table_name
except NameError:
    p_table_name = None

try:
    p_target_name
except NameError:
    p_target_name = None

try:
    p_status
except NameError:
    p_status = "SUCCESS"

try:
    p_failed_target_name
except NameError:
    p_failed_target_name = None

try:
    p_failure_activity
except NameError:
    p_failure_activity = None

etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"
pipelinerun_table = "bhg_bronze.meta.pipelinerun"
taskqueue_table = "bhg_bronze.meta.taskqueue"
taskaudit_table = "bhg_bronze.meta.taskaudit"
dataquality_table = "bhg_bronze.meta.dataquality"

bronze_table = "bhg_bronze.Form.br_tblFormQA"
silver_table = "bhg_silver.pats.sl_tblFormQuestionAnswers"
gold_workspace_id = "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
gold_warehouse_id = "d29ef036-8c2c-40b0-a8e0-3279f9a906e7"
gold_warehouse_prefix = "bhg_gold.pats"

def new_bigint(offset=0):
    return int(datetime.utcnow().strftime("%Y%m%d%H%M%S%f")[:18]) + int(offset)

def parse_context():
    if not p_audit_context_json:
        return {}
    if isinstance(p_audit_context_json, dict):
        return p_audit_context_json
    return json.loads(p_audit_context_json)

def get_context_for_target(ctx, target_name):
    return ctx.get(target_name) or ctx.get(str(target_name).upper()) or {}

def layer_tasks(layer):
    if not layer:
        return []
    if "tasks" in layer:
        return layer["tasks"] or []
    if layer.get("task_id") is not None:
        return [layer]
    return []

def safe_int(value, default=0):
    if value is None:
        return default
    try:
        return int(value)
    except Exception:
        return default

def row_value(row, name, default=None):
    data = row.asDict(recursive=True)
    return data.get(name, default)

def table_schema(table_name):
    return spark.table(table_name).schema

def align_to_table(table_name, df):
    target_schema = table_schema(table_name)
    cols = []
    for field in target_schema:
        if field.name in df.columns:
            cols.append(F.col(field.name).cast(field.dataType).alias(field.name))
        else:
            cols.append(F.lit(None).cast(field.dataType).alias(field.name))
    return df.select(*cols)

def is_delta_concurrency_error(exc):
    msg = str(exc)
    retry_tokens = [
        "ConcurrentAppendException",
        "ConcurrentWriteException",
        "ConcurrentDeleteReadException",
        "DELTA_CONCURRENT_APPEND",
        "DELTA_CONCURRENT"
    ]
    return any(token in msg for token in retry_tokens)

def with_delta_retry(action, label, max_attempts=6, base_sleep_seconds=2):
    last_error = None
    for attempt in range(1, max_attempts + 1):
        try:
            return action()
        except Exception as exc:
            last_error = exc
            if attempt >= max_attempts or not is_delta_concurrency_error(exc):
                raise
            sleep_seconds = base_sleep_seconds * (2 ** (attempt - 1)) + random.uniform(0, 1.5)
            print(
                f"{label}: Delta concurrent write detected "
                f"(attempt {attempt}/{max_attempts}); retrying in {sleep_seconds:.1f}s"
            )
            time.sleep(sleep_seconds)
    raise last_error

def delta_upsert(table_name, df, keys):
    df = align_to_table(table_name, df)
    condition = " AND ".join([f"t.{k} = s.{k}" for k in keys])
    updates = {c: f"s.{c}" for c in df.columns if c not in keys}
    inserts = {c: f"s.{c}" for c in df.columns}
    def action():
        return (
            DeltaTable.forName(spark, table_name).alias("t")
            .merge(df.alias("s"), condition)
            .whenMatchedUpdate(set=updates)
            .whenNotMatchedInsert(values=inserts)
            .execute()
        )
    return with_delta_retry(action, f"MERGE {table_name}")

def insert_append(table_name, df):
    aligned_df = align_to_table(table_name, df)
    def action():
        return aligned_df.write.mode("append").format("delta").saveAsTable(table_name)
    return with_delta_retry(action, f"APPEND {table_name}")

def exit_json(payload):
    result = json.dumps(payload)
    print(result)
    try:
        mssparkutils.notebook.exit(result)
    except NameError:
        pass

def read_gold_df(table_name):
    from com.microsoft.spark.fabric.Constants import Constants
    return (
        spark.read
        .option(Constants.WorkspaceId, gold_workspace_id)
        .option(Constants.DatawarehouseId, gold_warehouse_id)
        .synapsesql(f"{gold_warehouse_prefix}.{table_name}")
    )

def dq_counts_for_df(df, key_cols):
    row_count = df.count()
    null_cond = None
    for c in key_cols:
        cond = F.col(c).isNull()
        null_cond = cond if null_cond is None else (null_cond | cond)
    null_count = df.where(null_cond).count() if null_cond is not None else 0
    duplicate_count = (
        df.groupBy(*key_cols)
        .count()
        .where(F.col("count") > 1)
        .count()
    )
    return int(row_count), int(null_count), int(duplicate_count)

def bronze_counts_for_run(ingest_run_id):
    return (
        spark.table(bronze_table)
        .where(F.col("_ingest_run_id") == F.lit(ingest_run_id))
        .groupBy(
            F.col("_site_code").alias("SiteCode"),
            F.col("_source_database").alias("DataBaseName")
        )
        .agg(F.count(F.lit(1)).cast("long").alias("RowsCopied"))
    )

def bronze_task_counts_df(br_layer, ingest_run_id):
    task_rows = [
        (
            safe_int(t.get("task_id")),
            safe_int(t.get("run_id") or br_layer.get("run_id")),
            safe_int(t.get("config_id") or br_layer.get("config_id")),
            safe_int(t.get("task_config_id")),
            t.get("task_name"),
            t.get("target_table") or br_layer.get("target_table"),
            t.get("site_code"),
            t.get("data_base_name"),
            t.get("site_name")
        )
        for t in layer_tasks(br_layer)
    ]

    task_schema = StructType([
        StructField("TaskId", LongType()),
        StructField("RunId", LongType()),
        StructField("ConfigId", LongType()),
        StructField("TaskConfigId", LongType()),
        StructField("TaskName", StringType()),
        StructField("TargetTable", StringType()),
        StructField("SiteCode", StringType()),
        StructField("DataBaseName", StringType()),
        StructField("SiteName", StringType())
    ])
    tasks_df = spark.createDataFrame(task_rows, task_schema)

    counts_df = bronze_counts_for_run(ingest_run_id)
    return (
        tasks_df.alias("t")
        .join(counts_df.alias("c"), ["SiteCode", "DataBaseName"], "left")
        .select(
            "TaskId", "RunId", "ConfigId", "TaskConfigId", "TaskName",
            "TargetTable", "SiteCode", "DataBaseName", "SiteName",
            F.coalesce(F.col("RowsCopied"), F.lit(0)).cast("long").alias("RowsCopied")
        )
    )

def taskaudit_schema():
    return StructType([
        StructField("AuditId", LongType()),
        StructField("TaskId", LongType()),
        StructField("RunId", LongType()),
        StructField("ConfigId", LongType()),
        StructField("TaskConfigId", LongType()),
        StructField("TaskName", StringType()),
        StructField("TableName", StringType()),
        StructField("StepName", StringType()),
        StructField("Status", StringType()),
        StructField("LogLevel", StringType()),
        StructField("RowsRead", LongType()),
        StructField("RowsWritten", LongType()),
        StructField("RowsFailed", LongType()),
        StructField("ErrorMessage", StringType()),
        StructField("SiteCode", StringType()),
        StructField("DataBaseName", StringType()),
        StructField("SiteName", StringType()),
        StructField("StartTime", TimestampType()),
        StructField("EndTime", TimestampType()),
        StructField("DurationSeconds", LongType())
    ])

def append_taskaudit_rows(rows):
    if not rows:
        return
    audit_df = (
        spark.createDataFrame(rows, taskaudit_schema())
        .withColumn("EndTime", F.current_timestamp())
        .withColumn("CreatedAt", F.current_timestamp())
    )
    audit_df = audit_df.withColumn(
        "DurationSeconds",
        F.unix_timestamp("EndTime") - F.unix_timestamp("StartTime")
    )
    insert_append(taskaudit_table, audit_df)

def update_pipelinerun(run_id, status, success_tasks, failed_tasks, skipped_tasks):
    pr_df = spark.sql(f"SELECT * FROM {pipelinerun_table} WHERE RunId = {int(run_id)}").limit(1)
    if pr_df.count() == 0:
        raise Exception(f"No pipelinerun row found for RunId={run_id}")

    pr_update_df = (
        pr_df
        .withColumn("Status", F.lit(status))
        .withColumn("EndTime", F.current_timestamp())
        .withColumn("SuccessTasks", F.lit(int(success_tasks)))
        .withColumn("FailedTasks", F.lit(int(failed_tasks)))
        .withColumn("SkippedTasks", F.lit(int(skipped_tasks)))
    )
    delta_upsert(pipelinerun_table, pr_update_df, ["RunId"])

def update_taskqueue_for_tasks(task_ids, status, error_message=None):
    if not task_ids:
        return
    ids = ",".join([str(int(x)) for x in task_ids])
    tq_df = spark.sql(f"SELECT * FROM {taskqueue_table} WHERE TaskId IN ({ids})")
    if tq_df.count() != len(task_ids):
        raise Exception(f"Expected {len(task_ids)} taskqueue rows, found {tq_df.count()} for TaskIds {ids}")

    tq_update_df = (
        tq_df
        .withColumn("Status", F.lit(status))
        .withColumn("EndTime", F.current_timestamp())
        .withColumn("ErrorMessage", F.lit(error_message).cast("string"))
    )
    delta_upsert(taskqueue_table, tq_update_df, ["TaskId"])

def finish_single_task(layer, status, log_level, rows_read, rows_written, rows_failed, error_message, audit_offset):
    tasks = layer_tasks(layer)
    if len(tasks) != 1:
        raise Exception(f"Expected one task for target {layer.get('target_name')}, found {len(tasks)}")

    task = tasks[0]
    task_id = safe_int(task["task_id"])
    run_id = safe_int(task.get("run_id") or layer.get("run_id"))

    tq_df = spark.sql(f"SELECT * FROM {taskqueue_table} WHERE TaskId = {task_id}").limit(1)
    if tq_df.count() == 0:
        raise Exception(f"No taskqueue row found for TaskId={task_id}")
    start_time = tq_df.collect()[0]["StartTime"]

    update_taskqueue_for_tasks([task_id], status, error_message)

    append_taskaudit_rows([(
        new_bigint(audit_offset),
        task_id,
        run_id,
        safe_int(task.get("config_id") or layer.get("config_id")),
        safe_int(task.get("task_config_id")),
        task.get("task_name"),
        task.get("target_table") or layer.get("target_table"),
        "TASK_COMPLETE",
        status,
        log_level,
        int(rows_read),
        int(rows_written),
        int(rows_failed),
        error_message,
        task.get("site_code"),
        task.get("data_base_name"),
        task.get("site_name"),
        start_time,
        None,
        None
    )])

    update_pipelinerun(
        run_id,
        status,
        1 if status == "SUCCESS" else 0,
        1 if status == "FAILED" else 0,
        1 if status == "SKIPPED" else 0
    )

def finish_bronze_tasks(br_layer, ingest_run_id, status, log_level, error_message=None, audit_offset_base=100):
    br_task_count_df = bronze_task_counts_df(br_layer, ingest_run_id).cache()
    task_ids = [safe_int(r["TaskId"]) for r in br_task_count_df.select("TaskId").collect()]
    if not task_ids:
        raise Exception("No BR tasks found in audit context")

    update_taskqueue_for_tasks(task_ids, status, error_message)

    tq_df = spark.sql(
        f"SELECT TaskId, StartTime FROM {taskqueue_table} WHERE TaskId IN ({','.join([str(x) for x in task_ids])})"
    )

    rows = []
    for idx, r in enumerate(br_task_count_df.join(tq_df, ["TaskId"], "left").collect(), start=1):
        rows_copied = safe_int(r["RowsCopied"])
        rows_failed = 1 if status == "FAILED" else 0
        rows.append((
            new_bigint(audit_offset_base + idx),
            safe_int(r["TaskId"]),
            safe_int(r["RunId"]),
            safe_int(r["ConfigId"]),
            safe_int(r["TaskConfigId"]),
            r["TaskName"],
            r["TargetTable"],
            "TASK_COMPLETE",
            status,
            log_level,
            rows_copied,
            rows_copied if status != "SKIPPED" else 0,
            rows_failed,
            error_message,
            r["SiteCode"],
            r["DataBaseName"],
            r["SiteName"],
            r["StartTime"],
            None,
            None
        ))

    append_taskaudit_rows(rows)

    update_pipelinerun(
        safe_int(br_layer["run_id"]),
        status,
        len(task_ids) if status == "SUCCESS" else 0,
        len(task_ids) if status == "FAILED" else 0,
        len(task_ids) if status == "SKIPPED" else 0
    )

    return br_task_count_df
```

## Cell 2

```python
ctx = parse_context()
mode = p_mode.upper().strip()

if mode == "START_LAYER_RUNS":
    configs_df = spark.sql(f"""
        SELECT *
        FROM {etlconfig_table}
        WHERE IsActive = 1
          AND ConfigName LIKE '{p_config_name_prefix}%'
          AND TargetName IN ('BR', 'SL', 'GL')
    """)
    configs = configs_df.collect()
    if len(configs) != 3:
        raise Exception(f"Expected 3 active etlconfig rows for prefix {p_config_name_prefix}, found {len(configs)}")

    config_by_id = {safe_int(r["ConfigId"]): r for r in configs}
    config_by_target = {str(r["TargetName"]).upper(): r for r in configs}
    for required_target in ["BR", "SL", "GL"]:
        if required_target not in config_by_target:
            raise Exception(f"Missing active {required_target} etlconfig row for prefix {p_config_name_prefix}")

    config_ids = ",".join([str(r["ConfigId"]) for r in configs])
    tasks_df = spark.sql(f"""
        SELECT *
        FROM {taskconfig_table}
        WHERE IsActive = 1
          AND ConfigId IN ({config_ids})
    """)
    tasks = tasks_df.collect()
    if not tasks:
        raise Exception(f"No active taskconfig rows found for ConfigIds {config_ids}")

    tasks_by_target = {"BR": [], "SL": [], "GL": []}
    for task in tasks:
        cfg = config_by_id.get(safe_int(task["ConfigId"]))
        if not cfg:
            continue
        target = str(cfg["TargetName"]).upper()
        if target == "BR":
            if row_value(task, "SiteCode") and row_value(task, "DataBaseName"):
                tasks_by_target[target].append(task)
        else:
            tasks_by_target[target].append(task)

    if len(tasks_by_target["BR"]) == 0:
        raise Exception("Expected at least one active BR taskconfig row with SiteCode and DataBaseName")
    if len(tasks_by_target["SL"]) != 1:
        raise Exception(f"Expected 1 active SL taskconfig row, found {len(tasks_by_target['SL'])}")
    if len(tasks_by_target["GL"]) != 1:
        raise Exception(f"Expected 1 active GL taskconfig row, found {len(tasks_by_target['GL'])}")

    tasks_by_target["BR"] = sorted(tasks_by_target["BR"], key=lambda r: str(row_value(r, "SiteCode") or ""))

    run_rows = []
    task_rows = []
    audit_context = {}
    base_id = new_bigint(0)
    targets_in_order = [
        str(r["TargetName"]).upper()
        for r in sorted(configs, key=lambda row: safe_int(row_value(row, "ExecutionSequence"), 0))
    ]

    for target_idx, target_name in enumerate(targets_in_order, start=1):
        cfg = config_by_target[target_name]
        config_id = safe_int(cfg["ConfigId"])
        layer_tasks_cfg = tasks_by_target[target_name]
        run_id = base_id + (target_idx * 100000)
        layer_task_context = []

        for task_idx, task in enumerate(layer_tasks_cfg, start=1):
            task_id = run_id + task_idx
            site_code = row_value(task, "SiteCode")
            data_base_name = row_value(task, "DataBaseName")
            site_name = row_value(task, "SiteName")
            task_ctx = {
                "run_id": run_id,
                "config_id": config_id,
                "config_name": cfg["ConfigName"],
                "target_name": target_name,
                "task_config_id": safe_int(task["TaskConfigId"]),
                "task_id": task_id,
                "task_name": task["TaskName"],
                "target_table": task["TargetTable"],
                "site_code": site_code,
                "data_base_name": data_base_name,
                "site_name": site_name
            }
            layer_task_context.append(task_ctx)

            task_rows.append((
                task_id,
                run_id,
                config_id,
                safe_int(task["TaskConfigId"]),
                task["TaskName"],
                cfg["SourceType"],
                task["TargetTable"],
                safe_int(row_value(task, "ExecutionOrder"), 0),
                1,
                "RUNNING",
                0,
                0,
                1,
                0,
                0,
                site_code,
                data_base_name,
                site_name,
                None,
                None,
                None
            ))

        audit_context[target_name] = {
            "run_id": run_id,
            "config_id": config_id,
            "config_name": cfg["ConfigName"],
            "target_name": target_name,
            "target_table": layer_task_context[0]["target_table"],
            "tasks": layer_task_context
        }
        if len(layer_task_context) == 1:
            audit_context[target_name].update(layer_task_context[0])

        run_rows.append((
            run_id,
            config_id,
            cfg["ConfigName"],
            cfg["PipelineName"],
            cfg["PipelinePath"],
            cfg["SourceSystem"],
            cfg["TargetName"],
            cfg["EnvironmentName"],
            None,
            None,
            "RUNNING",
            cfg["TriggerType"],
            cfg["TriggeredBy"] or p_triggered_by,
            len(layer_task_context),
            0,
            0,
            0,
            0,
            1
        ))

    run_schema = StructType([
        StructField("RunId", LongType()), StructField("ConfigId", LongType()),
        StructField("ConfigName", StringType()), StructField("PipelineName", StringType()),
        StructField("PipelinePath", StringType()), StructField("SourceSystem", StringType()),
        StructField("TargetName", StringType()), StructField("EnvironmentName", StringType()),
        StructField("StartTime", TimestampType()), StructField("EndTime", TimestampType()),
        StructField("Status", StringType()), StructField("TriggerType", StringType()),
        StructField("TriggeredBy", StringType()), StructField("TotalTasks", IntegerType()),
        StructField("SuccessTasks", IntegerType()), StructField("FailedTasks", IntegerType()),
        StructField("SkippedTasks", IntegerType()), StructField("RestartFlag", IntegerType()),
        StructField("AttemptNumber", IntegerType())
    ])
    task_schema = StructType([
        StructField("TaskId", LongType()), StructField("RunId", LongType()),
        StructField("ConfigId", LongType()), StructField("TaskConfigId", LongType()),
        StructField("TaskName", StringType()), StructField("SourceType", StringType()),
        StructField("TargetTable", StringType()), StructField("ExecutionOrder", IntegerType()),
        StructField("ExecutionFlag", IntegerType()), StructField("Status", StringType()),
        StructField("RetryAttempt", IntegerType()), StructField("RestartFlag", IntegerType()),
        StructField("AttemptNumber", IntegerType()), StructField("HttpStatusCode", IntegerType()),
        StructField("ApiResponseTimeMs", LongType()), StructField("SiteCode", StringType()),
        StructField("DataBaseName", StringType()), StructField("SiteName", StringType()),
        StructField("StartTime", TimestampType()), StructField("EndTime", TimestampType()),
        StructField("ErrorMessage", StringType())
    ])

    run_df = (
        spark.createDataFrame(run_rows, run_schema)
        .withColumn("StartTime", F.current_timestamp())
        .withColumn("CreatedAt", F.current_timestamp())
    )
    task_df = (
        spark.createDataFrame(task_rows, task_schema)
        .withColumn("StartTime", F.current_timestamp())
        .withColumn("CreatedAt", F.current_timestamp())
    )
    insert_append(pipelinerun_table, run_df)
    insert_append(taskqueue_table, task_df)
    exit_json(audit_context)

elif mode == "FINALIZE_FORMQA_SUCCESS":
    br = get_context_for_target(ctx, "BR")
    sl = get_context_for_target(ctx, "SL")
    gl = get_context_for_target(ctx, "GL")

    if not br or not sl or not gl:
        raise Exception("Missing BR/SL/GL context for FINALIZE_FORMQA_SUCCESS")
    if not p_ingest_run_id:
        raise Exception("p_ingest_run_id is required for FINALIZE_FORMQA_SUCCESS")

    bronze_run_df = spark.table(bronze_table).where(F.col("_ingest_run_id") == F.lit(p_ingest_run_id))
    bronze_run_count = bronze_run_df.count()

    br_task_count_df = finish_bronze_tasks(
        br,
        p_ingest_run_id,
        status="SUCCESS",
        log_level="INFO",
        error_message=None,
        audit_offset_base=100
    )

    br_rows_read = int(br_task_count_df.agg(F.coalesce(F.sum("RowsCopied"), F.lit(0))).collect()[0][0])
    br_rows_written = br_rows_read

    silver_df = spark.table(silver_table)
    gold_df = read_gold_df(gl["target_table"])

    sl_row_count, sl_null_count, sl_duplicate_count = dq_counts_for_df(
        silver_df,
        ["SiteCode", "FormName", "FormId", "ClientId", "PreAdmissionId", "QuestionId", "QuestionOrderId"]
    )
    gl_row_count, gl_null_count, gl_duplicate_count = dq_counts_for_df(
        gold_df,
        ["SiteCode", "FormName", "FormId", "ClientId", "PreAdmissionId", "QuestionId", "QuestionOrderId"]
    )

    dq_schema = StructType([
        StructField("DqId", LongType()), StructField("RunId", LongType()),
        StructField("ConfigId", LongType()), StructField("TaskConfigId", LongType()),
        StructField("TableName", StringType()), StructField("RowCount", LongType()),
        StructField("NullCount", LongType()), StructField("DuplicateCount", LongType()),
        StructField("ValidationStatus", StringType())
    ])
    dq_df = spark.createDataFrame([
        (
            new_bigint(7), safe_int(sl["run_id"]), safe_int(sl["config_id"]),
            safe_int(sl["task_config_id"]), sl["target_table"],
            sl_row_count, sl_null_count, sl_duplicate_count,
            "SUCCESS" if sl_duplicate_count == 0 else "FAILED"
        ),
        (
            new_bigint(8), safe_int(gl["run_id"]), safe_int(gl["config_id"]),
            safe_int(gl["task_config_id"]), gl["target_table"],
            gl_row_count, gl_null_count, gl_duplicate_count,
            "SUCCESS" if gl_duplicate_count == 0 else "FAILED"
        )
    ], dq_schema).withColumn("CreatedAt", F.current_timestamp())
    insert_append(dataquality_table, dq_df)

    finish_single_task(sl, "SUCCESS", "INFO", bronze_run_count, sl_row_count, 0, None, 300)
    finish_single_task(gl, "SUCCESS", "INFO", sl_row_count, gl_row_count, 0, None, 301)

    exit_json({
        "status": "OK",
        "mode": mode,
        "bronze_tasks": len(layer_tasks(br)),
        "bronze_rows": bronze_run_count,
        "bronze_rows_read": br_rows_read,
        "bronze_rows_written": br_rows_written,
        "silver_rows": sl_row_count,
        "gold_rows": gl_row_count,
        "silver_duplicates": sl_duplicate_count,
        "gold_duplicates": gl_duplicate_count
    })

elif mode == "FINALIZE_FORMQA_FAILURE":
    br = get_context_for_target(ctx, "BR")
    sl = get_context_for_target(ctx, "SL")
    gl = get_context_for_target(ctx, "GL")

    if not br or not sl or not gl:
        raise Exception("Missing BR/SL/GL context for FINALIZE_FORMQA_FAILURE")
    if not p_ingest_run_id:
        raise Exception("p_ingest_run_id is required for FINALIZE_FORMQA_FAILURE")

    failed_target = (p_failed_target_name or "BR").upper()
    fail_all_layers = failed_target in ("ALL", "UNKNOWN", "AUTO")
    if failed_target not in ("BR", "SL", "GL", "ALL", "UNKNOWN", "AUTO"):
        failed_target = "BR"

    failure_message = (
        p_error_message
        or "FormQuestionAnswers pipeline failed. Check the failed parent/child activity run details."
    )

    bronze_run_df = spark.table(bronze_table).where(F.col("_ingest_run_id") == F.lit(p_ingest_run_id))
    bronze_run_count = bronze_run_df.count()

    target_order = ["BR", "SL", "GL"]
    failed_idx = 0 if fail_all_layers else target_order.index(failed_target)
    layers = {"BR": br, "SL": sl, "GL": gl}

    for idx, target_name in enumerate(target_order):
        layer = layers[target_name]
        if fail_all_layers:
            final_status = "FAILED"
            log_level = "ERROR"
            err = failure_message
        elif idx < failed_idx:
            final_status = "SUCCESS"
            log_level = "INFO"
            err = None
        elif idx == failed_idx:
            final_status = "FAILED"
            log_level = "ERROR"
            err = failure_message
        else:
            final_status = "SKIPPED"
            log_level = "WARN"
            err = f"Skipped because {failed_target} failed"

        if target_name == "BR":
            finish_bronze_tasks(
                br,
                p_ingest_run_id,
                status=final_status,
                log_level=log_level,
                error_message=err,
                audit_offset_base=400
            )
        elif target_name == "SL":
            if final_status == "SUCCESS":
                rows_read = bronze_run_count
                rows_written = spark.table(silver_table).count()
                rows_failed = 0
            elif final_status == "FAILED":
                rows_read = bronze_run_count
                rows_written = 0
                rows_failed = 1
            else:
                rows_read = rows_written = rows_failed = 0
            finish_single_task(layer, final_status, log_level, rows_read, rows_written, rows_failed, err, 500 + idx)
        elif target_name == "GL":
            if final_status == "SUCCESS":
                rows_read = spark.table(silver_table).count()
                rows_written = read_gold_df(layer["target_table"]).count()
                rows_failed = 0
            elif final_status == "FAILED":
                rows_read = spark.table(silver_table).count()
                rows_written = 0
                rows_failed = 1
            else:
                rows_read = rows_written = rows_failed = 0
            finish_single_task(layer, final_status, log_level, rows_read, rows_written, rows_failed, err, 500 + idx)

    payload = {
        "status": "FAILED",
        "mode": mode,
        "failed_target": failed_target,
        "failure_activity": p_failure_activity,
        "bronze_rows": bronze_run_count,
        "message": "Failure audit finalized; raising exception to keep pipeline failed."
    }
    print(json.dumps(payload))
    raise Exception(json.dumps(payload))

elif mode == "WRITE_DATAQUALITY":
    if not p_target_name:
        raise Exception("p_target_name is required for WRITE_DATAQUALITY")
    layer = get_context_for_target(ctx, p_target_name)
    if not layer:
        raise Exception(f"Missing {p_target_name} context for WRITE_DATAQUALITY")

    target = p_target_name.upper()
    table_name = p_table_name or layer["target_table"]
    if int(p_row_count or 0) > 0:
        row_count, null_count, duplicate_count = int(p_row_count), int(p_null_count or 0), int(p_duplicate_count or 0)
    elif target == "SL":
        row_count, null_count, duplicate_count = dq_counts_for_df(
            spark.table(silver_table),
            ["SiteCode", "FormName", "FormId", "ClientId", "PreAdmissionId", "QuestionId", "QuestionOrderId"]
        )
    elif target == "GL":
        row_count, null_count, duplicate_count = dq_counts_for_df(
            read_gold_df(table_name),
            ["SiteCode", "FormName", "FormId", "ClientId", "PreAdmissionId", "QuestionId", "QuestionOrderId"]
        )
    else:
        row_count, null_count, duplicate_count = 0, 0, 0

    validation_status = "SUCCESS" if str(p_status).upper() == "SUCCESS" and duplicate_count == 0 else "FAILED"
    dq_schema = StructType([
        StructField("DqId", LongType()), StructField("RunId", LongType()),
        StructField("ConfigId", LongType()), StructField("TaskConfigId", LongType()),
        StructField("TableName", StringType()), StructField("RowCount", LongType()),
        StructField("NullCount", LongType()), StructField("DuplicateCount", LongType()),
        StructField("ValidationStatus", StringType())
    ])
    dq_df = spark.createDataFrame([(
        new_bigint(7), safe_int(layer["run_id"]), safe_int(layer["config_id"]),
        safe_int(layer["task_config_id"]), table_name,
        row_count, null_count, duplicate_count, validation_status
    )], dq_schema).withColumn("CreatedAt", F.current_timestamp())
    insert_append(dataquality_table, dq_df)
    exit_json({"status": "OK", "mode": mode, "target_name": target, "row_count": row_count, "duplicate_count": duplicate_count})

elif mode in ("FINISH_TASK_SUCCESS", "FINISH_TASK_FAILURE"):
    if not p_target_name:
        raise Exception("p_target_name is required for FINISH_TASK modes")
    target = p_target_name.upper()
    layer = get_context_for_target(ctx, target)
    if not layer:
        raise Exception(f"Missing {target} context for {mode}")

    final_status = "SUCCESS" if mode == "FINISH_TASK_SUCCESS" else "FAILED"
    log_level = "INFO" if final_status == "SUCCESS" else "ERROR"
    err = None if final_status == "SUCCESS" else p_error_message

    if target == "BR":
        br_task_count_df = finish_bronze_tasks(
            layer,
            p_ingest_run_id,
            status=final_status,
            log_level=log_level,
            error_message=err,
            audit_offset_base=700
        )
        rows_written = int(br_task_count_df.agg(F.coalesce(F.sum("RowsCopied"), F.lit(0))).collect()[0][0])
        exit_json({"status": "OK", "mode": mode, "target_name": target, "rows_written": rows_written})
    elif target == "SL":
        rows_read = spark.table(bronze_table).where(F.col("_ingest_run_id") == F.lit(p_ingest_run_id)).count() if p_ingest_run_id else int(p_rows_read or 0)
        rows_written = spark.table(silver_table).count() if final_status == "SUCCESS" else int(p_rows_written or 0)
        rows_failed = int(p_rows_failed or (1 if final_status == "FAILED" else 0))
        finish_single_task(layer, final_status, log_level, rows_read, rows_written, rows_failed, err, 701)
        exit_json({"status": "OK", "mode": mode, "target_name": target, "rows_read": int(rows_read), "rows_written": int(rows_written)})
    elif target == "GL":
        rows_read = spark.table(silver_table).count()
        rows_written = read_gold_df(layer["target_table"]).count() if final_status == "SUCCESS" else int(p_rows_written or 0)
        rows_failed = int(p_rows_failed or (1 if final_status == "FAILED" else 0))
        finish_single_task(layer, final_status, log_level, rows_read, rows_written, rows_failed, err, 702)
        exit_json({"status": "OK", "mode": mode, "target_name": target, "rows_read": int(rows_read), "rows_written": int(rows_written)})
    else:
        raise Exception(f"Unsupported target for FINISH_TASK mode: {target}")

else:
    raise Exception(f"Unsupported p_mode: {p_mode}")
```
