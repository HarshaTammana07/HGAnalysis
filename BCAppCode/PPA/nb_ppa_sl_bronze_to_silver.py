from pyspark.sql import SparkSession, functions as F
import json
spark = SparkSession.builder.getOrCreate()

METHOD_NAME = "SFPatientPreAdmission"
DEFAULT_BRONZE_TABLE = "bhg_bronze.PatientPreAdmission.br_SF_PatientPreAdmission"
DEFAULT_SILVER_TABLE = "bhg_silver.pats.tbl_SF_PatientPreAdmission"
DEFAULT_INGEST_COLUMN = "IngestRunId"
DEFAULT_SITE_COLUMN = "SiteCode"
DEFAULT_DATABASE_COLUMN = "SourceDatabase"
MATCH_KEYS = ["SiteCode", "ID"]
BRONZE_METADATA_COLUMNS = {
    "SourceDatabase",
    "IngestRunId",
    "ExtractedAt",
    "SourceQueryStartDate",
    "SourceQueryEndDate",
    "_source_query_date_anchor"
}

try:
    p_audit_context_json
except NameError:
    p_audit_context_json = "{}"

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = None

try:
    p_bronze_method_results_json
except NameError:
    p_bronze_method_results_json = None


def parse_json(value, default):
    if value is None:
        return default
    if isinstance(value, (dict, list)):
        return value
    text = str(value).strip()
    if not text:
        return default
    try:
        return json.loads(text)
    except Exception:
        return default


def bronze_result():
    parsed = parse_json(p_bronze_method_results_json, {})
    result = parsed.get(METHOD_NAME) if isinstance(parsed, dict) else {}
    return result if isinstance(result, dict) else {}


def bronze_had_method_failure():
    result = bronze_result()
    status = str(result.get("status") or "").upper()
    failed_stage = str(result.get("failed_stage") or "").upper()
    return status in ("FAILED", "ERROR") or failed_stage == "BR"


def table_exists(table_name):
    try:
        return spark.catalog.tableExists(table_name)
    except Exception:
        try:
            spark.table(table_name).limit(1).count()
            return True
        except Exception:
            return False


def request_body(task):
    body = task.get("request_body") or task.get("RequestBody") or "{}"
    return parse_json(body, {})


def method_of(task):
    return str(task.get("method") or task.get("Method") or "").strip()


def layer_tasks(ctx, layer):
    section = ctx.get(layer) or {}
    tasks = section.get("tasks") or []
    if not tasks and section.get("task_config_id"):
        tasks = [section]
    return tasks


def first_method_task(tasks, method_name):
    for task in tasks:
        if method_of(task).lower() == method_name.lower():
            return task
    return tasks[0] if tasks else {}


def actual_col(df, wanted, required=False):
    by_lower = {c.lower(): c for c in df.columns}
    found = by_lower.get(wanted.lower())
    if required and not found:
        raise Exception(f"Column {wanted} was not found. Available columns: {df.columns}")
    return found


def col_or_null(df, wanted):
    found = actual_col(df, wanted, required=False)
    return F.col(found) if found else F.lit(None)


def to_pascal_column(name):
    if not name:
        return name
    if name[0].isupper():
        return name
    return name[0].upper() + name[1:]


def target_type_lookup(configured_target_schema):
    return {str(k).lower(): str(v) for k, v in (configured_target_schema or {}).items()}


def align_to_target(src_df, configured_target_columns, configured_target_schema, bronze_source_by_target=None):
    cols = configured_target_columns or list((configured_target_schema or {}).keys())
    target_schema = target_type_lookup(configured_target_schema)
    bronze_source_by_target = bronze_source_by_target or {}
    exprs = []
    for target_col in cols:
        source_candidates = [
            bronze_source_by_target.get(target_col),
            target_col,
        ]
        source_col = None
        for candidate in source_candidates:
            if not candidate:
                continue
            source_col = actual_col(src_df, candidate, required=False)
            if source_col:
                break
        expr = F.col(source_col) if source_col else F.lit(None)
        target_type = target_schema.get(target_col.lower())
        if target_type:
            expr = expr.cast(target_type)
        exprs.append(expr.alias(target_col))
    return src_df.select(*exprs)


