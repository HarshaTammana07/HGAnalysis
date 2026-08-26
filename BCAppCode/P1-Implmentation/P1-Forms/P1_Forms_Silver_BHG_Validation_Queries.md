# P1 Forms — BHG_DR vs Fabric Silver Validation Queries

Monthly and site-level row-count checks for the **9 Regional P1 Forms** methods. Run the **BHG_DR** query in Azure SQL and the **Fabric** query in the Fabric SQL analytics endpoint attached to your silver lakehouse.

Source-to-destination mapping: `BCAppCode/P1/P2-Analysis/Regional_P1_P2_Source_to_Destination.md` (§4 Forms).

## Test parameters


| Parameter | Value |
| --------- | ----- |
| Sites | `AHK`, `B12B`, `B24`, `B25`, `B26` |
| Months | **May, Jun, Jul 2026** (`2026-05-01` inclusive → `2026-08-01` exclusive) |
| Lookback (4 tables) | `LookbackDays = 15` — when validating against a specific Fabric run, set `@StartDate = WorkDate - 15` |
| Expected | Same `SiteCode` + `MonthEnd` + `RowCnt` on both sides for monthly checks; same site totals for full-load tables |


Adjust dates/sites as needed. If your Fabric catalog differs, prefix tables (e.g. `bhg_silver.pats.tbl_comprehensiveassessmentform`).

## Optional active-row filter

Several tables have `RowState`. For **active-only** parity, uncomment the `RowState` line in those queries.


| Has `RowState` | Tables |
| -------------- | ------ |
| Yes | ComprehensiveAssessmentForm, ConsenttoMarketing, TakeHomeAgreementandDiversionControl, NewDischargeTransferPlanForm |
| No | EandMFormPregnancy, EandMFormMDM, SF_DataForms, SMSTextConsentForm, TakeHomeRiskAssessment (use `IsDeleted` / `Isdeleted` if needed) |


**Legacy note:** `tbl_NewDischargeTransferPlanForm` sets `RowState = 1` regardless of `Isdeleted` in both C# and Fabric Silver.

## Quick reference


| # | Method | SAMMS source | BHG table | Fabric table | Date column | Load type | Merge key |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 1 | SaveComprehensiveAssessmentForm | `dbo.ComprehensiveAssessmentForm` | `pats.tbl_ComprehensiveAssessmentForm` | `pats.tbl_comprehensiveassessmentform` | `CreatedOn` | Full site | `SiteCode`, `Id` |
| 2 | SaveEMFormPregnancy | `dbo.EandMFormPregnancy` | `pats.tbl_EandMFormPregnancy` | `pats.tbl_eandmformpregnancy` | `CreatedOn` | Full site | `SiteCode`, `EandMFormId` |
| 3 | SaveEMFormMDM | `dbo.EandMForm` | `pats.tbl_EandMFormMDM` | `pats.tbl_eandmformmdm` | `CreatedOn` | Full site | `SiteCode`, `Id` |
| 4 | SaveDataForms | `dbo.SF_DataForms` | `pats.tbl_SF_DataForms` | `pats.tbl_sf_dataforms` | `CreatedOn` / `LastUpdatedOn` | Incremental (15-day) | `SiteCode`, `Id` |
| 5 | SaveSMSTextConsentForm | `dbo.SMSTextConsentForm` | `pats.tbl_SMSTextConsentForm` | `pats.tbl_smstextconsentform` | `CreatedOn` | Full site | `SiteCode`, `Id` |
| 6 | SaveConsenttoMarketing | `dbo.consenttomarketing` | `pats.tbl_ConsenttoMarketing` | `pats.tbl_consenttomarketing` | `CreatedOn` / `ModifiedOn` | Incremental (15-day) | `SiteCode`, `Id` |
| 7 | SaveTakeHomeAgreementandDiversionControl | `dbo.takehomeagreementanddiversioncontrol` | `pats.tbl_TakeHomeAgreementandDiversionControl` | `pats.tbl_takehomeagreementanddiversioncontrol` | `CreatedOn` / `ModifiedOn` | Incremental (15-day) | `SiteCode`, `Id` |
| 8 | SaveTakeHomeRiskAssessment | `dbo.TakeHomeRiskAssessment` | `pats.tbl_TakeHomeRiskAssessment` | `pats.tbl_takehomeriskassessment` | `CreatedOn` | Full site | `SiteCode`, `Id` |
| 9 | SaveNewDischargeTransferPlanForm | `dbo.newdischargetransferplanform` | `pats.tbl_NewDischargeTransferPlanForm` | `pats.tbl_newdischargetransferplanform` | `CreatedOn` / `ModifiedOn` | Incremental (15-day) | `SiteCode`, `Id` |


**Notes:**

