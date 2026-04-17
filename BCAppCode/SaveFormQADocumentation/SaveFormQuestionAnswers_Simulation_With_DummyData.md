# `SaveFormQuestionAnswers` — Step-by-Step Simulation with Dummy Data

This document walks through one complete run of `SaveFormQuestionAnswers` for **clinic B01** on **WorkDate = 2024-03-15**, with `formDaysBack = -25`, making `wrkdt = 2024-02-19`.

---

## Setup: What We Start With

### ctrl.tbl_Forms2Process (Form Config Loaded Before This Method Is Called)

This is the `f2p` list passed into the method.

| FormName | DateFilterEnabled | Enabled |
|---|---|---|
| Treatment Plan | true | true |
| Intake Assessment | true | true |
| Counseling Session Note | false | true |

> Rule reminder:
> - `DateFilterEnabled = true` → only reset rows that fall within the date window
> - `DateFilterEnabled = false` → reset ALL rows for this form type, no date check

---

### pats.tbl_dbo_FormQuestionAnswers — EXISTING Azure Rows for B01

These are the rows already in Azure **before** this run. This is what gets loaded into memory in **Step 2**.

| # | SiteCode | FormName | FormId | ClientId | PreAdmissionId | QuestionId | QuestionOrderId | AnswerValue | CreatedOn | UpdatedOn | RowState |
|---|---|---|---|---|---|---|---|---|---|---|---|
| R1 | B01 | Treatment Plan | FORM-TP-001 | 1001 | 500 | 10 | 1 | Initial | 2024-01-05 | 2024-02-25 | 1 |
| R2 | B01 | Treatment Plan | FORM-TP-001 | 1001 | 500 | 11 | 2 | Weekly | 2024-01-05 | 2024-02-25 | 1 |
| R3 | B01 | Intake Assessment | FORM-IA-002 | 1002 | 501 | 20 | 1 | Stable | 2024-01-10 | 2024-01-15 | 1 |
| R4 | B01 | Intake Assessment | FORM-IA-003 | 1003 | 502 | 20 | 1 | Improving | 2024-02-01 | NULL | 1 |
| R5 | B01 | Counseling Session Note | FORM-CS-004 | 1004 | 503 | 30 | 1 | Good progress | 2023-11-01 | 2023-12-01 | 1 |
| R6 | B01 | Counseling Session Note | FORM-CS-005 | 1005 | 504 | 30 | 1 | Attended | 2024-02-20 | NULL | 1 |
| R7 | B01 | TP-Quarterly | FORM-TP-006 | 1006 | 505 | 10 | 1 | Quarterly | 2024-02-22 | NULL | 1 |

**wrkdt = 2024-02-19** — this is the date boundary.

---

## Phase 3 — Pre-Pass: Soft-Reset Decision for Each Existing Row

Walk through each Azure row and decide: should `RowState` be reset to `0`?

---

### R1 — FormName: `"Treatment Plan"` → Found in f2p, `DateFilterEnabled = true`

| Check | Value | Result |
|---|---|---|
| FormName starts with "TP-"? | No — it's exactly "Treatment Plan" | No normalization needed |
| Found in f2p? | Yes | Use DateFilterEnabled |
| DateFilterEnabled? | true | Apply date check |
| UpdatedOn has value? | Yes — 2024-02-25 | Check both dates |
| CreatedOn (2024-01-05) >= wrkdt (2024-02-19)? | No | — |
| UpdatedOn (2024-02-25) >= wrkdt (2024-02-19)? | **Yes** | — |
| RowState == 1? | Yes | — |
| **Decision** | | **RowState → 0** |

---

### R2 — FormName: `"Treatment Plan"` → Found in f2p, `DateFilterEnabled = true`

Same form as R1, different question.

| Check | Value | Result |
|---|---|---|
| UpdatedOn (2024-02-25) >= wrkdt? | **Yes** | — |
| RowState == 1? | Yes | — |
| **Decision** | | **RowState → 0** |

---

### R3 — FormName: `"Intake Assessment"` → Found in f2p, `DateFilterEnabled = true`

