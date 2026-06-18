# FormQuestionAnswers Control Audit Writer — Explanation

This document explains `nb_formqa_control_audit_writer` (see `nb_formqa_control_audit_writer_complete.md` for the notebook source). The same pattern applies to FormAnswerSignatures (`nb_formanswersig_control_audit_writer`) with different config IDs and table names.

---

## Purpose

The audit writer is a **multi-mode Spark notebook** that records ETL run metadata in Fabric control tables under `bhg_bronze.meta`. It does not move business data. It:

1. **Starts** audit rows before Bronze/Silver/Gold run (`START_LAYER_RUNS`)
2. **Finalizes** audit rows after the pipeline completes (`FINALIZE_FORMQA_SUCCESS` or `FINALIZE_FORMQA_FAILURE`)
3. Optionally supports mid-run modes (`WRITE_DATAQUALITY`, `FINISH_TASK_*`) — not wired in the parent pipeline today

### Control tables written

| Table | What it stores |
|-------|----------------|
| `meta.pipelinerun` | One row per layer (BR, SL, GL) per pipeline execution |
| `meta.taskqueue` | One row per task (117 Bronze site tasks + 1 Silver + 1 Gold) |
| `meta.taskaudit` | Completion records with row counts, status, site info |
| `meta.dataquality` | Silver/Gold row counts, null PK checks, duplicate PK checks |

### Design change from older pipelines

- **`siteaudit` is no longer written**
- Per-site Bronze results go directly into **`taskaudit`** with `SiteCode`, `DataBaseName`, and `SiteName`

---

## Where it runs in the pipeline

```
lkp taskconfig
    → flt active sites (ConfigId=28, IsActive=1)
    → nb_formqa_audit_start          ← START_LAYER_RUNS (your screenshot)
    → Invoke Bronze child pipeline
    → nb_forms_bronze_to_silver
    → Gold load + versioned publish
    → nb_formqa_audit_finalize_success   OR   nb_formqa_audit_finalize_failure
    → email notification
```

The JSON returned by the **start** notebook is passed to **finalize** as `p_audit_context_json`:

```
@activity('nb_formqa_audit_start').output.result.exitValue
```

---

## Pipeline parameters — audit start (screenshot)

When the pipeline calls the notebook at the beginning, it passes these base parameters:

| Parameter | FormQuestionAnswers value | What it does |
|-----------|---------------------------|--------------|
| `p_mode` | `START_LAYER_RUNS` | Routes Cell 2 to the start logic |
| `p_config_name_prefix` | `SAMMS FormQuestionAnswers` | Finds the 3 `etlconfig` rows (ConfigId 28/29/30) |
| `p_pipeline_name` | `Execute_Form_QuestionAnswer` | Passed for consistency; **not used in START code** (see below) |
| `p_pipeline_path` | `/pipelines/Execute_Form_QuestionAnswer` | Passed for consistency; **not used in START code** |
| `p_triggered_by` | `Fabric` | Fallback for `pipelinerun.TriggeredBy` if `etlconfig` is null |

For FormAnswerSignatures, the same five parameters are used with prefix `SAMMS FormAnswerSignatures` and pipeline `Execute_Pipeline_AnswerSignature`.

### Why `p_pipeline_name` / `p_pipeline_path` are passed but not used at start

In `START_LAYER_RUNS`, pipeline name and path are read from **`meta.etlconfig`**, not from these parameters:

```python
cfg["PipelineName"]   # e.g. pl_forms_samms_to_lakehouse for Bronze layer
cfg["PipelinePath"]   # e.g. /pipelines/pl_forms_samms_to_lakehouse
```

Each layer (BR/SL/GL) can have a **different** underlying pipeline/notebook in `etlconfig`. The parent orchestrator name (`Execute_Form_QuestionAnswer`) is not the same as the Bronze child pipeline name. Parameters are kept for documentation and possible future use.

---

## `START_LAYER_RUNS` — step by step

