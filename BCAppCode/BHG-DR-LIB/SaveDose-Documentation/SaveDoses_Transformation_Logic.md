# `SaveDoses` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveDoses.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 11–207  
**What it does:** Reads medication dose records from a SAMMS clinic database and upserts them into the Azure destination table `pats.tbl_Dose`. One row per dose administration event per patient per clinic.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows fetched from SAMMS — one row per dose record |
| `sc` | string | SiteCode — identifies which clinic this run is for (e.g. `"B01"`, `"PHC"`) |
| `dtWrk` | DateTime | Date window boundary — drives both the pre-pass reset scope and the source query window |
| `reload` | bool | If `true`, hard-deletes all existing Azure rows for this SiteCode before processing |
| `db` | DbContext | EF Core database context — created internally if not passed in |

---

## Output — RCodes

| Field | Meaning |
|---|---|
| `IsResult` | `true` = success, `false` = exception occurred |
| `RowsProcessed` | Total rows in the incoming DataTable |
| `RowsIns` | How many new rows were inserted |
| `RowsUpd` | How many existing rows were updated |
| `ExceptMsg` | Exception message if the run failed |
| `ExceptInnerMsg` | Inner exception detail if available |

---

## How This Differs From `SaveDoseExcuse`

| Aspect | SaveDoses | SaveDoseExcuse |
|---|---|---|
| Return type | `RCodes` (full audit) | `bool` (pass/fail only) |
| Hard reload option | Yes — `reload` parameter deletes all rows first | No — no hard delete option |
| Pre-pass scope | Date-filtered: only resets rows where `DtDate >= dtWrk` | Full reset: ALL rows for the clinic set to `RowState = false` |
| Void/delete logic | Yes — `BlVoid + DtVoid` check + negative `CltId` guard | None |
| New row batching | Collected into a `newdoses` list, inserted via `AddRange` | Added inline one by one via `db.TblDoseExcuse.Add()` |
| Row count tracking | `RowsIns` / `RowsUpd` incremented | No tracking |
| Special sentinel | `CltId = -111` is negative but NOT treated as deleted | Not applicable |
| PHC hardcoding | If SiteCode == `"PHC"`, `SiteId` is forced to `105` | Not applicable |
| Optional column | `InventoryGroup` only mapped if column exists in source DataTable | Not applicable |

---

## Step-by-Step Logic

### Step 0 — Hard Reload (optional)

If `reload == true`, the method executes a raw SQL DELETE before touching any EF Core objects:

```sql
Delete from pats.tbl_Dose where SiteCode = '<sc>'
```

This wipes the entire site's dose history from Azure first. The rest of the method then re-inserts everything from the source DataTable as brand-new rows. This path is used for historical backfills or full re-sync scenarios.

---

### Step 1 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_Dose` for this `SiteCode` are loaded into memory.  
All lookups in subsequent steps run against this in-memory snapshot.

If `doses.Count == 0`, the flag `AllNewRows = true` is set — this bypasses the lookup step and takes a faster insert-only path.

---

### Step 2 — Pre-Pass: Mark In-Window Rows as Inactive (Soft-Reset)

Unlike `SaveDoseExcuse` which resets every row, this method only resets rows that fall inside the date window:

```
d.DtDate >= dtWrk.Date  →  d.RowState = false
```

Rows outside the date window are left completely untouched. This means old historical dose records that predate the lookback window keep their current `RowState` regardless of whether SAMMS returns them this run.

> **No SaveChanges here.** The pre-pass resets are not committed to Azure until the end of the method.

---

### Step 3 — Per-Row Lookup or New Row Construction

For every row coming in from SAMMS, three key fields are parsed eagerly before any branching:

| Source Column | Parsed As | Notes |
|---|---|---|
| `DoseId` | `long` | Primary identity key |
| `cltid` | `int` | Patient/client ID |
| `rowchksum` | `int` | Source checksum |
| `dtmeddate` | `DateTime` | Always parsed and stored at construction |

**If `AllNewRows == true`** (no prior Azure data for this clinic):  
A new `TblDose` is constructed immediately with: `SiteCode`, `DoseId`, `CltId`, `RowChkSum = 0`, `DtMedDate`.  
`RowChkSum` is set to `0` at construction so the checksum gate always fires on first write.

**If existing rows are present:**  
A lookup is performed against the in-memory snapshot using `DoseId` as the single key.

- Match found → `RowsUpd++`
- No match → new `TblDose` constructed with `RowState = true`, `NewRow = true`, `RowsIns++`

---

### Step 4 — Full Field Mapping (New Row or Checksum Changed)

The full field update fires if `NewRow == true` OR `rcs != dose.RowChkSum`.

#### Column Transformations

