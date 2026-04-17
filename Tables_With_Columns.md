# BHG_DR Database - All Destination Tables with Column Names
# Generated from BHG-DR-LIB EF Core Model Classes
# Source: BCAppCode\BHG-DR-LIB\Models\
# Date: 2026-03-25

---

## Schema: pats — Patient Clinical Data Tables

---

### pats.tbl_3pARNOTE
**Model Class:** `Tbl3pArnote`  
**Column Count:** 15  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | RowState |
| 5 | ArnId |
| 6 | ArnLiid |
| 7 | ArnNote |
| 8 | ArnUser |
| 9 | ArnDate |
| 10 | ArnDtRemoved |
| 11 | ArnStrRemovedReason |
| 12 | ArnStrRemovedUser |
| 13 | Bid |
| 14 | ArnDbnotes |
| 15 | GlobalBatchId |

### pats.tbl_3pClaimNote
**Model Class:** `Tbl3pClaimNote`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Tpcn |
| 3 | TpcnTpcid |
| 4 | TpcnDtmAdded |
| 5 | TpcnStrAdded |
| 6 | TpcnStrNote |
| 7 | TpcnStrType |
| 8 | TpcnDtTickler |
| 9 | TpcnDtTicklerRemoved |
| 10 | TpcnStrTicklerRemovedNote |
| 11 | TpcnStrTicklerRemovedUser |
| 12 | TpcnStrTicklerType |
| 13 | GlobalBatchId |
| 14 | RowChkSum |
| 15 | LastModAt |
| 16 | RowState |

### pats.tbl_3pElig
**Model Class:** `Tbl3pElig`  
**Column Count:** 20  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | RowState |
| 4 | LastModAt |
| 5 | EId |
| 6 | EClt |
| 7 | EPayer |
| 8 | EDate |
| 9 | EStaff |
| 10 | EPost |
| 11 | EResponse |
| 12 | EStatus |
| 13 | EFormat |
| 14 | Filepath |
| 15 | EElecstatus |
| 16 | EstaffStatus |
| 17 | EstaffNote |
| 18 | EScan |
| 19 | EOrigid |
| 20 | Pyeligcheck |

### pats.tbl_AdmissionAssessment
**Model Class:** `TblAdmissionAssessment`  
**Column Count:** 12  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | DataFormId |
| 5 | ClientId |
| 6 | CreatedBy |
| 7 | CreatedOn |
| 8 | ModifiedBy |
| 9 | ModifiedOn |
| 10 | IsDeleted |
| 11 | Version |
| 12 | LastModAt |

### pats.tbl_AdmissionAssessmentDimensionFiveSubstanceUse
**Model Class:** `TblAdmissionAssessmentDimensionFiveSubstanceUse`  
**Column Count:** 23  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | HadAnOverdose |
| 5 | YourPhysicalHealthWorse |
| 6 | YourPhysicalMentalWorse |
| 7 | HaveYouCalled911 |
| 8 | SubstanceUseJeopardized |
| 9 | CausedProblemsAtYourJob |
| 10 | HavingAnyFinancialTroubles |
| 11 | DoesYourTemperTend |
| 12 | HaveYouEverBeenArrested |
| 13 | RiskOfBeingArrested |
| 14 | OpenOrPendingCourtCases |
| 15 | AreYouOnProbation |
| 16 | LegalCustodyOfYourChildren |
| 17 | AnyOpenCasesWitHlocalDepartment |
| 18 | ChildrenLiveInYourHome |
| 19 | Comments |
| 20 | PreAdmissionId |
| 21 | DimensionFiveComments |
| 22 | Dimension5Problems |
| 23 | LastModAt |

### pats.tbl_AdmissionAssessmentDimensionFour
**Model Class:** `TblAdmissionAssessmentDimensionFour`  
**Column Count:** 25  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | IdontThinkUseDrugsTooMuch |
| 5 | TryingTtoDrinklessThanUsed |
| 6 | IenjoyMyDrinking |
| 7 | IshouldCutDownOnMyDrinking |
| 8 | WasteOfTimeToThinkAboutMyDrinking |
| 9 | RecentlyChangedMyDrinking |
| 10 | AnyoneCanTalkAboutWanting |
| 11 | ThinkAboutDrinkingLessAlcohol |
| 12 | MyDrinkingUse |
| 13 | NoNeedForMeToThinkAbout |
| 14 | ActuallyChangingMyDrinking |
| 15 | DrinkingLessAlcohol |
| 16 | PrecontemplationScale |
| 17 | ContemplationScale |
| 18 | ActionScale |
| 19 | StageOfChange |
| 20 | Comments4 |
| 21 | DdldimensionFourScore |
| 22 | PreAdmissionId |
| 23 | Dimension4Problems |
| 24 | StatusofChange |
| 25 | LastModAt |

### pats.tbl_AdmissionAssessmentDimensionOneDisorder
**Model Class:** `TblAdmissionAssessmentDimensionOneDisorder`  
**Column Count:** 56  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | OpioidDisorderPresent |
| 5 | AlcoholDisorderPresent |
| 6 | SedativeDisorderPresent |
| 7 | StimulantDisorderPresent |
| 8 | CannabisDisorderPresent |
| 9 | HallucinogenDisorderPresent |
| 10 | InhalantDisorderPresent |
| 11 | PhencyclidineDisorderPresent |
| 12 | Comments |
| 13 | MedicallyAssistedWithdrawal |
| 14 | MedicallyAssistedWithdrawalHowManyTimes |
| 15 | MedicallyAssistedWithdrawalRecentTimes |
| 16 | InpatientRehabilitation |
| 17 | InpatientRehabilitationHowManyTimes |
| 18 | InpatientRehabilitationSuccessfullyComplete |
| 19 | InpatientRehabilitationRecentTimes |
| 20 | IntensiveOutpatientTreatments |
| 21 | IntensiveOutpatientHowManyTimes |
| 22 | IntensiveOutpatientSuccessfullyComplete |
| 23 | IntensiveOutpatientRecentTimes |
| 24 | OutpatientTreatment |
| 25 | OutpatientTreatmentHowManyTimes |
| 26 | OutpatientTreatmentSuccessfullyComplete |
| 27 | OutpatientTreatmentRecentTimes |
| 28 | PreviousMat |
| 29 | PreviousMatmethadone |
| 30 | PreviousMatbuprenorphine |
| 31 | PreviousMatnaltrexone |
| 32 | PreviousMatwhatWasYourDose |
| 33 | PreviousMatwasItHelpful |
| 34 | HowLongDidYouTakeIt |
| 35 | DdlhowLongDidYouTakeIt |
| 36 | LongestPeriodOfSobriety |
| 37 | DdllongestPeriodOfSobrietyFromAllSubstances |
| 38 | SubstanceUseHistoryComments |
| 39 | DdldimensionOneScore |
| 40 | PreAdmissionId |
| 41 | HowDoYouProcureTheDrug |
| 42 | BuyOnTheStreet |
| 43 | FreeFromFamily |
| 44 | PrescriptionFromHealthcareProvider |
| 45 | SellingUseOwnSupply |
| 46 | Theft |
| 47 | DdlmedicallyAssistedWithdrawal |
| 48 | DdlinpatientRehabilitation |
| 49 | DdlintensiveOutpatient |
| 50 | DdloutpatientTreatment |
| 51 | ChkMedicallyAssistedWithdrawal |
| 52 | ChkInpatientRehabilitation |
| 53 | ChkIntensiveOutpatientTreatments |
| 54 | ChkOutpatientTreatment |
| 55 | ChkPreviousMat |
| 56 | LastModAt |

### pats.tbl_AdmissionAssessmentDimensionSix
**Model Class:** `TblAdmissionAssessmentDimensionSix`  
**Column Count:** 21  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | StableHousingOfYourOwn |
| 5 | AreYouBehindOnYourRent |
| 6 | AreYouBehindOnYourUtility |
| 7 | DoYouHaveEnoughMoney |
| 8 | DoYouHaveJob |
| 9 | DoYouHaveSourceOfIncome |
| 10 | PeopleInHomeWhoDrinkAlcohol |
| 11 | DrugSellingCommonInYourNeighborhood |
| 12 | SafeFromPhysicalOrSexualAbuse |
| 13 | FamilyMembersWhoAreInRecovery |
| 14 | FamilyWhoYouCanCountOnToSupport |
| 15 | AnyPeerSupport |
| 16 | Comments |
| 17 | DdldimensionSixScore |
| 18 | PreAdmissionId |
| 19 | DimensionSixComments |
| 20 | Dimension6Problems |
| 21 | LastModAt |

### pats.Tbl_AdmissionAssessmentDimensionThree
**Model Class:** `TblAdmissionAssessmentDimensionThree`  
**Column Count:** 27  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | HaveYouEverBeenKnockedUnconscious |
| 5 | DidYouAttendSpecialEducation |
| 6 | Comments3 |
| 7 | Anxiety |
| 8 | GeneralizedAnxietyDisorder |
| 9 | SocialPhobia |
| 10 | PanicDisorder |
| 11 | Agoraphobia |
| 12 | PostTraumaticStressDisorder |
| 13 | ObsessiveCompulsiveDisorder |
| 14 | Depression |
| 15 | BipolarDisorder |
| 16 | Schizophrenia |
| 17 | SchizoaffectiveDisorder |
| 18 | HospitalizedForMentalHealth |
| 19 | HowManyTimes |
| 20 | MostRecentHospitalization |
| 21 | DiagnosedComment3 |
| 22 | DdldimensionThreeScore |
| 23 | PreAdmissionId |
| 24 | DoYouHaveApsychiatristTxt |
| 25 | DoYouHaveApsychiatrist |
| 26 | Dimension3Problems |
| 27 | LastModAt |

### pats.tbl_AdmissionAssessmentDimensionTwo
**Model Class:** `TblAdmissionAssessmentDimensionTwo`  
**Column Count:** 37  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | HighBloodPressure |
| 5 | Diabetes |
| 6 | HeartDisease |
| 7 | EpilepsySeizures |
| 8 | Asthma |
| 9 | HepatitisA |
| 10 | HepatitisB |
| 11 | HepatitisC |
| 12 | Hiv |
| 13 | LiverDisease |
| 14 | Cancer |
| 15 | Tuberculosis |
| 16 | RenalKidneyDisease |
| 17 | ChronicPain |
| 18 | Blindness |
| 19 | PoorVision |
| 20 | Deafness |
| 21 | HearingLoss |
| 22 | DiagnosedComment2 |
| 23 | DoYouUseTobacco |
| 24 | DoYouHaveAnyConcerns |
| 25 | Comments2 |
| 26 | DdldimensionTwoScore |
| 27 | PreAdmissionId |
| 28 | Allergies |
| 29 | PrimaryCarePractitioner |
| 30 | Dimension2Problems |
| 31 | Copdemphysema |
| 32 | HighCholesterol |
| 33 | Gerd |
| 34 | Other |
| 35 | OtherTxt |
| 36 | HepatitisD |
| 37 | LastModAt |

### pats.Tbl_AdmissionAssessmentSubstanceUseHistory
**Model Class:** `TblAdmissionAssessmentSubstanceUseHistory`  
**Column Count:** 21  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | RowState |
| 5 | Id |
| 6 | PreAdmissionId |
| 7 | CltId |
| 8 | TxEpisode |
| 9 | SubstanceType |
| 10 | Substance |
| 11 | Route |
| 12 | Amount |
| 13 | FrequencyOfLastUse |
| 14 | PeakUse |
| 15 | AgeOfFirstUse |
| 16 | DateOfLastUse |
| 17 | Withdrawal |
| 18 | ListSymptoms |
| 19 | Notes |
| 20 | CreatedOn |
| 21 | DateOfReported |

### pats.tbl_AdmissionAssessmentSummary
**Model Class:** `TblAdmissionAssessmentSummary`  
**Column Count:** 24  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | AdmissionAssessmentId |
| 4 | PreAdmissionId |
| 5 | Ddlrecommendation |
| 6 | OpioidTreatmentServices |
| 7 | WithdrawalManagement |
| 8 | ClinicalSummary |
| 9 | AsamrecommendationForLevel |
| 10 | LevelOfCareAtVariance |
| 11 | SummaryComments |
| 12 | AdmissionAssessmentStaffSignature |
| 13 | AdmissionAssessmentStaffSignatureBy |
| 14 | AdmissionAssessmentStaffSignatureDate |
| 15 | AdmissionAssessmentProviderSignature |
| 16 | AdmissionAssessmentProviderSignatureBy |
| 17 | AdmissionAssessmentProviderSignatureDate |
| 18 | AdmissionAssessmentPatientSignature |
| 19 | AdmissionAssessmentPatientSignatureBy |
| 20 | AdmissionAssessmentPatientSignatureDate |
| 21 | AdmissionAssessmentSupervisorSignature |
| 22 | AdmissionAssessmentSupervisorSignatureBy |
| 23 | AdmissionAssessmentSupervisorSignatureDate |
| 24 | LastModAt |

### pats.Tbl_AppointmentAttend
**Model Class:** `TblAppointmentAttend`  
**Column Count:** 9  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowState |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | AAId |
| 6 | aaaptID |
| 7 | aacltid |
| 8 | aaDTENROLLED |
| 9 | aaDTREMOVED |

### pats.tbl_Appointments
**Model Class:** `TblAppointments`  
**Column Count:** 35  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | UniqueId |
| 3 | LastModAt |
| 4 | RowChkSum |
| 5 | Type |
| 6 | StartDate |
| 7 | EndDate |
| 8 | AllDay |
| 9 | Subject |
| 10 | Location |
| 11 | Description |
| 12 | Status |
| 13 | Label |
| 14 | ResourceId |
| 15 | ResourceIds |
| 16 | ReminderInfo |
| 17 | RecurrenceInfo |
| 18 | PercentComplete |
| 19 | GroupName |
| 20 | CustomField1 |
| 21 | Attendees |
| 22 | Service |
| 23 | ServiceModifier |
| 24 | TxtNote |
| 25 | Area |
| 26 | IntakeAppointmentMissed |
| 27 | SalesForceId |
| 28 | IsSalesForceSync |
| 29 | IsThirdPartySync |
| 30 | AppointmentType |
| 31 | IsDropIn |
| 32 | IsSchedule |
| 33 | NoofParticipants |
| 34 | GroupTimeAllowed |
| 35 | GracePeriod |

### pats.Tbl_AssessmentSubstanceUseHistory
**Model Class:** `TblAssessmentSubstanceUseHistory`  
**Column Count:** 23  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | RowState |
| 5 | Id |
| 6 | AssessmentFormId |
| 7 | CltId |
| 8 | PreAdmissionId |
| 9 | TxEpisode |
| 10 | SubstanceType |
| 11 | Substance |
| 12 | Route |
| 13 | Amount |
| 14 | FrequencyOfLastUse |
| 15 | PeakUse |
| 16 | AgeOfFirstUse |
| 17 | DateOfLastUse |
| 18 | Withdrawal |
| 19 | ListSymptoms |
| 20 | Notes |
| 21 | CreatedOn |
| 22 | DateOfReported |
| 23 | MasterID |

