from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window
import json

spark.conf.set("spark.sql.autoBroadcastJoinThreshold", -1)

def to_pascal_column(name):
    if not name:
        return name
    if name[0].isupper():
        return name
    return name[0].upper() + name[1:]


def actual_col(df, wanted, required=False):
    by_lower = {c.lower(): c for c in df.columns}
    found = by_lower.get(wanted.lower())
    if required and not found:
        raise Exception(f"Column {wanted} was not found. Available columns: {df.columns}")
    return found


def align_to_target(src_df, configured_target_columns, configured_target_schema, bronze_source_by_target=None):
    bronze_source_by_target = bronze_source_by_target or {}
    target_schema = {str(k).lower(): str(v) for k, v in (configured_target_schema or {}).items()}
    exprs = []
    for target_col in configured_target_columns:
        source_candidates = [
            bronze_source_by_target.get(target_col),
            target_col,
        ]
        source_col = None
        for candidate in source_candidates:
            if not candidate:
                continue
            source_col = actual_col(src_df, candidate, required=False)
            if source_col:
                break
        expr = col(source_col) if source_col else lit(None)
        target_type = target_schema.get(target_col.lower())
        if target_type:
            expr = expr.cast(target_type)
        exprs.append(expr.alias(target_col))
    return src_df.select(*exprs)


def project_existing_to_target(existing_df):
    df = existing_df
    legacy_site = actual_col(df, "_site_code", required=False)
    if legacy_site and not actual_col(df, "SiteCode", required=False):
        df = df.withColumn("SiteCode", col(legacy_site))
    legacy_mod = actual_col(df, "silver_updated_at", required=False)
    if legacy_mod and not actual_col(df, "LastModAt", required=False):
        df = df.withColumn("LastModAt", col(legacy_mod))
    return align_to_target(
        df,
        TARGET_COLUMNS,
        TARGET_SCHEMA,
        BRONZE_SOURCE_BY_TARGET,
    )

def notebook_exit(payload):
    text = json.dumps(payload, default=str, separators=(",", ":"))
    try:
        mssparkutils.notebook.exit(text)
    except NameError:
        print(text)
        raise SystemExit(text)

def result_payload(method_name, status, rows_read=0, rows_inserted=0, rows_updated=0, rows_skipped=0, message=None, site_results=None):
    body = {
        "method": method_name,
        "layer": "SL",
        "status": status,
        "rows_read": int(rows_read or 0),
        "rows_inserted": int(rows_inserted or 0),
        "rows_updated": int(rows_updated or 0),
        "rows_skipped": int(rows_skipped or 0)
    }
    if message:
        body["message"] = str(message)[:4000]
    if site_results is not None:
        body["site_results"] = site_results
    return {method_name: body}

# Pipeline passes p_ingest_run_id as a parameter.
# The try/except lets you run this manually during development.
try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

try:
    p_bronze_succeeded
except NameError:
    p_bronze_succeeded = "true"

try:
    p_sites_json
except NameError:
    p_sites_json = "[]"

try:
    p_bronze_method_results_json
except NameError:
    p_bronze_method_results_json = "{}"

bronze_had_method_failure = str(p_bronze_succeeded).lower() != "true"

def parse_json_list(raw):
    if not raw:
        return []
    try:
        parsed = json.loads(str(raw))
        return parsed if isinstance(parsed, list) else []
    except Exception:
        return []

def activeSiteCodes_for_method(method_name):
    sites = []
    seen = set()
    for row in parse_json_list(p_sites_json):
        if str(row.get("Method", "")).lower() == method_name.lower():
            site_code = row.get("SiteCode")
            if site_code and site_code not in seen:
                sites.append(str(site_code))
                seen.add(str(site_code))
    return sites

def build_site_results(method_name, successful_sites, failed_message=None):
    active_sites = activeSiteCodes_for_method(method_name)
    successful = {str(site) for site in successful_sites if site}
    results = []
    for site in active_sites:
        if site in successful:
            results.append({"site_code": site, "status": "SUCCESS"})
        elif bronze_had_method_failure:
            results.append({
                "site_code": site,
                "status": "FAILED",
                "failed_stage": "BR",
                "error_message": failed_message or f"{method_name} Bronze copy failed or did not write the site success marker."
            })
        else:
            results.append({"site_code": site, "status": "SUCCESS"})
    return results

# try:
#     p_bronze_succeeded
# except NameError:
#     p_bronze_succeeded = "true"

