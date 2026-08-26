# P1 Finance — Step 2: PascalCase merge_keys for Silver + Gold TaskConfig rows only.
# Run in a Fabric PySpark notebook attached to the Bronze lakehouse (bhg_bronze).
#
# Scope:
#   ConfigId 47 (Silver) and 48 (Gold) — 28 rows total (14 methods × 2 layers).
#   Does NOT touch ConfigId 46 (Bronze site rows).
#
# Updates per row:
#   WatermarkColumn, RequestBody.merge_keys, RequestBody.dq_keys
#   plus merge_key_transforms / legacy_ef_merge_keys where applicable.

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"

silver_config_id = 47
gold_config_id = 48
start_task_config_id = 8700
end_task_config_id = 10351

modified_by = "DEV_HA"

# PascalCase keys aligned with regenerated silver notebook TARGET_SCHEMA columns.
METHOD_SILVER_MERGE_KEYS = {
    "SaveBills": ["SiteCode", "BillID"],
    "SaveAuths": ["SiteCode", "TpaID"],
    "SaveAuthBillsub": [
        "SiteCode",
        "DsID",
        "PayDEFAULTSUBMIT",
        "PyPAYERID",
        "PySUBSID",
        "PyGROUP",
        "CptMod",
        "Charge",
    ],
    "SaveFmp": ["SiteCode", "FmpID"],
    "SavePayerCltHistory": ["SiteCode", "PchID"],
    "SaveFinancialHardshipApplication": ["SiteCode", "Id"],
    "Save3pElig": ["SiteCode", "EID"],
    "SaveClaimLineItem": ["SiteCode", "TpcliID"],
    "SaveClaimLineItemActivity": ["SiteCode", "LiaID"],
    "SaveClaims": ["SiteCode", "TpcID"],
    "SavePayerClient": ["SiteCode", "PyID", "PyCLTID"],
    "SaveTblDiags": ["SiteCode", "DgID"],
    "SaveClientDemo1var": ["SiteCode", "ClientID"],
    "SaveClientDemo2": ["SiteCode", "ClientID"],
}

METHOD_MERGE_KEY_TRANSFORMS = {
    "SavePayerClient": {"PyCLTID": "abs"},
}

METHOD_LEGACY_EF_MERGE_KEYS = {
    "SaveAuthBillsub": [
        "SiteCode",
        "DsID",
        "PyPAYERID",
        "PySUBSID",
        "PyGROUP",
        "CptMod",
        "Charge",
    ],
}


def compact_json(payload):
    return json.dumps(payload, separators=(",", ":"))


def parse_request_body(raw):
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return raw
    return json.loads(raw)


finance_methods = list(METHOD_SILVER_MERGE_KEYS.keys())

scope_filter = (
    (F.col("ConfigId").isin(silver_config_id, gold_config_id))
    & F.col("TaskConfigId").between(start_task_config_id, end_task_config_id)
    & F.col("Method").isin(finance_methods)
)

print("--- BEFORE: Silver + Gold merge_keys / WatermarkColumn ---")
display(
    spark.table(taskconfig_table)
    .where(scope_filter)
    .select(
        "TaskConfigId",
        "ConfigId",
        "Method",
        "TargetTable",
        "WatermarkColumn",
        F.get_json_object("RequestBody", "$.merge_keys").alias("merge_keys"),
        F.get_json_object("RequestBody", "$.dq_keys").alias("dq_keys"),
    )
    .orderBy("TaskConfigId")
)

rows = spark.table(taskconfig_table).where(scope_filter).collect()
if len(rows) != 28:
    raise Exception(
        f"Expected 28 Silver+Gold finance TaskConfig rows, found {len(rows)}. "
        "Verify TaskConfigId range and Method list before applying."
    )

