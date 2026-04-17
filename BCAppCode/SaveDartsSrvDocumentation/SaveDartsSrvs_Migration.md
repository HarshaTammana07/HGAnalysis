# DartsSrv ETL — Migration Reference: C# → Microsoft Fabric PySpark

**Source file:** `BHG-DR-LIB/SaveDartsSrvs.cs`  
**Target:** `SaveDartsSrvs_Fabric.py` — Microsoft Fabric Notebook (PySpark + Delta Lake)  
**ETL Schedule:** Schedule 9 (`BHGTaskRunner.exe 9`) — Counseling Sessions (DartsSrv)

---

## 1. Why This Migration Reduces Code

The C# file is **1,691 lines** containing **10 methods** (`SaveDartSrv2014` … `SaveDartSrv2023`) that are **byte-for-byte identical** in logic. The only difference between them is which EF Core DbSet they target:

| C# Method | EF Core DbSet | Azure SQL Table |
|---|---|---|
| `SaveDartSrv2014` | `db.TblDartsSrv` | `pats.tbl_DartsSrv` |
| `SaveDartSrv2015` | `db.TblDartsSrv2015` | `pats.tbl_DartsSrv_2015` |
| `SaveDartSrv2016` | `db.TblDartsSrv2016` | `pats.tbl_DartsSrv_2016` |
| … | … | … |
| `SaveDartSrv2023` | `db.TblDartsSrv2023` | `pats.tbl_DartsSrv_2023` |

The Python version replaces all 10 with **one parameterized function** `save_darts_srv(year, site_code, source_df)`.

**Line count comparison:**

| | C# | Python |
|---|---|---|
| Lines of code | ~1,691 | ~200 |
| Methods / functions | 10 (one per year) | 1 (generic, year-parameterized) |
| Year-specific logic | None (pure duplication) | `delta_table_name(year)` — 4 lines |
| Upsert engine | EF Core row-by-row `foreach` loop | Delta Lake `merge()` — set-based |

---

## 2. Architecture Comparison

### Old System (C# / BHGTaskRunner)

```
BHGTaskRunner.exe 9
  └── SelectConstructor (ActionKey=9)
        └── SQLSvrManager.GetDataTable()         ← ADO.NET read from SAMMS SQL Server
              └── SaveData.SaveDartSrv20XX()      ← EF Core upsert, one method per year
                    ├── db.TblDartsSrv20XX         ← LINQ WHERE to load existing Azure rows
                    │     .Where(x => x.SiteCode == sc).ToList()
                    ├── foreach row in DataTable   ← row-by-row loop (~N roundtrips)
                    │     ├── find existing by DsId in List<T>
                    │     ├── compare RowChkSum
                    │     └── set 50 fields manually
                    ├── db.TblDartsSrv20XX.AddRange(DSNew)
                    └── db.SaveChanges()            ← single batch INSERT for new rows
```

### New System (PySpark / Microsoft Fabric)

```
Fabric Pipeline / Notebook
  └── run_darts_srv_etl(work_date)
        └── spark.read.jdbc()                      ← reads all years in one JDBC pass
              └── for year in 2014..2023:
                    year_df = source_df.filter(year(DsDtStart) == year)
                    save_darts_srv(year, site_code, year_df)
                          └── DeltaTable.merge()    ← set-based MERGE
                                ├── WHEN MATCHED AND RowChkSum changed → UPDATE all cols
                                └── WHEN NOT MATCHED → INSERT
```

---

## 3. Concept-by-Concept Migration Map

### 3.1 Loading Existing Rows

**C# (per year, ~160 lines each):**
```csharp
List<Models.TblDartsSrv_2018> darts =
    db.TblDartsSrv2018
      .Where(x => x.SiteCode == sc)
      .ToList();

if (darts.Count == 0) { AllNewRows = true; }
```
**What it does:** Loads the entire year-table for one site into application memory as a C# List so the `foreach` can do in-memory lookups.

**Problem:** For large sites this loads tens of thousands of rows into memory on the ETL server and causes N+1 lookup patterns inside the `foreach`.

---

**Python equivalent:**
```python
delta_tbl = DeltaTable.forPath(spark, tbl_path)
```
**What it does:** Opens a reference to the Delta table. No data is loaded into memory. The MERGE operation runs entirely inside the Spark/Delta engine — the `WHERE RowChkSum <> source.RowChkSum` filter happens in the storage layer.

---

### 3.2 RowChkSum Upsert Loop

