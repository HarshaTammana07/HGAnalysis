# `SaveClaims` — Step-by-Step Simulation with Dummy Data

This document walks through one complete run of `SaveClaims` for **clinic VBRA** on **WorkDate = 2025-03-14**, with `yearly = true`.

---

## Setup: Method Call

```csharp
sd.SaveClaims(
    tbl:    <DataTable from SAMMS — 4 rows>,
    sc:     "VBRA",
    wrkdt:  2025-03-14,
    yearly: true,
    db:     null       // EF context will be created internally
)
```

> **yearly = true** means:
> - Load ALL claims for VBRA from Azure (no year filter in the query)
> - Then soft-reset RowState = false only for rows where `TpcCreatedDate.Year == 2025`
> - Rows from prior years (e.g. 2024) are loaded into memory for lookup but their RowState is never touched

---

## Step 1 — Initialization

```
res.IsResult       = true
res.RowsProcessed  = 4          ← set immediately to tbl.Rows.Count
db                 = new BHG_DRContext()   ← created because null was passed
RunDT              = 2025-03-14 09:22:05   ← DateTime.Now captured once
AllNewRows         = false
NewRow             = false
```

---

## Step 2 — Load Existing Azure Rows for VBRA

Query executed by EF Core:
```sql
SELECT * FROM pats.tbl_Claims WHERE SiteCode = 'VBRA'
```

Returns 5 rows into memory as `List<TblClaims> claims`:

| # | SiteCode | tpcID | TpcCreatedDate | TpcStrStatus | TpcStrPayer | TpccltId | TpcStrWeek | RowChkSum | RowState |
|---|---|---|---|---|---|---|---|---|---|
| R1 | VBRA | 5001 | **2025-01-10** | Submitted | BlueCross | 201 | 2025-W02 | 111111 | 1 |
| R2 | VBRA | 5002 | **2025-02-20** | Paid | Medicaid | 202 | 2025-W08 | 222222 | 1 |
| R3 | VBRA | 5003 | **2025-03-01** | Denied | Aetna | 203 | 2025-W09 | 333333 | 1 |
| R4 | VBRA | 5004 | **2024-11-15** | Paid | BlueCross | 204 | 2024-W46 | 444444 | 1 |
| R5 | VBRA | 5005 | **2025-02-05** | Submitted | Cigna | 205 | 2025-W06 | 555555 | 1 |

**claims.Count = 5 → AllNewRows remains false**

---

## Step 3 — Yearly RowState Pre-Reset

Since `yearly = true`, the code loops through all 5 in-memory rows and checks:

```csharp
if (c.TpcCreatedDate.Value.Year == wrkdt.Year)  // wrkdt.Year = 2025
    c.RowState = false;
```

| Row | TpcCreatedDate | TpcCreatedDate.Year | == 2025? | RowState After |
|---|---|---|---|---|
| R1 | 2025-01-10 | 2025 | **YES** | **0** |
| R2 | 2025-02-20 | 2025 | **YES** | **0** |
| R3 | 2025-03-01 | 2025 | **YES** | **0** |
| R4 | 2024-11-15 | 2024 | **NO** | 1 (unchanged) |
| R5 | 2025-02-05 | 2025 | **YES** | **0** |

> **db.SaveChanges() is NOT called yet.** These are in-memory changes only.
> R4 is a 2024 claim — its RowState is never touched, not now and not later.

---

## Step 4 — Incoming SAMMS DataTable (tbl)

This is what SAMMS returned for clinic VBRA today. **4 rows.**

