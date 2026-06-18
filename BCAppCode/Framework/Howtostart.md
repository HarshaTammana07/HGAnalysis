# How To Start A New Fabric ETL

This guide is the starting checklist for migrating one legacy C# ETL table into the Fabric Bronze/Silver/Gold pattern.

Use it before writing JSON, notebook code, control rows, or validation SQL. The main rule is simple: first understand the old C# behavior, then copy that behavior into Fabric with the current reusable framework.

## 1) Find The Legacy ETL

Start from the old runner:

- `BCAppCode/BHGTaskRunner/updatedProgram.cs`
- `BCAppCode/BHG-DR-LIB/`
- `BCAppCode/ControlTables/vw_mapAction.csv`
- `BCAppCode/ControlTables/vw_MapSrc2Dsn.csv`

For the table you are migrating, identify:

- Legacy task name, for example `pats.tbl_dartssrv`.
- Source database/table/view, for example `dbo.tblDartsSrv` in each SAMMS site database.
- Legacy destination table in `BHG_DR`, for example `pats.tbl_DartsSrv` or `pats.tbl_dbo_FormQuestionAnswers`.
- Legacy save path:
  - EF/save method, for example `SaveFormQuestionAnswers`.
  - Bulk/stored procedure path, for example `BulkDartsSrvLoader` plus `stg.DartsSrvMerge*`.
- Business key used for insert/update matching.
- Lookback window and any special date logic.
- Any source filters, optional columns, row-state rules, or form-specific rules.

Do not start with Fabric code first. The old C# tells us what the Fabric notebooks must preserve.

## 2) Check The Source And Destination Schema

Before building anything, compare column shape.

Check source:

```sql
select
    c.column_id,
    c.name,
    t.name as data_type,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable
from [SourceDatabase].sys.columns c
join [SourceDatabase].sys.tables tb
    on c.object_id = tb.object_id
join [SourceDatabase].sys.schemas s
    on tb.schema_id = s.schema_id
join [SourceDatabase].sys.types t
    on c.user_type_id = t.user_type_id
where s.name = 'dbo'
  and tb.name = '<source table>'
order by c.column_id;
```

Check destination/reference:

```sql
select
    ordinal_position,
    column_name,
    data_type,
    character_maximum_length,
    numeric_precision,
    numeric_scale,
    is_nullable
from information_schema.columns
where table_schema = '<schema>'
  and table_name = '<destination table>'
order by ordinal_position;
```

For Fabric, add only intentional framework or query-helper columns. Document every extra column.

Current examples:

- DartsSrv adds `DsDtStartYear`, derived from `DsDtStart`, so users can query by service year after we combine the old year-split legacy tables into one Gold table.
- Bronze adds technical metadata such as `_site_code`, `_source_database`, `_ingest_run_id`, `_extracted_at`, `_source_query_start_date`, and `_source_query_end_date`.
- Silver adds technical metadata such as `silver_created_at`, `silver_updated_at`, and `last_seen_at`.

## 3) Read The Control Mapping Tables

Use the control CSVs as the legacy source of truth:

- `vw_mapAction.csv` tells you action/step, source object, destination object, where condition, sort order, row tracking, connection, and active site behavior.
- `vw_MapSrc2Dsn.csv` tells you field mapping, primary keys, enabled columns, destination field names, field types, and checksum participation.

For checksum-based modules, also review:

- `BCAppCode/BHG-DR-LIB/SelectConstructor.cs`

Legacy checksum behavior is based on enabled mapped fields, excluding unsupported field types like `ntext`, `varbinary`, and `timestamp`. If Fabric computes `RowChkSum`, the expression must be built from the same business columns as legacy, not from every physical column.

## 4) Capture The Business Logic

Create a small note for each module before coding.

Required items:

- Business key.
- Insert rule.
- Update rule.
- Delete/inactive/reset rule.
- Lookback/date filter.
- Optional column behavior.
- Whether source is one table or generated from many form/config rows.
- Whether legacy uses EF or bulk/stored procedures.
- Whether validation should compare against SAMMS source or `BHG_DR`.