**C# (repeated in all 10 methods):**
```csharp
foreach (DataRow r in tbl.Rows)
{
    int myrcs = int.Parse(r["RowChkSum"].ToString());
    int ds    = int.Parse(r["dsid"].ToString());

    // find existing row by DsId
    dart = darts.Where(x => x.DsId == ds).FirstOrDefault();
    if (dart == null)
    {
        dart = new Models.TblDartsSrv_2018();
        dart.SiteCode = sc;
        dart.DsId     = ds;
        NewRow        = true;
    }

    // only write if checksum changed or new row
    if (dart.RowChkSum != myrcs || NewRow)
    {
        dart.RowChkSum  = myrcs;
        dart.DsClt      = int.Parse(r["DsClt"].ToString());
        dart.DsDim1     = bool.Parse(r["DsDim1"].ToString());
        // ... 48 more field assignments ...
        dart.LastModAt  = lastmod;
        if (NewRow) { DSNew.Add(dart); NewRow = false; }
    }
}
db.TblDartsSrv2018.AddRange(DSNew);
db.SaveChanges();
```
**What it does:** Row-by-row: for each source row, find the matching destination row by DsId in the in-memory List, compare RowChkSum, and either update or insert. Calls `db.SaveChanges()` once at the end.

**Problem:** O(N²) LINQ lookup inside the loop (`darts.Where(x => x.DsId == ds)`). All 50 field assignments are repeated verbatim in each of the 10 methods.

---

**Python equivalent:**
```python
delta_tbl.alias("target")
  .merge(
      incoming.alias("source"),
      "target.SiteCode = source.SiteCode AND target.DsId = source.DsId"
  )
  .whenMatchedUpdate(
      condition="target.RowChkSum <> source.RowChkSum",
      set={col: f"source.{col}" for col in all_cols}   # all 50 fields in one dict
  )
  .whenNotMatchedInsertAll()
  .execute()
```
**What it does:** The Delta engine evaluates the JOIN on `(SiteCode, DsId)` and the `RowChkSum` condition in parallel across all rows using the same predicate pushdown and file-skipping that Delta applies to all queries. No application-level loop. The `set` dict covers all 50 columns generically — no field listed individually.

---

### 3.3 Per-Year Method Routing

**C# — SelectConstructor routes by year:**
```csharp
// Inside SelectConstructor.cs — ActionKey=9 path
if (dsyr == 2018) SaveDartSrv2018(tbl, sc, akey, wrkdt, db);
if (dsyr == 2019) SaveDartSrv2019(tbl, sc, akey, wrkdt, db);
// etc.
```

**Python equivalent:**
```python
for year in range(2014, 2024):
    year_df = source_df.filter(F.year(F.col("DsDtStart")) == year)
    save_darts_srv(year, site_code, year_df)
```
No if/else chain. Year routing is a single `.filter()` call.

---

### 3.4 Table Name Resolution

**C# — implicit via EF Core DbSet property per method:**
```csharp
// SaveDartSrv2018:  db.TblDartsSrv2018.AddRange(DSNew)
// SaveDartSrv2019:  db.TblDartsSrv2019.AddRange(DSNew)
```

**Python equivalent:**
```python
def delta_table_name(year: int) -> str:
    if year == 2014:
        return "pats_tbl_DartsSrv"          # no year suffix for 2014
    return f"pats_tbl_DartsSrv_{year}"
```
4 lines replace 10 method-specific DbSet references.

---

### 3.5 Null/Empty Value Handling

**C# — guard on every single field (repeated 50× per method, 500× total):**
```csharp
if (r["DsClt"].ToString().Length > 0)         { dart.DsClt = int.Parse(r["DsClt"].ToString()); }
if (r["DsDtStart"].ToString().Length > 7)      { dart.DsDtStart = DateTime.Parse(r["DsDtStart"].ToString()); }
if (r["DsdblUnits"].ToString().Length > 0)     { dart.DsdblUnits = Double.Parse(r["DsdblUnits"].ToString()); }
```

**Python equivalent:**  
Spark JDBC reads SQL Server columns with their native nullability intact. No string-length guards needed. NULL columns arrive as Python `None`/Spark `null` automatically. If you need explicit coercion, use:
```python
incoming = incoming.withColumn("DsClt", F.col("DsClt").cast(IntegerType()))
```
But in practice, JDBC + Delta preserves types end-to-end without manual parsing.

---

### 3.6 Notes / Signature Columns (conditional block)

**C#:**
```csharp
if (tbl.Columns.Contains("dstxtnote"))
{
    dart.DstxtNote            = r["DstxtNote"].ToString();
    dart.DsRtbnote            = r["DsRtbnote"].ToString();
    dart.DsSigCltImg          = Encoding.ASCII.GetBytes(r["DsSigCltImg"].ToString());
    dart.DsSignatureCoSignImg = Encoding.ASCII.GetBytes(r["DsSignatureCoSignImg"].ToString());
    dart.DsSignatureImg       = Encoding.ASCII.GetBytes(r["DsSignatureImg"].ToString());
    // ...
}
```