| # | tpcid | tpccltid | tpcStrStatus | tpcStrPayer | tpcDtmAdded | tpcStrAdded | tpcClaimBatchID | tpcStrPrimary | tpcCreatedDate | tpcEncounter | tpcREBILLREASON | tpcStrWeek | tpcWKSTART | tpcPayerCIN | tpcSrvType | tpcClaimType | SiteID | tpcDBnotes | tpcReferring | f21diag1 | f21diag2 | f28totalcharge | f29amtpaid | f25taxid | RowChkSum |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| S1 | 5001 | 201 | Submitted | BlueCross | 2025-01-10 | jadmin | 3001 | INS-BC-001 | 2025-01-10 | ENC-2025-001 |  | 2025-W02 | 2025-01-06 | BC123456 | Counseling | 1 | 88 |  | NPI-001 | F32.9 | Z79.899 | 150.00 | 0.00 | 12-3456789 | **111111** |
| S2 | 5002 | 202 | **Adjusted** | Medicaid | 2025-02-20 | jadmin | 3002 | INS-MCD-002 | 2025-02-20 | ENC-2025-002 | Rebill-Adj | 2025-W08 | 2025-02-17 | MCD789012 | Counseling | 1 | 88 | Adjusted per EOB | NPI-001 | F11.20 |  | 175.00 | **52.50** | 12-3456789 | **999999** |
| S3 | 5003 | 203 | Denied | Aetna | 2025-03-01 | jadmin | 3003 | INS-AET-003 | 2025-03-01 | ENC-2025-003 |  | 2025-W09 | 2025-02-24 | AET345678 | Counseling | 1 | 88 |  | NPI-001 | F11.20 | F32.9 | 150.00 | 0.00 | 12-3456789 | **333333** |
| S4 | **6001** | 206 | Submitted | UnitedHealth | 2025-03-10 | jadmin | 3004 | INS-UH-004 | 2025-03-10 | ENC-2025-010 |  | 2025-W11 | 2025-03-10 | UH901234 | Counseling | 1 | 88 |  | NPI-002 | F11.20 |  | 200.00 | 0.00 | 12-3456789 | **777777** |

> **Notice:** tpcID=5005 (R5) is **not returned by SAMMS today** — that claim was voided/retracted. It will remain RowState=0 after the run (soft-deleted).

---

## Step 5 — Row-by-Row Processing

---

### S1 — tpcID = 5001

**Extract from DataRow:**
```
inttpcID = int.Parse("5001") = 5001
rcs      = int.Parse("111111") = 111111
```

**Lookup in in-memory list:**
```csharp
clm = claims.Where(x => x.TpcId == 5001).FirstOrDefault()
```
→ **Matches R1** ✅ → `res.RowsUpd += 1`

**Checksum check:**
```
rcs (111111) == clm.RowChkSum (111111)  →  NO CHANGE
NewRow = false
```

**→ Enters ELSE block (checksum unchanged):**

| Field | Value Set |
|---|---|
| `clm.RowState` | **true** (re-activated — was reset to false in Step 3) |
| `clm.LastModAt` | **2025-03-14 09:22:05** |

> No data columns are written. EF Core will only UPDATE `RowState` and `LastModAt` for this row.

---

### S2 — tpcID = 5002

**Extract from DataRow:**
```
inttpcID = int.Parse("5002") = 5002
rcs      = int.Parse("999999") = 999999
```

**Lookup in in-memory list:**
```csharp
clm = claims.Where(x => x.TpcId == 5002).FirstOrDefault()
```
→ **Matches R2** ✅ → `res.RowsUpd += 1`

**Checksum check:**
```
rcs (999999) != clm.RowChkSum (222222)  →  CHANGED
```

**→ Enters column mapping block:**

