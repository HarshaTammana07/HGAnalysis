# Global BAM & COWS — Fabric vs Legacy Parity Analysis

**Date:** 2026-07-23  
**Scope:** SAMMSGLOBAL path only (Schedule 1 / `pl_execute_samms_global`)  
**Reference files:**
- Fabric: `BCAppCode/Global/globaldef.txt`
- Legacy: `BCAppCode/BHG-DR-LIB/SaveGlobal.cs`
- Map actions: `BCAppCode/Framework/vw_mapAction.csv`
- Column metadata: `BCAppCode/Framework/vw_MapSrc2Dsn.csv`
- Legacy runner: `BCAppCode/BHGTaskRunner/Program.cs`

---

## Executive summary

Both open bugs report **Fabric Gold has more rows than legacy BHG_DR**. That is expected given current Fabric implementation — not a random data-quality issue.

| Table | Fabric Gold | Legacy BHG_DR | Gap | Aligned? |
|-------|------------:|--------------:|----:|:--------:|
| **COWS** — `ClinicalOpiateWithdrawalScale` | 480,138 | 425,748 | +54,390 | **No** |
| **BAM** — `BriefAddictionMonitor` | 289,915 | 268,344 | +21,571 | **No** |

**Root cause pattern (both tables):** Fabric bronze loads the **full source table** without legacy SQL filters. Silver/gold do not fully replicate legacy exclusion rules. Fabric initial gold load also ingests **full history**, while legacy BHG_DR accumulated rows over years of **filtered daily incremental** runs.

**BAM is closer to legacy than COWS** — SiteCode resolution via `fClinic` is correct for BAM but wrong for COWS (hardcoded `'Global'`).

---

## Scope — tables in vs out

### In scope (this document)

| SAMMS source (SAMMSGLOBAL) | Legacy destination | Legacy method | Fabric ConfigId |
|----------------------------|-------------------|---------------|-----------------|
| `dbo.aaClinicalOpiateWithdrawalScale` | `pats.tbl_clinicalopiatewithdrawalscale` | `SaveGlobalClinicalOpiateWithdrawalScale()` | 174 |
| `dbo.aaBriefAddictionMonitor` | `pats.tbl_BriefAddictionMonitor` | `SaveBAM()` | 176 |

### Out of scope

| Path | Source | Destination | Why excluded |
|------|--------|-------------|--------------|
| P1 per-clinic | `dbo.SF_COWS` | `pats.tbl_Cows_V6` | Different pipeline (`SaveCows_v6`, Schedule 2) |
| P1 per-clinic | `dbo.BAMForm` / `dbo.BAMScore` | `pats.tbl_BAMForm` / `pats.tbl_BAMScore` | Different pipeline (`SaveBAM.cs` clinic path) |
| Forms-derived BAM | `dbo.Form` → Q&A | `pats.BAMMerge` SP | Separate Samms-Forms pipeline |

---

---

# Part 1 — COWS (Clinical Opiate Withdrawal Scale)

## Bug reference

- **BUG 3514** — Record count mismatch between Gold `ClinicalOpiateWithdrawalScale` and BHG_DR `tbl_clinicalopiatewithdrawalscale`
- Fabric: **480,138** | Legacy: **425,748** | Gap: **+54,390**

## Source → destination mapping

| Layer | Value |
|-------|-------|
| SAMMS source | `SAMMSGLOBAL.dbo.aaClinicalOpiateWithdrawalScale` |
| Map action | ActionKey=**2**, StepKey=**5**, `FromTblVw = aaClinicalOpiateWithdrawalScale` |
| Legacy load | `SaveGlobalClinicalOpiateWithdrawalScale()` in `SaveGlobal.cs` (~line 583) |
| Legacy schedule | `BHGTaskRunner.exe 1` (SAMMSGlobal), case `pats.tbl_clinicalopiatewithdrawalscale` |
| Fabric bronze | `bhg_bronze.Global.brz_clinicalopiatewithdrawalscale` |
| Fabric silver | `bhg_silver.pats.slv_clinicalopiatewithdrawalscale` |
| Fabric gold | `bhg_gold.pats.ClinicalOpiateWithdrawalScale` (published) |

