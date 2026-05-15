# EF Core vs SqlBulkCopy — Complete Reference Guide
### Why Two Patterns Exist, How They Work, and Every Table Mapped to Its Pattern

---

## PART 1 — WHY TWO DIFFERENT PATTERNS EXIST

This is the most important thing to understand before migrating. The developers did not randomly choose between EF Core and SqlBulkCopy. There is a deliberate engineering reason for each choice.

---

### Why EF Core Row-by-Row Was Used

EF Core is used when:

1. **The table is small-to-medium volume.** Enrollment, assessments, appointments, billing — these tables have maybe a few hundred to a few thousand modified rows per site per day. Loading the entire site slice into a C# `List<T>` and scanning it is feasible at this scale.

2. **Per-row transformation logic exists in C#.** Orders are split by `OrderDate.Year` into separate year tables in C#. Claims for certain sites have special date-window logic. This branching is easier to maintain in C# than in a stored procedure.

3. **`RowChkSum` comparison is done in C# memory.** The pattern is:
   - Load new rows from SAMMS source into a `DataTable`.
   - Load ALL existing rows for this site from Azure into a `List<T>` (EF).
   - For each source row: find the match by primary key in the list using LINQ.
   - Compare `RowChkSum`. If different → update. If not found → insert. If same → skip.
   - Call `db.SaveChanges()` once at the end.
   
   This approach means **only truly changed rows are written to Azure**, minimising write I/O.

4. **No staging schema counterpart exists for the table.** Only the high-volume tables have `stg.tbl_xxx` staging tables in Azure BHG_DR.

**The performance cost of EF Core:** Loading the entire Azure site slice into C# memory is O(N) on RAM. The inner `LINQ.Where` loop inside each `foreach` over source rows is **O(N²)** — for a site with 50,000 rows, that is 2.5 billion comparisons in the worst case. This is the biggest performance bottleneck in the whole system and a key motivation for replacing it with Delta MERGE in Fabric.

---

### Why SqlBulkCopy + Stored Procedure MERGE Was Used

Bulk is used when:

1. **The table is high volume.** DartsSrv sessions, daily medication doses, claim lines — a single active site can have 100,000+ rows in these tables. Loading all of that into a C# `List<T>` would require gigabytes of RAM and the O(N²) LINQ loop would time out.

2. **The MERGE can be done SET-BASED inside SQL Server.** The stored procedure receives all staged rows and runs a single SQL `MERGE INTO pats.tbl_xxx USING stg.tbl_xxx ON (key match) WHEN MATCHED AND RowChkSum differs THEN UPDATE WHEN NOT MATCHED THEN INSERT`. SQL Server handles this as a set operation — orders of magnitude faster than a C# loop.

3. **The staging table is a known schema mirror.** These tables have dedicated `stg.tbl_xxx` tables in Azure with matching columns. The bulk copy writes into staging, the MERGE proc promotes to the final `pats.` table, then staging is truncated clean.

4. **The incremental logic (RowChkSum comparison) is inside the MERGE proc**, not in C#. The C# code just moves the data; the intelligence about what changed lives in SQL Server.

**The flow for every Bulk table:**
```
SAMMS source DB
      │
      │  ADO.NET SqlDataReader (SELECT with WHERE date filter)
      ▼
C# DataTable (in memory, only the filtered incremental slice)
      │
      │  TRUNCATE stg.tbl_xxx
      │  SqlBulkCopy.WriteToServer(DataTable)
      ▼
Azure BHG_DR  stg.tbl_xxx  (staging — all rows land here)
      │
      │  EXEC stg.XxxMerge (SQL Server MERGE proc)
      ▼
Azure BHG_DR  pats.tbl_xxx  (final destination — only changed rows updated)
      │
      │  TRUNCATE stg.tbl_xxx  (cleanup)
      ▼
Done
```

---

### The Fundamental Decision Criterion

| Question | Answer → Use |
|---|---|
| Can a single site have >10,000 rows in this table? | SqlBulkCopy |
| Does the table need per-row C# transformations (year routing, column stripping)? | EF Core |
| Does a `stg.tbl_xxx` staging table exist in Azure for this table? | SqlBulkCopy |
| Is this a reference/lookup/small master table? | EF Core |
| Is this a daily transactional table (doses, sessions, claims)? | SqlBulkCopy |

---

## PART 2 — COMPLETE TABLE INVENTORY: SQLBULKCOPY TABLES

These 13 logical destinations use the SqlBulkCopy + Stored Procedure MERGE path as their **primary production load**.

---

### BULK TABLE 1 — DartsSrv (Counseling Sessions)