### pats.tbl_BamForm
**Model Class:** `TblBamForm`  
**Column Count:** 63  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | ClientId |
| 5 | DataFormId |
| 6 | BAMDate |
| 7 | InterviewerID |
| 8 | ClinicianInterview |
| 9 | SelfReport |
| 10 | Phone |
| 11 | TimeStarted |
| 12 | InstructionsQ1 |
| 13 | InstructionsQ1Txt |
| 14 | InstructionsQ2 |
| 15 | InstructionsQ2Txt |
| 16 | InstructionsQ3 |
| 17 | InstructionsQ3Txt |
| 18 | InstructionsQ4 |
| 19 | InstructionsQ4Txt |
| 20 | InstructionsQ5 |
| 21 | InstructionsQ5Txt |
| 22 | InstructionsQ6 |
| 23 | InstructionsQ6Txt |
| 24 | InstructionsQ7A |
| 25 | InstructionsQ7B |
| 26 | InstructionsQ7C |
| 27 | InstructionsQ7D |
| 28 | InstructionsQ7E |
| 29 | InstructionsQ7F |
| 30 | InstructionsQ7G |
| 31 | InstructionsQ8 |
| 32 | InstructionsQ8Txt |
| 33 | InstructionsQ9 |
| 34 | InstructionsQ9Txt |
| 35 | InstructionsQ10 |
| 36 | InstructionsQ10Txt |
| 37 | InstructionsQ11 |
| 38 | InstructionsQ11Txt |
| 39 | InstructionsQ12 |
| 40 | InstructionsQ12Txt |
| 41 | InstructionsQ13 |
| 42 | InstructionsQ13Txt |
| 43 | InstructionsQ14 |
| 44 | InstructionsQ14Txt |
| 45 | InstructionsQ15 |
| 46 | InstructionsQ15Txt |
| 47 | InstructionsQ16 |
| 48 | InstructionsQ16Txt |
| 49 | InstructionsQ17 |
| 50 | InstructionsQ17Txt |
| 51 | TimeFinished |
| 52 | SubscaleScoreTxt1 |
| 53 | SubscaleScoreTxt2 |
| 54 | SubscaleScoreTxt3 |
| 55 | StaffSignature |
| 56 | StaffSignatureBy |
| 57 | StaffSignatureDate |
| 58 | CreatedBy |
| 59 | CreatedOn |
| 60 | ModifiedBy |
| 61 | ModifiedOn |
| 62 | IsDeleted |
| 63 | Version |

### pats.tbl_BamScore
**Model Class:** `TblBamScore`  
**Column Count:** 6  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ClientId |
| 4 | tprID |
| 5 | Description |
| 6 | Score |

### pats.tbl_BILLS
**Model Class:** `TblBills`  
**Column Count:** 25  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | BillId |
| 4 | BillCltid |
| 5 | BillGuestId |
| 6 | BillDate |
| 7 | BillBill |
| 8 | BillPay |
| 9 | BillPaytype |
| 10 | BillAdjust |
| 11 | BillReason |
| 12 | BillReceiptNum |
| 13 | StrUser |
| 14 | BlnDeposit |
| 15 | DtDeposit |
| 16 | BillAdjustid |
| 17 | Fifoallocated |
| 18 | Fifobalance |
| 19 | Costcenter |
| 20 | BillAptId |
| 21 | BillOrgdt |
| 22 | BillServId |
| 23 | BillSiteId |
| 24 | LastModAt |
| 25 | RowState |

### pats.tbl_Bottle
**Model Class:** `TblBottle`  
**Column Count:** 23  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | BottleId |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | RowState |
| 6 | Deanum |
| 7 | LotNumber |
| 8 | DtReceived |
| 9 | Liquid |
| 10 | BottleType |
| 11 | InitialAmount |
| 12 | DtClosed |
| 13 | BlnClosed |
| 14 | StrUser |
| 15 | White |
| 16 | SpecGrav |
| 17 | Weight |
| 18 | Color |
| 19 | InvGroup |
| 20 | BrId |
| 21 | SiteId |
| 22 | Manufacturer |
| 23 | ExpDate |

### pats.tbl_BriefAddictionMonitor
**Model Class:** `TblBriefAddictionMonitor`  
**Column Count:** 46  

| # | Column Name |
|---|-------------|
| 1 | FId |
| 2 | SiteCode |
| 3 | FClinic |
| 4 | FCltId |
| 5 | RowState |
| 6 | Date |
| 7 | ClinicianText |
| 8 | AdminList |
| 9 | IntervalList |
| 10 | UseCalc |
| 11 | RiskCalc |
| 12 | ProtectiveCalc |
| 13 | Q1answerList |
| 14 | Q2answerList |
| 15 | Q3answerList |
| 16 | Q4answerList |
| 17 | Q5answerList |
| 18 | Q6AnswerList |
| 19 | Test |
| 20 | Q1Answer |
| 21 | Q2Answer |
| 22 | Q3answer |
| 23 | Q4answer |
| 24 | Q5answer |
| 25 | Q6answer |
| 26 | Q7answerNumeric |
| 27 | Q7aList |
| 28 | Q7bList |
| 29 | Q7cList |
| 30 | Q7dList |
| 31 | Q7eList |
| 32 | Q7fList |
| 33 | Q7gList |
| 34 | Q8Answer |
| 35 | Q9Answer |
| 36 | Q10Answer |
| 37 | Q11Answer |
| 38 | Q12Answer |
| 39 | Q13Answer |
| 40 | Q14Answer |
| 41 | Q15Answer |
| 42 | Q15Answer1 |
| 43 | Q15Answer2 |
| 44 | Q16Answer |
| 45 | Q17Answer |
| 46 | Q14Answer2 |

### pats.tbl_CHECKIN
**Model Class:** `TblCheckIn`  
**Column Count:** 19  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | CiId |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | CiCltid |
| 6 | CiDate |
| 7 | CiTime |
| 8 | CiServeddtm |
| 9 | MinutesWaited |
| 10 | CiUser |
| 11 | CiHold |
| 12 | Cicltm4id |
| 13 | CicltName |
| 14 | CiCode |
| 15 | CiQueue |
| 16 | CiServedStaff |
| 17 | CiAmt |
| 18 | CiDoses |
| 19 | ciQUEUETIME |

### pats.tbl_ClaimLineItem
**Model Class:** `TblClaimLineItem`  
**Column Count:** 31  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | TpcliId |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | TpcliTpcid |
| 6 | TpcliDtmService |
| 7 | TpcliTxtService |
| 8 | TpcliIntUnits |
| 9 | TpcliDtmAdded |
| 10 | TpcliStrAdded |
| 11 | TpcliAmtCharge |
| 12 | TpcliStrCpt |
| 13 | TpcliStrModifier |
| 14 | TpcliStrNdc |
| 15 | TpcliStrPos |
| 16 | TpcliIntDx1 |
| 17 | TpcliIntDx2 |
| 18 | TpcliIntDx3 |
| 19 | TpcliIntDx4 |
| 20 | TpcliDiagnosis |
| 21 | TpcliDsid |
| 22 | TpcliPayerClaimId |
| 23 | TpcliProviderId |
| 24 | TpcliUnitfee |
| 25 | TpcliVoid |
| 26 | TpclivoidDt |
| 27 | TpclivoidUser |
| 28 | TpcliDtmServiceTo |
| 29 | TpcliIntMg |
| 30 | TpcliDbnotes |
| 31 | RowState |

### pats.tbl_ClaimLineItemActivity
**Model Class:** `TblClaimLineItemActivity`  
**Column Count:** 32  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LiaId |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | LiaTpcliid |
| 6 | LiaDtm |
| 7 | LiaStrUser |
| 8 | LaiPaidins |
| 9 | LaiContAdj |
| 10 | LaiGenadj |
| 11 | LaiCopay |
| 12 | LaiDeduc |
| 13 | LaiClient |
| 14 | LiaBitNoteOnly |
| 15 | LiaStrDesc |
| 16 | TprbId |
| 17 | LiaPending |
| 18 | Liaamt |
| 19 | Liastrtext |
| 20 | LiaAdjreason |
| 21 | LaiCoins |
| 22 | LiaAction1 |
| 23 | LiaAction2 |
| 24 | LiaAdjcontract |
| 25 | LiaAdjgeneral |
| 26 | LiaAnsi1 |
| 27 | LiaAnsi2 |
| 28 | LiaAnsimod1 |
| 29 | LiaAnsimod2 |
| 30 | BillId |
| 31 | LiaDbnotes |
| 32 | RowState |

### pats.tbl_Claims
**Model Class:** `TblClaims`  
**Column Count:** 96  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | TpcId |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | TpccltId |
| 6 | TpcStrStatus |
| 7 | TpcStrPayer |
| 8 | TpcDtmAdded |
| 9 | TpcStrAdded |
| 10 | F10oth |
| 11 | TpcClaimBatchId |
| 12 | F11insnumber |
| 13 | F11insplan |
| 14 | F11inssex |
| 15 | F12sig |
| 16 | F12sigdate |
| 17 | F13inssig |
| 18 | F14date |
| 19 | F15firstdate |
| 20 | F16dateunableend |
| 21 | F10auto |
| 22 | TpcStrPrimary |
| 23 | F10employ |
| 24 | F10local |
| 25 | F11insanother |
| 26 | F11insdob |
| 27 | F11insemploy |
| 28 | F16dateunablestart |
| 29 | F17refername |
| 30 | F17refernpi |
| 31 | F18datehospend |
| 32 | F18datehospstart |
| 33 | F19local |
| 34 | F1id |
| 35 | F20outsidelab |
| 36 | F21diag1 |
| 37 | F21diag2 |
| 38 | F21diag3 |
| 39 | F21diag4 |
| 40 | F22medresub |
| 41 | F23priorauth |
| 42 | F25taxid |
| 43 | F26account |
| 44 | F27assign |
| 45 | F28totalcharge |
| 46 | F29amtpaid |
| 47 | F2name |
| 48 | F30balancedue |
| 49 | F31date |
| 50 | F31phys |
| 51 | F32a |
| 52 | F32b |
| 53 | F32line1 |
| 54 | F32line2 |
| 55 | F32line3 |
| 56 | F32line4 |
| 57 | F33a |
| 58 | F33b |
| 59 | F33line1 |
| 60 | F33line2 |
| 61 | F33line3 |
| 62 | F33line4 |
| 63 | F33phone |
| 64 | F3dob |
| 65 | F4insname |
| 66 | F5add |
| 67 | F5city |
| 68 | F5phone |
| 69 | F5state |
| 70 | F5zip |
| 71 | F6insrel |
| 72 | F7insadd |
| 73 | F7inscity |
| 74 | F7insphone |
| 75 | F7insstate |
| 76 | F7inszip |
| 77 | F8stat |
| 78 | F9othinsdob |
| 79 | F9othinsemp |
| 80 | F9othinsname |
| 81 | F9othinsnumber |
| 82 | F9othinsplan |
| 83 | F9othinssex |
| 84 | TpcCreatedDate |
| 85 | TpcEncounter |
| 86 | TpcRebillreason |
| 87 | TpcStrWeek |
| 88 | TpcWkstart |
| 89 | TpcPayerCin |
| 90 | TpcSrvType |
| 91 | F3sex |
| 92 | TpcClaimType |
| 93 | SiteId |
| 94 | TpcDbnotes |
| 95 | TpcReferring |
| 96 | RowState |

### pats.tbl_CLIENTDEMO1
**Model Class:** `TblClientDemo1`  
**Column Count:** 37  

| # | Column Name |
|---|-------------|
| 1 | PrimKey |
| 2 | SiteCode |
| 3 | ClientId |
| 4 | RowChkSum |
| 5 | ClientM4id |
| 6 | FirstName |
| 7 | MiddleName |
| 8 | LastName |
| 9 | Suffix |
| 10 | Dob |
| 11 | Gender |
| 12 | Ssn |
| 13 | Email |
| 14 | Size |
| 15 | Address1 |
| 16 | Address2 |
| 17 | City |
| 18 | State |
| 19 | Zip |
| 20 | Phone |
| 21 | Preg |
| 22 | PregEdc |
| 23 | Marital |
| 24 | EmpStatus |
| 25 | Employer |
| 26 | WorkPhone |
| 27 | Income |
| 28 | Education |
| 29 | Hair |
| 30 | Eye |
| 31 | Height |
| 32 | Weight |
| 33 | Race |
| 34 | Language |
| 35 | County |
| 36 | LastModAt |
| 37 | RowState |

### pats.tbl_CLIENTDEMO2
**Model Class:** `TblClientDemo2`  
**Column Count:** 57  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | ClientId |
| 3 | RowChkSum |
| 4 | Counselor |
| 5 | Status |
| 6 | Prog |
| 7 | DateAdded |
| 8 | Amount |
| 9 | Freq |
| 10 | Dow1 |
| 11 | Dow2 |
| 12 | NextBill |
| 13 | LastBill |
| 14 | NextTp |
| 15 | PhysTb |
| 16 | Monthly |
| 17 | Bottles |
| 18 | Picpath |
| 19 | Remarks |
| 20 | Rin |
| 21 | Eth |
| 22 | Medicaid |
| 23 | EnrollDate |
| 24 | Bulk |
| 25 | Stand |
| 26 | Special |
| 27 | DtLastUa |
| 28 | Amsid |
| 29 | Nocensus |
| 30 | Changeuser |
| 31 | RepOldClient |
| 32 | Uaweekly |
| 33 | OptIn |
| 34 | Credit |
| 35 | Conttxdt |
| 36 | Ins |
| 37 | Risk |
| 38 | Clt3pBack |
| 39 | Clt3pfront |
| 40 | BiWeeklyUa |
| 41 | NurseNotes |
| 42 | Panel |
| 43 | Payday |
| 44 | FingerPrint1 |
| 45 | FingerPrint2 |
| 46 | Clt911Name |
| 47 | Clt911Ph |
| 48 | Clt911Relation |
| 49 | HolidayPickup |
| 50 | Ddapid |
| 51 | ProvClient |
| 52 | ProvClientId |
| 53 | BackFee |
| 54 | IsSalesForceSync |
| 55 | SalesForceId |
| 56 | LastModAt |
| 57 | RowState |

### pats.tbl_ClinicalOpiateWithdrawalScale
**Model Class:** `TblClinicalOpiateWithdrawalScale`  
**Column Count:** 24  