### 1. Load configuration

```sql
SELECT * FROM bhg_bronze.meta.etlconfig
WHERE IsActive = 1
  AND ConfigName LIKE 'SAMMS FormQuestionAnswers%'
  AND TargetName IN ('BR', 'SL', 'GL')
```

Expects **exactly 3 rows**. Fails if missing.

Then loads active `taskconfig` rows for ConfigIds 28, 29, 30.

### 2. Split tasks by layer

| Layer | Taskconfig expectation |
|-------|------------------------|
| **BR** | One row per site (~117), each with `SiteCode` + `DataBaseName` |
| **SL** | Exactly 1 row (TaskConfigId 92) |
| **GL** | Exactly 1 row (TaskConfigId 93) |

Site-level Bronze rows are created by `etlconfigandtaskconfigsqls` from `AllsiteCodesAndDatabses.txt`.

### 3. Generate run and task IDs

```python
base_id = new_bigint(0)           # timestamp-based bigint
run_id  = base_id + (layer_index * 100000)
task_id = run_id + task_index
```

Example structure:

```
BR  RunId = base + 100000  →  TaskIds 100001, 100002, … (one per site)
SL  RunId = base + 200000  →  TaskId  200001
GL  RunId = base + 300000  →  TaskId  300001
```

### 4. Insert `pipelinerun` and `taskqueue`

- **3 `pipelinerun` rows** — status `RUNNING`, `TotalTasks` = number of tasks in that layer
- **119 `taskqueue` rows** — status `RUNNING`; Bronze rows include `SiteCode`, `DataBaseName`, `SiteName`

### 5. Exit with `audit_context` JSON

The notebook returns JSON keyed by `BR`, `SL`, `GL`. Each layer includes `run_id`, `config_id`, `target_table`, and a `tasks` array (Bronze has one task object per site). Downstream finalize steps use this to know which IDs to close and which sites to count.

---

## Finalize parameters

### Success — `nb_formqa_audit_finalize_success`

| Parameter | Source | Purpose |
|-----------|--------|---------|
| `p_mode` | `FINALIZE_FORMQA_SUCCESS` | Route to success logic |
| `p_audit_context_json` | Start notebook exit value | BR/SL/GL run and task IDs |
| `p_ingest_run_id` | `@pipeline().RunId` | Match Bronze rows via `_ingest_run_id` |
| `p_status` | `SUCCESS` | Optional status hint |

### Failure — `nb_formqa_audit_finalize_failure`

| Parameter | Source | Purpose |
|-----------|--------|---------|
| `p_mode` | `FINALIZE_FORMQA_FAILURE` | Route to failure logic |
| `p_audit_context_json` | Start notebook exit value | Same context as success |
| `p_ingest_run_id` | `@pipeline().RunId` | Bronze row counts |
| `p_error_message` | Coalesce of failed activity errors | Stored on failed tasks |
| `p_failed_target_name` | `ALL` (current wiring) | Marks all layers FAILED |
| `p_failure_activity` | Activity name | Logged in exit payload |

**Bronze child failure:** Publish is **Skipped**, which **does** trigger `nb_formqa_audit_finalize_failure` (depends on Failed **or Skipped**). However, `p_failed_target_name = ALL` marks every layer FAILED with no per-site detail. See **Edge cases** section for full gaps.

---

## Cell 1 — helper functions

| Function | Role |
|----------|------|
| `new_bigint()` | Unique IDs from UTC timestamp |
| `parse_context()` | Parse `p_audit_context_json` |
| `get_context_for_target()` | Get BR, SL, or GL section from context |
| `align_to_table()` | Match DataFrame columns to Delta table schema |
| `insert_append()` / `delta_upsert()` | Write meta tables with Delta concurrency retry |
| `bronze_counts_for_run()` | Count Bronze rows per site for `_ingest_run_id` |
| `bronze_task_counts_df()` | Join audit context tasks to Bronze counts |
| `finish_bronze_tasks()` | Close all Bronze tasks; write per-site `taskaudit` |
| `finish_single_task()` | Close one Silver or Gold task |
| `dq_counts_for_df()` | Row count, nulls on PK, duplicates on PK |
| `read_gold_df()` | Read Gold warehouse table from Spark (see below) |
| `exit_json()` | Return JSON via `mssparkutils.notebook.exit()` |

