using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblTakeHomeAgreementandDiversionControl
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public DateTime? LastModAt { get; set; }
        public int? RowState { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? ClientId { get; set; }
        public int? DataFormId { get; set; }
        public string MedicaidID { get; set; }
        public string PatientSignature { get; set; }
        public string PatientSignatureBy { get; set; }
        public DateTime? PatientSignatureDate { get; set; }
        public string StaffSignature { get; set; }
        public string StaffSignatureBy { get;set; }
        public DateTime? StaffSignatureDate { get;set; }
        public bool? IsDeleted { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string Version { get; set; }
        public bool? Patients1 {  get; set; }
        public bool? Patients2 { get; set; }
    }
}
