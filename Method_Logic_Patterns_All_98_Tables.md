# Method-Level Logic Patterns — All 98 Save* Methods

**Purpose:** Captures every method's internal logic pattern — load type, pre-pass, RowChkSum, RowState, soft-delete, date window, key lookup, and known anomalies — across all BHG-DR-LIB Save* files.

**Source:** Derived from all 26 method-level documentation files + code analysis (April 2026).

---

## Pattern Legend (used across all tables)


| Symbol / Term            | Meaning                                                                             |
| ------------------------ | ----------------------------------------------------------------------------------- |
| **EF Core**              | EF Core row-by-row upsert (lookup → update or insert)                               |
| **Bulk**                 | `SqlBulkCopy` → staging table → stored procedure MERGE                              |
| **Mixed**                | Some sites use Bulk, others EF (site-code conditional)                              |
| **Pre-pass**             | Step that runs BEFORE the main upsert loop                                          |
| **RowState**             | `bool` or `int` column on Azure table — `1/true` = active, `0/false` = soft-deleted |
| **RowChkSum guard**      | Method reads `RowChkSum` from SAMMS and skips update if unchanged                   |
| **Stored no guard**      | `RowChkSum` column exists in destination but is never compared — always overwrites  |
| **Full pre-pass**        | All Azure rows for the site reset to `RowState=0/false` before the loop             |
| **Date-window pre-pass** | Only rows within the lookback window are reset to `RowState=0`                      |
| **Two-phase commit**     | Updates collected first, inserts collected second, single `SaveChanges()` at end    |


---

## 1. Patient Demographics


| Method               | Destination            | Load                          | Pre-pass                                                         | RowChkSum                 | RowState         | Date Window    | Key Lookup              | Soft Delete                          | Anomalies / Notes                                   |
| -------------------- | ---------------------- | ----------------------------- | ---------------------------------------------------------------- | ------------------------- | ---------------- | -------------- | ----------------------- | ------------------------------------ | --------------------------------------------------- |
| `SaveClientDemo1var` | `pats.tbl_ClientDemo1` | EF (per-row commit on INSERT) | Date-window reset `RowState=0` for B50x sites when `ActionKey=1` | **Yes — effective guard** | int 0/1          | -15 (metadata) | `SiteCode` + `ClientId` | Not-in-window rows stay `RowState=0` | Phone `Substring(24,0)` bug; RowTrax **active**     |
| `SaveClientDemo1`    | `pats.tbl_ClientDemo1` | EF                            | None                                                             | **Yes**                   | Not set (legacy) | -15            | `SiteCode` + `ClientId` | None                                 | NetAlystic partial skip; not in daily BHGTaskRunner |
| `SaveClientDemo2`    | `pats.tbl_ClientDemo2` | EF                            | Reset `RowState=0` when `ActionKey=1`                            | **Yes — effective**       | int 0/1          | -15            | `SiteCode` + `ClientId` | Not-in-window stays 0                | `InnerException` catch bug; RowTrax active          |
| `SaveClientDemo3`    | `pats.tbl_ClientDemo2` | EF (fingerprint only)         | N/A                                                              | **Commented out**         | —                | -15            | `ClientId`              | —                                    | **Disabled** in BHGTaskRunner — dead path           |


---

## 2. Enrollment


| Method           | Destination           | Load                                           | Pre-pass                                                                                         | RowChkSum                                                | RowState                             | Date Window | Key Lookup | Soft Delete                          | Anomalies                                                                                                            |
| ---------------- | --------------------- | ---------------------------------------------- | ------------------------------------------------------------------------------------------------ | -------------------------------------------------------- | ------------------------------------ | ----------- | ---------- | ------------------------------------ | -------------------------------------------------------------------------------------------------------------------- |
| `SaveEnrollment` | `pats.tbl_Enrollment` | EF + `UpdateRange(erolls)` (entire site slice) | **Yes — Full pre-pass:** if any Azure rows exist → all `RowState=false`; skipped if `AllNewRows` | **Bypassed** — `if (true)` since 6/26/2023; still stored | `RowState=true` set in column switch | Rolling -15 | `Id`       | Not-in-window stays `RowState=false` | Lab site skipped; Modality/TreatmentLevel column probes; `UpdateRange` rewrites whole site slice; RowTrax **active** |


---

## 3. Clinical Visits / Check-In / Treatment Level


| Method                  | Destination                  | Load             | Pre-pass | RowChkSum                                                                                                                       | RowState                                                                      | Date Window                                                           | Key Lookup              | Soft Delete               | Anomalies                                                                                                        |
| ----------------------- | ---------------------------- | ---------------- | -------- | ------------------------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------------------- | --------------------------------------------------------------------- | ----------------------- | ------------------------- | ---------------------------------------------------------------------------------------------------------------- |
| `SaveCheckIn`           | `pats.tbl_CheckIn`           | EF `Add` per row | None     | **Yes — effective** skip when unchanged; **bug:** existing rows never get `RowChkSum` updated → re-triggers update on every run | No                                                                            | Rolling (partial pre-load: `CiDate >= workdate OR CiId >= first-row`) | `CiId`                  | None                      | `AllNewRows` dead; `ciQUEUETIME` column probe; pre-load gap risk; MinutesWaited string truncation; RowTrax empty |
| `SaveTreatmentLevel`    | `pats.tbl_TreatmentLevel`    | EF (two-phase)   | None     | **Stored — no guard** (RowChkSum read in `sitecode` case but not compared)                                                      | **Effectively never set** — update copies default → overwrites existing value | Rolling (`wrkdt` unused)                                              | `SiteCode` + `ID`       | None                      | Per-field try/catch swallows errors silently; `recordon` guard `>5` chars; no RowTrax                            |
| `SaveAppointments`      | `pats.tbl_Appointments`      | EF               | None     | **Stored — no skip**                                                                                                            | No                                                                            | Full site in method                                                   | (composite key per doc) | None                      | RowTrax inactive                                                                                                 |
| `SaveAppointmentAttend` | `pats.tbl_AppointmentAttend` | EF               | None     | **Stored — no skip**                                                                                                            | `aacltid<0` → `RowState=false`                                                | Full site                                                             | (composite key)         | `aacltid<0` sets inactive | RowTrax inactive                                                                                                 |