- **Full-load tables (5):** Comprehensive Assessment, E&M Pregnancy, E&M MDM, SMS Text Consent, Take Home Risk Assessment — legacy C# uses `WHERE 1 = 1`. Prefer **site-total** checks; monthly `CreatedOn` buckets show when forms were created, not ETL run scope.
- **Lookback tables (4):** SF Data Forms, Consent to Marketing, Take Home Agreement, New Discharge Transfer Plan — match Fabric using `CreatedOn` and/or update-date columns with the same `@StartDate` as the pipeline run.
- **E&M Pregnancy** business key is `SiteCode + EandMFormId`, not `Id`.

---

## 1. SaveComprehensiveAssessmentForm

**Date column:** `CreatedOn` (full load — site-total check is primary)

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_ComprehensiveAssessmentForm
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_comprehensiveassessmentform]
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Site total (recommended for full load)

```sql
SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_ComprehensiveAssessmentForm   -- or [pats].[tbl_comprehensiveassessmentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  -- AND RowState = 1
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 2. SaveEMFormPregnancy

**Date column:** `CreatedOn` (clinical form date is `FormDate`)

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_EandMFormPregnancy
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_eandmformpregnancy]
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Site total (recommended for full load)

```sql
SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_EandMFormPregnancy   -- or [pats].[tbl_eandmformpregnancy]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 3. SaveEMFormMDM

**Date column:** `CreatedOn`

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_EandMFormMDM
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_eandmformmdm]
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Site total (recommended for full load)

```sql
SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_EandMFormMDM   -- or [pats].[tbl_eandmformmdm]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 4. SaveDataForms

**Date columns:** `CreatedOn`, `LastUpdatedOn` (incremental — 15-day lookback in Fabric)

### BHG_DR — monthly (activity in window)

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(LastUpdatedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_SF_DataForms
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (LastUpdatedOn >= '2026-05-01' AND LastUpdatedOn < '2026-08-01')
  )
GROUP BY SiteCode, EOMONTH(COALESCE(LastUpdatedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(LastUpdatedOn, CreatedOn));
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(LastUpdatedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_sf_dataforms]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (LastUpdatedOn >= '2026-05-01' AND LastUpdatedOn < '2026-08-01')
  )
GROUP BY SiteCode, EOMONTH(COALESCE(LastUpdatedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(LastUpdatedOn, CreatedOn));
```

### Lookback run check (match pipeline `@StartDate`)

```sql
DECLARE @StartDate date = '2026-07-13';   -- WorkDate 2026-07-28 minus 15 days

SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_SF_DataForms   -- or [pats].[tbl_sf_dataforms]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(LastUpdatedOn AS date) >= @StartDate
  )
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 5. SaveSMSTextConsentForm

**Date column:** `CreatedOn`

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_SMSTextConsentForm
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_smstextconsentform]
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Site total (recommended for full load)

```sql
SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_SMSTextConsentForm   -- or [pats].[tbl_smstextconsentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 6. SaveConsenttoMarketing

**Date columns:** `CreatedOn`, `ModifiedOn` (incremental — 15-day lookback)

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(ModifiedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_ConsenttoMarketing
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (ModifiedOn >= '2026-05-01' AND ModifiedOn < '2026-08-01')
  )
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn));
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(ModifiedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_consenttomarketing]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (ModifiedOn >= '2026-05-01' AND ModifiedOn < '2026-08-01')
  )
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn));
```

### Lookback run check

```sql
DECLARE @StartDate date = '2026-07-13';

SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_ConsenttoMarketing   -- or [pats].[tbl_consenttomarketing]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )
  -- AND RowState = 1
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 7. SaveTakeHomeAgreementandDiversionControl

**Date columns:** `CreatedOn`, `ModifiedOn` (incremental — 15-day lookback)

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(ModifiedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_TakeHomeAgreementandDiversionControl
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (ModifiedOn >= '2026-05-01' AND ModifiedOn < '2026-08-01')
  )
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn));
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(ModifiedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_takehomeagreementanddiversioncontrol]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (ModifiedOn >= '2026-05-01' AND ModifiedOn < '2026-08-01')
  )
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn));
```

### Lookback run check

```sql
DECLARE @StartDate date = '2026-07-13';

SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_TakeHomeAgreementandDiversionControl
     -- or [pats].[tbl_takehomeagreementanddiversioncontrol]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )
  -- AND RowState = 1
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 8. SaveTakeHomeRiskAssessment

**Date column:** `CreatedOn`

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_TakeHomeRiskAssessment
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_takehomeriskassessment]
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Site total (recommended for full load)

```sql
SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_TakeHomeRiskAssessment   -- or [pats].[tbl_takehomeriskassessment]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 9. SaveNewDischargeTransferPlanForm

**Date columns:** `CreatedOn`, `ModifiedOn`, `DischargeDate` (incremental — 15-day lookback)

### BHG_DR — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(ModifiedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_NewDischargeTransferPlanForm
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (ModifiedOn >= '2026-05-01' AND ModifiedOn < '2026-08-01')
  )
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn));
```

