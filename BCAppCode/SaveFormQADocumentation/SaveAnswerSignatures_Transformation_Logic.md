# `SaveAnswerSignatures` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveFormQAData.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 253–485  
**What it does:** Reads form signature data from SAMMS and upserts it into the Azure destination table `pats.tbl_dbo_FormAnswerSignatures`. One row per form instance — tracks the date each type of clinical signature was applied to a form.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows fetched from SAMMS — one row per form, with up to 9 signature dates pivoted as columns |
| `sc` | string | SiteCode — identifies which clinic this run is for (e.g. `"B01"`, `"NC"`) |
| `wrkdt` | DateTime | Date window boundary — rows on or after this date are considered "in scope" |
| `f2p` | List | Form processing config — loaded from `ctrl.tbl_Forms2Process` in Azure |
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

## How This Differs From `SaveFormQuestionAnswers`

| Aspect | SaveFormQuestionAnswers | SaveAnswerSignatures |
|---|---|---|
| Rows per form | Many (one per question/answer) | One (all signatures rolled into one row) |
| Composite key columns | 7 | 4 |
| RowChkSum used | No | Yes — stored, but comparison is **commented out** |
| Pre-pass SaveChanges | Deferred (no SaveChanges after pre-pass) | **Committed immediately** after pre-pass |
| ClientId stored | As-is (can be negative) | **Absolute value** always stored |
| Error handling in pre-pass | Not wrapped per row | Each row wrapped in its own try/catch — one bad row does not stop the others |

---

## Step-by-Step Logic

### Step 1 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_dbo_FormAnswerSignatures` for this `SiteCode` are loaded into memory.  
All lookups in subsequent steps run against this in-memory snapshot.

---

### Step 2 — Pre-Pass: Mark Rows as Inactive (Soft-Reset)

Same concept as `SaveFormQuestionAnswers` — existing Azure rows in scope are set to `RowState = 0` before fresh data re-activates them.

#### FormName Normalization

Any `FormName` starting with `"TP-"` is treated as `"Treatment Plan"` when looking up in the form config. This normalizes all Treatment Plan variants into one config entry.

#### The Reset Decision

| Scenario | What happens to RowState |
|---|---|
| Form found in config, `DateFilterEnabled = false` | Always reset to `0` — no date check, full replacement every run |
| Form found in config, `DateFilterEnabled = true`, row within date window | Reset to `0` |
| Form found in config, `DateFilterEnabled = true`, row outside date window | Left unchanged |
| Form NOT found in config at all | Same date-based check — reset to `0` if within window |

**"Within the date window"** means:  
`CreatedOn >= wrkdt` **OR** `UpdatedOn >= wrkdt`

#### Key Difference — Error Handling in Pre-Pass

Each row's reset logic is wrapped individually in a `try/catch` that **silently swallows exceptions**. If a single row has a null `CreatedOn` or any parse error, that row is skipped without failing the entire pre-pass. This is different from `SaveFormQuestionAnswers` which has no per-row protection here.

#### Key Difference — SaveChanges Called Immediately After Pre-Pass

Unlike `SaveFormQuestionAnswers` where the pre-pass resets are deferred, here `db.SaveChanges()` is called **right after the pre-pass loop**. The soft-resets are committed to Azure before the upsert even begins.

---

### Step 3 — Map Each Source Row to a Destination Object

For every row coming in from SAMMS, each column is mapped to the destination field.

#### Column Transformations

