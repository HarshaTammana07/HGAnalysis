using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_NewAdmissionAssessmentASAMDimension2", Schema = "pats")]
    public partial class TblNewAdmissionAssessmentASAMDimension2
    {
        [Key]
        [Column(Order = 1)] 
        public string SiteCode { get; set; }
        [Key]
        [Column(Order = 2)] 
        public int NewAdmissionAssessmentFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public bool? HighBloodPressure { get; set; }
        public bool? Cancer { get; set; }
        public bool? COPD { get; set; }
        public bool? HepatitisB { get; set; }
        public bool? Diabetes { get; set; }
        public bool? Deafness { get; set; }
        public bool? Blindness { get; set; }
        public bool? HepatitisC { get; set; }
        public bool? HeartDisease { get; set; }
        public bool? ChronicPain { get; set; }
        public bool? PoorVision { get; set; }
        public bool? HepatitisD { get; set; }
        public bool? Epilepsy { get; set; }
        public bool? KidneyDisease { get; set; }
        public bool? HearingLoss { get; set; }
        public bool? HIV { get; set; }
        public bool? Tuberculosis { get; set; }
        public bool? Asthma { get; set; }
        public bool? HepatitisA { get; set; }
        public bool? LiverDisease { get; set; }
        public bool? Otherchk { get; set; }
        public string Other { get; set; }
        public string AdditionalComments1 { get; set; }
        public string AllergiesComments { get; set; }
        public string PrimaryCareProvider { get; set; }
        public string AdditionalComments { get; set; }
        public bool? HasPrimaryCare { get; set; }
        public int? IsPregnant { get; set; }
        public bool? MedicalProblemsPreventClinic { get; set; }
        public bool? ReceivingPrenatalCare { get; set; }
        public int? Dimension2Pregnancy { get; set; }
        public int? Dimension2PhysicalHealth { get; set; }
        public string Dimension2Problems { get; set; }
        public bool? RowState { get; set; }
        public DateTime LastModAt { get; set; }
        //CONSTRAINT[PK_tbl_NewAdmissionAssessmentASAMDimension2] primary key(SiteCode ASC, NewAdmissionAssessmentFormId ASC)
    }
}
