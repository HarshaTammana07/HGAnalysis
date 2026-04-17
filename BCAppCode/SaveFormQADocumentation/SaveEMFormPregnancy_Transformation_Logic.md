# `SaveEMFormPregnancy` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveFormQAData.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 617–881  
**What it does:** Reads E&M (Evaluation & Management) form Pregnancy section data from SAMMS and upserts it into the Azure destination table `pats.tbl_eandmformpregnancy`. One row per E&M form — captures all pregnancy-related clinical observations documented by the medical provider during the visit.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows from SAMMS — one row per E&M pregnancy form for this clinic |
| `sc` | string | SiteCode — identifies which clinic this run is for |
| `wrkdt` | DateTime | Passed in but **not used** inside this method — no date filtering applied |
| `db` | DbContext | EF Core database context — created internally if not passed in |

> **Note:** No `f2p` (Forms2Process config) parameter. No form-type configuration — all records processed unconditionally, same as `SaveEMFormMDM`.

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

## How This Relates to `SaveEMFormMDM`

Both methods save E&M form data and share the same overall structure. The key differences:

| Aspect | SaveEMFormMDM | SaveEMFormPregnancy |
|---|---|---|
| Destination table | `pats.tbl_eandmformmdm` | `pats.tbl_eandmformpregnancy` |
| Match key | `Id` | `EandMformId` |
| `LastModAt` | Not tracked | **Tracked** — set to `runDate` captured at method start |
| Source join | EandMForm LEFT JOIN EandMFormMDM | EandMForm INNER JOIN EandMFormPregnancy |
| Column count | 16 columns | 35+ columns (all base fields + pregnancy-specific fields) |
| Pregnancy fields | None | Full pregnancy clinical dataset |

---

## Step-by-Step Logic

### Step 1 — Capture Run Timestamp

```
runDate = DateTime.Now
```

This timestamp is captured **once at the start** of the method and reused for every row's `LastModAt`. This ensures all rows written in the same run share the exact same timestamp — useful for auditing which rows were touched together.

---

### Step 2 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_eandmformpregnancy` for this `SiteCode` are loaded into memory.  
Two lists are maintained:
- `EMs` — existing Azure rows for lookups
- `NewEMs` — staging list for new rows to batch-insert at the end

---

### Step 3 — No Pre-Pass

There is **no soft-reset step**. No RowState column exists on this table. Existing Azure rows are never touched before the upsert begins. Every incoming SAMMS row either updates or inserts — nothing is ever deactivated.

---

### Step 4 — Map Each Source Row to a Destination Object

For every row in the DataTable, each column is mapped to the destination field. Columns are grouped below by their purpose.

#### Identity & Form Metadata Columns

| Source Column | Destination Field | Transformation | Notes |
|---|---|---|---|
| `SiteCode` | `SiteCode` | Stored from source row. Also sets `LastModAt = runDate` | `runDate` is the single timestamp captured in Step 1 |
| `EandMFormId` | `EandMformId` | `int.Parse()` — always required | This is the **primary key** and match key |
| `PreAdmissionId` | `PreAdmissionId` | `int.Parse()` — always required | No empty guard — will throw if missing |
| `ClientId` | `ClientId` | `int.Parse()` if not empty | Skipped if empty |
| `DataFormId` | `DataFormId` | `int.Parse()` if not empty | Skipped if empty |
| `CreatedOn` | `CreatedOn` | Parsed as DateTime if length > 6 | Skipped if empty or short |
| `CreatedBy` | `CreatedBy` | Stored as-is — always | Empty string stored if source is empty |
| `ModifiedOn` | `ModifiedOn` | Parsed as DateTime if length > 6 | Skipped if empty or short |
| `ModifiedBy` | `ModifiedBy` | Stored as-is — always | Empty string stored if source is empty |
| `IsDeleted` | `Isdeleted` | `"1"` → `true`; anything else → `false` | Bool — not used to gate behaviour |
| `FormDate` | `FormDate` | Parsed as DateTime if length > 6 | Clinical date of the form |
| `ServiceId` | `ServiceId` | `int.Parse()` if not empty | Skipped if empty |
| `Context` | `Context` | Stored as-is — always | Free-text context note |
| `Version` | `Version` | Stored as-is — always | Form version string |

#### Pregnancy-Specific Clinical Columns

These columns are unique to this method — they do not exist in `SaveEMFormMDM`.

