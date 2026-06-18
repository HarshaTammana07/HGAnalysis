using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;

namespace BHGTaskRunner
{
    class Program
    {
        static void Main(string[] args)
        {
            BHG_DR_LIB.SelectConstructor sc = new BHG_DR_LIB.SelectConstructor();
            BHG_DR_LIB.SQLSvrManager sm = new BHG_DR_LIB.SQLSvrManager();
            BHG_DR_LIB.BulkDartsSvc bldr = new BHG_DR_LIB.BulkDartsSvc();
            BHG_DR_LIB.Models.BHG_DRContext db = new BHG_DR_LIB.Models.BHG_DRContext();
            BHG_DR_LIB.SaveData sd = new BHG_DR_LIB.SaveData();
            bool ChkSumEnabled = true;
            DataTable SrcDt = new DataTable();
            string strFlds = "";
            string strCmd;
            List<BHG_DR_LIB.Models.VwTaskListMap> Tasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
            List<BHG_DR_LIB.Models.VwTaskListMap> pTasks;
            if (args.Length == 0)
            {
                pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
            }
            else
            {
                switch (args[0].ToString())
                {
                    case "1":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMSGlobal" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "2":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                            && (x.TaskName == "Central ETL P1" || x.TaskName == "Eastern ETL P1" || x.TaskName == "Mountain ETL P1" || x.TaskName == "Pacific ETL P1")).ToList();
                        break;
                    case "3":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                        && x.TaskName != "Central ETL P1" && x.TaskName != "Eastern ETL P1" && x.TaskName != "Mountain ETL P1" && x.TaskName != "Pacific ETL P1"
                        && x.TaskName != "Central ETL P2" && x.TaskName != "Eastern ETL P2" && x.TaskName != "Mountain ETL P2" && x.TaskName != "Pacific ETL P2" 
                        && x.TaskName != "SAMMSGlobal").ToList();
                        break;
                    case "4":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                            && (x.TaskName == "Central ETL P2" || x.TaskName == "Eastern ETL P2" || x.TaskName == "Mountain ETL P2" || x.TaskName == "Pacific ETL P2")).ToList();
                        break;
                    case "5":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "Samms-LAB" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "6":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "Samms-Forms" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "7":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-Notes" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "8":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-INV" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "9":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-DartSvc" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "10":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-Dose" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "11":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-Orders" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "12":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-PPA" && x.RunAt < DateTime.Now).ToList();
                        break;
                    case "13":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-UAR" && x.RunAt < DateTime.Now).ToList();
                        break;
                    default:
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
                        break;
                }
            }
            string pq = "P1";
            //if (args[1] != null)
            //{
            //    pq = args[1].ToString();
            //}
            
            foreach(var pt in pTasks.Where(x => x.ParentTaskId == null).OrderBy(z => z.WorkDate).ThenBy(o => o.RunAt).ToList())
            {
                BHG_DR_LIB.Models.TblTasks ptask = db.TblTasks.Where(x => x.TaskId == pt.TaskId).FirstOrDefault();
                ptask.Status = 18;
                DateTime pt_start = DateTime.Now;
                ptask.Duration = "0";
                //ptask.RunAt = pt_start;
                if (ptask.RowCount == null) { ptask.RowCount = 0; }
                string Lasttbl = "";
                string PrevSite = "";

                foreach (var st in Tasks.Where(x => x.ParentTaskId == pt.TaskId).OrderBy(o => o.TaskName).ThenBy(b => b.SiteCode).ThenBy(d => d.FromTblVw).ToList())
                {
                    DateTime st_start = DateTime.Now;
                    BHG_DR_LIB.Models.TblTasks task = db.TblTasks.Where(x => x.TaskId == st.TaskId).FirstOrDefault();
                    task.Status = 18;
                    task.ErrorMessage = "";
                    task.RunAt = st_start;
                    BHG_DR_LIB.Models.RCodes rCodes = new BHG_DR_LIB.Models.RCodes
                    {
                        IsResult = false,
                        TaskId = st.TaskId
                    };
                    //if ((Lasttbl != st.TaskName) || (st.TaskName.ToLower() == "ctrl.tbl_clinic") 
                    //    || (st.ActionKey == 7 && st.ActionStepKey == 6)
                    //    || (st.FromTblVw == "vw_PayerClt")
                    //    || (st.TaskName.ToLower() == "pats.tbl_claims")
                    //    || (st.TaskName.ToLower() == "pats.tbl_claimlineitem")
                    //    || (st.TaskName.ToLower() == "pats.tbl_claimlineitemactivity")
                    //    || (st.TaskName.ToLower() == "pats.tbl_dartssrv")
                    //    || (st.ActionKey == 6)
                    //    || (st.ActionKey == 7))
                    //{
                    List<BHG_DR_LIB.Models.VwMapSrc2Dsn> tdwork = db.WorkToDo.Where(x => x.Enabled
                            && x.ActionKey == st.ActionKey
                            && x.ActionStepKey == st.ActionStepKey).ToList();
                    if (st.SiteCode == "PHC") { tdwork = tdwork.Where(x => x.PHC_Enabled).ToList(); }
                    if (st.ActionKey == 3) { ChkSumEnabled = false; } else { ChkSumEnabled = true; }
                    bool NewSchema = false;
                    if (!st.IsNewSchema.HasValue) { NewSchema = false; } else { NewSchema = st.IsNewSchema.Value; }
                    strFlds = sc.GetSLT(tdwork, ChkSumEnabled, NewSchema, st.FromTblVw, st.SiteCode)
                        .Replace("@SiteCode", "'" + st.SiteCode + "'")
                        .Replace("@Samms", "'SAMMS'");
                    Lasttbl = st.TaskName;
                    PrevSite = st.SiteCode;
                    //}
                    //if (PrevSite != st.SiteCode)
                    //{
                    //    strFlds = strFlds.Replace(PrevSite, st.SiteCode);
                    //    PrevSite = st.SiteCode;
                    //}
                    //if (st.TaskName.ToLower() == "pats.tbl_formssammsclient")
                    //{
                    //    strFlds = strFlds.Replace("'Global'", "isnull((select Prefix from dbo.tblSites where sID = fscsite), 'Global')");
                    //}
                        int DaysBack = -15;
                    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw;
                    string strWhere = st.WhereCondition.Replace("@SiteCode", "'" + st.SiteCode + "'")
                        .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
                        //.Replace("@EnrollCutoff", "'" + Cutoff + "'")
                        .Replace("@Samms", "'SAMMS'");
                    try
                    {
                        switch (st.TaskName.ToLower())
                        {
                            case "pats.tbl_3parnote":
                                if (st.SiteCode == "Lab")
                                {
                                    strCmd = strCmd.Replace(", [globalBatchId] globalBatchId", "").Replace(", [globalBatchId]", "");
                                }
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.Save3pArnote(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                break;
                            case "pats.tbl_3pclaimnote":
                                if (st.SiteCode == "Lab")
                                {
                                    strCmd = strCmd.Replace(", [globalBatchId] globalBatchId", "").Replace(", [globalBatchId]", "");
                                }
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.Save3pClaimNote(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                break;
                            case "ctrl.tbl_3psetup":
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.Save3pSetup(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                break;
                            case "ctrl.tbl_claimstatus":
                                strCmd += " Where tpcbdtcreated >= '" + st.WorkDate.Value.AddMonths(-12).ToShortDateString() + "' " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveClaimStatus (SrcDt, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "ayx.tbl_preadmission_v6":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where upper(name) = 'SF_PatientPreAdmission'", st.ConStr);
                                if ((SrcDt.Rows.Count == 1) && (st.SchemaVersion != "V5"))
                                {
                                    DataTable tblCols = sm.GetTableData("Cols", "select name, column_id from sys.all_columns c where c.object_id = (select object_id from sys.tables where upper(name) = 'SF_PatientPreAdmission')", st.ConStr);
                                    List<BHG_DR_LIB.Models.TblCols> lstCols = new List<BHG_DR_LIB.Models.TblCols>();
                                    foreach (DataRow r in tblCols.Rows)
                                    {
                                        lstCols.Add(new BHG_DR_LIB.Models.TblCols
                                        {
                                            ColName = r["name"].ToString(),
                                            ColID = int.Parse(r["Column_id"].ToString())
                                        }
                                        );
                                    }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "clientaddress").FirstOrDefault() != null)
                                    {
                                        strCmd = "select SiteCode = '" + st.SiteCode + "', pp.id as PreAdmissionid, pp.PatientID as Clientid, clt.cltM4ID, pp.CreatedON, pp.Createdby, pp.PreAdmissionDate " + Environment.NewLine +
                                        ", RegistrationMode = Case when pp.RegistrationModeID = 0 then 'Phone' when pp.RegistrationModeID = 1 then 'Walk-In' when pp.RegistrationModeID = 2 then 'By Appointment' else Cast(pp.RegistrationModeID as varchar) end " + Environment.NewLine +
                                        ", tc.cdeDesc as ReferralSourcedesc, pp.PrimaryReferralSourceNote, pg.Description as Program,  pp.InsuranceType,pp.IntakeProgram, pp.IntakeProgramDate " + Environment.NewLine +
                                        ", IsCurrentlyInOpiateProgram = Case when pp.IsCurrentlyInOpiateProgram = 1 then 'Yes' when pp.IsCurrentlyInOpiateProgram = 0 then 'No' else cast(pp.IsCurrentlyInOpiateProgram as varchar) end " + Environment.NewLine +
                                        ", IsPatientAtPainManagementClinic = Case when pp.IsPatientAtPainManagementClinic = 1 then 'Yes' when pp.IsPatientAtPainManagementClinic = 0 then 'No' else cast(pp.IsPatientAtPainManagementClinic as varchar) end " + Environment.NewLine +
                                        ", IsHavingLegalPrescription = Case when pp.IsHavingLegalPrescription = 1 then 'Yes' when pp.IsHavingLegalPrescription = 0 then 'No' else cast(pp.IsHavingLegalPrescription as varchar) end " + Environment.NewLine +
                                        ", IsAnyLegalPrescriptionForPain = Case when pp.IsAnyLegalPrescriptionForPain = 1 then 'Yes' when pp.IsAnyLegalPrescriptionForPain = 0 then 'No' else cast(pp.IsAnyLegalPrescriptionForPain as varchar) end " + Environment.NewLine +
                                        ", IsAnyOngoingMedicalCondition = Case when pp.IsAnyOngoingMedicalCondition = 1 then 'Yes' when pp.IsAnyOngoingMedicalCondition = 0 then 'No' else cast(pp.IsAnyOngoingMedicalCondition as varchar) end " + Environment.NewLine +
                                        ", IsSuicidalThoughtWithin72Hours = Case when pp.IsSuicidalThoughtWithin72Hours = 1 then 'Yes' when pp.IsSuicidalThoughtWithin72Hours = 0 then 'No' else cast(pp.IsSuicidalThoughtWithin72Hours as varchar) end " + Environment.NewLine +
                                        ", IsHavingPlanForHowToCommitSuicide = Case when pp.IsHavingPlanForHowToCommitSuicide = 1 then 'Yes' when pp.IsHavingPlanForHowToCommitSuicide = 0 then 'No' else cast(pp.IsHavingPlanForHowToCommitSuicide as varchar) end " + Environment.NewLine +
                                        ", IsHomicidalThoughtWithin72Hours = Case when pp.IsHomicidalThoughtWithin72Hours = 1 then 'Yes' when pp.IsHomicidalThoughtWithin72Hours = 0 then 'No' else cast(pp.IsHomicidalThoughtWithin72Hours as varchar) end " + Environment.NewLine +
                                        ", IsRecentlyReleasedFromPenal = Case when pp.IsRecentlyReleasedFromPenal = 1 then 'Yes' when pp.IsRecentlyReleasedFromPenal = 0 then 'No' else cast(pp.IsRecentlyReleasedFromPenal as varchar) end " + Environment.NewLine +
                                        ", IsSpecialAccommodationRequired = Case when pp.IsSpecialAccommodationRequired = 1 then 'Yes' when pp.IsSpecialAccommodationRequired = 0 then 'No' else cast(pp.IsSpecialAccommodationRequired as varchar) end " + Environment.NewLine +
                                        ", pp.ReasonSeekingTreatment, pp.AccomodationNeeded, pp.ClientAddress, pp.Comments " + Environment.NewLine +
                                        ", IsPatientAdmitted = Case when pp.IsPatientAdmitted = 1 then 'Yes' when pp.IsPatientAdmitted = 0 then 'No' else cast(pp.IsPatientAdmitted as varchar) end " + Environment.NewLine +
                                        ", AreYouCurrentlyPregnant = Case when pp.AreYouCurrentlyPregnant = 0 then 'Yes' when pp.AreYouCurrentlyPregnant = 1 then 'No' when pp.AreYouCurrentlyPregnant = 2 then 'Unknown' else cast(pp.AreYouCurrentlyPregnant as varchar) end " + Environment.NewLine +
                                        ", BringIDProof = Case when pp.BringIDProof = 1 then 'Yes' when pp.BringIDProof = 0 then 'No' else cast(pp.BringIDProof as varchar) end " + Environment.NewLine +
                                        ", BringInsuranceCard = Case when pp.BringInsuranceCard = 1 then 'Yes' when pp.BringInsuranceCard = 0 then 'No' else cast(pp.BringInsuranceCard as varchar) end " + Environment.NewLine +
                                        ", ClinicInfo = Case when pp.ClinicInfo = 1 then 'Yes' when pp.ClinicInfo = 0 then 'No' else cast(pp.ClinicInfo as varchar) end " + Environment.NewLine +
                                        ", CurrntlyRecevingTreatmentForCondition = Case when pp.CurrntlyRecevingTreatmentForCondition = 1 then 'Yes' when pp.CurrntlyRecevingTreatmentForCondition = 0 then 'No' else cast(pp.CurrntlyRecevingTreatmentForCondition as varchar) end " + Environment.NewLine +
                                        ", IsAnyPrescriptionForPain = Case when pp.IsAnyPrescriptionForPain = 1 then 'Yes' when pp.IsAnyPrescriptionForPain = 0 then 'No' else cast(pp.IsAnyPrescriptionForPain as varchar) end " + Environment.NewLine +
                                        ", IsInsurance = Case when pp.IsInsurance = 1 then 'Yes' when pp.IsInsurance = 0 then 'No' else cast(pp.IsInsurance as varchar) end " + Environment.NewLine +
                                        ", isOverTheCounterMedications = Case when pp.isOverTheCounterMedications = 1 then 'Yes' when pp.isOverTheCounterMedications = 0 then 'No' else cast(pp.isOverTheCounterMedications as varchar) end " + Environment.NewLine +
                                        ", pp.ImmediateAssessment, pp.ImmediateAssessment911, pp.MedicalConditionsProviderName1, pp.MedicalConditionsProviderPhone1, pp.MedicalConditionsProviderName2, pp.MedicalConditionsProviderPhone2 " + Environment.NewLine +
                                        ", PlanOfSuicide = Case when pp.PlanOfSuicide = 1 then 'Yes' when pp.PlanOfSuicide = 0 then 'No' else cast(pp.PlanOfSuicide as varchar) end " + Environment.NewLine +
                                        ", PlanOnSpendingTimeAtClinic = Case when pp.PlanOnSpendingTimeAtClinic = 1 then 'Yes' when pp.PlanOnSpendingTimeAtClinic = 0 then 'No' else cast(pp.PlanOnSpendingTimeAtClinic as varchar) end " + Environment.NewLine +
                                        ", SAMMSProgram = tc2.cdeDesc, pp.OfficeUseWhy, pp.OngoingMedicalConditionsWha, pp.PreAdd_Address, pp.LastUpdatedBy, pp.LastUpdateOn, pp.PatientSignatureDate, pp.DateofRelease, pp.Version, pp.IsDeleted " + Environment.NewLine +
                                        ", RowChkSum = CHECKSUM(pp.id, pp.PatientID, pp.LastUpdatedBy, pp.LastUpdateOn, pp.PatientSignatureDate, pp.DateofRelease, pp.Version, pp.IsDeleted, clt.cltM4ID)" + Environment.NewLine +
                                        " from SF_PatientPreAdmission PP left join [dbo].[SF_Program] pg on pp.ProgramID = pg.id left join[dbo].[tblCodes] tc on pp.ReferralSourceID = tc.cdeID left join[dbo].[tblCodes] tc2 on pp.SammsProgramID = tc2.cdeID " + Environment.NewLine +
                                        " left join dbo.tblClient clt on(pp.PatientID = clt.cltID) ";
                                        //" where pp.CreatedOn >'' and pp.ClientAddress not like '%test data%' order by pp.PatientID, pp.ID";
                                        strCmd += " Where " + strWhere + " " + st.SortOrder;
                                        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                        rCodes = sd.SavePreAdmissionV6(SrcDt, st.SiteCode, null);
                                    }
                                    else
                                    {
                                        rCodes.IsResult = false;
                                        rCodes.ExceptMsg = "Column ClientAddress does not exists.";
                                        rCodes.RowsProcessed = SrcDt.Rows.Count;
                                        rCodes.RowsIns = 0;
                                        rCodes.RowsUpd = 0;
                                    }
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists or SAMMS Version 5.";
                                    rCodes.RowsProcessed = SrcDt.Rows.Count;
                                    rCodes.RowsIns = 0;
                                    rCodes.RowsUpd = 0;
                                }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "ctrl.tbl_clinic":  //What's up with this?
                                if (st.SiteCode == "Lab")
                                {
                                    strCmd = strCmd.Replace(", PullPicsFromDB", "");
                                }
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveClinic(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "ctrl.tbl_consents":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                if (st.SiteCode == "PHC")
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveGlobalConsentsPhc(SrcDt, null);
                                }
                                else
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveGlobalConsents(SrcDt, null);
                                }                                
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "ctrl.tbl_globaldevices":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveGlobalDevices(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "ctrl.tbl_user":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveGlobalUser(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "ctrl.tbl_usersites":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveGlobalUserSite(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_3pelig":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.Save3pElig(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_admissionassessment":
                                //strWhere = st.WhereCondition.Replace("@SiteCode", "'" + st.SiteCode + "'")
                                //    .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
                                //    .Replace("@workDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
                                //    .Replace("@Samms", "'SAMMS'");
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentsummary":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentSummary(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentdimensionfour":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentDimensionfour(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentdimensiononedisorder":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentDimensionOneDisorder(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentdimensionfivesubstanceuse":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentDimensionFiveSubstanceUse(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentdimensiontwo":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentDimensionTwo(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentdimensionthree":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentDimensionThree(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentdimensionsix":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentDimensionSix(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessment":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentoccupational":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentOccupational(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentfamily":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentFamily(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentlegal":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentLegal(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentmentalhealth":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentMentalHealth(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentsubstanceuse":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentSubstanceUse(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentphysicalhealth":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentPhysicalHealth(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmentsocial":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentSocial(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_reassessmenttreatment":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveReAssessmentTreatment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_appointments":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAppointments(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_appointmentattend":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAppointmentAttend(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_orientationchecklistnew":
                                //strCmd = strCmd.Replace("[Id]", "a.[Id]");
                                strCmd = strCmd.Replace("[IsDeleted]", "case when b.IsDeleted = 1 then 1 when a.IsDeleted = 1 then 1 Else 0 end ");
                                strCmd = strCmd.Replace("from dbo.OrientationChecklistNew"
                                               , "from dbo.OrientationChecklistNew a inner join (select Id PreAdminId, IsDeleted from SF_PatientPreAdmission) b on (a.PreAdmissionId = b.PreAdminId)");
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveOrientationCheckList(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "ctrl.tbl_invtype":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveInvTypes(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                break;
                            case "pats.tbl_liquidlog":
                                strCmd = strCmd.Replace("desc, ", ", ");
                                if (st.Reload.HasValue)
                                {
                                    if (st.Reload.Value)
                                    {
                                        strCmd += " " + st.SortOrder;
                                    }
                                    else
                                    {
                                        strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    }
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    //Run Bulk upload
                                    rCodes.IsResult = true;
                                    rCodes.RowsProcessed = SrcDt.Rows.Count;
                                    BHG_DR_LIB.SQLSvrManager sqlm = new BHG_DR_LIB.SQLSvrManager();
                                    _ = sqlm.ExeSqlCmd("Truncate Table [stg].[tbl_liquidlog]", sqlm.ConnectionString);
                                    SqlBulkCopy bc = new SqlBulkCopy(sqlm.ConnectionString)
                                    {
                                        DestinationTableName = "[stg].[tbl_liquidlog]",
                                        BulkCopyTimeout = 99999
                                    };
                                    foreach (DataColumn c in SrcDt.Columns)
                                    {
                                        bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));
                                    }
                                    try
                                    {
                                        // Write from the source to the destination.
                                        bc.WriteToServer(SrcDt);
                                        var tblr6 = sqlm.ExecStrPro("stg.sp_liquidlog_Merge", "@sitecode", st.SiteCode, sm.ConnectionString);
                                        //tblr6 = sqlm.ExecStrPro("pats.BAMMerge", "@sitecode", st.SiteCode, sm.ConnectionString);
                                    }
                                    catch (Exception e)
                                    {
                                        rCodes.IsResult = false;
                                        rCodes.ExceptMsg = e.Message.ToString();
                                    }
                                }
                                else
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveLiquidlog(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                }
                                break;
                            case "pats.tbl_bamform":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveBamForm(SrcDt, st.SiteCode, null);
                                break;
                            case "pats.tbl_bamscore":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveBamScore(SrcDt, st.SiteCode, null);
                                break;
                            case "pats.tbl_tbldiag10":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                //rCodes = sd.SaveTblDiags(SrcDt, st.SiteCode, null);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, null);
                                break;
                            case "pats.tbl_bottle":
                                if (st.SiteCode == "Lab")
                                {
                                    strCmd = strCmd.Replace(", [ExpDate] ExpDate", "").Replace(", [ExpDate]", "");
                                }
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveBottles(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                break;
                            case "pats.tbl_bills":
                                //strCmd += " Where " + strWhere + " " + st.SortOrder;
                                int BillDaysBack = DaysBack;
                                if (st.Reload.HasValue)
                                {
                                    if (st.Reload.Value)
                                    {
                                        BillDaysBack = -728250;
                                    } 
                                }
                                strCmd += " where year(billDate) >= " + st.WorkDate.Value.AddDays(BillDaysBack).Year.ToString() 
                                    + " and billdate <= '" + st.WorkDate.Value.AddDays(12).ToShortDateString() + "'"
                                    + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if (SrcDt.Rows.Count > 0)
                                {
                                    rCodes = sd.SaveBills(SrcDt, st.SiteCode, st.WorkDate.Value.Date, BillDaysBack, null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.RowsProcessed = SrcDt.Rows.Count;
                                    rCodes.RowsIns = 0;
                                    rCodes.RowsUpd = 0;
                                }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_briefaddictionmonitor":
                                //strCmd += " Where fCltID > 0 and [date] is not null and fClinic not in (25, 100) ";
                                strCmd += " Where fCltID > 0 and convert(date,ltrim(substring([date], CHARINDEX(', ', [date])+2, len([date])-CHARINDEX(', ', [date])-1)), 109) >= '"
                                    + st.WorkDate.Value.AddDays(-30).ToShortDateString()
                                    + "' and fClinic not in (25, 100) ";
                                //+ st.WhereCondition.Replace("@WorkDate", "'" + rundt.ToShortDateString() + "'");
                                if (st.SiteCode == "PHC")
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;");
                                }
                                else
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                }
                                rCodes = sd.SaveBAM(SrcDt, task.WorkDate.Value.AddDays(-30).Date, null);
                                //Add execute Stored Procedure for BAM
                                var tblr = sm.ExecStrPro("pats.BAMMergeGbl", "@sitecode", "Global", sm.ConnectionString);
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_checkin":
                                int cols = sm.GetTableData("Cols", "select name, column_id from sys.all_columns c where c.object_id = (select object_id from sys.tables where upper(name) = 'TBLCHECKIN') and upper(name) = 'CIQUEUETIME'", st.ConStr).Rows.Count;
                                if (cols == 0)
                                {
                                    strCmd = strCmd.Replace(", [ciQUEUETIME]", "").Replace(" ciQUEUETIME", "");
                                }
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveCheckIn(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_claims":
                                rCodes.IsResult = true;
                                //strCmd += " Where tpcClaimBatchID in (select tpcbID from dbo.tbl3pClaimBatch where tpcbDtCreated >= '" + "')";
                                //Tst claim loading
                                if ((st.SiteCode == "VBRA") || (st.SiteCode == "VMIN") || (st.SiteCode == "VWBY") || (st.SiteCode == "VBRP")
                                    //|| (st.SiteCode == "B24") || (st.SiteCode == "B25") || (st.SiteCode == "B26") || (st.SiteCode == "B33") 
                                    //|| (st.SiteCode == "B34") || (st.SiteCode == "B35") || (st.SiteCode == "B35A") || (st.SiteCode == "BG") 
                                    //|| (st.SiteCode == "ET") || (st.SiteCode == "FR") || (st.SiteCode == "LO") || (st.SiteCode == "RMD")
                                    )
                                {
                                    strCmd += " where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveClaims(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack).Date, true, null);
                                }
                                else
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.AddDays(DaysBack).Date, db);
                                }
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, //int.Parse(sm.GetTableData("lcltbl", 
                                            //"Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " where tpcCreatedDate > '12/31/2018' ", st.ConStr).Rows[0][0].ToString())
                                            SrcDt.Rows.Count,
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode + "' and RowState = 1", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_claimlineitem":
                                //" where cli.tpcliTPCID in (select distinct c.tpcID from dbo.tbl3pClaim c where c.tpcClaimBatchID in " +
                                //" (select tpcbID from dbo.tbl3pClaimBatch where tpcbDtCreated >= '8/1/2021'))"
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, SrcDt.Rows.Count
                                            //int.Parse(sm.GetTableData("lcltbl", "Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " where tpcliDtmAdded > '12/31/2018' ", st.ConStr).Rows[0][0].ToString())
                                        , int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + 
                                        " where SiteCode = '" + st.SiteCode + "' and RowState = 1", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_claimlineitemactivity":
                                //" where liaTPCLIID in (select distinct cli.tpcliID from dbo.tbl3pClaimLineItem cli where tpcliTPCID in " + 
                                //" (select distinct c.tpcID from dbo.tbl3pClaim c where c.tpcClaimBatchID in " + 
                                //" (select tpcbID from dbo.tbl3pClaimBatch where year(tpcbDtCreated) >= 2021)))"
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, //int.Parse(sm.GetTableData("lcltbl",
                                        //"Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " where liaDtm > '12/31/2018' "
                                        //, st.ConStr).Rows[0][0].ToString())
                                        SrcDt.Rows.Count, int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName +
                                        " where SiteCode = '" + st.SiteCode + "' and RowState = 1", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "stg.clientdemo":
                                strCmd = "select siteCode ='" + st.SiteCode + "', AMSID, clt3pBack, clt3pfront, clt911Name, clt911PH, clt911Relation" +
                                    ", cltADD1, cltADD2, cltAmount, cltBackFee, cltBIWEEKUA, cltBOTTLES, cltBULK, cltCHANGEUSER, cltCity, cltCONTTXDT" +
                                    ", cltCounselor, cltCounty, cltcredit, cltDOB, cltDOW1, cltDOW2, cltdtadded, cltdtLastUA, cltEducation, cltemail" +
                                    ", cltEmployer, cltEmpStatus, cltENROLLdt, cltETH, cltEye, cltFingerPrint1, cltFingerPrint2, cltFName, cltFreq" +
                                    ", cltGender, cltH, cltHair, cltHolidayPickup, cltID, cltIncome, cltINS, cltLANG, cltLastBill, cltLName, cltM4ID" +
                                    ", cltMARRY, cltMedicaid, cltMI, cltNextBill, cltNOCENSUS, cltnursenote, cltOptIn, cltPANEL, cltPAYDAY, cltPhone" +
                                    ", cltPICPATH, cltpreg, cltPregEDC, cltProg, cltRace, cltREMARKS, cltRIN, cltRISK, cltSize, cltSPECIAL, cltSSN" +
                                    ", cltSTAND, cltState, cltStatus, cltSuffix, cltW, cltWorkPh, cltzip, ddapid, dtNextTP, dtPhysTB, dtuaweekly" +
                                    ", isSalesForceSync, Monthly, provclt, provcltID, repOldClt, salesForceId, LastModAt = GetDate()" +
                                    ", RowChkSum = CHECKSUM([cltID], [cltM4ID], [cltFName], [cltMI], [cltLName], [cltDOB], [cltGender], [cltSSN], [cltSize]" +
                                    ", [cltADD1], [cltADD2], [cltCity], [cltState], [cltzip], [cltPhone], [cltEmployer], [cltWorkPh], [cltIncome], [cltEducation]" +
                                    ", [cltHair], [cltEye], [cltH], [cltW], [cltRace], [cltpreg], [cltLANG], [cltMARRY], [cltemail], [cltEmpStatus], [cltPregEDC]" +
                                    ", [cltSuffix], [cltCounty], cltCounselor, cltCHANGEUSER, isSalesForceSync, salesForceId, cltLastBill, cltFreq, cltProg" +
                                    ", cltMedicaid, cltAmount) from dbo.tblClient order by 1, cltID";
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.clientdemo", st.SiteCode, st.WorkDate.Value, null);
                                break;
                            case "pats.tbl_clientdemo1":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveClientDemo1var(SrcDt, st.SiteCode, st.ActionKey, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        // Run Table Counts
                                        strCmd = "Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw;
                                        if (st.FromTblVw == "vw_Clients")
                                        {
                                            strCmd += " where AddressType = 2 ";
                                        }
                                        if (st.FromTblVw == "ClientDemo")
                                        {
                                            strCmd += " where SiteCode = '" + st.SiteCode + "' and Patient_UID is not null";
                                        }
                                        DataTable ltbl = sm.GetTableData("lrc", strCmd, st.ConStr);
                                        DataTable atbl = sm.GetTableData("arc", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode
                                            + "' and RowState = 1", sm.ConnectionString);
                                        BHG_DR_LIB.Models.RCodes rc1 =
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, int.Parse(ltbl.Rows[0][0].ToString()), int.Parse(atbl.Rows[0][0].ToString()), null);
                                        if (!rc1.IsResult)
                                        {
                                            Console.WriteLine(rc1.ExceptMsg.ToString());
                                        }
                                    }
                                }
                                break;
                            case "pats.tbl_clientdemo2":
                                if (st.ActionKey == 3)
                                {
                                    //x = SaveClientDemo3(SrcDt, st.SiteCode);
                                }
                                else
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveClientDemo2(SrcDt, st.SiteCode, st.ActionKey, null);
                                    //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                }
                                break;
                            case "pats.tbl_clinicalopiatewithdrawalscale":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                if (st.SiteCode == "PHC")
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;");
                                }
                                else
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                }
                                rCodes = sd.SaveGlobalClinicalOpiateWithdrawalScale(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_cows_v6":
                                //Check if Forms table exists
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where upper(name) = 'SF_COWS'", st.ConStr);
                                // if yes then
                                if (SrcDt.Rows.Count == 1)
                                {
                                    DataTable tblCols = sm.GetTableData("Cols", "select name, column_id from sys.all_columns c where c.object_id = (select object_id from sys.tables where upper(name) = 'SF_COWS')", st.ConStr);
                                    List<BHG_DR_LIB.Models.TblCols> lstCols = new List<BHG_DR_LIB.Models.TblCols>();
                                    foreach (DataRow r in tblCols.Rows)
                                    {
                                        lstCols.Add(new BHG_DR_LIB.Models.TblCols
                                        {
                                            ColName = r["name"].ToString(),
                                            ColID = int.Parse(r["Column_id"].ToString())
                                        }
                                        );
                                    }

                                    strCmd = "select SiteCode = '" + st.SiteCode + "', COWID = c.id, CltID = p.PatientID, c.preadmissionid, dttime ";
                                    if (lstCols.Where(x => x.ColName.ToLower() == "reasonforthisAssessment").FirstOrDefault() != null)
                                    { strCmd += ", reasonforthisAssessment"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "ddlreasonforthisassessment").FirstOrDefault() != null)
                                    { strCmd += ", DDLreasonforthisAssessment"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "pulserate").FirstOrDefault() != null)
                                    { strCmd += " , isnull(c.RestingPulseRate, c.PulseRate) RestingPulseRate"; }
                                    else { strCmd += " , c.RestingPulseRate"; }
                                    strCmd += Environment.NewLine + ", drp.dropdownlistitem as RestingPulseRatedesc";
                                    if (lstCols.Where(x => x.ColName.ToLower() == "upsetgi").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.giupset, c.upsetGI) giupset"; } else { strCmd += ", c.giupset"; }
                                    strCmd += ", dgi.dropdownlistitem as GIUpsetdesc" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "sweat").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.sweating, c.Sweat) sweating"; } else { strCmd += ", c.sweating"; }
                                    strCmd += ", dsw.dropdownlistitem as Sweatingdesc";
                                    if (lstCols.Where(x => x.ColName.ToLower() == "tremorhand").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.tremor, c.TremorHand) tremor"; } else { strCmd += ", c.tremor"; }
                                    strCmd += ", dt.dropdownlistitem as Tremordesc" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "restless").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.Restlessness, c.Restless) Restlessness"; } else { strCmd += ", c.Restlessness"; }
                                    strCmd += ", dr.dropdownlistitem as Restlessnessdesc";
                                    if (lstCols.Where(x => x.ColName.ToLower() == "yawn").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.Yawning, c.Yawn) Yawning"; } else { strCmd += ", c.Yawning"; }
                                    strCmd += ", dy.dropdownlistitem as Yawningdec" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "pupil").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.PupilSize, c.Pupil) PupilSize"; } else { strCmd += ", c.PupilSize"; }
                                    strCmd += ", dps.dropdownlistitem as PupilSizedesc";
                                    if (lstCols.Where(x => x.ColName.ToLower() == "anxiety").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.AnxietyOrIrritability, c.Anxiety) AnxietyOrIrritability"; } else { strCmd += ", c.AnxietyOrIrritability"; }
                                    strCmd += ", doi.dropdownlistitem as AnxietyOrIrritabilitydesc" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "bonejointache").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.BoneOrJointAches, c.BoneJointAche) BoneOrJointAches"; } else { strCmd += ", c.BoneOrJointAches"; }
                                    strCmd += ", dbj.dropdownlistitem as BoneOrJointAchesdesc";
                                    if (lstCols.Where(x => x.ColName.ToLower() == "gooseflesh").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.GoosefleshSkin, c.GooseFlesh) GoosefleshSkin"; } else { strCmd += ", c.GoosefleshSkin"; }
                                    strCmd += ", dgf.dropdownlistitem as GoosefleshSkindesc" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "runnynose").FirstOrDefault() != null)
                                    { strCmd += ", isnull(c.RunnyNoseOrTearing, c.RunnyNose) RunnyNoseOrTearing"; } else { strCmd += ", c.RunnyNoseOrTearing"; }
                                    strCmd += ", drnt.dropdownlistitem as RunnyNoseOrTearingdesc, c.CompletedBy, c.CreatedOn, c.CreatedBy, c.UpdatedBy, c.UpdatedOn" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "isactive").FirstOrDefault() != null) { strCmd += ", c.IsActive"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "patientsignature").FirstOrDefault() != null) { strCmd += ", c.PatientSignature"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "isdeleted").FirstOrDefault() != null) { strCmd += ", c.IsDeleted"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "clientsignaturedate").FirstOrDefault() != null) { strCmd += ", c.ClientSignatureDate"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "version").FirstOrDefault() != null) { strCmd += ", c.Version"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "staffnamesignature").FirstOrDefault() != null) { strCmd += ", c.staffNameSignature"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "staffsignatureby").FirstOrDefault() != null) { strCmd += ", c.staffsignatureby"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "staffsignaturedate").FirstOrDefault() != null) { strCmd += ", c.staffsignaturedate"; }
                                    strCmd += Environment.NewLine +
                                        ", RowChkSum = CHECKSUM(c.id, p.PatientID, c.preadmissionid, dttime, c.RestingPulseRate, c.giupset, c.sweating, c.tremor, c.Restlessness " + Environment.NewLine +
                                        ", c.Yawning, c.PupilSize, c.AnxietyOrIrritability, c.BoneOrJointAches, c.GoosefleshSkin, c.RunnyNoseOrTearing, c.CreatedOn, c.UpdatedOn)" + Environment.NewLine +
                                        " from SF_Cows c left join DroDownListItems drp on c.RestingPulseRate = drp.Id " + Environment.NewLine +
                                        " left join DroDownListItems dgi on c.GIUpset = dgi.Id " + Environment.NewLine +
                                        " left join DroDownListItems dsw on c.Sweating = dsw.Id " + Environment.NewLine +
                                        " left join DroDownListItems dt on c.Tremor = dt.Id " + Environment.NewLine +
                                        " left join DroDownListItems dr on c.restlessness = dr.Id " + Environment.NewLine +
                                        " left join DroDownListItems dy on c.Yawning = dy.Id " + Environment.NewLine +
                                        " left join DroDownListItems dps on c.PupilSize = dps.Id " + Environment.NewLine +
                                        " left join DroDownListItems doi on c.AnxietyOrIrritability = doi.Id " + Environment.NewLine +
                                        " left join DroDownListItems dbj on c.BoneOrJointAches = dbj.Id " + Environment.NewLine +
                                        " left join DroDownListItems dgf on c.GoosefleshSkin = dgf.Id " + Environment.NewLine +
                                        " left join DroDownListItems drnt on c.RunnyNoseOrTearing = drnt.Id " + Environment.NewLine +
                                        " left join SF_PatientPreAdmission p on c.PreAdmissionId = p.ID " + Environment.NewLine +
                                        //" --left join dbo.tblClient clt on(p.PatientID = clt.cltID) " + Environment.NewLine +
                                        " order by 3, 2";
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveCows_v6(SrcDt, st.SiteCode, null);
                                }
                                else { rCodes.IsResult = true; rCodes.RowsProcessed = 0; }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_codes":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveCodes(SrcDt, st.SiteCode, null);

                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_customquestions":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveCustomQuestions(SrcDt, st.SiteCode, null);
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_customanswers":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveCustomAnswers(SrcDt, st.SiteCode, null);
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_dartssrv":
                                int offsetvalue = -15;
                                if (st.WorkDate.Value.DayOfWeek == DayOfWeek.Friday)
                                {
                                    if (st.WorkDate.Value.Month == st.WorkDate.Value.AddDays(1).Month)
                                    {
                                        offsetvalue = -90;
                                        if (st.WorkDate.Value.Date == DateTime.Parse("1/24/2025"))
                                        {
                                            offsetvalue = -200;
                                        }
                                    }
                                }
                                DateTime DartsDate = st.WorkDate.Value.AddDays(offsetvalue);
                                // Remove ServerType if it doesn't exist.
                                DataTable tblDartcols = sm.GetTableData("tcols", "select name, column_id from sys.all_columns c where c.object_id = " +
                                 "(select object_id from sys.all_objects where upper(name) = 'TBLDARTSSRV') and name = 'ServiceType'", st.ConStr);
                                if ((tblDartcols.Rows.Count == 0))
                                {
                                    strCmd = strCmd.Replace(", [ServiceType] ServiceType", "").Replace(", [ServiceType]", "");
                                
                                }
                                // Remove Holdid
                                tblDartcols = sm.GetTableData("tcols", "select name, column_id from sys.all_columns c where c.object_id = " +
                                 "(select object_id from sys.all_objects where upper(name) = 'TBLDARTSSRV') and name = 'HoldId'", st.ConStr);
                                if ((tblDartcols.Rows.Count == 0))
                                {
                                    strCmd = strCmd.Replace(", [HoldId] HoldId", "").Replace(", [HoldId]", "");
                                }
                                //strCmd += " Where Year(dsdtstart) = 2021 order by 1, 2";
                                strCmd += " Where dsClt is not null and (convert(date,dsdtstart) >= '" + DartsDate.ToShortDateString()
                                    + "' or convert(date,dsDtAdded) >= '" + DartsDate.ToShortDateString()
                                    + "' or convert(date,dsUpdate) >= '" + DartsDate.ToShortDateString()
                                    + "' or convert(date,dsBilled) >= '" + DartsDate.ToShortDateString()
                                    + "' or convert(date,dsSigDate) >= '" + DartsDate.ToShortDateString()
                                    + "' or dsClt <= 0) order by 1, 2";
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, DartsDate, null);
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, 
                                            int.Parse(sm.GetTableData("tbllcl", "select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " where dsClt > 0 " +
                                            " and (dsDtAdded > '12/31/2018' or dsDtStart = '12/31/2018')", 
                                            st.ConStr).Rows[0][0].ToString()), 
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from pats.vw_DartsSrv where SiteCode = '" + st.SiteCode + "'", 
                                              sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_dose":
                                // || (st.SiteCode == "VMIN")
                                if ((st.SiteCode == "V10A") || (st.SiteCode == "CBCO") || (st.SiteCode == "V21") || (st.SiteCode == "V10"))
                                {
                                    strWhere = "(Year(dtDate) >= " + st.WorkDate.Value.AddDays(DaysBack).AddYears(-1).Year
                                        + " or Year(dtMedDate) >= " + +st.WorkDate.Value.AddDays(DaysBack).AddYears(-1).Year + ")"
                                        + " and dtDate <= '" + st.WorkDate.Value.AddDays(2).ToShortDateString()
                                        + "' and CltId is not null and dtDate >= '" + st.WorkDate.Value.AddMonths(-1).ToShortDateString() + "'";
                                    if (st.Reload.Value)
                                    {
                                        strCmd += " Where CltID is not null and dtMedDate is not null " + st.SortOrder;
                                    }
                                    else
                                    {
                                        strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    }
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveDoses(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), st.Reload.Value, null);
                                }
                                else
                                {
                                    if (st.Reload.Value)
                                    {
                                        BHG_DR_LIB.SQLSvrManager sqlm = new BHG_DR_LIB.SQLSvrManager();
                                        _ = sqlm.ExeSqlCmd("delete from [pats].[tbl_dose] where SiteCode = '" + st.SiteCode + "'", sqlm.ConnectionString);
                                        strCmd += " Where CltID is not null and dtMedDate is not null " + st.SortOrder;
                                        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                    }
                                    else
                                    {
                                        strWhere = "(Year(dtDate) >= " + st.WorkDate.Value.AddDays(DaysBack).AddYears(-1).Year
                                            + " or Year(dtMedDate) >= " + +st.WorkDate.Value.AddDays(DaysBack).AddYears(-1).Year + ")"
                                            + " and dtDate <= '" + st.WorkDate.Value.AddDays(2).ToShortDateString()
                                            + "' and CltId is not null "
                                            + " and dtDate >= '" + st.WorkDate.Value.AddMonths(-6).ToShortDateString() + "'";
                                        strCmd += " Where " + strWhere + " " + st.SortOrder;
                                        SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                    }
                                }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName,
                                            int.Parse(sm.GetTableData("tbllcl", "select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " Where CltID is not null and dtMedDate is not null ",
                                            st.ConStr).Rows[0][0].ToString()),
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode + "' and RowState = 1",
                                              sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_dose_excuse":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes.IsResult = sd.SaveDoseExcuse(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                //rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, null);
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName,
                                            int.Parse(sm.GetTableData("tbllcl", "select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " where CltID > 0",
                                            st.ConStr).Rows[0][0].ToString()),
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode + "' and RowState = 1",
                                              sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_eandmformmdm":
                                strCmd = "SELECT SiteCode = '" + st.SiteCode + "', a.Id, a.PreAdmissionId, a.ClientId, a.DataFormId, a.CreatedBy\r\n"
                                    + ", a.CreatedOn, a.ModifiedBy, a.ModifiedOn, a.FormDate, a.ServiceId\r\n"
                                    + ", a.Context, a.[Version], c.MEdicalProviderSignatureDate, c.MEdicalProviderSignatureBy\r\n"
                                    + ", IsDeleted = case when a.Isdeleted = 1 or b.IsDeleted = 1 then 1 else 0 end \r\n"
                                    + " FROM [dbo].[EandMForm] a left join[dbo].[EandMFormMDM] c on (a.ID = c.EandMFormID) "
                                    + " inner join SF_PatientPreAdmission b on(a.PreAdmissionID = b.ID) ";
                                // + "\r\n Where a.CreatedOn >= '" + st.WorkDate.Value.AddDays(DaysBack)
                                // + "' or a.ModifiedOn >= '" + st.WorkDate.Value.AddDays(DaysBack) + "' or a.FormDate >= '" 
                                // + st.WorkDate.Value.AddDays(DaysBack) + "'";
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveEMFormMDM(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_eandmformpregnancy":
                                strCmd = "SELECT SiteCode = '" + st.SiteCode + "', a.ClientId, a.DataFormId, a.CreatedBy\r\n"
                                    + ", a.CreatedOn, a.ModifiedBy, a.ModifiedOn, a.FormDate, a.ServiceId\r\n"
                                    + ", a.Context, a.[Version], c.*\r\n"
                                    + ", IsDeleted = case when a.Isdeleted = 1 or b.IsDeleted = 1 then 1 else 0 end \r\n"
                                    + " FROM [dbo].[EandMForm] a inner join[dbo].[EandMFormPregnancy] c on (a.ID = c.EandMFormID) "
                                    + " inner join SF_PatientPreAdmission b on(a.PreAdmissionID = b.ID) ";
                                // + "\r\n Where a.CreatedOn >= '" + st.WorkDate.Value.AddDays(DaysBack)
                                // + "' or a.ModifiedOn >= '" + st.WorkDate.Value.AddDays(DaysBack) + "' or a.FormDate >= '" 
                                // + st.WorkDate.Value.AddDays(DaysBack) + "'";
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveEMFormPregnancy(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_enrollment":
                                if (st.SiteCode != "Lab")
                                {
                                    if (st.FromTblVw == "vw_Enrollment")
                                    {
                                        DataTable tblcols = sm.GetTableData("tcols", "select name, column_id from sys.all_columns c where c.object_id = " +
                                            "(select object_id from sys.all_views where upper(name) = 'VW_ENROLLMENT') and name = 'Modality'", st.ConStr);
                                    if ((tblcols.Rows.Count > 0) || (st.SiteCode == "CBNC"))
                                        {
                                            strCmd = "Select " + strFlds + ", Modality from " + st.SrcSchema + "." + st.FromTblVw;
                                            Console.WriteLine("Modality included in Site " + st.SiteCode.ToString());
                                        }
                                    }
                                    DataTable tblNRollcols = sm.GetTableData("tcols", "select name, column_id from sys.all_columns c where c.object_id = " +
                                        "(select object_id from sys.all_objects where upper(name) = '" + st.FromTblVw.ToString().ToUpper() 
                                        + "') and name = 'TreatmentLevel'", st.ConStr);
                                    if ((tblNRollcols.Rows.Count == 0))
                                    {
                                        strCmd = strCmd.Replace(", [TreatmentLevel] TreatmentLevel", "").Replace(", [TreatmentLevel]", "");
                                    }

                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null);
                                    //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                    if (st.RowTrax.HasValue)
                                    {
                                        if (st.RowTrax.Value)
                                        {
                                            int aztblcnt = int.Parse((sm.GetTableData("aztbl", "Select tblcnt = count(1) from " + st.TaskName + " Where SiteCode = '" +
                                                st.SiteCode + "' and RowState = 1", sm.ConnectionString)).Rows[0][0].ToString());
                                            sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, SrcDt.Rows.Count, aztblcnt, null);
                                        }
                                    }
                                }
                                else
                                { rCodes.IsResult = true; }
                                break;
                            case "pats.tbl_feesched":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveFeeSchedules(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_fmp":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveFmp(SrcDt, st.SiteCode, st.WorkDate.Value.Date, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_formssammsclient":
                                // Purge Forms SAMMS
                                if (st.SiteCode != "PHC")
                                {
                                    string scmd = "select fscsid, f.fscdate, fsccltid, f.fscsite, count(1) cnt from tblFormsSAMMSClient f " +
                                            //" inner join tblSites s on (f.fscsite = s.sID)" +
                                            "where fscsite not in (25, 100) " +  //fscCLTID < 0 and 
                                            "group by fscsid, f.fscDATE, fscCLTID, f.fscSite having f.fscDATE >= '1/1/2021' order by 1, 2, 3, 4";
                                    // Add code to pull connection string from table.
                                    //@"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;"
                                    string mydb = @"Data Source=BHGDALLSQL05\SQL2016SAMMS;Initial Catalog=SAMMSGLOBAL;Connection Timeout=60;Integrated Security=True;Encrypt=False";
                                    _ = sd.SaveFormCounts(sm.GetTableData("frms", scmd, mydb), null);
                                }
                                //SrcDt = sm.GetTableData("frms", scmd, @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;");
                                //if (SrcDt.Rows.Count > 0)
                                //{
                                //    sw.WriteLine("Site: " + st.SiteCode + "     Purge    Rows: " + SrcDt.Rows.Count.ToString());
                                //    bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.DsnTbl, st.SiteCode, null);
                                //}
                                //string[] Years = new string[] { "2020", "2021" };
                                //string[] Months = new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12" };
                                //foreach (string s in Years)
                                //{
                                //    foreach (string m in Months)
                                //    {
                                //        if (DateTime.Parse(m + "/1/" + s) <= DateTime.Today)
                                //        {
                                //            string strPurgeCmd = strCmd + " Where fscDATE > '12/31/2019' and fscsite not in (25, 100) and fscCLTID < 0 and Year(fscDate) = "
                                //                + s + " and Month(fscDATE) = " + m;
                                //            SrcDt = sm.GetTableData(st.FromTblVw, strPurgeCmd, st.ConStr);
                                //            if (SrcDt.Rows.Count > 0)
                                //            {
                                //                sw.WriteLine("Site: " + st.SiteCode + "     Purge    Rows: " + SrcDt.Rows.Count.ToString());
                                //                bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.DsnTbl, st.SiteCode, null);
                                //            }
                                //        }
                                //    }
                                //}
                                // Daily Runs
                                // Modified 3/24/2023 - BSC
                                //for (DateTime rundt = st.WorkDate.Value.Date.AddDays(-14); rundt <= st.WorkDate.Value.Date; rundt = rundt.AddDays(1))
                                //{
                                //    {
                                //        strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 100) " +
                                //            " and (convert(date,fscDATE) >= @dt or convert(date,clientSigDate) = @dt or convert(date,supervisorSigDate) = @dt " +
                                //            " or convert(date,physicianSigDate) = @dt or convert(date,nurseSigDate) = @dt or convert(date,doctextEditDate) = @dt " +
                                //            " or convert(date,GuardianSigDate) = @dt or convert(date,AdminnurseSigDate) = @dt or convert(date,staffSigDate) = @dt)";
                                //        string strCmd2 = "declare @dt date = '" + rundt.ToShortDateString() + "'; " + strCmd;
                                //        strCmd2 += strWhere + " order by 1, 2";
                                //        SrcDt = sm.GetTableData(st.FromTblVw, strCmd2, st.ConStr);
                                //        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, null);
                                //        //ts = DateTime.Now.Subtract(fsTsk.RunAt);
                                //        //fsTsk.Duration = ts.Hours.ToString().PadLeft(2, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');
                                //        //fsTsk.Status = xrefs.Where(x => x.Code == "Completed" && x.CodeType == "Status").First().Xref;
                                //        //fsTsk.RowCount = SrcDt.Rows.Count;
                                //        //ptsk.RowCount += fsTsk.RowCount;
                                //        //db.SaveChanges();
                                //    }
                                //    //rundt = rundt.AddDays(1);
                                //}
                                if (st.SiteCode == "PHC")
                                {
                                    strWhere = " Where fscDATE > '12/31/2019' and fscsite = 1 ";
                                    // Do we need the Clients < 0 ?
                                    strCmd += strWhere + " order by 1, 2";
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;");
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_formssammsclient", st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                    //rCodes = sd.SaveGlobalFormsSAMMSClients(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null);
                                }
                                else
                                {
                                    strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 38, 99, 100, 106, 115, 118) ";
                                    // Do we need the Clients < 0 ?
                                    strCmd += strWhere + " order by 1, 2";
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_formssammsclient", st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                //strCmd = "update pats.tbl_FormsSAMMSClient set SiteCode = l.SiteCode --, RowState = case when fscCltid < 0 then 0 else 1 end " +
                                //         " from pats.tbl_FormsSAMMSClient left join ctrl.tbl_Locations l on pats.tbl_FormsSAMMSClient.fscsite = l.sID " +
                                //         " where l.SiteCode is not null and pats.tbl_FormsSAMMSClient.SiteCode = 'Global'";
                                //_ = sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_globalpayor":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveGlobalPayer(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_orders":
                                rCodes.IsResult = true;
                                bool x = true;
                                strCmd += " Where " + st.WhereCondition;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                int cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2016).Count();
                                if (cnt > 0)
                                {
                                    x = sd.SaveOrders2016(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("Orderdate").Year == 2016).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2016"), null);
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2017).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2017(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2017).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2017"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2018).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2018(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2018).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2018"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2019).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2019(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2019).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2019"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2020).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2020(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2020).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2020"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2021).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2021(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2021).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2021"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2022).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2022(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2022).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2022"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2023).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2023(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2023).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2023"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2024).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2024(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2024).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2024"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2025).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2025(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2025).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2025"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2026).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2026(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2026).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2025"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2027).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2027(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2027).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2025"), null);
                                    }
                                }
                                if (x)
                                {
                                    cnt = SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2028).Count();
                                    if (cnt > 0)
                                    {
                                        x = sd.SaveOrders2028(SrcDt.AsEnumerable().Where(x => x.Field<DateTime>("OrderDate").Year == 2028).CopyToDataTable(), st.SiteCode, DateTime.Parse("12/31/2025"), null);
                                    }
                                }
                                rCodes.IsResult = x;
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.ActionKey == 1))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        //remove negitive Client Id from count.
                                        string strwhere = " where OrderDate is not null and cltID > 0 ";
                                        //if ((st.SiteCode == "WBY") || (st.SiteCode == "VMIN") V11, V10A, V10, CBCO, B12B)
                                        //{ strwhere = " where OrderDate is not null and CltId > 0 "; }
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, int.Parse(sm.GetTableData("lcltbl", "select count(1) from " + 
                                            st.SrcSchema + "." + st.FromTblVw + strwhere, st.ConStr).Rows[0][0].ToString()), 
                                            int.Parse((sm.GetTableData("aztbl", "Select tblcnt = count(1) from pats.vw_Orders Where SiteCode = '" +
                                                st.SiteCode + "'", sm.ConnectionString)).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_payerclient":
                                if (st.Reload.HasValue)
                                {
                                    if (st.Reload.Value == true)
                                    {
                                        //Skip where condition
                                    }
                                    else
                                    {
                                        strCmd += " Where " + st.WhereCondition.Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
                                        .Replace("@SiteCode", "'" + st.SiteCode + "'");
                                    }
                                }
                                else
                                {
                                    strCmd += " Where " + st.WhereCondition.Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
                                    .Replace("@SiteCode", "'" + st.SiteCode + "'");
                                }
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if (st.FromTblVw == "vw_PayerClt_INACTIVE")
                                {
                                    rCodes = sd.RemovePayerClients(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null);
                                }
                                else
                                {
                                    rCodes = sd.SavePayerClient(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null);
                                }
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.FromTblVw != "vw_PayerClt_INACTIVE"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, 
                                            int.Parse(sm.GetTableData("lcltbl", "Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw, st.ConStr).Rows[0][0].ToString()),
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode + "' ", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_pbi3payauth":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAuths(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_preadmissionreferralsource":
                                // Added 9-12-2025
                                DataTable RefSrctbl = sm.GetTableData("RefSrc",
                                    "select object_id from sys.all_objects where upper(name) = '" + st.FromTblVw.ToString().ToUpper() + "'",
                                    st.ConStr);
                                if (RefSrctbl.Rows.Count == 1)
                                {
                                    int mydaysback = DaysBack - 500;
                                    strCmd += " Where " + st.WhereCondition.Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(mydaysback).ToShortDateString() + "'")
                                    .Replace("@SiteCode", "'" + st.SiteCode + "'");
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    if (SrcDt.Rows.Count > 0)
                                    {
                                        rCodes = sd.SavePreAdminReferrals(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(mydaysback), null);
                                    }
                                    else { rCodes.IsResult = true; }
                                }
                                else { rCodes.IsResult = true; }
                                break;
                            case "pats.tbl_services":
                                strCmd += " Where " + st.WhereCondition;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if (SrcDt.Rows.Count > 0)
                                {
                                    rCodes = sd.SaveServices(SrcDt, st.SiteCode, null);
                                }
                                else { rCodes.IsResult = true; }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_uasched":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblUASched'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd = strCmd.Replace("Select ", "Select distinct ");
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveUASched(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                    if (st.RowTrax.HasValue)
                                    {
                                        if (st.RowTrax.Value)
                                        {
                                            sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, SrcDt.Rows.Count,
                                                //int.Parse(sm.GetTableData("lcltbl", "Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw, st.ConStr).Rows[0][0].ToString()),
                                                int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode + "' ", sm.ConnectionString).Rows[0][0].ToString()), null);
                                        }
                                    }
                                }
                                else
                                { rCodes.ExceptMsg = "Table does not exists."; }
                                break;
                            case "pats.tbl_labresultdetail":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                //rCodes = sd.SaveUAResultDetail(SrcDt, st.SiteCode, st.WorkDate.Value, null);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.Date, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_labresult":
                                if (st.SiteCode.ToUpper() != "LAB")
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveLABResults(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null);
                                }
                                else
                                { rCodes.IsResult = true; }
                                break;
                            case "pats.tbl_uaresultdetail":
                                if (st.WorkDate.Value.ToShortDateString() == "2/10/2026")
                                {
                                    strCmd += " Where 1 = 1 " + st.SortOrder;
                                }
                                else
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                }
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                //rCodes = sd.SaveUAResultDetail(SrcDt, st.SiteCode, st.WorkDate.Value, null);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.Date, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_uaresults":
                                bool reload = false;
                                if (st.Reload.HasValue)
                                {
                                    if (st.Reload.Value) { reload = st.Reload.Value; }
                                }
                                if (reload)
                                {
                                    strCmd += " Where uarresultdt is not null " + st.SortOrder;
                                }
                                else
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                }
                                if (st.SiteCode.ToLower() == "lab")
                                {
                                    strCmd = strCmd.Replace(", [LabName] LabName", "").Replace(", [LabName]", "");
                                    strCmd = strCmd.Replace(", [UAEval] UAEval", "").Replace(", [UAEval]", "");
                                }
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveUAResults(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), reload, null);
                                //rCodes = sd.SaveUAResults(SrcDt, st.SiteCode, st.WorkDate.Value, false, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName,
                                            int.Parse(sm.GetTableData("lcltbl", "Select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw, st.ConStr).Rows[0][0].ToString()),
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" + st.SiteCode + "' ", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_dbo_formquestionanswers":
                                //Check if Forms table exists  Mod: 08/22/2023
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'Form'", st.ConStr);
                                // if yes then
                                if (SrcDt.Rows.Count == 1)
                                {
                                    int formDaysBack = DaysBack - 15;
                                    DateTime wrkdt = st.WorkDate.Value.AddDays(formDaysBack).Date;
                                    if (st.Reload.HasValue)
                                    {
                                        if (st.Reload.Value)
                                        {
                                            wrkdt = DateTime.Parse("1/1/2010");
                                        }
                                    }
                                    strCmd = "select SiteCode, FormName, convert(varchar(100),FormId) FormId, PreAdmissionId, ClientId, QuestionId " +
                                        ", QuestionOrderId = isnull(x.QuestionOrderId, Row_Number() over(Partition by x.FormName, x.FormId, x.ClientId, x.QuestionId order by x.QuestionId, x.AnswerSeq)) " +
                                        ", QuestionText, OptionId, AnswerValue, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (select SiteCode = '" + st.SiteCode.ToString() +
                                        "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId" +
                                        "\r\n, IsDeleted = case when isnull(f.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end" +
                                        ", QuestionId = isnull(q.Id, 0), QuestionOrderId = q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value, AnswerSeq = a.Id " +
                                        "\r\n from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) left join Question q on (ft.Id = q.FormTemplateId)" +
                                        "\r\n left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId) inner join [dbo].[SF_PatientPreAdmission] pa on (f.PreAdmissionId = pa.ID)" +
                                        "\r\n left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)" +
                                        "\r\n where a.Value is not null and (f.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() + "') " +
                                        "\r\n union select SiteCode = '" + st.SiteCode.ToString() + "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId" +
                                        "\r\n, IsDeleted = case when isnull(f.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end" +
                                        "\r\n , QuestionId = isnull(q.Id, 0), QuestionOrderId = q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value, AnswerSeq = a.Id " +
                                        "\r\n from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) left join Question q on (ft.Id = q.FormTemplateId)" +
                                        "\r\n left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId) inner join [dbo].[SF_PatientPreAdmission] pa on (f.PreAdmissionId = pa.ID)" +
                                        "\r\n left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)" +
                                        "\r\n where q.Id is null and (f.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() + "')) x ";

                                    #region Dead Code
                                    ////" --Suicide Severity Rating Scale --(For ETL’ing to pats.tbl_dbo_FormQuestionAnswers) " Mod: 08/22/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SuicideSeverityRatingScale'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by w.FormName, w.FormId, w.ClientId, w.QuestionId order by w.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Suicide Severity Rating Scale' as [FormName] " +
                                    //    ", '1-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date,c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date,c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    " from [dbo].[SuicideSeverityRatingScale] c " +
                                    //    " left join SF_PatientPreAdmission pa on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Suicide Severity Rating Scale') w ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////" --Suicide Severity Rating Scale 2.0 --(For ETL’ing to pats.tbl_dbo_FormQuestionAnswers) " Mod: 08/23/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SAFETProtocolwithCSSRS'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by w.FormName, w.FormId, w.ClientId, w.QuestionId order by w.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Suicide Severity Rating Scale 2.0' as [FormName] " +
                                    //    ", '11-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    " from [dbo].[SAFETProtocolwithCSSRS] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Suicide Severity Rating Scale 2.0') w ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --Health Questionnaire " Mod: 08/23/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'HealthQuestionnaire'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by y.FormName, y.FormId, y.ClientId, y.QuestionId order by y.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Health Questionnaire' as [FormName] " +
                                    //    ", '2-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID]  " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.Createddate) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.Modifieddate) as [UpdatedOn], c.Isdeleted " +
                                    //    " from [dbo].[HealthQuestionnaire] c) y ";
                                    //    //" left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    //" left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    //" where df.FormName = 'Health Questionnaire') y ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //"  --Infectious Disease And Behavioral Screen " Mod: 8/24/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InfectiousDiseaseAndBehavioralScreen'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by z.FormName, z.FormId, z.ClientId, z.QuestionId order by z.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Infectious Disease And Behavioral Screen' as [FormName] " +
                                    //    ", '3-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "  from [dbo].[InfectiousDiseaseAndBehavioralScreen] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Infectious Disease And Behavioral Screen') z ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Consent to Treatment with an Approved Narcotic' " Modified: 8/16/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsentToTreatmentWithAnApprovedNarcotic'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by q.FormName, q.FormId, q.ClientId, q.QuestionId order by q.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Consent to Treatment with an Approved Narcotic'  as [FormName] " +
                                    //    ", '4-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "  from [dbo].[ConsentToTreatmentWithAnApprovedNarcotic] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Consent to Treatment with an Approved Narcotic' ) q";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Financial Hardship Application'  "  Modified: 8/16/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'FinancialHardshipApplication'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, cltId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by s.FormName, s.FormId, s.cltId, s.QuestionId order by s.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Financial Hardship Application'   as [FormName] " +
                                    //    ", '5-' + convert(varchar, c.cltId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.cltId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "  from [dbo].[FinancialHardshipApplication] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.cltId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.cltId = df.PatientId " +
                                    //    " where df.FormName = 'Financial Hardship Application') s";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Comprehensive Assessment Form'  " Modified: 10/03/2023, 10/11/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ComprehensiveAssessmentForm'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by t.FormName, t.FormId, t.ClientId, t.QuestionId order by t.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Comprehensive Assessment Form' as [FormName] " +
                                    //    ", '6-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn] " +
                                    //    ", isnull((select df.Isdeleted from SF_PatientPreAdmission pa inner join SF_DataForms df on pa.DataFormId = df.Id and pa.PatientId = df.PatientId " +
                                    //    " where df.FormName in ('Comprehensive Assessment', 'Comprehensive Assessment Form') and c.DataFormId = abs(pa.DataFormId) and " +
                                    //    " c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID), c.IsDeleted) Isdeleted " +
                                    //    "  from [dbo].[ComprehensiveAssessmentForm] c) t";
                                    //    //" left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    //" left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    //" where df.FormName in ('Comprehensive Assessment', 'Comprehensive Assessment Form')) t";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Admission Assessment' " Mod: 8/24/2023 9/13/2023, 10/11/2023, 
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'AdmissionAssessment'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName] " +
                                    //    ", '7-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.PreAdmissionId, c.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn] " +
                                    //    ", isnull((select df.Isdeleted from SF_PatientPreAdmission pa inner join SF_DataForms df on abs(pa.DataFormId) = df.Id and pa.PatientID = df.PatientId " +
                                    //    "  where df.FormName = 'Admission Assessment' and c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID), c.IsDeleted) IsDeleted " +
                                    //    "  from [dbo].[AdmissionAssessment] c) v ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Treatment Plan' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblTP17REVIEW'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //        ", QuestionOrderID = Row_Number() over(Partition by tp.FormName, tp.FormId, tp.ClientId, tp.QuestionId order by tp.QuestionId) " +
                                    //        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from ( " +
                                    //        "Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //        ", '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID] " +
                                    //        ", TprTYPE as AnswerValue, tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                    //        ", Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW " +
                                    //        " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //        ", '8-2-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 2 as QuestionID, 1 as QuestionOrderID, 'Treatment Phase Type' as QuestionText, null as [OptionID] " +
                                    //        ", tpTreatmentPhase as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                    //        ", Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW " +
                                    //        " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //        ", '8-3-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 3 as QuestionID, 1 as QuestionOrderID, 'Next Due' as QuestionText, null as [OptionID] " +
                                    //        ", convert(varchar, tprNEXT) as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                    //        ", Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW ";
                                    //}
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select * from sys.all_columns c where c.object_id = (select object_id from sys.tables where name = 'tblTP17REVIEW') and name = 'tprReviewFrequency'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //    ", '8-4-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //    ", null PreAdmissionId, ABS(tprCLTID) ClientId, 4 as QuestionID, 1 as QuestionOrderID, 'Review Frequency' as QuestionText, null as [OptionID] " +
                                    //    ", case when Len(tprReviewFrequency) > 6 then rtrim(substring(tprReviewFrequency, 6, LEN(tprReviewFrequency) - 5)) else rtrim(tprReviewFrequency) end as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn] " +
                                    //    ", null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW  ) tp ";

                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //else { strCmd += ") tp "; }

                                    //// Level Justification
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblORDERREQ'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union Select SiteCode, [FormName], FormID = FormId + '-' + convert(varchar,Row_Number() " +
                                    //        " over (Partition by tp2.FormName, tp2.FormId, tp2.ClientId, tp2.QuestionId order by tp2.AnswerValue)) " + 
                                    //        ", PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over (Partition by tp2.FormName, tp2.FormId, tp2.ClientId, tp2.QuestionId order by tp2.AnswerValue) " + 
                                    //        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" + 
                                    //        " select SiteCode = '" + st.SiteCode.ToString() + "', FormName = 'Level Justification' " + 
                                    //        ", FormID = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ " + 
                                    //        ", PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 1, QuestionText = 'Effective Date', OptionID = 0, AnswerValue = convert(varchar,EffectiveDate, 101) " + 
                                    //        ", Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) " + 
                                    //        ", IsDeleted = Case when cltID < 0 then 1 else 0 end from [dbo].[tblORDERREQ] " + 
                                    //        " where status = 'Approved' and(Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST') " + 
                                    //        " union select SiteCode = '" + st.SiteCode.ToString() + "', FormName = 'Level Justification' " + 
                                    //        ", FormID = '9-2-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ " + 
                                    //        ", PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 2, QuestionText = 'Expiration Date', OptionID = 0, AnswerValue = convert(varchar,expirationdate, 101) " + 
                                    //        ", Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) " + 
                                    //        ", IsDeleted = Case when cltID< 0 then 1 else 0 end from [dbo].[tblORDERREQ] " + 
                                    //        " where status = 'Approved' and(Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST')) tp2";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'BHG Notice Of Privacy Practices' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblBHGNoticeOfPrivacyPractices'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'BHG Notice Of Privacy Practices' as [FormName] " +
                                    //    ", '10-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[tblBHGNoticeOfPrivacyPractices]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'KS Patient Rights and Responsibilities' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'KSPatientRightsResponsibilities'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'KS Patient Rights and Responsibilities' as [FormName] " +
                                    //    ", '12-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[KSPatientRightsResponsibilities]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'CO - Consent Central Registry Colorado' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsentCentralRegistryColorado'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'CO - Consent Central Registry Colorado' as [FormName] " +
                                    //    ", '13-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[ConsentCentralRegistryColorado]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'MN - Consent to Central Registry' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'GeneralConsentAuthforReleaseInfo'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Consent to Central Registry' as [FormName] " +
                                    //    ", '14-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[GeneralConsentAuthforReleaseInfo]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Adverse Childhood Experiences' Added 6/22/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'AdverseChildhood'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Adverse Childhood Experiences' as [FormName] " +
                                    //    ", '15-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    ", a.PreAdmissionId, ClientId = isnull(pa.PatientID, 0), 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    ", a.Createdby, convert(date, a.CreatedDate) as [CreatedOn], a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedDate) as [UpdatedOn], a.Isdeleted " +
                                    //    "  from [dbo].[AdverseChildhood] a left join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Patient Information sheet' Added 6/22/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'PatientInformationSheet'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Patient Information sheet' as [FormName] " +
                                    //    ", '16-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    ", PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    ", Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[PatientInformationSheet]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Insurance Benefit Verification' Added 6/22/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InsuranceBenefitVerification'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Insurance Benefit Verification' as [FormName] " +
                                    //    ", '17-' + convert(varchar, pa.PatientID) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    ", a.PreAdmissionId, ClientId = pa.PatientID, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    ", a.Createdby, convert(date, a.CreatedOn) as [CreatedOn], a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted " +
                                    //    "  from [dbo].[InsuranceBenefitVerification] a left join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'MN - Mental Health Informed Consent' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'MentalHealthInformedConsent'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Mental Health Informed Consent' as [FormName] " +
                                    //    ", '18-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[MentalHealthInformedConsent]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'State Fact Form' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'StateFactForm'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'State Fact Form' as [FormName] " +
                                    //    ", '19-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[StateFactForm]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  --'Initial Services Plan and Vulnerable Adult Determination' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InitialServicesPlanandVAD'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Initial Services Plan and Vulnerable Adult Determination' as [FormName] " +
                                    //    ", '20-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[InitialServicesPlanandVAD]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //// [dbo].[tblMAARC]   MN - Authorization for Release of Information to the MAARC
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblMAARC'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Authorization for Release of Information to the MAARC' as [FormName] " +
                                    //    ", '21-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedDate) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[tblMAARC]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //// [dbo].[tblDAANESNotification]  MN - DAANES Notification Form
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblDAANESNotification'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'MN - DAANES Notification Form' as [FormName] " +
                                    //    ", '22-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[tblDAANESNotification]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //// [dbo].[ConsenttoTreatmentViaTelehealth]	MN - Consent to Treatment Via Telehealth
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsenttoTreatmentViaTelehealth'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Consent to Treatment Via Telehealth' as [FormName] " +
                                    //    ", '23-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "  from [dbo].[ConsenttoTreatmentViaTelehealth]) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //// 11/9/2023 - RI - BHOLD 
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'RIBHOLD'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'RI - BHOLD' as [FormName] " +
                                    //    " , '24-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    " , a.PreAdmissionId, a.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    " , a.Createdby, convert(date, a.CreatedOn) as [CreatedOn], a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]" +
                                    //    " , isnull((select IsDeleted from dbo.SF_DataForms df where df.FormName = 'RI - BHOLD' and pa.DataFormId = df.Id), a.Isdeleted) Isdeleted " +
                                    //    "  from [dbo].[RIBHOLD] a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //// 11/10/2023 - RI - Health Home Care Plan Review Form
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'RIHealthHomeCareReview'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    string formname = "RI - Health Home Care Plan Review Form";
                                    //    strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                    //    ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                    //    ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', '" + formname + "' as [FormName] " +
                                    //    " , '25-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    " , a.PreAdmissionId, a.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                    //    " , a.Createdby, convert(date, a.CreatedOn) as [CreatedOn], a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]" +
                                    //    " , isnull((select IsDeleted from dbo.SF_DataForms df where df.FormName = '" + formname + "' and pa.DataFormId = df.Id), a.Isdeleted) Isdeleted " +
                                    //    "  from [dbo].[RIHealthHomeCareReview] a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID) v";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    #endregion
                                    //// Loop for getting all Forms with seperate tables
                                    List<BHG_DR_LIB.Models.TblForms2Process> xForms = db.TblForms2Process.Where(x => x.Enabled && x.RowState).OrderBy(o => o.Prefix).ToList();
                                    foreach (var xf in xForms.Where(x => x.TableName != null))
                                    {
                                        SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = '" + xf.TableName + "'", st.ConStr);
                                        if (SrcDt.Rows.Count == 1)
                                        {
                                            strCmd += "\r\nUnion Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID" +
                                               ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId)" +
                                               ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted \r\nfrom (" +
                                               "Select SiteCode = '" + st.SiteCode.ToString() + "', [FormName] = '" + xf.FormName + "'";
                                            switch (xf.TableName.ToLower())
                                            {
                                                case "adversechildhood":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)" +
                                                        "\r\n, PreAdmissionId = a.PreAdmissionId" +
                                                        ", ClientId = a.ClientId" +
                                                        ", QuestionID = 0" +
                                                        ", QuestionOrderID = 1" +
                                                        ", QuestionText = null" +
                                                        ", [OptionID] = null" +
                                                        ", AnswerValue = null" +
                                                        " , a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.ModifiedBy as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                case "financialhardshipapplication":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, a.cltId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)" +
                                                              "\r\n, PreAdmissionId = a.PreAdmissionId" +
                                                              ", ClientId = a.cltId" +
                                                              ", QuestionID = 0" +
                                                              ", QuestionOrderID = 1" +
                                                              ", QuestionText = null" +
                                                              ", [OptionID] = null" +
                                                              ", AnswerValue = null" +
                                                              "\r\n, a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.ModifiedBy as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                case "tbltp17review":
                                                    strCmd += ", [FormID] = '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID)" +
                                                        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID] " +
                                                        ", TprTYPE as AnswerValue, tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                                        ", Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW " +
                                                        "\r\n union Select SiteCode = '" + st.SiteCode.ToString() + "', 'TP-' + TprTYPE as [FormName] " +
                                                        ", '8-2-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                                        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 2 as QuestionID, 1 as QuestionOrderID, 'Treatment Phase Type' as QuestionText, null as [OptionID] " +
                                                        ", tpTreatmentPhase as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                                        ", Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW " +
                                                        "\r\n union Select SiteCode = '" + st.SiteCode.ToString() + "', 'TP-' + TprTYPE as [FormName] " +
                                                        ", '8-3-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                                        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 3 as QuestionID, 1 as QuestionOrderID, 'Next Due' as QuestionText, null as [OptionID] " +
                                                        ", convert(varchar, tprNEXT) as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                                        ", Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW ";

                                                    SrcDt = sm.GetTableData(st.FromTblVw, "select * from sys.all_columns c where c.object_id = (select object_id from sys.tables where name = 'tblTP17REVIEW') and name = 'tprReviewFrequency'", st.ConStr);
                                                    if (SrcDt.Rows.Count == 1)
                                                    {
                                                        strCmd += "\r\nunion Select SiteCode = '" + st.SiteCode.ToString() + "', 'TP-' + TprTYPE as [FormName] " +
                                                        ", '8-4-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                                        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 4 as QuestionID, 1 as QuestionOrderID, 'Review Frequency' as QuestionText, null as [OptionID] " +
                                                        ", case when Len(tprReviewFrequency) > 6 then rtrim(substring(tprReviewFrequency, 6, LEN(tprReviewFrequency) - 5)) else rtrim(tprReviewFrequency) end as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn] " +
                                                        ", null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW  ) v ";

                                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                                    }
                                                    else { strCmd += ") v "; }
                                                    break;
                                                case "tblorderreq":
                                                    strCmd += "\r\n, FormID = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ " +
                                                       "\r\n, PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 1, QuestionText = 'Effective Date', OptionID = 0, AnswerValue = convert(varchar,EffectiveDate, 101) " +
                                                       "\r\n, Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) " +
                                                       "\r\n, IsDeleted = Case when cltID < 0 then 1 else 0 end from [dbo].[tblORDERREQ] " +
                                                       "\r\n where status = 'Approved' and(Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST') " +
                                                       "\r\n union select SiteCode = '" + st.SiteCode.ToString() + "', FormName = 'Level Justification' " +
                                                       "\r\n, FormID = '9-2-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ " +
                                                       "\r\n, PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 2, QuestionText = 'Expiration Date', OptionID = 0, AnswerValue = convert(varchar,expirationdate, 101) " +
                                                       "\r\n, Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) " +
                                                       "\r\n, IsDeleted = Case when cltID < 0 then 1 else 0 end from [dbo].[tblORDERREQ] " +
                                                       "\r\n where status = 'Approved' and (Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST')) v";
                                                    break;
                                                case "insurancebenefitverification":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)" +
                                                       "\r\n, PreAdmissionId = a.PreAdmissionId" +
                                                       ", ClientId = a.PreAdmissionId" +
                                                       ", QuestionID = 0" +
                                                       ", QuestionOrderID = 1" +
                                                       ", QuestionText = null" +
                                                       ", [OptionID] = null" +
                                                       ", AnswerValue = null" +
                                                       "\r\n, a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.ModifiedBy as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on (a.PreAdmissionId = pa.ID) \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                case "referralform":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)" +
                                                        "\r\n, PreAdmissionId = a.PreAdmissionId" +
                                                        ", ClientId = a.ClientId" +
                                                        ", QuestionID = 0" +
                                                        ", QuestionOrderID = 1" +
                                                        ", QuestionText = null" +
                                                        ", [OptionID] = null" +
                                                        ", AnswerValue = null" +
                                                        " , a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.updatedby as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                case "sf_understandingoftreatment":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, pa.PatientId) + '-' + convert(varchar, isnull(a.PreAdmissionId, 0)) + '-' + convert(varchar, isnull(a.id,0))" +
                                                        "\r\n, PreAdmissionId = a.PreAdmissionId" +
                                                        ", ClientId = pa.PatientId" +
                                                        ", QuestionID = 0" +
                                                        ", QuestionOrderID = 1" +
                                                        ", QuestionText = null" +
                                                        ", [OptionID] = null" +
                                                        ", AnswerValue = null" +
                                                        " , a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.LastUpdatedBy as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                    case "sf_patientpreadmission":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, a.PatientId) + '-' + convert(varchar, isnull(a.ParentPreAdmissionId, 0)) + '-' + convert(varchar, isnull(a.id,0))" +
                                                        "\r\n, PreAdmissionId = a.Id" +
                                                        ", ClientId = a.PatientId" +
                                                        ", QuestionID = 0" +
                                                        ", QuestionOrderID = 1" +
                                                        ", QuestionText = null" +
                                                        ", [OptionID] = null" +
                                                        ", AnswerValue = null" +
                                                        " , a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.LastUpdatedBy as [UpdatedBy]";  
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.ID = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                case "newperiodicreassessment":
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + convert(varchar, a.PatientId) + '-' + convert(varchar, isnull(a.ParentPreAdmissionId, 0)) + '-' + convert(varchar, isnull(a.id,0))" +
                                                        "\r\n, PreAdmissionId = a.Id" +
                                                        ", ClientId = a.PatientId" +
                                                        ", QuestionID = 0" +
                                                        ", QuestionOrderID = 1" +
                                                        ", QuestionText = null" +
                                                        ", [OptionID] = null" +
                                                        ", AnswerValue = null" +
                                                        " , a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.LastUpdatedBy as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.ID = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                                default:
                                                    strCmd += "\r\n, FormID = '" + xf.Prefix.ToString() + "-' + isnull(convert(varchar, a.ClientId), '0') + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)" +
                                                        "\r\n, PreAdmissionId = a.PreAdmissionId" +
                                                        ", ClientId = a.ClientId" +
                                                        ", QuestionID = 0" +
                                                        ", QuestionOrderID = 1" +
                                                        ", QuestionText = null" +
                                                        ", [OptionID] = null" +
                                                        ", AnswerValue = null" +
                                                        " , a.Createdby, [CreatedOn] = convert(date, a." + xf.CreatedOn + "), a.ModifiedBy as [UpdatedBy]";
                                                    if (xf.ModifiedOn != null)
                                                    {
                                                        strCmd += ", convert(date, a." + xf.ModifiedOn + ") as [UpdatedOn]";
                                                    }
                                                    else
                                                    {
                                                        strCmd += ", [UpdatedOn] = null";
                                                    }
                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";
                                                    strCmd += "\r\n  from " + xf.TableName + " a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v";
                                                    break;
                                            }
                                            if (xf.DateFilterEnabled)
                                            {
                                                strCmd += " Where (v.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(v.UpdatedOn, v.CreatedOn) >= '"
                                                    + wrkdt.ToShortDateString() + "')";
                                            }
                                        }
                                    }
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select distinct * from (" + strCmd + ") z", st.ConStr);
                                    if ((st.SiteCode.ToUpper() == "B37") || (st.SiteCode.ToUpper() == "DM") || (st.SiteCode.ToUpper() == "GAL") ||
                                        (st.SiteCode.ToUpper() == "HGT") || (st.SiteCode.ToUpper() == "LV1") || (st.SiteCode.ToUpper() == "NC") ||
                                        (st.SiteCode.ToUpper() == "PH") || (st.SiteCode.ToUpper() == "D07") || (st.SiteCode.ToUpper() == "B26")
                                        || (st.SiteCode.ToUpper() == "B24") || (st.SiteCode.ToUpper() == "DRD - SF") || (st.SiteCode.ToUpper() == "V12")
                                        || (st.SiteCode.ToUpper() == "B35") || (st.SiteCode.ToUpper() == "B25") || (st.SiteCode.ToUpper() == "V9")
                                        || (st.SiteCode.ToUpper() == "FW") || (st.SiteCode.ToUpper() == "LO") || (st.SiteCode.ToUpper() == "B42")
                                        )
                                    {
                                        rCodes.IsResult = true;
                                        rCodes.RowsProcessed = SrcDt.Rows.Count;
                                        BHG_DR_LIB.SQLSvrManager sqlm = new BHG_DR_LIB.SQLSvrManager();
                                        _ = sqlm.ExeSqlCmd("Truncate Table [stg].[tbl_FormQA]", sqlm.ConnectionString);
                                        SqlBulkCopy bc = new SqlBulkCopy(sqlm.ConnectionString)
                                        {
                                            DestinationTableName = "[stg].[tbl_FormQA]",
                                            BulkCopyTimeout = 99999
                                        };
                                        foreach (DataColumn c in SrcDt.Columns)
                                        {
                                            bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));
                                        }
                                        try
                                        {
                                            // Write from the source to the destination.
                                            bc.WriteToServer(SrcDt);
                                            var tblr6 = sqlm.ExecStrPro("stg.sp_FormQA_Merge", "@sitecode", st.SiteCode, sm.ConnectionString);
                                            tblr6 = sqlm.ExecStrPro("pats.BAMMerge", "@sitecode", st.SiteCode, sm.ConnectionString);
                                        }
                                        catch (Exception e)
                                        {
                                            rCodes.IsResult = false;
                                            rCodes.ExceptMsg = e.Message.ToString();
                                        }
                                    }
                                    else
                                    {
                                        rCodes = sd.SaveFormQuestionAnswers(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(formDaysBack), xForms, null);
                                        // add Execute Store Procedure for BAM here
                                        var tblr6 = sm.ExecStrPro("pats.BAMMerge", "@sitecode", st.SiteCode, sm.ConnectionString);
                                    }
                                }
                                else { rCodes.ExceptMsg = "No Form table."; rCodes.IsResult = true; }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_dbo_formanswersignatures":
                                //check if table exists and if yes then process
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'answersignature'", st.ConStr);
                                // if yes then
                                if (SrcDt.Rows.Count == 1)
                                {
                                    int formDaysBack = DaysBack - 15;
                                    DateTime wrkdt = st.WorkDate.Value.AddDays(formDaysBack).Date;
                                    if (st.WorkDate.Value.Date == DateTime.Parse("2/2/2024"))
                                    {
                                        wrkdt = DateTime.Parse("1/1/2010");
                                    }
                                    //strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    strCmd = "select distinct SiteCode, FormName, convert(varchar(100),FormId) FormId, ClientId, CreatedOn, UpdatedOn, Isdeleted \r\n" +
                                        ", CompletedBySignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'CompletedBySignatureSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", CounselorSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and \r\n" +
                                        " (DateField = 'CounselorSignatureSignatureDate' or DateField = 'CounselorSignatureDate') order by [DateTime] desc) \r\n" +
                                        ", DoctorSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'DoctorSignatureSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", MedicalProviderSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'MedicalProviderSignatureSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", PatientSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'PatientSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", ProviderSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'ProviderSignatureSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", RequestorSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'RequestorSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", StaffSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'StaffSignatureDate' order by [DateTime] desc) \r\n" +
                                        ", SupervisorSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'SupervisorSignatureSignatureDate' order by [DateTime] desc) \r\n" +
                                        " from (select SiteCode = '" + st.SiteCode.ToString() + "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.UpdatedOn" +
                                        ", IsDeleted = case when isnull(f.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end " +
                                        " from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) inner join [dbo].[SF_PatientPreAdmission] pa on (f.PreAdmissionId = pa.ID) " +
                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)" +
                                        //" where (f.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() +
                                        "/*')*/ union select SiteCode = '" + st.SiteCode.ToString() + "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.UpdatedOn" +
                                        ", IsDeleted = case when isnull(f.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end" +
                                        " from dbo.Form f left join FormTemplate ft on(f.FormTemplateId = ft.Id) inner join [dbo].[SF_PatientPreAdmission] pa on (f.PreAdmissionId = pa.ID) " +
                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)" +
                                        " where /*(f.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() + "') or*/ f.Isdeleted = 1) x \r\n";

                                    // Loop for getting all Forms with seperate tables
                                    List<BHG_DR_LIB.Models.TblForms2Process> xForms = db.TblForms2Process.Where(x => x.Enabled && x.RowState).ToList();
                                    foreach(var xf in xForms.Where(x => x.TableName != null))
                                    {
                                        SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = '" + xf.TableName + "'", st.ConStr);
                                        if (SrcDt.Rows.Count == 1)
                                        {
                                            switch (xf.TableName)
                                            {
                                                case "tblORDERREQ":
                                                    strCmd += " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Level Justification' as [FormName]" +
                                                        ", [FormID] = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-1'" +
                                                        ", ClientId = cltID /*, Createdby = Staff*/, [CreatedOn] = convert(date, DateAdded) " +
                                                        "/*, [UpdatedBy] = StatusUser*/, [UpdatedOn] = convert(date, statusDate) " +
                                                        ", Isdeleted = Case when cltID < 0 then 1 else 0 end " +
                                                        ", CompletedBySignatureSignatureDate = null " +
                                                        ", CounselorSignatureSignatureDate = null " +
                                                        ", DoctorSignatureSignatureDate = null " +
                                                        ", MedicalProviderSignatureSignatureDate = null " +
                                                        ", PatientSignatureDate = null " +
                                                        ", ProviderSignatureSignatureDate = case when (ISNull(convert(date, DrSigDt), convert(date, SigNurseDt)) is null and Status = 'Approved') then '1900-01-01' else ISNull(convert(date, DrSigDt), convert(date, SigNurseDt)) end" +
                                                        ", RequestorSignatureDate = null" +
                                                        ", StaffSignatureDate = null" +
                                                        ", SupervisorSignatureSignatureDate = case when convert(date, sigCoordinatorDt) is null and Status = 'Approved' then '1900-01-01' else convert(date, sigCoordinatorDt) end" +
                                                        " from [dbo].[tblORDERREQ] where status = 'Approved' and (Notes not like 'Test %' and Notes <> 'TEST' and DrNote <>'HEllo test' and DrNote <> 'TEST') \r\n";
                                                    if (xf.DateFilterEnabled)
                                                    {
                                                        strCmd += "and (DateAdded >= '" + wrkdt.ToShortDateString() + 
                                                            "' or isnull(statusDate, DateAdded) >= '" + wrkdt.ToShortDateString() 
                                                            //+ "' or SupervisorSignatureSignatureDate >= '" + wrkdt.ToShortDateString()
                                                            //+ "' or ProviderSignatureSignatureDate >= '" + wrkdt.ToShortDateString() 
                                                            + "') \r\n";
                                                    }
                                                    break;
                                                case "tblTP17REVIEW":
                                                    strCmd += " union select distinct SiteCode, FormName, FormID, ClientId, CreatedOn, UpdatedOn, IsDeleted " +
                                                        ", CompletedBySignatureSignatureDate, CounselorSignatureSignatureDate, DoctorSignatureSignatureDate, MedicalProviderSignatureSignatureDate " +
                                                        ", PatientSignatureDate, ProviderSignatureSignatureDate, RequestorSignatureDate, StaffSignatureDate, SupervisorSignatureSignatureDate from (" +
                                                        "Select SiteCode = '" + st.SiteCode.ToString() + "', 'TP-' + tprType as [FormName] " +
                                                        ", '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID]" +
                                                        ", null PreAdmissionId, tprCLTID ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID], TprTYPE as AnswerValue" +
                                                        ", tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID < 0 then 1 else 0 end" +
                                                        ", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null" +
                                                        ", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end" +
                                                        ", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end" +
                                                        ", RequestorSignatureDate = null" +
                                                        ", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                                        ", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from [dbo].tblTP17REVIEW) tp\r\n";
                                                    if (xf.DateFilterEnabled)
                                                    {
                                                        strCmd += " Where (CreatedOn >= '" + wrkdt.ToShortDateString() + 
                                                            "' or isnull(UpdatedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() +
                                                            "' or ProviderSignatureSignatureDate > = '" + wrkdt.ToShortDateString() +
                                                            "' or CompletedBySignatureSignatureDate >= '" + wrkdt.ToShortDateString() +
                                                            "' or PatientSignatureDate >= '" + wrkdt.ToShortDateString() +
                                                            "' or StaffSignatureDate > = '" + wrkdt.ToShortDateString() +
                                                            "' or SupervisorSignatureSignatureDate > = '" + wrkdt.ToShortDateString() + "') \r\n";
                                                    }
                                                    break;
                                                #region Default
                                                default:
                                                    strCmd += "union select distinct SiteCode = '" + st.SiteCode.ToString() + "', '" + xf.FormName + "' as [FormName]";
                                                    switch (xf.TableName)
                                                    {
                                                        case "SF_PatientPreAdmission":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, isnull(a.ParentPreAdmissionId, 0)) + '-' + convert(varchar, isnull(a.id, 0)) as [FormID]" +
                                                                ", ClientId = isnull(pa.PatientID, 0)";
                                                            break;
                                                        case "SF_DataForm":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, isnull(a.PreAdmissionId,0)) + '-' + convert(varchar, isnull(a.id, 0)) as [FormID]" +
                                                                ", ClientId = isnull(pa.PatientID, 0)";
                                                            break;
                                                        case "SF_UnderstandingOfTreatment":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID]" +
                                                                ", ClientId = isnull(pa.PatientID, 0)";
                                                            break;
                                                        case "InsuranceBenefitVerification":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID]" +
                                                                ", ClientId = isnull(pa.PatientID, 0)";
                                                            break;
                                                        case "FinancialHardshipApplication":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(a.CltID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID]" +
                                                                ", ClientId = isnull(a.CltID, 0)";
                                                            break;
                                                        case "xNewAdmissionAssessment":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(b.ClientId, 0)) + '-' + convert(varchar, b.PreAdmissionId) + '-' + convert(varchar, b.id) as [FormID]" +
                                                            ", ClientId = isnull(b.ClientId, 0) ";
                                                            break;
                                                        case "ConsenttoTreatmentforIOPOrEOPOrOP":
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(d.PatientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID]" +
                                                            ", ClientId = isnull(d.PatientId, 0) ";
                                                            break;
                                                        default:
                                                            strCmd += ", '" + xf.Prefix.ToString() + "-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID]" +
                                                            ", ClientId = isnull(a.ClientId, 0) ";
                                                            break;
                                                    }
                                                    strCmd += "\r\n, [CreatedOn] = convert(date, a." + xf.CreatedOn + ")";
                                                    if (xf.ModifiedOn != null)
                                                    { strCmd += "\r\n, [UpdatedOn] = convert(date, a." + xf.ModifiedOn + ")"; }
                                                    else { strCmd += ", [UpdatedOn] = null"; }

                                                    strCmd += "\r\n, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end";

                                                    if (xf.CompletedBy != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, CompletedBySignatureSignatureDate = case when convert(date, b." + xf.CompletedBy
                                                                + ") is null then '1900-01-01' else convert(date, b." + xf.CompletedBy + ") end ";
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, CompletedBySignatureSignatureDate = case when convert(date, a." + xf.CompletedBy
                                                                + ") is null then '1900-01-01' else convert(date, a." + xf.CompletedBy + ") end ";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, CompletedBySignatureSignatureDate = null "; }
                                                    if (xf.Counselor != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, CounselorSignatureSignatureDate = case when convert(date, b." + xf.Counselor
                                                                     + ") is null then '1900-01-01' else convert(date, b." + xf.Counselor + ") end";
                                                                break;
                                                            case "MNComprehensiveAssessment":
                                                                strCmd += "\r\n, CounselorSignatureSignatureDate = case when convert(date, b." + xf.Counselor
                                                                     + ") is null then '1900-01-01' else convert(date, b." + xf.Counselor + ") end";
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, CounselorSignatureSignatureDate = case when convert(date, a." + xf.Counselor
                                                                     + ") is null then '1900-01-01' else convert(date, a." + xf.Counselor + ") end";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, CounselorSignatureSignatureDate = null "; }
                                                    if (xf.Doctor != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, DoctorSignatureSignatureDate = case when convert(date, b." + xf.Doctor
                                                            + ") is null then '1900-01-01' else convert(date, b." + xf.Doctor + ") end";
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, DoctorSignatureSignatureDate = case when convert(date, a." + xf.Doctor
                                                            + ") is null then '1900-01-01' else convert(date, a." + xf.Doctor + ") end";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, DoctorSignatureSignatureDate = null"; }
                                                    if (xf.MedicalProvider != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, MedicalProviderSignatureSignatureDate = case when convert(date, b." + xf.MedicalProvider
                                                                    + ") is null then '1900-01-01' else convert(date, b." + xf.MedicalProvider + ") end";
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, MedicalProviderSignatureSignatureDate = case when convert(date, a." + xf.MedicalProvider
                                                            + ") is null then '1900-01-01' else convert(date, a." + xf.MedicalProvider + ") end";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, MedicalProviderSignatureSignatureDate = null"; }
                                                    if (xf.Patient != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "AdmissionAssessment":
                                                                strCmd += "\r\n, PatientSignatureDate = case when convert(date,aas." + xf.Patient + ") is null" +
                                                                    " then '1900-01-01' else convert(date,aas." + xf.Patient + ") end ";
                                                                break;
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, PatientSignatureDate = case when convert(date, b." + xf.Patient
                                                                    + ") is null then '1900-01-01' else convert(date, b." + xf.Patient + ") end";
                                                                break;
                                                            case "MNComprehensiveAssessment":
                                                                strCmd += "\r\n, PatientSignatureDate = case when convert(date, b." + xf.Patient
                                                                    + ") is null then '1900-01-01' else convert(date, b." + xf.Patient + ") end";
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, PatientSignatureDate = case when convert(date, a." + xf.Patient
                                                                    + ") is null then '1900-01-01' else convert(date, a." + xf.Patient + ") end";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, PatientSignatureDate = null"; }
                                                    if (xf.Provider != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "AdmissionAssessment":
                                                                strCmd += "\r\n, ProviderSignatureSignatureDate = case when convert(date, aas." + xf.Provider
                                                                    + ") is null then '1900-01-01' else convert(date, aas." + xf.Provider + ") end\r\n";
                                                                break;
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, ProviderSignatureSignatureDate = case when convert(date, b." + xf.Provider
                                                                    + ") is null then '1900-01-01' else convert(date, b." + xf.Provider + ") end\r\n";
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, ProviderSignatureSignatureDate = case when convert(date, a." + xf.Provider
                                                                    + ") is null then '1900-01-01' else convert(date, a." + xf.Provider + ") end\r\n";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, ProviderSignatureSignatureDate = null"; }
                                                    if (xf.Requestor != null)
                                                    {
                                                        strCmd += "\r\n, RequestorSignatureDate = case when convert(date, a." + xf.Requestor
                                                            + ") is null then '1900-01-01' else convert(date, a." + xf.Requestor + ") end\r\n";
                                                    }
                                                    else { strCmd += "\r\n, RequestorSignatureDate = null\r\n"; }
                                                    if (xf.Staff != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "AdmissionAssessment":
                                                                strCmd += "\r\n, [StaffSignatureDate] = case when convert(date, aas." + xf.Staff
                                                                    + ") is null then '1900-01-01' else convert(date, aas." + xf.Staff + ") end\r\n";
                                                                break;
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, [StaffSignatureDate] = case when convert(date, b." + xf.Staff
                                                                    + ") is null then '1900-01-01' else convert(date, b." + xf.Staff + ") end\r\n";
                                                                break;
                                                            case "MNComprehensiveAssessment":
                                                                strCmd += "\r\n, [StaffSignatureDate] = case when convert(date, b." + xf.Staff
                                                                    + ") is null then '1900-01-01' else convert(date, b." + xf.Staff + ") end\r\n";
                                                                break;
                                                            case "SF_PatientPreAdmission":
                                                                if (st.SiteCode.ToUpper() == "LAB")
                                                                {
                                                                    strCmd += "\r\n, [StaffSignatureDate] = null";
                                                                }
                                                                else
                                                                {
                                                                    strCmd += "\r\n, [StaffSignatureDate] = case when convert(date, a." + xf.Staff
                                                                    + ") is null then '1900-01-01' else convert(date, a." + xf.Staff + ") end";
                                                                }
                                                                break;
                                                            default:
                                                                strCmd += "\r\n, [StaffSignatureDate] = case when convert(date, a." + xf.Staff
                                                                    + ") is null then '1900-01-01' else convert(date, a." + xf.Staff + ") end";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, [StaffSignatureDate] = null"; }
                                                    if (xf.Supervisor != null)
                                                    {
                                                        switch (xf.TableName)
                                                        {
                                                            case "AdmissionAssessment":
                                                                strCmd += "\r\n, SupervisorSignatureSignatureDate = case when convert(date, aas." + xf.Supervisor
                                                                    + ") is null then '1900-01-01' else convert(date, aas." + xf.Supervisor + ") end\r\n";
                                                                break;
                                                            case "NewAdmissionAssessment":
                                                                strCmd += "\r\n, SupervisorSignatureSignatureDate = case when convert(date, b." + xf.Supervisor
                                                                                + ") is null then '1900-01-01' else convert(date, b." + xf.Supervisor + ") end\r\n";

                                                                break;
                                                            case "MNComprehensiveAssessment":
                                                                strCmd += "\r\n, SupervisorSignatureSignatureDate = case when convert(date, b." + xf.Supervisor
                                                                                + ") is null then '1900-01-01' else convert(date, b." + xf.Supervisor + ") end\r\n";

                                                                break;
                                                            default:
                                                                strCmd += "\r\n, SupervisorSignatureSignatureDate = case when convert(date, a." + xf.Supervisor
                                                                        + ") is null then '1900-01-01' else convert(date, a." + xf.Supervisor + ") end\r\n";
                                                                break;
                                                        }
                                                    }
                                                    else { strCmd += "\r\n, SupervisorSignatureSignatureDate = null"; }
                                                    strCmd += "\r\n from " + xf.TableName + " a";
                                                    if (xf.TableName == "SF_PatientPreAdmission")
                                                    {
                                                        strCmd += " inner join [dbo].[SF_PatientPreAdmission] pa on a.ID = pa.ID \r\n" +
                                                        " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id) \r\n";
                                                    }
                                                    else
                                                    {
                                                        strCmd += " inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID \r\n" +
                                                            " left join dbo.SF_DataForms d on (pa.DataFormId = d.Id) \r\n";
                                                    }
                                                    if (xf.TableName == "AdmissionAssessment")
                                                    {
                                                        strCmd += " inner join [dbo].[AdmissionAssessmentSummary] aas on (a.Id = aas.AdmissionAssessmentId and a.PreAdmissionId = aas.PreAdmissionId) \r\n";
                                                    }
                                                    if (xf.TableName == "NewAdmissionAssessment")
                                                    {
                                                        strCmd += " inner join [dbo].[NewAdmissionAssessmentASAMDimension6] b on (a.preadmissionID = b.preadmissionID and a.ID = b.NewAdmissionAssessmentFormId) \r\n";
                                                    }
                                                    if (xf.TableName == "MNComprehensiveAssessment")
                                                    {
                                                        strCmd += " inner join [dbo].[MNComprehensiveAssessmentSocialHistory] b on (a.preadmissionID = b.preadmissionID and a.Id = b.MNComprehensiveAssessmentFormId) \r\n";
                                                    }

                                                    if (xf.DateFilterEnabled)
                                                    {
                                                        strCmd += "\r\n where a." + xf.CreatedOn + " >= '" + wrkdt.ToShortDateString() + "' or isnull(a." + xf.ModifiedOn + ", a." + xf.CreatedOn + ") >= '" 
                                                            + wrkdt.ToShortDateString() + "'\r\n";
                                                    }
                                                    break;
                                                    #endregion
                                            }
                                        }
                                    }
                                    #region code removed
                                    ////" --Suicide Severity Rating Scale --(For ETL’ing to pats.tbl_dbo_FormQuestionAnswers) "
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SuicideSeverityRatingScale'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Suicide Severity Rating Scale' as [FormName]" +
                                    //    "     , '1-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "     , c.ClientId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " + 
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = convert(date, c.MedicalProviderSignatureDate) " +
                                    //    "     , PatientSignatureDate = null" +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, c.StaffSignatureDate) is null then '1900-01-01' else convert(date, c.StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = case when convert(date, c.SupervisorSignatureDate) is null then '1900-01-01' else convert(date, c.SupervisorSignatureDate) end " +
                                    //    "  from [dbo].[SuicideSeverityRatingScale] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Suicide Severity Rating Scale' ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //" --Suicide Severity Rating Scale 2.0 --(For ETL’ing to pats.tbl_dbo_FormQuestionAnswers) " Mod: 08/23/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SAFETProtocolwithCSSRS'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Suicide Severity Rating Scale 2.0' as [FormName]" +
                                    //    "     , '11-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "     , c.ClientId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null" +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, c.StaffSignatureDate) is null then '1900-01-01' else convert(date, c.StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = case when convert(date, c.SupervisorSignatureDate) is null then '1900-01-01' else convert(date, c.SupervisorSignatureDate) end " +
                                    //    "  from [dbo].[SAFETProtocolwithCSSRS] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Suicide Severity Rating Scale 2.0' ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //"  --Health Questionnaire "  Mod: 08/23/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'HealthQuestionnaire'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Health Questionnaire' as [FormName]" +
                                    //    "      , '2-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.ClientId, convert(date, c.Createddate), convert(date, c.Modifieddate), c.Isdeleted " +
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null " +
                                    //    "      , DoctorSignatureSignatureDate = case when convert(date, c.DoctorSignatureDate) is null then '1900-01-01' else convert(date, c.DoctorSignatureDate) end " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = null " +
                                    //    "      , ProviderSignatureSignatureDate = case when convert(date, c.NurseSignatureDate) is null then '1900-01-01' else convert(date, c.NurseSignatureDate) end " +
                                    //    "      , RequestorSignatureDate = null " +
                                    //    "      , StaffSignatureDate = null " +
                                    //    "      , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[HealthQuestionnaire]  c ";
                                    //    //" left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    //" left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    //" where df.FormName = 'Health Questionnaire' ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //    // if Enabling where filter, function will need to be updated to match filter.
                                    //}
                                    //"  --Infectious Disease And Behavioral Screen " Mod:8/24/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InfectiousDiseaseAndBehavioralScreen'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Infectious Disease And Behavioral Screen' as [FormName]" +
                                    //    "      , '3-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.ClientId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null" +
                                    //    "      , DoctorSignatureSignatureDate = null " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = null " +
                                    //    "      , ProviderSignatureSignatureDate = case when convert(date, c.MedicalStaffSignatureDate) is null then '1900-01-01' else convert(date, c.MedicalStaffSignatureDate) end " +
                                    //    "      , RequestorSignatureDate = null" +
                                    //    "      , StaffSignatureDate = null " +
                                    //    "      , SupervisorSignatureSignatureDate = null " +
                                    //    " from [dbo].[InfectiousDiseaseAndBehavioralScreen] c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Infectious Disease And Behavioral Screen' ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //    // if Enabling where filter, function will need to be updated to match filter.
                                    //}
                                    ////"  --Consent to Treatment with an Approved Narcotic " Modified: 8/16/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsentToTreatmentWithAnApprovedNarcotic'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Consent to Treatment with an Approved Narcotic' as [FormName]" +
                                    //    "      , '4-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.ClientId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn], df.Isdeleted " +
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null" +
                                    //    "      , DoctorSignatureSignatureDate = case when convert(date,c.DoctorSignatureDate) is null then '1900-01-01' else convert(date,c.DoctorSignatureDate) end " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = case when convert(date,c.PatientSignatureDate) is null then '1900-01-01' else convert(date,c.PatientSignatureDate) end  " +
                                    //    "      , ProviderSignatureSignatureDate = null " +
                                    //    "      , RequestorSignatureDate = null" +
                                    //    "      , StaffSignatureDate = case when convert(date,c.StaffSignatureDate) is null then '1900-01-01' else convert(date,c.StaffSignatureDate) end " +
                                    //    "      , SupervisorSignatureSignatureDate = null " +
                                    //    " from [dbo].[ConsentToTreatmentWithAnApprovedNarcotic]  c " +
                                    //    " left join SF_PatientPreAdmission pa  on c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID " +
                                    //    " left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId " +
                                    //    " where df.FormName = 'Consent to Treatment with an Approved Narcotic' ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' "
                                    //    // if Enabling where filter, function will need to be updated to match filter.
                                    //}
                                    //"  -- 'Financial Hardship Application' " Modified: 10/03/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'FinancialHardshipApplication'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Financial Hardship Application' as [FormName]" +
                                    //    "      , '5-' + convert(varchar, c.CltId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.CltId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn], isnull((select df.Isdeleted " +
                                    //    " from SF_DataForms df left join SF_PatientPreAdmission pa  on df.Id = abs(pa.DataFormId) and df.Id = pa.ID and df.PatientID = pa.PatientId " +
                                    //    " where df.FormName = 'Financial Hardship Application' and c.DataFormId = df.Id and c.CltId = df.PatientId), c.Isdeleted) Isdeleted " + 
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null" +
                                    //    "      , DoctorSignatureSignatureDate = null " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = case when convert(date,c.FHAPatientSignatureDate) is null then '1900-01-01' else convert(date,c.FHAPatientSignatureDate) end " +
                                    //    "      , ProviderSignatureSignatureDate = null " +
                                    //    "      , RequestorSignatureDate = null" +
                                    //    "      , StaffSignatureDate = null " +
                                    //    "      , SupervisorSignatureSignatureDate = null " +
                                    //    " from [dbo].[FinancialHardshipApplication]  c ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //"  -- 'Comprehensive Assessment Form' " Modified: 10/03/2023, 10/11/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ComprehensiveAssessmentForm'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Comprehensive Assessment Form' as [FormName]" +
                                    //    "      , '6-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] " +
                                    //    "      , c.ClientId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn] " +
                                    //    ", isnull((select df.Isdeleted from SF_PatientPreAdmission pa inner join SF_DataForms df on abs(pa.DataFormId) = df.Id and pa.PatientId = df.PatientId " +
                                    //    " where df.FormName in ('Comprehensive Assessment', 'Comprehensive Assessment Form') and c.DataFormId = abs(pa.DataFormId) and " +
                                    //    " c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID), c.IsDeleted) Isdeleted " +
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null" +
                                    //    "      , DoctorSignatureSignatureDate = null " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = case when convert(date,c.CAPatientSignatureDate) is null then '1900-01-01' else convert(date,c.CAPatientSignatureDate) end " +
                                    //    "      , ProviderSignatureSignatureDate = case when convert(date,c.CAProviderSignatureDate) is null then null else convert(date,c.CAProviderSignatureDate) end " +
                                    //    "      , RequestorSignatureDate = null" +
                                    //    "      , StaffSignatureDate = case when convert(date, c.CAStaffSignatureDate) is null then '1900-01-01' else convert(date,c.CAStaffSignatureDate) end " +
                                    //    "      , SupervisorSignatureSignatureDate = case when convert(date,c.CASupervisorSignatureDate) is null then null else convert(date,c.CASupervisorSignatureDate) end " +
                                    //    " from [dbo].[ComprehensiveAssessmentForm] c ";
                                    //    //" left join SF_DataForms df on df.Id = c.DataFormId and df.PatientId = c.ClientId " +
                                    //    //" inner join SF_PatientPreAdmission pa  on df.Id = abs(pa.DataFormId) and df.PatientId = pa.PatientID " +
                                    //    //" where df.FormName = 'Comprehensive Assessment Form' and c.PreAdmissionId = pa.ID  ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////"  -- 'Admission Assessment' " Mod: 8/24/2023 9/13/2023, 10/11/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'AdmissionAssessment'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName]" +
                                    //    "      , '7-' + convert(varchar, aa.ClientId) + '-' + convert(varchar, aa.PreAdmissionId) + '-' + convert(varchar, aa.id) as [FormID] " +
                                    //    "      , aa.ClientId, convert(date, aa.CreatedOn) as [CreatedOn], convert(date, aa.ModifiedOn) as [UpdatedOn] " +
                                    //    //", isnull((select df.Isdeleted from SF_PatientPreAdmission pa inner join SF_DataForms df on pa.DataFormId = df.Id and pa.PatientId = df.PatientId " +
                                    //    //"  where df.FormName = 'Admission Assessment' and aa.DataFormId = abs(pa.DataFormId) and aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID), aa.IsDeleted) IsDeleted " +
                                    //    ", isnull(pa.Isdeleted, aa.IsDeleted) IsDeleted" +
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null" +
                                    //    "      , DoctorSignatureSignatureDate = null " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = case when convert(date,aas.AdmissionAssessmentPatientSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentPatientSignatureDate) end " +
                                    //    "      , ProviderSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentProviderSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentProviderSignatureDate) end " +
                                    //    "      , RequestorSignatureDate = null" +
                                    //    "      , StaffSignatureDate = case when convert(date,aas.AdmissionAssessmentStaffSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentStaffSignatureDate) end " +
                                    //    "      , SupervisorSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) end " +
                                    //    " from [dbo].[AdmissionAssessment] aa inner join [dbo].[AdmissionAssessmentSummary] aas on (aa.Id = aas.AdmissionAssessmentId and aa.PreAdmissionId = aas.PreAdmissionId)" +
                                    //    " inner join SF_PatientPreAdmission pa on (aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID) " +
                                    //    "Where pa.DataFormId >= 0 or pa.DataFormId is null";
                                    //    //" left join SF_DataForms df on aa.DataFormId = df.Id and aa.ClientId = df.PatientId " +
                                    //    //" where df.FormName = 'Admission Assessment'";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    //"  -- 'Treatment Plan' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblTP17REVIEW'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode, FormName, FormID, ClientId, CreatedOn, UpdatedOn, IsDeleted " +
                                    //    ", CompletedBySignatureSignatureDate, CounselorSignatureSignatureDate, DoctorSignatureSignatureDate, MedicalProviderSignatureSignatureDate " +
                                    //    ", PatientSignatureDate, ProviderSignatureSignatureDate, RequestorSignatureDate, StaffSignatureDate, SupervisorSignatureSignatureDate from( " +
                                    //    " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //    ", '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //    ", null PreAdmissionId, tprCLTID ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID], TprTYPE as AnswerValue " +
                                    //    ", tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID < 0 then 1 else 0 end " +
                                    //    ", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null " +
                                    //    ", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end " +
                                    //    ", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end " +
                                    //    ", RequestorSignatureDate = null " +
                                    //    ", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                    //    ", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from [dbo].tblTP17REVIEW ) tp"; 
                                    //    //" union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //    //", '8-2-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //    //", null PreAdmissionId, ABS(tprCLTID) ClientId, 2 as QuestionID, 1 as QuestionOrderID, 'Treatment Phase Type' as QuestionText, null as [OptionID], tpTreatmentPhase as AnswerValue " +
                                    //    //", tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end " +
                                    //    //", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null " +
                                    //    //", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end " +
                                    //    //", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end " +
                                    //    //", RequestorSignatureDate = null " +
                                    //    //", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                    //    //", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from[dbo].tblTP17REVIEW " +
                                    //    //" union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                    //    //", '8-3-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                    //    //", null PreAdmissionId, ABS(tprCLTID) ClientId, 3 as QuestionID, 1 as QuestionOrderID, 'Next Due' as QuestionText, null as [OptionID], convert(varchar, tprNEXT) as AnswerValue " +
                                    //    //", tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end " +
                                    //    //", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null " +
                                    //    //", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end " +
                                    //    //", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end " +
                                    //    //", RequestorSignatureDate = null " +
                                    //    //", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                    //    //", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from[dbo].tblTP17REVIEW ";
                                    //}
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblORDERREQ'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //}
                                    #endregion
                                    #region
                                    ////"  -- 'BHG Notice Of Privacy Practices' " +
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblBHGNoticeOfPrivacyPractices'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union " +
                                    //    "select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'BHG Notice Of Privacy Practices' as [FormName]" +
                                    //    "      , '10-' + convert(varchar, aa.ClientId) + '-' + convert(varchar, aa.PreAdmissionId) + '-' + convert(varchar, aa.id) as [FormID] " +
                                    //    "      , aa.ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "      , CompletedBySignatureSignatureDate = null" +
                                    //    "      , CounselorSignatureSignatureDate = null" +
                                    //    "      , DoctorSignatureSignatureDate = null " +
                                    //    "      , MedicalProviderSignatureSignatureDate = null " +
                                    //    "      , PatientSignatureDate = case when convert(date,PatientSignatureDate) is null then '1900-01-01' else convert(date,PatientSignatureDate) end " +
                                    //    "      , ProviderSignatureSignatureDate = null " +
                                    //    "      , RequestorSignatureDate = null" +
                                    //    "      , StaffSignatureDate = null " +
                                    //    "      , SupervisorSignatureSignatureDate = null " +
                                    //    " from [dbo].[tblBHGNoticeOfPrivacyPractices] aa ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    //}
                                    ////" --KS Patient Rights and Responsibilities"
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'KSPatientRightsResponsibilities'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'KS Patient Rights and Responsibilities' as [FormName]" +
                                    //    "     , '12-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "     , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = case when convert(date,PatientSignatureDate) is null then '1900-01-01' else convert(date,PatientSignatureDate) end " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[KSPatientRightsResponsibilities] ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    ////" --CO - Consent Central Registry Colorado"
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsentCentralRegistryColorado'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'CO - Consent Central Registry Colorado' as [FormName]" +
                                    //    "     , '13-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "     , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = case when convert(date,PatientSignatureDate) is null then '1900-01-01' else convert(date,PatientSignatureDate) end " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[ConsentCentralRegistryColorado] ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    ////" --MN - Consent to Central Registry"
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'GeneralConsentAuthforReleaseInfo'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Consent to Central Registry' as [FormName]" +
                                    //    "     , '14-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "     , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = case when convert(date,PatientSignatureDate) is null then '1900-01-01' else convert(date,PatientSignatureDate) end " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[GeneralConsentAuthforReleaseInfo] ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 'Adverse Childhood Experiences' Added 6/24/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'AdverseChildhood'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Adverse Childhood Experiences' as [FormName]" +
                                    //    "     , '15-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(pa.PatientID, 0), convert(date, a.CreatedDate) as [CreatedOn], convert(date, a.ModifiedDate) as [UpdatedOn], a.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate =  case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[AdverseChildhood] a left join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 'Patient Information sheet' Added 6/27/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'PatientInformationsheet'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Patient Information sheet' as [FormName]" +
                                    //    "     , '16-' + convert(varchar, isnull(Clientid, 0)) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "     , ClientId = isnull(ClientID, 0), convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = null " +
                                    //    //case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[PatientInformationsheet] ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 'Insurance Benefit Verification' Added 6/27/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InsuranceBenefitVerification'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Insurance Benefit Verification' as [FormName]" +
                                    //    "     , '17-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(pa.PatientID, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = null " + 
                                    //    //case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[InsuranceBenefitVerification] a left join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 'MN - Mental Health Informed Consent' Added 6/27/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'MentalHealthInformedConsent'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Mental Health Informed Consent' as [FormName]" +
                                    //    "     , '18-' + convert(varchar, isnull(ClientId, 0)) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                    //    "     , ClientId = isnull(ClientId, 0), convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = case when convert(date, PatientSignatureDate) is null then '1900-01-01' else convert(date, PatientSignatureDate) end  " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[MentalHealthInformedConsent] ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 'State Fact Form' Added 7/16/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'StateFactForm'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'State Fact Form' as [FormName]" +
                                    //        "     , '19-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //        "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted " +
                                    //        "     , CompletedBySignatureSignatureDate = case when convert(date, CompletedBySignatureDate) is null then '1900-01-01' else convert(date, CompletedBySignatureDate) end " +
                                    //        "     , CounselorSignatureSignatureDate = null " +
                                    //        //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //        "     , DoctorSignatureSignatureDate = null " +
                                    //        "     , MedicalProviderSignatureSignatureDate = null " +
                                    //        "     , PatientSignatureDate = null " +
                                    //        "     , ProviderSignatureSignatureDate = null" +
                                    //        "     , RequestorSignatureDate = null" +
                                    //        "     , [StaffSignatureDate] = case when convert(date, CompletedBySignatureDate) is null then '1900-01-01' else convert(date, CompletedBySignatureDate) end " +
                                    //        "     , SupervisorSignatureSignatureDate = null " +
                                    //        "  from [dbo].[StateFactForm] a ";
                                    //        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 'Initial Services Plan and Vulnerable Adult Determination' Added 7/16/2023
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InitialServicesPlanandVAD'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Initial Services Plan and Vulnerable Adult Determination' as [FormName]" +
                                    //    "     , '20-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[InitialServicesPlanandVAD] a ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// [dbo].[tblMAARC]   MN - Authorization for Release of Information to the MAARC
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblMAARC'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Authorization for Release of Information to the MAARC' as [FormName]" +
                                    //    "     , '21-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedDate) as [UpdatedOn], a.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[tblMAARC] a ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// [dbo].[tblDAANESNotification]  MN - DAANES Notification Form
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblDAANESNotification'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'MN - DAANES Notification Form' as [FormName]" +
                                    //    "     , '22-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[tblDAANESNotification] a ";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    // [dbo].[ConsenttoTreatmentViaTelehealth]	MN - Consent to Treatment Via Telehealth
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsenttoTreatmentViaTelehealth'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'MN - Consent to Treatment Via Telehealth' as [FormName]" +
                                    //    "     , '23-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[ConsenttoTreatmentViaTelehealth] a ";
                                    //    --where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 11/9/2023 - RI - BHOLD
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'RIBHOLD'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'RI - BHOLD' as [FormName]" +
                                    //    "     , '24-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn] " +
                                    //    ", isnull((select IsDeleted from dbo.SF_DataForms df where df.FormName = 'RI - BHOLD' and pa.DataFormId = df.Id), a.Isdeleted) IsDeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = null " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, a.MedicalStaffSignatureDate) is null then '1900-01-01' else convert(date, a.MedicalStaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[RIBHOLD] a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    //// 11/10/2023 - RI - Health Home Care Plan Review Form
                                    //SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'RIHealthHomeCareReview'", st.ConStr);
                                    //if (SrcDt.Rows.Count == 1)
                                    //{
                                    //    strCmd += " \r\n union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'RI - Health Home Care Plan Review Form' as [FormName]" +
                                    //    "     , '25-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] " +
                                    //    "     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn] " +
                                    //    ", isnull((select IsDeleted from dbo.SF_DataForms df where df.FormName = 'RI - Health Home Care Plan Review Form' and pa.DataFormId = df.Id), a.Isdeleted) IsDeleted " +
                                    //    "     , CompletedBySignatureSignatureDate = null " +
                                    //    "     , CounselorSignatureSignatureDate = null " +
                                    //    //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                    //    "     , DoctorSignatureSignatureDate = null " +
                                    //    "     , MedicalProviderSignatureSignatureDate = null " +
                                    //    "     , PatientSignatureDate = case when convert(date, a.PatientSignatureDate) is null then '1900-01-01' else convert(date, a.PatientSignatureDate) end " +
                                    //    "     , ProviderSignatureSignatureDate = null" +
                                    //    "     , RequestorSignatureDate = null" +
                                    //    "     , [StaffSignatureDate] = case when convert(date, a.StaffSignatureDate) is null then '1900-01-01' else convert(date, a.StaffSignatureDate) end " +
                                    //    "     , SupervisorSignatureSignatureDate = null " +
                                    //    "  from [dbo].[RIHealthHomeCareReview] a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID";
                                    //    //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    //}
                                    #endregion
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveAnswerSignatures(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(formDaysBack), xForms, null);
                                }
                                else { rCodes.ExceptMsg = "No AnswerSignature table."; rCodes.IsResult = true; }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_vw3pbill":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAuthBills(SrcDt, st.SiteCode, st.WorkDate.Value.Date, false, null);
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, SrcDt.Rows.Count,
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '" 
                                            + st.SiteCode + "' ", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_vw3pbillsub":
                                strCmd = strCmd.Replace("Select ", "Select distinct ");
                                strCmd = strCmd.Replace("[CptMod] CptMod", "isnull([CptMod], ':(') CptMod");
                                strCmd = strCmd.Replace("[pySUBSID] pySUBSID", "isnull([pySUBSID], ':(') pySUBSID");
                                strCmd = strCmd.Replace("[charge] charge", "isnull(charge, 0) charge");
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if ((st.SiteCode == "B41") || (st.SiteCode == "B42"))
                                {
                                    rCodes = sd.SaveAuthBillsub(SrcDt, st.SiteCode, st.WorkDate.Value.Date, false, null);
                                }
                                else
                                {
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.AddDays(DaysBack).Date, db);
                                }
                                if ((st.RowTrax.HasValue))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName, SrcDt.Rows.Count,
                                            int.Parse(sm.GetTableData("Aztbl", "Select tblcnt = count(1) from " + st.TaskName + " where SiteCode = '"
                                            + st.SiteCode + "' ", sm.ConnectionString).Rows[0][0].ToString()), null);
                                    }
                                }
                                break;
                            case "pats.tbl_payerclthistory":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePayerCltHistory(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), false, null);
                                break;
                            case "pats.tbl_treatmentlevel":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveTreatmentLevel(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_admissionassessmentsubstanceusehistory":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAdmissionAssessmentSubstanceuseHistory(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_assessmentsubstanceusehistory":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveAssessmentSubstanceuseHistory(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_comprehensiveassessmentform ":
                            case "pats.tbl_comprehensiveassessmentform":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveComprehensiveAssessmentForm(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_pa":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_mncomprehensiveassessment":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'MNComprehensiveAssessment'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveMNCA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_mncomprehensiveassessmentlevelofcare":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'MNComprehensiveAssessmentlevelofcare'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveMNCALOC(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_vacomprehensiveassessment":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'VAComprehensiveAssessment'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveVACA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_vacomprehensiveassessmentsummary":
                            case "pats.pats.tbl_vacomprehensiveassessmentsummary":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'vacomprehensiveassessmentsummary'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveVACASummary(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newadmissionassessment":
                            case "pats.pats.tbl_newadmissionassessment":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'NewAdmissionassessment'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewAdmissionAssessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newadmissionassessmentasamdimension2":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'NewAdmissionassessmentASAMDimension2'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewAdmissionAssessmentASAMDimension2(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newadmissionassessmentasamdimension4":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'NewAdmissionassessmentASAMDimension4'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewAdmissionAssessmentASAMDimension4(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newadmissionassessmentasamdimension5":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'NewAdmissionassessmentASAMDimension5'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewAdmissionAssessmentASAMDimension5(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newadmissionassessmentasamdimension6":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'NewAdmissionassessmentASAMDimension6'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewAdmissionAssessmentASAMDimension6(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessment":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessment'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewPeriodicReassessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessmentcounselorreview":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessmentcounselorreview'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.Savenewperiodicreassessmentcounselorreview(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                    _ = sm.ExeSqlCmd("exec [pats].[MergeFormSignaturesPeriodicReassessments] '" + st.SiteCode + "'", sm.ConnectionString);

                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessmentd2":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessmentd2'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewPeriodicReassessmentD2(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessmentd3":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessmentd3'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewPeriodicReassessmentD3(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessmentd4":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessmentd4'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewPeriodicReassessmentD4(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessmentd5":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessmentd5'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewPeriodicReassessmentD5(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_newperiodicreassessmentd6":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newperiodicreassessmentd6'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewPeriodicReassessmentD6(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_financialhardshipapplication":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveFinancialHardshipApplication(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_pacounselorreview":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePACounselorReview(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_padimension1":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePADimension1(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_padimension2":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePADimension2(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_padimension3":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePADimension3(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_padimension4":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePADimension4(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_padimension5":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePADimension5(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_padimension6":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavePADimension6(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "ctrl.tbl_drodownlistitems":
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SavedropDownListItems(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                break;
                            case "pats.tbl_consenttomarketing":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'consenttomarketing'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveConsenttoMarketing(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists";
                                }
                                break;
                            case "pats.tbl_newdischargetransferplanform":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'newdischargetransferplanform'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveNewDischargeTransferPlanForm(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists";
                                }
                                break;
                            case "pats.tbl_mntreatmentservicereview":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'mntreatmentservicereview'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveMNTreatmentServiceReview(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists";
                                }
                                break;
                            case "pats.tbl_takehomeagreementanddiversioncontrol":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'takehomeagreementanddiversioncontrol'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveTakeHomeAgreementandDiversionControl(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists";
                                }
                                break;
                            case "sf.tbl_dataforms":
                            case "pats.tbl_sf_dataforms":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SF_DataForms'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    if (st.SiteCode.ToUpper() == "LAB")
                                    {
                                        strCmd = strCmd.Replace(", [dsID] dsID", "").Replace(", [dsID]", "")
                                            .Replace(", [EnrollmentDate] EnrollmentDate", "").Replace(", [EnrollmentDate]", "");
                                    }
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveDataForms(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists";
                                }
                                break;
                            case "pats.tbl_takehomeriskassessment":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'TakeHomeRiskAssessment'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    if (st.SiteCode.ToUpper() == "LAB")
                                    {
                                        //strCmd = strCmd.Replace(", [dsID] dsID", "").Replace(", [dsID]", "")
                                            //.Replace(", [EnrollmentDate] EnrollmentDate", "").Replace(", [EnrollmentDate]", "");
                                    }
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveTakeHomeRiskAssessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists";
                                }
                                break;
                            case "pats.tbl_smstextconsentform":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SMSTextConsentForm'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveSMSTextConsentForm(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                            case "pats.tbl_sf_patientpreadmission":
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SF_PatientPreAdmission'", st.ConStr);
                                if (SrcDt.Rows.Count == 1)
                                {
                                    strCmd += " Where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveSFPatientPreAdmission(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null);
                                }
                                else
                                {
                                    rCodes.IsResult = true;
                                    rCodes.ExceptMsg = "Table does not exists.";
                                }
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        rCodes.ExceptMsg = e.Message;
                        if (e.InnerException != null)
                        {
                            rCodes.ExceptInnerMsg = e.InnerException.Message;
                        }
                    }
                    //Close out SubTask
                    task.RowCount = rCodes.RowsProcessed;
                    task.RowsIns = rCodes.RowsIns;
                    task.RowsUpd = rCodes.RowsUpd;
                    task.ErrorMessage = rCodes.ExceptMsg + "     " + rCodes.ExceptInnerMsg;
                    if (task.ErrorMessage.Length > 500) { task.ErrorMessage = task.ErrorMessage.Substring(0, 499); }
                    task.Status = rCodes.IsResult ? 19 : 20;
                    TimeSpan ts = DateTime.Now.Subtract(st_start);
                    task.Duration = ts.Hours.ToString().PadLeft(2, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');
                    task.LastModAt = DateTime.Now;
                    ts = DateTime.Now.Subtract(pt_start);
                    ptask.Duration = ts.Hours.ToString().PadLeft(2, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0');
                    ptask.RowCount += 1;
                    ptask.LastModAt = DateTime.Now;
                    db.SaveChanges();
                }
                // Update Parent Task
                ptask.Status = 19;
                TimeSpan pts = DateTime.Now.Subtract(pt_start);
                ptask.Duration = pts.Hours.ToString().PadLeft(2, '0') + ":" + pts.Minutes.ToString().PadLeft(2, '0') + ":" + pts.Seconds.ToString().PadLeft(2, '0');
                ptask.LastModAt = DateTime.Now; 
                db.SaveChanges();
                Tasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
                if (args.Length == 0)
                {
                    pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
                }
                else
                {
                    switch (args[0].ToString())
                    {
                        case "1":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMSGlobal" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "2":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                                && (x.TaskName == "Central ETL P1" || x.TaskName == "Eastern ETL P1" || x.TaskName == "Mountain ETL P1" || x.TaskName == "Pacific ETL P1")).ToList();
                            break;
                        case "3":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                            && x.TaskName != "Central ETL P1" && x.TaskName != "Eastern ETL P1" && x.TaskName != "Mountain ETL P1" && x.TaskName != "Pacific ETL P1"
                            && x.TaskName != "Central ETL P2" && x.TaskName != "Eastern ETL P2" && x.TaskName != "Mountain ETL P2" && x.TaskName != "Pacific ETL P2"
                            && x.TaskName != "SAMMSGlobal").ToList();
                            break;
                        case "4":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                                && (x.TaskName == "Central ETL P2" || x.TaskName == "Eastern ETL P2" || x.TaskName == "Mountain ETL P2" || x.TaskName == "Pacific ETL P2")).ToList();
                            break;
                        case "5":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "Samms-LAB" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "6":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "Samms-Forms" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "7":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-Notes" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "8":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-INV" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "9":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-DartSvc" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "10":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-Dose" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "11":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-Orders" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "12":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-PPA" && x.RunAt < DateTime.Now).ToList();
                            break;
                        case "13":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.TaskName == "SAMMS-ETL-UAR" && x.RunAt < DateTime.Now).ToList();
                            break;
                        default:
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
                            break;
                    }
                }
            }
        }
    }
}
