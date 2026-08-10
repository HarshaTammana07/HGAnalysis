# P1 Forms Gold Watermark Merge Implementation Guide

## Purpose

P1 Forms currently treats Silver as the final audited layer and has an optional Gold publish step. The current Gold publish notebook performs full overwrite from Silver to Gold for active Gold taskconfig rows.

The proposed production approach is:

```text
First Gold run      -> full overwrite from Silver to Gold
Subsequent Gold run -> incremental MERGE from Silver to Gold using LastModAt watermark
```

This avoids rewriting full Gold tables every run while still allowing the client to activate/deactivate Gold at the taskconfig level.

## Current Gold Flow

Current parent flow:

```text
BR child
  -> SL child
  -> if_all_forms_methods_success
        True:
          flt_active_p1_forms_gold
          -> nb_p1_forms_optional_gold_publish
          -> nb_p1_forms_audit_finalize_success
        False:
          nb_p1_forms_audit_finalize_failure
```

Current Gold behavior:

```text
Read active Gold taskconfig rows where ConfigId = 99 and IsActive = 1
Read full Silver table from SourceTable
Overwrite Gold Warehouse table using TargetSchema + TargetTable
Validate source count = Gold count
```

Current Gold does not:

- store a watermark
- filter by `LastModAt`
- MERGE/upsert
- compare `RowChkSum`
- write separate Gold audit rows

## Proposed Gold Flow

For each active Gold taskconfig row:

```text
Read Gold taskconfig row
Parse RequestBody
Get source Silver table
Get target Gold table
Get watermark column, usually LastModAt
Get saved watermark value
Get merge keys
Get optional RowChkSum column

If first run / no watermark / Gold table missing:
    full overwrite Silver -> Gold
    validate count
    update watermark to MAX(LastModAt)

Else:
    read Silver rows where LastModAt >= saved watermark
    MERGE changed rows into Gold using merge keys
    update rows only when RowChkSum differs, if RowChkSum is configured
    insert new rows when not matched
    validate
    update watermark to new MAX(LastModAt)
```

## Why Merge Keys Are Required

`LastModAt` only tells us which rows changed.

Merge keys tell us which existing Gold row to update.

Example:

```text
Silver changed row:
SiteCode = AHK
FormId = F123
ClientId = 100
LastModAt = 2026-07-27 10:00

Gold existing row:
SiteCode = AHK
FormId = F123
ClientId = 100
LastModAt = 2026-07-20 09:00
```

The notebook needs merge keys such as:

```json
["SiteCode", "FormId", "ClientId"]
```

Without keys, Gold cannot know whether to update an existing row or insert a duplicate.

## Role Of RowChkSum

If `RowChkSum` exists, use it to avoid unnecessary updates.

Pattern:

```sql
WHEN MATCHED
 AND tgt.RowChkSum <> src.RowChkSum
THEN UPDATE

WHEN NOT MATCHED
THEN INSERT
```

Column roles:

```text
LastModAt   -> selects candidate changed rows from Silver
Merge keys  -> match Silver rows to existing Gold rows
RowChkSum   -> decide whether matched row needs update
```

## Taskconfig Changes

Gold taskconfig rows are still under:

```text
ConfigId = 99
```

Recommended values:

```text
LoadType        = WATERMARK_MERGE
WatermarkColumn = LastModAt
IsActive        = 1 only when client wants Gold to run
```

RequestBody should include:

```json
{
  "source_table": "bhg_silver.pats.tbl_example",
  "target_table": "bhg_gold.pats.tbl_example",
  "watermark_column": "LastModAt",
  "watermark_value": null,
  "merge_keys": ["SiteCode", "BusinessId"],
  "checksum_column": "RowChkSum"
}
```

If the framework does not want full `target_table`, it can continue using:

```text
TargetSchema
TargetTable
```

Then RequestBody can be:

```json
{
  "source_table": "bhg_silver.pats.tbl_example",
  "watermark_column": "LastModAt",
  "watermark_value": null,
  "merge_keys": ["SiteCode", "BusinessId"],
  "checksum_column": "RowChkSum"
}
```