| Property | Value |
|---|---|
| **Source** | All 80+ SAMMS clinic DBs |
| **Source Table** | `dbo.tblDartsSrv` |
| **Staging Table** | `stg.tbl_dartssrv` |
| **Final Destination** | `pats.tbl_DartsSrv` (view spanning year-partitioned tables) |
| **Year-partitioned tables** | `pats.tbl_DartsSrv_2014B4`, `_2015`, `_2016`, `_2017`, `_2018`, `_2019`, `_2020`, `_2021`, `_2022`, `_2023` |
| **Merge Stored Procedures** | `stg.DartsSrvMerge` (pre-2022), `stg.DartsSrvMerge22` … `stg.DartsSrvMerge28` (one per year) |
| **Schedule** | `BHGTaskRunner.exe 9` (SAMMS-ETL-DartSvc) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_dartssrv", ...)` |
| **Why Bulk?** | Highest-volume transactional table. A single site can have 200K+ rows. Year-partitioned merge means 8 separate MERGE procs run in sequence after bulk load. |
| **Secondary EF path** | `SaveDartsSrvs.cs` — `SaveDartSrv2014` through `SaveDartSrv2023`. Used for backfill / year-specific reloads only. See PART 3. |

**Special rules for DartsSrv:**
- `ServiceType` column stripped from SELECT if the column does not exist in the source SAMMS DB (older schema sites).
- Dynamic lookback: -15 days normally, -90 days on month-end Fridays, -200 days on specific special dates.
- WHERE clause checks 5 date columns: `dsdtstart`, `dsDtAdded`, `dsUpdate`, `dsBilled`, `dsSigDate`. Also includes `dsClt <= 0` rows.

---

### BULK TABLE 2 — Dose (Medication Dispensing)

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source Table** | `dbo.tblDose` |
| **Staging Table** | `stg.tbl_dose` |
| **Final Destination** | `pats.tbl_Dose` |
| **Merge Stored Procedure** | `stg.DoseMerge` |
| **Schedule** | `BHGTaskRunner.exe 10` (SAMMS-ETL-Dose) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_dose", ...)` |
| **Why Bulk?** | Daily medication records — one row per patient per dose date. Very high volume for active sites. |

**Exceptions — these specific sites use EF Core `SaveDoses` instead of Bulk:**

| Site Code | Reason |
|---|---|
| `V10A` | Uses narrower date window and EF upsert |
| `CBCO` | Uses narrower date window and EF upsert |
| `V21` | Uses narrower date window and EF upsert |
| `V10` | Uses narrower date window and EF upsert |

On **reload** (`st.Reload == true`) for all other sites: Azure rows for the site are deleted first, then SqlBulkCopy loads ALL historical dose records.

---

### BULK TABLE 3 — DoseExcuse

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source Table** | `dbo.tblDoseExcuse` (or equivalent) |
| **Staging Table** | `stg.tbl_dose_excuse` (staging proc defined in BulkDartsSvc) |
| **Final Destination** | `pats.tbl_Dose_Excuse` |
| **Merge Stored Procedure** | `stg.Dose_ExcuseMerge` |
| **Schedule** | `BHGTaskRunner.exe 10` (SAMMS-ETL-Dose) |
| **Load method in code** | Currently `sd.SaveDoseExcuse(...)` — **EF Core** (the Bulk line is commented out in Program.cs line 966) |
| **Why currently EF?** | The Bulk path was written and then commented out (`//rCodes = bldr.BulkDartsSrvLoader(...)`). The `stg.Dose_ExcuseMerge` proc exists in BulkDartsSvc switch but is reached via the other Bulk tables. Active load uses EF `SaveDoseExcuse`. |

> **Migration note:** DoseExcuse has a Bulk infrastructure ready (`stg.tbl_dose_excuse`, `stg.Dose_ExcuseMerge`) but is currently running EF. In Fabric, treat it the same as other Dose tables — Delta MERGE.

---

### BULK TABLE 4 — Claims

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source Table** | `dbo.tbl3pClaim` |
| **Staging Table** | `stg.tbl_claims` |
| **Final Destination** | `pats.tbl_Claims` |
| **Merge Stored Procedure** | `stg.ClaimsMerge` |
| **Schedule** | `BHGTaskRunner.exe 4` (Regional ETL P2) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claims", ...)` for most sites |
| **Why Bulk?** | High claim volume for active billing sites. |

**Exceptions — these 4 sites use EF Core `SaveClaims` instead of Bulk:**
`VBRA`, `VMIN`, `VWBY`, `VBRP`

---

