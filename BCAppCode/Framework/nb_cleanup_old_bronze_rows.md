# nb_cleanup_old_bronze_rows

Create one reusable Fabric notebook named `nb_cleanup_old_bronze_rows` and paste this cell.

Purpose:

- Reads Bronze taskconfig rows from `bhg_bronze.meta.etlconfig` + `bhg_bronze.meta.taskconfig`.
- Uses `ConfigName` prefix + `TargetName = BR` + optional method list.
- Parses each Bronze task `RequestBody.full_table`.
- Deletes old Bronze rows from every unique Bronze table.
- Cleans all matching Bronze tables, regardless of active/inactive site rows.
- Runs direct Delta `DELETE` without a pre-count scan.
- Works for ETLs with one Bronze table or many Bronze tables.

## Cell 1

```python
import json
import re
from pyspark.sql import functions as F

try:
    p_config_name_prefix
except NameError:
    p_config_name_prefix = ""

try:
    p_target_names_json
except NameError:
    p_target_names_json = '["BR"]'

try:
    p_methods_json
except NameError:
    p_methods_json = "[]"

try:
    p_retention_days
except NameError:
    p_retention_days = "7"

try:
    p_timestamp_columns_json
except NameError:
    p_timestamp_columns_json = '["ExtractedAt","_extracted_at"]'

etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"
bronze_lakehouse = "bhg_bronze"


def parse_json_list(raw):
    if raw is None:
        return []
    if isinstance(raw, (list, tuple, set)):
        return list(raw)
    text = str(raw).strip()
    if not text or text.lower() in ("null", "none"):
        return []
    parsed = json.loads(text)
    if parsed is None:
        return []
    return parsed if isinstance(parsed, list) else [parsed]


def parse_int(raw, default):
    try:
        return int(raw)
    except Exception:
        return default


def parse_body(raw):
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return raw
    text = str(raw).strip()
    if not text:
        return {}
    try:
        parsed = json.loads(text)
        return parsed if isinstance(parsed, dict) else {}
    except Exception:
        return {}


def clean_name(value):
    return str(value or "").strip()


def quote_identifier(full_name):
    parts = [p.strip() for p in str(full_name).split(".") if p.strip()]
    if len(parts) != 3:
        raise Exception(f"Expected a 3-part table name, received: {full_name}")
    for part in parts:
        if not re.match(r"^[A-Za-z_][A-Za-z0-9_]*$", part):
            raise Exception(f"Unsafe table identifier part '{part}' in {full_name}")
    return ".".join([f"`{p}`" for p in parts])


def bronze_full_table(row):
    body = parse_body(row.get("RequestBody") or row.get("request_body"))
    full_table = clean_name(body.get("full_table"))
    if full_table:
        return full_table

    target_schema = clean_name(row.get("TargetSchema"))
    target_table = clean_name(row.get("TargetTable"))
    if target_schema and target_table:
        if target_schema.lower().startswith(f"{bronze_lakehouse.lower()}."):
            return f"{target_schema}.{target_table}"
        return f"{bronze_lakehouse}.{target_schema}.{target_table}"

    return ""


config_name_prefix = clean_name(p_config_name_prefix)
if not config_name_prefix:
    raise Exception("p_config_name_prefix is required.")

target_names = [clean_name(x).upper() for x in parse_json_list(p_target_names_json) if clean_name(x)]
if not target_names:
    target_names = ["BR"]

methods = [clean_name(x).lower() for x in parse_json_list(p_methods_json) if clean_name(x)]
timestamp_candidates = [clean_name(x) for x in parse_json_list(p_timestamp_columns_json) if clean_name(x)]
if not timestamp_candidates:
    timestamp_candidates = ["ExtractedAt", "_extracted_at"]

retention_days = parse_int(p_retention_days, 7)
if retention_days < 1:
    raise Exception(f"p_retention_days must be >= 1. Received: {retention_days}")

etl_df = (
    spark.table(etlconfig_table)
    .where(F.col("ConfigName").startswith(config_name_prefix))
    .where(F.upper(F.col("TargetName")).isin(target_names))
    .select("ConfigId", "ConfigName", "TargetName")
)

task_df = spark.table(taskconfig_table)
joined_df = task_df.join(etl_df, "ConfigId", "inner")

if methods:
    joined_df = joined_df.where(F.lower(F.col("Method")).isin(methods))

rows = [r.asDict(recursive=True) for r in joined_df.collect()]

tables = []
seen = set()
for row in rows:
    table_name = bronze_full_table(row)
    if not table_name:
        continue
    key = table_name.lower()
    if key not in seen:
        seen.add(key)
        tables.append(table_name)

results = []

for table_name in tables:
    if not table_name.lower().startswith(f"{bronze_lakehouse.lower()}."):
        raise Exception(f"Cleanup refused non-Bronze table from taskconfig: {table_name}")

    result = {
        "table_name": table_name,
        "retention_days": retention_days,
        "status": "PENDING",
        "timestamp_column": None,
        "delete_predicate": None,
    }

    if not spark.catalog.tableExists(table_name):
        result["status"] = "SKIPPED_TABLE_NOT_FOUND"
        results.append(result)
        continue

    cols = spark.table(table_name).columns
    timestamp_col = next((c for c in timestamp_candidates if c in cols), None)
    if not timestamp_col:
        result["status"] = "SKIPPED_TIMESTAMP_COLUMN_NOT_FOUND"
        results.append(result)
        continue

    result["timestamp_column"] = timestamp_col
    table_ident = quote_identifier(table_name)
    col_ident = f"`{timestamp_col}`"
    delete_predicate = f"CAST({col_ident} AS DATE) < date_sub(current_date(), {retention_days})"
    result["delete_predicate"] = delete_predicate

    delete_sql = f"""
        DELETE FROM {table_ident}
        WHERE {delete_predicate}
    """
    spark.sql(delete_sql)
    result["status"] = "SUCCESS_DELETE_EXECUTED"

    results.append(result)

if results:
    display(spark.createDataFrame(results))
else:
    print("No Bronze tables found for cleanup.")

payload = {
    "status": "SUCCESS",
    "config_name_prefix": config_name_prefix,
    "target_names": target_names,
    "methods": methods,
    "retention_days": retention_days,
    "tables_checked": len(results),
    "results": results
}

mssparkutils.notebook.exit(json.dumps(payload, default=str, separators=(",", ":")))
```

## Notes Parameters

Use these parameters when attaching to Notes:

| Parameter | Value |
| --- | --- |
| `p_config_name_prefix` | `SAMMS Notes` |
| `p_target_names_json` | `["BR"]` |
| `p_methods_json` | `["3pArnote","3pClaimNote"]` |
| `p_retention_days` | `7` |
| `p_timestamp_columns_json` | `["ExtractedAt","_extracted_at"]` |
