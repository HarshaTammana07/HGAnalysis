using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_BamScore", Schema = "pats")]
    public partial class TblBamScore
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int tprID { get; set; }
        public string Description { get; set; }
        public string Score { get; set; }
    }
}
