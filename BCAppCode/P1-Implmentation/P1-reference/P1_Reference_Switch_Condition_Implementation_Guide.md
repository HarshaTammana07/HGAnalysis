# P1 Reference Switch Condition Implementation Guide

## Purpose

The current P1 Reference child pipeline has one `Filter + ForEach` branch per method/table. That works, but for 9 methods it makes the child pipeline wide and hard to maintain.

The optimized design is to use:

```text
One ForEach over active site/method taskconfig rows
    -> One Switch on item().Method
        -> Existing method-specific lookup/copy logic inside each case
```

This reduces visual clutter without losing the existing method-specific copy SQL, mappings, or site-level behavior.

## Current Pattern

Current child pipeline shape:

```text
flt_child_clinic_sites              -> fe_each_samms_site_clinic
flt_child_3p_setup_sites            -> fe_each_samms_site_3p_setup
flt_child_codes_sites               -> fe_each_samms_site_codes
flt_child_services_sites            -> fe_each_samms_site_services
flt_child_dropdown_list_items_sites -> fe_each_samms_site_dropdown_list_items
flt_child_custom_answers_sites      -> fe_each_samms_site_custom_answers
flt_child_custom_questions_sites    -> fe_each_samms_site_custom_questions
flt_child_pre_admission_v6_sites    -> fe_each_samms_site_pre_admission_v6
flt_child_preadmission_referral_source_sites -> fe_each_samms_site_preadmission_referral_source
```

Each branch filters `p_sites` for one method and runs the same method-specific Bronze copy logic.

## Proposed Switch Pattern

New child pipeline shape:

```text
fe_each_p1_reference_site_method
    -> sw_p1_reference_method
        Case SaveClinic
        Case Save3pSetup
        Case SaveCodes
        Case SaveServices
        Case SavedropDownListItems
        Case SaveCustomAnswers
        Case SaveCustomQuestions
        Case SavePreAdmissionV6
        Case SavePreAdminReferrals
    -> set_child_bronze_method_results
```

`fe_each_p1_reference_site_method` loops over:

```text
@pipeline().parameters.p_sites
```

The Switch expression is:

```text
@item().Method
```

Each Switch case contains the same lookup/copy activities that currently live inside that method's old `ForEach`.

## Important Design Guardrail

Do not simply replace 9 branches with one Switch and keep the old final Set Variable logic.

Why:

The old final `set_child_bronze_method_results` checks 9 separate ForEach activity statuses. After converting to one ForEach, those 9 activity names no longer exist. If we only check the single new ForEach status, one failed site could incorrectly mark all methods as failed, or a partial site failure could be hidden.

To preserve behavior, the child must still return method-level JSON like:

```json
{
  "SaveClinic": {
    "status": "SUCCESS",
    "failed_stage": "",
    "error_message": null,
    "site_results": []
  },
  "Save3pSetup": {
    "status": "FAILED",
    "failed_stage": "BR",
    "error_message": "Bronze site failed",
    "site_results": []
  }
}
```

## Recommended Result-Building Options

### Option 1: Keep Current Marker/Result Pattern

Use the existing method-specific status handling or marker logic to build `v_bronze_method_results_json`.

This is safest if zero-row successful sites must be treated as success.

Required changes:

- Replace 9 child filters with one ForEach + Switch.
- Keep or adapt existing site success marker/result logic.
- Update final `set_child_bronze_method_results` to read method-level status from that result logic.

### Option 2: Infer Site Success From Bronze Rows

Use each method's Bronze table and current `IngestRunId`:

```text
SELECT DISTINCT SiteCode
FROM BronzeTable
WHERE IngestRunId = current run
```

If a site wrote rows, treat it as Bronze success. If a site did not write rows and the ForEach had a failure, treat it as failed.

This removes extra marker activities/tables, but has one caveat:

If a site succeeds and legitimately returns 0 rows, Bronze rows cannot prove success. That site may be treated as failed or missing unless we add a separate success signal.

