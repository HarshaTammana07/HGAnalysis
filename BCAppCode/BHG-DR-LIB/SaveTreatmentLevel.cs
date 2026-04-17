using System;
using System.Data;
using System.Collections.Generic;
using System.Text;
using System.Linq;


namespace BHG_DR_LIB
{
    public partial class SaveData

    {
        public Models.RCodes SaveTreatmentLevel(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                DateTime runat = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblTreatmentLevel> ntls = new List<Models.TblTreatmentLevel>();
                List<Models.TblTreatmentLevel> treatmentLevels = db.TblTreatmentLevels.Where(x => x.SiteCode == sc).ToList();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblTreatmentLevel xtl = new Models.TblTreatmentLevel();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        try
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    xtl.SiteCode = dr[c.ColumnName].ToString();
                                    xtl.LastModAt = runat;
                                    xtl.RowChkSum = int.Parse(dr["RowChkSum"].ToString());
                                    break;
                                case "id":
                                    xtl.ID = int.Parse(dr[c.ColumnName].ToString());
                                    break;
                                case "treatmentlevel":
                                    xtl.TreatmentLevel = dr[c.ColumnName].ToString();
                                    break;
                                case "userid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xtl.UserID = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "cltid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        xtl.CltId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "recordon":
                                    if (dr[c.ColumnName].ToString().Length > 5)
                                    {
                                        xtl.RecordOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(c.ColumnName.ToString() + " : " + dr[c.ColumnName].ToString());
                        }
                    }
                    Models.TblTreatmentLevel dbxtl = treatmentLevels.FirstOrDefault(x => x.SiteCode == xtl.SiteCode && x.ID == xtl.ID);
                    if (dbxtl == null)
                    {
                        rc.RowsIns += 1;
                        ntls.Add(xtl);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbxtl.CltId = xtl.CltId;
                        dbxtl.LastModAt = xtl.LastModAt;
                        dbxtl.RecordOn = xtl.RecordOn;
                        dbxtl.RowChkSum = xtl.RowChkSum;
                        dbxtl.RowState = xtl.RowState;
                        dbxtl.TreatmentLevel = xtl.TreatmentLevel;
                        dbxtl.UserID = xtl.UserID;
                    }
                }
                db.SaveChanges();
                if (ntls.Count > 0)
                {
                    db.TblTreatmentLevels.AddRange(ntls);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                }
            }

            return rc;
        }
    }
}
