using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public Models.RCodes SaveCows_v6(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            Models.RCodes rCodes = new Models.RCodes
            {
                IsResult = true,
                RowsProcessed = tbl.Rows.Count
            };

            try 
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                List<Models.TblCowxref> cowxrefs = db.TblCowxref.ToList();
                List<Models.TblCowsV6> cows = db.TblCowsV6.Where(x => x.SiteCode == sc).ToList();
                List<Models.TblCowsV6> ncows = new List<Models.TblCowsV6>();
                foreach(DataRow r in tbl.Rows)
                {
                    Models.TblCowsV6 cow = new Models.TblCowsV6();
                    foreach(DataColumn c in tbl.Columns)
                    {
                        switch(c.ColumnName.ToLower())
                        {
                            case "sitecode":
                                cow.SiteCode = r[c.ColumnName].ToString();
                                break;
                            case "cowid":
                            case "id":
                                cow.Cowid = int.Parse(r[c.ColumnName].ToString());
                                cow.RowState = true;
                                cow.LastModAt = DateTime.Now;
                                break;
                            case "cltid":
                            case "patientid":
                                cow.CltId = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "preadmissionid":
                                cow.Preadmissionid = int.Parse(r[c.ColumnName].ToString());
                                break;
                            case "dttime":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    cow.Dttime = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "reasonforthisassessment":
                                if (r[c.ColumnName].ToString().Trim().Length == 0)
                                {
                                    if (tbl.Columns.Contains("ddlreasonforthisassessment"))
                                    {
                                        string reason = r["ddlreasonforthisassessment"].ToString();
                                        if (reason.Length > 0)
                                        {
                                            int lkReason = int.Parse(reason);
                                            cow.ReasonforthisAssessment = cowxrefs.FirstOrDefault(x => x.ColumnName == "AssessmentReason" && x.PermissibleValue == lkReason).DescripiveText;
                                            if (cow.ReasonforthisAssessment.Trim().Length == 0)
                                            {
                                                cow.ReasonforthisAssessment = reason;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (r[c.ColumnName].ToString().Trim().Length > 0)
                                    {
                                        cow.ReasonforthisAssessment = r[c.ColumnName].ToString();
                                    }
                                }
                                break;
                            case "ddlreasonforthisassessment":
                                string reasond = r["ddlreasonforthisassessment"].ToString();
                                if (reasond.Length > 0)
                                {
                                    int lkReason = int.Parse(reasond);
                                    Models.TblCowxref dtref = cowxrefs.FirstOrDefault(x => x.ColumnName == "AssessmentReason" && x.PermissibleValue == lkReason);
                                    if (dtref == null)
                                    {
                                        cow.ReasonforthisAssessment = reasond;
                                    }
                                    else
                                    {
                                        cow.ReasonforthisAssessment = dtref.DescripiveText;
                                    }
                                }
                                break;
                            case "restingpulserate":
                                if (r[c.ColumnName].ToString().Trim().Length > 0)
                                {
                                    cow.RestingPulseRate = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "restingpulseratedesc":
                                cow.RestingPulseRatedesc = r[c.ColumnName].ToString();
                                if ((cow.RestingPulseRatedesc.Trim().Length == 0) && (cow.RestingPulseRate.HasValue))
                                {
                                    cow.RestingPulseRatedesc = cowxrefs.Where(x => x.ColumnName == "PulseRate" && x.PermissibleValue == cow.RestingPulseRate).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "giupset":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.Giupset = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "giupsetdesc":
                                cow.Giupsetdesc = r[c.ColumnName].ToString();
                                if ((cow.Giupsetdesc.Trim().Length == 0) && (cow.Giupset.HasValue))
                                {
                                    cow.Giupsetdesc = cowxrefs.Where(x => x.ColumnName == "UpsetGI" && x.PermissibleValue == cow.Giupset).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "sweating":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.Sweating = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "sweatingdesc":
                                cow.Sweatingdesc = r[c.ColumnName].ToString();
                                if ((cow.Sweatingdesc.Trim().Length == 0) && (cow.Sweating.HasValue))
                                {
                                    cow.Sweatingdesc = cowxrefs.Where(x => x.ColumnName == "Sweat" && x.PermissibleValue == cow.Sweating).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "tremor":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.Tremor = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "tremordesc":
                                cow.Tremordesc = r[c.ColumnName].ToString();
                                if ((cow.Tremordesc.Trim().Length == 0) && (cow.Tremor.HasValue))
                                {
                                    cow.Tremordesc = cowxrefs.Where(x => x.ColumnName == "Tremorhand" && x.PermissibleValue == cow.Tremor).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "restlessness":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.Restlessness = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "restlessnessdesc":
                                cow.Restlessnessdesc = r[c.ColumnName].ToString(); 
                                if ((cow.Restlessnessdesc.Trim().Length == 0) && (cow.Restlessness.HasValue))
                                {
                                    cow.Restlessnessdesc = cowxrefs.Where(x => x.ColumnName == "Restless" && x.PermissibleValue == cow.Restlessness).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "yawning":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.Yawning = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "yawningdec":
                                cow.Yawningdec = r[c.ColumnName].ToString();
                                if ((cow.Yawningdec.Trim().Length == 0) && (cow.Yawning.HasValue))
                                {
                                    cow.Yawningdec = cowxrefs.Where(x => x.ColumnName == "Yawn" && x.PermissibleValue == cow.Yawning).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "pupilsize":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.PupilSize = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "pupilsizedesc":
                                cow.PupilSizedesc = r[c.ColumnName].ToString();
                                if ((cow.PupilSizedesc.Trim().Length == 0) && (cow.PupilSize.HasValue))
                                {
                                    cow.PupilSizedesc = cowxrefs.Where(x => x.ColumnName == "Pupil" && x.PermissibleValue == cow.PupilSize).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "anxietyorirritability":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.AnxietyOrIrritability = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "anxietyorirritabilitydesc":
                                cow.AnxietyOrIrritabilitydesc = r[c.ColumnName].ToString();
                                if ((cow.AnxietyOrIrritabilitydesc.Trim().Length == 0) && (cow.AnxietyOrIrritability.HasValue))
                                {
                                    cow.AnxietyOrIrritabilitydesc = cowxrefs.Where(x => x.ColumnName == "Anxiety" && x.PermissibleValue == cow.AnxietyOrIrritability).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "boneorjointaches":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.BoneOrJointAches = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "boneorjointachesdesc":
                                cow.BoneOrJointAchesdesc = r[c.ColumnName].ToString();
                                if ((cow.BoneOrJointAchesdesc.Trim().Length == 0) && (cow.BoneOrJointAches.HasValue))
                                {
                                    cow.BoneOrJointAchesdesc = cowxrefs.Where(x => x.ColumnName == "BoneJointAche" && x.PermissibleValue == cow.BoneOrJointAches).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "goosefleshskin":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.GoosefleshSkin = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "goosefleshskindesc":
                                cow.GoosefleshSkindesc = r[c.ColumnName].ToString();
                                if ((cow.GoosefleshSkindesc.Trim().Length == 0) && (cow.GoosefleshSkin.HasValue))
                                {
                                    cow.GoosefleshSkindesc = cowxrefs.Where(x => x.ColumnName == "Gooseflesh" && x.PermissibleValue == cow.GoosefleshSkin).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "runnynoseortearing":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    cow.RunnyNoseOrTearing = int.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "runnynoseortearingdesc":
                                cow.RunnyNoseOrTearingdesc = r[c.ColumnName].ToString();
                                if ((cow.RunnyNoseOrTearingdesc.Trim().Length == 0) && (cow.RunnyNoseOrTearing.HasValue))
                                {
                                    cow.RunnyNoseOrTearingdesc = cowxrefs.Where(x => x.ColumnName == "RunnyNose" && x.PermissibleValue == cow.RunnyNoseOrTearing).FirstOrDefault().DescripiveText;
                                }
                                break;
                            case "completedby":
                                //cow.CompletedBy = r[c.ColumnName].ToString();
                                break;
                            case "createdon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    cow.CreatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "createdby":
                                cow.CreatedBy = r[c.ColumnName].ToString();
                                break;
                            case "updatedby":
                                cow.UpdatedBy = r[c.ColumnName].ToString();
                                break;
                            case "updatedon":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    cow.UpdatedOn = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isactive":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString() == "")
                                    {
                                        cow.IsActive = true;
                                    }
                                }
                                break;
                            case "patientsignature":
                                cow.PatientSignature = r[c.ColumnName].ToString();
                                break;
                            case "clientsignaturedate":
                                if (r[c.ColumnName].ToString().Length > 6)
                                {
                                    cow.ClientSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "isdeleted":
                                if (r[c.ColumnName].ToString().Length > 0)
                                {
                                    if (r[c.ColumnName].ToString() == "1")
                                    {
                                        cow.IsDeleted = true;
                                        cow.RowState = false;
                                    }
                                }
                                break;
                            case "staffnamesignature":
                            case "staffsignatureby":
                                cow.StaffNameSignature = r["staffnamesignature"].ToString();
                                if (cow.StaffNameSignature.Trim().Length == 0)
                                {
                                    cow.StaffNameSignature = r[c.ColumnName].ToString();
                                }
                                break;
                            case "staffsignaturedate":
                                if (r[c.ColumnName].ToString().Trim().Length > 6)
                                {
                                    cow.StaffSignatureDate = DateTime.Parse(r[c.ColumnName].ToString());
                                }
                                break;
                            case "version":
                                cow.Version = r[c.ColumnName].ToString();
                                break;
                            case "rowchksum":
                                cow.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                                break;
                        }
                    }
                    Models.TblCowsV6 dbc = cows.Where(x => x.Cowid == cow.Cowid 
                        //&& x.CltId == cow.CltId
                        && x.Preadmissionid == cow.Preadmissionid).FirstOrDefault();
                    if (dbc == null)
                    {
                        ncows.Add(cow);
                        rCodes.RowsIns++;
                    }
                    else
                    {
                        dbc.CltId = cow.CltId;

                        dbc.AnxietyOrIrritability = cow.AnxietyOrIrritability;
                        dbc.BoneOrJointAches = cow.BoneOrJointAches;
                        dbc.Giupset = cow.Giupset;
                        dbc.GoosefleshSkin = cow.GoosefleshSkin;
                        dbc.PupilSize = cow.PupilSize;
                        dbc.RestingPulseRate = cow.RestingPulseRate;
                        dbc.Restlessness = cow.Restlessness;
                        dbc.RunnyNoseOrTearing = cow.RunnyNoseOrTearing;
                        dbc.Sweating = cow.Sweating;
                        dbc.Tremor = cow.Tremor;
                        dbc.Yawning = cow.Yawning;

                        dbc.AnxietyOrIrritabilitydesc = cow.AnxietyOrIrritabilitydesc;
                        dbc.BoneOrJointAchesdesc = cow.BoneOrJointAchesdesc;
                        dbc.Giupsetdesc = cow.Giupsetdesc;
                        dbc.GoosefleshSkindesc = cow.GoosefleshSkindesc;
                        dbc.PupilSizedesc = cow.PupilSizedesc;
                        dbc.ReasonforthisAssessment = cow.ReasonforthisAssessment;
                        dbc.RestingPulseRatedesc = cow.RestingPulseRatedesc;
                        dbc.Restlessnessdesc = cow.Restlessnessdesc;
                        dbc.RunnyNoseOrTearingdesc = cow.RunnyNoseOrTearingdesc;
                        dbc.Sweatingdesc = cow.Sweatingdesc;
                        dbc.Tremordesc = cow.Tremordesc;
                        dbc.Yawningdec = cow.Yawningdec;

                        dbc.ClientSignatureDate = cow.ClientSignatureDate;
                        dbc.PatientSignature = cow.PatientSignature;
                        dbc.StaffNameSignature = cow.StaffNameSignature;
                        dbc.StaffSignatureDate = cow.StaffSignatureDate;
                        //dbc.CompletedBy = cow.CompletedBy;
                        dbc.CreatedBy = cow.CreatedBy;
                        dbc.CreatedOn = cow.CreatedOn;
                        dbc.Dttime = cow.Dttime;
                        dbc.IsActive = cow.IsActive;
                        dbc.IsDeleted = cow.IsDeleted;
                        dbc.LastModAt = cow.LastModAt;
                        dbc.Preadmissionid = cow.Preadmissionid;
                        dbc.UpdatedBy = cow.UpdatedBy;
                        dbc.UpdatedOn = cow.UpdatedOn;
                        dbc.Version = cow.Version;
                        dbc.LastModAt = cow.LastModAt;
                        dbc.RowChkSum = cow.RowChkSum;
                        dbc.RowState = cow.RowState;
                        rCodes.RowsUpd++;
                    }
                }
                db.SaveChanges();
                if (ncows.Count > 0)
                {
                    db.TblCowsV6.AddRange(ncows);
                    db.SaveChanges();
                }
            }
            catch(Exception e)
            {
                rCodes.IsResult = false;
                rCodes.ExceptMsg = e.Message;
                if (e.InnerException != null)
                {
                    rCodes.ExceptInnerMsg = e.InnerException.Message;
                }
            }

                return rCodes;
        }
    }
}
