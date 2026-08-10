# P1 Finance Bronze-to-Silver Implementation Notes

Purpose: implementation-facing notes for building the P1 Finance Fabric Bronze copy activities and Silver merge notebooks. This complements `Finance_14_Tables_ETL_Transformation_Reference.md`; it does not replace it.

## Sources Reviewed

| Source | Why it matters |
| --- | --- |
| `P1/P2-Analysis/Regional_P1_P2_Source_to_Destination.md` | Finance domain scope and source-to-destination table list. |
| `P1-Implmentation/P1-Finance/Finance_14_Tables_ETL_Transformation_Reference.md` | Existing Finance transformation reference. |
| `P1-Implmentation/P1-Finance/columnsanddatatypesFinance.txt4` | BHG_DR target column and datatype export for all 14 Finance targets. |
| `P1-Implmentation/P1-Finance/Csvs/finance_vw_MapActions.csv` | Per-site source, destination, WHERE, step, enablement metadata for final tables. |
| `P1-Implmentation/P1-Finance/Csvs/finance_vw_MapSrc2Dsn.csv` | Field mapping metadata used by `SelectConstructor.GetSLT()`. |
| `P1-Implmentation/P1-Finance/Csvs/finance_map_coverage_check..csv` | Coverage confirmation for the 14 Finance tables. |
| `P1-Implmentation/P1-Finance/Csvs/finance_clientdemo_activity_copy_path.csv` | Legacy ClientDemo copy path through `stg.ClientDemo`; use only as source-query/SP context. |
| `BHGTaskRunner/updatedProgram.cs` | Runtime routing and source query overrides. |
| `BHG-DR-LIB/SelectConstructor.cs` | Dynamic SELECT construction and source-side `RowChkSum`. |
| `BHG-DR-LIB/Save*.cs` | EF save/upsert semantics for non-bulk tables and exception paths. |
| `BHG-DR-LIB/AllSPs/SP_script.ipynb` | SQL MERGE semantics for bulk/staging paths. |
| `BHG-DR-LIB/BulkDartsSvc.cs`, `BHG-DR-LIB_updated/BulkDartsSvc.cs` | Staging dispatch, SP calls, and staging truncate behavior. |

## Scope and Coverage

Finance scope is 14 Regional tables. The uploaded coverage CSV has 14 rows. `finance_vw_MapActions.csv` has 117 site rows per final table. `finance_vw_MapSrc2Dsn.csv` has mappings for all 14 tables.

`pats.tbl_ClientDemo1` and `pats.tbl_ClientDemo2` are intentionally different from the others. In legacy BHG_DR, their final-table MapAction rows are disabled and the enabled runner path loads `dbo.tblclient -> stg.ClientDemo -> ClientDemoMerge1/2`. For Fabric, treat them as two separate Finance methods, `ClientDemo1` and `ClientDemo2`, with separate Bronze copy activities and separate Silver merges. Do not create a shared Fabric `stg.ClientDemo` table as the implementation model.

| # | Finance target | Source object from map | Active legacy route | Map rows | Schema columns |
| --- | --- | --- | --- | ---: | ---: |
| 1 | `pats.tbl_Bills` | `dbo.tblBill` | EF `SaveBills` | 117 | 25 |
| 2 | `pats.tbl_pbi3PayAuth` | `dbo.tbl3PAYauth` | EF `SaveAuths` | 117 | 32 |
| 3 | `pats.tbl_vw3pBillSub` | `dbo.vw3pBillSub` | Bulk SP, except B41/B42 EF | 117 | 44 |
| 4 | `pats.tbl_Fmp` | `dbo.tblFMP` | EF `SaveFmp` | 117 | 17 |
| 5 | `pats.tbl_PayerCltHistory` | `dbo.tblPayerCltHistory` | EF `SavePayerCltHistory` | 117 | 7 |
| 6 | `pats.tbl_FinancialHardshipApplication` | `dbo.FinancialHardshipApplication` | EF `SaveFinancialHardshipApplication` | 117 | 46 |
| 7 | `pats.tbl_3pElig` | `dbo.Tbl3pElig` | EF `Save3pElig` | 117 | 20 |
| 8 | `pats.tbl_ClaimLineItem` | `dbo.tbl3pClaimLineItem` | Bulk `stg.ClaimLineItemMerge` | 117 | 31 |
| 9 | `pats.tbl_ClaimLineItemActivity` | `dbo.tbl3pClaimLineItemActivity` | Bulk `stg.ClaimLineItemActivityMerge` | 117 | 32 |
| 10 | `pats.tbl_Claims` | `dbo.tbl3pClaim` | Bulk `stg.ClaimsMerge`, except 4 EF sites | 117 | 96 |
| 11 | `pats.tbl_PayerClient` | `dbo.tblPayerClt` | EF `SavePayerClient` or `RemovePayerClients` | 117 | 45 |
| 12 | `pats.tbl_tbldiag10` | `dbo.Tbldiag10` | Bulk `stg.sp_tblDiag10Merge` | 117 | 17 |
| 13 | `pats.tbl_ClientDemo1` | `dbo.tblclient` | Fabric method `ClientDemo1`; legacy SP source was `stg.ClientDemo` | 117 disabled final rows; legacy has 117 active staging rows | 40 |
| 14 | `pats.tbl_ClientDemo2` | `dbo.tblclient` | Fabric method `ClientDemo2`; legacy SP source was `stg.ClientDemo` | 117 disabled final rows; legacy has 117 active staging rows | 57 |

