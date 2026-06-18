# FormAnswerSignatures — Microsoft Fabric ETL Pipeline Implementation Guide

**Pipeline Name:** FormAnswerSignatures Bronze → Silver ETL  
**Data:** Clinical form signature date records — one row per form instance, tracking when each role signed  
**Destination:** Microsoft Fabric Lakehouse (Bronze + Silver layers)  
**Silver Target:** `bhg_silver.pats.sl_tblFormAnswerSignatures`  
**Control Dependency:** `bhg_silver.ctrl.Forms2Process` (Fabric lakehouse table — read only from Spark, never from SAMMS)  
**Author Reference:** `FormAnswerSignatures_Tables_Columns_Logic.md`, `SaveAnswerSignatures_Transformation_Logic.md`, `BHGTaskRunner/Program.cs`, `SaveFormQAData.cs`

---

## 0. Orchestration Pattern — Multi-Pipeline Execution at Scale

### Architecture: 5 Parallel ForEach × 23 Sites + Parent Orchestrator

The AnswerSignatures pipeline follows the same orchestration pattern as FormQuestionAnswers:

```
Parent Pipeline (Execute_Form_AnswerSignature)
  │
  └── Executed_After_BronzCopy  (InvokePipeline — waitOnCompletion: true)
        │
        └── Child Pipeline (pl_answersig_samms_to_lakehouse)
              ├── ForEach 1 → 23 sites  (p_sites)
              ├── ForEach 2 → 23 sites  (p_sites2)
              ├── ForEach 3 → 23 sites  (p_sites3)
              ├── ForEach 4 → 23 sites  (p_sites4)
              └── ForEach 5 → 23 sites  (p_sites5)
              [all 5 ForEach run in parallel — 115 sites total]

  After InvokePipeline succeeds:
  └── nb_answersig_bronze_to_silver (Silver Notebook)
```

> **Why `waitOnCompletion: true` is critical:** All 115 sites must finish writing to Bronze before the Silver notebook runs. Without it, the Silver notebook starts mid-extraction and misses rows from still-running sites.

---

### Step-by-Step: Building the Parent Orchestrator Pipeline

#### 0.1 Create the Parent Pipeline

1. In Fabric, create a new data pipeline: **`Execute_Form_AnswerSignature`**
2. This pipeline has only **two activities** — one Invoke Pipeline and one Silver Notebook

#### 0.2 Add Parameters to the Parent Pipeline

In **Edit → Parameters** tab, add:

| Parameter | Type | Default |
|---|---|---|
| `p_ingest_run_id` | String | `@pipeline().RunId` |
| `p_lookback_days` | Int | `30` |
| `p_reload` | Bool | `false` |
| `p_sites` | Array | `[{"site_code": "...", "source_database": "..."}, ...]` (23 sites) |
| `p_sites2` | Array | (next 23 sites) |
| `p_sites3` | Array | (next 23 sites) |
| `p_sites4` | Array | (next 23 sites) |
| `p_sites5` | Array | (last 23 sites) |

#### 0.3 Add ONE Invoke Pipeline Activity

1. **Drag an Invoke Pipeline activity** onto the canvas
2. **Rename it:** `Executed_After_BronzCopy`
3. **No dependency** — starts immediately when parent runs
4. **Configure Settings tab:**

| Setting | Value |
|---|---|
| Pipeline | `pl_answersig_samms_to_lakehouse` (child pipeline) |
| Wait on completion | ✓ (must be checked) |

5. **Configure Parameters tab:**

| Parameter | Value |
|---|---|
| `p_ingest_run_id` | `@pipeline().parameters.p_ingest_run_id` (Expression) |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (Expression) |
| `p_reload` | `@pipeline().parameters.p_reload` (Expression) |
| `p_sites` | `@pipeline().parameters.p_sites` (Expression) |
| `p_sites2` | `@pipeline().parameters.p_sites2` (Expression) |
| `p_sites3` | `@pipeline().parameters.p_sites3` (Expression) |
| `p_sites4` | `@pipeline().parameters.p_sites4` (Expression) |
| `p_sites5` | `@pipeline().parameters.p_sites5` (Expression) |

**Full JSON:**
```json
{
    "name": "Executed_After_BronzCopy",
    "type": "InvokePipeline",
    "dependsOn": [],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "waitOnCompletion": true,
        "operationType": "InvokeFabricPipeline",
        "pipelineId": "cf9e89d0-9574-439e-85b9-c022327ed022",
        "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
        "parameters": {
            "p_ingest_run_id": { "value": "@pipeline().parameters.p_ingest_run_id", "type": "Expression" },
            "p_lookback_days": { "value": "@pipeline().parameters.p_lookback_days", "type": "Expression" },
            "p_reload": { "value": "@pipeline().parameters.p_reload", "type": "Expression" },
            "p_sites":  { "value": "@pipeline().parameters.p_sites",  "type": "Expression" },
            "p_sites2": { "value": "@pipeline().parameters.p_sites2", "type": "Expression" },
            "p_sites3": { "value": "@pipeline().parameters.p_sites3", "type": "Expression" },
            "p_sites4": { "value": "@pipeline().parameters.p_sites4", "type": "Expression" },
            "p_sites5": { "value": "@pipeline().parameters.p_sites5", "type": "Expression" }
        }
    },
    "externalReferences": { "connection": "9efac0af-aea0-4007-90e5-0fa555fb1fa2" }
}
```

#### 0.4 Add the Silver Notebook Activity

1. **Drag a Notebook activity** onto the canvas
2. **Rename it:** `nb_answersig_bronze_to_silver`
3. **Dependency:** `Executed_After_BronzCopy` → `nb_answersig_bronze_to_silver` (Succeeded)
4. **Configure Parameters:**

| Parameter | Value |
|---|---|
| `p_ingest_run_id` | `@pipeline().parameters.p_ingest_run_id` (Expression) |
| `p_lookback_days` | `@pipeline().parameters.p_lookback_days` (Expression) |
| `p_reload` | `@pipeline().parameters.p_reload` (Expression) |

