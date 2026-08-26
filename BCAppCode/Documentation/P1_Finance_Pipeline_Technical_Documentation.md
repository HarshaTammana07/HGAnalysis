# Microsoft Fabric — Pipeline Documentation — SAMMS P1 Finance ETL

| Field | Value |
|-------|-------|
| **Pipeline Name** | SAMMS P1 Finance ETL Pipeline |
| **Pipeline ID** | `pl_execute_finance` (`28b0f488-2025-4596-affc-e3340b75213a`) |
| **Bronze Child Pipeline ID** | `pl_p1_child_finance` (`08fab271-ffaf-4bf1-8222-3d0c4d116f8f`) |
| **Silver Child Pipeline ID** | `pl_p1_finance_child_bronze_to_silver` (`7b30341e-d0c4-4871-b476-df155af72915`) |
| **Version** | v1.0 |
| **Author** | [Name] |
| **Department** | Developer |
| **Created Date** | 17/08/2026 |
| **Last Updated** | 17/08/2026 |
| **Status** | Draft |
| **Environment** | Dev |

---

## 1. Document Control

### Version History

| Version | Date | Author | Change Summary | Approved By |
|---------|------|--------|----------------|-------------|
| v1.0 | 17/08/2026 | [Name] | Initial draft — generated from SAMMS P1 Finance Fabric pipeline design | [Name] |

### Reviewers

| Role | Name | Review Date | Comments |
|------|------|-------------|----------|
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | | | |

---

## 2. Executive Summary

### Business Purpose

The SAMMS P1 Finance ETL pipeline migrates and modernizes the legacy Regional ETL Phase 1 **finance** process (`BHGTaskRunner.exe 2`) within Microsoft Fabric. It extracts fourteen SAMMS finance entity types — billing, authorizations, claims, payer data, eligibility, diagnoses, and client demographics — from 115+ per-clinic SQL Server databases and loads them into Fabric using a metadata-driven Medallion architecture (**Bronze and Silver**). **Silver is the final destination layer** for this module.

The Fabric implementation separates orchestration into a parent pipeline, a Bronze child (fourteen parallel method loops), a Silver child (fourteen parallel merge notebooks sharing common Cell 1 runtime), and a reusable audit framework. This preserves legacy behavior: method-specific merge keys, `RowChkSum`-gated updates where applicable, bulk-claim merge paths, EF-site exceptions for claims, and **per-method partial success** when one finance table fails at one clinic.

The pipeline processes **fourteen finance methods** across all active clinic sites registered in `meta.taskconfig` (ConfigId 46).

### Stakeholders

| Role | Name | Email | Department |
|------|------|-------|------------|
| Business Owner | [Name] | [email@org.com] | [Dept] |
| Technical Owner | [Name] | [email@org.com] | [Dept] |
| Primary Consumer | [Name] | [email@org.com] | [Dept] |

### SLA & Criticality

| Field | Value |
|-------|-------|
| **Business Criticality** | High — feeds billing, claims, payer, and client finance reporting |
| **Data Freshness SLA** | [e.g. Data available by 6:00 AM daily] |
| **Max Acceptable Downtime** | [e.g. 4 hours] |
| **Escalation Contact** | [Name + Phone] |

---

## 3. Pipeline Overview

### Pipeline Metadata

| Field | Value |
|-------|-------|
| **Copy Job Name** | Fourteen Copy activities per method inside Bronze child — `cp_*_to_bronze` |
| **Copy Job Object ID** | Embedded in child pipeline `pl_p1_child_finance` |
| **Job Mode** | Batch (Bronze); mix of full-table, bulk, and 15-day incremental extracts |
| **Write Behavior** | Bronze: Append (tagged by `IngestRunId`); Silver: Delta MERGE per method |
| **Enable Staging** | No (Bronze Copy) |
| **Table Option** | Silver tables pre-created or created on first merge run |
| **Timeout** | `0.12:00:00` (12 hours) per Copy and notebook activity |
| **Retry Count** | 0 (Copy and Lookup default) |

### Data Flow

Source (per-clinic SAMMS SQL Server) → Bronze child (`pl_p1_child_finance`) → Bronze Lakehouse → Silver child (`pl_p1_finance_child_bronze_to_silver`) → **Silver Lakehouse (final)** → Downstream Reporting.

| Layer | Component | Details |
|-------|-----------|---------|
| **Source** | Per-clinic SAMMS SQL Server | Fourteen finance tables/views |
| **Bronze** | `pl_p1_child_finance` — 14 parallel Filter + ForEach + Lookup + Copy | Table-existence gate; `batchCount = 10` |
| **Silver** | `pl_p1_finance_child_bronze_to_silver` — 14 parallel notebooks | Method-specific Delta MERGE; TaskConfig-driven keys |
| **Destination** | Fabric Silver Lakehouse | Schema `pats` — fourteen finance tables; terminal layer |

### Parent Pipeline Activity Sequence

```
nb_get_p1_finance_taskconfig          (GL scope — optional Gold)
  → nb_p1_finance_audit_start
  → Executed_AfterBronz (pl_p1_child_finance)
  → set_bronze_method_results_from_child
  → Executed_AfterSilver (pl_p1_finance_child_bronze_to_silver)
  → set_silver_method_results_from_child
  → if_all_finance_methods_success
      → flt_active_p1_finance_gold → nb_p1_finance_optional_gold_publish (Inactive)
      → nb_p1_finance_audit_finalize_success / _failure
  → nb_p1_finance_notify_failed (Inactive)
```

