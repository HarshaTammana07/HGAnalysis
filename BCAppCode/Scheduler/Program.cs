using System;

namespace Scheduler
{
    class Program
    {
        static void Main(string[] args)
        {
            DateTime wrkdt = DateTime.Today;
            BHG_DR_LIB.SQLSvrManager db = new BHG_DR_LIB.SQLSvrManager();
            //select* from tsk.tbl_Tasks2 where convert(date, RunAt) >= convert(date, GetDate() - 1) and Status in (17, 20) and RowState = 24
            //select* from tsk.vw_Schedule where Enabled = 1

            db.ExeSqlCmd("update tsk.tbl_Tasks2 set Status = 17 where Status = 18 ", db.ConnectionString);
            db.ExeSqlCmd("update tsk.vwTaskList set RowState = 26 where Status = 17 and WorkDate < '" + wrkdt.ToShortDateString() + "' and WhereCondition = '1 = 1'", db.ConnectionString);

            string strCmd = "insert into tsk.tbl_Tasks2 (TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError, Reload) "
                + "select Name, NextRunTime, ActionKey, 0, 17, 24, GetDate(), 'Brian.Catellier', Case when scheduleid = 18 then 'PHC' else 'All' end, Convert(date, NextRunTime), '0', 0, 0, 0 "
                + "  from tsk.tbl_Schedule where Enabled = 1; "
                + "insert into tsk.tbl_Tasks2(ParentTaskId, TaskName, RunAt, ActionKey, ActionStepKey, Status, RowState, LastModAt, LastModBy, SiteCode, WorkDate, Duration, onCompletion, onError, Reload) "
                + "select t.TaskId, ma.DsnSchema + '.' + ma.DsnTbl, t.RunAt,  ma.ActionKey, ma.StepKey, 17, 24, GetDate(), 'Brian.Catellier', ma.SiteCode, t.WorkDate, '0', 0, 0, 0 "
                + "  from dms.vw_MapAction ma cross join tsk.tbl_Tasks2 t "
                + " where ma.Enabled = 1 and ma.IsActive = 1 and ConnectionID <> 3 "
                + " and t.Status = 17 and t.WorkDate = convert(date, '" +  wrkdt.ToShortDateString() + "') and "
                + "case when ma.SiteCode = 'PHC' then 'PHC ETL' " + " when ma.SiteCode = 'LAB'  and ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_ClientDemo1', 'pats.tbl_ClientDemo2') then 'Samms-LAB' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_dbo_FormAnswerSignatures', 'pats.tbl_dbo_FormQuestionAnswers') Then 'Samms-Forms' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_3parnote', 'pats.tbl_3pclaimnote') then 'SAMMS-ETL-Notes' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_Bottle', 'pats.tbl_LiquidLog', 'ctrl.Tbl_InvType', 'pats.tbl_orientationchecklistnew'" 
                + ", 'pats.tbl_labresult', 'pats.tbl_labresultdetail', 'pats.Tbl_Appointments', 'pats.Tbl_AdmissionAssessment', 'pats.Tbl_AdmissionAssessmentSummary', 'pats.Tbl_ReAssessment'"
                + ", 'pats.Tbl_AdmissionAssessmentDimensionOneDisorder', 'pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse', 'pats.Tbl_AdmissionAssessmentDimensionTwo'" 
                + ", 'pats.Tbl_ReAssessmentOccupational', 'pats.Tbl_ReAssessmentFamily', 'pats.Tbl_ReAssessmentLegal', 'pats.Tbl_ReAssessmentMentalHealth'" 
                + ", 'pats.Tbl_ReAssessmentPhysicalHealth', 'pats.Tbl_ReAssessmentSubstanceUse', 'pats.Tbl_ReAssessmentSocial', 'pats.Tbl_ReAssessmentTreatment'" 
                + ", 'pats.Tbl_AdmissionAssessmentDimensionThree', 'pats.tbl_AdmissionAssessmentDimensionSix', 'pats.tbl_AdmissionAssessmentDimensionfour') then 'SAMMS-ETL-Inv' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_DartsSrv' then 'SAMMS-ETL-DartSvc' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Dose' then 'SAMMS-ETL-Dose' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Dose_Excuse' then 'SAMMS-ETL-Dose' "
                + "     when ma.DsnSchema + '.' + ma.DsnTbl = 'pats.tbl_Orders' then 'SAMMS-ETL-Orders' "
                + "     when ma.TimeZone = 'EST' and ma.DsnSchema + '.' + ma.DsnTbl not in ('pats.tbl_DartsSrv', 'pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', 'pats.tbl_dose', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_feesched', 'pats.tbl_CheckIn'"
                + ", 'pats.tbl_EandMFormPregnancy', 'pats.tbl_EandMFormMDM', 'pats.tbl_Bills', 'pats.tbl_payerclthistory', 'pats.tbl_Orders') then 'Eastern ETL P1' "
                + "     when ma.TimeZone = 'EST' and ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', "
                + " 'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_feesched', 'pats.tbl_CheckIn'"
                + ", 'pats.tbl_EandMFormPregnancy', 'pats.tbl_EandMFormMDM', 'pats.tbl_Bills', 'pats.tbl_payerclthistory', 'pats.tbl_treatmentlevel', 'pats.tbl_Orders') then 'Eastern ETL P2' "
                + "     when ma.TimeZone = 'CST' and ma.DsnSchema + '.' + ma.DsnTbl not in ('pats.tbl_DartsSrv', 'pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', 'pats.tbl_dose', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_Enrollment', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_feesched', 'pats.tbl_CheckIn'" 
                + ", 'pats.tbl_EandMFormPregnancy', 'pats.tbl_EandMFormMDM', 'pats.tbl_Bills', 'pats.tbl_Orders') then 'Central ETL P1' "
                + "     when ma.TimeZone = 'CST' and ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_Enrollment', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_feesched', 'pats.tbl_CheckIn'"
                + ", 'pats.tbl_EandMFormPregnancy', 'pats.tbl_EandMFormMDM', 'pats.tbl_Bills', 'pats.tbl_treatmentlevel', 'pats.tbl_Orders') then 'Central ETL P2' "
                + "     when ma.TimeZone = 'MST' and ma.DsnSchema + '.' + ma.DsnTbl not in ('pats.tbl_DartsSrv', 'pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', 'pats.tbl_dose', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_Enrollment', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_FeeSched', 'pats.tbl_EandMFormMDM', 'pats.tbl_payerclthistory') then 'Mountain ETL P1' "
                + "     when ma.TimeZone = 'MST' and ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', 'pats.tbl_treatmentlevel', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_Enrollment', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_FeeSched', 'pats.tbl_EandMFormMDM', 'pats.tbl_payerclthistory') then 'Mountain ETL P2' "
                + "     when ma.TimeZone = 'PST' and ma.DsnSchema + '.' + ma.DsnTbl not in ('pats.tbl_DartsSrv', 'pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', 'pats.tbl_dose', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_Enrollment', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_FeeSched', 'pats.tbl_EandMFormMDM', 'pats.tbl_payerclthistory') then 'Pacific ETL P1' "
                + "     when ma.TimeZone = 'PST' and ma.DsnSchema + '.' + ma.DsnTbl in ('pats.tbl_claims', 'pats.tbl_claimlineitem', 'pats.tbl_claimlineitemactivity', 'pats.tbl_treatmentlevel', "
                + "'pats.tbl_dose_excuse', 'pats.tbl_uaresultdetail', 'pats.tbl_Enrollment', 'pats.tbl_GlobalPayor', 'pats.tbl_PayerClient', 'pats.tbl_FeeSched', 'pats.tbl_EandMFormMDM', 'pats.tbl_payerclthistory') then 'Pacific ETL P2' "
                + "     else 'SAMMSGlobal' end = t.TaskName "
                + "order by ma.ActionKey, ma.DsnTbl, ma.SiteCode; "
                + "update tsk.tbl_Schedule set NextRunTime = DATEADD(d, 1, NextRunTime), LastRunTime = dateadd(d, 1, LastRunTime) where Enabled = 1; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName in " 
                + "('pats.tbl_BriefAddictionMonitor', 'pats.tbl_clinicalopiatewithdrawalscale', 'pats.tbl_vw3pBillSub')"
                + " and SiteCode = 'PHC' and Status = 17 and RowState = 24; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_PayerClient' and Status = 17 and RowState = 24 and SiteCode = 'LAB' and ActionKey = 1 and ActionStepKey = 6; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_Cows_V6' and Status = 17 and RowState = 24 and SiteCode = 'PHC' and ActionKey = 1 and ActionStepKey = 23; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'ayx.tbl_PreAdmission_V6' and (SiteCode = 'PHC' or SiteCode = 'LAB') and RowState = 24; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_EandMFormMDM' and (SiteCode = 'PHC' or SiteCode = 'LAB') and RowState = 24; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_EandMFormPregnancy' and (SiteCode = 'PHC' or SiteCode = 'LAB') and RowState = 24; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where TaskName = 'pats.tbl_Appointments' and SiteCode = 'LAB' and RowState = 24; " 
                + "update tsk.vwTaskList set RowState = 26 where TaskName = 'ayx.tbl_PreAdmission_V6' and tsk.vwTaskList.SchemaVersion = 'V5' and RowState = 24; "
                + "update tsk.vwTaskList set RowState = 26 where TaskName = 'pats.Tbl_OrientationChecklistNew' and SiteCode = 'LAB' and RowState = 24; "
                + "update tsk.tbl_Tasks2 set RowState = 26 where SiteCode = 'LAB' and TaskName in ('pats.Tbl_AdmissionAssessment', 'pats.Tbl_ReAssessment'" 
                + ", 'pats.Tbl_AdmissionAssessmentDimensionOneDisorder', 'pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse', 'pats.Tbl_AdmissionAssessmentDimensionTwo'" 
                + ", 'pats.Tbl_ReAssessmentOccupational', 'pats.Tbl_ReAssessmentFamily', 'pats.Tbl_ReAssessmentLegal', 'pats.Tbl_ReAssessmentMentalHealth'" 
                + ", 'pats.Tbl_ReAssessmentPhysicalHealth', 'pats.Tbl_ReAssessmentSubstanceUse', 'pats.Tbl_ReAssessmentSocial', 'pats.Tbl_ReAssessmentTreatment', 'pats.tbl_PADimension1', 'pats.tbl_PADimension2' "
                + ", 'pats.tbl_PADimension3', 'pats.tbl_PADimension4', 'pats.tbl_PADimension5', 'pats.tbl_PADimension6', 'pats.tbl_AppointmentAttend', 'pats.tbl_PA' "
                + ", 'pats.Tbl_AdmissionAssessmentSummary', 'pats.Tbl_AdmissionAssessmentDimensionFour', 'pats.tbl_treatmentlevel', 'pats.tbl_FinancialHardshipApplication', 'pats.tbl_PACounselorReview' "
                + ", 'pats.Tbl_AdmissionAssessmentDimensionThree', 'pats.tbl_AdmissionAssessmentDimensionSix', 'pats.tbl_AdmissionAssessmentSubstanceuseHistory', 'pats.tbl_AssessmentSubstanceuseHistory', 'pats.tbl_ComprehensiveAssessmentForm' "
                + ", 'pats.tbl_BAMForm', 'pats.tbl_BAMScore', 'pats.tbl_TblDiag10' ); "
                + "delete from tsk.tbl_Tasks2 where RunAt <= DateAdd(m, -3, convert(date, GetDate())) or RowState = 26; ";

            db.ExeSqlCmd(strCmd, db.ConnectionString);

            //Console.WriteLine("Hello World!");
        }
    }
}
