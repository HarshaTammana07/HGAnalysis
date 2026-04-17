# `SaveEMFormMDM` — Step-by-Step Simulation with Dummy Data

This document walks through one complete run of `SaveEMFormMDM` for **clinic B01** on **RunDate = 2024-03-15**.

> Key upfront difference from `SaveFormQuestionAnswers` and `SaveAnswerSignatures`:
> - **No pre-pass** — nothing is reset before the upsert
> - **No wrkdt / date window** — all records loaded every run, no date check
> - **No RowState** — this table does not have a RowState column at all
> - **Single key: `Id`** — one integer uniquely identifies each E&M form record

---

## Setup: What We Start With

### pats.tbl_eandmformmdm — EXISTING Azure Rows for B01

These are the rows already in Azure **before** this run. Loaded into memory as `EMs` list.

| # | SiteCode | Id | PreAdmissionId | ClientId | DataFormId | FormDate | CreatedOn | CreatedBy | ModifiedOn | ModifiedBy | Context | Version | MedicalProviderSignatureDate | MedicalProviderSignatureBy | Isdeleted |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| E1 | B01 | 3001 | 500 | 1001 | 201 | 2024-01-10 | 2024-01-10 | drjones | 2024-02-20 | drjones | Low complexity | v1 | 2024-02-20 | drjones | false |
| E2 | B01 | 3002 | 501 | 1002 | 202 | 2024-02-01 | 2024-02-01 | drnash | NULL | NULL | Moderate complexity | v1 | NULL | NULL | false |
| E3 | B01 | 3003 | 502 | 1003 | 203 | 2023-11-15 | 2023-11-15 | drpatel | 2023-12-01 | drpatel | High complexity | v2 | 2023-12-01 | drpatel | false |
| E4 | B01 | 3004 | 503 | 1004 | 204 | 2024-01-05 | 2024-01-05 | drlee | NULL | NULL | Low complexity | v1 | NULL | NULL | false |

---

## No Pre-Pass Step

Unlike the other form save methods, there is **zero pre-pass here**.
Nothing happens to the existing Azure rows before SAMMS data is processed.
No RowState resets, no date checks, nothing.

The method goes straight from loading `EMs` into memory → column mapping each incoming SAMMS row.

---

## Incoming SAMMS DataTable (tbl)

This is what SAMMS returned for clinic B01 today. 5 rows — one per E&M form.

| # | SiteCode | Id | PreAdmissionId | ClientId | DataFormId | FormDate | CreatedOn | CreatedBy | ModifiedOn | ModifiedBy | Context | Version | MedicalProviderSignatureDate | MedicalProviderSignatureBy | IsDeleted | ServiceId |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| S1 | B01 | 3001 | 500 | 1001 | 201 | 2024-01-10 | 2024-01-10 | drjones | 2024-03-14 | drjones | Low complexity | v2 | **2024-03-14** | drjones | 0 | 88 |
| S2 | B01 | 3002 | 501 | 1002 | 202 | 2024-02-01 | 2024-02-01 | drnash | NULL | NULL | Moderate complexity | v1 | NULL | NULL | 0 | 89 |
| S3 | B01 | 3003 | 502 | 1003 | 203 | 2023-11-15 | 2023-11-15 | drpatel | 2023-12-01 | drpatel | High complexity | v2 | 2023-12-01 | drpatel | **1** | NULL |
| S4 | B01 | **3005** | 505 | 1006 | 206 | 2024-03-10 | 2024-03-10 | drmiller | NULL | NULL | Low complexity | v1 | NULL | NULL | 0 | 91 |
| S5 | B01 | **3006** | 506 | 1007 | NULL | 2024-03-12 | 2024-03-12 | drkim | NULL | NULL | NULL | v1 | NULL | NULL | 0 | NULL |

> **S1** — existing row, but the medical provider signed it since the last run (new `MedicalProviderSignatureDate`)
> **S2** — existing row, no changes
> **S3** — existing row, but now marked `IsDeleted = 1` (form was deleted in source)
> **S4** — brand new form, never seen in Azure
> **S5** — brand new form, `ClientId` empty and `DataFormId` empty

