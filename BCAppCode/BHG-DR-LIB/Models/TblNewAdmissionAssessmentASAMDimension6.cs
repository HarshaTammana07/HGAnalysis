using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblNewAdmissionAssessmentASAMDimension6
    {
        public string SiteCode { get; set; }
        public int NewAdmissionAssessmentFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? ReadinessQuestion1 { get; set; }
        public int? ReadinessQuestion2 { get; set; }
        public int? ReadinessQuestion3 { get; set; }
        public int? ReadinessQuestion4 { get; set; }
        public int? ReadinessQuestion5 { get; set; }
        public int? ReadinessQuestion6 { get; set; }
        public int? ReadinessQuestion7 { get; set; }
        public int? ReadinessQuestion8 { get; set; }
        public int? ReadinessQuestion9 { get; set; }
        public int? ReadinessQuestion10 { get; set; }
        public int? ReadinessQuestion11 { get; set; }
        public int? ReadinessQuestion12 { get; set; }
        public string StageOfChange { get; set; }
        public string AdditionalComments { get; set; }
        public string TreatmentPreferences { get; set; }
        public string ReasonNotWillingToAttend { get; set; }
        public string ReasonWillNotAdmitReason { get; set; }
        public string ReasonPatientIneligibleReason { get; set; }
        public string ReasonOtherReason { get; set; }
        public string ClinicalSummary { get; set; }
        public bool? HasTreatmentPreferences { get; set; }
        public bool? WillingToAttendRecommendedCare { get; set; }
        public bool? TransportationChallenges { get; set; }
        public bool? FoodHousingInsecurity { get; set; }
        public bool? ChildcareResponsibilities { get; set; }
        public bool? FinancialInsecurity { get; set; }
        public bool? LackEmploymentOpportunities { get; set; }
        public bool? LackJobSecurity { get; set; }
        public bool? LackHealthcareCoverage { get; set; }
        public bool? LackSocialSupports { get; set; }
        public bool? LanguageBarriers { get; set; }
        public bool? Level1 { get; set; }
        public bool? Level1_5 { get; set; }
        public bool? Level1_7 { get; set; }
        public bool? Level2_1 { get; set; }
        public bool? Level2_5 { get; set; }
        public bool? Level2_7 { get; set; }
        public bool? Level3_1 { get; set; }
        public bool? Level3_5 { get; set; }
        public bool? Level3_7 { get; set; }
        public bool? NonBIO { get; set; }
        public bool? BIO { get; set; }
        public bool? Level4 { get; set; }
        public bool? COE { get; set; }
        public bool? ReasonNotAligned { get; set; }
        public bool? ReasonNotAvailable { get; set; }
        public bool? ReasonClinicianJudgment { get; set; }
        public bool? ReasonPatientPreference { get; set; }
        public bool? ReasonOnWaitingList { get; set; }
        public bool? ReasonLacksPayment { get; set; }
        public bool? ReasonGeographicAccess { get; set; }
        public bool? ReasonCaregiverResponsibilities { get; set; }
        public bool? ReasonEmploymentResponsibilities { get; set; }
        public bool? ReasonCourtRequirements { get; set; }
        public bool? ReasonTransportationChallenges { get; set; }
        public bool? ReasonLanguageAccessibility { get; set; }
        public bool? ReasonWillNotAdmit { get; set; }
        public bool? ReasonPatientIneligible { get; set; }
        public bool? ReasonOther { get; set; }
        public string PatientSignature { get; set; }
        public string PatientSignatureBy { get; set; }
        public string SupervisorSignature { get; set; }
        public string SupervisorSignatureBy { get; set; }
        public string CounselorSignature { get; set; }
        public string CounselorSignatureBy { get; set; }
        public string ProviderSignature { get; set; }
        public string ProviderSignatureBy { get; set; }
        public bool? SuperviosorSignNA { get; set; }
        public DateTime? PatientSignatureDate { get; set; }
        public DateTime? SupervisorSignatureDate { get; set; }
        public DateTime? CounselorSignatureDate { get; set; }
        public DateTime? ProviderSignatureDate { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
