# `SaveOrders` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveOrders.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 12–178  
**What it does:** Reads prescription order records from a SAMMS clinic database and upserts them into the Azure destination table `pats.tbl_Orders`. This is the **generic/predecessor method** — it maps to the non-year-partitioned table. It is **not called by BHGTaskRunner in the daily run**; only the year-specific methods (`SaveOrders2016`–`SaveOrders2028`) are dispatched daily. This method exists as a historical predecessor and is available for manual backfill use.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows fetched from SAMMS — one row per order record |
| `sc` | string | SiteCode — identifies which clinic this run is for (e.g. `"B01"`, `"VBRA"`) |
| `wrkdt` | DateTime | Work date — passed in but **not used** in any WHERE or filter logic inside this method. Date filtering was applied by BHGTaskRunner before calling this method. |
| `db` | DbContext | EF Core database context — created internally if not passed in |

---

## Output

Returns a single `bool`:

| Value | Meaning |
|---|---|
| `true` | Run completed without an unhandled exception |
| `false` | An exception was caught — message written to `Console.WriteLine` |

> **No row counts are tracked.** There is no `RCodes` object — no `RowsIns`, `RowsUpd`, or structured error capture. Errors are written to console only.

---

## How This Differs From `SaveOrders20XX` (Year Methods)

| Aspect | SaveOrders (generic) | SaveOrders20XX (year methods) |
|---|---|---|
| Destination table | `pats.tbl_Orders` (non-partitioned) | `pats.tbl_Orders2016`–`pats.tbl_Orders2028` (year-partitioned) |
| Called daily? | **No** — not in BHGTaskRunner's dispatch loop | **Yes** — all 13 year methods are called per-clinic per run |
| Checksum gate | `NewRow \|\| rcs != o.RowChkSum \|\| rcs < 0` | Standard (`NewRow \|\| rcs != o.RowChkSum`) or always-true for 2024+ |
| Extra gate condition | **`rcs < 0`** — always updates when source checksum is negative | Not present in 2016–2023 methods |
| CltId filter | None — all CltId values processed | 2024+ only: skips rows where `cltid <= 0` |
| Schema-optional guards | None — all columns assumed present in DataTable | 2024+ only: `Columns.Contains()` guards on signature columns |
| Notes truncation | None | 2024+ only: truncates to 999 chars if > 1000 |
| effectivedate / expirationdate | Always parsed without length guard | 2019+: length > 7 guard before DateTime.Parse |
| dose / dose2 | Always parsed without length guard | 2019+: length > 0 guard before decimal.Parse |
| Pre-pass | Resets ALL rows (RowState + Active) — no date filter | Same |
| New row batching | Collected into `ords` list → `AddRange` | Same |
| Commit strategy | Two commits (updates then inserts) | Same |

---

## Step-by-Step Logic

### Step 1 — Guard: Empty DataTable

```
if (tbl.Rows.Count > 0) { ... }
```

If no rows were returned from SAMMS, the method returns `true` immediately without touching Azure.

---

### Step 2 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_Orders` for this `SiteCode` are loaded into memory, ordered by `OrderNum`:

```
db.TblOrders.Where(x => x.SiteCode == sc).OrderBy(o => o.OrderNum).ToList()
```

The commented-out date filter (`x.Orderdate.Value.Date == wrkdt.Date`) and the commented-out `.Take()` limit were removed — the full table for the site is always loaded regardless of date or count.

If `orders.Count == 0`, the flag `AllNewRows = true` is set — this bypasses the lookup step entirely.

---

### Step 3 — Pre-Pass: Reset ALL Rows (RowState and Active)

Both state flags are reset to false for every existing row:

```
foreach (TblOrders ord in orders):
    ord.RowState = false
    ord.Active   = false
```

There is no date window filter — **every single order row for this clinic is soft-reset** before the incoming SAMMS data re-activates those that still exist. The commented-out year filter (`ord.DateAdded.Value.Year == wrkdt.Year`) was removed.

> **No SaveChanges here.** Resets are held in EF Core's change tracker and flushed in the first commit at the end.

---

### Step 4 — Parse Key Fields Per Row

For each incoming SAMMS row:

| Source Column | Parsed As | Notes |
|---|---|---|
| `OrderNum` | `int` | Part of composite lookup key |
| `cltid` | `int` | Part of composite lookup key |
| `rowchksum` | `int` | Source checksum |

No CltId guard exists here — all CltId values (positive, negative, zero) are processed.

---

### Step 5 — Construct or Locate the Order Object

**If `AllNewRows == true`:**
```
o = new TblOrders {
    SiteCode  = sc,
    CltId     = cltid,
    OrderNum  = onum,
    RowChkSum = rcs   // stored at actual source value
}
NewRow = true
```