## Name Normalization Caveats

SQL Server is mostly case-insensitive here, but Fabric development should pick one canonical lowercase name per table.

Known drift:

| Concept | Seen in metadata/schema/SPs |
| --- | --- |
| PayAuth | `tbl_pbi3PayAuth`, `tbl_pbi3PAYauth` |
| FMP | `tbl_Fmp`, `tbl_FMP` |
| FinancialHardship | `tbl_FinancialHardshipApplication`, `Tbl_FinancialHardshipApplication` |
| Diag10 | `tbl_tbldiag10`, `tbl_TblDiag10`, `tbl_tblDiag10` |

Diag10 is the main risk. `updatedProgram.cs` routes `pats.tbl_tbldiag10` to staging name `stg.tbl_tbldiag10`; the SP script uses `pats.tbl_tblDiag10` and `stg.tbl_tblDiag10`; `BHG-DR-LIB_updated/BulkDartsSvc.cs` has the `stg.tbl_tbldiag10` dispatch while the base `BHG-DR-LIB/BulkDartsSvc.cs` does not. Verify the live BHG_DR table/staging names before implementing Fabric table names.

## Bronze Extraction Rules

### Standard Map-Driven Extraction

Most Finance source SELECTs are built through:

1. `vw_MapAction` gives source object, target object, `WhereCondition`, site, work date, sort order, and enabled flag.
2. `vw_MapSrc2Dsn` gives source fields and destination fields.
3. `SelectConstructor.GetSLT()` builds the SELECT list and appends `CHECKSUM(...) RowChkSum` when `ChkSumEnabled` is true.
4. `updatedProgram.cs` sets `ChkSumEnabled = false` only when `ActionKey == 3`; these Finance final rows are ActionKey 1 or 4, so source-side checksum is normally produced unless a custom query overrides it.

For Fabric Bronze, keep these operational columns on every Bronze table or equivalent metadata envelope:

| Column | Purpose |
| --- | --- |
| `_ingest_run_id` | Current Fabric pipeline run id. |
| `_site_code` or `SiteCode` | Site partition and Silver merge scope. |
| `_source_database` | SAMMS database name from taskconfig/map action. |
| `_work_date` | Work date used for WHERE replacement. |
| `_extracted_at` | Bronze extraction timestamp. |
| `_source_system` | `BHG_DR` / SAMMS source lineage. |

### Map WHERE vs Runner Override

Do not blindly use only the map `WhereCondition`. Several Finance tables have runner overrides.