---

## 4. Financial / Billing


| Method                       | Destination            | Load                                                                                      | Pre-pass                                                                          | RowChkSum                                                                  | RowState                      | Date Window                                          | Key Lookup            | Soft Delete                           | Anomalies                                                                                                |
| ---------------------------- | ---------------------- | ----------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- | -------------------------------------------------------------------------- | ----------------------------- | ---------------------------------------------------- | --------------------- | ------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `SaveBills` (DataTable path) | `pats.tbl_Bills`       | EF                                                                                        | **Partial** — active→inactive for year window; `SaveChanges` **before** main loop | **Yes — guard on match**; only `LastModAt` + `RowState` updated if changed | From `BillCltid` sign         | **Year window** + `BillDaysBack`; Azure matches year | `SiteCode` + `BillId` | In-window not returned stays inactive | `NewBills` dead code; `BillReason` off-by-2 offset; RowTrax empty                                        |
| `SaveBills` (string path)    | `pats.tbl_Bills`       | EF                                                                                        | No RowState reset                                                                 | **Yes**                                                                    | None                          | Caller's `SrcCmd`                                    | `BillId`              | —                                     | Unconditional `Update` after guard; used for backfill                                                    |
| `SaveAuthBills`              | `pats.tbl_vw3pbill`    | EF (**updates only — inserts broken**)                                                    | **Full pre-pass** — all `RowState=false` (no mid-commit)                          | **No**                                                                     | From `DsClt`                  | Standard -15                                         | `DsId`                | Active→false then re-activate         | **New rows never `db.Add*`* — insert silently lost; `billunits` no guard; PHC `SiteId` column order risk |
| `SaveAuthBillsub`            | `pats.tbl_vw3pBillSub` | **EF code bypassed** — production is `BulkDartsSrvLoader` → `stg.tbl_vw3pbillsub` → MERGE | N/A (bulk handles)                                                                | **Stored in source; no EF guard**                                          | bool                          | -15                                                  | 6-part key            | Bulk MERGE handles                    | EF path dead; `WrkDate`/`Reload` unused; replacement EF path broken                                      |
| `SaveFmp`                    | `pats.tbl_Fmp`         | EF                                                                                        | **Full pre-pass:** all `RowState==true` → `false` + `LastModAt=DateTime.Today`    | No                                                                         | Re-activated in column switch | Rolling (`dtWrk` **unused**)                         | `FmpId` (site-slice)  | Not-returned stays inactive           | `RowsUpd` never incremented; `LastModAt` stamped `DateTime.Today` not source date                        |


---

## 5. Insurance / Claims


| Method                      | Destination                      | Load                                                                                          | Pre-pass                                | RowChkSum                                                                 | RowState | Date Window                         | Key Lookup              | Soft Delete                         | Anomalies                                                                               |
| --------------------------- | -------------------------------- | --------------------------------------------------------------------------------------------- | --------------------------------------- | ------------------------------------------------------------------------- | -------- | ----------------------------------- | ----------------------- | ----------------------------------- | --------------------------------------------------------------------------------------- |
| `SaveClaims`                | `pats.tbl_Claims`                | **Mixed:** EF for VBRA/VMIN/VWBY/VBRP; else **BulkCopy** `stg.tbl_claims` → `stg.ClaimsMerge` | EF path: RowChkSum guard                | **Yes on EF path**                                                        | —        | Rolling -15                         | `ClaimId` + `SiteCode`  | `CleanupDeletedData` reconciliation | 4-site EF exception; Bulk is primary for all others                                     |
| `SaveClaimLineItem`         | `pats.tbl_ClaimLineItem`         | **Always Bulk** → `stg.tbl_claimlineitem` → `stg.ClaimLineItemMerge`                          | N/A                                     | —                                                                         | —        | -15                                 | (staging MERGE handles) | MERGE handles                       |                                                                                         |
| `SaveClaimLineItemActivity` | `pats.tbl_ClaimLineItemActivity` | **Always Bulk** → `stg.tbl_claimlineitemactivity` → `stg.ClaimLineItemActivityMerge`          | N/A                                     | —                                                                         | —        | -15                                 | (staging MERGE)         | MERGE handles                       |                                                                                         |
| `SaveClaimStatus`           | `ctrl.tbl_ClaimStatus`           | EF                                                                                            | —                                       | —                                                                         | —        | **-12 months**                      | `CsId`                  | —                                   | Schedule 8 external 3P data                                                             |
| `SavePayerClient`           | `pats.tbl_PayerClient`           | EF + per-row `Update` + `AddRange`                                                            | No                                      | **Disabled** `if (1==1)`                                                  | —        | Reload can skip WHERE; else rolling | `PyId` + `              | PyCltid                             | `                                                                                       |
| `RemovePayerClients`        | `pats.tbl_PayerClient`           | EF updates                                                                                    | No                                      | N/A                                                                       | —        | Inactive view path                  | `PyId`                  | `PyActive=false`                    |                                                                                         |
| `SavePayerCltHistory`       | `pats.tbl_PayerCltHistory`       | EF                                                                                            | No                                      | `**UpdateRange` commented** → updates silently lost                       | —        | Rolling                             | `PchId`                 | —                                   | Silent update loss is known anomaly                                                     |
| `SaveAuths`                 | `pats.tbl_pbi3payauth`           | EF                                                                                            | **Full pre-pass:** all `RowState=false` | **Yes — effective** guard; on match only `RowState`+`LastModAt` refreshed | bool     | -15                                 | `TpaId`                 | Not in 15d stays false              | 5 fields commented out; `TpServ` off-by-one; `tpeffdate` no dash-replace; RowTrax empty |


