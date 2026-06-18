using System;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveBottles (DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblBottle> bottles = db.TblBottle.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblBottle> newbottles = new List<Models.TblBottle>();
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblBottle btl = new Models.TblBottle();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                btl.SiteCode = sc;
                                btl.RowState = true;
                                btl.LastModAt = DateTime.Now;
                                break;
                            case "rowchksum":
                                btl.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "bottleid":
                                btl.BottleId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "deanum":
                                btl.Deanum = r[c.ColumnName].ToString();
                                break;
                            case "lotnumber":
                                btl.LotNumber = r[c.ColumnName].ToString();
                                break;
                            case "dtreceived":
                                btl.DtReceived = DateTime.Parse(r[c.ColumnName].ToString());
                                break;
                            case "liquid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    btl.Liquid = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "bottletype":
                                btl.BottleType = r[c.ColumnName].ToString();
                                break;
                            case "initialamount":
                                btl.InitialAmount = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "dtclosed":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    btl.DtClosed = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "blnclosed":
                                btl.BlnClosed = bool.Parse(r[c.ColumnName].ToString());
                                break;
                            case "struser":
                                btl.StrUser = r[c.ColumnName].ToString();
                                break;
                            case "white":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    btl.White = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "specgrav":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    btl.SpecGrav = decimal.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "weight":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    btl.Weight = decimal.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "color":
                                btl.Color = r[c.ColumnName].ToString();
                                break;
                            case "invgroup":
                                btl.InvGroup = r[c.ColumnName].ToString();
                                break;
                            case "brid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    btl.BrId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "siteid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    btl.SiteId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "manufacturer":
                                btl.Manufacturer = r[c.ColumnName].ToString();
                                break;
                            case "expdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    btl.ExpDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblBottle dbBottle = bottles.FirstOrDefault(x => x.BottleId == btl.BottleId);
                    if (dbBottle == null)
                    {
                        rc.RowsIns += 1;
                        newbottles.Add(btl);
                    }
                    else
                    {
                        if (dbBottle.RowChkSum != btl.RowChkSum)
                        {
                            rc.RowsUpd += 1;
                            dbBottle.BlnClosed = btl.BlnClosed;
                            dbBottle.BottleType = btl.BottleType;
                            dbBottle.BrId = btl.BrId;
                            dbBottle.Color = btl.Color;
                            dbBottle.Deanum = btl.Deanum;
                            dbBottle.DtClosed = btl.DtClosed;
                            dbBottle.DtReceived = btl.DtReceived;
                            dbBottle.ExpDate = btl.ExpDate;
                            dbBottle.InitialAmount = btl.InitialAmount;
                            dbBottle.InvGroup = btl.InvGroup;
                            dbBottle.LastModAt = btl.LastModAt;
                            dbBottle.Liquid = btl.Liquid;
                            dbBottle.LotNumber = btl.LotNumber;
                            dbBottle.Manufacturer = btl.Manufacturer;
                            dbBottle.RowChkSum = btl.RowChkSum;
                            dbBottle.RowState = btl.RowState;
                            dbBottle.SiteId = btl.SiteId;
                            dbBottle.SpecGrav = btl.SpecGrav;
                            dbBottle.StrUser = btl.StrUser;
                            dbBottle.Weight = btl.Weight;
                            dbBottle.White = btl.White;
                        }
                    }
                }
                db.SaveChanges();
                if (newbottles.Count > 0)
                {
                    db.TblBottle.AddRange(newbottles);
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
        public Models.RCodes SaveLiquidlog (DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblLiquidLog> liquidLogs = db.TblLiquidLog.Where(x => x.SiteCode == sc
                    //&& x.Dtm >= wrkdt.AddDays(-15)
                    ).ToList();
                List<Models.TblLiquidLog> newlogs = new List<Models.TblLiquidLog>();
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblLiquidLog lg = new Models.TblLiquidLog();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                lg.SiteCode = sc;
                                lg.LastModAt = DateTime.Now;
                                lg.RowState = true;
                                break;
                            case "liqid":
                                lg.LiqId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "pump":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.Pump = Int16.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "doseid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.DoseId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "btlid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.BtlId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "bkrid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.BkrId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "amt":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString().Trim() == "690122921") 
                                    { lg.Amt = 0; }
                                    else { lg.Amt = decimal.Parse(r[c.ColumnName].ToString()); }
                                }
                                break;
                            case "dtm":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    lg.Dtm = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "desc":
                                lg.Desc = r[c.ColumnName].ToString().Trim();
                                break;
                            case "staff":
                                lg.Staff = r[c.ColumnName].ToString();
                                break;
                            case "bllogonly":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.BlLogOnly = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "blprepack":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.BlPrepack = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "memonew":
                                lg.Memonew = r[c.ColumnName].ToString().Trim();
                                break;
                            case "memo":
                                lg.Memo = r[c.ColumnName].ToString().Trim();
                                break;
                            case "dtrti":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    lg.DtRti = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "acknowledgedate":
                                lg.AcknowledgeDate = r[c.ColumnName].ToString();
                                break;
                            case "acknowledgeuser":
                                lg.AcknowledgeUser = r[c.ColumnName].ToString();
                                break;
                            case "regionaldate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    lg.RegionalDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "regionaluser":
                                lg.RegionalUser = r[c.ColumnName].ToString();
                                break;
                            case "complainceuser":
                                lg.ComplainceUser = r[c.ColumnName].ToString();
                                break;
                            case "compliancedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    lg.ComplianceDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "invgroup":
                                lg.Invgroup = r[c.ColumnName].ToString();
                                break;
                            case "siteid":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    lg.SiteId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "rowchksum":
                                lg.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.TblLiquidLog dbll = liquidLogs.FirstOrDefault(x => x.LiqId == lg.LiqId);
                    if (dbll == null)
                    {
                        newlogs.Add(lg);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        if (dbll.RowChkSum != lg.RowChkSum)
                        {
                            rc.RowsUpd += 1;
                            dbll.AcknowledgeDate = lg.AcknowledgeDate;
                            dbll.AcknowledgeUser = lg.AcknowledgeUser;
                            dbll.Amt = lg.Amt;
                            dbll.BkrId = lg.BkrId;
                            dbll.BlLogOnly = lg.BlLogOnly;
                            dbll.BlPrepack = lg.BlPrepack;
                            dbll.BtlId = lg.BtlId;
                            dbll.ComplainceUser = lg.ComplainceUser;
                            dbll.ComplianceDate = lg.ComplianceDate;
                            dbll.Desc = lg.Desc;
                            dbll.DoseId = lg.DoseId;
                            dbll.Dtm = lg.Dtm;
                            dbll.DtRti = lg.DtRti;
                            dbll.Invgroup = lg.Invgroup;
                            dbll.LastModAt = lg.LastModAt;
                            dbll.Memo = lg.Memo;
                            dbll.Memonew = lg.Memonew;
                            dbll.Pump = lg.Pump;
                            dbll.RegionalDate = lg.RegionalDate;
                            dbll.RegionalUser = lg.RegionalUser;
                            dbll.RowChkSum = lg.RowChkSum;
                            dbll.RowState = lg.RowState;
                            dbll.SiteId = lg.SiteId;
                            dbll.Staff = lg.Staff;
                        }
                    }
                }
                db.SaveChanges();
                if (newlogs.Count > 0)
                {
                    db.TblLiquidLog.AddRange(newlogs);
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
        public Models.RCodes SaveInvTypes(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblInvtype> invtypes = db.TblInvtype.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblInvtype> newinv = new List<Models.TblInvtype>();
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblInvtype inv = new Models.TblInvtype();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                inv.SiteCode = sc;
                                inv.RowState = true;
                                inv.LastModAt = DateTime.Now;
                                break;
                            case "invid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.Invid = int.Parse(r[c.ColumnName].ToString());
                                }
                                else { inv.Invid = 0; }
                                break;
                            case "invname":
                                inv.InvName = r[c.ColumnName].ToString();
                                break;
                            case "invliquid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.InvLiquid = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "invunit":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.InvUnit = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "invtotal":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.InvTotal = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "invdivision":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.InvDivision = decimal.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "defaultmed":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.DefaultMed = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "displayname":
                                inv.DisplayName = r[c.ColumnName].ToString();
                                break;
                            case "invmedclass":
                                inv.InvMedclass = r[c.ColumnName].ToString();
                                break;
                            case "isfilm":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.IsFilm = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "type":
                                inv.Type = r[c.ColumnName].ToString();
                                break;
                            case "hasbeaker":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    inv.HasBeaker = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "invactual":
                                inv.InvActual = r[c.ColumnName].ToString();
                                break;
                            case "invlabelname":
                                inv.InvLabelName = r[c.ColumnName].ToString();
                                break;
                            case "invndc":
                                inv.InvNdc = r[c.ColumnName].ToString();
                                break;
                            case "invjcode":
                                inv.InvJcode = r[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblInvtype dbtyp = invtypes.FirstOrDefault(x => x.Invid == inv.Invid);
                    if (dbtyp == null)
                    {
                        newinv.Add(inv);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        if (dbtyp.RowChkSum != inv.RowChkSum)
                        {
                            rc.RowsUpd += 1;
                            dbtyp.DefaultMed = inv.DefaultMed;
                            dbtyp.DisplayName = inv.DisplayName;
                            dbtyp.HasBeaker = inv.HasBeaker;
                            dbtyp.InvActual = inv.InvActual;
                            dbtyp.InvDivision = inv.InvDivision;
                            dbtyp.InvJcode = inv.InvJcode;
                            dbtyp.InvLabelName = inv.InvLabelName;
                            dbtyp.InvLiquid = inv.InvLiquid;
                            dbtyp.InvMedclass = inv.InvMedclass;
                            dbtyp.InvName = inv.InvName;
                            dbtyp.InvNdc = inv.InvNdc;
                            dbtyp.InvTotal = inv.InvTotal;
                            dbtyp.InvUnit = inv.InvUnit;
                            dbtyp.IsFilm = inv.IsFilm;
                            dbtyp.LastModAt = inv.LastModAt;
                            dbtyp.RowChkSum = inv.RowChkSum;
                            dbtyp.RowState = inv.RowState;
                            dbtyp.Type = inv.Type;
                        }
                    }
                }
                db.SaveChanges();
                if (newinv.Count > 0)
                {
                    db.TblInvtype.AddRange(newinv);
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
        public Models.RCodes SaveOrientationCheckList(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblOrientationChecklistNew> OCLs = db.TblOrientationChecklistNew.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblOrientationChecklistNew> newOCL = new List<Models.TblOrientationChecklistNew>();
                DateTime RunDate = DateTime.Now;
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblOrientationChecklistNew ocl = new Models.TblOrientationChecklistNew();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                ocl.SiteCode = sc;
                                ocl.LastModEtl = RunDate;
                                break;
                            case "checklistid":
                            case "id":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.CheckListId = int.Parse(r[c.ColumnName].ToString());
                                }
                                else { ocl.CheckListId = 0; }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                else { ocl.CheckListId = 0; }
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ClientId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.DataFormId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientcomplaints":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PatientComplaints = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "accesstoemergency":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.AccesstoEmergency = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "codeofethics":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.CodeofEthics = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "confidentialitypolicy":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ConfidentialityPolicy = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "methods":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.Methods = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "explanationoffiancialobligations":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ExplanationofFiancialObligations = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "rulesforinvoluntarydetox":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.RulesforInvoluntaryDetox = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "firesafety":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.FireSafety = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "programrulesonpatientparking":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ProgramRulesonPatientParking = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonrestraint":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyonRestraint = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyontobaccoproducts":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyonTobaccoProducts = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonillicit":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyonIllicit = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonweapons":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyonWeapons = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "knowledgeofnames":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.KnowledgeofNames = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "programrules":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ProgramRules = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "aidshivprevention":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.Aidshivprevention = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "hepatitisprevention":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.HepatitisPrevention = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "purposeandprocess":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PurposeandProcess = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "individualtreatmentplan":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.IndividualTreatmentPlan = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyregardingurinedrug":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyRegardingUrineDrug = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dischargetransitioncriteria":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.DischargeTransitionCriteria = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "naturalprogression":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.NaturalProgression = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.CreatedBy = r[c.ColumnName].ToString();
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ModifiedBy = r[c.ColumnName].ToString();
                                }
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString() == "1")
                                { ocl.IsDeleted = true; }
                                else { ocl.IsDeleted = false; }
                                break;
                            case "version":
                            case "versionx":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.Version = r[c.ColumnName].ToString();
                                }
                                break;
                            case "staffsignature":
                                ocl.StaffSignature = r[c.ColumnName].ToString();
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignatureby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.StaffSignatureBy = r[c.ColumnName].ToString();
                                }
                                break;
                            case "patientsignature":
                                ocl.PatientSignature = r[c.ColumnName].ToString();
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignatureby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PatientSignatureBy = r[c.ColumnName].ToString();
                                }
                                break;
                            case "introductionnames":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.IntroductionNames = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "methodspatients":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.MethodsPatients = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "programoperatinghours":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ProgramOperatingHours = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonemergency":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyOnEmergency = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "codeofethicsconduct":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.CodeOfEthicsConduct = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "confidentialitypolicies":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ConfidentialityPolicies = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "explanationoffinancial":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ExplanationOfFinancial = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "firesafetyemergency":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.FireSafetyEmergency = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientparking":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PatientParking = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonseclusion":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyOnSeclusion = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyontobacco":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyOnTobacco = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonillicitpremises":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyOnIllicitPremises = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "purposeandprocessassessment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PurposeandProcessAssessment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "individualtreatment":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.IndividualTreatment = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "integrateddynamic":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.IntegratedDynamic = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyregarding":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyRegarding = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dosinginstructions":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.DosingInstructions = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "factorsconsidered":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.FactorsConsidered = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "safetyinstructions":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.SafetyInstructions = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "guestdosing":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.GuestDosing = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "dischargetransition":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.DischargeTransition = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "policyonbehaviors":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PolicyOnBehaviors = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "potentialrisks":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PotentialRisks = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "naturalprogressionmedication":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.NaturalProgressionMedication = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "overdoseprevention":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.OverdosePrevention = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "aidshivhepatitis":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.AIDSHIVHepatitis = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patienthasviewed":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PatientHasViewed = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientreceived":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.PatientReceived = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "applicablepatient":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.ApplicablePatient = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientrights":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    ocl.Patientrights = bool.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblOrientationChecklistNew dbocl = OCLs.FirstOrDefault(x => x.CheckListId == ocl.CheckListId);
                    if (dbocl == null)
                    {
                        newOCL.Add(ocl);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbocl.LastModEtl = ocl.LastModEtl;
                        dbocl.PreAdmissionId = ocl.PreAdmissionId;
                        dbocl.ClientId = ocl.ClientId;
                        dbocl.DataFormId = ocl.DataFormId;
                        dbocl.PatientComplaints = ocl.PatientComplaints;
                        dbocl.AccesstoEmergency = ocl.AccesstoEmergency;
                        dbocl.CodeofEthics = ocl.CodeofEthics;
                        dbocl.ConfidentialityPolicy = ocl.ConfidentialityPolicy;
                        dbocl.Methods = ocl.Methods;
                        dbocl.ExplanationofFiancialObligations = ocl.ExplanationofFiancialObligations;
                        dbocl.RulesforInvoluntaryDetox = ocl.RulesforInvoluntaryDetox;
                        dbocl.FireSafety = ocl.FireSafety;
                        dbocl.ProgramRulesonPatientParking = ocl.ProgramRulesonPatientParking;
                        dbocl.PolicyonRestraint = ocl.PolicyonRestraint;
                        dbocl.PolicyonTobaccoProducts = ocl.PolicyonTobaccoProducts;
                        dbocl.PolicyonIllicit = ocl.PolicyonIllicit;
                        dbocl.PolicyonWeapons = ocl.PolicyonWeapons;
                        dbocl.KnowledgeofNames = ocl.KnowledgeofNames;
                        dbocl.ProgramRules = ocl.ProgramRules;
                        dbocl.Aidshivprevention = ocl.Aidshivprevention;
                        dbocl.HepatitisPrevention = ocl.HepatitisPrevention;
                        dbocl.PurposeandProcess = ocl.PurposeandProcess;
                        dbocl.IndividualTreatmentPlan = ocl.IndividualTreatmentPlan;
                        dbocl.PolicyRegardingUrineDrug = ocl.PolicyRegardingUrineDrug;
                        dbocl.DischargeTransitionCriteria = ocl.DischargeTransitionCriteria;
                        dbocl.NaturalProgression = ocl.NaturalProgression;
                        dbocl.CreatedBy = ocl.CreatedBy;
                        dbocl.CreatedOn = ocl.CreatedOn;
                        dbocl.ModifiedBy = ocl.ModifiedBy;
                        dbocl.ModifiedOn = ocl.ModifiedOn;
                        dbocl.IsDeleted = ocl.IsDeleted;
                        dbocl.Version = ocl.Version;
                        dbocl.StaffSignatureDate = ocl.StaffSignatureDate;
                        dbocl.StaffSignatureBy = ocl.StaffSignatureBy;
                        dbocl.PatientSignatureDate = ocl.PatientSignatureDate;
                        dbocl.PatientSignatureBy = ocl.PatientSignatureBy;
                        dbocl.IntroductionNames = ocl.IntroductionNames;
                        dbocl.MethodsPatients = ocl.MethodsPatients;
                        dbocl.Patientrights = ocl.Patientrights;
                        dbocl.ProgramOperatingHours = ocl.ProgramOperatingHours;
                        dbocl.PolicyOnEmergency = ocl.PolicyOnEmergency;
                        dbocl.CodeOfEthicsConduct = ocl.CodeOfEthicsConduct;
                        dbocl.ConfidentialityPolicies = ocl.ConfidentialityPolicies;
                        dbocl.ExplanationOfFinancial = ocl.ExplanationOfFinancial;
                        dbocl.FireSafetyEmergency = ocl.FireSafetyEmergency;
                        dbocl.PatientParking = ocl.PatientParking;
                        dbocl.PolicyOnSeclusion = ocl.PolicyOnSeclusion;
                        dbocl.PolicyOnTobacco = ocl.PolicyOnTobacco;
                        dbocl.PolicyOnIllicitPremises = ocl.PolicyOnIllicitPremises;
                        dbocl.PurposeandProcessAssessment = ocl.PurposeandProcessAssessment;
                        dbocl.IndividualTreatment = ocl.IndividualTreatment;
                        dbocl.IntegratedDynamic = ocl.IntegratedDynamic;
                        dbocl.PolicyRegarding = ocl.PolicyRegarding;
                        dbocl.DosingInstructions = ocl.DosingInstructions;
                        dbocl.FactorsConsidered = ocl.FactorsConsidered;
                        dbocl.SafetyInstructions = ocl.SafetyInstructions;
                        dbocl.GuestDosing = ocl.GuestDosing;
                        dbocl.DischargeTransition = ocl.DischargeTransition;
                        dbocl.PolicyOnBehaviors = ocl.PolicyOnBehaviors;
                        dbocl.PotentialRisks = ocl.PotentialRisks;
                        dbocl.NaturalProgressionMedication = ocl.NaturalProgressionMedication;
                        dbocl.OverdosePrevention = ocl.OverdosePrevention;
                        dbocl.AIDSHIVHepatitis = ocl.AIDSHIVHepatitis;
                        dbocl.PatientHasViewed = ocl.PatientHasViewed;
                        dbocl.PatientReceived = ocl.PatientReceived;
                        dbocl.ApplicablePatient = ocl.ApplicablePatient;
                        dbocl.Patientrights = ocl.Patientrights;
                    }
                }
                db.SaveChanges();
                if (newOCL.Count > 0)
                {
                    db.TblOrientationChecklistNew.AddRange(newOCL);
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