**Integer / Dropdown selections (skipped if empty):**

| Source Column | Destination Field | What it captures |
|---|---|---|
| `DdlTrimester` | `Ddltrimester` | Which trimester the patient is in (1, 2, or 3) |
| `UdsRadioBtn` | `UdsradioBtn` | Urine Drug Screen radio button selection |
| `SmokerRadioBtn` | `SmokerRadioBtn` | Smoking status radio button selection |

**Bool / Checkbox fields (skipped if empty, `bool.Parse()` applied):**

| Source Column | Destination Field | What it captures |
|---|---|---|
| `Bleeding` | `Bleeding` | Patient reported bleeding |
| `Contraction` | `Contraction` | Patient reported contractions |
| `NauseaVomiting` | `NauseaVomiting` | Patient reported nausea or vomiting |
| `PrenatalCare` | `PrenatalCare` | Whether patient is receiving prenatal care |
| `ReviewedAndAcknowledged` | `ReviewedandAcknowledged` | Provider reviewed and acknowledged the pregnancy section |
| `NaPregnancyGrid` | `NapregnancyGrid` | N/A flag for the pregnancy grid section |

**Free-text fields (stored as-is, always — empty string if source is empty):**

| Source Column | Destination Field | What it captures |
|---|---|---|
| `DoseTxt` | `DoseTxt` | Current MAT medication dose |
| `MgTxt` | `MgTxt` | Milligrams of medication |
| `DoseStabilityTxt` | `DoseStabilityTxt` | Notes on dose stability |
| `SignsTxt` | `SignsTxt` | Clinical signs observed |
| `PregnancyOtherTxt` | `PregnancyOtherTxt` | Other pregnancy-related notes |
| `MedicationsTxt` | `MedicationsTxt` | Other medications patient is taking |
| `prenatalvitiamstxt` | `PrenatalVitaminsTxt` | Prenatal vitamins being taken |
| `AllergiesTxt` | `AllergiesTxt` | Known allergies |
| `ChangesInRoutine` | `ChangesInRoutineTxt` | Any changes in patient routine |
| `IllicitDrugTxt` | `IllicitDrugTxt` | Illicit drug use notes |
| `NoOfPregnanciestxt` | `NoOfPregnanciesTxt` | Number of pregnancies |
| `DeliveriesTxt` | `DeliveriesTxt` | Number of deliveries |
| `NameOfObTxt` | `NameofObtxt` | Name of OB/GYN provider |
| `PregnancyCommentsTxt` | `PregnancyCommentsTxt` | General pregnancy comments |
| `WtTxt` | `Wttxt` | Patient weight |
| `GravidaTxt` | `GravidaTxt` | Gravida count (total pregnancies) |
| `ParaTxt` | `ParaTxt` | Para count (viable births) |
| `Provider` | `Provider` | Provider name for this visit |
| `DateOfLastOb` | `DateOfLastOb` | Date of last OB visit — parsed as DateTime if length > 6 |

> **Source column typo:** The source column is named `prenatalvitiamstxt` (misspelled — "vitiams" instead of "vitamins"). The destination property is correctly spelled `PrenatalVitaminsTxt`. The case label must remain misspelled to match the source.

#### Empty Guard Patterns — Three Types Used

| Pattern | Applied to | Behaviour |
|---|---|---|
| Always stored (no guard) | All free-text fields, `CreatedBy`, `ModifiedBy`, `Context`, `Version`, `Provider` | Empty string written to Azure if source is empty |
| Skipped if empty | `ClientId`, `DataFormId`, `ServiceId`, integer radio buttons, bool checkboxes | Field left as null if source is empty |
| Length > 6 guard | All DateTime fields (`CreatedOn`, `ModifiedOn`, `FormDate`, `DateOfLastOb`) | Skipped if string is 6 characters or fewer |

---

### Step 5 — Lookup: Does This Row Already Exist in Azure?

The method searches the in-memory Azure snapshot using a **single-column key**:

```
EandMformId
```

`EandMformId` is the E&M form's unique identifier — it links directly back to `dbo.EandMForm.ID` in SAMMS. One pregnancy section row exists per E&M form.

---

### Step 6a — Row Found → UPDATE

All fields are updated unconditionally — no checksum check, no date check:

**Base fields updated (shared with SaveEMFormMDM):**
`ClientId`, `Context`, `CreatedBy`, `CreatedOn`, `DataFormId`, `FormDate`, `Isdeleted`, `ModifiedBy`, `ModifiedOn`, `PreAdmissionId`, `ServiceId`, `Version`

