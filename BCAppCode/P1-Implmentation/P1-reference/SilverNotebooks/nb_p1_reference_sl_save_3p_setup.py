# Notebook: nb_p1_reference_sl_save_3p_setup
# Method: Save3pSetup
# Cell 1: paste/import nb_p1_reference_sl_common_cell1.py
# Cell 2: method-specific code below

import json
import traceback
from delta.tables import DeltaTable
from pyspark.sql import functions as F

try:
    from notebookutils.mssparkutils.handlers.notebookHandler import NotebookExit
except Exception:
    NotebookExit = None

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
REFERENCE_CONFIG_NAME_PREFIX = "SAMMS P1 Reference"
REFERENCE_SILVER_TARGET_NAME = "SL"


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
    global_result = results.get("P1Reference")
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


def to_pascal_column(name):
    if not name:
        return name
    if name[0].isupper():
        return name
    return name[0].upper() + name[1:]


def pascalize_keys(keys):
    return [to_pascal_column(key) for key in keys]


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


def resolve_reference_config_id(target_name):
    target = str(target_name or "").strip().upper()
    rows = (
        spark.table(ETLCONFIG_TABLE)
        .where(F.lower(F.col("ConfigName")).startswith(REFERENCE_CONFIG_NAME_PREFIX.lower()))
        .where(F.upper(F.col("TargetName")) == F.lit(target))
        .where(F.col("IsActive") == F.lit(1))
        .select("ConfigId", "ConfigName", "TargetName")
        .orderBy(F.col("ExecutionSequence").asc(), F.col("ConfigId").asc())
        .limit(2)
        .collect()
    )
    if not rows:
        raise ValueError(
            f"No active etlconfig row found for prefix={REFERENCE_CONFIG_NAME_PREFIX}, TargetName={target}."
        )
    if len(rows) > 1:
        raise ValueError(
            f"Expected one active etlconfig row for prefix={REFERENCE_CONFIG_NAME_PREFIX}, TargetName={target}; found {len(rows)}."
        )
    return int(row_value(rows[0], "ConfigId"))


def resolve_reference_silver_metadata(method_name=None, silver_config_id=None):
    method = str(method_name or p_method_name or "").strip()
    if not method:
        raise ValueError("p_method_name is required to resolve P1 Reference silver taskconfig metadata.")

    silver_config_id = int(silver_config_id or resolve_reference_config_id(REFERENCE_SILVER_TARGET_NAME))

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
        raise ValueError(f"No active P1 Reference silver taskconfig row found for ConfigId={silver_config_id}, Method={method}.")
    if len(rows) > 1:
        raise ValueError(f"Expected one active P1 Reference silver taskconfig row for Method={method}, found {len(rows)}.")

    task_row = rows[0]
    request_body = parse_request_body(row_value(task_row, "RequestBody"))
    bronze_table = row_value(task_row, "SourceTable")
    silver_table = request_body.get("full_table") or row_value(task_row, "TargetPath")
    match_keys = pascalize_keys(dq_keys_from_taskconfig(task_row))
    method_from_config = row_value(task_row, "Method") or method

    if not bronze_table:
        raise ValueError(f"Silver taskconfig SourceTable is missing for Method={method_from_config}.")
    if not silver_table:
        raise ValueError(f"Silver taskconfig TargetPath/RequestBody.full_table is missing for Method={method_from_config}.")

    return {
        "method_name": str(method_from_config),
        "bronze_table": str(bronze_table),
        "silver_table": str(silver_table),
        "match_keys": match_keys,
        "task_config_id": row_value(task_row, "TaskConfigId"),
        "config_id": row_value(task_row, "ConfigId"),
    }


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


def transform_expr(transform, src_df):
    return transform(src_df) if callable(transform) else transform


def align_to_target(src_df, silver_table, configured_target_columns=None, transforms=None, drop_columns=None):
    transforms = transforms or {}
    cols = target_columns_for(src_df, silver_table, configured_target_columns, transforms, drop_columns)
    target_schema = {}
    if table_exists(silver_table):
        target_schema = {f.name: f.dataType for f in spark.table(silver_table).schema.fields}

    exprs = []
    for target_col in cols:
        if target_col in transforms:
            expr = transform_expr(transforms[target_col], src_df)
        else:
            source_col = actual_col(src_df, target_col, required=False)
            expr = F.col(source_col) if source_col else F.lit(None).cast("string")
        if target_col in target_schema:
            expr = expr.cast(target_schema[target_col])
        exprs.append(expr.alias(target_col))
    return src_df.select(*exprs)


def null_safe_condition(keys):
    return " AND ".join([f"target.`{k}` <=> source.`{k}`" for k in keys])