### BULK TABLE 5 — ClaimLineItem

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source Table** | `dbo.tbl3pClaimLineItem` |
| **Staging Table** | `stg.tbl_claimlineitem` |
| **Final Destination** | `pats.tbl_ClaimLineItem` |
| **Merge Stored Procedure** | `stg.ClaimLineItemMerge` |
| **Schedule** | `BHGTaskRunner.exe 4` (Regional ETL P2) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claimlineitem", ...)` |
| **Why Bulk?** | One line per service within a claim — very high row count. |

---

### BULK TABLE 6 — ClaimLineItemActivity

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source Table** | `dbo.tbl3pClaimLineItemActivity` |
| **Staging Table** | `stg.tbl_claimlineitemactivity` |
| **Final Destination** | `pats.tbl_ClaimLineItemActivity` |
| **Merge Stored Procedure** | `stg.ClaimLineItemActivityMerge` |
| **Schedule** | `BHGTaskRunner.exe 4` (Regional ETL P2) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claimlineitemactivity", ...)` |
| **Why Bulk?** | Payment/adjustment/denial events — one row per financial event per claim line. |

---

### BULK TABLE 7 — ClientDemo (Core Demographics)

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source Table** | `dbo.tblClient` |
| **Staging Table** | `stg.clientdemo` |
| **Final Destination** | `pats.tbl_ClientDemo1` + `pats.tbl_ClientDemo2` |
| **Merge Stored Procedures** | `stg.ClientDemoMerge1` + `stg.ClientDemoMerge2` (two procs, one per dest table) |
| **Schedule** | `BHGTaskRunner.exe 2` (Regional ETL P1) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.clientdemo", ...)` |
| **Why Bulk?** | The patient master table — every active patient for a site is re-sent on each run. Large sites have 10,000+ patients. |

**Important:** The SELECT for `stg.clientdemo` is **hardcoded** in Program.cs (not from `dms.tbl_MapSrc2Dsn`). It names every column explicitly including an inline `RowChkSum = CHECKSUM(...)` expression. This is the only table where the RowChkSum is computed inline in the SELECT string in C# code rather than by SelectConstructor.

**Secondary EF path:** `pats.tbl_clientdemo1` also has a separate EF path (`sd.SaveClientDemo1var`) used for incremental updates outside the full-refresh Bulk cycle.

---

### BULK TABLE 8 — FormsSAMMSClient

| Property | Value |
|---|---|
| **Source** | `SAMMSGLOBAL` database (not per-clinic SAMMS) |
| **Staging Table** | `stg.tbl_formssammsclient` |
| **Final Destination** | `pats.tbl_FormsSAMMSClient` |
| **Merge Stored Procedure** | `stg.FormsSAMMSMerge` (BHG sites) / `stg.FormsSAMMSMergePHC` (PHC) |
| **Schedule** | `BHGTaskRunner.exe 6` (Samms-Forms) and `BHGTaskRunner.exe 1` (SAMMSGlobal) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_formssammsclient", ...)` |
| **Why Bulk?** | Global form-to-client linkage table — tens of thousands of rows across all sites in a single SAMMSGLOBAL query. |

**Special:** After WriteToServer, an UPDATE runs to set `SiteCode` by joining `ctrl.tbl_Locations`. The destination table is NOT truncated before bulk (unlike most Bulk tables).

---

### BULK TABLE 9 — LiquidLog (Liquid Methadone Dispensing)

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Staging Table** | `stg.tbl_liquidlog` |
| **Final Destination** | `pats.tbl_LiquidLog` |
| **Merge Stored Procedure** | `stg.sp_liquidlog_Merge` |
| **Schedule** | `BHGTaskRunner.exe 8` (SAMMS-ETL-INV) |
| **Load method in code** | Inline `SqlBulkCopy` in Program.cs (NOT via `BulkDartsSvc` class — has its own inline code block at line ~463) |
| **Why Bulk?** | High-frequency dispensing records for methadone — one record per patient per day. |

**Hybrid behaviour:**
- `Reload == true` → Bulk path (truncate staging, bulk copy, call merge proc).
- `Reload == false` → EF path: `sd.SaveLiquidlog(SrcDt, ...)` incremental EF upsert.

---

### BULK TABLE 10 — UAResultDetail (Urine Analysis Detail)

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Staging Table** | `stg.tbl_uaresultdetail` |
| **Final Destination** | `pats.tbl_UAResultDetail` |
| **Merge Stored Procedure** | `stg.UAResultDetailMerge` |
| **Schedule** | `BHGTaskRunner.exe 5` (Samms-LAB) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_uaresultdetail", ...)` |
| **Why Bulk?** | Per-substance detail for every UA test — typically 5–15 rows per test result. High row count. |

---

