# ETL Transformations — Where to Find Them for All 11 Pipelines
**Date:** 2026-03-25  
**Key insight:** ALL 11 pipelines share the SAME transformation engine.  
The transformation logic does NOT live in separate files per pipeline.  
It is split across shared files in `BHG-DR-LIB` and `BHGTaskRunner`.

---

## How Transformation Works — The Three Layers

Every pipeline goes through exactly these three transformation layers:

```
LAYER 1 — EXTRACT (build + run the SELECT query)
  File: BHGTaskRunner/Program.cs  (lines 1–150)
  File: BHG-DR-LIB/SelectConstructor.cs
  File: BHG-DR-LIB/SQLSvrManager.cs

LAYER 2 — TRANSFORM (column mapping, type casting, checksum, business rules)
  File: BHGTaskRunner/Program.cs  (the ~131 switch cases, lines ~144–3375)
  File: BHG-DR-LIB/Save*.cs files (one or more per table group)

LAYER 3 — LOAD (upsert into Azure BHG_DR)
  File: BHG-DR-LIB/Save*.cs files (the actual INSERT/UPDATE/SKIP logic)
  File: BHG-DR-LIB/BulkDartsSvc.cs  (for bulk/staging loads)
  File: BHG-DR-LIB/Models/*.cs  (EF Core entity classes — the destination table shapes)
```

---

## The Transformation Files — Master Reference

### `BHGTaskRunner/Program.cs` — The Router (3,375 lines)
**Location:** `BCAppCode/BHGTaskRunner/Program.cs`  
**Role:** The central brain of ALL 11 pipelines. Contains:
- Arg-based pipeline selection (`args[0]` = "1" through "11")
- Parent/child task loop
- SELECT query assembly
- The **~131 switch cases** — one per destination table — that route each table to its Save method
- Date window logic, site-specific overrides, schema version gates

This single file is the entry point for transformations of EVERY pipeline.

---

### Shared Utility Files (used by ALL pipelines)

| File | Location | Role |
|---|---|---|
| `SelectConstructor.cs` | `BHG-DR-LIB/` | Builds the SELECT field list dynamically from `dms.vw_MapSrc2Dsn` column mappings. Computes `CHECKSUM(...)` expression. Skips old-schema fields per site. |
| `SQLSvrManager.cs` | `BHG-DR-LIB/` | Executes raw SQL against any connection string. Returns `DataTable`. Used to extract data from SAMMS. |
| `SaveData.cs` | `BHG-DR-LIB/` | The parent partial class that all Save*.cs files extend. |
| `SaveRowTrax.cs` | `BHG-DR-LIB/` (52 lines) | Writes row counts to `tsk.tbl_RowTrax` after each table load (audit trail). |
| `BulkDartsSvc.cs` | `PHC/` and `BHG-DR-LIB/` | SqlBulkCopy + stored procedure merge for high-volume tables (DartsSrv). |

---

## Pipeline-by-Pipeline Transformation File Mapping

---

### Pipeline 1 — SAMMSGlobal (`BHGTaskRunner.exe 1`)

**What it loads:** Global/shared tables that apply to all clinics regardless of timezone.  
**Tables:** `ctrl.tbl_USER`, `ctrl.tbl_USERSITES`, `ctrl.tbl_CONSENTS`, `ctrl.tbl_CLINIC`, `ctrl.tbl_CODES`, `ctrl.tbl_GlobalDevices`, `ctrl.tbl_3PSETUP`, `pats.tbl_XREF`, etc.

| Transformation Step | File | Key Logic |
|---|---|---|
| Route (switch case) | `BHGTaskRunner/Program.cs` | `case "ctrl.tbl_user"`, `case "ctrl.tbl_usersites"`, `case "ctrl.tbl_clinic"`, `case "ctrl.tbl_consents"`, `case "ctrl.tbl_globaldevices"`, `case "ctrl.tbl_3psetup"` |
| Save Users | `BHG-DR-LIB/SaveGlobal.cs` (2,208 lines) | `SaveGlobalUser()` — upserts staff records. Handles `UsrPassword` field masking. |
| Save User Sites | `BHG-DR-LIB/SaveGlobal.cs` | `SaveGlobalUserSite()` — links users to site codes. |
| Save Clinic | `BHG-DR-LIB/SaveClinic.cs` (839 lines) | `SaveClinic()` — upserts the massive tblClinic config row per site. Site-specific column skips (LAB, B28, B42A, etc.). |
| Save Codes | `BHG-DR-LIB/SaveCodes.cs` (390 lines) | `SaveCodes()` — upserts service/billing code definitions. Schema-gated fields (ReqAuth, Obat, IsPrescreening). |
| Save Consents | `BHG-DR-LIB/SaveGlobal.cs` | `SaveGlobalConsents()` — copies consent form definitions. |
| Save Global Devices | `BHG-DR-LIB/SaveGlobal.cs` | `SaveGlobalDevices()` — dispensing pumps and peripherals per clinic. |
| Save 3P Setup | `BHG-DR-LIB/SaveGlobal.cs` | `Save3pSetup()` — third-party billing configuration. |
| Model classes | `BHG-DR-LIB/Models/TblUser.cs` | Destination table shape for `ctrl.tbl_USER` |
| | `BHG-DR-LIB/Models/TblClinic.cs` | Destination table shape for `ctrl.tbl_CLINIC` |
| | `BHG-DR-LIB/Models/TblCodes.cs` | Destination table shape for `ctrl.tbl_CODES` |

---

### Pipelines 2 & 4 — Eastern/Central/Mountain/Pacific ETL P1 and P2 (`BHGTaskRunner.exe 2` and `4`)

**P1 = non-financial clinical data. P2 = financial/billing data.**  
Both use identical transformation files — the split is only in WHICH tables they process.

