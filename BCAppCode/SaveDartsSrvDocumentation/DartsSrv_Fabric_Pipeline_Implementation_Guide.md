# DartsSrv — Microsoft Fabric ETL Pipeline Implementation Guide

**Parent Pipeline:** `Execute_DartSrv`  
**Child Pipeline:** `dartSRV_Pipeline` (Bronze extraction)  
**Data:** Counseling session records (`tblDartsSrv`) from SAMMS SQL Server clinic databases  
**Destination:** Bronze Lakehouse → Silver Lakehouse → Gold Warehouse  
**Author Reference:** `dartdefintion.txt`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [Prerequisites — What You Need Before Starting](#3-prerequisites--what-you-need-before-starting)
4. [Pipeline Parameters — Define These First](#4-pipeline-parameters--define-these-first)
5. [Step 1 — Create the Child Pipeline: dartSRV_Pipeline](#5-step-1--create-the-child-pipeline-dartsrv_pipeline)
6. [Step 2 — Add the ForEach Activity (Child)](#6-step-2--add-the-foreach-activity-child)
7. [Step 3 — Inside ForEach: Lookup for Optional Columns](#7-step-3--inside-foreach-lookup-for-optional-columns)
8. [Step 4 — Inside ForEach: Copy Activity (Bronze Extraction)](#8-step-4--inside-foreach-copy-activity-bronze-extraction)
9. [Step 5 — Create the Parent Pipeline: Execute_DartSrv](#9-step-5--create-the-parent-pipeline-execute_dartsrv)
10. [Step 6 — Parent: ExecutePipeline (Invoke Child)](#10-step-6--parent-executepipeline-invoke-child)
11. [Step 7 — Parent: Notebook Bronze → Silver](#11-step-7--parent-notebook-bronze--silver)
12. [Step 8 — Parent: Truncate Gold Table](#12-step-8--parent-truncate-gold-table)
13. [Step 9 — Parent: Copy Silver → Gold](#13-step-9--parent-copy-silver--gold)
14. [Step 10 — Notebook: nb_darts_bronze_to_silver](#14-step-10--notebook-nb_darts_bronze_to_silver)
15. [Step 11 — Notebook Cell 1: Load Bronze and Prepare Silver](#15-step-11--notebook-cell-1-load-bronze-and-prepare-silver)
16. [Step 12 — Notebook Cell 2: Merge Into Silver](#16-step-12--notebook-cell-2-merge-into-silver)
17. [End-to-End Flow Summary](#17-end-to-end-flow-summary)
18. [How Change Detection Works (RowChkSum)](#18-how-change-detection-works-rowchksum)
19. [RowChkSum — Fabric vs Old C# ETL](#19-rowchksum--fabric-vs-old-c-etl)
20. [RowState in Fabric Silver and Gold](#20-rowstate-in-fabric-silver-and-gold)
21. [Architectural Decisions — Lookback and Single Silver Table](#21-architectural-decisions--lookback-and-single-silver-table)
22. [Why Five Date Columns in the WHERE Clause](#22-why-five-date-columns-in-the-where-clause)
23. [Troubleshooting Guide](#23-troubleshooting-guide)

---

## 1. Architecture Overview

The implementation uses **two pipelines**: a child pipeline for SAMMS → Bronze extraction, and a parent pipeline that orchestrates Bronze → Silver → Gold.

```
┌──────────────────────────────────────────────────────────────────────────┐
│  PARENT: Execute_DartSrv                                                   │
│                                                                          │
│  1) Exected_AfterBronz (ExecutePipeline)                                 │
│     └─ invokes child dartSRV_Pipeline → loads Bronze                     │
│                         │ Succeeded                                      │
│                         ▼                                                │
│  2) nb_darts_bronze_to_silver (Notebook)                                │
│     └─ Bronze → Silver MERGE (Delta upsert)                              │
│                         │ Succeeded                                      │
│                         ▼                                                │
│  3) Truncate_GoldTable_DartSrv (Script)                                  │
│     └─ TRUNCATE TABLE pats.gd_darts_srv                                  │
│                         │ Succeeded                                      │
│                         ▼                                                │
│  4) copy_darts_silver_to_gold (Copy)                                    │
│     └─ bhg_silver.pats.sl_tbldartsrv → bhg_gold.pats.gd_darts_srv        │
└──────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────────────────────────────────────────────────────┐
│  CHILD: dartSRV_Pipeline                                                 │
│                                                                          │
│  ForEach Site (fe_each_samms_database_copy1)                             │
│    1) lkp_check_optional_columns_exist (Lookup)                          │
│       Checks: ServiceType, dsTelehealthSession, HoldId, upsize_ts        │
│    2) Copy data2 (Copy)                                                  │
│       SAMMS → APPEND bhg_bronze.Dart.br_tblDartSrv                       │
└──────────────────────────────────────────────────────────────────────────┘
```

**Layer Targets:**

| Layer | Artifact | Schema | Table |
|---|---|---|---|
| Bronze | `bhg_bronze` Lakehouse | `Dart` | `br_tblDartSrv` |
| Silver | `bhg_silver` Lakehouse | `pats` | `sl_tblDartSrv` |
| Gold | `bhg_gold` Warehouse | `pats` | `gd_darts_srv` |

**Pipeline IDs (from definition):**

| Pipeline | Object ID |
|---|---|
| Child `dartSRV_Pipeline` | `b2101370-34a9-4695-bf4a-eebdb3ced50b` |
| Parent `Execute_DartSrv` | `12ad712f-3d73-48e9-b4b0-826ab69e388c` |

---

## 2. What This Pipeline Does — Plain English

The SAMMS system is an on-premise SQL Server database installed at each BHG clinic. Every counseling session a patient has is recorded in a table called `tblDartsSrv` inside that clinic's SAMMS database. There are 80+ clinics, each with their own SAMMS database.

This pipeline runs in two stages:

**Child pipeline (`dartSRV_Pipeline`):**
1. Visits each clinic's SAMMS database one at a time (ForEach loop)
2. Checks whether optional columns exist (`ServiceType`, `dsTelehealthSession`, `HoldId`, `upsize_ts`)
3. Extracts DartsSrv records touched within the lookback window (default 15 days)
4. Appends rows to Bronze with run metadata (`_ingest_run_id`, `_extracted_at`, etc.)

**Parent pipeline (`Execute_DartSrv`):**
5. Invokes the child pipeline and waits for Bronze load to finish
6. Runs notebook `nb_darts_bronze_to_silver` — MERGE Bronze into Silver using RowChkSum
7. Truncates Gold table `pats.gd_darts_srv`
8. Copies full Silver table into Gold warehouse with column renaming (`_site_code` → `SiteCode`, `silver_updated_at` → `LastModAt`, etc.)

---

## 3. Prerequisites — What You Need Before Starting

Before you build this pipeline, the following must already exist in your Fabric workspace:

| Item | What It Is | Where It Lives |
|---|---|---|
| SAMMS SQL Server connection | A Fabric connection to the on-premise SAMMS SQL Server gateway | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse | Bronze layer Lakehouse | Fabric workspace |
| `bhg_silver` Lakehouse | Silver layer Lakehouse | Fabric workspace |
| `bhg_gold` Warehouse | Gold layer (Fabric Data Warehouse) | Fabric workspace |
| `Dart` schema in `bhg_bronze` | Schema for DartsSrv Bronze tables | `bhg_bronze` Lakehouse |
| `pats` schema in `bhg_silver` | Schema for Silver patient tables | `bhg_silver` Lakehouse |
| `pats.gd_darts_srv` in Gold | Final reporting/analytics table | `bhg_gold` Warehouse |
| On-premise data gateway | Configured to reach SAMMS SQL Servers | Fabric settings |

**Connection ID to note down:** When you create the SAMMS SQL Server connection in Fabric, it gets a GUID. In this implementation the connection ID is:
```
9743b95a-fd66-4f7c-9767-e6eb0f1ecab7
```
Replace this with your actual connection GUID everywhere it appears.

**Workspace ID to note down:** Your Fabric workspace ID is:
```
c5097ffb-b78e-441d-9575-a82bac23cac8
```

**Bronze Lakehouse Artifact ID:**
```
77d24027-6a1c-43a8-a998-1a14dd3c0d52
```

**Silver Lakehouse Artifact ID:**
```
dd09d8b6-d862-4954-a0b2-fcf7372c6595
```

**Gold Warehouse Artifact ID:**
```
d29ef036-8c2c-40b0-a8e0-3279f9a906e7
```

---

## 4. Pipeline Parameters — Define These First

When you create the pipeline in Fabric, the very first thing you do is define three pipeline-level parameters. These are inputs that control the behavior of the entire pipeline on every run.

Go to: **Pipeline canvas → click empty space → Parameters tab → + New**

### Parameter 1: `p_ingest_run_id`

| Setting | Value |
|---|---|
| Name | `p_ingest_run_id` |
| Type | `String` |
| Default Value | `test-run-001` |

**Why:** Every time this pipeline runs, it needs a unique ID so the notebook can find exactly which rows were extracted in this run. When you trigger the pipeline in production you pass something like `DARTS-2026-05-07-001`. In test mode the default `test-run-001` is used. Every row written to Bronze will carry this ID in the `_ingest_run_id` column.

---

### Parameter 2: `p_lookback_days`

| Setting | Value |
|---|---|
| Name | `p_lookback_days` |
| Type | `Int` |
| Default Value | `15` |

**Why:** Instead of re-extracting all history on every run (which would be millions of rows), we only pull records that were touched in the last N days. The default is 15 days. For a deeper historical catch-up run, increase this to 90 or 200 when you trigger the pipeline manually.

---

### Parameter 3: `p_sites`

| Setting | Value |
|---|---|
| Name | `p_sites` |
| Type | `Array` |
| Default Value | See JSON below |

**Default Value (paste this exactly):**
```json
[
  {
    "site_code": "AHK",
    "source_database": "SAMMS-Ahoskie",
    "source_schema": "dbo",
    "source_table": "tblDartsSrv"
  }
]
```

**Why:** Each object in `p_sites` represents one SAMMS clinic database. The ForEach loop and Lookup query read all four properties from each item.

| Property | Meaning | Example |
|---|---|---|
| `site_code` | Clinic code written to `_site_code` in Bronze | `AHK` |
| `source_database` | SAMMS database name on SQL Server | `SAMMS-Ahoskie` |
| `source_schema` | Schema (always `dbo` for DartsSrv) | `dbo` |
| `source_table` | Source table (always `tblDartsSrv`) | `tblDartsSrv` |

> **Important:** All four properties must be present in every `p_sites` object. The Lookup and Copy queries use `item().source_schema` and `item().source_table`.

> **Parent vs child:** Child pipeline `dartSRV_Pipeline` defines `p_lookback_days` as **Int**. Parent pipeline `Execute_DartSrv` defines it as **String** — both default to `15`. When invoking the child from the parent, pass the integer value explicitly.

> **To add more clinics:** Add more objects to the JSON array with the same four properties.

---

## 5. Step 1 — Create the Child Pipeline: dartSRV_Pipeline

1. Open your Fabric workspace
2. Click **+ New** → **Data pipeline**
3. Name it: `dartSRV_Pipeline`
4. Click **Create**
5. Add all three parameters from Section 4 (`p_ingest_run_id`, `p_lookback_days` as **Int**, `p_sites`)
6. Build the ForEach + Lookup + Copy activities (Steps 2–4 below)

This child pipeline only handles **SAMMS → Bronze**. The parent pipeline `Execute_DartSrv` (Step 5) invokes it.

---

## 6. Step 2 — Add the ForEach Activity (Child)

The ForEach activity is the outer loop. It runs the inner activities once for each clinic in the `p_sites` array.

### Add the activity
1. From the **Activities** toolbar, drag a **ForEach** activity onto the canvas
2. Rename it: `fe_each_samms_database_copy1`

### Configure ForEach Settings tab

| Setting | Value | Why |
|---|---|---|
| Items | `@pipeline().parameters.p_sites` | This tells it to loop over the array you defined in `p_sites` |
| Sequential | **Checked (True)** | Run one clinic at a time to avoid overloading the SAMMS gateway |
| Batch count | Leave blank | Not needed when Sequential is true |

> **Why Sequential = True?**  
> The SAMMS SQL Servers are on-premise clinic machines with limited resources. Running all 80 clinics in parallel would overwhelm the gateway and cause timeouts. One at a time is safe and reliable.

### The JSON for this activity (for reference / direct paste into JSON editor)
```json
{
    "name": "fe_each_samms_database_copy1",
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

> The `"activities": []` section will be filled in Steps 3 and 4.

---

## 7. Step 3 — Inside ForEach: Lookup for Optional Columns

Four columns in `tblDartsSrv` do not exist in all SAMMS versions. This Lookup checks all four in one query and returns a flag (1 or 0) for each. The Copy activity uses those flags to select the real column or a NULL placeholder.

| Optional Column | Type | Why optional |
|---|---|---|
| `ServiceType` | varchar(100) | Added in a later SAMMS version |
| `dsTelehealthSession` | bit | Telehealth tracking — newer SAMMS only |
| `HoldId` | int | Hold management — newer SAMMS only |
| `upsize_ts` | varbinary(8) | SQL Server replication timestamp — not in all clinics |

### Add the activity
1. Click **Edit** on the ForEach activity to open its inner canvas
2. Drag a **Lookup** activity onto the inner canvas
3. Rename it: `lkp_check_optional_columns_exist`

### Configure Settings tab

| Setting | Value |
|---|---|
| Source dataset | Create a new SQL Server dataset using connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| Query | Use the expression below |

### Query — paste this into the Query field (Expression mode)

```
@concat(
'SELECT
    COUNT(CASE WHEN c.name = ''ServiceType''         THEN 1 END) AS service_type_exists,
    COUNT(CASE WHEN c.name = ''dsTelehealthSession'' THEN 1 END) AS telehealth_exists,
    COUNT(CASE WHEN c.name = ''HoldId''              THEN 1 END) AS holdid_exists,
    COUNT(CASE WHEN c.name = ''upsize_ts''           THEN 1 END) AS upsize_ts_exists
FROM [', item().source_database, '].sys.columns c
INNER JOIN [', item().source_database, '].sys.tables t
    ON c.object_id = t.object_id
INNER JOIN [', item().source_database, '].sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = ''', item().source_schema, '''
  AND t.name = ''', item().source_table, '''
  AND c.name IN (''ServiceType'', ''dsTelehealthSession'', ''HoldId'', ''upsize_ts'');'
)
```

**Example SQL built for AHK:**
```sql
SELECT
    COUNT(CASE WHEN c.name = 'ServiceType'         THEN 1 END) AS service_type_exists,
    COUNT(CASE WHEN c.name = 'dsTelehealthSession' THEN 1 END) AS telehealth_exists,
    COUNT(CASE WHEN c.name = 'HoldId'              THEN 1 END) AS holdid_exists,
    COUNT(CASE WHEN c.name = 'upsize_ts'           THEN 1 END) AS upsize_ts_exists
FROM [SAMMS-Ahoskie].sys.columns c
...
WHERE s.name = 'dbo' AND t.name = 'tblDartsSrv'
  AND c.name IN ('ServiceType', 'dsTelehealthSession', 'HoldId', 'upsize_ts');
```

**What it returns:** A single row with four columns:

| Output column | Value if exists | Value if absent |
|---|---|---|
| `service_type_exists` | `1` | `0` |
| `telehealth_exists` | `1` | `0` |
| `holdid_exists` | `1` | `0` |
| `upsize_ts_exists` | `1` | `0` |

**How to access in Copy activity:**
```
activity('lkp_check_optional_columns_exist').output.firstRow.service_type_exists
activity('lkp_check_optional_columns_exist').output.firstRow.telehealth_exists
activity('lkp_check_optional_columns_exist').output.firstRow.holdid_exists
activity('lkp_check_optional_columns_exist').output.firstRow.upsize_ts_exists
```

### Configure Policy tab

| Setting | Value |
|---|---|
| Timeout | `0.12:00:00` (12 hours) |
| Retry | `0` |
| Retry interval | `30` seconds |

### The JSON for this activity
```json
{
    "name": "lkp_check_optional_columns_exist",
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
                "value": "@concat(\n'SELECT\n    COUNT(CASE WHEN c.name = ''ServiceType''         THEN 1 END) AS service_type_exists,\n    COUNT(CASE WHEN c.name = ''dsTelehealthSession'' THEN 1 END) AS telehealth_exists,\n    COUNT(CASE WHEN c.name = ''HoldId''              THEN 1 END) AS holdid_exists,\n    COUNT(CASE WHEN c.name = ''upsize_ts''           THEN 1 END) AS upsize_ts_exists\nFROM [', item().source_database, '].sys.columns c\nINNER JOIN [', item().source_database, '].sys.tables t\n    ON c.object_id = t.object_id\nINNER JOIN [', item().source_database, '].sys.schemas s\n    ON t.schema_id = s.schema_id\nWHERE s.name = ''', item().source_schema, '''\n  AND t.name = ''', item().source_table, '''\n  AND c.name IN (''ServiceType'', ''dsTelehealthSession'', ''HoldId'', ''upsize_ts'');'\n)",
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

## 8. Step 4 — Inside ForEach: Copy Activity (Bronze Extraction)

This is the main data extraction activity. It runs after the Lookup succeeds.

### What it does
- Connects to the clinic's SAMMS SQL Server
- Runs a SELECT query that pulls all DartsSrv rows touched within the lookback window
- Appends all those rows to the Bronze Lakehouse table `bhg_bronze.Dart.br_tblDartSrv`
- Adds metadata columns (`_site_code`, `_ingest_run_id`, `_extracted_at`, etc.) to every row

### Add the activity
1. Still on the inner canvas of ForEach
2. Drag a **Copy** activity onto the canvas
3. Rename it: `Copy data2`
4. Draw a dependency arrow: from `lkp_check_optional_columns_exist` → `Copy data2` with condition **Succeeded**

This means: only run the copy if the Lookup completed without error.

### Configure Source tab

| Setting | Value |
|---|---|
| Source type | SQL Server |
| Connection | `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query | **Query** |
| Query | Use the full expression below (Expression mode) |
| Query timeout | `02:00:00` |

### The Source Query Expression (paste this into the Query field)

Switch the Query field to **Expression mode** and paste this entire block:

```
@concat(
'SELECT
    ''', item().site_code, ''' AS _site_code,
    ''', item().source_database, ''' AS _source_database,
    ''', pipeline().parameters.p_ingest_run_id, ''' AS _ingest_run_id,
    GETDATE() AS _extracted_at,
    CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())) AS _source_query_start_date,
    CONVERT(date, GETDATE()) AS _source_query_end_date,

    dsID,
    dsClt,
    dsDIM1,
    dsDIM2,
    dsDIM3,
    dsDIM4,
    dsDIM5,
    dsDIM6,
    dsTxtSrv,
    dsDtStart,
    dsDtEnd,
    dsTxtType,
    dsdblUnits,
    dsNoteID,
    dsDtAdded,
    dstxtStaff,
    dstxtNote,
    dsRTBNOTE,
    DSbilled,
    dsGROUPNUM,
    dsPROGRAM,
    dsUpdate,
    dsUPDATEStaff,
    ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.upsize_ts_exists, 1),
    'upsize_ts',
    'CAST(NULL AS varbinary(8)) AS upsize_ts'
),
',
    dsInvalidatedOn,
    dsError,
    dsTxtHIV,
    dsDartsGroup,
    repOldSrv,
    dsSignature,
    dsSigDate,
    dssigdateCOSIGN,
    dssignatureCOSIGN,
    dsSigUser,
    dsSigUserCosign,
    dsSIGCLT,
    dsSIGCLTDATE,
    dsSIGCLTUSER,
    dsAPTID,
    dsuncharted,
    dsTxDim1,
    dsTxDim2,
    dsTxDim3,
    dsTxDim4,
    dsTxDim5,
    dsTxDim6,
    dsDIAG,
    dsArea,
    dsGroupDefaultNote,
    dsGroupEnd,
    dsGroupIdentity,
    dsGroupStart,
    dsDIAG10,
    SiteID,
    dsDBnotes,
    dsSigCltImg,
    dsSignatureCoSignImg,
    dsSignatureIMG,
    MG,
    ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.service_type_exists, 1),
    'ServiceType',
    'CAST(NULL AS varchar(100)) AS ServiceType'
),
',
    ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.telehealth_exists, 1),
    'dsTelehealthSession',
    'CAST(NULL AS bit) AS dsTelehealthSession'
),
',
    ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.holdid_exists, 1),
    'HoldId',
    'CAST(NULL AS int) AS HoldId'
),
',

    RowChkSum = CHECKSUM(
        dsID,
        dsClt,
        dsDIM1,
        dsDIM2,
        dsDIM3,
        dsDIM4,
        dsDIM5,
        dsDIM6,
        dsTxtSrv,
        dsDtStart,
        dsDtEnd,
        dsTxtType,
        dsdblUnits,
        dsNoteID,
        dsDtAdded,
        dstxtStaff,
        DSbilled,
        dsGROUPNUM,
        dsPROGRAM,
        dsUpdate,
        dsUPDATEStaff,
        dsInvalidatedOn,
        dsError,
        dsTxtHIV,
        dsDartsGroup,
        repOldSrv,
        dsSigDate,
        dssigdateCOSIGN,
        dsSigUser,
        dsSigUserCosign,
        dsSIGCLTDATE,
        dsSIGCLTUSER,
        dsAPTID,
        dsuncharted,
        dsTxDim1,
        dsTxDim2,
        dsTxDim3,
        dsTxDim4,
        dsTxDim5,
        dsTxDim6,
        dsDIAG,
        dsArea,
        dsGroupDefaultNote,
        dsGroupEnd,
        dsGroupIdentity,
        dsGroupStart,
        dsDIAG10,
        SiteID,
        dsDBnotes,
        MG,
        ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.service_type_exists, 1),
    'ServiceType',
    'CAST(NULL AS varchar(100))'
),
',
        ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.telehealth_exists, 1),
    'dsTelehealthSession',
    'CAST(NULL AS bit)'
),
',
        ',
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.holdid_exists, 1),
    'HoldId',
    'CAST(NULL AS int)'
),
'
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table, '
WHERE dsClt IS NOT NULL
  AND (
       CONVERT(date, dsdtstart) >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
    OR CONVERT(date, dsDtAdded) >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
    OR CONVERT(date, dsUpdate) >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
    OR CONVERT(date, dsBilled) >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
    OR CONVERT(date, dsSigDate) >= CONVERT(date, DATEADD(day, -', string(pipeline().parameters.p_lookback_days), ', GETDATE()))
    OR dsClt <= 0
  )
ORDER BY 1, 2'
)
```

### Breaking Down What This Query Does

#### Metadata columns (first 6 columns)

```sql
'AHK'                         AS _site_code,
'SAMMS-Ahoskie'               AS _source_database,
'test-run-001'                AS _ingest_run_id,
GETDATE()                     AS _extracted_at,          -- exact extraction timestamp
CONVERT(date, DATEADD(day,-15,GETDATE())) AS _source_query_start_date,  -- lookback start
CONVERT(date, GETDATE())      AS _source_query_end_date  -- lookback end (today)
```

These columns are added by the pipeline (not from SAMMS). They are metadata that lets you trace every row back to exactly when and where it was extracted.

#### Data columns

All clinical data columns from `tblDartsSrv`, including conditional `upsize_ts`. See [Section 18](#18-how-change-detection-works-rowchksum).

#### Optional columns — four conditional columns

After `dsUPDATEStaff`, `upsize_ts` is conditional. After `MG`, `ServiceType`, `dsTelehealthSession`, and `HoldId` are conditional based on Lookup flags.

**CHECKSUM:** `ServiceType`, `dsTelehealthSession`, and `HoldId` are included in RowChkSum (NULL cast when absent). `upsize_ts` is extracted but excluded from CHECKSUM.

#### RowChkSum — change detection fingerprint

```sql
RowChkSum = CHECKSUM(dsID, dsClt, dsDIM1, ..., MG, ServiceType?, dsTelehealthSession?, HoldId?)
```

SQL Server's `CHECKSUM()` produces a row fingerprint. The Silver notebook compares Bronze vs Silver RowChkSum. See [Section 18](#18-how-change-detection-works-rowchksum).

> **Excluded from CHECKSUM:** `dstxtNote`, `dsRTBNOTE`, signature text/image columns, and `upsize_ts`.

#### WHERE clause — lookback filter

```sql
WHERE dsClt IS NOT NULL
  AND (
       CONVERT(date, dsdtstart) >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsDtAdded) >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsUpdate)  >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsBilled)  >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsSigDate) >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR dsClt <= 0
  )
ORDER BY 1, 2
```

See [Section 22](#22-why-five-date-columns-in-the-where-clause) for a detailed explanation of why five date columns are used.

### Configure Sink tab

| Setting | Value |
|---|---|
| Sink type | **Lakehouse** |
| Linked service name | `bhg_bronze` |
| Workspace ID | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder | `Tables` |
| Schema | `Dart` |
| Table | `br_tblDartSrv` |
| Table action | **Append** |
| Apply V-Order | No (unchecked) |

> **Why Append?**  
> Every pipeline run appends its rows into the same Bronze table. Rows from different runs coexist in Bronze, tagged by `_ingest_run_id`. The notebook later filters to only the rows belonging to the current run. Bronze is a permanent, immutable landing zone — you never overwrite it. This preserves a full history of every extraction.

### Configure Mapping / Translator tab

| Setting | Value |
|---|---|
| Type conversion | **Enabled** |
| Allow data truncation | **True** |
| Treat boolean as number | **False** |

### Configure Policy tab

| Setting | Value |
|---|---|
| Timeout | `0.12:00:00` (12 hours) |
| Retry | `0` |
| Retry interval | `30` seconds |

### The Full JSON for this activity
```json
{
    "name": "Copy data2",
    "type": "Copy",
    "dependsOn": [
        {
            "activity": "lkp_check_optional_columns_exist",
            "dependencyConditions": ["Succeeded"]
        }
    ],
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
                "value": "<<paste the full @concat expression from above>>",
                "type": "Expression"
            },
            "queryTimeout": "02:00:00",
            "partitionOption": "None",
            "datasetSettings": {
                "annotations": [],
                "type": "SqlServerTable",
                "schema": [],
                "externalReferences": {
                    "connection": "9743b95a-fd66-4f7c-9767-e6eb0f1ecab7"
                }
            }
        },
        "sink": {
            "type": "LakehouseTableSink",
            "tableActionOption": "Append",
            "partitionOption": "None",
            "applyVOrder": false,
            "datasetSettings": {
                "annotations": [],
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
                "typeProperties": {
                    "schema": "Dart",
                    "table": "br_tblDartSrv"
                }
            }
        },
        "enableStaging": false,
        "translator": {
            "type": "TabularTranslator",
            "typeConversion": true,
            "typeConversionSettings": {
                "allowDataTruncation": true,
                "treatBooleanAsNumber": false
            }
        }
    }
}
```

---

## 9. Step 5 — Create the Parent Pipeline: Execute_DartSrv

1. Click **+ New** → **Data pipeline**
2. Name it: `Execute_DartSrv`
3. Add parameters:
   - `p_ingest_run_id` (String, default `test-run-001`)
   - `p_sites` (Array — same JSON as child)
   - `p_lookback_days` (String, default `15`) — note: parent uses String type
4. Add four activities in order (Steps 6–9)

The parent pipeline owns Bronze → Silver → Gold. It does **not** contain the ForEach — that lives in the child.

---

## 10. Step 6 — Parent: ExecutePipeline (Invoke Child)

### Add the activity
1. Drag **Execute Pipeline** onto the parent canvas
2. Rename: `Exected_AfterBronz`
3. No upstream dependency (runs first)

### Configure Settings tab

| Setting | Value |
|---|---|
| Invoked pipeline | `dartSRV_Pipeline` |
| Wait on completion | **True** |

### Parameters passed to child

| Child parameter | Value |
|---|---|
| `p_ingest_run_id` | `@pipeline().parameters.p_ingest_run_id` |
| `p_lookback_days` | `15` (or pass from parent parameter) |
| `p_sites` | `@pipeline().parameters.p_sites` |

### JSON reference
```json
{
    "name": "Exected_AfterBronz",
    "type": "ExecutePipeline",
    "dependsOn": [],
    "typeProperties": {
        "pipeline": {
            "referenceName": "b2101370-34a9-4695-bf4a-eebdb3ced50b",
            "type": "PipelineReference"
        },
        "waitOnCompletion": true,
        "parameters": {
            "p_ingest_run_id": "test-run-001",
            "p_lookback_days": 15,
            "p_sites": {
                "value": "@pipeline().parameters.p_sites",
                "type": "Expression"
            }
        }
    }
}
```

---

## 11. Step 7 — Parent: Notebook Bronze → Silver

### Add the activity
1. Drag **Notebook** onto the parent canvas
2. Rename: `nb_darts_bronze_to_silver`
3. Dependency: `Exected_AfterBronz` → **Succeeded**

### Configure Settings tab

| Setting | Value |
|---|---|
| Notebook | `nb_darts_bronze_to_silver` |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Notebook ID | `89769158-4ba9-4e86-9b6d-ff22852dddd5` |

### Notebook parameter

| Parameter | Value |
|---|---|
| `p_ingest_run_id` | `@pipeline().parameters.p_ingest_run_id` (Expression) |

> **Note:** The notebook runs on the **parent** canvas, not inside the child. The child pipeline has an inactive `nb_darts_silver_to_gold` notebook — ignore it; Gold is loaded via Copy in the parent.

---

## 12. Step 8 — Parent: Truncate Gold Table

After Silver MERGE completes, Gold is fully refreshed (truncate + reload, not incremental merge).

### Add the activity
1. Drag **Script** activity onto parent canvas
2. Rename: `Truncate_GoldTable_DartSrv`
3. Dependency: `nb_darts_bronze_to_silver` → **Succeeded**

### Configure Settings tab

| Setting | Value |
|---|---|
| Connection | `bhg_gold` (Fabric Data Warehouse) |
| Script | `TRUNCATE TABLE pats.gd_darts_srv` |

### JSON reference
```json
{
    "name": "Truncate_GoldTable_DartSrv",
    "type": "Script",
    "dependsOn": [
        {
            "activity": "nb_darts_bronze_to_silver",
            "dependencyConditions": ["Succeeded"]
        }
    ],
    "linkedService": {
        "name": "bhg_gold"
    },
    "typeProperties": {
        "scripts": [
            {
                "type": "Query",
                "text": {
                    "value": "TRUNCATE TABLE pats.gd_darts_srv",
                    "type": "Expression"
                }
            }
        ],
        "scriptBlockExecutionTimeout": "02:00:00"
    }
}
```

---

## 13. Step 9 — Parent: Copy Silver → Gold

### Add the activity
1. Drag **Copy** onto parent canvas
2. Rename: `copy_darts_silver_to_gold`
3. Dependency: `Truncate_GoldTable_DartSrv` → **Succeeded**

### Configure Source tab

| Setting | Value |
|---|---|
| Source | Lakehouse table |
| Lakehouse | `bhg_silver` |
| Schema | `pats` |
| Table | `sl_tbldartsrv` |

### Configure Sink tab

| Setting | Value |
|---|---|
| Sink | Data Warehouse table |
| Warehouse | `bhg_gold` |
| Schema | `pats` |
| Table | `gd_darts_srv` |
| Write behavior | Insert |
| Allow COPY command | **True** |
| Enable staging | **True** |

### Column mappings (Silver → Gold)

The Copy activity uses explicit mappings. Key renames:

| Silver source | Gold sink |
|---|---|
| `_site_code` | `SiteCode` |
| `silver_updated_at` | `LastModAt` |
| `dsID` | `DsId` |
| `dsClt` | `DsClt` |
| `dsDtStartYear` | `DsDtStartYear` |
| `RowState` | `RowState` |
| `RowChkSum` | `RowChkSum` |
| `upsize_ts` | `upsize_ts` |
| `ServiceType` | `ServiceType` |
| `dsTelehealthSession` | `dsTelehealthSession` |
| `HoldId` | `HoldId` |

All other columns follow PascalCase in Gold (e.g. `dsTxtSrv` → `DsTxtSrv`, `dsDtStart` → `DsDtStart`). See `dartdefintion.txt` for the full mapping list (50+ columns).

> **Gold load strategy:** Truncate + full copy from Silver every run. Silver holds the merged current state; Gold is a reporting snapshot refreshed after each pipeline run.

---

## 14. Step 10 — Notebook: nb_darts_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_darts_bronze_to_silver`
3. Make sure it is attached to a Spark environment that has access to both `bhg_bronze` and `bhg_silver` Lakehouses
4. The notebook has **two cells** — Cell 1 and Cell 2

