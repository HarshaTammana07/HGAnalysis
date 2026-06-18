# Regional P1 — Fabric Pipeline Implementation Guide

Covers the architecture and approach for migrating **Regional ETL P1** (57 destination tables, 80+ SAMMS clinic sites) from the C# BHGTaskRunner into Microsoft Fabric using the Bronze → Silver → Gold medallion pattern, consistent with the existing Notes, FormAnswerSignatures, FormQuestionAnswers, DartsSrv, and Dose pipelines.

---

## Background — what P1 is today (C# ETL)

- Runner: `BHGTaskRunner.exe 2`
- Scheduler task names: `Eastern ETL P1`, `Central ETL P1`, `Mountain ETL P1`, `Pacific ETL P1`
- **57 destination tables** in `BHG_DR` (Azure SQL)
- Source: 80+ clinic SAMMS SQL Server databases (on-premises, via `ctrl.tbl_LocationCons`)
- Method mix: mostly `Save*` (EF Core upsert) per table, two tables via `BulkDartsSrvLoader` (`SqlBulkCopy` + stored-procedure MERGE)
- Every row carries a `RowChkSum = CHECKSUM(...)` — only changed rows are written

Full source-to-destination mapping: see `Regional_P1_P2_Source_to_Destination.md`

---

## Key constraint — JDBC is not available in Fabric notebooks

Fabric Notebooks **cannot** connect to on-premises SQL Server via JDBC through the on-premises data gateway. The only proven path for Bronze extraction from SAMMS is the **Fabric Pipeline Copy activity** (which supports the on-premises data gateway). This is the same mechanism used by all existing pipelines (Notes, FormAnswerSignatures, DartsSrv, etc.).

This rules out a single "loop over 57 tables in a notebook" Bronze extraction approach. The Copy activity must be used per table.

---

## Why the Notes pattern cannot be applied directly to 57 tables

The Notes child pipeline has:
- 2 Filter activities (one per Method)
- 2 ForEach blocks (one ARNote, one ClaimNote)
- 1 Copy per ForEach
- 2 Silver notebooks
- 2 Gold Copy activities

Applying that pattern literally to 57 P1 tables would require:
- 57 Filter activities
- 57 ForEach blocks
- 57 Silver notebooks
- 57 Truncate + Copy activities at Gold

That is unmanageable as a Fabric pipeline JSON and hits Fabric activity limits. The solution is **domain-grouped child pipelines**.

---

## Overall architecture — P1 in Fabric

```
Execute_P1  (parent pipeline)
│
├── lkp_p1_taskconfig           Lookup: reads meta.taskconfig for P1 ConfigId
├── flt_active_p1_sites         Filter: IsActive=1, Method in P1 table list
├── nb_p1_audit_start           Audit writer: mode = START_LAYER_RUNS
│
├── [6 domain child pipelines — run in PARALLEL, all waitOnCompletion=true]
│   ├── Execute pl_p1_assessments_to_bronze
│   ├── Execute pl_p1_clinical_to_bronze
│   ├── Execute pl_p1_financial_to_bronze
│   ├── Execute pl_p1_forms_to_bronze
│   ├── Execute pl_p1_reference_to_bronze
│   └── Execute pl_p1_bulk_to_bronze
│
├── [6 Silver notebooks — run in PARALLEL after all Bronze complete]
│   ├── nb_p1_silver_assessments
│   ├── nb_p1_silver_clinical
│   ├── nb_p1_silver_financial
│   ├── nb_p1_silver_forms
│   ├── nb_p1_silver_reference
│   └── nb_p1_silver_bulk  (or Script: run stg.* MERGE stored procedures)
│
├── Script: TRUNCATE all Gold tables  (depends on all Silver notebooks succeeded)
│
├── ForEach Gold table → cp_silver_to_gold  (batchCount: 10, isSequential: false)
│
├── Validate_P1_Gold_Load   (Script: COUNT_BIG all 57 Gold tables)
│
├── nb_p1_audit_finalize_success   (depends on Validate succeeded)
└── nb_p1_audit_finalize_failure   (depends on Validate failed/skipped)
```

