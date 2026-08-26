# Notebook: nb_p1_finance_sl_save_fmp
# Pipeline activity: nb_finance_sl_fmp
# Cell 1: paste/import nb_p1_finance_sl_common_cell1.py
# Cell 2: method-specific code below

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
            f"AND year(`BillDate`) >= {int(min_year)} "
            f"AND `BillDate` <= date_add(date'{max_end.isoformat()}', 15)"
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
            filter_sql = f"year(TpcCreatedDate) >= {int(min_year)}" if min_year else None
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
    demo2 = spark.table(silver_table).select("SiteCode", "ClientID", "RowState")
    changes = (
        demo2.alias("d2")
        .join(demo1.alias("d1"), (F.col("d2.SiteCode") == F.col("d1.SiteCode")) & (F.col("d2.ClientID") == F.col("d1.ClientID")), "inner")
        .where(~F.col("d2.RowState").eqNullSafe(F.col("d1.RowState")))
        .select(
            F.col("d1.SiteCode").alias("SiteCode"),
            F.col("d1.ClientID").alias("ClientID"),
            F.col("d1.RowState").alias("RowState"),
            F.col("d1.LastModAt").alias("LastModAt"),
        )
    )
    if changes.limit(1).count() == 0:
        return
    DeltaTable.forName(spark, silver_table).alias("target").merge(
        changes.alias("source"),
        "target.`SiteCode` <=> source.`SiteCode` AND target.`ClientID` <=> source.`ClientID`",
    ).whenMatchedUpdate(set={
        "RowState": "source.`RowState`",
        "LastModAt": "source.`LastModAt`",
    }).execute()

# Cell 2
METHOD_NAME = 'SaveFmp'
SAVE_FMP_CONFIG = resolve_finance_silver_metadata(METHOD_NAME)
METHOD_NAME = SAVE_FMP_CONFIG["method_name"]
BRONZE_TABLE = SAVE_FMP_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_FMP_CONFIG["silver_table"]
MATCH_KEYS = SAVE_FMP_CONFIG["match_keys"]
TARGET_SCHEMA = {
    "SiteCode": "string",
    "FmpID": "int",
    "FmpLngClt": "int",
    "FmpDtStart": "timestamp",
    "FmpDtProjEnd": "timestamp",
    "FmpDtEnd": "timestamp",
    "FmpIntRate": "int",
    "FmpStrReason": "string",
    "FmpStrDesc": "string",
    "FmpDtAdded": "timestamp",
    "FmpStrUserAdded": "string",
    "FmpDtEnded": "timestamp",
    "FmpStrUserEnded": "string",
    "FmPENDTEXT": "string",
    "AtriskTYPE": "string",
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
