using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPACounselorReview
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public bool? EarlyIntervention { get; set; }
        public bool? OutpatientTreatment { get; set; }
        public bool? IntensiveOutpatient { get; set; }
        public bool? PartialHospitalization { get; set; }
        public bool? ResidentialInpatient { get; set; }
        public bool? MedManagedIntensiveInpatient { get; set; }
        public bool? OTS { get; set; }
        public bool? OBOT { get; set; }
        public bool? OTP { get; set; }
        public bool? OBAT { get; set; }
        public bool? WithdrawalManagement { get; set; }
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
        public string SupervisorSignature { get; set; }
        public string SupervisorSignatureBy { get; set; }
        public DateTime? SupervisorSignatureDate { get; set; }
    }
}