| Target | Map WHERE | Actual legacy extraction behavior to preserve |
| --- | --- | --- |
| `pats.tbl_Bills` | `billDate >= DateAdd(m, -1, @WorkDate) and billDate <= DateAdd(d, 2, @WorkDate)` | Runner ignores map WHERE. It loads where `year(billDate) >= year(WorkDate + DaysBack)` and `billDate <= WorkDate + 12 days`; reload sets `BillDaysBack = -728250`. |
| `pats.tbl_pbi3PayAuth` | `1 = 1` | Map-driven full source extract, EF save. |
| `pats.tbl_vw3pBillSub` | `1 = 1` | Runner forces `SELECT DISTINCT`, `isnull(CptMod, ':(')`, `isnull(pySUBSID, ':(')`, and `isnull(charge, 0)`. B41/B42 use EF; all other sites bulk through `stg.tbl_vw3pBillSub`. |
| `pats.tbl_Fmp` | `1 = 1` | Map-driven full source extract, EF save. |
| `pats.tbl_PayerCltHistory` | `pyDtm is not null and pyDtm >= @WorkDate` | Map-driven incremental, EF save. |
| `pats.tbl_FinancialHardshipApplication` | `1 = 1` | Map-driven full source extract, EF save. |
| `pats.tbl_3pElig` | `Year(edate) >= Year(@WorkDate)` | Map-driven yearly extract, EF save. |
| `pats.tbl_Claims` | `Year(convert(date, tpcCreatedDate)) >= Year(@WorkDate)` | Sites `VBRA`, `VMIN`, `VWBY`, `VBRP` use the map WHERE and EF. All other sites ignore WHERE and bulk-load the full source table. |
| `pats.tbl_ClaimLineItem` | `convert(date, tpcliDtmAdded) = @WorkDate` | Runner ignores WHERE and bulk-loads the full source table. |
| `pats.tbl_ClaimLineItemActivity` | `CONVERT(date, liaDtm) = @WorkDate` | Runner ignores WHERE and bulk-loads the full source table. |
| `pats.tbl_PayerClient` | 360-day payer history / active / end-date filter | Runner skips WHERE on reload; otherwise applies WHERE. If source object is `vw_PayerClt_INACTIVE`, it calls remove/inactivate logic instead of upsert. |
| `pats.tbl_tbldiag10` | `1 = 1` | Map-driven full source extract, bulk through `stg.tbl_tbldiag10` in updated library. |
| `pats.tbl_ClientDemo1/2` | final-table map rows disabled | Legacy active path is one custom full SELECT from `dbo.tblClient` into `stg.ClientDemo`; for Fabric, split this into two method-level Bronze copies: `ClientDemo1` and `ClientDemo2`. |

## Silver Merge Rules by Table

These are the rules to port into Fabric Silver notebooks. `LastModAt` should follow legacy semantics where the BHG_DR target stores it. If we intentionally improve a bug or a commented checksum branch, document that as a migration decision.

