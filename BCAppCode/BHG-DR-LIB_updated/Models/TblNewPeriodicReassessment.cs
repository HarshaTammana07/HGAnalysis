using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("Tbl_NewPeriodicReassessment", Schema = "pats")]
    public partial class TblNewPeriodicReassessment
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? DataFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? ClientId { get; set; }
        public DateTime? Date { get; set; }
        public string CurrentPathway { get; set; }
        public int? CompletedAt { get; set; }
        public string CompletedAtOthers { get; set; }
        public bool IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public string Version { get; set; }
        //constraint PK_NewPeriodicReassessment primary key(SiteCode ASC, ID ASC)
    }
}
