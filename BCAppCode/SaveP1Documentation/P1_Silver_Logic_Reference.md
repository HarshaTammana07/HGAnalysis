# P1 Regional ETL â€” Silver Logic Reference

**Source of truth:** `BCAppCode/BHG-DR-LIB_updated/` (Save*.cs files) + `BCAppCode/BHGTaskRunner/updatedProgram.cs`

**Purpose:** This document is the authoritative, per-table reference for implementing the Silver (Bronze â†’ Silver) MERGE layer in Fabric notebooks for all 57 Regional P1 destination tables. It captures every detail derived directly from the C# EF Core upsert methods: merge key, RowState logic, pre-reset behaviour, RowChkSum usage, and special handling notes.

---

## How to read this document

| Column | Meaning |
|--------|---------|
| **#** | Sequential number aligned to P1 list |
| **BHG_DR Destination** | Azure `BHG_DR` schema.table |
| **Source SAMMS Object** | SAMMS source table/view |
| **Save Method** | C# method name in BHG_DR_LIB |
| **Save File** | Partial class file under `BHG-DR-LIB_updated/` |
| **Merge Key** | Fields used in `FirstOrDefault()` to match existing Azure row |
| **RowState / IsActive Logic** | When RowState (or IsActive) is set true/false |
| **Pre-Reset** | Whether all existing site rows are reset to `false` before the loop |
| **RowChkSum Used?** | Whether checksum comparison gates the update |
| **Source Date Column(s)** | Column(s) used in the source WHERE clause |
| **Special Notes** | Any critical handling unique to this table |

### General patterns used across all tables

1. **EF Core upsert pattern (most tables)**
   ```text
   1. Load existing Azure rows for site into memory list
   2. [Optional] Pre-reset all to RowState=false
   3. For each source row:
      a. Lookup existing by Merge Key
      b. If null â†’ INSERT new row
      c. If found AND RowChkSum differs (or always) â†’ UPDATE fields
   4. db.SaveChanges()
   ```
2. **BulkDartsSrvLoader pattern (bulk tables)**
   ```text
   1. Bulk-copy source DataTable into stg.tbl_* staging table
   2. Execute stored procedure (e.g. stg.sp_*_Merge) to MERGE from stg â†’ pats.*
   ```
3. **RowState semantics:** `true` = active/valid row shown to BI; `false` = logically deleted or out-of-window.
4. **RowSate (int) vs RowState (bool):** `tbl_CustomQuestions` and `tbl_CustomAnswers` use an int field called `RowSate` (typo in model). All other tables use bool `RowState`.
5. **Default DaysBack = -15** in all P1/P2 runs; extended to -90 on last-Friday-of-month and -200 on specific dates (DartsSrv only).

---

## Domain 1 â€” Pre-Admission & Patient Registry

These tables hold patient intake, pre-admission, and site configuration data.

### 1 â€” `ayx.tbl_PreAdmission_V6`

| Field | Value |
|-------|-------|
| **Source** | `dbo.SF_PatientPreAdmission` + joins to `tblCodes`, `SF_Program`, `tblClient` |
| **Save Method** | `SavePreAdmissionV6` |
| **Save File** | `SavePreAdmissionV6.cs` |
| **Merge Key** | `SiteCode + PreAdmissionid` (insert); secondary lookup on `SiteCode + Clientid` during update |
| **RowState Logic** | Always `true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes â€” `CHECKSUM(pp.id, pp.PatientID, pp.LastUpdatedBy, pp.LastUpdateOn, pp.PatientSignatureDate, pp.DateofRelease, pp.Version, pp.IsDeleted, clt.cltM4ID)` |
| **Source Date Column** | `pp.LastUpdateOn` (via WhereCondition from task table) |
| **Special Notes** | â€¢ Entire source SQL is built in `updatedProgram.cs` â€” not from `vw_MapSrc2Dsn`. <br>â€¢ First checks that `SF_PatientPreAdmission` table exists (`sys.tables` lookup). <br>â€¢ Then checks that `clientaddress` column exists; skips site if missing. <br>â€¢ Skipped if `SchemaVersion == "V5"`. <br>â€¢ Complex 4-way JOIN: `SF_PatientPreAdmission â†’ SF_Program â†’ tblCodes (2x) â†’ tblClient`. <br>â€¢ CHECKSUM covers key identity + last-change fields only. |

---

### 2 â€” `ctrl.tbl_3PSETUP`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tbl3PSETUP` |
| **Save Method** | `Save3pSetup` |
| **Save File** | `Save3pElig.cs` |
| **Merge Key** | `_pId` (source primary key; SiteCode is assigned, not part of lookup) |
| **RowState Logic** | None â€” no RowState field on this table |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | WhereCondition from task (typically `@WorkDate`) |
| **Special Notes** | Simple reference/configuration table per site. Rows are updated in place by `_pId`. |

---

