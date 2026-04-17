using System;
using System.Data;
using System.Collections.Generic;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveAdmissionAssessment(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessment> laas = db.TblAdmissionAssessment.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessment> xlaas = new List<Models.TblAdmissionAssessment>();
                foreach(DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessment aa = new Models.TblAdmissionAssessment();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                aa.SiteCode = sc;
                                aa.LastModAt = runat;
                                break;
                            case "id":
                                aa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                aa.PreAdmissionId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                aa.DataFormId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.DataFormId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                aa.ClientId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.ClientId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                aa.CreatedBy = dr[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    aa.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                aa.ModifiedBy = dr[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    aa.ModifiedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                aa.IsDeleted = false;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.IsDeleted = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                aa.Version = dr[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblAdmissionAssessment dbaa = laas.FirstOrDefault(x => x.Id == aa.Id);
                    if (dbaa == null)
                    {
                        xlaas.Add(aa);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbaa.PreAdmissionId = aa.PreAdmissionId;
                        dbaa.DataFormId = aa.DataFormId;
                        dbaa.ClientId = aa.ClientId;
                        dbaa.CreatedBy = aa.CreatedBy;
                        dbaa.CreatedOn = aa.CreatedOn;
                        dbaa.ModifiedBy = aa.ModifiedBy;
                        dbaa.ModifiedOn = aa.ModifiedOn;
                        dbaa.IsDeleted = aa.IsDeleted;
                        dbaa.Version = aa.Version;
                        dbaa.LastModAt = aa.LastModAt;
                    }
                }
                db.SaveChanges();
                if (xlaas.Count > 0)
                {
                    db.TblAdmissionAssessment.AddRange(xlaas);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentSummary(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentSummary> laas = db.TblAdmissionAssessmentSummary.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentSummary> xlaas = new List<Models.TblAdmissionAssessmentSummary>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentSummary aa = new Models.TblAdmissionAssessmentSummary();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                aa.SiteCode = sc;
                                aa.LastModAt = runat;
                                break;
                            case "id":
                                aa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                aa.PreAdmissionId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                aa.AdmissionAssessmentId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddlrecommendation":
                                aa.Ddlrecommendation = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.Ddlrecommendation = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "opioidtreatmentservices":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.OpioidTreatmentServices = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "withdrawalmanagement":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.WithdrawalManagement = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "clinicalsummary":
                                aa.ClinicalSummary = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "asamrecommendationforlevel":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AsamrecommendationForLevel = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "levelofcareatvariance":
                                aa.LevelOfCareAtVariance = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "summarycomments":
                                aa.SummaryComments = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentstaffsignature":
                                aa.AdmissionAssessmentStaffSignature = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentstaffsignatureby":
                                aa.AdmissionAssessmentStaffSignatureBy = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentstaffsignaturedate":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    aa.AdmissionAssessmentPatientSignatureDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentprovidersignature":
                                aa.AdmissionAssessmentProviderSignature = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentprovidersignatureby":
                                aa.AdmissionAssessmentProviderSignatureBy = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentprovidersignaturedate":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AdmissionAssessmentProviderSignatureDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentpatientsignature":
                                aa.AdmissionAssessmentPatientSignature = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentpatientsignatureby":
                                aa.AdmissionAssessmentPatientSignatureBy = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentpatientsignaturedate":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AdmissionAssessmentPatientSignatureDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentsupervisorsignature":
                                aa.AdmissionAssessmentSupervisorSignature = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentsupervisorsignatureby":
                                aa.AdmissionAssessmentSupervisorSignatureBy = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "admissionassessmentsupervisorsignaturedate":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AdmissionAssessmentSupervisorSignatureDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblAdmissionAssessmentSummary dbaa = laas.FirstOrDefault(x => x.Id == aa.Id);
                    if (dbaa == null)
                    {
                        xlaas.Add(aa);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbaa.PreAdmissionId = aa.PreAdmissionId;
                        dbaa.AdmissionAssessmentId = aa.AdmissionAssessmentId;
                        dbaa.AdmissionAssessmentPatientSignature = aa.AdmissionAssessmentPatientSignature;
                        dbaa.AdmissionAssessmentPatientSignatureBy = aa.AdmissionAssessmentPatientSignatureBy;
                        dbaa.AdmissionAssessmentPatientSignatureDate = aa.AdmissionAssessmentPatientSignatureDate;
                        dbaa.AdmissionAssessmentProviderSignature = aa.AdmissionAssessmentProviderSignature;
                        dbaa.AdmissionAssessmentProviderSignatureBy = aa.AdmissionAssessmentProviderSignatureBy;
                        dbaa.AdmissionAssessmentProviderSignatureDate = aa.AdmissionAssessmentProviderSignatureDate;
                        dbaa.AdmissionAssessmentStaffSignature = aa.AdmissionAssessmentStaffSignature;
                        dbaa.AdmissionAssessmentStaffSignatureBy = aa.AdmissionAssessmentStaffSignatureBy;
                        dbaa.AdmissionAssessmentStaffSignatureDate = aa.AdmissionAssessmentStaffSignatureDate;
                        dbaa.AdmissionAssessmentSupervisorSignature = aa.AdmissionAssessmentSupervisorSignature;
                        dbaa.AdmissionAssessmentSupervisorSignatureBy = aa.AdmissionAssessmentSupervisorSignatureBy;
                        dbaa.AdmissionAssessmentSupervisorSignatureDate = aa.AdmissionAssessmentSupervisorSignatureDate;
                        dbaa.AsamrecommendationForLevel = aa.AsamrecommendationForLevel;
                        dbaa.ClinicalSummary = aa.ClinicalSummary;
                        dbaa.Ddlrecommendation = aa.Ddlrecommendation;
                        dbaa.LevelOfCareAtVariance = aa.LevelOfCareAtVariance;
                        dbaa.OpioidTreatmentServices = aa.OpioidTreatmentServices;
                        dbaa.SummaryComments = aa.SummaryComments;
                        dbaa.WithdrawalManagement = aa.WithdrawalManagement;
                        dbaa.LastModAt = aa.LastModAt;
                    }
                }
                db.SaveChanges();
                if (xlaas.Count > 0)
                {
                    db.TblAdmissionAssessmentSummary.AddRange(xlaas);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentDimensionfour(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentDimensionFour> laas = db.TblAdmissionAssessmentDimensionFour.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentDimensionFour> xlaas = new List<Models.TblAdmissionAssessmentDimensionFour>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentDimensionFour aa = new Models.TblAdmissionAssessmentDimensionFour();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                aa.SiteCode = sc;
                                aa.LastModAt = runat;
                                break;
                            case "id":
                                aa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                aa.PreAdmissionId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                aa.AdmissionAssessmentId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "idontthinkusedrugstoomuch":
                                aa.IdontThinkUseDrugsTooMuch = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.IdontThinkUseDrugsTooMuch = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "tryingttodrinklessthanused":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.TryingTtoDrinklessThanUsed = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ienjoymydrinking":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.IenjoyMyDrinking = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "stageofchange":
                                aa.StageOfChange = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "ishouldcutdownonmydrinking":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.IshouldCutDownOnMyDrinking = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "wasteoftimetothinkaboutmydrinking":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.WasteOfTimeToThinkAboutMyDrinking = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "recentlychangedmydrinking":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.RecentlyChangedMyDrinking = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "anyonecantalkaboutwanting":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.AnyoneCanTalkAboutWanting = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "thinkaboutdrinkinglessalcohol":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.ThinkAboutDrinkingLessAlcohol = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "mydrinkinguse":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.MyDrinkingUse = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "noneedformetothinkabout":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.NoNeedForMeToThinkAbout = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "actuallychangingmydrinking":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.ActuallyChangingMyDrinking = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "drinkinglessalcohol":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.DrinkingLessAlcohol = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "precontemplationscale":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.PrecontemplationScale = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "contemplationscale":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.ContemplationScale = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "actionscale":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.ActionScale = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddldimensionfourscore":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.DdldimensionFourScore = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "statusofchange":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    aa.StatusofChange = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "comments4":
                                aa.Comments4 = dr[c.ColumnName].ToString().Trim();
                                break;
                            case "dimension4problems":
                                aa.Dimension4Problems = dr[c.ColumnName].ToString().Trim();
                                break;
                        }
                    }
                    Models.TblAdmissionAssessmentDimensionFour dbaa = laas.FirstOrDefault(x => x.Id == aa.Id);
                    if (dbaa == null)
                    {
                        xlaas.Add(aa);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbaa.PreAdmissionId = aa.PreAdmissionId;
                        dbaa.AdmissionAssessmentId = aa.AdmissionAssessmentId;
                        dbaa.ActionScale = aa.ActionScale;
                        dbaa.ActuallyChangingMyDrinking = aa.ActuallyChangingMyDrinking;
                        dbaa.AnyoneCanTalkAboutWanting = aa.AnyoneCanTalkAboutWanting;
                        dbaa.Comments4 = aa.Comments4;
                        dbaa.ContemplationScale = aa.ContemplationScale;
                        dbaa.DdldimensionFourScore = aa.DdldimensionFourScore;
                        dbaa.Dimension4Problems = aa.Dimension4Problems;
                        dbaa.DrinkingLessAlcohol = aa.DrinkingLessAlcohol;
                        dbaa.IdontThinkUseDrugsTooMuch = aa.IdontThinkUseDrugsTooMuch;
                        dbaa.IenjoyMyDrinking = aa.IenjoyMyDrinking;
                        dbaa.IshouldCutDownOnMyDrinking = aa.IshouldCutDownOnMyDrinking;
                        dbaa.LastModAt = aa.LastModAt;
                        dbaa.MyDrinkingUse = aa.MyDrinkingUse;
                        dbaa.NoNeedForMeToThinkAbout = aa.NoNeedForMeToThinkAbout;
                        dbaa.PrecontemplationScale = aa.PrecontemplationScale;
                        dbaa.RecentlyChangedMyDrinking = aa.RecentlyChangedMyDrinking;
                        dbaa.StageOfChange = aa.StageOfChange;
                        dbaa.StatusofChange = aa.StatusofChange;
                        dbaa.ThinkAboutDrinkingLessAlcohol = aa.ThinkAboutDrinkingLessAlcohol;
                        dbaa.TryingTtoDrinklessThanUsed = aa.TryingTtoDrinklessThanUsed;
                        dbaa.WasteOfTimeToThinkAboutMyDrinking = aa.WasteOfTimeToThinkAboutMyDrinking;
                    }
                }
                db.SaveChanges();
                if (xlaas.Count > 0)
                {
                    db.TblAdmissionAssessmentDimensionFour.AddRange(xlaas);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentDimensionOneDisorder 
            (DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentDimensionOneDisorder> dbList = db.TblAdmissionAssessmentDimensionOneDisorder.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentDimensionOneDisorder> xnList = new List<Models.TblAdmissionAssessmentDimensionOneDisorder>();
                foreach(DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentDimensionOneDisorder xa = new Models.TblAdmissionAssessmentDimensionOneDisorder();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "opioiddisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.OpioidDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "alcoholdisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AlcoholDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "sedativedisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.SedativeDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "stimulantdisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.StimulantDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "cannabisdisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.CannabisDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "hallucinogendisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HallucinogenDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "inhakantdisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.InhalantDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "phencyclidinedisorderpresent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PhencyclidineDisorderPresent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "comments":
                                xa.Comments = dr[c.ColumnName].ToString();
                                break;
                            case "medicallyassistedwithdrawal":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.MedicallyAssistedWithdrawal = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "medicallyassistedwithdrawalhowmanytimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.MedicallyAssistedWithdrawalHowManyTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "medicallyassistedwithdrawalrecenttimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.MedicallyAssistedWithdrawalRecentTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "inpatientrehabilitation":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.InpatientRehabilitation = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "inpatientrehabilitationhowmanytimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.InpatientRehabilitationHowManyTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "inpatientrehabilitationsuccessfullycomplete":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.InpatientRehabilitationSuccessfullyComplete = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "inpatientrehabilitationrecenttimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.InpatientRehabilitationRecentTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "intensiveoutpatienttreatments":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IntensiveOutpatientTreatments = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "intensiveoutpatienthowmanytimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IntensiveOutpatientHowManyTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "intensiveoutpatientsuccessfullycomplete":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IntensiveOutpatientSuccessfullyComplete = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "intensiveoutpatientrecenttimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IntensiveOutpatientRecentTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "outpatienttreatment":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.OutpatientTreatment = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "outpatienttreatmenthowmanytimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.OutpatientTreatmentHowManyTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "outpatienttreatmentsuccessfullycomplete":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.OutpatientTreatmentSuccessfullyComplete = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "outpatienttreatmentrecenttimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.OutpatientTreatmentRecentTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "previousmat":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreviousMat = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "previousmatmethadone":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreviousMatmethadone = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "previousmatbuprenorphine":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreviousMatbuprenorphine = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "previousmatnaltrexone":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreviousMatnaltrexone = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "previousmatwhatwasyourdose":
                                xa.PreviousMatwhatWasYourDose = dr[c.ColumnName].ToString();
                                break;
                            case "previousmatwasithelpful":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreviousMatwasItHelpful = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "howlongdidyoutakeit":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HowLongDidYouTakeIt = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddlhowlongdidyoutakeit":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdlhowLongDidYouTakeIt = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "longestperiodofsobriety":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.LongestPeriodOfSobriety = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddllongestperiodofsobrietyfromallsubstances":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdllongestPeriodOfSobrietyFromAllSubstances = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "substanceusehistorycomments":
                                xa.SubstanceUseHistoryComments = dr[c.ColumnName].ToString();
                                break;
                            case "ddldimensiononescore":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdldimensionOneScore = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "howdoyouprocurethedrug":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HowDoYouProcureTheDrug = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "buyonthestreet":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.BuyOnTheStreet = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "freefromfamily":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.FreeFromFamily = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "prescriptionfromhealthcareprovider":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PrescriptionFromHealthcareProvider = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "sellinguseownsupply":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.SellingUseOwnSupply = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "theft":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Theft = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddlmedicallyassistedwithdrawal":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdlmedicallyAssistedWithdrawal = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddlinpatientrehabilitation":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdlinpatientRehabilitation = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddlintensiveoutpatient":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdlintensiveOutpatient = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddloutpatienttreatment":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdloutpatientTreatment = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkmedicallyassistedwithdrawal":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkMedicallyAssistedWithdrawal = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkinpatientrehabilitation":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkInpatientRehabilitation = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkintensiveoutpatienttreatments":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkIntensiveOutpatientTreatments = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkoutpatienttreatment":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkOutpatientTreatment = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkpreviousmat":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkPreviousMat = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AdmissionAssessmentId = xa.AdmissionAssessmentId;
                        xdb.AlcoholDisorderPresent = xa.AlcoholDisorderPresent;
                        xdb.BuyOnTheStreet = xa.BuyOnTheStreet;
                        xdb.CannabisDisorderPresent = xa.CannabisDisorderPresent;
                        xdb.ChkInpatientRehabilitation = xa.ChkInpatientRehabilitation;
                        xdb.ChkIntensiveOutpatientTreatments = xa.ChkIntensiveOutpatientTreatments;
                        xdb.ChkMedicallyAssistedWithdrawal = xa.ChkMedicallyAssistedWithdrawal;
                        xdb.ChkOutpatientTreatment = xa.ChkOutpatientTreatment;
                        xdb.ChkPreviousMat = xa.ChkPreviousMat;
                        xdb.Comments = xa.Comments;
                        xdb.DdldimensionOneScore = xa.DdldimensionOneScore;
                        xdb.DdlhowLongDidYouTakeIt = xa.DdlhowLongDidYouTakeIt;
                        xdb.DdlinpatientRehabilitation = xa.DdlinpatientRehabilitation;
                        xdb.DdlintensiveOutpatient = xa.DdlintensiveOutpatient;
                        xdb.DdllongestPeriodOfSobrietyFromAllSubstances = xa.DdllongestPeriodOfSobrietyFromAllSubstances;
                        xdb.DdlmedicallyAssistedWithdrawal = xa.DdlmedicallyAssistedWithdrawal;
                        xdb.DdloutpatientTreatment = xa.DdloutpatientTreatment;
                        xdb.FreeFromFamily = xa.FreeFromFamily;
                        xdb.HallucinogenDisorderPresent = xa.HallucinogenDisorderPresent;
                        xdb.HowDoYouProcureTheDrug = xa.HowDoYouProcureTheDrug;
                        xdb.HowLongDidYouTakeIt = xa.HowLongDidYouTakeIt;
                        xdb.InhalantDisorderPresent = xa.InhalantDisorderPresent;
                        xdb.InpatientRehabilitation = xa.InpatientRehabilitation;
                        xdb.InpatientRehabilitationHowManyTimes = xa.InpatientRehabilitationHowManyTimes;
                        xdb.InpatientRehabilitationRecentTimes = xa.InpatientRehabilitationRecentTimes;
                        xdb.InpatientRehabilitationSuccessfullyComplete = xa.InpatientRehabilitationSuccessfullyComplete;
                        xdb.IntensiveOutpatientHowManyTimes = xa.IntensiveOutpatientHowManyTimes;
                        xdb.IntensiveOutpatientRecentTimes = xa.IntensiveOutpatientRecentTimes;
                        xdb.IntensiveOutpatientSuccessfullyComplete = xa.IntensiveOutpatientSuccessfullyComplete;
                        xdb.IntensiveOutpatientTreatments = xa.IntensiveOutpatientTreatments;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.LongestPeriodOfSobriety = xa.LongestPeriodOfSobriety;
                        xdb.MedicallyAssistedWithdrawal = xa.MedicallyAssistedWithdrawal;
                        xdb.MedicallyAssistedWithdrawalHowManyTimes = xa.MedicallyAssistedWithdrawalHowManyTimes;
                        xdb.MedicallyAssistedWithdrawalRecentTimes = xa.MedicallyAssistedWithdrawalRecentTimes;
                        xdb.OpioidDisorderPresent = xa.OpioidDisorderPresent;
                        xdb.OutpatientTreatment = xa.OutpatientTreatment;
                        xdb.OutpatientTreatmentHowManyTimes = xa.OutpatientTreatmentHowManyTimes;
                        xdb.OutpatientTreatmentRecentTimes = xa.OutpatientTreatmentRecentTimes;
                        xdb.OutpatientTreatmentSuccessfullyComplete = xa.OutpatientTreatmentSuccessfullyComplete;
                        xdb.PhencyclidineDisorderPresent = xa.PhencyclidineDisorderPresent;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.PrescriptionFromHealthcareProvider = xa.PrescriptionFromHealthcareProvider;
                        xdb.PreviousMat = xa.PreviousMat;
                        xdb.PreviousMatbuprenorphine = xa.PreviousMatbuprenorphine;
                        xdb.PreviousMatmethadone = xa.PreviousMatmethadone;
                        xdb.PreviousMatnaltrexone = xa.PreviousMatnaltrexone;
                        xdb.PreviousMatwasItHelpful = xa.PreviousMatwasItHelpful;
                        xdb.PreviousMatwhatWasYourDose = xa.PreviousMatwhatWasYourDose;
                        xdb.SedativeDisorderPresent = xa.SedativeDisorderPresent;
                        xdb.SellingUseOwnSupply = xa.SellingUseOwnSupply;
                        xdb.StimulantDisorderPresent = xa.StimulantDisorderPresent;
                        xdb.SubstanceUseHistoryComments = xa.SubstanceUseHistoryComments;
                        xdb.Theft = xa.Theft;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblAdmissionAssessmentDimensionOneDisorder.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }

            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentDimensionTwo(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentDimensionTwo> dbList = db.TblAdmissionAssessmentDimensionTwo.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentDimensionTwo> xnList = new List<Models.TblAdmissionAssessmentDimensionTwo>();

                foreach(DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentDimensionTwo xa = new Models.TblAdmissionAssessmentDimensionTwo();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "allergies":
                                xa.Allergies = dr[c.ColumnName].ToString();
                                break;
                            case "asthma":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Asthma = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "blindness":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Blindness = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "cancer":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Cancer = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chronicpain":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                { xa.ChronicPain = bool.Parse(dr[c.ColumnName].ToString()); }
                                break;
                            case "comments2":
                                xa.Comments2 = dr[c.ColumnName].ToString();
                                break;
                            case "copdemphysema":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Copdemphysema = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ddldimensiontwoscore":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdldimensionTwoScore = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "deafness":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Deafness = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "diabetes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Diabetes = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "diagnosedcomment2":
                                xa.DiagnosedComment2 = dr[c.ColumnName].ToString();
                                break;
                            case "dimension2problems":
                                xa.Dimension2Problems = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveanyconcerns":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAnyConcerns = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "doyouusetobacco":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouUseTobacco = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "epilepsyseizures":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.EpilepsySeizures = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "gerd":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Gerd = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "hearingloss":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HearingLoss = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "heartdisease":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HeartDisease = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "hepatitisa":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HepatitisA = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "hepatitisb":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HepatitisB = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "hepatitisc":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                { xa.HepatitisC = bool.Parse(dr[c.ColumnName].ToString()); }
                                break;
                                case "hepatitisd":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HepatitisD = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "highbloodpressure":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HighBloodPressure = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "highcholesterol":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HighCholesterol = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "hiv":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Hiv = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "liverdisease":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.LiverDisease = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "other":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Other = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "othertxt":
                                xa.OtherTxt = dr[c.ColumnName].ToString();
                                break;
                                case "poorvision":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PoorVision = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "primarycarepractitioner":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PrimaryCarePractitioner = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "renalkidneydisease":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.RenalKidneyDisease = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                                case "tuberculosis":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Tuberculosis = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AdmissionAssessmentId = xa.AdmissionAssessmentId;
                        xdb.Allergies = xa.Allergies;
                        xdb.Asthma = xa.Asthma;
                        xdb.Blindness = xa.Blindness;
                        xdb.Cancer = xa.Cancer;
                        xdb.ChronicPain = xa.ChronicPain;
                        xdb.Comments2 = xa.Comments2;
                        xdb.Copdemphysema = xa.Copdemphysema;
                        xdb.DdldimensionTwoScore = xa.DdldimensionTwoScore;
                        xdb.Deafness = xa.Deafness;
                        xdb.Diabetes = xa.Diabetes;
                        xdb.DiagnosedComment2 = xa.DiagnosedComment2;
                        xdb.Dimension2Problems = xa.Dimension2Problems;
                        xdb.DoYouHaveAnyConcerns = xa.DoYouHaveAnyConcerns;
                        xdb.DoYouUseTobacco = xa.DoYouUseTobacco;
                        xdb.EpilepsySeizures = xa.EpilepsySeizures;
                        xdb.Gerd = xa.Gerd;
                        xdb.HearingLoss = xa.HearingLoss;
                        xdb.HeartDisease = xa.HeartDisease;
                        xdb.HepatitisA = xa.HepatitisA;
                        xdb.HepatitisB = xa.HepatitisB;
                        xdb.HepatitisC = xa.HepatitisC;
                        xdb.HepatitisD = xa.HepatitisD;
                        xdb.HighBloodPressure = xa.HighBloodPressure;
                        xdb.HighCholesterol = xa.HighCholesterol;
                        xdb.Hiv = xa.Hiv;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.LiverDisease = xa.LiverDisease;
                        xdb.Other = xa.Other;
                        xdb.OtherTxt = xa.OtherTxt;
                        xdb.PoorVision = xa.PoorVision;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.PrimaryCarePractitioner = xa.PrimaryCarePractitioner;
                        xdb.RenalKidneyDisease = xa.RenalKidneyDisease;
                        xdb.Tuberculosis = xa.Tuberculosis;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblAdmissionAssessmentDimensionTwo.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentDimensionThree(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentDimensionThree> dbList = db.TblAdmissionAssessmentDimensionThree.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentDimensionThree> xnList = new List<Models.TblAdmissionAssessmentDimensionThree>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentDimensionThree xa = new Models.TblAdmissionAssessmentDimensionThree();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "agoraphobia":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Agoraphobia = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "anxiety":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Anxiety = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "bipolardisorder":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.BipolarDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "comments3":
                                xa.Comments3 = dr[c.ColumnName].ToString();
                                break;
                            case "ddldimensionthreescore":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdldimensionThreeScore = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "depression":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Depression = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "diagnosedcomment3":
                                xa.DiagnosedComment3 = dr[c.ColumnName].ToString();
                                break;
                            case "didyouattendspecialeducation":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DidYouAttendSpecialEducation = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dimension3problems":
                                xa.Dimension3Problems = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveapsychiatrist":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveApsychiatrist = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhaveapsychiatristtxt":
                                xa.DoYouHaveApsychiatristTxt = dr[c.ColumnName].ToString();
                                break;
                            case "generalizedanxietydisorder":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.GeneralizedAnxietyDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoueverbeenknockedunconscious":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouEverBeenKnockedUnconscious = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "hospitalizedformentalhealth":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HospitalizedForMentalHealth = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "howmanytimes":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HowManyTimes = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "mostrecenthospitalization":
                                xa.MostRecentHospitalization = dr[c.ColumnName].ToString();
                                break;
                            case "obsessivecompulsivedisorder":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ObsessiveCompulsiveDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "panicdisorder":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PanicDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "posttraumaticstressdisorder":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PostTraumaticStressDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "schizoaffectivedisorder":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.SchizoaffectiveDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "schizophrenia":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Schizophrenia = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "socialphobia":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.SocialPhobia = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AdmissionAssessmentId = xa.AdmissionAssessmentId;
                        xdb.Agoraphobia = xa.Agoraphobia;
                        xdb.Anxiety = xa.Anxiety;
                        xdb.BipolarDisorder = xa.BipolarDisorder;
                        xdb.Comments3 = xa.Comments3;
                        xdb.DdldimensionThreeScore = xa.DdldimensionThreeScore;
                        xdb.Depression = xa.Depression;
                        xdb.DiagnosedComment3 = xa.DiagnosedComment3;
                        xdb.DidYouAttendSpecialEducation = xa.DidYouAttendSpecialEducation;
                        xdb.Dimension3Problems = xa.Dimension3Problems;
                        xdb.DoYouHaveApsychiatrist = xa.DoYouHaveApsychiatrist;
                        xdb.DoYouHaveApsychiatristTxt = xa.DoYouHaveApsychiatristTxt;
                        xdb.GeneralizedAnxietyDisorder = xa.GeneralizedAnxietyDisorder;
                        xdb.HaveYouEverBeenKnockedUnconscious = xa.HaveYouEverBeenKnockedUnconscious;
                        xdb.HospitalizedForMentalHealth = xa.HospitalizedForMentalHealth;
                        xdb.HowManyTimes = xa.HowManyTimes;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.MostRecentHospitalization = xa.MostRecentHospitalization;
                        xdb.ObsessiveCompulsiveDisorder = xa.ObsessiveCompulsiveDisorder;
                        xdb.PanicDisorder = xa.PanicDisorder;
                        xdb.PostTraumaticStressDisorder = xa.PostTraumaticStressDisorder;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.SchizoaffectiveDisorder = xa.SchizoaffectiveDisorder;
                        xdb.Schizophrenia = xa.Schizophrenia;
                        xdb.SocialPhobia = xa.SocialPhobia;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblAdmissionAssessmentDimensionThree.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentDimensionFiveSubstanceUse(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentDimensionFiveSubstanceUse> dbaad5
                    = db.TblAdmissionAssessmentDimensionFiveSubstanceUse.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentDimensionFiveSubstanceUse> xnaad = new List<Models.TblAdmissionAssessmentDimensionFiveSubstanceUse>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentDimensionFiveSubstanceUse xa = new Models.TblAdmissionAssessmentDimensionFiveSubstanceUse();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "hadanoverdose":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HadAnOverdose = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "yourphysicalhealthworse":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.YourPhysicalHealthWorse = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "yourphysicalmetalworse":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.YourPhysicalMentalWorse = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoucalled911":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouCalled911 = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "substanceusejeopardized":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.SubstanceUseJeopardized = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "causedproblemsatyourjob":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.CausedProblemsAtYourJob = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "havinganyfinancialtroubles":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HavingAnyFinancialTroubles = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doesyourtempertend":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoesYourTemperTend = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoueverbeenarrested":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouEverBeenArrested = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "riskofbeingarrested":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.RiskOfBeingArrested = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "openorpendingcourtcases":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.OpenOrPendingCourtCases = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyouonprobation":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouOnProbation = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "legalcustodyofyourchildren":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.LegalCustodyOfYourChildren = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "anyopencaseswithlocaldepartment":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AnyOpenCasesWitHlocalDepartment = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "childrenliveinyourhome":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChildrenLiveInYourHome = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "comments":
                                xa.Comments = dr[c.ColumnName].ToString();
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dimensionfivecomments":
                                xa.DimensionFiveComments = dr[c.ColumnName].ToString();
                                break;
                            case "dimension5problems":
                                xa.Dimension5Problems = dr[c.ColumnName].ToString();
                                break;
                        }
                    }
                    var xdb = dbaad5.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        xnaad.Add(xa);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AdmissionAssessmentId = xa.AdmissionAssessmentId;
                        xdb.HadAnOverdose = xa.HadAnOverdose;
                        xdb.YourPhysicalHealthWorse = xa.YourPhysicalHealthWorse;
                        xdb.YourPhysicalMentalWorse = xa.YourPhysicalMentalWorse;
                        xdb.HaveYouCalled911 = xa.HaveYouCalled911;
                        xdb.SubstanceUseJeopardized = xa.SubstanceUseJeopardized;
                        xdb.CausedProblemsAtYourJob = xa.CausedProblemsAtYourJob;
                        xdb.HavingAnyFinancialTroubles = xa.HavingAnyFinancialTroubles;
                        xdb.DoesYourTemperTend = xa.DoesYourTemperTend;
                        xdb.HaveYouEverBeenArrested = xa.HaveYouEverBeenArrested;
                        xdb.RiskOfBeingArrested = xa.RiskOfBeingArrested;
                        xdb.OpenOrPendingCourtCases = xa.OpenOrPendingCourtCases;
                        xdb.AreYouOnProbation = xa.AreYouOnProbation;
                        xdb.LegalCustodyOfYourChildren = xa.LegalCustodyOfYourChildren;
                        xdb.AnyOpenCasesWitHlocalDepartment = xa.AnyOpenCasesWitHlocalDepartment;
                        xdb.ChildrenLiveInYourHome = xa.ChildrenLiveInYourHome;
                        xdb.Comments = xa.Comments;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.Dimension5Problems = xa.Dimension5Problems;
                        xdb.DimensionFiveComments = xa.DimensionFiveComments;
                        xdb.LastModAt = xa.LastModAt;
                    }
                }
                db.SaveChanges();
                if (xnaad.Count > 0)
                {
                    db.TblAdmissionAssessmentDimensionFiveSubstanceUse.AddRange(xnaad);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentDimensionSix(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentDimensionSix> dbList = db.TblAdmissionAssessmentDimensionSix.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentDimensionSix> xnList = new List<Models.TblAdmissionAssessmentDimensionSix>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentDimensionSix xa = new Models.TblAdmissionAssessmentDimensionSix();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "admissionassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AdmissionAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "anypeersupport":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AnyPeerSupport = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyoubehindonyourrent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouBehindOnYourRent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyoubehindonyourutility":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouBehindOnYourUtility = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "comments":
                                xa.Comments = dr[c.ColumnName].ToString();
                                break;
                            case "ddldimensionsixscore":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DdldimensionSixScore = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dimension6problems":
                                xa.Dimension6Problems = dr[c.ColumnName].ToString();
                                break;
                            case "dimensionsixcomments":
                                xa.DimensionSixComments = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveenoughmoney":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveEnoughMoney = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhavejob":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveJob = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhavesourceofincome":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveSourceOfIncome = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "drugsellingcommoninyourneighborhood":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DrugSellingCommonInYourNeighborhood = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "familymemberswhoareinrecovery":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.FamilyMembersWhoAreInRecovery = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "familywhoyoucancountontosupport":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.FamilyWhoYouCanCountOnToSupport = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "peopleinhomewhodrinkalcohol":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PeopleInHomeWhoDrinkAlcohol = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "safefromphysicalorsexualabuse":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.SafeFromPhysicalOrSexualAbuse = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "stablehousingofyourown":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.StableHousingOfYourOwn = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AdmissionAssessmentId = xa.AdmissionAssessmentId;
                        xdb.AnyPeerSupport = xa.AnyPeerSupport;
                        xdb.AreYouBehindOnYourRent = xa.AreYouBehindOnYourRent;
                        xdb.AreYouBehindOnYourUtility = xa.AreYouBehindOnYourUtility;
                        xdb.Comments = xa.Comments;
                        xdb.DdldimensionSixScore = xa.DdldimensionSixScore;
                        xdb.Dimension6Problems = xa.Dimension6Problems;
                        xdb.DimensionSixComments = xa.DimensionSixComments;
                        xdb.DoYouHaveEnoughMoney = xa.DoYouHaveEnoughMoney;
                        xdb.DoYouHaveJob = xa.DoYouHaveJob;
                        xdb.DoYouHaveSourceOfIncome = xa.DoYouHaveSourceOfIncome;
                        xdb.DrugSellingCommonInYourNeighborhood = xa.DrugSellingCommonInYourNeighborhood;
                        xdb.FamilyMembersWhoAreInRecovery = xa.FamilyMembersWhoAreInRecovery;
                        xdb.FamilyWhoYouCanCountOnToSupport = xa.FamilyWhoYouCanCountOnToSupport;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PeopleInHomeWhoDrinkAlcohol = xa.PeopleInHomeWhoDrinkAlcohol;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.SafeFromPhysicalOrSexualAbuse = xa.SafeFromPhysicalOrSexualAbuse;
                        xdb.StableHousingOfYourOwn = xa.StableHousingOfYourOwn;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblAdmissionAssessmentDimensionSix.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessment(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessment> ldbras = db.TblReAssessment.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessment> xnras = new List<Models.TblReAssessment>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessment ra = new Models.TblReAssessment();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                ra.SiteCode = sc;
                                ra.LastModAt = runat;
                                break;
                            case "id":
                                ra.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                ra.PreAdmissionId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                ra.DataFormId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.DataFormId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                ra.ClientId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.ClientId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                ra.CreatedBy = dr[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    ra.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                ra.ModifiedBy = dr[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    ra.ModifiedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                ra.IsDeleted = false;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.IsDeleted = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                ra.Version = dr[c.ColumnName].ToString();
                                break;
                            case "timeintreatment":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.TimeInTreatment = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblReAssessment dbra = ldbras.FirstOrDefault(x => x.Id == ra.Id);
                    if (dbra == null)
                    {
                        rc.RowsIns += 1;
                        xnras.Add(ra);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbra.PreAdmissionId = ra.PreAdmissionId;
                        dbra.ClientId = ra.ClientId;
                        dbra.DataFormId = ra.DataFormId;
                        dbra.CreatedBy = ra.CreatedBy;
                        dbra.CreatedOn = ra.CreatedOn;
                        dbra.ModifiedBy = ra.ModifiedBy;
                        dbra.ModifiedOn = ra.ModifiedOn;
                        dbra.IsDeleted = ra.IsDeleted;
                        dbra.TimeInTreatment = ra.TimeInTreatment;
                        dbra.Version = ra.Version;
                        dbra.LastModAt = ra.LastModAt;
                    }
                }
                db.SaveChanges();
                if (xnras.Count > 0)
                {
                    db.TblReAssessment.AddRange(xnras);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentOccupational(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentOccupational> dbList = db.TblReAssessmentOccupational.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentOccupational> xnList = new List<Models.TblReAssessmentOccupational>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentOccupational xa = new Models.TblReAssessmentOccupational();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyoucurrentlyafulltimestudent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouCurrentlyAfulltimeStudent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyoucurrentlyaparttimestudent":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouCurrentlyAparttimeStudent = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "commentsoccupational":
                                xa.CommentsOccupational = dr[c.ColumnName].ToString();
                                break;
                            case "haveyoufoundaparttimeorfulltimejob":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouFoundAparttimeOrFulltimeJob = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReassessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "whatisyourcurrentemploymentstatus":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.WhatIsYourCurrentEmploymentStatus = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AreYouCurrentlyAfulltimeStudent = xa.AreYouCurrentlyAfulltimeStudent;
                        xdb.AreYouCurrentlyAparttimeStudent = xa.AreYouCurrentlyAparttimeStudent;
                        xdb.CommentsOccupational = xa.CommentsOccupational;
                        xdb.HaveYouFoundAparttimeOrFulltimeJob = xa.HaveYouFoundAparttimeOrFulltimeJob;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReassessmentId = xa.ReassessmentId;
                        xdb.WhatIsYourCurrentEmploymentStatus = xa.WhatIsYourCurrentEmploymentStatus;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentOccupational.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentFamily(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentFamily> dbList = db.TblReAssessmentFamily.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentFamily> xnList = new List<Models.TblReAssessmentFamily>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentFamily xa = new Models.TblReAssessmentFamily();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyousafefromphysicalorsexualabuseinyourhome":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouSafeFromPhysicalOrSexualAbuseInYourHome = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "commentsfamily":
                                xa.CommentsFamily = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveanyopencaseswithyourlocaldepartment":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAnyOpenCasesWithYourLocalDepartment = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhaveenoughmoney":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveEnoughMoney = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhavelegalcustodyofyourchildren":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveLegalCustodyOfYourChildren = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhavestablehousingofyourown":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveStableHousingOfYourOwn = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReassessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AreYouSafeFromPhysicalOrSexualAbuseInYourHome = xa.AreYouSafeFromPhysicalOrSexualAbuseInYourHome;
                        xdb.CommentsFamily = xa.CommentsFamily;
                        xdb.DoYouHaveAnyOpenCasesWithYourLocalDepartment = xa.DoYouHaveAnyOpenCasesWithYourLocalDepartment;
                        xdb.DoYouHaveEnoughMoney = xa.DoYouHaveEnoughMoney;
                        xdb.DoYouHaveLegalCustodyOfYourChildren = xa.DoYouHaveLegalCustodyOfYourChildren;
                        xdb.DoYouHaveStableHousingOfYourOwn = xa.DoYouHaveStableHousingOfYourOwn;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReassessmentId = xa.ReassessmentId;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentFamily.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentLegal (DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentLegal> dbList = db.TblReAssessmentLegal.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentLegal> xnList = new List<Models.TblReAssessmentLegal>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentLegal xa = new Models.TblReAssessmentLegal();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyouinvolvedwithadrugtreatmentcourt":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouInvolvedWithAdrugTreatmentCourt = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyouonprobationorpayrole":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouOnProbationOrPayrole = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "commentslegal":
                                xa.CommentsLegal = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveanyopencriminalcases":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAnyOpenCriminalCases = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhaveanyopenorpendingcourtcases":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAnyOpenOrPendingCourtCases = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhaveanyopenwarrants":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAnyOpenWarrants = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouowemoneyforcourtfinesorfees":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouOweMoneyForCourtFinesOrFees = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoubeenarrested":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouBeenArrested = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReassessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        xdb.AreYouInvolvedWithAdrugTreatmentCourt = xa.AreYouInvolvedWithAdrugTreatmentCourt;
                        xdb.AreYouOnProbationOrPayrole = xa.AreYouOnProbationOrPayrole;
                        xdb.CommentsLegal = xa.CommentsLegal;
                        xdb.DoYouHaveAnyOpenCriminalCases = xa.DoYouHaveAnyOpenCriminalCases;
                        xdb.DoYouHaveAnyOpenOrPendingCourtCases = xa.DoYouHaveAnyOpenOrPendingCourtCases;
                        xdb.DoYouHaveAnyOpenWarrants = xa.DoYouHaveAnyOpenWarrants;
                        xdb.DoYouOweMoneyForCourtFinesOrFees = xa.DoYouOweMoneyForCourtFinesOrFees;
                        xdb.HaveYouBeenArrested = xa.HaveYouBeenArrested;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReassessmentId = xa.ReassessmentId;
                        rc.RowsUpd += 1;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentLegal.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentMentalHealth(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentMentalHealth> dbList = db.TblReAssessmentMentalHealth.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentMentalHealth> xnList = new List<Models.TblReAssessmentMentalHealth>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentMentalHealth xa = new Models.TblReAssessmentMentalHealth();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhaveapsychiatrist":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveApsychiatrist = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoubeenhospitalizedformentalhealthreasons":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouBeenHospitalizedForMentalHealthReasons = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "howhasyourmentalhealthchanged":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HowHasYourMentalHealthChanged = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.DoYouHaveApsychiatrist = xa.DoYouHaveApsychiatrist;
                        xdb.HaveYouBeenHospitalizedForMentalHealthReasons = xa.HaveYouBeenHospitalizedForMentalHealthReasons;
                        xdb.HowHasYourMentalHealthChanged = xa.HowHasYourMentalHealthChanged;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReAssessmentId = xa.ReAssessmentId;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentMentalHealth.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentPhysicalHealth(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentPhysicalHealth> dbList = db.TblReAssessmentPhysicalHealth.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentPhysicalHealth> xnList = new List<Models.TblReAssessmentPhysicalHealth>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentPhysicalHealth xa = new Models.TblReAssessmentPhysicalHealth();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxhepatitiscnegative":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkboxHepatitisCnegative = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxhepatitiscpostive":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkboxHepatitisCpostive = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxhivnegative":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkboxHivnegative = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxhivpostive":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkboxHivpostive = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxna":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ChkboxNa = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "commentsphysicalhealth":
                                xa.CommentsPhysicalHealth = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveaprimarycarepractitionerorclinic":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAprimaryCarePractitionerOrClinic = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoubeentestedforhivandhepatitisc":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouBeenTestedForHivandHepatitisC = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyoucalled911orbeeniitheemergencyroom":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouCalled911OrBeeniItheEmergencyRoom = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyouhadanyunsafesex":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouHadAnyUnsafeSex = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyouinjecteddrugs":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouInjectedDrugs = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "howhasyourphysicalhealthchanged":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HowHasYourPhysicalHealthChanged = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ifyouwerehepatitiscpositive":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IfYouWereHepatitisCpositive = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "ifyouwerehivpositive":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IfYouWereHivpositive = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReassessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.ChkboxHepatitisCnegative = xa.ChkboxHepatitisCnegative;
                        xdb.ChkboxHepatitisCpostive = xa.ChkboxHepatitisCpostive;
                        xdb.ChkboxHivnegative = xa.ChkboxHivnegative;
                        xdb.ChkboxHivpostive = xa.ChkboxHivpostive;
                        xdb.ChkboxNa = xa.ChkboxNa;
                        xdb.CommentsPhysicalHealth = xa.CommentsPhysicalHealth;
                        xdb.DoYouHaveAprimaryCarePractitionerOrClinic = xa.DoYouHaveAprimaryCarePractitionerOrClinic;
                        xdb.HaveYouBeenTestedForHivandHepatitisC = xa.HaveYouBeenTestedForHivandHepatitisC;
                        xdb.HaveYouCalled911OrBeeniItheEmergencyRoom = xa.HaveYouCalled911OrBeeniItheEmergencyRoom;
                        xdb.HaveYouHadAnyUnsafeSex = xa.HaveYouHadAnyUnsafeSex;
                        xdb.HaveYouInjectedDrugs = xa.HaveYouInjectedDrugs;
                        xdb.HowHasYourPhysicalHealthChanged = xa.HowHasYourPhysicalHealthChanged;
                        xdb.IfYouWereHepatitisCpositive = xa.IfYouWereHepatitisCpositive;
                        xdb.IfYouWereHivpositive = xa.IfYouWereHivpositive;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReassessmentId = xa.ReassessmentId;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentPhysicalHealth.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentSubstanceUse(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentSubstanceUse> dbList = db.TblReAssessmentSubstanceUse.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentSubstanceUse> xnList = new List<Models.TblReAssessmentSubstanceUse>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentSubstanceUse xa = new Models.TblReAssessmentSubstanceUse();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "commentssubstanceuse":
                                xa.CommentsSubstanceUse = dr[c.ColumnName].ToString();
                                break;
                            case "doyouusetobaccoorvapenicotine":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouUseTobaccoOrVapeNicotine = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyouhadanoverdose":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.HaveYouHadAnOverdose = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReAssessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.CommentsSubstanceUse = xa.CommentsSubstanceUse;
                        xdb.DoYouUseTobaccoOrVapeNicotine = xa.DoYouUseTobaccoOrVapeNicotine;
                        xdb.HaveYouHadAnOverdose = xa.HaveYouHadAnOverdose;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReAssessmentId = xa.ReAssessmentId;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentSubstanceUse.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentSocial(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentSocial> dbList = db.TblReAssessmentSocial.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentSocial> xnList = new List<Models.TblReAssessmentSocial>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentSocial xa = new Models.TblReAssessmentSocial();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "commentssocial":
                                xa.CommentsSocial = dr[c.ColumnName].ToString();
                                break;
                            case "doyouhaveanyfriendsrorfamilymemberswhodontdrink":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhavefriendsandfamilywhoyoucancountontosupportyou":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouHaveFriendsAndFamilyWhoYouCanCountOnToSupportYou = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouknowofanypeersupport":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouKnowOfAnyPeerSupport = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReassessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;

                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.CommentsSocial = xa.CommentsSocial;
                        xdb.DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink = xa.DoYouHaveAnyFriendsRorFamilyMembersWhoDontDrink;
                        xdb.DoYouHaveFriendsAndFamilyWhoYouCanCountOnToSupportYou = xa.DoYouHaveFriendsAndFamilyWhoYouCanCountOnToSupportYou;
                        xdb.DoYouKnowOfAnyPeerSupport = xa.DoYouKnowOfAnyPeerSupport;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReassessmentId = xa.ReassessmentId;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                        db.TblReAssessmentSocial.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveReAssessmentTreatment(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblReAssessmentTreatment> dbList = db.TblReAssessmentTreatment.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblReAssessmentTreatment> xnList = new List<Models.TblReAssessmentTreatment>();

                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblReAssessmentTreatment xa = new Models.TblReAssessmentTreatment();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                xa.SiteCode = sc;
                                xa.LastModAt = runat;
                                break;
                            case "id":
                                xa.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "areyousatisfiedwith":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.AreYouSatisfiedWith = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ClientId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouplanontaperingoff":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.DoYouPlanOnTaperingOff = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "iseventuallytaperingoff":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.IsEventuallyTaperingOff = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reassessmentid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    xa.ReassessmentId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "whathaveyoulearnedaboutwhatyouprefer":
                                xa.WhatHaveYouLearnedAboutWhatYouPrefer = dr[c.ColumnName].ToString();
                                break;
                            case "whatneedsdoyouhavethatwecanhelpyou":
                                xa.WhatNeedsDoYouHaveThatWeCanHelpYou = dr[c.ColumnName].ToString();
                                break;
                        }
                    }
                    var xdb = dbList.FirstOrDefault(x => x.Id == xa.Id);
                    if (xdb == null)
                    {
                        rc.RowsIns += 1;
                        xnList.Add(xa);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        xdb.AreYouSatisfiedWith = xa.AreYouSatisfiedWith;
                        xdb.ClientId = xa.ClientId;
                        xdb.DoYouPlanOnTaperingOff = xa.DoYouPlanOnTaperingOff;
                        xdb.IsEventuallyTaperingOff = xa.IsEventuallyTaperingOff;
                        xdb.LastModAt = xa.LastModAt;
                        xdb.PreAdmissionId = xa.PreAdmissionId;
                        xdb.ReassessmentId = xa.ReassessmentId;
                        xdb.WhatHaveYouLearnedAboutWhatYouPrefer = xa.WhatHaveYouLearnedAboutWhatYouPrefer;
                        xdb.WhatNeedsDoYouHaveThatWeCanHelpYou = xa.WhatNeedsDoYouHaveThatWeCanHelpYou;
                    }
                }
                db.SaveChanges();
                if (xnList.Count > 0)
                {
                    db.TblReAssessmentTreatment.AddRange(xnList);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAssessmentSubstanceuseHistory(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAssessmentSubstanceUseHistory> ldbras = db.TblAssessmentSubstanceUseHistories.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAssessmentSubstanceUseHistory> xnras = new List<Models.TblAssessmentSubstanceUseHistory>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAssessmentSubstanceUseHistory ra = new Models.TblAssessmentSubstanceUseHistory();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                ra.SiteCode = sc;
                                ra.LastModAt = runat;
                                ra.RowChkSum = int.Parse(dr["RowChkSum"].ToString());
                                ra.RowState = true;
                                break;
                            case "id":
                                ra.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                ra.PreAdmissionId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "assessmentformid":
                                ra.AssessmentFormId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.AssessmentFormId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "cltid":
                                ra.CltId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.CltId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                if (ra.CltId < 0) { ra.RowState = false; }
                                break;
                            case "createdon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    ra.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dateoflastuse":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    ra.DateOfLastUse = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "txepisode":
                                ra.TxEpisode = dr[c.ColumnName].ToString();
                                break;
                            case "substancetype":
                                ra.SubstanceType = dr[c.ColumnName].ToString();
                                break;
                            case "substance":
                                 ra.Substance = dr[c.ColumnName].ToString();
                                break;
                            case "route":
                                ra.Route = dr[c.ColumnName].ToString();
                                break;
                            case "amount":
                                ra.Amount = dr[c.ColumnName].ToString();
                                break;
                            case "frequencyoflastuse":
                                ra.FrequencyOfLastUse = dr[c.ColumnName].ToString();
                                break;
                            case "peakuse":
                                ra.PeakUse = dr[c.ColumnName].ToString();
                                break;
                            case "ageoffirstuse":
                                ra.AgeOfFirstUse = dr[c.ColumnName].ToString();
                                break;
                            case "listsymptoms":
                                ra.ListSymptoms = dr[c.ColumnName].ToString();
                                break;
                            case "notes":
                                ra.Notes = dr[c.ColumnName].ToString();
                                break;
                            case "masterid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.MasterID = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dateofreported":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    ra.DateOfReported = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "withdrawal":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.Withdrawal = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblAssessmentSubstanceUseHistory dbra = ldbras.FirstOrDefault(x => x.SiteCode == ra.SiteCode && x.Id == ra.Id);
                    if (dbra == null)
                    {
                        rc.RowsIns += 1;
                        xnras.Add(ra);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbra.PreAdmissionId = ra.PreAdmissionId;
                        dbra.CltId = ra.CltId;
                        dbra.AssessmentFormId = ra.AssessmentFormId;
                        dbra.AgeOfFirstUse = ra.AgeOfFirstUse;
                        dbra.CreatedOn = ra.CreatedOn;
                        dbra.Amount = ra.Amount;
                        dbra.DateOfLastUse = ra.DateOfLastUse;
                        dbra.DateOfReported = ra.DateOfReported;
                        dbra.FrequencyOfLastUse = ra.FrequencyOfLastUse;
                        dbra.LastModAt = ra.LastModAt;
                        dbra.ListSymptoms = ra.ListSymptoms;
                        dbra.MasterID = ra.MasterID;
                        dbra.Notes = ra.Notes;
                        dbra.PeakUse = ra.PeakUse;
                        dbra.Route = ra.Route;
                        dbra.RowChkSum = ra.RowChkSum;
                        dbra.RowState = ra.RowState;
                        dbra.Substance = ra.Substance;
                        dbra.SubstanceType = ra.SubstanceType;
                        dbra.TxEpisode = ra.TxEpisode;
                        dbra.Withdrawal = ra.Withdrawal;
                    }
                }
                db.SaveChanges();
                if (xnras.Count > 0)
                {
                    db.TblAssessmentSubstanceUseHistories.AddRange(xnras);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
        public Models.RCodes SaveAdmissionAssessmentSubstanceuseHistory(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblAdmissionAssessmentSubstanceUseHistory> ldbras = db.TblAdmissionAssessmentSubstanceUseHistory.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblAdmissionAssessmentSubstanceUseHistory> xnras = new List<Models.TblAdmissionAssessmentSubstanceUseHistory>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblAdmissionAssessmentSubstanceUseHistory ra = new Models.TblAdmissionAssessmentSubstanceUseHistory();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                ra.SiteCode = sc;
                                ra.LastModAt = runat;
                                ra.RowChkSum = int.Parse(dr["RowChkSum"].ToString());
                                ra.RowState = true;
                                break;
                            case "id":
                                ra.Id = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.Id = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                ra.PreAdmissionId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "cltid":
                                ra.CltId = 0;
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.CltId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                if (ra.CltId < 0) { ra.RowState = false; }
                                break;
                            case "createdon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    try
                                    {
                                        ra.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("CreatedOn: " + dr[c.ColumnName].ToString());
                                    }
                                }
                                break;
                            case "dateoflastuse":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    try
                                    {
                                        ra.DateOfLastUse = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    catch(Exception e)
                                    {
                                        Console.WriteLine("Dateoflastuse: " + dr[c.ColumnName].ToString());
                                    }
                                }
                                break;
                            case "txepisode":
                                ra.TxEpisode = dr[c.ColumnName].ToString();
                                break;
                            case "substancetype":
                                ra.SubstanceType = dr[c.ColumnName].ToString();
                                break;
                            case "substance":
                                ra.Substance = dr[c.ColumnName].ToString();
                                break;
                            case "route":
                                ra.Route = dr[c.ColumnName].ToString();
                                break;
                            case "amount":
                                ra.Amount = dr[c.ColumnName].ToString();
                                break;
                            case "frequencyoflastuse":
                                ra.FrequencyOfLastUse = dr[c.ColumnName].ToString();
                                break;
                            case "peakuse":
                                ra.PeakUse = dr[c.ColumnName].ToString();
                                break;
                            case "ageoffirstuse":
                                ra.AgeOfFirstUse = dr[c.ColumnName].ToString();
                                break;
                            case "listsymptoms":
                                ra.ListSymptoms = dr[c.ColumnName].ToString();
                                break;
                            case "notes":
                                ra.Notes = dr[c.ColumnName].ToString();
                                break;
                            case "dateofreported":
                                if (dr[c.ColumnName].ToString().Trim().Length > 6)
                                {
                                    try
                                    {
                                        ra.DateOfReported = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine("DateofReported: " + dr[c.ColumnName].ToString());
                                    }
                                }
                                break;
                            case "withdrawal":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    ra.Withdrawal = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblAdmissionAssessmentSubstanceUseHistory dbra = ldbras.FirstOrDefault(x => x.SiteCode == ra.SiteCode && x.Id == ra.Id);
                    if (dbra == null)
                    {
                        rc.RowsIns += 1;
                        xnras.Add(ra);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbra.PreAdmissionId = ra.PreAdmissionId;
                        dbra.CltId = ra.CltId;
                        dbra.AgeOfFirstUse = ra.AgeOfFirstUse;
                        dbra.CreatedOn = ra.CreatedOn;
                        dbra.Amount = ra.Amount;
                        dbra.DateOfLastUse = ra.DateOfLastUse;
                        dbra.DateOfReported = ra.DateOfReported;
                        dbra.FrequencyOfLastUse = ra.FrequencyOfLastUse;
                        dbra.LastModAt = ra.LastModAt;
                        dbra.ListSymptoms = ra.ListSymptoms;
                        dbra.Notes = ra.Notes;
                        dbra.PeakUse = ra.PeakUse;
                        dbra.Route = ra.Route;
                        dbra.RowChkSum = ra.RowChkSum;
                        dbra.RowState = ra.RowState;
                        dbra.Substance = ra.Substance;
                        dbra.SubstanceType = ra.SubstanceType;
                        dbra.TxEpisode = ra.TxEpisode;
                        dbra.Withdrawal = ra.Withdrawal;
                    }
                }
                db.SaveChanges();
                if (xnras.Count > 0)
                {
                    db.TblAdmissionAssessmentSubstanceUseHistory.AddRange(xnras);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rc;
        }
    }
}
