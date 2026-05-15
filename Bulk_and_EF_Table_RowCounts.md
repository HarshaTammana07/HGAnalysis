# BHG_DR — Bulk vs EF Core Table Row Counts

Snapshot of destination table row counts with load pattern. Staging counts reflect state at query time (non-zero staging may indicate a run in progress or incomplete cleanup).

---

## Table 1 — SqlBulkCopy + Stored Procedure MERGE (destination + staging)

| # | Table Name | Load Pattern | Row Count |
|---|------------|--------------|-----------|
| 1 | `pats.tbl_Dose` | Bulk (EF for 4 sites) | **180,565,142** |
| 2 | `pats.tbl_LiquidLog` | Bulk (reload) / EF (incr) | **143,073,994** |
| 3 | `pats.tbl_UAResultDetail` | Bulk | **68,763,344** |
| 4 | `pats.tbl_DartsSrv_2021` | Bulk | 28,617,731 |
| 5 | `pats.tbl_ClaimLineItemActivity` | Bulk | 50,177,640 |
| 6 | `pats.tbl_ClaimLineItem` | Bulk | 40,135,467 |
| 7 | `pats.tbl_DartsSrv_2023` | Bulk | 17,415,504 |
| 8 | `pats.tbl_DartsSrv_2020` | Bulk | 12,165,653 |
| 9 | `pats.tbl_vw3pBillSub` | Bulk | 12,117,238 |
| 10 | `pats.tbl_Claims` | Bulk (EF for 4 sites) | 10,000,496 |
| 11 | `pats.tbl_dbo_FormQuestionAnswers` | Bulk (18 sites) / EF (rest) | 20,769,466 |
| 12 | `pats.tbl_DartsSrv_2022` | Bulk | 14,220,791 |
| 13 | `pats.tbl_DartsSrv_2019` | Bulk | 7,947,200 |
| 14 | `pats.tbl_FormsSAMMSClient` | Bulk | 2,642,382 |
| 15 | `pats.tbl_ClientDemo2` | Bulk | 979,308 |
| 16 | `pats.tbl_ClientDemo1` | Bulk | 971,606 |
| 17 | `pats.tbl_LabResultDetail` | Bulk | 557,803 |
| 18 | `pats.tbl_Dose_Excuse` | EF (Bulk infra exists) | 460,770 |
| 19 | `pats.tbl_DartsSrv_2018` | Bulk | 0 |
| 20 | `pats.tbl_DartsSrv_2017` | Bulk | 0 |
| 21 | `pats.tbl_DartsSrv_2016` | Bulk | 0 |
| 22 | `pats.tbl_DartsSrv_2015` | Bulk | 0 |
| 23 | `pats.tbl_DartsSrv_2014B4` | Bulk | 0 |

### Staging tables (should be 0 between runs)

| Staging Table | Row Count | Note |
|---------------|-----------|------|
| `stg.tbl_FormQA` | 2,773 | Not empty — possible mid-run or failed cleanup |
| `stg.tbl_liquidlog` | 5,824 | Not empty — possible mid-run or failed cleanup |
| `stg.clientdemo` | 0 | OK |
| `stg.tbl_claimlineitem` | 0 | OK |
| `stg.tbl_claimlineitemactivity` | 0 | OK |
| `stg.tbl_claims` | 0 | OK |
| `stg.tbl_dartssrv` | 0 | OK |
| `stg.tbl_dose` | 0 | OK |
| `stg.tbl_dose_excuse` | 0 | OK |
| `stg.tbl_formssammsclient` | 0 | OK |
| `stg.tbl_labresultdetail` | 0 | OK |
| `stg.tbl_uaresultdetail` | 0 | OK |
| `stg.tbl_vw3pbillsub` | 0 | OK |

---

## Table 2 — EF Core Upsert (destination tables)