**If existing rows are present:**  
A lookup is performed against the in-memory snapshot using **two columns**: `OrderNum` AND `CltId`.

```
o = orders.Where(x => x.OrderNum == onum && x.CltId == cltid).FirstOrDefault()
```

- Match found → proceed to checksum gate
- No match → construct new `TblOrders` object with same four fields, `NewRow = true`

---

### Step 6 — Checksum Gate

```csharp
if ((NewRow) || (rcs != o.RowChkSum) || (rcs < 0))
```

**Three conditions trigger a full field update:**

| Condition | Meaning |
|---|---|
| `NewRow` | Row is brand new — no prior Azure record |
| `rcs != o.RowChkSum` | Source data changed since last run |
| `rcs < 0` | Source checksum is negative — always update regardless of match |

> **Why `rcs < 0`?** SQL Server's `CHECKSUM()` function can return negative integers. This is mathematically valid but creates an ambiguity — a stored negative checksum matching a new negative checksum might be coincidence rather than a true unchanged row. This extra condition forces a full update whenever the computed checksum is negative, avoiding any false "no change" conclusion on negative values. This condition is **unique to `SaveOrders`** — it is not in `SaveOrders2016`–`SaveOrders2023`.

---

### Step 7 — Full Field Mapping (Gate Triggered)

When the gate fires, all fields are written:

| Source Column | Destination Field | Guard / Transformation |
|---|---|---|
| *(implicit)* | `RowState` | Always `true` |
| *(implicit)* | `LastModAt` | `DateTime.Now` |
| `RowChkSum` | `RowChkSum` | `int.Parse(r["RowChkSum"])` — note capital R in key |
| `medtype` | `MedType` | Always |
| `dateadded` | `DateAdded` | `DateTime.Parse()` — no length guard |
| `orderdate` | `Orderdate` | `DateTime.Parse()` — no length guard |
| `doctor` | `Doctor` | Always |
| `effectivedate` | `EffectiveDate` | `DateTime.Parse()` — **no length guard** (will throw on empty string) |
| `expirationdate` | `ExpirationDate` | `DateTime.Parse()` — **no length guard** |
| `dose` | `Dose` | `decimal.Parse()` — **no length guard** |
| `dose2` | `Dose2` | `decimal.Parse()` — **no length guard** |
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
| `notes` | `Notes` | Always — no truncation guard |
| `active` | `Active` | `bool.Parse()` — no guard |
| `type` | `Type` | Always |
| `stype` | `Stype` | Always |
| `weeknum` | `Weeknum` | `int.Parse()` — no guard |
| `splitfirst` | `SplitFirst` | `bool.Parse()` — no guard |
| `blind` | `Blind` | `bool.Parse()` — no guard |
| `o_user` | `OUser` | Always |
| `cltM4id` | `CltM4id` | Always |
| `newdose` | `Newdose` | `int` — **length > 0** guard |
| `pckcode` | `Pckcode` | Always |
| `rxhistid` | `RxhistId` | Always |
| `ex` | `Ex` | `bool` — **length > 0** guard |
| `actbydate` | `ActbyDate` | `DateTime` — **length > 0** guard |
| `actbyuser` | `ActByUser` | Always |
| `white` | `White` | `bool` — **length > 0** guard |
| `repoldorder` | `RepOldOrder` | `decimal` — **length > 0** guard |
| `sigdr` | `SigDr` | Always — full ntext signature |
| `dtsig` | `DtSig` | `DateTime` — **length > 0** guard |
| `aws` | `Aws` | `bool` — **length > 0** guard |
| `blsched` | `BlSched` | `bool` — **length > 0** guard |
| `blverbal` | `BlVerbal` | `bool` — **length > 0** guard |
| `color` | `Color` | Always |
| `deactbydate` | `DeActbyDate` | `DateTime` — **length > 0** guard |
| `deactbyuser` | `DeActbyUser` | Always |
| `ordertypev5` | `OrderTypev5` | Always |
| `sigentered` | `Sigentered` | Always — full ntext signature |
| `signoted` | `Signoted` | Always — full ntext signature |
| `signoteddt` | `SigNoteddt` | `DateTime` — **length > 0** guard |
| `dtmid` | `Dtmid` | **Double guard:** length > 0 AND value != `"1900-01-01 00:00:00.000"` |
| `sigmid` | `SigMid` | Always — full ntext signature |
| `overapprove` | `OverApprove` | Always |
| `overapprovedt` | `OverapproveDt` | Always — stored as `varchar`, **not** DateTime |
| `sigentereddt` | `Sigentereddt` | `DateTime` — **length > 0** guard |
| `sigdrimg` | `SigDrImg` | `byte[]` — `Encoding.ASCII.GetBytes(r["sigdrimg"].ToString())` — always |
| `SigMidImg` | `SigMidImg` | `byte[]` — `Encoding.ASCII.GetBytes(r["SigMidImg"].ToString())` — always |
| `SigNotedImg` | `SigNotedImg` | `byte[]` — `Encoding.ASCII.GetBytes(r["SigNotedImg"].ToString())` — always |