| Check | Value | Result |
|---|---|---|
| UpdatedOn has value? | Yes — 2024-01-15 | Check both dates |
| CreatedOn (2024-01-10) >= wrkdt (2024-02-19)? | No | — |
| UpdatedOn (2024-01-15) >= wrkdt (2024-02-19)? | **No** | — |
| **Decision** | | **RowState unchanged — stays 1** |

> This row is outside the date window. It will NOT be touched in the pre-pass.

---

### R4 — FormName: `"Intake Assessment"` → Found in f2p, `DateFilterEnabled = true`

| Check | Value | Result |
|---|---|---|
| UpdatedOn has value? | No — NULL | Use CreatedOn only |
| CreatedOn (2024-02-01) >= wrkdt (2024-02-19)? | **No** | — |
| **Decision** | | **RowState unchanged — stays 1** |

> CreatedOn is before wrkdt. Outside the window.

---

### R5 — FormName: `"Counseling Session Note"` → Found in f2p, `DateFilterEnabled = false`

| Check | Value | Result |
|---|---|---|
| Found in f2p? | Yes | — |
| DateFilterEnabled? | **false** | No date check needed |
| **Decision** | | **RowState → 0 unconditionally** |

> Date doesn't matter. ALL Counseling Session Note rows are reset every run.

---

### R6 — FormName: `"Counseling Session Note"` → Found in f2p, `DateFilterEnabled = false`

| Check | Value | Result |
|---|---|---|
| DateFilterEnabled? | **false** | No date check needed |
| **Decision** | | **RowState → 0 unconditionally** |

---

### R7 — FormName: `"TP-Quarterly"` → Starts with "TP-" → Normalized to `"Treatment Plan"`

| Check | Value | Result |
|---|---|---|
| FormName starts with "TP-"? | **Yes** | Normalize to "Treatment Plan" |
| Found in f2p as "Treatment Plan"? | Yes | DateFilterEnabled = true |
| UpdatedOn has value? | No — NULL | Use CreatedOn only |
| CreatedOn (2024-02-22) >= wrkdt (2024-02-19)? | **Yes** | — |
| RowState == 1? | Yes | — |
| **Decision** | | **RowState → 0** |

---

### Pre-Pass Result Summary

| Row | FormName | Before RowState | After RowState | Reason |
|---|---|---|---|---|
| R1 | Treatment Plan | 1 | **0** | UpdatedOn (Feb 25) >= wrkdt (Feb 19) |
| R2 | Treatment Plan | 1 | **0** | UpdatedOn (Feb 25) >= wrkdt (Feb 19) |
| R3 | Intake Assessment | 1 | 1 | Both dates before wrkdt — outside window |
| R4 | Intake Assessment | 1 | 1 | CreatedOn before wrkdt — outside window |
| R5 | Counseling Session Note | 1 | **0** | DateFilterEnabled=false — always reset |
| R6 | Counseling Session Note | 1 | **0** | DateFilterEnabled=false — always reset |
| R7 | TP-Quarterly | 1 | **0** | Normalized to "Treatment Plan", CreatedOn >= wrkdt |

> **Note:** `db.SaveChanges()` is NOT called yet. These are in-memory changes only — they will be flushed together with the upsert writes later.

---

## Phase 4 — Incoming SAMMS DataTable (tbl)

This is what SAMMS returned for clinic B01 today. 5 rows.

