using BHG_DR_LIB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public RCodes SaveAuths(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes res = new RCodes();
            res.IsResult = true;
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                bool AllNewRows = false;
                bool NewRow = false;
                DateTime RunDT = DateTime.Now;
                Models.TblPbi3Payauth auth;
                List<Models.TblPbi3Payauth> auths;
                auths = db.Pbi3Payauths.Where(x => x.SiteCode == sc).ToList();
                if (auths.Count == 0) { AllNewRows = true; }
                else
                {
                    foreach (TblPbi3Payauth a in auths)
                    { a.RowState = false; }
                }
                foreach (DataRow r in tbl.Rows)
                {
                    int inttpaID = int.Parse(r["tpaid"].ToString());
                    int rcs = int.Parse(r["rowchksum"].ToString());
                    if (AllNewRows)
                    {
                        NewRow = true;
                        auth = new Models.TblPbi3Payauth
                        {
                            SiteCode = sc,
                            TpaId = inttpaID,
                            RowChkSum = rcs
                        };
                        res.RowsIns += 1;
                    }
                    else
                    {
                        auth = auths.Where(x => x.TpaId == inttpaID).FirstOrDefault();
                        if (auth == null)
                        {
                            auth = new Models.TblPbi3Payauth
                            {
                                SiteCode = sc,
                                TpaId = inttpaID,
                                RowChkSum = rcs
                            };
                            NewRow = true;
                            res.RowsIns += 1;
                        }
                    }
                    if (NewRow || (rcs != auth.RowChkSum))
                    {
                        auth.RowState = true;
                        auth.LastModAt = RunDT;
                        auth.RowChkSum = rcs;
                        auth.TpaCltid = int.Parse(r["tpacltid"].ToString());
                        auth.TpaPayer = r["tpaPayer"].ToString();
                        auth.TpaDesc = r["tpadesc"].ToString();
                        if (r["tpeffdate"].ToString().Length > 6)
                        {
                            auth.TpEffdate = DateTime.Parse(r["tpeffdate"].ToString());
                        }
                        if (r["tpaeffdate"].ToString().Length > 6)
                        { auth.TpaEffDate = DateTime.Parse(r["tpaeffdate"].ToString().Replace('-', '/')); }
                        if (r["tpatermdate"].ToString().Length > 6)
                        {
                            auth.TpaTermDate = DateTime.Parse(r["tpatermdate"].ToString().Replace('-', '/'));
                        }
                        auth.TpaStaff = r["tpastaff"].ToString();
                        if (r["tpadt"].ToString().Length > 6)
                        {
                            auth.Tpadt = DateTime.Parse(r["tpadt"].ToString().Replace('-', '/'));
                        }
                        auth.TpaAuthCode = r["tpaauthcode"].ToString();
                        auth.TpAuthpath = r["tpauthpath"].ToString();
                        auth.TpConfirmpath = r["tpconfirmpath"].ToString();
                        auth.TpFail = r["tpfail"].ToString();
                        auth.TpRequestForm = r["tprequestform"].ToString();
                        auth.TpResponseForm = r["tpresponseform"].ToString();
                        auth.TpServ = r["tpserv"].ToString().Trim();
                        if (auth.TpServ.Length > 300)
                        {
                            auth.TpServ = auth.TpServ.Substring(0, 299);
                        }
                        if (r["tptermdate"].ToString().Length > 6)
                        { auth.TpTermDate = DateTime.Parse(r["tptermdate"].ToString().Replace('-', '/')); }
                        if (r["tpunits"].ToString().Length > 0)
                        { auth.TpUnits = int.Parse(r["tpunits"].ToString()); }
                        auth.TpServapproved = r["tpservapproved"].ToString().Trim();
                        auth.TpNote = r["tpnote"].ToString();
                        auth.TpType = r["tptype"].ToString();
                        //auth.TpaCompKey = r["tpacompkey"].ToString();
                        //auth.TpaBigKey = r["tpabigkey"].ToString();
                        //auth.ProgGroup = r["proggroup"].ToString();
                        //auth.PayerGroup = r["payergroup"].ToString();
                        //auth.PayerType = r["payertype"].ToString();
                        res.RowsUpd += 1;
                    }
                    else
                    {
                        auth.RowState = true;
                        auth.LastModAt = RunDT;
                    }
                    if (NewRow || AllNewRows)
                    {
                        NewRow = false;
                        db.Pbi3Payauths.Add(auth);
                        //db.SaveChanges();
                    }
                }
                db.SaveChanges();
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message);
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }

        public RCodes SaveAuthBillsub(DataTable tbl, string sc, DateTime WrkDate, bool Reload, Models.BHG_DRContext db)
        {
            Models.RCodes res = new RCodes
            {
                IsResult = true, 
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            SQLSvrManager sm = new SQLSvrManager();
            try
            {
                DateTime RunDT = DateTime.Now;
                List<Models.Tblvw3pBillSub> BSubs = db.Tblvw3pBillSub.Where(x => x.SiteCode == sc).ToList();
                foreach(var r in BSubs)
                {
                    r.RowState = false;
                }
                //db.SaveChanges();
                List<Models.Tblvw3pBillSub> BSubsNew = new List<Tblvw3pBillSub>();
                foreach(DataRow r in tbl.Rows)
                {
                    Models.Tblvw3pBillSub bs = new Tblvw3pBillSub
                    {
                        SiteCode = sc,
                        LastModAt = RunDT,
                        RowState = true
                    };
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "descript":
                                bs.Descript = r[c.ColumnName].ToString();
                                break;
                            case "billdatecriteria":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    bs.Billdatecriteria = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "paydefaultsubmit":
                                bs.PayDefaultsubmit = r[c.ColumnName].ToString();
                                break;
                            case "scruberror":
                                bs.ScrubError = r[c.ColumnName].ToString();
                                break;
                            case "dsid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    bs.DsId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dsclt":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    bs.DsClt = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dstxtsrv":
                                bs.DsTxtSrv = r[c.ColumnName].ToString();
                                break;
                            case "dsdtstart":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    bs.DsDtStart = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dsdtend":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    bs.DsDtEnd = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dstxttype":
                                bs.DsTxtType = r[c.ColumnName].ToString();
                                break;
                            case "dsdblunits":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    bs.DsdblUnits = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "billunits":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    bs.BillUnits = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dstxtstaff":
                                bs.DstxtStaff = r[c.ColumnName].ToString();
                                break;
                            case "npi":
                                bs.Npi = r[c.ColumnName].ToString();
                                break;
                            case "dsbilled":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    bs.Dsbilled = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "pypayerid":
                                bs.PyPayerid = r[c.ColumnName].ToString().Trim();
                                break;
                            case "pysubsid":
                                bs.PySubsid = r[c.ColumnName].ToString();
                                break;
                            case "pygroup":
                                bs.PyGroup = r[c.ColumnName].ToString().Trim();
                                break;
                            case "cptcode":
                                bs.Cptcode = r[c.ColumnName].ToString();
                                bs.CptMod = r[c.ColumnName].ToString().Trim() + ":" + r["modifier"].ToString().Trim();
                                break;
                            case "charge":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    bs.Charge = double.Parse(r[c.ColumnName].ToString());
                                }
                                else { bs.Charge = double.Parse("0"); }
                                break;
                            case "tpaauthcode":
                                bs.TpaAuthCode = r[c.ColumnName].ToString();
                                break;
                            case "clientname":
                                bs.Clientname = r[c.ColumnName].ToString();
                                break;
                            case "cltdob":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    bs.CltDob = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "cltgender":
                                bs.CltGender = r[c.ColumnName].ToString();
                                break;
                            case "cltadd1":
                                bs.CltAdd1 = r[c.ColumnName].ToString();
                                break;
                            case "cltcity":
                                bs.CltCity = r[c.ColumnName].ToString();
                                break;
                            case "cltState":
                                bs.CltState = r[c.ColumnName].ToString();
                                break;
                            case "cltzip":
                                bs.Cltzip = r[c.ColumnName].ToString();
                                break;
                            case "cltphone":
                                bs.CltPhone = r[c.ColumnName].ToString();
                                break;
                            case "cltmarry":
                                bs.CltMarry = r[c.ColumnName].ToString();
                                break;
                            case "cltm4id":
                                bs.CltM4id = r[c.ColumnName].ToString();
                                break;
                            case "dsdiag":
                                bs.Dsdiag = r[c.ColumnName].ToString();
                                break;
                            case "modifier":
                                bs.Modifier = r[c.ColumnName].ToString();
                                break;
                            case "dspos":
                                bs.DsPos = r[c.ColumnName].ToString();
                                break;
                            case "ndc":
                                bs.Ndc = r[c.ColumnName].ToString();
                                break;
                            case "mg":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    bs.Mg = double.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "siteid":
                                bs.SiteId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "dsarea":
                                bs.Dsarea = r[c.ColumnName].ToString();
                                break;
                            case "payclass":
                                bs.Payclass = r[c.ColumnName].ToString();
                                break;
                            case "rowchksum":
                                bs.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.Tblvw3pBillSub dbs = BSubs.FirstOrDefault(x => x.SiteCode == bs.SiteCode && x.DsId == bs.DsId
                        && x.PyPayerid == bs.PyPayerid && x.PySubsid == bs.PySubsid
                        && x.PyGroup.Trim() == bs.PyGroup.Trim() && x.CptMod == bs.CptMod
                        && x.Charge == bs.Charge );
                    if (dbs == null)
                    {
                        db.SaveChanges();
                        List<Models.Tblvw3pBillSub> dbsx = BSubs.Where(x => x.SiteCode == bs.SiteCode && x.DsId == bs.DsId && x.DsClt == bs.DsClt && x.Modifier == bs.Modifier && x.RowState == false).ToList();
                        if (dbsx.Count == 0)
                        {
                            BSubsNew.Add(bs);
                            res.RowsIns += 1;
                        }
                        else
                        {
                            //Log Remove 
                            try
                            {
                                foreach (var x in dbsx)
                                {
                                    db.Tblvw3pBillSub.Remove(x);
                                }
                                //db.SaveChanges();
                            }
                            catch(Exception e)
                            {
                                //Console.WriteLine("SiteCode: " + dbs.SiteCode.ToString() + "  DSID: " + dbs.DsId.ToString() + "  dsClt: " + dbs.DsClt.ToString() + "   Modifier: " + dbs.Modifier.ToString());
                                //_ = sm.ExeSqlCmd("delete from pats.tbl_vw3pBillSub where sitecode = '" + dbs.SiteCode + "' and dsid = " + dbs.DsId.ToString(), sm.ConnectionString);
                            }
                            //BSubsNew.Add(bs);
                            //
                            //res.RowsIns += 1;
                        }
                    }
                    else
                    {
                        res.RowsUpd += 1;
                        dbs.Billdatecriteria = bs.Billdatecriteria;
                        dbs.BillUnits = bs.BillUnits;
                        //dbs.Charge = bs.Charge;
                        dbs.Clientname = bs.Clientname;
                        dbs.CltAdd1 = bs.CltAdd1;
                        dbs.CltCity = bs.CltCity;
                        dbs.CltDob = bs.CltDob;
                        dbs.CltGender = bs.CltGender;
                        dbs.CltM4id = bs.CltM4id;
                        dbs.CltMarry = bs.CltMarry;
                        dbs.CltPhone = bs.CltPhone;
                        dbs.CltState = bs.CltState;
                        dbs.Cltzip = bs.Cltzip;
                        dbs.Cptcode = bs.Cptcode;
                        dbs.Descript = bs.Descript;
                        dbs.Dsarea = bs.Dsarea;
                        dbs.Dsbilled = bs.Dsbilled;
                        dbs.DsClt = bs.DsClt;
                        dbs.DsdblUnits = bs.DsdblUnits;
                        dbs.Dsdiag = bs.Dsdiag;
                        dbs.DsDtEnd = bs.DsDtEnd;
                        dbs.DsDtStart = bs.DsDtStart;
                        dbs.DsPos = bs.DsPos;
                        dbs.DsTxtSrv = bs.DsTxtSrv;
                        dbs.DstxtStaff = bs.DstxtStaff;
                        dbs.DsTxtType = bs.DsTxtType;
                        dbs.LastModAt = bs.LastModAt;
                        dbs.Mg = bs.Mg;
                        dbs.Modifier = bs.Modifier;
                        dbs.Ndc = bs.Ndc;
                        dbs.Npi = bs.Npi;
                        dbs.Payclass = bs.Payclass;
                        dbs.PayDefaultsubmit = bs.PayDefaultsubmit;
                        //dbs.PyGroup = bs.PyGroup;
                        //dbs.PyPayerid = bs.PyPayerid;
                        //dbs.PySubsid = bs.PySubsid;
                        dbs.RowChkSum = bs.RowChkSum;
                        dbs.RowState = true; // bs.RowState;
                        dbs.ScrubError = bs.ScrubError;
                        dbs.SiteId = bs.SiteId;
                        dbs.TpaAuthCode = bs.TpaAuthCode;
                        //db.SaveChanges();
                    }
                }
                db.SaveChanges();
                if (BSubsNew.Count > 0)
                {
                    db.Tblvw3pBillSub.AddRange(BSubsNew);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message);
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }
    }
}