---

## Cell 2 — modes

| Mode | When used | Main actions |
|------|-----------|--------------|
| `START_LAYER_RUNS` | Pipeline start | Insert `pipelinerun` + `taskqueue`; return context JSON |
| `FINALIZE_FORMQA_SUCCESS` | After Gold publish succeeds | Bronze per-site audit; SL/GL DQ; close all tasks SUCCESS |
| `FINALIZE_FORMQA_FAILURE` | After Silver/Gold failure | Mark layers FAILED/SKIPPED; re-raise exception |
| `WRITE_DATAQUALITY` | Optional | Write one DQ row for SL or GL |
| `FINISH_TASK_SUCCESS` / `FINISH_TASK_FAILURE` | Optional | Close one layer only |

### Success finalize flow

1. **Bronze:** Count rows in `bhg_bronze.Form.br_tblFormQA` where `_ingest_run_id = p_ingest_run_id`, grouped by site. Update each Bronze `taskqueue` row; append `taskaudit` per site; set Bronze `pipelinerun` to SUCCESS.
2. **Silver:** DQ on 7-column PK; write `dataquality`; finalize SL task (rows read = Bronze total, rows written = Silver count).
3. **Gold:** DQ on same PK via `read_gold_df()`; write `dataquality`; finalize GL task (rows read = Silver count, rows written = Gold count).

### Bronze row counts at finalize (not at start)

At start, only `RUNNING` rows are created. Row counts are computed at finalize by joining:

- Audit context (site → `TaskId`)
- Bronze table: `_site_code`, `_source_database`, `_ingest_run_id`

---

## Gold `workspace_id` and `warehouse_id` — why they exist

These constants appear at the top of Cell 1:

```python
gold_workspace_id = "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
gold_warehouse_id = "d29ef036-8c2c-40b0-a8e0-3279f9a906e7"
gold_warehouse_prefix = "bhg_gold.pats"
```

### The problem they solve

The audit notebook runs on a **Spark session attached to the Bronze/Silver lakehouse**. Bronze and Silver tables are read with:

```python
spark.table("bhg_bronze.Form.br_tblFormQA")
spark.table("bhg_silver.pats.sl_tblFormQuestionAnswers")
```

**Gold is different.** Production data lives in a **Fabric Data Warehouse** (`bhg_gold`), not in the same lakehouse path. You cannot reliably `spark.table("bhg_gold.pats.tbl_dbo_FormQuestionAnswers")` from the Bronze lakehouse notebook the same way.

### What we do with them

They are used only in **`read_gold_df()`**:

```python
def read_gold_df(table_name):
    from com.microsoft.spark.fabric.Constants import Constants
    return (
        spark.read
        .option(Constants.WorkspaceId, gold_workspace_id)
        .option(Constants.DatawarehouseId, gold_warehouse_id)
        .synapsesql(f"{gold_warehouse_prefix}.{table_name}")
    )
```

This tells Spark:

| Constant | Meaning |
|----------|---------|
| `gold_workspace_id` | Fabric workspace that owns the Gold warehouse |
| `gold_warehouse_id` | Fabric Data Warehouse artifact ID for `bhg_gold` |
| `gold_warehouse_prefix` | Schema prefix (`bhg_gold.pats`) |

Together they open a **cross-artifact read** from the warehouse so the notebook can:

1. **Count Gold rows** when finalizing the GL task (`finish_single_task` on success/failure)
2. **Run data quality checks** on Gold — null PK columns, duplicate PK rows — same 7-column key as Silver
3. **Write `meta.dataquality`** rows for Gold with accurate `RowCount` and `DuplicateCount`