> **Important:** Add both Lakehouses to the notebook's "Lakehouse" panel on the left side before running. This is how the notebook can reference them by name (`bhg_bronze.Dart.br_tblDartSrv`, `bhg_silver.pats.sl_tblDartSrv`).

---

## 15. Step 11 — Notebook Cell 1: Load Bronze and Prepare Silver

### What Cell 1 does
1. Receives `p_ingest_run_id` from the parent pipeline
2. Reads Bronze filtered to this run only
3. Deduplicates on `_site_code + dsID` (keep latest `_extracted_at`)
4. Adds: `dsDtStartYear`, `silver_updated_at`, `last_seen_at`, `RowState`
5. Creates Silver table on first run, or adds `upsize_ts` column if missing on existing table
6. Stores `src_df` for Cell 2

### Cell 1 Code — paste this exactly

```python
from pyspark.sql.functions import col, current_timestamp, year, row_number, when
from pyspark.sql.window import Window

# The pipeline passes p_ingest_run_id as a parameter.
# The try/except allows you to run this cell manually during development
# without the pipeline by falling back to a hardcoded test value.
try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Dart.br_tblDartSrv"
silver_table = "bhg_silver.pats.sl_tblDartSrv"

print(f"Processing ingest_run_id: {p_ingest_run_id}")
print(f"Bronze table: {bronze_table}")
print(f"Silver table: {silver_table}")

# Read only the rows from THIS pipeline run.
# Bronze is an append-only table with rows from many runs.
# We filter to p_ingest_run_id so we only process today's extraction.
bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

# Safety check: if no rows found, the pipeline or copy activity failed.
# Raising an exception here marks the notebook activity as failed in the pipeline.
if bronze_count == 0:
    raise Exception(f"No Bronze rows found for ingest_run_id = {p_ingest_run_id}")

# Deduplicate within current run.
# Key = _site_code + dsID.
# If the same record appears twice (e.g., due to a retry), keep the latest one.
w = Window.partitionBy("_site_code", "dsID").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("dsID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")
    # Extract the year from session start date.
    # Used for partitioning and year-based filtering in Silver.
    .withColumn("dsDtStartYear", year(col("dsDtStart")))
    # Timestamp set every time this row is written/updated in Silver.
    .withColumn("silver_updated_at", current_timestamp())
    # Timestamp of when this row was last seen in a SAMMS extraction.
    .withColumn("last_seen_at", current_timestamp())
    # Matches the stored procedure logic: 1 = active/valid patient, 0 = placeholder (dsClt < 0).
    .withColumn("RowState", when(col("dsClt") >= 0, 1).otherwise(0))
)

# Create a temp view so you can query src_df with SQL if needed for debugging.
src_df.createOrReplaceTempView("vw_darts_current_run")

src_count = src_df.count()
print(f"Prepared source rows for Silver: {src_count}")

# If the Silver table does not exist yet (first-ever run), create it.
# This uses overwrite mode to write the full initial load.
# silver_created_at is added here and will never be overwritten in future updates.
if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write
        .format("delta")
        .mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created Silver table and inserted rows: {src_count}")
else:
    print(f"Silver table already exists: {silver_table}")
    silver_columns = {field.name for field in spark.table(silver_table).schema.fields}
    if "upsize_ts" not in silver_columns:
        spark.sql(f"ALTER TABLE {silver_table} ADD COLUMNS (upsize_ts BINARY)")
        print("Added missing Silver column: upsize_ts")
```