---

## 6. Medications / Doses


| Method           | Destination            | Load                                                                                          | Pre-pass                                     | RowChkSum    | RowState      | Date Window                                                                | Key Lookup        | Soft Delete                     | Anomalies                                          |
| ---------------- | ---------------------- | --------------------------------------------------------------------------------------------- | -------------------------------------------- | ------------ | ------------- | -------------------------------------------------------------------------- | ----------------- | ------------------------------- | -------------------------------------------------- |
| `SaveDoses`      | `pats.tbl_Dose`        | **Mixed:** EF for V10A/CBCO/V21/V10; **Bulk** `stg.tbl_dose` → `stg.DoseMerge` for all others | EF: RowState soft logic; Bulk: MERGE handles | EF path: Yes | Yes (EF path) | **Dynamic lookback:** -15 normal; -90 month-end Friday; -200 special dates | Site-specific key | EF: optional reload hard-delete | 4 EF-exception sites; Bulk is primary              |
| `SaveDoseExcuse` | `pats.tbl_Dose_Excuse` | **EF only**                                                                                   | —                                            | —            | —             | -15                                                                        | `DeId`            | —                               | Bulk infra exists in staging but EF is active path |


---

## 7. DartsSrv (Counseling Sessions)


| Method                              | Destination                               | Load                                                                      | Pre-pass | RowChkSum | RowState | Date Window                                     | Key Lookup                  | Soft Delete   | Anomalies                                              |
| ----------------------------------- | ----------------------------------------- | ------------------------------------------------------------------------- | -------- | --------- | -------- | ----------------------------------------------- | --------------------------- | ------------- | ------------------------------------------------------ |
| `BulkDartsSrvLoader` (DartsSrv)     | `pats.tbl_DartsSrv_20XX` (10 year tables) | **Bulk** → `stg.tbl_dartssrv` → `stg.DartsSrvMerge`…`stg.DartsSrvMerge28` | N/A      | —         | —        | **Dynamic:** -15 / -90 month-end / -200 special | (staging MERGE handles)     | MERGE handles | ServiceType column guard; year routing in stored procs |
| `SaveDartSrv2014`–`SaveDartSrv2023` | `pats.tbl_DartsSrv_20XX` per year         | **EF** (backfill/historical only)                                         | —        | Yes       | Yes      | Year-based full reload                          | Composite year-specific key | —             | One method per year; not used in daily run             |


---

## 8. Orders


| Method                            | Destination                           | Load | Pre-pass | RowChkSum                | RowState            | Date Window | Key Lookup                     | Soft Delete | Anomalies                                                                   |
| --------------------------------- | ------------------------------------- | ---- | -------- | ------------------------ | ------------------- | ----------- | ------------------------------ | ----------- | --------------------------------------------------------------------------- |
| `SaveOrders2016`–`SaveOrders2028` | `pats.tbl_Orders_20XX` (one per year) | EF   | —        | **Yes + RowState guard** | Yes + `Active` flag | -15 rolling | Year-partitioned composite key | —           | 13 methods — one per year table; `SaveOrders` (no year) not in daily runner |


---

## 9. UA / Lab Results


| Method                | Destination                | Load                                                                   | Pre-pass                                    | RowChkSum           | RowState                | Date Window                              | Key Lookup        | Soft Delete                   | Anomalies                                         |
| --------------------- | -------------------------- | ---------------------------------------------------------------------- | ------------------------------------------- | ------------------- | ----------------------- | ---------------------------------------- | ----------------- | ----------------------------- | ------------------------------------------------- |
| `SaveLABResults`      | `pats.tbl_LABresult`       | EF + `UpdateRange(v)` on full list                                     | `reInit=true` always → **full site reload** | **Yes — effective** | —                       | **Full site every run** (no date filter) | Composite per doc | —                             | `UpdateRange` rewrites all rows; LAB site skipped |
| `SaveUAResults`       | `pats.tbl_UAresults`       | EF                                                                     | Optional `reload` alternative broader WHERE | **Yes**             | —                       | Rolling; broader on reload               | Composite         | —                             |                                                   |
| `SaveUAResultDetail`  | `pats.tbl_UAresultDetail`  | **Always Bulk** → `stg.tbl_uaresultdetail` → `stg.UAResultDetailMerge` | N/A                                         | —                   | —                       | -15                                      | MERGE handles     | MERGE                         | EF completely bypassed                            |
| `SaveUASched`         | `pats.tbl_UASched`         | EF                                                                     | `sys.tables` pre-check; `DISTINCT` filter   | **Stored**          | `UasLngCltId<0` → false | -15; Lab site strips columns             | `UasId`           | `UasLngCltId<0` sets inactive | Table existence check guard                       |
| `SaveLABResultDetail` | `pats.tbl_LabResultDetail` | **Bulk** → `stg.tbl_labresultdetail` → `stg.LABResultDetailMerge`      | N/A                                         | —                   | —                       | -15                                      | MERGE             | MERGE                         |                                                   |