| Column | Raw DataRow Value | Transformation Applied | Mapped to EF Property |
|---|---|---|---|
| `RowChkSum` | "999999" | `int.Parse("999999")` | `clm.RowChkSum = 999999` |
| `LastModAt` | — | `DateTime.Now` | `clm.LastModAt = 2025-03-14 09:22:05` |
| `RowState` | — | hardcoded | `clm.RowState = true` |
| `tpccltid` | "202" | `int.Parse("202")` | `clm.TpccltId = 202` |
| `tpcstrstatus` | "Adjusted" | `.ToString()` | `clm.TpcStrStatus = "Adjusted"` |
| `tpcstrpayer` | "Medicaid" | `.ToString()` | `clm.TpcStrPayer = "Medicaid"` |
| `tpcDtmAdded` | "2025-02-20" | length=10 > 7 → `DateTime.Parse` | `clm.TpcDtmAdded = 2025-02-20` |
| `tpcstrAdded` | "jadmin" | `.ToString()` | `clm.TpcStrAdded = "jadmin"` |
| `tpcclaimbatchid` | "3002" | length=4 > 0 → `int.Parse("3002")` | `clm.TpcClaimBatchId = 3002` |
| `tpcstrprimary` | "INS-MCD-002" | `.ToString()` | `clm.TpcStrPrimary = "INS-MCD-002"` |
| `tpcCreatedDate` | "2025-02-20" | length=10 > 7 → `DateTime.Parse` | `clm.TpcCreatedDate = 2025-02-20` |
| `tpcEncounter` | "ENC-2025-002" | `.ToString()` | `clm.TpcEncounter = "ENC-2025-002"` |
| `tpcrebillreason` | "Rebill-Adj" | `.ToString()` | `clm.TpcRebillreason = "Rebill-Adj"` |
| `tpcstrweek` | "2025-W08" | `.ToString()` | `clm.TpcStrWeek = "2025-W08"` |
| `tpcwkstart` | "2025-02-17" | length=10 > 0 → `DateTime.Parse(...).Date` | `clm.TpcWkstart = 2025-02-17` |
| `tpcpayercin` | "MCD789012" | `.ToString()` | `clm.TpcPayerCin = "MCD789012"` |
| `tpcsrvtype` | "Counseling" | `.ToString()` | `clm.TpcSrvType = "Counseling"` |
| `tpcClaimtype` | "1" | length=1 > 0 → `int.Parse("1")` | `clm.TpcClaimType = 1` |
| `SiteID` | "88" | length=2 > 0 → `int.Parse("88")` | `clm.SiteId = 88` |
| `tpcdbnotes` | "Adjusted per EOB" | `.ToString()` | `clm.TpcDbnotes = "Adjusted per EOB"` |
| `tpcreferring` | "NPI-001" | `.ToString()` | `clm.TpcReferring = "NPI-001"` |
| `f21diag1` | "F11.20" | `.ToString()` | `clm.F21diag1 = "F11.20"` |
| `f21diag2` | "" | `.ToString()` | `clm.F21diag2 = ""` |
| `f28totalcharge` | "175.00" | `.ToString()` (stored as varchar) | `clm.F28totalcharge = "175.00"` |
| `f29amtpaid` | "52.50" | `.ToString()` (stored as varchar) | `clm.F29amtpaid = "52.50"` |
| `f25taxid` | "12-3456789" | `.ToString()` | `clm.F25taxid = "12-3456789"` |
| *(all other F# fields)* | "" | `.ToString()` | mapped as empty strings |

> **Key transformation notes for S2:**
> - `tpcwkstart` uses `.Date` to strip time — stored as SQL `date` type not `datetime`
> - `f28totalcharge` ("175.00") and `f29amtpaid` ("52.50") are stored **as-is as varchar strings** — no `decimal.Parse()`. Dollar amounts in tbl_Claims are varchar, not numeric.
> - `tpcDtmAdded` uses the **Length > 7** guard — "2025-02-20" is 10 chars, passes.
> - `tpcclaimbatchid` uses the **Length > 0** guard — "3002" is 4 chars, passes.

**NewRow = false → db.TblClaims.Add() is NOT called** — EF change tracking will generate an UPDATE automatically because the object was loaded from the DbContext.

**What changed vs old R2:**

| Field | Old Value | New Value |
|---|---|---|
| `TpcStrStatus` | "Paid" | **"Adjusted"** |
| `TpcRebillreason` | "" | **"Rebill-Adj"** |
| `F29amtpaid` | "0.00" | **"52.50"** |
| `TpcDbnotes` | "" | **"Adjusted per EOB"** |
| `RowChkSum` | 222222 | **999999** |
| `RowState` | 0 (was reset) | **1** (re-activated) |
| `LastModAt` | old | **2025-03-14 09:22:05** |

---

### S3 — tpcID = 5003

**Extract from DataRow:**
```
inttpcID = int.Parse("5003") = 5003
rcs      = int.Parse("333333") = 333333
```

**Lookup:**
→ **Matches R3** ✅ → `res.RowsUpd += 1`

**Checksum check:**
```
rcs (333333) == clm.RowChkSum (333333)  →  NO CHANGE
```

**→ Enters ELSE block:**

| Field | Value Set |
|---|---|
| `clm.RowState` | **true** |
| `clm.LastModAt` | **2025-03-14 09:22:05** |

> Same as S1 — only RowState and LastModAt updated, no data columns written.

---

### S4 — tpcID = 6001 (New Claim — Not in Azure)

**Extract from DataRow:**
```
inttpcID = int.Parse("6001") = 6001
rcs      = int.Parse("777777") = 777777
```

**Lookup:**
```csharp
clm = claims.Where(x => x.TpcId == 6001).FirstOrDefault()
```
→ **No match found** ❌ → `clm = null`

**New object created:**
```csharp
clm = new Models.TblClaims
{
    SiteCode  = "VBRA",
    TpcId     = 6001,
    RowChkSum = 777777
}
NewRow = true
res.RowsIns += 1
```

**Checksum check:**
```
NewRow = true  →  enters column mapping block unconditionally
```

**Column mapping (key fields shown):**

| Column | Raw DataRow Value | Transformation Applied | Mapped to EF Property |
|---|---|---|---|
| `RowChkSum` | "777777" | `int.Parse("777777")` | `clm.RowChkSum = 777777` |
| `LastModAt` | — | `DateTime.Now` | `clm.LastModAt = 2025-03-14 09:22:05` |
| `RowState` | — | hardcoded | `clm.RowState = true` |
| `tpccltid` | "206" | `int.Parse("206")` | `clm.TpccltId = 206` |
| `tpcstrstatus` | "Submitted" | `.ToString()` | `clm.TpcStrStatus = "Submitted"` |
| `tpcstrpayer` | "UnitedHealth" | `.ToString()` | `clm.TpcStrPayer = "UnitedHealth"` |
| `tpcDtmAdded` | "2025-03-10" | length=10 > 7 → `DateTime.Parse` | `clm.TpcDtmAdded = 2025-03-10` |
| `tpcclaimbatchid` | "3004" | length=4 > 0 → `int.Parse("3004")` | `clm.TpcClaimBatchId = 3004` |
| `tpcCreatedDate` | "2025-03-10" | length=10 > 7 → `DateTime.Parse` | `clm.TpcCreatedDate = 2025-03-10` |
| `tpcwkstart` | "2025-03-10" | length=10 > 0 → `DateTime.Parse(...).Date` | `clm.TpcWkstart = 2025-03-10` |
| `tpcClaimtype` | "1" | length=1 > 0 → `int.Parse("1")` | `clm.TpcClaimType = 1` |
| `SiteID` | "88" | length=2 > 0 → `int.Parse("88")` | `clm.SiteId = 88` |
| `f21diag1` | "F11.20" | `.ToString()` | `clm.F21diag1 = "F11.20"` |
| `f28totalcharge` | "200.00" | `.ToString()` | `clm.F28totalcharge = "200.00"` |
| `f29amtpaid` | "0.00" | `.ToString()` | `clm.F29amtpaid = "0.00"` |
| *(all other F# fields)* | "" | `.ToString()` | mapped as empty strings |

**NewRow = true → object is registered:**
```csharp
NewRow = false;
db.TblClaims.Add(clm);   // queued for INSERT
```

---

## Step 6 — db.SaveChanges() — Single Commit

One `db.SaveChanges()` call at the end of the method commits everything accumulated during the loop **in a single batch**:

| What Is Written | Detail |
|---|---|
| **UPDATE R1** | RowState 0→1, LastModAt updated — no data columns |
| **UPDATE R2** | RowState 0→1, LastModAt, RowChkSum, TpcStrStatus, TpcRebillreason, F29amtpaid, TpcDbnotes, and all other mapped columns |
| **UPDATE R3** | RowState 0→1, LastModAt — no data columns |
| **INSERT S4** | Brand new row tpcID=6001, all columns from mapping, RowState=1 |
| **R5 stays RowState=0** | No SaveChanges write needed — the reset to 0 from Step 3 is flushed here as part of the same SaveChanges |

> EF Core generates separate SQL statements per row:
> - Three `UPDATE pats.tbl_Claims SET ... WHERE SiteCode='VBRA' AND tpcID=...`
> - One `INSERT INTO pats.tbl_Claims (...) VALUES (...)`
>
> R5's RowState=0 (set in Step 3 pre-reset, never reversed) is also flushed here.

---

## Step 7 — What Happens to R5 (tpcID=5005)?

R5 was in Azure with RowState=1 before this run.

- **Step 3:** Its `TpcCreatedDate` (2025-02-05) is in 2025 → RowState was set to **0** in memory.
- **Step 5:** SAMMS did NOT return tpcID=5005 in the DataTable. The loop never found it.
- **Step 6:** RowState=0 is committed by SaveChanges.
- **Result:** R5 is now **soft-deleted** — RowState=0 in Azure. It is still physically present in the table but excluded from active reporting.

---

## Final State — pats.tbl_Claims for VBRA After the Run

| # | tpcID | TpcCreatedDate | TpcStrStatus | TpcStrPayer | RowChkSum | RowState | What Happened |
|---|---|---|---|---|---|---|---|
| R1 | 5001 | 2025-01-10 | Submitted | BlueCross | 111111 | **1** | Pre-reset → checksum unchanged → re-activated. Only RowState + LastModAt written |
| R2 | 5002 | 2025-02-20 | **Adjusted** | Medicaid | **999999** | **1** | Pre-reset → checksum changed → full column update + re-activated |
| R3 | 5003 | 2025-03-01 | Denied | Aetna | 333333 | **1** | Pre-reset → checksum unchanged → re-activated. Only RowState + LastModAt written |
| R4 | 5004 | 2024-11-15 | Paid | BlueCross | 444444 | **1** | **Completely untouched** — 2024 row, never in pre-reset, never in upsert loop |
| R5 | 5005 | 2025-02-05 | Submitted | Cigna | 555555 | **0** | Pre-reset to 0 → SAMMS did not return it → **soft-deleted** |
| NEW | 6001 | 2025-03-10 | Submitted | UnitedHealth | 777777 | **1** | **Brand new INSERT** — never existed in Azure before |

---

## Return Value — RCodes

```
res.IsResult       = true
res.RowsProcessed  = 4          ← tbl.Rows.Count (set at initialization)
res.RowsIns        = 1          ← S4 (new claim tpcID=6001)
res.RowsUpd        = 3          ← S1, S2, S3 (all found in Azure, counted as "update")
res.ExceptMsg      = ""         ← no exception
res.ExceptInnerMsg = ""
```

> **Note:** `RowsUpd = 3` counts all rows that were found in the in-memory list — including S1 and S3 which had no checksum change. It does NOT mean "3 data updates". Only S2 had actual data columns written.

---

## Edge Case — What If tpcDtmAdded Were NULL?

For S2 if SAMMS returned `tpcDtmAdded = ""` (empty / NULL):

```csharp
if (r["tpcDtmAdded"].ToString().Length > 7)   // "" has length 0 → 0 > 7 is FALSE
{
    clm.TpcDtmAdded = DateTime.Parse(r["tpcDtmAdded"].ToString());
}
// Guard fails → clm.TpcDtmAdded is left unchanged (null for new rows, old value for existing rows)
```

The field is skipped silently. No exception thrown.

---

## Edge Case — What If tpcClaimBatchID Were NULL?

For S4 if SAMMS returned `tpcclaimbatchid = ""`:

```csharp
if (r["tpcclaimbatchid"].ToString().Length > 0)   // "" has length 0 → FALSE
{
    clm.TpcClaimBatchId = int.Parse(r["tpcclaimbatchid"].ToString());
}
// Guard fails → clm.TpcClaimBatchId stays null (it's int? in the EF model)
```

---

## Edge Case — What If AllNewRows Were True?

If VBRA had **zero** existing rows in Azure (`claims.Count == 0`):

```csharp
AllNewRows = true;
```

The pre-reset foreach loop (Step 3) would still run — but `claims` is empty, so it does nothing.

In the main loop, for every row in tbl:
```csharp
// Lookup is SKIPPED entirely
clm = new Models.TblClaims { SiteCode="VBRA", TpcId=..., RowChkSum=... }
NewRow = true
res.RowsIns += 1
```

Every row is treated as a new INSERT — the per-row `claims.Where(...)` LINQ search is never executed. This is the **first-time load optimization**.

---

## Key Lessons from the Simulation

| Lesson | Where It Happened |
|---|---|
| **RowState goes 1→0→1 for active rows** | R1, R2, R3 were pre-reset to 0 in Step 3, then re-activated to 1 when SAMMS returned them |
| **2024 rows are completely untouched** | R4 was never in the pre-reset (wrong year) and never appeared in the SAMMS DataTable — its data and RowState are frozen |
| **Checksum unchanged = only RowState + LastModAt** | S1 and S3 had no data change — EF Core only updates 2 columns, not the full 80+ column set |
| **Checksum changed = full column remap** | S2 had every single field re-mapped even if most values are the same — the all-or-nothing remap is by design |
| **Dollar amounts are varchar, not decimal** | `f28totalcharge = "175.00"` is stored as a string — no `decimal.Parse()`, no rounding, no precision loss |
| **tpcWKSTART uses .Date to strip time** | Even though the source may have a time component, only the date part is stored (SQL `date` type) |
| **One SaveChanges() for everything** | Updates, inserts, and RowState=0 flushes all go in a single batch at the end — not row-by-row |
| **Soft-delete has no explicit DELETE** | R5 is not removed from Azure — it stays in the table with RowState=0. Downstream views filter `WHERE RowState = 1` |
| **RowsUpd counts lookups, not actual writes** | S1 and S3 incremented RowsUpd but wrote zero data columns — RowsUpd ≠ number of SQL UPDATE statements with data changes |