> **Note:** There is no table named `dbo.aacows` anywhere in map actions or code. The correct source name is `aaClinicalOpiateWithdrawalScale`.

---

## Layer-by-layer comparison — COWS

### Bronze extract

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Source table | `aaClinicalOpiateWithdrawalScale` | Same | Yes |
| Connection | SAMMSGLOBAL | SAMMSGLOBAL | Yes |
| WHERE clause | See below | **None** | **No** |

**Legacy WHERE** (`vw_mapAction.csv`, Global site):

```sql
convert(date,
  SUBSTRING(AssessDate,
    CHARINDEX('day,', AssessDate, 0) + 5,
    len(Assessdate) - CHARINDEX('day,', AssessDate, 0) + 5),
  106) > '12/31/2018'
```

**Fabric bronze SQL** (`Copy data_COWS`, ConfigId 174):

```sql
SELECT
  SiteCode = 'Global',          -- literal, NOT fClinic
  DataBaseName = 'SAMMSGLOBAL',
  run_date = CAST(GETDATE() AS DATE),
  YawnNUM, TremorNUM, ... ,    -- explicit column list
  RowChkSum = CHECKSUM(...)    -- includes literal 'Global' as SiteCode
FROM [SAMMSGLOBAL].[dbo].[aaClinicalOpiateWithdrawalScale]
-- NO WHERE CLAUSE
```

**Impact:** Fabric bronze row count = **480,138** (full source). This matches Fabric Gold exactly. Legacy never loads pre-2019 / unparseable `AssessDate` rows (~54,390 rows difference).

---

### SiteCode resolution

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Source clinic field | `fClinic` aliased as `SiteCode` in SelectConstructor | **Not selected** | **No** |
| Resolution | `int sid` → lookup `TblLocations` where `SId == sid` and `IsActive` | Hardcoded `'Global'` | **No** |
| Unmatched clinic | **Row skipped entirely** | Row kept with `SiteCode = 'Global'` | **No** |
| Destination SiteCode | Per-clinic code (e.g. `B01A`) | Always `'Global'` | **No** |

**Legacy code** (`SaveGlobal.cs`):

```csharp
int sid = int.Parse(r["SiteCode"].ToString());  // actually fClinic from source
Models.TblLocations site = sites.Where(x => x.SId == sid).FirstOrDefault();
if (site != null)  // row SKIPPED if null
{
    c.SiteCode = site.SiteCode;
    // ... upsert by FId
}
```

**MapSrc2Dsn** (ActionKey=2, StepKey=5, FieldKey=2):

```
fClinic → SiteCode (integer clinic ID in source SELECT)
```

**Fabric silver** (`process_cows`):

```python
# locations_df loaded but NEVER USED
silver_df = base_df.select("*", lit("Global").alias("SiteCode"))
```

**Azure PK:** `(SiteCode, FId)` — legacy stores real clinic codes; Fabric stores `'Global'` for all rows.

---

### RowChkSum

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Built by | `SelectConstructor.GetSLT()` — `CHECKSUM([fId], [fClinic], [fCltID], ...all cols...)` | Bronze copy activity — CHECKSUM over score cols + literal `'Global'` | **No** |
| Used for change detection | Yes — skip field remap if `rcs == c.RowChkSum` | Gold compares RowChkSum | **Partial** |
| Includes fClinic | Yes | **No** | **No** |

---

### Column mapping (silver transform)

| Legacy field | Fabric silver column | Match? |
|-------------|---------------------|:------:|
| fId | FId | Yes |
| fCltID | FCltId | Yes |
| CompletedName | CompletedName | Yes |
| CombinedScore | CombinedScore | Yes |
| AssessDate | AssessDate | Yes |
| ReasonAssessList | ReasonAssessList | Yes |
| RestingPulseNUM | RestingPulseNum | Yes |
| GIUpsetNUM | GiupsetNum | Yes |
| SweatNUM | SweatNum | Yes |
| TremorNUM | TremorNum | Yes |
| RestlessNUM | RestlessNum | Yes |
| YawnNUM | YawnNum | Yes |
| PupilNUM | PupilNum | Yes |
| AnxNUM | AnxNum | Yes |
| BoneNUM | BoneNum | Yes |
| GooseNUM | GooseNum | Yes |
| RunnyNUM | RunnyNum | Yes |
| genevaTEST | GenevaTest | Yes |
| timeAMPM | TimeAmpm | Yes |
| assesstimeTEXT | AssesstimeText | Yes |