| # | SiteCode | FormName | FormId | ClientId | PreAdmissionId | QuestionId | QuestionOrderId | QuestionText | OptionId | AnswerValue | CreatedOn | CreatedBy | UpdatedOn | UpdatedBy | IsDeleted | IsChildForm |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| S1 | B01 | Treatment Plan | form-tp-001 | 1001 | 500 | 10 | 1 | What is your plan type? | NULL | **Intensive** | 2024-01-05 | jsmith | 2024-02-25 | jsmith | 0 | 0 |
| S2 | B01 | Treatment Plan | form-tp-001 | 1001 | 500 | 11 | 2 | How often do you attend? | OPT-W | **Bi-Weekly** | 2024-01-05 | jsmith | 2024-02-25 | jsmith | 0 | 0 |
| S3 | B01 | Counseling Session Note | form-cs-005 | 1005 | 504 | 30 | 1 | Session notes | NULL | **Progressing well** | 2024-02-20 | kwilliams | NULL | NULL | 0 | 0 |
| S4 | B01 | New Patient Intake | FORM-NP-007 | 1007 | 506 | 40 | 1 | Primary concern | NULL | Anxiety | 2024-03-10 | tbrown | NULL | NULL | 0 | 0 |
| S5 | B01 | Counseling Session Note | FORM-CS-008 | **-1008** | 507 | 30 | 1 | Session notes | NULL | Cancelled | 2024-03-01 | mlee | NULL | NULL | **1** | 0 |

---

## Phase 4 & 5 — Column Mapping + Lookup for Each Source Row

### S1 — Treatment Plan, FORM-TP-001, ClientId=1001, Q10

**Column Mapping:**

| Column | Raw Value | Transformation | Mapped Result |
|---|---|---|---|
| `sitecode` | "B01" | Use parameter `sc` | `SiteCode = "B01"`, `LastModAt = now` |
| `formname` | "Treatment Plan" | As-is | `FormName = "Treatment Plan"` |
| `formid` | "form-tp-001" | `.ToUpper()` | `FormId = "FORM-TP-001"` |
| `clientid` | "1001" | int.Parse → 1001 >= 0 | `ClientId = 1001`, `RowState = 1` |
| `createdon` | "2024-01-05" | DateTime.Parse | `CreatedOn = 2024-01-05` |
| `createdby` | "jsmith" | As-is | `CreatedBy = "jsmith"` |
| `updatedon` | "2024-02-25" | DateTime.Parse | `UpdatedOn = 2024-02-25` |
| `updatedby` | "jsmith" | As-is | `UpdatedBy = "jsmith"` |
| `preadmissionid` | "500" | int.Parse | `PreAdmissionId = 500` |
| `isdeleted` | "0" | → RowState = 1 | `RowState = 1` (confirms active) |
| `ischildform` | "0" | → false | `IsChildForm = false` |
| `questionid` | "10" | int.Parse | `QuestionId = 10` |
| `questionorderid` | "1" | int.Parse | `QuestionOrderId = 1` |
| `questiontext` | "What is your plan type?" | As-is | `QuestionText = "What is your plan type?"` |
| `optionid` | "NULL" / empty | Length = 0, skip | `OptionId = null` |
| `answervalue` | "Intensive" | As-is | `AnswerValue = "Intensive"` |

**Lookup in Azure memory using composite key:**

```
SiteCode="B01" + FormName="Treatment Plan" + FormId="FORM-TP-001" + ClientId=1001
+ PreAdmissionId=500 + QuestionId=10 + QuestionOrderId=1
```

**→ Matches R1** ✅

**Update applied to R1:**

| Field | Old Value | New Value |
|---|---|---|
| `AnswerValue` | "Initial" | **"Intensive"** |
| `UpdatedOn` | 2024-02-25 | 2024-02-25 (same) |
| `UpdatedBy` | (null) | "jsmith" |
| `RowState` | 0 (was reset in pre-pass) | **1** (re-activated) |
| `LastModAt` | old | now |

---

### S2 — Treatment Plan, FORM-TP-001, ClientId=1001, Q11

Similar mapping to S1. Key columns:

| Column | Raw Value | Mapped Result |
|---|---|---|
| `formid` | "form-tp-001" | `FormId = "FORM-TP-001"` |
| `answervalue` | "Bi-Weekly" | `AnswerValue = "Bi-Weekly"` |
| `optionid` | "OPT-W" | `OptionId = "OPT-W"` |
| `isdeleted` | "0" | `RowState = 1` |

**Lookup → Matches R2** ✅

**Update applied to R2:**

| Field | Old Value | New Value |
|---|---|---|
| `AnswerValue` | "Weekly" | **"Bi-Weekly"** |
| `OptionId` | null | **"OPT-W"** |
| `RowState` | 0 (pre-pass reset) | **1** (re-activated) |