---

## 4. Source System

### Connection Details

| Field | Value |
|-------|-------|
| **Source Type** | SQL Server (SAMMS — one database per clinic) |
| **Server / Host** | On-premises via Fabric data gateway |
| **Database Name** | Per clinic — from TaskConfig `DataBaseName` |
| **Connection ID (Fabric)** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Authentication** | Fabric linked service / gateway |

### Source Tables

| # | Method | Source Table | Bronze Table | Has PK? |
|---|--------|--------------|--------------|---------|
| 1 | `SaveBills` | `dbo.tblBill` | `br_samms_bills` | [Yes/No] |
| 2 | `SaveAuths` | `dbo.tbl3PAYauth` | `br_samms_pbi3_pay_auth` | [Yes/No] |
| 3 | `SaveAuthBillsub` | `dbo.vw3pBillSub` | `br_samms_vw3p_bill_sub` | [Yes/No] |
| 4 | `SaveFmp` | `dbo.tblFMP` | `br_samms_fmp` | [Yes/No] |
| 5 | `SavePayerCltHistory` | `dbo.tblPayerCltHistory` | `br_samms_payer_clt_history` | [Yes/No] |
| 6 | `SaveFinancialHardshipApplication` | `dbo.FinancialHardshipApplication` | `br_samms_financial_hardship_application` | [Yes/No] |
| 7 | `Save3pElig` | `dbo.Tbl3pElig` | `br_samms_3p_elig` | [Yes/No] |
| 8 | `SaveClaimLineItem` | `dbo.tbl3pClaimLineItem` | `br_samms_claim_line_item` | [Yes/No] |
| 9 | `SaveClaimLineItemActivity` | `dbo.tbl3pClaimLineItemActivity` | `br_samms_claim_line_item_activity` | [Yes/No] |
| 10 | `SaveClaims` | `dbo.tbl3pClaim` | `br_samms_claims` | [Yes/No] |
| 11 | `SavePayerClient` | `dbo.tblPayerClt` | `br_samms_payer_client` | [Yes/No] |
| 12 | `SaveTblDiags` | `dbo.Tbldiag10` | `br_samms_tbldiag10` | [Yes/No] |
| 13 | `SaveClientDemo1var` | `dbo.tblClient` | `br_samms_client_demo1` | [Yes/No] |
| 14 | `SaveClientDemo2` | `dbo.tblClient` | `br_samms_client_demo2` | [Yes/No] |

**Active sites:** ~115 clinics — one TaskConfig row per site per method (ConfigId 46). Typical Bronze task volume: **~1,610** site × method tasks per run (14 × 115).

### Load Strategy

| Method | Load Type | Incremental Logic |
|--------|-----------|-------------------|
| `SaveBills` | Incremental | Year window on `billDate` ≥ year(workDate − lookback); `billDate` ≤ workDate + 12 days |
| `SaveAuths` | Full | `WHERE 1 = 1` |
| `SaveAuthBillsub` | Full | `SELECT DISTINCT`; null substitutions for legacy bulk merge |
| `SaveFmp` | Full | Full extract |
| `SavePayerCltHistory` | Incremental | `pyDtm >= workDate − lookback` |
| `SaveFinancialHardshipApplication` | Full | Full extract |
| `Save3pElig` | Incremental | `Year(edate) >= Year(workDate − lookback)` |
| `SaveClaimLineItem` | Full (bulk) | Full table — legacy bulk merge path |
| `SaveClaimLineItemActivity` | Full (bulk) | Full table — legacy bulk merge path |
| `SaveClaims` | Full (bulk) | Full table most sites; EF sites `VBRA`, `VMIN`, `VWBY`, `VBRP` use year window |
| `SavePayerClient` | Filtered | 360-day payer history OR active payer (`pyACTIVE`) |
| `SaveTblDiags` | Full | Full extract |
| `SaveClientDemo1var` | Full | ClientDemo1 column subset from `tblClient` |
| `SaveClientDemo2` | Full | ClientDemo2 column subset from `tblClient` |

**Source table gate:** Bronze Lookup verifies table/view exists before Copy. Missing objects skip the site for that method.

**RowChkSum:** `CHECKSUM(...)` computed in Bronze Copy SELECT for methods that use checksum-gated Silver merge (Bills, Auths, AuthBillsub, FHA, 3pElig, Claims, CLI, CLIA, PayerClient, ClientDemo1/2).

---

## 5. Destination System (Fabric Lakehouse)

### Lakehouse Details