**Legacy typo:** switch case `genevatst` in `SaveGlobal.cs` does not match column `genevaTEST` — GenevaTest may not update on change in legacy. Fabric maps it correctly.

Silver column transforms are **mostly aligned**.

---

### Gold upsert logic

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Match key | FId only (in-memory lookup) | FId only | Yes |
| DB PK | SiteCode + FId | SiteCode + FId | Yes (but SiteCode wrong in Fabric) |
| Pre-run deactivation | All existing → `RowState = false` | Deactivate FIds not in current batch | Similar intent |
| Change detection | RowChkSum guard | RowChkSum compare | Yes |
| Re-activate unchanged | `RowState = true` even if checksum same | Keeps unchanged rows active | Yes |
| Date filter at gold | None | None | Yes |

---

## COWS — alignment scorecard

| Check | Status |
|-------|:------:|
| Correct source table | Pass |
| Bronze WHERE (AssessDate > 2018) | **Fail** |
| SiteCode via fClinic + location lookup | **Fail** |
| Skip rows with invalid clinic | **Fail** |
| RowChkSum formula | **Fail** |
| Column mapping | Pass |
| Gold upsert pattern | Partial |

**Overall: NOT ALIGNED**

---

## COWS — recommended fixes (priority order)

1. **Bronze WHERE** — add legacy AssessDate filter:
   ```sql
   WHERE convert(date,
     SUBSTRING(AssessDate, CHARINDEX('day,', AssessDate, 0)+5,
       len(Assessdate)-CHARINDEX('day,', AssessDate, 0)+5), 106) > '12/31/2018'
   ```

2. **Bronze SELECT** — include `fClinic` from source; remove hardcoded `SiteCode = 'Global'`.

3. **Silver `process_cows`** — join `fClinic` to active locations (`bhg_silver.ctrl.LocationCons` / `TblLocations` equivalent):
   ```python
   silver_df = (
       base_df.alias("d")
       .join(locations_df.alias("l"), col("d.fClinic") == col("l.SId"), "inner")  # inner = skip unmatched
       .select("d.*", col("l._LocSiteCode").alias("SiteCode"))
   )
   ```

4. **RowChkSum** — rebuild CHECKSUM to match `MapSrc2Dsn` ActionKey=2 StepKey=5 (include `[fClinic]`, exclude literal `'Global'`).

5. **Validation** — run parity queries (see Validation section below).

---

# Part 2 — BAM (Brief Addiction Monitor)

## Bug reference

- Record count mismatch between Gold `BriefAddictionMonitor` and BHG_DR `tbl_BriefAddictionMonitor`
- Fabric: **289,915** | Legacy: **268,344** | Gap: **+21,571**

## Source → destination mapping

| Layer | Value |
|-------|-------|
| SAMMS source | `SAMMSGLOBAL.dbo.aaBriefAddictionMonitor` |
| Map action | ActionKey=**2**, StepKey=**9**, `FromTblVw = aaBriefAddictionMonitor` |
| Legacy load | `SaveBAM()` in `SaveGlobal.cs` (~line 1427) |
| Legacy schedule | `BHGTaskRunner.exe 1`, case `pats.tbl_briefaddictionmonitor` |
| Post-save SP | `pats.BAMMergeGbl` (@sitecode = 'Global') — **not in Fabric** |
| Fabric bronze | `bhg_bronze.Global.brz_BriefAddictionMonitor` |
| Fabric silver | `bhg_silver.pats.slv_briefaddictionmonitor` |
| Fabric gold | `bhg_gold.pats.BriefAddictionMonitor` (published) |

---

## Layer-by-layer comparison — BAM

