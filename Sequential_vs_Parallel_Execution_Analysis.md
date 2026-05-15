# ETL Execution Model — Sequential vs Parallel Analysis

## The Answer: Strictly Sequential — One Site at a Time

The system does **not** run sites in parallel. It processes them one by one, in a strict
single-threaded `foreach` loop inside `BHGTaskRunner.exe`.

---

## How It Actually Works — The Two-Loop Structure

```
BHGTaskRunner.exe 9  (SAMMS-ETL-DartSvc)
│
│  Step 1: Load ALL ready tasks from tsk.vw_TaskListMap
│          WHERE Status = 17 (ready) AND TaskName = 'SAMMS-ETL-DartSvc'
│          This gives a list like:
│            [Site B01 / pats.tbl_DartsSrv]
│            [Site B02 / pats.tbl_DartsSrv]
│            [Site B03 / pats.tbl_DartsSrv]
│            ... up to 80+ sites
│
│  Step 2: OUTER foreach — loops over PARENT tasks
│          (one parent = one scheduled batch, e.g. "Eastern ETL DartsSrv")
│
│          foreach (var pt in pTasks)
│          {
│              Step 3: INNER foreach — loops over CHILD tasks
│                      OrderBy TaskName → then SiteCode → then FromTblVw
│
│              foreach (var st in Tasks
│                          .Where(x => x.ParentTaskId == pt.TaskId)
│                          .OrderBy(o  => o.TaskName)
│                          .ThenBy(b  => b.SiteCode)    ← alphabetical site order
│                          .ThenBy(d  => d.FromTblVw))
│              {
│                  1. Mark task Status = 18  (Running)
│                  2. Build SELECT from dms.tbl_MapSrc2Dsn
│                  3. Execute SELECT against Site B01's SAMMS DB
│                  4. Call BulkDartsSrvLoader() or SaveDartSrv20XX()
│                  5. WAIT for completion  ← fully blocking
│                  6. Mark task Status = 19 (Done) or 20 (Error)
│                  7. Move to next site → Site B02
│              }
│          }
```

---

## Visualised — 3 Sites, 1 Table

```
Time ──────────────────────────────────────────────────────────►

Site B01  ──[SELECT]──[BulkCopy]──[MERGE]──✓
Site B02                                    ──[SELECT]──[BulkCopy]──[MERGE]──✓
Site B03                                                                       ──[SELECT]──[BulkCopy]──[MERGE]──✓
```

Each site waits for the previous one to **fully complete** before the next one starts.
There is zero parallelism inside a single `BHGTaskRunner.exe` process.

---

## Why It Was Designed Sequentially

| Reason | Detail |
|--------|--------|
| **Single shared staging table** | `stg.tbl_dartssrv` is shared by ALL sites. Two parallel sites would both TRUNCATE and write to the same table simultaneously — causing data corruption. |
| **Single EF DbContext** | `db` is one shared `BHG_DRContext` instance created at startup — it is not thread-safe and cannot be used across parallel threads. |
| **Azure SQL connection limits** | A single Azure SQL tier has a limited connection pool. 80 simultaneous `SqlBulkCopy` operations would exhaust it. |
| **MERGE proc safety** | `stg.DartsSrvMerge` runs after bulk load. If two sites' data were mixed in staging, the MERGE would produce incorrect results in the year-partitioned destination tables. |

---

## How They Achieve Speed Despite Being Sequential

They run **multiple instances of `BHGTaskRunner.exe` simultaneously** — each instance owns a
different regional schedule. The parallelism is at the **process level by timezone/region**,
not at the site level.

```
Windows Task Scheduler — all fire at the same time
│
├── BHGTaskRunner.exe 2  →  Eastern ETL P1   (processes Eastern sites sequentially)
├── BHGTaskRunner.exe 2  →  Central ETL P1   (processes Central sites sequentially)
├── BHGTaskRunner.exe 2  →  Mountain ETL P1  (processes Mountain sites sequentially)
└── BHGTaskRunner.exe 2  →  Pacific ETL P1   (processes Pacific sites sequentially)
```

