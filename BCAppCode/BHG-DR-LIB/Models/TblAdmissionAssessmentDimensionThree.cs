using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblAdmissionAssessmentDimensionThree
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? AdmissionAssessmentId { get; set; }
        public int? HaveYouEverBeenKnockedUnconscious { get; set; }
        public int? DidYouAttendSpecialEducation { get; set; }
        public string Comments3 { get; set; }
        public bool? Anxiety { get; set; }
        public bool? GeneralizedAnxietyDisorder { get; set; }
        public bool? SocialPhobia { get; set; }
        public bool? PanicDisorder { get; set; }
        public bool? Agoraphobia { get; set; }
        public bool? PostTraumaticStressDisorder { get; set; }
        public bool? ObsessiveCompulsiveDisorder { get; set; }
        public bool? Depression { get; set; }
        public bool? BipolarDisorder { get; set; }
        public bool? Schizophrenia { get; set; }
        public bool? SchizoaffectiveDisorder { get; set; }
        public int? HospitalizedForMentalHealth { get; set; }
        public int? HowManyTimes { get; set; }
        public string MostRecentHospitalization { get; set; }
        public string DiagnosedComment3 { get; set; }
        public int? DdldimensionThreeScore { get; set; }
        public int? PreAdmissionId { get; set; }
        public string DoYouHaveApsychiatristTxt { get; set; }
        public int? DoYouHaveApsychiatrist { get; set; }
        public string Dimension3Problems { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
