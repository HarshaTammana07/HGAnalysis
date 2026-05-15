# Forms ETL — Verification & Dependency Document

**Purpose:** This document is for verification with the C# developer.
It captures our understanding of the Forms ETL pipeline — source joins, lookup keys,
transformation logic, and dependencies for all 5 destination tables.
Please confirm each section is correct before we proceed with migration.

**Pipeline:** `Samms-Forms` — `BHGTaskRunner.exe 6`
**Sites processed:** ~115 active sites (sequential, one at a time)
**Overall lookback:** -30 days from run date (`WorkDate + DaysBack - 15`)

---

## How the Pipeline Starts — Task Queue Structure

> **Question for developer:** Is the task queue structure below correct?

When `Scheduler.exe` runs (night before), it creates tasks in `tsk.tbl_Tasks2`:

```
ParentTask  (TaskName = "Samms-Forms", SiteCode = All, Status = 17)
  │
  ├── Child: pats.tbl_dbo_FormQuestionAnswers   | SiteCode = B01 | Status = 17
  ├── Child: pats.tbl_dbo_FormAnswerSignatures  | SiteCode = B01 | Status = 17
  ├── Child: pats.tbl_dbo_FormQuestionAnswers   | SiteCode = B24 | Status = 17
  ├── Child: pats.tbl_dbo_FormAnswerSignatures  | SiteCode = B24 | Status = 17
  └── ... (115 sites × 2 task types = ~230 child rows)
```

**Key observation:** Only 2 task names appear in the queue.
The other 3 destination tables (`tbl_eandmformmdm`, `tbl_eandmformpregnancy`,
`tbl_comprehensiveassessmentform`) are processed **inline** within the same loop
iteration — they do not have separate task rows.

> **Verification question:** Are `tbl_eandmformmdm`, `tbl_eandmformpregnancy`, and
> `tbl_comprehensiveassessmentform` always run for every site, even if that site
> does not have EandMForm data? Or is there an existence check first?

---

## Role of `ctrl.tbl_Forms2Process`

> **Question for developer:** Is the Forms2Process logic below correct?

This Azure config table controls which **extra SAMMS form tables** (that do NOT follow
the standard `Form → Question → Answer` structure) get included via `UNION` in the
source query for `FormQuestionAnswers` and `FormAnswerSignatures`.


| Column              | Purpose                                                                                                   |
| ------------------- | --------------------------------------------------------------------------------------------------------- |
| `FormName`          | Logical form name (e.g. `Treatment Plan`) — becomes the `FormName` value in Azure                         |
| `TableName`         | The SAMMS table to UNION in (e.g. `tblTP17REVIEW`). `NULL` = uses standard Form/Q/A structure             |
| `Prefix`            | Numeric prefix used to construct the `FormId` in the UNION arm                                            |
| `DateFilterEnabled` | `true` → only reset/include rows within the -30 day window; `false` → include all rows regardless of date |
| `Enabled`           | `false` → skip this form entirely from source query                                                       |


**How it affects the source query at runtime:**

```
Base query:  SELECT from Form → FormTemplate → Question → Answer
             WHERE CreatedOn or UpdatedOn >= wrkdt

UNION        SELECT from tblTP17REVIEW → SF_PatientPreAdmission
             WHERE DateAdded >= wrkdt   (if DateFilterEnabled = true)

UNION        SELECT from tblORDERREQ → SF_PatientPreAdmission
             WHERE statusDate >= wrkdt  (if DateFilterEnabled = true)
```

> **Verification question 1:** Is the `Prefix` column used to build the `FormId`
> (e.g. `CAST(Prefix AS VARCHAR) + '-' + CAST(id AS VARCHAR)`)? Please confirm the
> exact `FormId` construction for each UNION arm.

> **Verification question 2:** If a `TableName` does not exist at a particular
> clinic's SAMMS database, is that UNION arm silently skipped?
> We see `sys.tables` checks in the code — please confirm.

---

## Table 1 — `pats.tbl_dbo_FormQuestionAnswers`

### Source JOINs (SAMMS)