Required changes:

- Child pipeline: remove marker copy activities.
- Audit notebook: replace marker-table reads with Bronze-table row checks.
- Silver notebook: process only sites present in the current-run Bronze rows.
- Taskconfig: ensure every Bronze task has RequestBody metadata:

```json
{
  "full_table": "bhg_bronze.P1Reference.br_samms_pre_admission_v6",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["SiteCode", "PreAdmissionid", "Clientid"]
}
```

## Required Pipeline Changes

### Child Pipeline

File:

```text
pl_p1_reference.txt
```

Changes:

- Remove or disable the 9 method-level `Filter` activities.
- Remove or disable the 9 method-level `ForEach` wrappers.
- Add one new ForEach:

```text
fe_each_p1_reference_site_method
```

- Add one Switch inside that ForEach:

```text
sw_p1_reference_method
```

- Switch cases should be:

```text
SaveClinic
Save3pSetup
SaveCodes
SaveServices
SavedropDownListItems
SaveCustomAnswers
SaveCustomQuestions
SavePreAdmissionV6
SavePreAdminReferrals
```

- Move each method's existing lookup/copy activities into the matching Switch case.
- Keep all existing source queries, target tables, mappings, and optional-column checks unchanged.

### Parent Pipeline

Minimal changes expected.

The parent can continue passing:

```text
p_sites
p_ingest_run_id
p_work_date
p_lookback_days
```

The parent should not need a structural redesign if the child still returns:

```text
v_bronze_method_results_json
```

### Audit Notebook

File:

```text
nb_audit_reference
```

Changes depend on the selected result-building option:

- If using marker/result logic, keep current audit logic mostly as-is.
- If inferring success from Bronze rows, audit must read Bronze tables from `taskconfig.RequestBody.full_table` instead of reading a marker table.

The audit notebook must still support:

```text
START_LAYER_RUNS
FINALIZE_SUCCESS
FINALIZE_FAILURE
```

### Silver Notebooks

If marker tables are removed, Silver notebooks should use Bronze rows for the current run to identify successful sites.

Silver should not process sites that failed Bronze.

### Taskconfig

No structural table changes are required.

But each active Bronze row must have correct metadata in `RequestBody`:

```json
{
  "full_table": "bhg_bronze.P1Reference.<bronze_table>",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["business", "key", "columns"]
}
```

## Failure Behavior To Preserve

Required behavior after optimization:

- If one site fails in Bronze, other sites still continue.
- If one method fails, other methods still continue.
- Silver processes only Bronze-success sites.
- Failed Bronze site is reflected in audit as failed or skipped for downstream layer.
- Final parent pipeline status should fail if any required method/site failed.
- Notification should contain the real failed method/site message, not a giant full JSON blob.

## Batch Count Guidance

Old design had multiple method ForEach activities, commonly with `batchCount = 3`.

With one combined ForEach, choose carefully:

```text
3 to 5  = closest to old behavior, slower but safer
10      = balanced
20      = faster, more source/Fabric load
50+     = not recommended unless source DB can handle it
```

For first test, use `batchCount = 3` or `5`.

## Testing Plan

1. Run with 5 active sites and all 9 methods.
2. Confirm Bronze rows load per method/site.
3. Intentionally break one site's `DataBaseName` for one method.
4. Confirm other sites/methods still load.
5. Confirm `v_bronze_method_results_json` identifies only the failed method/site.
6. Confirm Silver skips only the failed Bronze site.
7. Confirm audit tables show:

```text
Successful Bronze sites = SUCCESS
Failed Bronze site = FAILED
Silver for failed site = SKIPPED or not processed
Pipeline final status = FAILED when any method/site failed
```

## Recommendation

Use the Switch pattern only after deciding how zero-row successful sites should be handled.

If zero-row success must be tracked perfectly, keep a success signal outside Bronze data rows.

If zero-row success is acceptable as failed/missing for audit purposes, remove marker activities and infer success from Bronze rows for a simpler design.
