using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblMNComprehensiveAssessmentLevelOfCare
    {
        public string SiteCode { get; set; }
        public int MNComprehensiveAssessmentFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? SymptomsUrgentlyAddressed { get; set; }
        public string SymptomsUrgentlyAddressedExplain { get; set; }
        public bool? RisksofOpioid { get; set; }
        public bool? TreatmentOptions { get; set; }
        public bool? RisksofrecognitionOpioidOverdose { get; set; }
        public bool? AvailabilityAdministration { get; set; }
        public bool? Other { get; set; }
        public string OtherTxt { get; set; }
        public bool? LevelofCareRecommendation1 { get; set; }
        public bool? LevelofCareRecommendation21 { get; set; }
        public bool? LevelofCareRecommendation31 { get; set; }
        public bool? LevelofCareRecommendation33 { get; set; }
        public bool? LevelofCareRecommendation35 { get; set; }
        public bool? LevelofCareRecommendation37 { get; set; }
        public bool? LevelofCareRecommendation4 { get; set; }
        public int? OpioidTreatmentServices { get; set; }
        public int? WithdrawalManagement { get; set; }
        public int? ASAMRecommendation { get; set; }
        public bool? NALOC { get; set; }
        public bool? LOCNotAvailable { get; set; }
        public bool? ClinicianJudgment { get; set; }
        public bool? Patientpreference { get; set; }
        public bool? PatientWaitingForLOC { get; set; }
        public bool? RecommendedLOCAvailable { get; set; }
        public bool? Geographicaccessibility { get; set; }
        public bool? Familycaregiverresponsibilities { get; set; }
        public bool? EmploymentResponsibilities { get; set; }
        public bool? Courttreatmentrequirements { get; set; }
        public bool? Lackofphysicalaccess { get; set; }
        public bool? Languageaccessibility { get; set; }
        public bool? LOCIsAvailable { get; set; }
        public bool? Patientisineligible { get; set; }
        public string AdditionalComments { get; set; }
        public bool? LOCOther { get; set; }
        public string LOCIsAvailableReason { get; set; }
        public string PatientisineligibleReason { get; set; }
        public string OtherReason { get; set; }
        public bool? LevelofCareRecommendation25 { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