### Where they are used in the code

| Location | Usage |
|----------|--------|
| `FINALIZE_FORMQA_SUCCESS` | `gold_df = read_gold_df(gl["target_table"])` → DQ + GL row count |
| `FINALIZE_FORMQA_FAILURE` | `read_gold_df(layer["target_table"]).count()` when GL layer status is SUCCESS |
| `WRITE_DATAQUALITY` | DQ on Gold when `p_target_name = GL` |
| `FINISH_TASK_*` (GL) | Row counts for Gold task completion |

They are **not** used for loading or publishing Gold — the parent pipeline does that with Copy + Script activities against the `bhg_gold` linked service. The warehouse IDs are **read-only for audit/DQ** from the control notebook.

### Same IDs in the parent pipeline

The Gold publish script activity uses the same warehouse in its linked service:

- Workspace: `9141acfe-f2a5-4a5b-85a2-d8a86b46820c`
- Warehouse artifact: `d29ef036-8c2c-40b0-a8e0-3279f9a906e7`

Audit and publish therefore target the same Gold environment.

---

## FormQuestionAnswers vs FormAnswerSignatures

| Item | FormQuestionAnswers | FormAnswerSignatures |
|------|---------------------|----------------------|
| Config prefix | `SAMMS FormQuestionAnswers` | `SAMMS FormAnswerSignatures` |
| ConfigIds | 28 / 29 / 30 | 31 / 32 / 33 |
| Parent pipeline | `Execute_Form_QuestionAnswer` | `Execute_Pipeline_AnswerSignature` |
| Bronze table | `Form.br_tblFormQA` | `Forms.br_tblFormAnswerSig` |
| Silver table | `pats.sl_tblFormQuestionAnswers` | `pats.sl_tblFormAnswerSignatures` |
| Gold table | `pats.tbl_dbo_FormQuestionAnswers` | `pats.tbl_dbo_FormAnswerSignature` |
| Audit notebook | `nb_formqa_control_audit_writer` | `nb_formanswersig_control_audit_writer` |
| Gold workspace/warehouse IDs | Same Fabric Gold warehouse | Same (shared `bhg_gold`) |

---

## Quick reference — all notebook parameters

| Parameter | Used in START | Used in FINALIZE | Default (manual run) |
|-----------|---------------|------------------|----------------------|
| `p_mode` | Yes | Yes | `START_LAYER_RUNS` |
| `p_config_name_prefix` | Yes | No | `SAMMS FormQuestionAnswers` |
| `p_pipeline_name` | No | No | `Execute_Form_QuestionAnswer` |
| `p_pipeline_path` | No | No | `/pipelines/Execute_Form_QuestionAnswer` |
| `p_triggered_by` | Yes (fallback) | No | `Fabric` |
| `p_audit_context_json` | No | Yes | `{}` |
| `p_ingest_run_id` | No | Yes | None |
| `p_error_message` | No | Failure | None |
| `p_failed_target_name` | No | Failure | None |
| `p_failure_activity` | No | Failure | None |
| `p_status` | No | Optional | `SUCCESS` |
| `p_sites_json` | No | Passed, not used in code | `[]` |
| `p_target_name` | No | WRITE_DATAQUALITY / FINISH_TASK | None |
| `p_rows_read` / `p_rows_written` / etc. | No | FINISH_TASK optional | 0 |

---

## Edge cases, gaps, and missing behavior

This section lists known gaps between the **intended** control model and what the **current pipeline + notebook** actually do. Use it for troubleshooting, UAT, and future hardening work.

### Severity legend

| Level | Meaning |
|-------|---------|
| **Critical** | Wrong audit state or silent bad data; should fix before PROD |
| **High** | Misleading monitoring or ops blind spots |
| **Medium** | Works but inaccurate counts or coarse status |
| **Low** | Cleanup, unused params, or nice-to-have |

