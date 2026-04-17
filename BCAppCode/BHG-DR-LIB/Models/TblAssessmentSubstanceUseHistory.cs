using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblAssessmentSubstanceUseHistory
    {
        public string SiteCode { get; set; }
        public int RowChkSum { get; set; }
        public DateTime LastModAt { get; set; }
        public bool RowState { get; set; }
        public int Id { get; set; }
        public int AssessmentFormId { get; set; }
        public int CltId { get; set; }
        public int PreAdmissionId { get; set; }
        public string TxEpisode { get; set; }
        public string SubstanceType { get; set; }
        public string Substance { get; set; }
        public string Route { get; set; }
        public string Amount { get; set; }
        public string FrequencyOfLastUse { get; set; }
        public string PeakUse { get; set; }
        public string AgeOfFirstUse { get; set; }
        public DateTime DateOfLastUse { get; set; }
        public bool Withdrawal { get; set; }
        public string ListSymptoms { get; set; }
        public string Notes { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime DateOfReported {get; set;}
        public int MasterID { get; set; }
    }
}
