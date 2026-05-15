# Dose & Dose Excuse — Microsoft Fabric ETL Pipeline Implementation Guide

**Pipeline Name:** Dose Bronze → Silver ETL  
**Data:** Dose administration records (`tblDose`) and dose excuse records (`tblDoseExcuse`) from SAMMS SQL Server clinic databases  
**Destination:** Microsoft Fabric Lakehouse (Bronze + Silver layers)  
**Covers:** Both `pats.tbl_Dose` and `pats.tbl_Dose_Excuse` in a single pipeline  
**Author Reference:** `SaveDoses_ETL_Complete_Documentation.md`, `BHGTaskRunner/Program.cs`

---

## Table of Contents

1. [Architecture Overview](#1-architecture-overview)
2. [What This Pipeline Does — Plain English](#2-what-this-pipeline-does--plain-english)
3. [Prerequisites — What You Need Before Starting](#3-prerequisites--what-you-need-before-starting)
4. [Pipeline Parameters — Define These First](#4-pipeline-parameters--define-these-first)
5. [Step 1 — Create the Data Pipeline in Fabric](#5-step-1--create-the-data-pipeline-in-fabric)
6. [Step 2 — Add the ForEach Activity](#6-step-2--add-the-foreach-activity)
7. [Step 3 — Inside ForEach: Add the Lookup Activity (InventoryGroup Check)](#7-step-3--inside-foreach-add-the-lookup-activity-inventorygroup-check)
8. [Step 4 — Inside ForEach: Add Copy Activity 1 (Dose Bronze Extraction)](#8-step-4--inside-foreach-add-copy-activity-1-dose-bronze-extraction)
9. [Step 5 — Inside ForEach: Add Copy Activity 2 (Dose Excuse Bronze Extraction)](#9-step-5--inside-foreach-add-copy-activity-2-dose-excuse-bronze-extraction)
10. [Step 6 — Add Notebook Activity 1 (Dose Bronze → Silver)](#10-step-6--add-notebook-activity-1-dose-bronze--silver)
11. [Step 7 — Add Notebook Activity 2 (Dose Excuse Bronze → Silver)](#11-step-7--add-notebook-activity-2-dose-excuse-bronze--silver)
12. [Step 8 — Create Notebook: nb_dose_bronze_to_silver](#12-step-8--create-notebook-nb_dose_bronze_to_silver)
13. [Step 9 — Create Notebook: nb_dose_excuse_bronze_to_silver](#13-step-9--create-notebook-nb_dose_excuse_bronze_to_silver)
14. [End-to-End Flow Summary](#14-end-to-end-flow-summary)
15. [How Change Detection Works (RowChkSum)](#15-how-change-detection-works-rowchksum)
16. [RowState Logic — Active vs Soft-Deleted Records](#16-rowstate-logic--active-vs-soft-deleted-records)
17. [Why the WHERE Clause Has Two Date Columns and a Site-Group Split](#17-why-the-where-clause-has-two-date-columns-and-a-site-group-split)
18. [Known Anomalies and Cautions](#18-known-anomalies-and-cautions)
19. [Troubleshooting Guide](#19-troubleshooting-guide)

---

## 1. Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                    Microsoft Fabric Data Pipeline                    │
│                    pl_dose_samms_to_lakehouse                        │
│                                                                     │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │  ForEach Site (fe_each_samms_site)                          │   │
│  │  Iterates over every clinic in p_sites parameter            │   │
│  │                                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 1              │                               │   │
│  │  │ lkp_check_inventory     │  ← Query SAMMS sys.columns   │   │
│  │  │ _group_exists (Lookup)  │    Does InventoryGroup exist? │   │
│  │  └────────────┬────────────┘                               │   │
│  │               │ Succeeded                                   │   │
│  │               ▼                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 2              │                               │   │
│  │  │ cp_dose_to_bronze (Copy)│  ← SELECT all tblDose cols   │   │
│  │  │                         │    WHERE date lookback        │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │    Dose.br_tblDose            │   │
│  │  └────────────┬────────────┘                               │   │
│  │               │ Succeeded                                   │   │
│  │               ▼                                             │   │
│  │  ┌─────────────────────────┐                               │   │
│  │  │ Activity 3              │                               │   │
│  │  │ cp_excuse_to_bronze     │  ← SELECT all tblDoseExcuse  │   │
│  │  │ (Copy)                  │    WHERE 1=1 (all rows)       │   │
│  │  │  Source: SAMMS SQL Srv  │    APPEND → Bronze Lakehouse  │   │
│  │  │  Sink:   bhg_bronze     │    Dose.br_tblDoseExcuse      │   │
│  │  └─────────────────────────┘                               │   │
│  └─────────────────────────────────────────────────────────────┘   │
│                         │ ForEach Succeeded                        │
│                         ▼                                          │
│  ┌──────────────────────────────────┐                              │
│  │  nb_dose_bronze_to_silver        │  Cell 1: Load Bronze → dedup │
│  │  (Notebook — Dose)               │  Cell 2: Delta MERGE → Silver│
│  └──────────────────────────────────┘                              │
│                         │ Succeeded                                │
│                         ▼                                          │
│  ┌──────────────────────────────────┐                              │
│  │  nb_dose_excuse_bronze_to_silver │  Cell 1: Load Bronze → dedup │
│  │  (Notebook — Dose Excuse)        │  Cell 2: Delta MERGE → Silver│
│  └──────────────────────────────────┘                              │
└─────────────────────────────────────────────────────────────────────┘
```

**Lakehouse Targets:**


| Layer  | Lakehouse Name | Schema | Table              |
| ------ | -------------- | ------ | ------------------ |
| Bronze | `bhg_bronze`   | `Dose` | `br_tblDose`       |
| Bronze | `bhg_bronze`   | `Dose` | `br_tblDoseExcuse` |
| Silver | `bhg_silver`   | `pats` | `sl_tblDose`       |
| Silver | `bhg_silver`   | `pats` | `sl_tblDoseExcuse` |


---

## 2. What This Pipeline Does — Plain English

The SAMMS system records every medication dose dispensed to a patient and every time a patient's missed dose was formally excused. This data lives in two tables in each clinic's local SAMMS SQL Server database:

- `tblDose` — one row per dose administration event (which patient, which medication date, how much, who dispensed it, whether it was voided)
- `tblDoseExcuse` — one row per formally excused absence from dosing

There are 80+ clinics, each with their own SAMMS database.

This pipeline:

1. **Visits each clinic's SAMMS database** one at a time (ForEach loop)
2. **Checks** whether that clinic's SAMMS version has a column called `InventoryGroup` — not all versions do
3. **Extracts dose records** within a rolling lookback window (default 15 days back as the year anchor, plus a month-based window for the date range)
4. **Extracts all dose excuse records** for the site — no date filter, full table per site
5. **Appends** both extracts to their respective Bronze tables, tagged with a run ID
6. After all clinics are done, runs **two notebooks** that merge Bronze into Silver — one for Dose, one for Dose Excuse — using RowChkSum for change detection and applying RowState logic

---

## 3. Prerequisites — What You Need Before Starting


| Item                          | What It Is                                                     | Where It Lives                 |
| ----------------------------- | -------------------------------------------------------------- | ------------------------------ |
| SAMMS SQL Server connection   | A Fabric connection to the on-premise SAMMS SQL Server gateway | Fabric workspace → Connections |
| `bhg_bronze` Lakehouse        | Bronze layer Lakehouse                                         | Fabric workspace               |
| `bhg_silver` Lakehouse        | Silver layer Lakehouse                                         | Fabric workspace               |
| `Dose` schema in `bhg_bronze` | Schema for Dose Bronze tables                                  | `bhg_bronze` Lakehouse         |
| `pats` schema in `bhg_silver` | Schema for Silver patient tables                               | `bhg_silver` Lakehouse         |
| On-premise data gateway       | Configured to reach SAMMS SQL Servers                          | Fabric settings                |


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


**Why:** Every Bronze row written by this pipeline run carries this ID in the `_ingest_run_id` column. The Silver notebooks filter Bronze to only this run's rows. In production pass something like `DOSE-2026-05-08-001`.

---

### Parameter 2: `p_lookback_days`


| Setting       | Value             |
| ------------- | ----------------- |
| Name          | `p_lookback_days` |
| Type          | `Int`             |
| Default Value | `15`              |


**Why:** This is the `DaysBack` constant from the original C# ETL — always 15. It is used to compute the year anchor for the YEAR() filter. For example on 2026-05-08 with `p_lookback_days=15`, the year anchor is `YEAR(2026-04-23 minus 1 year)` = `YEAR(2025-04-23)` = `2025`. All dose records from year 2025 or later are candidates. Do not change this value unless you have a specific reason.

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
    "site_code": "ColoradoSpringsV5",
    "source_database": "SAMMS-ColoradoSpringsV5",
    "source_schema": "dbo",
    "source_table_dose": "tblDose",
    "source_table_excuse": "tblDOSE_Excuse",
    "dose_lookback_months": 6
  }
]
```

**Why:** This array controls which clinics the ForEach loop processes. For now it contains a single clinic for testing. After the single-database test passes, you will add all 80+ clinics.

Each object has six properties:


| Property               | Meaning                                                          | Example                   |
| ---------------------- | ---------------------------------------------------------------- | ------------------------- |
| `site_code`            | Clinic identifier used as a data tag in every row                | `ColoradoSpringsV5`       |
| `source_database`      | Exact database name on the SAMMS SQL Server                      | `SAMMS-ColoradoSpringsV5` |
| `source_schema`        | Schema where tblDose lives (always `dbo`)                        | `dbo`                     |
| `source_table_dose`    | Source dose table name                                           | `tblDose`                 |
| `source_table_excuse`  | Source dose excuse table name                                    | `tblDoseExcuse`           |
| `dose_lookback_months` | **1** for sites V10A, CBCO, V21, V10 — **6** for all other sites | `6`                       |


> **The `dose_lookback_months` split:** Four VA-affiliated sites (V10A, CBCO, V21, V10) use a tighter 1-month lookback window in the original C# ETL. All other sites use 6 months. Set this correctly per site when you expand to all clinics. See [Section 17](#17-why-the-where-clause-has-two-date-columns-and-a-site-group-split) for why.

---

## 5. Step 1 — Create the Data Pipeline in Fabric

1. Open your Fabric workspace
2. Click **+ New** → **Data pipeline**
3. Name it: `pl_dose_samms_to_lakehouse`
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

## 7. Step 3 — Inside ForEach: Add the Lookup Activity (InventoryGroup Check)

### Why this activity exists

Not all SAMMS clinic databases have the `InventoryGroup` column in `tblDose`. It was added in a newer SAMMS version. If you include `InventoryGroup` in the SELECT for a clinic that does not have it, the Copy activity fails with "invalid column name". So before extracting, you check whether the column exists. The result is used to either select the real column or substitute a NULL placeholder.

This is the same pattern used for `ServiceType` in the DartsSrv pipeline.

### Add the activity

1. Click **Edit** on the ForEach activity to open its inner canvas
2. Drag a **Lookup** activity onto the inner canvas
3. Rename it: `lkp_check_inventorygroup_exists`

### Configure Settings tab


| Setting        | Value                                                                      |
| -------------- | -------------------------------------------------------------------------- |
| Source dataset | SQL Server dataset using connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` |
| Use query      | **Query**                                                                  |
| Query          | Expression below                                                           |


### Query — paste into Query field (Expression mode)

```
@concat(
'SELECT inventory_group_exists = COUNT(1)
FROM [', item().source_database, '].sys.columns c
INNER JOIN [', item().source_database, '].sys.tables t
    ON c.object_id = t.object_id
INNER JOIN [', item().source_database, '].sys.schemas s
    ON t.schema_id = s.schema_id
WHERE s.name = ''', item().source_schema, '''
  AND t.name = ''', item().source_table_dose, '''
  AND c.name = ''InventoryGroup'';'
)
```

**What it returns:** A single row: `inventory_group_exists` = `1` if the column exists, `0` if not.

**How to reference the result later:**

```
activity('lkp_check_inventorygroup_exists').output.firstRow.inventory_group_exists
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
    "name": "lkp_check_inventorygroup_exists",
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
                "value": "@concat(\n'SELECT inventory_group_exists = COUNT(1)\nFROM [', item().source_database, '].sys.columns c\nINNER JOIN [', item().source_database, '].sys.tables t\n    ON c.object_id = t.object_id\nINNER JOIN [', item().source_database, '].sys.schemas s\n    ON t.schema_id = s.schema_id\nWHERE s.name = ''', item().source_schema, '''\n  AND t.name = ''', item().source_table_dose, '''\n  AND c.name = ''InventoryGroup'';'\n)",
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

## 8. Step 4 — Inside ForEach: Add Copy Activity 1 (Dose Bronze Extraction)

### What it does

- Connects to the clinic's SAMMS SQL Server
- Runs a SELECT query with a rolling lookback window for `tblDose`
- Appends rows to Bronze table `bhg_bronze.Dose.br_tblDose`
- Adds metadata columns to every row

### Add the activity

1. Still on the inner ForEach canvas
2. Drag a **Copy** activity
3. Rename it: `cp_dose_to_bronze`
4. Draw arrow: `lkp_check_inventorygroup_exists` → `cp_dose_to_bronze` with condition **Succeeded**

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
    CONVERT(date, GETDATE()) AS _source_query_end_date,

    DoseID,
    CltID,
    GuestID,
    dtDate,
    dtMedDate,
    Dose,
    strUser,
    blVoid,
    strVoidReason,
    blException,
    Bottletype,
    blBulk,
    blPrepack,
    dtVoid,
    dtGiven,
    dtPrep,
    ordernum,
    ExceptionReason,
    Exceptiontype,
    Manualauthuser,
    manualauthdtm,
    Dosenote,
    Dosesig,
    dosesigimg,
    siteid,
    Ppstaff,
    ',
if(
    equals(activity('lkp_check_inventorygroup_exists').output.firstRow.inventory_group_exists, 1),
    'InventoryGroup',
    'CAST(NULL AS varchar(100)) AS InventoryGroup'
),
',

    RowChkSum = CHECKSUM(
        DoseID,
        CltID,
        GuestID,
        dtDate,
        dtMedDate,
        Dose,
        strUser,
        blVoid,
        strVoidReason,
        blException,
        Bottletype,
        blBulk,
        blPrepack,
        dtVoid,
        dtGiven,
        dtPrep,
        ordernum,
        ExceptionReason,
        Exceptiontype,
        Manualauthuser,
        manualauthdtm,
        CAST(Dosenote AS nvarchar(4000)),
        CAST(Dosesig AS nvarchar(4000)),
        siteid,
        Ppstaff
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table_dose, '
WHERE CltID IS NOT NULL
  AND (
      YEAR(dtDate) >= YEAR(DATEADD(YEAR, -1, DATEADD(DAY, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())))
      OR YEAR(dtMedDate) >= YEAR(DATEADD(YEAR, -1, DATEADD(DAY, -', string(pipeline().parameters.p_lookback_days), ', GETDATE())))
  )
  AND dtDate <= DATEADD(DAY, 2, GETDATE())
  AND dtDate >= DATEADD(MONTH, -', string(item().dose_lookback_months), ', GETDATE())
ORDER BY dtDate, DoseID'
)

```

### Breaking Down What This Query Does

#### Metadata columns (first 6)

```sql
'ColoradoSpringsV5'                     AS _site_code,
'SAMMS-ColoradoSpringsV5'               AS _source_database,
'DOSE-2026-05-08-001'                   AS _ingest_run_id,
GETDATE()                               AS _extracted_at,
CONVERT(date, DATEADD(day,-15,GETDATE())) AS _source_query_date_anchor,
CONVERT(date, GETDATE())                AS _source_query_end_date
```

#### Data columns (26 columns from tblDose)

All clinical columns from `tblDose`. See [Section 15](#15-how-change-detection-works-rowchksum) for the column reference.

#### InventoryGroup — conditional column (same pattern as ServiceType in DartsSrv)

```
if(inventory_group_exists = 1,
    'InventoryGroup',
    'CAST(NULL AS varchar(100)) AS InventoryGroup'
)
```

The Bronze table always has an `InventoryGroup` column. For clinics without it, the value is NULL. This keeps the schema consistent across all clinics.

#### RowChkSum — change detection fingerprint

```sql
RowChkSum = CHECKSUM(DoseID, CltID, GuestID, dtDate, dtMedDate, Dose, ...)
```

Computes a single integer from all stable data columns. If any column changes in SAMMS, this integer changes. Used in the Silver MERGE to decide whether to do a full update or skip unchanged rows.

> **Important:** `dosesigimg` (varbinary) is intentionally **excluded** from CHECKSUM — `CHECKSUM()` does not work reliably on binary/image data. `InventoryGroup` is also excluded because it is an optional column not present in all SAMMS databases.

#### WHERE clause — lookback filter

```sql
WHERE CltID IS NOT NULL
  AND (
      YEAR(dtDate)    >= YEAR(DATEADD(YEAR, -1, DATEADD(DAY, -15, GETDATE())))
      OR YEAR(dtMedDate) >= YEAR(DATEADD(YEAR, -1, DATEADD(DAY, -15, GETDATE())))
  )
  AND dtDate <= DATEADD(DAY, 2, GETDATE())
  AND dtDate >= DATEADD(MONTH, -6, GETDATE())   -- or -1 for special sites
```

See [Section 17](#17-why-the-where-clause-has-two-date-columns-and-a-site-group-split) for a full explanation.

### Configure Sink tab


| Setting                 | Value                                  |
| ----------------------- | -------------------------------------- |
| Sink type               | **Lakehouse**                          |
| Linked service name     | `bhg_bronze`                           |
| Workspace ID            | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder             | `Tables`                               |
| Schema                  | `Dose`                                 |
| Table                   | `br_tblDose`                           |
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

## 9. Step 5 — Inside ForEach: Add Copy Activity 2 (Dose Excuse Bronze Extraction)

### What it does

- Connects to the same clinic's SAMMS SQL Server
- Extracts **all** rows from `tblDoseExcuse` — no date filter. The original C# ETL uses `WHERE 1=1` for dose excuse, meaning it pulls the full excuse history per site on every run. The Silver notebook handles deduplication and RowState.
- Appends rows to Bronze table `bhg_bronze.Dose.br_tblDoseExcuse`

### Add the activity

1. Still on the inner ForEach canvas
2. Drag a **Copy** activity
3. Rename it: `cp_excuse_to_bronze`
4. Draw arrow: `cp_dose_to_bronze` → `cp_excuse_to_bronze` with condition **Succeeded**

> **Why after Dose copy, not in parallel?** SAMMS on-premise servers are limited. Running two simultaneous SELECTs against the same clinic database in the same ForEach iteration adds unnecessary load. Sequential is safer.

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

    ExId,
    CltID,
    DtEx,
    Dtstamp,
    StrUser,

    RowChkSum = CHECKSUM(
        ExId,
        CltID,
        DtEx,
        Dtstamp,
        StrUser
    )

FROM [', item().source_database, '].', item().source_schema, '.', item().source_table_excuse, '
ORDER BY ExId'
)
```

> **No WHERE clause:** The original C# ETL uses `WHERE 1=1` for dose excuse — it pulls all excuse records for the site every run. RowState management in the notebook handles marking records as active/inactive.

> **StrExcused note:** The Silver table model has a `StrExcused` column, but `SaveDoseExcuse` in C# does not read it from the source DataTable. If your SAMMS database has a column named `StrExcused` in `tblDoseExcuse`, add it to both the SELECT and the CHECKSUM above. Query `INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblDoseExcuse'` on your test SAMMS to confirm.

### Configure Sink tab


| Setting                 | Value                                  |
| ----------------------- | -------------------------------------- |
| Sink type               | **Lakehouse**                          |
| Linked service name     | `bhg_bronze`                           |
| Workspace ID            | `c5097ffb-b78e-441d-9575-a82bac23cac8` |
| Artifact (Lakehouse) ID | `77d24027-6a1c-43a8-a998-1a14dd3c0d52` |
| Root folder             | `Tables`                               |
| Schema                  | `Dose`                                 |
| Table                   | `br_tblDoseExcuse`                     |
| Table action            | **Append**                             |
| Apply V-Order           | No (unchecked)                         |


### Configure Mapping / Translator tab


| Setting                 | Value       |
| ----------------------- | ----------- |
| Type conversion         | **Enabled** |
| Allow data truncation   | **True**    |
| Treat boolean as number | **False**   |


---

## 10. Step 6 — Add Notebook Activity 1 (Dose Bronze → Silver)

### Add the activity

1. Go back to the **main pipeline canvas** (outside ForEach)
2. Drag a **Notebook** activity
3. Rename it: `nb_dose_bronze_to_silver`
4. Draw arrow: `fe_each_samms_site` → `nb_dose_bronze_to_silver` with condition **Succeeded**

### Configure Settings tab


| Setting   | Value                                                |
| --------- | ---------------------------------------------------- |
| Notebook  | Select `nb_dose_bronze_to_silver` (create in Step 8) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8`               |


### Configure Parameters


| Parameter Name    | Type   | Value                                                 |
| ----------------- | ------ | ----------------------------------------------------- |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |


---

## 11. Step 7 — Add Notebook Activity 2 (Dose Excuse Bronze → Silver)

### Add the activity

1. Still on the **main pipeline canvas**
2. Drag another **Notebook** activity
3. Rename it: `nb_dose_excuse_bronze_to_silver`
4. Draw arrow: `nb_dose_bronze_to_silver` → `nb_dose_excuse_bronze_to_silver` with condition **Succeeded**

> **Why sequential notebooks?** The Dose Excuse notebook does not depend on the Dose notebook's results. However running them sequentially avoids Spark cluster contention during development. Once tested you may run them in parallel if needed.

### Configure Settings tab


| Setting   | Value                                                       |
| --------- | ----------------------------------------------------------- |
| Notebook  | Select `nb_dose_excuse_bronze_to_silver` (create in Step 9) |
| Workspace | `c5097ffb-b78e-441d-9575-a82bac23cac8`                      |


### Configure Parameters


| Parameter Name    | Type   | Value                                                 |
| ----------------- | ------ | ----------------------------------------------------- |
| `p_ingest_run_id` | String | `@pipeline().parameters.p_ingest_run_id` (Expression) |


---

## 12. Step 8 — Create Notebook: nb_dose_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_dose_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` Lakehouses in the notebook's Lakehouse panel

---

### Cell 1 — Load Bronze and Prepare Silver Source

```python
from pyspark.sql.functions import col, current_timestamp, year, row_number, when, lit
from pyspark.sql.window import Window

# Pipeline passes p_ingest_run_id as a parameter.
# The try/except lets you run this manually during development.
try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Dose.br_tblDose"
silver_table = "bhg_silver.pats.sl_tblDose"

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
# Key = _site_code + DoseID.
# If the same record appears twice (e.g., due to a retry), keep the latest extraction.
w = Window.partitionBy("_site_code", "DoseID").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("DoseID").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # ── RowState Logic ──────────────────────────────────────────────────────────
    # Rule 1: blVoid = true AND dtVoid = true → RowState = false (voided dose)
    # Rule 2: CltID < 0 AND CltID != -111 → RowState = false (invalid client)
    #         Note: CltID = -111 is a valid system client — do NOT soft-delete it
    # Otherwise → RowState = true (active dose record)
    .withColumn(
        "RowState",
        when(
            (col("blVoid") == True) & (col("dtVoid") == True),
            lit(False)
        ).when(
            (col("CltID") < 0) & (col("CltID") != -111),
            lit(False)
        ).otherwise(lit(True))
    )

    # Year of dtDate — useful for partitioning and year-based reporting
    .withColumn("dtDateYear", year(col("dtDate")))

    # Silver audit timestamps
    .withColumn("silver_updated_at", current_timestamp())
    .withColumn("last_seen_at", current_timestamp())
)

src_df.createOrReplaceTempView("vw_dose_current_run")

src_count = src_df.count()
print(f"Prepared source rows for Silver: {src_count}")

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
    print(f"Created Silver table and inserted rows: {src_count}")
else:
    print(f"Silver table already exists: {silver_table}")
```

### Explanation of Cell 1 — Key Decisions


| Code Section                                 | Why It Exists                                                                                                                |
| -------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------- |
| `try: p_ingest_run_id`                       | Lets you run the notebook manually without the pipeline for testing                                                          |
| Filter on `_ingest_run_id`                   | Bronze accumulates rows from every run — filter to just this run's rows                                                      |
| `if bronze_count == 0: raise Exception`      | Fail loudly if Bronze is empty — catches upstream failures before a silent empty Silver update                               |
| `Window.partitionBy("_site_code", "DoseID")` | Deduplicates in case a retry wrote the same record twice                                                                     |
| RowState `when blVoid & dtVoid`              | Mirrors `if ((dose.BlVoid == true) && (dose.DtVoid == true)) { dose.RowState = false; }` in `SaveDoses.cs`                   |
| RowState `when CltID < 0 AND CltID != -111`  | Mirrors the negative CltID check in `SaveDoses.cs`. **-111 is a valid system client — exclude it from the soft-delete rule** |
| `dtDateYear`                                 | Year of the actual administration date — for partition pruning and year-range queries                                        |
| `silver_created_at` on initial write         | Set once, never overwritten — records when this row first appeared in Silver                                                 |


---

### Cell 2 — Merge Into Silver Delta Table

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tblDose"

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
        # Unique business key: clinic + dose event ID
        "tgt._site_code = src._site_code AND tgt.DoseID = src.DoseID"
    )

    # CASE 1: Record exists AND data changed (RowChkSum differs or NULL on either side)
    # → Full update of all data columns + recalculate RowState
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
    # → Still refresh RowState in case void status changed without triggering checksum change
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum = src.RowChkSum
        """,
        set={
            "last_seen_at":               "current_timestamp()",
            "RowState":                   "src.RowState",
            "_ingest_run_id":             "src._ingest_run_id",
            "_extracted_at":              "src._extracted_at",
            "_source_query_date_anchor":  "src._source_query_date_anchor",
            "_source_query_end_date":     "src._source_query_end_date"
        }
    )

    # CASE 3: New record not yet in Silver → Insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("Dose Silver MERGE completed successfully.")
```

### Explanation of the Three MERGE Branches

#### Branch 1 — Full update (RowChkSum changed)

A dose record already exists in Silver but at least one column changed in SAMMS (dose was corrected, voided, exception added, etc.). All data columns are overwritten. `silver_created_at` is NOT overwritten — it records when this dose first entered Silver.

#### Branch 2 — Metadata-only update (RowChkSum identical)

The dose record is unchanged. It appeared in the extract only because its `dtDate` or `dtMedDate` fell within the lookback window. No data columns are rewritten — this avoids unnecessary Delta table write amplification for millions of unchanged rows.

**Note on RowState in Branch 2:** Even on a checksum-unchanged match, `RowState` is refreshed. This is because void status (`blVoid + dtVoid`) could theoretically change at the same time as other fields that cancel out in the CHECKSUM (extremely rare but possible). Refreshing RowState on every touch is the safe approach.

#### Branch 3 — New insert

The `_site_code + DoseID` combination has never been seen in Silver. Insert the complete row including `silver_created_at`.

---

## 13. Step 9 — Create Notebook: nb_dose_excuse_bronze_to_silver

1. In your Fabric workspace, click **+ New** → **Notebook**
2. Name it: `nb_dose_excuse_bronze_to_silver`
3. Attach both `bhg_bronze` and `bhg_silver` Lakehouses

---

### Cell 1 — Load Bronze and Prepare Silver Source

```python
from pyspark.sql.functions import col, current_timestamp, row_number, lit
from pyspark.sql.window import Window

try:
    p_ingest_run_id
except NameError:
    p_ingest_run_id = "test-run-001"

bronze_table = "bhg_bronze.Dose.br_tblDoseExcuse"
silver_table = "bhg_silver.pats.sl_tblDoseExcuse"

print(f"Processing ingest_run_id: {p_ingest_run_id}")

bronze_df = spark.table(bronze_table).where(col("_ingest_run_id") == p_ingest_run_id)

bronze_count = bronze_df.count()
print(f"Bronze rows for this run: {bronze_count}")

if bronze_count == 0:
    raise Exception(f"No Bronze Dose Excuse rows found for ingest_run_id = {p_ingest_run_id}")

# Deduplicate: key = _site_code + ExId
w = Window.partitionBy("_site_code", "ExId").orderBy(col("_extracted_at").desc())

src_df = (
    bronze_df
    .where(col("_site_code").isNotNull() & col("ExId").isNotNull())
    .withColumn("rn", row_number().over(w))
    .where(col("rn") == 1)
    .drop("rn")

    # ── RowState Logic ──────────────────────────────────────────────────────────
    # For Dose Excuse, ALL incoming rows are active (RowState = true).
    # The C# ETL pre-resets all existing rows to RowState=false before the upsert,
    # then sets any upserted row back to RowState=true.
    # In Fabric: every row returned by SAMMS this run is considered active.
    # Records that disappear from SAMMS (not returned) will retain their last RowState
    # in Silver — this is acceptable because excuse records are rarely deleted.
    .withColumn("RowState", lit(True))

    .withColumn("silver_updated_at", current_timestamp())
    .withColumn("last_seen_at", current_timestamp())
)

src_df.createOrReplaceTempView("vw_excuse_current_run")

src_count = src_df.count()
print(f"Prepared source rows for Dose Excuse Silver: {src_count}")

if not spark.catalog.tableExists(silver_table):
    (
        src_df
        .withColumn("silver_created_at", current_timestamp())
        .write
        .format("delta")
        .mode("overwrite")
        .saveAsTable(silver_table)
    )
    print(f"Created Dose Excuse Silver table: {src_count}")
else:
    print(f"Silver table exists: {silver_table}")
```

### Explanation of RowState for Dose Excuse

The C# `SaveDoseExcuse` method does a **full pre-reset** before each run — it sets every existing Azure excuse row to `RowState = false`, then sets any row returned from SAMMS back to `true`. Rows not returned by SAMMS stay `false` (soft-deleted).

In Fabric with the unified approach, we cannot do the pre-reset pattern easily without loading the entire Silver table into memory. The practical approach above sets all incoming rows to `RowState = true`. Rows that disappear from SAMMS are not automatically soft-deleted in this implementation.

**If you need strict soft-delete for Dose Excuse:** Add a post-merge step that sets `RowState = false` for all Silver rows where `_ingest_run_id` does not equal the current run:

```python
# Optional strict soft-delete (add as Cell 3 if needed)
silver_delta.update(
    condition=f"_ingest_run_id != '{p_ingest_run_id}'",
    set={"RowState": "false"}
)
```

---

### Cell 2 — Merge Into Dose Excuse Silver Delta Table

```python
from delta.tables import DeltaTable

silver_table = "bhg_silver.pats.sl_tblDoseExcuse"

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
        # Unique business key: clinic + excuse ID
        "tgt._site_code = src._site_code AND tgt.ExId = src.ExId"
    )

    # CASE 1: Record exists AND data changed → full update
    .whenMatchedUpdate(
        condition="""
            tgt.RowChkSum IS NULL
            OR src.RowChkSum IS NULL
            OR tgt.RowChkSum <> src.RowChkSum
        """,
        set=update_set
    )

    # CASE 2: Record exists AND data unchanged → metadata-only update
    .whenMatchedUpdate(
        condition="tgt.RowChkSum = src.RowChkSum",
        set={
            "last_seen_at":    "current_timestamp()",
            "RowState":        "src.RowState",
            "_ingest_run_id":  "src._ingest_run_id",
            "_extracted_at":   "src._extracted_at"
        }
    )

    # CASE 3: New record → insert all columns
    .whenNotMatchedInsert(values=insert_values)

    .execute()
)

print("Dose Excuse Silver MERGE completed successfully.")
```

---

## 14. End-to-End Flow Summary

```
TRIGGER: Pipeline runs with parameters:
  p_ingest_run_id  = "DOSE-2026-05-08-001"
  p_lookback_days  = 15
  p_sites          = [ { site_code: "ColoradoSpringsV5",
                         source_database: "SAMMS-ColoradoSpringsV5",
                         dose_lookback_months: 6 } ]

STEP 1 — ForEach begins
  ↓ Current item: { site_code: "ColoradoSpringsV5", source_database: "SAMMS-ColoradoSpringsV5", ... }

STEP 2 — lkp_check_inventorygroup_exists (Lookup)
  → Queries SAMMS-ColoradoSpringsV5 sys.columns for InventoryGroup
  → Returns: { inventory_group_exists: 1 }   (or 0 if absent)

STEP 3 — cp_dose_to_bronze (Copy Activity)
  → Builds dynamic SELECT from item() values + Lookup result
  → WHERE: CltID IS NOT NULL
           AND (YEAR(dtDate) >= 2025 OR YEAR(dtMedDate) >= 2025)
           AND dtDate <= today + 2 days
           AND dtDate >= today - 6 months
  → APPENDs rows tagged with _ingest_run_id = "DOSE-2026-05-08-001" to:
     bhg_bronze.Dose.br_tblDose

STEP 4 — cp_excuse_to_bronze (Copy Activity)
  → SELECT all tblDoseExcuse rows for this site (no date filter)
  → APPENDs rows tagged with _ingest_run_id = "DOSE-2026-05-08-001" to:
     bhg_bronze.Dose.br_tblDoseExcuse

STEP 5 — ForEach moves to next site and repeats Steps 2–4
  ... until all sites done ...

STEP 6 — ForEach completes

STEP 7 — nb_dose_bronze_to_silver (Cell 1)
  → Reads br_tblDose WHERE _ingest_run_id = "DOSE-2026-05-08-001"
  → Deduplicates on _site_code + DoseID
  → Computes RowState: false if voided, false if invalid CltID, true otherwise
  → Adds dtDateYear, silver_updated_at, last_seen_at
  → Creates Silver table on first run

STEP 8 — nb_dose_bronze_to_silver (Cell 2)
  → Delta MERGE into bhg_silver.pats.sl_tblDose
     RowChkSum changed   → FULL UPDATE of all data columns + new RowState
     RowChkSum same      → lightweight update (last_seen_at + RowState only)
     New record          → INSERT all columns

STEP 9 — nb_dose_excuse_bronze_to_silver (Cell 1 + Cell 2)
  → Same pattern for tblDoseExcuse
  → All incoming rows get RowState = true
  → Delta MERGE into bhg_silver.pats.sl_tblDoseExcuse

DONE: Both Silver tables are current for all changes within the lookback window.
```

---

## 15. How Change Detection Works (RowChkSum)

SQL Server's `CHECKSUM()` function computes a single integer from a list of column values. If any column changes, the integer changes. This is the same mechanism used in the original C# ETL.

### CHECKSUM columns for tblDose


| Included in CHECKSUM                               | Excluded                  | Reason for exclusion                               |
| -------------------------------------------------- | ------------------------- | -------------------------------------------------- |
| `DoseID`, `CltID`, `GuestID`                       | `dosesigimg`              | varbinary — `CHECKSUM()` unreliable on binary data |
| `dtDate`, `dtMedDate`, `dtGiven`, `dtPrep`         | `InventoryGroup`          | Optional column — not in all SAMMS versions        |
| `Dose`, `strUser`, `blVoid`, `strVoidReason`       | Pipeline metadata columns | Added by the pipeline, not from SAMMS              |
| `blException`, `Bottletype`, `blBulk`, `blPrepack` |                           |                                                    |
| `dtVoid`, `ordernum`, `ExceptionReason`            |                           |                                                    |
| `Exceptiontype`, `Manualauthuser`, `manualauthdtm` |                           |                                                    |
| `Dosenote`, `Dosesig`, `siteid`, `Ppstaff`         |                           |                                                    |


### CHECKSUM columns for tblDoseExcuse

All columns included — `ExId`, `CltID`, `DtEx`, `Dtstamp`, `StrUser`. No binary data or optional columns.

### Change detection flow

```
SAMMS today:  DoseID=5001, CltID=1234, Dose=80, blVoid=false, ..., RowChkSum=1928374651
Silver:       DoseID=5001, CltID=1234, Dose=80, blVoid=false, ..., RowChkSum=1928374651
→ Checksums EQUAL → lightweight metadata update only. Record unchanged.

SAMMS today:  DoseID=5001, CltID=1234, Dose=80, blVoid=true,  ..., RowChkSum=9876512340
Silver:       DoseID=5001, CltID=1234, Dose=80, blVoid=false, ..., RowChkSum=1928374651
→ Checksums DIFFER → full update. blVoid changed in SAMMS.
```

---

## 16. RowState Logic — Active vs Soft-Deleted Records

`RowState` is a bit column (`true`/`false`) in both `sl_tblDose` and `sl_tblDoseExcuse`.

### For tblDose


| Condition                           | RowState | Source                                               |
| ----------------------------------- | -------- | ---------------------------------------------------- |
| `blVoid = true` AND `dtVoid = true` | `false`  | Dose was voided in SAMMS                             |
| `CltID < 0` AND `CltID != -111`     | `false`  | Invalid/deleted patient ID                           |
| `CltID = -111`                      | `true`   | Special system client — valid despite negative value |
| All other rows                      | `true`   | Active, valid dose record                            |


> **Critical:** CltID = `-111` is a known system patient used in some SAMMS workflows. The original C# code explicitly exempts `-111` from the negative-CltID soft-delete rule with:  
> `if ((dose.CltId < 0) && (dose.CltId != -111)) { dose.RowState = false; }`  
> Do not forget this exemption in the notebook code.

> **Note on `dtVoid`:** Despite the `dt` prefix, `dtVoid` is a **bit** column (boolean), not a datetime. This is a naming anomaly in the SAMMS schema. Both `blVoid` and `dtVoid` must be `true` together for the void rule to trigger.

### For tblDoseExcuse


| Condition                                     | RowState                                     |
| --------------------------------------------- | -------------------------------------------- |
| Row returned from SAMMS in current run        | `true`                                       |
| Row in Silver but not returned in current run | Stays at last value (see note in Section 13) |


---

## 17. Why the WHERE Clause Has Two Date Columns and a Site-Group Split

### Two date columns: dtDate and dtMedDate

```sql
YEAR(dtDate) >= YEAR(DATEADD(YEAR, -1, DATEADD(DAY, -15, GETDATE())))
OR YEAR(dtMedDate) >= YEAR(DATEADD(YEAR, -1, DATEADD(DAY, -15, GETDATE())))
```

A dose event has two dates:

- `dtMedDate` — the **scheduled** date (when the dose was supposed to be given)
- `dtDate` — the **actual administration date** (when it was physically dispensed, which may differ)

Either date can fall in the extraction window. Using only `dtDate` would miss doses that were scheduled in the window but administered on a different day. Using only `dtMedDate` would miss doses where the administration date moved. Checking both ensures no dose events are missed.

### Year filter vs month filter

The WHERE has both a year-level filter and a month-level filter:

```sql
-- Year filter (broad): both dtDate and dtMedDate must be from the current year or prior year
YEAR(dtDate) >= YEAR(WorkDate - 15 days - 1 year)

-- Month filter (narrow): dtDate must be recent enough for the Silver merge to be meaningful
AND dtDate >= DATEADD(MONTH, -6, GETDATE())   -- or -1 for special sites
AND dtDate <= DATEADD(DAY, 2, GETDATE())
```

The year filter is inherited from the original C# ETL and catches any dose from the current or previous year. The month filter provides the practical rolling window for daily incremental loads.

### Site-group split: 1 month vs 6 months


| Sites                | `dose_lookback_months` | Reason                                                                                                                                                                                                                     |
| -------------------- | ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| V10A, CBCO, V21, V10 | `1`                    | VA-affiliated sites — these clinics have high-frequency dose data. A tighter 1-month window reduces extract volume while still covering recent changes. The original C# ETL uses `WorkDate.AddMonths(-1)` for these sites. |
| All other sites      | `6`                    | Standard sites — 6-month window ensures late-arriving corrections, backdated entries, and administrative changes are captured. Original C# ETL uses `WorkDate.AddMonths(-6)`.                                              |


Set `dose_lookback_months` correctly in your `p_sites` array entry for each clinic.

### Upper bound: today + 2 days

```sql
AND dtDate <= DATEADD(DAY, 2, GETDATE())
```

Allows 2 days of forward tolerance for doses entered with a future administration date (pre-scheduled take-home doses entered in advance).

---

## 18. Known Anomalies and Cautions

### 1 — dtVoid is a bit, not a datetime

`dtVoid` in `tblDose` is a **bit (boolean)** column despite its `dt` prefix. This naming anomaly exists in the SAMMS source. In your Spark notebook, compare it as a boolean (`True`/`False`), not as a date.

---

### 2 — InventoryGroup is optional

Not all SAMMS databases have `InventoryGroup` in `tblDose`. This is why the Lookup in Step 3 exists. The Copy activity substitutes `CAST(NULL AS varchar(100)) AS InventoryGroup` for clinics that lack it. In Silver, these rows will have `null` in `InventoryGroup`. This is expected.

---

### 3 — dosesigimg is varbinary

`dosesigimg` stores the electronic signature image as binary data. It is NOT included in `CHECKSUM()` because `CHECKSUM()` is unreliable on `varbinary` columns. It will be stored in Bronze/Silver as-is but will not drive change detection. If the signature image changes without any other column changing, it will not trigger an update. This is acceptable — signature images do not change after signing.

---

### 4 — CltID = -111 is valid

A negative `CltID` normally indicates a deleted or invalid patient. However CltID = `-111` is a known system-level client used by some SAMMS workflows. The C# ETL explicitly exempts it: `if ((dose.CltId < 0) && (dose.CltId != -111))`. This exemption is coded into the Spark notebook RowState logic. Do not remove it.

---

### 5 — StrExcused may exist in tblDoseExcuse

The Silver table model for `pats.tbl_Dose_Excuse` includes a `StrExcused` column. The C# `SaveDoseExcuse` method does not read it from the source DataTable in the current code. Before building the Fabric pipeline, confirm whether `tblDoseExcuse` in your SAMMS database has a `StrExcused` column:

```sql
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'tblDoseExcuse'
ORDER BY ORDINAL_POSITION
```

If it exists, add it to both the SELECT and the CHECKSUM in the Dose Excuse Copy activity query.

---

### 6 — Dose Excuse strict soft-delete not implemented by default

The C# ETL resets all existing dose excuse rows to `RowState = false` before each run, then sets rows from the current extract back to `true`. Rows not returned (deleted in SAMMS) stay `false`.

The Fabric notebook above does NOT implement this pre-reset by default, because it would require reading the entire Silver excuse table into memory. If strict soft-delete is required, add the optional Cell 3 described in Section 13.

---

### 7 — CHECKSUM columns must match exactly

The `CHECKSUM()` expression in the Fabric Copy activity query must use the **same columns in the same order** that the original C# `SelectConstructor` uses. If they differ, checksums will never match, and every row will trigger a full update on every run (wasted writes). Verify the checksum column list against the `dms.tbl_MapSrc2Dsn` metadata:

```sql
SELECT StepKey, FieldName, DsnFieldName, ChkSum
FROM dms.tbl_MapSrc2Dsn
WHERE ActionKey = 10
  AND DsnTbl IN ('tbl_Dose', 'tbl_dose')
ORDER BY StepKey
```

Columns where `ChkSum = 1` go into the CHECKSUM. The query above gives you the authoritative list from the BHG_DR control database.

---

## 19. Troubleshooting Guide

### Pipeline fails at `lkp_check_inventorygroup_exists`

**Symptom:** Lookup fails with connection error or timeout.  
**Cause:** SAMMS SQL Server for this clinic is unreachable, gateway is down, or connection GUID is wrong.  
**Fix:** Verify connection `9743b95a-fd66-4f7c-9767-e6eb0f1ecab7` is active. Check that `source_database` in `p_sites` exactly matches the actual SQL Server database name (case-sensitive in some configurations).

---

### Dose Copy activity copies 0 rows

**Symptom:** Copy succeeds but writes 0 rows.  
**Cause 1:** No dose records fall within the lookback window.  
**Fix 1:** Increase `dose_lookback_months` to 12 for this site and re-run, or increase `p_lookback_days` to 90 to widen the year anchor.  
**Cause 2:** `dtDate` is NULL for all rows at this clinic.  
**Fix 2:** Check if this clinic uses `dtMedDate` exclusively. If so, the YEAR() filter on `dtMedDate` should catch them, but the `AND dtDate >= ...` month filter will exclude rows where `dtDate IS NULL`. Consider adding `OR dtDate IS NULL` to the month filter for this clinic.

---

### Dose Excuse Copy activity copies 0 rows

**Symptom:** `tblDoseExcuse` returns 0 rows.  
**Cause:** This clinic has no dose excuse records (valid for newer or smaller clinics).  
**Fix:** Confirm by running `SELECT COUNT(*) FROM tblDoseExcuse` directly against that SAMMS database. If 0, the site simply has no excuses — this is not an error.

---

### Notebook raises "No Bronze rows found"

**Symptom:** `Exception: No Bronze rows found for ingest_run_id = test-run-001`  
**Cause 1:** Running notebook manually with default fallback run ID, but Bronze was written with a different ID.  
**Fix 1:** Change the fallback value in the `except NameError` block to match an actual `_ingest_run_id` in Bronze. Run `spark.table("bhg_bronze.Dose.br_tblDose").select("_ingest_run_id").distinct().show()` to see what IDs are in Bronze.  
**Cause 2:** Copy activity wrote 0 rows.  
**Fix 2:** See above section.

---

### RowChkSum always differs (every row triggers full update)

**Symptom:** Every run updates every row in Silver even when nothing changed.  
**Cause:** The CHECKSUM columns in the Fabric SELECT do not match the columns used by the C# `SelectConstructor` for ActionKey=10.  
**Fix:** Run the `dms.tbl_MapSrc2Dsn` query in Section 18 (#7) to get the authoritative column list. Update the `CHECKSUM(...)` expression in the Copy activity query to exactly match.

---

### blVoid / dtVoid values are wrong in Silver

**Symptom:** Doses that should be active appear as voided, or vice versa.  
**Cause:** `dtVoid` is being treated as a datetime instead of a boolean bit. In some DataFrame type inference, a bit column can be read as integer 0/1 instead of True/False.  
**Fix:** In the Spark notebook, cast explicitly:

```python
.withColumn(
    "RowState",
    when(
        (col("blVoid").cast("boolean") == True) & (col("dtVoid").cast("boolean") == True),
        lit(False)
    ).when(
        (col("CltID") < 0) & (col("CltID") != -111),
        lit(False)
    ).otherwise(lit(True))
)
```

---

### Silver MERGE is very slow

**Symptom:** Cell 2 runs for a long time.  
**Cause:** Silver table is large with no partition pruning.  
**Fix:** Add Delta partitioning by `dtDateYear` on first creation and include it in the merge condition:

```python
# In Cell 1, first creation:
.write
.format("delta")
.partitionBy("dtDateYear")
.mode("overwrite")
.saveAsTable(silver_table)

# In Cell 2, merge condition:
"tgt._site_code = src._site_code AND tgt.DoseID = src.DoseID AND tgt.dtDateYear = src.dtDateYear"
```

---

*End of Implementation Guide*