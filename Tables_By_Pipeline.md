# All ETL Tables Mapped to Their Pipelines

> **Source:** Derived from `Scheduler/Program.cs` CASE statement (routing logic) and `BHGTaskRunner/Program.cs` switch cases (all handled tables).  
> **Total destination tables handled by BHGTaskRunner:** 98 unique tables/views  
> **How routing works:** The Scheduler's CASE statement decides which pipeline (parent task) each table belongs to. It evaluates top-to-bottom — the first matching condition wins.
> **New in this version:** Each table now shows the **Source File** (BHG-DR-LIB .cs file) and **Method(s)** that write to it.

---

## Quick Summary

| Pipeline | Arg | Tables Count | What Type of Data |
|----------|-----|-------------|------------------|
| SAMMS-ETL-Inv | 8 | 24 | Assessments, inventory, lab results |
| ETL P1 (timezone batches) | 2 | ~38 | Patient/clinical non-financial |
| ETL P2 (timezone batches) | 4 | 9–14 (varies by timezone) | Financial / billing |
| SAMMS-ETL-Dose | 10 | 2 | Dosing records |
| Samms-Forms | 6 | 2 | Form Q&A and signatures |
| SAMMS-ETL-Notes | 7 | 2 | 3rd party billing notes |
| SAMMS-ETL-Orders | 11 | 1 | Orders (year-partitioned) |
| SAMMS-ETL-DartSvc | 9 | 1 | DART services |
| Samms-LAB | 5 | 2 | LAB site demographics only |
| SAMMSGlobal | 1 | remaining | Tables with no timezone in mapping |
| PHC ETL | separate | PHC site | PHC partner site (separate runner) |

---

## Pipeline 1 — SAMMSGlobal (arg `1`)

Tables that fall here are **not** matched by any explicit table or timezone condition — they land in the `ELSE 'SAMMSGlobal'` branch of the CASE. This typically means the site's `TimeZone` field in `dms.vw_MapAction` is empty or unrecognized (not EST/CST/MST/PST).

