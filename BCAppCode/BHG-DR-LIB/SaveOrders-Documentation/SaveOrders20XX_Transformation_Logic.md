# `SaveOrders20XX` — Transformation & Logic
# (Year-Partitioned Methods: 2016 through 2028)

**File:** `BCAppCode/BHG-DR-LIB/SaveOrders.cs`  
**Class:** `SaveData` (partial)  
**Methods covered:** `SaveOrders2016`, `SaveOrders2017`, `SaveOrders2018`, `SaveOrders2019`, `SaveOrders2020`, `SaveOrders2021`, `SaveOrders2022`, `SaveOrders2023`, `SaveOrders2024`, `SaveOrders2025`, `SaveOrders2026`, `SaveOrders2027`, `SaveOrders2028`  
**What they do:** Each method reads prescription order records whose `OrderDate` year matches the method's suffix, and upserts them into the corresponding Azure year-partitioned table (`pats.tbl_Orders2016` through `pats.tbl_Orders2028`). These are the **active daily methods** — BHGTaskRunner fetches all orders for a clinic at once, splits them by `OrderDate.Year`, and dispatches to the matching method.

> **Canonical reference method:** The logic documented here describes `SaveOrders2022` (a Generation 2 method, lines 1190–1365) as the standard. Every deviation in other years is listed explicitly in the **Generational Differences** section.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Year-filtered rows from SAMMS — only orders for this method's year |
| `sc` | string | SiteCode — identifies which clinic this run is for (e.g. `"B01"`, `"VBRA"`) |
| `wrkdt` | DateTime | Work date — passed in from BHGTaskRunner but **not used** inside the method for filtering |
| `db` | DbContext | EF Core database context — created internally if not passed in |

> **How BHGTaskRunner feeds this method:** The caller fetches a single DataTable containing all orders for the clinic (within the date lookback window), then splits by `row["orderdate"].Year` and calls `SaveOrders20XX` with only the rows that belong to that year. The method itself does not re-filter by year.

---

## Output

Returns a single `bool`:

| Value | Meaning |
|---|---|
| `true` | Run completed without an unhandled exception |
| `false` | An exception was caught — message written to `Console.WriteLine` |

> **No row counts are tracked.** There is no `RCodes` object — no `RowsIns`, `RowsUpd`, or structured error capture. Errors are written to console only.

---

## How This Differs From `SaveOrders` (Generic)

| Aspect | SaveOrders (generic) | SaveOrders20XX (year methods) |
|---|---|---|
| Destination table | `pats.tbl_Orders` (non-partitioned) | `pats.tbl_Orders2016`–`pats.tbl_Orders2028` (year-partitioned) |
| Called daily? | **No** — not in BHGTaskRunner's dispatch loop | **Yes** — all 13 year methods called per-clinic per run |
| Checksum gate extra condition | **`rcs < 0`** present | Not present in 2016–2023; replaced by always-true gate in 2024+ |
| CltId filter | None | **2024+ only:** skips rows where `cltid <= 0` |
| Schema-optional guards | None | **2024+ only:** `Columns.Contains()` checks for signature columns |
| Notes truncation | None | **2024+ only:** truncates to 999 chars if > 1000 |
| `InnerException` null guard | No | **2024+ only:** null check before reading `InnerException.Message` |

---

## Three Generations of Behavior

All 13 year-methods share the same overall flow. They differ in three important ways depending on which generation they belong to:

