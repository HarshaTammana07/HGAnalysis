using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SavePreAdmissionV6(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rCodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblPreAdmissionV6> PreAds = db.TblPreAdmissionV6.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblPreAdmissionV6> NewPAs = new List<Models.TblPreAdmissionV6>();

                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblPreAdmissionV6 pa = new Models.TblPreAdmissionV6();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                pa.SiteCode = r[c.ColumnName].ToString();
                                pa.RowState = true;
                                pa.EtllastModAt = DateTime.Now;
                                break;
                            case "preadmissionid":
                                pa.PreAdmissionid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                pa.Clientid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "cltm4id":
                                pa.cltM4ID = r[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    pa.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                pa.Createdby = r[c.ColumnName].ToString();
                                break;
                            case "preadmissiondate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    pa.PreAdmissionDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "registrationmode":
                                pa.RegistrationMode = r[c.ColumnName].ToString();
                                break;
                            case "referralsourcedesc":
                                pa.ReferralSourcedesc = r[c.ColumnName].ToString();
                                break;
                            case "primaryreferralsourcenote":
                                pa.PrimaryReferralSourceNote = r[c.ColumnName].ToString();
                                break;
                            case "program":
                                pa.Program = r[c.ColumnName].ToString();
                                break;
                            case "insurancetype":
                                pa.InsuranceType = r[c.ColumnName].ToString();
                                break;
                            case "intakeprogram":
                                pa.IntakeProgram = r[c.ColumnName].ToString();
                                break;
                            case "intakeprogramdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    pa.IntakeProgramDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "iscurrentlyinopiateprogram":
                                pa.IsCurrentlyInOpiateProgram = r[c.ColumnName].ToString();
                                break;
                            case "ispatientatpainmanagementclinic":
                                pa.IsPatientAtPainManagementClinic = r[c.ColumnName].ToString();
                                break;
                            case "ishavinglegalprescription":
                                pa.IsHavingLegalPrescription = r[c.ColumnName].ToString();
                                break;
                            case "isanylegalprescriptionforpain":
                                pa.IsAnyLegalPrescriptionForPain = r[c.ColumnName].ToString();
                                break;
                            case "isanyongoingmedicalcondition":
                                pa.IsAnyOngoingMedicalCondition = r[c.ColumnName].ToString();
                                break;
                            case "issuicidalthoughtwithin72hours":
                                pa.IsSuicidalThoughtWithin72Hours = r[c.ColumnName].ToString();
                                break;
                            case "ishavingplanforhowtocommitsuicide":
                                pa.IsHavingPlanForHowToCommitSuicide = r[c.ColumnName].ToString();
                                break;
                            case "ishomicidalthoughtwithin72hours":
                                pa.IsHomicidalThoughtWithin72Hours = r[c.ColumnName].ToString();
                                break;
                            case "isrecentlyreleasedfrompenal":
                                pa.IsRecentlyReleasedFromPenal = r[c.ColumnName].ToString();
                                break;
                            case "isspecialaccommodationrequired":
                                pa.IsSpecialAccommodationRequired = r[c.ColumnName].ToString();
                                break;
                            case "reasonseekingtreatment":
                                pa.ReasonSeekingTreatment = r[c.ColumnName].ToString();
                                break;
                            case "accomodationneeded":
                                pa.AccomodationNeeded = r[c.ColumnName].ToString();
                                break;
                            case "clientaddress":
                                pa.ClientAddress = r[c.ColumnName].ToString();
                                break;
                            case "comments":
                                pa.Comments = r[c.ColumnName].ToString();
                                break;
                            case "ispatientadmitted":
                                pa.IsPatientAdmitted = r[c.ColumnName].ToString();
                                break;
                            case "areyoucurrentlypregnant":
                                pa.AreYouCurrentlyPregnant = r[c.ColumnName].ToString();
                                break;
                            case "bringidproof":
                                pa.BringIdproof = r[c.ColumnName].ToString();
                                break;
                            case "bringinsurancecard":
                                pa.BringInsuranceCard = r[c.ColumnName].ToString();
                                break;
                            case "clinicinfo":
                                pa.ClinicInfo = r[c.ColumnName].ToString();
                                break;
                            case "currntlyrecevingtreatmentforcondition":
                                pa.CurrntlyRecevingTreatmentForCondition = r[c.ColumnName].ToString();
                                break;
                            case "isanyprescriptionforpain":
                                pa.IsAnyPrescriptionForPain = r[c.ColumnName].ToString();
                                break;
                            case "isinsurance":
                                pa.IsInsurance = r[c.ColumnName].ToString();
                                break;
                            case "isoverthecountermedications":
                                pa.IsOverTheCounterMedications = r[c.ColumnName].ToString();
                                break;
                            case "immediateassessment":
                                pa.ImmediateAssessment = r[c.ColumnName].ToString();
                                break;
                            case "immediateassessment911":
                                pa.ImmediateAssessment911 = r[c.ColumnName].ToString();
                                break;
                            case "medicalconditionsprovidername1":
                                pa.MedicalConditionsProviderName1 = r[c.ColumnName].ToString();
                                break;
                            case "medicalconditionsproviderphone1":
                                pa.MedicalConditionsProviderPhone1 = r[c.ColumnName].ToString();
                                break;
                            case "medicalconditionsprovidername2":
                                pa.MedicalConditionsProviderName2 = r[c.ColumnName].ToString();
                                break;
                            case "medicalconditionsproviderphone2":
                                pa.MedicalConditionsProviderPhone2 = r[c.ColumnName].ToString();
                                break;
                            case "planofsuicide":
                                pa.PlanOfSuicide = r[c.ColumnName].ToString();
                                break;
                            case "planonspendingtimeatclinic":
                                pa.PlanOnSpendingTimeAtClinic = r[c.ColumnName].ToString();
                                break;
                            case "sammsprogram":
                                pa.Sammsprogram = r[c.ColumnName].ToString();
                                break;
                            case "officeusewhy":
                                pa.OfficeUseWhy = r[c.ColumnName].ToString();
                                break;
                            case "ongoingmedicalconditionswha":
                                pa.OngoingMedicalConditionsWha = r[c.ColumnName].ToString();
                                break;
                            case "preaddaddress":
                                pa.PreAddAddress = r[c.ColumnName].ToString();
                                break;
                            case "lastupdatedby":
                                pa.LastUpdatedBy = r[c.ColumnName].ToString();
                                break;
                            case "lastupdateon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    pa.LastUpdateOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    pa.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dateofrelease":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    pa.DateofRelease = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                pa.Version = r[c.ColumnName].ToString();
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString() == "1")
                                {
                                    pa.RowState = false;
                                }
                                else
                                {
                                    pa.RowState = true;
                                }
                                break;
                            case "rowchksum":
                                pa.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.TblPreAdmissionV6 xpa = PreAds.Where(x => x.PreAdmissionid == pa.PreAdmissionid
                        && x.Clientid == pa.Clientid).FirstOrDefault();
                    if (xpa == null)
                    {
                        NewPAs.Add(pa);
                        rCodes.RowsIns++;
                    }
                    else
                    {
                        //if (xpa.RowChkSum != pa.RowChkSum)
                        {
                            xpa.AccomodationNeeded = pa.AccomodationNeeded;
                            xpa.AreYouCurrentlyPregnant = pa.AreYouCurrentlyPregnant;
                            xpa.BringIdproof = pa.BringIdproof;
                            xpa.BringInsuranceCard = pa.BringInsuranceCard;
                            xpa.ClientAddress = pa.ClientAddress;
                            xpa.ClinicInfo = pa.ClinicInfo;
                            xpa.Comments = pa.Comments;
                            xpa.Createdby = pa.Createdby;
                            xpa.CreatedOn = pa.CreatedOn;
                            xpa.CurrntlyRecevingTreatmentForCondition = pa.CurrntlyRecevingTreatmentForCondition;
                            xpa.DateofRelease = pa.DateofRelease;
                            xpa.EtllastModAt = pa.EtllastModAt;
                            xpa.ImmediateAssessment = pa.ImmediateAssessment;
                            xpa.ImmediateAssessment911 = pa.ImmediateAssessment911;
                            xpa.InsuranceType = pa.InsuranceType;
                            xpa.IntakeProgram = pa.IntakeProgram;
                            xpa.IntakeProgramDate = pa.IntakeProgramDate;
                            xpa.IsAnyLegalPrescriptionForPain = pa.IsAnyLegalPrescriptionForPain;
                            xpa.IsAnyOngoingMedicalCondition = pa.IsAnyOngoingMedicalCondition;
                            xpa.IsAnyPrescriptionForPain = pa.IsAnyPrescriptionForPain;
                            xpa.IsCurrentlyInOpiateProgram = pa.IsCurrentlyInOpiateProgram;
                            xpa.IsHavingLegalPrescription = pa.IsHavingLegalPrescription;
                            xpa.IsHavingPlanForHowToCommitSuicide = pa.IsHavingPlanForHowToCommitSuicide;
                            xpa.IsHomicidalThoughtWithin72Hours = pa.IsHomicidalThoughtWithin72Hours;
                            xpa.IsInsurance = pa.IsInsurance;
                            xpa.IsOverTheCounterMedications = pa.IsOverTheCounterMedications;
                            xpa.IsPatientAdmitted = pa.IsPatientAdmitted;
                            xpa.IsPatientAtPainManagementClinic = pa.IsPatientAtPainManagementClinic;
                            xpa.IsRecentlyReleasedFromPenal = pa.IsRecentlyReleasedFromPenal;
                            xpa.IsSpecialAccommodationRequired = pa.IsSpecialAccommodationRequired;
                            xpa.IsSuicidalThoughtWithin72Hours = pa.IsSuicidalThoughtWithin72Hours;
                            xpa.LastUpdatedBy = pa.LastUpdatedBy;
                            xpa.LastUpdateOn = pa.LastUpdateOn;
                            xpa.MedicalConditionsProviderName1 = pa.MedicalConditionsProviderName1;
                            xpa.MedicalConditionsProviderName2 = pa.MedicalConditionsProviderName2;
                            xpa.MedicalConditionsProviderPhone1 = pa.MedicalConditionsProviderPhone1;
                            xpa.MedicalConditionsProviderPhone2 = pa.MedicalConditionsProviderPhone2;
                            xpa.OfficeUseWhy = pa.OfficeUseWhy;
                            xpa.OngoingMedicalConditionsWha = pa.OngoingMedicalConditionsWha;
                            xpa.PatientSignatureDate = pa.PatientSignatureDate;
                            xpa.PlanOfSuicide = pa.PlanOfSuicide;
                            xpa.PlanOnSpendingTimeAtClinic = pa.PlanOnSpendingTimeAtClinic;
                            xpa.PreAddAddress = pa.PreAddAddress;
                            xpa.PreAdmissionDate = pa.PreAdmissionDate;
                            xpa.PrimaryReferralSourceNote = pa.PrimaryReferralSourceNote;
                            xpa.Program = pa.Program;
                            xpa.ReasonSeekingTreatment = pa.ReasonSeekingTreatment;
                            xpa.ReferralSourcedesc = pa.ReferralSourcedesc;
                            xpa.RegistrationMode = pa.RegistrationMode;
                            xpa.RowState = pa.RowState;
                            xpa.RowChkSum = pa.RowChkSum;
                            xpa.Sammsprogram = pa.Sammsprogram;
                            xpa.Version = pa.Version;
                            xpa.cltM4ID = pa.cltM4ID;
                        }
                        rCodes.RowsUpd++;
                    }
                }
                db.SaveChanges();
                if (NewPAs.Count > 0)
                {
                    db.TblPreAdmissionV6.AddRange(NewPAs);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rCodes.IsResult = false;
                rCodes.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rCodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }

            return rCodes;
        }

        public Models.RCodes SavePreAdminReferrals(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rCodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblPreAdmissionReferralSource> dbtbl = db.TblPreAdmissionReferralSource.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblPreAdmissionReferralSource> newPRS = new List<Models.TblPreAdmissionReferralSource>();
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblPreAdmissionReferralSource prs = new Models.TblPreAdmissionReferralSource();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                prs.SiteCode = r[c.ColumnName].ToString();
                                break;
                            case "id":
                                prs.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    prs.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0 )
                                {
                                    prs.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    prs.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "primaryreferralsource":
                                prs.PrimaryReferralSource = r[c.ColumnName].ToString();
                                break;
                            case "secondaryreferralsource":
                                prs.SecondaryReferralSource = r[c.ColumnName].ToString();
                                break;
                            case "referralsourcenote":
                                prs.ReferralSourceNote = r[c.ColumnName].ToString();
                                break;
                            case "createdby":
                                prs.CreatedBy = r[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    prs.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "lastupdatedby":
                                prs.LastUpdatedBy = r[c.ColumnName].ToString();
                                break;
                            case "lastupdateon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    prs.LastUpdateOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "enrollmentid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    prs.EnrollmentId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "program":
                                prs.Program = r[c.ColumnName].ToString();
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].GetType().ToString() == "System.Boolean")
                                {
                                    if (bool.Parse(r[c.ColumnName].ToString()))
                                    { prs.IsDeleted = 1; } else { prs.IsDeleted = 0; }
                                }
                                else
                                {
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        prs.IsDeleted = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    else
                                    {
                                        prs.IsDeleted = 0;
                                    }
                                }
                                break;
                            case "referralorganization":
                                prs.ReferralOrganization = r[c.ColumnName].ToString();
                                break;
                            case "referralname":
                                prs.ReferralName = r[c.ColumnName].ToString();
                                break;
                            case "accountnotinlist":
                                if (r[c.ColumnName].GetType().ToString() == "System.Boolean")
                                {
                                    if (bool.Parse(r[c.ColumnName].ToString()) == true)
                                    {
                                        prs.AccountNotInList = 1;
                                    }
                                    else { prs.AccountNotInList = 0; }
                                }
                                else
                                {
                                    if (r[c.ColumnName].ToString().Trim().Length > 0)
                                    {
                                        prs.AccountNotInList = int.Parse(r[c.ColumnName].ToString());
                                    }
                                }
                                break;
                            case "contactnotinlist":
                                if (r[c.ColumnName].GetType().ToString() == "System.Boolean")
                                {
                                    if (bool.Parse(r[c.ColumnName].ToString()) == true)
                                    {
                                        prs.ContactNotInList = 1;
                                    }
                                    else { prs.ContactNotInList = 0; }
                                }
                                else
                                {
                                    if (r[c.ColumnName].ToString().Trim().Length > 0)
                                    {
                                        prs.ContactNotInList = int.Parse(r[c.ColumnName].ToString());
                                    }
                                }
                                break;
                            case "whylefttreatmentofbhg":
                                prs.WhyLeftTreatmentOfBhg = r[c.ColumnName].ToString();
                                break;
                            case "whycomingbacktobhg":
                                prs.WhyComingBackToBhg = r[c.ColumnName].ToString();
                                break;
                            case "mostwanttododifferently":
                                prs.MostWantToDoDifferently = r[c.ColumnName].ToString();
                                break;
                            case "organization":
                                prs.Organization = r[c.ColumnName].ToString();
                                break;
                            case "name":
                                prs.Name = r[c.ColumnName].ToString();
                                break;
                            case "email":
                                prs.Email = r[c.ColumnName].ToString();
                                break;
                            case "phone":
                                prs.Phone = r[c.ColumnName].ToString();
                                break;
                            case "ispatientreadmit":
                                if (r[c.ColumnName].GetType().ToString() == "System.Boolean")
                                {
                                    prs.IsPatientReadmit = bool.Parse(r[c.ColumnName].ToString());
                                }
                                else
                                {
                                    if (r[c.ColumnName].ToString() == "1")
                                    {
                                        prs.IsPatientReadmit = true;
                                    }
                                    else
                                    {
                                        prs.IsPatientReadmit = false;
                                    }
                                }
                                break;
                            case "referralorganizationid":
                                prs.ReferralOrganizationId = r[c.ColumnName].ToString();
                                break;
                            case "referralnameid":
                                prs.ReferralNameId = r[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblPreAdmissionReferralSource dprs = dbtbl.FirstOrDefault(x => x.Id == prs.Id);
                    if (dprs == null)
                    {
                        newPRS.Add(prs);
                        rCodes.RowsIns += 1;
                    }
                    else
                    {
                        dprs.AccountNotInList = prs.AccountNotInList;
                        dprs.ClientId = prs.ClientId;
                        dprs.ContactNotInList = prs.ContactNotInList;
                        dprs.CreatedBy = prs.CreatedBy;
                        dprs.CreatedOn = prs.CreatedOn;
                        dprs.DataFormId = prs.DataFormId;
                        dprs.Email = prs.Email;
                        dprs.EnrollmentId = prs.EnrollmentId;
                        dprs.IsDeleted = prs.IsDeleted;
                        dprs.IsPatientReadmit = prs.IsPatientReadmit;
                        dprs.LastUpdatedBy = prs.LastUpdatedBy;
                        dprs.LastUpdateOn = prs.LastUpdateOn;
                        dprs.MostWantToDoDifferently = prs.MostWantToDoDifferently;
                        dprs.Name = prs.Name;
                        dprs.Organization = prs.Organization;
                        dprs.Phone = prs.Phone;
                        dprs.PreAdmissionId = prs.PreAdmissionId;
                        dprs.PrimaryReferralSource = prs.PrimaryReferralSource;
                        dprs.Program = prs.Program;
                        dprs.ReferralName = prs.ReferralName;
                        dprs.ReferralNameId = prs.ReferralNameId;
                        dprs.ReferralOrganization = prs.ReferralOrganization;
                        dprs.ReferralOrganizationId = prs.ReferralOrganizationId;
                        dprs.ReferralSourceNote = prs.ReferralSourceNote;
                        dprs.SecondaryReferralSource = prs.SecondaryReferralSource;
                        dprs.WhyComingBackToBhg = prs.WhyComingBackToBhg;
                        dprs.WhyLeftTreatmentOfBhg = prs.WhyLeftTreatmentOfBhg;
                        rCodes.RowsUpd += 1;
                    }
                }
                db.SaveChanges();
                if (newPRS.Count > 0)
                {
                    db.TblPreAdmissionReferralSource.AddRange(newPRS);
                    db.SaveChanges();
                }
            }
            catch(Exception e)
            {
                rCodes.IsResult = false;
                rCodes.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rCodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rCodes;
        }
    }
}
