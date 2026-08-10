# P1 Finance Silver Notebooks
Generated from `columnsanddatatypesFinance.txt4` and Finance taskconfig metadata.
Each notebook should use Cell 1 from `nb_p1_finance_sl_common_cell1.py`, followed by its method-specific Cell 2.
Recommended notebook names are listed with the pipeline activity name.

## Cell 1 - Common Finance Silver Runtime
```python
from datetime import datetime
import json
import traceback

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.window import Window

try:
    from notebookutils.mssparkutils.handlers.notebookHandler import NotebookExit
except Exception:
    NotebookExit = None

spark.conf.set("spark.sql.parquet.datetimeRebaseModeInRead", "LEGACY")
spark.conf.set("spark.sql.parquet.datetimeRebaseModeInWrite", "LEGACY")
spark.conf.set("spark.sql.parquet.int96RebaseModeInRead", "LEGACY")
spark.conf.set("spark.sql.parquet.int96RebaseModeInWrite", "LEGACY")

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = ""

try:
    p_bronze_method_results_json
except NameError:
    p_bronze_method_results_json = "{}"

try:
    p_sites_json
except NameError:
    p_sites_json = "[]"

try:
    p_method_name
except NameError:
    p_method_name = ""

METADATA_COLUMNS = {
    "sourcedatabase",
    "ingestrunid",
    "extractedat",
    "sourcequerystartdate",
    "sourcequeryenddate",
    "lookbackdate",
    "_ingest_run_id",
    "_source_database",
    "_site_code",
}

TASKCONFIG_TABLE = "bhg_bronze.meta.taskconfig"
ETLCONFIG_TABLE = "bhg_bronze.meta.etlconfig"
P1_FINANCE_CONFIG_NAME_PREFIX = "SAMMS P1 Finance"
P1_FINANCE_SILVER_TARGET_NAME = "SL"
CLAIMS_EF_SITES = {"VBRA", "VMIN", "VWBY", "VBRP"}


def row_value(row, name, default=None):
    row_dict = row.asDict(recursive=True) if hasattr(row, "asDict") else dict(row)
    wanted = name.lower()
    for key, value in row_dict.items():
        if str(key).lower() == wanted:
            return value
    return default


def parse_request_body(raw):
    if not raw:
        return {}
    try:
        parsed = json.loads(raw) if isinstance(raw, str) else raw
        return parsed if isinstance(parsed, dict) else {}
    except Exception:
        return {}


def dq_keys_from_taskconfig(task_row):
    request_body = parse_request_body(row_value(task_row, "RequestBody"))
    request_keys = request_body.get("dq_keys")
    if isinstance(request_keys, list):
        keys = [str(key).strip() for key in request_keys if str(key).strip()]
        if keys:
            return keys

    watermark_column = row_value(task_row, "WatermarkColumn")
    if watermark_column:
        keys = [key.strip() for key in str(watermark_column).split(",") if key.strip()]
        if keys:
            return keys

    raise ValueError(f"No dq_keys/WatermarkColumn found in taskconfig for Method={row_value(task_row, 'Method')}")


def resolve_finance_config_id(target_name):
    target = str(target_name or "").strip().upper()
    rows = (
        spark.table(ETLCONFIG_TABLE)
        .where(F.lower(F.col("ConfigName")).startswith(P1_FINANCE_CONFIG_NAME_PREFIX.lower()))
        .where(F.upper(F.col("TargetName")) == F.lit(target))
        .where(F.col("IsActive") == F.lit(1))
        .select("ConfigId", "ConfigName", "TargetName")
        .orderBy(F.col("ExecutionSequence").asc(), F.col("ConfigId").asc())
        .limit(2)
        .collect()
    )
    if not rows:
        raise ValueError(
            f"No active etlconfig row found for prefix={P1_FINANCE_CONFIG_NAME_PREFIX}, TargetName={target}."
        )
    if len(rows) > 1:
        raise ValueError(
            f"Expected one active etlconfig row for prefix={P1_FINANCE_CONFIG_NAME_PREFIX}, TargetName={target}; found {len(rows)}."
        )
    return int(row_value(rows[0], "ConfigId"))


def resolve_finance_silver_metadata(method_name=None, silver_config_id=None):
    method = str(method_name or p_method_name or "").strip()
    if not method:
        raise ValueError("p_method_name or method_name is required to resolve P1 Finance silver taskconfig metadata.")

    silver_config_id = int(silver_config_id or resolve_finance_config_id(P1_FINANCE_SILVER_TARGET_NAME))

    rows = (
        spark.table(TASKCONFIG_TABLE)
        .where(F.col("ConfigId") == F.lit(int(silver_config_id)))
        .where(F.lower(F.col("Method")) == F.lit(method.lower()))
        .where(F.col("IsActive") == F.lit(1))
        .orderBy(F.col("ExecutionOrder").asc(), F.col("TaskConfigId").asc())
        .limit(2)
        .collect()
    )

    if not rows:
        raise ValueError(f"No active P1 Finance silver taskconfig row found for ConfigId={silver_config_id}, Method={method}.")
    if len(rows) > 1:
        raise ValueError(f"Expected one active P1 Finance silver taskconfig row for Method={method}, found {len(rows)}.")

    task_row = rows[0]
    request_body = parse_request_body(row_value(task_row, "RequestBody"))
    bronze_table = row_value(task_row, "SourceTable") or request_body.get("source_table")
    silver_table = request_body.get("full_table") or row_value(task_row, "TargetPath")
    match_keys = dq_keys_from_taskconfig(task_row)
    method_from_config = row_value(task_row, "Method") or method

    if not bronze_table:
        raise ValueError(f"Silver taskconfig SourceTable/RequestBody.source_table is missing for Method={method_from_config}.")
    if not silver_table:
        raise ValueError(f"Silver taskconfig TargetPath/RequestBody.full_table is missing for Method={method_from_config}.")

    return {
        "method_name": str(method_from_config),
        "bronze_table": str(bronze_table),
        "silver_table": str(silver_table),
        "match_keys": match_keys,
        "task_config_id": row_value(task_row, "TaskConfigId"),
        "config_id": row_value(task_row, "ConfigId"),
        "request_body": request_body,
    }


def notebook_exit(payload):
    text = json.dumps(payload, default=str, separators=(",", ":"))
    try:
        mssparkutils.notebook.exit(text)
    except NameError:
        print(text)


def is_notebook_exit(ex):
    return (NotebookExit is not None and isinstance(ex, NotebookExit)) or ex.__class__.__name__ == "NotebookExit"


def result_payload(method_name, status, layer="SL", rows_read=0, rows_inserted=0, rows_updated=0, rows_skipped=0, message=None, site_results=None):
    body = {
        "method": method_name,
        "layer": layer,
        "status": status,
        "rows_read": int(rows_read or 0),
        "rows_inserted": int(rows_inserted or 0),
        "rows_updated": int(rows_updated or 0),
        "rows_skipped": int(rows_skipped or 0),
    }
    if message:
        body["message"] = str(message)[:4000]
    if site_results is not None:
        body["site_results"] = site_results
    return {method_name: body}


def parse_bronze_results():
    if not p_bronze_method_results_json:
        return {}
    try:
        parsed = json.loads(p_bronze_method_results_json)
        return parsed if isinstance(parsed, dict) else {}
    except Exception:
        return {}


def bronze_failed_for(method_name):
    results = parse_bronze_results()
    for global_key in ("P1Finance", "Finance"):
        global_result = results.get(global_key)
        if isinstance(global_result, dict):
            global_status = str(global_result.get("status", "")).upper()
            if global_status in {"FAILED", "ERROR"}:
                return True

    method_result = results.get(method_name)
    if not isinstance(method_result, dict):
        return False
    status = str(method_result.get("status", "")).upper()
    return status in {"FAILED", "ERROR"}


def parse_json_list(raw):
    if not raw:
        return []
    try:
        parsed = json.loads(str(raw))
        return parsed if isinstance(parsed, list) else []
    except Exception:
        return []


def active_site_rows_for_method(method_name):
    rows = []
    seen = set()
    for row in parse_json_list(p_sites_json):
        if not isinstance(row, dict):
            continue
        if str(row.get("Method", "")).lower() != method_name.lower():
            continue
        site_code = row.get("SiteCode")
        database_name = row.get("DataBaseName")
        key = (str(site_code or ""), str(database_name or ""))
        if site_code and key not in seen:
            rows.append({
                "site_code": str(site_code),
                "database_name": str(database_name) if database_name else None,
            })
            seen.add(key)
    return rows


def build_site_results(method_name, successful_sites, bronze_had_method_failure=False, failed_message=None):
    successful = {str(site) for site in successful_sites if site}
    results = []
    for site in active_site_rows_for_method(method_name):
        site_code = site["site_code"]
        item = {
            "site_code": site_code,
            "database_name": site.get("database_name"),
        }
        if site_code in successful:
            item["status"] = "SUCCESS"
        elif bronze_had_method_failure:
            item.update({
                "status": "FAILED",
                "failed_stage": "BR",
                "error_message": failed_message or f"{method_name} Bronze copy failed or did not write rows for this site."
            })
        else:
            item["status"] = "SUCCESS"
        results.append(item)
    return results


def table_exists(table_name):
    try:
        spark.table(table_name).limit(0).count()
        return True
    except Exception:
        return False


def actual_col(df, wanted, required=False):
    by_lower = {c.lower(): c for c in df.columns}
    found = by_lower.get(wanted.lower())
    if required and not found:
        raise ValueError(f"Column {wanted} was not found. Available columns: {df.columns}")
    return found


def col_or_null(df, wanted):
    found = actual_col(df, wanted, required=False)
    return F.col(found) if found else F.lit(None)


def successful_sites_from_bronze(bronze_df):
    site_col = actual_col(bronze_df, "SiteCode", required=False) or actual_col(bronze_df, "_site_code", required=False)
    if not site_col:
        return [], None
    sites = [
        str(row[site_col])
        for row in (
            bronze_df
            .where(F.col(site_col).isNotNull())
            .select(site_col)
            .distinct()
            .collect()
        )
    ]
    return sites, site_col


def load_bronze(bronze_table):
    if not table_exists(bronze_table):
        raise ValueError(f"Bronze table does not exist: {bronze_table}")
    df = spark.table(bronze_table)
    ingest_col = actual_col(df, "IngestRunId", required=False)
    if p_ingest_run_id and ingest_col:
        df = df.where(F.col(ingest_col) == F.lit(p_ingest_run_id))
    return df


def target_columns_for(src_df, silver_table, configured_target_columns=None, transforms=None, drop_columns=None):
    transforms = transforms or {}
    drop = {c.lower() for c in (drop_columns or [])}
    if configured_target_columns:
        return configured_target_columns
    if table_exists(silver_table):
        return spark.table(silver_table).columns
    cols = [c for c in src_df.columns if c.lower() not in METADATA_COLUMNS and c.lower() not in drop]
    for c in transforms:
        if c not in cols:
            cols.append(c)
    return cols


def target_type_lookup(silver_table, configured_target_schema=None):
    if table_exists(silver_table):
        return {f.name.lower(): f.dataType for f in spark.table(silver_table).schema.fields}
    return {str(k).lower(): str(v) for k, v in (configured_target_schema or {}).items()}


def transform_expr(transform, src_df):
    return transform(src_df) if callable(transform) else transform


def is_string_target_type(target_type):
    if target_type is None:
        return False
    if hasattr(target_type, "simpleString"):
        type_name = target_type.simpleString().lower()
    else:
        type_name = str(target_type).lower()
    return type_name == "string" or type_name.endswith("stringtype")


def legacy_ef_string(expr):
    """Mirror DataRow.ToString(): SQL NULL becomes empty string in legacy EF saves."""
    return F.coalesce(expr.cast("string"), F.lit(""))


def align_to_target(src_df, silver_table, configured_target_columns=None, configured_target_schema=None, transforms=None, drop_columns=None):
    transforms = transforms or {}
    cols = target_columns_for(src_df, silver_table, configured_target_columns, transforms, drop_columns)
    target_schema = target_type_lookup(silver_table, configured_target_schema)

    exprs = []
    for target_col in cols:
        if target_col in transforms:
            expr = transform_expr(transforms[target_col], src_df)
        else:
            source_col = actual_col(src_df, target_col, required=False)
            expr = F.col(source_col) if source_col else F.lit(None)
        target_type = target_schema.get(target_col.lower())
        if target_type:
            expr = expr.cast(target_type)
        if is_string_target_type(target_type):
            expr = legacy_ef_string(expr)
        exprs.append(expr.alias(target_col))
    return src_df.select(*exprs)


def sql_quote(value):
    return "'" + str(value).replace("'", "''") + "'"


def sql_in_values(values):
    clean = sorted({str(v) for v in values if v})
    return "(" + ",".join(sql_quote(v) for v in clean) + ")" if clean else None


def null_safe_condition(keys, match_key_transforms=None):
    transforms = match_key_transforms or {}
    parts = []
    for key in keys:
        if transforms.get(key) == "abs":
            parts.append(f"abs(target.`{key}`) <=> abs(source.`{key}`)")
        else:
            parts.append(f"target.`{key}` <=> source.`{key}`")
    return " AND ".join(parts)


def checksum_changed_condition(column="RowChkSum"):
    return f"NOT (target.`{column}` <=> source.`{column}`)"


def with_transformed_join_keys(df, keys, match_key_transforms=None, prefix="__key_"):
    transforms = match_key_transforms or {}
    out = df
    for key in keys:
        key_col = actual_col(out, key, required=True)
        out_col = f"{prefix}{key.lower()}"
        if transforms.get(key) == "abs":
            out = out.withColumn(out_col, F.abs(F.col(key_col)))
        else:
            out = out.withColumn(out_col, F.col(key_col))
    return out


def transformed_join_columns(keys, prefix="__key_"):
    return [f"{prefix}{key.lower()}" for key in keys]


def count_insert_update_candidates(source_df, silver_table, match_keys, match_key_transforms=None, update_strategy="always", checksum_column="RowChkSum"):
    if not table_exists(silver_table):
        return source_df.count(), 0

    target_df = spark.table(silver_table)
    src_keys = with_transformed_join_keys(source_df, match_keys, match_key_transforms, "__key_")
    tgt_keys = with_transformed_join_keys(target_df, match_keys, match_key_transforms, "__key_")
    join_cols = transformed_join_columns(match_keys, "__key_")

    target_key_cols = join_cols
    if update_strategy == "checksum" and actual_col(target_df, checksum_column, required=False):
        target_key_cols = join_cols + [actual_col(target_df, checksum_column)]

    target_keys = tgt_keys.select(*target_key_cols).dropDuplicates(join_cols)
    rows_inserted = src_keys.join(target_keys.select(*join_cols), join_cols, "left_anti").count()

    if update_strategy == "checksum" and actual_col(source_df, checksum_column, required=False) and actual_col(target_df, checksum_column, required=False):
        checksum_name = actual_col(target_df, checksum_column)
        joined = (
            src_keys.alias("s")
            .join(target_keys.alias("t"), join_cols, "inner")
            .where(~(F.col(f"s.{checksum_column}").eqNullSafe(F.col(f"t.{checksum_name}"))))
        )
        rows_updated = joined.count()
    else:
        rows_updated = src_keys.join(target_keys.select(*join_cols), join_cols, "inner").count()

    return rows_inserted, rows_updated


def dedupe_source(df, match_keys, order_columns=None, match_key_transforms=None):
    working = df
    partition_cols = []
    transforms = match_key_transforms or {}
    for key in match_keys:
        key_col = actual_col(working, key, required=True)
        working = working.where(F.col(key_col).isNotNull())
        temp_col = f"__dedupe_key_{len(partition_cols)}"
        key_expr = F.abs(F.col(key_col)) if transforms.get(key) == "abs" else F.col(key_col)
        working = working.withColumn(temp_col, key_expr)
        partition_cols.append(temp_col)

    order_exprs = []
    for col_name in order_columns or []:
        found = actual_col(working, col_name, required=False)
        if found:
            order_exprs.append(F.col(found).desc_nulls_last())
    if not order_exprs:
        for fallback in ("ExtractedAt", "LastModAt"):
            found = actual_col(working, fallback, required=False)
            if found:
                order_exprs.append(F.col(found).desc_nulls_last())
    if not order_exprs:
        return working.dropDuplicates(partition_cols).drop(*partition_cols)

    window = Window.partitionBy(*[F.col(c) for c in partition_cols]).orderBy(*order_exprs)
    return working.withColumn("__rn", F.row_number().over(window)).where(F.col("__rn") == 1).drop("__rn", *partition_cols)


def reset_rowstate_site_all(silver_table, sites, rowstate_value=False, update_lastmod=True):
    if not sites or not table_exists(silver_table):
        return 0
    target_df = spark.table(silver_table)
    site_col = actual_col(target_df, "SiteCode", required=False)
    rowstate_col = actual_col(target_df, "RowState", required=False)
    if not site_col or not rowstate_col:
        return 0

    condition = f"`{site_col}` IN {sql_in_values(sites)} AND `{rowstate_col}` = true"
    updates = {rowstate_col: "false"}
    lastmod_col = actual_col(target_df, "LastModAt", required=False)
    if update_lastmod and lastmod_col:
        updates[lastmod_col] = "current_timestamp()"
    DeltaTable.forName(spark, silver_table).update(condition=condition, set=updates)
    return 0


def reset_rowstate_by_filter(silver_table, condition_sql, update_lastmod=True):
    if not condition_sql or not table_exists(silver_table):
        return 0
    target_df = spark.table(silver_table)
    rowstate_col = actual_col(target_df, "RowState", required=False)
    if not rowstate_col:
        return 0
    condition = f"`{rowstate_col}` = true AND ({condition_sql})"
    updates = {rowstate_col: "false"}
    lastmod_col = actual_col(target_df, "LastModAt", required=False)
    if update_lastmod and lastmod_col:
        updates[lastmod_col] = "current_timestamp()"
    DeltaTable.forName(spark, silver_table).update(condition=condition, set=updates)
    return 0


def reset_rowstate_missing_by_key(silver_table, source_df, match_keys, sites=None, target_filter_sql=None, update_lastmod=True, match_key_transforms=None):
    if not table_exists(silver_table):
        return 0
    target_df = spark.table(silver_table)
    rowstate_col = actual_col(target_df, "RowState", required=False)
    site_col = actual_col(target_df, "SiteCode", required=False)
    if not rowstate_col:
        return 0

    scoped_target = target_df.where(F.col(rowstate_col).cast("boolean") == F.lit(True))
    if sites and site_col:
        scoped_target = scoped_target.where(F.col(site_col).isin([str(site) for site in sites]))
    if target_filter_sql:
        scoped_target = scoped_target.where(target_filter_sql)

    src_keys = with_transformed_join_keys(source_df.select(*match_keys).dropDuplicates(), match_keys, match_key_transforms, "__key_")
    tgt_keys = with_transformed_join_keys(scoped_target.select(*match_keys).dropDuplicates(), match_keys, match_key_transforms, "__key_")
    join_cols = transformed_join_columns(match_keys, "__key_")
    missing_keys = tgt_keys.join(src_keys.select(*join_cols).dropDuplicates(), join_cols, "left_anti").select(*match_keys)

    if missing_keys.limit(1).count() == 0:
        return 0

    updates = {rowstate_col: "false"}
    lastmod_col = actual_col(target_df, "LastModAt", required=False)
    if update_lastmod and lastmod_col:
        updates[lastmod_col] = "current_timestamp()"

    DeltaTable.forName(spark, silver_table).alias("target").merge(
        missing_keys.alias("source"),
        null_safe_condition(match_keys, match_key_transforms),
    ).whenMatchedUpdate(set=updates).execute()
    return missing_keys.count()


def min_year_from_source(df, source_date_column="SourceQueryStartDate"):
    found = actual_col(df, source_date_column, required=False)
    if not found:
        return None
    row = df.select(F.min(F.year(F.col(found))).alias("min_year")).collect()[0]
    return row["min_year"]


def max_date_from_source(df, source_date_column="SourceQueryEndDate"):
    found = actual_col(df, source_date_column, required=False)
    if not found:
        return None
    row = df.select(F.max(F.to_date(F.col(found))).alias("max_date")).collect()[0]
    return row["max_date"]


def apply_pre_reset(method_name, silver_table, bronze_df, silver_df, match_keys, successful_sites, reset_rule=None, match_key_transforms=None):
    reset_rule = reset_rule or {}
    mode = reset_rule.get("mode")
    if not mode:
        return 0

    if mode == "site_all":
        return reset_rowstate_site_all(silver_table, successful_sites, update_lastmod=reset_rule.get("update_lastmod", True))

    if mode == "bill_year_window":
        min_year = min_year_from_source(bronze_df)
        max_end = max_date_from_source(bronze_df)
        site_values = sql_in_values(successful_sites)
        if not min_year or not max_end or not site_values:
            return 0
        condition = (
            f"`SiteCode` IN {site_values} "
            f"AND year(`billDate`) >= {int(min_year)} "
            f"AND `billDate` <= date_add(date'{max_end.isoformat()}', 15)"
        )
        return reset_rowstate_by_filter(silver_table, condition, update_lastmod=True)

    if mode == "year_from_source":
        date_col = reset_rule["target_date_column"]
        min_year = min_year_from_source(bronze_df)
        site_values = sql_in_values(successful_sites)
        if not min_year or not site_values:
            return 0
        condition = f"`SiteCode` IN {site_values} AND year(`{date_col}`) >= {int(min_year)}"
        return reset_rowstate_by_filter(silver_table, condition, update_lastmod=reset_rule.get("update_lastmod", False))

    if mode == "missing_by_key":
        return reset_rowstate_missing_by_key(
            silver_table,
            silver_df,
            match_keys,
            sites=successful_sites,
            update_lastmod=reset_rule.get("update_lastmod", True),
            match_key_transforms=match_key_transforms,
        )

    if mode == "claims_mixed":
        site_col = actual_col(silver_df, "SiteCode", required=True)
        bulk_sites = [site for site in successful_sites if site not in CLAIMS_EF_SITES]
        ef_sites = [site for site in successful_sites if site in CLAIMS_EF_SITES]
        changed = 0
        if bulk_sites:
            bulk_source = silver_df.where(F.col(site_col).isin(bulk_sites))
            changed += reset_rowstate_missing_by_key(
                silver_table,
                bulk_source,
                match_keys,
                sites=bulk_sites,
                update_lastmod=True,
                match_key_transforms=match_key_transforms,
            )
        if ef_sites:
            min_year = min_year_from_source(bronze_df.where(F.col(site_col).isin(ef_sites)))
            filter_sql = f"year(tpcCreatedDate) >= {int(min_year)}" if min_year else None
            ef_source = silver_df.where(F.col(site_col).isin(ef_sites))
            changed += reset_rowstate_missing_by_key(
                silver_table,
                ef_source,
                match_keys,
                sites=ef_sites,
                target_filter_sql=filter_sql,
                update_lastmod=True,
                match_key_transforms=match_key_transforms,
            )
        return changed

    raise ValueError(f"Unsupported reset_rule mode: {mode}")


def delta_merge(
    silver_df,
    silver_table,
    match_keys,
    update_strategy="always",
    checksum_column="RowChkSum",
    unchanged_update_set=None,
    insert_condition=None,
    match_key_transforms=None,
):
    insert_all = {c: f"source.`{c}`" for c in silver_df.columns}
    update_all = {c: f"source.`{c}`" for c in silver_df.columns}
    merge = DeltaTable.forName(spark, silver_table).alias("target").merge(
        silver_df.alias("source"),
        null_safe_condition(match_keys, match_key_transforms),
    )

    if update_strategy == "checksum":
        merge = merge.whenMatchedUpdate(
            condition=checksum_changed_condition(checksum_column),
            set=update_all,
        )
        if unchanged_update_set:
            merge = merge.whenMatchedUpdate(set=unchanged_update_set)
    elif update_strategy == "always":
        merge = merge.whenMatchedUpdate(set=update_all)
    else:
        raise ValueError(f"Unsupported update_strategy: {update_strategy}")

    if insert_condition:
        merge = merge.whenNotMatchedInsert(condition=insert_condition, values=insert_all)
    else:
        merge = merge.whenNotMatchedInsert(values=insert_all)
    merge.execute()


def merge_to_silver(
    method_name,
    bronze_table,
    silver_table,
    match_keys,
    configured_target_columns=None,
    configured_target_schema=None,
    transforms=None,
    drop_columns=None,
    update_strategy="always",
    checksum_column="RowChkSum",
    unchanged_update_set=None,
    insert_condition=None,
    source_filter=None,
    reset_rule=None,
    match_key_transforms=None,
    dedupe_order_columns=None,
    post_merge=None,
):
    rows_read = 0
    rows_inserted = 0
    rows_updated = 0
    rows_skipped = 0
    site_results = None

    try:
        bronze_had_method_failure = bronze_failed_for(method_name)
        bronze_df = load_bronze(bronze_table)
        row_successful_sites, bronze_site_col = successful_sites_from_bronze(bronze_df)
        successful_sites = sorted(set(row_successful_sites))
        site_results = build_site_results(
            method_name,
            successful_sites,
            bronze_had_method_failure=bronze_had_method_failure,
            failed_message=f"{method_name} Bronze copy failed or did not write rows for this site.",
        )

        if bronze_had_method_failure:
            if not successful_sites:
                notebook_exit(result_payload(
                    method_name,
                    "SKIPPED",
                    rows_read=0,
                    message="All Bronze sites failed or no successful Bronze rows were found, so silver was skipped.",
                    site_results=site_results,
                ))
            if bronze_site_col:
                bronze_df = bronze_df.where(F.col(bronze_site_col).isin(successful_sites))

        rows_read = bronze_df.count()
        if rows_read == 0:
            if not table_exists(silver_table):
                empty_df = align_to_target(bronze_df, silver_table, configured_target_columns, configured_target_schema, transforms, drop_columns)
                for key in match_keys:
                    actual_col(empty_df, key, required=True)
                empty_df.limit(0).write.format("delta").mode("overwrite").saveAsTable(silver_table)
            notebook_exit(result_payload(method_name, "SUCCESS", rows_read=0, message="No bronze rows found for this ingest run.", site_results=site_results))

        if source_filter is not None:
            bronze_df = bronze_df.where(source_filter(bronze_df) if callable(source_filter) else source_filter)

        silver_df = align_to_target(bronze_df, silver_table, configured_target_columns, configured_target_schema, transforms, drop_columns)
        silver_df = dedupe_source(silver_df, match_keys, dedupe_order_columns, match_key_transforms)

        rows_after_scope = silver_df.count()
        if rows_after_scope == 0:
            if not table_exists(silver_table):
                silver_df.limit(0).write.format("delta").mode("overwrite").saveAsTable(silver_table)
            notebook_exit(result_payload(
                method_name,
                "SUCCESS",
                rows_read=rows_read,
                rows_skipped=rows_skipped,
                message="No rows remained after legacy scope/key rules.",
                site_results=site_results,
            ))

        if not table_exists(silver_table):
            silver_df.write.format("delta").mode("overwrite").saveAsTable(silver_table)
            rows_inserted = rows_after_scope
            if post_merge:
                post_merge(method_name, silver_table, silver_df)
            notebook_exit(result_payload(method_name, "SUCCESS", rows_read=rows_read, rows_inserted=rows_inserted, rows_skipped=rows_skipped, site_results=site_results))

        apply_pre_reset(method_name, silver_table, bronze_df, silver_df, match_keys, successful_sites, reset_rule, match_key_transforms)
        rows_inserted, rows_updated = count_insert_update_candidates(
            silver_df,
            silver_table,
            match_keys,
            match_key_transforms=match_key_transforms,
            update_strategy=update_strategy,
            checksum_column=checksum_column,
        )

        delta_merge(
            silver_df,
            silver_table,
            match_keys,
            update_strategy=update_strategy,
            checksum_column=checksum_column,
            unchanged_update_set=unchanged_update_set,
            insert_condition=insert_condition,
            match_key_transforms=match_key_transforms,
        )
        if post_merge:
            post_merge(method_name, silver_table, silver_df)

        notebook_exit(result_payload(method_name, "SUCCESS", rows_read=rows_read, rows_inserted=rows_inserted, rows_updated=rows_updated, rows_skipped=rows_skipped, site_results=site_results))

    except Exception as ex:
        if is_notebook_exit(ex):
            raise
        notebook_exit(result_payload(method_name, "FAILED", rows_read=rows_read, rows_inserted=rows_inserted, rows_updated=rows_updated, rows_skipped=rows_skipped, message=traceback.format_exc(), site_results=site_results))


def current_ts():
    return F.current_timestamp()


def current_date_ts():
    return F.current_date().cast("timestamp")


def bool_from_deleted(df, column_name="IsDeleted", true_value=False, false_value=True):
    return F.when(F.coalesce(col_or_null(df, column_name).cast("boolean"), F.lit(False)), F.lit(true_value)).otherwise(F.lit(false_value))


def trim_truncate(df, column_name, max_check_length, output_length):
    value = F.trim(col_or_null(df, column_name).cast("string"))
    return F.when(F.length(value) > F.lit(max_check_length), F.substring(value, 1, output_length)).otherwise(value)


def parse_legacy_date(df, column_name):
    value = col_or_null(df, column_name)
    typed = F.when(value.isNotNull(), value.cast("timestamp"))
    as_text = F.trim(F.regexp_replace(value.cast("string"), "-", "/"))
    from_string = F.when(
        F.length(as_text) > F.lit(6),
        F.coalesce(
            F.to_timestamp(as_text, "yyyy/MM/dd HH:mm:ss"),
            F.to_timestamp(as_text, "yyyy/MM/dd"),
            F.to_timestamp(as_text, "yyyy-MM-dd HH:mm:ss"),
            F.to_timestamp(as_text, "yyyy-MM-dd"),
        ),
    )
    return F.coalesce(typed, from_string)


def clientdemo_sql_substring_0_12(df, column_name):
    value = col_or_null(df, column_name).cast("string")
    return F.when(F.length(value) > 13, F.substring(value, 1, 11)).otherwise(value)


def sync_clientdemo2_rowstate_from_demo1(method_name, silver_table, silver_df):
    demo1_table = "bhg_silver.pats.tbl_ClientDemo1"
    if not table_exists(demo1_table) or not table_exists(silver_table):
        return
    demo1 = spark.table(demo1_table).select("SiteCode", "ClientID", "RowState", "LastModAt").dropDuplicates(["SiteCode", "ClientID"])
    demo2 = spark.table(silver_table).select("SiteCode", "clientID", "RowState")
    changes = (
        demo2.alias("d2")
        .join(demo1.alias("d1"), (F.col("d2.SiteCode") == F.col("d1.SiteCode")) & (F.col("d2.clientID") == F.col("d1.ClientID")), "inner")
        .where(~F.col("d2.RowState").eqNullSafe(F.col("d1.RowState")))
        .select(
            F.col("d1.SiteCode").alias("SiteCode"),
            F.col("d1.ClientID").alias("clientID"),
            F.col("d1.RowState").alias("RowState"),
            F.col("d1.LastModAt").alias("LastModAt"),
        )
    )
    if changes.limit(1).count() == 0:
        return
    DeltaTable.forName(spark, silver_table).alias("target").merge(
        changes.alias("source"),
        "target.`SiteCode` <=> source.`SiteCode` AND target.`clientID` <=> source.`clientID`",
    ).whenMatchedUpdate(set={
        "RowState": "source.`RowState`",
        "LastModAt": "source.`LastModAt`",
    }).execute()
```

