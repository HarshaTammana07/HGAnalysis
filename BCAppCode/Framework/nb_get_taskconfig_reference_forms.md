import json
from pyspark.sql import functions as F

try:
    p_config_ids_json
except NameError:
    p_config_ids_json = "[]"

try:
    p_config_name_prefix
except NameError:
    p_config_name_prefix = ""

try:
    p_target_names_json
except NameError:
    p_target_names_json = "[]"

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
    p_only_active_config
except NameError:
    p_only_active_config = "true"

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
etlconfig_table = "bhg_bronze.meta.etlconfig"


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


def non_blank_expr(column_expr):
    return column_expr.isNotNull() & (F.length(F.trim(column_expr.cast("string"))) > 0)


config_ids = [int(x) for x in parse_json_list(p_config_ids_json, "p_config_ids_json")]
config_name_prefix = str(p_config_name_prefix or "").strip()
target_names = [
    str(x).strip().upper()
    for x in parse_json_list(p_target_names_json, "p_target_names_json")
    if str(x).strip()
]
methods = [str(x).strip().lower() for x in parse_json_list(p_methods_json, "p_methods_json") if str(x).strip()]
target_tables = [
    str(x).strip().lower()
    for x in parse_json_list(p_target_tables_json, "p_target_tables_json")
    if str(x).strip()
]

only_active = parse_bool(p_only_active, True)
only_active_config = parse_bool(p_only_active_config, True)
require_site = parse_bool(p_require_site, True)
require_database = parse_bool(p_require_database, True)
require_source_table = parse_bool(p_require_source_table, False)

tc = spark.table(taskconfig_table).alias("tc")
ec = spark.table(etlconfig_table).alias("ec")

use_etl_filter = bool(config_name_prefix or target_names)

if config_ids:
    tc = tc.where(F.col("ConfigId").isin(config_ids))

if only_active:
    tc = tc.where(F.col("IsActive") == 1)

if methods:
    if "Method" not in tc.columns:
        raise Exception(f"Method column not found in {taskconfig_table}")
    tc = tc.where(F.lower(F.col("Method")).isin(methods))

if target_tables:
    if "TargetTable" not in tc.columns:
        raise Exception(f"TargetTable column not found in {taskconfig_table}")
    tc = tc.where(F.lower(F.col("TargetTable")).isin(target_tables))

if require_site:
    if "SiteCode" not in tc.columns:
        raise Exception(f"Required column not found in {taskconfig_table}: SiteCode")
    tc = tc.where(non_blank_expr(F.col("SiteCode")))

if require_database:
    if "DataBaseName" not in tc.columns:
        raise Exception(f"Required column not found in {taskconfig_table}: DataBaseName")
    tc = tc.where(non_blank_expr(F.col("DataBaseName")))

if require_source_table:
    if "SourceTable" not in tc.columns:
        raise Exception(f"Required column not found in {taskconfig_table}: SourceTable")
    tc = tc.where(non_blank_expr(F.col("SourceTable")))

if config_name_prefix:
    ec = ec.where(F.lower(F.col("ConfigName")).startswith(config_name_prefix.lower()))

if target_names:
    ec = ec.where(F.upper(F.col("TargetName")).isin(target_names))

if only_active_config:
    ec = ec.where(F.col("IsActive") == 1)

if config_ids:
    ec = ec.where(F.col("ConfigId").isin(config_ids))

etl_cols = [
    F.col("ConfigId").alias("EtlConfigId"),
    F.col("ConfigName").alias("ConfigName"),
    F.col("PipelineName").alias("PipelineName"),
    F.col("PipelinePath").alias("PipelinePath"),
    F.col("TargetName").alias("TargetName"),
    F.col("ExecutionSequence").alias("LayerExecutionSequence"),
    F.col("IsActive").alias("ConfigIsActive"),
]
ec_meta = ec.select(*etl_cols).alias("ec")

