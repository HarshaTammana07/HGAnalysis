using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveFinancialHardshipApplication (DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblFinancialHardshipApplication> fhaNew = new List<Models.TblFinancialHardshipApplication>();
                List<Models.TblFinancialHardshipApplication> dbfhas = db.TblFinancialHardshipApplications.Where(x => x.SiteCode == sc).ToList();
                
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblFinancialHardshipApplication xfha = new Models.TblFinancialHardshipApplication();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "id":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.Id = int.Parse(r[c.ColumnName].ToString());
                                }
                                xfha.SiteCode = sc;
                                xfha.RowState = true;
                                xfha.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cltid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.CltId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xfha.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                xfha.CreatedBy = r[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xfha.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                xfha.ModifiedBy = r[c.ColumnName].ToString();
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                else { xfha.IsDeleted = false; }
                                if (xfha.IsDeleted.HasValue)
                                {
                                    if (xfha.IsDeleted.Value)
                                    {
                                        xfha.RowState = false;
                                    }
                                }
                                break;
                            case "isidentification":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.IsIdentification = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isincome":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.IsIncome = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtincomeidentification":
                                xfha.txtIncomeIdentification = r[c.ColumnName].ToString();
                                break;
                            case "fhapatientsignature":
                                xfha.FHAPatientSignature = r[c.ColumnName].ToString();
                                break;
                            case "fhapatientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xfha.ExpirationDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fhapatientsignatureby":
                                xfha.FHAPatientSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "txtannualhouseholdincome":
                                xfha.txtAnnualHouseholdIncome = r[c.ColumnName].ToString();
                                break;
                            case "emergencyname":
                                xfha.EmergencyName = r[c.ColumnName].ToString();
                                break;
                            case "emergencyrelation":
                                xfha.EmergencyRelation = r[c.ColumnName].ToString();
                                break;
                            case "emergencyphone":
                                xfha.EmergencyPhone = r[c.ColumnName].ToString();
                                break;
                            case "txtauigross1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIGross1 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauigross2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIGross2 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauigross3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIGross3 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauisocial1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUISocial1 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauisocial2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUISocial2 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauisocial3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUISocial3 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauialimony1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIAlimony1 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauialimony2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIAlimony2 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauialimony3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIAlimony3 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauiself1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUISelf1 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauiself2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUISelf2 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauiself3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUISelf3 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauirent1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIRent1 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauirent2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIRent2 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "txtauirent3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.txtAUIRent3 = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                xfha.Version = r[c.ColumnName].ToString();
                                break;
                            case "iscurrentlyuninsured":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xfha.IscurrentlyUninsured = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "statusofapplication":
                                xfha.StatusofApplication = r[c.ColumnName].ToString();
                                break;
                            case "facts":
                                xfha.Facts = r[c.ColumnName].ToString();
                                break;
                            case "payclassapproved":
                                xfha.PayClassApproved = r[c.ColumnName].ToString();
                                break;
                            case "approvedby":
                                xfha.ApprovedBy = r[c.ColumnName].ToString();
                                break;
                            case "effectivedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xfha.EffectiveDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "expirationdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xfha.ExpirationDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblFinancialHardshipApplication dbxfha = dbfhas.FirstOrDefault(x => x.SiteCode == xfha.SiteCode && x.Id == xfha.Id);
                    if (dbxfha == null)
                    {
                        rc.RowsIns += 1;
                        fhaNew.Add(xfha);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxfha.ApprovedBy = xfha.ApprovedBy;
                        dbxfha.CltId = xfha.CltId;
                        dbxfha.CreatedBy = xfha.CreatedBy;
                        dbxfha.CreatedOn = xfha.CreatedOn;
                        dbxfha.DataFormId = xfha.DataFormId;
                        dbxfha.EffectiveDate = xfha.EffectiveDate;
                        dbxfha.EmergencyName = xfha.EmergencyName;
                        dbxfha.EmergencyPhone = xfha.EmergencyPhone;
                        dbxfha.EmergencyRelation = xfha.EmergencyRelation;
                        dbxfha.ExpirationDate = xfha.ExpirationDate;
                        dbxfha.Facts = xfha.Facts;
                        dbxfha.FHAPatientSignature = xfha.FHAPatientSignature;
                        dbxfha.FHAPatientSignatureBy = xfha.FHAPatientSignatureBy;
                        dbxfha.FHAPatientSignatureDate = xfha.FHAPatientSignatureDate;
                        dbxfha.IscurrentlyUninsured = xfha.IscurrentlyUninsured;
                        dbxfha.IsDeleted = xfha.IsDeleted;
                        dbxfha.IsIdentification = xfha.IsIdentification;
                        dbxfha.IsIncome = xfha.IsIncome;
                        dbxfha.LastModAt = xfha.LastModAt;
                        dbxfha.ModifiedBy = xfha.ModifiedBy;
                        dbxfha.ModifiedOn = xfha.ModifiedOn;
                        dbxfha.PayClassApproved = xfha.PayClassApproved;
                        dbxfha.PreAdmissionId = xfha.PreAdmissionId;
                        dbxfha.RowChkSum = xfha.RowChkSum;
                        dbxfha.RowState = xfha.RowState;
                        dbxfha.StatusofApplication = xfha.StatusofApplication;
                        dbxfha.txtAnnualHouseholdIncome = xfha.txtAnnualHouseholdIncome;
                        dbxfha.txtAUIAlimony1 = xfha.txtAUIAlimony1;
                        dbxfha.txtAUIAlimony2 = xfha.txtAUIAlimony2;
                        dbxfha.txtAUIAlimony3 = xfha.txtAUIAlimony3;
                        dbxfha.txtAUIGross1 = xfha.txtAUIGross1;
                        dbxfha.txtAUIGross2 = xfha.txtAUIGross2;
                        dbxfha.txtAUIGross3 = xfha.txtAUIGross3;
                        dbxfha.txtAUIRent1 = xfha.txtAUIRent1;
                        dbxfha.txtAUIRent2 = xfha.txtAUIRent2;
                        dbxfha.txtAUIRent3 = xfha.txtAUIRent3;
                        dbxfha.txtAUISelf1 = xfha.txtAUISelf1;
                        dbxfha.txtAUISelf2 = xfha.txtAUISelf2;
                        dbxfha.txtAUISelf3 = xfha.txtAUISelf3;
                        dbxfha.txtAUISocial1 = xfha.txtAUISocial1;
                        dbxfha.txtAUISocial2 = xfha.txtAUISocial2;
                        dbxfha.txtAUISocial3 = xfha.txtAUISocial3;
                        dbxfha.txtIncomeIdentification = xfha.txtIncomeIdentification;
                        dbxfha.Version = xfha.Version;
                    }
                }
                db.SaveChanges();
                if (fhaNew.Count > 0)
                {
                    db.TblFinancialHardshipApplications.AddRange(fhaNew);
                    db.SaveChanges();
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
        public Models.RCodes SavePACounselorReview(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPACounselorReview> NewItems = new List<Models.TblPACounselorReview>();
                List<Models.TblPACounselorReview> dbl = db.TblPACounselorReviews.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPACounselorReview xtm = new Models.TblPACounselorReview();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "earlyintervention":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EarlyIntervention = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "outpatienttreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OutpatientTreatment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "intensiveoutpatient":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.IntensiveOutpatient = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "partialhospitalization":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PartialHospitalization = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "residentialinpatient":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ResidentialInpatient = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "medmanagedintensiveinpatient":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.MedManagedIntensiveInpatient = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ots":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OTS = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "obot":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OBOT = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "otp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OTP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "obat":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OBAT = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "withdrawalmanagement":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.WithdrawalManagement = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "copephase1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CopePhase1 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "copephase2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CopePhase2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "copephase3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CopePhase3 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "induction":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Induction = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "stabilization":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Stabilization = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "maintenance":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Maintenance = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "datecompleted":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xtm.DateCompleted = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "usescore":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.UseScore = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "riskscore":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RiskScore = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "protectivescore":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ProtectiveScore = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clinicalsummary":
                                xtm.ClinicalSummary = r[c.ColumnName].ToString();
                                break;
                            case "patientsignature":
                                xtm.PatientSignature = r[c.ColumnName].ToString();
                                break;
                            case "patientsignatureby":
                                xtm.PatientSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xtm.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "counselorsignature":
                                xtm.CounselorSignature = r[c.ColumnName].ToString();
                                break;
                            case "counselorsignatureby":
                                xtm.CounselorSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "counselorsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xtm.CounselorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "supervisorsignature":
                                xtm.SupervisorSignature = r[c.ColumnName].ToString();
                                break;
                            case "supervisorsignatureby":
                                xtm.SupervisorSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "supervisorsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    xtm.SupervisorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblPACounselorReview dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.ClinicalSummary = xtm.ClinicalSummary;
                        dbxtm.CopePhase1 = xtm.CopePhase1;
                        dbxtm.CopePhase2 = xtm.CopePhase2;
                        dbxtm.CopePhase3 = xtm.CopePhase3;
                        dbxtm.CounselorSignature = xtm.CounselorSignature;
                        dbxtm.CounselorSignatureBy = xtm.CounselorSignatureBy;
                        dbxtm.CounselorSignatureDate = xtm.CounselorSignatureDate;
                        dbxtm.DateCompleted = xtm.DateCompleted;
                        dbxtm.EarlyIntervention = xtm.EarlyIntervention;
                        dbxtm.Induction = xtm.Induction;
                        dbxtm.IntensiveOutpatient = xtm.IntensiveOutpatient;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.Maintenance = xtm.Maintenance;
                        dbxtm.MedManagedIntensiveInpatient = xtm.MedManagedIntensiveInpatient;
                        dbxtm.OBAT = xtm.OBAT;
                        dbxtm.OBOT = xtm.OBOT;
                        dbxtm.OTP = xtm.OTP;
                        dbxtm.OTS = xtm.OTS;
                        dbxtm.OutpatientTreatment = xtm.OutpatientTreatment;
                        dbxtm.PartialHospitalization = xtm.PartialHospitalization;
                        dbxtm.PatientSignature = xtm.PatientSignature;
                        dbxtm.PatientSignatureBy = xtm.PatientSignatureBy;
                        dbxtm.PatientSignatureDate = xtm.PatientSignatureDate;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.ProtectiveScore = xtm.ProtectiveScore;
                        dbxtm.ResidentialInpatient = xtm.ResidentialInpatient;
                        dbxtm.RiskScore = xtm.RiskScore;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.Stabilization = xtm.Stabilization;
                        dbxtm.SupervisorSignature = xtm.SupervisorSignature;
                        dbxtm.SupervisorSignatureBy = xtm.SupervisorSignatureBy;
                        dbxtm.SupervisorSignatureDate = xtm.SupervisorSignatureDate;
                        dbxtm.UseScore = xtm.UseScore;
                        dbxtm.WithdrawalManagement = xtm.WithdrawalManagement;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPACounselorReviews.AddRange(NewItems);
                    db.SaveChanges();
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
        public Models.RCodes SavePADimension1(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPADimension1> NewItems = new List<Models.TblPADimension1>();
                List<Models.TblPADimension1> dbl = db.TblPADimension1s.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPADimension1 xtm = new Models.TblPADimension1();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lastuds":
                                xtm.LastUDS = r[c.ColumnName].ToString();
                                break;
                            case "udsresult":
                                xtm.UDSResult = r[c.ColumnName].ToString();
                                break;
                            case "illegalsubstances":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "illegalsubstancesbox":
                                xtm.IllegalSubstancesBox = r[c.ColumnName].ToString();
                                break;
                            case "overdose":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "overdosebox":
                                xtm.OverdoseBox = r[c.ColumnName].ToString();
                                break;
                            case "narcanavailable":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.NarcanAvailable = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cravings":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Cravings = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cravingrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CravingRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dimension1asamrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Dimension1ASAMRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uaeval":
                                xtm.UAEval = r[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblPADimension1 dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.CravingRating = xtm.CravingRating;
                        dbxtm.Cravings = xtm.Cravings;
                        dbxtm.Dimension1ASAMRating = xtm.Dimension1ASAMRating;
                        dbxtm.IllegalSubstances = xtm.IllegalSubstances;
                        dbxtm.IllegalSubstancesBox = xtm.IllegalSubstancesBox;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.LastUDS = xtm.LastUDS;
                        dbxtm.NarcanAvailable = xtm.NarcanAvailable;
                        dbxtm.Overdose = xtm.Overdose;
                        dbxtm.OverdoseBox = xtm.OverdoseBox;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.UAEval = xtm.UAEval;
                        dbxtm.UDSResult = xtm.UDSResult;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPADimension1s.AddRange(NewItems);
                    db.SaveChanges();
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

        public Models.RCodes SavePADimension2(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPADimension2> NewItems = new List<Models.TblPADimension2>();
                List<Models.TblPADimension2> dbl = db.TblPADimension2s.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPADimension2 xtm = new Models.TblPADimension2();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "physicalhealthchange":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PhysicalHealthChange = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "called911":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Called911 = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "called911box":
                                xtm.Called911Box = r[c.ColumnName].ToString();
                                break;
                            case "worseningmedicalcondition":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.WorseningMedicalCondition = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "worseningmedicalconditionbox":
                                xtm.WorseningMedicalConditionBox = r[c.ColumnName].ToString();
                                break;
                            case "primarycareprovider":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PrimaryCareProvider = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "primarycareproviderbox":
                                xtm.PrimaryCareProviderBox = r[c.ColumnName].ToString();
                                break;
                            case "unprotectedsex":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.UnprotectedSex = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "druginjection":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DrugInjection = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "sharingdrug":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.SharingDrug = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hivhepatits":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.HIVHepatits = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hivhepatitisbox":
                                xtm.HIVHepatitisBox = r[c.ColumnName].ToString();
                                break;
                            case "tobacconicotine":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.TobaccoNicotine = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "tobacconicotinefrequency":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.TobaccoNicotineFrequency = r[c.ColumnName].ToString();
                                }
                                break;
                            case "discontinuetobacconicotine":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DiscontinueTobaccoNicotine = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dimension2asamrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Dimension2ASAMRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblPADimension2 dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.Called911 = xtm.Called911;
                        dbxtm.Called911Box = xtm.Called911Box;
                        dbxtm.Dimension2ASAMRating = xtm.Dimension2ASAMRating;
                        dbxtm.DiscontinueTobaccoNicotine = xtm.DiscontinueTobaccoNicotine;
                        dbxtm.DrugInjection = xtm.DrugInjection;
                        dbxtm.HIVHepatitisBox = xtm.HIVHepatitisBox;
                        dbxtm.HIVHepatits = xtm.HIVHepatits;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.PhysicalHealthChange = xtm.PhysicalHealthChange;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.PrimaryCareProvider = xtm.PrimaryCareProvider;
                        dbxtm.PrimaryCareProviderBox = xtm.PrimaryCareProviderBox;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.SharingDrug = xtm.SharingDrug;
                        dbxtm.TobaccoNicotine = xtm.TobaccoNicotine;
                        dbxtm.TobaccoNicotineFrequency = xtm.TobaccoNicotineFrequency;
                        dbxtm.UnprotectedSex = xtm.UnprotectedSex;
                        dbxtm.WorseningMedicalCondition = xtm.WorseningMedicalCondition;
                        dbxtm.WorseningMedicalConditionBox = xtm.WorseningMedicalConditionBox;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPADimension2s.AddRange(NewItems);
                    db.SaveChanges();
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

        public Models.RCodes SavePADimension3(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPADimension3> NewItems = new List<Models.TblPADimension3>();
                List<Models.TblPADimension3> dbl = db.TblPADimension3s.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPADimension3 xtm = new Models.TblPADimension3();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "mentalhealthchange":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.MentalHealthChange = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "mentalhealthhospitalized":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.MentalHealthHospitalized = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "mentalhealthhospitalizedbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.MentalHealthHospitalizedbox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "worseningmentalhealth":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.WorseningMentalHealth = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "worseningmentalhealthbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.WorseningMentalHealthBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "agitation":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Agitation = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "decreasedpleasure":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DecreasedPleasure = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "anxiety":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Anxiety = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lackofinterest":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LackofInterest = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "confusion":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Confusion = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "panicattacks":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PanicAttacks = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "brainfog":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.BrainFog = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "numbness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Numbness = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "insomnia":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Insomnia = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "troublefallingasleep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.TroubleFallingAsleep = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "troublewakingup":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.TroubleWakingUp = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "headaches":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Headaches = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "stomachissues":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.StomachIssues = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fatigue":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Fatigue = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "restlessness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Restlessness = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "tearfulness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Tearfulness = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "increasedappetite":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.IncreasedAppetite = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "decreasedappetite":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DecreasedAppetite = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "feelingempty":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Feelingempty = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "irritability":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Irritability = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "anger":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Anger = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "guiltshame":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.GuiltShame = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "moodswings":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.MoodSwings = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "decreasedselfcontrol":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DecreasedSelfControl = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "nightmares":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Nightmares = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "decreasedenergy":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DecreasedEnergy = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "increasedenergy":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.IncreasedEnergy = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lackoffocus":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LackofFocus = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hallucinations":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Hallucinations = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isolation":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Isolation = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "obsessiveworryingthoughts":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ObsessiveWorryingThoughts = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lackofmotivation":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LackofMotivation = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "forgetfulness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Forgetfulness = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "nervousness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Nervousness = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "persistentsadness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PersistentSadness = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "disorganizedconfusedthoughts":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.DisorganizedConfusedThoughts = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "othermentalsymptoms":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OtherMentalSymptoms = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "othermentalsymptomsbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.OtherMentalSymptomsBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "wisheddead":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.WishedDead = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "killingyourself":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.KillingYourself = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "dimension3asamrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Dimension3ASAMRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblPADimension3 dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.Agitation = xtm.Agitation;
                        dbxtm.Anger = xtm.Anger;
                        dbxtm.Anxiety = xtm.Anxiety;
                        dbxtm.BrainFog = xtm.BrainFog;
                        dbxtm.Confusion = xtm.Confusion;
                        dbxtm.DecreasedAppetite = xtm.DecreasedAppetite;
                        dbxtm.DecreasedEnergy = xtm.DecreasedEnergy;
                        dbxtm.DecreasedPleasure = xtm.DecreasedPleasure;
                        dbxtm.DecreasedSelfControl = xtm.DecreasedSelfControl;
                        dbxtm.Dimension3ASAMRating = xtm.Dimension3ASAMRating;
                        dbxtm.DisorganizedConfusedThoughts = xtm.DisorganizedConfusedThoughts;
                        dbxtm.Fatigue = xtm.Fatigue;
                        dbxtm.Feelingempty = xtm.Feelingempty;
                        dbxtm.Forgetfulness = xtm.Forgetfulness;
                        dbxtm.GuiltShame = xtm.GuiltShame;
                        dbxtm.Hallucinations = xtm.Hallucinations;
                        dbxtm.Headaches = xtm.Headaches;
                        dbxtm.IncreasedAppetite = xtm.IncreasedAppetite;
                        dbxtm.IncreasedEnergy = xtm.IncreasedEnergy;
                        dbxtm.Insomnia = xtm.Insomnia;
                        dbxtm.Irritability = xtm.Irritability;
                        dbxtm.Isolation = xtm.Isolation;
                        dbxtm.KillingYourself = xtm.KillingYourself;
                        dbxtm.LackofFocus = xtm.LackofFocus;
                        dbxtm.LackofInterest = xtm.LackofInterest;
                        dbxtm.LackofMotivation = xtm.LackofMotivation;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.MentalHealthChange = xtm.MentalHealthChange;
                        dbxtm.MentalHealthHospitalized = xtm.MentalHealthHospitalized;
                        dbxtm.MentalHealthHospitalizedbox = xtm.MentalHealthHospitalizedbox;
                        dbxtm.MoodSwings = xtm.MoodSwings;
                        dbxtm.Nervousness = xtm.Nervousness;
                        dbxtm.Nightmares = xtm.Nightmares;
                        dbxtm.Numbness = xtm.Numbness;
                        dbxtm.ObsessiveWorryingThoughts = xtm.ObsessiveWorryingThoughts;
                        dbxtm.OtherMentalSymptoms = xtm.OtherMentalSymptoms;
                        dbxtm.OtherMentalSymptomsBox = xtm.OtherMentalSymptomsBox;
                        dbxtm.PanicAttacks = xtm.PanicAttacks;
                        dbxtm.PersistentSadness = xtm.PersistentSadness;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.Restlessness = xtm.Restlessness;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.StomachIssues = xtm.StomachIssues;
                        dbxtm.Tearfulness = xtm.Tearfulness;
                        dbxtm.TroubleFallingAsleep = xtm.TroubleFallingAsleep;
                        dbxtm.TroubleWakingUp = xtm.TroubleWakingUp;
                        dbxtm.WishedDead = xtm.WishedDead;
                        dbxtm.WorseningMentalHealth = xtm.WorseningMentalHealth;
                        dbxtm.WorseningMentalHealthBox = xtm.WorseningMentalHealthBox;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPADimension3s.AddRange(NewItems);
                    db.SaveChanges();
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

        public Models.RCodes SavePADimension4(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPADimension4> NewItems = new List<Models.TblPADimension4>();
                List<Models.TblPADimension4> dbl = db.TblPADimension4s.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPADimension4 xtm = new Models.TblPADimension4();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "motivationforchange":
                                xtm.MotivationforChange = r[c.ColumnName].ToString();
                                break;
                            case "treatmentsatisfaction":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.TreatmentSatisfaction = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentsatisfactionbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.TreatmentSatisfactionBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "eventuallydiscontinuing":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EventuallyDiscontinuing = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "discontinuing3to6months":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Discontinuing3to6Months = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "strengths":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Strengths = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "needs":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Needs = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "abilities":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Abilities = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "preferedfortreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreferedforTreatment = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "dimension4asamrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Dimension4ASAMRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblPADimension4 dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.Abilities = xtm.Abilities;
                        dbxtm.Dimension4ASAMRating = xtm.Dimension4ASAMRating;
                        dbxtm.Discontinuing3to6Months = xtm.Discontinuing3to6Months;
                        dbxtm.EventuallyDiscontinuing = xtm.EventuallyDiscontinuing;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.MotivationforChange = xtm.MotivationforChange;
                        dbxtm.Needs = xtm.Needs;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.PreferedforTreatment = xtm.PreferedforTreatment;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.Strengths = xtm.Strengths;
                        dbxtm.TreatmentSatisfaction = xtm.TreatmentSatisfaction;
                        dbxtm.TreatmentSatisfactionBox = xtm.TreatmentSatisfactionBox;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPADimension4s.AddRange(NewItems);
                    db.SaveChanges();
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

        public Models.RCodes SavePADimension5(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPADimension5> NewItems = new List<Models.TblPADimension5>();
                List<Models.TblPADimension5> dbl = db.TblPADimension5s.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPADimension5 xtm = new Models.TblPADimension5();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "triggers":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Triggers = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "copingstrategies":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CopingStrategies = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "continueusing":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ContinueUsing = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "continueusingbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ContinueUsingBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "employmentstatus":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EmploymentStatus = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "employmentstatusother":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EmploymentStatusOther = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "partfulltime":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PartFullTime = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "arrested":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Arrested = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "changeinlegalstatus":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ChangeinLegalStatus = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "changeinlegalstatusbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ChangeinLegalStatusBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "financialtrouble":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.FinancialTrouble = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dimension5asamrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Dimension5ASAMRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblPADimension5 dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.Arrested = xtm.Arrested;
                        dbxtm.ChangeinLegalStatus = xtm.ChangeinLegalStatus;
                        dbxtm.ChangeinLegalStatusBox = xtm.ChangeinLegalStatusBox;
                        dbxtm.ContinueUsing = xtm.ContinueUsing;
                        dbxtm.ContinueUsingBox = xtm.ContinueUsingBox;
                        dbxtm.CopingStrategies = xtm.CopingStrategies;
                        dbxtm.Dimension5ASAMRating = xtm.Dimension5ASAMRating;
                        dbxtm.EmploymentStatus = xtm.EmploymentStatus;
                        dbxtm.EmploymentStatusOther = xtm.EmploymentStatusOther;
                        dbxtm.FinancialTrouble = xtm.FinancialTrouble;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.PartFullTime = xtm.PartFullTime;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.Triggers = xtm.Triggers;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPADimension5s.AddRange(NewItems);
                    db.SaveChanges();
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

        public Models.RCodes SavePADimension6(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime runat = DateTime.Now;
                List<Models.TblPADimension6> NewItems = new List<Models.TblPADimension6>();
                List<Models.TblPADimension6> dbl = db.TblPADimension6s.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPADimension6 xtm = new Models.TblPADimension6();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "periodicreassessmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PeriodicReassessmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                xtm.SiteCode = sc;
                                xtm.RowState = true;
                                xtm.LastModAt = runat;
                                break;
                            case "rowchksum":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentlylivingother":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CurrentlyLivingOther = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "environmentstability":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EnvironmentStability = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "environmentstabilitybox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EnvironmentStabilityBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "safefromexploitation":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.SafefromExploitation = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "safefromexploitationbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.SafefromExploitationBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "threats":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Threats = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "threatsbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ThreatsBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "children":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Children = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "childrenage":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ChildrenAge = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "childrenagebox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ChildrenAgeBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "childrenlegalcustody":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ChildrenLegalCustody = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "childfamilyservicesopencases":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.ChildFamilyServicesOpenCases = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "friendsfamilysupport":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.FriendsFamilySupport = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "enoughmoney":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.EnoughMoney = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "familyfriendsinrecovery":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.FamilyFriendsinRecovery = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentlyconnectedsupport":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CurrentlyConnectedSupport = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentlyconnectedsupportbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.CurrentlyConnectedSupportBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "barriers":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Barriers = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "barriersbox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.BarriersBox = r[c.ColumnName].ToString().Trim();
                                }
                                break;
                            case "dimension6asamrating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Dimension6ASAMRating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "livesalone":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LivesAlone = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "houseapartment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.HouseApartment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "livekids":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LiveKids = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "shelter":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Shelter = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "livespartnerspouse":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LivesPartnerSpouse = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "soberlivinghome":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.SoberLivingHome = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "livesfamily":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LivesFamily = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "unhoused":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Unhoused = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "livesfriends":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.LivesFriends = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "other":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    xtm.Other = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblPADimension6 dbxtm = dbl.FirstOrDefault(x => x.SiteCode == xtm.SiteCode && x.PeriodicReassessmentId == xtm.PeriodicReassessmentId);
                    if (dbxtm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(xtm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtm.Barriers = xtm.Barriers;
                        dbxtm.BarriersBox = xtm.BarriersBox;
                        dbxtm.ChildFamilyServicesOpenCases = xtm.ChildFamilyServicesOpenCases;
                        dbxtm.Children = xtm.Children;
                        dbxtm.ChildrenAge = xtm.ChildrenAge;
                        dbxtm.ChildrenAgeBox = xtm.ChildrenAgeBox;
                        dbxtm.ChildrenLegalCustody = xtm.ChildrenLegalCustody;
                        dbxtm.CurrentlyConnectedSupport = xtm.CurrentlyConnectedSupport;
                        dbxtm.CurrentlyConnectedSupportBox = xtm.CurrentlyConnectedSupportBox;
                        dbxtm.CurrentlyLivingOther = xtm.CurrentlyLivingOther;
                        dbxtm.Dimension6ASAMRating = xtm.Dimension6ASAMRating;
                        dbxtm.EnoughMoney = xtm.EnoughMoney;
                        dbxtm.EnvironmentStability = xtm.EnvironmentStability;
                        dbxtm.EnvironmentStabilityBox = xtm.EnvironmentStabilityBox;
                        dbxtm.FamilyFriendsinRecovery = xtm.FamilyFriendsinRecovery;
                        dbxtm.FriendsFamilySupport = xtm.FriendsFamilySupport;
                        dbxtm.HouseApartment = xtm.HouseApartment;
                        dbxtm.LastModAt = xtm.LastModAt;
                        dbxtm.LiveKids = xtm.LiveKids;
                        dbxtm.LivesAlone = xtm.LivesAlone;
                        dbxtm.LivesFamily = xtm.LivesFamily;
                        dbxtm.LivesFriends = xtm.LivesFriends;
                        dbxtm.LivesPartnerSpouse = xtm.LivesPartnerSpouse;
                        dbxtm.Other = xtm.Other;
                        dbxtm.PreAdmissionId = xtm.PreAdmissionId;
                        dbxtm.RowChkSum = xtm.RowChkSum;
                        dbxtm.RowState = xtm.RowState;
                        dbxtm.SafefromExploitation = xtm.SafefromExploitation;
                        dbxtm.SafefromExploitationBox = xtm.SafefromExploitationBox;
                        dbxtm.Shelter = xtm.Shelter;
                        dbxtm.SoberLivingHome = xtm.SoberLivingHome;
                        dbxtm.Threats = xtm.Threats;
                        dbxtm.ThreatsBox = xtm.ThreatsBox;
                        dbxtm.Unhoused = xtm.Unhoused;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblPADimension6s.AddRange(NewItems);
                    db.SaveChanges();
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

        public Models.RCodes SavePA (DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rcodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (tbl.Rows.Count > 0)
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblPA> Newitems = new List<Models.TblPA>();
                List<Models.TblPA> dbList = db.TblPAs.Where(x => x.SiteCode.Trim() == sc.Trim()).ToList();
                try
                {
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblPA pa = new Models.TblPA();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    pa.SiteCode = sc.Trim();
                                    break;
                                case "id":
                                    pa.Id = double.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "dataformid":
                                    pa.DataFormId = double.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "preadmissionid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        pa.PreAdmissionId = double.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        pa.ClientId = double.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "date":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        pa.Date = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "currentpathway":
                                    pa.CurrentPathway = r[c.ColumnName].ToString();
                                    break;
                                case "currentpathwayphase":
                                    pa.CurrentPathwayPhase = r[c.ColumnName].ToString();
                                    break;
                                case "completedat":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        pa.CompletedAt = double.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "completedatothers":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        pa.CompletedAtOthers = r[c.ColumnName].ToString().Trim();
                                    }
                                    break;
                                case "isdeleted":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        if (bool.Parse(r[c.ColumnName].ToString()) == true)
                                        {
                                            pa.IsDeleted = 1;
                                        }
                                        else { pa.IsDeleted = 0; }
                                    }
                                    break;
                                case "createdby":
                                    pa.CreatedBy = r[c.ColumnName].ToString();
                                    break;
                                case "createdon":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        pa.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedby":
                                    pa.ModifiedBy = r[c.ColumnName].ToString();
                                    break;
                                case "modifiedon":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        pa.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "version":
                                    pa.Version = r[c.ColumnName].ToString();
                                    if (pa.Version.Length > 2)
                                    {
                                        pa.Version = pa.Version.Substring(0, 2);
                                    }
                                    break;
                            }
                        }
                        Models.TblPA tblPA = dbList.FirstOrDefault(x => x.SiteCode.Trim() == pa.SiteCode.Trim() && x.Id == pa.Id);
                        if (tblPA == null)
                        {
                            Newitems.Add(pa);
                            rcodes.RowsIns += 1;
                        }
                        else
                        {
                            rcodes.RowsUpd += 1;
                            tblPA.ClientId = pa.ClientId;
                            tblPA.CompletedAt = pa.CompletedAt;
                            tblPA.CompletedAtOthers = pa.CompletedAtOthers;
                            tblPA.CreatedBy = pa.CreatedBy;
                            tblPA.CreatedOn = pa.CreatedOn;
                            tblPA.CurrentPathway = pa.CurrentPathway;
                            tblPA.CurrentPathwayPhase = pa.CurrentPathwayPhase;
                            tblPA.DataFormId = pa.DataFormId;
                            tblPA.Date = pa.Date;
                            tblPA.IsDeleted = pa.IsDeleted;
                            tblPA.ModifiedBy = pa.ModifiedBy;
                            tblPA.ModifiedOn = pa.ModifiedOn;
                            tblPA.PreAdmissionId = pa.PreAdmissionId;
                            tblPA.Version = pa.Version;
                        }
                    }
                    db.SaveChanges();
                    if (Newitems.Count > 0)
                    {
                        db.TblPAs.AddRange(Newitems);
                        db.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    rcodes.IsResult = false;
                    rcodes.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    {
                        rcodes.ExceptInnerMsg = e.InnerException.Message;
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return rcodes;
        }

        public Models.RCodes SavedropDownListItems(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rcodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (tbl.Rows.Count > 0)
            {
                try
                {
                    DateTime runat = DateTime.Now;
                    if (db == null) { db = new Models.BHG_DRContext(); }
                    List<Models.TblDropDownListItems> Newitems = new List<Models.TblDropDownListItems>();
                    List<Models.TblDropDownListItems> dbList = db.TblDropDownListItems.Where(x => x.SiteCode.Trim() == sc.Trim()).ToList();

                    foreach(DataRow r in tbl.Rows)
                    {
                        Models.TblDropDownListItems itm = new Models.TblDropDownListItems();
                        foreach(DataColumn c in tbl.Columns)
                        {
                            switch(c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    itm.SiteCode = r[c.ColumnName].ToString();
                                    break;
                                case "id":
                                    itm.Id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "dropdownlistitem":
                                    itm.DropDownListItem = r[c.ColumnName].ToString();
                                    break;
                                case "dropdownlistid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        itm.DropDownListId = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    else
                                    { itm.DropDownListId = 0; }
                                    break;
                                case "ddapcode":
                                    itm.ddapcode = r[c.ColumnName].ToString().Trim();
                                    break;
                            }
                        }
                        Models.TblDropDownListItems dbx = dbList.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                        if (dbx == null)
                        {
                            Newitems.Add(itm);
                            rcodes.RowsIns += 1;
                        }
                        else
                        {
                            if (dbx.DropDownListItem != itm.DropDownListItem)
                            {
                                dbx.DropDownListItem = itm.DropDownListItem;
                            }
                            if (dbx.DropDownListId != itm.DropDownListId)
                            {
                                dbx.DropDownListId = itm.DropDownListId;
                            }
                            if (dbx.ddapcode.Trim() != itm.ddapcode)
                            {
                                dbx.ddapcode = itm.ddapcode;
                            }
                        }
                    }
                    db.SaveChanges();
                    if (Newitems.Count > 0)
                    {
                        db.TblDropDownListItems.AddRange(Newitems);
                        db.SaveChanges();
                    }
                }
                catch (Exception e)
                {
                    rcodes.IsResult = false;
                    rcodes.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    {
                        rcodes.ExceptInnerMsg = e.InnerException.Message;
                        Console.WriteLine(e.InnerException.Message);
                    }
                } 
            }
            return rcodes;
        }
    }

}