| Field | Value |
|-------|-------|
| **Workspace ID** | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| **Bronze Lakehouse Artifact ID** | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` (`bhg_bronze`) |
| **Silver Lakehouse Artifact ID** | `dd09d8b6-d862-4954-a0b2-fcf7372c6595` (`bhg_silver`) |
| **Destination Schema** | `P1Finance` (Bronze); `pats` (Silver) |
| **Table Pre-Created** | [Yes / No — Date: DD/MM/YYYY] |
| **Write Mode** | Bronze: Append; Silver: Delta MERGE (method-specific keys) |

### Source-to-Target Mapping

| Method | Bronze Table | Silver Table (Final) |
|--------|--------------|----------------------|
| `SaveBills` | `P1Finance.br_samms_bills` | `bhg_silver.pats.tbl_Bills` |
| `SaveAuths` | `P1Finance.br_samms_pbi3_pay_auth` | `bhg_silver.pats.tbl_pbi3PayAuth` |
| `SaveAuthBillsub` | `P1Finance.br_samms_vw3p_bill_sub` | `bhg_silver.pats.tbl_vw3pBillSub` |
| `SaveFmp` | `P1Finance.br_samms_fmp` | `bhg_silver.pats.tbl_Fmp` |
| `SavePayerCltHistory` | `P1Finance.br_samms_payer_clt_history` | `bhg_silver.pats.tbl_PayerCltHistory` |
| `SaveFinancialHardshipApplication` | `P1Finance.br_samms_financial_hardship_application` | `bhg_silver.pats.tbl_FinancialHardshipApplication` |
| `Save3pElig` | `P1Finance.br_samms_3p_elig` | `bhg_silver.pats.tbl_3pElig` |
| `SaveClaimLineItem` | `P1Finance.br_samms_claim_line_item` | `bhg_silver.pats.tbl_ClaimLineItem` |
| `SaveClaimLineItemActivity` | `P1Finance.br_samms_claim_line_item_activity` | `bhg_silver.pats.tbl_ClaimLineItemActivity` |
| `SaveClaims` | `P1Finance.br_samms_claims` | `bhg_silver.pats.tbl_Claims` |
| `SavePayerClient` | `P1Finance.br_samms_payer_client` | `bhg_silver.pats.tbl_PayerClient` |
| `SaveTblDiags` | `P1Finance.br_samms_tbldiag10` | `bhg_silver.pats.tbl_tbldiag10` |
| `SaveClientDemo1var` | `P1Finance.br_samms_client_demo1` | `bhg_silver.pats.tbl_ClientDemo1` |
| `SaveClientDemo2` | `P1Finance.br_samms_client_demo2` | `bhg_silver.pats.tbl_ClientDemo2` |

**Bronze site success:** Inferred from each method's Bronze table using TaskConfig `RequestBody.full_table`, `ingest_column` (`IngestRunId`), and `site_column` (`SiteCode`).

### Key Column Mappings / Silver Merge Keys

| Method | Merge Key (Silver) | Notable Rule |
|--------|-------------------|--------------|
| `SaveBills` | `SiteCode` + `billID` | `RowChkSum` gate; `RowState` from `billCLTID` |
| `SaveAuths` | `SiteCode` + `tpaID` | `RowChkSum` gate |
| `SaveAuthBillsub` | 8-column key (see finance taskconfig) | B41/B42 use reduced key set |
| `SaveFmp` | `SiteCode` + `fmpID` | No `RowChkSum`; full RowState cycle |
| `SavePayerCltHistory` | `SiteCode` + `pchID` | No `RowChkSum` / `RowState` |
| `SaveFinancialHardshipApplication` | `SiteCode` + `Id` | `RowState` from `IsDeleted` |
| `Save3pElig` | `SiteCode` + `eID` | Year-window Bronze scope |
| `SaveClaimLineItem` | `SiteCode` + `tpcliID` | Bulk merge path |
| `SaveClaimLineItemActivity` | `SiteCode` + `liaID` | Bulk merge path |
| `SaveClaims` | `SiteCode` + `tpcID` | Bulk most sites; EF incremental split |
| `SavePayerClient` | `SiteCode` + `pyID` + `pyCLTID` | `abs(pyCLTID)`; `pyACTIVE` |
| `SaveTblDiags` | `SiteCode` + `dgID` | No `RowChkSum` |
| `SaveClientDemo1var` | `SiteCode` + `ClientID` | Same source table as Demo2 |
| `SaveClientDemo2` | `SiteCode` + `clientID` | Same source table as Demo1 |

| Bronze Metadata | Silver Handling |
|-----------------|-----------------|
| `SiteCode`, `SourceDatabase`, `IngestRunId`, `ExtractedAt`, lookback dates | Used for filtering/dedup; stripped at Silver |
| `RowChkSum`, `LastModAt`, `RowState` | Transformed per method legacy rules |

### Row Size Validation

| Field | Value |
|-------|-------|
| **Calculated Row Size** | [Confirm per table — Claims and Bills may be wide] |
| **SQL Server Limit** | 8,060 bytes |
| **Status** | [PASS / FAIL] |
| **MAX Columns (off-row)** | [List if applicable] |

---

## 6. Control Table & Scheduling

### TaskConfig Entry (representative structure)

Each active clinic site × method is registered in `bhg_bronze.meta.taskconfig` (ConfigId 46). Silver has one TaskConfig row per method (ConfigId 47).

| Field | Bronze (ConfigId 46) | Silver (ConfigId 47) |
|-------|----------------------|----------------------|
| **Method** | One of fourteen `Save*` finance methods | Same method name |
| **SiteCode** | e.g. `AHK` | N/A (method-level task) |
| **DataBaseName** | e.g. `SAMMS-Ahoskie` | N/A |
| **SourceTable** | e.g. `dbo.tblBill` | Bronze full table path |
| **destination_schema** | `P1Finance` | `pats` |
| **destination_table** | `br_samms_*` | `tbl_*` Silver table name |
| **is_active** | `IsActive = 1` | `IsActive = 1` |

**Control model:** For each of the **fourteen finance methods**, TaskConfig has:

1. One **inactive Bronze template row** (`ConfigId = 46`, `SiteCode` null, `IsActive = 0`) — shape reference only  
2. **~115 active Bronze site rows** (`ConfigId = 46`, `SiteCode` + `DataBaseName` populated, `IsActive = 1`)  
3. One **Silver method row** (`ConfigId = 47`, no site)  
4. One **Gold method row** (`ConfigId = 48`, optional — out of active scope)

| Layer | ConfigId | TaskConfigId range (seed) | Row count (approx.) |
|-------|----------|---------------------------|---------------------|
| Bronze | 46 | `8700` – `10351` | 14 templates + 14 × 115 site rows = **1,624** |
| Silver | 47 | One row per method inside same range | **14** |
| Gold | 48 | Optional — out of active consumer scope | **14** |

Full seed script: `BCAppCode/P1-Implmentation/P1-Finance/finance_module_taskconfig_pyspark.py` (sites copied from P1 Forms `ConfigId = 97`).

### TaskConfig Insert and Update Operations (PySpark)

Run these cells in a **Fabric PySpark notebook attached to `bhg_bronze`**. They use Delta merge/update against `bhg_bronze.meta.taskconfig`.

#### What TaskConfig is used for

| Consumer | How TaskConfig is used |
|----------|------------------------|
| `nb_get_p1_finance_child_taskconfig` | Returns slim active Bronze site rows (ConfigId 46) for all 14 methods — avoids Fabric Lookup 4 MB limit |
| `nb_get_p1_finance_taskconfig` (parent) | Reads ConfigId 48 / GL for optional Gold path only |
| Bronze child filters (`flt_child_*_sites`) | One Filter per method on child TaskConfig JSON |
| Silver notebooks | `resolve_finance_silver_metadata()` reads ConfigId 47 row per method for merge keys, Bronze/Silver table paths |
| Audit / data quality | `RequestBody.dq_keys` / `merge_keys` define business keys for duplicate checks |

**Runtime lookback:** Pipeline parameters `p_lookback_days` (default **15**) and `p_work_date` drive incremental Copy SQL. Keep `LookbackDays` on incremental TaskConfig rows aligned for audit parity.

#### RequestBody (JSON string column)

Finance Bronze `RequestBody` includes richer metadata than Forms — checksum, row-state, and legacy-route hints from `finance_module_taskconfig_pyspark.py`.

**Bronze site row** (example — SaveBills):

```json
{
  "full_table": "bhg_bronze.P1Finance.br_samms_bills",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["SiteCode", "billID"],
  "merge_keys": ["SiteCode", "billID"],
  "checksum_column": "RowChkSum",
  "row_state_column": "RowState",
  "lastmod_column": "LastModAt"
}
```

**Silver method row** (example — SaveAuthBillsub):

```json
{
  "full_table": "bhg_silver.pats.tbl_vw3pBillSub",
  "dq_keys": ["SiteCode", "dsID", "payDEFAULTSUBMIT", "pyPAYERID", "pySUBSID", "pyGROUP", "CptMod", "charge"],
  "merge_keys": ["SiteCode", "dsID", "payDEFAULTSUBMIT", "pyPAYERID", "pySUBSID", "pyGROUP", "CptMod", "charge"],
  "legacy_ef_sites": ["B41", "B42"],
  "legacy_ef_merge_keys": ["SiteCode", "dsID", "pyPAYERID", "pySUBSID", "pyGROUP", "CptMod", "charge"]
}
```

| Method | Bronze `full_table` | Silver `dq_keys` | `LookbackDays` |
|--------|---------------------|------------------|----------------|
| `SaveBills` | `bhg_bronze.P1Finance.br_samms_bills` | `SiteCode`, `billID` | **15** |
| `SaveAuths` | `bhg_bronze.P1Finance.br_samms_pbi3_pay_auth` | `SiteCode`, `tpaID` | null |
| `SaveAuthBillsub` | `bhg_bronze.P1Finance.br_samms_vw3p_bill_sub` | 8-column key (see seed) | null |
| `SaveFmp` | `bhg_bronze.P1Finance.br_samms_fmp` | `SiteCode`, `fmpID` | null |
| `SavePayerCltHistory` | `bhg_bronze.P1Finance.br_samms_payer_clt_history` | `SiteCode`, `pchID` | **15** |
| `SaveFinancialHardshipApplication` | `bhg_bronze.P1Finance.br_samms_financial_hardship_application` | `SiteCode`, `Id` | null |
| `Save3pElig` | `bhg_bronze.P1Finance.br_samms_3p_elig` | `SiteCode`, `eID` | **15** |
| `SaveClaimLineItem` | `bhg_bronze.P1Finance.br_samms_claim_line_item` | `SiteCode`, `tpcliID` | null |
| `SaveClaimLineItemActivity` | `bhg_bronze.P1Finance.br_samms_claim_line_item_activity` | `SiteCode`, `liaID` | null |
| `SaveClaims` | `bhg_bronze.P1Finance.br_samms_claims` | `SiteCode`, `tpcID` | null (EF sites use 15 at runtime) |
| `SavePayerClient` | `bhg_bronze.P1Finance.br_samms_payer_client` | `SiteCode`, `pyID`, `pyCLTID` | **15** |
| `SaveTblDiags` | `bhg_bronze.P1Finance.br_samms_tbldiag10` | `SiteCode`, `dgID` | null |
| `SaveClientDemo1var` | `bhg_bronze.P1Finance.br_samms_client_demo1` | `SiteCode`, `ClientID` | null |
| `SaveClientDemo2` | `bhg_bronze.P1Finance.br_samms_client_demo2` | `SiteCode`, `clientID` | null |

#### 1. Insert a single new site (all fourteen Bronze methods)

Clones each inactive Bronze template (`ConfigId = 46`, `SiteCode IS NULL`, `IsActive = 0`) and creates **fourteen site rows** — one per finance method.

```python
import json
import re
from pyspark.sql import functions as F
from pyspark.sql.window import Window