---

## 10. Assessments (Admission + Reassessment)

**Shared pattern for all 17+ assessment methods:**

- **Load:** EF two-phase (collect updates, then inserts, single `SaveChanges`)
- **Pre-pass:** No
- **RowChkSum:** **No guard** — full field overwrite every run (no skip logic)
- **RowState:** Via `Id` case (`RowState=true`) for most; `CltId<0` → `RowState=false` for SubstanceUseHistory variants
- **Date window:** Rolling metadata WHERE
- **Soft delete:** None


| Method                                             | Destination                                                                  |
| -------------------------------------------------- | ---------------------------------------------------------------------------- |
| `SaveAdmissionAssessment`                          | `pats.tbl_AdmissionAssessment`                                               |
| `SaveAdmissionAssessmentSummary`                   | `pats.tbl_AdmissionAssessmentSummary`                                        |
| `SaveAdmissionAssessmentDimensionOneDisorder`      | `pats.tbl_AdmissionAssessmentDimensionOneDisorder`                           |
| `SaveAdmissionAssessmentDimensionTwo`              | `pats.tbl_AdmissionAssessmentDimensionTwo`                                   |
| `SaveAdmissionAssessmentDimensionThree`            | `pats.tbl_AdmissionAssessmentDimensionThree`                                 |
| `SaveAdmissionAssessmentDimensionfour`             | `pats.tbl_AdmissionAssessmentDimensionFour`                                  |
| `SaveAdmissionAssessmentDimensionFiveSubstanceUse` | `pats.tbl_AdmissionAssessmentDimensionFiveSubstanceUse`                      |
| `SaveAdmissionAssessmentDimensionSix`              | `pats.tbl_AdmissionAssessmentDimensionSix`                                   |
| `SaveAdmissionAssessmentSubstanceuseHistory`       | `pats.tbl_AdmissionAssessmentSubstanceuseHistory` — **RowChkSum + RowState** |
| `SaveAssessmentSubstanceuseHistory`                | `pats.tbl_AssessmentSubstanceuseHistory` — **RowChkSum + RowState**          |
| `SaveReAssessment`                                 | `pats.tbl_ReAssessment`                                                      |
| `SaveReAssessmentFamily`                           | `pats.tbl_ReAssessmentFamily`                                                |
| `SaveReAssessmentLegal`                            | `pats.tbl_ReAssessmentLegal`                                                 |
| `SaveReAssessmentMentalHealth`                     | `pats.tbl_ReAssessmentMentalHealth`                                          |
| `SaveReAssessmentOccupational`                     | `pats.tbl_ReAssessmentOccupational`                                          |
| `SaveReAssessmentPhysicalHealth`                   | `pats.tbl_ReAssessmentPhysicalHealth`                                        |
| `SaveReAssessmentSocial`                           | `pats.tbl_ReAssessmentSocial`                                                |
| `SaveReAssessmentSubstanceUse`                     | `pats.tbl_ReAssessmentSubstanceUse`                                          |
| `SaveReAssessmentTreatment`                        | `pats.tbl_ReAssessmentTreatment`                                             |


---

## 11. Pre-Admission / PA Data


