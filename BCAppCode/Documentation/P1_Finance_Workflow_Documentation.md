# SAMMS P1 Finance ETL — Workflow Document

**Developer Documentation**

| Field | Value |
|-------|-------|
| **Project Name** | BHG Fabric Migration |
| **Pipeline Name** | `pl_execute_finance` |
| **Parent Pipeline Object ID** | `28b0f488-2025-4596-affc-e3340b75213a` |
| **Bronze Child Pipeline** | `pl_p1_child_finance` (`08fab271-ffaf-4bf1-8222-3d0c4d116f8f`) |
| **Silver Child Pipeline** | `pl_p1_finance_child_bronze_to_silver` (`7b30341e-d0c4-4871-b476-df155af72915`) |
| **Developer Name** | [Name] |
| **Environment** | DEV |
| **Version** | 1.0 |
| **Last Updated** | 17/08/2026 |

---

## 1. General Information

**Purpose of the Pipeline:** To automate the extraction, transformation, and loading (ETL) of SAMMS P1 **Finance** records from 115+ clinic SQL Server databases into Microsoft Fabric using the Medallion Architecture (Bronze and Silver). **Silver is the final destination layer** for this module — fourteen finance tables are published to the Silver lakehouse for downstream billing, claims, payer, and client demographic reporting.

**Fourteen finance methods processed:**

| # | Method | Description | Silver Target |
|---|--------|-------------|---------------|
| 1 | `SaveBills` | Patient billing transactions | `bhg_silver.pats.tbl_Bills` |
| 2 | `SaveAuths` | 3rd-party pay authorization | `bhg_silver.pats.tbl_pbi3PayAuth` |
| 3 | `SaveAuthBillsub` | 3rd-party bill submission view | `bhg_silver.pats.tbl_vw3pBillSub` |
| 4 | `SaveFmp` | Financial management plan | `bhg_silver.pats.tbl_Fmp` |
| 5 | `SavePayerCltHistory` | Payer–client history | `bhg_silver.pats.tbl_PayerCltHistory` |
| 6 | `SaveFinancialHardshipApplication` | Financial hardship applications | `bhg_silver.pats.tbl_FinancialHardshipApplication` |
| 7 | `Save3pElig` | 3rd-party eligibility | `bhg_silver.pats.tbl_3pElig` |
| 8 | `SaveClaimLineItem` | Claim line items (bulk path) | `bhg_silver.pats.tbl_ClaimLineItem` |
| 9 | `SaveClaimLineItemActivity` | Claim line item activity (bulk path) | `bhg_silver.pats.tbl_ClaimLineItemActivity` |
| 10 | `SaveClaims` | Claims header (bulk path) | `bhg_silver.pats.tbl_Claims` |
| 11 | `SavePayerClient` | Active payer–client assignments | `bhg_silver.pats.tbl_PayerClient` |
| 12 | `SaveTblDiags` | ICD-10 diagnosis codes | `bhg_silver.pats.tbl_tbldiag10` |
| 13 | `SaveClientDemo1var` | Client demographics set 1 | `bhg_silver.pats.tbl_ClientDemo1` |
| 14 | `SaveClientDemo2` | Client demographics set 2 | `bhg_silver.pats.tbl_ClientDemo2` |

**Legacy context:** Part of Regional ETL P1 (`BHGTaskRunner.exe 2`); replaces legacy C# `SaveBills`, `SavePayorClient`, `Save3pElig`, bulk claim merge stored procedures, and related finance Save* methods.

**Important design notes:**

- Bronze uses **fixed Copy SQL per method** — same pattern as P1 Forms (not dynamic SQL like FormQA).
- Bronze child loads TaskConfig at runtime via **`nb_get_p1_finance_child_taskconfig`** (ConfigId 46 / BR).
- Bronze site success is **inferred from each method's Bronze table** using TaskConfig `RequestBody.full_table`, `ingest_column` (`IngestRunId`), and `site_column` (`SiteCode`) — no separate site-marker table.
- Silver runs as **fourteen parallel notebooks** inside child pipeline `pl_p1_finance_child_bronze_to_silver`, each using **common Cell 1** + **method-specific Cell 2** (see `P1_Finance_Silver_Notebook_Cells.md`).
- Optional Gold publish (`nb_p1_finance_optional_gold_publish`, ConfigId 48) exists on the success path but is **Inactive / out of active workflow scope** — Silver is the terminal layer for consumers.

---

## 2. Solution Overview

### Business Objective

Extract, normalize, and merge fourteen SAMMS finance entity types from per-clinic databases into Fabric Silver while preserving legacy C# and bulk-merge behavior: method-specific merge keys, `RowChkSum`-gated updates where applicable, `RowState` / `IsDeleted` handling, incremental lookback for applicable methods, and **per-method partial success** when one finance table fails at one clinic.

### End-to-End Data Flow

1. **Extract** raw finance data via Copy Data activities (Bronze child — 14 parallel method loops × ~115 sites).
2. **Transform and merge** Bronze into Silver using fourteen parallel PySpark notebooks with method-specific Delta MERGE rules (Silver child).
3. **Audit** — pipeline run, task queue, and data quality written to control tables; optional Gold publish when ConfigId 48 tasks are active.