## 1. Notebook: `nb_p1_finance_sl_save_bills`
Pipeline activity: `nb_finance_sl_bills`

Legacy note: Checksum-guarded EF merge. Pre-resets active rows in the loaded billDate year/window; unchanged rows still refresh RowState/LastModAt. BillReason and PHC BillSiteID match SaveBills.cs.

```python
METHOD_NAME = 'SaveBills'
SAVE_BILLS_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_BILLS_CONFIG["method_name"]
BRONZE_TABLE = SAVE_BILLS_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_BILLS_CONFIG["silver_table"]
MATCH_KEYS = SAVE_BILLS_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "RowChkSum": "int",
    "billID": "int",
    "billCLTID": "int",
    "billGuestID": "int",
    "billDate": "timestamp",
    "billBILL": "decimal(19,4)",
    "billPAY": "decimal(19,4)",
    "billPAYTYPE": "string",
    "BillAdjust": "decimal(19,4)",
    "billReason": "string",
    "billReceiptNum": "int",
    "strUser": "string",
    "blnDeposit": "boolean",
    "dtDeposit": "timestamp",
    "billADJUSTID": "int",
    "FIFOallocated": "boolean",
    "FIFObalance": "decimal(19,4)",
    "Costcenter": "string",
    "BillAptID": "int",
    "billORGdt": "date",
    "BillServID": "int",
    "BillSiteID": "int",
    "LastModAt": "timestamp",
    "RowState": "boolean"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'RowState': lambda df: F.when(F.coalesce(col_or_null(df, 'billCLTID').cast('int'), F.lit(0)) <= 0, F.lit(False)).otherwise(F.lit(True)),
    'billReason': lambda df: trim_truncate(df, 'billReason', 2500, 2498),
    'BillSiteID': lambda df: F.when(col_or_null(df, 'SiteCode') == F.lit('PHC'), F.lit(105)).otherwise(col_or_null(df, 'BillSiteID')),
}

UNCHANGED_UPDATE_SET = {
    "RowState": "source.`RowState`",
    "LastModAt": "current_timestamp()"
}
RESET_RULE = {
    "mode": "bill_year_window"
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='checksum',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 2. Notebook: `nb_p1_finance_sl_save_auths`
Pipeline activity: `nb_finance_sl_pbi3_pay_auth`

Legacy note: Checksum-guarded EF merge with full site RowState cycle. Date string normalization and tpServ truncation follow SaveAuths.cs.

```python
METHOD_NAME = 'SaveAuths'
SAVE_AUTHS_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_AUTHS_CONFIG["method_name"]
BRONZE_TABLE = SAVE_AUTHS_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_AUTHS_CONFIG["silver_table"]
MATCH_KEYS = SAVE_AUTHS_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "tpaID": "int",
    "tpEFFDate": "date",
    "tpaCLTID": "int",
    "tpaPayer": "string",
    "tpaDESC": "string",
    "tpaEffDATE": "timestamp",
    "tpaTermDATE": "timestamp",
    "tpaSTAFF": "string",
    "tpadt": "timestamp",
    "tpaAuthCode": "string",
    "tpAUTHPATH": "string",
    "tpCONFIRMPath": "string",
    "TpFail": "string",
    "tpRequestForm": "string",
    "tpResponseForm": "string",
    "tpServ": "string",
    "tpTermDate": "date",
    "tpUNITS": "int",
    "tpSERVAPPROVED": "string",
    "tpNOTE": "string",
    "tpTYPE": "string",
    "tpaCompKey": "string",
    "tpaBigKey": "string",
    "ProgGroup": "string",
    "PayerGroup": "string",
    "PayerType": "string",
    "LastModAt": "timestamp",
    "RowChkSum": "int",
    "RowState": "boolean",
    "Provider": "string",
    "ProviderSiteId": "int"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'RowState': lambda df: F.lit(True),
    'tpServ': lambda df: trim_truncate(df, 'tpServ', 300, 299),
    'tpSERVAPPROVED': lambda df: F.trim(col_or_null(df, 'tpSERVAPPROVED').cast('string')),
    'tpaEffDATE': lambda df: parse_legacy_date(df, 'tpaEffDATE'),
    'tpaTermDATE': lambda df: parse_legacy_date(df, 'tpaTermDATE'),
    'tpadt': lambda df: parse_legacy_date(df, 'tpadt'),
    'tpTermDate': lambda df: parse_legacy_date(df, 'tpTermDate'),
}

