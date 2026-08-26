# P1 Reference — Step 2: PascalCase merge_keys for Silver + Gold TaskConfig rows only.
# Run in a Fabric PySpark notebook attached to the Bronze lakehouse (bhg_bronze).
#
# Scope:
#   ConfigId 89 (Silver) and 90 (Gold) — 18 rows total (9 methods × 2 layers).
#   Does NOT touch ConfigId 88 (Bronze site rows).
#
# TaskConfigIds (from taskcomfigrefrencecurrentrows.txt):
#   Silver: 4768, 4886, 5004, 5122, 5240, 5358, 5476, 5594, 5712
#   Gold:   4769, 4887, 5005, 5123, 5241, 5359, 5477, 5595, 5713
#
# Updates per row:
#   WatermarkColumn, RequestBody.merge_keys, RequestBody.dq_keys
#   (preserves existing RequestBody fields such as full_table)
#
# Also fixes Gold SaveCustomAnswers (5359): adds missing CaQID to match Silver + C# lookup.

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"

silver_config_id = 89
gold_config_id = 90
bronze_config_id = 88

modified_by = "DEV_HA"

# Exact Silver + Gold TaskConfigIds from taskcomfigrefrencecurrentrows.txt
REFERENCE_SILVER_GOLD_TASK_CONFIG_IDS = [
    4768,
    4769,
    4886,
    4887,
    5004,
    5005,
    5122,
    5123,
    5240,
    5241,
    5358,
    5359,
    5476,
    5477,
    5594,
    5595,
    5712,
    5713,
]

# PascalCase keys aligned with regenerated reference silver notebook TARGET_COLUMNS.
METHOD_MERGE_KEYS = {
    "SaveClinic": ["SiteCode", "PKEY"],
    "Save3pSetup": ["SiteCode", "PID"],
    "SaveCodes": ["SiteCode", "CdeID"],
    "SaveServices": ["SiteCode", "SID"],
    "SavedropDownListItems": ["SiteCode", "Id"],
    "SaveCustomAnswers": ["SiteCode", "CaID", "CaQID", "CaCLTID"],
    "SaveCustomQuestions": ["SiteCode", "CID"],
    "SavePreAdmissionV6": ["SiteCode", "PreAdmissionid", "Clientid"],
    "SavePreAdminReferrals": ["SiteCode", "Id"],
}

reference_methods = list(METHOD_MERGE_KEYS.keys())


def compact_json(payload):
    return json.dumps(payload, separators=(",", ":"))


def parse_request_body(raw):
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return raw
    return json.loads(raw)


scope_filter = (
    F.col("TaskConfigId").isin(REFERENCE_SILVER_GOLD_TASK_CONFIG_IDS)
    & F.col("ConfigId").isin(silver_config_id, gold_config_id)
    & F.col("Method").isin(reference_methods)
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
        F.get_json_object("RequestBody", "$.full_table").alias("full_table"),
    )
    .orderBy("TaskConfigId")
)

rows = spark.table(taskconfig_table).where(scope_filter).collect()
if len(rows) != 18:
    raise Exception(
        f"Expected 18 Silver+Gold reference TaskConfig rows, found {len(rows)}. "
        "Verify TaskConfigId list and ConfigId 89/90 before applying."
    )

found_ids = {int(row["TaskConfigId"]) for row in rows}
expected_ids = set(REFERENCE_SILVER_GOLD_TASK_CONFIG_IDS)
if found_ids != expected_ids:
    raise Exception(
        f"TaskConfigId mismatch. Expected {sorted(expected_ids)}; found {sorted(found_ids)}."
    )

updates = []
for row in rows:
    method = row["Method"]
    if method not in METHOD_MERGE_KEYS:
        raise Exception(f"Unexpected Method in scope: {method} (TaskConfigId={row['TaskConfigId']})")

    new_keys = METHOD_MERGE_KEYS[method]
    payload = parse_request_body(row["RequestBody"])

    payload["merge_keys"] = new_keys
    payload["dq_keys"] = new_keys

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
        F.get_json_object("RequestBody", "$.full_table").alias("full_table"),
        "ModifiedBy",
        "ModifiedAt",
    )
    .orderBy("TaskConfigId")
)
display(after_df)

validation_rows = after_df.collect()
for row in validation_rows:
    method = row["Method"]
    expected_keys = METHOD_MERGE_KEYS[method]
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

bronze_sample = spark.sql(f"""
SELECT TaskConfigId, ConfigId, Method, WatermarkColumn,
       get_json_object(RequestBody, '$.merge_keys') AS merge_keys,
       get_json_object(RequestBody, '$.dq_keys') AS dq_keys
FROM {taskconfig_table}
WHERE ConfigId = {bronze_config_id}
  AND Method = 'SaveCodes'
  AND SiteCode IS NOT NULL
LIMIT 3
""")
print("--- BRONZE SPOT CHECK (should still be legacy camelCase cdeID) ---")
display(bronze_sample)

print("PascalCase merge_keys update complete for 18 Silver + Gold reference TaskConfig rows.")