**Python equivalent:**  
Controlled by the `include_notes` flag in `build_source_query()`. When `False`, the notes columns are simply not selected from SAMMS — they won't exist in the DataFrame and won't be merged into Delta. No `if col in df.columns` check needed:
```python
results = run_darts_srv_etl(work_date=datetime.today(), include_notes=True)
```

---

### 3.7 Connection Management

**C# — EF Core DbContext injected per call:**
```csharp
public bool SaveDartSrv2018(DataTable tbl, string sc, long akey,
                            DateTime wrkdt, Models.BHG_DRContext db)
{
    if (db == null) { db = new Models.BHG_DRContext(); }
    ...
    db.SaveChanges();
}
```

**Python equivalent:**  
No explicit connection management. Spark JDBC manages connection pooling. Delta Lake handles transactional writes. The Fabric Lakehouse connection is established once when `SparkSession` starts.

---

## 4. Column Mapping Reference

All 50 source → destination column mappings are identical between old and new systems. The table below shows the SAMMS source column, its C# type cast, and the Delta/Fabric column.

| SAMMS Source Col | C# Cast | Spark/Delta Type | Notes |
|---|---|---|---|
| `dsid` | `int.Parse()` | `IntegerType` | Primary key component |
| `RowChkSum` | `int.Parse()` | `IntegerType` | Change detection key |
| `DsClt` | `int.Parse()` | `IntegerType` | Client ID |
| `DsDim1`–`DsDim6` | `bool.Parse()` | `BooleanType` | Dimension flags |
| `DsTxtSrv` | `.ToString()` | `StringType` | Service text |
| `DsDtStart` | `DateTime.Parse()` | `TimestampType` | Session start — drives year partition |
| `DsDtEnd` | `DateTime.Parse()` | `TimestampType` | Session end |
| `DsTxtType` | `.ToString()` | `StringType` | Service type |
| `DsdblUnits` | `Double.Parse()` | `DoubleType` | Units |
| `DsNoteId` | `int.Parse()` | `IntegerType` | Linked note |
| `DsDtAdded` | `DateTime.Parse()` | `TimestampType` | Date added |
| `DstxtStaff` | `.ToString()` | `StringType` | Staff name |
| `Dsbilled` | `DateTime.Parse()` | `TimestampType` | Billing date |
| `DsGroupnum` | `.ToString()` | `StringType` | Group number |
| `dsPROGRAM` | `.ToString()` | `StringType` | Program (renamed `DsProgram`) |
| `DsUpdate` | `DateTime.Parse()` | `TimestampType` | Last update date |
| `DsUpdatestaff` | `.ToString()` | `StringType` | Update staff |
| `DsInvalidatedOn` | `DateTime.Parse()` | `TimestampType` | Invalidation date |
| `DsError` | `.ToString()` | `StringType` | Error flag |
| `DsTxtHiv` | `.ToString()` | `StringType` | HIV text |
| `DsDartsGroup` | `int.Parse()` | `IntegerType` | DARTS group |
| `RepOldSrv` | `decimal.Parse()` | `DecimalType` | Rep old service |
| `DsSigDate` | `DateTime.Parse()` | `TimestampType` | Signature date |
| `DssigdateCosign` | `DateTime.Parse()` | `TimestampType` | Co-sign date |
| `DsSigUser` | `.ToString()` | `StringType` | Signing user |
| `DsSigUserCosign` | `.ToString()` | `StringType` | Co-signing user |
| `DsSigcltdate` | `DateTime.Parse()` | `TimestampType` | Client sig date |
| `DsAptid` | `int.Parse()` | `IntegerType` | Appointment ID |
| `Dsuncharted` | `bool.Parse()` | `BooleanType` | Uncharted flag |
| `DsTxDim1`–`DsTxDim6` | `int.Parse()` | `IntegerType` | Tx dimension scores |
| `DsDiag` | `.ToString()` | `StringType` | ICD-9 diagnosis |
| `DsDiag10` | `.ToString()` | `StringType` | ICD-10 diagnosis |
| `DsArea` | `.ToString()` | `StringType` | Area |
| `DsGroupDefaultNote` | `bool.Parse()` | `BooleanType` | Group default note |
| `DsGroupEnd` | `DateTime.Parse()` | `TimestampType` | Group end date |
| `DsGroupIdentity` | `int.Parse()` | `IntegerType` | Group identity |
| `DsGroupStart` | `DateTime.Parse()` | `TimestampType` | Group start date |
| `SiteId` | `int.Parse()` | `IntegerType` | Site ID (from source) |
| `DsDbnotes` | `.ToString()` | `StringType` | DB notes |
| `Mg` | `Double.Parse()` | `DoubleType` | Mg value |
| `DstxtNote` *(notes)* | `.ToString()` | `StringType` | Note text |
| `DsRtbnote` *(notes)* | `.ToString()` | `StringType` | RTB note |
| `DsSigclt` *(notes)* | `.ToString()` | `StringType` | Client signature |
| `DsSignature` *(notes)* | `.ToString()` | `StringType` | Staff signature |
| `DssignatureCosign` *(notes)* | `.ToString()` | `StringType` | Co-sign signature |
| `DsSigcltuser` *(notes)* | `.ToString()` | `StringType` | Client sig user |
| `DsSigCltImg` *(notes)* | `Encoding.ASCII.GetBytes()` | `BinaryType` | Client sig image |
| `DsSignatureCoSignImg` *(notes)* | `Encoding.ASCII.GetBytes()` | `BinaryType` | Co-sign image |
| `DsSignatureImg` *(notes)* | `Encoding.ASCII.GetBytes()` | `BinaryType` | Staff sig image |
| `SiteCode` | injected from `sc` param | `StringType` | Added in Python via `F.lit(site_code)` |
| `LastModAt` | `DateTime.Now` | `TimestampType` | Added in Python via `F.lit(last_mod)` |