## Watermark Storage

No audit notebook change is required for watermark management.

The watermark should be updated by the Gold publish notebook because that notebook knows:

- which Gold task ran
- which Silver table was read
- whether Gold write/MERGE succeeded
- what the new `MAX(LastModAt)` is

Since there is no dedicated `WatermarkValue` column today, store the value in:

```text
taskconfig.RequestBody.watermark_value
```

Only update the watermark after:

```text
Gold write/MERGE succeeded
Validation succeeded
```

## Should Audit Notebook Change?

No, not for the current P1 Forms design.

Current audit scope is:

```text
BR -> SL
```

Gold is optional and controlled by taskconfig. It runs before audit finalize success, but Gold taskqueue/taskaudit/dataquality rows are not being written separately.

Audit notebook changes are needed only if we decide Gold must become an official audited layer:

```text
BR -> SL -> GL
```

That would require broader changes:

- include `GL` in active target layers
- create pipeline/taskqueue rows for GL
- finalize GL status
- write GL dataquality

For now, do not change audit notebook.

## Gold Notebook Changes

Notebook:

```text
nb_p1_reference_optional_gold_publish.md
```

or a Forms-specific copy:

```text
nb_p1_forms_optional_gold_publish
```

Required behavior:

1. Read `p_gold_tasks_json`.
2. For each active Gold task:
   - read `SourceTable` or `RequestBody.source_table`
   - read target from `TargetSchema + TargetTable`
   - parse `RequestBody.watermark_column`
   - parse `RequestBody.watermark_value`
   - parse `RequestBody.merge_keys`
   - parse `RequestBody.checksum_column`
3. Determine first run:
   - watermark is null/blank, or
   - Gold table does not exist, or
   - `LoadType` indicates full first load
4. First run:
   - full overwrite from Silver to Gold
   - set watermark to `MAX(LastModAt)`
5. Incremental run:
   - read changed Silver rows
   - MERGE into Gold
   - update only changed rows if checksum exists
   - insert new rows
   - update watermark
6. Return JSON result for pipeline debugging.

## Watermark Update Logic

After successful load:

```text
new_watermark = MAX(LastModAt) from rows loaded or from source Silver table
```

Then update the same taskconfig row:

```text
TaskConfigId = current Gold task TaskConfigId
```

Update `RequestBody.watermark_value`.

Important:

Use an overlap on next run to avoid timestamp precision issues:

```text
Silver WHERE LastModAt >= previous_watermark
```

Because MERGE uses keys and checksum, re-reading boundary rows is safe.

## Parent Pipeline Changes

Minimal.

Current parent flow can stay:

```text
flt_active_p1_forms_gold
  -> nb_p1_forms_optional_gold_publish
```

The filter still controls whether Gold runs:

```text
ConfigId = 99
IsActive = 1
```

If no Gold rows are active, preferred future design:

```text
If active Gold count > 0:
    run Gold notebook
Else:
    skip Gold notebook and finalize success
```

This avoids starting a Spark session when Gold is inactive.

## Validation

For first full run:

```text
Silver count = Gold count
Gold MAX(LastModAt) = stored watermark
```

For incremental run:

```text
Changed Silver rows count >= inserted + updated candidates
No duplicate merge keys in Gold
Gold MAX(LastModAt) >= previous watermark
Stored watermark updated only after success
```

## Risks And Decisions

Open decisions before implementation:

- Where exactly to store watermark if updating RequestBody JSON becomes hard.
- Whether to use a dedicated watermark table later.
- Whether Gold should remain outside audit or become official `GL` audit layer.
- Merge key list for each of the 9 P1 Forms tables.
- Whether every table has `RowChkSum`; if not, use merge keys only.

## Recommendation

Implement Gold incremental as:

```text
taskconfig-driven optional Gold notebook
first run overwrite
subsequent run watermark MERGE
watermark stored in taskconfig.RequestBody
audit notebook unchanged
```

This preserves the current P1 Forms BR/SL audit model and makes Gold efficient without adding extra pipeline clutter.
