
SaveInventory.cs & Save3pElig.cs — Method Metadata Tables
Format matches SaveOrders metadata standard
________________________________________

SUMMARY COMPARISON — All 8 Methods
________________________________________

| Field               | SaveBottles              | SaveLiquidlog            | SaveInvTypes             | SaveOrientationCheckList |
|---------------------|--------------------------|--------------------------|--------------------------|--------------------------|
| Module              | MAT / Medication inventory | MAT / Medication dispensing | MAT / Medication reference | Patient onboarding / Regulatory |
| Layer               | Incremental integration  | Incremental integration  | Reference refresh        | Always-update integration |
| Source table        | dbo.tblBottle            | dbo.tblLiquidLog         | dbo.tblInvType           | dbo.tblOrientationChecklist |
| Target table        | TblBottle                | TblLiquidLog             | TblInvtype               | TblOrientationChecklistNew |
| Primary key         | SiteCode + BottleId      | SiteCode + LiqId         | SiteCode + Invid         | SiteCode + CheckListId   |
| Schedule            | 8 — SAMMS-ETL-INV        | 8 — SAMMS-ETL-INV        | 8 — SAMMS-ETL-INV        | 8 — SAMMS-ETL-INV        |
| RowChkSum guard     | YES — proper guard       | YES — proper guard       | NO — always 0 → always updates | NO — no RowChkSum at all |
| RowState            | Set true on write        | Set true on write        | Set true on write        | Not used (IsDeleted instead) |
| Soft-delete reset   | No                       | No                       | No                       | No                       |
| Azure load scope    | All-time full site load  | All-time (date filter commented out) | All-time full site load | All-time full site load |
| Commit pattern      | Batch + AddRange         | Batch + AddRange         | Batch + AddRange         | Batch + AddRange         |
| yearly param        | YES                      | YES                      | YES                      | NO (unique — omitted)    |
| Special guards      | dtclosed length>6; liquid/specgrav/weight conditional | amt overflow 690122921→0; Pump uses Int16; dtm length>6 | Invid=0 if blank; all bool fields conditional | IsDeleted from "1" string; StaffSignature write-once; LastModEtl not LastModAt |

| Field               | Save3pElig               | Save3pSetup              | Save3pClaimNote          | Save3pArnote             |
|---------------------|--------------------------|--------------------------|--------------------------|--------------------------|
| Module              | Insurance / Eligibility  | Insurance / Billing config | Insurance / Claims workflow | Insurance / AR workflow |
| Layer               | Incremental + soft-delete | Per-row commit reference | Batched insert integration | Batched insert integration |
| Source table        | dbo.tbl3pElig            | dbo.tbl3psetup           | dbo.tbl3pClaimNote       | dbo.tbl3pArnote          |
| Target table        | Tbl3pElig                | Tbl3psetup               | Tbl3pClaimNote           | Tbl3pArnote              |
| Primary key         | SiteCode + EId           | SiteCode + _pId          | SiteCode + TpcnTpcid     | SiteCode + ArnId         |
| Schedule            | 8 — SAMMS-ETL-INV        | 8 — SAMMS-ETL-INV        | 8 — SAMMS-ETL-INV        | 8 — SAMMS-ETL-INV        |
| RowChkSum guard     | YES — proper guard       | YES — proper guard       | YES — proper guard       | YES — proper guard       |
| RowState            | Full soft-delete cycle   | Not managed              | Set true on write        | Set true on write        |
| Soft-delete reset   | YES — all rows → false before loop | No              | No                       | No                       |
| Azure load scope    | EDate.Year >= wrkdt.Year | All-time (no date filter) | Fixed: TpcnDtmAdded >= 1/1/2023 | Rolling: ArnDate >= wrkdt.AddDays(-10) |
| Commit pattern      | Single batch SaveChanges | Per-row SaveChanges (inside loop) | Batch updates + AddRange inserts | Batch updates + AddRange inserts |
| yearly param        | YES (both branches identical) | YES                 | YES                      | YES                      |
| Special guards      | eorigid: length>0; pyeligcheck: length>6 | SiteId=-1 if blank; BlHasPreloader/IndividualNpi=false if blank | TpcnDtmAdded/TpcnDtTickler: length>6; GlobalBatchId: length>0 | ArnLiid/Bid/GlobalBatchId: length>0; ArnDate/ArnDtRemoved: length>6 |

________________________________________

INDIVIDUAL FIELD / VALUE TABLES — SaveInventory.cs
________________________________________

