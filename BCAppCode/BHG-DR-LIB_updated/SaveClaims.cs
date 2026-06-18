using BHG_DR_LIB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public RCodes SaveClaims(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            RCodes res = new RCodes
            {
                IsResult = true, RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    Models.TblClaims clm;
                    List<Models.TblClaims> claims;
                    if (yearly)
                    {
                        claims = db.TblClaims.Where(x => x.SiteCode == sc
                            //&& x.TpcCreatedDate.Value.Year == wrkdt.Year
                            ).ToList();
                        foreach (TblClaims c in claims)
                        {
                            if (c.TpcCreatedDate.Value.Year == wrkdt.Year)
                            {
                                c.RowState = false;
                            }
                        }
                    }
                    else
                    {
                        claims = db.TblClaims.Where(x => x.SiteCode == sc
                            //&& x.TpcCreatedDate.Value.Date == wrkdt.Date
                            ).ToList();
                    }
                    if (claims.Count == 0) { AllNewRows = true; }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int inttpcID = int.Parse(r["tpcid"].ToString());
                        int rcs = int.Parse(r["rowchksum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            clm = new Models.TblClaims
                            {
                                SiteCode = sc,
                                TpcId = inttpcID,
                                RowChkSum = rcs
                            };
                            res.RowsIns += 1;
                        }
                        else
                        {
                            clm = claims.Where(x => x.TpcId == inttpcID).FirstOrDefault();
                            if (clm == null)
                            {
                                clm = new Models.TblClaims
                                {
                                    SiteCode = sc,
                                    TpcId = inttpcID,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else
                            { res.RowsUpd += 1; }
                        }
                        if (NewRow || (rcs != clm.RowChkSum))
                        {
                            clm.LastModAt = RunDT;
                            clm.RowState = true;
                            clm.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            clm.TpccltId = int.Parse(r["tpccltid"].ToString());
                            clm.TpcStrStatus = r["tpcstrstatus"].ToString();
                            clm.TpcStrPayer = r["tpcstrpayer"].ToString();
                            if (r["tpcDtmAdded"].ToString().Length > 7)
                            {
                                clm.TpcDtmAdded = DateTime.Parse(r["tpcDtmAdded"].ToString());
                            }
                            clm.TpcStrAdded = r["tpcstrAdded"].ToString();
                            clm.F10oth = r["f10oth"].ToString();
                            if (r["tpcclaimbatchid"].ToString().Length > 0)
                            {
                                clm.TpcClaimBatchId = int.Parse(r["tpcclaimbatchid"].ToString());
                            }
                            clm.F11insnumber = r["f11insnumber"].ToString();
                            clm.F11insplan = r["f11insplan"].ToString();
                            clm.F11inssex = r["f11inssex"].ToString();
                            clm.F12sig = r["f12sig"].ToString();
                            clm.F12sigdate = r["f12sigdate"].ToString();
                            clm.F13inssig = r["f13inssig"].ToString();
                            clm.F14date = r["f14date"].ToString();
                            clm.F15firstdate = r["f15firstdate"].ToString();
                            clm.F16dateunableend = r["f16dateunableend"].ToString();
                            clm.F10auto = r["f10auto"].ToString();
                            clm.TpcStrPrimary = r["tpcstrprimary"].ToString();
                            clm.F10employ = r["f10employ"].ToString();
                            clm.F10local = r["f10local"].ToString();
                            clm.F11insanother = r["f11insanother"].ToString();
                            clm.F11insdob = r["f11insdob"].ToString();
                            clm.F11insemploy = r["f11insemploy"].ToString();
                            clm.F16dateunablestart = r["f16dateunablestart"].ToString();
                            clm.F17refername = r["f17refername"].ToString();
                            clm.F17refernpi = r["f17refernpi"].ToString();
                            clm.F18datehospend = r["f18datehospend"].ToString();
                            clm.F18datehospstart = r["f18datehospstart"].ToString();
                            clm.F19local = r["f19local"].ToString();
                            clm.F1id = r["f1id"].ToString();
                            clm.F20outsidelab = r["f20outsidelab"].ToString();
                            clm.F21diag1 = r["f21diag1"].ToString();
                            clm.F21diag2 = r["f21diag2"].ToString();
                            clm.F21diag3 = r["f21diag3"].ToString();
                            clm.F21diag4 = r["f21diag4"].ToString();
                            clm.F22medresub = r["f22medresub"].ToString();
                            clm.F23priorauth = r["f23priorauth"].ToString();
                            clm.F25taxid = r["f25taxid"].ToString();
                            clm.F26account = r["f26account"].ToString();
                            clm.F27assign = r["f27assign"].ToString();
                            clm.F28totalcharge = r["f28totalcharge"].ToString();
                            clm.F29amtpaid = r["f29amtpaid"].ToString();
                            clm.F2name = r["f2name"].ToString();
                            clm.F30balancedue = r["f30balancedue"].ToString();
                            clm.F31date = r["f31date"].ToString();
                            clm.F31phys = r["f31phys"].ToString();
                            clm.F32a = r["f32a"].ToString();
                            clm.F32b = r["f32b"].ToString();
                            clm.F32line1 = r["f32line1"].ToString();
                            clm.F32line2 = r["f32line2"].ToString();
                            clm.F32line3 = r["f32line3"].ToString();
                            clm.F32line4 = r["f32line4"].ToString();
                            clm.F33a = r["f33a"].ToString();
                            clm.F33b = r["f33b"].ToString();
                            clm.F33line1 = r["f33line1"].ToString();
                            clm.F33line2 = r["f33line2"].ToString();
                            clm.F33line3 = r["f33line3"].ToString();
                            clm.F33line4 = r["f33line4"].ToString();
                            clm.F33phone = r["f33phone"].ToString();
                            clm.F3dob = r["f3dob"].ToString();
                            clm.F4insname = r["f4insname"].ToString();
                            clm.F5add = r["f5add"].ToString();
                            clm.F5city = r["f5city"].ToString();
                            clm.F5phone = r["f5phone"].ToString();
                            clm.F5state = r["f5state"].ToString();
                            clm.F5zip = r["f5zip"].ToString();
                            clm.F6insrel = r["f6insrel"].ToString();
                            clm.F7insadd = r["f7insadd"].ToString();
                            clm.F7inscity = r["f7inscity"].ToString();
                            clm.F7insphone = r["f7insphone"].ToString();
                            clm.F7insstate = r["f7insstate"].ToString();
                            clm.F7inszip = r["f7inszip"].ToString();
                            clm.F8stat = r["f8stat"].ToString();
                            clm.F9othinsdob = r["f9othinsdob"].ToString();
                            clm.F9othinsemp = r["f9othinsemp"].ToString();
                            clm.F9othinsname = r["f9othinsname"].ToString();
                            clm.F9othinsnumber = r["f9othinsnumber"].ToString();
                            clm.F9othinsplan = r["f9othinsplan"].ToString();
                            clm.F9othinssex = r["f9othinssex"].ToString();
                            if (r["tpcCreatedDate"].ToString().Length > 7)
                            {
                                clm.TpcCreatedDate = DateTime.Parse(r["tpcCreatedDate"].ToString());
                            }
                            clm.TpcEncounter = r["tpcEncounter"].ToString();
                            clm.TpcRebillreason = r["tpcrebillreason"].ToString();
                            clm.TpcStrWeek = r["tpcstrweek"].ToString();
                            if (r["tpcwkstart"].ToString().Length > 0)
                            {
                                clm.TpcWkstart = DateTime.Parse(r["tpcwkstart"].ToString()).Date;
                            }
                            clm.TpcPayerCin = r["tpcpayercin"].ToString();
                            clm.TpcSrvType = r["tpcsrvtype"].ToString();
                            clm.F3sex = r["f3sex"].ToString();
                            if (r["tpcclaimtype"].ToString().Length > 0)
                            {
                                clm.TpcClaimType = int.Parse(r["tpcClaimtype"].ToString());
                            }
                            if (r["SiteId"].ToString().Length > 0)
                            {
                                clm.SiteId = int.Parse(r["SiteID"].ToString());
                            }
                            clm.TpcDbnotes = r["tpcdbnotes"].ToString();
                            clm.TpcReferring = r["tpcreferring"].ToString();

                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                db.TblClaims.Add(clm);
                            }
                        }
                        else
                        { 
                            clm.RowState = true;
                            clm.LastModAt = RunDT;
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message;
                    res.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return res;
        }
        public RCodes SaveClaimLineItem(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            RCodes res = new RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            //List<Models.TblClaimLineItem> nClms = new List<TblClaimLineItem>();
            //List<Models.TblClaimLineItem> uClms = new List<TblClaimLineItem>();
            Models.TblClaimLineItem li = new TblClaimLineItem();
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    List<Models.TblClaimLineItem> items;
                    if (wrkdt.Date == DateTime.Parse("1/1/" + wrkdt.Year.ToString()))
                    {
                        items = db.TblClaimLineItem.Where(x => x.SiteCode == sc && x.TpcliDtmAdded.Value.Year == wrkdt.Year).ToList();
                        foreach(TblClaimLineItem t in items)
                        { t.RowState = false; }
                    }
                    else
                    {
                        items = db.TblClaimLineItem.Where(x => x.SiteCode == sc
                                    && x.TpcliDtmAdded.Value.Date == wrkdt.Date).ToList();
                    }
                    if (items.Count == 0)
                    {
                        //Console.WriteLine("No Records selected");
                        AllNewRows = true;
                        NewRow = true;
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        //Console.WriteLine("We have a row to add");
                        int intClt = int.Parse(r["tpcliid"].ToString());
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            li = new Models.TblClaimLineItem
                            {
                                SiteCode = sc,
                                TpcliId = intClt,
                                RowChkSum = rcs
                            };
                            NewRow = true;
                            res.RowsIns += 1;
                        }
                        else
                        {
                            li = items.Where(x => x.TpcliId == intClt).FirstOrDefault();
                            if (li == null)
                            {
                                li = new Models.TblClaimLineItem { SiteCode = sc, TpcliId = intClt, RowChkSum = rcs };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else
                            {
                                res.RowsUpd += 1;
                            }
                        }
                        if (NewRow || rcs != li.RowChkSum)
                        {
                            //Console.WriteLine(intClt.ToString());
                            li.LastModAt = RunDT;
                            li.RowState = true;
                            li.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["tpclitpcid"].ToString().Length > 0)
                            {
                                li.TpcliTpcid = int.Parse(r["tpclitpcid"].ToString());
                            }
                            if (r["tpcliDtmService"].ToString().Length > 7)
                            {
                                li.TpcliDtmService = DateTime.Parse(r["tpcliDtmService"].ToString());
                            }
                            li.TpcliTxtService = r["tpclitxtservice"].ToString();
                            if (r["tpcliintunits"].ToString().Length > 0)
                            {
                                li.TpcliIntUnits = int.Parse(r["tpcliintunits"].ToString());
                            }
                            if (r["tpcliDtmAdded"].ToString().Length > 7)
                            {
                                li.TpcliDtmAdded = DateTime.Parse(r["tpcliDtmAdded"].ToString());
                            }
                            if (r["tpcliAmtcharge"].ToString().Length > 0)
                            {
                                li.TpcliAmtCharge = decimal.Parse(r["tpcliAmtcharge"].ToString());
                            }
                            li.TpcliStrAdded = r["tpclistradded"].ToString();
                            li.TpcliStrCpt = r["tpclistrcpt"].ToString();
                            li.TpcliStrModifier = r["tpclistrModifier"].ToString();
                            li.TpcliStrNdc = r["tpclistrndc"].ToString();
                            li.TpcliStrPos = r["tpclistrpos"].ToString();
                            if (r["tpcliintdx1"].ToString().Length > 0)
                            {
                                li.TpcliIntDx1 = int.Parse(r["tpcliintdx1"].ToString());
                            }
                            if (r["tpcliintdx2"].ToString().Length > 0)
                            {
                                li.TpcliIntDx2 = int.Parse(r["tpcliintdx2"].ToString());
                            }
                            if (r["tpcliintdx3"].ToString().Length > 0)
                            {
                                li.TpcliIntDx3 = int.Parse(r["tpcliintdx3"].ToString());
                            }
                            if (r["tpcliintdx4"].ToString().Length > 0)
                            {
                                li.TpcliIntDx4 = int.Parse(r["tpcliintdx4"].ToString());
                            }
                            li.TpcliDiagnosis = r["tpclidiagnosis"].ToString();
                            if (r["tpclidsid"].ToString().Length > 0)
                            {
                                li.TpcliDsid = int.Parse(r["tpclidsid"].ToString());
                            }
                            li.TpcliPayerClaimId = r["tpclipayerclaimid"].ToString();
                            li.TpcliProviderId = r["tpcliproviderid"].ToString();
                            if (r["tpcliunitfee"].ToString().Length > 0)
                            {
                                li.TpcliUnitfee = decimal.Parse(r["tpcliunitfee"].ToString());
                            }
                            if (r["tpclivoid"].ToString().Length > 0)
                            {
                                li.TpcliVoid = bool.Parse(r["tpclivoid"].ToString());
                            }
                            if (r["tpclivoiddt"].ToString().Length > 3)
                            {
                                li.TpclivoidDt = DateTime.Parse(r["tpclivoiddt"].ToString()).Date;
                            }
                            li.TpclivoidUser = r["tpclivoiduser"].ToString();
                            if (r["tpcliDtmServiceTo"].ToString().Length > 7)
                            {
                                li.TpcliDtmServiceTo = DateTime.Parse(r["tpcliDtmServiceTo"].ToString());
                            }
                            if (r["tpcliintmg"].ToString().Length > 0)
                            {
                                li.TpcliIntMg = int.Parse(r["tpcliintmg"].ToString());
                            }
                            li.TpcliDbnotes = r["tpclidbnotes"].ToString();
                            if (NewRow || AllNewRows)
                            {
                                db.TblClaimLineItem.Add(li);
                                //db.SaveChanges();
                                NewRow = false;
                            }
                            //db.SaveChanges();
                        }
                        else
                        {
                            li.LastModAt = RunDT;
                            li.RowState = true;
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message;
                    res.ExceptInnerMsg = e.InnerException.Message; 
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    { Console.WriteLine(e.InnerException.Message); }
                    Console.WriteLine(li.SiteCode + ", " + li.TpcliId.ToString());
                }
            }
            return res;
        }
        public RCodes SaveClaimLineItemActivity(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            RCodes res = new RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    Models.TblClaimLineItemActivity lia;
                    List<Models.TblClaimLineItemActivity> clia;
                    if (wrkdt.Date == DateTime.Parse("1/1/"+ wrkdt.Year))
                    {
                        clia = db.TblClaimLineItemActivity.Where(x => x.SiteCode == sc
                            && x.LiaDtm.Value.Year == wrkdt.Year
                            ).ToList();
                        foreach(TblClaimLineItemActivity c in clia)
                        { c.RowState = false; }

                    }
                    else
                    {
                        clia = db.TblClaimLineItemActivity.Where(x => x.SiteCode == sc
                            && x.LiaDtm.Value.Date == wrkdt.Date).ToList();
                    }
                    if (clia.Count == 0)
                    {
                        AllNewRows = true;
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int liaid = int.Parse(r["liaid"].ToString());
                        int rcs = int.Parse(r["rowchksum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            lia = new Models.TblClaimLineItemActivity
                            {
                                SiteCode = sc,
                                RowChkSum = rcs,
                                LiaId = liaid
                            };
                            res.RowsIns += 1;
                        }
                        else
                        {
                            lia = clia.Where(x => x.LiaId == liaid).FirstOrDefault();
                            if (lia == null)
                            {
                                NewRow = true;
                                lia = new Models.TblClaimLineItemActivity
                                {
                                    SiteCode = sc,
                                    LiaId = liaid,
                                    RowChkSum = rcs
                                };
                                res.RowsIns += 1;
                            }
                            else
                            { res.RowsUpd += 1; }
                        }
                        if (NewRow || rcs != lia.RowChkSum)
                        {
                            lia.LastModAt = RunDT;
                            lia.RowState = true;
                            lia.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["liatpcliid"].ToString().Length > 0)
                            {
                                lia.LiaTpcliid = int.Parse(r["liatpcliid"].ToString());
                            }
                            if (r["liadtm"].ToString().Length > 7)
                            {
                                lia.LiaDtm = DateTime.Parse(r["liadtm"].ToString());
                            }
                            lia.LiaStrUser = r["liastruser"].ToString();
                            if (r["laipaidins"].ToString().Length > 0)
                            {
                                lia.LaiPaidins = decimal.Parse(r["laipaidins"].ToString());
                            }
                            if (r["laicontadj"].ToString().Length > 0)
                            {
                                lia.LaiContAdj = decimal.Parse(r["laicontadj"].ToString());
                            }
                            if (r["laigenadj"].ToString().Length > 0)
                            {
                                lia.LaiGenadj = decimal.Parse(r["laigenadj"].ToString());
                            }
                            if (r["laicopay"].ToString().Length > 0)
                            {
                                lia.LaiCopay = decimal.Parse(r["laicopay"].ToString());
                            }
                            if (r["laideduc"].ToString().Length > 0)
                            {
                                lia.LaiDeduc = decimal.Parse(r["laideduc"].ToString());
                            }
                            if (r["laiclient"].ToString().Length > 0)
                            {
                                lia.LaiClient = decimal.Parse(r["laiclient"].ToString());
                            }
                            if (r["liabitnoteonly"].ToString().Length > 0)
                            {
                                lia.LiaBitNoteOnly = bool.Parse(r["liabitnoteonly"].ToString());
                            }
                            lia.LiaStrDesc = r["liastrdesc"].ToString();
                            if (r["tprbid"].ToString().Length > 0)
                            {
                                lia.TprbId = int.Parse(r["tprbid"].ToString());
                            }
                            if (r["liapending"].ToString().Length > 0)
                            {
                                lia.LiaPending = bool.Parse(r["liapending"].ToString());
                            }
                            if (r["liaamt"].ToString().Length > 0)
                            {
                                lia.Liaamt = decimal.Parse(r["liaamt"].ToString());
                            }
                            lia.Liastrtext = r["liastrtext"].ToString();
                            lia.LiaAdjreason = r["liaadjreason"].ToString();
                            if (r["laicoins"].ToString().Length > 0)
                            {
                                lia.LaiCoins = decimal.Parse(r["laicoins"].ToString());
                            }
                            lia.LiaAction1 = r["liaaction1"].ToString();
                            lia.LiaAction2 = r["liaaction2"].ToString();
                            lia.LiaAdjcontract = r["liaadjcontract"].ToString();
                            lia.LiaAdjgeneral = r["liaadjgeneral"].ToString();
                            lia.LiaAnsi1 = r["liaansi1"].ToString();
                            lia.LiaAnsi2 = r["liaansi2"].ToString();
                            lia.LiaAnsimod1 = r["liaansimod1"].ToString();
                            lia.LiaAnsimod2 = r["liaansimod2"].ToString();
                            if (r["billid"].ToString().Length > 0)
                            {
                                lia.BillId = int.Parse(r["billid"].ToString());
                            }
                            lia.LiaDbnotes = r["liadbnotes"].ToString();
                            if (NewRow || AllNewRows)
                            {
                                db.TblClaimLineItemActivity.Add(lia);
                                NewRow = false;
                            }
                        }
                        else 
                        {
                            lia.LastModAt = RunDT;
                            lia.RowState = true;
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message;
                    res.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return res;
        }

        public bool CleanupDeletedData(DataTable tbl, string sc, string tblName, Models.BHG_DRContext db)
        {
            bool res = true;
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                switch (tblName.ToLower())
                {
                    case "claims":
                        // List<SelectClaims> Claims = db.sClaims.Where(x => x.SiteCode == sc).OrderBy(o => o.TpcId).ToList();
                        List<Models.TblClaims> claims = db.TblClaims.Where(x => x.SiteCode == sc).ToList();
                        Console.WriteLine("Claims - " + sc);
                        foreach (Models.TblClaims c in claims)
                        {
                            c.RowState = false;
                        }
                        foreach(DataRow r in tbl.Rows)
                        {
                            int id = int.Parse(r["tpcid"].ToString());
                            Models.TblClaims c = claims.Where(x => x.TpcId == id).FirstOrDefault();
                            if (c != null)
                            {
                                c.RowState = true;
                            }
                            //else
                            //{
                            //    Console.WriteLine(sc + "  " + id.ToString() + " missing");
                            //}
                        }
                        break;
                    case "claimlineitem":
                        List<Models.TblClaimLineItem> clis = db.TblClaimLineItem.Where(x => x.SiteCode == sc)
                            //.OrderBy(o => o.TpcliId)
                            .ToList();
                        Console.WriteLine("ClaimLineItems - " + sc);
                        foreach (Models.TblClaimLineItem scli in clis)
                        {
                            scli.RowState = false;
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            int id = int.Parse(r["tpcliID"].ToString());
                            int sid = int.Parse(r["tpcliDSID"].ToString());
                            int cid = int.Parse(r["tpcliTPCID"].ToString());
                            Models.TblClaimLineItem cli = clis.Where(x => x.TpcliId == id
                                && x.TpcliDsid == sid
                                && x.TpcliTpcid == cid).FirstOrDefault();

                            if (cli != null)
                            {
                                cli.RowState = true;
                            }
                            //else
                            //{
                            //    Console.WriteLine(sc + "  " + id.ToString() + " " + sid.ToString() + " " + cid.ToString() + " missing");
                            //}
                        }
                        break;
                    case "claimlineitemactivity":
                        Console.WriteLine("ClaimLineItemActivity - " + sc);
                        List<Models.TblClaimLineItemActivity> clias = db.TblClaimLineItemActivity.Where(x => x.SiteCode == sc).OrderBy(o => o.LiaId).ToList();
                        foreach (Models.TblClaimLineItemActivity s in clias)
                        {
                            s.RowState = false;
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            int id = int.Parse(r["liaID"].ToString());
                            int tpcid = int.Parse(r["liaTPCLIID"].ToString());
                            string dtm = r["liaDtm"].ToString();
                            Models.TblClaimLineItemActivity clia = clias.Where(x => x.LiaId == id && x.LiaTpcliid == tpcid).FirstOrDefault();
                            if (clia != null)
                            {
                                clia.RowState = true;
                            }
                            //else
                            //{
                            //    Console.WriteLine(sc + "  " + id.ToString() + " " + tpcid.ToString() + " " + r["liaDtm"].ToString() + " missing clia");
                            //}
                        }
                        break;
                }
                db.SaveChanges();
            }
            catch (Exception e)
            {
                res = false;
                Console.WriteLine(e.Message);
                if (e.InnerException.Message != null)
                { Console.WriteLine(e.InnerException.Message); }
            }
            return res;
        }
    }
}
