# `SaveEMFormMDM` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveFormQAData.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 486–616  
**What it does:** Reads E&M (Evaluation & Management) form MDM (Medical Decision Making) data from SAMMS and upserts it into the Azure destination table `pats.tbl_eandmformmdm`. One row per E&M form — captures the medical provider's decision making documentation and signature.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows fetched from SAMMS — one row per E&M form for this clinic |
| `sc` | string | SiteCode — identifies which clinic this run is for |
| `wrkdt` | DateTime | Passed in but **not used** inside this method — no date filtering applied |
| `db` | DbContext | EF Core database context — created internally if not passed in |

> **Note:** There is no `f2p` (Forms2Process config) parameter here. This method has no form-type-level configuration — it processes all E&M MDM records unconditionally.

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

## How This Differs From the Other Form Save Methods

| Aspect | SaveFormQuestionAnswers / SaveAnswerSignatures | SaveEMFormMDM |
|---|---|---|
| Pre-pass soft-reset | Yes — RowState is reset before upsert | **None** — no pre-pass at all |
| Date window filtering | Yes — `wrkdt` drives the pre-pass | **Not used** — all records loaded every run |
| Form config (`f2p`) | Yes — controls date filter per form type | **Not present** — no config lookup |
| RowState field | Actively managed (0/1) | **Not present** — no RowState on this table |
| RowChkSum | Present on Signatures | **Not present** |
| Soft-delete field | `RowState` | `Isdeleted` (bool) — stored but not used to gate anything |
| Composite key | Multi-column | **Single column: `Id`** |
| Source SQL WHERE clause | Date-filtered | **No WHERE clause** — full table loaded every run |

---

## Step-by-Step Logic

### Step 1 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_eandmformmdm` for this `SiteCode` are loaded into memory.  
Two lists are maintained:
- `EMs` — all existing Azure rows (used for lookups)
- `NewEMs` — staging list for new rows to insert at the end

---

### Step 2 — No Pre-Pass

There is **no soft-reset step** in this method. Existing Azure rows are never touched before the upsert begins. Every row from SAMMS either updates an existing record or creates a new one. Nothing is ever deactivated.

---

### Step 3 — Map Each Source Row to a Destination Object

For every row in the DataTable, each column is mapped to the destination field.

#### Column Transformations

| Source Column | Destination Field | Transformation | Notes |
|---|---|---|---|
| `SiteCode` | `SiteCode` | Stored directly from source row | No override with `sc` parameter — source value used |
| `Id` | `Id` | `int.Parse()` — always required | This is the primary key |
| `PreAdmissionId` | `PreAdmissionId` | `int.Parse()` — always required | No empty guard — will throw if missing |
| `ClientId` | `ClientId` | `int.Parse()` if not empty | Skipped if empty |
| `DataFormId` | `DataFormId` | `int.Parse()` if not empty | Skipped if empty |
| `CreatedOn` | `CreatedOn` | Parsed as DateTime if length > 6 | Skipped if empty or very short |
| `CreatedBy` | `CreatedBy` | Stored as-is — **always** (no empty guard) | Empty string stored if source is empty |
| `ModifiedOn` | `ModifiedOn` | Parsed as DateTime if length > 6 | Skipped if empty or very short |
| `ModifiedBy` | `ModifiedBy` | Stored as-is — **always** (no empty guard) | Empty string stored if source is empty |
| `IsDeleted` | `Isdeleted` | `"1"` → `true`; anything else → `false` | Simple bool — no RowState involvement |
| `FormDate` | `FormDate` | Parsed as DateTime if length > 6 | The clinical date of the form |
| `ServiceId` | `ServiceId` | `int.Parse()` if not empty | Skipped if empty |
| `Context` | `Context` | Stored as-is — **always** (no empty guard) | Free-text note |
| `Version` | `Version` | Stored as-is — **always** (no empty guard) | Form version string |
| `MedicalProviderSignatureDate` | `MedicalProviderSignatureDate` | Parsed as DateTime if length > 6 | Date the medical provider signed the MDM |
| `MedicalProviderSignatureBy` | `MedicalProviderSignatureBy` | Stored as-is — **always** (no empty guard) | Username of the signing provider |

#### Empty Guard Behaviour — Three Patterns Used

