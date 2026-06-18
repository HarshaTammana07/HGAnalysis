using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblConsenttoMarketing
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? DataFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? ClientId { get; set; }
        public bool? Text { get; set; }
        public bool? Email { get; set; }
        public bool? Phone { get; set; }
        public string PatientSignature { get; set; }
        public string PatientSignatureBy { get; set; }
        public DateTime? PatientSignatureDate { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public DateTime? LastModAt { get; set; }
        public int? RowState { get; set; }
    }
}
