using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveFormQuestionAnswers(DataTable tbl, string sc, DateTime wrkdt, bool yearly, Models.BHG_DRContext db)
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
                    if (d.UpdatedOn.HasValue)
                    {
                        if ((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.UpdatedOn.Value.Date >= wrkdt.Date))
                        {
                            d.RowState = 0;
                        }
                    }
                    else
                    {
                        if ((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.RowState == 1))
                        {
                            d.RowState = 0;
                        }
                    }
                }
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
                                fqa.ClientId = int.Parse(r[c.ColumnName].ToString());
                                if (fqa.ClientId < 0) { fqa.RowState = 0; } else { fqa.RowState = 1; }
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

        public Models.RCodes SaveAnswerSignatures(DataTable tbl, string sc, DateTime wrkdt, Models.BHG_DRContext db)
        {
            Models.RCodes rc = new Models.RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count };

            try 
            { 
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblDboFormAnswerSignatures> Ans = db.TblDboFormAnswerSignatures.Where(x => x.SiteCode == sc).ToList();
                foreach (var d in Ans)
                {
                    if (d.UpdatedOn.HasValue)
                    {
                        if ((d.CreatedOn.Value.Date >= wrkdt.Date) || (d.UpdatedOn.Value.Date >= wrkdt.Date))
                        {
                            d.RowState = 0;
                        }
                    }
                    else
                    {
                        if (d.CreatedOn.Value.Date >= wrkdt.Date)
                        {
                            d.RowState = 0;
                        }
                    }
                }

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
    }
}