| # | Column Name |
|---|-------------|
| 1 | FId |
| 2 | SiteCode |
| 3 | FCltId |
| 4 | RowState |
| 5 | RowChkSum |
| 6 | LastModAt |
| 7 | CompletedName |
| 8 | CombinedScore |
| 9 | AssessDate |
| 10 | ReasonAssessList |
| 11 | RestingPulseNum |
| 12 | GiupsetNum |
| 13 | SweatNum |
| 14 | TremorNum |
| 15 | RestlessNum |
| 16 | YawnNum |
| 17 | PupilNum |
| 18 | AnxNum |
| 19 | BoneNum |
| 20 | GooseNum |
| 21 | RunnyNum |
| 22 | GenevaTest |
| 23 | TimeAmpm |
| 24 | AssesstimeText |

### pats.Tbl_ComprehensiveAssessmentForm
**Model Class:** `TblComprehensiveAssessmentForm`  
**Column Count:** 153  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | RowState |
| 5 | Id |
| 6 | DataFormId |
| 7 | PreAdmissionId |
| 8 | ClientId |
| 9 | ClientName |
| 10 | ClientM4Id |
| 11 | CreatedBy |
| 12 | CreatedOn |
| 13 | ModifiedBy |
| 14 | ModifiedOn |
| 15 | DDLActiveSubstanceUsers |
| 16 | DDLCurrentJob |
| 17 | DDLRelationshipStatus |
| 18 | DDLWhatKindOfSchoolAttend |
| 19 | DDLLiveWithYou |
| 20 | DDLPreferredLanguage |
| 21 | DDLEmploymentStatus |
| 22 | DDLCheckMother |
| 23 | DDLCheckFather |
| 24 | DDLCheckSibling |
| 25 | DDLCheckMaternalGrandmother |
| 26 | DDLCheckMaternalGrandfather |
| 27 | DDLCheckMaternalAunt |
| 28 | DDLCheckMaternalUncle |
| 29 | DDLCheckMaternalCousins |
| 30 | DDLCheckPaternalGrandmother |
| 31 | DDLCheckPaternalGrandfather |
| 32 | DDLCheckPaternalAunt |
| 33 | DDLCheckPaternalUncle |
| 34 | DDLCheckPaternalCousins |
| 35 | DDLHighestGradeCompleted |
| 36 | DDLWhatBranch |
| 37 | DDLTypeDischarge |
| 38 | DDLGender |
| 39 | DDLSexualOrientation |
| 40 | DDLInfluenceDrugs |
| 41 | DDLWhatBranchType |
| 42 | IsVeteransAdministration |
| 43 | IsPeerSupportMeetings |
| 44 | IsFriendsRecovery |
| 45 | IsCourtFines |
| 46 | IsOpenWarrants |
| 47 | IsOpenCourtCases |
| 48 | IsDrugTreatmentCourt |
| 49 | IsIncarcerated |
| 50 | IsArrested |
| 51 | IsSaferSexPractices |
| 52 | IsMakeYouUncomfortable |
| 53 | IsFeelingTraumatized |
| 54 | IsPertainingBeingLGBT |
| 55 | IsLGBT |
| 56 | IsCourtOrderedChildSupportPayments |
| 57 | IsArmedForces |
| 58 | IsTrainingActivities |
| 59 | IsEmploymentSituation |
| 60 | IsHighSchoolDiplomaGED |
| 61 | IsReadWriteEffectively |
| 62 | IsMainstreamClasses |
| 63 | IsHeldBackSchool |
| 64 | IsHaveAnyChildren |
| 65 | IsChildSupportPayments |
| 66 | IsCareOfFamilyMembers |
| 67 | IsAbuseNeglectGrowingUp |
| 68 | IsUnderstandEnglish |
| 69 | IsCountToSupportYou |
| 70 | IsCloseRelationship |
| 71 | IsDeployOverseas |
| 72 | CheckMother |
| 73 | CheckFather |
| 74 | CheckSibling |
| 75 | CheckMaternalGrandmother |
| 76 | CheckMaternalGrandfather |
| 77 | CheckMaternalAunt |
| 78 | CheckMaternalUncle |
| 79 | CheckMaternalCousins |
| 80 | CheckPaternalGrandmother |
| 81 | CheckPaternalGrandfather |
| 82 | CheckPaternalAunt |
| 83 | CheckPaternalUncle |
| 84 | CheckPaternalCousins |
| 85 | CheckVisuallyShowMe |
| 86 | CheckVerballyExplainItToMe |
| 87 | CheckPersonalExperience |
| 88 | CheckTactilelyHandsOn |
| 89 | CheckTalkItThrough |
| 90 | CheckNoOne |
| 91 | CheckImmediateFamily |
| 92 | CheckExtendedFamily |
| 93 | CheckCloseFriendsOnly |
| 94 | CheckFriends |
| 95 | CheckPeopleWork |
| 96 | CheckEveryone |
| 97 | CheckGamblingDisorder |
| 98 | CheckFoodOvereating |
| 99 | CheckEatingDisorders |
| 100 | CheckInternetAddiction |
| 101 | CheckSocialMediaAddiction |
| 102 | CheckLoveIntimacyDependence |
| 103 | CheckFamilyDisorder |
| 104 | CheckFriendsYourselfRecovery |
| 105 | CheckCoworkers |
| 106 | CheckMeetings |
| 107 | CheckOnline |
| 108 | IsDeleted |
| 109 | HowLongHadCurrentJob |
| 110 | IsFullTimeStudent |
| 111 | IsPartTimeStudent |
| 112 | Version |
| 113 | ThoseWhoAreNotcisgender |
| 114 | CulturalPreferencesForYourTreatment |
| 115 | FamilyStruggledWithDrugAlcoholProblems |
| 116 | ExperiencedAnytraumaAbuseNeglect |
| 117 | PhysicalAbuseViolenceCaptivityOther |
| 118 | VerbalEmotionalFinancialAbuse |
| 119 | NeglectTraumaRelatedYourRace |
| 120 | SexualAbuseAssaultSexualExploitation |
| 121 | CurrentlyExperiencingAbuseNglectExploitation |
| 122 | AnyDifficultyCopingWithTrauma |
| 123 | HaveYouEverReceivedServices |
| 124 | ProbationorParole |
| 125 | SocialHistoryProblemsWithOther |
| 126 | FindSupportYourselfInRecoveryOther |
| 127 | RaceWhite |
| 128 | RaceBlack |
| 129 | RaceAmericanIndian |
| 130 | RaceAsian |
| 131 | RaceNativeHawaiian |
| 132 | RaceTwoorMore |
| 133 | RaceOther |
| 134 | RaceOtherTxt |
| 135 | Hispanic |
| 136 | NonHispanic |
| 137 | DDLTermsofGender |
| 138 | ObsevationofOthers |
| 139 | AffectedYourEmployment |
| 140 | PhysicalAbuse |
| 141 | SexualAbuse |
| 142 | VerbalAbuse |
| 143 | Neglect |
| 144 | Captivity |
| 145 | SexualExploitation |
| 146 | LaborExploitation |
| 147 | TraumaRelatedtoRace |
| 148 | TraumaOther |
| 149 | SupportiveSexualOrientaion |
| 150 | NotSupportiveSexualOrientaion |
| 151 | SubstancesAffectedYourLife |
| 152 | AlwaysFollowsSaferSexPracices |
| 153 | HaveHighSchoolDiploma |

### pats.tbl_COWS_V6
**Model Class:** `TblCowsV6`  
**Column Count:** 43  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Cowid |
| 3 | Preadmissionid |
| 4 | CltId |
| 5 | RowState |
| 6 | RowChkSum |
| 7 | LastModAt |
| 8 | Dttime |
| 9 | ReasonforthisAssessment |
| 10 | RestingPulseRate |
| 11 | RestingPulseRatedesc |
| 12 | Giupset |
| 13 | Giupsetdesc |
| 14 | Sweating |
| 15 | Sweatingdesc |
| 16 | Tremor |
| 17 | Tremordesc |
| 18 | Restlessness |
| 19 | Restlessnessdesc |
| 20 | Yawning |
| 21 | Yawningdec |
| 22 | PupilSize |
| 23 | PupilSizedesc |
| 24 | AnxietyOrIrritability |
| 25 | AnxietyOrIrritabilitydesc |
| 26 | BoneOrJointAches |
| 27 | BoneOrJointAchesdesc |
| 28 | GoosefleshSkin |
| 29 | GoosefleshSkindesc |
| 30 | RunnyNoseOrTearing |
| 31 | RunnyNoseOrTearingdesc |
| 32 | CompletedBy |
| 33 | CreatedOn |
| 34 | CreatedBy |
| 35 | UpdatedBy |
| 36 | UpdatedOn |
| 37 | IsActive |
| 38 | PatientSignature |
| 39 | ClientSignatureDate |
| 40 | IsDeleted |
| 41 | StaffNameSignature |
| 42 | Version |
| 43 | StaffSignatureDate |

### pats.tbl_CustomAnswers
**Model Class:** `TblCustomAnswers`  
**Column Count:** 8  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowCheckSum |
| 4 | RowSate |
| 5 | CaId |
| 6 | CaQid |
| 7 | CaCltid |
| 8 | CaAns |

### pats.tbl_CustomQuestions
**Model Class:** `TblCustomQuestions`  
**Column Count:** 6  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowCheckSum |
| 4 | RowSate |
| 5 | CId |
| 6 | CQuestion |

### pats.tbl_DartsSrv
**Model Class:** `TblDartsSrv`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2015
**Model Class:** `TblDartsSrv_2015`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2016
**Model Class:** `TblDartsSrv_2016`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2017
**Model Class:** `TblDartsSrv_2017`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2018
**Model Class:** `TblDartsSrv_2018`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2019
**Model Class:** `TblDartsSrv_2019`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2020
**Model Class:** `TblDartsSrv_2020`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2021
**Model Class:** `TblDartsSrv_2021`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2022
**Model Class:** `TblDartsSrv_2022`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2023
**Model Class:** `TblDartsSrv_2023`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrv_2024
**Model Class:** `TblDartsSrv_2024`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_DartsSrvStg
**Model Class:** `TblDartsSrvStg`  
**Column Count:** 62  

| # | Column Name |
|---|-------------|
| 1 | DsId |
| 2 | DsClt |
| 3 | SiteCode |
| 4 | RowChkSum |
| 5 | DsDim1 |
| 6 | DsDim2 |
| 7 | DsDim3 |
| 8 | DsDim4 |
| 9 | DsDim5 |
| 10 | DsDim6 |
| 11 | DsTxtSrv |
| 12 | DsDtStart |
| 13 | DsDtEnd |
| 14 | DsTxtType |
| 15 | DsdblUnits |
| 16 | DsNoteId |
| 17 | DsDtAdded |
| 18 | DstxtStaff |
| 19 | DstxtNote |
| 20 | DsRtbnote |
| 21 | Dsbilled |
| 22 | DsGroupnum |
| 23 | DsProgram |
| 24 | DsUpdate |
| 25 | DsUpdatestaff |
| 26 | UpsizeTs |
| 27 | DsInvalidatedOn |
| 28 | DsError |
| 29 | DsTxtHiv |
| 30 | DsDartsGroup |
| 31 | RepOldSrv |
| 32 | DsSignature |
| 33 | DsSigDate |
| 34 | DssigdateCosign |
| 35 | DssignatureCosign |
| 36 | DsSigUser |
| 37 | DsSigUserCosign |
| 38 | DsSigclt |
| 39 | DsSigcltdate |
| 40 | DsSigcltuser |
| 41 | DsAptid |
| 42 | Dsuncharted |
| 43 | DsTxDim1 |
| 44 | DsTxDim2 |
| 45 | DsTxDim3 |
| 46 | DsTxDim4 |
| 47 | DsTxDim5 |
| 48 | DsTxDim6 |
| 49 | DsDiag |
| 50 | DsArea |
| 51 | DsGroupDefaultNote |
| 52 | DsGroupEnd |
| 53 | DsGroupIdentity |
| 54 | DsGroupStart |
| 55 | DsDiag10 |
| 56 | SiteId |
| 57 | DsDbnotes |
| 58 | DsSigCltImg |
| 59 | DsSignatureCoSignImg |
| 60 | DsSignatureImg |
| 61 | Mg |
| 62 | LastModAt |

### pats.tbl_dbo_AnswerSignatures
**Model Class:** `TblDboAnswerSignatures`  
**Column Count:** 13  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | FormId |
| 3 | SignatureId |
| 4 | Sign |
| 5 | SignedDateTime |
| 6 | ImageField |
| 7 | DataField |
| 8 | DateField |
| 9 | SignatureName |
| 10 | IsChildForm |
| 11 | RowState |
| 12 | LastModAt |
| 13 | RowChkSum |

### pats.tbl_dbo_FormAnswerSignatures
**Model Class:** `TblDboFormAnswerSignatures`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | FormName |
| 3 | FormId |
| 4 | ClientId |
| 5 | CreatedOn |
| 6 | UpdatedOn |
| 7 | CompletedBySignatureSignatureDate |
| 8 | CounselorSignatureSignatureDate |
| 9 | DoctorSignatureSignatureDate |
| 10 | MedicalProviderSignatureSignatureDate |
| 11 | PatientSignatureDate |
| 12 | ProviderSignatureSignatureDate |
| 13 | RequestorSignatureDate |
| 14 | StaffSignatureDate |
| 15 | SupervisorSignatureSignatureDate |
| 16 | RowState |
| 17 | LastModAt |
| 18 | RowChkSum |

### pats.tbl_dbo_FormQuestionAnswers
**Model Class:** `TblDboFormQuestionAnswers`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | FormName |
| 3 | FormId |
| 4 | ClientId |
| 5 | CreatedOn |
| 6 | CreatedBy |
| 7 | UpdatedOn |
| 8 | UpdatedBy |
| 9 | PreAdmissionId |
| 10 | IsDeleted |
| 11 | IsChildForm |
| 12 | QuestionId |
| 13 | QuestionOrderId |
| 14 | QuestionText |
| 15 | OptionId |
| 16 | AnswerValue |
| 17 | RowState |
| 18 | LastModAt |

### pats.tbl_DIAG10
**Model Class:** `TblDiag10`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | TblDiag10 |
| 2 | SiteCode |
| 3 | dgID |
| 4 | dgCLTID |
| 5 | dgDIAG |
| 6 | dgDESC |
| 7 | dgDATE |
| 8 | dgSTAFF |
| 9 | dgdt |
| 10 | dgPRIMARY |
| 11 | dgDIAG10 |
| 12 | dgDIAG10Description |
| 13 | dgNote |
| 14 | dgType |
| 15 | EnrollmentId |
| 16 | dgEndDate |

