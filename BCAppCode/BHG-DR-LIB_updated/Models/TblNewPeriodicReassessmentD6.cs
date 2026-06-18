using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations.Schema;

namespace BHG_DR_LIB.Models
{
    [Table("Tbl_NewPeriodicReassessmentD6", Schema = "pats")]
    public partial class TblNewPeriodicReassessmentD6
    {
        public string SiteCode { get; set; }
        public int NewPeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public string Motivationformakingorsustainingchanges { get; set; }
        public int? Satisfiedyourprogress { get; set; }
        public string SatisfiedyourprogressExplain { get; set; }
        public int? Iseventuallydiscontinuing { get; set; }
        public int? Planondiscontinuing { get; set; }
        public string WhatStrengthsareusing { get; set; }
        public string WhatNeedsdoyouhave { get; set; }
        public string ListanyAbilities { get; set; }
        public string Haveyoulearnedprefer { get; set; }
        public int? HasTreatmentPreferences { get; set; }
        public string TreatmentPreferences { get; set; }
        public int? WillingToAttendRecommendedCare { get; set; }
        public string NotWillingReason { get; set; }
        public bool? TransportationChallenges { get; set; }
        public bool? FoodHousingInsecurity { get; set; }
        public bool? ChildcareResponsibilities { get; set; }
        public bool? FinancialInsecurity { get; set; }
        public bool? LackEducationEmployment { get; set; }
        public bool? LackJobSecurity { get; set; }
        public bool? LackHealthcareCoverage { get; set; }
        public bool? LackSocialSupports { get; set; }
        public bool? LanguageBarriers { get; set; }
        public bool? RowState { get; set; }
        public DateTime? LastModAt { get; set; }
    }
}