BRONZE_SOURCE_BY_TARGET = {
    "SiteCode": "SiteCode",
    "ID": "ID",
    "PatientID": "PatientID",
    "RegistrationModeID": "RegistrationModeID",
    "IsIntakeAppointmentScheduled": "IsIntakeAppointmentScheduled",
    "IntakeAppointmentScheduledDateTime": "IntakeAppointmentScheduledDateTime",
    "ReferralSourceID": "ReferralSourceID",
    "ProgramID": "ProgramID",
    "OpiatesUsageNoOfYears": "OpiatesUsageNoOfYears",
    "OpiatesUsageNoOfMonths": "OpiatesUsageNoOfMonths",
    "DrugChoiceID": "DrugChoiceID",
    "DrugAdministrationTypeID": "DrugAdministrationTypeID",
    "IllicitSubstanceOther": "IllicitSubstanceOther",
    "IllicitSubstanceOtherMgPerDay": "IllicitSubstanceOtherMgPerDay",
    "IsCurrentlyInOpiateProgram": "IsCurrentlyInOpiateProgram",
    "CurrentOpiateProgramLocation": "CurrentOpiateProgramLocation",
    "CurrentOpiateProgramNoOfYears": "CurrentOpiateProgramNoOfYears",
    "CurrentOpiateProgramNoOfMonths": "CurrentOpiateProgramNoOfMonths",
    "CurrentOpiateProgramDose": "CurrentOpiateProgramDose",
    "IsPatientAtPainManagementClinic": "IsPatientAtPainManagementClinic",
    "PainManagementClinicLocation": "PainManagementClinicLocation",
    "PainManagementClinicFromDate": "PainManagementClinicFromDate",
    "PainManagementClinicToDate": "PainManagementClinicToDate",
    "IsHavingLegalPrescription": "IsHavingLegalPrescription",
    "IsAnyLegalPrescriptionForPain": "IsAnyLegalPrescriptionForPain",
    "IsAnyOngoingMedicalCondition": "IsAnyOngoingMedicalCondition",
    "IsCurrentlyPregnant": "IsCurrentlyPregnant",
    "IsSuicidalThoughtWithin72Hours": "IsSuicidalThoughtWithin72Hours",
    "SuicidalThoughtIntentScale": "SuicidalThoughtIntentScale",
    "IsHavingPlanForHowToCommitSuicide": "IsHavingPlanForHowToCommitSuicide",
    "IsHomicidalThoughtWithin72Hours": "IsHomicidalThoughtWithin72Hours",
    "IsRecentlyReleasedFromPenal": "IsRecentlyReleasedFromPenal",
    "ReleasedFromPenalDate": "ReleasedFromPenalDate",
    "IsSpecialAccommodationRequired": "IsSpecialAccommodationRequired",
    "IsAbleToAttendTreatementCenterDaily": "IsAbleToAttendTreatementCenterDaily",
    "IsAbleToPayIntakeAndWeeklyFee": "IsAbleToPayIntakeAndWeeklyFee",
    "IsEligibleForFurtherAssessment": "IsEligibleForFurtherAssessment",
    "IsReferralOffered": "IsReferralOffered",
    "ReferralOfferedList": "ReferralOfferedList",
    "RejectedReferralOffered": "RejectedReferralOffered",
    "ReasonSeekingTreatment": "ReasonSeekingTreatment",
    "IsPatientAdmitted": "IsPatientAdmitted",
    "IsParticipitatingInOtherOpioidProgram": "IsParticipitatingInOtherOpioidProgram",
    "IsPresentedWithoutIDProof": "IsPresentedWithoutIDProof",
    "IsPresentedAtCloseOfDay": "IsPresentedAtCloseOfDay",
    "IsRequestedAdmissionDrUnavailable": "IsRequestedAdmissionDrUnavailable",
    "RequestedAdmissionDay": "RequestedAdmissionDay",
    "RequestedAdmissionAMOrPM": "RequestedAdmissionAMOrPM",
    "IsUnableToPay": "IsUnableToPay",
    "IsUnableToProvideEvidenceofAddiction": "IsUnableToProvideEvidenceofAddiction",
    "IsAddictionToUnstableNeeding": "IsAddictionToUnstableNeeding",
    "IsImpairment": "IsImpairment",
    "IsMedicalProblemsNeedingStabilization": "IsMedicalProblemsNeedingStabilization",
    "IsPsychiatricProblemNeedingInpatientTreatment": "IsPsychiatricProblemNeedingInpatientTreatment",
    "IsServiceDeclined": "IsServiceDeclined",
    "IsOther": "IsOther",
    "OtherDescription": "OtherDescription",
    "Comments": "Comments",
    "Active": "Active",
    "CreatedBy": "CreatedBy",
    "CreatedOn": "CreatedOn",
    "LastUpdatedBy": "LastUpdatedBy",
    "LastUpdateOn": "LastUpdateOn",
    "PreAdmissionDate": "PreAdmissionDate",
    "LegalPrescription1": "LegalPrescription1",
    "LegalPrescription2": "LegalPrescription2",
    "LegalPrescription3": "LegalPrescription3",
    "MedicalCondition1": "MedicalCondition1",
    "MedicalCondition2": "MedicalCondition2",
    "MedicalCondition3": "MedicalCondition3",
    "DrugofchoiceAdministered": "DrugofchoiceAdministered",
    "RequestedAdmissionDayFri": "requestedAdmissionDayFri",
    "RequestedAdmissionDayMon": "requestedAdmissionDayMon",
    "RequestedAdmissionDayThu": "requestedAdmissionDayThu",
    "RequestedAdmissionDayTue": "requestedAdmissionDayTue",
    "RequestedAdmissionDayWed": "requestedAdmissionDayWed",
    "AccomodationNeeded": "AccomodationNeeded",
    "AcknowledgeClientSignature": "AcknowledgeClientSignature",
    "AcknowledgeClientSignatureDate": "AcknowledgeClientSignatureDate",
    "AcknowledgeClientSignatureDateP": "AcknowledgeClientSignatureDateP",
    "AcknowledgeClientSignatureP": "AcknowledgeClientSignatureP",
    "AcknowledgeWitnessSignature": "AcknowledgeWitnessSignature",
    "AcknowledgeWitnessSignatureDate": "AcknowledgeWitnessSignatureDate",
    "AcknowledgeWitnessSignatureDateP": "AcknowledgeWitnessSignatureDateP",
    "AcknowledgeWitnessSignatureP": "AcknowledgeWitnessSignatureP",
    "ActiveP": "ActiveP",
    "AlcoholAmount": "AlcoholAmount",
    "AlcoholLastDrink": "AlcoholLastDrink",
    "ApplicantName": "ApplicantName",
    "AreYouCurrentlyPregnant": "AreYouCurrentlyPregnant",
    "Biopsychosocialassessment": "Biopsychosocialassessment",
    "BiopsychosocialassessmentP": "BiopsychosocialassessmentP",
    "BiopsychosocialText": "BiopsychosocialText",
    "BiopsychosocialTextP": "BiopsychosocialTextP",
    "BringIDProof": "BringIDProof",
    "BringInsuranceCard": "BringInsuranceCard",
    "ChronicMedicalCondition": "ChronicMedicalCondition",
    "ClientDetails": "ClientDetails",
    "ClientNameP": "ClientNameP",
    "ClinicInfo": "ClinicInfo",
    "Created": "Created",
    "CreatedByP": "CreatedByP",
    "CreatedP": "CreatedP",
    "CurrentlyMedicatedTreatmentOther": "CurrentlyMedicatedTreatmentOther",
    "CurrentOpiateProgramFrom": "CurrentOpiateProgramFrom",
    "CurrentOpiateProgramTo": "CurrentOpiateProgramTo",
    "CurrentOpiateWhatProgram": "CurrentOpiateWhatProgram",
    "CurrntlyRecevingTreatmentForCondition": "CurrntlyRecevingTreatmentForCondition",
    "DailyTime": "DailyTime",
    "DataFormId": "DataFormId",
    "Date": "Date",
    "DateP": "DateP",
    "Daysafterdischarge": "daysafterdischarge",
    "DaysafterdischargeP": "daysafterdischargeP",
    "DeniedDuetoCapacity": "DeniedDuetoCapacity",
    "Describeother": "describeother",
    "Describeother1": "describeother1",
    "Describeother1P": "describeother1P",
    "Describeother2": "describeother2",
    "Describeother2P": "describeother2P",
    "DescribeotherP": "describeotherP",
    "Diagnosis": "Diagnosis",
    "DiagnosisP": "DiagnosisP",
    "DiagnosisText": "DiagnosisText",
    "DiagnosisTextP": "DiagnosisTextP",
    "DischargeReasonsText": "DischargeReasonsText",
    "DischargeReasonsTextP": "DischargeReasonsTextP",
    "DischargeSummary": "DischargeSummary",
    "DischargeSummaryP": "DischargeSummaryP",
    "DischargeSummaryText": "DischargeSummaryText",
    "DischargeSummaryTextP": "DischargeSummaryTextP",
    "DoctorSignature": "DoctorSignature",
    "DoctorSignatureDate": "DoctorSignatureDate",
    "DropdownPresentlyInPainScale1to10": "dropdownPresentlyInPainScale1to10",
    "DrugApplicantTime": "DrugApplicantTime",
    "DrugDaily": "DrugDaily",
    "DrugLastUsed": "DrugLastUsed",
    "DrugTaken": "DrugTaken",
    "DrugUsing": "DrugUsing",
    "EnrollmentId": "EnrollmentId",
    "GynDoctorProviderName": "GynDoctorProviderName",
    "GynDoctorProviderPhone": "GynDoctorProviderPhone",
    "HasGynDoctor": "HasGynDoctor",
    "ImmediateAssessment": "ImmediateAssessment",
    "ImmediateAssessment911": "ImmediateAssessment911",
    "InformationToObtained": "InformationToObtained",
    "InformationToObtainedFax": "InformationToObtainedFax",
    "InformationToObtainedFaxP": "InformationToObtainedFaxP",
    "InformationToObtainedP": "InformationToObtainedP",
    "InformationToObtainedVerbal": "InformationToObtainedVerbal",
    "InformationToObtainedVerbalP": "InformationToObtainedVerbalP",
    "InformationToObtainedWritten": "InformationToObtainedWritten",
    "InformationToObtainedWrittenP": "InformationToObtainedWrittenP",
    "InitialSceeningParticipationText": "InitialSceeningParticipationText",
    "InitialSceeningParticipationTextP": "InitialSceeningParticipationTextP",
    "InitialScreeningSummaryText": "InitialScreeningSummaryText",
    "InitialScreeningSummaryTextP": "InitialScreeningSummaryTextP",
    "InsuranceDescription": "InsuranceDescription",
    "InsuranceType": "InsuranceType",
    "IntakeProgram": "IntakeProgram",
    "IntakeProgramDate": "IntakeProgramDate",
    "IsAllergies": "IsAllergies",
    "IsAmPm": "IsAmPm",
    "IsAnyPrescriptionForPain": "IsAnyPrescriptionForPain",
    "IsApplicantPregnant": "IsApplicantPregnant",
    "IsBehaviorallyunstabldangerous": "IsBehaviorallyunstabldangerous",
    "IsClinicAddress": "IsClinicAddress",
    "IsCSUAtFullCapacity": "IsCSUAtFullCapacity",
    "IsDeleted": "IsDeleted",
    "IsDetox": "IsDetox",
    "IsDidnotMeetMedicalNecessity": "IsDidnotMeetMedicalNecessity",
    "IsDrinkingAlcohol": "IsDrinkingAlcohol",
    "IsEVSCompleted": "IsEVSCompleted",
    "IsInsurance": "IsInsurance",
    "IsInsuranceAvailable": "IsInsuranceAvailable",
    "IsInsuranceCard": "IsInsuranceCard",
    "IsMaintenance": "IsMaintenance",
    "IsMedicalEmergency": "IsMedicalEmergency",
    "IsOverTheCounterMedications": "isOverTheCounterMedications",
    "IsPackets": "IsPackets",
    "IsPastTreatmentHistory": "IsPastTreatmentHistory",
    "IsPhysicalHealthUnstable": "IsPhysicalHealthUnstable",
    "IsPictureId": "IsPictureId",
    "IsPlanSendTime": "IsPlanSendTime",
    "IsPreviousMentalHealthTreatment": "IsPreviousMentalHealthTreatment",
    "IsTakingPrescriptionMedication": "IsTakingPrescriptionMedication",
    "IsTriagedtoMedicalDetoxFacility": "IsTriagedtoMedicalDetoxFacility",
    "LastUsedTime": "LastUsedTime",
    "MedicalConditionsProviderName1": "MedicalConditionsProviderName1",
    "MedicalConditionsProviderName2": "MedicalConditionsProviderName2",
    "MedicalConditionsProviderPhone1": "MedicalConditionsProviderPhone1",
    "MedicalConditionsProviderPhone2": "MedicalConditionsProviderPhone2",
    "MedicalEmergencyDescribe": "MedicalEmergencyDescribe",
    "Medicalinformation": "Medicalinformation",
    "MedicalinformationP": "MedicalinformationP",
    "MedicalText": "MedicalText",
    "MedicalTextP": "MedicalTextP",
    "MedicationConditions": "MedicationConditions",
    "MedicationName1": "MedicationName1",
    "MedicationName2": "MedicationName2",
    "MedicationName3": "MedicationName3",
    "MedicationName4": "MedicationName4",
    "MedicationName5": "MedicationName5",
    "MentalHealthTreatmentDescription": "MentalHealthTreatmentDescription",
    "Modified": "Modified",
    "ModifiedBy": "ModifiedBy",
    "ModifiedByP": "ModifiedByP",
    "ModifiedP": "ModifiedP",
    "NeedReferal": "NeedReferal",
    "ObservationComments": "ObservationComments",
    "OfficeUseTime": "OfficeUseTime",
    "OfficeUseWhy": "OfficeUseWhy",
    "OngoingMedicalConditionsWha": "OngoingMedicalConditionsWha",
    "Other": "other",
    "Other2": "other2",
    "Other2P": "other2P",
    "OtherCode": "OtherCode",
    "OtherP": "otherP",
    "OtherText": "OtherText",
    "OtherTextP": "OtherTextP",
    "OverTheCounterMedicationsText1": "OverTheCounterMedicationsText1",
    "Participationininitialsceeningprocess": "Participationininitialsceeningprocess",
    "ParticipationininitialsceeningprocessP": "ParticipationininitialsceeningprocessP",
    "PatientSignature": "PatientSignature",
    "PatientSignatureDate": "PatientSignatureDate",
    "PersonForIntakeProcess": "PersonForIntakeProcess",
    "Phone1": "Phone1",
    "Phone2": "Phone2",
    "Phone3": "Phone3",
    "Phone4": "Phone4",
    "Phone5": "Phone5",
    "PlanOfSuicide": "PlanOfSuicide",
    "PlanOnSpendingTimeAtClinic": "PlanOnSpendingTimeAtClinic",
    "PreAdd_Address": "PreAdd_Address",
    "Prescriber1": "Prescriber1",
    "Prescriber2": "Prescriber2",
    "Prescriber3": "Prescriber3",
    "Prescriber4": "Prescriber4",
    "Prescriber5": "Prescriber5",
    "ProgressRecoveryGoalsText": "ProgressRecoveryGoalsText",
    "ProgressRecoveryGoalsTextP": "ProgressRecoveryGoalsTextP",
    "Progresstowardsrecoverygoals": "Progresstowardsrecoverygoals",
    "ProgresstowardsrecoverygoalsP": "ProgresstowardsrecoverygoalsP",
    "ProofOfOpiateDependence": "ProofOfOpiateDependence",
    "PurposeOfObtainingRelease": "PurposeOfObtainingRelease",
    "PurposeOfObtainingReleaseP": "PurposeOfObtainingReleaseP",
    "Reason": "Reason",
    "Reasonsfordischarge": "Reasonsfordischarge",
    "ReasonsfordischargeP": "ReasonsfordischargeP",
    "RecentHeadTrauma": "RecentHeadTrauma",
    "Recoveryplansorgoals": "Recoveryplansorgoals",
    "RecoveryplansorgoalsP": "RecoveryplansorgoalsP",
    "RecoveryPlansText": "RecoveryPlansText",
    "RecoveryPlansTextP": "RecoveryPlansTextP",
    "Referralrecommendations": "Referralrecommendations",
    "ReferralrecommendationsP": "ReferralrecommendationsP",
    "ReferralRecommendationsText": "ReferralRecommendationsText",
    "ReferralRecommendationsTextP": "ReferralRecommendationsTextP",
    "ReferredBy": "ReferredBy",
    "Relapseepisodes": "Relapseepisodes",
    "RelapseepisodesP": "RelapseepisodesP",
    "RelapseEpisodesText": "RelapseEpisodesText",
    "RelapseEpisodesTextP": "RelapseEpisodesTextP",
    "RevocationClientSignature": "RevocationClientSignature",
    "RevocationClientSignatureDate": "RevocationClientSignatureDate",
    "RevocationClientSignatureDateP": "RevocationClientSignatureDateP",
    "RevocationClientSignatureP": "RevocationClientSignatureP",
    "RevocationWitnessSignature": "RevocationWitnessSignature",
    "RevocationWitnessSignatureDate": "RevocationWitnessSignatureDate",
    "RevocationWitnessSignatureDateP": "RevocationWitnessSignatureDateP",
    "RevocationWitnessSignatureP": "RevocationWitnessSignatureP",
    "RNPStaffSign": "RNPStaffSign",
    "RNPStaffSignDate": "RNPStaffSignDate",
    "SammsProgramID": "SammsProgramID",
    "SeizureInPast7Days": "SeizureInPast7Days",
    "SetupAppointment": "SetupAppointment",
    "SignatureDate": "SignatureDate",
    "SignatureOfConsenP": "SignatureOfConsenP",
    "SignatureOfConsent": "SignatureOfConsent",
    "SignatureOfConsentP": "SignatureOfConsentP",
    "SignatureRnpStaff": "SignatureRnpStaff",
    "SubstanceAbuseWhat": "SubstanceAbuseWhat",
    "SubstanceAbuseWhen": "SubstanceAbuseWhen",
    "SubstanceAbuseWhere": "SubstanceAbuseWhere",
    "Summaryofinitialscreeningprocess": "Summaryofinitialscreeningprocess",
    "SummaryofinitialscreeningprocessP": "SummaryofinitialscreeningprocessP",
    "SupervisorSignature": "SupervisorSignature",
    "SupervisorSignatureDate": "SupervisorSignatureDate",
    "TakenTime": "TakenTime",
    "TimeFinishingRNtriage": "TimeFinishingRNtriage",
    "TimeStartingRNtriage": "TimeStartingRNtriage",
    "Treatmentrecommendations": "Treatmentrecommendations",
    "TreatmentrecommendationsP": "TreatmentrecommendationsP",
    "TreatmentText": "TreatmentText",
    "TreatmentTextP": "TreatmentTextP",
    "TypeOfMedication": "TypeOfMedication",
    "Upondischargefromtreatment": "Upondischargefromtreatment",
    "UpondischargefromtreatmentP": "UpondischargefromtreatmentP",
    "Uponreceiptofinformationrequested": "Uponreceiptofinformationrequested",
    "UponreceiptofinformationrequestedP": "UponreceiptofinformationrequestedP",
    "Uponreceiptofpaymentforservicesrendered": "Uponreceiptofpaymentforservicesrendered",
    "UponreceiptofpaymentforservicesrenderedP": "UponreceiptofpaymentforservicesrenderedP",
    "Urinalysisresults": "Urinalysisresults",
    "UrinalysisresultsP": "UrinalysisresultsP",
    "UrinalysisText": "UrinalysisText",
    "UrinalysisTextP": "UrinalysisTextP",
    "WhatAccomodations": "WhatAccomodations",
    "ParentPreAdmissionId": "ParentPreAdmissionId",
    "IsRequireChildCare": "IsRequireChildCare",
    "AnswerRangeThree": "AnswerRangeThree",
    "AnswerRangeSix": "AnswerRangeSix",
    "AnswerRangeNine": "AnswerRangeNine",
    "AnswerRangeAbove": "AnswerRangeAbove",
    "IsReliableTransportation": "IsReliableTransportation",
    "RequireTransportationServices": "RequireTransportationServices",
    "IsTransfer": "IsTransfer",
    "WhereTransfer": "WhereTransfer",
    "DropdownNumberOfChildren": "dropdownNumberOfChildren",
    "Incarcenated": "Incarcenated",
    "ClientAddress": "ClientAddress",
    "IsEmployed": "IsEmployed",
    "IsAttemptedSuicide": "IsAttemptedSuicide",
    "IsThoughtsOfKilling": "IsThoughtsOfKilling",
    "IsMentalHealthTreatment": "isMentalHealthTreatment",
    "MentalHealthTreatmentWhere": "MentalHealthTreatmentWhere",
    "MentalHealthTreatmentWhen": "MentalHealthTreatmentWhen",
    "MentalHealthTreatmentWhat": "MentalHealthTreatmentWhat",
    "ThoughtsOfHurtingTxt": "ThoughtsOfHurtingTxt",
    "SuicideDetailsTxt": "SuicideDetailsTxt",
    "ReliableTransportationTxt": "ReliableTransportationTxt",
    "EmployedYesTxt": "EmployedYesTxt",
    "EmployedNoTxt": "EmployedNoTxt",
    "IsPlanForHowToHurtSomeElse": "IsPlanForHowToHurtSomeElse",
    "PacketTypeID": "PacketTypeID",
    "UsingOpioids": "UsingOpioids",
    "TimeOfIntake": "TimeOfIntake",
    "SubstanceUseFromDate": "SubstanceUseFromDate",
    "SubstanceUseToDate": "SubstanceUseToDate",
    "MentalHealthFromDate": "MentalHealthFromDate",
    "MentalHealthToDate": "MentalHealthToDate",
    "ReasonForDenial": "ReasonForDenial",
    "DischargeReason": "DischargeReason",
    "ReferedTo": "ReferedTo",
    "DateOfStaffSignature": "DateOfStaffSignature",
    "NameOfWitness": "nameOfWitness",
    "StaffAtHCRC": "staffAtHCRC",
    "SUDinformationMA": "SUDinformationMA",
    "ProtectedHealthInformationMA": "protectedHealthInformationMA",
    "NamedIndividualMA": "namedIndividualMA",
    "NamedEntityMA": "namedEntityMA",
    "NamedThirdPartyMA": "namedThirdPartyMA",
    "NamedEntityWithoutTreatmentMA": "namedEntityWithoutTreatmentMA",
    "NamedIndividualPaticipantMA": "namedIndividualPaticipantMA",
    "GenralTreatingproviderrelationshipMA": "genralTreatingproviderrelationshipMA",
    "PurposeOfDisclosureMA": "purposeOfDisclosureMA",
    "ExclusionsMA": "exclusionsMA",
    "RepresentativesRelationshiptoPatientMA": "representativesRelationshiptoPatientMA",
    "HCRCClinicLocationMA": "HCRCClinicLocationMA",
    "AddressMA": "AddressMA",
    "PhoneMA": "PhoneMA",
    "FAXMA": "FAXMA",
    "UnlessrevokedMA": "unlessrevokedMA",
    "Istheresubstanceusehistory": "istheresubstanceusehistory",
    "Iscurrentlypatientpainmanagementmethadone": "iscurrentlypatientpainmanagementmethadone",
    "Txtpatientatpainmanagementclinicmethadone": "txtpatientatpainmanagementclinicmethadone",
    "Interestedtransferringetsyes": "interestedtransferringetsyes",
    "Ispreviouslybeenpatientmedicationassisted": "ispreviouslybeenpatientmedicationassisted",
    "Txtpreviouslybeenpatientmedicationassisted": "txtpreviouslybeenpatientmedicationassisted",
    "Datepreviouslypatientmedicationassisted": "datepreviouslypatientmedicationassisted",
    "Ispregnant": "ispregnant",
    "Currentlypregnant": "currentlypregnant",
    "Isplantopaytreatment": "isplantopaytreatment",
    "Txtplantopaytreatmentprivateinsurance": "txtplantopaytreatmentprivateinsurance",
    "Txtplantopaytreatmentother": "txtplantopaytreatmentother",
    "Isdoyouhavepicture": "isdoyouhavepicture",
    "Datetimelastopioid": "datetimelastopioid",
    "Issymptomsofopioidwithdrawal": "issymptomsofopioidwithdrawal",
    "Txtsymptomsofopioidwithdrawaltrue": "txtsymptomsofopioidwithdrawaltrue",
    "Ishadalcoholinlast12hours": "ishadalcoholinlast12hours",
    "Txthadanyalcohol": "txthadanyalcohol",
    "Isprioretspatientyes": "isprioretspatientyes",
    "Prioretspatient": "prioretspatient",
    "Lastprescription": "lastprescription",
    "Ishaveyouhospitalizedinlast30daysyes": "ishaveyouhospitalizedinlast30daysyes",
    "Txthospitalisedlast30days": "txthospitalisedlast30days",
    "NameOfWitnessQ8": "nameOfWitnessQ8",
    "UnlessrevokedQ8": "unlessrevokedQ8",
    "FAXQ8": "FAXQ8",
    "PhoneQ8": "PhoneQ8",
    "AddressQ8": "AddressQ8",
    "HCRCClinicLocationQ8": "HCRCClinicLocationQ8",
    "RepresentativesRelationshiptoPatientQ8": "representativesRelationshiptoPatientQ8",
    "ExclusionsQ8": "exclusionsQ8",
    "PurposeOfDisclosureQ8": "purposeOfDisclosureQ8",
    "GenralTreatingproviderrelationshipQ8": "genralTreatingproviderrelationshipQ8",
    "NamedIndividualPaticipantQ8": "namedIndividualPaticipantQ8",
    "NamedEntityWithoutTreatmentQ8": "namedEntityWithoutTreatmentQ8",
    "NamedThirdPartyQ8": "namedThirdPartyQ8",
    "NamedEntityQ8": "namedEntityQ8",
    "NamedIndividualQ8": "namedIndividualQ8",
    "ProtectedHealthInformationQ8": "protectedHealthInformationQ8",
    "SUDinformationQ8": "SUDinformationQ8",
    "StaffAtHCRCQ8": "staffAtHCRCQ8",
    "AdditionalDoc": "AdditionalDoc",
    "ChkboxSUDInformationQ2_1": "ChkboxSUDInformationQ2_1",
    "ChkboxSUDInformationQ2_2": "ChkboxSUDInformationQ2_2",
    "ChkboxSUDInformationQ2_3": "ChkboxSUDInformationQ2_3",
    "ChkboxSUDInformationQ2_4": "ChkboxSUDInformationQ2_4",
    "ChkboxSUDInformationQ2_5": "ChkboxSUDInformationQ2_5",
    "ChkboxSUDInformationQ2_6": "ChkboxSUDInformationQ2_6",
    "ChkboxSUDInformationQ2_7": "ChkboxSUDInformationQ2_7",
    "ChkboxSUDInformationQ2_8": "ChkboxSUDInformationQ2_8",
    "ChkboxSUDInformationQ2_9": "ChkboxSUDInformationQ2_9",
    "ChkboxSUDInformationQ2_10": "ChkboxSUDInformationQ2_10",
    "ChkboxSUDInformationQ2_11": "ChkboxSUDInformationQ2_11",
    "ChkboxProtectedHealthInformationQ2_1": "ChkboxProtectedHealthInformationQ2_1",
    "ChkboxProtectedHealthInformationQ2_2": "ChkboxProtectedHealthInformationQ2_2",
    "ChkboxProtectedHealthInformationQ2_3": "ChkboxProtectedHealthInformationQ2_3",
    "ChkboxProtectedHealthInformationQ2_4": "ChkboxProtectedHealthInformationQ2_4",
    "ChkboxSUDInformationQ8_1": "ChkboxSUDInformationQ8_1",
    "ChkboxSUDInformationQ8_2": "ChkboxSUDInformationQ8_2",
    "ChkboxSUDInformationQ8_3": "ChkboxSUDInformationQ8_3",
    "ChkboxSUDInformationQ8_4": "ChkboxSUDInformationQ8_4",
    "ChkboxSUDInformationQ8_5": "ChkboxSUDInformationQ8_5",
    "ChkboxSUDInformationQ8_6": "ChkboxSUDInformationQ8_6",
    "ChkboxSUDInformationQ8_7": "ChkboxSUDInformationQ8_7",
    "ChkboxSUDInformationQ8_8": "ChkboxSUDInformationQ8_8",
    "ChkboxSUDInformationQ8_9": "ChkboxSUDInformationQ8_9",
    "ChkboxSUDInformationQ8_10": "ChkboxSUDInformationQ8_10",
    "ChkboxSUDInformationQ8_11": "ChkboxSUDInformationQ8_11",
    "ChkboxProtectedHealthInformationQ8_1": "ChkboxProtectedHealthInformationQ8_1",
    "ChkboxProtectedHealthInformationQ8_2": "ChkboxProtectedHealthInformationQ8_2",
    "ChkboxProtectedHealthInformationQ8_3": "ChkboxProtectedHealthInformationQ8_3",
    "ChkboxProtectedHealthInformationQ8_4": "ChkboxProtectedHealthInformationQ8_4",
    "RadioNamedIndividualQ2": "radioNamedIndividualQ2",
    "RadioNamedthirdpartyPayerQ2": "radioNamedthirdpartyPayerQ2",
    "RadioNamedEntityProvideRelationWithMeQ2": "radioNamedEntityProvideRelationWithMeQ2",
    "RadioNamedEntityWithoutProvideRelationWithMeQ2": "radioNamedEntityWithoutProvideRelationWithMeQ2",
    "RadioNamedIndividualParticipantQ2": "radioNamedIndividualParticipantQ2",
    "RadioGeneralDesignationQ2": "radioGeneralDesignationQ2",
    "RadioNamedIndividualQ8": "radioNamedIndividualQ8",
    "RadioNamedthirdpartyPayerQ8": "radioNamedthirdpartyPayerQ8",
    "RadioNamedEntityProvideRelationWithMeQ8": "radioNamedEntityProvideRelationWithMeQ8",
    "RadioNamedEntityWithoutProvideRelationWithMeQ8": "radioNamedEntityWithoutProvideRelationWithMeQ8",
    "RadioNamedIndividualParticipantQ8": "radioNamedIndividualParticipantQ8",
    "RadioGeneralDesignationQ8": "radioGeneralDesignationQ8",
    "RadioNamedIndividualQ2select": "radioNamedIndividualQ2select",
    "RadioNamedIndividualParticipantQ2select": "radioNamedIndividualParticipantQ2select",
    "RadioNamedIndividualQ8select": "radioNamedIndividualQ8select",
    "RadioNamedIndividualParticipantQ8select": "radioNamedIndividualParticipantQ8select",
    "ChkBoxRevokeROIQ2": "chkBoxRevokeROIQ2",
    "TxtBoxRevokeROIQ2": "txtBoxRevokeROIQ2",
    "ChkBoxRevokeROIQ8": "chkBoxRevokeROIQ8",
    "TxtBoxRevokeROIQ8": "txtBoxRevokeROIQ8",
    "NamedEntitesIDQ2": "NamedEntitesIDQ2",
    "NamedEntitesIDQ8": "NamedEntitesIDQ8",
    "ChkboxSUDInformationQ2_12": "ChkboxSUDInformationQ2_12",
    "ChkboxSUDInformationQ2_13": "ChkboxSUDInformationQ2_13",
    "SUDInformationOtherQ2": "SUDInformationOtherQ2",
    "ChkboxSUDInformationQ8_12": "ChkboxSUDInformationQ8_12",
    "ChkboxSUDInformationQ8_13": "ChkboxSUDInformationQ8_13",
    "SUDInformationOtherQ8": "SUDInformationOtherQ8",
    "StaffSignCredentials": "StaffSignCredentials",
    "DateofRelease": "DateofRelease",
    "DoYouHaveInsuranceNo": "DoYouHaveInsuranceNo",
    "Version": "Version",
    "PrimaryReferralSourceNote": "PrimaryReferralSourceNote",
    "SecondaryReferralSource": "SecondaryReferralSource",
    "PrimaryReferralSource": "PrimaryReferralSource",
    "InitialScreening": "InitialScreening",
    "DDLPrimarySubstance": "DDLPrimarySubstance",
    "CurrentlyUsingOpoidDrug": "CurrentlyUsingOpoidDrug",
    "MedicatedAssistedTreatmentProgram": "MedicatedAssistedTreatmentProgram",
    "PainManagementClinic": "PainManagementClinic",
    "ReceivedTreatmentForAddiction": "ReceivedTreatmentForAddiction",
    "RequireAssistiveTechnologies": "RequireAssistiveTechnologies",
    "HaveYouUsedOpoidDrug": "HaveYouUsedOpoidDrug",
    "AnswerAllQuestion": "AnswerAllQuestion",
    "AnswerAllQuestionTxt": "AnswerAllQuestionTxt",
    "AppointmentDate": "AppointmentDate",
    "AppointmentTime": "AppointmentTime",
    "ScreenedBy": "ScreenedBy",
    "ScreenedByDate": "ScreenedByDate",
    "FollowupScreening": "FollowupScreening",
    "ReviewedResponsesFromCallCenter": "ReviewedResponsesFromCallCenter",
    "AnyChangestoInformation": "AnyChangestoInformation",
    "HadAnySuicidalThoughts": "HadAnySuicidalThoughts",
    "HadAnySuicidalThoughtsTxt": "HadAnySuicidalThoughtsTxt",
    "HadAnyHomicidalThoughts": "HadAnyHomicidalThoughts",
    "HadAnyHomicidalThoughtsTxt": "HadAnyHomicidalThoughtsTxt",
    "HaveBeenCarcerated": "HaveBeenCarcerated",
    "HaveAnyUrgentNeeds": "HaveAnyUrgentNeeds",
    "AdditionalComments": "AdditionalComments",
    "Comment": "Comment",
    "ReceiveingAnyTreatment": "ReceiveingAnyTreatment",
    "ReasonforReferral": "ReasonforReferral",
    "NA": "NA",
    "PacketVersion": "PacketVersion"
}