| Feature | Gen 1: 2016–2018 | Gen 2: 2019–2023 | Gen 3: 2024–2028 |
|---|---|---|---|
| `effectivedate` length guard | **No** — parsed unconditionally | **Yes** — `length > 7` | **No** — parsed unconditionally (reverted) |
| `expirationdate` length guard | **No** — parsed unconditionally | **Yes** — `length > 7` | **No** — parsed unconditionally (reverted) |
| `dose` length guard | **No** — parsed unconditionally | **Yes** — `length > 0` | **No** — parsed unconditionally (reverted) |
| `dose2` length guard | **No** — parsed unconditionally | **Yes** — `length > 0` | **No** — parsed unconditionally (reverted) |
| Checksum gate | `NewRow \|\| rcs != o.RowChkSum` | `NewRow \|\| rcs != o.RowChkSum` | `NewRow \|\| rcs != o.RowChkSum \|\| o.RowChkSum < 0 \|\| rcs == o.RowChkSum` — **always true** |
| CltId filter | None | None | **`cltid > 0` required** — rows with zero or negative CltId are silently skipped |
| Schema-optional guards on signature columns | None | None | `tbl.Columns.Contains()` wraps `sigdr`, `sigentered`, `signoted`, `sigmid`, `sigdrimg`, `sigmidimg`, `signotedimg` |
| `sigentereddt` storage | Always stored (length > 0 guard) | Always stored (length > 0 guard) | **BUG:** `tbl.Columns.Contains("sogentereddt")` — typo prevents storage |
| Notes truncation | None | None | `> 1000 chars → Substring(0, 999).Trim()` |
| `InnerException.Message` null guard | No | No | **Yes** — `if (e.InnerException.Message != null)` |
| New rows debug print | No | No | **Yes** — `Console.WriteLine(o.CltId + " " + o.OrderNum)` for new rows |

> **The 2024+ checksum gate is always true.** The expression `(rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum)` covers all possible values — either the two checksums are equal or they are not. Every matched row therefore receives a full field update on every run. This is effectively a full-refresh mode for 2024+ orders.

> **The `sigentereddt` bug (2024+):** The schema check reads `tbl.Columns.Contains("sogentereddt")` — `"sog..."` is a typo. Because no source column is named `"sogentereddt"`, this check always returns false, and `Sigentereddt` is never stored for 2024+ orders regardless of whether the source has the column.

---

## Step-by-Step Logic

*(Using `SaveOrders2022` — Gen 2 — as the canonical example)*

### Step 1 — Guard: Empty DataTable

```
if (tbl.Rows.Count > 0) { ... }
```

If no rows were returned from SAMMS for this year, the method returns `true` immediately without touching Azure.

---

### Step 2 — Load All Existing Azure Rows for This Clinic (This Year's Table)

All rows for this SiteCode are loaded into memory from the year-specific table:

```
db.TblOrders2022.Where(x => x.SiteCode == sc).OrderBy(o => o.OrderNum).ToList()
```

The commented-out year filters (`x.Orderdate.Value.Year == 20XX` and `ord.DateAdded.Value.Year == wrkdt.Year`) were removed — the full year-table snapshot for the clinic is always loaded.

If `orders.Count == 0`, the flag `AllNewRows = true` is set.

---

### Step 3 — Pre-Pass: Reset ALL Rows (RowState and Active)

Both state flags are reset to false for every existing row in this year's table:

```
foreach (TblOrders20XX ord in orders):
    ord.RowState = false
    ord.Active   = false
```

No date window filter — every order on record for this clinic in this year's partition is soft-reset.

> **No SaveChanges here.** Resets are held in EF Core's change tracker and flushed in the first commit at the end.

---

### Step 4 — Parse Key Fields Per Row

For each incoming SAMMS row:

| Source Column | Parsed As | Notes |
|---|---|---|
| `OrderNum` | `int` | Part of composite lookup key |
| `cltid` | `int` | Part of composite lookup key |
| `rowchksum` | `int` | Source checksum |

**2024+ only:** The entire per-row block is wrapped in `if (cltid > 0)`. Rows where `cltid <= 0` are silently skipped — no insert, no update, no error. Rows skipped this way remain at `RowState = false` from the pre-pass.

---

### Step 5 — Construct or Locate the Order Object

**If `AllNewRows == true`:**
```
o = new TblOrders20XX {
    SiteCode  = sc,
    CltId     = cltid,
    OrderNum  = onum,
    RowChkSum = rcs
}
NewRow = true
```

**If existing rows are present:**  
Lookup against the in-memory snapshot using **two columns**: `OrderNum` AND `CltId`.

```
o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault()
```

- Match found → proceed to checksum gate
- No match → construct new object with same four fields, `NewRow = true`

