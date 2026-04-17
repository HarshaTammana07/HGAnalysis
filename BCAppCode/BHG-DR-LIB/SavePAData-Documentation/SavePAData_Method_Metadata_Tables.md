
SavePAData.cs — Method Metadata Tables
10 methods | Schedule 6 — Samms-Forms | BHG-DR-LIB
________________________________________

METHOD 1 — SavePA

| Field | Value |
|---|---|
| Name | Pre-Admission form header load |
| Module | Clinical Assessment / Pre-Admission (PA) |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.PA`; column list from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_PA` |
| Load type | EF Core upsert only — no bulk. Guard: `if (tbl.Rows.Count > 0)` — silent early return on empty DataTable. Composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum. No RowState. No LastModAt. `IsDeleted` stored as int (0/1 converted from bool). |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_PA` → PA header records linking patient, treatment pathway, and assessment date; parent record for all six ASAM Dimension sub-tables |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SavePA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SavePAData.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SavePAData.cs`; detail in `SavePAData-Documentation\SavePAData_ETL_Complete_Documentation.md` |
| Known anomalies | `createdon` uses weak guard `length > 0` instead of standard `length > 6` — risk of `DateTime.Parse` exception on short strings. `Version` silently truncated to 2 characters (`Substring(0,2)`) if longer. Numeric key fields (Id, DataFormId, PreAdmissionId, ClientId) parsed with `double.Parse` — EF model stores them as `double`, unusual vs other PA methods that use `int`. `wrkdt` parameter accepted but never used inside the method. |

________________________________________

METHOD 2 — SaveFinancialHardshipApplication

| Field | Value |
|---|---|
| Name | Financial Hardship Application load |
| Module | Billing / Sliding Scale Fee Administration |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.FinancialHardshipApplication`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_FinancialHardshipApplication` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit: updates first, then `AddRange` inserts |
| Load type column | RowChkSum stored but NOT used to guard updates — every existing row unconditionally overwritten. RowState=true stamped on every row; set to false if `IsDeleted`=true (soft-delete). |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_FinancialHardshipApplication` → sliding scale fee approval records, income documentation, payor classification workflows |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveFinancialHardshipApplication(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SavePAData.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SavePAData.cs`; detail in `SavePAData-Documentation\SavePAData_ETL_Complete_Documentation.md` |
| Known anomalies | CRITICAL BUG: `fhapatientsignaturedate` case writes to `xfha.ExpirationDate` instead of `xfha.FHAPatientSignatureDate` — `FHAPatientSignatureDate` is NEVER populated in Azure. Both `fhapatientsignaturedate` and `expirationdate` case labels target the same EF property (`ExpirationDate`); whichever column appears last in the DataTable wins, producing incorrect data. `wrkdt` parameter accepted but never used. |

________________________________________

METHOD 3 — SavePACounselorReview

| Field | Value |
|---|---|
| Name | PA / Periodic Reassessment counselor review load |
| Module | Clinical Assessment / PA Counselor Review |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.PACounselorReview`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_PACounselorReview` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + PeriodicReassessmentId); new rows staged in list; two-phase commit |
| Load type column | RowChkSum stored but NOT used to guard updates — every existing row unconditionally overwritten. RowState=true and LastModAt=DateTime.Now stamped on every row. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_PACounselorReview` → counselor clinical summary, MAT modality recommendations (OTS/OBOT/OTP/OBAT/Induction/Stabilization/Maintenance), COPE phases, USE/risk/protective scores, signature block — feeds treatment compliance and clinical outcome dashboards |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SavePACounselorReview(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SavePAData.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SavePAData.cs`; detail in `SavePAData-Documentation\SavePAData_ETL_Complete_Documentation.md` |
| Known anomalies | CopePhase1, CopePhase2, CopePhase3 are correctly mapped here (no swap). `wrkdt` parameter accepted but never used inside the method. |

________________________________________

METHODS 4–9 — SavePADimension1 through SavePADimension6 (combined)

All six methods share an identical structural pattern. The table below documents the shared
properties. Per-dimension clinical scope differences are noted in the rows that vary.