### BULK TABLE 11 — LABResultDetail (External Lab Detail)

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Staging Table** | `stg.tbl_labresultdetail` |
| **Final Destination** | `pats.tbl_LabResultDetail` |
| **Merge Stored Procedure** | `stg.LABResultDetailMerge` |
| **Schedule** | `BHGTaskRunner.exe 5` (Samms-LAB) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_labresultdetail", ...)` |
| **Why Bulk?** | External lab panel results (LabCorp/Quest) — one row per lab analyte per result. |

---

### BULK TABLE 12 — BillSub (Bill Submission — vw3pBillSub)

| Property | Value |
|---|---|
| **Source** | All SAMMS clinic DBs |
| **Source View** | `dbo.vw3pBillSub` |
| **Staging Table** | `stg.tbl_vw3pbillsub` |
| **Final Destination** | `pats.tbl_vw3pBillSub` |
| **Merge Stored Procedure** | `stg.sp_BillSubMerge` |
| **Schedule** | `BHGTaskRunner.exe 8` (SAMMS-ETL-INV) |
| **Load method in code** | `bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_vw3pbillsub", ...)` |
| **Why Bulk?** | Bill submission linkage records — high volume for billing-active sites. |

**Note:** SELECT applies column-level transforms before bulk: `isnull([CptMod], ':(')`, `isnull([pySUBSID], ':(')`, `isnull(charge, 0)`. The null-replacement happens in the SELECT string in C#.

---

### BULK TABLE 13 — FormQA (Form Question Answers — selected sites only)

| Property | Value |
|---|---|
| **Source** | SAMMS clinic DBs (specific sites only) |
| **Staging Table** | `stg.tbl_FormQA` |
| **Final Destination** | `pats.tbl_dbo_FormQuestionAnswers` + `pats.tbl_BAMForm` (via `pats.BAMMerge`) |
| **Merge Stored Procedures** | `stg.sp_FormQA_Merge` → then `pats.BAMMerge` |
| **Schedule** | `BHGTaskRunner.exe 6` (Samms-Forms) |
| **Load method in code** | Inline `SqlBulkCopy` in Program.cs at line ~2159 |
| **Why Bulk for these sites?** | High form volume at specific large clinics. |

**Sites that use Bulk for FormQA:**
`B37`, `DM`, `GAL`, `HGT`, `LV1`, `NC`, `PH`, `D07`, `B26`, `B24`, `DRD-SF`, `V12`, `B35`, `B25`, `V9`, `FW`, `LO`, `B42`

**All other sites** use EF Core `sd.SaveFormQuestionAnswers(...)`.

---

## PART 3 — COMPLETE TABLE INVENTORY: EF CORE TABLES

All tables below use the EF Core row-by-row upsert pattern as their primary production load.
The C# flow for each is: ADO.NET SELECT from SAMMS → `DataTable` → call `Save*` method → EF loads Azure slice → LINQ loop → `db.SaveChanges()`.

---

### GROUP A — Counseling Sessions (EF Backfill Path)

These are the **same destination tables** as BULK TABLE 1 (DartsSrv), but reached via a different code path for backfills and historical reloads.

| Method | Destination Table | Notes |
|---|---|---|
| `SaveDartSrv2014` | `pats.tbl_DartsSrv_2014B4` | Sessions 2008–2014 |
| `SaveDartSrv2015` | `pats.tbl_DartsSrv_2015` | 2015 sessions |
| `SaveDartSrv2016` | `pats.tbl_DartsSrv_2016` | 2016 sessions |
| `SaveDartSrv2017` | `pats.tbl_DartsSrv_2017` | 2017 sessions |
| `SaveDartSrv2018` | `pats.tbl_DartsSrv_2018` | 2018 sessions |
| `SaveDartSrv2019` | `pats.tbl_DartsSrv_2019` | 2019 sessions |
| `SaveDartSrv2020` | `pats.tbl_DartsSrv_2020` | 2020 sessions |
| `SaveDartSrv2021` | `pats.tbl_DartsSrv_2021` | 2021 sessions |
| `SaveDartSrv2022` | `pats.tbl_DartsSrv_2022` | 2022 sessions |
| `SaveDartSrv2023` | `pats.tbl_DartsSrv_2023` | 2023 sessions |

**When is EF used for DartsSrv?** When `SelectConstructor` triggers a year-specific ActionStepKey for backfill. The daily incremental run always goes through Bulk.

---

### GROUP B — Prescription Orders

Year-partitioned in C# — the `pats.tbl_Orders` destination is chosen based on `OrderDate.Year`.