taskconfig_table = "bhg_bronze.meta.taskconfig"
bronze_config_id = 46
modified_by = "[Name]"

new_site_code = "B99"
new_source_database = "SAMMS-ExampleV5"

site_name = re.sub(r"^SAMMS-", "", new_source_database)
site_name = re.sub(r"(?i)V[0-9]+$", "", site_name).strip()

existing = (
    spark.table(taskconfig_table)
    .where(f"ConfigId = {bronze_config_id} AND SiteCode = '{new_site_code}'")
    .count()
)
if existing > 0:
    raise Exception(f"Site {new_site_code} already exists for ConfigId={bronze_config_id}.")

template_df = (
    spark.table(taskconfig_table)
    .where(f"ConfigId = {bronze_config_id} AND SiteCode IS NULL AND IsActive = 0")
)
template_count = template_df.select("Method").distinct().count()
if template_count != 14:
    raise Exception(f"Expected 14 inactive Bronze templates, found {template_count}.")

max_id = (
    spark.table(taskconfig_table)
    .agg(F.max("TaskConfigId"))
    .collect()[0][0]
)
next_id = int(max_id or 10351) + 1

task_cols = spark.table(taskconfig_table).columns
site_df = spark.createDataFrame(
    [(new_site_code, new_source_database, site_name)],
    ["SiteCode", "DataBaseName", "SiteName"]
)

