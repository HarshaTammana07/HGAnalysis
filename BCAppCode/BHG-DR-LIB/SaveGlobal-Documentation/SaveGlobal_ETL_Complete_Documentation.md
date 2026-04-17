
Global Reference Data ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract global reference and clinic-configuration data from SAMMS SQL Server databases and from external billing systems, and to load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What "global" reference data is and why it exists across 13 save methods
- What each of the 13 methods in SaveGlobal.cs does and how they relate to each other
- What systems and files are involved from start to finish
- How the Scheduler creates tasks
- How BHGTaskRunner picks tasks up and drives the pipeline
- How SelectConstructor builds the source SQL
- How SQLSvrManager executes against the source databases
- What the source tables look like and their relevant columns
- What the destination tables look like and their relevant columns
- How the four distinct load patterns work across the 13 methods
- How RowChkSum, RowState, and IsActive are used for change detection and soft-delete
- How date-windowed resets work in the forms and BAM methods
- How RowTrax audit tracking works
- What happens when errors occur
- All known anomalies, bugs, and quirks in the code
________________________________________

2. High-Level Business Summary

What is "global" reference data?

Unlike patient transaction data (doses, counseling sessions, appointments, lab results), global data is the shared configuration and reference infrastructure that every clinic depends on. It does not belong to individual patients — it belongs to the clinic, the system, or the network.

BHG operates 80+ SAMMS-connected clinics. Each clinic runs its own SAMMS SQL Server instance. Much of the configuration in each SAMMS database is shared across the network: the same payers appear at many clinics, the same consent forms are used system-wide, the same users move between clinics. SaveGlobal.cs consolidates all of this reference data from across every clinic into a single centralised picture in Azure BHG_DR.

The 13 save methods in SaveGlobal.cs handle the following business domains:

1.  Fee Schedules (SaveFeeSchedules)
    The mapping between service codes (CPT codes), payers, and reimbursement rates. Each clinic may have its own fee schedule, or clinics on a shared SAMMS system share one. Fee schedules drive billing and revenue cycle management.

2.  Global Payers (SaveGlobalPayer)
    The directory of insurance payers configured in SAMMS. For SAMMS-connected clinics, all clinics share the same payer list. AdvMD-connected clinics have their own. Payer records hold submission format, EDI payer IDs, billing thresholds, and claim-level settings.

3.  Users (SaveGlobalUser)
    The staff directory. Every clinician, counselor, physician, nurse, and administrative user in SAMMS is captured here. Records include NPI numbers, DEA numbers, roles, credentials, and flags indicating whether the user is a counselor, doctor, calendar user, etc.

4.  User Site Assignments (SaveGlobalUserSite)
    Which user is assigned to which clinic site and their default site. Supports multi-site user lookups and calendar/scheduling features.

5.  Clinical Opiate Withdrawal Scale (SaveGlobalClinicalOpiateWithdrawalScale)
    COWS (Clinical Opiate Withdrawal Scale) assessment records. A clinical scoring tool used to measure the severity of opioid withdrawal in patients. Scores are entered per patient per observation, and the combined score drives dosing decisions in MAT (Medication-Assisted Treatment) clinics.

6.  Forms SAMMS Clients (SaveGlobalFormsSAMMSClients)
    The consent and form-signature audit log. Every time a form is presented to a client for signature, a record is created in SAMMS linking the client, form template, date, and signature status (client, staff, physician, nurse, supervisor, guardian, admin nurse). This table is queried heavily for compliance and audit purposes.

7.  Consents — SAMMS (SaveGlobalConsents)
    The master list of consent form templates used across SAMMS clinics. Defines which signature types each form requires (client, staff, physician, nurse, guardian, supervisor, admin nurse) and whether the form has a recurring schedule, supports BAC capture, etc.

8.  Consents — PHC (SaveGlobalConsentsPhc)
    The equivalent consent form template list for PHC-system clinics. Functionally identical to SaveGlobalConsents but writes to a separate Azure destination table (TblConsentsPhc).

9.  Devices (SaveGlobalDevices)
    The inventory of hardware devices registered in SAMMS: dispensing pumps, BAC devices, fingerprint scanners, signature pads, check-in kiosks, receipt printers. Each device is tied to a clinic site (via integer SId → SiteCode lookup) and has flags for its capabilities.

10. Brief Addiction Monitor (SaveBAM)
    BAM (Brief Addiction Monitor) assessment records. A 17-question clinical outcomes tool used to track patient recovery progress over time. Questions cover substance use, risk factors, and protective factors. This is a form-based data entity keyed by FId (form ID) and SiteCode. After loading, a stored procedure (pats.BAMMergeGbl) aggregates the data globally.

11. Services (SaveServices)
    The per-clinic service catalogue. Each clinic defines its available service types (individual counseling, group therapy, methadone dispensing, etc.) with associated CPT codes, cost, billing flags, and scheduling rules. This is the foundation for DartsSrv service coding.

12. Form Counts (SaveFormCounts)
    An aggregated count of how many forms were completed per client per day at each site, pulled from the central SAMMSGLOBAL database. Used for QA dashboards and compliance reporting.

13. Claim Status (SaveClaimStatus)
    The status of 3P (third-party billing) electronic claim submission batches. Tracks whether 837 EDI files have been uploaded and their current processing state per clinic database. This method belongs to Schedule 8 (SAMMS-ETL-INV), not SAMMSGlobal.

________________________________________

3. Systems Involved

Source Systems:
    SAMMS SQL Server databases (one per clinic, per-clinic connection strings in ctrl.tbl_LocationCons)
    SAMMSGLOBAL SQL Server (centralised SAMMS system database — used for Form Counts)
    PHC SQL Server (PHCSQLVM — used for PHC-specific BAM and PHC Consents)
    AdvMD / 3P billing systems (external — Claim Status source)

Destination System:
    Azure SQL — BHG_DR database

Key files:
    BHGTaskRunner\Program.cs            — Orchestration; routes each destination table to its save method
    BHG-DR-LIB\SaveGlobal.cs           — All 13 save methods (subject of this document)
    BHG-DR-LIB\SelectConstructor.cs    — Builds SELECT statements from dms.tbl_MapSrc2Dsn metadata
    BHG-DR-LIB\SQLSvrManager.cs        — ADO.NET wrapper for executing SQL against source databases
    BHG-DR-LIB\BulkDartsSvc.cs         — Used for FormsSAMMSClients bulk path (production alternative)
    BHG-DR-LIB\Models\BHG_DRContext.cs — EF Core DbContext; maps all Azure tables

Key Azure control tables:
    tsk.tbl_Tasks / vw_TaskListMap  — Task queue; Status=17 means ready to run
    ctrl.tbl_LocationCons           — One row per clinic: SiteCode, ConnectionId, connection string
    dms.tbl_MapSrc2Dsn              — Column mapping metadata per ActionKey
    dms.tbl_MapAction               — Maps DsnTbl (destination table) to FromTblVw (source view/table)
    tsk.tbl_RowTrax                 — Row count audit trail per site per task

________________________________________

4. How the Scheduler Creates Tasks

The Scheduler populates tsk.tbl_Tasks with one row per (clinic, table) combination that is due to run. For SAMMSGlobal (Schedule 1), the Scheduler identifies every active clinic in ctrl.tbl_LocationCons and creates task records for each global reference table. The relevant fields in tsk.vw_TaskListMap are:

    TaskName       — "SAMMSGlobal" (Schedule 1) or the INV task name (Schedule 8 for ClaimStatus)
    SiteCode       — The clinic's site code (e.g., "B01A", "B02B") or a shared code like "SAMMS"
    DsnTbl         — The Azure destination table name (e.g., "pats.tbl_feesched")
    FromTblVw      — The SAMMS source table or view name (from dms.tbl_MapAction)
    ConStr         — The SAMMS connection string for this clinic
    WhereCondition — Any pre-built WHERE clause fragment
    SortOrder      — Optional ORDER BY clause
    WorkDate       — The reference date for the run
    Status         — 17 = ready to run; updated to complete/error by BHGTaskRunner

SAMMSGlobal tasks cover all clinics in the network for the global reference tables. Because many clinics share the same SAMMS system (ConnectionId=2), payer and fee schedule tasks normalise the SiteCode to "SAMMS", writing a single shared record set instead of per-clinic duplicates.

________________________________________

5. How BHGTaskRunner Picks Up and Routes Tasks

BHGTaskRunner.exe is invoked with argument "1" for SAMMSGlobal:

    BHGTaskRunner.exe 1

This causes Program.cs to load only tasks where TaskName == "SAMMSGlobal" and Status == 17:

    pTasks = db.VwTaskList.Where(x => x.SiteCode != "PHC" && x.Status == 17
             && x.TaskName == "SAMMSGlobal" && x.RunAt < DateTime.Now).ToList();

BHGTaskRunner.exe 8 handles Schedule 8 (SAMMS-ETL-INV), which includes SaveClaimStatus.