---

### Step 6 — Checksum Gate

**Gen 1 and Gen 2 (2016–2023):**
```csharp
if ((NewRow) || (rcs != o.RowChkSum))
```
Full update only fires when the row is new or the source checksum differs from the stored one.

**Gen 3 (2024–2028):**
```csharp
if ((NewRow) || (rcs != o.RowChkSum) || (o.RowChkSum < 0) || (rcs == o.RowChkSum))
```
This gate is always `true` — the last two OR conditions together cover all possible values. Every row processed receives a full field update. This makes 2024+ a full-refresh-per-run design.

---

### Step 7 — Full Field Mapping (Gate Triggered)

When the gate fires, all fields are written. The table below reflects Gen 2 (2019–2023). Gen-specific differences are annotated in the **Guard / Transformation** column.

| Source Column | Destination Field | Guard / Transformation |
|---|---|---|
| *(implicit)* | `RowState` | Always `true` |
| *(implicit)* | `LastModAt` | `DateTime.Now` |
| `RowChkSum` | `RowChkSum` | `int.Parse(r["RowChkSum"])` — note capital R in key |
| `medtype` | `MedType` | Always |
| `dateadded` | `DateAdded` | `DateTime.Parse()` — no length guard |
| `orderdate` | `Orderdate` | `DateTime.Parse()` — no length guard |
| `doctor` | `Doctor` | Always |
| `effectivedate` | `EffectiveDate` | **Gen 1:** no guard. **Gen 2:** `length > 7` guard. **Gen 3:** no guard (reverted) |
| `expirationdate` | `ExpirationDate` | **Gen 1:** no guard. **Gen 2:** `length > 7` guard. **Gen 3:** no guard (reverted) |
| `dose` | `Dose` | **Gen 1:** no guard. **Gen 2:** `length > 0` guard. **Gen 3:** no guard (reverted) |
| `dose2` | `Dose2` | **Gen 1:** no guard. **Gen 2:** `length > 0` guard. **Gen 3:** no guard (reverted) |
| `changeby` | `Changeby` | `int.Parse()` — no guard |
| `intervals` | `Intervals` | `Int16.Parse()` — no guard |
| `sunday` | `Sunday` | `bool.Parse()` — no guard |
| `monday` | `Monday` | `bool.Parse()` — no guard |
| `tuesday` | `Tuesday` | `bool.Parse()` — no guard |
| `wednesday` | `Wednesday` | `bool.Parse()` — no guard |
| `thursday` | `Thursday` | `bool.Parse()` — no guard |
| `friday` | `Friday` | `bool.Parse()` — no guard |
| `saturday` | `Saturday` | `bool.Parse()` — no guard |
| `sunday2` | `Sunday2` | `bool.Parse()` — no guard |
| `monday2` | `Monday2` | `bool.Parse()` — no guard |
| `tuesday2` | `Tuesday2` | `bool.Parse()` — no guard |
| `wednesday2` | `Wednesday2` | `bool.Parse()` — no guard |
| `thursday2` | `Thursday2` | `bool.Parse()` — no guard |
| `friday2` | `Friday2` | `bool.Parse()` — no guard |
| `saturday2` | `Saturday2` | `bool.Parse()` — no guard |
| `notes` | `Notes` | **Gen 1–2:** always stored as-is. **Gen 3:** truncated to 999 chars if `> 1000` |
| `active` | `Active` | `bool.Parse()` — no guard |
| `type` | `Type` | Always |
| `stype` | `Stype` | Always |
| `weeknum` | `Weeknum` | `int.Parse()` — no guard |
| `splitfirst` | `SplitFirst` | `bool.Parse()` — no guard |
| `blind` | `Blind` | `bool.Parse()` — no guard |
| `o_user` | `OUser` | Always |
| `cltM4id` | `CltM4id` | Always |
| `newdose` | `Newdose` | `int` — `length > 0` guard (all generations) |
| `pckcode` | `Pckcode` | Always |
| `rxhistid` | `RxhistId` | Always |
| `ex` | `Ex` | `bool` — `length > 0` guard (all generations) |
| `actbydate` | `ActbyDate` | `DateTime` — `length > 0` guard (all generations) |
| `actbyuser` | `ActByUser` | Always |
| `white` | `White` | `bool` — `length > 0` guard (all generations) |
| `repoldorder` | `RepOldOrder` | `decimal` — `length > 0` guard (all generations) |
| `sigdr` | `SigDr` | **Gen 1–2:** always. **Gen 3:** `tbl.Columns.Contains("sigdr")` guard |
| `dtsig` | `DtSig` | `DateTime` — `length > 0` guard (all generations) |
| `aws` | `Aws` | `bool` — `length > 0` guard (all generations) |
| `blsched` | `BlSched` | `bool` — `length > 0` guard (all generations) |
| `blverbal` | `BlVerbal` | `bool` — `length > 0` guard (all generations) |
| `color` | `Color` | Always |
| `deactbydate` | `DeActbyDate` | `DateTime` — `length > 0` guard (all generations) |
| `deactbyuser` | `DeActbyUser` | Always |
| `ordertypev5` | `OrderTypev5` | Always |
| `sigentered` | `Sigentered` | **Gen 1–2:** always. **Gen 3:** `tbl.Columns.Contains("sigentered")` guard |
| `signoted` | `Signoted` | **Gen 1–2:** always. **Gen 3:** `tbl.Columns.Contains("signoted")` guard |
| `signoteddt` | `SigNoteddt` | `DateTime` — `length > 0` guard (all generations) |
| `dtmid` | `Dtmid` | **Double guard (all generations):** `length > 0` AND `!= "1900-01-01 00:00:00.000"` |
| `sigmid` | `SigMid` | **Gen 1–2:** always. **Gen 3:** `tbl.Columns.Contains("sigmid")` guard |
| `overapprove` | `OverApprove` | Always |
| `overapprovedt` | `OverapproveDt` | Always — stored as `varchar`, **not** DateTime |
| `sigentereddt` | `Sigentereddt` | **Gen 1–2:** `length > 0` guard. **Gen 3:** **BUG** — `tbl.Columns.Contains("sogentereddt")` (typo) always false — **never stored** |
| `sigdrimg` | `SigDrImg` | **Gen 1–2:** always (`Encoding.ASCII.GetBytes`). **Gen 3:** `tbl.Columns.Contains("sigdrimg")` guard |
| `SigMidImg` | `SigMidImg` | **Gen 1–2:** always (`Encoding.ASCII.GetBytes`). **Gen 3:** `tbl.Columns.Contains("sigmidimg")` guard |
| `SigNotedImg` | `SigNotedImg` | **Gen 1–2:** always (`Encoding.ASCII.GetBytes`). **Gen 3:** `tbl.Columns.Contains("signotedimg")` guard |

