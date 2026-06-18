using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("Tbl_NewPeriodicReassessmentCounselorReview", Schema = "pats")]
    public partial class TblNewPeriodicReassessmentCounselorReview
    {
        public string SiteCode { get; set; }
        public int NewPeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
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
        public string ReasonWillNotAdmitReason { get; set; }
        public string ReasonPatientIneligibleReason { get; set; }
        public string ReasonOtherReason { get; set; }
        public bool? CopePhase1 { get; set; }
        public bool? CopePhase2 { get; set; }
        public bool? CopePhase3 { get; set; }
        public bool? Induction { get; set; }
        public bool? Stabilization { get; set; }
        public bool? Maintenance { get; set; }
        public DateTime? DateCompleted { get; set; }
        public int? UseScore { get; set; }
        public int? RiskScore { get; set; }
        public int? ProtectiveScore { get; set; }
        public string ClinicalSummary { get; set; }
        public string PatientSignature { get; set; }
        public string PatientSignatureBy { get; set; }
        public DateTime? PatientSignatureDate { get; set; }
        public string CounselorSignature { get; set; }
        public string CounselorSignatureBy { get; set; }
        public DateTime? CounselorSignatureDate { get; set; }
        public string ProviderSignature { get; set; }
        public string ProviderSignatureBy { get; set; }
        public DateTime? ProviderSignatureDate { get; set; }
        public string SupervisorSignature { get; set; }
        public string SupervisorSignatureBy { get; set; }
        public DateTime? SupervisorSignatureDate { get; set; }
        public bool? RR { get; set; }
        //constraint PK_NewPeriodicReassessmentCounselorReview primary key(SiteCode ASC, NewPeriodicReassessmentId ASC)
    }
}