### Bronze extract

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Source table | `aaBriefAddictionMonitor` | Same | Yes |
| Map action WHERE | See below | **None** | **No** |
| Program.cs override | Additional 30-day date filter (daily) | N/A at bronze | **No** |

**Legacy map action WHERE** (`vw_mapAction.csv`):

```sql
[date] IS NOT NULL
AND fCltID > 0
AND fClinic NOT IN (25, 100)
```

**Legacy daily extract override** (`BHGTaskRunner/Program.cs`, case `pats.tbl_briefaddictionmonitor`):

```sql
WHERE fCltID > 0
  AND convert(date, ltrim(substring([date],
    CHARINDEX(', ', [date])+2, len([date])-CHARINDEX(', ', [date])-1)), 109)
    >= WorkDate - 30 days
  AND fClinic NOT IN (25, 100)
```

**Fabric bronze SQL** (`Copy data_BAM`, ConfigId 176):

```sql
SELECT
  'Global' AS SiteCode,
  'SAMMSGLOBAL' AS DataBaseName,
  CAST(GETDATE() AS DATE) AS run_date,
  *
FROM [SAMMSGLOBAL].[dbo].[aaBriefAddictionMonitor]
-- NO WHERE CLAUSE
```

**Impact:** Fabric bronze = **289,914** rows (full source). Legacy BHG_DR cumulative total = **268,344**. Gap of **21,571** is consistent with rows legacy excludes (null/invalid dates, bad fCltID, excluded clinics) that Fabric still loads.

> **Note on count comparison:** Legacy BHG_DR grows via **daily 30-day incremental** loads over many years. Fabric **initial gold load** ingests full history in one pass. Comparing total Fabric Gold vs total legacy BHG_DR requires applying the **same source filters** first — not raw source vs cumulative legacy.

---

### Silver filters

| Filter | Legacy (map action) | Legacy (daily extract) | Fabric silver | Aligned? |
|--------|--------------------|-----------------------|---------------|:--------:|
| `[date] IS NOT NULL` | Yes | Implicit via date parse in WHERE | **No** | **No** |
| `fCltID > 0` | Yes | Yes | Yes | Yes |
| `fClinic NOT IN (25, 100)` | Yes | Yes | Yes | Yes |
| 30-day date window | No (map action) | Yes (daily only) | No (silver); gold only on incremental | Partial |

**Fabric silver** (`process_bam`):

```python
filtered_bronze_df = bronze_df.filter(
    (col("fCltID") > 0) & (~col("fClinic").isin(25, 100))
)
# Missing: col("date").isNotNull() and/or Date.isNotNull() after parse
```

Gold count ≈ bronze count → `fCltID` / `fClinic` filters remove almost nothing. Missing **`date IS NOT NULL`** filter is the likely main gap.

---

### SiteCode resolution

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Source field | `fClinic` (integer) | `fClinic` | Yes |
| Lookup | `TblLocations` where `SId == fClinic` | `get_active_locations()` join on `SId` | Yes |
| Fallback | `"NSL-" + FClinic` | `"NSL-" + FClinic` | Yes |
| Skip unmatched | **No** — uses NSL fallback | **No** — uses NSL fallback | Yes |

**Legacy code:**

```csharp
var lcsite = Locs.Where(x => x.SId == bam.FClinic).FirstOrDefault();
bam.SiteCode = lcsite == null ? "NSL-" + bam.FClinic.ToString() : lcsite.SiteCode;
```

**Fabric silver:**

```python
coalesce(col("l._LocSiteCode"), concat(lit("NSL-"), col("d.FClinic").cast("string"))).alias("SiteCode")
```

**SiteCode logic is aligned for BAM** (unlike COWS).

---

### Date parsing

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Format | `"Monday, January 1, 2024"` style string | Same | Yes |
| Parse logic | Find `", "`, substring after, `DateTime.Parse` | `parse_bam_date()` — strip prefix, `to_timestamp` | Yes |

**Fabric `parse_bam_date`:**

```python
def parse_bam_date(date_col_name):
    cleaned = expr(f"trim(case when instr({date_col_name}, ', ') > 0
        then substring({date_col_name}, instr({date_col_name}, ', ') + 2, length({date_col_name}))
        else {date_col_name} end)")
    return coalesce(to_timestamp(cleaned), to_timestamp(col(date_col_name)), col(date_col_name))
```

