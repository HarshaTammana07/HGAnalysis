using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblPayerCltHistory
    {
        public string SiteCode { get; set; }
        public int PchId { get; set; }
        public int PyId { get; set; }
        public string PyChange { get; set; }
        public DateTime PyDtm { get; set; }
        public string PyUser { get; set; }
        public string PyNote { get; set; }
    }
}
