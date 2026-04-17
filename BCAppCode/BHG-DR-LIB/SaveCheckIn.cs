using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveCheckIn(DataTable tbl, string sc, DateTime workdate, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes();
            rc.IsResult = true;
            rc.RowsProcessed = tbl.Rows.Count;

            if (tbl.Rows.Count > 0)
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        //Models.TblCheckIn chkIn;
                        int ciid = int.Parse(tbl.Rows[0]["ciid"].ToString());
                        var chkIns = db.TblCheckIn.Where(x => x.SiteCode == sc
                            && (x.CiDate.Date >= workdate.Date || x.CiId >= ciid)
                            ).ToList();
                        if (chkIns == null) { AllNewRows = true; }
                        Models.TblCheckIn chkin;
                        foreach (DataRow r in tbl.Rows)
                        {
                            int id = int.Parse(r["ciID"].ToString());
                            int rcs = int.Parse(r["RowChkSum"].ToString());
                            DateTime dt = DateTime.Parse(r["ciDate"].ToString());
                            if (AllNewRows)
                            {
                                NewRow = true;
                                chkin = new Models.TblCheckIn
                                {
                                    SiteCode = sc,
                                    CiId = id,
                                    RowChkSum = rcs,
                                    CiDate = dt
                                };
                                rc.RowsIns += 1;
                            }
                            else
                            {
                                chkin = chkIns.Where(x => x.CiId == id).FirstOrDefault();
                                if (chkin == null)
                                {
                                    NewRow = true;
                                    chkin = new Models.TblCheckIn
                                    {
                                        SiteCode = sc,
                                        CiId = id,
                                        RowChkSum = rcs,
                                        CiDate = dt
                                    };
                                    rc.RowsIns += 1;
                                }
                                else
                                {
                                    rc.RowsUpd += 1;
                                }
                            }
                            if ((chkin.RowChkSum != rcs) || (NewRow))
                            {
                                chkin.CiCltid = r["ciCLTID"].ToString();
                                chkin.CiTime = DateTime.Parse(r["ciTime"].ToString());
                                if (r["ciServeddtm"].ToString().Length > 7)
                                {
                                    DateTime sdt = DateTime.Parse(r["ciServeddtm"].ToString());
                                    chkin.CiServeddtm = sdt;
                                    TimeSpan tsWait = sdt.Subtract(chkin.CiTime);
                                    int lx = tsWait.TotalMinutes.ToString().Length < 4 ? tsWait.TotalMinutes.ToString().Length : 4;
                                    chkin.MinutesWaited = decimal.Parse(tsWait.TotalMinutes.ToString().Substring(0, lx));
                                }
                                chkin.CiDate = DateTime.Parse(r["ciDate"].ToString());
                                chkin.LastModAt = DateTime.Now;
                                chkin.CicltName = r["cicltName"].ToString();
                                if (r["ciHOLD"].ToString().Length > 0)
                                {
                                    chkin.CiHold = bool.Parse(r["ciHold"].ToString());
                                }
                                chkin.Cicltm4id = r["cicltm4id"].ToString();
                                chkin.CiCode = r["ciCode"].ToString();
                                chkin.CiQueue = r["ciQueue"].ToString();
                                chkin.CiServedStaff = r["ciServedStaff"].ToString();
                                if (r["ciAmt"].ToString().Length > 0)
                                {
                                    chkin.CiAmt = int.Parse(r["ciAmt"].ToString());
                                }
                                if (r["ciDoses"].ToString().Length > 0)
                                {
                                    chkin.CiDoses = int.Parse(r["ciDoses"].ToString());
                                }
                                if (tbl.Columns.Contains("ciQUEUETIME"))
                                {
                                    if (r["ciqueuetime"].ToString().Length > 6)
                                    {
                                        chkin.ciQUEUETIME = DateTime.Parse(r["ciqueuetime"].ToString());
                                    }
                                }
                            }
                            if (NewRow || AllNewRows)
                            {
                                NewRow = false;
                                db.TblCheckIn.Add(chkin);
                            }
                        }
                        //db.TblCheckIn.UpdateRange(chkins);
                        db.SaveChanges();
                    }
                    catch (Exception e)
                    {
                        rc.IsResult = false;
                        Console.WriteLine(e.Message.ToString());
                        rc.ExceptMsg = e.Message;
                        if (e.InnerException != null)
                        {
                            Console.WriteLine(e.InnerException.Message.ToString());
                            rc.ExceptInnerMsg = e.InnerException.Message;
                        }
                    }
                }
            }
            return rc;
        }
    }
}
