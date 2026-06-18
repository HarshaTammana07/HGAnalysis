using BHG_DR_LIB.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public RCodes SaveBamForm(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes res = new RCodes();
            res.IsResult = true;
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime RunDT = DateTime.Now;
                List<Models.TblBamForm> BamForms = db.TblBamForms.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblBamForm> NewBFs = new List<TblBamForm>(); 
                
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblBamForm bf = new TblBamForm();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                bf.SiteCode = sc;
                                break;
                            case "id":
                                bf.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.PreAdmissionId = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.ClientId = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "dataformid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.DataFormId = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "bamdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                { bf.BAMDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "interviewerid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InterviewerID = (r[c.ColumnName].ToString()); }
                                break;
                            case "clinicianinterview":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.ClinicianInterview = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "selfreport":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.SelfReport = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "phone":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.Phone = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "timestarted":
                                if (r[c.ColumnName].ToString().Length > 6)
                                { bf.TimeStarted = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq1":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ1 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq1txt":
                                bf.InstructionsQ1Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq2":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ2 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq2txt":
                                bf.InstructionsQ2Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq3":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ3 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq3txt":
                                bf.InstructionsQ3Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq4":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ4 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq4txt":
                                bf.InstructionsQ4Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq5":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ5 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq5txt":
                                bf.InstructionsQ5Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq6":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ6 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq6txt":
                                bf.InstructionsQ6Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq7a":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7A = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq7b":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7B = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq7c":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7C = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq7d":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7D = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq7e":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7E = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq7f":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7F = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq7g":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ7G = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq8":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ8 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq8txt":
                                bf.InstructionsQ8Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq9":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ9 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq9txt":
                                bf.InstructionsQ9Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq10":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ10 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq10txt":
                                bf.InstructionsQ10Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq11":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ11 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq11txt":
                                bf.InstructionsQ11Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq12":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ12 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq12txt":
                                bf.InstructionsQ12Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq13":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ13 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq13txt":
                                bf.InstructionsQ13Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq14":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ14 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq14txt":
                                bf.InstructionsQ14Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq15":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ15 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq15txt":
                                bf.InstructionsQ15Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq16":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ16 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq16txt":
                                bf.InstructionsQ16Txt = r[c.ColumnName].ToString();
                                break;
                            case "instructionsq17":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.InstructionsQ17 = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "instructionsq17txt":
                                bf.InstructionsQ17Txt = r[c.ColumnName].ToString();
                                break;
                            case "timefinished":
                                if (r[c.ColumnName].ToString().Length > 6)
                                { bf.TimeFinished = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "subscalescoretxt1":
                                bf.SubscaleScoreTxt1 = r[c.ColumnName].ToString();
                                break;
                            case "subscalescoretxt2":
                                bf.SubscaleScoreTxt2 = r[c.ColumnName].ToString();
                                break;
                            case "subscalescoretxt3":
                                bf.SubscaleScoreTxt3 = r[c.ColumnName].ToString();
                                break;
                            case "staffsignature":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.StaffSignature = (r[c.ColumnName].ToString()); }
                                break;
                            case "staffsignatureby":
                                bf.StaffSignatureBy = r[c.ColumnName].ToString();
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "createdby":
                                bf.CreatedBy = r[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "modifiedby": 
                                bf.ModifiedBy = r[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.ModifiedOn = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bf.IsDeleted = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "version":
                                bf.Version = r[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblBamForm dbbf = BamForms.FirstOrDefault(x => x.SiteCode == bf.SiteCode && x.Id == bf.Id);
                    if (dbbf == null)
                    {
                        NewBFs.Add(bf);
                        res.RowsIns += 1;
                    }
                    else
                    {
                        res.RowsUpd += 1;
                        dbbf.BAMDate = bf.BAMDate;
                        dbbf.ClientId = bf.ClientId;
                        dbbf.ClinicianInterview = bf.ClinicianInterview;
                        dbbf.CreatedBy = bf.CreatedBy;
                        dbbf.CreatedOn = bf.CreatedOn;
                        dbbf.DataFormId = bf.DataFormId;
                        dbbf.InstructionsQ1 = bf.InstructionsQ1;
                        dbbf.InstructionsQ10 = bf.InstructionsQ10;
                        dbbf.InstructionsQ10Txt = bf.InstructionsQ10Txt;
                        dbbf.InstructionsQ11 = bf.InstructionsQ11;
                        dbbf.InstructionsQ11Txt = bf.InstructionsQ11Txt;
                        dbbf.InstructionsQ12 = bf.InstructionsQ12;
                        dbbf.InstructionsQ12Txt = bf.InstructionsQ12Txt;
                        dbbf.InstructionsQ13 = bf.InstructionsQ13;
                        dbbf.InstructionsQ13Txt = bf.InstructionsQ13Txt;
                        dbbf.InstructionsQ14 = bf.InstructionsQ14;
                        dbbf.InstructionsQ14Txt = bf.InstructionsQ14Txt;
                        dbbf.InstructionsQ15 = bf.InstructionsQ15;
                        dbbf.InstructionsQ15Txt = bf.InstructionsQ15Txt;
                        dbbf.InstructionsQ16 = bf.InstructionsQ16;
                        dbbf.InstructionsQ16Txt = bf.InstructionsQ16Txt;
                        dbbf.InstructionsQ17 = bf.InstructionsQ17;
                        dbbf.InstructionsQ17Txt = bf.InstructionsQ17Txt;
                        dbbf.InstructionsQ1Txt = bf.InstructionsQ1Txt;
                        dbbf.InstructionsQ2 = bf.InstructionsQ2;
                        dbbf.InstructionsQ2Txt = bf.InstructionsQ2Txt;
                        dbbf.InstructionsQ3 = bf.InstructionsQ3;
                        dbbf.InstructionsQ3Txt = bf.InstructionsQ3Txt;
                        dbbf.InstructionsQ4 = bf.InstructionsQ4;
                        dbbf.InstructionsQ4Txt = bf.InstructionsQ4Txt;
                        dbbf.InstructionsQ5 = bf.InstructionsQ5;
                        dbbf.InstructionsQ5Txt = bf.InstructionsQ5Txt;
                        dbbf.InstructionsQ6 = bf.InstructionsQ6;
                        dbbf.InstructionsQ6Txt = bf.InstructionsQ6Txt;
                        dbbf.InstructionsQ7A = bf.InstructionsQ7A;
                        dbbf.InstructionsQ7B = bf.InstructionsQ7B;
                        dbbf.InstructionsQ7C = bf.InstructionsQ7C;
                        dbbf.InstructionsQ7D = bf.InstructionsQ7D;
                        dbbf.InstructionsQ7E = bf.InstructionsQ7E;
                        dbbf.InstructionsQ7F = bf.InstructionsQ7F;
                        dbbf.InstructionsQ7G = bf.InstructionsQ7G;
                        dbbf.InstructionsQ8 = bf.InstructionsQ8;
                        dbbf.InstructionsQ8Txt = bf.InstructionsQ8Txt;
                        dbbf.InstructionsQ9 = bf.InstructionsQ9;
                        dbbf.InstructionsQ9Txt = bf.InstructionsQ9Txt;
                        dbbf.InterviewerID = bf.InterviewerID;
                        dbbf.IsDeleted = bf.IsDeleted;
                        dbbf.ModifiedBy = bf.ModifiedBy;
                        dbbf.ModifiedOn = bf.ModifiedOn;
                        dbbf.Phone = bf.Phone;
                        dbbf.PreAdmissionId = bf.PreAdmissionId;
                        dbbf.SelfReport = bf.SelfReport;
                        dbbf.StaffSignature = bf.StaffSignature;
                        dbbf.StaffSignatureBy = bf.StaffSignatureBy;
                        dbbf.StaffSignatureDate = bf.StaffSignatureDate;
                        dbbf.SubscaleScoreTxt1 = bf.SubscaleScoreTxt1;
                        dbbf.SubscaleScoreTxt2 = bf.SubscaleScoreTxt2;
                        dbbf.SubscaleScoreTxt3 = bf.SubscaleScoreTxt3;
                        dbbf.TimeFinished = bf.TimeFinished;
                        dbbf.TimeStarted = bf.TimeStarted;
                        dbbf.Version = bf.Version;
                    }
                }
                db.SaveChanges();
                if (NewBFs.Count > 0)
                {
                    db.TblBamForms.AddRange(NewBFs);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message);
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }
        public RCodes SaveBamScore(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes res = new RCodes();
            res.IsResult = true;
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime RunDT = DateTime.Now;
                List<Models.TblBamScore> BamScores = db.TblBamScores.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblBamScore> NewBSs = new List<TblBamScore>();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblBamScore bs = new TblBamScore();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                bs.SiteCode = sc;
                                break;
                            case "id":
                                bs.Id = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bs.ClientId = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "tprid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bs.tprID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "description":
                                if (r[c.ColumnName].ToString().Length > 6)
                                { bs.Description = (r[c.ColumnName].ToString()); }
                                break;
                            case "score":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { bs.Score = (r[c.ColumnName].ToString()); }
                                break;
                            }
                    }
                    Models.TblBamScore dbbs = BamScores.FirstOrDefault(x => x.SiteCode == bs.SiteCode && x.Id == bs.Id);
                    if (dbbs == null)
                    {
                        NewBSs.Add(bs);
                        res.RowsIns += 1;
                    }
                    else
                    {
                        res.RowsUpd += 1;
                        dbbs.ClientId = bs.ClientId;
                        dbbs.tprID = bs.tprID;
                        dbbs.Description = bs.Description;
                        dbbs.Score = bs.Score;
                        }
                }
                db.SaveChanges();
                if (NewBSs.Count > 0)
                {
                    db.TblBamScores.AddRange(NewBSs);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message);
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }
        public RCodes SaveTblDiags(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes res = new RCodes();
            res.IsResult = true;
            if (db == null) { db = new Models.BHG_DRContext(); }
            try
            {
                DateTime RunDT = DateTime.Now;
                List<Models.TblDiag10> TDs = db.TblDiag10s.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblDiag10> NewTDs = new List<TblDiag10>();

                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblDiag10 td = new TblDiag10();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                td.SiteCode = sc;
                                td.LastModAt = RunDT;
                                td.RowState = true;
                                break;
                            case "dgid":
                                td.dgID = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "dgcltid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgCLTID = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "dgdiag":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgDIAG = (r[c.ColumnName].ToString()); }
                                break;
                            case "dgdate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                { td.dgDATE = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "dgdesc":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgDESC = (r[c.ColumnName].ToString()); }
                                break;
                            case "dgstaff":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgSTAFF = (r[c.ColumnName].ToString()); }
                                break;
                            case "dgdt":
                                if (r[c.ColumnName].ToString().Length > 6)
                                { td.dgdt = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "dgprimary":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgPRIMARY = bool.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "dgdiag10":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgDIAG10 = (r[c.ColumnName].ToString()); }
                                break;
                            case "dgdiag10description":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgDIAG10Description = (r[c.ColumnName].ToString()); }
                                break;
                            case "dgnote":
                                td.dgNote = r[c.ColumnName].ToString();
                                break;
                            case "dgtype":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.dgType = (r[c.ColumnName].ToString()); }
                                break;
                            case "enrollmentid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                { td.EnrollmentId = int.Parse(r[c.ColumnName].ToString()); }
                                break;
                            case "dgenddate":
                                if (r[c.ColumnName].ToString().Length > 8)
                                { td.dgEndDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                                break;
                        }
                    }
                    Models.TblDiag10 dbtd = TDs.FirstOrDefault(x => x.SiteCode == td.SiteCode && x.dgID == td.dgID);
                    if (dbtd == null)
                    {
                        NewTDs.Add(td);
                        res.RowsIns += 1;
                    }
                    else
                    {
                        res.RowsUpd += 1;
                        dbtd.dgCLTID = td.dgCLTID;
                        dbtd.dgDIAG = td.dgDIAG;
                        dbtd.dgDESC = td.dgDESC;
                        dbtd.dgDATE = td.dgDATE;
                        dbtd.dgSTAFF = td.dgSTAFF;
                        dbtd.dgdt = td.dgdt;
                        dbtd.dgPRIMARY = td.dgPRIMARY;
                        dbtd.dgDIAG10 = td.dgDIAG10;
                        dbtd.dgDIAG10Description = td.dgDIAG10Description;
                        dbtd.dgNote = td.dgNote;
                        dbtd.dgType = td.dgType;
                        dbtd.EnrollmentId = td.EnrollmentId;
                        dbtd.dgEndDate = td.dgEndDate;
                        dbtd.RowState = td.RowState;
                        dbtd.LastModAt = td.LastModAt;
                    }
                }
                db.SaveChanges();
                if (NewTDs.Count > 0)
                {
                    db.TblDiag10s.AddRange(NewTDs);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                res.IsResult = false;
                Console.WriteLine(e.Message);
                res.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    Console.WriteLine(e.InnerException.Message);
                    res.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return res;
        }
    }
}