| Method                                | Destination                             | Load           | Pre-pass | RowChkSum                        | RowState                                          | Date Window                                         | Key Lookup                    | Soft Delete              | Anomalies                                                                                                                       |
| ------------------------------------- | --------------------------------------- | -------------- | -------- | -------------------------------- | ------------------------------------------------- | --------------------------------------------------- | ----------------------------- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------------- |
| `SavePreAdmissionV6`                  | `ayx.tbl_preadmission_v6`               | EF (two-phase) | No       | **Stored — guard commented out** | From `sitecode` + `isdeleted` (column order risk) | ~-15                                                | `PreAdmissionid` + `Clientid` | `isdeleted` → `RowState` | 3-layer pre-check (SF table + V5 skip + ClientAddress column); `AreYouCurrentlyPregnant` reversed CASE; hardcoded custom SELECT |
| `SavePreAdminReferrals`               | `pats.tbl_PreAdmissionReferralSource`   | EF (two-phase) | No       | **None**                         | Via `isdeleted` int mapping                       | **~-515 days** (`DaysBack-500`); `wrkdt` **unused** | `Id`                          | Via `isdeleted`          | Table existence pre-check; dual-type bool fields                                                                                |
| `SavePA`                              | `pats.tbl_PA`                           | EF             | No       | **None**                         | No RowState on entity                             | Rolling                                             | `Id` + `double` composite     | None                     | No RowChkSum, no RowState, no LastModAt                                                                                         |
| `SaveFinancialHardshipApplication`    | `pats.tbl_FinancialHardshipApplication` | EF (two-phase) | No       | **Stored — no guard**            | `RowState=true` + `LastModAt` on key case         | Rolling (`wrkdt` **unused**)                        | `Id`                          | None                     | Patient signature date bug                                                                                                      |
| `SavePACounselorReview`               | `pats.tbl_PACounselorReview`            | EF (two-phase) | No       | **No guard**                     | Yes                                               | Rolling                                             | `Id`                          | None                     | `wrkdt` unused                                                                                                                  |
| `SavePADimension1`–`SavePADimension6` | `pats.tbl_PADimension1`–`6`             | EF (two-phase) | No       | **No guard**                     | Yes (`RowState=true` + `LastModAt`)               | Rolling (`wrkdt` unused)                            | `Id`                          | None                     | All 6 share same pattern                                                                                                        |
| `SavedropDownListItems`               | `ctrl.tbl_DropDownListItems`            | EF             | No       | **Selective field compare**      | —                                                 | Rolling                                             | Key per doc                   | —                        | Task name typo in scheduler (`drodownlistitems`)                                                                                |


---

## 12. Clinical Scores / COWS / BAM


| Method                                    | Destination                              | Load                                | Pre-pass                | RowChkSum                      | RowState                                                                              | Date Window                                   | Key Lookup                 | Soft Delete                    | Anomalies                                                                                                                                                          |
| ----------------------------------------- | ---------------------------------------- | ----------------------------------- | ----------------------- | ------------------------------ | ------------------------------------------------------------------------------------- | --------------------------------------------- | -------------------------- | ------------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| `SaveCows_v6`                             | `pats.tbl_CowsV6`                        | EF (explicit field copy; two-phase) | No                      | **Stored — not used as guard** | `RowState=true` in `cowid` case; `isdeleted=1` → `RowState=false` (column-order risk) | **No WHERE** — full `SF_COWS` table every run | `Cowid` + `Preadmissionid` | `isdeleted` → `RowState=false` | Not metadata-driven; `CompletedBy` unmapped; `IsActive` branch dead; cowxref NREF risk; `RowState` column-order dependency (Anomaly 5); `LastModAt` assigned twice |
| `SaveBamForm`                             | `pats.tbl_BamForm`                       | EF                                  | —                       | Per method section             | Per method section                                                                    | -15 / forms window                            | Composite                  | —                              | Part of Samms-Forms pipeline; `pats.BAMMerge` runs after                                                                                                           |
| `SaveBamScore`                            | `pats.tbl_BamScore`                      | EF                                  | —                       | Per method section             | —                                                                                     | Same                                          | Links to form              | —                              |                                                                                                                                                                    |
| `SaveTblDiags`                            | `pats.tbl_TblDiag10`                     | EF                                  | —                       | —                              | —                                                                                     | Rolling                                       | ICD-10 key                 | —                              |                                                                                                                                                                    |
| `SaveBAM` (SaveGlobal)                    | `pats.tbl_BriefAddictionMonitor`         | EF                                  | —                       | —                              | —                                                                                     | -30 days                                      | Composite                  | —                              | Uses hardcoded PHCSQLVM SAMMSGLOBAL conn for PHC; `pats.BAMMergeGbl` SP runs after                                                                                 |
| `SaveGlobalClinicalOpiateWithdrawalScale` | `pats.tbl_clinicalopiatewithdrawalscale` | EF                                  | Date-window forms logic | **Yes — guard**                | —                                                                                     | Rolling                                       | Composite                  | —                              | PHC: hardcoded PHCSQLVM SAMMSGLOBAL connection                                                                                                                     |


---

## 13. Inventory


| Method                     | Destination                        | Load                    | Pre-pass | RowChkSum                                              | RowState | Date Window               | Key Lookup | Soft Delete | Anomalies                                                   |
| -------------------------- | ---------------------------------- | ----------------------- | -------- | ------------------------------------------------------ | -------- | ------------------------- | ---------- | ----------- | ----------------------------------------------------------- |
| `SaveBottles`              | `pats.tbl_Bottle`                  | EF (batched `AddRange`) | —        | **Yes — guard**                                        | Yes      | -15                       | Composite  | —           | Schedule 8 INV                                              |
| `SaveLiquidlog`            | `pats.tbl_LiquidLog`               | EF (batched)            | —        | **Yes — guard**                                        | Yes      | **Reload path** available | Composite  | —           | Bulk reload infra exists in staging; EF is incremental path |
| `SaveInvTypes`             | `ctrl.tbl_InvType`                 | EF                      | —        | **Not mapped** → always "changed" — rewrites every run | —        | -15                       | `ItId`     | —           | RowChkSum never correctly computed for this table           |
| `SaveOrientationCheckList` | `pats.tbl_OrientationChecklistNew` | EF                      | —        | **No**                                                 | —        | Rolling                   | Composite  | —           |                                                             |


---

## 14. Forms (FormQuestionAnswers, Signatures, E&M, Comprehensive)


