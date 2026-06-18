using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes Save3pElig(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try 
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.Tbl3pElig> Eligs;
                if (yearly)
                {
                    Eligs = db.Tbl3pElig.Where(x => x.EDate.Value.Year >= wrkdt.Year
                        && x.SiteCode == sc).ToList();
                }
                else
                {
                    Eligs = db.Tbl3pElig.Where(x => x.EDate.Value.Year >= wrkdt.Year
                        && x.SiteCode == sc).ToList();
                }
                foreach(Models.Tbl3pElig el in Eligs)
                {
                    el.RowState = false;
                }
                foreach(DataRow r in tbl.Rows)
                {
                    int eid = int.Parse(r["eid"].ToString());
                    int rcs = int.Parse(r["RowChkSum"].ToString());

                    Models.Tbl3pElig pe = Eligs.Where(x => x.EId == eid).FirstOrDefault();
                    if (pe == null)
                    {
                        pe = new Models.Tbl3pElig
                        {
                            SiteCode = sc,
                            EId = eid,
                            RowState = true,
                            RowChkSum = 0
                        };
                        Eligs.Add(pe);
                        db.Tbl3pElig.Add(pe);
                    }
                    if (pe.RowChkSum != rcs)
                    {
                        pe.RowChkSum = rcs;
                        pe.LastModAt = DateTime.Now;
                        pe.RowState = true;
                        pe.EClt = int.Parse(r["eclt"].ToString());
                        pe.EPayer = r["epayer"].ToString();
                        pe.EDate = DateTime.Parse(r["edate"].ToString());
                        pe.EStaff = r["estaff"].ToString();
                        pe.EPost = r["epost"].ToString();
                        pe.EResponse = r["eresponse"].ToString();
                        pe.EStatus = r["estatus"].ToString();
                        pe.EFormat = r["eformat"].ToString();
                        pe.Filepath = r["filepath"].ToString();
                        pe.EElecstatus = r["eelecstatus"].ToString();
                        pe.EstaffStatus = r["estaffstatus"].ToString();
                        pe.EstaffNote = r["estaffnote"].ToString();
                        pe.EScan = r["escan"].ToString();
                        if (r["eorigid"].ToString().Length > 0)
                        {
                            pe.EOrigid = int.Parse(r["eorigid"].ToString());
                        }
                        if (r["pyeligcheck"].ToString().Length > 6)
                        {
                            pe.Pyeligcheck = DateTime.Parse(r["pyeligcheck"].ToString());
                        }
                    }
                    else
                    {
                        pe.RowState = true;
                    }
                }
                db.SaveChanges();
            }
            catch(Exception e)
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
        public Models.RCodes Save3pSetup(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count, 
                RowsIns = 0, 
                RowsUpd = 0
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.Tbl3psetup> tblSetup;
                tblSetup = db.Tbl3psetup.Where(x => x.SiteCode == sc).ToList();
                DateTime execDT = DateTime.Now;

                foreach (DataRow r in tbl.Rows)
                {
                    Models.Tbl3psetup psetup = new Models.Tbl3psetup();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                psetup.SiteCode = sc;
                                psetup.LastModAt = execDT;
                                break;
                            case "3pid":
                            case "pid":
                                psetup._pId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "clinic":
                                psetup.Clinic = r[c.ColumnName].ToString();
                                break;
                            case "address":
                                psetup.Address = r[c.ColumnName].ToString();
                                break;
                            case "state":
                                psetup.State = r[c.ColumnName].ToString();
                                break;
                            case "zip":
                                psetup.Zip = r[c.ColumnName].ToString();
                                break;
                            case "npi":
                                psetup.Npi = r[c.ColumnName].ToString();
                                break;
                            case "taxid":
                                psetup.TaxId = r[c.ColumnName].ToString();
                                break;
                            case "medicaid":
                                psetup.Medicaid = r[c.ColumnName].ToString();
                                break;
                            case "city":
                                psetup.City = r[c.ColumnName].ToString();
                                break;
                            case "drlname":
                                psetup.Drlname = r[c.ColumnName].ToString();
                                break;
                            case "drfname":
                                psetup.Drfname = r[c.ColumnName].ToString();
                                break;
                            case "drnpi":
                                psetup.Drnpi = r[c.ColumnName].ToString();
                                break;
                            case "provideraddress":
                                psetup.ProviderAddress = r[c.ColumnName].ToString();
                                break;
                            case "providercity":
                                psetup.ProviderCity = r[c.ColumnName].ToString();
                                break;
                            case "providername":
                                psetup.ProviderName = r[c.ColumnName].ToString();
                                break;
                            case "providerphone":
                                psetup.ProviderPhone = r[c.ColumnName].ToString();
                                break;
                            case "providerstate":
                                psetup.ProviderState = r[c.ColumnName].ToString();
                                break;
                            case "providerzip":
                                psetup.ProviderZip = r[c.ColumnName].ToString();
                                break;
                            case "siteid":
                                if (r[c.ColumnName].ToString().Length == 0)
                                {
                                    psetup.SiteId = -1;
                                }
                                else
                                {
                                    psetup.SiteId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "clia":
                                psetup.Clia = r[c.ColumnName].ToString();
                                break;
                            case "strdbnotes":
                                psetup.StrDbnotes = r[c.ColumnName].ToString();
                                break;
                            case "providerdesc":
                                psetup.ProviderDesc = r[c.ColumnName].ToString();
                                break;
                            case "blhaspreloader":
                                if (r[c.ColumnName].ToString().Length == 0)
                                {
                                    psetup.BlHasPreloader = false;
                                }
                                else
                                {
                                    psetup.BlHasPreloader = bool.Parse(r[c.ColumnName].ToString());
                                    if (psetup.BlHasPreloader == null)
                                    {
                                        psetup.BlHasPreloader = false;
                                    }
                                }
                                break;
                            case "individualnpi":
                                if (r[c.ColumnName].ToString().Length == 0)
                                {
                                    psetup.IndividualNpi = false;
                                }
                                else
                                {
                                    psetup.IndividualNpi = bool.Parse(r[c.ColumnName].ToString());
                                    if (psetup.IndividualNpi == null)
                                    {
                                        psetup.IndividualNpi = false;
                                    }
                                }
                                break;
                            case "taxonomy":
                                psetup.Taxonomy = r[c.ColumnName].ToString();
                                break;
                            case "sftpun":
                                psetup.Sftpun = r[c.ColumnName].ToString();
                                break;
                            case "sftppw":
                                psetup.Sftppw = r[c.ColumnName].ToString();
                                break;
                            case "rowchksum":
                                psetup.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.Tbl3psetup dbSetup = tblSetup.FirstOrDefault(x => x._pId == psetup._pId);
                    if (dbSetup == null)
                    {
                        rc.RowsIns += 1;
                        db.Tbl3psetup.Add(psetup);
                    }
                    else
                    {
                        if (dbSetup.RowChkSum != psetup.RowChkSum)
                        {
                            rc.RowsUpd += 1;
                            dbSetup.RowChkSum = psetup.RowChkSum;
                            dbSetup.Address = psetup.Address;
                            dbSetup.BlHasPreloader = psetup.BlHasPreloader;
                            dbSetup.City = psetup.City;
                            dbSetup.Clia = psetup.Clia;
                            dbSetup.Clinic = psetup.Clinic;
                            dbSetup.Drfname = psetup.Drfname;
                            dbSetup.Drlname = psetup.Drlname;
                            dbSetup.Drnpi = psetup.Drnpi;
                            dbSetup.IndividualNpi = psetup.IndividualNpi;
                            dbSetup.LastModAt = psetup.LastModAt;
                            dbSetup.Medicaid = psetup.Medicaid;
                            dbSetup.Npi = psetup.Npi;
                            dbSetup.ProviderAddress = psetup.ProviderAddress;
                            dbSetup.ProviderCity = psetup.ProviderCity;
                            dbSetup.ProviderDesc = psetup.ProviderDesc;
                            dbSetup.ProviderName = psetup.ProviderName;
                            dbSetup.ProviderPhone = psetup.ProviderPhone;
                            dbSetup.ProviderState = psetup.ProviderState;
                            dbSetup.ProviderZip = psetup.ProviderZip;
                            dbSetup.Sftppw = psetup.Sftppw;
                            dbSetup.Sftpun = psetup.Sftpun;
                            dbSetup.SiteId = psetup.SiteId;
                            dbSetup.State = psetup.State;
                            dbSetup.StrDbnotes = psetup.StrDbnotes;
                            dbSetup.TaxId = psetup.TaxId;
                            dbSetup.Taxonomy = psetup.Taxonomy;
                            dbSetup.Zip = psetup.Zip;
                        }
                    }
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
        public Models.RCodes Save3pClaimNote(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count,
                RowsIns = 0,
                RowsUpd = 0
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.Tbl3pClaimNote> newCNs = new List<Models.Tbl3pClaimNote>();
                List<Models.Tbl3pClaimNote> tblCNs;
                tblCNs = db.Tbl3pClaimNote.Where(x => x.SiteCode == sc && x.TpcnDtmAdded >= DateTime.Parse("1/1/2023")).ToList();
                DateTime execDT = DateTime.Now;

                foreach (DataRow r in tbl.Rows)
                {
                    Models.Tbl3pClaimNote claimNote = new Models.Tbl3pClaimNote();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                claimNote.SiteCode = sc;
                                claimNote.LastModAt = execDT;
                                claimNote.RowState = true;
                                break;
                            case "tpcn":
                                claimNote.Tpcn = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "tpcntpcid":
                                claimNote.TpcnTpcid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "tpcndtmadded":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    claimNote.TpcnDtmAdded = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "tpcnstradded":
                                claimNote.TpcnStrAdded = r[c.ColumnName].ToString();
                                break;
                            case "tpcnstrnote":
                                claimNote.TpcnStrNote = r[c.ColumnName].ToString();
                                break;
                            case "tpcnstrtype":
                                claimNote.TpcnStrType = r[c.ColumnName].ToString();
                                break;
                            case "tpcndttickler":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    claimNote.TpcnDtTickler = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "tpcndtticklerremoved":
                                claimNote.TpcnDtTicklerRemoved = r[c.ColumnName].ToString();
                                break;
                            case "tpcnstrticklerremovednote":
                                claimNote.TpcnStrTicklerRemovedNote = r[c.ColumnName].ToString();
                                break;
                            case "tpcnstrticklerremoveduser":
                                claimNote.TpcnStrTicklerRemovedUser = r[c.ColumnName].ToString();
                                break;
                            case "tpcnstrticklertype":
                                claimNote.TpcnStrTicklerType = r[c.ColumnName].ToString();
                                break;
                            case "globalbatchid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    claimNote.GlobalBatchId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "rowchksum":
                                claimNote.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.Tbl3pClaimNote dbclaimNote = tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid);
                    if (dbclaimNote == null)
                    {
                        rc.RowsIns += 1;
                        //db.Tbl3pClaimNote.Add(claimNote);
                        newCNs.Add(claimNote);
                    }
                    else
                    {
                        if (dbclaimNote.RowChkSum != claimNote.RowChkSum)
                        {
                            rc.RowsUpd += 1;
                            dbclaimNote.RowChkSum = claimNote.RowChkSum;
                            dbclaimNote.RowState = claimNote.RowState;
                            dbclaimNote.GlobalBatchId = claimNote.GlobalBatchId;
                            dbclaimNote.LastModAt = claimNote.LastModAt;
                            dbclaimNote.TpcnDtmAdded = claimNote.TpcnDtmAdded;
                            dbclaimNote.TpcnDtTickler = claimNote.TpcnDtTickler;
                            dbclaimNote.TpcnDtTicklerRemoved = claimNote.TpcnDtTicklerRemoved;
                            dbclaimNote.TpcnStrAdded = claimNote.TpcnStrAdded;
                            dbclaimNote.TpcnStrNote = claimNote.TpcnStrNote;
                            dbclaimNote.TpcnStrTicklerRemovedNote = claimNote.TpcnStrTicklerRemovedNote;
                            dbclaimNote.TpcnStrTicklerRemovedUser = claimNote.TpcnStrTicklerRemovedUser;
                            dbclaimNote.TpcnStrTicklerType = claimNote.TpcnStrTicklerType;
                            dbclaimNote.TpcnStrType = claimNote.TpcnStrType;
                            dbclaimNote.TpcnTpcid = claimNote.TpcnTpcid;
                            //db.SaveChanges();
                        }
                    }
                }
                db.SaveChanges();
                if (newCNs.Count > 0)
                {
                    db.Tbl3pClaimNote.AddRange(newCNs);
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
        public Models.RCodes Save3pArnote(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count,
                RowsIns = 0,
                RowsUpd = 0
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.Tbl3pArnote> newARs = new List<Models.Tbl3pArnote>(); 
                List<Models.Tbl3pArnote> tblARs;
                tblARs = db.Tbl3pArnote.Where(x => x.SiteCode == sc && x.ArnDate >= wrkdt.AddDays(-10)).ToList();
                //DateTime.Parse("1/1/2023")).ToList();
                DateTime execDT = DateTime.Now;

                foreach (DataRow r in tbl.Rows)
                {
                    Models.Tbl3pArnote ar = new Models.Tbl3pArnote();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                ar.SiteCode = sc;
                                ar.LastModAt = execDT;
                                ar.RowState = true;
                                break;
                            case "arnid":
                                ar.ArnId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "arnliid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ar.ArnLiid = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "arndate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    ar.ArnDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "arndtremoved":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    ar.ArnDtRemoved = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "arnnote":
                                ar.ArnNote = r[c.ColumnName].ToString();
                                break;
                            case "arnuser":
                                ar.ArnUser = r[c.ColumnName].ToString();
                                break;
                            case "arnstrremovedreason":
                                ar.ArnStrRemovedReason = r[c.ColumnName].ToString();
                                break;
                            case "arnstrremoveduser":
                                ar.ArnStrRemovedUser = r[c.ColumnName].ToString();
                                break;
                            case "bid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ar.Bid = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "arndbnotes":
                                ar.ArnDbnotes = r[c.ColumnName].ToString();
                                break;
                            case "globalbatchid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ar.GlobalBatchId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "rowchksum":
                                ar.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.Tbl3pArnote dbar = tblARs.FirstOrDefault(x => x.ArnId == ar.ArnId);
                    if (dbar == null)
                    {
                        rc.RowsIns += 1;
                        //db.Tbl3pArnote.Add(ar);
                        newARs.Add(ar);
                    }
                    else
                    {
                        if (dbar.RowChkSum != ar.RowChkSum)
                        {
                            rc.RowsUpd += 1;
                            dbar.RowChkSum = ar.RowChkSum;
                            dbar.RowState = ar.RowState;
                            dbar.GlobalBatchId = ar.GlobalBatchId;
                            dbar.LastModAt = ar.LastModAt;
                            dbar.ArnDate = ar.ArnDate;
                            dbar.ArnDbnotes = ar.ArnDbnotes;
                            dbar.ArnDtRemoved = ar.ArnDtRemoved;
                            dbar.ArnLiid = ar.ArnLiid;
                            dbar.ArnNote = ar.ArnNote;
                            dbar.ArnStrRemovedReason = ar.ArnStrRemovedReason;
                            dbar.ArnStrRemovedUser = ar.ArnStrRemovedUser;
                            dbar.ArnUser = ar.ArnUser;
                            dbar.Bid = ar.Bid;
                            //db.SaveChanges();
                        }
                    }
                }
                db.SaveChanges();
                if (newARs.Count > 0)
                {
                    db.Tbl3pArnote.AddRange(newARs);
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
