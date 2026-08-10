from pathlib import Path
import ast
import copy
import csv
import json
import re
from collections import defaultdict


BASE_PATH = Path("P1-Implmentation/P1-Finance/pl_p1_finance.txt")
TASKCONFIG_PATH = Path("P1-Implmentation/P1-Finance/finance_module_taskconfig_pyspark.py")
SCHEMA_PATH = Path("P1-Implmentation/P1-Finance/columnsanddatatypesFinance.txt4")
MAP_PATH = Path("P1-Implmentation/P1-Finance/Csvs/finance_vw_MapSrc2Dsn.csv")
ACTIONS_PATH = Path("P1-Implmentation/P1-Finance/Csvs/finance_vw_MapActions.csv")

SLUG_BY_METHOD = {
    "SaveBills": "bills",
    "SaveAuths": "pbi3_pay_auth",
    "SaveAuthBillsub": "vw3p_bill_sub",
    "SaveFmp": "fmp",
    "SavePayerCltHistory": "payer_clt_history",
    "SaveFinancialHardshipApplication": "financial_hardship_application",
    "Save3pElig": "3p_elig",
    "SaveClaimLineItem": "claim_line_item",
    "SaveClaimLineItemActivity": "claim_line_item_activity",
    "SaveClaims": "claims",
    "SavePayerClient": "payer_client",
    "SaveTblDiags": "tbldiag10",
    "SaveClientDemo1var": "client_demo1",
    "SaveClientDemo2": "client_demo2",
}

NOTEBOOK_ACTIVITY_BY_METHOD = {
    "SaveBills": "nb_sl_bills",
    "SaveAuths": "nb_sl_auths",
    "SaveAuthBillsub": "nb_sl_bill_sub",
    "SaveFmp": "nb_sl_fmp",
    "SavePayerCltHistory": "nb_sl_payer_hist",
    "SaveFinancialHardshipApplication": "nb_sl_fha",
    "Save3pElig": "nb_sl_elig",
    "SaveClaimLineItem": "nb_sl_cli",
    "SaveClaimLineItemActivity": "nb_sl_lia",
    "SaveClaims": "nb_sl_claims",
    "SavePayerClient": "nb_sl_payer_clt",
    "SaveTblDiags": "nb_sl_diag10",
    "SaveClientDemo1var": "nb_sl_demo1",
    "SaveClientDemo2": "nb_sl_demo2",
}

NOTEBOOK_NAME_BY_METHOD = {
    "SaveBills": "nb_p1_finance_sl_save_bills",
    "SaveAuths": "nb_p1_finance_sl_save_auths",
    "SaveAuthBillsub": "nb_p1_finance_sl_save_auth_billsub",
    "SaveFmp": "nb_p1_finance_sl_save_fmp",
    "SavePayerCltHistory": "nb_p1_finance_sl_save_payer_clt_history",
    "SaveFinancialHardshipApplication": "nb_p1_finance_sl_save_financial_hardship_application",
    "Save3pElig": "nb_p1_finance_sl_save_3p_elig",
    "SaveClaimLineItem": "nb_p1_finance_sl_save_claim_line_item",
    "SaveClaimLineItemActivity": "nb_p1_finance_sl_save_claim_line_item_activity",
    "SaveClaims": "nb_p1_finance_sl_save_claims",
    "SavePayerClient": "nb_p1_finance_sl_save_payer_client",
    "SaveTblDiags": "nb_p1_finance_sl_save_tbl_diags",
    "SaveClientDemo1var": "nb_p1_finance_sl_save_client_demo1",
    "SaveClientDemo2": "nb_p1_finance_sl_save_client_demo2",
}

NOTEBOOK_ID_BY_METHOD = {
    method: notebook_name for method, notebook_name in NOTEBOOK_NAME_BY_METHOD.items()
}

CHILD_TASKCONFIG_ACTIVITY_NAME = "nb_get_p1_finance_child_taskconfig"

COMPACT_TASKCONFIG_COLUMNS = [
    "TaskConfigId",
    "ConfigId",
    "TargetName",
    "Method",
    "SourceTable",
    "LoadType",
    "IsIncremental",
    "WatermarkColumn",
    "LookbackDays",
    "TargetSchema",
    "TargetTable",
    "TargetPath",
    "ExecutionOrder",
    "SiteCode",
    "DataBaseName",
    "SiteName",
    "IsActive",
]

METADATA_COLS = [
    "SiteCode",
    "SourceDatabase",
    "IngestRunId",
    "ExtractedAt",
    "SourceQueryStartDate",
    "SourceQueryEndDate",
    "LookbackDate",
]

CLIENT_DEMO_CHECKSUM_FIELDS = [
    "cltID", "cltM4ID", "cltFName", "cltMI", "cltLName", "cltDOB", "cltGender",
    "cltSSN", "cltSize", "cltADD1", "cltADD2", "cltCity", "cltState", "cltzip",
    "cltPhone", "cltEmployer", "cltWorkPh", "cltIncome", "cltEducation", "cltHair",
    "cltEye", "cltH", "cltW", "cltRace", "cltpreg", "cltLANG", "cltMARRY",
    "cltemail", "cltEmpStatus", "cltPregEDC", "cltSuffix", "cltCounty",
    "cltCounselor", "cltCHANGEUSER", "isSalesForceSync", "salesForceId",
    "cltLastBill", "cltFreq", "cltProg", "cltMedicaid", "cltAmount",
]

COMMON_POLICY = {
    "timeout": "0.12:00:00",
    "retry": 0,
    "retryIntervalInSeconds": 30,
    "secureOutput": False,
    "secureInput": False,
}


def parse_existing_pipeline_jsons():
    text = BASE_PATH.read_text(encoding="utf-8")
    decoder = json.JSONDecoder()
    pos = 0
    objects = []
    while True:
        start = text.find("{", pos)
        if start < 0:
            break
        try:
            obj, end = decoder.raw_decode(text[start:])
            objects.append(obj)
            pos = start + end
        except json.JSONDecodeError:
            pos = start + 1
    if len(objects) < 3:
        raise RuntimeError("Expected at least three JSON objects in pl_p1_finance.txt.")
    return objects[:3]