#### P1 Tables and Their Transform Files

| Destination Table | Switch Case (BHGTaskRunner) | Save Method File | Key Transformation Logic |
|---|---|---|---|
| `pats.tbl_ENROLLMENT` | `case "pats.tbl_enrollment"` | `SaveEnrollment.cs` (499 lines) | Upsert by SiteCode+EnrollId. Handles null DischargeDate. RowChkSum comparison. |
| `pats.tbl_CLIENTDEMO1` | `case "pats.tbl_clientdemo1"` | `SaveCleints.cs` (735 lines) | `SaveClientDemo1var()`. SSN/PII field handling. |
| `pats.tbl_CLIENTDEMO2` | `case "pats.tbl_clientdemo2"` | `SaveCleints.cs` | `SaveClientDemo2()` for normal upsert, `SaveClientDemo3()` for ActionKey=3 variant. |
| `pats.tbl_UARESULTS` | `case "pats.tbl_uaresults"` | `SaveUAResults.cs` (698 lines) | `SaveUAResults()`. Handles Reload flag (delete+reinsert). Lab name mapping. |
| `pats.tbl_UARESULTDETAIL` | `case "pats.tbl_uaresultdetail"` | `SaveUAResults.cs` | `SaveUAResultDetail()`. Schema-gated fields (UardFullNote, UardKey, UardNote for old schema). |
| `pats.tbl_UASched` | `case "pats.tbl_uasched"` | `SaveUAResults.cs` | `SaveUASched()`. |
| `pats.tbl_SERVICES` | `case "pats.tbl_services"` | `SaveGlobal.cs` | `SaveServices()`. |
| `pats.tbl_FMP` | `case "pats.tbl_fmp"` | `SaveFmp.cs` (160 lines) | `SaveFmp()`. Financial management plan records. |
| `pats.tbl_LABRESULT` | `case "pats.tbl_labresult"` | `SaveInventory.cs` (829 lines) | `SaveLabResult()`. Lab order to result linkage. |
| `pats.tbl_LABRESULTDETAIL` | `case "pats.tbl_labresultdetail"` | `SaveInventory.cs` | `SaveLabResultDetail()`. |
| `pats.tbl_DIAG10` | `case "pats.tbl_tbldiag10"` | `BHGTaskRunner/Program.cs` inline | Direct upsert logic inline in switch. |
| `pats.tbl_GLOBALPAYOR` | `case "pats.tbl_globalpayor"` | `SaveGlobal.cs` | `SaveGlobalPayer()`. Payer/insurance company master. |
| `pats.tbl_PayerClient` | `case "pats.tbl_payerclient"` | `SavePayorClient.cs` (382 lines) | `SavePayerClient()` and `RemovePayerClients()` (for inactive payer removal). |
| `pats.tbl_PayerCltHistory` | `case "pats.tbl_payerclthistory"` | `SavePayorClient.cs` | `SavePayerCltHistory()`. |
| `pats.tbl_FEESCHED` | `case "pats.tbl_feesched"` | `SaveGlobal.cs` | `SaveFeeSchedules()`. Fee schedule entries per payer. |
| `pats.tbl_pbi3PAYauth` | `case "pats.tbl_pbi3payauth"` | `SaveAuths.cs` (426 lines) | `SaveAuths()`. Prior authorization records. |
| `pats.tbl_CustomAnswers` | `case "pats.tbl_customanswers"` | `SaveCustomQA.cs` (183 lines) | `SaveCustomAnswers()`. |
| `pats.tbl_CustomQuestions` | `case "pats.tbl_customquestions"` | `SaveCustomQA.cs` | `SaveCustomQuestions()`. |
| `pats.tbl_FORMSSAMMSCLIENT` | `case "pats.tbl_formssammsclient"` | `SaveGlobal.cs` | `SaveGlobalFormsSAMMSClients()`. Form completion records. |
| `pats.tbl_AppointmentAttend` | `case "pats.tbl_appointmentattend"` | `SaveAppointments.cs` (349 lines) | `SaveAppointmentAttend()`. |
| `pats.tbl_Appointments` | `case "pats.tbl_appointments"` | `SaveAppointments.cs` | `SaveAppointments()`. |
| `ctrl.tbl_INVTYPE` | `case "ctrl.tbl_invtype"` | `SaveInventory.cs` | `SaveInvType()`. Medication type master. |
| `ayx.tbl_PreAdmission_V6` | `case "ayx.tbl_preadmission_v6"` | `SavePreAdmissionV6.cs` (BHG-DR-LIB, 559 lines) | `SavePreAdmissionV6()`. Checks for `SF_PatientPreAdmission` table existence. Schema V5 sites skipped. Boolean → 'Yes'/'No' conversion for many fields. |
| `ctrl.tbl_claimstatus` | `case "ctrl.tbl_claimstatus"` | `BHGTaskRunner/Program.cs` inline | 12-month lookback. Direct save. |

#### P2 Tables and Their Transform Files (financial/billing)