### 3 â€” `ctrl.tbl_Clinic`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblClinic` |
| **Save Method** | `SaveClinic` |
| **Save File** | `SaveClinic.cs` |
| **Merge Key** | `SiteCode + Pkey` |
| **RowState Logic** | None â€” no RowState field on this table |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” all fields always updated |
| **Source Date Column** | WhereCondition from task |
| **Special Notes** | â€¢ For SiteCode `"Lab"`: strips `PullPicsFromDB` column from SELECT (column doesn't exist on Lab). <br>â€¢ All columns always refreshed â€” no checksum gating. |

---

### 4 â€” `ctrl.tbl_DroDownListItems`

| Field | Value |
|-------|-------|
| **Source** | `dbo.DroDownListItems` |
| **Save Method** | `SavedropDownListItems` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `true` always (set in `case "id":`) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | None â€” full table load |
| **Special Notes** | Reference data. Insert new; update existing fields when found. |

---

### 5 â€” `pats.tbl_3pElig`

| Field | Value |
|-------|-------|
| **Source** | `dbo.Tbl3pElig` |
| **Save Method** | `Save3pElig` |
| **Save File** | `Save3pElig.cs` |
| **Merge Key** | `SiteCode + EId` (where `EId` = source `eid`) |
| **RowState Logic** | `true` â€” set on any row from source; remains `true` after checksum match too |
| **Pre-Reset** | **YES** â€” all existing rows for site where `EDate.Year >= wrkdt.Year` set to `RowState = false` before loop |
| **RowChkSum Used?** | Yes â€” only updates columns when `RowChkSum != rcs`; but always restores `RowState = true` |
| **Source Date Column** | `edate` (year-filtered in the pre-load query) |
| **Special Notes** | â€¢ Called with `yearly = true` in P1 (both branch paths are identical â€” year filter used in both). <br>â€¢ Pre-reset is scoped to current-year rows only; prior-year rows untouched. <br>â€¢ If checksum matches â†’ only `RowState` is restored to `true` (no field update). |

---

## Domain 2 â€” Clinical & ASAM Assessments

These tables hold structured assessment forms (new admission, periodic reassessment, comprehensive assessments).

### 6 â€” `pats.tbl_Admissionassessmentsubstanceusehistory`

| Field | Value |
|-------|-------|
| **Source** | `dbo.admissionassessmentsubstanceusehistory` |
| **Save Method** | `SaveAdmissionAssessmentSubstanceuseHistory` |
| **Save File** | `SaveAssessments.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `true` always; `false` if `CltId < 0` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” always updates |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Admission assessment substance use history sub-table. Related to `tbl_AdmissionAssessmentSummary` (P2 table). |

---

### 7 â€” `pats.tbl_ComprehensiveAssessmentForm`

| Field | Value |
|-------|-------|
| **Source** | `dbo.ComprehensiveAssessmentForm` |
| **Save Method** | `SaveComprehensiveAssessmentForm` |
| **Save File** | `SaveFormQAData.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | Not explicitly managed in C# code (no RowState gating observed) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” full update on match |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | This is the BHG legacy comprehensive assessment form (not the state-specific MN/VA versions). |

---

### 8 â€” `pats.tbl_consenttomarketing`

| Field | Value |
|-------|-------|
| **Source** | `dbo.consenttomarketing` |
| **Save Method** | `SaveConsenttoMarketing` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `RowState = 1` (int field); set to `0` if `IsDeleted == true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” always updates all fields |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Uses integer `RowState` (1/0), not bool. Logical delete via `IsDeleted` field from source. |

---

### 9 â€” `pats.tbl_Cows_V6`

| Field | Value |
|-------|-------|
| **Source** | `dbo.SF_COWS` + joins to `DroDownListItems` (multiple) + `SF_PatientPreAdmission` |
| **Save Method** | `SaveCows_v6` |
| **Save File** | `SaveCows.cs` |
| **Merge Key** | `SiteCode + COWID` (where `COWID` = `c.id` in source) |
| **RowState Logic** | `true` always |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes â€” `CHECKSUM(c.id, p.PatientID, c.preadmissionid, dttime, c.RestingPulseRate, ...)` |
| **Source Date Column** | None â€” full table loaded; ordered by CltID + COWID |
| **Special Notes** | â€¢ First checks `SF_COWS` table exists via `sys.tables` lookup; skips site if absent. <br>â€¢ Dynamically adjusts SELECT for renamed columns across SAMMS versions (e.g. `PulseRate`â†’`RestingPulseRate`, `Sweat`â†’`sweating`, `Restless`â†’`Restlessness`, etc.) based on `sys.all_columns` inspection. <br>â€¢ Entire SQL built in `updatedProgram.cs`, not from `vw_MapSrc2Dsn`. <br>â€¢ 12 LEFT JOINs to `DroDownListItems` for description lookups. |

---

### 10 â€” `pats.tbl_EandMFormPregnancy`

| Field | Value |
|-------|-------|
| **Source** | `dbo.EandMFormPregnancy` joined to `dbo.EandMForm` + `SF_PatientPreAdmission` |
| **Save Method** | `SaveEMFormPregnancy` |
| **Save File** | `SaveFormQAData.cs` |
| **Merge Key** | `SiteCode + Id` (EandMFormPregnancy.Id) |
| **RowState Logic** | `IsDeleted` flag: source computes `IsDeleted = CASE WHEN a.IsDeleted = 1 OR b.IsDeleted = 1 THEN 1 ELSE 0 END` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” full update on match |
| **Source Date Column** | None â€” full site load; WHERE commented out in code |
| **Special Notes** | â€¢ SQL built in `updatedProgram.cs`: 3-table join `EandMForm INNER JOIN EandMFormPregnancy ON EandMFormID INNER JOIN SF_PatientPreAdmission`. <br>â€¢ `IsDeleted` aggregates both form and pre-admission delete flags. <br>â€¢ Appears in both P1 and P2 task lists (timezone-dependent routing). |

---

### 11 â€” `pats.tbl_FinancialHardshipApplication`

| Field | Value |
|-------|-------|
| **Source** | `dbo.FinancialHardshipApplication` |
| **Save Method** | `SaveFinancialHardshipApplication` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `true` by default; `false` if `IsDeleted == true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes (`RowChkSum` field populated) |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Contains patient financial hardship application data including income, signatures, expiration dates. |

---

### 12 â€” `pats.tbl_MNComprehensiveAssessment`

| Field | Value |
|-------|-------|
| **Source** | `dbo.MNComprehensiveAssessment` |
| **Save Method** | `SaveMNCA` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No RowState field â€” not managed |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” full update on match |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Minnesota-specific Comprehensive Assessment form. Only populated for MN sites. |

---

### 13 â€” `pats.tbl_MNComprehensiveAssessmentLevelOfCare`

| Field | Value |
|-------|-------|
| **Source** | `dbo.MNComprehensiveAssessmentLevelOfCare` |
| **Save Method** | `SaveMNCALOC` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + MNComprehensiveAssessmentFormId` |
| **RowState Logic** | No RowState field |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Child table of MN Comprehensive Assessment. One-to-one relationship (FK = MNComprehensiveAssessmentFormId). |

---

### 14 â€” `pats.tbl_mntreatmentservicereview`

| Field | Value |
|-------|-------|
| **Source** | `dbo.mntreatmentservicereview` |
| **Save Method** | `SaveMNTreatmentServiceReview` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `RowState = 1` (int); set to `0` if `IsDeleted == true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Minnesota-specific treatment service review form. Integer RowState (1/0). |

---

### 15 â€” `pats.tbl_NewAdmissionassessment`

| Field | Value |
|-------|-------|
| **Source** | `dbo.NewAdmissionAssessment` |
| **Save Method** | `SaveNewAdmissionAssessment` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No explicit RowState â€” follows standard update pattern |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” always updates |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Main new-model admission assessment header. Parent to Dimension tables 2, 4, 5, 6. |

---

### 16 â€” `pats.tbl_NewAdmissionAssessmentASAMDimension6`

| Field | Value |
|-------|-------|
| **Source** | `dbo.NewAdmissionAssessmentASAMDimension6` |
| **Save Method** | `SaveNewAdmissionAssessmentASAMDimension6` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewAdmissionAssessmentFormId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 6 (Community/Social) sub-table of New Admission Assessment. <br>â€¢ **Note:** Dimensions 2, 4, 5 are in **P2** (not P1). Only Dimension 6 is in P1. |

---

### 17 â€” `pats.tbl_newdischargetransferplanform`

| Field | Value |
|-------|-------|
| **Source** | `dbo.newdischargetransferplanform` |
| **Save Method** | `SaveNewDischargeTransferPlanForm` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `RowState = 1` (int) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | New-model discharge/transfer plan form. Integer RowState. |

---

### 18 â€” `pats.tbl_NewPeriodicReassessment`

| Field | Value |
|-------|-------|
| **Source** | `dbo.NewPeriodicReassessment` |
| **Save Method** | `SaveNewPeriodicReassessment` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No explicit RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Parent header for the new-model Periodic Reassessment. FK for all D2â€“D6 sub-tables is `NewPeriodicReassessmentId`. |

---

### 19 â€” `pats.tbl_NewPeriodicReassessmentCounselorReview`

| Field | Value |
|-------|-------|
| **Source** | `dbo.NewPeriodicReassessmentCounselorReview` |
| **Save Method** | `SaveNewPeriodicReassessmentCounselorReview` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewPeriodicReassessmentId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | One-to-one child of `tbl_NewPeriodicReassessment`. |

---

### 20 â€” `pats.tbl_newperiodicreassessmentd2`

| Field | Value |
|-------|-------|
| **Source** | `dbo.newperiodicreassessmentd2` |
| **Save Method** | `SaveNewPeriodicReassessmentD2` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewPeriodicReassessmentId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 2 (Biomedical) sub-table of New Periodic Reassessment. |

---

### 21 â€” `pats.tbl_newperiodicreassessmentd3`

| Field | Value |
|-------|-------|
| **Source** | `dbo.newperiodicreassessmentd3` |
| **Save Method** | `SaveNewPeriodicReassessmentD3` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewPeriodicReassessmentId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 3 (Emotional/Behavioral) sub-table. |

---

### 22 â€” `pats.tbl_newperiodicreassessmentd4`

| Field | Value |
|-------|-------|
| **Source** | `dbo.newperiodicreassessmentd4` |
| **Save Method** | `SaveNewPeriodicReassessmentD4` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewPeriodicReassessmentId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 4 (Readiness to Change) sub-table. |

---

### 23 â€” `pats.tbl_newperiodicreassessmentd5`

| Field | Value |
|-------|-------|
| **Source** | `dbo.newperiodicreassessmentd5` |
| **Save Method** | `SaveNewPeriodicReassessmentD5` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewPeriodicReassessmentId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 5 (Relapse Potential) sub-table. |

---

### 24 â€” `pats.tbl_newperiodicreassessmentd6`

| Field | Value |
|-------|-------|
| **Source** | `dbo.newperiodicreassessmentd6` |
| **Save Method** | `SaveNewPeriodicReassessmentD6` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + NewPeriodicReassessmentId` |
| **RowState Logic** | No RowState |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 6 (Community/Social) sub-table of New Periodic Reassessment. |

---

### 25 â€” `pats.tbl_PA`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PeriodicReassessment` |
| **Save Method** | `SavePA` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + Id` (code: `dbList.FirstOrDefault(x => x.SiteCode.Trim() == pa.SiteCode.Trim() && x.Id == pa.Id)`) |
| **RowState Logic** | `true` always (set in `case "id":`) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition (typically `ModifiedOn`) |
| **Special Notes** | Header of the PA (Periodic Assessment) family. Note `.Trim()` on both sides of SiteCode comparison â€” defensive coding. |

---

### 26 â€” `pats.tbl_PACounselorReview`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PACounselorReview` |
| **Save Method** | `SavePACounselorReview` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` (set in `case "periodicreassessmentid":`) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes (`RowChkSum` field stored) |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Child of `tbl_PA`. FK = `PeriodicReassessmentId`. |

---

### 27 â€” `pats.tbl_PADimension1`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PADimension1` |
| **Save Method** | `SavePADimension1` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` (set in `case "periodicreassessmentid":`) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 1 (Acute Intoxication) of legacy PA. |

---

### 28 â€” `pats.tbl_PADimension2`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PADimension2` |
| **Save Method** | `SavePADimension2` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 2 (Biomedical) of legacy PA. |

---

### 29 â€” `pats.tbl_PADimension3`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PADimension3` |
| **Save Method** | `SavePADimension3` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 3 (Emotional/Behavioral) of legacy PA. |

---

### 30 â€” `pats.tbl_PADimension4`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PADimension4` |
| **Save Method** | `SavePADimension4` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 4 (Readiness to Change) of legacy PA. |

---

### 31 â€” `pats.tbl_PADimension5`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PADimension5` |
| **Save Method** | `SavePADimension5` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 5 (Relapse Potential) of legacy PA. |

---

### 32 â€” `pats.tbl_PADimension6`

| Field | Value |
|-------|-------|
| **Source** | `dbo.PADimension6` |
| **Save Method** | `SavePADimension6` |
| **Save File** | `SavePAData.cs` |
| **Merge Key** | `SiteCode + PeriodicReassessmentId` |
| **RowState Logic** | `true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | ASAM Dimension 6 (Community/Social) of legacy PA. |

---

### 33 â€” `pats.tbl_SMSTextConsentForm`

| Field | Value |
|-------|-------|
| **Source** | `dbo.SMSTextConsentForm` |
| **Save Method** | `SaveSMSTextConsentForm` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `RowState = 1` (int) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Patient SMS text consent form. Integer RowState. |

---

### 34 â€” `pats.tbl_takehomeagreementanddiversioncontrol`

| Field | Value |
|-------|-------|
| **Source** | `dbo.takehomeagreementanddiversioncontrol` |
| **Save Method** | `SaveTakeHomeAgreementandDiversionControl` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `RowState = 1` (int); set to `0` if `IsDeleted == true` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Take-home medication agreement and diversion control form. Integer RowState. |

---

### 35 â€” `pats.tbl_TakeHomeRiskAssessment`

| Field | Value |
|-------|-------|
| **Source** | `dbo.TakeHomeRiskAssessment` |
| **Save Method** | `SaveTakeHomeRiskAssessment` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | Not explicitly set in SaveDataFeb26.cs (no IsDeleted logic for this table) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Take-home risk assessment form. |

---

### 36 â€” `pats.tbl_VAComprehensiveAssessment`

| Field | Value |
|-------|-------|
| **Source** | `dbo.VAComprehensiveAssessment` |
| **Save Method** | `SaveVACA` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No RowState field |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | `CreatedOn` / `ModifiedOn` via WhereCondition |
| **Special Notes** | Virginia-specific Comprehensive Assessment form. Only populated for VA sites. |

---

### 37 â€” `pats.tbl_vacomprehensiveassessmentsummary`

| Field | Value |
|-------|-------|
| **Source** | `dbo.vacomprehensiveassessmentsummary` |
| **Save Method** | `SaveVACASummary` |
| **Save File** | `SaveCA.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No RowState field |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Child summary table of VA Comprehensive Assessment. |

---

## Domain 3 â€” Clinical Operations (Daily Dispensing / Attendance)

### 38 â€” `pats.tbl_AppointmentAttend`

| Field | Value |
|-------|-------|
| **Source** | `dbo.AppointmentAttend` |
| **Save Method** | `SaveAppointmentAttend` |
| **Save File** | `SaveAppointments.cs` |
| **Merge Key** | `SiteCode + AAId` |
| **RowState Logic** | `true` by default (set in `case "sitecode":`); `false` if `aacltid < 0` |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes (`RowChkSum` read from `case "sitecode":`) |
| **Source Date Column** | `aaDTENROLLED` / `aaDTREMOVED` via WhereCondition |
| **Special Notes** | Appointment attendance tracking. Negative CltId marks deleted/inactive rows. |

---

### 39 â€” `pats.tbl_BAMForm`

| Field | Value |
|-------|-------|
| **Source** | `dbo.BAMForm` |
| **Save Method** | `SaveBamForm` |
| **Save File** | `SaveBAM.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No RowState field on this table |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No â€” all fields always updated on match |
| **Source Date Column** | `BAMDate` via WhereCondition |
| **Special Notes** | Brief Addiction Monitor (BAM) form header. |

---

### 40 â€” `pats.tbl_BAMScore`

| Field | Value |
|-------|-------|
| **Source** | `dbo.BAMScore` |
| **Save Method** | `SaveBamScore` |
| **Save File** | `SaveBAM.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | No RowState field |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | BAM computed score child table. |

---

### 41 â€” `pats.tbl_CheckIn`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblCHECKIN` |
| **Save Method** | `SaveCheckIn` |
| **Save File** | `SaveCheckIn.cs` |
| **Merge Key** | `SiteCode + CiId` |
| **RowState Logic** | No RowState field |
| **Pre-Reset** | No â€” but **windowed load**: retrieves existing rows where `CiDate >= workdate OR CiId >= firstCiId` |
| **RowChkSum Used?** | Yes â€” skips update if checksum matches |
| **Source Date Column** | `ciDate` â€” default DaysBack = -15 |
| **Special Notes** | â€¢ `workdate` = `WorkDate.AddDays(-15)`. <br>â€¢ Load window uses the first `ciid` from source data as lower-bound OR date. <br>â€¢ For SiteCode `"Lab"`: strips `ciQUEUETIME` column from SELECT (column doesn't exist on Lab SAMMS). <br>â€¢ Also checks dynamically if `ciqueuetime` column exists before including it. <br>â€¢ Calculates `MinutesWaited` from `ciTime` to `ciServeddtm`. |

---

### 42 â€” `pats.tbl_Enrollment`

| Field | Value |
|-------|-------|
| **Source** | `bhg.vw_Enrollment` / `dbo.tblENROLL` / `oak.vw_pt_Enrollments` (varies by site) |
| **Save Method** | `SaveEnrollment` |
| **Save File** | `SaveEnrollment.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | **Always `true`** after processing (forced on line `if (true)`) |
| **Pre-Reset** | **YES** â€” if site has existing rows: all set to `RowState = false` before loop |
| **RowChkSum Used?** | Yes (stored, but update runs regardless due to `if (true)`) |
| **Source Date Column** | `EnrollDate` / `DischargeDate` via WhereCondition |
| **Special Notes** | â€¢ Pre-reset makes this a **full site refresh** â€” every row in source will be marked active; any row not in source stays false. <br>â€¢ The `if (true)` condition means RowChkSum is ignored â€” all fields always refreshed. <br>â€¢ Dynamic column check for `TreatmentLevel` (not on all SAMMS versions). <br>â€¢ Dynamic check for `Modality` column in `vw_Enrollment`. <br>â€¢ Skipped entirely for SiteCode `"Lab"`. <br>â€¢ Appears in **both P1 and P2**. |

---

### 43 â€” `pats.tbl_Fmp`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblFMP` |
| **Save Method** | `SaveFmp` |
| **Save File** | `SaveFmp.cs` |
| **Merge Key** | `SiteCode + FmpId` |
| **RowState Logic** | `true` always (set in `case "sitecode":`) |
| **Pre-Reset** | **YES** â€” all existing rows for site with `RowState == true` set to `false` before loop |
| **RowChkSum Used?** | No â€” all fields always refreshed |
| **Source Date Column** | `FmpDtStart` via WhereCondition |
| **Special Notes** | Flexible Medication Program records. Pre-reset + always-true RowState = full effective refresh pattern. |

---

### 44 â€” `pats.tbl_UAResults`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblUAResult` |
| **Save Method** | `SaveUAResults` |
| **Save File** | `SaveUAResults.cs` |
| **Merge Key** | `SiteCode + UarId` |
| **RowState Logic** | `true` unless in `reInit` (reload) mode |
| **Pre-Reset** | No (unless `reInit=true` which loads ALL site rows) |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | `uarresultdt` â€” default DaysBack = -15; no WHERE on reload |
| **Special Notes** | â€¢ `reInit` flag: when `Reload = true` in task config, `WHERE uarresultdt is not null` (full table). <br>â€¢ For SiteCode `"Lab"`: strips `LabName` and `UAEval` columns from SELECT. <br>â€¢ Special full-refresh date override on `2/10/2026` (temporary backfill). |

---

### 45 â€” `pats.tbl_UASched`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblUASched` |
| **Save Method** | `SaveUASched` |
| **Save File** | `SaveUAResults.cs` |
| **Merge Key** | `SiteCode + UasId + UasLngCltId` (3-part key) |
| **RowState Logic** | Not observed in code |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition (DaysBack = -15) |
| **Special Notes** | â€¢ First checks `tblUASched` table exists via `sys.tables`; skips site if absent. <br>â€¢ Source SELECT uses `DISTINCT` (`strCmd.Replace("Select ", "Select distinct ")`). <br>â€¢ 3-part merge key is unusual â€” ensures uniqueness across patient schedule records. |

---

## Domain 4 â€” Financial & Insurance

### 46 â€” `pats.tbl_Bills`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblBill` |
| **Save Method** | `SaveBills` |
| **Save File** | `SaveBills.cs` |
| **Merge Key** | `SiteCode + BillId` |
| **RowState Logic** | `true` if `BillCltid > 0`; `false` if `BillCltid <= 0` (negative CltId = void/orphan) |
| **Pre-Reset** | **YES** â€” windowed pre-reset: all rows for site where `BillDate.Year >= wrkdt.AddDays(DaysBack).Year AND BillDate <= wrkdt.AddDays(DaysBack)` set to `RowState = false` |
| **RowChkSum Used?** | Yes â€” skips field update if checksum matches, but still restores RowState |
| **Source Date Column** | `billDate` â€” custom WHERE: `WHERE YEAR(billDate) >= [DaysBackYear] AND billdate <= [Today+12]` |
| **Special Notes** | â€¢ Custom WHERE in `updatedProgram.cs`: year-based lower bound, not date-based like other tables. <br>â€¢ Reload mode (`Reload = true`): sets `DaysBack = -728250` (full history reload). <br>â€¢ Even when checksum matches, RowState is evaluated and re-set from `BillCltid`. <br>â€¢ Appears in **both P1 and P2**. |

---

### 47 â€” `pats.tbl_PayerCltHistory`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblPayerCltHistory` |
| **Save Method** | `SavePayerCltHistory` |
| **Save File** | `SavePayorClient.cs` |
| **Merge Key** | `PchId` only (SiteCode is assigned but NOT used in lookup) |
| **RowState Logic** | No RowState field |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | `pydtm` via WhereCondition |
| **Special Notes** | Payer client history (change log). Insert-only for new `PchId`; update existing. No logical delete pattern. Appears in both P1 and P2. |

---

### 48 â€” `pats.tbl_pbi3PayAuth`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tbl3PAYauth` |
| **Save Method** | `SaveAuths` |
| **Save File** | `SaveAuths.cs` |
| **Merge Key** | `SiteCode + TpaId` |
| **RowState Logic** | `true` on any update; initial state: `false` from pre-reset |
| **Pre-Reset** | **YES** â€” all existing rows for site set to `RowState = false` before loop |
| **RowChkSum Used?** | Yes â€” only updates fields if checksum differs; but RowState always set to `true` |
| **Source Date Column** | `tpaeffdate` / `tpatermdate` via WhereCondition |
| **Special Notes** | Pre-authorization records. Pre-reset + true-on-presence = soft-delete pattern (inactive auths not in source window stay false). |

---

### 49 â€” `pats.tbl_SF_DataForms`

| Field | Value |
|-------|-------|
| **Source** | `dbo.SF_DataForms` |
| **Save Method** | `SaveDataForms` |
| **Save File** | `SaveDataFeb26.cs` |
| **Merge Key** | `SiteCode + Id` |
| **RowState Logic** | `RowState = 1` (int) |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | SF DataForms reference table (form definitions used by `EandMForm`, `ComprehensiveAssessmentForm`, etc.). Integer RowState. |

---

### 50 â€” `pats.tbl_vw3pBillSub`

| Field | Value |
|-------|-------|
| **Source** | `dbo.vw3pBillSub` |
| **Save Method** | `SaveAuthBillsub` |
| **Save File** | `SaveAuths.cs` |
| **Merge Key** | `SiteCode + DsId` (+ additional fields in extended lookup) |
| **RowState Logic** | `true` (set immediately in new object construction); initial state: `false` from pre-reset |
| **Pre-Reset** | **YES** â€” all existing rows for site set to `RowState = false` before loop |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | `Billdatecriteria` via WhereCondition + `WrkDate` |
| **Special Notes** | Authorization billing submission records. Same pre-reset + true-on-presence pattern as `tbl_pbi3PayAuth`. |

---

## Domain 5 â€” Reference & Configuration

### 51 â€” `pats.tbl_Codes`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblCodes` |
| **Save Method** | `SaveCodes` |
| **Save File** | `SaveCodes.cs` |
| **Merge Key** | `SiteCode + CdeId` |
| **RowState Logic** | **None** â€” this method returns `bool`, not `RCodes`; no RowState field on table |
| **Pre-Reset** | No |
| **RowChkSum Used?** | Yes â€” only updates when checksum differs |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Site-specific code table (services, billing codes, etc.). No logical delete pattern. |

---

### 52 â€” `pats.tbl_CustomAnswers`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblCUSTOMANSWERS` |
| **Save Method** | `SaveCustomAnswers` |
| **Save File** | `SaveCustomQA.cs` |
| **Merge Key** | `SiteCode + CId` (custom answer ID) |
| **RowState Logic** | `RowSate = 1` (int, **typo in field name** â€” `RowSate` not `RowState`); `0` if `CId < 0` |
| **Pre-Reset** | **YES** â€” all rows for site with `RowSate == 1` set to `0` before loop |
| **RowChkSum Used?** | Yes (`RowCheckSum` â€” note different field name vs other tables) |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | â€¢ Field name is `RowSate` (not `RowState`) and `RowCheckSum` (not `RowChkSum`) â€” typos in the EF model. <br>â€¢ In Fabric MERGE, match the exact column names in the Delta table. |

---

### 53 â€” `pats.tbl_CustomQuestions`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblCUSTOMQUESTIONS` |
| **Save Method** | `SaveCustomQuestions` |
| **Save File** | `SaveCustomQA.cs` |
| **Merge Key** | `SiteCode + CId` |
| **RowState Logic** | `RowSate = 1` (int); `0` if `CId < 0` |
| **Pre-Reset** | **YES** â€” all rows for site with `RowSate == 1` set to `0` before loop |
| **RowChkSum Used?** | Yes (`RowCheckSum`) |
| **Source Date Column** | Via WhereCondition |
| **Special Notes** | Same `RowSate`/`RowCheckSum` typo pattern as `tbl_CustomAnswers`. |

---

### 54 â€” `pats.tbl_PreadmissionReferralSource`

| Field | Value |
|-------|-------|
| **Source** | `dbo.SF_PatientPreadmissionReferralSource` |
| **Save Method** | `SavePreAdminReferrals` |
| **Save File** | `SavePreAdmissionV6.cs` |
| **Merge Key** | `Id` only (SiteCode assigned but NOT used in `FirstOrDefault` lookup) |
| **RowState Logic** | `true` always |
| **Pre-Reset** | No |
| **RowChkSum Used?** | No |
| **Source Date Column** | Extended DaysBack: `WorkDate.AddDays(-515)` (`DaysBack - 500 = -15 - 500`) |
| **Special Notes** | â€¢ Added September 2025. <br>â€¢ First checks that source table exists (`sys.all_objects`); skips site if absent. <br>â€¢ DaysBack is aggressively extended to -515 days to capture older referral records. |

---

### 55 â€” `pats.tbl_SERVICES`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblSERVICES` |
| **Save Method** | `SaveServices` |
| **Save File** | `SaveGlobal.cs` |
| **Merge Key** | `SiteCode + SId` |
| **RowState Logic** | **IsActive** field (not RowState): `IsActive = false` on pre-reset, `true` when row found in source |
| **Pre-Reset** | **YES** â€” all rows for site with `IsActive == true` set to `false` before loop |
| **RowChkSum Used?** | Yes |
| **Source Date Column** | No WHERE on services â€” full table via `st.WhereCondition` |
| **Special Notes** | â€¢ Uses `IsActive` (bool) instead of `RowState`. In Fabric Delta MERGE, treat `IsActive` as the activity flag. <br>â€¢ Pre-reset + source-driven true = any service no longer in SAMMS becomes inactive. |

---

## Domain 6 â€” Bulk-Load Tables (No Row-by-Row MERGE)

These tables use `BulkDartsSrvLoader` â€” data goes directly to a staging table via `SqlBulkCopy`, then a stored procedure performs the MERGE. There is no EF Core row-by-row upsert.

### 56 â€” `pats.tbl_TblDiag10`

| Field | Value |
|-------|-------|
| **Source** | `dbo.Tbldiag10` |
| **Save Method** | `BulkDartsSrvLoader` |
| **Save File** | `BulkDartsSvc.cs` |
| **Staging Table** | `stg.tbl_TblDiag10` (derived from destination: `"stg." + TaskName.Substring(5, ...)`) |
| **Merge Key** | Defined inside the stored procedure `stg.sp_TblDiag10_Merge` (not in C#) |
| **RowState Logic** | Managed by stored procedure |
| **Pre-Reset** | Managed by stored procedure |
| **RowChkSum Used?** | Managed by stored procedure |
| **Source Date Column** | Via WhereCondition from task |
| **Special Notes** | â€¢ Bulk copy â†’ staging â†’ SP merge pattern. <br>â€¢ In Fabric: Bronze â†’ Silver uses the same staging-then-MERGE approach (notebook runs SP after bulk load). <br>â€¢ Investigate SP `stg.sp_TblDiag10_Merge` in BHG_DR for exact MERGE logic. |

---

### 57 â€” `stg.ClientDemo`

| Field | Value |
|-------|-------|
| **Source** | `dbo.tblClient` |
| **Save Method** | `BulkDartsSrvLoader` |
| **Save File** | `BulkDartsSvc.cs` |
| **Staging Table** | `stg.clientdemo` (hardcoded â€” not derived from TaskName) |
| **Merge Key** | Defined in stored procedure (if any) |
| **RowState Logic** | Managed downstream |
| **Pre-Reset** | No C# pre-reset |
| **RowChkSum Used?** | Yes â€” embedded in the hardcoded SELECT in `updatedProgram.cs` |
| **Source Date Column** | None â€” **full table load** (`SELECT ... FROM dbo.tblClient ORDER BY 1, cltID`) |
| **Special Notes** | â€¢ SQL is hardcoded in `updatedProgram.cs` (not from `vw_MapSrc2Dsn`). <br>â€¢ Includes computed `RowChkSum = CHECKSUM(cltID, cltM4ID, cltFName, ...)` with ~18 key demographic fields. <br>â€¢ Includes `LastModAt = GetDate()`. <br>â€¢ Full table every run â€” no date filtering. <br>â€¢ Destination `stg.clientdemo` is a staging table used by downstream processes to populate patient demographics. |

---

## Appendix A â€” Tables with Special WHERE Logic (not standard DaysBack)

| BHG_DR Destination | WHERE Override | Source |
|-------------------|----------------|--------|
| `pats.tbl_Bills` | `YEAR(billDate) >= [DaysBackYear] AND billdate <= [Today+12]` | Custom in `updatedProgram.cs` |
| `pats.tbl_BriefAddictionMonitor` | `fCltID > 0 AND DATE >= [Today-30]` | Custom BAM WHERE |
| `pats.tbl_CheckIn` | `ciDate >= [DaysBack=âˆ’15]` (standard); also uses `CiId >= firstId` guard | Standard |
| `pats.tbl_PreadmissionReferralSource` | `WorkDate.AddDays(-515)` â€” intentionally extended | Code comment |
| `stg.ClientDemo` | None â€” **full table every run** | Hardcoded SQL |
| `pats.tbl_Cows_V6` | None â€” **full table every run** (ordered by CltID + COWID) | Hardcoded SQL |
| `pats.tbl_EandMFormPregnancy` | None â€” full site load (WHERE commented out) | Code comment |
| `pats.tbl_dbo_FormQuestionAnswers` | `f.CreatedOn >= [DaysBack-30] OR UpdatedOn >= [DaysBack-30]` | Custom extended |

---

## Appendix B â€” Pre-Reset Summary

Tables that reset RowState/IsActive for all existing rows **before** the source loop:

| Table | Reset Field | Scope | Effect |
|-------|-------------|-------|--------|
| `pats.tbl_3pElig` | `RowState = false` | Year-filtered rows for site | Soft-delete rows not in current year window |
| `pats.tbl_pbi3PayAuth` | `RowState = false` | All rows for site | Any auth not in source stays false |
| `pats.tbl_vw3pBillSub` | `RowState = false` | All rows for site | Same pattern |
| `pats.tbl_Bills` | `RowState = false` | Windowed rows (year + date range) | Rows in window not in source stay false |
| `pats.tbl_Enrollment` | `RowState = false` | All rows for site (if count > 0) | Full site refresh |
| `pats.tbl_Fmp` | `RowState = false` | All rows for site with `RowState = true` | Effective full refresh |
| `pats.tbl_CustomAnswers` | `RowSate = 0` | All rows for site with `RowSate = 1` | Soft-delete pattern (note: int field `RowSate`) |
| `pats.tbl_CustomQuestions` | `RowSate = 0` | All rows for site with `RowSate = 1` | Same as CustomAnswers |
| `pats.tbl_SERVICES` | `IsActive = false` | All rows for site with `IsActive = true` | Services not in source become inactive |
| `pats.tbl_FeeSched` (P2) | `IsActive = false` | All rows for site | Fee schedules not in source become inactive |

---

## Appendix C â€” RowChkSum Column in Source SELECT

The C# `GetSLT()` method builds the SELECT including `RowChkSum = CHECKSUM(...)`. For hardcoded SQL (PreAdmission_V6, COWS, Bills, ClientDemo), the CHECKSUM expression is written manually. Here is where each table gets its RowChkSum:

| Table | RowChkSum Source |
|-------|-----------------|
| `ayx.tbl_PreAdmission_V6` | Hardcoded CHECKSUM over 9 fields (id, PatientID, LastUpdatedBy, LastUpdateOn, PatientSignatureDate, DateofRelease, Version, IsDeleted, cltM4ID) |
| `ctrl.tbl_3PSETUP` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `ctrl.tbl_DroDownListItems` | No RowChkSum in code |
| `pats.tbl_3pElig` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_Bills` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_CheckIn` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_Cows_V6` | Hardcoded CHECKSUM over 16 fields |
| `pats.tbl_Enrollment` | From `vw_MapSrc2Dsn` via `GetSLT()` (but ignored due to `if (true)`) |
| `pats.tbl_Fmp` | From `vw_MapSrc2Dsn` via `GetSLT()` (but ignored â€” always updates) |
| `pats.tbl_pbi3PayAuth` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_PA` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_PACounselorReview` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_PADimension1â€“6` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `pats.tbl_UAResults` | From `vw_MapSrc2Dsn` via `GetSLT()` |
| `stg.ClientDemo` | Hardcoded CHECKSUM over ~18 demographic fields |
| Most other tables | From `vw_MapSrc2Dsn` via `GetSLT()` |

> **Note:** `GetSLT()` excludes `ntext`, `varbinary`, and `timestamp` field types from the CHECKSUM expression. These types appear as raw columns but do not participate in change detection.

---

## Appendix D â€” Tables Without RowState (no activity flag)

These tables have no `RowState` (or `IsActive`) field and cannot be logically deleted:

| Table | Notes |
|-------|-------|
| `ctrl.tbl_3PSETUP` | Reference config â€” always current |
| `ctrl.tbl_Clinic` | One row per clinic â€” always current |
| `pats.tbl_BAMForm` | BAM forms â€” no delete |
| `pats.tbl_BAMScore` | BAM scores â€” no delete |
| `pats.tbl_Codes` | Reference codes â€” no delete |
| `pats.tbl_CheckIn` | Daily check-in â€” windowed, no delete |
| `pats.tbl_MNComprehensiveAssessment` | MN-only |
| `pats.tbl_MNComprehensiveAssessmentLevelOfCare` | MN-only |
| `pats.tbl_NewAdmissionassessment` | Assessment form |
| `pats.tbl_NewAdmissionAssessmentASAMDimension6` | Assessment sub-table |
| `pats.tbl_NewPeriodicReassessment` | Assessment form |
| `pats.tbl_NewPeriodicReassessmentCounselorReview` | Assessment sub-table |
| `pats.tbl_newperiodicreassessmentd2â€“d6` | Assessment sub-tables |
| `pats.tbl_PayerCltHistory` | History log â€” no delete |
| `pats.tbl_VAComprehensiveAssessment` | VA-only |
| `pats.tbl_vacomprehensiveassessmentsummary` | VA-only |

---

## Appendix E â€” Fabric Silver MERGE Template

Use this PySpark Delta MERGE template as the foundation for each Silver notebook. Adjust `merge_key`, `RowState` column name and type, and pre-reset logic per the table entries above.

```python
from delta.tables import DeltaTable

# â”€â”€ Load Bronze (staged source data for this site + run) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
bronze_df = spark.read.format("delta").load("abfss://bronze/tbl_xxx_sitecode")

# â”€â”€ Pre-Reset (tables that require it â€” see Appendix B) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Only for tables listed in Appendix B:
silver_tbl = DeltaTable.forPath(spark, "abfss://silver/tbl_xxx")
silver_tbl.update(
    condition="SiteCode = 'SITECODE'",    # scope to site
    set={"RowState": "false"}             # or "0" for int RowSate tables
)

# â”€â”€ MERGE Bronze â†’ Silver â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
silver_tbl.alias("tgt").merge(
    bronze_df.alias("src"),
    "tgt.SiteCode = src.SiteCode AND tgt.Id = src.Id"   # â† use exact Merge Key from table entry
).whenMatchedUpdate(
    condition="tgt.RowChkSum <> src.RowChkSum",          # â† omit condition for tables where RowChkSum not used
    set={
        "RowState":   "src.RowState",
        "RowChkSum":  "src.RowChkSum",
        "LastModAt":  "current_timestamp()",
        # ... all other columns
    }
).whenNotMatchedInsert(
    values={
        "SiteCode":   "src.SiteCode",
        "Id":         "src.Id",
        "RowState":   "src.RowState",
        "RowChkSum":  "src.RowChkSum",
        "LastModAt":  "current_timestamp()",
        # ... all other columns
    }
).execute()
```

### Key variations by table type

| Scenario | Template Variation |
|----------|-------------------|
| **No RowChkSum** | Remove `condition="tgt.RowChkSum <> src.RowChkSum"` from `whenMatchedUpdate` |
| **Pre-Reset required** | Add the `silver_tbl.update(...)` block before MERGE |
| **Integer RowSate** (CustomAnswers/Questions) | Use `"RowSate"` (not `"RowState"`) and `"1"`/`"0"` (not `"true"`/`"false"`) |
| **IsActive flag** (Services, FeeSched) | Replace RowState with `IsActive` and `"True"`/`"False"` |
| **No RowState** | Omit RowState from set/values |
| **Bulk-load tables** | Skip MERGE entirely; use SP call after bulk copy |

---

---

## Appendix F â€” Lookback Windows & Source WHERE Conditions

**This is the most critical section for building Fabric Copy Activity `sqlReaderQuery` expressions.**

### How `@WorkDate` resolves

```
@WorkDate = WorkDate.AddDays(-15)   â† hardcoded default in updatedProgram.cs line 139
```

So for a run on `2026-06-15`, `@WorkDate` = `2026-05-31`.

- `@WorkDate` is replaced **before** the switch statement â€” all standard WHERE conditions receive it automatically.
- `@SiteCode` is also replaced: becomes the literal site code string (e.g. `'AHK'`).
- `@Samms` becomes `'SAMMS'`.

> **Source:** `WhereCondition` column in `dms.vw_MapAction` / `vw_mapAction.csv`. Override notes come from `updatedProgram.cs` switch cases.

---

### Full Lookback Window Table â€” All 57 P1 Tables

| # | BHG_DR Destination | Source SAMMS Object | WhereCondition (from `vw_mapAction`) | Resolved Window | Override in Code? |
|---|--------------------|--------------------|------------------------------------|-----------------|-------------------|
| 1 | `ayx.tbl_PreAdmission_V6` | `SF_PatientPreAdmission` | `len(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE '%test data%'` | **Full load** â€” data quality filter only, no date | No override; entire SQL hardcoded in `updatedProgram.cs` |
| 2 | `ctrl.tbl_3PSETUP` | `tbl3PSETUP` | `1 = 1` | **Full load** | None |
| 3 | `ctrl.tbl_Clinic` | `tblClinic` | `1 = 1` | **Full load** | None |
| 4 | `ctrl.tbl_DroDownListItems` | `DroDownListItems` | `1 = 1` | **Full load** | None |
| 5 | `pats.tbl_3pElig` | `Tbl3pElig` | `Year(edate) >= Year(@WorkDate)` | **Current year** â€” all records where `edate` year = run year (or later). @WorkDate = todayâˆ’15; Year() means year boundary, not day | None â€” but Pre-reset scoped to same year window |
| 6 | `pats.tbl_Admissionassessmentsubstanceusehistory` | `admissionassessmentsubstanceusehistory` | `1 = 1` | **Full load** | None |
| 7 | `pats.tbl_AppointmentAttend` | `AppointmentAttend` | `1 = 1` | **Full load** | None |
| 8 | `pats.tbl_BAMForm` | `BAMForm` | `1 = 1` | **Full load** | None |
| 9 | `pats.tbl_BAMScore` | `BAMScore` | `1 = 1` | **Full load** | None |
| 10 | `pats.tbl_Bills` | `tblBill` | `billDate >= DateAdd(m,-1,@WorkDate) AND billDate <= DateAdd(d,2,@WorkDate)` | **Ignored** â€” overridden in code | **YES** â€” `updatedProgram.cs` replaces with: `WHERE YEAR(billDate) >= [Year(WorkDateâˆ’15)] AND billdate <= [WorkDate+12]`. Normal run = current year. Reload = `DaysBack=-728250` (full history). |
| 11 | `pats.tbl_CheckIn` | `tblCHECKIN` | `convert(date,ciDT) >= convert(date,@WorkDate)` | **âˆ’15 days** on `ciDT` (check-in date) | None |
| 12 | `pats.tbl_Codes` | `tblCodes` | `1 = 1` | **Full load** | None |
| 13 | `pats.tbl_ComprehensiveAssessmentForm` | `ComprehensiveAssessmentForm` | `1 = 1` | **Full load** | None |
| 14 | `pats.tbl_consenttomarketing` | `consenttomarketing` | `(CreatedOn >= @WorkDate OR ModifiedOn >= @WorkDate)` | **âˆ’15 days** on `CreatedOn` OR `ModifiedOn` | None |
| 15 | `pats.tbl_Cows_V6` | `SF_COWS` | `SiteCode = @SiteCode` | **Full load** (SiteCode filter only) | YES â€” entire SQL hardcoded; no date column in WHERE |
| 16 | `pats.tbl_CustomAnswers` | `tblCUSTOMANSWERS` | `1 = 1` | **Full load** | None |
| 17 | `pats.tbl_CustomQuestions` | `tblCUSTOMQUESTIONS` | `1 = 1` | **Full load** | None |
| 18 | `pats.tbl_EandMFormPregnancy` | `EandMFormPregnancy` + joins | `1 = 1` | **Full load** | None â€” date WHERE commented out in code |
| 19 | `pats.tbl_Enrollment` | `tblENROLL` / `vw_Enrollment` | `1 = 1` | **Full load** | None |
| 20 | `pats.tbl_FinancialHardshipApplication` | `FinancialHardshipApplication` | `1 = 1` | **Full load** | None |
| 21 | `pats.tbl_Fmp` | `tblFMP` | `1 = 1` | **Full load** | None |
| 22 | `pats.tbl_MNComprehensiveAssessment` | `MNComprehensiveAssessment` | `1 = 1` | **Full load** | None |
| 23 | `pats.tbl_MNComprehensiveAssessmentLevelOfCare` | `MNComprehensiveAssessmentLevelOfCare` | `1 = 1` | **Full load** | None |
| 24 | `pats.tbl_mntreatmentservicereview` | `mntreatmentservicereview` | `(CreatedOn >= @WorkDate OR ModifiedOn >= @WorkDate)` | **âˆ’15 days** on `CreatedOn` OR `ModifiedOn` | None |
| 25 | `pats.tbl_NewAdmissionassessment` | `NewAdmissionAssessment` | `1 = 1` | **Full load** | None |
| 26 | `pats.tbl_NewAdmissionAssessmentASAMDimension6` | `NewAdmissionAssessmentASAMDimension6` | `1 = 1` | **Full load** | None |
| 27 | `pats.tbl_newdischargetransferplanform` | `newdischargetransferplanform` | `(CreatedOn >= @WorkDate OR ModifiedOn >= @WorkDate)` | **âˆ’15 days** on `CreatedOn` OR `ModifiedOn` | None |
| 28 | `pats.tbl_NewPeriodicReassessment` | `NewPeriodicReassessment` | `1 = 1` | **Full load** | None |
| 29 | `pats.tbl_NewPeriodicReassessmentCounselorReview` | `NewPeriodicReassessmentCounselorReview` | `1 = 1` | **Full load** | None |
| 30 | `pats.tbl_newperiodicreassessmentd2` | `newperiodicreassessmentd2` | `1 = 1` | **Full load** | None |
| 31 | `pats.tbl_newperiodicreassessmentd3` | `newperiodicreassessmentd3` | `1 = 1` | **Full load** | None |
| 32 | `pats.tbl_newperiodicreassessmentd4` | `newperiodicreassessmentd4` | `1 = 1` | **Full load** | None |
| 33 | `pats.tbl_newperiodicreassessmentd5` | `newperiodicreassessmentd5` | `1 = 1` | **Full load** | None |
| 34 | `pats.tbl_newperiodicreassessmentd6` | `newperiodicreassessmentd6` | `1 = 1` | **Full load** | None |
| 35 | `pats.tbl_PA` | `PeriodicReassessment` | `1 = 1` | **Full load** | None |
| 36 | `pats.tbl_PACounselorReview` | `PACounselorReview` | `1 = 1` | **Full load** | None |
| 37 | `pats.tbl_PADimension1` | `PADimension1` | `1 = 1` | **Full load** | None |
| 38 | `pats.tbl_PADimension2` | `PADimension2` | `1 = 1` | **Full load** | None |
| 39 | `pats.tbl_PADimension3` | `PADimension3` | `1 = 1` | **Full load** | None |
| 40 | `pats.tbl_PADimension4` | `PADimension4` | `1 = 1` | **Full load** | None |
| 41 | `pats.tbl_PADimension5` | `PADimension5` | `1 = 1` | **Full load** | None |
| 42 | `pats.tbl_PADimension6` | `PADimension6` | `1 = 1` | **Full load** | None |
| 43 | `pats.tbl_PayerCltHistory` | `tblPayerCltHistory` | `pyDtm IS NOT NULL AND pyDtm >= @WorkDate` | **âˆ’15 days** on `pyDtm` | None |
| 44 | `pats.tbl_pbi3PayAuth` | `tbl3PAYauth` | `1 = 1` | **Full load** | None |
| 45 | `pats.tbl_PreadmissionReferralSource` | `SF_PatientPreadmissionReferralSource` | `ISNULL(LastUpdateOn, CreatedOn) >= @WorkDate` | **âˆ’515 days** (`DaysBack - 500 = -515`) on `LastUpdateOn` or `CreatedOn` | YES â€” `updatedProgram.cs`: `mydaysback = DaysBack - 500 = -515` |
| 46 | `pats.tbl_SERVICES` | `tblSERVICES` | `1 = 1` | **Full load** | None |
| 47 | `pats.tbl_SF_DataForms` | `SF_DataForms` | `(CreatedOn >= @WorkDate OR LastUpdatedOn >= @WorkDate)` | **âˆ’15 days** on `CreatedOn` OR `LastUpdatedOn` | None |
| 48 | `pats.tbl_SMSTextConsentForm` | `SMSTextConsentForm` | `1 = 1` | **Full load** | None |
| 49 | `pats.tbl_takehomeagreementanddiversioncontrol` | `takehomeagreementanddiversioncontrol` | `(CreatedOn >= @WorkDate OR ModifiedOn >= @WorkDate)` | **âˆ’15 days** on `CreatedOn` OR `ModifiedOn` | None |
| 50 | `pats.tbl_TakeHomeRiskAssessment` | `TakeHomeRiskAssessment` | `1 = 1` | **Full load** | None |
| 51 | `pats.tbl_TblDiag10` | `Tbldiag10` | `1 = 1` | **Full load** | None â€” BulkDartsSrvLoader |
| 52 | `pats.tbl_UAResults` | `tblUAResult` | `convert(date,uarResultDt) >= convert(date,@WorkDate) OR convert(date,uarDropDt) >= convert(date,@WorkDate) OR convert(date,uarcreateddt) >= convert(date,@WorkDate)` | **âˆ’15 days** on 3 date columns | YES â€” when `Reload=true`: WHERE changes to `uarresultdt IS NOT NULL` (full table) |
| 53 | `pats.tbl_UASched` | `tblUASched` | `uasDt >= @WorkDate OR uasDtAdded >= @WorkDate` | **âˆ’15 days** on `uasDt` OR `uasDtAdded` | None; also: `SELECT DISTINCT` added in code |
| 54 | `pats.tbl_VAComprehensiveAssessment` | `VAComprehensiveAssessment` | `1 = 1` | **Full load** | None |
| 55 | `pats.tbl_vacomprehensiveassessmentsummary` | `vacomprehensiveassessmentsummary` | `1 = 1` | **Full load** | None |
| 56 | `pats.tbl_vw3pBillSub` | `vw3pBillSub` | `1 = 1` | **Full load** | None â€” but `SaveAuthBillsub` called with `WrkDate` param |
| 57 | `stg.ClientDemo` | `tblclient` | `1 = 1` (task config) | **Full load** | YES â€” entire SQL hardcoded in `updatedProgram.cs`; ORDER BY `cltID` |

---

### Summary by category

| Category | Tables | Bronze Query Strategy |
|----------|--------|----------------------|
| **Full load** (`1 = 1`) | 36 of 57 tables | Copy entire source table each run. Silver MERGE handles dedup via RowChkSum. |
| **âˆ’15 days date filter** | 10 tables | `WHERE [dateCol] >= DATEADD(d,-15,@RunDate)`. Fabric: pipeline parameter `@RunDate`. |
| **Year-boundary** | 2 tables | `WHERE YEAR([dateCol]) >= YEAR(DATEADD(d,-15,@RunDate))` |
| **Custom override in code** | 4 tables | Bills (year+12d), PreAdmission_V6 (quality filter), Cows_V6 (SiteCode only), ClientDemo (full hardcoded SQL) |
| **Extended lookback** | 1 table | PreadmissionReferralSource: âˆ’515 days |

---

### Tables with date-based WHERE â€” exact column list

These tables require a `@RunDate` pipeline parameter for the Fabric Copy Activity `sqlReaderQuery`:

| BHG_DR Destination | Date Columns in WHERE | WHERE Template |
|--------------------|----------------------|----------------|
| `pats.tbl_CheckIn` | `ciDT` | `WHERE convert(date,ciDT) >= convert(date,'@{pipeline().parameters.RunDate}')` |
| `pats.tbl_consenttomarketing` | `CreatedOn`, `ModifiedOn` | `WHERE (CreatedOn >= '@{...}' OR ModifiedOn >= '@{...}')` |
| `pats.tbl_mntreatmentservicereview` | `CreatedOn`, `ModifiedOn` | Same |
| `pats.tbl_newdischargetransferplanform` | `CreatedOn`, `ModifiedOn` | Same |
| `pats.tbl_takehomeagreementanddiversioncontrol` | `CreatedOn`, `ModifiedOn` | Same |
| `pats.tbl_SF_DataForms` | `CreatedOn`, `LastUpdatedOn` | `WHERE (CreatedOn >= '@{...}' OR LastUpdatedOn >= '@{...}')` |
| `pats.tbl_PayerCltHistory` | `pyDtm` | `WHERE pyDtm IS NOT NULL AND pyDtm >= '@{...}'` |
| `pats.tbl_UAResults` | `uarResultDt`, `uarDropDt`, `uarcreateddt` | `WHERE (convert(date,uarResultDt) >= convert(date,'@{...}') OR convert(date,uarDropDt) >= convert(date,'@{...}') OR convert(date,uarcreateddt) >= convert(date,'@{...}'))` |
| `pats.tbl_UASched` | `uasDt`, `uasDtAdded` | `WHERE uasDt >= '@{...}' OR uasDtAdded >= '@{...}'` |
| `pats.tbl_PreadmissionReferralSource` | `LastUpdateOn`, `CreatedOn` | `WHERE ISNULL(LastUpdateOn,CreatedOn) >= '@{adddays(pipeline().parameters.RunDate,-515)}'` |
| `pats.tbl_3pElig` | `edate` | `WHERE Year(edate) >= Year('@{...}')` |
| `pats.tbl_Bills` | `billDate` | `WHERE YEAR(billDate) >= YEAR(DATEADD(d,-15,@RunDate)) AND billDate <= DATEADD(d,12,@RunDate)` |

> **Fabric `@RunDate` parameter** = today's date passed in at pipeline trigger time. The `-15` offset is applied inside the `sqlReaderQuery` expression.  
> For tables with `1 = 1`, no `@RunDate` parameter is needed in the Copy Activity query.

---

---

## Appendix G — Bronze Column Lists per Table (ActionKey=1, Enabled=1)

> **Source:** `dms.vw_MapSrc2Dsn` filtered to `ActionKey=1, Enabled=1`, sorted by `FieldKey`.
> 
> **How to read this appendix:**
> - **Source (FieldName)** = column name to use in SAMMS source `SELECT` (e.g. in Fabric Copy Activity `sqlReaderQuery`)
> - **Destination (DsnFieldName)** = target column name in BHG_DR table
> - **PK** = participates in Merge Key (`PK1` = SiteCode param, `PK2` = primary key, `PK3+` = composite key parts)
> - **In CHECKSUM** = whether this field is included in `CHECKSUM(...)` expression (`No` for types: `ntext`, `varbinary`, `timestamp`, `image`, `Parameter`)
> - **`[HARDCODED SQL]`** = SELECT is built in `updatedProgram.cs`, not from `vw_MapSrc2Dsn`
> 
> **Fabric `@concat` SELECT template for Copy Activity:**
> ```
> SELECT 'SITECODE' AS SiteCode, col1, col2, ..., CHECKSUM(col1,col2,...) AS RowChkSum FROM dbo.SourceTable WHERE <lookback>
> ```
---

### StepKey 3 --- pats.tbl_Bills

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | nvarchar | PK1 | No |
| 2 | billID | billID | int | PK2 | Yes |
| 3 | billCLTID | billCLTID | int |  | Yes |
| 4 | billGuestID | billGuestID | int |  | Yes |
| 5 | billDate | billDate | datetime |  | Yes |
| 6 | billBILL | billBILL | money |  | Yes |
| 7 | billPAY | billPAY | money |  | Yes |
| 8 | billPAYTYPE | billPAYTYPE | varchar |  | Yes |
| 9 | BillAdjust | BillAdjust | money |  | Yes |
| 10 | billReason | billReason | varchar |  | Yes |
| 11 | billReceiptNum | billReceiptNum | int |  | Yes |
| 12 | strUser | strUser | varchar |  | Yes |
| 13 | blnDeposit | blnDeposit | bit |  | Yes |
| 14 | dtDeposit | dtDeposit | datetime |  | Yes |
| 15 | billADJUSTID | billADJUSTID | int |  | Yes |
| 16 | FIFOallocated | FIFOallocated | bit |  | Yes |
| 17 | FIFObalance | FIFObalance | money |  | Yes |
| 18 | Costcenter | Costcenter | varchar |  | Yes |
| 19 | BillAptID | BillAptID | int |  | Yes |
| 20 | billORGdt | billORGdt | date |  | Yes |
| 21 | BillServID | BillServID | int |  | Yes |
| 22 | BillSiteID | BillSiteID | int |  | Yes |

---

### StepKey 5 --- pats.tbl_Enrollment

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | ID | ID | int | PK2 | Yes |
| 2 | @SiteCode | SiteCode | nvarchar | PK1 | No |
| 3 | cltID | cltID | int |  | Yes |
| 4 | Program | Program | varchar |  | Yes |
| 5 | EnrollDate | EnrollDate | datetime |  | Yes |
| 6 | EnrollReasonCode | EnrollReasonCode | varchar |  | Yes |
| 7 | EnrollReasonText | EnrollReasonText | varchar |  | Yes |
| 8 | DischargeReasonCode | DischargeReasonCode | varchar |  | Yes |
| 9 | DischargeReasonText | DischargeReasonText | varchar |  | Yes |
| 10 | DischargeDate | DischargeDate | datetime |  | Yes |
| 11 | strStaff | strStaff | varchar |  | Yes |
| 12 | Transfer | Transfer | bit |  | Yes |
| 13 | ParentEnrollID | ParentEnrollID | int |  | Yes |
| 14 | NoDarts_Enroll | NoDarts_Enroll | bit |  | Yes |
| 15 | NoDarts_Discharge | NoDarts_Discharge | bit |  | Yes |
| 16 | repOldEnroll | repOldEnroll | numeric |  | Yes |
| 17 | DASAreason | DASAreason | varchar |  | Yes |
| 18 | dtLastContact | dtLastContact | datetime |  | Yes |
| 19 | strArrests | strArrests | varchar |  | Yes |
| 20 | strBaby | strBaby | varchar |  | Yes |
| 21 | strBabyDF | strBabyDF | varchar |  | Yes |
| 22 | strEduc | strEduc | varchar |  | Yes |
| 23 | strEmpStat | strEmpStat | varchar |  | Yes |
| 24 | strLiving | strLiving | varchar |  | Yes |
| 25 | strNILF | strNILF | varchar |  | Yes |
| 26 | strPriFreq | strPriFreq | varchar |  | Yes |
| 27 | strPriProb | strPriProb | varchar |  | Yes |
| 28 | strSecFreq | strSecFreq | varchar |  | Yes |
| 29 | strSecProb | strSecProb | varchar |  | Yes |
| 30 | strSelfHelp | strSelfHelp | varchar |  | Yes |
| 31 | strSelfHelpDet | strSelfHelpDet | varchar |  | Yes |
| 32 | strSuppInt | strSuppInt | varchar |  | Yes |
| 33 | strTerFreq | strTerFreq | varchar |  | Yes |
| 34 | strTerProb | strTerProb | varchar |  | Yes |
| 35 | strSchoolJobTraining | strSchoolJobTraining | varchar |  | Yes |
| 36 | Counselor | Counselor | varchar |  | Yes |
| 37 | DischargeSubReasonCode | DischargeSubReasonCode | varchar |  | Yes |
| 38 | EnrollSubReasonCode | EnrollSubReasonCode | varchar |  | Yes |
| 39 | OnDemand | OnDemand | bit |  | Yes |
| 40 | physician | physician | varchar |  | Yes |
| 41 | SiteID | SiteID | int |  | Yes |
| 42 | MODULE | MODULE | varchar |  | Yes |
| 43 | MODULENote | MODULENote | varchar |  | Yes |
| 44 | dischargeIncome | dischargeIncome | nvarchar |  | Yes |
| 45 | intakeIncome | intakeIncome | nvarchar |  | Yes |
| 46 | strDBnotes | strDBnotes | varchar |  | Yes |
| 47 | deleterecord | deleterecord | varchar |  | Yes |
| 48 | TreatmentLevel | TreatmentLevel | nvarchar |  | Yes |

---

### StepKey 7 --- pats.tbl_CheckIn

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | ciID | ciID | int | PK2 | Yes |
| 3 | ciCLTID | ciCLTID | varchar |  | Yes |
| 4 | ciDT | ciDate | datetime |  | Yes |
| 5 | ciTime | ciTime | datetime |  | Yes |
| 6 | ciServedDtm | ciServedDtm | datetime |  | Yes |
| 7 | ciUSER | ciUser | varchar |  | Yes |
| 8 | ciHOLD | ciHOLD | bit |  | Yes |
| 9 | cicltm4id | cicltm4id | varchar |  | Yes |
| 10 | cicltName | cicltName | varchar |  | Yes |
| 11 | ciCode | ciCode | varchar |  | Yes |
| 12 | ciQueue | ciQueue | varchar |  | Yes |
| 13 | ciServedStaff | ciServedStaff | varchar |  | Yes |
| 14 | ciAmt | ciAmt | int |  | Yes |
| 15 | ciDoses | ciDoses | int |  | Yes |
| 16 | ciQUEUETIME | ciQUEUETIME | datetime |  | Yes |

---

### StepKey 9 --- pats.tbl_UAResults

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | uarResultDt | uarResultDt | datetime | PK2 | Yes |
| 3 | uarID | uarID | int | PK3 | Yes |
| 4 | uarLngCltID | uarLngCltID | int |  | Yes |
| 5 | uarSchedID | uarSchedID | int |  | Yes |
| 6 | uarDropDt | uarDropDt | datetime |  | Yes |
| 7 | uarCreatedBy | uarCreatedBy | varchar |  | Yes |
| 8 | uarCreatedDt | uarCreatedDt | datetime |  | Yes |
| 9 | cpID | cpID | int |  | Yes |
| 10 | oldnum | oldnum | int |  | Yes |
| 11 | oldClient | oldClient | varchar |  | Yes |
| 12 | repOldUAr | repOldUAr | numeric |  | Yes |
| 13 | uarLabKey | uarLabKey | varchar |  | Yes |
| 14 | uarUpdatedBy | uarUpdatedBy | varchar |  | Yes |
| 15 | uarUpdatedDt | uarUpdatedDt | datetime |  | Yes |
| 16 | uaType | uaType | varchar |  | Yes |
| 17 | SiteID | SiteID | int |  | Yes |
| 18 | uaDBnotes | uaDBnotes | varchar |  | Yes |
| 19 | uaNurseNote | uaNurseNote | varchar |  | Yes |
| 20 | uaSigDt | uaSigDt | datetime |  | Yes |
| 21 | uaSigUser | uaSigUser | varchar |  | Yes |
| 22 | location | location_ | varchar |  | Yes |
| 23 | scheduledDate | scheduledDate | datetime |  | Yes |
| 24 | uaBase64 | uaBase64 | varchar |  | Yes |
| 25 | UAProgram | UAProgram | varchar |  | Yes |
| 26 | LabName | LabName | varchar |  | Yes |
| 27 | UAEval | UAEval | varchar |  | Yes |

---

### StepKey 11 --- pats.tbl_Codes

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | cdeID | cdeID | int | PK2 | Yes |
| 3 | cdeGroup | cdeGroup | varchar |  | Yes |
| 4 | cdeDesc | cdeDesc | varchar |  | Yes |
| 5 | cdeBillable | cdeBillable | bit |  | Yes |
| 6 | cdeUA | cdeUA | bit |  | Yes |
| 7 | cdeIntAmt | cdeIntAmt | int |  | Yes |
| 8 | cdeLiquid | cdeLiquid | bit |  | Yes |
| 9 | cdeSTAFFCODE | cdeSTAFFCODE | bit |  | Yes |
| 10 | cdeFUND | cdeFUND | varchar |  | Yes |
| 11 | cdeMODAlity | cdeMODAlity | varchar |  | Yes |
| 12 | cdeDRUGFREE | cdeDRUGFREE | bit |  | Yes |
| 13 | cdePROVIDER | cdePROVIDER | varchar |  | Yes |
| 14 | cdeSiteNum | cdeSiteNum | varchar |  | Yes |
| 15 | rowguid | rowguid | uniqueidentifier |  | Yes |
| 16 | cdeBillableResidential | cdeBillableResidential | bit |  | Yes |
| 17 | cdeServiceSetting | cdeServiceSetting | varchar |  | Yes |
| 18 | cdeDischargeType | cdeDischargeType | varchar |  | Yes |
| 19 | cdeSigRequired | cdeSigRequired | bit |  | Yes |
| 20 | cdeResidential | cdeResidential | bit |  | Yes |
| 21 | cdeAllowOverlap | cdeAllowOverlap | bit |  | Yes |
| 22 | duiAMT | duiAMT | decimal |  | Yes |
| 23 | duiHourRate | duiHourRate | decimal |  | Yes |
| 24 | blDEFAULT | blDEFAULT | bit |  | Yes |
| 25 | WeeklyFee | WeeklyFee | int |  | Yes |
| 26 | MustHaveBilling | MustHaveBilling | bit |  | Yes |
| 27 | Suboxoneprog | Suboxoneprog | bit |  | Yes |
| 28 | cdeInsurance | cdeInsurance | bit |  | Yes |
| 29 | DefRate | DefRate | money |  | Yes |
| 30 | SiteID | SiteID | int |  | Yes |
| 31 | cdelblcolor | cdelblcolor | varchar |  | Yes |
| 32 | cde3pdonotbill | cde3pdonotbill | bit |  | Yes |
| 33 | cde3pPOSoverride | cde3pPOSoverride | varchar |  | Yes |
| 34 | IsPrescreening | IsPrescreening | bit |  | Yes |
| 35 | OBAT | OBAT | bit |  | Yes |
| 36 | ReqAuth | ReqAuth | bit |  | Yes |
| 37 | IntakeProg | IntakeProg | bit |  | Yes |

---

### StepKey 12 --- pats.tbl_pbi3PayAuth

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | tpaID | tpaID | int | PK2 | Yes |
| 3 | tpEFFDate | tpEFFDate | date |  | Yes |
| 4 | tpaCLTID | tpaCLTID | int |  | Yes |
| 5 | tpaPayer | tpaPayer | nvarchar |  | Yes |
| 6 | tpaDESC | tpaDESC | ntext |  | No |
| 7 | tpaEffDATE | tpaEffDATE | datetime |  | Yes |
| 8 | tpaTermDATE | tpaTermDATE | datetime |  | Yes |
| 9 | tpaSTAFF | tpaSTAFF | nvarchar |  | Yes |
| 10 | tpadt | tpadt | datetime |  | Yes |
| 11 | tpaAuthCode | tpaAuthCode | varchar |  | Yes |
| 12 | tpAUTHPATH | tpAUTHPATH | varchar |  | Yes |
| 13 | tpCONFIRMPath | tpCONFIRMPath | varchar |  | Yes |
| 14 | TpFail | TpFail | varchar |  | Yes |
| 15 | tpRequestForm | tpRequestForm | varchar |  | Yes |
| 16 | tpResponseForm | tpResponseForm | varchar |  | Yes |
| 17 | tpServ | tpServ | varchar |  | Yes |
| 18 | tpTermDate | tpTermDate | date |  | Yes |
| 19 | tpUNITS | tpUNITS | int |  | Yes |
| 20 | tpSERVAPPROVED | tpSERVAPPROVED | varchar |  | Yes |
| 21 | tpNOTE | tpNOTE | varchar |  | Yes |
| 22 | tpTYPE | tpTYPE | varchar |  | Yes |

---

### StepKey 16 --- pats.tbl_3pElig

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | eID | eID | int | PK2 | Yes |
| 3 | eCLT | eCLT | int |  | Yes |
| 4 | ePAYER | ePAYER | varchar |  | Yes |
| 5 | eDATE | eDATE | date |  | Yes |
| 6 | eStaff | eStaff | varchar |  | Yes |
| 7 | ePOST | ePOST | varchar |  | Yes |
| 8 | eRESPONSE | eRESPONSE | varchar |  | Yes |
| 9 | eStatus | eStatus | varchar |  | Yes |
| 10 | eFormat | eFormat | varchar |  | Yes |
| 11 | Filepath | Filepath | varchar |  | Yes |
| 12 | eELECSTATUS | eELECSTATUS | varchar |  | Yes |
| 13 | EStaffSTATUS | EStaffSTATUS | varchar |  | Yes |
| 14 | EStaffNote | EStaffNote | varchar |  | Yes |
| 15 | eSCAN | eSCAN | varchar |  | Yes |
| 16 | eORIGID | eORIGID | int |  | Yes |
| 17 | pyeligcheck | pyeligcheck | date |  | Yes |

---

### StepKey 17 --- pats.tbl_SERVICES

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | sID | sID | int | PK2 | Yes |
| 3 | sSERVICE | sSERVICE | varchar |  | Yes |
| 4 | sArea | sArea | varchar |  | Yes |
| 5 | sCost | sCost | money |  | Yes |
| 6 | sCPTCODE | sCptCode | varchar |  | Yes |
| 7 | sREQSIG | sREQSIG | bit |  | Yes |
| 8 | sREQTIME | sREQTIME | bit |  | Yes |
| 9 | blAllowOverlap | blAllowOverlap | bit |  | Yes |
| 10 | oldArea | oldArea | varchar |  | Yes |
| 11 | oldSrv | oldSrv | varchar |  | Yes |
| 12 | sFilter | sFilter | bit |  | Yes |
| 13 | sReportBillable | sReportBillable | bit |  | Yes |
| 14 | sTimeOnly | sTimeOnly | bit |  | Yes |

---

### StepKey 18 --- ctrl.tbl_Clinic

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PKEY | PKEY | int | PK2 | Yes |
| 3 | DoseWarn | DoseWarn | nvarchar |  | Yes |
| 4 | DoseStop | DoseStop | nvarchar |  | Yes |
| 5 | Photos | Photos | nvarchar |  | Yes |
| 6 | Bottles | Bottles | bit |  | Yes |
| 7 | Overdue | Overdue | int |  | Yes |
| 8 | BillHold | BillHold | int |  | Yes |
| 9 | Test | Test | int |  | Yes |
| 10 | TBTest | TBTest | bit |  | Yes |
| 11 | Force | Force | bit |  | Yes |
| 12 | LastUpdated | LastUpdated | datetime |  | Yes |
| 13 | Note | Note | ntext |  | No |
| 14 | Provider | Provider | nvarchar |  | Yes |
| 15 | Site | Site | nvarchar |  | Yes |
| 16 | CLINICCODE | CLINICCODE | nvarchar |  | Yes |
| 17 | DischargeGuest | DischargeGuest | bit |  | Yes |
| 18 | NumInventory | NumInventory | int |  | Yes |
| 19 | SCHEDUA | SCHEDUA | bit |  | Yes |
| 20 | UAMONTHLY | UAMONTHLY | bit |  | Yes |
| 21 | ClinicNAME | ClinicNAME | varchar |  | Yes |
| 22 | TPAUTOMATION | TPAUTOMATION | int |  | Yes |
| 23 | RequireDarts | RequireDarts | bit |  | Yes |
| 24 | PhysicalTestDays | PhysicalTestDays | int |  | Yes |
| 25 | ASPhysicalTest | ASPhysicalTest | bit |  | Yes |
| 26 | ServiceOverlapPopup | ServiceOverlapPopup | bit |  | Yes |
| 27 | OrangeandWhite | OrangeandWhite | bit |  | Yes |
| 28 | ToxProvider | ToxProvider | varchar |  | Yes |
| 29 | NumberofReceipts | NumberofReceipts | int |  | Yes |
| 30 | PasswordEnforce | PasswordEnforce | bit |  | Yes |
| 31 | PasswordLength | PasswordLength | int |  | Yes |
| 32 | helpfile | helpfile | varchar |  | Yes |
| 33 | ScanPath | ScanPath | varchar |  | Yes |
| 34 | DateSigStart | DateSigStart | datetime |  | Yes |
| 35 | ElecSigs | ElecSigs | bit |  | Yes |
| 36 | CreditPriorWeek | CreditPriorWeek | bit |  | Yes |
| 37 | DefaultTabOrange | DefaultTabOrange | bit |  | Yes |
| 38 | BottleWeight | BottleWeight | decimal |  | Yes |
| 39 | spGRAVITY | spGRAVITY | decimal |  | Yes |
| 40 | DoseCharge | DoseCharge | bit |  | Yes |
| 41 | TimeOffset | TimeOffset | int |  | Yes |
| 42 | CoSign | CoSign | bit |  | Yes |
| 43 | DefaultProgram | DefaultProgram | varchar |  | Yes |
| 44 | ClientSecurity | ClientSecurity | bit |  | Yes |
| 45 | AutoCheckin | AutoCheckin | bit |  | Yes |
| 46 | CheckinCheck | CheckinCheck | bit |  | Yes |
| 47 | OrderService | OrderService | bit |  | Yes |
| 48 | Residential | Residential | bit |  | Yes |
| 49 | BillDirection | BillDirection | varchar |  | Yes |
| 50 | SmallReceipts | SmallReceipts | bit |  | Yes |
| 51 | DuplicateCheckinCheck | DuplicateCheckinCheck | bit |  | Yes |
| 52 | NoPrintCheckinLabel | NoPrintCheckinLabel | bit |  | Yes |
| 53 | AdDomain | AdDomain | varchar |  | Yes |
| 54 | AutoSetLABELPrinter | AutoSetLABELPrinter | varchar |  | Yes |
| 55 | AutoSetReceiptPrinter | AutoSetReceiptPrinter | varchar |  | Yes |
| 56 | ClinicLetter | ClinicLetter | varchar |  | Yes |
| 57 | ClinicState | ClinicState | varchar |  | Yes |
| 58 | Liquid | Liquid | bit |  | Yes |
| 59 | OtherInvType | OtherInvType | varchar |  | Yes |
| 60 | PrintDoseAmt | PrintDoseAmt | bit |  | Yes |
| 61 | Tabs | Tabs | bit |  | Yes |
| 62 | DailyServices | DailyServices | bit |  | Yes |
| 63 | clientsearchRIN | clientsearchRIN | bit |  | Yes |
| 64 | ClientServiceBilling | ClientServiceBilling | bit |  | Yes |
| 65 | DischargeClearsHolds | DischargeClearsHolds | bit |  | Yes |
| 66 | DrugFreeOnly | DrugFreeOnly | bit |  | Yes |
| 67 | halfweekcredit | halfweekcredit | bit |  | Yes |
| 68 | AllowRinZero | AllowRinZero | bit |  | Yes |
| 69 | AllowAnyRin | AllowAnyRin | bit |  | Yes |
| 70 | DefaultShowHoldAtNursing | DefaultShowHoldAtNursing | bit |  | Yes |
| 71 | HideElecSigDates | HideElecSigDates | bit |  | Yes |
| 72 | QueSearch | QueSearch | bit |  | Yes |
| 73 | EducFieldIsEmpStatus | EducFieldIsEmpStatus | bit |  | Yes |
| 74 | AutoImportUA | AutoImportUA | bit |  | Yes |
| 75 | FastDose | FastDose | bit |  | Yes |
| 76 | RecIDPrint | RecIDPrint | bit |  | Yes |
| 77 | NurseSig | NurseSig | bit |  | Yes |
| 78 | ORDER2CONFIRM | ORDER2CONFIRM | int |  | Yes |
| 79 | ORDERCONFIRM | ORDERCONFIRM | int |  | Yes |
| 80 | ToxACCT | ToxACCT | varchar |  | Yes |
| 81 | spGravityClear | spGravityClear | decimal |  | Yes |
| 82 | toxtixnum | toxtixnum | int |  | Yes |
| 83 | toxTixspecial | toxTixspecial | varchar |  | Yes |
| 84 | AutoOrderExpirationHolds | AutoOrderExpirationHolds | bit |  | Yes |
| 85 | reqallintake | reqallintake | bit |  | Yes |
| 86 | NumberOfBulkLabels | NumberOfBulkLabels | int |  | Yes |
| 87 | UAonVISIT | UAonVISIT | bit |  | Yes |
| 88 | Diversion_Padding | Diversion_Padding | int |  | Yes |
| 89 | WORDPATH | WORDPATH | varchar |  | Yes |
| 90 | ScanDeleteOriginal | ScanDeleteOriginal | bit |  | Yes |
| 91 | UDSpanelRequired | UDSpanelRequired | bit |  | Yes |
| 92 | AutoDischargeCredit | AutoDischargeCredit | bit |  | Yes |
| 93 | BeakerColors | BeakerColors | bit |  | Yes |
| 94 | NumberPriorTransactionsOnReceipt | NumberPriorTransactionsOnReceipt | int |  | Yes |
| 95 | AlwaysAllowUseSavedSignature | AlwaysAllowUseSavedSignature | bit |  | Yes |
| 96 | NewBottleLabels | NewBottleLabels | bit |  | Yes |
| 97 | DocTemplatePath | DocTemplatePath | varchar |  | Yes |
| 98 | ReportDir | ReportDir | varchar |  | Yes |
| 99 | reportServer | reportServer | varchar |  | Yes |
| 100 | DonotallowCASCADE | DonotallowCASCADE | bit |  | Yes |
| 101 | isBHG | isBHG | bit |  | Yes |
| 102 | MultipleQueues | MultipleQueues | bit |  | Yes |
| 103 | LabAcct | LabAcct | varchar |  | Yes |
| 104 | AlwaysAskBagLabel | AlwaysAskBagLabel | bit |  | Yes |
| 105 | PrepackBagLabelDefault | PrepackBagLabelDefault | bit |  | Yes |
| 106 | DefaultShowHoldFront | DefaultShowHoldFront | bit |  | Yes |
| 107 | ShowFutureUAholds | ShowFutureUAholds | bit |  | Yes |
| 108 | OpenOnSunday | OpenOnSunday | bit |  | Yes |
| 109 | ChargeBeforeDose | ChargeBeforeDose | bit |  | Yes |
| 110 | LandscapeLabel | LandscapeLabel | bit |  | Yes |
| 111 | sigIMGPATH | sigIMGPATH | varchar |  | Yes |
| 112 | sigIMGURI | sigIMGURI | varchar |  | Yes |
| 113 | SignBeforeDose | SignBeforeDose | bit |  | Yes |
| 114 | SortClientSearchbyID | SortClientSearchbyID | bit |  | Yes |
| 115 | UApath | UApath | varchar |  | Yes |
| 116 | AdjustmentEmail | AdjustmentEmail | varchar |  | Yes |
| 117 | blAdjustatDischarge | blAdjustatDischarge | bit |  | Yes |
| 118 | Fifo_Bottle | Fifo_Bottle | bit |  | Yes |
| 119 | UseCostCenter | UseCostCenter | bit |  | Yes |
| 120 | ForceCheckin | ForceCheckin | bit |  | Yes |
| 121 | VerifyMedAdjustment | VerifyMedAdjustment | bit |  | Yes |
| 122 | PinSigs | PinSigs | bit |  | Yes |
| 123 | COMBINE3PAYFEES | COMBINE3PAYFEES | bit |  | Yes |
| 124 | PinBeforeSig | PinBeforeSig | bit |  | Yes |
| 125 | siglcd | siglcd | bit |  | Yes |
| 126 | DictionaryPath | DictionaryPath | varchar |  | Yes |
| 127 | grammerpath | grammerpath | varchar |  | Yes |
| 128 | DiversionType | DiversionType | varchar |  | Yes |
| 129 | ismedmark | ismedmark | bit |  | Yes |
| 130 | ServiceDimsLinkToTP | ServiceDimsLinkToTP | bit |  | Yes |
| 131 | advancedtesting | advancedtesting | bit |  | Yes |
| 132 | AllowActOldOrder | AllowActOldOrder | bit |  | Yes |
| 133 | FirstInitialonTOXlabel | FirstInitialonTOXlabel | bit |  | Yes |
| 134 | Over100check | Over100check | bit |  | Yes |
| 135 | NoQuePop | NoQuePop | bit |  | Yes |
| 136 | offsetdoseconfirm | offsetdoseconfirm | bit |  | Yes |
| 137 | OrderRequestsNeedBothSigs | OrderRequestsNeedBothSigs | bit |  | Yes |
| 138 | SmallTox | SmallTox | bit |  | Yes |
| 139 | toxservice | toxservice | bit |  | Yes |
| 140 | Zebra | Zebra | bit |  | Yes |
| 141 | FingerPrintSig | FingerPrintSig | bit |  | Yes |
| 142 | VOregistrationpath | VOregistrationpath | varchar |  | Yes |
| 143 | blockaptcalhold | blockaptcalhold | bit |  | Yes |
| 144 | CalendarStartTime | CalendarStartTime | int |  | Yes |
| 145 | EligPW | EligPW | nvarchar |  | Yes |
| 146 | EligUN | EligUN | nvarchar |  | Yes |
| 147 | FiveDayCalendarWeek | FiveDayCalendarWeek | bit |  | Yes |
| 148 | multitenant | multitenant | bit |  | Yes |
| 149 | pumpwindow | pumpwindow | bit |  | Yes |
| 150 | RequireEmergencyContact | RequireEmergencyContact | bit |  | Yes |
| 151 | QueueTwice | QueueTwice | bit |  | Yes |
| 152 | CheckUAIsPrescription | CheckUAIsPrescription | bit |  | Yes |
| 153 | enableBusPass | enableBusPass | bit |  | Yes |
| 154 | ClaimDir | ClaimDir | varchar |  | Yes |
| 155 | isIHC | isIHC | bit |  | Yes |
| 156 | Phase | Phase | bit |  | Yes |
| 157 | setEvalsOtherFocus | setEvalsOtherFocus | int |  | Yes |
| 158 | EnableHoldayPickupCalifornia | EnableHoldayPickupCalifornia | int |  | Yes |
| 159 | ZeroSSNs | ZeroSSNs | bit |  | Yes |
| 160 | EnableTouchSig | EnableTouchSig | bit |  | Yes |
| 161 | CreditDosesDischarge | CreditDosesDischarge | bit |  | Yes |
| 162 | AllowBulkDrSigs | AllowBulkDrSigs | bit |  | Yes |
| 163 | EnableAlertsMedChanges | EnableAlertsMedChanges | bit |  | Yes |
| 164 | EnableOrderAlerts | EnableOrderAlerts | bit |  | Yes |
| 165 | EnableTestingAlerts | EnableTestingAlerts | bit |  | Yes |
| 166 | EnableAtRiskAlerts | EnableAtRiskAlerts | bit |  | Yes |
| 167 | EnableAdministeringClientMeds | EnableAdministeringClientMeds | bit |  | Yes |
| 168 | DisableServiceUnits | DisableServiceUnits | bit |  | Yes |
| 169 | isMultiProgram | isMultiProgram | bit |  | Yes |
| 170 | versionNbr | versionNbr | decimal |  | Yes |
| 171 | LabelPrintMedTypeInsteadOfMedClass | LabelPrintMedTypeInsteadOfMedClass | bit |  | Yes |
| 172 | EnableEnrollDischargeDateInSearchGrid | EnableEnrollDischargeDateInSearchGrid | bit |  | Yes |
| 173 | EnableServiceRevisions | EnableServiceRevisions | bit |  | Yes |
| 174 | enableBAC | enableBAC | bit |  | Yes |
| 175 | SammsFormsDefaultIndexNumber | SammsFormsDefaultIndexNumber | int |  | Yes |
| 176 | enableDriveMapping | enableDriveMapping | bit |  | Yes |
| 177 | destructbottle | destructbottle | bit |  | Yes |
| 178 | dontprintorders | dontprintorders | bit |  | Yes |
| 179 | DisablePrintServiceMessageAfterSavePrompt | DisablePrintServiceMessageAfterSavePrompt | bit |  | Yes |
| 180 | DisableOtherAsReferralSource | DisableOtherAsReferralSource | bit |  | Yes |
| 181 | EnableInventory4and5 | EnableInventory4and5 | bit |  | Yes |
| 182 | SigPadTest | SigPadTest | bit |  | Yes |
| 183 | EnableRssAlerts | EnableRssAlerts | bit |  | Yes |
| 184 | iispath | iispath | varchar |  | Yes |
| 185 | PrintAlternativeZebraAndDymoLabelVersion1 | PrintAlternativeZebraAndDymoLabelVersion1 | bit |  | Yes |
| 186 | isRNP | isRNP | bit |  | Yes |
| 187 | EnableAutoPopulateCity | EnableAutoPopulateCity | bit |  | Yes |
| 188 | IntakePacketURL | IntakePacketURL | varchar |  | Yes |
| 189 | NoCheckinatPay | NoCheckinatPay | bit |  | Yes |
| 190 | EnableAutoHoldOnAbnormalLab | EnableAutoHoldOnAbnormalLab | bit |  | Yes |
| 191 | MultiQueueRefreshIntervalTimeSet | MultiQueueRefreshIntervalTimeSet | int |  | Yes |
| 192 | EnableSuffixMiddleInitialInFirstNameOfSearch | EnableSuffixMiddleInitialInFirstNameOfSearch | bit |  | Yes |
| 193 | enableUserLoginAtBACqueueModeElseInitials | enableUserLoginAtBACqueueModeElseInitials | bit |  | Yes |
| 194 | SiteID | SiteID | int |  | Yes |
| 195 | PrintSitesAddressDependingOnSites | PrintSitesAddressDependingOnSites | bit |  | Yes |
| 196 | DoNotPrintDOE | DoNotPrintDOE | bit |  | Yes |
| 197 | enableAutoSpSAMMSBilling | enableAutoSpSAMMSBilling | bit |  | Yes |
| 198 | enableCounselorSelectionInMultiProgramSectionOnly | enableCounselorSelectionInMultiProgramSectionOnly | bit |  | Yes |
| 199 | URLassessment | URLassessment | varchar |  | Yes |
| 200 | DisableTPproblemAndInOwnWords | DisableTPproblemAndInOwnWords | bit |  | Yes |
| 201 | enableCustomizableRequirementsForClientInfo | enableCustomizableRequirementsForClientInfo | bit |  | Yes |
| 202 | enableIntakeDischargeIncomeInputs | enableIntakeDischargeIncomeInputs | bit |  | Yes |
| 203 | enableAutoBillingDuringEachToxPrint | enableAutoBillingDuringEachToxPrint | bit |  | Yes |
| 204 | isSH | isSH | bit |  | Yes |
| 205 | enableBACstopDose | enableBACstopDose | bit |  | Yes |
| 206 | enableBACnurseHoldEvenBlowZero | enableBACnurseHoldEvenBlowZero | bit |  | Yes |
| 207 | EnableSignaturesDuringPillCount | EnableSignaturesDuringPillCount | bit |  | Yes |
| 208 | EnableSignatureWhenAdministeringMeds | EnableSignatureWhenAdministeringMeds | bit |  | Yes |
| 209 | CHSAMSID | CHSAMSID | varchar |  | Yes |
| 210 | FTS | FTS | int |  | Yes |
| 211 | Over20 | Over20 | bit |  | Yes |
| 212 | forcefindtype | forcefindtype | bit |  | Yes |
| 213 | HnPUrl | HnPUrl | varchar |  | Yes |
| 214 | EnablePrintMedTypeColorOnIDCard | EnablePrintMedTypeColorOnIDCard | bit |  | Yes |
| 215 | EnablePortraitLabelDoubleSide | EnablePortraitLabelDoubleSide | bit |  | Yes |
| 216 | enableCommentsOnMultiCheckin | enableCommentsOnMultiCheckin | bit |  | Yes |
| 217 | PullPicsFromDB | PullPicsFromDB | bit |  | Yes |
| 218 | EnableCompetentCheckBoxAtDosing | EnableCompetentCheckBoxAtDosing | bit |  | Yes |
| 219 | EnableActivateOrderWhenNotInSuboxoneProg | EnableActivateOrderWhenNotInSuboxoneProg | bit |  | Yes |
| 220 | EnablePrintToxLandscape | EnablePrintToxLandscape | bit |  | Yes |
| 221 | EnableFlagNurseForBAC | EnableFlagNurseForBAC | bit |  | Yes |
| 222 | BottleReturnNote | BottleReturnNote | bit |  | Yes |
| 223 | BHGMarginTH | BHGMarginTH | int |  | Yes |
| 224 | BHGMarginTox | BHGMarginTox | int |  | Yes |
| 225 | NoCheckinService | NoCheckinService | bit |  | Yes |
| 226 | NoSAMMSFormHeader | NoSAMMSFormHeader | bit |  | Yes |
| 227 | PrintUnitDoseLabel | PrintUnitDoseLabel | bit |  | Yes |
| 228 | AuthBasedOnProgram | AuthBasedOnProgram | bit |  | Yes |
| 229 | LandscapeZebra | LandscapeZebra | bit |  | Yes |
| 230 | ShowBalanceAtDispense | ShowBalanceAtDispense | bit |  | Yes |
| 231 | SingleQueueRefreshIntervalTimeSet | SingleQueueRefreshIntervalTimeSet | int |  | Yes |
| 232 | MultiDosingClinic | MultiDosingClinic | bit |  | Yes |
| 233 | BlasterWide | BlasterWide | bit |  | Yes |
| 234 | PumpCalibrate | PumpCalibrate | bit |  | Yes |
| 235 | CheckVisitingPatient | CheckVisitingPatient | bit |  | Yes |
| 236 | RequireClientSignatureOrderRequest | RequireClientSignatureOrderRequest | bit |  | Yes |
| 237 | DischargedAllowAddPayer | DischargedAllowAddPayer | bit |  | Yes |
| 238 | DymoDetailed | DymoDetailed | bit |  | Yes |

---

### StepKey 19 --- pats.tbl_CustomQuestions

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | Parameter | PK1 | No |
| 2 | cID | cID | int | PK2 | Yes |
| 3 | cQuestion | cQuestion | varchar |  | Yes |

---

### StepKey 20 --- pats.tbl_CustomAnswers

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | Parameter | PK1 | No |
| 2 | caID | caID | int | PK2 | Yes |
| 3 | caQID | caQID | int | PK3 | Yes |
| 4 | caCLTID | caCLTID | int | PK4 | Yes |
| 5 | caANS | caANS | varchar |  | Yes |

---

### StepKey 23 --- pats.tbl_Cows_V6 [HARDCODED SQL]

Source SQL built entirely in updatedProgram.cs (SaveCows_v6). Dynamically detects renamed columns via sys.all_columns.
Core columns: SiteCode(param), id->COWID, p.PatientID, preadmissionid, dttime, dtdate, CltID, RestingPulseRate, sweating, Restlessness, gooseflesh, runnyNose, Lacrimation, Tremor, Yawning, AnxietyIrritability, BoneJointAching, GIUpset, temp, score, 12x DroDownListItems JOIN descriptions.
CHECKSUM fields: c.id, p.PatientID, c.preadmissionid, dttime, c.RestingPulseRate, c.Sweat, c.Restless, c.GooseFlesh, c.RunnyNose, c.Lacrimation, c.Tremor, c.YawningFreq, c.AnxietyIrritability, c.BoneJointAchingMuscle, c.GIUpset, c.temp, c.score

---

### StepKey 24 --- ayx.tbl_PreAdmission_V6 [HARDCODED SQL]

Source SQL built entirely in updatedProgram.cs (SavePreAdmissionV6). 4-way JOIN: SF_PatientPreAdmission + SF_Program + tblCodes(x2) + tblClient.
Key columns: SiteCode(param), pp.id, pp.PatientID, pp.PatientAddress, pp.PatientCity, pp.PatientState, pp.PatientZip, pp.IntakeProgramDate, pp.LastUpdateOn, pp.LastUpdatedBy, pp.PatientSignatureDate, pp.DateofRelease, pp.Version, pp.IsDeleted, pp.CreatedOn, pp.ClientAddress, clt.cltM4ID
CHECKSUM fields: pp.id, pp.PatientID, pp.LastUpdatedBy, pp.LastUpdateOn, pp.PatientSignatureDate, pp.DateofRelease, pp.Version, pp.IsDeleted, clt.cltM4ID

---

### StepKey 26 --- pats.tbl_UASched

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | uasID | uasID | int | PK2 | Yes |
| 3 | uasLngCltID | uasLngCltID | int |  | Yes |
| 4 | uasDt | uasDt | datetime |  | Yes |
| 5 | uasDtAdded | uasDtAdded | datetime |  | Yes |
| 6 | uasStat | uasStat | varchar |  | Yes |
| 7 | uasStatDt | uasStatDt | datetime |  | Yes |
| 8 | uasStatUser | uasStatUser | nvarchar |  | Yes |
| 9 | lngCPAno | lngCPAno | int |  | Yes |
| 10 | uasNOTE | uasNOTE | varchar |  | Yes |
| 11 | oldNum | oldNum | varchar |  | Yes |
| 12 | oldClient | oldClient | varchar |  | Yes |
| 13 | repOldUAs | repOldUAs | numeric |  | Yes |
| 14 | uasCollectedBy | uasCollectedBy | varchar |  | Yes |
| 15 | uasCollectedDate | uasCollectedDate | datetime |  | Yes |
| 16 | uasManifestDate | uasManifestDate | datetime |  | Yes |
| 17 | uasPanel | uasPanel | varchar |  | Yes |
| 18 | uasPanelOther | uasPanelOther | varchar |  | Yes |
| 19 | uasType | uasType | varchar |  | Yes |
| 20 | uasETG | uasETG | bit |  | Yes |
| 21 | uapriority | uapriority | varchar |  | Yes |
| 22 | uasticketprintdate | uasticketprintdate | datetime |  | Yes |

---

### StepKey 27 --- pats.tbl_vw3pBillSub

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | descript | descript | varchar |  | Yes |
| 3 | billdatecriteria | billdatecriteria | datetime |  | Yes |
| 4 | payDEFAULTSUBMIT | payDEFAULTSUBMIT | varchar |  | Yes |
| 5 | ScrubError | ScrubError | varchar |  | Yes |
| 6 | dsID | dsID | int | PK2 | Yes |
| 7 | dsClt | dsClt | int |  | Yes |
| 8 | dsTxtSrv | dsTxtSrv | nvarchar |  | Yes |
| 9 | dsDtStart | dsDtStart | datetime |  | Yes |
| 10 | dsDtEnd | dsDtEnd | datetime |  | Yes |
| 11 | dsTxtType | dsTxtType | nvarchar |  | Yes |
| 12 | dsdblUnits | dsdblUnits | float |  | Yes |
| 13 | billUnits | billUnits | float |  | Yes |
| 14 | dstxtStaff | dstxtStaff | nvarchar |  | Yes |
| 15 | npi | npi | varchar |  | Yes |
| 16 | DSbilled | DSbilled | datetime |  | Yes |
| 17 | pyPAYERID | pyPAYERID | varchar |  | Yes |
| 18 | pySUBSID | pySUBSID | varchar |  | Yes |
| 19 | pyGROUP | pyGROUP | varchar |  | Yes |
| 20 | CPTCODE | CPTCODE | varchar |  | Yes |
| 21 | charge | charge | float |  | Yes |
| 22 | tpaAuthCode | tpaAuthCode | varchar |  | Yes |
| 23 | clientname | clientname | varchar |  | Yes |
| 24 | cltDOB | cltDOB | datetime |  | Yes |
| 25 | cltGender | cltGender | varchar |  | Yes |
| 26 | cltADD1 | cltADD1 | varchar |  | Yes |
| 27 | cltCity | cltCity | varchar |  | Yes |
| 28 | cltState | cltState | varchar |  | Yes |
| 29 | cltzip | cltzip | varchar |  | Yes |
| 30 | cltPhone | cltPhone | varchar |  | Yes |
| 31 | cltMARRY | cltMARRY | varchar |  | Yes |
| 32 | cltM4ID | cltM4ID | varchar |  | Yes |
| 33 | dsdiag | dsdiag | varchar |  | Yes |
| 34 | Modifier | Modifier | varchar |  | Yes |
| 35 | dsPOS | dsPOS | varchar |  | Yes |
| 36 | NDC | NDC | varchar |  | Yes |
| 37 | MG | MG | float |  | Yes |
| 38 | SiteID | SiteID | int |  | Yes |
| 39 | dsarea | dsarea | varchar |  | Yes |
| 40 | payclass | payclass | varchar |  | Yes |

---

### StepKey 29 --- pats.tbl_PayerCltHistory

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | pchID | pchID | int | PK2 | Yes |
| 3 | pyID | pyID | int |  | Yes |
| 4 | pyChange | pyChange | varchar |  | Yes |
| 5 | pyDtm | pyDtm | datetime |  | Yes |
| 6 | pyUser | pyUser | varchar |  | Yes |
| 7 | pyNote | pyNote | varchar |  | Yes |

---

### StepKey 30 --- stg.ClientDemo [HARDCODED SQL]

Source SQL hardcoded in updatedProgram.cs. Full table load from dbo.tblclient. Includes LastModAt = GetDate().
Columns: SiteCode(param), cltID, cltM4ID, cltFName, cltLName, cltMName, cltSSN, cltDOB, cltSex, cltRace, cltEthnicity, cltAddress, cltCity, cltState, cltZip, cltPhone, cltPhone2, cltEmail, cltStatus, cltActive, LastModAt=GetDate()
CHECKSUM fields: cltID, cltM4ID, cltFName, cltLName, cltMName, cltSSN, cltDOB, cltSex, cltRace, cltEthnicity, cltAddress, cltCity, cltState, cltZip, cltPhone, cltPhone2, cltEmail, cltStatus, cltActive

---

### StepKey 31 --- ctrl.tbl_3PSETUP

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | 3pID | pID | int | PK2 | Yes |
| 3 | Clinic | Clinic | varchar |  | Yes |
| 4 | Address | Address | varchar |  | Yes |
| 5 | State | State | varchar |  | Yes |
| 6 | Zip | Zip | varchar |  | Yes |
| 7 | NPI | NPI | varchar |  | Yes |
| 8 | TaxID | TaxID | varchar |  | Yes |
| 9 | Medicaid | Medicaid | varchar |  | Yes |
| 10 | City | City | varchar |  | Yes |
| 11 | DRlname | DRlname | varchar |  | Yes |
| 12 | DRfname | DRfname | varchar |  | Yes |
| 13 | DRnpi | DRnpi | varchar |  | Yes |
| 14 | ProviderAddress | ProviderAddress | varchar |  | Yes |
| 15 | ProviderCity | ProviderCity | varchar |  | Yes |
| 16 | ProviderName | ProviderName | varchar |  | Yes |
| 17 | ProviderPhone | ProviderPhone | varchar |  | Yes |
| 18 | ProviderState | ProviderState | varchar |  | Yes |
| 19 | ProviderZip | ProviderZip | varchar |  | Yes |
| 20 | SiteID | SiteID | int |  | Yes |
| 21 | Clia | Clia | varchar |  | Yes |
| 22 | strDBNotes | strDBNotes | varchar |  | Yes |
| 23 | ProviderDesc | ProviderDesc | varchar |  | Yes |
| 24 | blHasPreloader | blHasPreloader | bit |  | Yes |
| 25 | IndividualNPI | IndividualNPI | bit |  | Yes |
| 26 | Taxonomy | Taxonomy | varchar |  | Yes |
| 27 | RowChkSum | RowChkSum | int |  | Yes |

---

### StepKey 32 --- pats.tbl_PreadmissionReferralSource

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | char | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | ClientId | ClientId | int |  | Yes |
| 5 | DataFormId | DataFormId | int |  | Yes |
| 6 | PrimaryReferralSource | PrimaryReferralSource | varchar |  | Yes |
| 7 | SecondaryReferralSource | SecondaryReferralSource | varchar |  | Yes |
| 8 | ReferralSourceNote | ReferralSourceNote | varchar |  | Yes |
| 9 | CreatedBy | CreatedBy | varchar |  | Yes |
| 10 | CreatedOn | CreatedOn | datetime |  | Yes |
| 11 | LastUpdatedBy | LastUpdatedBy | varchar |  | Yes |
| 12 | LastUpdateOn | LastUpdateOn | datetime |  | Yes |
| 13 | EnrollmentId | EnrollmentId | int |  | Yes |
| 14 | Program | Program | char |  | Yes |
| 15 | IsDeleted | IsDeleted | int |  | Yes |
| 16 | ReferralOrganization | ReferralOrganization | varchar |  | Yes |
| 17 | ReferralName | ReferralName | varchar |  | Yes |
| 18 | AccountNotInList | AccountNotInList | int |  | Yes |
| 19 | ContactNotInList | ContactNotInList | int |  | Yes |
| 20 | WhyLeftTreatmentOfBHG | WhyLeftTreatmentOfBHG | varchar |  | Yes |
| 21 | WhyComingBackToBHG | WhyComingBackToBHG | varchar |  | Yes |
| 22 | MostWantToDoDifferently | MostWantToDoDifferently | varchar |  | Yes |
| 23 | Organization | Organization | varchar |  | Yes |
| 24 | Name | Name | varchar |  | Yes |
| 25 | Email | Email | varchar |  | Yes |
| 26 | Phone | Phone | varchar |  | Yes |
| 27 | IsPatientReadmit | IsPatientReadmit | bit |  | Yes |
| 28 | ReferralOrganizationID | ReferralOrganizationID | varchar |  | Yes |
| 29 | ReferralNameID | ReferralNameID | varchar |  | Yes |

---

### StepKey 33 --- pats.tbl_Fmp

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | fmpID | fmpID | int | PK2 | Yes |
| 3 | fmpLngClt | fmpLngClt | int |  | Yes |
| 4 | fmpDtStart | fmpDtStart | datetime |  | Yes |
| 5 | fmpDtProjEnd | fmpDtProjEnd | datetime |  | Yes |
| 6 | fmpDtEnd | fmpDtEnd | datetime |  | Yes |
| 7 | fmpIntRate | fmpIntRate | int |  | Yes |
| 8 | fmpStrReason | fmpStrReason | varchar |  | Yes |
| 9 | fmpStrDesc | fmpStrDesc | varchar |  | Yes |
| 10 | fmpDtAdded | fmpDtAdded | datetime |  | Yes |
| 11 | fmpStrUserAdded | fmpStrUserAdded | varchar |  | Yes |
| 12 | fmpDtEnded | fmpDtEnded | datetime |  | Yes |
| 13 | fmpStrUserEnded | fmpStrUserEnded | varchar |  | Yes |
| 14 | fmPENDTEXT | fmPENDTEXT | varchar |  | Yes |
| 15 | atriskTYPE | atriskTYPE | varchar |  | Yes |

---

### StepKey 58 --- pats.tbl_EandMFormPregnancy

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |

---

### StepKey 62 --- pats.tbl_Admissionassessmentsubstanceusehistory

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | CltId | CltId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | TxEpisode | TxEpisode | nvarchar |  | Yes |
| 6 | SubstanceType | SubstanceType | nvarchar |  | Yes |
| 7 | Substance | Substance | nvarchar |  | Yes |
| 8 | Route | Route | nvarchar |  | Yes |
| 9 | Amount | Amount | nvarchar |  | Yes |
| 10 | FrequencyOfLastUse | FrequencyOfLastUse | nvarchar |  | Yes |
| 11 | PeakUse | PeakUse | nvarchar |  | Yes |
| 12 | AgeOfFirstUse | AgeOfFirstUse | nvarchar |  | Yes |
| 13 | DateOfLastUse | DateOfLastUse | datetime |  | Yes |
| 14 | Withdrawal | Withdrawal | bit |  | Yes |
| 15 | ListSymptoms | ListSymptoms | nvarchar |  | Yes |
| 16 | Notes | Notes | nvarchar |  | Yes |
| 17 | CreatedOn | CreatedOn | datetime |  | Yes |
| 18 | DateOfReported | DateOfReported | datetime |  | Yes |

---

### StepKey 63 --- pats.tbl_ComprehensiveAssessmentForm

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | DataFormId | DataFormId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | ClientName | ClientName | nvarchar |  | Yes |
| 7 | ClientM4Id | ClientM4Id | nvarchar |  | Yes |
| 8 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 9 | CreatedOn | CreatedOn | datetime |  | Yes |
| 10 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 11 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 12 | DDLActiveSubstanceUsers | DDLActiveSubstanceUsers | int |  | Yes |
| 13 | DDLCurrentJob | DDLCurrentJob | int |  | Yes |
| 14 | DDLRelationshipStatus | DDLRelationshipStatus | int |  | Yes |
| 15 | DDLWhatKindOfSchoolAttend | DDLWhatKindOfSchoolAttend | int |  | Yes |
| 16 | DDLLiveWithYou | DDLLiveWithYou | int |  | Yes |
| 17 | DDLPreferredLanguage | DDLPreferredLanguage | int |  | Yes |
| 18 | DDLEmploymentStatus | DDLEmploymentStatus | int |  | Yes |
| 19 | DDLCheckMother | DDLCheckMother | int |  | Yes |
| 20 | DDLCheckFather | DDLCheckFather | int |  | Yes |
| 21 | DDLCheckSibling | DDLCheckSibling | int |  | Yes |
| 22 | DDLCheckMaternalGrandmother | DDLCheckMaternalGrandmother | int |  | Yes |
| 23 | DDLCheckMaternalGrandfather | DDLCheckMaternalGrandfather | int |  | Yes |
| 24 | DDLCheckMaternalAunt | DDLCheckMaternalAunt | int |  | Yes |
| 25 | DDLCheckMaternalUncle | DDLCheckMaternalUncle | int |  | Yes |
| 26 | DDLCheckMaternalCousins | DDLCheckMaternalCousins | int |  | Yes |
| 27 | DDLCheckPaternalGrandmother | DDLCheckPaternalGrandmother | int |  | Yes |
| 28 | DDLCheckPaternalGrandfather | DDLCheckPaternalGrandfather | int |  | Yes |
| 29 | DDLCheckPaternalAunt | DDLCheckPaternalAunt | int |  | Yes |
| 30 | DDLCheckPaternalUncle | DDLCheckPaternalUncle | int |  | Yes |
| 31 | DDLCheckPaternalCousins | DDLCheckPaternalCousins | int |  | Yes |
| 32 | DDLHighestGradeCompleted | DDLHighestGradeCompleted | int |  | Yes |
| 33 | DDLWhatBranch | DDLWhatBranch | int |  | Yes |
| 34 | DDLTypeDischarge | DDLTypeDischarge | int |  | Yes |
| 35 | DDLGender | DDLGender | int |  | Yes |
| 36 | DDLSexualOrientation | DDLSexualOrientation | int |  | Yes |
| 37 | DDLInfluenceDrugs | DDLInfluenceDrugs | int |  | Yes |
| 38 | DDLWhatBranchType | DDLWhatBranchType | int |  | Yes |
| 39 | IsVeteransAdministration | IsVeteransAdministration | bit |  | Yes |
| 40 | IsPeerSupportMeetings | IsPeerSupportMeetings | bit |  | Yes |
| 41 | IsFriendsRecovery | IsFriendsRecovery | bit |  | Yes |
| 42 | IsCourtFines | IsCourtFines | bit |  | Yes |
| 43 | IsOpenWarrants | IsOpenWarrants | bit |  | Yes |
| 44 | IsOpenCourtCases | IsOpenCourtCases | bit |  | Yes |
| 45 | IsDrugTreatmentCourt | IsDrugTreatmentCourt | bit |  | Yes |
| 46 | IsIncarcerated | IsIncarcerated | bit |  | Yes |
| 47 | IsArrested | IsArrested | bit |  | Yes |
| 48 | IsSaferSexPractices | IsSaferSexPractices | bit |  | Yes |
| 49 | IsMakeYouUncomfortable | IsMakeYouUncomfortable | bit |  | Yes |
| 50 | IsFeelingTraumatized | IsFeelingTraumatized | bit |  | Yes |
| 51 | IsPertainingBeingLGBT | IsPertainingBeingLGBT | bit |  | Yes |
| 52 | IsLGBT | IsLGBT | bit |  | Yes |
| 53 | IsCourtOrderedChildSupportPayments | IsCourtOrderedChildSupportPayments | bit |  | Yes |
| 54 | IsArmedForces | IsArmedForces | bit |  | Yes |
| 55 | IsTrainingActivities | IsTrainingActivities | bit |  | Yes |
| 56 | IsEmploymentSituation | IsEmploymentSituation | bit |  | Yes |
| 57 | IsHighSchoolDiplomaGED | IsHighSchoolDiplomaGED | bit |  | Yes |
| 58 | IsReadWriteEffectively | IsReadWriteEffectively | bit |  | Yes |
| 59 | IsMainstreamClasses | IsMainstreamClasses | bit |  | Yes |
| 60 | IsHeldBackSchool | IsHeldBackSchool | bit |  | Yes |
| 61 | IsHaveAnyChildren | IsHaveAnyChildren | bit |  | Yes |
| 62 | IsChildSupportPayments | IsChildSupportPayments | bit |  | Yes |
| 63 | IsCareOfFamilyMembers | IsCareOfFamilyMembers | bit |  | Yes |
| 64 | IsAbuseNeglectGrowingUp | IsAbuseNeglectGrowingUp | bit |  | Yes |
| 65 | IsUnderstandEnglish | IsUnderstandEnglish | bit |  | Yes |
| 66 | IsCountToSupportYou | IsCountToSupportYou | bit |  | Yes |
| 67 | IsCloseRelationship | IsCloseRelationship | bit |  | Yes |
| 68 | IsDeployOverseas | IsDeployOverseas | bit |  | Yes |
| 69 | CheckMother | CheckMother | bit |  | Yes |
| 70 | CheckFather | CheckFather | bit |  | Yes |
| 71 | CheckSibling | CheckSibling | bit |  | Yes |
| 72 | CheckMaternalGrandmother | CheckMaternalGrandmother | bit |  | Yes |
| 73 | CheckMaternalGrandfather | CheckMaternalGrandfather | bit |  | Yes |
| 74 | CheckMaternalAunt | CheckMaternalAunt | bit |  | Yes |
| 75 | CheckMaternalUncle | CheckMaternalUncle | bit |  | Yes |
| 76 | CheckMaternalCousins | CheckMaternalCousins | bit |  | Yes |
| 77 | CheckPaternalGrandmother | CheckPaternalGrandmother | bit |  | Yes |
| 78 | CheckPaternalGrandfather | CheckPaternalGrandfather | bit |  | Yes |
| 79 | CheckPaternalAunt | CheckPaternalAunt | bit |  | Yes |
| 80 | CheckPaternalUncle | CheckPaternalUncle | bit |  | Yes |
| 81 | CheckPaternalCousins | CheckPaternalCousins | bit |  | Yes |
| 82 | CheckVisuallyShowMe | CheckVisuallyShowMe | bit |  | Yes |
| 83 | CheckVerballyExplainItToMe | CheckVerballyExplainItToMe | bit |  | Yes |
| 84 | CheckPersonalExperience | CheckPersonalExperience | bit |  | Yes |
| 85 | CheckTactilelyHandsOn | CheckTactilelyHandsOn | bit |  | Yes |
| 86 | CheckTalkItThrough | CheckTalkItThrough | bit |  | Yes |
| 87 | CheckNoOne | CheckNoOne | bit |  | Yes |
| 88 | CheckImmediateFamily | CheckImmediateFamily | bit |  | Yes |
| 89 | CheckExtendedFamily | CheckExtendedFamily | bit |  | Yes |
| 90 | CheckCloseFriendsOnly | CheckCloseFriendsOnly | bit |  | Yes |
| 91 | CheckFriends | CheckFriends | bit |  | Yes |
| 92 | CheckPeopleWork | CheckPeopleWork | bit |  | Yes |
| 93 | CheckEveryone | CheckEveryone | bit |  | Yes |
| 94 | CheckGamblingDisorder | CheckGamblingDisorder | bit |  | Yes |
| 95 | CheckFoodOvereating | CheckFoodOvereating | bit |  | Yes |
| 96 | CheckEatingDisorders | CheckEatingDisorders | bit |  | Yes |
| 97 | CheckInternetAddiction | CheckInternetAddiction | bit |  | Yes |
| 98 | CheckSocialMediaAddiction | CheckSocialMediaAddiction | bit |  | Yes |
| 99 | CheckLoveIntimacyDependence | CheckLoveIntimacyDependence | bit |  | Yes |
| 100 | CheckFamilyDisorder | CheckFamilyDisorder | bit |  | Yes |
| 101 | CheckFriendsYourselfRecovery | CheckFriendsYourselfRecovery | bit |  | Yes |
| 102 | CheckCoworkers | CheckCoworkers | bit |  | Yes |
| 103 | CheckMeetings | CheckMeetings | bit |  | Yes |
| 104 | CheckOnline | CheckOnline | bit |  | Yes |
| 105 | IsDeleted | IsDeleted | bit |  | Yes |
| 106 | HowLongHadCurrentJob | HowLongHadCurrentJob | int |  | Yes |
| 107 | IsFullTimeStudent | IsFullTimeStudent | bit |  | Yes |
| 108 | IsPartTimeStudent | IsPartTimeStudent | bit |  | Yes |
| 109 | Version | Version | nvarchar |  | Yes |
| 110 | ThoseWhoAreNotcisgender | ThoseWhoAreNotcisgender | nvarchar |  | Yes |
| 111 | CulturalPreferencesForYourTreatment | CulturalPreferencesForYourTreatment | bit |  | Yes |
| 112 | FamilyStruggledWithDrugAlcoholProblems | FamilyStruggledWithDrugAlcoholProblems | bit |  | Yes |
| 113 | ExperiencedAnytraumaAbuseNeglect | ExperiencedAnytraumaAbuseNeglect | bit |  | Yes |
| 114 | PhysicalAbuseViolenceCaptivityOther | PhysicalAbuseViolenceCaptivityOther | bit |  | Yes |
| 115 | VerbalEmotionalFinancialAbuse | VerbalEmotionalFinancialAbuse | bit |  | Yes |
| 116 | NeglectTraumaRelatedYourRace | NeglectTraumaRelatedYourRace | bit |  | Yes |
| 117 | SexualAbuseAssaultSexualExploitation | SexualAbuseAssaultSexualExploitation | bit |  | Yes |
| 118 | CurrentlyExperiencingAbuseNglectExploitation | CurrentlyExperiencingAbuseNglectExploitation | bit |  | Yes |
| 119 | AnyDifficultyCopingWithTrauma | AnyDifficultyCopingWithTrauma | int |  | Yes |
| 120 | HaveYouEverReceivedServices | HaveYouEverReceivedServices | int |  | Yes |
| 121 | ProbationorParole | ProbationorParole | bit |  | Yes |
| 122 | SocialHistoryProblemsWithOther | SocialHistoryProblemsWithOther | bit |  | Yes |
| 123 | FindSupportYourselfInRecoveryOther | FindSupportYourselfInRecoveryOther | bit |  | Yes |
| 124 | RaceWhite | RaceWhite | bit |  | Yes |
| 125 | RaceBlack | RaceBlack | bit |  | Yes |
| 126 | RaceAmericanIndian | RaceAmericanIndian | bit |  | Yes |
| 127 | RaceAsian | RaceAsian | bit |  | Yes |
| 128 | RaceNativeHawaiian | RaceNativeHawaiian | bit |  | Yes |
| 129 | RaceTwoorMore | RaceTwoorMore | bit |  | Yes |
| 130 | RaceOther | RaceOther | bit |  | Yes |
| 131 | RaceOtherTxt | RaceOtherTxt | nvarchar |  | Yes |
| 132 | Hispanic | Hispanic | bit |  | Yes |
| 133 | NonHispanic | NonHispanic | bit |  | Yes |
| 134 | DDLTermsofGender | DDLTermsofGender | int |  | Yes |
| 135 | ObsevationofOthers | ObsevationofOthers | bit |  | Yes |
| 136 | AffectedYourEmployment | AffectedYourEmployment | bit |  | Yes |
| 137 | PhysicalAbuse | PhysicalAbuse | bit |  | Yes |
| 138 | SexualAbuse | SexualAbuse | bit |  | Yes |
| 139 | VerbalAbuse | VerbalAbuse | bit |  | Yes |
| 140 | Neglect | Neglect | bit |  | Yes |
| 141 | Captivity | Captivity | bit |  | Yes |
| 142 | SexualExploitation | SexualExploitation | bit |  | Yes |
| 143 | LaborExploitation | LaborExploitation | bit |  | Yes |
| 144 | TraumaRelatedtoRace | TraumaRelatedtoRace | bit |  | Yes |
| 145 | TraumaOther | TraumaOther | bit |  | Yes |
| 146 | SupportiveSexualOrientaion | SupportiveSexualOrientaion | nvarchar |  | Yes |
| 147 | NotSupportiveSexualOrientaion | NotSupportiveSexualOrientaion | nvarchar |  | Yes |
| 148 | SubstancesAffectedYourLife | SubstancesAffectedYourLife | nvarchar |  | Yes |
| 149 | AlwaysFollowsSaferSexPracices | AlwaysFollowsSaferSexPracices | bit |  | Yes |
| 150 | HaveHighSchoolDiploma | HaveHighSchoolDiploma | bit |  | Yes |

---

### StepKey 64 --- pats.tbl_FinancialHardshipApplication

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | DataFormId | DataFormId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | cltId | cltId | int |  | Yes |
| 6 | CreatedOn | CreatedOn | datetime |  | Yes |
| 7 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 8 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 9 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 10 | IsDeleted | IsDeleted | bit |  | Yes |
| 11 | IsIdentification | IsIdentification | bit |  | Yes |
| 12 | IsIncome | IsIncome | bit |  | Yes |
| 13 | txtIncomeIdentification | txtIncomeIdentification | nvarchar |  | Yes |
| 14 | FHAPatientSignature | FHAPatientSignature | nvarchar |  | Yes |
| 15 | FHAPatientSignatureDate | FHAPatientSignatureDate | datetime |  | Yes |
| 16 | FHAPatientSignatureBy | FHAPatientSignatureBy | nvarchar |  | Yes |
| 17 | txtAnnualHouseholdIncome | txtAnnualHouseholdIncome | nvarchar |  | Yes |
| 18 | EmergencyName | EmergencyName | nvarchar |  | Yes |
| 19 | EmergencyRelation | EmergencyRelation | nvarchar |  | Yes |
| 20 | EmergencyPhone | EmergencyPhone | nvarchar |  | Yes |
| 21 | txtAUIGross1 | txtAUIGross1 | float |  | Yes |
| 22 | txtAUIGross2 | txtAUIGross2 | float |  | Yes |
| 23 | txtAUIGross3 | txtAUIGross3 | float |  | Yes |
| 24 | txtAUISocial1 | txtAUISocial1 | float |  | Yes |
| 25 | txtAUISocial2 | txtAUISocial2 | float |  | Yes |
| 26 | txtAUISocial3 | txtAUISocial3 | float |  | Yes |
| 27 | txtAUIAlimony1 | txtAUIAlimony1 | float |  | Yes |
| 28 | txtAUIAlimony2 | txtAUIAlimony2 | float |  | Yes |
| 29 | txtAUIAlimony3 | txtAUIAlimony3 | float |  | Yes |
| 30 | txtAUISelf1 | txtAUISelf1 | float |  | Yes |
| 31 | txtAUISelf2 | txtAUISelf2 | float |  | Yes |
| 32 | txtAUISelf3 | txtAUISelf3 | float |  | Yes |
| 33 | txtAUIRent1 | txtAUIRent1 | float |  | Yes |
| 34 | txtAUIRent2 | txtAUIRent2 | float |  | Yes |
| 35 | txtAUIRent3 | txtAUIRent3 | float |  | Yes |
| 36 | Version | Version | nvarchar |  | Yes |
| 37 | IscurrentlyUninsured | IscurrentlyUninsured | bit |  | Yes |
| 38 | StatusofApplication | StatusofApplication | nvarchar |  | Yes |
| 39 | Facts | Facts | nvarchar |  | Yes |
| 40 | PayClassApproved | PayClassApproved | nvarchar |  | Yes |
| 41 | ApprovedBy | ApprovedBy | nvarchar |  | Yes |
| 42 | EffectiveDate | EffectiveDate | datetime |  | Yes |
| 43 | ExpirationDate | ExpirationDate | datetime |  | Yes |

---

### StepKey 65 --- pats.tbl_PACounselorReview

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | EarlyIntervention | EarlyIntervention | bit |  | Yes |
| 5 | OutpatientTreatment | OutpatientTreatment | bit |  | Yes |
| 6 | IntensiveOutpatient | IntensiveOutpatient | bit |  | Yes |
| 7 | PartialHospitalization | PartialHospitalization | bit |  | Yes |
| 8 | ResidentialInpatient | ResidentialInpatient | bit |  | Yes |
| 9 | MedManagedIntensiveInpatient | MedManagedIntensiveInpatient | bit |  | Yes |
| 10 | OTS | OTS | bit |  | Yes |
| 11 | OBOT | OBOT | bit |  | Yes |
| 12 | OTP | OTP | bit |  | Yes |
| 13 | OBAT | OBAT | bit |  | Yes |
| 14 | WithdrawalManagement | WithdrawalManagement | bit |  | Yes |
| 15 | CopePhase1 | CopePhase1 | bit |  | Yes |
| 16 | CopePhase2 | CopePhase2 | bit |  | Yes |
| 17 | CopePhase3 | CopePhase3 | bit |  | Yes |
| 18 | Induction | Induction | bit |  | Yes |
| 19 | Stabilization | Stabilization | bit |  | Yes |
| 20 | Maintenance | Maintenance | bit |  | Yes |
| 21 | DateCompleted | DateCompleted | datetime |  | Yes |
| 22 | UseScore | UseScore | int |  | Yes |
| 23 | RiskScore | RiskScore | int |  | Yes |
| 24 | ProtectiveScore | ProtectiveScore | int |  | Yes |
| 25 | ClinicalSummary | ClinicalSummary | nvarchar |  | Yes |
| 26 | PatientSignature | PatientSignature | nvarchar |  | Yes |
| 27 | PatientSignatureBy | PatientSignatureBy | nvarchar |  | Yes |
| 28 | PatientSignatureDate | PatientSignatureDate | datetime |  | Yes |
| 29 | CounselorSignature | CounselorSignature | nvarchar |  | Yes |
| 30 | CounselorSignatureBy | CounselorSignatureBy | nvarchar |  | Yes |
| 31 | CounselorSignatureDate | CounselorSignatureDate | datetime |  | Yes |
| 32 | SupervisorSignature | SupervisorSignature | nvarchar |  | Yes |
| 33 | SupervisorSignatureBy | SupervisorSignatureBy | nvarchar |  | Yes |
| 34 | SupervisorSignatureDate | SupervisorSignatureDate | datetime |  | Yes |

---

### StepKey 66 --- pats.tbl_PADimension1

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | LastUDS | LastUDS | nvarchar |  | Yes |
| 5 | UDSResult | UDSResult | nvarchar |  | Yes |
| 6 | IllegalSubstances | IllegalSubstances | int |  | Yes |
| 7 | IllegalSubstancesBox | IllegalSubstancesBox | nvarchar |  | Yes |
| 8 | Overdose | Overdose | int |  | Yes |
| 9 | OverdoseBox | OverdoseBox | nvarchar |  | Yes |
| 10 | NarcanAvailable | NarcanAvailable | int |  | Yes |
| 11 | Cravings | Cravings | int |  | Yes |
| 12 | CravingRating | CravingRating | int |  | Yes |
| 13 | Dimension1ASAMRating | Dimension1ASAMRating | int |  | Yes |
| 14 | UAEval | UAEval | varchar |  | Yes |

---

### StepKey 67 --- pats.tbl_PADimension2

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | PhysicalHealthChange | PhysicalHealthChange | int |  | Yes |
| 5 | Called911 | Called911 | int |  | Yes |
| 6 | Called911Box | Called911Box | nvarchar |  | Yes |
| 7 | WorseningMedicalCondition | WorseningMedicalCondition | int |  | Yes |
| 8 | WorseningMedicalConditionBox | WorseningMedicalConditionBox | nvarchar |  | Yes |
| 9 | PrimaryCareProvider | PrimaryCareProvider | int |  | Yes |
| 10 | PrimaryCareProviderBox | PrimaryCareProviderBox | nvarchar |  | Yes |
| 11 | UnprotectedSex | UnprotectedSex | bit |  | Yes |
| 12 | DrugInjection | DrugInjection | bit |  | Yes |
| 13 | SharingDrug | SharingDrug | bit |  | Yes |
| 14 | HIVHepatits | HIVHepatits | int |  | Yes |
| 15 | HIVHepatitisBox | HIVHepatitisBox | nvarchar |  | Yes |
| 16 | TobaccoNicotine | TobaccoNicotine | int |  | Yes |
| 17 | TobaccoNicotineFrequency | TobaccoNicotineFrequency | nvarchar |  | Yes |
| 18 | DiscontinueTobaccoNicotine | DiscontinueTobaccoNicotine | int |  | Yes |
| 19 | Dimension2ASAMRating | Dimension2ASAMRating | int |  | Yes |

---

### StepKey 68 --- pats.tbl_PADimension3

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | MentalHealthChange | MentalHealthChange | int |  | Yes |
| 5 | MentalHealthHospitalized | MentalHealthHospitalized | int |  | Yes |
| 6 | MentalHealthHospitalizedbox | MentalHealthHospitalizedbox | nvarchar |  | Yes |
| 7 | WorseningMentalHealth | WorseningMentalHealth | int |  | Yes |
| 8 | WorseningMentalHealthBox | WorseningMentalHealthBox | nvarchar |  | Yes |
| 9 | Agitation | Agitation | bit |  | Yes |
| 10 | DecreasedPleasure | DecreasedPleasure | bit |  | Yes |
| 11 | Anxiety | Anxiety | bit |  | Yes |
| 12 | LackofInterest | LackofInterest | bit |  | Yes |
| 13 | Confusion | Confusion | bit |  | Yes |
| 14 | PanicAttacks | PanicAttacks | bit |  | Yes |
| 15 | BrainFog | BrainFog | bit |  | Yes |
| 16 | Numbness | Numbness | bit |  | Yes |
| 17 | Insomnia | Insomnia | bit |  | Yes |
| 18 | TroubleFallingAsleep | TroubleFallingAsleep | bit |  | Yes |
| 19 | TroubleWakingUp | TroubleWakingUp | bit |  | Yes |
| 20 | Headaches | Headaches | bit |  | Yes |
| 21 | StomachIssues | StomachIssues | bit |  | Yes |
| 22 | Fatigue | Fatigue | bit |  | Yes |
| 23 | Restlessness | Restlessness | bit |  | Yes |
| 24 | Tearfulness | Tearfulness | bit |  | Yes |
| 25 | IncreasedAppetite | IncreasedAppetite | bit |  | Yes |
| 26 | DecreasedAppetite | DecreasedAppetite | bit |  | Yes |
| 27 | Feelingempty | Feelingempty | bit |  | Yes |
| 28 | Irritability | Irritability | bit |  | Yes |
| 29 | Anger | Anger | bit |  | Yes |
| 30 | GuiltShame | GuiltShame | bit |  | Yes |
| 31 | MoodSwings | MoodSwings | bit |  | Yes |
| 32 | DecreasedSelfControl | DecreasedSelfControl | bit |  | Yes |
| 33 | Nightmares | Nightmares | bit |  | Yes |
| 34 | DecreasedEnergy | DecreasedEnergy | bit |  | Yes |
| 35 | IncreasedEnergy | IncreasedEnergy | bit |  | Yes |
| 36 | LackofFocus | LackofFocus | bit |  | Yes |
| 37 | Hallucinations | Hallucinations | bit |  | Yes |
| 38 | Isolation | Isolation | bit |  | Yes |
| 39 | ObsessiveWorryingThoughts | ObsessiveWorryingThoughts | bit |  | Yes |
| 40 | LackofMotivation | LackofMotivation | bit |  | Yes |
| 41 | Forgetfulness | Forgetfulness | bit |  | Yes |
| 42 | Nervousness | Nervousness | bit |  | Yes |
| 43 | PersistentSadness | PersistentSadness | bit |  | Yes |
| 44 | DisorganizedConfusedThoughts | DisorganizedConfusedThoughts | bit |  | Yes |
| 45 | OtherMentalSymptoms | OtherMentalSymptoms | bit |  | Yes |
| 46 | OtherMentalSymptomsBox | OtherMentalSymptomsBox | nvarchar |  | Yes |
| 47 | WishedDead | WishedDead | int |  | Yes |
| 48 | KillingYourself | KillingYourself | nvarchar |  | Yes |
| 49 | Dimension3ASAMRating | Dimension3ASAMRating | int |  | Yes |

---

### StepKey 69 --- pats.tbl_PADimension4

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | MotivationforChange | MotivationforChange | nvarchar |  | Yes |
| 5 | TreatmentSatisfaction | TreatmentSatisfaction | int |  | Yes |
| 6 | TreatmentSatisfactionBox | TreatmentSatisfactionBox | nvarchar |  | Yes |
| 7 | EventuallyDiscontinuing | EventuallyDiscontinuing | int |  | Yes |
| 8 | Discontinuing3to6Months | Discontinuing3to6Months | int |  | Yes |
| 9 | Strengths | Strengths | nvarchar |  | Yes |
| 10 | Needs | Needs | nvarchar |  | Yes |
| 11 | Abilities | Abilities | nvarchar |  | Yes |
| 12 | PreferedforTreatment | PreferedforTreatment | nvarchar |  | Yes |
| 13 | Dimension4ASAMRating | Dimension4ASAMRating | int |  | Yes |

---

### StepKey 70 --- pats.tbl_PADimension5

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | Triggers | Triggers | nvarchar |  | Yes |
| 5 | CopingStrategies | CopingStrategies | nvarchar |  | Yes |
| 6 | ContinueUsing | ContinueUsing | int |  | Yes |
| 7 | ContinueUsingBox | ContinueUsingBox | nvarchar |  | Yes |
| 8 | EmploymentStatus | EmploymentStatus | int |  | Yes |
| 9 | EmploymentStatusOther | EmploymentStatusOther | nvarchar |  | Yes |
| 10 | PartFullTime | PartFullTime | int |  | Yes |
| 11 | Arrested | Arrested | int |  | Yes |
| 12 | ChangeinLegalStatus | ChangeinLegalStatus | int |  | Yes |
| 13 | ChangeinLegalStatusBox | ChangeinLegalStatusBox | nvarchar |  | Yes |
| 14 | FinancialTrouble | FinancialTrouble | int |  | Yes |
| 15 | Dimension5ASAMRating | Dimension5ASAMRating | int |  | Yes |

---

### StepKey 71 --- pats.tbl_PADimension6

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | PeriodicReassessmentId | PeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | CurrentlyLivingOther | CurrentlyLivingOther | nvarchar |  | Yes |
| 5 | EnvironmentStability | EnvironmentStability | int |  | Yes |
| 6 | EnvironmentStabilityBox | EnvironmentStabilityBox | nvarchar |  | Yes |
| 7 | SafefromExploitation | SafefromExploitation | int |  | Yes |
| 8 | SafefromExploitationBox | SafefromExploitationBox | nvarchar |  | Yes |
| 9 | Threats | Threats | int |  | Yes |
| 10 | ThreatsBox | ThreatsBox | nvarchar |  | Yes |
| 11 | Children | Children | int |  | Yes |
| 12 | ChildrenAge | ChildrenAge | int |  | Yes |
| 13 | ChildrenAgeBox | ChildrenAgeBox | nvarchar |  | Yes |
| 14 | ChildrenLegalCustody | ChildrenLegalCustody | int |  | Yes |
| 15 | ChildFamilyServicesOpenCases | ChildFamilyServicesOpenCases | int |  | Yes |
| 16 | FriendsFamilySupport | FriendsFamilySupport | int |  | Yes |
| 17 | EnoughMoney | EnoughMoney | int |  | Yes |
| 18 | FamilyFriendsinRecovery | FamilyFriendsinRecovery | int |  | Yes |
| 19 | CurrentlyConnectedSupport | CurrentlyConnectedSupport | int |  | Yes |
| 20 | CurrentlyConnectedSupportBox | CurrentlyConnectedSupportBox | nvarchar |  | Yes |
| 21 | Barriers | Barriers | int |  | Yes |
| 22 | BarriersBox | BarriersBox | nvarchar |  | Yes |
| 23 | Dimension6ASAMRating | Dimension6ASAMRating | int |  | Yes |
| 24 | LivesAlone | LivesAlone | bit |  | Yes |
| 25 | HouseApartment | HouseApartment | bit |  | Yes |
| 26 | LiveKids | LiveKids | bit |  | Yes |
| 27 | Shelter | Shelter | bit |  | Yes |
| 28 | LivesPartnerSpouse | LivesPartnerSpouse | bit |  | Yes |
| 29 | SoberLivingHome | SoberLivingHome | bit |  | Yes |
| 30 | LivesFamily | LivesFamily | bit |  | Yes |
| 31 | Unhoused | Unhoused | bit |  | Yes |
| 32 | LivesFriends | LivesFriends | bit |  | Yes |
| 33 | Other | Other | bit |  | Yes |

---

### StepKey 72 --- pats.tbl_AppointmentAttend

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | aaID | aaID | int | PK2 | Yes |
| 3 | aaaptID | aaaptID | int |  | Yes |
| 4 | aacltid | aacltid | int |  | Yes |
| 5 | aaDTENROLLED | aaDTENROLLED | date |  | Yes |
| 6 | aaDTREMOVED | aaDTREMOVED | date |  | Yes |

---

### StepKey 73 --- pats.tbl_PA

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | char | PK1 | No |
| 2 | Id | Id | float | PK2 | Yes |
| 3 | DataFormId | DataFormId | float |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | float |  | Yes |
| 5 | ClientId | ClientId | float |  | Yes |
| 6 | Date | Date | date |  | Yes |
| 7 | CurrentPathway | CurrentPathway | char |  | Yes |
| 8 | CurrentPathwayPhase | CurrentPathwayPhase | varchar |  | Yes |
| 9 | CompletedAt | CompletedAt | float |  | Yes |
| 10 | CompletedAtOthers | CompletedAtOthers | varchar |  | Yes |
| 11 | IsDeleted | IsDeleted | float |  | Yes |
| 12 | CreatedBy | CreatedBy | varchar |  | Yes |
| 13 | CreatedOn | CreatedOn | datetime |  | Yes |
| 14 | ModifiedBy | ModifiedBy | varchar |  | Yes |
| 15 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 16 | Version | Version | char |  | Yes |

---

### StepKey 74 --- pats.tbl_MNComprehensiveAssessment

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | DataFormId | DataFormId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | TodayDate | TodayDate | datetime |  | Yes |
| 7 | ReferradBy | ReferradBy | int |  | Yes |
| 8 | ReferradByOther | ReferradByOther | nvarchar |  | Yes |
| 9 | ReferralReason | ReferralReason | int |  | Yes |
| 10 | ReferralReasonOther | ReferralReasonOther | nvarchar |  | Yes |
| 11 | InsuranceId | InsuranceId | nvarchar |  | Yes |
| 12 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 13 | CreatedOn | CreatedOn | datetime |  | Yes |
| 14 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 15 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 16 | Version | Version | nvarchar |  | Yes |
| 17 | IsDeleted | IsDeleted | bit |  | Yes |

---

### StepKey 75 --- pats.tbl_MNComprehensiveAssessmentLevelOfCare

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | MNComprehensiveAssessmentFormId | MNComprehensiveAssessmentFormId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | SymptomsUrgentlyAddressed | SymptomsUrgentlyAddressed | int |  | Yes |
| 5 | SymptomsUrgentlyAddressedExplain | SymptomsUrgentlyAddressedExplain | nvarchar |  | Yes |
| 6 | RisksofOpioid | RisksofOpioid | bit |  | Yes |
| 7 | TreatmentOptions | TreatmentOptions | bit |  | Yes |
| 8 | RisksofrecognitionOpioidOverdose | RisksofrecognitionOpioidOverdose | bit |  | Yes |
| 9 | AvailabilityAdministration | AvailabilityAdministration | bit |  | Yes |
| 10 | Other | Other | bit |  | Yes |
| 11 | OtherTxt | OtherTxt | nvarchar |  | Yes |
| 12 | LevelofCareRecommendation1 | LevelofCareRecommendation1 | bit |  | Yes |
| 13 | LevelofCareRecommendation21 | LevelofCareRecommendation21 | bit |  | Yes |
| 14 | LevelofCareRecommendation31 | LevelofCareRecommendation31 | bit |  | Yes |
| 15 | LevelofCareRecommendation33 | LevelofCareRecommendation33 | bit |  | Yes |
| 16 | LevelofCareRecommendation35 | LevelofCareRecommendation35 | bit |  | Yes |
| 17 | LevelofCareRecommendation37 | LevelofCareRecommendation37 | bit |  | Yes |
| 18 | LevelofCareRecommendation4 | LevelofCareRecommendation4 | bit |  | Yes |
| 19 | OpioidTreatmentServices | OpioidTreatmentServices | int |  | Yes |
| 20 | WithdrawalManagement | WithdrawalManagement | int |  | Yes |
| 21 | ASAMRecommendation | ASAMRecommendation | int |  | Yes |
| 22 | NALOC | NALOC | bit |  | Yes |
| 23 | LOCNotAvailable | LOCNotAvailable | bit |  | Yes |
| 24 | ClinicianJudgment | ClinicianJudgment | bit |  | Yes |
| 25 | Patientpreference | Patientpreference | bit |  | Yes |
| 26 | PatientWaitingForLOC | PatientWaitingForLOC | bit |  | Yes |
| 27 | RecommendedLOCAvailable | RecommendedLOCAvailable | bit |  | Yes |
| 28 | Geographicaccessibility | Geographicaccessibility | bit |  | Yes |
| 29 | Familycaregiverresponsibilities | Familycaregiverresponsibilities | bit |  | Yes |
| 30 | EmploymentResponsibilities | EmploymentResponsibilities | bit |  | Yes |
| 31 | Courttreatmentrequirements | Courttreatmentrequirements | bit |  | Yes |
| 32 | Lackofphysicalaccess | Lackofphysicalaccess | bit |  | Yes |
| 33 | Languageaccessibility | Languageaccessibility | bit |  | Yes |
| 34 | LOCIsAvailable | LOCIsAvailable | bit |  | Yes |
| 35 | Patientisineligible | Patientisineligible | bit |  | Yes |
| 36 | AdditionalComments | AdditionalComments | nvarchar |  | Yes |
| 37 | LOCOther | LOCOther | bit |  | Yes |
| 38 | LOCIsAvailableReason | LOCIsAvailableReason | nvarchar |  | Yes |
| 39 | PatientisineligibleReason | PatientisineligibleReason | nvarchar |  | Yes |
| 40 | OtherReason | OtherReason | nvarchar |  | Yes |
| 41 | LevelofCareRecommendation25 | LevelofCareRecommendation25 | bit |  | Yes |

---

### StepKey 76 --- pats.tbl_VAComprehensiveAssessment

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | DataFormId | DataFormId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 7 | CreatedOn | CreatedOn | datetime |  | Yes |
| 8 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 9 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 10 | IsDeleted | IsDeleted | bit |  | Yes |

---

### StepKey 77 --- pats.tbl_vacomprehensiveassessmentsummary

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | VAComprehensiveAssessmentId | VAComprehensiveAssessmentId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | DDLRecommendation | DDLRecommendation | int |  | Yes |
| 6 | OpioidTreatmentServices | OpioidTreatmentServices | int |  | Yes |
| 7 | WithdrawalManagement | WithdrawalManagement | int |  | Yes |
| 8 | ClinicalSummary | ClinicalSummary | nvarchar |  | Yes |
| 9 | ASAMRecommendationForLevel | ASAMRecommendationForLevel | int |  | Yes |
| 10 | LevelOfCareAtVariance | LevelOfCareAtVariance | nvarchar |  | Yes |
| 11 | SummaryComments | SummaryComments | nvarchar |  | Yes |

---

### StepKey 78 --- pats.tbl_NewAdmissionassessment

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | DataFormId | DataFormId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | CreatedOn | CreatedOn | datetime |  | Yes |
| 7 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 8 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 9 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 10 | IsDeleted | IsDeleted | bit |  | Yes |
| 11 | Version | Version | nvarchar |  | Yes |

---

### StepKey 79 --- pats.tbl_NewAdmissionAssessmentASAMDimension6

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewAdmissionAssessmentFormId | NewAdmissionAssessmentFormId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | ReadinessQuestion1 | ReadinessQuestion1 | int |  | Yes |
| 5 | ReadinessQuestion2 | ReadinessQuestion2 | int |  | Yes |
| 6 | ReadinessQuestion3 | ReadinessQuestion3 | int |  | Yes |
| 7 | ReadinessQuestion4 | ReadinessQuestion4 | int |  | Yes |
| 8 | ReadinessQuestion5 | ReadinessQuestion5 | int |  | Yes |
| 9 | ReadinessQuestion6 | ReadinessQuestion6 | int |  | Yes |
| 10 | ReadinessQuestion7 | ReadinessQuestion7 | int |  | Yes |
| 11 | ReadinessQuestion8 | ReadinessQuestion8 | int |  | Yes |
| 12 | ReadinessQuestion9 | ReadinessQuestion9 | int |  | Yes |
| 13 | ReadinessQuestion10 | ReadinessQuestion10 | int |  | Yes |
| 14 | ReadinessQuestion11 | ReadinessQuestion11 | int |  | Yes |
| 15 | ReadinessQuestion12 | ReadinessQuestion12 | int |  | Yes |
| 16 | StageOfChange | StageOfChange | nvarchar |  | Yes |
| 17 | AdditionalComments | AdditionalComments | nvarchar |  | Yes |
| 18 | TreatmentPreferences | TreatmentPreferences | nvarchar |  | Yes |
| 19 | ReasonNotWillingToAttend | ReasonNotWillingToAttend | nvarchar |  | Yes |
| 20 | ReasonWillNotAdmitReason | ReasonWillNotAdmitReason | nvarchar |  | Yes |
| 21 | ReasonPatientIneligibleReason | ReasonPatientIneligibleReason | nvarchar |  | Yes |
| 22 | ReasonOtherReason | ReasonOtherReason | nvarchar |  | Yes |
| 23 | ClinicalSummary | ClinicalSummary | nvarchar |  | Yes |
| 24 | HasTreatmentPreferences | HasTreatmentPreferences | bit |  | Yes |
| 25 | WillingToAttendRecommendedCare | WillingToAttendRecommendedCare | bit |  | Yes |
| 26 | TransportationChallenges | TransportationChallenges | bit |  | Yes |
| 27 | FoodHousingInsecurity | FoodHousingInsecurity | bit |  | Yes |
| 28 | ChildcareResponsibilities | ChildcareResponsibilities | bit |  | Yes |
| 29 | FinancialInsecurity | FinancialInsecurity | bit |  | Yes |
| 30 | LackEmploymentOpportunities | LackEmploymentOpportunities | bit |  | Yes |
| 31 | LackJobSecurity | LackJobSecurity | bit |  | Yes |
| 32 | LackHealthcareCoverage | LackHealthcareCoverage | bit |  | Yes |
| 33 | LackSocialSupports | LackSocialSupports | bit |  | Yes |
| 34 | LanguageBarriers | LanguageBarriers | bit |  | Yes |
| 35 | Level1 | Level1 | bit |  | Yes |
| 36 | Level1_5 | Level1_5 | bit |  | Yes |
| 37 | Level1_7 | Level1_7 | bit |  | Yes |
| 38 | Level2_1 | Level2_1 | bit |  | Yes |
| 39 | Level2_5 | Level2_5 | bit |  | Yes |
| 40 | Level2_7 | Level2_7 | bit |  | Yes |
| 41 | Level3_1 | Level3_1 | bit |  | Yes |
| 42 | Level3_5 | Level3_5 | bit |  | Yes |
| 43 | Level3_7 | Level3_7 | bit |  | Yes |
| 44 | NonBIO | NonBIO | bit |  | Yes |
| 45 | BIO | BIO | bit |  | Yes |
| 46 | Level4 | Level4 | bit |  | Yes |
| 47 | COE | COE | bit |  | Yes |
| 48 | ReasonNotAligned | ReasonNotAligned | bit |  | Yes |
| 49 | ReasonNotAvailable | ReasonNotAvailable | bit |  | Yes |
| 50 | ReasonClinicianJudgment | ReasonClinicianJudgment | bit |  | Yes |
| 51 | ReasonPatientPreference | ReasonPatientPreference | bit |  | Yes |
| 52 | ReasonOnWaitingList | ReasonOnWaitingList | bit |  | Yes |
| 53 | ReasonLacksPayment | ReasonLacksPayment | bit |  | Yes |
| 54 | ReasonGeographicAccess | ReasonGeographicAccess | bit |  | Yes |
| 55 | ReasonCaregiverResponsibilities | ReasonCaregiverResponsibilities | bit |  | Yes |
| 56 | ReasonEmploymentResponsibilities | ReasonEmploymentResponsibilities | bit |  | Yes |
| 57 | ReasonCourtRequirements | ReasonCourtRequirements | bit |  | Yes |
| 58 | ReasonTransportationChallenges | ReasonTransportationChallenges | bit |  | Yes |
| 59 | ReasonLanguageAccessibility | ReasonLanguageAccessibility | bit |  | Yes |
| 60 | ReasonWillNotAdmit | ReasonWillNotAdmit | bit |  | Yes |
| 61 | ReasonPatientIneligible | ReasonPatientIneligible | bit |  | Yes |
| 62 | ReasonOther | ReasonOther | bit |  | Yes |
| 63 | PatientSignature | PatientSignature | nvarchar |  | Yes |
| 64 | PatientSignatureBy | PatientSignatureBy | nvarchar |  | Yes |
| 65 | SupervisorSignature | SupervisorSignature | nvarchar |  | Yes |
| 66 | SupervisorSignatureBy | SupervisorSignatureBy | nvarchar |  | Yes |
| 67 | CounselorSignature | CounselorSignature | nvarchar |  | Yes |
| 68 | CounselorSignatureBy | CounselorSignatureBy | nvarchar |  | Yes |
| 69 | ProviderSignature | ProviderSignature | nvarchar |  | Yes |
| 70 | ProviderSignatureBy | ProviderSignatureBy | nvarchar |  | Yes |
| 71 | SuperviosorSignNA | SuperviosorSignNA | bit |  | Yes |
| 72 | PatientSignatureDate | PatientSignatureDate | datetime |  | Yes |
| 73 | SupervisorSignatureDate | SupervisorSignatureDate | datetime |  | Yes |
| 74 | CounselorSignatureDate | CounselorSignatureDate | datetime |  | Yes |
| 75 | ProviderSignatureDate | ProviderSignatureDate | datetime |  | Yes |

---

### StepKey 80 --- ctrl.tbl_DroDownListItems

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | DropDownListItem | DropDownListItem | varchar |  | Yes |
| 4 | DropDownListId | DropDownListId | int |  | Yes |
| 5 | ddapcode | ddapcode | char |  | Yes |

---

### StepKey 81 --- pats.tbl_BAMForm

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | ClientId | ClientId | int |  | Yes |
| 5 | DataFormId | DataFormId | int |  | Yes |
| 6 | BAMDate | BAMDate | datetime |  | Yes |
| 7 | InterviewerID | InterviewerID | nvarchar |  | Yes |
| 8 | ClinicianInterview | ClinicianInterview | bit |  | Yes |
| 9 | SelfReport | SelfReport | bit |  | Yes |
| 10 | Phone | Phone | bit |  | Yes |
| 11 | TimeStarted | TimeStarted | datetime |  | Yes |
| 12 | InstructionsQ1 | InstructionsQ1 | int |  | Yes |
| 13 | InstructionsQ1Txt | InstructionsQ1Txt | nvarchar |  | Yes |
| 14 | InstructionsQ2 | InstructionsQ2 | int |  | Yes |
| 15 | InstructionsQ2Txt | InstructionsQ2Txt | nvarchar |  | Yes |
| 16 | InstructionsQ3 | InstructionsQ3 | int |  | Yes |
| 17 | InstructionsQ3Txt | InstructionsQ3Txt | nvarchar |  | Yes |
| 18 | InstructionsQ4 | InstructionsQ4 | int |  | Yes |
| 19 | InstructionsQ4Txt | InstructionsQ4Txt | nvarchar |  | Yes |
| 20 | InstructionsQ5 | InstructionsQ5 | int |  | Yes |
| 21 | InstructionsQ5Txt | InstructionsQ5Txt | nvarchar |  | Yes |
| 22 | InstructionsQ6 | InstructionsQ6 | int |  | Yes |
| 23 | InstructionsQ6Txt | InstructionsQ6Txt | nvarchar |  | Yes |
| 24 | InstructionsQ7A | InstructionsQ7A | int |  | Yes |
| 25 | InstructionsQ7B | InstructionsQ7B | int |  | Yes |
| 26 | InstructionsQ7C | InstructionsQ7C | int |  | Yes |
| 27 | InstructionsQ7D | InstructionsQ7D | int |  | Yes |
| 28 | InstructionsQ7E | InstructionsQ7E | int |  | Yes |
| 29 | InstructionsQ7F | InstructionsQ7F | int |  | Yes |
| 30 | InstructionsQ7G | InstructionsQ7G | int |  | Yes |
| 31 | InstructionsQ8 | InstructionsQ8 | int |  | Yes |
| 32 | InstructionsQ8Txt | InstructionsQ8Txt | nvarchar |  | Yes |
| 33 | InstructionsQ9 | InstructionsQ9 | int |  | Yes |
| 34 | InstructionsQ9Txt | InstructionsQ9Txt | nvarchar |  | Yes |
| 35 | InstructionsQ10 | InstructionsQ10 | int |  | Yes |
| 36 | InstructionsQ10Txt | InstructionsQ10Txt | nvarchar |  | Yes |
| 37 | InstructionsQ11 | InstructionsQ11 | int |  | Yes |
| 38 | InstructionsQ11Txt | InstructionsQ11Txt | nvarchar |  | Yes |
| 39 | InstructionsQ12 | InstructionsQ12 | int |  | Yes |
| 40 | InstructionsQ12Txt | InstructionsQ12Txt | nvarchar |  | Yes |
| 41 | InstructionsQ13 | InstructionsQ13 | int |  | Yes |
| 42 | InstructionsQ13Txt | InstructionsQ13Txt | nvarchar |  | Yes |
| 43 | InstructionsQ14 | InstructionsQ14 | int |  | Yes |
| 44 | InstructionsQ14Txt | InstructionsQ14Txt | nvarchar |  | Yes |
| 45 | InstructionsQ15 | InstructionsQ15 | int |  | Yes |
| 46 | InstructionsQ15Txt | InstructionsQ15Txt | nvarchar |  | Yes |
| 47 | InstructionsQ16 | InstructionsQ16 | int |  | Yes |
| 48 | InstructionsQ16Txt | InstructionsQ16Txt | nvarchar |  | Yes |
| 49 | InstructionsQ17 | InstructionsQ17 | int |  | Yes |
| 50 | InstructionsQ17Txt | InstructionsQ17Txt | nvarchar |  | Yes |
| 51 | TimeFinished | TimeFinished | datetime |  | Yes |
| 52 | SubscaleScoreTxt1 | SubscaleScoreTxt1 | nvarchar |  | Yes |
| 53 | SubscaleScoreTxt2 | SubscaleScoreTxt2 | nvarchar |  | Yes |
| 54 | SubscaleScoreTxt3 | SubscaleScoreTxt3 | nvarchar |  | Yes |
| 55 | StaffSignature | StaffSignature | nvarchar |  | Yes |
| 56 | StaffSignatureBy | StaffSignatureBy | nvarchar |  | Yes |
| 57 | StaffSignatureDate | StaffSignatureDate | datetime |  | Yes |
| 58 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 59 | CreatedOn | CreatedOn | datetime |  | Yes |
| 60 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 61 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 62 | IsDeleted | IsDeleted | bit |  | Yes |
| 63 | Version | Version | nvarchar |  | Yes |

---

### StepKey 82 --- pats.tbl_BAMScore

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | ClientId | ClientId | int |  | Yes |
| 4 | tprID | tprID | int |  | Yes |
| 5 | Description | Description | varchar |  | Yes |
| 6 | Score | Score | varchar |  | Yes |

---

### StepKey 83 --- pats.tbl_TblDiag10

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | dgID | dgID | int | PK2 | Yes |
| 3 | dgCLTID | dgCLTID | int |  | Yes |
| 4 | dgDIAG | dgDIAG | nvarchar |  | Yes |
| 5 | dgDESC | dgDESC | ntext |  | No |
| 6 | dgDATE | dgDATE | datetime |  | Yes |
| 7 | dgSTAFF | dgSTAFF | nvarchar |  | Yes |
| 8 | dgdt | dgdt | datetime |  | Yes |
| 9 | dgPRIMARY | dgPRIMARY | bit |  | Yes |
| 10 | dgDIAG10 | dgDIAG10 | nvarchar |  | Yes |
| 11 | dgDIAG10Description | dgDIAG10Description | ntext |  | No |
| 12 | dgNote | dgNote | nvarchar |  | Yes |
| 13 | dgType | dgType | nvarchar |  | Yes |
| 14 | EnrollmentId | EnrollmentId | int |  | Yes |
| 15 | dgEndDate | dgEndDate | datetime |  | Yes |

---

### StepKey 84 --- pats.tbl_NewPeriodicReassessment

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | DataFormId | DataFormId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | Date | Date | datetime |  | Yes |
| 7 | CurrentPathway | CurrentPathway | nvarchar |  | Yes |
| 8 | CompletedAt | CompletedAt | int |  | Yes |
| 9 | CompletedAtOthers | CompletedAtOthers | nvarchar |  | Yes |
| 10 | IsDeleted | IsDeleted | bit |  | Yes |
| 11 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 12 | CreatedOn | CreatedOn | datetime |  | Yes |
| 13 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 14 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 15 | Version | Version | nvarchar |  | Yes |

---

### StepKey 85 --- pats.tbl_NewPeriodicReassessmentCounselorReview

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewPeriodicReassessmentId | NewPeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | Level1 | Level1 | bit |  | Yes |
| 5 | Level1_5 | Level1_5 | bit |  | Yes |
| 6 | Level1_7 | Level1_7 | bit |  | Yes |
| 7 | Level2_1 | Level2_1 | bit |  | Yes |
| 8 | Level2_5 | Level2_5 | bit |  | Yes |
| 9 | Level2_7 | Level2_7 | bit |  | Yes |
| 10 | Level3_1 | Level3_1 | bit |  | Yes |
| 11 | Level3_5 | Level3_5 | bit |  | Yes |
| 12 | Level3_7 | Level3_7 | bit |  | Yes |
| 13 | NonBIO | NonBIO | bit |  | Yes |
| 14 | BIO | BIO | bit |  | Yes |
| 15 | Level4 | Level4 | bit |  | Yes |
| 16 | COE | COE | bit |  | Yes |
| 17 | ReasonNotAligned | ReasonNotAligned | bit |  | Yes |
| 18 | ReasonNotAvailable | ReasonNotAvailable | bit |  | Yes |
| 19 | ReasonClinicianJudgment | ReasonClinicianJudgment | bit |  | Yes |
| 20 | ReasonPatientPreference | ReasonPatientPreference | bit |  | Yes |
| 21 | ReasonOnWaitingList | ReasonOnWaitingList | bit |  | Yes |
| 22 | ReasonLacksPayment | ReasonLacksPayment | bit |  | Yes |
| 23 | ReasonGeographicAccess | ReasonGeographicAccess | bit |  | Yes |
| 24 | ReasonCaregiverResponsibilities | ReasonCaregiverResponsibilities | bit |  | Yes |
| 25 | ReasonEmploymentResponsibilities | ReasonEmploymentResponsibilities | bit |  | Yes |
| 26 | ReasonCourtRequirements | ReasonCourtRequirements | bit |  | Yes |
| 27 | ReasonTransportationChallenges | ReasonTransportationChallenges | bit |  | Yes |
| 28 | ReasonLanguageAccessibility | ReasonLanguageAccessibility | bit |  | Yes |
| 29 | ReasonWillNotAdmit | ReasonWillNotAdmit | bit |  | Yes |
| 30 | ReasonPatientIneligible | ReasonPatientIneligible | bit |  | Yes |
| 31 | ReasonOther | ReasonOther | bit |  | Yes |
| 32 | ReasonWillNotAdmitReason | ReasonWillNotAdmitReason | nvarchar |  | Yes |
| 33 | ReasonPatientIneligibleReason | ReasonPatientIneligibleReason | nvarchar |  | Yes |
| 34 | ReasonOtherReason | ReasonOtherReason | nvarchar |  | Yes |
| 35 | CopePhase1 | CopePhase1 | bit |  | Yes |
| 36 | CopePhase2 | CopePhase2 | bit |  | Yes |
| 37 | CopePhase3 | CopePhase3 | bit |  | Yes |
| 38 | Induction | Induction | bit |  | Yes |
| 39 | Stabilization | Stabilization | bit |  | Yes |
| 40 | Maintenance | Maintenance | bit |  | Yes |
| 41 | DateCompleted | DateCompleted | datetime |  | Yes |
| 42 | UseScore | UseScore | int |  | Yes |
| 43 | RiskScore | RiskScore | int |  | Yes |
| 44 | ProtectiveScore | ProtectiveScore | int |  | Yes |
| 45 | ClinicalSummary | ClinicalSummary | nvarchar |  | Yes |
| 46 | PatientSignature | PatientSignature | nvarchar |  | Yes |
| 47 | PatientSignatureBy | PatientSignatureBy | nvarchar |  | Yes |
| 48 | PatientSignatureDate | PatientSignatureDate | datetime |  | Yes |
| 49 | CounselorSignature | CounselorSignature | nvarchar |  | Yes |
| 50 | CounselorSignatureBy | CounselorSignatureBy | nvarchar |  | Yes |
| 51 | CounselorSignatureDate | CounselorSignatureDate | datetime |  | Yes |
| 52 | ProviderSignature | ProviderSignature | nvarchar |  | Yes |
| 53 | ProviderSignatureBy | ProviderSignatureBy | nvarchar |  | Yes |
| 54 | ProviderSignatureDate | ProviderSignatureDate | datetime |  | Yes |
| 55 | SupervisorSignature | SupervisorSignature | nvarchar |  | Yes |
| 56 | SupervisorSignatureBy | SupervisorSignatureBy | nvarchar |  | Yes |
| 57 | SupervisorSignatureDate | SupervisorSignatureDate | datetime |  | Yes |
| 58 | RR | RR | bit |  | Yes |

---

### StepKey 86 --- pats.tbl_newperiodicreassessmentd2

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewPeriodicReassessmentId | NewPeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | PhysicalHealthChange | PhysicalHealthChange | int |  | Yes |
| 5 | Called911 | Called911 | int |  | Yes |
| 6 | Called911Box | Called911Box | nvarchar |  | Yes |
| 7 | WorseningMedicalCondition | WorseningMedicalCondition | int |  | Yes |
| 8 | WorseningMedicalConditionBox | WorseningMedicalConditionBox | nvarchar |  | Yes |
| 9 | PrimaryCareProvider | PrimaryCareProvider | int |  | Yes |
| 10 | PrimaryCareProviderBox | PrimaryCareProviderBox | nvarchar |  | Yes |
| 11 | CurrentlyPregnant | CurrentlyPregnant | int |  | Yes |
| 12 | CurrentlyReceivingPrenatalCare | CurrentlyReceivingPrenatalCare | int |  | Yes |
| 13 | CurrentlyPregnantROIBox | CurrentlyPregnantROIBox | nvarchar |  | Yes |
| 14 | UnprotectedSex | UnprotectedSex | bit |  | Yes |
| 15 | DrugInjection | DrugInjection | bit |  | Yes |
| 16 | SharingDrug | SharingDrug | bit |  | Yes |
| 17 | NoneOfTheAbove | NoneOfTheAbove | bit |  | Yes |
| 18 | HIVHepatits | HIVHepatits | int |  | Yes |
| 19 | HIVHepatitisBox | HIVHepatitisBox | nvarchar |  | Yes |
| 20 | TobaccoNicotine | TobaccoNicotine | int |  | Yes |
| 21 | TobaccoNicotineFrequency | TobaccoNicotineFrequency | nvarchar |  | Yes |
| 22 | DiscontinueTobaccoNicotine | DiscontinueTobaccoNicotine | int |  | Yes |
| 23 | PhysicalHealth | PhysicalHealth | int |  | Yes |
| 24 | PregnancyRelatedConcern | PregnancyRelatedConcern | int |  | Yes |

---

### StepKey 87 --- pats.tbl_newperiodicreassessmentd3

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewPeriodicReassessmentId | NewPeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | MentalHealthChange | MentalHealthChange | int |  | Yes |
| 5 | MentalHealthHospitalized | MentalHealthHospitalized | int |  | Yes |
| 6 | MentalHealthHospitalizedBox | MentalHealthHospitalizedBox | nvarchar |  | Yes |
| 7 | WorseningMentalHealth | WorseningMentalHealth | int |  | Yes |
| 8 | WorseningMentalHealthBox | WorseningMentalHealthBox | nvarchar |  | Yes |
| 9 | Agitation | Agitation | bit |  | Yes |
| 10 | DecreasedPleasure | DecreasedPleasure | bit |  | Yes |
| 11 | Anxiety | Anxiety | bit |  | Yes |
| 12 | LackofInterest | LackofInterest | bit |  | Yes |
| 13 | Confusion | Confusion | bit |  | Yes |
| 14 | PanicAttacks | PanicAttacks | bit |  | Yes |
| 15 | BrainFog | BrainFog | bit |  | Yes |
| 16 | Numbness | Numbness | bit |  | Yes |
| 17 | Insomnia | Insomnia | bit |  | Yes |
| 18 | TroubleFallingAsleep | TroubleFallingAsleep | bit |  | Yes |
| 19 | TroubleWakingUp | TroubleWakingUp | bit |  | Yes |
| 20 | Headaches | Headaches | bit |  | Yes |
| 21 | StomachIssues | StomachIssues | bit |  | Yes |
| 22 | Fatigue | Fatigue | bit |  | Yes |
| 23 | Restlessness | Restlessness | bit |  | Yes |
| 24 | Tearfulness | Tearfulness | bit |  | Yes |
| 25 | IncreasedAppetite | IncreasedAppetite | bit |  | Yes |
| 26 | DecreasedAppetite | DecreasedAppetite | bit |  | Yes |
| 27 | Feelingempty | Feelingempty | bit |  | Yes |
| 28 | Irritability | Irritability | bit |  | Yes |
| 29 | Anger | Anger | bit |  | Yes |
| 30 | GuiltShame | GuiltShame | bit |  | Yes |
| 31 | MoodSwings | MoodSwings | bit |  | Yes |
| 32 | DecreasedSelfControl | DecreasedSelfControl | bit |  | Yes |
| 33 | Nightmares | Nightmares | bit |  | Yes |
| 34 | DecreasedEnergy | DecreasedEnergy | bit |  | Yes |
| 35 | IncreasedEnergy | IncreasedEnergy | bit |  | Yes |
| 36 | LackofFocus | LackofFocus | bit |  | Yes |
| 37 | Hallucinations | Hallucinations | bit |  | Yes |
| 38 | Isolation | Isolation | bit |  | Yes |
| 39 | ObsessiveWorryingThoughts | ObsessiveWorryingThoughts | bit |  | Yes |
| 40 | LackofMotivation | LackofMotivation | bit |  | Yes |
| 41 | Forgetfulness | Forgetfulness | bit |  | Yes |
| 42 | Nervousness | Nervousness | bit |  | Yes |
| 43 | PersistentSadness | PersistentSadness | bit |  | Yes |
| 44 | DisorganizedConfusedThoughts | DisorganizedConfusedThoughts | bit |  | Yes |
| 45 | OtherMentalSymptoms | OtherMentalSymptoms | bit |  | Yes |
| 46 | OtherMentalSymptomsBox | OtherMentalSymptomsBox | nvarchar |  | Yes |
| 47 | WishedDead | WishedDead | int |  | Yes |
| 48 | KillingYourself | KillingYourself | nvarchar |  | Yes |
| 49 | ActivePsychiatricSymptoms | ActivePsychiatricSymptoms | int |  | Yes |
| 50 | PersistentDisability | PersistentDisability | int |  | Yes |

---

### StepKey 88 --- pats.tbl_newperiodicreassessmentd4

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewPeriodicReassessmentId | NewPeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | Triggers | Triggers | nvarchar |  | Yes |
| 5 | CopingStrategies | CopingStrategies | nvarchar |  | Yes |
| 6 | ContinueUsing | ContinueUsing | int |  | Yes |
| 7 | ContinueUsingBox | ContinueUsingBox | nvarchar |  | Yes |
| 8 | EmploymentStatus | EmploymentStatus | int |  | Yes |
| 9 | EmploymentStatusOther | EmploymentStatusOther | nvarchar |  | Yes |
| 10 | PartFullTime | PartFullTime | int |  | Yes |
| 11 | Arrested | Arrested | int |  | Yes |
| 12 | ChangeinLegalStatus | ChangeinLegalStatus | int |  | Yes |
| 13 | ChangeinLegalStatusBox | ChangeinLegalStatusBox | nvarchar |  | Yes |
| 14 | FinancialTrouble | FinancialTrouble | int |  | Yes |
| 15 | RiskySubstanceUse | RiskySubstanceUse | int |  | Yes |
| 16 | RiskySUDRelatedBehaviors | RiskySUDRelatedBehaviors | int |  | Yes |

---

### StepKey 89 --- pats.tbl_newperiodicreassessmentd5

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewPeriodicReassessmentId | NewPeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | Children | Children | int |  | Yes |
| 5 | ChildrenAge | ChildrenAge | int |  | Yes |
| 6 | ChildrenAgeBox | ChildrenAgeBox | nvarchar |  | Yes |
| 7 | ChildrenLegalCustody | ChildrenLegalCustody | int |  | Yes |
| 8 | ChildFamilyServicesOpenCases | ChildFamilyServicesOpenCases | int |  | Yes |
| 9 | FriendsFamilySupport | FriendsFamilySupport | int |  | Yes |
| 10 | EnoughMoney | EnoughMoney | int |  | Yes |
| 11 | FamilyFriendsinRecovery | FamilyFriendsinRecovery | int |  | Yes |
| 12 | CurrentlyConnectedSupport | CurrentlyConnectedSupport | int |  | Yes |
| 13 | CurrentlyConnectedSupportBox | CurrentlyConnectedSupportBox | nvarchar |  | Yes |
| 14 | Barriers | Barriers | int |  | Yes |
| 15 | BarriersBox | BarriersBox | nvarchar |  | Yes |
| 16 | LivingSituationToday | LivingSituationToday | int |  | Yes |
| 17 | Pests | Pests | bit |  | Yes |
| 18 | Mold | Mold | bit |  | Yes |
| 19 | LeadPaintPipes | LeadPaintPipes | bit |  | Yes |
| 20 | LackofHeat | LackofHeat | bit |  | Yes |
| 21 | OvenOrStove | OvenOrStove | bit |  | Yes |
| 22 | SmokeDetectors | SmokeDetectors | bit |  | Yes |
| 23 | Waterleaks | Waterleaks | bit |  | Yes |
| 24 | NoneOfAbove | NoneOfAbove | bit |  | Yes |
| 25 | LastassessmentWorried | LastassessmentWorried | int |  | Yes |
| 26 | LastassessmentFoodBought | LastassessmentFoodBought | int |  | Yes |
| 27 | LastassessmentSkipMedications | LastassessmentSkipMedications | int |  | Yes |
| 28 | HardToPay | HardToPay | int |  | Yes |
| 29 | FindingOrKeepingWork | FindingOrKeepingWork | int |  | Yes |
| 30 | SpeakLanguage | SpeakLanguage | int |  | Yes |
| 31 | SchoolTraining | SchoolTraining | int |  | Yes |
| 32 | LackOfTransportation | LackOfTransportation | int |  | Yes |
| 33 | AnyoneHurtYou | AnyoneHurtYou | int |  | Yes |
| 34 | InsultOrTalkDown | InsultOrTalkDown | int |  | Yes |
| 35 | ThreatenWithHarm | ThreatenWithHarm | int |  | Yes |
| 36 | ScreamOrCurseAtYou | ScreamOrCurseAtYou | int |  | Yes |
| 37 | EffectivelyinCurrentEnvironment | EffectivelyinCurrentEnvironment | int |  | Yes |
| 38 | SafetyCurrentEnvironment | SafetyCurrentEnvironment | int |  | Yes |
| 39 | SupportCurrentEnvironment | SupportCurrentEnvironment | int |  | Yes |
| 40 | Level1 | Level1 | bit |  | Yes |
| 41 | Level1_5 | Level1_5 | bit |  | Yes |
| 42 | Level1_7 | Level1_7 | bit |  | Yes |
| 43 | Level2_1 | Level2_1 | bit |  | Yes |
| 44 | Level2_5 | Level2_5 | bit |  | Yes |
| 45 | Level2_7 | Level2_7 | bit |  | Yes |
| 46 | Level3_1 | Level3_1 | bit |  | Yes |
| 47 | Level3_5 | Level3_5 | bit |  | Yes |
| 48 | Level3_7 | Level3_7 | bit |  | Yes |
| 49 | NonBIO | NonBIO | bit |  | Yes |
| 50 | BIO | BIO | bit |  | Yes |
| 51 | Level4 | Level4 | bit |  | Yes |
| 52 | COE | COE | bit |  | Yes |
| 53 | RR | RR | bit |  | Yes |

---

### StepKey 90 --- pats.tbl_newperiodicreassessmentd6

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | NewPeriodicReassessmentId | NewPeriodicReassessmentId | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | Motivationformakingorsustainingchanges | Motivationformakingorsustainingchanges | nvarchar |  | Yes |
| 5 | Satisfiedyourprogress | Satisfiedyourprogress | int |  | Yes |
| 6 | SatisfiedyourprogressExplain | SatisfiedyourprogressExplain | nvarchar |  | Yes |
| 7 | Iseventuallydiscontinuing | Iseventuallydiscontinuing | int |  | Yes |
| 8 | Planondiscontinuing | Planondiscontinuing | int |  | Yes |
| 9 | WhatStrengthsareusing | WhatStrengthsareusing | nvarchar |  | Yes |
| 10 | WhatNeedsdoyouhave | WhatNeedsdoyouhave | nvarchar |  | Yes |
| 11 | ListanyAbilities | ListanyAbilities | nvarchar |  | Yes |
| 12 | Haveyoulearnedprefer | Haveyoulearnedprefer | nvarchar |  | Yes |
| 13 | HasTreatmentPreferences | HasTreatmentPreferences | int |  | Yes |
| 14 | TreatmentPreferences | TreatmentPreferences | nvarchar |  | Yes |
| 15 | WillingToAttendRecommendedCare | WillingToAttendRecommendedCare | int |  | Yes |
| 16 | NotWillingReason | NotWillingReason | nvarchar |  | Yes |
| 17 | TransportationChallenges | TransportationChallenges | bit |  | Yes |
| 18 | FoodHousingInsecurity | FoodHousingInsecurity | bit |  | Yes |
| 19 | ChildcareResponsibilities | ChildcareResponsibilities | bit |  | Yes |
| 20 | FinancialInsecurity | FinancialInsecurity | bit |  | Yes |
| 21 | LackEducationEmployment | LackEducationEmployment | bit |  | Yes |
| 22 | LackJobSecurity | LackJobSecurity | bit |  | Yes |
| 23 | LackHealthcareCoverage | LackHealthcareCoverage | bit |  | Yes |
| 24 | LackSocialSupports | LackSocialSupports | bit |  | Yes |
| 25 | LanguageBarriers | LanguageBarriers | bit |  | Yes |

---

### StepKey 94 --- pats.tbl_consenttomarketing

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | DataFormId | DataFormId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | Text | Text | bit |  | Yes |
| 7 | Email | Email | bit |  | Yes |
| 8 | Phone | Phone | bit |  | Yes |
| 9 | PatientSignature | PatientSignature | nvarchar |  | Yes |
| 10 | PatientSignatureDate | PatientSignatureDate | datetime |  | Yes |
| 11 | PatientSignatureBy | PatientSignatureBy | nvarchar |  | Yes |
| 12 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 13 | CreatedOn | CreatedOn | datetime |  | Yes |
| 14 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 15 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 16 | IsDeleted | IsDeleted | bit |  | Yes |

---

### StepKey 95 --- pats.tbl_newdischargetransferplanform

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | ClientId | ClientId | int |  | Yes |
| 5 | DataFormId | DataFormId | int |  | Yes |
| 6 | CreatedOn | CreatedOn | datetime |  | Yes |
| 7 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 8 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 9 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 10 | Isdeleted | Isdeleted | bit |  | Yes |
| 11 | DischargeReason | DischargeReason | int |  | Yes |
| 12 | SammsDischargeReason | SammsDischargeReason | int |  | Yes |
| 13 | DischargeDate | DischargeDate | datetime |  | Yes |
| 14 | AdmissionDate | AdmissionDate | datetime |  | Yes |
| 15 | ProgramId | ProgramId | int |  | Yes |
| 16 | Version | Version | varchar |  | Yes |
| 17 | PrimaryDischargeReason | PrimaryDischargeReason | nvarchar |  | Yes |
| 18 | SecondaryDischargeReason | SecondaryDischargeReason | nvarchar |  | Yes |

---

### StepKey 96 --- pats.tbl_mntreatmentservicereview

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | DataFormId | DataFormId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | ReviewPeriod | ReviewPeriod | datetime |  | Yes |
| 7 | SessionStartTime | SessionStartTime | nvarchar |  | Yes |
| 8 | SessionEndTime | SessionEndTime | nvarchar |  | Yes |
| 9 | ServiceDate | ServiceDate | datetime |  | Yes |
| 10 | TreatmentServiceReviewMissAppointment | TreatmentServiceReviewMissAppointment | datetime |  | Yes |
| 11 | TelehealthSession | TelehealthSession | bit |  | Yes |
| 12 | TreatmentServiceReview | TreatmentServiceReview | bit |  | Yes |
| 13 | TreatmentGoal | TreatmentGoal | nvarchar |  | Yes |
| 14 | AreMethodsEffective | AreMethodsEffective | bit |  | Yes |
| 15 | PhysicalMentalHealthProblems | PhysicalMentalHealthProblems | nvarchar |  | Yes |
| 16 | ToxicologyResults | ToxicologyResults | nvarchar |  | Yes |
| 17 | TreatmentPlanning | TreatmentPlanning | nvarchar |  | Yes |
| 18 | SignificantTreatmentPlanning | SignificantTreatmentPlanning | bit |  | Yes |
| 19 | TreatmentPlanningChanges | TreatmentPlanningChanges | bit |  | Yes |
| 20 | AgreeTreatmentPlanningChanges | AgreeTreatmentPlanningChanges | bit |  | Yes |
| 21 | TreatmentPlanChangesExplain | TreatmentPlanChangesExplain | nvarchar |  | Yes |
| 22 | TreatmentServiceReviewReferralsMade | TreatmentServiceReviewReferralsMade | bit |  | Yes |
| 23 | TreatmentServiceReviewMHReferralsMade | TreatmentServiceReviewMHReferralsMade | bit |  | Yes |
| 24 | CoordinationWithReferrals | CoordinationWithReferrals | bit |  | Yes |
| 25 | CoordinationWithReferralsExplain | CoordinationWithReferralsExplain | nvarchar |  | Yes |
| 26 | AdmissionVulnerableAdult | AdmissionVulnerableAdult | bit |  | Yes |
| 27 | CurrentlyVulnerableAdult | CurrentlyVulnerableAdult | bit |  | Yes |
| 28 | IndividualAbusePrevention | IndividualAbusePrevention | bit |  | Yes |
| 29 | ReasonorAssessmentProcess | ReasonorAssessmentProcess | nvarchar |  | Yes |
| 30 | StaffSignature | StaffSignature | nvarchar |  | Yes |
| 31 | StaffSignatureDate | StaffSignatureDate | datetime |  | Yes |
| 32 | StaffSignatureBy | StaffSignatureBy | nvarchar |  | Yes |
| 33 | IsDeleted | IsDeleted | bit |  | Yes |
| 34 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 35 | CreatedOn | CreatedOn | datetime |  | Yes |
| 36 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 37 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 38 | ReviewPeriodToday | ReviewPeriodToday | datetime |  | Yes |

---

### StepKey 97 --- pats.tbl_takehomeagreementanddiversioncontrol

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | ClientId | ClientId | int |  | Yes |
| 5 | DataFormId | DataFormId | int |  | Yes |
| 6 | MedicaidID | MedicaidID | nvarchar |  | Yes |
| 7 | PatientSignature | PatientSignature | nvarchar |  | Yes |
| 8 | PatientSignatureBy | PatientSignatureBy | nvarchar |  | Yes |
| 9 | PatientSignatureDate | PatientSignatureDate | datetime |  | Yes |
| 10 | StaffSignature | StaffSignature | nvarchar |  | Yes |
| 11 | StaffSignatureBy | StaffSignatureBy | nvarchar |  | Yes |
| 12 | StaffSignatureDate | StaffSignatureDate | datetime |  | Yes |
| 13 | IsDeleted | IsDeleted | bit |  | Yes |
| 14 | CreatedBy | CreatedBy | varchar |  | Yes |
| 15 | CreatedOn | CreatedOn | datetime |  | Yes |
| 16 | ModifiedBy | ModifiedBy | varchar |  | Yes |
| 17 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 18 | Version | Version | nvarchar |  | Yes |
| 19 | Patients1 | Patients1 | bit |  | Yes |
| 20 | Patients2 | Patients2 | bit |  | Yes |

---

### StepKey 98 --- pats.tbl_SF_DataForms

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | FormName | FormName | nvarchar |  | Yes |
| 4 | FormURL | FormURL | nvarchar |  | Yes |
| 5 | PatientId | PatientId | int |  | Yes |
| 6 | IsDeleted | IsDeleted | bit |  | Yes |
| 7 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 8 | CreatedOn | CreatedOn | datetime |  | Yes |
| 9 | LastUpdatedBy | LastUpdatedBy | nvarchar |  | Yes |
| 10 | LastUpdatedOn | LastUpdatedOn | date |  | Yes |
| 11 | Program | Program | nvarchar |  | Yes |
| 12 | EnrollmentId | EnrollmentId | int |  | Yes |
| 13 | dsID | dsID | int |  | Yes |
| 14 | EnrollmentDate | EnrollmentDate | datetime |  | Yes |

---

### StepKey 99 --- pats.tbl_TakeHomeRiskAssessment

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | varchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | DataFormId | DataFormId | int |  | Yes |
| 4 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | TakeHomeDosesUnsafe | TakeHomeDosesUnsafe | int |  | Yes |
| 7 | LastDoseIncrease | LastDoseIncrease | int |  | Yes |
| 8 | LikelihoodOfUsingMedication | LikelihoodOfUsingMedication | int |  | Yes |
| 9 | SafeguardingMedication | SafeguardingMedication | int |  | Yes |
| 10 | AbstainingFromOtherSubstances | AbstainingFromOtherSubstances | int |  | Yes |
| 11 | LogisticalBarriers | LogisticalBarriers | int |  | Yes |
| 12 | TotalScore | TotalScore | nvarchar |  | Yes |
| 13 | StaffSignature | StaffSignature | nvarchar |  | Yes |
| 14 | StaffSignatureDate | StaffSignatureDate | datetime |  | Yes |
| 15 | StaffSignatureBy | StaffSignatureBy | nvarchar |  | Yes |
| 16 | IsDeleted | IsDeleted | bit |  | Yes |
| 17 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 18 | CreatedOn | CreatedOn | datetime |  | Yes |
| 19 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 20 | ModifiedOn | ModifiedOn | datetime |  | Yes |

---

### StepKey 100 --- pats.tbl_SMSTextConsentForm

| # | Source (FieldName) | Destination (DsnFieldName) | FieldType | PK | In CHECKSUM |
|---|--------------------|--------------------------|-----------|----|----|
| 1 | @SiteCode | SiteCode | nvarchar | PK1 | No |
| 2 | Id | Id | int | PK2 | Yes |
| 3 | PreAdmissionId | PreAdmissionId | int |  | Yes |
| 4 | DataFormId | DataFormId | int |  | Yes |
| 5 | ClientId | ClientId | int |  | Yes |
| 6 | ClientName | ClientName | nvarchar |  | Yes |
| 7 | PhoneNo | PhoneNo | nvarchar |  | Yes |
| 8 | DoNotAgreetoReceive | DoNotAgreetoReceive | bit |  | Yes |
| 9 | PatientSignature | PatientSignature | nvarchar |  | Yes |
| 10 | PatientSignatureDate | PatientSignatureDate | datetime |  | Yes |
| 11 | PatientSignatureBy | PatientSignatureBy | nvarchar |  | Yes |
| 12 | CreatedBy | CreatedBy | nvarchar |  | Yes |
| 13 | CreatedOn | CreatedOn | datetime |  | Yes |
| 14 | ModifiedBy | ModifiedBy | nvarchar |  | Yes |
| 15 | ModifiedOn | ModifiedOn | datetime |  | Yes |
| 16 | IsDeleted | IsDeleted | bit |  | Yes |
| 17 | Version | Version | nvarchar |  | Yes |
| 18 | Permission | Permission | bit |  | Yes |


---

*Document generated: 2026-06-15 | Source of truth: `BHG-DR-LIB_updated/` + `BHGTaskRunner/updatedProgram.cs` + `ControlTables/vw_mapAction.csv` + `Framework/vw_MapSrc2Dsn.csv`*

