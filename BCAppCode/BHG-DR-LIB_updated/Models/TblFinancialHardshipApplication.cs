using System;
using System.Collections.Generic;

namespace BHG_DR_LIB.Models
{
    public partial class TblFinancialHardshipApplication
    {
        public string SiteCode { get; set; }
        public DateTime? LastModAt { get; set; }
        public bool? RowState { get; set; }
        public int? RowChkSum { get; set; }
        public int Id { get; set; }
        public int? DataFormId { get; set; }
        public int? PreAdmissionId { get; set; }
        public int? CltId { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedOn { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsIdentification { get; set; }
        public bool? IsIncome { get; set; }
        public string txtIncomeIdentification { get; set; }
        public string FHAPatientSignature { get; set; }
        public DateTime? FHAPatientSignatureDate { get; set; }
        public string FHAPatientSignatureBy { get; set; }
        public string txtAnnualHouseholdIncome { get; set; }
        public string EmergencyName { get; set; }
        public string EmergencyRelation { get; set; }
        public string EmergencyPhone { get; set; }
        public double? txtAUIGross1 { get; set; }
        public double? txtAUIGross2 { get; set; }
        public double? txtAUIGross3 { get; set; }
        public double? txtAUISocial1 { get; set; }
        public double? txtAUISocial2 { get; set; }
        public double? txtAUISocial3 { get; set; }
        public double? txtAUIAlimony1 { get; set; }
        public double? txtAUIAlimony2 { get; set; }
        public double? txtAUIAlimony3 { get; set; }
        public double? txtAUISelf1 { get; set; }
        public double? txtAUISelf2 { get; set; }
        public double? txtAUISelf3 { get; set; }
        public double? txtAUIRent1 { get; set; }
        public double? txtAUIRent2 { get; set; }
        public double? txtAUIRent3 { get; set; }
        public string Version { get; set; }
        public bool? IscurrentlyUninsured { get; set; }
        public string StatusofApplication { get; set; }
        public string Facts { get; set; }
        public string PayClassApproved { get; set; }
        public string ApprovedBy { get; set; }
        public DateTime? EffectiveDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }
}