> In practice, most BHG sites have a timezone set, so tables listed here as "also P1" will go to the appropriate timezone P1 batch for sites WITH a timezone.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_clinic` | `ctrl` | Clinic master / configuration data | `SaveClinic.cs` | `SaveClinic` |
| 2 | `tbl_consents` | `ctrl` | Consent types | `SaveGlobal.cs` | `SaveGlobalConsents` |
| 3 | `tbl_globaldevices` | `ctrl` | Devices / hardware registered | `SaveGlobal.cs` | `SaveGlobalDevices` |
| 4 | `tbl_user` | `ctrl` | Staff / user accounts | `SaveGlobal.cs` | `SaveGlobalUser` |
| 5 | `tbl_usersites` | `ctrl` | User-to-site assignments | `SaveGlobal.cs` | `SaveGlobalUserSite` |
| 6 | `tbl_drodownlistitems` | `ctrl` | Dropdown reference values | `SavePAData.cs` | `SavedropDownListItems` |
| 7 | `tbl_3psetup` | `ctrl` | 3rd party billing setup config | `Save3pElig.cs` | `Save3pSetup` |
| 8 | `tbl_claimstatus` | `ctrl` | Claim status codes | `SaveGlobal.cs` | `SaveClaimStatus` |
| 9 | `tbl_preadmission_v6` | `ayx` | Pre-admission (SalesForce V6 schema) | `SavePreAdmissionV6.cs` | `SavePreAdmissionV6` |
| 10 | `clientdemo` | `stg` | Full patient demographics (bulk path) | `BulkDartsSvc.cs` | `BulkDartsSrvLoader` → `stg.ClientDemoMerge1/2` |
| 11 | `tbl_codes` | `pats` | Reference/lookup codes | `SaveCodes.cs` | `SaveCodes` |
| 12 | `tbl_customquestions` | `pats` | Site-defined custom clinical questions | `SaveCustomQA.cs` | `SaveCustomQuestions` |
| 13 | `tbl_customanswers` | `pats` | Answers to custom questions | `SaveCustomQA.cs` | `SaveCustomAnswers` |
| 14 | `tbl_briefaddictionmonitor` | `pats` | BAM — Brief Addiction Monitor (old format) | `SaveGlobal.cs` | `SaveBAM` |
| 15 | `tbl_bamform` | `pats` | BAM form (new SalesForce format) | `SaveBAM.cs` | `SaveBamForm` |
| 16 | `tbl_bamscore` | `pats` | BAM calculated scores | `SaveBAM.cs` | `SaveBamScore` |
| 17 | `tbl_clinicalopiatewithdrawalscale` | `pats` | COWS — old format assessment | `SaveGlobal.cs` | `SaveGlobalClinicalOpiateWithdrawalScale` |
| 18 | `tbl_cows_v6` | `pats` | COWS — V6 SalesForce format | `SaveCows.cs` | `SaveCows_v6` |
| 19 | `tbl_tbldiag10` | `pats` | ICD-10 diagnosis codes | `SaveBAM.cs` | `SaveTblDiags` |
| 20 | `tbl_fmp` | `pats` | Financial / miscellaneous payments | `SaveFmp.cs` | `SaveFmp` |
| 21 | `tbl_uasched` | `pats` | UA screening schedule | `SaveUAResults.cs` | `SaveUASched` |
| 22 | `tbl_uaresults` | `pats` | Urinalysis results | `SaveUAResults.cs` | `SaveUAResults` |
| 23 | `tbl_labresult` | `pats` | Lab results | `SaveUAResults.cs` | `SaveLABResults` |
| 24 | `tbl_services` | `pats` | Clinical services | `SaveGlobal.cs` | `SaveServices` |
| 25 | `tbl_pbi3payauth` | `pats` | Payer authorizations | `SaveAuths.cs` | `SaveAuths` |
| 26 | `tbl_3pelig` | `pats` | 3rd party eligibility | `Save3pElig.cs` | `Save3pElig` |
| 27 | `tbl_formssammsclient` | `pats` | SAMMS forms metadata | `SaveGlobal.cs` / `BulkDartsSvc.cs` | `SaveGlobalFormsSAMMSClients` / `BulkDartsSrvLoader` |
| 28 | `tbl_vw3pbill` | `pats` | 3rd party billing view | `SaveBills.cs` | `SaveAuthBills` |
| 29 | `tbl_preadmissionreferralsource` | `pats` | Pre-admission referral source | `SavePreAdmissionV6.cs` | `SavePreAdminReferrals` |
| 30 | `tbl_admissionassessmentsubstanceusehistory` | `pats` | Admission — substance use history | `SaveAssessments.cs` | `SaveAdmissionAssessmentSubstanceuseHistory` |
| 31 | `tbl_assessmentsubstanceusehistory` | `pats` | Ongoing — substance use history | `SaveAssessments.cs` | `SaveAssessmentSubstanceuseHistory` |
| 32 | `tbl_comprehensiveassessmentform` | `pats` | Comprehensive Assessment Form | `SaveFormQAData.cs` | `SaveComprehensiveAssessmentForm` |
| 33 | `tbl_pa` | `pats` | Pre-Admission main record | `SavePAData.cs` | `SavePA` |
| 34 | `tbl_pacounselorreview` | `pats` | Pre-Admission counselor review | `SavePAData.cs` | `SavePACounselorReview` |
| 35 | `tbl_padimension1` | `pats` | Pre-Admission ASAM Dimension 1 | `SavePAData.cs` | `SavePADimension1` |
| 36 | `tbl_padimension2` | `pats` | Pre-Admission ASAM Dimension 2 | `SavePAData.cs` | `SavePADimension2` |
| 37 | `tbl_padimension3` | `pats` | Pre-Admission ASAM Dimension 3 | `SavePAData.cs` | `SavePADimension3` |
| 38 | `tbl_padimension4` | `pats` | Pre-Admission ASAM Dimension 4 | `SavePAData.cs` | `SavePADimension4` |
| 39 | `tbl_padimension5` | `pats` | Pre-Admission ASAM Dimension 5 | `SavePAData.cs` | `SavePADimension5` |
| 40 | `tbl_padimension6` | `pats` | Pre-Admission ASAM Dimension 6 | `SavePAData.cs` | `SavePADimension6` |
| 41 | `tbl_financialhardshipapplication` | `pats` | Financial Hardship Application | `SavePAData.cs` | `SaveFinancialHardshipApplication` |
| 42 | `tbl_mncomprehensiveassessment` | `pats` | MN state Comprehensive Assessment | `SaveCA.cs` | `SaveMNCA` |
| 43 | `tbl_mncomprehensiveassessmentlevelofcare` | `pats` | MN — Level of Care determination | `SaveCA.cs` | `SaveMNCALOC` |
| 44 | `tbl_vacomprehensiveassessment` | `pats` | VA Comprehensive Assessment | `SaveCA.cs` | `SaveVACA` |
| 45 | `tbl_vacomprehensiveassessmentsummary` | `pats` | VA Comprehensive Assessment Summary | `SaveCA.cs` | `SaveVACASummary` |
| 46 | `tbl_newadmissionassessment` | `pats` | New-format Admission Assessment | `SaveCA.cs` | `SaveNewAdmissionAssessment` |
| 47 | `tbl_newadmissionassessmentasamdimension6` | `pats` | New Admission — ASAM Dimension 6 | `SaveCA.cs` | `SaveNewAdmissionAssessmentASAMDimension6` |
| 48 | `tbl_newperiodicreassessment` | `pats` | New-format Periodic Re-Assessment | `SaveCA.cs` | `SaveNewPeriodicReassessment` |
| 49 | `tbl_newperiodicreassessmentcounselorreview` | `pats` | New Periodic Re-Assessment — Counselor Review | `SaveCA.cs` | `Savenewperiodicreassessmentcounselorreview` |
| 50 | `tbl_appointmentattend` | `pats` | Appointment attendance records | `SaveAppointments.cs` | `SaveAppointmentAttend` |
| 51 | `tbl_clientdemo1` | `pats` | Patient demographics part 1 (non-LAB sites) | `SaveCleints.cs` | `SaveClientDemo1var`, `SaveClientDemo1` |
| 52 | `tbl_clientdemo2` | `pats` | Patient demographics part 2 (non-LAB sites) | `SaveCleints.cs` | `SaveClientDemo2`, `SaveClientDemo3` |

> **Note:** For sites **with** a timezone (EST/CST/MST/PST) configured in `dms.vw_MapAction`, these tables are assigned to the **appropriate timezone's ETL P1 batch** (Eastern P1, Central P1, etc.) instead of SAMMSGlobal.

---

## Pipeline 2 — Eastern / Central / Mountain / Pacific ETL P1 (arg `2`)

Tables that go to **P1** for sites with a timezone. The same table name appears in up to 4 different P1 batches simultaneously — one per timezone:
- Eastern ETL P1 (EST sites)
- Central ETL P1 (CST sites)
- Mountain ETL P1 (MST sites)
- Pacific ETL P1 (PST sites)

**These are ALL the SAMMSGlobal tables listed above, assigned to P1 when the site has a timezone.** Plus the following table that has a timezone-specific P1 vs P2 variation:

| Table | EST | CST | MST | PST |
|-------|-----|-----|-----|-----|
| `pats.tbl_enrollment` | **P1** | P2 | P2 | P2 |
| `pats.tbl_checkin` | P2 | P2 | **P1** | **P1** |
| `pats.tbl_eandmformpregnancy` | P2 | P2 | **P1** | **P1** |
| `pats.tbl_bills` | P2 | P2 | **P1** | **P1** |
| `pats.tbl_payerclthistory` | P2 | **P1** | P2 | P2 |

---

## Pipeline 3 — Eastern / Central / Mountain / Pacific ETL P2 (arg `4`)

Financial and billing tables. These are **excluded from P1** and go to the **P2 batch** for their timezone. The P2 list varies slightly by timezone.

### Tables in P2 for ALL 4 timezones (EST, CST, MST, PST):

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_claims` | `pats` | Insurance claims | `SaveClaims.cs` / `BulkDartsSvc.cs` | `SaveClaims` / `BulkDartsSrvLoader` |
| 2 | `tbl_claimlineitem` | `pats` | Claim line items | `SaveClaims.cs` / `BulkDartsSvc.cs` | `SaveClaimLineItem` / `BulkDartsSrvLoader` |
| 3 | `tbl_claimlineitemactivity` | `pats` | Claim activity history | `SaveClaims.cs` / `BulkDartsSvc.cs` | `SaveClaimLineItemActivity` / `BulkDartsSrvLoader` |
| 4 | `tbl_uaresultdetail` | `pats` | Urinalysis result details | `SaveUAResults.cs` / `BulkDartsSvc.cs` | `SaveUAResultDetail` / `BulkDartsSrvLoader` |
| 5 | `tbl_globalpayor` | `pats` | Payer / insurance master | `SaveGlobal.cs` | `SaveGlobalPayer` |
| 6 | `tbl_payerclient` | `pats` | Patient-to-payer assignment | `SavePayorClient.cs` | `SavePayerClient`, `RemovePayerClients` |
| 7 | `tbl_feesched` | `pats` | Fee schedules by payer | `SaveGlobal.cs` | `SaveFeeSchedules` |
| 8 | `tbl_eandmformmdm` | `pats` | E&M Medical Decision Making form | `SaveFormQAData.cs` | `SaveEMFormMDM` |
| 9 | `tbl_treatmentlevel` | `pats` | Patient treatment level | `SaveTreatmentLevel.cs` | `SaveTreatmentLevel` |