---

### S3 — Counseling Session Note, form-cs-005, ClientId=1005, Q30

**Column Mapping highlights:**

| Column | Raw Value | Mapped Result |
|---|---|---|
| `formid` | "form-cs-005" | `FormId = "FORM-CS-005"` |
| `clientid` | "1005" | `ClientId = 1005`, `RowState = 1` |
| `updatedon` | "" (empty) | Skipped — `UpdatedOn = null` |
| `answervalue` | "Progressing well" | `AnswerValue = "Progressing well"` |
| `isdeleted` | "0" | `RowState = 1` |

**Lookup:**
```
SiteCode="B01" + FormName="Counseling Session Note" + FormId="FORM-CS-005"
+ ClientId=1005 + PreAdmissionId=504 + QuestionId=30 + QuestionOrderId=1
```

**→ Matches R6** ✅

**Update applied to R6:**

| Field | Old Value | New Value |
|---|---|---|
| `AnswerValue` | "Attended" | **"Progressing well"** |
| `RowState` | 0 (pre-pass reset) | **1** (re-activated) |

---

### S4 — New Patient Intake, FORM-NP-007, ClientId=1007, Q40

This form name (`"New Patient Intake"`) does **not exist** in f2p and has **never been seen before** in Azure for B01.

**Column Mapping highlights:**

| Column | Raw Value | Mapped Result |
|---|---|---|
| `formid` | "FORM-NP-007" | `FormId = "FORM-NP-007"` |
| `clientid` | "1007" | `ClientId = 1007`, `RowState = 1` |
| `preadmissionid` | "506" | `PreAdmissionId = 506` |
| `createdon` | "2024-03-10" | `CreatedOn = 2024-03-10` |
| `updatedon` | "" | Skipped — `UpdatedOn = null` |
| `isdeleted` | "0" | `RowState = 1` |
| `questiontext` | "Primary concern" | `QuestionText = "Primary concern"` |
| `answervalue` | "Anxiety" | `AnswerValue = "Anxiety"` |

**Lookup → No match found** ❌

**→ Added to `newfqas` insert list** as a brand new row.

---

### S5 — Counseling Session Note, FORM-CS-008, ClientId=-1008, Q30

**This row has a NEGATIVE ClientId and IsDeleted = "1" — a deleted record.**

**Column Mapping highlights:**

| Column | Raw Value | Transformation | Mapped Result |
|---|---|---|---|
| `clientid` | "-1008" | int.Parse → -1008 < 0 | `ClientId = -1008`, `RowState = 0` ← first assignment |
| `preadmissionid` | "507" | int.Parse | `PreAdmissionId = 507` |
| `isdeleted` | "1" | → IsDeleted=true, RowState=0 | `IsDeleted = true`, `RowState = 0` ← final (same result) |
| `answervalue` | "Cancelled" | As-is | `AnswerValue = "Cancelled"` |

**Lookup:**
```
SiteCode="B01" + FormName="Counseling Session Note" + FormId="FORM-CS-008"
+ ClientId=-1008 + PreAdmissionId=507 + QuestionId=30 + QuestionOrderId=1
```

**→ No match found** ❌ (this form instance has never been in Azure before)

