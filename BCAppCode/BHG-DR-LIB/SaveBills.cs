using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveBills(DataTable tbl, string sc, DateTime wrkdt, int DaysBack, Models.BHG_DRContext db)
        {
            Models.RCodes rcodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (tbl.Rows.Count > 0)
            {
                wrkdt = DateTime.Parse(wrkdt.ToShortDateString());
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    try
                    {
                        bool AllNewRows = false;
                        bool NewRow = false;
                        Models.TblBills bill;
                        List<Models.TblBills> bills;
                        List<Models.TblBills> NewBills = new List<Models.TblBills>();
                        
                        bills = db.TblBills.Where(x => x.SiteCode == sc
                            && x.BillDate.Value.Year >= wrkdt.AddDays(DaysBack).Year
                            && x.BillDate <= wrkdt.AddDays(15)).ToList();
                        
                        foreach (Models.TblBills b in bills)
                        {
                            if (b.RowState)
                            {
                                b.RowState = false;
                                b.LastModAt = DateTime.Now;
                            }
                        }
                        db.SaveChanges();
                        //if (bills == null)
                        //{
                        //    AllNewRows = true;
                        //}
                        foreach (DataRow r in tbl.Rows)
                        {
                            int billid = int.Parse(r["billid"].ToString());
                            int bcltid = 0;
                            if (r["billcltid"].ToString().Length > 0)
                            {
                                bcltid = int.Parse(r["billcltid"].ToString());
                            }
                            int myrcs = int.Parse(r["RowChkSum"].ToString());
                            bill = bills.FirstOrDefault(x => x.SiteCode == sc
                                && x.BillId == billid
                                //&& x.BillCltid == bcltid  //Removed 20230621
                                );
                            if (bill == null)
                            {
                                NewRow = true;
                                bill = new Models.TblBills
                                {
                                    SiteCode = sc,
                                    BillId = billid,
                                    RowChkSum = 0,
                                    BillCltid = bcltid
                                };
                                if (bcltid <= 0)
                                {
                                    bill.RowState = false;
                                }
                                else
                                {
                                    bill.RowState = true;
                                }
                            }
                            //}
                            if (bill.RowChkSum == null) { bill.RowChkSum = 0; }
                            if ((bill.RowChkSum != myrcs) || (NewRow))
                            {
                                if (bcltid <= 0)
                                {
                                    bill.RowState = false;
                                }
                                else
                                {
                                    bill.RowState = true;
                                }
                                bill.RowChkSum = myrcs;
                                try
                                {
                                    if (r["billcltid"].ToString().Length > 0) { bill.BillCltid = int.Parse(r["billcltid"].ToString()); }
                                    if (r["billguestid"].ToString().Length > 0) { bill.BillGuestId = int.Parse(r["billguestid"].ToString()); }
                                    if (r["billdate"].ToString().Length > 7) { bill.BillDate = DateTime.Parse(r["billdate"].ToString()); }
                                    if (r["billbill"].ToString().Length > 0) { bill.BillBill = decimal.Parse(r["billbill"].ToString()); }
                                    if (r["billpay"].ToString().Length > 0) { bill.BillPay = decimal.Parse(r["billpay"].ToString()); }
                                    bill.BillPaytype = r["billpaytype"].ToString();
                                    if (r["billadjust"].ToString().Length > 0) { bill.BillAdjust = decimal.Parse(r["billadjust"].ToString()); }
                                    bill.BillReason = r["billreason"].ToString().Trim();
                                    if (bill.BillReason.Length > 2500) { 
                                        bill.BillReason = bill.BillReason.Substring(0, 2498); }
                                    if (r["billreceiptnum"].ToString().Length > 0) { 
                                        bill.BillReceiptNum = int.Parse(r["billreceiptnum"].ToString()); }
                                    bill.StrUser = r["struser"].ToString();
                                    if (r["blnDeposit"].ToString().Length > 0) { bill.BlnDeposit = bool.Parse(r["blnDeposit"].ToString()); }
                                    if (r["billadjustid"].ToString().Length > 0) { bill.BillAdjustid = int.Parse(r["billadjustid"].ToString()); }
                                    if (r["Fifoallocated"].ToString().Length > 0) { bill.Fifoallocated = bool.Parse(r["Fifoallocated"].ToString()); }
                                    if (r["Fifobalance"].ToString().Length > 0) { bill.Fifobalance = decimal.Parse(r["Fifobalance"].ToString()); }
                                    bill.Costcenter = r["Costcenter"].ToString();
                                    if (r["BillAptId"].ToString().Length > 0) { bill.BillAptId = int.Parse(r["BillAptId"].ToString()); }
                                    if (r["BillOrgdt"].ToString().Length > 0) { bill.BillOrgdt = DateTime.Parse(r["BillOrgdt"].ToString()); }
                                    if (r["BillServId"].ToString().Length > 0) { bill.BillServId = int.Parse(r["BillServId"].ToString()); }
                                    if (r["BillSiteId"].ToString().Length > 0) { bill.BillSiteId = int.Parse(r["BillSiteId"].ToString()); }
                                    if (bill.SiteCode == "PHC") { bill.BillSiteId = 105; }
                                }
                                catch(Exception e)
                                {
                                    foreach(DataColumn g in tbl.Columns)
                                    {
                                        Console.WriteLine(g.ColumnName.ToString() + ": " + r[g.ColumnName].ToString());
                                    }
                                }
                                bill.LastModAt = DateTime.Now;
                                if (NewRow || AllNewRows)
                                {
                                    Models.TblBills bl = db.TblBills.FirstOrDefault(x => x.SiteCode == bill.SiteCode
                                        && x.BillId == bill.BillId);
                                    if (bl == null)
                                    {
                                        db.TblBills.Add(bill);
                                        //bills.Add(bill);
                                    }
                                    //else
                                    //{
                                    //    db.TblBills.Update(bill);
                                    //}
                                    NewRow = false;
                                    //NewBills.Add(bill);
                                    rcodes.RowsIns += 1;
                                }
                                else
                                {
                                    db.TblBills.Update(bill);
                                    rcodes.RowsUpd += 1;
                                }
                            }
                            else
                            {
                                bill.LastModAt = DateTime.Now;
                                if (bcltid <= 0)
                                {
                                    bill.RowState = false;
                                }
                                else
                                {
                                    bill.RowState = true;
                                }
                                rcodes.RowsUpd += 1;
                            }
                        }
                        db.SaveChanges();
                        if (NewBills.Count > 0)
                        {
                            //dbi = new Models.BHG_DRContext();
                            foreach(Models.TblBills nb in NewBills)
                            {
                                Models.TblBills dbBill = db.TblBills.FirstOrDefault(x => 
                                x.SiteCode == nb.SiteCode && x.BillId == nb.BillId);
                                if (dbBill == null)
                                {
                                    db.TblBills.Add(nb);
                                }
                                else
                                {
                                    db.TblBills.Update(nb);
                                }
                            }
                            //db.TblBills.AddRange(NewBills);
                            //bills.AddRange(NewBills);
                            db.SaveChanges();
                        }
                    }
                    catch (Exception e)
                    {
                        rcodes.IsResult = false;
                        rcodes.ExceptMsg = e.Message;
                        if (e.InnerException != null)
                        {
                            rcodes.ExceptInnerMsg = e.InnerException.Message;
                        }
                    }
                }
            }
            return rcodes;
        }
        public Models.RCodes SaveBills(string SrcCmd, string SrcCon, string sc, DateTime wrkdt, bool yearly)
        {
            Models.RCodes rcodes = new Models.RCodes
            {
                IsResult = true
            };

            Models.BHG_DRContext db = new Models.BHG_DRContext();
            SQLSvrManager ssm = new SQLSvrManager();
            bool AllNewRows = false;
            bool NewRow = false;

            try
            {
                // Get Source Data
                DataTable SrcDt = new DataTable(); ;
                Task stask = Task.Run(() =>
                {
                    SrcDt = ssm.GetTableData("Bills", SrcCmd, SrcCon);
                });
                // Get Azure Data
                Models.TblBills bill;
                List<Models.TblBills> bills = null;
                Task ztask;
                if (yearly)
                {
                    ztask = Task.Run(() =>
                    {
                        bills = db.TblBills.Where(x => x.SiteCode == sc
                        //&& x.BillDate >= wrkdt.AddMonths(-1) && x.BillDate <= wrkdt.AddDays(31)
                        ).ToList();
                    });
                }
                else
                {
                    ztask = Task.Run(() =>
                    {
                        bills = db.TblBills.Where(x => x.SiteCode == sc
                        && x.BillDate >= wrkdt.AddMonths(-1) && x.BillDate <= wrkdt.AddDays(31)).ToList();
                    });
                }
                ztask.Wait();
                if (bills == null)
                {
                    AllNewRows = true;
                }
                stask.Wait();
                rcodes.RowsProcessed = SrcDt.Rows.Count;
                foreach (DataRow r in SrcDt.Rows)
                {
                    int billid = int.Parse(r["billid"].ToString());
                    int myrcs = int.Parse(r["RowChkSum"].ToString());
                    if (AllNewRows)
                    {
                        NewRow = true;
                        bill = new Models.TblBills
                        {
                            SiteCode = sc,
                            BillId = billid,
                            RowChkSum = myrcs
                        };
                    }
                    else
                    {
                        bill = bills.Where(x => x.BillId == billid).FirstOrDefault();
                        if (bill == null)
                        {
                            NewRow = true;
                            bill = new Models.TblBills
                            {
                                SiteCode = sc,
                                BillId = billid,
                                RowChkSum = myrcs
                            };
                        }
                    }
                    if (bill.RowChkSum == null) { bill.RowChkSum = myrcs; }
                    if ((bill.RowChkSum != myrcs) || (NewRow))
                    {
                        foreach (DataColumn c in SrcDt.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "rowchksum":
                                    bill.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    break;
                                case "billcltid":
                                    if (r["billcltid"].ToString().Length > 0)
                                    { bill.BillCltid = int.Parse(r["billcltid"].ToString()); }
                                    break;
                                case "billguestid":
                                    if (r["billguestid"].ToString().Length > 0)
                                    { bill.BillGuestId = int.Parse(r["billguestid"].ToString()); }
                                    break;
                                case "billdate":
                                    if (r["billdate"].ToString().Length > 7)
                                    { bill.BillDate = DateTime.Parse(r["billdate"].ToString()); }
                                    break;
                                case "billbill":
                                    if (r["billbill"].ToString().Length > 0)
                                    { bill.BillBill = decimal.Parse(r["billbill"].ToString()); }
                                    break;
                                case "billpay":
                                    if (r["billpay"].ToString().Length > 0)
                                    { bill.BillPay = decimal.Parse(r["billpay"].ToString()); }
                                    break;
                                case "billpaytype":
                                    bill.BillPaytype = r["billpaytype"].ToString();
                                    break;
                                case "billadjust":
                                    if (r["billadjust"].ToString().Length > 0)
                                    { bill.BillAdjust = decimal.Parse(r["billadjust"].ToString()); }
                                    break;
                                case "billreason":
                                    bill.BillReason = r["billreason"].ToString().Trim();
                                    if (bill.BillReason.Length > 2500) { bill.BillReason = bill.BillReason.Substring(0, 2498); }
                                    break;
                                case "billreceiptnum":
                                    if (r["billreceiptnum"].ToString().Length > 0)
                                    { bill.BillReceiptNum = int.Parse(r["billreceiptnum"].ToString()); }
                                    break;
                                case "struser":
                                    bill.StrUser = r["struser"].ToString();
                                    break;
                                case "blndeposit":
                                    if (r["blnDeposit"].ToString().Length > 0)
                                    { bill.BlnDeposit = bool.Parse(r["blnDeposit"].ToString()); }
                                    break;
                                case "billadjustid":
                                    if (r["billadjustid"].ToString().Length > 0) 
                                    { bill.BillAdjustid = int.Parse(r["billadjustid"].ToString()); }
                                    break;
                                case "fifoallocated":
                                    if (r["Fifoallocated"].ToString().Length > 0) 
                                    { bill.Fifoallocated = bool.Parse(r["Fifoallocated"].ToString()); }
                                    break;
                                case "fifobalance":
                                    if (r["Fifobalance"].ToString().Length > 0) 
                                    { bill.Fifobalance = decimal.Parse(r["Fifobalance"].ToString()); }
                                    break;
                                case "costcenter":
                                    bill.Costcenter = r["Costcenter"].ToString();
                                    break;
                                case "billaptid":
                                    if (r["BillAptId"].ToString().Length > 0) 
                                    { bill.BillAptId = int.Parse(r["BillAptId"].ToString()); }
                                    break;
                                case "billorgdt":
                                    if (r["BillOrgdt"].ToString().Length > 0) 
                                    { bill.BillOrgdt = DateTime.Parse(r["BillOrgdt"].ToString()); }
                                    break;
                                case "billservid":
                                    if (r["BillServId"].ToString().Length > 0) 
                                    { bill.BillServId = int.Parse(r["BillServId"].ToString()); }
                                    break;
                                case "billsiteid":
                                    if (r["BillSiteId"].ToString().Length > 0) 
                                    { bill.BillSiteId = int.Parse(r["BillSiteId"].ToString()); }
                                    break;
                                case "lastmodat":
                                    bill.LastModAt = DateTime.Now;
                                    break;
                            }
                        }
                        if (NewRow || AllNewRows)
                        {
                            db.TblBills.Add(bill);
                            NewRow = false;
                        }
                    }
                    db.TblBills.Update(bill);
                }
                db.SaveChanges();
            }
            catch(Exception e)
            {
                rcodes.IsResult = false;
                rcodes.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rcodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rcodes;
        }

        public Models.RCodes SaveAuthBills(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rcode = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                if (tbl.Rows.Count > 0)
                {
                    //List<Models.TblVw3pbill> newpbs = new List<Models.TblVw3pbill>();
                    List<Models.TblVw3pbill> pbills = db.TblVw3pbill.Where(x => x.SiteCode == sc).ToList();
                    foreach (var pb in pbills)
                    {
                        if ((bool)pb.RowState)
                        {
                            pb.RowState = false;
                        }
                    }
                    foreach (DataRow dr in tbl.Rows)
                    {
                        Models.TblVw3pbill pb = new Models.TblVw3pbill();
                        foreach (DataColumn c in tbl.Columns)
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    pb.SiteCode = dr[c.ColumnName].ToString();
                                    break;
                                case "descript":
                                    pb.Descript = dr[c.ColumnName].ToString();
                                    break;
                                case "billdatecriteria":
                                    if (dr[c.ColumnName].ToString().Length > 6)
                                    {
                                        pb.Billdatecriteria = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "paydefaultsubmit":
                                    pb.PayDefaultsubmit = dr[c.ColumnName].ToString();
                                    break;
                                case "scruberror":
                                    pb.ScrubError = dr[c.ColumnName].ToString();
                                    break;
                                case "dsid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        pb.DsId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dsclt":
                                    pb.DsClt = int.Parse(dr[c.ColumnName].ToString());
                                    if (pb.DsClt < 0) { pb.RowState = false; } else { pb.RowState = true; }
                                    break;
                                case "dstxtsrv":
                                    pb.DsTxtSrv = dr[c.ColumnName].ToString();
                                    break;
                                case "dsdtstart":
                                    if (dr[c.ColumnName].ToString().Length > 6)
                                    {
                                        pb.DsDtStart = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dsdtend":
                                    if (dr[c.ColumnName].ToString().Length > 6)
                                    {
                                        pb.DsDtEnd = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dstxttype":
                                    pb.DsTxtType = dr[c.ColumnName].ToString();
                                    break;
                                case "dsdblunits":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        pb.DsdblUnits = double.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "billunits":
                                    pb.BillUnits = double.Parse(dr[c.ColumnName].ToString());
                                    break;
                                case "dstxtstaff":
                                    pb.DstxtStaff = dr[c.ColumnName].ToString();
                                    break;
                                case "npi":
                                    pb.Npi = dr[c.ColumnName].ToString();
                                    break;
                                case "dsbilled":
                                    if (dr[c.ColumnName].ToString().Length > 6)
                                    {
                                        pb.Dsbilled = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "pypayerid":
                                    pb.PyPayerid = dr[c.ColumnName].ToString();
                                    break;
                                case "pysubsid":
                                    pb.PySubsid = dr[c.ColumnName].ToString();
                                    break;
                                case "pygroup":
                                    pb.PyGroup = dr[c.ColumnName].ToString();
                                    break;
                                case "cptcode":
                                    pb.Cptcode = dr[c.ColumnName].ToString();
                                    break;
                                case "charge":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        pb.Charge = double.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "tpaauthcode":
                                    pb.TpaAuthCode = dr[c.ColumnName].ToString();
                                    break;
                                case "clientname":
                                    pb.Clientname = dr[c.ColumnName].ToString();
                                    break;
                                case "cltdob":
                                    if (dr[c.ColumnName].ToString().Length > 6)
                                    {
                                        pb.CltDob = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "cltgender":
                                    pb.CltGender = dr[c.ColumnName].ToString();
                                    break;
                                case "cltadd1":
                                    pb.CltAdd1 = dr[c.ColumnName].ToString();
                                    break;
                                case "cltcity":
                                    pb.CltCity = dr[c.ColumnName].ToString();
                                    break;
                                case "cltstate":
                                    pb.CltState = dr[c.ColumnName].ToString();
                                    break;
                                case "cltzip":
                                    pb.Cltzip = dr[c.ColumnName].ToString();
                                    break;
                                case "cltphone":
                                    pb.CltPhone = dr[c.ColumnName].ToString();
                                    break;
                                case "cltmarry":
                                    pb.CltMarry = dr[c.ColumnName].ToString();
                                    break;
                                case "cltm4id":
                                    pb.CltM4id = dr[c.ColumnName].ToString();
                                    break;
                                case "dsdiag":
                                    pb.Dsdiag = dr[c.ColumnName].ToString();
                                    break;
                                case "modifier":
                                    pb.Modifier = dr[c.ColumnName].ToString();
                                    break;
                                case "dspos":
                                    pb.DsPos = dr[c.ColumnName].ToString();
                                    break;
                                case "ndc":
                                    pb.Ndc = dr[c.ColumnName].ToString();
                                    break;
                                case "mg":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        pb.Mg = double.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "siteid":
                                    if (pb.SiteCode == "PHC") { pb.SiteId = 105; }
                                    else
                                    {
                                        if (dr[c.ColumnName].ToString().Length > 0)
                                        {
                                            pb.SiteId = int.Parse(dr[c.ColumnName].ToString());
                                        }
                                    }
                                    break;
                                case "dsarea":
                                    pb.Dsarea = dr[c.ColumnName].ToString();
                                    break;
                                case "payclass":
                                    pb.Payclass = dr[c.ColumnName].ToString();
                                    break;
                            }
                        }
                        Models.TblVw3pbill pbx = pbills.FirstOrDefault(x => x.DsId == pb.DsId);
                        if (pbx != null)
                        {
                            pbx.Billdatecriteria = pb.Billdatecriteria;
                            pbx.BillUnits = pb.BillUnits;
                            pbx.Charge = pb.Charge;
                            pbx.Clientname = pb.Clientname;
                            pbx.CltAdd1 = pb.CltAdd1;
                            pbx.CltCity = pb.CltCity;
                            pbx.CltDob = pb.CltDob;
                            pbx.CltGender = pb.CltGender;
                            pbx.CltM4id = pb.CltM4id;
                            pbx.CltMarry = pb.CltMarry;
                            pbx.CltPhone = pb.CltPhone;
                            pbx.CltState = pb.CltState;
                            pbx.Cltzip = pb.Cltzip;
                            pbx.Cptcode = pb.Cptcode;
                            pbx.Descript = pb.Descript;
                            pbx.Dsarea = pb.Dsarea;
                            pbx.Dsbilled = pb.Dsbilled;
                            pbx.DsClt = pb.DsClt;
                            pbx.DsdblUnits = pb.DsdblUnits;
                            pbx.Dsdiag = pb.Dsdiag;
                            pbx.DsDtEnd = pb.DsDtEnd;
                            pbx.DsDtStart = pb.DsDtStart;
                            pbx.DsPos = pb.DsPos;
                            pbx.DsTxtSrv = pb.DsTxtSrv;
                            pbx.DstxtStaff = pb.DstxtStaff;
                            pbx.DsTxtType = pb.DsTxtType;
                            pbx.LastModAt = DateTime.Now;
                            pbx.Mg = pb.Mg;
                            pbx.Modifier = pb.Modifier;
                            pbx.Ndc = pb.Ndc;
                            pbx.Npi = pb.Npi;
                            pbx.Payclass = pb.Payclass;
                            pbx.PayDefaultsubmit = pb.PayDefaultsubmit;
                            pbx.PyGroup = pb.PyGroup;
                            pbx.PyPayerid = pb.PyPayerid;
                            pbx.PySubsid = pb.PySubsid;
                            pbx.RowState = pb.RowState;
                            pbx.ScrubError = pb.ScrubError;
                            pbx.SiteId = pb.SiteId;
                            pbx.TpaAuthCode = pb.TpaAuthCode;
                            rcode.RowsUpd++;
                        }
                        else
                        {
                            pb.LastModAt = DateTime.Now;
                            //newpbs.Add(pb);
                            pbills.Add(pb);
                            rcode.RowsIns++;
                        }
                    }
                    db.SaveChanges();
                    //if (newpbs.Count > 0)
                    //{
                    //    db.TblVw3pbill.AddRange(newpbs);
                    //    db.SaveChanges();
                    //}
                }
            }
            catch(Exception e)
            {
                rcode.IsResult = false;
                rcode.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rcode.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rcode;
        }
    }
}