### Tables with timezone-specific P1/P2 assignment:

| Table | Schema | EST | CST | MST | PST | What It Is | Source File | Method(s) |
|-------|--------|-----|-----|-----|-----|-----------|-------------|-----------|
| `tbl_checkin` | `pats` | **P2** | **P2** | P1 | P1 | Patient check-in records | `SaveCheckIn.cs` | `SaveCheckIn` |
| `tbl_eandmformpregnancy` | `pats` | **P2** | **P2** | P1 | P1 | E&M Pregnancy form | `SaveFormQAData.cs` | `SaveEMFormPregnancy` |
| `tbl_bills` | `pats` | **P2** | **P2** | P1 | P1 | Billing records | `SaveBills.cs` | `SaveBills` |
| `tbl_payerclthistory` | `pats` | **P2** | P1 | **P2** | **P2** | History of payer changes | `SavePayorClient.cs` | `SavePayerCltHistory` |
| `tbl_enrollment` | `pats` | P1 | **P2** | **P2** | **P2** | Patient program enrollment | `SaveEnrollment.cs` | `SaveEnrollment` |

> **Why these differ by timezone?** It's hardcoded business logic in the Scheduler CASE statement. The P1 and P2 "exclusion lists" were defined per timezone and are slightly inconsistent — likely reflecting differences in how clinics in different regions manage their data or which tables were added at different times.

---

## Pipeline 4 — SAMMS-ETL-Inv (arg `8`)