| Destination Table | Switch Case | Save Method File | Key Logic |
|---|---|---|---|
| `pats.tbl_BILLS` | `case "pats.tbl_bills"` | `SaveBills.cs` (643 lines, both in BHG-DR-LIB and PHC) | `SaveBills()`. Financial transaction records. |
| `pats.tbl_CHECKIN` | `case "pats.tbl_checkin"` | `SaveCheckIn.cs` (132 lines) | `SaveCheckIn()`. Patient check-in events. |
| `pats.tbl_Claims` | `case "pats.tbl_claims"` | `SaveClaims.cs` (649 lines) | `SaveClaims()`. Insurance claims. |
| `pats.tbl_ClaimLineItem` | `case "pats.tbl_claimlineitem"` | `SaveClaims.cs` | `SaveClaimLineItem()`. Line items per claim. |
| `pats.tbl_ClaimLineItemActivity` | `case "pats.tbl_claimlineitemactivity"` | `SaveClaims.cs` | `SaveClaimLineItemActivity()`. Payment/adjustment activity per line item. |
| `pats.tbl_3pElig` | `case "pats.tbl_3pelig"` | `Save3pElig.cs` (555 lines) | `Save3pElig()`. Eligibility check records per payer. |
| `pats.tbl_EandMFormMDM` | `case "pats.tbl_eandmformmdm"` | `BHGTaskRunner/Program.cs` inline | E&M medical decision making form. |
| `pats.tbl_EandMFormPregnancy` | `case "pats.tbl_eandmformpregnancy"` | `BHGTaskRunner/Program.cs` inline | Pregnancy-related E&M form. |
| `pats.tbl_TreatmentLevel` | `case "pats.tbl_treatmentlevel"` | `SaveTreatmentLevel.cs` (109 lines) | `SaveTreatmentLevel()`. ASAM treatment level per client. |
| `pats.tbl_vw3pbill` | `case "pats.tbl_vw3pbill"` | `SaveAuths.cs` | `SaveAuthBillsub()`. Third-party billing view. |
| `pats.tbl_vw3pBillSub` | `case "pats.tbl_vw3pbillsub"` | `SaveAuths.cs` | Billing sub-record. |

---

### Pipeline 3 — Remaining Pipelines bucket (`BHGTaskRunner.exe 3`)

Arg `3` picks up everything NOT covered by args 1, 2, or 4. This is essentially a catch-all for specialty pipelines (Notes, Inv, DartSvc, Dose, Orders, Forms, LAB) when they are run under arg 3 scheduling. Each of those specialty pipelines has its own dedicated arg (7–11) as well.

---

### Pipeline 5 — Samms-LAB (`BHGTaskRunner.exe 5`)

**What it loads:** Clinical demographics for LAB-connected sites only.  
**Tables:** `pats.tbl_ClientDemo1`, `pats.tbl_ClientDemo2`

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | `case "pats.tbl_clientdemo1"`, `case "pats.tbl_clientdemo2"` |
| Save | `SaveCleints.cs` (735 lines) | `SaveClientDemo1var()`, `SaveClientDemo2()`. Same files as ETL P1 — but scoped to LAB SiteCode only. |
| Model | `BHG-DR-LIB/Models/TblClientDemo1.cs` | 36 demographic columns |
| | `BHG-DR-LIB/Models/TblClientDemo2.cs` | 57 program/enrollment columns |

---

### Pipeline 6 — Samms-Forms (`BHGTaskRunner.exe 6`)

**What it loads:** Dynamic form question/answer data from SAMMS forms engine.  
**Tables:** `pats.tbl_dbo_FormAnswerSignatures`, `pats.tbl_dbo_FormQuestionAnswers`

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | `case "pats.tbl_dbo_formanswersignatures"`, `case "pats.tbl_dbo_formquestionanswers"` |
| Save (BHG-DR-LIB) | `SaveFormQAData.cs` (1,986 lines) | `SaveFormAnswerSignatures()`, `SaveFormQuestionAnswers()`. Handles dynamic form schemas. |
| Save (PHC version) | `PHC/SaveFormQAData.cs` | PHC-specific form save variant. |
| Model | `BHG-DR-LIB/Models/TblDboFormAnswerSignatures.cs` | Signature records per form |
| | `BHG-DR-LIB/Models/TblDboFormQuestionAnswers.cs` | Q&A records per form submission |

**Key transformation notes:**
- These tables are schema-flexible — the form structure varies per SAMMS version
- `ctrl.tbl_Forms2Process` is used as config to decide which form names get processed
- Handles `IsChildForm` flag for nested forms
- Multiple signature types: patient, staff, counselor, doctor, supervisor, provider

---

### Pipeline 7 — SAMMS-ETL-Notes (`BHGTaskRunner.exe 7`)

**What it loads:** Third-party billing notes (AR notes and claim notes).  
**Tables:** `pats.tbl_3pARNOTE`, `pats.tbl_3pClaimNote`

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | `case "pats.tbl_3parnote"`, `case "pats.tbl_3pclaimnote"` |
| Save (3pARNote) | `BHG-DR-LIB/SaveGlobal.cs` | `Save3pArnote()`. AR note records attached to claim line items. LAB site: strips `globalBatchId` column (not present on old LAB schema). |
| Save (3pClaimNote) | `BHG-DR-LIB/SaveGlobal.cs` | `Save3pClaimNote()`. Notes attached to claims. Same LAB column strip. |
| Model | `BHG-DR-LIB/Models/Tbl3pArnote.cs` | 15 columns |
| | `BHG-DR-LIB/Models/Tbl3pClaimNote.cs` | 16 columns |

---

### Pipeline 8 — SAMMS-ETL-Inv (`BHGTaskRunner.exe 8`)

