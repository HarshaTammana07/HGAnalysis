using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_NewAdmissionAssessmentASAMDimension4", Schema = "pats")]
    public partial class TblNewAdmissionAssessmentASAMDimension4
    {
        [Key]
        [Column(Order = 1)]
        public string SiteCode { get; set; }
        [Key]
        [Column(Order = 2)]
        public int NewAdmissionAssessmentFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public bool? HadAnOverdose { get; set; }
        public bool? HaveNarcanAvailable { get; set; }
        public bool? HaveYouCalled911 { get; set; }
        public bool? HaveSubstanceUsePutYouInDanger { get; set; }
        public bool? HaveYourPhysicalHealthWorse { get; set; }
        public bool? YourPhysicalMentalWorse { get; set; }
        public bool? CausedProblemsAtYourJob { get; set; }
        public bool? HavingAnyFinancialTrouble { get; set; }
        public bool? JeopardizedYourHousing { get; set; }
        public bool? HavingProblemInRelationship { get; set; }
        public bool? DoesYourTemperCauseProblem { get; set; }
        public bool? HaveYouBeenArrested { get; set; }
        public bool? SubstanceUsePutYouAtRiskOfArrested { get; set; }
        public bool? HaveAnyOpenCourtCases { get; set; }
        public bool? OnProbation { get; set; }
        public int? HaveLeggalCustodyOfChildren { get; set; }
        public int? HaveAnyCasesWithLocalDepartment { get; set; }
        public bool? AnyChildrenLiveInHome { get; set; }
        public string AddtionalComments { get; set; }
        public int? Dimension4SUDRisk { get; set; }
        public int? Dimension4SUDRiskBehaviours { get; set; }
        public bool? RowState { get; set; }
        public DateTime? LastModAt { get; set; }
        // CONSTRAINT[PK_tbl_NewAdmissionAssessmentASAMDimension4] primary key(SiteCode ASC, NewAdmissionAssessmentFormId ASC)
    }
}
