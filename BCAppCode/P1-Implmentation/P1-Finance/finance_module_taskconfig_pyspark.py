# P1 Finance module TaskConfig setup
# Run this in a Fabric Spark notebook attached to the Bronze lakehouse.
#
# ID layout:
#   For each Finance method:
#     1 inactive generic Bronze row, 115 active Bronze site rows, then 1 Silver row, then 1 Gold row.
#   Start TaskConfigId: 8700
#   End TaskConfigId:   10351
#
# Uses the same SAMMS site universe as P1 Forms Bronze ConfigId=97.

import json
import re

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, LongType, StringType, StructField, StructType


etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"

# Naming standard: DEV_<first two letters of developer name>, uppercase.
created_by = "DEV_HA"
start_task_config_id = 8700

bronze_config_id = 46
silver_config_id = 47
gold_config_id = 48

# Finance should use the same site/database set already approved for Forms.
forms_bronze_config_id = 97
expected_site_count = 115

bronze_schema = "P1Finance"
bronze_lakehouse = "bhg_bronze"
silver_lakehouse = "bhg_silver"
gold_lakehouse = "bhg_gold"

ingest_column = "IngestRunId"
site_column = "SiteCode"
database_column = "SourceDatabase"

default_lookback_days = 15


finance_tables = [
    {
        "display_name": "Bills",
        "method": "SaveBills",
        "source_table": "dbo.tblBill",
        "bronze_table": "br_samms_bills",
        "silver_schema": "pats",
        "silver_table": "tbl_Bills",
        "gold_schema": "pats",
        "gold_table": "tbl_Bills",
        "merge_keys": ["SiteCode", "BillID"],
        "is_incremental": 1,
        "lookback_days": default_lookback_days,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "year(billDate) >= year(@WorkDate - @LookbackDays) and billDate <= @WorkDate + 12 days",
        "source_filter_strategy": "BILLS_YEAR_WINDOW",
        "source_date_column": "billDate",
        "future_days": 12,
        "legacy_route": "SaveBills",
    },
    {
        "display_name": "PBI 3 Pay Auth",
        "method": "SaveAuths",
        "source_table": "dbo.tbl3PAYauth",
        "bronze_table": "br_samms_pbi3_pay_auth",
        "silver_schema": "pats",
        "silver_table": "tbl_pbi3PayAuth",
        "gold_schema": "pats",
        "gold_table": "tbl_pbi3PayAuth",
        "merge_keys": ["SiteCode", "TpaID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL",
        "legacy_route": "SaveAuths",
    },
    {
        "display_name": "VW 3P Bill Sub",
        "method": "SaveAuthBillsub",
        "source_table": "dbo.vw3pBillSub",
        "bronze_table": "br_samms_vw3p_bill_sub",
        "silver_schema": "pats",
        "silver_table": "tbl_vw3pBillSub",
        "gold_schema": "pats",
        "gold_table": "tbl_vw3pBillSub",
        "merge_keys": ["SiteCode", "DsID", "PayDEFAULTSUBMIT", "PyPAYERID", "PySUBSID", "PyGROUP", "CptMod", "Charge"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL_DISTINCT_WITH_NULL_SUBSTITUTIONS",
        "legacy_route": "BulkDartsSrvLoader/stg.sp_BillSubMerge",
        "legacy_ef_sites": ["B41", "B42"],
        "legacy_ef_merge_keys": ["SiteCode", "DsID", "PyPAYERID", "PySUBSID", "PyGROUP", "CptMod", "Charge"],
        "source_overrides": {
            "select_distinct": True,
            "null_substitutions": {"CptMod": ":(", "pySUBSID": ":(", "charge": 0},
        },
    },
    {
        "display_name": "FMP",
        "method": "SaveFmp",
        "source_table": "dbo.tblFMP",
        "bronze_table": "br_samms_fmp",
        "silver_schema": "pats",
        "silver_table": "tbl_Fmp",
        "gold_schema": "pats",
        "gold_table": "tbl_Fmp",
        "merge_keys": ["SiteCode", "FmpID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": None,
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL",
        "legacy_route": "SaveFmp",
    },
    {
        "display_name": "Payer Client History",
        "method": "SavePayerCltHistory",
        "source_table": "dbo.tblPayerCltHistory",
        "bronze_table": "br_samms_payer_clt_history",
        "silver_schema": "pats",
        "silver_table": "tbl_PayerCltHistory",
        "gold_schema": "pats",
        "gold_table": "tbl_PayerCltHistory",
        "merge_keys": ["SiteCode", "PchID"],
        "is_incremental": 1,
        "lookback_days": default_lookback_days,
        "checksum_column": None,
        "row_state_column": None,
        "lastmod_column": None,
        "source_where_template": "pyDtm is not null and pyDtm >= @WorkDate - @LookbackDays",
        "source_filter_strategy": "INCREMENTAL_BY_PYDTM",
        "source_date_column": "pyDtm",
        "legacy_route": "SavePayerCltHistory",
        "legacy_update_caveat": "UpdateRange(PCHUpd) is commented in SavePayorClient.cs.",
    },
    {
        "display_name": "Financial Hardship Application",
        "method": "SaveFinancialHardshipApplication",
        "source_table": "dbo.FinancialHardshipApplication",
        "bronze_table": "br_samms_financial_hardship_application",
        "silver_schema": "pats",
        "silver_table": "tbl_FinancialHardshipApplication",
        "gold_schema": "pats",
        "gold_table": "tbl_FinancialHardshipApplication",
        "merge_keys": ["SiteCode", "Id"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "delete_column": "IsDeleted",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL",
        "legacy_route": "SaveFinancialHardshipApplication",
    },
    {
        "display_name": "3P Eligibility",
        "method": "Save3pElig",
        "source_table": "dbo.Tbl3pElig",
        "bronze_table": "br_samms_3p_elig",
        "silver_schema": "pats",
        "silver_table": "tbl_3pElig",
        "gold_schema": "pats",
        "gold_table": "tbl_3pElig",
        "merge_keys": ["SiteCode", "EID"],
        "is_incremental": 1,
        "lookback_days": default_lookback_days,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "Year(edate) >= Year(@WorkDate - @LookbackDays)",
        "source_filter_strategy": "YEAR_WINDOW",
        "source_date_column": "edate",
        "legacy_route": "Save3pElig",
    },
    {
        "display_name": "Claim Line Item",
        "method": "SaveClaimLineItem",
        "source_table": "dbo.tbl3pClaimLineItem",
        "bronze_table": "br_samms_claim_line_item",
        "silver_schema": "pats",
        "silver_table": "tbl_ClaimLineItem",
        "gold_schema": "pats",
        "gold_table": "tbl_ClaimLineItem",
        "merge_keys": ["SiteCode", "TpcliID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "Legacy map has convert(date, tpcliDtmAdded) = @WorkDate; runner bulk path loads full source table.",
        "source_filter_strategy": "FULL_RUNNER_OVERRIDE",
        "legacy_route": "BulkDartsSrvLoader/stg.ClaimLineItemMerge",
    },
    {
        "display_name": "Claim Line Item Activity",
        "method": "SaveClaimLineItemActivity",
        "source_table": "dbo.tbl3pClaimLineItemActivity",
        "bronze_table": "br_samms_claim_line_item_activity",
        "silver_schema": "pats",
        "silver_table": "tbl_ClaimLineItemActivity",
        "gold_schema": "pats",
        "gold_table": "tbl_ClaimLineItemActivity",
        "merge_keys": ["SiteCode", "LiaID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "Legacy map has CONVERT(date, liaDtm) = @WorkDate; runner bulk path loads full source table.",
        "source_filter_strategy": "FULL_RUNNER_OVERRIDE",
        "legacy_route": "BulkDartsSrvLoader/stg.ClaimLineItemActivityMerge",
    },
    {
        "display_name": "Claims",
        "method": "SaveClaims",
        "source_table": "dbo.tbl3pClaim",
        "bronze_table": "br_samms_claims",
        "silver_schema": "pats",
        "silver_table": "tbl_Claims",
        "gold_schema": "pats",
        "gold_table": "tbl_Claims",
        "merge_keys": ["SiteCode", "TpcID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "Legacy map has Year(convert(date, tpcCreatedDate)) >= Year(@WorkDate); runner bulk path loads full source table for most sites.",
        "source_filter_strategy": "FULL_RUNNER_OVERRIDE_WITH_EF_EXCEPTIONS",
        "legacy_route": "BulkDartsSrvLoader/stg.ClaimsMerge",
        "legacy_ef_sites": ["VBRA", "VMIN", "VWBY", "VBRP"],
        "legacy_ef_lookback_days": default_lookback_days,
        "legacy_ef_where_template": "Year(convert(date, tpcCreatedDate)) >= Year(@WorkDate - @LookbackDays)",
    },
    {
        "display_name": "Payer Client",
        "method": "SavePayerClient",
        "source_table": "dbo.tblPayerClt",
        "bronze_table": "br_samms_payer_client",
        "silver_schema": "pats",
        "silver_table": "tbl_PayerClient",
        "gold_schema": "pats",
        "gold_table": "tbl_PayerClient",
        "merge_keys": ["SiteCode", "PyID", "PyCLTID"],
        "is_incremental": 1,
        "lookback_days": default_lookback_days,
        "payer_history_days": 360,
        "checksum_column": "RowChkSum",
        "row_state_column": None,
        "lastmod_column": "LastModAt",
        "active_column": "pyACTIVE",
        "source_where_template": "pyid in (select distinct pyID from dbo.tblPayerCltHistory where pyDtm >= DateAdd(d, -@PayerHistoryDays, @WorkDate - @LookbackDays)) or pyACTIVE = 1 or isnull(pyEND, GetDate()) >= DateAdd(d, -@PayerHistoryDays, @WorkDate - @LookbackDays) or pyEnd >= DateAdd(d, -@PayerHistoryDays, @WorkDate - @LookbackDays)",
        "source_filter_strategy": "PAYERCLIENT_360_DAY_OR_ACTIVE",
        "legacy_route": "SavePayerClient/RemovePayerClients",
        "merge_key_transforms": {"PyCLTID": "abs"},
        "inactive_source_table": "dbo.vw_PayerClt_INACTIVE",
    },
    {
        "display_name": "Diag10",
        "method": "SaveTblDiags",
        "source_table": "dbo.Tbldiag10",
        "bronze_table": "br_samms_tbldiag10",
        "silver_schema": "pats",
        "silver_table": "tbl_tbldiag10",
        "gold_schema": "pats",
        "gold_table": "tbl_tbldiag10",
        "merge_keys": ["SiteCode", "DgID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": None,
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL",
        "legacy_route": "BulkDartsSrvLoader/stg.sp_tblDiag10Merge",
        "naming_caveat": "Verify live BHG_DR naming: tbl_tbldiag10 vs tbl_TblDiag10 vs tbl_tblDiag10.",
    },
    {
        "display_name": "Client Demo 1",
        "method": "SaveClientDemo1var",
        "source_table": "dbo.tblClient",
        "bronze_table": "br_samms_client_demo1",
        "silver_schema": "pats",
        "silver_table": "tbl_ClientDemo1",
        "gold_schema": "pats",
        "gold_table": "tbl_ClientDemo1",
        "merge_keys": ["SiteCode", "ClientID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL_CLIENTDEMO_DIRECT",
        "legacy_route": "Legacy stg.ClientDemo -> stg.ClientDemoMerge1; Fabric method is separate, no shared stg target.",
        "clientdemo_part": "ClientDemo1",
    },
    {
        "display_name": "Client Demo 2",
        "method": "SaveClientDemo2",
        "source_table": "dbo.tblClient",
        "bronze_table": "br_samms_client_demo2",
        "silver_schema": "pats",
        "silver_table": "tbl_ClientDemo2",
        "gold_schema": "pats",
        "gold_table": "tbl_ClientDemo2",
        "merge_keys": ["SiteCode", "ClientID"],
        "is_incremental": 0,
        "lookback_days": None,
        "checksum_column": "RowChkSum",
        "row_state_column": "RowState",
        "lastmod_column": "LastModAt",
        "source_where_template": "1 = 1",
        "source_filter_strategy": "FULL_CLIENTDEMO_DIRECT",
        "legacy_route": "Legacy stg.ClientDemo -> stg.ClientDemoMerge2; Fabric method is separate, no shared stg target.",
        "clientdemo_part": "ClientDemo2",
    },
]


def full_table(lakehouse, schema_name, table_name):
    return f"{lakehouse}.{schema_name}.{table_name}"


def compact_json(payload):
    return json.dumps(payload, separators=(",", ":"))


def site_name_from_database(source_database):
    site_name = re.sub(r"^SAMMS-", "", source_database or "")
    site_name = re.sub(r"(?i)V[0-9]+$", "", site_name)
    return site_name.strip()


def make_task(
    task_config_id,
    config_id,
    task_name,
    method,
    auth_type,
    source_table,
    load_type,
    is_incremental,
    watermark_column,
    lookback_days,
    target_schema,
    target_table,
    target_path,
    execution_order,
    request_payload,
    dependency_task_config_id,
    site_code=None,
    database_name=None,
    site_name=None,
    is_active=1,
):
    return {
        "TaskConfigId": task_config_id,
        "ConfigId": config_id,
        "TaskName": task_name,
        "Endpoint": None,
        "Method": method,
        "AuthType": auth_type,
        "SourceTable": source_table,
        "PaginationEnabled": 0,
        "PaginationParam": None,
        "LoadType": load_type,
        "IsIncremental": is_incremental,
        "WatermarkColumn": watermark_column,
        "LookbackDays": lookback_days,
        "TargetSchema": target_schema,
        "TargetTable": target_table,
        "TargetPath": target_path,
        "ExecutionOrder": execution_order,
        "RetryCount": 0,
        "TimeoutSeconds": 43200,
        "RequestBody": compact_json(request_payload),
        "DependencyTaskConfigId": dependency_task_config_id,
        "SiteCode": site_code,
        "DataBaseName": database_name,
        "SiteName": site_name,
        "IsActive": is_active,
        "CreatedBy": created_by,
        "ModifiedBy": created_by,
    }


def common_request_payload(item, full_table_name):
    payload = {
        "full_table": full_table_name,
        "dq_keys": item["merge_keys"],
        "merge_keys": item["merge_keys"],
        "checksum_column": item.get("checksum_column"),
        "row_state_column": item.get("row_state_column"),
        "lastmod_column": item.get("lastmod_column"),
    }

    optional_keys = [
        "delete_column",
        "active_column",
        "merge_key_transforms",
        "legacy_ef_merge_keys",
        "legacy_ef_sites",
        "legacy_ef_lookback_days",
        "legacy_ef_where_template",
        "legacy_update_caveat",
        "naming_caveat",
        "inactive_source_table",
        "clientdemo_part",
    ]
    for key in optional_keys:
        if item.get(key) is not None:
            payload[key] = item[key]

    return payload


site_rows = (
    spark.table(taskconfig_table)
    .where(
        (F.col("ConfigId") == forms_bronze_config_id)
        & F.col("SiteCode").isNotNull()
        & F.col("DataBaseName").isNotNull()
    )
    .select(
        F.col("SiteCode").alias("site_code"),
        F.col("DataBaseName").alias("source_database"),
    )
    .dropDuplicates()
    .orderBy("site_code")
    .collect()
)

samms_sites = [
    {"site_code": row.site_code, "source_database": row.source_database}
    for row in site_rows
]

if len(samms_sites) != expected_site_count:
    raise Exception(
        f"Expected {expected_site_count} SAMMS sites from P1 Forms TaskConfig "
        f"ConfigId={forms_bronze_config_id}, found {len(samms_sites)}."
    )

site_codes = [site["site_code"] for site in samms_sites]
if len(site_codes) != len(set(site_codes)):
    raise Exception("Duplicate site_code values found in Forms TaskConfig site list.")


task_rows = []
task_id = start_task_config_id

for table_order, item in enumerate(finance_tables, start=1):
    bronze_full_table = full_table(bronze_lakehouse, bronze_schema, item["bronze_table"])
    silver_full_table = full_table(silver_lakehouse, item["silver_schema"], item["silver_table"])
    gold_full_table = full_table(gold_lakehouse, item["gold_schema"], item["gold_table"])
    merge_key_column = ",".join(item["merge_keys"])
    bronze_load_type = "INCREMENTAL" if item["is_incremental"] else "FULL"

    bronze_request = common_request_payload(item, bronze_full_table)
    bronze_request.update({
        "ingest_column": ingest_column,
        "site_column": site_column,
        "database_column": database_column,
        "source_table": item["source_table"],
        "source_where_template": item["source_where_template"],
        "source_filter_strategy": item["source_filter_strategy"],
        "source_date_column": item.get("source_date_column"),
        "source_overrides": item.get("source_overrides"),
        "future_days": item.get("future_days"),
        "payer_history_days": item.get("payer_history_days"),
        "legacy_route": item.get("legacy_route"),
    })

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=bronze_config_id,
            task_name=f"P1 Finance {item['display_name']} Bronze",
            method=item["method"],
            auth_type="SQLServer",
            source_table=item["source_table"],
            load_type=bronze_load_type,
            is_incremental=item["is_incremental"],
            watermark_column=merge_key_column,
            lookback_days=item["lookback_days"],
            target_schema=bronze_schema,
            target_table=item["bronze_table"],
            target_path=bronze_full_table,
            execution_order=table_order,
            request_payload=bronze_request,
            dependency_task_config_id=None,
            is_active=0,
        )
    )
    task_id += 1

    for site in samms_sites:
        task_rows.append(
            make_task(
                task_config_id=task_id,
                config_id=bronze_config_id,
                task_name=f"P1 Finance {item['display_name']} Bronze - {site['site_code']}",
                method=item["method"],
                auth_type="SQLServer",
                source_table=item["source_table"],
                load_type=bronze_load_type,
                is_incremental=item["is_incremental"],
                watermark_column=merge_key_column,
                lookback_days=item["lookback_days"],
                target_schema=bronze_schema,
                target_table=item["bronze_table"],
                target_path=bronze_full_table,
                execution_order=table_order,
                request_payload=bronze_request,
                dependency_task_config_id=None,
                site_code=site["site_code"],
                database_name=site["source_database"],
                site_name=site_name_from_database(site["source_database"]),
            )
        )
        task_id += 1

    silver_request = common_request_payload(item, silver_full_table)
    silver_request.update({
        "source_table": bronze_full_table,
        "target_table": silver_full_table,
    })

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=silver_config_id,
            task_name=f"P1 Finance {item['display_name']} Silver",
            method=item["method"],
            auth_type="Lakehouse",
            source_table=bronze_full_table,
            load_type="MERGE",
            is_incremental=item["is_incremental"],
            watermark_column=merge_key_column,
            lookback_days=item["lookback_days"],
            target_schema=item["silver_schema"],
            target_table=item["silver_table"],
            target_path=silver_full_table,
            execution_order=table_order,
            request_payload=silver_request,
            dependency_task_config_id=None,
        )
    )
    task_id += 1

    gold_request = common_request_payload(item, gold_full_table)
    gold_request.update({
        "source_table": silver_full_table,
        "target_table": gold_full_table,
    })

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=gold_config_id,
            task_name=f"P1 Finance {item['display_name']} Gold",
            method=item["method"],
            auth_type="Warehouse",
            source_table=silver_full_table,
            load_type="VERSIONED_FULL_OVERWRITE",
            is_incremental=0,
            watermark_column=merge_key_column,
            lookback_days=None,
            target_schema=item["gold_schema"],
            target_table=item["gold_table"],
            target_path=gold_full_table,
            execution_order=table_order,
            request_payload=gold_request,
            dependency_task_config_id=None,
        )
    )
    task_id += 1


expected_task_count = len(finance_tables) * (len(samms_sites) + 3)
if len(task_rows) != expected_task_count:
    raise Exception(f"Expected {expected_task_count} taskconfig rows, built {len(task_rows)}")

end_task_config_id = start_task_config_id + expected_task_count - 1
incoming_task_config_ids = [row["TaskConfigId"] for row in task_rows]

if incoming_task_config_ids[0] != 8700 or incoming_task_config_ids[-1] != 10351:
    raise Exception(
        f"Unexpected TaskConfigId range: {incoming_task_config_ids[0]}-{incoming_task_config_ids[-1]}"
    )

seen_methods = {row["Method"] for row in task_rows}
expected_methods = {item["method"] for item in finance_tables}
if seen_methods != expected_methods:
    raise Exception(f"Method mismatch: {sorted(seen_methods)} != {sorted(expected_methods)}")

if len(expected_methods) != 14:
    raise Exception(f"Expected 14 Finance methods, found {len(expected_methods)}")

for item in finance_tables:
    if not item["bronze_table"].startswith("br_"):
        raise Exception(f"Bronze table must start with br_: {item['bronze_table']}")
    if not item["silver_table"].startswith("tbl_"):
        raise Exception(f"Silver table must keep tbl_ target naming: {item['silver_table']}")
    if not item["gold_table"].startswith("tbl_"):
        raise Exception(f"Gold table must keep tbl_ target naming: {item['gold_table']}")

for row in task_rows:
    parsed_request_body = json.loads(row["RequestBody"])
    expected_merge_keys = row["WatermarkColumn"].split(",")

    if not row["Method"]:
        raise Exception(f"Missing Method for TaskConfigId={row['TaskConfigId']}")

    if not parsed_request_body.get("full_table"):
        raise Exception(f"Missing full_table in RequestBody for TaskConfigId={row['TaskConfigId']}")

    if parsed_request_body.get("merge_keys") != expected_merge_keys:
        raise Exception(
            f"merge_keys and WatermarkColumn do not match for TaskConfigId={row['TaskConfigId']}: "
            f"{parsed_request_body.get('merge_keys')} != {expected_merge_keys}"
        )

    if parsed_request_body.get("dq_keys") != expected_merge_keys:
        raise Exception(
            f"dq_keys and WatermarkColumn do not match for TaskConfigId={row['TaskConfigId']}: "
            f"{parsed_request_body.get('dq_keys')} != {expected_merge_keys}"
        )

    if row["ConfigId"] == bronze_config_id:
        for column_name in ["ingest_column", "site_column", "database_column", "source_table", "source_filter_strategy"]:
            if not parsed_request_body.get(column_name):
                raise Exception(f"Missing {column_name} in Bronze RequestBody for TaskConfigId={row['TaskConfigId']}")
        if row["SiteCode"] and not row["DataBaseName"]:
            raise Exception(f"Bronze row missing SiteCode/DataBaseName for TaskConfigId={row['TaskConfigId']}")

    if row["Method"] in ["SaveClientDemo1var", "SaveClientDemo2"] and row["TargetTable"].lower() == "clientdemo":
        raise Exception("ClientDemo must not target shared stg.ClientDemo in Fabric taskconfig.")


required_config_ids = [bronze_config_id, silver_config_id, gold_config_id]
existing_config_ids = {
    row.ConfigId
    for row in (
        spark.table(etlconfig_table)
        .where(F.col("ConfigId").isin(required_config_ids))
        .select("ConfigId")
        .collect()
    )
}

missing_config_ids = sorted(set(required_config_ids) - existing_config_ids)
if missing_config_ids:
    raise Exception(f"Missing etlconfig rows for ConfigId(s): {missing_config_ids}. Run ETLConfig setup first.")


conflicting_taskconfig_df = (
    spark.table(taskconfig_table)
    .where(F.col("TaskConfigId").between(start_task_config_id, end_task_config_id))
    .where(~F.col("ConfigId").isin(required_config_ids))
)

if conflicting_taskconfig_df.count() > 0:
    display(
        conflicting_taskconfig_df.select(
            "TaskConfigId",
            "ConfigId",
            "TaskName",
            "TargetSchema",
            "TargetTable",
            "IsActive",
        ).orderBy("TaskConfigId")
    )
    raise Exception(
        f"TaskConfigId range {start_task_config_id}-{end_task_config_id} is already used outside "
        f"ConfigIds {required_config_ids}. Choose a new start_task_config_id before running this setup."
    )


task_schema = StructType([
    StructField("TaskConfigId", LongType(), True),
    StructField("ConfigId", LongType(), True),
    StructField("TaskName", StringType(), True),
    StructField("Endpoint", StringType(), True),
    StructField("Method", StringType(), True),
    StructField("AuthType", StringType(), True),
    StructField("SourceTable", StringType(), True),
    StructField("PaginationEnabled", IntegerType(), True),
    StructField("PaginationParam", StringType(), True),
    StructField("LoadType", StringType(), True),
    StructField("IsIncremental", IntegerType(), True),
    StructField("WatermarkColumn", StringType(), True),
    StructField("LookbackDays", IntegerType(), True),
    StructField("TargetSchema", StringType(), True),
    StructField("TargetTable", StringType(), True),
    StructField("TargetPath", StringType(), True),
    StructField("ExecutionOrder", IntegerType(), True),
    StructField("RetryCount", IntegerType(), True),
    StructField("TimeoutSeconds", IntegerType(), True),
    StructField("RequestBody", StringType(), True),
    StructField("DependencyTaskConfigId", LongType(), True),
    StructField("SiteCode", StringType(), True),
    StructField("DataBaseName", StringType(), True),
    StructField("SiteName", StringType(), True),
    StructField("IsActive", IntegerType(), True),
    StructField("CreatedBy", StringType(), True),
    StructField("ModifiedBy", StringType(), True),
])

task_df = (
    spark.createDataFrame(task_rows, task_schema)
    .withColumn("CreatedAt", F.current_timestamp())
    .withColumn("ModifiedAt", F.current_timestamp())
)

task_cols = [field.name for field in task_df.schema.fields]
task_update = {
    column_name: f"source.{column_name}"
    for column_name in task_cols
    if column_name not in ["TaskConfigId", "CreatedAt", "CreatedBy"]
}
task_insert = {column_name: f"source.{column_name}" for column_name in task_cols}

DeltaTable.forName(spark, taskconfig_table).alias("target") \
    .merge(task_df.alias("source"), "target.TaskConfigId = source.TaskConfigId") \
    .whenMatchedUpdate(set=task_update) \
    .whenNotMatchedInsert(values=task_insert) \
    .execute()

# Merge preserves CreatedBy on matched rows; normalize audit columns for finance task rows.
spark.sql(f"""
UPDATE {taskconfig_table}
SET CreatedBy = '{created_by}', ModifiedBy = '{created_by}'
WHERE TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
""")


display(spark.sql(f"""
SELECT
    ConfigId,
    TargetTable,
    MIN(TaskConfigId) AS MinTaskConfigId,
    MAX(TaskConfigId) AS MaxTaskConfigId,
    COUNT(*) AS TaskCount,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveTaskCount,
    SUM(CASE WHEN SiteCode IS NOT NULL THEN 1 ELSE 0 END) AS SiteTaskCount
FROM {taskconfig_table}
WHERE TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
GROUP BY ConfigId, TargetTable
ORDER BY MinTaskConfigId
"""))

display(spark.sql(f"""
SELECT
    TaskConfigId,
    ConfigId,
    TaskName,
    Method,
    AuthType,
    SourceTable,
    LoadType,
    IsIncremental,
    WatermarkColumn,
    LookbackDays,
    TargetSchema,
    TargetTable,
    TargetPath,
    SiteCode,
    DataBaseName,
    SiteName,
    DependencyTaskConfigId,
    ExecutionOrder,
    IsActive,
    RequestBody
FROM {taskconfig_table}
WHERE TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
ORDER BY TaskConfigId
LIMIT 80
"""))