### Source Systems

- On-premises SAMMS SQL Server databases (one database per clinic, ~115 active sites).
- Connection via Fabric on-premises data gateway (`9743b95a-fd66-4f7c-9767-e6eb0f1ecab7`).

### Destination Systems

- **Bronze:** `bhg_bronze` Lakehouse — schema `P1Finance` (append by `IngestRunId`).
- **Silver (final):** `bhg_silver` Lakehouse — schema `pats` (fourteen finance tables).

### Overall Architecture Diagram

```
pl_execute_finance (PARENT)
|
+- nb_get_p1_finance_taskconfig          (GL config — optional Gold path)
+- nb_p1_finance_audit_start
|
+- Executed_AfterBronz -> pl_p1_child_finance (BRONZE CHILD)
|     +- nb_get_p1_finance_child_taskconfig
|     +- 14 x (Filter -> ForEach sites -> Lookup -> If -> Copy)
|     +- set_child_bronze_method_results
|
+- set_bronze_method_results_from_child
+- Executed_AfterSilver -> pl_p1_finance_child_bronze_to_silver (SILVER CHILD)
|     +- 14 parallel Silver MERGE notebooks (nb_sl_*)
|     +- set_silver_method_results_part1 / part2 / return
|
+- set_silver_method_results_from_child
+- if_all_finance_methods_success
|     +- TRUE  -> flt_active_p1_finance_gold (Inactive)
|     |          -> nb_p1_finance_optional_gold_publish (Inactive)
|     |          -> nb_p1_finance_audit_finalize_success
|     +- FALSE -> nb_p1_finance_audit_finalize_failure
+- nb_p1_finance_notify_failed (Inactive)
```

---

## 3. Pipeline Flow

### Parent Pipeline (`pl_execute_finance`)

#### Activity 1: Load Task Configuration (Gold scope)

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_p1_finance_taskconfig` |
| **Activity Type** | Notebook |
| **Notebook Object ID** | `58c4435d-9b5c-4345-aba5-f4f52bd18ad0` |
| **Purpose** | Reads `meta.taskconfig` for optional Gold layer (ConfigId 48 / GL). Bronze child uses its own child TaskConfig notebook. |
| **Execution Sequence** | 1 |
| **Dependencies** | None |

**Configuration details:**

| Parameter | Value |
|-----------|-------|
| `p_config_name_prefix` | `SAMMS P1 Finance` |
| `p_target_names_json` | `["GL"]` |
| `p_methods_json` | All 14 Save* finance methods |
| `p_only_active` | `true` |

---

#### Activity 2: Audit Start

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_p1_finance_audit_start` |
| **Notebook Object ID** | `139d42ab-817b-420b-9504-2bb1823e7e6c` |
| **Purpose** | Opens pipeline run and layer task queue entries |
| **Parameters** | `p_mode = START_LAYER_RUNS`, `p_config_name_prefix = SAMMS P1 Finance` |

---

#### Activity 3: Bronze Child Pipeline

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterBronz` |
| **Activity Type** | Invoke Pipeline |
| **Child Pipeline** | `pl_p1_child_finance` (`08fab271-ffaf-4bf1-8222-3d0c4d116f8f`) |
| **Purpose** | Runs all 14 Bronze method loops |
| **Parameters passed** | `p_ingest_run_id`, `p_work_date`, `p_lookback_days`, `p_sites_json` |

---

#### Activity 4: Capture Bronze Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_bronze_method_results_from_child` |
| **Purpose** | Stores child return JSON in `v_bronze_method_results_json` |

---

#### Activity 5: Silver Child Pipeline

| Field | Value |
|-------|-------|
| **Activity Name** | `Executed_AfterSilver` |
| **Child Pipeline** | `pl_p1_finance_child_bronze_to_silver` (`7b30341e-d0c4-4871-b476-df155af72915`) |
| **Parameters passed** | `p_ingest_run_id`, `p_bronze_method_results_json`, `p_sites_json` |

---

#### Activity 6: Capture Silver Results

| Field | Value |
|-------|-------|
| **Activity Name** | `set_silver_method_results_from_child` |
| **Purpose** | Stores child return JSON in `v_silver_method_results_json` |

---

#### Activity 7: Success / Failure Branch

| Field | Value |
|-------|-------|
| **Activity Name** | `if_all_finance_methods_success` |
| **Condition** | No `FAILED` in bronze or silver method JSON |
| **TRUE branch** | Optional Gold (Inactive) → `nb_p1_finance_audit_finalize_success` |
| **FALSE branch** | `nb_p1_finance_audit_finalize_failure` |

---

### Bronze Child Pipeline (`pl_p1_child_finance`)

#### Step 1: Child TaskConfig

| Field | Value |
|-------|-------|
| **Activity Name** | `nb_get_p1_finance_child_taskconfig` |
| **Notebook Object ID** | `b8091d01-8235-40fd-aa4e-083a9957435b` |
| **Purpose** | Returns slim active Bronze site rows (ConfigId 46) for all 14 methods |

