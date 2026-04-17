using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace bhg.TestCode
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            BHG_DR_LIB.SelectConstructor sc = new BHG_DR_LIB.SelectConstructor();
            BHG_DR_LIB.SQLSvrManager sm = new BHG_DR_LIB.SQLSvrManager();
            BHG_DR_LIB.SaveData sd = new BHG_DR_LIB.SaveData();
            BHG_DR_LIB.BulkDartsSvc bldr = new BHG_DR_LIB.BulkDartsSvc();
            BHG_DR_LIB.Models.BHG_DRContext db = new BHG_DR_LIB.Models.BHG_DRContext();

            DataTable SrcDt = new DataTable();
            string strFlds = "";
            string strCmd;
            int RowsIns = 0;
            int RowsUpd = 0;

    //        // Test ZeroDollarDenials
    //        strCmd = "truncate table pats.tbl_vw_ZeroDollarDenials; " + Environment.NewLine +
    //" insert into pats.tbl_vw_ZeroDollarDenials([SiteCode], [tpccltID], [ClientM4ID], [Date] \r\n" +
    //" , [Payor], [Description], [liaANSI2], [Amount], [TransactionType], [SvcDate] \r\n" +
    //" , [tpcliTPCID], [liaStrUser], [CPT], [tpcliID]) \r\n" +
    //" SELECT [SiteCode], [tpccltID], [ClientM4ID], [Date], [Payor], [Description], [liaANSI2], [Amount], [TransactionType], [SvcDate] \r\n" +
    //" , [tpcliTPCID], [liaStrUser], [CPT], [tpcliID] FROM [pats].[vw_ZeroDollarDenials] ";
    //        sm.ExeSqlCmd(strCmd, sm.ConnectionString);


            //DataTable tblr = sm.ExecStrPro("pats.BAMMerge", "@sitecode", "B12B", sm.ConnectionString);
            //foreach(DataRow r in tblr.Rows)
            //{
            //    if (r[0].ToString() == "UPDATE")
            //    {
            //        RowsUpd++;
            //    }
            //    else
            //    {
            //        RowsIns++;
            //    }
            //}

            //List<BHG_DR_LIB.Models.VwTaskListMap> Tasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17 && x.RunAt < DateTime.Now).ToList();
            //var st = Tasks.Where(x => x.ParentTaskId == pt.TaskId).OrderBy(o => o.TaskName).ThenBy(b => b.SiteCode).ThenBy(d => d.FromTblVw).ToList()

            BHG_DR_LIB.Models.VwTaskListMap st = new BHG_DR_LIB.Models.VwTaskListMap
            //{
            //    ActionKey = 1, ActionStepKey = 4,
            //    SiteCode = "B12B",
            //    WorkDate = DateTime.Parse("1/1/2020"),
            //    FromTblVw = "tblDartsSrv",
            //    WhereCondition = "convert(date,DsDtStart) = @WorkDate",
            //    SrcSchema = "dbo", IsNewSchema = false,
            //    TaskName = "pats.tbl_FormsSAMMSClient", SortOrder = "Order by 1,2", 
            //    ConStr = @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Connection Timeout=60;Integrated Security=True;Encrypt=False;Initial Catalog=SAMMS-ColoradoSpringsV5;"

            //};
            {
                //ActionKey = 4, ActionStepKey = 1,
                ActionKey = 2,
                ActionStepKey = 7,
                SiteCode = "Global",
                WorkDate = DateTime.Parse("1/1/2020"),
                //FromTblVw = "tbl3pClaim", 
                FromTblVw = "tblFORMSSAMMSCLIENT",
                WhereCondition = "Year(convert(date, tpcCreatedDate)) = Year(@WorkDate)",
                SrcSchema = "dbo",
                IsNewSchema = false,
                TaskName = "pats.tbl_FormsSAMMSClient",
                SortOrder = "Order by 1, 2",
                //TaskName = "pats.tbl_Claims", SortOrder = "Order by tpccltID",
                //ConStr = @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Connection Timeout=60;Integrated Security=True;Encrypt=False;Initial Catalog=SAMMS-ColoradoSpringsV5;"
                ConStr = @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Connection Timeout=60;Integrated Security=True;Encrypt=False;Initial Catalog=SAMMSGLOBAL;"
            };
            //{
            //    //
            //    ActionKey = 1, ActionStepKey = 24,
            //    SiteCode = "V9", SchemaVersion = "V6",
            //    WorkDate = DateTime.Parse("3/28/2023"), 
            //    FromTblVw = "SF_PatientPreAdmission",
            //    WhereCondition = "len(pp.CreatedOn) > 0 and pp.ClientAddress not like '%test data%' ", 
            //    SrcSchema = "dbo", IsNewSchema = false, 
            //    TaskName = "tbl_PreAdmission_V6", SortOrder = "Order by 1, 3, 2",
            //    ConStr = @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Connection Timeout=60;Integrated Security=True;Encrypt=False;Initial Catalog=SAMMS-NashvilleV5;"
            //};

            bool ChkSumEnabled = true;

            List<BHG_DR_LIB.Models.VwMapSrc2Dsn> tdwork = db.WorkToDo.Where(x => x.Enabled
                            && x.ActionKey == st.ActionKey
                            && x.ActionStepKey == st.ActionStepKey).ToList();
            if (st.SiteCode == "PHC") { tdwork = tdwork.Where(x => x.PHC_Enabled).ToList(); }
            if (st.ActionKey == 3) { ChkSumEnabled = false; } else { ChkSumEnabled = true; }
            strFlds = sc.GetSLT(tdwork, ChkSumEnabled, st.IsNewSchema.Value, st.FromTblVw, st.SiteCode)
                        .Replace("@SiteCode", "'" + st.SiteCode + "'")
                        .Replace("@Samms", "'SAMMS'");
            if (st.TaskName.ToLower() == "pats.tbl_formssammsclient")
            {
                strFlds = strFlds.Replace("'Global'", "isnull((select Prefix from dbo.tblSites where sID = fscsite), 'Global')");
            }
            strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw;
            string strWhere = st.WhereCondition.Replace("@SiteCode", "'" + st.SiteCode + "'")
                .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(-14).ToShortDateString() + "'")
                //.Replace("@EnrollCutoff", "'" + Cutoff + "'")
                .Replace("@Samms", "'SAMMS'");

            if ((st.ActionKey == 1) && (st.ActionStepKey == 4))
            {
                string sstrCmd = strCmd + " Where Year(dsdtstart) = 2019 order by 1, 2";
                SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
                _ = sd.SaveDartSrv2019(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2019"), null);
                sstrCmd = strCmd + " Where Year(dsdtstart) = 2020 order by 1, 2";
                SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
                _ = sd.SaveDartSrv2020(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2020"), null);
                sstrCmd = strCmd + " Where Year(dsdtstart) = 2021 order by 1, 2";
                SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
                _ = sd.SaveDartSrv2021(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2021"), null);
                sstrCmd = strCmd + " Where Year(dsdtstart) = 2022 order by 1, 2";
                SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
                _ = sd.SaveDartSrv2022(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2022"), null);
                sstrCmd = strCmd + " Where Year(dsdtstart) = 2023 order by 1, 2";
                SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
                _ = sd.SaveDartSrv2023(SrcDt, st.SiteCode, 0, DateTime.Parse("1/1/2023"), null);

            }
            if ((st.ActionKey == 1) && (st.ActionStepKey == 24))
            {
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
                        BHG_DR_LIB.Models.RCodes rCodes = sd.SavePreAdmissionV6(SrcDt, st.SiteCode, null);
                    }
                }
            }
            if ((st.ActionKey == 2) && (st.ActionStepKey == 7))
            {

                //string scmd = "select fscsid, f.fscdate, fsccltid, f.fscsite, count(1) cnt from tblFormsSAMMSClient f " +
                //                            //" inner join tblSites s on (f.fscsite = s.sID)" +
                //                            "where fscsite not in (25, 100) " +  //fscCLTID < 0 and 
                //                            "group by fscsid, f.fscDATE, fscCLTID, f.fscSite having f.fscDATE >= '1/1/2021' order by 1, 2, 3, 4";
                //_ = sd.SaveFormCounts(sm.GetTableData("frms", scmd, @"Data Source=BHGDALLSQL05\MSSQLSERVER2K16;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;"), null);

                strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 38, 99, 100, 106, 115, 118) ";
                strCmd += strWhere + " order by 1, 2";

                SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                _ = bldr.BulkDartsSrvLoader(SrcDt, st.TaskName, st.SiteCode, st.WorkDate.Value, null);
                //strCmd = "update pats.tbl_FormsSAMMSClient set SiteCode = l.SiteCode " + 
                //    " from pats.tbl_FormsSAMMSClient left join ctrl.tbl_Locations l on pats.tbl_FormsSAMMSClient.fscsite = l.sID " + 
                //    " where l.SiteCode is not null and pats.tbl_FormsSAMMSClient.SiteCode = 'Global'";
                //_ = sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                //DateTime stopDate = DateTime.Parse("12/31/2022");
                //for (DateTime rundt = st.WorkDate.Value.Date.AddDays(-14); rundt <= stopDate; rundt = rundt.AddDays(1))
                //{
                //    {
                //        strWhere = " Where fscDATE > '12/31/2019' and fscsite not in (25, 100) and " +
                //            "(convert(date,fscDATE) = @dt or convert(date,clientSigDate) = @dt or convert(date,supervisorSigDate) = @dt " +
                //            " or convert(date,physicianSigDate) = @dt or convert(date,nurseSigDate) = @dt or convert(date,doctextEditDate) = @dt " +
                //            " or convert(date,GuardianSigDate) = @dt or convert(date,AdminnurseSigDate) = @dt or convert(date,staffSigDate) = @dt)";
                //        string strCmd2 = "declare @dt date = '" + rundt.ToShortDateString() + "'; " + strCmd;
                //        strCmd2 += strWhere + " order by 1, 2";

                //        SrcDt = sm.GetTableData(st.FromTblVw, strCmd2, st.ConStr);
                //        BHG_DR_LIB.Models.RCodes rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg." + st.TaskName.Substring(5, st.TaskName.Length - 5), st.SiteCode, st.WorkDate.Value, null);

                //        Console.WriteLine(rundt.ToShortDateString());
                //    }
                //}
            }
            if ((st.ActionKey == 4) && (st.ActionStepKey ==1))
            {
                DateTime dtWorkDate = DateTime.Parse("1/1/2020");
                string sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2020 " + st.SortOrder;
                SrcDt = sm.GetTableData(st.FromTblVw, sstrCmd, st.ConStr);
                _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate, true, null);
                sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2021 " + st.SortOrder;
                _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate.AddYears(1), true, null);
                sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2022 " + st.SortOrder;
                _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate.AddYears(2), true, null);
                sstrCmd = strCmd + " Where Year(convert(date, tpcCreatedDate)) = 2023 " + st.SortOrder;
                _ = sd.Save3pElig(SrcDt, st.SiteCode, dtWorkDate.AddYears(3), true, null);
            }
        }
    }
}
