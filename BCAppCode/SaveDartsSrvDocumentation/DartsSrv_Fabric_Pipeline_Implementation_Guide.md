# DartsSrv — Microsoft Fabric ETL Pipeline Implementation Guide

**Pipeline Name:** DartsSrv Bronze → Silver ETL  
**Data:** Counseling session records (`tblDartsSrv`) from SAMMS SQL Server clinic databases  
**Destination:** Microsoft Fabric Lakehouse (Bronze + Silver layers)  
**Author Reference:** `FabricPipelineImplementation.txt`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [Prerequisites — What You Need Before Starting](#3-prerequisites--what-you-need-before-starting)
4. [Pipeline Parameters — Define These First](#4-pipeline-parameters--define-these-first)
5. [Step 1 — Create the Data Pipeline in Fabric](#5-step-1--create-the-data-pipeline-in-fabric)
6. [Step 2 — Add the ForEach Activity](#6-step-2--add-the-foreach-activity)
7. [Step 3 — Inside ForEach: Add the Lookup Activity (ServiceType Check)](#7-step-3--inside-foreach-add-the-lookup-activity-servicetype-check)
8. [Step 4 — Inside ForEach: Add the Copy Activity (Bronze Extraction)](#8-step-4--inside-foreach-add-the-copy-activity-bronze-extraction)
9. [Step 5 — Add the Notebook Activity (Bronze → Silver)](#9-step-5--add-the-notebook-activity-bronze--silver)
10. [Step 6 — Create the Notebook: nb_darts_bronze_to_silver](#10-step-6--create-the-notebook-nb_darts_bronze_to_silver)
11. [Step 7 — Notebook Cell 1: Load Bronze and Prepare Silver Source](#11-step-7--notebook-cell-1-load-bronze-and-prepare-silver-source)
12. [Step 8 — Notebook Cell 2: Merge Into Silver Delta Table](#12-step-8--notebook-cell-2-merge-into-silver-delta-table)
13. [End-to-End Flow Summary](#13-end-to-end-flow-summary)
14. [How Change Detection Works (RowChkSum)](#14-how-change-detection-works-rowchksum)
15. [RowChkSum Column Alignment — Old ETL vs Fabric (Verified)](#15-rowchksum-column-alignment--old-etl-vs-fabric-verified)
16. [RowState — Does It Exist for DartsSrv?](#16-rowstate--does-it-exist-for-dartssrv)
17. [Architectural Decisions — Lookback and Single Silver Table](#17-architectural-decisions--lookback-and-single-silver-table)
18. [Why Five Date Columns in the WHERE Clause](#18-why-five-date-columns-in-the-where-clause)
19. [Troubleshooting Guide](#19-troubleshooting-guide)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Microsoft Fabric Data Pipeline                    │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  ForEach Site (fe_each_samms_database_copy1)                │   │
│  │  Iterates over every clinic in p_sites parameter            │   │
│  │                                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 1              │                               │   │
│  │  │ lkp_check_servicetype   │  ← Query SAMMS sys.columns   │   │
│  │  │ _column_exists (Lookup) │    Does ServiceType exist?   │   │
│  │  └────────────┬────────────┘                               │   │
│  │               │ Succeeded                                   │   │
│  │               ▼                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 2              │                               │   │
│  │  │ Copy data2 (Copy)       │  ← SELECT all DartsSrv cols  │   │
│  │  │                         │    WHERE date lookback        │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │                               │   │
│  │  │          Dart.br_tbl    │                               │   │
│  │  │          DartSrv        │                               │   │
│  │  └─────────────────────────┘                               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                         │ ForEach Succeeded                        │
│                         ▼                                          │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  nb_darts_bronze_to_silver (Notebook)                       │   │
│  │                                                             │   │
│  │  Cell 1: Read Bronze → Deduplicate → Prepare src_df        │   │
│  │  Cell 2: Delta MERGE src_df → bhg_silver.pats.sl_tblDart   │   │
│  └─────────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────────┘
```

**Lakehouse Targets:**

| Layer | Lakehouse Name | Schema | Table |
|---|---|---|---|
| Bronze | `bhg_bronze` | `Dart` | `br_tblDartSrv` |
| Silver | `bhg_silver` | `pats` | `sl_tblDartSrv` |

---

## 2. What This Pipeline Does — Plain English

The SAMMS system is an on-premise SQL Server database installed at each BHG clinic. Every counseling session a patient has is recorded in a table called `tblDartsSrv` inside that clinic's SAMMS database. There are 80+ clinics, each with their own SAMMS database.

This pipeline:
1. **Visits each clinic's SAMMS database** one at a time (ForEach loop)
2. **Checks** whether that specific clinic's SAMMS version has a column called `ServiceType` — not all clinic versions do
3. **Extracts** all DartsSrv records that were created, updated, signed, or billed within the last N days (default 15)
4. **Appends** the extracted rows to a Bronze Lakehouse table, tagging them with a run ID and extraction timestamp
5. After all clinics are processed, runs a **notebook** that reads those Bronze rows and performs a smart MERGE into the Silver table — only updating a row if its content actually changed (using a CHECKSUM comparison)

---

## 3. Prerequisites — What You Need Before Starting

Before you build this pipeline, the following must already exist in your Fabric workspace:

| Item | What It Is | Where It Lives |
|---|---|---|
| SAMMS SQL Server connection | A Fabric connection to the on-premise SAMMS SQL Server gateway | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse | Bronze layer Lakehouse | Fabric workspace |
| `bhg_silver` Lakehouse | Silver layer Lakehouse | Fabric workspace |
| `Dart` schema in `bhg_bronze` | Schema for DartsSrv Bronze tables | `bhg_bronze` Lakehouse |
| `pats` schema in `bhg_silver` | Schema for Silver patient tables | `bhg_silver` Lakehouse |
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
    "site_code": "ColoradoSpringsV5",
    "source_database": "SAMMS-ColoradoSpringsV5",
    "source_schema": "dbo",
    "source_table": "tblDartsSrv"
  }
]
```

**Why:** This array tells the ForEach loop which clinics to process. Each object in the array represents one SAMMS clinic database. In production you add all 80+ clinics to this array. Each object has four properties:

| Property | Meaning | Example |
|---|---|---|
| `site_code` | Human-readable clinic code used as a data tag | `ColoradoSpringsV5` |
| `source_database` | The exact database name on the SAMMS SQL Server | `SAMMS-ColoradoSpringsV5` |
| `source_schema` | Schema where `tblDartsSrv` lives (always `dbo`) | `dbo` |
| `source_table` | Source table name | `tblDartsSrv` |

> **To add more clinics:** Add more objects to this JSON array. Each clinic gets its own object with the four properties above.

---

## 5. Step 1 — Create the Data Pipeline in Fabric

1. Open your Fabric workspace
2. Click **+ New** → **Data pipeline**
3. Name it: `pl_darts_samms_to_lakehouse` (or your preferred naming convention)
4. Click **Create**
5. On the pipeline canvas, click an empty area and go to the **Parameters** tab on the right panel
6. Add all three parameters from Section 4 above

---

## 6. Step 2 — Add the ForEach Activity

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

## 7. Step 3 — Inside ForEach: Add the Lookup Activity (Optional Columns Check)

This is the first activity that runs inside the ForEach loop for each clinic.

### Why this activity exists

Three columns in `tblDartsSrv` do not exist in all SAMMS versions — they were added in newer releases. If you try to SELECT a column that does not exist in a clinic's database, the Copy activity fails immediately with a "column not found" error. This single Lookup checks for all three optional columns in one round-trip and returns a flag (1 or 0) for each. The Copy activity then uses those flags to decide whether to select the real column or substitute a NULL placeholder.

| Optional Column | Type | Why optional |
|---|---|---|
| `ServiceType` | varchar(100) | Added in a later SAMMS version |
| `dsTelehealthSession` | bit | Added for telehealth tracking — newer SAMMS only |
| `HoldId` | int | Added for hold management — newer SAMMS only |

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
    COUNT(CASE WHEN c.name = ''HoldId''              THEN 1 END) AS holdid_exists
FROM [', item().source_database, '].sys.columns c
INNER JOIN [', item().source_database, '].sys.tables t
    ON c.object_id = t.object_id
INNER JOIN [', item().source_database, '].sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = ''', item().source_schema, '''
  AND t.name = ''', item().source_table, '''
  AND c.name IN (''ServiceType'', ''dsTelehealthSession'', ''HoldId'');'
)
```

**How to read this expression:**

One query, one round-trip to SAMMS. The `COUNT(CASE WHEN ...)` pattern counts how many times each column name appears in `sys.columns`. Since a column either exists once or not at all, the result is always `1` (exists) or `0` (does not exist).

The SQL it builds looks like this for `ColoradoSpringsV5`:
```sql
SELECT
    COUNT(CASE WHEN c.name = 'ServiceType'         THEN 1 END) AS service_type_exists,
    COUNT(CASE WHEN c.name = 'dsTelehealthSession' THEN 1 END) AS telehealth_exists,
    COUNT(CASE WHEN c.name = 'HoldId'              THEN 1 END) AS holdid_exists
FROM [SAMMS-ColoradoSpringsV5].sys.columns c
INNER JOIN [SAMMS-ColoradoSpringsV5].sys.tables t ON c.object_id = t.object_id
INNER JOIN [SAMMS-ColoradoSpringsV5].sys.schemas s ON t.schema_id = s.schema_id
WHERE s.name = 'dbo'
  AND t.name = 'tblDartsSrv'
  AND c.name IN ('ServiceType', 'dsTelehealthSession', 'HoldId');
```

**What it returns:** A single row with three columns:

| Output column | Value if exists | Value if absent |
|---|---|---|
| `service_type_exists` | `1` | `0` |
| `telehealth_exists` | `1` | `0` |
| `holdid_exists` | `1` | `0` |

**How to access the results later:** In the Copy activity you reference them as:
```
activity('lkp_check_optional_columns_exist').output.firstRow.service_type_exists
activity('lkp_check_optional_columns_exist').output.firstRow.telehealth_exists
activity('lkp_check_optional_columns_exist').output.firstRow.holdid_exists
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
                "value": "@concat(\n'SELECT\n    COUNT(CASE WHEN c.name = ''ServiceType''         THEN 1 END) AS service_type_exists,\n    COUNT(CASE WHEN c.name = ''dsTelehealthSession'' THEN 1 END) AS telehealth_exists,\n    COUNT(CASE WHEN c.name = ''HoldId''              THEN 1 END) AS holdid_exists\nFROM [', item().source_database, '].sys.columns c\nINNER JOIN [', item().source_database, '].sys.tables t\n    ON c.object_id = t.object_id\nINNER JOIN [', item().source_database, '].sys.schemas s\n    ON t.schema_id = s.schema_id\nWHERE s.name = ''', item().source_schema, '''\n  AND t.name = ''', item().source_table, '''\n  AND c.name IN (''ServiceType'', ''dsTelehealthSession'', ''HoldId'');'\n)",
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

## 8. Step 4 — Inside ForEach: Add the Copy Activity (Bronze Extraction)

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
        MG
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
'ColoradoSpringsV5'           AS _site_code,           -- which clinic this row came from
'SAMMS-ColoradoSpringsV5'     AS _source_database,      -- which SAMMS database
'DARTS-2026-05-07-001'        AS _ingest_run_id,         -- which pipeline run
GETDATE()                     AS _extracted_at,          -- exact extraction timestamp
CONVERT(date, DATEADD(day,-15,GETDATE())) AS _source_query_start_date,  -- lookback start
CONVERT(date, GETDATE())      AS _source_query_end_date  -- lookback end (today)
```

These columns are added by the pipeline (not from SAMMS). They are metadata that lets you trace every row back to exactly when and where it was extracted.

#### Data columns (next 55 columns)

All clinical data columns from `tblDartsSrv` — see [Section 14](#14-how-change-detection-works-rowchksum) for the full column reference.

#### Optional columns — three conditional columns

After the `MG` column, the query conditionally adds three columns based on the Lookup result:

```
-- ServiceType
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.service_type_exists, 1),
    'ServiceType',
    'CAST(NULL AS varchar(100)) AS ServiceType'
),

-- dsTelehealthSession
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.telehealth_exists, 1),
    'dsTelehealthSession',
    'CAST(NULL AS bit) AS dsTelehealthSession'
),

-- HoldId
if(
    equals(activity('lkp_check_optional_columns_exist').output.firstRow.holdid_exists, 1),
    'HoldId',
    'CAST(NULL AS int) AS HoldId'
)
```

**What this does for each column:** If the Lookup returned `1` (column exists in this clinic's SAMMS), it selects the real column. If it returned `0` (column does not exist), it inserts a typed NULL placeholder with the same column name. Either way, every clinic's Bronze rows have the same schema — consistent columns across all 80+ clinics regardless of SAMMS version.

**Why these three are NOT in the CHECKSUM:** Same reason as `ServiceType` — they don't exist in all clinic versions. Including an optional column in the CHECKSUM would produce different hash values between clinics that have it and clinics that don't, making cross-clinic change detection unreliable.

#### RowChkSum — change detection fingerprint

```sql
RowChkSum = CHECKSUM(dsID, dsClt, dsDIM1, ..., MG)
```

SQL Server's `CHECKSUM()` function produces a single integer that acts as a fingerprint for the entire row. If even one column changes, the CHECKSUM changes. In the Silver notebook, we compare the CHECKSUM from this extraction against what is already in Silver. If they match, the row has not changed and we skip the update. See [Section 14](#14-how-change-detection-works-rowchksum) for more detail.

> **Important:** `ServiceType`, `dsTelehealthSession`, and `HoldId` are intentionally NOT included in the CHECKSUM. Binary image columns (`dsSigCltImg`, `dsSignatureCoSignImg`, `dsSignatureIMG`) and text columns (`dstxtNote`, `dsRTBNOTE`, `dsSignature`, `dssignatureCOSIGN`, `dsSigClt`) are also excluded because `CHECKSUM()` does not work reliably on those data types.

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

See [Section 15](#15-why-five-date-columns-in-the-where-clause) for a detailed explanation of why five date columns are used.

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

## 9. Step 5 — Add the Notebook Activity (Bronze → Silver)

After the ForEach finishes (all clinics extracted to Bronze), the pipeline runs a Spark notebook to process those Bronze rows and merge them into Silver.

### Add the activity
1. Go back to the **main pipeline canvas** (outside the ForEach)
2. Drag a **Notebook** activity onto the canvas
3. Rename it: `nb_darts_bronze_to_silver`
4. Draw a dependency arrow: from `fe_each_samms_database_copy1` → `nb_darts_bronze_to_silver` with condition **Succeeded**

This means: only run the notebook if ALL sites in the ForEach loop completed without error.

### Configure Settings tab

| Setting | Value |
|---|---|
| Notebook | Select the notebook `nb_darts_bronze_to_silver` (create it in Step 6 first) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8` |

### Configure Parameters (inside the Notebook activity)

Add one parameter to pass to the notebook:

| Parameter Name | Type | Value |
|---|---|---|
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |

**Why:** The notebook needs to know which pipeline run to process. The `_ingest_run_id` value is passed from the pipeline into the notebook as a parameter, so the notebook only reads the Bronze rows from this specific run.

### Configure Policy tab

| Setting | Value |
|---|---|
| Timeout | `0.12:00:00` (12 hours) |
| Retry | `0` |
| Retry interval | `30` seconds |

### The JSON for this activity
```json
{
    "name": "nb_darts_bronze_to_silver",
    "type": "TridentNotebook",
    "dependsOn": [
        {
            "activity": "fe_each_samms_database_copy1",
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
        "notebookId": "89769158-4ba9-4e86-9b6d-ff22852dddd5",
        "workspaceId": "c5097ffb-b78e-441d-9575-a82bac23cac8",
        "parameters": {
            "p_ingest_run_id": {
                "value": {
                    "value": "@pipeline().parameters.p_ingest_run_id",
                    "type": "Expression"
                },
                "type": "string"
            }
        }
    }
}
```

> **Note:** Replace `notebookId` with the actual GUID of your notebook after you create it in Step 6.

---

## 10. Step 6 — Create the Notebook: nb_darts_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_darts_bronze_to_silver`
3. Make sure it is attached to a Spark environment that has access to both `bhg_bronze` and `bhg_silver` Lakehouses
4. The notebook has **two cells** — Cell 1 and Cell 2

> **Important:** Add both Lakehouses to the notebook's "Lakehouse" panel on the left side before running. This is how the notebook can reference them by name (`bhg_bronze.Dart.br_tblDartSrv`, `bhg_silver.pats.sl_tblDartSrv`).

---

## 11. Step 7 — Notebook Cell 1: Load Bronze and Prepare Silver Source

### What Cell 1 does
1. Receives the `p_ingest_run_id` parameter from the pipeline
2. Reads the Bronze table filtered to only this run's rows
3. Deduplicates: if the same `_site_code + dsID` appears more than once in Bronze for this run, keep only the most recently extracted one
4. Adds computed columns: `dsDtStartYear`, `silver_updated_at`, `last_seen_at`, `is_current`
5. Checks if the Silver table exists — if not, creates it from scratch with the current data
6. Stores the prepared DataFrame as `src_df` for Cell 2 to use

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

---

## 12. Step 8 — Notebook Cell 2: Merge Into Silver Delta Table

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

## 13. End-to-End Flow Summary

Here is the complete flow in order, from pipeline trigger to Silver table update:

```
TRIGGER: Pipeline runs with parameters:
  p_ingest_run_id  = "DARTS-2026-05-07-001"
  p_lookback_days  = 15
  p_sites          = [ {ColoradoSpringsV5}, {B01}, {B02}, ... ]

STEP 1 — ForEach begins iterating over p_sites
  ↓ Current item: { site_code: "ColoradoSpringsV5", source_database: "SAMMS-ColoradoSpringsV5", ... }

STEP 2 — lkp_check_optional_columns_exist (Lookup)
  → Runs one query against SAMMS-ColoradoSpringsV5 sys.columns
  → Returns: { service_type_exists: 1, telehealth_exists: 0, holdid_exists: 0 }   (1 = exists, 0 = absent)

STEP 3 — Copy data2 (Copy Activity)
  → Builds dynamic SELECT query using item() values + Lookup result
  → Runs SELECT with lookback WHERE clause against SAMMS-ColoradoSpringsV5.dbo.tblDartsSrv
  → Gets N rows of DartsSrv data touched in last 15 days
  → APPENDs those rows (tagged with _ingest_run_id="DARTS-2026-05-07-001") to:
     bhg_bronze.Dart.br_tblDartSrv

STEP 4 — ForEach moves to next site (B01) and repeats Steps 2-3
  ... continues for all clinics in p_sites ...

STEP 5 — ForEach completes (all clinics done)

STEP 6 — nb_darts_bronze_to_silver Notebook (Cell 1)
  → Reads bhg_bronze.Dart.br_tblDartSrv WHERE _ingest_run_id = "DARTS-2026-05-07-001"
  → Gets all rows from ALL clinics for this run
  → Deduplicates on _site_code + dsID (keep latest)
  → Adds: dsDtStartYear, silver_updated_at, last_seen_at, is_current
  → If Silver table does not exist: creates it (first run only)

STEP 7 — nb_darts_bronze_to_silver Notebook (Cell 2)
  → Runs Delta MERGE into bhg_silver.pats.sl_tblDartSrv
     RowChkSum changed   → FULL UPDATE of all data columns
     RowChkSum same      → lightweight update (last_seen_at, run metadata only)
     New record          → INSERT all columns

DONE: Silver table is current with all changes from last 15 days across all clinics.
```

---

## 14. How Change Detection Works (RowChkSum)

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
Row from Bronze:  dsID=12345, _site_code='ColoradoSpringsV5', RowChkSum=1482937461
Row in Silver:    dsID=12345, _site_code='ColoradoSpringsV5', RowChkSum=1482937461

→ Checksums are EQUAL → only touch metadata (last_seen_at etc.)
→ No full row rewrite. Record is considered UNCHANGED.
```

```
Row from Bronze:  dsID=12345, _site_code='ColoradoSpringsV5', RowChkSum=9876543210
Row in Silver:    dsID=12345, _site_code='ColoradoSpringsV5', RowChkSum=1482937461

→ Checksums DIFFER → full update of all data columns
→ Something changed in SAMMS since last run.
```

### What columns are included in RowChkSum

| Included | Excluded | Reason for exclusion |
|---|---|---|
| All numeric + date + varchar columns | `dstxtNote`, `dsRTBNOTE` | `ntext` type — CHECKSUM unreliable on large text |
| `dsID`, `dsClt`, all `dsDIM*`, `dsTxtSrv` | `dsSignature`, `dssignatureCOSIGN`, `dsSigClt` | `ntext` / signature text fields |
| `dsDtStart`, `dsDtEnd`, `dsDtAdded`, `dsUpdate` | `dsSigCltImg`, `dsSignatureCoSignImg`, `dsSignatureIMG` | `varbinary` image data |
| `DSbilled`, `dsGROUPNUM`, `dsPROGRAM`, etc. | `ServiceType` | Optional column — not in all clinics |
| All `dsTxDim*`, `dsDIAG`, `dsDIAG10`, `SiteID`, `MG` | | |

---

## 15. RowChkSum Column Alignment — Old ETL vs Fabric (Verified)

This section provides a definitive side-by-side comparison of which columns go into the `CHECKSUM()` in the old C# ETL versus your Fabric implementation. **They are identical.**

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
| 32 | `dsAPTID` | Yes | Yes | int | Linked appointment ID |
| 33 | `dsuncharted` | Yes | Yes | bit | Uncharted session flag |
| 34 | `dsTxDim1` | Yes | Yes | int | Treatment dimension score 1 |
| 35 | `dsTxDim2` | Yes | Yes | int | Treatment dimension score 2 |
| 36 | `dsTxDim3` | Yes | Yes | int | Treatment dimension score 3 |
| 37 | `dsTxDim4` | Yes | Yes | int | Treatment dimension score 4 |
| 38 | `dsTxDim5` | Yes | Yes | int | Treatment dimension score 5 |
| 39 | `dsTxDim6` | Yes | Yes | int | Treatment dimension score 6 |
| 40 | `dsDIAG` | Yes | Yes | varchar(100) | ICD-9 diagnosis code |
| 41 | `dsArea` | Yes | Yes | varchar(100) | Treatment area |
| 42 | `dsGroupDefaultNote` | Yes | Yes | bit | Group default note flag |
| 43 | `dsGroupEnd` | Yes | Yes | datetime | Group session end |
| 44 | `dsGroupIdentity` | Yes | Yes | int | Group identity key |
| 45 | `dsGroupStart` | Yes | Yes | datetime | Group session start |
| 46 | `dsDIAG10` | Yes | Yes | varchar(100) | ICD-10 diagnosis code |
| 47 | `SiteID` | Yes | Yes | int | SAMMS internal site numeric ID |
| 48 | `dsDBnotes` | Yes | Yes | varchar(250) | Admin/DB notes |
| 49 | `MG` | Yes | Yes | float | Milligrams (MAT programs) |

**Total: 49 columns — same in both old ETL and Fabric.** ✓

### Columns intentionally excluded from CHECKSUM (same exclusions in both)

| Column | Type | Why Excluded |
|---|---|---|
| `dstxtNote` | ntext | SQL Server `CHECKSUM()` is unreliable on `ntext` — produces inconsistent results |
| `dsRTBNOTE` | ntext | Same reason |
| `dsSignature` | ntext | Same reason |
| `dssignatureCOSIGN` | ntext | Same reason |
| `dsSIGCLT` | ntext | Same reason |
| `dsSigCltImg` | varbinary(max) | `CHECKSUM()` does not work on binary data |
| `dsSignatureCoSignImg` | varbinary(max) | Same reason |
| `dsSignatureIMG` | varbinary(max) | Same reason |
| `ServiceType` | varchar (optional) | Optional column — does not exist in all SAMMS versions |
| `dsTelehealthSession` | bit (optional) | Optional column — does not exist in all SAMMS versions |
| `HoldId` | int (optional) | Optional column — does not exist in all SAMMS versions |
| `SiteCode` / `_site_code` | varchar | ETL-injected metadata — same value for every row from the same site, adds no change signal |
| `LastModAt` / `silver_updated_at` | datetime | ETL-managed timestamp — changes on every write even when data is unchanged |

> **Conclusion: RowChkSum is fully aligned. The same 49 columns are hashed in both the old ETL and the Fabric pipeline. A row with a matching RowChkSum in Fabric Silver will have the same hash as the same row had in Azure SQL BHG_DR.**

---

## 16. RowState — Implementation in Fabric Silver

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
| `RowState` | Computed: `dsClt >= 0 → 1, else 0` | Active vs placeholder flag — matches SP logic |
| `silver_created_at` | `current_timestamp()` on first INSERT | When this record was first loaded into Silver |
| `silver_updated_at` | `current_timestamp()` on every update | When this record's data last changed |
| `last_seen_at` | `current_timestamp()` on every run | Last time this record appeared in any extraction |
| `dsInvalidatedOn` | SAMMS data column | Whether session was voided — change detected via RowChkSum |

---

## 17. Architectural Decisions — Lookback and Single Silver Table

### Decision 1: Fixed lookback window (`p_lookback_days`)

**Old ETL behavior:**
```
Normal run        → -15 days
Last Friday/month → -90 days
Special override  → -200 days
```

**Fabric behavior:** Fixed `p_lookback_days = 15` parameter.

**Why this is not a gap:** The dynamic 15/90/200 day logic will be driven by **control tables** in the future Fabric architecture. When the pipeline is triggered, the orchestration layer will read the control table to determine the correct lookback value for that day and pass it as `p_lookback_days`. The pipeline itself is intentionally kept simple — it receives whatever value it is given and uses it. No change to the pipeline is needed when control tables are implemented.

**For manual runs today:** If you need the 90-day window (e.g., running at month end), simply pass `p_lookback_days = 90` when you trigger the pipeline manually.

---

### Decision 2: Single Silver table instead of year-partitioned tables

**Old ETL behavior:** 11 separate Azure SQL tables, one per year:
```
pats.tbl_DartsSrv_2014B4   (2008–2014)
pats.tbl_DartsSrv_2015
pats.tbl_DartsSrv_2016
...
pats.tbl_DartsSrv_2024
```

**Fabric behavior:** Single unified Silver table: `bhg_silver.pats.sl_tblDartSrv`

**Why this is the correct design for Fabric:** This is a deliberate client decision. Delta Lake on Fabric handles large table scale differently than Azure SQL. Instead of splitting physically by year, the single Silver table uses the `dsDtStartYear` column (computed in Cell 1) for logical partitioning. Delta's predicate pushdown means a query filtering `WHERE dsDtStartYear = 2023` only reads 2023 data — the same performance benefit as a separate table, without the complexity of 11 separate merge operations.

**The `dsDtStartYear` column already computed in Cell 1 is ready for this.** When you want to partition the Silver Delta table by year, add `.partitionBy("dsDtStartYear")` to the initial write in Cell 1 and include `dsDtStartYear` in the Silver merge condition in Cell 2 for maximum query efficiency.

---

## 18. Why Five Date Columns in the WHERE Clause

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

## 19. Troubleshooting Guide

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

*End of Implementation Guide*