---

#### Step 2: Per-Method Bronze Pattern (×14, parallel)

Each method follows the same activity chain:

```
flt_child_<method>_sites
  -> fe_each_samms_site_<method>   (batchCount = 10)
       -> lkp_check_<method>        (source table exists?)
       -> if_<method>_exists
            -> cp_<method>_to_bronze
```

| Method | Filter Activity | ForEach Activity | Lookup Gate | Copy Activity |
|--------|-----------------|------------------|-------------|---------------|
| `SaveBills` | `flt_child_bills_sites` | `fe_each_samms_site_bills` | `lkp_check_bills` (`dbo.tblBill`) | `cp_bills_to_bronze` |
| `SaveAuths` | `flt_child_pbi3_pay_auth_sites` | `fe_each_samms_site_pbi3_pay_auth` | `lkp_check_pbi3_pay_auth` | `cp_pbi3_pay_auth_to_bronze` |
| `SaveAuthBillsub` | `flt_child_vw3p_bill_sub_sites` | `fe_each_samms_site_vw3p_bill_sub` | `lkp_check_vw3p_bill_sub` | `cp_vw3p_bill_sub_to_bronze` |
| `SaveFmp` | `flt_child_fmp_sites` | `fe_each_samms_site_fmp` | `lkp_check_fmp` | `cp_fmp_to_bronze` |
| `SavePayerCltHistory` | `flt_child_payer_clt_history_sites` | `fe_each_samms_site_payer_clt_history` | `lkp_check_payer_clt_history` | `cp_payer_clt_history_to_bronze` |
| `SaveFinancialHardshipApplication` | `flt_child_financial_hardship_application_sites` | `fe_each_samms_site_financial_hardship_application` | `lkp_check_financial_hardship_application` | `cp_financial_hardship_application_to_bronze` |
| `Save3pElig` | `flt_child_3p_elig_sites` | `fe_each_samms_site_3p_elig` | `lkp_check_3p_elig` | `cp_3p_elig_to_bronze` |
| `SaveClaimLineItem` | `flt_child_claim_line_item_sites` | `fe_each_samms_site_claim_line_item` | `lkp_check_claim_line_item` | `cp_claim_line_item_to_bronze` |
| `SaveClaimLineItemActivity` | `flt_child_claim_line_item_activity_sites` | `fe_each_samms_site_claim_line_item_activity` | `lkp_check_claim_line_item_activity` | `cp_claim_line_item_activity_to_bronze` |
| `SaveClaims` | `flt_child_claims_sites` | `fe_each_samms_site_claims` | `lkp_check_claims` | `cp_claims_to_bronze` |
| `SavePayerClient` | `flt_child_payer_client_sites` | `fe_each_samms_site_payer_client` | `lkp_check_payer_client` | `cp_payer_client_to_bronze` |
| `SaveTblDiags` | `flt_child_tbldiag10_sites` | `fe_each_samms_site_tbldiag10` | `lkp_check_tbldiag10` | `cp_tbldiag10_to_bronze` |
| `SaveClientDemo1var` | `flt_child_client_demo1_sites` | `fe_each_samms_site_client_demo1` | `lkp_check_client_demo1` | `cp_client_demo1_to_bronze` |
| `SaveClientDemo2` | `flt_child_client_demo2_sites` | `fe_each_samms_site_client_demo2` | `lkp_check_client_demo2` | `cp_client_demo2_to_bronze` |

---

#### Step 3: Return Bronze Method JSON

| Field | Value |
|-------|-------|
| **Activity Name** | `set_child_bronze_method_results` |
| **Purpose** | Builds per-method SUCCESS/FAILED JSON from all 14 ForEach completions; returns to parent via pipeline return value |

---

### Silver Child Pipeline (`pl_p1_finance_child_bronze_to_silver`)

Fourteen notebooks run **in parallel** (no inter-notebook dependency). Each receives `p_method_name`, `p_ingest_run_id`, `p_bronze_method_results_json`, and `p_sites_json`.

Results are concatenated in two parts (`set_silver_method_results_part1` / `part2`) because Fabric expression length limits — then returned as `v_silver_method_results_json`.

---

## 4. Source Details

### Connection

| Field | Value |
|-------|-------|
| **Source Type** | SQL Server (SAMMS — one DB per clinic) |
| **Gateway Connection ID** | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| **Database** | Per clinic — from TaskConfig `DataBaseName` |

### Source Tables and Load Strategy