TARGET_SCHEMA = {
    "SiteCode": "string",
    "ID": "int",
    "PatientID": "int",
    "RegistrationModeID": "int",
    "IsIntakeAppointmentScheduled": "boolean",
    "IntakeAppointmentScheduledDateTime": "timestamp",
    "ReferralSourceID": "int",
    "ProgramID": "int",
    "OpiatesUsageNoOfYears": "int",
    "OpiatesUsageNoOfMonths": "int",
    "DrugChoiceID": "int",
    "DrugAdministrationTypeID": "int",
    "IllicitSubstanceOther": "string",
    "IllicitSubstanceOtherMgPerDay": "int",
    "IsCurrentlyInOpiateProgram": "boolean",
    "CurrentOpiateProgramLocation": "string",
    "CurrentOpiateProgramNoOfYears": "int",
    "CurrentOpiateProgramNoOfMonths": "int",
    "CurrentOpiateProgramDose": "string",
    "IsPatientAtPainManagementClinic": "boolean",
    "PainManagementClinicLocation": "string",
    "PainManagementClinicFromDate": "timestamp",
    "PainManagementClinicToDate": "timestamp",
    "IsHavingLegalPrescription": "boolean",
    "IsAnyLegalPrescriptionForPain": "boolean",
    "IsAnyOngoingMedicalCondition": "boolean",
    "IsCurrentlyPregnant": "boolean",
    "IsSuicidalThoughtWithin72Hours": "boolean",
    "SuicidalThoughtIntentScale": "int",
    "IsHavingPlanForHowToCommitSuicide": "boolean",
    "IsHomicidalThoughtWithin72Hours": "boolean",
    "IsRecentlyReleasedFromPenal": "boolean",
    "ReleasedFromPenalDate": "timestamp",
    "IsSpecialAccommodationRequired": "boolean",
    "IsAbleToAttendTreatementCenterDaily": "boolean",
    "IsAbleToPayIntakeAndWeeklyFee": "boolean",
    "IsEligibleForFurtherAssessment": "boolean",
    "IsReferralOffered": "boolean",
    "ReferralOfferedList": "string",
    "RejectedReferralOffered": "boolean",
    "ReasonSeekingTreatment": "string",
    "IsPatientAdmitted": "boolean",
    "IsParticipitatingInOtherOpioidProgram": "boolean",
    "IsPresentedWithoutIDProof": "boolean",
    "IsPresentedAtCloseOfDay": "boolean",
    "IsRequestedAdmissionDrUnavailable": "boolean",
    "RequestedAdmissionDay": "int",
    "RequestedAdmissionAMOrPM": "boolean",
    "IsUnableToPay": "boolean",
    "IsUnableToProvideEvidenceofAddiction": "boolean",
    "IsAddictionToUnstableNeeding": "boolean",
    "IsImpairment": "boolean",
    "IsMedicalProblemsNeedingStabilization": "boolean",
    "IsPsychiatricProblemNeedingInpatientTreatment": "boolean",
    "IsServiceDeclined": "boolean",
    "IsOther": "boolean",
    "OtherDescription": "string",
    "Comments": "string",
    "Active": "boolean",
    "CreatedBy": "string",
    "CreatedOn": "timestamp",
    "LastUpdatedBy": "string",
    "LastUpdateOn": "timestamp",
    "PreAdmissionDate": "timestamp",
    "LegalPrescription1": "string",
    "LegalPrescription2": "string",
    "LegalPrescription3": "string",
    "MedicalCondition1": "string",
    "MedicalCondition2": "string",
    "MedicalCondition3": "string",
    "DrugofchoiceAdministered": "string",
    "RequestedAdmissionDayFri": "boolean",
    "RequestedAdmissionDayMon": "boolean",
    "RequestedAdmissionDayThu": "boolean",
    "RequestedAdmissionDayTue": "boolean",
    "RequestedAdmissionDayWed": "boolean",
    "AccomodationNeeded": "string",
    "AcknowledgeClientSignature": "string",
    "AcknowledgeClientSignatureDate": "timestamp",
    "AcknowledgeClientSignatureDateP": "timestamp",
    "AcknowledgeClientSignatureP": "string",
    "AcknowledgeWitnessSignature": "string",
    "AcknowledgeWitnessSignatureDate": "timestamp",
    "AcknowledgeWitnessSignatureDateP": "timestamp",
    "AcknowledgeWitnessSignatureP": "string",
    "ActiveP": "boolean",
    "AlcoholAmount": "string",
    "AlcoholLastDrink": "string",
    "ApplicantName": "string",
    "AreYouCurrentlyPregnant": "int",
    "Biopsychosocialassessment": "boolean",
    "BiopsychosocialassessmentP": "boolean",
    "BiopsychosocialText": "string",
    "BiopsychosocialTextP": "string",
    "BringIDProof": "boolean",
    "BringInsuranceCard": "boolean",
    "ChronicMedicalCondition": "string",
    "ClientDetails": "string",
    "ClientNameP": "string",
    "ClinicInfo": "boolean",
    "Created": "timestamp",
    "CreatedByP": "string",
    "CreatedP": "timestamp",
    "CurrentlyMedicatedTreatmentOther": "string",
    "CurrentOpiateProgramFrom": "timestamp",
    "CurrentOpiateProgramTo": "timestamp",
    "CurrentOpiateWhatProgram": "string",
    "CurrntlyRecevingTreatmentForCondition": "boolean",
    "DailyTime": "string",
    "DataFormId": "int",
    "Date": "timestamp",
    "DateP": "timestamp",
    "Daysafterdischarge": "boolean",
    "DaysafterdischargeP": "boolean",
    "DeniedDuetoCapacity": "boolean",
    "Describeother": "string",
    "Describeother1": "string",
    "Describeother1P": "string",
    "Describeother2": "string",
    "Describeother2P": "string",
    "DescribeotherP": "string",
    "Diagnosis": "boolean",
    "DiagnosisP": "boolean",
    "DiagnosisText": "string",
    "DiagnosisTextP": "string",
    "DischargeReasonsText": "string",
    "DischargeReasonsTextP": "string",
    "DischargeSummary": "boolean",
    "DischargeSummaryP": "boolean",
    "DischargeSummaryText": "string",
    "DischargeSummaryTextP": "string",
    "DoctorSignature": "string",
    "DoctorSignatureDate": "timestamp",
    "DropdownPresentlyInPainScale1to10": "int",
    "DrugApplicantTime": "string",
    "DrugDaily": "string",
    "DrugLastUsed": "string",
    "DrugTaken": "string",
    "DrugUsing": "string",
    "EnrollmentId": "int",
    "GynDoctorProviderName": "string",
    "GynDoctorProviderPhone": "string",
    "HasGynDoctor": "boolean",
    "ImmediateAssessment": "string",
    "ImmediateAssessment911": "string",
    "InformationToObtained": "int",
    "InformationToObtainedFax": "boolean",
    "InformationToObtainedFaxP": "boolean",
    "InformationToObtainedP": "int",
    "InformationToObtainedVerbal": "boolean",
    "InformationToObtainedVerbalP": "boolean",
    "InformationToObtainedWritten": "boolean",
    "InformationToObtainedWrittenP": "boolean",
    "InitialSceeningParticipationText": "string",
    "InitialSceeningParticipationTextP": "string",
    "InitialScreeningSummaryText": "string",
    "InitialScreeningSummaryTextP": "string",
    "InsuranceDescription": "string",
    "InsuranceType": "string",
    "IntakeProgram": "string",
    "IntakeProgramDate": "timestamp",
    "IsAllergies": "boolean",
    "IsAmPm": "boolean",
    "IsAnyPrescriptionForPain": "boolean",
    "IsApplicantPregnant": "boolean",
    "IsBehaviorallyunstabldangerous": "boolean",
    "IsClinicAddress": "boolean",
    "IsCSUAtFullCapacity": "boolean",
    "IsDeleted": "boolean",
    "IsDetox": "boolean",
    "IsDidnotMeetMedicalNecessity": "boolean",
    "IsDrinkingAlcohol": "boolean",
    "IsEVSCompleted": "boolean",
    "IsInsurance": "boolean",
    "IsInsuranceAvailable": "boolean",
    "IsInsuranceCard": "boolean",
    "IsMaintenance": "boolean",
    "IsMedicalEmergency": "boolean",
    "IsOverTheCounterMedications": "boolean",
    "IsPackets": "boolean",
    "IsPastTreatmentHistory": "boolean",
    "IsPhysicalHealthUnstable": "boolean",
    "IsPictureId": "boolean",
    "IsPlanSendTime": "boolean",
    "IsPreviousMentalHealthTreatment": "boolean",
    "IsTakingPrescriptionMedication": "boolean",
    "IsTriagedtoMedicalDetoxFacility": "boolean",
    "LastUsedTime": "string",
    "MedicalConditionsProviderName1": "string",
    "MedicalConditionsProviderName2": "string",
    "MedicalConditionsProviderPhone1": "string",
    "MedicalConditionsProviderPhone2": "string",
    "MedicalEmergencyDescribe": "string",
    "Medicalinformation": "boolean",
    "MedicalinformationP": "boolean",
    "MedicalText": "string",
    "MedicalTextP": "string",
    "MedicationConditions": "string",
    "MedicationName1": "string",
    "MedicationName2": "string",
    "MedicationName3": "string",
    "MedicationName4": "string",
    "MedicationName5": "string",
    "MentalHealthTreatmentDescription": "string",
    "Modified": "timestamp",
    "ModifiedBy": "string",
    "ModifiedByP": "string",
    "ModifiedP": "timestamp",
    "NeedReferal": "string",
    "ObservationComments": "string",
    "OfficeUseTime": "int",
    "OfficeUseWhy": "string",
    "OngoingMedicalConditionsWha": "string",
    "Other": "boolean",
    "Other2": "boolean",
    "Other2P": "boolean",
    "OtherCode": "boolean",
    "OtherP": "boolean",
    "OtherText": "string",
    "OtherTextP": "string",
    "OverTheCounterMedicationsText1": "string",
    "Participationininitialsceeningprocess": "boolean",
    "ParticipationininitialsceeningprocessP": "boolean",
    "PatientSignature": "string",
    "PatientSignatureDate": "timestamp",
    "PersonForIntakeProcess": "boolean",
    "Phone1": "string",
    "Phone2": "string",
    "Phone3": "string",
    "Phone4": "string",
    "Phone5": "string",
    "PlanOfSuicide": "boolean",
    "PlanOnSpendingTimeAtClinic": "boolean",
    "PreAdd_Address": "string",
    "Prescriber1": "string",
    "Prescriber2": "string",
    "Prescriber3": "string",
    "Prescriber4": "string",
    "Prescriber5": "string",
    "ProgressRecoveryGoalsText": "string",
    "ProgressRecoveryGoalsTextP": "string",
    "Progresstowardsrecoverygoals": "boolean",
    "ProgresstowardsrecoverygoalsP": "boolean",
    "ProofOfOpiateDependence": "boolean",
    "PurposeOfObtainingRelease": "int",
    "PurposeOfObtainingReleaseP": "int",
    "Reason": "string",
    "Reasonsfordischarge": "boolean",
    "ReasonsfordischargeP": "boolean",
    "RecentHeadTrauma": "boolean",
    "Recoveryplansorgoals": "boolean",
    "RecoveryplansorgoalsP": "boolean",
    "RecoveryPlansText": "string",
    "RecoveryPlansTextP": "string",
    "Referralrecommendations": "boolean",
    "ReferralrecommendationsP": "boolean",
    "ReferralRecommendationsText": "string",
    "ReferralRecommendationsTextP": "string",
    "ReferredBy": "string",
    "Relapseepisodes": "boolean",
    "RelapseepisodesP": "boolean",
    "RelapseEpisodesText": "string",
    "RelapseEpisodesTextP": "string",
    "RevocationClientSignature": "string",
    "RevocationClientSignatureDate": "timestamp",
    "RevocationClientSignatureDateP": "timestamp",
    "RevocationClientSignatureP": "string",
    "RevocationWitnessSignature": "string",
    "RevocationWitnessSignatureDate": "timestamp",
    "RevocationWitnessSignatureDateP": "timestamp",
    "RevocationWitnessSignatureP": "string",
    "RNPStaffSign": "string",
    "RNPStaffSignDate": "timestamp",
    "SammsProgramID": "int",
    "SeizureInPast7Days": "boolean",
    "SetupAppointment": "string",
    "SignatureDate": "timestamp",
    "SignatureOfConsenP": "string",
    "SignatureOfConsent": "string",
    "SignatureOfConsentP": "string",
    "SignatureRnpStaff": "string",
    "SubstanceAbuseWhat": "string",
    "SubstanceAbuseWhen": "string",
    "SubstanceAbuseWhere": "string",
    "Summaryofinitialscreeningprocess": "boolean",
    "SummaryofinitialscreeningprocessP": "boolean",
    "SupervisorSignature": "string",
    "SupervisorSignatureDate": "timestamp",
    "TakenTime": "string",
    "TimeFinishingRNtriage": "string",
    "TimeStartingRNtriage": "string",
    "Treatmentrecommendations": "boolean",
    "TreatmentrecommendationsP": "boolean",
    "TreatmentText": "string",
    "TreatmentTextP": "string",
    "TypeOfMedication": "int",
    "Upondischargefromtreatment": "boolean",
    "UpondischargefromtreatmentP": "boolean",
    "Uponreceiptofinformationrequested": "boolean",
    "UponreceiptofinformationrequestedP": "boolean",
    "Uponreceiptofpaymentforservicesrendered": "boolean",
    "UponreceiptofpaymentforservicesrenderedP": "boolean",
    "Urinalysisresults": "boolean",
    "UrinalysisresultsP": "boolean",
    "UrinalysisText": "string",
    "UrinalysisTextP": "string",
    "WhatAccomodations": "string",
    "ParentPreAdmissionId": "int",
    "IsRequireChildCare": "boolean",
    "AnswerRangeThree": "boolean",
    "AnswerRangeSix": "boolean",
    "AnswerRangeNine": "boolean",
    "AnswerRangeAbove": "boolean",
    "IsReliableTransportation": "boolean",
    "RequireTransportationServices": "int",
    "IsTransfer": "int",
    "WhereTransfer": "string",
    "DropdownNumberOfChildren": "int",
    "Incarcenated": "string",
    "ClientAddress": "string",
    "IsEmployed": "boolean",
    "IsAttemptedSuicide": "boolean",
    "IsThoughtsOfKilling": "boolean",
    "IsMentalHealthTreatment": "boolean",
    "MentalHealthTreatmentWhere": "string",
    "MentalHealthTreatmentWhen": "string",
    "MentalHealthTreatmentWhat": "string",
    "ThoughtsOfHurtingTxt": "string",
    "SuicideDetailsTxt": "string",
    "ReliableTransportationTxt": "string",
    "EmployedYesTxt": "string",
    "EmployedNoTxt": "string",
    "IsPlanForHowToHurtSomeElse": "boolean",
    "PacketTypeID": "int",
    "UsingOpioids": "string",
    "TimeOfIntake": "boolean",
    "SubstanceUseFromDate": "timestamp",
    "SubstanceUseToDate": "timestamp",
    "MentalHealthFromDate": "timestamp",
    "MentalHealthToDate": "timestamp",
    "ReasonForDenial": "int",
    "DischargeReason": "string",
    "ReferedTo": "string",
    "DateOfStaffSignature": "timestamp",
    "NameOfWitness": "string",
    "StaffAtHCRC": "int",
    "SUDinformationMA": "int",
    "ProtectedHealthInformationMA": "int",
    "NamedIndividualMA": "string",
    "NamedEntityMA": "string",
    "NamedThirdPartyMA": "string",
    "NamedEntityWithoutTreatmentMA": "string",
    "NamedIndividualPaticipantMA": "string",
    "GenralTreatingproviderrelationshipMA": "string",
    "PurposeOfDisclosureMA": "string",
    "ExclusionsMA": "string",
    "RepresentativesRelationshiptoPatientMA": "string",
    "HCRCClinicLocationMA": "string",
    "AddressMA": "string",
    "PhoneMA": "string",
    "FAXMA": "string",
    "UnlessrevokedMA": "string",
    "Istheresubstanceusehistory": "boolean",
    "Iscurrentlypatientpainmanagementmethadone": "boolean",
    "Txtpatientatpainmanagementclinicmethadone": "string",
    "Interestedtransferringetsyes": "boolean",
    "Ispreviouslybeenpatientmedicationassisted": "boolean",
    "Txtpreviouslybeenpatientmedicationassisted": "string",
    "Datepreviouslypatientmedicationassisted": "timestamp",
    "Ispregnant": "boolean",
    "Currentlypregnant": "timestamp",
    "Isplantopaytreatment": "int",
    "Txtplantopaytreatmentprivateinsurance": "string",
    "Txtplantopaytreatmentother": "string",
    "Isdoyouhavepicture": "boolean",
    "Datetimelastopioid": "timestamp",
    "Issymptomsofopioidwithdrawal": "boolean",
    "Txtsymptomsofopioidwithdrawaltrue": "string",
    "Ishadalcoholinlast12hours": "boolean",
    "Txthadanyalcohol": "string",
    "Isprioretspatientyes": "boolean",
    "Prioretspatient": "timestamp",
    "Lastprescription": "timestamp",
    "Ishaveyouhospitalizedinlast30daysyes": "boolean",
    "Txthospitalisedlast30days": "string",
    "NameOfWitnessQ8": "string",
    "UnlessrevokedQ8": "string",
    "FAXQ8": "string",
    "PhoneQ8": "string",
    "AddressQ8": "string",
    "HCRCClinicLocationQ8": "string",
    "RepresentativesRelationshiptoPatientQ8": "string",
    "ExclusionsQ8": "string",
    "PurposeOfDisclosureQ8": "string",
    "GenralTreatingproviderrelationshipQ8": "string",
    "NamedIndividualPaticipantQ8": "string",
    "NamedEntityWithoutTreatmentQ8": "string",
    "NamedThirdPartyQ8": "string",
    "NamedEntityQ8": "string",
    "NamedIndividualQ8": "string",
    "ProtectedHealthInformationQ8": "int",
    "SUDinformationQ8": "int",
    "StaffAtHCRCQ8": "int",
    "AdditionalDoc": "string",
    "ChkboxSUDInformationQ2_1": "boolean",
    "ChkboxSUDInformationQ2_2": "boolean",
    "ChkboxSUDInformationQ2_3": "boolean",
    "ChkboxSUDInformationQ2_4": "boolean",
    "ChkboxSUDInformationQ2_5": "boolean",
    "ChkboxSUDInformationQ2_6": "boolean",
    "ChkboxSUDInformationQ2_7": "boolean",
    "ChkboxSUDInformationQ2_8": "boolean",
    "ChkboxSUDInformationQ2_9": "boolean",
    "ChkboxSUDInformationQ2_10": "boolean",
    "ChkboxSUDInformationQ2_11": "boolean",
    "ChkboxProtectedHealthInformationQ2_1": "boolean",
    "ChkboxProtectedHealthInformationQ2_2": "boolean",
    "ChkboxProtectedHealthInformationQ2_3": "boolean",
    "ChkboxProtectedHealthInformationQ2_4": "boolean",
    "ChkboxSUDInformationQ8_1": "boolean",
    "ChkboxSUDInformationQ8_2": "boolean",
    "ChkboxSUDInformationQ8_3": "boolean",
    "ChkboxSUDInformationQ8_4": "boolean",
    "ChkboxSUDInformationQ8_5": "boolean",
    "ChkboxSUDInformationQ8_6": "boolean",
    "ChkboxSUDInformationQ8_7": "boolean",
    "ChkboxSUDInformationQ8_8": "boolean",
    "ChkboxSUDInformationQ8_9": "boolean",
    "ChkboxSUDInformationQ8_10": "boolean",
    "ChkboxSUDInformationQ8_11": "boolean",
    "ChkboxProtectedHealthInformationQ8_1": "boolean",
    "ChkboxProtectedHealthInformationQ8_2": "boolean",
    "ChkboxProtectedHealthInformationQ8_3": "boolean",
    "ChkboxProtectedHealthInformationQ8_4": "boolean",
    "RadioNamedIndividualQ2": "boolean",
    "RadioNamedthirdpartyPayerQ2": "boolean",
    "RadioNamedEntityProvideRelationWithMeQ2": "boolean",
    "RadioNamedEntityWithoutProvideRelationWithMeQ2": "boolean",
    "RadioNamedIndividualParticipantQ2": "boolean",
    "RadioGeneralDesignationQ2": "boolean",
    "RadioNamedIndividualQ8": "boolean",
    "RadioNamedthirdpartyPayerQ8": "boolean",
    "RadioNamedEntityProvideRelationWithMeQ8": "boolean",
    "RadioNamedEntityWithoutProvideRelationWithMeQ8": "boolean",
    "RadioNamedIndividualParticipantQ8": "boolean",
    "RadioGeneralDesignationQ8": "boolean",
    "RadioNamedIndividualQ2select": "int",
    "RadioNamedIndividualParticipantQ2select": "int",
    "RadioNamedIndividualQ8select": "int",
    "RadioNamedIndividualParticipantQ8select": "int",
    "ChkBoxRevokeROIQ2": "boolean",
    "TxtBoxRevokeROIQ2": "string",
    "ChkBoxRevokeROIQ8": "boolean",
    "TxtBoxRevokeROIQ8": "string",
    "NamedEntitesIDQ2": "int",
    "NamedEntitesIDQ8": "int",
    "ChkboxSUDInformationQ2_12": "boolean",
    "ChkboxSUDInformationQ2_13": "boolean",
    "SUDInformationOtherQ2": "string",
    "ChkboxSUDInformationQ8_12": "boolean",
    "ChkboxSUDInformationQ8_13": "boolean",
    "SUDInformationOtherQ8": "string",
    "StaffSignCredentials": "string",
    "DateofRelease": "timestamp",
    "DoYouHaveInsuranceNo": "string",
    "Version": "string",
    "PrimaryReferralSourceNote": "string",
    "SecondaryReferralSource": "string",
    "PrimaryReferralSource": "string",
    "InitialScreening": "int",
    "DDLPrimarySubstance": "int",
    "CurrentlyUsingOpoidDrug": "boolean",
    "MedicatedAssistedTreatmentProgram": "boolean",
    "PainManagementClinic": "boolean",
    "ReceivedTreatmentForAddiction": "boolean",
    "RequireAssistiveTechnologies": "boolean",
    "HaveYouUsedOpoidDrug": "boolean",
    "AnswerAllQuestion": "boolean",
    "AnswerAllQuestionTxt": "string",
    "AppointmentDate": "timestamp",
    "AppointmentTime": "string",
    "ScreenedBy": "string",
    "ScreenedByDate": "timestamp",
    "FollowupScreening": "int",
    "ReviewedResponsesFromCallCenter": "boolean",
    "AnyChangestoInformation": "string",
    "HadAnySuicidalThoughts": "boolean",
    "HadAnySuicidalThoughtsTxt": "string",
    "HadAnyHomicidalThoughts": "boolean",
    "HadAnyHomicidalThoughtsTxt": "string",
    "HaveBeenCarcerated": "boolean",
    "HaveAnyUrgentNeeds": "boolean",
    "AdditionalComments": "string",
    "Comment": "string",
    "ReceiveingAnyTreatment": "int",
    "ReasonforReferral": "string",
    "NA": "boolean",
    "PacketVersion": "string"
}

