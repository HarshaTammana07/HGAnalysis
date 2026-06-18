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
        public Models.RCodes SaveNewAdmissionAssessmentASAMDimension2(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewAdmissionAssessmentASAMDimension2> NewItems = new List<Models.TblNewAdmissionAssessmentASAMDimension2>();
                    List<Models.TblNewAdmissionAssessmentASAMDimension2> dbList = db.TblNewAdmissionAssessmentASAMDimension2s.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewAdmissionAssessmentASAMDimension2 ca = new Models.TblNewAdmissionAssessmentASAMDimension2();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    ca.RowState = true; 
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
                                case "additionalcomments":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AdditionalComments = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "additionalcomments1":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AdditionalComments1 = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "allergiescomments":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AllergiesComments = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "asthma":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Asthma = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "blindness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Blindness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "cancer":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Cancer = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "chronicpain":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChronicPain = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "copd":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.COPD = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "deafness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Deafness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "diabetes":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Diabetes = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension2physicalhealth":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension2PhysicalHealth = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension2pregnancy":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension2Pregnancy = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension2problems":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension2Problems = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "epilepsy":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Epilepsy = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hasprimarycare":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HasPrimaryCare = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hearingloss":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HearingLoss = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "heartdisease":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HeartDisease = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hepatitisa":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HepatitisA = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hepatitisb":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HepatitisB = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hepatitisc":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HepatitisC = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hepatitisd":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HepatitisD = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "highbloodpressure":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HighBloodPressure = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hiv":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HIV = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ispregnant":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsPregnant = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "kidneydisease":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.KidneyDisease = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "liverdisease":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LiverDisease = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "medicalproblemspreventclinic":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.MedicalProblemsPreventClinic = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "other":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Other = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "otherchk":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Otherchk = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "poorvision":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PoorVision = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "primarycareprovider":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PrimaryCareProvider = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "receivingprenatalcare":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ReceivingPrenatalCare = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tuberculosis":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Tuberculosis = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewAdmissionAssessmentASAMDimension2 dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewAdmissionAssessmentFormId == ca.NewAdmissionAssessmentFormId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.AdditionalComments = ca.AdditionalComments;
                            dbca.AdditionalComments1 = ca.AdditionalComments1;
                            dbca.AllergiesComments = ca.AllergiesComments;
                            dbca.Asthma = ca.Asthma;
                            dbca.Blindness = ca.Blindness;
                            dbca.Cancer = ca.Cancer;
                            dbca.ChronicPain = ca.ChronicPain;
                            dbca.COPD = ca.COPD;
                            dbca.Deafness = ca.Deafness;
                            dbca.Diabetes = ca.Diabetes;
                            dbca.Dimension2PhysicalHealth = ca.Dimension2PhysicalHealth;
                            dbca.Dimension2Pregnancy = ca.Dimension2Pregnancy;
                            dbca.Dimension2Problems = ca.Dimension2Problems;
                            dbca.Epilepsy = ca.Epilepsy;
                            dbca.HasPrimaryCare = ca.HasPrimaryCare;
                            dbca.HearingLoss = ca.HearingLoss;
                            dbca.HeartDisease = ca.HeartDisease;
                            dbca.HepatitisA = ca.HepatitisA;
                            dbca.HepatitisB = ca.HepatitisB;
                            dbca.HepatitisC = ca.HepatitisC;
                            dbca.HepatitisD = ca.HepatitisD;
                            dbca.HighBloodPressure = ca.HighBloodPressure;
                            dbca.HIV = ca.HIV;
                            dbca.IsPregnant = ca.IsPregnant;
                            dbca.KidneyDisease = ca.KidneyDisease;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.LiverDisease = ca.LiverDisease;
                            dbca.MedicalProblemsPreventClinic = ca.MedicalProblemsPreventClinic;
                            dbca.Other = ca.Other;
                            dbca.Otherchk = ca.Otherchk;
                            dbca.PoorVision = ca.PoorVision;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.PrimaryCareProvider = ca.PrimaryCareProvider;
                            dbca.ReceivingPrenatalCare = ca.ReceivingPrenatalCare;
                            dbca.RowState = ca.RowState;
                            dbca.Tuberculosis = ca.Tuberculosis;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewAdmissionAssessmentASAMDimension2s.AddRange(NewItems);
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
        public Models.RCodes SaveNewAdmissionAssessmentASAMDimension4(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewAdmissionAssessmentASAMDimension4> NewItems = new List<Models.TblNewAdmissionAssessmentASAMDimension4>();
                    List<Models.TblNewAdmissionAssessmentASAMDimension4> dbList = db.TblNewAdmissionAssessmentASAMDimension4s.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewAdmissionAssessmentASAMDimension4 ca = new Models.TblNewAdmissionAssessmentASAMDimension4();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    ca.RowState = true;
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
                                case "addtionalcomments":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AddtionalComments = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "anychildrenliveinhome":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AnyChildrenLiveInHome = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "causedproblemsatyourjob":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CausedProblemsAtYourJob = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension4sudrisk":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension4SUDRisk = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension4sudriskbehaviours":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension4SUDRiskBehaviours = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "doesyourtempercauseproblem":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DoesYourTemperCauseProblem = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hadanoverdose":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HadAnOverdose = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveanycaseswithlocaldepartment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveAnyCasesWithLocalDepartment = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveanyopencourtcases":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveAnyOpenCourtCases = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveleggalcustodyofchildren":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveLeggalCustodyOfChildren = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "havenarcanavailable":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveNarcanAvailable = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "havesubstanceuseputyouindanger":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveSubstanceUsePutYouInDanger = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveyoubeenarrested":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveYouBeenArrested = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveyoucalled911":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveYouCalled911 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveyourphysicalhealthworse":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveYourPhysicalHealthWorse = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "havinganyfinancialtrouble":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HavingAnyFinancialTrouble = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "havingprobleminrelationship":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HavingProblemInRelationship = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "jeopardizedyourhousing":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.JeopardizedYourHousing = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "onprobation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OnProbation = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "substanceuseputyouatriskofarrested":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SubstanceUsePutYouAtRiskOfArrested = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "yourphysicalmentalworse":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.YourPhysicalMentalWorse = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewAdmissionAssessmentASAMDimension4 dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewAdmissionAssessmentFormId == ca.NewAdmissionAssessmentFormId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.AddtionalComments = ca.AddtionalComments;
                            dbca.AnyChildrenLiveInHome = ca.AnyChildrenLiveInHome;
                            dbca.CausedProblemsAtYourJob = ca.CausedProblemsAtYourJob;
                            dbca.Dimension4SUDRisk = ca.Dimension4SUDRisk;
                            dbca.Dimension4SUDRiskBehaviours = ca.Dimension4SUDRiskBehaviours;
                            dbca.DoesYourTemperCauseProblem = ca.DoesYourTemperCauseProblem;
                            dbca.HadAnOverdose = ca.HadAnOverdose;
                            dbca.HaveAnyCasesWithLocalDepartment = ca.HaveAnyCasesWithLocalDepartment;
                            dbca.HaveAnyOpenCourtCases = ca.HaveAnyOpenCourtCases;
                            dbca.HaveLeggalCustodyOfChildren = ca.HaveLeggalCustodyOfChildren;
                            dbca.HaveNarcanAvailable = ca.HaveNarcanAvailable;
                            dbca.HaveSubstanceUsePutYouInDanger = ca.HaveSubstanceUsePutYouInDanger;
                            dbca.HaveYouBeenArrested = ca.HaveYouBeenArrested;
                            dbca.HaveYouCalled911 = ca.HaveYouCalled911;
                            dbca.HaveYourPhysicalHealthWorse = ca.HaveYourPhysicalHealthWorse;
                            dbca.HavingAnyFinancialTrouble = ca.HavingAnyFinancialTrouble;
                            dbca.HavingProblemInRelationship = ca.HavingProblemInRelationship;
                            dbca.JeopardizedYourHousing = ca.JeopardizedYourHousing;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.OnProbation = ca.OnProbation;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.RowState = ca.RowState;
                            dbca.SubstanceUsePutYouAtRiskOfArrested = ca.SubstanceUsePutYouAtRiskOfArrested;
                            dbca.YourPhysicalMentalWorse = ca.YourPhysicalMentalWorse;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewAdmissionAssessmentASAMDimension4s.AddRange(NewItems);
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
        public Models.RCodes SaveNewAdmissionAssessmentASAMDimension5(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TBLNewAdmissionAssessmentASAMDimension5> NewItems = new List<Models.TBLNewAdmissionAssessmentASAMDimension5>();
                    List<Models.TBLNewAdmissionAssessmentASAMDimension5> dbList = db.TBLNewAdmissionAssessmentASAMDimension5s.Where(x => x.SiteCode == sc).ToList();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TBLNewAdmissionAssessmentASAMDimension5 ca = new Models.TBLNewAdmissionAssessmentASAMDimension5();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = sc;
                                    ca.LastModAt = runat;
                                    ca.RowState = true; 
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
                                case "appliancesnotworking":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AppliancesNotWorking = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "bio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.BIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "coe":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.COE = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension5functioning":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension5Functioning = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension5safety":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension5Safety = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dimension5support":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Dimension5Support = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "drugusecommoninneighborhood":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DrugUseCommonInNeighborhood = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "employmenthelp":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.EmploymentHelp = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "financialstrain":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FinancialStrain = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "fooddidntlast":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FoodDidntLast = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "foodworry":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FoodWorry = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "friendsfamilyinrecovery":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FriendsFamilyInRecovery = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "knowpeersupportmeetings":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.KnowPeerSupportMeetings = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackofheat":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackOfHeat = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "leadpaint":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LeadPaint = bool.Parse(r[c.ColumnName].ToString());
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
                                case "level4":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level4 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "livingsituation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LivingSituation = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "mold":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Mold = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "neededucationhelp":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NeedEducationHelp = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nonbio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NonBIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "none":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.None = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "peopleinhomeusesubstances":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PeopleInHomeUseSubstances = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "pests":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Pests = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicalharmfrequency":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PhysicalHarmFrequency = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "rr":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RR = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "skippedmedications":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SkippedMedications = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "smokedetectorsmissing":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SmokeDetectorsMissing = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "speaknonenglishlanguage":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SpeakNonEnglishLanguage = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supportforrecovery":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupportForRecovery = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "threatsfrequency":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ThreatsFrequency = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "transportationproblems":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TransportationProblems = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "verbalabusefrequency":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.VerbalAbuseFrequency = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "waterleaks":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WaterLeaks = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "yellingfrequency":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.YellingFrequency = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TBLNewAdmissionAssessmentASAMDimension5 dbca = dbList.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewAdmissionAssessmentFormId == ca.NewAdmissionAssessmentFormId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            dbca.AppliancesNotWorking = ca.AppliancesNotWorking;
                            dbca.BIO = ca.BIO;
                            dbca.COE = ca.COE;
                            dbca.Dimension5Functioning = ca.Dimension5Functioning;
                            dbca.Dimension5Safety = ca.Dimension5Safety;
                            dbca.Dimension5Support = ca.Dimension5Support;
                            dbca.DrugUseCommonInNeighborhood = ca.DrugUseCommonInNeighborhood;
                            dbca.EmploymentHelp = ca.EmploymentHelp;
                            dbca.FinancialStrain = ca.FinancialStrain;
                            dbca.FoodDidntLast = ca.FoodDidntLast;
                            dbca.FoodWorry = ca.FoodWorry;
                            dbca.FriendsFamilyInRecovery = ca.FriendsFamilyInRecovery;
                            dbca.KnowPeerSupportMeetings = ca.KnowPeerSupportMeetings;
                            dbca.LackOfHeat = ca.LackOfHeat;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.LeadPaint = ca.LeadPaint;
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
                            dbca.LivingSituation = ca.LivingSituation;
                            dbca.Mold = ca.Mold;
                            dbca.NeedEducationHelp = ca.NeedEducationHelp;
                            dbca.NonBIO = ca.NonBIO;
                            dbca.None = ca.None;
                            dbca.PeopleInHomeUseSubstances = ca.PeopleInHomeUseSubstances;
                            dbca.Pests = ca.Pests;
                            dbca.PhysicalHarmFrequency = ca.PhysicalHarmFrequency;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.RowState = ca.RowState;
                            dbca.RR = ca.RR;
                            dbca.SkippedMedications = ca.SkippedMedications;
                            dbca.SmokeDetectorsMissing = ca.SmokeDetectorsMissing;
                            dbca.SpeakNonEnglishLanguage = ca.SpeakNonEnglishLanguage;
                            dbca.SupportForRecovery = ca.SupportForRecovery;
                            dbca.ThreatsFrequency = ca.ThreatsFrequency;
                            dbca.TransportationProblems = ca.TransportationProblems;
                            dbca.VerbalAbuseFrequency = ca.VerbalAbuseFrequency;
                            dbca.WaterLeaks = ca.WaterLeaks;
                            dbca.YellingFrequency = ca.YellingFrequency;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TBLNewAdmissionAssessmentASAMDimension5s.AddRange(NewItems);
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
        public Models.RCodes SaveNewPeriodicReassessmentD2(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewPeriodicReassessmentD2> Listitems = db.TblNewPeriodicReassessmentD2s.Where(x => x.SiteCode == sc).ToList();
                    List<Models.TblNewPeriodicReassessmentD2> NewItems = new List<Models.TblNewPeriodicReassessmentD2>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessmentD2 ca = new Models.TblNewPeriodicReassessmentD2();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = r[c.ColumnName].ToString();
                                    ca.RowState = true;
                                    ca.LastModAt = runat;
                                    break;
                                case "newperiodicreassessmentid":
                                    ca.NewPeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "called911":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Called911 = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "called911box":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Called911Box = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "currentlypregnant":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentlyPregnant = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "currentlypregnantroibox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentlyPregnantROIBox = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "currentlyreceivingprenatamcare":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentlyReceivingPrenatalCare = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "discontinuetobacconicotine":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DiscontinueTobaccoNicotine = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "druginjection":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DrugInjection = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hivhepatitisbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HIVHepatitisBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hivhepitits":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HIVHepatits = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "noneoftheabove":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NoneOfTheAbove = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicalhealth":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PhysicalHealth = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicalhealthchange":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PhysicalHealthChange = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "pregnancyrelatedconcern":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PregnancyRelatedConcern = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "primarycareprovider":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PrimaryCareProvider = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "primarycareproviderbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PrimaryCareProviderBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "sharingdrug":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SharingDrug = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tobacconicotine":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TobaccoNicotine = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tobacconicotinefrequency":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TobaccoNicotineFrequency = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "unprotectedsex":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.UnprotectedSex = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "worseningmedicalcondition":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WorseningMedicalCondition = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "worseningmedicalconditionbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WorseningMedicalConditionBox = r[c.ColumnName].ToString();
                                    }
                                    break;
                            }
                        }
                        Models.TblNewPeriodicReassessmentD2 dbca = Listitems.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewPeriodicReassessmentId == ca.NewPeriodicReassessmentId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            rc.RowsUpd += 1;
                            dbca.Called911 = ca.Called911;
                            dbca.Called911Box = ca.Called911Box;
                            dbca.CurrentlyPregnant = ca.CurrentlyPregnant;
                            dbca.CurrentlyPregnantROIBox = ca.CurrentlyPregnantROIBox;
                            dbca.CurrentlyReceivingPrenatalCare = ca.CurrentlyReceivingPrenatalCare;
                            dbca.DiscontinueTobaccoNicotine = ca.DiscontinueTobaccoNicotine;
                            dbca.DrugInjection = ca.DrugInjection;
                            dbca.HIVHepatitisBox = ca.HIVHepatitisBox;
                            dbca.HIVHepatits = ca.HIVHepatits;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.NoneOfTheAbove = ca.NoneOfTheAbove;
                            dbca.PhysicalHealth = ca.PhysicalHealth;
                            dbca.PhysicalHealthChange = ca.PhysicalHealthChange;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.PregnancyRelatedConcern = ca.PregnancyRelatedConcern;
                            dbca.PrimaryCareProvider = ca.PrimaryCareProvider;
                            dbca.PrimaryCareProviderBox = ca.PrimaryCareProviderBox;
                            dbca.RowState = ca.RowState;
                            dbca.SharingDrug = ca.SharingDrug;
                            dbca.TobaccoNicotine = ca.TobaccoNicotine;
                            dbca.TobaccoNicotineFrequency = ca.TobaccoNicotineFrequency;
                            dbca.UnprotectedSex = ca.UnprotectedSex;
                            dbca.WorseningMedicalCondition = ca.WorseningMedicalCondition;
                            dbca.WorseningMedicalConditionBox = ca.WorseningMedicalConditionBox;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessmentD2s.AddRange(NewItems);
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
        public Models.RCodes SaveNewPeriodicReassessmentD3(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewPeriodicReassessmentD3> Listitems = db.TblNewPeriodicReassessmentD3s.Where(x => x.SiteCode == sc).ToList();
                    List<Models.TblNewPeriodicReassessmentD3> NewItems = new List<Models.TblNewPeriodicReassessmentD3>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessmentD3 ca = new Models.TblNewPeriodicReassessmentD3();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = r[c.ColumnName].ToString();
                                    ca.RowState = true;
                                    ca.LastModAt = runat;
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
                                case "activepsychiatricsymptoms":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ActivePsychiatricSymptoms = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "agitation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Agitation = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "anger":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Anger = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "anxiety":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Anxiety = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "brainfog":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.BrainFog = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "confusion":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Confusion = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "decreasedappetite":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DecreasedAppetite = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "decreasedenergy":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DecreasedEnergy = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "decreasedpleasure":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DecreasedPleasure = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "decreasedselfcontrol":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DecreasedSelfControl = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "disorganizedconfusedthoughts":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DisorganizedConfusedThoughts = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "fatigue":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Fatigue = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "feelingempty":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Feelingempty = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "forgetfulness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Forgetfulness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "guiltshame":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.GuiltShame = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hallucinations":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Hallucinations = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "headaches":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Headaches = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "increasedappetite":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IncreasedAppetite = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "increasedenergy":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IncreasedEnergy = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "insomnia":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Insomnia = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "irritability":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Irritability = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isolation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Isolation = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "killingyourself":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.KillingYourself = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackoffocus":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackofFocus = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackofinterest":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackofInterest = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackofmotivation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackofMotivation = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "mentalhealthchange":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.MentalHealthChange = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "mentalhealthhospitalized":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.MentalHealthHospitalized = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "mentalhealthhospitalizedbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.MentalHealthHospitalizedBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "moodswings":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.MoodSwings = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nervousness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Nervousness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nightmares":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Nightmares = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "numbness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Numbness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "obsessiveworryingthoughts":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ObsessiveWorryingThoughts = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "othermentalsymptoms":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OtherMentalSymptoms = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "othermentalsymptomsbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OtherMentalSymptomsBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "panicattacks":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PanicAttacks = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "persistentdisability":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PersistentDisability = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "persistentsadness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PersistentSadness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "restlessness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Restlessness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.StomachIssues = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tearfulness":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Tearfulness = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "troublefallingasleep":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TroubleFallingAsleep = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "troublewakingup":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TroubleWakingUp = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "wisheddead":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WishedDead = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "worseningmentalhealth":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WorseningMentalHealth = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "worseningmentalhealthbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WorseningMentalHealthBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewPeriodicReassessmentD3 dbca = Listitems.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewPeriodicReassessmentId == ca.NewPeriodicReassessmentId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            rc.RowsUpd += 1;
                            dbca.ActivePsychiatricSymptoms = ca.ActivePsychiatricSymptoms;
                            dbca.Agitation = ca.Agitation;
                            dbca.Anger = ca.Anger;
                            dbca.Anxiety = ca.Anxiety;
                            dbca.BrainFog = ca.BrainFog;
                            dbca.Confusion = ca.Confusion;
                            dbca.DecreasedAppetite = ca.DecreasedAppetite;
                            dbca.DecreasedEnergy = ca.DecreasedEnergy;
                            dbca.DecreasedPleasure = ca.DecreasedPleasure;
                            dbca.DecreasedSelfControl = ca.DecreasedSelfControl;
                            dbca.DisorganizedConfusedThoughts = ca.DisorganizedConfusedThoughts;
                            dbca.Fatigue = ca.Fatigue;
                            dbca.Feelingempty = ca.Feelingempty;
                            dbca.Forgetfulness = ca.Forgetfulness;
                            dbca.GuiltShame = ca.GuiltShame;
                            dbca.Hallucinations = ca.Hallucinations;
                            dbca.Headaches = ca.Headaches;
                            dbca.IncreasedAppetite = ca.IncreasedAppetite;
                            dbca.IncreasedEnergy = ca.IncreasedEnergy;
                            dbca.Insomnia = ca.Insomnia;
                            dbca.Irritability = ca.Irritability;
                            dbca.Isolation = ca.Isolation;
                            dbca.KillingYourself = ca.KillingYourself;
                            dbca.LackofFocus = ca.LackofFocus;
                            dbca.LackofInterest = ca.LackofInterest;
                            dbca.LackofMotivation = ca.LackofMotivation;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.MentalHealthChange = ca.MentalHealthChange;
                            dbca.MentalHealthHospitalized = ca.MentalHealthHospitalized;
                            dbca.MentalHealthHospitalizedBox = ca.MentalHealthHospitalizedBox;
                            dbca.MoodSwings = ca.MoodSwings;
                            dbca.Nervousness = ca.Nervousness;
                            dbca.Nightmares = ca.Nightmares;
                            dbca.Numbness = ca.Numbness;
                            dbca.ObsessiveWorryingThoughts = ca.ObsessiveWorryingThoughts;
                            dbca.OtherMentalSymptoms = ca.OtherMentalSymptoms;
                            dbca.OtherMentalSymptomsBox = ca.OtherMentalSymptomsBox;
                            dbca.PanicAttacks = ca.PanicAttacks;
                            dbca.PersistentDisability = ca.PersistentDisability;
                            dbca.PersistentSadness = ca.PersistentSadness;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.Restlessness = ca.Restlessness;
                            dbca.RowState = ca.RowState;
                            dbca.StomachIssues = ca.StomachIssues;
                            dbca.Tearfulness = ca.Tearfulness;
                            dbca.TroubleFallingAsleep = ca.TroubleFallingAsleep;
                            dbca.TroubleWakingUp = ca.TroubleWakingUp;
                            dbca.WishedDead = ca.WishedDead;
                            dbca.WorseningMentalHealth = ca.WorseningMentalHealth;
                            dbca.WorseningMentalHealthBox = ca.WorseningMentalHealthBox;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessmentD3s.AddRange(NewItems);
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
        public Models.RCodes SaveNewPeriodicReassessmentD4(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewPeriodicReassessmentD4> Listitems = db.TblNewPeriodicReassessmentD4s.Where(x => x.SiteCode == sc).ToList();
                    List<Models.TblNewPeriodicReassessmentD4> NewItems = new List<Models.TblNewPeriodicReassessmentD4>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessmentD4 ca = new Models.TblNewPeriodicReassessmentD4();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = r[c.ColumnName].ToString();
                                    ca.RowState = true;
                                    ca.LastModAt = runat;
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
                                case "arrested":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Arrested = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "changeinlegalstatus":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChangeinLegalStatus = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "changeinlegalstatusbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChangeinLegalStatusBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "continueusing":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ContinueUsing = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "continueusingbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ContinueUsingBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "copingstrategies":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CopingStrategies = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "employmentstatus":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.EmploymentStatus = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "employmentstatusother":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.EmploymentStatusOther = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "financialtrouble":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FinancialTrouble = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "partfulltime":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PartFullTime = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "riskysubstanceuse":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RiskySubstanceUse = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "riskysudrelatedbehaviors":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RiskySUDRelatedBehaviors = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewPeriodicReassessmentD4 dbca = Listitems.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewPeriodicReassessmentId == ca.NewPeriodicReassessmentId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            rc.RowsUpd += 1;
                            dbca.Arrested = ca.Arrested;
                            dbca.ChangeinLegalStatus = ca.ChangeinLegalStatus;
                            dbca.ChangeinLegalStatusBox = ca.ChangeinLegalStatusBox;
                            dbca.ContinueUsing = ca.ContinueUsing;
                            dbca.ContinueUsingBox = ca.ContinueUsingBox;
                            dbca.CopingStrategies = ca.CopingStrategies;
                            dbca.EmploymentStatus = ca.EmploymentStatus;
                            dbca.EmploymentStatusOther = ca.EmploymentStatusOther;
                            dbca.FinancialTrouble = ca.FinancialTrouble;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.PartFullTime = ca.PartFullTime;
                            dbca.RiskySubstanceUse = ca.RiskySubstanceUse;
                            dbca.RiskySUDRelatedBehaviors = ca.RiskySUDRelatedBehaviors;
                            dbca.RowState = ca.RowState;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessmentD4s.AddRange(NewItems);
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
        public Models.RCodes SaveNewPeriodicReassessmentD5(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewPeriodicReassessmentD5> Listitems = db.TblNewPeriodicReassessmentD5s.Where(x => x.SiteCode == sc).ToList();
                    List<Models.TblNewPeriodicReassessmentD5> NewItems = new List<Models.TblNewPeriodicReassessmentD5>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessmentD5 ca = new Models.TblNewPeriodicReassessmentD5();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = r[c.ColumnName].ToString();
                                    ca.RowState = true;
                                    ca.LastModAt = runat;
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
                                case "anyonehurtyou":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AnyoneHurtYou = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "barriers":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Barriers = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "barriersbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.BarriersBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "bio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.BIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "childfamilyservicesopencases":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChildFamilyServicesOpenCases = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "children":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Children = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "childrenage":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChildrenAge = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "childrenagebox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChildrenAgeBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "childrenlegalcustody":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ChildrenLegalCustody = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "coe":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.COE = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "currentlyconnectedsupport":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentlyConnectedSupport = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "currentlyconnectedsupportbox":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentlyConnectedSupportBox = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "effectivelyincurrentenvironment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.EffectivelyinCurrentEnvironment = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "enoughmoney":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.EnoughMoney = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "familyfriendsinrecovery":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FamilyFriendsinRecovery = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "findingorkeepingwork":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FindingOrKeepingWork = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "friendsfamilysupport":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FriendsFamilySupport = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hardtopay":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HardToPay = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "insultortalkdown":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.InsultOrTalkDown = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackofheat":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackofHeat = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lackoftransportation":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackOfTransportation = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lastassessmentfoodbought":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LastassessmentFoodBought = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lastassessmentskipmedications":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LastassessmentSkipMedications = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "lastassessmentworried":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LastassessmentWorried = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "leadpaintpipes":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LeadPaintPipes = bool.Parse(r[c.ColumnName].ToString());
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
                                case "level4":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Level4 = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "livingsituationtoday":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LivingSituationToday = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "mold":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Mold = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nonbio":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NonBIO = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "noneofabove":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NoneOfAbove = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ovenorstove":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.OvenOrStove = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "pests":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Pests = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "rr":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RR = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "safetycurrentenvironment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SafetyCurrentEnvironment = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "schooltraining":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SchoolTraining = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "screamorcurseatyou":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ScreamOrCurseAtYou = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "smokedetectors":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SmokeDetectors = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "speaklanguage":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SpeakLanguage = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supportcurrentenvironment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupportCurrentEnvironment = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "threatenwithharm":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ThreatenWithHarm = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "waterleaks":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Waterleaks = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        Models.TblNewPeriodicReassessmentD5 dbca = Listitems.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewPeriodicReassessmentId == ca.NewPeriodicReassessmentId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            rc.RowsUpd += 1;
                            dbca.AnyoneHurtYou = ca.AnyoneHurtYou;
                            dbca.Barriers = ca.Barriers;
                            dbca.BarriersBox = ca.BarriersBox;
                            dbca.BIO = ca.BIO;
                            dbca.ChildFamilyServicesOpenCases = ca.ChildFamilyServicesOpenCases;
                            dbca.Children = ca.Children;
                            dbca.ChildrenAge = ca.ChildrenAge;
                            dbca.ChildrenAgeBox = ca.ChildrenAgeBox;
                            dbca.ChildrenLegalCustody = ca.ChildrenLegalCustody;
                            dbca.COE = ca.COE;
                            dbca.CurrentlyConnectedSupport = ca.CurrentlyConnectedSupport;
                            dbca.CurrentlyConnectedSupportBox = ca.CurrentlyConnectedSupportBox;
                            dbca.EffectivelyinCurrentEnvironment = ca.EffectivelyinCurrentEnvironment;
                            dbca.EnoughMoney = ca.EnoughMoney;
                            dbca.FamilyFriendsinRecovery = ca.FamilyFriendsinRecovery;
                            dbca.FindingOrKeepingWork = ca.FindingOrKeepingWork;
                            dbca.FriendsFamilySupport = ca.FriendsFamilySupport;
                            dbca.HardToPay = ca.HardToPay;
                            dbca.InsultOrTalkDown = ca.InsultOrTalkDown;
                            dbca.LackofHeat = ca.LackofHeat;
                            dbca.LackOfTransportation = ca.LackOfTransportation;
                            dbca.LastassessmentFoodBought = ca.LastassessmentFoodBought;
                            dbca.LastassessmentSkipMedications = ca.LastassessmentWorried;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.LeadPaintPipes = ca.LeadPaintPipes;
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
                            dbca.LivingSituationToday = ca.LivingSituationToday;
                            dbca.Mold = ca.Mold;
                            dbca.NonBIO = ca.NonBIO;
                            dbca.NoneOfAbove = ca.NoneOfAbove;
                            dbca.OvenOrStove = ca.OvenOrStove;
                            dbca.Pests = ca.Pests;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.RowState = ca.RowState;
                            dbca.RR = ca.RR;
                            dbca.SafetyCurrentEnvironment = ca.SafetyCurrentEnvironment;
                            dbca.SchoolTraining = ca.SchoolTraining;
                            dbca.ScreamOrCurseAtYou = ca.ScreamOrCurseAtYou;
                            dbca.SmokeDetectors = ca.SmokeDetectors;
                            dbca.SpeakLanguage = ca.SpeakLanguage;
                            dbca.SupportCurrentEnvironment = ca.SupportCurrentEnvironment;
                            dbca.ThreatenWithHarm = ca.ThreatenWithHarm;
                            dbca.Waterleaks = ca.Waterleaks;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessmentD5s.AddRange(NewItems);
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
        public Models.RCodes SaveNewPeriodicReassessmentD6(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblNewPeriodicReassessmentD6> Listitems = db.TblNewPeriodicReassessmentD6s.Where(x => x.SiteCode == sc).ToList();
                    List<Models.TblNewPeriodicReassessmentD6> NewItems = new List<Models.TblNewPeriodicReassessmentD6>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblNewPeriodicReassessmentD6 ca = new Models.TblNewPeriodicReassessmentD6();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = r[c.ColumnName].ToString();
                                    ca.RowState = true;
                                    ca.LastModAt = runat;
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
                                case "motivationformakingorsustainingchanges":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Motivationformakingorsustainingchanges = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "satisfiedyourprogress":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Satisfiedyourprogress = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "satisfiedyourprogressexplain":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SatisfiedyourprogressExplain = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "iseventuallydiscontinuing":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Iseventuallydiscontinuing = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "planondiscontinuing":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Planondiscontinuing = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "whatstrengthsareusing":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WhatStrengthsareusing = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "whatneedsdoyouhave":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WhatNeedsdoyouhave = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "listanyabilities":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ListanyAbilities = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveyoulearnedprefer":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Haveyoulearnedprefer = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hastreatmentpreferences":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HasTreatmentPreferences = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "treatmentpreferences":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TreatmentPreferences = (r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "willingtoattendrecommendedcare":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.WillingToAttendRecommendedCare = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "notwillingreason":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NotWillingReason = (r[c.ColumnName].ToString());
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
                                case "lackeducationemployment":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LackEducationEmployment = bool.Parse(r[c.ColumnName].ToString());
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
                            }
                        }
                        Models.TblNewPeriodicReassessmentD6 dbca = Listitems.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.NewPeriodicReassessmentId == ca.NewPeriodicReassessmentId);
                        if (dbca == null)
                        {
                            rc.RowsIns += 1;
                            NewItems.Add(ca);
                        }
                        else
                        {
                            rc.RowsUpd += 1;
                            dbca.ChildcareResponsibilities = ca.ChildcareResponsibilities;
                            dbca.FinancialInsecurity = ca.FinancialInsecurity;
                            dbca.FoodHousingInsecurity = ca.FoodHousingInsecurity;
                            dbca.HasTreatmentPreferences = ca.HasTreatmentPreferences;
                            dbca.Haveyoulearnedprefer = ca.Haveyoulearnedprefer;
                            dbca.Iseventuallydiscontinuing = ca.Iseventuallydiscontinuing;
                            dbca.LackEducationEmployment = ca.LackEducationEmployment;
                            dbca.LackHealthcareCoverage = ca.LackHealthcareCoverage;
                            dbca.LackJobSecurity = ca.LackJobSecurity;
                            dbca.LackSocialSupports = ca.LackSocialSupports;
                            dbca.LanguageBarriers = ca.LanguageBarriers;
                            dbca.LastModAt = ca.LastModAt;
                            dbca.ListanyAbilities = ca.ListanyAbilities;
                            dbca.Motivationformakingorsustainingchanges = ca.Motivationformakingorsustainingchanges;
                            dbca.NotWillingReason = ca.NotWillingReason;
                            dbca.Planondiscontinuing = ca.Planondiscontinuing;
                            dbca.PreAdmissionId = ca.PreAdmissionId;
                            dbca.RowState = ca.RowState;
                            dbca.Satisfiedyourprogress = ca.Satisfiedyourprogress;
                            dbca.SatisfiedyourprogressExplain = ca.SatisfiedyourprogressExplain;
                            dbca.TransportationChallenges = ca.TransportationChallenges;
                            dbca.TreatmentPreferences = ca.TreatmentPreferences;
                            dbca.WhatNeedsdoyouhave = ca.WhatNeedsdoyouhave;
                            dbca.WhatStrengthsareusing = ca.WhatStrengthsareusing;
                            dbca.WillingToAttendRecommendedCare = ca.WillingToAttendRecommendedCare;
                        }
                    }
                    db.SaveChanges();
                    if (NewItems.Count > 0)
                    {
                        db.TblNewPeriodicReassessmentD6s.AddRange(NewItems);
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