```sql
SELECT ...
FROM dbo.Form f
  LEFT JOIN  dbo.FormTemplate ft        ON f.FormTemplateId = ft.Id
  LEFT JOIN  dbo.Question q             ON ft.Id = q.FormTemplateId
  LEFT JOIN  dbo.Answer a               ON f.Id = a.FormId AND q.Id = a.QuestionId
  INNER JOIN dbo.SF_PatientPreAdmission pa ON f.PreAdmissionId = pa.ID
  LEFT JOIN  dbo.SF_DataForms d         ON pa.DataFormId = d.Id

UNION
-- Additional arms per ctrl.tbl_Forms2Process (e.g. tblTP17REVIEW, tblORDERREQ)
SELECT ...
FROM dbo.[tblTP17REVIEW] v
  INNER JOIN dbo.SF_PatientPreAdmission pa ON v.PreAdmissionId = pa.ID
  LEFT JOIN  dbo.SF_DataForms d            ON pa.DataFormId = d.Id
```

### Date Filter (WHERE)

```
wrkdt = WorkDate + (DaysBack - 15)   →   approx -30 days from run date

WHERE (f.CreatedOn >= wrkdt OR ISNULL(f.UpdatedOn, f.CreatedOn) >= wrkdt)
  AND a.Value IS NOT NULL

UNION
WHERE (f.CreatedOn >= wrkdt OR ISNULL(f.UpdatedOn, f.CreatedOn) >= wrkdt)
  AND q.Id IS NULL   ← rows with no question (form-level only records)
```

**Special override:** If `st.Reload = true` → `wrkdt` is forced to `'1/1/2010'` (full historical reload).

### Upsert Lookup Key (how existing Azure rows are matched)


| Key Column        | Source                                                   |
| ----------------- | -------------------------------------------------------- |
| `SiteCode`        | ETL parameter `sc`                                       |
| `FormName`        | `ft.FormName` (normalized — `TP-`* → `"Treatment Plan"`) |
| `FormId`          | `UPPER(f.Id)` or UNION arm equivalent                    |
| `ClientId`        | `pa.ClientId`                                            |
| `PreAdmissionId`  | `f.PreAdmissionId` (empty → stored as **-1**)            |
| `QuestionId`      | `q.Id` (null → stored as **0**)                          |
| `QuestionOrderId` | `q.QuestionOrderId`                                      |


### RowState Logic (int — 0 or 1)


| RowState                      | Condition                                                 |
| ----------------------------- | --------------------------------------------------------- |
| **0 (soft-deleted/inactive)** | `ClientId < 0` in source                                  |
| **0 (soft-deleted/inactive)** | `IsDeleted = "1"` in source — **this wins over ClientId** |
| **1 (active)**                | `ClientId >= 0` AND `IsDeleted != "1"`                    |


### Pre-pass (before main upsert loop)

```
1. Load ALL Azure rows for this SiteCode
2. For each row:
   - Check tbl_Forms2Process for this FormName
   - If DateFilterEnabled = true  → set RowState = 0 only if row is within wrkdt window
   - If DateFilterEnabled = false → set RowState = 0 unconditionally
3. Commit this soft-reset (SaveChanges)
4. Then run upsert loop — incoming rows re-activate (RowState = 1) matching rows
```

**Effect:** Any row NOT returned from SAMMS stays RowState = 0 (soft-deleted).

### Load Path


| Sites                                                                                 | Path                                                                              |
| ------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, DRD-SF, V12, B35, B25, V9, FW, LO, B42 | **Bulk** → `TRUNCATE stg.tbl_FormQA` → `SqlBulkCopy` → `EXEC stg.sp_FormQA_Merge` |
| All other sites                                                                       | **EF Core** → `SaveFormQuestionAnswers()`                                         |


### Notable Column Transformations


| Column           | Transformation                                              |
| ---------------- | ----------------------------------------------------------- |
| `FormId`         | Always stored as `UPPER(source value)`                      |
| `PreAdmissionId` | Empty string → stored as **-1**                             |
| `QuestionId`     | NULL → stored as **0**                                      |
| `QuestionText`   | **NOT updated** on match — only written on INSERT           |
| `IsDeleted`      | Final authority on RowState — overrides ClientId sign logic |
| `IsChildForm`    | `1` or `0` from source flag                                 |