---

## Bronze layer — domain-grouped child pipelines

### Why domain groups?

Each group contains 8–15 related tables. Inside one child pipeline, one `ForEach` iterates over all SAMMS sites. Inside the `ForEach`, there is one Copy activity per table in that group — each with its own `@concat(...)` SQL expression, its own date-column lookback filter, and its own Bronze sink table name.

This mirrors the Notes child pipeline (2 Copies in ForEach) but with more tables per ForEach. No notebook is needed for SQL building — the `@concat` expression in the Copy source is sufficient, exactly like Notes.

---

### Domain split — 57 tables across 6 child pipelines

#### 1. `pl_p1_assessments_to_bronze` (~15 tables)

| Source (SAMMS) | Bronze Sink |
|---|---|
| `dbo.PeriodicReassessment` | `br_tblPA` |
| `dbo.PACounselorReview` | `br_tblPACounselorReview` |
| `dbo.PADimension1` | `br_tblPADimension1` |
| `dbo.PADimension2` | `br_tblPADimension2` |
| `dbo.PADimension3` | `br_tblPADimension3` |
| `dbo.PADimension4` | `br_tblPADimension4` |
| `dbo.PADimension5` | `br_tblPADimension5` |
| `dbo.PADimension6` | `br_tblPADimension6` |
| `dbo.NewAdmissionAssessment` | `br_tblNewAdmissionAssessment` |
| `dbo.NewAdmissionAssessmentASAMDimension6` | `br_tblNewAdmissionAssessmentASAMDimension6` |
| `dbo.NewPeriodicReassessment` | `br_tblNewPeriodicReassessment` |
| `dbo.NewPeriodicReassessmentCounselorReview` | `br_tblNewPeriodicReassessmentCounselorReview` |
| `dbo.newperiodicreassessmentd2` | `br_tblNewPeriodicReassessmentD2` |
| `dbo.newperiodicreassessmentd3` | `br_tblNewPeriodicReassessmentD3` |
| `dbo.newperiodicreassessmentd4` | `br_tblNewPeriodicReassessmentD4` |
| `dbo.newperiodicreassessmentd5` | `br_tblNewPeriodicReassessmentD5` |
| `dbo.newperiodicreassessmentd6` | `br_tblNewPeriodicReassessmentD6` |
| `dbo.admissionassessmentsubstanceusehistory` | `br_tblAdmissionAssessmentSubstanceUseHistory` |

#### 2. `pl_p1_clinical_to_bronze` (~12 tables)

| Source (SAMMS) | Bronze Sink |
|---|---|
| `dbo.tblCHECKIN` | `br_tblCheckIn` |
| `dbo.BAMForm` | `br_tblBAMForm` |
| `dbo.BAMScore` | `br_tblBAMScore` |
| `dbo.SF_COWS` | `br_tblCows_V6` |
| `dbo.tblUAResult` | `br_tblUAResults` |
| `dbo.tblUASched` | `br_tblUASched` |
| `dbo.AppointmentAttend` | `br_tblAppointmentAttend` |
| `dbo.MNComprehensiveAssessment` | `br_tblMNComprehensiveAssessment` |
| `dbo.MNComprehensiveAssessmentLevelOfCare` | `br_tblMNComprehensiveAssessmentLevelOfCare` |
| `dbo.mntreatmentservicereview` | `br_tblMNTreatmentServiceReview` |
| `bhg.vw_Enrollment / dbo.tblENROLL / oak.vw_pt_Enrollments` | `br_tblEnrollment` |
| `dbo.VAComprehensiveAssessment` | `br_tblVAComprehensiveAssessment` |
| `dbo.vacomprehensiveassessmentsummary` | `br_tblVAComprehensiveAssessmentSummary` |

#### 3. `pl_p1_financial_to_bronze` (~7 tables)