TARGET_COLUMNS = list(TARGET_SCHEMA.keys())


def empty_result(status, message, site_results=None):
    payload = {
        METHOD_NAME: {
            "method": METHOD_NAME,
            "layer": "SL",
            "status": status,
            "failed_stage": "BR" if status != "SUCCESS" else "",
            "rows_read": 0,
            "rows_inserted": 0,
            "rows_updated": 0,
            "rows_skipped": 0,
            "message": message,
            "site_results": site_results or []
        }
    }
    mssparkutils.notebook.exit(json.dumps(payload))

ctx = parse_json(p_audit_context_json, {})
br_tasks = layer_tasks(ctx, "BR")
sl_tasks = layer_tasks(ctx, "SL")
br_task = first_method_task(br_tasks, METHOD_NAME)
sl_task = first_method_task(sl_tasks, METHOD_NAME)

br_body = request_body(br_task)
sl_body = request_body(sl_task)

bronze_table = br_body.get("full_table") or DEFAULT_BRONZE_TABLE
silver_table = sl_body.get("full_table") or DEFAULT_SILVER_TABLE
ingest_column = br_body.get("ingest_column") or DEFAULT_INGEST_COLUMN
site_column = br_body.get("site_column") or DEFAULT_SITE_COLUMN
database_column = br_body.get("database_column") or DEFAULT_DATABASE_COLUMN
match_keys = sl_body.get("dq_keys") or MATCH_KEYS
match_keys = [c for c in match_keys if c in MATCH_KEYS] or MATCH_KEYS