templates_with_id = (
    template_df
    .withColumn("rn", F.row_number().over(Window.orderBy("ExecutionOrder", "Method")))
    .withColumn("TaskConfigId", F.lit(next_id) + F.col("rn") - 1)
)

new_rows_df = site_df.alias("s").crossJoin(templates_with_id.alias("t")).select([
    F.col("t.TaskConfigId").cast("long").alias("TaskConfigId") if c == "TaskConfigId" else
    F.lit(bronze_config_id).cast("long").alias("ConfigId") if c == "ConfigId" else
    F.concat(F.col("t.TaskName"), F.lit(" - "), F.col("s.SiteCode")).alias("TaskName") if c == "TaskName" else
    F.col("s.SiteCode").alias("SiteCode") if c == "SiteCode" else
    F.col("s.DataBaseName").alias("DataBaseName") if c == "DataBaseName" else
    F.col("s.SiteName").alias("SiteName") if c == "SiteName" else
    F.lit(1).cast("int").alias("IsActive") if c == "IsActive" else
    F.lit(None).cast("long").alias("DependencyTaskConfigId") if c == "DependencyTaskConfigId" else
    F.current_timestamp().alias("CreatedAt") if c == "CreatedAt" else
    F.current_timestamp().alias("ModifiedAt") if c == "ModifiedAt" else
    F.lit(modified_by).alias("CreatedBy") if c == "CreatedBy" else
    F.lit(modified_by).alias("ModifiedBy") if c == "ModifiedBy" else
    F.col(f"t.{c}").alias(c)
    for c in task_cols
])

new_rows_df.write.format("delta").mode("append").saveAsTable(taskconfig_table)
display(spark.sql(f"""
SELECT TaskConfigId, Method, TaskName, SiteCode, DataBaseName, IsActive, LookbackDays, TargetTable
FROM {taskconfig_table}
WHERE ConfigId = {bronze_config_id} AND SiteCode = '{new_site_code}'
ORDER BY ExecutionOrder, Method
"""))
```

#### 2. Activate or deactivate a site

**All fourteen methods for one site:**

```python
from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
bronze_config_id = 46
site_code = "B24"
is_active = 0              # 1 = activate, 0 = deactivate
modified_by = "[Name]"

DeltaTable.forName(spark, taskconfig_table).update(
    condition=f"ConfigId = {bronze_config_id} AND SiteCode = '{site_code}'",
    set={
        "IsActive": F.lit(is_active),
        "ModifiedBy": F.lit(modified_by),
        "ModifiedAt": F.current_timestamp()
    }
)
```

**Single method for one site** (e.g. disable only `SaveBills` at `B24`):

```python
method_name = "SaveBills"

DeltaTable.forName(spark, taskconfig_table).update(
    condition=f"ConfigId = {bronze_config_id} AND SiteCode = '{site_code}' AND Method = '{method_name}'",
    set={
        "IsActive": F.lit(is_active),
        "ModifiedBy": F.lit(modified_by),
        "ModifiedAt": F.current_timestamp()
    }
)
```

#### 3. Change lookback days

**Option A — one pipeline run:** set `p_lookback_days` and `p_work_date` when triggering `pl_execute_finance`.

**Option B — persist on TaskConfig** for incremental methods:

```python
from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
new_lookback_days = 30
modified_by = "[Name]"