### pats.tbl_DOSE
**Model Class:** `TblDose`  
**Column Count:** 31  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowState |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | DoseId |
| 6 | CltId |
| 7 | DtMedDate |
| 8 | GuestId |
| 9 | DtDate |
| 10 | Dose |
| 11 | StrUser |
| 12 | BlVoid |
| 13 | StrVoidReason |
| 14 | BlException |
| 15 | Bottletype |
| 16 | Ordernum |
| 17 | ExceptionReason |
| 18 | BlBulk |
| 19 | BlPrepack |
| 20 | Dtgiven |
| 21 | Dtprep |
| 22 | DtVoid |
| 23 | Ppstaff |
| 24 | Exceptiontype |
| 25 | Manualauthdtm |
| 26 | Manualauthuser |
| 27 | Dosenote |
| 28 | Dosesig |
| 29 | InventoryGroup |
| 30 | SiteId |
| 31 | DoseSigImg |

### pats.tbl_DOSE_Excuse
**Model Class:** `TblDoseExcuse`  
**Column Count:** 10  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | RowState |
| 4 | ExId |
| 5 | CltId |
| 6 | DtEx |
| 7 | StrExcused |
| 8 | Dtstamp |
| 9 | StrUser |
| 10 | LastModAt |

### pats.tbl_EandMFormMDM
**Model Class:** `TblEandMformMdm`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | ClientId |
| 5 | DataFormId |
| 6 | CreatedOn |
| 7 | CreatedBy |
| 8 | ModifiedOn |
| 9 | ModifiedBy |
| 10 | Isdeleted |
| 11 | FormDate |
| 12 | ServiceId |
| 13 | Context |
| 14 | Version |
| 15 | MedicalProviderSignatureDate |
| 16 | MedicalProviderSignatureBy |

### pats.tbl_EandMFormPregnancy
**Model Class:** `TblEandMFormPregnancy`  
**Column Count:** 43  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | EandMformId |
| 3 | PreAdmissionId |
| 4 | ClientId |
| 5 | DataFormId |
| 6 | CreatedOn |
| 7 | CreatedBy |
| 8 | ModifiedOn |
| 9 | ModifiedBy |
| 10 | Isdeleted |
| 11 | FormDate |
| 12 | ServiceId |
| 13 | Context |
| 14 | Version |
| 15 | Ddltrimester |
| 16 | DoseTxt |
| 17 | MgTxt |
| 18 | DoseStabilityTxt |
| 19 | SignsTxt |
| 20 | Bleeding |
| 21 | Contraction |
| 22 | NauseaVomiting |
| 23 | PregnancyOtherTxt |
| 24 | MedicationsTxt |
| 25 | PrenatalVitaminsTxt |
| 26 | AllergiesTxt |
| 27 | ChangesInRoutineTxt |
| 28 | UdsradioBtn |
| 29 | SmokerRadioBtn |
| 30 | IllicitDrugTxt |
| 31 | NoOfPregnanciesTxt |
| 32 | DeliveriesTxt |
| 33 | DateOfLastOb |
| 34 | NameofObtxt |
| 35 | PregnancyCommentsTxt |
| 36 | Wttxt |
| 37 | GravidaTxt |
| 38 | ParaTxt |
| 39 | Provider |
| 40 | PrenatalCare |
| 41 | ReviewedandAcknowledged |
| 42 | NapregnancyGrid |
| 43 | LastModAt |

### pats.tbl_ENROLLMENT
**Model Class:** `TblEnrollment`  
**Column Count:** 54  

| # | Column Name |
|---|-------------|
| 1 | Id |
| 2 | SiteCode |
| 3 | RowChkSum |
| 4 | CltId |
| 5 | RowState |
| 6 | Program |
| 7 | EnrollDate |
| 8 | EnrollReasonCode |
| 9 | EnrollReasonText |
| 10 | DischargeReasonCode |
| 11 | DischargeReasonText |
| 12 | DischargeDate |
| 13 | StrStaff |
| 14 | Transfer |
| 15 | UpsizeTs |
| 16 | ParentEnrollId |
| 17 | NoDartsEnroll |
| 18 | NoDartsDischarge |
| 19 | RepOldEnroll |
| 20 | Dasareason |
| 21 | DtLastContact |
| 22 | StrArrests |
| 23 | StrBaby |
| 24 | StrBabyDf |
| 25 | StrEduc |
| 26 | StrEmpStat |
| 27 | StrLiving |
| 28 | StrNilf |
| 29 | StrPriFreq |
| 30 | StrPriProb |
| 31 | StrSecFreq |
| 32 | StrSecProb |
| 33 | StrSelfHelp |
| 34 | StrSelfHelpDet |
| 35 | StrSuppInt |
| 36 | StrTerFreq |
| 37 | StrTerProb |
| 38 | StrSchoolJobTraining |
| 39 | Counselor |
| 40 | DischargeSubReasonCode |
| 41 | EnrollSubReasonCode |
| 42 | OnDemand |
| 43 | Physician |
| 44 | SiteId |
| 45 | Module |
| 46 | Modulenote |
| 47 | DischargeIncome |
| 48 | IntakeIncome |
| 49 | StrDbnotes |
| 50 | Deleterecord |
| 51 | DtLastQuery |
| 52 | LastModAt |
| 53 | Modality |
| 54 | TreatmentLevel |

### pats.tbl_FEESCHED
**Model Class:** `TblFeeSched`  
**Column Count:** 30  

| # | Column Name |
|---|-------------|
| 1 | FsId |
| 2 | FsSite |
| 3 | RowChkSum |
| 4 | FsPayid |
| 5 | DsService |
| 6 | Cptcode |
| 7 | Fee |
| 8 | Contractual |
| 9 | Datespan |
| 10 | GroupTogether |
| 11 | Modifier |
| 12 | UnitMin |
| 13 | ProviderBill |
| 14 | CoAble |
| 15 | DefaultWeekFee |
| 16 | RevCode |
| 17 | Startdate |
| 18 | Enddate |
| 19 | Pos |
| 20 | AttendingBill |
| 21 | Pay2310A |
| 22 | Pay2310C |
| 23 | ReferredByAttending |
| 24 | BillAttendingOrder |
| 25 | BillOrderDoctor |
| 26 | FsMasterId |
| 27 | Notes1 |
| 28 | Notes2 |
| 29 | LastModAt |
| 30 | IsActive |

### pats.Tbl_FinancialHardshipApplication
**Model Class:** `TblFinancialHardshipApplication`  
**Column Count:** 46  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | Id |
| 6 | DataFormId |
| 7 | PreAdmissionId |
| 8 | CltId |
| 9 | CreatedBy |
| 10 | CreatedOn |
| 11 | ModifiedBy |
| 12 | ModifiedOn |
| 13 | IsDeleted |
| 14 | IsIdentification |
| 15 | IsIncome |
| 16 | txtIncomeIdentification |
| 17 | FHAPatientSignature |
| 18 | FHAPatientSignatureDate |
| 19 | FHAPatientSignatureBy |
| 20 | txtAnnualHouseholdIncome |
| 21 | EmergencyName |
| 22 | EmergencyRelation |
| 23 | EmergencyPhone |
| 24 | txtAUIGross1 |
| 25 | txtAUIGross2 |
| 26 | txtAUIGross3 |
| 27 | txtAUISocial1 |
| 28 | txtAUISocial2 |
| 29 | txtAUISocial3 |
| 30 | txtAUIAlimony1 |
| 31 | txtAUIAlimony2 |
| 32 | txtAUIAlimony3 |
| 33 | txtAUISelf1 |
| 34 | txtAUISelf2 |
| 35 | txtAUISelf3 |
| 36 | txtAUIRent1 |
| 37 | txtAUIRent2 |
| 38 | txtAUIRent3 |
| 39 | Version |
| 40 | IscurrentlyUninsured |
| 41 | StatusofApplication |
| 42 | Facts |
| 43 | PayClassApproved |
| 44 | ApprovedBy |
| 45 | EffectiveDate |
| 46 | ExpirationDate |

### pats.tbl_FMP
**Model Class:** `TblFmp`  
**Column Count:** 17  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | FmpId |
| 3 | FmpLngClt |
| 4 | FmpDtStart |
| 5 | FmpDtProjEnd |
| 6 | FmpDtEnd |
| 7 | FmpIntRate |
| 8 | FmpStrReason |
| 9 | FmpStrDesc |
| 10 | FmpDtAdded |
| 11 | FmpStrUserAdded |
| 12 | FmpDtEnded |
| 13 | FmpStrUserEnded |
| 14 | FmPendtext |
| 15 | AtriskType |
| 16 | RowState |
| 17 | LastModAt |

### pats.tbl_FormsSAMMSClient
**Model Class:** `TblFormsSammsclient`  
**Column Count:** 43  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Fscsid |
| 3 | FscDate |
| 4 | FscCltid |
| 5 | Fscsite |
| 6 | FscFormid |
| 7 | Fscform |
| 8 | ClientSig |
| 9 | StaffSig |
| 10 | SupervisorSig |
| 11 | PhysicianSig |
| 12 | ClientSigDate |
| 13 | StaffSigDate |
| 14 | SupervisorSigDate |
| 15 | PhysicianSigDate |
| 16 | Doctext |
| 17 | NurseSig |
| 18 | NurseSigBy |
| 19 | NurseSigDate |
| 20 | PhysicianSigBy |
| 21 | StaffSigBy |
| 22 | SupervisorSigBy |
| 23 | DoctextEditDate |
| 24 | DoctextEditBy |
| 25 | GuardianSig |
| 26 | GuardianSigBy |
| 27 | GuardianSigDate |
| 28 | ScanLink |
| 29 | ScanReplace |
| 30 | ClientSigImg |
| 31 | GuardianSigImg |
| 32 | NurseSigImg |
| 33 | PhysicianSigImg |
| 34 | StaffSigImg |
| 35 | SupervisorSigImg |
| 36 | Bac |
| 37 | AdminNurseSig |
| 38 | AdminNurseSigBy |
| 39 | AdminnurseSigDate |
| 40 | AdminnurseSigImg |
| 41 | LastModAt |
| 42 | RowChkSum |
| 43 | RowState |

### pats.tbl_GLOBALPAYOR
**Model Class:** `TblGlobalPayor`  
**Column Count:** 45  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | PayId |
| 3 | RowChkSum |
| 4 | PayName |
| 5 | PayAddress |
| 6 | PayCity |
| 7 | PaySt |
| 8 | Payzip |
| 9 | PayPh |
| 10 | PayFx |
| 11 | PayNote |
| 12 | PayDefaultsubmit |
| 13 | PayAuthformat |
| 14 | PayClmnum |
| 15 | Pay835 |
| 16 | PaySubmitType |
| 17 | PayBillamt |
| 18 | PayDosetype |
| 19 | PayerNumber |
| 20 | Payaddressjoin |
| 21 | PayCheckAuth |
| 22 | Payclass |
| 23 | PayIndfreq |
| 24 | PayindRate |
| 25 | PayIndunit |
| 26 | PayLabelName |
| 27 | Paynamejoin |
| 28 | PayOverride |
| 29 | PayPos |
| 30 | Payregion |
| 31 | PayReqauth |
| 32 | Paysig |
| 33 | PayGlclass |
| 34 | PayLab |
| 35 | NoClaimLevelRendering |
| 36 | Enddate |
| 37 | Revcode |
| 38 | StartDate |
| 39 | Pay2310A |
| 40 | Pay2310B |
| 41 | Pay2310C |
| 42 | SupressSecondary |
| 43 | AltTaxId |
| 44 | LastModAt |
| 45 | RowState |

### pats.tbl_LABRESULT
**Model Class:** `TblLabresult`  
**Column Count:** 22  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | LabrId |
| 5 | LablngCltId |
| 6 | LabrSchedId |
| 7 | LabrDropDt |
| 8 | LabrResultDt |
| 9 | LabrCreatedBy |
| 10 | LabrCreatedDt |
| 11 | Labnote |
| 12 | UpsizeTs |
| 13 | RepOldLab |
| 14 | LabrUpdatedBy |
| 15 | LabrUpdatedDt |
| 16 | LabBase64 |
| 17 | LabrLotno |
| 18 | OldClient |
| 19 | SiteId |
| 20 | LabOrderId |
| 21 | SupplementaryReport |
| 22 | LabName |

### pats.tbl_LABRESULTDETAIL
**Model Class:** `TblLabresultdetail`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | LabrdId |
| 5 | LabrdRecId |
| 6 | LabrdResult |
| 7 | Labdetail |
| 8 | LabrdFullNote |
| 9 | LabrdKey |
| 10 | LabrdNote |
| 11 | LabrdRx |

### pats.tbl_LiquidLog
**Model Class:** `TblLiquidLog`  
**Column Count:** 26  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LiqId |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | RowState |
| 6 | Pump |
| 7 | DoseId |
| 8 | BtlId |
| 9 | BkrId |
| 10 | Amt |
| 11 | Dtm |
| 12 | Desc |
| 13 | Staff |
| 14 | BlLogOnly |
| 15 | BlPrepack |
| 16 | Memonew |
| 17 | Memo |
| 18 | DtRti |
| 19 | AcknowledgeDate |
| 20 | AcknowledgeUser |
| 21 | RegionalDate |
| 22 | RegionalUser |
| 23 | ComplainceUser |
| 24 | ComplianceDate |
| 25 | Invgroup |
| 26 | SiteId |

### pats.Tbl_MNComprehensiveAssessment
**Model Class:** `TblMNComprehensiveAssessment`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | DataFormId |
| 5 | ClientId |
| 6 | TodayDate |
| 7 | ReferradBy |
| 8 | ReferradByOther |
| 9 | ReferralReason |
| 10 | ReferralReasonOther |
| 11 | InsuranceId |
| 12 | CreatedBy |
| 13 | CreatedOn |
| 14 | ModifiedBy |
| 15 | ModifiedOn |
| 16 | Version |
| 17 | IsDeleted |
| 18 | LastModAt |

### pats.tbl_MNComprehensiveAssessmentLevelOfCare
**Model Class:** `TblMNComprehensiveAssessmentLevelOfCare`  
**Column Count:** 42  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | MNComprehensiveAssessmentFormId |
| 3 | PreAdmissionId |
| 4 | SymptomsUrgentlyAddressed |
| 5 | SymptomsUrgentlyAddressedExplain |
| 6 | RisksofOpioid |
| 7 | TreatmentOptions |
| 8 | RisksofrecognitionOpioidOverdose |
| 9 | AvailabilityAdministration |
| 10 | Other |
| 11 | OtherTxt |
| 12 | LevelofCareRecommendation1 |
| 13 | LevelofCareRecommendation21 |
| 14 | LevelofCareRecommendation31 |
| 15 | LevelofCareRecommendation33 |
| 16 | LevelofCareRecommendation35 |
| 17 | LevelofCareRecommendation37 |
| 18 | LevelofCareRecommendation4 |
| 19 | OpioidTreatmentServices |
| 20 | WithdrawalManagement |
| 21 | ASAMRecommendation |
| 22 | NALOC |
| 23 | LOCNotAvailable |
| 24 | ClinicianJudgment |
| 25 | Patientpreference |
| 26 | PatientWaitingForLOC |
| 27 | RecommendedLOCAvailable |
| 28 | Geographicaccessibility |
| 29 | Familycaregiverresponsibilities |
| 30 | EmploymentResponsibilities |
| 31 | Courttreatmentrequirements |
| 32 | Lackofphysicalaccess |
| 33 | Languageaccessibility |
| 34 | LOCIsAvailable |
| 35 | Patientisineligible |
| 36 | AdditionalComments |
| 37 | LOCOther |
| 38 | LOCIsAvailableReason |
| 39 | PatientisineligibleReason |
| 40 | OtherReason |
| 41 | LevelofCareRecommendation25 |
| 42 | LastModAt |