### Fabric Silver — monthly

```sql
SELECT
    SiteCode,
    EOMONTH(COALESCE(ModifiedOn, CreatedOn)) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_newdischargetransferplanform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        (CreatedOn >= '2026-05-01' AND CreatedOn < '2026-08-01')
     OR (ModifiedOn >= '2026-05-01' AND ModifiedOn < '2026-08-01')
  )
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn))
ORDER BY SiteCode, EOMONTH(COALESCE(ModifiedOn, CreatedOn));
```

### Lookback run check

```sql
DECLARE @StartDate date = '2026-07-13';

SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_NewDischargeTransferPlanForm
     -- or [pats].[tbl_newdischargetransferplanform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
  AND (
        CAST(CreatedOn AS date) >= @StartDate
     OR CAST(ModifiedOn AS date) >= @StartDate
  )
  -- AND RowState = 1
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## All full-load tables — site totals (one query)

Run in BHG_DR and Fabric Silver. Expect matching counts per table/site.

### BHG_DR

```sql
SELECT 'tbl_ComprehensiveAssessmentForm' AS TableName, SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_ComprehensiveAssessmentForm
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_EandMFormPregnancy', SiteCode, COUNT(*)
FROM pats.tbl_EandMFormPregnancy
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_EandMFormMDM', SiteCode, COUNT(*)
FROM pats.tbl_EandMFormMDM
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_SMSTextConsentForm', SiteCode, COUNT(*)
FROM pats.tbl_SMSTextConsentForm
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_TakeHomeRiskAssessment', SiteCode, COUNT(*)
FROM pats.tbl_TakeHomeRiskAssessment
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY TableName, SiteCode;
```

### Fabric Silver

```sql
SELECT 'tbl_comprehensiveassessmentform' AS TableName, SiteCode, COUNT(*) AS RowCnt
FROM [pats].[tbl_comprehensiveassessmentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_eandmformpregnancy', SiteCode, COUNT(*)
FROM [pats].[tbl_eandmformpregnancy]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_eandmformmdm', SiteCode, COUNT(*)
FROM [pats].[tbl_eandmformmdm]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_smstextconsentform', SiteCode, COUNT(*)
FROM [pats].[tbl_smstextconsentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode

UNION ALL
SELECT 'tbl_takehomeriskassessment', SiteCode, COUNT(*)
FROM [pats].[tbl_takehomeriskassessment]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode
ORDER BY TableName, SiteCode;
```

---

## Business key duplicate checks (Fabric Silver)

Most tables: `SiteCode + Id`. Pregnancy: `SiteCode + EandMFormId`.

```sql
-- Example: Comprehensive Assessment
SELECT SiteCode, Id, COUNT(*) AS DuplicateCount
FROM [pats].[tbl_comprehensiveassessmentform]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, Id
HAVING COUNT(*) > 1;

-- Example: E&M Pregnancy
SELECT SiteCode, EandMFormId, COUNT(*) AS DuplicateCount
FROM [pats].[tbl_eandmformpregnancy]
WHERE SiteCode IN ('AHK', 'B12B', 'B24', 'B25', 'B26')
GROUP BY SiteCode, EandMFormId
HAVING COUNT(*) > 1;
```

Expected: no rows returned.

---

## Known legacy / Fabric parity notes

| Table | Note |
| ----- | ---- |
| `tbl_TakeHomeRiskAssessment` | `TakeHomeDosesUnsafe` and `AbstainingFromOtherSubstances` are intentionally `NULL` in Fabric (C# never maps them). |
| `tbl_NewDischargeTransferPlanForm` | `RowState` is always `1` regardless of `Isdeleted`. |
| `tbl_TakeHomeAgreementandDiversionControl` | Source may have `Patient1`/`Patient2`; bronze aliases to `Patients1`/`Patients2`. |
| Lookback tables | Fabric Silver may have fewer rows than BHG_DR if BHG was loaded over a longer history and Fabric has only recent 15-day extracts. Compare using the same `@StartDate`. |

---

## How to compare results

1. Export both result sets to CSV (or use a spreadsheet).
2. Join on `SiteCode` + `MonthEnd` (monthly) or `SiteCode` (site totals).
3. Flag rows where `RowCnt` differs.
4. For mismatches, spot-check one business key row (`SiteCode` + `Id`, or `SiteCode` + `EandMFormId` for pregnancy).
5. For lookback tables, always align `@StartDate` with the Fabric pipeline run (`WorkDate - LookbackDays`).

Schema validation queries: see `P1_Forms_Unit_Testing_Guide.md`.