---

### 1. Pipeline wiring gaps

#### 1.1 Bronze failure — audit *does* finalize, but coarsely (High)

**Earlier note corrected:** When the Bronze child fails, downstream activities (Silver, Gold, Publish) are **Skipped**. `nb_formqa_audit_finalize_failure` depends on Publish **Failed OR Skipped**, so it **does run** in most Bronze failure scenarios.

**Remaining problems:**

- Pipeline passes `p_failed_target_name = "ALL"`, so **every layer (BR/SL/GL) is marked FAILED**, even though Bronze may have partially loaded data before the child failed.
- There is **no per-site FAILED** distinction — all 117 Bronze tasks get the same status and the same error message.
- `nb_notify_failed_child` sends email on Bronze failure, but **`nb_formqa_audit_finalize_failure` also runs** — ops may get email + audit failure without a clear single source of truth.

**Missing:** Layer-accurate failure (`p_failed_target_name = "BR"`) and optional partial-success Bronze audit when only some ForEach iterations fail (today the whole child fails).

---

#### 1.2 No failure email for Silver / Gold failures (High)

| Activity | When it runs |
|----------|--------------|
| `nb_notify_success` | Only after `nb_formqa_audit_finalize_success` |
| `nb_notify_failed_child` | Only when **Bronze child** fails |

If **`nb_forms_bronze_to_silver`**, **Copy**, or **Publish** fails, there is **no dedicated failure notification** activity in the parent pipeline (only audit finalize failure, which re-raises an exception).

**Missing:** A general `nb_notify_failed` tied to finalize failure or each failed activity.

---

#### 1.3 Audit start failure leaves nothing to finalize (Critical)

If `nb_formqa_audit_start` fails:

- No `pipelinerun` / `taskqueue` rows (or partial rows if failure mid-append — see 2.4).
- Publish is Skipped → finalize failure may run but **`p_audit_context_json` is empty/invalid** → finalize raises `"Missing BR/SL/GL context"`.
- Audit is never properly closed.

**Missing:** Guard activity or failure branch that handles “start never completed”.

---

#### 1.4 Finalize failure itself can fail (Critical)

If `nb_formqa_audit_finalize_failure` throws (bad context, Gold read error, Delta conflict after retries, missing taskqueue rows):

- Control tables can stay **`RUNNING`** forever.
- Pipeline shows Failed, but audit is incomplete.

**Missing:** Dead-letter / cleanup job for stale `RUNNING` rows, or a minimal “force close” mode.

---

#### 1.5 Mid-run audit modes not wired (Medium)

The notebook supports `FINISH_TASK_SUCCESS`, `FINISH_TASK_FAILURE`, and `WRITE_DATAQUALITY`, but the parent pipeline **never calls them**. Audit is only written at start and at end.

**Missing (optional):** Close Bronze immediately after child succeeds, Silver after MERGE, before Gold — would give fresher ops visibility if parent dies mid-run.

---

#### 1.6 Unused parameters passed from pipeline (Low)

| Parameter | Passed from pipeline | Used in notebook |
|-----------|----------------------|------------------|
| `p_pipeline_name` | Start | No |
| `p_pipeline_path` | Start | No |
| `p_sites_json` | Finalize success/failure | No |
| `p_audit_context_json` | Bronze child | Child pipeline does not consume it |

**Missing:** Either use them (validation, logging) or remove from pipeline to avoid confusion.

---

### 2. Audit logic gaps (notebook)

#### 2.1 DQ failure does not fail the pipeline (Critical)

On `FINALIZE_FORMQA_SUCCESS`:

```python
"SUCCESS" if sl_duplicate_count == 0 else "FAILED"   # written to dataquality only
finish_single_task(sl, "SUCCESS", ...)               # task still SUCCESS
finish_single_task(gl, "SUCCESS", ...)
```

If Silver or Gold has **duplicate PK rows** or **null PK columns**:

- `meta.dataquality.ValidationStatus` = `FAILED`
- `taskqueue` / `pipelinerun` / `taskaudit` still = **SUCCESS**
- Parent pipeline completes successfully

**Also:** `ValidationStatus` checks duplicates only — **`null_count` does not affect status**.

**Missing:** Fail finalize (or fail pipeline) when `duplicate_count > 0` or `null_count > 0`, or add a separate gate activity.

---

#### 2.2 Silver/Gold row counts are full-table, not run-scoped (High)

| Metric | What code counts |
|--------|------------------|
| Bronze finalize | Rows for **this** `_ingest_run_id` only ✓ |
| Silver DQ / finalize | **Entire** `sl_tblFormQuestionAnswers` table |
| Gold DQ / finalize | **Entire** production Gold table |

Legacy `tsk.tbl_RowTrax` compared source vs destination **for the run**. Here, Silver/Gold counts include **all historical rows**, not just rows touched this run.

**Effect:** `RowsRead` / `RowsWritten` on SL/GL tasks and DQ `RowCount` do not represent incremental load size. Hard to detect “this run added 0 rows” when table already has millions.

**Missing:** Run-scoped counts (e.g. filter by `_ingest_run_id` on Bronze join, or `silver_updated_at` / load timestamp for this pipeline RunId).

---

#### 2.3 Zero-row Bronze sites still marked SUCCESS (Medium)

Sites with no `Form` table, or sites that extract 0 rows for the lookback window:

- Left join in `bronze_task_counts_df` → `RowsCopied = 0`
- Finalize still sets status **SUCCESS** with 0 rows written

This may be correct (inactive site) or wrong (connection/SQL failure). **No differentiation.**

**Missing:** Threshold or flag for unexpected zero-row sites; compare to prior run averages.

---

#### 2.4 No transactional START — orphan RUNNING rows (High)

`START_LAYER_RUNS` uses **append** to `pipelinerun` and `taskqueue`. If the notebook or pipeline is cancelled mid-run:

- Partial inserts possible
- Rows stay **`RUNNING`** with no finalize

**Missing:** Idempotent start keyed by Fabric `pipeline().RunId`, or scheduled cleanup of stale RUNNING rows older than N hours.

---

#### 2.5 Two different “run IDs” — easy to confuse (High)

| ID | Format | Used for |
|----|--------|----------|
| Fabric `pipeline().RunId` | GUID string | Bronze `_ingest_run_id`, `p_ingest_run_id` |
| Audit `pipelinerun.RunId` | Timestamp bigint from `new_bigint()` | `taskqueue`, `taskaudit`, `dataquality` |

There is **no column** in `pipelinerun` storing the Fabric pipeline RunId. You cannot join Fabric Monitor directly to audit tables by one shared key.

**Missing:** Store `@pipeline().RunId` (and/or `@pipeline().RunGroupId`) on `pipelinerun` / `taskqueue` as `ExternalRunId` or similar.

---

#### 2.6 Task ID space limit if site count grows (Low)

```python
run_id = base_id + (target_idx * 100000)
task_id = run_id + task_idx
```

Bronze uses one `task_id` per site sequentially. **~999 sites max per layer** before IDs collide with the next layer’s block (`200000`). Today ~117 sites — safe. Adding many more sites requires revisiting ID allocation.

---

#### 2.7 `new_bigint()` collision on concurrent starts (Low)

Two pipeline runs starting in the same UTC second could generate overlapping `base_id` values. Unlikely in scheduled runs; possible under manual parallel triggers.

**Missing:** Random suffix, sequence table, or include Fabric RunId hash in IDs.

---

#### 2.8 `read_gold_df()` failure after successful Gold publish (Critical)

Order in parent pipeline:

1. Publish Gold (table swap) **succeeds**
2. `nb_formqa_audit_finalize_success` runs
3. `read_gold_df()` fails (permissions, warehouse offline, wrong IDs in PROD)