Important: `BHG_DR` is useful for destination shape and legacy behavior, but it is not the best primary count-validation source. It is a downstream system that may have been updated at a different time, with previous upserts retained from prior runs. For row-count validation, compare Fabric against the live SAMMS source using the same site, same lookback date, and same source filter.

## 5) Choose The Fabric Load Pattern

Current standard pattern:

1. Bronze:
   - Append source rows.
   - One active Bronze task per active site/database.
   - Include source metadata columns.
   - Use optional-column lookup when older site databases do not have the same schema.

2. Silver:
   - Read only current `_ingest_run_id` from Bronze.
   - Normalize names and types.
   - Deduplicate by business key.
   - Merge into the Silver Delta table.
   - Preserve old business update behavior where required.

3. Gold:
   - Gold is a Warehouse table.
   - Copy Silver to a Gold load table.
   - Publish using the versioned table pattern:
     - Create `*_load`.
     - Copy into `*_load`.
     - Create a version table from the load table.
     - Validate the version table.
     - Rename current production table to backup.
     - Rename version table to production.

Do not truncate production Gold directly for full-refresh Gold tables. Use the versioned publish/swap pattern so reporting keeps the previous production table until the new table is validated.

## 6) Add Control Rows

For each ETL, create three `meta.etlconfig` rows:

- Bronze row with `TargetName = BR`.
- Silver row with `TargetName = SL`.
- Gold row with `TargetName = GL`.

Then create `meta.taskconfig` rows:

- Bronze: one active row per active site/database.
- Silver: one active layer-level row.
- Gold: one active layer-level row.

Bronze task rows must include:

- `ConfigId`
- `TaskConfigId`
- `TaskName`
- `SourceTable`
- `TargetTable`
- `SiteCode`
- `DataBaseName`
- `SiteName`
- `LookbackDays` when the pipeline is designed to read lookback from control tables
- `IsActive = 1`

Silver and Gold rows can leave site fields null unless the module needs site-level layer tasks.

Reference the current framework table guide:

- `BCAppCode/Framework/controlAudittables.txt`

Current module config ranges:

| Module | Bronze ConfigId | Silver ConfigId | Gold ConfigId |
| --- | ---: | ---: | ---: |
| DartsSrv | 25 | 26 | 27 |
| FormQuestionAnswers | 28 | 29 | 30 |
| FormAnswerSignatures | 31 | 32 | 33 |

## 7) Add Audit Flow

The current control/audit framework writes to:

- `meta.pipelinerun`
- `meta.taskqueue`
- `meta.taskaudit`
- `meta.dataquality`

`meta.siteaudit` is deprecated for these current ETLs. Per-site Bronze counts are written into `meta.taskaudit` using `SiteCode`, `DataBaseName`, and `SiteName`.

The audit notebook pattern is:

1. Start mode:
   - `START_LAYER_RUNS`
   - Reads active `etlconfig` and `taskconfig`.
   - Creates one `pipelinerun` per layer.
   - Creates one `taskqueue` row per task.
   - Returns `p_audit_context_json` to the parent pipeline.

2. Success finalizer:
   - Module-specific mode such as `FINALIZE_DARTS_SUCCESS`, `FINALIZE_FORMQA_SUCCESS`, or `FINALIZE_FORMANSWERSIG_SUCCESS`.
   - Updates `taskqueue` to `SUCCESS`.
   - Updates `pipelinerun` to `SUCCESS`.
   - Appends `taskaudit` rows.
   - Appends `dataquality` rows.

3. Failure finalizer:
   - Module-specific mode such as `FINALIZE_DARTS_FAILURE`, `FINALIZE_FORMQA_FAILURE`, or `FINALIZE_FORMANSWERSIG_FAILURE`.
   - Updates failed/upstream task status.
   - Stores available pipeline error message.
   - Appends failure audit rows.
   - Raises an exception so the Fabric pipeline remains failed.

