using System;
using System.ComponentModel.DataAnnotations;

namespace BHG_DR_LIB.Models
{
    public partial class VwTaskListMap
    {
        [Key]
        public int TaskId { get; set; }
        public string TaskName { get; set; }
        public DateTime RunAt { get; set; }
        public int ActionKey { get; set; }
        public int ActionStepKey { get; set; }
        public int Status { get; set; }
        public string Duration { get; set; }
        public int OnCompletion { get; set; }
        public int OnError { get; set; }
        public DateTime LastModAt { get; set; }
        public string LastModBy { get; set; }
        public int RowState { get; set; }
        public int? DependentTaskId { get; set; }
        public int? ParentTaskId { get; set; }
        public string SiteCode { get; set; }
        public DateTime? WorkDate { get; set; }
        public int? RowCount { get; set; }
        public string ErrorMessage { get; set; }
        public string ConStr { get; set; }
        public string WhereCondition { get; set; }
        public string SortOrder { get; set; }
        public bool? IsNewSchema { get; set; }
        public string SrcSchema { get; set; }
        public string FromTblVw { get; set; }
        public string SchemaVersion { get; set; }
        public bool? RowTrax { get; set; }
    }
}