--- Method 1 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Medication bottle inventory load |
| Module              | MAT / Medication inventory |
| Layer               | Target load / incremental integration |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tblBottle; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8) |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | TblBottle (inv schema or equivalent) |
| Load type           | EF Core upsert only — no bulk. Dynamic column switch builds btl object per row; RowChkSum guards column writes; new rows staged in newbottles list; AddRange inserts after update batch |
| Load type column    | RowChkSum (change detection — updates only when changed); RowState=true set on every write (no soft-delete reset cycle) |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | TblBottle → joined with TblLiquidLog via BtlId for bottle-level inventory reconciliation; DEA 222 audit reports; controlled substance receipt tracking |
| Connection / method | Source: strCmd += " Where " + st.WhereCondition then sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveBottles(SrcDt, st.SiteCode, WorkDate, false, null) |
| Server / DB / API   | Source: st.ConStr (from ctrl.tbl_LocationCons). Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\SaveInventory.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\SaveInventory.cs; detail in SaveInventory-Documentation\SaveInventory_ETL_Complete_Documentation.md |

--- Method 2 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Liquid medication dispensing log load |
| Module              | MAT / Medication dispensing |
| Layer               | Target load / incremental integration |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tblLiquidLog; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8) |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | TblLiquidLog |
| Load type           | EF Core upsert only — no bulk. Dynamic column switch; amt overflow guard (690122921 → 0); RowChkSum guards column writes; Staff field mapped on build but NOT copied on update (omission); batched AddRange inserts; Azure load scope: all-time (date filter x.Dtm >= wrkdt.AddDays(-15) currently commented out) |
| Load type column    | RowChkSum (change detection); RowState=true set on every write; note: Staff field excluded from update block |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | TblLiquidLog → joined with TblBottle (BtlId), dose records (DoseId), beaker (BkrId); dispensing analytics; DEA inventory audit trail; regional/compliance acknowledgement tracking |
| Connection / method | Source: strCmd += " Where " + st.WhereCondition then sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveLiquidlog(SrcDt, st.SiteCode, WorkDate, false, null) |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\SaveInventory.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\SaveInventory.cs; detail in SaveInventory-Documentation\SaveInventory_ETL_Complete_Documentation.md |

--- Method 3 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Inventory type (medication definition) load |
| Module              | MAT / Medication reference data |
| Layer               | Target load / effective full-refresh per run |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tblInvType; column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8); RowChkSum generated by SelectConstructor but NOT mapped in Save method switch |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | TblInvtype |
| Load type           | EF Core upsert only — no bulk. Dynamic column switch; rowchksum case absent from switch → inv.RowChkSum stays 0 on every run; comparison (dbtyp.RowChkSum != 0) always fires → full column update on every existing row every run; batched AddRange for new rows. Typically 3–10 rows per clinic so overhead is negligible |
| Load type column    | RowChkSum NOT mapped in switch — behaves as full refresh; RowState=true on every write; LastModAt updated every run for all rows |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | TblInvtype → referenced by dose, order, and billing pipelines; InvNdc (NDC code) and InvJcode (HCPCS J-code) used in insurance claims; InvMedclass used in medication class reporting; InvDivision (dilution factor) used in dose calculation validation |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveInvTypes(SrcDt, st.SiteCode, WorkDate, false, null) |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\SaveInventory.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\SaveInventory.cs; detail in SaveInventory-Documentation\SaveInventory_ETL_Complete_Documentation.md |

--- Method 4 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Patient orientation checklist load |
| Module              | Patient onboarding / Regulatory compliance (OTP/SAMHSA) |
| Layer               | Target load / always-update integration |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tblOrientationChecklist; column list from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8); no RowChkSum column used |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | TblOrientationChecklistNew |
| Load type           | EF Core upsert only — no bulk. No RowChkSum — every matched row always updated (31 fields written unconditionally); StaffSignature and PatientSignature are write-once (not in update block — only set on INSERT); IsDeleted derived from string "1"=true; version and versionx both map to Version property; batched AddRange for new rows |
| Load type column    | No RowChkSum; IsDeleted from source "1"/"0" string comparison; LastModEtl (distinct from LastModAt used by all other methods in this file) |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | TblOrientationChecklistNew → OTP accreditation audit reports; patient rights disclosure compliance verification; SAMHSA/CARF inspection evidence |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveOrientationCheckList(SrcDt, st.SiteCode, WorkDate, null) — NOTE: no yearly parameter; cannot be called from pathways that pass yearly flag |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\SaveInventory.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\SaveInventory.cs; detail in SaveInventory-Documentation\SaveInventory_ETL_Complete_Documentation.md |

________________________________________

INDIVIDUAL FIELD / VALUE TABLES — Save3pElig.cs
________________________________________

