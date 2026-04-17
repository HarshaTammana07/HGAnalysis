using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text;

namespace BHG_DR_LIB
{
    public class BulkDartsSvc
    {
        private string strDartMerge = "";

        public Models.RCodes BulkDartsSrvLoader(DataTable tbl, string dsnSchTbl, string sitecode, DateTime wrkDt, Models.BHG_DRContext db)
        {
            Models.RCodes rst = new Models.RCodes
            {
                IsResult = true
            };
            if (tbl.Rows.Count > 0)
            {
                #region ListBlock

                //List<Models.TblDartsSrvStg> Darts = new List<Models.TblDartsSrvStg>();

                //foreach(DataRow r in tbl.Rows)
                //{
                //    Models.TblDartsSrvStg d = new Models.TblDartsSrvStg();
                //    foreach(DataColumn c in tbl.Columns)
                //    {
                //        switch(c.ColumnName.ToLower())
                //        {
                //            case "dsid":
                //                d.DsId = int.Parse(r[c.ColumnName].ToString());
                //                break;
                //            case "dsclt":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsClt = int.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "sitecode":
                //                d.SiteCode = r[c.ColumnName].ToString();
                //                break;
                //            case "rowchksum":
                //                d.RowChkSum = int.Parse(r[c.ColumnName].ToString());
                //                break;
                //            case "dsdim1":
                //                if (r[c.ColumnName].ToString().Length > 0)
                //                {
                //                    d.DsDim1 = bool.Parse(r[c.ColumnName].ToString());
                //                }
                //                break;
                //            case "dsdim2":
                //                if (r[c.ColumnName].ToString().Length > 0)
                //                {
                //                    d.DsDim2 = bool.Parse(r[c.ColumnName].ToString());
                //                }
                //                break;
                //            case "dsdim3":
                //                if (r[c.ColumnName].ToString().Length > 0)
                //                {
                //                    d.DsDim3 = bool.Parse(r[c.ColumnName].ToString());
                //                }
                //                break;
                //            case "dsdim4":
                //                if (r[c.ColumnName].ToString().Length > 0)
                //                {
                //                    d.DsDim4 = bool.Parse(r[c.ColumnName].ToString());
                //                }
                //                break;
                //            case "dsdim5":
                //                if (r[c.ColumnName].ToString().Length > 0)
                //                {
                //                    d.DsDim5 = bool.Parse(r[c.ColumnName].ToString());
                //                }
                //                break;
                //            case "dsdim6":
                //                if (r[c.ColumnName].ToString().Length > 0)
                //                {
                //                    d.DsDim6 = bool.Parse(r[c.ColumnName].ToString());
                //                }
                //                break;
                //            case "dstxtsrv":
                //                d.DsTxtSrv = r[c.ColumnName].ToString();
                //                break;
                //            case "dsdtstart":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsDtStart = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsdtend":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsDtEnd = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dstxttype":
                //                d.DsTxtType = r[c.ColumnName].ToString();
                //                break;
                //            case "dsdblunits":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsdblUnits = Double.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsnoteid":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsNoteId = int.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsdtadded":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsDtAdded = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dstxtstaff":
                //                d.DstxtStaff = r["DstxtStaff"].ToString();
                //                break;
                //            case "dstxtnote":
                //                d.DstxtNote = r["DstxtNote"].ToString();
                //                break;
                //            case "dsrtbnote":
                //                d.DsRtbnote = r["DsRtbnote"].ToString();
                //                break;
                //            case "dsbilled":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.Dsbilled = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsgroupnum":
                //                d.DsGroupnum = r["DsGroupnum"].ToString();
                //                break;
                //            case "dsprogram":
                //                d.DsProgram = r[c.ColumnName].ToString();
                //                break;
                //            case "dsupdate":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsUpdate = DateTime.Parse(r["DsUpdate"].ToString()); }
                //                break;
                //            case "dsupdatestaff":
                //                d.DsUpdatestaff = r["DsUpdatestaff"].ToString();
                //                break;
                //            case "upsize_ts":
                //                break;
                //            case "dsinvalidatedon":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsInvalidatedOn = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dserror":
                //                d.DsError = r[c.ColumnName].ToString();
                //                break;
                //            case "dstxthiv":
                //                d.DsTxtHiv = r[c.ColumnName].ToString();
                //                break;
                //            case "dsdartsgroup":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsDartsGroup = int.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "repoldsrv":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.RepOldSrv = decimal.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dssignature":
                //                d.DsSignature = r["DsSignature"].ToString();
                //                break;
                //            case "dssigdate":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsSigDate = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dssigdatecosign":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DssigdateCosign = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dssignaturecosign":
                //                d.DssignatureCosign = r["DssignatureCosign"].ToString();
                //                break;
                //            case "dssiguser":
                //                d.DsSigUser = r[c.ColumnName].ToString();
                //                break;
                //            case "dssigusercosign":
                //                d.DsSigUserCosign = r[c.ColumnName].ToString();
                //                break;
                //            case "dssigclt":
                //                d.DsSigclt = r["DsSigclt"].ToString();
                //                break;
                //            case "dssigcltdate":
                //                if (r["DsSigcltdate"].ToString().Length > 7) { d.DsSigcltdate = DateTime.Parse(r["DsSigcltdate"].ToString()); }
                //                break;
                //            case "dssigcltuser":
                //                d.DsSigcltuser = r["DsSigcltuser"].ToString();
                //                break;
                //            case "dsaptid":
                //                if (r["DsAptid"].ToString().Length > 0) { d.DsAptid = int.Parse(r["DsAptid"].ToString()); }
                //                break;
                //            case "dsuncharted":
                //                if (r["Dsuncharted"].ToString().Length > 0) { d.Dsuncharted = bool.Parse(r["Dsuncharted"].ToString()); }
                //                break;
                //            case "dstxdim1":
                //                if (r["DsTxDim1"].ToString().Length > 0) { d.DsTxDim1 = int.Parse(r["DsTxDim1"].ToString()); }
                //                break;
                //            case "dstxdim2":
                //                if (r["DsTxDim2"].ToString().Length > 0) { d.DsTxDim2 = int.Parse(r["DsTxDim2"].ToString()); }
                //                break;
                //            case "dstxdim3":
                //                if (r["DsTxDim3"].ToString().Length > 0) { d.DsTxDim3 = int.Parse(r["DsTxDim3"].ToString()); }
                //                break;
                //            case "dstxdim4":
                //                if (r["DsTxDim4"].ToString().Length > 0) { d.DsTxDim4 = int.Parse(r["DsTxDim4"].ToString()); }
                //                break;
                //            case "dstxdim5":
                //                if (r["DsTxDim5"].ToString().Length > 0) { d.DsTxDim5 = int.Parse(r["DsTxDim5"].ToString()); }
                //                break;
                //            case "dstxdim6":
                //                if (r["DsTxDim6"].ToString().Length > 0) { d.DsTxDim6 = int.Parse(r["DsTxDim6"].ToString()); }
                //                break;
                //            case "dsdiag":
                //                d.DsDiag = r["DsDiag"].ToString();
                //                break;
                //            case "dsarea":
                //                d.DsArea = r["DsArea"].ToString();
                //                break;
                //            case "dsgroupdefaultnote":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsGroupDefaultNote = bool.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsgroupend":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsGroupEnd = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsgroupidentity":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.DsGroupIdentity = int.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsgroupstart":
                //                if (r[c.ColumnName].ToString().Length > 7) { d.DsGroupStart = DateTime.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsdiag10":
                //                d.DsDiag10 = r[c.ColumnName].ToString();
                //                break;
                //            case "siteid":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.SiteId = int.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "dsdbnotes":
                //                d.DsDbnotes = r["DsDbnotes"].ToString();
                //                break;
                //            case "dssigcltimg":
                //                if (r["DsSigCltImg"].ToString().Length > 0) { d.DsSigCltImg = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString()); }
                //                break;
                //            case "dssignaturecosignimg":
                //                if (r["DsSignatureCoSignImg"].ToString().Length > 0) { d.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString()); }
                //                break;
                //            case "dssignatureimg":
                //                if (r["DsSignatureImg"].ToString().Length > 0) { d.DsSignatureImg = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString()); }
                //                break;
                //            case "mg":
                //                if (r[c.ColumnName].ToString().Length > 0) { d.Mg = Double.Parse(r[c.ColumnName].ToString()); }
                //                break;
                //            case "lastmodat":
                //                break;
                //        }
                //    }
                //    Darts.Add(d);
                //}
                #endregion

                // Bulk load
                SQLSvrManager sm = new SQLSvrManager();

                SqlBulkCopy bc = new SqlBulkCopy(sm.ConnectionString);
                //bc.DestinationTableName = "stg.tbl_DartsSrv";
                bc.DestinationTableName = dsnSchTbl;
                bc.BulkCopyTimeout = 99999;
                if (dsnSchTbl.ToLower() != "stg.tbl_formscounts")
                {
                    if (dsnSchTbl.ToLower() != "pats.tbl_formssammsclient")
                    {
                        _ = sm.ExeSqlCmd("Truncate Table " + dsnSchTbl, sm.ConnectionString);
                    }
                }
                //if (dsnSchTbl.ToLower() == "pats.tbl_formssammsclient")
                //{
                //    _ = sm.ExeSqlCmd("Truncate Table " + dsnSchTbl, sm.ConnectionString);
                //}

                foreach (DataColumn c in tbl.Columns)
                {
                    bc.ColumnMappings.Add(new SqlBulkCopyColumnMapping(c.ColumnName, c.ColumnName));
                }

                try
                {
                    // Write from the source to the destination.
                    bc.WriteToServer(tbl);

                    // Execute Merge procedure
                    switch (dsnSchTbl.ToLower())
                    {
                        case "pats.tbl_formssammsclient":
                            string strcmd = "update f set SiteCode = l.SiteCode, f.RowState = case when f.fscCLTID < 0 then 0 else 1 end, f.LastModAt = GetDate() " +
                                " from pats.tbl_FormsSAMMSClient f left join ctrl.tbl_Locations l on f.fscsite = l.sID " +
                                " where l.SiteCode is not null and (f.SiteCode = 'Global' or f.SiteCode<> l.SiteCode or f.fscCLTID < 0) ";
                            rst.RowsProcessed = sm.ExeSqlCmd(strcmd, sm.ConnectionString);
                            break;
                        case "stg.tbl_claims":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.ClaimsMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.tbl_claimlineitem":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.ClaimLineItemMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.tbl_claimlineitemactivity":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.ClaimLineItemActivityMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.clientdemo":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.ClientDemoMerge1 '" + sitecode + "'", sm.ConnectionString);
                            _ = sm.ExeSqlCmd("exec stg.ClientDemoMerge2 '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.tbl_dartssrv":
                                rst.RowsProcessed = sm.ExeSqlCmd("exec stg.DartsSrvMerge ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge22 ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge23 ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge24 ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge25 ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge26 ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge27 ", sm.ConnectionString);
                                rst.RowsProcessed += sm.ExeSqlCmd("exec stg.DartsSrvMerge28 ", sm.ConnectionString);
                            break;
                        case "stg.tbl_dose":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.DoseMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.tbl_dose_excuse":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.Dose_ExcuseMerge ", sm.ConnectionString);
                            break;
                        case "stg.tbl_formssammsclient":
                            if (sitecode == "PHC")
                            {
                                rst.RowsProcessed = sm.ExeSqlCmd("exec stg.FormsSAMMSMergePHC ", sm.ConnectionString);
                            }
                            else
                            {
                                rst.RowsProcessed = sm.ExeSqlCmd("exec stg.FormsSAMMSMerge ", sm.ConnectionString);
                            }
                            break;
                        case "stg.tbl_formscounts":
                            //rst.RowsProcessed = sm.ExeSqlCmd("exec stg.FormsMergeCounts ", sm.ConnectionString);
                            break;
                        case "stg.tbl_uaresultdetail":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.UAResultDetailMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.tbl_labresultdetail":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.LABResultDetailMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                        case "stg.tbl_vw3pbillsub":
                            rst.RowsProcessed = sm.ExeSqlCmd("exec stg.sp_BillSubMerge '" + sitecode + "'", sm.ConnectionString);
                            break;
                    }
                    if ((dsnSchTbl.ToLower() != "stg.tbl_formscounts") && (dsnSchTbl.ToLower() != "pats.tbl_formssammsclient"))
                    {
                        //if (dsnSchTbl.ToLower() != "stg.tbl_dose")
                        {
                            _ = sm.ExeSqlCmd("Truncate Table " + dsnSchTbl, sm.ConnectionString);
                        }
                    }
                }
                catch (Exception ex)
                {
                    rst.IsResult = false;
                    Console.WriteLine(ex.Message);
                    rst.ExceptMsg = ex.Message;
                    if (ex.InnerException != null)
                    {
                        rst.ExceptInnerMsg = ex.InnerException.Message;
                    }
                }
                finally
                {
                    bc.Close();
                }
            }
            return rst;
        }
    }
}
