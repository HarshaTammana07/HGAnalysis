using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveLABResults(DataTable tbl, string sc, DateTime wrkdt, bool reInit, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                bool NewRow = false;
                bool AllNewRows = false;
                DateTime RunDT = DateTime.Now;
                List<Models.TblLabresult> vsNew = new List<Models.TblLabresult>();
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    var v = new List<Models.TblLabresult>();
                    if (reInit)
                    {
                        v = db.TblLabresult.Where(x => x.SiteCode == sc).ToList();
                    }
                    else
                    {
                        v = db.TblLabresult.Where(x => x.SiteCode == sc
                                && (x.LabrResultDt.Value.Date >= wrkdt.Date || x.LabrDropDt >= wrkdt.Date || x.LabrCreatedDt >= wrkdt.Date)
                            ).ToList();
                    }
                    if (v.Count == 0) { AllNewRows = true; }
                    Models.TblLabresult vr;
                    foreach (DataRow r in tbl.Rows)
                    {
                        int lngCltId = 0;
                        int rid = int.Parse(r["LABrID"].ToString());
                        if (r["lablngCltId"].ToString().Length > 0)
                        {
                            lngCltId = int.Parse(r["lablngCltId"].ToString());
                        }
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        DateTime _LabrResultDt = DateTime.Parse(r["LABrresultdt"].ToString());
                        if (AllNewRows)
                        {
                            vr = new Models.TblLabresult
                            {
                                SiteCode = sc,
                                LabrId = rid,
                                LablngCltId = lngCltId,
                                RowChkSum = rcs,
                                LabrResultDt = _LabrResultDt,
                                LastModAt = RunDT
                            };
                            NewRow = true;
                            res.RowsIns += 1;
                        }
                        else
                        {
                            vr = v.Where(x => x.LabrId == rid && x.LablngCltId == lngCltId).FirstOrDefault();
                            if (vr == null)
                            {
                                vr = new Models.TblLabresult
                                {
                                    SiteCode = sc,
                                    LabrId = rid,
                                    LablngCltId = lngCltId,
                                    RowChkSum = rcs,
                                    LabrResultDt = _LabrResultDt,
                                    LastModAt = RunDT
                                };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else { res.RowsUpd += 1; vr.LastModAt = RunDT; }
                        }
                        if (NewRow || rcs != vr.RowChkSum)
                        {
                            vr.LastModAt = RunDT;
                            vr.RowChkSum = rcs;
                            foreach (DataColumn c in tbl.Columns)
                            {
                                switch (c.ColumnName.ToLower())
                                {
                                    case "labrresultdt":
                                        if (r[c.ColumnName].ToString().Length > 6)
                                        {
                                            vr.LabrResultDt = DateTime.Parse(r["labrresultdt"].ToString());
                                        }
                                        break;
                                    case "labrlngcltid":
                                    case "lablngcltid":
                                        //if (r[c.ColumnName].ToString().Length > 0)
                                        //{ vr.LablngCltId = int.Parse(r[c.ColumnName].ToString()); }
                                        //else { vr.LablngCltId = 0; }
                                        break;
                                    case "labrschedid":
                                        if (r[c.ColumnName].ToString().Length > 0)
                                        { vr.LabrSchedId = int.Parse(r[c.ColumnName].ToString()); }
                                        break;
                                    case "labrdropdt":
                                        if (r[c.ColumnName].ToString().Length > 6)
                                        { vr.LabrDropDt = DateTime.Parse(r[c.ColumnName].ToString()); }
                                        break;
                                    case "labresultdt":
                                        if (r[c.ColumnName].ToString().Length > 0)
                                        { vr.LabrResultDt = DateTime.Parse(r[c.ColumnName].ToString()); }
                                        break;
                                    case "labrcreatedby":
                                        vr.LabrCreatedBy = r[c.ColumnName].ToString();
                                        break;
                                    case "labrcreateddt":
                                        if (r[c.ColumnName].ToString().Length > 6)
                                        { vr.LabrCreatedDt = DateTime.Parse(r[c.ColumnName].ToString()); }
                                        break;
                                    case "labnote":
                                        vr.Labnote = r[c.ColumnName].ToString();
                                        break;
                                    case "repoldlab":
                                        if (r[c.ColumnName].ToString().Length > 0)
                                        { vr.RepOldLab = int.Parse(r[c.ColumnName].ToString()); }
                                        break;
                                    case "oldclient":
                                        vr.OldClient = r["oldClient"].ToString();
                                        break;
                                    case "labrlotno":
                                        if (r[c.ColumnName].ToString().Length > 0)
                                        { vr.LabrLotno = r[c.ColumnName].ToString(); }
                                        break;
                                    case "laborderid":
                                        if (r[c.ColumnName].ToString().Length > 0)
                                        { vr.LabOrderId = r[c.ColumnName].ToString(); }
                                        break;
                                    case "labrupdateby":
                                        vr.LabrUpdatedBy = r[c.ColumnName].ToString();
                                        break;
                                    case "labrupdatedt":
                                        if (r[c.ColumnName].ToString().Length > 6)
                                        { vr.LabrUpdatedDt = DateTime.Parse(r[c.ColumnName].ToString()); }
                                        break;
                                    case "supplementaryreport":
                                        vr.SupplementaryReport = r[c.ColumnName].ToString();
                                        break;
                                    case "siteid":
                                        if (vr.SiteCode == "PHC") { vr.SiteId = 105; }
                                        else
                                        {
                                            if (r[c.ColumnName].ToString().Length > 0)
                                            { vr.SiteId = int.Parse(r[c.ColumnName].ToString()); }
                                        }
                                        break;
                                    case "labbase64":
                                        vr.LabBase64 = r[c.ColumnName].ToString();
                                        break;
                                    case "labname":
                                        vr.LabName = r[c.ColumnName].ToString();
                                        break;
                                }
                            }
                            if (NewRow)
                            {
                                vsNew.Add(vr);
                                NewRow = false;
                            }
                        }
                    }
                    db.TblLabresult.UpdateRange(v);
                    db.SaveChanges();
                    //Console.WriteLine("Rows Affected: " + resultInfo.RowsAffected);
                    if (vsNew.Count > 0)
                    {
                        db.TblLabresult.AddRange(vsNew);
                        db.SaveChanges();
                    }

                    //Console.WriteLine("Rows Affected: " + resultInfo.RowsAffected);
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message.ToString());
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    res.ExceptMsg += "    " + e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message.ToString());
                }
            }
            return res;
        }
        public Models.RCodes SaveUAResults(DataTable tbl, string sc, DateTime wrkdt, bool reInit, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            { 
                IsResult = true, 
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                bool NewRow = false;
                bool AllNewRows = false;
                DateTime RunDT = DateTime.Now;
                List<Models.TblUaresults> vsNew = new List<Models.TblUaresults>();
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    var v = new List<Models.TblUaresults>();
                    if (reInit)
                    {
                        v = db.TblUaresults.Where(x => x.SiteCode == sc).ToList();
                    }
                    else
                    {
                        v = db.TblUaresults.Where(x => x.SiteCode == sc
                                && (x.UarResultDt.Date >= wrkdt.Date.AddMonths(-3) || x.UarDropDt >= wrkdt.Date.AddMonths(-3) 
                                    || x.UarCreatedDt >= wrkdt.Date.AddMonths(-3) || x.LastModAt >= wrkdt.AddMonths(-3).Date)
                            ).ToList();
                    }
                    if (v.Count == 0) { AllNewRows = true; }
                    Models.TblUaresults vr;
                    foreach (DataRow r in tbl.Rows)
                    {
                        int uar = int.Parse(r["uarID"].ToString().Trim());
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        int uarLngCltId = int.Parse(r["uarLngCltID"].ToString().Trim());
                        DateTime _uarResultDt = DateTime.Parse(r["uarresultdt"].ToString().Trim());
                        if (AllNewRows)
                        {
                            vr = new Models.TblUaresults
                            {
                                SiteCode = sc,
                                UarId = uar,
                                RowChkSum = rcs,
                                UarResultDt = _uarResultDt
                            };
                            NewRow = true;
                            res.RowsIns += 1;
                        }
                        else
                        {
                            vr = v.FirstOrDefault(x => x.SiteCode == sc && x.UarId == uar && x.UarLngCltId == uarLngCltId);
                            if (vr == null)
                            {
                                vr = new Models.TblUaresults
                                {
                                    SiteCode = sc,
                                    UarId = uar,
                                    RowChkSum = rcs,
                                    UarResultDt = _uarResultDt
                                };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else { res.RowsUpd += 1; vr.LastModAt = RunDT; vr.RowChkSum = 0; NewRow = false; }
                        }
                        if (NewRow || rcs != vr.RowChkSum)
                        {
                            vr.LastModAt = RunDT;
                            vr.RowChkSum = rcs;
                            foreach(DataColumn c in tbl.Columns)
                            {
                                switch(c.ColumnName.ToLower())
                                {
                                    case "uarresultdt":
                                        if (r[c.ColumnName].ToString().Length > 6)
                                        {
                                            vr.UarResultDt = DateTime.Parse(r["uarresultdt"].ToString());
                                        }
                                        break;
                                    case "uarlngcltid":
                                        if (r["uarLngCltID"].ToString().Length > 0)
                                        { vr.UarLngCltId = int.Parse(r["uarLngCltID"].ToString()); }
                                        else { vr.UarLngCltId = 0; }
                                        break;
                                    case "uarschedid":
                                        if (r["uarSchedID"].ToString().Length > 0)
                                        { vr.UarSchedId = int.Parse(r["uarSchedID"].ToString()); }
                                        break;
                                    case "uardropdt":
                                        if (r["uarDropDt"].ToString().Length > 6)
                                        { vr.UarDropDt = DateTime.Parse(r["uarDropDt"].ToString()); }
                                        break;
                                    case "createdby":
                                        vr.UarCreatedBy = r["uarCreatedBy"].ToString();
                                        break;
                                    case "uarcreateddt":
                                        if (r["uarCreatedDt"].ToString().Length > 6)
                                        { vr.UarCreatedDt = DateTime.Parse(r["uarCreatedDt"].ToString()); }
                                        break;
                                    case "cpid":
                                        if (r["cpID"].ToString().Length > 0)
                                        { vr.CpId = int.Parse(r["cpID"].ToString()); }
                                        break;
                                    case "uanote":
                                        vr.UaNote = r["uaNOTE"].ToString();
                                        break;
                                    case "oldnum":
                                        if (r["oldnum"].ToString().Length > 0)
                                        { vr.Oldnum = int.Parse(r["oldnum"].ToString()); }
                                        break;
                                    case "oldclient":
                                        vr.OldClient = r["oldClient"].ToString();
                                        break;
                                    case "repolduar":
                                        if (r["repOldUAr"].ToString().Length > 0)
                                        { vr.RepOldUar = decimal.Parse(r["repOldUAr"].ToString()); }
                                        break;
                                    case "uarlabkey":
                                        vr.UarLabKey = r["uarLabKey"].ToString();
                                        break;
                                    case "uarupdateby":
                                        vr.UarUpdatedBy = r["uarUpdatedBy"].ToString();
                                        break;
                                    case "uarupdatedt":
                                        if (r["uarUpdatedDt"].ToString().Length > 6)
                                        { vr.UarUpdatedDt = DateTime.Parse(r["uarUpdatedDt"].ToString()); }
                                        break;
                                    case "uatype":
                                        vr.UaType = r["uaType"].ToString();
                                        break;
                                    case "siteid":
                                        if (vr.SiteCode == "PHC") { vr.SiteId = 105; }
                                        else
                                        {
                                            if (r["SiteID"].ToString().Length > 0)
                                            { vr.SiteId = int.Parse(r["SiteID"].ToString()); }
                                        }
                                        break;
                                    case "uadbnotes":
                                        vr.UaDbnotes = r["uaDBnotes"].ToString();
                                        break;
                                    case "uanursenote":
                                        vr.UaNurseNote = r["uaNurseNote"].ToString();
                                        break;
                                    case "uasig":
                                        vr.UaSig = r["uaSig"].ToString();
                                        break;
                                    case "uasigdt":
                                        if (r["uaSigDt"].ToString().Length > 6)
                                        { vr.UaSigDt = DateTime.Parse(r["uaSigDt"].ToString()); }
                                        break;
                                    case "uasiguser":
                                        vr.UaSigUser = r["uaSigUser"].ToString();
                                        break;
                                    case "location_":
                                        vr.Location = r["location_"].ToString(); 
                                        break;
                                    case "scheduleddate":
                                        if (r["scheduledDate"].ToString().Length > 6)
                                        { vr.ScheduledDate = DateTime.Parse(r["scheduledDate"].ToString()); }
                                        break;
                                    case "uabase64":
                                        vr.UaBase64 = r["uaBase64"].ToString();
                                        break;
                                    case "uaprogram":
                                        vr.Uaprogram = r["UAProgram"].ToString();
                                        break;
                                    case "labname":
                                        vr.LabName = r[c.ColumnName].ToString();
                                        break;
                                    case "uaeval":
                                        vr.UAEval = r[c.ColumnName].ToString();
                                        break;
                                }
                            }
                            if (NewRow)
                            {
                                vsNew.Add(vr);
                                NewRow = false;
                            }
                        }
                    }
                    db.TblUaresults.UpdateRange(v);
                    db.SaveChanges();
                    //Console.WriteLine("Rows Affected: " + resultInfo.RowsAffected);
                    if (vsNew.Count > 0)
                    {
                        db.TblUaresults.AddRange(vsNew);
                        db.SaveChanges();
                    }
                    
                    //Console.WriteLine("Rows Affected: " + resultInfo.RowsAffected);
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message.ToString());
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    res.ExceptMsg += "    " + e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message.ToString());
                }
            }
            return res;
        }
        public Models.RCodes SaveUAResultDetail(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                bool NewRow = false;
                bool AllNewRows = false;
                DateTime RunDT = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    //var rid = tbl.Rows
                    var rds = db.TblUaresultDetail.Where(x => x.SiteCode == sc)
                        .OrderBy(o => o.UardRecId)
                        .ThenBy(t => t.UardId)
                        //.Take(tbl.Rows.Count)
                        .ToList();
                    if (rds.Count == 0) { AllNewRows = true; }
                    Models.TblUaresultDetail rd;
                    List<Models.TblUaresultDetail> rdsNew = new List<Models.TblUaresultDetail>();
                    foreach (DataRow r in tbl.Rows)
                    {
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        int uard = int.Parse(r["uardid"].ToString());
                        if (AllNewRows)
                        {
                            rd = new Models.TblUaresultDetail
                            {
                                SiteCode = sc,
                                RowChkSum = rcs,
                                UardId = uard
                            };
                            NewRow = true;
                            res.RowsIns += 1;
                        }
                        else
                        {
                            rd = rds.Where(x => x.UardId == uard).FirstOrDefault();
                            if (rd == null)
                            {
                                rd = new Models.TblUaresultDetail
                                {
                                    SiteCode = sc,
                                    RowChkSum = rcs,
                                    UardId = uard
                                };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else { res.RowsUpd += 1; }
                        }
                        if (NewRow || rcs != rd.RowChkSum)
                        {
                            rd.LastModAt = RunDT;
                            rd.UardRecId = int.Parse(r["uardRecId"].ToString());
                            rd.UardResult = r["uardResult"].ToString();
                            if (r["uardRX"].ToString().Length > 0)
                            { rd.UardRx = bool.Parse(r["uardRX"].ToString()); }
                            rd.UaDetail = r["uaDetail"].ToString();
                            if (tbl.Columns.Contains("uardfullnote"))
                            {
                                rd.UardFullNote = r["uardFullNote"].ToString();
                            }
                            if (tbl.Columns.Contains("usardkey"))
                            {
                                rd.UardKey = r["uardkey"].ToString();
                            }
                            if (tbl.Columns.Contains("uardnote"))
                            {
                                rd.UardNote = r["uardNote"].ToString();
                            }
                        }
                        if (NewRow)
                        {
                            rdsNew.Add(rd);
                            NewRow = false;
                        }
                    }
                    db.TblUaresultDetail.UpdateRange(rds);
                    db.SaveChanges();
                    if (rdsNew.Count > 0)
                    {
                        db.TblUaresultDetail.AddRange(rdsNew);
                        db.SaveChanges();
                    }
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message.ToString());
                res.ExceptMsg = e.Message;
                //Console.WriteLine(e.InnerException.ToString());
                if (e.InnerException != null)
                {
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }
        public Models.RCodes SaveUASched(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try 
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                // Get Azure Table data for Site
                List<Models.TblUasched> Scheds = db.TblUasched.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblUasched> NewSchds = new List<Models.TblUasched>();

                //Clean up Deleted Rows

                // Process Table data
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblUasched uas = new Models.TblUasched();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                uas.SiteCode = sc;
                                uas.LastModAt = DateTime.Now;
                                break;
                            case "uasid":
                                uas.UasId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "uaslngcltid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    uas.UasLngCltId = int.Parse(r[c.ColumnName].ToString());
                                }
                                else { uas.UasLngCltId = -1; }
                                if (uas.UasLngCltId < 0) { uas.RowState = false; } else { uas.RowState = true; }
                                break;
                            case "uasdt":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    uas.UasDt = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uasdtadded":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    uas.UasDtAdded = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uasstat":
                                uas.UasStat = r[c.ColumnName].ToString();
                                break;
                            case "uasstatdt":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    uas.UasStatDt = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uasstatuser":
                                uas.UasStatUser = r[c.ColumnName].ToString();
                                break;
                            case "lngcpano":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    uas.LngCpano = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uasnote":
                                uas.UasNote = r[c.ColumnName].ToString();
                                break;
                            case "oldnum":
                                uas.OldNum = r[c.ColumnName].ToString();
                                break;
                            case "oldclient":
                                uas.OldClient = r[c.ColumnName].ToString();
                                break;
                            case "repolduas":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    uas.RepOldUas = decimal.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uascollectedby":
                                uas.UasCollectedBy = r[c.ColumnName].ToString();
                                break;
                            case "uascollecteddate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    uas.UasCollectedDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uasmanifestdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    uas.UasManifestDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "uaspanel":
                                uas.UasPanel = r[c.ColumnName].ToString();
                                break;
                            case "uaspanelother":
                                uas.UasPanelOther = r[c.ColumnName].ToString();
                                break;
                            case "uastype":
                                uas.UasType = r[c.ColumnName].ToString();
                                break;
                            case "uasetg":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString() == "0")
                                    {
                                        uas.UasEtg = false;
                                    }
                                    else
                                    {
                                        uas.UasEtg = true;
                                    }
                                }
                                break;
                            case "uapriority":
                                uas.Uapriority = r[c.ColumnName].ToString();
                                break;
                            case "uasticketprintdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    uas.Uasticketprintdate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "rowchksum":
                                uas.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    //Models.TblUasched dbuas = db.TblUasched.FirstOrDefault(x => x.SiteCode == uas.SiteCode && x.UasId == uas.UasId);
                    Models.TblUasched dbuas = Scheds.FirstOrDefault(x => x.SiteCode == uas.SiteCode 
                        && x.UasId == uas.UasId
                        && x.UasLngCltId == uas.UasLngCltId);
                    if (dbuas == null)
                    {
                        //db.TblUasched.Add(uas);
                        NewSchds.Add(uas);
                        res.RowsIns++;
                    }
                    else
                    {
                        dbuas.LastModAt = uas.LastModAt;
                        dbuas.LngCpano = uas.LngCpano;
                        dbuas.OldClient = uas.OldClient;
                        dbuas.OldNum = uas.OldNum;
                        dbuas.RepOldUas = uas.RepOldUas;
                        dbuas.RowChkSum = uas.RowChkSum;
                        dbuas.RowState = uas.RowState;
                        dbuas.Uapriority = uas.Uapriority;
                        dbuas.UasCollectedBy = uas.UasCollectedBy;
                        dbuas.UasCollectedDate = uas.UasCollectedDate;
                        dbuas.UasDt = uas.UasDt;
                        dbuas.UasDtAdded = uas.UasDtAdded;
                        dbuas.UasEtg = uas.UasEtg;
                        dbuas.UasManifestDate = uas.UasManifestDate;
                        dbuas.UasNote = uas.UasNote;
                        dbuas.UasPanel = uas.UasPanel;
                        dbuas.UasPanelOther = uas.UasPanelOther;
                        dbuas.UasStat = uas.UasStat;
                        dbuas.UasStatDt = uas.UasStatDt;
                        dbuas.UasStatUser = uas.UasStatUser;
                        dbuas.Uasticketprintdate = uas.Uasticketprintdate;
                        dbuas.UasType = uas.UasType;
                        res.RowsUpd++;
                    }
                }
                db.SaveChanges();
                if (NewSchds.Count > 0)
                {
                    db.TblUasched.AddRange(NewSchds);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message.ToString());
                res.ExceptMsg = e.Message;
                //if (e.InnerException != null)
                //{
                //    Console.WriteLine(e.InnerException.ToString());
                //}
            }
            return res;
        }
    }
}
