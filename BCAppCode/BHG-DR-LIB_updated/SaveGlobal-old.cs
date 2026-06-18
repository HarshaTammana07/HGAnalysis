using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveFeeSchedules(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool NewRow = false;
                    bool AllNewRows = false;
                    Models.TblFeeSched fs;
                    List<Models.TblFeeSched> feeScheds = db.TblFeeSched.ToList();
                    if (feeScheds == null) { AllNewRows = true; }
                    foreach (var f in feeScheds.Where(x => x.IsActive == true).ToList())
                    {
                        f.IsActive = false;
                        f.RowChkSum = 0;
                    }
                    db.SaveChanges();
                    foreach (DataRow r in tbl.Rows)
                    {
                        int fsid = int.Parse(r["fsid"].ToString());
                        int myrcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            fs = new Models.TblFeeSched();
                            fs.FsId = fsid;
                            fs.RowChkSum = myrcs;
                            fs.IsActive = true;
                        }
                        else
                        {
                            fs = feeScheds.Where(x => x.FsId == fsid).FirstOrDefault();
                            if (fs == null)
                            {
                                NewRow = true;
                                fs = new Models.TblFeeSched();
                                fs.FsId = fsid;
                                fs.RowChkSum = myrcs;
                                fs.IsActive = true;
                            }
                        }
                        if ((fs.RowChkSum != myrcs) || (NewRow))
                        {
                            fs.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            fs.IsActive = true;
                            fs.FsPayid = r["FsPayid"].ToString();
                            fs.DsService = r["DsService"].ToString();
                            fs.Cptcode = r["cptcode"].ToString();
                            if (r["fee"].ToString().ToString().Length > 0) { fs.Fee = decimal.Parse(r["fee"].ToString()); }
                            if (r["Contractual"].ToString().Length > 0) { fs.Contractual = decimal.Parse(r["Contractual"].ToString()); }
                            if (r["Datespan"].ToString().Length > 0) { fs.Datespan = bool.Parse(r["Datespan"].ToString()); }
                            if (r["GroupTogether"].ToString().Length > 0) { fs.GroupTogether = bool.Parse(r["GroupTogether"].ToString()); }
                            fs.Modifier = r["Modifier"].ToString();
                            fs.UnitMin = r["UnitMin"].ToString();
                            if (r["ProviderBill"].ToString().Length > 0) { fs.ProviderBill = bool.Parse(r["ProviderBill"].ToString()); }
                            if (r["CoAble"].ToString().Length > 0) { fs.CoAble = bool.Parse(r["CoAble"].ToString()); }
                            if (r["DefaultWeekFee"].ToString().Length > 0) { fs.DefaultWeekFee = bool.Parse(r["DefaultWeekFee"].ToString()); }
                            fs.RevCode = r["RevCode"].ToString();
                            if (r["Startdate"].ToString().Length > 7) { fs.Startdate = DateTime.Parse(r["Startdate"].ToString()); }
                            if (r["Enddate"].ToString().Length > 7) { fs.Enddate = DateTime.Parse(r["Enddate"].ToString()); }
                            else { fs.Enddate = null; }
                            fs.Pos = r["pos"].ToString();
                            if (r["AttendingBill"].ToString().Length > 0) { fs.AttendingBill = bool.Parse(r["AttendingBill"].ToString()); }
                            if (r["Pay2310A"].ToString().Length > 0) { fs.Pay2310A = bool.Parse(r["Pay2310A"].ToString()); }
                            if (r["Pay2310C"].ToString().Length > 0) { fs.Pay2310C = bool.Parse(r["Pay2310C"].ToString()); }
                            if (r["ReferredByAttending"].ToString().Length > 0) { fs.ReferredByAttending = bool.Parse(r["ReferredByAttending"].ToString()); }
                            if (r["BillAttendingOrder"].ToString().Length > 0) { fs.BillAttendingOrder = bool.Parse(r["BillAttendingOrder"].ToString()); }
                            if (r["BillOrderDoctor"].ToString().Length > 0) { fs.BillOrderDoctor = bool.Parse(r["BillOrderDoctor"].ToString()); }
                            if (r["FsMasterId"].ToString().Length > 0) { fs.FsMasterId = int.Parse(r["FsMasterId"].ToString()); }
                            fs.Notes1 = r["Notes1"].ToString();
                            fs.Notes2 = r["Notes2"].ToString();
                            fs.LastModAt = DateTime.Now;
                            if (NewRow)
                            {
                                NewRow = false;
                                db.TblFeeSched.Add(fs);
                            }
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    rc.IsResult = false;
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    rc.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveGlobalPayer(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool NewRow = false;
                    bool AllNewRows = false;
                    Models.TblGlobalPayor gp;
                    List<Models.TblGlobalPayor> globalPayors = db.TblGlobalPayor.ToList();
                    if (globalPayors.Count == 0) { AllNewRows = true; }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int intgp = int.Parse(r["PayID"].ToString());
                        int myrcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            gp = new Models.TblGlobalPayor();
                            gp.PayId = intgp;
                            gp.RowChkSum = myrcs;
                            NewRow = true;
                        }
                        else
                        {
                            gp = globalPayors.Where(x => x.PayId == intgp).FirstOrDefault();
                            if (gp == null)
                            {
                                gp = new Models.TblGlobalPayor();
                                gp.PayId = intgp;
                                gp.RowChkSum = myrcs;
                                NewRow = true;
                            }
                        }
                        if ((gp.RowChkSum != myrcs) || (NewRow))
                        {
                            gp.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            gp.PayName = r["payname"].ToString();
                            gp.PayAddress = r["payaddress"].ToString();
                            gp.PayCity = r["paycity"].ToString();
                            gp.PaySt = r["payst"].ToString();
                            gp.Payzip = r["payzip"].ToString();
                            gp.PayPh = r["payph"].ToString();
                            gp.PayFx = r["payfx"].ToString();
                            gp.PayNote = r["paynote"].ToString();
                            gp.PayDefaultsubmit = r["paydefaultsubmit"].ToString();
                            gp.PayAuthformat = r["payauthformat"].ToString();
                            if (r["payclmnum"].ToString().Trim().Length > 0) { gp.PayClmnum = int.Parse(r["payclmnum"].ToString()); }
                            if (r["pay835"].ToString().Trim().Length > 0) { gp.Pay835 = bool.Parse(r["pay835"].ToString()); }
                            gp.PaySubmitType = r["paysubmittype"].ToString();
                            if (r["paybillamt"].ToString().Trim().Length > 0) { gp.PayBillamt = decimal.Parse(r["paybillamt"].ToString()); }
                            gp.PayDosetype = r["paydosetype"].ToString();
                            gp.PayerNumber = r["payernumber"].ToString();
                            gp.Payaddressjoin = r["payaddressjoin"].ToString();
                            if (r["paycheckauth"].ToString().Trim().Length > 0) { gp.PayCheckAuth = int.Parse(r["paycheckauth"].ToString()); }
                            gp.Payclass = r["payclass"].ToString();
                            gp.PayIndfreq = r["payindfreq"].ToString();
                            if (r["payindrate"].ToString().Trim().Length > 0) { gp.PayindRate = decimal.Parse(r["payindrate"].ToString()); }
                            if (r["payindunit"].ToString().Trim().Length > 0) { gp.PayIndunit = int.Parse(r["payindunit"].ToString()); }
                            gp.PayLabelName = r["paylabelname"].ToString();
                            gp.Paynamejoin = r["paynamejoin"].ToString();
                            gp.PayOverride = r["payoverride"].ToString();
                            gp.PayPos = r["paypos"].ToString();
                            gp.Payregion = r["payregion"].ToString();
                            if (r["payreqauth"].ToString().Trim().Length > 0) { gp.PayReqauth = bool.Parse(r["payreqauth"].ToString()); }
                            if (r["paysig"].ToString().Trim().Length > 0) { gp.Paysig = bool.Parse(r["paysig"].ToString()); }
                            gp.PayGlclass = r["payglclass"].ToString();
                            if (r["paylab"].ToString().Trim().Length > 0) { gp.PayLab = bool.Parse(r["paylab"].ToString()); }
                            if (r["noclaimlevelrendering"].ToString().Length > 0) { gp.NoClaimLevelRendering = bool.Parse(r["NoClaimLevelRendering"].ToString()); }
                            if (r["enddate"].ToString().Trim().Length > 7) { gp.Enddate = DateTime.Parse(r["EndDate"].ToString()); }
                            gp.Revcode = r["revcode"].ToString();
                            if (r["startdate"].ToString().Trim().Length > 7) { gp.StartDate = DateTime.Parse(r["startdate"].ToString()); }
                            if (r["pay2310a"].ToString().Trim().Length > 0) { gp.Pay2310A = bool.Parse(r["pay2310a"].ToString()); }
                            if (r["pay2310b"].ToString().Trim().Length > 0) { gp.Pay2310B = bool.Parse(r["pay2310b"].ToString()); }
                            if (r["pay2310c"].ToString().Trim().Length > 0) { gp.Pay2310C = bool.Parse(r["pay2310c"].ToString()); }
                            if (r["supresssecondary"].ToString().Trim().Length > 0) { gp.SupressSecondary = bool.Parse(r["supresssecondary"].ToString()); }
                            gp.AltTaxId = r["alttaxid"].ToString();
                            gp.LastModAt = DateTime.Now;
                            if (NewRow)
                            {
                                NewRow = false;
                                db.TblGlobalPayor.Add(gp);
                            }
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    rc.IsResult = false;
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    rc.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes SaveGlobalUser(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    List<Models.TblUser> users = db.TblUser.ToList();
                    List<Models.TblUser> newUsers = new List<Models.TblUser>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        if (r["uskey"].ToString().Length > 0)
                        {
                            int ukey = int.Parse(r["UsKey"].ToString());
                            Models.TblUser user = users.Where(x => x.Uskey == ukey).FirstOrDefault();
                            if (user == null)
                            {
                                user = new Models.TblUser
                                {
                                    Uskey = ukey
                                };
                                newUsers.Add(user);
                            }
                            user.Userfullname = r["userfullname"].ToString();
                            user.EmailId = r["emailid"].ToString();
                            if (r["isdasacounselor"].ToString().Length > 0)
                            {
                                user.IsDasacounselor = int.Parse(r["isdasacounselor"].ToString());
                            }
                            if (r["usrActive"].ToString().Length > 0)
                            {
                                user.UsrActive = bool.Parse(r["usrActive"].ToString());
                            }
                            if (r["usrcalendaruser"].ToString().Length > 0)
                            { user.UsrCalendarUser = bool.Parse(r["usrcalendaruser"].ToString()); }
                            if (r["usrcosig"].ToString().Length > 0)
                            { user.UsrCosig = bool.Parse(r["usrcosig"].ToString()); }
                            if (r["usrcounselor"].ToString().Length > 0)
                            { user.UsrCounselor = bool.Parse(r["usrcounselor"].ToString()); }
                            user.Usrcred = r["usrcred"].ToString();
                            user.UsrDea = r["usrDea"].ToString();
                            user.UsrDescription = r["usrdescription"].ToString();
                            if (r["usrdoctor"].ToString().Length > 0)
                            { user.UsrDoctor = bool.Parse(r["usrdoctor"].ToString()); }
                            user.UsrExt = r["usrext"].ToString();
                            user.UsrFname = r["usrfname"].ToString();
                            user.UsrGroups = r["usrgroups"].ToString();
                            if (r["usrlicensed"].ToString().Length > 0)
                            { user.UsrLicensed = bool.Parse(r["usrlicensed"].ToString()); }
                            user.UsrLname = r["usrlname"].ToString();
                            user.UsrLocation = r["usrlocation"].ToString();
                            user.UsrName = r["usrname"].ToString();
                            user.Usrnpi = r["usrnpi"].ToString();
                            user.UsrPassword = r["usrpassword"].ToString();
                            if (r["usrpasswordchanged"].ToString().Length > 0)
                            { user.UsrPasswordChanged = DateTime.Parse(r["usrpasswordchanged"].ToString()); }
                            user.Usrphone = r["usrphone"].ToString();
                            user.UsrPin = r["usrpin"].ToString();
                            user.UsrRole = r["usrrole"].ToString();
                            user.UsrSignature = r["usrsignature"].ToString();
                            if (r["usrsignatureimage"].ToString().Length > 0)
                            { user.UsrSignatureImage = Encoding.ASCII.GetBytes(r["usrsignatureimage"].ToString()); }
                            user.UsrSsn = r["usrssn"].ToString();
                            user.UsrSuper = r["usrsuper"].ToString();
                            user.UsrTaxonomy = r["usrtaxonomy"].ToString();
                            user.UsrTemplate = r["usrtemplate"].ToString();
                            if (r["usrtemplatechanged"].ToString().Length > 0)
                            { user.Usrtemplatechanged = bool.Parse(r["usrtemplatechanged"].ToString()); }
                            user.Usrxdea = r["usrxdea"].ToString();
                        }
                    }
                    db.TblUser.UpdateRange(users);
                    db.SaveChanges();
                    if (newUsers.Count > 0)
                    {
                        db.TblUser.AddRange(newUsers);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptInnerMsg = e.InnerException.Message;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message.ToString());
            }
            return rc;
        }
        public Models.RCodes SaveGlobalUserSite(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    //bool AllNewRows = false;
                    bool NewRow = false;
                    List<Models.TblUserSites> sites = db.TblUserSites.ToList();
                    //if (sites.Count == 0) { AllNewRows = true; }
                    foreach (DataRow r in tbl.Rows)
                    {
                        if (r["usid"].ToString().Length > 0)
                        {
                            int usid = int.Parse(r["usid"].ToString());
                            Models.TblUserSites s = sites.Where(x => x.UsId == usid).FirstOrDefault();
                            if (s == null)
                            {
                                s = new Models.TblUserSites
                                {
                                    UsId = usid
                                };
                            }
                            s.UsName = r["usName"].ToString();
                            if (r["ussite"].ToString().Length > 0)
                            {
                                s.UsSite = int.Parse(r["ussite"].ToString());
                            }
                            if (r["usdefault"].ToString().Length > 0)
                            {
                                s.UsDefault = bool.Parse(r["usdefault"].ToString());
                            }
                            if (NewRow)
                            {
                                NewRow = false;
                                sites.Add(s);
                            }
                        }
                    }
                    db.TblUserSites.UpdateRange(sites);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                rc.ExceptInnerMsg = e.InnerException.Message;
                Console.WriteLine(e.Message.ToString());
            }

            return rc;
        }
        public Models.RCodes SaveGlobalClinicalOpiateWithdrawalScale(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try 
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblClinicalOpiateWithdrawalScale> cos = db.TblClinicalOpiateWithdrawalScale.ToList();
                foreach(var v in cos)
                {
                    v.RowState = false;
                }
                List<Models.TblLocations> sites = db.TblLocations.Where(x => x.IsActive).ToList();
                foreach(DataRow r in tbl.Rows)
                {
                    int fid = int.Parse(r["fid"].ToString());
                    int sid = int.Parse(r["SiteCode"].ToString());
                    int rcs = int.Parse(r["RowChkSum"].ToString());

                    Models.TblLocations site = sites.Where(x => x.SId == sid).FirstOrDefault();
                    if (site != null)
                    {
                        Models.TblClinicalOpiateWithdrawalScale c = cos.Where(x => x.FId == fid).FirstOrDefault();
                        if (c == null)
                        {
                            c = new Models.TblClinicalOpiateWithdrawalScale
                            {
                                FId = fid,
                                SiteCode = site.SiteCode, 
                                RowState = true, 
                                RowChkSum = 0
                            };
                            cos.Add(c);
                            db.TblClinicalOpiateWithdrawalScale.Add(c);
                        }
                        if (rcs != c.RowChkSum)
                        {
                            c.RowChkSum = rcs;
                            c.RowState = true;
                            c.LastModAt = DateTime.Now;
                            c.RowChkSum = rcs;
                            c.SiteCode = site.SiteCode;
                            c.FCltId = int.Parse(r["fcltid"].ToString());
                            c.CompletedName = r["completedname"].ToString();
                            c.CombinedScore = r["combinedscore"].ToString();
                            c.AssessDate = r["AssessDate"].ToString();
                            c.ReasonAssessList = r["reasonassesslist"].ToString();
                            c.RestingPulseNum = r["restingpulsenum"].ToString();
                            c.GiupsetNum = r["giupsetnum"].ToString();
                            c.SweatNum = r["sweatnum"].ToString();
                            c.TremorNum = r["tremornum"].ToString();
                            c.RestlessNum = r["restlessnum"].ToString();
                            c.YawnNum = r["yawnnum"].ToString();
                            c.PupilNum = r["pupilnum"].ToString();
                            c.AnxNum = r["anxnum"].ToString();
                            c.BoneNum = r["bonenum"].ToString();
                            c.GooseNum = r["goosenum"].ToString();
                            c.RunnyNum = r["runnynum"].ToString();
                            c.GenevaTest = r["genevaTest"].ToString();
                            c.TimeAmpm = r["timeampm"].ToString();
                            c.AssesstimeText = r["assesstimetext"].ToString();
                        }
                        else
                        {
                            //c.LastModAt = DateTime.Now;
                            c.RowState = true;
                        }
                    }
                }
                db.SaveChanges();
            }
            catch(Exception e)
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
