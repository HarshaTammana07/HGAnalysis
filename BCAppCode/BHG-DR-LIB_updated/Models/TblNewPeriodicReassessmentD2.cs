using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("Tbl_NewPeriodicReassessmentD2", Schema = "pats")]
    public partial class TblNewPeriodicReassessmentD2
    {
        public string SiteCode { get; set; }
        public int NewPeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
		public int? PhysicalHealthChange { get; set; }
		public int? Called911 { get; set; }
		public string Called911Box { get; set; }
		public int? WorseningMedicalCondition { get; set; }
		public string WorseningMedicalConditionBox { get; set; }
		public int? PrimaryCareProvider { get; set; }
		public string PrimaryCareProviderBox { get; set; }
		public int? CurrentlyPregnant { get; set; }
		public int? CurrentlyReceivingPrenatalCare { get; set; }
		public string CurrentlyPregnantROIBox { get; set; }
		public bool? UnprotectedSex { get; set; }
		public bool? DrugInjection { get; set; }
		public bool? SharingDrug { get; set; }
		public bool? NoneOfTheAbove { get; set; }
		public int? HIVHepatits { get; set; }
		public string HIVHepatitisBox { get; set; }
		public int? TobaccoNicotine { get; set; }
		public string TobaccoNicotineFrequency { get; set; }
		public int? DiscontinueTobaccoNicotine { get; set; }
		public int? PhysicalHealth { get; set; }
		public int? PregnancyRelatedConcern { get; set; }
		public bool? RowState { get; set; }
		public DateTime? LastModAt { get; set; }
	}
}