| Method | Destination Table | Notes |
|---|---|---|
| `SaveOrders` | `pats.tbl_Orders` | Base / pre-2016 orders |
| `SaveOrders2016` | `pats.tbl_Orders_2016` | |
| `SaveOrders2017` | `pats.tbl_Orders_2017` | |
| `SaveOrders2018` | `pats.tbl_Orders_2018` | |
| `SaveOrders2019` | `pats.tbl_Orders_2019` | |
| `SaveOrders2020` | `pats.tbl_Orders_2020` | |
| `SaveOrders2021` | `pats.tbl_Orders_2021` | |
| `SaveOrders2022` | `pats.tbl_Orders_2022` | |
| `SaveOrders2023` | `pats.tbl_Orders_2023` | |
| `SaveOrders2024` | `pats.tbl_Orders_2024` | |
| `SaveOrders2025` | `pats.tbl_Orders_2025` | |
| `SaveOrders2026` | `pats.tbl_Orders_2026` | |
| `SaveOrders2027` | `pats.tbl_Orders_2027` | |
| `SaveOrders2028` | `pats.tbl_Orders_2028` | Future-proofed |

**Schedule:** `BHGTaskRunner.exe 11` (SAMMS-ETL-Orders)  
**Why EF?** A single ADO.NET query fetches all orders across all years, then C# splits them by year and routes to the correct `SaveOrders20XX` method. This year-routing logic is easier in C# than in a stored procedure.

---

### GROUP C — Billing Records

| Method | Destination Table | Schedule | Notes |
|---|---|---|---|
| `SaveBills` | `pats.tbl_Bills` | Regional ETL P2 (arg=4) | Standard billing — `DaysBack` controls lookback |
| `SaveAuthBills` | `pats.tbl_AuthBills` | Regional ETL P2 (arg=4) | Authorization-linked billing |

---

### GROUP D — Insurance Claims (EF Exception Sites)

| Method | Destination Table | Schedule | Exception Sites |
|---|---|---|---|
| `SaveClaims` | `pats.tbl_Claims` | Regional ETL P2 (arg=4) | Only `VBRA`, `VMIN`, `VWBY`, `VBRP` — all others use Bulk |
| `SaveClaimLineItem` | `pats.tbl_ClaimLineItem` | Regional ETL P2 | — |
| `SaveClaimLineItemActivity` | `pats.tbl_ClaimLineItemActivity` | Regional ETL P2 | — |
| `CleanupDeletedData` | Various (passed as param) | Regional ETL P2 | Removes rows deleted at source |

---

### GROUP E — Authorizations

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveAuths` | `pats.tbl_Auths` | Regional ETL P2 (arg=4) |
| `SaveAuthBillsub` | `pats.tbl_vw3pBillSub` | Regional ETL P2 (arg=4) |

---

### GROUP F — 3rd Party Eligibility

| Method | Destination Table | Schedule |
|---|---|---|
| `Save3pElig` | `pats.tbl_3pElig` | Regional ETL P2 (arg=4) |
| `Save3pSetup` | `pats.tbl_3pSetup` | Regional ETL P2 (arg=4) |
| `Save3pClaimNote` | `pats.tbl_3pClaimNote` | Regional ETL P2 (arg=4) |
| `Save3pArnote` | `pats.tbl_3pArnote` | Regional ETL P2 (arg=4) |

---

### GROUP G — Client Demographics (EF Incremental Path)

These run alongside the Bulk ClientDemo path. They handle columns not in the Bulk set, or are triggered for incremental updates rather than full refresh.

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveClientDemo1var` | `pats.tbl_ClientDemo1` | Regional ETL P1 (arg=2) |
| `SaveClientDemo1` | `pats.tbl_ClientDemo1` | Regional ETL P1 (arg=2) |
| `SaveClientDemo2` | `pats.tbl_ClientDemo2` | Regional ETL P1 (arg=2) |
| `SaveClientDemo3` | `pats.tbl_ClientDemo3` | Regional ETL P1 (arg=2) |

---

### GROUP H — Medication / Dose (EF Exception Sites + DoseExcuse)

| Method | Destination Table | Schedule | Notes |
|---|---|---|---|
| `SaveDoses` | `pats.tbl_Dose` | arg=10 | Only for sites `V10A`, `CBCO`, `V21`, `V10` — all others use Bulk |
| `SaveDoseExcuse` | `pats.tbl_Dose_Excuse` | arg=10 | Currently EF for all sites (Bulk was commented out) |

---

### GROUP I — UA / Lab Results