### Explanation of each section

| Code Section | Why It Exists |
|---|---|
| `try: p_ingest_run_id` | When you run the notebook manually (outside the pipeline), this variable doesn't exist. The fallback lets you test without the pipeline. |
| `spark.table(bronze_table).where(...)` | Bronze accumulates rows from every run. We must filter to only the current run's rows using `_ingest_run_id`. |
| `if bronze_count == 0: raise Exception` | Fail loudly. If no Bronze rows exist for this run, something went wrong upstream (the Copy activity may have silently produced zero rows). Better to fail here than silently produce an empty Silver update. |
| `Window.partitionBy("_site_code", "dsID").orderBy(...)` | In theory each `_site_code + dsID` should appear exactly once. But if a run was retried or the ForEach had a partial failure, duplicates are possible. This window function keeps only the newest extraction of each record. |
| `.withColumn("dsDtStartYear", year(col("dsDtStart")))` | Extracts the year (e.g., `2023`) from the session start date. Useful for partitioning the Silver table and for year-based reporting queries. |
| `.withColumn("RowState", when(col("dsClt") >= 0, 1).otherwise(0))` | Mirrors the stored procedure logic exactly: `1` = active/valid patient record (`dsClt >= 0`), `0` = placeholder/test record (`dsClt < 0`). Auto-included in Cell 2 merge since it is part of `src_df.columns`. |
| `silver_created_at` in the initial write | This timestamp marks when the record was FIRST ever loaded into Silver. It is set once here and never overwritten in Cell 2's merge logic. |
| `if not spark.catalog.tableExists(silver_table)` | First time ever: create the Silver table. Subsequent runs: skip this block and go to Cell 2's merge. |
| `ALTER TABLE ... ADD COLUMNS (upsize_ts BINARY)` | If Silver was created before `upsize_ts` was added to Bronze extraction, this adds the column without rebuilding the table. |

