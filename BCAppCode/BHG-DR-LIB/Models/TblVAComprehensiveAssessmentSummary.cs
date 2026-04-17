using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblVAComprehensiveAssessmentSummary
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int VAComprehensiveAssessmentId { get; set; }
        public int PreAdmissionId { get; set; }
        public int DDLRecommendation { get; set; }
        public int OpioidTreatmentServices { get; set; }
        public int WithdrawalManagement { get; set; }
        public string ClinicalSummary { get; set; }
        public int ASAMRecommendationForLevel { get; set; }
        public string LevelOfCareAtVariance { get; set; }
        public string SummaryComments { get; set; }
    }
}