> **`dtmid` sentinel exclusion (all generations):** The value `"1900-01-01 00:00:00.000"` is never stored. It represents "not yet signed by a mid-level provider" in SAMMS. If received, `Dtmid` is left as null or its prior value.

> **`overapprovedt` is varchar (all generations):** Despite the `"dt"` suffix, this field is stored as a string. It is not DateTime-parsed.

> **Gen 3 `sigentereddt` bug:** The check `tbl.Columns.Contains("sogentereddt")` has a typo (`sog` instead of `sig`). This column name never exists, so `Sigentereddt` is never written for orders from 2024 onward, even when the source has the column.

---

### Step 8 — Checksum Unchanged Path (Gen 1 and Gen 2 Only)

When the gate does **not** fire (checksum matches, no new row), only activity markers are refreshed:

**Gen 1 (2016–2018):**
```
o.LastModAt = DateTime.Now
o.RowState  = true
```

**Gen 2 (2019–2023):**
```
o.RowState  = true
o.LastModAt = DateTime.Now
```

The assignment order differs between generations (Gen 1 reversed) but has no functional impact.

**Gen 3 (2024+):** This path is effectively unreachable — the checksum gate is always true, so every row always takes the full update path.

---

### Step 9 — Queue New Rows for Batch Insert

```
if (NewRow || AllNewRows):
    NewRow = false
    ords.Add(o)
```

