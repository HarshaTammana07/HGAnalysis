# Chat Handoff Summary - Fabric ETL Work

This file summarizes the important work, decisions, and current status from the long Fabric migration chat. Use this as the starting point for a new Codex chat.

## Current Focus

Most recent focus was the **Doses ETL** under:

- `BCAppCode/Doses-ETL/dosedefinistion.txt`
- `BCAppCode/Doses-ETL/controlaudtdose.txt`
- `BCAppCode/Doses-ETL/update_dose_taskconfig_pyspark.py`
- `BCAppCode/Doses-ETL/Dose_DoseExcuse_Unit_Testing_Guide.md`

The latest requirement was to remove the `sl_` prefix from Silver Dose tables because Silver is now the final serving layer.

Final Silver table names are now:

```text
bhg_silver.pats.tbl_dose
bhg_silver.pats.tbl_dose_excuse
```

Not:

```text
bhg_silver.pats.sl_tbl_dose
bhg_silver.pats.sl_tbl_dose_excuse
```

## Most Recent Doses Changes

### Files Updated

- `BCAppCode/Doses-ETL/dosedefinistion.txt`
- `BCAppCode/Doses-ETL/update_dose_taskconfig_pyspark.py`
- `BCAppCode/Doses-ETL/dosescontrolandaudittables.txt`
- `BCAppCode/Doses-ETL/Dose_DoseExcuse_Unit_Testing_Guide.md`
- `BCAppCode/Doses-ETL/Dose_DoseExcuse_Parity_Fix_Findings.md`
- `BCAppCode/Documentation/Dose_Workflow_Documentation.md`
- `BCAppCode/Doses-ETL/allcolumndatatypedoses.txt`

### What Changed

The active Dose pipeline definition now uses final Silver table names:

```text
tbl_dose
tbl_dose_excuse
```

The inactive Gold copy activities were also updated so that, if enabled later, their Lakehouse source tables point to:

```text
pats.tbl_dose
pats.tbl_dose_excuse
```

The two Silver notebooks now resolve table names dynamically from `p_taskconfig_json`:

- `nb_doses_brnz_to_slv`
- `nb_dose_excuse_brnz_to_slv`

They use:

- Bronze table from ConfigId `7`
- Silver table from ConfigId `8`

Defaults are still present in the notebook code, but only for manual notebook testing. In real pipeline runs, table names come from taskconfig.

Defaults:

```python
default_bronze_table = "bhg_bronze.Dose.br_tblDose"
default_silver_table = "bhg_silver.pats.tbl_dose"

default_bronze_table = "bhg_bronze.Dose.br_tblDoseExcuse"
default_silver_table = "bhg_silver.pats.tbl_dose_excuse"
```

If `p_taskconfig_json` has rows but the required Method/ConfigId row is missing, the notebook raises an error instead of silently using defaults.

### TaskConfig Updater

Run the full file:

```text
BCAppCode/Doses-ETL/update_dose_taskconfig_pyspark.py
```

It is safe to run repeatedly. It uses Delta `MERGE`/updates and does not duplicate rows.

It updates:

- Bronze ConfigId `7`
- Silver ConfigId `8`
- Gold ConfigId `9`

Important expected Silver rows after running:

```text
ConfigId = 8, Method = Dose
TargetSchema = bhg_silver.pats
TargetTable = tbl_dose
RequestBody.full_table = bhg_silver.pats.tbl_dose
DQ keys = ["SiteCode","DoseId"]

ConfigId = 8, Method = DoseExcuse
TargetSchema = bhg_silver.pats
TargetTable = tbl_dose_excuse
RequestBody.full_table = bhg_silver.pats.tbl_dose_excuse
DQ keys = ["SiteCode","ExId"]
```

Suggested validation query:

```sql
SELECT
    TaskConfigId,
    ConfigId,
    Method,
    SourceTable,
    TargetSchema,
    TargetTable,
    RequestBody
FROM meta.taskconfig
WHERE ConfigId IN (7, 8, 9)
  AND Method IN ('Dose', 'DoseExcuse')
ORDER BY ConfigId, Method, TaskConfigId;
```

## Dose Logic Status

Dose and DoseExcuse are considered aligned with legacy logic based on the analysis done.

### DoseExcuse

DoseExcuse source extraction is effectively full table per selected site.

Business key:

```text
SiteCode + ExId
```

Silver RowState behavior:

- Existing rows for successful sites are reset to `RowState = false`
- Rows returned from current Bronze are merged and set active
- Missing old rows may remain inactive

### Dose

Dose has special source filtering aligned to legacy logic.

Business key:

```text
SiteCode + DoseId
```