--- Method 1 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Third-party insurance eligibility check load |
| Module              | Insurance / Eligibility verification |
| Layer               | Target load / incremental integration with soft-delete |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tbl3pElig; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8) |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | Tbl3pElig (ins schema or equivalent) |
| Load type           | EF Core upsert only — no bulk. Pattern A: all Azure rows for site loaded; all reset to RowState=false before loop; RowChkSum guards column mapping; eorigid mapped only if length>0; pyeligcheck mapped only if length>6; RowState re-set to true for each row found in source; rows not seen remain RowState=false (soft-deleted); single batch SaveChanges |
| Load type column    | RowChkSum (change detection); RowState full soft-delete cycle — reset false before loop, re-activate per row found; Azure scope: EDate.Year >= wrkdt.Year |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | Tbl3pElig → insurance eligibility history per patient; payer verification audit trail; eligibility-to-claim matching reports |
| Connection / method | Source: strCmd += " Where " + st.WhereCondition then sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.Save3pElig(SrcDt, st.SiteCode, WorkDate.AddDays(DaysBack), true/false, null) |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\Save3pElig.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\Save3pElig.cs; detail in Save3pElig-Documentation\Save3pElig_ETL_Complete_Documentation.md |

--- Method 2 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Third-party payer / provider billing setup load |
| Module              | Insurance / Billing configuration |
| Layer               | Target load / per-row commit reference data |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tbl3psetup; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8); primary key column accepted as "3pid" or "pid" (both aliases handled) |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | Tbl3psetup |
| Load type           | EF Core upsert only — no bulk. Pattern B: dynamic column switch; SiteId=-1 if blank (sentinel); BlHasPreloader and IndividualNpi defaulted to false if blank; RowChkSum guards updates; db.SaveChanges() called inside loop after EVERY row (per-row commit — partial success intentional); no batched AddRange (new rows added individually within loop) |
| Load type column    | RowChkSum (change detection — skips row if checksum matches); per-row commit means partial writes survive a mid-batch failure; no RowState management |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | Tbl3psetup → payer credential lookups; NPI and TaxId used in claim submission; SFTP credentials (Sftpun/Sftppw) used for electronic claim delivery; Taxonomy and Medicaid IDs used in UB-04 / CMS-1500 filing |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.Save3pSetup(SrcDt, st.SiteCode, WorkDate, false, null) |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\Save3pElig.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\Save3pElig.cs; detail in Save3pElig-Documentation\Save3pElig_ETL_Complete_Documentation.md |