| Source Column | Destination Field | Transformation Applied |
|---|---|---|
| `SiteCode` | `SiteCode` | Stored from the **source row** (unlike `SaveFormQuestionAnswers` which uses the parameter). Also sets `RowState = 1` and `LastModAt = now` as defaults |
| `FormName` | `FormName` | Stored as-is |
| `FormId` | `FormId` | Converted to **UPPER CASE** |
| `ClientId` | `ClientId` | Stored as `Math.Abs()` — **always the absolute (positive) value**. Then checks original sign to set `RowState = 0` if negative |
| `CreatedOn` | `CreatedOn` | Parsed as DateTime. Only stored if string length > 6 |
| `UpdatedOn` | `UpdatedOn` | Parsed as DateTime. Only stored if string length > 6 |
| `CompletedBySignatureSignatureDate` | `CompletedBySignatureSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `CounselorSignatureSignatureDate` | `CounselorSignatureSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `DoctorSignatureSignatureDate` | `DoctorSignatureSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `MedicalProviderSignatureSignatureDate` | `MedicalProviderSignatureSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `PatientSignatureDate` | `PatientSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `ProviderSignatureSignatureDate` | `ProviderSignatureSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `RequestorSignatureDate` | `RequestorSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `StaffSignatureDate` | `StaffSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `SupervisorSignatureSignatureDate` | `SupervisorSignatureSignatureDate` | Parsed as DateTime. Only stored if length > 6 |
| `RowChkSum` | `RowChkSum` | Parsed as integer. Stored for reference |
| `IsDeleted` | `RowState` | Complex logic — see below |

> **Why length > 6?** Signature dates arriving as `"1/1/1900"` (the sentinel for "unsigned") are 8 characters and DO get stored. Truly empty strings (`""`) or very short garbage values are skipped. This is a length guard, not a value guard.

#### How RowState Gets Set During Mapping

RowState is set in two places and the last one processed wins:

**1. `SiteCode` column sets the default:**
- `RowState = 1` (active) — this is the starting assumption for every new row

**2. `ClientId` can lower it:**
- Original value is negative → `RowState = 0`
- `ClientId` stored value is always `Math.Abs()` regardless (always positive in destination)

**3. `IsDeleted` is the final override:**

| IsDeleted value | ClientId at that point | Final RowState |
|---|---|---|
| `"1"` | Any | `0` — deleted |
| `"0"` | `< 0` (was negative) | `0` — deleted (ClientId negative takes precedence when IsDeleted is not "1") |
| `"0"` | `>= 0` | `1` — active |
| Empty string | Any | RowState unchanged from what ClientId set |

> **Key difference from `SaveFormQuestionAnswers`:** Here the `isdeleted = "0"` branch also checks `ClientId`. A negative ClientId **combined with** `IsDeleted = "0"` still results in `RowState = 0`. In `SaveFormQuestionAnswers`, `IsDeleted = "0"` always forces `RowState = 1` unconditionally.

---

### Step 4 — Lookup: Does This Row Already Exist in Azure?

The method searches the in-memory Azure snapshot using a **4-column composite key**:

```
SiteCode + FormName + FormId (upper) + ClientId
```

This key uniquely identifies: *one form instance for one patient at one clinic.*

> **Simpler key than SaveFormQuestionAnswers** (7 columns) because signatures are one row per form — not one row per question.

---

### Step 5a — Row Found → UPDATE

If a match is found, **all signature-related fields are updated unconditionally**. There is no checksum comparison gate:

```
// This check is commented out — updates happen regardless:
// if (dbAns.RowChkSum != a.RowChkSum)
```

| Fields Updated |
|---|
| `RowChkSum` |
| `CreatedOn` |
| `UpdatedOn` |
| `CompletedBySignatureSignatureDate` |
| `CounselorSignatureSignatureDate` |
| `DoctorSignatureSignatureDate` |
| `MedicalProviderSignatureSignatureDate` |
| `PatientSignatureDate` |
| `ProviderSignatureSignatureDate` |
| `RequestorSignatureDate` |
| `StaffSignatureDate` |
| `SupervisorSignatureSignatureDate` |
| `LastModAt` |
| `RowState` |

The following fields are **never changed** once a row exists:

| Fields NOT Updated | Why |
|---|---|
| `SiteCode`, `FormName`, `FormId`, `ClientId` | These are the identity of the record |

