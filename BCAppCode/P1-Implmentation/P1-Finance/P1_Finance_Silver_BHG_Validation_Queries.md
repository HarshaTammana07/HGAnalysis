# P1 Finance — BHG_DR vs Fabric Silver Validation Queries

Monthly row-count checks for the **14 Regional P1 Finance** methods. Run the **BHG_DR** query in Azure SQL and the **Fabric** query in the Fabric SQL analytics endpoint / warehouse attached to your silver lakehouse.

## Test parameters (120-day run window)


| Parameter | Value                                                                                                |
| --------- | ---------------------------------------------------------------------------------------------------- |
| Sites     | `AHK`, `B42D`, `CBCO`, `HS`, `TTCC`                                                                  |
| Months    | **May, Jun, Jul 2026** (`2026-05-01` inclusive → `2026-08-01` exclusive)                             |
| Expected  | Same `SiteCode` + `MonthEnd` + `RowCnt` on both sides (± known gaps in `P1_Finance_Known_Issues.md`) |


Adjust dates/sites as needed. If your Fabric catalog differs, prefix tables (e.g. `bhg_silver.pats.tbl_bills`).

## Optional active-row filter

Several tables have `RowState`. BHG bulk paths often keep inactive rows in the table. For **active-only** parity, uncomment the `RowState` line in those queries.


| Has `RowState` | Tables                                                                                                                        |
| -------------- | ----------------------------------------------------------------------------------------------------------------------------- |
| Yes            | Bills, Auths, AuthBillsub, FMP, FHA, 3pElig, Claims, ClaimLineItem, ClaimLineItemActivity, TblDiags, ClientDemo1, ClientDemo2 |
| No             | PayerCltHistory, PayerClient (`pyACTIVE` instead)                                                                             |


## Quick reference


| #   | Method                           | BHG table                               | Fabric table                            | Date column      | Load type                        |
| --- | -------------------------------- | --------------------------------------- | --------------------------------------- | ---------------- | -------------------------------- |
| 1   | SaveBills                        | `pats.tbl_Bills`                        | `pats.tbl_bills`                        | `billDate`       | Year window + reload (not daily) |
| 2   | SaveAuths                        | `pats.tbl_pbi3PAYauth`                  | `pats.tbl_pbi3payauth`                  | `tpadt`          | Full site                        |
| 3   | SaveAuthBillsub                  | `pats.tbl_vw3pBillSub`                  | `pats.tbl_vw3pbillsub`                  | `DSbilled`       | Full site (bulk)                 |
| 4   | SaveFmp                          | `pats.tbl_FMP`                          | `pats.tbl_fmp`                          | `fmpDtAdded`     | Full site                        |
| 5   | SavePayerCltHistory              | `pats.tbl_PayerCltHistory`              | `pats.tbl_payerclthistory`              | `pyDtm`          | Incremental (`pyDtm`)            |
| 6   | SaveFinancialHardshipApplication | `pats.Tbl_FinancialHardshipApplication` | `pats.tbl_financialhardshipapplication` | `CreatedOn`      | Full site                        |
| 7   | Save3pElig                       | `pats.tbl_3pElig`                       | `pats.tbl_3pelig`                       | `eDATE`          | Year window                      |
| 8   | SaveClaimLineItem                | `pats.tbl_ClaimLineItem`                | `pats.tbl_claimlineitem`                | `tpcliDtmAdded`  | Full site (bulk)                 |
| 9   | SaveClaimLineItemActivity        | `pats.tbl_ClaimLineItemActivity`        | `pats.tbl_claimlineitemactivity`        | `liaDtm`         | Full site (bulk)                 |
| 10  | SaveClaims                       | `pats.tbl_Claims`                       | `pats.tbl_claims`                       | `tpcCreatedDate` | Full site (bulk)                 |
| 11  | SavePayerClient                  | `pats.tbl_PayerClient`                  | `pats.tbl_payerclient`                  | `pyAddDate`      | Filtered / reload                |
| 12  | SaveTblDiags                     | `pats.tbl_tbldiag10`                    | `pats.tbl_tbldiag10`                    | `dgDATE`         | Full site                        |
| 13  | SaveClientDemo1                  | `pats.tbl_ClientDemo1`                  | `pats.tbl_clientdemo1`                  | `LastModAt`      | Full site                        |
| 14  | SaveClientDemo2                  | `pats.tbl_ClientDemo2`                  | `pats.tbl_clientdemo2`                  | `DateAdded`      | Full site                        |