| Silver target | Legacy key | RowChkSum behavior | RowState / active behavior | Special transforms |
| --- | --- | --- | --- | --- |
| `pats.tbl_Bills` | `(SiteCode, BillId)` | Guarded: full update only when `RowChkSum` differs or row is new. Null existing checksum treated as 0. | Pre-resets active rows in the loaded year window to false; set true unless `BillCltid <= 0`. | `BillReason` trimmed, truncated to 2498 chars if length > 2500. |
| `pats.tbl_pbi3PayAuth` | `(SiteCode, TpaId)` | Guarded: full update only when checksum differs or row is new; unchanged rows still get `RowState=true`, `LastModAt=RunDT`. | Site-wide pre-reset to false, then current source rows true. | Date strings replace `-` with `/`; `TpServ` truncated to 299 chars if length > 300. |
| `pats.tbl_vw3pBillSub` | Bulk SP key: `SiteCode`, `dsID`, `payDEFAULTSUBMIT`, `pyPAYERID`, `pySUBSID`, `pyGROUP`, `CptMod`, `charge`. EF B41/B42 key is `SiteCode`, `DsId`, `PyPayerid`, `PySubsid`, `PyGroup`, `CptMod`, `Charge`. | Bulk SP updates matched rows unconditionally; checksum predicate is commented. EF path stores checksum but has no skip guard. | Bulk pre-reset only checks whether any staging row exists for the site, not row-level missing BillSub keys. EF path pre-resets all loaded site rows false, then sets matched/new rows true. | Runner applies `SELECT DISTINCT`; `CptMod` null -> `':('`; `pySUBSID` null -> `':('`; `charge` null -> `0`; EF builds `CptMod = cptcode + ':' + modifier`. |
| `pats.tbl_Fmp` | `(SiteCode, FmpId)` in practice; code lookup is by `FmpId` after loading site rows. | No `RowChkSum` column on target; every matched row is overwritten. | Site rows pre-reset false; loaded rows set true. | `LastModAt` uses `DateTime.Today` / current run date style. |
| `pats.tbl_PayerCltHistory` | `(SiteCode, PchId)` in practice; code lookup is `PchId` after loading site rows. | No RowChkSum. | No RowState column. | Existing EF update path has `UpdateRange(PCHUpd)` commented out. Existing tracked entities may still update if attached, but treat this as a migration decision: preserve observed behavior or intentionally fix. |
| `pats.tbl_FinancialHardshipApplication` | `(SiteCode, Id)` | Source checksum is stored, but no compare guard; all matched fields overwrite. | New rows default `RowState=true`; `IsDeleted=true` sets `RowState=false`. | Many bool/date parsing branches; `IsDeleted` empty defaults false. **Legacy bug parity:** `FHAPatientSignatureDate` stays null; `ExpirationDate` = coalesce(source `ExpirationDate`, source `FHAPatientSignatureDate`) when length > 6 (`SavePAData.cs`). |
| `pats.tbl_3pElig` | `(SiteCode, EId)` | Guarded: update when checksum differs; unchanged rows set `RowState=true`. New rows initialize checksum as 0 to force mapping. | Year-window rows pre-reset false, then current rows true. | Source filter is yearly: `Year(edate) >= Year(@WorkDate)`. |
| `pats.tbl_Claims` | `(SiteCode, TpcId)` | EF exception path is checksum-guarded. Bulk `stg.ClaimsMerge` updates every matched row; no active checksum predicate. | Bulk pre-resets target rows missing from staging by site/key to false. EF pre-reset is yearly. | Bulk inserts only when `s.tpcID > 0`. Four sites use EF: `VBRA`, `VMIN`, `VWBY`, `VBRP`. |
| `pats.tbl_ClaimLineItem` | `(SiteCode, TpcliId)` | EF path is checksum-guarded; active bulk SP updates every matched row. | Bulk pre-resets target rows missing from staging by site/key to false. | Active runner bulk-loads full source table despite daily map WHERE. |
| `pats.tbl_ClaimLineItemActivity` | `(SiteCode, LiaId)` | EF path is checksum-guarded; active bulk SP updates every matched row. | Bulk pre-resets target rows missing from staging by site/key to false. | Active runner bulk-loads full source table despite daily map WHERE. |
| `pats.tbl_PayerClient` | `(SiteCode, PyId, abs(PyCltid))` | Source checksum is read/stored, but guard is disabled via `if (1 == 1)`, so all matched fields overwrite. | No RowState column. `PyActive` is the active/inactive flag. `RemovePayerClients` sets `PyActive=false`. | Reload skips WHERE. Normal load applies 360-day/active/end-date WHERE. |
| `pats.tbl_tbldiag10` | `(SiteCode, dgID)` | No target `RowChkSum` in schema export; SP checksum predicate is commented and not usable as-is. | SP pre-resets target rows missing from staging by site/key to false, then matched/new rows true. | Naming must be verified before implementation. |
| `pats.tbl_ClientDemo1` | `(SiteCode, ClientID)` matched to source `cltID` | Legacy bulk `ClientDemoMerge1` updates matched rows unconditionally; checksum predicate is commented. Legacy EF path is checksum-guarded but inactive for SAMMS map rows. | Legacy bulk pre-resets rows missing from full `stg.ClientDemo` by site/client to `RowState=0`, then current rows `RowState=1`. Fabric should apply the same target semantics inside the `ClientDemo1` Silver merge. | Separate Fabric Bronze method from `dbo.tblClient`; use the Demo1 target column subset and preserve the legacy inline `CHECKSUM(...)` expression unless we intentionally define a target-specific checksum. |
| `pats.tbl_ClientDemo2` | `(SiteCode, ClientID)` matched to source `cltID` | Legacy bulk `ClientDemoMerge2` updates matched rows only when checksum differs. Legacy EF path is checksum-guarded but inactive for SAMMS map rows. | Legacy bulk pre-resets missing rows to `RowState=0`; after merge, syncs RowState/LastModAt from Demo1 where different. In Fabric, avoid depending on Demo1 as a physical staging side effect; make the `ClientDemo2` Silver rule explicit. | Separate Fabric Bronze method from `dbo.tblClient`; use the Demo2 target column subset and preserve the legacy inline `CHECKSUM(...)` expression unless we intentionally define a target-specific checksum. |

## Bulk SP Porting Corrections