---

## Column Mapping + Lookup for Each Source Row

### S1 — Id = 3001, new signature date added

**Column Mapping:**

| Column | Raw Value | Guard | Transformation | Mapped Result |
|---|---|---|---|---|
| `sitecode` | "B01" | None | Stored as-is | `SiteCode = "B01"` |
| `id` | "3001" | None | int.Parse() | `Id = 3001` |
| `preadmissionid` | "500" | None | int.Parse() | `PreAdmissionId = 500` |
| `clientid` | "1001" | Length > 0 → Yes | int.Parse() | `ClientId = 1001` |
| `dataformid` | "201" | Length > 0 → Yes | int.Parse() | `DataFormId = 201` |
| `createdon` | "2024-01-10" | Length > 6 → Yes (10 chars) | DateTime.Parse() | `CreatedOn = 2024-01-10` |
| `createdby` | "drjones" | None (always stored) | As-is | `CreatedBy = "drjones"` |
| `modifiedon` | "2024-03-14" | Length > 6 → Yes | DateTime.Parse() | `ModifiedOn = 2024-03-14` |
| `modifiedby` | "drjones" | None (always stored) | As-is | `ModifiedBy = "drjones"` |
| `isdeleted` | "0" | — | "0" → false | `Isdeleted = false` |
| `formdate` | "2024-01-10" | Length > 6 → Yes | DateTime.Parse() | `FormDate = 2024-01-10` |
| `serviceid` | "88" | Length > 0 → Yes | int.Parse() | `ServiceId = 88` |
| `context` | "Low complexity" | None (always stored) | As-is | `Context = "Low complexity"` |
| `version` | "v2" | None (always stored) | As-is | `Version = "v2"` |
| `medicalprovidersignaturedate` | "2024-03-14" | Length > 6 → Yes | DateTime.Parse() | `MedicalProviderSignatureDate = 2024-03-14` |
| `medicalprovidersignatureby` | "drjones" | None (always stored) | As-is | `MedicalProviderSignatureBy = "drjones"` |

**Lookup using single key `Id = 3001`:**

**→ Matches E1** ✅

**Update applied to E1:**

| Field | Old Value | New Value | Changed? |
|---|---|---|---|
| `ModifiedOn` | 2024-02-20 | **2024-03-14** | Yes |
| `MedicalProviderSignatureDate` | 2024-02-20 | **2024-03-14** | Yes |
| `Version` | v1 | **v2** | Yes |
| `ServiceId` | null | **88** | Yes |
| `Isdeleted` | false | false | No change |
| `SiteCode` | B01 | B01 | Never updated — identity field |
| `Id` | 3001 | 3001 | Never updated — primary key |

---

### S2 — Id = 3002, no real changes

**Column Mapping highlights:**

| Column | Raw Value | Guard | Mapped Result |
|---|---|---|---|
| `id` | "3002" | None | `Id = 3002` |
| `modifiedon` | "" (empty) | Length > 6 → **No (0 chars)** | `ModifiedOn = null` — **skipped** |
| `modifiedby` | "" (empty) | None — **always stored** | `ModifiedBy = ""` — empty string written |
| `medicalprovidersignaturedate` | "" (empty) | Length > 6 → **No** | Skipped — null |
| `isdeleted` | "0" | — | `Isdeleted = false` |

**Lookup → `Id = 3002` → Matches E2** ✅

**Update applied to E2:**

| Field | Old Value | New Value | Note |
|---|---|---|---|
| `ModifiedOn` | NULL | NULL | Empty string skipped by length guard |
| `ModifiedBy` | NULL | **""** | No guard — empty string is written |
| `ServiceId` | null | **89** | Newly populated |
| All other fields | same | same | No real change in data |

> Notice: `ModifiedBy` changes from null to an empty string — this is because string fields have **no empty guard**. This is a subtle but real side-effect of the "always stored" rule.

---

### S3 — Id = 3003, IsDeleted = 1