# if str(p_bronze_succeeded).lower() not in ("true", "1", "yes"):
#     raise Exception(f"Bronze failed for 3pArnote; skipping Silver MERGE for ingest_run_id={p_ingest_run_id}")

try:
    p_taskconfig_json
except NameError:
    p_taskconfig_json = "[]"

try:
    p_method
except NameError:
    p_method = "3pArnote"

def taskconfig_rows(raw):
    if raw is None:
        return []
    text = str(raw).strip()
    if text in ("", "[]", "{}"):
        return []
    parsed = json.loads(text)
    if isinstance(parsed, str):
        parsed = json.loads(parsed)
    if isinstance(parsed, list):
        return parsed
    if isinstance(parsed, dict):
        for key in ("value", "rows", "items", "taskconfig", "tasks"):
            value = parsed.get(key)
            if isinstance(value, list):
                return value
    return []

def row_value(row, name):
    if not isinstance(row, dict):
        return None
    for key in (name, name[:1].lower() + name[1:], name.lower()):
        if key in row:
            return row.get(key)
    return None

def target_from_request_body(row):
    request_body = row_value(row, "RequestBody")
    if not request_body:
        return None, None
    try:
        parsed = json.loads(str(request_body))
        full_table = parsed.get("full_table")
        if full_table:
            parts = str(full_table).split(".")
            if len(parts) >= 3:
                return parts[-2], parts[-1]
    except Exception:
        pass
    return None, None

def resolve_taskconfig_target(target_name_to_match, layer_name, default_schema, default_table):
    rows = taskconfig_rows(p_taskconfig_json)
    for row in rows:
        target_name = row_value(row, "TargetName")
        method = row_value(row, "Method")
        if str(target_name).upper() == str(target_name_to_match).upper() and str(method).lower() == str(p_method).lower():
            target_schema = row_value(row, "TargetSchema")
            target_table = row_value(row, "TargetTable")
            if not target_schema or not target_table:
                body_schema, body_table = target_from_request_body(row)
                target_schema = target_schema or body_schema
                target_table = target_table or body_table
            if not target_schema or not target_table:
                raise Exception(f"{layer_name} taskconfig row for TargetName={target_name_to_match}, Method={p_method} is missing TargetSchema/TargetTable and RequestBody.full_table.")
            return str(target_schema), str(target_table)

    if rows:
        raise Exception(f"No {layer_name} taskconfig row found for TargetName={target_name_to_match}, Method={p_method}.")

    return default_schema, default_table

bronze_schema, bronze_target_table = resolve_taskconfig_target("BR", "Bronze", "Notes", "br_tbl3pArnote")
bronze_table = f"bhg_bronze.{bronze_schema}.{bronze_target_table}"
target_schema, target_table = resolve_taskconfig_target("SL", "Silver", "pats", "tbl_3pARNOTE")
silver_table = f"bhg_silver.{target_schema}.{target_table}"
legacy_silver_table = f"bhg_silver.{target_schema}.sl_{target_table}"
# 3pArnote PascalCase silver schema (bronze names unchanged)
BRONZE_SOURCE_BY_TARGET = {
    "SiteCode": "SiteCode",
    "ArnID": "arnID",
    "ArnLIID": "arnLIID",
    "ArnNOTE": "arnNOTE",
    "ArnUSER": "arnUSER",
    "ArnDATE": "arnDATE",
    "ArnDtRemoved": "arnDtRemoved",
    "ArnStrRemovedReason": "arnStrRemovedReason",
    "ArnStrRemovedUser": "arnStrRemovedUser",
    "Bid": "bid",
    "ArnDBnotes": "arnDBnotes",
    "GlobalBatchId": "globalBatchId",
    "RowChkSum": "RowChkSum",
    "LastModAt": "LastModAt",
    "RowState": "RowState"
}

TARGET_SCHEMA = {
    "SiteCode": "string",
    "ArnID": "int",
    "ArnLIID": "int",
    "ArnNOTE": "string",
    "ArnUSER": "string",
    "ArnDATE": "timestamp",
    "ArnDtRemoved": "timestamp",
    "ArnStrRemovedReason": "string",
    "ArnStrRemovedUser": "string",
    "Bid": "int",
    "ArnDBnotes": "string",
    "GlobalBatchId": "long",
    "RowChkSum": "int",
    "LastModAt": "timestamp",
    "RowState": "boolean"
}

TARGET_COLUMNS = list(TARGET_SCHEMA.keys())
final_columns = TARGET_COLUMNS

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

