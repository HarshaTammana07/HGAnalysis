# P1 Finance — PascalCase merge_keys for Bronze TaskConfig rows (ConfigId 46).
# Run in a Fabric PySpark notebook attached to the Bronze lakehouse (bhg_bronze).
#
# Run AFTER bronze Copy pipeline mappings use PascalCase sink column names.
# Run BEFORE bronze table reload.
#
# Scope: all Finance Bronze rows (template + site rows) for 14 methods (~1,624 rows).
# Does NOT touch ConfigId 47 (Silver) or 48 (Gold).

import json

from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"

bronze_config_id = 46

modified_by = "DEV_HA"

METHOD_MERGE_KEYS = {
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

finance_methods = list(METHOD_MERGE_KEYS.keys())

# All Finance Bronze rows: ConfigId 46 + one of the 14 Finance methods.
# Includes inactive template rows (SiteCode IS NULL) and all site rows (115 per method).
scope_filter = (F.col("ConfigId") == bronze_config_id) & F.col("Method").isin(finance_methods)


def compact_json(payload):
    return json.dumps(payload, separators=(",", ":"))


def parse_request_body(raw):
    if raw is None:
        return {}
    if isinstance(raw, dict):
        return raw
    return json.loads(raw)


print("--- BEFORE: Bronze merge_keys sample (one row per method) ---")
display(
    spark.table(taskconfig_table)
    .where(scope_filter)
    .select(
        "TaskConfigId",
        "Method",
        "SiteCode",
        "WatermarkColumn",
        F.get_json_object("RequestBody", "$.merge_keys").alias("merge_keys"),
    )
    .where(F.col("SiteCode").isNull())
    .orderBy("TaskConfigId")
)

rows = spark.table(taskconfig_table).where(scope_filter).collect()
row_count = len(rows)
if row_count == 0:
    raise Exception("No Bronze finance TaskConfig rows found for ConfigId=46.")

print(f"Updating {row_count} Bronze TaskConfig row(s) (ConfigId=46, 14 Finance methods).")

updates = []
for row in rows:
    method = row["Method"]
    if method not in METHOD_MERGE_KEYS:
        raise Exception(f"Unexpected Method in scope: {method} (TaskConfigId={row['TaskConfigId']})")

    new_keys = METHOD_MERGE_KEYS[method]
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

print(f"--- PLANNED UPDATES ({len(updates)} rows) ---")
display(
    update_df.join(
        spark.table(taskconfig_table).select("TaskConfigId", "Method", "SiteCode"),
        "TaskConfigId",
    )
    .where(F.col("SiteCode").isNull())
    .select("TaskConfigId", "Method", "WatermarkColumn", "RequestBody")
    .orderBy("TaskConfigId")
)

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

print("--- AFTER: Bronze template rows ---")
after_df = (
    spark.table(taskconfig_table)
    .where(scope_filter & F.col("SiteCode").isNull())
    .select(
        "TaskConfigId",
        "Method",
        "WatermarkColumn",
        F.get_json_object("RequestBody", "$.merge_keys").alias("merge_keys"),
        F.get_json_object("RequestBody", "$.dq_keys").alias("dq_keys"),
        "ModifiedBy",
        "ModifiedAt",
    )
    .orderBy("TaskConfigId")
)
display(after_df)

for row in after_df.collect():
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
            f"TaskConfigId={row['TaskConfigId']} merge_keys/dq_keys mismatch for {method}"
        )

print("--- Silver spot check (should still be PascalCase BillID) ---")
display(
    spark.sql(f"""
SELECT TaskConfigId, ConfigId, Method, WatermarkColumn,
       get_json_object(RequestBody, '$.merge_keys') AS merge_keys
FROM {taskconfig_table}
WHERE ConfigId = 47 AND Method = 'SaveBills'
LIMIT 1
""")
)

print(f"PascalCase merge_keys update complete for {len(updates)} Bronze TaskConfig rows.")