**What it loads:** Inventory, lab results, assessments, appointments, bottles, liquid log, orientation checklists, and all ASAM dimension assessment forms.  
**Tables:** `pats.tbl_Bottle`, `pats.tbl_LiquidLog`, `pats.tbl_LABRESULT`, `pats.tbl_LABRESULTDETAIL`, `pats.tbl_Appointments`, all ReAssessment tables, all AdmissionAssessment tables, `pats.tbl_OrientationChecklistNew`, `ctrl.tbl_INVTYPE`

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | Multiple switch cases — `case "pats.tbl_bottle"`, `case "pats.tbl_liquidlog"`, `case "pats.tbl_labresult"`, `case "pats.tbl_admissionassessment"`, `case "pats.tbl_reassessment"`, all dimension cases |
| Save Bottle/LiquidLog | `SaveInventory.cs` (829 lines) | `SaveBottle()`, `SaveLiquidLog()`. Inventory tracking and dispensing logs. |
| Save Lab Results | `SaveInventory.cs` | `SaveLabResult()`, `SaveLabResultDetail()`. Lab order → result → detail chain. |
| Save Appointments | `SaveAppointments.cs` (349 lines) | `SaveAppointments()`, `SaveAppointmentAttend()`. Scheduling data. |
| Save All Assessments | `SaveAssessments.cs` (3,239 lines) | The biggest transform file. Contains methods for: AdmissionAssessment, ReAssessment, all 6 PA Dimensions, PACounselorReview, all AdmissionAssessment Dimensions (1–6), SubstanceUseHistory, AssessmentSummary, ComprehensiveAssessmentForm, MNComprehensiveAssessment, VAComprehensiveAssessment, BAM Form/Score, COWS, NewAdmissionAssessment, NewPeriodicReassessment, FinancialHardshipApplication, PreAdmissionReferralSource, OrientationChecklistNew |
| Save PA Data | `SavePAData.cs` (2,177 lines) | `SavePA()`, `SavePADimension1–6()`, `SavePACounselorReview()`. Periodic assessment forms with all 6 ASAM dimensions. |
| Save CA Forms | `SaveCA.cs` (2,014 lines) | `SaveComprehensiveAssessmentForm()`, `SaveMNComprehensiveAssessment()`, `SaveVAComprehensiveAssessment()`. State-specific assessment forms (MN = Minnesota, VA = Veterans Administration). |
| Save BAM | `SaveBAM.cs` (550 lines) | `SaveBamForm()`, `SaveBamScore()`, `SaveTblDiags()`. Brief Addiction Monitor forms and scores. Diagnosis (ICD-10) records. |
| Save COWS | `SaveCows.cs` (387 lines) | `SaveClinicalOpiateWithdrawalScale()`, `SaveCowsV6()`. Two versions of COWS form (original and V6). |
| Save Inv Type | `SaveInventory.cs` | `SaveInvType()`. Medication/inventory type master per clinic. |
| Model classes | Many in `BHG-DR-LIB/Models/` | `TblBottle.cs`, `TblLiquidLog.cs`, `TblLabresult.cs`, `TblAdmissionAssessment.cs`, `TblReAssessment.cs`, `TblPADimension1–6.cs`, etc. |

---

### Pipeline 9 — SAMMS-ETL-DartSvc (`BHGTaskRunner.exe 9`)

**What it loads:** Counseling/treatment service records (the largest table in the system, partitioned by year).  
**Tables:** `pats.tbl_DartsSrv`, `pats.tbl_DartsSrv_2015` through `pats.tbl_DartsSrv_2024`, `pats.tbl_DartsSrvStg` (staging)

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | `case "pats.tbl_dartssrv"` — dynamic lookback logic to determine how many months back to pull |
| Bulk Extract | `BulkDartsSvc.cs` (336 lines in PHC, also in BHG-DR-LIB) | `SqlBulkCopy` into staging table `pats.tbl_DartsSrvStg`. Then calls a SQL Server stored procedure to merge staging → year-partitioned final tables. Bypasses EF Core for performance. |
| Save (EF path) | `SaveDartsSrvs.cs` (1,690 lines) | `SaveDartSrv2014()` through `SaveDartSrv2023()`. Each method handles one year partition. EF Core upsert per row. Used for smaller/historical loads. |
| Save (old path) | `SaveDartsSrvs-old.cs` | Older version of the same methods — kept for reference/fallback. |
| Staging helper | `SaveDartsSrv.cs` (176 lines) | Helper methods for the staging/bulk path. |
| Model classes | `BHG-DR-LIB/Models/TblDartsSrv.cs` | 57 columns — same structure used for all year partitions |
| | `BHG-DR-LIB/Models/TblDartsSrv_2015.cs` through `TblDartsSrv_2024.cs` | Same 57 columns, different table name per year |
| | `BHG-DR-LIB/Models/TblDartsSrvStg.cs` | Staging table — same structure |

**Key transformation notes:**
- DartsSrv is the BIGGEST table — potentially millions of rows per site
- Uses dual write paths: `SqlBulkCopy` (fast, bulk) vs EF upsert (slow, precise)
- Year partitioning: one physical table per year (2015 through 2024+)
- Dynamic lookback: BHGTaskRunner calculates how many months back to pull based on last successful run

---

### Pipeline 10 — SAMMS-ETL-Dose (`BHGTaskRunner.exe 10`)

**What it loads:** Daily medication dispensing records and dose excuses.  
**Tables:** `pats.tbl_DOSE`, `pats.tbl_DOSE_Excuse`

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | `case "pats.tbl_dose"`, `case "pats.tbl_dose_excuse"` |
| Save Doses | `SaveDoses.cs` (301 lines) | `SaveDoses()`. Upserts dispensing records. SiteId field included. Handles `BlVoid` (voided doses). Manages `Dtgiven` (actual administration time). |
| Save Dose Excuse | `SaveDoses.cs` | `SaveDoseExcuse()`. Records when a patient was excused from dosing. |
| Model | `BHG-DR-LIB/Models/TblDose.cs` | 30 columns including DoseId, CltId, DtMedDate, Dose amount, void flags, bottle type |
| | `BHG-DR-LIB/Models/TblDoseExcuse.cs` | 10 columns |

**Key transformation notes:**
- Dose is a very high-frequency table — one row per patient per day
- Has a **dual write path**: Dose records can go via EF Core upsert OR via BulkDartsSvc staging for large volumes
- `Reload` flag: if set, DELETE all existing doses for the site/date range before reinserting