### BAMMerge

**YES — `pats.BAMMerge @sitecode` runs after EVERY site, on BOTH load paths.**

```
Bulk path:   EXEC stg.sp_FormQA_Merge → EXEC pats.BAMMerge @sitecode='B01'
EF path:     SaveFormQuestionAnswers() → EXEC pats.BAMMerge @sitecode='B01'
```

> **Verification question:** What does `pats.BAMMerge` recalculate exactly?
> Is it aggregating BAM scores from `FormQuestionAnswers` rows? Which destination
> tables does it write to? How long does it typically take per site?

---

## Table 2 — `pats.tbl_dbo_FormAnswerSignatures`

### Source JOINs (SAMMS)

```sql
SELECT ...
FROM dbo.Form f
  LEFT JOIN  dbo.FormTemplate ft           ON f.FormTemplateId = ft.Id
  INNER JOIN dbo.SF_PatientPreAdmission pa ON f.PreAdmissionId = pa.ID
  LEFT JOIN  dbo.SF_DataForms d            ON pa.DataFormId = d.Id

-- 9 correlated subqueries — one per signature slot:
  ( SELECT TOP 1 DateField FROM dbo.AnswerSignature
    WHERE FormId = f.Id AND Sign = 'CounselorSign'
    ORDER BY DateTime DESC ) AS CounselorSignDate,
  ... (8 more signature columns same pattern)

UNION
-- ctrl.tbl_Forms2Process extra arms (tblTP17REVIEW, tblORDERREQ)
SELECT ...
FROM dbo.[tblTP17REVIEW] v
  INNER JOIN dbo.SF_PatientPreAdmission pa ON v.PreAdmissionId = pa.ID
  ...
```

**Note:** When a signature is missing, a sentinel `'1/1/1900'` is stored instead of NULL.

### Date Filter (WHERE)

Date filter in the base query is **commented out** in the code — the full form list is
pulled without a date filter, but the UNION arms respect `DateFilterEnabled` from
`ctrl.tbl_Forms2Process`.

**Special override:** If `WorkDate = 2/2/2024` → `wrkdt` forced to `'1/1/2010'`.

### Upsert Lookup Key


| Key Column | Source                                            |
| ---------- | ------------------------------------------------- |
| `SiteCode` | Source row value                                  |
| `FormName` | From `FormTemplate.FormName`                      |
| `FormId`   | `UPPER(source value)`                             |
| `ClientId` | `Math.Abs(source value)` — always stored positive |


### RowState Logic (int — 0 or 1)


| RowState | Condition                                                           |
| -------- | ------------------------------------------------------------------- |
| **0**    | `ClientId < 0` (stored as Abs, but original sign used for RowState) |
| **0**    | `IsDeleted = "1"`                                                   |
| **0**    | `IsDeleted = "0"` AND source `ClientId < 0`                         |
| **1**    | `IsDeleted = "0"` AND source `ClientId >= 0`                        |


### Pre-pass

Same soft-reset as `FormQuestionAnswers` (per `ctrl.tbl_Forms2Process` rules),
but `**SaveChanges` is called immediately** after pre-pass before the upsert loop.
Per-row `try/catch` wraps the pre-pass.

### Load Path

**EF Core only** — no Bulk path for this table.

### Notable Column Transformations


| Column          | Transformation                                                              |
| --------------- | --------------------------------------------------------------------------- |
| `FormId`        | `UPPER()`                                                                   |
| `ClientId`      | `Math.Abs()` — stored positive regardless of source sign                    |
| `RowChkSum`     | Stored but comparison **commented out** — always overwrites signature dates |
| Signature dates | Length checked `> 6` before parsing — avoids sentinel/empty parse errors    |


### BAMMerge

**NO** — `pats.BAMMerge` is NOT called after `FormAnswerSignatures`.

> **Verification question:** Why is `RowChkSum` comparison commented out for
> `FormAnswerSignatures`? Was this intentional (always refresh signatures) or a bug?

---

## Table 3 — `pats.tbl_eandmformmdm`

### Source JOINs (SAMMS)