def load_finance_tables():
    src = TASKCONFIG_PATH.read_text(encoding="utf-8")
    mod = ast.parse(src)
    finance_expr = None
    for node in mod.body:
        if isinstance(node, ast.Assign):
            names = [t.id for t in node.targets if isinstance(t, ast.Name)]
            if "finance_tables" in names:
                finance_expr = ast.get_source_segment(src, node.value)
                break
    if finance_expr is None:
        raise RuntimeError("finance_tables assignment not found.")
    return eval(finance_expr, {"__builtins__": {}}, {"default_lookback_days": 15})


def load_schema():
    schema_by_table = defaultdict(list)
    schema_row_by_table_col = defaultdict(dict)
    with SCHEMA_PATH.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f, delimiter="\t")
        for row in reader:
            if not row.get("TableName") or not row.get("ColumnName"):
                continue
            if str(row.get("SourceSystem", "")).upper() != "BHG_DR":
                continue
            table_key = row["TableName"].lower()
            schema_by_table[table_key].append(row["ColumnName"])
            schema_row_by_table_col[table_key][row["ColumnName"].lower()] = row
    return schema_by_table, schema_row_by_table_col


def load_map_rows():
    rows_by_table = defaultdict(list)
    with MAP_PATH.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            rows_by_table[row["FinanceTableName"].lower()].append(row)
    return rows_by_table


