# `SaveFormQuestionAnswers` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveFormQAData.cs`  
**Class:** `SaveData` (partial)  
**What it does:** Reads form question/answer rows from SAMMS (clinic SQL Server) and upserts them into the Azure destination table `pats.tbl_dbo_FormQuestionAnswers`.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows fetched from SAMMS — all Q&A data for this clinic in the date window |
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

## Step-by-Step Logic

### Step 1 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_dbo_FormQuestionAnswers` for this `SiteCode` are loaded into memory.  
Every lookup and comparison in the steps below runs against this in-memory snapshot — not against the database repeatedly.

---

### Step 2 — Pre-Pass: Mark Rows as Inactive (Soft-Reset)

Before processing any new data, the method walks through every existing Azure row and decides whether to set its `RowState = 0` (inactive).

This is the **soft-delete preparation** step. The idea is:
- Mark rows that are "in scope" as inactive first
- Then let the fresh SAMMS data re-activate them
- Any row that was marked inactive but never re-activated = it was deleted or removed in the source

#### FormName Normalization (done before the check)

Any `FormName` that starts with `"TP-"` (e.g. `TP-Initial`, `TP-Quarterly`) is treated as `"Treatment Plan"` when looking up in the form config (`f2p`). This normalizes all Treatment Plan variants into one config entry.

#### The Reset Decision

| Scenario | What happens to RowState |
|---|---|
| Form found in config, `DateFilterEnabled = false` | Always reset to `0` — no date check, full replacement every run |
| Form found in config, `DateFilterEnabled = true`, row is within the date window | Reset to `0` |
| Form found in config, `DateFilterEnabled = true`, row is outside the date window | Left unchanged |
| Form NOT found in config at all | Same date-based check as above — reset to `0` if within window |

**"Within the date window"** means:  
`CreatedOn >= wrkdt` **OR** `UpdatedOn >= wrkdt`  
(whichever date is available)

> **Important:** These RowState changes are NOT written to the database yet. They are held in memory and committed later together with all the upsert writes — one single batch at the end.

---

### Step 3 — Map Each Source Row to a Destination Object

For every row coming in from SAMMS, the method reads each column and maps it to the corresponding destination field.

#### Column Transformations

| Source Column | Destination Field | Transformation Applied |
|---|---|---|
| `SiteCode` | `SiteCode` | **Ignored** — always uses the `sc` parameter instead. Also sets `LastModAt = now` |
| `FormName` | `FormName` | Stored as-is (no normalization here — raw value from source) |
| `FormId` | `FormId` | Converted to **UPPER CASE** — ensures case-insensitive matching |
| `ClientId` | `ClientId` | Parsed as integer. If empty → stored as `0`. Also sets initial `RowState` (see below) |
| `CreatedOn` | `CreatedOn` | Parsed as DateTime. Skipped if empty |
| `CreatedBy` | `CreatedBy` | Stored as string. Skipped if empty |
| `UpdatedOn` | `UpdatedOn` | Parsed as DateTime. Skipped if empty |
| `UpdatedBy` | `UpdatedBy` | Stored as string. Skipped if empty |
| `PreAdmissionId` | `PreAdmissionId` | Parsed as integer. If empty → stored as **`-1`** (not 0 — `-1` means no context) |
| `IsDeleted` | `IsDeleted` + `RowState` | `"1"` → `IsDeleted = true`, `RowState = 0`. `"0"` or empty → `RowState = 1` |
| `IsChildForm` | `IsChildForm` | `"1"` → `true`, anything else → `false`. Skipped entirely if empty |
| `QuestionId` | `QuestionId` | Parsed as integer. If empty → stored as **`0`** (forms with no mapped questions) |
| `QuestionOrderId` | `QuestionOrderId` | Parsed as integer. Skipped if empty |
| `QuestionText` | `QuestionText` | Stored as string. Skipped if empty |
| `OptionId` | `OptionId` | Stored as string. Skipped if empty |
| `AnswerValue` | `AnswerValue` | Stored as string. Skipped if empty |

#### How RowState Gets Set During Mapping

`RowState` is determined by **two columns** and the last one processed wins:

1. **`ClientId` sets it first:**
   - `ClientId < 0` → `RowState = 0` (negative ID = patient was deleted in source)
   - `ClientId >= 0` → `RowState = 1`

2. **`IsDeleted` overrides it:**
   - `IsDeleted = "1"` → `RowState = 0` (overrides even a valid positive ClientId)
   - `IsDeleted = "0"` or empty → `RowState = 1` (overrides even a negative ClientId)

