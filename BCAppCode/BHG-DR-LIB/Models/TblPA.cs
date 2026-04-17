using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblPA
    {
	   public string SiteCode { get; set; }
	   public double Id { get; set; }
	   public double? DataFormId { get; set; }
	   public double? PreAdmissionId { get; set; }
	   public double? ClientId { get; set; }
	   public DateTime? Date { get; set; }
	   public string CurrentPathway { get; set; }
	   public string CurrentPathwayPhase { get; set; }
	   public double? CompletedAt { get; set; }
	   public string CompletedAtOthers { get; set; }
	   public double? IsDeleted { get; set; }
	   public string CreatedBy { get; set; }
	   public DateTime? CreatedOn { get; set; }
	   public string ModifiedBy { get; set; }
	   public DateTime? ModifiedOn { get; set; }
	   public string Version { get; set; }

	}
}