| Method | Destination Table | Schedule | Notes |
|---|---|---|---|
| `SaveUAResults` | `pats.tbl_UAResult` | arg=5 (Samms-LAB) | In-clinic drug screen headers — EF |
| `SaveUAResultDetail` | `pats.tbl_UAResultDetail` | arg=5 | **Bulk** (see PART 2, Table 10) |
| `SaveUASched` | `pats.tbl_UASched` | arg=5 | UA schedule — EF |
| `SaveLABResults` | `pats.tbl_LabResult` | arg=5 | External lab headers — EF (`SiteCode != "LAB"` guard) |
| `SaveUAResultDetail` (EF path) | `pats.tbl_UAResultDetail` | arg=5 | EF version commented out but exists in code |
| `LABResultDetail` | `pats.tbl_LabResultDetail` | arg=5 | **Bulk** (see PART 2, Table 11) |

---

### GROUP J — Admission Assessments (ASAM)

All from `SaveAssessments.cs`. Schedule: `BHGTaskRunner.exe 8` (SAMMS-ETL-INV).

| Method | Destination Table |
|---|---|
| `SaveAdmissionAssessment` | `pats.Tbl_AdmissionAssessment` |
| `SaveAdmissionAssessmentSummary` | `pats.Tbl_AdmissionAssessmentSummary` |
| `SaveAdmissionAssessmentDimensionFour` | `pats.Tbl_AdmissionAssessmentDimensionFour` |
| `SaveAdmissionAssessmentDimensionOneDisorder` | `pats.Tbl_AdmissionAssessmentDimensionOneDisorder` |
| `SaveAdmissionAssessmentDimensionTwo` | `pats.Tbl_AdmissionAssessmentDimensionTwo` |
| `SaveAdmissionAssessmentDimensionThree` | `pats.Tbl_AdmissionAssessmentDimensionThree` |
| `SaveAdmissionAssessmentDimensionFiveSubstanceUse` | `pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse` |
| `SaveAdmissionAssessmentDimensionSix` | `pats.tbl_AdmissionAssessmentDimensionSix` |
| `SaveAdmissionAssessmentSubstanceuseHistory` | `pats.tbl_AdmissionAssessmentSubstanceuseHistory` |

---

### GROUP K — Reassessments

All from `SaveAssessments.cs`. Schedule: `BHGTaskRunner.exe 8`.

| Method | Destination Table |
|---|---|
| `SaveReAssessment` | `pats.Tbl_ReAssessment` |
| `SaveReAssessmentOccupational` | `pats.Tbl_ReAssessmentOccupational` |
| `SaveReAssessmentFamily` | `pats.Tbl_ReAssessmentFamily` |
| `SaveReAssessmentLegal` | `pats.Tbl_ReAssessmentLegal` |
| `SaveReAssessmentMentalHealth` | `pats.Tbl_ReAssessmentMentalHealth` |
| `SaveReAssessmentPhysicalHealth` | `pats.Tbl_ReAssessmentPhysicalHealth` |
| `SaveReAssessmentSubstanceUse` | `pats.Tbl_ReAssessmentSubstanceUse` |
| `SaveReAssessmentSocial` | `pats.Tbl_ReAssessmentSocial` |
| `SaveReAssessmentTreatment` | `pats.Tbl_ReAssessmentTreatment` |
| `SaveAssessmentSubstanceuseHistory` | `pats.tbl_AssessmentSubstanceuseHistory` |

---

### GROUP L — Pre-Admission

| Method | Destination Table | Schedule |
|---|---|---|
| `SavePreAdmissionV6` | `ayx.tbl_PreAdmission_V6` | Regional ETL P1 (arg=2) |
| `SavePreAdminReferrals` | `pats.tbl_PreAdmission_Referrals` | Regional ETL P1 (arg=2) |

---

### GROUP M — Pre-Admission (PA Data)

All from `SavePAData.cs`. Schedule: `BHGTaskRunner.exe 8`.

| Method | Destination Table |
|---|---|
| `SavePA` | `pats.tbl_PA` |
| `SavePACounselorReview` | `pats.tbl_PACounselorReview` |
| `SaveFinancialHardshipApplication` | `pats.tbl_FinancialHardshipApplication` |
| `SavedropDownListItems` | `pats.tbl_dropDownListItems` |
| `SavePADimension1` | `pats.tbl_PADimension1` |
| `SavePADimension2` | `pats.tbl_PADimension2` |
| `SavePADimension3` | `pats.tbl_PADimension3` |
| `SavePADimension4` | `pats.tbl_PADimension4` |
| `SavePADimension5` | `pats.tbl_PADimension5` |
| `SavePADimension6` | `pats.tbl_PADimension6` |

---

### GROUP N — Global Reference Data

All from `SaveGlobal.cs`. Schedule: `BHGTaskRunner.exe 1` (SAMMSGlobal). Source is often `SAMMSGLOBAL` or a single site query, not a per-clinic loop.