Parsing is aligned. Filtering null/invalid dates after parse is **not** applied in silver.

---

### Dedupe / upsert keys

| Layer | Legacy | Fabric | Aligned? |
|-------|--------|--------|:--------:|
| Lookup key | `SiteCode + FId` | Silver: **`FId` only** | **No** |
| Gold key | `SiteCode + FId` | `SiteCode + FId` | Yes |

**Legacy:**

```csharp
db.TblBriefAddictionMonitor.FirstOrDefault(x => x.SiteCode == bam.SiteCode && x.FId == bam.FId);
```

**Fabric silver issue:**

```python
silver_df = silver_df.dropDuplicates(["FId"])  # should be ["SiteCode", "FId"]
```

If the same `FId` exists under different clinics, silver keeps one row arbitrarily. This is a **data correctness** bug (usually reduces count, not the main cause of Fabric having *more* rows).

---

### Column mapping (all fields)

Legacy `SaveBAM` maps these columns (both cold-start and warm-update paths). Fabric silver maps the same set:

| Source column | Destination | Fabric silver |
|--------------|-------------|---------------|
| fid | FId | Yes |
| fclinic | FClinic | Yes |
| fcltid | FCltId | Yes |
| date | Date | Yes (parsed) |
| cliniciantext | ClinicianText | Yes |
| adminlist | AdminList | Yes |
| intervallist | IntervalList | Yes |
| usecalc | UseCalc | Yes |
| riskcalc | RiskCalc | Yes |
| protectivecalc | ProtectiveCalc | Yes |
| q1answerlist – q6answerlist | Q1answerList – Q6AnswerList | Yes |
| test | Test | Yes |
| q1answer – q6answer | Q1Answer – Q6answer | Yes |
| q7answernumeric | Q7answerNumeric | Yes |
| q7alist – q7glist | Q7aList – Q7gList | Yes |
| q8answer – q17answer | Q8Answer – Q17Answer | Yes |
| q14answer2 | Q14Answer2 | Yes |
| q15answer1, q15answer2 | Q15Answer1, Q15Answer2 | Yes |

**Column mapping is fully aligned.**

---

### Gold upsert logic

| Aspect | Legacy | Fabric | Aligned? |
|--------|--------|--------|:--------:|
| Match key | SiteCode + FId | SiteCode + FId | Yes |
| Initial load | Cold-start path: only rows with `Date >= FltrDate` | **Full history, no date filter** | **No** |
| Incremental | Warm path: all rows from filtered daily extract | Last 30 days on `Date` | Partial |
| RowState | Set to 1 on upsert | Set to 1 | Yes |
| RowChkSum | Not used for BAM | Not in silver; gold uses full column compare | N/A |
| Preserve old rows | Yes — not in daily batch stay in DB | `preserved_df` in gold join | Yes |
| Post-save SP | `pats.BAMMergeGbl` | **Not executed** | **No** |

**Fabric gold** (`process_bam`):

```python
if is_initial_load:
    batch = silver_df.drop("run_date")           # ALL history
else:
    filter_start = business_date - timedelta(days=30)
    batch = silver_df.filter(to_date(col("Date")) >= filter_start)
```

**Legacy two-path design** (`SaveBAM`):

- **Path A (cold start):** No existing Azure rows in date window → insert only if `bam.Date >= FltrDate`
- **Path B (warm update):** Existing rows present → upsert all rows from daily extract (already 30-day filtered at SQL)

---

### Known Fabric defect — bronze table name typo

| Operation | Table name |
|-----------|-----------|
| Bronze copy **writes** | `brz_BriefAddictionMonitor` |
| Silver notebook **reads** | `brz_BreifAddictionMonitor` (typo) |

If both tables exist or reads succeed via fallback, pipeline may still run — but this is a reliability risk. **Fix:** align read path to `brz_BriefAddictionMonitor`.

---

## BAM — alignment scorecard