| Method                            | Destination                            | Load                                                                                                                            | Pre-pass                                                                                                                                     | RowChkSum    | RowState             | Date Window                               | Key Lookup                                                     | Soft Delete                                      | Anomalies                                                                                                                       |
| --------------------------------- | -------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------- | ------------ | -------------------- | ----------------------------------------- | -------------------------------------------------------------- | ------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------------- |
| `SaveFormQuestionAnswers`         | `pats.tbl_dbo_FormQuestionAnswers`     | **Mixed:** Bulk for 18 high-vol sites (B37/DM/GAL/HGT/LV1/NC/PH/D07/B26/B24/DRD-SF/V12/B35/B25/V9/FW/LO/B42); EF for all others | **Yes — date-window pre-pass per `ctrl.tbl_Forms2Process` rules:** `DateFilterEnabled=true` → reset rows in window; `false` → reset all rows | No RowChkSum | int `RowState`       | **-30 days** (`DaysBack - 15`)            | `SiteCode` + `FormName` + `FormId` + `ClientId` + `QuestionId` | Not-returned rows stay `RowState=0`              | Dynamic UNION SELECT built per site; `ctrl.tbl_Forms2Process` drives extra source UNIONs; `pats.BAMMerge` runs after every site |
| `SaveAnswerSignatures`            | `pats.tbl_dbo_FormAnswerSignatures`    | EF only (always)                                                                                                                | **Yes — same `TblForms2Process` window rules as FormQA**                                                                                     | No           | RowState             | **-30 days**                              | `SiteCode` + `FormId`                                          | Not-returned stays 0                             | 9-column pivot query; same UNION extension from `tbl_Forms2Process`                                                             |
| `SaveEMFormMDM`                   | `pats.tbl_EandMFormMDM`                | EF                                                                                                                              | **None**                                                                                                                                     | **No**       | No (no RowState col) | **No date filter** — full table every run | `Id` (single int key)                                          | `IsDeleted` stored but not used to gate anything | Always-overwrites; `IsDeleted` stored only; never soft-deletes via RowState                                                     |
| `SaveEMFormPregnancy`             | `pats.tbl_EandMFormPregnancy`          | EF                                                                                                                              | **None**                                                                                                                                     | **No**       | —                    | **No date filter** — full table every run | Composite with pregnancy                                       | —                                                | WHERE clause commented out                                                                                                      |
| `SaveComprehensiveAssessmentForm` | `pats.tbl_ComprehensiveAssessmentForm` | EF                                                                                                                              | —                                                                                                                                            | —            | —                    | **-15 days** (standard `DaysBack`)        | Composite                                                      | —                                                | Uses `SelectConstructor` metadata path                                                                                          |


---

## 15. Global / Reference Tables


| Method                        | Destination                 | Load                                                                                                              | Pre-pass                                                          | RowChkSum                | RowState   | Date Window                                   | Key Lookup          | Soft Delete          | Anomalies                                                                                         |
| ----------------------------- | --------------------------- | ----------------------------------------------------------------------------------------------------------------- | ----------------------------------------------------------------- | ------------------------ | ---------- | --------------------------------------------- | ------------------- | -------------------- | ------------------------------------------------------------------------------------------------- |
| `SaveClinic`                  | `ctrl.tbl_Clinic`           | EF                                                                                                                | No                                                                | **No** — full overwrite  | No         | Rolling                                       | `Pkey` (site-slice) | None                 | No RowChkSum/RowState/soft-delete; `AllNewRows` dead; Lab strips `PullPicsFromDB`; RowTrax empty  |
| `SaveCodes` (BHGTaskRunner)   | `pats.tbl_Codes`            | EF                                                                                                                | No                                                                | **Yes — effective**      | No         | -15                                           | `CdeId`             | None                 | **Critical bug:** new rows only `codes.Add` — not `db.Add` → inserts never written; RowTrax empty |
| `SaveFeeSchedules`            | `pats.tbl_FeeSched`         | EF                                                                                                                | **Partial:** `IsActive=false` for all, then re-activated on match | **Yes — guards** updates | `IsActive` | Rolling                                       | Composite fee key   | `IsActive=false`     | `SiteCode="SAMMS"` for shared rows                                                                |
| `SaveGlobalPayer`             | `pats.tbl_GlobalPayor`      | EF                                                                                                                | **Full:** all `RowState=0`                                        | **Yes — guards**         | int        | Rolling                                       | Composite           | Not-returned stays 0 |                                                                                                   |
| `SaveGlobalUser`              | `ctrl.tbl_user`             | EF + `UpdateRange` + `AddRange`                                                                                   | No                                                                | **No**                   | —          | Rolling                                       | UserId              | —                    | `UpdateRange` rewrites all users; `NewRow` bug in doc                                             |
| `SaveGlobalUserSite`          | `ctrl.tbl_usersites`        | EF                                                                                                                | No                                                                | **No**                   | —          | Rolling                                       | `UserId`+`SiteId`   | —                    |                                                                                                   |
| `SaveGlobalConsents`          | `ctrl.tbl_Consents`         | EF                                                                                                                | —                                                                 | —                        | —          | Rolling                                       | Consent key         | —                    |                                                                                                   |
| `SaveGlobalDevices`           | `ctrl.tbl_globaldevices`    | EF                                                                                                                | —                                                                 | —                        | —          | Rolling                                       | `SId`→`SiteCode`    | —                    |                                                                                                   |
| `SaveGlobalFormsSAMMSClients` | `pats.tbl_FormsSAMMSClient` | **EF code commented — Bulk is active:** `BulkDartsSrvLoader` → `stg.tbl_formssammsclient` → `stg.FormsSAMMSMerge` | N/A                                                               | —                        | —          | **Static: > '12/31/2019'** (always from 2020) | MERGE handles       | MERGE                | EF (`sd.`*) calls commented out; PHC uses `stg.FormsSAMMSMergePHC`                                |
| `SaveServices`                | `pats.tbl_Services`         | EF                                                                                                                | —                                                                 | —                        | —          | Rolling                                       | Service key         | —                    |                                                                                                   |
| `SaveFormCounts`              | (aggregated counts)         | Hardcoded SAMMSGLOBAL conn                                                                                        | —                                                                 | —                        | —          | —                                             | —                   | —                    | Not per-clinic — single SAMMSGLOBAL query                                                         |