```sql
SELECT a.*, c.*
FROM dbo.EandMForm a
  LEFT JOIN  dbo.EandMFormMDM c            ON a.ID = c.EandMFormID
  INNER JOIN dbo.SF_PatientPreAdmission b  ON a.PreAdmissionID = b.ID
```

### Date Filter (WHERE)

**None — WHERE clause is commented out.**
Full table is pulled on every run. No date window applied.

### `ctrl.tbl_Forms2Process`

**Not used** — this table has no Forms2Process dependency.

### Upsert Lookup Key

`Id` (single column — source form ID)

### RowState Logic

**No `RowState` column** on this table.
`Isdeleted` (bool) is stored: source `"1"` → `true`, anything else → `false`.

### Pre-pass

**None.**

### Load Path

**EF Core only.**

### Notable Column Transformations


| Column                     | Transformation                              |
| -------------------------- | ------------------------------------------- |
| `SiteCode`                 | From source row                             |
| `Isdeleted`                | Source `"1"` → bool `true`                  |
| `CreatedBy` / `ModifiedBy` | Always written — can be empty string        |
| Dates                      | Length `> 6` check before parsing           |
| `RowChkSum`                | **Not present** — no checksum on this table |


### BAMMerge

**NO.**

> **Verification question:** Since there is no date filter, the full `EandMFormMDM`
> table is pulled every night for every site. Is this intentional?
> How large does this table typically get per site?

---

## Table 4 — `pats.tbl_eandmformpregnancy`

### Source JOINs (SAMMS)

```sql
SELECT a.*, c.*, [IsDeleted] = CASE WHEN a.IsDeleted = 1 THEN '1' ELSE '0' END
FROM dbo.EandMForm a
  INNER JOIN dbo.EandMFormPregnancy c      ON a.ID = c.EandMFormID
  INNER JOIN dbo.SF_PatientPreAdmission b  ON a.PreAdmissionID = b.ID
```

**Note:** Uses `INNER JOIN` to `EandMFormPregnancy` — only forms that have a pregnancy
record are included (unlike MDM which uses `LEFT JOIN`).

### Date Filter (WHERE)

**None — WHERE clause is commented out.** Full table every run.

### `ctrl.tbl_Forms2Process`

**Not used.**

### Upsert Lookup Key

`EandMFormId` (mapped from `a.ID`)

### RowState Logic

**No `RowState` column.** `Isdeleted` bool only.

### Pre-pass

**None.**

### Load Path

**EF Core only.**

### Notable Column Transformations


| Column               | Transformation                                            |
| -------------------- | --------------------------------------------------------- |
| `LastModAt`          | Set to `**runDate`** (run date, not source modified date) |
| `prenatalvitiamstxt` | Source column has a **typo** — must match exactly         |
| Checkbox fields      | `bool.Parse()` — source must be `"True"/"False"`          |
| Dates                | Length `> 6` check                                        |


### BAMMerge

**NO.**

> **Verification question:** `LastModAt` is set to the ETL run date, not the actual
> source `ModifiedDate`. Is this by design, or should it reflect the source value?

---

## Table 5 — `pats.tbl_comprehensiveassessmentform`

### Source (SAMMS)

Source table / view name comes from the **task row** (`st.FromTblVw` from `dms.tbl_MapSrc2Dsn`),
not hardcoded. The `WHERE` clause also comes from the task row (`st.WhereCondition`).

```sql
SELECT [columns from dms.tbl_MapSrc2Dsn]
FROM [st.FromTblVw]               -- metadata-driven source table
WHERE [st.WhereCondition]         -- metadata-driven WHERE
ORDER BY [st.SortOrder]
```

### Date Filter (WHERE)

Driven by `st.WhereCondition` from the task metadata.
The `wrkdt` variable inside `SaveComprehensiveAssessmentForm` is **not used**.

### `ctrl.tbl_Forms2Process`

**Not used.**

### Upsert Lookup Key

`SiteCode + Id`

### RowState Logic (bool)