| Method | Source Table | Load Type | Incremental / Filter Logic |
|--------|--------------|-----------|----------------------------|
| `SaveBills` | `dbo.tblBill` | Incremental | Year window on `billDate` ≥ year(workDate − lookback); `billDate` ≤ workDate + 12 days |
| `SaveAuths` | `dbo.tbl3PAYauth` | Full | `WHERE 1 = 1` |
| `SaveAuthBillsub` | `dbo.vw3pBillSub` | Full | `SELECT DISTINCT`; null substitutions for `CptMod`, `pySUBSID`, `charge` |
| `SaveFmp` | `dbo.tblFMP` | Full | `WHERE 1 = 1` |
| `SavePayerCltHistory` | `dbo.tblPayerCltHistory` | Incremental | `pyDtm IS NOT NULL AND pyDtm >= workDate − lookback` |
| `SaveFinancialHardshipApplication` | `dbo.FinancialHardshipApplication` | Full | `WHERE 1 = 1` |
| `Save3pElig` | `dbo.Tbl3pElig` | Incremental | `Year(edate) >= Year(workDate − lookback)` |
| `SaveClaimLineItem` | `dbo.tbl3pClaimLineItem` | Full (bulk) | Full table extract (legacy bulk merge path) |
| `SaveClaimLineItemActivity` | `dbo.tbl3pClaimLineItemActivity` | Full (bulk) | Full table extract |
| `SaveClaims` | `dbo.tbl3pClaim` | Full (bulk) | Full table most sites; **EF sites** `VBRA`, `VMIN`, `VWBY`, `VBRP` use year window on `tpcCreatedDate` |
| `SavePayerClient` | `dbo.tblPayerClt` (+ inactive view) | Filtered | 360-day payer history OR active payer filter (legacy `SavePayerClient`) |
| `SaveTblDiags` | `dbo.Tbldiag10` | Full | `WHERE 1 = 1` |
| `SaveClientDemo1var` | `dbo.tblClient` | Full | ClientDemo1 column subset |
| `SaveClientDemo2` | `dbo.tblClient` | Full | ClientDemo2 column subset |

**Default lookback:** `p_lookback_days = 15` (overridable per TaskConfig row `LookbackDays`).

**Active sites:** ~115 clinics — one TaskConfig Bronze row per site per method (ConfigId 46). Typical Bronze task volume: **~1,610** site × method tasks per run (14 × 115).

**Source table gate:** Bronze Lookup verifies table/view exists before Copy. Missing objects skip the site for that method.

---

## 5. Destination Details

### Lakehouses

| Layer | Lakehouse | Schema | Write Mode |
|-------|-----------|--------|------------|
| Bronze | `bhg_bronze` | `P1Finance` | Append (tagged by `IngestRunId`) |
| Silver (final) | `bhg_silver` | `pats` | Delta MERGE per method |

### Source-to-Target Mapping

| Method | Bronze Table | Silver Table (Final) | Merge Key |
|--------|--------------|----------------------|-----------|
| `SaveBills` | `P1Finance.br_samms_bills` | `pats.tbl_Bills` | `SiteCode` + `billID` |
| `SaveAuths` | `P1Finance.br_samms_pbi3_pay_auth` | `pats.tbl_pbi3PayAuth` | `SiteCode` + `tpaID` |
| `SaveAuthBillsub` | `P1Finance.br_samms_vw3p_bill_sub` | `pats.tbl_vw3pBillSub` | `SiteCode` + `dsID` + `payDEFAULTSUBMIT` + `pyPAYERID` + `pySUBSID` + `pyGROUP` + `CptMod` + `charge` (B41/B42: reduced key set) |
| `SaveFmp` | `P1Finance.br_samms_fmp` | `pats.tbl_Fmp` | `SiteCode` + `fmpID` |
| `SavePayerCltHistory` | `P1Finance.br_samms_payer_clt_history` | `pats.tbl_PayerCltHistory` | `SiteCode` + `pchID` |
| `SaveFinancialHardshipApplication` | `P1Finance.br_samms_financial_hardship_application` | `pats.tbl_FinancialHardshipApplication` | `SiteCode` + `Id` |
| `Save3pElig` | `P1Finance.br_samms_3p_elig` | `pats.tbl_3pElig` | `SiteCode` + `eID` |
| `SaveClaimLineItem` | `P1Finance.br_samms_claim_line_item` | `pats.tbl_ClaimLineItem` | `SiteCode` + `tpcliID` |
| `SaveClaimLineItemActivity` | `P1Finance.br_samms_claim_line_item_activity` | `pats.tbl_ClaimLineItemActivity` | `SiteCode` + `liaID` |
| `SaveClaims` | `P1Finance.br_samms_claims` | `pats.tbl_Claims` | `SiteCode` + `tpcID` |
| `SavePayerClient` | `P1Finance.br_samms_payer_client` | `pats.tbl_PayerClient` | `SiteCode` + `pyID` + `pyCLTID` (abs on `pyCLTID`) |
| `SaveTblDiags` | `P1Finance.br_samms_tbldiag10` | `pats.tbl_tbldiag10` | `SiteCode` + `dgID` |
| `SaveClientDemo1var` | `P1Finance.br_samms_client_demo1` | `pats.tbl_ClientDemo1` | `SiteCode` + `ClientID` |
| `SaveClientDemo2` | `P1Finance.br_samms_client_demo2` | `pats.tbl_ClientDemo2` | `SiteCode` + `clientID` |

**Bronze site success:** Inferred from Bronze row presence per `IngestRunId` and `SiteCode` — no separate marker table.