join_type = "inner" if use_etl_filter else "left"
df = tc.alias("tc").join(
    ec_meta,
    F.col("tc.ConfigId") == F.col("ec.EtlConfigId"),
    join_type,
)

if use_etl_filter:
    config_count = ec_meta.select("EtlConfigId").distinct().count()
    if config_count == 0:
        raise Exception(
            "No etlconfig rows matched the supplied filters. "
            f"ConfigNamePrefix='{config_name_prefix}', TargetNames={target_names}, "
            f"OnlyActiveConfig={only_active_config}"
        )

select_exprs = [
    F.col("tc.TaskConfigId").alias("TaskConfigId"),
    F.col("tc.ConfigId").alias("ConfigId"),
    F.col("ec.ConfigName").alias("ConfigName"),
    F.col("ec.TargetName").alias("TargetName"),
    F.col("ec.PipelineName").alias("PipelineName"),
    F.col("ec.PipelinePath").alias("PipelinePath"),
    F.col("ec.LayerExecutionSequence").alias("LayerExecutionSequence"),
    F.col("ec.ConfigIsActive").alias("ConfigIsActive"),
    F.col("tc.TaskName").alias("TaskName"),
    F.col("tc.Endpoint").alias("Endpoint"),
    F.col("tc.Method").alias("Method"),
    F.col("tc.SourceTable").alias("SourceTable"),
    F.col("tc.PaginationEnabled").alias("PaginationEnabled"),
    F.col("tc.PaginationParam").alias("PaginationParam"),
    F.col("tc.LoadType").alias("LoadType"),
    F.col("tc.IsIncremental").alias("IsIncremental"),
    F.col("tc.WatermarkColumn").alias("WatermarkColumn"),
    F.col("tc.LookbackDays").alias("LookbackDays"),
    F.col("tc.TargetSchema").alias("TargetSchema"),
    F.col("tc.TargetTable").alias("TargetTable"),
    F.col("tc.TargetPath").alias("TargetPath"),
    F.col("tc.ExecutionOrder").alias("ExecutionOrder"),
    F.col("tc.RetryCount").alias("RetryCount"),
    F.col("tc.TimeoutSeconds").alias("TimeoutSeconds"),
    F.col("tc.RequestBody").alias("RequestBody"),
    F.col("tc.DependencyTaskConfigId").alias("DependencyTaskConfigId"),
    F.col("tc.SiteCode").alias("SiteCode"),
    F.col("tc.DataBaseName").alias("DataBaseName"),
    F.col("tc.SiteName").alias("SiteName"),
    F.col("tc.IsActive").alias("IsActive"),
    F.col("tc.CreatedAt").alias("CreatedAt"),
    F.col("tc.CreatedBy").alias("CreatedBy"),
    F.col("tc.ModifiedAt").alias("ModifiedAt"),
    F.col("tc.ModifiedBy").alias("ModifiedBy"),
    F.col("tc.AuthType").alias("AuthType"),
]

result_df = df.select(*select_exprs).orderBy(
    F.col("LayerExecutionSequence").asc_nulls_last(),
    F.col("TargetName").asc_nulls_last(),
    F.col("ConfigId").asc(),
    F.col("Method").asc_nulls_last(),
    F.col("SiteCode").asc_nulls_last(),
    F.col("TaskConfigId").asc(),
)

row_count = result_df.count()
if row_count == 0:
    raise Exception(
        "No active taskconfig rows found for the supplied filters. "
        f"ConfigIds={config_ids}, ConfigNamePrefix='{config_name_prefix}', "
        f"TargetNames={target_names}, Methods={methods}, TargetTables={target_tables}"
    )

if row_count > 5000:
    raise Exception(f"Taskconfig result has {row_count} rows; Fabric Lookup/ForEach should stay under 5000 rows.")

rows = [row.asDict(recursive=True) for row in result_df.collect()]
display(result_df)

mssparkutils.notebook.exit(json.dumps(rows, default=str))