---

## 16. Step 12 — Notebook Cell 2: Merge Into Silver

### What Cell 2 does
1. Opens the Silver Delta table
2. Runs a Delta MERGE (upsert) comparing incoming Bronze data against existing Silver data
3. Uses `_site_code + dsID` as the unique record key
4. Uses `RowChkSum` to decide whether to do a full update or just a lightweight metadata touch
5. Inserts brand new records

### Cell 2 Code — paste this exactly

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tblDartSrv"

# Confirm Silver table exists before attempting merge.
# If Cell 1 created it on the first run, it will exist. Otherwise it must pre-exist.
if not spark.catalog.tableExists(silver_table):
    raise Exception(f"Silver table does not exist: {silver_table}")

# Open the Silver table as a Delta table object so we can run MERGE.
silver_delta = DeltaTable.forName(spark, silver_table)

src_cols = src_df.columns

# Build the update column set for full updates (when RowChkSum changed).
# We update ALL columns EXCEPT silver_created_at — that timestamp must never change
# once a record is first created.
update_cols = [
    c for c in src_cols
    if c != "silver_created_at"
]

update_set = {c: f"src.{c}" for c in update_cols}

# Override timestamps to use database server time rather than the Bronze extraction time.
update_set["silver_updated_at"] = "current_timestamp()"
update_set["last_seen_at"]      = "current_timestamp()"