incremental_methods = [
    "SaveBills",
    "SavePayerCltHistory",
    "Save3pElig",
    "SavePayerClient",
]
methods_sql = ", ".join([f"'{m}'" for m in incremental_methods])

DeltaTable.forName(spark, taskconfig_table).update(
    condition=f"ConfigId = 46 AND SiteCode IS NOT NULL AND Method IN ({methods_sql})",
    set={
        "LookbackDays": F.lit(new_lookback_days),
        "ModifiedBy": F.lit(modified_by),
        "ModifiedAt": F.current_timestamp()
    }
)

DeltaTable.forName(spark, taskconfig_table).update(
    condition=f"ConfigId = 47 AND Method IN ({methods_sql})",
    set={
        "LookbackDays": F.lit(new_lookback_days),
        "ModifiedBy": F.lit(modified_by),
        "ModifiedAt": F.current_timestamp()
    }
)
```

#### 4. Update RequestBody (e.g. change DQ keys or Bronze table reference)

```python
import json
from delta.tables import DeltaTable
from pyspark.sql import functions as F

taskconfig_table = "bhg_bronze.meta.taskconfig"
site_code = "B24"
method_name = "SaveAuthBillsub"
modified_by = "[Name]"

updated_request_body = json.dumps({
    "full_table": "bhg_bronze.P1Finance.br_samms_vw3p_bill_sub",
    "ingest_column": "IngestRunId",
    "site_column": "SiteCode",
    "database_column": "SourceDatabase",
    "dq_keys": ["SiteCode", "dsID", "payDEFAULTSUBMIT", "pyPAYERID", "pySUBSID", "pyGROUP", "CptMod", "charge"],
    "merge_keys": ["SiteCode", "dsID", "payDEFAULTSUBMIT", "pyPAYERID", "pySUBSID", "pyGROUP", "CptMod", "charge"],
    "checksum_column": "RowChkSum",
    "row_state_column": "RowState",
    "lastmod_column": "LastModAt",
    "legacy_ef_sites": ["B41", "B42"],
    "legacy_ef_merge_keys": ["SiteCode", "dsID", "pyPAYERID", "pySUBSID", "pyGROUP", "CptMod", "charge"]
})

DeltaTable.forName(spark, taskconfig_table).update(
    condition=f"ConfigId = 46 AND SiteCode = '{site_code}' AND Method = '{method_name}'",
    set={
        "RequestBody": F.lit(updated_request_body),
        "ModifiedBy": F.lit(modified_by),
        "ModifiedAt": F.current_timestamp()
    }
)
```

#### 5. Verification queries

```python
display(spark.sql("""
SELECT ConfigId, Method, COUNT(*) AS RowCnt,
       SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCnt
FROM bhg_bronze.meta.taskconfig
WHERE ConfigId IN (46, 47)
GROUP BY ConfigId, Method
ORDER BY ConfigId, Method
"""))