> **Why is the RowChkSum check commented out?** The intent was to skip the update if nothing changed (same as how BulkDartsSrv works). The comment was removed — meaning every run always refreshes all signature dates regardless of whether they changed. This is intentional for signatures because a new signature on an old form must always be captured.

---

### Step 5b — Row Not Found → INSERT (batched)

If no match is found, the new row is added to a staging list (`newAns`). All new rows are collected first, then inserted together in one batch at the end.

---

### Step 6 — Write Everything to the Database (Two Commits)

**Commit 1 (after upsert loop):** Saves all field updates for matched rows.

**Commit 2 (inserts):** Inserts all new rows via `AddRange` — one bulk insert, not row-by-row.

> Note: Unlike `SaveFormQuestionAnswers` which has **one** commit covering both pre-pass resets AND updates, here the pre-pass resets were already committed in Step 2. So this commit covers only the upsert results.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **TP- forms normalized to one config entry** | `TP-Initial`, `TP-Quarterly` etc. → `"Treatment Plan"` for config lookup |
| **SiteCode comes from the source row** | Unlike `SaveFormQuestionAnswers`, the source value is used here (not the `sc` parameter) |
| **ClientId always stored as positive** | `Math.Abs()` applied — destination never holds a negative ClientId |
| **Negative source ClientId = deleted** | If source ClientId was negative, `RowState = 0` even though stored ClientId is positive |
| **IsDeleted = "0" + negative ClientId = still deleted** | Both signals must agree to produce `RowState = 1` |
| **FormId always stored upper-case** | Case-insensitive matching at lookup time |
| **Signature dates use length > 6 guard** | Catches empty strings and very short garbage values — `"1/1/1900"` (8 chars) is valid and stored |
| **RowChkSum is stored but NOT used as a gate** | The comparison check is commented out — every matched row is always fully updated |
| **Pre-pass errors are silently swallowed per row** | Individual bad rows in the pre-pass do not abort the whole run |
| **Pre-pass is committed immediately** | `db.SaveChanges()` is called right after the pre-pass — before the upsert begins |
| **Inserts are always batched** | `AddRange` + single `SaveChanges` at the end |

---

## The 9 Signature Date Fields — What They Represent

Each field captures the date a specific role signed the form. If the source `AnswerSignature` table has a row for that field but the signature itself is null (not yet signed), the ETL stores `1900-01-01` as a sentinel — meaning *"the slot exists but is unsigned"*, which is different from null (slot never submitted).

| Field | Who signed |
|---|---|
| `CompletedBySignatureSignatureDate` | Person who completed/submitted the form |
| `CounselorSignatureSignatureDate` | Counselor |
| `DoctorSignatureSignatureDate` | Doctor |
| `MedicalProviderSignatureSignatureDate` | Medical provider (NP, PA, MD) |
| `PatientSignatureDate` | Patient |
| `ProviderSignatureSignatureDate` | Generic provider |
| `RequestorSignatureDate` | Person who requested the form |
| `StaffSignatureDate` | Staff member |
| `SupervisorSignatureSignatureDate` | Supervisor |

---

## Soft-Delete — How It Works End to End

```
BEFORE RUN:
  Azure has 500 signature rows for clinic B01, all RowState = 1

STEP 2 (pre-pass — committed immediately):
  150 rows are within the date window
  → those 150 rows are set to RowState = 0 and saved to Azure

STEP 5 (upsert):
  SAMMS returns 140 of those 150 form rows today
  → those 140 are matched, all signature dates refreshed, RowState set back to 1

AFTER RUN:
  140 rows → RowState = 1  (re-activated — signatures refreshed)
   10 rows → RowState = 0  (not seen in today's data — soft-deleted)
  350 rows → RowState = 1  (outside date window — untouched)
```

The 10 rows that remained at `RowState = 0` represent forms whose signatures were in Azure but did not return from SAMMS — those forms were deleted or the patient was deactivated in the source system.