> **Fields with no length guard on required dates/decimals:** `effectivedate`, `expirationdate`, `dose`, and `dose2` are parsed unconditionally. An empty string value from SAMMS will throw a parse exception — this is the main parse-risk difference from `SaveOrders2019+`, which added guards on these four fields.

> **`dtmid` sentinel exclusion:** The value `"1900-01-01 00:00:00.000"` is never stored in `Dtmid`. It means "not yet signed by mid-level provider" in SAMMS. If received, `Dtmid` is left unchanged (null or its prior value).

> **Signature images always stored:** `sigdrimg`, `SigMidImg`, and `SigNotedImg` are converted unconditionally via `Encoding.ASCII.GetBytes()`. There is no schema-optional guard — if the column is absent from the DataTable, this will throw a `ColumnNotFoundException`.

---

### Step 8 — Checksum Unchanged Path (Existing Rows Only)

If an existing row is found and the gate does **not** fire (checksum matches and is non-negative):

```
o.RowState  = true
o.LastModAt = DateTime.Now
```

The row is re-activated (undoing the pre-pass reset) and its timestamp updated. No data fields change.

---

### Step 9 — Queue New Rows for Batch Insert

```
if (NewRow || AllNewRows):
    NewRow = false
    ords.Add(o)
```

New rows are staged in the `ords` list during the loop. They are **not** added to EF Core inline — they wait for the batch insert in Step 10.

---

### Step 10 — Write Everything to the Database (Two Commits)

**Commit 1:**
```
db.SaveChanges()
```
Flushes pre-pass resets (RowState=false, Active=false) and all field updates to existing matched rows.

**Commit 2:**
```
db.TblOrders.AddRange(ords)
db.SaveChanges()
```
Batch-inserts all new rows collected in `ords`. Only runs if `ords.Count > 0`.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **Not called in daily run** | BHGTaskRunner dispatches `SaveOrders2016`–`2028` only; this method is available for manual/backfill use against `pats.tbl_Orders` |
| **Full pre-pass reset** | Every order row for the clinic is set to `RowState = false, Active = false` — no date window |
| **Two-column lookup key** | `OrderNum + CltId` — both required to uniquely identify an order within a clinic |
| **Extra checksum gate: `rcs < 0`** | Always runs full update when source CHECKSUM() returns a negative value, regardless of stored match |
| **No CltId filter** | All CltId values processed — including negative and zero |
| **No Notes truncation** | Notes stored as-is; may cause SaveChanges failure if > 1000 chars |
| **No schema-optional guards** | All columns assumed present in DataTable — including all three signature image columns |
| **No length guards on effectivedate, expirationdate, dose, dose2** | These are parsed unconditionally; empty strings cause parse exceptions |
| **`dtmid` sentinel excluded** | `"1900-01-01 00:00:00.000"` is never stored — `Dtmid` left as null/prior value |
| **`overapprovedt` is varchar** | Stored as string despite the "dt" name — not parsed as DateTime |
| **New rows batched** | `ords.Add(o)` during loop → `AddRange` + `SaveChanges()` at end |
| **Two commits** | Commit 1 = updates+resets; Commit 2 = inserts |
| **Errors to console only** | `Console.WriteLine(e.Message)` — no structured error return |

---

## Soft-Delete — How It Works End to End

```
BEFORE RUN:
  Azure has 1,800 order rows for clinic B01, all RowState = true, Active = varies

STEP 3 (pre-pass — NOT yet committed):
  ALL 1,800 rows → RowState = false, Active = false

STEP 7 (upsert):
  SAMMS returns 1,780 order rows
  → 1,780 matched by OrderNum + CltId:
      Checksum changed or rcs < 0 → full field update, RowState = true, Active = from source
      Checksum unchanged and rcs >= 0 → RowState = true, LastModAt refreshed

STEP 10 — Commit 1 (SaveChanges — updates + resets):
  1,780 rows re-activated, 20 rows remain RowState = false, Active = false

STEP 10 — Commit 2 (new inserts if any):
  Any brand-new OrderNum+CltId combos inserted

AFTER RUN:
  1,780 rows → RowState = true   (confirmed present in source)
     20 rows → RowState = false  (not returned from SAMMS — soft-deleted)
```

The 20 rows remaining at `RowState = false` represent orders that are in Azure but not returned by the source query — they may have been cancelled, fallen outside the date window, or deleted from SAMMS.