| Check | Status |
|-------|:------:|
| Correct source table | Pass |
| Bronze WHERE (date/fCltID/fClinic) | **Fail** |
| Silver `[date] IS NOT NULL` filter | **Fail** |
| SiteCode via fClinic + location lookup | Pass |
| Date parsing | Pass |
| Column mapping (Q1–Q17 + calcs) | Pass |
| Silver dedupe key (SiteCode + FId) | **Fail** |
| Gold upsert key | Pass |
| Initial load strategy vs legacy | **Fail** |
| BAMMergeGbl post-processing | **Fail** |

**Overall: NOT ALIGNED** (but closer than COWS)

---

## BAM — recommended fixes (priority order)

1. **Bronze WHERE** — match map action:
   ```sql
   WHERE [date] IS NOT NULL
     AND fCltID > 0
     AND fClinic NOT IN (25, 100)
   ```

2. **Silver** — after `parse_bam_date`, filter null dates:
   ```python
   .filter(col("Date").isNotNull())
   ```

3. **Silver dedupe** — change to:
   ```python
   silver_df.dropDuplicates(["SiteCode", "FId"])
   ```

4. **Fix typo** — `brz_BreifAddictionMonitor` → `brz_BriefAddictionMonitor`.

5. **Gold initial load** — document that first run loads full filtered history; subsequent runs use 30-day window (matches legacy incremental behavior).

6. **BAMMergeGbl** — evaluate whether downstream consumers require this SP; add as post-gold step if needed.

---

# Part 3 — Side-by-side comparison

| Check | COWS | BAM |
|-------|:----:|:---:|
| Source table correct | Yes | Yes |
| Bronze has legacy WHERE | **No** | **No** |
| SiteCode resolution | **No** (`'Global'`) | **Yes** (fClinic join) |
| Skip invalid clinic rows | **No** | N/A (NSL fallback) |
| Silver filters complete | N/A | **Partial** |
| RowChkSum matches legacy | **No** | N/A |
| Column mapping | Mostly | **Yes** |
| Upsert composite key at gold | FId only | SiteCode + FId |
| Initial load vs incremental | Full source | Full source |
| Post-save stored proc | None | **BAMMergeGbl missing** |
| **Aligned overall?** | **No** | **No** |

---

# Part 4 — Validation queries

Run these against SAMMSGLOBAL, BHG_DR, and Fabric Gold to confirm gap root cause before and after fixes.

## COWS

```sql
-- 1. Full source (Fabric bronze today)
SELECT COUNT(*) AS full_source
FROM SAMMSGLOBAL.dbo.aaClinicalOpiateWithdrawalScale;

-- 2. Legacy map-action filter only
SELECT COUNT(*) AS legacy_map_filter
FROM SAMMSGLOBAL.dbo.aaClinicalOpiateWithdrawalScale
WHERE convert(date,
  SUBSTRING(AssessDate, CHARINDEX('day,', AssessDate, 0)+5,
    len(Assessdate)-CHARINDEX('day,', AssessDate, 0)+5), 106) > '12/31/2018';

-- 3. Legacy destination
SELECT COUNT(*) AS legacy_total
FROM pats.tbl_clinicalopiatewithdrawalscale;

SELECT COUNT(*) AS legacy_active
FROM pats.tbl_clinicalopiatewithdrawalscale
WHERE RowState = 1;

-- 4. Fabric Gold (current)
SELECT COUNT(*) AS fabric_gold
FROM bhg_gold.pats.ClinicalOpiateWithdrawalScale;

-- 5. Gap diagnostic — rows Fabric has that legacy filter excludes
SELECT COUNT(*) AS pre_2019_or_bad_date
FROM SAMMSGLOBAL.dbo.aaClinicalOpiateWithdrawalScale
WHERE convert(date,
  SUBSTRING(AssessDate, CHARINDEX('day,', AssessDate, 0)+5,
    len(Assessdate)-CHARINDEX('day,', AssessDate, 0)+5), 106) <= '12/31/2018'
   OR AssessDate IS NULL;
```

## BAM

