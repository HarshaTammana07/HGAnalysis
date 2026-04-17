
SaveCA.cs — Method Metadata Tables
All 8 methods | Schedule 6 — Samms-Forms | BHG-DR-LIB
________________________________________

METHOD 1 — SaveMNCA

| Field | Value |
|---|---|
| Name | Minnesota Comprehensive Assessment header load |
| Module | Clinical Assessment / MN CA |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.MNComprehensiveAssessment`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_MNComprehensiveAssessment` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit: updates first, then `AddRange` inserts |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. IsDeleted mapped from source. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_MNComprehensiveAssessment` → MN CA compliance reporting, level-of-care audit, treatment plan reviews |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveMNCA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `MNComprehensiveAssessment` before calling this method. If absent: task fails with "Table does not exists." |
| Known anomalies | `TodayDate` uses weak guard (`length > 0`) instead of standard `length > 6` — risk of `DateTime.Parse` exception on short malformed date strings. `RowsUpd` counter not incremented (only `RowsIns` tracked). |

________________________________________

METHOD 2 — SaveMNCALOC

| Field | Value |
|---|---|
| Name | Minnesota Comprehensive Assessment Level of Care sub-form load |
| Module | Clinical Assessment / MN CA Level of Care |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.MNComprehensiveAssessmentlevelofcare`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_MNComprehensiveAssessmentLevelOfCare` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + MNComprehensiveAssessmentFormId); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. No IsDeleted. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_MNComprehensiveAssessmentLevelOfCare` → ASAM level-of-care determination records, accreditation and payer-required variance documentation |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveMNCALOC(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `MNComprehensiveAssessmentlevelofcare` before calling this method |
| Known anomalies | `RowsUpd` counter not incremented. This is the most data-rich of the two MN CA tables — captures all ASAM level flags, opioid counseling checklist, variance reasons, and accessibility barriers. |

________________________________________

METHOD 3 — SaveVACA

| Field | Value |
|---|---|
| Name | VA Comprehensive Assessment header load |
| Module | Clinical Assessment / VA CA |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.VAComprehensiveAssessment`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_VAComprehensiveAssessment` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. IsDeleted mapped from source. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_VAComprehensiveAssessment` → VA clinic CA header records, links to VA CA Summary sub-form via Id |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveVACA(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `VAComprehensiveAssessment` before calling this method |
| Known anomalies | The VA CA header is a thin record — all clinical content lives in the VA CA Summary sub-form (SaveVACASummary). `RowsUpd` counter not incremented. |

________________________________________

METHOD 4 — SaveVACASummary

| Field | Value |
|---|---|
| Name | VA Comprehensive Assessment clinical summary sub-form load |
| Module | Clinical Assessment / VA CA Summary |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.vacomprehensiveassessmentsummary`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_VAComprehensiveAssessmentSummary` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. No IsDeleted. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_VAComprehensiveAssessmentSummary` → ASAM level recommendation, OTP service codes, withdrawal management, clinical narrative for VA clinic compliance reporting |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveVACASummary(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `vacomprehensiveassessmentsummary` before calling. NOTE: Duplicate case label `pats.pats.tbl_vacomprehensiveassessmentsummary` (double-prefix typo) also exists in BHGTaskRunner and routes to the same method. |
| Known anomalies | `LastModAt` assignment is commented out — the ETL audit timestamp is never populated or updated for VA CA Summary records. `RowsUpd` counter not incremented. |

________________________________________

METHOD 5 — SaveNewAdmissionAssessment

| Field | Value |
|---|---|
| Name | New Admission Assessment header load |
| Module | Clinical Assessment / New Admission Assessment (NAA) |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.NewAdmissionassessment`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_NewAdmissionAssessment` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. IsDeleted mapped from source. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_NewAdmissionAssessment` → NAA header records, links to NAA ASAM Dimension 6 sub-form, drives new patient admission assessment compliance tracking |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveNewAdmissionAssessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `NewAdmissionassessment` before calling. NOTE: Duplicate case label `pats.pats.tbl_newadmissionassessment` (double-prefix typo) also exists in BHGTaskRunner and routes to the same method. |
| Known anomalies | The NAA header is a thin record — all clinical content (ASAM dimensions, signature block, readiness questions) lives in the ASAM Dimension 6 sub-form. `RowsUpd` counter not incremented. |

________________________________________

METHOD 6 — SaveNewAdmissionAssessmentASAMDimension6

| Field | Value |
|---|---|
| Name | New Admission Assessment ASAM Dimension 6 sub-form load |
| Module | Clinical Assessment / NAA ASAM Dimension 6 |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.NewAdmissionassessmentASAMDimension6`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_NewAdmissionAssessmentASAMDimension6` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + NewAdmissionAssessmentFormId); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. No IsDeleted. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_NewAdmissionAssessmentASAMDimension6` → readiness-to-change scores (Q1–Q12), stage of change, social determinants barriers, ASAM level recommendations, four-party signature block, clinical summary — drives prior authorization and accreditation audit workflows |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveNewAdmissionAssessmentASAMDimension6(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `NewAdmissionassessmentASAMDimension6` before calling |
| Known anomalies | All four signature date fields (PatientSignatureDate, CounselorSignatureDate, SupervisorSignatureDate, ProviderSignatureDate) use weak guard `length > 0` instead of standard `length > 6` — risk of `DateTime.Parse` exception on short strings. Source column `superviosorsignna` contains a typo ("iosor" instead of "isor") preserved in the SAMMS table, EF model property `SuperviosorSignNA`, and the switch case — must remain consistent across all three or mapping silently breaks. `RowsUpd` counter not incremented. Most data-rich form in SaveCA.cs. |

________________________________________

METHOD 7 — SaveNewPeriodicReassessment

| Field | Value |
|---|---|
| Name | New Periodic Reassessment header load |
| Module | Clinical Assessment / New Periodic Reassessment (NPR) |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.newperiodicreassessment`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_NewPeriodicReassessment` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + Id); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. No IsDeleted. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_NewPeriodicReassessment` → NPR header records linking patient, date, treatment pathway, and completion location; parent record for NPR Counselor Review sub-form |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.SaveNewPeriodicReassessment(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `newperiodicreassessment` before calling |
| Known anomalies | `LastModAt` assignment is commented out — the ETL audit timestamp is never populated or updated for NPR header records. `RowsUpd` counter not incremented. |

________________________________________

METHOD 8 — Savenewperiodicreassessmentcounselorreview

| Field | Value |
|---|---|
| Name | New Periodic Reassessment counselor review sub-form load |
| Module | Clinical Assessment / NPR Counselor Review |
| Layer | Target load / incremental EF Core upsert |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | `ctrl.tbl_LocationCons.DbName` where ActionKey = 6 + SiteCode |
| Source table | `dbo.newperiodicreassessmentcounselorreview`; column list + RowChkSum from `SelectConstructor` / `dms.tbl_MapSrc2Dsn` (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | `pats.tbl_NewPeriodicReassessmentCounselorReview` |
| Load type | EF Core upsert only — no bulk. Full site slice loaded to memory; composite key lookup (SiteCode + NewPeriodicReassessmentId); new rows staged in list; two-phase commit |
| Load type column | No RowChkSum guard — every existing row unconditionally overwritten. No RowState. No IsDeleted. |
| Frequency | Daily |
| Schedule | Schedule 6 — `BHGTaskRunner.exe 6` (Samms-Forms) |
| Parent | Samms-Forms |
| Downstream | `pats.tbl_NewPeriodicReassessmentCounselorReview` → periodic ASAM level reassessment, COPE phase, MAT pathway (Induction/Stabilization/Maintenance), BAM-derived risk/use/protective scores, variance flags, four-party signature block — drives treatment plan compliance and periodic review reporting |
| Connection / method | Source: `sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)`. Target: `sd.Savenewperiodicreassessmentcounselorreview(SrcDt, st.SiteCode, st.WorkDate.Value.AddDays(DaysBack), null)` |
| Server / DB / API | Source: `st.ConStr`. Target: Azure BHG_DR via `BHG_DRContext` |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveCA.cs |
| Status | Active |
| Folder | `BHG-DR-LIB\SaveCA.cs`; detail in `SaveCA-Documentation\SaveCA_ETL_Complete_Documentation.md` |
| Table existence pre-check | YES — BHGTaskRunner checks `sys.tables` for `newperiodicreassessmentcounselorreview` before calling |
| Known anomalies | CRITICAL BUG: `copephase1` source column maps to `ca.CopePhase2` and `copephase2` maps to `ca.CopePhase1` — values are swapped in Azure for every row. `CopePhase3` is correctly mapped. `LastModAt` assignment is commented out — ETL audit timestamp never populated. `CounselorSignatureDate` uses weak guard `length > 0` instead of standard `length > 6` — risk of `DateTime.Parse` exception. `RowsUpd` counter not incremented. Method name is entirely lowercase — inconsistent with C# naming conventions. |