---

### Pipeline 11 — SAMMS-ETL-Orders (`BHGTaskRunner.exe 11`)

**What it loads:** Medication orders (prescriptions/authorizations) — year-partitioned like DartsSrv.  
**Tables:** `pats.tbl_ORDERS`, `pats.tbl_ORDERS_2016` through `pats.tbl_ORDERS_2028`

| Transformation Step | File | Key Logic |
|---|---|---|
| Route | `BHGTaskRunner/Program.cs` | `case "pats.tbl_orders"` — splits DataTable in memory by year of OrderDate |
| Save | `SaveOrders.cs` (2,448 lines) | `SaveOrders2016()` through `SaveOrders2028()`. One method per year partition. |
| | | After extracting all orders for a site, the DataTable is filtered in C# memory: `SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2025)` — then each year's subset is passed to its year-specific method. |
| Model | `BHG-DR-LIB/Models/TblOrders.cs` | 57 columns including OrderNum, CltId, MedType, Dose, EffectiveDate, ExpirationDate, day-of-week flags (Sunday/Monday/.../Saturday), signatures |
| | `BHG-DR-LIB/Models/TblOrders2016.cs` through `TblOrders2028.cs` | Same 57 columns, different table per year |

**Key transformation notes:**
- Orders have day-of-week flag columns (Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday) — boolean flags per day
- Two-dose-per-day support: Sunday2, Monday2, etc.
- Signature tracking: SigDr, DtSig, Sigentered, Signoted columns
- Year split is done **in C# memory** — one SAMMS query returns all orders, then C# splits by year before writing

---

## Summary — Which File Handles Which Pipeline

| Pipeline | Arg | Key Transform Files |
|---|---|---|
| SAMMSGlobal | 1 | `SaveGlobal.cs`, `SaveClinic.cs`, `SaveCodes.cs`, `SaveAuths.cs` |
| Eastern/Central/Mountain/Pacific P1 | 2 | `SaveEnrollment.cs`, `SaveCleints.cs`, `SaveUAResults.cs`, `SaveFmp.cs`, `SaveInventory.cs`, `SaveAppointments.cs`, `SaveGlobal.cs`, `SavePayorClient.cs`, `Save3pElig.cs`, `SavePreAdmissionV6.cs`, `SaveTreatmentLevel.cs`, `SaveCustomQA.cs` |
| Remaining catch-all | 3 | Same as pipelines 6–11 |
| Eastern/Central/Mountain/Pacific P2 | 4 | `SaveBills.cs`, `SaveCheckIn.cs`, `SaveClaims.cs`, `SavePayorClient.cs`, `SaveAuths.cs`, `SaveTreatmentLevel.cs` |
| Samms-LAB | 5 | `SaveCleints.cs` |
| Samms-Forms | 6 | `SaveFormQAData.cs` (both BHG-DR-LIB and PHC versions) |
| SAMMS-ETL-Notes | 7 | `SaveGlobal.cs` (Save3pArnote, Save3pClaimNote methods) |
| SAMMS-ETL-Inv | 8 | `SaveInventory.cs`, `SaveAssessments.cs`, `SavePAData.cs`, `SaveCA.cs`, `SaveBAM.cs`, `SaveCows.cs`, `SaveAppointments.cs` |
| SAMMS-ETL-DartSvc | 9 | `SaveDartsSrvs.cs`, `BulkDartsSvc.cs`, `SaveDartsSrv.cs` |
| SAMMS-ETL-Dose | 10 | `SaveDoses.cs` |
| SAMMS-ETL-Orders | 11 | `SaveOrders.cs` |

---

## Full File List — Every Transformation File

