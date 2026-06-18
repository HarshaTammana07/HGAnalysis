using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BHG_DR_LIB.Models
{
    public partial class TblSMSTextConsentForm
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? DataFormId { get; set; }
        public int? ClientId { get; set; }
        public string ClientName { get; set; }
        public string PhoneNo { get; set; }
        public bool? DoNotAgreetoReceive { get; set; }
        public string PatientSignature { get; set; }
        public DateTime? PatientSignatureDate { get; set; }
        public string PatientSignatureBy { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public string Version { get; set; }
        public bool? Permission { get; set; }
    }
}