### Bronze Metadata Columns (added in every Copy)

| Column | Purpose |
|--------|---------|
| `SiteCode` | Clinic identifier |
| `SourceDatabase` | SAMMS database name |
| `IngestRunId` | Pipeline run filter |
| `ExtractedAt` | Extraction timestamp |
| `SourceQueryStartDate` / `SourceQueryEndDate` | Query window audit |
| `LookbackDate` | Effective lookback date used |
| `RowChkSum` | `CHECKSUM(...)` at source (where applicable) |
| `LastModAt` | Load timestamp |
| `RowState` | Legacy active/inactive flag (method-specific mapping) |

Bronze metadata columns are dropped or transformed at Silver — not carried as-is to final reporting columns.

---

## 6. Notebook Documentation

### `nb_get_p1_finance_taskconfig` (parent)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `58c4435d-9b5c-4345-aba5-f4f52bd18ad0` |
| **Purpose** | Slim TaskConfig read for optional Gold layer |
| **Scope** | ConfigId 48 / GL only on parent |

---

### `nb_get_p1_finance_child_taskconfig` (Bronze child)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `b8091d01-8235-40fd-aa4e-083a9957435b` |
| **Purpose** | Returns slim Bronze site rows for all 14 methods (ConfigId 46) |
| **Output** | JSON array — consumed by 14 Filter activities |

---

### `nb_p1_finance_audit_start` / `_finalize_success` / `_finalize_failure`

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `139d42ab-817b-420b-9504-2bb1823e7e6c` |
| **Purpose** | Audit lifecycle for P1 Finance |
| **Output Tables** | `meta.pipelinerun`, `meta.taskqueue`, `meta.taskaudit`, `meta.dataquality` |

---

### Silver Notebooks (×14)

All Silver notebooks share **Cell 1** (`nb_p1_finance_sl_common_cell1.py` logic) for runtime resolution, site inference, and merge helpers. Each adds a **method-specific Cell 2** documented in `BCAppCode/P1-Implmentation/P1-Finance/SilverNotebooks/P1_Finance_Silver_Notebook_Cells.md`.

| Pipeline Activity | Recommended Notebook Name | Notebook Object ID | Method |
|-------------------|----------------------------|--------------------|--------|
| `nb_sl_bills` | `nb_p1_finance_sl_save_bills` | `4810926a-3573-4b69-a61c-0e9941f8fd5b` | `SaveBills` |
| `nb_sl_auths` | `nb_p1_finance_sl_save_auths` | `a8014d62-bf11-4f68-a387-d3e7f6e3010c` | `SaveAuths` |
| `nb_sl_bill_sub` | `nb_p1_finance_sl_save_auth_billsub` | `b1221415-8f9d-42b1-bb2e-6d5a58ac998c` | `SaveAuthBillsub` |
| `nb_sl_fmp` | `nb_p1_finance_sl_save_fmp` | `606070b9-c04d-4fef-802c-0a1c143bd5d5` | `SaveFmp` |
| `nb_sl_payer_hist` | `nb_p1_finance_sl_save_payer_clt_history` | `5cc0ec14-b857-496e-8740-d4f01e9bf847` | `SavePayerCltHistory` |
| `nb_sl_fha` | `nb_p1_finance_sl_save_financial_hardship_application` | `9653eddc-c10a-438c-91b4-980cd9628de4` | `SaveFinancialHardshipApplication` |
| `nb_sl_elig` | `nb_p1_finance_sl_save_3p_elig` | `20f3f0e7-3ba0-4268-8435-acf33151bc31` | `Save3pElig` |
| `nb_sl_cli` | `nb_p1_finance_sl_save_claim_line_item` | `2d915a7b-97c3-4116-ad91-e4721434c77f` | `SaveClaimLineItem` |
| `nb_sl_lia` | `nb_p1_finance_sl_save_claim_line_item_activity` | `a604704c-e851-4cee-bf3d-2586e86afefe` | `SaveClaimLineItemActivity` |
| `nb_sl_claims` | `nb_p1_finance_sl_save_claims` | `3e6cede9-338e-42f0-9c01-e596e2905844` | `SaveClaims` |
| `nb_sl_payer_clt` | `nb_p1_finance_sl_save_payer_client` | `3ce16233-97fb-4aa0-b6e7-2668d62a547c` | `SavePayerClient` |
| `nb_sl_diag10` | `nb_p1_finance_sl_save_tbldiag10` | `93c66b96-2ddf-4ad1-9d30-bd199fe2b85c` | `SaveTblDiags` |
| `nb_sl_demo1` | `nb_p1_finance_sl_save_client_demo1` | `54c886c8-b8f8-4321-8810-5820d26457f0` | `SaveClientDemo1var` |
| `nb_sl_demo2` | `nb_p1_finance_sl_save_client_demo2` | `cad7c002-7007-4613-899b-53f7efb6e2de` | `SaveClientDemo2` |

