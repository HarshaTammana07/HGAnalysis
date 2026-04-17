using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblNewAdmissionAssessment
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? PreAdmissionId { get; set; }
        public int DataFormId { get; set; }
        public int ClientId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedOn { get; set; }
        public string ModifiedBy { get; set; }
        public bool IsDeleted { get; set; }
        public string Version { get; set; }
        public DateTime LastModAt { get; set; }
    }
}