def load_sort_orders():
    sort_order_by_table = {}
    with ACTIONS_PATH.open(encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        for row in reader:
            table = row["DsnTbl"].lower()
            sort_order_by_table.setdefault(table, row["SortOrder"])
    return sort_order_by_table


def clean_name(name):
    if name is None:
        return None
    name = name.strip()
    if len(name) >= 2 and name[0] == "[" and name[-1] == "]":
        return name[1:-1]
    return name


def qident(name):
    name = clean_name(name)
    return "[" + name.replace("]", "]]") + "]"


def cast_null(row):
    data_type = row["DataType"].lower()
    max_len = row.get("MaxLength", "NULL")
    precision = row.get("NumericPrecision", "NULL")
    scale = row.get("NumericScale", "NULL")
    if data_type in {"nvarchar", "varchar", "char", "nchar"}:
        length = "max" if max_len in ("NULL", "", "-1", "1073741823") else max_len
        return f"CAST(NULL AS {data_type}({length}))"
    if data_type in {"ntext", "text"}:
        return "CAST(NULL AS nvarchar(max))"
    if data_type == "varbinary":
        length = "max" if max_len in ("NULL", "", "-1") else max_len
        return f"CAST(NULL AS varbinary({length}))"
    if data_type in {"numeric", "decimal"}:
        if precision not in ("NULL", "") and scale not in ("NULL", ""):
            return f"CAST(NULL AS {data_type}({precision},{scale}))"
        return f"CAST(NULL AS {data_type}(18,0))"
    return f"CAST(NULL AS {data_type})"


def source_expr_for_row(row, method):
    field = row["FieldName"].strip()
    target = clean_name(row["DsnFieldName"])
    if method == "SaveAuthBillsub":
        if target.lower() == "pysubsid":
            return "ISNULL([pySUBSID], ':(')"
        if target.lower() == "charge":
            return "ISNULL([charge], 0)"
    if field.startswith("@") or "." in field or "case " in field.lower():
        return field
    return qident(field)


def checksum_expr(method, rows):
    if method in {"SaveClientDemo1var", "SaveClientDemo2"}:
        return "CHECKSUM(" + ", ".join(qident(c) for c in CLIENT_DEMO_CHECKSUM_FIELDS) + ")"
    fields = []
    for row in rows:
        if row["Enabled"] != "1":
            continue
        field = row["FieldName"].strip()
        if field == "RowChkSum" or field.startswith("@"):
            continue
        if row["FieldType"].lower() in {"ntext", "varbinary", "timestamp"}:
            continue
        if "." in field or "case " in field.lower():
            continue
        fields.append(qident(field))
    if not fields:
        return None
    return "CHECKSUM(" + ", ".join(fields) + ")"


def rowstate_expr(method, data_type):
    if method == "SaveBills":
        return "CASE WHEN ISNULL([billCLTID], 0) <= 0 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END"
    if method == "SaveFinancialHardshipApplication":
        return "CASE WHEN ISNULL([IsDeleted], 0) = 1 THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END"
    if data_type.lower() == "int":
        return "1"
    return "CAST(1 AS bit)"


def where_clause(spec):
    method = spec["method"]
    if method == "SaveBills":
        return (
            "WHERE YEAR([billDate]) >= YEAR(DATEADD(day, -__LOOKBACK_DAYS__, "
            "CONVERT(date, '__WORK_DATE__'))) AND [billDate] <= DATEADD(day, 12, "
            "CONVERT(date, '__WORK_DATE__'))"
        )
    if method == "SavePayerCltHistory":
        return (
            "WHERE [pyDtm] IS NOT NULL AND [pyDtm] >= DATEADD(day, -__LOOKBACK_DAYS__, "
            "CONVERT(date, '__WORK_DATE__'))"
        )
    if method == "Save3pElig":
        return (
            "WHERE YEAR([edate]) >= YEAR(DATEADD(day, -__LOOKBACK_DAYS__, "
            "CONVERT(date, '__WORK_DATE__')))"
        )
    if method == "SavePayerClient":
        hist_cutoff = "DATEADD(day, -360, DATEADD(day, -__LOOKBACK_DAYS__, CONVERT(date, '__WORK_DATE__')))"
        return (
            "WHERE [pyid] IN (SELECT DISTINCT [pyID] FROM [__DATABASE__].[dbo].[tblPayerCltHistory] "
            f"WHERE [pyDtm] >= {hist_cutoff}) OR [pyACTIVE] = 1 OR ISNULL([pyEND], GETDATE()) >= "
            f"{hist_cutoff} OR [pyEnd] >= {hist_cutoff}"
        )
    return "WHERE 1 = 1"


def build_select_sql(spec, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table, where_override=None):
    method = spec["method"]
    table_key = spec["silver_table"].lower()
    if table_key not in schema_by_table:
        raise RuntimeError(f"Schema not found for {spec['silver_table']}.")
    rows = map_rows_by_table.get(table_key, [])
    enabled_by_target = {}
    for row in rows:
        if row["Enabled"] != "1":
            continue
        target = clean_name(row["DsnFieldName"])
        if not target or target.upper() == "NULL":
            continue
        enabled_by_target[target.lower()] = row

    schema_cols = schema_by_table[table_key]
    schema_rows = schema_row_by_table_col[table_key]
    row_checksum = checksum_expr(method, rows)
    expressions = [
        ("'__SITE_CODE__'", "SiteCode"),
        ("'__DATABASE__'", "SourceDatabase"),
        ("'__INGEST_RUN_ID__'", "IngestRunId"),
        ("GETDATE()", "ExtractedAt"),
        ("CONVERT(date, DATEADD(day, -__LOOKBACK_DAYS__, CONVERT(date, '__WORK_DATE__')))", "SourceQueryStartDate"),
        ("CONVERT(date, '__WORK_DATE__')", "SourceQueryEndDate"),
        ("CONVERT(date, DATEADD(day, -__LOOKBACK_DAYS__, CONVERT(date, '__WORK_DATE__')))", "LookbackDate"),
    ]

    for col in schema_cols:
        if col.lower() == "sitecode":
            continue
        row_schema = schema_rows[col.lower()]
        lower = col.lower()
        if lower == "rowchksum" and row_checksum:
            expr = row_checksum
        elif lower == "lastmodat":
            expr = "GETDATE()"
        elif lower == "rowstate":
            expr = rowstate_expr(method, row_schema["DataType"])
        elif method == "SaveAuthBillsub" and lower == "cptmod":
            expr = "ISNULL(NULLIF(ISNULL([CPTCODE], '') + ':' + ISNULL([Modifier], ''), ':'), ':(')"
        elif method == "SaveClientDemo1var" and lower == "primkey":
            expr = cast_null(row_schema)
        elif method == "SavePayerClient" and lower == "pcid":
            expr = cast_null(row_schema)
        elif lower in enabled_by_target:
            expr = source_expr_for_row(enabled_by_target[lower], method)
        else:
            expr = cast_null(row_schema)
        expressions.append((expr, col))

    select_keyword = "SELECT DISTINCT" if method == "SaveAuthBillsub" else "SELECT"
    lines = [select_keyword]
    for idx, (expr, alias) in enumerate(expressions):
        comma = "," if idx < len(expressions) - 1 else ""
        lines.append(f"    {expr} AS {qident(alias)}{comma}")
    schema, obj = spec["source_table"].split(".", 1)
    lines.append(f"FROM [__DATABASE__].{qident(schema)}.{qident(obj)}")
    lines.append(where_override if where_override is not None else where_clause(spec))
    sort = sort_order_by_table.get(spec["silver_table"].lower()) or "Order by 1, 2"
    if method in {"SaveClientDemo1var", "SaveClientDemo2"}:
        sort = "ORDER BY 1, [cltID]"
    lines.append(sort.replace("Order by", "ORDER BY").replace("order by", "ORDER BY"))
    return "\n".join(lines)


PLACEHOLDER_DYNAMIC = {
    "__SITE_CODE__": "item().SiteCode",
    "__DATABASE__": "item().DataBaseName",
    "__INGEST_RUN_ID__": "if(equals(pipeline().parameters.p_ingest_run_id, ''), pipeline().RunId, pipeline().parameters.p_ingest_run_id)",
    "__WORK_DATE__": "if(or(equals(pipeline().parameters.p_work_date, ''), equals(pipeline().parameters.p_work_date, null)), formatDateTime(utcNow(), 'yyyy-MM-dd'), pipeline().parameters.p_work_date)",
    "__LOOKBACK_DAYS__": "string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days))",
    "__CLAIMS_WHERE__": (
        "if(or(equals(item().SiteCode, 'VBRA'), or(equals(item().SiteCode, 'VMIN'), "
        "or(equals(item().SiteCode, 'VWBY'), equals(item().SiteCode, 'VBRP')))), "
        "concat('WHERE YEAR(CONVERT(date, [tpcCreatedDate])) >= YEAR(DATEADD(day, -', "
        "string(coalesce(item().LookbackDays, pipeline().parameters.p_lookback_days)), "
        "', CONVERT(date, ''', "
        "if(or(equals(pipeline().parameters.p_work_date, ''), equals(pipeline().parameters.p_work_date, null)), formatDateTime(utcNow(), 'yyyy-MM-dd'), pipeline().parameters.p_work_date), "
        "''')))'), "
        "'WHERE 1 = 1')"
    ),
}
PLACEHOLDER_PATTERN = re.compile("|".join(re.escape(k) for k in PLACEHOLDER_DYNAMIC))


def adf_static(value):
    return "'" + value.replace("'", "''") + "'"


def adf_concat(sql, with_at=True):
    args = []
    pos = 0
    for match in PLACEHOLDER_PATTERN.finditer(sql):
        if match.start() > pos:
            args.append(adf_static(sql[pos:match.start()]))
        args.append(PLACEHOLDER_DYNAMIC[match.group(0)])
        pos = match.end()
    if pos < len(sql):
        args.append(adf_static(sql[pos:]))
    body = "concat(\n" + ",\n".join(args) + "\n)"
    return "@" + body if with_at else body


def build_claims_compact_sql(spec, map_rows_by_table, sort_order_by_table):
    rows = map_rows_by_table.get(spec["silver_table"].lower(), [])
    row_checksum = checksum_expr(spec["method"], rows)
    if not row_checksum:
        raise RuntimeError("Claims RowChkSum expression could not be built.")
    schema, obj = spec["source_table"].split(".", 1)
    sort = sort_order_by_table.get(spec["silver_table"].lower()) or "Order by 1, 2"
    lines = [
        "SELECT",
        "    '__SITE_CODE__' AS [SiteCode],",
        "    '__DATABASE__' AS [SourceDatabase],",
        "    '__INGEST_RUN_ID__' AS [IngestRunId],",
        "    GETDATE() AS [ExtractedAt],",
        "    CONVERT(date, DATEADD(day, -__LOOKBACK_DAYS__, CONVERT(date, '__WORK_DATE__'))) AS [SourceQueryStartDate],",
        "    CONVERT(date, '__WORK_DATE__') AS [SourceQueryEndDate],",
        "    CONVERT(date, DATEADD(day, -__LOOKBACK_DAYS__, CONVERT(date, '__WORK_DATE__'))) AS [LookbackDate],",
        f"    {row_checksum} AS [RowChkSum],",
        "    GETDATE() AS [LastModAt],",
        "    CAST(1 AS bit) AS [RowState],",
        "    src.*",
        f"FROM [__DATABASE__].{qident(schema)}.{qident(obj)} AS src",
        "__CLAIMS_WHERE__",
        sort.replace("Order by", "ORDER BY").replace("order by", "ORDER BY"),
    ]
    return "\n".join(lines)


def query_expression(spec, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table):
    if spec["method"] != "SaveClaims":
        return adf_concat(build_select_sql(spec, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table))
    return adf_concat(build_claims_compact_sql(spec, map_rows_by_table, sort_order_by_table))


def mappings_for_spec(spec, schema_by_table):
    table_key = spec["silver_table"].lower()
    cols = METADATA_COLS + [c for c in schema_by_table[table_key] if c.lower() != "sitecode"]
    mappings = []
    seen = set()
    for col in cols:
        key = col.lower()
        if key in seen:
            continue
        seen.add(key)
        mappings.append({"source": {"name": col}, "sink": {"name": col}})
    return mappings


def source_exists_query(spec):
    schema, obj = spec["source_table"].split(".", 1)
    slug = SLUG_BY_METHOD[spec["method"]]
    alias = "three_p_elig_exists" if slug == "3p_elig" else f"{slug}_exists"
    return (
        "@concat(\n"
        f"'SELECT {alias} = COUNT(1) FROM [',\n"
        "item().DataBaseName,\n"
        "'].sys.objects o INNER JOIN [',\n"
        "item().DataBaseName,\n"
        f"'].sys.schemas s ON o.schema_id = s.schema_id WHERE s.name = ''{schema}'' "
        f"AND o.name = ''{obj}'' AND o.type IN (''U'', ''V'')'\n"
        ")"
    )


def copy_activity(spec, base_copy, bronze_sink_template, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table):
    method = spec["method"]
    slug = SLUG_BY_METHOD[method]
    sink = copy.deepcopy(bronze_sink_template)
    sink["datasetSettings"]["typeProperties"]["schema"] = "P1Finance"
    sink["datasetSettings"]["typeProperties"]["table"] = spec["bronze_table"]
    return {
        "name": f"cp_{slug}_to_bronze",
        "type": "Copy",
        "dependsOn": [],
        "policy": copy.deepcopy(COMMON_POLICY),
        "typeProperties": {
            "source": {
                "type": "SqlServerSource",
                "sqlReaderQuery": {
                    "value": query_expression(spec, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table),
                    "type": "Expression",
                },
                "queryTimeout": "02:00:00",
                "partitionOption": "None",
                "datasetSettings": copy.deepcopy(base_copy["typeProperties"]["source"]["datasetSettings"]),
            },
            "sink": sink,
            "enableStaging": False,
            "translator": {
                "type": "TabularTranslator",
                "mappings": mappings_for_spec(spec, schema_by_table),
                "typeConversion": True,
                "typeConversionSettings": {
                    "allowDataTruncation": True,
                    "treatBooleanAsNumber": False,
                },
            },
        },
    }


def filter_activity(spec):
    method = spec["method"]
    slug = SLUG_BY_METHOD[method]
    return {
        "name": f"flt_child_{slug}_sites",
        "type": "Filter",
        "dependsOn": [{
            "activity": CHILD_TASKCONFIG_ACTIVITY_NAME,
            "dependencyConditions": ["Succeeded"],
        }],
        "typeProperties": {
            "items": {
                "value": f"@json(activity('{CHILD_TASKCONFIG_ACTIVITY_NAME}').output.result.exitValue)",
                "type": "Expression",
            },
            "condition": {"value": f"@equals(item().Method, '{method}')", "type": "Expression"},
        },
    }


def foreach_activity(spec, base_lookup, base_copy, bronze_sink_template, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table):
    method = spec["method"]
    slug = SLUG_BY_METHOD[method]
    source_connection = copy.deepcopy(
        base_lookup["typeProperties"]["datasetSettings"]["externalReferences"]
    )
    lookup = {
        "name": f"lkp_check_{slug}",
        "type": "Lookup",
        "dependsOn": [],
        "policy": copy.deepcopy(COMMON_POLICY),
        "typeProperties": {
            "source": {
                "type": "SqlServerSource",
                "sqlReaderQuery": {"value": source_exists_query(spec), "type": "Expression"},
                "queryTimeout": "02:00:00",
                "partitionOption": "None",
            },
            "firstRowOnly": True,
            "datasetSettings": {
                "annotations": [],
                "type": "SqlServerTable",
                "schema": [],
                "externalReferences": source_connection,
            },
        },
    }
    if_condition = {
        "name": f"if_{slug}_exists",
        "type": "IfCondition",
        "dependsOn": [{"activity": f"lkp_check_{slug}", "dependencyConditions": ["Succeeded"]}],
        "typeProperties": {
            "expression": {
                "value": (
                    "@equals(activity('lkp_check_3p_elig').output.firstRow.three_p_elig_exists, 1)"
                    if slug == "3p_elig"
                    else f"@equals(activity('lkp_check_{slug}').output.firstRow.{slug}_exists, 1)"
                ),
                "type": "Expression",
            },
            "ifFalseActivities": [],
            "ifTrueActivities": [
                copy_activity(
                    spec, base_copy, bronze_sink_template, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table
                )
            ],
        },
    }
    return {
        "name": f"fe_each_samms_site_{slug}",
        "type": "ForEach",
        "dependsOn": [{"activity": f"flt_child_{slug}_sites", "dependencyConditions": ["Succeeded"]}],
        "typeProperties": {
            "items": {"value": f"@activity('flt_child_{slug}_sites').output.value", "type": "Expression"},
            "isSequential": False,
            "batchCount": 10,
            "activities": [lookup, if_condition],
        },
    }


def make_bronze_methods_sequential(activities, finance_tables):
    by_name = {activity["name"]: activity for activity in activities}
    previous_foreach_name = None
    for spec in finance_tables:
        slug = SLUG_BY_METHOD[spec["method"]]
        filter_name = f"flt_child_{slug}_sites"
        foreach_name = f"fe_each_samms_site_{slug}"
        filter_item = by_name[filter_name]
        foreach_item = by_name[foreach_name]
        filter_item["dependsOn"] = []
        if previous_foreach_name:
            filter_item["dependsOn"] = [{
                "activity": previous_foreach_name,
                "dependencyConditions": ["Succeeded", "Failed", "Skipped"],
            }]
        foreach_item["dependsOn"] = [{
            "activity": filter_name,
            "dependencyConditions": ["Succeeded"],
        }]
        previous_foreach_name = foreach_name


def child_taskconfig_activity(taskconfig_activity, methods):
    activity = copy.deepcopy(taskconfig_activity)
    activity["name"] = CHILD_TASKCONFIG_ACTIVITY_NAME
    activity["dependsOn"] = []
    params = activity.setdefault("typeProperties", {}).setdefault("parameters", {})
    params["p_config_name_prefix"] = {"value": "SAMMS P1 Finance", "type": "string"}
    params["p_target_names_json"] = {"value": json.dumps(["BR"], separators=(",", ":")), "type": "string"}
    params["p_methods_json"] = {"value": json.dumps(methods, separators=(",", ":")), "type": "string"}
    params["p_only_active"] = {"value": "true", "type": "string"}
    params["p_require_site"] = {"value": "true", "type": "string"}
    params["p_require_database"] = {"value": "true", "type": "string"}
    params["p_require_source_table"] = {"value": "true", "type": "string"}
    params["p_output_columns_json"] = {
        "value": json.dumps(COMPACT_TASKCONFIG_COLUMNS, separators=(",", ":")),
        "type": "string",
    }
    return activity


def bronze_results_expression(finance_tables):
    args = ["'{'"]
    for idx, spec in enumerate(finance_tables):
        method = spec["method"]
        foreach_name = f"fe_each_samms_site_{SLUG_BY_METHOD[method]}"
        if idx:
            args.append("','")
        args.extend([
            f"'\"{method}\":{{\"status\":\"'",
            f"if(equals(activity('{foreach_name}').Status,'Succeeded'),'SUCCESS','FAILED')",
            "'\",\"failed_stage\":\"'",
            f"if(equals(activity('{foreach_name}').Status,'Succeeded'),'','BR')",
            "'\",\"error_message\":'",
            f"if(equals(activity('{foreach_name}').Status,'Succeeded'),'null','\"{method} Bronze ForEach failed\"')",
            "'}'",
        ])
    args.append("'}'")
    return "@concat(" + ",".join(args) + ")"


def child_pipeline(forms_child, finance_tables, taskconfig_activity, base_lookup, base_copy, bronze_sink_template, schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table):
    child = {
        "name": forms_child.get("name") or "pl_p1_child_finance",
        "objectId": forms_child.get("objectId") or "TODO_FINANCE_BRONZE_CHILD_PIPELINE_ID",
        "properties": {
            "activities": [],
            "parameters": {
                "p_ingest_run_id": {"type": "string"},
                "p_work_date": {"type": "string", "defaultValue": "2026-08-06"},
                "p_lookback_days": {"type": "int", "defaultValue": 15},
                "p_sites": {"type": "array", "defaultValue": []},
                "p_sites_json": {"type": "string", "defaultValue": "[]"},
            },
            "variables": {"v_bronze_method_results_json": {"type": "String", "defaultValue": "{}"}},
            "lastModifiedByObjectId": forms_child["properties"].get("lastModifiedByObjectId"),
            "lastPublishTime": forms_child["properties"].get("lastPublishTime"),
        },
    }
    methods = [spec["method"] for spec in finance_tables]
    activities = [child_taskconfig_activity(taskconfig_activity, methods)]
    for spec in finance_tables:
        activities.append(filter_activity(spec))
        activities.append(
            foreach_activity(
                spec, base_lookup, base_copy, bronze_sink_template,
                schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table
            )
        )
    activities.append({
        "name": "set_child_bronze_method_results",
        "type": "SetVariable",
        "dependsOn": [
            {"activity": f"fe_each_samms_site_{SLUG_BY_METHOD[spec['method']]}", "dependencyConditions": ["Completed"]}
            for spec in finance_tables
        ],
        "policy": {"secureOutput": False, "secureInput": False},
        "typeProperties": {
            "variableName": "pipelineReturnValue",
            "value": [{
                "key": "v_bronze_method_results_json",
                "value": {"type": "Expression", "content": bronze_results_expression(finance_tables)},
            }],
            "setSystemVariable": True,
        },
    })
    child["properties"]["activities"] = activities
    return child


def nested_or_methods(methods):
    if len(methods) == 1:
        return f"equals(item().Method, '{methods[0]}')"
    return f"or(equals(item().Method, '{methods[0]}'), {nested_or_methods(methods[1:])})"


def failure_details_expr(finance_tables):
    args = ["'Failed items only: '"]
    for spec in finance_tables:
        method = spec["method"]
        args.append(
            f"if(and(contains(variables('v_bronze_method_results_json'),'{method}'),"
            f"not(equals(json(variables('v_bronze_method_results_json'))['{method}']['status'],'SUCCESS'))),"
            f"concat('BR {method} - ',string(json(variables('v_bronze_method_results_json'))['{method}']['error_message']),'; '),'')"
        )
    for spec in finance_tables:
        method = spec["method"]
        args.append(
            f"if(and(contains(variables('v_silver_method_results_json'),'{method}'),"
            f"equals(json(variables('v_silver_method_results_json'))['{method}']['status'],'FAILED')),"
            f"concat('SL {method} - ',string(json(variables('v_silver_method_results_json'))['{method}']['message']),'; '),'')"
        )
    args.append(
        "if(and(not(contains(variables('v_bronze_method_results_json'),'FAILED')),not(contains(variables('v_silver_method_results_json'),'FAILED'))),"
        "'Pipeline failed but no method-level FAILED status was returned. Check Fabric activity details.','')"
    )
    return "@concat(" + ",".join(args) + ")"


def replace_strings(value):
    replacements = {
        "SAMMS P1 Forms": "SAMMS P1 Finance",
        "P1 Forms ETL": "P1 Finance ETL",
        "Forms Bronze child pipeline failed before returning method results": "Finance Bronze child pipeline failed before returning method results",
        "Forms Silver child pipeline failed before returning method results": "Finance Silver child pipeline failed before returning method results",
        "P1FormsSilver": "P1FinanceSilver",
        "P1Forms": "P1Finance",
        "pl_execute_forms": "pl_execute_finance",
        "/pipelines/pl_execute_forms": "/pipelines/pl_execute_finance",
        "nb_get_p1_forms_taskconfig": "nb_get_p1_finance_taskconfig",
        "flt_active_p1_forms_sites": "flt_active_p1_finance_sites",
        "nb_p1_forms_audit_start": "nb_p1_finance_audit_start",
        "if_all_forms_methods_success": "if_all_finance_methods_success",
        "nb_p1_forms_notify_failed": "nb_p1_finance_notify_failed",
        "flt_active_p1_forms_gold": "flt_active_p1_finance_gold",
        "nb_p1_forms_optional_gold_publish": "nb_p1_finance_optional_gold_publish",
        "nb_p1_forms_audit_finalize_success": "nb_p1_finance_audit_finalize_success",
        "nb_p1_forms_audit_finalize_failure": "nb_p1_finance_audit_finalize_failure",
    }
    if isinstance(value, str):
        for old, new in replacements.items():
            value = value.replace(old, new)
        return value
    if isinstance(value, list):
        return [replace_strings(item) for item in value]
    if isinstance(value, dict):
        return {key: replace_strings(item) for key, item in value.items()}
    return value


def parent_pipeline(forms_parent, finance_tables):
    methods = [spec["method"] for spec in finance_tables]
    parent = replace_strings(copy.deepcopy(forms_parent))
    parent["name"] = "pl_execute_finance"
    parent["objectId"] = forms_parent.get("objectId") or "TODO_FINANCE_PARENT_PIPELINE_ID"
    parent["properties"]["parameters"]["p_work_date"]["defaultValue"] = "2026-08-06"
    parent["properties"]["activities"] = [
        activity
        for activity in parent["properties"]["activities"]
        if activity.get("name") != "flt_active_p1_finance_sites"
    ]
    activities = {activity["name"]: activity for activity in parent["properties"]["activities"]}

    get_taskconfig = activities["nb_get_p1_finance_taskconfig"]
    get_taskconfig["typeProperties"]["parameters"]["p_config_name_prefix"]["value"] = "SAMMS P1 Finance"
    get_taskconfig["typeProperties"]["parameters"]["p_target_names_json"]["value"] = json.dumps(["GL"], separators=(",", ":"))
    get_taskconfig["typeProperties"]["parameters"]["p_methods_json"]["value"] = json.dumps(methods, separators=(",", ":"))
    get_taskconfig["typeProperties"]["parameters"]["p_only_active"] = {"value": "true", "type": "string"}
    get_taskconfig["typeProperties"]["parameters"]["p_require_site"] = {"value": "false", "type": "string"}
    get_taskconfig["typeProperties"]["parameters"]["p_require_database"] = {"value": "false", "type": "string"}
    get_taskconfig["typeProperties"]["parameters"]["p_require_source_table"] = {"value": "true", "type": "string"}
    get_taskconfig["typeProperties"]["parameters"]["p_output_columns_json"] = {
        "value": json.dumps(COMPACT_TASKCONFIG_COLUMNS, separators=(",", ":")),
        "type": "string",
    }

    audit_start = activities["nb_p1_finance_audit_start"]
    audit_start["dependsOn"] = [{
        "activity": "nb_get_p1_finance_taskconfig",
        "dependencyConditions": ["Succeeded"],
    }]

    set_bronze = activities["set_bronze_method_results_from_child"]
    set_bronze["typeProperties"]["value"]["value"] = (
        "@if(equals(activity('Executed_AfterBronz').Status,'Succeeded'), "
        "string(activity('Executed_AfterBronz').output.properties.returnValue.v_bronze_method_results_json), "
        "'{\\\"P1Finance\\\":{\\\"status\\\":\\\"FAILED\\\",\\\"failed_stage\\\":\\\"BR\\\","
        "\\\"error_message\\\":\\\"Finance Bronze child pipeline failed before returning method results\\\"}}')"
    )
    set_silver = activities["set_silver_method_results_from_child"]
    set_silver["typeProperties"]["value"]["value"] = (
        "@if(equals(activity('Executed_AfterSilver').Status,'Succeeded'), "
        "string(activity('Executed_AfterSilver').output.properties.returnValue.v_silver_method_results_json), "
        "'{\\\"P1FinanceSilver\\\":{\\\"method\\\":\\\"P1FinanceSilver\\\",\\\"layer\\\":\\\"SL\\\","
        "\\\"status\\\":\\\"FAILED\\\",\\\"rows_read\\\":0,\\\"rows_inserted\\\":0,\\\"rows_updated\\\":0,"
        "\\\"rows_skipped\\\":0,\\\"message\\\":\\\"Finance Silver child pipeline failed before returning method results\\\"}}')"
    )

    invoke_bronze = activities["Executed_AfterBronz"]
    invoke_bronze["dependsOn"] = [{
        "activity": "nb_p1_finance_audit_start",
        "dependencyConditions": ["Succeeded"],
    }]
    invoke_bronze["typeProperties"]["parameters"].pop("p_sites", None)
    invoke_bronze["typeProperties"]["parameters"].pop("p_sites_json", None)
    invoke_bronze["typeProperties"]["parameters"]["p_work_date"] = {
        "value": "@pipeline().parameters.p_work_date",
        "type": "Expression",
    }
    invoke_silver = activities["Executed_AfterSilver"]
    invoke_silver["typeProperties"]["parameters"]["p_sites_json"] = {"value": "[]", "type": "string"}

    for audit_activity_name in [
        "nb_p1_finance_audit_finalize_failure",
        "nb_p1_finance_audit_finalize_success",
    ]:
        for branch in activities["if_all_finance_methods_success"]["typeProperties"].values():
            if not isinstance(branch, list):
                continue
            for branch_activity in branch:
                if branch_activity.get("name") == audit_activity_name:
                    branch_activity["typeProperties"]["parameters"]["p_sites_json"]["value"] = "[]"
                    branch_activity["typeProperties"]["parameters"]["p_sites_json"].pop("type", None)
                    branch_activity["typeProperties"]["parameters"]["p_sites_json"]["type"] = "string"

    notify = activities["nb_p1_finance_notify_failed"]
    notify["typeProperties"]["parameters"]["Description"]["value"] = (
        "P1 Finance pipeline failed or audit finalization failed. See error details."
    )
    notify["typeProperties"]["parameters"]["Error_Msg"]["value"]["value"] = failure_details_expr(finance_tables)
    return parent


def silver_notebook_activity(spec, workspace_id, notebook_id_by_method):
    method = spec["method"]
    activity_name = NOTEBOOK_ACTIVITY_BY_METHOD[method]
    depends_on = []
    if method == "SaveClientDemo2":
        depends_on = [{
            "activity": NOTEBOOK_ACTIVITY_BY_METHOD["SaveClientDemo1var"],
            "dependencyConditions": ["Succeeded"],
        }]
    return {
        "name": activity_name,
        "type": "TridentNotebook",
        "dependsOn": depends_on,
        "policy": copy.deepcopy(COMMON_POLICY),
        "typeProperties": {
            "notebookId": notebook_id_by_method.get(method) or NOTEBOOK_ID_BY_METHOD[method],
            "workspaceId": workspace_id,
            "parameters": {
                "p_ingest_run_id": {
                    "value": {"value": "@pipeline().parameters.p_ingest_run_id", "type": "Expression"},
                    "type": "string",
                },
                "p_bronze_method_results_json": {
                    "value": {"value": "@pipeline().parameters.p_bronze_method_results_json", "type": "Expression"},
                    "type": "string",
                },
                "p_sites_json": {
                    "value": {"value": "@pipeline().parameters.p_sites_json", "type": "Expression"},
                    "type": "string",
                },
                "p_method_name": {"value": method, "type": "string"},
            },
        },
    }


def silver_result_fragment(spec):
    method = spec["method"]
    activity_name = NOTEBOOK_ACTIVITY_BY_METHOD[method]
    exit_value = f"string(activity('{activity_name}').output.result.exitValue)"
    fallback = (
        f"concat('\"{method}\":{{\"method\":\"{method}\",\"layer\":\"SL\",\"status\":\"',"
        f"if(equals(activity('{activity_name}').Status,'Skipped'),'SKIPPED','FAILED'),"
        "'\",\"rows_read\":0,\"rows_inserted\":0,\"rows_updated\":0,\"rows_skipped\":0,\"message\":\"',"
        f"if(equals(activity('{activity_name}').Status,'Skipped'),'{method} SL skipped','{method} SL failed/no exit'),"
        "'\"}')"
    )
    return (
        f"if(equals(activity('{activity_name}').Status,'Succeeded'),"
        f"if(greater(length({exit_value}),2),substring({exit_value},1,sub(length({exit_value}),2)),{fallback}),"
        f"{fallback})"
    )


def silver_results_expression(finance_tables):
    args = []
    for idx, spec in enumerate(finance_tables):
        if idx:
            args.append("','")
        args.append(silver_result_fragment(spec))
    return "@concat(" + ",".join(args) + ")"


def silver_pipeline(forms_silver, finance_tables, workspace_id, notebook_id_by_method):
    silver = {
        "name": forms_silver.get("name") or "pl_p1_finance_child_bronze_to_silver",
        "objectId": forms_silver.get("objectId") or "TODO_FINANCE_BRONZE_TO_SILVER_CHILD_PIPELINE_ID",
        "properties": {
            "activities": [],
            "parameters": {
                "p_ingest_run_id": {"type": "string"},
                "p_bronze_method_results_json": {"type": "string"},
                "p_sites_json": {"type": "string", "defaultValue": "[]"},
            },
            "variables": {
                "v_silver_method_results_part1": {"type": "String"},
                "v_silver_method_results_part2": {"type": "String"},
            },
            "lastModifiedByObjectId": forms_silver["properties"].get("lastModifiedByObjectId"),
            "lastPublishTime": forms_silver["properties"].get("lastPublishTime"),
        },
    }
    activities = [silver_notebook_activity(spec, workspace_id, notebook_id_by_method) for spec in finance_tables]
    first_half = finance_tables[:7]
    second_half = finance_tables[7:]
    activities.append({
        "name": "set_silver_method_results_part1",
        "type": "SetVariable",
        "dependsOn": [
            {
                "activity": NOTEBOOK_ACTIVITY_BY_METHOD[spec["method"]],
                "dependencyConditions": ["Succeeded", "Failed", "Skipped"],
            }
            for spec in finance_tables
        ],
        "policy": {"secureOutput": False, "secureInput": False},
        "typeProperties": {
            "variableName": "v_silver_method_results_part1",
            "value": {"value": silver_results_expression(first_half), "type": "Expression"},
        },
    })
    activities.append({
        "name": "set_silver_method_results_part2",
        "type": "SetVariable",
        "dependsOn": [{
            "activity": "set_silver_method_results_part1",
            "dependencyConditions": ["Succeeded"],
        }],
        "policy": {"secureOutput": False, "secureInput": False},
        "typeProperties": {
            "variableName": "v_silver_method_results_part2",
            "value": {"value": silver_results_expression(second_half), "type": "Expression"},
        },
    })
    activities.append({
        "name": "set_silver_method_results_return",
        "type": "SetVariable",
        "dependsOn": [{
            "activity": "set_silver_method_results_part2",
            "dependencyConditions": ["Succeeded"],
        }],
        "policy": {"secureOutput": False, "secureInput": False},
        "typeProperties": {
            "variableName": "pipelineReturnValue",
            "value": [{
                "key": "v_silver_method_results_json",
                "value": {
                    "type": "Expression",
                    "content": "@concat('{',variables('v_silver_method_results_part1'),',',variables('v_silver_method_results_part2'),'}')",
                },
            }],
            "setSystemVariable": True,
        },
    })
    silver["properties"]["activities"] = activities
    return silver


def iter_activities(activities):
    for activity in activities:
        yield activity
        type_properties = activity.get("typeProperties", {})
        for key in ("activities", "ifTrueActivities", "ifFalseActivities"):
            for child in type_properties.get(key, []) or []:
                yield from iter_activities([child])


def copy_query_lengths(pipeline):
    rows = []
    for activity in iter_activities(pipeline["properties"].get("activities", [])):
        if activity.get("type") != "Copy":
            continue
        query = activity["typeProperties"]["source"].get("sqlReaderQuery", {})
        value = query.get("value", "") if isinstance(query, dict) else str(query)
        rows.append((activity["name"], len(value)))
    return rows


def main():
    forms_child, forms_parent, forms_silver = parse_existing_pipeline_jsons()
    finance_tables = load_finance_tables()
    schema_by_table, schema_row_by_table_col = load_schema()
    map_rows_by_table = load_map_rows()
    sort_order_by_table = load_sort_orders()

    base_foreach = next(
        activity
        for activity in forms_child["properties"]["activities"]
        if activity.get("type") == "ForEach"
    )
    base_lookup = base_foreach["typeProperties"]["activities"][0]
    base_if = base_foreach["typeProperties"]["activities"][1]
    base_copy = base_if["typeProperties"]["ifTrueActivities"][0]
    bronze_sink_template = copy.deepcopy(base_copy["typeProperties"]["sink"])

    parent_activities = {activity["name"]: activity for activity in forms_parent["properties"]["activities"]}
    taskconfig_activity = parent_activities.get("nb_get_p1_forms_taskconfig") or parent_activities.get("nb_get_p1_finance_taskconfig")
    if not taskconfig_activity:
        raise RuntimeError("Could not find taskconfig notebook activity in parent pipeline template.")
    workspace_id = taskconfig_activity["typeProperties"]["workspaceId"]
    child_taskconfig_template = next(
        (
            activity
            for activity in forms_child["properties"]["activities"]
            if activity.get("name") == CHILD_TASKCONFIG_ACTIVITY_NAME
        ),
        taskconfig_activity,
    )
    existing_silver_notebooks = {
        activity["name"]: activity["typeProperties"].get("notebookId")
        for activity in forms_silver["properties"].get("activities", [])
        if activity.get("type") == "TridentNotebook"
    }
    notebook_id_by_method = {
        spec["method"]: existing_silver_notebooks.get(NOTEBOOK_ACTIVITY_BY_METHOD[spec["method"]])
        for spec in finance_tables
    }

    child = child_pipeline(
        forms_child, finance_tables, child_taskconfig_template, base_lookup, base_copy, bronze_sink_template,
        schema_by_table, schema_row_by_table_col, map_rows_by_table, sort_order_by_table
    )
    parent = parent_pipeline(forms_parent, finance_tables)
    silver = silver_pipeline(forms_silver, finance_tables, workspace_id, notebook_id_by_method)

    for obj in (child, parent, silver):
        json.loads(json.dumps(obj))
    combined_json = "\n".join(json.dumps(obj, indent=4, ensure_ascii=True) for obj in (child, parent, silver))
    for forbidden in [
        "SaveComprehensiveAssessmentForm",
        "SaveEMFormPregnancy",
        "P1Forms",
        "SAMMS P1 Forms",
        "pl_p1_forms",
        "pl_execute_forms",
    ]:
        if forbidden in combined_json:
            raise RuntimeError(f"Forbidden Forms token still present: {forbidden}")
    if "stg.ClientDemo" in combined_json or "stg.clientdemo" in combined_json:
        raise RuntimeError("Shared ClientDemo staging reference found.")
    if len(child["properties"]["activities"]) != 30:
        raise RuntimeError("Unexpected Bronze child activity count.")
    if len(silver["properties"]["activities"]) != 17:
        raise RuntimeError("Unexpected Silver child activity count.")
    oversized = [(name, length) for name, length in copy_query_lengths(child) if length > 8000]
    if oversized:
        details = ", ".join(f"{name}={length}" for name, length in oversized)
        raise RuntimeError(f"Copy sqlReaderQuery expression exceeds 8000 characters: {details}")
    oversized_setvars = []
    for activity in silver["properties"]["activities"]:
        if activity.get("type") != "SetVariable":
            continue
        value = activity.get("typeProperties", {}).get("value")
        expr = value.get("value") if isinstance(value, dict) else None
        if isinstance(expr, str) and len(expr) > 8000:
            oversized_setvars.append((activity["name"], len(expr)))
    if oversized_setvars:
        details = ", ".join(f"{name}={length}" for name, length in oversized_setvars)
        raise RuntimeError(f"Silver SetVariable expression exceeds 8000 characters: {details}")

    header = """P1 FINANCE FINAL PIPELINE JSONS
Generated: 2026-08-06

Rebuilt from the Forms pipeline design for the 14 SAMMS P1 Finance methods.
Sources used: finance_module_taskconfig_pyspark.py, columnsanddatatypesFinance.txt4, finance_vw_MapActions.csv, finance_vw_MapSrc2Dsn.csv, and the Finance transformation notes.

Deployment placeholders to replace after Finance artifacts are created:
- TODO_FINANCE_BRONZE_CHILD_PIPELINE_ID
- TODO_FINANCE_PARENT_PIPELINE_ID
- TODO_FINANCE_BRONZE_TO_SILVER_CHILD_PIPELINE_ID

Bronze-to-Silver notebookId values use the recommended notebook names.

Child json:
"""
    output = header + json.dumps(child, indent=4, ensure_ascii=True)
    output += "\n\nParent Json:\n" + json.dumps(parent, indent=4, ensure_ascii=True)
    output += "\n\nBronze To Silver Child Json:\n" + json.dumps(silver, indent=4, ensure_ascii=True) + "\n"
    tmp_path = BASE_PATH.with_suffix(".generated.txt")
    tmp_path.write_text(output, encoding="utf-8")
    print(f"Generated {tmp_path}")
    print(f"Bronze child activities: {len(child['properties']['activities'])}")
    print(f"Parent activities: {len(parent['properties']['activities'])}")
    print(f"Bronze-to-Silver child activities: {len(silver['properties']['activities'])}")
    print("Methods: " + ", ".join(spec["method"] for spec in finance_tables))


if __name__ == "__main__":
    main()