| Source (SAMMS) | Bronze Sink |
|---|---|
| `dbo.tblBill` | `br_tblBills` |
| `dbo.tbl3PAYauth` | `br_tblpbi3PayAuth` |
| `dbo.vw3pBillSub` | `br_tblvw3pBillSub` |
| `dbo.tblFMP` | `br_tblFmp` |
| `dbo.tblPayerCltHistory` | `br_tblPayerCltHistory` |
| `dbo.FinancialHardshipApplication` | `br_tblFinancialHardshipApplication` |
| `dbo.Tbl3pElig` | `br_tbl3pElig` |

#### 4. `pl_p1_forms_to_bronze` (~10 tables)

| Source (SAMMS) | Bronze Sink |
|---|---|
| `dbo.ComprehensiveAssessmentForm` | `br_tblComprehensiveAssessmentForm` |
| `dbo.EandMFormPregnancy` | `br_tblEandMFormPregnancy` |
| `dbo.SF_DataForms` | `br_tblSF_DataForms` |
| `dbo.SMSTextConsentForm` | `br_tblSMSTextConsentForm` |
| `dbo.consenttomarketing` | `br_tblConsentToMarketing` |
| `dbo.takehomeagreementanddiversioncontrol` | `br_tblTakeHomeAgreementandDiversionControl` |
| `dbo.TakeHomeRiskAssessment` | `br_tblTakeHomeRiskAssessment` |
| `dbo.newdischargetransferplanform` | `br_tblNewDischargeTransferPlanForm` |

#### 5. `pl_p1_reference_to_bronze` (~9 tables)

| Source (SAMMS) | Bronze Sink |
|---|---|
| `dbo.tblClinic` | `br_tblClinic` |
| `dbo.tbl3PSETUP` | `br_tbl3PSETUP` |
| `dbo.tblCodes` | `br_tblCodes` |
| `dbo.tblSERVICES` | `br_tblSERVICES` |
| `dbo.DroDownListItems` | `br_tblDroDownListItems` |
| `dbo.tblCUSTOMANSWERS` | `br_tblCustomAnswers` |
| `dbo.tblCUSTOMQUESTIONS` | `br_tblCustomQuestions` |
| `dbo.SF_PatientPreAdmission` → `ayx.tbl_PreAdmission_V6` | `br_tblPreAdmission_V6` |
| `dbo.SF_PatientPreadmissionReferralSource` | `br_tblPreadmissionReferralSource` |

#### 6. `pl_p1_bulk_to_bronze` (~2 tables — special path)

| Source (SAMMS) | Bronze Sink | Notes |
|---|---|---|
| `dbo.Tbldiag10` | `br_tblDiag10` | Uses `BulkDartsSrvLoader` in C#; may need stg.* MERGE stored procedure instead of Delta MERGE Silver |
| `dbo.tblclient` → `stg.ClientDemo` | `br_tblClientDemo` | Bulk-staged client data; post-Bronze step is a stored procedure, not a Silver notebook |

---

### Inside each domain child pipeline — structure

```
pl_p1_<domain>_to_bronze
  └── ForEach site  (isSequential: false, batchCount: 5)
        ├── lkp_check_<table1>_exists   → if exists → cp_<table1>_to_bronze
        ├── lkp_check_<table2>_exists   → if exists → cp_<table2>_to_bronze
        ...  (each independent — no dependsOn between them)
        └── lkp_check_<tableN>_exists   → if exists → cp_<tableN>_to_bronze
```

**Each Copy activity:**
- **Source type:** `SqlServerSource`
- **sqlReaderQuery:** `@concat(...)` expression — hard-coded column list for that table, date-column lookback filter, RowChkSum, site metadata columns (`_site_code`, `_source_database`, `_ingest_run_id`, `_extracted_at`, `_source_query_date_anchor`)
- **Connection:** same on-premises data gateway connection used by other pipelines
- **Sink type:** `LakehouseTableSink`
- **Sink table action:** `Append`
- **Sink schema:** `P1` (or domain name — e.g. `Assessments`, `Clinical`, etc.)
- **Sink table:** `br_<tableName>`