if not p_ingest_run_id:
    raise Exception("p_ingest_run_id is required for PPA Bronze-to-Silver processing")

active_site_tasks = [t for t in br_tasks if method_of(t).lower() == METHOD_NAME.lower()]
if not active_site_tasks:
    active_site_tasks = br_tasks
active_sites = [
    {
        "site_code": str(t.get("site_code") or t.get("SiteCode") or ""),
        "database_name": str(t.get("data_base_name") or t.get("DataBaseName") or "")
    }
    for t in active_site_tasks
    if (t.get("site_code") or t.get("SiteCode"))
]

bronze_raw_df = spark.table(bronze_table)
if ingest_column not in bronze_raw_df.columns:
    raise Exception(f"Bronze ingest column {ingest_column} not found in {bronze_table}")

run_bronze_df = bronze_raw_df.where(F.col(ingest_column) == F.lit(p_ingest_run_id))
rows_read = run_bronze_df.count()

bronze_site_counts = {}
if site_column in run_bronze_df.columns:
    bronze_site_counts = {
        str(r["site_code"]): int(r["row_count"] or 0)
        for r in (
            run_bronze_df
            .where(F.col(site_column).isNotNull())
            .groupBy(F.col(site_column).cast("string").alias("site_code"))
            .count()
            .withColumnRenamed("count", "row_count")
            .collect()
        )
    }

