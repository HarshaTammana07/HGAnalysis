using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveCustomQuestions(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rcodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                List<Models.TblCustomQuestions> NewCQs = new List<Models.TblCustomQuestions>();
                List <Models.TblCustomQuestions> CQs = db.TblCustomQuestions.Where(x => x.SiteCode == sc).OrderBy(o => o.CId).ToList();
                foreach(Models.TblCustomQuestions c in CQs)
                {
                    if (c.RowSate == 1) { c.RowSate = 0; }
                }
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblCustomQuestions cq = new Models.TblCustomQuestions();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                cq.SiteCode = r[c.ColumnName].ToString();
                                cq.RowSate = 1;
                                cq.LastModAt = DateTime.Now;
                                break;
                            case "rowchksum":
                            case "rowchecksum":
                                cq.RowCheckSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "cid":
                                cq.CId = int.Parse(r[c.ColumnName].ToString());
                                if (cq.CId < 0) { cq.RowSate = 0; }
                                break;
                            case "cquestion":
                                cq.CQuestion = r[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblCustomQuestions dcq = CQs.Where(x => x.CId == cq.CId).FirstOrDefault();
                    if (dcq == null)
                    {
                        NewCQs.Add(cq);
                    }
                    else
                    {
                        if (cq.CId < 0) { cq.RowSate = 0; } else { cq.RowSate = 1; }
                        if (dcq.RowCheckSum == cq.RowCheckSum)
                        {
                            dcq.RowSate = cq.RowSate;
                            dcq.LastModAt = DateTime.Now;
                        }
                        else
                        {
                            dcq.CQuestion = cq.CQuestion;
                            dcq.LastModAt = DateTime.Now;
                            dcq.RowCheckSum = cq.RowCheckSum;
                            dcq.RowSate = cq.RowSate;
                        }
                    }
                }
                db.SaveChanges();
                if (NewCQs.Count > 0)
                {
                    db.TblCustomQuestions.AddRange(NewCQs);
                    db.SaveChanges();
                }
            }

            catch (Exception e)
            {
                rcodes.IsResult = false;
                rcodes.ExceptMsg = e.Message;
                if (e.InnerException.Message != null)
                {
                    rcodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rcodes;
        }
        public Models.RCodes SaveCustomAnswers(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rcodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                List<Models.TblCustomAnswers> CAs = db.TblCustomAnswers.Where(x => x.SiteCode == sc).OrderBy(o => o.CaId).ThenBy(p => p.CaCltid).ToList();
                List<Models.TblCustomAnswers> NewCAs = new List<Models.TblCustomAnswers>();
                foreach (Models.TblCustomAnswers ca in CAs)
                {
                    if (ca.RowSate == 1) { ca.RowSate = 0; }
                }
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblCustomAnswers ca = new Models.TblCustomAnswers();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                ca.SiteCode = r[c.ColumnName].ToString();
                                ca.RowSate = 1;
                                ca.LastModAt = DateTime.Now;
                                break;
                            case "rowchksum":
                            case "rowchecksum":
                                ca.RowCheckSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "caid":
                                ca.CaId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "caqid":
                                ca.CaQid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "cacltid":
                                ca.CaCltid = int.Parse(r[c.ColumnName].ToString());
                                if (ca.CaCltid < 0) { ca.RowSate = 0; } else { ca.RowSate = 1; }
                                break;
                            case "caans":
                                ca.CaAns = r[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblCustomAnswers dca = CAs.Where(x => x.CaId == ca.CaId
                        && x.CaQid == ca.CaQid 
                        && x.CaCltid == ca.CaCltid).FirstOrDefault();
                    if (dca == null)
                    {
                        NewCAs.Add(ca);
                    }
                    else
                    {
                        if (dca.RowCheckSum == ca.RowCheckSum)
                        {
                            dca.RowSate = ca.RowSate;
                            dca.LastModAt = DateTime.Now;
                        }
                        else
                        {
                            dca.LastModAt = DateTime.Now;
                            dca.RowCheckSum = ca.RowCheckSum;
                            dca.RowSate = ca.RowSate;
                            dca.CaAns = ca.CaAns;
                        }
                    }
                }
                db.SaveChanges();
                if (NewCAs.Count > 0)
                {
                    db.TblCustomAnswers.AddRange(NewCAs);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rcodes.IsResult = false;
                rcodes.ExceptMsg = e.Message;
                if (e.InnerException.Message != null)
                {
                    rcodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rcodes;
        }

    }
}
