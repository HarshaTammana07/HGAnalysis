using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveFormQuestionAnswers(DataTable tbl, string sc, DateTime wrkdt, List<Models.TblForms2Process> f2p, Models.BHG_DRContext db)
        {
            Models.RCodes rCodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                
                List<Models.TblDboFormQuestionAnswers> fqas = db.TblDboFormQuestionAnswers.Where(x => x.SiteCode == sc).ToList();
                foreach (var d in fqas)
                {
                    string formname = d.FormName;
                    if (d.FormName.StartsWith("TP-"))
                    {
                        formname = "Treatment Plan";
                    }
                    Models.TblForms2Process xf = f2p.FirstOrDefault(x => x.FormName == formname);
                    if (xf != null)
                    {
                        if (xf.DateFilterEnabled)
                        {
                            if (d.UpdatedOn.HasValue)
                            {
                                if (d.CreatedOn.HasValue)
                                {
                                    if (((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.UpdatedOn.Value.Date >= wrkdt.Date)) && (d.RowState == 1))
                                    {
                                        d.RowState = 0;
                                    }
                                }
                            }
                            else
                            {
                                if (d.CreatedOn.HasValue)
                                {
                                    if ((d.CreatedOn.Value.Date >= wrkdt.Date) && (d.RowState == 1))
                                    {
                                        d.RowState = 0;
                                    }
                                }
                            }
                        }
                        else { d.RowState = 0; }
                    }
                    else
                    {
                        if (d.UpdatedOn.HasValue)
                        {
                            if (((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.UpdatedOn.Value.Date >= wrkdt.Date)) && (d.RowState == 1))
                            {
                                d.RowState = 0;
                            }
                        }
                        else
                        {
                            if ((d.CreatedOn.Value.Date >= wrkdt.Date) && (d.RowState == 1))
                            {
                                d.RowState = 0;
                            }
                        }
                    }
                }
                //db.SaveChanges();
                List<Models.TblDboFormQuestionAnswers> newfqas = new List<Models.TblDboFormQuestionAnswers>();
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblDboFormQuestionAnswers fqa = new Models.TblDboFormQuestionAnswers();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                fqa.SiteCode = sc;
                                fqa.LastModAt = DateTime.Now;
                                break;
                            case "formname":
                                fqa.FormName = r[c.ColumnName].ToString();
                                break;
                            case "formid":
                                fqa.FormId = r[c.ColumnName].ToString().ToUpper();
                                break;
                            case "clientid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.ClientId = int.Parse(r[c.ColumnName].ToString());
                                    if (fqa.ClientId < 0) { fqa.RowState = 0; } else { fqa.RowState = 1; }
                                }
                                else { fqa.ClientId = 0; }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.CreatedBy = r[c.ColumnName].ToString();
                                }
                                break;
                            case "updatedon":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.UpdatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "updatedby":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.UpdatedBy = r[c.ColumnName].ToString();
                                }
                                break;
                            case "preadmissionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.PreAdmissionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                else
                                { fqa.PreAdmissionId = -1; }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString() == "1")
                                    {
                                        fqa.IsDeleted = true;
                                        fqa.RowState = 0;
                                    }
                                    else
                                    {
                                        fqa.RowState = 1;
                                    }
                                }
                                else
                                {
                                    fqa.RowState = 1;
                                }
                                break;
                            case "ischildform":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString() == "1")
                                    {
                                        fqa.IsChildForm = true;
                                    }
                                    else
                                    {
                                        fqa.IsChildForm = false;
                                    }
                                }
                                break;
                            case "questionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.QuestionId = int.Parse(r[c.ColumnName].ToString());
                                }
                                else { fqa.QuestionId = 0; }
                                break;
                            case "questionorderid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.QuestionOrderId = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "questiontext":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.QuestionText = r[c.ColumnName].ToString();
                                }
                                break;
                            case "optionid":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.OptionId = r[c.ColumnName].ToString();
                                }
                                break;
                            case "answervalue":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    fqa.AnswerValue = r[c.ColumnName].ToString();
                                }
                                break;
                        }
                    }
                    //Models.TblDboFormQuestionAnswers qax = db.TblDboFormQuestionAnswers.Where(x => x.SiteCode == fqa.SiteCode
                    Models.TblDboFormQuestionAnswers qax = fqas.Where(x => x.SiteCode == fqa.SiteCode
                        && x.FormName == fqa.FormName
                        && x.FormId.ToUpper() == fqa.FormId.ToUpper()
                        && x.ClientId == fqa.ClientId
                        && x.PreAdmissionId == fqa.PreAdmissionId
                        && x.QuestionId == fqa.QuestionId
                        && x.QuestionOrderId == fqa.QuestionOrderId).FirstOrDefault();
                    if (qax == null)
                    {
                        newfqas.Add(fqa);
                        //db.TblDboFormQuestionAnswers.Add(fqa);
                        rCodes.RowsIns += 1;
                        //db.SaveChanges();
                    }
                    else
                    {
                        qax.PreAdmissionId = fqa.PreAdmissionId;
                        qax.CreatedBy = fqa.CreatedBy;
                        qax.CreatedOn = fqa.CreatedOn;
                        qax.UpdatedBy = fqa.UpdatedBy;
                        qax.UpdatedOn = fqa.UpdatedOn;
                        qax.AnswerValue = fqa.AnswerValue;
                        qax.OptionId = fqa.OptionId;
                        qax.LastModAt = fqa.LastModAt;
                        qax.RowState = fqa.RowState;
                        rCodes.RowsUpd += 1;
                        //db.SaveChanges();
                        //if (sc == "NC")
                        //{
                        //    db.SaveChanges();
                        //}
                    }
                }
                db.SaveChanges();
                if (newfqas.Count > 0)
                {
                    db.TblDboFormQuestionAnswers.AddRange(newfqas);
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rCodes.IsResult = false;
                rCodes.ExceptMsg = e.Message.ToString();
                if (e.InnerException != null)
                {
                    rCodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }
            return rCodes;
        }

        public Models.RCodes SaveAnswerSignatures(DataTable tbl, string sc, DateTime wrkdt, List<Models.TblForms2Process> f2p, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count };

            try 
            { 
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblDboFormAnswerSignatures> Ans = db.TblDboFormAnswerSignatures.Where(x => x.SiteCode == sc).ToList();
                foreach (var d in Ans)
                {
                    string formname = d.FormName;
                    if (d.FormName.StartsWith("TP-"))
                    {
                        formname = "Treatment Plan";
                    }
                    Models.TblForms2Process xf = f2p.FirstOrDefault(x => x.FormName == formname);
                    if (xf != null)
                    {
                        try
                        {
                            if (xf.DateFilterEnabled)
                            {
                                if (d.UpdatedOn.HasValue)
                                {
                                    if (d.CreatedOn.HasValue)
                                    {
                                        if (((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.UpdatedOn.Value.Date >= wrkdt.Date)) && (d.RowState == 1))
                                        {
                                            d.RowState = 0;
                                        }
                                    }
                                }
                                else
                                {
                                    if (d.CreatedOn.HasValue)
                                    {
                                        if ((d.CreatedOn.Value.Date >= wrkdt.Date) && (d.RowState == 1))
                                        {
                                            d.RowState = 0;
                                        }
                                    }
                                }
                            }
                            else { d.RowState = 0; }
                        }
                        catch (Exception e)
                        { }
                    }
                    else
                    {
                        try
                        {
                            if (d.UpdatedOn.HasValue)
                            {
                                if (((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.UpdatedOn.Value.Date >= wrkdt.Date)) && (d.RowState == 1))
                                {
                                    d.RowState = 0;
                                }
                            }
                            else
                            {
                                if ((d.CreatedOn.Value.Date >= wrkdt.Date) && (d.RowState == 1))
                                {
                                    d.RowState = 0;
                                }
                            }
                        }
                        catch(Exception ex)
                        { }
                    }
                }
                db.SaveChanges();
                List<Models.TblDboFormAnswerSignatures> newAns = new List<Models.TblDboFormAnswerSignatures>();
                foreach (DataRow r in tbl.Rows)
                {
                    Models.TblDboFormAnswerSignatures a = new Models.TblDboFormAnswerSignatures();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                a.SiteCode = r[c.ColumnName].ToString();
                                a.RowState = 1;
                                a.LastModAt = DateTime.Now;
                                break;
                            case "formname":
                                a.FormName = r[c.ColumnName].ToString();
                                break;
                            case "formid":
                                a.FormId = r[c.ColumnName].ToString().ToUpper();
                                break;
                            case "clientid":
                                a.ClientId = Math.Abs(int.Parse(r[c.ColumnName].ToString()));
                                if (int.Parse(r[c.ColumnName].ToString()) < 0)
                                {
                                    a.RowState = 0;
                                }
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "updatedon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.UpdatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "completedbysignaturesignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.CompletedBySignatureSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "counselorsignaturesignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.CounselorSignatureSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "doctorsignaturesignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.DoctorSignatureSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "medicalprovidersignaturesignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.MedicalProviderSignatureSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "patientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.PatientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "providersignaturesignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.ProviderSignatureSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "requestorsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.RequestorSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "supervisorsignaturesignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    a.SupervisorSignatureSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "rowchksum":
                                a.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString() == "1")
                                    { a.RowState = 0; }
                                    else
                                    {
                                        if (a.ClientId < 0)
                                        { a.RowState = 0; }
                                        else
                                        { a.RowState = 1; }
                                    }
                                }
                                break;
                        }
                    }
                    Models.TblDboFormAnswerSignatures dbAns = Ans.Where(x => x.SiteCode == a.SiteCode
                            && x.FormName == a.FormName
                            && x.FormId.ToUpper() == a.FormId.ToUpper()
                            && x.ClientId == a.ClientId).FirstOrDefault();
                    if (dbAns == null)
                    {
                        newAns.Add(a);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        //if (dbAns.RowChkSum != a.RowChkSum)
                        {
                            dbAns.RowChkSum = a.RowChkSum;
                            dbAns.CreatedOn = a.CreatedOn;
                            dbAns.UpdatedOn = a.UpdatedOn;
                            dbAns.CompletedBySignatureSignatureDate = a.CompletedBySignatureSignatureDate;
                            dbAns.CounselorSignatureSignatureDate = a.CounselorSignatureSignatureDate;
                            dbAns.LastModAt = a.LastModAt;
                            dbAns.RowState = a.RowState;
                            dbAns.DoctorSignatureSignatureDate = a.DoctorSignatureSignatureDate;
                            dbAns.MedicalProviderSignatureSignatureDate = a.MedicalProviderSignatureSignatureDate;
                            dbAns.PatientSignatureDate = a.PatientSignatureDate;
                            dbAns.ProviderSignatureSignatureDate = a.ProviderSignatureSignatureDate;
                            dbAns.RequestorSignatureDate = a.RequestorSignatureDate;
                            dbAns.StaffSignatureDate = a.StaffSignatureDate;
                            dbAns.SupervisorSignatureSignatureDate = a.SupervisorSignatureSignatureDate;
                            rc.RowsUpd += 1;
                        }
                    }
                }
                db.SaveChanges();
                if (newAns.Count > 0)
                {
                    db.TblDboFormAnswerSignatures.AddRange(newAns);
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
        public Models.RCodes SaveEMFormMDM(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count };
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblEandMformMdm> EMs = db.TblEandMformMdm.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblEandMformMdm> NewEMs = new List<Models.TblEandMformMdm>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblEandMformMdm nEM = new Models.TblEandMformMdm();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                nEM.SiteCode = dr[c.ColumnName].ToString();
                                break;
                            case "id":
                                nEM.Id = int.Parse(dr[c.ColumnName].ToString());
                                break;
                            case "preadmissionid":
                                nEM.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.ClientId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.DataFormId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                nEM.CreatedBy = dr[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.ModifiedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                nEM.ModifiedBy = dr[c.ColumnName].ToString();
                                break;
                            case "isdeleted":
                                if (dr[c.ColumnName].ToString() == "1")
                                { nEM.Isdeleted = true; }
                                else { nEM.Isdeleted = false; }
                                break;
                            case "formdate":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.FormDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "serviceid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.ServiceId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "context":
                                nEM.Context = dr[c.ColumnName].ToString();
                                break;
                            case "version":
                                nEM.Version = dr[c.ColumnName].ToString();
                                break;
                            case "medicalprovidersignaturedate":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.MedicalProviderSignatureDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "medicalprovidersignatureby":
                                nEM.MedicalProviderSignatureBy = dr[c.ColumnName].ToString();
                                break;
                        }
                    }
                    Models.TblEandMformMdm dbem = EMs.FirstOrDefault(x => x.Id == nEM.Id);
                    if (dbem == null)
                    {
                        NewEMs.Add(nEM);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        dbem.ClientId = nEM.ClientId;
                        dbem.Context = nEM.Context;
                        dbem.CreatedBy = nEM.CreatedBy;
                        dbem.CreatedOn = nEM.CreatedOn;
                        dbem.DataFormId = nEM.DataFormId;
                        dbem.FormDate = nEM.FormDate;
                        dbem.Isdeleted = nEM.Isdeleted;
                        dbem.MedicalProviderSignatureBy = nEM.MedicalProviderSignatureBy;
                        dbem.MedicalProviderSignatureDate = nEM.MedicalProviderSignatureDate;
                        dbem.ModifiedBy = nEM.ModifiedBy;
                        dbem.ModifiedOn = nEM.ModifiedOn;
                        dbem.PreAdmissionId = nEM.PreAdmissionId;
                        dbem.ServiceId = nEM.ServiceId;
                        dbem.Version = nEM.Version;
                        rc.RowsUpd += 1;
                    }
                }
                db.SaveChanges();
                if (NewEMs.Count > 0)
                {
                    db.TblEandMformMdm.AddRange(NewEMs);
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
        public Models.RCodes SaveEMFormPregnancy(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count };
            try
            {
                DateTime runDate = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblEandMformPregnancy> EMs = db.TblEandMformPregnancy.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblEandMformPregnancy> NewEMs = new List<Models.TblEandMformPregnancy>();
                foreach (DataRow dr in tbl.Rows)
                {
                    Models.TblEandMformPregnancy nEM = new Models.TblEandMformPregnancy();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        switch (c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                nEM.SiteCode = dr[c.ColumnName].ToString();
                                nEM.LastModAt = runDate;
                                break;
                            case "eandmformid":
                                nEM.EandMformId = int.Parse(dr[c.ColumnName].ToString());
                                break;
                            case "preadmissionid":
                                nEM.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                break;
                            case "clientid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.ClientId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dataformid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.DataFormId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "createdon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                nEM.CreatedBy = dr[c.ColumnName].ToString();
                                break;
                            case "modifiedon":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.ModifiedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "modifiedby":
                                nEM.ModifiedBy = dr[c.ColumnName].ToString();
                                break;
                            case "isdeleted":
                                if (dr[c.ColumnName].ToString() == "1")
                                { nEM.Isdeleted = true; }
                                else { nEM.Isdeleted = false; }
                                break;
                            case "formdate":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.FormDate = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "serviceid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.ServiceId = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "context":
                                nEM.Context = dr[c.ColumnName].ToString();
                                break;
                            case "version":
                                nEM.Version = dr[c.ColumnName].ToString();
                                break;
                            case "ddltrimester":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.Ddltrimester = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "dosetxt":
                                nEM.DoseTxt = dr[c.ColumnName].ToString();
                                break;
                            case "mgtxt":
                                nEM.MgTxt = dr[c.ColumnName].ToString();
                                break;
                            case "dosestabilitytxt":
                                nEM.DoseStabilityTxt = dr[c.ColumnName].ToString();
                                break;
                            case "signstxt":
                                nEM.SignsTxt = dr[c.ColumnName].ToString();
                                break;
                            case "bleeding":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.Bleeding = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "contraction":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.Contraction = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "nauseavomiting":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.NauseaVomiting = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "pregnancyothertxt":
                                nEM.PregnancyOtherTxt = dr[c.ColumnName].ToString();
                                break;
                            case "medicationstxt":
                                nEM.MedicationsTxt = dr[c.ColumnName].ToString();
                                break;
                            case "prenatalvitiamstxt":
                                nEM.PrenatalVitaminsTxt = dr[c.ColumnName].ToString();
                                break;
                            case "allergiestxt":
                                nEM.AllergiesTxt = dr[c.ColumnName].ToString();
                                break;
                            case "changesinroutine":
                                nEM.ChangesInRoutineTxt = dr[c.ColumnName].ToString();
                                break;
                            case "udsradiobtn":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.UdsradioBtn = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "smokerradiobtn":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.SmokerRadioBtn = int.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "illicitdrugtxt":
                                nEM.IllicitDrugTxt = dr[c.ColumnName].ToString();
                                break;
                            case "noofpregnanciestxt":
                                nEM.NoOfPregnanciesTxt = dr[c.ColumnName].ToString();
                                break;
                            case "deliveriestxt":
                                nEM.DeliveriesTxt = dr[c.ColumnName].ToString();
                                break;
                            case "dateoflastob":
                                if (dr[c.ColumnName].ToString().Length > 6)
                                {
                                    nEM.DateOfLastOb = DateTime.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "nameofobtxt":
                                nEM.NameofObtxt = dr[c.ColumnName].ToString();
                                break;
                            case "pregnancycommentstxt":
                                nEM.PregnancyCommentsTxt = dr[c.ColumnName].ToString();
                                break;
                            case "wttxt":
                                nEM.Wttxt = dr[c.ColumnName].ToString();
                                break;
                            case "gravidatxt":
                                nEM.GravidaTxt = dr[c.ColumnName].ToString();
                                break;
                            case "paratxt":
                                nEM.ParaTxt = dr[c.ColumnName].ToString();
                                break;
                            case "provider":
                                nEM.Provider = dr[c.ColumnName].ToString();
                                break;
                            case "prenatalcare":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.PrenatalCare = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "reviewedandacknowledged":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.ReviewedandAcknowledged = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                            case "napregnancygrid":
                                if (dr[c.ColumnName].ToString().Length > 0)
                                {
                                    nEM.NapregnancyGrid = bool.Parse(dr[c.ColumnName].ToString());
                                }
                                break;
                        }
                    }
                    Models.TblEandMformPregnancy dbem = EMs.FirstOrDefault(x => x.EandMformId == nEM.EandMformId);
                    if (dbem == null)
                    {
                        NewEMs.Add(nEM);
                        rc.RowsIns += 1;
                    }
                    else
                    {
                        dbem.ClientId = nEM.ClientId;
                        dbem.Context = nEM.Context;
                        dbem.CreatedBy = nEM.CreatedBy;
                        dbem.CreatedOn = nEM.CreatedOn;
                        dbem.DataFormId = nEM.DataFormId;
                        dbem.FormDate = nEM.FormDate;
                        dbem.Isdeleted = nEM.Isdeleted;
                        dbem.Ddltrimester = nEM.Ddltrimester;
                        dbem.DoseTxt = nEM.DoseTxt;
                        dbem.DoseStabilityTxt = nEM.DoseStabilityTxt;
                        dbem.MgTxt = nEM.MgTxt;
                        dbem.AllergiesTxt = nEM.AllergiesTxt;
                        dbem.Bleeding = nEM.Bleeding;
                        dbem.ChangesInRoutineTxt = nEM.ChangesInRoutineTxt;
                        dbem.Contraction = nEM.Contraction;
                        dbem.DateOfLastOb = nEM.DateOfLastOb;
                        dbem.DeliveriesTxt = nEM.DeliveriesTxt;
                        dbem.GravidaTxt = nEM.GravidaTxt;
                        dbem.IllicitDrugTxt = nEM.IllicitDrugTxt;
                        dbem.LastModAt = nEM.LastModAt;
                        dbem.MedicationsTxt = nEM.MedicationsTxt;
                        dbem.NameofObtxt = nEM.NameofObtxt;
                        dbem.NapregnancyGrid = nEM.NapregnancyGrid;
                        dbem.NauseaVomiting = nEM.NauseaVomiting;
                        dbem.NoOfPregnanciesTxt = nEM.NoOfPregnanciesTxt;
                        dbem.ParaTxt = nEM.ParaTxt;
                        dbem.PregnancyCommentsTxt = nEM.PregnancyCommentsTxt;
                        dbem.PregnancyOtherTxt = nEM.PregnancyOtherTxt;
                        dbem.PrenatalCare = nEM.PrenatalCare;
                        dbem.PrenatalVitaminsTxt = nEM.PrenatalVitaminsTxt;
                        dbem.Provider = nEM.Provider;
                        dbem.ReviewedandAcknowledged = nEM.ReviewedandAcknowledged;
                        dbem.SignsTxt = nEM.SignsTxt;
                        dbem.SmokerRadioBtn = nEM.SmokerRadioBtn;
                        dbem.UdsradioBtn = nEM.UdsradioBtn;
                        dbem.Wttxt = nEM.Wttxt;
                        dbem.ModifiedBy = nEM.ModifiedBy;
                        dbem.ModifiedOn = nEM.ModifiedOn;
                        dbem.PreAdmissionId = nEM.PreAdmissionId;
                        dbem.ServiceId = nEM.ServiceId;
                        dbem.Version = nEM.Version;
                        rc.RowsUpd += 1;
                    }
                }
                db.SaveChanges();
                if (NewEMs.Count > 0)
                {
                    db.TblEandMformPregnancy.AddRange(NewEMs);
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

        public Models.RCodes SaveComprehensiveAssessmentForm (DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count };
            try
            {
                DateTime runDate = DateTime.Now;
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblComprehensiveAssessmentForm> forms = db.TblComprehensiveAssessmentForms.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblComprehensiveAssessmentForm> xforms = new List<Models.TblComprehensiveAssessmentForm>();
                foreach(DataRow dr in tbl.Rows)
                {
                    Models.TblComprehensiveAssessmentForm ca = new Models.TblComprehensiveAssessmentForm();
                    foreach (DataColumn c in tbl.Columns)
                    {
                        try
                        {
                            switch (c.ColumnName.ToLower())
                            {
                                case "sitecode":
                                    ca.SiteCode = dr[c.ColumnName].ToString();
                                    ca.LastModAt = runDate;
                                    ca.RowChkSum = int.Parse(dr["rowchksum"].ToString());
                                    ca.RowState = true;
                                    break;
                                case "id":
                                    ca.Id = int.Parse(dr[c.ColumnName].ToString());
                                    break;
                                case "affectedyouremployment":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AffectedYourEmployment = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "alwaysfollowssafersexpracices":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AlwaysFollowsSaferSexPracices = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "anydifficultycopingwithtrauma":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.AnyDifficultyCopingWithTrauma = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "captivity":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Captivity = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkclosefriendsonly":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckCloseFriendsOnly = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkcoworkers":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckCoworkers = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkeatingdisorders":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckEatingDisorders = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkeveryone":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckEveryone = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkextendedfamily":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckExtendedFamily = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkfamilydisorder":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckFamilyDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkfather":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckFather = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkfoodovereating":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckFoodOvereating = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkfriends":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckFriends = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkfriendsyourselfrecovery":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckFriendsYourselfRecovery = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkgamblingdisorder":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckGamblingDisorder = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkimmediatefamily":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckImmediateFamily = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkinternetaddiction":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckInternetAddiction = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkloveintimacydependence":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckLoveIntimacyDependence = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmaternalaunt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMaternalAunt = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmaternalcousins":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMaternalCousins = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmaternalgrandfather":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMaternalGrandfather = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmaternalgrandmother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMaternalGrandmother = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmaternaluncle":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMaternalUncle = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmeetings":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMeetings = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkmother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckMother = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checknoone":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckNoOne = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkonline":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckOnline = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpaternalaunt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPaternalAunt = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpaternalcousins":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPaternalCousins = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpaternalgrandfather":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPaternalGrandfather = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpaternalgrandmother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPaternalGrandmother = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpaternaluncle":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPaternalUncle = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpeoplework":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPeopleWork = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkpersonalexperience":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckPersonalExperience = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checksibling":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckSibling = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checksocialmediaaddiction":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckSocialMediaAddiction = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checktactilelyhandson":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckTactilelyHandsOn = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checktalkitthrough":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckTalkItThrough = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkverballyexplainittome":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckVerballyExplainItToMe = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "checkvisuallyshowme":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CheckVisuallyShowMe = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "clientm4id":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientM4Id = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "clientname":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ClientName = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "createdby":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CreatedBy = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "createdon":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CreatedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "culturalpreferencesforyourtreatment":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CulturalPreferencesForYourTreatment = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "currentlyexperiencingabusenglectexploitation":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.CurrentlyExperiencingAbuseNglectExploitation = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "dataformid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DataFormId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlactivesubstanceusers":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLActiveSubstanceUsers = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckfather":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckFather = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckmaternalaunt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckMaternalAunt = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckmaternalcousins":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckMaternalCousins = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckmaternalgrandfather":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckMaternalGrandfather = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckmaternalgrandmother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckMaternalGrandmother = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckmaternaluncle":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckMaternalUncle = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckmother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckMother = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckpaternalaunt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckPaternalAunt = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckpaternalcousins":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckPaternalCousins = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckpaternalgrandfather":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckPaternalGrandfather = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckpaternalgrandmother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckPaternalGrandmother = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcheckpaternaluncle":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckPaternalUncle = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlchecksibling":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCheckSibling = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlcurrentjob":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLCurrentJob = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlemploymentstatus":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLEmploymentStatus = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlgender":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLGender = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlhighestgradecompleted":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLHighestGradeCompleted = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlinfluencedrugs":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLInfluenceDrugs = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddllivewithyou":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLLiveWithYou = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlpreferredlanguage":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLPreferredLanguage = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlrelationshipstatus":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLRelationshipStatus = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlsexualorientation":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLSexualOrientation = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddltermsofgender":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLTermsofGender = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddltypedischarge":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLTypeDischarge = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlwhatbranch":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLWhatBranch = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlwhatbranchtype":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLWhatBranchType = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ddlwhatkindofschoolattend":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.DDLWhatKindOfSchoolAttend = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "experiencedanytraumaabuseneglect":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ExperiencedAnytraumaAbuseNeglect = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "familystruggledwithdrugalcoholproblems":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FamilyStruggledWithDrugAlcoholProblems = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "findsupportyourselfinrecoveryother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.FindSupportYourselfInRecoveryOther = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "havehighschooldiploma":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveHighSchoolDiploma = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "haveyoueverreceivedservices":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HaveYouEverReceivedServices = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "hispanic":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Hispanic = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "howlonghadcurrentjob":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.HowLongHadCurrentJob = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isabuseneglectgrowingup":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsAbuseNeglectGrowingUp = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isarmedforces":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsArmedForces = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isarrested":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsArrested = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "iscareoffamilymembers":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsCareOfFamilyMembers = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ischildsupportpayments":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsChildSupportPayments = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "iscloserelationship":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsCloseRelationship = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "iscounttosupportyou":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsCountToSupportYou = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "iscourtfines":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsCourtFines = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "iscourtorderedchildsupportpayments":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsCourtOrderedChildSupportPayments = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isdeleted":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RowState = ca.IsDeleted = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isdeployoverseas":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsDeployOverseas = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isdrugtreatmentcourt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsDrugTreatmentCourt = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isemploymentsituation":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsEmploymentSituation = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isfeelingtraumatized":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsFeelingTraumatized = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isfriendsrecovery":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsFriendsRecovery = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isfulltimestudent":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsFullTimeStudent = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ishaveanychildren":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsHaveAnyChildren = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isheldbackschool":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsHeldBackSchool = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ishighschooldiplomaged":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsHighSchoolDiplomaGED = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isincarcerated":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsIncarcerated = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "islgbt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsLGBT = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ismainstreamclasses":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsMainstreamClasses = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ismakeyouuncomfortable":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsMakeYouUncomfortable = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isopencourtcases":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsOpenCourtCases = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isopenwarrants":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsOpenWarrants = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isparttimestudent":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsPartTimeStudent = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ispeersupportmeetings":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsPeerSupportMeetings = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "ispertainingbeinglgbt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsPertainingBeingLGBT = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isreadwriteeffectively":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsReadWriteEffectively = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "issafersexpractices":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsSaferSexPractices = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "istrainingactivities":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsTrainingActivities = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isunderstandenglish":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsUnderstandEnglish = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "isveteransadministration":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.IsVeteransAdministration = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "laborexploitation":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.LaborExploitation = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "modifiedby":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ModifiedBy = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "modifiedon":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ModifiedOn = DateTime.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "neglect":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Neglect = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "neglecttraumarelatedyourrace":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NeglectTraumaRelatedYourRace = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "nonhispanic":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NonHispanic = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "notsupportivesexualorientaion":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.NotSupportiveSexualOrientaion = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "obsevationofothers":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ObsevationofOthers = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicalabuse":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PhysicalAbuse = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "physicalabuseviolencecaptivityother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PhysicalAbuseViolenceCaptivityOther = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "preadmissionid":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.PreAdmissionId = int.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "probationorparole":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ProbationorParole = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "raceamericanindian":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceAmericanIndian = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "raceasian":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceAsian = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "raceblack":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceBlack = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "racenativehawaiian":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceNativeHawaiian = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "raceother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceOther = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "raceothertxt":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceOtherTxt = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "racetwoormore":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceTwoorMore = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "racewhite":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.RaceWhite = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "sexualabuse":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SexualAbuse = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "sexualabuseassaultsexualexploitation":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SexualAbuseAssaultSexualExploitation = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "sexualexploitation":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SexualExploitation = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "socialhistoryproblemswithother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SocialHistoryProblemsWithOther = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "substancesaffectedyourlife":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SubstancesAffectedYourLife = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "supportivesexualorientaion":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.SupportiveSexualOrientaion = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "thosewhoarenotcisgender":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.ThoseWhoAreNotcisgender = dr[c.ColumnName].ToString();
                                    }
                                    break;
                                case "traumaother":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TraumaOther = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "traumarelatedtorace":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.TraumaRelatedtoRace = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "verbalabuse":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.VerbalAbuse = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "verbalemotionalfinancialabuse":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.VerbalEmotionalFinancialAbuse = bool.Parse(dr[c.ColumnName].ToString());
                                    }
                                    break;
                                case "version":
                                    if (dr[c.ColumnName].ToString().Length > 0)
                                    {
                                        ca.Version = dr[c.ColumnName].ToString();
                                    }
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(c.ColumnName.ToString());
                        }
                    }
                    Models.TblComprehensiveAssessmentForm dbca = forms.FirstOrDefault(x => x.SiteCode == ca.SiteCode && x.Id == ca.Id);
                    if (dbca == null)
                    {
                        rc.RowsIns += 1;
                        xforms.Add(ca);
                    }
                    else
                    {
                        rc.RowsUpd += 1;
                        dbca.AffectedYourEmployment = ca.AffectedYourEmployment;
                        dbca.AlwaysFollowsSaferSexPracices = ca.AlwaysFollowsSaferSexPracices;
                        dbca.AnyDifficultyCopingWithTrauma = ca.AnyDifficultyCopingWithTrauma;
                        dbca.Captivity = ca.Captivity;
                        dbca.CheckCloseFriendsOnly = ca.CheckCloseFriendsOnly;
                        dbca.CheckCoworkers = ca.CheckCoworkers;
                        dbca.CheckEatingDisorders = ca.CheckEatingDisorders;
                        dbca.CheckEveryone = ca.CheckEveryone;
                        dbca.CheckExtendedFamily = ca.CheckExtendedFamily;
                        dbca.CheckFamilyDisorder = ca.CheckFamilyDisorder;
                        dbca.CheckFather = ca.CheckFather;
                        dbca.CheckFoodOvereating = ca.CheckFoodOvereating;
                        dbca.CheckFriends = ca.CheckFriends;
                        dbca.CheckFriendsYourselfRecovery = ca.CheckFriendsYourselfRecovery;
                        dbca.CheckGamblingDisorder = ca.CheckGamblingDisorder;
                        dbca.CheckImmediateFamily = ca.CheckImmediateFamily;
                        dbca.CheckInternetAddiction = ca.CheckInternetAddiction;
                        dbca.CheckLoveIntimacyDependence = ca.CheckLoveIntimacyDependence;
                        dbca.CheckMaternalAunt = ca.CheckMaternalAunt;
                        dbca.CheckMaternalCousins = ca.CheckMaternalCousins;
                        dbca.CheckMaternalGrandfather = ca.CheckMaternalGrandfather;
                        dbca.CheckMaternalGrandmother = ca.CheckMaternalGrandmother;
                        dbca.CheckMaternalUncle = ca.CheckMaternalUncle;
                        dbca.CheckMeetings = ca.CheckMeetings;
                        dbca.CheckMother = ca.CheckMother;
                        dbca.CheckNoOne = ca.CheckNoOne;
                        dbca.CheckOnline = ca.CheckOnline;
                        dbca.CheckPaternalAunt = ca.CheckPaternalAunt;
                        dbca.CheckPaternalCousins = ca.CheckPaternalCousins;
                        dbca.CheckPaternalGrandfather = ca.CheckPaternalGrandfather;
                        dbca.CheckPaternalGrandmother = ca.CheckPaternalGrandmother;
                        dbca.CheckPaternalUncle = ca.CheckPaternalUncle;
                        dbca.CheckPeopleWork = ca.CheckPeopleWork;
                        dbca.CheckPersonalExperience = ca.CheckPersonalExperience;
                        dbca.CheckSibling = ca.CheckSibling;
                        dbca.CheckSocialMediaAddiction = ca.CheckSocialMediaAddiction;
                        dbca.CheckTactilelyHandsOn = ca.CheckTactilelyHandsOn;
                        dbca.CheckTalkItThrough = ca.CheckTalkItThrough;
                        dbca.CheckVerballyExplainItToMe = ca.CheckVerballyExplainItToMe;
                        dbca.CheckVisuallyShowMe = ca.CheckVisuallyShowMe;
                        dbca.ClientId = ca.ClientId;
                        dbca.ClientM4Id = ca.ClientM4Id;
                        dbca.ClientName = ca.ClientName;
                        dbca.CreatedBy = ca.CreatedBy;
                        dbca.CreatedOn = ca.CreatedOn;
                        dbca.CulturalPreferencesForYourTreatment = ca.CulturalPreferencesForYourTreatment;
                        dbca.CurrentlyExperiencingAbuseNglectExploitation = ca.CurrentlyExperiencingAbuseNglectExploitation;
                        dbca.DataFormId = ca.DataFormId;
                        dbca.DDLActiveSubstanceUsers = ca.DDLActiveSubstanceUsers;
                        dbca.DDLCheckFather = ca.DDLCheckFather;
                        dbca.DDLCheckMaternalAunt = ca.DDLCheckMaternalAunt;
                        dbca.DDLCheckMaternalCousins = ca.DDLCheckMaternalCousins;
                        dbca.DDLCheckMaternalGrandfather = ca.DDLCheckMaternalGrandfather;
                        dbca.DDLCheckMaternalGrandmother = ca.DDLCheckMaternalGrandmother;
                        dbca.DDLCheckMaternalUncle = ca.DDLCheckMaternalUncle;
                        dbca.DDLCheckMother = ca.DDLCheckMother;
                        dbca.DDLCheckPaternalAunt = ca.DDLCheckPaternalAunt;
                        dbca.DDLCheckPaternalCousins = ca.DDLCheckPaternalCousins;
                        dbca.DDLCheckPaternalGrandfather = ca.DDLCheckPaternalGrandfather;
                        dbca.DDLCheckPaternalGrandmother = ca.DDLCheckPaternalGrandmother;
                        dbca.DDLCheckPaternalUncle = ca.DDLCheckPaternalUncle;
                        dbca.DDLCheckSibling = ca.DDLCheckSibling;
                        dbca.DDLCurrentJob = ca.DDLCurrentJob;
                        dbca.DDLEmploymentStatus = ca.DDLEmploymentStatus;
                        dbca.DDLGender = ca.DDLGender;
                        dbca.DDLHighestGradeCompleted = ca.DDLHighestGradeCompleted;
                        dbca.DDLInfluenceDrugs = ca.DDLInfluenceDrugs;
                        dbca.DDLLiveWithYou = ca.DDLLiveWithYou;
                        dbca.DDLPreferredLanguage = ca.DDLPreferredLanguage;
                        dbca.DDLRelationshipStatus = ca.DDLRelationshipStatus;
                        dbca.DDLSexualOrientation = ca.DDLSexualOrientation;
                        dbca.DDLTermsofGender = ca.DDLTermsofGender;
                        dbca.DDLTypeDischarge = ca.DDLTypeDischarge;
                        dbca.DDLWhatBranch = ca.DDLWhatBranch;
                        dbca.DDLWhatBranchType = ca.DDLWhatBranchType;
                        dbca.DDLWhatKindOfSchoolAttend = ca.DDLWhatKindOfSchoolAttend;
                        dbca.ExperiencedAnytraumaAbuseNeglect = ca.ExperiencedAnytraumaAbuseNeglect;
                        dbca.FamilyStruggledWithDrugAlcoholProblems = ca.FamilyStruggledWithDrugAlcoholProblems;
                        dbca.FindSupportYourselfInRecoveryOther = ca.FindSupportYourselfInRecoveryOther;
                        dbca.HaveHighSchoolDiploma = ca.HaveHighSchoolDiploma;
                        dbca.HaveYouEverReceivedServices = ca.HaveYouEverReceivedServices;
                        dbca.Hispanic = ca.Hispanic;
                        dbca.HowLongHadCurrentJob = ca.HowLongHadCurrentJob;
                        dbca.IsAbuseNeglectGrowingUp = ca.IsAbuseNeglectGrowingUp;
                        dbca.IsArmedForces = ca.IsArmedForces;
                        dbca.IsArrested = ca.IsArrested;
                        dbca.IsCareOfFamilyMembers = ca.IsCareOfFamilyMembers;
                        dbca.IsChildSupportPayments = ca.IsChildSupportPayments;
                        dbca.IsCloseRelationship = ca.IsCloseRelationship;
                        dbca.IsCountToSupportYou = ca.IsCountToSupportYou;
                        dbca.IsCourtFines = ca.IsCourtFines;
                        dbca.IsCourtOrderedChildSupportPayments = ca.IsCourtOrderedChildSupportPayments;
                        dbca.IsDeleted = ca.IsDeleted;
                        dbca.IsDeployOverseas = ca.IsDeployOverseas;
                        dbca.IsDrugTreatmentCourt = ca.IsDrugTreatmentCourt;
                        dbca.IsEmploymentSituation = ca.IsEmploymentSituation;
                        dbca.IsFeelingTraumatized = ca.IsFeelingTraumatized;
                        dbca.IsFriendsRecovery = ca.IsFriendsRecovery;
                        dbca.IsFullTimeStudent = ca.IsFullTimeStudent;
                        dbca.IsHaveAnyChildren = ca.IsHaveAnyChildren;
                        dbca.IsHeldBackSchool = ca.IsHeldBackSchool;
                        dbca.IsHighSchoolDiplomaGED = ca.IsHighSchoolDiplomaGED;
                        dbca.IsIncarcerated = ca.IsIncarcerated;
                        dbca.IsLGBT = ca.IsLGBT;
                        dbca.IsMainstreamClasses = ca.IsMainstreamClasses;
                        dbca.IsMakeYouUncomfortable = ca.IsMakeYouUncomfortable;
                        dbca.IsOpenCourtCases = ca.IsOpenCourtCases;
                        dbca.IsOpenWarrants = ca.IsOpenWarrants;
                        dbca.IsPartTimeStudent = ca.IsPartTimeStudent;
                        dbca.IsPeerSupportMeetings = ca.IsPeerSupportMeetings;
                        dbca.IsPertainingBeingLGBT = ca.IsPertainingBeingLGBT;
                        dbca.IsReadWriteEffectively = ca.IsReadWriteEffectively;
                        dbca.IsSaferSexPractices = ca.IsSaferSexPractices;
                        dbca.IsTrainingActivities = ca.IsTrainingActivities;
                        dbca.IsUnderstandEnglish = ca.IsUnderstandEnglish;
                        dbca.IsVeteransAdministration = ca.IsVeteransAdministration;
                        dbca.LaborExploitation = ca.LaborExploitation;
                        dbca.LastModAt = ca.LastModAt;
                        dbca.ModifiedBy = ca.ModifiedBy;
                        dbca.ModifiedOn = ca.ModifiedOn;
                        dbca.Neglect = ca.Neglect;
                        dbca.NeglectTraumaRelatedYourRace = ca.NeglectTraumaRelatedYourRace;
                        dbca.NonHispanic = ca.NonHispanic;
                        dbca.NotSupportiveSexualOrientaion = ca.NotSupportiveSexualOrientaion;
                        dbca.ObsevationofOthers = ca.ObsevationofOthers;
                        dbca.PhysicalAbuse = ca.PhysicalAbuse;
                        dbca.PhysicalAbuseViolenceCaptivityOther = ca.PhysicalAbuseViolenceCaptivityOther;
                        dbca.PreAdmissionId = ca.PreAdmissionId;
                        dbca.ProbationorParole = ca.ProbationorParole;
                        dbca.RaceAmericanIndian = ca.RaceAmericanIndian;
                        dbca.RaceAsian = ca.RaceAsian;
                        dbca.RaceBlack = ca.RaceBlack;
                        dbca.RaceNativeHawaiian = ca.RaceNativeHawaiian;
                        dbca.RaceOther = ca.RaceOther;
                        dbca.RaceOtherTxt = ca.RaceOtherTxt;
                        dbca.RaceTwoorMore = ca.RaceTwoorMore;
                        dbca.RaceWhite = ca.RaceWhite;
                        dbca.RowChkSum = ca.RowChkSum;
                        dbca.RowState = ca.RowState;
                        dbca.SexualAbuse = ca.SexualAbuse;
                        dbca.SexualAbuseAssaultSexualExploitation = ca.SexualAbuseAssaultSexualExploitation;
                        dbca.SexualExploitation = ca.SexualExploitation;
                        dbca.SocialHistoryProblemsWithOther = ca.SocialHistoryProblemsWithOther;
                        dbca.SubstancesAffectedYourLife = ca.SubstancesAffectedYourLife;
                        dbca.SupportiveSexualOrientaion = ca.SupportiveSexualOrientaion;
                        dbca.ThoseWhoAreNotcisgender = ca.ThoseWhoAreNotcisgender;
                        dbca.TraumaOther = ca.TraumaOther;
                        dbca.TraumaRelatedtoRace = ca.TraumaRelatedtoRace;
                        dbca.VerbalAbuse = ca.VerbalAbuse;
                        dbca.VerbalEmotionalFinancialAbuse = ca.VerbalEmotionalFinancialAbuse;
                        dbca.Version = ca.Version;
                    }
                }
                db.SaveChanges();
                if (xforms.Count > 0)
                {
                    db.TblComprehensiveAssessmentForms.AddRange(xforms);
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