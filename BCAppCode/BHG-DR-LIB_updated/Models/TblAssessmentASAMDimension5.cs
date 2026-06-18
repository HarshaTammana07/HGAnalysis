using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("tbl_NewAdmissionAssessmentASAMDimension5", Schema = "pats")]
    public partial class TblAssessmentASAMDimension5
    {
        [Key]
        [Column(Order = 1)]
        public string SiteCode { get; set; }
        [Key]
        [Column(Order = 2)]
        public int NewAdmissionAssessmentFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public bool? PeopleInHomeUseSubstances { get; set; }
        public bool? DrugUseCommonInNeighborhood { get; set; }
        public bool? FriendsFamilyInRecovery { get; set; }
        public bool? SupportForRecovery { get; set; }
        public bool? KnowPeerSupportMeetings { get; set; }
        public int? LivingSituation { get; set; }
        public int? FoodWorry { get; set; }
        public int? FoodDidntLast { get; set; }
        public bool? SkippedMedications { get; set; }
        public int? FinancialStrain { get; set; }
        public int? EmploymentHelp { get; set; }
        public bool? SpeakNonEnglishLanguage { get; set; }
        public bool? NeedEducationHelp { get; set; }
        public bool? TransportationProblems { get; set; }
        public int? PhysicalHarmFrequency { get; set; }
        public int? VerbalAbuseFrequency { get; set; }
        public int? ThreatsFrequency { get; set; }
        public int? YellingFrequency { get; set; }
        public bool? Pests { get; set; }
        public bool? Mold { get; set; }
        public bool? LeadPaint { get; set; }
        public bool? LackOfHeat { get; set; }
        public bool? AppliancesNotWorking { get; set; }
        public bool? SmokeDetectorsMissing { get; set; }
        public bool? WaterLeaks { get; set; }
        public bool? None { get; set; }
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
        public int? Dimension5Support { get; set; }
        public int? Dimension5Functioning { get; set; }
        public int? Dimension5Safety { get; set; }
        public bool? RR { get; set; }
        public bool? RowState { get; set; }
        public DateTime? LastModAt { get; set; }
        //CONSTRAINT[PK_tbl_NewAdmissionAssessmentASAMDimension5] primary key(SiteCode ASC, NewAdmissionAssessmentFormId ASC)
    }
}
