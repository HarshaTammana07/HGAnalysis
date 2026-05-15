# Bulk Table Runtime Results
**Period:** Last 30 days &nbsp;|&nbsp; **Source:** `tsk.tbl_Tasks2` &nbsp;|&nbsp; **Status:** Completed (19) only

---

## Runtime Summary Table

| # | Table Name | Load Pattern | Site Runs | Unique Sites | Avg Duration | Min Duration | Max Duration | Total Duration (Sequential) | Avg Row Count | Max Row Count |
|---|-----------|-------------|----------:|-------------:|:------------:|:------------:|:------------:|:---------------------------:|-------------:|-------------:|
| 1 | `pats.tbl_DartsSrv` | Bulk — DartsSrvMerge ×8 | 3,437 | 116 | 00:02:26 | 00:00:04 | 00:31:29 | **39:56:49** | 58,928 | 935,395 |
| 2 | `pats.tbl_Dose` | Bulk (EF for 4 sites) | 3,441 | 116 | 00:00:52 | 00:00:00 | 00:16:37 | **49:43:41** | 51,307 | 183,136 |
| 3 | `pats.tbl_ClaimLineItemActivity` | Bulk | 3,441 | 116 | 00:00:41 | 00:00:00 | 00:06:59 | **39:39:02** | 426,826 | 2,747,119 |
| 4 | `pats.tbl_dbo_FormQuestionAnswers` | Bulk (18 sites) / EF (rest) | 3,441 | 116 | 00:00:35 | 00:00:00 | 00:22:44 | **33:55:06** | 2,165 | 23,125 |
| 5 | `pats.tbl_Claims` | Bulk (EF for 4 sites) | 3,441 | 116 | 00:00:34 | 00:00:00 | 00:05:02 | **33:07:50** | 77,805 | 389,705 |
| 6 | `pats.tbl_ClaimLineItem` | Bulk | 3,441 | 116 | 00:00:32 | 00:00:00 | 00:05:49 | **31:17:34** | 336,027 | 2,417,949 |
| 7 | `pats.tbl_vw3pBillSub` | Bulk | 3,441 | 116 | 00:00:07 | 00:00:00 | 01:39:56 | **07:01:31** | 6,087 | 124,106 |
| 8 | `pats.tbl_Dose_Excuse` | EF (Bulk infra exists) | 3,441 | 116 | 00:00:05 | 00:00:00 | 00:01:39 | **05:09:13** | 3,907 | 58,222 |
| 9 | `pats.tbl_FormsSAMMSClient` | Bulk | 30 | 1 | 00:05:25 | 00:04:03 | 00:06:50 | **02:42:34** | 5,279,673 | 5,279,676 |
| 10 | `pats.tbl_LiquidLog` | Bulk (reload) / EF (incr) | 3,441 | 116 | 00:00:00 | 00:00:00 | 00:00:12 | **00:54:10** | 7,022 | 37,251 |
| 11 | `pats.tbl_UAResultDetail` | Bulk | 3,441 | 116 | 00:00:00 | 00:00:00 | 00:00:12 | **00:21:07** | 4,101 | 15,960 |
| 12 | `pats.tbl_LabResultDetail` | Bulk | 3,441 | 116 | 00:00:00 | 00:00:00 | 00:00:09 | **00:13:39** | 4,749 | 88,853 |

---

## Column Definitions

| Column | What It Means |
|--------|--------------|
| **Site Runs** | Total site × day executions recorded in last 30 days |
| **Unique Sites** | How many distinct sites this table ran for |
| **Avg Duration** | Average time per single site run (one site, one night) |
| **Min Duration** | Fastest single site run — smallest / least active clinic |
| **Max Duration** | Slowest single site run — the Fabric parallel bottleneck |
| **Total Duration (Sequential)** | Sum of ALL site runtimes — what the current nightly run spends on this table end-to-end |
| **Avg Row Count** | Average rows fetched from SAMMS source per site run |
| **Max Row Count** | Largest single site extraction (most active / largest clinic) |

---

## Key Observations

### 1. Heaviest Tables by Total Sequential Time

| Table | Total Duration | What This Means |
|-------|---------------|-----------------|
| `pats.tbl_Dose` | **49:43:41** | Across all sites, the system spends ~50 hours total on Dose (spread across 30 days — ~1.7 hrs per night) |
| `pats.tbl_DartsSrv` | **39:56:49** | ~1.3 hrs per night across all 116 sites |
| `pats.tbl_ClaimLineItemActivity` | **39:39:02** | ~1.3 hrs per night — high row count per site (avg 426K rows) |