```
BCAppCode/
├── BHGTaskRunner/
│   └── Program.cs                    ← ROUTER for all 11 pipelines (3,375 lines)
│
├── BHG-DR-LIB/
│   ├── SelectConstructor.cs          ← Builds SELECT queries (shared by all)
│   ├── SQLSvrManager.cs              ← Executes SQL on SAMMS (shared by all)
│   ├── SaveData.cs                   ← Base partial class for all Save methods
│   │
│   ├── Save3pElig.cs                 ← Pipeline 2/4: Eligibility checks
│   ├── SaveAppointments.cs           ← Pipeline 8: Appointments + Attendance
│   ├── SaveAssessments.cs            ← Pipeline 8: ALL assessment forms (3,239 lines!)
│   ├── SaveAuths.cs                  ← Pipeline 1/2: Prior authorizations + billing
│   ├── SaveBAM.cs                    ← Pipeline 8: Brief Addiction Monitor + Diagnoses
│   ├── SaveBills.cs                  ← Pipeline 4: Financial billing records
│   ├── SaveCA.cs                     ← Pipeline 8: Comprehensive Assessment forms (2,014 lines)
│   ├── SaveCheckIn.cs                ← Pipeline 4: Patient check-in events
│   ├── SaveClaims.cs                 ← Pipeline 4: Insurance claims + line items
│   ├── SaveCleints.cs                ← Pipeline 2/5: Client demographics (note: typo in filename)
│   ├── SaveClinic.cs                 ← Pipeline 1: Clinic configuration
│   ├── SaveCodes.cs                  ← Pipeline 1: Service/billing codes
│   ├── SaveCows.cs                   ← Pipeline 8: COWS withdrawal scale
│   ├── SaveCustomQA.cs               ← Pipeline 2: Custom questions + answers
│   ├── SaveDartsSrv.cs               ← Pipeline 9: DartsSrv helpers
│   ├── SaveDartsSrvs.cs              ← Pipeline 9: DartsSrv year-partitioned upserts (1,690 lines)
│   ├── SaveDartsSrvs-old.cs          ← Pipeline 9: Old version (kept for reference)
│   ├── SaveDoses.cs                  ← Pipeline 10: Medication doses + excuses
│   ├── SaveEnrollment.cs             ← Pipeline 2: Patient enrollment records
│   ├── SaveFmp.cs                    ← Pipeline 2: Financial management plans
│   ├── SaveFormQAData.cs             ← Pipeline 6: Dynamic form Q&A data (1,986 lines)
│   ├── SaveGlobal.cs                 ← Pipeline 1/2/7: Users, consents, devices, notes (2,208 lines)
│   ├── SaveGlobal-old.cs             ← Old version (kept for reference)
│   ├── SaveGlobalorg.cs              ← Older global save variant
│   ├── SaveInventory.cs              ← Pipeline 8: Bottles, liquid log, lab results, inv types
│   ├── SaveOrders.cs                 ← Pipeline 11: Year-partitioned medication orders (2,448 lines)
│   ├── SavePAData.cs                 ← Pipeline 8: Periodic Assessment + all 6 ASAM dimensions (2,177 lines)
│   ├── SavePayorClient.cs            ← Pipeline 2/4: Payer client insurance records
│   ├── SavePreAdmissionV6.cs         ← Pipeline 2: Pre-admission intake form V6
│   ├── SaveRowTrax.cs                ← ALL pipelines: Row count audit trail
│   ├── SaveTreatmentLevel.cs         ← Pipeline 4: ASAM treatment level
│   ├── SaveUAResults.cs              ← Pipeline 2: Urine analysis results + schedule
│   │
│   ├── BulkDartsSvc.cs               ← Pipeline 9: SqlBulkCopy for DartsSrv
│   │
│   └── Models/
│       └── Tbl*.cs                   ← EF Core entity classes (destination table shapes)
│
└── PHC/
    ├── BulkDartsSvc.cs               ← PHC version of bulk DartsSrv loader
    ├── SaveBills.cs                  ← PHC-specific bills transformer
    ├── SaveFormQAData.cs             ← PHC-specific form Q&A transformer
    ├── SavePreAdmissionV6.cs         ← PHC-specific pre-admission transformer
    └── SaveUAResults.cs              ← PHC-specific UA results transformer
```

---

## Key Observation

**There are NO separate transformation folders per pipeline.** All 11 pipelines share the same `BHG-DR-LIB/Save*.cs` files. The pipeline separation only happens:

1. In `BHGTaskRunner/Program.cs` — the `switch(args[0])` that picks which parent tasks to run
2. In `Scheduler.sql` logic — the CASE statement that assigns each (site × table) to a specific pipeline name
3. In the database task queue (`tsk.tbl_Tasks2`) — which parent task a child task is linked to

The **transformation logic itself** is reused across pipelines. For example, `SaveEnrollment.cs` is called whether the task came from Eastern P1 or Central P1 — same code, different site's data.

---

## STORED PROCEDURES — Complete Map

