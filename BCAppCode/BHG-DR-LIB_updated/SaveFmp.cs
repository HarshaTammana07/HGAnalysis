using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveFmp(DataTable tbl, string sc, DateTime dtWrk, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime LastMod = DateTime.Today;
                List<Models.TblFmp> fmps = db.TblFmp.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblFmp> nFmps = new List<Models.TblFmp>();
                //Loop for Deleted rows
                foreach(var f in fmps)
                {
                    if (f.RowState == true) 
                    { 
                        f.RowState = false;
                        f.LastModAt = LastMod;
                    }
                }
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblFmp fmp = new Models.TblFmp();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                fmp.SiteCode = sc;
                                fmp.LastModAt = LastMod;
                                fmp.RowState = true;
                                break;
                            case "fmpid":
                                fmp.FmpId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "fmplngclt":
                                if (r[c.ColumnName].ToString().Length > 1)
                                {
                                    fmp.FmpLngClt = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpdtstart":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    fmp.FmpDtStart = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpdtprojend":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    fmp.FmpDtProjEnd = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpdtend":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    fmp.FmpDtEnd = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpintrate":
                                if (r[c.ColumnName].ToString().Length > 1)
                                {
                                    fmp.FmpIntRate = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpstrreason":
                                fmp.FmpStrReason = r[c.ColumnName].ToString();
                                break;
                            case "fmpstrdesc":
                                fmp.FmpStrDesc = r[c.ColumnName].ToString();
                                break;
                            case "fmpdtadded":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    fmp.FmpDtAdded = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpstruseradded":
                                fmp.FmpStrUserAdded = r[c.ColumnName].ToString();
                                break;
                            case "fmpdtended":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    fmp.FmpDtEnded = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "fmpstruserended":
                                fmp.FmpStrUserEnded = r[c.ColumnName].ToString();
                                break;
                            case "fmpendtext":
                                fmp.FmPendtext = r[c.ColumnName].ToString();
                                break;
                            case "atrisktype":
                                fmp.AtriskType = r[c.ColumnName].ToString();
                                break;
                            case "rowstate":
                                fmp.RowState = true;
                                break;
                            case "lastmodat":
                                fmp.LastModAt = LastMod;
                                break;
                        }
                    }
                    Models.TblFmp dbF = fmps.FirstOrDefault(x => x.FmpId == fmp.FmpId);
                    if (dbF == null)
                    {
                        nFmps.Add(fmp);
                        res.RowsIns += 1;
                    }
                    else
                    {
                        dbF.AtriskType = fmp.AtriskType;
                        dbF.FmpDtAdded = fmp.FmpDtAdded;
                        dbF.FmpDtEnd = fmp.FmpDtEnd;
                        dbF.FmpDtEnded = fmp.FmpDtEnded;
                        dbF.FmpDtProjEnd = fmp.FmpDtProjEnd;
                        dbF.FmpDtStart = fmp.FmpDtStart;
                        dbF.FmPendtext = fmp.FmPendtext;
                        dbF.FmpIntRate = fmp.FmpIntRate;
                        dbF.FmpLngClt = fmp.FmpLngClt;
                        dbF.FmpStrDesc = fmp.FmpStrDesc;
                        dbF.FmpStrReason = fmp.FmpStrReason;
                        dbF.FmpStrUserAdded = fmp.FmpStrUserAdded;
                        dbF.FmpStrUserEnded = fmp.FmpStrUserEnded;
                        dbF.LastModAt = fmp.LastModAt;
                        dbF.RowState = fmp.RowState;
                    }
                }
                db.SaveChanges();
                if (nFmps.Count > 0)
                {
                    db.TblFmp.AddRange(nFmps);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
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
    }
}