| Field | Value |
|-------|-------|
| **Input** | Bronze table for method + `IngestRunId`; TaskConfig via `resolve_finance_silver_metadata()` |
| **Business Logic** | Filter to successful Bronze sites; align schema; deduplicate on match keys; RowChkSum-gated update where configured |
| **Merge/Upsert Logic** | TaskConfig `dq_keys`; null-safe `<=>` match; `whenMatchedUpdate` + `whenNotMatchedInsert` |
| **Error Handling** | Per-method isolation — SKIPPED when Bronze failed; FAILED returns traceback in exit JSON |
| **Exit JSON** | Per-method `status`, `rows_read`, `rows_inserted`, `rows_updated`, `rows_skipped`, `site_results` |

**Method-specific legacy notes (Silver):**

| Method | Notable Silver behavior |
|--------|-------------------------|
| `SaveBills` | `RowChkSum` gate; `RowState` from `billCLTID <= 0` |
| `SaveAuthBillsub` | Full site RowState cycle; B41/B42 use alternate merge keys |
| `SaveFmp` | No `RowChkSum`; full RowState cycle; `LastModAt = today` |
| `SavePayerCltHistory` | No `RowChkSum` / `RowState` |
| `SaveFinancialHardshipApplication` | `RowState` from `IsDeleted` |
| `SaveClaims` | Bulk merge for most sites; EF sites (`VBRA`, `VMIN`, `VWBY`, `VBRP`) use incremental year-window Bronze scope |
| `SavePayerClient` | `abs(pyCLTID)` transform; `pyACTIVE` handling; no `RowState` column |
| `SaveClientDemo1var` / `SaveClientDemo2` | Separate column maps from same `dbo.tblClient` source |

---

### `nb_p1_finance_optional_gold_publish` (optional — Inactive)

| Field | Value |
|-------|-------|
| **Notebook Object ID** | `c1e2d16d-7945-4d2f-8cee-dced07b2e1e4` |
| **State** | **Inactive** on success path |
| **Scope** | Not part of Silver-terminal consumer flow |

---

## 7. Copy Activity Documentation

Each Bronze Copy activity follows the method-specific SQL pattern with shared metadata columns.

| Field | Value |
|-------|-------|
| **Source** | SAMMS SQL Server (clinic database via gateway) |
| **Destination** | `bhg_bronze.P1Finance.br_samms_*` |
| **Mapping** | Explicit column map per method + metadata columns |
| **Incremental Logic** | 15-day lookback (default) for Bills, PayerCltHistory, 3pElig, PayerClient, Claims-EF; full extract for bulk claim tables and others |
| **Retry Configuration** | 0 (pipeline default) |
| **Timeout** | `0.12:00:00` (12 hours) |
| **Write Mode** | Append |
| **ForEach batchCount** | 10 per method loop |

---

## 8. PySpark Transformations

### Data Cleansing (Bronze → Silver)

- Filter Bronze to current `IngestRunId`.
- Determine successful sites from Bronze rows (TaskConfig-driven `site_column` / `ingest_column`).
- Deduplicate on method match keys before MERGE (latest `ExtractedAt` wins within run).
- Align to Silver target schema via method-specific Cell 2 column maps.
- Strip Bronze metadata columns (`METADATA_COLUMNS` set in common runtime).

### Business Rules Implemented (Silver)

| Rule | Description |
|------|-------------|
| Match keys from TaskConfig | `dq_keys` / `WatermarkColumn` must match Silver MERGE keys |
| RowChkSum gate | Update only when checksum changed (methods that use `RowChkSum`) |
| Full update without checksum | FMP, PayerCltHistory — legacy parity |
| Bronze method failure | Silver SKIPPED for that method when Bronze reports FAILED |
| Zero Bronze rows | Silver SUCCESS with zero counts when no data in scope |
| Claims EF split | `CLAIMS_EF_SITES` processed with incremental merge strategy |
| Per-site results | `site_results` in exit JSON for audit and troubleshooting |

### Delta Operations (Silver — Final Layer)

| Operation | When |
|-----------|------|
| **CREATE TABLE** | First run when Silver table does not exist |
| **MERGE — Matched** | Update when RowChkSum changed (or always for non-checksum methods) |
| **MERGE — Not Matched** | INSERT new key combination |

### Performance Optimizations

- Fourteen parallel Bronze ForEach loops with `batchCount = 10` limits gateway concurrency per method.
- Fourteen parallel Silver notebooks — one MERGE per method per run.
- Lookup gate skips Copy for clinics without required tables/views.

### Error Handling

- Per-site isolation in Bronze ForEach — one clinic failure does not stop other sites in the same method.
- Per-method isolation in Silver — one finance table failing does not block the other thirteen.
- Partial success propagated via bronze/silver method JSON to audit finalize.

---

## 9. Parameters and Variables

### Parent Pipeline Parameters (`pl_execute_finance`)

| Parameter | Type | Default | Usage |
|-----------|------|---------|-------|
| `p_ingest_run_id` | string | (empty → RunId) | Tags Bronze rows; filters Silver |
| `p_work_date` | string | `2026-08-06` | Work date for lookback SQL (`billDate`, `pyDtm`, `edate`, etc.) |
| `p_lookback_days` | int | 15 | Incremental window for applicable methods |

