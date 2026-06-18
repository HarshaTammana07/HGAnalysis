using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SavePayerClient(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                bool AllNewRows = false;
                bool NewRow = false;
                List<Models.TblPayerClient> PCNew = new List<Models.TblPayerClient>();
                Models.TblPayerClient pc;
                List<Models.TblPayerClient> payerClients;
                if (yearly)
                {
                    payerClients = db.TblPayerClient.Where(x => x.SiteCode == sc
                    //&& x.Pcid >= 1
                    //&& (x.PyAddDate == null || x.PyAddDate == wrkdt)
                    ).ToList();
                }
                else
                {
                    payerClients = db.TblPayerClient.Where(x => x.SiteCode == sc
                        //&& x.Pcid >= 1
                        //&& (x.PyAddDate == null || x.PyAddDate == wrkdt)
                        ).ToList();
                }
                if (payerClients.Count == 0) { AllNewRows = true; }
                foreach (DataRow r in tbl.Rows)
                {
                    int pyid = int.Parse(r["pyid"].ToString());
                    int myrcs = int.Parse(r["RowChkSum"].ToString());
                    string pyadd = r["pyadd"].ToString();
                    //DateTime pystart = DateTime.Today;
                    //if (r["PyStart"].ToString().Length > 7) { pystart = DateTime.Parse(r["PyStart"].ToString()); }
                    int cltid = 0;
                    if (r["pycltid"].ToString().Length > 0)
                    {
                        cltid = int.Parse(r["PyCltid"].ToString());
                    }
                    if (AllNewRows)
                    {
                        NewRow = true;
                        pc = new Models.TblPayerClient
                        {
                            PyId = pyid,
                            SiteCode = sc,
                            RowChkSum = myrcs,
                            PyCltid = cltid,
                            Pyadd = pyadd
                        };
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        pc = payerClients.Where(x => x.PyId == pyid
                           && Math.Abs(x.PyCltid) == Math.Abs(cltid)
                           //&& x.PyStart.Date == pystart.Date
                           //&& x.Pyadd == pyadd
                           ).FirstOrDefault();
                        if (pc == null)
                        {
                            NewRow = true;
                            pc = new Models.TblPayerClient
                            {
                                PyId = pyid,
                                SiteCode = sc,
                                RowChkSum = myrcs,
                                PyCltid = cltid,
                                //PyStart = pystart, 
                                Pyadd = pyadd
                            };
                            rc.RowsIns += 1;
                        }
                        else
                        {
                            NewRow = false;
                            rc.RowsUpd += 1;
                        }
                    }
                    if (1 == 1)
                    //if ((pc.RowChkSum == myrcs) || (pc.RowChkSum != myrcs) || (NewRow))
                    {
                        foreach (DataColumn dc in tbl.Columns)
                        {
                            switch (dc.ColumnName.ToLower())
                            {
                                case "rowchksum":
                                    pc.RowChkSum = int.Parse(r["RowChkSum"].ToString());
                                    break;
                                case "pypayerid":
                                    pc.PyPayerid = r["PyPayerid"].ToString();
                                    break;
                                case "pypayertype":
                                    pc.PyPayertype = r["PyPayertype"].ToString();
                                    break;
                                case "pysubsid":
                                    pc.PySubsid = r["PySubsid"].ToString();
                                    break;
                                case "pygroup":
                                    pc.PyGroup = r["PyGroup"].ToString();
                                    break;
                                case "pyauth":
                                    pc.PyAuth = r["PyAuth"].ToString();
                                    break;
                                case "pystart":
                                    if (r["PyStart"].ToString().Length > 7) { pc.PyStart = DateTime.Parse(r["PyStart"].ToString()); }
                                    break;
                                case "pyend":
                                    if (r["PyEnd"].ToString().Length > 7)
                                    { pc.PyEnd = DateTime.Parse(r["PyEnd"].ToString()); }
                                    else { pc.PyEnd = null; }
                                    break;
                                case "pycltid":
                                    //if (r["PyCltid"].ToString().Length > 0) 
                                    //{ pc.PyCltid = int.Parse(r["PyCltid"].ToString()); }
                                    pc.PyCltid = cltid;
                                    break;
                                case "pyactive":
                                    if (r["PyActive"].ToString().Length > 0) { pc.PyActive = bool.Parse(r["PyActive"].ToString()); }
                                    break;
                                case "pyadd":
                                    pc.Pyadd = r["pyadd"].ToString();
                                    break;
                                case "pycity":
                                    pc.Pycity = r["Pycity"].ToString();
                                    break;
                                case "pydob":
                                    if (r["PyDob"].ToString().Length > 7) { pc.PyDob = DateTime.Parse(r["PyDob"].ToString()); }
                                    break;
                                case "pyfirst":
                                    pc.Pyfirst = r["Pyfirst"].ToString();
                                    break;
                                case "pylast":
                                    pc.Pylast = r["Pylast"].ToString();
                                    break;
                                case "pyphone":
                                    pc.PyPhone = r["PyPhone"].ToString();
                                    break;
                                case "pysame":
                                    if (r["Pysame"].ToString().Length > 0) { pc.Pysame = bool.Parse(r["Pysame"].ToString()); }
                                    break;
                                case "pystate":
                                    pc.Pystate = r["Pystate"].ToString();
                                    break;
                                case "pyzip":
                                    pc.Pyzip = r["Pyzip"].ToString();
                                    break;
                                case "pyadddate":
                                case "payadddate":
                                    if (r[dc.ColumnName].ToString().Length > 7)
                                    { pc.PyAddDate = DateTime.Parse(r[dc.ColumnName].ToString()); }
                                    else { pc.PyAddDate = null; }
                                    break;
                                case "pyadduser":
                                    pc.PyAddUser = r["PyAddUser"].ToString();
                                    break;
                                case "pyback":
                                    pc.PyBack = r["PyBack"].ToString();
                                    break;
                                case "pybupe":
                                    pc.Pybupe = r["Pybupe"].ToString();
                                    break;
                                case "pycoins":
                                    if (r["Pycoins"].ToString().Length > 0) { pc.Pycoins = decimal.Parse(r["Pycoins"].ToString()); }
                                    break;
                                case "pycopay":
                                    if (r["Pycopay"].ToString().Length > 0) { pc.Pycopay = decimal.Parse(r["Pycopay"].ToString()); }
                                    break;
                                case "pyded":
                                    pc.Pyded = r["Pyded"].ToString();
                                    break;
                                case "pydeduct":
                                    if (r["Pydeduct"].ToString().Length > 0) { pc.Pydeduct = decimal.Parse(r["Pydeduct"].ToString()); }
                                    break;
                                case "pydeductleft":
                                    if (r["Pydeductleft"].ToString().Length > 0) { pc.Pydeductleft = decimal.Parse(r["Pydeductleft"].ToString()); }
                                    break;
                                case "pyeligcheck":
                                    if (r["PyEligCheck"].ToString().Length > 7) { pc.PyEligCheck = DateTime.Parse(r["PyEligCheck"].ToString()); }
                                    break;
                                case "pyeliguser":
                                    pc.PyEligUser = r["PyEligUser"].ToString();
                                    break;
                                case "pyfront":
                                    pc.Pyfront = r["Pyfront"].ToString();
                                    break;
                                case "pymmt":
                                    pc.Pymmt = r["Pymmt"].ToString();
                                    break;
                                case "pyout":
                                    pc.Pyout = r["Pyout"].ToString();
                                    break;
                                case "pyprojectedend":
                                    if (r["PyProjectedEnd"].ToString().Length > 7)
                                    { pc.PyProjectedEnd = DateTime.Parse(r["PyProjectedEnd"].ToString()); }
                                    else { pc.PyProjectedEnd = null; }
                                    break;
                                case "tempsavepayer":
                                    pc.TempSavePayer = r["TempSavePayer"].ToString();
                                    break;
                                case "pybasicnum":
                                    pc.PyBasicNum = r["PyBasicNum"].ToString();
                                    break;
                                case "pycategory":
                                    pc.PyCategory = r["PyCategory"].ToString();
                                    break;
                                case "pyhmoprovider":
                                    pc.PyHmoprovider = r["PyHmoprovider"].ToString();
                                    break;
                                case "pylocaloffice":
                                    pc.PyLocalOffice = r["PyLocalOffice"].ToString();
                                    break;
                                case "pydbnotes":
                                    pc.PyDbnotes = r["PyDbnotes"].ToString();
                                    break;
                            }
                        }
                        pc.LastModAt = DateTime.Now;
                        if (NewRow || AllNewRows)
                        {
                            //payerClients.Add(pc);
                            PCNew.Add(pc);
                            NewRow = false;
                        }
                        else
                        {
                            //db.TblPayerClient.UpdateRange(payerClients);
                            db.TblPayerClient.Update(pc);
                        }
                    }
                }
                db.SaveChanges();
                if (PCNew.Count > 0)
                {
                    db.TblPayerClient.AddRange(PCNew);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
        public Models.RCodes RemovePayerClients(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                List<Models.TblPayerClient> pcs = db.TblPayerClient.Where(x => x.SiteCode == sc).ToList();

                foreach(DataRow r in tbl.Rows)
                {
                    long id = long.Parse(r["pyID"].ToString());
                    int cltid = 0;
                    if (r["pycltid"].ToString().Length > 0)
                    {
                        cltid = int.Parse(r["PyCltid"].ToString());
                    }
                    DateTime dtadd = DateTime.Today;
                    if (r["PyAddDate"].ToString().Length > 7)
                    {
                        dtadd = DateTime.Parse(r["PyAddDate"].ToString());
                    }
                    Models.TblPayerClient pc = pcs.FirstOrDefault(x => x.PyId == id
                        && Math.Abs(x.PyCltid) == Math.Abs(cltid)
                        //&& x.PyAddDate.Value.Date == dtadd.Date
                        );
                    if (pc != null)
                    {
                        pc.PyActive = false;
                        pc.LastModAt = DateTime.Now;
                        db.TblPayerClient.Update(pc);
                    }
                }
                db.SaveChanges();
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }

            return rc;
        }
        public Models.RCodes SavePayerCltHistory(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                List<Models.TblPayerCltHistory> PCHs = db.TblPayerCltHistory.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblPayerCltHistory> PCHNew = new List<Models.TblPayerCltHistory>();
                List<Models.TblPayerCltHistory> PCHUpd = new List<Models.TblPayerCltHistory>();
                foreach (DataRow r in tbl.Rows)
                {
                    int pchid = int.Parse(r["pchid"].ToString());
                    Models.TblPayerCltHistory pch = PCHs.FirstOrDefault(x => x.PchId == pchid);
                    if (pch == null)
                    {
                        pch = new Models.TblPayerCltHistory();
                        pch.SiteCode = sc;
                        pch.PchId = pchid;
                        pch.PyId = int.Parse(r["pyid"].ToString());
                        pch.PyChange = r["pychange"].ToString();
                        pch.PyDtm = DateTime.Parse(r["pydtm"].ToString());
                        pch.PyUser = r["pyuser"].ToString();
                        pch.PyNote = r["pynote"].ToString();
                        PCHNew.Add(pch);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        pch.PyId = int.Parse(r["pyid"].ToString());
                        pch.PyChange = r["pychange"].ToString();
                        pch.PyDtm = DateTime.Parse(r["pydtm"].ToString());
                        pch.PyUser = r["pyuser"].ToString();
                        pch.PyNote = r["pynote"].ToString();
                        PCHUpd.Add(pch);
                        rc.RowsUpd += 1;
                    }
                }
                if (PCHUpd.Count > 0)
                {
                    //db.TblPayerCltHistory.UpdateRange(PCHUpd);
                    db.SaveChanges();
                }
                if (PCHNew.Count > 0)
                {
                    db.TblPayerCltHistory.AddRange(PCHNew);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rc.IsResult = false;
                rc.ExceptMsg = e.Message;
                Console.WriteLine(e.Message);
                if (e.InnerException != null)
                {
                    rc.ExceptInnerMsg = e.InnerException.Message;
                    Console.WriteLine(e.InnerException.Message);
                }
            }
            return rc;
        }
    }
}