--- Method 3 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Third-party claim notes load |
| Module              | Insurance / Claims workflow / Tickler management |
| Layer               | Target load / batched insert integration |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tbl3pClaimNote; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8) |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | Tbl3pClaimNote |
| Load type           | EF Core upsert only — no bulk. Pattern B: dynamic column switch; lookup key is TpcnTpcid (parent claim ID — not the note's own Tpcn ID); RowChkSum guards column updates; new rows staged in newCNs list; two-phase commit: updates batch first (db.SaveChanges()), then AddRange(newCNs) + db.SaveChanges() for inserts; TpcnDtmAdded and TpcnDtTickler parsed only if length>6; GlobalBatchId parsed only if length>0 |
| Load type column    | RowChkSum (change detection); RowState=true always set on write; Azure load scope: TpcnDtmAdded >= 1/1/2023 (hard-coded year cutoff) |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | Tbl3pClaimNote → claim workflow history; tickler follow-up reminders; denial management note trail; linked to parent claim via TpcnTpcid |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.Save3pClaimNote(SrcDt, st.SiteCode, WorkDate, false, null) |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\Save3pElig.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\Save3pElig.cs; detail in Save3pElig-Documentation\Save3pElig_ETL_Complete_Documentation.md |

--- Method 4 of 4 ---

| Field               | Value |
|---------------------|-------|
| Name                | Accounts receivable (AR) notes load |
| Module              | Insurance / AR workflow / Denial management |
| Layer               | Target load / batched insert integration |
| Source system       | SAMMS (SQL Server — per clinic) |
| Source DB           | ctrl.tbl_LocationCons.DbName where ActionKey = 8 + SiteCode |
| Source table        | dbo.tbl3pArnote; column list + RowChkSum from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 8) |
| Target DB           | Azure SQL — BHG_DR |
| Target table        | Tbl3pArnote |
| Load type           | EF Core upsert only — no bulk. Pattern B: dynamic column switch; lookup key is ArnId (unique AR note ID); RowChkSum guards column updates; new rows staged in newARs list; two-phase commit: updates batch first (db.SaveChanges()), then AddRange(newARs) + db.SaveChanges() for inserts; ArnLiid, Bid, GlobalBatchId parsed only if length>0; ArnDate and ArnDtRemoved parsed only if length>6 |
| Load type column    | RowChkSum (change detection); RowState=true always set on write; Azure load scope: ArnDate >= wrkdt.AddDays(-10) — rolling 10-day window (dynamic, unlike ClaimNote's fixed 2023 cutoff) |
| Frequency           | Daily |
| Schedule            | Schedule 8 — BHGTaskRunner.exe 8 (SAMMS-ETL-INV) |
| Parent              | SAMMS-ETL-INV |
| Downstream          | Tbl3pArnote → AR follow-up workflow; denial appeal tracking; linked to claim line items via ArnLiid; Bid links to remittance batch for payment reconciliation |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.Save3pArnote(SrcDt, st.SiteCode, WorkDate, false, null) |
| Server / DB / API   | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner               | BHGTaskRunner / BHG-DR-LIB\Save3pElig.cs |
| Status              | Active |
| Folder              | BHG-DR-LIB\Save3pElig.cs; detail in Save3pElig-Documentation\Save3pElig_ETL_Complete_Documentation.md |

________________________________________

QUICK LOOKUP — Azure Load Scope by Method
________________________________________

| Method                    | Azure WHERE Clause Applied at Load Time                        | Implication |
|---------------------------|----------------------------------------------------------------|-------------|
| SaveBottles               | SiteCode == sc (no date filter)                                | Full site history in memory |
| SaveLiquidlog             | SiteCode == sc (date filter commented out)                     | Full site history in memory; risk if re-enabled without aligning source SELECT |
| SaveInvTypes              | SiteCode == sc (no date filter)                                | Full site; OK — 3–10 rows per clinic |
| SaveOrientationCheckList  | SiteCode == sc (no date filter)                                | Full site; OK — one row per patient |
| Save3pElig                | SiteCode == sc AND EDate.Year >= wrkdt.Year                    | Current year + future only |
| Save3pSetup               | SiteCode == sc (no date filter)                                | Full site; OK — low volume |
| Save3pClaimNote           | SiteCode == sc AND TpcnDtmAdded >= '1/1/2023'                  | Hard-coded 2023+ cutoff |
| Save3pArnote              | SiteCode == sc AND ArnDate >= wrkdt.AddDays(-10)               | Rolling 10-day window |

________________________________________

QUICK LOOKUP — Change Detection Behavior
________________________________________

| Method                    | RowChkSum in Switch? | Update Condition                            | Practical Effect |
|---------------------------|----------------------|---------------------------------------------|-----------------|
| SaveBottles               | YES                  | dbBottle.RowChkSum != btl.RowChkSum         | Updates only changed bottles |
| SaveLiquidlog             | YES                  | dbll.RowChkSum != lg.RowChkSum              | Updates only changed log entries |
| SaveInvTypes              | NO                   | dbtyp.RowChkSum != 0 (inv.RowChkSum always 0) | All existing rows updated every run |
| SaveOrientationCheckList  | N/A                  | No guard — always updates                  | Every matched checklist updated every run |
| Save3pElig                | YES (direct, not switch) | pe.RowChkSum != rcs                    | Updates only changed elig records |
| Save3pSetup               | YES                  | dbSetup.RowChkSum != psetup.RowChkSum       | Updates only changed setup rows |
| Save3pClaimNote           | YES                  | dbclaimNote.RowChkSum != claimNote.RowChkSum | Updates only changed notes |
| Save3pArnote              | YES                  | dbar.RowChkSum != ar.RowChkSum              | Updates only changed AR notes |

________________________________________

QUICK LOOKUP — Commit Pattern by Method
________________________________________

| Method                    | SaveChanges Call Location      | New Row Insert Method       | Failure Scope |
|---------------------------|--------------------------------|-----------------------------|---------------|
| SaveBottles               | After full loop (batch)        | AddRange(newbottles) + SaveChanges | Partial if AddRange fails after batch succeeds |
| SaveLiquidlog             | After full loop (batch)        | AddRange(newlogs) + SaveChanges | Same as above |
| SaveInvTypes              | After full loop (batch)        | AddRange(newinv) + SaveChanges | Same as above |
| SaveOrientationCheckList  | After full loop (batch)        | AddRange(newOCL) + SaveChanges | Same as above |
| Save3pElig                | After full loop (single batch) | Registered with db.Add() during loop; committed in batch | Full rollback if SaveChanges fails |
| Save3pSetup               | Inside loop — after EVERY row  | db.Add() + SaveChanges per row | Only failing row and later rows lost |
| Save3pClaimNote           | After full loop (batch)        | AddRange(newCNs) + SaveChanges | Partial if AddRange fails after batch |
| Save3pArnote              | After full loop (batch)        | AddRange(newARs) + SaveChanges | Same as above |