**Lookup (table-exists check):**
- Same pattern as `lkp_check_answersig_table` in FormAnswerSignatures
- Query: `SELECT COUNT(1) AS table_exists FROM [<DB>].sys.tables WHERE name = '<SourceTable>'`
- If result = 0 → site does not have this table → skip (If Condition False branch = empty)

**Parameters passed to child pipeline:**
- `p_ingest_run_id` (from `pipeline().RunId`)
- `p_work_date`
- `p_lookback_days` (default 15; 90 on month-end Fridays; 200 on special dates)
- `p_sites` (array from `flt_active_p1_sites` output)

---

## Silver layer — domain-grouped notebooks

One Silver notebook per domain group. Runs **after all Bronze child pipelines complete**. Domain notebooks run in **parallel** with each other.

Each Silver notebook:
1. Reads Bronze table(s) filtered by `_ingest_run_id`
2. Deduplicates on business key (PK columns per table)
3. Applies `RowState` logic (same rules as C# `Save*` method for that table)
4. Computes `LastModAt = current_timestamp()`
5. Delta MERGE into Silver (`bhg_silver.<schema>.sl_<tableName>`)
   - Match: `_site_code + PK columns`
   - When matched + RowChkSum changed → full update
   - When matched + RowChkSum same → metadata-only update (`last_seen_at`, `_ingest_run_id`)
   - When not matched → insert

| Silver notebook | Tables covered |
|---|---|
| `nb_p1_silver_assessments` | All 18 assessment-group tables |
| `nb_p1_silver_clinical` | All 13 clinical-group tables |
| `nb_p1_silver_financial` | All 7 financial-group tables |
| `nb_p1_silver_forms` | All 8 form-group tables |
| `nb_p1_silver_reference` | All 9 reference-group tables |
| `nb_p1_silver_bulk` | Script activity: exec stg.* MERGE stored procedures for ClientDemo, Diag10 |

---

## Gold layer

### Step 1 — Truncate all Gold tables (Script activity)

One `Script` activity against `bhg_gold` Data Warehouse. Runs after all Silver notebooks succeeded.

```sql
TRUNCATE TABLE pats.gd_pa;
TRUNCATE TABLE pats.gd_pa_counselor_review;
TRUNCATE TABLE pats.gd_pa_dimension1;
-- ... all 55 non-bulk Gold tables
```

### Step 2 — Copy Silver → Gold (ForEach or parallel Copies)

Two options:

**Option A — ForEach over table list (fewer activities in pipeline)**
- One `ForEach` activity with `batchCount: 10`, `isSequential: false`
- Pipeline variable `p1_gold_tables` = array of `{silver_table, gold_table}` objects
- One Copy inside ForEach with dynamic source/sink table names from `item()`

**Option B — Parallel Copy activities (more transparent, same pattern as Notes)**
- ~55 Copy activities grouped with `dependsOn: Truncate_GoldTables_P1`
- All run in parallel after the TRUNCATE
- Grouped into 6-8 batches using `dependsOn` if Fabric limits apply

Recommended: **Option A (ForEach)** for maintainability at 57-table scale.

### Step 3 — Validate Gold Load (Script activity)

```sql
SELECT TableName = 'gd_pa',         RowCount = COUNT_BIG(*) FROM pats.gd_pa
UNION ALL
SELECT 'gd_pa_dimension1',           COUNT_BIG(*) FROM pats.gd_pa_dimension1
-- ... all Gold tables
```

---

## Audit writer

Same single audit-writer notebook used by all existing pipelines. Same modes:

| Activity | Mode passed |
|---|---|
| `nb_p1_audit_start` | `START_LAYER_RUNS` |
| `nb_p1_audit_finalize_success` | `FINALIZE_P1_SUCCESS` |
| `nb_p1_audit_finalize_failure` | `FINALIZE_P1_FAILURE` |

Parameters passed to audit finalize (success and failure):
- `p_audit_context_json` — from `nb_p1_audit_start` exit value
- `p_ingest_run_id` — `pipeline().RunId`
- `p_sites_json` — `string(activity('flt_active_p1_sites').output.value)`
- `p_status` — `SUCCESS` or `FAILED`
- `p_error_message` — `coalesce(activity(...).error, ...)` chain of all activities (failure path only)

---

## Bulk tables — special handling

`stg.ClientDemo` and `pats.tbl_TblDiag10` use `BulkDartsSrvLoader` in C# (`SqlBulkCopy` into `stg.*` then a stored-procedure MERGE into `pats.*`).

In Fabric, these follow a different path after Bronze:

```
cp_<bulktable>_to_bronze  (same Copy activity pattern)
  └── nb_p1_silver_bulk  (OR Script activity)
        └── exec stg.<MergeStoredProc> for each site
              └── Stored procedure MERGEs stg.* into pats.*
```

These do **not** go through the Delta MERGE Silver notebook pattern. The stored procedures in Azure BHG_DR handle the final load. Gold for these tables may be a direct query/view against `pats.*` rather than a separate `gd_*` copy.

---

## taskconfig table entries needed

One row per domain group in `bhg_bronze.meta.taskconfig`:

| ConfigId | ConfigName | IsActive | Notes |
|---|---|---|---|
| TBD | `P1 Assessments` | 1 | Assessment + ASAM group |
| TBD | `P1 Clinical` | 1 | Clinical/UA/Enrollment group |
| TBD | `P1 Financial` | 1 | Bills/Auths/Payor group |
| TBD | `P1 Forms` | 1 | Assessment forms group |
| TBD | `P1 Reference` | 1 | Clinic/Setup/Codes group |
| TBD | `P1 Bulk` | 1 | ClientDemo/Diag10 bulk group |

The `Method` field per row carries the SAMMS source table name so `flt_active_p1_sites` can group sites by domain.

---

## Comparison — Notes pattern vs P1 pattern

| | Notes (implemented) | P1 (proposed) |
|---|---|---|
| Tables | 2 | 57 |
| Child pipelines | 1 | 6 (one per domain) |
| Copies per ForEach | 1 per table type | 8–15 per domain child |
| Bronze sink | `Notes.br_tbl3pArnote`, `Notes.br_tbl3pClaimNote` | `P1.<schema>.br_<tableName>` × 57 |
| SQL building | `@concat(...)` in Copy source | Same — `@concat(...)` per Copy |
| Table-exists check | Lookup: check `sys.columns` for `globalBatchId` | Lookup: check `sys.tables` for table name |
| Silver | 2 notebooks (parallel) | 6 notebooks (parallel, domain-grouped) |
| Silver merge key | `_site_code + arnID` / `_site_code + tpcnTPCID` | `_site_code + <PK per table>` |
| Gold Truncate + Copy | 2 | 55+ (ForEach or parallel Copies) |
| Audit writer notebook | 1 shared | Same 1 shared |
| JDBC needed? | No | No |

---

## Related files

| File | Purpose |
|---|---|
| `Scheduler/Regional_P1_P2_Source_to_Destination.md` | Full 57-table source-to-destination mapping |
| `Scheduler/Scheduler_ETL_and_Tables.md` | Timezone routing, scheduler batches |
| `BHGTaskRunner/updatedProgram.cs` | Task runner switch — maps destination table to Save method |
| `BHG-DR-LIB_updated/SavePAData.cs` | PA, PADimension, FinancialHardship Save methods |
| `BHG-DR-LIB_updated/SaveCA.cs` | NewAdmission, Periodic, MNCA, VACA Save methods |
| `BHG-DR-LIB_updated/SaveDataFeb26.cs` | ConsentToMarketing, DataForms, TakeHome etc. |
| `BHG-DR-LIB_updated/BulkDartsSvc.cs` | BulkDartsSrvLoader — ClientDemo, Diag10 |
| `BHG-DR-LIB/Save3pElig-Documentation/notesdefinetion.txt` | Notes child/parent pipeline JSON reference |
| `SaveFormQADocumentation/formanswersignaturedefination.txt` | FormAnswerSignatures pipeline JSON reference |