Per-site Bronze row-count logic is based on current-run Bronze rows:

```sql
select
    _site_code as SiteCode,
    _source_database as DataBaseName,
    count(*) as RowsCopied
from <bronze table>
where _ingest_run_id = '<current ingest run id>'
group by _site_code, _source_database;
```

This is then joined back to `taskqueue`/`taskconfig` by `SiteCode + DataBaseName`.

## 8) Build The Parent Pipeline

The current parent pattern should include:

1. Lookup active Bronze site tasks from `taskconfig`.
2. Audit start notebook.
3. Invoke/execute Bronze child pipeline for each active site.
4. Silver notebook.
5. Prepare Gold load table script activity.
6. Copy Silver to Gold load table.
7. Publish/version Gold script activity.
8. Audit success notebook.
9. Failure path that sets error variables and calls the audit failure notebook.

The parent should pass:

- `p_ingest_run_id`
- `p_lookback_days` or a control-table-derived lookback value
- active site list
- `p_audit_context_json`
- failure stage/error values for failure finalization

## 9) Build The Child/Bronze Pipeline

The child pipeline should:

- Receive one site/database object from the parent.
- Check optional columns if the source schema differs by site.
- Build the source SQL dynamically from the site database.
- Append to the Bronze table.
- Add source metadata columns.

Example Darts optional columns:

- `ServiceType`
- `dsTelehealthSession`
- `HoldId`
- `upsize_ts`

When an optional source column does not exist, emit a typed `NULL` with the expected alias.

## 10) Validate Correctly

Validate Fabric against the source system first.

For DartsSrv, use the same source filter as the pipeline:

```sql
declare @StartDate date = '<lookback start date>';

select
    '<SiteCode>' as SiteCode,
    count(*) as samms_count,
    count(distinct dsID) as samms_distinct_dsid,
    min(dsDtStart) as min_dsdtstart,
    max(dsDtStart) as max_dsdtstart
from [<SAMMS database>].dbo.tblDartsSrv
where dsClt is not null
  and (
        convert(date, dsDtStart) >= @StartDate
     or convert(date, dsDtAdded) >= @StartDate
     or convert(date, dsUpdate) >= @StartDate
     or convert(date, dsBilled) >= @StartDate
     or convert(date, dsSigDate) >= @StartDate
     or dsClt <= 0
  );
```

Then compare Fabric Gold using the same site and same date logic:

```sql
declare @StartDate date = '<lookback start date>';

select
    SiteCode,
    count(*) as fabric_count,
    count(distinct DsId) as fabric_distinct_dsid,
    min(DsDtStart) as min_dsdtstart,
    max(DsDtStart) as max_dsdtstart
from [bhg_gold].[pats].[gd_darts_srv]
where SiteCode = '<SiteCode>'
  and DsClt is not null
  and (
        convert(date, DsDtStart) >= @StartDate
     or convert(date, DsDtAdded) >= @StartDate
     or convert(date, DsUpdate) >= @StartDate
     or convert(date, DsBilled) >= @StartDate
     or convert(date, DsSigDate) >= @StartDate
     or DsClt <= 0
  )
group by SiteCode;
```

If Fabric count differs from `BHG_DR` but matches SAMMS with the same filter, the Fabric pipeline is usually correct. The mismatch is commonly caused by different run time, source data changing during the day, or `BHG_DR` containing older upsert history from previous runs.

## Current Module Patterns

### DartsSrv

Reference docs:

- `BCAppCode/SaveDartsSrvDocumentation/dartdefintion.txt`
- `BCAppCode/SaveDartsSrvDocumentation/nb_control_audit_writer_complete.md`
- `BCAppCode/SaveDartsSrvDocumentation/DartsSrv_Gold_vs_SAMMS_Validation_Guide.md`

Legacy:

- Runner case: `pats.tbl_dartssrv`.
- Source: `dbo.tblDartsSrv` in each SAMMS site database.
- Legacy load path: `BulkDartsSrvLoader` into `stg.tbl_dartssrv`, then `stg.DartsSrvMerge*`.
- Legacy/reference EF behavior: match by `SiteCode + DsId`; business update when `RowChkSum` changes.
- Default lookback in old code was 15 days, with special legacy exceptions in `updatedProgram.cs`.

Fabric:

- Bronze: `bhg_bronze.Dart.br_tblDartSrv`.
- Silver: `bhg_silver.pats.sl_tbldartsrv`.
- Gold load: `pats.gd_darts_srv_load`.
- Gold production: `pats.gd_darts_srv`.
- Business key: `SiteCode + DsId`.
- Change detection: `RowChkSum`.
- Added helper column: `DsDtStartYear = year(DsDtStart)`.
- Gold publish: versioned table swap.
- Audit configs: 25/26/27.

### FormQuestionAnswers

Reference docs:

- `BCAppCode/SaveFormQADocumentation/formquestionanswerdefinition.txt`
- `BCAppCode/SaveFormQADocumentation/nb_formqa_control_audit_writer_complete.md`

Legacy:

- Save method: `SaveFormQuestionAnswers` in `SaveFormQAData.cs`.
- Uses enabled `Forms2Process` rows.
- Key in legacy model: `SiteCode + FormName + FormId + ClientId + QuestionId + QuestionOrderId`.
- Handles row-state reset/inactive behavior based on form config and date filters.

Fabric:

- Bronze: `bhg_bronze.Form.br_tblFormQA`.
- Silver: `bhg_silver.pats.sl_tblFormQuestionAnswers`.
- Gold load: `pats.tbl_dbo_FormQuestionAnswers_load`.
- Gold production: `pats.tbl_dbo_FormQuestionAnswers`.
- Uses `bhg_silver.ctrl.Forms2Process`.
- Silver merge performs full refresh/update for matched rows from current source output.
- Gold publish: versioned table swap.
- Audit configs: 28/29/30.

### FormAnswerSignatures

Reference docs:

- `BCAppCode/SaveFormQADocumentation/formanswersignaturedefination.txt`
- `BCAppCode/SaveFormQADocumentation/nb_formanswersig_control_audit_writer_complete.md`

Legacy:

- Save method is in `SaveFormQAData.cs`.
- Uses enabled `Forms2Process` rows.
- Key in legacy model: `SiteCode + FormName + FormId + ClientId`.
- Includes row-state and stale-row handling, including the Periodic Reassessment style reset behavior.

Fabric:

- Bronze: `bhg_bronze.Forms.br_tblFormAnswerSig`.
- Silver: `bhg_silver.pats.sl_tblFormAnswerSignatures`.
- Gold load: `pats.tbl_dbo_FormAnswerSignature_load`.
- Gold production: `pats.tbl_dbo_FormAnswerSignature`.
- Uses `bhg_silver.ctrl.Forms2Process`.
- Silver merge performs full refresh/update for matched rows from current source output.
- Gold publish: versioned table swap.
- Audit configs: 31/32/33.

## Final Pre-Build Checklist

Before saying an ETL is ready to test, confirm:

- Legacy source object is known.
- Legacy destination table is known.
- Legacy C# method/path is reviewed.
- Business key is documented.
- Lookback and source filter match legacy.
- Optional source columns are handled.
- Row checksum logic matches legacy where applicable.
- Bronze, Silver, Gold table schemas are created.
- `etlconfig` rows are inserted for BR/SL/GL.
- `taskconfig` rows are inserted for per-site Bronze and layer-level Silver/Gold.
- Parent pipeline passes the same parameters the notebooks expect.
- Audit start and success/failure finalizers are wired.
- Gold uses versioned publish/swap for overwrite/full-refresh scenarios.
- Validation SQL compares Fabric to SAMMS source using the same filter.
- Any difference from legacy behavior is documented clearly.