successful_sites = set(bronze_site_counts.keys())
bronze_failed = bronze_had_method_failure()
site_results = []
for site in active_sites:
    site_code = site["site_code"]
    item = {**site, "rows_written": int(bronze_site_counts.get(site_code, 0))}
    if site_code in successful_sites:
        site_results.append({**item, "status": "SUCCESS"})
    elif bronze_failed:
        site_results.append({
            **item,
            "status": "FAILED",
            "failed_stage": "BR",
            "error_message": "PPA Bronze copy failed or did not write rows for this site/run."
        })
    else:
        site_results.append({**item, "status": "SUCCESS"})

if rows_read == 0:
    if bronze_failed:
        empty_result("SKIPPED", "No successful Bronze rows found for this ingest run; Silver skipped.", site_results)
    empty_result("SUCCESS", "Bronze completed successfully but returned no PPA rows for this ingest run.", site_results)

metadata_to_drop = [c for c in BRONZE_METADATA_COLUMNS if c in run_bronze_df.columns]
payload_df = run_bronze_df.drop(*metadata_to_drop)
payload_df = payload_df.dropDuplicates(match_keys)

for key in match_keys:
    if key not in payload_df.columns:
        raise Exception(f"Required merge key {key} not found in PPA Bronze payload")

payload_df = align_to_target(
    payload_df,
    TARGET_COLUMNS,
    TARGET_SCHEMA,
    BRONZE_SOURCE_BY_TARGET,
)

