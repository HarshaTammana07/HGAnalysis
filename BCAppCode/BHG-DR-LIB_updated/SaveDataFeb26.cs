using BHG_DR_LIB.Models;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Markup;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveConsenttoMarketing(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblConsenttoMarketing> dblist = db.TblConsenttoMarketings.Where(x => x.SiteCode == sc).ToList();
                List<TblConsenttoMarketing> newItems = new List<TblConsenttoMarketing>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblConsenttoMarketing itm = new TblConsenttoMarketing();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;
                                itm.LastModAt = runat;
                                itm.RowState = 1;
                                break;
                            case "id":
                                itm.Id = int.Parse(r["id"].ToString());
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "text":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Text = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "email":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Email = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "phone":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Phone = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignature":
                                itm.PatientSignature = r[c.ColumnName].ToString();
                                break;
                            case "patientsignatureby":
                                itm.PatientSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                itm.CreatedBy = r[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                itm.ModifiedBy = r[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                    if (itm.IsDeleted == true) { itm.RowState = 0; }
                                }
                                break;
                        }
                    }
                    TblConsenttoMarketing dbitm = dblist.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm != null)
                    {
                        rc.RowsUpd += 1;
                        dbitm.PreAdmissionId = itm.PreAdmissionId;
                        dbitm.ClientId = itm.ClientId;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.Email = itm.Email;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.LastModAt = itm.LastModAt;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedOn = itm.ModifiedOn;
                        dbitm.PatientSignature = itm.PatientSignature;
                        dbitm.PatientSignatureBy = itm.PatientSignatureBy;
                        dbitm.PatientSignatureDate = itm.PatientSignatureDate;
                        dbitm.Phone = itm.Phone;
                        dbitm.RowState = itm.RowState;
                        dbitm.Text = itm.Text;
                    }
                    else
                    {
                        rc.RowsIns += 1;
                        newItems.Add(itm);
                    }
                }
                db.SaveChanges();
                if (newItems.Count > 0)
                {
                    db.TblConsenttoMarketings.AddRange(newItems);
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
        public Models.RCodes SaveNewDischargeTransferPlanForm(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblNewDischargeTransferPlanForm> dblist = db.TblNewDischargeTransferPlanForms.Where(x => x.SiteCode == sc).ToList();
                List<TblNewDischargeTransferPlanForm> newItms = new List<TblNewDischargeTransferPlanForm>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblNewDischargeTransferPlanForm itm = new TblNewDischargeTransferPlanForm();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;
                                itm.LastModAt = runat;
                                itm.RowState = 1;
                                break;
                            case "id":
                                itm.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "admissiondate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AdmissionDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dischargedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DischargeDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dischargereason":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DischargeReason = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "primarydischargereason":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PrimaryDischargeReason = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "programid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ProgramId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "sammsdischargereason":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.SammsDischargeReason = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "secondarydischargereason":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.SecondaryDischargeReason = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Version = (r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    TblNewDischargeTransferPlanForm dbitm = dblist.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm == null)
                    {
                        newItms.Add(itm);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbitm.AdmissionDate = itm.AdmissionDate;
                        dbitm.ClientId = itm.ClientId;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.DischargeDate = itm.DischargeDate;
                        dbitm.DischargeReason = itm.DischargeReason;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.LastModAt = itm.LastModAt;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedOn = itm.ModifiedOn;
                        dbitm.PreAdmissionId = itm.PreAdmissionId;
                        dbitm.PrimaryDischargeReason = itm.PrimaryDischargeReason;
                        dbitm.ProgramId = itm.ProgramId;
                        dbitm.RowState = itm.RowState;
                        dbitm.SammsDischargeReason = itm.SammsDischargeReason;
                        dbitm.SecondaryDischargeReason = itm.SecondaryDischargeReason;
                        dbitm.Version = itm.Version;
                    }
                }
                db.SaveChanges();
                if (newItms.Count > 0)
                {
                    db.TblNewDischargeTransferPlanForms.AddRange(newItms);
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
        public Models.RCodes SaveMNTreatmentServiceReview(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblMNTreatmentServiceReview> dblist = db.TblMNTreatmentServiceReviews.Where(x => x.SiteCode == sc).ToList();
                List<TblMNTreatmentServiceReview> newItems = new List<TblMNTreatmentServiceReview>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblMNTreatmentServiceReview itm = new TblMNTreatmentServiceReview();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;
                                itm.LastModAt = runat;
                                itm.RowState = 1;
                                break;
                            case "id":
                                itm.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "admissionvulnerableadult":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AdmissionVulnerableAdult = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "agreetreatmentplanningchanges":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AgreeTreatmentPlanningChanges = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "aremethodseffective":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AreMethodsEffective = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "coodinationwithreferrals":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CoordinationWithReferrals = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "coodinationwithreferralsexplain":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CoordinationWithReferralsExplain = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentyvulnerableadult":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrentlyVulnerableAdult = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "indivdualabuseprevention":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IndividualAbusePrevention = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "physicalmentalhealthproblems":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PhysicalMentalHealthProblems = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "reasonorassessmentprocess":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ReasonorAssessmentProcess = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "reviewperiod":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ReviewPeriod = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "reviewperiodtoday":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ReviewPeriodToday = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "servicedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ServiceDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "sessionendtime":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.SessionEndTime = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "sessionstarttime":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.SessionStartTime = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "significanttreatmentplanning":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.SignificantTreatmentPlanning = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignature":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignature = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignatureby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignatureBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "telehealthsession":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TelehealthSession = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "toxicologyresults":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ToxicologyResults = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentgoal":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentGoal = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentplanchangesexplain":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentPlanChangesExplain = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentplanning":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentPlanning = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentplanningchanges":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentPlanningChanges = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentservicereview":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentServiceReview = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentservicereviewmhreferralsmade":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentServiceReviewMHReferralsMade = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentservicereviewmissappointment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentServiceReviewMissAppointment = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "treatmentservicereviewreferralsmade":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TreatmentServiceReviewReferralsMade = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    TblMNTreatmentServiceReview dbitm = dblist.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm != null)
                    {
                        rc.RowsUpd += 1;
                        dbitm.AdmissionVulnerableAdult = itm.AdmissionVulnerableAdult;
                        dbitm.AgreeTreatmentPlanningChanges = itm.AgreeTreatmentPlanningChanges;
                        dbitm.AreMethodsEffective = itm.AreMethodsEffective;
                        dbitm.ClientId = itm.ClientId;
                        dbitm.CoordinationWithReferrals = itm.CoordinationWithReferrals;
                        dbitm.CoordinationWithReferralsExplain = itm.CoordinationWithReferralsExplain;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.CurrentlyVulnerableAdult = itm.CurrentlyVulnerableAdult;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.IndividualAbusePrevention = itm.IndividualAbusePrevention;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.LastModAt = itm.LastModAt;
                        dbitm.StaffSignature = itm.StaffSignature;
                        dbitm.StaffSignatureBy = itm.StaffSignatureBy;
                        dbitm.StaffSignatureDate = itm.StaffSignatureDate;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedOn = itm.ModifiedOn;
                        dbitm.PhysicalMentalHealthProblems = itm.PhysicalMentalHealthProblems;
                        dbitm.PreAdmissionId = itm.PreAdmissionId;
                        dbitm.ReasonorAssessmentProcess = itm.ReasonorAssessmentProcess;
                        dbitm.ReviewPeriod = itm.ReviewPeriod;
                        dbitm.ReviewPeriodToday = itm.ReviewPeriodToday;
                        dbitm.RowState = itm.RowState;
                        dbitm.ServiceDate = itm.ServiceDate;
                        dbitm.SessionEndTime = itm.SessionEndTime;
                        dbitm.SessionStartTime = itm.SessionStartTime;
                        dbitm.SignificantTreatmentPlanning = itm.SignificantTreatmentPlanning;
                        dbitm.TelehealthSession = itm.TelehealthSession;
                        dbitm.ToxicologyResults = itm.ToxicologyResults;
                        dbitm.TreatmentGoal = itm.TreatmentGoal;
                        dbitm.TreatmentPlanChangesExplain = itm.TreatmentPlanChangesExplain;
                        dbitm.TreatmentPlanning = itm.TreatmentPlanning;
                        dbitm.TreatmentPlanningChanges = itm.TreatmentPlanningChanges;
                        dbitm.TreatmentServiceReview = itm.TreatmentServiceReview;
                        dbitm.TreatmentServiceReviewMHReferralsMade = itm.TreatmentServiceReviewMHReferralsMade;
                        dbitm.TreatmentServiceReviewMissAppointment = itm.TreatmentServiceReviewMissAppointment;
                        dbitm.TreatmentServiceReviewReferralsMade = itm.TreatmentServiceReviewReferralsMade;
                    }
                    else
                    {
                        rc.RowsIns += 1;
                        newItems.Add(itm);
                    }
                }
                db.SaveChanges();
                if (newItems.Count > 0)
                {
                    db.TblMNTreatmentServiceReviews.AddRange(newItems);
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
        public Models.RCodes SaveTakeHomeAgreementandDiversionControl(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblTakeHomeAgreementandDiversionControl> dblist = db.TblTakeHomeAgreementandDiversionControls.Where(x => x.SiteCode == sc).ToList();
                List<TblTakeHomeAgreementandDiversionControl> newItems = new List<TblTakeHomeAgreementandDiversionControl>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblTakeHomeAgreementandDiversionControl itm = new TblTakeHomeAgreementandDiversionControl();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;
                                itm.LastModAt = runat;
                                itm.RowState = 1;
                                break;
                            case "id":
                                itm.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                    if (itm.IsDeleted == true) { itm.RowState = 0; }
                                }
                                break;
                            case "medicaidid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.MedicaidID = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patient1":
                            case "patients1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Patients1 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patient2":
                            case "patients2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Patients2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignature":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PatientSignature = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignatureby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PatientSignatureBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignature":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignature = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignatureby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignatureBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Version = (r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    TblTakeHomeAgreementandDiversionControl dbitm = dblist.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm != null)
                    {
                        rc.RowsUpd += 1;
                        dbitm.ClientId = itm.ClientId;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.LastModAt = itm.LastModAt;
                        dbitm.MedicaidID = itm.MedicaidID;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedOn = itm.ModifiedOn;
                        dbitm.Patients1 = itm.Patients1;
                        dbitm.Patients2 = itm.Patients2;
                        dbitm.PatientSignature = itm.PatientSignature;
                        dbitm.PatientSignatureBy = itm.PatientSignatureBy;
                        dbitm.PatientSignatureDate = itm.PatientSignatureDate;
                        dbitm.PreAdmissionId = itm.PreAdmissionId;
                        dbitm.RowState = itm.RowState;
                        dbitm.StaffSignature = itm.StaffSignature;
                        dbitm.StaffSignatureBy = itm.StaffSignatureBy;
                        dbitm.StaffSignatureDate = itm.StaffSignatureDate;
                        dbitm.Version = itm.Version;
                    }
                    else
                    {
                        rc.RowsIns += 1;
                        newItems.Add(itm);
                    }
                }
                db.SaveChanges();
                if (newItems.Count > 0)
                {
                    db.TblTakeHomeAgreementandDiversionControls.AddRange(newItems);
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
        public Models.RCodes SaveDataForms(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblDataForms> dbList = db.TblDataForms.Where(x => x.SiteCode == sc).ToList();
                List<TblDataForms> newItems = new List<TblDataForms>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblDataForms itm = new TblDataForms();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;
                                break;
                            case "id":
                                itm.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dsid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.dsID = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "enrollmentdate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.EnrollmentDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "enrollmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.EnrollmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "formname":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.FormName = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "formurl":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.FormURL = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lastupdatedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.LastUpdatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "lastupdatedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.LastUpdatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PatientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "program":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Program = (r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    TblDataForms dbitm = dbList.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm != null)
                    {
                        rc.RowsUpd += 1;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.dsID = itm.dsID;
                        dbitm.EnrollmentDate = itm.EnrollmentDate;
                        dbitm.EnrollmentId = itm.EnrollmentId;
                        dbitm.FormName = itm.FormName;
                        dbitm.FormURL = itm.FormURL;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.LastUpdatedBy = itm.LastUpdatedBy;
                        dbitm.LastUpdatedOn = itm.LastUpdatedOn;
                        dbitm.PatientId = itm.PatientId;
                        dbitm.Program = itm.Program;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        newItems.Add(itm);
                    }
                }
                db.SaveChanges();
                if (newItems.Count > 0)
                {
                    db.TblDataForms.AddRange(newItems);
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
        public Models.RCodes SaveTakeHomeRiskAssessment(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblTakeHomeRiskAssessment> dbList = db.TblTakeHomeRiskAssessments.Where(x => x.SiteCode == sc).ToList();
                List<TblTakeHomeRiskAssessment> newItems = new List<TblTakeHomeRiskAssessment>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblTakeHomeRiskAssessment itm = new TblTakeHomeRiskAssessment();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;

                                break;
                            case "id":
                                itm.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lastdoseincrease":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.LastDoseIncrease = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "likelihoodofusingmedication":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.LikelihoodOfUsingMedication = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "logisticalbarriers":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.LogisticalBarriers = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "safeguardingmedication":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.SafeguardingMedication = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignature":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignature = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignatureby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignatureBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "takehomedoseunsafe":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TakeHomeDosesUnsafe = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "totalscore":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.TotalScore = (r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    TblTakeHomeRiskAssessment dbitm = dbList.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm != null)
                    {
                        rc.RowsUpd += 1;
                        dbitm.ClientId = itm.ClientId;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.LastDoseIncrease = itm.LastDoseIncrease;
                        dbitm.LikelihoodOfUsingMedication = itm.LikelihoodOfUsingMedication;
                        dbitm.LogisticalBarriers = itm.LogisticalBarriers;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedOn = itm.ModifiedOn;
                        dbitm.PreAdmissionId = itm.PreAdmissionId;
                        dbitm.SafeguardingMedication = itm.SafeguardingMedication;
                        dbitm.StaffSignature = itm.StaffSignature;
                        dbitm.StaffSignatureBy = itm.StaffSignatureBy;
                        dbitm.StaffSignatureDate = itm.StaffSignatureDate;
                        dbitm.TakeHomeDosesUnsafe = itm.TakeHomeDosesUnsafe;
                        dbitm.TotalScore = itm.TotalScore;
                    }
                    else
                    {
                        rc.RowsIns += 1;
                        newItems.Add(itm);
                    }
                }
                db.SaveChanges();
                if (newItems.Count > 0)
                {
                    db.TblTakeHomeRiskAssessments.AddRange(newItems);
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
        public Models.RCodes SaveSMSTextConsentForm(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblSMSTextConsentForm> tblSMs = db.TblSMSTextConsentForms.Where(x => x.SiteCode == sc).ToList();
                List<TblSMSTextConsentForm> newItems = new List<TblSMSTextConsentForm>();
                foreach (DataRow r in tbl.Rows)
                {
                    TblSMSTextConsentForm itm = new TblSMSTextConsentForm();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;

                                break;
                            case "id":
                                itm.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedBy = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clientname":
                                itm.ClientName = r[c.ColumnName].ToString();
                                break;
                            case "phoneno":
                                itm.PhoneNo = r[c.ColumnName].ToString();
                                break;
                            case "donotagreetoreceive":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DoNotAgreetoReceive = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignature":
                                itm.PatientSignature = r[c.ColumnName].ToString();
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignatureby":
                                itm.PatientSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "version":
                                itm.Version = r[c.ColumnName].ToString();
                                break;
                            case "permission":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Permission = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    TblSMSTextConsentForm dbitm = tblSMs.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.Id == itm.Id);
                    if (dbitm != null)
                    {
                        dbitm.ClientId = itm.ClientId;
                        dbitm.ClientName = itm.ClientName;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.Version = itm.Version;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.DoNotAgreetoReceive = itm.DoNotAgreetoReceive;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedOn = itm.ModifiedOn;
                        dbitm.PatientSignature = itm.PatientSignature;
                        dbitm.PatientSignatureBy = itm.PatientSignatureBy;
                        dbitm.PatientSignatureDate = itm.PatientSignatureDate;
                        dbitm.Permission = itm.Permission;
                        dbitm.PhoneNo = itm.PhoneNo;
                        dbitm.PreAdmissionId = itm.PreAdmissionId;
                        rc.RowsUpd += 1;
                    }
                    else
                    {
                        newItems.Add(itm);
                        rc.RowsIns += 1;
                    }
                }
                db.SaveChanges();
                if (newItems.Count > 0)
                {
                    db.TblSMSTextConsentForms.AddRange(newItems);
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
        public Models.RCodes SaveSFPatientPreAdmission(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                List<TblSFPatientPreAdmission> NewItems = new List<TblSFPatientPreAdmission>();
                List<TblSFPatientPreAdmission> dbList = db.TblSFPatientPreAdmissions.Where(x => x.SiteCode == sc).ToList();

                foreach (DataRow r in tbl.Rows)
                {
                    TblSFPatientPreAdmission itm = new TblSFPatientPreAdmission();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        //Console.WriteLine(c.ColumnName.ToString());
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                itm.SiteCode = sc;
                                break;
                            case "id":
                                itm.ID = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "accomodationneeded":
                                itm.AccomodationNeeded = r[c.ColumnName].ToString();
                                break;
                            case "acknowledgeclientsignature":
                                itm.AcknowledgeClientSignature = r[c.ColumnName].ToString();
                                break;
                            case "acknowledgeclientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AcknowledgeClientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "acknowledgeclientsignaturedatep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AcknowledgeClientSignatureDateP = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "acknowledgeclientsignaturep":
                                itm.AcknowledgeClientSignatureP = r[c.ColumnName].ToString();
                                break;
                            case "acknowledgewitnesssignature":
                                itm.AcknowledgeWitnessSignature = r[c.ColumnName].ToString();
                                break;
                            case "acknowledgewitnesssignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AcknowledgeWitnessSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "acknowledgewitnesssignaturedatep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AcknowledgeWitnessSignatureDateP = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "acknowledgewitnesssignaturep":
                                itm.AcknowledgeWitnessSignatureP = r[c.ColumnName].ToString();
                                break;
                            case "active":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Active = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "activep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ActiveP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "additionalcomments":
                                itm.AdditionalComments = r[c.ColumnName].ToString();
                                break;
                            case "additionaldoc":
                                itm.AdditionalDoc = r[c.ColumnName].ToString();
                                break;
                            case "addressma":
                                itm.AddressMA = r[c.ColumnName].ToString();
                                break;
                            case "addressq8":
                                itm.AddressQ8 = r[c.ColumnName].ToString();
                                break;
                            case "alcoholamount":
                                itm.AlcoholAmount = r[c.ColumnName].ToString();
                                break;
                            case "alcohollastdrink":
                                itm.AlcoholLastDrink = r[c.ColumnName].ToString();
                                break;
                            case "answerallquestion":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AnswerAllQuestion = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "answerallquestiontxt":
                                itm.AnswerAllQuestionTxt = r[c.ColumnName].ToString();
                                break;
                            case "answerrangeabove":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AnswerRangeAbove = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "answerrangenine":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AnswerRangeNine = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "answerrangesix":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AnswerRangeSix = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "answerrangethree":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AnswerRangeThree = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "anychangestoinformation":
                                itm.AnyChangestoInformation = r[c.ColumnName].ToString();
                                break;
                            case "applicantname":
                                itm.ApplicantName = r[c.ColumnName].ToString();
                                break;
                            case "appointmentdate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AppointmentDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "appointmenttime":
                                itm.AppointmentTime = r[c.ColumnName].ToString();
                                break;
                            case "areyoucurrentlypregnant":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.AreYouCurrentlyPregnant = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "biopsychosocialassessment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Biopsychosocialassessment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "biopsychosocialassessmentp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.BiopsychosocialassessmentP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "biopsychosocialtext":
                                itm.BiopsychosocialText = r[c.ColumnName].ToString();
                                break;
                            case "biopsychosocialtextp":
                                itm.BiopsychosocialTextP = r[c.ColumnName].ToString();
                                break;
                            case "bringidproof":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.BringIDProof = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "bringinsurancecard":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.BringInsuranceCard = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxrevokeroiq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.chkBoxRevokeROIQ2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxrevokeroiq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.chkBoxRevokeROIQ8 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentlypregnant":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.currentlypregnant = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq2_1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ2_1 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq2_2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ2_2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq2_3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ2_3 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq2_4":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ2_4 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq8_1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ8_1 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq8_2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ8_2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq8_3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ8_3 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxprotectedhealthinformationq8_4":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxProtectedHealthInformationQ8_4 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_1 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_10":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_10 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_11":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_11 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_12":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_12 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_13":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_13 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_3 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_4":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_4 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_5":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_5 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_6":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_6 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_7":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_7 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_8 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq2_9":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ2_9 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_1 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_10":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_10 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_11":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_11 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_12":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_12 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_13":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_13 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_2 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_3 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_4":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_4 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_5":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_5 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_6":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_6 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_7":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_7 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_8 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chkboxsudinformationq8_9":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ChkboxSUDInformationQ8_9 = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "chronicmedicalcondition":
                                itm.ChronicMedicalCondition = (r[c.ColumnName].ToString());
                                break;
                            case "clientaddress":
                                itm.ClientAddress = r[c.ColumnName].ToString();
                                break;
                            case "clientdetails":
                                itm.ClientDetails = r[c.ColumnName].ToString();
                                break;
                            case "clientnamep":
                                itm.ClientNameP = r[c.ColumnName].ToString();
                                break;
                            case "clinicinfo":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ClinicInfo = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "comment":
                                itm.Comment = r[c.ColumnName].ToString();
                                break;
                            case "comments":
                                itm.Comments = r[c.ColumnName].ToString();
                                break;
                            case "created":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Created = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                itm.CreatedBy = r[c.ColumnName].ToString();
                                break;
                            case "createdbyp":
                                itm.CreatedByP = r[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CreatedP = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentlymedicatedtreatmentother":
                                itm.CurrentlyMedicatedTreatmentOther = r[c.ColumnName].ToString();
                                break;
                            case "currentlyusingopoiddrug":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrentlyUsingOpoidDrug = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentopiateprogramdose":
                                itm.CurrentOpiateProgramDose = (r[c.ColumnName].ToString());
                                break;
                            case "currentopiateprogramfrom":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrentOpiateProgramFrom = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentopiatprogramlocation":
                                itm.CurrentOpiateProgramLocation = r[c.ColumnName].ToString();
                                break;
                            case "currentopiateprogramnoofmonths":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrentOpiateProgramNoOfMonths = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentopiateprogramnoofyears":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrentOpiateProgramNoOfYears = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentopiateprogramto":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrentOpiateProgramTo = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "currentopiatewhatprogram":
                                itm.CurrentOpiateWhatProgram = r[c.ColumnName].ToString();
                                break;
                            case "currentlyrecevingtreatmentforcondition":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.CurrntlyRecevingTreatmentForCondition = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dailytime":
                                itm.DailyTime = r[c.ColumnName].ToString();
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "date":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Date = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dateofrelease":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DateofRelease = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dateofstaffsignature":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DateOfStaffSignature = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "datep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DateP = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "datepreviouslypatientmedicationassisted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.datepreviouslypatientmedicationassisted = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "datetimelastopioid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.datetimelastopioid = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "daysafterdischarge":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.daysafterdischarge = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "daysafterdischargep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.daysafterdischargeP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ddlprimarysubstance":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DDLPrimarySubstance = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "deniedduetocapacity":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DeniedDuetoCapacity = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "describeother":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.describeother = r[c.ColumnName].ToString();
                                }
                                break;
                            case "describeother1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.describeother1 = r[c.ColumnName].ToString();
                                }
                                break;
                            case "describeother1p":
                                itm.describeother1P = r[c.ColumnName].ToString();
                                break;
                            case "describeother2":
                                itm.describeother2 = r[c.ColumnName].ToString();
                                break;
                            case "describeotherp2":
                                itm.describeother2P = r[c.ColumnName].ToString();
                                break;
                            case "describeotherp":
                                itm.describeotherP = r[c.ColumnName].ToString();
                                break;
                            case "diagnosis":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.Diagnosis = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "diagnosisp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DiagnosisP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "diagnosistext":
                                itm.DiagnosisText = r[c.ColumnName].ToString();
                                break;
                            case "diagnosistextp":
                                itm.DiagnosisTextP = r[c.ColumnName].ToString();
                                break;
                            case "dischargereason":
                                itm.DischargeReason = r[c.ColumnName].ToString();
                                break;
                            case "dischargereasonstext":
                                itm.DischargeReasonsText = r[c.ColumnName].ToString();
                                break;
                            case "dischargereasonstextp":
                                itm.DischargeReasonsTextP = r[c.ColumnName].ToString();
                                break;
                            case "dischargesummary":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DischargeSummary = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dischargesummaryp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DischargeSummaryP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dischargesummarytext":
                                itm.DischargeSummaryText = r[c.ColumnName].ToString();
                                break;
                            case "dischargesummarytextp":
                                itm.DischargeSummaryTextP = r[c.ColumnName].ToString();
                                break;
                            case "doctorsignature":
                                itm.DoctorSignature = r[c.ColumnName].ToString();
                                break;
                            case "doctorsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DoctorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "doyouhaveinsuranceno":
                                itm.DoYouHaveInsuranceNo = r[c.ColumnName].ToString();
                                break;
                            case "dropdownnumberofchildren":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.dropdownNumberOfChildren = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dropdownpresentlyinpainscale1to10":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.dropdownPresentlyInPainScale1to10 = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "drugadministrationtypeid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DrugAdministrationTypeID = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "drugapplicanttime":
                                itm.DrugApplicantTime = r[c.ColumnName].ToString();
                                break;
                            case "drugchoiceid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.DrugChoiceID = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "drugdaily":
                                itm.DrugDaily = r[c.ColumnName].ToString();
                                break;
                            case "druglastused":
                                itm.DrugLastUsed = r[c.ColumnName].ToString();
                                break;
                            case "drugofchoiceadministered":
                                itm.DrugofchoiceAdministered = r[c.ColumnName].ToString();
                                break;
                            case "drugtaken":
                                itm.DrugTaken = r[c.ColumnName].ToString();
                                break;
                            case "drugusing":
                                itm.DrugUsing = r[c.ColumnName].ToString();
                                break;
                            case "employednotxt":
                                itm.EmployedNoTxt = r[c.ColumnName].ToString();
                                break;
                            case "employedyestxt":
                                itm.EmployedYesTxt = r[c.ColumnName].ToString();
                                break;
                            case "enrollmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.EnrollmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "exclusionsma":
                                itm.exclusionsMA = r[c.ColumnName].ToString();
                                break;
                            case "exclusionsq8":
                                itm.exclusionsQ8 = r[c.ColumnName].ToString();
                                break;
                            case "faxma":
                                itm.FAXMA = r[c.ColumnName].ToString();
                                break;
                            case "faxq8":
                                itm.FAXQ8 = r[c.ColumnName].ToString();
                                break;
                            case "followupscreening":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.FollowupScreening = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "generaltreatingproviderrelationshipma":
                                itm.genralTreatingproviderrelationshipMA = r[c.ColumnName].ToString();
                                break;
                            case "generaltreatingproviderrelationshipq8":
                                itm.genralTreatingproviderrelationshipQ8 = r[c.ColumnName].ToString();
                                break;
                            case "gyndoctorprovidername":
                                itm.GynDoctorProviderName = r[c.ColumnName].ToString();
                                break;
                            case "gyndoctorproviderphone":
                                itm.GynDoctorProviderPhone = r[c.ColumnName].ToString();
                                break;
                            case "hadanyhomicidalthoughts":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.HadAnyHomicidalThoughts = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hadanyhomicidalthoughtstxt":
                                itm.HadAnyHomicidalThoughtsTxt = r[c.ColumnName].ToString();
                                break;
                            case "hadanysuicidalthoughts":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.HadAnySuicidalThoughts = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hadanysuicidalthoughtstxt":
                                itm.HadAnySuicidalThoughtsTxt = r[c.ColumnName].ToString();
                                break;
                            case "hasgyndoctor":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.HasGynDoctor = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "haveanyurgentneeds":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.HaveAnyUrgentNeeds = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "havebeencarcerated":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.HaveBeenCarcerated = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "haveyouusedopoiddrug":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.HaveYouUsedOpoidDrug = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hcrcliniclocationma":
                                itm.HCRCClinicLocationMA = (r[c.ColumnName].ToString());
                                break;
                            case "hcrcliniclocationq8":
                                itm.HCRCClinicLocationQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "illicitsubstanceother":
                                itm.IllicitSubstanceOther = r[c.ColumnName].ToString();
                                break;
                            case "illicitsubstanceothermgperday":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IllicitSubstanceOtherMgPerDay = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "immediateassessment":
                                itm.ImmediateAssessment = (r[c.ColumnName].ToString());
                                break;
                            case "immediateassessment911":
                                itm.ImmediateAssessment911 = (r[c.ColumnName].ToString());
                                break;
                            case "incarcenated":
                                itm.Incarcenated = (r[c.ColumnName].ToString());
                                break;
                            case "informationtoobtained":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtained = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedfax":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedFax = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedfaxp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedFaxP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedP = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedverbal":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedVerbal = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedverbalp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedVerbalP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedwritten":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedWritten = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "informationtoobtainedwrittenp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InformationToObtainedWrittenP = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "initialscreeningparticipationtext":
                                itm.InitialSceeningParticipationText = (r[c.ColumnName].ToString());
                                break;
                            case "initialscreeningparticitiontextp":
                                itm.InitialSceeningParticipationTextP = (r[c.ColumnName].ToString());
                                break;
                            case "initialscreening":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.InitialScreening = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "initialscreeningsummarytext":
                                itm.InitialScreeningSummaryText = (r[c.ColumnName].ToString());
                                break;
                            case "initialscreeningsummarytextp":
                                itm.InitialScreeningSummaryTextP = (r[c.ColumnName].ToString());
                                break;
                            case "insurancedescription":
                                itm.InsuranceDescription = (r[c.ColumnName].ToString());
                                break;
                            case "insurancetype":
                                itm.InsuranceType = (r[c.ColumnName].ToString());
                                break;
                            case "intakeappointmentscheduleddatetime":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IntakeAppointmentScheduledDateTime = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "intakeprogram":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IntakeProgram = (r[c.ColumnName].ToString());
                                }
                                break;
                            case "intakeprogramdate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IntakeProgramDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "interestedtransferringetsyes":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.interestedtransferringetsyes = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isabletoattendtreatmentcenterdaily":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAbleToAttendTreatementCenterDaily = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isabletopayintakeandweeklyfee":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAbleToPayIntakeAndWeeklyFee = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isaddictiontounstableneeding":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAddictionToUnstableNeeding = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isallergies":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAllergies = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isampm":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAmPm = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isanylegalprescriptionforpain":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAnyLegalPrescriptionForPain = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isanyongoingmedicalcondition":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAnyOngoingMedicalCondition = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isanyprescriptionforpain":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAnyPrescriptionForPain = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isapplicantpregant":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsApplicantPregnant = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isattemptedsuicide":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsAttemptedSuicide = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isbehaviorallyunstabldangerous":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsBehaviorallyunstabldangerous = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isclinicaddress":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsClinicAddress = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "iscsuatfullcapacity":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsCSUAtFullCapacity = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "iscurrentlyinopiateprogram":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsCurrentlyInOpiateProgram = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "iscurrentlypatientpainmanagementmethadone":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.iscurrentlypatientpainmanagementmethadone = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "iscurrentlypregnant":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsCurrentlyPregnant = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdetox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDetox = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdidnotmeetmedicalnecessity":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDidnotMeetMedicalNecessity = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdoyouhavepicture":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.isdoyouhavepicture = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdrinkingalcohol":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsDrinkingAlcohol = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "iseligibleforfurtherassessment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsEligibleForFurtherAssessment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isemployed":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsEmployed = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isevscompleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsEVSCompleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ishadalcoholinlast12hours":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ishadalcoholinlast12hours = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ishaveyouhospitalizedinlast30daysyes":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ishaveyouhospitalizedinlast30daysyes = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ishavinglegalprescription":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsHavingLegalPrescription = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ishavingplanforhowtocommitsuicide":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsHavingPlanForHowToCommitSuicide = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ishomicidalthoughtwithin72hours":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsHomicidalThoughtWithin72Hours = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isimpairment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsImpairment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isinsurance":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsInsurance = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isinsuranceavailable":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsInsuranceAvailable = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isinsurancecard":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsInsuranceCard = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isintakeappointmentscheduled":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsIntakeAppointmentScheduled = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ismaintenance":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsMaintenance = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ismedicalemergency":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsMedicalEmergency = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ismedicalproblemsneedingstabilization":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsMedicalProblemsNeedingStabilization = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ismentalhealthtreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.isMentalHealthTreatment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isother":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsOther = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isoverthecountermedications":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.isOverTheCounterMedications = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispackets":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPackets = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isparticipitatinginotheropiodprogram":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsParticipitatingInOtherOpioidProgram = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispasttreatmenthistory":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPastTreatmentHistory = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispatientadmitted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPatientAdmitted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispatientatpainmanagementclinic":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPatientAtPainManagementClinic = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isphysicalhealthunstable":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPhysicalHealthUnstable = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispictureid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPictureId = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isplanforhowtohurtsomeoneelse":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPlanForHowToHurtSomeElse = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isplansendtime":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPlanSendTime = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isplanto[aytreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.isplantopaytreatment = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispregnant":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ispregnant = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispresentedatcloseofday":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPresentedAtCloseOfDay = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispresentedwithoutidproof":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPresentedWithoutIDProof = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispreviouslybeen[atientmedicationassisted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.ispreviouslybeenpatientmedicationassisted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispreviousmentalhealthtreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPreviousMentalHealthTreatment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isprioretspatientyes":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.isprioretspatientyes = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ispsychiatricproblemneedinginplatienttreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsPsychiatricProblemNeedingInpatientTreatment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isrecentlyreleasedfrompenal":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsRecentlyReleasedFromPenal = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isreferraloffered":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsReferralOffered = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isreliabletransportation":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsReliableTransportation = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isrequestedadmissiondrunavalable":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    itm.IsRequestedAdmissionDrUnavailable = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isrequirechildcare":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsRequireChildCare = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "isservicedeclined":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsServiceDeclined = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "isspecialaccommodationrequired":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsSpecialAccommodationRequired = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "issuicidalthoughtwithin72hours":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsSuicidalThoughtWithin72Hours = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "issymptomsofopiodwithdrawal":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.issymptomsofopioidwithdrawal = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "istakingpresscriptionmedication":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsTakingPrescriptionMedication = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "istheresubstanceusehistory":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.istheresubstanceusehistory = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "isthoughtsofkilling":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsThoughtsOfKilling = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "istransfer":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsTransfer = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "istriagedtomedicaldetoxfacility":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsTriagedtoMedicalDetoxFacility = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "isunabletopay":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsUnableToPay = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "isunabletoprovideevidenceofaddicition":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.IsUnableToProvideEvidenceofAddiction = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "lastprescription":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.lastprescription = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "lastupdatedby":
                                itm.LastUpdatedBy = (r[c.ColumnName].ToString());
                                break;
                            case "lastupdateon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.LastUpdateOn = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "lastusedtime":
                                itm.LastUsedTime = (r[c.ColumnName].ToString());
                                break;
                            case "legalprescription1":
                                itm.LegalPrescription1 = (r[c.ColumnName].ToString());
                                break;
                            case "legalprescription2":
                                itm.LegalPrescription2 = (r[c.ColumnName].ToString());
                                break;
                            case "legalprescription3":
                                itm.LegalPrescription3 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalcondition1":
                                itm.MedicalCondition1 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalcondition2":
                                itm.MedicalCondition2 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalcondition3":
                                itm.MedicalCondition3 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalconditionsprovidername1":
                                itm.MedicalConditionsProviderName1 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalconditionsprovidername2":
                                itm.MedicalConditionsProviderName2 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalconitionsproviderphone1":
                                itm.MedicalConditionsProviderPhone1 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalconditionsproviderphone2":
                                itm.MedicalConditionsProviderPhone2 = (r[c.ColumnName].ToString());
                                break;
                            case "medicalemergencydescribe":
                                itm.MedicalEmergencyDescribe = (r[c.ColumnName].ToString());
                                break;
                            case "medicalinformation":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Medicalinformation = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "medicalinformationp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.MedicalinformationP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "medicaltext":
                                itm.MedicalText = (r[c.ColumnName].ToString());
                                break;
                            case "medicaltextp":
                                itm.MedicalTextP = (r[c.ColumnName].ToString());
                                break;
                            case "medicatedassistedtreatmentprogram":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.MedicatedAssistedTreatmentProgram = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "medicationconditions":
                                itm.MedicationConditions = (r[c.ColumnName].ToString());
                                break;
                            case "medicationname1":
                                itm.MedicationName1 = (r[c.ColumnName].ToString());
                                break;
                            case "medicationname2":
                                itm.MedicationName2 = (r[c.ColumnName].ToString());
                                break;
                            case "medicationname3":
                                itm.MedicationName3 = (r[c.ColumnName].ToString());
                                break;
                            case "medicationname4":
                                itm.MedicationName4 = (r[c.ColumnName].ToString());
                                break;
                            case "medicationname5":
                                itm.MedicationName5 = (r[c.ColumnName].ToString());
                                break;
                            case "mentalhealthfromdate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.MentalHealthFromDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "mentalhealthtodate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.MentalHealthToDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "mentalhealthtreatmentdescrption":
                                itm.MentalHealthTreatmentDescription = (r[c.ColumnName].ToString());
                                break;
                            case "mentalhealthtreatmentwhat":
                                itm.MentalHealthTreatmentWhat = (r[c.ColumnName].ToString());
                                break;
                            case "mentalhealthtreatmentwhen":
                                itm.MentalHealthTreatmentWhen = (r[c.ColumnName].ToString());
                                break;
                            case "mentalhealthtreatmentwhere":
                                itm.MentalHealthTreatmentWhere = (r[c.ColumnName].ToString());
                                break;
                            case "modified":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Modified = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "modifiedby":
                                itm.ModifiedBy = (r[c.ColumnName].ToString());
                                break;
                            case "modifiedbyp":
                                itm.ModifiedByP = (r[c.ColumnName].ToString());
                                break;
                            case "modifiedp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ModifiedP = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "na":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.NA = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "namedentitesidq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.NamedEntitesIDQ2 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "namedentitesdq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.NamedEntitesIDQ8 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "namedentityma":
                                itm.namedEntityMA = (r[c.ColumnName].ToString());
                                break;
                            case "namedentityq8":
                                itm.namedEntityQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "namedentitywithouttreatmentma":
                                itm.namedEntityWithoutTreatmentMA = (r[c.ColumnName].ToString());
                                break;
                            case "namedentitywithouttreatmentq8":
                                itm.namedEntityWithoutTreatmentQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "namedindividualma":
                                itm.namedIndividualMA = (r[c.ColumnName].ToString());
                                break;
                            case "namedindividualpaticipantma":
                                itm.namedIndividualPaticipantMA = (r[c.ColumnName].ToString());
                                break;
                            case "namedindividualpaticipantq8":
                                itm.namedIndividualPaticipantQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "namedindividualq8":
                                itm.namedIndividualQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "namedthridpartyma":
                                itm.namedThirdPartyMA = (r[c.ColumnName].ToString());
                                break;
                            case "namedthridpartyq8":
                                itm.namedThirdPartyQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "nameofwitness":
                                itm.nameOfWitness = (r[c.ColumnName].ToString());
                                break;
                            case "namwofwitnessq8":
                                itm.nameOfWitnessQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "needreferal":
                                itm.NeedReferal = (r[c.ColumnName].ToString());
                                break;
                            case "observationcomments":
                                itm.ObservationComments = (r[c.ColumnName].ToString());
                                break;
                            case "officeusetime":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.OfficeUseTime = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "officeusewhy":
                                itm.OfficeUseWhy = (r[c.ColumnName].ToString());
                                break;
                            case "ongoingmedicalconditionswha":
                                itm.OngoingMedicalConditionsWha = (r[c.ColumnName].ToString());
                                break;
                            case "opiatesusagenoofmonths":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.OpiatesUsageNoOfMonths = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "opiatesusagenoofyears":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.OpiatesUsageNoOfYears = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "other":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.other = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "other2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.other2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "other2p":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.other2P = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "othercode":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.OtherCode = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "otherdescription":
                                itm.OtherDescription = (r[c.ColumnName].ToString());
                                break;
                            case "otherp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.otherP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "othertext":
                                itm.OtherText = (r[c.ColumnName].ToString());
                                break;
                            case "othertextp":
                                itm.OtherTextP = (r[c.ColumnName].ToString());
                                break;
                            case "overthecountermedicationstext1":
                                itm.OverTheCounterMedicationsText1 = (r[c.ColumnName].ToString());
                                break;
                            case "packettypeid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PacketTypeID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "packetversion":
                                itm.PacketVersion = (r[c.ColumnName].ToString());
                                break;
                            case "painmanagementclinic":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PainManagementClinic = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "painmanagementclinicfromdate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PainManagementClinicFromDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "painmanagementcliniclocation":
                                itm.PainManagementClinicLocation = (r[c.ColumnName].ToString());
                                break;
                            case "painmanagementclinictodate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PainManagementClinicToDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "parentpreadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ParentPreAdmissionId = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "participationinitialscreeningprocess":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Participationininitialsceeningprocess = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "participationinitialscreeningprossp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ParticipationininitialsceeningprocessP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "patientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PatientID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "patientsignature":
                                itm.PatientSignature = (r[c.ColumnName].ToString());
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "personforintakeprocess":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PersonForIntakeProcess = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "phone1":
                                itm.Phone1 = (r[c.ColumnName].ToString());
                                break;
                            case "phone2":
                                itm.Phone2 = (r[c.ColumnName].ToString());
                                break;
                            case "phone3":
                                itm.Phone3 = (r[c.ColumnName].ToString());
                                break;
                            case "phone4":
                                itm.Phone4 = (r[c.ColumnName].ToString());
                                break;
                            case "phone5":
                                itm.Phone5 = (r[c.ColumnName].ToString());
                                break;
                            case "phonema":
                                itm.PhoneMA = (r[c.ColumnName].ToString());
                                break;
                            case "phoneq8":
                                itm.PhoneQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "planofsuicide":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PlanOfSuicide = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "planonspendingtimeatclinic":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PlanOnSpendingTimeAtClinic = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "preadd_address":
                                itm.PreAdd_Address = (r[c.ColumnName].ToString());
                                break;
                            case "preadmissiondate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PreAdmissionDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "prescriber1":
                                itm.Prescriber1 = (r[c.ColumnName].ToString());
                                break;
                            case "prescriber2":
                                itm.Prescriber2 = (r[c.ColumnName].ToString());
                                break;
                            case "prescriber3":
                                itm.Prescriber3 = (r[c.ColumnName].ToString());
                                break;
                            case "prescriber4":
                                itm.Prescriber4 = (r[c.ColumnName].ToString());
                                break;
                            case "prescriber5":
                                itm.Prescriber5 = (r[c.ColumnName].ToString());
                                break;
                            case "primaryreferralsource":
                                itm.PrimaryReferralSource = (r[c.ColumnName].ToString());
                                break;
                            case "primaryreferralsourcenote":
                                itm.PrimaryReferralSourceNote = (r[c.ColumnName].ToString());
                                break;
                            case "prioretspatient":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.prioretspatient = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "programid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ProgramID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "progressrecoverygoalstext":
                                itm.ProgressRecoveryGoalsText = (r[c.ColumnName].ToString());
                                break;
                            case "progressrecoverygoaltextp":
                                itm.ProgressRecoveryGoalsTextP = (r[c.ColumnName].ToString());
                                break;
                            case "progresstowardsrecoverygoals":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Progresstowardsrecoverygoals = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "progresstowardsrecoverygoalp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ProgresstowardsrecoverygoalsP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "proofofopiatedependence":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ProofOfOpiateDependence = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "protectedhealthinformationma":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.protectedHealthInformationMA = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "protectedhealthinformationq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.protectedHealthInformationQ8 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "purposeofdisclosurema":
                                itm.purposeOfDisclosureMA = (r[c.ColumnName].ToString());
                                break;
                            case "purposeofdiscloureq8":
                                itm.purposeOfDisclosureQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "purposeofobtainingrelease":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PurposeOfObtainingRelease = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "purposeofobtainingreleasep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.PurposeOfObtainingReleaseP = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radiogeneraldesgnationq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioGeneralDesignationQ2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radiogeneraldesignationq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioGeneralDesignationQ8 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionameentityproviderelationwithmeq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedEntityProvideRelationWithMeQ2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionameentityproviderelationwithmeq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedEntityProvideRelationWithMeQ8 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedentitywithoutproviderelationwithmeq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedEntityWithoutProvideRelationWithMeQ2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedentitywithoutproviderelationwithmeq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedEntityWithoutProvideRelationWithMeQ8 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualparticipantq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualParticipantQ2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualparticipantq2select":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualParticipantQ2select = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualparticipantq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualParticipantQ8 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualparticipantq8select":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualParticipantQ8select = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualQ2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualq2select":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualQ2select = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualQ8 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedindividualq8select":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedIndividualQ8select = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedthirdpartypayerq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedthirdpartyPayerQ2 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "radionamedthirdpartypayerq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.radioNamedthirdpartyPayerQ8 = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "reason":
                                itm.Reason = (r[c.ColumnName].ToString());
                                break;
                            case "reasonfordenial":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReasonForDenial = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "reasonforreferral":
                                itm.ReasonforReferral = (r[c.ColumnName].ToString());
                                break;
                            case "reasonseekingtreatment":
                                itm.ReasonSeekingTreatment = (r[c.ColumnName].ToString());
                                break;
                            case "reasonsfordischarge":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Reasonsfordischarge = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "reasonsfordischargep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReasonsfordischargeP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "receivedtreatmentforaddicition":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReceivedTreatmentForAddiction = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "receiveinganytreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReceiveingAnyTreatment = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "recentheadtrauma":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RecentHeadTrauma = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "recoveryplansorgoals":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Recoveryplansorgoals = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "recoveryplansorgoalsp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RecoveryplansorgoalsP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "recoveryplanstext":
                                itm.RecoveryPlansText = (r[c.ColumnName].ToString());
                                break;
                            case "recoveryplanstextp":
                                itm.RecoveryPlansTextP = (r[c.ColumnName].ToString());
                                break;
                            case "referedto":
                                itm.ReferedTo = (r[c.ColumnName].ToString());
                                break;
                            case "referralofferedlist":
                                itm.ReferralOfferedList = (r[c.ColumnName].ToString());
                                break;
                            case "referralrecommendations":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Referralrecommendations = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "referralrecommendationsp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReferralrecommendationsP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "referralrecommendationstext":
                                itm.ReferralRecommendationsText = (r[c.ColumnName].ToString());
                                break;
                            case "referralrecommendationstextp":
                                itm.ReferralRecommendationsTextP = (r[c.ColumnName].ToString());
                                break;
                            case "referralsourceid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReferralSourceID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "referredby":
                                itm.ReferredBy = (r[c.ColumnName].ToString());
                                break;
                            case "registrationmodeid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RegistrationModeID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "rejectedreferraloffered":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RejectedReferralOffered = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "relapseepisodes":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Relapseepisodes = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "relapseepisodesp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RelapseepisodesP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "relapseepisodestext":
                                itm.RelapseEpisodesText = (r[c.ColumnName].ToString());
                                break;
                            case "relapseepisodestextp":
                                itm.RelapseEpisodesTextP = (r[c.ColumnName].ToString());
                                break;
                            case "releasedfrompenaldate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReleasedFromPenalDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "reliabletransportationtxt":
                                itm.ReliableTransportationTxt = (r[c.ColumnName].ToString());
                                break;
                            case "representativesrelationshiptopatientma":
                                itm.representativesRelationshiptoPatientMA = (r[c.ColumnName].ToString());
                                break;
                            case "representativesrelationshiptopatientq8":
                                itm.representativesRelationshiptoPatientQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "requestedadmissionamorpm":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RequestedAdmissionAMOrPM = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requestedadmissionday":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RequestedAdmissionDay = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requestadmissiondayfri":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.requestedAdmissionDayFri = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requestedadmissiondaymon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.requestedAdmissionDayMon = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requestedadmissiondaythu":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.requestedAdmissionDayThu = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requestedadmissiondaytue":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.requestedAdmissionDayTue = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requestedadmissiondaywed":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.requestedAdmissionDayWed = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requireassistivetechnologies":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RequireAssistiveTechnologies = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "requiretransportationservices":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RequireTransportationServices = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "reviewedresponsesfromcallcenter":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ReviewedResponsesFromCallCenter = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "revocationclientsignature":
                                itm.RevocationClientSignature = (r[c.ColumnName].ToString());
                                break;
                            case "revocationclientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RevocationClientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "revocationclientsignaturedatep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RevocationClientSignatureDateP = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "revocationclientsignaturep":
                                itm.RevocationClientSignatureP = (r[c.ColumnName].ToString());
                                break;
                            case "revocationwithnesssignature":
                                itm.RevocationWitnessSignature = (r[c.ColumnName].ToString());
                                break;
                            case "revocationwitnesssignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RevocationWitnessSignatureDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "revocationwitnesssignaturedatep":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RevocationWitnessSignatureDateP = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "revocationwitnesssignaturep":
                                itm.RevocationWitnessSignatureP = (r[c.ColumnName].ToString());
                                break;
                            case "rnpstaffsign":
                                itm.RNPStaffSign = (r[c.ColumnName].ToString());
                                break;
                            case "rnpstaffsigndate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.RNPStaffSignDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "sammsprogramid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SammsProgramID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "screenedby":
                                itm.ScreenedBy = (r[c.ColumnName].ToString());
                                break;
                            case "screenedbydate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.ScreenedByDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "secondaryreferralsource":
                                itm.SecondaryReferralSource = (r[c.ColumnName].ToString());
                                break;
                            case "seizureinpast7days":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SeizureInPast7Days = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "setupappointment":
                                itm.SetupAppointment = (r[c.ColumnName].ToString());
                                break;
                            case "signaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SignatureDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "signatureofconsenp":
                                itm.SignatureOfConsenP = (r[c.ColumnName].ToString());
                                break;
                            case "signatureofconsent":
                                itm.SignatureOfConsent = (r[c.ColumnName].ToString());
                                break;
                            case "signatureofconsentp":
                                itm.SignatureOfConsentP = (r[c.ColumnName].ToString());
                                break;
                            case "signaturernpstaff":
                                itm.SignatureRnpStaff = (r[c.ColumnName].ToString());
                                break;
                            case "staffathcrc":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.staffAtHCRC = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "staffathcrcq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.staffAtHCRCQ8 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "staffsigncredentials":
                                itm.StaffSignCredentials = (r[c.ColumnName].ToString());
                                break;
                            case "substanceabusewhat":
                                itm.SubstanceAbuseWhat = (r[c.ColumnName].ToString());
                                break;
                            case "substanceabusewhen":
                                itm.SubstanceAbuseWhen = (r[c.ColumnName].ToString());
                                break;
                            case "substanceabusewhere":
                                itm.SubstanceAbuseWhere = (r[c.ColumnName].ToString());
                                break;
                            case "substanceusefromdate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SubstanceUseFromDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "substanceusetodate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SubstanceUseToDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "sudinformationma":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SUDinformationMA = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "sudinformationotherq2":
                                itm.SUDInformationOtherQ2 = (r[c.ColumnName].ToString());
                                break;
                            case "sudinformationotherq8":
                                itm.SUDInformationOtherQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "sudinformationq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SUDinformationQ8 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "suicidalthoughtintentscale":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SuicidalThoughtIntentScale = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "suicidedetailstxt":
                                itm.SuicideDetailsTxt = (r[c.ColumnName].ToString());
                                break;
                            case "summaryofinitialscreeningprocess":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Summaryofinitialscreeningprocess = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "summaryofinitialscreeningprocessp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SummaryofinitialscreeningprocessP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "supervisorsignature":
                                itm.SupervisorSignature = (r[c.ColumnName].ToString());
                                break;
                            case "supervisorsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.SupervisorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "takentime":
                                itm.TakenTime = (r[c.ColumnName].ToString());
                                break;
                            case "thoughtofhurtingtxt":
                                itm.ThoughtsOfHurtingTxt = (r[c.ColumnName].ToString());
                                break;
                            case "timefinishingrntriage":
                                itm.TimeFinishingRNtriage = (r[c.ColumnName].ToString());
                                break;
                            case "timeofintake":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.TimeOfIntake = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "timestartingrntriage":
                                itm.TimeStartingRNtriage = (r[c.ColumnName].ToString());
                                break;
                            case "treatmentrecommendations":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Treatmentrecommendations = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "treatmentcommendationsp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.TreatmentrecommendationsP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "treatmenttext":
                                itm.TreatmentText = (r[c.ColumnName].ToString());
                                break;
                            case "treatmenttextp":
                                itm.TreatmentTextP = (r[c.ColumnName].ToString());
                                break;
                            case "txtboxrevokeroiq2":
                                itm.txtBoxRevokeROIQ2 = (r[c.ColumnName].ToString());
                                break;
                            case "txtboxrevokeroiq8":
                                itm.txtBoxRevokeROIQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "txthadanyalcohol":
                                itm.txthadanyalcohol = (r[c.ColumnName].ToString());
                                break;
                            case "txthospitalisedlast30days":
                                itm.txthospitalisedlast30days = (r[c.ColumnName].ToString());
                                break;
                            case "txtpatientpainmanagementclinicmethadone":
                                itm.txtpatientatpainmanagementclinicmethadone = (r[c.ColumnName].ToString());
                                break;
                            case "txtplantopaytreatmentother":
                                itm.txtplantopaytreatmentother = (r[c.ColumnName].ToString());
                                break;
                            case "txtplantopaytreatmentprivateinsurance":
                                itm.txtplantopaytreatmentprivateinsurance = (r[c.ColumnName].ToString());
                                break;
                            case "txtpreviouslybeenpatientmedicationassisted":
                                itm.txtpreviouslybeenpatientmedicationassisted = (r[c.ColumnName].ToString());
                                break;
                            case "txtsymptomsofpioidwithdrawaltrue":
                                itm.txtsymptomsofopioidwithdrawaltrue = (r[c.ColumnName].ToString());
                                break;
                            case "typeofmedication":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.TypeOfMedication = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "unlessrevokedma":
                                itm.unlessrevokedMA = (r[c.ColumnName].ToString());
                                break;
                            case "unlessrevokedq8":
                                itm.unlessrevokedQ8 = (r[c.ColumnName].ToString());
                                break;
                            case "upondischragefromtreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Upondischargefromtreatment = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "upondischargefromtreatmentp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.UpondischargefromtreatmentP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "uponreceiptofinformationrequested":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Uponreceiptofinformationrequested = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "uponreceiptofinformationrequestedp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.UponreceiptofinformationrequestedP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "uponreceiptofpaymentforservicesrebdered":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Uponreceiptofpaymentforservicesrendered = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "uponreceiptofpaymentforservicesrenderedP":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.UponreceiptofpaymentforservicesrenderedP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "urinalysisresults":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.Urinalysisresults = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "urinalysisresultsp":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { itm.UrinalysisresultsP = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "urinalysistext":
                                itm.UrinalysisText = (r[c.ColumnName].ToString());
                                break;
                            case "urinalysistextp":
                                itm.UrinalysisTextP = (r[c.ColumnName].ToString());
                                break;
                            case "usingopiods":
                                itm.UsingOpioids = (r[c.ColumnName].ToString());
                                break;
                            case "version":
                                itm.Version = (r[c.ColumnName].ToString());
                                break;
                            case "whataccomofations":
                                itm.WhatAccomodations = (r[c.ColumnName].ToString());
                                break;
                            case "wheretransfer":
                                itm.WhereTransfer = (r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    TblSFPatientPreAdmission dbitm = dbList.FirstOrDefault(x => x.SiteCode == itm.SiteCode && x.ID == itm.ID);
                    if (dbitm == null)
                    {
                        rc.RowsIns += 1;
                        NewItems.Add(itm);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbitm.AccomodationNeeded = itm.AccomodationNeeded;
                        dbitm.AcknowledgeClientSignature = itm.AcknowledgeClientSignature;
                        dbitm.AcknowledgeClientSignatureDate = itm.AcknowledgeClientSignatureDate;
                        dbitm.AcknowledgeClientSignatureDateP = itm.AcknowledgeClientSignatureDateP;
                        dbitm.AcknowledgeClientSignatureP = itm.AcknowledgeClientSignatureP;
                        dbitm.AcknowledgeWitnessSignature = itm.AcknowledgeWitnessSignature;
                        dbitm.AcknowledgeWitnessSignatureDate = itm.AcknowledgeWitnessSignatureDate;
                        dbitm.AcknowledgeWitnessSignatureDateP = itm.AcknowledgeWitnessSignatureDateP;
                        dbitm.AcknowledgeWitnessSignatureP = itm.AcknowledgeWitnessSignatureP;
                        dbitm.Active = itm.Active;
                        dbitm.ActiveP = itm.ActiveP;
                        dbitm.AdditionalComments = itm.AdditionalComments;
                        dbitm.AdditionalDoc = itm.AdditionalDoc;
                        dbitm.AddressMA = itm.AddressMA;
                        dbitm.AddressQ8 = itm.AddressQ8;
                        dbitm.AlcoholAmount = itm.AlcoholAmount;
                        dbitm.AlcoholLastDrink = itm.AlcoholLastDrink;
                        dbitm.AnswerAllQuestion = itm.AnswerAllQuestion;
                        dbitm.AnswerAllQuestionTxt = itm.AnswerAllQuestionTxt;
                        dbitm.AnswerRangeAbove = itm.AnswerRangeAbove;
                        dbitm.AnswerRangeNine = itm.AnswerRangeNine;
                        dbitm.AnswerRangeSix = itm.AnswerRangeSix;
                        dbitm.AnswerRangeThree = itm.AnswerRangeThree;
                        dbitm.AnyChangestoInformation = itm.AnyChangestoInformation;
                        dbitm.ApplicantName = itm.ApplicantName;
                        dbitm.AppointmentDate = itm.AppointmentDate;
                        dbitm.AppointmentTime = itm.AppointmentTime;
                        dbitm.AreYouCurrentlyPregnant = itm.AreYouCurrentlyPregnant;
                        dbitm.Biopsychosocialassessment = itm.Biopsychosocialassessment;
                        dbitm.BiopsychosocialassessmentP = itm.BiopsychosocialassessmentP;
                        dbitm.BiopsychosocialText = itm.BiopsychosocialText;
                        dbitm.BiopsychosocialTextP = itm.BiopsychosocialTextP;
                        dbitm.BringIDProof = itm.BringIDProof;
                        dbitm.BringInsuranceCard = itm.BringInsuranceCard;
                        dbitm.ChkboxProtectedHealthInformationQ2_1 = itm.ChkboxProtectedHealthInformationQ2_1;
                        dbitm.ChkboxProtectedHealthInformationQ2_2 = itm.ChkboxProtectedHealthInformationQ2_2;
                        dbitm.ChkboxProtectedHealthInformationQ2_3 = itm.ChkboxProtectedHealthInformationQ2_3;
                        dbitm.ChkboxProtectedHealthInformationQ2_4 = itm.ChkboxProtectedHealthInformationQ2_4;
                        dbitm.ChkboxProtectedHealthInformationQ8_1 = itm.ChkboxProtectedHealthInformationQ8_1;
                        dbitm.ChkboxProtectedHealthInformationQ8_2 = itm.ChkboxProtectedHealthInformationQ8_2;
                        dbitm.ChkboxProtectedHealthInformationQ8_3 = itm.ChkboxProtectedHealthInformationQ8_3;
                        dbitm.ChkboxProtectedHealthInformationQ8_4 = itm.ChkboxProtectedHealthInformationQ8_4;
                        dbitm.chkBoxRevokeROIQ2 = itm.chkBoxRevokeROIQ2;
                        dbitm.chkBoxRevokeROIQ8 = itm.chkBoxRevokeROIQ8;
                        dbitm.ChkboxSUDInformationQ2_1 = itm.ChkboxSUDInformationQ2_1;
                        dbitm.ChkboxSUDInformationQ2_10 = itm.ChkboxSUDInformationQ2_10;
                        dbitm.ChkboxSUDInformationQ2_11 = itm.ChkboxSUDInformationQ2_11;
                        dbitm.ChkboxSUDInformationQ2_12 = itm.ChkboxSUDInformationQ2_12;
                        dbitm.ChkboxSUDInformationQ2_13 = itm.ChkboxSUDInformationQ2_13;
                        dbitm.ChkboxSUDInformationQ2_2 = itm.ChkboxSUDInformationQ2_2;
                        dbitm.ChkboxSUDInformationQ2_3 = itm.ChkboxSUDInformationQ2_3;
                        dbitm.ChkboxSUDInformationQ2_4 = itm.ChkboxSUDInformationQ2_4;
                        dbitm.ChkboxSUDInformationQ2_5 = itm.ChkboxSUDInformationQ2_5;
                        dbitm.ChkboxSUDInformationQ2_6 = itm.ChkboxSUDInformationQ2_6;
                        dbitm.ChkboxSUDInformationQ2_7 = itm.ChkboxSUDInformationQ2_7;
                        dbitm.ChkboxSUDInformationQ2_8 = itm.ChkboxSUDInformationQ2_8;
                        dbitm.ChkboxSUDInformationQ2_9 = itm.ChkboxSUDInformationQ2_9;
                        dbitm.ChkboxSUDInformationQ8_1 = itm.ChkboxSUDInformationQ8_1;
                        dbitm.ChkboxSUDInformationQ8_10 = itm.ChkboxSUDInformationQ8_10;
                        dbitm.ChkboxSUDInformationQ8_11 = itm.ChkboxSUDInformationQ8_11;
                        dbitm.ChkboxSUDInformationQ8_12 = itm.ChkboxSUDInformationQ8_12;
                        dbitm.ChkboxSUDInformationQ8_13 = itm.ChkboxSUDInformationQ8_13;
                        dbitm.ChkboxSUDInformationQ8_2 = itm.ChkboxSUDInformationQ8_2;
                        dbitm.ChkboxSUDInformationQ8_3 = itm.ChkboxSUDInformationQ8_3;
                        dbitm.ChkboxSUDInformationQ8_4 = itm.ChkboxSUDInformationQ8_4;
                        dbitm.ChkboxSUDInformationQ8_5 = itm.ChkboxSUDInformationQ8_5;
                        dbitm.ChkboxSUDInformationQ8_6 = itm.ChkboxSUDInformationQ8_6;
                        dbitm.ChkboxSUDInformationQ8_7 = itm.ChkboxSUDInformationQ8_7;
                        dbitm.ChkboxSUDInformationQ8_8 = itm.ChkboxSUDInformationQ8_8;
                        dbitm.ChkboxSUDInformationQ8_9 = itm.ChkboxSUDInformationQ8_9;
                        dbitm.ChronicMedicalCondition = itm.ChronicMedicalCondition;
                        dbitm.ClientAddress = itm.ClientAddress;
                        dbitm.ClientDetails = itm.ClientDetails;
                        dbitm.ClientNameP = itm.ClientNameP;
                        dbitm.ClinicInfo = itm.ClinicInfo;
                        dbitm.Comment = itm.Comment;
                        dbitm.Comments = itm.Comments;
                        dbitm.Created = itm.Created;
                        dbitm.CreatedBy = itm.CreatedBy;
                        dbitm.CreatedByP = itm.CreatedByP;
                        dbitm.CreatedOn = itm.CreatedOn;
                        dbitm.CreatedP = itm.CreatedP;
                        dbitm.CurrentlyMedicatedTreatmentOther = itm.CurrentlyMedicatedTreatmentOther;
                        dbitm.currentlypregnant = itm.currentlypregnant;
                        dbitm.CurrentlyUsingOpoidDrug = itm.CurrentlyUsingOpoidDrug;
                        dbitm.CurrentOpiateProgramDose = itm.CurrentOpiateProgramDose;
                        dbitm.CurrentOpiateProgramFrom = itm.CurrentOpiateProgramFrom;
                        dbitm.CurrentOpiateProgramLocation = itm.CurrentOpiateProgramLocation;
                        dbitm.CurrentOpiateProgramNoOfMonths = itm.CurrentOpiateProgramNoOfMonths;
                        dbitm.CurrentOpiateProgramNoOfYears = itm.CurrentOpiateProgramNoOfYears;
                        dbitm.CurrentOpiateProgramTo = itm.CurrentOpiateProgramTo;
                        dbitm.CurrentOpiateWhatProgram = itm.CurrentOpiateWhatProgram;
                        dbitm.CurrntlyRecevingTreatmentForCondition = itm.CurrntlyRecevingTreatmentForCondition;
                        dbitm.DailyTime = itm.DailyTime;
                        dbitm.DataFormId = itm.DataFormId;
                        dbitm.Date = itm.Date;
                        dbitm.DateofRelease = itm.DateofRelease;
                        dbitm.DateOfStaffSignature = itm.DateOfStaffSignature;
                        dbitm.DateP = itm.DateP;
                        dbitm.datepreviouslypatientmedicationassisted = itm.datepreviouslypatientmedicationassisted;
                        dbitm.datetimelastopioid = itm.datetimelastopioid;
                        dbitm.daysafterdischarge = itm.daysafterdischarge;
                        dbitm.daysafterdischargeP = itm.daysafterdischargeP;
                        dbitm.DDLPrimarySubstance = itm.DDLPrimarySubstance;
                        dbitm.DeniedDuetoCapacity = itm.DeniedDuetoCapacity;
                        dbitm.describeother = itm.describeother;
                        dbitm.describeother1 = itm.describeother1;
                        dbitm.describeother1P = itm.describeother1P;
                        dbitm.describeother2 = itm.describeother2;
                        dbitm.describeother2P = itm.describeother2P;
                        dbitm.describeotherP = itm.describeotherP;
                        dbitm.Diagnosis = itm.Diagnosis;
                        dbitm.DiagnosisP = itm.DiagnosisP;
                        dbitm.DiagnosisText = itm.DiagnosisText;
                        dbitm.DiagnosisTextP = itm.DiagnosisTextP;
                        dbitm.DischargeReason = itm.DischargeReason;
                        dbitm.DischargeReasonsText = itm.DischargeReasonsText;
                        dbitm.DischargeReasonsTextP = itm.DischargeReasonsTextP;
                        dbitm.DischargeSummary = itm.DischargeSummary;
                        dbitm.DischargeSummaryP = itm.DischargeSummaryP;
                        dbitm.DischargeSummaryText = itm.DischargeSummaryText;
                        dbitm.DischargeSummaryTextP = itm.DischargeSummaryTextP;
                        dbitm.DoctorSignature = itm.DoctorSignature;
                        dbitm.DoctorSignatureDate = itm.DoctorSignatureDate;
                        dbitm.DoYouHaveInsuranceNo = itm.DoYouHaveInsuranceNo;
                        dbitm.dropdownNumberOfChildren = itm.dropdownNumberOfChildren;
                        dbitm.dropdownPresentlyInPainScale1to10 = itm.dropdownPresentlyInPainScale1to10;
                        dbitm.DrugAdministrationTypeID = itm.DrugAdministrationTypeID;
                        dbitm.DrugApplicantTime = itm.DrugApplicantTime;
                        dbitm.DrugChoiceID = itm.DrugChoiceID;
                        dbitm.DrugDaily = itm.DrugDaily;
                        dbitm.DrugLastUsed = itm.DrugLastUsed;
                        dbitm.DrugofchoiceAdministered = itm.DrugofchoiceAdministered;
                        dbitm.DrugTaken = itm.DrugTaken;
                        dbitm.DrugUsing = itm.DrugUsing;
                        dbitm.EmployedNoTxt = itm.EmployedNoTxt;
                        dbitm.EmployedYesTxt = itm.EmployedYesTxt;
                        dbitm.EnrollmentId = itm.EnrollmentId;
                        dbitm.exclusionsMA = itm.exclusionsMA;
                        dbitm.exclusionsQ8 = itm.exclusionsQ8;
                        dbitm.FAXMA = itm.FAXMA;
                        dbitm.FAXQ8 = itm.FAXQ8;
                        dbitm.FollowupScreening = itm.FollowupScreening;
                        dbitm.genralTreatingproviderrelationshipMA = itm.genralTreatingproviderrelationshipMA;
                        dbitm.genralTreatingproviderrelationshipQ8 = itm.genralTreatingproviderrelationshipQ8;
                        dbitm.GynDoctorProviderName = itm.GynDoctorProviderName;
                        dbitm.GynDoctorProviderPhone = itm.GynDoctorProviderPhone;
                        dbitm.HadAnyHomicidalThoughts = itm.HadAnyHomicidalThoughts;
                        dbitm.HadAnyHomicidalThoughtsTxt = itm.HadAnyHomicidalThoughtsTxt;
                        dbitm.HadAnySuicidalThoughts = itm.HadAnySuicidalThoughts;
                        dbitm.HadAnySuicidalThoughtsTxt = itm.HadAnySuicidalThoughtsTxt;
                        dbitm.HasGynDoctor = itm.HasGynDoctor;
                        dbitm.HaveAnyUrgentNeeds = itm.HaveAnyUrgentNeeds;
                        dbitm.HaveBeenCarcerated = itm.HaveBeenCarcerated;
                        dbitm.HaveYouUsedOpoidDrug = itm.HaveYouUsedOpoidDrug;
                        dbitm.HCRCClinicLocationMA = itm.HCRCClinicLocationMA;
                        dbitm.HCRCClinicLocationQ8 = itm.HCRCClinicLocationQ8;
                        dbitm.IllicitSubstanceOther = itm.IllicitSubstanceOther;
                        dbitm.IllicitSubstanceOtherMgPerDay = itm.IllicitSubstanceOtherMgPerDay;
                        dbitm.ImmediateAssessment = itm.ImmediateAssessment;
                        dbitm.ImmediateAssessment911 = itm.ImmediateAssessment911;
                        dbitm.Incarcenated = itm.Incarcenated;
                        dbitm.InformationToObtained = itm.InformationToObtained;
                        dbitm.InformationToObtainedFax = itm.InformationToObtainedFax;
                        dbitm.InformationToObtainedFaxP = itm.InformationToObtainedFaxP;
                        dbitm.InformationToObtainedP = itm.InformationToObtainedP;
                        dbitm.InformationToObtainedVerbal = itm.InformationToObtainedVerbal;
                        dbitm.InformationToObtainedVerbalP = itm.InformationToObtainedVerbalP;
                        dbitm.InformationToObtainedWritten = itm.InformationToObtainedWritten;
                        dbitm.InformationToObtainedWrittenP = itm.InformationToObtainedWrittenP;
                        dbitm.InitialSceeningParticipationText = itm.InitialSceeningParticipationText;
                        dbitm.InitialSceeningParticipationTextP = itm.InitialSceeningParticipationTextP;
                        dbitm.InitialScreening = itm.InitialScreening;
                        dbitm.InitialScreeningSummaryText = itm.InitialScreeningSummaryText;
                        dbitm.InitialScreeningSummaryTextP = itm.InitialScreeningSummaryTextP;
                        dbitm.InsuranceDescription = itm.InsuranceDescription;
                        dbitm.InsuranceType = itm.InsuranceType;
                        dbitm.IntakeAppointmentScheduledDateTime = itm.IntakeAppointmentScheduledDateTime;
                        dbitm.IntakeProgram = itm.IntakeProgram;
                        dbitm.IntakeProgramDate = itm.IntakeProgramDate;
                        dbitm.interestedtransferringetsyes = itm.interestedtransferringetsyes;
                        dbitm.IsAbleToAttendTreatementCenterDaily = itm.IsAbleToAttendTreatementCenterDaily;
                        dbitm.IsAbleToPayIntakeAndWeeklyFee = itm.IsAbleToPayIntakeAndWeeklyFee;
                        dbitm.IsAddictionToUnstableNeeding = itm.IsAddictionToUnstableNeeding;
                        dbitm.IsAllergies = itm.IsAllergies;
                        dbitm.IsAmPm = itm.IsAmPm;
                        dbitm.IsAnyLegalPrescriptionForPain = itm.IsAnyLegalPrescriptionForPain;
                        dbitm.IsAnyOngoingMedicalCondition = itm.IsAnyOngoingMedicalCondition;
                        dbitm.IsAnyPrescriptionForPain = itm.IsAnyPrescriptionForPain;
                        dbitm.IsApplicantPregnant = itm.IsApplicantPregnant;
                        dbitm.IsAttemptedSuicide = itm.IsAttemptedSuicide;
                        dbitm.IsBehaviorallyunstabldangerous = itm.IsBehaviorallyunstabldangerous;
                        dbitm.IsClinicAddress = itm.IsClinicAddress;
                        dbitm.IsCSUAtFullCapacity = itm.IsCSUAtFullCapacity;
                        dbitm.IsCurrentlyInOpiateProgram = itm.IsCurrentlyInOpiateProgram;
                        dbitm.iscurrentlypatientpainmanagementmethadone = itm.iscurrentlypatientpainmanagementmethadone;
                        dbitm.IsCurrentlyPregnant = itm.IsCurrentlyPregnant;
                        dbitm.IsDeleted = itm.IsDeleted;
                        dbitm.IsDetox = itm.IsDetox;
                        dbitm.IsDidnotMeetMedicalNecessity = itm.IsDidnotMeetMedicalNecessity;
                        dbitm.isdoyouhavepicture = itm.isdoyouhavepicture;
                        dbitm.IsDrinkingAlcohol = itm.IsDrinkingAlcohol;
                        dbitm.IsEligibleForFurtherAssessment = itm.IsEligibleForFurtherAssessment;
                        dbitm.IsEmployed = itm.IsEmployed;
                        dbitm.IsEVSCompleted = itm.IsEVSCompleted;
                        dbitm.ishadalcoholinlast12hours = itm.ishadalcoholinlast12hours;
                        dbitm.ishaveyouhospitalizedinlast30daysyes = itm.ishaveyouhospitalizedinlast30daysyes;
                        dbitm.IsHavingLegalPrescription = itm.IsHavingLegalPrescription;
                        dbitm.IsHavingPlanForHowToCommitSuicide = itm.IsHavingPlanForHowToCommitSuicide;
                        dbitm.IsHomicidalThoughtWithin72Hours = itm.IsHomicidalThoughtWithin72Hours;
                        dbitm.IsImpairment = itm.IsImpairment;
                        dbitm.IsInsurance = itm.IsInsurance;
                        dbitm.IsInsuranceAvailable = itm.IsInsuranceAvailable;
                        dbitm.IsInsuranceCard = itm.IsInsuranceCard;
                        dbitm.IsIntakeAppointmentScheduled = itm.IsIntakeAppointmentScheduled;
                        dbitm.IsMaintenance = itm.IsMaintenance;
                        dbitm.IsMedicalEmergency = itm.IsMedicalEmergency;
                        dbitm.IsMedicalProblemsNeedingStabilization = itm.IsMedicalProblemsNeedingStabilization;
                        dbitm.isMentalHealthTreatment = itm.isMentalHealthTreatment;
                        dbitm.IsOther = itm.IsOther;
                        dbitm.isOverTheCounterMedications = itm.isOverTheCounterMedications;
                        dbitm.IsPackets = itm.IsPackets;
                        dbitm.IsParticipitatingInOtherOpioidProgram = itm.IsParticipitatingInOtherOpioidProgram;
                        dbitm.IsPastTreatmentHistory = itm.IsPastTreatmentHistory;
                        dbitm.IsPatientAdmitted = itm.IsPatientAdmitted;
                        dbitm.IsPatientAtPainManagementClinic = itm.IsPatientAtPainManagementClinic;
                        dbitm.IsPhysicalHealthUnstable = itm.IsPhysicalHealthUnstable;
                        dbitm.IsPictureId = itm.IsPictureId;
                        dbitm.IsPlanForHowToHurtSomeElse = itm.IsPlanForHowToHurtSomeElse;
                        dbitm.IsPlanSendTime = itm.IsPlanSendTime;
                        dbitm.isplantopaytreatment = itm.isplantopaytreatment;
                        dbitm.ispregnant = itm.ispregnant;
                        dbitm.IsPresentedAtCloseOfDay = itm.IsPresentedAtCloseOfDay;
                        dbitm.IsPresentedWithoutIDProof = itm.IsPresentedWithoutIDProof;
                        dbitm.ispreviouslybeenpatientmedicationassisted = itm.ispreviouslybeenpatientmedicationassisted;
                        dbitm.IsPreviousMentalHealthTreatment = itm.IsPreviousMentalHealthTreatment;
                        dbitm.isprioretspatientyes = itm.isprioretspatientyes;
                        dbitm.IsPsychiatricProblemNeedingInpatientTreatment = itm.IsPsychiatricProblemNeedingInpatientTreatment;
                        dbitm.IsRecentlyReleasedFromPenal = itm.IsRecentlyReleasedFromPenal;
                        dbitm.IsReferralOffered = itm.IsReferralOffered;
                        dbitm.IsReliableTransportation = itm.IsReliableTransportation;
                        dbitm.IsRequestedAdmissionDrUnavailable = itm.IsRequestedAdmissionDrUnavailable;
                        dbitm.IsRequireChildCare = itm.IsRequireChildCare;
                        dbitm.IsServiceDeclined = itm.IsServiceDeclined;
                        dbitm.IsSpecialAccommodationRequired = itm.IsSpecialAccommodationRequired;
                        dbitm.IsSuicidalThoughtWithin72Hours = itm.IsSuicidalThoughtWithin72Hours;
                        dbitm.issymptomsofopioidwithdrawal = itm.issymptomsofopioidwithdrawal;
                        dbitm.IsTakingPrescriptionMedication = itm.IsTakingPrescriptionMedication;
                        dbitm.istheresubstanceusehistory = itm.istheresubstanceusehistory;
                        dbitm.IsThoughtsOfKilling = itm.IsThoughtsOfKilling;
                        dbitm.IsTransfer = itm.IsTransfer;
                        dbitm.IsTriagedtoMedicalDetoxFacility = itm.IsTriagedtoMedicalDetoxFacility;
                        dbitm.IsUnableToPay = itm.IsUnableToPay;
                        dbitm.IsUnableToProvideEvidenceofAddiction = itm.IsUnableToProvideEvidenceofAddiction;
                        dbitm.lastprescription = itm.lastprescription;
                        dbitm.LastUpdatedBy = itm.LastUpdatedBy;
                        dbitm.LastUpdateOn = itm.LastUpdateOn;
                        dbitm.LastUsedTime = itm.LastUsedTime;
                        dbitm.LegalPrescription1 = itm.LegalPrescription1;
                        dbitm.LegalPrescription2 = itm.LegalPrescription2;
                        dbitm.LegalPrescription3 = itm.LegalPrescription3;
                        dbitm.MedicalCondition1 = itm.MedicalCondition1;
                        dbitm.MedicalCondition2 = itm.MedicalCondition2;
                        dbitm.MedicalCondition3 = itm.MedicalCondition3;
                        dbitm.MedicalConditionsProviderName1 = itm.MedicalConditionsProviderName1;
                        dbitm.MedicalConditionsProviderName2 = itm.MedicalConditionsProviderName2;
                        dbitm.MedicalConditionsProviderPhone1 = itm.MedicalConditionsProviderPhone1;
                        dbitm.MedicalConditionsProviderPhone2 = itm.MedicalConditionsProviderPhone2;
                        dbitm.MedicalEmergencyDescribe = itm.MedicalEmergencyDescribe;
                        dbitm.Medicalinformation = itm.Medicalinformation;
                        dbitm.MedicalinformationP = itm.MedicalinformationP;
                        dbitm.MedicalText = itm.MedicalText;
                        dbitm.MedicalTextP = itm.MedicalTextP;
                        dbitm.MedicatedAssistedTreatmentProgram = itm.MedicatedAssistedTreatmentProgram;
                        dbitm.MedicationConditions = itm.MedicationConditions;
                        dbitm.MedicationName1 = itm.MedicationName1;
                        dbitm.MedicationName2 = itm.MedicationName2;
                        dbitm.MedicationName3 = itm.MedicationName3;
                        dbitm.MedicationName4 = itm.MedicationName4;
                        dbitm.MedicationName5 = itm.MedicationName5;
                        dbitm.MentalHealthFromDate = itm.MentalHealthFromDate;
                        dbitm.MentalHealthToDate = itm.MentalHealthToDate;
                        dbitm.MentalHealthTreatmentDescription = itm.MentalHealthTreatmentDescription;
                        dbitm.MentalHealthTreatmentWhat = itm.MentalHealthTreatmentWhat;
                        dbitm.MentalHealthTreatmentWhen = itm.MentalHealthTreatmentWhen;
                        dbitm.MentalHealthTreatmentWhere = itm.MentalHealthTreatmentWhere;
                        dbitm.Modified = itm.Modified;
                        dbitm.ModifiedBy = itm.ModifiedBy;
                        dbitm.ModifiedByP = itm.ModifiedByP;
                        dbitm.ModifiedP = itm.ModifiedP;
                        dbitm.NA = itm.NA;
                        dbitm.NamedEntitesIDQ2 = itm.NamedEntitesIDQ2;
                        dbitm.NamedEntitesIDQ8 = itm.NamedEntitesIDQ8;
                        dbitm.namedEntityMA = itm.namedEntityMA;
                        dbitm.namedEntityQ8 = itm.namedEntityQ8;
                        dbitm.namedEntityWithoutTreatmentMA = itm.namedEntityWithoutTreatmentMA;
                        dbitm.namedEntityWithoutTreatmentQ8 = itm.namedEntityWithoutTreatmentQ8;
                        dbitm.namedIndividualMA = itm.namedIndividualMA;
                        dbitm.namedIndividualPaticipantMA = itm.namedIndividualPaticipantMA;
                        dbitm.namedIndividualPaticipantQ8 = itm.namedIndividualPaticipantQ8;
                        dbitm.namedIndividualQ8 = itm.namedIndividualQ8;
                        dbitm.namedThirdPartyMA = itm.namedThirdPartyMA;
                        dbitm.namedThirdPartyQ8 = itm.namedThirdPartyQ8;
                        dbitm.nameOfWitness = itm.nameOfWitness;
                        dbitm.nameOfWitnessQ8 = itm.nameOfWitnessQ8;
                        dbitm.NeedReferal = itm.NeedReferal;
                        dbitm.ObservationComments = itm.ObservationComments;
                        dbitm.OfficeUseTime = itm.OfficeUseTime;
                        dbitm.OfficeUseWhy = itm.OfficeUseWhy;
                        dbitm.OngoingMedicalConditionsWha = itm.OngoingMedicalConditionsWha;
                        dbitm.OpiatesUsageNoOfMonths = itm.OpiatesUsageNoOfMonths;
                        dbitm.OpiatesUsageNoOfYears = itm.OpiatesUsageNoOfYears;
                        dbitm.other = itm.other;
                        dbitm.other2 = itm.other2;
                        dbitm.other2P = itm.other2P;
                        dbitm.OtherCode = itm.OtherCode;
                        dbitm.OtherDescription = itm.OtherDescription;
                        dbitm.otherP = itm.otherP;
                        dbitm.OtherText = itm.OtherText;
                        dbitm.OtherTextP = itm.OtherTextP;
                        dbitm.OverTheCounterMedicationsText1 = itm.OverTheCounterMedicationsText1;
                        dbitm.PacketTypeID = itm.PacketTypeID;
                        dbitm.PacketVersion = itm.PacketVersion;
                        dbitm.PainManagementClinic = itm.PainManagementClinic;
                        dbitm.PainManagementClinicFromDate = itm.PainManagementClinicFromDate;
                        dbitm.PainManagementClinicLocation = itm.PainManagementClinicLocation;
                        dbitm.PainManagementClinicToDate = itm.PainManagementClinicToDate;
                        dbitm.ParentPreAdmissionId = itm.ParentPreAdmissionId;
                        dbitm.Participationininitialsceeningprocess = itm.Participationininitialsceeningprocess;
                        dbitm.ParticipationininitialsceeningprocessP = itm.ParticipationininitialsceeningprocessP;
                        dbitm.PatientID = itm.PatientID;
                        dbitm.PatientSignature = itm.PatientSignature;
                        dbitm.PatientSignatureDate = itm.PatientSignatureDate;
                        dbitm.PersonForIntakeProcess = itm.PersonForIntakeProcess;
                        dbitm.Phone1 = itm.Phone1;
                        dbitm.Phone2 = itm.Phone2;
                        dbitm.Phone3 = itm.Phone3;
                        dbitm.Phone4 = itm.Phone4;
                        dbitm.Phone5 = itm.Phone5;
                        dbitm.PhoneMA = itm.PhoneMA;
                        dbitm.PhoneQ8 = itm.PhoneQ8;
                        dbitm.PlanOfSuicide = itm.PlanOfSuicide;
                        dbitm.PlanOnSpendingTimeAtClinic = itm.PlanOnSpendingTimeAtClinic;
                        dbitm.PreAdd_Address = itm.PreAdd_Address;
                        dbitm.PreAdmissionDate = itm.PreAdmissionDate;
                        dbitm.Prescriber1 = itm.Prescriber1;
                        dbitm.Prescriber2 = itm.Prescriber2;
                        dbitm.Prescriber3 = itm.Prescriber3;
                        dbitm.Prescriber4 = itm.Prescriber4;
                        dbitm.Prescriber5 = itm.Prescriber5;
                        dbitm.PrimaryReferralSource = itm.PrimaryReferralSource;
                        dbitm.PrimaryReferralSourceNote = itm.PrimaryReferralSourceNote;
                        dbitm.prioretspatient = itm.prioretspatient;
                        dbitm.ProgramID = itm.ProgramID;
                        dbitm.ProgressRecoveryGoalsText = itm.ProgressRecoveryGoalsText;
                        dbitm.ProgressRecoveryGoalsTextP = itm.ProgressRecoveryGoalsTextP;
                        dbitm.Progresstowardsrecoverygoals = itm.Progresstowardsrecoverygoals;
                        dbitm.ProgresstowardsrecoverygoalsP = itm.ProgresstowardsrecoverygoalsP;
                        dbitm.ProofOfOpiateDependence = itm.ProofOfOpiateDependence;
                        dbitm.protectedHealthInformationMA = itm.protectedHealthInformationMA;
                        dbitm.protectedHealthInformationQ8 = itm.protectedHealthInformationQ8;
                        dbitm.purposeOfDisclosureMA = itm.purposeOfDisclosureMA;
                        dbitm.purposeOfDisclosureQ8 = itm.purposeOfDisclosureQ8;
                        dbitm.PurposeOfObtainingRelease = itm.PurposeOfObtainingRelease;
                        dbitm.PurposeOfObtainingReleaseP = itm.PurposeOfObtainingReleaseP;
                        dbitm.radioGeneralDesignationQ2 = itm.radioGeneralDesignationQ2;
                        dbitm.radioGeneralDesignationQ8 = itm.radioGeneralDesignationQ8;
                        dbitm.radioNamedEntityProvideRelationWithMeQ2 = itm.radioNamedEntityProvideRelationWithMeQ2;
                        dbitm.radioNamedEntityProvideRelationWithMeQ8 = itm.radioNamedEntityProvideRelationWithMeQ8;
                        dbitm.radioNamedEntityWithoutProvideRelationWithMeQ2 = itm.radioNamedEntityWithoutProvideRelationWithMeQ2;
                        dbitm.radioNamedEntityWithoutProvideRelationWithMeQ8 = itm.radioNamedEntityWithoutProvideRelationWithMeQ8;
                        dbitm.radioNamedIndividualParticipantQ2 = itm.radioNamedIndividualParticipantQ2;
                        dbitm.radioNamedIndividualParticipantQ2select = itm.radioNamedIndividualParticipantQ2select;
                        dbitm.radioNamedIndividualParticipantQ8 = itm.radioNamedIndividualParticipantQ8;
                        dbitm.radioNamedIndividualParticipantQ8select = itm.radioNamedIndividualParticipantQ8select;
                        dbitm.radioNamedIndividualQ2 = itm.radioNamedIndividualQ2;
                        dbitm.radioNamedIndividualQ2select = itm.radioNamedIndividualQ2select;
                        dbitm.radioNamedIndividualQ8 = itm.radioNamedIndividualQ8;
                        dbitm.radioNamedIndividualQ8select = itm.radioNamedIndividualQ8select;
                        dbitm.radioNamedthirdpartyPayerQ2 = itm.radioNamedthirdpartyPayerQ2;
                        dbitm.radioNamedthirdpartyPayerQ8 = itm.radioNamedthirdpartyPayerQ8;
                        dbitm.Reason = itm.Reason;
                        dbitm.ReasonForDenial = itm.ReasonForDenial;
                        dbitm.ReasonforReferral = itm.ReasonforReferral;
                        dbitm.ReasonSeekingTreatment = itm.ReasonSeekingTreatment;
                        dbitm.Reasonsfordischarge = itm.Reasonsfordischarge;
                        dbitm.ReasonsfordischargeP = itm.ReasonsfordischargeP;
                        dbitm.ReceivedTreatmentForAddiction = itm.ReceivedTreatmentForAddiction;
                        dbitm.ReceiveingAnyTreatment = itm.ReceiveingAnyTreatment;
                        dbitm.RecentHeadTrauma = itm.RecentHeadTrauma;
                        dbitm.Recoveryplansorgoals = itm.Recoveryplansorgoals;
                        dbitm.RecoveryplansorgoalsP = itm.RecoveryplansorgoalsP;
                        dbitm.RecoveryPlansText = itm.RecoveryPlansText;
                        dbitm.RecoveryPlansTextP = itm.RecoveryPlansTextP;
                        dbitm.ReferedTo = itm.ReferedTo;
                        dbitm.ReferralOfferedList = itm.ReferralOfferedList;
                        dbitm.Referralrecommendations = itm.Referralrecommendations;
                        dbitm.ReferralrecommendationsP = itm.ReferralrecommendationsP;
                        dbitm.ReferralRecommendationsText = itm.ReferralRecommendationsText;
                        dbitm.ReferralRecommendationsTextP = itm.ReferralRecommendationsTextP;
                        dbitm.ReferralSourceID = itm.ReferralSourceID;
                        dbitm.ReferredBy = itm.ReferredBy;
                        dbitm.RegistrationModeID = itm.RegistrationModeID;
                        dbitm.RejectedReferralOffered = itm.RejectedReferralOffered;
                        dbitm.Relapseepisodes = itm.Relapseepisodes;
                        dbitm.RelapseepisodesP = itm.RelapseepisodesP;
                        dbitm.RelapseEpisodesText = itm.RelapseEpisodesText;
                        dbitm.RelapseEpisodesTextP = itm.RelapseEpisodesTextP;
                        dbitm.ReleasedFromPenalDate = itm.ReleasedFromPenalDate;
                        dbitm.ReliableTransportationTxt = itm.ReliableTransportationTxt;
                        dbitm.representativesRelationshiptoPatientMA = itm.representativesRelationshiptoPatientMA;
                        dbitm.representativesRelationshiptoPatientQ8 = itm.representativesRelationshiptoPatientQ8;
                        dbitm.RequestedAdmissionAMOrPM = itm.RequestedAdmissionAMOrPM;
                        dbitm.RequestedAdmissionDay = itm.RequestedAdmissionDay;
                        dbitm.requestedAdmissionDayFri = itm.requestedAdmissionDayFri;
                        dbitm.requestedAdmissionDayMon = itm.requestedAdmissionDayMon;
                        dbitm.requestedAdmissionDayThu = itm.requestedAdmissionDayThu;
                        dbitm.requestedAdmissionDayTue = itm.requestedAdmissionDayTue;
                        dbitm.requestedAdmissionDayWed = itm.requestedAdmissionDayWed;
                        dbitm.RequireAssistiveTechnologies = itm.RequireAssistiveTechnologies;
                        dbitm.RequireTransportationServices = itm.RequireTransportationServices;
                        dbitm.ReviewedResponsesFromCallCenter = itm.ReviewedResponsesFromCallCenter;
                        dbitm.RevocationClientSignature = itm.RevocationClientSignature;
                        dbitm.RevocationClientSignatureDate = itm.RevocationClientSignatureDate;
                        dbitm.RevocationClientSignatureDateP = itm.RevocationClientSignatureDateP;
                        dbitm.RevocationClientSignatureP = itm.RevocationClientSignatureP;
                        dbitm.RevocationWitnessSignature = itm.RevocationWitnessSignature;
                        dbitm.RevocationWitnessSignatureDate = itm.RevocationWitnessSignatureDate;
                        dbitm.RevocationWitnessSignatureDateP = itm.RevocationWitnessSignatureDateP;
                        dbitm.RevocationWitnessSignatureP = itm.RevocationWitnessSignatureP;
                        dbitm.RNPStaffSign = itm.RNPStaffSign;
                        dbitm.RNPStaffSignDate = itm.RNPStaffSignDate;
                        dbitm.SammsProgramID = itm.SammsProgramID;
                        dbitm.ScreenedBy = itm.ScreenedBy;
                        dbitm.ScreenedByDate = itm.ScreenedByDate;
                        dbitm.SecondaryReferralSource = itm.SecondaryReferralSource;
                        dbitm.SeizureInPast7Days = itm.SeizureInPast7Days;
                        dbitm.SetupAppointment = itm.SetupAppointment;
                        dbitm.SignatureDate = itm.SignatureDate;
                        dbitm.SignatureOfConsenP = itm.SignatureOfConsenP;
                        dbitm.SignatureOfConsent = itm.SignatureOfConsent;
                        dbitm.SignatureOfConsentP = itm.SignatureOfConsentP;
                        dbitm.SignatureRnpStaff = itm.SignatureRnpStaff;
                        dbitm.staffAtHCRC = itm.staffAtHCRC;
                        dbitm.staffAtHCRCQ8 = itm.staffAtHCRCQ8;
                        dbitm.StaffSignCredentials = itm.StaffSignCredentials;
                        dbitm.SubstanceAbuseWhat = itm.SubstanceAbuseWhat;
                        dbitm.SubstanceAbuseWhen = itm.SubstanceAbuseWhen;
                        dbitm.SubstanceAbuseWhere = itm.SubstanceAbuseWhere;
                        dbitm.SubstanceUseFromDate = itm.SubstanceUseFromDate;
                        dbitm.SubstanceUseToDate = itm.SubstanceUseToDate;
                        dbitm.SUDinformationMA = itm.SUDinformationMA;
                        dbitm.SUDInformationOtherQ2 = itm.SUDInformationOtherQ2;
                        dbitm.SUDInformationOtherQ8 = itm.SUDInformationOtherQ8;
                        dbitm.SUDinformationQ8 = itm.SUDinformationQ8;
                        dbitm.SuicidalThoughtIntentScale = itm.SuicidalThoughtIntentScale;
                        dbitm.SuicideDetailsTxt = itm.SuicideDetailsTxt;
                        dbitm.Summaryofinitialscreeningprocess = itm.Summaryofinitialscreeningprocess;
                        dbitm.SummaryofinitialscreeningprocessP = itm.SummaryofinitialscreeningprocessP;
                        dbitm.SupervisorSignature = itm.SupervisorSignature;
                        dbitm.SupervisorSignatureDate = itm.SupervisorSignatureDate;
                        dbitm.TakenTime = itm.TakenTime;
                        dbitm.ThoughtsOfHurtingTxt = itm.ThoughtsOfHurtingTxt;
                        dbitm.TimeFinishingRNtriage = itm.TimeFinishingRNtriage;
                        dbitm.TimeOfIntake = itm.TimeOfIntake;
                        dbitm.TimeStartingRNtriage = itm.TimeStartingRNtriage;
                        dbitm.Treatmentrecommendations = itm.Treatmentrecommendations;
                        dbitm.TreatmentrecommendationsP = itm.TreatmentrecommendationsP;
                        dbitm.TreatmentText = itm.TreatmentText;
                        dbitm.TreatmentTextP = itm.TreatmentTextP;
                        dbitm.txtBoxRevokeROIQ2 = itm.txtBoxRevokeROIQ2;
                        dbitm.txtBoxRevokeROIQ8 = itm.txtBoxRevokeROIQ8;
                        dbitm.txthadanyalcohol = itm.txthadanyalcohol;
                        dbitm.txthospitalisedlast30days = itm.txthospitalisedlast30days;
                        dbitm.txtpatientatpainmanagementclinicmethadone = itm.txtpatientatpainmanagementclinicmethadone;
                        dbitm.txtplantopaytreatmentother = itm.txtplantopaytreatmentother;
                        dbitm.txtplantopaytreatmentprivateinsurance = itm.txtplantopaytreatmentprivateinsurance;
                        dbitm.txtpreviouslybeenpatientmedicationassisted = itm.txtpreviouslybeenpatientmedicationassisted;
                        dbitm.txtsymptomsofopioidwithdrawaltrue = itm.txtsymptomsofopioidwithdrawaltrue;
                        dbitm.TypeOfMedication = itm.TypeOfMedication;
                        dbitm.unlessrevokedMA = itm.unlessrevokedMA;
                        dbitm.unlessrevokedQ8 = itm.unlessrevokedQ8;
                        dbitm.Upondischargefromtreatment = itm.Upondischargefromtreatment;
                        dbitm.UpondischargefromtreatmentP = itm.UpondischargefromtreatmentP;
                        dbitm.Uponreceiptofinformationrequested = itm.Uponreceiptofinformationrequested;
                        dbitm.UponreceiptofinformationrequestedP = itm.UponreceiptofinformationrequestedP;
                        dbitm.Uponreceiptofpaymentforservicesrendered = itm.Uponreceiptofpaymentforservicesrendered;
                        dbitm.UponreceiptofpaymentforservicesrenderedP = itm.UponreceiptofpaymentforservicesrenderedP;
                        dbitm.Urinalysisresults = itm.Urinalysisresults;
                        dbitm.UrinalysisresultsP = itm.UrinalysisresultsP;
                        dbitm.UrinalysisText = itm.UrinalysisText;
                        dbitm.UrinalysisTextP = itm.UrinalysisTextP;
                        dbitm.UsingOpioids = itm.UsingOpioids;
                        dbitm.Version = itm.Version;
                        dbitm.WhatAccomodations = itm.WhatAccomodations;
                        dbitm.WhereTransfer = itm.WhereTransfer;
                    }
                }
                db.SaveChanges();
                if (NewItems.Count > 0)
                {
                    db.TblSFPatientPreAdmissions.AddRange(NewItems);
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

public Models.RCodes SaveBareBones(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
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
                foreach (DataRow r in tbl.Rows)
                {
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":

                                break;
                            case "id":
                                break;
                        }
                    }
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