Yes — stored procedures (SPs) are a real and important part of the transformation layer. They live in the Azure **BHG_DR** database (not in the C# code). The C# code calls them after bulk-loading data into staging tables. There are two categories:

### Category A: Staging → Final Table Merge SPs (`stg` schema)

These SPs are called by `BulkDartsSvc.cs` after a `SqlBulkCopy` bulk-load into a staging table. Their job is to **MERGE staging data into the real destination tables** using SQL MERGE statements server-side. This is the "bulk path" — faster than EF Core row-by-row upserts for high-volume tables.

The pattern for all of them is:
```
C# SqlBulkCopy → stg.tbl_XXX (staging) → exec stg.XXXMerge → pats.tbl_XXX (final)
```

| Stored Procedure | Called From | Staging Table | Final Destination | Pipeline | Purpose |
|---|---|---|---|---|---|
| `stg.ClaimsMerge` | `BulkDartsSvc.cs` (line 279) | `stg.tbl_claims` | `pats.tbl_Claims` | P2 (Financial) | Merges insurance claim records from staging into final claims table per site |
| `stg.ClaimLineItemMerge` | `BulkDartsSvc.cs` (line 282) | `stg.tbl_claimlineitem` | `pats.tbl_ClaimLineItem` | P2 (Financial) | Merges claim line items per site |
| `stg.ClaimLineItemActivityMerge` | `BulkDartsSvc.cs` (line 285) | `stg.tbl_claimlineitemactivity` | `pats.tbl_ClaimLineItemActivity` | P2 (Financial) | Merges payment/adjustment activity per claim line item |
| `stg.ClientDemoMerge1` | `BulkDartsSvc.cs` (line 288) | `stg.clientdemo` | `pats.tbl_ClientDemo1` | SAMMSGlobal / LAB | Merges client demographic data (part 1 — personal info) |
| `stg.ClientDemoMerge2` | `BulkDartsSvc.cs` (line 289) | `stg.clientdemo` | `pats.tbl_ClientDemo2` | SAMMSGlobal / LAB | Merges client demographic data (part 2 — program info). Called immediately after ClientDemoMerge1 on same staging data. |
| `stg.DartsSrvMerge` | `BulkDartsSvc.cs` (line 292) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv` (current year) | SAMMS-ETL-DartSvc (9) | Merges counseling service records for current/recent years |
| `stg.DartsSrvMerge22` | `BulkDartsSvc.cs` (line 293) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2022` | SAMMS-ETL-DartSvc (9) | Merges 2022 service records from staging |
| `stg.DartsSrvMerge23` | `BulkDartsSvc.cs` (line 294) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2023` | SAMMS-ETL-DartSvc (9) | Merges 2023 service records from staging |
| `stg.DartsSrvMerge24` | `BulkDartsSvc.cs` (line 295) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2024` | SAMMS-ETL-DartSvc (9) | Merges 2024 service records from staging |
| `stg.DartsSrvMerge25` | `BulkDartsSvc.cs` (line 296) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2025` | SAMMS-ETL-DartSvc (9) | Merges 2025 service records from staging |
| `stg.DartsSrvMerge26` | `BulkDartsSvc.cs` (line 297) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2026` | SAMMS-ETL-DartSvc (9) | Merges 2026 service records from staging |
| `stg.DartsSrvMerge27` | `BulkDartsSvc.cs` (line 298) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2027` | SAMMS-ETL-DartSvc (9) | Merges 2027 service records from staging |
| `stg.DartsSrvMerge28` | `BulkDartsSvc.cs` (line 299) | `stg.tbl_dartssrv` | `pats.tbl_DartsSrv_2028` | SAMMS-ETL-DartSvc (9) | Merges 2028 service records from staging |
| `stg.DoseMerge` | `BulkDartsSvc.cs` (line 302) | `stg.tbl_dose` | `pats.tbl_DOSE` | SAMMS-ETL-Dose (10) | Merges daily medication dispensing records per site |
| `stg.Dose_ExcuseMerge` | `BulkDartsSvc.cs` (line 305) | `stg.tbl_dose_excuse` | `pats.tbl_DOSE_Excuse` | SAMMS-ETL-Dose (10) | Merges dose excuse records (when patient was excused from dosing) |
| `stg.FormsSAMMSMerge` | `BulkDartsSvc.cs` (line 314) | `stg.tbl_formssammsclient` | `pats.tbl_FormsSAMMSClient` | Samms-Forms (6) | Merges SAMMS form completion records for non-PHC sites |
| `stg.FormsSAMMSMergePHC` | `BulkDartsSvc.cs` (line 310) | `stg.tbl_formssammsclient` | `pats.tbl_FormsSAMMSClient` | PHC ETL | PHC-specific version of the forms merge SP |
| `stg.UAResultDetailMerge` | `BulkDartsSvc.cs` (line 321) | `stg.tbl_uaresultdetail` | `pats.tbl_UARESULTDETAIL` | ETL P1 | Merges urine analysis result detail records per site |
| `stg.LABResultDetailMerge` | `BulkDartsSvc.cs` (line 324) | `stg.tbl_labresultdetail` | `pats.tbl_LABRESULTDETAIL` | SAMMS-ETL-Inv (8) | Merges lab result detail records per site |
| `stg.sp_BillSubMerge` | `BulkDartsSvc.cs` (line 327) | `stg.tbl_vw3pbillsub` | `pats.tbl_vw3pBillSub` | P2 (Financial) | Merges third-party billing sub-records per site |
| `stg.sp_liquidlog_Merge` | `BHGTaskRunner/Program.cs` (line 477) | `stg.tbl_liquidlog` | `pats.tbl_LiquidLog` | SAMMS-ETL-Inv (8) | Merges liquid medication dispensing log per site. Called with `@sitecode` parameter. |
| `stg.sp_FormQA_Merge` | `BHGTaskRunner/Program.cs` (line 2172) | `stg.tbl_FormQA` | `pats.tbl_dbo_FormQuestionAnswers` | Samms-Forms (6) | Merges form question/answer data for bulk-path sites (specific site list only) |

---

### Category B: Post-Load Aggregation/Calculation SPs (`pats` schema)

These SPs run **after** the ETL load is complete. They compute aggregations, KPIs, or cross-table calculations server-side in Azure SQL. They do not read from SAMMS — they work entirely within BHG_DR.

| Stored Procedure | Called From | When It Runs | Pipeline | Purpose |
|---|---|---|---|---|
| `pats.BAMMerge` | `BHGTaskRunner/Program.cs` (lines 2173, 2185) | After `pats.tbl_dbo_FormQuestionAnswers` is loaded for each site | Samms-Forms (6) | Computes BAM (Brief Addiction Monitor) scores from raw form Q&A answers. Takes `@sitecode` parameter. Runs per-site after forms load. |
| `pats.BAMMergeGbl` | `BHGTaskRunner/Program.cs` (line 565) | After `pats.tbl_BamForm` is loaded | SAMMS-ETL-Inv (8) | Global/aggregated version of BAM merge. Takes `@sitecode = 'Global'`. Runs after all sites' BAM forms are loaded. |
| `pats.Populate_BAM_Bucketed` | `BHG/AzureAgent/Program.cs` (line 125) | ~6:24 AM by AzureAgent timer | AzureAgent (not ETL pipeline) | Populates a bucketed/summarized BAM results table for reporting. Runs on a timer, not triggered by ETL pipelines. |
| `pats.SP_CounselingStateReq` | `BHG/AzureAgent/Program.cs` (line 227) | Scheduled time by AzureAgent | AzureAgent (not ETL pipeline) | Computes counseling state requirements (GroupUnits, IndividualUnits, UpToDate flags). Uses INSERT…EXEC pattern. Takes `@dt` date parameter. |
| `pats.SP_MedInvMerge` | `BHG/AzureAgent/Program.cs` (line 246) | Scheduled time by AzureAgent | AzureAgent (not ETL pipeline) | Merges medication inventory data across sites in BHG_DR. Runs independently of ETL pipelines. |
| `pats.MergeServicesMissingSigCode` | `BHG/AzureAgent/Program.cs` (line 159, commented out) | Was scheduled by AzureAgent | AzureAgent (currently disabled) | Was intended to fix services records missing a sig code. Currently commented out — not running. |

---

### Category C: Inline SQL (not SPs but server-side logic in C#)

Some transformations use inline SQL strings with server-side logic instead of stored procedures:

| Operation | File | Line | Pipeline | What It Does |
|---|---|---|---|---|
| `UPDATE pats.tbl_FormsSAMMSClient SET SiteCode = ...` | `BulkDartsSvc.cs` (line 276) | After bulk load | Samms-Forms (6) | Inline UPDATE that resolves `SiteCode` from `ctrl.tbl_Locations` join on `sID` field. Fixes `fscCLTID < 0` records. Not a stored procedure but server-side logic. |
| `DELETE from pats.tbl_Dose WHERE SiteCode = '...'` | `BHGTaskRunner/Program.cs` (line 932) | Before reload | SAMMS-ETL-Dose (10) | When `Reload` flag is set, deletes ALL dose records for a site before re-inserting. Inline DELETE. |
| `TRUNCATE TABLE stg.tbl_liquidlog` | `BHGTaskRunner/Program.cs` (line 463) | Before bulk load | SAMMS-ETL-Inv (8) | Clears staging table before SqlBulkCopy. |
| `TRUNCATE TABLE stg.tbl_FormQA` | `BHGTaskRunner/Program.cs` (line 2158) | Before bulk load | Samms-Forms (6) | Clears forms staging table before SqlBulkCopy. |
| `TRUNCATE TABLE stg.tbl_XXX` | `BulkDartsSvc.cs` (line 251, 334) | Before and after each bulk | All bulk-path pipelines | Clears every staging table before loading and again after merge completes. |

---

### How Staging + SP Pattern Works (The Bulk Path)

For high-volume tables the ETL uses a **two-step bulk approach** instead of EF Core row-by-row:

```
STEP 1: C# SqlBulkCopy
────────────────────────────────────────────────────────────
C# reads from SAMMS → DataTable in RAM
SqlBulkCopy.WriteToServer(DataTable)
→ TRUNCATE stg.tbl_XXX  (clear staging first)
→ Bulk insert ALL rows into stg.tbl_XXX  (very fast, no row-by-row)

STEP 2: SQL Server Stored Procedure (the MERGE)
────────────────────────────────────────────────────────────
exec stg.XXXMerge '@sitecode'
→ Inside the SP, SQL Server runs a MERGE statement:
   MERGE pats.tbl_XXX AS target
   USING stg.tbl_XXX  AS source
   ON (target.SiteCode = source.SiteCode AND target.PrimaryKey = source.PrimaryKey)
   WHEN MATCHED AND target.RowChkSum <> source.RowChkSum THEN UPDATE ...
   WHEN NOT MATCHED THEN INSERT ...
→ TRUNCATE stg.tbl_XXX  (clean up staging after merge)
```

This approach is used for:
- `DartsSrv` (millions of counseling records)
- `Dose` (one record per patient per day × all sites)
- `Claims`, `ClaimLineItem`, `ClaimLineItemActivity` (large financial tables)
- `LiquidLog` (dispensing pump logs)
- `FormQuestionAnswers` (for large/complex sites)
- `UAResultDetail`, `LABResultDetail`
- `BillSub`, `FormsSAMMSClient`

---

### Complete Stored Procedure Reference Table

| SP Name | Schema | Type | Called By | Pipeline | Active? |
|---|---|---|---|---|---|
| `ClaimsMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | P2 Financial | Yes |
| `ClaimLineItemMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | P2 Financial | Yes |
| `ClaimLineItemActivityMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | P2 Financial | Yes |
| `ClientDemoMerge1` | `stg` | Staging Merge | `BulkDartsSvc.cs` | SAMMSGlobal / LAB | Yes |
| `ClientDemoMerge2` | `stg` | Staging Merge | `BulkDartsSvc.cs` | SAMMSGlobal / LAB | Yes |
| `DartsSrvMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge22` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge23` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge24` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge25` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge26` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge27` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DartsSrvMerge28` | `stg` | Staging Merge | `BulkDartsSvc.cs` | DartSvc (9) | Yes |
| `DoseMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | Dose (10) | Yes |
| `Dose_ExcuseMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | Dose (10) | Yes |
| `FormsSAMMSMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | Forms (6) | Yes |
| `FormsSAMMSMergePHC` | `stg` | Staging Merge | `BulkDartsSvc.cs` | PHC ETL | Yes |
| `UAResultDetailMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | ETL P1 | Yes |
| `LABResultDetailMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | ETL-Inv (8) | Yes |
| `sp_BillSubMerge` | `stg` | Staging Merge | `BulkDartsSvc.cs` | P2 Financial | Yes |
| `sp_liquidlog_Merge` | `stg` | Staging Merge | `BHGTaskRunner/Program.cs` | ETL-Inv (8) | Yes |
| `sp_FormQA_Merge` | `stg` | Staging Merge | `BHGTaskRunner/Program.cs` | Forms (6) | Yes |
| `BAMMerge` | `pats` | Post-Load Aggregation | `BHGTaskRunner/Program.cs` | Forms (6) | Yes |
| `BAMMergeGbl` | `pats` | Post-Load Aggregation | `BHGTaskRunner/Program.cs` | ETL-Inv (8) | Yes |
| `Populate_BAM_Bucketed` | `pats` | Scheduled Aggregation | `AzureAgent/Program.cs` | AzureAgent | Yes |
| `SP_CounselingStateReq` | `pats` | Scheduled KPI Calc | `AzureAgent/Program.cs` | AzureAgent | Yes |
| `SP_MedInvMerge` | `pats` | Scheduled Merge | `AzureAgent/Program.cs` | AzureAgent | Yes |
| `MergeServicesMissingSigCode` | `pats` | Scheduled Fix | `AzureAgent/Program.cs` | AzureAgent | **Disabled** (commented out) |
| `FormsMergeCounts` | `stg` | Staging Merge | `BulkDartsSvc.cs` | Forms (6) | **Disabled** (commented out) |
