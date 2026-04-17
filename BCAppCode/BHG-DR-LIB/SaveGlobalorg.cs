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
                    string fsSite = sc; //r["fsSite"].ToString();
                    if (db.TblLocationCons.Where(x => x.SiteCode == sc).FirstOrDefault().ConnectionId == 2)
                    {
                        fsSite = "SAMMS";
                    }
                    bool NewRow = false;
                    bool AllNewRows = false;
                    Models.TblFeeSched fs;
                    List<Models.TblFeeSched> feeScheds = db.TblFeeSched.Where(x => x.FsSite == fsSite).ToList();
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
                            fs = new Models.TblFeeSched
                            {
                                FsId = fsid,
                                FsSite = fsSite,
                                RowChkSum = myrcs,
                                IsActive = true
                            };
                        }
                        else
                        {
                            fs = feeScheds.Where(x => x.FsId == fsid).FirstOrDefault();
                            if (fs == null)
                            {
                                NewRow = true;
                                fs = new Models.TblFeeSched
                                {
                                    FsId = fsid,
                                    FsSite = fsSite,
                                    RowChkSum = myrcs,
                                    IsActive = true
                                };
                            }
                        }
                        if ((fs.RowChkSum != myrcs) || (NewRow))
                        {
                            foreach(DataColumn c in tbl.Columns)
                            {
                                switch(c.ColumnName.ToLower())
                                {
                                    case "rowchksum":
                                        fs.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                        break;
                                    case "fspayid":
                                        fs.FsPayid = r["FsPayid"].ToString();
                                        break;
                                    case "dsservice":
                                        fs.DsService = r["DsService"].ToString();
                                        break;
                                    case "cptcode":
                                        fs.Cptcode = r["cptcode"].ToString();
                                        break;
                                    case "fee":
                                        if (r["fee"].ToString().ToString().Length > 0) { fs.Fee = decimal.Parse(r["fee"].ToString()); }
                                        break;
                                    case "contractual":
                                        if (r["Contractual"].ToString().Length > 0) { fs.Contractual = decimal.Parse(r["Contractual"].ToString()); }
                                        break;
                                    case "datespan":
                                        if (r["Datespan"].ToString().Length > 0) { fs.Datespan = bool.Parse(r["Datespan"].ToString()); }
                                        break;
                                    case "grouptogether":
                                        if (r["GroupTogether"].ToString().Length > 0) { fs.GroupTogether = bool.Parse(r["GroupTogether"].ToString()); }
                                        break;
                                    case "modifier":
                                        fs.Modifier = r["Modifier"].ToString();
                                        break;
                                    case "unitmin":
                                        fs.UnitMin = r["UnitMin"].ToString();
                                        break;
                                    case "providerbill":
                                        if (r["ProviderBill"].ToString().Length > 0) { fs.ProviderBill = bool.Parse(r["ProviderBill"].ToString()); }
                                        break;
                                    case "coable":
                                        if (r["CoAble"].ToString().Length > 0) { fs.CoAble = bool.Parse(r["CoAble"].ToString()); }
                                        break;
                                    case "defaultweekfee":
                                        if (r["DefaultWeekFee"].ToString().Length > 0) { fs.DefaultWeekFee = bool.Parse(r["DefaultWeekFee"].ToString()); }
                                        break;
                                    case "revcode":
                                        fs.RevCode = r["RevCode"].ToString();
                                        break;
                                    case "startdate":
                                        if (r["Startdate"].ToString().Length > 7) { fs.Startdate = DateTime.Parse(r["Startdate"].ToString()); }
                                        break;
                                    case "enddate":
                                        if (r["Enddate"].ToString().Length > 7) { fs.Enddate = DateTime.Parse(r["Enddate"].ToString()); }
                                        else { fs.Enddate = null; }
                                        break;
                                    case "pos":
                                        fs.Pos = r["pos"].ToString();
                                        break;
                                    case "attendingbill":
                                        if (r["AttendingBill"].ToString().Length > 0) { fs.AttendingBill = bool.Parse(r["AttendingBill"].ToString()); }
                                        break;
                                    case "pay2310a":
                                        if (r["Pay2310A"].ToString().Length > 0) { fs.Pay2310A = bool.Parse(r["Pay2310A"].ToString()); }
                                        break;
                                    case "pay2310c":
                                        if (r["Pay2310C"].ToString().Length > 0) { fs.Pay2310C = bool.Parse(r["Pay2310C"].ToString()); }
                                        break;
                                    case "referredbyattending":
                                        if (r["ReferredByAttending"].ToString().Length > 0) { fs.ReferredByAttending = bool.Parse(r["ReferredByAttending"].ToString()); }
                                        break;
                                    case "billattendingorder":
                                        if (r["BillAttendingOrder"].ToString().Length > 0) { fs.BillAttendingOrder = bool.Parse(r["BillAttendingOrder"].ToString()); }
                                        break;
                                    case "billorderdoctor":
                                        if (r["BillOrderDoctor"].ToString().Length > 0) { fs.BillOrderDoctor = bool.Parse(r["BillOrderDoctor"].ToString()); }
                                        break;
                                    case "fsmasterid":
                                        if (r["FsMasterId"].ToString().Length > 0) { fs.FsMasterId = int.Parse(r["FsMasterId"].ToString()); }
                                        break;
                                    case "notes1":
                                        fs.Notes1 = r["Notes1"].ToString();
                                        break;
                                    case "notes2":
                                        fs.Notes2 = r["Notes2"].ToString();
                                        break;
                                }
                            }
                            fs.IsActive = true;
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
                    int conid = db.TblLocationCons.Where(x => x.SiteCode == sc).FirstOrDefault().ConnectionId;
                    string siteCode = conid switch
                    {
                        2 => "SAMMS",
                        4 => "AdvMD-AR",
                        _ => sc,
                    };
                    bool NewRow = false;
                    bool AllNewRows = false;
                    Models.TblGlobalPayor gp;
                    List<Models.TblGlobalPayor> globalPayors = db.TblGlobalPayor.Where(x => x.SiteCode == siteCode).ToList();
                    if (globalPayors.Count == 0) { AllNewRows = true; }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int intgp = int.Parse(r["PayID"].ToString());
                        int myrcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            gp = new Models.TblGlobalPayor
                            {
                                PayId = intgp,
                                RowChkSum = myrcs,
                                SiteCode = siteCode
                            };
                            NewRow = true;
                        }
                        else
                        {
                            gp = globalPayors.Where(x => x.PayId == intgp).FirstOrDefault();
                            if (gp == null)
                            {
                                gp = new Models.TblGlobalPayor
                                {
                                    PayId = intgp,
                                    RowChkSum = myrcs,
                                    SiteCode = siteCode
                                };
                                NewRow = true;
                            }
                        }
                        if ((gp.RowChkSum != myrcs) || (NewRow))
                        {
                            foreach (DataColumn c in tbl.Columns)
                            {
                                switch (c.ColumnName.ToLower())
                                {
                                    case "sitecode":
                                        gp.SiteCode = siteCode;
                                        break;
                                    case "rowchksum":
                                        gp.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                        break;
                                    case "payname":
                                        gp.PayName = r["payname"].ToString();
                                        break;
                                    case "payaddress":
                                        gp.PayAddress = r["payaddress"].ToString();
                                        break;
                                    case "paycity":
                                        gp.PayCity = r["paycity"].ToString();
                                        break;
                                    case "payst":
                                        gp.PaySt = r["payst"].ToString();
                                        break;
                                    case "payzip":
                                        gp.Payzip = r["payzip"].ToString();
                                        break;
                                    case "payph":
                                        gp.PayPh = r["payph"].ToString();
                                        break;
                                    case "payfx":
                                        gp.PayFx = r["payfx"].ToString();
                                        break;
                                    case "paynote":
                                        gp.PayNote = r["paynote"].ToString();
                                        break;
                                    case "paydefaultsubmit":
                                        gp.PayDefaultsubmit = r["paydefaultsubmit"].ToString();
                                        break;
                                    case "payauthformat":
                                        gp.PayAuthformat = r["payauthformat"].ToString();
                                        break;
                                    case "payclmnnum":
                                        if (r["payclmnum"].ToString().Trim().Length > 0) { gp.PayClmnum = int.Parse(r["payclmnum"].ToString()); }
                                        break;
                                    case "pay835":
                                        if (r["pay835"].ToString().Trim().Length > 0) { gp.Pay835 = bool.Parse(r["pay835"].ToString()); }
                                        break;
                                    case "paysubmittype":
                                        gp.PaySubmitType = r["paysubmittype"].ToString();
                                        break;
                                    case "paybillamt":
                                        if (r["paybillamt"].ToString().Trim().Length > 0)
                                        { gp.PayBillamt = decimal.Parse(r["paybillamt"].ToString()); }
                                        break;
                                    case "paydosetype":
                                        gp.PayDosetype = r["paydosetype"].ToString();
                                        break;
                                    case "payernumber":
                                        gp.PayerNumber = r["payernumber"].ToString();
                                        break;
                                    case "payaddressjoin":
                                        gp.Payaddressjoin = r["payaddressjoin"].ToString();
                                        break;
                                    case "paycheckauth":
                                        if (r["paycheckauth"].ToString().Trim().Length > 0)
                                        { gp.PayCheckAuth = int.Parse(r["paycheckauth"].ToString()); }
                                        break;
                                    case "payclass":
                                        gp.Payclass = r["payclass"].ToString();
                                        break;
                                    case "payindfreq":
                                        gp.PayIndfreq = r["payindfreq"].ToString();
                                        break;
                                    case "payindrate":
                                        if (r["payindrate"].ToString().Trim().Length > 0)
                                        { gp.PayindRate = decimal.Parse(r["payindrate"].ToString()); }
                                        break;
                                    case "payindunit":
                                        if (r["payindunit"].ToString().Trim().Length > 0)
                                        { gp.PayIndunit = int.Parse(r["payindunit"].ToString()); }
                                        break;
                                    case "paylabelname":
                                        gp.PayLabelName = r["paylabelname"].ToString();
                                        break;
                                    case "paynamejoin":
                                        gp.Paynamejoin = r["paynamejoin"].ToString();
                                        break;
                                    case "payoverride":
                                        gp.PayOverride = r["payoverride"].ToString();
                                        break;
                                    case "paypos":
                                        gp.PayPos = r["paypos"].ToString();
                                        break;
                                    case "payregion":
                                        gp.Payregion = r["payregion"].ToString();
                                        break;
                                    case "payreqauth":
                                        if (r["payreqauth"].ToString().Trim().Length > 0) { gp.PayReqauth = bool.Parse(r["payreqauth"].ToString()); }
                                        break;
                                    case "paysig":
                                        if (r["paysig"].ToString().Trim().Length > 0) { gp.Paysig = bool.Parse(r["paysig"].ToString()); }
                                        break;
                                    case "payglclass":
                                        gp.PayGlclass = r["payglclass"].ToString();
                                        break;
                                    case "paylab":
                                        if (r["paylab"].ToString().Trim().Length > 0) { gp.PayLab = bool.Parse(r["paylab"].ToString()); }
                                        break;
                                    case "noclaimlevelrendering":
                                        if (r["noclaimlevelrendering"].ToString().Length > 0)
                                        { gp.NoClaimLevelRendering = bool.Parse(r["NoClaimLevelRendering"].ToString()); }
                                        break;
                                    case "enddate":
                                        if (r["enddate"].ToString().Trim().Length > 7)
                                        { gp.Enddate = DateTime.Parse(r["EndDate"].ToString()); }
                                        break;
                                    case "revcode":
                                        gp.Revcode = r["revcode"].ToString();
                                        break;
                                    case "startdate":
                                        if (r["startdate"].ToString().Trim().Length > 7)
                                        { gp.StartDate = DateTime.Parse(r["startdate"].ToString()); }
                                        break;
                                    case "pay2310a":
                                        if (r["pay2310a"].ToString().Trim().Length > 0)
                                        { gp.Pay2310A = bool.Parse(r["pay2310a"].ToString()); }
                                        break;
                                    case "pay2310b":
                                        if (r["pay2310b"].ToString().Trim().Length > 0)
                                        { gp.Pay2310B = bool.Parse(r["pay2310b"].ToString()); }
                                        break;
                                    case "pay2310c":
                                        if (r["pay2310c"].ToString().Trim().Length > 0)
                                        { gp.Pay2310C = bool.Parse(r["pay2310c"].ToString()); }
                                        break;
                                    case "supresssecondary":
                                        if (r["supresssecondary"].ToString().Trim().Length > 0)
                                        { gp.SupressSecondary = bool.Parse(r["supresssecondary"].ToString()); }
                                        break;
                                    case "alttaxid":
                                        gp.AltTaxId = r["alttaxid"].ToString();
                                        break;
                                }
                            }
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
                    rc.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine(e.InnerException.Message);
                        rc.ExceptInnerMsg = e.InnerException.Message;
                    }
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
                            foreach(DataColumn dc in tbl.Columns)
                            {
                                switch (dc.ColumnName.ToLower())
                                {
                                    case "fcltid":
                                        c.FCltId = int.Parse(r["fcltid"].ToString());
                                        break;
                                    case "completedname":
                                        c.CompletedName = r["completedname"].ToString();
                                        break;
                                    case "combinedscore":
                                        c.CombinedScore = r["combinedscore"].ToString();
                                        break;
                                    case "assessdate":
                                        c.AssessDate = r["AssessDate"].ToString();
                                        break;
                                    case "reasonassesslist":
                                        c.ReasonAssessList = r["reasonassesslist"].ToString();
                                        break;
                                    case "restingpulsenum":
                                        c.RestingPulseNum = r["restingpulsenum"].ToString();
                                        break;
                                    case "giupsetnum":
                                        c.GiupsetNum = r["giupsetnum"].ToString();
                                        break;
                                    case "sweatnum":
                                        c.SweatNum = r["sweatnum"].ToString();
                                        break;
                                    case "tremornum":
                                        c.TremorNum = r["tremornum"].ToString();
                                        break;
                                    case "restlessnum":
                                        c.RestlessNum = r["restlessnum"].ToString();
                                        break;
                                    case "yawnnum":
                                        c.YawnNum = r["yawnnum"].ToString();
                                        break;
                                    case "pupilnum":
                                        c.PupilNum = r["pupilnum"].ToString();
                                        break;
                                    case "anxnum":
                                        c.AnxNum = r["anxnum"].ToString();
                                        break;
                                    case "bonenum":
                                        c.BoneNum = r["bonenum"].ToString();
                                        break;
                                    case "goosenum":
                                        c.GooseNum = r["goosenum"].ToString();
                                        break;
                                    case "runnynum":
                                        c.RunnyNum = r["runnynum"].ToString();
                                        break;
                                    case "genevatst":
                                        c.GenevaTest = r["genevaTest"].ToString();
                                        break;
                                    case "timeampm":
                                        c.TimeAmpm = r["timeampm"].ToString();
                                        break;
                                    case "assesstimetext":
                                        c.AssesstimeText = r["assesstimetext"].ToString();
                                        break;
                                }
                            }
                            c.RowChkSum = rcs;
                            c.RowState = true;
                            c.LastModAt = DateTime.Now;
                            c.SiteCode = site.SiteCode;
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
        public Models.RCodes SaveGlobalFormsSAMMSClients(DataTable tbl, string sc, Models.BHG_DRContext db)
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
                    List<Models.TblFormsSammsclient> adata = db.TblFormsSammsclient.ToList();
                    DateTime RunDT = DateTime.Now;
                    foreach(DataRow r in tbl.Rows)
                    {
                        Models.TblFormsSammsclient n = new Models.TblFormsSammsclient();
                        foreach(DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "fscsid":
                                    n.Fscsid = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fscDATE":
                                    n.FscDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fscCLTID":
                                    n.FscCltid = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fscsite":
                                    n.Fscsite = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fscFORMID":
                                    n.FscFormid = r[c.ColumnName].ToString();
                                    break;
                                case "fscform":
                                    n.Fscform = r[c.ColumnName].ToString();
                                    break;
                                case "clientSig":
                                    n.ClientSig = r[c.ColumnName].ToString();
                                    break;
                                case "staffSig":
                                    n.StaffSig = r[c.ColumnName].ToString();
                                    break;
                                case "supervisorSig":
                                    n.SupervisorSig = r[c.ColumnName].ToString();
                                    break;
                                case "physicianSig":
                                    n.PhysicianSig = r[c.ColumnName].ToString();
                                    break;
                                case "clientSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.ClientSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "staffSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.StaffSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.SupervisorSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicianSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.PhysicianSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "doctext":
                                    n.Doctext = r[c.ColumnName].ToString();
                                    break;
                                case "nurseSig":
                                    n.NurseSig = r[c.ColumnName].ToString();
                                    break;
                                case "nurseSigBy":
                                    n.NurseSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "nurseSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.NurseSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicianSigBy":
                                    n.PhysicianSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "staffSigBy":
                                    n.StaffSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "supervisorSigBy":
                                    n.SupervisorSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "doctextEditDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.DoctextEditDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "doctextEditBy":
                                    n.DoctextEditBy = r[c.ColumnName].ToString();
                                    break;
                                case "GuardianSig":
                                    n.GuardianSig = r[c.ColumnName].ToString();
                                    break;
                                case "GuardianSigBy":
                                    n.GuardianSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "GuardianSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.GuardianSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ScanLink":
                                    n.ScanLink = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "ScanReplace":
                                    n.ScanReplace = bool.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "clientSigImg":
                                    //n.ClientSigImg =
                                    break;
                                case "GuardianSigImg":
                                    break;
                                case "nurseSigImg":
                                    break;
                                case "physicianSigImg":
                                    break;
                                case "staffSigImg":
                                    break;
                                case "supervisorSigImg":
                                    break;
                                case "BAC":
                                    break;
                                case "AdminNurseSig":
                                    n.AdminNurseSig = r[c.ColumnName].ToString();
                                    break;
                                case "AdminNurseSigBy":
                                    n.AdminNurseSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "AdminnurseSigDate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.AdminnurseSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "AdminnurseSigImg":
                                    break;
                                case "LastModAt":
                                    n.LastModAt = RunDT;
                                    break;
                                case "RowChkSum":
                                    n.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                    break;
                            }
                        }
                        Models.TblFormsSammsclient xn = adata.Where(x => x.Fscsid == n.Fscsid).FirstOrDefault();
                        if (xn == null)
                        {
                            adata.Add(n);
                        }
                        else
                        {
                            xn.FscDate = n.FscDate;
                            xn.FscCltid = n.FscCltid;
                            xn.Fscsite = n.Fscsite;
                            xn.FscFormid = n.FscFormid;
                            xn.Fscform = n.Fscform;
                            xn.ClientSig = n.ClientSig;
                            xn.StaffSig = n.StaffSig;
                            xn.SupervisorSig = n.SupervisorSig;
                            xn.PhysicianSig = n.PhysicianSig;
                            xn.ClientSigDate = n.ClientSigDate;
                            xn.StaffSigDate = n.StaffSigDate;
                            xn.SupervisorSigDate = n.SupervisorSigDate;
                            xn.PhysicianSigDate = n.PhysicianSigDate;
                            xn.Doctext = n.Doctext;
                            xn.NurseSig = n.NurseSig;
                            xn.NurseSigBy = n.NurseSigBy;
                            xn.NurseSigDate = n.NurseSigDate;
                            xn.PhysicianSigBy = n.PhysicianSigBy;
                            xn.StaffSigBy = n.StaffSigBy;
                            xn.DoctextEditDate = n.DoctextEditDate;
                            xn.DoctextEditBy = n.DoctextEditBy;
                            xn.GuardianSig = n.GuardianSig;
                            xn.GuardianSigBy = n.GuardianSigBy;
                            xn.GuardianSigDate = n.GuardianSigDate;
                            xn.ScanLink = n.ScanLink;
                            xn.ScanReplace = n.ScanReplace;
                            xn.ClientSigImg = n.ClientSigImg;
                            xn.GuardianSigImg = n.GuardianSigImg;
                            xn.NurseSigImg = n.NurseSigImg;
                            xn.PhysicianSigImg = n.PhysicianSigImg;
                            xn.StaffSigImg = n.StaffSigImg;
                            xn.SupervisorSigImg = n.SupervisorSigImg;
                            xn.Bac = n.Bac;
                            xn.AdminNurseSig = n.AdminNurseSig;
                            xn.AdminNurseSigBy = n.AdminNurseSigBy;
                            xn.AdminnurseSigDate = n.AdminnurseSigDate;
                            xn.AdminnurseSigImg = n.AdminnurseSigImg;
                            xn.RowChkSum = n.RowChkSum;
                            xn.LastModAt = n.LastModAt;
                        }
                    }
                    db.SaveChanges();
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
    }
}
