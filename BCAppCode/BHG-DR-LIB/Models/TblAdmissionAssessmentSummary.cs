using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblAdmissionAssessmentSummary
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? AdmissionAssessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? Ddlrecommendation { get; set; }
        public int? OpioidTreatmentServices { get; set; }
        public int? WithdrawalManagement { get; set; }
        public string ClinicalSummary { get; set; }
        public int? AsamrecommendationForLevel { get; set; }
        public string LevelOfCareAtVariance { get; set; }
        public string SummaryComments { get; set; }
        public string AdmissionAssessmentStaffSignature { get; set; }
        public string AdmissionAssessmentStaffSignatureBy { get; set; }
        public DateTime? AdmissionAssessmentStaffSignatureDate { get; set; }
        public string AdmissionAssessmentProviderSignature { get; set; }
        public string AdmissionAssessmentProviderSignatureBy { get; set; }
        public DateTime? AdmissionAssessmentProviderSignatureDate { get; set; }
        public string AdmissionAssessmentPatientSignature { get; set; }
        public string AdmissionAssessmentPatientSignatureBy { get; set; }
        public DateTime? AdmissionAssessmentPatientSignatureDate { get; set; }
        public string AdmissionAssessmentSupervisorSignature { get; set; }
        public string AdmissionAssessmentSupervisorSignatureBy { get; set; }
        public DateTime? AdmissionAssessmentSupervisorSignatureDate { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
