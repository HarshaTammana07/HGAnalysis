using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_ClaimLineItem", Schema = "pats")]
    public partial class SelectClaimLineItem
    {
        [Key]
        [StringLength(25)]
        public string SiteCode { get; set; }
        [Key]
        [Column("tpcliID")]
        public int TpcliId { get; set; }
        public bool RowState { get; set; }
    }
}
