using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveClientDemo1var (DataTable tbl, string sc, int actionkey, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count, 
                RowsIns = 0, RowsUpd = 0
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime starttime = DateTime.Now;
                Models.TblClientDemo1 clt;
                List<Models.TblClientDemo1> clients;
                if (sc.StartsWith("B50"))
                {
                    clients = db.TblClientDemo1.Where(x => x.SiteCode.StartsWith("B50")).ToList();
                    foreach (var s in clients)
                    {
                        if (s.SiteCode == sc)
                        {
                            s.RowState = 0;
                        }
                    }
                }
                else
                {
                    clients = db.TblClientDemo1.Where(x => x.SiteCode == sc).ToList();
                    if (actionkey == 1)
                    {
                        foreach (var s in clients)
                        {
                            s.RowState = 0;
                        }
                    }
                }
                foreach (DataRow r in tbl.Rows)
                {
                    int cid = int.Parse(r["ClientID"].ToString());
                    int rcs = int.Parse(r["RowChkSum"].ToString());
                    clt = clients.Where(x => x.ClientId == cid && x.SiteCode == sc).FirstOrDefault();
                    if (clt == null)
                    {
                        clt = new Models.TblClientDemo1
                        {
                            SiteCode = sc,
                            ClientId = cid, 
                            RowChkSum = 0, 
                            LastModAt = starttime,
                            RowState = 1
                        };
                        db.TblClientDemo1.Add(clt);
                        db.SaveChanges();
                        res.RowsIns++;
                    }
                    if (clt.RowChkSum != rcs)
                    {
                        clt.LastModAt = starttime;
                        clt.RowState = 1;
                        clt.RowChkSum = rcs;
                        clt.SiteCode = sc;
                        foreach (DataColumn c in r.Table.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "clientm4id":
                                    clt.ClientM4id = r[c.ColumnName].ToString();
                                    break;
                                case "firstname":
                                    if (r[c.ColumnName].ToString() != "''")
                                    {
                                        clt.FirstName = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "middlename":
                                    if (r[c.ColumnName].ToString() != "''")
                                    {
                                        clt.MiddleName = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "lastname":
                                    if (r[c.ColumnName].ToString() != "''")
                                    {
                                        clt.LastName = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "suffix":
                                    if (r[c.ColumnName].ToString() != "''")
                                    {
                                        clt.Suffix = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "dob":
                                    if (r[c.ColumnName].ToString().Replace("'", "").Trim().Length > 7)
                                    {
                                        clt.Dob = DateTime.Parse(r[c.ColumnName].ToString().Replace("'", ""));
                                    }
                                    break;
                                case "gender":
                                    if (r[c.ColumnName].ToString() != "''")
                                    {
                                        clt.Gender = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "ssn":
                                    clt.Ssn = r[c.ColumnName].ToString();
                                    break;
                                case "email":
                                    clt.Email = r[c.ColumnName].ToString();
                                    break;
                                case "size":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        clt.Size = int.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "address1":
                                    clt.Address1 = r[c.ColumnName].ToString().Trim();
                                    break;
                                case "address2":
                                    clt.Address2 = r[c.ColumnName].ToString().Trim();
                                    break;
                                case "city":
                                    clt.City = r[c.ColumnName].ToString().Trim();
                                    break;
                                case "state":
                                    clt.State = r[c.ColumnName].ToString();
                                    break;
                                case "zip":
                                    clt.Zip = r[c.ColumnName].ToString();
                                    break;
                                case "phone":
                                    if (r[c.ColumnName].ToString().Length > 24)
                                    {
                                        clt.Phone = r[c.ColumnName].ToString().Substring(24, 0);
                                    }
                                    else
                                    {
                                        clt.Phone = r[c.ColumnName].ToString();
                                    }
                                    break;
                                case "preg":
                                    if (r[c.ColumnName].ToString().Length > 0)
                                    {
                                        clt.Preg = bool.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "pregedc":
                                    if (r[c.ColumnName].ToString().Length > 7)
                                    {
                                        clt.PregEdc = DateTime.Parse(r[c.ColumnName].ToString());
                                    }
                                    break;
                                case "marital":
                                    clt.Marital = r[c.ColumnName].ToString();
                                    break;
                                case "empstatus":
                                    clt.EmpStatus = r[c.ColumnName].ToString();
                                    break;
                                case "employer":
                                    clt.Employer = r[c.ColumnName].ToString();
                                    break;
                                case "workphone":
                                    clt.WorkPhone = r[c.ColumnName].ToString();
                                    break;
                                case "income":
                                    clt.Income = r[c.ColumnName].ToString();
                                    break;
                                case "education":
                                    clt.Education = r[c.ColumnName].ToString();
                                    break;
                                case "hair":
                                    clt.Hair = r[c.ColumnName].ToString();
                                    break;
                                case "eye":
                                    clt.Eye = r[c.ColumnName].ToString();
                                    break;
                                case "height":
                                    clt.Height = r[c.ColumnName].ToString();
                                    break;
                                case "weight":
                                    clt.Weight = r[c.ColumnName].ToString();
                                    break;
                                case "race":
                                    clt.Race = r[c.ColumnName].ToString();
                                    break;
                                case "language":
                                    clt.Language = r[c.ColumnName].ToString();
                                    break;
                                case "county":
                                    clt.County = r[c.ColumnName].ToString();
                                    break;
                            }
                        }
                        res.RowsUpd++;
                    }
                    else
                    {
                        clt.RowState = 1;
                        //clt.LastModAt = DateTime.Now;
                        res.RowsUpd++;
                    }
                }
                db.SaveChanges();
                res.RowsUpd -= res.RowsIns;
            }
            catch(Exception e)
            {
                res.IsResult = false;
                res.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    res.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return res;
        }
        public Models.RCodes SaveClientDemo1(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblClientDemo1 clt;
                    //Console.WriteLine(sc);
                    List<Models.TblClientDemo1> clients = db.TblClientDemo1.Where(x => x.SiteCode == sc).ToList();
                    if (clients == null)
                    {
                        AllNewRows = true;
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int intClient = int.Parse(r["ClientID"].ToString());
                        int myChkSum = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            clt = new Models.TblClientDemo1
                            {
                                SiteCode = sc,
                                ClientId = intClient,
                                RowChkSum = myChkSum
                            };
                        }
                        else
                        {
                            clt = clients.Where(x => x.ClientId == intClient).FirstOrDefault();
                            if (clt == null)
                            {
                                NewRow = true;
                                clt = new Models.TblClientDemo1
                                {
                                    SiteCode = sc,
                                    ClientId = intClient,
                                    RowChkSum = myChkSum
                                };
                            }
                        }
                        if (clt.RowChkSum == null) { clt.RowChkSum = int.Parse(r["RowChkSum"].ToString()); }
                        if ((clt.RowChkSum != myChkSum) || (NewRow))
                        {
                            clt.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                            if (r["ClientM4ID"].ToString() != "''")
                            {
                                clt.ClientM4id = r["ClientM4ID"].ToString();
                            }
                            if (r["Gender"].ToString() != "''")
                            {
                                clt.Gender = r["Gender"].ToString();
                            }
                            if (r["preg"].ToString().Length > 0) { clt.Preg = bool.Parse(r["preg"].ToString()); }
                            clt.City = r["City"].ToString();
                            clt.State = r["State"].ToString();
                            clt.Zip = r["zip"].ToString();
                            clt.EmpStatus = r["EmpStatus"].ToString();
                            clt.Race = r["Race"].ToString();
                            clt.County = r["County"].ToString();
                            clt.Marital = r["Marital"].ToString();
                            clt.LastModAt = DateTime.Now;

                            if (sc.ToLower() != "gb" && sc.ToLower() != "sos" && sc.ToLower() != "pawtucket")
                            {
                                //Columns not in NetAlystic
                                if (r["FirstName"].ToString() != "''")
                                {
                                    clt.FirstName = r["FirstName"].ToString();
                                }
                                if (r["MiddleName"].ToString() != "''")
                                {
                                    clt.MiddleName = r["MiddleName"].ToString();
                                }
                                if (r["LastName"].ToString() != "''")
                                {
                                    clt.LastName = r["LastName"].ToString();
                                }
                                if (r["Suffix"].ToString() != "''")
                                {
                                    clt.Suffix = r["Suffix"].ToString();
                                }
                                if (r["DOB"].ToString().Replace("'", "").Length >= 8)
                                {
                                    //Console.WriteLine(r["DOB"].ToString());
                                    clt.Dob = DateTime.Parse(r["DOB"].ToString().Replace("'", "").Trim());
                                }
                                if (r["SSN"].ToString() != "''")
                                {
                                    clt.Ssn = r["SSN"].ToString();
                                }
                                clt.Email = r["email"].ToString();
                                if (r["Size"].ToString().Length > 0) { clt.Size = int.Parse(r["Size"].ToString()); }
                                clt.Address1 = r["Address1"].ToString().Trim();
                                clt.Address2 = r["Address2"].ToString().Trim();
                                clt.Phone = r["Phone"].ToString().Substring(24, 0);
                                if (r["PregEDC"].ToString().Length > 8) { clt.PregEdc = DateTime.Parse(r["PregEDC"].ToString()); }
                                clt.Employer = r["Employer"].ToString();
                                clt.WorkPhone = r["WorkPhone"].ToString();
                                clt.Income = r["Income"].ToString();
                                clt.Education = r["Education"].ToString();
                                clt.Hair = r["Hair"].ToString();
                                clt.Eye = r["Eye"].ToString();
                                clt.Height = r["Height"].ToString();
                                clt.Weight = r["Weight"].ToString();
                                clt.Language = r["Language"].ToString();
                            }
                            if (NewRow || AllNewRows)
                            {
                                db.TblClientDemo1.Add(clt);
                                clients.Add(clt);
                                NewRow = false;
                            }
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message;
                    Console.WriteLine(e.Message);
                    if (e.InnerException != null)
                    {
                        res.ExceptInnerMsg = e.InnerException.Message;
                        Console.WriteLine(e.InnerException.Message);
                    }
                }
            }
            return res;
        }
        public Models.RCodes SaveClientDemo2(DataTable tbl, string sc, int actionkey, Models.BHG_DRContext db)
        {
            Models.RCodes res = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblClientDemo2 clt;
                    List<Models.TblClientDemo2> clients = db.TblClientDemo2.Where(x => x.SiteCode == sc).ToList();
                    if (actionkey == 1)
                    {
                        foreach (var s in clients)
                        {
                            s.RowState = 0;
                        }
                    }
                    if (clients == null)
                    {
                        AllNewRows = true;
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int intClient = int.Parse(r["ClientID"].ToString());
                        int myrcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            clt = new Models.TblClientDemo2
                            {
                                SiteCode = sc,
                                ClientId = intClient,
                                RowChkSum = myrcs,
                                RowState = 1
                            };
                        }
                        else
                        {
                            clt = clients.Where(x => x.ClientId == intClient).FirstOrDefault();
                            if (clt == null)
                            {
                                NewRow = true;
                                clt = new Models.TblClientDemo2
                                {
                                    SiteCode = sc,
                                    ClientId = intClient,
                                    RowChkSum = myrcs,
                                    RowState = 1
                                };
                            }
                        }
                        if (clt.RowChkSum == null) { clt.RowChkSum = myrcs; }
                        if ((clt.RowChkSum != myrcs) || (NewRow))
                        {
                            foreach(DataColumn dc in tbl.Columns)
                            {
                                switch(dc.ColumnName.ToLower())
                                {
                                    case "rowchksum":
                                        clt.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                        break;
                                    case "counselor":
                                        clt.Counselor = r["Counselor"].ToString();
                                        break;
                                    case "status":
                                        clt.Status = r["Status"].ToString();
                                        break;
                                    case "prog":
                                        clt.Prog = r["prog"].ToString();
                                        break;
                                    case "dateadded":
                                        if (r["DateAdded"].ToString().Length > 8) { clt.DateAdded = DateTime.Parse(r["DateAdded"].ToString()); }
                                        break;
                                    case "amount":
                                        clt.Amount = r["Amount"].ToString();
                                        break;
                                    case "freq":
                                        clt.Freq = r["Freq"].ToString();
                                        break;
                                    case "dow1":
                                        clt.Dow1 = r["Dow1"].ToString();
                                        break;
                                    case "dow2":
                                        clt.Dow2 = r["Dow2"].ToString();
                                        break;
                                    case "nextbill":
                                        if (r["NextBill"].ToString().Length > 8) { clt.NextBill = DateTime.Parse(r["NextBill"].ToString()); }
                                        break;
                                    case "lastbill":
                                        if (r["LastBill"].ToString().Length > 8) { clt.LastBill = DateTime.Parse(r["LastBill"].ToString()); }
                                        break;
                                    case "nexttp":
                                        if (r["NextTP"].ToString().Length > 8) { clt.NextTp = DateTime.Parse(r["NextTP"].ToString()); }
                                        break;
                                    case "phystb":
                                        if (r["PhysTB"].ToString().Length > 8) { clt.PhysTb = DateTime.Parse(r["PhysTB"].ToString()); }
                                        break;
                                    case "bottles":
                                        if (r["Bottles"].ToString().Length > 0) { clt.Bottles = Int16.Parse(r["Bottles"].ToString()); }
                                        break;
                                    case "monthly":
                                        if (r["monthly"].ToString().Length > 0) { clt.Monthly = bool.Parse(r["monthly"].ToString()); }
                                        break;
                                    case "picpath":
                                        clt.Picpath = r["PicPath"].ToString();
                                        break;
                                    case "rin":
                                        clt.Rin = r["RIN"].ToString();
                                        break;
                                    case "eth":
                                        clt.Eth = r["ETH"].ToString();
                                        break;
                                    case "medicaid":
                                        if (r["Medicaid"].ToString().Length > 0) { clt.Medicaid = bool.Parse(r["Medicaid"].ToString()); }
                                        break;
                                    case "enrolldate":
                                        if (r["EnrollDate"].ToString().Length > 8) { clt.EnrollDate = DateTime.Parse(r["EnrollDate"].ToString()); }
                                        break;
                                    case "bulk":
                                        if (r["BULK"].ToString().Length > 0) { clt.Bulk = bool.Parse(r["BULK"].ToString()); }
                                        break;
                                    case "stand":
                                        if (r["Stand"].ToString().Length > 0) { clt.Stand = bool.Parse(r["Stand"].ToString()); }
                                        break;
                                    case "special":
                                        clt.Special = r["Special"].ToString();
                                        break;
                                    case "dtlastua":
                                        clt.DtLastUa = r["dtLastUA"].ToString();
                                        break;
                                    case "amsid":
                                        clt.Amsid = r["Amsid"].ToString();
                                        break;
                                    case "nocensus":
                                        if (r["NOCENSUS"].ToString().Length > 0) { clt.Nocensus = bool.Parse(r["NOCENSUS"].ToString()); }
                                        break;
                                    case "changeuser":
                                        clt.Changeuser = r["Changeuser"].ToString();
                                        break;
                                    case "repoldclient":
                                        if (r["RepoldClient"].ToString().Length > 0) { clt.RepOldClient = decimal.Parse(r["RepoldClient"].ToString()); }
                                        break;
                                    case "uaweekly":
                                        if (r["uaweekly"].ToString().Length > 8) { clt.Uaweekly = DateTime.Parse(r["uaweekly"].ToString()); }
                                        break;
                                    case "optin":
                                        if (r["Optin"].ToString().Length > 0) { clt.OptIn = bool.Parse(r["Optin"].ToString()); }
                                        break;
                                    case "credit":
                                        if (r["credit"].ToString().Length > 0) { clt.Credit = int.Parse(r["credit"].ToString()); }
                                        break;
                                    case "conttxdt":
                                        if (r["conttxdt"].ToString().Length > 8) { clt.Conttxdt = DateTime.Parse(r["conttxdt"].ToString()); }
                                        break;
                                    case "ins":
                                        clt.Ins = r["ins"].ToString();
                                        break;
                                    case "risk":
                                        clt.Risk = r["risk"].ToString();
                                        break;
                                    case "clt3pback":
                                        clt.Clt3pBack = r["clt3pback"].ToString();
                                        break;
                                    case "clt3pfront":
                                        clt.Clt3pfront = r["Clt3pfront"].ToString();
                                        break;
                                    case "biweeklyua":
                                        if (r["biweeklyUA"].ToString().Length > 0) { clt.BiWeeklyUa = bool.Parse(r["biweeklyua"].ToString()); }
                                        break;
                                    case "nursenotes":
                                        clt.NurseNotes = r["NurseNotes"].ToString();
                                        break;
                                    case "panel":
                                        clt.Panel = r["panel"].ToString();
                                        break;
                                    case "payday":
                                        clt.Payday = r["payday"].ToString();
                                        break;
                                    case "fingerprint1":
                                        if (r["FingerPrint1"] != DBNull.Value) { clt.FingerPrint1 = (byte[])r["FingerPrint1"]; }
                                        break;
                                    case "fingerprint2":
                                        if (r["FingerPrint2"] != DBNull.Value) { clt.FingerPrint2 = (byte[])r["FingerPrint2"]; }
                                        break;
                                    case "clt911name":
                                        clt.Clt911Name = r["clt911Name"].ToString();
                                        break;
                                    case "clt911ph":
                                        clt.Clt911Ph = r["clt911ph"].ToString();
                                        break;
                                    case "clt911relation":
                                        clt.Clt911Relation = r["clt911relation"].ToString();
                                        break;
                                    case "salesforceid":
                                        clt.SalesForceId = r["SalesforceId"].ToString();
                                        break;
                                    case "issalesforcesync":
                                        if (r["isSalesForceSync"].ToString().Length > 0) { clt.IsSalesForceSync = int.Parse(r["isSalesForceSync"].ToString()); }
                                        break;
                                    case "holidaypickup":
                                        if (r["HolidayPickup"].ToString().Length > 0) { clt.HolidayPickup = bool.Parse(r["HolidayPickup"].ToString()); }
                                        break;
                                    case "ddapid":
                                        if (r["ddapid"].ToString().Length > 0) { clt.Ddapid = long.Parse(r["ddapid"].ToString()); }
                                        break;
                                    case "provclient":
                                        if (r["ProvClient"].ToString().Length > 0) { clt.ProvClient = long.Parse(r["ProvClient"].ToString()); }
                                        break;
                                    case "provclientid":
                                        if (r["ProvClientID"].ToString().Length > 0) { clt.ProvClientId = long.Parse(r["ProvClientID"].ToString()); }
                                        break;
                                    case "backfee":
                                        if (r["BackFee"].ToString().Length > 0) { clt.BackFee = decimal.Parse(r["BackFee"].ToString()); }
                                        break;
                                    case "remarks":
                                        clt.Remarks = r["Remarks"].ToString();
                                        break;
                                }
                            }
                            clt.LastModAt = DateTime.Now;
                            clt.RowState = 1;
                            if (NewRow || AllNewRows)
                            {
                                db.TblClientDemo2.Add(clt);
                                NewRow = false;
                            }
                        }
                        else
                        {
                            clt.RowState = 1;
                            clt.LastModAt = DateTime.Now;
                        }
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res.IsResult = false;
                    res.ExceptMsg = e.Message;
                    res.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return res;
        }
        public bool SaveClientDemo3(DataTable tbl, string sc)
        {
            bool res = true;
            using (var db = new Models.BHG_DRContext())
            {
                try
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    Models.TblClientDemo2 clt;
                    List<Models.TblClientDemo2> clients = db.TblClientDemo2.Where(x => x.SiteCode == sc).ToList();
                    if (clients == null)
                    {
                        AllNewRows = true;
                    }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int intClient = int.Parse(r["ClientID"].ToString());
                        //int myrcs = int.Parse(r["RowChkSum"].ToString());
                        if (AllNewRows)
                        {
                            NewRow = true;
                            clt = new Models.TblClientDemo2();
                            clt.SiteCode = sc;
                            clt.ClientId = intClient;
                            //clt.RowChkSum = myrcs;
                        }
                        else
                        {
                            clt = clients.Where(x => x.ClientId == intClient).FirstOrDefault();
                            if (clt == null)
                            {
                                NewRow = true;
                                clt = new Models.TblClientDemo2();
                                clt.SiteCode = sc;
                                clt.ClientId = intClient;
                                //clt.RowChkSum = myrcs;
                            }
                        }
                        //if (clt.RowChkSum == null) { clt.RowChkSum = myrcs; }
                        //if ((clt.RowChkSum != myrcs) || (NewRow))
                        //{
                        //    clt.Counselor = r["Counselor"].ToString();
                        //    clt.Status = r["Status"].ToString();
                        //    clt.Prog = r["prog"].ToString();
                        //    if (r["DateAdded"].ToString().Length > 8) { clt.DateAdded = DateTime.Parse(r["DateAdded"].ToString()); }
                        //    clt.Amount = r["Amount"].ToString();
                        //    clt.Freq = r["Freq"].ToString();
                        //    clt.Dow1 = r["Dow1"].ToString();
                        //    clt.Dow2 = r["Dow2"].ToString();
                        //    if (r["NextBill"].ToString().Length > 8) { clt.NextBill = DateTime.Parse(r["NextBill"].ToString()); }
                        //    if (r["LastBill"].ToString().Length > 8) { clt.LastBill = DateTime.Parse(r["LastBill"].ToString()); }
                        //    if (r["NextTP"].ToString().Length > 8) { clt.NextTp = DateTime.Parse(r["NextTP"].ToString()); }
                        //    if (r["PhysTB"].ToString().Length > 8) { clt.PhysTb = DateTime.Parse(r["PhysTB"].ToString()); }
                        //    if (r["Bottles"].ToString().Length > 0) { clt.Bottles = Int16.Parse(r["Bottles"].ToString()); }
                        //    if (r["monthly"].ToString().Length > 0) { clt.Monthly = bool.Parse(r["monthly"].ToString()); }
                        //    clt.Picpath = r["PicPath"].ToString();
                        //    //clt.Remarks = r["Remarks"].ToString();
                        //    clt.Rin = r["RIN"].ToString();
                        //    clt.Eth = r["ETH"].ToString();
                        //    if (r["Medicaid"].ToString().Length > 0) { clt.Medicaid = bool.Parse(r["Medicaid"].ToString()); }
                        //    if (r["EnrollDate"].ToString().Length > 8) { clt.EnrollDate = DateTime.Parse(r["EnrollDate"].ToString()); }
                        //    if (r["BULK"].ToString().Length > 0) { clt.Bulk = bool.Parse(r["BULK"].ToString()); }
                        //    if (r["Stand"].ToString().Length > 0) { clt.Stand = bool.Parse(r["Stand"].ToString()); }
                        //    clt.Special = r["Special"].ToString();
                        //    clt.DtLastUa = r["dtLastUA"].ToString();
                        //    clt.Amsid = r["Amsid"].ToString();
                        //    if (r["NOCENSUS"].ToString().Length > 0) { clt.Nocensus = bool.Parse(r["NOCENSUS"].ToString()); }
                        //    clt.Changeuser = r["Changeuser"].ToString();
                        //    if (r["RepoldClient"].ToString().Length > 0) { clt.RepOldClient = decimal.Parse(r["RepoldClient"].ToString()); }
                        //    if (r["uaweekly"].ToString().Length > 8) { clt.Uaweekly = DateTime.Parse(r["uaweekly"].ToString()); }
                        //    if (r["Optin"].ToString().Length > 0) { clt.OptIn = bool.Parse(r["Optin"].ToString()); }
                        //    if (r["credit"].ToString().Length > 0) { clt.Credit = int.Parse(r["credit"].ToString()); }
                        //    if (r["conttxdt"].ToString().Length > 8) { clt.Conttxdt = DateTime.Parse(r["conttxdt"].ToString()); }
                        //    clt.Ins = r["ins"].ToString();
                        //    clt.Risk = r["risk"].ToString();
                        //    clt.Clt3pBack = r["clt3pback"].ToString();
                        //    clt.Clt3pfront = r["Clt3pfront"].ToString();
                        //    if (r["biweeklyUA"].ToString().Length > 0) { clt.BiWeeklyUa = bool.Parse(r["biweeklyua"].ToString()); }
                        //    clt.NurseNotes = r["NurseNotes"].ToString();
                        //    clt.Panel = r["panel"].ToString();
                        //    clt.Payday = r["payday"].ToString();
                        if (r.ItemArray.Contains("FingerPrint1"))
                        {
                            if (r["FingerPrint1"] != DBNull.Value) { clt.FingerPrint1 = (byte[])r["FingerPrint1"]; }
                            if (r["FingerPrint2"] != DBNull.Value) { clt.FingerPrint2 = (byte[])r["FingerPrint2"]; }
                        }
                        //clt.Clt911Name = r["clt911Name"].ToString();
                        //clt.Clt911Ph = r["clt911ph"].ToString();
                        //clt.Clt911Relation = r["clt911relation"].ToString();
                        //clt.SalesForceId = r["SalesforceId"].ToString();
                        //if (r["isSalesForceSync"].ToString().Length > 0) { clt.IsSalesForceSync = int.Parse(r["isSalesForceSync"].ToString()); }
                        //if (r["HolidayPickup"].ToString().Length > 0) { clt.HolidayPickup = bool.Parse(r["HolidayPickup"].ToString()); }
                        //if (r["ddapid"].ToString().Length > 0) { clt.Ddapid = long.Parse(r["ddapid"].ToString()); }
                        //if (r["ProvClient"].ToString().Length > 0) { clt.ProvClient = long.Parse(r["ProvClient"].ToString()); }
                        //if (r["ProvClientID"].ToString().Length > 0) { clt.ProvClientId = long.Parse(r["ProvClientID"].ToString()); }
                        //if (r["BackFee"].ToString().Length > 0) { clt.BackFee = decimal.Parse(r["BackFee"].ToString()); }

                        if (NewRow || AllNewRows)
                        {
                            db.TblClientDemo2.Add(clt);
                            NewRow = false;
                        }
                        //}
                    }
                    db.SaveChanges();
                }
                catch (Exception e)
                {
                    res = false;
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return res;
        }
    }
}
