using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPADimension5
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public string Triggers { get; set; }
        public string CopingStrategies { get; set; }
        public int? ContinueUsing { get; set; }
        public string ContinueUsingBox { get; set; }
        public int? EmploymentStatus { get; set; }
        public string EmploymentStatusOther { get; set; }
        public int? PartFullTime { get; set; }
        public int? Arrested { get; set; }
        public int? ChangeinLegalStatus { get; set; }
        public string ChangeinLegalStatusBox { get; set; }
        public int? FinancialTrouble { get; set; }
        public int? Dimension5ASAMRating { get; set; }
    }
}
