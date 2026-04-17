using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BHG_DWP.Models
{
    [Table("vw_MapAction", Schema = "dms")]
    public class vwSubTask
    {
        [Key]
        public string CompKey { get; set; }
        public string SiteCode { get; set; }
        public long ActionKey { get; set; }
        public int StepKey { get; set; }
        public bool Enabled { get; set; }
        public string ConType { get; set; }
        public int ConnectionId { get; set; }
        public string ConName { get; set; }
        public string ConStr { get; set; }
        public string dbName { get; set; }
        public string CtrlMethod { get; set; }
        public DateTime? EnrollCutoff { get; set; }
        public DateTime? ContractDate { get; set; }
        public string ClinicName { get; set; }
        public bool IsActive { get; set; }
        public bool IsNewSchema { get; set; }
        public string SrcSchema { get; set; }
        public string FromTblVw { get; set; }
        public string DsnSchema { get; set; }
        public string DsnTbl { get; set; }
        public string WhereCondition { get; set; }
        public string SortOrder { get; set; }
        public bool ReInitialize { get; set; }
        public string SchemaVersion { get; set; }
        public bool? RowTrax { get; set; }
        //public string PreCmd { get; set; }
        //public string PostCmd { get; set; }
    }
}