The existing Finance reference describes several bulk SPs as RowChkSum-gated. The SP scripts show that this is not true for every table.

| SP | Actual matched-row behavior |
| --- | --- |
| `stg.ClaimsMerge` | `WHEN MATCHED THEN UPDATE`; no checksum predicate. |
| `stg.ClaimLineItemMerge` | `WHEN MATCHED THEN UPDATE`; no checksum predicate. |
| `stg.ClaimLineItemActivityMerge` | `WHEN MATCHED THEN UPDATE`; no checksum predicate. |
| `stg.sp_BillSubMerge` | `WHEN MATCHED --and t.RowChkSum <> s.RowChkSum`; checksum predicate commented. |
| `stg.ClientDemoMerge1` | `WHEN MATCHED --and t.RowChkSum <> s.RowChkSum`; checksum predicate commented. |
| `stg.ClientDemoMerge2` | `WHEN MATCHED and t.RowChkSum <> s.RowChkSum THEN`; checksum predicate active. |
| `stg.sp_tblDiag10Merge` | No target checksum in schema export; commented checksum predicate appears in script but is not active. |

When implementing Silver, choose deliberately:

1. Preserve legacy behavior exactly for reconciliation against BHG_DR.
2. Or standardize on checksum-gated updates where the legacy code clearly intended it but commented it out.

For initial parity, use option 1.

## Recommended Fabric Shape

Bronze:

| Bronze stream | Produces Silver target(s) | Notes |
| --- | --- | --- |
| Bills | `pats.tbl_bills` | Custom date-window extraction. |
| PayAuth | `pats.tbl_pbi3payauth` | Full source by site. |
| BillSub | `pats.tbl_vw3pbillsub` | Distinct/isnull source override; site exceptions B41/B42 can be handled in Silver or documented as legacy-only if parity allows. |
| Fmp | `pats.tbl_fmp` | Full source by site. |
| PayerCltHistory | `pats.tbl_payerclthistory` | Incremental by `pyDtm`. |
| FinancialHardshipApplication | `pats.tbl_financialhardshipapplication` | Full source by site. |
| 3pElig | `pats.tbl_3pelig` | Year-window source. |
| Claims | `pats.tbl_claims` | Full source by site for most sites; four EF exception sites use WHERE in legacy. |
| ClaimLineItem | `pats.tbl_claimlineitem` | Full source by site despite map WHERE. |
| ClaimLineItemActivity | `pats.tbl_claimlineitemactivity` | Full source by site despite map WHERE. |
| PayerClient | `pats.tbl_payerclient` | Active/upsert path plus possible inactive-source path. |
| Diag10 | `pats.tbl_tbldiag10` | Full source by site; verify table/staging names. |
| ClientDemo1 | `pats.tbl_clientdemo1` | Separate Bronze copy and Silver merge from `dbo.tblClient`; no shared Fabric staging table. |
| ClientDemo2 | `pats.tbl_clientdemo2` | Separate Bronze copy and Silver merge from `dbo.tblClient`; no shared Fabric staging table. |

Silver:

1. Read current-run Bronze rows for the table/stream.
2. Process only sites with successful Bronze rows or with explicit success metadata, consistent with Reference/Forms audit design.
3. Deduplicate current-run Bronze by the legacy merge key. If multiple rows exist for the same key in one run, keep the latest `_extracted_at` / source `LastModAt` where available.
4. Pre-reset RowState only where legacy does it, and only within the same scope as legacy: site, full-load source, year-window, or loaded rows.
5. Apply the table-specific merge rule above.
6. Keep row count and duplicate-key validation queries per table for BHG_DR vs Fabric Silver parity.

## Open Decisions Before Build

| Decision | Why it matters |
| --- | --- |
| Preserve bulk SP unconditional updates, or standardize checksum-gated Silver updates? | BHG_DR parity says preserve unconditional updates first. Performance and Delta history may favor checksum gates later. |
| Canonical Diag10 target/staging name | Metadata, schema, and SP script disagree. Must settle before table creation and copy activity naming. |
| PayerCltHistory update behavior | Legacy code has a commented `UpdateRange`; decide whether Fabric fixes this or matches existing behavior. |
| Claims/LineItem/Activity full-load volume | Legacy bulk path extracts full source for most sites. Fabric copy design should confirm gateway/load impact. |
| Zero-row Bronze success signal | Reference notes allow inferring success from Bronze rows, but Finance incremental tables can legitimately return zero rows. Decide whether task/audit needs explicit success marker metadata. |

