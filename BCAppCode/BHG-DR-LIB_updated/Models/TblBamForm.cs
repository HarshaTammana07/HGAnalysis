using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_BamForm", Schema = "pats")]
    public partial class TblBamForm
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int PreAdmissionId { get; set; }
        public int ClientId { get; set; }
        public int DataFormId { get; set; }
        public DateTime? BAMDate { get; set; }
        public string InterviewerID { get; set; }
        public bool ClinicianInterview { get; set; }
        public bool SelfReport { get; set; }
        public bool Phone { get; set; }
        public DateTime TimeStarted { get; set; }
        public int InstructionsQ1 { get; set; }
        public string InstructionsQ1Txt { get; set; }
        public int InstructionsQ2 { get; set; }
        public string InstructionsQ2Txt { get; set; }
        public int InstructionsQ3 { get; set; }
        public string InstructionsQ3Txt { get; set; }
        public int InstructionsQ4 { get; set; }
        public string InstructionsQ4Txt { get; set; }
        public int InstructionsQ5 { get; set; }
        public string InstructionsQ5Txt { get; set; }
        public int InstructionsQ6 { get; set; }
        public string InstructionsQ6Txt { get; set; }
        public int InstructionsQ7A { get; set; }
        public int InstructionsQ7B { get; set; }
        public int InstructionsQ7C { get; set; }
        public int InstructionsQ7D { get; set; }
        public int InstructionsQ7E { get; set; }
        public int InstructionsQ7F { get; set; }
        public int InstructionsQ7G { get; set; }
        public int InstructionsQ8 { get; set; }
        public string InstructionsQ8Txt { get; set; }
        public int InstructionsQ9 { get; set; }
        public string InstructionsQ9Txt { get; set; }
        public int InstructionsQ10 { get; set; }
        public string InstructionsQ10Txt { get; set; }
        public int InstructionsQ11 { get; set; }
        public string InstructionsQ11Txt { get; set; }
        public int InstructionsQ12 { get; set; }
        public string InstructionsQ12Txt { get; set; }
        public int InstructionsQ13 { get; set; }
        public string InstructionsQ13Txt { get; set; }
        public int InstructionsQ14 { get; set; }
        public string InstructionsQ14Txt { get; set; }
        public int InstructionsQ15 { get; set; }
        public string InstructionsQ15Txt { get; set; }
        public int InstructionsQ16 { get; set; }
        public string InstructionsQ16Txt { get; set; }
        public int InstructionsQ17 { get; set; }
        public string InstructionsQ17Txt { get; set; }
        public DateTime? TimeFinished { get; set; }
        public string SubscaleScoreTxt1 { get; set; }
        public string SubscaleScoreTxt2 { get; set; }
        public string SubscaleScoreTxt3 { get; set; }
        public string StaffSignature { get; set; }
        public string StaffSignatureBy { get; set; }
        public DateTime? StaffSignatureDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool IsDeleted { get; set; }
        public string Version { get; set; }

    }
}
