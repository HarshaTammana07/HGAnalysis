# SAMMS Source Tables by ETL Pipeline

**Scope:** All 12 `BHGTaskRunner.exe` arguments found in `BCAppCode/BHGTaskRunner/updatedProgram.cs`.

**Source system:** Clinic SAMMS SQL Server databases, plus the global SAMMS database for global/reference rows.

**Metadata source:** `BCAppCode/ControlTables/vw_mapAction.csv`

**Routing source:** `BCAppCode/Scheduler/Program.cs` and the `args[0]` switch in `BCAppCode/BHGTaskRunner/updatedProgram.cs`.

**Important notes:**

- `BHGTaskRunner.exe 3` is a catch-all runner. In code it runs any ready parent task except P1, P2, and `SAMMSGlobal`, so its source tables are the union of the dedicated non-regional parents that are currently queued.
- `BHGTaskRunner.exe 5` exists for `Samms-LAB`, but the active metadata export has the LAB `tbl_ClientDemo1` and `tbl_ClientDemo2` rows disabled.
- `BHGTaskRunner.exe 12` exists for `SAMMS-ETL-PPA` in `updatedProgram.cs`; the checked-in `Scheduler/Program.cs` does not yet route rows to that parent. The PPA-related mappings are listed below from active metadata.

---

## All 12 Runner Arguments

| Arg | Parent task filter in `updatedProgram.cs` | ETL group | Source coverage |
|---:|---|---|---|
| 1 | `SAMMSGlobal` | Global/reference ETL | Global SAMMS reference tables and global form/client tables |
| 2 | `Central ETL P1`, `Eastern ETL P1`, `Mountain ETL P1`, `Pacific ETL P1` | Regional P1 | Broad clinic clinical/reference set by timezone |
| 3 | Excludes only P1, P2, and `SAMMSGlobal` | Catch-all | Whatever non-P1/P2/non-global parent tasks are pending, usually rows 5-12 below |
| 4 | `Central ETL P2`, `Eastern ETL P2`, `Mountain ETL P2`, `Pacific ETL P2` | Regional P2 | Claims, payer/client, billing, UA detail, E&M, and related support tables |
| 5 | `Samms-LAB` | LAB special ETL | Intended for LAB client demo tables; disabled in active metadata export |
| 6 | `Samms-Forms` | Forms ETL | SAMMS form answers and answer signatures |
| 7 | `SAMMS-ETL-Notes` | Notes ETL | Third-party AR and claim notes |
| 8 | `SAMMS-ETL-INV` | Inventory/assessment ETL | Inventory, lab results, appointments, admission assessment, reassessment |
| 9 | `SAMMS-ETL-DartSvc` | DART service ETL | DART service/session table |
| 10 | `SAMMS-ETL-Dose` | Dose ETL | Dose and dose excuse tables |
| 11 | `SAMMS-ETL-Orders` | Orders ETL | Medication orders table |
| 12 | `SAMMS-ETL-PPA` | PPA / pre-admission ETL | PPA-related active mappings exist, but scheduler routing is not checked in |

---

## Complete Grouped Source Table Matrix