### pats.tbl_NewAdmissionAssessment
**Model Class:** `TblNewAdmissionAssessment`  
**Column Count:** 12  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | DataFormId |
| 5 | ClientId |
| 6 | CreatedOn |
| 7 | CreatedBy |
| 8 | ModifiedOn |
| 9 | ModifiedBy |
| 10 | IsDeleted |
| 11 | Version |
| 12 | LastModAt |

### pats.tbl_NewAdmissionAssessmentASAMDimension6
**Model Class:** `TblNewAdmissionAssessmentASAMDimension6`  
**Column Count:** 76  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | NewAdmissionAssessmentFormId |
| 3 | PreAdmissionId |
| 4 | ReadinessQuestion1 |
| 5 | ReadinessQuestion2 |
| 6 | ReadinessQuestion3 |
| 7 | ReadinessQuestion4 |
| 8 | ReadinessQuestion5 |
| 9 | ReadinessQuestion6 |
| 10 | ReadinessQuestion7 |
| 11 | ReadinessQuestion8 |
| 12 | ReadinessQuestion9 |
| 13 | ReadinessQuestion10 |
| 14 | ReadinessQuestion11 |
| 15 | ReadinessQuestion12 |
| 16 | StageOfChange |
| 17 | AdditionalComments |
| 18 | TreatmentPreferences |
| 19 | ReasonNotWillingToAttend |
| 20 | ReasonWillNotAdmitReason |
| 21 | ReasonPatientIneligibleReason |
| 22 | ReasonOtherReason |
| 23 | ClinicalSummary |
| 24 | HasTreatmentPreferences |
| 25 | WillingToAttendRecommendedCare |
| 26 | TransportationChallenges |
| 27 | FoodHousingInsecurity |
| 28 | ChildcareResponsibilities |
| 29 | FinancialInsecurity |
| 30 | LackEmploymentOpportunities |
| 31 | LackJobSecurity |
| 32 | LackHealthcareCoverage |
| 33 | LackSocialSupports |
| 34 | LanguageBarriers |
| 35 | Level1 |
| 36 | Level1_5 |
| 37 | Level1_7 |
| 38 | Level2_1 |
| 39 | Level2_5 |
| 40 | Level2_7 |
| 41 | Level3_1 |
| 42 | Level3_5 |
| 43 | Level3_7 |
| 44 | NonBIO |
| 45 | BIO |
| 46 | Level4 |
| 47 | COE |
| 48 | ReasonNotAligned |
| 49 | ReasonNotAvailable |
| 50 | ReasonClinicianJudgment |
| 51 | ReasonPatientPreference |
| 52 | ReasonOnWaitingList |
| 53 | ReasonLacksPayment |
| 54 | ReasonGeographicAccess |
| 55 | ReasonCaregiverResponsibilities |
| 56 | ReasonEmploymentResponsibilities |
| 57 | ReasonCourtRequirements |
| 58 | ReasonTransportationChallenges |
| 59 | ReasonLanguageAccessibility |
| 60 | ReasonWillNotAdmit |
| 61 | ReasonPatientIneligible |
| 62 | ReasonOther |
| 63 | PatientSignature |
| 64 | PatientSignatureBy |
| 65 | SupervisorSignature |
| 66 | SupervisorSignatureBy |
| 67 | CounselorSignature |
| 68 | CounselorSignatureBy |
| 69 | ProviderSignature |
| 70 | ProviderSignatureBy |
| 71 | SuperviosorSignNA |
| 72 | PatientSignatureDate |
| 73 | SupervisorSignatureDate |
| 74 | CounselorSignatureDate |
| 75 | ProviderSignatureDate |
| 76 | LastModAt |

### pats.tbl_NewPeriodicReassessment
**Model Class:** `TblNewPeriodicReassessment`  
**Column Count:** 15  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | DataFormId |
| 4 | PreAdmissionId |
| 5 | ClientId |
| 6 | Date |
| 7 | CurrentPathway |
| 8 | CompletedAt |
| 9 | CompletedAtOthers |
| 10 | IsDeleted |
| 11 | CreatedBy |
| 12 | CreatedOn |
| 13 | ModifiedBy |
| 14 | ModifiedOn |
| 15 | Version |

### pats.tbl_NewPeriodicReassessmentCounselorReview
**Model Class:** `TblNewPeriodicReassessmentCounselorReview`  
**Column Count:** 58  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | NewPeriodicReassessmentId |
| 3 | PreAdmissionId |
| 4 | Level1 |
| 5 | Level1_5 |
| 6 | Level1_7 |
| 7 | Level2_1 |
| 8 | Level2_5 |
| 9 | Level2_7 |
| 10 | Level3_1 |
| 11 | Level3_5 |
| 12 | Level3_7 |
| 13 | NonBIO |
| 14 | BIO |
| 15 | Level4 |
| 16 | COE |
| 17 | ReasonNotAligned |
| 18 | ReasonNotAvailable |
| 19 | ReasonClinicianJudgment |
| 20 | ReasonPatientPreference |
| 21 | ReasonOnWaitingList |
| 22 | ReasonLacksPayment |
| 23 | ReasonGeographicAccess |
| 24 | ReasonCaregiverResponsibilities |
| 25 | ReasonEmploymentResponsibilities |
| 26 | ReasonCourtRequirements |
| 27 | ReasonTransportationChallenges |
| 28 | ReasonLanguageAccessibility |
| 29 | ReasonWillNotAdmit |
| 30 | ReasonPatientIneligible |
| 31 | ReasonOther |
| 32 | ReasonWillNotAdmitReason |
| 33 | ReasonPatientIneligibleReason |
| 34 | ReasonOtherReason |
| 35 | CopePhase1 |
| 36 | CopePhase2 |
| 37 | CopePhase3 |
| 38 | Induction |
| 39 | Stabilization |
| 40 | Maintenance |
| 41 | DateCompleted |
| 42 | UseScore |
| 43 | RiskScore |
| 44 | ProtectiveScore |
| 45 | ClinicalSummary |
| 46 | PatientSignature |
| 47 | PatientSignatureBy |
| 48 | PatientSignatureDate |
| 49 | CounselorSignature |
| 50 | CounselorSignatureBy |
| 51 | CounselorSignatureDate |
| 52 | ProviderSignature |
| 53 | ProviderSignatureBy |
| 54 | ProviderSignatureDate |
| 55 | SupervisorSignature |
| 56 | SupervisorSignatureBy |
| 57 | SupervisorSignatureDate |
| 58 | RR |

### pats.tbl_ORDERS
**Model Class:** `TblOrders`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2016
**Model Class:** `TblOrders2016`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2017
**Model Class:** `TblOrders2017`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2018
**Model Class:** `TblOrders2018`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2019
**Model Class:** `TblOrders2019`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2020
**Model Class:** `TblOrders2020`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2021
**Model Class:** `TblOrders2021`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2022
**Model Class:** `TblOrders2022`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2023
**Model Class:** `TblOrders2023`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2024
**Model Class:** `TblOrders2024`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2025
**Model Class:** `TblOrders2025`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2026
**Model Class:** `TblOrders2026`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2027
**Model Class:** `TblOrders2027`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.tbl_ORDERS_2028
**Model Class:** `TblOrders2028`  
**Column Count:** 67  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | OrderNum |
| 3 | CltId |
| 4 | RowChkSum |
| 5 | RowState |
| 6 | LastModAt |
| 7 | MedType |
| 8 | DateAdded |
| 9 | Orderdate |
| 10 | Doctor |
| 11 | EffectiveDate |
| 12 | ExpirationDate |
| 13 | Dose |
| 14 | Dose2 |
| 15 | Changeby |
| 16 | Intervals |
| 17 | Sunday |
| 18 | Monday |
| 19 | Tuesday |
| 20 | Wednesday |
| 21 | Thursday |
| 22 | Friday |
| 23 | Saturday |
| 24 | Sunday2 |
| 25 | Monday2 |
| 26 | Tuesday2 |
| 27 | Wednesday2 |
| 28 | Thursday2 |
| 29 | Friday2 |
| 30 | Saturday2 |
| 31 | Notes |
| 32 | Active |
| 33 | Type |
| 34 | Stype |
| 35 | Weeknum |
| 36 | SplitFirst |
| 37 | Blind |
| 38 | OUser |
| 39 | CltM4id |
| 40 | Newdose |
| 41 | Pckcode |
| 42 | RxhistId |
| 43 | Ex |
| 44 | ActbyDate |
| 45 | ActByUser |
| 46 | White |
| 47 | RepOldOrder |
| 48 | SigDr |
| 49 | DtSig |
| 50 | Aws |
| 51 | BlSched |
| 52 | BlVerbal |
| 53 | Color |
| 54 | DeActbyDate |
| 55 | DeActbyUser |
| 56 | OrderTypev5 |
| 57 | Sigentered |
| 58 | Signoted |
| 59 | SigNoteddt |
| 60 | Dtmid |
| 61 | SigMid |
| 62 | OverApprove |
| 63 | OverapproveDt |
| 64 | Sigentereddt |
| 65 | SigDrImg |
| 66 | SigMidImg |
| 67 | SigNotedImg |

### pats.Tbl_OrientationChecklistNew
**Model Class:** `TblOrientationChecklistNew`  
**Column Count:** 40  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | CheckListId |
| 3 | PreAdmissionId |
| 4 | ClientId |
| 5 | DataFormId |
| 6 | PatientComplaints |
| 7 | AccesstoEmergency |
| 8 | CodeofEthics |
| 9 | ConfidentialityPolicy |
| 10 | Methods |
| 11 | ExplanationofFiancialObligations |
| 12 | RulesforInvoluntaryDetox |
| 13 | FireSafety |
| 14 | ProgramRulesonPatientParking |
| 15 | PolicyonRestraint |
| 16 | PolicyonTobaccoProducts |
| 17 | PolicyonIllicit |
| 18 | PolicyonWeapons |
| 19 | KnowledgeofNames |
| 20 | ProgramRules |
| 21 | Aidshivprevention |
| 22 | HepatitisPrevention |
| 23 | PurposeandProcess |
| 24 | IndividualTreatmentPlan |
| 25 | PolicyRegardingUrineDrug |
| 26 | DischargeTransitionCriteria |
| 27 | NaturalProgression |
| 28 | CreatedBy |
| 29 | CreatedOn |
| 30 | ModifiedBy |
| 31 | ModifiedOn |
| 32 | IsDeleted |
| 33 | Version |
| 34 | StaffSignatureDate |
| 35 | StaffSignatureBy |
| 36 | PatientSignatureDate |
| 37 | PatientSignatureBy |
| 38 | LastModEtl |
| 39 | PatientSignature |
| 40 | StaffSignature |

### pats.tbl_PA
**Model Class:** `TblPA`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | DataFormId |
| 4 | PreAdmissionId |
| 5 | ClientId |
| 6 | Date |
| 7 | CurrentPathway |
| 8 | CurrentPathwayPhase |
| 9 | CompletedAt |
| 10 | CompletedAtOthers |
| 11 | IsDeleted |
| 12 | CreatedBy |
| 13 | CreatedOn |
| 14 | ModifiedBy |
| 15 | ModifiedOn |
| 16 | Version |

### pats.Tbl_PACounselorReview
**Model Class:** `TblPACounselorReview`  
**Column Count:** 37  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | EarlyIntervention |
| 8 | OutpatientTreatment |
| 9 | IntensiveOutpatient |
| 10 | PartialHospitalization |
| 11 | ResidentialInpatient |
| 12 | MedManagedIntensiveInpatient |
| 13 | OTS |
| 14 | OBOT |
| 15 | OTP |
| 16 | OBAT |
| 17 | WithdrawalManagement |
| 18 | CopePhase1 |
| 19 | CopePhase2 |
| 20 | CopePhase3 |
| 21 | Induction |
| 22 | Stabilization |
| 23 | Maintenance |
| 24 | DateCompleted |
| 25 | UseScore |
| 26 | RiskScore |
| 27 | ProtectiveScore |
| 28 | ClinicalSummary |
| 29 | PatientSignature |
| 30 | PatientSignatureBy |
| 31 | PatientSignatureDate |
| 32 | CounselorSignature |
| 33 | CounselorSignatureBy |
| 34 | CounselorSignatureDate |
| 35 | SupervisorSignature |
| 36 | SupervisorSignatureBy |
| 37 | SupervisorSignatureDate |

### pats.Tbl_PADimension1
**Model Class:** `TblPADimension1`  
**Column Count:** 17  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | LastUDS |
| 8 | UDSResult |
| 9 | IllegalSubstances |
| 10 | IllegalSubstancesBox |
| 11 | Overdose |
| 12 | OverdoseBox |
| 13 | NarcanAvailable |
| 14 | Cravings |
| 15 | CravingRating |
| 16 | Dimension1ASAMRating |
| 17 | UAEval |

### pats.Tbl_PADimension2
**Model Class:** `TblPADimension2`  
**Column Count:** 22  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | PhysicalHealthChange |
| 8 | Called911 |
| 9 | Called911Box |
| 10 | WorseningMedicalCondition |
| 11 | WorseningMedicalConditionBox |
| 12 | PrimaryCareProvider |
| 13 | PrimaryCareProviderBox |
| 14 | UnprotectedSex |
| 15 | DrugInjection |
| 16 | SharingDrug |
| 17 | HIVHepatits |
| 18 | HIVHepatitisBox |
| 19 | TobaccoNicotine |
| 20 | TobaccoNicotineFrequency |
| 21 | DiscontinueTobaccoNicotine |
| 22 | Dimension2ASAMRating |