> `IsDeleted` is the **final authority** on whether a row is active or inactive.

---

### Step 4 — Lookup: Does This Row Already Exist in Azure?

For each mapped row, the method searches the in-memory Azure snapshot using a **7-column composite key**:

```
SiteCode + FormName + FormId + ClientId + PreAdmissionId + QuestionId + QuestionOrderId
```

This key uniquely identifies: *one answer, to one question, on one specific form instance, for one patient, at one clinic.*

---

### Step 5a — Row Found → UPDATE

If the row already exists in Azure, only these fields are updated:

| Fields Updated |
|---|
| `PreAdmissionId` |
| `CreatedBy` |
| `CreatedOn` |
| `UpdatedBy` |
| `UpdatedOn` |
| `AnswerValue` |
| `OptionId` |
| `LastModAt` |
| `RowState` |

The following fields are **never changed** once a row exists:

| Fields NOT Updated | Why |
|---|---|
| `SiteCode`, `FormName`, `FormId`, `ClientId` | These are the identity of the record — they cannot change |
| `QuestionId`, `QuestionOrderId` | Part of the composite key |
| `QuestionText` | This is template metadata, not the patient's answer — treated as stable once stored |
| `IsChildForm`, `IsDeleted` | Not refreshed on update |

---

### Step 5b — Row Not Found → INSERT (batched)

If no match is found, the new row is added to a **staging list** (`newfqas`). It is not written to the database yet — all new rows are collected first, then inserted together in one batch at the end.

---

### Step 6 — Write Everything to the Database (Two Commits)

**Commit 1:** Saves all field updates AND all the RowState soft-resets from Step 2 together in one transaction.

**Commit 2:** Inserts all new rows from the staging list together via `AddRange` — one bulk insert, not row-by-row.

> Splitting into two commits is intentional: updates (including the pre-pass resets) go first, then inserts. This avoids conflicts between the pre-pass reset and a brand-new insert for the same key.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **TP- forms are all one config entry** | `TP-Initial`, `TP-Quarterly` etc. all map to `"Treatment Plan"` in the form config lookup |
| **SiteCode always comes from the parameter** | The `SiteCode` column in the source row is ignored — `sc` is used. Prevents data from landing in the wrong clinic |
| **FormId is always stored upper-case** | Prevents duplicate rows from mixed-case GUIDs |
| **Empty PreAdmissionId = -1, not 0** | `-1` explicitly means "no pre-admission context". `0` is a valid ID |
| **Empty QuestionId = 0** | Represents forms that were submitted but have no questions mapped in the template |
| **Negative ClientId = soft-deleted** | Source uses negative IDs to signal patient deletion |
| **IsDeleted wins over ClientId for RowState** | If `IsDeleted = "0"`, the row is active regardless of a negative ClientId |
| **Date window controls soft-delete scope** | Only rows with `CreatedOn` or `UpdatedOn >= wrkdt` are reset in the pre-pass |
| **DateFilterEnabled = false = full replacement** | All rows for that form type are reset to inactive on every run |
| **QuestionText is never updated** | Template question text is stable — only the answer (`AnswerValue`, `OptionId`) changes |
| **Inserts are always batched** | `AddRange` + single `SaveChanges` — never one insert per row |

---

## What the `ctrl.tbl_Forms2Process` Config Controls

This table in Azure tells the method how to handle each form type:

| Config Column | Effect |
|---|---|
| `FormName` | Which form name this config row applies to |
| `DateFilterEnabled = true` | Only reset rows that fall within the date window |
| `DateFilterEnabled = false` | Reset ALL rows for this form type on every run (full replacement) |
| `Enabled = false` | This form type is skipped entirely — never included in the source query |

---

## Soft-Delete — How It Works End to End

```
BEFORE RUN:
  Azure has 1,000 rows for clinic B01, RowState = 1 (all active)

STEP 2 (pre-pass):
  300 rows have CreatedOn or UpdatedOn >= wrkdt
  → those 300 rows are temporarily set to RowState = 0

STEP 5 (upsert):
  SAMMS returns 280 of those 300 rows today
  → those 280 are matched and their RowState is set back to 1

AFTER RUN:
  280 rows → RowState = 1  (re-activated — still exist in source)
   20 rows → RowState = 0  (not seen in today's data — soft-deleted)
  700 rows → RowState = 1  (outside the date window — untouched)
```

The 20 rows that remained at `RowState = 0` represent records that were in Azure but did not come back from SAMMS — they were deleted or deactivated in the clinic's system.
