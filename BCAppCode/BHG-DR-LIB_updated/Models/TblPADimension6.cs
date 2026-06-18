using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPADimension6
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public string CurrentlyLivingOther { get; set; }
        public int? EnvironmentStability { get; set; }
        public string EnvironmentStabilityBox { get; set; }
        public int? SafefromExploitation { get; set; }
        public string SafefromExploitationBox { get; set; }
        public int? Threats { get; set; }
        public string ThreatsBox { get; set; }
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
        public int? Dimension6ASAMRating { get; set; }
        public bool? LivesAlone { get; set; }
        public bool? HouseApartment { get; set; }
        public bool? LiveKids { get; set; }
        public bool? Shelter { get; set; }
        public bool? LivesPartnerSpouse { get; set; }
        public bool? SoberLivingHome { get; set; }
        public bool? LivesFamily { get; set; }
        public bool? Unhoused { get; set; }
        public bool? LivesFriends { get; set; }
        public bool? Other { get; set; }
    }
}
