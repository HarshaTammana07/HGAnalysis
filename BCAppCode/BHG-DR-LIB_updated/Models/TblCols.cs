using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    public partial class TblCols
    {
        [Key]
        public string ColName { get; set; }
        public int ColID { get; set; }
    }
}
