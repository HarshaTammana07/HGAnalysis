using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPADimension2
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? PhysicalHealthChange { get; set; }
        public int? Called911 { get; set; }
        public string Called911Box { get; set; }
        public int? WorseningMedicalCondition { get; set; }
        public string WorseningMedicalConditionBox { get; set; }
        public int? PrimaryCareProvider { get; set; }
        public string PrimaryCareProviderBox { get; set; }
        public bool UnprotectedSex { get; set; }
        public bool DrugInjection { get; set; }
        public bool SharingDrug { get; set; }
        public int? HIVHepatits { get; set; }
        public string HIVHepatitisBox { get; set; }
        public int? TobaccoNicotine { get; set; }
        public string TobaccoNicotineFrequency { get; set; }
        public int? DiscontinueTobaccoNicotine { get; set; }
        public int? Dimension2ASAMRating { get; set; }
    }
}