updates = []
for row in rows:
    method = row["Method"]
    if method not in METHOD_SILVER_MERGE_KEYS:
        raise Exception(f"Unexpected Method in scope: {method} (TaskConfigId={row['TaskConfigId']})")

    new_keys = METHOD_SILVER_MERGE_KEYS[method]
    payload = parse_request_body(row["RequestBody"])

    payload["merge_keys"] = new_keys
    payload["dq_keys"] = new_keys

    if method in METHOD_MERGE_KEY_TRANSFORMS:
        payload["merge_key_transforms"] = METHOD_MERGE_KEY_TRANSFORMS[method]

    if method in METHOD_LEGACY_EF_MERGE_KEYS:
        payload["legacy_ef_merge_keys"] = METHOD_LEGACY_EF_MERGE_KEYS[method]

    updates.append(
        {
            "TaskConfigId": int(row["TaskConfigId"]),
            "WatermarkColumn": ",".join(new_keys),
            "RequestBody": compact_json(payload),
            "ModifiedBy": modified_by,
        }
    )

update_df = spark.createDataFrame(updates)

print("--- PLANNED UPDATES ---")
display(update_df.orderBy("TaskConfigId"))

DeltaTable.forName(spark, taskconfig_table).alias("target").merge(
    update_df.alias("source"),
    "target.TaskConfigId = source.TaskConfigId",
).whenMatchedUpdate(
    set={
        "WatermarkColumn": "source.WatermarkColumn",
        "RequestBody": "source.RequestBody",
        "ModifiedBy": "source.ModifiedBy",
        "ModifiedAt": "current_timestamp()",
    }
).execute()

print("--- AFTER: Silver + Gold merge_keys / WatermarkColumn ---")
after_df = (
    spark.table(taskconfig_table)
    .where(scope_filter)
    .select(
        "TaskConfigId",
        "ConfigId",
        "Method",
        "TargetTable",
        "WatermarkColumn",
        F.get_json_object("RequestBody", "$.merge_keys").alias("merge_keys"),
        F.get_json_object("RequestBody", "$.dq_keys").alias("dq_keys"),
        F.get_json_object("RequestBody", "$.merge_key_transforms").alias("merge_key_transforms"),
        F.get_json_object("RequestBody", "$.legacy_ef_merge_keys").alias("legacy_ef_merge_keys"),
        "ModifiedBy",
        "ModifiedAt",
    )
    .orderBy("TaskConfigId")
)
display(after_df)

# Validation: every row must have matching WatermarkColumn, merge_keys, dq_keys.
validation_rows = after_df.collect()
for row in validation_rows:
    method = row["Method"]
    expected_keys = METHOD_SILVER_MERGE_KEYS[method]
    expected_watermark = ",".join(expected_keys)
    actual_merge_keys = json.loads(row["merge_keys"]) if row["merge_keys"] else []
    actual_dq_keys = json.loads(row["dq_keys"]) if row["dq_keys"] else []

    if row["WatermarkColumn"] != expected_watermark:
        raise Exception(
            f"TaskConfigId={row['TaskConfigId']} WatermarkColumn mismatch: "
            f"{row['WatermarkColumn']} != {expected_watermark}"
        )
    if actual_merge_keys != expected_keys or actual_dq_keys != expected_keys:
        raise Exception(
            f"TaskConfigId={row['TaskConfigId']} merge_keys/dq_keys mismatch for {method}: "
            f"merge_keys={actual_merge_keys}, dq_keys={actual_dq_keys}, expected={expected_keys}"
        )

# Confirm Bronze rows were not touched (spot-check one method).
bronze_sample = spark.sql(f"""
SELECT TaskConfigId, ConfigId, Method, WatermarkColumn,
       get_json_object(RequestBody, '$.merge_keys') AS merge_keys
FROM {taskconfig_table}
WHERE ConfigId = 46
  AND Method = 'SaveBills'
  AND SiteCode IS NOT NULL
LIMIT 3
""")
print("--- BRONZE SPOT CHECK (should still be camelCase billID) ---")
display(bronze_sample)

print("PascalCase merge_keys update complete for 28 Silver + Gold TaskConfig rows.")
