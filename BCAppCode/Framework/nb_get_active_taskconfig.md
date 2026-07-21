# nb_get_active_taskconfig

Create a Fabric notebook named `nb_get_active_taskconfig` and paste this cell.

Purpose:

- Reads `bhg_bronze.meta.taskconfig` inside Spark.
- Filters to the requested ConfigId/method/table set.
- Returns only small runtime columns through `mssparkutils.notebook.exit(...)`.
- Avoids Fabric Lookup's 4 MB output limit caused by reading wide `taskconfig` rows such as `RequestBody`.

Use this notebook before parent-pipeline site/method filters. Downstream activities should read:

```text
@json(activity('nb_get_active_taskconfig').output.result.exitValue)
```

## Cell 1

```python
import json
from pyspark.sql import functions as F

try:
   p_config_ids_json
except NameError:
   p_config_ids_json = "[]"

try:
   p_methods_json
except NameError:
   p_methods_json = "[]"

try:
   p_target_tables_json
except NameError:
   p_target_tables_json = "[]"

try:
   p_only_active
except NameError:
   p_only_active = "true"

try:
   p_require_site
except NameError:
   p_require_site = "true"

try:
   p_require_database
except NameError:
   p_require_database = "true"

try:
   p_require_source_table
except NameError:
   p_require_source_table = "false"

taskconfig_table = "bhg_bronze.meta.taskconfig"


def parse_json_list(raw, name):
   if raw is None:
       return []
   if isinstance(raw, (list, tuple, set)):
       return list(raw)

   raw_text = str(raw).strip()
   if raw_text == "" or raw_text.lower() in ("null", "none"):
       return []

   parsed = json.loads(raw_text)
   if parsed is None:
       return []
   if isinstance(parsed, list):
       return parsed
   return [parsed]


def parse_bool(raw, default=False):
   if raw is None:
       return default
   if isinstance(raw, bool):
       return raw
   return str(raw).strip().lower() in ("1", "true", "yes", "y")


def require_non_blank(df, column_name):
   if column_name not in df.columns:
       raise Exception(f"Required column not found in {taskconfig_table}: {column_name}")
   return df.where(F.col(column_name).isNotNull() & (F.length(F.trim(F.col(column_name).cast("string"))) > 0))


config_ids = [int(x) for x in parse_json_list(p_config_ids_json, "p_config_ids_json")]
methods = [str(x).strip().lower() for x in parse_json_list(p_methods_json, "p_methods_json") if str(x).strip()]
target_tables = [str(x).strip().lower() for x in parse_json_list(p_target_tables_json, "p_target_tables_json") if str(x).strip()]

only_active = parse_bool(p_only_active, True)
require_site = parse_bool(p_require_site, True)
require_database = parse_bool(p_require_database, True)
require_source_table = parse_bool(p_require_source_table, False)

df = spark.table(taskconfig_table)

if config_ids:
   df = df.where(F.col("ConfigId").isin(config_ids))

if only_active:
   df = df.where(F.col("IsActive") == 1)

if methods:
   if "Method" not in df.columns:
       raise Exception(f"Method column not found in {taskconfig_table}")
   df = df.where(F.lower(F.col("Method")).isin(methods))

if target_tables:
   if "TargetTable" not in df.columns:
       raise Exception(f"TargetTable column not found in {taskconfig_table}")
   df = df.where(F.lower(F.col("TargetTable")).isin(target_tables))

if require_site:
   df = require_non_blank(df, "SiteCode")

if require_database:
   df = require_non_blank(df, "DataBaseName")

if require_source_table:
   df = require_non_blank(df, "SourceTable")

preferred_columns = [
   "TaskConfigId",
   "ConfigId",
   "TaskName",
   "Endpoint",
   "Method",
   "SourceTable",
   "LoadType",
   "IsIncremental",
   "WatermarkColumn",
   "LookbackDays",
   "TargetSchema",
   "TargetTable",
   "TargetPath",
   "SiteCode",
   "DataBaseName",
   "IsActive"
]

available_columns = [c for c in preferred_columns if c in df.columns]
if not available_columns:
   raise Exception(f"No runtime columns were available from {taskconfig_table}")

result_df = df.select(*available_columns).orderBy(
   *[c for c in ["ConfigId", "Method", "SiteCode", "TaskConfigId"] if c in available_columns]
)

row_count = result_df.count()
if row_count == 0:
   raise Exception(
       "No active taskconfig rows found for the supplied filters. "
       f"ConfigIds={config_ids}, Methods={methods}, TargetTables={target_tables}"
   )

if row_count > 5000:
   raise Exception(f"Taskconfig result has {row_count} rows; Fabric Lookup/ForEach should stay under 5000 rows.")

rows = [row.asDict(recursive=True) for row in result_df.collect()]
display(result_df)

mssparkutils.notebook.exit(json.dumps(rows, default=str))
```

## Darts Parameters

Use these parameters for DartsSrv Bronze site rows:

| Parameter | Value |
| --- | --- |
| `p_config_ids_json` | `[25]` |
| `p_methods_json` | `["DartsSrv"]` |
| `p_only_active` | `true` |
| `p_require_site` | `true` |
| `p_require_database` | `true` |
| `p_require_source_table` | `true` |

The Darts parent filter should use:

```text
@json(activity('nb_get_darts_taskconfig').output.result.exitValue)
```