### Parent Pipeline Variables

| Variable | Set By | Used By |
|----------|--------|---------|
| `v_bronze_method_results_json` | `set_bronze_method_results_from_child` | Silver child, audit, notify |
| `v_silver_method_results_json` | `set_silver_method_results_from_child` | Audit finalize |

### Bronze Child Parameters (`pl_p1_child_finance`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Bronze metadata |
| `p_work_date` | string | Lookback SQL anchor date |
| `p_lookback_days` | int | Incremental Copy SQL |
| `p_sites` / `p_sites_json` | array / string | Optional site scope override |

### Silver Child Parameters (`pl_p1_finance_child_bronze_to_silver`)

| Parameter | Type | Usage |
|-----------|------|-------|
| `p_ingest_run_id` | string | Bronze row filter |
| `p_bronze_method_results_json` | string | Skip logic when method failed at BR |
| `p_sites_json` | string | Site scope for Silver `site_results` |

### ETL Config

| ConfigId | TargetName | Purpose |
|----------|------------|---------|
| 46 | BR | Bronze extraction |
| 47 | SL | Silver merge |
| 48 | GL | Optional Gold publish — **Inactive in workflow scope** |

Audit prefix: **`SAMMS P1 Finance`**.

TaskConfig seed: `BCAppCode/P1-Implmentation/P1-Finance/finance_module_taskconfig_pyspark.py` (TaskConfigId **8700–10351**; sites copied from P1 Forms ConfigId 97).

---

## 10. Dependencies

### Activity Execution Order (Parent — Silver Terminal)

```
nb_get_p1_finance_taskconfig
  -> nb_p1_finance_audit_start
  -> Executed_AfterBronz (pl_p1_child_finance)
  -> set_bronze_method_results_from_child
  -> Executed_AfterSilver (pl_p1_finance_child_bronze_to_silver)
  -> set_silver_method_results_from_child
  -> if_all_finance_methods_success
      -> TRUE: optional Gold (Inactive) -> nb_p1_finance_audit_finalize_success
      -> FALSE: nb_p1_finance_audit_finalize_failure
```

### External Dependencies

| Dependency | Requirement |
|------------|-------------|
| On-premises gateway | SAMMS SQL Server reachable |
| Fabric lakehouses | `bhg_bronze`, `bhg_silver` online |
| TaskConfig | ConfigId 46 Bronze rows active for target sites (~115 × 14 methods) |
| P1 Forms site universe | Finance TaskConfig seeded from P1 Forms site list (ConfigId 97) |

### Conditional Execution Logic

| Condition | Behavior |
|-----------|----------|
| Source table/view missing | Site skipped — no Copy |
| Bronze method ForEach fails | Method status FAILED in bronze JSON; Silver may SKIPPED |
| Silver notebook fails | Method status FAILED in silver JSON; audit finalize failure path |
| All methods succeed | Audit finalize success (Gold path Inactive) |

### Inactive Activities

| Activity | State | Notes |
|----------|-------|-------|
| `flt_active_p1_finance_gold` | Inactive | Gold filter not executed |
| `nb_p1_finance_optional_gold_publish` | Inactive | Gold publish not active |
| `nb_p1_finance_notify_failed` | Inactive | Notifications not active |

---

## 11. Validation

### Source Validation

- Per-method Lookup gate before extraction (table/view existence in `dbo` schema).

### Row Count Validation

- Audit `DataQuality` records Bronze vs Silver counts after finalize.
- Compare Bronze rows for `IngestRunId` vs Silver merge counts per method.
- BHG_DR parity queries: `BCAppCode/P1-Implmentation/P1-Finance/P1_Finance_Silver_BHG_Validation_Queries.md`.

### Business Validations

| Validation | Detail |
|------------|--------|
| Merge keys | AuthBillsub 8-key vs B41/B42 reduced key; PayerClient 3-key with abs `pyCLTID` |
| RowChkSum | Bills, Auths, AuthBillsub, FHA, 3pElig, Claims, CLI, CLIA, PayerClient, ClientDemo1/2 |
| Claims EF sites | `VBRA`, `VMIN`, `VWBY`, `VBRP` incremental scope vs bulk sites |
| Bills date window | Year lookback + 12-day forward cap on `billDate` |

### Data Quality Checks

- `DuplicateCount`, `NullCount` in `meta.dataquality`
- `ValidationStatus` PASS/FAIL per method
- Silver notebook exit JSON per-method `rows_read` / `rows_inserted` / `rows_updated`

---

## 12. Error Handling

### Failure Scenarios

| Scenario | Impact | Handling |
|----------|--------|----------|
| Gateway / SAMMS failure | Site Copy fails | Other sites continue; method may partial-fail |
| Source table missing | Copy skipped for site | Lookup gate |
| Silver MERGE failure | Method fails at SL | Other 13 methods continue; audit partial finalize |
| TaskConfig empty | No sites to process | Filter returns empty ForEach |
| Audit finalize failure | Pipeline marked Failed | Review method JSON for FAILED entries |

### Retry Logic

