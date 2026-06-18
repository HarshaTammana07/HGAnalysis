using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveRowTrax(string sc, DateTime rcdate, string tblname, int sammscnt, int azurecnt, Models.BHG_DRContext db)
        {
            Models.RCodes rCodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = 1
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }

                Models.TblRowTrax rt = db.TblRowTrax.Where(x => x.SiteCode == sc
                    && x.Rcdate.Date == rcdate.Date
                    && x.TblName == tblname).FirstOrDefault();

                if (rt == null)
                {
                    rt = new Models.TblRowTrax
                    {
                        SiteCode = sc, Rcdate = rcdate, TblName = tblname, SammsCnt = sammscnt, AzureCnt = azurecnt, LastModAt = DateTime.Now
                    };
                    db.TblRowTrax.Add(rt);
                }
                else
                {
                    rt.AzureCnt = azurecnt;
                    rt.SammsCnt = sammscnt;
                    rt.LastModAt = DateTime.Now;
                }
                db.SaveChanges();
            }
            catch (Exception e)
            {
                rCodes.ExceptMsg = e.Message.ToString();
                rCodes.IsResult = false;
            }

            return rCodes;
        }
    }
}
