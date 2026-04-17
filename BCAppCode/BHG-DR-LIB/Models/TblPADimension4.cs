using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPADimension4
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public string MotivationforChange { get; set; }
        public int? TreatmentSatisfaction { get; set; }
        public string TreatmentSatisfactionBox { get; set; }
        public int? EventuallyDiscontinuing { get; set; }
        public int? Discontinuing3to6Months { get; set; }
        public string Strengths { get; set; }
        public string Needs { get; set; }
        public string Abilities { get; set; }
        public string PreferedforTreatment { get; set; }
        public int? Dimension4ASAMRating { get; set; }
    }
}
