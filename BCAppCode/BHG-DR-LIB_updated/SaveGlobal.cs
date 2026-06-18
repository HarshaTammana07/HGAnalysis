using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Data.SqlClient;
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
                            rc.RowsIns += 1;
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
                                rc.RowsIns += 1;
                            }
                            else
                            { rc.RowsUpd += 1; }
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
                    if (sc == "B59A") { siteCode = "AdvMD-NF"; }
                    bool NewRow = false;
                    bool AllNewRows = false;
                    Models.TblGlobalPayor gp;
                    List<Models.TblGlobalPayor> globalPayors = db.TblGlobalPayor.Where(x => x.SiteCode == siteCode).ToList();
                    if (globalPayors.Count == 0) { AllNewRows = true; }
                    else
                    {
                        foreach(var gps in globalPayors.Where(x => x.RowState == 1).ToList())
                        {
                            gps.RowState = 0;
                        }
                    }
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
                                SiteCode = siteCode, 
                                RowState = 1
                            };
                            NewRow = true;
                            rc.RowsIns += 1;
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
                                    SiteCode = siteCode, 
                                    RowState = 1
                                };
                                NewRow = true;
                                rc.RowsIns += 1;
                            }
                            else { rc.RowsUpd += 1; }
                        }
                        if ((gp.RowChkSum != myrcs) || (NewRow))
                        {
                            foreach (DataColumn c in tbl.Columns)
                            {
                                switch (c.ColumnName.ToLower())
                                {
                                    case "sitecode":
                                        gp.SiteCode = siteCode;
                                        gp.RowState = 1;
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
                            gp.RowState = 1;
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
                                rc.RowsIns += 1;
                            }
                            else { rc.RowsUpd += 1; }
                            //user.Userfullname = r["userfullname"].ToString();
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
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
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
                    List<Models.TblUserSites> newSites = new List<Models.TblUserSites>();
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
                                rc.RowsIns += 1;
                            }
                            else { rc.RowsUpd += 1; }
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
                                newSites.Add(s);
                            }
                        }
                    }
                    //db.TblUserSites.UpdateRange(sites);
                    db.SaveChanges();
                    if (newSites.Count > 0 )
                    {
                        db.TblUserSites.AddRange(newSites);
                        db.SaveChanges();
                    }
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
                            rc.RowsIns += 1;
                        }
                        else { rc.RowsUpd += 1; }
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
        public Models.RCodes SaveGlobalFormsSAMMSClients(DataTable tbl, string sc, DateTime FltrDate, bool firsthalf, Models.BHG_DRContext db)
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
                    List<Models.TblLocations> Locs = db.TblLocations.Where(x => x.IsActive
                        && x.SId != null).ToList();
                    List<Models.TblFormsSammsclient> adata = db.TblFormsSammsclient
                        .Where(x => x.FscDate.Value.Date == FltrDate.Date).ToList();
                    foreach (Models.TblFormsSammsclient f in adata)
                    { f.RowState = 0; }
                    List<Models.TblFormsSammsclient> updList = new List<Models.TblFormsSammsclient>();
                    List<Models.TblFormsSammsclient> addList = new List<Models.TblFormsSammsclient>();
                    DateTime RunDT = DateTime.Now;
                    foreach(DataRow r in tbl.Rows)
                    {
                        Models.TblFormsSammsclient n = new Models.TblFormsSammsclient
                        {
                            LastModAt = RunDT,
                            RowState = 1
                        };

                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "fscsid":
                                    n.Fscsid = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fscdate":
                                    n.FscDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fsccltid":
                                    n.FscCltid = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "fscsite":
                                    n.Fscsite = int.Parse(r[c.ColumnName].ToString());
                                    var lc = Locs.Where(x => x.SId == n.Fscsite).FirstOrDefault();
                                    if (lc != null)
                                    { n.SiteCode = lc.SiteCode; }
                                    else
                                    { n.SiteCode = "SAMMS"; }
                                    break;
                                case "fscformid":
                                    n.FscFormid = r[c.ColumnName].ToString();
                                    break;
                                case "fscform":
                                    n.Fscform = r[c.ColumnName].ToString();
                                    break;
                                case "clientsig":
                                    n.ClientSig = r[c.ColumnName].ToString();
                                    break;
                                case "staffsig":
                                    n.StaffSig = r[c.ColumnName].ToString();
                                    break;
                                case "supervisorsig":
                                    n.SupervisorSig = r[c.ColumnName].ToString();
                                    break;
                                case "physiciansig":
                                    n.PhysicianSig = r[c.ColumnName].ToString();
                                    break;
                                case "clientsigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.ClientSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "staffsigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.StaffSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "supervisorsigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.SupervisorSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physiciansigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.PhysicianSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "doctext":
                                    n.Doctext = r[c.ColumnName].ToString();
                                    break;
                                case "nursesig":
                                    n.NurseSig = r[c.ColumnName].ToString();
                                    break;
                                case "nursesigby":
                                    n.NurseSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "nursesigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.NurseSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physiciansigby":
                                    n.PhysicianSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "staffsigby":
                                    n.StaffSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "supervisorsigby":
                                    n.SupervisorSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "doctexteditdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.DoctextEditDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "doctexteditby":
                                    n.DoctextEditBy = r[c.ColumnName].ToString();
                                    break;
                                case "guardiansig":
                                    n.GuardianSig = r[c.ColumnName].ToString();
                                    break;
                                case "guardiansigby":
                                    n.GuardianSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "guardiansigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.GuardianSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "scanlink":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        n.ScanLink = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "scanreplace":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        n.ScanReplace = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientsigimg":
                                    //n.ClientSigImg =
                                    break;
                                case "guardiansigimg":
                                    break;
                                case "nursesigimg":
                                    break;
                                case "physiciansigimg":
                                    break;
                                case "staffsigimg":
                                    break;
                                case "supervisorsigimg":
                                    break;
                                case "bac":
                                    break;
                                case "adminnursesig":
                                    n.AdminNurseSig = r[c.ColumnName].ToString();
                                    break;
                                case "adminnursesigby":
                                    n.AdminNurseSigBy = r[c.ColumnName].ToString();
                                    break;
                                case "adminnursesigdate":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        n.AdminnurseSigDate = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "adminnursesigimg":
                                    break;
                                case "lastmodat":
                                    n.LastModAt = RunDT;
                                    break;
                                case "rowchksum":
                                    n.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                    break;
                            }
                        }
                        Models.TblFormsSammsclient xn = adata.Where(x => x.Fscsid == n.Fscsid).FirstOrDefault();
                        if (xn == null)
                        {
                            addList.Add(n);
                            rc.RowsIns += 1;
                        }
                        else
                        {
                            rc.RowsUpd += 1;
                            xn.RowState = 1;
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
                            //xn.ClientSigImg = n.ClientSigImg;
                            //xn.GuardianSigImg = n.GuardianSigImg;
                            //xn.NurseSigImg = n.NurseSigImg;
                            //xn.PhysicianSigImg = n.PhysicianSigImg;
                            xn.StaffSigImg = n.StaffSigImg;
                            //xn.SupervisorSigImg = n.SupervisorSigImg;
                            xn.Bac = n.Bac;
                            xn.AdminNurseSig = n.AdminNurseSig;
                            xn.AdminNurseSigBy = n.AdminNurseSigBy;
                            xn.AdminnurseSigDate = n.AdminnurseSigDate;
                            //xn.AdminnurseSigImg = n.AdminnurseSigImg;
                            xn.RowChkSum = n.RowChkSum;
                            xn.LastModAt = n.LastModAt;
                            updList.Add(xn);
                        }
                    }
                    //db.TblFormsSammsclient.Bulk
                    tbl.Dispose();
                    if (addList.Count > 0)
                    {
                        db.TblFormsSammsclient.AddRange(addList);
                    }
                    if (updList.Count > 0)
                    {
                        db.TblFormsSammsclient.UpdateRange(updList);
                        List<Models.TblFormsSammsclient> dlt = adata.Where(x => x.RowState == 0).ToList();
                        if (dlt.Count > 0)
                        {
                            db.TblFormsSammsclient.UpdateRange(dlt);
                        }
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message.ToString());
            }
            return rc;
        }
        public Models.RCodes SaveGlobalConsents(DataTable tbl, Models.BHG_DRContext db)
        {
            Models.RCodes rst = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblConsents> dbConsents = db.TblConsents.ToList();
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblConsents nd = new Models.TblConsents();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "cid":
                                nd.Cid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "cname":
                                nd.CName = r[c.ColumnName].ToString();
                                break;
                            case "clientsig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.ClientSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.StaffSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "supervisorsig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.SupervisorSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "physiciansig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.PhysicianSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "nursesig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.NurseSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "guardiansig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.GuardianSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "denyguardian":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.DenyGuardian = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.CDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cdays":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Cdays = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "bac":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Bac = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Ted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "adminnursesig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.AdminnurseSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "blrecurr":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Blrecurr = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ismhform":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.IsMhform = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblConsents xd = db.TblConsents.Where(x => x.Cid == nd.Cid).FirstOrDefault();
                    if (xd == null)
                    {
                        //dbConsents.Add(nd);
                        db.TblConsents.Add(nd);
                        rst.RowsIns += 1;
                    }
                    else
                    {
                        rst.RowsUpd += 1;
                        xd.CName = nd.CName;
                        xd.ClientSig = nd.ClientSig;
                        xd.StaffSig = nd.StaffSig;
                        xd.SupervisorSig = nd.SupervisorSig;
                        xd.PhysicianSig = nd.PhysicianSig;
                        xd.NurseSig = nd.NurseSig;
                        xd.GuardianSig = nd.GuardianSig;
                        xd.DenyGuardian = nd.DenyGuardian;
                        xd.CDeleted = nd.CDeleted;
                        xd.Cdays = nd.Cdays;
                        xd.Bac = nd.Bac;
                        xd.Ted = nd.Ted;
                        xd.AdminnurseSig = nd.AdminnurseSig;
                        xd.Blrecurr = nd.Blrecurr;
                        xd.IsMhform = nd.IsMhform;
                    }
                    db.SaveChanges();
                }
                //db.SaveChanges();
            }
            catch (Exception e)
            {
                rst.IsResult = false;
                if (e.InnerException != null)
                {
                    rst.ExceptInnerMsg = e.InnerException.Message;
                }
                rst.ExceptMsg = e.Message;
                Console.WriteLine(e.Message.ToString());
            }
            return rst;
        }
        public Models.RCodes SaveGlobalConsentsPhc(DataTable tbl, Models.BHG_DRContext db)
        {
            Models.RCodes rst = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblConsents_Phc> dbConsents = db.TblConsentsPhc.ToList();
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblConsents_Phc nd = new Models.TblConsents_Phc();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "cid":
                                nd.Cid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "cname":
                                nd.CName = r[c.ColumnName].ToString();
                                break;
                            case "clientsig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.ClientSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.StaffSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "supervisorsig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.SupervisorSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "physiciansig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.PhysicianSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "nursesig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.NurseSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "guardiansig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.GuardianSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "denyguardian":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.DenyGuardian = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.CDeleted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cdays":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Cdays = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "bac":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Bac = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Ted = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "adminnursesig":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.AdminnurseSig = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "blrecurr":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.Blrecurr = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ismhform":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    nd.IsMhform = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblConsents_Phc xd = db.TblConsentsPhc.Where(x => x.Cid == nd.Cid).FirstOrDefault();
                    if (xd == null)
                    {
                        //dbConsents.Add(nd);
                        db.TblConsentsPhc.Add(nd);
                        rst.RowsIns += 1;
                    }
                    else
                    {
                        rst.RowsUpd += 1;
                        xd.CName = nd.CName;
                        xd.ClientSig = nd.ClientSig;
                        xd.StaffSig = nd.StaffSig;
                        xd.SupervisorSig = nd.SupervisorSig;
                        xd.PhysicianSig = nd.PhysicianSig;
                        xd.NurseSig = nd.NurseSig;
                        xd.GuardianSig = nd.GuardianSig;
                        xd.DenyGuardian = nd.DenyGuardian;
                        xd.CDeleted = nd.CDeleted;
                        xd.Cdays = nd.Cdays;
                        xd.Bac = nd.Bac;
                        xd.Ted = nd.Ted;
                        xd.AdminnurseSig = nd.AdminnurseSig;
                        xd.Blrecurr = nd.Blrecurr;
                        xd.IsMhform = nd.IsMhform;
                    }
                    db.SaveChanges();
                }
                //db.SaveChanges();
            }
            catch (Exception e)
            {
                rst.IsResult = false;
                if (e.InnerException != null)
                {
                    rst.ExceptInnerMsg = e.InnerException.Message;
                }
                rst.ExceptMsg = e.Message;
                Console.WriteLine(e.Message.ToString());
            }
            return rst;
        }
        public Models.RCodes SaveGlobalDevices(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblGlobalDevices> newDevices = new List<Models.TblGlobalDevices>();
                List<Models.TblGlobalDevices> dbGblDevices = db.TblGlobalDevices.ToList();
                List<Models.TblLocations> locations = db.TblLocations.ToList();
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblGlobalDevices gd = new Models.TblGlobalDevices();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                gd.SiteCode = "unk";
                                break;
                            case "did":
                                gd.DId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "ddeviceid":
                                gd.DDeviceid = r[c.ColumnName].ToString();
                                break;
                            case "dsid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DSid = int.Parse(r[c.ColumnName].ToString());
                                    Models.TblLocations lc = locations.FirstOrDefault(x => x.SId == gd.DSid);
                                    if (lc != null)
                                    {
                                        gd.SiteCode = lc.SiteCode;
                                    }
                                }
                                break;
                            case "dpumpnum":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DPumpnum = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dcheckin":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DCheckin = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dpumptype":
                                gd.DPumptype = r[c.ColumnName].ToString();
                                break;
                            case "dtestmode":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DTestmode = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dlabel":
                                gd.DLabel = r[c.ColumnName].ToString();
                                break;
                            case "dreceipt":
                                gd.DReceipt = r[c.ColumnName].ToString();
                                break;
                            case "dsigpad":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DSigpad = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "ddispense":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DDispense = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dfingerprint":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DFingerprint = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dtouchscreen":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DTouchScreen = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dbacqueuepc":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    gd.DBacqueuePc = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblGlobalDevices cgd = dbGblDevices.FirstOrDefault(x => x.DId == gd.DId);
                    if (cgd == null)
                    {
                        newDevices.Add(gd);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        cgd.DBacqueuePc = gd.DBacqueuePc;
                        cgd.DCheckin = gd.DCheckin;
                        cgd.DDeviceid = gd.DDeviceid;
                        cgd.DDispense = gd.DDispense;
                        cgd.DFingerprint = gd.DFingerprint;
                        cgd.DLabel = gd.DLabel;
                        cgd.DPumpnum = gd.DPumpnum;
                        cgd.DPumptype = gd.DPumptype;
                        cgd.DReceipt = gd.DReceipt;
                        cgd.DSid = gd.DSid;
                        cgd.DSigpad = gd.DSigpad;
                        cgd.DTestmode = gd.DTestmode;
                        cgd.DTouchScreen = gd.DTouchScreen;
                    }
                }
                db.SaveChanges();
                if (newDevices.Count > 0)
                {
                    db.TblGlobalDevices.AddRange(newDevices);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
                rc.ExceptMsg = e.Message;
            }
            return rc;
        }
        public Models.RCodes SaveBAM(DataTable tbl, DateTime FltrDate, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (tbl.Rows.Count > 0)
            {
                try
                {
                    if (db == null) { db = new Models.BHG_DRContext(); }

                    List<Models.TblLocations> Locs = db.TblLocations.Where(x => x.IsActive
                        && x.SId != null).ToList();
                    List<Models.TblBriefAddictionMonitor> Bams = db.TblBriefAddictionMonitor
                        .Where(x => x.Date.Value.Date >= FltrDate.Date)
                        .ToList();

                    if (Bams.Count() == 0)
                    {
                        List<Models.TblBriefAddictionMonitor> nbs = new List<Models.TblBriefAddictionMonitor>();
                        foreach (DataRow r in tbl.Rows)
                        {
                            Models.TblBriefAddictionMonitor bam = new Models.TblBriefAddictionMonitor();
                            foreach (DataColumn c in tbl.Columns)
                            {
                                switch (c.ColumnName.ToLower())
                                {
                                    case "fid":
                                        bam.FId = int.Parse(r[c.ColumnName].ToString());
                                        bam.RowState = 1;
                                        break;
                                    case "fclinic":
                                        bam.FClinic = int.Parse(r[c.ColumnName].ToString());
                                        var lcsite = Locs.Where(x => x.SId == bam.FClinic).FirstOrDefault();
                                        if (lcsite == null)
                                        {
                                            bam.SiteCode = "NSL-" + bam.FClinic.ToString();
                                        }
                                        else
                                        {
                                            bam.SiteCode = lcsite.SiteCode;
                                        }
                                        break;
                                    case "fcltid":
                                        bam.FCltId = int.Parse(r[c.ColumnName].ToString());
                                        break;
                                    case "date":
                                        int i = r[c.ColumnName].ToString().IndexOf(", ") + 2;
                                        int l = r[c.ColumnName].ToString().Length - i;
                                        string sd = r[c.ColumnName].ToString().Substring(i, l);
                                        bam.Date = DateTime.Parse(sd.Trim());
                                        break;
                                    case "cliniciantext":
                                        bam.ClinicianText = r[c.ColumnName].ToString();
                                        break;
                                    case "adminlist":
                                        bam.AdminList = r[c.ColumnName].ToString();
                                        break;
                                    case "intervallist":
                                        bam.IntervalList = r[c.ColumnName].ToString();
                                        break;
                                    case "usecalc":
                                        bam.UseCalc = r[c.ColumnName].ToString();
                                        break;
                                    case "riskcalc":
                                        bam.RiskCalc = r[c.ColumnName].ToString();
                                        break;
                                    case "protectivecalc":
                                        bam.ProtectiveCalc = r[c.ColumnName].ToString();
                                        break;
                                    case "q1answerlist":
                                        bam.Q1answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q2answerlist":
                                        bam.Q2answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q3answerlist":
                                        bam.Q3answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q4answerlist":
                                        bam.Q4answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q5answerlist":
                                        bam.Q5answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q6answerlist":
                                        bam.Q6AnswerList = r[c.ColumnName].ToString();
                                        break;
                                    case "test":
                                        bam.Test = r[c.ColumnName].ToString();
                                        break;
                                    case "q1answer":
                                        bam.Q1Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q2answer":
                                        bam.Q2Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q3answer":
                                        bam.Q3answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q4answer":
                                        bam.Q4answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q5answer":
                                        bam.Q5answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q6answer":
                                        bam.Q6answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q7answernumeric":
                                        bam.Q7answerNumeric = r[c.ColumnName].ToString();
                                        break;
                                    case "q7alist":
                                        bam.Q7aList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7blist":
                                        bam.Q7bList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7clist":
                                        bam.Q7cList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7dlist":
                                        bam.Q7dList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7elist":
                                        bam.Q7eList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7flist":
                                        bam.Q7fList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7glist":
                                        bam.Q7gList = r[c.ColumnName].ToString();
                                        break;
                                    case "q8answer":
                                        bam.Q8Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q9answer":
                                        bam.Q9Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q10answer":
                                        bam.Q10Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q11answer":
                                        bam.Q11Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q12answer":
                                        bam.Q12Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q13answer":
                                        bam.Q13Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q14answer":
                                        bam.Q14Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q15answer":
                                        bam.Q15Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q15answer1":
                                        bam.Q15Answer1 = r[c.ColumnName].ToString();
                                        break;
                                    case "q15answer2":
                                        bam.Q15Answer2 = r[c.ColumnName].ToString();
                                        break;
                                    case "q16answer":
                                        bam.Q16Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q17answer":
                                        bam.Q17Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q14answer2":
                                        bam.Q14Answer2 = r[c.ColumnName].ToString();
                                        break;
                                }
                            }
                            if (bam.Date >= FltrDate.Date)
                            {
                                nbs.Add(bam);
                                Models.TblBriefAddictionMonitor xbm = db.TblBriefAddictionMonitor.FirstOrDefault(x => x.SiteCode == bam.SiteCode
                                    && x.FId == bam.FId);
                                if (xbm == null)
                                {
                                    db.TblBriefAddictionMonitor.Add(bam);
                                    rc.RowsIns += 1;
                                }
                                else
                                {
                                    rc.RowsUpd += 1;
                                    xbm.Date = bam.Date;
                                    xbm.FClinic = bam.FClinic;
                                    xbm.FCltId = bam.FCltId;
                                    xbm.AdminList = bam.AdminList;
                                    xbm.ClinicianText = bam.ClinicianText;
                                    xbm.IntervalList = bam.IntervalList;
                                    xbm.ProtectiveCalc = bam.ProtectiveCalc;
                                    xbm.Q10Answer = bam.Q10Answer;
                                    xbm.Q11Answer = bam.Q11Answer;
                                    xbm.Q12Answer = bam.Q12Answer;
                                    xbm.Q13Answer = bam.Q13Answer;
                                    xbm.Q14Answer = bam.Q14Answer;
                                    xbm.Q14Answer2 = bam.Q14Answer2;
                                    xbm.Q15Answer = bam.Q15Answer;
                                    xbm.Q15Answer1 = bam.Q15Answer1;
                                    xbm.Q15Answer2 = bam.Q15Answer2;
                                    xbm.Q16Answer = bam.Q16Answer;
                                    xbm.Q17Answer = bam.Q17Answer;
                                    xbm.Q1Answer = bam.Q1Answer;
                                    xbm.Q1answerList = bam.Q1answerList;
                                    xbm.Q2Answer = bam.Q2Answer;
                                    xbm.Q2answerList = bam.Q2answerList;
                                    xbm.Q3answer = bam.Q3answer;
                                    xbm.Q3answerList = bam.Q3answerList;
                                    xbm.Q4answer = bam.Q4answer;
                                    xbm.Q4answerList = bam.Q4answerList;
                                    xbm.Q5answer = bam.Q5answer;
                                    xbm.Q5answerList = bam.Q5answerList;
                                    xbm.Q6answer = bam.Q6answer;
                                    xbm.Q6AnswerList = bam.Q6AnswerList;
                                    xbm.Q7aList = bam.Q7aList;
                                    xbm.Q7answerNumeric = bam.Q7answerNumeric;
                                    xbm.Q7bList = bam.Q7bList;
                                    xbm.Q7cList = bam.Q7cList;
                                    xbm.Q7dList = bam.Q7dList;
                                    xbm.Q7eList = bam.Q7eList;
                                    xbm.Q7fList = bam.Q7fList;
                                    xbm.Q7gList = bam.Q7gList;
                                    xbm.Q8Answer = bam.Q8Answer;
                                    xbm.Q9Answer = bam.Q9Answer;
                                    xbm.RiskCalc = bam.RiskCalc;
                                    xbm.RowState = bam.RowState;
                                    xbm.Test = bam.Test;
                                    xbm.UseCalc = bam.UseCalc;
                                }
                            }
                            db.SaveChanges();
                        }
                        db.SaveChanges();
                        //if (nbs.Count > 0)
                        //{
                        //    db.TblBriefAddictionMonitor.AddRange(nbs);
                        //    db.SaveChanges();
                        //}
                    }
                    else
                    {
                        List<Models.TblBriefAddictionMonitor> nbs = new List<Models.TblBriefAddictionMonitor>();
                        foreach (DataRow r in tbl.Rows)
                        {
                            Models.TblBriefAddictionMonitor bam = new Models.TblBriefAddictionMonitor();
                            foreach (DataColumn c in tbl.Columns)
                            {
                                switch (c.ColumnName.ToLower())
                                {
                                    case "fid":
                                        bam.FId = int.Parse(r[c.ColumnName].ToString());
                                        bam.RowState = 1;
                                        break;
                                    case "fclinic":
                                        bam.FClinic = int.Parse(r[c.ColumnName].ToString());
                                        var lcsite = Locs.Where(x => x.SId == bam.FClinic).FirstOrDefault();
                                        if (lcsite == null)
                                        {
                                            bam.SiteCode = "NSL-" + bam.FClinic.ToString();
                                        }
                                        else
                                        {
                                            bam.SiteCode = lcsite.SiteCode;
                                        }
                                        break;
                                    case "fcltid":
                                        bam.FCltId = int.Parse(r[c.ColumnName].ToString());
                                        break;
                                    case "date":
                                        int i = r[c.ColumnName].ToString().IndexOf(", ") + 2;
                                        int l = r[c.ColumnName].ToString().Length - i;
                                        string sd = r[c.ColumnName].ToString().Substring(i, l);
                                        bam.Date = DateTime.Parse(sd.Trim());
                                        break;
                                    case "cliniciantext":
                                        bam.ClinicianText = r[c.ColumnName].ToString();
                                        break;
                                    case "adminlist":
                                        bam.AdminList = r[c.ColumnName].ToString();
                                        break;
                                    case "intervallist":
                                        bam.IntervalList = r[c.ColumnName].ToString();
                                        break;
                                    case "usecalc":
                                        bam.UseCalc = r[c.ColumnName].ToString();
                                        break;
                                    case "riskcalc":
                                        bam.RiskCalc = r[c.ColumnName].ToString();
                                        break;
                                    case "protectivecalc":
                                        bam.ProtectiveCalc = r[c.ColumnName].ToString();
                                        break;
                                    case "q1answerlist":
                                        bam.Q1answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q2answerlist":
                                        bam.Q2answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q3answerlist":
                                        bam.Q3answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q4answerlist":
                                        bam.Q4answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q5answerlist":
                                        bam.Q5answerList = r[c.ColumnName].ToString();
                                        break;
                                    case "q6answerlist":
                                        bam.Q6AnswerList = r[c.ColumnName].ToString();
                                        break;
                                    case "test":
                                        bam.Test = r[c.ColumnName].ToString();
                                        break;
                                    case "q1answer":
                                        bam.Q1Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q2answer":
                                        bam.Q2Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q3answer":
                                        bam.Q3answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q4answer":
                                        bam.Q4answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q5answer":
                                        bam.Q5answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q6answer":
                                        bam.Q6answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q7answernumeric":
                                        bam.Q7answerNumeric = r[c.ColumnName].ToString();
                                        break;
                                    case "q7alist":
                                        bam.Q7aList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7blist":
                                        bam.Q7bList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7clist":
                                        bam.Q7cList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7dlist":
                                        bam.Q7dList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7elist":
                                        bam.Q7eList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7flist":
                                        bam.Q7fList = r[c.ColumnName].ToString();
                                        break;
                                    case "q7glist":
                                        bam.Q7gList = r[c.ColumnName].ToString();
                                        break;
                                    case "q8answer":
                                        bam.Q8Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q9answer":
                                        bam.Q9Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q10answer":
                                        bam.Q10Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q11answer":
                                        bam.Q11Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q12answer":
                                        bam.Q12Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q13answer":
                                        bam.Q13Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q14answer":
                                        bam.Q14Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q15answer":
                                        bam.Q15Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q15answer1":
                                        bam.Q15Answer1 = r[c.ColumnName].ToString();
                                        break;
                                    case "q15answer2":
                                        bam.Q15Answer2 = r[c.ColumnName].ToString();
                                        break;
                                    case "q16answer":
                                        bam.Q16Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q17answer":
                                        bam.Q17Answer = r[c.ColumnName].ToString();
                                        break;
                                    case "q14answer2":
                                        bam.Q14Answer2 = r[c.ColumnName].ToString();
                                        break;
                                }
                            }
                            Models.TblBriefAddictionMonitor b = db.TblBriefAddictionMonitor.FirstOrDefault(x => x.SiteCode == bam.SiteCode && x.FId == bam.FId);
                            if (b == null)
                            {
                                nbs.Add(bam);
                                db.TblBriefAddictionMonitor.Add(bam);
                                rc.RowsIns += 1;
                                //db.SaveChanges();
                            }
                            else
                            {
                                rc.RowsUpd += 1;
                                b.Date = bam.Date;
                                b.FClinic = bam.FClinic;
                                b.FCltId = bam.FCltId;
                                b.AdminList = bam.AdminList;
                                b.ClinicianText = bam.ClinicianText;
                                b.IntervalList = bam.IntervalList;
                                b.ProtectiveCalc = bam.ProtectiveCalc;
                                b.Q10Answer = bam.Q10Answer;
                                b.Q11Answer = bam.Q11Answer;
                                b.Q12Answer = bam.Q12Answer;
                                b.Q13Answer = bam.Q13Answer;
                                b.Q14Answer = bam.Q14Answer;
                                b.Q14Answer2 = bam.Q14Answer2;
                                b.Q15Answer = bam.Q15Answer;
                                b.Q15Answer1 = bam.Q15Answer1;
                                b.Q15Answer2 = bam.Q15Answer2;
                                b.Q16Answer = bam.Q16Answer;
                                b.Q17Answer = bam.Q17Answer;
                                b.Q1Answer = bam.Q1Answer;
                                b.Q1answerList = bam.Q1answerList;
                                b.Q2Answer = bam.Q2Answer;
                                b.Q2answerList = bam.Q2answerList;
                                b.Q3answer = bam.Q3answer;
                                b.Q3answerList = bam.Q3answerList;
                                b.Q4answer = bam.Q4answer;
                                b.Q4answerList = bam.Q4answerList;
                                b.Q5answer = bam.Q5answer;
                                b.Q5answerList = bam.Q5answerList;
                                b.Q6answer = bam.Q6answer;
                                b.Q6AnswerList = bam.Q6AnswerList;
                                b.Q7aList = bam.Q7aList;
                                b.Q7answerNumeric = bam.Q7answerNumeric;
                                b.Q7bList = bam.Q7bList;
                                b.Q7cList = bam.Q7cList;
                                b.Q7dList = bam.Q7dList;
                                b.Q7eList = bam.Q7eList;
                                b.Q7fList = bam.Q7fList;
                                b.Q7gList = bam.Q7gList;
                                b.Q8Answer = bam.Q8Answer;
                                b.Q9Answer = bam.Q9Answer;
                                b.RiskCalc = bam.RiskCalc;
                                b.RowState = bam.RowState;
                                b.Test = bam.Test;
                                b.UseCalc = bam.UseCalc;
                            }
                            //db.SaveChanges();
                        }
                        db.SaveChanges();
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    rc.IsResult = false;
                    if (e.InnerException != null)
                    {
                        rc.ExceptInnerMsg = e.InnerException.Message;
                    }
                    rc.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message.ToString());
                }
            }
            return rc;
        }
        public Models.RCodes SaveServices(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                bool AllNewRows = false;
                List<Models.TblServices> services = db.TblServices.Where(x => x.SiteCode == sc).ToList();
                if (services.Count == 0) { AllNewRows = true; }
                else
                {
                    //Set Inactive
                    foreach (Models.TblServices s in services.Where(x => x.IsActive == true).ToList())
                    {
                        s.IsActive = false;
                        //s.LastModAt = DateTime.Now;
                    }
                }
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblServices s = new Models.TblServices();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                s.SiteCode = sc;
                                break;
                            case "rowchksum":
                                s.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "sid":
                                s.SId = int.Parse(r[c.ColumnName].ToString());
                                s.IsActive = true;
                                break;
                            case "sservice":
                                s.SService = r[c.ColumnName].ToString();
                                break;
                            case "sarea":
                                s.SArea = r[c.ColumnName].ToString();
                                break;
                            case "scost":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    s.SCost = decimal.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "scptcode":
                                s.SCptcode = r[c.ColumnName].ToString();
                                break;
                            case "sreqsig":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    s.SReqsig = bool.Parse(r[c.ColumnName].ToString().Trim());
                                }
                                break;
                            case "sreqtime":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    s.SReqtime = bool.Parse(r[c.ColumnName].ToString().Trim());
                                }
                                break;
                            case "blallowoverlap":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    s.BlAllowOverlap = bool.Parse(r[c.ColumnName].ToString().Trim());
                                }
                                break;
                            case "oldarea":
                                s.OldArea = r[c.ColumnName].ToString();
                                break;
                            case "oldsrv":
                                s.OldSrv = r[c.ColumnName].ToString();
                                break;
                            case "sfilter":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    s.SFilter = bool.Parse(r[c.ColumnName].ToString().Trim());
                                }
                                break;
                            case "sreportbillable":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    s.SReportBillable = bool.Parse(r[c.ColumnName].ToString().Trim());
                                }
                                break;
                            case "stimeonly":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    s.STimeOnly = bool.Parse(r[c.ColumnName].ToString().Trim());
                                }
                                break;
                        }
                    }
                    if (AllNewRows)
                    {
                        s.CreatedOn = DateTime.Now;
                        s.LastModAt = s.CreatedOn;
                        db.TblServices.Add(s);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        Models.TblServices svc = services.Where(x => x.SiteCode == s.SiteCode && x.SId == s.SId).FirstOrDefault();
                        if (svc == null)
                        {
                            s.CreatedOn = DateTime.Now;
                            s.LastModAt = s.CreatedOn;
                            s.IsActive = true;
                            services.Add(s);
                            rc.RowsIns += 1;
                            //db.TblServices.Add(s);
                        }
                        else
                        {
                            if (svc.RowChkSum != s.RowChkSum)
                            {
                                svc.RowChkSum = s.RowChkSum;
                                svc.SService = s.SService;
                                svc.SArea = s.SArea;
                                svc.SCost = s.SCost;
                                svc.SCptcode = s.SCptcode;
                                svc.SFilter = s.SFilter;
                                svc.SReportBillable = s.SReportBillable;
                                svc.SReqsig = s.SReqsig;
                                svc.SReqtime = s.SReqtime;
                                svc.STimeOnly = s.STimeOnly;
                                svc.BlAllowOverlap = s.BlAllowOverlap;
                                svc.OldArea = s.OldArea;
                                svc.OldSrv = s.OldSrv;
                                svc.LastModAt = DateTime.Now;
                            }
                            svc.IsActive = true;
                            rc.RowsUpd += 1;
                        }
                        //db.SaveChanges();
                    }
                }
                db.SaveChanges();
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message.ToString());
            }

            return rc;
        }
        public Models.RCodes SaveFormCounts(DataTable tbl, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblFormsCounts> FrmCnts = db.TblFormsCounts.ToList();
                List<Models.TblLocations> Locs = db.TblLocations.Where(x => x.IsActive
                        && x.SId != null).ToList();
                foreach (DataRow r in tbl.Rows)
                {
                    DateTime dt = DateTime.Parse(r["fscDate"].ToString());
                    int sc = int.Parse(r["fscsite"].ToString());
                    var lc = Locs.Where(x => x.SId == sc).FirstOrDefault();
                    if (lc != null)
                    {
                        int sid = int.Parse(r["fscsid"].ToString());
                        int clt = int.Parse(r["fscCLTID"].ToString());
                        int cnt = int.Parse(r["cnt"].ToString());
                        Models.TblFormsCounts frm = FrmCnts.FirstOrDefault(x =>
                            x.FscDate == dt
                            && x.SiteCode == lc.SiteCode
                            && x.fscsid == sid
                            && Math.Abs(x.fscCltID) == Math.Abs(clt));
                        if (frm == null)
                        {
                            frm = new Models.TblFormsCounts { FscDate = dt, SiteCode = lc.SiteCode, fscsid = sid, fscCltID = clt, Cnt = cnt };
                            db.TblFormsCounts.Add(frm);
                            res.RowsIns += 1;
                        }
                        else
                        {
                            res.RowsUpd += 1;
                            frm.Cnt = cnt;
                            frm.fscCltID = clt;
                        }
                        db.SaveChanges();
                    }
                }
            }
            catch(Exception e)
            {
                res.IsResult = false;
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }
        public Models.RCodes SaveClaimStatus (DataTable tbl, DateTime wrkdt, Models.BHG_DRContext db)
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
                    List<Models.TblClaimStatus> dbCS = db.TblClaimStatuses.ToList();
                    List<Models.TblClaimStatus> newCS = new List<Models.TblClaimStatus>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        Models.TblClaimStatus cs = new Models.TblClaimStatus();
                        foreach(DataColumn c in tbl.Columns)
                        {
                            switch(c.ColumnName.ToLower())
                            {
                                case "id":
                                    cs.id = int.Parse(r[c.ColumnName].ToString());
                                    break;
                                case "databasename":
                                    cs.DatabaseName = r[c.ColumnName].ToString();
                                    break;
                                case "fileuploadstatus":
                                    cs.FileUploadStatus = r[c.ColumnName].ToString();
                                    break;
                                case "tpcb837":
                                    cs.tpcb837 = r[c.ColumnName].ToString();
                                    break;
                                case "tpcbdtcreated":
                                    if (r[c.ColumnName].ToString().Length > 6)
                                    {
                                        cs.tpcbDtCreated = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tpcbfile":
                                    cs.tpcbFILE = r[c.ColumnName].ToString();
                                    break;
                                case "tpcbid":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        cs.tpcbID = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tpcbstrsubmittype":
                                    cs.tpcbStrSubmitType = r[c.ColumnName].ToString();
                                    break;
                            }
                            var xcs = dbCS.FirstOrDefault(x => x.id == cs.id);
                            if (xcs == null)
                            {
                                newCS.Add(cs);
                                rc.RowsIns += 1;
                            }
                            else
                            {
                                rc.RowsUpd += 1;
                                xcs.DatabaseName = cs.DatabaseName;
                                xcs.FileUploadStatus = cs.FileUploadStatus;
                                xcs.tpcb837 = cs.tpcb837;
                                xcs.tpcbDtCreated = cs.tpcbDtCreated;
                                xcs.tpcbFILE = cs.tpcbFILE;
                                xcs.tpcbID = cs.tpcbID;
                                xcs.tpcbStrSubmitType = cs.tpcbStrSubmitType;
                            }
                        }
                    }
                    //db.TblClaimStatuses.UpdateRange(dbCS);
                    db.SaveChanges();
                    if (newCS.Count > 0)
                    {
                        db.TblClaimStatuses.AddRange(newCS);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message.ToString());
            }
            return rc;
        }
    }
}