---

## 16. 3rd Party Eligibility / Notes


| Method            | Destination            | Load      | Pre-pass                                                       | RowChkSum                    | RowState | Date Window | Key Lookup         | Soft Delete              | Anomalies       |
| ----------------- | ---------------------- | --------- | -------------------------------------------------------------- | ---------------------------- | -------- | ----------- | ------------------ | ------------------------ | --------------- |
| `Save3pElig`      | `pats.tbl_3pElig`      | EF        | **Full pre-pass:** all `RowState=false` → re-activate on match | **Yes — column-level guard** | bool     | Rolling     | Composite elig key | Not-returned stays false |                 |
| `Save3pSetup`     | `ctrl.tbl_3pSetup`     | EF switch | No full pre-pass                                               | —                            | —        | Rolling     | Setup key          | —                        | Batched inserts |
| `Save3pClaimNote` | `pats.tbl_3pClaimNote` | EF switch | No                                                             | —                            | —        | Rolling     | Note key           | —                        | Batched inserts |
| `Save3pArnote`    | `pats.tbl_3pARNote`    | EF switch | No                                                             | —                            | —        | Rolling     | AR note key        | —                        | Batched inserts |


---

## 17. State-Specific Comprehensive Assessments (CA)

**Shared pattern for all 8 SaveCA methods:**

- **Load:** EF (two-phase updates then inserts)
- **Pre-pass:** `sys.tables` existence check — skips site entirely if table not present
- **RowChkSum:** No skip guard — full overwrite
- **RowState:** From key column
- **Date window:** Rolling metadata WHERE passed from caller
- **Soft delete:** None
- **Anomalies:** All state-specific — only active at relevant state clinics


| Method                                       | Destination                                       | State            |
| -------------------------------------------- | ------------------------------------------------- | ---------------- |
| `SaveMNCA`                                   | `pats.tbl_MNComprehensiveAssessment`              | Minnesota        |
| `SaveMNCALOC`                                | `pats.tbl_MNComprehensiveAssessmentLevelOfCare`   | Minnesota        |
| `SaveVACA`                                   | `pats.tbl_VAComprehensiveAssessment`              | Virginia         |
| `SaveVACASummary`                            | `pats.tbl_VAComprehensiveAssessmentSummary`       | Virginia         |
| `SaveNewAdmissionAssessment`                 | `pats.tbl_NewAdmissionAssessment`                 | New format (all) |
| `SaveNewAdmissionAssessmentASAMDimension6`   | `pats.tbl_NewAdmissionAssessmentASAMDimension6`   | New format       |
| `SaveNewPeriodicReassessment`                | `pats.tbl_NewPeriodicReassessment`                | New format       |
| `Savenewperiodicreassessmentcounselorreview` | `pats.tbl_NewPeriodicReassessmentCounselorReview` | New format       |


---

## 18. Custom Q&A


| Method                | Destination                | Load | Pre-pass                                                              | RowChkSum                                    | RowState                 | Date Window | Key Lookup                   | Soft Delete              | Anomalies                                                              |
| --------------------- | -------------------------- | ---- | --------------------------------------------------------------------- | -------------------------------------------- | ------------------------ | ----------- | ---------------------------- | ------------------------ | ---------------------------------------------------------------------- |
| `SaveCustomQuestions` | `pats.tbl_CustomQuestions` | EF   | **Partial:** `RowSate==1` → `0` (note: typo `RowSate` not `RowState`) | **Yes — effective** (`RowCheckSum` property) | `RowSate` int (typo)     | -15         | `CId`                        | Re-activate from extract | Catch block `InnerException` bug; `RowsIns/Upd` not set; RowTrax empty |
| `SaveCustomAnswers`   | `pats.tbl_CustomAnswers`   | EF   | **Partial:** `RowSate==1` → `0`                                       | **Yes — effective**                          | Driven by `CaCltid` sign | -15         | `CaId` + `CaQid` + `CaCltid` | Re-activate from extract | Same catch bug                                                         |


---

## SUMMARY — The 7 Core Logic Patterns

These 7 patterns cover **every single method** across all 98 tables. Each method is a combination of 2–3 of them.

### Pattern A — Full Pre-pass + RowState + RowChkSum Guard

**Best practice.** Most data-safe pattern.

