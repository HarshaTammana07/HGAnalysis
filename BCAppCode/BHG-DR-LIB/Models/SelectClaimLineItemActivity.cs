using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_ClaimLineItemActivity", Schema = "pats")]
    public partial class SelectClaimLineItemActivity
    {
        [Key]
        [StringLength(25)]
        public string SiteCode { get; set; }
        [Key]
        [Column("liaID")]
        public int LiaId { get; set; }
        public bool RowState { get; set; }
    }
}
