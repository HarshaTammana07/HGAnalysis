# 3rd-Party AR Notes & Claim Notes — Microsoft Fabric ETL Pipeline Implementation Guide

**Pipeline Name:** Notes Bronze → Silver ETL  
**Data:** 3rd-party AR notes (`tbl3pArnote`) and claim notes (`tbl3pClaimNote`) from SAMMS SQL Server clinic databases  
**Destination:** Microsoft Fabric Lakehouse (Bronze + Silver layers)  
**Covers:** Both `pats.tbl_3pARNOTE` and `pats.tbl_3pClaimNote` in a single pipeline  
**BHGTaskRunner Arg:** `7` (`SAMMS-ETL-Notes`)  
**Author Reference:** `Save3pElig.cs` → `Save3pArnote` / `Save3pClaimNote`, `BHGTaskRunner/Program.cs` lines 146–163, `Scheduler/Program.cs` line 27

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [Prerequisites — What You Need Before Starting](#3-prerequisites--what-you-need-before-starting)
4. [Pipeline Parameters — Define These First](#4-pipeline-parameters--define-these-first)
5. [Step 1 — Create the Data Pipeline in Fabric](#5-step-1--create-the-data-pipeline-in-fabric)
6. [Step 2 — Add the ForEach Activity](#6-step-2--add-the-foreach-activity)
7. [Step 3 — Inside ForEach: Add the Lookup Activity (globalBatchId Check)](#7-step-3--inside-foreach-add-the-lookup-activity-globalbatchid-check)
8. [Step 4 — Inside ForEach: Add Copy Activity 1 (ARNote Bronze Extraction)](#8-step-4--inside-foreach-add-copy-activity-1-arnote-bronze-extraction)
9. [Step 5 — Inside ForEach: Add Copy Activity 2 (ClaimNote Bronze Extraction)](#9-step-5--inside-foreach-add-copy-activity-2-claimnote-bronze-extraction)
10. [Step 6 — Add Notebook Activity 1 (ARNote Bronze → Silver)](#10-step-6--add-notebook-activity-1-arnote-bronze--silver)
11. [Step 7 — Add Notebook Activity 2 (ClaimNote Bronze → Silver)](#11-step-7--add-notebook-activity-2-claimnote-bronze--silver)
12. [Step 8 — Create Notebook: nb_3parnote_bronze_to_silver](#12-step-8--create-notebook-nb_3parnote_bronze_to_silver)
13. [Step 9 — Create Notebook: nb_3pclaimnote_bronze_to_silver](#13-step-9--create-notebook-nb_3pclaimnote_bronze_to_silver)
14. [End-to-End Flow Summary](#14-end-to-end-flow-summary)
15. [How Change Detection Works (RowChkSum)](#15-how-change-detection-works-rowchksum)
16. [RowState Logic — Active Records Only](#16-rowstate-logic--active-records-only)
17. [Why the WHERE Clause Has a 2023 Floor AND a Rolling Window](#17-why-the-where-clause-has-a-2023-floor-and-a-rolling-window)
18. [The globalBatchId LAB-Site Special Handling](#18-the-globalbatchid-lab-site-special-handling)
19. [The ClaimNote Match-Key Anomaly (TpcnTpcid vs Tpcn)](#19-the-claimnote-match-key-anomaly-tpcntpcid-vs-tpcn)
20. [Known Anomalies and Cautions](#20-known-anomalies-and-cautions)
21. [Troubleshooting Guide](#21-troubleshooting-guide)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Microsoft Fabric Data Pipeline                    │
│                    pl_notes_samms_to_lakehouse                       │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  ForEach Site (fe_each_samms_site)                          │   │
│  │  Iterates over every clinic in p_sites parameter            │   │
│  │                                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 1              │                               │   │
│  │  │ lkp_check_globalbatch   │  ← Query SAMMS sys.columns   │   │
│  │  │ _id_exists (Lookup)     │    Does globalBatchId exist?  │   │
│  │  └────────────┬────────────┘                               │   │
│  │               │ Succeeded                                   │   │
│  │               ▼                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 2              │                               │   │
│  │  │ cp_arnote_to_bronze     │  ← SELECT all tbl3pArnote    │   │
│  │  │ (Copy)                  │    WHERE date lookback        │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │    Notes.br_tbl3pArnote       │   │
│  │  └────────────┬────────────┘                               │   │
│  │               │ Succeeded                                   │   │
│  │               ▼                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 3              │                               │   │
│  │  │ cp_claimnote_to_bronze  │  ← SELECT all tbl3pClaimNote │   │
│  │  │ (Copy)                  │    WHERE date lookback        │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │    Notes.br_tbl3pClaimNote    │   │
│  │  └─────────────────────────┘                               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                         │ ForEach Succeeded                        │
│                         ▼                                          │
│  ┌──────────────────────────────────┐                              │
│  │  nb_3parnote_bronze_to_silver    │  Cell 1: Load Bronze → dedup │
│  │  (Notebook — ARNote)             │  Cell 2: Delta MERGE → Silver│
│  └──────────────────────────────────┘                              │
│                         │ Succeeded                                │
│                         ▼                                          │
│  ┌──────────────────────────────────┐                              │
│  │  nb_3pclaimnote_bronze_to_silver │  Cell 1: Load Bronze → dedup │
│  │  (Notebook — ClaimNote)          │  Cell 2: Delta MERGE → Silver│
│  └──────────────────────────────────┘                              │
└─────────────────────────────────────────────────────────────────────┘
```

**Lakehouse Targets:**

| Layer  | Lakehouse Name | Schema  | Table                  |
| ------ | -------------- | ------- | ---------------------- |
| Bronze | `bhg_bronze`   | `Notes` | `br_tbl3pArnote`       |
| Bronze | `bhg_bronze`   | `Notes` | `br_tbl3pClaimNote`    |
| Silver | `bhg_silver`   | `pats`  | `sl_tbl_3pARNOTE`      |
| Silver | `bhg_silver`   | `pats`  | `sl_tbl_3pClaimNote`   |

---

## 2. What This Pipeline Does — Plain English

The SAMMS system records third-party billing activity for each clinic. Two tables track note-level detail:

- `tbl3pArnote` — one row per AR (Accounts Receivable) note on a third-party billing line item. Tracks who added a note, when, whether it was removed, and free-text content.
- `tbl3pClaimNote` — one row per note on a third-party claim. Tracks tickler dates, note type, who added the note, and free-text content.

There are 80+ clinics, each with their own SAMMS database.

This pipeline:

1. **Visits each clinic's SAMMS database** one at a time (ForEach loop)
2. **Checks** whether that clinic's SAMMS version has a `globalBatchId` column — the LAB site does not have it, and including it would cause the query to fail
3. **Extracts AR note records** within the rolling lookback window (15 days back, 2023 floor, plus OR on `arnDtRemoved`)
4. **Extracts claim note records** within the rolling lookback window (15 days back, 2023 floor, plus OR on `tpcnDtTickler`)
5. **Appends** both extracts to their respective Bronze tables, tagged with a run ID
6. After all clinics are done, runs **two notebooks** that merge Bronze into Silver — one for ARNote, one for ClaimNote — using RowChkSum for change detection

---

## 3. Prerequisites — What You Need Before Starting

| Item                           | What It Is                                                     | Where It Lives                 |
| ------------------------------ | -------------------------------------------------------------- | ------------------------------ |
| SAMMS SQL Server connection    | A Fabric connection to the on-premise SAMMS SQL Server gateway | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse         | Bronze layer Lakehouse                                         | Fabric workspace               |
| `bhg_silver` Lakehouse         | Silver layer Lakehouse                                         | Fabric workspace               |
| `Notes` schema in `bhg_bronze` | Schema for Notes Bronze tables                                 | `bhg_bronze` Lakehouse         |
| `pats` schema in `bhg_silver`  | Schema for Silver patient tables                               | `bhg_silver` Lakehouse         |
| On-premise data gateway        | Configured to reach SAMMS SQL Servers                          | Fabric settings                |

**Connection ID to note down:** When you create the SAMMS SQL Server connection in Fabric, it gets a GUID. In this implementation the connection ID is:

```
9743b95a-fd66-4f7c-9767-e6eb0f1ecab7
```

Replace this with your actual connection GUID everywhere it appears.

**Workspace ID:**

```
c5097ffb-b78e-441d-9575-a82bac23cac8
```

**Bronze Lakehouse Artifact ID:**

```
77d24027-6a1c-43a8-a998-1a14dd3c0d52
```

---

## 4. Pipeline Parameters — Define These First

Go to: **Pipeline canvas → click empty space → Parameters tab → + New**

### Parameter 1: `p_ingest_run_id`

| Setting       | Value             |
| ------------- | ----------------- |
| Name          | `p_ingest_run_id` |
| Type          | `String`          |
| Default Value | `test-run-001`    |

**Why:** Every Bronze row written by this pipeline run carries this ID in the `_ingest_run_id` column. The Silver notebooks filter Bronze to only this run's rows. In production pass something like `NOTES-2026-05-20-001`.

---

### Parameter 2: `p_lookback_days`

| Setting       | Value             |
| ------------- | ----------------- |
| Name          | `p_lookback_days` |
| Type          | `Int`             |
| Default Value | `15`              |

**Why:** This is the `DaysBack = -15` constant from `BHGTaskRunner/Program.cs` line 136. It is used to compute `@WorkDate` = today minus 15 days, which becomes the rolling anchor in the WHERE clause. Do not change this unless you have a specific reason.

---

### Parameter 3: `p_sites`

| Setting       | Value          |
| ------------- | -------------- |
| Name          | `p_sites`      |
| Type          | `Array`        |
| Default Value | See JSON below |

**Default Value (single-database test — paste this exactly):**

```json
[
  {
    "site_code": "AHK",
    "source_database": "SAMMS-AHK",
    "source_schema": "dbo",
    "source_table_arnote": "tbl3pArnote",
    "source_table_claimnote": "tbl3pClaimNote"
  }
]
```

**Why:** This array controls which clinics the ForEach loop processes. For now it contains a single clinic for testing. After the single-database test passes, add all 80+ clinics.

Each object has five properties:

| Property                 | Meaning                                                              | Example          |
| ------------------------ | -------------------------------------------------------------------- | ---------------- |
| `site_code`              | Clinic identifier — tagged on every Bronze row                       | `AHK`            |
| `source_database`        | Exact database name on the SAMMS SQL Server                          | `SAMMS-AHK`      |
| `source_schema`          | Schema where source tables live (always `dbo`)                       | `dbo`            |
| `source_table_arnote`    | Source AR note table name                                            | `tbl3pArnote`    |
| `source_table_claimnote` | Source claim note table name                                         | `tbl3pClaimNote` |

> **LAB site note:** The LAB SAMMS database does not have a `globalBatchId` column. When adding the LAB site to `p_sites`, still use the same object shape — the Lookup activity handles the conditional column automatically. See [Section 18](#18-the-globalbatchid-lab-site-special-handling).

---

## 5. Step 1 — Create the Data Pipeline in Fabric

1. Open your Fabric workspace
2. Click **+ New** → **Data pipeline**
3. Name it: `pl_notes_samms_to_lakehouse`
4. Click **Create**
5. On the pipeline canvas, click empty space → **Parameters** tab → add all three parameters from Section 4

---

## 6. Step 2 — Add the ForEach Activity

### Add the activity

1. From the **Activities** toolbar, drag a **ForEach** activity onto the canvas
2. Rename it: `fe_each_samms_site`

### Configure ForEach Settings tab

| Setting     | Value                            | Why                                                                                  |
| ----------- | -------------------------------- | ------------------------------------------------------------------------------------ |
| Items       | `@pipeline().parameters.p_sites` | Loop over the sites array                                                            |
| Sequential  | **Checked (True)**               | Process one clinic at a time — SAMMS on-premise servers cannot handle parallel loads |
| Batch count | Leave blank                      | Not needed when Sequential is true                                                   |

### JSON for reference

```json
{
    "name": "fe_each_samms_site",
    "type": "ForEach",
    "dependsOn": [],
    "typeProperties": {
        "items": {
            "value": "@pipeline().parameters.p_sites",
            "type": "Expression"
        },
        "isSequential": true,
        "activities": []
    }
}
```

---

## 7. Step 3 — Inside ForEach: Add the Lookup Activity (globalBatchId Check)

### Why this activity exists

The original C# ETL in `BHGTaskRunner/Program.cs` lines 147–150 and 156–159 does this:

```csharp
if (st.SiteCode == "Lab")
{
    strCmd = strCmd.Replace(", [globalBatchId] globalBatchId", "").Replace(", [globalBatchId]", "");
}
```

The LAB SAMMS database does not have a `globalBatchId` column in `tbl3pArnote` or `tbl3pClaimNote`. If you include it in the SELECT for LAB, the query fails with "invalid column name". Rather than hardcoding the LAB exception, the Fabric approach uses a Lookup to check whether `globalBatchId` exists in the source table for this clinic — the same pattern used for `InventoryGroup` in the Dose pipeline. This makes the logic work automatically for any future site that also lacks the column.

### Add the activity

1. Click **Edit** on the ForEach activity to open its inner canvas
2. Drag a **Lookup** activity onto the inner canvas
3. Rename it: `lkp_check_globalbatchid_exists`

### Configure Settings tab

| Setting        | Value                                                                      |
| -------------- | -------------------------------------------------------------------------- |
| Source dataset | SQL Server dataset using connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query      | **Query**                                                                  |
| Query          | Expression below                                                           |

### Query — paste into Query field (Expression mode)

```
@concat(
'SELECT globalbatchid_exists = COUNT(1)
FROM [', item().source_database, '].sys.columns c
INNER JOIN [', item().source_database, '].sys.tables t
    ON c.object_id = t.object_id
INNER JOIN [', item().source_database, '].sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = ''', item().source_schema, '''
  AND t.name = ''', item().source_table_arnote, '''
  AND c.name = ''globalBatchId'';'
)
```

**What it returns:** A single row: `globalbatchid_exists` = `1` if the column exists, `0` if not.

**How to reference the result later:**

```
activity('lkp_check_globalbatchid_exists').output.firstRow.globalbatchid_exists
```

### Configure Policy tab

| Setting        | Value        |
| -------------- | ------------ |
| Timeout        | `0.12:00:00` |
| Retry          | `0`          |
| Retry interval | `30` seconds |

### JSON for reference

```json
{
    "name": "lkp_check_globalbatchid_exists",
    "type": "Lookup",
    "dependsOn": [],
    "policy": {
        "timeout": "0.12:00:00",
        "retry": 0,
        "retryIntervalInSeconds": 30,
        "secureOutput": false,
        "secureInput": false
    },
    "typeProperties": {
        "source": {
            "type": "SqlServerSource",
            "sqlReaderQuery": {
                "value": "@concat(\n'SELECT globalbatchid_exists = COUNT(1)\nFROM [', item().source_database, '].sys.columns c\nINNER JOIN [', item().source_database, '].sys.tables t\n    ON c.object_id = t.object_id\nINNER JOIN [', item().source_database, '].sys.schemas s\n    ON t.schema_id = s.schema_id\nWHERE s.name = ''', item().source_schema, '''\n  AND t.name = ''', item().source_table_arnote, '''\n  AND c.name = ''globalBatchId'';'\n)",
                "type": "Expression"
            },
            "queryTimeout": "02:00:00",
            "partitionOption": "None"
        },
        "datasetSettings": {
            "annotations": [],
            "type": "SqlServerTable",
            "schema": [],
            "externalReferences": {
                "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7"
            }
        }
    }
}
```

---

## 8. Step 4 — Inside ForEach: Add Copy Activity 1 (ARNote Bronze Extraction)

### What it does

- Connects to the clinic's SAMMS SQL Server
- Runs a SELECT query with the rolling lookback WHERE condition for `tbl3pArnote`
- Appends rows to Bronze table `bhg_bronze.Notes.br_tbl3pArnote`
- Adds metadata columns to every row

### Add the activity

1. Still on the inner ForEach canvas
2. Drag a **Copy** activity
3. Rename it: `cp_arnote_to_bronze`
4. Draw arrow: `lkp_check_globalbatchid_exists` → `cp_arnote_to_bronze` with condition **Succeeded**

### Configure Source tab

| Setting       | Value                                  |
| ------------- | -------------------------------------- |
| Source type   | SQL Server                             |
| Connection    | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query     | **Query** (Expression mode)            |
| Query timeout | `02:00:00`                             |

### The Source Query Expression — paste this into Query field

```
@concat(
'SELECT
    ''', item().site_code, ''' AS _site_code,
    ''', item().source_database, ''' AS _source_database,
    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,
    GETDATE() AS _extracted_at,
    CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())) AS _source_query_date_anchor,

    arnID,
    arnLIID,
    arnNOTE,
    arnUSER,
    arnDATE,
    arnDtRemoved,
    arnStrRemovedReason,
    arnStrRemovedUser,
    bid,
    arnDBnotes,
    ',
if(
    equals(activity('lkp_check_globalbatchid_exists').output.firstRow.globalbatchid_exists, 1),
    'globalBatchId',
    'CAST(NULL AS bigint) AS globalBatchId'
),
',

    RowChkSum = CHECKSUM(
        arnID,
        arnLIID,
        arnNOTE,
        arnUSER,
        arnDATE,
        arnDtRemoved,
        arnStrRemovedReason,
        arnStrRemovedUser,
        bid,
        arnDBnotes,
        ',
if(
    equals(activity('lkp_check_globalbatchid_exists').output.firstRow.globalbatchid_exists, 1),
    'globalBatchId',
    'CAST(NULL AS bigint)'
),
'
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table_arnote, '
WHERE (arnDATE >= ''1/1/2023'' AND arnDATE >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())))
   OR arnDtRemoved >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
ORDER BY arnID'
)
```

### Breaking Down What This Query Does

#### Metadata columns (first 5)

```sql
'AHK'                                   AS _site_code,
'SAMMS-AHK'                             AS _source_database,
'NOTES-2026-05-20-001'                  AS _ingest_run_id,
GETDATE()                               AS _extracted_at,
CONVERT(date, DATEADD(day,-15,GETDATE())) AS _source_query_date_anchor
```

#### Data columns (10 columns from tbl3pArnote)

All clinical columns from `tbl3pArnote`. These map directly to `pats.tbl_3pARNOTE` in Azure/Silver.

Column-to-destination mapping (from `dms.tbl_MapSrc2Dsn` ActionKey=1, StepKey=35):

| Source Field       | Destination Field      | PrimaryKey |
| ------------------ | ---------------------- | ---------- |
| `@SiteCode`        | `SiteCode`             | 1 (PK)     |
| `arnID`            | `arnID`                | 2 (PK)     |
| `arnLIID`          | `arnLIID`              | —          |
| `arnNOTE`          | `arnNOTE`              | —          |
| `arnUSER`          | `arnUSER`              | —          |
| `arnDATE`          | `arnDATE`              | —          |
| `arnDtRemoved`     | `arnDtRemoved`         | —          |
| `arnStrRemovedReason` | `arnStrRemovedReason` | —         |
| `arnStrRemovedUser`   | `arnStrRemovedUser`   | —         |
| `bid`              | `bid`                  | —          |
| `arnDBnotes`       | `arnDBnotes`           | —          |
| `globalBatchId`    | `globalBatchId`        | —          |
| *(computed)*       | `RowChkSum`            | —          |
| *(set by Save)*    | `RowState`             | —          |

#### globalBatchId — conditional column

```
if(globalbatchid_exists = 1,
    'globalBatchId',
    'CAST(NULL AS bigint) AS globalBatchId'
)
```

The Bronze table always has a `globalBatchId` column. For the LAB site (and any other site that lacks it), the value is NULL. The CHECKSUM computation uses the same conditional so the checksum is consistent regardless.

#### RowChkSum — change detection fingerprint

Computed over all 10 data fields including `globalBatchId` (or NULL for LAB). See [Section 15](#15-how-change-detection-works-rowchksum) for full details.

#### WHERE clause — lookback filter (exact match to Scheduler WhereCondition)

```sql
WHERE (arnDATE >= '1/1/2023' AND arnDATE >= CONVERT(date, DATEADD(day, -15, GETDATE())))
   OR arnDtRemoved >= CONVERT(date, DATEADD(day, -15, GETDATE()))
```

This is the **exact** `WhereCondition` from `dms.vw_MapAction` for ActionKey=1, StepKey=35, with `@WorkDate` substituted as `DATEADD(day, -15, GETDATE())`. See [Section 17](#17-why-the-where-clause-has-a-2023-floor-and-a-rolling-window) for a full explanation.

### Configure Sink tab

| Setting                 | Value                                  |
| ----------------------- | -------------------------------------- |
| Sink type               | **Lakehouse**                          |
| Linked service name     | `bhg_bronze`                           |
| Workspace ID            | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder             | `Tables`                               |
| Schema                  | `Notes`                                |
| Table                   | `br_tbl3pArnote`                       |
| Table action            | **Append**                             |
| Apply V-Order           | No (unchecked)                         |

### Configure Mapping / Translator tab

| Setting                 | Value       |
| ----------------------- | ----------- |
| Type conversion         | **Enabled** |
| Allow data truncation   | **True**    |
| Treat boolean as number | **False**   |

### Configure Policy tab

| Setting        | Value        |
| -------------- | ------------ |
| Timeout        | `0.12:00:00` |
| Retry          | `0`          |
| Retry interval | `30` seconds |

---

## 9. Step 5 — Inside ForEach: Add Copy Activity 2 (ClaimNote Bronze Extraction)

### What it does

- Connects to the same clinic's SAMMS SQL Server
- Runs a SELECT query with the rolling lookback WHERE condition for `tbl3pClaimNote`
- Appends rows to Bronze table `bhg_bronze.Notes.br_tbl3pClaimNote`

### Add the activity

1. Still on the inner ForEach canvas
2. Drag a **Copy** activity
3. Rename it: `cp_claimnote_to_bronze`
4. Draw arrow: `cp_arnote_to_bronze` → `cp_claimnote_to_bronze` with condition **Succeeded**

> **Why after ARNote copy, not in parallel?** SAMMS on-premise servers are limited. Running two simultaneous SELECTs against the same clinic database adds unnecessary load. Sequential is safer.

### Configure Source tab

| Setting       | Value                                  |
| ------------- | -------------------------------------- |
| Source type   | SQL Server                             |
| Connection    | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query     | **Query** (Expression mode)            |
| Query timeout | `02:00:00`                             |

### The Source Query Expression

```
@concat(
'SELECT
    ''', item().site_code, ''' AS _site_code,
    ''', item().source_database, ''' AS _source_database,
    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,
    GETDATE() AS _extracted_at,
    CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())) AS _source_query_date_anchor,

    tpcn,
    tpcnTPCID,
    tpcnDtmAdded,
    tpcnStrAdded,
    tpcnStrNote,
    tpcnStrType,
    tpcnDtTickler,
    tpcnDtTicklerRemoved,
    tpcnStrTicklerRemovedNote,
    tpcnStrTicklerRemovedUser,
    tpcnStrTicklerType,
    ',
if(
    equals(activity('lkp_check_globalbatchid_exists').output.firstRow.globalbatchid_exists, 1),
    'globalBatchId',
    'CAST(NULL AS bigint) AS globalBatchId'
),
',

    RowChkSum = CHECKSUM(
        tpcn,
        tpcnTPCID,
        tpcnDtmAdded,
        tpcnStrAdded,
        tpcnStrNote,
        tpcnStrType,
        tpcnDtTickler,
        tpcnDtTicklerRemoved,
        tpcnStrTicklerRemovedNote,
        tpcnStrTicklerRemovedUser,
        tpcnStrTicklerType,
        ',
if(
    equals(activity('lkp_check_globalbatchid_exists').output.firstRow.globalbatchid_exists, 1),
    'globalBatchId',
    'CAST(NULL AS bigint)'
),
'
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table_claimnote, '
WHERE (tpcnDtmAdded >= ''1/1/2023'' AND tpcnDtmAdded >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())))
   OR tpcnDtTickler >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
ORDER BY tpcn'
)
```

### Column-to-destination mapping for tbl3pClaimNote

From `dms.tbl_MapSrc2Dsn` ActionKey=1, StepKey=34:

| Source Field              | Destination Field           | PrimaryKey |
| ------------------------- | --------------------------- | ---------- |
| `@SiteCode`               | `SiteCode`                  | 1 (PK)     |
| `tpcn`                    | `tpcn`                      | 2 (PK)     |
| `tpcnTPCID`               | `tpcnTPCID`                 | —          |
| `tpcnDtmAdded`            | `tpcnDtmAdded`              | —          |
| `tpcnStrAdded`            | `tpcnStrAdded`              | —          |
| `tpcnStrNote`             | `tpcnStrNote`               | —          |
| `tpcnStrType`             | `tpcnStrType`               | —          |
| `tpcnDtTickler`           | `tpcnDtTickler`             | —          |
| `tpcnDtTicklerRemoved`    | `tpcnDtTicklerRemoved`      | —          |
| `tpcnStrTicklerRemovedNote`  | `tpcnStrTicklerRemovedNote` | —        |
| `tpcnStrTicklerRemovedUser`  | `tpcnStrTicklerRemovedUser` | —        |
| `tpcnStrTicklerType`      | `tpcnStrTicklerType`        | —          |
| `globalBatchId`           | `globalBatchId`             | —          |
| *(computed)*              | `RowChkSum`                 | —          |
| *(set by Save)*           | `RowState`                  | —          |

> **Critical — Azure PK vs Match Key:** The Azure PK for `pats.tbl_3pClaimNote` is `(SiteCode, tpcn)`. However the C# upsert in `Save3pClaimNote` (line 376) matches on `TpcnTpcid`, NOT on `tpcn`. See [Section 19](#19-the-claimnote-match-key-anomaly-tpcntpcid-vs-tpcn) for the full explanation and what this means for the Fabric MERGE.

### Configure Sink tab

| Setting                 | Value                                  |
| ----------------------- | -------------------------------------- |
| Sink type               | **Lakehouse**                          |
| Linked service name     | `bhg_bronze`                           |
| Workspace ID            | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder             | `Tables`                               |
| Schema                  | `Notes`                                |
| Table                   | `br_tbl3pClaimNote`                    |
| Table action            | **Append**                             |
| Apply V-Order           | No (unchecked)                         |

### Configure Mapping / Translator tab

| Setting                 | Value       |
| ----------------------- | ----------- |
| Type conversion         | **Enabled** |
| Allow data truncation   | **True**    |
| Treat boolean as number | **False**   |

### Configure Policy tab

| Setting        | Value        |
| -------------- | ------------ |
| Timeout        | `0.12:00:00` |
| Retry          | `0`          |
| Retry interval | `30` seconds |

---

## 10. Step 6 — Add Notebook Activity 1 (ARNote Bronze → Silver)

### Add the activity

1. Go back to the **main pipeline canvas** (outside ForEach)
2. Drag a **Notebook** activity
3. Rename it: `nb_3parnote_bronze_to_silver`
4. Draw arrow: `fe_each_samms_site` → `nb_3parnote_bronze_to_silver` with condition **Succeeded**

### Configure Settings tab

| Setting   | Value                                                  |
| --------- | ------------------------------------------------------ |
| Notebook  | Select `nb_3parnote_bronze_to_silver` (create in Step 8) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8`                 |

### Configure Parameters

| Parameter Name    | Type   | Value                                                 |
| ----------------- | ------ | ----------------------------------------------------- |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |

---

## 11. Step 7 — Add Notebook Activity 2 (ClaimNote Bronze → Silver)

### Add the activity

1. Still on the **main pipeline canvas**
2. Drag another **Notebook** activity
3. Rename it: `nb_3pclaimnote_bronze_to_silver`
4. Draw arrow: `nb_3parnote_bronze_to_silver` → `nb_3pclaimnote_bronze_to_silver` with condition **Succeeded**

> **Why sequential notebooks?** ClaimNote does not depend on ARNote results. However running them sequentially avoids Spark cluster contention during development. Once tested you may run them in parallel if needed.

### Configure Settings tab

| Setting   | Value                                                       |
| --------- | ----------------------------------------------------------- |
| Notebook  | Select `nb_3pclaimnote_bronze_to_silver` (create in Step 9) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8`                      |

### Configure Parameters

| Parameter Name    | Type   | Value                                                 |
| ----------------- | ------ | ----------------------------------------------------- |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |

---

## 12. Step 8 — Create Notebook: nb_3parnote_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_3parnote_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` Lakehouses in the notebook's Lakehouse panel

---

### Cell 1 — Load Bronze and Prepare Silver Source

```python
from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window

# Pipeline passes p_ingest_run_id as a parameter.
# The try/except lets you run this manually during development.
try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Notes.br_tbl3pArnote"
silver_table = "bhg_silver.pats.sl_tbl_3pARNOTE"

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

# Read only rows from THIS pipeline run.
bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze rows found for ingest_run_id = {p_ingest_run_id}")

# Deduplicate within current run.
# Business key = _site_code + arnID   (mirrors Azure PK: SiteCode, ArnId)
# If the same record appears twice (e.g. due to a retry), keep the latest extraction.
w = Window.partitionBy("_site_code", "arnID").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("arnID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # ── RowState Logic ─────────────────────────────────────────────────────────
    # Save3pArnote always sets RowState = true for every incoming row.
    # There is NO pre-reset and NO soft-delete condition for ARNote.
    # C# reference: ar.RowState = true  (set in the "sitecode" case, line 452)
    # Any record returned from SAMMS is considered active.
    .withColumn("RowState", lit(True))

    # Silver audit timestamps
    .withColumn("silver_updated_at", current_timestamp())
    .withColumn("last_seen_at", current_timestamp())
)

src_df.createOrReplaceTempView("vw_arnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ARNote Silver: {src_count}")

# First-ever run: create Silver table
if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write
        .format("delta")
        .mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created ARNote Silver table and inserted rows: {src_count}")
else:
    print(f"Silver table already exists: {silver_table}")
```

### Explanation of Cell 1 — Key Decisions

| Code Section                             | Why It Exists                                                                                     |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `try: p_ingest_run_id`                   | Lets you run the notebook manually without the pipeline for testing                               |
| Filter on `_ingest_run_id`               | Bronze accumulates rows from every run — filter to just this run's rows                           |
| `if bronze_count == 0: raise Exception`  | Fail loudly if Bronze is empty — catches upstream failures before a silent empty Silver update    |
| `Window.partitionBy("_site_code","arnID")` | Deduplicates in case a retry wrote the same record twice                                        |
| `RowState = lit(True)`                   | Mirrors `ar.RowState = true` in `Save3pArnote` — ALL incoming rows are active, no exceptions     |
| `silver_created_at` on initial write     | Set once, never overwritten — records when this row first appeared in Silver                      |

---

### Cell 2 — Merge Into ARNote Silver Delta Table

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tbl_3pARNOTE"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = src_df.columns

# Update all columns EXCEPT silver_created_at (preserve original creation timestamp)
update_cols = [c for c in src_cols if c != "silver_created_at"]
update_set  = {c: f"src.{c}" for c in update_cols}

# Always use server time for audit timestamps on update
update_set["silver_updated_at"] = "current_timestamp()"
update_set["last_seen_at"]      = "current_timestamp()"

# On insert: write all columns including silver_created_at
insert_values = {c: f"src.{c}" for c in src_cols}

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # Merge key: clinic + note ID (mirrors Azure PK: SiteCode + ArnId)
        "tgt._site_code = src._site_code AND tgt.arnID = src.arnID"
    )

    # CASE 1: Record exists AND data changed (RowChkSum differs or NULL on either side)
    # → Full update of all data columns
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: Record exists AND data has NOT changed (RowChkSum identical)
    # → Lightweight metadata-only update. No data columns touched.
    # → Still refresh RowState (defensive — ARNote RowState is always true, but consistent)
    .whenMatchedUpdate(
        condition="tgt.RowChkSum = src.RowChkSum",
        set={
            "last_seen_at":              "current_timestamp()",
            "RowState":                  "src.RowState",
            "_ingest_run_id":            "src._ingest_run_id",
            "_extracted_at":             "src._extracted_at",
            "_source_query_date_anchor": "src._source_query_date_anchor"
        }
    )

    # CASE 3: New record not yet in Silver → Insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ARNote Silver MERGE completed successfully.")
```

### Explanation of the Three MERGE Branches

#### Branch 1 — Full update (RowChkSum changed)

An AR note already exists in Silver but at least one column changed in SAMMS (note text edited, removal date set, user updated, etc.). All data columns are overwritten. `silver_created_at` is NOT overwritten.

#### Branch 2 — Metadata-only update (RowChkSum identical)

The AR note is unchanged. It appeared in the extract because its `arnDATE` or `arnDtRemoved` fell within the lookback window. No data columns are rewritten — this avoids unnecessary Delta table write amplification.

#### Branch 3 — New insert

The `_site_code + arnID` combination has never been seen in Silver. Insert the complete row including `silver_created_at`.

---

## 13. Step 9 — Create Notebook: nb_3pclaimnote_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_3pclaimnote_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` Lakehouses

---

### Cell 1 — Load Bronze and Prepare Silver Source

```python
from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Notes.br_tbl3pClaimNote"
silver_table = "bhg_silver.pats.sl_tbl_3pClaimNote"

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze ClaimNote rows found for ingest_run_id = {p_ingest_run_id}")

# ── CRITICAL: Deduplicate on _site_code + tpcnTPCID ─────────────────────────
# The C# Save3pClaimNote matches existing records by TpcnTpcid, NOT by tpcn
# (the Azure PK). See Section 19 for full explanation.
# For Fabric, we use the same match logic: _site_code + tpcnTPCID.
w = Window.partitionBy("_site_code", "tpcnTPCID").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("tpcnTPCID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # ── RowState Logic ─────────────────────────────────────────────────────────
    # Save3pClaimNote always sets RowState = true for every incoming row.
    # There is NO pre-reset and NO soft-delete condition for ClaimNote.
    # C# reference: claimNote.RowState = true  (set in the "sitecode" case, line 324)
    .withColumn("RowState", lit(True))

    .withColumn("silver_updated_at", current_timestamp())
    .withColumn("last_seen_at", current_timestamp())
)

src_df.createOrReplaceTempView("vw_claimnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ClaimNote Silver: {src_count}")

if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write
        .format("delta")
        .mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created ClaimNote Silver table: {src_count}")
else:
    print(f"Silver table exists: {silver_table}")
```

---

### Cell 2 — Merge Into ClaimNote Silver Delta Table

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tbl_3pClaimNote"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = src_df.columns

update_cols = [c for c in src_cols if c != "silver_created_at"]
update_set  = {c: f"src.{c}" for c in update_cols}
update_set["silver_updated_at"] = "current_timestamp()"
update_set["last_seen_at"]      = "current_timestamp()"

insert_values = {c: f"src.{c}" for c in src_cols}

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # ── CRITICAL: Match on tpcnTPCID not tpcn ──────────────────────────────
        # The C# Save3pClaimNote line 376:
        #   tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid)
        # Uses TpcnTpcid as the match key, not the Azure PK (tpcn).
        # Fabric MERGE must mirror this: _site_code + tpcnTPCID
        "tgt._site_code = src._site_code AND tgt.tpcnTPCID = src.tpcnTPCID"
    )

    # CASE 1: Record exists AND data changed → Full update
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: Record exists AND data unchanged → Metadata-only update
    .whenMatchedUpdate(
        condition="tgt.RowChkSum = src.RowChkSum",
        set={
            "last_seen_at":              "current_timestamp()",
            "RowState":                  "src.RowState",
            "_ingest_run_id":            "src._ingest_run_id",
            "_extracted_at":             "src._extracted_at",
            "_source_query_date_anchor": "src._source_query_date_anchor"
        }
    )

    # CASE 3: New record → Insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ClaimNote Silver MERGE completed successfully.")
```

---

## 14. End-to-End Flow Summary

```
TRIGGER: Pipeline runs with parameters:
  p_ingest_run_id  = "NOTES-2026-05-20-001"
  p_lookback_days  = 15
  p_sites          = [ { site_code: "AHK",
                         source_database: "SAMMS-AHK",
                         source_schema: "dbo",
                         source_table_arnote: "tbl3pArnote",
                         source_table_claimnote: "tbl3pClaimNote" } ]

STEP 1 — ForEach begins
  ↓ Current item: { site_code: "AHK", source_database: "SAMMS-AHK", ... }

STEP 2 — lkp_check_globalbatchid_exists (Lookup)
  → Queries SAMMS-AHK sys.columns for globalBatchId in tbl3pArnote
  → Returns: { globalbatchid_exists: 1 }   (or 0 for LAB site)

STEP 3 — cp_arnote_to_bronze (Copy Activity)
  → Builds dynamic SELECT from item() values + Lookup result
  → WHERE: (arnDATE >= '1/1/2023' AND arnDATE >= today - 15 days)
           OR arnDtRemoved >= today - 15 days
  → APPENDs rows tagged with _ingest_run_id = "NOTES-2026-05-20-001" to:
     bhg_bronze.Notes.br_tbl3pArnote

STEP 4 — cp_claimnote_to_bronze (Copy Activity)
  → Builds dynamic SELECT from item() values + Lookup result
  → WHERE: (tpcnDtmAdded >= '1/1/2023' AND tpcnDtmAdded >= today - 15 days)
           OR tpcnDtTickler >= today - 15 days
  → APPENDs rows tagged with _ingest_run_id = "NOTES-2026-05-20-001" to:
     bhg_bronze.Notes.br_tbl3pClaimNote

STEP 5 — ForEach moves to next site and repeats Steps 2–4
  ... until all sites done ...

STEP 6 — ForEach completes

STEP 7 — nb_3parnote_bronze_to_silver (Cell 1)
  → Reads br_tbl3pArnote WHERE _ingest_run_id = "NOTES-2026-05-20-001"
  → Deduplicates on _site_code + arnID
  → Sets RowState = true for all rows (no exceptions)
  → Adds silver_updated_at, last_seen_at
  → Creates Silver table on first run

STEP 8 — nb_3parnote_bronze_to_silver (Cell 2)
  → Delta MERGE into bhg_silver.pats.sl_tbl_3pARNOTE
     RowChkSum changed   → FULL UPDATE of all data columns
     RowChkSum same      → lightweight update (last_seen_at + RowState only)
     New record          → INSERT all columns

STEP 9 — nb_3pclaimnote_bronze_to_silver (Cell 1 + Cell 2)
  → Same pattern for tbl3pClaimNote
  → Deduplicate on _site_code + tpcnTPCID  ← NOT tpcn (see Section 19)
  → All incoming rows get RowState = true
  → Delta MERGE into bhg_silver.pats.sl_tbl_3pClaimNote

DONE: Both Silver tables are current for all changes within the lookback window.
```

---

## 15. How Change Detection Works (RowChkSum)

SQL Server's `CHECKSUM()` function computes a single integer from a list of column values. If any column changes, the integer changes. This is the same mechanism used in the original C# ETL.

### CHECKSUM columns for tbl3pArnote

| Included in CHECKSUM                                           | Excluded                  | Reason for exclusion                     |
| -------------------------------------------------------------- | ------------------------- | ---------------------------------------- |
| `arnID`, `arnLIID`, `arnNOTE`, `arnUSER`                       | `@SiteCode` (injected)    | Constant — injected by C#, not from source column |
| `arnDATE`, `arnDtRemoved`, `arnStrRemovedReason`               | `RowState` (derived)      | Set by Save method, not from source      |
| `arnStrRemovedUser`, `bid`, `arnDBnotes`, `globalBatchId`      | Pipeline metadata columns | Added by pipeline, not from SAMMS        |

### CHECKSUM columns for tbl3pClaimNote

| Included in CHECKSUM                                                        | Excluded                  | Reason for exclusion                     |
| --------------------------------------------------------------------------- | ------------------------- | ---------------------------------------- |
| `tpcn`, `tpcnTPCID`, `tpcnDtmAdded`, `tpcnStrAdded`                         | `@SiteCode` (injected)    | Constant — injected by C#, not from source column |
| `tpcnStrNote`, `tpcnStrType`, `tpcnDtTickler`, `tpcnDtTicklerRemoved`        | `RowState` (derived)      | Set by Save method, not from source      |
| `tpcnStrTicklerRemovedNote`, `tpcnStrTicklerRemovedUser`, `tpcnStrTicklerType`, `globalBatchId` | Pipeline metadata columns | Added by pipeline, not from SAMMS |

### Change detection flow

```
SAMMS today:  arnID=100, arnNOTE="Patient called", RowChkSum=1234567890
Silver:       arnID=100, arnNOTE="Patient called", RowChkSum=1234567890
→ Checksums EQUAL → lightweight metadata update only. Record unchanged.

SAMMS today:  arnID=100, arnNOTE="Patient called back", RowChkSum=9876543210
Silver:       arnID=100, arnNOTE="Patient called",      RowChkSum=1234567890
→ Checksums DIFFER → full update. arnNOTE changed in SAMMS.
```

---

## 16. RowState Logic — Active Records Only

`RowState` is a bit column (`true`/`false`) in both `sl_tbl_3pARNOTE` and `sl_tbl_3pClaimNote`.

### For tbl3pArnote

The C# `Save3pArnote` sets `ar.RowState = true` in the `"sitecode"` column case (line 452 of `Save3pElig.cs`). This runs for **every** incoming row before any other field is processed. There is **no pre-reset** and **no soft-delete condition** in `Save3pArnote`.

| Condition                         | RowState | Source                                        |
| --------------------------------- | -------- | --------------------------------------------- |
| Row returned from SAMMS this run  | `true`   | Set unconditionally by `Save3pArnote`         |
| Row in Silver, NOT returned       | Stays at last value | No pre-reset in C# — record persists |

### For tbl3pClaimNote

Same pattern. The C# `Save3pClaimNote` sets `claimNote.RowState = true` in the `"sitecode"` case (line 324 of `Save3pElig.cs`). No pre-reset. No soft-delete.

| Condition                         | RowState | Source                                        |
| --------------------------------- | -------- | --------------------------------------------- |
| Row returned from SAMMS this run  | `true`   | Set unconditionally by `Save3pClaimNote`      |
| Row in Silver, NOT returned       | Stays at last value | No pre-reset in C# — record persists |

> **Contrast with Dose Excuse:** The Dose Excuse Save method DOES pre-reset all existing rows to `RowState = false` before the upsert. ARNote and ClaimNote do NOT do this. All incoming rows are simply marked active.

---

## 17. Why the WHERE Clause Has a 2023 Floor AND a Rolling Window

### Two conditions combined with OR

The `WhereCondition` from `dms.vw_MapAction` for both tables follows the same pattern:

**ARNote:**
```sql
(arnDATE >= '1/1/2023' AND arnDATE >= @WorkDate) OR arnDtRemoved >= @WorkDate
```

**ClaimNote:**
```sql
(tpcnDtmAdded >= '1/1/2023' AND tpcnDtmAdded >= @WorkDate) OR tpcnDtTickler >= @WorkDate
```

`@WorkDate` is substituted in `BHGTaskRunner/Program.cs` line 139 as `WorkDate.AddDays(-15)` — today minus 15 days.

### Branch 1 — The 2023 floor + rolling window

```sql
arnDATE >= '1/1/2023' AND arnDATE >= @WorkDate
```

A record qualifies through this branch only if its primary date is **both** on or after 2023-01-01 **and** within the 15-day rolling window. The 2023 floor prevents the pipeline from ever pulling very old notes (pre-2023) even if they somehow had a recent `arnDATE` entry.

### Branch 2 — Removal/tickler activity

```sql
OR arnDtRemoved >= @WorkDate
```

An AR note where the note was **removed** (soft-deleted in SAMMS) within the last 15 days is pulled even if `arnDATE` is older than 2023 or outside the main window. This ensures removal events (and their audit trail) are always captured.

For ClaimNote:
```sql
OR tpcnDtTickler >= @WorkDate
```

A claim note where the **tickler date** was set or updated within the last 15 days is pulled even if `tpcnDtmAdded` is outside the window. This captures tickler management activity that may occur on older claims.

### Why this matters for Fabric implementation

The Fabric Copy query exactly reproduces both branches. Do NOT simplify to just `arnDATE >= today - 15`. Doing so would miss:
- Notes removed in the last 15 days where the note itself was added before the window
- Claim notes with recently-set ticklers on older claims

---

## 18. The globalBatchId LAB-Site Special Handling

### What the legacy C# does

`BHGTaskRunner/Program.cs` lines 147–150 and 156–159:

```csharp
case "pats.tbl_3parnote":
    if (st.SiteCode == "Lab")
    {
        strCmd = strCmd.Replace(", [globalBatchId] globalBatchId", "").Replace(", [globalBatchId]", "");
    }
```

After `GetSLT` builds the SELECT (which includes `globalBatchId` because `Enabled=1` in the mapping), this code strips it out for the LAB site because the LAB SAMMS database does not have that column.

### What Fabric does instead

The Lookup activity (`lkp_check_globalbatchid_exists`) queries `sys.columns` for the presence of `globalBatchId` in the source table. Both Copy activities then use an `if()` expression:

```
if(globalbatchid_exists = 1,
    'globalBatchId',
    'CAST(NULL AS bigint) AS globalBatchId'
)
```

This is more robust than the C# approach because it:
1. Works automatically for any site that lacks the column, not just LAB
2. Does not require hardcoding site code comparisons in the query expression
3. Keeps the Bronze and Silver schemas consistent across all sites (LAB rows have `globalBatchId = NULL`)

The CHECKSUM expression applies the same conditional so checksum values are consistent for the LAB site across runs.

---

## 19. The ClaimNote Match-Key Anomaly (TpcnTpcid vs Tpcn)

### The Azure PK vs the C# match key

The Azure table `pats.tbl_3pClaimNote` has a composite primary key on `(SiteCode, tpcn)` — defined in `BHG_DRContext.cs` line 884:

```csharp
entity.HasKey(e => new { e.SiteCode, e.Tpcn }).HasName("PK_ClaimNotes");
```

However, `Save3pClaimNote` in `Save3pElig.cs` line 376 does **not** use `tpcn` to find existing records:

```csharp
Models.Tbl3pClaimNote dbclaimNote = tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid);
```

It matches on **`TpcnTpcid`** — the foreign key to the parent claim, not the row's own PK.

### What this means

| Scenario                                             | C# Behavior                                   | Fabric Behavior (this guide)          |
| ---------------------------------------------------- | --------------------------------------------- | ------------------------------------- |
| Same `tpcnTPCID`, different `tpcn` across runs       | C# finds it via TpcnTpcid, updates in place   | Fabric MERGE finds it via tpcnTPCID, updates |
| Two rows with same `tpcnTPCID` in source (rare edge) | C# takes the first match                      | Window dedup in Cell 1 takes latest extraction |
| New `tpcnTPCID` not in Silver                        | C# inserts a new row                          | Fabric inserts                        |

### Why the Fabric MERGE uses tpcnTPCID

To maintain behavioral parity with the legacy C# ETL, the Silver MERGE condition is:

```python
"tgt._site_code = src._site_code AND tgt.tpcnTPCID = src.tpcnTPCID"
```

NOT:

```python
"tgt._site_code = src._site_code AND tgt.tpcn = src.tpcn"   # WRONG — would not match C# behavior
```

> **If you create the Silver table with a unique constraint or partition on `tpcn`,** be aware that the MERGE key (`tpcnTPCID`) is different from the Delta table's natural uniqueness column (`tpcn`). The Delta MERGE will still work correctly because the merge condition drives the match, not the table structure.

---

## 20. Known Anomalies and Cautions

### 1 — globalBatchId is absent from LAB site

The LAB SAMMS database does not have `globalBatchId` in its note tables. The Lookup activity handles this automatically. LAB rows will have `globalBatchId = NULL` in Bronze and Silver. This is expected and matches legacy behavior.

---

### 2 — ClaimNote Azure load window is a fixed 2023 floor, not rolling

In `Save3pClaimNote` (line 311), the C# loads existing Azure rows for comparison:

```csharp
tblCNs = db.Tbl3pClaimNote.Where(x => x.SiteCode == sc && x.TpcnDtmAdded >= DateTime.Parse("1/1/2023")).ToList();
```

The `wrkdt` argument (WorkDate - 15 days) is **NOT** used for the Azure load. The floor is **hardcoded to 2023-01-01**. This means the C# always loads all claim notes since 2023 for the site into memory before upserting.

In Fabric, the Delta MERGE replaces this pattern — the MERGE operates against the entire Silver table regardless. This is equivalent behavior without the memory concern.

---

### 3 — ARNote Azure load window has an extra 10-day stretch

In `Save3pArnote` (line 438), the C# loads existing Azure rows:

```csharp
tblARs = db.Tbl3pArnote.Where(x => x.SiteCode == sc && x.ArnDate >= wrkdt.AddDays(-10)).ToList();
```

Where `wrkdt = WorkDate - 15`. So the Azure load covers `ArnDate >= WorkDate - 25 days`. This is purely a C# memory optimization (reduces the in-memory list size). In Fabric the Delta MERGE against Silver handles this automatically — no equivalent implementation needed.

---

### 4 — RowChkSum and RowState are Enabled=0 in mapping

In `dms.tbl_MapSrc2Dsn`, both `RowChkSum` and `RowState` have `Enabled=0` for these action steps. This means `GetSLT` does NOT include them in the source SELECT via the field mapping. Instead:

- `RowChkSum` is added separately by `SelectConstructor.GetSLT` when `ChkSumEnabled=true` (ActionKey ≠ 3, so it IS enabled here)
- `RowState` is set entirely by the Save method — never read from source

In Fabric: `RowChkSum` is computed in the Copy activity query (inside `CHECKSUM(...)`). `RowState` is set to `lit(True)` in the notebook. Do not try to read either from the SAMMS source tables.

---

### 5 — CHECKSUM columns must match the mapping exactly

The `CHECKSUM()` expressions in the Fabric Copy activity queries must use the **same columns** that `SelectConstructor.GetSLT` uses. If they differ, checksums will never match, and every row will trigger a full Silver update on every run (unnecessary writes). Verify against BHG_DR:

```sql
SELECT ActionKey, ActionStepKey, FieldKey, FieldName, DsnFieldName, Enabled
FROM dms.tbl_MapSrc2Dsn
WHERE ActionKey = 1
  AND ActionStepKey IN (34, 35)
ORDER BY ActionStepKey, FieldKey
```

Fields with `Enabled = 1` are included in the SELECT (and should be in CHECKSUM). Fields with `Enabled = 0` (`RowChkSum`, `RowState`) are excluded from the SELECT but are still present in the destination.

---

### 6 — tpcnDtTicklerRemoved is varchar, not datetime

`tpcnDtTicklerRemoved` in `tbl3pClaimNote` is mapped as a **string** (`varchar`) in the EF model despite the `Dt` prefix suggesting a date. Treat it as string in Spark (no `.cast("date")` needed). The C# reads it as `.ToString()` directly.

---

## 21. Troubleshooting Guide

### Pipeline fails at `lkp_check_globalbatchid_exists`

**Symptom:** Lookup fails with connection error or timeout.  
**Cause:** SAMMS SQL Server for this clinic is unreachable, gateway is down, or connection GUID is wrong.  
**Fix:** Verify connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` is active. Check that `source_database` in `p_sites` exactly matches the actual SQL Server database name (case-sensitive in some configurations).

---

### ARNote Copy activity copies 0 rows

**Symptom:** Copy succeeds but writes 0 rows.  
**Cause 1:** No AR notes fall within the lookback window (no `arnDATE` in last 15 days AND no `arnDtRemoved` in last 15 days, for this site).  
**Fix 1:** Confirm by running directly against the SAMMS database:
```sql
SELECT COUNT(*)
FROM dbo.tbl3pArnote
WHERE (arnDATE >= '1/1/2023' AND arnDATE >= DATEADD(day,-15,GETDATE()))
   OR arnDtRemoved >= DATEADD(day,-15,GETDATE())
```
If 0, the site genuinely has no qualifying records this run. Not an error.  
**Cause 2:** The WHERE condition date literals are being interpreted differently by the SAMMS SQL Server locale.  
**Fix 2:** Replace `'1/1/2023'` with `'2023-01-01'` (ISO format) in the query expression to avoid locale ambiguity.

---

### ClaimNote Copy activity copies 0 rows

**Symptom:** `tbl3pClaimNote` returns 0 rows.  
**Cause:** Same as ARNote — run the equivalent check:
```sql
SELECT COUNT(*)
FROM dbo.tbl3pClaimNote
WHERE (tpcnDtmAdded >= '1/1/2023' AND tpcnDtmAdded >= DATEADD(day,-15,GETDATE()))
   OR tpcnDtTickler >= DATEADD(day,-15,GETDATE())
```

---

### Notebook raises "No Bronze rows found"

**Symptom:** `Exception: No Bronze rows found for ingest_run_id = test-run-001`  
**Cause 1:** Running notebook manually with default fallback run ID, but Bronze was written with a different ID.  
**Fix 1:** Run `spark.table("bhg_bronze.Notes.br_tbl3pArnote").select("_ingest_run_id").distinct().show()` to see what IDs are in Bronze, then update the fallback.  
**Cause 2:** Copy activity wrote 0 rows (see above).

---

### RowChkSum always differs (every row triggers full update on every run)

**Symptom:** Every run updates every row in Silver even when nothing changed.  
**Cause:** The CHECKSUM column list in the Fabric Copy query does not match the columns used by `SelectConstructor` for ActionKey=1, StepKey=34/35.  
**Fix:** Run the `dms.tbl_MapSrc2Dsn` query in Section 20 (#5) to confirm the enabled field list. Update the `CHECKSUM(...)` expression in both Copy activity queries to exactly match.

---

### Silver MERGE matches on wrong rows (ClaimNote duplicate key errors)

**Symptom:** ClaimNote Silver has duplicate `tpcnTPCID` values or MERGE produces unexpected results.  
**Cause:** The MERGE is accidentally using `tpcn` as the key instead of `tpcnTPCID`.  
**Fix:** Verify that the MERGE condition in `nb_3pclaimnote_bronze_to_silver` Cell 2 reads:
```python
"tgt._site_code = src._site_code AND tgt.tpcnTPCID = src.tpcnTPCID"
```
NOT `tgt.tpcn = src.tpcn`. See [Section 19](#19-the-claimnote-match-key-anomaly-tpcntpcid-vs-tpcn).

---

### globalBatchId error for LAB site

**Symptom:** Copy activity fails with "invalid column name 'globalBatchId'" for the LAB site.  
**Cause:** The Lookup result for LAB should return `globalbatchid_exists = 0`, which causes the `if()` to substitute `CAST(NULL AS bigint)`. If it returned `1` instead, the column name is included and fails.  
**Fix:** Run the Lookup query manually against the LAB SAMMS database to confirm `globalBatchId` is not present in `sys.columns`. If the column truly does not exist, the Lookup returns 0 and the `if()` substitution should take effect. Check the `if()` expression syntax in the Copy query for typos.

---

*End of Implementation Guide*







# 3rd-Party AR Notes & Claim Notes — Microsoft Fabric ETL Pipeline Implementation Guide

**Pipeline Name:** Notes Bronze → Silver ETL  
**Data:** 3rd-party AR notes (`tbl3pArnote`) and claim notes (`tbl3pClaimNote`) from SAMMS SQL Server clinic databases  
**Destination:** Microsoft Fabric Lakehouse (Bronze + Silver layers)  
**Covers:** Both `pats.tbl_3pARNOTE` and `pats.tbl_3pClaimNote` in a single pipeline  
**BHGTaskRunner Arg:** `7` (`SAMMS-ETL-Notes`)  
**Author Reference:** `Save3pElig.cs` → `Save3pArnote` / `Save3pClaimNote`, `BHGTaskRunner/Program.cs` lines 146–163, `Scheduler/Program.cs` line 27

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [Prerequisites — What You Need Before Starting](#3-prerequisites--what-you-need-before-starting)
4. [Pipeline Parameters — Define These First](#4-pipeline-parameters--define-these-first)
5. [Step 1 — Create the Data Pipeline in Fabric](#5-step-1--create-the-data-pipeline-in-fabric)
6. [Step 2 — Add the ForEach Activity](#6-step-2--add-the-foreach-activity)
7. [Step 3 — Inside ForEach: Add Copy Activity 1 (ARNote Bronze Extraction)](#7-step-3--inside-foreach-add-copy-activity-1-arnote-bronze-extraction)
8. [Step 4 — Inside ForEach: Add Copy Activity 2 (ClaimNote Bronze Extraction)](#8-step-4--inside-foreach-add-copy-activity-2-claimnote-bronze-extraction)
9. [Step 5 — Add Notebook Activity 1 (ARNote Bronze → Silver)](#9-step-5--add-notebook-activity-1-arnote-bronze--silver)
10. [Step 6 — Add Notebook Activity 2 (ClaimNote Bronze → Silver)](#10-step-6--add-notebook-activity-2-claimnote-bronze--silver)
11. [Step 7 — Create Notebook: nb_3parnote_bronze_to_silver](#11-step-7--create-notebook-nb_3parnote_bronze_to_silver)
12. [Step 8 — Create Notebook: nb_3pclaimnote_bronze_to_silver](#12-step-8--create-notebook-nb_3pclaimnote_bronze_to_silver)
13. [End-to-End Flow Summary](#13-end-to-end-flow-summary)
14. [How Change Detection Works (RowChkSum)](#14-how-change-detection-works-rowchksum)
15. [RowState Logic — Active Records Only](#15-rowstate-logic--active-records-only)
16. [Why the WHERE Clause Has a 2023 Floor AND a Rolling Window](#16-why-the-where-clause-has-a-2023-floor-and-a-rolling-window)
17. [The ClaimNote Match-Key Anomaly (TpcnTpcid vs Tpcn)](#17-the-claimnote-match-key-anomaly-tpcntpcid-vs-tpcn)
18. [Known Anomalies and Cautions](#18-known-anomalies-and-cautions)
19. [Troubleshooting Guide](#19-troubleshooting-guide)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Microsoft Fabric Data Pipeline                    │
│                    pl_notes_samms_to_lakehouse                       │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  ForEach Site (fe_each_samms_site)                          │   │
│  │  Iterates over every clinic in p_sites parameter            │   │
│  │                                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 1              │                               │   │
│  │  │ cp_arnote_to_bronze     │  ← SELECT all tbl3pArnote    │   │
│  │  │ (Copy)                  │    WHERE date lookback        │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │    Notes.br_tbl3pArnote       │   │
│  │  └────────────┬────────────┘                               │   │
│  │               │ Succeeded                                   │   │
│  │               ▼                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 2              │                               │   │
│  │  │ cp_claimnote_to_bronze  │  ← SELECT all tbl3pClaimNote │   │
│  │  │ (Copy)                  │    WHERE date lookback        │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │    Notes.br_tbl3pClaimNote    │   │
│  │  └─────────────────────────┘                               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                         │ ForEach Succeeded                        │
│                         ▼                                          │
│  ┌──────────────────────────────────┐                              │
│  │  nb_3parnote_bronze_to_silver    │  Cell 1: Load Bronze → dedup │
│  │  (Notebook — ARNote)             │  Cell 2: Delta MERGE → Silver│
│  └──────────────────────────────────┘                              │
│                         │ Succeeded                                │
│                         ▼                                          │
│  ┌──────────────────────────────────┐                              │
│  │  nb_3pclaimnote_bronze_to_silver │  Cell 1: Load Bronze → dedup │
│  │  (Notebook — ClaimNote)          │  Cell 2: Delta MERGE → Silver│
│  └──────────────────────────────────┘                              │
└─────────────────────────────────────────────────────────────────────┘
```

**Lakehouse Targets:**

| Layer  | Lakehouse Name | Schema  | Table                  |
| ------ | -------------- | ------- | ---------------------- |
| Bronze | `bhg_bronze`   | `Notes` | `br_tbl3pArnote`       |
| Bronze | `bhg_bronze`   | `Notes` | `br_tbl3pClaimNote`    |
| Silver | `bhg_silver`   | `pats`  | `sl_tbl_3pARNOTE`      |
| Silver | `bhg_silver`   | `pats`  | `sl_tbl_3pClaimNote`   |

---

## 2. What This Pipeline Does — Plain English

The SAMMS system records third-party billing activity for each clinic. Two tables track note-level detail:

- `tbl3pArnote` — one row per AR (Accounts Receivable) note on a third-party billing line item. Tracks who added a note, when, whether it was removed, and free-text content.
- `tbl3pClaimNote` — one row per note on a third-party claim. Tracks tickler dates, note type, who added the note, and free-text content.

There are 80+ clinics, each with their own SAMMS database.

This pipeline:

1. **Visits each clinic's SAMMS database** one at a time (ForEach loop)
2. **Extracts AR note records** within the rolling lookback window (15 days back, 2023 floor, plus OR on `arnDtRemoved`)
3. **Extracts claim note records** within the rolling lookback window (15 days back, 2023 floor, plus OR on `tpcnDtTickler`)
4. **Appends** both extracts to their respective Bronze tables, tagged with a run ID
5. After all clinics are done, runs **two notebooks** that merge Bronze into Silver — one for ARNote, one for ClaimNote — using RowChkSum for change detection

---

## 3. Prerequisites — What You Need Before Starting

| Item                           | What It Is                                                     | Where It Lives                 |
| ------------------------------ | -------------------------------------------------------------- | ------------------------------ |
| SAMMS SQL Server connection    | A Fabric connection to the on-premise SAMMS SQL Server gateway | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse         | Bronze layer Lakehouse                                         | Fabric workspace               |
| `bhg_silver` Lakehouse         | Silver layer Lakehouse                                         | Fabric workspace               |
| `Notes` schema in `bhg_bronze` | Schema for Notes Bronze tables                                 | `bhg_bronze` Lakehouse         |
| `pats` schema in `bhg_silver`  | Schema for Silver patient tables                               | `bhg_silver` Lakehouse         |
| On-premise data gateway        | Configured to reach SAMMS SQL Servers                          | Fabric settings                |

**Connection ID to note down:** When you create the SAMMS SQL Server connection in Fabric, it gets a GUID. In this implementation the connection ID is:

```
9743b95a-fd66-4f7c-9767-e6eb0f1ecab7
```

Replace this with your actual connection GUID everywhere it appears.

**Workspace ID:**

```
c5097ffb-b78e-441d-9575-a82bac23cac8
```

**Bronze Lakehouse Artifact ID:**

```
77d24027-6a1c-43a8-a998-1a14dd3c0d52
```

---

## 4. Pipeline Parameters — Define These First

Go to: **Pipeline canvas → click empty space → Parameters tab → + New**

### Parameter 1: `p_ingest_run_id`

| Setting       | Value             |
| ------------- | ----------------- |
| Name          | `p_ingest_run_id` |
| Type          | `String`          |
| Default Value | `test-run-001`    |

**Why:** Every Bronze row written by this pipeline run carries this ID in the `_ingest_run_id` column. The Silver notebooks filter Bronze to only this run's rows. In production pass something like `NOTES-2026-05-20-001`.

---

### Parameter 2: `p_lookback_days`

| Setting       | Value             |
| ------------- | ----------------- |
| Name          | `p_lookback_days` |
| Type          | `Int`             |
| Default Value | `15`              |

**Why:** This is the `DaysBack = -15` constant from `BHGTaskRunner/Program.cs` line 136. It is used to compute `@WorkDate` = today minus 15 days, which becomes the rolling anchor in the WHERE clause. Do not change this unless you have a specific reason.

---

### Parameter 3: `p_sites`

| Setting       | Value          |
| ------------- | -------------- |
| Name          | `p_sites`      |
| Type          | `Array`        |
| Default Value | See JSON below |

**Default Value (single-database test — paste this exactly):**

```json
[
  {
    "site_code": "AHK",
    "source_database": "SAMMS-AHK",
    "source_schema": "dbo",
    "source_table_arnote": "tbl3pArnote",
    "source_table_claimnote": "tbl3pClaimNote"
  }
]
```

**Why:** This array controls which clinics the ForEach loop processes. For now it contains a single clinic for testing. After the single-database test passes, add all 80+ clinics.

Each object has five properties:

| Property                 | Meaning                                                              | Example          |
| ------------------------ | -------------------------------------------------------------------- | ---------------- |
| `site_code`              | Clinic identifier — tagged on every Bronze row                       | `AHK`            |
| `source_database`        | Exact database name on the SAMMS SQL Server                          | `SAMMS-AHK`      |
| `source_schema`          | Schema where source tables live (always `dbo`)                       | `dbo`            |
| `source_table_arnote`    | Source AR note table name                                            | `tbl3pArnote`    |
| `source_table_claimnote` | Source claim note table name                                         | `tbl3pClaimNote` |

> **LAB site note:** The LAB SAMMS database does not have a `globalBatchId` column. When adding the LAB site to `p_sites`, still use the same object shape — the Lookup activity handles the conditional column automatically. See [Section 18](#18-the-globalbatchid-lab-site-special-handling).

---

## 5. Step 1 — Create the Data Pipeline in Fabric

1. Open your Fabric workspace
2. Click **+ New** → **Data pipeline**
3. Name it: `pl_notes_samms_to_lakehouse`
4. Click **Create**
5. On the pipeline canvas, click empty space → **Parameters** tab → add all three parameters from Section 4

---

## 6. Step 2 — Add the ForEach Activity

### Add the activity

1. From the **Activities** toolbar, drag a **ForEach** activity onto the canvas
2. Rename it: `fe_each_samms_site`

### Configure ForEach Settings tab

| Setting     | Value                            | Why                                                                                  |
| ----------- | -------------------------------- | ------------------------------------------------------------------------------------ |
| Items       | `@pipeline().parameters.p_sites` | Loop over the sites array                                                            |
| Sequential  | **Checked (True)**               | Process one clinic at a time — SAMMS on-premise servers cannot handle parallel loads |
| Batch count | Leave blank                      | Not needed when Sequential is true                                                   |

### JSON for reference

```json
{
    "name": "fe_each_samms_site",
    "type": "ForEach",
    "dependsOn": [],
    "typeProperties": {
        "items": {
            "value": "@pipeline().parameters.p_sites",
            "type": "Expression"
        },
        "isSequential": true,
        "activities": []
    }
}
```

---

## 7. Step 3 — Inside ForEach: Add Copy Activity 1 (ARNote Bronze Extraction)

### What it does

- Connects to the clinic's SAMMS SQL Server
- Runs a SELECT query with the rolling lookback WHERE condition for `tbl3pArnote`
- Appends rows to Bronze table `bhg_bronze.Notes.br_tbl3pArnote`
- Adds metadata columns to every row

### Add the activity

1. Click **Edit** on the ForEach activity to open its inner canvas
2. Drag a **Copy** activity onto the inner canvas
3. Rename it: `cp_arnote_to_bronze`

### Configure Source tab

| Setting       | Value                                  |
| ------------- | -------------------------------------- |
| Source type   | SQL Server                             |
| Connection    | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query     | **Query** (Expression mode)            |
| Query timeout | `02:00:00`                             |

### The Source Query Expression — paste this into Query field

```
@concat(
'SELECT
    ''', item().site_code, ''' AS _site_code,
    ''', item().source_database, ''' AS _source_database,
    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,
    GETDATE() AS _extracted_at,
    CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())) AS _source_query_date_anchor,

    arnID,
    arnLIID,
    arnNOTE,
    arnUSER,
    arnDATE,
    arnDtRemoved,
    arnStrRemovedReason,
    arnStrRemovedUser,
    bid,
    arnDBnotes,
    globalBatchId,

    RowChkSum = CHECKSUM(
        arnID,
        arnLIID,
        arnNOTE,
        arnUSER,
        arnDATE,
        arnDtRemoved,
        arnStrRemovedReason,
        arnStrRemovedUser,
        bid,
        arnDBnotes,
        globalBatchId
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table_arnote, '
WHERE (arnDATE >= ''2023-01-01'' AND arnDATE >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())))
   OR arnDtRemoved >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
ORDER BY arnID'
)
```

### Breaking Down What This Query Does

#### Metadata columns (first 5)

```sql
'AHK'                                   AS _site_code,
'SAMMS-AHK'                             AS _source_database,
'NOTES-2026-05-20-001'                  AS _ingest_run_id,
GETDATE()                               AS _extracted_at,
CONVERT(date, DATEADD(day,-15,GETDATE())) AS _source_query_date_anchor
```

#### Data columns (10 columns from tbl3pArnote)

All clinical columns from `tbl3pArnote`. These map directly to `pats.tbl_3pARNOTE` in Azure/Silver.

Column-to-destination mapping (from `dms.tbl_MapSrc2Dsn` ActionKey=1, StepKey=35):

| Source Field       | Destination Field      | PrimaryKey |
| ------------------ | ---------------------- | ---------- |
| `@SiteCode`        | `SiteCode`             | 1 (PK)     |
| `arnID`            | `arnID`                | 2 (PK)     |
| `arnLIID`          | `arnLIID`              | —          |
| `arnNOTE`          | `arnNOTE`              | —          |
| `arnUSER`          | `arnUSER`              | —          |
| `arnDATE`          | `arnDATE`              | —          |
| `arnDtRemoved`     | `arnDtRemoved`         | —          |
| `arnStrRemovedReason` | `arnStrRemovedReason` | —         |
| `arnStrRemovedUser`   | `arnStrRemovedUser`   | —         |
| `bid`              | `bid`                  | —          |
| `arnDBnotes`       | `arnDBnotes`           | —          |
| `globalBatchId`    | `globalBatchId`        | —          |
| *(computed)*       | `RowChkSum`            | —          |
| *(set by Save)*    | `RowState`             | —          |

#### globalBatchId

Included directly in the SELECT. All sites in scope have this column in their SAMMS databases.

#### RowChkSum — change detection fingerprint

Computed over all 10 data fields including `globalBatchId` (or NULL for LAB). See [Section 15](#15-how-change-detection-works-rowchksum) for full details.

#### WHERE clause — lookback filter (exact match to Scheduler WhereCondition)

```sql
WHERE (arnDATE >= '1/1/2023' AND arnDATE >= CONVERT(date, DATEADD(day, -15, GETDATE())))
   OR arnDtRemoved >= CONVERT(date, DATEADD(day, -15, GETDATE()))
```

This is the **exact** `WhereCondition` from `dms.vw_MapAction` for ActionKey=1, StepKey=35, with `@WorkDate` substituted as `DATEADD(day, -15, GETDATE())`. See [Section 17](#17-why-the-where-clause-has-a-2023-floor-and-a-rolling-window) for a full explanation.

### Configure Sink tab

| Setting                 | Value                                  |
| ----------------------- | -------------------------------------- |
| Sink type               | **Lakehouse**                          |
| Linked service name     | `bhg_bronze`                           |
| Workspace ID            | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder             | `Tables`                               |
| Schema                  | `Notes`                                |
| Table                   | `br_tbl3pArnote`                       |
| Table action            | **Append**                             |
| Apply V-Order           | No (unchecked)                         |

### Configure Mapping / Translator tab

| Setting                 | Value       |
| ----------------------- | ----------- |
| Type conversion         | **Enabled** |
| Allow data truncation   | **True**    |
| Treat boolean as number | **False**   |

### Configure Policy tab

| Setting        | Value        |
| -------------- | ------------ |
| Timeout        | `0.12:00:00` |
| Retry          | `0`          |
| Retry interval | `30` seconds |

---

## 8. Step 4 — Inside ForEach: Add Copy Activity 2 (ClaimNote Bronze Extraction)

### What it does

- Connects to the same clinic's SAMMS SQL Server
- Runs a SELECT query with the rolling lookback WHERE condition for `tbl3pClaimNote`
- Appends rows to Bronze table `bhg_bronze.Notes.br_tbl3pClaimNote`

### Add the activity

1. Still on the inner ForEach canvas
2. Drag a **Copy** activity
3. Rename it: `cp_claimnote_to_bronze`
4. Draw arrow: `cp_arnote_to_bronze` → `cp_claimnote_to_bronze` with condition **Succeeded**

> **Why after ARNote copy, not in parallel?** SAMMS on-premise servers are limited. Running two simultaneous SELECTs against the same clinic database adds unnecessary load. Sequential is safer.

### Configure Source tab

| Setting       | Value                                  |
| ------------- | -------------------------------------- |
| Source type   | SQL Server                             |
| Connection    | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query     | **Query** (Expression mode)            |
| Query timeout | `02:00:00`                             |

### The Source Query Expression

```
@concat(
'SELECT
    ''', item().site_code, ''' AS _site_code,
    ''', item().source_database, ''' AS _source_database,
    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,
    GETDATE() AS _extracted_at,
    CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())) AS _source_query_date_anchor,

    tpcn,
    tpcnTPCID,
    tpcnDtmAdded,
    tpcnStrAdded,
    tpcnStrNote,
    tpcnStrType,
    tpcnDtTickler,
    tpcnDtTicklerRemoved,
    tpcnStrTicklerRemovedNote,
    tpcnStrTicklerRemovedUser,
    tpcnStrTicklerType,
    globalBatchId,

    RowChkSum = CHECKSUM(
        tpcn,
        tpcnTPCID,
        tpcnDtmAdded,
        tpcnStrAdded,
        tpcnStrNote,
        tpcnStrType,
        tpcnDtTickler,
        tpcnDtTicklerRemoved,
        tpcnStrTicklerRemovedNote,
        tpcnStrTicklerRemovedUser,
        tpcnStrTicklerType,
        globalBatchId
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table_claimnote, '
WHERE (tpcnDtmAdded >= ''2023-01-01'' AND tpcnDtmAdded >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())))
   OR tpcnDtTickler >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
ORDER BY tpcn'
)
```

### Column-to-destination mapping for tbl3pClaimNote

From `dms.tbl_MapSrc2Dsn` ActionKey=1, StepKey=34:

| Source Field              | Destination Field           | PrimaryKey |
| ------------------------- | --------------------------- | ---------- |
| `@SiteCode`               | `SiteCode`                  | 1 (PK)     |
| `tpcn`                    | `tpcn`                      | 2 (PK)     |
| `tpcnTPCID`               | `tpcnTPCID`                 | —          |
| `tpcnDtmAdded`            | `tpcnDtmAdded`              | —          |
| `tpcnStrAdded`            | `tpcnStrAdded`              | —          |
| `tpcnStrNote`             | `tpcnStrNote`               | —          |
| `tpcnStrType`             | `tpcnStrType`               | —          |
| `tpcnDtTickler`           | `tpcnDtTickler`             | —          |
| `tpcnDtTicklerRemoved`    | `tpcnDtTicklerRemoved`      | —          |
| `tpcnStrTicklerRemovedNote`  | `tpcnStrTicklerRemovedNote` | —        |
| `tpcnStrTicklerRemovedUser`  | `tpcnStrTicklerRemovedUser` | —        |
| `tpcnStrTicklerType`      | `tpcnStrTicklerType`        | —          |
| `globalBatchId`           | `globalBatchId`             | —          |
| *(computed)*              | `RowChkSum`                 | —          |
| *(set by Save)*           | `RowState`                  | —          |

> **Critical — Azure PK vs Match Key:** The Azure PK for `pats.tbl_3pClaimNote` is `(SiteCode, tpcn)`. However the C# upsert in `Save3pClaimNote` (line 376) matches on `TpcnTpcid`, NOT on `tpcn`. See [Section 19](#19-the-claimnote-match-key-anomaly-tpcntpcid-vs-tpcn) for the full explanation and what this means for the Fabric MERGE.

### Configure Sink tab

| Setting                 | Value                                  |
| ----------------------- | -------------------------------------- |
| Sink type               | **Lakehouse**                          |
| Linked service name     | `bhg_bronze`                           |
| Workspace ID            | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder             | `Tables`                               |
| Schema                  | `Notes`                                |
| Table                   | `br_tbl3pClaimNote`                    |
| Table action            | **Append**                             |
| Apply V-Order           | No (unchecked)                         |

### Configure Mapping / Translator tab

| Setting                 | Value       |
| ----------------------- | ----------- |
| Type conversion         | **Enabled** |
| Allow data truncation   | **True**    |
| Treat boolean as number | **False**   |

### Configure Policy tab

| Setting        | Value        |
| -------------- | ------------ |
| Timeout        | `0.12:00:00` |
| Retry          | `0`          |
| Retry interval | `30` seconds |

---

## 9. Step 5 — Add Notebook Activity 1 (ARNote Bronze → Silver)

### Add the activity

1. Go back to the **main pipeline canvas** (outside ForEach)
2. Drag a **Notebook** activity
3. Rename it: `nb_3parnote_bronze_to_silver`
4. Draw arrow: `fe_each_samms_site` → `nb_3parnote_bronze_to_silver` with condition **Succeeded**

### Configure Settings tab

| Setting   | Value                                                  |
| --------- | ------------------------------------------------------ |
| Notebook  | Select `nb_3parnote_bronze_to_silver` (create in Step 8) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8`                 |

### Configure Parameters

| Parameter Name    | Type   | Value                                                 |
| ----------------- | ------ | ----------------------------------------------------- |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |

---

## 10. Step 6 — Add Notebook Activity 2 (ClaimNote Bronze → Silver)

### Add the activity

1. Still on the **main pipeline canvas**
2. Drag another **Notebook** activity
3. Rename it: `nb_3pclaimnote_bronze_to_silver`
4. Draw arrow: `nb_3parnote_bronze_to_silver` → `nb_3pclaimnote_bronze_to_silver` with condition **Succeeded**

> **Why sequential notebooks?** ClaimNote does not depend on ARNote results. However running them sequentially avoids Spark cluster contention during development. Once tested you may run them in parallel if needed.

### Configure Settings tab

| Setting   | Value                                                       |
| --------- | ----------------------------------------------------------- |
| Notebook  | Select `nb_3pclaimnote_bronze_to_silver` (create in Step 9) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8`                      |

### Configure Parameters

| Parameter Name    | Type   | Value                                                 |
| ----------------- | ------ | ----------------------------------------------------- |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |

---

## 11. Step 7 — Create Notebook: nb_3parnote_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_3parnote_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` Lakehouses in the notebook's Lakehouse panel

---

### Cell 1 — Load Bronze and Prepare Silver Source

```python
from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window

# Pipeline passes p_ingest_run_id as a parameter.
# The try/except lets you run this manually during development.
try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Notes.br_tbl3pArnote"
silver_table = "bhg_silver.pats.sl_tbl_3pARNOTE"

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

# Read only rows from THIS pipeline run.
bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze rows found for ingest_run_id = {p_ingest_run_id}")

# Deduplicate within current run.
# Business key = _site_code + arnID   (mirrors Azure PK: SiteCode, ArnId)
# If the same record appears twice (e.g. due to a retry), keep the latest extraction.
w = Window.partitionBy("_site_code", "arnID").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("arnID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # ── RowState Logic ─────────────────────────────────────────────────────────
    # Save3pArnote always sets RowState = true for every incoming row.
    # There is NO pre-reset and NO soft-delete condition for ARNote.
    # C# reference: ar.RowState = true  (set in the "sitecode" case, line 452)
    # Any record returned from SAMMS is considered active.
    .withColumn("RowState", lit(True))

    # Silver audit timestamps
    .withColumn("silver_updated_at", current_timestamp())
    .withColumn("last_seen_at", current_timestamp())
)

src_df.createOrReplaceTempView("vw_arnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ARNote Silver: {src_count}")

# First-ever run: create Silver table
if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write
        .format("delta")
        .mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created ARNote Silver table and inserted rows: {src_count}")
else:
    print(f"Silver table already exists: {silver_table}")
```

### Explanation of Cell 1 — Key Decisions

| Code Section                             | Why It Exists                                                                                     |
| ---------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `try: p_ingest_run_id`                   | Lets you run the notebook manually without the pipeline for testing                               |
| Filter on `_ingest_run_id`               | Bronze accumulates rows from every run — filter to just this run's rows                           |
| `if bronze_count == 0: raise Exception`  | Fail loudly if Bronze is empty — catches upstream failures before a silent empty Silver update    |
| `Window.partitionBy("_site_code","arnID")` | Deduplicates in case a retry wrote the same record twice                                        |
| `RowState = lit(True)`                   | Mirrors `ar.RowState = true` in `Save3pArnote` — ALL incoming rows are active, no exceptions     |
| `silver_created_at` on initial write     | Set once, never overwritten — records when this row first appeared in Silver                      |

---

### Cell 2 — Merge Into ARNote Silver Delta Table

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tbl_3pARNOTE"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = src_df.columns

# Update all columns EXCEPT silver_created_at (preserve original creation timestamp)
update_cols = [c for c in src_cols if c != "silver_created_at"]
update_set  = {c: f"src.{c}" for c in update_cols}

# Always use server time for audit timestamps on update
update_set["silver_updated_at"] = "current_timestamp()"
update_set["last_seen_at"]      = "current_timestamp()"

# On insert: write all columns including silver_created_at
insert_values = {c: f"src.{c}" for c in src_cols}

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # Merge key: clinic + note ID (mirrors Azure PK: SiteCode + ArnId)
        "tgt._site_code = src._site_code AND tgt.arnID = src.arnID"
    )

    # CASE 1: Record exists AND data changed (RowChkSum differs or NULL on either side)
    # → Full update of all data columns
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: Record exists AND data has NOT changed (RowChkSum identical)
    # → Lightweight metadata-only update. No data columns touched.
    # → Still refresh RowState (defensive — ARNote RowState is always true, but consistent)
    .whenMatchedUpdate(
        condition="tgt.RowChkSum = src.RowChkSum",
        set={
            "last_seen_at":              "current_timestamp()",
            "RowState":                  "src.RowState",
            "_ingest_run_id":            "src._ingest_run_id",
            "_extracted_at":             "src._extracted_at",
            "_source_query_date_anchor": "src._source_query_date_anchor"
        }
    )

    # CASE 3: New record not yet in Silver → Insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ARNote Silver MERGE completed successfully.")
```

### Explanation of the Three MERGE Branches

#### Branch 1 — Full update (RowChkSum changed)

An AR note already exists in Silver but at least one column changed in SAMMS (note text edited, removal date set, user updated, etc.). All data columns are overwritten. `silver_created_at` is NOT overwritten.

#### Branch 2 — Metadata-only update (RowChkSum identical)

The AR note is unchanged. It appeared in the extract because its `arnDATE` or `arnDtRemoved` fell within the lookback window. No data columns are rewritten — this avoids unnecessary Delta table write amplification.

#### Branch 3 — New insert

The `_site_code + arnID` combination has never been seen in Silver. Insert the complete row including `silver_created_at`.

---

## 12. Step 8 — Create Notebook: nb_3pclaimnote_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_3pclaimnote_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` Lakehouses

---

### Cell 1 — Load Bronze and Prepare Silver Source

```python
from pyspark.sql.functions import col, current_timestamp, lit, row_number
from pyspark.sql.window import Window

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Notes.br_tbl3pClaimNote"
silver_table = "bhg_silver.pats.sl_tbl_3pClaimNote"

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze ClaimNote rows found for ingest_run_id = {p_ingest_run_id}")

# ── CRITICAL: Deduplicate on _site_code + tpcnTPCID ─────────────────────────
# The C# Save3pClaimNote matches existing records by TpcnTpcid, NOT by tpcn
# (the Azure PK). See Section 19 for full explanation.
# For Fabric, we use the same match logic: _site_code + tpcnTPCID.
w = Window.partitionBy("_site_code", "tpcnTPCID").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("tpcnTPCID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # ── RowState Logic ─────────────────────────────────────────────────────────
    # Save3pClaimNote always sets RowState = true for every incoming row.
    # There is NO pre-reset and NO soft-delete condition for ClaimNote.
    # C# reference: claimNote.RowState = true  (set in the "sitecode" case, line 324)
    .withColumn("RowState", lit(True))

    .withColumn("silver_updated_at", current_timestamp())
    .withColumn("last_seen_at", current_timestamp())
)

src_df.createOrReplaceTempView("vw_claimnote_current_run")

src_count = src_df.count()
print(f"Prepared source rows for ClaimNote Silver: {src_count}")

if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write
        .format("delta")
        .mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created ClaimNote Silver table: {src_count}")
else:
    print(f"Silver table exists: {silver_table}")
```

---

### Cell 2 — Merge Into ClaimNote Silver Delta Table

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tbl_3pClaimNote"

if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = src_df.columns

update_cols = [c for c in src_cols if c != "silver_created_at"]
update_set  = {c: f"src.{c}" for c in update_cols}
update_set["silver_updated_at"] = "current_timestamp()"
update_set["last_seen_at"]      = "current_timestamp()"

insert_values = {c: f"src.{c}" for c in src_cols}

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # ── CRITICAL: Match on tpcnTPCID not tpcn ──────────────────────────────
        # The C# Save3pClaimNote line 376:
        #   tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid)
        # Uses TpcnTpcid as the match key, not the Azure PK (tpcn).
        # Fabric MERGE must mirror this: _site_code + tpcnTPCID
        "tgt._site_code = src._site_code AND tgt.tpcnTPCID = src.tpcnTPCID"
    )

    # CASE 1: Record exists AND data changed → Full update
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: Record exists AND data unchanged → Metadata-only update
    .whenMatchedUpdate(
        condition="tgt.RowChkSum = src.RowChkSum",
        set={
            "last_seen_at":              "current_timestamp()",
            "RowState":                  "src.RowState",
            "_ingest_run_id":            "src._ingest_run_id",
            "_extracted_at":             "src._extracted_at",
            "_source_query_date_anchor": "src._source_query_date_anchor"
        }
    )

    # CASE 3: New record → Insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("ClaimNote Silver MERGE completed successfully.")
```

---

## 13. End-to-End Flow Summary

```
TRIGGER: Pipeline runs with parameters:
  p_ingest_run_id  = "NOTES-2026-05-20-001"
  p_lookback_days  = 15
  p_sites          = [ { site_code: "AHK",
                         source_database: "SAMMS-AHK",
                         source_schema: "dbo",
                         source_table_arnote: "tbl3pArnote",
                         source_table_claimnote: "tbl3pClaimNote" } ]

STEP 1 — ForEach begins
  ↓ Current item: { site_code: "AHK", source_database: "SAMMS-AHK", ... }

STEP 2 — lkp_check_globalbatchid_exists (Lookup)
  → Queries SAMMS-AHK sys.columns for globalBatchId in tbl3pArnote
  → Returns: { globalbatchid_exists: 1 }   (or 0 for LAB site)

STEP 3 — cp_arnote_to_bronze (Copy Activity)
  → Builds dynamic SELECT from item() values + Lookup result
  → WHERE: (arnDATE >= '1/1/2023' AND arnDATE >= today - 15 days)
           OR arnDtRemoved >= today - 15 days
  → APPENDs rows tagged with _ingest_run_id = "NOTES-2026-05-20-001" to:
     bhg_bronze.Notes.br_tbl3pArnote

STEP 4 — cp_claimnote_to_bronze (Copy Activity)
  → Builds dynamic SELECT from item() values + Lookup result
  → WHERE: (tpcnDtmAdded >= '1/1/2023' AND tpcnDtmAdded >= today - 15 days)
           OR tpcnDtTickler >= today - 15 days
  → APPENDs rows tagged with _ingest_run_id = "NOTES-2026-05-20-001" to:
     bhg_bronze.Notes.br_tbl3pClaimNote

STEP 5 — ForEach moves to next site and repeats Steps 2–4
  ... until all sites done ...

STEP 6 — ForEach completes

STEP 7 — nb_3parnote_bronze_to_silver (Cell 1)
  → Reads br_tbl3pArnote WHERE _ingest_run_id = "NOTES-2026-05-20-001"
  → Deduplicates on _site_code + arnID
  → Sets RowState = true for all rows (no exceptions)
  → Adds silver_updated_at, last_seen_at
  → Creates Silver table on first run

STEP 8 — nb_3parnote_bronze_to_silver (Cell 2)
  → Delta MERGE into bhg_silver.pats.sl_tbl_3pARNOTE
     RowChkSum changed   → FULL UPDATE of all data columns
     RowChkSum same      → lightweight update (last_seen_at + RowState only)
     New record          → INSERT all columns

STEP 9 — nb_3pclaimnote_bronze_to_silver (Cell 1 + Cell 2)
  → Same pattern for tbl3pClaimNote
  → Deduplicate on _site_code + tpcnTPCID  ← NOT tpcn (see Section 19)
  → All incoming rows get RowState = true
  → Delta MERGE into bhg_silver.pats.sl_tbl_3pClaimNote

DONE: Both Silver tables are current for all changes within the lookback window.
```

---

## 14. How Change Detection Works (RowChkSum)

SQL Server's `CHECKSUM()` function computes a single integer from a list of column values. If any column changes, the integer changes. This is the same mechanism used in the original C# ETL.

### CHECKSUM columns for tbl3pArnote

| Included in CHECKSUM                                           | Excluded                  | Reason for exclusion                     |
| -------------------------------------------------------------- | ------------------------- | ---------------------------------------- |
| `arnID`, `arnLIID`, `arnNOTE`, `arnUSER`                       | `@SiteCode` (injected)    | Constant — injected by C#, not from source column |
| `arnDATE`, `arnDtRemoved`, `arnStrRemovedReason`               | `RowState` (derived)      | Set by Save method, not from source      |
| `arnStrRemovedUser`, `bid`, `arnDBnotes`, `globalBatchId`      | Pipeline metadata columns | Added by pipeline, not from SAMMS        |

### CHECKSUM columns for tbl3pClaimNote

| Included in CHECKSUM                                                        | Excluded                  | Reason for exclusion                     |
| --------------------------------------------------------------------------- | ------------------------- | ---------------------------------------- |
| `tpcn`, `tpcnTPCID`, `tpcnDtmAdded`, `tpcnStrAdded`                         | `@SiteCode` (injected)    | Constant — injected by C#, not from source column |
| `tpcnStrNote`, `tpcnStrType`, `tpcnDtTickler`, `tpcnDtTicklerRemoved`        | `RowState` (derived)      | Set by Save method, not from source      |
| `tpcnStrTicklerRemovedNote`, `tpcnStrTicklerRemovedUser`, `tpcnStrTicklerType`, `globalBatchId` | Pipeline metadata columns | Added by pipeline, not from SAMMS |

### Change detection flow

```
SAMMS today:  arnID=100, arnNOTE="Patient called", RowChkSum=1234567890
Silver:       arnID=100, arnNOTE="Patient called", RowChkSum=1234567890
→ Checksums EQUAL → lightweight metadata update only. Record unchanged.

SAMMS today:  arnID=100, arnNOTE="Patient called back", RowChkSum=9876543210
Silver:       arnID=100, arnNOTE="Patient called",      RowChkSum=1234567890
→ Checksums DIFFER → full update. arnNOTE changed in SAMMS.
```

---

## 15. RowState Logic — Active Records Only

`RowState` is a bit column (`true`/`false`) in both `sl_tbl_3pARNOTE` and `sl_tbl_3pClaimNote`.

### For tbl3pArnote

The C# `Save3pArnote` sets `ar.RowState = true` in the `"sitecode"` column case (line 452 of `Save3pElig.cs`). This runs for **every** incoming row before any other field is processed. There is **no pre-reset** and **no soft-delete condition** in `Save3pArnote`.

| Condition                         | RowState | Source                                        |
| --------------------------------- | -------- | --------------------------------------------- |
| Row returned from SAMMS this run  | `true`   | Set unconditionally by `Save3pArnote`         |
| Row in Silver, NOT returned       | Stays at last value | No pre-reset in C# — record persists |

### For tbl3pClaimNote

Same pattern. The C# `Save3pClaimNote` sets `claimNote.RowState = true` in the `"sitecode"` case (line 324 of `Save3pElig.cs`). No pre-reset. No soft-delete.

| Condition                         | RowState | Source                                        |
| --------------------------------- | -------- | --------------------------------------------- |
| Row returned from SAMMS this run  | `true`   | Set unconditionally by `Save3pClaimNote`      |
| Row in Silver, NOT returned       | Stays at last value | No pre-reset in C# — record persists |

> **Contrast with Dose Excuse:** The Dose Excuse Save method DOES pre-reset all existing rows to `RowState = false` before the upsert. ARNote and ClaimNote do NOT do this. All incoming rows are simply marked active.

---

## 16. Why the WHERE Clause Has a 2023 Floor AND a Rolling Window

### Two conditions combined with OR

The `WhereCondition` from `dms.vw_MapAction` for both tables follows the same pattern:

**ARNote:**
```sql
(arnDATE >= '1/1/2023' AND arnDATE >= @WorkDate) OR arnDtRemoved >= @WorkDate
```

**ClaimNote:**
```sql
(tpcnDtmAdded >= '1/1/2023' AND tpcnDtmAdded >= @WorkDate) OR tpcnDtTickler >= @WorkDate
```

`@WorkDate` is substituted in `BHGTaskRunner/Program.cs` line 139 as `WorkDate.AddDays(-15)` — today minus 15 days.

### Branch 1 — The 2023 floor + rolling window

```sql
arnDATE >= '1/1/2023' AND arnDATE >= @WorkDate
```

A record qualifies through this branch only if its primary date is **both** on or after 2023-01-01 **and** within the 15-day rolling window. The 2023 floor prevents the pipeline from ever pulling very old notes (pre-2023) even if they somehow had a recent `arnDATE` entry.

### Branch 2 — Removal/tickler activity

```sql
OR arnDtRemoved >= @WorkDate
```

An AR note where the note was **removed** (soft-deleted in SAMMS) within the last 15 days is pulled even if `arnDATE` is older than 2023 or outside the main window. This ensures removal events (and their audit trail) are always captured.

For ClaimNote:
```sql
OR tpcnDtTickler >= @WorkDate
```

A claim note where the **tickler date** was set or updated within the last 15 days is pulled even if `tpcnDtmAdded` is outside the window. This captures tickler management activity that may occur on older claims.

### Why this matters for Fabric implementation

The Fabric Copy query exactly reproduces both branches. Do NOT simplify to just `arnDATE >= today - 15`. Doing so would miss:
- Notes removed in the last 15 days where the note itself was added before the window
- Claim notes with recently-set ticklers on older claims

---

## 17. The ClaimNote Match-Key Anomaly (TpcnTpcid vs Tpcn)

### The Azure PK vs the C# match key

The Azure table `pats.tbl_3pClaimNote` has a composite primary key on `(SiteCode, tpcn)` — defined in `BHG_DRContext.cs` line 884:

```csharp
entity.HasKey(e => new { e.SiteCode, e.Tpcn }).HasName("PK_ClaimNotes");
```

However, `Save3pClaimNote` in `Save3pElig.cs` line 376 does **not** use `tpcn` to find existing records:

```csharp
Models.Tbl3pClaimNote dbclaimNote = tblCNs.FirstOrDefault(x => x.TpcnTpcid == claimNote.TpcnTpcid);
```

It matches on **`TpcnTpcid`** — the foreign key to the parent claim, not the row's own PK.

### What this means

| Scenario                                             | C# Behavior                                   | Fabric Behavior (this guide)          |
| ---------------------------------------------------- | --------------------------------------------- | ------------------------------------- |
| Same `tpcnTPCID`, different `tpcn` across runs       | C# finds it via TpcnTpcid, updates in place   | Fabric MERGE finds it via tpcnTPCID, updates |
| Two rows with same `tpcnTPCID` in source (rare edge) | C# takes the first match                      | Window dedup in Cell 1 takes latest extraction |
| New `tpcnTPCID` not in Silver                        | C# inserts a new row                          | Fabric inserts                        |

### Why the Fabric MERGE uses tpcnTPCID

To maintain behavioral parity with the legacy C# ETL, the Silver MERGE condition is:

```python
"tgt._site_code = src._site_code AND tgt.tpcnTPCID = src.tpcnTPCID"
```

NOT:

```python
"tgt._site_code = src._site_code AND tgt.tpcn = src.tpcn"   # WRONG — would not match C# behavior
```

> **If you create the Silver table with a unique constraint or partition on `tpcn`,** be aware that the MERGE key (`tpcnTPCID`) is different from the Delta table's natural uniqueness column (`tpcn`). The Delta MERGE will still work correctly because the merge condition drives the match, not the table structure.

---

## 18. Known Anomalies and Cautions

### 1 — ClaimNote Azure load window is a fixed 2023 floor, not rolling

In `Save3pClaimNote` (line 311), the C# loads existing Azure rows for comparison:

```csharp
tblCNs = db.Tbl3pClaimNote.Where(x => x.SiteCode == sc && x.TpcnDtmAdded >= DateTime.Parse("1/1/2023")).ToList();
```

The `wrkdt` argument (WorkDate - 15 days) is **NOT** used for the Azure load. The floor is **hardcoded to 2023-01-01**. This means the C# always loads all claim notes since 2023 for the site into memory before upserting.

In Fabric, the Delta MERGE replaces this pattern — the MERGE operates against the entire Silver table regardless. This is equivalent behavior without the memory concern.

---

### 2 — ARNote Azure load window has an extra 10-day stretch

In `Save3pArnote` (line 438), the C# loads existing Azure rows:

```csharp
tblARs = db.Tbl3pArnote.Where(x => x.SiteCode == sc && x.ArnDate >= wrkdt.AddDays(-10)).ToList();
```

Where `wrkdt = WorkDate - 15`. So the Azure load covers `ArnDate >= WorkDate - 25 days`. This is purely a C# memory optimization (reduces the in-memory list size). In Fabric the Delta MERGE against Silver handles this automatically — no equivalent implementation needed.

---

### 3 — RowChkSum and RowState are Enabled=0 in mapping

In `dms.tbl_MapSrc2Dsn`, both `RowChkSum` and `RowState` have `Enabled=0` for these action steps. This means `GetSLT` does NOT include them in the source SELECT via the field mapping. Instead:

- `RowChkSum` is added separately by `SelectConstructor.GetSLT` when `ChkSumEnabled=true` (ActionKey ≠ 3, so it IS enabled here)
- `RowState` is set entirely by the Save method — never read from source

In Fabric: `RowChkSum` is computed in the Copy activity query (inside `CHECKSUM(...)`). `RowState` is set to `lit(True)` in the notebook. Do not try to read either from the SAMMS source tables.

---

### 4 — CHECKSUM columns must match the mapping exactly

The `CHECKSUM()` expressions in the Fabric Copy activity queries must use the **same columns** that `SelectConstructor.GetSLT` uses. If they differ, checksums will never match, and every row will trigger a full Silver update on every run (unnecessary writes). Verify against BHG_DR:

```sql
SELECT ActionKey, ActionStepKey, FieldKey, FieldName, DsnFieldName, Enabled
FROM dms.tbl_MapSrc2Dsn
WHERE ActionKey = 1
  AND ActionStepKey IN (34, 35)
ORDER BY ActionStepKey, FieldKey
```

Fields with `Enabled = 1` are included in the SELECT (and should be in CHECKSUM). Fields with `Enabled = 0` (`RowChkSum`, `RowState`) are excluded from the SELECT but are still present in the destination.

---

### 5 — tpcnDtTicklerRemoved is varchar, not datetime

`tpcnDtTicklerRemoved` in `tbl3pClaimNote` is mapped as a **string** (`varchar`) in the EF model despite the `Dt` prefix suggesting a date. Treat it as string in Spark (no `.cast("date")` needed). The C# reads it as `.ToString()` directly.

---

## 19. Troubleshooting Guide

### Pipeline fails at `lkp_check_globalbatchid_exists`

**Symptom:** Lookup fails with connection error or timeout.  
**Cause:** SAMMS SQL Server for this clinic is unreachable, gateway is down, or connection GUID is wrong.  
**Fix:** Verify connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` is active. Check that `source_database` in `p_sites` exactly matches the actual SQL Server database name (case-sensitive in some configurations).

---

### ARNote Copy activity copies 0 rows

**Symptom:** Copy succeeds but writes 0 rows.  
**Cause 1:** No AR notes fall within the lookback window (no `arnDATE` in last 15 days AND no `arnDtRemoved` in last 15 days, for this site).  
**Fix 1:** Confirm by running directly against the SAMMS database:
```sql
SELECT COUNT(*)
FROM dbo.tbl3pArnote
WHERE (arnDATE >= '1/1/2023' AND arnDATE >= DATEADD(day,-15,GETDATE()))
   OR arnDtRemoved >= DATEADD(day,-15,GETDATE())
```
If 0, the site genuinely has no qualifying records this run. Not an error.  
**Cause 2:** The WHERE condition date literals are being interpreted differently by the SAMMS SQL Server locale.  
**Fix 2:** Replace `'1/1/2023'` with `'2023-01-01'` (ISO format) in the query expression to avoid locale ambiguity.

---

### ClaimNote Copy activity copies 0 rows

**Symptom:** `tbl3pClaimNote` returns 0 rows.  
**Cause:** Same as ARNote — run the equivalent check:
```sql
SELECT COUNT(*)
FROM dbo.tbl3pClaimNote
WHERE (tpcnDtmAdded >= '1/1/2023' AND tpcnDtmAdded >= DATEADD(day,-15,GETDATE()))
   OR tpcnDtTickler >= DATEADD(day,-15,GETDATE())
```

---

### Notebook raises "No Bronze rows found"

**Symptom:** `Exception: No Bronze rows found for ingest_run_id = test-run-001`  
**Cause 1:** Running notebook manually with default fallback run ID, but Bronze was written with a different ID.  
**Fix 1:** Run `spark.table("bhg_bronze.Notes.br_tbl3pArnote").select("_ingest_run_id").distinct().show()` to see what IDs are in Bronze, then update the fallback.  
**Cause 2:** Copy activity wrote 0 rows (see above).

---

### RowChkSum always differs (every row triggers full update on every run)

**Symptom:** Every run updates every row in Silver even when nothing changed.  
**Cause:** The CHECKSUM column list in the Fabric Copy query does not match the columns used by `SelectConstructor` for ActionKey=1, StepKey=34/35.  
**Fix:** Run the `dms.tbl_MapSrc2Dsn` query in Section 20 (#5) to confirm the enabled field list. Update the `CHECKSUM(...)` expression in both Copy activity queries to exactly match.

---

### Silver MERGE matches on wrong rows (ClaimNote duplicate key errors)

**Symptom:** ClaimNote Silver has duplicate `tpcnTPCID` values or MERGE produces unexpected results.  
**Cause:** The MERGE is accidentally using `tpcn` as the key instead of `tpcnTPCID`.  
**Fix:** Verify that the MERGE condition in `nb_3pclaimnote_bronze_to_silver` Cell 2 reads:
```python
"tgt._site_code = src._site_code AND tgt.tpcnTPCID = src.tpcnTPCID"
```
NOT `tgt.tpcn = src.tpcn`. See [Section 19](#19-the-claimnote-match-key-anomaly-tpcntpcid-vs-tpcn).

---

*End of Implementation Guide*
