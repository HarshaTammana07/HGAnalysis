# Forms2Process — Bronze Extraction Columns (All 75 Enabled Tables)

**Applies to:** QA path (`pats.tbl_dbo_FormQuestionAnswers`) + AS path (`pats.tbl_dbo_FormAnswerSignatures`)
**Goal:** Extract once from SAMMS into Bronze; split into QA and AS in Silver.

---

## Standard Column Set (applies to all Default tables)

> Every Default table uses these base source columns. Sig date columns vary per table and are listed in column 2.

| Core Column | Source Column Name | Notes |
|---|---|---|
| id | `id` | Row PK |
| PreAdmissionId | `PreAdmissionId` | FK to SF_PatientPreAdmission |
| ClientId | `ClientId` | Patient reference |
| Createdby | `Createdby` | Varies casing across tables; same physical column |
| CreatedOn | Varies — see per-table entry | Column name stored in Forms2Process.CreatedOn |
| ModifiedBy | `ModifiedBy` | |
| ModifiedOn | Varies — see per-table entry | Column name from Forms2Process.ModifiedOn; NULL for some |
| IsDeleted | `IsDeleted` | Raw flag; final IsDeleted computed in Silver using pa + d |

**Additional join columns needed from dependency tables (Bronze must also carry):**
- `SF_PatientPreAdmission`: `ID`, `PatientId`, `IsDeleted`, `DataFormId`
- `SF_DataForms`: `Id`, `IsDeleted`

---

## All 75 Tables

