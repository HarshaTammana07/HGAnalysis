using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("Tbl_NewPeriodicReassessmentD4", Schema = "pats")]
    public partial class TblNewPeriodicReassessmentD4
    {
        public string SiteCode { get; set; }
        public int NewPeriodicReassessmentId { get; set; }
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
        public int? RiskySubstanceUse { get; set; }
        public int? RiskySUDRelatedBehaviors { get; set; }
        public bool? RowState { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