---

### 2. Worst Outlier — `pats.tbl_vw3pBillSub`

```
Avg Duration : 00:00:07   (7 seconds per site on average)
Max Duration : 01:39:56   ← ONE site took 1 hour 40 minutes
Total        : 07:01:31
```
> One site is an extreme outlier — 99× slower than average.
> In the current sequential system this delay blocks the entire pipeline for that region.
> In Fabric parallel execution, this outlier site would set the wall-clock time for the whole batch.
> **Action:** Identify which site has Max = 01:39:56 using Query 2 in `Bulk_Table_Runtime_Analysis.md`.

---

### 3. `pats.tbl_FormsSAMMSClient` — Runs Only Once (Not Per Site)

```
Site Runs    : 30    (one run per day — not 116 per day)
Unique Sites : 1     (runs once against SAMMSGLOBAL, not per clinic)
Avg Duration : 00:05:25
Max Duration : 00:06:50
Avg Row Count: 5,279,673   ← 5.3 million rows per run
```
> This table pulls from the central `SAMMSGLOBAL` database, not from per-clinic SAMMS.
> Not a ForEach candidate in Fabric — runs as a single notebook activity.
> Loads 5.3M rows per run — will need Spark executor memory tuned accordingly.

---

### 4. Near-Zero Runtime Tables — `LiquidLog`, `UAResultDetail`, `LabResultDetail`

```
All three show Avg = 00:00:00 and Max = 00:00:09 to 00:00:12
```
> These run so fast per site that the task timer rounds to zero.
> Incremental slice is tiny (small date window, few changed rows).
> Zero migration risk — these will be faster in Fabric without any special tuning.

---

### 5. `pats.tbl_dbo_FormQuestionAnswers` — Outlier Max Duration

```
Avg Duration : 00:00:35
Max Duration : 00:22:44   ← one site 39× slower than average
```
> One large site (one of the 18 Bulk-enabled sites) has significantly more form records.
> In Fabric, this site sets the wall-clock time for the ForEach batch it falls in.

---

## Fabric Migration Impact — Sequential vs Parallel

The table below shows what changes when you move from sequential to parallel execution in Fabric.
**Current sequential** cost = TotalDuration ÷ 30 days = per-night cost.
**Fabric parallel** cost ≈ MaxDuration (slowest single site = new wall-clock time).

| Table | Current Nightly Cost (sequential) | Fabric Parallel Cost (MaxDuration) | Saving |
|-------|----------------------------------|-------------------------------------|--------|
| `pats.tbl_Dose` | ~01:39:27 / night | 00:16:37 | ~83% faster |
| `pats.tbl_DartsSrv` | ~01:19:53 / night | 00:31:29 | ~60% faster |
| `pats.tbl_ClaimLineItemActivity` | ~01:19:38 / night | 00:06:59 | ~91% faster |
| `pats.tbl_dbo_FormQuestionAnswers` | ~01:07:50 / night | 00:22:44 | ~66% faster |
| `pats.tbl_Claims` | ~01:06:16 / night | 00:05:02 | ~92% faster |
| `pats.tbl_ClaimLineItem` | ~01:02:35 / night | 00:05:49 | ~91% faster |
| `pats.tbl_vw3pBillSub` | ~00:14:03 / night | 01:39:56 ⚠️ | Outlier site must be investigated |
| `pats.tbl_Dose_Excuse` | ~00:10:18 / night | 00:01:39 | ~84% faster |
| `pats.tbl_FormsSAMMSClient` | ~00:05:25 / night | 00:06:50 | Single run — no change |
| `pats.tbl_LiquidLog` | ~00:01:48 / night | 00:00:12 | ~89% faster |
| `pats.tbl_UAResultDetail` | ~00:00:42 / night | 00:00:12 | ~71% faster |
| `pats.tbl_LabResultDetail` | ~00:00:27 / night | 00:00:09 | ~67% faster |

> ⚠️ **`pats.tbl_vw3pBillSub` warning:** The Max Duration (01:39:56) exceeds the current
> nightly sequential cost (00:14:03). This means one outlier site is extraordinarily slow.
> Before going live in Fabric, identify and fix this site (network issue, large data volume,
> or missing index on the SAMMS source view `dbo.vw3pBillSub`).

---

*Data from `tsk.tbl_Tasks2` — last 30 days, Status=19, child tasks only.*
*Nightly cost estimated as TotalDuration ÷ 30.*
