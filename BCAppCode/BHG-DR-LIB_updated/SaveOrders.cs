using BHG_DR_LIB.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public bool SaveOrders(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders> ords = new List<Models.TblOrders>();
                        Models.TblOrders o;
                        int onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        List<Models.TblOrders> orders = db.TblOrders.Where(x => x.SiteCode == sc
                                         //&& x.Orderdate.Value.Date == wrkdt.Date)
                                         ).OrderBy(o => o.OrderNum)
                                         //.Take(tbl.Rows.Count + 1500)
                                         .ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                { 
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum) || (rcs < 0))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                o.SigDr = r["sigdr"].ToString();
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                o.Sigentered = r["sigentered"].ToString();
                                o.Signoted = r["signoted"].ToString();
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                o.SigMid = r["sigmid"].ToString();
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString());
                                o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString());
                                o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString());

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2016(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2016> ords = new List<Models.TblOrders2016>();
                        Models.TblOrders2016 o;
                        int onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        List<Models.TblOrders2016> orders = db.TblOrders2016.Where(x => x.SiteCode == sc
                                           //&& x.Orderdate.Value.Year == 2016
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2016 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                { 
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2016
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2016
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                o.SigDr = r["sigdr"].ToString();
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                o.Sigentered = r["sigentered"].ToString();
                                o.Signoted = r["signoted"].ToString();
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                o.SigMid = r["sigmid"].ToString();
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString());
                                o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString());
                                o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString());

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.LastModAt = DateTime.Now;
                                o.RowState = true;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2016.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2017(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2017> ords = new List<Models.TblOrders2017>();
                        Models.TblOrders2017 o;
                        int onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        List<Models.TblOrders2017> orders = db.TblOrders2017.Where(x => x.SiteCode == sc
                                           //&& x.Orderdate.Value.Year == 2017
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2017 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                { 
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2017
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2017
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                o.SigDr = r["sigdr"].ToString();
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                o.Sigentered = r["sigentered"].ToString();
                                o.Signoted = r["signoted"].ToString();
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                o.SigMid = r["sigmid"].ToString();
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString());
                                o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString());
                                o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString());

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.LastModAt = DateTime.Now;
                                o.RowState = true;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2017.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2018(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2018> ords = new List<Models.TblOrders2018>();
                        Models.TblOrders2018 o;
                        int onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        List<Models.TblOrders2018> orders = db.TblOrders2018.Where(x => x.SiteCode == sc
                                           //&& x.Orderdate.Value.Year == 2018
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2018 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                { 
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2018
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2018
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                o.SigDr = r["sigdr"].ToString();
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                o.Sigentered = r["sigentered"].ToString();
                                o.Signoted = r["signoted"].ToString();
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                o.SigMid = r["sigmid"].ToString();
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString());
                                o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString());
                                o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString());

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2018.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2019(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2019> ords = new List<Models.TblOrders2019>();
                        Models.TblOrders2019 o;
                        int onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        List<Models.TblOrders2019> orders = db.TblOrders2019.Where(x => x.SiteCode == sc
                                           //&& x.DateAdded.Value.Year == 2019
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2019 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                { 
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2019
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2019
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                //Console.WriteLine(o.OrderNum.ToString() + "   " + r["effectivedate"].ToString() + "    " + r["expirationdate"].ToString() + "   " + r["dose"].ToString() + "   " + r["dose2"].ToString());
                                if (r["effectivedate"].ToString().Length > 7) { o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString()); }
                                if (r["expirationdate"].ToString().Length > 7) { o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString()); }
                                if (r["dose"].ToString().Length > 0) { o.Dose = decimal.Parse(r["dose"].ToString()); }
                                if (r["dose2"].ToString().Length > 0) { o.Dose2 = decimal.Parse(r["dose2"].ToString()); }
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                o.SigDr = r["sigdr"].ToString();
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                o.Sigentered = r["sigentered"].ToString();
                                o.Signoted = r["signoted"].ToString();
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                o.SigMid = r["sigmid"].ToString();
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString());
                                o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString());
                                o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString());

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2019.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2020(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2020> ords = new List<Models.TblOrders2020>();
                        Models.TblOrders2020 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2020> orders = db.TblOrders2020.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2020 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2020
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2020
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (tbl.Columns.Contains("sogentereddt"))
                                {
                                    if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                }
                                if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2020.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2021(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2021> ords = new List<Models.TblOrders2021>();
                        Models.TblOrders2021 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2021> orders = db.TblOrders2021.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2021 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2021
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2021
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (tbl.Columns.Contains("sogentereddt"))
                                {
                                    if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                }
                                if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2021.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2022(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2022> ords = new List<Models.TblOrders2022>();
                        Models.TblOrders2022 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2022> orders = db.TblOrders2022.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2022 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2022
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2022
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString().Trim();
                                if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (tbl.Columns.Contains("sogentereddt"))
                                {
                                    if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                }
                                if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2022.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2023(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2023> ords = new List<Models.TblOrders2023>();
                        Models.TblOrders2023 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2023> orders = db.TblOrders2023.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2023 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (AllNewRows)
                            {
                                o = new Models.TblOrders2023
                                {
                                    SiteCode = sc,
                                    CltId = cltid,
                                    OrderNum = onum,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                            else
                            {
                                o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                if (o == null)
                                {
                                    o = new Models.TblOrders2023
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                            }
                            if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                                o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                o.MedType = r["medtype"].ToString();
                                o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                o.Doctor = r["doctor"].ToString();
                                o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                o.Dose = decimal.Parse(r["dose"].ToString());
                                o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                o.Changeby = int.Parse(r["changeby"].ToString());
                                o.Intervals = Int16.Parse(r["intervals"].ToString());
                                o.Sunday = bool.Parse(r["sunday"].ToString());
                                o.Monday = bool.Parse(r["monday"].ToString());
                                o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                o.Thursday = bool.Parse(r["thursday"].ToString());
                                o.Friday = bool.Parse(r["friday"].ToString());
                                o.Saturday = bool.Parse(r["saturday"].ToString());
                                o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                o.Monday2 = bool.Parse(r["monday2"].ToString());
                                o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                o.Friday2 = bool.Parse(r["friday2"].ToString());
                                o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                o.Notes = r["notes"].ToString();
                                if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                o.Active = bool.Parse(r["active"].ToString());
                                o.Type = r["type"].ToString();
                                o.Stype = r["stype"].ToString();
                                o.Weeknum = int.Parse(r["weeknum"].ToString());
                                o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                o.Blind = bool.Parse(r["blind"].ToString());
                                o.OUser = r["o_user"].ToString();
                                o.CltM4id = r["cltM4id"].ToString();
                                if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                o.Pckcode = r["pckcode"].ToString();
                                o.RxhistId = r["rxhistid"].ToString();
                                if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                o.ActByUser = r["actbyuser"].ToString();
                                if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                o.Color = r["color"].ToString();
                                if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                o.DeActbyUser = r["deactbyuser"].ToString();
                                o.OrderTypev5 = r["ordertypev5"].ToString();
                                if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                if (r["dtmid"].ToString().Length > 0)
                                {
                                    if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                    {
                                        o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                    }
                                }
                                if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                o.OverApprove = r["overapprove"].ToString();
                                o.OverapproveDt = r["overapprovedt"].ToString();
                                if (tbl.Columns.Contains("sogentereddt"))
                                {
                                    if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                }
                                if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                if (NewRow || AllNewRows)
                                {
                                    NewRow = false;
                                    ords.Add(o);
                                    //Console.WriteLine("Added " + o.OrderNum.ToString());
                                }
                                //db.SaveChanges();
                                //Console.WriteLine("Saved " + o.OrderNum.ToString());
                            }
                            else
                            {
                                o.RowState = true;
                                o.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2023.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2024(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2024> ords = new List<Models.TblOrders2024>();
                        Models.TblOrders2024 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2024> orders = db.TblOrders2024.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2024 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (cltid > 0)
                            {
                                if (AllNewRows)
                                {
                                    o = new Models.TblOrders2024
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                                else
                                {
                                    o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                    if (o == null)
                                    {
                                        o = new Models.TblOrders2024
                                        {
                                            SiteCode = sc,
                                            CltId = cltid,
                                            OrderNum = onum,
                                            RowChkSum = rcs
                                        };
                                        NewRow = true;
                                    }
                                }
                                if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                                {
                                    if (NewRow)
                                    { Console.WriteLine(o.CltId.ToString() + "  " + o.OrderNum.ToString()); }
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                    o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    o.MedType = r["medtype"].ToString();
                                    o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                    o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                    o.Doctor = r["doctor"].ToString();
                                    o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                    o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                    o.Dose = decimal.Parse(r["dose"].ToString());
                                    o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                    o.Changeby = int.Parse(r["changeby"].ToString());
                                    o.Intervals = Int16.Parse(r["intervals"].ToString());
                                    o.Sunday = bool.Parse(r["sunday"].ToString());
                                    o.Monday = bool.Parse(r["monday"].ToString());
                                    o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                    o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                    o.Thursday = bool.Parse(r["thursday"].ToString());
                                    o.Friday = bool.Parse(r["friday"].ToString());
                                    o.Saturday = bool.Parse(r["saturday"].ToString());
                                    o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                    o.Monday2 = bool.Parse(r["monday2"].ToString());
                                    o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                    o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                    o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                    o.Friday2 = bool.Parse(r["friday2"].ToString());
                                    o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                    o.Notes = r["notes"].ToString();
                                    if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                    o.Active = bool.Parse(r["active"].ToString());
                                    o.Type = r["type"].ToString();
                                    o.Stype = r["stype"].ToString();
                                    o.Weeknum = int.Parse(r["weeknum"].ToString());
                                    o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                    o.Blind = bool.Parse(r["blind"].ToString());
                                    o.OUser = r["o_user"].ToString();
                                    o.CltM4id = r["cltM4id"].ToString();
                                    if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                    o.Pckcode = r["pckcode"].ToString();
                                    o.RxhistId = r["rxhistid"].ToString();
                                    if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                    if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                    o.ActByUser = r["actbyuser"].ToString();
                                    if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                    if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                    if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                    if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                    if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                    if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                    if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                    o.Color = r["color"].ToString();
                                    if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                    o.DeActbyUser = r["deactbyuser"].ToString();
                                    o.OrderTypev5 = r["ordertypev5"].ToString();
                                    if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                    if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                    if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                    if (r["dtmid"].ToString().Length > 0)
                                    {
                                        if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                        {
                                            o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                        }
                                    }
                                    if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                    o.OverApprove = r["overapprove"].ToString();
                                    o.OverapproveDt = r["overapprovedt"].ToString();
                                    if (tbl.Columns.Contains("sogentereddt"))
                                    {
                                        if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                    }
                                    if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                    if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                    if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                    if (NewRow || AllNewRows)
                                    {
                                        NewRow = false;
                                        ords.Add(o);
                                        //Console.WriteLine("Added " + o.OrderNum.ToString());
                                    }
                                    //Console.WriteLine("Saved " + o.OrderNum.ToString()); 
                                    //db.SaveChanges();                                    
                                }
                                else
                                {
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                }
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2024.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2025(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2025> ords = new List<Models.TblOrders2025>();
                        Models.TblOrders2025 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2025> orders = db.TblOrders2025.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2025 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (cltid != 0)
                            {
                                if (AllNewRows)
                                {
                                    o = new Models.TblOrders2025
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                                else
                                {
                                    o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                    if (o == null)
                                    {
                                        o = new Models.TblOrders2025
                                        {
                                            SiteCode = sc,
                                            CltId = cltid,
                                            OrderNum = onum,
                                            RowChkSum = rcs
                                        };
                                        NewRow = true;
                                    }
                                }
                                if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                                {
                                    if (NewRow)
                                    { Console.WriteLine(o.CltId.ToString() + "  " + o.OrderNum.ToString()); }
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                    o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    o.MedType = r["medtype"].ToString();
                                    o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                    o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                    o.Doctor = r["doctor"].ToString();
                                    o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                    o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                    o.Dose = decimal.Parse(r["dose"].ToString());
                                    o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                    o.Changeby = int.Parse(r["changeby"].ToString());
                                    o.Intervals = Int16.Parse(r["intervals"].ToString());
                                    o.Sunday = bool.Parse(r["sunday"].ToString());
                                    o.Monday = bool.Parse(r["monday"].ToString());
                                    o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                    o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                    o.Thursday = bool.Parse(r["thursday"].ToString());
                                    o.Friday = bool.Parse(r["friday"].ToString());
                                    o.Saturday = bool.Parse(r["saturday"].ToString());
                                    o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                    o.Monday2 = bool.Parse(r["monday2"].ToString());
                                    o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                    o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                    o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                    o.Friday2 = bool.Parse(r["friday2"].ToString());
                                    o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                    o.Notes = r["notes"].ToString();
                                    if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                    o.Active = bool.Parse(r["active"].ToString());
                                    o.Type = r["type"].ToString();
                                    o.Stype = r["stype"].ToString();
                                    o.Weeknum = int.Parse(r["weeknum"].ToString());
                                    o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                    o.Blind = bool.Parse(r["blind"].ToString());
                                    o.OUser = r["o_user"].ToString();
                                    o.CltM4id = r["cltM4id"].ToString();
                                    if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                    o.Pckcode = r["pckcode"].ToString();
                                    o.RxhistId = r["rxhistid"].ToString();
                                    if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                    if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                    o.ActByUser = r["actbyuser"].ToString();
                                    if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                    if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                    if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                    if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                    if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                    if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                    if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                    o.Color = r["color"].ToString();
                                    if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                    o.DeActbyUser = r["deactbyuser"].ToString();
                                    o.OrderTypev5 = r["ordertypev5"].ToString();
                                    if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                    if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                    if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                    if (r["dtmid"].ToString().Length > 0)
                                    {
                                        if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                        {
                                            o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                        }
                                    }
                                    if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                    o.OverApprove = r["overapprove"].ToString();
                                    o.OverapproveDt = r["overapprovedt"].ToString();
                                    if (tbl.Columns.Contains("sogentereddt"))
                                    {
                                        if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                    }
                                    if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                    if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                    if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                    if (NewRow || AllNewRows)
                                    {
                                        NewRow = false;
                                        ords.Add(o);
                                        //Console.WriteLine("Added " + o.OrderNum.ToString());
                                    }
                                    //db.SaveChanges();
                                    //Console.WriteLine("Saved " + o.OrderNum.ToString());
                                }
                                else
                                {
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                }
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2025.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2026(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2026> ords = new List<Models.TblOrders2026>();
                        Models.TblOrders2026 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2026> orders = db.TblOrders2026.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2026 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (cltid != 0)
                            {
                                if (AllNewRows)
                                {
                                    o = new Models.TblOrders2026
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                                else
                                {
                                    o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                    if (o == null)
                                    {
                                        o = new Models.TblOrders2026
                                        {
                                            SiteCode = sc,
                                            CltId = cltid,
                                            OrderNum = onum,
                                            RowChkSum = rcs
                                        };
                                        NewRow = true;
                                    }
                                }
                                if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                                {
                                    if (NewRow)
                                    { Console.WriteLine(o.CltId.ToString() + "  " + o.OrderNum.ToString()); }
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                    o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    o.MedType = r["medtype"].ToString();
                                    o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                    o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                    o.Doctor = r["doctor"].ToString();
                                    o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                    o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                    o.Dose = decimal.Parse(r["dose"].ToString());
                                    o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                    o.Changeby = int.Parse(r["changeby"].ToString());
                                    o.Intervals = Int16.Parse(r["intervals"].ToString());
                                    o.Sunday = bool.Parse(r["sunday"].ToString());
                                    o.Monday = bool.Parse(r["monday"].ToString());
                                    o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                    o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                    o.Thursday = bool.Parse(r["thursday"].ToString());
                                    o.Friday = bool.Parse(r["friday"].ToString());
                                    o.Saturday = bool.Parse(r["saturday"].ToString());
                                    o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                    o.Monday2 = bool.Parse(r["monday2"].ToString());
                                    o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                    o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                    o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                    o.Friday2 = bool.Parse(r["friday2"].ToString());
                                    o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                    o.Notes = r["notes"].ToString();
                                    if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                    o.Active = bool.Parse(r["active"].ToString());
                                    o.Type = r["type"].ToString();
                                    o.Stype = r["stype"].ToString();
                                    o.Weeknum = int.Parse(r["weeknum"].ToString());
                                    o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                    o.Blind = bool.Parse(r["blind"].ToString());
                                    o.OUser = r["o_user"].ToString();
                                    o.CltM4id = r["cltM4id"].ToString();
                                    if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                    o.Pckcode = r["pckcode"].ToString();
                                    o.RxhistId = r["rxhistid"].ToString();
                                    if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                    if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                    o.ActByUser = r["actbyuser"].ToString();
                                    if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                    if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                    if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                    if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                    if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                    if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                    if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                    o.Color = r["color"].ToString();
                                    if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                    o.DeActbyUser = r["deactbyuser"].ToString();
                                    o.OrderTypev5 = r["ordertypev5"].ToString();
                                    if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                    if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                    if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                    if (r["dtmid"].ToString().Length > 0)
                                    {
                                        if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                        {
                                            o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                        }
                                    }
                                    if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                    o.OverApprove = r["overapprove"].ToString();
                                    o.OverapproveDt = r["overapprovedt"].ToString();
                                    if (tbl.Columns.Contains("sogentereddt"))
                                    {
                                        if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                    }
                                    if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                    if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                    if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                    if (NewRow || AllNewRows)
                                    {
                                        NewRow = false;
                                        ords.Add(o);
                                        //Console.WriteLine("Added " + o.OrderNum.ToString());
                                    }
                                    //db.SaveChanges();
                                    //Console.WriteLine("Saved " + o.OrderNum.ToString());
                                }
                                else
                                {
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                }
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2026.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2027(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2027> ords = new List<Models.TblOrders2027>();
                        Models.TblOrders2027 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2027> orders = db.TblOrders2027.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2027 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (cltid != 0)
                            {
                                if (AllNewRows)
                                {
                                    o = new Models.TblOrders2027
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                                else
                                {
                                    o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                    if (o == null)
                                    {
                                        o = new Models.TblOrders2027
                                        {
                                            SiteCode = sc,
                                            CltId = cltid,
                                            OrderNum = onum,
                                            RowChkSum = rcs
                                        };
                                        NewRow = true;
                                    }
                                }
                                if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                                {
                                    if (NewRow)
                                    { Console.WriteLine(o.CltId.ToString() + "  " + o.OrderNum.ToString()); }
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                    o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    o.MedType = r["medtype"].ToString();
                                    o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                    o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                    o.Doctor = r["doctor"].ToString();
                                    o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                    o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                    o.Dose = decimal.Parse(r["dose"].ToString());
                                    o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                    o.Changeby = int.Parse(r["changeby"].ToString());
                                    o.Intervals = Int16.Parse(r["intervals"].ToString());
                                    o.Sunday = bool.Parse(r["sunday"].ToString());
                                    o.Monday = bool.Parse(r["monday"].ToString());
                                    o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                    o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                    o.Thursday = bool.Parse(r["thursday"].ToString());
                                    o.Friday = bool.Parse(r["friday"].ToString());
                                    o.Saturday = bool.Parse(r["saturday"].ToString());
                                    o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                    o.Monday2 = bool.Parse(r["monday2"].ToString());
                                    o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                    o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                    o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                    o.Friday2 = bool.Parse(r["friday2"].ToString());
                                    o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                    o.Notes = r["notes"].ToString();
                                    if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                    o.Active = bool.Parse(r["active"].ToString());
                                    o.Type = r["type"].ToString();
                                    o.Stype = r["stype"].ToString();
                                    o.Weeknum = int.Parse(r["weeknum"].ToString());
                                    o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                    o.Blind = bool.Parse(r["blind"].ToString());
                                    o.OUser = r["o_user"].ToString();
                                    o.CltM4id = r["cltM4id"].ToString();
                                    if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                    o.Pckcode = r["pckcode"].ToString();
                                    o.RxhistId = r["rxhistid"].ToString();
                                    if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                    if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                    o.ActByUser = r["actbyuser"].ToString();
                                    if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                    if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                    if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                    if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                    if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                    if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                    if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                    o.Color = r["color"].ToString();
                                    if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                    o.DeActbyUser = r["deactbyuser"].ToString();
                                    o.OrderTypev5 = r["ordertypev5"].ToString();
                                    if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                    if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                    if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                    if (r["dtmid"].ToString().Length > 0)
                                    {
                                        if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                        {
                                            o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                        }
                                    }
                                    if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                    o.OverApprove = r["overapprove"].ToString();
                                    o.OverapproveDt = r["overapprovedt"].ToString();
                                    if (tbl.Columns.Contains("sogentereddt"))
                                    {
                                        if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                    }
                                    if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                    if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                    if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                    if (NewRow || AllNewRows)
                                    {
                                        NewRow = false;
                                        ords.Add(o);
                                        //Console.WriteLine("Added " + o.OrderNum.ToString());
                                    }
                                    //db.SaveChanges();
                                    //Console.WriteLine("Saved " + o.OrderNum.ToString());
                                }
                                else
                                {
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                }
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2027.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
        public bool SaveOrders2028(DataTable tbl, string sc, DateTime wrkdt, BHG_DR_LIB.Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        List<Models.TblOrders2028> ords = new List<Models.TblOrders2028>();
                        Models.TblOrders2028 o;
                        int onum = 0;
                        if (tbl.Rows.Count > 0)
                        {
                            onum = int.Parse(tbl.Rows[0]["OrderNum"].ToString());
                        }
                        List<Models.TblOrders2028> orders = db.TblOrders2028.Where(x => x.SiteCode == sc
                                           //&& x.Active == true
                                           ).OrderBy(o => o.OrderNum).ToList();
                        if (orders.Count == 0) { AllNewRows = true; }
                        else
                        {
                            foreach (TblOrders2028 ord in orders)
                            {
                                //if (ord.DateAdded.Value.Year == wrkdt.Year)
                                {
                                    ord.RowState = false;
                                    ord.Active = false;
                                }
                            }
                        }
                        foreach (DataRow r in tbl.Rows)
                        {
                            onum = int.Parse(r["OrderNum"].ToString());
                            int cltid = int.Parse(r["cltid"].ToString());
                            int rcs = int.Parse(r["rowchksum"].ToString());
                            if (cltid != 0)
                            {
                                if (AllNewRows)
                                {
                                    o = new Models.TblOrders2028
                                    {
                                        SiteCode = sc,
                                        CltId = cltid,
                                        OrderNum = onum,
                                        RowChkSum = rcs
                                    };
                                    NewRow = true;
                                }
                                else
                                {
                                    o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault();
                                    if (o == null)
                                    {
                                        o = new Models.TblOrders2028
                                        {
                                            SiteCode = sc,
                                            CltId = cltid,
                                            OrderNum = onum,
                                            RowChkSum = rcs
                                        };
                                        NewRow = true;
                                    }
                                }
                                if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
                                {
                                    if (NewRow)
                                    { Console.WriteLine(o.CltId.ToString() + "  " + o.OrderNum.ToString()); }
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                    o.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    o.MedType = r["medtype"].ToString();
                                    o.DateAdded = DateTime.Parse(r["dateadded"].ToString());
                                    o.Orderdate = DateTime.Parse(r["orderdate"].ToString());
                                    o.Doctor = r["doctor"].ToString();
                                    o.EffectiveDate = DateTime.Parse(r["effectivedate"].ToString());
                                    o.ExpirationDate = DateTime.Parse(r["expirationdate"].ToString());
                                    o.Dose = decimal.Parse(r["dose"].ToString());
                                    o.Dose2 = decimal.Parse(r["dose2"].ToString());
                                    o.Changeby = int.Parse(r["changeby"].ToString());
                                    o.Intervals = Int16.Parse(r["intervals"].ToString());
                                    o.Sunday = bool.Parse(r["sunday"].ToString());
                                    o.Monday = bool.Parse(r["monday"].ToString());
                                    o.Tuesday = bool.Parse(r["tuesday"].ToString());
                                    o.Wednesday = bool.Parse(r["wednesday"].ToString());
                                    o.Thursday = bool.Parse(r["thursday"].ToString());
                                    o.Friday = bool.Parse(r["friday"].ToString());
                                    o.Saturday = bool.Parse(r["saturday"].ToString());
                                    o.Sunday2 = bool.Parse(r["sunday2"].ToString());
                                    o.Monday2 = bool.Parse(r["monday2"].ToString());
                                    o.Tuesday2 = bool.Parse(r["tuesday2"].ToString());
                                    o.Wednesday2 = bool.Parse(r["wednesday2"].ToString());
                                    o.Thursday2 = bool.Parse(r["thursday2"].ToString());
                                    o.Friday2 = bool.Parse(r["friday2"].ToString());
                                    o.Saturday2 = bool.Parse(r["saturday2"].ToString());
                                    o.Notes = r["notes"].ToString();
                                    if (o.Notes.Length > 1000) { o.Notes = o.Notes.Substring(0, 999).Trim(); }
                                    o.Active = bool.Parse(r["active"].ToString());
                                    o.Type = r["type"].ToString();
                                    o.Stype = r["stype"].ToString();
                                    o.Weeknum = int.Parse(r["weeknum"].ToString());
                                    o.SplitFirst = bool.Parse(r["splitfirst"].ToString());
                                    o.Blind = bool.Parse(r["blind"].ToString());
                                    o.OUser = r["o_user"].ToString();
                                    o.CltM4id = r["cltM4id"].ToString();
                                    if (r["newdose"].ToString().Length > 0) { o.Newdose = int.Parse(r["newdose"].ToString()); }
                                    o.Pckcode = r["pckcode"].ToString();
                                    o.RxhistId = r["rxhistid"].ToString();
                                    if (r["ex"].ToString().Length > 0) { o.Ex = bool.Parse(r["ex"].ToString()); }
                                    if (r["actbydate"].ToString().Length > 0) { o.ActbyDate = DateTime.Parse(r["actbydate"].ToString()); }
                                    o.ActByUser = r["actbyuser"].ToString();
                                    if (r["white"].ToString().Length > 0) { o.White = bool.Parse(r["white"].ToString()); }
                                    if (r["repoldorder"].ToString().Length > 0) { o.RepOldOrder = decimal.Parse(r["repoldorder"].ToString()); }
                                    if (tbl.Columns.Contains("sigdr")) { o.SigDr = r["sigdr"].ToString(); }
                                    if (r["dtsig"].ToString().Length > 0) { o.DtSig = DateTime.Parse(r["dtsig"].ToString()); }
                                    if (r["aws"].ToString().Length > 0) { o.Aws = bool.Parse(r["aws"].ToString()); }
                                    if (r["blsched"].ToString().Length > 0) { o.BlSched = bool.Parse(r["blsched"].ToString()); }
                                    if (r["blverbal"].ToString().Length > 0) { o.BlVerbal = bool.Parse(r["blverbal"].ToString()); }
                                    o.Color = r["color"].ToString();
                                    if (r["deactbydate"].ToString().Length > 0) { o.DeActbyDate = DateTime.Parse(r["deactbydate"].ToString()); }
                                    o.DeActbyUser = r["deactbyuser"].ToString();
                                    o.OrderTypev5 = r["ordertypev5"].ToString();
                                    if (tbl.Columns.Contains("sigentered")) { o.Sigentered = r["sigentered"].ToString(); }
                                    if (tbl.Columns.Contains("signoted")) { o.Signoted = r["signoted"].ToString(); }
                                    if (r["signoteddt"].ToString().Length > 0) { o.SigNoteddt = DateTime.Parse(r["signoteddt"].ToString()); }
                                    if (r["dtmid"].ToString().Length > 0)
                                    {
                                        if (r["dtmid"].ToString() != "1900-01-01 00:00:00.000")
                                        {
                                            o.Dtmid = DateTime.Parse(r["dtmid"].ToString());
                                        }
                                    }
                                    if (tbl.Columns.Contains("sigmid")) { o.SigMid = r["sigmid"].ToString(); }
                                    o.OverApprove = r["overapprove"].ToString();
                                    o.OverapproveDt = r["overapprovedt"].ToString();
                                    if (tbl.Columns.Contains("sogentereddt"))
                                    {
                                        if (r["sigentereddt"].ToString().Length > 0) { o.Sigentereddt = DateTime.Parse(r["sigentereddt"].ToString()); }
                                    }
                                    if (tbl.Columns.Contains("sigdrimg")) { o.SigDrImg = Encoding.ASCII.GetBytes(r["sigdrimg"].ToString()); }
                                    if (tbl.Columns.Contains("sigmidimg")) { o.SigMidImg = Encoding.ASCII.GetBytes(r["SigMidImg"].ToString()); }
                                    if (tbl.Columns.Contains("signotedimg")) { o.SigNotedImg = Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString()); }

                                    if (NewRow || AllNewRows)
                                    {
                                        NewRow = false;
                                        ords.Add(o);
                                        //Console.WriteLine("Added " + o.OrderNum.ToString());
                                    }
                                    //db.SaveChanges();
                                    //Console.WriteLine("Saved " + o.OrderNum.ToString());
                                }
                                else
                                {
                                    o.RowState = true;
                                    o.LastModAt = DateTime.Now;
                                }
                            }
                        }
                        db.SaveChanges();
                        if (ords.Count > 0)
                        {
                            db.TblOrders2028.AddRange(ords);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        res = false;
                        Console.WriteLine(e.Message);
                        if (e.InnerException.Message != null)
                        {
                            Console.WriteLine(e.InnerException.Message);
                        }
                    }
                }
            }
            return res;
        }
    }
}