### pats.Tbl_PADimension3
**Model Class:** `TblPADimension3`  
**Column Count:** 52  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | MentalHealthChange |
| 8 | MentalHealthHospitalized |
| 9 | MentalHealthHospitalizedbox |
| 10 | WorseningMentalHealth |
| 11 | WorseningMentalHealthBox |
| 12 | Agitation |
| 13 | DecreasedPleasure |
| 14 | Anxiety |
| 15 | LackofInterest |
| 16 | Confusion |
| 17 | PanicAttacks |
| 18 | BrainFog |
| 19 | Numbness |
| 20 | Insomnia |
| 21 | TroubleFallingAsleep |
| 22 | TroubleWakingUp |
| 23 | Headaches |
| 24 | StomachIssues |
| 25 | Fatigue |
| 26 | Restlessness |
| 27 | Tearfulness |
| 28 | IncreasedAppetite |
| 29 | DecreasedAppetite |
| 30 | Feelingempty |
| 31 | Irritability |
| 32 | Anger |
| 33 | GuiltShame |
| 34 | MoodSwings |
| 35 | DecreasedSelfControl |
| 36 | Nightmares |
| 37 | DecreasedEnergy |
| 38 | IncreasedEnergy |
| 39 | LackofFocus |
| 40 | Hallucinations |
| 41 | Isolation |
| 42 | ObsessiveWorryingThoughts |
| 43 | LackofMotivation |
| 44 | Forgetfulness |
| 45 | Nervousness |
| 46 | PersistentSadness |
| 47 | DisorganizedConfusedThoughts |
| 48 | OtherMentalSymptoms |
| 49 | OtherMentalSymptomsBox |
| 50 | WishedDead |
| 51 | KillingYourself |
| 52 | Dimension3ASAMRating |

### pats.Tbl_PADimension4
**Model Class:** `TblPADimension4`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | MotivationforChange |
| 8 | TreatmentSatisfaction |
| 9 | TreatmentSatisfactionBox |
| 10 | EventuallyDiscontinuing |
| 11 | Discontinuing3to6Months |
| 12 | Strengths |
| 13 | Needs |
| 14 | Abilities |
| 15 | PreferedforTreatment |
| 16 | Dimension4ASAMRating |

### pats.Tbl_PADimension5
**Model Class:** `TblPADimension5`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | Triggers |
| 8 | CopingStrategies |
| 9 | ContinueUsing |
| 10 | ContinueUsingBox |
| 11 | EmploymentStatus |
| 12 | EmploymentStatusOther |
| 13 | PartFullTime |
| 14 | Arrested |
| 15 | ChangeinLegalStatus |
| 16 | ChangeinLegalStatusBox |
| 17 | FinancialTrouble |
| 18 | Dimension5ASAMRating |

### pats.Tbl_PADimension6
**Model Class:** `TblPADimension6`  
**Column Count:** 36  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | LastModAt |
| 3 | RowState |
| 4 | RowChkSum |
| 5 | PeriodicReassessmentId |
| 6 | PreAdmissionId |
| 7 | CurrentlyLivingOther |
| 8 | EnvironmentStability |
| 9 | EnvironmentStabilityBox |
| 10 | SafefromExploitation |
| 11 | SafefromExploitationBox |
| 12 | Threats |
| 13 | ThreatsBox |
| 14 | Children |
| 15 | ChildrenAge |
| 16 | ChildrenAgeBox |
| 17 | ChildrenLegalCustody |
| 18 | ChildFamilyServicesOpenCases |
| 19 | FriendsFamilySupport |
| 20 | EnoughMoney |
| 21 | FamilyFriendsinRecovery |
| 22 | CurrentlyConnectedSupport |
| 23 | CurrentlyConnectedSupportBox |
| 24 | Barriers |
| 25 | BarriersBox |
| 26 | Dimension6ASAMRating |
| 27 | LivesAlone |
| 28 | HouseApartment |
| 29 | LiveKids |
| 30 | Shelter |
| 31 | LivesPartnerSpouse |
| 32 | SoberLivingHome |
| 33 | LivesFamily |
| 34 | Unhoused |
| 35 | LivesFriends |
| 36 | Other |

### pats.tbl_PayerClient
**Model Class:** `TblPayerClient`  
**Column Count:** 45  

| # | Column Name |
|---|-------------|
| 1 | Pcid |
| 2 | RowChkSum |
| 3 | SiteCode |
| 4 | PyId |
| 5 | PyPayerid |
| 6 | PyPayertype |
| 7 | PySubsid |
| 8 | PyGroup |
| 9 | PyAuth |
| 10 | PyStart |
| 11 | PyEnd |
| 12 | PyCltid |
| 13 | PyActive |
| 14 | Pyadd |
| 15 | Pycity |
| 16 | PyDob |
| 17 | Pyfirst |
| 18 | Pylast |
| 19 | PyPhone |
| 20 | Pysame |
| 21 | Pystate |
| 22 | Pyzip |
| 23 | PyAddDate |
| 24 | PyAddUser |
| 25 | PyBack |
| 26 | Pybupe |
| 27 | Pycoins |
| 28 | Pycopay |
| 29 | Pyded |
| 30 | Pydeduct |
| 31 | Pydeductleft |
| 32 | PyEligCheck |
| 33 | PyEligUser |
| 34 | Pyfront |
| 35 | Pymmt |
| 36 | Pyout |
| 37 | PyProjectedEnd |
| 38 | TempSavePayer |
| 39 | PyBasicNum |
| 40 | PyCategory |
| 41 | PyHmoprovider |
| 42 | PyLocalOffice |
| 43 | PyDbnotes |
| 44 | TypeOfAgreementCode |
| 45 | LastModAt |

### pats.tbl_PayerCltHistory
**Model Class:** `TblPayerCltHistory`  
**Column Count:** 7  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | PchId |
| 3 | PyId |
| 4 | PyChange |
| 5 | PyDtm |
| 6 | PyUser |
| 7 | PyNote |

### pats.tbl_pbi3PAYauth
**Model Class:** `TblPbi3Payauth`  
**Column Count:** 30  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | TpaId |
| 3 | TpEffdate |
| 4 | TpaCltid |
| 5 | TpaPayer |
| 6 | TpaDesc |
| 7 | TpaEffDate |
| 8 | TpaTermDate |
| 9 | TpaStaff |
| 10 | Tpadt |
| 11 | TpaAuthCode |
| 12 | TpAuthpath |
| 13 | TpConfirmpath |
| 14 | TpFail |
| 15 | TpRequestForm |
| 16 | TpResponseForm |
| 17 | TpServ |
| 18 | TpTermDate |
| 19 | TpUnits |
| 20 | TpServapproved |
| 21 | TpNote |
| 22 | TpType |
| 23 | TpaCompKey |
| 24 | TpaBigKey |
| 25 | ProgGroup |
| 26 | PayerGroup |
| 27 | PayerType |
| 28 | LastModAt |
| 29 | RowChkSum |
| 30 | RowState |

### pats.tbl_PreAdmissionReferralSource
**Model Class:** `TblPreAdmissionReferralSource`  
**Column Count:** 29  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | ClientId |
| 5 | DataFormId |
| 6 | PrimaryReferralSource |
| 7 | SecondaryReferralSource |
| 8 | ReferralSourceNote |
| 9 | CreatedBy |
| 10 | CreatedOn |
| 11 | LastUpdatedBy |
| 12 | LastUpdateOn |
| 13 | EnrollmentId |
| 14 | Program |
| 15 | IsDeleted |
| 16 | ReferralOrganization |
| 17 | ReferralName |
| 18 | AccountNotInList |
| 19 | ContactNotInList |
| 20 | WhyLeftTreatmentOfBhg |
| 21 | WhyComingBackToBhg |
| 22 | MostWantToDoDifferently |
| 23 | Organization |
| 24 | Name |
| 25 | Email |
| 26 | Phone |
| 27 | IsPatientReadmit |
| 28 | ReferralOrganizationId |
| 29 | ReferralNameId |

### pats.tbl_ReAssessment
**Model Class:** `TblReAssessment`  
**Column Count:** 13  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | ClientId |
| 5 | DataFormId |
| 6 | CreatedBy |
| 7 | ModifiedBy |
| 8 | CreatedOn |
| 9 | ModifiedOn |
| 10 | IsDeleted |
| 11 | TimeInTreatment |
| 12 | Version |
| 13 | LastModAt |

### pats.tbl_ReAssessmentFamily
**Model Class:** `TblReAssessmentFamily`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ReassessmentId |
| 4 | PreAdmissionId |
| 5 | DoYouHaveStableHousingOfYourOwn |
| 6 | AreYouSafeFromPhysicalOrSexualAbuseInYourHome |
| 7 | DoYouHaveLegalCustodyOfYourChildren |
| 8 | DoYouHaveAnyOpenCasesWithYourLocalDepartment |
| 9 | DoYouHaveEnoughMoney |
| 10 | CommentsFamily |
| 11 | LastModAt |

### pats.tbl_ReAssessmentLegal
**Model Class:** `TblReAssessmentLegal`  
**Column Count:** 13  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ReassessmentId |
| 4 | PreAdmissionId |
| 5 | HaveYouBeenArrested |
| 6 | DoYouHaveAnyOpenOrPendingCourtCases |
| 7 | AreYouOnProbationOrPayrole |
| 8 | AreYouInvolvedWithAdrugTreatmentCourt |
| 9 | DoYouHaveAnyOpenCriminalCases |
| 10 | DoYouHaveAnyOpenWarrants |
| 11 | DoYouOweMoneyForCourtFinesOrFees |
| 12 | CommentsLegal |
| 13 | LastModAt |

### pats.tbl_ReAssessmentMentalHealth
**Model Class:** `TblReAssessmentMentalHealth`  
**Column Count:** 8  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | ReAssessmentId |
| 5 | HaveYouBeenHospitalizedForMentalHealthReasons |
| 6 | HowHasYourMentalHealthChanged |
| 7 | DoYouHaveApsychiatrist |
| 8 | LastModAt |

### pats.tbl_ReAssessmentOccupational
**Model Class:** `TblReAssessmentOccupational`  
**Column Count:** 10  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ReassessmentId |
| 4 | PreAdmissionId |
| 5 | WhatIsYourCurrentEmploymentStatus |
| 6 | AreYouCurrentlyAfulltimeStudent |
| 7 | AreYouCurrentlyAparttimeStudent |
| 8 | HaveYouFoundAparttimeOrFulltimeJob |
| 9 | CommentsOccupational |
| 10 | LastModAt |

### pats.tbl_ReAssessmentPhysicalHealth
**Model Class:** `TblReAssessmentPhysicalHealth`  
**Column Count:** 19  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ReassessmentId |
| 4 | PreAdmissionId |
| 5 | HaveYouInjectedDrugs |
| 6 | HaveYouHadAnyUnsafeSex |
| 7 | HaveYouBeenTestedForHivandHepatitisC |
| 8 | ChkboxNa |
| 9 | ChkboxHivnegative |
| 10 | ChkboxHivpostive |
| 11 | ChkboxHepatitisCnegative |
| 12 | ChkboxHepatitisCpostive |
| 13 | IfYouWereHivpositive |
| 14 | IfYouWereHepatitisCpositive |
| 15 | DoYouHaveAprimaryCarePractitionerOrClinic |
| 16 | HowHasYourPhysicalHealthChanged |
| 17 | HaveYouCalled911OrBeeniItheEmergencyRoom |
| 18 | CommentsPhysicalHealth |
| 19 | LastModAt |

### pats.tbl_ReAssessmentSocial
**Model Class:** `TblReAssessmentSocial`  
**Column Count:** 9  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ReassessmentId |
| 4 | PreAdmissionId |
| 5 | DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink |
| 6 | DoYouHaveFriendsAndFamilyWhoYouCanCountOnToSupportYou |
| 7 | DoYouKnowOfAnyPeerSupport |
| 8 | CommentsSocial |
| 9 | LastModAt |

### pats.tbl_ReAssessmentSubstanceUse
**Model Class:** `TblReAssessmentSubstanceUse`  
**Column Count:** 8  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | ReAssessmentId |
| 5 | HaveYouHadAnOverdose |
| 6 | DoYouUseTobaccoOrVapeNicotine |
| 7 | CommentsSubstanceUse |
| 8 | LastModAt |

### pats.tbl_ReAssessmentTreatment
**Model Class:** `TblReAssessmentTreatment`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | ReassessmentId |
| 4 | PreAdmissionId |
| 5 | AreYouSatisfiedWith |
| 6 | IsEventuallyTaperingOff |
| 7 | DoYouPlanOnTaperingOff |
| 8 | WhatNeedsDoYouHaveThatWeCanHelpYou |
| 9 | WhatHaveYouLearnedAboutWhatYouPrefer |
| 10 | ClientId |
| 11 | LastModAt |

### pats.tbl_SERVICES
**Model Class:** `TblServices`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | SId |
| 4 | SService |
| 5 | SArea |
| 6 | SCost |
| 7 | SCptcode |
| 8 | SReqsig |
| 9 | SReqtime |
| 10 | BlAllowOverlap |
| 11 | OldArea |
| 12 | OldSrv |
| 13 | SFilter |
| 14 | SReportBillable |
| 15 | STimeOnly |
| 16 | CreatedOn |
| 17 | LastModAt |
| 18 | IsActive |

### pats.tbl_TreatmentLevel
**Model Class:** `TblTreatmentLevel`  
**Column Count:** 9  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | RowState |
| 4 | LastModAt |
| 5 | ID |
| 6 | TreatmentLevel |
| 7 | UserID |
| 8 | CltId |
| 9 | RecordOn |

### pats.tbl_UARESULTDETAIL
**Model Class:** `TblUAResultDetail`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | UardId |
| 5 | UardRecId |
| 6 | UardResult |
| 7 | UardRx |
| 8 | UaDetail |
| 9 | UardFullNote |
| 10 | UardKey |
| 11 | UardNote |

### pats.tbl_UARESULTS
**Model Class:** `TblUAResults`  
**Column Count:** 33  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | UarResultDt |
| 5 | UarId |
| 6 | UarLngCltId |
| 7 | UarSchedId |
| 8 | UarDropDt |
| 9 | UarCreatedBy |
| 10 | UarCreatedDt |
| 11 | CpId |
| 12 | UaNote |
| 13 | Oldnum |
| 14 | OldClient |
| 15 | UpsizeTs |
| 16 | RepOldUar |
| 17 | UarLabKey |
| 18 | UarUpdatedBy |
| 19 | UarUpdatedDt |
| 20 | UaType |
| 21 | SiteId |
| 22 | UaDbnotes |
| 23 | UaNurseNote |
| 24 | UaSig |
| 25 | UaSigDt |
| 26 | UaSigImg |
| 27 | UaSigUser |
| 28 | Location |
| 29 | ScheduledDate |
| 30 | UaBase64 |
| 31 | Uaprogram |
| 32 | LabName |
| 33 | UAEval |