- Before loop: all Azure rows for site → `RowState=0`
- In loop: if `RowChkSum` matches → skip (no write); if changed → update; if new → insert
- After loop: only rows returned from SAMMS have `RowState=1`
- Not-returned rows stay `RowState=0` → soft-deleted
- **Used by:** `SaveFeeSchedules`, `SaveGlobalPayer`, `Save3pElig`, `SaveAuths`

### Pattern B — No Pre-pass + RowChkSum Guard

Rows updated only when checksum changed. Old rows never deactivated.

- In loop: lookup by key; if `RowChkSum` same → skip entirely; if different → update; if missing → insert
- **Used by:** `SaveCodes`, `SaveClientDemo1var`, `SaveClientDemo2`, `SaveInventory (Bottles/LiquidLog)`

### Pattern C — Partial Pre-pass + RowChkSum Guard (date-window)

- Only rows within the date window are reset to `RowState=0`
- Rows outside the window untouched
- **Used by:** `SaveBills` (DataTable), `SaveFormQuestionAnswers`, `SaveCustomQuestions`, `SaveCustomAnswers`

### Pattern D — Full Pre-pass + No RowChkSum (full overwrite)

- Before loop: all `RowState=false`
- In loop: always overwrite all fields regardless of change
- **Used by:** `SaveFmp`, `SaveEnrollment`

### Pattern E — EF No Pre-pass, No RowChkSum (always overwrite)

Simplest pattern. Hits Azure on every row every run.

- No pre-pass, no checksum — just lookup and overwrite
- **Used by:** All SaveAssessment* methods, `SaveCA`*, `SavePADimension*`, `SaveTreatmentLevel`, `SaveCows_v6`

### Pattern F — Bulk + Stored Procedure MERGE

No EF involved at all.

- SqlBulkCopy to staging table
- EXEC stored procedure MERGE (handles insert/update/delete atomically)
- **Used by:** `DartsSrv`, `Dose` (most sites), `Claims` (most sites), `ClaimLineItem`, `ClaimLineItemActivity`, `UAResultDetail`, `LabResultDetail`, `FormsSAMMSClients`, `FormQuestionAnswers` (18 sites)

### Pattern G — No RowState, No RowChkSum, No Pre-pass (full load every run)

No incremental logic at all. Entire table reloaded from SAMMS each night.

- **Used by:** `SaveClinic`, `SaveEMFormMDM`, `SaveEMFormPregnancy`, `SaveGlobalUser`, `SaveCows_v6` (no WHERE clause)

---

## Cross-cutting Anomalies Found Across Methods


| Anomaly                                                    | Methods Affected                                                                                                                                                                            |
| ---------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `RowChkSum` stored but never compared as guard             | `SaveTreatmentLevel`, `SaveCows_v6`, `SavePreAdmissionV6`, `SaveAppointments`, `SaveAppointmentAttend`, `SaveEnrollment` (bypassed `if true`)                                               |
| `RowChkSum` guard effective and working correctly          | `SaveCodes`, `SaveClientDemo1var`, `SaveClientDemo2`, `SaveFeeSchedules`, `SaveGlobalPayer`, `SaveAuths`, `Save3pElig`, `SaveBottles`, `SaveLiquidLog`, `SaveOrders20XX`, `SaveDartSrv20XX` |
| `UpdateRange` rewrites entire site slice (no per-row diff) | `SaveEnrollment`, `SaveLABResults`, `SaveGlobalUser`                                                                                                                                        |
| `wrkdt` variable computed but never used in WHERE          | `SaveFmp`, `SavePreAdminReferrals`, `SavePADimension`*, `SaveFinancialHardshipApplication`, `SaveTreatmentLevel`                                                                            |
| No date filter — full table reloaded every run             | `SaveCows_v6` (no WHERE), `SaveEMFormMDM`, `SaveEMFormPregnancy`, `SaveLABResults`                                                                                                          |
| `RowState` column-order dependency risk                    | `SaveCows_v6` (cowid vs isdeleted), `SavePreAdmissionV6` (sitecode vs isdeleted)                                                                                                            |
| Insert path broken (new rows never written)                | `SaveAuthBills` (never `db.Add`), `SaveCodes` (BHGTaskRunner: `codes.Add` not `db.Add`)                                                                                                     |
| `RowsUpd` counter never incremented                        | `SaveFmp`, several others                                                                                                                                                                   |
| Hardcoded SAMMSGLOBAL connection (`PHCSQLVM`)              | `SaveBAM` (PHC path), `SaveGlobalClinicalOpiateWithdrawalScale` (PHC path)                                                                                                                  |
| PHC_Enabled column filter on `dms.tbl_MapSrc2Dsn`          | All tables that run for PHC — column subset via `tdwork.Where(x => x.PHC_Enabled)`                                                                                                          |
| RowTrax audit active                                       | `SaveClientDemo1var`, `SaveEnrollment`, `SaveUAResults` variants                                                                                                                            |
| RowTrax empty / not called                                 | Most EF methods — `SaveClinic`, `SaveCheckIn`, `SaveBills`, `SaveAuths`, many others                                                                                                        |
| EF path dead — Bulk replaced it                            | `SaveAuthBillsub` (EF dead), `SaveGlobalFormsSAMMSClients` (EF commented)                                                                                                                   |
| `sys.tables` pre-check guards entire site                  | `SaveCows_v6`, `SavePreAdmissionV6`, all `SaveCA*` methods, `SaveFormQuestionAnswers`, `SaveAnswerSignatures`                                                                               |