| Field | SavePADimension1 | SavePADimension2 | SavePADimension3 | SavePADimension4 | SavePADimension5 | SavePADimension6 |
|---|---|---|---|---|---|---|
| Name | ASAM Dim 1 — Substance use / withdrawal | ASAM Dim 2 — Biomedical conditions | ASAM Dim 3 — Emotional / behavioral / cognitive | ASAM Dim 4 — Readiness to change | ASAM Dim 5 — Relapse / continued use potential | ASAM Dim 6 — Recovery / living environment |
| Module | Clinical Assessment / ASAM Dimensions | Clinical Assessment / ASAM Dimensions | Clinical Assessment / ASAM Dimensions | Clinical Assessment / ASAM Dimensions | Clinical Assessment / ASAM Dimensions | Clinical Assessment / ASAM Dimensions |
| Layer | Target load / incremental EF Core upsert | ← same | ← same | ← same | ← same | ← same |
| Source system | SAMMS (SQL Server — per clinic) | ← same | ← same | ← same | ← same | ← same |
| Source DB | `ctrl.tbl_LocationCons.DbName` ActionKey = 6 | ← same | ← same | ← same | ← same | ← same |
| Source table | `dbo.PADimension1` | `dbo.PADimension2` | `dbo.PADimension3` | `dbo.PADimension4` | `dbo.PADimension5` | `dbo.PADimension6` |
| Target DB | Azure SQL — BHG_DR | ← same | ← same | ← same | ← same | ← same |
| Target table | `pats.tbl_PADimension1` | `pats.tbl_PADimension2` | `pats.tbl_PADimension3` | `pats.tbl_PADimension4` | `pats.tbl_PADimension5` | `pats.tbl_PADimension6` |
| Composite key | SiteCode + PeriodicReassessmentId | ← same | ← same | ← same | ← same | ← same |
| Load type | EF Core upsert — full site slice to memory; two-phase commit | ← same | ← same | ← same | ← same | ← same |
| RowChkSum | Stored, NOT used to guard updates | ← same | ← same | ← same | ← same | ← same |
| RowState | true on every row | ← same | ← same | ← same | ← same | ← same |
| LastModAt | DateTime.Now on every row | ← same | ← same | ← same | ← same | ← same |
| Frequency | Daily | ← same | ← same | ← same | ← same | ← same |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` | ← same | ← same | ← same | ← same | ← same |
| Clinical fields | LastUDS, UDSResult, IllegalSubstances, Overdose, NarcanAvailable, Cravings, CravingRating, Dimension1ASAMRating, UAEval | PhysicalHealthChange, Called911, WorseningMedicalCondition, PrimaryCareProvider, UnprotectedSex, DrugInjection, SharingDrug, HIVHepatits, TobaccoNicotine, Dimension2ASAMRating | MentalHealthChange, MentalHealthHospitalized, WorseningMentalHealth, 35 bool symptom flags (Agitation, Anxiety, BrainFog, WishedDead, KillingYourself…), Dimension3ASAMRating | MotivationforChange, TreatmentSatisfaction, EventuallyDiscontinuing, Discontinuing3to6Months, Strengths, Needs, Abilities, PreferedforTreatment, Dimension4ASAMRating | Triggers, CopingStrategies, ContinueUsing, EmploymentStatus, PartFullTime, Arrested, ChangeinLegalStatus, FinancialTrouble, Dimension5ASAMRating | EnvironmentStability, SafefromExploitation, Threats, Children/Custody, FriendsFamilySupport, EnoughMoney, Barriers, 10 housing bool flags (LivesAlone, HouseApartment, Shelter, Unhoused…), Dimension6ASAMRating |
| Owner | BHGTaskRunner / BHG-DR-LIB\SavePAData.cs | ← same | ← same | ← same | ← same | ← same |
| Status | Active | ← same | ← same | ← same | ← same | ← same |
| Known anomalies | **CRITICAL BUG**: `illegalsubstances` and `overdose` case labels both write to `xtm.PreAdmissionId` instead of `xtm.IllegalSubstances` / `xtm.Overdose` → both fields are NEVER populated in Azure; PreAdmissionId potentially corrupted | `hivhepatits` field name contains typo (missing second 'i') — preserved in source column, EF property, and switch case | Dimension 3 is the largest sub-table (42 fields). `WishedDead` (int) and `KillingYourself` (string) are the most clinically sensitive fields — feed safety alerting | `preferedfortreatment` source column contains typo (missing 'd') — preserved in EF property `PreferedforTreatment` and switch case | No specific bugs | No specific bugs |

________________________________________

METHOD 10 — SavedropDownListItems

| Field | Value |
|---|---|
| Name | Clinical reference dropdown list items load |
| Module | Reference Data / Clinic Configuration |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.DropDownListItems`; column list from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `ctrl.tbl_DropDownListItems` |
| Load type | EF Core upsert only — no bulk. Guard: `if (tbl.Rows.Count > 0)` — silent early return on empty DataTable. Composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit. Uses selective field-level comparison before writing (unique pattern in this file — only writes fields that have changed). |
| Load type column | No RowChkSum. No RowState. No LastModAt. Field-level conditionals: `if (dbx.DropDownListItem != itm.DropDownListItem)` etc. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `ctrl.tbl_DropDownListItems` → per-clinic coded value reference table; keeps Azure dropdown decode values in sync with SAMMS configuration for all reporting and ETL decode lookups |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SavedropDownListItems(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SavePAData.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SavePAData.cs`; detail in `SavePAData-Documentation\SavePAData_ETL_Complete_Documentation.md` |
| Known anomalies | TaskName in BHGTaskRunner case and Scheduler task config is `ctrl.tbl_drodownlistitems` — "drodown" is missing the letter 'p'. Both Scheduler and BHGTaskRunner use the same misspelling consistently so dispatch works correctly, but any new tooling using the correct spelling "dropdown" will fail to match. `wrkdt` parameter accepted but never used. |