## Quick Validation Checklist

Before implementing copy activity:

1. Confirm all 14 target schemas from `columnsanddatatypesFinance.txt4`.
2. Confirm all 14 map-action groups from `finance_vw_MapActions.csv`.
3. Confirm ClientDemo1 and ClientDemo2 are modeled as separate Fabric methods, using the legacy `stg.ClientDemo` metadata only to understand the old source query and SP behavior.
4. Confirm Silver keys from this document, not the `PrimaryKey` flag in `finance_vw_MapSrc2Dsn.csv`.
5. Confirm Diag10 live object names in BHG_DR.

## One-time backfill: SaveAuths date columns (and other `parse_legacy_date` tables)

**Symptom:** Bronze has `tpaEffDATE`, `tpaTermDATE`, `tpTermDate`, `tpadt` populated, but Silver shows null while `RowChkSum` still matches BHG_DR.

**Cause:** An earlier `parse_legacy_date()` string round-trip nulled typed bronze timestamps on first insert. Checksum-guarded merge then only refreshed `RowState` / `LastModAt` on later runs.

**Fix deployed:** `parse_legacy_date()` in `nb_p1_finance_sl_common_cell1.py` now coalesces direct `cast("timestamp")` (typed bronze) with the legacy `-` → `/` string path.

**After redeploying the updated common cell to Fabric**, existing Silver rows do **not** self-heal. Run **one** of these once for `pats.tbl_pbi3payauth`:

1. **Preferred (single site test):** Delete Silver rows for the test site, then re-run Silver for that ingest:
   ```sql
   DELETE FROM bhg_silver.pats.tbl_pbi3payauth WHERE SiteCode = 'AHK';
   ```
   Re-run `nb_p1_finance_sl_save_auths` with the same `p_ingest_run_id` that has bronze data.

2. **Full table reload:** Truncate `bhg_silver.pats.tbl_pbi3payauth`, re-run Silver for all sites (bronze must still be available for the ingest run).

3. **Temporary force-update:** Change `update_strategy` to `'always'` in `nb_p1_finance_sl_save_auths.py` for one run only, then revert to `'checksum'`.

**Validate:** Spot-check `SiteCode='AHK', tpaID=1` — all four date columns should match BHG_DR; `RowChkSum` should remain `2035242672`.

**Scope / risk:** This change only affects columns that use `parse_legacy_date()` (SaveAuths, FHA, FMP, PayerClient dates, etc.). Merge keys, checksum logic, and RowState rules are unchanged. Other tables benefit from the same fix if they had the same typed-bronze null pattern.

## SaveFinancialHardshipApplication: ExpirationDate legacy bug parity

**Symptom:** BHG_DR has `ExpirationDate = 2024-01-17` for AHK `Id=1`, but bronze source `ExpirationDate` is null; the date lives in bronze `FHAPatientSignatureDate`.

**Cause:** `SavePAData.cs` assigns `FHAPatientSignatureDate` source values to `xfha.ExpirationDate` (wrong target column); `FHAPatientSignatureDate` on the entity stays null.

**Fix deployed:** `nb_p1_finance_sl_save_financial_hardship_application.py` sets `ExpirationDate` via `coalesce(parse_legacy_date(ExpirationDate), parse_legacy_date(FHAPatientSignatureDate))` and keeps `FHAPatientSignatureDate = null`.

**After redeploying the updated FHA silver notebook**, re-run `nb_p1_finance_sl_save_financial_hardship_application` with the same `p_ingest_run_id`. No Silver delete is required (`update_strategy='always'` overwrites matched rows).

**Validate:** `SiteCode='AHK', Id=1` — `ExpirationDate = 2024-01-17`, `FHAPatientSignatureDate` null, `RowChkSum = -1742202879`.

Before accepting Silver:

1. Compare BHG_DR vs Fabric row counts by table and site.
2. Compare duplicate business keys by table and site.
3. Compare active row counts where `RowState` or `PyActive` exists.
4. Spot-check checksum parity for checksum tables.
5. Spot-check the known special cases: Bills date window, BillSub null substitutions, Claims full-load path, ClientDemo split, PayerClient inactive path, Diag10 naming.
