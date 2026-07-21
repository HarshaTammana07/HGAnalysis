# P1 Reference module TaskConfig setup
# Run this in a Fabric Spark notebook attached to the Bronze lakehouse.
#
# ID layout:
#   For each reference table:
#     1 inactive generic Bronze row, 115 active Bronze site rows, then 1 Silver row, then 1 Gold row.
#   Start TaskConfigId: 4652
#   End TaskConfigId:   5713

import json
import re

from delta.tables import DeltaTable
from pyspark.sql import functions as F
from pyspark.sql.types import IntegerType, LongType, StringType, StructField, StructType


etlconfig_table = "bhg_bronze.meta.etlconfig"
taskconfig_table = "bhg_bronze.meta.taskconfig"

created_by = "Harsha"
start_task_config_id = 4652

bronze_config_id = 88
silver_config_id = 89
gold_config_id = 90

bronze_schema = "P1Reference"
bronze_lakehouse = "bhg_bronze"
silver_lakehouse = "bhg_silver"
gold_lakehouse = "bhg_gold"

ingest_column = "IngestRunId"
site_column = "SiteCode"
database_column = "SourceDatabase"


# Same SAMMS site JSON used in Framework/etlconfigandtaskconfigsqls.
samms_sites_json = r'''
[
  {"site_code": "AHK", "source_database": "SAMMS-Ahoskie"},
  {"site_code": "B12B", "source_database": "SAMMS-ColoradoSpringsV5"},
  {"site_code": "B24", "source_database": "SAMMS-PaintsvilleV5"},
  {"site_code": "B25", "source_database": "SAMMS-PikevilleV5"},
  {"site_code": "B26", "source_database": "SAMMS-HazardV5"},
  {"site_code": "B27", "source_database": "SAMMS-SavannahV6"},
  {"site_code": "B28", "source_database": "SAMMS-WestPlainesV5"},
  {"site_code": "B29", "source_database": "SAMMS-PoplarBluffV5"},
  {"site_code": "B30", "source_database": "SAMMS-KCNv5"},
  {"site_code": "B31", "source_database": "SAMMS-DyersburgV5"},
  {"site_code": "B33", "source_database": "SAMMS-Paducah"},
  {"site_code": "B34", "source_database": "SAMMS-CorbinV5"},
  {"site_code": "B35", "source_database": "SAMMS-LexingtonV5"},
  {"site_code": "B35A", "source_database": "SAMMS-BereaV5"},
  {"site_code": "B36", "source_database": "SAMMS-AshevilleV5"},
  {"site_code": "B37", "source_database": "SAMMS-ClydeV5"},
  {"site_code": "B38", "source_database": "SAMMS-SpartanburgV5"},
  {"site_code": "B39", "source_database": "SAMMS-AikenV5"},
  {"site_code": "B41", "source_database": "SAMMS-ChesapeakeV5"},
  {"site_code": "B42", "source_database": "SAMMS-VirginiaBeachV5"},
  {"site_code": "B42A", "source_database": "SAMMS-NewportNewsV5"},
  {"site_code": "B42B", "source_database": "SAMMS-FranklinV5"},
  {"site_code": "B42C", "source_database": "SAMMS-GlenAllenV5"},
  {"site_code": "B42D", "source_database": "SAMMS-ChesapeakeSouthV5"},
  {"site_code": "B44", "source_database": "SAMMS-AlbanyV5"},
  {"site_code": "B45", "source_database": "SAMMS-TiftonV5"},
  {"site_code": "B46", "source_database": "SAMMS-WashingtonDCv5"},
  {"site_code": "B47", "source_database": "SAMMS-MobileV5"},
  {"site_code": "B48", "source_database": "SAMMS-TuscaloosaV5"},
  {"site_code": "B51", "source_database": "SAMMS-NorthLittleRockV6"},
  {"site_code": "B52", "source_database": "SAMMS-JacksonGAV5"},
  {"site_code": "B54", "source_database": "SAMMS-GadsdenV5"},
  {"site_code": "B55", "source_database": "SAMMS-ShoalsV5"},
  {"site_code": "B57", "source_database": "SAMMS-Pawtucket"},
  {"site_code": "B57A", "source_database": "SAMMS-Johnston"},
  {"site_code": "B57B", "source_database": "SAMMS-Middletown"},
  {"site_code": "B57C", "source_database": "SAMMS-Providence"},
  {"site_code": "B57D", "source_database": "SAMMS-Westerly"},
  {"site_code": "B66A", "source_database": "SAMMS-Bremen"},
  {"site_code": "B72", "source_database": "SAMMS-Mobile-OBOT"},
  {"site_code": "B73", "source_database": "SAMMS-Montgomery"},
  {"site_code": "B75", "source_database": "SAMMS-LawrenceV6"},
  {"site_code": "B76", "source_database": "SAMMS-Huntsville-OBOT"},
  {"site_code": "BAT", "source_database": "SAMMS-Batesville"},
  {"site_code": "BG", "source_database": "SAMMS-BowlingGreen"},
  {"site_code": "BOI", "source_database": "SAMMS-Boise"},
  {"site_code": "CBCO", "source_database": "SAMMS-CoeurdAleneV6"},
  {"site_code": "CON", "source_database": "SAMMS-Conway"},
  {"site_code": "D07", "source_database": "SAMMS-KnoxvilleV6"},
  {"site_code": "D08", "source_database": "SAMMS-MadisonV6"},
  {"site_code": "D09", "source_database": "SAMMS-MurfreesboroV6"},
  {"site_code": "DA", "source_database": "SAMMS-Davenport"},
  {"site_code": "DM", "source_database": "SAMMS-DesMoines"},
  {"site_code": "DRD-CO", "source_database": "SAMMS-ColumbiaV5"},
  {"site_code": "DRD-KC", "source_database": "SAMMS-KCv5"},
  {"site_code": "DRD-KVB", "source_database": "SAMMS-KVBv5"},
  {"site_code": "DRD-KVC", "source_database": "SAMMS-KVCv5"},
  {"site_code": "DRD-NOLA", "source_database": "SAMMS-NOLAv5"},
  {"site_code": "DRD-SF", "source_database": "SAMMS-SFv5"},
  {"site_code": "ELC", "source_database": "SAMMS-ElizabethCity"},
  {"site_code": "ET", "source_database": "SAMMS-Elizabethtown"},
  {"site_code": "FAY", "source_database": "SAMMS-Fayetteville"},
  {"site_code": "FR", "source_database": "SAMMS-Frankfort"},
  {"site_code": "FS", "source_database": "SAMMS-FortSmith"},
  {"site_code": "FW", "source_database": "SAMMS-FortWayne"},
  {"site_code": "GAL", "source_database": "SAMMS-Gaylord"},
  {"site_code": "HGT", "source_database": "SAMMS-Hagerstown"},
  {"site_code": "HNT", "source_database": "SAMMS-Huntsville"},
  {"site_code": "HS", "source_database": "SAMMS-HotSprings"},
  {"site_code": "JON", "source_database": "SAMMS-Jonesboro"},
  {"site_code": "LAN", "source_database": "SAMMS-Lansing"},
  {"site_code": "LO", "source_database": "SAMMS-Louisville"},
  {"site_code": "LV1", "source_database": "SAMMS-Cheyenne"},
  {"site_code": "LV2", "source_database": "SAMMS-DesertInn"},
  {"site_code": "LV3", "source_database": "SAMMS-McDaniel"},
  {"site_code": "MNRE", "source_database": "SAMMS-Monroe"},
  {"site_code": "MP", "source_database": "SAMMS-MtPleasant"},
  {"site_code": "MRD", "source_database": "SAMMS-Meridian"},
  {"site_code": "NC", "source_database": "SAMMS-NorthCharleston"},
  {"site_code": "NLR", "source_database": "SAMMS-NLROBOT"},
  {"site_code": "PH", "source_database": "SAMMS-Phoenix"},
  {"site_code": "RE", "source_database": "SAMMS-Reno"},
  {"site_code": "RMD", "source_database": "SAMMS-Richmond"},
  {"site_code": "SFN", "source_database": "SAMMS-SFNv5"},
  {"site_code": "SHP", "source_database": "SAMMS-Shreveport"},
  {"site_code": "STN", "source_database": "SAMMS-Staunton"},
  {"site_code": "STVN", "source_database": "SAMMS-Stevenson"},
  {"site_code": "TE", "source_database": "SAMMS-Tempe"},
  {"site_code": "TEX", "source_database": "SAMMS-Texarkana"},
  {"site_code": "TTCA", "source_database": "SAMMS-BessemerV5"},
  {"site_code": "TTCB", "source_database": "SAMMS-CullmanV5"},
  {"site_code": "TTCC", "source_database": "SAMMS-GrandBay"},
  {"site_code": "TU", "source_database": "SAMMS-Tucson"},
  {"site_code": "V1", "source_database": "SAMMS-VCPHCS-I-MemphisV5"},
  {"site_code": "V10", "source_database": "SAMMS-BoulderV5"},
  {"site_code": "V10A", "source_database": "SAMMS-FortCollinsV5"},
  {"site_code": "V11", "source_database": "SAMMS-VCPHCS-XI-NorthDenverV5"},
  {"site_code": "V12", "source_database": "SAMMS-VCPHCS-XII-DowntownDenverV5"},
  {"site_code": "V12A", "source_database": "SAMMS-CentennialV5"},
  {"site_code": "V14", "source_database": "SAMMS-VCPHCS-XIV-BridgewayV5"},
  {"site_code": "V15", "source_database": "SAMMS-JoplinV5"},
  {"site_code": "V17", "source_database": "SAMMS-ColumbiaTNv5"},
  {"site_code": "V19", "source_database": "SAMMS-JacksonV5"},
  {"site_code": "V20", "source_database": "SAMMS-ParisV5"},
  {"site_code": "V21", "source_database": "SAMMS-RaleighV5"},
  {"site_code": "V5", "source_database": "SAMMS-NONTCv5"},
  {"site_code": "V5B", "source_database": "SAMMS-HoumaV6"},
  {"site_code": "V6", "source_database": "SAMMS-LCv5"},
  {"site_code": "V8", "source_database": "SAMMS-VCPHCS-VIII-MemphisV5"},
  {"site_code": "V9", "source_database": "SAMMS-NashvilleV5"},
  {"site_code": "VBRA", "source_database": "SAMMS-BrainerdV6"},
  {"site_code": "VBRP", "source_database": "SAMMS-BrooklynParkV6"},
  {"site_code": "VMIN", "source_database": "SAMMS-MinneapolisV6"},
  {"site_code": "VWBY", "source_database": "SAMMS-WoodburyV6"},
  {"site_code": "WIL", "source_database": "SAMMS-Wilson"}
]
'''