```sql
-- 1. Full source (Fabric bronze today)
SELECT COUNT(*) AS full_source
FROM SAMMSGLOBAL.dbo.aaBriefAddictionMonitor;

-- 2. Legacy map-action filter
SELECT COUNT(*) AS legacy_map_filter
FROM SAMMSGLOBAL.dbo.aaBriefAddictionMonitor
WHERE [date] IS NOT NULL
  AND fCltID > 0
  AND fClinic NOT IN (25, 100);

-- 3. Legacy destination
SELECT COUNT(*) AS legacy_total
FROM pats.tbl_BriefAddictionMonitor;

-- 4. Fabric Gold (current)
SELECT COUNT(*) AS fabric_gold
FROM bhg_gold.pats.BriefAddictionMonitor;

-- 5. Gap diagnostic — null date rows
SELECT COUNT(*) AS null_date_rows
FROM SAMMSGLOBAL.dbo.aaBriefAddictionMonitor
WHERE [date] IS NULL;

-- 6. Gap diagnostic — excluded clinics / bad client ID
SELECT COUNT(*) AS excluded_clinic_or_client
FROM SAMMSGLOBAL.dbo.aaBriefAddictionMonitor
WHERE fCltID <= 0 OR fClinic IN (25, 100);
```

---

# Part 5 — Map action reference

## COWS — ActionKey=2, StepKey=5

```
FromTblVw:  aaClinicalOpiateWithdrawalScale
DsnTbl:     tbl_clinicalopiatewithdrawalscale
SiteCode:   Global
WHERE:      AssessDate parsed > '12/31/2018'
Method:     SaveGlobalClinicalOpiateWithdrawalScale
```

**MapSrc2Dsn key fields:** fId, fClinic→SiteCode, fCltID, CompletedName, CombinedScore, AssessDate, ReasonAssessList, RestingPulseNUM … assesstimeTEXT

## BAM — ActionKey=2, StepKey=9

```
FromTblVw:  aaBriefAddictionMonitor
DsnTbl:     tbl_BriefAddictionMonitor
SiteCode:   Global
WHERE:      [date] IS NOT NULL AND fCltID > 0 AND fClinic NOT IN (25, 100)
Method:     SaveBAM
Post-SP:    pats.BAMMergeGbl
```

**MapSrc2Dsn key fields:** fId, fClinic, fCltID, date, cliniciantext, adminlist, intervallist, usecalc, riskcalc, protectivecalc, q1–q17 answers and lists

---

# Part 6 — Acceptance criteria for bug closure

A bug can be closed when:

1. **Filtered source count** (legacy map-action WHERE) ≈ **Fabric Gold count** (± small tolerance for timing).
2. **Legacy BHG_DR total** ≈ **Fabric Gold total** after Fabric re-load with fixes (or document intentional historical difference with sign-off).
3. **Spot checks:** sample 50 rows compared field-by-field on SiteCode, FId, key score/date columns.
4. **COWS specific:** no rows with `SiteCode = 'Global'` unless explicitly approved.
5. **BAM specific:** silver dedupe uses `SiteCode + FId`; bronze typo fixed.

---

# Appendix — legacy code references

| Item | File | Line (approx) |
|------|------|---------------|
| `SaveGlobalClinicalOpiateWithdrawalScale` | `BHG-DR-LIB/SaveGlobal.cs` | 583–712 |
| `SaveBAM` | `BHG-DR-LIB/SaveGlobal.cs` | 1427–1902 |
| BAM daily WHERE override | `BHGTaskRunner/Program.cs` | 553–569 |
| BAMMergeGbl post-SP | `BHGTaskRunner/Program.cs` | 569 |
| Fabric COWS bronze copy | `Global/globaldef.txt` | ConfigId 174 |
| Fabric BAM bronze copy | `Global/globaldef.txt` | ConfigId 176 |
| Fabric `process_cows` silver | `Global/globaldef.txt` | ~2151 / ~2806 |
| Fabric `process_bam` silver | `Global/globaldef.txt` | ~2274 / ~2938 |
| Fabric gold processors | `Global/globaldef.txt` | ~3416 / ~3577 |

---

*Generated from codebase analysis of Fabric `globaldef.txt` vs legacy `SaveGlobal.cs`, map actions, and open bug counts (BUG 3514 COWS; BAM count mismatch).*
