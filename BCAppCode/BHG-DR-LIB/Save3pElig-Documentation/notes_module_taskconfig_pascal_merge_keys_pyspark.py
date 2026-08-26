# Notes — PascalCase dq_keys for Silver + Gold TaskConfig rows.
# Run in a Fabric PySpark notebook attached to the Bronze lakehouse (bhg_bronze).
#
# Scope:
#   ConfigId 35 (Silver) and ConfigId 36 (Gold) — 4 rows total (2 methods × 2 layers).
#   Does NOT touch ConfigId 34 (Bronze site rows).
#
# TaskConfigIds (from taskconfigrows.txt):
#   145 — 3pArnote Silver     dq_keys: SiteCode, ArnID
#   146 — 3pClaimNote Silver  dq_keys: SiteCode, Tpcn
#   147 — 3pArnote Gold       dq_keys: SiteCode, ArnID
#   148 — 3pClaimNote Gold    dq_keys: SiteCode, TpcnTPCID  (gold warehouse key unchanged except PascalCase)

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
silver_config_id = 35
gold_config_id = 36
modified_by = "DEV_HA"

NOTES_SILVER_GOLD_TASK_CONFIG_IDS = [145, 146, 147, 148]

# Silver ClaimNote merges on Tpcn (Brian/C# parity). Gold ClaimNote keeps TpcnTPCID as dq key.
TASK_CONFIG_MERGE_KEYS = {
    145: ["SiteCode", "ArnID"],
    146: ["SiteCode", "Tpcn"],
    147: ["SiteCode", "ArnID"],
    148: ["SiteCode", "TpcnTPCID"],
}


def compact_json(payload):
    return json.dumps(payload, separators=(",", ":"))


def parse_request_body(raw):
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return raw
    return json.loads(raw)


scope_filter = (
    F.col("TaskConfigId").isin(NOTES_SILVER_GOLD_TASK_CONFIG_IDS)
    & F.col("ConfigId").isin(silver_config_id, gold_config_id)
)

print("--- BEFORE: Silver + Gold dq_keys ---")
display(
    spark.table(taskconfig_table)
    .where(scope_filter)
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
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
if len(rows) != len(NOTES_SILVER_GOLD_TASK_CONFIG_IDS):
    raise Exception(
        f"Expected {len(NOTES_SILVER_GOLD_TASK_CONFIG_IDS)} Notes Silver+Gold TaskConfig rows, "
        f"found {len(rows)}. Verify TaskConfigIds 145–148 before applying."
    )

found_ids = {int(row["TaskConfigId"]) for row in rows}
expected_ids = set(NOTES_SILVER_GOLD_TASK_CONFIG_IDS)
if found_ids != expected_ids:
    raise Exception(
        f"TaskConfigId mismatch. Expected {sorted(expected_ids)}; found {sorted(found_ids)}."
    )

updates = []
for row in rows:
    task_config_id = int(row["TaskConfigId"])
    if task_config_id not in TASK_CONFIG_MERGE_KEYS:
        raise Exception(f"No merge key mapping for TaskConfigId={task_config_id}")

    new_keys = TASK_CONFIG_MERGE_KEYS[task_config_id]
    payload = parse_request_body(row["RequestBody"])
    payload["dq_keys"] = new_keys
    if "merge_keys" in payload:
        payload["merge_keys"] = new_keys

    updates.append(
        {
            "TaskConfigId": task_config_id,
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
        "RequestBody": "source.RequestBody",
        "ModifiedBy": "source.ModifiedBy",
        "ModifiedAt": "current_timestamp()",
    }
).execute()

print("--- AFTER: Silver + Gold dq_keys ---")
after_df = (
    spark.table(taskconfig_table)
    .where(scope_filter)
    .select(
        "TaskConfigId",
        "ConfigId",
        "TaskName",
        "Method",
        F.get_json_object("RequestBody", "$.dq_keys").alias("dq_keys"),
        "ModifiedBy",
        "ModifiedAt",
    )
    .orderBy("TaskConfigId")
)
display(after_df)

for row in after_df.collect():
    expected = TASK_CONFIG_MERGE_KEYS[int(row["TaskConfigId"])]
    actual = json.loads(row["dq_keys"]) if row["dq_keys"] else []
    if actual != expected:
        raise Exception(
            f"TaskConfigId={row['TaskConfigId']} dq_keys mismatch: {actual} != {expected}"
        )

print("Done. Updated 4 Notes Silver+Gold TaskConfig rows. Bronze (ConfigId 34) unchanged.")