if table_exists(silver_table):
    silver_df = align_to_target(
        spark.table(silver_table),
        TARGET_COLUMNS,
        TARGET_SCHEMA,
        BRONZE_SOURCE_BY_TARGET,
    )

    target_keys_df = silver_df.select(*match_keys).dropDuplicates()
    payload_keys_df = payload_df.select(*match_keys).dropDuplicates()

    update_df = payload_df.join(target_keys_df, match_keys, "inner")
    insert_df = payload_df.join(target_keys_df, match_keys, "left_anti")
    remaining_df = silver_df.join(payload_keys_df, match_keys, "left_anti")

    rows_updated = update_df.count()
    rows_inserted = insert_df.count()

    final_df = remaining_df.unionByName(update_df).unionByName(insert_df)
else:
    rows_updated = 0
    rows_inserted = payload_df.count()
    final_df = payload_df

(
    final_df.write
    .format("delta")
    .mode("overwrite")
    .option("overwriteSchema", "true")
    .saveAsTable(silver_table)
)

failed_sites = [s for s in site_results if s.get("status") != "SUCCESS"]
status = "FAILED" if failed_sites else "SUCCESS"
message = None if status == "SUCCESS" else "One or more PPA Bronze sites failed. Silver loaded successful Bronze rows only."

payload = {
    METHOD_NAME: {
        "method": METHOD_NAME,
        "layer": "SL",
        "status": status,
        "failed_stage": "BR" if failed_sites else "",
        "rows_read": int(rows_read),
        "rows_inserted": int(rows_inserted),
        "rows_updated": int(rows_updated),
        "rows_skipped": int(rows_read - rows_inserted - rows_updated) if rows_read >= (rows_inserted + rows_updated) else 0,
        "message": message,
        "site_results": site_results
    }
}

mssparkutils.notebook.exit(json.dumps(payload))