### pats.tbl_UASched
**Model Class:** `TblUasched`  
**Column Count:** 25  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | UasId |
| 3 | UasLngCltId |
| 4 | UasDt |
| 5 | UasDtAdded |
| 6 | UasStat |
| 7 | UasStatDt |
| 8 | UasStatUser |
| 9 | LngCpano |
| 10 | UasNote |
| 11 | OldNum |
| 12 | OldClient |
| 13 | RepOldUas |
| 14 | UasCollectedBy |
| 15 | UasCollectedDate |
| 16 | UasManifestDate |
| 17 | UasPanel |
| 18 | UasPanelOther |
| 19 | UasType |
| 20 | UasEtg |
| 21 | Uapriority |
| 22 | Uasticketprintdate |
| 23 | RowChkSum |
| 24 | LastModAt |
| 25 | RowState |

### pats.tbl_VAComprehensiveAssessment
**Model Class:** `TblVAComprehensiveAssessment`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | PreAdmissionId |
| 4 | DataFormId |
| 5 | ClientId |
| 6 | CreatedBy |
| 7 | CreatedOn |
| 8 | ModifiedBy |
| 9 | ModifiedOn |
| 10 | IsDeleted |
| 11 | LastModAt |

### pats.tbl_VAComprehensiveAssessmentSummary
**Model Class:** `TblVAComprehensiveAssessmentSummary`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | VAComprehensiveAssessmentId |
| 4 | PreAdmissionId |
| 5 | DDLRecommendation |
| 6 | OpioidTreatmentServices |
| 7 | WithdrawalManagement |
| 8 | ClinicalSummary |
| 9 | ASAMRecommendationForLevel |
| 10 | LevelOfCareAtVariance |
| 11 | SummaryComments |

### pats.tbl_vw3pbill
**Model Class:** `TblVw3pbill`  
**Column Count:** 42  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Descript |
| 3 | Billdatecriteria |
| 4 | PayDefaultsubmit |
| 5 | ScrubError |
| 6 | DsId |
| 7 | DsClt |
| 8 | DsTxtSrv |
| 9 | DsDtStart |
| 10 | DsDtEnd |
| 11 | DsTxtType |
| 12 | DsdblUnits |
| 13 | BillUnits |
| 14 | DstxtStaff |
| 15 | Npi |
| 16 | Dsbilled |
| 17 | PyPayerid |
| 18 | PySubsid |
| 19 | PyGroup |
| 20 | Cptcode |
| 21 | Charge |
| 22 | TpaAuthCode |
| 23 | Clientname |
| 24 | CltDob |
| 25 | CltGender |
| 26 | CltAdd1 |
| 27 | CltCity |
| 28 | CltState |
| 29 | Cltzip |
| 30 | CltPhone |
| 31 | CltMarry |
| 32 | CltM4id |
| 33 | Dsdiag |
| 34 | Modifier |
| 35 | DsPos |
| 36 | Ndc |
| 37 | Mg |
| 38 | SiteId |
| 39 | Dsarea |
| 40 | Payclass |
| 41 | RowState |
| 42 | LastModAt |

### pats.tbl_vw3pBillSub
**Model Class:** `Tblvw3pBillSub`  
**Column Count:** 44  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Descript |
| 3 | Billdatecriteria |
| 4 | PayDefaultsubmit |
| 5 | ScrubError |
| 6 | DsId |
| 7 | DsClt |
| 8 | DsTxtSrv |
| 9 | DsDtStart |
| 10 | DsDtEnd |
| 11 | DsTxtType |
| 12 | DsdblUnits |
| 13 | BillUnits |
| 14 | DstxtStaff |
| 15 | Npi |
| 16 | Dsbilled |
| 17 | PyPayerid |
| 18 | PySubsid |
| 19 | PyGroup |
| 20 | Cptcode |
| 21 | Charge |
| 22 | TpaAuthCode |
| 23 | Clientname |
| 24 | CltDob |
| 25 | CltGender |
| 26 | CltAdd1 |
| 27 | CltCity |
| 28 | CltState |
| 29 | Cltzip |
| 30 | CltPhone |
| 31 | CltMarry |
| 32 | CltM4id |
| 33 | Dsdiag |
| 34 | Modifier |
| 35 | DsPos |
| 36 | Ndc |
| 37 | Mg |
| 38 | SiteId |
| 39 | Dsarea |
| 40 | Payclass |
| 41 | LastModAt |
| 42 | RowState |
| 43 | RowChkSum |
| 44 | CptMod |

## Schema: ctrl — Control / Configuration Tables

---

### ctrl.tbl_3PSETUP
**Model Class:** `Tbl3psetup`  
**Column Count:** 30  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | _pId |
| 3 | Clinic |
| 4 | Address |
| 5 | State |
| 6 | Zip |
| 7 | Npi |
| 8 | TaxId |
| 9 | Medicaid |
| 10 | City |
| 11 | Drlname |
| 12 | Drfname |
| 13 | Drnpi |
| 14 | ProviderAddress |
| 15 | ProviderCity |
| 16 | ProviderName |
| 17 | ProviderPhone |
| 18 | ProviderState |
| 19 | ProviderZip |
| 20 | SiteId |
| 21 | Clia |
| 22 | StrDbnotes |
| 23 | ProviderDesc |
| 24 | BlHasPreloader |
| 25 | IndividualNpi |
| 26 | Taxonomy |
| 27 | Sftpun |
| 28 | Sftppw |
| 29 | RowChkSum |
| 30 | LastModAt |

### ctrl.tbl_claimstatus
**Model Class:** `TblClaimStatus`  
**Column Count:** 8  

| # | Column Name |
|---|-------------|
| 1 | id |
| 2 | DatabaseName |
| 3 | tpcbID |
| 4 | tpcbDtCreated |
| 5 | tpcbStrSubmitType |
| 6 | tpcb837 |
| 7 | tpcbFILE |
| 8 | FileUploadStatus |

### ctrl.tbl_CLINIC
**Model Class:** `TblClinic`  
**Column Count:** 238  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Pkey |
| 3 | DoseWarn |
| 4 | DoseStop |
| 5 | Photos |
| 6 | Bottles |
| 7 | Overdue |
| 8 | BillHold |
| 9 | Test |
| 10 | Tbtest |
| 11 | Force |
| 12 | LastUpdated |
| 13 | Note |
| 14 | Provider |
| 15 | Site |
| 16 | Cliniccode |
| 17 | DischargeGuest |
| 18 | NumInventory |
| 19 | Schedua |
| 20 | Uamonthly |
| 21 | ClinicName |
| 22 | Tpautomation |
| 23 | RequireDarts |
| 24 | PhysicalTestDays |
| 25 | AsphysicalTest |
| 26 | ServiceOverlapPopup |
| 27 | OrangeandWhite |
| 28 | ToxProvider |
| 29 | NumberofReceipts |
| 30 | PasswordEnforce |
| 31 | PasswordLength |
| 32 | Helpfile |
| 33 | ScanPath |
| 34 | DateSigStart |
| 35 | ElecSigs |
| 36 | CreditPriorWeek |
| 37 | DefaultTabOrange |
| 38 | BottleWeight |
| 39 | SpGravity |
| 40 | DoseCharge |
| 41 | TimeOffset |
| 42 | CoSign |
| 43 | DefaultProgram |
| 44 | ClientSecurity |
| 45 | AutoCheckin |
| 46 | CheckinCheck |
| 47 | OrderService |
| 48 | Residential |
| 49 | BillDirection |
| 50 | SmallReceipts |
| 51 | DuplicateCheckinCheck |
| 52 | NoPrintCheckinLabel |
| 53 | AdDomain |
| 54 | AutoSetLabelprinter |
| 55 | AutoSetReceiptPrinter |
| 56 | ClinicLetter |
| 57 | ClinicState |
| 58 | Liquid |
| 59 | OtherInvType |
| 60 | PrintDoseAmt |
| 61 | Tabs |
| 62 | DailyServices |
| 63 | ClientsearchRin |
| 64 | ClientServiceBilling |
| 65 | DischargeClearsHolds |
| 66 | DrugFreeOnly |
| 67 | Halfweekcredit |
| 68 | AllowRinZero |
| 69 | AllowAnyRin |
| 70 | DefaultShowHoldAtNursing |
| 71 | HideElecSigDates |
| 72 | QueSearch |
| 73 | EducFieldIsEmpStatus |
| 74 | AutoImportUa |
| 75 | FastDose |
| 76 | RecIdprint |
| 77 | NurseSig |
| 78 | Order2confirm |
| 79 | Orderconfirm |
| 80 | ToxAcct |
| 81 | SpGravityClear |
| 82 | Toxtixnum |
| 83 | ToxTixspecial |
| 84 | AutoOrderExpirationHolds |
| 85 | Reqallintake |
| 86 | NumberOfBulkLabels |
| 87 | UaonVisit |
| 88 | DiversionPadding |
| 89 | Wordpath |
| 90 | ScanDeleteOriginal |
| 91 | UdspanelRequired |
| 92 | AutoDischargeCredit |
| 93 | BeakerColors |
| 94 | NumberPriorTransactionsOnReceipt |
| 95 | AlwaysAllowUseSavedSignature |
| 96 | NewBottleLabels |
| 97 | DocTemplatePath |
| 98 | ReportDir |
| 99 | ReportServer |
| 100 | DonotallowCascade |
| 101 | IsBhg |
| 102 | MultipleQueues |
| 103 | LabAcct |
| 104 | AlwaysAskBagLabel |
| 105 | PrepackBagLabelDefault |
| 106 | DefaultShowHoldFront |
| 107 | ShowFutureUaholds |
| 108 | OpenOnSunday |
| 109 | ChargeBeforeDose |
| 110 | LandscapeLabel |
| 111 | SigImgpath |
| 112 | SigImguri |
| 113 | SignBeforeDose |
| 114 | SortClientSearchbyId |
| 115 | Uapath |
| 116 | AdjustmentEmail |
| 117 | BlAdjustatDischarge |
| 118 | FifoBottle |
| 119 | UseCostCenter |
| 120 | ForceCheckin |
| 121 | VerifyMedAdjustment |
| 122 | PinSigs |
| 123 | Combine3payfees |
| 124 | PinBeforeSig |
| 125 | Siglcd |
| 126 | DictionaryPath |
| 127 | Grammerpath |
| 128 | DiversionType |
| 129 | Ismedmark |
| 130 | ServiceDimsLinkToTp |
| 131 | Advancedtesting |
| 132 | AllowActOldOrder |
| 133 | FirstInitialonToxlabel |
| 134 | Over100check |
| 135 | NoQuePop |
| 136 | Offsetdoseconfirm |
| 137 | OrderRequestsNeedBothSigs |
| 138 | SmallTox |
| 139 | Toxservice |
| 140 | Zebra |
| 141 | FingerPrintSig |
| 142 | Voregistrationpath |
| 143 | Blockaptcalhold |
| 144 | CalendarStartTime |
| 145 | EligPw |
| 146 | EligUn |
| 147 | FiveDayCalendarWeek |
| 148 | Multitenant |
| 149 | Pumpwindow |
| 150 | RequireEmergencyContact |
| 151 | QueueTwice |
| 152 | CheckUaisPrescription |
| 153 | EnableBusPass |
| 154 | ClaimDir |
| 155 | IsIhc |
| 156 | Phase |
| 157 | SetEvalsOtherFocus |
| 158 | EnableHoldayPickupCalifornia |
| 159 | ZeroSsns |
| 160 | EnableTouchSig |
| 161 | CreditDosesDischarge |
| 162 | AllowBulkDrSigs |
| 163 | EnableAlertsMedChanges |
| 164 | EnableOrderAlerts |
| 165 | EnableTestingAlerts |
| 166 | EnableAtRiskAlerts |
| 167 | EnableAdministeringClientMeds |
| 168 | DisableServiceUnits |
| 169 | IsMultiProgram |
| 170 | VersionNbr |
| 171 | LabelPrintMedTypeInsteadOfMedClass |
| 172 | EnableEnrollDischargeDateInSearchGrid |
| 173 | EnableServiceRevisions |
| 174 | EnableBac |
| 175 | SammsFormsDefaultIndexNumber |
| 176 | EnableDriveMapping |
| 177 | Destructbottle |
| 178 | Dontprintorders |
| 179 | DisablePrintServiceMessageAfterSavePrompt |
| 180 | DisableOtherAsReferralSource |
| 181 | EnableInventory4and5 |
| 182 | SigPadTest |
| 183 | EnableRssAlerts |
| 184 | Iispath |
| 185 | PrintAlternativeZebraAndDymoLabelVersion1 |
| 186 | IsRnp |
| 187 | EnableAutoPopulateCity |
| 188 | IntakePacketUrl |
| 189 | NoCheckinatPay |
| 190 | EnableAutoHoldOnAbnormalLab |
| 191 | MultiQueueRefreshIntervalTimeSet |
| 192 | EnableSuffixMiddleInitialInFirstNameOfSearch |
| 193 | EnableUserLoginAtBacqueueModeElseInitials |
| 194 | SiteId |
| 195 | PrintSitesAddressDependingOnSites |
| 196 | DoNotPrintDoe |
| 197 | EnableAutoSpSammsbilling |
| 198 | EnableCounselorSelectionInMultiProgramSectionOnly |
| 199 | Urlassessment |
| 200 | DisableTpproblemAndInOwnWords |
| 201 | EnableCustomizableRequirementsForClientInfo |
| 202 | EnableIntakeDischargeIncomeInputs |
| 203 | EnableAutoBillingDuringEachToxPrint |
| 204 | IsSh |
| 205 | EnableBacstopDose |
| 206 | EnableBacnurseHoldEvenBlowZero |
| 207 | EnableSignaturesDuringPillCount |
| 208 | EnableSignatureWhenAdministeringMeds |
| 209 | Chsamsid |
| 210 | Fts |
| 211 | Over20 |
| 212 | Forcefindtype |
| 213 | HnPurl |
| 214 | EnablePrintMedTypeColorOnIdcard |
| 215 | EnablePortraitLabelDoubleSide |
| 216 | EnableCommentsOnMultiCheckin |
| 217 | PullPicsFromDb |
| 218 | EnableCompetentCheckBoxAtDosing |
| 219 | EnableActivateOrderWhenNotInSuboxoneProg |
| 220 | EnablePrintToxLandscape |
| 221 | EnableFlagNurseForBac |
| 222 | BottleReturnNote |
| 223 | BhgmarginTh |
| 224 | BhgmarginTox |
| 225 | NoCheckinService |
| 226 | NoSammsformHeader |
| 227 | PrintUnitDoseLabel |
| 228 | AuthBasedOnProgram |
| 229 | LandscapeZebra |
| 230 | ShowBalanceAtDispense |
| 231 | SingleQueueRefreshIntervalTimeSet |
| 232 | MultiDosingClinic |
| 233 | BlasterWide |
| 234 | PumpCalibrate |
| 235 | CheckVisitingPatient |
| 236 | RequireClientSignatureOrderRequest |
| 237 | DischargedAllowAddPayer |
| 238 | DymoDetailed |

