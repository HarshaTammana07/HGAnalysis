using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;


namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveDoses (DataTable tbl, string sc, DateTime dtWrk, bool reload, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (tbl.Rows.Count > 0)
            { 
            List<Models.TblDose> newdoses = new List<Models.TblDose>();
            //List<Models.TblDose> UpdDoses = new List<Models.TblDose>();

            if (db == null) { db = new Models.BHG_DRContext(); }
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    Models.TblDose dose;
                    if (reload)
                    {
                        SQLSvrManager sm = new SQLSvrManager();
                        _ = sm.ExeSqlCmd("Delete from pats.tbl_Dose where SiteCode = '" + sc + "'", sm.ConnectionString);
                    }
                    List<Models.TblDose> doses = db.TblDose.Where(x => x.SiteCode == sc
                        //&& (x.DtDate.Value.Year >= dtWrk.Year || x.DtMedDate.Year >= dtWrk.Year)
                        ).ToList();
                    if (doses.Count == 0) { AllNewRows = true; }
                    else
                    {
                        foreach (Models.TblDose d in doses)
                        {
                            if (d.DtDate >= dtWrk.Date) { d.RowState = false; }
                        }
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        long intDoseId = long.Parse(r["DoseId"].ToString());
                        int intcltid = int.Parse(r["cltid"].ToString());
                        int rcs = int.Parse(r["rowchksum"].ToString());
                        DateTime meddt = DateTime.Parse(r["dtmeddate"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dose = new Models.TblDose
                            {
                                SiteCode = sc,
                                DoseId = intDoseId,
                                CltId = intcltid,
                                RowChkSum = 0,
                                DtMedDate = meddt
                            };
                            res.RowsIns += 1;
                        }
                        else
                        {
                            dose = doses.Where(x => x.DoseId == intDoseId).FirstOrDefault();
                            if (dose == null)
                            {
                                dose = new Models.TblDose
                                {
                                    SiteCode = sc,
                                    DoseId = intDoseId,
                                    CltId = intcltid,
                                    RowChkSum = 0,
                                    RowState = true,
                                    DtMedDate = meddt
                                };
                                NewRow = true;
                                res.RowsIns += 1;
                            }
                            else
                            { res.RowsUpd += 1; }
                        }
                        if (NewRow || (rcs != dose.RowChkSum))
                        {
                            dose.RowState = true;
                            dose.LastModAt = RunDT;
                            dose.RowChkSum = rcs;
                            //if (r["dtmeddate"].ToString().Length > 6)
                            //{
                            //    dose.DtMedDate = DateTime.Parse(r["dtmeddate"].ToString());
                            //}
                            if (r["dtdate"].ToString().Length > 6)
                            {
                                dose.DtDate = DateTime.Parse(r["dtdate"].ToString());
                            }
                            if (r["guestid"].ToString().Length > 0)
                            {
                                dose.GuestId = int.Parse(r["guestid"].ToString());
                            }
                            if (r["dose"].ToString().Length > 0)
                            {
                                dose.Dose = int.Parse(r["dose"].ToString());
                            }
                            dose.StrUser = r["struser"].ToString();
                            dose.StrVoidReason = r["strvoidreason"].ToString();
                            dose.Bottletype = r["bottletype"].ToString();
                            if (r["Blvoid"].ToString().Length > 0)
                            {
                                dose.BlVoid =  bool.Parse(r["blvoid"].ToString());
                            }
                            if (r["blexception"].ToString().Length > 0)
                            {
                                dose.BlException = bool.Parse(r["blexception"].ToString());
                            }
                            if (r["ordernum"].ToString().Length > 0)
                            {
                                dose.Ordernum = int.Parse(r["ordernum"].ToString());
                            }
                            dose.ExceptionReason = r["exceptionreason"].ToString();
                            if (r["blbulk"].ToString().Length > 0)
                            {
                                dose.BlBulk = bool.Parse(r["blbulk"].ToString());
                            }
                            if (r["blprepack"].ToString().Length > 0)
                            {
                                dose.BlPrepack = bool.Parse(r["blprepack"].ToString());
                            }
                            if (r["dtvoid"].ToString().Length > 0)
                            {
                                dose.DtVoid = bool.Parse(r["dtvoid"].ToString());
                            }
                            if (r["dtgiven"].ToString().Length > 6)
                            {
                                dose.Dtgiven = DateTime.Parse(r["dtgiven"].ToString());
                            }
                            if (r["dtprep"].ToString().Length > 0)
                            {
                                dose.Dtprep = DateTime.Parse(r["dtprep"].ToString());
                            }
                            dose.Ppstaff = r["ppstaff"].ToString();
                            dose.Exceptiontype = r["exceptiontype"].ToString();
                            dose.Manualauthuser = r["Manualauthuser"].ToString();
                            if (r["manualauthdtm"].ToString().Length > 6)
                            {
                                dose.Manualauthdtm = DateTime.Parse(r["manualauthdtm"].ToString());
                            }
                            dose.Dosenote = r["dosenote"].ToString();
                            dose.Dosesig = r["dosesig"].ToString();
                            if (r.Table.Columns.Contains("InventoryGroup"))
                            {
                                dose.InventoryGroup = r["inventorygroup"].ToString();
                            }
                            if (dose.SiteCode == "PHC") { dose.SiteId = 105; }
                            else
                            {
                                if (r["siteid"].ToString().Length > 0)
                                {
                                    dose.SiteId = int.Parse(r["siteid"].ToString());
                                }
                            }
                            //if (r["dosesigimg"].ToString().Length > 0)
                            dose.DoseSigImg = System.Text.Encoding.ASCII.GetBytes(r["dosesigimg"].ToString());
                            if ((dose.BlVoid == true) && (dose.DtVoid == true)) { dose.RowState = false; }
                        }
                        else
                        {
                            dose.RowState = true;
                            if ((dose.BlVoid == true) && (dose.DtVoid == true)) { dose.RowState = false; }
                            if ((dose.CltId < 0) && (dose.CltId != -111)) { dose.RowState = false; }
                        }
                        if (NewRow || AllNewRows)
                        {
                            NewRow = false;
                            if ((dose.BlVoid == true) && (dose.DtVoid == true)) { dose.RowState = false; }
                            if ((dose.CltId < 0) && (dose.CltId != -111)) { dose.RowState = false; }
                            newdoses.Add(dose);
                        }
                    }
                    db.SaveChanges();
                    //db.BulkUpdate(UpdDoses);
                    if (newdoses.Count > 0)
                    {
                        //if (newdoses.Count >= 10000)
                        //{

                        //}
                        //else
                        {
                            db.TblDose.AddRange(newdoses);
                            db.SaveChanges();
                        }
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
            }
            return res;
        }
        public bool SaveDoseExcuse(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            bool res = true;
            if (tbl.Rows.Count > 0)
            { 
            if (db == null) { db = new Models.BHG_DRContext(); }
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    DateTime RunDT = DateTime.Now;
                    Models.TblDoseExcuse dose;
                    List<Models.TblDoseExcuse> doses = db.TblDoseExcuse.Where(x => x.SiteCode == sc).ToList();
                    if (doses.Count == 0) { AllNewRows = true; }
                    else
                    {
                        foreach (Models.TblDoseExcuse d in doses)
                        { d.RowState = false; }
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int intExId = int.Parse(r["ExId"].ToString());
                        int intcltid = int.Parse(r["cltid"].ToString());
                        int rcs = int.Parse(r["rowchksum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            dose = new Models.TblDoseExcuse
                            {
                                SiteCode = sc,
                                ExId = intExId,
                                CltId = intcltid,
                                RowChkSum = rcs
                            };
                        }
                        else
                        {
                            dose = doses.Where(x => x.ExId == intExId).FirstOrDefault();
                            if (dose == null)
                            {
                                dose = new Models.TblDoseExcuse
                                {
                                    SiteCode = sc,
                                    ExId = intExId,
                                    CltId = intcltid,
                                    RowChkSum = rcs
                                };
                                NewRow = true;
                                
                            }
                        }
                        if (NewRow || (rcs != dose.RowChkSum))
                        {
                            dose.RowState = true;
                            dose.LastModAt = RunDT;
                            dose.CltId = intcltid;
                            if (r["DtEx"].ToString().Length > 6)
                            {
                                dose.DtEx = DateTime.Parse(r["DtEx"].ToString());
                            }
                            if (r["Dtstamp"].ToString().Length > 6)
                            {
                                dose.Dtstamp = DateTime.Parse(r["Dtstamp"].ToString());
                            }
                            dose.StrUser = r["StrUser"].ToString();

                        }
                        else
                        {
                            dose.RowState = true;
                            dose.LastModAt = RunDT;
                        }
                        if (NewRow || AllNewRows)
                        {
                            NewRow = false;
                            db.TblDoseExcuse.Add(dose);
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
    }
}