| Method | Destination Table |
|---|---|
| `SaveFeeSchedules` | `pats.tbl_FeeSched` |
| `SaveGlobalPayer` | `pats.tbl_GlobalPayor` |
| `SaveGlobalUser` | `pats.tbl_GlobalUser` |
| `SaveGlobalUserSite` | `pats.tbl_GlobalUserSite` |
| `SaveGlobalClinicalOpiateWithdrawalScale` | `pats.tbl_ClinicalOpiateWithdrawalScale` |
| `SaveGlobalFormsSAMMSClients` | `pats.tbl_FormsSAMMSClient` (via EF — the older path, now superseded by Bulk) |
| `SaveGlobalConsents` | `pats.tbl_Consents` |
| `SaveGlobalConsentsPhc` | `pats.tbl_Consents` (PHC-specific handling) |
| `SaveGlobalDevices` | `pats.tbl_Devices` |
| `SaveBAM` | `pats.tbl_BriefAddictionMonitor` |
| `SaveServices` | `pats.tbl_Services` |
| `SaveFormCounts` | `stg.tbl_FormsCounts` |
| `SaveClaimStatus` | `pats.tbl_ClaimStatus` |

---

### GROUP O — Service / Lookup Codes

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveCodes (bool)` | `pats.tbl_Codes` | arg=1 (SAMMSGlobal) |
| `SaveCodes (RCodes)` | `pats.tbl_Codes` | arg=1 (SAMMSGlobal) |

---

### GROUP P — Clinic Master Data

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveClinic` | `ctrl.tbl_Clinic` | arg=1 (SAMMSGlobal) |

---

### GROUP Q — Inventory

All from `SaveInventory.cs`. Schedule: `BHGTaskRunner.exe 8`.

| Method | Destination Table |
|---|---|
| `SaveBottles` | `pats.tbl_Bottle` |
| `SaveLiquidlog` | `pats.tbl_LiquidLog` (EF incremental path; Bulk on reload — see PART 2 Table 9) |
| `SaveInvTypes` | `ctrl.tbl_InvType` |
| `SaveOrientationCheckList` | `pats.tbl_OrientationChecklistNew` |

---

### GROUP R — Forms & QA (EF path — most sites)

| Method | Destination Table | Schedule | Notes |
|---|---|---|---|
| `SaveFormQuestionAnswers` | `pats.tbl_dbo_FormQuestionAnswers` | arg=6 (Samms-Forms) | EF for most sites; Bulk for specific large sites (see PART 2 Table 13) |
| `SaveAnswerSignatures` | `pats.tbl_dbo_FormAnswerSignatures` | arg=6 | EF only |
| `SaveEMFormMDM` | `pats.tbl_EandMFormMDM` | arg=6 | EF only |
| `SaveEMFormPregnancy` | `pats.tbl_EandMFormPregnancy` | arg=6 | EF only |
| `SaveComprehensiveAssessmentForm` | `pats.tbl_ComprehensiveAssessmentForm` | arg=6 | EF only |

---

### GROUP S — Custom Clinic Forms

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveCustomQuestions` | `pats.tbl_CustomQuestions` | Regional ETL P1 (arg=2) |
| `SaveCustomAnswers` | `pats.tbl_CustomAnswers` | Regional ETL P1 (arg=2) |

---

### GROUP T — Clinical Scales

| Method | Destination Table | Schedule | Notes |
|---|---|---|---|
| `SaveCows_v6` | `pats.tbl_Cows_V6` | arg=8 | PHC sites excluded |

---

### GROUP U — State-Specific Clinical Assessments

All from `SaveCA.cs`. Schedule: `BHGTaskRunner.exe 8`.

| Method | Destination Table |
|---|---|
| `SaveMNCA` | `pats.tbl_MNCA` |
| `SaveMNCALOC` | `pats.tbl_MNCALOC` |
| `SaveVACA` | `pats.tbl_VACA` |
| `SaveVACASummary` | `pats.tbl_VACASummary` |
| `SaveNewAdmissionAssessment` | `pats.Tbl_AdmissionAssessment` |
| `SaveNewAdmissionAssessmentASAMDimension6` | `pats.tbl_AdmissionAssessmentDimensionSix` |
| `SaveNewPeriodicReassessment` | `pats.Tbl_ReAssessment` |
| `Savenewperiodicreassessmentcounselorreview` | `pats.tbl_PACounselorReview` |

---

### GROUP V — Brief Addiction Monitor (BAM)

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveBamForm` | `pats.tbl_BAMForm` | arg=1 (SAMMSGlobal) |
| `SaveBamScore` | `pats.tbl_BAMScore` | arg=1 (SAMMSGlobal) |

---

### GROUP W — Patient Enrollment

