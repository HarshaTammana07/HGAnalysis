
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_Orders_2020", Schema = "pats")]
    public partial class TblOrders2020
    {
        [Key]
        [StringLength(25)]
        public string SiteCode { get; set; }
        [Key]
        public int OrderNum { get; set; }
        [Key]
        [Column("cltID")]
        public int CltId { get; set; }
        public int RowChkSum { get; set; }
        public bool? RowState { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime LastModAt { get; set; }
        [Column("medType")]
        [StringLength(50)]
        public string MedType { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DateAdded { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? Orderdate { get; set; }
        [StringLength(50)]
        public string Doctor { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? EffectiveDate { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? ExpirationDate { get; set; }
        public decimal? Dose { get; set; }
        public decimal? Dose2 { get; set; }
        public int? Changeby { get; set; }
        public short? Intervals { get; set; }
        public bool? Sunday { get; set; }
        public bool? Monday { get; set; }
        public bool? Tuesday { get; set; }
        public bool? Wednesday { get; set; }
        public bool? Thursday { get; set; }
        public bool? Friday { get; set; }
        public bool? Saturday { get; set; }
        public bool? Sunday2 { get; set; }
        public bool? Monday2 { get; set; }
        public bool? Tuesday2 { get; set; }
        public bool? Wednesday2 { get; set; }
        public bool? Thursday2 { get; set; }
        public bool? Friday2 { get; set; }
        public bool? Saturday2 { get; set; }
        [StringLength(1000)]
        public string Notes { get; set; }
        public bool? Active { get; set; }
        [StringLength(50)]
        public string Type { get; set; }
        [StringLength(50)]
        public string Stype { get; set; }
        public int? Weeknum { get; set; }
        [Column("splitFIRST")]
        public bool? SplitFirst { get; set; }
        [Column("BLIND")]
        public bool? Blind { get; set; }
        [Column("o_User")]
        [StringLength(100)]
        public string OUser { get; set; }
        [Column("cltM4ID")]
        [StringLength(50)]
        public string CltM4id { get; set; }
        [Column("newdose")]
        public int? Newdose { get; set; }
        [Column("pckcode")]
        [StringLength(50)]
        public string Pckcode { get; set; }
        [Column("rxhistID")]
        [StringLength(50)]
        public string RxhistId { get; set; }
        [Column("EX")]
        public bool? Ex { get; set; }
        [Column("ActbyDATE", TypeName = "datetime")]
        public DateTime? ActbyDate { get; set; }
        [StringLength(100)]
        public string ActByUser { get; set; }
        [Column("white")]
        public bool? White { get; set; }
        [Column("repOldOrder", TypeName = "numeric(18, 0)")]
        public decimal? RepOldOrder { get; set; }
        [Column("sigDr", TypeName = "ntext")]
        public string SigDr { get; set; }
        [Column("dtSig", TypeName = "datetime")]
        public DateTime? DtSig { get; set; }
        [Column("aws")]
        public bool? Aws { get; set; }
        [Column("blSched")]
        public bool? BlSched { get; set; }
        [Column("blVerbal")]
        public bool? BlVerbal { get; set; }
        [StringLength(50)]
        public string Color { get; set; }
        [Column(TypeName = "datetime")]
        public DateTime? DeActbyDate { get; set; }
        [StringLength(100)]
        public string DeActbyUser { get; set; }
        [StringLength(50)]
        public string OrderTypev5 { get; set; }
        [Column("sigentered", TypeName = "ntext")]
        public string Sigentered { get; set; }
        [Column("signoted", TypeName = "ntext")]
        public string Signoted { get; set; }
        [Column("sigNOTEDDT", TypeName = "datetime")]
        public DateTime? SigNoteddt { get; set; }
        [Column("DTMID", TypeName = "datetime")]
        public DateTime? Dtmid { get; set; }
        [Column("sigMID", TypeName = "ntext")]
        public string SigMid { get; set; }
        [StringLength(50)]
        public string OverApprove { get; set; }
        [Column("OverapproveDT")]
        [StringLength(50)]
        public string OverapproveDt { get; set; }
        [Column("sigentereddt", TypeName = "datetime")]
        public DateTime? Sigentereddt { get; set; }
        [Column("sigDrImg")]
        public byte[] SigDrImg { get; set; }
        [Column("sigMidImg")]
        public byte[] SigMidImg { get; set; }
        [Column("sigNotedImg")]
        public byte[] SigNotedImg { get; set; }
    }
}