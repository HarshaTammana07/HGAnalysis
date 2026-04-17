using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveMNCA(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblMNComprehensiveAssessment> NewItems = new List<Models.TblMNComprehensiveAssessment>();
                    List<Models.TblMNComprehensiveAssessment> dbList = db.TblMNComprehensiveAssessments.Where(x => x.SiteCode == sc).ToList();
                    foreach(DataRow r in tbl.Rows)
                    {
                        Models.TblMNComprehensiveAssessment ca = new Models.TblMNComprehensiveAssessment();
                        foreach(DataColumn c in tbl.Columns)
                        {
                            switch(c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    break;
                                case "id":
                                    ca.Id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dataformid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "todaydate":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TodayDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "referradby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReferradBy = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "referradbyother":
                                    ca.ReferradByOther = r[c.ColumnName].ToString();
                                    break;
                                case "referralreason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReferralReason = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "referralreasonother":
                                    ca.ReferralReasonOther = r[c.ColumnName].ToString();
                                    break;
                                case "insuranceid":
                                    ca.InsuranceId = r[c.ColumnName].ToString();
                                    break;
                                case "createdby":
                                    ca.CreatedBy = r[c.ColumnName].ToString();
                                    break;
                                case "createdon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedby":
                                    ca.ModifiedBy = r[c.ColumnName].ToString();
                                    break;
                                case "modifiedon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "version":
                                    ca.Version = r[c.ColumnName].ToString();
                                    break;
                                case "isdeleted":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblMNComprehensiveAssessment dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.ClientId = ca.ClientId;
                            dbca.CreatedBy = ca.CreatedBy;
                            dbca.CreatedOn = ca.CreatedOn;
                            dbca.DataFormId = ca.DataFormId;
                            dbca.InsuranceId = ca.InsuranceId;
                            dbca.IsDeleted = ca.IsDeleted;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.ModifiedBy = ca.ModifiedBy;
                            dbca.ModifiedOn = ca.ModifiedOn;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.ReferradBy = ca.ReferradBy;
                            dbca.ReferradByOther = ca.ReferradByOther;
                            dbca.ReferralReason = ca.ReferralReason;
                            dbca.ReferralReasonOther = ca.ReferralReasonOther;
                            dbca.TodayDate = ca.TodayDate;
                            dbca.Version = ca.Version;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblMNComprehensiveAssessments.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveMNCALOC (DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblMNComprehensiveAssessmentLevelOfCare> NewItems = new List<Models.TblMNComprehensiveAssessmentLevelOfCare>();
                    List<Models.TblMNComprehensiveAssessmentLevelOfCare> dbList = db.TblMNComprehensiveAssessmentLevelOfCares.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblMNComprehensiveAssessmentLevelOfCare ca = new Models.TblMNComprehensiveAssessmentLevelOfCare();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    break;
                                case "mncomprehensiveassessmentformid":
                                    ca.MNComprehensiveAssessmentFormId = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "symptomsurgentlyaddressed":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SymptomsUrgentlyAddressed = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "symptomsurgentlyaddressedexplain":
                                    ca.SymptomsUrgentlyAddressedExplain = r[c.ColumnName].ToString();
                                    break;
                                case "risksofopioid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RisksofOpioid = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "treatmentoptions":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TreatmentOptions = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "risksofrecognitionopioidoverdose":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RisksofrecognitionOpioidOverdose = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "availabilityadministration":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AvailabilityAdministration = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "other":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Other = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "othertxt":
                                    ca.OtherTxt = r[c.ColumnName].ToString();
                                    break;
                                case "levelofcarerecommendation1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation21":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation21 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation31":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation31 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation33":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation33 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation35":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation35 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation37":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation37 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation4":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation4 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "opioidtreatmentservices":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OpioidTreatmentServices = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "withdrawalmanagement":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WithdrawalManagement = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "asamrecommendation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ASAMRecommendation = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "naloc":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NALOC = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "locnotavailable":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LOCNotAvailable = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clinicianjudgment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClinicianJudgment = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientpreference":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Patientpreference = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientwaitingforloc":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientWaitingForLOC = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "recommendedlocavailable":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RecommendedLOCAvailable = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "geographicaccessibility":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Geographicaccessibility = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "familycaregiverresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Familycaregiverresponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "employmentresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.EmploymentResponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "courttreatmentrequirements":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Courttreatmentrequirements = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackofphysicalaccess":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Lackofphysicalaccess = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "languageaccessibility":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Languageaccessibility = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "locisavailable":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LOCIsAvailable = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientisineligible":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Patientisineligible = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "additionalcomments":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AdditionalComments = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "locother":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LOCOther = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "LOCIsAvailableReason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LOCIsAvailableReason = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientisineligiblereason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientisineligibleReason = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "otherreason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OtherReason = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcarerecommendation25":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelofCareRecommendation25 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblMNComprehensiveAssessmentLevelOfCare dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode 
                            && x.MNComprehensiveAssessmentFormId == ca.MNComprehensiveAssessmentFormId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.AdditionalComments = ca.AdditionalComments;
                            dbca.ASAMRecommendation = ca.ASAMRecommendation;
                            dbca.AvailabilityAdministration = ca.AvailabilityAdministration;
                            dbca.ClinicianJudgment = ca.ClinicianJudgment;
                            dbca.Courttreatmentrequirements = ca.Courttreatmentrequirements;
                            dbca.EmploymentResponsibilities = ca.EmploymentResponsibilities;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.Familycaregiverresponsibilities = ca.Familycaregiverresponsibilities;
                            dbca.Geographicaccessibility = ca.Geographicaccessibility;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.Lackofphysicalaccess = ca.Lackofphysicalaccess;
                            dbca.Languageaccessibility = ca.Languageaccessibility;
                            dbca.LevelofCareRecommendation1 = ca.LevelofCareRecommendation1;
                            dbca.LevelofCareRecommendation21 = ca.LevelofCareRecommendation21;
                            dbca.LevelofCareRecommendation25 = ca.LevelofCareRecommendation25;
                            dbca.LevelofCareRecommendation31 = ca.LevelofCareRecommendation31;
                            dbca.LevelofCareRecommendation33 = ca.LevelofCareRecommendation33;
                            dbca.LevelofCareRecommendation35 = ca.LevelofCareRecommendation35;
                            dbca.LevelofCareRecommendation37 = ca.LevelofCareRecommendation37;
                            dbca.LevelofCareRecommendation4 = ca.LevelofCareRecommendation4;
                            dbca.LOCIsAvailable = ca.LOCIsAvailable;
                            dbca.LOCIsAvailableReason = ca.LOCIsAvailableReason;
                            dbca.LOCNotAvailable = ca.LOCNotAvailable;
                            dbca.LOCOther = ca.LOCOther;
                            dbca.NALOC = ca.NALOC;
                            dbca.OpioidTreatmentServices = ca.OpioidTreatmentServices;
                            dbca.Other = ca.Other;
                            dbca.OtherReason = ca.OtherReason;
                            dbca.OtherTxt = ca.OtherTxt;
                            dbca.Patientisineligible = ca.Patientisineligible;
                            dbca.PatientisineligibleReason = ca.PatientisineligibleReason;
                            dbca.Patientpreference = ca.Patientpreference;
                            dbca.PatientWaitingForLOC = ca.PatientWaitingForLOC;
                            dbca.RecommendedLOCAvailable = ca.RecommendedLOCAvailable;
                            dbca.RisksofOpioid = ca.RisksofOpioid;
                            dbca.RisksofrecognitionOpioidOverdose = ca.RisksofrecognitionOpioidOverdose;
                            dbca.SymptomsUrgentlyAddressed = ca.SymptomsUrgentlyAddressed;
                            dbca.SymptomsUrgentlyAddressedExplain = ca.SymptomsUrgentlyAddressedExplain;
                            dbca.TreatmentOptions = ca.TreatmentOptions;
                            dbca.WithdrawalManagement = ca.WithdrawalManagement;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblMNComprehensiveAssessmentLevelOfCares.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }

            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveVACA(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblVAComprehensiveAssessment> NewItems = new List<Models.TblVAComprehensiveAssessment>();
                    List<Models.TblVAComprehensiveAssessment> dbList = db.TblVAComprehensiveAssessments.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblVAComprehensiveAssessment ca = new Models.TblVAComprehensiveAssessment();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    break;
                                case "id":
                                    ca.Id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dataformid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "createdby":
                                    ca.CreatedBy = r[c.ColumnName].ToString();
                                    break;
                                case "createdon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedby":
                                    ca.ModifiedBy = r[c.ColumnName].ToString();
                                    break;
                                case "modifiedon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isdeleted":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblVAComprehensiveAssessment dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.ClientId = ca.ClientId;
                            dbca.CreatedBy = ca.CreatedBy;
                            dbca.CreatedOn = ca.CreatedOn;
                            dbca.DataFormId = ca.DataFormId;
                            dbca.IsDeleted = ca.IsDeleted;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.ModifiedBy = ca.ModifiedBy;
                            dbca.ModifiedOn = ca.ModifiedOn;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblVAComprehensiveAssessments.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveVACASummary(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblVAComprehensiveAssessmentSummary> NewItems = new List<Models.TblVAComprehensiveAssessmentSummary>();
                    List<Models.TblVAComprehensiveAssessmentSummary> dbList = db.TblVAComprehensiveAssessmentSummaries.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblVAComprehensiveAssessmentSummary ca = new Models.TblVAComprehensiveAssessmentSummary();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    //ca.LastModAt = runat;
                                    break;
                                case "id":
                                    ca.Id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "vacomprehensiveassessmentid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.VAComprehensiveAssessmentId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlrecommendation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLRecommendation = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "opioidtreatmentservices":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OpioidTreatmentServices = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "withdrawalmanagement":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WithdrawalManagement = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clinicalsummary":
                                    ca.ClinicalSummary = r[c.ColumnName].ToString();
                                    break;
                                case "asamrecommendationforlevel":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ASAMRecommendationForLevel = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "levelofcareatvariance":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LevelOfCareAtVariance = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "summarycomments":
                                    ca.SummaryComments = r[c.ColumnName].ToString();
                                    break;
                            }
                        }
                        Models.TblVAComprehensiveAssessmentSummary dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.ASAMRecommendationForLevel = ca.ASAMRecommendationForLevel;
                            dbca.ClinicalSummary = ca.ClinicalSummary;
                            dbca.DDLRecommendation = ca.DDLRecommendation;
                            dbca.LevelOfCareAtVariance = ca.LevelOfCareAtVariance;
                            dbca.OpioidTreatmentServices = ca.OpioidTreatmentServices;
                            dbca.SummaryComments = ca.SummaryComments;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.VAComprehensiveAssessmentId = ca.VAComprehensiveAssessmentId;
                            dbca.WithdrawalManagement = ca.WithdrawalManagement;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblVAComprehensiveAssessmentSummaries.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }

        public Models.RCodes SaveNewAdmissionAssessment(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblNewAdmissionAssessment> NewItems = new List<Models.TblNewAdmissionAssessment>();
                    List<Models.TblNewAdmissionAssessment> dbList = db.TblNewAdmissionAssessments.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewAdmissionAssessment ca = new Models.TblNewAdmissionAssessment();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    break;
                                case "id":
                                    ca.Id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dataformid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "createdby":
                                    ca.CreatedBy = r[c.ColumnName].ToString();
                                    break;
                                case "createdon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedby":
                                    ca.ModifiedBy = r[c.ColumnName].ToString();
                                    break;
                                case "modifiedon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isdeleted":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "version":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Version = r[c.ColumnName].ToString();
                                    }
                                    break;
                            }
                        }
                        Models.TblNewAdmissionAssessment dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.ClientId = ca.ClientId;
                            dbca.CreatedBy = ca.CreatedBy;
                            dbca.CreatedOn = ca.CreatedOn;
                            dbca.DataFormId = ca.DataFormId;
                            dbca.IsDeleted = ca.IsDeleted;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.ModifiedBy = ca.ModifiedBy;
                            dbca.ModifiedOn = ca.ModifiedOn;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.Version = ca.Version;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewAdmissionAssessments.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveNewAdmissionAssessmentASAMDimension6(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblNewAdmissionAssessmentASAMDimension6> NewItems = new List<Models.TblNewAdmissionAssessmentASAMDimension6>();
                    List<Models.TblNewAdmissionAssessmentASAMDimension6> dbList = db.TblNewAdmissionAssessmentASAMDimension6s.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewAdmissionAssessmentASAMDimension6 ca = new Models.TblNewAdmissionAssessmentASAMDimension6();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    break;
                                case "newadmissionassessmentformid":
                                    ca.NewAdmissionAssessmentFormId = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion1 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion2":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion2 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion3":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion3 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion4":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion4 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion5 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion6":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion6 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion7 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion8":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion8 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion9":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion9 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion10":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion10 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion11":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion11 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "readinessquestion12":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReadinessQuestion12 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "stageofchange":
                                    ca.StageOfChange = r[c.ColumnName].ToString();
                                    break;
                                case "additionalcomments":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AdditionalComments = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "treatmentpreferences":
                                    ca.TreatmentPreferences = r[c.ColumnName].ToString();
                                    break;
                                case "reasonnotwillingtoattend":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonNotWillingToAttend = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonwillnotadmitreason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonWillNotAdmitReason = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonpatientineligiblereason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonPatientIneligibleReason = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "reasonotherreason":
                                    ca.ReasonOtherReason = r[c.ColumnName].ToString();
                                    break;
                                case "clinicalsummary":
                                    ca.ClinicalSummary = r[c.ColumnName].ToString();
                                    break;
                                case "hastreatmentpreferences":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HasTreatmentPreferences = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "willingtoattendrecommendedcare":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WillingToAttendRecommendedCare = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "transportationchallenges":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TransportationChallenges = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "foodhousinginsecurity":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FoodHousingInsecurity = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "childcareresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChildcareResponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "financialinsecurity":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FinancialInsecurity = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackemploymentopportunities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackEmploymentOpportunities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackjobsecurity":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackJobSecurity = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackhealthcarecoverage":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackHealthcareCoverage = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lacksocialsupports":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackSocialSupports = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "languagebarriers":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LanguageBarriers = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level1_5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level1_5 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level1_7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level1_7 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level2_1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level2_1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level2_5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level2_5 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level2_7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level2_7 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level3_1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level3_1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level3_5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level3_5 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level3_7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level3_7 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nonbio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NonBIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "bio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.BIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level4":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level4 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "coe":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.COE = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonnotaligned":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonNotAligned = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonnotavailable":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonNotAvailable = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonclinicianjudgment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonClinicianJudgment = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonpatientpreference":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonPatientPreference = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasononwaitinglist":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonOnWaitingList = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonlackspayment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonLacksPayment = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasongeographicaccess":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonGeographicAccess = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasoncaregiverresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonCaregiverResponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonemploymentresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonEmploymentResponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasoncourtrequirements":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonCourtRequirements = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasontransportationchallenges":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonTransportationChallenges = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonlanguageaccessibility":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonLanguageAccessibility = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonwillnotadmit":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonWillNotAdmit = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonpatientineligible":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonPatientIneligible = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonother":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonOther = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientsignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientsignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupervisorSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupervisorSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "counselorsignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CounselorSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "counselorsignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CounselorSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "providersignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProviderSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "providersignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProviderSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "superviosorsignna":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SuperviosorSignNA = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientsignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupervisorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "counselorsignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CounselorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "providersignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProviderSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewAdmissionAssessmentASAMDimension6 dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewAdmissionAssessmentFormId == ca.NewAdmissionAssessmentFormId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.AdditionalComments = ca.AdditionalComments;
                            dbca.BIO = ca.BIO;
                            dbca.ChildcareResponsibilities = ca.ChildcareResponsibilities;
                            dbca.ClinicalSummary = ca.ClinicalSummary;
                            dbca.COE = ca.COE;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.CounselorSignature = ca.CounselorSignature;
                            dbca.CounselorSignatureBy = ca.CounselorSignatureBy;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.CounselorSignatureDate = ca.CounselorSignatureDate;
                            dbca.FinancialInsecurity = ca.FinancialInsecurity;
                            dbca.FoodHousingInsecurity = ca.FoodHousingInsecurity;
                            dbca.HasTreatmentPreferences = ca.HasTreatmentPreferences;
                            dbca.LackEmploymentOpportunities = ca.LackEmploymentOpportunities;
                            dbca.LackHealthcareCoverage = ca.LackHealthcareCoverage;
                            dbca.LackJobSecurity = ca.LackJobSecurity;
                            dbca.LackSocialSupports = ca.LackSocialSupports;
                            dbca.LanguageBarriers = ca.LanguageBarriers;
                            dbca.Level1 = ca.Level1;
                            dbca.Level1_5 = ca.Level1_5;
                            dbca.Level1_7 = ca.Level1_7;
                            dbca.Level2_1 = ca.Level2_1;
                            dbca.Level2_5 = ca.Level2_5;
                            dbca.Level2_7 = ca.Level2_7;
                            dbca.Level3_1 = ca.Level3_1;
                            dbca.Level3_5 = ca.Level3_5;
                            dbca.Level3_7 = ca.Level3_7;
                            dbca.Level4 = ca.Level4;
                            dbca.NonBIO = ca.NonBIO;
                            dbca.PatientSignature = ca.PatientSignature;
                            dbca.PatientSignatureBy = ca.PatientSignatureBy;
                            dbca.PatientSignatureDate = ca.PatientSignatureDate;
                            dbca.ProviderSignature = ca.ProviderSignature;
                            dbca.ProviderSignatureBy = ca.ProviderSignatureBy;
                            dbca.ProviderSignatureDate = ca.ProviderSignatureDate;
                            dbca.ReadinessQuestion1 = ca.ReadinessQuestion1;
                            dbca.ReadinessQuestion10 = ca.ReadinessQuestion10;
                            dbca.ReadinessQuestion11 = ca.ReadinessQuestion11;
                            dbca.ReadinessQuestion12 = ca.ReadinessQuestion12;
                            dbca.ReadinessQuestion2 = ca.ReadinessQuestion2;
                            dbca.ReadinessQuestion3 = ca.ReadinessQuestion3;
                            dbca.ReadinessQuestion4 = ca.ReadinessQuestion4;
                            dbca.ReadinessQuestion5 = ca.ReadinessQuestion5;
                            dbca.ReadinessQuestion6 = ca.ReadinessQuestion6;
                            dbca.ReadinessQuestion7 = ca.ReadinessQuestion7;
                            dbca.ReadinessQuestion8 = ca.ReadinessQuestion8;
                            dbca.ReadinessQuestion9 = ca.ReadinessQuestion9;
                            dbca.ReasonCaregiverResponsibilities = ca.ReasonCaregiverResponsibilities;
                            dbca.ReasonClinicianJudgment = ca.ReasonClinicianJudgment;
                            dbca.ReasonCourtRequirements = ca.ReasonCourtRequirements;
                            dbca.ReasonEmploymentResponsibilities = ca.ReasonEmploymentResponsibilities;
                            dbca.ReasonGeographicAccess = ca.ReasonGeographicAccess;
                            dbca.ReasonLacksPayment = ca.ReasonLacksPayment;
                            dbca.ReasonLanguageAccessibility = ca.ReasonLanguageAccessibility;
                            dbca.ReasonNotAligned = ca.ReasonNotAligned;
                            dbca.ReasonNotAvailable = ca.ReasonNotAvailable;
                            dbca.ReasonNotWillingToAttend = ca.ReasonNotWillingToAttend;
                            dbca.ReasonOnWaitingList = ca.ReasonOnWaitingList;
                            dbca.ReasonOther = ca.ReasonOther;
                            dbca.ReasonOtherReason = ca.ReasonOtherReason;
                            dbca.ReasonPatientIneligible = ca.ReasonPatientIneligible;
                            dbca.ReasonPatientIneligibleReason = ca.ReasonPatientIneligibleReason;
                            dbca.ReasonPatientPreference = ca.ReasonPatientPreference;
                            dbca.ReasonTransportationChallenges = ca.ReasonTransportationChallenges;
                            dbca.ReasonWillNotAdmit = ca.ReasonWillNotAdmit;
                            dbca.ReasonWillNotAdmitReason = ca.ReasonWillNotAdmitReason;
                            dbca.StageOfChange = ca.StageOfChange;
                            dbca.SuperviosorSignNA = ca.SuperviosorSignNA;
                            dbca.SupervisorSignature = ca.SupervisorSignature;
                            dbca.SupervisorSignatureBy = ca.SupervisorSignatureBy;
                            dbca.SupervisorSignatureDate = ca.SupervisorSignatureDate;
                            dbca.TransportationChallenges = ca.TransportationChallenges;
                            dbca.TreatmentPreferences = ca.TreatmentPreferences;
                            dbca.WillingToAttendRecommendedCare = ca.WillingToAttendRecommendedCare;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewAdmissionAssessmentASAMDimension6s.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveNewPeriodicReassessment(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblNewPeriodicReassessment> NewItems = new List<Models.TblNewPeriodicReassessment>();
                    List<Models.TblNewPeriodicReassessment> dbList = db.TblNewPeriodicReassessments.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessment ca = new Models.TblNewPeriodicReassessment();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    //ca.LastModAt = runat;
                                    break;
                                case "id":
                                    ca.Id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dataformid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "date":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.Date = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "currentpathway":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentPathway = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "completedat":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CompletedAt = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "completedatothers":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CompletedAtOthers = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isdeleted":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "createdby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CreatedBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "createdon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ModifiedBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "version":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Version = (r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewPeriodicReassessment dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.ClientId = ca.ClientId;
                            dbca.CompletedAt = ca.CompletedAt;
                            dbca.CompletedAtOthers = ca.CompletedAtOthers;
                            dbca.CreatedBy = ca.CreatedBy;
                            dbca.CreatedOn = ca.CreatedOn;
                            dbca.CurrentPathway = ca.CurrentPathway;
                            dbca.DataFormId = ca.DataFormId;
                            dbca.Date = ca.Date;
                            dbca.IsDeleted = ca.IsDeleted;
                            dbca.ModifiedBy = ca.ModifiedBy;
                            dbca.ModifiedOn = ca.ModifiedOn;
                            dbca.Version = ca.Version;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessments.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes Savenewperiodicreassessmentcounselorreview(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblNewPeriodicReassessmentCounselorReview> NewItems = new List<Models.TblNewPeriodicReassessmentCounselorReview>();
                    List<Models.TblNewPeriodicReassessmentCounselorReview> dbList = db.TblNewPeriodicReassessmentCounselorReviews.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessmentCounselorReview ca = new Models.TblNewPeriodicReassessmentCounselorReview();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    //ca.LastModAt = runat;
                                    break;
                                case "newperiodicreassessmentid":
                                    ca.NewPeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level1_5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level1_5 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level1_7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level1_7 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level2_1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level2_1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level2_5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level2_5 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level2_7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level2_7 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level3_1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level3_1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level3_5":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level3_5 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level3_7":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level3_7 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nonbio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NonBIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "bio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.BIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "level4":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level4 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "coe":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.COE = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonnotaligned":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonNotAligned = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonnotavailable":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonNotAvailable = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonclinicianjudgment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonClinicianJudgment = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonpatientpreference":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonPatientPreference = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasononwaitinglist":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonOnWaitingList = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonlackspayment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonLacksPayment = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasongeographicaccess":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonGeographicAccess = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasoncaregiverresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonCaregiverResponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonemploymentresponsibilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonEmploymentResponsibilities = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasoncourtrequirements":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonCourtRequirements = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasontransportationchallenges":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonTransportationChallenges = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonlanguageaccessibility":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonLanguageAccessibility = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonwillnotadmit":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonWillNotAdmit = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonpatientineligible":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonPatientIneligible = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonother":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonOther = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonwillnotadmitreason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonWillNotAdmitReason = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonpatientineligiblereason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonPatientIneligibleReason = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "reasonotherreason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReasonOtherReason = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "copephase1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CopePhase2 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "copephase2":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CopePhase1 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "copephase3":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CopePhase3 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "induction":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Induction = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clinicalsummary":
                                    ca.ClinicalSummary = r[c.ColumnName].ToString();
                                    break;
                                case "stabilization":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Stabilization = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "maintenance":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Maintenance = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "datecompleted":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.DateCompleted = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "usescore":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.UseScore = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "riskscore":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RiskScore = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "protectivescore":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProtectiveScore = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientsignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientsignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PatientSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "patientsignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "counselorsignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CounselorSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "counselorsignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CounselorSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "counselorsignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CounselorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "providersignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProviderSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "providersignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProviderSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "providersignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.ProviderSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsignature":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupervisorSignature = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsignatureby":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupervisorSignatureBy = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsignaturedate":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        ca.SupervisorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "rr":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RR = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewPeriodicReassessmentCounselorReview dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewPeriodicReassessmentId == ca.NewPeriodicReassessmentId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.BIO = ca.BIO;
                            dbca.ClinicalSummary = ca.ClinicalSummary;
                            dbca.COE = ca.COE;
                            dbca.CopePhase1 = ca.CopePhase1;
                            dbca.CopePhase2 = ca.CopePhase2;
                            dbca.CopePhase3 = ca.CopePhase3;
                            dbca.CounselorSignature = ca.CounselorSignature;
                            dbca.CounselorSignatureBy = ca.CounselorSignatureBy;
                            dbca.CounselorSignatureDate = ca.CounselorSignatureDate;
                            dbca.DateCompleted = ca.DateCompleted;
                            dbca.Induction = ca.Induction;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.Level1 = ca.Level1;
                            dbca.Level1_5 = ca.Level1_5;
                            dbca.Level1_7 = ca.Level1_7;
                            dbca.Level2_1 = ca.Level2_1;
                            dbca.Level2_5 = ca.Level2_5;
                            dbca.Level2_7 = ca.Level2_7;
                            dbca.Level3_1 = ca.Level3_1;
                            dbca.Level3_5 = ca.Level3_5;
                            dbca.Level3_7 = ca.Level3_7;
                            dbca.Level4 = ca.Level4;
                            dbca.Maintenance = ca.Maintenance;
                            dbca.NonBIO = ca.NonBIO;
                            dbca.PatientSignature = ca.PatientSignature;
                            dbca.PatientSignatureBy = ca.PatientSignatureBy;
                            dbca.PatientSignatureDate = ca.PatientSignatureDate;
                            dbca.ProviderSignature = ca.ProviderSignature;
                            dbca.ProviderSignatureBy = ca.ProviderSignatureBy;
                            dbca.ProviderSignatureDate = ca.ProviderSignatureDate;
                            dbca.ProtectiveScore = ca.ProtectiveScore;
                            dbca.ReasonCaregiverResponsibilities = ca.ReasonCaregiverResponsibilities;
                            dbca.ReasonClinicianJudgment = ca.ReasonClinicianJudgment;
                            dbca.ReasonCourtRequirements = ca.ReasonCourtRequirements;
                            dbca.ReasonEmploymentResponsibilities = ca.ReasonEmploymentResponsibilities;
                            dbca.ReasonGeographicAccess = ca.ReasonGeographicAccess;
                            dbca.ReasonLacksPayment = ca.ReasonLacksPayment;
                            dbca.ReasonLanguageAccessibility = ca.ReasonLanguageAccessibility;
                            dbca.ReasonNotAligned = ca.ReasonNotAligned;
                            dbca.ReasonNotAvailable = ca.ReasonNotAvailable;
                            dbca.ReasonOnWaitingList = ca.ReasonOnWaitingList;
                            dbca.ReasonOther = ca.ReasonOther;
                            dbca.ReasonOtherReason = ca.ReasonOtherReason;
                            dbca.ReasonPatientIneligible = ca.ReasonPatientIneligible;
                            dbca.ReasonPatientIneligibleReason = ca.ReasonPatientIneligibleReason;
                            dbca.ReasonPatientPreference = ca.ReasonPatientPreference;
                            dbca.ReasonTransportationChallenges = ca.ReasonTransportationChallenges;
                            dbca.ReasonWillNotAdmit = ca.ReasonWillNotAdmit;
                            dbca.ReasonWillNotAdmitReason = ca.ReasonWillNotAdmitReason;
                            dbca.SupervisorSignature = ca.SupervisorSignature;
                            dbca.SupervisorSignatureBy = ca.SupervisorSignatureBy;
                            dbca.SupervisorSignatureDate = ca.SupervisorSignatureDate;
                            dbca.RiskScore = ca.RiskScore;
                            dbca.RR = ca.RR;
                            dbca.Stabilization = ca.Stabilization;
                            dbca.UseScore = ca.UseScore;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessmentCounselorReviews.AddRange(NewItems);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }

    }
}