display(spark.sql("""
SELECT TaskConfigId, Method, SiteCode, DataBaseName, IsActive, LookbackDays, TargetTable
FROM bhg_bronze.meta.taskconfig
WHERE ConfigId = 46 AND SiteCode = 'B24'
ORDER BY ExecutionOrder, Method
"""))
```

### ETL Config (`meta.etlconfig`)

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 46 | BR | Bronze extraction |
| 47 | SL | Silver merge |
| 48 | GL | Optional Gold publish — **Inactive in active documentation scope** |

Audit prefix: **`SAMMS P1 Finance`**.

**Note:** Optional Gold publish (`nb_p1_finance_optional_gold_publish`, ConfigId 48) is **Inactive** on the success path. **Silver remains the operational terminal layer** for downstream consumers.

### Schedule Configuration

| Field | Value |
|-------|-------|
| **Frequency** | |
| **Trigger Time** | |
| **Timezone** | |
| **Legacy Schedule** | `BHGTaskRunner.exe 2` (Regional ETL P1) |

### Notebook / Pipeline Entry Point

`nb_get_p1_finance_taskconfig` → `nb_p1_finance_audit_start` → `Executed_AfterBronz` → `set_bronze_method_results_from_child` → `Executed_AfterSilver` → `set_silver_method_results_from_child` → `if_all_finance_methods_success` → audit finalize

Bronze child entry: `nb_get_p1_finance_child_taskconfig` → 14 × (Filter → ForEach → Lookup → Copy) → `set_child_bronze_method_results`

---

## 7. Notebook / PySpark Implementation

### Notebook Details

| Notebook | Object ID | Purpose |
|----------|-----------|---------|
| `nb_get_p1_finance_taskconfig` | `58c4435d-9b5c-4345-aba5-f4f52bd18ad0` | Slim TaskConfig JSON (ConfigId 48 / GL) |
| `nb_get_p1_finance_child_taskconfig` | `b8091d01-8235-40fd-aa4e-083a9957435b` | Slim TaskConfig JSON (ConfigId 46 / BR) |
| `nb_p1_finance_audit_start` / `_finalize_success` / `_failure` | `139d42ab-817b-420b-9504-2bb1823e7e6c` | Audit lifecycle with per-method partial finalize |
| `nb_sl_bills` | `4810926a-3573-4b69-a61c-0e9941f8fd5b` | Silver MERGE — `SaveBills` |
| `nb_sl_auths` | `a8014d62-bf11-4f68-a387-d3e7f6e3010c` | Silver MERGE — `SaveAuths` |
| `nb_sl_bill_sub` | `b1221415-8f9d-42b1-bb2e-6d5a58ac998c` | Silver MERGE — `SaveAuthBillsub` |
| `nb_sl_fmp` | `606070b9-c04d-4fef-802c-0a1c143bd5d5` | Silver MERGE — `SaveFmp` |
| `nb_sl_payer_hist` | `5cc0ec14-b857-496e-8740-d4f01e9bf847` | Silver MERGE — `SavePayerCltHistory` |
| `nb_sl_fha` | `9653eddc-c10a-438c-91b4-980cd9628de4` | Silver MERGE — `SaveFinancialHardshipApplication` |
| `nb_sl_elig` | `20f3f0e7-3ba0-4268-8435-acf33151bc31` | Silver MERGE — `Save3pElig` |
| `nb_sl_cli` | `2d915a7b-97c3-4116-ad91-e4721434c77f` | Silver MERGE — `SaveClaimLineItem` |
| `nb_sl_lia` | `a604704c-e851-4cee-bf3d-2586e86afefe` | Silver MERGE — `SaveClaimLineItemActivity` |
| `nb_sl_claims` | `3e6cede9-338e-42f0-9c01-e596e2905844` | Silver MERGE — `SaveClaims` |
| `nb_sl_payer_clt` | `3ce16233-97fb-4aa0-b6e7-2668d62a547c` | Silver MERGE — `SavePayerClient` |
| `nb_sl_diag10` | `93c66b96-2ddf-4ad1-9d30-bd199fe2b85c` | Silver MERGE — `SaveTblDiags` |
| `nb_sl_demo1` | `54c886c8-b8f8-4321-8810-5820d26457f0` | Silver MERGE — `SaveClientDemo1var` |
| `nb_sl_demo2` | `cad7c002-7007-4613-899b-53f7efb6e2de` | Silver MERGE — `SaveClientDemo2` |
| `nb_p1_finance_optional_gold_publish` | `c1e2d16d-7945-4d2f-8cee-dced07b2e1e4` | Optional Gold — **Inactive** |
| `nb_p1_finance_notify_failed` | `77c87686-120d-486b-9146-6a794d794e38` | Failure notification — **Inactive** |

Silver notebook cell reference: `BCAppCode/P1-Implmentation/P1-Finance/SilverNotebooks/P1_Finance_Silver_Notebook_Cells.md` (common Cell 1 + method Cell 2 per notebook).

### Bronze Child Pipeline Activities (not notebooks)

| Pattern | Type | Purpose |
|---------|------|---------|
| `flt_child_*_sites` | Filter | One filter per method (14) |
| `fe_each_samms_site_*` | ForEach | Per-site Copy (`batchCount = 10`) |
| `lkp_check_*` | Lookup | Source table/view existence |
| `if_*_exists` | IfCondition | Gate Copy |
| `cp_*_to_bronze` | Copy | SAMMS → Bronze Append |
| `set_child_bronze_method_results` | SetVariable | Return per-method JSON to parent |

| Field | Value |
|-------|-------|
| **Method / Function Names** | Fourteen `Save*` finance methods (see Section 4) |
| **Language** | PySpark (Silver notebooks); SQL (Bronze Copy) |
| **Error Strategy** | Per-site and per-method isolation; partial success via method result JSON |
| **Retry Attempts** | 0 (Copy/Lookup) |
| **Failure Notification** | `nb_p1_finance_notify_failed` exists but **Inactive** |
| **Audit Log Tables** | `bhg_bronze.meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

### Transformation Logic

- **Metadata tagging at Bronze** — `SiteCode`, `SourceDatabase`, `IngestRunId`, `ExtractedAt`, lookback date columns, `RowChkSum` where applicable
- **Table-existence gate** — skip Copy when source missing at clinic
- **Bronze site success inference** — Silver reads successful sites from Bronze rows via TaskConfig
- **Within-run Bronze filter** — Silver reads rows for current `IngestRunId` only
- **Common Silver runtime** — `resolve_finance_silver_metadata()`, `checksum_changed_condition()`, EF-site split for Claims
- **RowChkSum gate** — update only when checksum changed (methods configured with `checksum_column`)
- **Silver result JSON split** — `set_silver_method_results_part1` / `part2` due to Fabric expression length limits

### Parent Pipeline Parameters and Variables

| Parameter / Variable | Default / Set by | Purpose |
|----------------------|------------------|---------|
| `p_lookback_days` | 15 | Incremental Copy SQL |
| `p_work_date` | `2026-08-06` | Lookback anchor for date filters |
| `p_ingest_run_id` | pipeline RunId | Bronze row tag; Silver filter |
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver child, audit finalize |
| `v_silver_method_results_json` | `set_silver_method_results_from_child` | Audit finalize |

---

## 8. Security & Compliance

### Access & Permissions

