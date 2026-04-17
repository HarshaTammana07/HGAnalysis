using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblPADimension3
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int PeriodicReassessmentId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? MentalHealthChange { get; set; }
        public int? MentalHealthHospitalized { get; set; }
        public string MentalHealthHospitalizedbox { get; set; }
        public int? WorseningMentalHealth { get; set; }
        public string WorseningMentalHealthBox { get; set; }
        public bool? Agitation { get; set; }
        public bool? DecreasedPleasure { get; set; }
        public bool? Anxiety { get; set; }
        public bool? LackofInterest { get; set; }
        public bool? Confusion { get; set; }
        public bool? PanicAttacks { get; set; }
        public bool? BrainFog { get; set; }
        public bool? Numbness { get; set; }
        public bool? Insomnia { get; set; }
        public bool? TroubleFallingAsleep { get; set; }
        public bool? TroubleWakingUp { get; set; }
        public bool? Headaches { get; set; }
        public bool? StomachIssues { get; set; }
        public bool? Fatigue { get; set; }
        public bool? Restlessness { get; set; }
        public bool? Tearfulness { get; set; }
        public bool? IncreasedAppetite { get; set; }
        public bool? DecreasedAppetite { get; set; }
        public bool? Feelingempty { get; set; }
        public bool? Irritability { get; set; }
        public bool? Anger { get; set; }
        public bool? GuiltShame { get; set; }
        public bool? MoodSwings { get; set; }
        public bool? DecreasedSelfControl { get; set; }
        public bool? Nightmares { get; set; }
        public bool? DecreasedEnergy { get; set; }
        public bool? IncreasedEnergy { get; set; }
        public bool? LackofFocus { get; set; }
        public bool? Hallucinations { get; set; }
        public bool? Isolation { get; set; }
        public bool? ObsessiveWorryingThoughts { get; set; }
        public bool? LackofMotivation { get; set; }
        public bool? Forgetfulness { get; set; }
        public bool? Nervousness { get; set; }
        public bool? PersistentSadness { get; set; }
        public bool? DisorganizedConfusedThoughts { get; set; }
        public bool? OtherMentalSymptoms { get; set; }
        public string OtherMentalSymptomsBox { get; set; }
        public int? WishedDead { get; set; }
        public string KillingYourself { get; set; }
        public int? Dimension3ASAMRating { get; set; }
    }
}
