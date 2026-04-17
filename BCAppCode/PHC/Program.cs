using System;
using System.Collections.Generic;
using System.Data;
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
                            && (x.TaskName == "Central ETL" || x.TaskName == "Estern ETL" || x.TaskName == "Mountain ETL" || x.TaskName == "Pacific ETL")).ToList();
                        break;
                    case "3":
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                        && x.TaskName != "Central ETL" && x.TaskName != "Estern ETL" && x.TaskName != "Mountain ETL" && x.TaskName != "Pacific ETL"
                        && x.TaskName != "SAMMSGlobal").ToList();
                        break;
                    default:
                        pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
                        break;
                }
            }
            
            foreach(var pt in pTasks.Where(x => x.ParentTaskId == null).OrderBy(z => z.WorkDate).ThenBy(o => o.RunAt).ToList())
            {
                BHG_DR_LIB.Models.TblTasks ptask = db.TblTasks.Where(x => x.TaskId == pt.TaskId).FirstOrDefault();
                ptask.Status = 18;
                DateTime pt_start = DateTime.Now;
                ptask.Duration = "0";
                if (ptask.RowCount == null) { ptask.RowCount = 0; }
                string Lasttbl = "";
                string PrevSite = "";
                foreach (var st in Tasks.Where(x => x.ParentTaskId == pt.TaskId).OrderBy(o => o.TaskName).ThenBy(b => b.SiteCode).ThenBy(d => d.FromTblVw).ToList())
                {
                    DateTime st_start = DateTime.Now;
                    BHG_DR_LIB.Models.TblTasks task = db.TblTasks.Where(x => x.TaskId == st.TaskId).FirstOrDefault();
                    task.Status = 18;
                    task.ErrorMessage = "";
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
                    strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw;
                    string strWhere = st.WhereCondition.Replace("@SiteCode", "'" + st.SiteCode + "'")
                        .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(-10).ToShortDateString() + "'")
                        //.Replace("@EnrollCutoff", "'" + Cutoff + "'")
                        .Replace("@Samms", "'SAMMS'");
                    try
                    {
                        switch (st.TaskName.ToLower())
                        {
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
                                        ",  pp.ReasonSeekingTreatment, pp.AccomodationNeeded, pp.ClientAddress, pp.Comments " + Environment.NewLine +
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
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveGlobalConsents(SrcDt, null);
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
                                rCodes = sd.Save3pElig(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(-10), true, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_bills":
                                //strCmd += " Where " + strWhere + " " + st.SortOrder;
                                strCmd += " where year(billDate) = " + st.WorkDate.Value.AddDays(-10).Year.ToString() + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if (SrcDt.Rows.Count > 0)
                                {
                                    rCodes = sd.SaveBills(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(-10), true, null);
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
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes.IsResult = sd.SaveCheckIn(SrcDt, st.SiteCode, st.WorkDate.Value, null);
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
                                if (st.SiteCode == "VBRA")
                                {
                                    strCmd += " where " + strWhere + " " + st.SortOrder;
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveClaims(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(-14).Date, true, null);
                                }
                                else
                                {
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, db);
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

                                    strCmd = "select SiteCode = '" + st.SiteCode + "', COWID = c.id, CltID = p.PatientID, c.preadmissionid, dttime, reasonforthisAssessment, c.RestingPulseRate" + Environment.NewLine +
                                        ", drp.dropdownlistitem as RestingPulseRatedesc, c.giupset, dgi.dropdownlistitem as GIUpsetdesc" + Environment.NewLine +
                                        ", c.sweating,dsw.dropdownlistitem as Sweatingdesc, c.tremor, dt.dropdownlistitem as Tremordesc" + Environment.NewLine +
                                        ", c.Restlessness  ,dr.dropdownlistitem as Restlessnessdesc, c.Yawning, dy.dropdownlistitem as Yawningdec" + Environment.NewLine +
                                        ", c.PupilSize, dps.dropdownlistitem as PupilSizedesc, c.AnxietyOrIrritability, doi.dropdownlistitem as AnxietyOrIrritabilitydesc" + Environment.NewLine +
                                        ", c.BoneOrJointAches, dbj.dropdownlistitem as BoneOrJointAchesdesc, c.GoosefleshSkin, dgf.dropdownlistitem as GoosefleshSkindesc" + Environment.NewLine +
                                        ", c.RunnyNoseOrTearing, drnt.dropdownlistitem as RunnyNoseOrTearingdesc, c.CompletedBy, c.CreatedOn, c.CreatedBy, c.UpdatedBy, c.UpdatedOn" + Environment.NewLine;
                                    if (lstCols.Where(x => x.ColName.ToLower() == "isactive").FirstOrDefault() != null) { strCmd += ", c.IsActive"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "patientsignature").FirstOrDefault() != null) { strCmd += ", c.PatientSignature"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "isdeleted").FirstOrDefault() != null) { strCmd += ", c.IsDeleted"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "clientsignaturedate").FirstOrDefault() != null) { strCmd += ", c.ClientSignatureDate"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "version").FirstOrDefault() != null) { strCmd += ", c.Version"; }
                                    if (lstCols.Where(x => x.ColName.ToLower() == "staffnamesignature").FirstOrDefault() != null) { strCmd += ", c.staffNameSignature"; }
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
                                rCodes.IsResult = sd.SaveCodes(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
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
                                //strCmd += " Where Year(dsdtstart) = 2021 order by 1, 2";
                                strCmd += " Where convert(date,dsdtstart) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
                                    + "' or convert(date,dsDtAdded) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
                                    + "' or convert(date,dsUpdate) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
                                    + "' or convert(date,dsBilled) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
                                    + "' or convert(date,dsSigDate) >= '" + st.WorkDate.Value.AddDays(-14).ToShortDateString()
                                    + "' or dsClt <= 0 order by 1, 2";
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, null);
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
                                strWhere = "Year(dtDate) = " + st.WorkDate.Value.AddDays(-10).Year;
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if ((st.SiteCode == "V10A") || (st.SiteCode == "VMIN"))
                                {
                                    rCodes = sd.SaveDoses(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(-10).Year, null);
                                }
                                else
                                {
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value.AddDays(-10), null);
                                }
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                        sd.SaveRowTrax(st.SiteCode, st.WorkDate.Value.Date, st.TaskName,
                                            int.Parse(sm.GetTableData("tbllcl", "select tblcnt = count(1) from " + st.SrcSchema + "." + st.FromTblVw + " where CltID > 0 and dtMedDate > '12/31/2019'",
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
                            case "pats.tbl_enrollment":
                                if (st.SiteCode != "Lab")
                                {
                                    if (st.FromTblVw == "vw_Enrollment")
                                    {
                                        DataTable tblcols = sm.GetTableData("tcols", "select name, column_id from sys.all_columns c where c.object_id = " +
                                            "(select object_id from sys.tables where upper(name) = 'vw_Enrollment' and name = 'Modality')", st.ConStr);
                                    if (tblcols.Rows.Count > 0)
                                        {
                                            strCmd = "Select " + strFlds + ", Modality from " + st.SrcSchema + "." + st.FromTblVw;
                                        }
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
                            case "pats.tbl_formssammsclient":
                                // Purge Forms SAMMS
                                if (st.SiteCode != "PHC")
                                {
                                    string scmd = "select fscsid, f.fscdate, fsccltid, f.fscsite, count(1) cnt from tblFormsSAMMSClient f " +
                                            //" inner join tblSites s on (f.fscsite = s.sID)" +
                                            "where fscsite not in (25, 100) " +  //fscCLTID < 0 and 
                                            "group by fscsid, f.fscDATE, fscCLTID, f.fscSite having f.fscDATE >= '1/1/2021' order by 1, 2, 3, 4";
                                    _ = sd.SaveFormCounts(sm.GetTableData("frms", scmd, @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;"), null);
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
                                    strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 38, 99, 100, 106, 115, 118) ";
                                    // Do we need the Clients < 0 ?
                                    strCmd += strWhere + " order by 1, 2";
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;");
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, st.TaskName, st.SiteCode, st.WorkDate.Value.AddDays(-10), null);
                                }
                                else
                                {
                                    strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 38, 99, 100, 106, 115, 118) ";
                                    // Do we need the Clients < 0 ?
                                    strCmd += strWhere + " order by 1, 2";
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = bldr.BulkDartsSrvLoader(SrcDt, st.TaskName, st.SiteCode, st.WorkDate.Value.AddDays(-10), null);
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
                                strCmd += " Where " + st.WhereCondition.Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(-10).ToShortDateString() + "'")
                                .Replace("@SiteCode", "'" + st.SiteCode + "'");
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                if (st.FromTblVw == "vw_PayerClt_INACTIVE")
                                {
                                    rCodes = sd.RemovePayerClients(SrcDt, st.SiteCode, DateTime.Today, true, null);
                                }
                                else
                                {
                                    rCodes = sd.SavePayerClient(SrcDt, st.SiteCode, DateTime.Today, true, null);
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
                                rCodes.IsResult = sd.SaveAuths(SrcDt, st.SiteCode, null);
                                rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
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
                            case "pats.tbl_uaresultdetail":
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
                            case "pats.tbl_uaresults":
                                //strCmd += " Where 1 = 1 " + st.SortOrder;
                                strCmd += " Where " + strWhere + " " + st.SortOrder;
                                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                rCodes = sd.SaveUAResults(SrcDt, st.SiteCode, st.WorkDate.Value, true, null);
                                //rCodes = sd.SaveUAResults(SrcDt, st.SiteCode, st.WorkDate.Value, false, null);
                                //rCodes.RowsProcessed = SrcDt.Rows.Count;
                                if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
                                {
                                    if (st.RowTrax.Value)
                                    {
                                    }
                                }
                                break;
                            case "pats.tbl_dbo_formquestionanswers":
                                //Check if Forms table exists
                                SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'Form'", st.ConStr);
                                // if yes then
                                if (SrcDt.Rows.Count == 1)
                                {
                                    DateTime wrkdt = st.WorkDate.Value.AddDays(-10).Date;
                                    //switch (st.SiteCode)
                                    //{
                                    //    //case "D07":
                                    //    //case "V20":
                                    //      //  wrkdt = st.WorkDate.Value.Date; 
                                    //       // break;
                                    //    default:
                                    //        wrkdt = DateTime.Parse("1/1/2000");
                                    //        break;
                                    //}
                                    strCmd = "select SiteCode, FormName, convert(varchar(100),FormId) FormId, PreAdmissionId, ClientId, QuestionId " +
                                        ", QuestionOrderId = isnull(x.QuestionOrderId, Row_Number() over(Partition by x.FormName, x.FormId, x.ClientId, x.QuestionId order by x.QuestionId, x.AnswerSeq)) " +
                                        ", QuestionText, OptionId, AnswerValue, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (select SiteCode = '" + st.SiteCode.ToString() +
                                        "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId, f.IsDeleted /*, f.IsChildForm*/ " +
                                        ", QuestionId = isnull(q.Id, 0), QuestionOrderId = q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value, AnswerSeq = a.Id " +
                                        " from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) left join Question q on (ft.Id = q.FormTemplateId) " +
                                        " left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId) where a.Value is not null and (f.CreatedOn >= '" + wrkdt.ToShortDateString() +
                                        "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() + "') " +
                                        " union select SiteCode = '" + st.SiteCode.ToString() + "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId, f.IsDeleted  /*, f.IsChildForm */ " +
                                        " , QuestionId = isnull(q.Id, 0), QuestionOrderId = q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value, AnswerSeq = a.Id " +
                                        " from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) left join Question q on (ft.Id = q.FormTemplateId) " +
                                        " left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId) where q.Id is null and (f.CreatedOn >= '" + wrkdt.ToShortDateString() +
                                        "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() + "')) x";

                                    //" --Suicide Severity Rating Scale --(For ETL’ing to pats.tbl_dbo_FormQuestionAnswers) "
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SuicideSeverityRatingScale'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by w.FormName, w.FormId, w.ClientId, w.QuestionId order by w.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Suicide Severity Rating Scale' as [FormName] " +
                                        ", '1-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , PreAdmissionId, ClientId, 0 as QuestionID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date,CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date,ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        " from [dbo].[SuicideSeverityRatingScale]) w ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --Health Questionnaire " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'HealthQuestionnaire'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by y.FormName, y.FormId, y.ClientId, y.QuestionId order by y.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Health Questionnaire' as [FormName] " +
                                        ", '2-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID]  " +
                                        "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date, Createddate) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, Modifieddate) as [UpdatedOn], Isdeleted " +
                                        " from [dbo].[HealthQuestionnaire]) y ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --Infectious Disease And Behavioral Screen " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InfectiousDiseaseAndBehavioralScreen'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by z.FormName, z.FormId, z.ClientId, z.QuestionId order by z.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Infectious Disease And Behavioral Screen' as [FormName] " +
                                        ", '3-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "  from [dbo].[InfectiousDiseaseAndBehavioralScreen]) z ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --'Consent to Treatment with an Approved Narcotic' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsentToTreatmentWithAnApprovedNarcotic'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by q.FormName, q.FormId, q.ClientId, q.QuestionId order by q.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Consent to Treatment with an Approved Narcotic'  as [FormName] " +
                                        ", '4-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "  from [dbo].[ConsentToTreatmentWithAnApprovedNarcotic]) q";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --'Financial Hardship Application'  " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'FinancialHardshipApplication'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, cltId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by s.FormName, s.FormId, s.cltId, s.QuestionId order by s.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Financial Hardship Application'   as [FormName] " +
                                        ", '5-' + convert(varchar, cltId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , PreAdmissionId, cltId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "  from [dbo].[FinancialHardshipApplication]) s";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --'Comprehensive Assessment Form'  " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ComprehensiveAssessmentForm'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by t.FormName, t.FormId, t.ClientId, t.QuestionId order by t.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Comprehensive Assessment Form' as [FormName] " +
                                        ", '6-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "  from [dbo].[ComprehensiveAssessmentForm]) t";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --'Admission Assessment' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'AdmissionAssessment'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                        ", QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) " +
                                        ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName] " +
                                        ", '7-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue " +
                                        "      , Createdby, convert(date, CreatedOn) as [CreatedOn], ModifiedBy as [UpdatedBy], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "  from [dbo].[AdmissionAssessment]) v";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --'Treatment Plan' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblTP17REVIEW'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID " +
                                            ", QuestionOrderID = Row_Number() over(Partition by tp.FormName, tp.FormId, tp.ClientId, tp.QuestionId order by tp.QuestionId) " +
                                            ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from( " +
                                            "Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                            ", '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                            ", null PreAdmissionId, ABS(tprCLTID) ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID] " +
                                            ", TprTYPE as AnswerValue, tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                            ", Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW " +
                                            " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                            ", '8-2-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                            ", null PreAdmissionId, ABS(tprCLTID) ClientId, 2 as QuestionID, 1 as QuestionOrderID, 'Treatment Phase Type' as QuestionText, null as [OptionID] " +
                                            ", tpTreatmentPhase as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                            ", Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW " +
                                            " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                            ", '8-3-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                            ", null PreAdmissionId, ABS(tprCLTID) ClientId, 3 as QuestionID, 1 as QuestionOrderID, 'Next Due' as QuestionText, null as [OptionID] " +
                                            ", convert(varchar, tprNEXT) as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] " +
                                            ", Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW ";
                                    }
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select * from sys.all_columns c where c.object_id = (select object_id from sys.tables where name = 'tblTP17REVIEW') and name = 'tprReviewFrequency'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                        ", '8-4-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                        ", null PreAdmissionId, ABS(tprCLTID) ClientId, 4 as QuestionID, 1 as QuestionOrderID, 'Review Frequency' as QuestionText, null as [OptionID] " +
                                        ", rtrim(substring(tprReviewFrequency, 6, LEN(tprReviewFrequency) - 5)) as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn] " +
                                        ", null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW  ) tp ";

                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(Modifieddate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    else { strCmd += ") tp "; }

                                    // Level Justification
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblORDERREQ'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += Environment.NewLine + " union Select SiteCode, [FormName], FormID = FormId + '-' + convert(varchar,Row_Number() " + Environment.NewLine +
                                            " over (Partition by tp2.FormName, tp2.FormId, tp2.ClientId, tp2.QuestionId order by tp2.AnswerValue)) " + Environment.NewLine +
                                            ", PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by tp2.FormName, tp2.FormId, tp2.ClientId, tp2.QuestionId order by tp2.AnswerValue) " + Environment.NewLine +
                                            ", QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (" + Environment.NewLine +
                                            " select SiteCode = '" + st.SiteCode.ToString() + "', FormName = 'Level Justification' " + Environment.NewLine +
                                            ", FormID = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ " + Environment.NewLine +
                                            ", PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 1, QuestionText = 'Effective Date', OptionID = 0, AnswerValue = convert(varchar,EffectiveDate, 101) " + Environment.NewLine +
                                            ", Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) " + Environment.NewLine +
                                            ", IsDeleted = Case when cltID < 0 then 1 else 0 end from [dbo].[tblORDERREQ] " + Environment.NewLine +
                                            " where status = 'Approved' and(Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST') " + Environment.NewLine +
                                            " union select SiteCode = '" + st.SiteCode.ToString() + "', FormName = 'Level Justification' " + Environment.NewLine +
                                            ", FormID = '9-2-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ " + Environment.NewLine +
                                            ", PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 2, QuestionText = 'Expiration Date', OptionID = 0, AnswerValue = convert(varchar,expirationdate, 101) " + Environment.NewLine +
                                            ", Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) " + Environment.NewLine +
                                            ", IsDeleted = Case when cltID< 0 then 1 else 0 end from [dbo].[tblORDERREQ] " + Environment.NewLine +
                                            " where status = 'Approved' and(Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST')) tp2";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }

                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveFormQuestionAnswers(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(-10), false, null);
                                    // add Execute Store Procedure for BAM here
                                    var tblr6 = sm.ExecStrPro("pats.BAMMerge", "@sitecode", st.SiteCode, sm.ConnectionString);
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
                                    DateTime wrkdt = st.WorkDate.Value.AddDays(-10).Date;
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
                                        " from (select SiteCode = '" + st.SiteCode.ToString() + "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.UpdatedOn, IsDeleted  from dbo.Form f left join FormTemplate ft \r\n" +
                                        " on (f.FormTemplateId = ft.Id) where (f.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() +
                                        "') union select SiteCode = '" + st.SiteCode.ToString() +
                                        "', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.UpdatedOn, Isdeleted from dbo.Form f left join FormTemplate ft on(f.FormTemplateId = ft.Id) \r\n" +
                                        " where (f.CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt.ToShortDateString() + "') or Isdeleted = 1) x \r\n";
                                    //" --Suicide Severity Rating Scale --(For ETL’ing to pats.tbl_dbo_FormQuestionAnswers) "
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'SuicideSeverityRatingScale'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Suicide Severity Rating Scale' as [FormName]" +
                                        "     , '1-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "     , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "     , CompletedBySignatureSignatureDate = null " +
                                        "     , CounselorSignatureSignatureDate = null " + 
                                        //case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  " +
                                        "     , DoctorSignatureSignatureDate = null " +
                                        "     , MedicalProviderSignatureSignatureDate = convert(date, MedicalProviderSignatureDate) " +
                                        "     , PatientSignatureDate = null" +
                                        "     , ProviderSignatureSignatureDate = null" +
                                        "     , RequestorSignatureDate = null" +
                                        "     , [StaffSignatureDate] = case when convert(date, StaffSignatureDate) is null then '1900-01-01' else convert(date, StaffSignatureDate) end " +
                                        "     , SupervisorSignatureSignatureDate = case when convert(date, SupervisorSignatureDate) is null then '1900-01-01' else convert(date, SupervisorSignatureDate) end " +
                                        "  from [dbo].[SuicideSeverityRatingScale] ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedOn, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " 
                                    }
                                    //"  --Health Questionnaire " 
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'HealthQuestionnaire'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Health Questionnaire' as [FormName]" +
                                        "      , '2-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , ClientId, convert(date, Createddate), convert(date, Modifieddate), Isdeleted " +
                                        "      , CompletedBySignatureSignatureDate = null" +
                                        "      , CounselorSignatureSignatureDate = null " +
                                        "      , DoctorSignatureSignatureDate = case when convert(date, DoctorSignatureDate) is null then '1900-01-01' else convert(date, DoctorSignatureDate) end " +
                                        "      , MedicalProviderSignatureSignatureDate = null " +
                                        "      , PatientSignatureDate = null " +
                                        "      , ProviderSignatureSignatureDate = case when convert(date, NurseSignatureDate) is null then '1900-01-01' else convert(date, NurseSignatureDate) end " +
                                        "      , RequestorSignatureDate = null " +
                                        "      , StaffSignatureDate = null " +
                                        "      , SupervisorSignatureSignatureDate = null " +
                                        "  from [dbo].[HealthQuestionnaire] ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --Infectious Disease And Behavioral Screen " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'InfectiousDiseaseAndBehavioralScreen'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Infectious Disease And Behavioral Screen' as [FormName]" +
                                        "      , '3-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "      , CompletedBySignatureSignatureDate = null" +
                                        "      , CounselorSignatureSignatureDate = null" +
                                        "      , DoctorSignatureSignatureDate = null " +
                                        "      , MedicalProviderSignatureSignatureDate = null " +
                                        "      , PatientSignatureDate = null " +
                                        "      , ProviderSignatureSignatureDate = case when convert(date, MedicalStaffSignatureDate) is null then '1900-01-01' else convert(date, MedicalStaffSignatureDate) end " +
                                        "      , RequestorSignatureDate = null" +
                                        "      , StaffSignatureDate = null " +
                                        "      , SupervisorSignatureSignatureDate = null " +
                                        " from [dbo].[InfectiousDiseaseAndBehavioralScreen] ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  --Consent to Treatment with an Approved Narcotic " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ConsentToTreatmentWithAnApprovedNarcotic'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Consent to Treatment with an Approved Narcotic' as [FormName]" +
                                        "      , '4-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "      , CompletedBySignatureSignatureDate = null" +
                                        "      , CounselorSignatureSignatureDate = null" +
                                        "      , DoctorSignatureSignatureDate = case when convert(date,DoctorSignatureDate) is null then '1900-01-01' else convert(date,DoctorSignatureDate) end " +
                                        "      , MedicalProviderSignatureSignatureDate = null " +
                                        "      , PatientSignatureDate = case when convert(date,PatientSignatureDate) is null then '1900-01-01' else convert(date,PatientSignatureDate) end  " +
                                        "      , ProviderSignatureSignatureDate = null " +
                                        "      , RequestorSignatureDate = null" +
                                        "      , StaffSignatureDate = case when convert(date,StaffSignatureDate) is null then '1900-01-01' else convert(date,StaffSignatureDate) end " +
                                        "      , SupervisorSignatureSignatureDate = null " +
                                        " from [dbo].[ConsentToTreatmentWithAnApprovedNarcotic] ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  -- 'Financial Hardship Application' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'FinancialHardshipApplication'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Financial Hardship Application' as [FormName]" +
                                        "      , '5-' + convert(varchar, CltId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , CltId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "      , CompletedBySignatureSignatureDate = null" +
                                        "      , CounselorSignatureSignatureDate = null" +
                                        "      , DoctorSignatureSignatureDate = null " +
                                        "      , MedicalProviderSignatureSignatureDate = null " +
                                        "      , PatientSignatureDate = case when convert(date,FHAPatientSignatureDate) is null then '1900-01-01' else convert(date,FHAPatientSignatureDate) end " +
                                        "      , ProviderSignatureSignatureDate = null " +
                                        "      , RequestorSignatureDate = null" +
                                        "      , StaffSignatureDate = null " +
                                        "      , SupervisorSignatureSignatureDate = null " +
                                        " from [dbo].[FinancialHardshipApplication] ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  -- 'Comprehensive Assessment Form' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'ComprehensiveAssessmentForm'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Comprehensive Assessment Form' as [FormName]" +
                                        "      , '6-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, id) as [FormID] " +
                                        "      , ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "      , CompletedBySignatureSignatureDate = null" +
                                        "      , CounselorSignatureSignatureDate = null" +
                                        "      , DoctorSignatureSignatureDate = null " +
                                        "      , MedicalProviderSignatureSignatureDate = null " +
                                        "      , PatientSignatureDate = case when convert(date,CAPatientSignatureDate) is null then '1900-01-01' else convert(date,CAPatientSignatureDate) end " +
                                        "      , ProviderSignatureSignatureDate = case when convert(date,CAProviderSignatureDate) is null then '1900-01-01' else convert(date,CAProviderSignatureDate) end " +
                                        "      , RequestorSignatureDate = null" +
                                        "      , StaffSignatureDate = case when convert(date,CAStaffSignatureDate) is null then '1900-01-01' else convert(date,CAStaffSignatureDate) end " +
                                        "      , SupervisorSignatureSignatureDate = case when convert(date,CASupervisorSignatureDate) is null then '1900-01-01' else convert(date,CASupervisorSignatureDate) end " +
                                        " from [dbo].[ComprehensiveAssessmentForm] ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  -- 'Admission Assessment' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'AdmissionAssessment'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName]" +
                                        "      , '7-' + convert(varchar, aa.ClientId) + '-' + convert(varchar, aa.PreAdmissionId) + '-' + convert(varchar, aa.id) as [FormID] " +
                                        "      , aa.ClientId, convert(date, CreatedOn) as [CreatedOn], convert(date, ModifiedOn) as [UpdatedOn], Isdeleted " +
                                        "      , CompletedBySignatureSignatureDate = null" +
                                        "      , CounselorSignatureSignatureDate = null" +
                                        "      , DoctorSignatureSignatureDate = null " +
                                        "      , MedicalProviderSignatureSignatureDate = null " +
                                        "      , PatientSignatureDate = case when convert(date,AdmissionAssessmentPatientSignatureDate) is null then '1900-01-01' else convert(date,AdmissionAssessmentPatientSignatureDate) end " +
                                        "      , ProviderSignatureSignatureDate = case when convert(date,AdmissionAssessmentProviderSignatureDate) is null then '1900-01-01' else convert(date,AdmissionAssessmentProviderSignatureDate) end " +
                                        "      , RequestorSignatureDate = null" +
                                        "      , StaffSignatureDate = case when convert(date,AdmissionAssessmentStaffSignatureDate) is null then '1900-01-01' else convert(date,AdmissionAssessmentStaffSignatureDate) end " +
                                        "      , SupervisorSignatureSignatureDate = case when convert(date,AdmissionAssessmentSupervisorSignatureDate) is null then '1900-01-01' else convert(date,AdmissionAssessmentSupervisorSignatureDate) end " +
                                        " from [dbo].[AdmissionAssessment] aa inner join [dbo].[AdmissionAssessmentSummary] aas on aa.Id = aas.AdmissionAssessmentId and aa.PreAdmissionId = aas.PreAdmissionId ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    //"  -- 'Treatment Plan' " +
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblTP17REVIEW'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union " +
                                        " select distinct SiteCode, FormName, FormID, ClientId, CreatedOn, UpdatedOn, IsDeleted " +
                                        ", CompletedBySignatureSignatureDate, CounselorSignatureSignatureDate, DoctorSignatureSignatureDate, MedicalProviderSignatureSignatureDate " +
                                        ", PatientSignatureDate, ProviderSignatureSignatureDate, RequestorSignatureDate, StaffSignatureDate, SupervisorSignatureSignatureDate from( " +
                                        " Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                        ", '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                        ", null PreAdmissionId, tprCLTID ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID], TprTYPE as AnswerValue " +
                                        ", tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end " +
                                        ", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null " +
                                        ", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end " +
                                        ", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end " +
                                        ", RequestorSignatureDate = null " +
                                        ", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                        ", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from [dbo].tblTP17REVIEW ) tp"; 
                                        //" union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                        //", '8-2-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                        //", null PreAdmissionId, ABS(tprCLTID) ClientId, 2 as QuestionID, 1 as QuestionOrderID, 'Treatment Phase Type' as QuestionText, null as [OptionID], tpTreatmentPhase as AnswerValue " +
                                        //", tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end " +
                                        //", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null " +
                                        //", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end " +
                                        //", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end " +
                                        //", RequestorSignatureDate = null " +
                                        //", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                        //", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from[dbo].tblTP17REVIEW " +
                                        //" union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Treatment Plan' as [FormName] " +
                                        //", '8-3-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] " +
                                        //", null PreAdmissionId, ABS(tprCLTID) ClientId, 3 as QuestionID, 1 as QuestionOrderID, 'Next Due' as QuestionText, null as [OptionID], convert(varchar, tprNEXT) as AnswerValue " +
                                        //", tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end " +
                                        //", CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null " +
                                        //", PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end " +
                                        //", ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end " +
                                        //", RequestorSignatureDate = null " +
                                        //", StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end " +
                                        //", SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from[dbo].tblTP17REVIEW ";
                                    }
                                    SrcDt = sm.GetTableData(st.FromTblVw, "select name from sys.tables t where name = 'tblORDERREQ'", st.ConStr);
                                    if (SrcDt.Rows.Count == 1)
                                    {
                                        strCmd += " union Select SiteCode = '" + st.SiteCode.ToString() + "', 'Level Justification' as [FormName] " +
                                            ", [FormID] = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-1' /*+ convert(varchar, tprTPID)*/ " +
                                            ", ClientId = cltID /*, Createdby = Staff*/, [CreatedOn] = convert(date, DateAdded) " +
                                            "/*, [UpdatedBy] = StatusUser*/, [UpdatedOn] = convert(date, statusDate) " + 
                                            ", Isdeleted = Case when cltID < 0 then 1 else 0 end " + 
                                            ", CompletedBySignatureSignatureDate = null " + 
                                            ", CounselorSignatureSignatureDate = null " +
                                            ", DoctorSignatureSignatureDate = null " +
                                            ", MedicalProviderSignatureSignatureDate = null " + 
                                            ", PatientSignatureDate = null " + 
                                            ", ProviderSignatureSignatureDate = case when (ISNull(convert(date, DrSigDt), convert(date, SigNurseDt)) is null and Status = 'Approved') then '1900-01-01' else ISNull(convert(date, DrSigDt), convert(date, SigNurseDt)) end " + 
                                            ", RequestorSignatureDate = null " +
                                            ", StaffSignatureDate = null " + 
                                            ", SupervisorSignatureSignatureDate = case when convert(date, sigCoordinatorDt) is null and Status = 'Approved' then '1900-01-01' else convert(date, sigCoordinatorDt) end " +
                                            " from [dbo].[tblORDERREQ] where status = 'Approved' and (Notes not like 'Test %' and Notes <> 'TEST' and DrNote <>'HEllo test' and DrNote <> 'TEST') ";
                                        //--where CreatedOn >= '" + wrkdt.ToShortDateString() + "' or isnull(ModifiedDate, CreatedOn) >= '" + wrkdt.ToShortDateString() + "' " +
                                    }
                                    
                                    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                                    rCodes = sd.SaveAnswerSignatures(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(-10), null);
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
                                && (x.TaskName == "Central ETL" || x.TaskName == "Estern ETL" || x.TaskName == "Mountain ETL" || x.TaskName == "Pacific ETL")).ToList();
                            break;
                        case "3":
                            pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now
                            && x.TaskName != "Central ETL" && x.TaskName != "Estern ETL" && x.TaskName != "Mountain ETL" && x.TaskName != "Pacific ETL"
                            && x.TaskName != "SAMMSGlobal").ToList();
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