For each task, Program.cs builds a base SELECT statement using SelectConstructor, then appends WHERE and ORDER BY from the task record, retrieves data using SQLSvrManager.GetTableData(), and dispatches to the appropriate save method via a large switch on st.DsnTbl.ToLower():

    case "pats.tbl_feesched":                    → sd.SaveFeeSchedules(...)
    case "pats.tbl_globalpayor":                 → sd.SaveGlobalPayer(...)
    case "ctrl.tbl_user":                        → sd.SaveGlobalUser(...)
    case "ctrl.tbl_usersites":                   → sd.SaveGlobalUserSite(...)
    case "pats.tbl_clinicalopiatewithdrawalscale": → sd.SaveGlobalClinicalOpiateWithdrawalScale(...)
    case "pats.tbl_formssammsclient":            → sd.SaveGlobalFormsSAMMSClients(...)  [or BulkDartsSrvLoader]
    case "ctrl.tbl_globalconsents":              → sd.SaveGlobalConsents(...) or sd.SaveGlobalConsentsPhc(...)
    case "ctrl.tbl_globaldevices":               → sd.SaveGlobalDevices(...)
    case "pats.tbl_briefaddictionmonitor":       → sd.SaveBAM(...)
    case "pats.tbl_services":                    → sd.SaveServices(...)
    case "pats.tbl_formssammsclient":            → also triggers sd.SaveFormCounts(...) inline
    case "ctrl.tbl_claimstatus":                 → sd.SaveClaimStatus(...)    [Schedule 8 only]

Note for FormsSAMMSClients: the current production code in Program.cs routes this table to BulkDartsSrvLoader (bulk copy to staging), not to sd.SaveGlobalFormsSAMMSClients. The EF Core path (sd.SaveGlobalFormsSAMMSClients) is present in SaveGlobal.cs and commented out in Program.cs, retained for fallback or per-clinic use.

________________________________________

6. How SelectConstructor Builds the Source SQL

SelectConstructor.cs reads dms.tbl_MapSrc2Dsn for the relevant ActionKey (1 = SAMMSGlobal) to build a dynamic SELECT statement listing all columns that should be extracted, plus a RowChkSum expression computed from those columns using SQL CHECKSUM(). This generated statement is stored in st.CmdText and passed to SQLSvrManager.GetTableData() as the base query. Program.cs appends the WHERE condition and sort order for each task before execution.

For the COWS, BAM, and FormsSAMMSClients, the WHERE clause is often built directly in Program.cs with hardcoded logic (e.g., date filters, site exclusions) rather than purely from task metadata.

________________________________________

7. How SQLSvrManager Executes Against Source

SQLSvrManager.GetTableData(fromTblVw, cmdText, conStr) executes the SELECT statement against the clinic's SAMMS SQL Server using the connection string from ctrl.tbl_LocationCons. It returns an ADO.NET DataTable with all matching rows. This DataTable is then passed to the appropriate save method.

For shared-SAMMS clinics (ConnectionId=2), the same query runs against the shared SAMMSGLOBAL database, and the result set covers all clinics on that system. The save methods then normalise the SiteCode during processing (resolving integer site IDs to SiteCode strings via TblLocations).

________________________________________

8. Source Table Schemas — SAMMS

The source columns listed below are those observed in the switch/case column mapping blocks in SaveGlobal.cs. The actual source table names come from dms.tbl_MapAction.FromTblVw.

8.1 Fee Schedules (SaveFeeSchedules)
    Source: dbo.tblFeeSched (SAMMS)
    Key columns: fsid, RowChkSum, FsPayid, DsService, cptcode, fee, Contractual, Datespan, GroupTogether, Modifier, UnitMin, ProviderBill, CoAble, DefaultWeekFee, RevCode, Startdate, Enddate, pos, AttendingBill, Pay2310A, Pay2310C, ReferredByAttending, BillAttendingOrder, BillOrderDoctor, FsMasterId, Notes1, Notes2

8.2 Global Payers (SaveGlobalPayer)
    Source: dbo.tblPayor or dbo.tblGlobalPayor (SAMMS)
    Key columns: PayID, RowChkSum, sitecode, payname, payaddress, paycity, payst, payzip, payph, payfx, paynote, paydefaultsubmit, payauthformat, payclmnum, pay835, paysubmittype, paybillamt, paydosetype, payernumber, payaddressjoin, paycheckauth, payclass, payindfreq, payindrate, payindunit, paylabelname, paynamejoin, payoverride, paypos, payregion, payreqauth, paysig, payglclass, paylab, noclaimlevelrendering, enddate, revcode, startdate, pay2310a, pay2310b, pay2310c, supresssecondary, alttaxid

8.3 Users (SaveGlobalUser)
    Source: dbo.tblUser (SAMMS)
    Key columns: uskey, emailid, isdasacounselor, usrActive, usrcalendaruser, usrcosig, usrcounselor, usrcred, usrDea, usrdescription, usrdoctor, usrext, usrfname, usrgroups, usrlicensed, usrlname, usrlocation, usrname, usrnpi, usrpassword, usrpasswordchanged, usrphone, usrpin, usrrole, usrsignature, usrsignatureimage, usrssn, usrsuper, usrtaxonomy, usrtemplate, usrtemplatechanged, usrxdea
    Note: userfullname is commented out in the mapping; not currently extracted.

8.4 User Site Assignments (SaveGlobalUserSite)
    Source: dbo.tblUserSites (SAMMS)
    Key columns: usid, usName, ussite, usdefault

8.5 COWS (SaveGlobalClinicalOpiateWithdrawalScale)
    Source: dbo.tblClinicalOpiateWithdrawalScale or equivalent form view (SAMMS)
    Key columns: fid, SiteCode (integer), RowChkSum, fcltid, completedname, combinedscore, assessdate, reasonassesslist, restingpulsenum, giupsetnum, sweatnum, tremornum, restlessnum, yawnnum, pupilnum, anxnum, bonenum, goosenum, runnynum, genevaTest, timeampm, assesstimetext

8.6 Forms SAMMS Clients (SaveGlobalFormsSAMMSClients)
    Source: dbo.tblFormsSammsClient or equivalent view (SAMMS / SAMMSGLOBAL)
    Key columns: fscsid, fscdate, fsccltid, fscsite, fscformid, fscform, clientsig, staffsig, supervisorsig, physiciansig, clientsigdate, staffsigdate, supervisorsigdate, physiciansigdate, doctext, nursesig, nursesigby, nursesigdate, physiciansigby, staffsigby, supervisorsigby, doctexteditdate, doctexteditby, guardiansig, guardiansigby, guardiansigdate, scanlink, scanreplace, adminnursesig, adminnursesigby, adminnursesigdate, lastmodat, rowchksum
    Note: All sig-image columns (clientsigimg, guardiansigimg, nursesigimg, physiciansigimg, staffsigimg, supervisorsigimg, adminnursesigimg, bac) are present in the switch but have empty bodies — image data is intentionally not extracted.

8.7 Consent Templates (SaveGlobalConsents / SaveGlobalConsentsPhc)
    Source: dbo.tblConsentTypes or equivalent (SAMMS / PHC SAMMSGLOBAL)
    Key columns: cid, cname, clientsig, staffsig, supervisorsig, physiciansig, nursesig, guardiansig, denyguardian, cdeleted, cdays, bac, ted, adminnursesig, blrecurr, ismhform

8.8 Devices (SaveGlobalDevices)
    Source: dbo.tblDevices or dbo.tblGlobalDevices (SAMMS)
    Key columns: sitecode, did, ddeviceid, dsid, dpumpnum, dcheckin, dpumptype, dtestmode, dlabel, dreceipt, dsigpad, ddispense, dfingerprint, dtouchscreen, dbacqueuepc

8.9 BAM (SaveBAM)
    Source: dbo.tblBriefAddictionMonitor or equivalent form view (SAMMS / SAMMSGLOBAL / PHC)
    Key columns: fid, fclinic, fcltid, date, cliniciantext, adminlist, intervallist, usecalc, riskcalc, protectivecalc, q1answerlist … q6answerlist, test, q1answer … q17answer, q7answernumeric, q7alist … q7glist, q14answer2, q15answer1, q15answer2 (17 individual answer fields + 7 list fields + 3 calculated scores)

8.10 Services (SaveServices)
    Source: dbo.tblService or dbo.tblServices (SAMMS, per-clinic)
    Key columns: sitecode, rowchksum, sid, sservice, sarea, scost, scptcode, sreqsig, sreqtime, blallowoverlap, oldarea, oldsrv, sfilter, sreportbillable, stimeonly

8.11 Form Counts (SaveFormCounts)
    Source: Aggregated GROUP BY query on SAMMSGLOBAL.dbo (hardcoded connection string to BHGDALLSQL05\SQL2016SAMMS / SAMMSGLOBAL)
    Columns: fscDate, fscsite, fscsid, fscCLTID, cnt (count of forms)

8.12 Claim Status (SaveClaimStatus)
    Source: 3P claim batch status table (external billing system / AdvMD or equivalent)
    Key columns: id, databasename, fileuploadstatus, tpcb837, tpcbdtcreated, tpcbFILE, tpcbID, tpcbStrSubmitType

________________________________________

9. Destination Table Schemas — Azure BHG_DR

