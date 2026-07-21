# Scheduler — ETL Schedules and Associated Tables

> **Source note:** Scheduler routing documented here is from `updatedSchedulerProgrma.cs` (current). Legacy `Program.cs` differs — see [Updated vs legacy Scheduler](#updated-vs-legacy-scheduler) below. Table lists also use `dms.vw_MapAction` (`BCAppCode/Framework/vw_mapAction.csv`).

## Purpose

`Scheduler.exe` is a .NET Core console app that runs once per day (typically via Windows Task Scheduler). It does **not** move data. It builds the daily task queue in Azure `BHG_DR` that `BHGTaskRunner.exe` later executes.

```
tsk.tbl_Schedule          →  parent tasks in tsk.tbl_Tasks2
dms.vw_MapAction          →  child tasks (one row per site × destination table)
BHGTaskRunner.exe <1-12>  →  picks up Status=17 tasks and runs ETL
```

---

## Updated vs legacy Scheduler

Changes in `updatedSchedulerProgrma.cs` compared to checked-in `Program.cs`:

| Area | Legacy `Program.cs` | Updated `updatedSchedulerProgrma.cs` |
|------|---------------------|----------------------------------------|
| Parent task `SiteCode` | `Case when scheduleid = 18 then 'PHC' else 'All' end` | Always `'All'` |
| New batch | — | `SAMMS-ETL-PPA` for `pats.tbl_SF_PatientPreAdmission` (first CASE branch) |
| ASAM dimension tables | P1 for all timezones | `pats.tbl_NewAdmissionAssessmentASAMDimension2/4/5` moved to **P2 for all timezones** |
| EST/CST P2 list | Included `pats.tbl_Orders` | `pats.tbl_Orders` removed from P2 list *(still routed to `SAMMS-ETL-Orders` by earlier CASE branch)* |
| MST/PST P1 exclude | No ASAM dimension excludes | Adds ASAM Dimension 2/4/5 to P1 exclude lists |

**Runner pairing:** `SAMMS-ETL-PPA` is handled by `BHGTaskRunner.exe 12` in `updatedProgram.cs` (not in legacy `Program.cs` args 1–11).

---

## Control and Scheduling Tables (Azure BHG_DR)


| Table / View       | Schema | Role                                                                       |
| ------------------ | ------ | -------------------------------------------------------------------------- |
| `tbl_Schedule`     | `tsk`  | Defines enabled ETL schedules: name, `NextRunTime`, `ActionKey`, `Enabled` |
| `tbl_Tasks2`       | `tsk`  | Task queue — parent rows (batch) and child rows (site + table)             |
| `vwTaskList`       | `tsk`  | View joined by `BHGTaskRunner`; exposes tasks with mapping metadata        |
| `vw_MapAction`     | `dms`  | Which source objects map to which destination tables per site              |
| `vw_MapSrc2Dsn`    | `dms`  | Column-level mapping used by `SelectConstructor` at run time               |
| `tbl_LocationCons` | `ctrl` | Per-clinic SAMMS connection strings (used by runner, not scheduler)        |
| `tbl_RowTrax`      | `tsk`  | Row-count audit trail written by runner after each child task              |


### Task status values (Scheduler / Runner)


| Field      | Value | Meaning                                                       |
| ---------- | ----- | ------------------------------------------------------------- |
| `Status`   | 17    | Pending — ready for `BHGTaskRunner`                           |
| `Status`   | 18    | Processing — runner is executing                              |
| `Status`   | 19    | Completed                                                     |
| `Status`   | 20    | Error                                                         |
| `RowState` | 24    | Active child task                                             |
| `RowState` | 26    | Skipped / archived — excluded from run and eventually deleted |


---

## Daily Scheduler Flow

1. **Reset stuck tasks:** `Status 18 → 17` on `tsk.tbl_Tasks2`
2. **Archive stale vwTaskList rows:** set `RowState = 26` where `WorkDate < today` and `WhereCondition = '1 = 1'`
3. **Insert parent tasks** from `tsk.tbl_Schedule` where `Enabled = 1` → one parent row per schedule for today (`SiteCode = 'All'` on parent in updated scheduler)
4. **Insert child tasks** by cross-joining `dms.vw_MapAction` (active rows, `ConnectionID <> 3`) with today's parent tasks
5. **Assign batch name** to each child using the CASE logic below (must match parent `TaskName`)
6. **Mark unsupported combos** as `RowState = 26` (see Exclusions section)
7. **Advance schedule:** `NextRunTime += 1 day`, `LastRunTime += 1 day`
8. **Purge old tasks:** delete from `tsk.tbl_Tasks2` where `RunAt <= 3 months ago` OR `RowState = 26`

---

## ETL Schedules → BHGTaskRunner Argument


| BHGTaskRunner arg | Parent `TaskName` in `tbl_Tasks2`                                       | Description                                                                                     |
| ----------------- | ----------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------- |
| `1`               | `SAMMSGlobal`                                                           | Global / shared reference tables                                                                |
| `2`               | `Eastern ETL P1`, `Central ETL P1`, `Mountain ETL P1`, `Pacific ETL P1` | Regional Phase 1 — core clinical / operational tables by timezone                               |
| `3`               | *(catch-all)*                                                           | Everything not in P1, P2, or Global — includes PHC, specialty bundles not run by dedicated args |
| `4`               | `Eastern ETL P2`, `Central ETL P2`, `Mountain ETL P2`, `Pacific ETL P2` | Regional Phase 2 — billing, claims, payer, check-in, etc.                                       |
| `5`               | `Samms-LAB`                                                             | LAB site client demographics                                                                    |
| `6`               | `Samms-Forms`                                                           | Dynamic form answers and signatures                                                             |
| `7`               | `SAMMS-ETL-Notes`                                                       | Third-party clinical notes                                                                      |
| `8`               | `SAMMS-ETL-INV`                                                         | Inventory, lab results, assessments, appointments                                               |
| `9`               | `SAMMS-ETL-DartSvc`                                                     | DART counseling session records                                                                 |
| `10`              | `SAMMS-ETL-Dose`                                                        | Medication dose records                                                                         |
| `11`              | `SAMMS-ETL-Orders`                                                      | Prescription orders (year-partitioned in Azure)                                                 |
| `12`              | `SAMMS-ETL-PPA`                                                         | Patient pre-admission (`pats.tbl_SF_PatientPreAdmission`) — updated scheduler + `updatedProgram.cs` only |


PHC child tasks still route to parent `**PHC ETL**` via `SiteCode = 'PHC'` in the CASE logic. PHC is handled by the separate `PHC` project, not the main `BHGTaskRunner` SAMMS batches. *(Legacy scheduler also set parent `SiteCode = 'PHC'` when `scheduleid = 18`; updated scheduler always uses `'All'` on the parent row.)*

---

## Child Task Routing Logic (from `updatedSchedulerProgrma.cs`)

The Scheduler assigns each `vw_MapAction` row to a parent batch using this priority order:


| Condition                                                                      | Parent `TaskName`   |
| ------------------------------------------------------------------------------ | ------------------- |
| `pats.tbl_SF_PatientPreAdmission`                                            | `SAMMS-ETL-PPA`     |
| `SiteCode = 'PHC'`                                                             | `PHC ETL`           |
| `SiteCode = 'LAB'` AND table in `pats.tbl_ClientDemo1`, `pats.tbl_ClientDemo2` | `Samms-LAB`         |
| Table in form tables list                                                      | `Samms-Forms`       |
| Table in notes list                                                            | `SAMMS-ETL-Notes`   |
| Table in inventory/assessment list                                             | `SAMMS-ETL-INV`     |
| `pats.tbl_DartsSrv`                                                            | `SAMMS-ETL-DartSvc` |
| `pats.tbl_Dose`, `pats.tbl_Dose_Excuse`                                        | `SAMMS-ETL-Dose`    |
| `pats.tbl_Orders`                                                              | `SAMMS-ETL-Orders`  |
| `TimeZone = EST` + table in P2 list                                            | `Eastern ETL P2`    |
| `TimeZone = EST` + table **not** in P1 exclude list                            | `Eastern ETL P1`    |
| `TimeZone = CST` + table in P2 list                                            | `Central ETL P2`    |
| `TimeZone = CST` + table **not** in P1 exclude list                            | `Central ETL P1`    |
| `TimeZone = MST` + table in P2 list                                            | `Mountain ETL P2`   |
| `TimeZone = MST` + table **not** in P1 exclude list                            | `Mountain ETL P1`   |
| `TimeZone = PST` + table in P2 list                                            | `Pacific ETL P2`    |
| `TimeZone = PST` + table **not** in P1 exclude list                            | `Pacific ETL P1`    |
| *(default)*                                                                    | `SAMMSGlobal`       |


### Explicit table lists in routing CASE

**Samms-Forms**

- `pats.tbl_dbo_FormAnswerSignatures`
- `pats.tbl_dbo_FormQuestionAnswers`

**SAMMS-ETL-Notes**

- `pats.tbl_3parnote`
- `pats.tbl_3pclaimnote`

**SAMMS-ETL-INV**

- `pats.tbl_Bottle`, `pats.tbl_LiquidLog`, `ctrl.Tbl_InvType`, `pats.tbl_orientationchecklistnew`
- `pats.tbl_labresult`, `pats.tbl_labresultdetail`, `pats.Tbl_Appointments`
- `pats.Tbl_AdmissionAssessment`, `pats.Tbl_AdmissionAssessmentSummary`, `pats.Tbl_ReAssessment`
- `pats.Tbl_AdmissionAssessmentDimensionOneDisorder`, `pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse`, `pats.Tbl_AdmissionAssessmentDimensionTwo`
- `pats.Tbl_ReAssessmentOccupational`, `pats.Tbl_ReAssessmentFamily`, `pats.Tbl_ReAssessmentLegal`, `pats.Tbl_ReAssessmentMentalHealth`
- `pats.Tbl_ReAssessmentPhysicalHealth`, `pats.Tbl_ReAssessmentSubstanceUse`, `pats.Tbl_ReAssessmentSocial`, `pats.Tbl_ReAssessmentTreatment`
- `pats.Tbl_AdmissionAssessmentDimensionThree`, `pats.tbl_AdmissionAssessmentDimensionSix`, `pats.tbl_AdmissionAssessmentDimensionfour`

**SAMMS-ETL-PPA** *(updated scheduler only)*

- `pats.tbl_SF_PatientPreAdmission` → source `dbo.SF_PatientPreAdmission` per site

**Dedicated single-table ETLs**

- `pats.tbl_DartsSrv` → `SAMMS-ETL-DartSvc`
- `pats.tbl_Dose`, `pats.tbl_Dose_Excuse` → `SAMMS-ETL-Dose`
- `pats.tbl_Orders` → `SAMMS-ETL-Orders`

**Samms-LAB** (LAB site only)

- `pats.tbl_ClientDemo1`
- `pats.tbl_ClientDemo2`

### Regional P2 tables (by timezone)

**Eastern (EST) P2**

- `pats.tbl_claims`, `pats.tbl_claimlineitem`, `pats.tbl_claimlineitemactivity`
- `pats.tbl_dose_excuse`, `pats.tbl_uaresultdetail`
- `pats.tbl_GlobalPayor`, `pats.tbl_PayerClient`, `pats.tbl_feesched`, `pats.tbl_CheckIn`
- `pats.tbl_EandMFormPregnancy`, `pats.tbl_EandMFormMDM`, `pats.tbl_Bills`, `pats.tbl_payerclthistory`
- `pats.tbl_treatmentlevel`
- `pats.tbl_NewAdmissionAssessmentASAMDimension2`, `pats.tbl_NewAdmissionAssessmentASAMDimension4`, `pats.tbl_NewAdmissionAssessmentASAMDimension5`

**Central (CST) P2** — same as Eastern except adds `pats.tbl_Enrollment`, omits `pats.tbl_payerclthistory`

**Mountain / Pacific (MST/PST) P2**

- `pats.tbl_claims`, `pats.tbl_claimlineitem`, `pats.tbl_claimlineitemactivity`, `pats.tbl_treatmentlevel`
- `pats.tbl_dose_excuse`, `pats.tbl_uaresultdetail`, `pats.tbl_Enrollment`
- `pats.tbl_GlobalPayor`, `pats.tbl_PayerClient`, `pats.tbl_FeeSched`, `pats.tbl_EandMFormMDM`, `pats.tbl_payerclthistory`
- `pats.tbl_NewAdmissionAssessmentASAMDimension2`, `pats.tbl_NewAdmissionAssessmentASAMDimension4`, `pats.tbl_NewAdmissionAssessmentASAMDimension5`

Tables in P2 lists are **excluded** from the corresponding timezone P1 batch. *(Updated scheduler also excludes ASAM Dimension 2/4/5 from P1 in all four timezones.)*

**P1 exclude lists (updated scheduler)** additionally include ASAM Dimension 2/4/5 for EST, CST, MST, and PST alongside the billing/clinical tables listed in the CASE statements.

---

## Excluded Site + Table Combinations (`RowState = 26`)

After child tasks are inserted, the Scheduler marks these as skipped:


| Rule                  | Tables / Sites                                                                                                              |
| --------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| PHC-only skip         | `pats.tbl_BriefAddictionMonitor`, `pats.tbl_clinicalopiatewithdrawalscale`, `pats.tbl_vw3pBillSub` where `SiteCode = 'PHC'` |
| LAB PayerClient       | `pats.tbl_PayerClient` where `SiteCode = 'LAB'`, `ActionKey = 1`, `ActionStepKey = 6`                                       |
| PHC COWS              | `pats.tbl_Cows_V6` where `SiteCode = 'PHC'`, `ActionKey = 1`, `ActionStepKey = 23`                                          |
| PreAdmission V6       | `ayx.tbl_PreAdmission_V6` where `SiteCode IN ('PHC','LAB')`; also `vwTaskList` where `SchemaVersion = 'V5'`                 |
| E&M forms             | `pats.tbl_EandMFormMDM`, `pats.tbl_EandMFormPregnancy` where `SiteCode IN ('PHC','LAB')`                                    |
| LAB Appointments      | `pats.tbl_Appointments` where `SiteCode = 'LAB'`                                                                            |
| LAB orientation       | `pats.Tbl_OrientationChecklistNew` where `SiteCode = 'LAB'` (via `vwTaskList`)                                              |
| LAB assessment bundle | Long list of assessment/PA/BAM/diag tables where `SiteCode = 'LAB'` (see `updatedSchedulerProgrma.cs` lines 81–88) |


---

## Destination Tables by ETL Batch

Tables below are **Azure BHG_DR destination** names from `dms.vw_MapAction` (`DsnSchema.DsnTbl`), filtered to `Enabled = 1`, `IsActive = 1`, `ConnectionID <> 3`, classified using the routing rules in `updatedSchedulerProgrma.cs`.

### SAMMSGlobal (10 tables)

- `ctrl.tbl_ClaimStatus`
- `ctrl.tbl_CONSENTS`
- `ctrl.tbl_GlobalDevices`
- `ctrl.tbl_User`
- `ctrl.tbl_UserSites`
- `pats.tbl_BriefAddictionMonitor`
- `pats.tbl_clinicalopiatewithdrawalscale`
- `pats.tbl_FeeSched`
- `pats.tbl_FormsSAMMSClient`
- `pats.tbl_GlobalPayor`

### SAMMS-ETL-PPA (1 table) — updated scheduler

Runner: `BHGTaskRunner.exe 12` (`updatedProgram.cs`)

- `pats.tbl_SF_PatientPreAdmission` ← `dbo.SF_PatientPreAdmission` at each clinic site

*(Related mappings `ayx.tbl_PreAdmission_V6` and `pats.tbl_PreadmissionReferralSource` from the same source family remain on Regional P1 in the updated scheduler.)*

### Combined Regional P1 — all timezones (57 tables)

Union of Eastern + Central + Mountain + Pacific P1. Runner: `BHGTaskRunner.exe 2`.

**Removed from P1 vs legacy doc:** `pats.tbl_SF_PatientPreAdmission` (now `SAMMS-ETL-PPA`), `pats.tbl_NewAdmissionAssessmentASAMDimension2/4/5` (now P2 all timezones).

- `ayx.tbl_PreAdmission_V6`
- `ctrl.tbl_3PSETUP`
- `ctrl.tbl_Clinic`
- `ctrl.tbl_DroDownListItems`
- `pats.tbl_3pElig`
- `pats.tbl_Admissionassessmentsubstanceusehistory`
- `pats.tbl_AppointmentAttend`
- `pats.tbl_BAMForm`
- `pats.tbl_BAMScore`
- `pats.tbl_Bills`
- `pats.tbl_CheckIn`
- `pats.tbl_Codes`
- `pats.tbl_ComprehensiveAssessmentForm`
- `pats.tbl_consenttomarketing`
- `pats.tbl_Cows_V6`
- `pats.tbl_CustomAnswers`
- `pats.tbl_CustomQuestions`
- `pats.tbl_EandMFormPregnancy`
- `pats.tbl_Enrollment`
- `pats.tbl_FinancialHardshipApplication`
- `pats.tbl_Fmp`
- `pats.tbl_MNComprehensiveAssessment`
- `pats.tbl_MNComprehensiveAssessmentLevelOfCare`
- `pats.tbl_mntreatmentservicereview`
- `pats.tbl_NewAdmissionassessment`
- `pats.tbl_NewAdmissionAssessmentASAMDimension6`
- `pats.tbl_newdischargetransferplanform`
- `pats.tbl_NewPeriodicReassessment`
- `pats.tbl_NewPeriodicReassessmentCounselorReview`
- `pats.tbl_newperiodicreassessmentd2`
- `pats.tbl_newperiodicreassessmentd3`
- `pats.tbl_newperiodicreassessmentd4`
- `pats.tbl_newperiodicreassessmentd5`
- `pats.tbl_newperiodicreassessmentd6`
- `pats.tbl_PA`
- `pats.tbl_PACounselorReview`
- `pats.tbl_PADimension1`
- `pats.tbl_PADimension2`
- `pats.tbl_PADimension3`
- `pats.tbl_PADimension4`
- `pats.tbl_PADimension5`
- `pats.tbl_PADimension6`
- `pats.tbl_PayerCltHistory`
- `pats.tbl_pbi3PayAuth`
- `pats.tbl_PreadmissionReferralSource`
- `pats.tbl_SERVICES`
- `pats.tbl_SF_DataForms`
- `pats.tbl_SMSTextConsentForm`
- `pats.tbl_takehomeagreementanddiversioncontrol`
- `pats.tbl_TakeHomeRiskAssessment`
- `pats.tbl_TblDiag10`
- `pats.tbl_UAResults`
- `pats.tbl_UASched`
- `pats.tbl_VAComprehensiveAssessment`
- `pats.tbl_vacomprehensiveassessmentsummary`
- `pats.tbl_vw3pBillSub`
- `stg.ClientDemo`

### Combined Regional P2 — all timezones (17 tables)

Union of Eastern + Central + Mountain + Pacific P2 per `updatedSchedulerProgrma.cs`. Runner: `BHGTaskRunner.exe 4`.

**Added vs legacy doc:** `pats.tbl_NewAdmissionAssessmentASAMDimension2`, `pats.tbl_NewAdmissionAssessmentASAMDimension4`, `pats.tbl_NewAdmissionAssessmentASAMDimension5` (P2 in all timezones).

- `pats.tbl_Bills`
- `pats.tbl_CheckIn`
- `pats.tbl_ClaimLineItem`
- `pats.tbl_ClaimLineItemActivity`
- `pats.tbl_Claims`
- `pats.tbl_EandMFormMDM`
- `pats.tbl_EandMFormPregnancy`
- `pats.tbl_Enrollment`
- `pats.tbl_FeeSched`
- `pats.tbl_GlobalPayor`
- `pats.tbl_NewAdmissionAssessmentASAMDimension2`
- `pats.tbl_NewAdmissionAssessmentASAMDimension4`
- `pats.tbl_NewAdmissionAssessmentASAMDimension5`
- `pats.tbl_PayerClient`
- `pats.tbl_PayerCltHistory`
- `pats.tbl_TreatmentLevel`
- `pats.tbl_UAResultDetail`

**Also in P2 CASE lists but routed to dedicated ETL instead:**

- `pats.tbl_Orders` → `SAMMS-ETL-Orders` (arg `11`)
- `pats.tbl_Dose_Excuse` → `SAMMS-ETL-Dose` (arg `10`)

### Tables that appear in both combined P1 and combined P2

Five destination tables show up in **both** the combined P1 list (61) and combined P2 list (14). This is expected. The Scheduler assigns P1 vs P2 by **site timezone + table name**, not by table alone.


| BHG_DR table                  | P1 in             | P2 in                      |
| ----------------------------- | ----------------- | -------------------------- |
| `pats.tbl_Bills`              | Mountain, Pacific | Eastern, Central           |
| `pats.tbl_CheckIn`            | Mountain, Pacific | Eastern, Central           |
| `pats.tbl_EandMFormPregnancy` | Mountain, Pacific | Eastern, Central           |
| `pats.tbl_Enrollment`         | Eastern           | Central, Mountain, Pacific |
| `pats.tbl_PayerCltHistory`    | Central           | Eastern, Mountain, Pacific |


**Why this happens**

The Scheduler CASE in `updatedSchedulerProgrma.cs` uses different P1 exclude lists and P2 lists per timezone (EST, CST, MST, PST). The same Azure destination table can therefore route to:

- **P1** for sites in one timezone
- **P2** for sites in another timezone

Example — `pats.tbl_Bills`:

- EST/CST sites → `Eastern ETL P2` / `Central ETL P2`
- MST/PST sites → `Mountain ETL P1` / `Pacific ETL P1` (not in MST/PST P2 CASE lists)

Example — `pats.tbl_Enrollment`:

- EST sites → `Eastern ETL P1`
- CST/MST/PST sites → `Central ETL P2` / `Mountain ETL P2` / `Pacific ETL P2`

**No double load for the same site**

Each **site + table** combination is scheduled into **one batch only** per day. Overlap in the combined lists means the table name appears in P1 for some timezones and P2 for others — not that the same site runs both.

```
Site AH (EST) + pats.tbl_Bills     →  Eastern ETL P2 only
Site CBBO (MST) + pats.tbl_Bills   →  Mountain ETL P1 only
```

Both write to the same Azure table `pats.tbl_Bills`, from different clinic sites in different batches.

**Why P1 vs P2 exists**


| Phase  | Purpose                     | Typical data                                   |
| ------ | --------------------------- | ---------------------------------------------- |
| **P1** | Core clinical / operational | Assessments, forms, client data                |
| **P2** | Billing / financial         | Claims, payers, bills, check-in, fee schedules |


Eastern and Central split billing-heavy tables into P2 so P1 can finish first. Mountain and Pacific use slightly different exclude/P2 lists, so some tables that are P2 in the east run in P1 in mountain/pacific timezones.

**BHG_DR query — see P1/P2 overlap by timezone**

```sql
SELECT
    ma.DsnSchema + '.' + ma.DsnTbl AS bhg_dr_table,
    ma.TimeZone,
    CASE
        WHEN ma.TimeZone = 'EST' AND ma.DsnSchema + '.' + ma.DsnTbl IN (
            'pats.tbl_claims','pats.tbl_claimlineitem','pats.tbl_claimlineitemactivity',
            'pats.tbl_dose_excuse','pats.tbl_uaresultdetail','pats.tbl_GlobalPayor',
            'pats.tbl_PayerClient','pats.tbl_feesched','pats.tbl_CheckIn',
            'pats.tbl_EandMFormPregnancy','pats.tbl_EandMFormMDM','pats.tbl_Bills',
            'pats.tbl_payerclthistory','pats.tbl_treatmentlevel','pats.tbl_Orders'
        ) THEN 'P2'
        WHEN ma.TimeZone = 'EST' THEN 'P1'
        WHEN ma.TimeZone = 'CST' AND ma.DsnSchema + '.' + ma.DsnTbl IN (
            'pats.tbl_claims','pats.tbl_claimlineitem','pats.tbl_claimlineitemactivity',
            'pats.tbl_dose_excuse','pats.tbl_uaresultdetail','pats.tbl_Enrollment',
            'pats.tbl_GlobalPayor','pats.tbl_PayerClient','pats.tbl_feesched','pats.tbl_CheckIn',
            'pats.tbl_EandMFormPregnancy','pats.tbl_EandMFormMDM','pats.tbl_Bills',
            'pats.tbl_treatmentlevel','pats.tbl_Orders'
        ) THEN 'P2'
        WHEN ma.TimeZone = 'CST' THEN 'P1'
        WHEN ma.TimeZone IN ('MST','PST') AND ma.DsnSchema + '.' + ma.DsnTbl IN (
            'pats.tbl_claims','pats.tbl_claimlineitem','pats.tbl_claimlineitemactivity',
            'pats.tbl_treatmentlevel','pats.tbl_dose_excuse','pats.tbl_uaresultdetail',
            'pats.tbl_Enrollment','pats.tbl_GlobalPayor','pats.tbl_PayerClient',
            'pats.tbl_FeeSched','pats.tbl_EandMFormMDM','pats.tbl_payerclthistory'
        ) THEN 'P2'
        WHEN ma.TimeZone IN ('MST','PST') THEN 'P1'
    END AS phase
FROM dms.vw_MapAction ma
WHERE ma.Enabled = 1
  AND ma.IsActive = 1
  AND ma.ConnectionID <> 3
  AND ma.DsnSchema + '.' + ma.DsnTbl IN (
      'pats.tbl_Bills','pats.tbl_CheckIn','pats.tbl_EandMFormPregnancy',
      'pats.tbl_Enrollment','pats.tbl_PayerCltHistory'
  )
ORDER BY bhg_dr_table, ma.TimeZone, ma.SiteCode;
```

### Regional ETL P1 — per timezone detail


| Batch           | Timezone | Table count | BHGTaskRunner arg |
| --------------- | -------- | ----------- | ----------------- |
| Eastern ETL P1  | EST      | 54          | `2`               |
| Central ETL P1  | CST      | 54          | `2`               |
| Mountain ETL P1 | MST      | 56          | `2`               |
| Pacific ETL P1  | PST      | 56          | `2`               |


**57 unique destination tables** across all P1 batches (updated scheduler routing).

**Timezone differences:**

- **Eastern only:** `pats.tbl_Enrollment` is P1 (Central routes Enrollment to P2)
- **Central only:** `pats.tbl_PayerCltHistory` is P1 (Eastern routes it to P2)
- **Mountain / Pacific only:** `pats.tbl_Bills`, `pats.tbl_CheckIn`, `pats.tbl_EandMFormPregnancy` are P1 (Eastern/Central route those to P2)
- **All timezones (updated):** ASAM Dimension 2/4/5 are **P2**, not P1

**BHG_DR lookup query (all active P1 mappings):**

```sql
SELECT DISTINCT
    ma.TimeZone,
    ma.SiteCode,
    ma.DsnSchema + '.' + ma.DsnTbl AS bhg_dr_table,
    ma.SrcSchema + '.' + ma.FromTblVw AS source_table
FROM dms.vw_MapAction ma
WHERE ma.Enabled = 1
  AND ma.IsActive = 1
  AND ma.ConnectionID <> 3
  AND ma.SiteCode NOT IN ('PHC', 'Global', 'LAB')
  AND ma.TimeZone IN ('EST', 'CST', 'MST', 'PST')
  AND ma.DsnSchema + '.' + ma.DsnTbl NOT IN (
      -- tables routed to dedicated ETL batches instead of regional P1/P2
      'pats.tbl_dbo_FormAnswerSignatures', 'pats.tbl_dbo_FormQuestionAnswers',
      'pats.tbl_3parnote', 'pats.tbl_3pclaimnote',
      'pats.tbl_DartsSrv', 'pats.tbl_Dose', 'pats.tbl_Dose_Excuse', 'pats.tbl_Orders'
  )
ORDER BY ma.TimeZone, bhg_dr_table, ma.SiteCode;
```

### Per-timezone P1 / P2 detail (updated scheduler)

Use the **Combined Regional P1** and **Combined Regional P2** lists above as the source of truth for `updatedSchedulerProgrma.cs`.

Per-timezone counts after the ASAM Dimension 2/4/5 move to P2:

| Batch | P1 tables | P2 tables |
|-------|-----------|-----------|
| Eastern ETL | 54 | 14 |
| Central ETL | 54 | 14 |
| Mountain ETL | 56 | 14 |
| Pacific ETL | 56 | 14 |

P2 in every timezone includes: claims/billing set **plus** `pats.tbl_NewAdmissionAssessmentASAMDimension2/4/5`.

### Samms-LAB (2 tables)

- `pats.tbl_ClientDemo1`
- `pats.tbl_ClientDemo2`

*(LAB site only; routed by `SiteCode = 'LAB'` in CASE logic.)*

### Samms-Forms (2 tables)

- `pats.tbl_dbo_FormAnswerSignatures`
- `pats.tbl_dbo_FormQuestionAnswers`

### SAMMS-ETL-Notes (2 tables)

- `pats.tbl_3pArnote`
- `pats.tbl_3pClaimNote`

### SAMMS-ETL-INV (24 tables)

- `ctrl.tbl_InvType`
- `pats.Tbl_AdmissionAssessment`, `pats.Tbl_AdmissionAssessmentSummary`, `pats.Tbl_ReAssessment`
- All admission/reassessment dimension and sub-assessment tables in the INV CASE list
- `pats.Tbl_Appointments`, `pats.tbl_Bottle`, `pats.Tbl_LabResult`, `pats.tbl_LabResultDetail`
- `pats.tbl_LiquidLog`, `pats.Tbl_OrientationChecklistNew`

### SAMMS-ETL-DartSvc (1 table)

- `pats.tbl_DartsSrv` → staged via `stg.tbl_dartssrv`, merged to `pats.tbl_DartsSrv_20XX` year tables

### SAMMS-ETL-Dose (2 tables)

- `pats.tbl_Dose`
- `pats.tbl_Dose_Excuse`

### SAMMS-ETL-Orders (1 table)

- `pats.tbl_Orders` → year-partitioned destination tables in Azure

---

## Related Runtime Components


| Component           | Role                                                                                     |
| ------------------- | ---------------------------------------------------------------------------------------- |
| `BHGTaskRunner.exe` | Executes pending tasks; reads `tsk.vwTaskList`, pulls from SAMMS via `SelectConstructor` |
| `BHG-DR-LIB`        | Shared save/bulk classes (`SaveData`, `BulkDartsSvc`, etc.)                              |
| `ETLMgr`            | Desktop ops UI over `tsk.tbl_Tasks2`                                                     |
| `PHC` project       | Separate runner for `PHC ETL` parent tasks                                               |


---

## Useful Queries

**Today's pending parent tasks:**

```sql
SELECT TaskId, TaskName, RunAt, SiteCode, Status, RowState
FROM tsk.tbl_Tasks2
WHERE ParentTaskId IS NULL
  AND WorkDate = CONVERT(date, GETDATE())
  AND Status = 17
  AND RowState = 24;
```

**Child tasks for a batch:**

```sql
SELECT c.TaskName, c.SiteCode, c.ActionKey, c.ActionStepKey, c.Status
FROM tsk.tbl_Tasks2 p
JOIN tsk.tbl_Tasks2 c ON c.ParentTaskId = p.TaskId
WHERE p.TaskName = 'SAMMS-ETL-Dose'
  AND p.WorkDate = CONVERT(date, GETDATE())
  AND c.RowState = 24;
```

**Active mapping rows:**

```sql
SELECT TimeZone, SiteCode, ActionKey, StepKey, DsnSchema, DsnTbl, FromTblVw
FROM dms.vw_MapAction
WHERE Enabled = 1 AND IsActive = 1 AND ConnectionID <> 3
ORDER BY SiteCode, DsnTbl;
```

---

## Source Files


| File                                             | Purpose                                              |
| ------------------------------------------------ | ---------------------------------------------------- |
| `BCAppCode/Scheduler/updatedSchedulerProgrma.cs` | **Current** scheduler routing (documented here)      |
| `BCAppCode/Scheduler/Program.cs`                 | Legacy scheduler (PHC parent SiteCode, no PPA batch) |
| `BCAppCode/BHGTaskRunner/updatedProgram.cs`      | Runner args `1`–`12` including `SAMMS-ETL-PPA`       |
| `BCAppCode/BHGTaskRunner/Program.cs`             | Legacy runner args `1`–`11` only                     |
| `BCAppCode/Scheduler/Regional_P1_P2_Source_to_Destination.md` | P1/P2 source → destination mapping       |
| `BCAppCode/Framework/vw_mapAction.csv`           | Export of `dms.vw_MapAction`                         |
| `BCAppCode/Framework/vw_MapSrc2Dsn.csv`          | Column mapping export                                |
| `BCAppCode_EXPLAINED.md`                         | Broader architecture overview                        |