reference_tables = [
    {
        "display_name": "Clinic",
        "method": "SaveClinic",
        "source_table": "dbo.tblClinic",
        "bronze_table": "br_samms_clinic",
        "silver_schema": "ctrl",
        "silver_table": "sl_clinic",
        "gold_schema": "ctrl",
        "gold_table": "gd_clinic",
        "dq_keys": ["SiteCode", "PKEY"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "3P Setup",
        "method": "Save3pSetup",
        "source_table": "dbo.tbl3PSETUP",
        "bronze_table": "br_samms_3p_setup",
        "silver_schema": "ctrl",
        "silver_table": "sl_3p_setup",
        "gold_schema": "ctrl",
        "gold_table": "gd_3p_setup",
        "dq_keys": ["SiteCode", "pID"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Codes",
        "method": "SaveCodes",
        "source_table": "dbo.tblCodes",
        "bronze_table": "br_samms_codes",
        "silver_schema": "pats",
        "silver_table": "sl_codes",
        "gold_schema": "pats",
        "gold_table": "gd_codes",
        "dq_keys": ["SiteCode", "cdeID"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Services",
        "method": "SaveServices",
        "source_table": "dbo.tblSERVICES",
        "bronze_table": "br_samms_services",
        "silver_schema": "pats",
        "silver_table": "sl_services",
        "gold_schema": "pats",
        "gold_table": "gd_services",
        "dq_keys": ["SiteCode", "sID"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Dropdown List Items",
        "method": "SavedropDownListItems",
        "source_table": "dbo.DroDownListItems",
        "bronze_table": "br_samms_dropdown_list_items",
        "silver_schema": "ctrl",
        "silver_table": "sl_dropdown_list_items",
        "gold_schema": "ctrl",
        "gold_table": "gd_dropdown_list_items",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Custom Answers",
        "method": "SaveCustomAnswers",
        "source_table": "dbo.tblCUSTOMANSWERS",
        "bronze_table": "br_samms_custom_answers",
        "silver_schema": "pats",
        "silver_table": "sl_custom_answers",
        "gold_schema": "pats",
        "gold_table": "gd_custom_answers",
        "dq_keys": ["SiteCode", "caID", "caQID", "caCLTID"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Custom Questions",
        "method": "SaveCustomQuestions",
        "source_table": "dbo.tblCUSTOMQUESTIONS",
        "bronze_table": "br_samms_custom_questions",
        "silver_schema": "pats",
        "silver_table": "sl_custom_questions",
        "gold_schema": "pats",
        "gold_table": "gd_custom_questions",
        "dq_keys": ["SiteCode", "cID"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "PreAdmission V6",
        "method": "SavePreAdmissionV6",
        "source_table": "dbo.SF_PatientPreAdmission",
        "bronze_table": "br_samms_pre_admission_v6",
        "silver_schema": "ayx",
        "silver_table": "sl_pre_admission_v6",
        "gold_schema": "ayx",
        "gold_table": "gd_pre_admission_v6",
        "dq_keys": ["SiteCode", "PreAdmissionid", "Clientid"],
        "is_incremental": 0,
        "lookback_days": None,
    },
    {
        "display_name": "Preadmission Referral Source",
        "method": "SavePreAdminReferrals",
        "source_table": "dbo.SF_PatientPreadmissionReferralSource",
        "bronze_table": "br_samms_preadmission_referral_source",
        "silver_schema": "pats",
        "silver_table": "sl_preadmission_referral_source",
        "gold_schema": "pats",
        "gold_table": "gd_preadmission_referral_source",
        "dq_keys": ["SiteCode", "Id"],
        "is_incremental": 1,
        "lookback_days": 515,
    },
]


def full_table(lakehouse, schema_name, table_name):
    return f"{lakehouse}.{schema_name}.{table_name}"


def request_body(payload):
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
        "RequestBody": request_body(request_payload),
        "DependencyTaskConfigId": dependency_task_config_id,
        "SiteCode": site_code,
        "DataBaseName": database_name,
        "SiteName": site_name,
        "IsActive": is_active,
        "CreatedBy": created_by,
        "ModifiedBy": created_by,
    }


samms_sites = json.loads(samms_sites_json)
if len(samms_sites) != 115:
    raise Exception(f"Expected 115 SAMMS sites from framework JSON, found {len(samms_sites)}")

site_codes = [site["site_code"] for site in samms_sites]
if len(site_codes) != len(set(site_codes)):
    raise Exception("Duplicate site_code values found in SAMMS site JSON.")


task_rows = []
task_id = start_task_config_id

for table_order, item in enumerate(reference_tables, start=1):
    bronze_full_table = full_table(bronze_lakehouse, bronze_schema, item["bronze_table"])
    silver_full_table = full_table(silver_lakehouse, item["silver_schema"], item["silver_table"])
    gold_full_table = full_table(gold_lakehouse, item["gold_schema"], item["gold_table"])
    dq_watermark_column = ",".join(item["dq_keys"])

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=bronze_config_id,
            task_name=f"P1 Reference {item['display_name']} Bronze",
            method=item["method"],
            auth_type="SQLServer",
            source_table=item["source_table"],
            load_type="INCREMENTAL" if item["is_incremental"] else "FULL",
            is_incremental=item["is_incremental"],
            watermark_column=dq_watermark_column,
            lookback_days=item["lookback_days"],
            target_schema=bronze_schema,
            target_table=item["bronze_table"],
            target_path=bronze_full_table,
            execution_order=table_order,
            request_payload={
                "full_table": bronze_full_table,
                "ingest_column": ingest_column,
                "site_column": site_column,
                "database_column": database_column,
                "dq_keys": item["dq_keys"],
            },
            dependency_task_config_id=None,
            is_active=0,
        )
    )
    task_id += 1

    for site_order, site in enumerate(samms_sites, start=1):
        task_rows.append(
            make_task(
                task_config_id=task_id,
                config_id=bronze_config_id,
                task_name=f"P1 Reference {item['display_name']} Bronze - {site['site_code']}",
                method=item["method"],
                auth_type="SQLServer",
                source_table=item["source_table"],
                load_type="INCREMENTAL" if item["is_incremental"] else "FULL",
                is_incremental=item["is_incremental"],
                watermark_column=dq_watermark_column,
                lookback_days=item["lookback_days"],
                target_schema=bronze_schema,
                target_table=item["bronze_table"],
                target_path=bronze_full_table,
                execution_order=table_order,
                request_payload={
                    "full_table": bronze_full_table,
                    "ingest_column": ingest_column,
                    "site_column": site_column,
                    "database_column": database_column,
                    "dq_keys": item["dq_keys"],
                },
                dependency_task_config_id=None,
                site_code=site["site_code"],
                database_name=site["source_database"],
                site_name=site_name_from_database(site["source_database"]),
            )
        )
        task_id += 1

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=silver_config_id,
            task_name=f"P1 Reference {item['display_name']} Silver",
            method=item["method"],
            auth_type="Lakehouse",
            source_table=bronze_full_table,
            load_type="MERGE",
            is_incremental=item["is_incremental"],
            watermark_column=dq_watermark_column,
            lookback_days=item["lookback_days"],
            target_schema=item["silver_schema"],
            target_table=item["silver_table"],
            target_path=silver_full_table,
            execution_order=table_order,
            request_payload={
                "full_table": silver_full_table,
                "dq_keys": item["dq_keys"],
            },
            dependency_task_config_id=None,
        )
    )
    task_id += 1

    task_rows.append(
        make_task(
            task_config_id=task_id,
            config_id=gold_config_id,
            task_name=f"P1 Reference {item['display_name']} Gold",
            method=item["method"],
            auth_type="Warehouse",
            source_table=silver_full_table,
            load_type="VERSIONED_FULL_OVERWRITE",
            is_incremental=0,
            watermark_column=dq_watermark_column,
            lookback_days=None,
            target_schema=item["gold_schema"],
            target_table=item["gold_table"],
            target_path=gold_full_table,
            execution_order=table_order,
            request_payload={
                "full_table": item["gold_table"],
                "dq_keys": item["dq_keys"],
            },
            dependency_task_config_id=None,
        )
    )
    task_id += 1


expected_task_count = len(reference_tables) * (len(samms_sites) + 3)
if len(task_rows) != expected_task_count:
    raise Exception(f"Expected {expected_task_count} taskconfig rows, built {len(task_rows)}")

end_task_config_id = start_task_config_id + expected_task_count - 1
incoming_task_config_ids = [row["TaskConfigId"] for row in task_rows]

if incoming_task_config_ids[0] != 4652 or incoming_task_config_ids[-1] != 5713:
    raise Exception(
        f"Unexpected TaskConfigId range: {incoming_task_config_ids[0]}-{incoming_task_config_ids[-1]}"
    )

for row in task_rows:
    parsed_request_body = json.loads(row["RequestBody"])
    expected_dq_keys = row["WatermarkColumn"].split(",")

    if not row["Method"]:
        raise Exception(f"Missing Method for TaskConfigId={row['TaskConfigId']}")

    if not parsed_request_body.get("full_table"):
        raise Exception(f"Missing full_table in RequestBody for TaskConfigId={row['TaskConfigId']}")

    if parsed_request_body.get("dq_keys") != expected_dq_keys:
        raise Exception(
            f"dq_keys and WatermarkColumn do not match for TaskConfigId={row['TaskConfigId']}: "
            f"{parsed_request_body.get('dq_keys')} != {expected_dq_keys}"
        )

    if row["ConfigId"] == bronze_config_id:
        for column_name in ["ingest_column", "site_column", "database_column"]:
            if not parsed_request_body.get(column_name):
                raise Exception(f"Missing {column_name} in Bronze RequestBody for TaskConfigId={row['TaskConfigId']}")
        if row["SiteCode"] and not row["DataBaseName"]:
            raise Exception(f"Bronze row missing SiteCode/DataBaseName for TaskConfigId={row['TaskConfigId']}")


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
        "ConfigIds 88/89/90. Choose a new start_task_config_id before running this setup."
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
    SiteCode,
    DataBaseName,
    SiteName,
    DependencyTaskConfigId,
    ExecutionOrder,
    IsActive
FROM {taskconfig_table}
WHERE TaskConfigId BETWEEN {start_task_config_id} AND {end_task_config_id}
ORDER BY TaskConfigId
LIMIT 50
"""))
