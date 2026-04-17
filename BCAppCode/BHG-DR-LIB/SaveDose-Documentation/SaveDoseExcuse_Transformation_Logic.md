# `SaveDoseExcuse` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveDoses.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 208–299  
**What it does:** Reads dose excuse records from a SAMMS clinic database and upserts them into the Azure destination table `pats.tbl_DoseExcuse`. One row per excuse event — tracks when a patient's missed or take-home dose was formally excused, including who processed it and when.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows fetched from SAMMS — one row per excuse record |
| `sc` | string | SiteCode — identifies which clinic this run is for (e.g. `"B01"`, `"NC"`) |
| `db` | DbContext | EF Core database context — created internally if not passed in |

---

## Output

Returns a single `bool`:

| Value | Meaning |
|---|---|
| `true` | Run completed without an unhandled exception |
| `false` | An exception was caught — message written to `Console.WriteLine` |

> **No row counts are tracked.** Unlike `SaveDoses`, there is no `RCodes` object — no `RowsIns`, `RowsUpd`, or structured error capture. Errors are written to console only.

---

## How This Differs From `SaveDoses`

| Aspect | SaveDoses | SaveDoseExcuse |
|---|---|---|
| Return type | `RCodes` (full audit trail) | `bool` (pass/fail only) |
| Parameters | `tbl, sc, dtWrk, reload, db` | `tbl, sc, db` — no date window, no reload flag |
| Hard reload option | Yes — deletes all rows if `reload = true` | No — no delete option at all |
| Pre-pass scope | Date-filtered: only rows where `DtDate >= dtWrk` | **Full reset: ALL rows for the clinic** regardless of date |
| Void / delete logic | `BlVoid + DtVoid` check + negative `CltId` guard | None — no soft-delete logic on rows |
| `CltId` sentinel (-111) | Exempt from negative-CltId delete rule | Not applicable |
| New row handling | Batched into `newdoses` list → `AddRange` | Added inline one at a time via `db.TblDoseExcuse.Add()` |
| Commit strategy | Two commits (one for updates, one for inserts) | Single `db.SaveChanges()` covers everything |
| Row count tracking | `RowsIns` / `RowsUpd` incremented | No tracking |
| Error capture | Stored in `RCodes.ExceptMsg` | `Console.WriteLine` only |
| Fields mapped | ~25 columns including booleans, image bytes, optional columns | 5 columns — very lean record |

---

## Step-by-Step Logic

### Step 1 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_DoseExcuse` for this `SiteCode` are loaded into memory.  
All lookups in subsequent steps run against this in-memory snapshot.

If `doses.Count == 0`, the flag `AllNewRows = true` is set — this bypasses the lookup step and takes a faster insert-only path.

---

### Step 2 — Pre-Pass: Mark ALL Rows as Inactive (Full Reset)

Unlike `SaveDoses` which only resets rows inside a date window, this method resets every single existing row for the clinic:

```
foreach row in doses  →  d.RowState = false
```

There is no date filter. Every dose excuse on record for this SiteCode is soft-reset before the source data re-activates those that still exist in SAMMS.

> **No SaveChanges here.** The resets are held in EF Core's change tracker and committed together with all updates in the single `db.SaveChanges()` at the end.

---

### Step 3 — Per-Row Lookup or New Row Construction

For every row coming in from SAMMS, three key fields are parsed eagerly:

| Source Column | Parsed As | Notes |
|---|---|---|
| `ExId` | `int` | Primary identity key |
| `cltid` | `int` | Patient/client ID |
| `rowchksum` | `int` | Source checksum |

**If `AllNewRows == true`** (no prior Azure data for this clinic):  
A new `TblDoseExcuse` is constructed immediately with: `SiteCode`, `ExId`, `CltId`, `RowChkSum` (set to the actual source checksum — unlike `SaveDoses` which initializes to `0`).

**If existing rows are present:**  
A lookup is performed against the in-memory snapshot using `ExId` as the single key.

- Match found → proceed to checksum check
- No match → new `TblDoseExcuse` constructed with `SiteCode`, `ExId`, `CltId`, `RowChkSum`, `NewRow = true`

---

### Step 4a — Full Field Mapping (New Row or Checksum Changed)

The full field update fires if `NewRow == true` OR `rcs != dose.RowChkSum`.

#### Column Transformations

