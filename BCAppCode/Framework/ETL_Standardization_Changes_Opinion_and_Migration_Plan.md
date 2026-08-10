# ETL Standardization Changes — Opinion & Migration Plan

Detailed technical opinion on the 5 proposed cross-cutting changes to the Fabric ETL framework (applies to DartsSrv, P1 Reference, P1 Forms, Doses, Global, and every future migration).

Scope reviewed to form this opinion:

- `BCAppCode/Framework/controlAudittables.txt`
- `BCAppCode/Framework/nb_get_active_taskconfig.md`
- `BCAppCode/Framework/Howtostart.md`
- `BCAppCode/Framework/etlconfigandtaskconfigsqls`
- `BCAppCode/Framework/vw_mapAction.csv`, `vw_MapSrc2Dsn.csv`
- `BCAppCode/Framework/Optional_Gold_Layer_Control_Guide.md`
- `BCAppCode/SaveDartsSrvDocumentation/dartcustomloaddefintion.txt`, `dartdefintion.txt`
- `BCAppCode/P1-Implmentation/P1-reference/pl_p1_reference.txt`

---

## 0. TL;DR — My Overall Opinion

| # | Change | Possible? | Difficulty (1–10) | Effort | Verdict |
|---|--------|-----------|--------------------|--------|---------|
| 1 | Metadata-driven columns/WHERE (no hardcoding) | Yes | **9/10** | 6–10 weeks | High value, high risk — do it as a **framework rewrite**, not a patch |
| 2 | Stable string config key instead of `ConfigId` | Yes | **2/10** | 2–4 days | Easy, low risk — **do this first and immediately** |
| 3 | Minimize Bronze metadata columns | Yes | **6/10** | 2–3 weeks | Real value, wide blast radius — needs a **compatibility shim** |
| 4 | Generic reusable framework activities (no ETL names) | Yes | **3/10** | 1–2 weeks | Mostly discipline, not new engineering — **do this second** |
| 5 | Switch instead of multiple Filters in Bronze | Yes, with caveats | **5/10** | 1–2 weeks per multi-method ETL | Needs a design decision — see §5 for two valid options |

**My honest opinion, up front:** all five are legitimate, well-reasoned asks and match the *spirit* of the legacy metadata-driven design (`vw_mapAction.csv` + `vw_MapSrc2Dsn.csv` + `SelectConstructor.cs`). None of them are "impossible" in Fabric. But they are **not equal in cost**. #2 and #4 are cheap and should happen almost immediately. #1 is a genuine framework-level rewrite — it is the correct long-term direction, but it is the same order of effort as building a second `SelectConstructor.cs`, just in PySpark instead of C#. #3 and #5 are medium changes whose real cost is **regression risk on already-built ETLs** (DartsSrv, P1 Reference, Forms), not the new code itself.

Recommended sequencing is in §7. Don't do them in the order they were listed — do #2 → #4 → #5 → #3 → #1.

---

## 1. Avoid Hardcoding Columns / WHERE Conditions

### What legacy already does (and why this ask is correct)

The C# ETL is *already* metadata-driven, and that's the standard this ask wants Fabric to match:

- `vw_mapAction.csv` — one row per `SiteCode + ActionKey + StepKey`, and it stores the **WHERE clause as data**:

```12:14:BCAppCode/Framework/vw_mapAction.csv
EST,AHK,1,4,1,SQL,2,SAMMS,...,dbo,tblDartsSrv,pats,tbl_DartsSrv,"convert(date,DsDtStart) = @WorkDate","Order by dsID, dsClt",0,V6,AHK-1-4,1
```

  `WhereCondition` literally contains `@WorkDate` / `@SiteCode` placeholders that `SelectConstructor.cs` substitutes at runtime, per site, per action.

- `vw_MapSrc2Dsn.csv` — one row per **field**, with `Enabled`, `PrimaryKey`, `FieldType`, `DsnFieldName` (destination alias), and a `CompKey` used for checksum construction:

```1:4:BCAppCode/Framework/vw_MapSrc2Dsn.csv
ActionKey,ActionStepKey,FieldKey,FieldName,PHC_Enabled,Enabled,PrimaryKey,FieldType,FieldLength,FieldPrecision,FieldScale,DsnFieldName,Nullable,Default,FormatConvert,CompKey
1,1,0,@SiteCode,1,1,1,Parameter,25,0,0,SiteCode,0,@SiteCode,NULL,1-1-000
1,1,1,cltID,1,1,2,int,4,10,0,ClientID,0,NULL,NULL,1-1-001
```

  `SelectConstructor.cs` builds the `SELECT col AS alias, col2 AS alias2 ...` list dynamically from **enabled** rows only, and builds `RowChkSum` from the same enabled-field set. No column name is hardcoded in C# — it's all metadata.

### What Fabric does today (the violation)

Every Bronze Copy activity in every pipeline I reviewed has the **entire SELECT + WHERE baked as a literal string inside the pipeline JSON**. Example from P1 Reference (`SavePreAdmissionV6`):

```3966:3966:BCAppCode/P1-Implmentation/P1-reference/pl_p1_reference.txt
"value": "@concat(\n'SELECT\n    ''',\nitem().SiteCode,\n... pp.id AS [PreAdmissionid],\n    pp.PatientID AS [Clientid], ... WHERE LEN(pp.CreatedOn) > 0 AND pp.ClientAddress NOT LIKE ''%test data%''\nORDER BY pp.PatientID, pp.ID'\n)"
```

Every column, every alias, every join, and the WHERE clause are hand-typed into the Fabric `concat()` expression. This is true for all 9 P1 Reference methods, and the same pattern exists (with the same problem) in `dartdefintion.txt` / `dartcustomloaddefintion.txt`. Changing one column mapping today means **editing pipeline JSON and republishing a pipeline**, exactly the problem metadata-driven design was invented to avoid in the legacy system.

### Is it possible in Fabric? Yes — but not the same way ADF Copy activities were designed to be used

There are three ways to actually get there, in increasing order of correctness and decreasing order of "how ADF/Fabric wants you to build pipelines":

**Option A — Expression-built SQL from control-table columns (partial fix, low effort).**
Extend `taskconfig` (or a new `meta.columnmapping` table) so the `WHERE` clause and column list live as data, and the Fabric `concat()` expression pulls from `item().WhereCondition` / `item().SelectColumns` instead of a literal string typed into the pipeline. This is mechanically similar to what exists today (Fabric still assembles a string via `concat()`), but the string's *ingredients* come from a control table instead of being typed into the JSON.
  - Effort: Low–Medium per ETL (days), because you still touch every pipeline once to parameterize it.
  - Limitation: Column **lists** (35+ columns per DartsSrv row) become a giant JSON array/string stored in a control table cell — awkward to maintain by hand, though better than editing pipeline JSON.

**Option B — Generic PySpark/notebook query builder (real fix, matches `SelectConstructor.cs`).**
Port the essence of `SelectConstructor.cs` into a shared notebook: read `meta.columnmapping` (Enabled, DsnFieldName, FieldType, PrimaryKey, ChecksumParticipant) + `meta.taskconfig.WhereCondition` (with `@WorkDate`/`@SiteCode`-style placeholders), build the `SELECT ... FROM ... WHERE ...` string in Python, then either (a) execute it via JDBC directly from the notebook and write to Bronze with Spark, or (b) pass the built string into a Copy activity's `sqlReaderQuery` parameter dynamically.
  - This is the *correct* long-term architecture and the one that actually satisfies "no hardcoding" for real.
  - Effort: High. This is a genuine framework build: one generic Bronze extraction notebook, a new control table for column mapping, a migration of the ~15+ Bronze SQL templates already built (P1 Reference × 9, Forms × 2, DartsSrv, Doses, Global) into that metadata, plus validation that every migrated query still produces the same output as the hand-written one.
  - Risk: JDBC-from-notebook extraction has different parallelism/retry/connection-pool characteristics than the native Fabric SQL Server Copy connector. Needs a performance validation pass across all 115 sites before trusting it in production.