| Method | Destination Table | Schedule |
|---|---|---|
| `SaveEnrollment` | `pats.tbl_Enrollment` | Regional ETL P1 and P2 (arg=2 and 4) |

---

### GROUP X — Daily Patient Activity

| Method | Destination Table | Schedule | Notes |
|---|---|---|---|
| `SaveCheckIn` | `pats.tbl_CheckIn` | Regional ETL P2 (arg=4) | `ciQUEUETIME` column stripped if not in source |
| `SaveTreatmentLevel` | `pats.tbl_TreatmentLevel` | Regional ETL P2 (arg=4) | |
| `SaveAppointmentAttend` | `pats.tbl_AppointmentAttend` | arg=8 | |
| `SaveAppointments` | `pats.Tbl_Appointments` | arg=8 | |

---

### GROUP Y — Insurance / Payer

| Method | Destination Table | Schedule |
|---|---|---|
| `SavePayerClient` | `pats.tbl_PayerClient` | Regional ETL P2 (arg=4) |
| `RemovePayerClients` | `pats.tbl_PayerClient` | Regional ETL P2 (arg=4) |
| `SavePayerCltHistory` | `pats.tbl_PayerCltHistory` | Regional ETL P2 (arg=4) |

---

### GROUP Z — Audit and Financial

| Method | Destination Table | Called By |
|---|---|---|
| `SaveRowTrax` | `tsk.tbl_RowTrax` | All Save* methods (when `RowTrax=true`) |
| `SaveFmp` | `pats.tbl_FMP` | Regional ETL |

---

## PART 4 — SUMMARY COUNTS

| Category | Count |
|---|---|
| Tables using SqlBulkCopy as primary path | **13** |
| Tables using EF Core as primary path | **85+** |
| Tables with both Bulk and EF paths (hybrid) | **5** (DartsSrv, ClientDemo, Dose, LiquidLog, FormQA) |
| Staging tables in `stg.` schema | **13** (one per Bulk table) |
| Merge stored procedures | **17** (DartsSrvMerge × 8, + 9 others) |

---

## PART 5 — WHAT THIS MEANS FOR FABRIC MIGRATION

### Replace EF Core (Pattern A) With:
```python
# PySpark Delta MERGE
target_table = DeltaTable.forName(spark, "silver.pats_tbl_enrollment")
target_table.alias("target").merge(
    source_df.alias("source"),
    "target.SiteCode = source.SiteCode AND target.EnrollmentId = source.EnrollmentId"
).whenMatchedUpdate(
    condition="target.RowChkSum <> source.RowChkSum",
    set={col: f"source.{col}" for col in source_df.columns}
).whenNotMatchedInsertAll().execute()
```
- No staging table needed.
- No in-memory list loading.
- Delta engine handles the MERGE set-based — same logic, vastly faster.

### Replace SqlBulkCopy + SP MERGE (Pattern B) With:
```python
# PySpark: write incremental slice to Bronze (Delta), then Silver MERGE
# Bronze — append new slice
source_df.write.format("delta").mode("append").saveAsTable("bronze.pats_tbl_dartssrv")

# Silver — MERGE using same key + RowChkSum logic as SQL MERGE proc
target_table = DeltaTable.forName(spark, "silver.pats_tbl_dartssrv")
target_table.alias("t").merge(
    source_df.alias("s"),
    "t.SiteCode = s.SiteCode AND t.DsId = s.DsId"
).whenMatchedUpdate(
    condition="t.RowChkSum <> s.RowChkSum",
    set={...}
).whenNotMatchedInsertAll().execute()
```
- No staging table needed — Delta replaces `stg.tbl_xxx`.
- The MERGE proc logic moves from T-SQL into PySpark `.merge()`.
- Year-partitioned DartsSrv → in Fabric use a single Delta table with a `Year` partition column instead of 10 separate physical tables.

### Key Hybrid Tables to Watch
These 5 tables need careful handling because they have **both** paths still active:

| Table | Daily Path | Reload/Backfill Path |
|---|---|---|
| `pats.tbl_DartsSrv` | Bulk (primary) | EF SaveDartSrv20XX |
| `pats.tbl_ClientDemo1/2` | Bulk (stg.clientdemo) | EF SaveClientDemo1var |
| `pats.tbl_Dose` | Bulk (most sites) | EF for V10A, CBCO, V21, V10 |
| `pats.tbl_LiquidLog` | EF incremental | Bulk on Reload |
| `pats.tbl_dbo_FormQuestionAnswers` | EF (most sites) | Bulk for 18 specific large sites |

In Fabric, consolidate both paths into a single Delta MERGE notebook with a `reload` parameter flag — one notebook, two modes.