- Pipeline activities: retry = 0 (default).

### Recovery Steps

1. Identify failed method/stage from `v_bronze_method_results_json` or `v_silver_method_results_json`.
2. Query `meta.taskaudit` and `meta.dataquality` for the ingest run ID.
3. Fix root cause (gateway, missing table, schema drift, merge key mismatch).
4. Re-run pipeline with new `RunId`.
5. If bad Silver merge confirmed, use Delta time travel on affected table before re-run.

---

## 13. Monitoring

### Pipeline Monitoring

- Fabric run history — parent and both child pipeline activity status.
- `meta.pipelinerun` — BR and SL layer status.

### Log Locations

| Table | Query Filter |
|-------|--------------|
| `meta.taskqueue` | `TaskName LIKE '%P1 Finance%'` AND `PipelineRunId = '<run_id>'` |
| `meta.taskaudit` | `TaskName LIKE '%P1 Finance%'` AND `PipelineRunId = '<run_id>'` |
| `meta.dataquality` | `ConfigId IN (46, 47)` |

### Troubleshooting Approach

| Symptom | Check |
|---------|-------|
| No sites in Bronze | TaskConfig ConfigId 46 active? Child TaskConfig notebook output? |
| One method fails all sites | Bronze ForEach for that method — gateway connectivity |
| Silver SKIPPED | `v_bronze_method_results_json` — method FAILED at BR |
| Row count mismatch | `meta.dataquality` for method and ingest run |
| Claims mismatch on EF sites | Verify `VBRA`/`VMIN`/`VWBY`/`VBRP` incremental Bronze scope |
| AuthBillsub duplicates | Verify 8-key vs B41/B42 reduced merge key |
| PayerClient gaps | 360-day history filter + `pyACTIVE` logic |

---

## 14. Pre-Checks

Before executing the pipeline, verify:

| Check | Detail |
|-------|--------|
| **Source availability** | SAMMS databases accessible via gateway |
| **Environment readiness** | `bhg_bronze` and `bhg_silver` lakehouses online |
| **Parameter validation** | `p_lookback_days` (15), `p_work_date`, `p_ingest_run_id` (if override) |
| **TaskConfig active** | ConfigId 46 rows active for target sites (~115 × 14 methods) |
| **Gateway capacity** | 14 parallel Bronze methods × batchCount 10 |
| **Silver notebooks deployed** | All 14 `nb_sl_*` notebooks published with common Cell 1 + method Cell 2 |

---

## 15. Post-Checks

After execution, validate:

| Check | Detail |
|-------|--------|
| **Pipeline execution status** | Fabric monitor Succeeded (or Failed with expected partial failure) |
| **Bronze completion** | Rows in each `P1Finance.br_samms_*` table for ingest run |
| **Silver merge** | Row counts in all fourteen `pats.tbl_*` / `pats.tbl_*` Silver tables |
| **Audit tables** | TaskQueue, TaskAudit, DataQuality populated |
| **Per-method JSON** | No unexpected FAILED in bronze/silver result variables |
| **BHG parity** | Run validation queries from `P1_Finance_Silver_BHG_Validation_Queries.md` |

### Sample Validation Queries

```sql
-- Task queue for this run
SELECT *
FROM meta.taskqueue
WHERE TaskName LIKE '%P1 Finance%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Task audit detail
SELECT *
FROM meta.taskaudit
WHERE TaskName LIKE '%P1 Finance%'
  AND PipelineRunId = '<pipeline_run_id>';

-- Data quality metrics
SELECT *
FROM meta.dataquality
WHERE ConfigId IN (46, 47)
  AND PipelineRunId = '<pipeline_run_id>';

-- Bronze row count for one method
SELECT SiteCode, COUNT(*) AS row_count
FROM P1Finance.br_samms_bills
WHERE IngestRunId = '<pipeline_run_id>'
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 16. Screenshots

Please upload and insert the required screenshots below (one block per item):

**1. Pipeline overview — `pl_execute_finance` parent canvas**

*[Insert screenshot]*

**2. Bronze child pipeline — 14 parallel ForEach blocks**

*[Insert screenshot]*

**3. Silver child pipeline — 14 parallel notebooks**

*[Insert screenshot]*

**4. Child TaskConfig notebook — `nb_get_p1_finance_child_taskconfig`**

*[Insert screenshot]*

**5. Audit start notebook parameters**

*[Insert screenshot]*

**6. Copy activity — source connection and dynamic SQL (SaveBills example)**

*[Insert screenshot]*

**7. Copy activity — Bronze destination mapping**

*[Insert screenshot]*

**8. Silver notebook — common Cell 1 + SaveClaims Cell 2 (EF site split)**

*[Insert screenshot]*

**9. Silver notebook — Delta MERGE and RowChkSum gate**

*[Insert screenshot]*

**10. IfCondition — audit finalize success / failure branches**

*[Insert screenshot]*

**11. Fabric monitor — partial success with one method FAILED**

*[Insert screenshot]*

**12. TaskConfig sample — ConfigId 46 site row for SaveBills**

*[Insert screenshot]*