**Column Mapping highlights:**

| Column | Raw Value | Guard | Mapped Result |
|---|---|---|---|
| `id` | "3003" | None | `Id = 3003` |
| `isdeleted` | **"1"** | — | `"1"` → `Isdeleted = true` |
| `serviceid` | "NULL" / "" | Length > 0 → **No** | `ServiceId = null` — skipped |
| `medicalprovidersignaturedate` | "2023-12-01" | Length > 6 → Yes | `MedicalProviderSignatureDate = 2023-12-01` |

**Lookup → `Id = 3003` → Matches E3** ✅

**Update applied to E3:**

| Field | Old Value | New Value | Changed? |
|---|---|---|---|
| `Isdeleted` | false | **true** | Yes — form marked as deleted |
| `ServiceId` | null | null | Skipped — source was empty |
| All other fields | same | same | Refreshed but unchanged |

> `Isdeleted = true` is stored but **does not gate any behaviour** in this method. The row stays in Azure — it is not removed. It is just flagged. Downstream reporting can filter on `Isdeleted = true` to exclude it.

---

### S4 — Id = 3005, brand new form

**Column Mapping highlights:**

| Column | Raw Value | Guard | Mapped Result |
|---|---|---|---|
| `id` | "3005" | None | `Id = 3005` |
| `preadmissionid` | "505" | None | `PreAdmissionId = 505` |
| `clientid` | "1006" | Length > 0 → Yes | `ClientId = 1006` |
| `createdon` | "2024-03-10" | Length > 6 → Yes | `CreatedOn = 2024-03-10` |
| `modifiedon` | "" | Length > 6 → **No** | Skipped — null |
| `modifiedby` | "" | None — always stored | `ModifiedBy = ""` |
| `medicalprovidersignaturedate` | "" | Length > 6 → **No** | Skipped — null |
| `medicalprovidersignatureby` | "" | None — always stored | `MedicalProviderSignatureBy = ""` |
| `isdeleted` | "0" | — | `Isdeleted = false` |

**Lookup → `Id = 3005` → No match in EMs** ❌

**→ Added to `NewEMs` insert list.**

---

### S5 — Id = 3006, empty ClientId and DataFormId

This row tests the **skipped-if-empty** behaviour for integer fields.

**Column Mapping highlights:**

| Column | Raw Value | Guard | Transformation | Mapped Result |
|---|---|---|---|---|
| `id` | "3006" | None | int.Parse() | `Id = 3006` |
| `preadmissionid` | "506" | None | int.Parse() | `PreAdmissionId = 506` |
| `clientid` | **""** | Length > 0 → **No (0 chars)** | Skipped | `ClientId = null` |
| `dataformid` | **""** | Length > 0 → **No** | Skipped | `DataFormId = null` |
| `context` | **""** | None — always stored | As-is | `Context = ""` — empty string written |
| `serviceid` | **""** | Length > 0 → **No** | Skipped | `ServiceId = null` |
| `medicalprovidersignatureby` | **""** | None — always stored | As-is | `MedicalProviderSignatureBy = ""` |
| `isdeleted` | "0" | — | "0" → false | `Isdeleted = false` |

**Lookup → `Id = 3006` → No match in EMs** ❌

**→ Added to `NewEMs` insert list** — with `ClientId = null`, `DataFormId = null`, `Context = ""`.

---

## Commit 1 — db.SaveChanges() — Update Commit

Saves all field updates for E1, E2, E3.

| Row | What Was Written |
|---|---|
| E1 (Id=3001) | ModifiedOn, MedicalProviderSignatureDate, Version, ServiceId updated |
| E2 (Id=3002) | ModifiedBy="" written, ServiceId=89 written |
| E3 (Id=3003) | Isdeleted flipped to true |

---

## Commit 2 — AddRange + SaveChanges() — Insert Commit

Two new rows inserted from `NewEMs`:

| SiteCode | Id | PreAdmissionId | ClientId | DataFormId | Context | Isdeleted | MedicalProviderSignatureDate |
|---|---|---|---|---|---|---|---|
| B01 | 3005 | 505 | 1006 | 206 | "Low complexity" | false | NULL |
| B01 | 3006 | 506 | **null** | **null** | **""** | false | NULL |

---

## What Happened to E4 (Id = 3004)?

**Nothing at all.**

E4 was loaded into memory (`EMs` list) at the start but SAMMS did not return a row for Id=3004 today. Because there is **no pre-pass and no RowState**, E4 is completely untouched. It stays exactly as it was before the run.

> This is the biggest behavioural difference vs `SaveFormQuestionAnswers`:
> - In `SaveFormQuestionAnswers`: E4's equivalent would have been reset to `RowState = 0` in the pre-pass (if within date window), and since SAMMS didn't return it, it would stay at 0 — **soft-deleted**.
> - In `SaveEMFormMDM`: E4 has no RowState. It is **never touched**. It just sits there. No soft-delete concept exists here.

---

## Final State — pats.tbl_eandmformmdm for B01 After the Run

| # | SiteCode | Id | ClientId | FormDate | ModifiedOn | Version | MedicalProviderSignatureDate | Isdeleted | What Happened |
|---|---|---|---|---|---|---|---|---|---|
| E1 | B01 | 3001 | 1001 | 2024-01-10 | **2024-03-14** | **v2** | **2024-03-14** | false | Updated — new signature captured |
| E2 | B01 | 3002 | 1002 | 2024-02-01 | NULL | v1 | NULL | false | Updated — ServiceId added, ModifiedBy="" written |
| E3 | B01 | 3003 | 1003 | 2023-11-15 | 2023-12-01 | v2 | 2023-12-01 | **true** | Updated — marked as deleted |
| E4 | B01 | 3004 | 1004 | 2024-01-05 | NULL | v1 | NULL | false | **Completely untouched** — SAMMS didn't return it, no soft-delete |
| NEW | B01 | 3005 | 1006 | 2024-03-10 | NULL | v1 | NULL | false | New row inserted |
| NEW | B01 | 3006 | **null** | 2024-03-12 | NULL | v1 | NULL | false | New row inserted with null ClientId and empty Context |

---

## Outcome Summary

| Outcome | Count | Rows |
|---|---|---|
| **RowsProcessed** | 5 | S1–S5 (all incoming SAMMS rows) |
| **RowsUpd** | 3 | E1, E2, E3 |
| **RowsIns** | 2 | S4 (Id=3005), S5 (Id=3006) |
| **Untouched** | 1 | E4 — not in SAMMS today, no soft-delete mechanism |

---

## Key Lessons from the Simulation

| Lesson | Where it happened |
|---|---|
| **No pre-pass = no soft-delete** | E4 was never in SAMMS today but it still sits in Azure with `Isdeleted = false`. It is invisible to any cleanup |
| **Single key `Id` is very simple but powerful** | S1–S3 matched instantly with just one integer. No FormName, no ClientId, no PreAdmissionId needed in the key |
| **`IsDeleted` is stored but does nothing internally** | E3 got `Isdeleted = true` but it is still in Azure as a queryable row. No row removal, no RowState flip |
| **Always-stored string fields write empty strings** | E2's `ModifiedBy` went from null → `""` because the source was empty and there is no guard. This can create subtle data dirtiness |
| **Empty int fields stay null** | S5's `ClientId` and `DataFormId` were empty strings — the length guard prevented a parse error and left them as null |
| **Length > 6 date guard stops bad DateTime parses** | An empty `ModifiedOn` of `""` (0 chars) is silently skipped. A value like `"1/1/1900"` (8 chars) would be stored |
| **`wrkdt` is completely ignored** | Even though the RunDate was 2024-03-15 and wrkdt was passed in, it was never referenced. All 5 source rows were processed regardless of their dates |
| **`SiteCode` comes from the source row, not the `sc` parameter** | Just like `SaveAnswerSignatures` — the source DataTable value is used directly. If the source has a wrong SiteCode in a row, that wrong value goes into Azure |