**Note:** `SaveClientDemo1` has no source business date on the target; `LastModAt` reflects **ETL/silver merge time**, so May/Jun/Jul buckets show when rows were last touched, not client add date.

---

## 1. SaveBills

**Date column:** `billDate`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(billDate) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_Bills
WHERE billDate >= '2026-05-01'
  AND billDate <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(billDate)
ORDER BY SiteCode, EOMONTH(billDate);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(billDate) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_bills]
WHERE billDate >= '2026-05-01'
  AND billDate <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(billDate)
ORDER BY SiteCode, EOMONTH(billDate);
```

---

## 2. SaveAuths

**Date column:** `tpadt` (auth entry date)

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(tpadt) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_pbi3PAYauth
WHERE tpadt >= '2026-05-01'
  AND tpadt <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(tpadt)
ORDER BY SiteCode, EOMONTH(tpadt);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(tpadt) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_pbi3payauth]
WHERE tpadt >= '2026-05-01'
  AND tpadt <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(tpadt)
ORDER BY SiteCode, EOMONTH(tpadt);
```

---

## 3. SaveAuthBillsub-test with dsDtStart not with dsbilled for this

**Date column:** `DSbilled` (use `billdatecriteria` if `DSbilled` is null-heavy)

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(DSbilled) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_vw3pBillSub
WHERE DSbilled >= '2026-05-01'
  AND DSbilled <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(DSbilled)
ORDER BY SiteCode, EOMONTH(DSbilled);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(DSbilled) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_vw3pbillsub]
WHERE DSbilled >= '2026-05-01'
  AND DSbilled <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(DSbilled)
ORDER BY SiteCode, EOMONTH(DSbilled);
```

---

## 4. SaveFmp

**Date column:** `fmpDtAdded`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(fmpDtAdded) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_FMP
WHERE fmpDtAdded >= '2026-05-01'
  AND fmpDtAdded <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(fmpDtAdded)
ORDER BY SiteCode, EOMONTH(fmpDtAdded);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(fmpDtAdded) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_fmp]
WHERE fmpDtAdded >= '2026-05-01'
  AND fmpDtAdded <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(fmpDtAdded)
ORDER BY SiteCode, EOMONTH(fmpDtAdded);
```

---

## 5. SavePayerCltHistory

**Date column:** `pyDtm` (incremental source filter)

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(pyDtm) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_PayerCltHistory
WHERE pyDtm >= '2026-05-01'
  AND pyDtm <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, EOMONTH(pyDtm)
ORDER BY SiteCode, EOMONTH(pyDtm);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(pyDtm) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_payerclthistory]
WHERE pyDtm >= '2026-05-01'
  AND pyDtm <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
GROUP BY SiteCode, EOMONTH(pyDtm)
ORDER BY SiteCode, EOMONTH(pyDtm);
```

---

## 6. SaveFinancialHardshipApplication

**Date column:** `CreatedOn`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.Tbl_FinancialHardshipApplication
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(CreatedOn) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_financialhardshipapplication]
WHERE CreatedOn >= '2026-05-01'
  AND CreatedOn <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(CreatedOn)
ORDER BY SiteCode, EOMONTH(CreatedOn);
```

---

## 7. Save3pElig

**Date column:** `eDATE`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(eDATE) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_3pElig
WHERE eDATE >= '2026-05-01'
  AND eDATE <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(eDATE)
ORDER BY SiteCode, EOMONTH(eDATE);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(eDATE) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_3pelig]
WHERE eDATE >= '2026-05-01'
  AND eDATE <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(eDATE)
ORDER BY SiteCode, EOMONTH(eDATE);
```

---

## 8. SaveClaimLineItem

**Date column:** `tpcliDtmAdded`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(tpcliDtmAdded) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_ClaimLineItem
WHERE tpcliDtmAdded >= '2026-05-01'
  AND tpcliDtmAdded <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(tpcliDtmAdded)
ORDER BY SiteCode, EOMONTH(tpcliDtmAdded);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(tpcliDtmAdded) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_claimlineitem]
WHERE tpcliDtmAdded >= '2026-05-01'
  AND tpcliDtmAdded <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(tpcliDtmAdded)
ORDER BY SiteCode, EOMONTH(tpcliDtmAdded);
```

---

## 9. SaveClaimLineItemActivity

