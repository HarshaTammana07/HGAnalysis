using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblAppointmentAttend
    {
        public string SiteCode { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public DateTime? LastModAt { get; set; }
        public int AAId { get; set; }
        public int? aaaptID { get; set; }
        public int? aacltid { get; set; }
        public DateTime? aaDTENROLLED { get; set; }
        public DateTime? aaDTREMOVED { get; set; }
    }
}