| Arg | ETL / parent task | SAMMS source table(s) / view(s) | Azure destination table(s) | Metadata status |
|---:|---|---|---|---|
| 1 | `SAMMSGlobal` | `dbo.aaBriefAddictionMonitor`<br>`dbo.aaClinicalOpiateWithdrawalScale`<br>`dbo.tblClaimStatus`<br>`dbo.tblCONSENTS`<br>`dbo.tblDEVICE`<br>`dbo.tblFEESCHED`<br>`dbo.tblFORMSSAMMSCLIENT`<br>`dbo.tblPAYER`<br>`dbo.tbluser`<br>`dbo.tblUserSites` | `ctrl.tbl_ClaimStatus`<br>`ctrl.tbl_CONSENTS`<br>`ctrl.tbl_GlobalDevices`<br>`ctrl.tbl_User`<br>`ctrl.tbl_UserSites`<br>`pats.tbl_BriefAddictionMonitor`<br>`pats.tbl_clinicalopiatewithdrawalscale`<br>`pats.tbl_FeeSched`<br>`pats.tbl_FormsSAMMSClient`<br>`pats.tbl_GlobalPayor` | 10 active global mappings |
| 2 | Regional ETL P1: `Central ETL P1`, `Eastern ETL P1`, `Mountain ETL P1`, `Pacific ETL P1` | `dbo.admissionassessmentsubstanceusehistory`<br>`dbo.AppointmentAttend`<br>`dbo.BAMForm`<br>`dbo.BAMScore`<br>`dbo.ComprehensiveAssessmentForm`<br>`dbo.consenttomarketing`<br>`dbo.DroDownListItems`<br>`dbo.EandMFormPregnancy`<br>`dbo.FinancialHardshipApplication`<br>`dbo.MNComprehensiveAssessment`<br>`dbo.MNComprehensiveAssessmentLevelOfCare`<br>`dbo.mntreatmentservicereview`<br>`dbo.NewAdmissionAssessment`<br>`dbo.NewAdmissionAssessmentASAMDimension2`<br>`dbo.NewAdmissionAssessmentASAMDimension4`<br>`dbo.NewAdmissionAssessmentASAMDimension5`<br>`dbo.NewAdmissionAssessmentASAMDimension6`<br>`dbo.newdischargetransferplanform`<br>`dbo.NewPeriodicReassessment`<br>`dbo.NewPeriodicReassessmentCounselorReview`<br>`dbo.newperiodicreassessmentd2`<br>`dbo.newperiodicreassessmentd3`<br>`dbo.newperiodicreassessmentd4`<br>`dbo.newperiodicreassessmentd5`<br>`dbo.newperiodicreassessmentd6`<br>`dbo.PACounselorReview`<br>`dbo.PADimension1`<br>`dbo.PADimension2`<br>`dbo.PADimension3`<br>`dbo.PADimension4`<br>`dbo.PADimension5`<br>`dbo.PADimension6`<br>`dbo.PeriodicReassessment`<br>`dbo.SF_COWS`<br>`dbo.SF_DataForms`<br>`dbo.SF_PatientPreAdmission`<br>`dbo.SF_PatientPreadmissionReferralSource`<br>`dbo.SMSTextConsentForm`<br>`dbo.takehomeagreementanddiversioncontrol`<br>`dbo.TakeHomeRiskAssessment`<br>`dbo.tbl3PAYauth`<br>`dbo.Tbl3pElig`<br>`dbo.tbl3PSETUP`<br>`dbo.tblBill`<br>`dbo.tblCHECKIN`<br>`dbo.tblclient`<br>`dbo.tblClinic`<br>`dbo.tblCodes`<br>`dbo.tblCUSTOMANSWERS`<br>`dbo.tblCUSTOMQUESTIONS`<br>`dbo.Tbldiag10`<br>`dbo.tblENROLL`<br>`dbo.tblFMP`<br>`dbo.tblPayerCltHistory`<br>`dbo.tblSERVICES`<br>`dbo.tblTreatmentLevel`<br>`dbo.tblUAResult`<br>`dbo.tblUASched`<br>`dbo.VAComprehensiveAssessment`<br>`dbo.vacomprehensiveassessmentsummary`<br>`dbo.vw3pBillSub` | `ayx.tbl_PreAdmission_V6`<br>`ctrl.tbl_3PSETUP`<br>`ctrl.tbl_Clinic`<br>`ctrl.tbl_DroDownListItems`<br>`pats.tbl_3pElig`<br>`pats.tbl_Admissionassessmentsubstanceusehistory`<br>`pats.tbl_AppointmentAttend`<br>`pats.tbl_BAMForm`<br>`pats.tbl_BAMScore`<br>`pats.tbl_Bills`<br>`pats.tbl_CheckIn`<br>`pats.tbl_Codes`<br>`pats.tbl_ComprehensiveAssessmentForm`<br>`pats.tbl_consenttomarketing`<br>`pats.tbl_Cows_V6`<br>`pats.tbl_CustomAnswers`<br>`pats.tbl_CustomQuestions`<br>`pats.tbl_EandMFormPregnancy`<br>`pats.tbl_Enrollment`<br>`pats.tbl_FinancialHardshipApplication`<br>`pats.tbl_Fmp`<br>`pats.tbl_MNComprehensiveAssessment`<br>`pats.tbl_MNComprehensiveAssessmentLevelOfCare`<br>`pats.tbl_mntreatmentservicereview`<br>`pats.tbl_NewAdmissionassessment`<br>`pats.tbl_NewAdmissionAssessmentASAMDimension2`<br>`pats.tbl_NewAdmissionAssessmentASAMDimension4`<br>`pats.tbl_NewAdmissionAssessmentASAMDimension5`<br>`pats.tbl_NewAdmissionAssessmentASAMDimension6`<br>`pats.tbl_newdischargetransferplanform`<br>`pats.tbl_NewPeriodicReassessment`<br>`pats.tbl_NewPeriodicReassessmentCounselorReview`<br>`pats.tbl_newperiodicreassessmentd2`<br>`pats.tbl_newperiodicreassessmentd3`<br>`pats.tbl_newperiodicreassessmentd4`<br>`pats.tbl_newperiodicreassessmentd5`<br>`pats.tbl_newperiodicreassessmentd6`<br>`pats.tbl_PA`<br>`pats.tbl_PACounselorReview`<br>`pats.tbl_PADimension1`<br>`pats.tbl_PADimension2`<br>`pats.tbl_PADimension3`<br>`pats.tbl_PADimension4`<br>`pats.tbl_PADimension5`<br>`pats.tbl_PADimension6`<br>`pats.tbl_PayerCltHistory`<br>`pats.tbl_pbi3PayAuth`<br>`pats.tbl_PreadmissionReferralSource`<br>`pats.tbl_SERVICES`<br>`pats.tbl_SF_DataForms`<br>`pats.tbl_SF_PatientPreAdmission`<br>`pats.tbl_SMSTextConsentForm`<br>`pats.tbl_takehomeagreementanddiversioncontrol`<br>`pats.tbl_TakeHomeRiskAssessment`<br>`pats.tbl_TblDiag10`<br>`pats.tbl_TreatmentLevel`<br>`pats.tbl_UAResults`<br>`pats.tbl_UASched`<br>`pats.tbl_VAComprehensiveAssessment`<br>`pats.tbl_vacomprehensiveassessmentsummary`<br>`pats.tbl_vw3pBillSub`<br>`stg.ClientDemo` | 62 active unique mappings across regional time zones |
| 3 | Catch-all runner | Same as any ready non-P1, non-P2, non-`SAMMSGlobal` parent tasks. In practice this can include `Samms-LAB`, `Samms-Forms`, `SAMMS-ETL-Notes`, `SAMMS-ETL-INV`, `SAMMS-ETL-DartSvc`, `SAMMS-ETL-Dose`, `SAMMS-ETL-Orders`, and `SAMMS-ETL-PPA`. | Same destinations as whichever dedicated parent tasks are pending. | Code filter only; not a separate metadata table group |
| 4 | Regional ETL P2: `Central ETL P2`, `Eastern ETL P2`, `Mountain ETL P2`, `Pacific ETL P2` | `dbo.EandMForm`<br>`dbo.EandMFormPregnancy`<br>`dbo.tbl3pClaim`<br>`dbo.tbl3pClaimLineItem`<br>`dbo.tbl3pClaimLineItemActivity`<br>`dbo.tblBill`<br>`dbo.tblCHECKIN`<br>`dbo.tblENROLL`<br>`dbo.tblPayerClt`<br>`dbo.tblPayerCltHistory`<br>`dbo.tblUAResultDetail` | `pats.tbl_Bills`<br>`pats.tbl_CheckIn`<br>`pats.tbl_ClaimLineItem`<br>`pats.tbl_ClaimLineItemActivity`<br>`pats.tbl_Claims`<br>`pats.tbl_EandMFormMDM`<br>`pats.tbl_EandMFormPregnancy`<br>`pats.tbl_Enrollment`<br>`pats.tbl_PayerClient`<br>`pats.tbl_PayerCltHistory`<br>`pats.tbl_UAResultDetail` | 11 active unique mappings |
| 5 | `Samms-LAB` | `dbo.tblClient` | `pats.tbl_ClientDemo1`<br>`pats.tbl_ClientDemo2` | Present for LAB, but `Enabled = 0` in active export |
| 6 | `Samms-Forms` | `dbo.AnswerSignature`<br>`dbo.Form` | `pats.tbl_dbo_FormAnswerSignatures`<br>`pats.tbl_dbo_FormQuestionAnswers` | 2 active unique mappings |
| 7 | `SAMMS-ETL-Notes` | `dbo.tbl3pArnote`<br>`dbo.tbl3pClaimNote` | `pats.tbl_3pArnote`<br>`pats.tbl_3pClaimNote` | 2 active unique mappings |
| 8 | `SAMMS-ETL-INV` | `dbo.AdmissionAssessment`<br>`dbo.AdmissionAssessmentDimensionFiveSubstanceUse`<br>`dbo.AdmissionAssessmentDimensionFour`<br>`dbo.AdmissionAssessmentDimensionOneDisorder`<br>`dbo.AdmissionAssessmentDimensionSix`<br>`dbo.AdmissionAssessmentDimensionThree`<br>`dbo.AdmissionAssessmentDimensionTwo`<br>`dbo.AdmissionAssessmentSummary`<br>`dbo.Appointments`<br>`dbo.OrientationChecklistNew`<br>`dbo.ReAssessment`<br>`dbo.ReAssessmentFamily`<br>`dbo.ReAssessmentLegal`<br>`dbo.ReAssessmentMentalHealth`<br>`dbo.ReAssessmentOccupational`<br>`dbo.ReAssessmentPhysicalHealth`<br>`dbo.ReAssessmentSocial`<br>`dbo.ReAssessmentSubstanceUse`<br>`dbo.ReAssessmentTreatment`<br>`dbo.tblBottle`<br>`dbo.tblINVTYPE`<br>`dbo.tblLABRESULT`<br>`dbo.tblLABRESULTDETAIL`<br>`dbo.tblLiquidLog` | `ctrl.tbl_InvType`<br>`pats.Tbl_AdmissionAssessment`<br>`pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse`<br>`pats.tbl_AdmissionAssessmentDimensionFour`<br>`pats.Tbl_AdmissionAssessmentDimensionOneDisorder`<br>`pats.tbl_AdmissionAssessmentDimensionSix`<br>`pats.Tbl_AdmissionAssessmentDimensionThree`<br>`pats.Tbl_AdmissionAssessmentDimensionTwo`<br>`pats.Tbl_AdmissionAssessmentSummary`<br>`pats.Tbl_Appointments`<br>`pats.tbl_Bottle`<br>`pats.Tbl_LabResult`<br>`pats.tbl_LabResultDetail`<br>`pats.tbl_LiquidLog`<br>`pats.Tbl_OrientationChecklistNew`<br>`pats.Tbl_ReAssessment`<br>`pats.tbl_ReAssessmentFamily`<br>`pats.tbl_ReAssessmentLegal`<br>`pats.tbl_ReAssessmentMentalHealth`<br>`pats.tbl_ReAssessmentOccupational`<br>`pats.tbl_ReAssessmentPhysicalHealth`<br>`pats.tbl_ReAssessmentSocial`<br>`pats.tbl_ReAssessmentSubstanceUse`<br>`pats.tbl_ReAssessmentTreatment` | 24 active unique mappings |
| 9 | `SAMMS-ETL-DartSvc` | `dbo.tblDartsSrv` | `pats.tbl_DartsSrv` | 1 active unique mapping |
| 10 | `SAMMS-ETL-Dose` | `dbo.tblDose`<br>`dbo.tblDOSE_Excuse` | `pats.tbl_Dose`<br>`pats.tbl_Dose_Excuse` | 2 active unique mappings |
| 11 | `SAMMS-ETL-Orders` | `dbo.tblOrder` | `pats.tbl_Orders` | 1 active unique mapping |
| 12 | `SAMMS-ETL-PPA` | `dbo.SF_PatientPreAdmission`<br>`dbo.SF_PatientPreadmissionReferralSource` | `ayx.tbl_PreAdmission_V6`<br>`pats.tbl_PreadmissionReferralSource`<br>`pats.tbl_SF_PatientPreAdmission` | Active PPA-related mappings exist, but checked-in scheduler does not route them to `SAMMS-ETL-PPA` yet |