Inventory, assessments, and lab results. These are **explicitly named in the CASE** — regardless of site timezone, they always go to SAMMS-ETL-Inv.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_bottle` | `pats` | Medication bottles | `SaveInventory.cs` | `SaveBottles` |
| 2 | `tbl_liquidlog` | `pats` | Liquid medication log | `SaveInventory.cs` | `SaveLiquidlog` |
| 3 | `tbl_invtype` | `ctrl` | Inventory type definitions | `SaveInventory.cs` | `SaveInvTypes` |
| 4 | `tbl_orientationchecklistnew` | `pats` | New patient orientation checklist | `SaveInventory.cs` | `SaveOrientationCheckList` |
| 5 | `tbl_labresult` | `pats` | Lab test results | `SaveUAResults.cs` | `SaveLABResults` |
| 6 | `tbl_labresultdetail` | `pats` | Lab result detail rows | `BulkDartsSvc.cs` | `BulkDartsSrvLoader` → `stg.LABResultDetailMerge` |
| 7 | `tbl_appointments` | `pats` | Patient appointment scheduling | `SaveAppointments.cs` | `SaveAppointments` |
| 8 | `tbl_admissionassessment` | `pats` | Admission assessment (ASAM) | `SaveAssessments.cs` | `SaveAdmissionAssessment` |
| 9 | `tbl_admissionassessmentsummary` | `pats` | Admission assessment summary | `SaveAssessments.cs` | `SaveAdmissionAssessmentSummary` |
| 10 | `tbl_reassessment` | `pats` | Periodic re-assessment (main) | `SaveAssessments.cs` | `SaveReAssessment` |
| 11 | `tbl_admissionassessmentdimensiononedisorder` | `pats` | ASAM Dimension 1 — Substance Use | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionOneDisorder` |
| 12 | `tbl_admissionassessmentdimensionfivesubstanceuse` | `pats` | ASAM Dimension 5 — Relapse Potential | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionFiveSubstanceUse` |
| 13 | `tbl_admissionassessmentdimensiontwo` | `pats` | ASAM Dimension 2 — Biomedical | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionTwo` |
| 14 | `tbl_reassessmentoccupational` | `pats` | Re-assessment — Employment/Education | `SaveAssessments.cs` | `SaveReAssessmentOccupational` |
| 15 | `tbl_reassessmentfamily` | `pats` | Re-assessment — Family/Social | `SaveAssessments.cs` | `SaveReAssessmentFamily` |
| 16 | `tbl_reassessmentlegal` | `pats` | Re-assessment — Legal Status | `SaveAssessments.cs` | `SaveReAssessmentLegal` |
| 17 | `tbl_reassessmentmentalhealth` | `pats` | Re-assessment — Mental Health | `SaveAssessments.cs` | `SaveReAssessmentMentalHealth` |
| 18 | `tbl_reassessmentphysicalhealth` | `pats` | Re-assessment — Physical Health | `SaveAssessments.cs` | `SaveReAssessmentPhysicalHealth` |
| 19 | `tbl_reassessmentsubstanceuse` | `pats` | Re-assessment — Substance Use | `SaveAssessments.cs` | `SaveReAssessmentSubstanceUse` |
| 20 | `tbl_reassessmentsocial` | `pats` | Re-assessment — Social Environment | `SaveAssessments.cs` | `SaveReAssessmentSocial` |
| 21 | `tbl_reassessmenttreatment` | `pats` | Re-assessment — Treatment Plan | `SaveAssessments.cs` | `SaveReAssessmentTreatment` |
| 22 | `tbl_admissionassessmentdimensionthree` | `pats` | ASAM Dimension 3 — Emotional/Behavioral | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionThree` |
| 23 | `tbl_admissionassessmentdimensionsix` | `pats` | ASAM Dimension 6 — Living Environment | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionSix` |
| 24 | `tbl_admissionassessmentdimensionfour` | `pats` | ASAM Dimension 4 — Readiness for Change | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionfour` |

**Total: 24 tables**

---

## Pipeline 5 — SAMMS-ETL-Dose (arg `10`)

Always goes to this batch regardless of site timezone.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_dose` | `pats` | Medication dosing records (methadone/buprenorphine) | `SaveDoses.cs` / `BulkDartsSvc.cs` | `SaveDoses` (EF, 4 sites) / `BulkDartsSrvLoader` (bulk, most sites) |
| 2 | `tbl_dose_excuse` | `pats` | Missed dose excuse records | `SaveDoses.cs` | `SaveDoseExcuse` |

**Total: 2 tables**

---

## Pipeline 6 — Samms-Forms (arg `6`)

Always goes to this batch regardless of site timezone.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_dbo_formquestionanswers` | `pats` | All clinical form question & answer data | `SaveFormQAData.cs` / `BulkDartsSvc.cs` | `SaveFormQuestionAnswers` (EF) / `BulkDartsSrvLoader` (bulk) |
| 2 | `tbl_dbo_formanswersignatures` | `pats` | Form signature records | `SaveFormQAData.cs` | `SaveAnswerSignatures` |

**Total: 2 tables**

---

## Pipeline 7 — SAMMS-ETL-Notes (arg `7`)

Always goes to this batch regardless of site timezone.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_3parnote` | `pats` | 3rd party AR (Accounts Receivable) notes | `Save3pElig.cs` | `Save3pArnote` |
| 2 | `tbl_3pclaimnote` | `pats` | 3rd party claim notes | `Save3pElig.cs` | `Save3pClaimNote` |