| RowState                | Condition                                                                  |
| ----------------------- | -------------------------------------------------------------------------- |
| `true` (default active) | Set in `sitecode` branch — also sets `RowChkSum` and `LastModAt`           |
| Matches `IsDeleted`     | `isdeleted` branch: `RowState = IsDeleted = bool.Parse(source)`            |
| **Inverted sense:**     | `IsDeleted = true` → `RowState = true` (both match); active = both `false` |


> **Verification question:** The `RowState` / `IsDeleted` pairing here seems inverted
> compared to other tables (`RowState=true` normally means active, but here it mirrors
> `IsDeleted=true`). Is this intentional? A deleted record has `RowState=true`?

### Pre-pass

**None.**

### Load Path

**EF Core only.**

### Notable Column Transformations


| Column                    | Transformation                                                     |
| ------------------------- | ------------------------------------------------------------------ |
| `RowChkSum`               | Stored from source but **never compared** — always overwrites      |
| Per-column error handling | Each column wrapped in `try/catch` — parse errors silently skipped |
| 100+ columns              | Extensive — bool / int / DateTime / string parsing per field       |


### BAMMerge

**NO.**

---

## Cross-Reference Summary — All 5 Tables


| #   | Destination Table                      | Scheduled Task?    | SAMMS Source Tables                                                                                                                                                                                                          | JOIN Type                                                                                                                                                                                                                         | ctrl.tbl_Forms2Process                                           | Load Path                                 | Pre-pass                                                        | BAMMerge |
| --- | -------------------------------------- | ------------------ | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------- | ----------------------------------------- | --------------------------------------------------------------- | -------- |
| 1   | `pats.tbl_dbo_FormQuestionAnswers`     | **Yes** (task row) | `dbo.Form` (base) `dbo.FormTemplate` `dbo.Question` `dbo.Answer` `dbo.SF_PatientPreAdmission` `dbo.SF_DataForms` + extra tables per config (e.g. `dbo.tblTP17REVIEW`, `dbo.tblORDERREQ`)                                     | Form→FormTemplate: LEFT JOIN FormTemplate→Question: LEFT JOIN Form→Answer: LEFT JOIN (on FormId + QuestionId) Form→SF_PatientPreAdmission: INNER JOIN PA→SF_DataForms: LEFT JOIN config tables→SF_PatientPreAdmission: INNER JOIN | **Yes** — drives extra UNION arms + pre-pass date reset per form | **Bulk** (18 sites) / **EF** (all others) | **Yes** — soft-reset RowState=0 per Forms2Process rules         | **YES**  |
| 2   | `pats.tbl_dbo_FormAnswerSignatures`    | **Yes** (task row) | `dbo.Form` `dbo.FormTemplate` `dbo.SF_PatientPreAdmission` `dbo.SF_DataForms` `dbo.AnswerSignature` (9× correlated subquery, one per signature slot) + extra tables per config (e.g. `dbo.tblTP17REVIEW`, `dbo.tblORDERREQ`) | Form→FormTemplate: LEFT JOIN Form→SF_PatientPreAdmission: INNER JOIN PA→SF_DataForms: LEFT JOIN AnswerSignature: correlated subquery (TOP 1 ORDER BY DateTime DESC) config tables→SF_PatientPreAdmission: INNER JOIN              | **Yes** — drives extra UNION arms + pre-pass date reset          | **EF only**                               | **Yes** — soft-reset RowState=0 (SaveChanges immediately after) | **NO**   |
| 3   | `pats.tbl_eandmformmdm`                | **No** (inline)    | `dbo.EandMForm` `dbo.EandMFormMDM` `dbo.SF_PatientPreAdmission`                                                                                                                                                              | EandMForm→EandMFormMDM: LEFT JOIN EandMForm→SF_PatientPreAdmission: INNER JOIN                                                                                                                                                    | **No**                                                           | **EF only**                               | **No**                                                          | **NO**   |
| 4   | `pats.tbl_eandmformpregnancy`          | **No** (inline)    | `dbo.EandMForm` `dbo.EandMFormPregnancy` `dbo.SF_PatientPreAdmission`                                                                                                                                                        | EandMForm→EandMFormPregnancy: **INNER JOIN** (only forms with a pregnancy record) EandMForm→SF_PatientPreAdmission: INNER JOIN                                                                                                    | **No**                                                           | **EF only**                               | **No**                                                          | **NO**   |
| 5   | `pats.tbl_comprehensiveassessmentform` | **No** (inline)    | Source table/view comes from task metadata (`dms.tbl_MapSrc2Dsn` → `st.FromTblVw`). Typically a SAMMS view or table covering comprehensive assessment data.                                                                  | Defined in `dms.tbl_MapSrc2Dsn` — not hardcoded                                                                                                                                                                                   | **No**                                                           | **EF only**                               | **No**                                                          | **NO**   |


