using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblClaimStatus
    {
        public int id { get; set; }
        public string DatabaseName { get; set; }
        public int? tpcbID { get; set; }
        public DateTime? tpcbDtCreated { get; set; }
        public string tpcbStrSubmitType { get; set; }
        public string tpcb837 { get; set; }
        public string tpcbFILE { get; set; }
        public string FileUploadStatus { get; set; }
    }
}