New rows are staged in the `ords` list during the loop. They are **not** added to EF Core inline — they wait for the batch insert in Step 10.

**Gen 3 (2024+):** Before staging the new row, the method prints to console:
```
Console.WriteLine(o.CltId.ToString() + "  " + o.OrderNum.ToString())
```

---

### Step 10 — Write Everything to the Database (Two Commits)

**Commit 1:**
```
db.SaveChanges()
```
Flushes pre-pass resets (`RowState=false`, `Active=false`) and all field updates to existing matched rows.

**Commit 2:**
```
db.TblOrders20XX.AddRange(ords)
db.SaveChanges()
```
Batch-inserts all new rows. Only runs if `ords.Count > 0`.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **Called daily by BHGTaskRunner** | BHGTaskRunner fetches all orders for the clinic, splits by `OrderDate.Year`, dispatches to matching method |
| **Full pre-pass reset** | Every order for the clinic in this year's table is set `RowState = false, Active = false` — no date window |
| **Two-column lookup key** | `OrderNum + CltId` — both required to uniquely identify an order |
| **Standard checksum gate (Gen 1–2)** | Full update fires when row is new OR `rcs != o.RowChkSum` |
| **Always-true gate (Gen 3)** | Every matched row gets a full update every run — effectively full-refresh mode |
| **CltId filter (Gen 3 only)** | `cltid <= 0` rows silently skipped — not inserted, not updated |
| **`dtmid` sentinel excluded** | `"1900-01-01 00:00:00.000"` never stored — `Dtmid` left null |
| **`overapprovedt` is varchar** | Stored as string regardless of name |
| **Notes truncation (Gen 3 only)** | Notes longer than 1000 chars truncated to 999 |
| **Schema-optional guards (Gen 3 only)** | Signature columns wrapped in `Columns.Contains()` — skipped silently if source schema lacks them |
| **`sigentereddt` bug (Gen 3)** | `tbl.Columns.Contains("sogentereddt")` typo — `Sigentereddt` is never stored for 2024+ orders |
| **New rows batched** | `ords.Add(o)` during loop → `AddRange` + `SaveChanges()` at end |
| **Two commits** | Commit 1 = updates + resets; Commit 2 = inserts |
| **Errors to console only** | `Console.WriteLine(e.Message)` — no structured error return |
| **`InnerException` null guard (Gen 3 only)** | 2016–2023 call `e.InnerException.Message` without null check — NPE risk if exception has no inner. 2024+ adds `if (e.InnerException.Message != null)` |

---

## Soft-Delete — How It Works End to End

```
BEFORE RUN:
  pats.tbl_Orders2022 has 940 rows for clinic B01
  All rows: RowState = true, Active = varies

STEP 3 (pre-pass — NOT yet committed):
  ALL 940 rows → RowState = false, Active = false

STEP 7 (upsert):
  BHGTaskRunner passed in 925 rows whose orderdate.Year == 2022
  → Each row looked up by OrderNum + CltId:
      New rows         → added to ords list
      Existing + gate  → full field update, RowState = true, Active = from source
      Existing, no gate (Gen 1–2 only) → RowState = true, LastModAt refreshed

STEP 10 — Commit 1 (SaveChanges — updates + resets):
  925 rows re-activated (RowState = true)
  15 rows remain RowState = false, Active = false (not returned from source)

STEP 10 — Commit 2 (new inserts if any):
  Any new OrderNum+CltId combos inserted

AFTER RUN:
  925 rows → RowState = true   (confirmed present in source)
   15 rows → RowState = false  (not returned — soft-deleted)
```

The 15 rows remaining at `RowState = false` represent orders that are in Azure but not returned by the source query — they may have been cancelled or fallen outside the lookback window in SAMMS.

> **Because the pre-pass resets ALL rows in this year's partition**, any order not returned by the source query will be soft-deleted. The correctness of this depends on BHGTaskRunner's date lookback window being broad enough to capture all still-active orders for the clinic and year.
