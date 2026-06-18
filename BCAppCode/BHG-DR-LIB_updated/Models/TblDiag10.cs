using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_tbldiag10", Schema = "pats")]

    public class TblDiag10
    {
        public string SiteCode { get; set; }
        public int dgID { get; set; }
        public int? dgCLTID { get; set; }
        public string dgDIAG { get; set; }
        public string dgDESC { get; set; }
        public DateTime? dgDATE { get; set; }
        public string dgSTAFF { get; set; }
        public DateTime? dgdt { get; set; }
        public bool? dgPRIMARY { get; set; }
        public string dgDIAG10 { get; set; }
        public string dgDIAG10Description { get; set; }
        public string dgNote { get; set; }
        public string dgType { get; set; }
        public int? EnrollmentId { get; set; }
        public DateTime? dgEndDate { get; set; }
        public bool? RowState { get; set; }
        public DateTime? LastModAt { get; set; }
    //CONSTRAINT[PK_tbldiag10] PRIMARY KEY CLUSTERED(SiteCode ASC, [dgID] ASC)
    }
}
