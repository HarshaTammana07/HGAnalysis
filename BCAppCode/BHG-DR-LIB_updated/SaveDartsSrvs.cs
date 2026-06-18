using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;


namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public bool SaveDartSrv2014(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv> DSNew = new List<Models.TblDartsSrv>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv> darts = db.TblDartsSrv.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv();
                            dart.SiteCode = sc;
                            dart.DsId = ds;
                            dart.RowChkSum = myrcs;
                            dart.LastModAt = lastmod;
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv();
                                dart.SiteCode = sc;
                                dart.DsId = ds;
                                dart.RowChkSum = myrcs;
                                dart.LastModAt = lastmod;
                                NewRow = true;
                            }
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();
                    
                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2015(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2015> DSNew = new List<Models.TblDartsSrv_2015>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2015 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2015> darts = db.TblDartsSrv2015.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2015();
                            dart.SiteCode = sc;
                            dart.DsId = ds;
                            dart.LastModAt = lastmod;
                            dart.RowChkSum = myrcs;
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2015();
                                dart.SiteCode = sc;
                                dart.DsId = ds;
                                dart.RowChkSum = myrcs;
                                dart.LastModAt = lastmod;
                                NewRow = true;
                            }
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2015.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2016(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2016> DSNew = new List<Models.TblDartsSrv_2016>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2016 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2016> darts = db.TblDartsSrv2016.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2016();
                            dart.SiteCode = sc;
                            dart.DsId = ds;
                            dart.RowChkSum = myrcs;
                            dart.LastModAt = lastmod;
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2016();
                                dart.SiteCode = sc;
                                dart.DsId = ds;
                                dart.RowChkSum = myrcs;
                                dart.LastModAt = lastmod;
                                NewRow = true;
                            }
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2016.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2017(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2017> DSNew = new List<Models.TblDartsSrv_2017>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2017 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2017> darts = db.TblDartsSrv2017.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2017
                            {
                                SiteCode = sc,
                                DsId = ds,
                                RowChkSum = myrcs, 
                                LastModAt = lastmod
                            };
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2017
                                {
                                    SiteCode = sc,
                                    DsId = ds,
                                    RowChkSum = myrcs, 
                                    LastModAt = lastmod
                                };
                                NewRow = true;
                            }
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2017.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2018(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2018> DSNew = new List<Models.TblDartsSrv_2018>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2018 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2018> darts = db.TblDartsSrv2018.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2018
                            {
                                SiteCode = sc,
                                DsId = ds,
                                RowChkSum = myrcs, 
                                LastModAt = lastmod
                            };
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2018
                                {
                                    SiteCode = sc,
                                    DsId = ds,
                                    RowChkSum = myrcs, 
                                    LastModAt = lastmod
                                };
                                NewRow = true;
                            }
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2018.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2019(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2019> DSNew = new List<Models.TblDartsSrv_2019>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2019 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2019> darts = db.TblDartsSrv2019.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2019
                            {
                                SiteCode = sc,
                                DsId = ds,
                                RowChkSum = myrcs, 
                                LastModAt = lastmod
                            };
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2019
                                {
                                    SiteCode = sc,
                                    DsId = ds,
                                    RowChkSum = myrcs, 
                                    LastModAt = lastmod
                                };
                                NewRow = true;
                            }
                            dart.LastModAt = lastmod;
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2019.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2020(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2020> DSNew = new List<Models.TblDartsSrv_2020>();
            List<Models.TblDartsSrv_2020> dsUpd = new List<Models.TblDartsSrv_2020>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2020 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2020> darts = db.TblDartsSrv2020.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                       //|| x.DsDtEnd == null)
                       //&& x.DsId <= EndID
                       )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2020
                            {
                                SiteCode = sc,
                                DsId = ds,
                                RowChkSum = myrcs, 
                                LastModAt = lastmod
                            };
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2020
                                {
                                    SiteCode = sc,
                                    DsId = ds,
                                    RowChkSum = myrcs, 
                                    LastModAt = lastmod
                                };
                                NewRow = true;
                            }
                            dart.LastModAt = lastmod;
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                            else
                            {
                                dsUpd.Add(dart);
                            }
                            //if (pcnt == 10000)
                            //{
                            //    pcnt = 0;
                            //    db.SaveChanges();
                            //}
                        }
                    }
                    if (dsUpd.Count > 0)
                    {
                        db.TblDartsSrv2020.UpdateRange(dsUpd);
                        db.SaveChanges();
                    }
                    //db.BulkUpdate(dsUpd);

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2020.AddRange(DSNew);
                        //db.BulkInsert(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2021(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2021> DSNew = new List<Models.TblDartsSrv_2021>();
            List<Models.TblDartsSrv_2021> dsUpd = new List<Models.TblDartsSrv_2021>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2021 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2021> darts = db.TblDartsSrv2021.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2021
                            {
                                SiteCode = sc,
                                DsId = ds,
                                RowChkSum = myrcs,
                                LastModAt = lastmod
                            };
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2021
                                {
                                    SiteCode = sc,
                                    DsId = ds,
                                    RowChkSum = myrcs, 
                                    LastModAt = lastmod
                                };
                                NewRow = true;
                            }
                            dart.LastModAt = lastmod;
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                            else 
                            {
                                dsUpd.Add(dart);
                            }
                        }
                    }
                    if (dsUpd.Count > 0)
                    {
                        db.TblDartsSrv2021.UpdateRange(dsUpd);
                        db.SaveChanges();
                    }
                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2021.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2022(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2022> DSNew = new List<Models.TblDartsSrv_2022>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2022 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2022> darts = db.TblDartsSrv2022.Where(x => x.SiteCode == sc
                       //&& (x.DsId >= StartID
                       //|| x.Dsbilled.Value.Date == wrkdt.Date
                       //|| x.Dsbilled == null
                       //|| x.DsUpdate.Value.Date == wrkdt
                       //|| x.DsDtAdded.Value.Date == wrkdt
                       //|| x.DsDtStart.Value.Date == wrkdt
                       //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2022();
                            dart.SiteCode = sc;
                            dart.DsId = ds;
                            dart.RowChkSum = myrcs;
                            dart.LastModAt = lastmod;
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2022();
                                dart.SiteCode = sc;
                                dart.DsId = ds;
                                dart.RowChkSum = myrcs;
                                dart.LastModAt = lastmod;
                                NewRow = true;
                            }
                            dart.LastModAt = lastmod;
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2022.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
        public bool SaveDartSrv2023(DataTable tbl, string sc, long akey, DateTime wrkdt, Models.BHG_DRContext db)
        {
            bool res = true;
            List<Models.TblDartsSrv_2023> DSNew = new List<Models.TblDartsSrv_2023>();
            wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
            int StartID = int.Parse(tbl.Rows[0]["dsid"].ToString());
            int EndID = int.Parse(tbl.Rows[tbl.Rows.Count - 1]["dsid"].ToString());
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblDartsSrv_2023 dart = null;
                    int pcnt = 0;
                    DateTime lastmod = DateTime.Now;
                    //Console.WriteLine("Getting List.");
                    List<Models.TblDartsSrv_2023> darts = db.TblDartsSrv2023.Where(x => x.SiteCode == sc
                        //&& (x.DsId >= StartID
                        //|| x.Dsbilled.Value.Date == wrkdt.Date
                        //|| x.Dsbilled == null
                        //|| x.DsUpdate.Value.Date == wrkdt
                        //|| x.DsDtAdded.Value.Date == wrkdt
                        //|| x.DsDtStart.Value.Date == wrkdt
                        //|| x.DsDtEnd.Value.Date == wrkdt)
                        //|| x.DsDtEnd == null)
                        //&& x.DsId <= EndID
                        )
                        //.Take(tbl.Rows.Count + 500)
                        .ToList();
                    if (darts.Count == 0)
                    {
                        AllNewRows = true;
                        //Console.WriteLine("No Rows - All New");
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        pcnt++;
                        int myrcs = 0;
                        if (r.Table.Columns.Contains("rowchksum"))
                        {
                            myrcs = int.Parse(r["RowChkSum"].ToString());
                        }
                        int ds = int.Parse(r["dsid"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dart = new Models.TblDartsSrv_2023();
                            dart.SiteCode = sc;
                            dart.DsId = ds;
                            dart.RowChkSum = myrcs;
                            dart.LastModAt = lastmod;
                        }
                        else
                        {
                            dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
                            if (dart == null)
                            {
                                dart = new Models.TblDartsSrv_2023();
                                dart.SiteCode = sc;
                                dart.DsId = ds;
                                dart.RowChkSum = myrcs;
                                dart.LastModAt = lastmod;
                                NewRow = true;
                            }
                            dart.LastModAt = lastmod;
                        }
                        if ((dart.RowChkSum != myrcs) || (NewRow))
                        {
                            dart.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["DsClt"].ToString().Length > 0) { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
                            if (r["DsDim1"].ToString().Length > 0) { dart.DsDim1 = bool.Parse(r["DsDim1"].ToString()); }
                            if (r["DsDim2"].ToString().Length > 0) { dart.DsDim2 = bool.Parse(r["DsDim2"].ToString()); }
                            if (r["DsDim3"].ToString().Length > 0) { dart.DsDim3 = bool.Parse(r["DsDim3"].ToString()); }
                            if (r["DsDim4"].ToString().Length > 0) { dart.DsDim4 = bool.Parse(r["DsDim4"].ToString()); }
                            if (r["DsDim5"].ToString().Length > 0) { dart.DsDim5 = bool.Parse(r["DsDim5"].ToString()); }
                            if (r["DsDim6"].ToString().Length > 0) { dart.DsDim6 = bool.Parse(r["DsDim6"].ToString()); }
                            dart.DsTxtSrv = r["DsTxtSrv"].ToString();
                            if (r["DsDtStart"].ToString().Length > 7) { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
                            if (r["DsDtEnd"].ToString().Length > 7) { dart.DsDtEnd = DateTime.Parse(r["DsDtEnd"].ToString()); }
                            dart.DsTxtType = r["DsTxtType"].ToString();
                            if (r["DsdblUnits"].ToString().Length > 0) { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
                            if (r["DsNoteId"].ToString().Length > 0) { dart.DsNoteId = int.Parse(r["DsNoteId"].ToString()); }
                            if (r["DsDtAdded"].ToString().Length > 0) { dart.DsDtAdded = DateTime.Parse(r["DsDtAdded"].ToString()); }
                            dart.DstxtStaff = r["DstxtStaff"].ToString();
                            if (r["Dsbilled"].ToString().Length > 7) { dart.Dsbilled = DateTime.Parse(r["Dsbilled"].ToString()); }
                            dart.DsGroupnum = r["DsGroupnum"].ToString();
                            dart.DsProgram = r["dsPROGRAM"].ToString();
                            if (r["DsUpdate"].ToString().Length > 7) { dart.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                            dart.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                            if (r["DsInvalidatedOn"].ToString().Length > 7) { dart.DsInvalidatedOn = DateTime.Parse(r["DsInvalidatedOn"].ToString()); }
                            dart.DsError = r["DsError"].ToString();
                            dart.DsTxtHiv = r["DsTxtHiv"].ToString();
                            if (r["DsDartsGroup"].ToString().Length > 0) { dart.DsDartsGroup = int.Parse(r["DsDartsGroup"].ToString()); }
                            if (r["RepOldSrv"].ToString().Length > 0) { dart.RepOldSrv = decimal.Parse(r["RepOldSrv"].ToString()); }
                            if (r["DsSigDate"].ToString().Length > 7) { dart.DsSigDate = DateTime.Parse(r["DsSigDate"].ToString()); }
                            if (r["DssigdateCosign"].ToString().Length > 7) { dart.DssigdateCosign = DateTime.Parse(r["DssigdateCosign"].ToString()); }
                            dart.DsSigUser = r["DsSigUser"].ToString();
                            dart.DsSigUserCosign = r["DsSigUserCosign"].ToString();
                            if (r["DsSigcltdate"].ToString().Length > 7) { dart.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                            if (r["DsAptid"].ToString().Length > 0) { dart.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                            if (r["Dsuncharted"].ToString().Length > 0) { dart.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                            if (r["DsTxDim1"].ToString().Length > 0) { dart.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                            if (r["DsTxDim2"].ToString().Length > 0) { dart.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                            if (r["DsTxDim3"].ToString().Length > 0) { dart.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                            if (r["DsTxDim4"].ToString().Length > 0) { dart.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                            if (r["DsTxDim5"].ToString().Length > 0) { dart.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                            if (r["DsTxDim6"].ToString().Length > 0) { dart.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                            dart.DsDiag = r["DsDiag"].ToString();
                            dart.DsArea = r["DsArea"].ToString();
                            if (r["DsGroupDefaultNote"].ToString().Length > 0) { dart.DsGroupDefaultNote = bool.Parse(r["DsGroupDefaultNote"].ToString()); }
                            if (r["DsGroupEnd"].ToString().Length > 7) { dart.DsGroupEnd = DateTime.Parse(r["DsGroupEnd"].ToString()); }
                            if (r["DsGroupIdentity"].ToString().Length > 0) { dart.DsGroupIdentity = int.Parse(r["DsGroupIdentity"].ToString()); }
                            if (r["DsGroupStart"].ToString().Length > 7) { dart.DsGroupStart = DateTime.Parse(r["DsGroupStart"].ToString()); }
                            dart.DsDiag10 = r["DsDiag10"].ToString();
                            if (r["SiteId"].ToString().Length > 0) { dart.SiteId = int.Parse(r["SiteId"].ToString()); }
                            dart.DsDbnotes = r["DsDbnotes"].ToString();
                            if (r["Mg"].ToString().Length > 0) { dart.Mg = Double.Parse(r["Mg"].ToString()); }
                            dart.LastModAt = lastmod;
                            //if (tbl.Columns.Contains("upsize_ts"))
                            //{
                            //    //if (r["upsize_ts"].ToString().Length > 0) { dart.UpsizeTs = Encoding.ASCII.GetBytes(r["upsize_ts"].ToString()); }
                            //}
                            if (tbl.Columns.Contains("dstxtnote"))
                            {
                                dart.DstxtNote = r["DstxtNote"].ToString();
                                dart.DsRtbnote = r["DsRtbnote"].ToString();
                                dart.DsSigclt = r["DsSigclt"].ToString();
                                dart.DsSignature = r["DsSignature"].ToString();
                                dart.DssignatureCosign = r["DssignatureCosign"].ToString();
                                dart.DsSigcltuser = r["DsSigcltuser"].ToString();
                                if (r["DsSigCltImg"].ToString().Length > 0) { dart.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                                if (r["DsSignatureImg"].ToString().Length > 0) { dart.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                            }
                            dart.LastModAt = lastmod;
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                //darts.Add(dart);
                                DSNew.Add(dart);
                                //Console.WriteLine("Adding Row");
                            }
                        }
                    }
                    //db.TblDartsSrv.Update();
                    db.SaveChanges();

                    if (DSNew.Count > 0)
                    {
                        db.TblDartsSrv2023.AddRange(DSNew);
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    //Console.WriteLine(e.InnerException.Message);
                    //Console.ReadKey();
                }
            }
            return res;
        }
    }
}
