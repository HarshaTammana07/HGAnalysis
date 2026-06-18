using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblAdmissionAssessmentDimensionFour
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? AdmissionAssessmentId { get; set; }
        public int? IdontThinkUseDrugsTooMuch { get; set; }
        public int? TryingTtoDrinklessThanUsed { get; set; }
        public int? IenjoyMyDrinking { get; set; }
        public int? IshouldCutDownOnMyDrinking { get; set; }
        public int? WasteOfTimeToThinkAboutMyDrinking { get; set; }
        public int? RecentlyChangedMyDrinking { get; set; }
        public int? AnyoneCanTalkAboutWanting { get; set; }
        public int? ThinkAboutDrinkingLessAlcohol { get; set; }
        public int? MyDrinkingUse { get; set; }
        public int? NoNeedForMeToThinkAbout { get; set; }
        public int? ActuallyChangingMyDrinking { get; set; }
        public int? DrinkingLessAlcohol { get; set; }
        public int? PrecontemplationScale { get; set; }
        public int? ContemplationScale { get; set; }
        public int? ActionScale { get; set; }
        public string StageOfChange { get; set; }
        public string Comments4 { get; set; }
        public int? DdldimensionFourScore { get; set; }
        public int? PreAdmissionId { get; set; }
        public string Dimension4Problems { get; set; }
        public int? StatusofChange { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