| Pattern | Columns | Behaviour |
|---|---|---|
| Always stored (no guard) | `CreatedBy`, `ModifiedBy`, `Context`, `Version`, `MedicalProviderSignatureBy` | Empty string written to Azure if source is empty |
| Skipped if empty | `ClientId`, `DataFormId`, `ServiceId` | Field left as null if source is empty |
| Length > 6 guard | `CreatedOn`, `ModifiedOn`, `FormDate`, `MedicalProviderSignatureDate` | DateTime fields skipped if string is 6 chars or fewer |

---

### Step 4 — Lookup: Does This Row Already Exist in Azure?

The method searches the in-memory Azure snapshot using a **single-column key**:

```
Id
```

`Id` is the E&M form's unique identifier within the clinic. It is the only field used for matching.

> **Simpler than all other form methods** — no SiteCode, FormName, or ClientId needed in the key because `Id` is already unique per form instance.

---

### Step 5a — Row Found → UPDATE

If a match is found, all fields are updated unconditionally — no checksum comparison, no RowState check:

| Fields Updated |
|---|
| `ClientId` |
| `Context` |
| `CreatedBy` |
| `CreatedOn` |
| `DataFormId` |
| `FormDate` |
| `Isdeleted` |
| `MedicalProviderSignatureBy` |
| `MedicalProviderSignatureDate` |
| `ModifiedBy` |
| `ModifiedOn` |
| `PreAdmissionId` |
| `ServiceId` |
| `Version` |

The following fields are **never changed** on a matched row:

| Field NOT Updated | Why |
|---|---|
| `SiteCode` | Part of the record's identity |
| `Id` | Primary key — never changes |

---

### Step 5b — Row Not Found → INSERT (batched)

If no match is found, the new row is added to the `NewEMs` staging list. All new rows are collected and inserted together at the end.

---

### Step 6 — Write Everything to the Database (Two Commits)

**Commit 1:** Saves all field updates for matched rows.

**Commit 2:** Inserts all new rows via `AddRange` — one bulk insert, not row-by-row.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **No pre-pass, no soft-delete** | This method never sets any row inactive. It only adds or updates |
| **All records loaded every run** | The source SQL has no WHERE clause — every E&M MDM record is fetched regardless of date |
| **`wrkdt` is ignored** | The date window parameter is passed in but never referenced inside the method |
| **`Id` is the sole match key** | No multi-column composite key needed — `Id` uniquely identifies each E&M form |
| **`IsDeleted` is stored as bool** | `"1"` → `true`, anything else → `false`. It is stored for reporting but does not gate any behaviour |
| **`CreatedBy` and `ModifiedBy` always written** | No empty guard — blank string is written if source is empty |
| **`SiteCode` comes from source row** | Not overridden by the `sc` parameter (same as `SaveAnswerSignatures`) |
| **No RowChkSum** | Every matched row is always fully updated — no change detection |
| **Inserts are always batched** | `AddRange` + single `SaveChanges` — never one insert per row |

---

## What Is MDM?

MDM stands for **Medical Decision Making** — it is the section of an E&M (Evaluation & Management) form where the medical provider documents:
- Their clinical assessment of the patient at this visit
- The complexity of the decision made
- Who signed off and when (`MedicalProviderSignatureDate`, `MedicalProviderSignatureBy`)

The E&M form header lives in `dbo.EandMForm` in SAMMS. The MDM section is a separate linked table `dbo.EandMFormMDM`. The source SQL joins them together so this method receives a flat combined row per form.

---

## Source Query Context (from BHGTaskRunner)

The SQL that produces the DataTable for this method is:

```
FROM dbo.EandMForm a
LEFT JOIN dbo.EandMFormMDM c ON (a.ID = c.EandMFormID)
INNER JOIN SF_PatientPreAdmission b ON (a.PreAdmissionID = b.ID)
```

`IsDeleted` in the source is computed as:
- `1` if `a.Isdeleted = 1` OR `b.IsDeleted = 1` (either the form or the admission is deleted)
- `0` otherwise

The WHERE clause is **commented out** — meaning all records are loaded on every run regardless of date. This is intentional because E&M forms are infrequent but clinically critical, and missing a medical provider signature update would be a compliance risk.