| # | Table Name | Columns to Extract from Source | Path |
|---|---|---|---|
| 1 | `RIBHOLD` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **MedicalStaffSignatureDate** | **Default** |
| 2 | `RIHealthHomeCareReview` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 3 | `RIHealthHomeConsentToReceive` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 4 | `RIHealthHomeEligibilityFollUpChecklist` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 5 | `RIHealthHomeHistory` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 6 | `RIHealthHomeNote` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 7 | `RIHealthHomeTriageAssessment` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 8 | `RIHealthHomePatientCenteredPlan` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 9 | `RIOverdosePreventionEducation` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 10 | `RIPHQ9` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 11 | `PatientRightsandResponsibilities` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 12 | `OrderforServices` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **ProviderSignatureDate** | **Default** |
| 13 | `GAConsenttoTreatmentwithanApprovedNarcotic` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **ProviderSignatureDate**, **StaffSignatureDate** | **Default** |
| 14 | `GAConsentCentralRegistryGeorgia` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 15 | `TransitionandDischargePlan` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **ProviderSignatureDate**, **StaffSignatureDate**, **SupervisorSignatureDate** | **Default** |
| 16 | `NCConsenttoCentralRegistry` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 17 | `NCInitialTransitionDischargePlan` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 18 | `CrisisPrevention` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 19 | `NCConsentAuthDisclosureSubDisorder` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 20 | `NCPersonCenteredProfile` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **ProviderSignatureDate**, **StaffSignatureDate**, **SupervisorSignatureDate** | **Default** |
| 21 | `NCPIE` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 22 | `NinetyDayReview` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **MedicalProviderSignatureDate**, **StaffSignatureDate** | **Default** — note: `StaffSignatureDate` is mapped to the Provider sig slot in AS output; `MedicalProviderSignatureDate` to MedicalProvider sig slot |
| 23 | `StateFactForm` *(AR, Prefix 46)* | id, PreAdmissionId, ClientId, Createdby, **Createdon**, ModifiedBy, **ModifiedOn**, IsDeleted, **CompletedBySignatureDate** | **Default** — CreatedOn source column is `Createdon` (lowercase 'd') |
| 24 | `ConsenttoTreatmentViaTelehealth` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 25 | `tblDAANESNotification` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 26 | `tblMAARC` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedDate**, IsDeleted, **StaffSignatureDate** | **Default** — ModifiedOn source column is `ModifiedDate` (not `ModifiedOn`) |
| 27 | `InitialServicesPlanandVAD` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 28 | `StateFactForm` *(MN, Prefix 19)* | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **CompletedBySignatureDate** (also mapped to Staff sig slot) | **Default** — both CompletedBy and Staff sig slots point to same source column `CompletedBySignatureDate` |
| 29 | `MentalHealthInformedConsent` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 30 | `InsuranceBenefitVerification` | id, **PreAdmissionId**, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted | **Customized (QA + AS)**<br>• No `ClientId` column in source table<br>• **QA:** ClientId = `PreAdmissionId` (same column used for both)<br>• **AS:** ClientId = `pa.PatientId` (from joined SF_PatientPreAdmission)<br>• FormID formula: `Prefix-PreAdmId-PreAdmId-id` |
| 31 | `PatientInformationsheet` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted | **Default** — no sig date columns |
| 32 | `AdverseChildhood` | id, PreAdmissionId, ClientId, Createdby, **CreatedDate**, ModifiedBy, **ModifiedDate**, IsDeleted, **StaffSignatureDate** | **Customized (QA only)**<br>• CreatedOn source col = `CreatedDate`; ModifiedOn = `ModifiedDate`<br>• **QA FormID:** `Prefix-PreAdmId-PreAdmId-id` (PreAdmissionId used twice, not ClientId)<br>• AS: goes through default path normally |
| 33 | `GeneralConsentAuthforReleaseInfo` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 34 | `ConsentCentralRegistryColorado` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 35 | `KSPatientRightsResponsibilities` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 36 | `tblBHGNoticeOfPrivacyPractices` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate** | **Default** |
| 37 | `SuicideSeverityRatingScale` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate**, **SupervisorSignatureDate** | **Default** |
| 38 | `SAFETProtocolwithCSSRS` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate**, **SupervisorSignatureDate** | **Default** |
| 39 | `HealthQuestionnaire` | id, PreAdmissionId, ClientId, Createdby, **Createddate**, ModifiedBy, **Modifieddate**, IsDeleted, **DoctorSignatureDate**, **NurseSignatureDate** | **Default** — CreatedOn col = `Createddate`; ModifiedOn col = `Modifieddate`; MedicalProvider sig slot uses `NurseSignatureDate` |
| 40 | `InfectiousDiseaseAndBehavioralScreen` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **MedicalStaffSignatureDate** | **Default** |
| 41 | `ConsentToTreatmentWithAnApprovedNarcotic` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **DoctorSignatureDate**, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 42 | `FinancialHardshipApplication` | id, PreAdmissionId, **cltId**, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **FHAPatientSignatureDate** | **Customized (QA + AS)**<br>• ClientId source column is `cltId` (not `ClientId`)<br>• FormID formula uses `cltId` in the ClientId slot<br>• Patient sig date source column = `FHAPatientSignatureDate` |
| 43 | `ComprehensiveAssessmentForm` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **CAPatientSignatureDate**, **CAStaffSignatureDate** | **Default** — sig date source columns have form-prefixed names (`CA...`) |
| 44 | `AdmissionAssessment` | **From main table:** id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted<br>**From `AdmissionAssessmentSummary` (aas):** **AdmissionAssessmentPatientSignatureDate**, **AdmissionAssessmentStaffSignatureDate**, **AdmissionAssessmentSupervisorSignatureDate** | **Customized (AS only)**<br>• **AS:** Patient / Staff / Supervisor sig dates live in `AdmissionAssessmentSummary`, not in the main table — requires extra `INNER JOIN AdmissionAssessmentSummary aas ON a.Id = aas.AdmissionAssessmentId AND a.PreAdmissionId = aas.PreAdmissionId`<br>• **QA:** goes through default path (no extra join needed) |
| 45 | `tblTP17REVIEW` | **tprCLTID**, **tpRID**, **tprTPID**, **tprType**, **TprTYPE**, **tpTreatmentPhase**, **tprNEXT**, **tprReviewFrequency** *(if column exists)*, **tprDT**, **tprCreatedby**, **tprCLIRNTSIGDate**, **tprDRSIGDate**, **tprCOUNSSIGDate**, **tprSUPERSIGDate**, **Isdeleted** | **Customized (QA + AS) — Completely Custom**<br>• No `id`, no `PreAdmissionId`, no `ClientId`, no standard date columns<br>• No join to SF_PatientPreAdmission or SF_DataForms<br>• **QA:** Produces 3–4 question rows per source record (TreatmentPlanType, TreatmentPhaseType, NextDue, ReviewFrequency)<br>• **AS:** PatientSig = `tprCLIRNTSIGDate`, ProviderSig = `tprDRSIGDate`, StaffSig = `tprCOUNSSIGDate`, SupervisorSig = `tprSUPERSIGDate`<br>• IsDeleted = `CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END` |
| 46 | `tblORDERREQ` | **cltID**, **ReqNum**, **DateAdded**, **statusDate**, **Status**, **Notes**, **DrNote**, **EffectiveDate**, **expirationdate**, **Staff** *(used as Createdby)*, **StatusUser** *(used as UpdatedBy)*, **DrSigDt**, **SigNurseDt**, **sigCoordinatorDt** | **Customized (QA + AS) — Completely Custom**<br>• No `id`, no `PreAdmissionId`, no standard `ClientId` column (`cltID` is the client ref)<br>• No join to SF_PatientPreAdmission or SF_DataForms<br>• Filter at extraction: `Status = 'Approved'` and exclude test records<br>• **QA:** Produces 2 question rows per record (EffectiveDate, ExpirationDate)<br>• **AS:** ProviderSig = `ISNULL(DrSigDt, SigNurseDt)`, SupervisorSig = `sigCoordinatorDt`<br>• IsDeleted = `CASE WHEN cltID < 0 THEN 1 ELSE 0 END` |
| 47 | `ConsentforReleaseConInfoRevised` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 48 | `GuestDosingPermanentTransfer` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **CounselorSignatureDate**, **MedicalStaffSignatureDate**, **PatientSignatureDate**, **ProviderSignatureDate** | **Default** |
| 49 | `SF_PatientPreAdmission` | **Id** *(as PreAdmissionId)*, **PatientId** *(as ClientId)*, **ParentPreAdmissionId**, Createdby, **CreatedOn**, **LastUpdatedBy** *(as ModifiedBy)*, **LastUpdateOn** *(as ModifiedOn)*, IsDeleted, **PatientSignatureDate** *(Staff sig slot)* | **Customized (QA + AS)**<br>• `Id` is used as PreAdmissionId (the record IS the pre-admission row)<br>• `PatientId` is used as ClientId (no separate `ClientId` column)<br>• `ParentPreAdmissionId` is used in FormID formula<br>• Self-join: `SF_PatientPreAdmission a INNER JOIN SF_PatientPreAdmission pa ON a.ID = pa.ID`<br>• UpdatedBy source = `LastUpdatedBy`; ModifiedOn source = `LastUpdateOn` |
| 50 | `ReferralForm` | id, PreAdmissionId, ClientId, Createdby, **createdon**, **updatedby** *(as ModifiedBy)*, **updatedon** *(as ModifiedOn)*, IsDeleted, **patientsignaturedate**, **StaffSignDate** | **Customized (QA only)**<br>• UpdatedBy source column = `updatedby` (not `ModifiedBy`)<br>• `DateFilterEnabled = 0` — **no date filter applied; always full extract** regardless of lookback window<br>• AS: goes through default path |
| 51 | `SF_UnderstandingOfTreatment` | id, PreAdmissionId, Createdby, **Createddate**, **LastUpdatedBy** *(as ModifiedBy)*, **UpdatedDate** *(as ModifiedOn)*, IsDeleted, **PatientSignatureDate**, **CounselorSignatureDate** *(Staff sig slot)* | **Customized (QA + AS)**<br>• ClientId comes from `pa.PatientId` (joined SF_PatientPreAdmission), not from the source table itself — source table has no `ClientId` column<br>• UpdatedBy source = `LastUpdatedBy`; CreatedOn col = `Createddate`; ModifiedOn col = `UpdatedDate` |
| 52 | `MOConsentCentralRegistryMissouri` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 53 | `SCConsentReleaseCentralRegistry` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 54 | `ConsentToDiscloseAssignmentofBenefits` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 55 | `ConsenttoTreatmentforIOPOrEOPOrOP` | id, PreAdmissionId, ClientId, Createdby, **Createdon**, ModifiedBy, **Modifiedon**, IsDeleted, **PatientSignatureDate**, **ProviderSignatureDate**, **StaffSignatureDate** | **Default** — CreatedOn col = `Createdon`; ModifiedOn col = `Modifiedon` |
| 56 | `GPRA` | id, PreAdmissionId, ClientId, Createdby, **Createdon**, ModifiedBy, **Modifiedon**, IsDeleted, **StaffSignatureDate** | **Default** — CreatedOn col = `Createdon`; ModifiedOn col = `Modifiedon` |
| 57 | `MATandDriving` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 58 | `PatientRightsAndResponsibilitiesV2` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 59 | `RequestReleaseofMedicalRecordsV2` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 60 | `TakeHomeRiskAssessment` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **StaffSignatureDate** | **Default** |
| 61 | `AdultNutritionalScreen` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **MedicalStaffSignatureDate**, **PatientSignatureDate**, **ProviderSignatureDate** | **Default** |
| 62 | `ConsentForFollowUpContact` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 63 | `ConsentReleaseEmergencyContact` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 64 | `ConsentCentralRegistryAlabama` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate** | **Default** |
| 65 | `ConsentCentralRegistryLouisiana` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 66 | `OpioidOverdoseRisks` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 67 | `NoticeofPrivacyPracticesDC` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate** | **Default** |
| 68 | `NewAdmissionAssessment` | **From main table:** id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted<br>**From `NewAdmissionAssessmentASAMDimension6` (b):** **CounselorSignatureDate**, **PatientSignatureDate**, **ProviderSignatureDate**, **SupervisorSignatureDate** | **Customized (AS only)**<br>• **AS:** Counselor / Patient / Provider / Supervisor sig dates live in `NewAdmissionAssessmentASAMDimension6`, not in the main table — requires extra `INNER JOIN NewAdmissionAssessmentASAMDimension6 b ON a.preadmissionID = b.preadmissionID AND a.ID = b.NewAdmissionAssessmentFormId`<br>• **QA:** goes through default path (no extra join) |
| 69 | `ConsentandScreenFTST` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **ProviderSignatureDate**, **StaffSignatureDate** | **Default** |
| 70 | `ConsenttoReleaseInformationtotheHealthDepartmentRevised` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 71 | `TakeHomeAgreementandDiversionControl` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 72 | `TakeHomeGuidelinesForm` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 73 | `KYPatientRightsandResp` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **PatientSignatureDate**, **StaffSignatureDate** | **Default** |
| 74 | `PPDTest` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **ProviderSignatureDate**, **StaffSignatureDate** | **Default** |
| 75 | `MNComprehensiveAssessment` | id, PreAdmissionId, ClientId, Createdby, **CreatedOn**, ModifiedBy, **ModifiedOn**, IsDeleted, **CounselorSignatureDate**, **PatientSignatureDate**, **SupervisorSignatureDate** | **Default** |