# Read only rows from THIS pipeline run.
bronze_df = spark.table(bronze_table).where(col("IngestRunId") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

successful_bronze_sites = [
    row["SiteCode"]
    for row in (
        bronze_df
        .where(col("SiteCode").isNotNull())
        .select("SiteCode")
        .distinct()
        .collect()
    )
]
site_results = build_site_results("3pArnote", successful_bronze_sites)

if bronze_count == 0:
    notebook_exit(result_payload(
        "3pArnote",
        "SKIPPED" if bronze_had_method_failure else "SUCCESS",
        rows_read=0,
        message=f"No successful Bronze sites found for 3pArnote ingest_run_id = {p_ingest_run_id}",
        site_results=site_results
    ))

# Deduplicate within current run.
# Business key = SiteCode + arnID   (mirrors Azure PK: SiteCode, ArnId)
# If the same record appears twice (e.g. due to a retry), keep the latest extraction.
w = Window.partitionBy("SiteCode", "arnID").orderBy(col("ExtractedAt").desc())

src_work_df = (
    bronze_df
    .where(col("SiteCode").isNotNull() & col("arnID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # RowState logic: ARNote treats every returned SAMMS row as active.
    # There is no pre-reset or soft-delete condition for ARNote.
    .withColumn("RowState", lit(True))
    .withColumn("LastModAt", current_timestamp())
)

src_df = align_to_target(
    src_work_df,
    TARGET_COLUMNS,
    TARGET_SCHEMA,
    BRONZE_SOURCE_BY_TARGET,
)

src_df.createOrReplaceTempView("vw_arnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ARNote Silver: {src_count}")

# First-ever run: create Silver table
created_silver_table = False
if not spark.catalog.tableExists(silver_table):
    if spark.catalog.tableExists(legacy_silver_table):
        legacy_df = spark.table(legacy_silver_table)
        migrated_df = project_existing_to_target(legacy_df).cache()
        migrated_count = migrated_df.count()
        (
            migrated_df
            .write
            .format("delta")
            .mode("overwrite")
            .option("overwriteSchema", "true")
            .saveAsTable(silver_table)
        )
        migrated_df.unpersist()
        print(f"Migrated ARNote Silver table from {legacy_silver_table} to {silver_table}. Rows preserved: {migrated_count}")
    else:
        (
            src_df
            .write
            .format("delta")
            .mode("overwrite")
            .saveAsTable(silver_table)
        )
        created_silver_table = True
        print(f"Created ARNote Silver table and inserted rows: {src_count}")
else:
    print(f"Silver table already exists: {silver_table}")

from delta.tables import DeltaTable

try:
    silver_table
except NameError:
    target_schema, target_table = resolve_taskconfig_target("SL", "Silver", "pats", "tbl_3pARNOTE")
    silver_table = f"bhg_silver.{target_schema}.{target_table}"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

# One-time normalization for Silver tables created before Silver became the final layer.
# This removes internal Fabric columns and keeps only the former Gold/reporting columns.
existing_cols = spark.table(silver_table).columns
if existing_cols != final_columns:
    existing_df = spark.table(silver_table)
    normalized_df = project_existing_to_target(existing_df).cache()
    normalized_count = normalized_df.count()
    (
        normalized_df
        .write
        .format("delta")
        .mode("overwrite")
        .option("overwriteSchema", "true")
        .saveAsTable(silver_table)
    )
    normalized_df.unpersist()
    print(f"Normalized ARNote Silver table to final schema. Rows preserved: {normalized_count}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = final_columns
update_set = {c: f"src.{c}" for c in src_cols}
insert_values = {c: f"src.{c}" for c in src_cols}

match_keys = ["SiteCode", "ArnID"]
if created_silver_table:
    rows_inserted = src_count
    rows_updated = 0
else:
    target_keys = spark.table(silver_table).select(*match_keys).dropDuplicates()
    rows_inserted = src_df.join(target_keys, match_keys, "left_anti").count()
    rows_updated = (
        src_df.alias("src")
        .join(spark.table(silver_table).alias("tgt"), match_keys, "inner")
        .where("""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """)
        .count()
    )

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # Merge key: clinic + note ID (mirrors Azure PK: SiteCode + ArnId)
        "tgt.SiteCode = src.SiteCode AND tgt.ArnID = src.ArnID"
    )

    # CASE 1: Record exists AND data changed: full update
    # Full update of all data columns
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: New record: insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ARNote Silver MERGE completed successfully.")
notebook_exit(result_payload(
    "3pArnote",
    "SUCCESS",
    rows_read=src_count,
    rows_inserted=rows_inserted,
    rows_updated=rows_updated,
    site_results=site_results
))