Dose source window logic:

- `CltId IS NOT NULL`
- `DtDate <= WorkDate + 2 days`
- special sites `V10A`, `CBCO`, `V21`, `V10` use `DtDate >= WorkDate - 1 month`
- all other sites use `DtDate >= WorkDate - 6 months`
- year guard uses `WorkDate - LookbackDays - 1 year`

Dose inactive logic:

```text
BlVoid = true AND DtVoid = true -> RowState = false
CltId < 0 AND CltId <> -111    -> RowState = false
```

Fabric inactive rows were validated and explained by `BlVoid = 1 AND DtVoid = 1`.

## Dose Testing Guide

Created:

```text
BCAppCode/Doses-ETL/Dose_DoseExcuse_Unit_Testing_Guide.md
```

It contains tester-ready SQL for:

- BHG_DR schema comparison
- Fabric Silver schema comparison
- column count validation
- DoseExcuse counts
- Dose window counts
- inactive RowState analysis
- duplicate key checks
- audit validation

Current final Fabric Silver tables in that guide:

```text
bhg_silver.pats.tbl_dose
bhg_silver.pats.tbl_dose_excuse
```

## Important Validation Learnings

For Dose, do not compare full BHG_DR total counts directly to Fabric Silver counts.

Reason:

- BHG_DR contains historical rows from legacy upserts over years.
- Fabric Silver currently reflects the current rebuilt/processed window.
- Correct validation is source/window-based, not full historical table count.

For DoseExcuse:

- Active counts should match or be close.
- Total counts can differ if BHG_DR contains historical inactive rows not present in rebuilt Fabric.

## Notes ETL Status

Main files:

- `BCAppCode/BHG-DR-LIB/Save3pElig-Documentation/notesdefinetion.txt`
- `BCAppCode/BHG-DR-LIB/Save3pElig-Documentation/nb_notes_control_audit_writer.md`

Notes includes two methods:

- `3pArnote`
- `3pClaimNote`

Important decisions:

- Silver is final for Notes.
- Gold activities may exist but can be inactive/optional.
- Silver table names are final legacy-style names, not `sl_` names.
- Audit is centralized for both methods.
- Site-level Bronze behavior was implemented so one failed site does not block successful sites.
- Silver skips only failed Bronze sites and processes successful sites.

Important fix already handled:

- New Fabric `Invoke Pipeline` return shape differs from legacy invoke. Parent variable extraction had to read `returnValue` correctly.

## FormQuestionAnswers Status

Main files:

- `BCAppCode/SaveFormQADocumentation/formquestionanswerdefinition.txt`
- `BCAppCode/SaveFormQADocumentation/nb_formqa_control_audit_writer_complete.md`
- `BCAppCode/SaveFormQADocumentation/nb_centralized_audit.txt`

Important decisions:

- Silver is final.
- Final Silver table:

```text
bhg_silver.pats.tbl_dbo_FormQuestionAnswers
```

- Bronze schema consolidated under:

```text
bhg_bronze.Forms
```

- Bronze table:

```text
bhg_bronze.Forms.br_tblFormQA
```

- Bronze success marker:

```text
bhg_bronze.Forms.br_formqa_site_success
```

- Site-level Bronze handling was implemented/tested.
- If one site fails in Bronze, other sites continue to Silver.
- Gold copy activities were added as optional direct Silver-to-Gold copy, but Silver remains final.

Taskconfig request body for Bronze should use:

```json
{
  "full_table": "bhg_bronze.Forms.br_tblFormQA",
  "ingest_column": "_ingest_run_id",
  "site_column": "SiteCode",
  "database_column": "_source_database",
  "dq_keys": ["SiteCode","FormName","FormId","ClientId","PreAdmissionId","QuestionId","QuestionOrderId"]
}
```

## FormAnswerSignatures Status

Main files:

- `BCAppCode/SaveFormQADocumentation/formanswersignaturedefination.txt`
- `BCAppCode/SaveFormQADocumentation/nb_formanswersig_control_audit_writer_complete.md`

Important decisions:

- Silver is final.
- Final Silver table:

```text
bhg_silver.pats.tbl_dbo_FormAnswerSignatures
```

- Gold destination table is singular:

```text
pats.tbl_dbo_FormAnswerSignature
```

Not plural.

- Bronze schema consolidated under:

```text
bhg_bronze.Forms
```

- Bronze table:

```text
bhg_bronze.Forms.br_tblFormAnswerSig
```

- Bronze success marker:

```text
bhg_bronze.Forms.br_answersig_site_success
```

Site-level failure was tested by intentionally breaking AHK database name and activating another site.

## DartsSrv Status

Main files:

- `BCAppCode/SaveDartsSrvDocumentation/dartdefintion.txt`
- `BCAppCode/SaveDartsSrvDocumentation/dartcustomloaddefintion.txt`

Important decisions:

- Darts uses active sites from site/taskconfig.
- Bronze append behavior was clarified.
- Silver merge/upsert uses `SiteCode + DsId`.
- RowChkSum logic was aligned to legacy.
- Gold versioning was simplified in later changes.
- Custom load uses taskconfig/custom-load audit pattern.

Important validation lesson:

- Validating Fabric against BHG_DR can mislead because BHG_DR has historical upsert state.
- Better validation is against SAMMS source using the same lookback/window/filter logic.

## P1 Reference / P1 Forms Status

Main files:

- `BCAppCode/P1-Implmentation/P1-reference/pl_p1_reference.txt`
- `BCAppCode/P1-Implmentation/P1-reference/nb_p1_reference_optional_gold_publish.md`
- `BCAppCode/P1-Implmentation/P1-Forms/pl_p1_forms.txt`

Important decisions:

- Optional Gold should be driven by taskconfig/etlconfig, not manual pipeline edits.
- If Gold taskconfig rows are inactive, Gold notebook/activity should skip or be guarded by an If Condition.
- For many methods, one optional Gold notebook can process only active Gold methods by filtering taskconfig.
- P1 Reference optional Gold pattern was documented and applied as a reusable idea.

## Audit Framework Current Understanding

Primary reusable notebook patterns:

- `nb_notes_control_audit_writer.md`
- `controlaudtdose.txt`
- `nb_centralized_audit.txt`

Audit tracks:

- `meta.pipelinerun`
- `meta.taskqueue`
- `meta.taskaudit`
- `meta.dataquality`

Important behavior:

- Bronze logs site-level rows.
- Silver logs method/table-level rows.
- Data quality reads tables from `RequestBody.full_table`.
- DQ keys come from `RequestBody.dq_keys`.
- `PipelineRunId` is now expected to be populated where supported.

Important note:

- If a layer fails after a previous layer succeeded, use failed target/stage correctly so successful previous layer audit rows are still finalized.

## General Naming Direction

Where Silver is final, use final table names instead of `sl_` prefix.

Examples:

```text
tbl_dose
tbl_dose_excuse
tbl_dbo_FormQuestionAnswers
tbl_dbo_FormAnswerSignatures
tbl_3pARNOTE
tbl_3pClaimNote
```

Bronze should use PascalCase business columns where possible, with metadata columns aligned to each module's convention.

## Next Things To Watch

1. After running Doses taskconfig updater, verify ConfigId `8` rows point to `tbl_dose` and `tbl_dose_excuse`.
2. If old Silver tables exist, decide whether to delete/ignore:

```text
bhg_silver.pats.sl_tbl_dose
bhg_silver.pats.sl_tbl_dose_excuse
```

3. If Gold is enabled later for Dose, verify Gold source reads from final Silver names.
4. Re-run Doses pipeline after deleting/recreating final Silver tables if needed.
5. Re-run tester queries from `Dose_DoseExcuse_Unit_Testing_Guide.md`.

## Useful Quick Checks

Taskconfig check:

```sql
SELECT
    TaskConfigId,
    ConfigId,
    Method,
    SourceTable,
    TargetSchema,
    TargetTable,
    RequestBody
FROM meta.taskconfig
WHERE ConfigId IN (7, 8, 9)
  AND Method IN ('Dose', 'DoseExcuse')
ORDER BY ConfigId, Method, TaskConfigId;
```

Fabric Dose final table count:

```sql
SELECT
    SiteCode,
    COUNT(*) AS fabric_count,
    COUNT(DISTINCT DoseId) AS fabric_distinct_doseid,
    SUM(CASE WHEN RowState = 1 THEN 1 ELSE 0 END) AS fabric_active_count,
    SUM(CASE WHEN RowState = 0 THEN 1 ELSE 0 END) AS fabric_inactive_count
FROM bhg_silver.pats.tbl_dose
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode
ORDER BY SiteCode;
```

Fabric DoseExcuse final table count:

```sql
SELECT
    SiteCode,
    COUNT(*) AS fabric_count,
    COUNT(DISTINCT ExId) AS fabric_distinct_exid,
    SUM(CASE WHEN RowState = 1 THEN 1 ELSE 0 END) AS fabric_active_count,
    SUM(CASE WHEN RowState = 0 THEN 1 ELSE 0 END) AS fabric_inactive_count
FROM bhg_silver.pats.tbl_dose_excuse
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode
ORDER BY SiteCode;
```