**Result:** Production Gold is updated, but audit/DQ not finalized — **worst-case split brain** for ops.

**Missing:** Retry finalize; or run DQ before swap; or read Gold via linked service with same credentials as publish.

---

#### 2.9 Hardcoded Gold workspace/warehouse IDs (Critical for PROD)

```python
gold_workspace_id = "9141acfe-f2a5-4a5b-85a2-d8a86b46820c"
gold_warehouse_id = "d29ef036-8c2c-40b0-a8e0-3279f9a906e7"
```

These are **DEV** artifact IDs embedded in notebook code. PROD/UAT promotion without updating them will break Gold DQ reads or read the wrong warehouse.

**Missing:** Parameterize via pipeline (`p_gold_workspace_id`, `p_gold_warehouse_id`) or read from `etlconfig.ConnectionConfig`.

---

### 3. Config / taskconfig edge cases

#### 3.1 Site list drift (Medium)

Bronze site rows come from `AllsiteCodesAndDatabses.txt` baked into `etlconfigandtaskconfigsqls`. A new clinic added to SAMMS but not to that script:

- Won’t appear in `taskconfig`
- Won’t be in `flt_active_formquestionanswers_sites`
- **Never extracted** — no audit task for that site

**Missing:** Process to sync site list from `ctrl.tbl_LocationCons` (legacy source of truth).

---

#### 3.2 Reactivating template TaskConfigId 91 (Medium)

Site generation deactivates template row 91. If someone reactivates 91 **and** site rows exist:

- `START_LAYER_RUNS` may pick up extra Bronze tasks (row without `SiteCode` filtered out for 91 only if SiteCode null — 91 has null SiteCode so excluded from BR list actually)

Template 91 has no SiteCode — filtered out at line `if row_value(task, "SiteCode") and row_value(task, "DataBaseName")`. Low risk but confusing in taskconfig table.

---

#### 3.3 Silver/Gold `DependencyTaskConfigId` nulled (Medium)

Site setup script sets `DependencyTaskConfigId = NULL` on tasks 92 and 93. Dependency is **documented in taskconfig** but not enforced by orchestration or audit notebook.

**Missing:** Either enforce in scheduler or restore dependency IDs for reporting.

---

#### 3.4 Wrong or duplicate `etlconfig` prefix (Critical)

`ConfigName LIKE 'SAMMS FormQuestionAnswers%'` must return **exactly 3** rows. Extra active rows (typo, duplicate DEV rows) cause start to **fail hard** — entire pipeline blocked.

---

### 4. Bronze child + data edge cases

#### 4.1 Partial ForEach failure (High)

Child pipeline `fe_each_samms_site` runs sites in parallel (`batchCount: 5`). If **one site Copy fails**, the whole child pipeline fails:

- Other sites may have appended Bronze rows for this run
- Finalize marks **all** Bronze tasks FAILED (with `p_failed_target_name = ALL`)
- No per-site success/failure in audit

**Missing:** Per-site error handling in child + `FINISH_TASK_*` per site, or continue-on-error with failed site list in context.

---

#### 4.2 Site skipped silently — no Form table (Medium)

`if_form_table_exists` → false → **no Bronze extract**, no error. Audit still creates a task for that site at start; finalize shows **0 rows SUCCESS**.

Indistinguishable from “ran fine, no data in lookback window”.

---

#### 4.3 Silver raises if zero Bronze rows for run (High)

`nb_forms_bronze_to_silver` raises if no Bronze rows for `p_ingest_run_id`. Possible when:

- All sites skipped (no Form table)
- All sites returned 0 rows
- Wrong `p_ingest_run_id`

Pipeline fails before Gold; finalize failure runs with all layers FAILED.

---

#### 4.4 Gold publish validates empty load (Good — not a gap)

Publish script throws if `_load` table is empty — prevents swapping empty data into production. Audit finalize failure runs after that.

---