This notebook reads Bronze filtered by `_ingest_run_id`, processes all 115 sites' rows together, and merges into Silver in a single pass.

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [How This Pipeline Differs From FormQuestionAnswers](#3-how-this-pipeline-differs-from-formquestionanswers)
4. [Prerequisites — What You Need Before Starting](#4-prerequisites--what-you-need-before-starting)
5. [Pipeline Parameters and Variables — Define These First](#5-pipeline-parameters-and-variables--define-these-first)
6. [Step 1 — Create the Data Pipeline in Fabric](#6-step-1--create-the-data-pipeline-in-fabric)
7. [Step 2 — Add the ForEach Activity](#7-step-2--add-the-foreach-activity)
8. [Step 3 — Inside ForEach: Lookup 1 — Check AnswerSignature Table Exists](#8-step-3--inside-foreach-lookup-1--check-answersignature-table-exists)
9. [Step 4 — Inside ForEach: IfCondition — Gate on AnswerSignature Table](#9-step-4--inside-foreach-ifcondition--gate-on-answersignature-table)
10. [Step 5 — Inside True Branch: Lookup 2 — Get All Existing Tables](#10-step-5--inside-true-branch-lookup-2--get-all-existing-tables)
11. [Step 6 — Inside True Branch: Notebook Activity — SQL Builder](#11-step-6--inside-true-branch-notebook-activity--sql-builder)
12. [Step 7 — Inside True Branch: Copy Activity — Extract to Bronze](#12-step-7--inside-true-branch-copy-activity--extract-to-bronze)
13. [Step 8 — Inside True Branch: Copy Activity — Extract to Bronze](#13-step-8--inside-true-branch-copy-activity--extract-to-bronze)
14. [Step 9 — Add the Silver Notebook Activity (After ForEach)](#14-step-9--add-the-silver-notebook-activity-after-foreach)
15. [Step 10 — Create the SQL Builder Notebook: nb_answersig_build_site_sql](#15-step-10--create-the-sql-builder-notebook-nb_answersig_build_site_sql)
16. [Step 11 — Create the Silver Notebook: nb_answersig_bronze_to_silver](#16-step-11--create-the-silver-notebook-nb_answersig_bronze_to_silver)
17. [Step 12 — Silver Notebook Cell 1: Load Bronze and Prepare Source](#17-step-12--silver-notebook-cell-1-load-bronze-and-prepare-source)
18. [Step 13 — Silver Notebook Cell 2: Pre-Pass RowState Reset](#18-step-13--silver-notebook-cell-2-pre-pass-rowstate-reset)
19. [Step 14 — Silver Notebook Cell 3: Delta MERGE Into Silver](#19-step-14--silver-notebook-cell-3-delta-merge-into-silver)
20. [End-to-End Flow Summary](#20-end-to-end-flow-summary)
21. [Why the Base Query Has No Date Filter](#21-why-the-base-query-has-no-date-filter)
22. [The 9 Signature Columns — AnswerSignature Correlated Subquery Pattern](#22-the-9-signature-columns--answersignature-correlated-subquery-pattern)
23. [The Forms2Process Loop — 3 Switch Cases](#23-the-forms2process-loop--3-switch-cases)
24. [Default Case — Level A: FormID and ClientId Per Table](#24-default-case--level-a-formid-and-clientid-per-table)
25. [Default Case — Level B: Signature Date Columns Per Table](#25-default-case--level-b-signature-date-columns-per-table)
26. [AdmissionAssessment — The aas Join Exception](#26-admissionassessment--the-aas-join-exception)
27. [NewAdmissionAssessment — The b Join Exception](#27-newadmissionassessment--the-b-join-exception)
28. [The 4-Column Primary Key and RowChkSum Behaviour](#28-the-4-column-primary-key-and-rowchksum-behaviour)
29. [ClientId — Always Stored as Absolute Value](#29-clientid--always-stored-as-absolute-value)
30. [Pre-Pass Differences — Committed Immediately](#30-pre-pass-differences--committed-immediately)
31. [Troubleshooting Guide](#31-troubleshooting-guide)

---

## 1. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                   Microsoft Fabric Data Pipeline                          │
│                   pl_answersig_samms_to_lakehouse                         │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  ForEach Site (fe_each_samms_site)                                 │  │
│  │  Iterates over every clinic in p_sites parameter                   │  │
│  │                                                                    │  │
│  │  ┌───────────────────────────────────────────────┐                │  │
│  │  │ lkp_check_answersig_table (Lookup)            │                │  │
│  │  │ → SELECT name FROM sys.tables                 │                │  │
│  │  │   WHERE name = 'answersignature'              │                │  │
│  │  └─────────────────┬─────────────────────────────┘                │  │
│  │                    │ Succeeded                                     │  │
│  │                    ▼                                               │  │
│  │  ┌───────────────────────────────────────────────┐                │  │
│  │  │ if_answersig_table_exists (IfCondition)        │                │  │
│  │  │ condition: firstRow.answersig_exists = 1       │                │  │
│  │  │                                                │                │  │
│  │  │  TRUE BRANCH ────────────────────────────────┐│                │  │
│  │  │  ┌──────────────────────────────────────┐    ││                │  │
│  │  │  │ lkp_get_existing_tables (Lookup)     │    ││                │  │
│  │  │  │ → SELECT name FROM sys.tables        │    ││                │  │
│  │  │  └────────────────┬─────────────────────┘    ││                │  │
│  │  │                   │ Succeeded                  ││                │  │
│  │  │                   ▼                            ││                │  │
│  │  │  ┌──────────────────────────────────────┐     ││                │  │
│  │  │  │ nb_answersig_build_site_sql (Notebook)│     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ READS: bhg_silver.ctrl               │     ││                │  │
│  │  │  │        .Forms2Process            │     ││                │  │
│  │  │  │        (Fabric lakehouse — NO SAMMS) │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ BUILDS: Full UNION SQL with          │     ││                │  │
│  │  │  │  base Form/AnswerSignature query +   │     ││                │  │
│  │  │  │  Forms2Process UNIONs (3 cases)      │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ OUTPUT: SQL via notebook.exit()      │     ││                │  │
│  │  │  └────────────────┬─────────────────────┘     ││                │  │
│  │  │                   │ exitValue                   ││                │  │
│  │  │                   ▼                            ││                │  │
│  │  │  ┌──────────────────────────────────────┐     ││                │  │
│  │  │  │ cp_answersig_to_bronze (Copy)         │     ││                │  │
│  │  │  │ query = activity exit value           │     ││                │  │
│  │  │  │ → SAMMS SQL Server (Fabric gateway)  │     ││                │  │
│  │  │  │ → APPEND → bhg_bronze                │     ││                │  │
│  │  │  │         Forms.br_tblFormAnswerSig    │     ││                │  │
│  │  │  └──────────────────────────────────────┘     ││                │  │
│  │  └──────────────────────────────────────────────┘│                │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                         │ ForEach Succeeded                              │
│                         ▼                                                │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  nb_answersig_bronze_to_silver (Notebook Activity)                 │  │
│  │                                                                    │  │
│  │  Cell 1: Read Bronze → dedup on 4-col PK → ABS(ClientId)         │  │
│  │          Compute RowState                                          │  │
│  │  Cell 2: Pre-pass RowState reset (committed before MERGE)         │  │
│  │  Cell 3: Delta MERGE → bhg_silver.pats.sl_tblFormAnswerSignatures │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

**Lakehouse Targets:**

| Layer | Lakehouse | Schema | Table |
|---|---|---|---|
| Bronze | `bhg_bronze` | `Forms` | `br_tblFormAnswerSig` |
| Silver | `bhg_silver` | `pats` | `sl_tblFormAnswerSignatures` |
| Control | `bhg_silver` | `ctrl` | `Forms2Process` |

---

## 2. What This Pipeline Does — Plain English

Every clinical form in SAMMS can be signed by multiple roles — the patient, counselor, doctor, medical provider, supervisor, and others. The `AnswerSignature` table tracks those signatures per form instance. This pipeline:

1. **Checks** whether the SAMMS database has an `AnswerSignature` table — if not, the site is skipped
2. **Gets all SAMMS table names** via a Lookup against `sys.tables`
3. **Builds the full UNION SQL** in a Spark notebook that reads `bhg_silver.ctrl.Forms2Process` (no SAMMS connection from the notebook)
4. **Executes that SQL** via a single Copy activity against SAMMS — one query, one Bronze append
5. Runs the **Silver notebook** that reads Bronze, resets stale RowState values, and Delta-MERGEs into Silver using a **4-column composite key** (one row per form — simpler than FormQuestionAnswers)

The key design principle: **one row per form instance in Silver**, carrying all 9 signature date columns. This is the complement to FormQuestionAnswers (which has one row per question/answer).

---

## 3. How This Pipeline Differs From FormQuestionAnswers

Understanding these differences is critical — both pipelines share the same architecture but the SQL logic and Silver MERGE rules are different in important ways.

| Aspect | FormQuestionAnswers | FormAnswerSignatures |
|---|---|---|
| **Entry gate table** | `sys.tables WHERE name='Form'` | `sys.tables WHERE name='answersignature'` |
| **Full reload trigger** | `p_reload = true` parameter | `p_reload = true` parameter (replaces the hard-coded `2/2/2024` date in old system) |
| **Base query date filter** | Active — `WHERE CreatedOn >= wrkdt` | **Commented out** — pulls ALL forms regardless of date |
| **Base UNION 2** | Forms with `q.Id IS NULL` (no questions) | Forms where `f.Isdeleted = 1` (deleted forms) |
| **Signature columns** | None — not applicable | 9 columns, each a correlated subquery against `AnswerSignature` or direct table column |
| **`SELECT DISTINCT` wrapper** | `SELECT DISTINCT * FROM (...) z` | **No wrapper** — `strCmd` is executed directly |
| **Forms2Process ORDER BY** | `ORDER BY Prefix` | **No ORDER BY** |
| **Top-level switch cases** | 9 cases + default | `tblORDERREQ`, `tblTP17REVIEW`, default |
| **Default case complexity** | Single level (FormID/ClientId formula) | **Two nested levels** — Level A: FormID/ClientId; Level B: each of 9 sig date columns |
| **sig-date column names** | Not applicable | Read from `Forms2Process` columns: `CompletedBy`, `Counselor`, `Doctor`, `MedicalProvider`, `Patient`, `Provider`, `Requestor`, `Staff`, `Supervisor` |
| **Silver PK columns** | 7: SiteCode, FormName, FormId, ClientId, PreAdmissionId, QuestionId, QuestionOrderId | **4**: SiteCode, FormName, FormId, ClientId |
| **RowChkSum** | Not present | Column exists on entity, but **source query never generates it** — always null; update guard was also commented out |
| **ClientId storage** | Raw value; if `< 0` → RowState=0 | **`Math.Abs()` — always positive**; if original `< 0` → RowState=0 |
| **Pre-pass `SaveChanges`** | Deferred (committed with updates) | **Committed immediately** before upsert begins |
| **BAMMerge stored procedure** | Called after EF path | **Not called** |
| **Bulk path** | Yes (18-site allowlist) | **No bulk path** — always EF path |

---

## 4. Prerequisites — What You Need Before Starting

Same as FormQuestionAnswers pipeline:

| Item | What It Is | Where It Lives |
|---|---|---|
| SAMMS SQL Server Fabric connection | On-prem gateway connection for Copy + Lookup activities | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse | Bronze layer | Fabric workspace |
| `bhg_silver` Lakehouse | Silver layer | Fabric workspace |
| `Forms` schema in `bhg_bronze` | Schema for both FormQA and AnswerSig Bronze tables | `bhg_bronze` Lakehouse |
| `pats` schema in `bhg_silver` | Schema for Silver patient tables | `bhg_silver` Lakehouse |
| `ctrl` schema in `bhg_silver` | Control/reference table schema | `bhg_silver` Lakehouse |
| `bhg_silver.ctrl.Forms2Process` | Pre-loaded Fabric version of BHG_DR's `ctrl.Forms2Process` | Must include all sig-date columns |
| On-premise data gateway | Routes Copy/Lookup activities to SAMMS | Fabric settings |

**Key IDs:**

```
Workspace ID:            c5097ffb-b78e-441d-9575-a82bac23cac8
Bronze Lakehouse ID:     77d24027-6a1c-43a8-a998-1a14dd3c0d52
SAMMS Connection ID:     9743b95a-fd66-4f7c-9767-e6eb0f1ecab7
```

**`bhg_silver.ctrl.Forms2Process` — additional columns required for AnswerSignatures:**

These columns are NOT used by FormQuestionAnswers but are essential for AnswerSignatures. They store the actual source column names for each signature type in each form table.

| Column | Type | Meaning | Example value |
|---|---|---|---|
| `CompletedBy` | varchar | Source column name for CompletedBy sig date | `CompletedBySignatureDate` or `null` |
| `Counselor` | varchar | Source column name for Counselor sig date | `CounselorSignatureDate` or `null` |
| `Doctor` | varchar | Source column name for Doctor sig date | `DoctorSignatureDate` or `null` |
| `MedicalProvider` | varchar | Source column name for MedicalProvider sig date | `MedicalStaffSignatureDate` or `null` |
| `Patient` | varchar | Source column name for Patient sig date | `PatientSignatureDate` or `null` |
| `Provider` | varchar | Source column name for Provider sig date | `ProviderSignatureDate` or `null` |
| `Requestor` | varchar | Source column name for Requestor sig date | `null` (rarely populated) |
| `Staff` | varchar | Source column name for Staff sig date | `StaffSignatureDate` or `null` |
| `Supervisor` | varchar | Source column name for Supervisor sig date | `SupervisorSignatureDate` or `null` |

When a column is `null`, the corresponding signature date column in Silver is set to `null` for that form type.

---

## 5. Pipeline Parameters and Variables — Define These First

### Parameters

Go to: **Pipeline canvas → click empty space → Parameters tab → + New**

#### Parameter 1: `p_ingest_run_id`

| Setting | Value |
|---|---|
| Name | `p_ingest_run_id` |
| Type | `String` |
| Default | `test-run-001` |

---

#### Parameter 2: `p_lookback_days`

| Setting | Value |
|---|---|
| Name | `p_lookback_days` |
| Type | `Int` |
| Default | `30` |

**Note:** This value is used for the Forms2Process custom table date filters (`DateFilterEnabled = 1`). The base Form/AnswerSignature query intentionally has **no date filter** — it always pulls all forms. See Section 21 for why.

---

#### Parameter 3: `p_sites`

| Setting | Value |
|---|---|
| Name | `p_sites` |
| Type | `Array` |
| Default | See JSON below |

**Default Value:**
```json
[
  {
    "site_code": "ColoradoSpringsV5",
    "source_database": "SAMMS-ColoradoSpringsV5"
  }
]
```

---

#### Parameter 4: `p_reload`

| Setting | Value |
|---|---|
| Name | `p_reload` |
| Type | `Bool` |
| Default | `false` |

**Why:** When `true`, wrkdt for Forms2Process date filters is set to `1/1/2010`. This replaces the old system's hard-coded `WorkDate == 2/2/2024` trigger that was a one-time migration step. Use `p_reload = true` for a full historical reload going forward.

---

---

## 6. Step 1 — Create the Data Pipeline in Fabric

1. Open your Fabric workspace → click **+ New** → **Data pipeline**
2. Name it: `pl_answersig_samms_to_lakehouse`
3. Click **Create**
4. Add all four parameters as defined in Section 5

---

## 7. Step 2 — Add the ForEach Activity

1. Drag a **ForEach** onto the canvas
2. Rename it: `fe_each_samms_site`

**Settings tab:**

| Setting | Value |
|---|---|
| Items | `@pipeline().parameters.p_sites` |
| Sequential | **Checked (True)** |

**JSON:**
```json
{
    "name": "fe_each_samms_site",
    "type": "ForEach",
    "dependsOn": [],
    "typeProperties": {
        "items": { "value": "@pipeline().parameters.p_sites", "type": "Expression" },
        "isSequential": true,
        "activities": []
    }
}
```

---

## 8. Step 3 — Inside ForEach: Lookup 1 — Check AnswerSignature Table Exists

### What this does

Probes for the `answersignature` table — note the **lowercase name** used in the old system's probe: `WHERE name = 'answersignature'`. This is the correct table name to check. If it does not exist, this clinic has not been upgraded to the SAMMS version that supports the form signature engine.

> **This is different from FormQuestionAnswers** which checks for the `Form` table. A clinic could theoretically have `Form` but not `answersignature`, or vice versa.

### Add the activity
1. Click **Edit** on the ForEach to open the inner canvas
2. Drag a **Lookup** activity
3. Rename it: `lkp_check_answersig_table`

### Configure Settings tab

| Setting | Value |
|---|---|
| Source type | SQL Server |
| Connection | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| First row only | **Checked (True)** |

**Query expression (Expression mode):**
```
@concat(
'SELECT answersig_exists = COUNT(1)
FROM [', item().source_database, '].sys.tables
WHERE name = ''answersignature'''
)
```

This produces:
```sql
SELECT answersig_exists = COUNT(1)
FROM [SAMMS-ColoradoSpringsV5].sys.tables
WHERE name = 'answersignature'
```

Returns `{ "answersig_exists": 1 }` or `{ "answersig_exists": 0 }`.

**Policy:** Timeout `0.12:00:00`, Retry `0`.

**JSON:**
```json
{
    "name": "lkp_check_answersig_table",
    "type": "Lookup",
    "dependsOn": [],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "source": {
            "type": "SqlServerSource",
            "sqlReaderQuery": {
                "value": "@concat('SELECT answersig_exists = COUNT(1) FROM [', item().source_database, '].sys.tables WHERE name = ''answersignature''')",
                "type": "Expression"
            },
            "queryTimeout": "02:00:00"
        },
        "firstRowOnly": true,
        "datasetSettings": {
            "type": "SqlServerTable",
            "schema": [],
            "externalReferences": { "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7" }
        }
    }
}
```

---

## 9. Step 4 — Inside ForEach: IfCondition — Gate on AnswerSignature Table

### Add the activity
1. Drag an **IfCondition** activity
2. Rename it: `if_answersig_table_exists`
3. Dependency: `lkp_check_answersig_table` → `if_answersig_table_exists` (Succeeded)

**Condition:**
```
@equals(activity('lkp_check_answersig_table').output.firstRow.answersig_exists, 1)
```

**JSON:**
```json
{
    "name": "if_answersig_table_exists",
    "type": "IfCondition",
    "dependsOn": [
        { "activity": "lkp_check_answersig_table", "dependencyConditions": ["Succeeded"] }
    ],
    "typeProperties": {
        "expression": {
            "value": "@equals(activity('lkp_check_answersig_table').output.firstRow.answersig_exists, 1)",
            "type": "Expression"
        },
        "ifTrueActivities": [],
        "ifFalseActivities": []
    }
}
```

Click **Edit** on the True branch to open it. All remaining activities in this section go inside `ifTrueActivities`.

---

## 10. Step 5 — Inside True Branch: Lookup 2 — Get All Existing Tables

Identical to the FormQuestionAnswers pipeline. Returns ALL table names from this SAMMS database so the SQL builder notebook can filter against them.

### Add the activity
1. Drag a **Lookup** onto the True branch canvas
2. Rename it: `lkp_get_existing_tables`

**Query expression:**
```
@concat('SELECT name FROM [', item().source_database, '].sys.tables')
```

**First row only:** Unchecked (returns all rows).

**JSON:**
```json
{
    "name": "lkp_get_existing_tables",
    "type": "Lookup",
    "dependsOn": [],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "source": {
            "type": "SqlServerSource",
            "sqlReaderQuery": {
                "value": "@concat('SELECT name FROM [', item().source_database, '].sys.tables')",
                "type": "Expression"
            },
            "queryTimeout": "02:00:00"
        },
        "firstRowOnly": false,
        "datasetSettings": {
            "type": "SqlServerTable",
            "schema": [],
            "externalReferences": { "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7" }
        }
    }
}
```

---

## 10a. Step 5a - Inside True Branch: Lookup 3 - Get Existing Columns

Returns every table/column pair from the current SAMMS database. This protects
the SQL builder from site-specific schema drift where a table exists but one of
the signature columns configured in `Forms2Process` does not exist for that site.

### Add the activity
1. Drag a **Lookup** onto the True branch canvas
2. Rename it: `lkp_get_existing_columns`
3. Dependency: `lkp_get_existing_tables` -> `lkp_get_existing_columns` (Succeeded)

**Query expression:**
```
@concat(
'SELECT table_name = t.name, column_name = c.name
FROM [', item().source_database, '].sys.tables t
JOIN [', item().source_database, '].sys.columns c
  ON t.object_id = c.object_id'
)
```

**First row only:** Unchecked.

---

## 11. Step 6 — Inside True Branch: Notebook Activity — SQL Builder

This notebook is a **pure string generator**. It reads `bhg_silver.ctrl.Forms2Process` (Fabric lakehouse — no SAMMS connection) and builds the full UNION SQL string for the site. The Copy activity executes it.

### Add the activity
1. Drag a **Notebook** activity
2. Rename it: `nb_answersig_build_site_sql`
3. Dependency: `lkp_get_existing_columns` → `nb_answersig_build_site_sql` (Succeeded)
   *(must depend on `lkp_get_existing_columns`, not just `lkp_get_existing_tables`, so both lookup outputs are available)*

### Configure Parameters

| Parameter Name | Type | Value |
|---|---|---|
| `p_site_code` | String | `@item().site_code` (Expression) |
| `p_source_database` | String | `@item().source_database` (Expression) |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |
| `p_lookback_days` | Int | `@pipeline().parameters.p_lookback_days` (Expression) |
| `p_reload` | Bool | `@pipeline().parameters.p_reload` (Expression) |
| `p_existing_tables_json` | String | `@string(activity('lkp_get_existing_tables').output.value)` (Expression) |
| `p_existing_columns_json` | String | `@string(activity('lkp_get_existing_columns').output.value)` (Expression) |

**Policy:** Timeout `0.12:00:00`, Retry `0`.

> **Why both lookups must be wired:** `p_existing_tables_json` gates which Forms2Process tables get a UNION block. `p_existing_columns_json` gates which sig-date columns within each block are emitted vs set to null. Without `p_existing_columns_json`, `column_exists()` always returns `False` and every sig-date column in the default-case Forms2Process tables silently becomes `null`.

**JSON:**
```json
{
    "name": "nb_answersig_build_site_sql",
    "type": "TridentNotebook",
    "dependsOn": [
        { "activity": "lkp_get_existing_columns", "dependencyConditions": ["Succeeded"] }
    ],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "notebookId": "<<your-notebook-guid>>",
        "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
        "parameters": {
            "p_site_code":               { "value": { "value": "@item().site_code",                                                             "type": "Expression" }, "type": "string" },
            "p_source_database":         { "value": { "value": "@item().source_database",                                                      "type": "Expression" }, "type": "string" },
            "p_ingest_run_id":           { "value": { "value": "@pipeline().parameters.p_ingest_run_id",                                       "type": "Expression" }, "type": "string" },
            "p_lookback_days":           { "value": { "value": "@pipeline().parameters.p_lookback_days",                                       "type": "Expression" }, "type": "int"    },
            "p_reload":                  { "value": { "value": "@pipeline().parameters.p_reload",                                               "type": "Expression" }, "type": "bool"   },
            "p_existing_tables_json":    { "value": { "value": "@string(activity('lkp_get_existing_tables').output.value)",                     "type": "Expression" }, "type": "string" },
            "p_existing_columns_json":   { "value": { "value": "@string(activity('lkp_get_existing_columns').output.value)",                    "type": "Expression" }, "type": "string" }
        }
    }
}
```

---

## 12. Step 7 — Inside True Branch: Copy Activity — Extract to Bronze

The notebook `nb_answersig_build_site_sql` constructs the full UNION SQL and returns it via `mssparkutils.notebook.exit()`. The Copy activity receives this directly — no Set Variable intermediary needed.

### Add the activity
1. Drag a **Copy** activity
2. Rename it: `cp_answersig_to_bronze`
3. Dependency: `nb_answersig_build_site_sql` → `cp_answersig_to_bronze` (Succeeded)

**Important database-context note:** The SQL generated by `nb_answersig_build_site_sql`
uses fully qualified source tables like `[SAMMS-ColoradoSpringsV5].dbo.[Form]`.
This is intentional. Fabric SQL Server connections may keep their default database
(for example `SAMMSGLOBAL`) even when the current loop item has a different
`source_database`. Fully qualifying every source table prevents `Invalid object
name 'dbo.Form'` errors in the Copy activity.

### Configure Source tab

| Setting | Value |
|---|---|
| Source type | SQL Server |
| Connection | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| Query | `@activity('nb_answersig_build_site_sql').output.result.exitValue` (Expression) |
| Query timeout | `02:00:00` |

### Configure Sink tab

| Setting | Value |
|---|---|
| Sink type | Lakehouse |
| Workspace ID | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder | `Tables` |
| Schema | `Forms` |
| Table | `br_tblFormAnswerSig` |
| Table action | **Append** |
| Apply V-Order | No |

**Why a separate Bronze table from FormQuestionAnswers?**  
FormAnswerSignatures has a completely different column structure (9 signature date columns vs question/answer columns). They cannot share a Bronze table.

### Configure Mapping tab

| Setting | Value |
|---|---|
| Type conversion | Enabled |
| Allow data truncation | True |
| Treat boolean as number | False |

**Policy:** Timeout `0.12:00:00`, Retry `0`.

**JSON:**
```json
{
    "name": "cp_answersig_to_bronze",
    "type": "Copy",
    "dependsOn": [
        { "activity": "nb_answersig_build_site_sql", "dependencyConditions": ["Succeeded"] }
    ],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "source": {
            "type": "SqlServerSource",
            "sqlReaderQuery": { "value": "@activity('nb_answersig_build_site_sql').output.result.exitValue", "type": "Expression" },
            "queryTimeout": "02:00:00",
            "partitionOption": "None",
            "datasetSettings": {
                "type": "SqlServerTable",
                "schema": [],
                "externalReferences": { "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7" }
            }
        },
        "sink": {
            "type": "LakehouseTableSink",
            "tableActionOption": "Append",
            "partitionOption": "None",
            "applyVOrder": false,
            "datasetSettings": {
                "linkedService": {
                    "name": "bhg_bronze",
                    "properties": {
                        "type": "Lakehouse",
                        "typeProperties": {
                            "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
                            "artifactId": "77d24027-6a1c-43a8-a998-1a14dd3c0d52",
                            "rootFolder": "Tables"
                        }
                    }
                },
                "type": "LakehouseTable",
                "schema": [],
                "typeProperties": { "schema": "Forms", "table": "br_tblFormAnswerSig" }
            }
        },
        "enableStaging": false,
        "translator": {
            "type": "TabularTranslator",
            "typeConversion": true,
            "typeConversionSettings": { "allowDataTruncation": true, "treatBooleanAsNumber": false }
        }
    }
}
```

---

## 14. Step 9 — Add the Silver Notebook Activity (After ForEach)

1. Go back to the **main pipeline canvas**
2. Drag a **Notebook** activity
3. Rename it: `nb_answersig_bronze_to_silver`
4. Dependency: `fe_each_samms_site` → `nb_answersig_bronze_to_silver` (Succeeded)

### Configure Parameters

| Parameter | Type | Value |
|---|---|---|
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |
| `p_lookback_days` | Int | `@pipeline().parameters.p_lookback_days` (Expression) |
| `p_reload` | Bool | `@pipeline().parameters.p_reload` (Expression) |

**Policy:** Timeout `0.12:00:00`, Retry `0`.

---

## 15. Step 10 — Create the SQL Builder Notebook: nb_answersig_build_site_sql

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_answersig_build_site_sql`
3. Attach `bhg_silver` to the Lakehouse panel — **do NOT attach any SAMMS connection**
4. The notebook has **one cell**

### The Full SQL Builder Notebook — paste this exactly

```python
# ─────────────────────────────────────────────────────────────────────
# nb_answersig_build_site_sql
#
# PURPOSE: Pure SQL string generator — NO SAMMS connection.
#
# READS:  bhg_silver.ctrl.Forms2Process  (Fabric lakehouse)
# INPUT:  p_existing_tables_json  — JSON string from Lookup 2 (sys.tables)
#         p_site_code, p_source_database, p_ingest_run_id,
#         p_lookback_days, p_reload
#
# BUILDS: Full UNION SQL for FormAnswerSignatures:
#           UNION 1: All active forms (no date filter — matches old system)
#           UNION 2: Deleted forms (f.Isdeleted = 1)
#           + UNION per Forms2Process table (3 switch cases)
#
# OUTPUT: SQL string via mssparkutils.notebook.exit()
#         The Copy activity executes this string against SAMMS.
# ─────────────────────────────────────────────────────────────────────
import json
from datetime import datetime, timedelta

# ── Parameters (injected by pipeline; fallbacks for manual dev runs) ──
try: p_site_code
except NameError: p_site_code = "ColoradoSpringsV5"

try: p_source_database
except NameError: p_source_database = "SAMMS-ColoradoSpringsV5"

try: p_ingest_run_id
except NameError: p_ingest_run_id = "test-run-001"

try: p_lookback_days
except NameError: p_lookback_days = 30

try: p_reload
except NameError: p_reload = False

try: p_existing_tables_json
except NameError: p_existing_tables_json = "[]"

try: p_existing_columns_json
except NameError: p_existing_columns_json = "[]"

# ── Build the set of tables that exist in this SAMMS database ────────
existing_rows   = json.loads(p_existing_tables_json)
existing_tables = {row["name"].lower() for row in existing_rows}
existing_column_rows = json.loads(p_existing_columns_json)
existing_columns = {
    (row["table_name"].lower(), row["column_name"].lower())
    for row in existing_column_rows
}
existing_columns_by_table = {}
for row in existing_column_rows:
    existing_columns_by_table.setdefault(row["table_name"].lower(), []).append(row["column_name"])

def column_exists(table_name, column_name):
    if not table_name or not column_name:
        return False
    return (str(table_name).lower(), str(column_name).lower()) in existing_columns

def resolve_column(table_name, configured_col):
    """
    Resolve a Forms2Process column name against the actual source table columns.
    Exact match wins. If not found, allow one unique suffix/contains match so
    values like PatientSignatureDate can map to AdmissionAssessmentPatientSignatureDate.
    """
    if not table_name or not configured_col:
        return None
    table_key = str(table_name).lower()
    configured_key = str(configured_col).lower()
    actual_cols = existing_columns_by_table.get(table_key, [])

    for actual in actual_cols:
        if actual.lower() == configured_key:
            return actual

    suffix_matches = [actual for actual in actual_cols if actual.lower().endswith(configured_key)]
    if len(suffix_matches) == 1:
        print(f"  INFO resolved column: {table_name}.{configured_col} -> {suffix_matches[0]}")
        return suffix_matches[0]

    contains_matches = [actual for actual in actual_cols if configured_key in actual.lower()]
    if len(contains_matches) == 1:
        print(f"  INFO resolved column: {table_name}.{configured_col} -> {contains_matches[0]}")
        return contains_matches[0]

    if len(suffix_matches) > 1 or len(contains_matches) > 1:
        print(f"  WARN ambiguous column: {table_name}.{configured_col}; emitting NULL")
    return None

print(f"Site:           {p_site_code}")
print(f"Database:       {p_source_database}")
print(f"Run ID:         {p_ingest_run_id}")
print(f"Existing tables in SAMMS (count): {len(existing_tables)}")

# ── Compute wrkdt (used only for DateFilterEnabled=1 tables) ─────────
# NOTE: The base Form/AnswerSignature query does NOT use wrkdt.
# wrkdt only affects the optional WHERE clauses on Forms2Process UNIONs.
if p_reload:
    wrkdt = "2010-01-01"
else:
    wrkdt = (datetime.now() - timedelta(days=int(p_lookback_days))).strftime("%Y-%m-%d")

print(f"wrkdt (for Forms2Process date filters): {wrkdt}")

# ── Read Forms2Process from bhg_silver.ctrl ───────────────────────────
# NOTE: No ORDER BY Prefix — unlike FormQuestionAnswers which sorts by Prefix.
# The old system does: db.TblForms2Process.Where(x => x.Enabled && x.RowState).ToList()
# with no ordering.
f2p_df = (
    spark.table("bhg_silver.ctrl.Forms2Process")
    .filter("Enabled = true AND RowState = true")
    .toPandas()
)
print(f"Forms2Process rows loaded: {len(f2p_df)}")
print("Forms2Process columns:")
print(f2p_df.columns.tolist())

aa_debug = f2p_df[
    f2p_df["TableName"].astype(str).str.strip().str.lower().eq("admissionassessment")
]
print("AdmissionAssessment Forms2Process row:")
if aa_debug.empty:
    print("  NOT FOUND")
else:
    print(
        aa_debug[[
            "FormName",
            "TableName",
            "Patient",
            "Staff",
            "Supervisor",
            "Provider",
            "CreatedOn",
            "ModifiedOn",
            "Prefix"
        ]].to_string(index=False)
    )

sc = p_site_code  # shorthand
db = f"[{p_source_database}]"   # source SAMMS database, e.g. [SAMMS-ColoradoSpringsV5]

def tbl(name):
    """Return a fully-qualified SAMMS table reference."""
    return f"{db}.dbo.[{name}]"

# ── Metadata columns injected into every SELECT ──────────────────────
meta_cols = (
    f"  '{sc}' AS _site_code,\n"
    f"  '{p_source_database}' AS _source_database,\n"
    f"  '{p_ingest_run_id}' AS _ingest_run_id,\n"
    f"  GETDATE() AS _extracted_at,\n"
    f"  '{wrkdt}' AS _lookback_date,\n"
)

# ── Standard IsDeleted expression (alias 'a') — used in Forms2Process loop
# NOTE: The base Form query uses alias 'f', not 'a'. See base query below
#       where f.IsDeleted is referenced directly.
std_isdeleted = (
    "IsDeleted = CASE "
    "WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 "
    "AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 "
    "THEN 0 ELSE 1 END"
)

# IsDeleted expression for the base Form query — alias is 'f' not 'a'
base_isdeleted = (
    "IsDeleted = CASE "
    "WHEN ISNULL(f.IsDeleted,0)=0 AND pa.IsDeleted<>1 "
    "AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 "
    "THEN 0 ELSE 1 END"
)

# ── Helper: sig date CASE expression with 1900-01-01 sentinel ────────
def sig_date_expr(alias, col_name):
    """
    Returns: CASE WHEN CONVERT(date, {alias}.{col_name}) IS NULL
                  THEN '1900-01-01'
                  ELSE CONVERT(date, {alias}.{col_name})
             END
    Matches old system's pattern exactly.
    """
    return (
        f"CASE WHEN CONVERT(date, {alias}.{col_name}) IS NULL "
        f"THEN '1900-01-01' "
        f"ELSE CONVERT(date, {alias}.{col_name}) END"
    )

# ── Step 1: Base query — Form/FormTemplate + AnswerSignature subqueries
#
# UNION 1: All active forms — NO date filter (WHERE clause is commented
#   out in the old system and intentionally omitted here).
# UNION 2: Deleted forms (WHERE f.Isdeleted = 1).
#
# ── Base query inner SELECT (shared by UNION 1 and UNION 2) ─────────
# Alias is 'f' for Form — NOT 'a'. IsDeleted uses base_isdeleted (f.IsDeleted).
base_inner_select = (
    f"  SELECT SiteCode='{sc}', ft.FormName, f.id AS FormId, f.ClientId,\n"
    f"         f.CreatedOn, f.UpdatedOn,\n"
    f"         {base_isdeleted}\n"
    f"  FROM {tbl('Form')} f WITH (NOLOCK)\n"
    f"    LEFT JOIN {tbl('FormTemplate')} ft WITH (NOLOCK) ON f.FormTemplateId = ft.Id\n"
    f"    INNER JOIN {tbl('SF_PatientPreAdmission')} pa WITH (NOLOCK) ON f.PreAdmissionId = pa.ID\n"
    f"    LEFT JOIN {tbl('SF_DataForms')} d WITH (NOLOCK) ON pa.DataFormId = d.Id"
)

# ── Step 1: Base query — CTE pre-aggregation of AnswerSignature ──────
#
# PERFORMANCE CHANGE (replacing 9 correlated subqueries):
# Old approach: 9 × SELECT TOP 1 ... FROM AnswerSignature WHERE FormId = x.FormId
#   → AnswerSignature hit ~9 times per Form row → ~161k seeks for 17k rows → 290s wait.
# New approach: one CTE groups AnswerSignature once by FormId, then a single
#   LEFT JOIN resolves all 9 sig dates → AnswerSignature scanned once → ~10-30s.
#
# Sentinel logic: CASE WHEN Sign IS NULL THEN '1900-01-01' ELSE DateTime END
#   is applied inside the CTE, preserving exact parity with old correlated subquery.
# Counselor: CTE covers BOTH 'CounselorSignatureSignatureDate' and
#   'CounselorSignatureDate' DateField values (some SAMMS versions use either).
# If no AnswerSignature row exists for a slot → LEFT JOIN produces NULL.
# This must stay NULL. The old correlated subquery only returns the
# 1900-01-01 sentinel when an AnswerSignature row exists but Sign is NULL.
#
strCmd = (
    f"WITH _AS_agg AS (\n"
    f"  SELECT FormId,\n"
    f"    MAX(CASE WHEN DateField = 'CompletedBySignatureSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS CompletedBySignatureSignatureDate,\n"
    f"    MAX(CASE WHEN DateField IN ('CounselorSignatureSignatureDate','CounselorSignatureDate')\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS CounselorSignatureSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'DoctorSignatureSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS DoctorSignatureSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'MedicalProviderSignatureSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS MedicalProviderSignatureSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'PatientSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS PatientSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'ProviderSignatureSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS ProviderSignatureSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'RequestorSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS RequestorSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'StaffSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS StaffSignatureDate,\n"
    f"    MAX(CASE WHEN DateField = 'SupervisorSignatureSignatureDate'\n"
    f"        THEN CASE WHEN Sign IS NULL THEN '1900-01-01'\n"
    f"             ELSE CONVERT(varchar(20),[DateTime],23) END END) AS SupervisorSignatureSignatureDate\n"
    f"  FROM {tbl('AnswerSignature')} WITH (NOLOCK)\n"
    f"  WHERE DateField IN (\n"
    f"    'CompletedBySignatureSignatureDate','CounselorSignatureSignatureDate',\n"
    f"    'CounselorSignatureDate','DoctorSignatureSignatureDate',\n"
    f"    'MedicalProviderSignatureSignatureDate','PatientSignatureDate',\n"
    f"    'ProviderSignatureSignatureDate','RequestorSignatureDate',\n"
    f"    'StaffSignatureDate','SupervisorSignatureSignatureDate'\n"
    f"  )\n"
    f"  GROUP BY FormId\n"
    f")\n"
    f"SELECT DISTINCT\n{meta_cols}"
    f"  x.SiteCode, x.FormName,\n"
    f"  CONVERT(varchar(100), x.FormId) AS FormId,\n"
    f"  x.ClientId, x.CreatedOn, x.UpdatedOn, x.IsDeleted,\n"
    # Sig date columns — keep NULL when no AnswerSignature row exists.
    # The 1900-01-01 sentinel is already applied inside the CTE only when
    # a matching row exists and Sign is NULL, matching old C# behaviour.
    f"  ag.CompletedBySignatureSignatureDate AS CompletedBySignatureSignatureDate,\n"
    f"  ag.CounselorSignatureSignatureDate AS CounselorSignatureSignatureDate,\n"
    f"  ag.DoctorSignatureSignatureDate AS DoctorSignatureSignatureDate,\n"
    f"  ag.MedicalProviderSignatureSignatureDate AS MedicalProviderSignatureSignatureDate,\n"
    f"  ag.PatientSignatureDate AS PatientSignatureDate,\n"
    f"  ag.ProviderSignatureSignatureDate AS ProviderSignatureSignatureDate,\n"
    f"  ag.RequestorSignatureDate AS RequestorSignatureDate,\n"
    f"  ag.StaffSignatureDate AS StaffSignatureDate,\n"
    f"  ag.SupervisorSignatureSignatureDate AS SupervisorSignatureSignatureDate\n"
    f"FROM (\n"
    # UNION 1 — all active forms, NO date filter (intentional — see Section 21)
    f"{base_inner_select}\n"
    f"\n  UNION\n\n"
    # UNION 2 — deleted forms only
    f"{base_inner_select}\n"
    f"  WHERE f.Isdeleted = 1\n"
    f") x\n"
    f"LEFT JOIN _AS_agg ag ON ag.FormId = x.FormId\n"
)

# ── Legacy special branch: Periodic Reassessment / Prefix 99 ─────────
# BHG_DR contains FormAnswerSignature rows with:
#   FormName = 'Periodic Reassessment'
#   FormId   = '99-' + ClientId + '-' + PreAdmissionId + '-' + Id
# These rows are not generated by the current active Forms2Process config.
# They come from NewPeriodicReassessment + NewPeriodicReassessmentCounselorReview.
# This mirrors [pats].[MergeFormSignaturesPeriodicReassessments]:
# it is intentionally full-site, not lookback filtered.
# Guard with existing_tables so sites without these source tables skip cleanly.
if (
    "newperiodicreassessment" in existing_tables
    and "newperiodicreassessmentcounselorreview" in existing_tables
):
    block = (
        f"\nUNION\n"
        f"SELECT DISTINCT\n{meta_cols}"
        f"  SiteCode='{sc}', 'Periodic Reassessment' AS [FormName],\n"
        f"  '99-' + CONVERT(varchar, ISNULL(a.ClientId, 0))"
        f" + '-' + CONVERT(varchar, ISNULL(b.PreAdmissionId, 0))"
        f" + '-' + CONVERT(varchar, a.Id) AS [FormID],\n"
        f"  ClientId = ISNULL(a.ClientId, 0),\n"
        f"  [CreatedOn] = CONVERT(date, a.CreatedOn),\n"
        f"  [UpdatedOn] = CONVERT(date, a.ModifiedOn),\n"
        f"  IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 THEN 0 ELSE 1 END,\n"
        f"  CompletedBySignatureSignatureDate = null,\n"
        f"  CounselorSignatureSignatureDate = CONVERT(date, b.CounselorSignatureDate),\n"
        f"  DoctorSignatureSignatureDate = null,\n"
        f"  MedicalProviderSignatureSignatureDate = null,\n"
        f"  PatientSignatureDate = CONVERT(date, b.PatientSignatureDate),\n"
        f"  ProviderSignatureSignatureDate = CONVERT(date, b.ProviderSignatureDate),\n"
        f"  RequestorSignatureDate = null,\n"
        f"  StaffSignatureDate = null,\n"
        f"  SupervisorSignatureSignatureDate = CONVERT(date, b.SupervisorSignatureDate)\n"
        f"FROM {tbl('NewPeriodicReassessment')} a\n"
        f"LEFT JOIN {tbl('NewPeriodicReassessmentCounselorReview')} b\n"
        f"    ON a.Id = b.NewPeriodicReassessmentId\n"
        f"   AND a.PreAdmissionId = b.PreAdmissionId\n"
    )
    strCmd += block
    print("  + NewPeriodicReassessment (Periodic Reassessment / Prefix 99)")
else:
    print("  SKIP Periodic Reassessment / Prefix 99: source tables not found")

# ── Step 2: Loop over Forms2Process — UNION in each custom table ─────
# NOTE: No ORDER BY — matches old system's unordered loop.
# 3 switch cases: tblORDERREQ, tblTP17REVIEW, default.

for _, xf in f2p_df.iterrows():
    table_name   = xf["TableName"]
    form_name    = xf["FormName"]
    prefix       = xf["Prefix"]
    created_on   = xf["CreatedOn"]
    modified_on  = xf["ModifiedOn"]
    date_filter  = bool(xf["DateFilterEnabled"])

    # Sig date source column names — may be None/NaN when that role
    # does not sign this form type.
    def col_val(col):
        v = xf.get(col)
        if v is None:
            return None
        s = str(v).strip()
        if s == "" or s.lower() in ("nan", "none", "null"):
            return None
        return s

    col_completed_by  = col_val("CompletedBy")
    col_counselor     = col_val("Counselor")
    col_doctor        = col_val("Doctor")
    col_medical_prov  = col_val("MedicalProvider")
    col_patient       = col_val("Patient")
    col_provider      = col_val("Provider")
    col_requestor     = col_val("Requestor")
    col_staff         = col_val("Staff")
    col_supervisor    = col_val("Supervisor")

    if str(table_name).strip().lower() == "admissionassessment":
        print("AdmissionAssessment loop values:")
        print("Patient =", col_patient)
        print("Staff =", col_staff)
        print("Supervisor =", col_supervisor)
        print("Provider =", col_provider)

    if not table_name or str(table_name) in ("", "nan"):
        continue

    if table_name.lower() not in existing_tables:
        print(f"  SKIP (not in this SAMMS): {table_name}")
        continue

    # ────────────────────────────────────────────────────────────────
    # CASE: tblORDERREQ
    # FormID trailing '-1' (not '-' like in FormQA).
    # ClientId = cltID (raw, not ABS — EF layer stores ABS later).
    # ProviderSig = ISNULL(DrSigDt, SigNurseDt) with 1900-01-01 sentinel.
    # SupervisorSig = sigCoordinatorDt with 1900-01-01 sentinel.
    # All other sig cols = null.
    # ────────────────────────────────────────────────────────────────
    if table_name == "tblORDERREQ":
        test_filter = (
            "status = 'Approved' "
            "AND Notes NOT LIKE 'Test %' AND Notes <> 'TEST' "
            "AND DrNote <> 'HEllo test' AND DrNote <> 'TEST'"
        )
        block = (
            f"\nUNION\n"
            f"SELECT\n{meta_cols}"
            f"  SiteCode='{sc}', 'Level Justification' AS [FormName],\n"
            f"  '9-1-' + CONVERT(varchar, ABS(cltID)) + '-' + CONVERT(varchar, ReqNum) + '-1' AS FormId,\n"
            f"  ClientId = cltID,\n"
            f"  [CreatedOn]  = CONVERT(date, DateAdded),\n"
            f"  [UpdatedOn]  = CONVERT(date, statusDate),\n"
            f"  IsDeleted    = CASE WHEN cltID < 0 THEN 1 ELSE 0 END,\n"
            f"  CompletedBySignatureSignatureDate      = null,\n"
            f"  CounselorSignatureSignatureDate        = null,\n"
            f"  DoctorSignatureSignatureDate           = null,\n"
            f"  MedicalProviderSignatureSignatureDate  = null,\n"
            f"  PatientSignatureDate                   = null,\n"
            f"  ProviderSignatureSignatureDate = CASE\n"
            f"      WHEN ISNULL(CONVERT(date, DrSigDt), CONVERT(date, SigNurseDt)) IS NULL\n"
            f"       AND status = 'Approved' THEN '1900-01-01'\n"
            f"      ELSE ISNULL(CONVERT(date, DrSigDt), CONVERT(date, SigNurseDt)) END,\n"
            f"  RequestorSignatureDate                 = null,\n"
            f"  StaffSignatureDate                     = null,\n"
            f"  SupervisorSignatureSignatureDate = CASE\n"
            f"      WHEN CONVERT(date, sigCoordinatorDt) IS NULL AND status = 'Approved'\n"
            f"      THEN '1900-01-01'\n"
            f"      ELSE CONVERT(date, sigCoordinatorDt) END\n"
            f"FROM {tbl('tblORDERREQ')}\n"
            f"WHERE {test_filter}\n"
        )
        if date_filter:
            block += (
                f"  AND (DateAdded >= '{wrkdt}'"
                f" OR ISNULL(statusDate, DateAdded) >= '{wrkdt}')\n"
            )
        strCmd += block
        print(f"  + tblORDERREQ (Level Justification)")
        continue

    # ────────────────────────────────────────────────────────────────
    # CASE: tblTP17REVIEW
    # ClientId = tprCLTID (raw — NOT ABS, unlike FormQA).
    # FormID = '8-1-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID
    # Direct sig date columns (no AnswerSignature correlated subquery).
    # PatientSig → tprCLIRNTSIGDate, ProviderSig → tprDRSIGDate,
    # StaffSig   → tprCOUNSSIGDate (with compound null guard),
    # SupervisorSig → tprSUPERSIGDate (no null guard — raw value).
    # DateFilterEnabled: checks 7 columns.
    # ────────────────────────────────────────────────────────────────
    elif table_name == "tblTP17REVIEW":
        block = (
            f"\nUNION\n"
            f"SELECT DISTINCT\n{meta_cols}"
            f"  SiteCode, FormName, FormID, ClientId, CreatedOn, UpdatedOn, IsDeleted,\n"
            f"  CompletedBySignatureSignatureDate,\n"
            f"  CounselorSignatureSignatureDate,\n"
            f"  DoctorSignatureSignatureDate,\n"
            f"  MedicalProviderSignatureSignatureDate,\n"
            f"  PatientSignatureDate,\n"
            f"  ProviderSignatureSignatureDate,\n"
            f"  RequestorSignatureDate,\n"
            f"  StaffSignatureDate,\n"
            f"  SupervisorSignatureSignatureDate\n"
            f"FROM (\n"
            f"  SELECT SiteCode='{sc}', 'TP-' + tprType AS [FormName],\n"
            f"    '8-1-' + CONVERT(varchar, ABS(tprCLTID))"
            f" + '-' + CONVERT(varchar, tpRID)"
            f" + '-' + CONVERT(varchar, tprTPID) AS [FormID],\n"
            f"    tprCLTID AS ClientId,\n"                                 # raw — NOT ABS
            f"    CONVERT(date, tprDT) AS [CreatedOn],\n"
            f"    null AS [UpdatedOn],\n"
            f"    IsDeleted = CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END,\n"
            f"    CompletedBySignatureSignatureDate     = null,\n"
            f"    CounselorSignatureSignatureDate       = null,\n"
            f"    DoctorSignatureSignatureDate          = null,\n"
            f"    MedicalProviderSignatureSignatureDate = null,\n"
            f"    PatientSignatureDate = CASE\n"
            f"        WHEN CONVERT(date, tprCLIRNTSIGDate) IS NULL THEN '1900-01-01'\n"
            f"        ELSE CONVERT(date, tprCLIRNTSIGDate) END,\n"
            f"    ProviderSignatureSignatureDate = CASE\n"
            f"        WHEN CONVERT(date, tprDRSIGDate) IS NULL THEN '1900-01-01'\n"
            f"        ELSE CONVERT(date, tprDRSIGDate) END,\n"
            f"    RequestorSignatureDate = null,\n"
            # StaffSig: sentinel only when BOTH CounsSig AND SuperSig are null
            f"    StaffSignatureDate = CASE\n"
            f"        WHEN CONVERT(date, tprCOUNSSIGDate) IS NULL\n"
            f"         AND CONVERT(date, tprSUPERSIGDate) IS NULL THEN '1900-01-01'\n"
            f"        ELSE CONVERT(date, tprCOUNSSIGDate) END,\n"
            # SupervisorSig: raw value, no null guard
            f"    SupervisorSignatureSignatureDate = CONVERT(date, tprSUPERSIGDate)\n"
            f"  FROM {tbl('tblTP17REVIEW')}\n"
            f") tp\n"
        )
        if date_filter:
            # 7-column date filter for TP17REVIEW
            block += (
                f"WHERE (CreatedOn >= '{wrkdt}'\n"
                f"    OR ISNULL(UpdatedOn, CreatedOn) >= '{wrkdt}'\n"
                f"    OR ProviderSignatureSignatureDate >= '{wrkdt}'\n"
                f"    OR CompletedBySignatureSignatureDate >= '{wrkdt}'\n"
                f"    OR PatientSignatureDate >= '{wrkdt}'\n"
                f"    OR StaffSignatureDate >= '{wrkdt}'\n"
                f"    OR SupervisorSignatureSignatureDate >= '{wrkdt}')\n"
            )
        strCmd += block
        print(f"  + tblTP17REVIEW (Treatment Plan)")
        continue

    # ────────────────────────────────────────────────────────────────
    # DEFAULT CASE — all other Forms2Process tables.
    # Two nested levels:
    #   Level A: FormID formula and ClientId source — per table name
    #   Level B: Each of 9 sig date columns — per table name
    #            (uses different alias for AdmissionAssessment and
    #             NewAdmissionAssessment)
    # ────────────────────────────────────────────────────────────────
    else:
        # ── Level A: FormID / ClientId ────────────────────────────
        if table_name == "SF_PatientPreAdmission":
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(pa.PatientID, 0))"
                f" + '-' + CONVERT(varchar, ISNULL(a.ParentPreAdmissionId, 0))"
                f" + '-' + CONVERT(varchar, ISNULL(a.id, 0))"
            )
            client_id_expr = "ISNULL(pa.PatientID, 0)"
        elif table_name == "SF_DataForm":
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(pa.PatientID, 0))"
                f" + '-' + CONVERT(varchar, ISNULL(a.PreAdmissionId, 0))"
                f" + '-' + CONVERT(varchar, ISNULL(a.id, 0))"
            )
            client_id_expr = "ISNULL(pa.PatientID, 0)"
        elif table_name == "SF_UnderstandingOfTreatment":
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(pa.PatientID, 0))"
                f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
                f" + '-' + CONVERT(varchar, a.id)"
            )
            client_id_expr = "ISNULL(pa.PatientID, 0)"
        elif table_name == "InsuranceBenefitVerification":
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(pa.PatientID, 0))"
                f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
                f" + '-' + CONVERT(varchar, a.id)"
            )
            client_id_expr = "ISNULL(pa.PatientID, 0)"
        elif table_name == "FinancialHardshipApplication":
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(a.CltID, 0))"
                f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
                f" + '-' + CONVERT(varchar, a.id)"
            )
            client_id_expr = "ISNULL(a.CltID, 0)"
        elif table_name == "xNewAdmissionAssessment":
            # Uses b. alias from the NewAdmissionAssessmentASAMDimension6 join
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(b.ClientId, 0))"
                f" + '-' + CONVERT(varchar, b.PreAdmissionId)"
                f" + '-' + CONVERT(varchar, b.id)"
            )
            client_id_expr = "ISNULL(b.ClientId, 0)"
        else:
            # Default sub-case
            form_id_expr = (
                f"'{prefix}-'"
                f" + CONVERT(varchar, ISNULL(a.ClientId, 0))"
                f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
                f" + '-' + CONVERT(varchar, a.id)"
            )
            client_id_expr = "ISNULL(a.ClientId, 0)"

        # UpdatedOn fragment
        updated_on = (
            f"CONVERT(date, a.{modified_on})" if (modified_on and str(modified_on) not in ("", "nan", "None"))
            else "null"
        )

        # ── Level B: 9 signature date columns ─────────────────────
        # For AdmissionAssessment: Patient, Provider, Staff, Supervisor → aas. alias
        # For NewAdmissionAssessment: all 9 → b. alias
        # For MNComprehensiveAssessment: Counselor, Patient, Staff, Supervisor → b. alias
        # SF_PatientPreAdmission: Staff → null when site is "LAB"
        # All others: → a. alias
        def build_sig_col(col_val, col_label, default_alias="a"):
            """Build one signature date column fragment."""
            if col_val is None:
                return f"{col_label} = null"
            alias = default_alias
            source_table = table_name
            if table_name == "AdmissionAssessment" and col_label in (
                "PatientSignatureDate", "ProviderSignatureSignatureDate",
                "StaffSignatureDate", "SupervisorSignatureSignatureDate"
            ):
                alias = "aas"
                source_table = "AdmissionAssessmentSummary"
            elif table_name == "NewAdmissionAssessment":
                alias = "b"
                source_table = "NewAdmissionAssessmentASAMDimension6"
            elif table_name == "MNComprehensiveAssessment" and col_label in (
                "CounselorSignatureSignatureDate",
                "PatientSignatureDate",
                "StaffSignatureDate",
                "SupervisorSignatureSignatureDate"
            ):
                alias = "b"
                source_table = "MNComprehensiveAssessmentSocialHistory"
            # Special: SF_PatientPreAdmission + LAB site → StaffSignatureDate = null
            if table_name == "SF_PatientPreAdmission" and col_label == "StaffSignatureDate" and p_site_code.upper() == "LAB":
                return f"{col_label} = null"
            resolved_col = resolve_column(source_table, col_val)
            if resolved_col is None:
                print(f"  WARN missing column: {source_table}.{col_val}; emitting NULL for {col_label}")
                return f"{col_label} = null"
            return f"{col_label} = {sig_date_expr(alias, resolved_col)}"

        block = (
            f"\nUNION\n"
            f"SELECT DISTINCT\n{meta_cols}"
            f"  SiteCode='{sc}', '{form_name}' AS [FormName],\n"
            f"  {form_id_expr} AS [FormID],\n"
            f"  ClientId = {client_id_expr},\n"
            f"  [CreatedOn]  = CONVERT(date, a.{created_on}),\n"
            f"  [UpdatedOn]  = {updated_on},\n"
            f"  {std_isdeleted},\n"
            f"  {build_sig_col(col_completed_by,  'CompletedBySignatureSignatureDate')},\n"
            f"  {build_sig_col(col_counselor,      'CounselorSignatureSignatureDate')},\n"
            f"  {build_sig_col(col_doctor,         'DoctorSignatureSignatureDate')},\n"
            f"  {build_sig_col(col_medical_prov,   'MedicalProviderSignatureSignatureDate')},\n"
            f"  {build_sig_col(col_patient,        'PatientSignatureDate')},\n"
            f"  {build_sig_col(col_provider,       'ProviderSignatureSignatureDate')},\n"
            f"  {build_sig_col(col_requestor,      'RequestorSignatureDate')},\n"
            f"  {build_sig_col(col_staff,          'StaffSignatureDate')},\n"
            f"  {build_sig_col(col_supervisor,     'SupervisorSignatureSignatureDate')}\n"
        )

        # ── Join strategy ────────────────────────────────────────
        if table_name == "SF_PatientPreAdmission":
            # Self-join: ON a.ID = pa.ID (not a.PreAdmissionId = pa.ID)
            block += (
                f"FROM {tbl(table_name)} a\n"
                f"INNER JOIN {tbl('SF_PatientPreAdmission')} pa ON a.ID = pa.ID\n"
                f"LEFT JOIN {tbl('SF_DataForms')} d ON pa.DataFormId = d.Id\n"
            )
        else:
            block += (
                f"FROM {tbl(table_name)} a\n"
                f"INNER JOIN {tbl('SF_PatientPreAdmission')} pa ON a.PreAdmissionId = pa.ID\n"
                f"LEFT JOIN {tbl('SF_DataForms')} d ON pa.DataFormId = d.Id\n"
            )

        # Additional join for AdmissionAssessment
        # Guard: only add the INNER JOIN if AdmissionAssessmentSummary actually exists
        # in this SAMMS version. Without the guard, sites where the primary table
        # exists but the summary table doesn't would fail at Copy activity runtime.
        if table_name == "AdmissionAssessment":
            if "admissionassessmentsummary" in existing_tables:
                block += (
                    f"INNER JOIN {tbl('AdmissionAssessmentSummary')} aas\n"
                    f"    ON a.Id = aas.AdmissionAssessmentId\n"
                    f"   AND a.PreAdmissionId = aas.PreAdmissionId\n"
                )
            else:
                # Summary table absent — aas sig cols will all have been set to null
                # by column_exists() check above; join is skipped entirely.
                print(f"  WARN: AdmissionAssessmentSummary not found — aas sig cols will be null")

        # Additional join for NewAdmissionAssessment
        # Same guard: only add if the dimension table exists in this SAMMS version.
        if table_name == "NewAdmissionAssessment":
            if "newadmissionassessmentasamdimension6" in existing_tables:
                block += (
                    f"INNER JOIN {tbl('NewAdmissionAssessmentASAMDimension6')} b\n"
                    f"    ON a.preadmissionID = b.preadmissionID\n"
                    f"   AND a.ID = b.NewAdmissionAssessmentFormId\n"
                )
            else:
                print(f"  WARN: NewAdmissionAssessmentASAMDimension6 not found — b sig cols will be null")

        # Additional join for MNComprehensiveAssessment
        # updatedProgram.cs routes Counselor/Patient/Staff/Supervisor signature dates
        # through MNComprehensiveAssessmentSocialHistory alias b.
        if table_name == "MNComprehensiveAssessment":
            if "mncomprehensiveassessmentsocialhistory" in existing_tables:
                block += (
                    f"INNER JOIN {tbl('MNComprehensiveAssessmentSocialHistory')} b\n"
                    f"    ON a.preadmissionID = b.preadmissionID\n"
                    f"   AND a.Id = b.MNComprehensiveAssessmentFormId\n"
                )
            else:
                print(f"  WARN: MNComprehensiveAssessmentSocialHistory not found — b sig cols will be null")

        # Date filter WHERE clause
        if date_filter and modified_on and str(modified_on) not in ("", "nan", "None"):
            block += (
                f"WHERE a.{created_on} >= '{wrkdt}'\n"
                f"   OR ISNULL(a.{modified_on}, a.{created_on}) >= '{wrkdt}'\n"
            )
        elif date_filter:
            block += f"WHERE a.{created_on} >= '{wrkdt}'\n"

        strCmd += block
        print(f"  + {table_name} (Prefix {prefix}, DateFilter={date_filter})")

# ── Step 3: Return the full SQL string ───────────────────────────────
# NOTE: Unlike FormQuestionAnswers there is NO "SELECT DISTINCT * FROM (...) z"
# wrapper. The strCmd is executed directly — this matches the old system.
print(f"\nSQL built for site {p_site_code}. Approximate length: {len(strCmd):,} chars.")

mssparkutils.notebook.exit(strCmd)
```

---

## 16. Step 11 — Create the Silver Notebook: nb_answersig_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_answersig_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` to the Lakehouse panel
4. The notebook has **three cells**

---

## 17. Step 12 — Silver Notebook Cell 1: Load Bronze and Prepare Source

### Key differences from FormQuestionAnswers Cell 1

| Aspect | FormQuestionAnswers | FormAnswerSignatures |
|---|---|---|
| PK columns | 7 | **4**: `SiteCode`, `FormName`, `FormId`, `ClientId` |
| ClientId storage | Raw (can be negative) | **`abs()` always** — stored as positive; negative original → RowState=0 |
| RowChkSum | Not present | Column exists, but old source query does not generate it; remains null |

### Cell 1 Code

```python
from pyspark.sql.functions import col, current_timestamp, row_number, when, lit, upper, abs as spark_abs
from pyspark.sql.window import Window
from datetime import datetime, timedelta

# ── Parameters ───────────────────────────────────────────────────────
try: p_ingest_run_id
except NameError: p_ingest_run_id = "test-run-001"

try: p_lookback_days
except NameError: p_lookback_days = 30

try: p_reload
except NameError: p_reload = False

bronze_table = "bhg_bronze.Forms.br_tblFormAnswerSig"
silver_table = "bhg_silver.pats.sl_tblFormAnswerSignatures"
f2p_table    = "bhg_silver.ctrl.Forms2Process"

print(f"Run ID: {p_ingest_run_id}")

# Compute wrkdt for pre-pass (Cell 2)
if p_reload:
    wrkdt = datetime(2010, 1, 1)
else:
    wrkdt = datetime.now() - timedelta(days=int(p_lookback_days))

# Load Forms2Process for pre-pass
f2p_df = (
    spark.table(f2p_table)
    .filter("Enabled = true AND RowState = true")
    .select("FormName", "DateFilterEnabled")
    .toPandas()
)
# Note: non_date_filtered_forms is NOT computed here — Cell 2 re-derives the
# exact set it needs (with TP-* normalization) right before the pre-pass resets.
# Computing it here would be dead code.

# Read Bronze for this run
bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)
bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze rows found for ingest_run_id = {p_ingest_run_id}")

# Deduplicate on 4-column PK — keep latest _extracted_at
# FormId normalised to UPPER (mirrors a.FormId.ToUpper() in old EF path)
pk_cols = ["SiteCode", "FormName", "FormId", "ClientId"]

w = Window.partitionBy(*pk_cols).orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .withColumn("FormId", upper(col("FormId")))
    # ClientId: store as ABS, track original sign for RowState
    .withColumn("_orig_client_id_sign", when(col("ClientId") < 0, lit(-1)).otherwise(lit(1)))
    .withColumn("ClientId", spark_abs(col("ClientId")))     # always positive in Silver
    .withColumn("_rn", row_number().over(w))
    .where(col("_rn") == 1)
    .drop("_rn")
    # RowState: 0 if original ClientId was negative OR IsDeleted=1
    # Note: negative-sign check uses _orig_client_id_sign because ClientId is now ABS
    .withColumn(
        "RowState",
        when(
            (col("IsDeleted") == 1) | (col("_orig_client_id_sign") == -1),
            lit(0)
        ).otherwise(lit(1))
    )
    .drop("_orig_client_id_sign")
    .withColumn("LastModAt",         current_timestamp())
    .withColumn("silver_updated_at", current_timestamp())
    # Silver mirrors the old BHG_DR answer-signature table shape. These are
    # Bronze lineage/control columns only and are not part of the old target.
    .drop("_site_code", "_source_database", "_ingest_run_id", "_extracted_at", "_lookback_date", "IsDeleted")
)

src_count = src_df.count()
print(f"Deduplicated source rows: {src_count}")
src_df.createOrReplaceTempView("vw_answersig_current_run")

# First-ever run: create Silver table
if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write.format("delta").mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created Silver table: {src_count} rows inserted.")
else:
    print(f"Silver table exists: {silver_table}")
```

---

## 18. Step 13 — Silver Notebook Cell 2: Pre-Pass RowState Reset

### Key difference from FormQuestionAnswers: committed immediately

In the old EF code, `SaveAnswerSignatures` calls `db.SaveChanges()` **right after the pre-pass loop** — before any upsert begins. This means the soft-resets are already persisted in Azure before the MERGE starts. In this Fabric notebook, Cell 2 (the pre-pass) runs as a separate Delta update operation before Cell 3 (the MERGE), which achieves the same committed-before-merge behaviour.

The pre-pass rules, including the critical TP-* normalization:

| Condition | Reset Applied |
|---|---|
| `DateFilterEnabled = 0` | Unconditional: `SET RowState = 0` for ALL rows of this form at affected sites |
| `DateFilterEnabled = 1` | Date-gated: reset where `CreatedOn >= wrkdt OR UpdatedOn >= wrkdt` AND `RowState = 1` |
| Form NOT found in config | Date-gated (same as above) |
| Silver FormName starts with `TP-` | **Normalized to `"Treatment Plan"`** before looking up DateFilterEnabled in Forms2Process — this is the critical mapping the old C# performs |

**Why TP- normalization matters:** Silver holds rows with FormName like `"TP-Initial"`, `"TP-Annual"`, `"TP-Quarterly"` etc. (from the `tblTP17REVIEW` UNION). Forms2Process has a **single** `"Treatment Plan"` entry that controls `DateFilterEnabled` for all of them. Without normalization, `"TP-Initial"` would never match any Forms2Process entry and would fall through to the date-gated path — which might be correct, but depends on the `"Treatment Plan"` entry's `DateFilterEnabled` value. The old C# is explicit: map first, then look up.

### Cell 2 Code

```python
from delta.tables import DeltaTable
from pyspark.sql.functions import col, lit

silver_table = "bhg_silver.pats.sl_tblFormAnswerSignatures"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)
wrkdt_str = wrkdt.strftime("%Y-%m-%d")

# Identify sites processed in this run
sites_this_run = (
    spark.table("bhg_bronze.Forms.br_tblFormAnswerSig")
    .where(col("_ingest_run_id") == p_ingest_run_id)
    .select("_site_code").distinct()
    .rdd.flatMap(lambda x: x).collect()
)
print(f"Sites in this run: {sites_this_run}")

if not sites_this_run:
    # Cell 1 guards bronze_count == 0, but _site_code being null across all rows
    # could still produce an empty list here, making site_filter = "" and producing
    # invalid SQL in the Delta update conditions below.
    raise Exception(f"No _site_code values found in Bronze for run {p_ingest_run_id}")

site_filter = " OR ".join([f"SiteCode = '{s}'" for s in sites_this_run])

# ── TP-* normalization ────────────────────────────────────────────────
# Old C#: if (d.FormName.StartsWith("TP-")) formname = "Treatment Plan"
# Silver rows from tblTP17REVIEW carry FormName like "TP-Initial", "TP-Annual".
# Forms2Process has a single "Treatment Plan" entry that controls DateFilterEnabled
# for ALL of them. We read that entry's DateFilterEnabled here.
tp_row = f2p_df[f2p_df["FormName"] == "Treatment Plan"]
tp_date_filtered = True  # default: treat TP-* as date-filtered if not found
if not tp_row.empty:
    tp_date_filtered = bool(tp_row.iloc[0]["DateFilterEnabled"])

print(f"Treatment Plan DateFilterEnabled: {tp_date_filtered}")

# ── Build the set of non-date-filtered FormNames ──────────────────────
# Excludes "Treatment Plan" — handled separately via the TP-* LIKE pattern.
non_date_filtered_forms_no_tp = set(
    f2p_df[(f2p_df["DateFilterEnabled"] == False) & (f2p_df["FormName"] != "Treatment Plan")]
    ["FormName"].tolist()
)

# ── Unconditional reset ───────────────────────────────────────────────
# Applies to: non-date-filtered standard forms, and TP-* if "Treatment Plan"
# is also non-date-filtered.
uncond_parts = []
if non_date_filtered_forms_no_tp:
    ndf_list = ", ".join([f"'{f}'" for f in non_date_filtered_forms_no_tp])
    uncond_parts.append(f"FormName IN ({ndf_list})")
if not tp_date_filtered:
    # "Treatment Plan" has DateFilterEnabled=False → reset ALL TP-* rows
    uncond_parts.append("FormName LIKE 'TP-%'")

if uncond_parts:
    uncond_condition = " OR ".join(uncond_parts)
    silver_delta.update(
        condition=f"({site_filter}) AND ({uncond_condition}) AND RowState = 1",
        set={"RowState": lit(0)}
    )
    print(f"Unconditional pre-pass reset applied.")

# ── Date-gated reset ──────────────────────────────────────────────────
# Applies to: date-filtered forms, unknown forms (not in config),
# and TP-* rows when "Treatment Plan" has DateFilterEnabled=True.
# The unconditionally reset rows are already at RowState=0 — re-applying
# RowState=0 to them is a safe no-op (WHERE RowState=1 already excluded them
# after the first update committed).
silver_delta.update(
    condition=(
        f"({site_filter})"
        f" AND RowState = 1"
        f" AND (CreatedOn >= '{wrkdt_str}'"
        f"      OR COALESCE(UpdatedOn, CreatedOn) >= '{wrkdt_str}')"
    ),
    set={"RowState": lit(0)}
)
print(f"Date-gated pre-pass reset applied (wrkdt={wrkdt_str}).")

# Pre-pass IS committed here (Cell 2 completes before Cell 3 starts).
# This matches SaveAnswerSignatures which calls db.SaveChanges() immediately
# after the pre-pass loop — before any upsert begins.
print("Pre-pass committed. Proceeding to MERGE.")
```

---

## 19. Step 14 — Silver Notebook Cell 3: Delta MERGE Into Silver

### The 4-column composite PK

```
SiteCode + FormName + FormId(UPPER) + ClientId
```

This identifies one form instance for one patient at one clinic. Simpler than FormQuestionAnswers (7 columns) because signatures are one row per form, not one row per question.

**Note on ClientId matching:** The Bronze source has ClientId already converted to `abs()` by Cell 1. The Silver MERGE matches on the stored (positive) ClientId — consistent on both sides.

### RowChkSum behaviour

`RowChkSum` is a column on the destination entity `TblDboFormAnswerSignatures` in the old C# EF model, and the EF save method has a mapping case for it. **However, the old BHGTaskRunner Program.cs never includes `RowChkSum` in the `strCmd` SELECT it builds** — there is no `CHECKSUM(...)` expression anywhere in the AnswerSignatures query. The EF column switch can map it if the DataTable has that column, but since the source SQL never generates it, `RowChkSum` is never populated at all.

In Fabric: `RowChkSum` is **not computed and not present** in Bronze. The Silver schema may define the column as nullable; it will remain `null` for all rows. The MERGE always updates all signature date columns unconditionally regardless — the commented-out guard in the old code (`// if (dbAns.RowChkSum != a.RowChkSum)`) was never exercised because RowChkSum was never available.

### Cell 3 Code

```python
from delta.tables import DeltaTable
from pyspark.sql.functions import col, current_timestamp

silver_table = "bhg_silver.pats.sl_tblFormAnswerSignatures"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = src_df.columns

# Update all mutable columns on a match.
# silver_created_at is immutable — never overwritten.
# SiteCode, FormName, FormId, ClientId are identity columns — not updated.
identity_cols = {"silver_created_at", "SiteCode", "FormName", "FormId", "ClientId"}
update_cols   = [c for c in src_cols if c not in identity_cols]

update_set = {c: f"src.{c}" for c in update_cols}
update_set["silver_updated_at"] = "current_timestamp()"
update_set["LastModAt"]         = "current_timestamp()"

insert_values = {c: f"src.{c}" for c in src_cols}
insert_values["silver_created_at"] = "current_timestamp()"

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # 4-column composite PK
        """
        tgt.SiteCode = src.SiteCode
        AND tgt.FormName <=> src.FormName
        AND tgt.FormId   = src.FormId
        AND tgt.ClientId = src.ClientId
        """
    )
    # Matched: full update — RowChkSum comparison guard is commented out
    # in the old system; we always update all signature date columns.
    .whenMatchedUpdate(set=update_set)
    # Not matched: new form signature record
    .whenNotMatchedInsert(values=insert_values)
    .execute()
)

# Mirrors [pats].[MergeFormSignaturesPeriodicReassessments]:
# after the full-site Periodic Reassessment merge, any older active
# Periodic Reassessment row for those processed sites that was not present
# in the current source set is marked inactive.
current_pr_df = (
    src_df
    .where(col("FormName") == "Periodic Reassessment")
    .select("SiteCode", "FormName", "FormId", "ClientId")
    .distinct()
)

pr_sites = [r["SiteCode"] for r in current_pr_df.select("SiteCode").distinct().collect()]

if pr_sites:
    silver_pr_df = (
        spark.table(silver_table)
        .where((col("FormName") == "Periodic Reassessment") & (col("SiteCode").isin(pr_sites)) & (col("RowState") == 1))
        .select("SiteCode", "FormName", "FormId", "ClientId")
    )

    stale_pr_df = (
        silver_pr_df.alias("tgt")
        .join(
            current_pr_df.alias("src"),
            on=["SiteCode", "FormName", "FormId", "ClientId"],
            how="left_anti"
        )
        .distinct()
    )

    stale_pr_count = stale_pr_df.count()
    if stale_pr_count:
        (
            silver_delta.alias("tgt")
            .merge(
                stale_pr_df.alias("src"),
                """
                tgt.SiteCode = src.SiteCode
                AND tgt.FormName = src.FormName
                AND tgt.FormId   = src.FormId
                AND tgt.ClientId = src.ClientId
                """
            )
            .whenMatchedUpdate(
                set={
                    "RowState": "0",
                    "silver_updated_at": "current_timestamp()"
                }
            )
            .execute()
        )
    print(f"Periodic Reassessment stale-row reset complete: {stale_pr_count} rows set inactive.")
else:
    print("No Periodic Reassessment source rows in this run; stale-row reset skipped.")

print("Silver MERGE for FormAnswerSignatures completed successfully.")
```

---

## 20. End-to-End Flow Summary

```
TRIGGER: Pipeline runs with parameters:
  p_ingest_run_id = "ANSWERSIG-2026-05-11-001"
  p_lookback_days = 30
  p_reload        = false
  p_sites         = [ {ColoradoSpringsV5}, {B01}, {B02}, ... ]

STEP 1 — ForEach begins: item = ColoradoSpringsV5

STEP 2 — lkp_check_answersig_table (Lookup)
  → SELECT answersig_exists=COUNT(1) FROM [SAMMS-ColoradoSpringsV5].sys.tables
     WHERE name = 'answersignature'
  → Result: { answersig_exists: 1 } → True branch
  → Result: { answersig_exists: 0 } → False branch (empty) → site skipped

STEP 3 — lkp_get_existing_tables (Lookup, True branch)
  → SELECT name FROM [SAMMS-ColoradoSpringsV5].sys.tables
  → Result: [{"name":"Form"}, {"name":"AnswerSignature"}, ...]

STEP 4 — nb_answersig_build_site_sql (Notebook, True branch)
  READS: bhg_silver.ctrl.Forms2Process  (NO SAMMS connection)
  BUILDS base UNION SQL (correct structure — sig subqueries in outer SELECT):
    SELECT DISTINCT
      SiteCode, FormName, FormId, ClientId, CreatedOn, UpdatedOn, IsDeleted,
      -- 9 correlated subqueries in SELECT list, referencing x.FormId:
      CompletedBySignatureSignatureDate = (SELECT TOP 1 CASE WHEN Sign IS NULL
          THEN '1/1/1900' ELSE [DateTime] END FROM AnswerSignature
          WHERE FormId = x.FormId AND DateField = '...' ORDER BY [DateTime] DESC),
      ... [8 more subqueries] ...
    FROM (
      -- UNION 1: All active forms (NO date filter — f.IsDeleted alias)
      SELECT SiteCode, ft.FormName, f.id AS FormId, f.ClientId,
             f.CreatedOn, f.UpdatedOn,
             IsDeleted = CASE WHEN ISNULL(f.IsDeleted,0)=0 ... END
      FROM dbo.Form f JOIN ...
      UNION
      -- UNION 2: Deleted forms (same SELECT, adds WHERE f.Isdeleted=1)
      SELECT ... WHERE f.Isdeleted = 1
    ) x
    , CounselorSignatureSignatureDate   = (SELECT TOP 1 ... DateField IN (...) ...)
    , ... [7 more subqueries] ...

  LOOPS Forms2Process (no ORDER BY):
    tblORDERREQ → tblORDERREQ case → Level Justification UNION
    tblTP17REVIEW → tblTP17REVIEW case → Treatment Plan UNION (direct sig cols)
    AdmissionAssessment → default case → aas. join for Patient/Provider/Staff/Supervisor
    NewAdmissionAssessment → default case → b. join for all sig cols
    SF_PatientPreAdmission → default case → self-join, LAB-site StaffSig=null
    FinancialHardshipApplication → default case → a.CltID as ClientId
    All other tables → default case → a. alias for sig cols
    Missing tables → SKIP

  RETURNS: full SQL string via mssparkutils.notebook.exit()
           (NO SELECT DISTINCT wrapper — unlike FormQA)

STEP 5 — cp_answersig_to_bronze (Copy)
  Source: SAMMS SQL Server (via Fabric gateway)
    query = @activity('nb_answersig_build_site_sql').output.result.exitValue
  Sink: bhg_bronze.Forms.br_tblFormAnswerSig  (APPEND)
  → N rows written, tagged with _ingest_run_id

STEP 7 — ForEach moves to next site → repeats Steps 2-6

... continues for all clinics ...

STEP 8 — ForEach complete.

STEP 9 — nb_answersig_bronze_to_silver (Notebook)

  CELL 1:
  → Read Bronze WHERE _ingest_run_id = "ANSWERSIG-2026-05-11-001"
  → Deduplicate on 4-column PK (keep latest _extracted_at)
  → Normalise FormId to UPPER
  → ClientId = ABS() — always stored as positive; negative original → RowState=0
  → Compute RowState (0 if original ClientId<0 or IsDeleted=1, else 1)
  → Load Forms2Process for pre-pass metadata

  CELL 2 (Pre-pass — COMMITTED BEFORE MERGE):
  → Identify sites in this run
  → Non-date-filtered forms: UPDATE SET RowState=0 unconditionally
  → Date-filtered + TP-* forms: UPDATE SET RowState=0 WHERE date >= wrkdt
  → Cell 2 completes → soft-resets are persisted before MERGE starts

  CELL 3 (MERGE):
  → Delta MERGE src_df → bhg_silver.pats.sl_tblFormAnswerSignatures
     MATCH on 4-col PK (SiteCode + FormName + FormId + ClientId)
     whenMatched    → full update (no RowChkSum gate — always refresh)
     whenNotMatched → insert with silver_created_at

DONE: Silver is current. 9 signature dates refreshed for all form instances
      seen this run. Soft-deleted forms remain at RowState=0.
```

---

## 21. Why the Base Query Has No Date Filter

```python
# In the old system:
# " where (f.CreatedOn >= '" + wrkdt + "' or isnull(f.UpdatedOn, f.CreatedOn) >= '" + wrkdt + "')
# /*')*/ union select ..."
# ↑ The WHERE clause is COMMENTED OUT — note the /*')*/ that closes
#   the commented-out string concatenation.
```

The Form/AnswerSignature base query intentionally pulls **all forms regardless of date**. The reason is that a form's signature dates change independently of the form's creation date. A form created 2 years ago might be signed today for the first time — or re-signed after a correction. If the base query had a date filter, those late-signing events would be missed entirely.

**How this affects the pipeline:**
- The base query could return a very large number of rows for sites with many historical forms
- The `wrkdt` parameter is still meaningful — it controls the **Forms2Process custom table** UNIONs that do have `DateFilterEnabled` guards
- For the base Form query, every form in the system is pulled every run — Bronze will accumulate all rows, and the Silver MERGE only updates what changed (all 9 sig dates per form are refreshed)

This also explains why this pipeline may produce significantly more Bronze rows per site than FormQuestionAnswers.

---

## 22. The 9 Signature Columns — AnswerSignature Correlated Subquery Pattern

For the base Form query, each of the 9 signature dates is resolved by a correlated subquery against the `AnswerSignature` table:

```sql
CompletedBySignatureSignatureDate = (
    SELECT TOP 1
        CASE WHEN Sign IS NULL THEN '1/1/1900' ELSE [DateTime] END
    FROM AnswerSignature
    WHERE FormId = x.FormId
      AND DateField = 'CompletedBySignatureSignatureDate'
    ORDER BY [DateTime] DESC
)
```

**Three-state encoding:**

| AnswerSignature state | Returned value | Meaning |
|---|---|---|
| Row exists, `Sign IS NOT NULL` | The actual signature date | Form was signed by this role |
| Row exists, `Sign IS NULL` | `'1/1/1900'` | Slot exists but form not yet signed by this role |
| No row in AnswerSignature | `NULL` | This signature slot was never submitted for this form |

The sentinel `'1/1/1900'` means *"slot present but unsigned"* — different from `NULL` meaning *"slot never submitted"*. This is why the old system uses a length > 6 guard when parsing dates: `"1/1/1900"` is 8 characters and is a valid storable value, while empty strings or null should be skipped.

**CounselorSignatureSignatureDate — dual DateField match:**

```sql
CounselorSignatureSignatureDate = (
    SELECT TOP 1 CASE WHEN Sign IS NULL THEN '1/1/1900' ELSE [DateTime] END
    FROM AnswerSignature
    WHERE FormId = x.FormId
      AND (DateField = 'CounselorSignatureSignatureDate'
        OR DateField = 'CounselorSignatureDate')
    ORDER BY [DateTime] DESC
)
```

This OR condition handles SAMMS versions that used the older column name `CounselorSignatureDate` before it was standardised to `CounselorSignatureSignatureDate`.

---

## 23. The Forms2Process Loop — 3 Switch Cases

| Case | FormID Structure | ClientId | Sig date source | Notes |
|---|---|---|---|---|
| `tblORDERREQ` | `9-1-ABS(cltID)-ReqNum-1` | `cltID` raw | Direct columns: `DrSigDt`/`SigNurseDt`, `sigCoordinatorDt` | Note trailing `-1` vs FormQA's `-` |
| `tblTP17REVIEW` | `8-1-ABS(tprCLTID)-tpRID-tprTPID` | `tprCLTID` **raw** (NOT ABS) | Direct cols: `tprCLIRNTSIGDate`, `tprDRSIGDate`, `tprCOUNSSIGDate`, `tprSUPERSIGDate` | ClientId stored raw here; EF layer takes ABS during Silver merge |
| default | Level A formula per table | Level A per table | Level B per table using `a.`, `aas.`, or `b.` alias | 65 standard + special join tables |

---

## 24. Default Case — Level A: FormID and ClientId Per Table

The default case dispatches FormID construction and ClientId source based on the specific table name:

| TableName | FormID Formula | ClientId Source |
|---|---|---|
| `SF_PatientPreAdmission` | `Prefix-ISNULL(pa.PatientID,0)-ISNULL(a.ParentPreAdmId,0)-ISNULL(a.id,0)` | `ISNULL(pa.PatientID, 0)` |
| `SF_DataForm` | `Prefix-ISNULL(pa.PatientID,0)-ISNULL(a.PreAdmId,0)-ISNULL(a.id,0)` | `ISNULL(pa.PatientID, 0)` |
| `SF_UnderstandingOfTreatment` | `Prefix-ISNULL(pa.PatientID,0)-a.PreAdmId-a.id` | `ISNULL(pa.PatientID, 0)` |
| `InsuranceBenefitVerification` | `Prefix-ISNULL(pa.PatientID,0)-a.PreAdmId-a.id` | `ISNULL(pa.PatientID, 0)` |
| `FinancialHardshipApplication` | `Prefix-ISNULL(a.CltID,0)-a.PreAdmId-a.id` | `ISNULL(a.CltID, 0)` |
| `xNewAdmissionAssessment` | `Prefix-ISNULL(b.ClientId,0)-b.PreAdmId-b.id` | `ISNULL(b.ClientId, 0)` |
| *(all other tables — default sub-case)* | `Prefix-ISNULL(a.ClientId,0)-a.PreAdmId-a.id` | `ISNULL(a.ClientId, 0)` |

**Why `ISNULL(..., 0)`?** The `FormID` is a varchar concatenation. A `NULL` ClientId or PreAdmissionId would produce a `NULL` FormID, breaking the PK. Defaulting to `0` preserves the row with a deterministic identity.

---

## 25. Default Case — Level B: Signature Date Columns Per Table

For each of the 9 signature columns, the source column name comes from `Forms2Process` (the `CompletedBy`, `Counselor`, `Doctor`, `MedicalProvider`, `Patient`, `Provider`, `Requestor`, `Staff`, `Supervisor` columns). If the column is `null` → the signature column is `null`. If populated → `CASE WHEN CONVERT(date, {alias}.{col}) IS NULL THEN '1900-01-01' ELSE CONVERT(date, {alias}.{col}) END`.

**Alias selection per table:**

| Table | Patient | Provider | Staff | Supervisor | Other sig cols |
|---|---|---|---|---|---|
| `AdmissionAssessment` | `aas.` | `aas.` | `aas.` | `aas.` | `a.` |
| `NewAdmissionAssessment` | `b.` | `b.` | `b.` | `b.` | `b.` |
| `SF_PatientPreAdmission` (site=LAB) | `a.` | `a.` | **null** | `a.` | `a.` |
| All others | `a.` | `a.` | `a.` | `a.` | `a.` |

**RequestorSignatureDate** never uses a table-specific alias — it is always `a.` regardless of table.

---

## 26. AdmissionAssessment — The aas Join Exception

For `AdmissionAssessment`, the Patient, Provider, Staff, and Supervisor signature dates live in a **separate summary table** (`AdmissionAssessmentSummary`), not in the main `AdmissionAssessment` row. The query adds an extra INNER JOIN:

```sql
INNER JOIN [dbo].[AdmissionAssessmentSummary] aas
    ON a.Id = aas.AdmissionAssessmentId
   AND a.PreAdmissionId = aas.PreAdmissionId
```

Then in the sig date columns:
```sql
PatientSignatureDate =
    CASE WHEN CONVERT(date, aas.AdmissionAssessmentPatientSignatureDate) IS NULL
         THEN '1900-01-01'
         ELSE CONVERT(date, aas.AdmissionAssessmentPatientSignatureDate)
    END
```

The `aas.` column names come from `Forms2Process.Patient` (e.g., `AdmissionAssessmentPatientSignatureDate`), `Forms2Process.Staff`, etc.

If the `AdmissionAssessmentSummary` table does not exist in this SAMMS version, the entire `AdmissionAssessment` UNION block will fail. The `existing_tables` probe protects against this: if `admissionassessment` is in `existing_tables` but `admissionassessmentsummary` is not, consider adding a separate probe for the summary table.

---

## 27. NewAdmissionAssessment — The b Join Exception

For `NewAdmissionAssessment`, all 9 signature dates come from `NewAdmissionAssessmentASAMDimension6` (alias `b`). The query adds:

```sql
INNER JOIN [dbo].[NewAdmissionAssessmentASAMDimension6] b
    ON a.preadmissionID = b.preadmissionID
   AND a.ID = b.NewAdmissionAssessmentFormId
```

All sig date column references use `b.{xf.SigColumn}` regardless of which sig type.

**Note on the `xNewAdmissionAssessment` Level A sub-case:** The code in `Program.cs` has a case for `xNewAdmissionAssessment` (with an `x` prefix) in the Level A FormID switch. This is the `b.` ClientId path. The actual table joined is `NewAdmissionAssessment` (without `x`). The `x` prefix in the switch case is a historical naming artefact — the table itself is `NewAdmissionAssessment`.

---

## 28. The 4-Column Primary Key and RowChkSum Behaviour

**Composite PK:**
```
SiteCode + FormName + FormId(UPPER) + ClientId(ABS)
```

One row per form instance, per patient, per clinic. This is simpler than FormQuestionAnswers (7 columns) because this table holds one row per form, not one row per question.

**RowChkSum:**
- `RowChkSum` is a column on the old C# EF entity `TblDboFormAnswerSignatures`
- The EF save method has a mapping case: `case "rowchksum": a.RowChkSum = int.Parse(value)`
- **However, the runner SQL (`strCmd` in Program.cs) never generates a `RowChkSum` column** — no `CHECKSUM(...)` expression is present in the AnswerSignatures query. The EF mapping case therefore never fires.
- The comparison guard was also commented out: `// if (dbAns.RowChkSum != a.RowChkSum)` — and could not have been exercised even if it were active.

In Fabric:
- `RowChkSum` is **not computed** by the SQL builder notebook — do not add it to Bronze
- Define the column as **nullable** in the Silver schema if schema parity is needed
- The MERGE always updates all signature date columns unconditionally — no checksum gate

---

## 29. ClientId — Always Stored as Absolute Value

```python
# Old system:
a.ClientId = Math.Abs(int.Parse(value))   # always positive in Silver
if originalValue < 0: a.RowState = 0       # negative → soft-deleted

# Fabric Cell 1:
.withColumn("_orig_sign", when(col("ClientId") < 0, lit(-1)).otherwise(lit(1)))
.withColumn("ClientId", spark_abs(col("ClientId")))  # always positive
.withColumn("RowState", when((col("IsDeleted")==1) | (col("_orig_sign")==-1), 0).otherwise(1))
```

**Why this matters for Silver MERGE matching:**

The Silver table stores `ClientId` as a positive integer. The Bronze source (after Cell 1 transforms it) also has positive `ClientId`. The MERGE matches on equal positive values — consistent on both sides.

**Why `IsDeleted = "0"` does not guarantee `RowState = 1`:**

Unlike FormQuestionAnswers where `IsDeleted = "0"` always forces `RowState = 1`, here:
- `IsDeleted = "0"` AND `ClientId >= 0` → `RowState = 1`
- `IsDeleted = "0"` AND original `ClientId < 0` → `RowState = 0` (negative client = void record)

Both signals must agree to produce an active row.

---

## 30. Pre-Pass Differences — Committed Immediately

The key operational difference:

| | FormQuestionAnswers | FormAnswerSignatures |
|---|---|---|
| When pre-pass saves | Deferred — `SaveChanges()` was commented out after pre-pass; committed together with upserts | **Immediately** — `db.SaveChanges()` called right after pre-pass loop, before upsert begins |
| Cell ordering in Fabric notebook | Cell 2 (pre-pass) → Cell 3 (MERGE) — both update Silver | Same — Cell 2 commits first, Cell 3 MERGEs |
| Effect on re-activation | Pre-pass reset and MERGE update happen in one Delta commit pass | Pre-pass reset is a separate committed Delta write; MERGE is a second committed write |

In Fabric, this difference is implemented naturally: Cell 2 runs `silver_delta.update(...)` and completes before Cell 3 runs `silver_delta.merge(...)`. Each cell is its own Delta transaction.

---

## 31. Troubleshooting Guide

### Site skipped — no AnswerSignature table

**Symptom:** Site produces no Bronze rows; `answersig_exists = 0` in Lookup 1.  
**Cause:** This clinic's SAMMS database does not have the `AnswerSignature` table (older version).  
**Action:** Expected behaviour for older SAMMS versions. No intervention needed unless the site was recently upgraded.

---

### `lkp_get_existing_tables` or Copy activity fails with connection error

**Cause:** On-prem gateway unreachable for this site's SAMMS server.  
**Fix:** Verify Fabric connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` is active. Confirm `source_database` in `p_sites` exactly matches the actual SQL Server database name.

---

### `nb_answersig_build_site_sql` fails: `Forms2Process` empty or missing sig-date columns

**Symptom:** Notebook raises an exception or all sig date columns generate `null`.  
**Cause 1:** `bhg_silver.ctrl.Forms2Process` has not been loaded with the sig-date columns (`CompletedBy`, `Counselor`, `Doctor`, etc.).  
**Fix 1:** Re-load `Forms2Process` including all 9 sig-date columns from the old Azure BHG_DR database:
```sql
SELECT * FROM ctrl.Forms2Process WHERE Enabled=1 AND RowState=1
```
**Cause 2:** `col_val()` helper returning `None` for all sig-date columns — columns exist in the table but all rows have `NULL` values for a given form.  
**Fix 2:** This is valid — forms without a specific sig type (e.g., no Doctor signature on a consent form) correctly produce `null` for that column.

---

### Copy activity produces 0 rows

**Cause:** The base Form query is pulling ALL forms (no date filter) but this SAMMS database is empty or has no Form records.  
**Fix:** Verify the SAMMS database is the correct one by running `SELECT COUNT(*) FROM dbo.Form` directly against it.

---

### Silver MERGE is slow

**Fix:** Z-order the Silver table:
```python
spark.sql("""
    OPTIMIZE bhg_silver.pats.sl_tblFormAnswerSignatures
    ZORDER BY (SiteCode, FormName, ClientId)
""")
```

---

### Signature dates all showing as `1/1/1900` or `1900-01-01`

**Symptom:** All 9 sig date columns in Silver are `1900-01-01` for forms that should have real dates.  
**Cause 1 (base query):** The `AnswerSignature` table exists but has no rows (Sign IS NULL for all entries).  
**Cause 2:** The correlated subquery is not matching any rows. Verify `FormId` in `AnswerSignature` matches `f.id` (the base query's `x.FormId`).  
**Diagnosis query against SAMMS:**
```sql
SELECT TOP 10 FormId, DateField, Sign, [DateTime]
FROM AnswerSignature
ORDER BY [DateTime] DESC
```
If `Sign` is always NULL, forms have been submitted but not signed. `1900-01-01` is the correct sentinel in that case.

---

### FormId matching fails — duplicate rows in Silver (case mismatch)

**Symptom:** The same form appears twice in Silver with different `FormId` casing.  
**Fix:** One-time normalisation:
```python
spark.sql("UPDATE bhg_silver.pats.sl_tblFormAnswerSignatures SET FormId = UPPER(FormId)")
```

---

### AdmissionAssessment fails with join error

**Symptom:** Copy activity fails for a site with a message about `AdmissionAssessmentSummary` not found.  
**Cause:** `AdmissionAssessment` table exists but `AdmissionAssessmentSummary` does not in this SAMMS version.  
**Fix:** In the SQL builder notebook, add a check for `admissionassessmentsummary` in `existing_tables` before generating the `AdmissionAssessment` UNION block. If the summary table is absent, skip the entire AdmissionAssessment UNION for that site.

---

### tblTP17REVIEW — ClientId negative values stored in Silver

**Symptom:** Some `tblTP17REVIEW` rows have negative `ClientId` values in Silver.  
**Cause:** The old system stores `tprCLTID` **raw** (not ABS) for tblTP17REVIEW. Cell 1 of the Silver notebook applies `abs()` to ClientId universally, which means the stored value in Silver will be positive. The sign is captured in `RowState = 0`. This is the correct and expected behaviour.

---

*End of Implementation Guide*


