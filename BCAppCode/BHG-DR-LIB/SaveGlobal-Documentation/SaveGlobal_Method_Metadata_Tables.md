
# SaveGlobal.cs — Method Metadata Tables

All 13 methods — Field / Value quick-reference tables.
Schedule: 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) | ActionKey = 1 | File: BHG-DR-LIB\SaveGlobal.cs
Exception: SaveClaimStatus → Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) | ActionKey = 8

---

**SaveFeeSchedules**

| Field | Value |
|---|---|
| Name | Fee schedule load |
| Module | Billing / Revenue cycle management |
| Layer | Global reference load / full-snapshot refresh |
| Source system | SAMMS (SQL Server — per clinic or shared SAMMS system) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode (normalised to "SAMMS" if ConnectionId=2) |
| Source table | dbo.tblFeeSched; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblFeeSched |
| Load type | EF Core upsert only — no bulk. Pre-deactivation: all existing records for fsSite set to IsActive=false and RowChkSum=0 before processing. Dynamic column switch; lookup key is FsId; RowChkSum guard present but non-functional (zeroed during pre-deactivation — always re-maps every row); IsActive restored to true on write; SiteCode normalised to "SAMMS" for ConnectionId=2 clinics |
| Load type column | RowChkSum (stored and checked but neutralised by pre-deactivation — full refresh every run); IsActive for soft-delete ghost removal; Azure scope: all rows for fsSite (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | pats.TblFeeSched → billing rate lookups; claim generation; CPT / service-to-payer rate validation |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveFeeSchedules(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr (SAMMS or SAMMSGLOBAL for shared clinics). Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalPayer**

| Field | Value |
|---|---|
| Name | Global payer directory load |
| Module | Billing / Insurance payer configuration |
| Layer | Global reference load / full-snapshot refresh |
| Source system | SAMMS (SQL Server — per clinic or shared SAMMS system); AdvMD billing system |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode (normalised to "SAMMS" / "AdvMD-AR" / "AdvMD-NF" per ConnectionId) |
| Source table | dbo.tblPayor or dbo.tblGlobalPayor; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblGlobalPayor |
| Load type | EF Core upsert only — no bulk. Pre-deactivation: all existing records for normalised SiteCode set to RowState=0 before processing. Dynamic column switch; lookup key is PayId; RowChkSum guards column-level updates (effective); RowState restored to 1 on write; complex SiteCode normalisation via ConnectionId switch (SAMMS / AdvMD-AR / AdvMD-NF); site B59A always maps to "AdvMD-NF" regardless of ConnectionId |
| Load type column | RowChkSum (stored and effective — guards column updates); RowState=0/1 for soft-delete ghost removal; Azure scope: all rows for normalised SiteCode (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | pats.TblGlobalPayor → claim submission routing; EDI payer ID lookup; billing threshold and auth requirement checks per payer |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalPayer(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalUser**

| Field | Value |
|---|---|
| Name | Global staff user directory load |
| Module | Staff management / User reference |
| Layer | Global reference load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode |
| Source table | dbo.tblUser; direct field mapping (no column switch — known column names used directly) |
| Target DB | Azure SQL — BHG_DR |
| Target table | ctrl.TblUser |
| Load type | EF Core upsert only — no bulk. Direct field assignment (no dynamic column switch). Lookup key is UsKey (global integer — same across all SAMMS systems). Guard: skip rows where uskey is null/empty. Two-phase commit: UpdateRange(users) then AddRange(newUsers). Signature image stored as Encoding.ASCII.GetBytes(). userfullname field commented out and not extracted |
| Load type column | No RowChkSum; no soft-delete; no date filter; always overwrites all fields for matched users; users are global — no site partitioning |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | ctrl.TblUser → counselor assignment in DartsSrv; prescriber NPI/DEA for order validation; calendar user flags; co-signer and doctor role resolution |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalUser(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalUserSite**

| Field | Value |
|---|---|
| Name | User-site assignment load |
| Module | Staff management / Multi-site user assignment |
| Layer | Global reference load / always-update integration (with NewRow bug — new records never inserted) |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode |
| Source table | dbo.tblUserSites; direct field mapping |
| Target DB | Azure SQL — BHG_DR |
| Target table | ctrl.TblUserSites |
| Load type | EF Core upsert only — no bulk. Direct field assignment; lookup key is UsId. Guard: skip rows where usid is null/empty. ⚠ Known bug: local variable NewRow is declared false and never set to true — new user-site assignments are never added to newSites list and never inserted; only existing records are updated via SaveChanges() |
| Load type column | No RowChkSum; no soft-delete; no date filter; always overwrites matched records; ⚠ new records silently dropped due to NewRow bug |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | ctrl.TblUserSites → user default site resolution; multi-site access control; calendar and scheduling lookups by site |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalUserSite(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active (with bug — new assignments never inserted) |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalClinicalOpiateWithdrawalScale**

| Field | Value |
|---|---|
| Name | Clinical Opiate Withdrawal Scale (COWS) assessment load |
| Module | MAT / Opioid withdrawal clinical scoring |
| Layer | Global reference load / full-snapshot refresh with genuine RowChkSum guard |
| Source system | SAMMS (SQL Server — per clinic or SAMMSGLOBAL for shared systems) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode (source provides integer SiteCode — resolved to string via TblLocations) |
| Source table | dbo.tblClinicalOpiateWithdrawalScale or equivalent form view; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblClinicalOpiateWithdrawalScale |
| Load type | EF Core upsert only — no bulk. Pre-deactivation: all existing records set to RowState=false. Dynamic column switch; lookup key is FId; genuine RowChkSum guard — only rows with changed data re-mapped; RowState=true restored on all matched rows whether data changed or not; integer SiteCode resolved to string via TblLocations lookup; rows with unresolved integer SiteCode are silently skipped |
| Load type column | RowChkSum (genuine and effective — the only method in SaveGlobal.cs where the guard works as intended; unchanged rows re-activate only, not re-mapped); RowState boolean for soft-delete ghost removal; Azure scope: all records (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | pats.TblClinicalOpiateWithdrawalScale → COWS combined score for MAT dosing decisions; withdrawal severity tracking per patient observation |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalClinicalOpiateWithdrawalScale(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalFormsSAMMSClients**

| Field | Value |
|---|---|
| Name | Form-signature audit log load (EF Core fallback path) |
| Module | Compliance / Consent and form-signature audit |
| Layer | Target load / date-window reset integration |
| Source system | SAMMS (SQL Server — per clinic or SAMMSGLOBAL for shared systems) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode (integer fscsite resolved to string via TblLocations; defaults to "SAMMS" if not found) |
| Source table | dbo.tblFormsSammsClient or equivalent view; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblFormsSammsclient |
| Load type | EF Core upsert only — no bulk. ⚠ Currently commented out in production (BulkDartsSrvLoader is the active path). Date-window reset: all Azure records for FltrDate set to RowState=0 at start; full day's source re-processed; dynamic column switch; lookup key is Fscsid; no RowChkSum guard — always overwrites matched records; soft-deleted residuals persisted via UpdateRange(dlt); two-phase commit: AddRange(new) + UpdateRange(matched) + UpdateRange(deleted); all signature image columns silently skipped (empty case bodies) |
| Load type column | RowChkSum stored but not used as guard; RowState=0/1 for date-windowed soft-delete; Azure scope: records where FscDate == FltrDate (single day only) |
| Frequency | Daily (fallback / per-clinic path only; production uses BulkDartsSrvLoader) |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | pats.TblFormsSammsclient → consent compliance reporting; signature audit trail; form completion tracking per client per visit |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalFormsSAMMSClients(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), true, null) — production path uses BulkDartsSrvLoader to stg.tbl_formssammsclient instead |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active (EF Core path commented out in BHGTaskRunner — BulkDartsSrvLoader is production) |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalConsents**

| Field | Value |
|---|---|
| Name | Consent form template load — SAMMS clinics |
| Module | Compliance / Consent form configuration |
| Layer | Global reference load / always-update integration |
| Source system | SAMMS (SQL Server — SAMMSGLOBAL consent type table) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 (global — no per-clinic SiteCode; routed when st.SiteCode != "PHC") |
| Source table | dbo.tblConsentTypes or equivalent; column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | ctrl.TblConsents |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Cid (live per-row database query — not in-memory lookup); no RowChkSum; no soft-delete; per-row db.SaveChanges() inside the foreach loop; unconditional overwrite on match. Pre-loaded dbConsents list is unused (minor inefficiency) |
| Load type column | No RowChkSum; no soft-delete; no date filter; per-row commit (row isolation but poor performance for large sets); Azure scope: all consent template records — global, no SiteCode filter |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | ctrl.TblConsents → consent form signature-type requirements; form workflow configuration; BAC capture flag; recurring schedule flag |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalConsents(SrcDt, null) — routed when st.SiteCode != "PHC" |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalConsentsPhc**

| Field | Value |
|---|---|
| Name | Consent form template load — PHC clinics |
| Module | Compliance / Consent form configuration (PHC) |
| Layer | Global reference load / always-update integration |
| Source system | PHC SQL Server (PHCSQLVM — PHC SAMMSGLOBAL) |
| Source DB | PHC SAMMSGLOBAL (routed when st.SiteCode == "PHC") |
| Source table | dbo.tblConsentTypes or equivalent (PHC SAMMSGLOBAL); column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DRAzure SQL — BHG_DR |
| Target table | ctrl.TblConsentsPhc |
| Load type | EF Core upsert only — no bulk. Structurally identical to SaveGlobalConsents. Dynamic column switch; lookup key is Cid (live per-row database query); no RowChkSum; no soft-delete; per-row db.SaveChanges() inside the foreach loop; unconditional overwrite on match. Writes to separate target table (TblConsentsPhc) instead of TblConsents |
| Load type column | No RowChkSum; no soft-delete; no date filter; per-row commit; Azure scope: all PHC consent template records |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | ctrl.TblConsentsPhc → PHC-specific consent form signature requirements and form workflow configuration |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;..."). Target: sd.SaveGlobalConsentsPhc(SrcDt, null) — routed when st.SiteCode == "PHC" |
| Server / DB / API | Source: PHCSQLVM / SAMMSGLOBAL. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveGlobalDevices**

| Field | Value |
|---|---|
| Name | Hardware device inventory load |
| Module | MAT / Clinic device configuration |
| Layer | Global reference load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode (integer DSid resolved to string SiteCode via TblLocations) |
| Source table | dbo.tblDevices or dbo.tblGlobalDevices; column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | ctrl.TblGlobalDevices |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is DId; no RowChkSum; two-phase commit (update existing in-memory + AddRange new). Integer DSid resolved to string SiteCode via TblLocations lookup. ⚠ case "sitecode" hardcodes gd.SiteCode = "unk" — overwritten by the "dsid" case resolution; devices with null/missing DSid permanently retain SiteCode = "unk" |
| Load type column | No RowChkSum; no soft-delete; no date filter; always overwrites matched records; ⚠ SiteCode "unk" persists for devices where DSid is null or absent |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | ctrl.TblGlobalDevices → dispensing pump capability flags; BAC device availability; fingerprint scanner and signature pad configuration per clinic |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveGlobalDevices(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveBAM**

| Field | Value |
|---|---|
| Name | Brief Addiction Monitor (BAM) assessment load |
| Module | Clinical outcomes / Recovery progress monitoring |
| Layer | Target load / date-window two-path integration |
| Source system | SAMMS (SQL Server — SAMMSGLOBAL shared) or PHC SQL Server (PHCSQLVM) |
| Source DB | SAMMSGLOBAL (shared global) or PHCSQLVM depending on st.SiteCode; integer FClinic resolved to SiteCode via TblLocations |
| Source table | dbo.tblBriefAddictionMonitor or equivalent form view; column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1); WHERE fCltID > 0 AND date >= WorkDate - 30 days AND fClinic not in (25, 100) |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblBriefAddictionMonitor |
| Load type | EF Core upsert only — no bulk. Two-path design: Path A (cold start — no Azure records for 30-day window): per-row live db lookup + per-row SaveChanges (⚠ N+1 commits); Path B (warm update): per-row live db lookup + single SaveChanges after loop. Composite lookup key is FId + SiteCode; RowState=1 on every write; no RowChkSum; SAMMS Date field is a formatted string (e.g. "Monday, January 1, 2024") — parsed via substring to strip day-of-week prefix; unresolved FClinic → SiteCode = "NSL-{FClinic}". Post-save: pats.BAMMergeGbl stored procedure called by Program.cs for global aggregation |
| Load type column | No RowChkSum; RowState=1 always set (no soft-delete); Azure scope: Date >= WorkDate - 30 days (rolling 30-day window); ⚠ Path A cold start calls SaveChanges per-row — N+1 commit performance issue |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | pats.TblBriefAddictionMonitor → BAM recovery risk/protective scores; clinician review workflow; feeds pats.BAMMergeGbl global aggregation stored procedure |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr or PHCSQLVM). Target: sd.SaveBAM(SrcDt, task.WorkDate.Value.AddDays(-30).Date, null). Post-save: sm.ExecStrPro("pats.BAMMergeGbl", "@sitecode", "Global", sm.ConnectionString) |
| Server / DB / API | Source: SAMMSGLOBAL or PHCSQLVM depending on SiteCode. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveServices**

| Field | Value |
|---|---|
| Name | Per-clinic service catalogue load |
| Module | Scheduling / Service type configuration |
| Layer | Global reference load / full-snapshot refresh with genuine RowChkSum guard |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 1 + SiteCode |
| Source table | dbo.tblService or dbo.tblServices; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 1) |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblServices |
| Load type | EF Core upsert only — no bulk. Pre-deactivation: all existing records for SiteCode set to IsActive=false. AllNewRows path if no existing records. Dynamic column switch; composite lookup key is SiteCode + SId; genuine RowChkSum guard (RowChkSum NOT zeroed on deactivation — only changed rows re-mapped); IsActive restored to true on all matched records. ⚠ Known bug: when existing site has new SId not in list, services.Add(s) is called but db.TblServices.Add(s) is commented out — new services silently dropped from EF change tracker |
| Load type column | RowChkSum (genuine and effective — RowChkSum preserved during pre-deactivation; only changed rows fully re-mapped); IsActive for soft-delete ghost removal; Azure scope: all records for SiteCode (no date filter); ⚠ new services at existing-site clinics silently dropped |
| Frequency | Daily |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) |
| Parent | SAMMSGlobal |
| Downstream | pats.TblServices → DartsSrv service type validation; CPT code lookup per service; scheduling rules; billable flag and time-only flag per service |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr); WHERE st.WhereCondition. Target: sd.SaveServices(SrcDt, st.SiteCode, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active (with bug — new services at existing-site clinics never inserted) |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveFormCounts**

| Field | Value |
|---|---|
| Name | Form completion count aggregation load |
| Module | Compliance / QA dashboard / Form volume reporting |
| Layer | Target load / inline aggregated integration |
| Source system | SAMMSGLOBAL (centralised SQL Server — hardcoded connection to BHGDALLSQL05\SQL2016SAMMS) |
| Source DB | SAMMSGLOBAL (hardcoded: Data Source=BHGDALLSQL05\SQL2016SAMMS; Initial Catalog=SAMMSGLOBAL — not via st.ConStr) |
| Source table | Aggregated GROUP BY query on SAMMSGLOBAL.dbo — groups by fscsid, fscDATE, fscCLTID, fscsite having fscDATE >= '1/1/2021'; not via st.FromTblVw; query hardcoded in Program.cs |
| Target DB | Azure SQL — BHG_DR |
| Target table | pats.TblFormsCounts |
| Load type | EF Core upsert only — no bulk. Not a standalone task — called inline from case "pats.tbl_formssammsclient" in Program.cs; return value discarded (_ =). Composite lookup key: FscDate + SiteCode + fscsid + Math.Abs(fscCltID); integer fscsite resolved to SiteCode via TblLocations; rows with unresolved site silently skipped; per-row db.SaveChanges() inside loop; Math.Abs(clt) handles negative client IDs (deleted patients in SAMMS) |
| Load type column | No RowChkSum; no soft-delete; per-row commit; Azure scope: source hardcoded to fscDATE >= '1/1/2021'; negative client IDs matched symmetrically via Math.Abs |
| Frequency | Daily (triggered inline during FormsSAMMSClients task — not a standalone scheduled call) |
| Schedule | Schedule 1 — BHGTaskRunner.exe 1 (SAMMSGlobal) — inline call only |
| Parent | SAMMSGlobal (FormsSAMMSClients case) |
| Downstream | pats.TblFormsCounts → form completion volume metrics per site per day; QA compliance dashboard; per-client form count tracking |
| Connection / method | Source: sm.GetTableData("frms", scmd, hardcoded SAMMSGLOBAL connection string). Target: sd.SaveFormCounts(SrcDt, null) — return value discarded |
| Server / DB / API | Source: BHGDALLSQL05\SQL2016SAMMS / SAMMSGLOBAL (hardcoded). Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |

---

**SaveClaimStatus**

| Field | Value |
|---|---|
| Name | 3P claim batch submission status load |
| Module | Insurance / AR workflow / Claim submission tracking |
| Layer | Target load / always-update integration (with structural bug) |
| Source system | 3P billing / AdvMD claim batch system (external) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table | 3P claim batch status table (external billing system); column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8); st.FromTblVw per task |
| Target DB | Azure SQL — BHG_DR |
| Target table | ctrl.TblClaimStatuses |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is id; two-phase commit (update in-memory + AddRange new). ⚠ Known bug: xcs lookup and newCS.Add(cs) are inside the foreach DataColumn loop — upsert logic executes N times per row (once per column); rc.RowsIns and rc.RowsUpd inflated by column count (~7x); data outcome approximately correct but counters are incorrect and logic is fragile |
| Load type column | No RowChkSum; no soft-delete; Azure scope: tpcbdtcreated >= WorkDate.AddMonths(-12) (12-month rolling window); ⚠ upsert logic inside column loop inflates row counters |
| Frequency | Daily |
| Schedule | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent | SAMMS-ETL-INV |
| Downstream | ctrl.TblClaimStatuses → 837 EDI file upload status tracking; claim batch monitoring; billing operations and submission audit |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr); WHERE tpcbdtcreated >= WorkDate.AddMonths(-12). Target: sd.SaveClaimStatus(SrcDt, st.WorkDate.Value.AddDays(DaysBack), null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveGlobal.cs |
| Status | Active (with bug — upsert logic inside column loop) |
| Folder | BHG-DR-LIB\SaveGlobal.cs; detail in SaveGlobal-Documentation\SaveGlobal_ETL_Complete_Documentation.md |
