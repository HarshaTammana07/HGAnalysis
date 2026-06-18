using BHG_DR_LIB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveEnrollment(DataTable tbl, string sc, long akey, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            List<Models.TblEnrollment> ERNew = new List<Models.TblEnrollment>();
            Models.TblEnrollment eroll = new TblEnrollment();

            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    
                    //May have to filter this by Admit and Discharge
                    List<Models.TblEnrollment> erolls = db.TblEnrollment.Where(x => x.SiteCode == sc).ToList();
                    if (erolls.Count == 0)
                    {
                        AllNewRows = true;
                    }
                    else 
                    { 
                        foreach(TblEnrollment e in erolls)
                        {
                            e.RowState = false;
                        }
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int enid = int.Parse(r["id"].ToString());
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        { myrcs = int.Parse(r["RowChkSum"].ToString()); }
                        if (AllNewRows)
                        {
                            NewRow = true;
                            eroll = new Models.TblEnrollment
                            {
                                SiteCode = sc,
                                Id = enid,
                                RowChkSum = myrcs
                            };
                            res.RowsIns += 1;
                        }
                        else
                        {
                            eroll = erolls.Where(x => x.Id == enid).FirstOrDefault();
                            if (eroll == null)
                            {
                                NewRow = true;
                                eroll = new Models.TblEnrollment
                                {
                                    SiteCode = sc,
                                    Id = enid,
                                    RowChkSum = myrcs
                                };
                                res.RowsIns += 1;
                            }
                            else { res.RowsUpd += 1; }
                        }
                        // Changed 6/26/2023
                        //(eroll.RowChkSum == myrcs) || ((eroll.RowChkSum != myrcs) || (NewRow))
                        if (true)
                        {
                            eroll.RowState = true;
                            eroll.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            eroll.LastModAt = DateTime.Now;
                            foreach (DataColumn c in r.Table.Columns)
                            {
                                switch (c.ColumnName.ToLower())
                                {
                                    case "cltid":
                                        if (r["CltId"].ToString().Length > 0) { eroll.CltId = int.Parse(r["CltId"].ToString()); }
                                        break;
                                    case "program":
                                        eroll.Program = r["Program"].ToString();
                                        break;
                                    case "enrolldate":
                                        if (r["EnrollDate"].ToString().Trim().Length > 7) 
                                        { eroll.EnrollDate = DateTime.Parse(r["EnrollDate"].ToString()); }
                                        break;
                                    case "dischargedate":
                                        if (r["DischargeDate"].ToString().Trim().Length > 7) 
                                        { eroll.DischargeDate = DateTime.Parse(r["DischargeDate"].ToString()); }
                                        else { eroll.DischargeDate = null; }
                                        break;
                                    case "enrollreasoncode":
                                        eroll.EnrollReasonCode = r["EnrollReasonCode"].ToString();
                                        break;
                                    case "enrollreasontext":
                                        eroll.EnrollReasonText = r["EnrollReasonText"].ToString();
                                        break;
                                    case "dischargereasoncode":
                                        eroll.DischargeReasonCode = r["DischargeReasonCode"].ToString();
                                        break;
                                    case "dischargereasontext":
                                        eroll.DischargeReasonText = r["DischargeReasonText"].ToString();
                                        break;
                                    case "strstaff":
                                        eroll.StrStaff = r["StrStaff"].ToString();
                                        break;
                                    case "transfer":
                                        if (r["Transfer"].ToString().Length > 0) { eroll.Transfer = bool.Parse(r["Transfer"].ToString()); }
                                        break;
                                    case "parentenrollid":
                                        if (r["ParentEnrollId"].ToString().Length > 0) { eroll.ParentEnrollId = int.Parse(r["ParentEnrollId"].ToString()); }
                                        break;
                                    case "nodarts_enroll":
                                        if (r["NoDarts_Enroll"].ToString().Length > 0) { eroll.NoDartsEnroll = bool.Parse(r["NoDarts_Enroll"].ToString()); }
                                        break;
                                    case "nodarts_discharge":
                                        if (r["NoDarts_Discharge"].ToString().Length > 0) { eroll.NoDartsDischarge = bool.Parse(r["NoDarts_Discharge"].ToString()); }
                                        break;
                                    case "repoldenroll":
                                        if (r["RepOldEnroll"].ToString().Length > 0) { eroll.RepOldEnroll = decimal.Parse(r["RepOldEnroll"].ToString()); }
                                        break;
                                    case "dasareason":
                                        eroll.Dasareason = r["Dasareason"].ToString();
                                        break;
                                    case "dtlastcontact":
                                        if (r["DtLastContact"].ToString().Length > 7) { eroll.DtLastContact = DateTime.Parse(r["DtLastContact"].ToString()); }
                                        break;
                                    case "strarrests":
                                        eroll.StrArrests = r["StrArrests"].ToString();
                                        break;
                                    case "strbaby":
                                        eroll.StrBaby = r["StrBaby"].ToString();
                                        break;
                                    case "strbabydf":
                                        eroll.StrBabyDf = r["StrBabyDf"].ToString();
                                        break;
                                    case "streduc":
                                        eroll.StrEduc = r["StrEduc"].ToString();
                                        break;
                                    case "strempstat":
                                        eroll.StrEmpStat = r["StrEmpStat"].ToString();
                                        break;
                                    case "strliving":
                                        eroll.StrLiving = r["StrLiving"].ToString();
                                        break;
                                    case "strnilf":
                                        eroll.StrNilf = r["StrNilf"].ToString();
                                        break;
                                    case "strprifreq":
                                        eroll.StrPriFreq = r["StrPriFreq"].ToString();
                                        break;
                                    case "strpriprob":
                                        eroll.StrPriProb = r["StrPriProb"].ToString();
                                        break;
                                    case "strsecfreq":
                                        eroll.StrSecFreq = r["StrSecFreq"].ToString();
                                        break;
                                    case "strsecprob":
                                        eroll.StrSecProb = r["StrSecProb"].ToString();
                                        break;
                                    case "strselfhelp":
                                        eroll.StrSelfHelp = r["StrSelfHelp"].ToString();
                                        break;
                                    case "strselfhelpdet":
                                        eroll.StrSelfHelpDet = r["StrSelfHelpDet"].ToString();
                                        break;
                                    case "strsuppint":
                                        eroll.StrSuppInt = r["StrSuppInt"].ToString();
                                        break;
                                    case "strterfreq":
                                        eroll.StrTerFreq = r["StrTerFreq"].ToString();
                                        break;
                                    case "strterprob":
                                        eroll.StrTerProb = r["StrTerProb"].ToString();
                                        break;
                                    case "strschooljobtraining":
                                        eroll.StrSchoolJobTraining = r["StrSchoolJobTraining"].ToString();
                                        break;
                                    case "counselor":
                                        eroll.Counselor = r["Counselor"].ToString();
                                        break;
                                    case "dischargesubreasoncode":
                                        eroll.DischargeSubReasonCode = r["DischargeSubReasonCode"].ToString();
                                        break;
                                    case "enrollsubreasoncode":
                                        eroll.EnrollSubReasonCode = r["EnrollSubReasonCode"].ToString();
                                        break;
                                    case "ondemand":
                                        if (r["OnDemand"].ToString().Length > 0) { eroll.OnDemand = bool.Parse(r["OnDemand"].ToString()); }
                                        break;
                                    case "physician":
                                        eroll.Physician = r["Physician"].ToString();
                                        break;
                                    case "siteid":
                                        if (eroll.SiteCode == "PHC") { eroll.SiteId = 105; }
                                        else
                                        {
                                            if (r["SiteId"].ToString().Length > 0) { eroll.SiteId = int.Parse(r["SiteId"].ToString()); }
                                        }
                                        break;
                                    case "module":
                                        eroll.Module = r["Module"].ToString();
                                        break;
                                    case "modulenote":
                                        eroll.Modulenote = r["Modulenote"].ToString();
                                        break;
                                    case "dischargeincome":
                                        eroll.DischargeIncome = r["DischargeIncome"].ToString();
                                        break;
                                    case "intakeincome":
                                        eroll.IntakeIncome = r["IntakeIncome"].ToString();
                                        break;
                                    case "strdbnotes":
                                        eroll.StrDbnotes = r["StrDbnotes"].ToString();
                                        break;
                                    case "deleterecord":
                                        eroll.Deleterecord = r["Deleterecord"].ToString();
                                        break;
                                    case "upsizets":
                                        //if (tbl.Columns.Contains("upsizets"))
                                        //{
                                        //    if (r["UpsizeTs"].ToString().Length > 0) 
                                        //    { eroll.UpsizeTs = Encoding.ASCII.GetBytes(r["UpsizeTs"].ToString()); }
                                        //}
                                        break;
                                    case "modality":
                                        eroll.Modality = r[c.ColumnName].ToString();
                                        break;
                                    case "treatmentlevel":
                                        eroll.TreatmentLevel = r[c.ColumnName].ToString();
                                        break;
                                }
                            }
                            #region Hide old code
                            //Console.WriteLine(r["DischargeDate"].ToString());
                            // Add Switch statement for Enrollment Date changes
                            //switch (sc)
                            //{
                            //    case "B24":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate == DateTime.Parse("1950-01-01")) { eroll.EnrollDate = DateTime.Parse("2000-01-01"); }
                            //        }
                            //        break;
                            //    case "B30":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2016-02-01"))
                            //            {
                            //                if (eroll.DischargeDate.HasValue)
                            //                {
                            //                    if (eroll.DischargeDate >= DateTime.Parse("2016-02-01"))
                            //                    {
                            //                        eroll.EnrollDate = DateTime.Parse("2016-02-01");
                            //                    }
                            //                } 
                            //            }
                            //        }
                            //        break;
                            //    case "B33":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate == DateTime.Parse("1913-06-18")) { eroll.EnrollDate = DateTime.Parse("2013-06-18"); }
                            //        }
                            //        break;
                            //    case "B35A":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-06-01")) { eroll.EnrollDate = null; }
                            //        }
                            //        if (eroll.DischargeDate.HasValue)
                            //        {
                            //            if (eroll.DischargeDate < DateTime.Parse("2018-06-01")) { eroll.DischargeDate = null; }
                            //        }
                            //        break;
                            //    case "B36":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2015-09-07")) { eroll.EnrollDate = DateTime.Parse("2015-09-07"); }
                            //        }
                            //        break;
                            //    case "B37":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2015-09-07")) { eroll.EnrollDate = DateTime.Parse("2015-09-07"); }
                            //        }
                            //        break;
                            //    case "B38":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2016-07-25")) { eroll.EnrollDate = DateTime.Parse("2016-07-25"); }
                            //        }
                            //        break;
                            //    case "B39":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2016-07-25")) { eroll.EnrollDate = DateTime.Parse("2016-07-25"); }
                            //        }
                            //        break;
                            //    case "B41":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2016-10-24")) { eroll.EnrollDate = DateTime.Parse("2016-10-24"); }
                            //        }
                            //        break;
                            //    case "B42":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2016-10-24")) { eroll.EnrollDate = DateTime.Parse("2016-10-24"); }
                            //        }
                            //        break;
                            //    case "B42A":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-12-06")) { eroll.EnrollDate = DateTime.Parse("2018-12-06"); }
                            //        }
                            //        break;
                            //    case "B42B":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-12-19")) { eroll.EnrollDate = DateTime.Parse("2018-12-19"); }
                            //        }
                            //        break;
                            //    case "B42C":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2019-07-17")) { eroll.EnrollDate = DateTime.Parse("2019-07-17"); }
                            //        }
                            //        break;
                            //    case "B42D":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2019-06-11")) { eroll.EnrollDate = DateTime.Parse("2019-06-11"); }
                            //        }
                            //        break;
                            //    case "B44":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2017-06-19")) { eroll.EnrollDate = DateTime.Parse("2017-06-19"); }
                            //        }
                            //        break;
                            //    case "B45":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2017-06-19")) { eroll.EnrollDate = DateTime.Parse("2017-06-19"); }
                            //        }
                            //        break;
                            //    case "B46":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2019-03-20")) { eroll.EnrollDate = DateTime.Parse("2019-03-20"); }
                            //        }
                            //        break;
                            //    case "B47":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2019-09-10")) { eroll.EnrollDate = DateTime.Parse("2019-09-10"); }
                            //        }
                            //        break;
                            //    case "B48":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2017-11-19")) { eroll.EnrollDate = DateTime.Parse("2017-11-19"); }
                            //        }
                            //        break;
                            //    case "B52":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-06-01")) { eroll.EnrollDate = DateTime.Parse("2018-06-01"); }
                            //        }
                            //        break;
                            //    case "B54":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-06-01")) { eroll.EnrollDate = DateTime.Parse("2018-06-01"); }
                            //        }
                            //        break;
                            //    case "B55":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-04-22")) { eroll.EnrollDate = DateTime.Parse("2018-04-22"); }
                            //        }
                            //        break;
                            //    case "DRD-SF":
                            //        if (eroll.DischargeDate.HasValue)
                            //        {
                            //            if (eroll.DischargeDate == DateTime.Parse("2019-05-29")) { eroll.DischargeDate = DateTime.Parse("2017-05-25"); }
                            //        }
                            //        break;
                            //    case "NOLA":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("1985-01-01")) { eroll.EnrollDate = DateTime.Parse("1985-01-01"); }
                            //            if (eroll.EnrollDate == DateTime.Parse("6002-07-08")) { eroll.EnrollDate = DateTime.Parse("2006-07-08"); }
                            //        }
                            //        if (eroll.DischargeDate.HasValue)
                            //        {
                            //            if (eroll.DischargeDate < DateTime.Parse("1985-01-01")) { eroll.DischargeDate = DateTime.Parse("1985-01-01"); }
                            //        }
                            //        break;
                            //    case "SFN":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate == DateTime.Parse("2017-05-25")) { eroll.EnrollDate = DateTime.Parse("2017-05-29"); }
                            //            if (eroll.EnrollDate < DateTime.Parse("2017-05-29")) { eroll.EnrollDate = null; }
                            //        }
                            //        if (eroll.DischargeDate.HasValue)
                            //        {
                            //            if (eroll.DischargeDate < DateTime.Parse("2017-05-29")) { eroll.DischargeDate = null; }
                            //        }
                            //        break;
                            //    case "TTCA":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2018-09-24")) { eroll.EnrollDate = DateTime.Parse("2018-09-24"); }
                            //        }
                            //        break;
                            //    case "TTCB":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2019-02-26")) { eroll.EnrollDate = DateTime.Parse("2019-02-26"); }
                            //        }
                            //        break;
                            //    case "V10A":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate <= DateTime.Parse("2016-08-29")) { eroll.EnrollDate = null; }
                            //            if (eroll.EnrollDate == DateTime.Parse("2016-08-30")) { eroll.EnrollDate = DateTime.Parse("2016-08-29"); }
                            //        }
                            //        if (eroll.DischargeDate.HasValue)
                            //        {
                            //            if (eroll.DischargeDate <= DateTime.Parse("2016-08-29")) { eroll.DischargeDate = null; }
                            //        }
                            //        break;
                            //    case "V12":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("1985-01-01")) { eroll.EnrollDate = DateTime.Parse("1985-01-01"); }
                            //        }
                            //        break;
                            //    case "V12A":
                            //        if (eroll.EnrollDate.HasValue)
                            //        {
                            //            if (eroll.EnrollDate < DateTime.Parse("2017-03-30")) { eroll.EnrollDate = null; }
                            //        }
                            //        if (eroll.DischargeDate.HasValue)
                            //        {
                            //            if (eroll.DischargeDate < DateTime.Parse("2017-03-30")) { eroll.DischargeDate = null; }
                            //        }
                            //        break;

                            //    default:
                            //        break;
                            //}
                            #endregion
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                erolls.Add(eroll);
                                ERNew.Add(eroll);
                                //db.SaveChanges();
                            }
                        }
                        //else
                        //{
                        //    eroll.RowState = true;
                        //    eroll.LastModAt = DateTime.Now;
                        //}
                    }
                    db.TblEnrollment.UpdateRange(erolls);
                    if (ERNew.Count > 0)
                    {
                        db.TblEnrollment.AddRange(ERNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message + " " + eroll.SiteCode.ToString() + " " + eroll.CltId.ToString();
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    {
                        res.ExceptInnerMsg = e.InnerException.Message;
                    }
                }
            }
            return res;
        }
    }
}