---

## Schedule 12 PPA Routing Gap

`updatedProgram.cs` contains this queue filter:

```csharp
case "12":
    pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC"
        && x.Status == 17
        && x.TaskName == "SAMMS-ETL-PPA"
        && x.RunAt < DateTime.Now).ToList();
    break;
```

The checked-in `Scheduler/Program.cs` routing CASE does not include `SAMMS-ETL-PPA`. With the current scheduler file, the PPA-related mappings are routed by the older regional rules instead:

| Source | Destination | Current scheduler behavior |
|---|---|---|
| `dbo.SF_PatientPreAdmission` | `ayx.tbl_PreAdmission_V6` | Regional P1 |
| `dbo.SF_PatientPreadmissionReferralSource` | `pats.tbl_PreadmissionReferralSource` | Regional P1 |
| `dbo.SF_PatientPreAdmission` | `pats.tbl_SF_PatientPreAdmission` | Regional P1 |

If production already has a newer scheduler or manual parent-task insert for `SAMMS-ETL-PPA`, these are the source mappings that match the PPA handler cases in `updatedProgram.cs`.

---

## Lookup Query

Use this against the control database to verify active source/destination mappings:

```sql
SELECT DISTINCT
    ma.ActionKey,
    ma.StepKey,
    ma.TimeZone,
    ma.SiteCode,
    ma.SrcSchema + '.' + ma.FromTblVw AS SAMMS_Source,
    ma.DsnSchema + '.' + ma.DsnTbl AS Azure_Destination,
    ma.WhereCondition
FROM dms.vw_MapAction ma
WHERE ma.Enabled = 1
  AND ma.IsActive = 1
  AND ma.SiteCode <> 'PHC'
ORDER BY
    ma.ActionKey,
    ma.StepKey,
    ma.TimeZone,
    ma.SiteCode;
```

---

*Derived from `BCAppCode/BHGTaskRunner/updatedProgram.cs`, `BCAppCode/Scheduler/Program.cs`, and `BCAppCode/ControlTables/vw_mapAction.csv`.*