Each process handles its own time-zone group one site at a time.
Four regional processes run **in parallel with each other**, but **within each process sites are sequential**.

---

## Schedule 9 (DartsSrv) — Execution Flow Example

```
Eastern  BHGTaskRunner.exe 9:   B01 → B02 → B03 → B04 → ... (sequential)
Central  BHGTaskRunner.exe 9:   C01 → C02 → C03 → C04 → ... (sequential)
Mountain BHGTaskRunner.exe 9:   M01 → M02 → M03 → M04 → ... (sequential)
Pacific  BHGTaskRunner.exe 9:   P01 → P02 → P03 → P04 → ... (sequential)
         ↑___________________________________________________________↑
                     These 4 processes run in parallel
```

Total wall-clock time ≈ time to process the slowest region's sites end-to-end.

---

## Task Status Values in tsk.tbl_Tasks

| Status Code | Meaning |
|-------------|---------|
| `17` | Ready to run — picked up by scheduler |
| `18` | Currently running |
| `19` | Completed successfully |
| `20` | Failed / error |

---

## What This Means for Fabric Migration

In Fabric you can achieve **true site-level parallelism** that the current system cannot do,
because Delta Lake handles concurrent writes safely without a shared staging table.

### Current system bottleneck
```
80 sites × avg 3 min per site = 240 min (4 hours) per schedule
split across 4 regional processes = ~60 min wall-clock time
```

### Fabric potential with parallel notebooks
```python
# Fabric Pipeline — ForEach activity with parallelism enabled
# Each site = one isolated Notebook activity
# No shared staging table — Delta partitioned by SiteCode

For Each site in site_list (parallel = TRUE, batch_count = 20):
    → Notebook: extract_dartssrv(site_code=site)
        → Read from SAMMS source DB via JDBC
        → Write to Bronze Delta  (append, partition = SiteCode)
    → Notebook: merge_dartssrv(site_code=site)
        → Silver Delta MERGE on (SiteCode, DsId)
        → WHEN MATCHED AND RowChkSum <> source.RowChkSum THEN UPDATE
        → WHEN NOT MATCHED THEN INSERT
```

```
# Expected wall-clock time with 20 parallel notebooks:
80 sites ÷ 20 parallel = 4 batches × avg 3 min = ~12 min total
vs current ~60 min = 5× faster
```

### Key enablers in Fabric vs current system

| Constraint | Current System | Microsoft Fabric |
|------------|---------------|-----------------|
| Staging table conflicts | One shared `stg.tbl_dartssrv` — must be sequential | No staging needed — Delta handles concurrent writes |
| Thread safety | Single `BHG_DRContext` not thread-safe | Each notebook is an isolated Spark session |
| Connection pool | Azure SQL connection limit forces serialisation | JDBC connection per notebook — scales horizontally |
| MERGE safety | T-SQL MERGE proc reads from shared staging | Delta MERGE is row-level safe across parallel jobs |
| Parallelism granularity | Regional (4 processes) | Site-level (80 parallel notebooks) |

### Recommended Fabric pipeline design
```
Pipeline: ETL_DartsSrv
│
├── Activity: Get Active Sites
│   └── Lookup → ctrl.tbl_LocationCons WHERE IsActive = 1
│
├── Activity: ForEach Site  (parallel = TRUE, batchCount = 20)
│   └── Notebook: nb_dartssrv_extract_merge
│       Parameters: site_code, work_date, lookback_days
│       Steps:
│         1. JDBC read from SAMMS source with date filter
│         2. Append to Bronze Delta (onelake/bronze/dartssrv)
│         3. Delta MERGE into Silver (onelake/silver/dartssrv)
│
└── Activity: Log run summary → Gold audit table
```

---

*Analysis based on `BHGTaskRunner/Program.cs` lines 81–144 — outer/inner foreach loops and
task status management. Regional split confirmed at lines 33–69 (args switch case 1–11).*
