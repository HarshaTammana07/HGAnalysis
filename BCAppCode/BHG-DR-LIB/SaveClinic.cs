using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace BHG_DR_LIB
{
    public partial class SaveData
    {
        public BHG_DR_LIB.Models.RCodes SaveClinic(DataTable tbl, string sc, Models.BHG_DRContext db)
        {
            BHG_DR_LIB.Models.RCodes rCodes = new Models.RCodes();
            rCodes.IsResult = true;
            Models.TblClinic c;
            try
            {
                if (db == null) { db = new Models.BHG_DRContext(); }
                {
                    bool AllNewRows = false;
                    bool NewRow = false;
                    var clinics = db.TblClinic.Where(x => x.SiteCode == sc).ToList();
                    if (clinics.Count == 0) { AllNewRows = true; }
                    foreach (DataRow r in tbl.Rows)
                    {
                        int pkey = int.Parse(r["pkey"].ToString());
                        c = clinics.Where(x => x.Pkey == pkey).FirstOrDefault();
                        if (c == null)
                        {
                            c = new Models.TblClinic
                            {
                                Pkey = pkey,
                                SiteCode = sc
                            };
                            NewRow = true;
                            rCodes.RowsIns += 1;
                        }
                        else
                        { rCodes.RowsUpd += 1; }
                        foreach (DataColumn tc in tbl.Columns)
                        {
                            switch (tc.ColumnName.ToLower())
                            {
                                case "dosewarn":
                                    c.DoseWarn = r["dosewarn"].ToString();
                                    break;
                                case "dosestop":
                                    c.DoseStop = r["dosestop"].ToString();
                                    break;
                                case "photos":
                                    c.Photos = r["photos"].ToString();
                                    break;
                                case "bottles":
                                    if (r["bottles"].ToString().Length > 0) { c.Bottles = bool.Parse(r["bottles"].ToString()); }
                                    break;
                                case "overdue":
                                    if (r["overdue"].ToString().Length > 0) { c.Overdue = int.Parse(r["overdue"].ToString()); }
                                    break;
                                case "billhold":
                                    if (r["billhold"].ToString().Length > 0) { c.BillHold = int.Parse(r["billhold"].ToString()); }
                                    break;
                                case "test":
                                    if (r["test"].ToString().Length > 0) { c.Test = int.Parse(r["test"].ToString()); }
                                    break;
                                case "tbtest":
                                    if (r["tbtest"].ToString().Length > 0) { c.Tbtest = bool.Parse(r["tbtest"].ToString()); }
                                    break;
                                case "force":
                                    if (r["force"].ToString().Length > 0) { c.Force = bool.Parse(r["force"].ToString()); }
                                    break;
                                case "lastupdated":
                                    c.LastUpdated = DateTime.Parse(r["lastupdated"].ToString());
                                    break;
                                case "note":
                                    c.Note = r["note"].ToString();
                                    break;
                                case "provider":
                                    c.Provider = r["provider"].ToString();
                                    break;
                                case "site":
                                    c.Site = r["site"].ToString();
                                    break;
                                case "cliniccode":
                                    c.Cliniccode = r["cliniccode"].ToString();
                                    break;
                                case "dischargeguest":
                                    if (r["dischargeguest"].ToString().Length > 0) { c.DischargeGuest = bool.Parse(r["dischargeguest"].ToString()); }
                                    break;
                                case "numinventory":
                                    if (r["NumInventory"].ToString().Length > 0) { c.NumInventory = int.Parse(r["NumInventory"].ToString()); }
                                    break;
                                case "schedua":
                                    if (r["schedua"].ToString().Length > 0) { c.Schedua = bool.Parse(r["schedua"].ToString()); }
                                    break;
                                case "uamonthly":
                                    if (r["uamonthly"].ToString().Length > 0) { c.Uamonthly = bool.Parse(r["uamonthly"].ToString()); }
                                    break;
                                case "clinicname":
                                    c.ClinicName = r["clinicname"].ToString();
                                    break;
                                case "tpautomation":
                                    if (r["tpautomation"].ToString().Length > 0) { c.Tpautomation = int.Parse(r["tpautomation"].ToString()); }
                                    break;
                                case "requiredarts":
                                    if (r["requiredarts"].ToString().Length > 0) { c.RequireDarts = bool.Parse(r["requiredarts"].ToString()); }
                                    break;
                                case "physicaltestdays":
                                    if (r["physicaltestdays"].ToString().Length > 0) { c.PhysicalTestDays = int.Parse(r["physicaltestdays"].ToString()); }
                                    break;
                                case "asphysicaltest":
                                    if (r["asphysicaltest"].ToString().Length > 0) { c.AsphysicalTest = bool.Parse(r["asphysicaltest"].ToString()); }
                                    break;
                                case "serviceoverlappopup":
                                    if (r["serviceoverlappopup"].ToString().Length > 0) { c.ServiceOverlapPopup = bool.Parse(r["serviceoverlappopup"].ToString()); }
                                    break;
                                case "orangeandwhite":
                                    if (r["orangeandwhite"].ToString().Length > 0) { c.OrangeandWhite = bool.Parse(r["orangeandwhite"].ToString()); }
                                    break;
                                case "ToxProvider":
                                    c.ToxProvider = r["ToxProvider"].ToString();
                                    break;
                                case "numberofreceipts":
                                    if (r["numberofreceipts"].ToString().Length > 0) { c.NumberofReceipts = int.Parse(r["numberofreceipts"].ToString()); }
                                    break;
                                case "passwordenforce":
                                    if (r["passwordenforce"].ToString().Length > 0) { c.PasswordEnforce = bool.Parse(r["passwordenforce"].ToString()); }
                                    break;
                                case "passwordlength":
                                    if (r["passwordlength"].ToString().Length > 0) { c.PasswordLength = int.Parse(r["passwordlength"].ToString()); }
                                    break;
                                case "helpfile":
                                    c.Helpfile = r["helpfile"].ToString();
                                    break;
                                case "scanpath":
                                    c.ScanPath = r["scanpath"].ToString();
                                    break;
                                case "datesigstart":
                                    c.DateSigStart = DateTime.Parse(r["datesigstart"].ToString());
                                    break;
                                case "elecsigs":
                                    if (r["elecsigs"].ToString().Length > 0) { c.ElecSigs = bool.Parse(r["elecsigs"].ToString()); }
                                    break;
                                case "CreditPriorWeek":
                                    if (r["CreditPriorWeek"].ToString().Length > 0) { c.CreditPriorWeek = bool.Parse(r["CreditPriorWeek"].ToString()); }
                                    break;
                                case "defaulttaborange":
                                    if (r["defaulttaborange"].ToString().Length > 0) { c.DefaultTabOrange = bool.Parse(r["defaulttaborange"].ToString()); }
                                    break;
                                case "bottleweight":
                                    if (r["bottleweight"].ToString().Length > 0) { c.BottleWeight = decimal.Parse(r["bottleweight"].ToString()); }
                                    break;
                                case "SpGravity":
                                    if (r["SpGravity"].ToString().Length > 0) { c.SpGravity = decimal.Parse(r["SpGravity"].ToString()); }
                                    break;
                                case "dosecharge":
                                    if (r["dosecharge"].ToString().Length > 0) { c.DoseCharge = bool.Parse(r["dosecharge"].ToString()); }
                                    break;
                                case "timeoffset":
                                    if (r["timeoffset"].ToString().Length > 0) { c.TimeOffset = int.Parse(r["timeoffset"].ToString()); }
                                    break;
                                case "cosign":
                                    if (r["cosign"].ToString().Length > 0) { c.CoSign = bool.Parse(r["cosign"].ToString()); }
                                    break;
                                case "defaultprogram":
                                    c.DefaultProgram = r["defaultprogram"].ToString();
                                    break;
                                case "clientsecurity":
                                    if (r["clientsecurity"].ToString().Length > 0) { c.ClientSecurity = bool.Parse(r["clientsecurity"].ToString()); }
                                    break;
                                case "autocheckin":
                                    if (r["autocheckin"].ToString().Length > 0) { c.AutoCheckin = bool.Parse(r["autocheckin"].ToString()); }
                                    break;
                                case "checkincheck":
                                    if (r["checkincheck"].ToString().Length > 0) { c.CheckinCheck = bool.Parse(r["checkincheck"].ToString()); }
                                    break;
                                case "orderservice":
                                    if (r["orderservice"].ToString().Length > 0) { c.OrderService = bool.Parse(r["orderservice"].ToString()); }
                                    break;
                                case "residential":
                                    if (r["residential"].ToString().Length > 0) { c.Residential = bool.Parse(r["residential"].ToString()); }
                                    break;
                                case "billdirection":
                                    c.BillDirection = r["billdirection"].ToString();
                                    break;
                                case "smallreceipts":
                                    if (r["smallreceipts"].ToString().Length > 0) { c.SmallReceipts = bool.Parse(r["smallreceipts"].ToString()); }
                                    break;
                                case "duplicatecheckincheck":
                                    if (r["DuplicateCheckinCheck"].ToString().Length > 0) { c.DuplicateCheckinCheck = bool.Parse(r["DuplicateCheckinCheck"].ToString()); }
                                    break;
                                case "noprintcheckinlabel":
                                    if (r["NoPrintCheckinLabel"].ToString().Length > 0) { c.NoPrintCheckinLabel = bool.Parse(r["NoPrintCheckinLabel"].ToString()); }
                                    break;
                                case "addomain":
                                    c.AdDomain = r["addomain"].ToString();
                                    break;
                                case "autosetlabelprinter":
                                    c.AutoSetLabelprinter = r["AutoSetLabelprinter"].ToString();
                                    break;
                                case "autosetreceiptprinter":
                                    c.AutoSetReceiptPrinter = r["AutoSetReceiptPrinter"].ToString();
                                    break;
                                case "clinicletter":
                                    c.ClinicLetter = r["clinicletter"].ToString();
                                    break;
                                case "clinicstate":
                                    c.ClinicState = r["clinicstate"].ToString();
                                    break;
                                case "liquid":
                                    if (r["liquid"].ToString().Length > 0) { c.Liquid = bool.Parse(r["liquid"].ToString()); }
                                    break;
                                case "otherinvtype":
                                    c.OtherInvType = r["OtherInvType"].ToString();
                                    break;
                                case "printdoseamt":
                                    if (r["printdoseamt"].ToString().Length > 0) { c.PrintDoseAmt = bool.Parse(r["printdoseamt"].ToString()); }
                                    break;
                                case "tabs":
                                    if (r["tabs"].ToString().Length > 0) { c.Tabs = bool.Parse(r["tabs"].ToString()); }
                                    break;
                                case "dailyservices":
                                    if (r["dailyservices"].ToString().Length > 0) { c.DailyServices = bool.Parse(r["dailyservices"].ToString()); }
                                    break;
                                case "clientsearchrin":
                                    if (r["ClientsearchRin"].ToString().Length > 0) { c.ClientsearchRin = bool.Parse(r["ClientsearchRin"].ToString()); }
                                    break;
                                case "dischargeclearsholds":
                                        if (r["DischargeClearsHolds"].ToString().Length > 0) { c.DischargeClearsHolds = bool.Parse(r["DischargeClearsHolds"].ToString()); }
                                    break;
                                case "drugfreeonly":
                                    if (r["drugfreeonly"].ToString().Length > 0) { c.DrugFreeOnly = bool.Parse(r["drugfreeonly"].ToString()); }
                                    break;
                                case "halfweekcredit":
                                    if (r["halfweekcredit"].ToString().Length > 0) { c.Halfweekcredit = bool.Parse(r["halfweekcredit"].ToString()); }
                                    break;
                                case "allowrinzero":
                                    if (r["allowrinzero"].ToString().Length > 0) { c.AllowRinZero = bool.Parse(r["allowrinzero"].ToString()); }
                                    break;
                                case "allowanyrin":
                                    if (r["allowanyrin"].ToString().Length > 0) { c.AllowAnyRin = bool.Parse(r["allowanyrin"].ToString()); }
                                    break;
                                case "defaultshowholdatnursing":
                                    if (r["DefaultShowHoldAtNursing"].ToString().Length > 0) { c.DefaultShowHoldAtNursing = bool.Parse(r["DefaultShowHoldAtNursing"].ToString()); }
                                    break;
                                case "hideelecsigdates":
                                    if (r["HideElecSigDates"].ToString().Length > 0) { c.HideElecSigDates = bool.Parse(r["HideElecSigDates"].ToString()); }
                                    break;
                                case "quesearch":
                                    if (r["quesearch"].ToString().Length > 0) { c.QueSearch = bool.Parse(r["quesearch"].ToString()); }
                                    break;
                                case "educfieldisempstatus":
                                    if (r["EducFieldIsEmpStatus"].ToString().Length > 0) { c.EducFieldIsEmpStatus = bool.Parse(r["EducFieldIsEmpStatus"].ToString()); }
                                    break;
                                case "autoimportua":
                                    if (r["autoimportua"].ToString().Length > 0) { c.AutoImportUa = bool.Parse(r["autoimportua"].ToString()); }
                                    break;
                                case "fastdose":
                                    if (r["fastdose"].ToString().Length > 0) { c.FastDose = bool.Parse(r["fastdose"].ToString()); }
                                    break;
                                case "recidprint":
                                    if (r["RecIdprint"].ToString().Length > 0) { c.RecIdprint = bool.Parse(r["RecIdprint"].ToString()); }
                                    break;
                                case "nursesig":
                                    if (r["nursesig"].ToString().Length > 0) { c.NurseSig = bool.Parse(r["nursesig"].ToString()); }
                                    break;
                                case "order2confirm":
                                    if (r["order2confirm"].ToString().Length > 0) { c.Order2confirm = int.Parse(r["order2confirm"].ToString()); }
                                    break;
                                case "orderconfirm":
                                    if (r["orderconfirm"].ToString().Length > 0) { c.Orderconfirm = int.Parse(r["orderconfirm"].ToString()); }
                                    break;
                                case "toxacct":
                                    c.ToxAcct = r["ToxACCT"].ToString();
                                    break;
                                case "spgravityclear":
                                    if (r["spgravityclear"].ToString().Length > 0) { c.SpGravityClear = decimal.Parse(r["spgravityclear"].ToString()); }
                                    break;
                                case "toxtixnum":
                                    c.Toxtixnum = int.Parse(r["Toxtixnum"].ToString());
                                    break;
                                case "toxtixspecial":
                                    c.ToxTixspecial = r["ToxTixspecial"].ToString();
                                    break;
                                case "autoorderexpirationholds":
                                    if (r["AutoOrderExpirationHolds"].ToString().Length > 0) { c.AutoOrderExpirationHolds = bool.Parse(r["AutoOrderExpirationHolds"].ToString()); }
                                    break;
                                case "reqallintake":
                                    if (r["Reqallintake"].ToString().Length > 0) { c.Reqallintake = bool.Parse(r["Reqallintake"].ToString()); }
                                    break;
                                case "numberofbulklabels":
                                    if (r["numberofbulklabels"].ToString().Length > 0) { c.NumberOfBulkLabels = int.Parse(r["numberofbulklabels"].ToString()); }
                                    break;
                                case "uaonvisit":
                                    if (r["uaonvisit"].ToString().Length > 0) { c.UaonVisit = bool.Parse(r["uaonvisit"].ToString()); }
                                    break;
                                case "diversion_padding":
                                    if (r["Diversion_Padding"].ToString().Length > 0) { c.DiversionPadding = int.Parse(r["Diversion_Padding"].ToString()); }
                                    break;
                                case "wordpath":
                                    c.Wordpath = r["wordpath"].ToString();
                                    break;
                                case "scandeleteoriginal":
                                    if (r["ScanDeleteOriginal"].ToString().Length > 0) { c.ScanDeleteOriginal = bool.Parse(r["ScanDeleteOriginal"].ToString()); }
                                    break;
                                case "udspanelrequired":
                                    if (r["UdspanelRequired"].ToString().Length > 0) { c.UdspanelRequired = bool.Parse(r["UdspanelRequired"].ToString()); }
                                    break;
                                case "autodischargecredit":
                                    if (r["AutoDischargeCredit"].ToString().Length > 0) { c.AutoDischargeCredit = bool.Parse(r["AutoDischargeCredit"].ToString()); }
                                    break;
                                case "beakercolors":
                                    if (r["BeakerColors"].ToString().Length > 0) { c.BeakerColors = bool.Parse(r["BeakerColors"].ToString()); }
                                    break;
                                case "numberpriortransactionsonreceipt":
                                    if (r["NumberPriorTransactionsOnReceipt"].ToString().Length > 0) { c.NumberPriorTransactionsOnReceipt = int.Parse(r["NumberPriorTransactionsOnReceipt"].ToString()); }
                                    break;
                                case "alwaysallowusesavedsignature":
                                    if (r["AlwaysAllowUseSavedSignature"].ToString().Length > 0) { c.AlwaysAllowUseSavedSignature = bool.Parse(r["AlwaysAllowUseSavedSignature"].ToString()); }
                                    break;
                                case "newbottlelabels":
                                    if (r["NewBottleLabels"].ToString().Length > 0) { c.NewBottleLabels = bool.Parse(r["NewBottleLabels"].ToString()); }
                                    break;
                                case "doctemplatepath":
                                    c.DocTemplatePath = r["DocTemplatePath"].ToString();
                                    break;
                                case "reportdir":
                                    c.ReportDir = r["ReportDir"].ToString();
                                    break;
                                case "reportserver":
                                    c.ReportServer = r["reportserver"].ToString();
                                    break;
                                case "donotallowcascade":
                                    if (r["donotallowcascade"].ToString().Length > 0) { c.DonotallowCascade = bool.Parse(r["donotallowcascade"].ToString()); }
                                    break;
                                case "isbhg":
                                    if (r["isbhg"].ToString().Length > 0) { c.IsBhg = bool.Parse(r["isbhg"].ToString()); }
                                    break;
                                case "multiplequeues":
                                    if (r["MultipleQueues"].ToString().Length > 0) { c.MultipleQueues = bool.Parse(r["MultipleQueues"].ToString()); }
                                    break;
                                case "labacct":
                                    c.LabAcct = r["labacct"].ToString();
                                    break;
                                case "alwaysaskbaglabel":
                                    if (r["AlwaysAskBagLabel"].ToString().Length > 0) { c.AlwaysAskBagLabel = bool.Parse(r["AlwaysAskBagLabel"].ToString()); }
                                    break;
                                case "prepackbaglabeldefault":
                                    if (r["PrepackBagLabelDefault"].ToString().Length > 0) { c.PrepackBagLabelDefault = bool.Parse(r["PrepackBagLabelDefault"].ToString()); }
                                    break;
                                case "defaultshowholdfront":
                                    if (r["DefaultShowHoldFront"].ToString().Length > 0) { c.DefaultShowHoldFront = bool.Parse(r["DefaultShowHoldFront"].ToString()); }
                                    break;
                                case "showfutureuaholds":
                                    if (r["ShowFutureUAholds"].ToString().Length > 0) { c.ShowFutureUaholds = bool.Parse(r["ShowFutureUAholds"].ToString()); }
                                    break;
                                case "openonsunday":
                                    if (r["openonsunday"].ToString().Length > 0) { c.OpenOnSunday = bool.Parse(r["openonsunday"].ToString()); }
                                    break;
                                case "chargebeforedose":
                                    if (r["ChargeBeforeDose"].ToString().Length > 0) { c.ChargeBeforeDose = bool.Parse(r["ChargeBeforeDose"].ToString()); }
                                    break;
                                case "landscapelabel":
                                    if (r["LandscapeLabel"].ToString().Length > 0) { c.LandscapeLabel = bool.Parse(r["LandscapeLabel"].ToString()); }
                                    break;
                                case "sigimgpath":
                                    c.SigImgpath = r["sigIMGPATH"].ToString();
                                    break;
                                case "sigimguri":
                                    c.SigImguri = r["sigIMGURI"].ToString();
                                    break;
                                case "signbeforedose":
                                    if (r["SignBeforeDose"].ToString().Length > 0) { c.SignBeforeDose = bool.Parse(r["SignBeforeDose"].ToString()); }
                                    break;
                                case "sortclientsearchbyid":
                                    if (r["SortClientSearchbyID"].ToString().Length > 0) { c.SortClientSearchbyId = bool.Parse(r["SortClientSearchbyID"].ToString()); }
                                    break;
                                case "uapath":
                                    c.Uapath = r["UApath"].ToString();
                                    break;
                                case "adjustmentemail":
                                    c.AdjustmentEmail = r["AdjustmentEmail"].ToString();
                                    break;
                                case "bladjustatdischarge":
                                    if (r["blAdjustatDischarge"].ToString().Length > 0) { c.BlAdjustatDischarge = bool.Parse(r["blAdjustatDischarge"].ToString()); }
                                    break;
                                case "fifo_bottle":
                                    if (r["Fifo_Bottle"].ToString().Length > 0) { c.FifoBottle = bool.Parse(r["Fifo_Bottle"].ToString()); }
                                    break;
                                case "usecostcenter":
                                    if (r["UseCostCenter"].ToString().Length > 0) { c.UseCostCenter = bool.Parse(r["UseCostCenter"].ToString()); }
                                    break;
                                case "forcecheckin":
                                    if (r["forcecheckin"].ToString().Length > 0) { c.ForceCheckin = bool.Parse(r["forcecheckin"].ToString()); }
                                    break;
                                case "verifymedadjustment":
                                    if (r["VerifyMedAdjustment"].ToString().Length > 0) { c.VerifyMedAdjustment = bool.Parse(r["VerifyMedAdjustment"].ToString()); }
                                    break;
                                case "pinsigs":
                                    if (r["pinsigs"].ToString().Length > 0) { c.PinSigs = bool.Parse(r["pinsigs"].ToString()); }
                                    break;
                                case "combine3payfees":
                                    if (r["COMBINE3PAYFEES"].ToString().Length > 0) { c.Combine3payfees = bool.Parse(r["COMBINE3PAYFEES"].ToString()); }
                                    break;
                                case "pinbeforesig":
                                    if (r["pinbeforesig"].ToString().Length > 0) { c.PinBeforeSig = bool.Parse(r["pinbeforesig"].ToString()); }
                                    break;
                                case "siglcd":
                                    if (r["siglcd"].ToString().Length > 0) { c.Siglcd = bool.Parse(r["siglcd"].ToString()); }
                                    break;
                                case "dictionarypath":
                                    c.DictionaryPath = r["DictionaryPath"].ToString();
                                    break;
                                case "grammerpath":
                                    c.Grammerpath = r["grammerpath"].ToString();
                                    break;
                                case "diversiontype":
                                    c.DiversionType = r["diversionType"].ToString();
                                    break;
                                case "ismedmark":
                                    if (r["Ismedmark"].ToString().Length > 0) { c.Ismedmark = bool.Parse(r["Ismedmark"].ToString()); }
                                    break;
                                case "servicedimslinktotp":
                                    if (r["ServiceDimsLinkToTp"].ToString().Length > 0) { c.ServiceDimsLinkToTp = bool.Parse(r["ServiceDimsLinkToTp"].ToString()); }
                                    break;
                                case "advancedtesting":
                                    if (r["Advancedtesting"].ToString().Length > 0) { c.Advancedtesting = bool.Parse(r["Advancedtesting"].ToString()); }
                                    break;
                                case "allowactoldorder":
                                    if (r["AllowActOldOrder"].ToString().Length > 0) { c.AllowActOldOrder = bool.Parse(r["AllowActOldOrder"].ToString()); }
                                    break;
                                case "firstinitialontoxlabel":
                                    if (r["FirstInitialonToxlabel"].ToString().Length > 0) { c.FirstInitialonToxlabel = bool.Parse(r["FirstInitialonToxlabel"].ToString()); }
                                    break;
                                case "over100check":
                                    if (r["Over100check"].ToString().Length > 0) { c.Over100check = bool.Parse(r["Over100check"].ToString()); }
                                    break;
                                case "noquepop":
                                    if (r["NoQuePop"].ToString().Length > 0) { c.NoQuePop = bool.Parse(r["NoQuePop"].ToString()); }
                                    break;
                                case "offsetdoseconfirm":
                                    if (r["offsetdoseconfirm"].ToString().Length > 0) { c.Offsetdoseconfirm = bool.Parse(r["offsetdoseconfirm"].ToString()); }
                                    break;
                                case "orderrequestsneedbothsigs":
                                    if (r["OrderRequestsNeedBothSigs"].ToString().Length > 0) { c.OrderRequestsNeedBothSigs = bool.Parse(r["OrderRequestsNeedBothSigs"].ToString()); }
                                    break;
                                case "smalltox":
                                    if (r["smalltox"].ToString().Length > 0) { c.SmallTox = bool.Parse(r["smalltox"].ToString()); }
                                    break;
                                case "toxservice":
                                    if (r["Toxservice"].ToString().Length > 0) { c.Toxservice = bool.Parse(r["Toxservice"].ToString()); }
                                    break;
                                case "zebra":
                                    if (r["Zebra"].ToString().Length > 0) { c.Zebra = bool.Parse(r["Zebra"].ToString()); }
                                    break;
                                case "fingerprintsig":
                                    if (r["FingerPrintSig"].ToString().Length > 0) { c.FingerPrintSig = bool.Parse(r["FingerPrintSig"].ToString()); }
                                    break;
                                case "voregistrationpath":
                                    c.Voregistrationpath = r["Voregistrationpath"].ToString();
                                    break;
                                case "blockaptcalhold":
                                    if (r["Blockaptcalhold"].ToString().Length > 0) 
                                    { c.Blockaptcalhold = bool.Parse(r["Blockaptcalhold"].ToString()); }
                                    break;
                                case "calendarstarttime":
                                    if (r["CalendarStartTime"].ToString().Length > 0) 
                                    { c.CalendarStartTime = int.Parse(r["CalendarStartTime"].ToString()); }
                                    break;
                                case "eligpw":
                                    c.EligPw = r["eligpw"].ToString();
                                    break;
                                case "eligun":
                                    c.EligUn = r["eligun"].ToString();
                                    break;
                                case "fivedaycalendarweek":
                                    if (r["FiveDayCalendarWeek"].ToString().Length > 0) 
                                    { c.FiveDayCalendarWeek = bool.Parse(r["FiveDayCalendarWeek"].ToString()); }
                                    break;
                                case "multitenant":
                                    if (r["Multitenant"].ToString().Length > 0) { c.Multitenant = bool.Parse(r["Multitenant"].ToString()); }
                                    break;
                                case "pumpwindow":
                                    if (r["Pumpwindow"].ToString().Length > 0) { c.Pumpwindow = bool.Parse(r["Pumpwindow"].ToString()); }
                                    break;
                                case "requireemergencycontact":
                                    if (r["RequireEmergencyContact"].ToString().Length > 0) 
                                    { c.RequireEmergencyContact = bool.Parse(r["RequireEmergencyContact"].ToString()); }
                                    break;
                                case "queuetwice":
                                    if (r["QueueTwice"].ToString().Length > 0) { c.QueueTwice = bool.Parse(r["QueueTwice"].ToString()); }
                                    break;
                                case "checkuaisprescription":
                                    if (r["CheckUaisPrescription"].ToString().Length > 0) 
                                    { c.CheckUaisPrescription = bool.Parse(r["CheckUaisPrescription"].ToString()); }
                                    break;
                                case "enablebuspass":
                                    if (r["EnableBusPass"].ToString().Length > 0) { c.EnableBusPass = bool.Parse(r["EnableBusPass"].ToString()); }
                                    break;
                                case "claimdir":
                                    c.ClaimDir = r["ClaimDir"].ToString();
                                    break;
                                case "isihc":
                                    if (r["IsIhc"].ToString().Length > 0) { c.IsIhc = bool.Parse(r["IsIhc"].ToString()); }
                                    break;
                                case "phase":
                                    if (r["Phase"].ToString().Length > 0) { c.Phase = bool.Parse(r["Phase"].ToString()); }
                                    break;
                                case "setevalsotherfocus":
                                    if (r["setEvalsOtherFocus"].ToString().Length > 0) 
                                    { c.SetEvalsOtherFocus = int.Parse(r["setEvalsOtherFocus"].ToString()); }
                                    break;
                                case "enableholdaypickupcalifornia":
                                    if (r["EnableHoldayPickupCalifornia"].ToString().Length > 0) 
                                    { c.EnableHoldayPickupCalifornia = int.Parse(r["EnableHoldayPickupCalifornia"].ToString()); }
                                    break;
                                case "zerossns":
                                    if (r["ZeroSSNs"].ToString().Length > 0) { c.ZeroSsns = bool.Parse(r["ZeroSSNs"].ToString()); }
                                    break;
                                case "enabletouchsig":
                                    if (r["EnableTouchSig"].ToString().Length > 0) 
                                    { c.EnableTouchSig = bool.Parse(r["EnableTouchSig"].ToString()); }
                                    break;
                                case "creditdosesdischarge":
                                    if (r["CreditDosesDischarge"].ToString().Length > 0) 
                                    { c.CreditDosesDischarge = bool.Parse(r["CreditDosesDischarge"].ToString()); }
                                    break;
                                case "allowbulkdrsigs":
                                    if (r["AllowBulkDrSigs"].ToString().Length > 0) { c.AllowBulkDrSigs = bool.Parse(r["AllowBulkDrSigs"].ToString()); }
                                    break;
                                case "enablealertsmedchanges":
                                    if (r["EnableAlertsMedChanges"].ToString().Length > 0) 
                                    { c.EnableAlertsMedChanges = bool.Parse(r["EnableAlertsMedChanges"].ToString()); }
                                    break;
                                case "enableorderalerts":
                                    if (r["EnableOrderAlerts"].ToString().Length > 0) 
                                    { c.EnableOrderAlerts = bool.Parse(r["EnableOrderAlerts"].ToString()); }
                                    break;
                                case "enabletestingalerts":
                                    if (r["EnableTestingAlerts"].ToString().Length > 0) 
                                    { c.EnableTestingAlerts = bool.Parse(r["EnableTestingAlerts"].ToString()); }
                                    break;
                                case "enableatriskalerts":
                                    if (r["EnableAtRiskAlerts"].ToString().Length > 0) 
                                    { c.EnableAtRiskAlerts = bool.Parse(r["EnableAtRiskAlerts"].ToString()); }
                                    break;
                                case "enableadministeringclientmeds":
                                    if (r["EnableAdministeringClientMeds"].ToString().Length > 0) 
                                    { c.EnableAdministeringClientMeds = bool.Parse(r["EnableAdministeringClientMeds"].ToString()); }
                                    break;
                                case "disableserviceunits":
                                    if (r["DisableServiceUnits"].ToString().Length > 0) 
                                    { c.DisableServiceUnits = bool.Parse(r["DisableServiceUnits"].ToString()); }
                                    break;
                                case "ismultiprogram":
                                    if (r["ismultiprogram"].ToString().Length > 0) 
                                    { c.IsMultiProgram = bool.Parse(r["isMultiProgram"].ToString()); }
                                    break;
                                case "versionnbr":
                                    if (r["versionNbr"].ToString().Length > 0) 
                                    { c.VersionNbr = decimal.Parse(r["versionNbr"].ToString()); }
                                    break;
                                case "labelprintmedtypeinsteadofmedclass":
                                    if (r["LabelPrintMedTypeInsteadOfMedClass"].ToString().Length > 0) 
                                    { c.LabelPrintMedTypeInsteadOfMedClass = bool.Parse(r["LabelPrintMedTypeInsteadOfMedClass"].ToString()); }
                                    break;
                                case "enableenrolldischargedateinsearchgrid":
                                    if (r["EnableEnrollDischargeDateInSearchGrid"].ToString().Length > 0) 
                                    { c.EnableEnrollDischargeDateInSearchGrid = bool.Parse(r["EnableEnrollDischargeDateInSearchGrid"].ToString()); }
                                    break;
                                case "enableservicerevisions":
                                    if (r["EnableServiceRevisions"].ToString().Length > 0) 
                                    { c.EnableServiceRevisions = bool.Parse(r["EnableServiceRevisions"].ToString()); }
                                    break;
                                case "enablebac":
                                    if (r["enableBAC"].ToString().Length > 0) { c.EnableBac = bool.Parse(r["enableBAC"].ToString()); }
                                    break;
                                case "sammsformsdefaultindexnumber":
                                    if (r["SammsFormsDefaultIndexNumber"].ToString().Length > 0) 
                                    { c.SammsFormsDefaultIndexNumber = int.Parse(r["SammsFormsDefaultIndexNumber"].ToString()); }
                                    break;
                                case "enabledrivemapping":
                                    if (r["enableDriveMapping"].ToString().Length > 0) 
                                    { c.EnableDriveMapping = bool.Parse(r["enableDriveMapping"].ToString()); }
                                    break;
                                case "destructbottle":
                                    if (r["destructbottle"].ToString().Length > 0) 
                                    { c.Destructbottle = bool.Parse(r["destructbottle"].ToString()); }
                                    break;
                                case "dontprintorders":
                                    if (r["dontprintorders"].ToString().Length > 0) 
                                    { c.Dontprintorders = bool.Parse(r["dontprintorders"].ToString()); }
                                    break;
                                case "disableprintservicemessageaftersaveprompt":
                                    if (r["DisablePrintServiceMessageAfterSavePrompt"].ToString().Length > 0) 
                                    { c.DisablePrintServiceMessageAfterSavePrompt = bool.Parse(r["DisablePrintServiceMessageAfterSavePrompt"].ToString()); }
                                    break;
                                case "disableotherasreferralsource":
                                    if (r["DisableOtherAsReferralSource"].ToString().Length > 0) 
                                    { c.DisableOtherAsReferralSource = bool.Parse(r["DisableOtherAsReferralSource"].ToString()); }
                                    break;
                                case "enableinventory4and5":
                                    if (r["EnableInventory4and5"].ToString().Length > 0) 
                                    { c.EnableInventory4and5 = bool.Parse(r["EnableInventory4and5"].ToString()); }
                                    break;
                                case "sigpadtest":
                                    if (r["SigPadTest"].ToString().Length > 0) 
                                    { c.SigPadTest = bool.Parse(r["SigPadTest"].ToString()); }
                                    break;
                                case "enablerssalerts":
                                    if (r["EnableRssAlerts"].ToString().Length > 0) 
                                    { c.EnableRssAlerts = bool.Parse(r["EnableRssAlerts"].ToString()); }
                                    break;
                                case "iispath":
                                    c.Iispath = r["iispath"].ToString();
                                    break;
                                case "printalternativezebraanddymolabelversion1":
                                    if (r["PrintAlternativeZebraAndDymoLabelVersion1"].ToString().Length > 0) 
                                    { c.PrintAlternativeZebraAndDymoLabelVersion1 = bool.Parse(r["PrintAlternativeZebraAndDymoLabelVersion1"].ToString()); }
                                    break;
                                case "isrnp":
                                    if (r["IsRnp"].ToString().Length > 0) { c.IsRnp = bool.Parse(r["IsRnp"].ToString()); }
                                    break;
                                case "enableautopopulatecity":
                                    if (r["EnableAutoPopulateCity"].ToString().Length > 0) 
                                    { c.EnableAutoPopulateCity = bool.Parse(r["EnableAutoPopulateCity"].ToString()); }
                                    break;
                                case "intakepacketurl":
                                    c.IntakePacketUrl = r["IntakePacketURL"].ToString();
                                    break;
                                case "nocheckinatpay":
                                    if (r["NoCheckinatPay"].ToString().Length > 0) { c.NoCheckinatPay = bool.Parse(r["NoCheckinatPay"].ToString()); }
                                    break;
                                case "enableautoholdonabnormallab":
                                    if (r["EnableAutoHoldOnAbnormalLab"].ToString().Length > 0) 
                                    { c.EnableAutoHoldOnAbnormalLab = bool.Parse(r["EnableAutoHoldOnAbnormalLab"].ToString()); }
                                    break;
                                case "multiqueuerefreshintervaltimeset":
                                    if (r["MultiQueueRefreshIntervalTimeSet"].ToString().Length > 0) { c.MultiQueueRefreshIntervalTimeSet = int.Parse(r["MultiQueueRefreshIntervalTimeSet"].ToString()); }
                                    break;
                                case "enablesuffixmiddleinitialinfirstnameofsearch":
                                    if (r["EnableSuffixMiddleInitialInFirstNameOfSearch"].ToString().Length > 0) 
                                    { c.EnableSuffixMiddleInitialInFirstNameOfSearch = bool.Parse(r["EnableSuffixMiddleInitialInFirstNameOfSearch"].ToString()); }
                                    break;
                                case "enableuserloginatbacqueuemodeelseinitials":
                                    if (r["enableUserLoginAtBACqueueModeElseInitials"].ToString().Length > 0) 
                                    { c.EnableUserLoginAtBacqueueModeElseInitials = bool.Parse(r["enableUserLoginAtBACqueueModeElseInitials"].ToString()); }
                                    break;
                                case "siteid":
                                    if (r["SiteID"].ToString().Length > 0) { c.SiteId = int.Parse(r["SiteID"].ToString()); }
                                    break;
                                case "printsitesaddressdependingonsites":
                                    if (r["PrintSitesAddressDependingOnSites"].ToString().Length > 0) { c.PrintSitesAddressDependingOnSites = bool.Parse(r["PrintSitesAddressDependingOnSites"].ToString()); }
                                    break;
                                case "donotprintdoe":
                                    if (r["DoNotPrintDOE"].ToString().Length > 0) { c.DoNotPrintDoe = bool.Parse(r["DoNotPrintDOE"].ToString()); }
                                    break;
                                case "enableautospsammsbilling":
                                    if (r["enableAutoSpSAMMSBilling"].ToString().Length > 0) 
                                    { c.EnableAutoSpSammsbilling = bool.Parse(r["enableAutoSpSAMMSBilling"].ToString()); }
                                    break;
                                case "enablecounselorselectioninmultiprogramsectiononly":
                                    if (r["enableCounselorSelectionInMultiProgramSectionOnly"].ToString().Length > 0) 
                                    { c.EnableCounselorSelectionInMultiProgramSectionOnly = bool.Parse(r["enableCounselorSelectionInMultiProgramSectionOnly"].ToString()); }
                                    break;
                                case "urlassessment":
                                    c.Urlassessment = r["urlassessment"].ToString();
                                    break;
                                case "disabletpproblemandinownwords":
                                    if (r["DisableTPproblemAndInOwnWords"].ToString().Length > 0) 
                                    { c.DisableTpproblemAndInOwnWords = bool.Parse(r["DisableTPproblemAndInOwnWords"].ToString()); }
                                    break;
                                case "enablecustomizablerequirementsforclientinfo":
                                    if (r["enableCustomizableRequirementsForClientInfo"].ToString().Length > 0) 
                                    { c.EnableCustomizableRequirementsForClientInfo = bool.Parse(r["enableCustomizableRequirementsForClientInfo"].ToString()); }
                                    break;
                                case "enableintakedischargeincomeinputs":
                                    if (r["enableIntakeDischargeIncomeInputs"].ToString().Length > 0) 
                                    { c.EnableIntakeDischargeIncomeInputs = bool.Parse(r["enableIntakeDischargeIncomeInputs"].ToString()); }
                                    break;
                                case "enableautobillingduringeachtoxprint":
                                    if (r["enableAutoBillingDuringEachToxPrint"].ToString().Length > 0) 
                                    { c.EnableAutoBillingDuringEachToxPrint = bool.Parse(r["enableAutoBillingDuringEachToxPrint"].ToString()); }
                                    break;
                                case "issh":
                                    if (r["isSH"].ToString().Length > 0) { c.IsSh = bool.Parse(r["isSH"].ToString()); }
                                    break;
                                case "enablebacstopdose":
                                    if (r["enableBACstopDose"].ToString().Length > 0) 
                                    { c.EnableBacstopDose = bool.Parse(r["enableBACstopDose"].ToString()); }
                                    break;
                                case "enablebacnurseholdevenblowzero":
                                    if (r["enableBACnurseHoldEvenBlowZero"].ToString().Length > 0) 
                                    { c.EnableBacnurseHoldEvenBlowZero = bool.Parse(r["enableBACnurseHoldEvenBlowZero"].ToString()); }
                                    break;
                                case "enablesignaturesduringpillcount":
                                    if (r["EnableSignaturesDuringPillCount"].ToString().Length > 0) 
                                    { c.EnableSignaturesDuringPillCount = bool.Parse(r["EnableSignaturesDuringPillCount"].ToString()); }
                                    break;
                                case "enablesignaturewhenadministeringmeds":
                                    if (r["EnableSignatureWhenAdministeringMeds"].ToString().Length > 0) 
                                    { c.EnableSignatureWhenAdministeringMeds = bool.Parse(r["EnableSignatureWhenAdministeringMeds"].ToString()); }
                                    break;
                                case "chsamsid":
                                    c.Chsamsid = r["CHSAMSID"].ToString();
                                    break;
                                case "fts":
                                    if (r["FTS"].ToString().Length > 0) { c.Fts = int.Parse(r["FTS"].ToString()); }
                                    break;
                                case "over20":
                                    if (r["Over20"].ToString().Length > 0) { c.Over20 = bool.Parse(r["Over20"].ToString()); }
                                    break;
                                case "forcefindtype":
                                    if (r["forcefindtype"].ToString().Length > 0) { c.Forcefindtype = bool.Parse(r["forcefindtype"].ToString()); }
                                    break;
                                case "hnpurl":
                                    c.HnPurl = r["HnPUrl"].ToString();
                                    break;
                                case "enableprintmedtypecoloronidcard":
                                    if (r["EnablePrintMedTypeColorOnIDCard"].ToString().Length > 0) 
                                    { c.EnablePrintMedTypeColorOnIdcard = bool.Parse(r["EnablePrintMedTypeColorOnIDCard"].ToString()); }
                                    break;
                                case "enableportraitlabeldoubleside":
                                    if (r["EnablePortraitLabelDoubleSide"].ToString().Length > 0) 
                                    { c.EnablePortraitLabelDoubleSide = bool.Parse(r["EnablePortraitLabelDoubleSide"].ToString()); }
                                    break;
                                case "enablecommentsonmulticheckin":
                                    if (r["enableCommentsOnMultiCheckin"].ToString().Length > 0) 
                                    { c.EnableCommentsOnMultiCheckin = bool.Parse(r["enableCommentsOnMultiCheckin"].ToString()); }
                                    break;
                                case "pullpicsfromdb":
                                    if (r["PullPicsFromDB"].ToString().Length > 0) 
                                    { c.PullPicsFromDb = bool.Parse(r["PullPicsFromDB"].ToString()); }
                                    break;
                                case "enablecompetentcheckboxatdosing":
                                    if (r["EnableCompetentCheckBoxAtDosing"].ToString().Length > 0) 
                                    { c.EnableCompetentCheckBoxAtDosing = bool.Parse(r["EnableCompetentCheckBoxAtDosing"].ToString()); }
                                    break;
                                case "enableactivateorderwhennotinsuboxoneprog":
                                    if (r["EnableActivateOrderWhenNotInSuboxoneProg"].ToString().Length > 0) 
                                    { c.EnableActivateOrderWhenNotInSuboxoneProg = bool.Parse(r["EnableActivateOrderWhenNotInSuboxoneProg"].ToString()); }
                                    break;
                                case "enableprinttoxlandscape":
                                    if (r["EnablePrintToxLandscape"].ToString().Length > 0) 
                                    { c.EnablePrintToxLandscape = bool.Parse(r["EnablePrintToxLandscape"].ToString()); }
                                    break;
                                case "enableflagnurseforbac":
                                    if (r["EnableFlagNurseForBAC"].ToString().Length > 0) 
                                    { c.EnableFlagNurseForBac = bool.Parse(r["EnableFlagNurseForBAC"].ToString()); }
                                    break;
                                case "bottlereturnnote":
                                    if (r["BottleReturnNote"].ToString().Length > 0) 
                                    { c.BottleReturnNote = bool.Parse(r["BottleReturnNote"].ToString()); }
                                    break;
                                case "bhgmarginth":
                                    if (r["BHGMarginTH"].ToString().Length > 0) 
                                    { c.BhgmarginTh = int.Parse(r["BHGMarginTH"].ToString()); }
                                    break;
                                case "bhgmargintox":
                                    if (r["BHGMarginTox"].ToString().Length > 0) 
                                    { c.BhgmarginTox = int.Parse(r["BHGMarginTox"].ToString()); }
                                    break;
                                case "nocheckinservice":
                                    if (r["NoCheckinService"].ToString().Length > 0)
                                    {
                                        if (r["NoCheckinService"].ToString().ToLower() == "false") { c.NoCheckinService = 0; }
                                        else
                                        {
                                            c.NoCheckinService = 1;
                                            //int.Parse(r["NoCheckinService"].ToString()); Remove 8/22/2023
                                        }
                                    }
                                    break;
                                case "nosammsformheader":
                                    if (r["NoSAMMSFormHeader"].ToString().Length > 0) { c.NoSammsformHeader = bool.Parse(r["NoSAMMSFormHeader"].ToString()); }
                                    break;
                                case "printunitdoselabel":
                                    if (r["PrintUnitDoseLabel"].ToString().Length > 0) { c.PrintUnitDoseLabel = bool.Parse(r["PrintUnitDoseLabel"].ToString()); }
                                    break;
                                case "authbasedonprogram":
                                    if (r["AuthBasedOnProgram"].ToString().Length > 0) { c.AuthBasedOnProgram = bool.Parse(r["AuthBasedOnProgram"].ToString()); }
                                    break;
                                case "landscapezebra":
                                    if (r["LandscapeZebra"].ToString().Length > 0) { c.LandscapeZebra = bool.Parse(r["LandscapeZebra"].ToString()); }
                                    break;
                                case "showbalanceatdispense":
                                    if (r["ShowBalanceAtDispense"].ToString().Length > 0) { c.ShowBalanceAtDispense = bool.Parse(r["ShowBalanceAtDispense"].ToString()); }
                                    break;
                                case "singlequeuerefreshintervaltimeset":
                                    if (r["SingleQueueRefreshIntervalTimeSet"].ToString().Length > 0) 
                                    { c.SingleQueueRefreshIntervalTimeSet = int.Parse(r["SingleQueueRefreshIntervalTimeSet"].ToString()); }
                                    break;
                                case "multidosingclinic":
                                    if (r["MultiDosingClinic"].ToString().Length > 0) { c.MultiDosingClinic = bool.Parse(r["MultiDosingClinic"].ToString()); }
                                    break;
                                case "blasterwide":
                                    if (r["BlasterWide"].ToString().Length > 0) { c.BlasterWide = bool.Parse(r["BlasterWide"].ToString()); }
                                    break;
                                case "pumpcalibrate":
                                    if (r["PumpCalibrate"].ToString().Length > 0) { c.PumpCalibrate = bool.Parse(r["PumpCalibrate"].ToString()); }
                                    break;
                                case "checkvisitingpatient":
                                    if (r["CheckVisitingPatient"].ToString().Length > 0) { c.CheckVisitingPatient = bool.Parse(r["CheckVisitingPatient"].ToString()); }
                                    break;
                                case "requireclientsignatureorderrequest":
                                    if (r["RequireClientSignatureOrderRequest"].ToString().Length > 0) 
                                    { c.RequireClientSignatureOrderRequest = bool.Parse(r["RequireClientSignatureOrderRequest"].ToString()); }
                                    break;
                                case "dischargedallowaddpayer":
                                    if (r["DischargedAllowAddPayer"].ToString().Length > 0) 
                                    { c.DischargedAllowAddPayer = bool.Parse(r["DischargedAllowAddPayer"].ToString()); }
                                    break;
                                case "dymodetailed":
                                        if (r["DymoDetailed"].ToString().Length > 0) { c.DymoDetailed = bool.Parse(r["DymoDetailed"].ToString()); }
                                    break;
                            }
                        }
                        if (NewRow)
                        {
                            db.TblClinic.Add(c);
                            NewRow = false;
                        }
                    }
                    db.SaveChanges();
                }
            }
            catch (Exception e)
            {
                rCodes.IsResult = false;
                rCodes.ExceptMsg = e.Message.ToString();
                Console.WriteLine(e.Message.ToString());
                if (e.InnerException != null) 
                { 
                    rCodes.ExceptInnerMsg = e.InnerException.Message.ToString();
                    Console.WriteLine(e.InnerException.Message.ToString());
                }
            }
            return rCodes;
        }
    }
}
