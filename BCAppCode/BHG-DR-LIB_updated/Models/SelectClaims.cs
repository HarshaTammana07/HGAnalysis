using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_Claims", Schema = "pats")]
    public partial class SelectClaims
    {
        [Key]
        [StringLength(25)]
        public string SiteCode { get; set; }
        [Key]
        [Column("tpcID")]
        public int TpcId { get; set; }
        public bool RowState { get; set; }

    }
}