| Field | Value |
|-------|-------|
| **Source DB Permission** | Per-clinic SAMMS access via on-premises gateway |
| **Authentication Method** | Fabric connection |
| **Credential Storage** | |
| **Workspace Access** | BHG PLATFORM CORE DEV |

### Data Classification

Finance and client demographic data may contain **PHI/PII**. Confirm classification with the data/compliance owner.

| Field | Value |
|-------|-------|
| **Data Classification** | Billing, claims, payer, and client finance records |
| **Contains PHI / PII** | |
| **HIPAA Applicable** | |
| **Masking in Non-Prod** | |
| **Compliance Standards** | |

---

## 9. Testing & Validation

### Test Results

| Environment | Test Date | Tested By | Row Count Match | Result |
|-------------|-----------|-----------|-----------------|--------|
| Dev | [DD/MM/YYYY] | [Name] | [Yes/No] | [Pass/Fail] |
| UAT | [DD/MM/YYYY] | [Name] | [Yes/No] | [Pass/Fail] |
| Prod (dry run) | [DD/MM/YYYY] | [Name] | [Yes/No] | [Pass/Fail] |

### Validation Checklist

- Row count captured and compared (`RowCount` metric) for ConfigIds 46 and 47
- BHG_DR parity queries for all fourteen Silver tables — see `P1_Finance_Silver_BHG_Validation_Queries.md`
- Duplicate records identified — verify method-specific merge keys (8-key AuthBillsub, 3-key PayerClient)
- Null values quantified on merge key columns
- Overall `ValidationStatus` PASS or FAIL per method
- Audit log populated after each run (~1,610 Bronze + 14 Silver tasks)
- Per-method bronze/silver JSON checked for `FAILED` or `SKIPPED`
- Test sites: `AHK`, `B42D`, `CBCO`, `HS`, `TTCC` (validation guide)
- Claims EF sites (`VBRA`, `VMIN`, `VWBY`, `VBRP`) validated separately from bulk sites

### Performance Metrics

| Metric | Value |
|--------|-------|
| **Source Row Count** | [Per method, per run] |
| **Destination Row Count** | [Bronze append vs Silver merge] |
| **Load Duration** | [e.g. 120–240 mins] |
| **Throughput** | [e.g. rows/min] |

---

## 10. DevOps & Source Control

### Repository Details

| Field | Value |
|-------|-------|
| **Azure DevOps Org** | [Organisation name] |
| **Repository Name** | [e.g. fabric-pipelines] |
| **Feature Branch** | [e.g. feature/samms-p1-finance-etl] |
| **PR Raised By** | [Developer name] |
| **PR Approved By** | [Reviewer name] |
| **Merge Date** | [DD/MM/YYYY] |

### Rollback Plan

| Field | Value |
|-------|-------|
| **Rollback Trigger** | Silver MERGE failure or confirmed bad finance data for one method |
| **Rollback Steps** | [e.g. Delta time travel on affected `pats.tbl_*` table; re-run with corrected `p_lookback_days` / `p_work_date`] |
| **Rollback Owner** | [Person responsible] |
| **Estimated RTO** | [e.g. 2 hours] |

---

## 11. Known Issues & Limitations

| ID | Issue Description | Workaround / Notes | Target Fix Date |
|----|-------------------|-------------------|-----------------|
| 001 | Bronze append-only — rows accumulate by `IngestRunId` | Silver holds merged state; filter Bronze by ingest run for validation | |
| 002 | Per-clinic schema variance — not all finance tables exist | Lookup gate skips missing tables | |
| 003 | AuthBillsub B41/B42 use reduced merge keys | Verify `legacy_ef_merge_keys` in TaskConfig RequestBody | |
| 004 | Claims bulk vs EF-site split | `VBRA`, `VMIN`, `VWBY`, `VBRP` use incremental year window | |
| 005 | PayerClient 360-day filter complexity | Align with legacy `SavePayerClient` scope | |
| 006 | FMP / PayerCltHistory / TblDiags — no RowChkSum gate | Full update path in Silver | |
| 007 | Optional Gold (ConfigId 48) — Inactive | Silver is operational terminal layer | |
| 008 | Notifications inactive | Monitor via Fabric run history and audit tables | |
| 009 | Silver JSON built in two parts | Expression length limit — part1 + part2 concatenated | |
| 010 | ClientDemo1 `LastModAt` is ETL time | Monthly validation buckets reflect merge time, not client add date | |

---

## 12. Sign-Off & Approvals

| Role | Name | Signature | Date |
|------|------|-----------|------|
| Developer | [Name] | | |
| Technical Lead | Satya Narayana. A | | |
| Data Architect | Praveen Vaddi | | |
| QA Engineer | [Name] | | |
| Business Owner | [Name] | | |

**Document Status:** Draft — pending stakeholder review and test result population.

**Related documentation:**

- Workflow: `BCAppCode/Documentation/P1_Finance_Workflow_Documentation.md`
- Silver notebook cells: `BCAppCode/P1-Implmentation/P1-Finance/SilverNotebooks/P1_Finance_Silver_Notebook_Cells.md`
- BHG validation queries: `BCAppCode/P1-Implmentation/P1-Finance/P1_Finance_Silver_BHG_Validation_Queries.md`
- TaskConfig seed: `BCAppCode/P1-Implmentation/P1-Finance/finance_module_taskconfig_pyspark.py`