def checksum_changed_condition(checksum_col):
    return f"NOT (target.`{checksum_col}` <=> source.`{checksum_col}`)"


def checksum_same_condition(checksum_col):
    return f"target.`{checksum_col}` <=> source.`{checksum_col}`"


def pre_reset_sites(silver_table, source_df, flag_col, active_value, inactive_value):
    if not table_exists(silver_table) or "SiteCode" not in source_df.columns:
        return 0
    sites = [r[0] for r in source_df.select("SiteCode").where(F.col("SiteCode").isNotNull()).distinct().collect()]
    if not sites:
        return 0
    before = spark.table(silver_table).where((F.col("SiteCode").isin(sites)) & (F.col(flag_col) == F.lit(active_value))).count()
    if before:
        DeltaTable.forName(spark, silver_table).update(
            condition=(F.col("SiteCode").isin(sites)) & (F.col(flag_col) == F.lit(active_value)),
            set={flag_col: F.lit(inactive_value)},
        )
    return before


def apply_legacy_services_insert_scope(source_df, silver_table, match_keys):
    # Legacy SaveServices does not insert a brand-new sID when that site already has service rows.
    if not table_exists(silver_table) or "SiteCode" not in source_df.columns:
        return source_df, 0

    sites = [r[0] for r in source_df.select("SiteCode").where(F.col("SiteCode").isNotNull()).distinct().collect()]
    if not sites:
        return source_df, 0

    target_df = spark.table(silver_table)
    existing_sites = target_df.where(F.col("SiteCode").isin(sites)).select("SiteCode").distinct().withColumn("_site_has_services", F.lit(1))
    target_keys = target_df.select(*match_keys).dropDuplicates().withColumn("_matched_service_key", F.lit(1))

    scoped = source_df.join(existing_sites, "SiteCode", "left").join(target_keys, match_keys, "left")
    skipped = scoped.where(F.col("_site_has_services").isNotNull() & F.col("_matched_service_key").isNull()).count()
    allowed = scoped.where(F.col("_site_has_services").isNull() | F.col("_matched_service_key").isNotNull())
    return allowed.drop("_site_has_services", "_matched_service_key"), skipped


