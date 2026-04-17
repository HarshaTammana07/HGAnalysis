
# SaveAssessments.cs — Method Metadata Tables

All 19 methods — Field / Value quick-reference tables.
Schedule: 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) | ActionKey = 6 | File: BHG-DR-LIB\SaveAssessments.cs

---

**SaveAdmissionAssessment**

| Field | Value |
|---|---|
| Name | Admission assessment header load |
| Module | Clinical Assessments / ASAM admission evaluation |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessment — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessment |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; IsDeleted defaults false if empty; new rows staged in list; two-phase commit: batch updates then AddRange inserts |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter); wrkdt accepted but unused |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessment → anchor record for all ASAM dimension sub-tables; linked via AdmissionAssessmentId |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessment(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentSummary**

| Field | Value |
|---|---|
| Name | Admission assessment clinical summary and signatures load |
| Module | Clinical Assessments / ASAM admission evaluation |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentSummary — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentSummary |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; four signature sets (Staff/Provider/Patient/Supervisor) mapped with .Trim(); new rows staged in list; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter); ⚠ Known bug: AdmissionAssessmentStaffSignatureDate maps to PatientSignatureDate property — staff date never stored in Azure |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentSummary → ASAM level-of-care recommendation; clinical narrative; signature audit trail |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentSummary(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentDimensionfour**

| Field | Value |
|---|---|
| Name | Admission assessment ASAM Dimension 4 — Readiness to Change load |
| Module | Clinical Assessments / ASAM Dimension 4 / SOCRATES scale |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentDimensionFour — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentDimensionFour |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; IdontThinkUseDrugsTooMuch defaults 0; other int fields are nullable (conditional parse only); new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentDimensionFour → SOCRATES item scores; PrecontemplationScale, ContemplationScale, ActionScale; StageOfChange text |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentDimensionfour(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentDimensionOneDisorder**

| Field | Value |
|---|---|
| Name | Admission assessment ASAM Dimension 1 — Acute Intoxication and Withdrawal load |
| Module | Clinical Assessments / ASAM Dimension 1 / Substance disorder diagnosis |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentDimensionOneDisorder — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentDimensionOneDisorder |
| Load type | EF Core upsert only — no bulk. Dynamic column switch (~55 fields); lookup key is Id; no RowChkSum guard — always overwrites; substance disorder flags, treatment history (4 modalities), MAT history, drug procurement flags all mapped with conditional parse; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter); largest method in file by field count |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentDimensionOneDisorder → 8 substance disorder flags; prior treatment history per modality; MAT history; procurement methods; DdldimensionOneScore |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentDimensionOneDisorder(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentDimensionTwo**

| Field | Value |
|---|---|
| Name | Admission assessment ASAM Dimension 2 — Biomedical Conditions load |
| Module | Clinical Assessments / ASAM Dimension 2 / Medical history |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentDimensionTwo — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentDimensionTwo |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; 23+ medical condition bool flags + infectious disease flags (HIV, Hep A/B/C/D, TB) with conditional bool.Parse; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentDimensionTwo → chronic medical conditions; infectious disease flags; allergies; tobacco use; DdldimensionTwoScore |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentDimensionTwo(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentDimensionThree**

| Field | Value |
|---|---|
| Name | Admission assessment ASAM Dimension 3 — Emotional and Behavioral Conditions load |
| Module | Clinical Assessments / ASAM Dimension 3 / Mental health |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentDimensionThree — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentDimensionThree |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; mental health condition bool flags with conditional bool.Parse; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentDimensionThree → mental health disorder flags (Agoraphobia, Anxiety, BipolarDisorder, Depression, etc.); DdldimensionThreeScore; Dimension3Problems |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentDimensionThree(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentDimensionFiveSubstanceUse**

| Field | Value |
|---|---|
| Name | Admission assessment ASAM Dimension 5 — Relapse Risk and Recovery Environment load |
| Module | Clinical Assessments / ASAM Dimension 5 / Relapse potential |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentDimensionFiveSubstanceUse — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentDimensionFiveSubstanceUse |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; overdose, legal, financial, and family risk factor fields with conditional int.Parse; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter); source column 'yourphysicalmetalworse' preserves SAMMS typo (maps to YourPhysicalMentalWorse property) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentDimensionFiveSubstanceUse → overdose history; legal risk factors (arrests, probation, court cases, child custody); financial stress; DimensionFiveComments |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentDimensionFiveSubstanceUse(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentDimensionSix**

| Field | Value |
|---|---|
| Name | Admission assessment ASAM Dimension 6 — Recovery and Living Environment load |
| Module | Clinical Assessments / ASAM Dimension 6 / Recovery environment |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentDimensionSix — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentDimensionSix |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; housing, employment, peer support, and neighborhood risk factor fields with conditional int.Parse; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentDimensionSix → housing stability; employment status; income source; peer support access; drug culture in neighborhood; DdldimensionSixScore |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentDimensionSix(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessment**

| Field | Value |
|---|---|
| Name | Re-assessment header load |
| Module | Clinical Assessments / ASAM periodic re-assessment |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessment — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessment |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; IsDeleted defaults false if empty; adds TimeInTreatment vs admission assessment header; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessment → anchor record for all 8 re-assessment domain sub-tables; linked via ReassessmentId; TimeInTreatment tracks treatment duration |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessment(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentOccupational**

| Field | Value |
|---|---|
| Name | Re-assessment occupational domain load |
| Module | Clinical Assessments / Re-assessment / Occupational and vocational domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentOccupational — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentOccupational |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; employment status, student status, job-finding fields with conditional int.Parse; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentOccupational → employment status per re-assessment; student enrollment; job-finding progress since last assessment |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentOccupational(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentFamily**

| Field | Value |
|---|---|
| Name | Re-assessment family domain load |
| Module | Clinical Assessments / Re-assessment / Family and social support domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentFamily — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentFamily |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; housing stability, financial capacity, child custody, DFS case, domestic safety fields; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentFamily → housing stability; child custody status; DFS open cases; domestic safety per re-assessment interval |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentFamily(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentLegal**

| Field | Value |
|---|---|
| Name | Re-assessment legal domain load |
| Module | Clinical Assessments / Re-assessment / Legal domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentLegal — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentLegal |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; drug court, probation/parole, open warrants, court fines, criminal cases, arrest fields; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentLegal → legal entanglement tracking per re-assessment interval; drug court involvement; probation compliance |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentLegal(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentMentalHealth**

| Field | Value |
|---|---|
| Name | Re-assessment mental health domain load |
| Module | Clinical Assessments / Re-assessment / Mental health domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentMentalHealth — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentMentalHealth |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; 3 clinical fields (DoYouHaveApsychiatrist, HaveYouBeenHospitalizedForMentalHealthReasons, HowHasYourMentalHealthChanged); new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter); FK property name is ReAssessmentId (uppercase A, differs from most re-assessment sub-tables which use ReassessmentId) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentMentalHealth → psychiatrist access; recent MH hospitalization; mental health change score per re-assessment |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentMentalHealth(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentPhysicalHealth**

| Field | Value |
|---|---|
| Name | Re-assessment physical health domain load |
| Module | Clinical Assessments / Re-assessment / Physical health domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentPhysicalHealth — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentPhysicalHealth |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; HIV/HCV test result checkboxes (bool.Parse), care access, IV drug use, unsafe sex, ER use fields; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentPhysicalHealth → HIV/HCV test results per re-assessment; IV drug use tracking; ER use; primary care provider access |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentPhysicalHealth(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentSubstanceUse**

| Field | Value |
|---|---|
| Name | Re-assessment substance use domain load |
| Module | Clinical Assessments / Re-assessment / Substance use domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentSubstanceUse — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentSubstanceUse |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; 3 clinical fields (HaveYouHadAnOverdose, DoYouUseTobaccoOrVapeNicotine, CommentsSubstanceUse); new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter); FK property name is ReAssessmentId (uppercase A, same as MentalHealth) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentSubstanceUse → overdose tracking per re-assessment interval; tobacco/vaping use; CommentsSubstanceUse free text |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentSubstanceUse(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentSocial**

| Field | Value |
|---|---|
| Name | Re-assessment social support domain load |
| Module | Clinical Assessments / Re-assessment / Social support domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentSocial — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentSocial |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; sober social network, support network, peer support awareness fields; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentSocial → sober social network quality; support system strength; peer support awareness per re-assessment |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentSocial(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveReAssessmentTreatment**

| Field | Value |
|---|---|
| Name | Re-assessment treatment satisfaction domain load |
| Module | Clinical Assessments / Re-assessment / Treatment satisfaction and goals domain |
| Layer | Target load / always-update integration |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.ReAssessmentTreatment — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblReAssessmentTreatment |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is Id; no RowChkSum guard — always overwrites; satisfaction score, tapering plan, tapering goal fields plus 2 free-text narrative fields; new rows staged; two-phase commit |
| Load type column | No change detection — all existing rows always overwritten; Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblReAssessmentTreatment → treatment satisfaction scores; tapering intentions; patient-reported learning and unmet needs per re-assessment |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveReAssessmentTreatment(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAssessmentSubstanceuseHistory**

| Field | Value |
|---|---|
| Name | Re-assessment substance use history detail load |
| Module | Clinical Assessments / Re-assessment / Substance use history detail |
| Layer | Target load / incremental integration with RowChkSum and RowState |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AssessmentSubstanceUseHistory — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAssessmentSubstanceUseHistories |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is composite SiteCode + Id; RowChkSum read in "sitecode" case via dr["RowChkSum"] directly; RowChkSum stored on every update but NO skip guard — all rows still always overwritten; AssessmentFormId mapped; new rows staged in list; two-phase commit: batch updates then AddRange inserts |
| Load type column | RowChkSum stored per row (no skip guard — does not prevent writes); RowState = true set on all rows; RowState = false when CltId < 0 (negative CltId = invalid/deleted patient sentinel in SAMMS); Azure scope: all rows WHERE SiteCode = sc (no date filter); AssessmentFormId maps to periodic assessment form |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAssessmentSubstanceUseHistories → substance-by-substance use history per re-assessment episode; route, amount, frequency, age of first use, withdrawal per substance |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAssessmentSubstanceuseHistory(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

**SaveAdmissionAssessmentSubstanceuseHistory**

| Field | Value |
|---|---|
| Name | Admission assessment substance use history detail load |
| Module | Clinical Assessments / Admission Assessment / Substance use history detail |
| Layer | Target load / incremental integration with RowChkSum and RowState |
| Source system | SAMMS (SQL Server — per clinic) |
| Source DB | ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode |
| Source table | dbo.AdmissionAssessmentSubstanceUseHistory — column list built from SelectConstructor / dms.tbl_MapSrc2Dsn (ActionKey 6) |
| Target DB | Azure SQL — BHG_DR |
| Target table | TblAdmissionAssessmentSubstanceUseHistory |
| Load type | EF Core upsert only — no bulk. Dynamic column switch; lookup key is composite SiteCode + Id; RowChkSum read in "sitecode" case; RowChkSum stored on every update but NO skip guard; AssessmentFormId NOT present (unlike method 18); new rows staged; two-phase commit |
| Load type column | RowChkSum stored per row (no skip guard); RowState = true set on all rows; RowState = false when CltId < 0; CreatedOn and DateOfLastUse use per-field try/catch with Console.WriteLine on parse failure; DateOfReported uses .Trim() + length > 6 + try/catch (most defensive date handling in file); Azure scope: all rows WHERE SiteCode = sc (no date filter) |
| Frequency | Daily |
| Schedule | Schedule 6 — BHGTaskRunner.exe 6 (SAMMS-Forms) |
| Parent | SAMMS-Forms |
| Downstream | TblAdmissionAssessmentSubstanceUseHistory → substance-by-substance use history at admission; complete drug use profile per patient; route, amount, frequency, age of first use, withdrawal per substance |
| Connection / method | Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveAdmissionAssessmentSubstanceuseHistory(SrcDt, st.SiteCode, WorkDate, null) |
| Server / DB / API | Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext |
| Owner | BHGTaskRunner / BHG-DR-LIB\SaveAssessments.cs |
| Status | Active |
| Folder | BHG-DR-LIB\SaveAssessments.cs; detail in SaveAssessments-Documentation\SaveAssessments_ETL_Complete_Documentation.md |

---

> **Source table confidence notes:**
> Confirmed directly from code/SQL artefacts: `dbo.AdmissionAssessment`, `dbo.AdmissionAssessmentSummary`, `dbo.ReAssessment`, `dbo.ReAssessmentSocial`, `dbo.ReAssessmentTreatment`, `dbo.AdmissionAssessmentSubstanceUseHistory`.
> Remaining tables (all Dimension tables, ReAssessmentOccupational/Family/Legal/MentalHealth/PhysicalHealth/SubstanceUse, AssessmentSubstanceUseHistory) follow the confirmed SAMMS naming convention (`dbo.{EFEntitySuffix}`) and match column evidence in the switch statements. Authoritative source is `dms.tbl_MapAction.FromTblVw` in the Azure BHG_DR database.