---

## Execution Order Per Site (one site at a time — sequential)

```
For each site (e.g. B01):
  │
  ├── 1. Process task: pats.tbl_dbo_FormQuestionAnswers
  │         ├── Build UNION query (base F/Q/A + Forms2Process arms)
  │         ├── Existence check: sys.tables for each extra SAMMS table
  │         ├── Pull data from SAMMS (wrkdt = -30 days)
  │         ├── Pre-pass: soft-reset Azure rows (per Forms2Process DateFilterEnabled)
  │         ├── Upsert: Bulk (18 sites) OR EF (all others)
  │         └── EXEC pats.BAMMerge @sitecode = 'B01'   ← always runs
  │
  ├── 2. Process task: pats.tbl_dbo_FormAnswerSignatures
  │         ├── Build UNION query (base Form + 9 signature correlated subqueries + Forms2Process arms)
  │         ├── Pull data from SAMMS
  │         ├── Pre-pass: soft-reset Azure rows (SaveChanges immediately)
  │         └── EF upsert
  │
  ├── 3. Inline: pats.tbl_eandmformmdm
  │         ├── Pull full EandMForm + EandMFormMDM (NO date filter)
  │         └── EF upsert (no pre-pass, no BAMMerge)
  │
  ├── 4. Inline: pats.tbl_eandmformpregnancy
  │         ├── Pull full EandMForm + EandMFormPregnancy (NO date filter)
  │         └── EF upsert (no pre-pass, no BAMMerge)
  │
  └── 5. Inline: pats.tbl_comprehensiveassessmentform
            ├── Pull from metadata-driven source (strWhere from task row)
            └── EF upsert (no pre-pass, no BAMMerge)

  → Move to next site (B02, B03, ... until all ~115 sites done)
```

---

## Known Issues / Open Questions for Developer


| #   | Area                                | Question                                                                                                                                              |
| --- | ----------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | BAMMerge                            | What exactly does `pats.BAMMerge` recalculate? Which tables does it write to? Typical runtime per site?                                               |
| 2   | BAMMerge scope                      | Why does BAMMerge run only after `FormQuestionAnswers` and not after `FormAnswerSignatures`?                                                          |
| 3   | EandMForm date filter               | The WHERE clause for `tbl_eandmformmdm` and `tbl_eandmformpregnancy` is commented out — full load every run. Is this intentional?                     |
| 4   | RowState on ComprehensiveAssessment | `RowState=true` mirrors `IsDeleted=true` — this appears inverted vs all other tables. Is this by design?                                              |
| 5   | AnswerSignatures RowChkSum          | `RowChkSum` comparison is commented out — always overwrites. Intentional or bug?                                                                      |
| 6   | Forms2Process UNION arms            | If a SAMMS table from `tbl_Forms2Process.TableName` does not exist at a clinic, is the UNION arm silently skipped via `sys.tables` check?             |
| 7   | FormId construction                 | What is the exact logic for constructing `FormId` in UNION arms (e.g. tblTP17REVIEW)? Is the `Prefix` column used as a concatenation prefix?          |
| 8   | Inline vs task rows                 | Tables 3, 4, 5 run inline without separate task rows — if they fail, does the error appear on the `FormQuestionAnswers` task row, or is it swallowed? |
| 9   | pats.BAMMergeGbl                    | This runs in the `SAMMSGlobal` pipeline (Schedule 1) — is it a separate aggregation from `pats.BAMMerge`? Does one depend on the other running first? |
| 10  | PreAdmissionId = -1                 | When `PreAdmissionId` is empty in source, it is stored as `-1` in Azure. Are there downstream reports that filter out `PreAdmissionId = -1`?          |