# For new inserts, write all columns including silver_created_at.
insert_values = {c: f"src.{c}" for c in src_cols}

(
    silver_delta.alias("tgt")
    .merge(
        src_df.alias("src"),
        # Match on: same clinic + same SAMMS record ID.
        # This is the unique business key for a DartsSrv record.
        "tgt._site_code = src._site_code AND tgt.dsID = src.dsID"
    )

    # CASE 1: Matching record found AND RowChkSum is different (or NULL on either side).
    # The record's data has actually changed. Overwrite all data columns.
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: Matching record found AND RowChkSum is identical.
    # The record's data has NOT changed. Only update lightweight audit/tracking fields.
    # This avoids unnecessary Delta table write amplification.
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum = src.RowChkSum
        """,
        set={
            "last_seen_at":              "current_timestamp()",
            "_ingest_run_id":            "src._ingest_run_id",
            "_extracted_at":             "src._extracted_at",
            "_source_query_start_date":  "src._source_query_start_date",
            "_source_query_end_date":    "src._source_query_end_date"
        }
    )

    # CASE 3: No matching record found in Silver.
    # This is a brand-new record. Insert it with all columns.
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("Silver MERGE completed successfully.")
```

### Explanation of the three MERGE branches

#### Branch 1 — `whenMatchedUpdate` where RowChkSum differs (full update)

```
tgt.RowChkSum IS NULL OR src.RowChkSum IS NULL OR tgt.RowChkSum <> src.RowChkSum
```

**When this fires:** A record already exists in Silver for this `_site_code + dsID`, but the CHECKSUM has changed. This means at least one data column was modified in SAMMS since the last run (the session was updated, signed, billed, etc.).

**What it does:** Overwrites all data columns in the Silver row with the new values from Bronze. Does NOT touch `silver_created_at` (the original creation timestamp is preserved).

#### Branch 2 — `whenMatchedUpdate` where RowChkSum is identical (metadata-only update)

```
tgt.RowChkSum = src.RowChkSum
```

**When this fires:** A record already exists in Silver and the data has not changed at all. The record came through because its date fields fell within the lookback window, but nothing was actually modified.

**What it does:** Only updates five lightweight fields — `last_seen_at`, `_ingest_run_id`, `_extracted_at`, `_source_query_start_date`, `_source_query_end_date`. No data columns are touched. This avoids unnecessary Delta table rewrites for unchanged rows, which keeps the Silver table size manageable.

#### Branch 3 — `whenNotMatchedInsert` (new record)

**When this fires:** The `_site_code + dsID` combination does not exist anywhere in Silver. This is a brand-new counseling session record.

**What it does:** Inserts the complete row including all data columns and `silver_created_at`.

---

## 17. End-to-End Flow Summary

```
TRIGGER: Execute_DartSrv runs with:
  p_ingest_run_id  = "test-run-001"
  p_lookback_days  = 15
  p_sites          = [ { site_code: "AHK", source_database: "SAMMS-Ahoskie", ... }, ... ]

STEP 1 — Exected_AfterBronz invokes child dartSRV_Pipeline
  ↓ ForEach over p_sites (sequential)

STEP 2 — lkp_check_optional_columns_exist (per site)
  → Returns: service_type_exists, telehealth_exists, holdid_exists, upsize_ts_exists

STEP 3 — Copy data2 (per site)
  → Dynamic SELECT with optional columns + RowChkSum
  → APPEND to bhg_bronze.Dart.br_tblDartSrv

STEP 4 — Child pipeline completes (all sites in Bronze)

STEP 5 — nb_darts_bronze_to_silver (Cell 1)
  → Read Bronze WHERE _ingest_run_id = current run
  → Deduplicate, add dsDtStartYear, RowState, timestamps
  → Add upsize_ts column to Silver if missing

STEP 6 — nb_darts_bronze_to_silver (Cell 2)
  → Delta MERGE into bhg_silver.pats.sl_tblDartSrv
     RowChkSum changed  → full update
     RowChkSum same     → metadata-only update
     New record         → insert

STEP 7 — Truncate_GoldTable_DartSrv
  → TRUNCATE TABLE pats.gd_darts_srv

STEP 8 — copy_darts_silver_to_gold
  → Full reload Silver → Gold with column renames

DONE: Silver is merged current state; Gold is full reporting snapshot.
```

---

## 18. How Change Detection Works (RowChkSum)

The CHECKSUM concept is the most important part of this pipeline to understand. Without it, every run would rewrite millions of rows even when nothing changed.

### How it works

SQL Server's `CHECKSUM()` function takes a list of columns and computes a single integer from all their values combined. Think of it as a fingerprint for the row.

```sql
RowChkSum = CHECKSUM(dsID, dsClt, dsDIM1, dsDIM2, ..., MG)
-- Example result: 1482937461
```

If any column in that list changes, the CHECKSUM changes. If all columns are the same, the CHECKSUM is the same.

### During extraction (in the Copy activity)

Every row pulled from SAMMS includes its CHECKSUM computed at the source:
```sql
-- Row extracted from SAMMS:
dsID=12345, dsClt=678, dsDtStart='2026-05-01', ..., RowChkSum=1482937461
```

### During the Silver MERGE (in the notebook)

```
Row from Bronze:  dsID=12345, _site_code='AHK', RowChkSum=1482937461
Row in Silver:    dsID=12345, _site_code='AHK', RowChkSum=1482937461

→ Checksums are EQUAL → only touch metadata (last_seen_at etc.)
→ No full row rewrite. Record is considered UNCHANGED.
```

```
Row from Bronze:  dsID=12345, _site_code='AHK', RowChkSum=9876543210
Row in Silver:    dsID=12345, _site_code='AHK', RowChkSum=1482937461

→ Checksums DIFFER → full update of all data columns
→ Something changed in SAMMS since last run.
```

### What columns are included in RowChkSum (Fabric definition)

| Included | Excluded | Reason for exclusion |
|---|---|---|
| All numeric + date + varchar business columns | `dstxtNote`, `dsRTBNOTE` | `ntext` — CHECKSUM unreliable on large text |
| `dsSIGCLTUSER`, `dsDBnotes`, `MG` | `dsSignature`, `dssignatureCOSIGN`, `dsSIGCLT` | Signature text fields |
| `ServiceType`, `dsTelehealthSession`, `HoldId` (conditional) | `dsSigCltImg`, `dsSignatureCoSignImg`, `dsSignatureIMG` | `varbinary` image data |
| | `upsize_ts` | `timestamp`/`varbinary` — extracted but not fingerprinted |

**Optional columns in CHECKSUM:** When a clinic lacks `ServiceType`, `dsTelehealthSession`, or `HoldId`, the Copy query uses `CAST(NULL AS ...)` inside CHECKSUM so all clinics produce comparable fingerprints.

---

## 19. RowChkSum — Fabric vs Old C# ETL

### Side-by-side column list

| # | Column | In Old ETL CHECKSUM | In Fabric CHECKSUM | Type in SAMMS | Notes |
|---|---|---|---|---|---|
| 1 | `dsID` | Yes | Yes | int | PK — always changes fingerprint |
| 2 | `dsClt` | Yes | Yes | int | Patient/client ID |
| 3 | `dsDIM1` | Yes | Yes | bit | Clinical dimension flag |
| 4 | `dsDIM2` | Yes | Yes | bit | |
| 5 | `dsDIM3` | Yes | Yes | bit | |
| 6 | `dsDIM4` | Yes | Yes | bit | |
| 7 | `dsDIM5` | Yes | Yes | bit | |
| 8 | `dsDIM6` | Yes | Yes | bit | |
| 9 | `dsTxtSrv` | Yes | Yes | varchar(100) | Service type description |
| 10 | `dsDtStart` | Yes | Yes | datetime | Session start date |
| 11 | `dsDtEnd` | Yes | Yes | datetime | Session end date |
| 12 | `dsTxtType` | Yes | Yes | varchar(50) | Service type code |
| 13 | `dsdblUnits` | Yes | Yes | float | Units/hours |
| 14 | `dsNoteID` | Yes | Yes | int | Linked note ID |
| 15 | `dsDtAdded` | Yes | Yes | datetime | Record created date |
| 16 | `dstxtStaff` | Yes | Yes | varchar(100) | Delivering staff |
| 17 | `DSbilled` | Yes | Yes | datetime | Billing date |
| 18 | `dsGROUPNUM` | Yes | Yes | varchar(50) | Group session number |
| 19 | `dsPROGRAM` | Yes | Yes | varchar(50) | Program type |
| 20 | `dsUpdate` | Yes | Yes | datetime | Last update date |
| 21 | `dsUPDATEStaff` | Yes | Yes | varchar(50) | Last updating staff |
| 22 | `dsInvalidatedOn` | Yes | Yes | datetime | Void date |
| 23 | `dsError` | Yes | Yes | varchar(4000) | Error message |
| 24 | `dsTxtHIV` | Yes | Yes | varchar(50) | HIV program flag |
| 25 | `dsDartsGroup` | Yes | Yes | int | Group session key |
| 26 | `repOldSrv` | Yes | Yes | numeric(18,0) | Legacy service code |
| 27 | `dsSigDate` | Yes | Yes | datetime | Counselor sign date |
| 28 | `dssigdateCOSIGN` | Yes | Yes | datetime | Co-signer sign date |
| 29 | `dsSigUser` | Yes | Yes | varchar(50) | Signing counselor username |
| 30 | `dsSigUserCosign` | Yes | Yes | varchar(50) | Co-signing username |
| 31 | `dsSIGCLTDATE` | Yes | Yes | datetime | Client sign date |
| 32 | `dsSIGCLTUSER` | No | Yes | varchar(50) | Fabric includes; old C# SelectConstructor omitted from CHECKSUM |
| 33 | `dsAPTID` | Yes | Yes | int | Linked appointment ID |
| 34 | `dsuncharted` | Yes | Yes | bit | Uncharted session flag |
| 35–41 | `dsTxDim1`–`dsTxDim6`, `dsDIAG`, `dsArea`, `dsGroupDefaultNote`, `dsGroupEnd` | Yes | Yes | mixed | Treatment/group fields |
| 42 | `dsGroupIdentity` | Yes | Yes | int | Group identity key |
| 43 | `dsGroupStart` | Yes | Yes | datetime | Group session start |
| 44 | `dsDIAG10` | Yes | Yes | varchar(100) | ICD-10 diagnosis code |
| 45 | `SiteID` | Yes | Yes | int | SAMMS internal site numeric ID |
| 46 | `dsDBnotes` | Yes | Yes | varchar(250) | Admin/DB notes |
| 47 | `MG` | Yes | Yes | float | Milligrams (MAT programs) |
| 48 | `ServiceType` | No | Yes (conditional) | varchar(100) | Fabric includes via NULL cast when column absent |
| 49 | `dsTelehealthSession` | No | Yes (conditional) | bit | Same |
| 50 | `HoldId` | No | Yes (conditional) | int | Same |

**Fabric CHECKSUM: up to 52 columns** (49 base + 3 optional). Old C# ETL used 49 base columns without optional fields.

### Columns excluded from CHECKSUM (both)

| Column | Type | Why Excluded |
|---|---|---|
| `dstxtNote`, `dsRTBNOTE` | ntext | CHECKSUM unreliable on large text |
| `dsSignature`, `dssignatureCOSIGN`, `dsSIGCLT` | ntext | Same |
| `dsSigCltImg`, `dsSignatureCoSignImg`, `dsSignatureIMG` | varbinary | CHECKSUM does not work on binary |
| `upsize_ts` | varbinary(8) | Extracted to Silver/Gold but not fingerprinted |
| `_site_code`, `silver_updated_at` | metadata | ETL-managed — no change signal |

> **Note:** Fabric intentionally extends CHECKSUM with optional columns (using NULL when absent) so changes to `ServiceType`, telehealth, or hold fields trigger Silver updates. This differs from the old C# path where those columns were excluded entirely.

---

## 20. RowState in Fabric Silver and Gold

### Where RowState comes from — the stored procedure

The bulk merge stored procedure (`stg.DartsSrvMerge25` and all equivalent year SPs) sets `RowState` on every INSERT and UPDATE:

```sql
-- On UPDATE (line 47 of DartsSrvMerge25):
RowState = case when s.dsClt >= 0 then 1 else 0 end

-- On INSERT (line 70 of DartsSrvMerge25):
case when s.dsClt >= 0 then 1 else 0 end
```

### What the values mean

| Value | Condition | Meaning |
|---|---|---|
| `1` | `dsClt >= 0` | Active / valid patient session record |
| `0` | `dsClt < 0` | Placeholder or test record (negative client IDs used for system/test purposes) |

### How it is implemented in Fabric (Notebook Cell 1)

```python
.withColumn("RowState", when(col("dsClt") >= 0, 1).otherwise(0))
```

This is computed in the notebook from `dsClt`, which is already in the Bronze data. Since `RowState` becomes part of `src_df.columns`, Cell 2's merge picks it up automatically — no extra code needed in the merge.

### How "soft deleted" or voided sessions are tracked

`RowState` only flags active vs placeholder records. If a session is voided in SAMMS, the clinic staff sets `dsInvalidatedOn` to the void date. Since `dsInvalidatedOn` is in the CHECKSUM, that change is detected on the next run and Silver is updated automatically.

- Active session: `dsInvalidatedOn = NULL`, `RowState = 1`
- Voided session: `dsInvalidatedOn = '2026-05-07'`, `RowState = 1` (still 1 — voiding does not change dsClt)
- Placeholder record: `dsClt < 0`, `RowState = 0`

### Complete Fabric Silver audit columns

| Fabric Silver Column | Source | Purpose |
|---|---|---|
| `RowState` | Computed: `dsClt >= 0 → 1, else 0` | Active vs placeholder — copied to Gold as `RowState` |
| `upsize_ts` | SAMMS (optional) | Replication timestamp — copied to Gold unchanged |
| `silver_created_at` | `current_timestamp()` on first INSERT | When this record was first loaded into Silver |
| `silver_updated_at` | `current_timestamp()` on every update | When this record's data last changed |
| `last_seen_at` | `current_timestamp()` on every run | Last time this record appeared in any extraction |
| `dsInvalidatedOn` | SAMMS data column | Whether session was voided — change detected via RowChkSum |

---

## 21. Architectural Decisions — Lookback and Single Silver Table

### Decision 1: Fixed lookback window (`p_lookback_days`)

**Old ETL behavior:**
```
Normal run        → -15 days
Last Friday/month → -90 days
Special override  → -200 days
```

**Fabric behavior:** Fixed `p_lookback_days = 15` on child pipeline (Int parameter).

**Why this is not a gap:** Dynamic 15/90/200 day logic will come from **control tables** later. The orchestration layer will pass the correct value as `p_lookback_days`. For manual runs today, pass `90` or `200` when triggering the parent pipeline.

### Decision 2: Single Silver table + Gold warehouse reload

**Old ETL:** Year-partitioned Azure tables (`pats.tbl_DartsSrv_20XX`) via multiple merge SPs.

**Fabric behavior:**
- Silver: one Delta table `bhg_silver.pats.sl_tblDartSrv` with `dsDtStartYear` for filtering
- Gold: `bhg_gold.pats.gd_darts_srv` — truncated and fully reloaded from Silver each run

The `dsDtStartYear` column (Cell 1) supports logical year filtering. Add `.partitionBy("dsDtStartYear")` on first Silver write for performance at scale.

### Decision 3: Two-pipeline orchestration

**Child `dartSRV_Pipeline`:** SAMMS → Bronze only (ForEach + Lookup + Copy).

**Parent `Execute_DartSrv`:** Invokes child, runs Silver notebook, then Truncate + Copy to Gold.

---

## 22. Why Five Date Columns in the WHERE Clause

```sql
WHERE dsClt IS NOT NULL
  AND (
       CONVERT(date, dsdtstart) >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsDtAdded) >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsUpdate)  >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsBilled)  >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR CONVERT(date, dsSigDate) >= CONVERT(date, DATEADD(day, -15, GETDATE()))
    OR dsClt <= 0
  )
```

A counseling session has a life cycle. A session created weeks ago might be:
- Signed by the counselor today (`dsSigDate` just updated)
- Billed to insurance today (`DSbilled` just updated)
- Corrected/updated by staff today (`dsUpdate` just updated)

If you only checked `dsdtstart >= 15 days ago`, you would miss all of these updates. The session happened in the past, so its start date would not fall in the 15-day window, yet the record changed today.

| Date Column | What It Tracks | Example Scenario Captured |
|---|---|---|
| `dsdtstart` | Session start date | New sessions created in the last 15 days |
| `dsDtAdded` | When record was created in SAMMS | Backdated entries added recently |
| `dsUpdate` | Last time staff saved changes | Corrections, edits, amendments |
| `DSbilled` | When the session was billed | Late billing updates |
| `dsSigDate` | When counselor signed the note | Sessions signed/countersigned recently |
| `dsClt <= 0` | Placeholder records | Negative client IDs used for test/system records |

By checking all five, the pipeline captures any record that was touched for any reason within the lookback window.

---

## 23. Troubleshooting Guide

### Pipeline fails at `lkp_check_optional_columns_exist`

**Symptom:** Lookup activity fails with connection error or timeout.  
**Cause:** The SAMMS SQL Server for this clinic is unreachable, the on-premise gateway is down, or the connection GUID is wrong.  
**Fix:** Verify the connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` is active in Fabric Connections. Test the gateway. Confirm the `source_database` value in `p_sites` exactly matches the actual SQL Server database name.

---

### Copy activity copies 0 rows

**Symptom:** Copy succeeds but writes 0 rows to Bronze.  
**Cause:** The WHERE clause returned no rows — either no records were touched in the last `p_lookback_days` days, or the date columns are all NULL for that clinic.  
**Fix:** Increase `p_lookback_days` to 30 or 90 and re-run. Check that `tblDartsSrv` actually has rows in that database.

---

### Notebook Cell 1 raises "No Bronze rows found"

**Symptom:** `Exception: No Bronze rows found for ingest_run_id = test-run-001`  
**Cause 1:** You are running the notebook manually with the default `p_ingest_run_id = "test-run-001"`, but Bronze was written with a different run ID.  
**Fix 1:** Change the fallback value in the `except NameError` block to match an actual `_ingest_run_id` value present in Bronze.  
**Cause 2:** The Copy activity did not write any rows (see above).  
**Fix 2:** Re-run the pipeline with a larger `p_lookback_days`.

---

### Notebook Cell 2 raises "Silver table does not exist"

**Symptom:** Cell 2 raises the exception immediately.  
**Cause:** Cell 1 was supposed to create the Silver table on the first run, but it was never reached, or the Silver table creation failed.  
**Fix:** Run Cell 1 first. If Cell 1 created the table, Cell 2 will find it. If Cell 1 failed, check that `bhg_silver` is attached to the notebook's Lakehouse panel and the `pats` schema exists.

---

### Silver MERGE is very slow

**Symptom:** Cell 2 runs for a long time.  
**Cause:** The Silver table is large and the MERGE must scan the entire table to find matching records.  
**Fix:** Add Delta partitioning by `dsDtStartYear` on the Silver table. Also make sure the merge condition includes `dsDtStartYear` so Delta can prune partitions:

```python
# In Cell 1, write with partitioning on first creation:
.write
.format("delta")
.partitionBy("dsDtStartYear")
.mode("overwrite")
.saveAsTable(silver_table)

# In Cell 2, update the merge condition to include year:
"tgt._site_code = src._site_code AND tgt.dsID = src.dsID AND tgt.dsDtStartYear = src.dsDtStartYear"
```

---

### ServiceType values are all NULL even for clinics that have the column

**Symptom:** `ServiceType` column is always `NULL` in Bronze even for newer clinics.  
**Cause:** The `source_schema` in the `p_sites` array is wrong, or the table name is different in that clinic.  
**Fix:** Confirm that `source_schema = "dbo"` and `source_table = "tblDartsSrv"` are correct for each clinic object in `p_sites`. Run the Lookup query manually against that database to verify.

---

### Gold copy fails after truncate

**Symptom:** `copy_darts_silver_to_gold` fails; Gold table is empty.  
**Cause:** Column mapping mismatch between Silver and Gold, or Silver table name case (`sl_tbldartsrv` vs `sl_tblDartSrv`).  
**Fix:** Verify mappings in `dartdefintion.txt`. Re-run parent pipeline from Silver notebook step if Gold was truncated but copy failed.

---

### upsize_ts missing in Silver on older deployments

**Symptom:** Copy to Gold fails on `upsize_ts` column.  
**Cause:** Silver table created before `upsize_ts` was added to extraction.  
**Fix:** Cell 1 runs `ALTER TABLE ... ADD COLUMNS (upsize_ts BINARY)` automatically. Re-run notebook Cell 1 if needed.

---

*End of Implementation Guide*
