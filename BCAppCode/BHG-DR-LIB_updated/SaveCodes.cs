using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public bool SaveCodes(DataTable tbl, string sc, bool PYear, Models.BHG_DRContext db)
        {
            bool res = true;
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    var codes = db.TblCodes.Where(x => x.SiteCode == sc).ToList();
                    if (codes.Count == 0) { AllNewRows = true; }
                    Models.TblCodes c;
                    foreach (DataRow r in tbl.Rows)
                    {
                        int cid = int.Parse(r["cdeID"].ToString());
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            c = new Models.TblCodes
                            {
                                SiteCode = sc,
                                CdeId = cid,
                                RowChkSum = rcs
                            };
                            NewRow = true;
                        }
                        else
                        {
                            c = codes.Where(x => x.CdeId == cid).FirstOrDefault();
                            if (c == null)
                            {
                                c = new Models.TblCodes
                                {
                                    SiteCode = sc,
                                    CdeId = cid,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                            }
                        }
                        if ((NewRow) || (rcs != c.RowChkSum))
                        {
                            c.LastModAt = RunDT;
                            foreach(DataColumn m in tbl.Columns)
                            {
                                switch(m.ColumnName.ToLower())
                                {
                                    case "cdegroup":
                                        c.CdeGroup = r["cdeGroup"].ToString();
                                        break;
                                    case "cdedesc":
                                        c.CdeDesc = r["cdeDesc"].ToString();
                                        break;
                                    case "cdebillable":
                                        if (r["cdeBillable"].ToString().Length > 0)
                                        { c.CdeBillable = bool.Parse(r["cdeBillable"].ToString()); }
                                        break;
                                    case "cdeua":
                                        if (r["cdeUA"].ToString().Length > 0) { c.CdeUa = bool.Parse(r["cdeUA"].ToString()); }
                                        break;
                                    case "cdeintamt":
                                        if (r["cdeIntAmt"].ToString().Length > 0) { c.CdeIntAmt = int.Parse(r["cdeIntAmt"].ToString()); }
                                        break;
                                    case "cdeliquid":
                                        if (r["cdeLiquid"].ToString().Length > 0) { c.CdeLiquid = bool.Parse(r["cdeliquid"].ToString()); }
                                        break;
                                    case "cdestaffcode":
                                        if (r["cdeSTAFFCODE"].ToString().Length > 0) { c.CdeStaffcode = bool.Parse(r["cdeSTAFFCODE"].ToString()); }
                                        break;
                                    case "cdefund":
                                        c.CdeFund = r["cdefund"].ToString();
                                        break;
                                    case "cdemodality":
                                        c.CdeModality = r["cdeModality"].ToString();
                                        break;
                                    case "cdedrugfree":
                                        if (r["cdeDRUGFREE"].ToString().Length > 0) { c.CdeDrugfree = bool.Parse(r["cdeDRUGFREE"].ToString()); }
                                        break;
                                    case "cdeprovider":
                                        c.CdeProvider = r["cdeProvider"].ToString();
                                        break;
                                    case "cdesitenum":
                                        c.CdeSiteNum = r["cdeSiteNum"].ToString();
                                        break;
                                    case "rowguid":
                                        if (r["rowguid"].ToString().Length > 0) { c.Rowguid = Guid.Parse(r["rowguid"].ToString()); }
                                        break;
                                    case "cdebillableresidential":
                                        if (r["cdeBillableResidential"].ToString().Length > 0) { c.CdeBillableResidential = bool.Parse(r["cdeBillableResidential"].ToString()); }
                                        break;
                                    case "cdeservicesetting":
                                        c.CdeServiceSetting = r["cdeServiceSetting"].ToString();
                                        break;
                                    case "cdedischargetype":
                                        c.CdeDischargeType = r["cdeDischargeType"].ToString();
                                        break;
                                    case "cdesigrequired":
                                        if (r["cdeSigRequired"].ToString().Length > 0) { c.CdeSigRequired = bool.Parse(r["cdeSigRequired"].ToString()); }
                                        break;
                                    case "cderesidential":
                                        if (r["cdeResidential"].ToString().Length > 0) { c.CdeResidential = bool.Parse(r["cdeResidential"].ToString()); }
                                        break;
                                    case "cdeallowoverlap":
                                        if (r["cdeAllowOverlap"].ToString().Length > 0) { c.CdeAllowOverlap = bool.Parse(r["cdeAllowOverlap"].ToString()); }
                                        break;
                                    case "duiamt":
                                        if (r["duiAMT"].ToString().Length > 0) { c.DuiAmt = decimal.Parse(r["duiAMT"].ToString()); }
                                        break;
                                    case "duihourrate":
                                        if (r["duiHourRate"].ToString().Length > 0) { c.DuiHourRate = decimal.Parse(r["duiHourRate"].ToString()); }
                                        break;
                                    case "bldefault":
                                        if (r["blDEFAULT"].ToString().Length > 0) { c.BlDefault = bool.Parse(r["blDEFAULT"].ToString()); }
                                        break;
                                    case "weeklyfee":
                                        if (r["WeeklyFee"].ToString().Length > 0) { c.WeeklyFee = int.Parse(r["WeeklyFee"].ToString()); }
                                        break;
                                    case "musthavebilling":
                                        if (r["MustHaveBilling"].ToString().Length > 0) { c.MustHaveBilling = bool.Parse(r["MustHaveBilling"].ToString()); }
                                        break;
                                    case "suboxoneprog":
                                        if (r["Suboxoneprog"].ToString().Length > 0) { c.Suboxoneprog = bool.Parse(r["Suboxoneprog"].ToString()); }
                                        break;
                                    case "cdeinsurance":
                                        if (r["cdeInsurance"].ToString().Length > 0) { c.CdeInsurance = bool.Parse(r["cdeInsurance"].ToString()); }
                                        break;
                                    case "defrate":
                                        if (r["DefRate"].ToString().Length > 0) { c.DefRate = decimal.Parse(r["DefRate"].ToString()); }
                                        break;
                                    case "siteid":
                                        if (c.SiteCode == "PHC") { c.SiteId = 105; }
                                        else
                                        {
                                            if (r["SiteID"].ToString().Length > 0) { c.SiteId = int.Parse(r["SiteID"].ToString()); }
                                        }
                                        break;
                                    case "cdelblcolor":
                                        c.Cdelblcolor = r["cdelblcolor"].ToString();
                                        break;
                                    case "cde3pdonotbill":
                                        if (r["cde3pdonotbill"].ToString().Length > 0) { c.Cde3pdonotbill = bool.Parse(r["cde3pdonotbill"].ToString()); }
                                        break;
                                    case "cde3pPOSoverride":
                                        c.Cde3pPosoverride = r["cde3pPOSoverride"].ToString();
                                        break;
                                    case "isprescreening":
                                        if (r["IsPrescreening"].ToString().Length > 0) { c.IsPrescreening = bool.Parse(r["IsPrescreening"].ToString()); }
                                        break;
                                    case "obat":
                                        if (r["OBAT"].ToString().Length > 0) { c.Obat = bool.Parse(r["OBAT"].ToString()); }
                                        break;
                                    case "reqauth":
                                        if (r["ReqAuth"].ToString().Length > 0) { c.ReqAuth = bool.Parse(r["ReqAuth"].ToString()); }
                                        break;
                                    case "IntakeProg":
                                        if (r["IntakeProg"].ToString().Length > 0) { c.IntakeProg = bool.Parse(r["IntakeProg"].ToString()); }
                                        break;
                                }
                            }
                        }
                        if (NewRow || AllNewRows)
                        {
                            codes.Add(c);
                            NewRow = false;
                        }
                    }
                    db.TblCodes.UpdateRange(codes);
                    //if () //Add new rows
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message.ToString());
                    if (e.InnerException != null)
                    {
                        Console.WriteLine(e.InnerException.Message.ToString());
                    }
                }
            }
            return res;
        }
        public Models.RCodes SaveCodes(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes();
            res.IsResult = true;
            res.RowsProcessed = tbl.Rows.Count;

            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    //var resultInfo = new Z.BulkOperations.ResultInfo();
                    var codes = db.TblCodes.Where(x => x.SiteCode == sc).ToList();
                    if (codes.Count == 0) { AllNewRows = true; }
                    Models.TblCodes c;
                    foreach (DataRow r in tbl.Rows)
                    {
                        int cid = int.Parse(r["cdeID"].ToString());
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            c = new Models.TblCodes
                            {
                                SiteCode = sc,
                                CdeId = cid,
                                RowChkSum = rcs
                            };
                            NewRow = true;
                            res.RowsIns += 1;
                        }
                        else
                        {
                            c = codes.Where(x => x.CdeId == cid).FirstOrDefault();
                            if (c == null)
                            {
                                c = new Models.TblCodes
                                {
                                    SiteCode = sc,
                                    CdeId = cid,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else
                            {
                                res.RowsUpd += 1;
                            }
                        }
                        if ((NewRow) || (rcs != c.RowChkSum))
                        {
                            c.LastModAt = RunDT;
                            foreach (DataColumn m in tbl.Columns)
                            {
                                switch (m.ColumnName.ToLower())
                                {
                                    case "cdegroup":
                                        c.CdeGroup = r["cdeGroup"].ToString();
                                        break;
                                    case "cdedesc":
                                        c.CdeDesc = r["cdeDesc"].ToString();
                                        break;
                                    case "cdebillable":
                                        if (r["cdeBillable"].ToString().Length > 0)
                                        { c.CdeBillable = bool.Parse(r["cdeBillable"].ToString()); }
                                        break;
                                    case "cdeua":
                                        if (r["cdeUA"].ToString().Length > 0) { c.CdeUa = bool.Parse(r["cdeUA"].ToString()); }
                                        break;
                                    case "cdeintamt":
                                        if (r["cdeIntAmt"].ToString().Length > 0) { c.CdeIntAmt = int.Parse(r["cdeIntAmt"].ToString()); }
                                        break;
                                    case "cdeliquid":
                                        if (r["cdeLiquid"].ToString().Length > 0) { c.CdeLiquid = bool.Parse(r["cdeliquid"].ToString()); }
                                        break;
                                    case "cdestaffcode":
                                        if (r["cdeSTAFFCODE"].ToString().Length > 0) { c.CdeStaffcode = bool.Parse(r["cdeSTAFFCODE"].ToString()); }
                                        break;
                                    case "cdefund":
                                        c.CdeFund = r["cdefund"].ToString();
                                        break;
                                    case "cdemodality":
                                        c.CdeModality = r["cdeModality"].ToString();
                                        break;
                                    case "cdedrugfree":
                                        if (r["cdeDRUGFREE"].ToString().Length > 0) { c.CdeDrugfree = bool.Parse(r["cdeDRUGFREE"].ToString()); }
                                        break;
                                    case "cdeprovider":
                                        c.CdeProvider = r["cdeProvider"].ToString();
                                        break;
                                    case "cdesitenum":
                                        c.CdeSiteNum = r["cdeSiteNum"].ToString();
                                        break;
                                    case "rowguid":
                                        if (r["rowguid"].ToString().Length > 0) { c.Rowguid = Guid.Parse(r["rowguid"].ToString()); }
                                        break;
                                    case "cdebillableresidential":
                                        if (r["cdeBillableResidential"].ToString().Length > 0) { c.CdeBillableResidential = bool.Parse(r["cdeBillableResidential"].ToString()); }
                                        break;
                                    case "cdeservicesetting":
                                        c.CdeServiceSetting = r["cdeServiceSetting"].ToString();
                                        break;
                                    case "cdedischargetype":
                                        c.CdeDischargeType = r["cdeDischargeType"].ToString();
                                        break;
                                    case "cdesigrequired":
                                        if (r["cdeSigRequired"].ToString().Length > 0) { c.CdeSigRequired = bool.Parse(r["cdeSigRequired"].ToString()); }
                                        break;
                                    case "cderesidential":
                                        if (r["cdeResidential"].ToString().Length > 0) { c.CdeResidential = bool.Parse(r["cdeResidential"].ToString()); }
                                        break;
                                    case "cdeallowoverlap":
                                        if (r["cdeAllowOverlap"].ToString().Length > 0) { c.CdeAllowOverlap = bool.Parse(r["cdeAllowOverlap"].ToString()); }
                                        break;
                                    case "duiamt":
                                        if (r["duiAMT"].ToString().Length > 0) { c.DuiAmt = decimal.Parse(r["duiAMT"].ToString()); }
                                        break;
                                    case "duihourrate":
                                        if (r["duiHourRate"].ToString().Length > 0) { c.DuiHourRate = decimal.Parse(r["duiHourRate"].ToString()); }
                                        break;
                                    case "bldefault":
                                        if (r["blDEFAULT"].ToString().Length > 0) { c.BlDefault = bool.Parse(r["blDEFAULT"].ToString()); }
                                        break;
                                    case "weeklyfee":
                                        if (r["WeeklyFee"].ToString().Length > 0) { c.WeeklyFee = int.Parse(r["WeeklyFee"].ToString()); }
                                        break;
                                    case "musthavebilling":
                                        if (r["MustHaveBilling"].ToString().Length > 0) { c.MustHaveBilling = bool.Parse(r["MustHaveBilling"].ToString()); }
                                        break;
                                    case "suboxoneprog":
                                        if (r["Suboxoneprog"].ToString().Length > 0) { c.Suboxoneprog = bool.Parse(r["Suboxoneprog"].ToString()); }
                                        break;
                                    case "cdeinsurance":
                                        if (r["cdeInsurance"].ToString().Length > 0) { c.CdeInsurance = bool.Parse(r["cdeInsurance"].ToString()); }
                                        break;
                                    case "defrate":
                                        if (r["DefRate"].ToString().Length > 0) { c.DefRate = decimal.Parse(r["DefRate"].ToString()); }
                                        break;
                                    case "siteid":
                                        if (r["SiteID"].ToString().Length > 0) { c.SiteId = int.Parse(r["SiteID"].ToString()); }
                                        break;
                                    case "cdelblcolor":
                                        c.Cdelblcolor = r["cdelblcolor"].ToString();
                                        break;
                                    case "cde3pdonotbill":
                                        if (r["cde3pdonotbill"].ToString().Length > 0) { c.Cde3pdonotbill = bool.Parse(r["cde3pdonotbill"].ToString()); }
                                        break;
                                    case "cde3pPOSoverride":
                                        c.Cde3pPosoverride = r["cde3pPOSoverride"].ToString();
                                        break;
                                    case "isprescreening":
                                        if (r["IsPrescreening"].ToString().Length > 0) { c.IsPrescreening = bool.Parse(r["IsPrescreening"].ToString()); }
                                        break;
                                    case "obat":
                                        if (r["OBAT"].ToString().Length > 0) { c.Obat = bool.Parse(r["OBAT"].ToString()); }
                                        break;
                                    case "reqauth":
                                        if (r["ReqAuth"].ToString().Length > 0) { c.ReqAuth = bool.Parse(r["ReqAuth"].ToString()); }
                                        break;
                                    case "IntakeProg":
                                        if (r["IntakeProg"].ToString().Length > 0) { c.IntakeProg = bool.Parse(r["IntakeProg"].ToString()); }
                                        break;
                                }
                            }
                        }
                        if (NewRow || AllNewRows)
                        {
                            codes.Add(c);
                            NewRow = false;
                        }
                    }
                    //db.TblCodes.BulkMerge(codes, options => {
                    //options.ColumnInputExpression = x => new { x.SiteCode, x.cdeID, x.RowChkSum, x.LastModAt };
                    //  options.UseRowsAffected = true;
                    //  options.ResultInfo = resultInfo;
                    //});
                    db.SaveChanges();
                    //if (codes)
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message.ToString());
                    if (e.InnerException != null)
                    {
                        Console.WriteLine(e.InnerException.Message.ToString());
                        res.ExceptInnerMsg = e.InnerException.Message;
                    }
                }
            }
            return res;
        }

    }
}