| Source Column | Destination Field | Guard / Transformation |
|---|---|---|
| *(implicit)* | `RowState` | Always set to `true` (active) |
| *(implicit)* | `LastModAt` | Set to `RunDT` (DateTime.Now at method start) |
| `cltid` | `CltId` | Always re-assigned — even on updates this field is refreshed |
| `DtEx` | `DtEx` | DateTime — only stored if string length > 6 |
| `Dtstamp` | `Dtstamp` | DateTime — only stored if string length > 6 |
| `StrUser` | `StrUser` | Always stored |

> **`CltId` is explicitly re-assigned in the update path.** Unlike `SaveDoses` where `CltId` is fixed at construction and never updated, here the update branch overwrites `CltId` from the source row every time a checksum change is detected. This means a patient ID correction in SAMMS will propagate to existing Azure rows.

> **`RowChkSum` is not explicitly updated in the field-mapping block.** For new rows it is set at construction. For existing rows that match on checksum, the existing value already matches. For existing rows where checksum changed — the checksum stored at construction is used. The checksum is effectively managed at construction time, not in the update block.

---

### Step 4b — Checksum Unchanged Path (Existing Rows Only)

If an existing row is found and the checksum has not changed, no data fields are updated. Only activity markers are refreshed:

```
RowState = true
LastModAt = RunDT
```

The row is re-activated (undoing the pre-pass reset) and its timestamp updated, but no data changes.

---

### Step 5 — New Row Insertion (Inline, Not Batched)

Unlike `SaveDoses` which collects all new rows into a list and inserts them at the end via `AddRange`, here each new row is added immediately:

```csharp
db.TblDoseExcuse.Add(dose);
```

This adds each new row to EF Core's change tracker inline during the loop. All pending inserts are then flushed in the single `SaveChanges()` at the end.

---

### Step 6 — Write Everything to the Database (Single Commit)

```
db.SaveChanges()
```

One call commits everything:
- Pre-pass `RowState = false` resets (Step 2)
- Full field updates for checksum-changed existing rows (Step 4a)
- `RowState = true` + `LastModAt` refreshes for unchanged rows (Step 4b)
- All new row inserts (Step 5)

> This is a single-commit design, in contrast to `SaveDoses` which uses two commits (one for updates, one for inserts).

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **Full pre-pass reset** | Every excuse row for the clinic is set to `RowState = false` before processing — no date window filter |
| **Single lookup key** | `ExId` alone identifies a dose excuse record |
| **Checksum gate** | Full field update only fires when `RowChkSum` changed or the row is new |
| **`CltId` refreshed on update** | Patient ID is re-applied from the source on every checksum-changed update |
| **No void logic** | There is no `BlVoid` or `DtVoid` equivalent — excuse records are never soft-deleted based on field values |
| **No negative-CltId logic** | Negative client IDs do not trigger soft-deletes here (unlike `SaveDoses`) |
| **Single commit** | Pre-pass resets, updates, and inserts all committed in one `db.SaveChanges()` |
| **No row count tracking** | No `RowsIns` / `RowsUpd` — method returns a plain `bool` |
| **Errors go to console** | Exceptions are written to `Console.WriteLine`, not captured in a structured return object |
| **New rows added inline** | `db.TblDoseExcuse.Add(dose)` called inside the loop — not batched into a list |

---

## Soft-Delete — How It Works End to End

```
BEFORE RUN:
  Azure has 320 excuse rows for clinic B01, all RowState = true

STEP 2 (pre-pass — NOT yet committed):
  ALL 320 rows are set to RowState = false in memory

STEP 4 (upsert):
  SAMMS returns 308 excuse records today
  → 308 are matched, checksum checked:
      - Unchanged checksums → RowState = true, LastModAt refreshed
      - Changed checksums   → RowState = true, all fields updated

STEP 6 (SaveChanges — single commit):
  All changes written at once

AFTER RUN:
  308 rows → RowState = true  (re-activated by today's data)
   12 rows → RowState = false (not returned from SAMMS — soft-deleted)
```

The 12 rows that stay at `RowState = false` represent dose excuses that were in Azure but did not come back from SAMMS this run — those excuses were deleted or reversed in the source system.

> **Because the pre-pass resets ALL rows (not just date-windowed ones)**, any excuse record not returned by the source query will be soft-deleted — including very old historical records. This is a more aggressive reset than `SaveDoses` applies. The correctness of this approach depends on the source query for this action returning the complete current set of active excuse records for the clinic, not just a recent window.
