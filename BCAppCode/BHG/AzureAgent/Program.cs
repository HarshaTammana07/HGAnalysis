using System;

namespace AzureAgent
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            BHG_DR_LIB.SQLSvrManager sm = new BHG_DR_LIB.SQLSvrManager();

            DateTime RunTime = DateTime.Now;
            string strCmd = string.Empty;

            if ((RunTime.Hour == 2) && (RunTime.Minute >= 24) && (RunTime.Minute <= 26))
            {
                try
                {
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                            "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 2:24 AM', 'Starting ...Transaction table load.'); \r\n";
                            //+ "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                            //"  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:24 AM', 'pats.tbl_vw_SignatureReportSAMMSForms'); ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                    strCmd = "truncate table ayx.tbl_Transactions; " + Environment.NewLine +
                        "insert into ayx.tbl_Transactions([SiteCode], [tpccltID], [ClientM4ID], [Date], [Program] " + Environment.NewLine +
                        ", [Payor], [Description], [CPT], [Amount], [TransactionType], [SvcDate], [liaStrUser], [tpcliID], [tpcliTPCID], [ProgOrder]) " + Environment.NewLine +
                        "select [SiteCode], [tpccltID], [ClientM4ID], [Date], [Program], [Payor], [Description], [CPT], [Amount], [TransactionType], [SvcDate], [liaStrUser] " +
                         Environment.NewLine + ", [tpcliID], [tpcliTPCID], [ProgOrder] from ayx.vw_Transactions order by SiteCode, tpccltID, [Date], SvcDate, CPT; ";

                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
                catch(Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'ayx.tbl_Transactions', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
            }

            RunTime = DateTime.Now;
            if ((RunTime.Hour == 6) && (RunTime.Minute >= 45) && (RunTime.Minute <= 50))
            {
                strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:45 AM', 'Starting tbl_vw_CounselorSupervision_KPISite...'); \r\n";
                sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                strCmd = "insert into [pba].[tbl_vw_CounselorSupervision_KPISite] ([MaxDay], [SiteCode], [Patcnt], [PatsCSLate], " +
                    "[PatsTPLate], [SumSchedCoun45], [PatsCSDONE], [Pats% MissedDose_w_call], [Pats% TP_DONE], [Pats% SchedCoun], " +
                    "[SiteNum_PatNotes], [Site_NoteRate], [SiteNum_Patwforms], [Site_FormRate]) " +
                    "select top 1000[MaxDay], [SiteCode], [Patcnt], [PatsCSLate], [PatsTPLate], [SumSchedCoun45], [PatsCSDONE], " +
                    "[Pats% MissedDose_w_call], [Pats% TP_DONE], [Pats% SchedCoun], [SiteNum_PatNotes], [Site_NoteRate], [SiteNum_Patwforms], " +
                    "[Site_FormRate] FROM [pba].[vw_CounselorSupervision_KPISite]; ";
                sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:45 AM', 'Starting tbl_vw_CounselorSupervision_KPICounselor...'); \r\n";
                sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                strCmd = "insert into pba.tbl_vw_CounselorSupervision_KPICounselor ([MaxDay], [Counselor], [CounselorEmail], [Patcnt], " +
                    "[PatsCSLate], [PatsTPLate], [PatsCSDONE], [Pats% MissedDose_w_call], [Pats% TP_DONE], [Pats% SchedCoun], [Num_PatNotes], " +
                    "[Counselor_NoteRate], [Num_Patwforms], [Counselor_FormRate]) " +
                    "select top 1000 [MaxDay], [Counselor] = isnull(Counselor, 'No Counselor'), [CounselorEmail], [Patcnt], [PatsCSLate], " +
                    "[PatsTPLate], [PatsCSDONE], [Pats% MissedDose_w_call], [Pats% TP_DONE], [Pats% SchedCoun], [Num_PatNotes], " +
                    "[Counselor_NoteRate], [Num_Patwforms], [Counselor_FormRate] from pba.vw_CounselorSupervision_KPICounselor; ";
                sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:45 AM', 'Finished tbl_vw_CounselorSupervision_KPICounselor...'); \r\n";
            }

            RunTime = DateTime.Now;
            if ((RunTime.Hour == 6) && (RunTime.Minute >= 24) && (RunTime.Minute <= 26))
            {
                strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:24 AM', 'Starting ...'); \r\n";
                sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                /// ZeroDollarDenials
                /// 
                try
                {
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                             "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:24 AM', 'Starting ZeroDollarDenials') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);


                    strCmd = "truncate table pats.tbl_vw_ZeroDollarDenials; " + Environment.NewLine +
                        " insert into pats.tbl_vw_ZeroDollarDenials([SiteCode], [tpccltID], [ClientM4ID], [Date] \r\n" +
                        " , [Payor], [Description], [liaANSI2], [Amount], [TransactionType], [SvcDate] \r\n" +
                        " , [tpcliTPCID], [liaStrUser], [CPT], [tpcliID], LastModAt) \r\n" +
                        " SELECT [SiteCode], [tpccltID], [ClientM4ID], [Date], [Payor], [Description], [liaANSI2], [Amount], [TransactionType], [SvcDate] \r\n" +
                        " , [tpcliTPCID], [liaStrUser], [CPT], [tpcliID], GetDate() FROM [pats].[vw_ZeroDollarDenials] ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
                catch (Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'ZeroDollarDenials', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
                // Signature Report
                try
                {

                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:24 AM', 'pats.tbl_vw_SignatureReportSAMMSForms'); ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                    strCmd = "truncate table pats.tbl_vw_SignatureReportSAMMSForms; " + Environment.NewLine +
                    "insert into pats.tbl_vw_SignatureReportSAMMSForms([SiteCode] " + Environment.NewLine +
                    ", [FormLineID], [FormName], [ClientID], [PatientID], [FormDate], [First], [Last], [Counselor], [ClientSignDate], [CounselorSignDate] " + Environment.NewLine +
                    ", [DrSignDate], [NurseSignDate], [SuprvSignDate], [Program], [signaturesneeded]) " + Environment.NewLine +
                    " SELECT [SiteCode], [FormLineID], [FormName], [ClientID], [PatientID], [FormDate] " + Environment.NewLine +
                    ", [First], [Last], [Counselor], [ClientSignDate], [CounselorSignDate], [DrSignDate], [NurseSignDate], [SuprvSignDate], [Program], [signaturesneeded] " + Environment.NewLine +
                    " FROM [pats].[vw_SignatureReportSAMMSForms] ORDER BY SiteCode, FormName, ClientID, FormDate";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'Agent Excution 6:24 AM', 'pats.Populate_BAM_Bucketed') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                    sm.ExeSqlCmd("exec [pats].[Populate_BAM_Bucketed]", sm.ConnectionString);

                }
                catch (Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_SignatureReportSAMMSForms', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
                //Execute Stored Procedure MergeServicesMissingSigCode
                try
                {
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'MergeServicesMissingSigCode', 'Starting') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                    strCmd = "truncate table pats.tbl_ServicesMissingSigCode; " + Environment.NewLine +
                        " Insert into pats.tbl_ServicesMissingSigCode ([patientid], [clientId], [firstname], [LastName], [Status], [SiteCode] " + Environment.NewLine +
                        " , [SiteID], [prog], [counselor], [dsTxtSrv], [dstxtStaff], [dsTxtType], [dsDtAdded], [dsDtStart], [dsDtEnd] " + Environment.NewLine +
                        " , [dsdblUnits], [dsUpdate], [dsUPDATEStaff], [dstxtNote], [dsdbnotes], [dsGROUPNUM], [dsArea] " + Environment.NewLine +
                        " , [dsID], [dsSigDate], [dsbilled], [StaffName], [StaffActive]) " + Environment.NewLine +
                        " SELECT [patientid], [clientId], [firstname], [LastName], [Status], [SiteCode], [SiteID], [prog] " + Environment.NewLine +
                        "      , [counselor], [dsTxtSrv], [dstxtStaff], [dsTxtType], [dsDtAdded], [dsDtStart], [dsDtEnd] " + Environment.NewLine +
                        "      , [dsdblUnits], [dsUpdate], [dsUPDATEStaff], [dstxtNote], [dsdbnotes], [dsGROUPNUM], [dsArea] " + Environment.NewLine +
                        "      , [dsID], [dsSigDate], [dsbilled], [StaffName], [StaffActive] FROM [pats].[ServicesMissingSigCode] ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);

                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'MergeServicesMissingSigCode', 'Completed') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                    
                    //sm.ExeSqlCmd("exec pats.MergeServicesMissingSigCode ", sm.ConnectionString);
                }
                catch (Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'MergeServicesMissingSigCode', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
            }

            RunTime = DateTime.Now;
            if ((RunTime.Hour == 6) && (RunTime.Minute >= 45) && (RunTime.Minute <= 48))
            {
                try
                {
                    sm.ExeSqlCmd("insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                            "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_Treatment_Plan', 'Starting... ') ", sm.ConnectionString);

                    strCmd = "truncate table pats.tbl_vw_Treatment_Plan; " + Environment.NewLine +
                        "insert into pats.tbl_vw_Treatment_Plan ([Region], [sitecode], [statecode], [ClinicName], [PatientID], [clientid], [first], [last]" + Environment.NewLine +
                        ", [formid], [formname], [tcode], [true14enrolldate], [Duration], [Counselor], [proggroup], [Program], [Payor] " + Environment.NewLine +
                        ", [finClass], [formdate], [DayspastTP], [ClientSignDate], [CounselorSignDate], [DrSignDate], [SuprvSignDate], [NextTPdue] " + Environment.NewLine +
                        ", [TPInterval], [TP_Late], [TP_DueSoon]) " + Environment.NewLine +
                        "select [Region], [sitecode], [statecode], [ClinicName], [PatientID], [clientid], [first], [last]" + Environment.NewLine +
                        ", [formid], [formname], [tcode], [true14enrolldate], [Duration], [Counselor], [proggroup], [Program], [Payor]" + Environment.NewLine +
                        ", [finClass], [formdate], [DayspastTP], [ClientSignDate], [CounselorSignDate], [DrSignDate], [SuprvSignDate], [NextTPdue]" + Environment.NewLine +
                        ", [TPInterval], [TP_Late], [TP_DueSoon] " + Environment.NewLine +
                        " from pats.vw_Treatment_Plan order by 1, 2, 3, clientid, Program, formdate ";

                    BHG_DR_LIB.Models.RCodes rcode = sm.ExeSqlCmd(strCmd, sm.ConnectionString, false);
                    if (rcode.IsResult == false)
                    {
                        string err = rcode.ExceptMsg;
                        if (rcode.ExceptInnerMsg.Length > 0) { err += "     " + rcode.ExceptInnerMsg; }
                        if (err.Length > 500) { err = err.Substring(0, 499); }
                        strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                            "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_Treatment_Plan', '" + err + "') ";
                        sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                    }
                    sm.ExeSqlCmd("insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                          "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_Treatment_Plan', 'Completed... ') ", sm.ConnectionString);
                }
                catch (Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_Treatment_Plan', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
            }

            // trending counseling statereq
            RunTime = DateTime.Now;
            if ((RunTime.Hour == 7) && ((RunTime.Minute >= 0) && (RunTime.Minute <=10)))
            {

                try {
                    sm.ExeSqlCmd("insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                            "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_TrendingCounseling_StateReq', 'Starting... ') ", sm.ConnectionString);

                    strCmd = "declare @dt date = getdate()-1; insert into [pats].[tbl_Vw_TrendingCounseling_StateReq] " +
                        " (ActiveDate, [Location], ProgGroup, cltID, PatientID, FinClass, PayorType, PatientCount, CodeLevel, StateCode" +
                        ", LOS, [Sessions], Unit, Frequency, IndividualMin, GroupMin, BHGStrict, Individual, Groups, LastTPDate, TP_Late" +
                        ", GroupUnits, IndividualUnits, UpToDate, UpToDateTrue) Exec pats.SP_CounselingStateReq @dt";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString, false);
                    sm.ExeSqlCmd("insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                          "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_TrendingCounseling_StateReq', 'Completed... ') ", sm.ConnectionString);
                }
                catch (Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_TrendingCounseling_StateReq', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
                try
                {
                    sm.ExeSqlCmd("insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                            "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_MedInvMerge', 'Starting... ') ", sm.ConnectionString);

                    strCmd = "Exec pats.SP_MedInvMerge";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString, false);
                    sm.ExeSqlCmd("insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                          "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_MedInvMerge', 'Completed... ') ", sm.ConnectionString);
                }
                catch (Exception e)
                {
                    string err = e.Message.ToString();
                    if (e.InnerException != null) { err += "     " + e.InnerException.Message.ToString(); }
                    if (err.Length > 500) { err = err.Substring(0, 499); }
                    strCmd = "insert into tsk.tbl_ErrorLog (ProcessName, ProcessDate, ProcedureName, ErrorMessage) \r\n" +
                        "  Values('AzureAgent', '" + DateTime.Now + "', 'pats.tbl_vw_MedInvMerge', '" + err + "') ";
                    sm.ExeSqlCmd(strCmd, sm.ConnectionString);
                }
            }
        }
    }
}
