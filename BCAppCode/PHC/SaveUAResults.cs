using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
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
                            && (x.UarResultDt.Date >= wrkdt.Date || x.UarDropDt >= wrkdt.Date)
                            ).ToList();
                    }
                    if (v.Count == 0) { AllNewRows = true; }
                    Models.TblUaresults vr;
                    foreach (DataRow r in tbl.Rows)
                    {
                        int uar = int.Parse(r["uarID"].ToString());
                        int rcs = int.Parse(r["RowChkSum"].ToString());
                        DateTime _uarResultDt = DateTime.Parse(r["uarresultdt"].ToString());
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
                            vr = v.Where(x => x.UarId == uar).FirstOrDefault();
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
                            else { res.RowsUpd += 1; }
                        }
                        if (NewRow || rcs != vr.RowChkSum)
                        {
                            vr.LastModAt = RunDT;
                            vr.UarResultDt = DateTime.Parse(r["uarresultdt"].ToString());
                            if (r["uarLngCltID"].ToString().Length > 0)
                            { vr.UarLngCltId = int.Parse(r["uarLngCltID"].ToString()); }
                            if (r["uarSchedID"].ToString().Length > 0)
                            { vr.UarSchedId = int.Parse(r["uarSchedID"].ToString()); }
                            if (r["uarDropDt"].ToString().Length > 0)
                            { vr.UarDropDt = DateTime.Parse(r["uarDropDt"].ToString()); }
                            vr.UarCreatedBy = r["uarCreatedBy"].ToString();
                            if (r["uarCreatedDt"].ToString().Length > 0)
                            { vr.UarCreatedDt = DateTime.Parse(r["uarCreatedDt"].ToString()); }
                            if (r["cpID"].ToString().Length > 0)
                            { vr.CpId = int.Parse(r["cpID"].ToString()); }
                            if (tbl.Columns.Contains("uaNOTE")) { vr.UaNote = r["uaNOTE"].ToString(); }
                            if (r["oldnum"].ToString().Length > 0)
                            { vr.Oldnum = int.Parse(r["oldnum"].ToString()); }
                            vr.OldClient = r["oldClient"].ToString();
                            //if (r["upsize_ts"].ToString().Length > 0)
                            //{ vr.upsize_ts = (byte)(r["upsize_ts"]); }
                            if (r["repOldUAr"].ToString().Length > 0)
                            { vr.RepOldUar = decimal.Parse(r["repOldUAr"].ToString()); }
                            vr.UarLabKey = r["uarLabKey"].ToString();
                            vr.UarUpdatedBy = r["uarUpdatedBy"].ToString();
                            if (r["uarUpdatedDt"].ToString().Length > 0)
                            { vr.UarUpdatedDt = DateTime.Parse(r["uarUpdatedDt"].ToString()); }
                            vr.UaType = r["uaType"].ToString();
                            if (r["SiteID"].ToString().Length > 0)
                            { vr.SiteId = int.Parse(r["SiteID"].ToString()); }
                            vr.UaDbnotes = r["uaDBnotes"].ToString();
                            vr.UaNurseNote = r["uaNurseNote"].ToString();
                            if (tbl.Columns.Contains("uasig")) { vr.UaSig = r["uaSig"].ToString(); }
                            if (r["uaSigDt"].ToString().Length > 0)
                            { vr.UaSigDt = DateTime.Parse(r["uaSigDt"].ToString()); }

                            vr.UaSigUser = r["uaSigUser"].ToString();
                            if (tbl.Columns.Contains("location_"))
                            {
                                vr.Location = r["location_"].ToString();
                                if (r["scheduledDate"].ToString().Length > 0)
                                { vr.ScheduledDate = DateTime.Parse(r["scheduledDate"].ToString()); }
                                vr.UaBase64 = r["uaBase64"].ToString();
                                vr.Uaprogram = r["UAProgram"].ToString();
                            }

                            if (NewRow)
                            {
                                //v.Add(vr);
                                vsNew.Add(vr);
                                NewRow = false;
                            }
                        }
                    }
                    db.SaveChanges();
                    //db.TblUaresults.UpdateRange(v);
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
                //Console.WriteLine(e.InnerException.Message.ToString());
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
                    //db.TblUaresultDetail.UpdateRange(rds);
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
                Console.WriteLine(e.InnerException.ToString());
            }
            return res;
        }
    }
}
