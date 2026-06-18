using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPADimension1
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public string LastUDS { get; set; }
        public string UDSResult { get; set; }
        public int? IllegalSubstances { get; set; }
        public string IllegalSubstancesBox { get; set; }
        public int? Overdose { get; set; }
        public string OverdoseBox { get; set; }
        public int? NarcanAvailable { get; set; }
        public int? Cravings { get; set; }
        public int? CravingRating { get; set; }
        public int? Dimension1ASAMRating { get; set; }
        public string UAEval { get; set; }
    }
}
