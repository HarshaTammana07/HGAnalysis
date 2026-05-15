# FormQuestionAnswers — Microsoft Fabric ETL Pipeline Implementation Guide

**Pipeline Name:** FormQuestionAnswers Bronze → Silver ETL  
**Data:** Clinical form question-answer records from 75 SAMMS form tables per clinic database  
**Destination:** Microsoft Fabric Lakehouse (Bronze + Silver layers)  
**Silver Target:** `bhg_silver.pats.sl_tblFormQuestionAnswers`  
**Control Dependency:** `bhg_silver.ctrl.tbl_Forms2Process` (Fabric lakehouse table — read only from Spark, never from SAMMS)  
**Author Reference:** `FormQuestionAnswers_Tables_Columns_Logic.md`, `Forms2Process_Bronze_Extraction_Columns.md`, `BHGTaskRunner/Program.cs`, `SaveFormQAData.cs`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [Why This Design Uses a SQL Builder Notebook + Copy Activity](#3-why-this-design-uses-a-sql-builder-notebook--copy-activity)
4. [Prerequisites — What You Need Before Starting](#4-prerequisites--what-you-need-before-starting)
5. [Pipeline Parameters and Variables — Define These First](#5-pipeline-parameters-and-variables--define-these-first)
6. [Step 1 — Create the Data Pipeline in Fabric](#6-step-1--create-the-data-pipeline-in-fabric)
7. [Step 2 — Add the ForEach Activity](#7-step-2--add-the-foreach-activity)
8. [Step 3 — Inside ForEach: Lookup 1 — Check Form Table Exists](#8-step-3--inside-foreach-lookup-1--check-form-table-exists)
9. [Step 4 — Inside ForEach: IfCondition — Gate on Form Table](#9-step-4--inside-foreach-ifcondition--gate-on-form-table)
10. [Step 5 — Inside IfCondition True Branch: Lookup 2 — Get All Existing Tables](#10-step-5--inside-ifcondition-true-branch-lookup-2--get-all-existing-tables)
11. [Step 6 — Inside True Branch: Notebook Activity — SQL Builder](#11-step-6--inside-true-branch-notebook-activity--sql-builder)
12. [Step 7 — Inside True Branch: Set Variable — Capture Built SQL](#12-step-7--inside-true-branch-set-variable--capture-built-sql)
13. [Step 8 — Inside True Branch: Copy Activity — Extract to Bronze](#13-step-8--inside-true-branch-copy-activity--extract-to-bronze)
14. [Step 9 — Add the Silver Notebook Activity (After ForEach)](#14-step-9--add-the-silver-notebook-activity-after-foreach)
15. [Step 10 — Create the SQL Builder Notebook: nb_forms_build_site_sql](#15-step-10--create-the-sql-builder-notebook-nb_forms_build_site_sql)
16. [Step 11 — Create the Silver Notebook: nb_forms_bronze_to_silver](#16-step-11--create-the-silver-notebook-nb_forms_bronze_to_silver)
17. [Step 12 — Silver Notebook Cell 1: Load Bronze and Prepare Source](#17-step-12--silver-notebook-cell-1-load-bronze-and-prepare-source)
18. [Step 13 — Silver Notebook Cell 2: Pre-Pass RowState Reset](#18-step-13--silver-notebook-cell-2-pre-pass-rowstate-reset)
19. [Step 14 — Silver Notebook Cell 3: Delta MERGE Into Silver](#19-step-14--silver-notebook-cell-3-delta-merge-into-silver)
20. [End-to-End Flow Summary](#20-end-to-end-flow-summary)
21. [How the Dynamic SQL Generation Works (Forms2Process)](#21-how-the-dynamic-sql-generation-works-forms2process)
22. [The Base Query — Form + FormTemplate + Question + Answer](#22-the-base-query--form--formtemplate--question--answer)
23. [The Forms2Process Switch Cases — All 9 Variants](#23-the-forms2process-switch-cases--all-9-variants)
24. [IsDeleted — Legacy Nullable Pass-Through](#24-isdeleted--legacy-nullable-pass-through)
25. [RowState Pre-Pass — Why It Exists and How It Works](#25-rowstate-pre-pass--why-it-exists-and-how-it-works)
26. [The 7-Column Primary Key — No RowChkSum](#26-the-7-column-primary-key--no-rowchksum)
27. [Lookback Window — 30 Days and ReferralForm Exception](#27-lookback-window--30-days-and-referralform-exception)
28. [tblTP17REVIEW — Treatment Plan Special Case](#28-tblt-p17review--treatment-plan-special-case)
29. [tblORDERREQ — Level Justification Special Case](#29-tblorderreq--level-justification-special-case)
30. [Troubleshooting Guide](#30-troubleshooting-guide)

---

## 1. Architecture Overview

```
┌──────────────────────────────────────────────────────────────────────────┐
│                   Microsoft Fabric Data Pipeline                          │
│                   pl_forms_samms_to_lakehouse                             │
│                                                                          │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  ForEach Site (fe_each_samms_site)                                 │  │
│  │  Iterates over every clinic in p_sites parameter                   │  │
│  │                                                                    │  │
│  │  ┌───────────────────────────────────────────────┐                │  │
│  │  │ lkp_check_form_table (Lookup)                 │                │  │
│  │  │ → SELECT name FROM sys.tables WHERE name='Form'│               │  │
│  │  └─────────────────┬─────────────────────────────┘                │  │
│  │                    │ Succeeded                                     │  │
│  │                    ▼                                               │  │
│  │  ┌───────────────────────────────────────────────┐                │  │
│  │  │ if_form_table_exists (IfCondition)             │                │  │
│  │  │ condition: firstRow.form_exists = 1            │                │  │
│  │  │                                                │                │  │
│  │  │  TRUE BRANCH ────────────────────────────────┐│                │  │
│  │  │  ┌──────────────────────────────────────┐    ││                │  │
│  │  │  │ lkp_get_existing_tables (Lookup)     │    ││                │  │
│  │  │  │ → SELECT name FROM sys.tables        │    ││                │  │
│  │  │  │   (returns ALL tables in this SAMMS) │    ││                │  │
│  │  │  └────────────────┬─────────────────────┘    ││                │  │
│  │  │                   │ Succeeded                  ││                │  │
│  │  │                   ▼                            ││                │  │
│  │  │  ┌──────────────────────────────────────┐     ││                │  │
│  │  │  │ nb_forms_build_site_sql (Notebook)   │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ INPUT: site_code, wrkdt,             │     ││                │  │
│  │  │  │        existing_tables (JSON),        │     ││                │  │
│  │  │  │        ingest_run_id                  │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ READS: bhg_silver.ctrl               │     ││                │  │
│  │  │  │        .tbl_Forms2Process            │     ││                │  │
│  │  │  │        (Fabric lakehouse — NO SAMMS) │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ BUILDS: Full UNION SQL string        │     ││                │  │
│  │  │  │         (base + Forms2Process UNIONs)│     ││                │  │
│  │  │  │         Only for tables found in     │     ││                │  │
│  │  │  │         Lookup 2 result              │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ OUTPUT: SQL string via               │     ││                │  │
│  │  │  │         mssparkutils.notebook.exit() │     ││                │  │
│  │  │  └────────────────┬─────────────────────┘     ││                │  │
│  │  │                   │ exitValue                   ││                │  │
│  │  │                   ▼                            ││                │  │
│  │  │  ┌──────────────────────────────────────┐     ││                │  │
│  │  │  │ sv_set_site_sql (Set Variable)       │     ││                │  │
│  │  │  │ v_site_sql = exitValue               │     ││                │  │
│  │  │  └────────────────┬─────────────────────┘     ││                │  │
│  │  │                   │ Succeeded                   ││                │  │
│  │  │                   ▼                            ││                │  │
│  │  │  ┌──────────────────────────────────────┐     ││                │  │
│  │  │  │ cp_forms_to_bronze (Copy Activity)   │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ Source: SAMMS SQL Server             │     ││                │  │
│  │  │  │   query = @variables('v_site_sql')   │     ││                │  │
│  │  │  │   (full UNION SQL, already built)    │     ││                │  │
│  │  │  │                                      │     ││                │  │
│  │  │  │ Sink: bhg_bronze                     │     ││                │  │
│  │  │  │       Form.br_tblFormQA (APPEND)    │     ││                │  │
│  │  │  └──────────────────────────────────────┘     ││                │  │
│  │  └──────────────────────────────────────────────┘│                │  │
│  └────────────────────────────────────────────────────────────────────┘  │
│                         │ ForEach Succeeded                              │
│                         ▼                                                │
│  ┌────────────────────────────────────────────────────────────────────┐  │
│  │  nb_forms_bronze_to_silver (Notebook Activity)                     │  │
│  │                                                                    │  │
│  │  Cell 1: Read Bronze → filter run → deduplicate                    │  │
│  │          Compute RowState from IsDeleted + ClientId               │  │
│  │  Cell 2: Pre-pass RowState reset on existing Silver rows          │  │
│  │  Cell 3: Delta MERGE → bhg_silver.pats.sl_tblFormQuestionAnswers  │  │
│  └────────────────────────────────────────────────────────────────────┘  │
└──────────────────────────────────────────────────────────────────────────┘
```

**Lakehouse Targets:**

| Layer | Lakehouse | Schema | Table |
|---|---|---|---|
| Bronze | `bhg_bronze` | `Forms` | `br_tblFormQA` |
| Silver | `bhg_silver` | `pats` | `sl_tblFormQuestionAnswers` |
| Control | `bhg_silver` | `ctrl` | `Forms2Process` |

---

## 2. What This Pipeline Does — Plain English

The SAMMS system stores clinical form data across two layers:

1. **The generic form engine** (`Form`, `FormTemplate`, `Question`, `Answer` tables) — handles most forms uniformly
2. **Custom per-form tables** (e.g., `AdmissionAssessment`, `tblTP17REVIEW`, `ReferralForm`) — dedicated tables for complex or legacy forms

There are **75 enabled form tables** tracked in the control table `tbl_Forms2Process`, which lives in `bhg_silver.ctrl`.

This pipeline, for each clinic:
1. **Checks** whether the SAMMS database has a `Form` table — if not, the site is skipped silently
2. **Reads all SAMMS table names** using a Lookup against `sys.tables` — this tells us which form tables actually exist in that clinic's version of SAMMS
3. **Builds the complete UNION SQL string** using a Spark notebook that reads `bhg_silver.ctrl.tbl_Forms2Process` and generates one UNION block per form table that exists in this SAMMS — the notebook never connects to SAMMS
4. **Executes that one SQL string** against SAMMS via a single Fabric Copy activity (on-prem gateway) — one query, one result set, one Bronze write
5. After all clinics are done, runs a **Silver notebook** that reads Bronze, resets stale RowState values via a pre-pass, and Delta-MERGEs into Silver using a 7-column composite primary key

---

## 3. Why This Design Uses a SQL Builder Notebook + Copy Activity

| Approach | Why It Does Not Work for FormQuestionAnswers |
|---|---|
| **Single static Copy activity** | Cannot conditionally include/exclude 75 table UNIONs based on which tables exist in each SAMMS version |
| **One Copy activity per form table** | 75 tables × 80 sites = 6,000 Copy activity invocations per run — unmanageable |
| **JDBC from Spark notebook** | Fabric Spark notebooks cannot connect to on-prem SAMMS SQL Servers through the Fabric on-prem data gateway. JDBC requires a direct network path; the gateway is only usable by Copy activities |
| **Pipeline expression-only query building** | Fabric pipeline expressions have no `forEach` or `map` construct; you cannot loop over 75 items and build conditional UNION blocks in a single `@concat()` expression |

**This design's solution:**

| Activity | Connects To | Does What |
|---|---|---|
| `lkp_get_existing_tables` (Lookup) | SAMMS SQL Server (via Fabric gateway) | Gets list of ALL tables in this SAMMS database |
| `nb_forms_build_site_sql` (Notebook) | `bhg_silver.ctrl` lakehouse ONLY | Reads Forms2Process, builds full UNION SQL string, returns it via `mssparkutils.notebook.exit()` |
| `sv_set_site_sql` (Set Variable) | Pipeline variable | Captures the SQL string from notebook exit value |
| `cp_forms_to_bronze` (Copy) | SAMMS SQL Server (via Fabric gateway) | Executes the pre-built SQL string — one query, all form tables UNIONed |

The Spark notebook does **zero SAMMS communication**. It is a pure Python string generator that reads a Fabric lakehouse table and produces SQL text. All SAMMS communication happens exclusively through Fabric Copy and Lookup activities, which properly route through the on-prem data gateway.

---

## 4. Prerequisites — What You Need Before Starting

| Item | What It Is | Where It Lives |
|---|---|---|
| SAMMS SQL Server Fabric connection | On-prem gateway connection for Copy + Lookup activities | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse | Bronze layer | Fabric workspace |
| `bhg_silver` Lakehouse | Silver layer | Fabric workspace |
| `Forms` schema in `bhg_bronze` | Schema for FormQA Bronze tables | `bhg_bronze` Lakehouse |
| `pats` schema in `bhg_silver` | Schema for Silver patient tables | `bhg_silver` Lakehouse |
| `ctrl` schema in `bhg_silver` | Control/reference table schema | `bhg_silver` Lakehouse |
| `bhg_silver.ctrl.tbl_Forms2Process` | Pre-loaded Fabric version of BHG_DR's `ctrl.tbl_Forms2Process` | Must be loaded before first run |
| On-premise data gateway | Routes Fabric Copy/Lookup activities to SAMMS SQL Servers | Fabric settings |

**Key IDs to note down:**

```
Workspace ID:            c5097ffb-b78e-441d-9575-a82bac23cac8
Bronze Lakehouse ID:     77d24027-6a1c-43a8-a998-1a14dd3c0d52
SAMMS Connection ID:     9743b95a-fd66-4f7c-9767-e6eb0f1ecab7
```

Replace these with your actual GUIDs everywhere they appear.

**`bhg_silver.ctrl.tbl_Forms2Process` required columns:**

| Column | Type | Used For |
|---|---|---|
| `TableName` | varchar | SAMMS source table name |
| `FormName` | varchar | Literal injected into SQL as form name |
| `Prefix` | int | Numeric prefix for FormID construction |
| `Enabled` | bit | `1` = active |
| `RowState` | bit | `1` = active |
| `CreatedOn` | varchar | Source column name for created date (varies per table) |
| `ModifiedOn` | varchar | Source column name for modified date (NULL if not applicable) |
| `DateFilterEnabled` | bit | `1` = apply lookback filter; `0` = always full extract |

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

**Why:** Tags every Bronze row with the current pipeline run so the Silver notebook filters to only this run's rows.

---

#### Parameter 2: `p_lookback_days`

| Setting | Value |
|---|---|
| Name | `p_lookback_days` |
| Type | `Int` |
| Default | `30` |

**Why:** The old system uses `DaysBack - 15 = -30` total for forms. Use `365` for a historical deep reload.

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

| Property | Meaning |
|---|---|
| `site_code` | Human-readable clinic code injected as a literal in the SQL |
| `source_database` | Exact database name on the SAMMS SQL Server — used by the Lookup and Copy activities to target the right database |

---

#### Parameter 4: `p_reload`

| Setting | Value |
|---|---|
| Name | `p_reload` |
| Type | `Bool` |
| Default | `false` |

**Why:** When `true`, lookback is overridden to `1/1/2010` for a full historical reload. Mirrors `st.Reload` in the old system.

---

### Pipeline Variables

Go to: **Pipeline canvas → click empty space → Variables tab → + New**

#### Variable 1: `v_site_sql`

| Setting | Value |
|---|---|
| Name | `v_site_sql` |
| Type | `String` |
| Default | *(leave blank)* |

**Why:** The SQL builder notebook writes the full UNION SQL for the current site into this variable. The Copy activity then reads from it. This variable is overwritten on each iteration of the ForEach loop.

---

## 6. Step 1 — Create the Data Pipeline in Fabric

1. Open your Fabric workspace
2. Click **+ New** → **Data pipeline**
3. Name it: `pl_forms_samms_to_lakehouse`
4. Click **Create**
5. On the pipeline canvas, click an empty area → **Parameters tab** → add all four parameters from Section 5
6. On the same canvas → **Variables tab** → add `v_site_sql`

---

## 7. Step 2 — Add the ForEach Activity

1. Drag a **ForEach** onto the canvas
2. Rename it: `fe_each_samms_site`

**Settings tab:**

| Setting | Value |
|---|---|
| Items | `@pipeline().parameters.p_sites` |
| Sequential | **Checked (True)** |
| Batch count | Leave blank |

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

## 8. Step 3 — Inside ForEach: Lookup 1 — Check Form Table Exists

### What this does

Probes the SAMMS database for the `Form` table. If it does not exist, this clinic has not been upgraded to the SAMMS form engine and is skipped entirely.

### Add the activity
1. Click **Edit** on the ForEach to open its inner canvas
2. Drag a **Lookup** activity
3. Rename it: `lkp_check_form_table`

### Configure Settings tab

| Setting | Value |
|---|---|
| Source type | SQL Server |
| Connection | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| Query | Use the expression below |
| First row only | **Checked (True)** |

**Query expression (Expression mode):**
```
@concat(
'SELECT form_exists = COUNT(1)
FROM [', item().source_database, '].sys.tables
WHERE name = ''Form'''
)
```

**This produces:**
```sql
SELECT form_exists = COUNT(1)
FROM [SAMMS-ColoradoSpringsV5].sys.tables
WHERE name = 'Form'
```

Returns a single row: `{ "form_exists": 1 }` or `{ "form_exists": 0 }`.

**Policy tab:**

| Setting | Value |
|---|---|
| Timeout | `0.12:00:00` |
| Retry | `0` |

**JSON:**
```json
{
    "name": "lkp_check_form_table",
    "type": "Lookup",
    "dependsOn": [],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "source": {
            "type": "SqlServerSource",
            "sqlReaderQuery": {
                "value": "@concat('SELECT form_exists = COUNT(1) FROM [', item().source_database, '].sys.tables WHERE name = ''Form''')",
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

## 9. Step 4 — Inside ForEach: IfCondition — Gate on Form Table

### Add the activity
1. On the inner canvas, drag an **IfCondition** activity
2. Rename it: `if_form_table_exists`
3. Draw a dependency: `lkp_check_form_table` → `if_form_table_exists` (Succeeded)

### Configure Activities tab — Condition

| Setting | Value |
|---|---|
| Condition | `@equals(activity('lkp_check_form_table').output.firstRow.form_exists, 1)` |

**Why:** If `form_exists = 1`, proceed to extract. If `0`, the IfCondition's False branch is empty — the site is silently skipped.

**JSON:**
```json
{
    "name": "if_form_table_exists",
    "type": "IfCondition",
    "dependsOn": [
        { "activity": "lkp_check_form_table", "dependencyConditions": ["Succeeded"] }
    ],
    "typeProperties": {
        "expression": {
            "value": "@equals(activity('lkp_check_form_table').output.firstRow.form_exists, 1)",
            "type": "Expression"
        },
        "ifTrueActivities": [],
        "ifFalseActivities": []
    }
}
```

All remaining activities in this section go into `ifTrueActivities`. Click **Edit** on the IfCondition's True branch to open it.

---

## 10. Step 5 — Inside IfCondition True Branch: Lookup 2 — Get All Existing Tables

### What this does

Queries `sys.tables` for the current SAMMS database and returns the names of ALL user tables. The SQL builder notebook intersects this list with `tbl_Forms2Process.TableName` to know which form tables to include in the UNION SQL. This is the exact equivalent of the `sys.tables` probe the old C# code runs per form table — done here in a single batch for all tables at once.

### Add the activity
1. Inside the True branch canvas, drag a **Lookup** activity
2. Rename it: `lkp_get_existing_tables`

### Configure Settings tab

| Setting | Value |
|---|---|
| Source type | SQL Server |
| Connection | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| Query | Use the expression below |
| First row only | **Unchecked** |

**Query expression (Expression mode):**
```
@concat('SELECT name FROM [', item().source_database, '].sys.tables')
```

**This produces:**
```sql
SELECT name FROM [SAMMS-ColoradoSpringsV5].sys.tables
```

Returns all table names in this SAMMS database — e.g., `[{"name":"Form"}, {"name":"AdmissionAssessment"}, ...]`.

> **Why not filter to only form table names?** Querying all tables with no WHERE clause requires no maintenance when new form tables are added. The SQL builder notebook filters the results against Forms2Process.

**Policy tab:** Same as Lookup 1 (timeout 12h, retry 0).

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

## 11. Step 6 — Inside True Branch: Notebook Activity — SQL Builder

### What this does

This notebook is a **pure string generator**. It:
- Receives the list of existing SAMMS tables (from Lookup 2) as a JSON string parameter
- Reads `bhg_silver.ctrl.tbl_Forms2Process` (Fabric lakehouse — no SAMMS connection)
- Builds the full UNION SQL for this site (base query + one UNION block per enabled form table that exists in this SAMMS)
- Returns the SQL string via `mssparkutils.notebook.exit()` — the pipeline captures this as the notebook's `exitValue`

**This notebook makes zero connections to SAMMS.**

### Add the activity
1. On the True branch canvas, drag a **Notebook** activity
2. Rename it: `nb_forms_build_site_sql`
3. Draw a dependency: `lkp_get_existing_tables` → `nb_forms_build_site_sql` (Succeeded)

### Configure Settings tab

| Setting | Value |
|---|---|
| Notebook | Select `nb_forms_build_site_sql` (create it in Step 10) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Configure Parameters

| Parameter Name | Type | Value |
|---|---|---|
| `p_site_code` | String | `@item().site_code` (Expression) |
| `p_source_database` | String | `@item().source_database` (Expression) |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |
| `p_lookback_days` | Int | `@pipeline().parameters.p_lookback_days` (Expression) |
| `p_reload` | Bool | `@pipeline().parameters.p_reload` (Expression) |
| `p_existing_tables_json` | String | `@string(activity('lkp_get_existing_tables').output.value)` (Expression) |

> **`p_existing_tables_json`** is the Lookup 2 result serialised to a JSON string. The notebook deserialises it and extracts the table name set.

**Policy tab:** Timeout `0.12:00:00`, Retry `0`.

**JSON:**
```json
{
    "name": "nb_forms_build_site_sql",
    "type": "TridentNotebook",
    "dependsOn": [
        { "activity": "lkp_get_existing_tables", "dependencyConditions": ["Succeeded"] }
    ],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "notebookId": "<<your-notebook-guid>>",
        "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
        "parameters": {
            "p_site_code":             { "value": { "value": "@item().site_code",                                                               "type": "Expression" }, "type": "string" },
            "p_source_database":       { "value": { "value": "@item().source_database",                                                        "type": "Expression" }, "type": "string" },
            "p_ingest_run_id":         { "value": { "value": "@pipeline().parameters.p_ingest_run_id",                                         "type": "Expression" }, "type": "string" },
            "p_lookback_days":         { "value": { "value": "@pipeline().parameters.p_lookback_days",                                         "type": "Expression" }, "type": "int"    },
            "p_reload":                { "value": { "value": "@pipeline().parameters.p_reload",                                                 "type": "Expression" }, "type": "bool"   },
            "p_existing_tables_json":  { "value": { "value": "@string(activity('lkp_get_existing_tables').output.value)",                       "type": "Expression" }, "type": "string" }
        }
    }
}
```

---

## 12. Step 7 — Inside True Branch: Set Variable — Capture Built SQL

After the notebook exits, its return value is captured into the pipeline variable `v_site_sql`.

### Add the activity
1. Drag a **Set Variable** activity onto the True branch canvas
2. Rename it: `sv_set_site_sql`
3. Draw a dependency: `nb_forms_build_site_sql` → `sv_set_site_sql` (Succeeded)

### Configure Settings tab

| Setting | Value |
|---|---|
| Variable name | `v_site_sql` |
| Value | `@activity('nb_forms_build_site_sql').output.result.exitValue` (Expression) |

**JSON:**
```json
{
    "name": "sv_set_site_sql",
    "type": "SetVariable",
    "dependsOn": [
        { "activity": "nb_forms_build_site_sql", "dependencyConditions": ["Succeeded"] }
    ],
    "typeProperties": {
        "variableName": "v_site_sql",
        "value": {
            "value": "@activity('nb_forms_build_site_sql').output.result.exitValue",
            "type": "Expression"
        }
    }
}
```

---

## 13. Step 8 — Inside True Branch: Copy Activity — Extract to Bronze

This is the **only** activity that connects to SAMMS for data extraction. It executes the pre-built UNION SQL string from `v_site_sql` against the clinic's SAMMS SQL Server and appends the results to Bronze.

### Add the activity
1. Drag a **Copy** activity onto the True branch canvas
2. Rename it: `cp_forms_to_bronze`
3. Draw a dependency: `sv_set_site_sql` → `cp_forms_to_bronze` (Succeeded)

### Configure Source tab

| Setting | Value |
|---|---|
| Source type | SQL Server |
| Connection | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| Query | `@variables('v_site_sql')` (Expression) |
| Query timeout | `02:00:00` |

> The `v_site_sql` variable holds the complete `SELECT DISTINCT * FROM (...all UNIONs...) z` string built by the notebook. The Copy activity executes it verbatim against the current site's SAMMS database.

### Configure Sink tab

| Setting | Value |
|---|---|
| Sink type | **Lakehouse** |
| Workspace ID | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder | `Tables` |
| Schema | `Forms` |
| Table | `br_tblFormQA` |
| Table action | **Append** |
| Apply V-Order | No |

### Configure Mapping tab

| Setting | Value |
|---|---|
| Type conversion | **Enabled** |
| Allow data truncation | **True** |
| Treat boolean as number | **False** |

### Configure Policy tab

| Setting | Value |
|---|---|
| Timeout | `0.12:00:00` |
| Retry | `0` |
| Retry interval | `30` seconds |

**JSON:**
```json
{
    "name": "cp_forms_to_bronze",
    "type": "Copy",
    "dependsOn": [
        { "activity": "sv_set_site_sql", "dependencyConditions": ["Succeeded"] }
    ],
    "policy": { "timeout": "0.12:00:00", "retry": 0, "retryIntervalInSeconds": 30 },
    "typeProperties": {
        "source": {
            "type": "SqlServerSource",
            "sqlReaderQuery": { "value": "@variables('v_site_sql')", "type": "Expression" },
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
                "typeProperties": { "schema": "Forms", "table": "br_tblFormQA" }
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

1. Go back to the **main pipeline canvas** (outside the ForEach)
2. Drag a **Notebook** activity
3. Rename it: `nb_forms_bronze_to_silver`
4. Draw dependency: `fe_each_samms_site` → `nb_forms_bronze_to_silver` (Succeeded)

### Configure Settings tab

| Setting | Value |
|---|---|
| Notebook | Select `nb_forms_bronze_to_silver` (create it in Step 11) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Configure Parameters

| Parameter | Type | Value |
|---|---|---|
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |
| `p_lookback_days` | Int | `@pipeline().parameters.p_lookback_days` (Expression) |
| `p_reload` | Bool | `@pipeline().parameters.p_reload` (Expression) |

**Policy:** Timeout `0.12:00:00`, Retry `0`.

---

## 15. Step 10 — Create the SQL Builder Notebook: nb_forms_build_site_sql

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_forms_build_site_sql`
3. Attach `bhg_silver` to the notebook's Lakehouse panel — **do NOT attach any SQL Server linked service**
4. The notebook has **one cell**

> **Critical:** This notebook must have `bhg_silver` in its Lakehouse panel so it can read `bhg_silver.ctrl.tbl_Forms2Process`. It should NOT be configured with any SAMMS connection — all SAMMS communication happens through the Copy and Lookup activities in the pipeline.

### The Full SQL Builder Notebook — paste this exactly

```python
# ─────────────────────────────────────────────────────────────────────
# nb_forms_build_site_sql
#
# PURPOSE: Pure SQL string generator — NO SAMMS connection.
#
# READS:  bhg_silver.ctrl.tbl_Forms2Process  (Fabric lakehouse)
# INPUT:  p_existing_tables_json  — JSON string from Lookup 2 (sys.tables)
#         p_site_code, p_source_database, p_ingest_run_id,
#         p_lookback_days, p_reload
#
# OUTPUT: Full UNION SQL string via mssparkutils.notebook.exit()
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

# ── Build the set of tables that exist in this SAMMS database ────────
# p_existing_tables_json is the string form of the Lookup 2 output:
#   '[{"name":"Form"},{"name":"AdmissionAssessment"},...]'
existing_rows = json.loads(p_existing_tables_json)
existing_tables = {row["name"].lower() for row in existing_rows}

print(f"Site:           {p_site_code}")
print(f"Database:       {p_source_database}")
print(f"Run ID:         {p_ingest_run_id}")
print(f"Existing tables in SAMMS (count): {len(existing_tables)}")

# ── Compute lookback date ────────────────────────────────────────────
if p_reload:
    wrkdt = "2010-01-01"
else:
    wrkdt = (datetime.now() - timedelta(days=int(p_lookback_days))).strftime("%Y-%m-%d")

print(f"Lookback date:  {wrkdt}")

# ── Read Forms2Process from bhg_silver.ctrl ───────────────────────────
f2p_df = (
    spark.table("bhg_silver.ctrl.Forms2Process")
    .filter("Enabled = true AND RowState = true")
    .orderBy("Prefix")
    .toPandas()
)
print(f"Forms2Process rows loaded: {len(f2p_df)}")

sc = p_site_code   # shorthand
db = f"[{p_source_database}]"   # source SAMMS database, e.g. [SAMMS-ColoradoSpringsV5]

def tbl(name):
    """Return a fully-qualified SAMMS table reference."""
    return f"{db}.dbo.[{name}]"

def sys_all_columns():
    """Return the source database sys.all_columns reference."""
    return f"{db}.sys.all_columns"

# ── Standard SQL fragments reused across cases ─────────────────────
# Legacy SaveFormQA keeps IsDeleted nullable. It passes source IsDeleted through
# and lets SaveFormQuestionAnswers derive RowState from IsDeleted + ClientId.
std_isdeleted = "IsDeleted = a.IsDeleted"
std_pa_join = (
    f"INNER JOIN {tbl('SF_PatientPreAdmission')} pa ON a.PreAdmissionId = pa.ID "
    f"LEFT JOIN {tbl('SF_DataForms')} d ON pa.DataFormId = d.Id"
)

# ── Build metadata literal columns (added to every SELECT) ───────────
# These land as real columns in Bronze and carry extraction context.
meta_cols = (
    f"  '{sc}' AS _site_code,\n"
    f"  '{p_source_database}' AS _source_database,\n"
    f"  '{p_ingest_run_id}' AS _ingest_run_id,\n"
    f"  GETDATE() AS _extracted_at,\n"
    f"  '{wrkdt}' AS _lookback_date,\n"
)

# ── Step 1: Base query — Form/FormTemplate/Question/Answer ───────────
# UNION 1: forms with answered questions (a.Value IS NOT NULL)
# UNION 2: forms with no questions at all (q.Id IS NULL — header-only)
#
# Outer SELECT adds QuestionOrderId via ROW_NUMBER() for rows where
# q.QuestionOrderId is NULL (synthetic ordering for forms without it).

strCmd = f"""
SELECT
  {meta_cols}
  SiteCode, FormName,
  CONVERT(varchar(100), FormId) AS FormId,
  PreAdmissionId, ClientId, QuestionId,
  QuestionOrderId = ISNULL(x.QuestionOrderId,
      ROW_NUMBER() OVER (
          PARTITION BY x.FormName, x.FormId, x.ClientId, x.QuestionId
          ORDER BY x.QuestionId, x.AnswerSeq
      )
  ),
  QuestionText, OptionId, AnswerValue,
  CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted
FROM (

  SELECT
      SiteCode  = '{sc}',
      ft.FormName,
      FormId    = f.id,
      f.ClientId,
      f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy,
      f.PreAdmissionId,
      IsDeleted = f.IsDeleted,
      QuestionId      = ISNULL(q.Id, 0),
      QuestionOrderId = q.QuestionOrderId,
      q.QuestionText,
      a.OptionId,
      AnswerValue = a.Value,
      AnswerSeq   = a.Id
  FROM {tbl('Form')} f
      LEFT JOIN {tbl('FormTemplate')} ft ON f.FormTemplateId = ft.Id
      LEFT JOIN {tbl('Question')} q      ON ft.Id = q.FormTemplateId
      LEFT JOIN {tbl('Answer')} a        ON f.Id = a.FormId AND q.Id = a.QuestionId
  WHERE a.Value IS NOT NULL
    AND (f.CreatedOn >= '{wrkdt}' OR ISNULL(f.UpdatedOn, f.CreatedOn) >= '{wrkdt}')

  UNION

  SELECT
      SiteCode  = '{sc}',
      ft.FormName,
      FormId    = f.id,
      f.ClientId,
      f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy,
      f.PreAdmissionId,
      IsDeleted = f.IsDeleted,
      QuestionId      = ISNULL(q.Id, 0),
      QuestionOrderId = q.QuestionOrderId,
      q.QuestionText,
      a.OptionId,
      AnswerValue = a.Value,
      AnswerSeq   = a.Id
  FROM {tbl('Form')} f
      LEFT JOIN {tbl('FormTemplate')} ft ON f.FormTemplateId = ft.Id
      LEFT JOIN {tbl('Question')} q      ON ft.Id = q.FormTemplateId
      LEFT JOIN {tbl('Answer')} a        ON f.Id = a.FormId AND q.Id = a.QuestionId
  WHERE q.Id IS NULL
    AND (f.CreatedOn >= '{wrkdt}' OR ISNULL(f.UpdatedOn, f.CreatedOn) >= '{wrkdt}')

) x
"""

# ── Step 2: Loop over Forms2Process — UNION in each custom table ─────
# For each enabled row in tbl_Forms2Process:
#   - Check if TableName exists in this SAMMS (using existing_tables set)
#   - Dispatch to the correct switch case
#   - Append a UNION block
#   - If DateFilterEnabled, append a WHERE clause

def updated_on_sql(modified_on, alias="a"):
    """Build the UpdatedOn column fragment. Handles NULL ModifiedOn column name."""
    if modified_on and str(modified_on) not in ("", "nan", "None"):
        return f", CONVERT(date, {alias}.{modified_on}) AS [UpdatedOn]"
    return ", [UpdatedOn] = null"

for _, xf in f2p_df.iterrows():
    table_name   = xf["TableName"]
    form_name    = xf["FormName"]
    prefix       = xf["Prefix"]
    created_on   = xf["CreatedOn"]
    modified_on  = xf["ModifiedOn"]
    date_filter  = bool(xf["DateFilterEnabled"])

    if not table_name or str(table_name) in ("", "nan"):
        continue

    # Skip tables not present in this SAMMS version
    if table_name.lower() not in existing_tables:
        print(f"  SKIP (not in this SAMMS): {table_name}")
        continue

    tname_lower = table_name.lower()

    # Every case wraps its UNION in the same outer SELECT for ROW_NUMBER
    union_wrap_open = (
        f"\nUNION\n"
        f"SELECT\n{meta_cols}"
        f"  SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID,\n"
        f"  QuestionOrderID = ROW_NUMBER() OVER(\n"
        f"      PARTITION BY v.FormName, v.FormId, v.ClientId, v.QuestionId\n"
        f"      ORDER BY v.QuestionId\n"
        f"  ),\n"
        f"  QuestionText, OptionID, AnswerValue,\n"
        f"  Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted\n"
        f"FROM (\n"
        f"  SELECT SiteCode = '{sc}', [FormName] = '{form_name}'"
    )

    if tname_lower == "adversechildhood":
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.id)"
            f"\n    , PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.ModifiedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {std_pa_join}"
            f"\n) v"
        )

    elif tname_lower == "financialhardshipapplication":
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, a.cltId)"
            f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.id)"
            f"\n    , PreAdmissionId = a.PreAdmissionId, ClientId = a.cltId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.ModifiedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {std_pa_join}"
            f"\n) v"
        )

    elif tname_lower == "tbltp17review":
        # Completely custom — no standard cols, no PA join.
        # tprReviewFrequency (UNION D) checked via existing_tables set.
        # tblTP17REVIEW never gets a date filter — always full extract.
        has_review_freq = "tprReviewFrequency".lower() in existing_tables  # won't work this way

        # NOTE: existing_tables contains TABLE names from sys.tables, not column names.
        # For the tprReviewFrequency column check, the old system queries sys.all_columns.
        # Since we have no SAMMS connection in this notebook, we use a safe approach:
        # Always include UNION D wrapped in a conditional SELECT that returns no rows
        # when the column does not exist — using SQL Server's TRY_CAST on sys.all_columns.
        # The column probe is embedded in the SQL itself:
        #   WHERE EXISTS (SELECT 1 FROM [source_db].sys.all_columns WHERE object_id=OBJECT_ID('[source_db].dbo.[tblTP17REVIEW]') AND name='tprReviewFrequency')
        # Since tprReviewFrequency appears inside a UNION SELECT with a WHERE clause
        # that references sys.all_columns (not the column itself), SQL Server
        # does NOT validate the column at parse time — the WHERE runs first.
        # This is the correct SQL Server pattern for optional column UNIONs.
        #
        # IMPORTANT: This works because we are selecting a computed column name
        # ('Review Frequency') not the actual column tprReviewFrequency in the
        # EXISTS-gated UNION — the actual column reference is guarded separately.

        # Build UNION A-C (always present)
        tp_block = (
            f"\nUNION\n"
            f"SELECT\n{meta_cols}"
            f"  SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID,\n"
            f"  QuestionOrderID = ROW_NUMBER() OVER(\n"
            f"      PARTITION BY tp.FormName, tp.FormId, tp.ClientId, tp.QuestionId\n"
            f"      ORDER BY tp.QuestionId\n"
            f"  ),\n"
            f"  QuestionText, OptionID, AnswerValue,\n"
            f"  Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted\n"
            f"FROM (\n"
            # UNION A — QuestionID 1
            f"  SELECT SiteCode='{sc}', 'TP-'+TprTYPE AS [FormName]\n"
            f"    , '8-1-'+CONVERT(varchar,ABS(tprCLTID))+'-'+CONVERT(varchar,tpRID)+'-'+CONVERT(varchar,tprTPID) AS [FormID]\n"
            f"    , null AS PreAdmissionId, ABS(tprCLTID) AS ClientId\n"
            f"    , 1 AS QuestionID, 1 AS QuestionOrderID\n"
            f"    , 'Treatment Plan Type' AS QuestionText, null AS [OptionID]\n"
            f"    , TprTYPE AS AnswerValue, tprCreatedby AS Createdby\n"
            f"    , CONVERT(date,tprDT) AS [CreatedOn], null AS [UpdatedBy], null AS [UpdatedOn]\n"
            f"    , CASE WHEN tprCLTID<0 THEN 1 ELSE 0 END AS Isdeleted\n"
            f"  FROM {tbl('tblTP17REVIEW')}\n"
            # UNION B — QuestionID 2
            f"  UNION SELECT SiteCode='{sc}', 'TP-'+TprTYPE\n"
            f"    , '8-2-'+CONVERT(varchar,ABS(tprCLTID))+'-'+CONVERT(varchar,tpRID)+'-'+CONVERT(varchar,tprTPID)\n"
            f"    , null, ABS(tprCLTID), 2, 1, 'Treatment Phase Type', null\n"
            f"    , tpTreatmentPhase, tprCreatedby, CONVERT(date,tprDT), null, null\n"
            f"    , CASE WHEN tprCLTID<0 THEN 1 ELSE 0 END\n"
            f"  FROM {tbl('tblTP17REVIEW')}\n"
            # UNION C — QuestionID 3
            f"  UNION SELECT SiteCode='{sc}', 'TP-'+TprTYPE\n"
            f"    , '8-3-'+CONVERT(varchar,ABS(tprCLTID))+'-'+CONVERT(varchar,tpRID)+'-'+CONVERT(varchar,tprTPID)\n"
            f"    , null, ABS(tprCLTID), 3, 1, 'Next Due', null\n"
            f"    , CONVERT(varchar,tprNEXT), tprCreatedby, CONVERT(date,tprDT), null, null\n"
            f"    , CASE WHEN tprCLTID<0 THEN 1 ELSE 0 END\n"
            f"  FROM {tbl('tblTP17REVIEW')}\n"
            # UNION D — QuestionID 4 (tprReviewFrequency — guarded by EXISTS on sys.all_columns)
            # The actual column tprReviewFrequency is referenced ONLY inside a subquery
            # that is gated by a WHERE EXISTS check on sys.all_columns.
            # SQL Server validates column names at compile time for static SQL, so we use
            # a dynamic approach: embed UNION D always but gate the SAMMS table reference
            # inside an EXISTS subquery that short-circuits before accessing the column.
            # The safest portable pattern: use a scalar subquery for the value.
            f"  UNION SELECT SiteCode='{sc}', 'TP-'+TprTYPE\n"
            f"    , '8-4-'+CONVERT(varchar,ABS(tprCLTID))+'-'+CONVERT(varchar,tpRID)+'-'+CONVERT(varchar,tprTPID)\n"
            f"    , null, ABS(tprCLTID), 4, 1, 'Review Frequency', null\n"
            f"    , (\n"
            f"        SELECT CASE WHEN LEN(tprReviewFrequency)>6\n"
            f"               THEN RTRIM(SUBSTRING(tprReviewFrequency,6,LEN(tprReviewFrequency)-5))\n"
            f"               ELSE RTRIM(tprReviewFrequency) END\n"
            f"        FROM {tbl('tblTP17REVIEW')} t2\n"
            f"        WHERE t2.tpRID = r.tpRID AND t2.tprTPID = r.tprTPID\n"
            f"          AND EXISTS (SELECT 1 FROM {sys_all_columns()}\n"
            f"              WHERE object_id=OBJECT_ID('{db}.dbo.[tblTP17REVIEW]') AND name='tprReviewFrequency')\n"
            f"      ) AS AnswerValue\n"
            f"    , tprCreatedby, CONVERT(date,tprDT), null, null\n"
            f"    , CASE WHEN tprCLTID<0 THEN 1 ELSE 0 END\n"
            f"  FROM {tbl('tblTP17REVIEW')} r\n"
            f"  WHERE EXISTS (SELECT 1 FROM {sys_all_columns()}\n"
            f"      WHERE object_id=OBJECT_ID('{db}.dbo.[tblTP17REVIEW]') AND name='tprReviewFrequency')\n"
            f") tp"
        )
        strCmd += tp_block
        print(f"  + tblTP17REVIEW (with tprReviewFrequency guard)")
        continue  # skip standard date filter append below

    elif tname_lower == "tblorderreq":
        test_filter = (
            "status = 'Approved' "
            "AND Notes NOT LIKE 'Test %' AND Notes <> 'TEST' "
            "AND DrNote <> 'HEllo test' AND DrNote <> 'TEST'"
        )
        block = (
            f"\nUNION\n"
            f"SELECT\n{meta_cols}"
            f"  SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID,\n"
            f"  QuestionOrderID = ROW_NUMBER() OVER(\n"
            f"      PARTITION BY v.FormName, v.FormId, v.ClientId, v.QuestionId\n"
            f"      ORDER BY v.QuestionId\n"
            f"  ),\n"
            f"  QuestionText, OptionID, AnswerValue,\n"
            f"  Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted\n"
            f"FROM (\n"
            # UNION A — QuestionID 1: Effective Date
            f"  SELECT SiteCode='{sc}', FormName='Level Justification'\n"
            f"    , '9-1-'+CONVERT(varchar,ABS(cltID))+'-'+CONVERT(varchar,ReqNum)+'-' AS FormID\n"
            f"    , ReqNum AS PreadmissionId, cltID AS ClientId\n"
            f"    , 1 AS QuestionID, 'Effective Date' AS QuestionText, 0 AS OptionID\n"
            f"    , CONVERT(varchar,EffectiveDate,101) AS AnswerValue\n"
            f"    , Staff AS Createdby, CONVERT(date,DateAdded) AS CreatedOn\n"
            f"    , StatusUser AS UpdatedBy, CONVERT(date,statusDate) AS UpdatedOn\n"
            f"    , CASE WHEN cltID<0 THEN 1 ELSE 0 END AS IsDeleted\n"
            f"  FROM {tbl('tblORDERREQ')} WHERE {test_filter}\n"
            # UNION B — QuestionID 2: Expiration Date
            f"  UNION SELECT SiteCode='{sc}', 'Level Justification'\n"
            f"    , '9-2-'+CONVERT(varchar,ABS(cltID))+'-'+CONVERT(varchar,ReqNum)+'-'\n"
            f"    , ReqNum, cltID, 2, 'Expiration Date', 0\n"
            f"    , CONVERT(varchar,expirationdate,101)\n"
            f"    , Staff, CONVERT(date,DateAdded)\n"
            f"    , StatusUser, CONVERT(date,statusDate)\n"
            f"    , CASE WHEN cltID<0 THEN 1 ELSE 0 END\n"
            f"  FROM {tbl('tblORDERREQ')} WHERE {test_filter}\n"
            f") v"
        )
        strCmd += block
        print(f"  + tblORDERREQ (Level Justification)")
        continue  # no date filter

    elif tname_lower == "insurancebenefitverification":
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.id)"
            f"\n    , PreAdmissionId = a.PreAdmissionId, ClientId = a.PreAdmissionId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.ModifiedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {std_pa_join}"
            f"\n) v"
        )

    elif tname_lower == "referralform":
        # UpdatedBy = a.updatedby (not ModifiedBy). DateFilterEnabled=0 — no date filter.
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, a.ClientId)"
            f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.id)"
            f"\n    , PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.updatedby AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {std_pa_join}"
            f"\n) v"
        )
        strCmd += block
        print(f"  + {table_name} (ReferralForm — always full extract, no date filter)")
        continue  # explicitly skip date filter append

    elif tname_lower == "sf_understandingoftreatment":
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, pa.PatientId)"
            f" + '-' + CONVERT(varchar, ISNULL(a.PreAdmissionId, 0))"
            f" + '-' + CONVERT(varchar, ISNULL(a.id, 0))"
            f"\n    , PreAdmissionId = a.PreAdmissionId, ClientId = pa.PatientId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.LastUpdatedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {std_pa_join}"
            f"\n) v"
        )

    elif tname_lower == "sf_patientpreadmission":
        self_join = (
            f"INNER JOIN {tbl('SF_PatientPreAdmission')} pa ON a.ID = pa.ID "
            f"LEFT JOIN {tbl('SF_DataForms')} d ON pa.DataFormId = d.Id"
        )
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, a.PatientId)"
            f" + '-' + CONVERT(varchar, ISNULL(a.ParentPreAdmissionId, 0))"
            f" + '-' + CONVERT(varchar, ISNULL(a.id, 0))"
            f"\n    , PreAdmissionId = a.Id, ClientId = a.PatientId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.LastUpdatedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {self_join}"
            f"\n) v"
        )

    elif tname_lower == "newperiodicreassessment":
        self_join = (
            f"INNER JOIN {tbl('SF_PatientPreAdmission')} pa ON a.ID = pa.ID "
            f"LEFT JOIN {tbl('SF_DataForms')} d ON pa.DataFormId = d.Id"
        )
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + CONVERT(varchar, a.PatientId)"
            f" + '-' + CONVERT(varchar, ISNULL(a.ParentPreAdmissionId, 0))"
            f" + '-' + CONVERT(varchar, ISNULL(a.id, 0))"
            f"\n    , PreAdmissionId = a.Id, ClientId = a.PatientId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.LastUpdatedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {self_join}"
            f"\n) v"
        )

    else:
        # DEFAULT — covers all 65 standard forms
        block = (
            f"{union_wrap_open}"
            f"\n    , FormID = '{prefix}-'"
            f" + ISNULL(CONVERT(varchar, a.ClientId), '0')"
            f" + '-' + CONVERT(varchar, a.PreAdmissionId)"
            f" + '-' + CONVERT(varchar, a.id)"
            f"\n    , PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId"
            f"\n    , QuestionID = 0, QuestionOrderID = 1"
            f"\n    , QuestionText = null, [OptionID] = null, AnswerValue = null"
            f"\n    , a.Createdby"
            f"\n    , [CreatedOn] = CONVERT(date, a.{created_on})"
            f"\n    , a.ModifiedBy AS [UpdatedBy]"
            f"{updated_on_sql(modified_on)}"
            f"\n    , {std_isdeleted}"
            f"\n  FROM {tbl(table_name)} a {std_pa_join}"
            f"\n) v"
        )

    # Append date filter WHERE clause when DateFilterEnabled = 1
    if date_filter:
        block += (
            f"\nWHERE ("
            f"v.CreatedOn >= '{wrkdt}'"
            f" OR ISNULL(v.UpdatedOn, v.CreatedOn) >= '{wrkdt}'"
            f")"
        )

    strCmd += block
    print(f"  + {table_name} (Prefix {prefix}, DateFilter={date_filter})")

# ── Step 3: Wrap in DISTINCT and return ─────────────────────────────
final_sql = f"SELECT DISTINCT * FROM ({strCmd}) z"

print(f"\nSQL built for site {p_site_code}. Approximate length: {len(final_sql):,} chars.")

# Return the SQL string to the pipeline via notebook exit value.
# The pipeline Set Variable activity captures this as v_site_sql.
mssparkutils.notebook.exit(final_sql)
```

---

## 16. Step 11 — Create the Silver Notebook: nb_forms_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_forms_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` to the Lakehouse panel
4. The notebook has **three cells**

---

## 17. Step 12 — Silver Notebook Cell 1: Load Bronze and Prepare Source

### What Cell 1 does
1. Reads Bronze filtered to this run's rows
2. Deduplicates on the 7-column PK — keeps latest `_extracted_at`
3. Normalises `FormId` to UPPER (mirrors `fqa.FormId.ToUpper()` in the old EF path)
4. Preserves nullable `IsDeleted` exactly like the old EF path
5. Adds nullable `IsChildForm` because the legacy SQL comments it out for the main Form path
6. Computes `RowState`: `0` if `ClientId < 0` or `IsDeleted = 1`; `1` otherwise
7. Drops `_site_code` from the final Silver shape so only `SiteCode` is exposed
8. Adds audit columns (`LastModAt`, `silver_updated_at`)
9. Loads `tbl_Forms2Process` into `f2p_df` for use in Cell 2's pre-pass
10. Creates the Silver table on the very first run

### Cell 1 Code

```python
from pyspark.sql.functions import col, current_timestamp, row_number, when, lit, upper
from pyspark.sql.window import Window
from datetime import datetime, timedelta

try: p_ingest_run_id
except NameError: p_ingest_run_id = "test-run-001"

try: p_lookback_days
except NameError: p_lookback_days = 30

try: p_reload
except NameError: p_reload = False

bronze_table = "bhg_bronze.Form.br_tblFormQA"
silver_table = "bhg_silver.pats.sl_tblFormQuestionAnswers"
f2p_table    = "bhg_silver.ctrl.Forms2Process"

print(f"Run ID: {p_ingest_run_id}")

# Compute wrkdt for Cell 2 pre-pass
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
non_date_filtered_forms = set(
    f2p_df[f2p_df["DateFilterEnabled"] == False]["FormName"].tolist()
)

# Read Bronze for this run
bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)
bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze rows found for ingest_run_id = {p_ingest_run_id}")

# Deduplicate on 7-column PK — keep latest _extracted_at per key
pk_cols = ["SiteCode", "FormName", "FormId", "ClientId",
           "PreAdmissionId", "QuestionId", "QuestionOrderId"]

w = Window.partitionBy(*pk_cols).orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .withColumn("FormId", upper(col("FormId")))       # normalise to UPPER
    .withColumn("_rn", row_number().over(w))
    .where(col("_rn") == 1)
    .drop("_rn")
    .withColumn("IsDeleted", col("IsDeleted").cast("boolean"))
    .withColumn("IsChildForm", lit(None).cast("boolean"))
    .withColumn(
        "RowState",
        when((col("IsDeleted") == True) | (col("ClientId") < 0), lit(0)).otherwise(lit(1))
    )
    .drop("_site_code")
    .withColumn("LastModAt",         current_timestamp())
    .withColumn("silver_updated_at", current_timestamp())
)

src_count = src_df.count()
print(f"Deduplicated source rows: {src_count}")
src_df.createOrReplaceTempView("vw_forms_current_run")

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

### Why the pre-pass is essential

Before merging new data, existing Silver rows within the current extraction scope are reset to `RowState = 0`. This is how the pipeline detects soft-deletes: a form deleted in SAMMS will not appear in the next extraction. The pre-pass ensures its Silver row goes to `RowState = 0`, and the MERGE in Cell 3 will not re-activate it (since it's absent from source).

**Rules (from `SaveFormQuestionAnswers` in `SaveFormQAData.cs`):**

| Form type | Pre-pass action |
|---|---|
| `DateFilterEnabled = 0` (e.g., ReferralForm) | Reset `RowState = 0` unconditionally for all rows of this form at affected sites |
| `DateFilterEnabled = 1` (most forms) | Reset `RowState = 0` only where `CreatedOn >= wrkdt OR UpdatedOn >= wrkdt` AND `RowState = 1` |
| `FormName` starts with `TP-` | Treated as `Treatment Plan` for Forms2Process lookup (date-gated reset) |

### Cell 2 Code

```python
from delta.tables import DeltaTable
from pyspark.sql.functions import col, lit

silver_table = "bhg_silver.pats.sl_tblFormQuestionAnswers"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)
wrkdt_str = wrkdt.strftime("%Y-%m-%d")

# Identify sites processed in this run
sites_this_run = (
    spark.table("bhg_bronze.Form.br_tblFormQA")
    .where(col("_ingest_run_id") == p_ingest_run_id)
    .select("SiteCode").distinct()
    .rdd.flatMap(lambda x: x).collect()
)
print(f"Sites in this run: {sites_this_run}")

site_filter = " OR ".join([f"SiteCode = '{s}'" for s in sites_this_run])

# Unconditional reset — non-date-filtered forms (e.g., ReferralForm)
if non_date_filtered_forms:
    ndf_list = ", ".join([f"'{f}'" for f in non_date_filtered_forms])
    silver_delta.update(
        condition=f"({site_filter}) AND FormName IN ({ndf_list}) AND RowState = 1",
        set={"RowState": lit(0)}
    )
    print(f"Unconditional pre-pass reset applied to: {non_date_filtered_forms}")

# Date-gated reset — all other forms (date-filtered + unknown + TP-* forms)
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
print("Pre-pass complete.")
```

---

## 19. Step 14 — Silver Notebook Cell 3: Delta MERGE Into Silver

### The 7-column composite PK — no RowChkSum

FormQuestionAnswers does **not** use `RowChkSum`. The `AnswerValue` column is free-text (`nvarchar(max)`) — SQL Server's `CHECKSUM()` is unreliable on large text and can produce collisions. The old system never checksummed this table and always did a full column update on every match. This notebook follows the same pattern.

**Match key:**
```
SiteCode + FormName + FormId(UPPER) + ClientId + PreAdmissionId + QuestionId + QuestionOrderId
```

### Cell 3 Code

```python
from delta.tables import DeltaTable
from pyspark.sql.functions import current_timestamp

silver_table = "bhg_silver.pats.sl_tblFormQuestionAnswers"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols    = src_df.columns
update_cols = [c for c in src_cols if c != "silver_created_at"]

update_set = {c: f"src.{c}" for c in update_cols}
update_set["silver_updated_at"] = "current_timestamp()"
update_set["LastModAt"]         = "current_timestamp()"

insert_values = {c: f"src.{c}" for c in src_cols}
insert_values["silver_created_at"] = "current_timestamp()"

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        """
        tgt.SiteCode         = src.SiteCode
        AND tgt.FormName     = src.FormName
        AND tgt.FormId       = src.FormId
        AND tgt.ClientId     = src.ClientId
        AND tgt.PreAdmissionId  = src.PreAdmissionId
        AND tgt.QuestionId   = src.QuestionId
        AND tgt.QuestionOrderId = src.QuestionOrderId
        """
    )
    # Matched: full update — no RowChkSum, always refresh from source
    .whenMatchedUpdate(set=update_set)
    # Not matched: new record — insert with silver_created_at
    .whenNotMatchedInsert(values=insert_values)
    .execute()
)

print("Silver MERGE for FormQuestionAnswers completed successfully.")
```

---

## 20. End-to-End Flow Summary

```
TRIGGER: Pipeline runs with parameters:
  p_ingest_run_id = "FORMS-2026-05-08-001"
  p_lookback_days = 30
  p_reload        = false
  p_sites         = [ {ColoradoSpringsV5}, {B01}, {B02}, ... ]

STEP 1 — ForEach begins: item = ColoradoSpringsV5

STEP 2 — lkp_check_form_table (Lookup)
  → SELECT form_exists = COUNT(1) FROM [SAMMS-ColoradoSpringsV5].sys.tables
     WHERE name = 'Form'
  → Result: { form_exists: 1 }  → proceed to IfCondition True branch
  → Result: { form_exists: 0 }  → IfCondition False branch (empty) → skip site

STEP 3 — lkp_get_existing_tables (Lookup, True branch)
  → SELECT name FROM [SAMMS-ColoradoSpringsV5].sys.tables
  → Result: [{"name":"Form"},{"name":"AdmissionAssessment"}, ...75+ names...]

STEP 4 — nb_forms_build_site_sql (Notebook, True branch)
  INPUT received:
    p_site_code            = "ColoradoSpringsV5"
    p_source_database      = "SAMMS-ColoradoSpringsV5"
    p_ingest_run_id        = "FORMS-2026-05-08-001"
    p_lookback_days        = 30  → wrkdt = "2026-04-08"
    p_existing_tables_json = '[{"name":"Form"},{"name":"AdmissionAssessment"},...]'

  Reads: bhg_silver.ctrl.tbl_Forms2Process  (75 rows)
  Builds: existing_tables set from JSON parameter
  Generates: base UNION SQL (Form/Question/Answer)
  Loops Forms2Process:
    AdmissionAssessment → in existing_tables → default case → UNION appended
    ReferralForm        → in existing_tables → referralform case → UNION, no date filter
    tblTP17REVIEW       → in existing_tables → custom case → 3-4 UNIONs, tprReviewFrequency guarded
    tblORDERREQ         → in existing_tables → custom case → 2 UNIONs, approved-only filter
    FinancialHardshipAppl → in existing_tables → custom cltId case → UNION
    MissingFormTable    → NOT in existing_tables → skip
    ... continues for all 75 rows ...
  Returns: complete SQL string via mssparkutils.notebook.exit()

STEP 5 — sv_set_site_sql (Set Variable)
  → v_site_sql = exitValue (full UNION SQL string)

STEP 6 — cp_forms_to_bronze (Copy Activity)
  Source: SAMMS SQL Server (via Fabric on-prem gateway)
    query = @variables('v_site_sql')
    [executes: SELECT DISTINCT * FROM (...all UNIONs...) z]
  Sink: bhg_bronze.Form.br_tblFormQA  (APPEND)
  → N rows written to Bronze tagged with _ingest_run_id

STEP 7 — ForEach moves to next site (B01) → repeats Steps 2-6

... continues for all clinics in p_sites ...

STEP 8 — ForEach completes. All sites processed.

STEP 9 — nb_forms_bronze_to_silver (Notebook)

  CELL 1:
  → Read Bronze WHERE _ingest_run_id = "FORMS-2026-05-08-001"
  → Deduplicate on 7-column PK (keep latest)
  → Normalise FormId to UPPER
  → Compute RowState (0 if ClientId<0 or IsDeleted=1, else 1)
  → Add LastModAt, silver_updated_at
  → Load Forms2Process for pre-pass metadata

  CELL 2 (Pre-pass):
  → Identify sites in this run from Bronze
  → Non-date-filtered forms (ReferralForm etc.):
       UPDATE Silver SET RowState=0 WHERE SiteCode IN (...) AND FormName IN (...)
  → All other forms (date-gated):
       UPDATE Silver SET RowState=0
         WHERE SiteCode IN (...) AND RowState=1
           AND (CreatedOn >= wrkdt OR UpdatedOn >= wrkdt)

  CELL 3 (MERGE):
  → Delta MERGE src_df → bhg_silver.pats.sl_tblFormQuestionAnswers
     MATCH on 7-column PK
     whenMatched    → full update (no RowChkSum — always refresh)
     whenNotMatched → insert with silver_created_at

DONE: Silver is current. Soft-deleted forms at RowState=0.
      New and updated form records at RowState=1.
```

---

## 21. How the Dynamic SQL Generation Works (Forms2Process)

`Forms2Process` is a metadata-driven UNION builder. Each enabled row drives one UNION block in the final SQL. The SQL builder notebook is the Fabric equivalent of the C# `foreach (var xf in xForms)` loop in `Program.cs`.

**What happens per Forms2Process row:**

```
tbl_Forms2Process row:
  TableName         = "AdmissionAssessment"
  FormName          = "Admission Assessment"
  Prefix            = 7
  CreatedOn         = "CreatedOn"
  ModifiedOn        = "ModifiedOn"
  DateFilterEnabled = 1

Notebook checks: "admissionassessment" in existing_tables?
  → YES (Lookup 2 confirmed it exists in this SAMMS)

Notebook generates:
  UNION
  SELECT _site_code, _source_database, _ingest_run_id, ...metadata...,
         SiteCode, FormName, FormID, ...
  FROM (
    SELECT SiteCode = 'ColoradoSpringsV5', [FormName] = 'Admission Assessment'
         , FormID = '7-' + ISNULL(CONVERT(varchar, a.ClientId),'0')
                  + '-' + CONVERT(varchar, a.PreAdmissionId)
                  + '-' + CONVERT(varchar, a.id)
         , PreAdmissionId = a.PreAdmissionId
         , ClientId = a.ClientId
         , QuestionID = 0, QuestionOrderID = 1
         , QuestionText = null, [OptionID] = null, AnswerValue = null
         , a.Createdby
         , [CreatedOn] = CONVERT(date, a.CreatedOn)
         , a.ModifiedBy AS [UpdatedBy]
         , CONVERT(date, a.ModifiedOn) AS [UpdatedOn]
         , IsDeleted = a.IsDeleted
    FROM [SAMMS-ColoradoSpringsV5].dbo.[AdmissionAssessment] a
    INNER JOIN [SAMMS-ColoradoSpringsV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
    LEFT JOIN [SAMMS-ColoradoSpringsV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  ) v
  WHERE (v.CreatedOn >= '2026-04-08' OR ISNULL(v.UpdatedOn, v.CreatedOn) >= '2026-04-08')
```

This pattern repeats for all 75 tables, producing a single SQL string passed to the Copy activity.

---

## 22. The Base Query — Form + FormTemplate + Question + Answer

```
dbo.Form (f)                        — one row per form instance per patient visit
  └─ LEFT JOIN dbo.FormTemplate ft  — the form definition (name)
       └─ LEFT JOIN dbo.Question q  — each question in the template
            └─ LEFT JOIN dbo.Answer a — the patient's answer per question
  INNER JOIN dbo.SF_PatientPreAdmission pa — patient pre-admission episode
  LEFT JOIN dbo.SF_DataForms d             — data form container
```

**UNION 1** — Forms with answered questions (`a.Value IS NOT NULL`):
- Filter: `f.CreatedOn >= wrkdt OR ISNULL(f.UpdatedOn, f.CreatedOn) >= wrkdt`

**UNION 2** — Form headers with no questions (`q.Id IS NULL`):
- Same date filter
- These are forms that exist in the system but have no Q&A structure (existence-only records)

The outer SELECT adds a synthetic `QuestionOrderId` via `ROW_NUMBER()` for any row where `q.QuestionOrderId` is `NULL`.

---

## 23. The Forms2Process Switch Cases — All 9 Variants

All cases produce the same 15-column output (plus metadata columns) so they UNION cleanly.

| Case | FormID Formula | ClientId From | UpdatedBy Col | PA Join Key | Special |
|---|---|---|---|---|---|
| `adversechildhood` | `Prefix-PreAdmId-PreAdmId-id` | `a.ClientId` | `a.ModifiedBy` | `a.PreAdmissionId = pa.ID` | PreAdmId used TWICE in FormID |
| `financialhardshipapplication` | `Prefix-cltId-PreAdmId-id` | `a.cltId` | `a.ModifiedBy` | `a.PreAdmissionId = pa.ID` | Source column is `cltId` not `ClientId` |
| `tbltp17review` | `8-{1-4}-ABS(tprCLTID)-tpRID-tprTPID` | `ABS(tprCLTID)` | `null` | No PA join | 3–4 rows per record; tprReviewFrequency guarded by `EXISTS (sys.all_columns)` |
| `tblorderreq` | `9-{1-2}-ABS(cltID)-ReqNum-''` | `cltID` | `StatusUser` | No PA join | 2 rows per record; filter: `Status='Approved'` + exclude test data |
| `insurancebenefitverification` | `Prefix-PreAdmId-PreAdmId-id` | `a.PreAdmissionId` | `a.ModifiedBy` | `a.PreAdmissionId = pa.ID` | No `ClientId` column — `PreAdmissionId` used for both slots |
| `referralform` | `Prefix-ClientId-PreAdmId-id` | `a.ClientId` | `a.updatedby` | `a.PreAdmissionId = pa.ID` | `DateFilterEnabled=0` — no date filter, always full extract |
| `sf_understandingoftreatment` | `Prefix-pa.PatientId-PreAdmId-id` | `pa.PatientId` | `a.LastUpdatedBy` | `a.PreAdmissionId = pa.ID` | ClientId from joined PA table, not source table |
| `sf_patientpreadmission` | `Prefix-PatientId-ParentPAId-id` | `a.PatientId` | `a.LastUpdatedBy` | `a.ID = pa.ID` (self-join) | Record IS the PA row; `a.Id` = PreAdmissionId |
| `newperiodicreassessment` | `Prefix-PatientId-ParentPAId-id` | `a.PatientId` | `a.LastUpdatedBy` | `a.ID = pa.ID` (self-join) | Same as `sf_patientpreadmission` |
| **default** (65 tables) | `Prefix-ClientId-PreAdmId-id` | `a.ClientId` | `a.ModifiedBy` | `a.PreAdmissionId = pa.ID` | Standard path for all other forms |

---

## 24. IsDeleted — Legacy Nullable Pass-Through

Legacy `Program.cs` passes the source table's nullable `IsDeleted` value into `SaveFormQuestionAnswers`. It does not coalesce NULL to 0 before saving.

```sql
IsDeleted = a.IsDeleted
```

For the main `Form`/`FormTemplate`/`Question`/`Answer` path, the old SQL selects `f.IsDeleted`; `f.IsChildForm` is present in comments only and is not returned to the save method. This is why BHG_DR commonly has `IsChildForm = NULL`.

**Exceptions:**
- `tbltp17review`: `CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END`
- `tblorderreq`: `CASE WHEN cltID < 0 THEN 1 ELSE 0 END`

**How IsDeleted flows to RowState in Silver:**

```python
RowState = 0  if IsDeleted == 1   (logically deleted in source)
RowState = 0  if ClientId < 0     (negative ClientId = void/placeholder record)
RowState = 1  otherwise
```

---

## 25. RowState Pre-Pass — Why It Exists and How It Works

The pre-pass resets `RowState = 0` on existing Silver rows **before** the MERGE runs. The MERGE then either re-activates a row (if it reappears in source) or leaves it at `RowState = 0` (soft-deleted because it was absent from this extraction).

**Without the pre-pass:** A form deleted in SAMMS would remain at `RowState = 1` in Silver forever, because the MERGE's `whenNotMatchedBySource` branch does not fire unless explicitly coded.

**Pre-pass decision table:**

| Condition | Reset Applied |
|---|---|
| `DateFilterEnabled = 0` (e.g., ReferralForm) | Unconditional: `SET RowState = 0` for ALL rows of this form at affected sites |
| `DateFilterEnabled = 1` | Date-gated: `SET RowState = 0` WHERE `CreatedOn >= wrkdt OR UpdatedOn >= wrkdt` AND `RowState = 1` |
| FormName like `TP-*` | Treated as date-filtered (same as DateFilterEnabled = 1) |
| Form not found in Forms2Process | Treated as date-filtered |

---

## 26. The 7-Column Primary Key — No RowChkSum

```
_site_code + FormName + FormId(UPPER) + ClientId + PreAdmissionId + QuestionId + QuestionOrderId
```

**FormId is always normalised to UPPER** — the old C# EF path explicitly calls `fqa.FormId.ToUpper()` before the in-memory PK lookup. Cell 1 of the Silver notebook applies `upper(col("FormId"))` during the Bronze read to match this behaviour.

**No RowChkSum:** `AnswerValue` is `nvarchar(max)` — `CHECKSUM()` on large text types is unreliable in SQL Server (can produce false matches). The old system never used a checksum for this table. Every matched Silver row gets a full column update.

---

## 27. Lookback Window — 30 Days and ReferralForm Exception

```
Old system:   DaysBack = -15  →  formDaysBack = DaysBack - 15 = -30
Fabric:       p_lookback_days = 30  (equivalent)

Reload:       p_reload = true  →  wrkdt = 2010-01-01
```

**Date columns checked in the base query:**
- `f.CreatedOn >= wrkdt`
- `ISNULL(f.UpdatedOn, f.CreatedOn) >= wrkdt`

**Date columns for Forms2Process tables** — the actual column names come from `tbl_Forms2Process.CreatedOn` and `tbl_Forms2Process.ModifiedOn` because they vary per table. The SQL builder notebook injects these directly: `CONVERT(date, a.{xf.CreatedOn})`.

**ReferralForm (`DateFilterEnabled = 0`):** No WHERE clause is appended. All rows are always extracted regardless of wrkdt. The notebook's `continue` statement on the `referralform` case skips the date filter append explicitly.

---

## 28. tblTP17REVIEW — Treatment Plan Special Case

| Property | Value |
|---|---|
| PA join | None — standalone table |
| FormName | Dynamic: `'TP-' + TprTYPE` |
| FormID | `8-{1-4}-ABS(tprCLTID)-tpRID-tprTPID` |
| Rows per record | 3 (or 4 if `tprReviewFrequency` column exists) |
| IsDeleted | `CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END` |
| Date filter | None |

**tprReviewFrequency handling:** Since the SQL builder notebook cannot probe `sys.all_columns` directly (no SAMMS connection), UNION D is always included in the generated SQL but is guarded by an `EXISTS` predicate against the source database's `sys.all_columns`. For example: `EXISTS (SELECT 1 FROM [SAMMS-ColoradoSpringsV5].sys.all_columns WHERE object_id=OBJECT_ID('[SAMMS-ColoradoSpringsV5].dbo.[tblTP17REVIEW]') AND name='tprReviewFrequency')`. If the column does not exist, the EXISTS check returns false and the UNION D produces zero rows — the query still succeeds.

**The 4 questions per Treatment Plan record:**

| QuestionID | QuestionText | AnswerValue |
|---|---|---|
| 1 | `Treatment Plan Type` | `TprTYPE` |
| 2 | `Treatment Phase Type` | `tpTreatmentPhase` |
| 3 | `Next Due` | `CONVERT(varchar, tprNEXT)` |
| 4 | `Review Frequency` | `RTRIM(SUBSTRING(..., 6, LEN-5))` if `>6 chars`, else raw |

---

## 29. tblORDERREQ — Level Justification Special Case

| Property | Value |
|---|---|
| PA join | None — standalone table |
| FormName | Hard-coded `'Level Justification'` |
| FormID | `9-{1-2}-ABS(cltID)-ReqNum-''` |
| Rows per record | 2 (EffectiveDate + ExpirationDate) |
| IsDeleted | `CASE WHEN cltID < 0 THEN 1 ELSE 0 END` |
| Date filter | None |
| Extraction filter | `Status = 'Approved'` AND exclude test records |

`PreAdmissionId` slot is populated with `ReqNum` — there is no actual pre-admission ID for these records.

---

## 30. Troubleshooting Guide

### Site skipped — no Form table

**Symptom:** Site produces no Bronze rows; `form_exists = 0` in the Lookup result.  
**Cause:** This clinic is on an older SAMMS version that predates the form engine.  
**Action:** Expected behaviour. No intervention needed unless the site has been upgraded.

---

### `lkp_get_existing_tables` Lookup fails

**Symptom:** Lookup activity fails with connection or timeout error.  
**Cause:** The SAMMS SQL Server for this site is unreachable through the on-prem gateway.  
**Fix:** Verify connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` is active. Check the gateway. Confirm `source_database` in `p_sites` matches the actual SQL Server database name exactly.

---

### `nb_forms_build_site_sql` produces empty SQL or fails

**Symptom:** Notebook exits with an empty string or raises an exception.  
**Cause 1:** `bhg_silver.ctrl.tbl_Forms2Process` is empty or not loaded.  
**Fix 1:** Populate the table from BHG_DR: `SELECT * FROM ctrl.tbl_Forms2Process WHERE Enabled=1 AND RowState=1` and load the result into the lakehouse.  
**Cause 2:** `p_existing_tables_json` is `"[]"` — Lookup 2 returned no rows.  
**Fix 2:** Run `SELECT name FROM sys.tables` manually against the SAMMS database to confirm it returns results.

---

### Copy activity (`cp_forms_to_bronze`) produces 0 rows

**Symptom:** Copy succeeds but writes 0 rows.  
**Cause:** The date lookback filter (`wrkdt`) excludes all rows. No form was created or updated within `p_lookback_days` days.  
**Fix:** Increase `p_lookback_days` to `90` or `365` for a deeper look. For a full historical load, set `p_reload = true`.

---

### Silver MERGE is slow

**Fix:** Z-order the Silver table on the most selective columns:
```python
spark.sql("""
    OPTIMIZE bhg_silver.pats.sl_tblFormQuestionAnswers
    ZORDER BY (_site_code, FormName, ClientId)
""")
```

---

### Silver rows with duplicate FormId (case mismatch)

**Symptom:** Same logical record appears twice — one with mixed-case `FormId`, one with UPPER.  
**Fix:** Run once to normalise:
```python
spark.sql("UPDATE bhg_silver.pats.sl_tblFormQuestionAnswers SET FormId = UPPER(FormId)")
```
Then re-run the Silver notebook to re-merge with consistent casing.

---

### ReferralForm rows stuck at RowState = 1 after deletion in SAMMS

**Cause:** `DateFilterEnabled = 0` for ReferralForm must be set in `bhg_silver.ctrl.tbl_Forms2Process`.  
**Fix:** Confirm `DateFilterEnabled = 0` for the `ReferralForm` row in the control table. The pre-pass unconditional reset only applies to forms where this flag is false.

---

*End of Implementation Guide*