**Option C — Hybrid (recommended starting point).**
Keep Copy activities (for performance/connector reliability) but make **only the WHERE clause and the optional-column substitutions** metadata-driven immediately (this covers 90% of "hardcoded logic that changes over time" — lookback windows, quality filters, per-site column existence), and defer the full column-list metadata-driven rebuild (Option B) to a second phase, prioritized by which ETLs actually need frequent column changes (Reference/Forms tables change more often than DartsSrv's fixed schema).

### Control table changes required

New table, modeled directly on `vw_MapSrc2Dsn.csv`:

```sql
CREATE TABLE meta.columnmapping (
    ColumnMappingId   BIGINT IDENTITY PRIMARY KEY,
    ConfigKey         VARCHAR(100) NOT NULL,   -- see §2, stable string not ConfigId
    Method            VARCHAR(100) NOT NULL,
    SourceColumn      VARCHAR(200) NOT NULL,
    DestColumn        VARCHAR(200) NOT NULL,
    FieldType         VARCHAR(50),
    IsPrimaryKey      BIT DEFAULT 0,
    ParticipatesInChecksum BIT DEFAULT 0,
    Enabled           BIT DEFAULT 1,
    OrdinalPosition   INT,
    IsOptionalColumn  BIT DEFAULT 0,           -- drives "check-exists, else NULL" pattern already used for Darts
    ModifiedAt        DATETIME2,
    ModifiedBy        VARCHAR(100)
);
```

Extend `meta.taskconfig`:

```sql
ALTER TABLE meta.taskconfig ADD WhereConditionTemplate VARCHAR(4000) NULL;  -- @WorkDate / @LookbackDays placeholders
```

### Bottlenecks

- Fabric expression language is not a great string-templating engine for 40+ column SELECT lists — pushes you toward Option B (notebook-based), which is the expensive path.
- Every currently-working Bronze pipeline (P1 Reference's 9, DartsSrv, Forms×2) has to be re-validated against SAMMS after migration — this is the single biggest time sink, not the code itself.
- `RowChkSum`/checksum parity is legally/operationally sensitive (it drives what gets updated downstream) — any drift in "which columns participate in the checksum" from the metadata port will silently change Silver merge behavior.

---

## 2. Stable String Config Key Instead of `ConfigId`

### The problem, concretely

Every pipeline parameter and every taskconfig-reading notebook call hardcodes numeric `ConfigId` values directly in pipeline JSON:

```4566:4567:BCAppCode/P1-Implmentation/P1-reference/pl_p1_reference.txt
"p_config_ids_json": {
    "value": "[88,90]",
```

```179:179:BCAppCode/Framework/nb_get_active_taskconfig.md
| `p_config_ids_json` | `[25]` |
```

`ConfigId` is a bigint identity column, assigned in whatever order rows happen to get inserted in a given environment (`Framework/etlconfigandtaskconfigsqls` shows DartsSrv = 25/26/27 and FormQuestionAnswers = 28/29/30 chosen by hand at seed time). If DEV and PROD `meta.etlconfig` tables were seeded independently, or seeded in a different order, or a row gets re-created after a delete, **the same pipeline JSON promoted across environments can silently point at the wrong ConfigId** — this is a real, not theoretical, promotion risk.

### Is it possible? Yes, and it's genuinely easy

`meta.etlconfig` already has `ConfigName` (e.g. `"SAMMS DartsSrv Bronze Pipeline"`) and `TargetName` (`BR`/`SL`/`GL`). `controlAudittables.txt` even shows the team **already using `ConfigName LIKE`** as an alternate lookup pattern in ad hoc validation queries — the string-based identity concept is not new to this codebase, it's just not used in pipeline parameters yet.

**Recommended fix:**

```sql
ALTER TABLE meta.etlconfig ADD ConfigKey VARCHAR(100) NULL;

-- backfill, one-time, per environment (values must match across DEV/TEST/PROD)
UPDATE meta.etlconfig SET ConfigKey = 'DARTS_BR' WHERE ConfigId = 25;
UPDATE meta.etlconfig SET ConfigKey = 'DARTS_SL' WHERE ConfigId = 26;
UPDATE meta.etlconfig SET ConfigKey = 'DARTS_GL' WHERE ConfigId = 27;
UPDATE meta.etlconfig SET ConfigKey = 'P1REFERENCE_BR' WHERE ConfigId = 88;
UPDATE meta.etlconfig SET ConfigKey = 'P1REFERENCE_SL' WHERE ConfigId = 89;
UPDATE meta.etlconfig SET ConfigKey = 'P1REFERENCE_GL' WHERE ConfigId = 90;
-- ... one row per existing module (≈10 modules today)

ALTER TABLE meta.etlconfig ADD CONSTRAINT UQ_etlconfig_ConfigKey UNIQUE (ConfigKey);
```

Then change `nb_get_active_taskconfig` (already the generic notebook — see §4) to accept `p_config_keys_json` and resolve to `ConfigId` **internally**, so joins downstream still use the fast bigint key, but every pipeline JSON / parameter only ever contains a portable string:

```python
try:
    p_config_keys_json
except NameError:
    p_config_keys_json = "[]"

config_keys = [str(x).strip().upper() for x in parse_json_list(p_config_keys_json, "p_config_keys_json")]

if config_keys:
    etl_df = spark.table("bhg_bronze.meta.etlconfig").where(F.upper(F.col("ConfigKey")).isin(config_keys))
    config_ids = [r.ConfigId for r in etl_df.select("ConfigId").collect()]
    df = df.where(F.col("ConfigId").isin(config_ids))
```

Pipeline JSON changes from:

```json
"p_config_ids_json": { "value": "[88,90]", "type": "string" }
```

to:

```json
"p_config_keys_json": { "value": "[\"P1REFERENCE_BR\",\"P1REFERENCE_GL\"]", "type": "string" }
```

### Effort & risk

- **2–4 days total**, including backfilling `ConfigKey` for every existing module (≈10 today: DartsSrv, FormQA, FormAnswerSig, P1 Reference×3 layers, etc.) and updating the handful of pipelines that currently hardcode `p_config_ids_json`.
- Zero risk to existing runtime behavior if done as an **additive** column with a resolution step in the shared notebook — old `p_config_ids_json` callers keep working while you migrate pipelines one at a time.
- Bottleneck: discipline, not technology — someone has to own the `ConfigKey` naming convention (`{MODULE}_{BR|SL|GL}`) and enforce the uniqueness constraint so two developers don't pick the same key for different modules.

**This should be done immediately and independently of the other 4 changes.** It's the cheapest, safest, and highest ratio of "future pain avoided" to "effort spent" of the five asks.

---

## 3. Minimize Bronze Metadata Columns (Keep Only `ExtractedAt`)

### Current state

Every Bronze row in every pipeline carries this full set of metadata columns, duplicated per row (confirmed in `pl_p1_reference.txt` translator mappings and `Howtostart.md` §2):

`SiteCode`, `SourceDatabase`, `IngestRunId`, `ExtractedAt`, `SourceQueryStartDate`, `SourceQueryEndDate`, `LookbackDate` — **7 metadata columns per Bronze row**, on top of the business columns.

### Why this happened (and why it's not purely wasteful)

These columns are not decorative — they're load-bearing for downstream logic that already exists in production:

```280:288:BCAppCode/Framework/controlAudittables.txt
select
    _site_code as SiteCode,
    _source_database as DataBaseName,
    count(*) as RowsCopied
from bhg_bronze.Dart.br_tblDartSrv
where _ingest_run_id = '<current pipeline ingest run id>'
group by _site_code, _source_database;
```

Bronze audit row-counts, Silver's "only process current run" filter, and validation queries all currently read `SiteCode`/`SourceDatabase`/`IngestRunId` **directly off the Bronze table**, not by joining out to an audit table. Reducing Bronze to `ExtractedAt` only, as literally requested, would break every one of those without a replacement join path.

### Is it possible? Yes, with one caveat: you need at least one join key

You cannot go to "only `ExtractedAt`" and still know which site/run a row belongs to, unless you accept one of:

- **Physical separation** — one Bronze table (or Delta partition) per site, so "which site" is structural, not a column. This is a bigger structural change than the ask intends and doesn't remove the run/date columns anyway.
- **One join key column retained** — keep `IngestRunId` (or `TaskConfigId`) as the *only* Bronze metadata column beyond `ExtractedAt`, and join out to `meta.taskqueue`/`meta.taskaudit` (which **already store** `SiteCode`, `DataBaseName`, `SiteName` per task — see `controlAudittables.txt` §5/§6) to recover `SiteCode`/`SourceDatabase`/lookback dates when needed.

**My recommendation: keep `ExtractedAt` + `IngestRunId` (2 columns), drop the other 5.** This is close enough to the spirit of the ask ("minimize... retain only essential... retrieve the rest by joining") while remaining technically implementable, because `IngestRunId` is the one column every downstream Silver/audit query already keys off of.

```sql
-- meta.taskqueue and meta.taskaudit already carry these (controlAudittables.txt §5/§6):
-- SiteCode, DataBaseName, SiteName, TaskConfigId, RunId

-- Bronze row count with the reduced column set becomes:
select
    tq.SiteCode,
    tq.DataBaseName,
    count(*) as RowsCopied
from bhg_bronze.Dart.br_tblDartSrv b
join meta.taskqueue tq
    on b.IngestRunId = tq.RunId          -- or a dedicated join key, see below
where b.IngestRunId = '<current run id>'
group by tq.SiteCode, tq.DataBaseName;
```

One honest caution: `IngestRunId` today is a *pipeline run* identifier shared across **all sites in a run**, not a per-site key — for a Bronze table with 115 sites landing in the same run, `IngestRunId` alone is not enough to isolate a single site's rows for the join above (you'd get all sites' rows for that run). You would either need to also retain a lightweight per-site key (e.g., a numeric `TaskConfigId` — cheaper to store than the 4 string columns being removed), or restructure the join to happen at the *pipeline-run* grain (fine for audit row-counts, not fine if a downstream Silver notebook genuinely needs `SiteCode` per row for its merge). **This is the real design decision to make before touching any code**, not the "which columns to drop" part.

### Storage-overhead reality check

I want to be candid here: the *storage* savings argument is weaker than it sounds. `SiteCode`/`SourceDatabase` are extremely low-cardinality strings (dozens to ~120 distinct values) — Delta/Parquet dictionary-encodes and compresses these columns very efficiently already, so removing them saves relatively little disk. The **real value** of this change is:
1. Single source of truth for execution metadata (no drift between what Bronze says and what the audit tables say).
2. Fewer columns to keep consistent across every new ETL's Bronze schema.
3. Cleaner separation of "data" vs "execution metadata" — which is good architecture regardless of the byte count.

If the goal was specifically disk savings, I'd say this delivers less than advertised. If the goal is architectural cleanliness and consistency, it delivers a lot.

### Effort & bottleneck

- Effort: Medium (2–3 weeks) — not because the Bronze-side change is hard (it's a translator/mapping edit per pipeline), but because **every already-built Silver merge notebook and every audit finalizer** (DartsSrv, P1 Reference ×9, Forms ×2) currently reads these columns directly from Bronze and has to be rewritten to join instead.
- This is the change most likely to cause **silent regressions** in already-working, already-validated pipelines if not done carefully — recommend a compatibility shim (add the join-based columns as a Spark view with the *same names*, e.g. `br_tblDartSrv_enriched`, so existing notebook code doesn't need to change immediately) rather than a hard cutover.

---

## 4. Generic, Reusable Framework Activities (No ETL-Specific Names)

### Current state — this is the most avoidable problem of the five

`nb_get_active_taskconfig` is already written to be fully generic — every filter is parameter-driven:

```20:57:BCAppCode/Framework/nb_get_active_taskconfig.md
p_config_ids_json / p_methods_json / p_target_tables_json / p_only_active / p_require_site / p_require_database / p_require_source_table
```

But the same document then shows it being **wrapped/renamed per ETL** rather than called directly:

```179:189:BCAppCode/Framework/nb_get_active_taskconfig.md
| `p_config_ids_json` | `[25]` |
...
The Darts parent filter should use:
@json(activity('nb_get_darts_taskconfig').output.result.exitValue)
```

`nb_get_darts_taskconfig` and (by the same pattern) `nb_get_p1_reference_taskconfig` appear to be **cloned copies** of the one generic notebook, renamed per ETL, rather than the same shared notebook artifact invoked with different parameters. Same story for the audit finalizer: `controlAudittables.txt` shows per-ETL **mode strings** (`FINALIZE_DARTS_SUCCESS`, presumably `FINALIZE_FORMQA_SUCCESS`, `FINALIZE_FORMANSWERSIG_SUCCESS` per `Howtostart.md` §7) baked into what should be one generic finalizer.

### Is it possible? Yes — this is mostly a discipline/consolidation problem, not a new capability

Two things need to happen:

1. **Stop cloning `nb_get_active_taskconfig`.** Every parent pipeline should call the *one* notebook object, passing `p_config_keys_json` (from §2) and `p_methods_json`. Delete `nb_get_darts_taskconfig`, `nb_get_p1_reference_taskconfig`, etc. as separate notebook artifacts once callers are repointed.
2. **Make the audit finalizer's "mode" generic**, not ETL-named. Instead of `FINALIZE_DARTS_SUCCESS` / `FINALIZE_FORMQA_SUCCESS`, use one mode (`FINALIZE_SUCCESS`) parameterized by the Bronze table name + grouping key columns (which are already sitting in `taskconfig`/`taskqueue`, e.g. `TargetTable`, `SiteCode`, `DataBaseName`). The finalizer's actual per-ETL differences (e.g., DartsSrv groups by `SiteCode + DataBaseName`; P1 Reference groups by `Method + SiteCode` because of the 9-method site-success table) can be expressed as **parameters**, not as separate code paths named after the ETL.

```python
# Instead of: if mode == "FINALIZE_DARTS_SUCCESS": ... elif mode == "FINALIZE_FORMQA_SUCCESS": ...
# Generic call:
nb_audit_finalize(
    mode="FINALIZE_SUCCESS",
    config_keys=["DARTS_BR", "DARTS_SL", "DARTS_GL"],
    bronze_table="bhg_bronze.Dart.br_tblDartSrv",
    group_by_columns=["SiteCode", "DataBaseName"],
)
```

### Effort & bottleneck

- Effort: **Low–Medium (1–2 weeks)**. The generic logic mostly already exists (`nb_get_active_taskconfig` proves the team already knows how to write it this way) — this is a consolidation pass, not new invention.
- Real bottleneck: some ETL-specific behavior is currently *implicitly* encoded inside the "ETL-named" branches (e.g., P1 Reference's `br_p1_reference_site_success` marker table is structurally different from DartsSrv's per-site Bronze count approach). Before deleting the ETL-named copies, each one has to be read carefully to make sure its quirks become **explicit parameters** in the generic version, not silently dropped.
- Do this **before** #1, #3, and #5 — a genuinely generic framework makes all three of those changes easier to implement once, instead of once per ETL.

---

## 5. Switch Activity Instead of Multiple Filters in Bronze

### Current state — the pattern being targeted

P1 Reference's Bronze child pipeline (`pl_reference`) has **9 near-identical Filter activities**, one per method, each a sibling at the top level, each followed by its own `ForEach`:

```10:20:BCAppCode/P1-Implmentation/P1-reference/pl_p1_reference.txt
"name": "flt_child_clinic_sites",
"type": "Filter",
...
"condition": { "value": "@equals(item().Method, 'SaveClinic')", ... }
```

...repeated for `Save3pSetup`, `SaveCodes`, `SaveServices`, `SavedropDownListItems`, `SaveCustomAnswers`, `SaveCustomQuestions`, `SavePreAdmissionV6`, `SavePreAdminReferrals` (confirmed at lines 2237, 2510, 2783, 3056, 3329, 3602, 4148 of the same file). Each is structurally identical except for the method name and the SQL inside its `ForEach`.

### Important precedent: Switch already exists in this codebase — but for a different problem

DartsSrv's custom-load extension already uses `Switch`:

```1620:1636:BCAppCode/SaveDartsSrvDocumentation/dartcustomloaddefintion.txt
"name": "Switch1",
"type": "Switch",
"dependsOn": [{ "activity": "lkp_check_optional_columns_exist", ... }],
"typeProperties": { "on": { "value": "@item().LoadType", ... }, "cases": [...] }
```

Notice: that `Switch1` is **inside** a per-site `ForEach`, branching on `@item().LoadType` — a *per-row* value. That's a different shape of problem than P1 Reference's 9 sibling Filters, which split **one array into 9 method-based subsets**, each with its own downstream `ForEach`.

### Why "Switch outside the ForEach" doesn't map 1:1 onto the current problem

A `Switch` activity evaluates **one scalar expression once per activity execution** — it does not iterate an array. It is a good replacement for `@item().LoadType` per-row branching (which is exactly how DartsSrv already uses it), but it is **not a native replacement for "split this array into 9 subsets"** the way 9 `Filter` activities are used in P1 Reference today. There are two technically valid ways to satisfy the spirit of the ask, and they have different costs:

**Option A — Switch nested inside a Methods-level ForEach (lower blast radius, recommended).**
Replace the 9 sibling `Filter → ForEach` pairs with:
1. One `ForEach` over a small array of method names (9 items, `batchCount: 9`, fully parallel — cheap, since it's not iterating sites).
2. Inside each iteration, one `Switch` on `@item()` (the method name) with 9 cases.
3. Each case contains the method-specific `Filter`-by-method (now trivial: `@equals(item().Method, <case value>)` becomes redundant if you pre-group, but you still need *some* mechanism to get "sites for this method" — either a `Filter` inside the case, or better, pass the pre-grouped site list as the `ForEach` item directly using an expression like `@filter(pipeline().parameters.p_sites, ...)`).

This reduces 9 sibling top-level activities to 1 `ForEach` + 1 `Switch`, which is meaningfully simpler to read and maintain, but there is still filtering logic — it just lives inside the Switch case instead of as 9 separate top-level Filters. This is the most honest, low-risk interpretation of "Switch instead of multiple Filters."

**Option B — Literal "Switch outside any ForEach" (matches the ask word-for-word, higher orchestration cost).**
Change the **parent** pipeline (`pl_execute_reference`) to invoke the Bronze child pipeline **once per method** (9 `InvokePipeline` calls, or 1 `ForEach` over methods at the parent level with `waitOnCompletion` per call) instead of once for all 9 methods together. The child pipeline (`pl_reference`) then receives `p_method` as a **scalar parameter**, and its very first activity is a `Switch` on `@pipeline().parameters.p_method` — evaluated exactly once per child pipeline run, genuinely "outside" (i.e., above/before) any `ForEach`. Each case contains the method-specific `ForEach`-over-sites + Lookup + Copy.

This is the version that literally satisfies "Switch outside the ForEach instead of Filter," and it also has a side benefit: method-level pipeline runs become independently retryable/observable in the Fabric monitoring UI. The cost is that the parent orchestration becomes more complex (9 child invocations instead of 1), and Fabric's per-activity invocation overhead (auth, cold start) is paid 9× instead of 1× per run — for 115 sites × 9 methods this is a real but not prohibitive scheduling overhead, worth a timing test on one environment before committing.

### My recommendation

Use **Option A** for P1 Reference and any future multi-method ETL — it delivers the readability and maintainability win (fewer, less-repetitive top-level activities) with a fraction of the orchestration risk of Option B. Reserve **Option B**'s per-method child invocation pattern only for cases where you specifically want method-level retry/monitoring granularity (which is a legitimate but separate ask from "avoid multiple Filters").

For single-method ETLs (DartsSrv, FormQuestionAnswers, FormAnswerSignatures), this change **does not apply** — they don't have the "9 sibling Filters" problem to begin with; their existing `Switch` usage (per-site `LoadType` routing) is already the correct pattern and should be left as-is.

### Effort & bottleneck

- Effort: **1–2 weeks per multi-method ETL** you choose to convert (today that's really only P1 Reference — Forms has 2 methods, not 9, so the win there is smaller).
- Bottleneck: this is a pipeline JSON restructuring on an ETL that is already built, tested, and presumably close to (or in) production for 115 sites — every restructuring like this needs a full re-run/parity check against the unit testing guide (`P1_Reference_Unit_Testing_Guide.md`) before being trusted.

---

## 6. Consolidated Control-Table Schema Changes

| Table | Change | Why | Related change |
|---|---|---|---|
| `meta.etlconfig` | Add `ConfigKey VARCHAR(100)` + unique constraint | Environment-stable identifier | #2 |
| `meta.taskconfig` | Add `WhereConditionTemplate VARCHAR(4000)` | Metadata-driven WHERE, replaces literal SQL in pipeline JSON | #1 |
| *(new)* `meta.columnmapping` | New table: `ConfigKey`, `Method`, `SourceColumn`, `DestColumn`, `FieldType`, `IsPrimaryKey`, `ParticipatesInChecksum`, `Enabled`, `OrdinalPosition`, `IsOptionalColumn` | Fabric equivalent of `vw_MapSrc2Dsn.csv` | #1 |
| Bronze tables (all) | Remove `SourceDatabase`, `SourceQueryStartDate`, `SourceQueryEndDate`, `LookbackDate`; keep `ExtractedAt` + `IngestRunId` (or `TaskConfigId`) | Minimize per-row metadata duplication | #3 |
| `meta.taskqueue` / `meta.taskaudit` | No schema change — already carry `SiteCode`/`DataBaseName`/`SiteName` per task (confirmed in `controlAudittables.txt` §5/§6) | Becomes the join target for anything removed from Bronze | #3 |
| Notebook artifacts | Delete ETL-named clones (`nb_get_darts_taskconfig`, `nb_get_p1_reference_taskconfig`); all callers point at `nb_get_active_taskconfig` | Enforce genuinely shared framework code | #4 |
| Audit finalizer notebook | Collapse `FINALIZE_<ETL>_SUCCESS` / `FINALIZE_<ETL>_FAILURE` modes into `FINALIZE_SUCCESS` / `FINALIZE_FAILURE` + parameters | Same | #4 |

---

## 7. Recommended Sequencing (Not The Order Listed In The Ask)

```text
Phase 1 (days)         Phase 2 (1–2 wks)         Phase 3 (2–3 wks)         Phase 4 (6–10 wks)
─────────────────      ─────────────────         ─────────────────         ──────────────────
#2 ConfigKey     ───▶   #4 Generic framework ──▶  #5 Switch (P1 Ref)  ──▶   #1 Metadata-driven
(additive,              (delete ETL-named          #3 Minimize Bronze       columns/WHERE
zero regression          clones, consolidate        metadata (compat        (framework rewrite,
risk)                    finalizer modes)           shim first, then         Option B/C from §1)
                                                     hard cutover)
```

**Why this order:**
1. #2 is free and removes an environment-promotion landmine immediately — no reason to wait.
2. #4 makes every subsequent change cheaper, because you're modifying one shared notebook instead of N cloned ones.
3. #5 and #3 are both "restructure an already-built ETL" changes — do them together per-ETL (touch P1 Reference once for both, not twice), with a full parity re-test against `P1_Reference_Unit_Testing_Guide.md`-style validation each time.
4. #1 is the real framework rewrite — it benefits most from #2 (`ConfigKey`) and #4 (generic notebooks) already being in place, and it's the one place I'd budget real design/prototype time before touching production pipelines. Prototype on **one small ETL first** (e.g., `SavedropDownListItems` — only 5 columns, no complex joins) before attempting DartsSrv's 45+ column `SELECT`.

---

## 8. Overall Risks & Bottlenecks Summary

| Risk | Affects | Mitigation |
|---|---|---|
| Regression in already-validated pipelines (P1 Reference, DartsSrv) | #1, #3, #5 | Re-run existing unit testing guides after every structural change; never combine a structural change with a business-logic change in the same pass |
| `RowChkSum` drift if checksum column list is ported incorrectly to metadata | #1 | Diff the metadata-derived checksum column list against the current hardcoded `CHECKSUM(...)` call, column-for-column, before cutover |
| JDBC-from-notebook performance vs native Copy connector | #1 (Option B) | Time-box a proof-of-concept against one large SAMMS site before committing the whole framework to notebook-based extraction |
| Losing per-site row identity in Bronze after metadata reduction | #3 | Keep one lightweight per-site join key (not zero); don't literally reduce to `ExtractedAt`-only |
| `ConfigKey` collisions across teams/environments | #2 | Unique constraint + a short naming convention doc (`{MODULE}_{BR|SL|GL}`) |
| Increased Fabric orchestration overhead from per-method child invocations | #5 (Option B only) | Prefer Option A (Switch nested under a Methods-ForEach) unless method-level retry granularity is a hard requirement |
| Hidden ETL-specific quirks lost when deleting cloned notebooks | #4 | Read each ETL-named clone fully before deleting it; convert quirks to explicit parameters, don't assume they're interchangeable |

---

## 9. Final Answer To "Is It Possible?"

Yes to all five. Nothing here requires a capability Fabric doesn't have. The honest caveats are:

- #1 is the same *size* of work as the original `SelectConstructor.cs` build, just re-targeted at Fabric/PySpark — budget it like a framework project, not a ticket.
- #3's literal wording ("only `ExtractedAt`") isn't fully implementable without losing the ability to identify which site a row came from — I'd negotiate that down to "`ExtractedAt` + one join key" as the real target.
- #5's literal wording ("Switch outside the ForEach") is achievable but requires changing how the parent pipeline invokes the Bronze child (once per method instead of once total) — Option A gets you 90% of the maintainability win without that orchestration change, and is what I'd actually ship.
- #2 and #4 have no real caveats — they're correct as stated and should just be done.