| # | Table Name | Category | Load Type | Row Count |
|---|------------|----------|-----------|-----------|
| 1 | `pats.tbl_3pArnote` | 3p Elig | EF Core Upsert | **64,480,291** |
| 2 | `pats.tbl_CheckIn` | Activity | EF Core Upsert | **31,523,430** |
| 3 | `pats.tbl_3pClaimNote` | 3p Elig | EF Core Upsert | **14,764,395** |
| 4 | `pats.tbl_UASched` | UA/Lab | EF Core Upsert | **9,950,734** |
| 5 | `pats.tbl_dbo_FormAnswerSignatures` | Forms | EF Core Upsert | 7,367,521 |
| 6 | `pats.tbl_Bills` | Billing | EF Core Upsert | 23,926,400 |
| 7 | `pats.tbl_Appointments` | Activity | EF Core Upsert | 2,247,734 |
| 8 | `pats.tbl_BAMScore` | BAM | EF Core Upsert | 1,392,831 |
| 9 | `tsk.tbl_RowTrax` | Audit | EF Core Upsert | 1,012,415 |
| 10 | `pats.tbl_Enrollment` | Enrollment | EF Core Upsert | 867,845 |
| 11 | `pats.tbl_PayerCltHistory` | Payer | EF Core Upsert | 700,897 |
| 12 | `pats.tbl_Cows_V6` | Clinical | EF Core Upsert | 641,660 |
| 13 | `pats.tbl_3pElig` | 3p Elig | EF Core Upsert | 578,432 |
| 14 | `pats.tbl_PayerClient` | Payer | EF Core Upsert | 579,378 |
| 15 | `pats.tbl_EandMFormMDM` | Forms | EF Core Upsert | 473,254 |
| 16 | `pats.tbl_ClinicalOpiateWithdrawalScale` | Global Ref | EF Core Upsert | 425,747 |
| 17 | `pats.tbl_Orders_2025` | Orders | EF Core Upsert | 381,958 |
| 18 | `pats.tbl_LabResult` | UA/Lab | EF Core Upsert | 384,445 |
| 19 | `pats.tbl_Orders_2024` | Orders | EF Core Upsert | 413,396 |
| 20 | `pats.tbl_Orders_2023` | Orders | EF Core Upsert | 313,810 |
| 21 | `pats.tbl_Orders_2022` | Orders | EF Core Upsert | 271,390 |
| 22 | `pats.tbl_Orders_2021` | Orders | EF Core Upsert | 268,677 |
| 23 | `pats.tbl_Orders_2020` | Orders | EF Core Upsert | 273,862 |
| 24 | `pats.tbl_Orders_2019` | Orders | EF Core Upsert | 252,309 |
| 25 | `pats.tbl_Orders_2018` | Orders | EF Core Upsert | 227,254 |
| 26 | `pats.tbl_Orders_2017` | Orders | EF Core Upsert | 224,518 |
| 27 | `pats.tbl_Orders_2016` | Orders | EF Core Upsert | 230,218 |
| 28 | `pats.tbl_Orders_2026` | Orders | EF Core Upsert | 111,714 |
| 29 | `pats.tbl_Orders` | Orders | EF Core Upsert | 122,367 |
| 30 | `pats.tbl_Orders_2027` | Orders | EF Core Upsert | 0 |
| 31 | `pats.tbl_Orders_2028` | Orders | EF Core Upsert | 0 |
| 32 | `pats.tbl_BriefAddictionMonitor` | Global Ref | EF Core Upsert | 268,343 |
| 33 | `pats.tbl_AdmissionAssessmentSubstanceuseHistory` | Assessment | EF Core Upsert | 64,872 |
| 34 | `pats.tbl_FinancialHardshipApplication` | PA Data | EF Core Upsert | 64,260 |
| 35 | `pats.tbl_AdmissionAssessmentSummary` | Assessment | EF Core Upsert | 48,305 |
| 36 | `pats.tbl_AdmissionAssessment` | Assessment | EF Core Upsert | 48,279 |
| 37 | `pats.tbl_AdmissionAssessmentDimensionOneDisorder` | Assessment | EF Core Upsert | 47,997 |
| 38 | `pats.tbl_AdmissionAssessmentDimensionThree` | Assessment | EF Core Upsert | 46,036 |
| 39 | `pats.tbl_AdmissionAssessmentDimensionTwo` | Assessment | EF Core Upsert | 46,257 |
| 40 | `pats.tbl_AdmissionAssessmentDimensionFour` | Assessment | EF Core Upsert | 45,738 |
| 41 | `pats.tbl_AdmissionAssessmentDimensionFiveSubstanceUse` | Assessment | EF Core Upsert | 45,645 |
| 42 | `pats.tbl_AdmissionAssessmentDimensionSix` | Assessment | EF Core Upsert | 45,611 |
| 43 | `pats.tbl_ComprehensiveAssessmentForm` | Forms | EF Core Upsert | 44,146 |
| 44 | `pats.tbl_BAMForm` | BAM | EF Core Upsert | 42,738 |
| 45 | `pats.tbl_FeeSched` | Global Ref | EF Core Upsert | 72,959 |
| 46 | `pats.tbl_ReAssessmentPhysicalHealth` | ReAssessment | EF Core Upsert | 78,871 |
| 47 | `pats.tbl_ReAssessmentMentalHealth` | ReAssessment | EF Core Upsert | 78,733 |
| 48 | `pats.tbl_ReAssessmentFamily` | ReAssessment | EF Core Upsert | 78,632 |
| 49 | `pats.tbl_ReAssessmentOccupational` | ReAssessment | EF Core Upsert | 78,554 |
| 50 | `pats.tbl_ReAssessmentLegal` | ReAssessment | EF Core Upsert | 78,449 |
| 51 | `pats.tbl_ReAssessmentSocial` | ReAssessment | EF Core Upsert | 78,434 |
| 52 | `pats.tbl_ReAssessmentSubstanceUse` | ReAssessment | EF Core Upsert | 79,713 |
| 53 | `pats.tbl_ReAssessment` | ReAssessment | EF Core Upsert | 79,716 |
| 54 | `pats.tbl_ReAssessmentTreatment` | ReAssessment | EF Core Upsert | 78,203 |
| 55 | `pats.tbl_AssessmentSubstanceuseHistory` | ReAssessment | EF Core Upsert | 0 |
| 56 | `pats.tbl_PA` | PA Data | EF Core Upsert | 101,747 |
| 57 | `pats.tbl_PADimension1` | PA Data | EF Core Upsert | 101,747 |
| 58 | `pats.tbl_PACounselorReview` | PA Data | EF Core Upsert | 100,947 |
| 59 | `pats.tbl_PADimension2` | PA Data | EF Core Upsert | 100,840 |
| 60 | `pats.tbl_PADimension3` | PA Data | EF Core Upsert | 100,721 |
| 61 | `pats.tbl_PADimension4` | PA Data | EF Core Upsert | 100,657 |
| 62 | `pats.tbl_PADimension5` | PA Data | EF Core Upsert | 100,592 |
| 63 | `pats.tbl_PADimension6` | PA Data | EF Core Upsert | 100,571 |
| 64 | `pats.tbl_Bottle` | Inventory | EF Core Upsert | 1,755,279 |
| 65 | `pats.tbl_OrientationChecklistNew` | Inventory | EF Core Upsert | 33,236 |
| 66 | `ctrl.tbl_InvType` | Inventory | EF Core Upsert | 822 |
| 67 | `pats.tbl_EandMFormPregnancy` | Forms | EF Core Upsert | 54,865 |
| 68 | `pats.tbl_CustomAnswers` | Forms | EF Core Upsert | 195,326 |
| 69 | `pats.tbl_CustomQuestions` | Forms | EF Core Upsert | 2,009 |
| 70 | `ctrl.tbl_Clinic` | Global Ref | EF Core Upsert | 118 |
| 71 | `pats.tbl_GlobalPayor` | Global Ref | EF Core Upsert | 2,933 |
| 72 | `pats.tbl_Services` | Global Ref | EF Core Upsert | 7,636 |
| 73 | `pats.tbl_Codes` | Global Ref | EF Core Upsert | 37,249 |
| 74 | `pats.tbl_TreatmentLevel` | Activity | EF Core Upsert | 70,679 |
| 75 | `pats.tbl_AppointmentAttend` | Activity | EF Core Upsert | 23,901 |
| 76 | `pats.tbl_FMP` | Financial | EF Core Upsert | 174,075 |
| 77 | `ayx.tbl_PreAdmission_V6` | Pre-Admission | EF Core Upsert | 28,193 |

---

## Notes

- **Hybrid tables** appear in one list as primary path; e.g. `pats.tbl_LiquidLog` is Bulk on reload and EF on incremental; `pats.tbl_dbo_FormQuestionAnswers` is Bulk for 18 sites and EF elsewhere.
- **DartsSrv 2014–2018** show 0 rows in the Bulk snapshot — historical years may not have been backfilled to Azure.
- **EF snapshot** omits tables that were not in your count run (e.g. `pats.tbl_3pSetup`, `pats.tbl_ClientDemo3`, `pats.tbl_GlobalUser`, `pats.tbl_GlobalUserSite`, `pats.tbl_Consents`, `pats.tbl_Devices`, `pats.tbl_ClaimStatus`, `pats.tbl_dropDownListItems`, `pats.tbl_PreAdmission_Referrals`, `pats.tbl_MNCA`, `pats.tbl_MNCALOC`, `pats.tbl_VACA`, `pats.tbl_VACASummary`, `pats.tbl_tblDiag10` / SaveTblDiags if present). Re-run the EF SQL script from `EF_vs_BulkCopy_Complete_Reference.md` to fill gaps.

---

*Generated for BHG_DR migration planning. Row counts are point-in-time.*