**Pregnancy-specific fields updated:**
`Ddltrimester`, `DoseTxt`, `DoseStabilityTxt`, `MgTxt`, `AllergiesTxt`, `Bleeding`, `ChangesInRoutineTxt`, `Contraction`, `DateOfLastOb`, `DeliveriesTxt`, `GravidaTxt`, `IllicitDrugTxt`, `LastModAt`, `MedicationsTxt`, `NameofObtxt`, `NapregnancyGrid`, `NauseaVomiting`, `NoOfPregnanciesTxt`, `ParaTxt`, `PregnancyCommentsTxt`, `PregnancyOtherTxt`, `PrenatalCare`, `PrenatalVitaminsTxt`, `Provider`, `ReviewedandAcknowledged`, `SignsTxt`, `SmokerRadioBtn`, `UdsradioBtn`, `Wttxt`

The following fields are **never changed** on a matched row:

| Field NOT Updated | Why |
|---|---|
| `SiteCode` | Part of the record's identity |
| `EandMformId` | Primary key — never changes |

---

### Step 6b — Row Not Found → INSERT (batched)

New row is added to `NewEMs` staging list. Collected and inserted together at the end.

---

### Step 7 — Write Everything to the Database (Two Commits)

**Commit 1:** Saves all field updates for matched rows.

**Commit 2:** Inserts all new rows via `AddRange` — one bulk insert.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **No pre-pass, no soft-delete** | Never sets any row inactive — only adds or updates |
| **All records loaded every run** | Source SQL has no WHERE clause — full table fetched regardless of date |
| **`wrkdt` is ignored** | Date window parameter is passed in but never used inside this method |
| **`EandMformId` is the sole match key** | One pregnancy record per E&M form — single key is sufficient |
| **`LastModAt` tracked via `runDate`** | Timestamp captured once at method start — all rows in same run share it |
| **`runDate` not `DateTime.Now` per row** | Ensures consistent timestamp across the batch — unlike `SaveAnswerSignatures` which uses `DateTime.Now` inline |
| **`IsDeleted` stored as bool** | `"1"` → `true`, else `false` — stored for reporting only, does not gate anything |
| **Free-text fields always written** | No empty guard — blank string stored if source is empty |
| **Bool fields skipped if empty** | `bool.Parse()` requires a valid value — skipped to avoid parse exception |
| **`prenatalvitiamstxt` typo is intentional** | Source column has this spelling — the case label must match it exactly |
| **`SiteCode` from source row** | Not overridden by `sc` parameter |
| **Inserts are always batched** | `AddRange` + single `SaveChanges` |

---

## What Is the E&M Pregnancy Section?

For pregnant patients enrolled in a MAT (Medication-Assisted Treatment) program, the E&M visit includes a pregnancy-specific section. The provider documents:

- **Obstetric status** — gravida/para counts, trimester, last OB visit, OB provider name
- **Current symptoms** — bleeding, contractions, nausea/vomiting, weight
- **MAT medication context** — current dose, milligrams, dose stability
- **Substance use** — UDS result, smoking status, illicit drug use
- **Support** — prenatal vitamins, allergies, medications, prenatal care status
- **Provider acknowledgment** — `ReviewedAndAcknowledged` flag, provider name, signature

This data feeds compliance reporting for pregnant patients on MAT programs — tracking that prenatal care coordination and appropriate medical oversight are in place.

---

## Source Query Context (from BHGTaskRunner)

The SQL that produces the DataTable for this method is:

```
FROM dbo.EandMForm a
INNER JOIN dbo.EandMFormPregnancy c ON (a.ID = c.EandMFormID)
INNER JOIN SF_PatientPreAdmission b ON (a.PreAdmissionID = b.ID)
```

Key differences from `SaveEMFormMDM` source:
- Uses **INNER JOIN** to `EandMFormPregnancy` (not LEFT JOIN) — only E&M forms that actually have a pregnancy section are returned
- Uses `c.*` — all columns from `EandMFormPregnancy` are selected without being named individually in the SQL

`IsDeleted` is computed as:
- `1` if `a.Isdeleted = 1` OR `b.IsDeleted = 1`
- `0` otherwise

The WHERE clause is **commented out** — all records loaded every run, same as `SaveEMFormMDM`.