9.1 pats.TblFeeSched
    Azure PK:   FsId + FsSite (composite; FsId is the fee schedule ID from SAMMS; FsSite is the normalised site code — "SAMMS" for shared-SAMMS clinics)
    ETL fields: FsId, FsSite, RowChkSum, IsActive, LastModAt, FsPayid, DsService, Cptcode, Fee, Contractual, Datespan, GroupTogether, Modifier, UnitMin, ProviderBill, CoAble, DefaultWeekFee, RevCode, Startdate, Enddate, Pos, AttendingBill, Pay2310A, Pay2310C, ReferredByAttending, BillAttendingOrder, BillOrderDoctor, FsMasterId, Notes1, Notes2
    Control:    IsActive — set to false for all existing records before processing; set to true on any record touched in the current run. RowChkSum guards column-level updates.

9.2 pats.TblGlobalPayor
    Azure PK:   PayId + SiteCode (composite; SiteCode normalised per ConnectionId)
    ETL fields: PayId, SiteCode, RowChkSum, RowState, LastModAt, PayName, PayAddress, PayCity, PaySt, Payzip, PayPh, PayFx, PayNote, PayDefaultsubmit, PayAuthformat, PayClmnum, Pay835, PaySubmitType, PayBillamt, PayDosetype, PayerNumber, Payaddressjoin, PayCheckAuth, Payclass, PayIndfreq, PayindRate, PayIndunit, PayLabelName, Paynamejoin, PayOverride, PayPos, Payregion, PayReqauth, Paysig, PayGlclass, PayLab, NoClaimLevelRendering, Enddate, Revcode, StartDate, Pay2310A, Pay2310B, Pay2310C, SupressSecondary, AltTaxId
    Control:    RowState — 0 set on all existing records before processing; 1 set on matched records. RowChkSum guards column-level updates.

9.3 ctrl.TblUser
    Azure PK:   Uskey (integer; the SAMMS user key — global across all SAMMS systems)
    ETL fields: Uskey, EmailId, IsDasacounselor, UsrActive, UsrCalendarUser, UsrCosig, UsrCounselor, Usrcred, UsrDea, UsrDescription, UsrDoctor, UsrExt, UsrFname, UsrGroups, UsrLicensed, UsrLname, UsrLocation, UsrName, Usrnpi, UsrPassword, UsrPasswordChanged, Usrphone, UsrPin, UsrRole, UsrSignature, UsrSignatureImage, UsrSsn, UsrSuper, UsrTaxonomy, UsrTemplate, Usrtemplatechanged, Usrxdea
    Control:    No RowChkSum; two-phase commit (UpdateRange then AddRange).

9.4 ctrl.TblUserSites
    Azure PK:   UsId (integer; user-site assignment ID)
    ETL fields: UsId, UsName, UsSite, UsDefault
    Control:    No RowChkSum; new records appended via newSites list (see anomaly — NewRow bug).

9.5 pats.TblClinicalOpiateWithdrawalScale
    Azure PK:   FId (form instance ID)
    ETL fields: FId, SiteCode, RowChkSum, RowState, LastModAt, FCltId, CompletedName, CombinedScore, AssessDate, ReasonAssessList, RestingPulseNum, GiupsetNum, SweatNum, TremorNum, RestlessNum, YawnNum, PupilNum, AnxNum, BoneNum, GooseNum, RunnyNum, GenevaTest, TimeAmpm, AssesstimeText
    Control:    RowState — all set to false before processing; re-set to true on matched rows. RowChkSum guards column-level updates. Site ID resolved via TblLocations lookup.

9.6 pats.TblFormsSammsclient
    Azure PK:   Fscsid (form-client session ID)
    ETL fields: Fscsid, FscDate, FscCltid, Fscsite, SiteCode, FscFormid, Fscform, ClientSig, StaffSig, SupervisorSig, PhysicianSig, ClientSigDate, StaffSigDate, SupervisorSigDate, PhysicianSigDate, Doctext, NurseSig, NurseSigBy, NurseSigDate, PhysicianSigBy, StaffSigBy, SupervisorSigBy, DoctextEditDate, DoctextEditBy, GuardianSig, GuardianSigBy, GuardianSigDate, ScanLink, ScanReplace, StaffSigImg (stub only), AdminNurseSig, AdminNurseSigBy, AdminnurseSigDate, RowChkSum, RowState, LastModAt
    Control:    RowState — set to 0 on all records for the run date at start; re-set to 1 on matched records. No RowChkSum guard — always overwrites matched records.

9.7 ctrl.TblConsents
    Azure PK:   Cid (consent form template ID)
    ETL fields: Cid, CName, ClientSig, StaffSig, SupervisorSig, PhysicianSig, NurseSig, GuardianSig, DenyGuardian, CDeleted, Cdays, Bac, Ted, AdminnurseSig, Blrecurr, IsMhform
    Control:    No RowChkSum; no RowState. Saves per-row (SaveChanges inside loop).

9.8 ctrl.TblConsentsPhc
    Azure PK:   Cid (same as above; separate table for PHC data)
    ETL fields: Same field set as TblConsents
    Control:    Same per-row SaveChanges approach as TblConsents.

9.9 ctrl.TblGlobalDevices
    Azure PK:   DId (device ID)
    ETL fields: DId, SiteCode, DDeviceid, DSid, DPumpnum, DCheckin, DPumptype, DTestmode, DLabel, DReceipt, DSigpad, DDispense, DFingerprint, DTouchScreen, DBacqueuePc
    Control:    No RowChkSum; two-phase commit (update existing, then AddRange new).

9.10 pats.TblBriefAddictionMonitor
    Azure PK:   FId + SiteCode (composite; same FId may exist across different clinic systems)
    ETL fields: FId, SiteCode, RowState, FCltId, FClinic, Date, ClinicianText, AdminList, IntervalList, UseCalc, RiskCalc, ProtectiveCalc, Q1answerList … Q6AnswerList (answer code lists), Q1Answer … Q17Answer (individual free-text answers), Q7answerNumeric, Q7aList … Q7gList, Q14Answer2, Q15Answer1, Q15Answer2
    Control:    RowState set to 1 on every write. No RowChkSum. Two paths: cold-start (first load for the date window) vs. warm-update. After save: stored procedure pats.BAMMergeGbl runs to aggregate data globally.

9.11 pats.TblServices
    Azure PK:   SId + SiteCode (composite)
    ETL fields: SId, SiteCode, RowChkSum, IsActive, CreatedOn, LastModAt, SService, SArea, SCost, SCptcode, SReqsig, SReqtime, BlAllowOverlap, OldArea, OldSrv, SFilter, SReportBillable, STimeOnly
    Control:    IsActive — set to false on all existing records before processing; set to true on matched records. RowChkSum guards column-level updates.

9.12 pats.TblFormsCounts
    Azure PK:   FscDate + SiteCode + fscsid + fscCltID (composite)
    ETL fields: FscDate, SiteCode, fscsid, fscCltID, Cnt
    Control:    No RowChkSum; per-row SaveChanges. Match uses Math.Abs(fscCltID) to handle negative client IDs (deleted patients).

9.13 ctrl.TblClaimStatuses
    Azure PK:   id (integer; claim batch status ID)
    ETL fields: id, DatabaseName, FileUploadStatus, tpcb837, tpcbDtCreated, tpcbFILE, tpcbID, tpcbStrSubmitType
    Control:    No RowChkSum; two-phase commit (update existing + AddRange new). Has structural bug — see Known Anomalies.

________________________________________

10. The Four Load Patterns

The 13 methods fall into four distinct load patterns based on how they handle existing records and detect changes.

──────────────────────────────────────────────────────────────────────────────
PATTERN 1 — Full-Snapshot / Pre-Deactivation Refresh
Methods: SaveFeeSchedules, SaveGlobalPayer, SaveGlobalClinicalOpiateWithdrawalScale, SaveServices
──────────────────────────────────────────────────────────────────────────────

These methods treat every run as a complete snapshot replacement. Before processing any source rows, they mark every existing Azure record for the site as inactive or deleted:

    SaveFeeSchedules:                        sets IsActive = false; RowChkSum = 0 on all existing records for fsSite
    SaveGlobalPayer:                         sets RowState = 0 on all existing records for siteCode
    SaveGlobalClinicalOpiateWithdrawalScale: sets RowState = false on all existing records
    SaveServices:                            sets IsActive = false on all existing records for sc (SiteCode)

Then, for every source row:
    - If the record does not exist in Azure, create it (insert path)
    - If the record exists, check RowChkSum — update only if changed
    - Either way, restore the active flag (IsActive=true / RowState=1/true)

At the end: any record that was NOT present in the source remains deactivated. This is a "ghost removal" approach — removed-at-source records become logically deleted in Azure without physically deleting rows.

──────────────────────────────────────────────────────────────────────────────
PATTERN 2 — Always-Update Upsert (No Change Detection)
Methods: SaveGlobalUser, SaveGlobalUserSite, SaveGlobalConsents, SaveGlobalConsentsPhc, SaveGlobalDevices
──────────────────────────────────────────────────────────────────────────────

These methods do not pre-deactivate and do not use RowChkSum to guard updates. For every source row, they look up the target by primary key and either:
    - Insert a new record if not found
    - Overwrite all mapped fields unconditionally if found

No soft-delete. No active/inactive flag. Every run fully overwrites matched records.

