using BHG_DR_LIB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace BHG_DR_LIB
{
    public class SelectConstructor
    {
        public string GetSLT(List<VwMapSrc2Dsn> mywork, bool ChkSumEnabled, bool NewSchema, string tblName, string sc)
        {
            string myRes = "";
            string myChksum = "";
            foreach (Models.VwMapSrc2Dsn r in mywork)
            {
                bool skipfield = false;
                if (!NewSchema)
                {
                    //Console.WriteLine("NewSchema = " + NewSchema.ToString());
                    switch (tblName.ToLower())
                    {
                        case "tblcodes":
                            if ((r.FieldName.ToLower() == "reqauth") ||
                                (r.FieldName.ToLower() == "obat") ||
                                (r.FieldName.ToLower() == "isprescreening") ||
                                (r.FieldName.ToLower() == "cde3pposoverride"))
                            {
                                skipfield = true;
                                //Console.WriteLine("Skipfield " + r.FieldName.ToString());
                            }
                            if ((sc == "V14") && (r.FieldName.ToLower() == "intakeprog"))
                            { skipfield = true; }
                            break;
                        case "tbluaresult":
                            if ((r.FieldName.ToLower() == "location_") ||
                                (r.FieldName.ToLower() == "location") ||
                                (r.FieldName.ToLower() == "scheduleddate") ||
                                (r.FieldName.ToLower() == "uabase64") ||
                                (r.FieldName.ToLower() == "uaprogram"))
                            {
                                skipfield = true;
                            }
                            break;
                        case "tbluaresultdetail":
                            if ((r.FieldName.ToLower() == "uardfullnote") ||
                                (r.FieldName.ToLower() == "uardkey") ||
                                (r.FieldName.ToLower() == "uardnote"))
                            { skipfield = true; }
                            break;
                        case "tblclinic":
                            if ((r.FieldName.ToLower() == "blasterwide") ||
                                (r.FieldName.ToLower() == "pumpcalibrate") ||
                                (r.FieldName.ToLower() == "checkvisitingpatient") ||
                                (r.FieldName.ToLower() == "requireclientsignatureorderrequest") ||
                                (r.FieldName.ToLower() == "dischargeallowpayer") ||
                                (r.FieldName.ToLower() == "dymodetailed") ||
                                (r.FieldName.ToLower() == "blasterwide")
                                )
                            { skipfield = true; }
                            if ((sc == "B28") || (sc == "B42A") || (sc == "B54") || (sc == "B55") || (sc == "TTCB"))
                            {
                                if (r.FieldName.ToLower() == "dischargedallowaddpayer") { skipfield = true; }
                            }
                            if (sc.ToUpper() == "LAB")
                            {
                                if ((r.FieldName.ToLower() == "enablecommentsonmulticheckin") ||
                                (r.FieldName.ToLower() == "PullPicsFromD") ||
                                (r.FieldName.ToLower() == "enablecompetentcheckboxatdosing") ||
                                (r.FieldName.ToLower() == "enableactivateorderwhennotinsuboxoneprog") ||
                                (r.FieldName.ToLower() == "enableprinttoxlandscape") ||
                                (r.FieldName.ToLower() == "enableflagnurseforbac")
                                )
                                { skipfield = true; }
                            }
                            break;
                        //case "tblorder":
                        //    if ((r.FieldName.ToLower() == "sigdr")
                        //        || (r.FieldName.ToLower() == "sigentered")
                        //        || (r.FieldName.ToLower() == "signoted")
                        //        || (r.FieldName.ToLower() == "sigmid")
                        //        || (r.FieldName.ToLower() == "sigdrimg")
                        //        || (r.FieldName.ToLower() == "signotedimg")
                        //       )
                        //    { skipfield = false; }
                        //    break;
                        default:
                            break;

                    }
                }
                if (r.FieldName == "RowChkSum") { skipfield = true; }
                if (!skipfield)
                {
                    if ((r.FieldName.Contains('@')) || (r.FieldName.Contains('.')) || (r.FieldName.Contains("case ")))
                    {
                        myRes += r.FieldName + " " + r.DsnFieldName + ", ";
                    }
                    else
                    {
                        //    if (r.FormatConvert.Length > 0)
                        //    {
                        //        myChksum += "[" + r.FieldName + "], ";

                        //    }
                        //    else
                        //    {
                        if (r.FieldType.ToLower() != "ntext")
                        {
                            if (r.FieldType.ToLower() != "varbinary")
                            {
                                if (r.FieldType.ToLower() != "timestamp")
                                {
                                    myChksum += "[" + r.FieldName + "], ";
                                }
                            }
                        }
                        myRes += "[" + r.FieldName + "]";
                        if (r.DsnFieldName != null)
                        {
                            myRes += " " + r.DsnFieldName + ", ";
                        }
                        else { myRes += ", "; }
                        //    }
                    }
                }
            }
            if (myChksum.Length > 2)
            {
                myChksum = myChksum.Substring(0, myChksum.Length - 2);
            }
            if (ChkSumEnabled)
            { myRes += "CHECKSUM(" + myChksum + ") RowChkSum "; }
            else
            {
                if (myRes.Length > 2)
                {
                    myRes = myRes.Substring(0, myRes.Length - 2);
                }
            }
            return myRes;
        }

        public string GetSDMT(List<VwMapSrc2Dsn> mywork, bool ChkSumEnabled, bool NewSchema, string tblName, string sc)
        {
            string myRes = "";
            string myChksum = "";
            foreach (Models.VwMapSrc2Dsn r in mywork)
            {
                bool skipfield = false;
                if (!NewSchema)
                {
                    //Console.WriteLine("NewSchema = " + NewSchema.ToString());
                    switch (tblName.ToLower())
                    {
                        case "tblcodes":
                            if ((r.FieldName.ToLower() == "reqauth") ||
                                (r.FieldName.ToLower() == "obat") ||
                                (r.FieldName.ToLower() == "isprescreening") ||
                                (r.FieldName.ToLower() == "cde3pposoverride"))
                            {
                                skipfield = true;
                                //Console.WriteLine("Skipfield " + r.FieldName.ToString());
                            }
                            if ((sc == "V14") && (r.FieldName.ToLower() == "intakeprog"))
                            { skipfield = true; }
                            break;
                        case "tbluaresult":
                            if ((r.FieldName.ToLower() == "location_") ||
                                (r.FieldName.ToLower() == "location") ||
                                (r.FieldName.ToLower() == "scheduleddate") ||
                                (r.FieldName.ToLower() == "uabase64") ||
                                (r.FieldName.ToLower() == "uaprogram"))
                            {
                                skipfield = true;
                            }
                            break;
                        case "tbluaresultdetail":
                            if ((r.FieldName.ToLower() == "uardfullnote") ||
                                (r.FieldName.ToLower() == "uardkey") ||
                                (r.FieldName.ToLower() == "uardnote"))
                            { skipfield = true; }
                            break;
                        case "tblclinic":
                            if ((r.FieldName.ToLower() == "blasterwide") ||
                                (r.FieldName.ToLower() == "pumpcalibrate") ||
                                (r.FieldName.ToLower() == "checkvisitingpatient") ||
                                (r.FieldName.ToLower() == "requireclientsignatureorderrequest") ||
                                (r.FieldName.ToLower() == "dischargeallowpayer") ||
                                (r.FieldName.ToLower() == "dymodetailed") ||
                                (r.FieldName.ToLower() == "blasterwide")
                                )
                            { skipfield = true; }
                            if ((sc == "B28") || (sc == "B42A") || (sc == "B54") || (sc == "B55") || (sc == "TTCB"))
                            {
                                if (r.FieldName.ToLower() == "dischargedallowaddpayer") { skipfield = true; }
                            }
                            if ((sc.ToUpper() == "LAB") && ((r.FieldName.ToLower() == "enablecommentsonmulticheckin") ||
                                (r.FieldName.ToLower() == "PullPicsFromD") ||
                                (r.FieldName.ToLower() == "enablecompetentcheckboxatdosing") ||
                                (r.FieldName.ToLower() == "enableactivateorderwhennotinsuboxoneprog") ||
                                (r.FieldName.ToLower() == "enableprinttoxlandscape") ||
                                (r.FieldName.ToLower() == "enableflagnurseforbac")
                                ))
                            { skipfield = true; }
                            break;
                        //case "tblorder":
                        //    if ((r.FieldName.ToLower() == "sigdr")
                        //        || (r.FieldName.ToLower() == "sigentered")
                        //        || (r.FieldName.ToLower() == "signoted")
                        //        || (r.FieldName.ToLower() == "sigmid")
                        //        || (r.FieldName.ToLower() == "sigdrimg")
                        //        || (r.FieldName.ToLower() == "signotedimg")
                        //       )
                        //    { skipfield = false; }
                        //    break;
                        default:
                            break;

                    }
                }
                if (r.FieldName == "RowChkSum") { skipfield = true; }
                if (!skipfield)
                {
                    if ((r.FieldName.Contains('@')) && (r.PrimaryKey.HasValue))
                    {
                        myRes += r.FieldName + " " + r.DsnFieldName + ", ";
                    }
                    else
                    {
                        //    if (r.FormatConvert.Length > 0)
                        //    {
                        //        myChksum += "[" + r.FieldName + "], ";

                        //    }
                        //    else
                        //    {
                        if (r.FieldType.ToLower() != "ntext")
                        {
                            if (r.FieldType.ToLower() != "varbinary")
                            {
                                if (r.FieldType.ToLower() != "timestamp")
                                {
                                    myChksum += "[" + r.FieldName + "], ";
                                }
                            }
                        }
                        if (r.PrimaryKey.HasValue)
                        {
                            myRes += "[" + r.FieldName + "]";
                            if (r.DsnFieldName != null)
                            {
                                myRes += " " + r.DsnFieldName + ", ";
                            }
                            else { myRes += ", "; }
                        }
                    }
                }
            }
            myChksum = myChksum.Substring(0, myChksum.Length - 2);
            if (ChkSumEnabled)
            { myRes += "CHECKSUM(" + myChksum + ") RowChkSum "; }
            else { myRes = myRes.Substring(0, myRes.Length - 2); }
            return myRes;
        }

        public List<YearlyAuditData> GetWorkingSet(string year, string sites, string tblname)
        {
            bool site = false;
            bool tbl = false;
            List<Models.YearlyAuditData> data;
            BHG_DRContext db = new BHG_DRContext();
            if (tblname.ToLower() != "all") { tbl = true; }
            if (sites.ToLower() != "all") { site = true; }
            if (site && tbl)
            {
                if (tblname == "tbl_dartssrv")
                {
                    switch (sites.ToLower())
                    {
                        case "b1":
                            data = db.YearlyAuditData.Where(x => x.Enabled
                                && x.SiteCode.StartsWith("B")
                                && x.DsnTbl == tblname 
                                && x.WrkYear == year).ToList();
                            break;
                        case "b2":
                            data = db.YearlyAuditData.Where(x => x.Enabled
                                && !x.SiteCode.StartsWith("B")
                                && !x.SiteCode.StartsWith("V")
                                && x.DsnTbl == tblname 
                                && x.WrkYear == year).ToList();
                            break;
                        case "b3":
                            data = db.YearlyAuditData.Where(x => x.Enabled
                                && x.SiteCode.StartsWith("V")
                                && x.DsnTbl == tblname 
                                && x.WrkYear == year).ToList();
                            break;
                        default:
                            data = db.YearlyAuditData.Where(x => x.Enabled
                                && x.SiteCode == sites
                                && x.DsnTbl == tblname
                                && x.WrkYear == year).ToList();
                            break;
                    }
                }
                else
                {
                    data = db.YearlyAuditData.Where(x => x.Enabled
                        && x.SiteCode == sites
                        && x.DsnTbl == tblname
                        && x.WrkYear == year).ToList();
                }
            }
            else
            {
                if (site && !tbl)
                {
                    data = db.YearlyAuditData.Where(x => x.Enabled
                    && x.SiteCode == sites
                    && x.WrkYear == year).ToList();
                }
                else
                {
                    if (tbl)
                    {
                        data = db.YearlyAuditData.Where(x => x.Enabled
                            && x.WrkYear == year && x.DsnTbl == tblname).ToList();
                    }
                    else
                    {
                        if (tblname == "tbl_dartssrv")
                        {
                            switch (sites.ToLower())
                            {
                                case "b1":
                                    data = db.YearlyAuditData.Where(x => x.Enabled
                                        && x.SiteCode.StartsWith("B")
                                        && x.WrkYear == year).ToList();
                                    break;
                                case "b2":
                                    data = db.YearlyAuditData.Where(x => x.Enabled
                                        && !x.SiteCode.StartsWith("B")
                                        && !x.SiteCode.StartsWith("V")
                                        && x.WrkYear == year).ToList();
                                    break;
                                case "b3":
                                    data = db.YearlyAuditData.Where(x => x.Enabled
                                        && x.SiteCode.StartsWith("V")
                                        && x.WrkYear == year).ToList();
                                    break;
                                default:
                                    data = db.YearlyAuditData.Where(x => x.Enabled
                                        && x.WrkYear == year).ToList();
                                    break;
                            }
                        }
                        else
                        {
                            data = db.YearlyAuditData.Where(x => x.Enabled
                            && x.WrkYear == year).ToList();
                        }
                    }
                }
            }
            return data;
        }

        public RCodes SyncRDB2(YearlyAuditData st, int i, int ptask, DateTime wrkdate)
        {
            RCodes rcodes = new RCodes
            {
                IsResult = true, 
            };

            DateTime TaskStart = DateTime.Now;
            try 
            {
                bool ChkSumEnabled = true;
                DataTable SrcDt;

                List<VwMapSrc2Dsn> mywork = GetWork2Do(st.ActionKey, st.StepKey);

                string Cutoff = st.EnrollCutoff.HasValue ? DateTime.Parse(st.EnrollCutoff.ToString()).ToShortTimeString() : DateTime.Parse("1900-01-01").ToShortDateString();
                if (st.ActionKey == 3) { ChkSumEnabled = false; } else { ChkSumEnabled = true; }
                string strFlds = GetSLT(mywork, ChkSumEnabled, st.IsNewSchema.Value, st.FromTblVw, st.SiteCode)
                    .Replace("@SiteCode", "'" + st.SiteCode + "'")
                    .Replace("@Samms", "'SAMMS'");
                string strWhere;
                switch (st.FromTblVw.ToLower())
                {
                    case "tblclaims":
                        if (st.ActionKey == 5)
                        {
                            strWhere = "SiteCode = '" + st.SiteCode + "' and Year(convert(date, tpcCreatedDate)) = " + st.WrkYear;
                        }
                        else
                        {
                            strWhere = "Year(convert(date, tpcCreatedDate)) = " + st.WrkYear;
                        }
                        break;
                    //case "tblorder":
                    //    strWhere = "Year(OrderDate) = " + st.WrkYear; // + " and Active = 1";
                    //    break;
                    case "tbldartssvc":
                        strWhere = "Year(dsdtstart) = " + st.WrkYear;
                        break;
                    case "tbldose":
                        strWhere = "Year(dtDate) = " + st.WrkYear;
                        break;
                    case "tbldoseexcuse":
                        strWhere = st.WhereCondition;
                        break;
                    case "tbl3pelig":
                        strWhere = "Year(edate) = " + st.WrkYear;
                        break;
                    case "tbl3payauth":
                        strWhere = "1 = 1";
                        break;
                    default:
                        strWhere = "Year(" + st.DateField + ") = " + st.WrkYear; 
                        break;
                }
                List<string> pkFlds = mywork.Where(x => x.PrimaryKey > 0).OrderBy(o => o.PrimaryKey)
                    .AsEnumerable().Select(s => s.DsnFieldName).ToList();
                
                string strCmd = "Select " + strFlds +
                    " from " + st.SrcSchema + "." + st.FromTblVw;
                //Console.WriteLine(strCmd);
                //Console.ReadKey();
                if (st.ReInitialize)
                {

                }
                else
                {
                    strCmd += " Where " + strWhere;
                }
                if (st.SortOrder != null)
                {
                    strCmd += " " + st.SortOrder;
                }
                ////Get Source Data
                ///
                SQLSvrManager ssm = new SQLSvrManager();
                SrcDt = ssm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                rcodes.RowsProcessed = SrcDt.Rows.Count;
                Console.WriteLine(i.ToString("00000") + "   " + st.SiteCode + " " + st.DsnTbl + " Rows = " +
                    SrcDt.Rows.Count.ToString() + "    " + DateTime.Now.ToShortTimeString());
                //Console.ReadKey();
                
                SaveData sd = new SaveData();
                bool x = false;
                switch (st.DsnTbl.ToLower())
                {
                    case "tbl_clientdemo1":
                        rcodes = sd.SaveClientDemo1var(SrcDt, st.SiteCode, (int)st.ActionKey, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_clientdemo2":
                        if (st.ActionKey == 3)
                        {
                            x = sd.SaveClientDemo3(SrcDt, st.SiteCode);
                        }
                        else
                        {
                            rcodes = sd.SaveClientDemo2(SrcDt, st.SiteCode, (int)st.ActionKey, null);
                            x = rcodes.IsResult;
                        }
                        break;
                    case "tbl_bills":
                        x = sd.SaveBills(SrcDt, st.SiteCode, DateTime.Today, 15, null).IsResult;
                        break;
                    case "tbl_codes":
                        //x = sd.SaveCodes(SrcDt, st.SiteCode, null);
                        x = false;
                        break;
                    case "tbl_checkin":
                        x = sd.SaveCheckIn(SrcDt, st.SiteCode, DateTime.Today, null).IsResult;
                        break;
                    case "tbl_claims":
                        //x = sd.SaveClaims(SrcDt, st.SiteCode, wrkdate, true, null);
                        rcodes = sd.SaveClaims(SrcDt, st.SiteCode, wrkdate, true, null);
                        x = rcodes.IsResult;
                        //if (x)
                        //{
                        //    x = RunClaimCleanup(st.SiteCode, "claims");
                        //}
                        break;
                    case "tbl_claimlineitem":
                        //Console.WriteLine("Running ClaimLineItems");
                        rcodes = sd.SaveClaimLineItem(SrcDt, st.SiteCode, DateTime.Parse("1/1/" + st.WrkYear), null);
                        x = rcodes.IsResult;
                        //if (x)
                        //{
                        //    x = RunClaimCleanup(st.SiteCode, "claimlineitem");
                        //}
                        break;
                    case "tbl_claimlineitemactivity":
                        rcodes = sd.SaveClaimLineItemActivity(SrcDt, st.SiteCode, DateTime.Parse("1/1/"+ st.WrkYear), null);
                        x = rcodes.IsResult;
                        //if (x)
                        //{
                        //    x = RunClaimCleanup(st.SiteCode, "claimlineitemactivity");
                        //}
                        break;
                    case "tbl_clinic":
                        rcodes = sd.SaveClinic(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_dartssrv":
                        switch (st.WrkYear)
                        {
                            case "2008":
                            case "2009":
                            case "2010":
                            case "2011":
                            case "2012":
                            case "2013":
                            case "2014":
                                x = sd.SaveDartSrv2014(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2015":
                                x = sd.SaveDartSrv2015(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2016":
                                x = sd.SaveDartSrv2016(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2017":
                                x = sd.SaveDartSrv2017(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2018":
                                x = sd.SaveDartSrv2018(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2019":
                                x = sd.SaveDartSrv2019(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2020":
                                x = sd.SaveDartSrv2020(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2021":
                                x = sd.SaveDartSrv2021(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                            case "2022":
                                x = sd.SaveDartSrv2022(SrcDt, st.SiteCode, st.ActionKey, DateTime.Parse("1/1/" + st.WrkYear), null);
                                break;
                        }
                        break;
                    case "tbl_dose":
                        rcodes = sd.SaveDoses(SrcDt, st.SiteCode, DateTime.Parse("1/1/" + st.WrkYear), st.ReInitialize, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_dose_excuse":
                        x = sd.SaveDoseExcuse(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_enrollment":
                        if (st.SiteCode != "Lab")
                        {
                            rcodes = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null);
                        }
                        else
                        { x = true; }
                        break;
                    case "tbl_ClinicalOpiateWithdrawalScale":
                        rcodes = sd.SaveGlobalClinicalOpiateWithdrawalScale(SrcDt, st.SiteCode, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_feesched":
                        rcodes = sd.SaveFeeSchedules(SrcDt, st.SiteCode, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_globalpayor":
                        rcodes = sd.SaveGlobalPayer(SrcDt, st.SiteCode, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_payerclient":
                        if (st.FromTblVw.ToString().ToLower() == "vw_payerclt_inactive")
                        {
                            rcodes = sd.RemovePayerClients(SrcDt, st.SiteCode, DateTime.Today, true, null);
                        }
                        else
                        {
                            rcodes = sd.SavePayerClient(SrcDt, st.SiteCode, DateTime.Today, true, null);
                        }
                        break;
                    case "tbl_orders":
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
                        break;
                    case "tbl_uaresults":
                        x = sd.SaveUAResults(SrcDt, st.SiteCode, DateTime.Today, st.ReInitialize, null).IsResult;
                        break;
                    case "tbl_uaresultdetail":
                        x = sd.SaveUAResultDetail(SrcDt, st.SiteCode, DateTime.Today, null).IsResult;
                        break;
                    case "tbl_user":
                        rcodes = sd.SaveGlobalUser(SrcDt, st.SiteCode, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_usersites":
                        rcodes = sd.SaveGlobalUserSite(SrcDt, st.SiteCode, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_consents":
                        rcodes = sd.SaveGlobalConsents(SrcDt, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_3pelig":
                        rcodes = sd.Save3pElig(SrcDt, st.SiteCode, wrkdate, true, null);
                        x = rcodes.IsResult;
                        break;
                    case "tbl_pbi3payauth":
                        x = sd.SaveAuths(SrcDt, st.SiteCode, null).IsResult;
                        break;
                    case "tbl_services":
                        rcodes = sd.SaveServices(SrcDt, st.SiteCode, null);
                        x = rcodes.IsResult;
                        break;
                }
                rcodes.IsResult = x;
                TimeSpan ts = DateTime.Now.Subtract(TaskStart);
                TblTasks mytsk = new TblTasks
                {
                    ActionKey = int.Parse(st.ActionKey.ToString()),
                    ActionStepKey = st.StepKey,
                    DependentTaskId = 0,
                    Duration = ts.Hours.ToString().PadLeft(2, '0') + ":" + ts.Minutes.ToString().PadLeft(2, '0') + ":" + ts.Seconds.ToString().PadLeft(2, '0'),
                    LastModAt = DateTime.Now,
                    LastModBy = Environment.UserName,
                    OnCompletion = 0,
                    OnError = 0,
                    ParentTaskId = ptask,
                    TaskName = st.DsnSchema + "." + st.DsnTbl,
                    RunAt = DateTime.Now,
                    RowState = 24,
                    Status = x ? 19 : 20,
                    SiteCode = st.SiteCode,
                    WorkDate = wrkdate,
                    //DateTime.Parse(DateTime.Today.ToShortDateString().Substring(0,6) + st.WrkYear), 
                    RowCount = SrcDt.Rows.Count,
                    ErrorMessage = rcodes.ExceptMsg + "     " + rcodes.ExceptInnerMsg
                };
                SaveTask(mytsk);
            }
            catch (Exception e)
            {
                rcodes.IsResult = false;
                Console.WriteLine(e.Message.ToString());
            }

            return rcodes;
        }

        public RCodes SyncRDB(Models.VwMapAction st, List<Models.VwMapSrc2Dsn> mywork, DateTime pDT, int Taskid, bool Firsthalf, Models.BHG_DRContext dbs)
        {
            SQLSvrManager ssm = new SQLSvrManager();
            SaveData sd = new SaveData();
            RCodes rCodes = new RCodes
            {
                IsResult = false,
                TaskId = Taskid
            };

            DataTable SrcDt;
            Microsoft.Data.SqlClient.SqlCommand sqlCmd = GetSQLCmd(st, mywork, pDT, Firsthalf);
            string strCmd = sqlCmd.CommandText;

            if (st.DsnTbl != "tbl_bills")
            {
                //Task tasksvc = Task.Run(() => { 
                SrcDt = ssm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
                //});
                Console.WriteLine(st.SiteCode + " " + st.DsnTbl
                    + " Rows = " + SrcDt.Rows.Count.ToString()
                    + "    " + DateTime.Now.ToShortTimeString()
                    + "    " + pDT.ToShortDateString());
                rCodes.RowsProcessed = SrcDt.Rows.Count;
            }
            else
            {
                SrcDt = new DataTable();
            }
            
            DateTime ModAt = DateTime.Now;
            // EntityFrameork version
            //if (SrcDt.Rows.Count > 0)
            //{
                switch (st.DsnTbl.ToLower())
                {
                    case "tbl_clientdemo1":
                        rCodes = sd.SaveClientDemo1var(SrcDt, st.SiteCode, (int)st.ActionKey, null);
                        break;
                    case "tbl_clientdemo2":
                        if (st.ActionKey == 3)
                        {
                            //x = SaveClientDemo3(SrcDt, st.SiteCode);
                        }
                        else
                        {
                            rCodes = sd.SaveClientDemo2(SrcDt, st.SiteCode, (int)st.ActionKey, null);
                        }
                        break;
                    case "tbl_bills":
                        rCodes = sd.SaveBills(SrcDt, st.SiteCode, pDT, 15, null);
                        //rCodes = sd.SaveBills(strCmd, st.ConStr, st.SiteCode, pDT, false);
                        break;
                    case "tbl_codes":
                        rCodes.IsResult = sd.SaveCodes(SrcDt, st.SiteCode, false, dbs);
                        break;
                    case "tbl_checkin":
                        rCodes = sd.SaveCheckIn(SrcDt, st.SiteCode, pDT, null);
                        break;
                    case "tbl_claims":
                        rCodes = sd.SaveClaims(SrcDt, st.SiteCode, pDT, true, null);
                        break;
                    case "tbl_claimlineitem":
                        //Console.WriteLine("Running ClaimLineItems");
                        rCodes = sd.SaveClaimLineItem(SrcDt, st.SiteCode, pDT, null);
                        break;
                    case "tbl_claimlineitemactivity":
                        rCodes = sd.SaveClaimLineItemActivity(SrcDt, st.SiteCode, pDT, null);
                        break;
                    case "tbl_clinic":
                        rCodes = sd.SaveClinic(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_dartssrv":
                        switch (pDT.Year)
                        {
                            case 2015:
                                rCodes.IsResult = sd.SaveDartSrv2015(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2016:
                                rCodes.IsResult = sd.SaveDartSrv2016(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2017:
                                rCodes.IsResult = sd.SaveDartSrv2017(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2018:
                                rCodes.IsResult = sd.SaveDartSrv2018(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2019:
                                rCodes.IsResult = sd.SaveDartSrv2019(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2020:
                                rCodes.IsResult = sd.SaveDartSrv2020(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2021:
                                rCodes.IsResult = sd.SaveDartSrv2021(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                            case 2022:
                                rCodes.IsResult = sd.SaveDartSrv2022(SrcDt, st.SiteCode, st.ActionKey, pDT, null);
                                break;
                        }
                        break;
                    case "tbl_dose":
                        rCodes = sd.SaveDoses(SrcDt, st.SiteCode, pDT, st.ReInitialize, null);
                        break;
                    case "tbl_dose_excuse":
                        rCodes.IsResult = sd.SaveDoseExcuse(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_enrollment":
                        if (st.SiteCode != "Lab")
                        {
                            rCodes = sd.SaveEnrollment(SrcDt, st.SiteCode, st.ActionKey, null);
                        }
                        else
                        { rCodes.IsResult = true; }
                        break;
                    case "tbl_3pelig":
                        rCodes = sd.Save3pElig(SrcDt, st.SiteCode, pDT, true, null);
                        break;
                    case "tbl_clinicalopiatewithdrawalscale":
                        rCodes = sd.SaveGlobalClinicalOpiateWithdrawalScale(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_feesched":
                        rCodes = sd.SaveFeeSchedules(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_formssammsclient":
                        rCodes = sd.SaveGlobalFormsSAMMSClients(SrcDt, st.SiteCode, pDT, Firsthalf, null);
                        break;
                    case "tbl_globalpayor":
                        rCodes = sd.SaveGlobalPayer(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_payerclient":
                        rCodes = sd.SavePayerClient(SrcDt, st.SiteCode, pDT, false, null);
                        break;
                    case "tbl_orders":
                    bool x = true;
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
                    rCodes.IsResult = x;
                    break;
                case "tbl_uaresults":
                        rCodes = sd.SaveUAResults(SrcDt, st.SiteCode, pDT, st.ReInitialize, null);
                        break;
                    case "tbl_uaresultdetail":
                        rCodes = sd.SaveUAResultDetail(SrcDt, st.SiteCode, pDT, null);
                        break;
                    case "tbl_user":
                        rCodes = sd.SaveGlobalUser(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_usersites":
                        rCodes = sd.SaveGlobalUserSite(SrcDt, st.SiteCode, null);
                        break;
                    case "tbl_consents":
                        rCodes = sd.SaveGlobalConsents(SrcDt, null);
                        break;
                    case "tbl_pbi3payauth":
                        rCodes = sd.SaveAuths(SrcDt, st.SiteCode, null);
                        break;
                }
            //}
            //else
            //{ rCodes.IsResult = true; }
            if (!rCodes.IsResult) { Console.WriteLine(st.SiteCode + " " + st.DsnTbl + "Failed to update!"); }
            //else
            //{
            //    //using (var db = new Models.BHG_DRContext())
            //    if (dbs == null) { dbs = new Models.BHG_DRContext(); }
            //    {
            //        Models.TblCtrlSiteTableInitLog stil = dbs.TblCtrlSiteTableInitLog.Where(x => x.SiteCode == st.SiteCode
            //            && x.TableName == st.FromTblVw
            //            && x.WorkDate.Date == pDT.Date).FirstOrDefault();
            //        if (stil == null)
            //        {
            //            stil = new Models.TblCtrlSiteTableInitLog
            //            {
            //                SiteCode = st.SiteCode,
            //                TableName = st.FromTblVw,
            //                WorkDate = pDT.Date,
            //                RunDate = DateTime.Now,
            //                TranRows = SrcDt.Rows.Count
            //            };
            //            dbs.TblCtrlSiteTableInitLog.Add(stil);
            //        }
            //        else
            //        {
            //            stil.TranRows = SrcDt.Rows.Count;
            //            stil.RunDate = DateTime.Now;
            //        }
            //        dbs.SaveChanges();
            //    }
            //}
            return rCodes;
        }

        public bool RunClaimCleanup(string sites, string tblname)
        {
            bool res = true;
            string strCmd = "Select ";
            int i = 0;
            DataTable SiteLocs;
            SQLSvrManager ssm = new SQLSvrManager();
            SaveData sd = new SaveData();

            if (sites.ToLower() == "all")
            {
                SiteLocs = ssm.GetTableData("Sites"
                    , "select distinct SiteCode, ConStr, dbName from ctrl.vw_LocationCons where IsActive = 1 order by SiteCode"
                    , "Data Source=bhgazuresql01.database.windows.net;Initial Catalog=BHG_DR;Persist Security Info=True;User ID=brian.catellier@bhgrecovery.com;Password=Red32Ranger32!;Authentication=\"Active Directory Password\"");
            }
            else
            {
                SiteLocs = ssm.GetTableData("Sites"
                    , "select distinct SiteCode, ConStr, dbName from ctrl.vw_LocationCons where IsActive = 1 and SiteCode = '" + sites + "' order by SiteCode"
                    , "Data Source=bhgazuresql01.database.windows.net;Initial Catalog=BHG_DR;Persist Security Info=True;User ID=brian.catellier@bhgrecovery.com;Password=Red32Ranger32!;Authentication=\"Active Directory Password\"");
            }
            //string[] tblname = { "Claims", "ClaimLineItem", "ClaimLineItemActivity" };
            //foreach (string s in tblname)
            //{
                foreach (DataRow r in SiteLocs.Rows)
                {
                    switch (tblname.ToLower())
                    {
                        case "claims":
                            strCmd = "Select tpcid from tbl3pclaim order by tpcid";
                            break;
                        case "claimlineitem":
                            strCmd = "select tpcliID, tpcliDSID, tpcliTPCID from tbl3pClaimLineItem order by tpcliID, tpcliDSID, tpcliTPCID";
                            break;
                        case "claimlineitemactivity":
                            strCmd = "select liaID, liaTPCLIID, liaDtm from tbl3pClaimLineItemActivity order by liaID";
                            break;
                    }
                    DataTable SrcDt = ssm.GetTableData("ClaimsData", strCmd, r["ConStr"].ToString() + ";Initial Catalog=" + r["dbName"].ToString());
                    res = sd.CleanupDeletedData(SrcDt, r[0].ToString(), tblname, null);
                } 
            //}
            return res;
        }
        public List<Models.VwMapSrc2Dsn> GetWork2Do(long actionkey, int stepkey)
        {
            BHG_DRContext db = new BHG_DRContext();
            return db.WorkToDo.Where(x => x.Enabled
                        && x.ActionKey == actionkey
                        && x.ActionStepKey == stepkey).OrderBy(o => o.FieldKey).ToList();
        }
        public TblTasks GetParentTask(DateTime wrkDate)
        {
            BHG_DRContext db = new BHG_DRContext();
            TblTasks task = db.TblTasks.Where(x => x.TaskName == "SAMMS" && x.RunAt.Date == wrkDate.Date
                && (x.Status == 22 || x.Status == 18 || x.Status == 19)
                && x.RowState == 24).FirstOrDefault();
            if (task == null)
            {
                task = db.TblTasks.Where(x => x.TaskName == "SAMMS"
                    && x.RunAt.Date == wrkDate.AddDays(-1).Date
                    && (x.Status == 22 || x.Status == 18 || x.Status == 19)
                    && x.RowState == 24).FirstOrDefault();
            }
            return task;
        }
        public int GetParentTaskId(DateTime wrkDate)
        {
            int res = 0;
            BHG_DRContext db = new BHG_DRContext();
            TblTasks task = db.TblTasks.Where(x => x.TaskName == "SAMMS" && x.RunAt.Date == wrkDate.Date
                && x.Status == 22
                && x.RowState == 24).FirstOrDefault();
            if (task != null)
            {
                res = task.TaskId;
            }
            else
            {
                task = db.TblTasks.Where(x => x.TaskName == "SAMMS" 
                    && x.RunAt.Date == wrkDate.AddDays(-1).Date
                    && x.Status == 22
                    && x.RowState == 24).FirstOrDefault();
                if (task != null)
                {
                    res = task.TaskId;
                }
            }
            return res;
        }
        private void SaveTask(TblTasks mytask)
        {
            BHG_DRContext db = new BHG_DRContext();
            db.TblTasks.Add(mytask);
            db.SaveChanges();
        }
        private Microsoft.Data.SqlClient.SqlCommand GetSQLCmd(Models.VwMapAction st, List<Models.VwMapSrc2Dsn> mywork, DateTime pDT, bool FirstHalf)
        {
            bool ChkSumEnabled = true;
            string strCmd = "";
            string Cutoff = st.EnrollCutoff.HasValue ? DateTime.Parse(st.EnrollCutoff.ToString()).ToShortTimeString() : DateTime.Parse("1900-01-01").ToShortDateString();
            if (st.ActionKey == 3) { ChkSumEnabled = false; } else { ChkSumEnabled = true; }
            string strFlds = GetSLT(mywork, ChkSumEnabled, st.IsNewSchema.Value, st.FromTblVw, st.SiteCode).Replace("@SiteCode", "'" + st.SiteCode + "'");
            string strWhere = st.WhereCondition.Replace("@SiteCode", "'" + st.SiteCode + "'")
                .Replace("@WorkDate", "'" + pDT.ToShortDateString() + "'")
                .Replace("@EnrollCutoff", "'" + Cutoff + "'")
                .Replace("@Samms", "'SAMMS'");
            List<string> pkFlds = mywork.Where(x => x.PrimaryKey > 0).OrderBy(o => o.PrimaryKey)
                .AsEnumerable().Select(s => s.DsnFieldName).ToList();

            strCmd = "Select " + strFlds + " from " + st.SrcSchema + "." + st.FromTblVw;
            switch (st.FromTblVw.ToLower())
            {
                case "tblclaims":
                    strWhere = "Year(convert(date, tpcCreatedDate)) = " + pDT.Year;
                    break;
                case "tblorder":
                    strWhere = "Year(OrderDate) = " + pDT.Year; // + " and Active = 1";
                    break;
                case "tbldartssvc":
                    strWhere = "Year(dsdtstart) = " + pDT.Year;
                    break;
                case "tbldose":
                    strWhere = "Year(dtDate) = " + pDT.Year;
                    break;
                //case "tblformssammsclient":
                //    if (FirstHalf)
                //    {
                //        strWhere += " and Day(fscDATE) <= 15";
                //    }
                //    else
                //    {
                //        strWhere += " and Day(fscDATE) > 15";
                //    }
                //    break;
                default:
                    break;
            }
            strCmd += " Where " + strWhere;
            if (st.SortOrder != null)
            {
                strCmd += " " + st.SortOrder;
            }

            return new Microsoft.Data.SqlClient.SqlCommand(strCmd, new Microsoft.Data.SqlClient.SqlConnection(st.ConStr));
        }
    }
}