**Date column:** `liaDtm`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(liaDtm) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_ClaimLineItemActivity
WHERE liaDtm >= '2026-05-01'
  AND liaDtm <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(liaDtm)
ORDER BY SiteCode, EOMONTH(liaDtm);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(liaDtm) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_claimlineitemactivity]
WHERE liaDtm >= '2026-05-01'
  AND liaDtm <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(liaDtm)
ORDER BY SiteCode, EOMONTH(liaDtm);
```

---

## 10. SaveClaims

**Date column:** `tpcCreatedDate`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(tpcCreatedDate) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_Claims
WHERE tpcCreatedDate >= '2026-05-01'
  AND tpcCreatedDate <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(tpcCreatedDate)
ORDER BY SiteCode, EOMONTH(tpcCreatedDate);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(tpcCreatedDate) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_claims]
WHERE tpcCreatedDate >= '2026-05-01'
  AND tpcCreatedDate <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(tpcCreatedDate)
ORDER BY SiteCode, EOMONTH(tpcCreatedDate);
```

---

## 11. SavePayerClient

**Date column:** `pyAddDate` (payer record add date; no `RowState` — use `pyACTIVE = 1` for active-only)

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(pyAddDate) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_PayerClient
WHERE pyAddDate >= '2026-05-01'
  AND pyAddDate <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND pyACTIVE = 1
GROUP BY SiteCode, EOMONTH(pyAddDate)
ORDER BY SiteCode, EOMONTH(pyAddDate);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(pyAddDate) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_payerclient]
WHERE pyAddDate >= '2026-05-01'
  AND pyAddDate <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND pyACTIVE = 1
GROUP BY SiteCode, EOMONTH(pyAddDate)
ORDER BY SiteCode, EOMONTH(pyAddDate);
```

---

## 12. SaveTblDiags

**Date column:** `dgDATE`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(dgDATE) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_tbldiag10
WHERE dgDATE >= '2026-05-01'
  AND dgDATE <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(dgDATE)
ORDER BY SiteCode, EOMONTH(dgDATE);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(dgDATE) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_tbldiag10]
WHERE dgDATE >= '2026-05-01'
  AND dgDATE <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(dgDATE)
ORDER BY SiteCode, EOMONTH(dgDATE);
```

---

## 13. SaveClientDemo1

**Date column:** `LastModAt` (ETL timestamp — see note in quick reference)

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(LastModAt) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_ClientDemo1
WHERE LastModAt >= '2026-05-01'
  AND LastModAt <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(LastModAt)
ORDER BY SiteCode, EOMONTH(LastModAt);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(LastModAt) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_clientdemo1]
WHERE LastModAt >= '2026-05-01'
  AND LastModAt <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(LastModAt)
ORDER BY SiteCode, EOMONTH(LastModAt);
```

**Site total check (no month bucket)** — often easier for full-load Demo1:

```sql
SELECT SiteCode, COUNT(*) AS RowCnt
FROM pats.tbl_ClientDemo1   -- or [pats].[tbl_clientdemo1]
WHERE SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode
ORDER BY SiteCode;
```

---

## 14. SaveClientDemo2

**Date column:** `DateAdded`

### BHG_DR

```sql
SELECT
    SiteCode,
    EOMONTH(DateAdded) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM pats.tbl_ClientDemo2
WHERE DateAdded >= '2026-05-01'
  AND DateAdded <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(DateAdded)
ORDER BY SiteCode, EOMONTH(DateAdded);
```

### Fabric Silver

```sql
SELECT
    SiteCode,
    EOMONTH(DateAdded) AS MonthEnd,
    COUNT(*) AS RowCnt
FROM [pats].[tbl_clientdemo2]
WHERE DateAdded >= '2026-05-01'
  AND DateAdded <  '2026-08-01'
  AND SiteCode IN ('AHK', 'B42D', 'CBCO', 'HS', 'TTCC')
  -- AND RowState = 1
GROUP BY SiteCode, EOMONTH(DateAdded)
ORDER BY SiteCode, EOMONTH(DateAdded);
```

---

## How to compare results

1. Export both result sets to CSV (or use a spreadsheet).
2. Join on `SiteCode` + `MonthEnd`.
3. Flag rows where `RowCnt` differs.
4. For mismatches, spot-check one business key row (see `Csvs/data1.csv` pattern) before opening a defect.

Known acceptable diffs: see `P1_Finance_Known_Issues.md` (`PCID`, `PrimKey`, etc.).