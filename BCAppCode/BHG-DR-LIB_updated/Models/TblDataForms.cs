using System;
using System.Collections.Generic;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BHG_DR_LIB.Models
{
    public partial class TblDataForms
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public string FormName { get; set; }
        public string FormURL { get; set; }
        public int PatientId { get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string LastUpdatedBy { get; set; }
        public DateTime? LastUpdatedOn { get; set; }
        public string Program { get; set; }
        public int? EnrollmentId { get; set; }
        public int? dsID { get; set; }
        public DateTime? EnrollmentDate { get; set; }
    }
}
