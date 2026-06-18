using System;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB.Models
{
    public partial class TblMNTreatmentServiceReview
    {
        public string SiteCode { get; set; }
        public int Id { get; set; }
        public int? DataFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? ClientId { get; set; }
        public DateTime? ReviewPeriod { get; set; } 
        public string SessionStartTime { get; set; }
        public string SessionEndTime { get; set; }
        public DateTime? ServiceDate { get; set; }
        public DateTime? TreatmentServiceReviewMissAppointment {  get; set; }
        public bool? TelehealthSession { get; set; }
        public bool? TreatmentServiceReview { get; set; }
        public string TreatmentGoal { get; set; }
        public bool? AreMethodsEffective { get; set; }
        public string PhysicalMentalHealthProblems { get; set; }
        public string ToxicologyResults { get; set; }
        public string TreatmentPlanning {  get; set; }
        public bool? SignificantTreatmentPlanning { get; set; }
        public bool? TreatmentPlanningChanges { get; set; }
        public bool? AgreeTreatmentPlanningChanges { get; set; }
        public string TreatmentPlanChangesExplain { get; set; }
        public bool? TreatmentServiceReviewReferralsMade { get; set; }
        public bool? TreatmentServiceReviewMHReferralsMade { get; set; }
        public bool? CoordinationWithReferrals {  get; set; }
        public string CoordinationWithReferralsExplain { get; set; }
        public bool? AdmissionVulnerableAdult { get; set; }
        public bool? CurrentlyVulnerableAdult { get; set; }
        public bool? IndividualAbusePrevention { get; set; }
        public string ReasonorAssessmentProcess { get; set; }
        public string StaffSignature { get; set; }
        public DateTime? StaffSignatureDate { get; set; }
        public string StaffSignatureBy {  get; set; }
        public bool? IsDeleted { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public DateTime? ReviewPeriodToday { get; set; }
        public DateTime? LastModAt { get; set; }
        public int? RowState { get; set; }
    }
}
