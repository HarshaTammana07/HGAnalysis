using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblTakeHomeRiskAssessment
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? DataFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? ClientId { get; set; }
        public int? TakeHomeDosesUnsafe { get; set; }
        public int? LastDoseIncrease { get; set; }
        public int? LikelihoodOfUsingMedication { get; set; }
        public int? SafeguardingMedication { get; set; }
        public int? AbstainingFromOtherSubstances { get; set; }
        public int? LogisticalBarriers { get; set; }
        public string TotalScore { get; set; }
        public string StaffSignature { get; set; }
        public DateTime? StaffSignatureDate { get; set; }
        public string StaffSignatureBy { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }

    }
}