| Source Column | Destination Field | Guard / Transformation |
|---|---|---|
| *(implicit)* | `RowState` | Always set to `true` (active) |
| *(implicit)* | `LastModAt` | Set to `RunDT` (DateTime.Now at method start) |
| `rowchksum` | `RowChkSum` | Stored as-is |
| `dtdate` | `DtDate` | DateTime — only stored if string length > 6 |
| `guestid` | `GuestId` | int — only stored if string length > 0 |
| `dose` | `Dose` | int — only stored if string length > 0 |
| `struser` | `StrUser` | Always stored |
| `strvoidreason` | `StrVoidReason` | Always stored |
| `bottletype` | `Bottletype` | Always stored |
| `Blvoid` | `BlVoid` | bool — only stored if string length > 0 |
| `blexception` | `BlException` | bool — only stored if string length > 0 |
| `ordernum` | `Ordernum` | int — only stored if string length > 0 |
| `exceptionreason` | `ExceptionReason` | Always stored |
| `blbulk` | `BlBulk` | bool — only stored if string length > 0 |
| `blprepack` | `BlPrepack` | bool — only stored if string length > 0 |
| `dtvoid` | `DtVoid` | **bool** — only stored if string length > 0. Despite the `dt` prefix, this is a boolean flag, not a DateTime |
| `dtgiven` | `Dtgiven` | DateTime — only stored if string length > 6 |
| `dtprep` | `Dtprep` | DateTime — stored if length > 0 (**not > 6** — shorter guard than other date fields) |
| `ppstaff` | `Ppstaff` | Always stored |
| `exceptiontype` | `Exceptiontype` | Always stored |
| `Manualauthuser` | `Manualauthuser` | Always stored |
| `manualauthdtm` | `Manualauthdtm` | DateTime — only stored if string length > 6 |
| `dosenote` | `Dosenote` | Always stored |
| `dosesig` | `Dosesig` | Always stored |
| `inventorygroup` | `InventoryGroup` | Only mapped if column **exists** in the source DataTable — guarded by `tbl.Columns.Contains("InventoryGroup")` |
| `siteid` | `SiteId` | int — only stored if length > 0. **Exception:** if `SiteCode == "PHC"`, always hardcoded to `105` |
| `dosesigimg` | `DoseSigImg` | byte[] — always stored as `System.Text.Encoding.ASCII.GetBytes(value)` |

> **`dtmeddate` is handled at construction, not here.** It is parsed eagerly before the `AllNewRows` branch and set on the new object. The conditional storage that existed previously is commented out.

> **`InventoryGroup` column guard** allows this same method to run against multiple SAMMS schema versions where some older clinics may not have that column in their source extract.

> **`dtprep` uses length > 0**, not the standard length > 6 guard used by all other DateTime fields. An empty string would be silently filtered, but a very short non-parseable string would throw a parse exception.

#### Void Check (Applied After Field Mapping)

```
if (BlVoid == true && DtVoid == true)  →  RowState = false
```

Both flags must be `true` to trigger the void. A dose with only `BlVoid = true` but `DtVoid = false` is **not** voided.

---

### Step 4b — Checksum Unchanged Path (Existing Rows Only)

If an existing row is found and the checksum has not changed, no fields are updated. Only state is refreshed:

```
RowState = true
if (BlVoid == true && DtVoid == true)  →  RowState = false
if (CltId < 0 && CltId != -111)        →  RowState = false
```

> **`CltId = -111` is a special sentinel.** A negative `CltId` normally signals that the patient/row should be soft-deleted. The value `-111` is exempt — it is treated as a valid active record despite being negative. All other negative `CltId` values result in `RowState = false`.

---

### Step 5 — PHC SiteId Override

When `SiteCode == "PHC"`, `SiteId` is always forced to `105` regardless of what the source row provides. This hardcoded clinic identity is specific to the PHC pipeline feeding into the same `pats.tbl_Dose` table.

---

### Step 6 — Write Everything to the Database (Two Commits)

**Commit 1 (after upsert loop):** `db.SaveChanges()` — commits all field updates made to existing matched rows, and also flushes the pre-pass `RowState = false` resets that were deferred since Step 2.

**Commit 2 (inserts):** All new rows collected in `newdoses` are inserted in one batch via `db.TblDose.AddRange(newdoses)` then `db.SaveChanges()`.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **Hard reload wipes first** | `reload = true` executes a raw DELETE before any EF processing |
| **Pre-pass is date-filtered** | Only doses with `DtDate >= dtWrk` are soft-reset — historical doses outside the window are never touched |
| **Single lookup key** | `DoseId` alone identifies a dose — no composite key needed |
| **Checksum gate** | Full field update only fires when `RowChkSum` changed or the row is new — unchanged rows only get `RowState` refreshed |
| **Void = both flags true** | `BlVoid + DtVoid` must both be `true` to soft-delete a dose |
| **-111 is a protected CltId** | Negative CltId means deleted, EXCEPT `-111` which is a known sentinel treated as active |
| **InventoryGroup is schema-optional** | Column is only read if it exists in the source DataTable — supports older SAMMS schemas |
| **PHC gets hardcoded SiteId 105** | PHC clinic bypasses the `siteid` column and always writes `105` |
| **DoseSigImg always stored** | Signature image is always stored as ASCII bytes regardless of content |
| **`dtvoid` is a bool not a DateTime** | The `dt` prefix is misleading — this field represents a boolean void status, not a date |
| **Inserts are batched** | All new rows collected into a list, then inserted in one `AddRange` call |
| **Pre-pass not separately committed** | Pre-pass resets and upsert updates are committed together in the first `SaveChanges` |

---

## Soft-Delete — How It Works End to End

```
BEFORE RUN:
  Azure has 1,200 dose rows for clinic B01, all RowState = true
  400 of those rows have DtDate >= dtWrk (within the lookback window)

STEP 2 (pre-pass — NOT yet committed):
  Those 400 rows are set to RowState = false in memory

STEP 4 (upsert):
  SAMMS returns 385 of those 400 doses today
  → 385 are matched, checksums checked, fields refreshed where needed, RowState set back to true

STEP 6 (SaveChanges — Commit 1):
  385 reactivated rows written to Azure
  15 rows remain RowState = false (not returned from SAMMS — soft-deleted)

AFTER RUN:
  385 rows → RowState = true  (re-activated — data refreshed if checksum changed)
   15 rows → RowState = false (not seen in today's data — dose was voided or deleted in source)
  800 rows → RowState = true  (outside lookback window — completely untouched)
```

The 15 rows that stay at `RowState = false` represent dose records that were in Azure's lookback window but not returned by SAMMS — indicating those doses were voided or removed at the source clinic.