Commit strategy varies:
    SaveGlobalUser:      UpdateRange(users) then AddRange(newUsers) — two SaveChanges calls
    SaveGlobalDevices:   per-row update, then AddRange(newDevices) at end — two SaveChanges calls
    SaveGlobalConsents/Phc: db.SaveChanges() called inside the foreach row loop (per-row commit)
    SaveGlobalUserSite:  db.SaveChanges() once after the loop (but has the NewRow bug — see §16)

──────────────────────────────────────────────────────────────────────────────
PATTERN 3 — Date-Window Reset / Full-Replace for Time Slice
Methods: SaveGlobalFormsSAMMSClients, SaveBAM
──────────────────────────────────────────────────────────────────────────────

These methods do not touch records outside the active date window but treat records within the window as a complete snapshot:

    SaveGlobalFormsSAMMSClients: loads all records where FscDate == FltrDate. Sets RowState=0 for all of them at start. Re-processes the full day's source data. Inserts new records (AddRange), updates matched records (UpdateRange), updates soft-deleted records (RowState=0 remains).

    SaveBAM: loads records where Date >= FltrDate (FltrDate = WorkDate - 30 days). If no records exist (cold-start path), inserts all source rows. If records exist (warm-update path), upserts each row using FId + SiteCode composite key lookup. No pre-deactivation.

Neither method guards updates with RowChkSum — matched records are always fully overwritten.

──────────────────────────────────────────────────────────────────────────────
PATTERN 4 — Inline / Aggregated Save
Methods: SaveFormCounts, SaveClaimStatus
──────────────────────────────────────────────────────────────────────────────

SaveFormCounts is not called as a standalone task. It is called inline from within the pats.tbl_formssammsclient case in Program.cs, using a hardcoded aggregation query against the central SAMMSGLOBAL database. It upserts one row per (date, site, session, client) group.

SaveClaimStatus is called as a standard per-task method under Schedule 8 (SAMMS-ETL-INV). It has a structural bug in its update/insert detection logic (see §16).

________________________________________

11. Method-by-Method Logic