def merge_to_silver(
    method_name,
    bronze_table,
    silver_table,
    match_keys,
    configured_target_columns=None,
    transforms=None,
    drop_columns=None,
    checksum_col=None,
    same_checksum_update_columns=None,
    pre_reset=None,
    insert_only_columns=None,
    preserve_legacy_services_insert_scope=False,
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
            failed_message=f"{method_name} Bronze copy failed or did not write rows for this site."
        )

        if bronze_had_method_failure:
            if not successful_sites:
                notebook_exit(result_payload(
                    method_name,
                    "SKIPPED",
                    rows_read=0,
                    message="All Bronze sites failed or no successful Bronze rows were found, so silver was skipped.",
                    site_results=site_results
                ))
            if bronze_site_col:
                bronze_df = bronze_df.where(F.col(bronze_site_col).isin(successful_sites))

        rows_read = bronze_df.count()
        if rows_read == 0:
            if not table_exists(silver_table):
                empty_df = align_to_target(bronze_df, silver_table, configured_target_columns, transforms, drop_columns)
                for key in match_keys:
                    actual_col(empty_df, key, required=True)
                empty_df.limit(0).write.format("delta").mode("overwrite").saveAsTable(silver_table)
            notebook_exit(result_payload(method_name, "SUCCESS", rows_read=0, message="No bronze rows found for this ingest run.", site_results=site_results))

        silver_df = align_to_target(bronze_df, silver_table, configured_target_columns, transforms, drop_columns)
        resolved_keys = []
        for key in match_keys:
            resolved = actual_col(silver_df, key, required=True)
            resolved_keys.append(resolved)
            silver_df = silver_df.where(F.col(resolved).isNotNull())
        silver_df = silver_df.dropDuplicates(resolved_keys)

        target_exists = table_exists(silver_table)
        if pre_reset and target_exists:
            rows_skipped += pre_reset_sites(
                silver_table,
                silver_df,
                pre_reset["flag_col"],
                pre_reset["active_value"],
                pre_reset["inactive_value"],
            )

        if preserve_legacy_services_insert_scope:
            silver_df, legacy_skipped = apply_legacy_services_insert_scope(silver_df, silver_table, resolved_keys)
            rows_skipped += legacy_skipped

        rows_after_scope = silver_df.count()
        if rows_after_scope == 0:
            if not target_exists:
                silver_df.limit(0).write.format("delta").mode("overwrite").saveAsTable(silver_table)
            notebook_exit(result_payload(method_name, "SUCCESS", rows_read=rows_read, rows_skipped=rows_skipped, message="No rows remained after legacy scope/key rules.", site_results=site_results))

        if not target_exists:
            silver_df.write.format("delta").mode("overwrite").saveAsTable(silver_table)
            rows_inserted = rows_after_scope
            notebook_exit(result_payload(method_name, "SUCCESS", rows_read=rows_read, rows_inserted=rows_inserted, rows_skipped=rows_skipped, site_results=site_results))

        target_keys = spark.table(silver_table).select(*[actual_col(spark.table(silver_table), key, required=True) for key in match_keys]).dropDuplicates()
        join_keys = [actual_col(silver_df, key, required=True) for key in match_keys]
        target_join_keys = [actual_col(spark.table(silver_table), key, required=True) for key in match_keys]
        rows_inserted = silver_df.join(target_keys, join_keys, "left_anti").count()
        rows_updated = silver_df.join(target_keys, join_keys, "inner").count()

        insert_only = set(insert_only_columns or [])
        update_columns = [c for c in silver_df.columns if c not in insert_only]
        update_all = {c: f"source.`{c}`" for c in update_columns}
        insert_all = {c: f"source.`{c}`" for c in silver_df.columns}

        merge_builder = DeltaTable.forName(spark, silver_table).alias("target").merge(
            silver_df.alias("source"),
            null_safe_condition(match_keys),
        )

        if checksum_col and same_checksum_update_columns:
            same_update = {c: f"source.`{c}`" for c in same_checksum_update_columns}
            merge_builder = merge_builder.whenMatchedUpdate(
                condition=checksum_changed_condition(checksum_col),
                set=update_all,
            ).whenMatchedUpdate(
                condition=checksum_same_condition(checksum_col),
                set=same_update,
            )
        elif checksum_col:
            merge_builder = merge_builder.whenMatchedUpdate(
                condition=checksum_changed_condition(checksum_col),
                set=update_all,
            )
        else:
            merge_builder = merge_builder.whenMatchedUpdate(set=update_all)

        merge_builder.whenNotMatchedInsert(values=insert_all).execute()
        notebook_exit(result_payload(method_name, "SUCCESS", rows_read=rows_read, rows_inserted=rows_inserted, rows_updated=rows_updated, rows_skipped=rows_skipped, site_results=site_results))

    except Exception as ex:
        if is_notebook_exit(ex):
            raise
        notebook_exit(result_payload(method_name, "FAILED", rows_read=rows_read, rows_inserted=rows_inserted, rows_updated=rows_updated, rows_skipped=rows_skipped, message=traceback.format_exc(), site_results=site_results))

# Cell 2
SAVE_3P_SETUP_CONFIG = resolve_reference_silver_metadata()
METHOD_NAME = SAVE_3P_SETUP_CONFIG["method_name"]
BRONZE_TABLE = SAVE_3P_SETUP_CONFIG["bronze_table"]
SILVER_TABLE = SAVE_3P_SETUP_CONFIG["silver_table"]
MATCH_KEYS = SAVE_3P_SETUP_CONFIG["match_keys"]
TARGET_COLUMNS = [
    'SiteCode', 'PID', 'Clinic', 'Address', 'State', 'Zip', 'NPI', 'TaxID', 'Medicaid', 'City',
    'DRlname', 'DRfname', 'DRnpi', 'ProviderAddress', 'ProviderCity', 'ProviderName', 'ProviderPhone',
    'ProviderState', 'ProviderZip', 'SiteID', 'Clia', 'StrDBNotes', 'ProviderDesc', 'BlHasPreloader',
    'IndividualNPI', 'Taxonomy', 'SFTPUN', 'SFTPPW', 'RowChkSum', 'LastModAt'
]
TRANSFORMS = {
    "SiteID": lambda df: F.coalesce(col_or_null(df, "SiteID").cast("int"), F.lit(-1)),
    "BlHasPreloader": lambda df: F.coalesce(col_or_null(df, "BlHasPreloader").cast("boolean"), F.lit(False)),
    "IndividualNPI": lambda df: F.coalesce(col_or_null(df, "IndividualNPI").cast("boolean"), F.lit(False)),
    "LastModAt": lambda df: F.current_timestamp(),
}

# Save3pSetup C# behavior: match by site-scoped pID, update only when RowChkSum changes.
merge_to_silver(
    method_name=METHOD_NAME,
    bronze_table=BRONZE_TABLE,
    silver_table=SILVER_TABLE,
    match_keys=MATCH_KEYS,
    configured_target_columns=TARGET_COLUMNS,
    transforms=TRANSFORMS,
    checksum_col="RowChkSum",
)