**Total: 2 tables**

---

## Pipeline 8 — SAMMS-ETL-Orders (arg `11`)

Always goes to this batch regardless of site timezone.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_orders` | `pats` | Medication/service orders — split by year into `tbl_Orders2016` through `tbl_Orders2028` at save time | `SaveOrders.cs` | `SaveOrders2016`, `SaveOrders2017`, … `SaveOrders2028` (one method per year) |

**Total: 1 table (but saves into 13 year-partitioned destination tables)**

---

## Pipeline 9 — SAMMS-ETL-DartSvc (arg `9`)

Always goes to this batch regardless of site timezone.

| # | Table | Schema | What It Is | Source File | Method(s) |
|---|-------|--------|-----------|-------------|-----------|
| 1 | `tbl_dartssrv` | `pats` | DART services billed (Drug Abuse Reporting Tool) | `BulkDartsSvc.cs` (primary daily) / `SaveDartsSrvs.cs` (backfill/EF) | `BulkDartsSrvLoader` → `stg.DartsSrvMerge` procs (bulk) / `SaveDartSrv2014`–`SaveDartSrv2023` (EF per year) |

**Total: 1 table**

---

## Pipeline 10 — Samms-LAB (arg `5`)

Only applies to the **LAB site** for demographic tables. For all other sites, `tbl_clientdemo1` and `tbl_clientdemo2` go to the timezone P1 batch instead.

| # | Table | Schema | Site | What It Is | Source File | Method(s) |
|---|-------|--------|------|-----------|-------------|-----------|
| 1 | `tbl_clientdemo1` | `pats` | LAB only | Patient demographics part 1 | `SaveCleints.cs` | `SaveClientDemo1var`, `SaveClientDemo1` |
| 2 | `tbl_clientdemo2` | `pats` | LAB only | Patient demographics part 2 | `SaveCleints.cs` | `SaveClientDemo2`, `SaveClientDemo3` |

**Total: 2 tables (LAB site only)**

---

## Pipeline 11 — PHC ETL (separate PHC runner)

PHC (a special partner site) has its own schedule entry (`scheduleId = 18`) and its own ETL runner (the `PHC/` folder code). The `BHGTaskRunner.exe` **excludes PHC** (`SiteCode != "PHC"`). PHC runs the same destination tables as other sites, but:

- Uses a hardcoded `PHCSQLVM` connection for some tables
- Several tables are **skipped entirely** for PHC (via Scheduler skip rules):

| Skipped for PHC | Reason |
|----------------|--------|
| `pats.tbl_BriefAddictionMonitor` | Pulled via hardcoded PHCSQLVM connection in PHC runner instead |
| `pats.tbl_clinicalopiatewithdrawalscale` | Same — PHC uses direct PHCSQLVM |
| `pats.tbl_vw3pBillSub` | Not supported for PHC |
| `pats.tbl_Cows_V6` (ActionKey=1, Step=23) | PHC doesn't have SF_COWS table |
| `ayx.tbl_PreAdmission_V6` | PHC uses older pre-admission schema |
| `pats.tbl_EandMFormMDM` | Not used at PHC |
| `pats.tbl_EandMFormPregnancy` | Not used at PHC |

---

## Complete Table-to-Pipeline Reference (all 98)

| Table (lowercase) | Schema | Pipeline | Arg | Write Method | Source File | Method(s) |
|-------------------|--------|----------|-----|-------------|-------------|-----------|
| `tbl_3parnote` | pats | SAMMS-ETL-Notes | 7 | EF upsert | `Save3pElig.cs` | `Save3pArnote` |
| `tbl_3pclaimnote` | pats | SAMMS-ETL-Notes | 7 | EF upsert | `Save3pElig.cs` | `Save3pClaimNote` |
| `tbl_3pelig` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `Save3pElig.cs` | `Save3pElig` |
| `tbl_3psetup` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `Save3pElig.cs` | `Save3pSetup` |
| `tbl_admissionassessment` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessment` |
| `tbl_admissionassessmentdimensionfivesubstanceuse` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionFiveSubstanceUse` |
| `tbl_admissionassessmentdimensionfour` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionfour` |
| `tbl_admissionassessmentdimensiononedisorder` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionOneDisorder` |
| `tbl_admissionassessmentdimensionsix` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionSix` |
| `tbl_admissionassessmentdimensionthree` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionThree` |
| `tbl_admissionassessmentdimensiontwo` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentDimensionTwo` |
| `tbl_admissionassessmentsummary` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentSummary` |
| `tbl_admissionassessmentsubstanceusehistory` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveAssessments.cs` | `SaveAdmissionAssessmentSubstanceuseHistory` |
| `tbl_appointments` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAppointments.cs` | `SaveAppointments` |
| `tbl_appointmentattend` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveAppointments.cs` | `SaveAppointmentAttend` |
| `tbl_assessmentsubstanceusehistory` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveAssessments.cs` | `SaveAssessmentSubstanceuseHistory` |
| `tbl_bamform` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveBAM.cs` | `SaveBamForm` |
| `tbl_bamscore` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveBAM.cs` | `SaveBamScore` |
| `tbl_bills` | pats | P2 (EST/CST) · P1 (MST/PST) | 4/2 | EF upsert | `SaveBills.cs` | `SaveBills` |
| `tbl_bottle` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveInventory.cs` | `SaveBottles` |
| `tbl_briefaddictionmonitor` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveBAM` |
| `tbl_checkin` | pats | P2 (EST/CST) · P1 (MST/PST) | 4/2 | EF upsert | `SaveCheckIn.cs` | `SaveCheckIn` |
| `tbl_claimlineitem` | pats | ETL P2 (all timezones) | 4 | BulkCopy | `SaveClaims.cs` / `BulkDartsSvc.cs` | `SaveClaimLineItem` / `BulkDartsSrvLoader` |
| `tbl_claimlineitemactivity` | pats | ETL P2 (all timezones) | 4 | BulkCopy | `SaveClaims.cs` / `BulkDartsSvc.cs` | `SaveClaimLineItemActivity` / `BulkDartsSrvLoader` |
| `tbl_claims` | pats | ETL P2 (all timezones) | 4 | BulkCopy or EF (site-specific) | `SaveClaims.cs` / `BulkDartsSvc.cs` | `SaveClaims` / `BulkDartsSrvLoader` |
| `tbl_claimstatus` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveClaimStatus` |
| `tbl_clientdemo1` | pats | Samms-LAB (LAB site) · ETL P1 (others) | 5/2 | EF upsert | `SaveCleints.cs` | `SaveClientDemo1var`, `SaveClientDemo1` |
| `tbl_clientdemo2` | pats | Samms-LAB (LAB site) · ETL P1 (others) | 5/2 | EF upsert | `SaveCleints.cs` | `SaveClientDemo2`, `SaveClientDemo3` |
| `tbl_clinicalopiatewithdrawalscale` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveGlobalClinicalOpiateWithdrawalScale` |
| `tbl_clinic` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveClinic.cs` | `SaveClinic` |
| `tbl_codes` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCodes.cs` | `SaveCodes` |
| `tbl_comprehensiveassessmentform` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveFormQAData.cs` | `SaveComprehensiveAssessmentForm` |
| `tbl_consents` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveGlobalConsents` |
| `tbl_cows_v6` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCows.cs` | `SaveCows_v6` |
| `tbl_customanswers` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCustomQA.cs` | `SaveCustomAnswers` |
| `tbl_customquestions` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCustomQA.cs` | `SaveCustomQuestions` |
| `tbl_dartssrv` | pats | SAMMS-ETL-DartSvc | 9 | BulkCopy | `BulkDartsSvc.cs` / `SaveDartsSrvs.cs` | `BulkDartsSrvLoader` (daily bulk) / `SaveDartSrv2014`–`SaveDartSrv2023` (EF backfill) |
| `tbl_dbo_formanswersignatures` | pats | Samms-Forms | 6 | EF upsert | `SaveFormQAData.cs` | `SaveAnswerSignatures` |
| `tbl_dbo_formquestionanswers` | pats | Samms-Forms | 6 | EF upsert or BulkCopy | `SaveFormQAData.cs` / `BulkDartsSvc.cs` | `SaveFormQuestionAnswers` / `BulkDartsSrvLoader` |
| `tbl_dose` | pats | SAMMS-ETL-Dose | 10 | BulkCopy (EF for 4 specific sites) | `SaveDoses.cs` / `BulkDartsSvc.cs` | `SaveDoses` / `BulkDartsSrvLoader` |
| `tbl_dose_excuse` | pats | SAMMS-ETL-Dose | 10 | EF upsert | `SaveDoses.cs` | `SaveDoseExcuse` |
| `tbl_drodownlistitems` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavedropDownListItems` |
| `tbl_eandmformmdm` | pats | ETL P2 (all timezones) | 4 | EF upsert | `SaveFormQAData.cs` | `SaveEMFormMDM` |
| `tbl_eandmformpregnancy` | pats | P2 (EST/CST) · P1 (MST/PST) | 4/2 | EF upsert | `SaveFormQAData.cs` | `SaveEMFormPregnancy` |
| `tbl_enrollment` | pats | P1 (EST) · P2 (CST/MST/PST) | 2/4 | EF upsert | `SaveEnrollment.cs` | `SaveEnrollment` |
| `tbl_feesched` | pats | ETL P2 (all timezones) | 4 | EF upsert | `SaveGlobal.cs` | `SaveFeeSchedules` |
| `tbl_financialhardshipapplication` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SaveFinancialHardshipApplication` |
| `tbl_fmp` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveFmp.cs` | `SaveFmp` |
| `tbl_formssammsclient` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | BulkCopy | `SaveGlobal.cs` / `BulkDartsSvc.cs` | `SaveGlobalFormsSAMMSClients` / `BulkDartsSrvLoader` |
| `tbl_globaldevices` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveGlobalDevices` |
| `tbl_globalpayor` | pats | ETL P2 (all timezones) | 4 | EF upsert | `SaveGlobal.cs` | `SaveGlobalPayer` |
| `tbl_invtype` | ctrl | SAMMS-ETL-Inv | 8 | EF upsert | `SaveInventory.cs` | `SaveInvTypes` |
| `tbl_labresult` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveUAResults.cs` | `SaveLABResults` |
| `tbl_labresultdetail` | pats | SAMMS-ETL-Inv | 8 | BulkCopy | `BulkDartsSvc.cs` | `BulkDartsSrvLoader` → `stg.LABResultDetailMerge` |
| `tbl_liquidlog` | pats | SAMMS-ETL-Inv | 8 | EF upsert or BulkCopy | `SaveInventory.cs` | `SaveLiquidlog` |
| `tbl_mncomprehensiveassessment` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveMNCA` |
| `tbl_mncomprehensiveassessmentlevelofcare` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveMNCALOC` |
| `tbl_newadmissionassessment` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveNewAdmissionAssessment` |
| `tbl_newadmissionassessmentasamdimension6` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveNewAdmissionAssessmentASAMDimension6` |
| `tbl_newperiodicreassessment` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveNewPeriodicReassessment` |
| `tbl_newperiodicreassessmentcounselorreview` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `Savenewperiodicreassessmentcounselorreview` |
| `tbl_orders` | pats | SAMMS-ETL-Orders | 11 | EF upsert (per year) | `SaveOrders.cs` | `SaveOrders2016`–`SaveOrders2028` (one per year) |
| `tbl_orientationchecklistnew` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveInventory.cs` | `SaveOrientationCheckList` |
| `tbl_pa` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePA` |
| `tbl_pacounselorreview` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePACounselorReview` |
| `tbl_padimension1` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePADimension1` |
| `tbl_padimension2` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePADimension2` |
| `tbl_padimension3` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePADimension3` |
| `tbl_padimension4` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePADimension4` |
| `tbl_padimension5` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePADimension5` |
| `tbl_padimension6` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePAData.cs` | `SavePADimension6` |
| `tbl_payerclient` | pats | ETL P2 (all timezones) | 4 | EF upsert | `SavePayorClient.cs` | `SavePayerClient`, `RemovePayerClients` |
| `tbl_payerclthistory` | pats | P2 (EST/MST/PST) · P1 (CST) | 4/2 | EF upsert | `SavePayorClient.cs` | `SavePayerCltHistory` |
| `tbl_pbi3payauth` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveAuths.cs` | `SaveAuths` |
| `tbl_preadmission_v6` | ayx | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePreAdmissionV6.cs` | `SavePreAdmissionV6` |
| `tbl_preadmissionreferralsource` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SavePreAdmissionV6.cs` | `SavePreAdminReferrals` |
| `tbl_reassessment` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessment` |
| `tbl_reassessmentfamily` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentFamily` |
| `tbl_reassessmentlegal` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentLegal` |
| `tbl_reassessmentmentalhealth` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentMentalHealth` |
| `tbl_reassessmentoccupational` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentOccupational` |
| `tbl_reassessmentphysicalhealth` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentPhysicalHealth` |
| `tbl_reassessmentsocial` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentSocial` |
| `tbl_reassessmentsubstanceuse` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentSubstanceUse` |
| `tbl_reassessmenttreatment` | pats | SAMMS-ETL-Inv | 8 | EF upsert | `SaveAssessments.cs` | `SaveReAssessmentTreatment` |
| `tbl_services` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveServices` |
| `tbl_tbldiag10` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveBAM.cs` | `SaveTblDiags` |
| `tbl_treatmentlevel` | pats | ETL P2 (all timezones) | 4 | EF upsert | `SaveTreatmentLevel.cs` | `SaveTreatmentLevel` |
| `tbl_uaresultdetail` | pats | ETL P2 (all timezones) | 4 | BulkCopy | `SaveUAResults.cs` / `BulkDartsSvc.cs` | `SaveUAResultDetail` / `BulkDartsSrvLoader` |
| `tbl_uaresults` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveUAResults.cs` | `SaveUAResults` |
| `tbl_uasched` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveUAResults.cs` | `SaveUASched` |
| `tbl_user` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveGlobalUser` |
| `tbl_usersites` | ctrl | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveGlobal.cs` | `SaveGlobalUserSite` |
| `tbl_vacomprehensiveassessment` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveVACA` |
| `tbl_vacomprehensiveassessmentsummary` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | EF upsert | `SaveCA.cs` | `SaveVACASummary` |
| `tbl_vw3pbill` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | BulkCopy | `SaveBills.cs` | `SaveAuthBills` |
| `tbl_vw3pbillsub` | pats | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | BulkCopy | `SaveAuths.cs` / `BulkDartsSvc.cs` | `SaveAuthBillsub` / `BulkDartsSrvLoader` |
| `clientdemo` | stg | ETL P1 (by timezone) / SAMMSGlobal | 2/1 | BulkCopy | `BulkDartsSvc.cs` | `BulkDartsSrvLoader` → `stg.ClientDemoMerge1`, `stg.ClientDemoMerge2` |

---

## By Write Method

| Write Method | Tables |
|-------------|--------|
| **EF Core row-by-row upsert** | ~85 tables — lower-volume, change detection via RowChkSum |
| **SqlBulkCopy + stored proc merge** | ~13 tables — high volume: claims, claimlineitem, claimlineitemactivity, dartssrv, dose (most sites), labresultdetail, uaresultdetail, liquidlog (reload), formssammsclient, formquestionanswers (some sites), vw3pbill, vw3pbillsub, clientdemo |

---

## By Source File (BHG-DR-LIB)

| Source File | Tables Written |
|-------------|---------------|
| `BulkDartsSvc.cs` | `tbl_dartssrv` (bulk daily), `tbl_dose` (bulk path), `tbl_labresultdetail`, `tbl_uaresultdetail` (bulk), `tbl_claims`/`tbl_claimlineitem`/`tbl_claimlineitemactivity` (bulk), `tbl_dbo_formquestionanswers` (bulk), `tbl_formssammsclient` (bulk), `tbl_vw3pbillsub` (bulk), `clientdemo` (stg bulk) |
| `Save3pElig.cs` | `tbl_3pelig`, `tbl_3psetup`, `tbl_3parnote`, `tbl_3pclaimnote` |
| `SaveAppointments.cs` | `tbl_appointments`, `tbl_appointmentattend` |
| `SaveAssessments.cs` | `tbl_admissionassessment`, `tbl_admissionassessmentsummary`, `tbl_admissionassessmentdimension*` (×7), `tbl_admissionassessmentsubstanceusehistory`, `tbl_assessmentsubstanceusehistory`, `tbl_reassessment`, `tbl_reassessment*` (×8) |
| `SaveAuths.cs` | `tbl_pbi3payauth`, `tbl_vw3pbillsub` |
| `SaveBAM.cs` | `tbl_bamform`, `tbl_bamscore`, `tbl_tbldiag10` |
| `SaveBills.cs` | `tbl_bills`, `tbl_vw3pbill` |
| `SaveCA.cs` | `tbl_mncomprehensiveassessment`, `tbl_mncomprehensiveassessmentlevelofcare`, `tbl_vacomprehensiveassessment`, `tbl_vacomprehensiveassessmentsummary`, `tbl_newadmissionassessment`, `tbl_newadmissionassessmentasamdimension6`, `tbl_newperiodicreassessment`, `tbl_newperiodicreassessmentcounselorreview` |
| `SaveCheckIn.cs` | `tbl_checkin` |
| `SaveClaims.cs` | `tbl_claims`, `tbl_claimlineitem`, `tbl_claimlineitemactivity` |
| `SaveCleints.cs` | `tbl_clientdemo1`, `tbl_clientdemo2` |
| `SaveClinic.cs` | `tbl_clinic` |
| `SaveCodes.cs` | `tbl_codes` |
| `SaveCows.cs` | `tbl_cows_v6` |
| `SaveCustomQA.cs` | `tbl_customquestions`, `tbl_customanswers` |
| `SaveDartsSrvs.cs` | `tbl_dartssrv` (EF backfill, per-year `SaveDartSrv2014`–`SaveDartSrv2023`) |
| `SaveDoses.cs` | `tbl_dose` (EF path, 4 sites), `tbl_dose_excuse` |
| `SaveEnrollment.cs` | `tbl_enrollment` |
| `SaveFmp.cs` | `tbl_fmp` |
| `SaveFormQAData.cs` | `tbl_dbo_formquestionanswers`, `tbl_dbo_formanswersignatures`, `tbl_comprehensiveassessmentform`, `tbl_eandmformmdm`, `tbl_eandmformpregnancy` |
| `SaveGlobal.cs` | `tbl_briefaddictionmonitor`, `tbl_claimstatus`, `tbl_clinicalopiatewithdrawalscale`, `tbl_consents`, `tbl_feesched`, `tbl_formssammsclient`, `tbl_globaldevices`, `tbl_globalpayor`, `tbl_services`, `tbl_user`, `tbl_usersites` |
| `SaveInventory.cs` | `tbl_bottle`, `tbl_liquidlog`, `tbl_invtype`, `tbl_orientationchecklistnew` |
| `SaveOrders.cs` | `tbl_orders` (via `SaveOrders2016`–`SaveOrders2028`) |
| `SavePAData.cs` | `tbl_pa`, `tbl_pacounselorreview`, `tbl_padimension1`–`tbl_padimension6`, `tbl_drodownlistitems`, `tbl_financialhardshipapplication` |
| `SavePayorClient.cs` | `tbl_payerclient`, `tbl_payerclthistory` |
| `SavePreAdmissionV6.cs` | `tbl_preadmission_v6`, `tbl_preadmissionreferralsource` |
| `SaveTreatmentLevel.cs` | `tbl_treatmentlevel` |
| `SaveUAResults.cs` | `tbl_uaresults`, `tbl_uasched`, `tbl_labresult`, `tbl_uaresultdetail` |

---

## By Schema

| Schema | Table Count | Purpose |
|--------|------------|---------|
| `pats` | 79 | Patient and clinical data |
| `ctrl` | 10 | Clinic control / configuration |
| `ayx` | 1 | Alteryx analytics (pre-admission V6) |
| `stg` | 1 | Staging (bulk demographics) |
| **Total** | **98** | |

---

*Derived from static analysis of Scheduler/Program.cs, BHGTaskRunner/Program.cs, and all Save*/Bulk*.cs files in BHG-DR-LIB. Updated April 2026.*
