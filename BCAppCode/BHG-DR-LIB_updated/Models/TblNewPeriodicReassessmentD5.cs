using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("Tbl_NewPeriodicReassessmentD5", Schema = "pats")]
    public partial class TblNewPeriodicReassessmentD5
    {
        public string SiteCode { get; set; }
        public int NewPeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? Children { get; set; }
        public int? ChildrenAge { get; set; }
        public string ChildrenAgeBox { get; set; }
        public int? ChildrenLegalCustody { get; set; }
        public int? ChildFamilyServicesOpenCases { get; set; }
        public int? FriendsFamilySupport { get; set; }
        public int? EnoughMoney { get; set; }
        public int? FamilyFriendsinRecovery { get; set; }
        public int? CurrentlyConnectedSupport { get; set; }
        public string CurrentlyConnectedSupportBox { get; set; }
        public int? Barriers { get; set; }
        public string BarriersBox { get; set; }
        public int? LivingSituationToday { get; set; }
        public bool? Pests { get; set; }
        public bool? Mold { get; set; }
        public bool? LeadPaintPipes { get; set; }
        public bool? LackofHeat { get; set; }
        public bool? OvenOrStove { get; set; }
        public bool? SmokeDetectors { get; set; }
        public bool? Waterleaks { get; set; }
        public bool? NoneOfAbove { get; set; }
        public int? LastassessmentWorried { get; set; }
        public int? LastassessmentFoodBought { get; set; }
        public int? LastassessmentSkipMedications { get; set; }
        public int? HardToPay { get; set; }
        public int? FindingOrKeepingWork { get; set; }
        public int? SpeakLanguage { get; set; }
        public int? SchoolTraining { get; set; }
        public int? LackOfTransportation { get; set; }
        public int? AnyoneHurtYou { get; set; }
        public int? InsultOrTalkDown { get; set; }
        public int? ThreatenWithHarm { get; set; }
        public int? ScreamOrCurseAtYou { get; set; }
        public int? EffectivelyinCurrentEnvironment { get; set; }
        public int? SafetyCurrentEnvironment { get; set; }
        public int? SupportCurrentEnvironment { get; set; }
        public bool? Level1 { get; set; }
        public bool? Level1_5 { get; set; }
        public bool? Level1_7 { get; set; }
        public bool? Level2_1 { get; set; }
        public bool? Level2_5 { get; set; }
        public bool? Level2_7 { get; set; }
        public bool? Level3_1 { get; set; }
        public bool? Level3_5 { get; set; }
        public bool? Level3_7 { get; set; }
        public bool? NonBIO { get; set; }
        public bool? BIO { get; set; }
        public bool? Level4 { get; set; }
        public bool? COE { get; set; }
        public bool? RR { get; set; }
        public bool? RowState { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