---

## Summary

| Category | Count | Tables |
|---|---|---|
| **Default** | 65 | All standard tables — uses id, PreAdmissionId, ClientId, Createdby, [CreatedOn], ModifiedBy, [ModifiedOn], IsDeleted + table-specific sig date columns |
| **Customized** | 10 | `InsuranceBenefitVerification`, `AdverseChildhood`, `FinancialHardshipApplication`, `tblTP17REVIEW`, `tblORDERREQ`, `SF_PatientPreAdmission`, `ReferralForm`, `SF_UnderstandingOfTreatment`, `AdmissionAssessment`, `NewAdmissionAssessment` |

### Customized Tables — Difference Summary

| Table | QA Difference | AS Difference |
|---|---|---|
| `InsuranceBenefitVerification` | ClientId = PreAdmissionId (no ClientId col); FormID uses PreAdmId twice | ClientId = pa.PatientId (from join) |
| `AdverseChildhood` | FormID = Prefix-PreAdmId-PreAdmId-id (not ClientId); CreatedOn=CreatedDate | None — AS uses default |
| `FinancialHardshipApplication` | ClientId col = `cltId` not `ClientId` | ClientId col = `cltId`; FormID uses CltID |
| `tblTP17REVIEW` | Completely custom — no standard cols, no pa join; 3–4 question rows per record | Completely custom — sig dates from direct columns (tprCLIRNTSIGDate etc.) |
| `tblORDERREQ` | Completely custom — no standard cols, no pa join; 2 question rows per record; filter: Status='Approved' | Completely custom — ProviderSig=ISNULL(DrSigDt,SigNurseDt); SupervisorSig=sigCoordinatorDt |
| `SF_PatientPreAdmission` | ClientId=PatientId; PreAdmId=Id; self-join; UpdatedBy=LastUpdatedBy | Same as QA |
| `ReferralForm` | UpdatedBy col = `updatedby`; DateFilterEnabled=0 (always full extract) | None — AS uses default |
| `SF_UnderstandingOfTreatment` | ClientId = pa.PatientId (from join); UpdatedBy=LastUpdatedBy | Same as QA |
| `AdmissionAssessment` | None — QA uses default | Patient/Staff/Supervisor sig dates from `AdmissionAssessmentSummary` (inner join) |
| `NewAdmissionAssessment` | None — QA uses default | Counselor/Patient/Provider/Supervisor sig dates from `NewAdmissionAssessmentASAMDimension6` (inner join) |