### 5. Failure finalize behavior (`p_failed_target_name = "ALL"`)

Current pipeline always passes **`ALL`**. Effect:

| Layer | Status on any downstream failure |
|-------|----------------------------------|
| BR | FAILED |
| SL | FAILED |
| GL | FAILED |

The notebook **supports** nuanced cascade (`BR` failed → SL/GL SKIPPED), but pipeline does not use it. Bronze partial success is never reflected.

**Recommended:** Set `p_failed_target_name` from the first failed activity (BR vs SL vs GL) instead of hardcoded `ALL`.

---

### 6. Operational / security gaps

#### 6.1 Notification notebook credentials in source (Critical)

`formquestionanswerdefinition.txt` includes SMTP username/password in the notification notebook. Credentials in repo/notebook are a security risk and will break on rotation.

**Missing:** Azure Key Vault / Fabric credential reference.

---

#### 6.2 No automated stale RUNNING report (Medium)

Operators must manually query:

```sql
SELECT * FROM bhg_bronze.meta.pipelinerun WHERE Status = 'RUNNING' AND StartTime < dateadd(hour, -4, current_timestamp())
```

**Missing:** Scheduled monitoring notebook or Power BI alert on stale runs.

---

### 7. Recommended fixes (priority order)

| Priority | Fix |
|----------|-----|
| 1 | Fail pipeline or task when DQ duplicates/nulls detected on success path |
| 2 | Store Fabric `pipeline().RunId` on `pipelinerun` for correlation |
| 3 | Parameterize Gold workspace/warehouse IDs per environment |
| 4 | Add failure email for Silver/Gold (not only Bronze child) |
| 5 | Replace `p_failed_target_name = ALL` with layer-specific failure |
| 6 | Stale RUNNING cleanup job + idempotent START |
| 7 | Run-scoped Silver/Gold counts for audit accuracy |
| 8 | Wire per-layer `FINISH_TASK_*` after Bronze child and Silver MERGE |
| 9 | Per-site continue-on-error in Bronze ForEach with failed site list in audit |
| 10 | Sync site list from legacy `ctrl.tbl_LocationCons` instead of static JSON |

---

### 8. Quick diagnostic queries

**Stale running audit rows**

```sql
SELECT RunId, ConfigName, TargetName, Status, StartTime, TotalTasks
FROM bhg_bronze.meta.pipelinerun
WHERE Status = 'RUNNING'
ORDER BY StartTime DESC;
```

**Bronze sites with zero rows for latest Fabric run** (replace ingest run id)

```sql
SELECT t.SiteCode, t.DataBaseName, COUNT(b.*) AS bronze_rows
FROM bhg_bronze.meta.taskqueue t
LEFT JOIN bhg_bronze.Form.br_tblFormQA b
  ON b._site_code = t.SiteCode
 AND b._source_database = t.DataBaseName
 AND b._ingest_run_id = '<fabric-pipeline-run-id>'
WHERE t.ConfigId = 28
GROUP BY t.SiteCode, t.DataBaseName
HAVING COUNT(b.*) = 0;
```

**DQ failed but tasks succeeded**

```sql
SELECT dq.*, ta.Status AS task_status
FROM bhg_bronze.meta.dataquality dq
JOIN bhg_bronze.meta.taskaudit ta
  ON dq.RunId = ta.RunId AND dq.TaskConfigId = ta.TaskConfigId
WHERE dq.ValidationStatus = 'FAILED'
  AND ta.Status = 'SUCCESS'
ORDER BY dq.CreatedAt DESC;
```

---

## Related files

- `nb_formqa_control_audit_writer_complete.md` — notebook source (2 cells)
- `formquestionanswerdefinition.txt` — parent/child pipeline JSON and parameter wiring
- `etlconfigandtaskconfigsqls` — etlconfig/taskconfig setup and per-site Bronze rows
- `FormQuestionAnswers_Fabric_Pipeline_Implementation_Guide.md` — full pipeline guide