---

## 5. Fabric Lakehouse Setup

### Delta Table Names

| C# EF Core DbSet | Fabric Delta Table | Path |
|---|---|---|
| `db.TblDartsSrv` | `pats_tbl_DartsSrv` | `Tables/pats_tbl_DartsSrv` |
| `db.TblDartsSrv2015` | `pats_tbl_DartsSrv_2015` | `Tables/pats_tbl_DartsSrv_2015` |
| … | … | … |
| `db.TblDartsSrv2023` | `pats_tbl_DartsSrv_2023` | `Tables/pats_tbl_DartsSrv_2023` |

Tables are auto-created by `save_darts_srv()` on first run if they do not exist.

### Control Table

The Fabric notebook reads `ctrl_tbl_LocationCons` (replicated from Azure SQL `ctrl.tbl_LocationCons`) to get the list of active SAMMS clinic connection strings. This is the same control table BHGTaskRunner reads.

### Notebook Environment Requirements

- Runtime: Fabric Spark (Runtime 1.2+, Spark 3.4+)
- Libraries: `delta-spark` (pre-installed in Fabric), `pyodbc` or `mssql-jdbc` driver
- Key Vault: Store SAMMS connection strings as Fabric Environment secrets or Azure Key Vault references

---

## 6. Running the Notebook

### Daily incremental run (equivalent to BHGTaskRunner.exe 9):
```python
from SaveDartsSrvs_Fabric import run_darts_srv_etl
from datetime import datetime

results = run_darts_srv_etl(work_date=datetime.today())
```

### Historical backfill for a specific year + site:
```python
from SaveDartsSrvs_Fabric import run_darts_srv_for_site

run_darts_srv_for_site(
    site_code="ABC",
    jdbc_url="jdbc:sqlserver://...",
    jdbc_user="...",
    jdbc_password="...",
    work_date=datetime(2022, 1, 1),
    years=[2022]             # backfill only 2022 partition
)
```

### Notes-inclusive run (pulls DstxtNote, DsSignature, etc.):
```python
results = run_darts_srv_etl(
    work_date=datetime.today(),
    include_notes=True
)
```

---

## 7. Key Improvements Summary

| Aspect | C# BHGTaskRunner | Python Fabric Notebook |
|---|---|---|
| Lines of code | ~1,691 | ~200 |
| Methods | 10 (one per year) | 1 generic function |
| Upsert mechanism | EF Core row-by-row foreach | Delta Lake set-based MERGE |
| Null handling | 50× manual string-length guards | Native Spark null semantics |
| Memory usage | Loads full year-table per site into app memory | No application-level data load |
| Year routing | `if (dsyr == 20XX)` chain in SelectConstructor | `filter(year(DsDtStart) == year)` |
| New year support | Add a new 160-line method + new EF Core model | Update `range(2014, 2025)` in one place |
| Parallelism | Single-threaded per site | Spark distributed across all sites |
| Change detection | RowChkSum compared in C# foreach | RowChkSum compared in Delta MERGE predicate |
| Source read | ADO.NET `SqlDataAdapter.Fill(DataTable)` | `spark.read.jdbc()` with predicate pushdown |