UNCHANGED_UPDATE_SET = {
    "RowState": "True",
    "LastModAt": "current_timestamp()"
}
RESET_RULE = {
    "mode": "site_all"
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='checksum',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 3. Notebook: `nb_p1_finance_sl_save_auth_billsub`
Pipeline activity: `nb_finance_sl_vw3p_bill_sub`

Legacy note: Unconditional matched update. SP checksum predicate is commented; EF B41/B42 path also stores checksum without a skip guard.

```python
METHOD_NAME = 'SaveAuthBillsub'
SAVE_AUTH_BILLSUB_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_AUTH_BILLSUB_CONFIG["method_name"]
BRONZE_TABLE = SAVE_AUTH_BILLSUB_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_AUTH_BILLSUB_CONFIG["silver_table"]
MATCH_KEYS = SAVE_AUTH_BILLSUB_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "descript": "string",
    "billdatecriteria": "timestamp",
    "payDEFAULTSUBMIT": "string",
    "ScrubError": "string",
    "dsID": "int",
    "dsClt": "int",
    "dsTxtSrv": "string",
    "dsDtStart": "timestamp",
    "dsDtEnd": "timestamp",
    "dsTxtType": "string",
    "dsdblUnits": "double",
    "billUnits": "double",
    "dstxtStaff": "string",
    "npi": "string",
    "DSbilled": "timestamp",
    "pyPAYERID": "string",
    "pySUBSID": "string",
    "pyGROUP": "string",
    "CPTCODE": "string",
    "charge": "double",
    "tpaAuthCode": "string",
    "clientname": "string",
    "cltDOB": "timestamp",
    "cltGender": "string",
    "cltADD1": "string",
    "cltCity": "string",
    "cltState": "string",
    "cltzip": "string",
    "cltPhone": "string",
    "cltMARRY": "string",
    "cltM4ID": "string",
    "dsdiag": "string",
    "Modifier": "string",
    "dsPOS": "string",
    "NDC": "string",
    "MG": "double",
    "SiteID": "int",
    "dsarea": "string",
    "payclass": "string",
    "LastModAt": "timestamp",
    "RowState": "boolean",
    "RowChkSum": "int",
    "CptMod": "string"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'RowState': lambda df: F.lit(True),
    'pySUBSID': lambda df: F.coalesce(col_or_null(df, 'pySUBSID').cast('string'), F.lit(':(')),
    'charge': lambda df: F.coalesce(col_or_null(df, 'charge').cast('double'), F.lit(0.0)),
    'CptMod': lambda df: F.coalesce(col_or_null(df, 'CptMod').cast('string'), F.lit(':(')),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {
    "mode": "site_all",
    "update_lastmod": False
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 4. Notebook: `nb_p1_finance_sl_save_fmp`
Pipeline activity: `nb_finance_sl_fmp`

Legacy note: No RowChkSum target. Full site RowState cycle and DateTime.Today-style LastModAt match SaveFmp.cs.

```python
METHOD_NAME = 'SaveFmp'
SAVE_FMP_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_FMP_CONFIG["method_name"]
BRONZE_TABLE = SAVE_FMP_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_FMP_CONFIG["silver_table"]
MATCH_KEYS = SAVE_FMP_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "fmpID": "int",
    "fmpLngClt": "int",
    "fmpDtStart": "timestamp",
    "fmpDtProjEnd": "timestamp",
    "fmpDtEnd": "timestamp",
    "fmpIntRate": "int",
    "fmpStrReason": "string",
    "fmpStrDesc": "string",
    "fmpDtAdded": "timestamp",
    "fmpStrUserAdded": "string",
    "fmpDtEnded": "timestamp",
    "fmpStrUserEnded": "string",
    "fmPENDTEXT": "string",
    "atriskTYPE": "string",
    "RowState": "boolean",
    "LastModAt": "timestamp"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_date_ts(),
    'RowState': lambda df: F.lit(True),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {
    "mode": "site_all"
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 5. Notebook: `nb_p1_finance_sl_save_payer_clt_history`
Pipeline activity: `nb_finance_sl_payer_clt_history`

Legacy note: No RowChkSum/RowState. Loaded EF entities are tracked, so updates are preserved even though UpdateRange is commented.

```python
METHOD_NAME = 'SavePayerCltHistory'
SAVE_PAYER_CLT_HISTORY_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_PAYER_CLT_HISTORY_CONFIG["method_name"]
BRONZE_TABLE = SAVE_PAYER_CLT_HISTORY_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_PAYER_CLT_HISTORY_CONFIG["silver_table"]
MATCH_KEYS = SAVE_PAYER_CLT_HISTORY_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "pchID": "int",
    "pyID": "int",
    "pyChange": "string",
    "pyDtm": "timestamp",
    "pyUser": "string",
    "pyNote": "string"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 6. Notebook: `nb_p1_finance_sl_save_financial_hardship_application`
Pipeline activity: `nb_finance_sl_financial_hardship_application`

Legacy note: Unconditional EF update. IsDeleted empty defaults false. Mirrors SavePAData.cs bug: FHAPatientSignatureDate stays null; ExpirationDate coalesces source ExpirationDate then FHAPatientSignatureDate (length > 6).

```python
METHOD_NAME = 'SaveFinancialHardshipApplication'
SAVE_FINANCIAL_HARDSHIP_APPLICATION_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_FINANCIAL_HARDSHIP_APPLICATION_CONFIG["method_name"]
BRONZE_TABLE = SAVE_FINANCIAL_HARDSHIP_APPLICATION_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_FINANCIAL_HARDSHIP_APPLICATION_CONFIG["silver_table"]
MATCH_KEYS = SAVE_FINANCIAL_HARDSHIP_APPLICATION_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "RowState": "boolean",
    "RowChkSum": "int",
    "LastModAt": "timestamp",
    "Id": "int",
    "DataFormId": "int",
    "PreAdmissionId": "int",
    "cltId": "int",
    "CreatedOn": "timestamp",
    "CreatedBy": "string",
    "ModifiedOn": "timestamp",
    "ModifiedBy": "string",
    "IsDeleted": "boolean",
    "IsIdentification": "boolean",
    "IsIncome": "boolean",
    "txtIncomeIdentification": "string",
    "FHAPatientSignature": "string",
    "FHAPatientSignatureDate": "timestamp",
    "FHAPatientSignatureBy": "string",
    "txtAnnualHouseholdIncome": "string",
    "EmergencyName": "string",
    "EmergencyRelation": "string",
    "EmergencyPhone": "string",
    "txtAUIGross1": "double",
    "txtAUIGross2": "double",
    "txtAUIGross3": "double",
    "txtAUISocial1": "double",
    "txtAUISocial2": "double",
    "txtAUISocial3": "double",
    "txtAUIAlimony1": "double",
    "txtAUIAlimony2": "double",
    "txtAUIAlimony3": "double",
    "txtAUISelf1": "double",
    "txtAUISelf2": "double",
    "txtAUISelf3": "double",
    "txtAUIRent1": "double",
    "txtAUIRent2": "double",
    "txtAUIRent3": "double",
    "Version": "string",
    "IscurrentlyUninsured": "boolean",
    "StatusofApplication": "string",
    "Facts": "string",
    "PayClassApproved": "string",
    "ApprovedBy": "string",
    "EffectiveDate": "timestamp",
    "ExpirationDate": "timestamp"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'IsDeleted': lambda df: F.coalesce(col_or_null(df, 'IsDeleted').cast('boolean'), F.lit(False)),
    'RowState': lambda df: bool_from_deleted(df, 'IsDeleted', false_value=True, true_value=False),
    'FHAPatientSignatureDate': lambda df: F.lit(None),
    'ExpirationDate': lambda df: F.coalesce(
        parse_legacy_date(df, 'ExpirationDate'),
        parse_legacy_date(df, 'FHAPatientSignatureDate'),
    ),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 7. Notebook: `nb_p1_finance_sl_save_3p_elig`
Pipeline activity: `nb_finance_sl_3p_elig`

Legacy note: Checksum-guarded EF merge. Pre-resets the loaded eDate year scope and reactivates unchanged rows.

```python
METHOD_NAME = 'Save3pElig'
SAVE3P_ELIG_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE3P_ELIG_CONFIG["method_name"]
BRONZE_TABLE = SAVE3P_ELIG_CONFIG["bronze_table"]
SILVER_TABLE = SAVE3P_ELIG_CONFIG["silver_table"]
MATCH_KEYS = SAVE3P_ELIG_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "RowChkSum": "int",
    "RowState": "boolean",
    "LastModAt": "timestamp",
    "eID": "int",
    "eCLT": "int",
    "ePAYER": "string",
    "eDATE": "date",
    "eStaff": "string",
    "ePOST": "string",
    "eRESPONSE": "string",
    "eStatus": "string",
    "eFormat": "string",
    "Filepath": "string",
    "eELECSTATUS": "string",
    "EStaffSTATUS": "string",
    "EStaffNote": "string",
    "eSCAN": "string",
    "eORIGID": "int",
    "pyeligcheck": "date"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'RowState': lambda df: F.lit(True),
}

UNCHANGED_UPDATE_SET = {
    "RowState": "True"
}
RESET_RULE = {
    "mode": "year_from_source",
    "target_date_column": "edate",
    "update_lastmod": False
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='checksum',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 8. Notebook: `nb_p1_finance_sl_save_claim_line_item`
Pipeline activity: `nb_finance_sl_claim_line_item`

Legacy note: Bulk SP parity: missing-by-key RowState reset, unconditional matched update, full-load source despite daily map WHERE.

```python
METHOD_NAME = 'SaveClaimLineItem'
SAVE_CLAIM_LINE_ITEM_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_CLAIM_LINE_ITEM_CONFIG["method_name"]
BRONZE_TABLE = SAVE_CLAIM_LINE_ITEM_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_CLAIM_LINE_ITEM_CONFIG["silver_table"]
MATCH_KEYS = SAVE_CLAIM_LINE_ITEM_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "tpcliID": "int",
    "RowChkSum": "int",
    "LastModAt": "timestamp",
    "RowState": "boolean",
    "tpcliTPCID": "int",
    "tpcliDtmService": "timestamp",
    "tpcliTxtService": "string",
    "tpcliIntUnits": "int",
    "tpcliDtmAdded": "timestamp",
    "tpcliStrAdded": "string",
    "tpcliAmtCharge": "decimal(19,4)",
    "tpcliStrCPT": "string",
    "tpcliStrModifier": "string",
    "tpcliStrNDC": "string",
    "tpcliStrPOS": "string",
    "tpcliIntDx1": "int",
    "tpcliIntDx2": "int",
    "tpcliIntDx3": "int",
    "tpcliIntDx4": "int",
    "tpcliDiagnosis": "string",
    "tpcliDSID": "int",
    "tpcliPayerClaimID": "string",
    "tpcliProviderId": "string",
    "tpcliUNITFEE": "decimal(19,4)",
    "tpcliVOID": "boolean",
    "tpclivoidDT": "date",
    "tpclivoidUSER": "string",
    "tpcliDtmServiceTo": "timestamp",
    "tpcliIntMg": "int",
    "tpcliDBnotes": "string"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowState': lambda df: F.lit(True),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {
    "mode": "missing_by_key"
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 9. Notebook: `nb_p1_finance_sl_save_claim_line_item_activity`
Pipeline activity: `nb_finance_sl_claim_line_item_activity`

Legacy note: Bulk SP parity: missing-by-key RowState reset and unconditional matched update.

```python
METHOD_NAME = 'SaveClaimLineItemActivity'
SAVE_CLAIM_LINE_ITEM_ACTIVITY_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_CLAIM_LINE_ITEM_ACTIVITY_CONFIG["method_name"]
BRONZE_TABLE = SAVE_CLAIM_LINE_ITEM_ACTIVITY_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_CLAIM_LINE_ITEM_ACTIVITY_CONFIG["silver_table"]
MATCH_KEYS = SAVE_CLAIM_LINE_ITEM_ACTIVITY_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "liaID": "int",
    "RowChkSum": "int",
    "LastModAt": "timestamp",
    "RowState": "boolean",
    "liaTPCLIID": "int",
    "liaDtm": "timestamp",
    "liaStrUser": "string",
    "laiPAIDINS": "decimal(19,4)",
    "laiContADJ": "decimal(19,4)",
    "laiGENADJ": "decimal(19,4)",
    "laiCOPay": "decimal(19,4)",
    "laiDEDUC": "decimal(19,4)",
    "laiCLIENT": "decimal(19,4)",
    "liaBitNoteOnly": "boolean",
    "liaStrDesc": "string",
    "tprbID": "int",
    "liaPending": "boolean",
    "liaamt": "decimal(19,4)",
    "liastrtext": "string",
    "liaADJREASON": "string",
    "laiCOINS": "decimal(19,4)",
    "liaAction1": "string",
    "liaAction2": "string",
    "liaADJcontract": "string",
    "liaADJgeneral": "string",
    "liaANSI1": "string",
    "liaANSI2": "string",
    "liaANSImod1": "string",
    "liaANSImod2": "string",
    "BillID": "int",
    "liaDBnotes": "string"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowState': lambda df: F.lit(True),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {
    "mode": "missing_by_key"
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 10. Notebook: `nb_p1_finance_sl_save_claims`
Pipeline activity: `nb_finance_sl_claims`

Legacy note: Bulk path is unconditional; EF exception sites are checksum-oriented but final values align with unconditional same-value updates. PHC SiteID override and tpcID > 0 insert rule match ClaimsMerge.

```python
METHOD_NAME = 'SaveClaims'
SAVE_CLAIMS_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_CLAIMS_CONFIG["method_name"]
BRONZE_TABLE = SAVE_CLAIMS_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_CLAIMS_CONFIG["silver_table"]
MATCH_KEYS = SAVE_CLAIMS_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "tpcID": "int",
    "RowChkSum": "int",
    "LastModAt": "timestamp",
    "RowState": "boolean",
    "tpccltID": "int",
    "tpcStrStatus": "string",
    "tpcStrPayer": "string",
    "tpcDtmAdded": "timestamp",
    "tpcStrAdded": "string",
    "f10oth": "string",
    "tpcClaimBatchID": "int",
    "f11insnumber": "string",
    "f11insplan": "string",
    "f11inssex": "string",
    "f12sig": "string",
    "f12sigdate": "string",
    "f13inssig": "string",
    "f14date": "string",
    "f15firstdate": "string",
    "f16dateunableend": "string",
    "f10auto": "string",
    "tpcStrPrimary": "string",
    "f10employ": "string",
    "f10local": "string",
    "f11insanother": "string",
    "f11insdob": "string",
    "f11insemploy": "string",
    "f16dateunablestart": "string",
    "f17refername": "string",
    "f17refernpi": "string",
    "f18datehospend": "string",
    "f18datehospstart": "string",
    "f19local": "string",
    "f1id": "string",
    "f20outsidelab": "string",
    "f21diag1": "string",
    "f21diag2": "string",
    "f21diag3": "string",
    "f21diag4": "string",
    "f22medresub": "string",
    "f23priorauth": "string",
    "f25taxid": "string",
    "f26account": "string",
    "f27assign": "string",
    "f28totalcharge": "string",
    "f29amtpaid": "string",
    "f2name": "string",
    "f30balancedue": "string",
    "f31date": "string",
    "f31phys": "string",
    "f32a": "string",
    "f32b": "string",
    "f32line1": "string",
    "f32line2": "string",
    "f32line3": "string",
    "f32line4": "string",
    "f33a": "string",
    "f33b": "string",
    "f33line1": "string",
    "f33line2": "string",
    "f33line3": "string",
    "f33line4": "string",
    "f33phone": "string",
    "f3dob": "string",
    "f4insname": "string",
    "f5add": "string",
    "f5city": "string",
    "f5phone": "string",
    "f5state": "string",
    "f5zip": "string",
    "f6insrel": "string",
    "f7insadd": "string",
    "f7inscity": "string",
    "f7insphone": "string",
    "f7insstate": "string",
    "f7inszip": "string",
    "f8stat": "string",
    "f9othinsdob": "string",
    "f9othinsemp": "string",
    "f9othinsname": "string",
    "f9othinsnumber": "string",
    "f9othinsplan": "string",
    "f9othinssex": "string",
    "tpcCreatedDate": "timestamp",
    "tpcEncounter": "string",
    "tpcREBILLREASON": "string",
    "tpcStrWeek": "string",
    "tpcWKSTART": "date",
    "tpcPayerCIN": "string",
    "tpcSrvType": "string",
    "f3sex": "string",
    "tpcClaimType": "int",
    "SiteID": "int",
    "tpcDBnotes": "string",
    "tpcReferring": "string"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowState': lambda df: F.lit(True),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'SiteID': lambda df: F.when(col_or_null(df, 'SiteCode') == F.lit('PHC'), F.lit(105)).otherwise(col_or_null(df, 'SiteID')),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {
    "mode": "claims_mixed"
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition='source.`tpcID` > 0',
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 11. Notebook: `nb_p1_finance_sl_save_payer_client`
Pipeline activity: `nb_finance_sl_payer_client`

Legacy note: Unconditional EF update because the legacy checksum guard is disabled with if (1 == 1). Match uses abs(pyCLTID), but stores the source pyCLTID value.

```python
METHOD_NAME = 'SavePayerClient'
SAVE_PAYER_CLIENT_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_PAYER_CLIENT_CONFIG["method_name"]
BRONZE_TABLE = SAVE_PAYER_CLIENT_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_PAYER_CLIENT_CONFIG["silver_table"]
MATCH_KEYS = SAVE_PAYER_CLIENT_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "PCID": "long",
    "RowChkSum": "int",
    "SiteCode": "string",
    "pyID": "int",
    "pyPAYERID": "string",
    "pyPAYERTYPE": "string",
    "pySUBSID": "string",
    "pyGROUP": "string",
    "pyAUTH": "string",
    "pySTART": "timestamp",
    "pyEND": "timestamp",
    "pyCLTID": "int",
    "pyACTIVE": "boolean",
    "pyadd": "string",
    "pycity": "string",
    "pyDOB": "timestamp",
    "pyfirst": "string",
    "pylast": "string",
    "pyPhone": "string",
    "pysame": "boolean",
    "pystate": "string",
    "pyzip": "string",
    "pyAddDate": "date",
    "PyAddUser": "string",
    "pyBACK": "string",
    "pybupe": "string",
    "pycoins": "decimal(19,4)",
    "pycopay": "decimal(19,4)",
    "pyded": "string",
    "pydeduct": "decimal(19,4)",
    "pydeductleft": "decimal(19,4)",
    "pyEligCheck": "timestamp",
    "pyEligUser": "string",
    "pyfront": "string",
    "pymmt": "string",
    "pyout": "string",
    "pyProjectedEnd": "date",
    "tempSavePayer": "string",
    "pyBasicNum": "string",
    "pyCategory": "string",
    "pyHMOprovider": "string",
    "pyLocalOffice": "string",
    "pyDBnotes": "string",
    "TypeOfAgreementCode": "string",
    "LastModAt": "timestamp"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {}
MATCH_KEY_TRANSFORMS = {
    "pyCLTID": "abs"
}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 12. Notebook: `nb_p1_finance_sl_save_tbl_diags`
Pipeline activity: `nb_finance_sl_tbldiag10`

Legacy note: Bulk SP parity for Diag10: no target RowChkSum, missing-by-key RowState reset, unconditional matched update.

```python
METHOD_NAME = 'SaveTblDiags'
SAVE_TBL_DIAGS_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_TBL_DIAGS_CONFIG["method_name"]
BRONZE_TABLE = SAVE_TBL_DIAGS_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_TBL_DIAGS_CONFIG["silver_table"]
MATCH_KEYS = SAVE_TBL_DIAGS_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "dgID": "int",
    "dgCLTID": "int",
    "dgDIAG": "string",
    "dgDESC": "string",
    "dgDATE": "timestamp",
    "dgSTAFF": "string",
    "dgdt": "timestamp",
    "dgPRIMARY": "boolean",
    "dgDIAG10": "string",
    "dgDIAG10Description": "string",
    "dgNote": "string",
    "dgType": "string",
    "EnrollmentId": "int",
    "dgEndDate": "timestamp",
    "LastModAt": "timestamp",
    "RowState": "boolean"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowState': lambda df: F.lit(True),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {
    "mode": "missing_by_key",
    "update_lastmod": False
}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 13. Notebook: `nb_p1_finance_sl_save_client_demo1`
Pipeline activity: `nb_finance_sl_client_demo1`

Legacy note: ClientDemoMerge1 parity: unconditional update; pre-reset is commented in the SP. Phone truncation follows SQL SUBSTRING(cltphone, 0, 12).

```python
METHOD_NAME = 'SaveClientDemo1var'
SAVE_CLIENT_DEMO1VAR_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_CLIENT_DEMO1VAR_CONFIG["method_name"]
BRONZE_TABLE = SAVE_CLIENT_DEMO1VAR_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_CLIENT_DEMO1VAR_CONFIG["silver_table"]
MATCH_KEYS = SAVE_CLIENT_DEMO1VAR_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "PrimKey": "long",
    "SiteCode": "string",
    "ClientID": "int",
    "RowChkSum": "int",
    "ClientM4ID": "string",
    "FirstName": "string",
    "MiddleName": "string",
    "LastName": "string",
    "Suffix": "string",
    "DOB": "date",
    "Gender": "string",
    "SSN": "string",
    "email": "string",
    "Size": "int",
    "Address1": "string",
    "Address2": "string",
    "City": "string",
    "State": "string",
    "zip": "string",
    "Phone": "string",
    "preg": "boolean",
    "PregEDC": "date",
    "Marital": "string",
    "EmpStatus": "string",
    "Employer": "string",
    "WorkPhone": "string",
    "Income": "string",
    "Education": "string",
    "Hair": "string",
    "Eye": "string",
    "Height": "string",
    "Weight": "string",
    "Race": "string",
    "Language": "string",
    "County": "string",
    "LastModAt": "timestamp",
    "RowState": "int",
    "isSalesForceSync": "int",
    "SalesForceId": "string",
    "OptinDiversion": "boolean"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowState': lambda df: F.lit(1),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
    'Phone': lambda df: clientdemo_sql_substring_0_12(df, 'Phone'),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = None

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='always',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```

## 14. Notebook: `nb_p1_finance_sl_save_client_demo2`
Pipeline activity: `nb_finance_sl_client_demo2`

Legacy note: ClientDemoMerge2 parity: matched updates only when RowChkSum changes; post-merge RowState/LastModAt sync from Demo1 is preserved.

```python
METHOD_NAME = 'SaveClientDemo2'
SAVE_CLIENT_DEMO2_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_CLIENT_DEMO2_CONFIG["method_name"]
BRONZE_TABLE = SAVE_CLIENT_DEMO2_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_CLIENT_DEMO2_CONFIG["silver_table"]
MATCH_KEYS = SAVE_CLIENT_DEMO2_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "clientID": "int",
    "RowChkSum": "int",
    "Counselor": "string",
    "Status": "string",
    "Prog": "string",
    "DateAdded": "timestamp",
    "Amount": "string",
    "Freq": "string",
    "DOW1": "string",
    "DOW2": "string",
    "NextBill": "timestamp",
    "LastBill": "timestamp",
    "NextTP": "timestamp",
    "PhysTB": "timestamp",
    "Monthly": "boolean",
    "BOTTLES": "smallint",
    "PICPATH": "string",
    "REMARKS": "string",
    "RIN": "string",
    "ETH": "string",
    "Medicaid": "boolean",
    "EnrollDate": "timestamp",
    "BULK": "boolean",
    "STAND": "boolean",
    "SPECIAL": "string",
    "dtLastUA": "string",
    "AMSID": "string",
    "NOCENSUS": "boolean",
    "CHANGEUSER": "string",
    "RepOldClient": "decimal(18,0)",
    "UAWeekly": "timestamp",
    "OptIn": "boolean",
    "credit": "int",
    "CONTTXDT": "date",
    "INS": "string",
    "RISK": "string",
    "Clt3pBack": "string",
    "Clt3pfront": "string",
    "BiWeeklyUA": "boolean",
    "NurseNotes": "string",
    "PANEL": "string",
    "PAYDAY": "string",
    "FingerPrint1": "binary",
    "FingerPrint2": "binary",
    "Clt911Name": "string",
    "Clt911PH": "string",
    "Clt911Relation": "string",
    "HolidayPickup": "boolean",
    "ddapid": "long",
    "ProvClient": "long",
    "ProvClientID": "long",
    "BackFee": "decimal(19,4)",
    "isSalesForceSync": "int",
    "SalesForceId": "string",
    "LastModAt": "timestamp",
    "RowState": "int"
}
TARGET_COLUMNS = list(TARGET_SCHEMA.keys())

TRANSFORMS = {
    'LastModAt': lambda df: current_ts(),
    'RowState': lambda df: F.lit(1),
    'RowChkSum': lambda df: col_or_null(df, 'RowChkSum').cast('int'),
}

UNCHANGED_UPDATE_SET = {}
RESET_RULE = {}
MATCH_KEY_TRANSFORMS = {}
POST_MERGE = sync_clientdemo2_rowstate_from_demo1

merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    configured_target_schema=TARGET_SCHEMA,
    transforms=TRANSFORMS,
    update_strategy='checksum',
    unchanged_update_set=UNCHANGED_UPDATE_SET,
    insert_condition=None,
    reset_rule=RESET_RULE,
    match_key_transforms=MATCH_KEY_TRANSFORMS,
    dedupe_order_columns=['ExtractedAt', 'LastModAt'],
    post_merge=POST_MERGE,
)
```