**→ Added to `newfqas` insert list** — but with `RowState = 0` (inactive from the start since it's deleted in source).

---

## Phase 6 — db.SaveChanges() — First Commit

Writes everything accumulated so far:

| What is written | Detail |
|---|---|
| Pre-pass RowState resets (R1, R2, R5, R6, R7) | **But** R1, R2, R6 were then re-activated by upsert — their final state is RowState=1 |
| Updates to R1 | AnswerValue, RowState=1, LastModAt, etc. |
| Updates to R2 | AnswerValue, OptionId, RowState=1, LastModAt |
| Updates to R6 | AnswerValue, RowState=1, LastModAt |

---

## Phase 7 — AddRange + Second Commit — Inserts

Two new rows are inserted from `newfqas`:

| Row | SiteCode | FormName | FormId | ClientId | QuestionId | AnswerValue | RowState |
|---|---|---|---|---|---|---|---|
| S4 | B01 | New Patient Intake | FORM-NP-007 | 1007 | 40 | Anxiety | **1** |
| S5 | B01 | Counseling Session Note | FORM-CS-008 | -1008 | 30 | Cancelled | **0** |

---

## Final State — pats.tbl_dbo_FormQuestionAnswers for B01 After the Run

| # | SiteCode | FormName | FormId | ClientId | QuestionId | AnswerValue | RowState | What Happened |
|---|---|---|---|---|---|---|---|---|
| R1 | B01 | Treatment Plan | FORM-TP-001 | 1001 | 10 | **Intensive** | **1** | Pre-reset → re-activated, answer updated |
| R2 | B01 | Treatment Plan | FORM-TP-001 | 1001 | 11 | **Bi-Weekly** | **1** | Pre-reset → re-activated, answer + option updated |
| R3 | B01 | Intake Assessment | FORM-IA-002 | 1002 | 20 | Stable | 1 | Outside date window — **completely untouched** |
| R4 | B01 | Intake Assessment | FORM-IA-003 | 1003 | 20 | Improving | 1 | Outside date window — **completely untouched** |
| R5 | B01 | Counseling Session Note | FORM-CS-004 | 1004 | 30 | Good progress | **0** | Pre-reset (DateFilterEnabled=false) — **NOT seen in SAMMS today → stays 0 = soft-deleted** |
| R6 | B01 | Counseling Session Note | FORM-CS-005 | 1005 | 30 | **Progressing well** | **1** | Pre-reset → re-activated, answer updated |
| R7 | B01 | TP-Quarterly | FORM-TP-006 | 1006 | 10 | Quarterly | **0** | Pre-reset (TP- normalized) — **NOT seen in SAMMS today → stays 0 = soft-deleted** |
| NEW | B01 | New Patient Intake | FORM-NP-007 | 1007 | 40 | Anxiety | **1** | Brand new row inserted |
| NEW | B01 | Counseling Session Note | FORM-CS-008 | -1008 | 30 | Cancelled | **0** | New insert but starts inactive — deleted in source |

---

## Outcome Summary

| Outcome | Count | Rows |
|---|---|---|
| **RowsProcessed** | 5 | S1, S2, S3, S4, S5 (rows in the incoming DataTable) |
| **RowsUpd** | 3 | R1, R2, R6 |
| **RowsIns** | 2 | S4 (new form), S5 (deleted form, new to Azure) |
| **Soft-deleted (stayed 0)** | 2 | R5, R7 — pre-reset but never matched by SAMMS data today |
| **Untouched (outside window)** | 2 | R3, R4 — never entered pre-pass, never matched |

---

## Key Lessons from the Simulation

| Lesson | Where it happened |
|---|---|
| **TP- normalization matters** | R7 was caught by the pre-pass only because "TP-Quarterly" was normalized to "Treatment Plan" — otherwise it would have been treated as unregistered |
| **DateFilterEnabled=false resets ALL rows** | R5 was from 2023 — no date check, still got reset. Then SAMMS didn't return it, so it soft-deleted |
| **Outside the date window = totally safe** | R3 and R4 were never touched at all — not in pre-pass, not in upsert. Their data and RowState are frozen |
| **RowState goes 1→0→1 for matched rows** | R1, R2, R6 were reset to 0 in pre-pass, then the SAMMS row re-activated them to 1. Net effect = updated and still active |
| **FormId is upper-cased in mapping** | S1 arrived as "form-tp-001" (lowercase) but was stored and matched as "FORM-TP-001" (upper) |
| **isdeleted overwrites clientid for RowState** | S5 had ClientId=-1008 (sets RowState=0) AND IsDeleted="1" (also RowState=0) — they agreed, but isdeleted always has the final word |
| **Deleted records still get inserted** | S5 was new to Azure but already deleted in source — it is inserted with RowState=0 right away |
| **BAMMerge runs after this** | After all of this, `pats.BAMMerge @sitecode='B01'` recalculates behavioral metrics from the updated form Q&A data |