11.1 SaveFeeSchedules
─────────────────────────────────────────────────────────────────
Signature: SaveFeeSchedules(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Full-Snapshot / Pre-Deactivation Refresh
Schedule:  1 (SAMMSGlobal)
Target:    pats.TblFeeSched

SITE CODE NORMALISATION
The method first checks the clinic's ConnectionId from TblLocationCons. If ConnectionId == 2 (indicating a shared SAMMS system), the site code used for querying/writing is forced to "SAMMS" regardless of the actual SiteCode parameter. This merges all shared-SAMMS clinics into one fee schedule bucket in Azure.

PRE-RUN DEACTIVATION
All existing TblFeeSched records where FsSite == fsSite and IsActive == true are set to IsActive = false and RowChkSum = 0. db.SaveChanges() is called immediately. This means any record NOT in the current source batch will remain inactive.

PROCESSING LOOP
For each source row:
  1. Parse fsid (fee schedule line ID) and myrcs (RowChkSum from source).
  2. Look up the existing record in the pre-loaded in-memory list.
  3. If not found: create new TblFeeSched; set NewRow=true; RowsIns++.
  4. If found: RowsUpd++.
  5. If RowChkSum differs OR new row: execute the dynamic column switch to map all fields.
  6. Set IsActive=true and LastModAt=DateTime.Now.
  7. If new row: call db.TblFeeSched.Add(fs).

COMMIT
Single db.SaveChanges() after all rows processed.

NOTE: Setting RowChkSum = 0 on pre-deactivation and then guarding the column-level update with (fs.RowChkSum != myrcs) means that all existing records will always be fully updated on every run — the pre-set of 0 guarantees a mismatch. This effectively makes the RowChkSum guard non-functional for records that existed before. New records are handled via the NewRow flag.

──────────────────────────────────────────────────────────────────────────────

11.2 SaveGlobalPayer
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalPayer(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Full-Snapshot / Pre-Deactivation Refresh
Schedule:  1 (SAMMSGlobal)
Target:    pats.TblGlobalPayor

SITE CODE NORMALISATION
More complex than fee schedules. A switch on ConnectionId determines siteCode:
    ConnectionId 2  → "SAMMS"     (shared SAMMS system)
    ConnectionId 4  → "AdvMD-AR"  (AdvMD AR billing system)
    any other       → the actual sc value

Additionally, a hardcoded special case overrides the above for site B59A:
    if (sc == "B59A") { siteCode = "AdvMD-NF"; }

This means B59A always writes to the "AdvMD-NF" payer bucket regardless of ConnectionId.

PRE-RUN DEACTIVATION
All existing TblGlobalPayor records for siteCode where RowState == 1 are set to RowState = 0. No SaveChanges at this point (deactivation is tracked in-memory).

PROCESSING LOOP
For each source row:
  1. Parse PayID and myrcs.
  2. If AllNewRows: create new record; set NewRow=true; RowsIns++.
  3. Else: look up by PayId; if not found, create new record; if found, RowsUpd++.
  4. If RowChkSum differs OR new row: map all fields via switch.
  5. Set LastModAt and RowState=1.
  6. If new row: call db.TblGlobalPayor.Add(gp).

COMMIT
Single db.SaveChanges() after all rows processed. Note: RowState=0 pre-deactivation was applied to the in-memory list and will be committed here alongside the updates.

──────────────────────────────────────────────────────────────────────────────

11.3 SaveGlobalUser
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalUser(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Always-Update Upsert
Schedule:  1 (SAMMSGlobal)
Target:    ctrl.TblUser

DESIGN
Users are global — there is no SiteCode per user. The method loads all existing TblUser records into memory (db.TblUser.ToList()) and maintains a separate newUsers list.

PROCESSING LOOP
For each source row:
  1. Guard: skip if r["uskey"].ToString().Length == 0 (null/empty user key).
  2. Parse ukey (UsKey integer primary key).
  3. Look up in memory: if not found, create new TblUser and add to newUsers; RowsIns++.
  4. If found: RowsUpd++.
  5. Map all fields directly (no column switch — direct field assignment using known column names).
  6. Signature image: if usrsignatureimage has length > 0, stored as Encoding.ASCII.GetBytes().

NOTE: The userfullname field is commented out (not extracted). This field does not exist in the current source or was removed from the mapping.

TWO-PHASE COMMIT
    db.TblUser.UpdateRange(users)  — marks all existing users for update
    db.SaveChanges()               — commits all updates
    if (newUsers.Count > 0): db.TblUser.AddRange(newUsers); db.SaveChanges()

──────────────────────────────────────────────────────────────────────────────

11.4 SaveGlobalUserSite
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalUserSite(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Always-Update Upsert (with NewRow bug — see §16)
Schedule:  1 (SAMMSGlobal)
Target:    ctrl.TblUserSites

DESIGN
User-site assignments are global. Loads all existing TblUserSites into memory. Maintains a newSites list for insertions. Direct field assignment (no column switch).

PROCESSING LOOP
For each source row:
  1. Guard: skip if r["usid"].ToString().Length == 0.
  2. Parse usid.
  3. Look up in memory by UsId; if not found, create new TblUserSites; RowsIns++.
  4. If found: RowsUpd++.
  5. Map UsName, UsSite, UsDefault directly.
  6. If NewRow: add to newSites and reset NewRow to false.

BUG — NewRow never set to true: The local variable NewRow is declared as false and never set to true anywhere in the method. The check `if (NewRow) { newSites.Add(s); }` will therefore never execute. New user-site assignments are never added to newSites, and because db.SaveChanges() before the AddRange call only commits updates to the pre-loaded list, truly new records are never persisted to Azure. See §16 for full analysis.

COMMIT
db.SaveChanges() after the loop (commits updates to pre-loaded records).
if (newSites.Count > 0): db.TblUserSites.AddRange(newSites); db.SaveChanges()
— this second block never executes due to the NewRow bug.

──────────────────────────────────────────────────────────────────────────────

11.5 SaveGlobalClinicalOpiateWithdrawalScale
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalClinicalOpiateWithdrawalScale(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Full-Snapshot / Pre-Deactivation Refresh + RowChkSum guard
Schedule:  1 (SAMMSGlobal)
Target:    pats.TblClinicalOpiateWithdrawalScale

PRE-RUN DEACTIVATION
All existing TblClinicalOpiateWithdrawalScale records are set to RowState = false (soft-delete all). No SaveChanges yet — this is in-memory.

SITE LOOKUP
The source provides SiteCode as an integer (SId). For each row, the method resolves the integer to the string SiteCode by looking up TblLocations (pre-loaded, active sites only). If the integer SId does not exist in TblLocations, the row is skipped entirely (no processing, no error).

PROCESSING LOOP (only for rows with a valid site):
  1. Parse fid, sid (integer site code), and rcs (RowChkSum).
  2. Resolve sid to SiteCode string via TblLocations. Skip if not found.
  3. Look up existing TblClinicalOpiateWithdrawalScale by FId.
  4. If not found: create new record; add to in-memory list and to db; RowsIns++.
  5. If found: RowsUpd++.
  6. RowChkSum guard: if rcs != c.RowChkSum, execute column switch and update all fields.
  7. On change: set c.RowChkSum = rcs; c.RowState = true; c.LastModAt = DateTime.Now; c.SiteCode = site.SiteCode.
  8. If NO change: c.RowState = true (re-activate even without data change).

This is the only method in SaveGlobal.cs that has a genuine, effective RowChkSum guard: rows whose data has not changed since the last run are re-activated (RowState=true) but their fields are not re-mapped. This is the intended change-detection pattern.

COMMIT
Single db.SaveChanges() after all rows processed.

──────────────────────────────────────────────────────────────────────────────

11.6 SaveGlobalFormsSAMMSClients
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalFormsSAMMSClients(DataTable tbl, string sc, DateTime FltrDate, bool firsthalf, Models.BHG_DRContext db)
Pattern:   Date-Window Reset / Full-Replace for Time Slice
Schedule:  1 (SAMMSGlobal) — but currently the BulkDartsSrvLoader path is active in production
Target:    pats.TblFormsSammsclient

NOTE ON PRODUCTION STATUS
In BHGTaskRunner\Program.cs, the call to sd.SaveGlobalFormsSAMMSClients() is commented out. The production path uses BulkDartsSrvLoader (SqlBulkCopy into staging) instead. This method remains functional code for per-clinic or fallback scenarios.

DATE WINDOW
The method receives FltrDate (WorkDate + DaysBack from Program.cs). It loads all existing TblFormsSammsclient records for that exact date (FscDate.Date == FltrDate.Date) into adata, and immediately sets RowState=0 on all of them (marking the entire day's data as stale).

SITE LOOKUP
For each source row, the integer fscsite field is resolved to a SiteCode string via TblLocations. If the lookup fails, SiteCode is defaulted to "SAMMS".

PROCESSING LOOP
A new TblFormsSammsclient object `n` is built for every source row via the column switch. LastModAt is set to a single RunDT timestamp captured before the loop (consistent timestamps).

After building `n`:
  1. Look up xn = adata.Where(x => x.Fscsid == n.Fscsid).
  2. If not found: add to addList; RowsIns++.
  3. If found: copy all fields from n to xn; set xn.RowState=1; add to updList; RowsUpd++.

SOFT-DELETE RESIDUAL
After processing, any adata record that was NOT in the source retains RowState=0. These are explicitly updated back to Azure:
    dlt = adata.Where(x => x.RowState == 0)  → UpdateRange(dlt)  (persists the soft-delete)

TWO-PHASE COMMIT
    AddRange(addList) for new records
    UpdateRange(updList) for matched records
    UpdateRange(dlt) for soft-deleted residuals
    Single db.SaveChanges()

──────────────────────────────────────────────────────────────────────────────

11.7 SaveGlobalConsents
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalConsents(DataTable tbl, Models.BHG_DRContext db)
Pattern:   Always-Update Upsert
Schedule:  1 (SAMMSGlobal) — SiteCode = "SAMMS" (global consent template list)
Target:    ctrl.TblConsents

DESIGN
Consent templates are a global reference table — no SiteCode parameter. The method loads all existing TblConsents into memory (dbConsents), though this list is not actually used for lookups (the lookup is done directly against db.TblConsents per row). This is a minor inefficiency.

PROCESSING LOOP
For each source row:
  1. Build a new TblConsents object nd via the column switch.
  2. Look up xd = db.TblConsents.Where(x => x.Cid == nd.Cid) — live database query per row.
  3. If not found: db.TblConsents.Add(nd); RowsIns++.
  4. If found: copy all fields from nd to xd; RowsUpd++.
  5. db.SaveChanges() — called inside the loop after each row.

PER-ROW COMMIT
Unlike most methods, db.SaveChanges() is inside the foreach loop. This means every row is committed individually. This provides isolation (one failed row does not roll back others) but has poor performance for large datasets.

──────────────────────────────────────────────────────────────────────────────

11.8 SaveGlobalConsentsPhc
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalConsentsPhc(DataTable tbl, Models.BHG_DRContext db)
Pattern:   Always-Update Upsert
Schedule:  1 (SAMMSGlobal) — SiteCode = "PHC" (routed when st.SiteCode == "PHC")
Target:    ctrl.TblConsentsPhc

DESIGN
This method is structurally identical to SaveGlobalConsents. The only differences are:
    - Target table: db.TblConsentsPhc instead of db.TblConsents
    - Model type: Models.TblConsents_Phc instead of Models.TblConsents

In Program.cs, routing is:
    if (st.SiteCode == "PHC") → sd.SaveGlobalConsentsPhc(SrcDt, null)
    else                      → sd.SaveGlobalConsents(SrcDt, null)

Both methods process the same column set (cid, cname, clientsig, staffsig, etc.) and use the same per-row SaveChanges pattern.

──────────────────────────────────────────────────────────────────────────────

11.9 SaveGlobalDevices
─────────────────────────────────────────────────────────────────
Signature: SaveGlobalDevices(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Always-Update Upsert
Schedule:  1 (SAMMSGlobal)
Target:    ctrl.TblGlobalDevices

DESIGN
Loads all existing TblGlobalDevices into memory (dbGblDevices). Maintains newDevices list for insertions. Loads TblLocations for integer SId → SiteCode resolution.

PROCESSING LOOP
For each source row:
  1. Build a new TblGlobalDevices object gd via the column switch.
  2. Notable: case "sitecode" sets gd.SiteCode = "unk" (hardcoded; see §16 — Devices SiteCode bug).
  3. case "dsid" parses the integer site ID and resolves to SiteCode via TblLocations lookup. If found: gd.SiteCode = lc.SiteCode. This overwrites the "unk" placeholder.
  4. After the column switch: look up cgd by DId in dbGblDevices.
  5. If not found: newDevices.Add(gd); RowsIns++.
  6. If found: copy all device fields from gd to cgd; RowsUpd++.

TWO-PHASE COMMIT
    db.SaveChanges() — commits all field updates to existing devices
    if (newDevices.Count > 0): db.TblGlobalDevices.AddRange(newDevices); db.SaveChanges()

NOTE: For devices where DSid is null or empty, SiteCode remains "unk". No error is raised.

──────────────────────────────────────────────────────────────────────────────

11.10 SaveBAM
─────────────────────────────────────────────────────────────────
Signature: SaveBAM(DataTable tbl, DateTime FltrDate, Models.BHG_DRContext db)
Pattern:   Date-Window Reset / Two-Path Design
Schedule:  1 (SAMMSGlobal) — case "pats.tbl_briefaddictionmonitor" in Program.cs
Target:    pats.TblBriefAddictionMonitor
Post-save: stored procedure pats.BAMMergeGbl executes after SaveBAM returns

DATE FILTER
Program.cs passes FltrDate = task.WorkDate.Value.AddDays(-30).Date. The BAM source query in Program.cs also uses -30 days as the WHERE condition. Azure records loaded are those with Date >= FltrDate.

Two-path design: if (Bams.Count() == 0) — no existing Azure records for the date window.

PATH A — Cold Start (no existing Azure records):
For each source row: build a BAM object, set RowState=1, map all columns.

DATE PARSING (used in both paths):
The Date column in SAMMS is stored as a formatted string like "Monday, January 1, 2024". The method uses substring parsing to extract the date:
    int i = r["date"].ToString().IndexOf(", ") + 2;
    int l = r["date"].ToString().Length - i;
    string sd = r["date"].ToString().Substring(i, l);
    bam.Date = DateTime.Parse(sd.Trim());
This strips the day-of-week prefix by finding the second comma and taking everything after it.

SITE LOOKUP (used in both paths):
FClinic (integer) is resolved to SiteCode via TblLocations. If not found: SiteCode = "NSL-" + FClinic.ToString(). The "NSL-" prefix marks unresolved sites.

For each row in Path A:
  1. Build bam object. Check bam.Date >= FltrDate.Date (date guard).
  2. Look up by SiteCode + FId in db.TblBriefAddictionMonitor (live database query per row).
  3. If not found: db.TblBriefAddictionMonitor.Add(bam); RowsIns++.
  4. If found: copy all fields; RowsUpd++.
  5. db.SaveChanges() called per-row (inside inner loop).

PATH B — Warm Update (existing Azure records present):
Identical column mapping and site resolution. Differences:
  1. No per-row date guard (assumes source already filtered).
  2. Live db lookup by SiteCode + FId per row.
  3. If not found: db.TblBriefAddictionMonitor.Add(bam); RowsIns++.
  4. If found: copy all fields; RowsUpd++.
  5. db.SaveChanges() after the full row loop (not per-row).

POST-SAVE STORED PROCEDURE
After SaveBAM returns, Program.cs calls:
    sm.ExecStrPro("pats.BAMMergeGbl", "@sitecode", "Global", sm.ConnectionString)
This stored procedure aggregates BAM data across all clinic site codes into a global summary.

──────────────────────────────────────────────────────────────────────────────

11.11 SaveServices
─────────────────────────────────────────────────────────────────
Signature: SaveServices(DataTable tbl, string sc, Models.BHG_DRContext db)
Pattern:   Full-Snapshot / Pre-Deactivation Refresh + RowChkSum guard
Schedule:  1 (SAMMSGlobal) — case "pats.tbl_services" in Program.cs
Target:    pats.TblServices

PRE-RUN DEACTIVATION
All existing TblServices records for the clinic (SiteCode == sc) where IsActive == true are set to IsActive = false. No SaveChanges at this point.

AllNewRows flag is used: if services.Count == 0, skip the deactivation loop and set AllNewRows=true.

PROCESSING LOOP
For each source row, a new TblServices object s is built via the column switch. The SiteCode is always forced to sc (the parameter), not the value from the source row.

If AllNewRows:
    s.CreatedOn = DateTime.Now; s.LastModAt = s.CreatedOn
    db.TblServices.Add(s); RowsIns++

Else:
    Look up svc in services list (SiteCode + SId).
    If not found: s.CreatedOn = ...; s.LastModAt = ...; s.IsActive = true; services.Add(s); RowsIns++    (Note: does NOT call db.Add — see §16)
    If found AND RowChkSum changed: copy all service fields; svc.LastModAt = DateTime.Now
    Either way: svc.IsActive = true; RowsUpd++

EFFECTIVE RowChkSum GUARD
Unlike SaveFeeSchedules (which zeroed RowChkSum on deactivation), SaveServices does NOT reset RowChkSum during pre-deactivation. This means the RowChkSum check `if (svc.RowChkSum != s.RowChkSum)` is genuine — only rows with changed data are fully updated. IsActive is always restored to true.

COMMIT
Single db.SaveChanges() after all rows processed.

ANOMALY — New record not added to db
When a new service is found (AllNewRows == false AND not in services list), the code sets services.Add(s) and RowsIns++ but does not call db.TblServices.Add(s). The final SaveChanges will not persist the new record because it was never added to the EF Core change tracker. This appears to be a bug. New services added at a clinic that already has services will be silently dropped. See §16.

──────────────────────────────────────────────────────────────────────────────

11.12 SaveFormCounts
─────────────────────────────────────────────────────────────────
Signature: SaveFormCounts(DataTable tbl, Models.BHG_DRContext db)
Pattern:   Inline Aggregated Save
Schedule:  1 (SAMMSGlobal) — called inline from case "pats.tbl_formssammsclient" in Program.cs
Target:    pats.TblFormsCounts

INVOCATION
Not called as a standalone task. Program.cs hardcodes a GROUP BY aggregation query and a direct connection string to BHGDALLSQL05\SQL2016SAMMS / SAMMSGLOBAL:

    string mydb = @"Data Source=BHGDALLSQL05\SQL2016SAMMS;Initial Catalog=SAMMSGLOBAL;Connection Timeout=60;Integrated Security=True;Encrypt=False";
    _ = sd.SaveFormCounts(sm.GetTableData("frms", scmd, mydb), null);

The aggregation query groups by fscsid, fscDATE, fscCLTID, fscsite having fscDATE >= '1/1/2021'. The result is passed directly to SaveFormCounts. The return value is discarded (_ =).

PROCESSING LOOP
For each source row:
  1. Parse dt (date), sc (integer site code), sid, clt (client ID), cnt (count).
  2. Resolve integer site to string SiteCode via TblLocations. Skip row if not found.
  3. Build composite lookup key: FscDate == dt AND SiteCode == lc.SiteCode AND fscsid == sid AND Math.Abs(fscCltID) == Math.Abs(clt).
  4. Math.Abs handles negative client IDs (deleted patients in SAMMS have negative cltid).
  5. If not found: create new TblFormsCounts; db.TblFormsCounts.Add(frm); RowsIns++.
  6. If found: update frm.Cnt and frm.fscCltID (with the raw potentially-negative clt value).
  7. db.SaveChanges() per-row (inside loop).

──────────────────────────────────────────────────────────────────────────────

11.13 SaveClaimStatus
─────────────────────────────────────────────────────────────────
Signature: SaveClaimStatus(DataTable tbl, DateTime wrkdt, Models.BHG_DRContext db)
Pattern:   Always-Update Upsert (with structural bug)
Schedule:  8 (SAMMS-ETL-INV) — case "ctrl.tbl_claimstatus" in Program.cs
Target:    ctrl.TblClaimStatuses

DATE SCOPE
Program.cs applies a 12-month lookback on the source query:
    WHERE tpcbdtcreated >= WorkDate.AddMonths(-12)
All claim batch records created within the last 12 months are extracted and upserted.

DESIGN
Loads all existing TblClaimStatuses into memory (dbCS). Maintains newCS list for insertions.

STRUCTURAL BUG — Lookup inside column loop
The upsert check and update code appears inside the foreach DataColumn loop, not outside it:

    foreach (DataRow r in tbl.Rows)
    {
        foreach (DataColumn c in tbl.Columns)
        {
            switch (c.ColumnName.ToLower())  { ... map one field ... }

            var xcs = dbCS.FirstOrDefault(x => x.id == cs.id);  // ← INSIDE column loop
            if (xcs == null) { newCS.Add(cs); rc.RowsIns += 1; }
            else             { rc.RowsUpd += 1; xcs.DatabaseName = ...; }
        }
    }

This means for every column in the DataTable, the lookup and upsert logic runs once. For a row with N columns, the code will attempt to add the row to newCS up to N times before cs.id is fully populated, call rc.RowsIns += 1 or rc.RowsUpd += 1 N times per row, and run duplicate update assignments to xcs on every column iteration. In practice, because the first columns parsed (id is the first case) fill cs.id early, the lookup will find xcs after the first few columns. Duplicate additions to newCS are bounded. However, RowsIns and RowsUpd counters will be inflated by the column count. The final AddRange and SaveChanges at the outer level still operate correctly on the populated lists. See §16.

COMMIT
    db.SaveChanges() after the main row loop.
    if (newCS.Count > 0): db.TblClaimStatuses.AddRange(newCS); db.SaveChanges()

________________________________________

12. RowChkSum — Change Detection

RowChkSum in SaveGlobal.cs methods varies significantly in its effectiveness:

EFFECTIVE — SaveGlobalClinicalOpiateWithdrawalScale, SaveServices
    RowChkSum is NOT zeroed during pre-deactivation. The guard `if (rcs != c.RowChkSum)` is genuine. Rows with unchanged data are re-activated but not re-mapped. This is the intended pattern.

NEUTRALISED BY PRE-DEACTIVATION — SaveFeeSchedules
    RowChkSum is set to 0 during the pre-deactivation loop before processing. The guard `if ((fs.RowChkSum != myrcs) || (NewRow))` will always be true for existing records because 0 never matches the source myrcs. All existing records are fully re-written on every run. The guard is effectively non-functional.

NOT USED — SaveGlobalUser, SaveGlobalUserSite, SaveGlobalConsents, SaveGlobalConsentsPhc, SaveGlobalDevices, SaveFormCounts, SaveClaimStatus
    These methods perform unconditional overwrites. No RowChkSum comparison.

STORED BUT NOT GUARDING — SaveGlobalPayer, SaveGlobalFormsSAMMSClients
    RowChkSum is stored in the target table as an audit trail but is not used to skip updates. All rows within scope are fully re-processed on every run.

________________________________________

13. RowState and IsActive — Soft-Delete

Three control fields are used across the 13 methods to implement soft-delete semantics:

RowState (integer 0/1) used in: SaveGlobalPayer, SaveGlobalFormsSAMMSClients, SaveBAM
    0 = logically deleted / inactive; 1 = active
    SaveGlobalPayer and SaveGlobalFormsSAMMSClients set RowState=0 before processing, then restore to 1 for matched records. SaveBAM always sets RowState=1 on write.

RowState (boolean false/true) used in: SaveGlobalClinicalOpiateWithdrawalScale
    false = logically deleted; true = active
    All records set to false before processing; restored to true for matched records.

IsActive (boolean) used in: SaveFeeSchedules, SaveServices
    false = inactive; true = active
    All matching site records set to false before processing; restored to true for matched records.

NOT USED — SaveGlobalUser, SaveGlobalUserSite, SaveGlobalConsents, SaveGlobalConsentsPhc, SaveGlobalDevices, SaveFormCounts, SaveClaimStatus
    No soft-delete mechanism. Records are only inserted or updated. There is no way to detect or flag records that have been removed at the source.

________________________________________

14. Load Scoping and Date Windows

FULL-SCOPE (no date filter at Azure query level):
    SaveFeeSchedules, SaveGlobalPayer, SaveGlobalUser, SaveGlobalUserSite, SaveGlobalClinicalOpiateWithdrawalScale, SaveGlobalConsents, SaveGlobalConsentsPhc, SaveGlobalDevices, SaveServices, SaveClaimStatus
    → All records for the site/scope are loaded into memory; source provides the complete set.

DATE-WINDOWED:
    SaveGlobalFormsSAMMSClients → Azure scope: FscDate == FltrDate (single day)
    SaveBAM                     → Azure scope: Date >= FltrDate (WorkDate - 30 days)
    SaveFormCounts              → Source scope: fscDATE >= '1/1/2021' (hardcoded since 2021)
    SaveClaimStatus             → Source scope: tpcbdtcreated >= WorkDate.AddMonths(-12)

________________________________________

15. Error Handling

All 13 methods wrap their core logic in a try/catch(Exception e). On exception:
    rc.IsResult = false
    rc.ExceptMsg = e.Message
    rc.ExceptInnerMsg = e.InnerException.Message (if InnerException != null)
    Console.WriteLine(e.Message) / Console.WriteLine(e.InnerException.Message)

The method returns the RCodes object to Program.cs. BHGTaskRunner reads IsResult=false and logs the failure. There is no retry mechanism at the method level.

Important: most methods use a single SaveChanges() after the row loop, so an exception during SaveChanges rolls back all changes for that batch. SaveGlobalConsents, SaveGlobalConsentsPhc, SaveBAM (Path A), and SaveFormCounts call SaveChanges() inside the row loop — an error on row N does not roll back rows 1..N-1 (partial commit).

Some methods have per-field null/empty guards (if r["field"].ToString().Length > 0) to prevent parse exceptions on empty strings for numeric, boolean, and datetime fields. These guards are present but not consistent across all methods — SaveGlobalUser notably uses direct field assignments without Length guards for several string fields (null SAMMS strings become empty strings in .ToString(), which is safe for string assignments but not for numeric/bool parses).

________________________________________

16. Known Anomalies and Quirks

ANOMALY 1 — SaveGlobalUserSite: NewRow bug — new records never inserted
    Location: Lines 529, 557-561
    The local variable `bool NewRow = false` is declared but never set to true. The block `if (NewRow) { NewRow = false; newSites.Add(s); }` never executes. As a result, new user-site assignments are never added to newSites, and the AddRange at the end never runs. Only updates to existing TblUserSites records are persisted.
    Impact: Any user-site assignment that does not already exist in Azure will be silently lost.
    Fix: Set `NewRow = true` when `s = new TblUserSites { UsId = usid }` is created.

ANOMALY 2 — SaveGlobalDevices: SiteCode hardcoded to "unk" in switch case
    Location: Lines 1306-1308
    case "sitecode": gd.SiteCode = "unk";
    The intent of this case block is unclear — the actual SiteCode is correctly resolved from DSid in the case "dsid" block below it. The "sitecode" case appears to be either a leftover stub or was intended to prevent the source SiteCode from being used directly (since the source provides an integer SiteCode, not a string). Because "dsid" overwrites SiteCode with the resolved value, the "unk" is only persistent for records where DSid is null or absent.

ANOMALY 3 — SaveFeeSchedules: RowChkSum zeroed during pre-deactivation renders guard non-functional
    Location: Lines 36-38
    f.IsActive = false; f.RowChkSum = 0;
    Setting RowChkSum to 0 means the subsequent check `if ((fs.RowChkSum != myrcs) || (NewRow))` will always evaluate to true for any existing record (source myrcs is never 0 for a real row). The RowChkSum guard is effectively disabled — all records are always fully re-processed. This means SaveFeeSchedules performs a full refresh on every run, not a selective update.

ANOMALY 4 — SaveServices: New records under existing-site path not added to EF change tracker
    Location: Lines 2011-2018
    When AllNewRows == false and a new service SId is not found in the services list, the code sets services.Add(s) and RowsIns++ but does not call db.TblServices.Add(s). The call db.TblServices.Add(s) is commented out (line 2018: //db.TblServices.Add(s)). Without db.Add(), EF Core does not track this new entity. The final db.SaveChanges() will not persist it. New services at existing-site clinics are silently dropped.
    Impact: New service types added in SAMMS for a clinic that already has services in Azure will never appear in Azure TblServices until AllNewRows is true (i.e., the Azure data is cleared first).

ANOMALY 5 — SaveClaimStatus: Upsert logic inside foreach DataColumn loop
    Location: Lines 2167-2183
    The xcs lookup, the newCS.Add(cs), and the field-copy update block are all inside the foreach DataColumn loop (not after it). For each row, this logic executes N times (once per column). rc.RowsIns and rc.RowsUpd will be inflated by approximately the column count (~7 columns). For new records, cs.id is not populated until the "id" column is processed; iterations before the "id" column is hit will find xcs == null and attempt to add cs (with id=0) to newCS. For existing records, all field-copy assignments run once per column iteration (redundant but harmless since the last value wins). The final AddRange + SaveChanges still operates on the populated newCS list correctly if duplicate entries resolve to the same cs object. However, the logic is fragile and incorrect.
    Fix: Move the xcs lookup and upsert block outside the foreach DataColumn loop.

ANOMALY 6 — SaveBAM: db.SaveChanges() called per-row in Path A (cold start)
    Location: Line 1662
    In Path A (Bams.Count() == 0), db.SaveChanges() is called inside the nested row loop. Additionally, db.SaveChanges() is called again after the loop (line 1664). This results in N+1 SaveChanges calls for N rows in a cold-start load. Performance degrades linearly with row count. Path B calls SaveChanges only once after the loop.
    Impact: Cold-start BAM loads for large datasets (30-day windows) will be significantly slower than warm-update loads.

ANOMALY 7 — SaveGlobalFormsSAMMSClients: Image columns silently skipped
    Location: Lines 861-873
    All signature image case blocks (clientsigimg, guardiansigimg, nursesigimg, physiciansigimg, staffsigimg, supervisorsigimg) have empty bodies. Signature image binary data from SAMMS is not extracted to Azure. The columns are mapped in the switch to prevent the default fall-through but the field is not actually stored. In the update path, xn.StaffSigImg = n.StaffSigImg is present — but n.StaffSigImg will always be null since it is never set. This is intentional (image data excluded by design).

ANOMALY 8 — SaveGlobalUser: userfullname commented out
    Location: Line 447
    //user.Userfullname = r["userfullname"].ToString();
    The full name field is commented out. The user's full name is not captured in Azure TblUser. It can be reconstructed from UsrFname + UsrLname if needed, but is not directly available.

________________________________________

17. End-to-End Flow Diagram

SaveGlobal methods span two BHGTaskRunner schedules: most run under Schedule 1 (SAMMSGlobal) with ActionKey = 1 in dms.tbl_MapSrc2Dsn. SaveClaimStatus alone runs under Schedule 8 (SAMMS-ETL-INV) with ActionKey = 8 — the same Program.cs switch body is shared, but tasks are filtered by TaskName before execution so Claim Status never runs with exe 1.

Windows Task Scheduler
        |
        | (daily, overnight)
        V
Scheduler.exe
        |
        |-- reset stuck tasks (Status 18 → 17 where applicable)
        |-- insert parent task rows per schedule (e.g. SAMMSGlobal, SAMMS-ETL-INV)
        |-- insert child tasks per clinic × global destination table:
        |       pats.tbl_feesched, pats.tbl_globalpayor, ctrl.tbl_user, ctrl.tbl_usersites,
        |       pats.tbl_clinicalopiatewithdrawalscale, pats.tbl_formssammsclient,
        |       ctrl.tbl_globalconsents, ctrl.tbl_globaldevices,
        |       pats.tbl_briefaddictionmonitor, pats.tbl_services, ctrl.tbl_claimstatus, ...
        |-- each task: Status=17 (ready), RunAt, DsnTbl, FromTblVw, ConStr, WhereCondition, WorkDate
        |-- advance tsk.tbl_Schedule NextRunTime += 1 day
        |
        V
tsk.tbl_Tasks / vw_TaskListMap (Azure BHG_DR)
        |
        | (when RunAt < now and Status=17)
        V
+================================================================================+
|  BRANCH A — SCHEDULE 1 — SAMMSGLOBAL (12 of 13 SaveGlobal methods)              |
+================================================================================+
        |
        V
BHGTaskRunner.exe 1
        |
        |-- filter: TaskName = 'SAMMSGlobal', SiteCode != 'PHC', Status=17, RunAt < now
        |-- parent task may be marked running (Status=18) per Program.cs pattern
        |
        |-- for each child task st:
        |
        |       get column mappings from dms.tbl_MapSrc2Dsn (ActionKey=1)
        |       SelectConstructor builds base SELECT: columns + CHECKSUM(...) AS RowChkSum
        |               FROM {st.FromTblVw}
        |
        |       strCmd += st.WhereCondition + st.SortOrder (some tasks add hardcoded WHERE in Program.cs)
        |
        |       V
        |   SQLSvrManager.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |       |-- connection from ctrl.tbl_LocationCons for this SiteCode
        |       |-- PHC SAMMSGLOBAL / PHCSQLVM used for BAM or Consents when st.SiteCode == 'PHC'
        |       V
        |   DataTable SrcDt (in-memory rows from SAMMS)
        |       |
        |       +--[switch st.DsnTbl.ToLower() — routes to SaveData method]
        |       |
        |       +--[pats.tbl_feesched]
        |       |       SaveFeeSchedules(SrcDt, st.SiteCode, null)
        |       |       → pre-deactivate IsActive; RowChkSum zeroed (guard ineffective); upsert → db.SaveChanges()
        |       |       V
        |       |   Azure pats.TblFeeSched
        |       |
        |       +--[pats.tbl_globalpayor]
        |       |       SaveGlobalPayer(SrcDt, st.SiteCode, null)
        |       |       → normalise SiteCode; RowState=0 pre-pass; RowChkSum guard; upsert → SaveChanges()
        |       |       V
        |       |   Azure pats.TblGlobalPayor
        |       |
        |       +--[ctrl.tbl_user]
        |       |       SaveGlobalUser(SrcDt, st.SiteCode, null)
        |       |       → UpdateRange + SaveChanges + AddRange(newUsers) + SaveChanges()
        |       |       V
        |       |   Azure ctrl.TblUser
        |       |
        |       +--[ctrl.tbl_usersites]
        |       |       SaveGlobalUserSite(SrcDt, st.SiteCode, null)
        |       |       → SaveChanges (⚠ NewRow bug — inserts never reach AddRange)
        |       |       V
        |       |   Azure ctrl.TblUserSites
        |       |
        |       +--[pats.tbl_clinicalopiatewithdrawalscale]
        |       |       SaveGlobalClinicalOpiateWithdrawalScale(SrcDt, st.SiteCode, null)
        |       |       → RowState=false all; TblLocations SId→SiteCode; genuine RowChkSum guard → SaveChanges()
        |       |       V
        |       |   Azure pats.TblClinicalOpiateWithdrawalScale
        |       |
        |       +--[pats.tbl_formssammsclient]
        |       |       PRODUCTION: BulkDartsSrvLoader(SrcDt, "stg.tbl_formssammsclient", ...) → MERGE stored procs → pats.TblFormsSammsclient
        |       |       FALLBACK (commented): SaveGlobalFormsSAMMSClients(..., FltrDate, ...) → pats.TblFormsSammsclient
        |       |       INLINE (same case): SaveFormCounts(sm.GetTableData("frms", scmd, SAMMSGLOBAL hardcoded conn), null) → pats.TblFormsCounts
        |       |       V
        |       |   Azure pats.TblFormsSammsclient (+ TblFormsCounts via inline aggregation)
        |       |
        |       +--[ctrl.tbl_globalconsents]
        |       |       if st.SiteCode == 'PHC' → SaveGlobalConsentsPhc(SrcDt, null)
        |       |       else                  → SaveGlobalConsents(SrcDt, null)
        |       |       → per-row SaveChanges inside loop
        |       |       V
        |       |   Azure ctrl.TblConsents or ctrl.TblConsentsPhc
        |       |
        |       +--[ctrl.tbl_globaldevices]
        |       |       SaveGlobalDevices(SrcDt, st.SiteCode, null)
        |       |       → SaveChanges + AddRange(newDevices) + SaveChanges()
        |       |       V
        |       |   Azure ctrl.TblGlobalDevices
        |       |
        |       +--[pats.tbl_briefaddictionmonitor]
        |       |       SaveBAM(SrcDt, task.WorkDate.AddDays(-30).Date, null)
        |       |       → cold vs warm path; then Program.cs: ExecStrPro("pats.BAMMergeGbl", "@sitecode", "Global", ...)
        |       |       V
        |       |   Azure pats.TblBriefAddictionMonitor → BAMMergeGbl aggregates globally
        |       |
        |       +--[pats.tbl_services]
        |               SaveServices(SrcDt, st.SiteCode, null)
        |               → pre-deactivate IsActive; RowChkSum guard (⚠ db.Add commented for new SId at existing site)
        |               V
        |           Azure pats.TblServices
        |
        |-- RowTrax audit (if st.RowTrax = true and SiteCode != 'PHC' — where implemented in Program.cs for each DsnTbl)
        |       → tsk.tbl_RowTrax (source row count vs destination count for that task)
        |
        V
BHGTaskRunner marks child task Status=20 (complete) when processing succeeds

+================================================================================+
|  BRANCH B — SCHEDULE 8 — SAMMS-ETL-INV (1 of 13 — SaveClaimStatus only)        |
+================================================================================+
        |
        V
BHGTaskRunner.exe 8
        |
        |-- filter: TaskName = 'SAMMS-ETL-INV' (or equivalent INV task name), Status=17, SiteCode != 'PHC'
        |-- for each task where st.DsnTbl == 'ctrl.tbl_claimstatus':
        |
        |       SelectConstructor / ActionKey=8 (where used for this task type)
        |       strCmd += WHERE tpcbdtcreated >= WorkDate.AddMonths(-12) + SortOrder
        |       SQLSvrManager.GetTableData → SrcDt
        |       SaveClaimStatus(SrcDt, st.WorkDate.Value.AddDays(DaysBack), null)
        |       → two-phase upsert (⚠ upsert logic inside column loop — bug)
        |       V
        |   Azure ctrl.TblClaimStatuses
        |
        |-- RowTrax if enabled for this task
        V
Task Status=20 (complete) on success

________________________________________

18. File Reference Map

Primary source file (all 13 methods):
    BHG-DR-LIB\SaveGlobal.cs (2209 lines)

Orchestration:
    BHGTaskRunner\Program.cs
        Lines 32-33    : Schedule 1 task filter (SAMMSGlobal)
        Lines 145-172  : Schedule 8 dispatch (ClaimStatus and 3P methods)
        Lines 266-316  : Consents, Devices, User, UserSite dispatch
        Lines 549-572  : BAM dispatch + pats.BAMMergeGbl call
        Lines 1043-1078: FeeSchedules dispatch + SaveFormCounts inline
        Lines 1133-1135: FormsSAMMSClients dispatch (BulkDartsSrvLoader production path)
        Lines 1156-1163: GlobalPayer dispatch
        Lines 1360-1373: Services dispatch

Library classes (BHG-DR-LIB):
    SelectConstructor.cs  — Builds source SELECT from dms.tbl_MapSrc2Dsn
    SQLSvrManager.cs      — ADO.NET wrapper for SAMMS query execution
    BulkDartsSvc.cs       — Production bulk path for FormsSAMMSClients
    BHG_DRContext.cs      — EF Core DbContext (TblFeeSched, TblGlobalPayor, TblUser, TblUserSites, TblClinicalOpiateWithdrawalScale, TblFormsSammsclient, TblConsents, TblConsentsPhc, TblGlobalDevices, TblBriefAddictionMonitor, TblServices, TblFormsCounts, TblClaimStatuses)

Azure control tables (BHG_DR):
    tsk.tbl_Tasks / vw_TaskListMap  — Task queue
    ctrl.tbl_LocationCons           — Clinic connection strings and ConnectionId
    dms.tbl_MapSrc2Dsn              — Column mapping per ActionKey
    dms.tbl_MapAction               — Source table → destination table mapping

Companion files:
    BHG-DR-LIB\SaveGlobalorg.cs — Older/org-level version of some global save methods
    PHC\Program.cs              — PHC-specific orchestration (routes Consents to SaveGlobalConsentsPhc)

________________________________________

19. Quick Reference Summary

┌─────────────────────────────────────────────────────────────────────────────────────────────────┐
│ Method                                  │ Target Table                   │ Schedule │ Pattern      │
├─────────────────────────────────────────────────────────────────────────────────────────────────┤
│ SaveFeeSchedules                        │ pats.TblFeeSched               │    1     │ Pre-Deact    │
│ SaveGlobalPayer                         │ pats.TblGlobalPayor            │    1     │ Pre-Deact    │
│ SaveGlobalUser                          │ ctrl.TblUser                   │    1     │ Always-Upd   │
│ SaveGlobalUserSite                      │ ctrl.TblUserSites              │    1     │ Always-Upd   │
│ SaveGlobalClinicalOpiateWithdrawalScale │ pats.TblClinicalOpiateWith...  │    1     │ Pre-Deact+Ck │
│ SaveGlobalFormsSAMMSClients             │ pats.TblFormsSammsclient       │    1     │ Date-Window  │
│ SaveGlobalConsents                      │ ctrl.TblConsents               │    1     │ Always-Upd   │
│ SaveGlobalConsentsPhc                   │ ctrl.TblConsentsPhc            │    1     │ Always-Upd   │
│ SaveGlobalDevices                       │ ctrl.TblGlobalDevices          │    1     │ Always-Upd   │
│ SaveBAM                                 │ pats.TblBriefAddictionMonitor  │    1     │ Date-Window  │
│ SaveServices                            │ pats.TblServices               │    1     │ Pre-Deact+Ck │
│ SaveFormCounts                          │ pats.TblFormsCounts            │    1     │ Inline/Agg   │
│ SaveClaimStatus                         │ ctrl.TblClaimStatuses          │    8     │ Always-Upd   │
└─────────────────────────────────────────────────────────────────────────────────────────────────┘

Pattern key:
    Pre-Deact      = Pre-deactivation (IsActive/RowState reset) + full snapshot refresh
    Pre-Deact+Ck   = Pre-deactivation + genuine RowChkSum guard (only changed rows re-mapped)
    Always-Upd     = Unconditional upsert; no RowChkSum; no soft-delete
    Date-Window    = Time-sliced reset: deactivate records for the date window, then re-process
    Inline/Agg     = Called from within another table's case; aggregated source query

RowChkSum effectiveness:
    Genuine guard (works as intended):  SaveGlobalClinicalOpiateWithdrawalScale, SaveServices
    Neutralised (always triggers):      SaveFeeSchedules (zeroed during pre-deactivation)
    Stored but not guarding:            SaveGlobalPayer, SaveGlobalFormsSAMMSClients
    Not used:                           All others

Known bugs requiring attention:
    SaveGlobalUserSite — NewRow never true → new site assignments silently dropped
    SaveServices       — New records at existing-site clinics silently dropped (db.Add commented out)
    SaveClaimStatus    — Upsert logic inside column loop → inflated row counters, fragile behavior

Post-save stored procedures:
    SaveBAM → pats.BAMMergeGbl (cross-clinic BAM aggregation)