### ctrl.tbl_CODES
**Model Class:** `TblCodes`  
**Column Count:** 39  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | RowChkSum |
| 3 | LastModAt |
| 4 | CdeId |
| 5 | CdeGroup |
| 6 | CdeDesc |
| 7 | CdeBillable |
| 8 | CdeUa |
| 9 | CdeIntAmt |
| 10 | CdeLiquid |
| 11 | CdeStaffcode |
| 12 | CdeFund |
| 13 | CdeModality |
| 14 | CdeDrugfree |
| 15 | CdeProvider |
| 16 | CdeSiteNum |
| 17 | Rowguid |
| 18 | CdeBillableResidential |
| 19 | CdeServiceSetting |
| 20 | CdeDischargeType |
| 21 | CdeSigRequired |
| 22 | CdeResidential |
| 23 | CdeAllowOverlap |
| 24 | DuiAmt |
| 25 | DuiHourRate |
| 26 | BlDefault |
| 27 | WeeklyFee |
| 28 | MustHaveBilling |
| 29 | Suboxoneprog |
| 30 | CdeInsurance |
| 31 | DefRate |
| 32 | SiteId |
| 33 | Cdelblcolor |
| 34 | Cde3pdonotbill |
| 35 | Cde3pPosoverride |
| 36 | IsPrescreening |
| 37 | Obat |
| 38 | ReqAuth |
| 39 | IntakeProg |

### ctrl.tbl_COLS
**Model Class:** `TblCols`  
**Column Count:** 2  

| # | Column Name |
|---|-------------|
| 1 | ColName |
| 2 | ColID |

### ctrl.tbl_CONNECTIONS
**Model Class:** `TblConnections`  
**Column Count:** 8  

| # | Column Name |
|---|-------------|
| 1 | ConId |
| 2 | ConType |
| 3 | ConStr |
| 4 | ConName |
| 5 | UserName |
| 6 | Password |
| 7 | LastModBy |
| 8 | LastModAt |

### ctrl.tbl_CONSENTS
**Model Class:** `TblConsents`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | Cid |
| 2 | CName |
| 3 | ClientSig |
| 4 | StaffSig |
| 5 | SupervisorSig |
| 6 | PhysicianSig |
| 7 | NurseSig |
| 8 | GuardianSig |
| 9 | DenyGuardian |
| 10 | CDeleted |
| 11 | Cdays |
| 12 | Bac |
| 13 | Ted |
| 14 | AdminnurseSig |
| 15 | Blrecurr |
| 16 | IsMhform |

### ctrl.tbl_CONSENTS_PHC
**Model Class:** `TblConsents_Phc`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | Cid |
| 2 | CName |
| 3 | ClientSig |
| 4 | StaffSig |
| 5 | SupervisorSig |
| 6 | PhysicianSig |
| 7 | NurseSig |
| 8 | GuardianSig |
| 9 | DenyGuardian |
| 10 | CDeleted |
| 11 | Cdays |
| 12 | Bac |
| 13 | Ted |
| 14 | AdminnurseSig |
| 15 | Blrecurr |
| 16 | IsMhform |

### ctrl.tbl_COWXREF
**Model Class:** `TblCowxref`  
**Column Count:** 3  

| # | Column Name |
|---|-------------|
| 1 | ColumnName |
| 2 | PermissibleValue |
| 3 | DescripiveText |

### ctrl.tbl_DroDownListItems
**Model Class:** `TblDropDownListItems`  
**Column Count:** 5  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Id |
| 3 | DropDownListItem |
| 4 | DropDownListId |
| 5 | ddapcode |

### ctrl.tbl_Forms2Process
**Model Class:** `TblForms2Process`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | FormProcessId |
| 2 | Enabled |
| 3 | RowState |
| 4 | DateFilterEnabled |
| 5 | FormName |
| 6 | TableName |
| 7 | CompletedBy |
| 8 | Counselor |
| 9 | Doctor |
| 10 | MedicalProvider |
| 11 | Patient |
| 12 | Provider |
| 13 | Requestor |
| 14 | Staff |
| 15 | Supervisor |
| 16 | Prefix |
| 17 | CreatedOn |
| 18 | ModifiedOn |

### ctrl.tbl_GlobalDevices
**Model Class:** `TblGlobalDevices`  
**Column Count:** 15  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | DId |
| 3 | DDeviceid |
| 4 | DSid |
| 5 | DPumpnum |
| 6 | DCheckin |
| 7 | DPumptype |
| 8 | DTestmode |
| 9 | DLabel |
| 10 | DReceipt |
| 11 | DSigpad |
| 12 | DDispense |
| 13 | DFingerprint |
| 14 | DTouchScreen |
| 15 | DBacqueuePc |

### ctrl.tbl_INVTYPE
**Model Class:** `TblInvtype`  
**Column Count:** 20  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Invid |
| 3 | RowChkSum |
| 4 | LastModAt |
| 5 | RowState |
| 6 | InvName |
| 7 | InvLiquid |
| 8 | InvUnit |
| 9 | InvTotal |
| 10 | InvDivision |
| 11 | DefaultMed |
| 12 | DisplayName |
| 13 | InvMedclass |
| 14 | IsFilm |
| 15 | Type |
| 16 | HasBeaker |
| 17 | InvActual |
| 18 | InvLabelName |
| 19 | InvNdc |
| 20 | InvJcode |

### ctrl.tbl_LocationCmds
**Model Class:** `TblLocationCmds`  
**Column Count:** 8  

| # | Column Name |
|---|-------------|
| 1 | CmdId |
| 2 | SiteCode |
| 3 | CmdType |
| 4 | CmdOrder |
| 5 | ConId |
| 6 | StepKey |
| 7 | CmdCtrl |
| 8 | CmdStr |

### ctrl.tbl_LocationCons
**Model Class:** `TblLocationCons`  
**Column Count:** 6  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | EffectiveDate |
| 3 | ConnectionId |
| 4 | DbName |
| 5 | ActionKey |
| 6 | SchemaVersion |

### ctrl.tbl_Locations
**Model Class:** `TblLocations`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | ClinicName |
| 3 | RegionCode |
| 4 | IsActive |
| 5 | SiteClinic |
| 6 | Location |
| 7 | StateCode |
| 8 | ZipCode |
| 9 | Latitude |
| 10 | Longitude |
| 11 | ContractDate |
| 12 | EnrollCutoff |
| 13 | SammstrxDate |
| 14 | AltRegion |
| 15 | AcctCmpyId |
| 16 | SId |
| 17 | IsNewSchema |
| 18 | VpregionCode |

### ctrl.tbl_SiteTableInit
**Model Class:** `TblCtrlSiteTableInit`  
**Column Count:** 6  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | TableName |
| 3 | DateField |
| 4 | DateNulls |
| 5 | MinDate |
| 6 | InitRunDate |

### ctrl.tbl_SiteTableInitLog
**Model Class:** `TblCtrlSiteTableInitLog`  
**Column Count:** 5  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | TableName |
| 3 | WorkDate |
| 4 | RunDate |
| 5 | TranRows |

### ctrl.tbl_USER
**Model Class:** `TblUser`  
**Column Count:** 33  

| # | Column Name |
|---|-------------|
| 1 | Uskey |
| 2 | UsrName |
| 3 | UsrPassword |
| 4 | UsrGroups |
| 5 | UsrDescription |
| 6 | UsrFname |
| 7 | UsrLname |
| 8 | UsrSsn |
| 9 | Usrcred |
| 10 | UsrActive |
| 11 | UsrPasswordChanged |
| 12 | UsrSignature |
| 13 | UsrTemplate |
| 14 | Usrtemplatechanged |
| 15 | UsrCounselor |
| 16 | UsrPin |
| 17 | UsrCosig |
| 18 | UsrSignatureImage |
| 19 | UsrSuper |
| 20 | IsDasacounselor |
| 21 | UsrLocation |
| 22 | UsrRole |
| 23 | EmailId |
| 24 | Userfullname |
| 25 | UsrCalendarUser |
| 26 | UsrDea |
| 27 | UsrDoctor |
| 28 | UsrLicensed |
| 29 | Usrnpi |
| 30 | Usrphone |
| 31 | Usrxdea |
| 32 | UsrExt |
| 33 | UsrTaxonomy |

### ctrl.tbl_USERSITES
**Model Class:** `TblUserSites`  
**Column Count:** 4  

| # | Column Name |
|---|-------------|
| 1 | UsId |
| 2 | UsName |
| 3 | UsSite |
| 4 | UsDefault |

### ctrl.tbl_XREF
**Model Class:** `TblXref`  
**Column Count:** 7  

| # | Column Name |
|---|-------------|
| 1 | Xref |
| 2 | DisplayOrder |
| 3 | Code |
| 4 | LastModBy |
| 5 | LastModAt |
| 6 | Descrptn |
| 7 | ParentXref |

## Schema: ayx — Analytics / Alteryx Tables

---

### ayx.tbl_PreAdmission_V6
**Model Class:** `TblPreAdmissionV6`  
**Column Count:** 57  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | PreAdmissionid |
| 3 | Clientid |
| 4 | cltM4ID |
| 5 | CreatedOn |
| 6 | Createdby |
| 7 | PreAdmissionDate |
| 8 | RegistrationMode |
| 9 | ReferralSourcedesc |
| 10 | PrimaryReferralSourceNote |
| 11 | Program |
| 12 | InsuranceType |
| 13 | IntakeProgram |
| 14 | IntakeProgramDate |
| 15 | IsCurrentlyInOpiateProgram |
| 16 | IsPatientAtPainManagementClinic |
| 17 | IsHavingLegalPrescription |
| 18 | IsAnyLegalPrescriptionForPain |
| 19 | IsAnyOngoingMedicalCondition |
| 20 | IsSuicidalThoughtWithin72Hours |
| 21 | IsHavingPlanForHowToCommitSuicide |
| 22 | IsHomicidalThoughtWithin72Hours |
| 23 | IsRecentlyReleasedFromPenal |
| 24 | IsSpecialAccommodationRequired |
| 25 | ReasonSeekingTreatment |
| 26 | AccomodationNeeded |
| 27 | ClientAddress |
| 28 | Comments |
| 29 | IsPatientAdmitted |
| 30 | AreYouCurrentlyPregnant |
| 31 | BringIdproof |
| 32 | BringInsuranceCard |
| 33 | ClinicInfo |
| 34 | CurrntlyRecevingTreatmentForCondition |
| 35 | IsAnyPrescriptionForPain |
| 36 | IsInsurance |
| 37 | IsOverTheCounterMedications |
| 38 | ImmediateAssessment |
| 39 | ImmediateAssessment911 |
| 40 | MedicalConditionsProviderName1 |
| 41 | MedicalConditionsProviderPhone1 |
| 42 | MedicalConditionsProviderName2 |
| 43 | MedicalConditionsProviderPhone2 |
| 44 | PlanOfSuicide |
| 45 | PlanOnSpendingTimeAtClinic |
| 46 | Sammsprogram |
| 47 | OfficeUseWhy |
| 48 | OngoingMedicalConditionsWha |
| 49 | PreAddAddress |
| 50 | LastUpdatedBy |
| 51 | LastUpdateOn |
| 52 | PatientSignatureDate |
| 53 | DateofRelease |
| 54 | Version |
| 55 | RowChkSum |
| 56 | EtllastModAt |
| 57 | RowState |

## Schema: stg — Staging Tables

---

### stg.tbl_FormsCounts
**Model Class:** `TblFormsCounts`  
**Column Count:** 5  

| # | Column Name |
|---|-------------|
| 1 | FscDate |
| 2 | SiteCode |
| 3 | fscsid |
| 4 | fscCltID |
| 5 | Cnt |

## Schema: tsk — Task Management Tables

---

### tsk.tbl_RowTrax
**Model Class:** `TblRowTrax`  
**Column Count:** 6  

| # | Column Name |
|---|-------------|
| 1 | SiteCode |
| 2 | Rcdate |
| 3 | TblName |
| 4 | SammsCnt |
| 5 | AzureCnt |
| 6 | LastModAt |

## Schema: other — Other Tables

---

### unknown.TblMapSrc2Dsn
**Model Class:** `TblMapSrc2Dsn`  
**Column Count:** 16  

| # | Column Name |
|---|-------------|
| 1 | ActionKey |
| 2 | ActionStepKey |
| 3 | FieldKey |
| 4 | FieldName |
| 5 | Enabled |
| 6 | PrimaryKey |
| 7 | FieldType |
| 8 | FieldLength |
| 9 | FieldPrecision |
| 10 | FieldScale |
| 11 | LastModAt |
| 12 | LastModBy |
| 13 | DsnFieldName |
| 14 | Nullable |
| 15 | Default |
| 16 | FormatConvert |

### unknown.TblSchedule
**Model Class:** `TblSchedule`  
**Column Count:** 11  

| # | Column Name |
|---|-------------|
| 1 | ScheduleId |
| 2 | Enabled |
| 3 | RowState |
| 4 | LastModBy |
| 5 | LastModAt |
| 6 | Name |
| 7 | TriggerKey |
| 8 | ActionKey |
| 9 | NextRunTime |
| 10 | LastRunTime |
| 11 | LastOpResult |

### unknown.TblTasks
**Model Class:** `TblTasks`  
**Column Count:** 25  

| # | Column Name |
|---|-------------|
| 1 | TaskId |
| 2 | TaskName |
| 3 | RunAt |
| 4 | ActionKey |
| 5 | ActionStepKey |
| 6 | Status |
| 7 | Duration |
| 8 | OnCompletion |
| 9 | OnError |
| 10 | LastModAt |
| 11 | LastModBy |
| 12 | RowState |
| 13 | DependentTaskId |
| 14 | ParentTaskId |
| 15 | SiteCode |
| 16 | WorkDate |
| 17 | RowCount |
| 18 | ErrorMessage |
| 19 | RowsIns |
| 20 | RowsUpd |
| 21 | Reload |
| 22 | IsResult |
| 23 | RowsProcessed |
| 24 | ExceptMsg |
| 25 | ExceptInnerMsg |

### unknown.TblTriggers
**Model Class:** `TblTriggers`  
**Column Count:** 18  

| # | Column Name |
|---|-------------|
| 1 | TriggerKey |
| 2 | LastModBy |
| 3 | LastModAt |
| 4 | RowState |
| 5 | TriggerName |
| 6 | TriggerEnabled |
| 7 | TaskType |
| 8 | TrgSetting |
| 9 | TrgStartTime |
| 10 | TrgRecur |
| 11 | TrgRecurPrd |
| 12 | TrgExpire |
| 13 | TrgWklyDays |
| 14 | TrgMmonths |
| 15 | TrgMdays |
| 16 | TrgDelay |
| 17 | TrgRepeat |
| 18 | TrgRepeatDuration |

