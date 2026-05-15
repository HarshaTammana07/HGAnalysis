# BHG ETL — Site Source Patterns Per Table

**Question answered:** Do all 98 tables loop through all 80+ sites, and what is the source for each?  
**Derived from:** `BHGTaskRunner/Program.cs` switch cases + `ctrl.tbl_LocationCons` routing logic  
**Date:** April 2026

---

## Short Answer

**No — not all 98 tables pull from all 80+ sites.**  
There are **6 distinct source-site patterns** across the 98 tables.  
Understanding this is critical before designing Fabric pipeline ForEach loops.

---

## The 6 Source-Site Patterns

---

### Pattern A — ALL Active BHG Sites (~82 tables)

The task queue (`tsk.tbl_Tasks2`) creates **one child task per site per table**.  
The loop iterates every site in `ctrl.tbl_LocationCons` where `Enabled = true`.  
Each site uses its own connection string (`st.ConStr`) to its own SAMMS SQL Server.

**Pipelines:** 2 (P1), 4 (P2), 8 (Inv), 9 (DartSvc), 10 (Dose), 11 (Orders), 7 (Notes)

**Examples of tables in this pattern:**


| Table                          | Pipeline | Domain                  |
| ------------------------------ | -------- | ----------------------- |
| `pats.tbl_DartsSrv_20XX`       | 9        | Counseling sessions     |
| `pats.tbl_Enrollment`          | P1 / P2  | Patient enrollment      |
| `pats.tbl_ClientDemo1`         | P1       | Demographics            |
| `pats.tbl_ClientDemo2`         | P1       | Demographics            |
| `pats.tbl_Dose`                | 10       | Medication dosing       |
| `pats.tbl_Orders_20XX`         | 11       | Prescriptions           |
| `pats.tbl_Claims`              | P2       | Insurance claims        |
| `pats.tbl_UAResult`            | P1       | Drug test results       |
| `pats.tbl_UAResultDetail`      | P2       | Drug test detail        |
| `pats.tbl_Bills`               | P2       | Billing records         |
| `pats.tbl_3pElig`              | P2       | 3rd party eligibility   |
| `pats.tbl_3pArnote`            | 7        | AR billing notes        |
| `pats.tbl_3pClaimNote`         | 7        | Claim notes             |
| `ctrl.tbl_Clinic`              | Global   | Clinic config           |
| `ctrl.tbl_User`                | Global   | Staff accounts          |
| `pats.tbl_Codes`               | Global   | Service codes           |
| `pats.tbl_BAMForm`             | Global   | BAM assessments         |
| `pats.tbl_ReAssessment`        | 8        | Periodic re-assessments |
| `pats.tbl_AdmissionAssessment` | 8        | Admission assessment    |
| `pats.tbl_CheckIn`             | P2       | Daily check-in          |
| `pats.tbl_TreatmentLevel`      | P2       | ASAM level of care      |
| `pats.tbl_PayerClient`         | P2       | Insurance coverage      |
| `pats.tbl_Bottle`              | 8        | Take-home medication    |
| `pats.tbl_Appointments`        | 8        | Appointment scheduling  |
| `ayx.tbl_PreAdmission_V6`      | P1       | Pre-admission intake    |
| ... and all remaining tables   | various  | various                 |


**Fabric implication:** ForEach over `meta.dim_site` where `runner_type = 'BHG'` and `enabled = true`.

---

### Pattern B — ONE Shared SAMMSGLOBAL Database (~4 tables)

These tables come from **a single shared central SAMMSGLOBAL database**, not from each clinic's individual SAMMS.  
The connection is either driven by `st.ConStr` pointing to SAMMSGLOBAL, or **hardcoded** directly in the source code for PHC.

**Evidence from code:**

```csharp
// BAM — PHC uses hardcoded SAMMSGLOBAL connection
if (st.SiteCode == "PHC")
    SrcDt = sm.GetTableData(...,
        @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;");
else
    SrcDt = sm.GetTableData(..., st.ConStr);  // BHG sites use their own ConStr

// COWS (ClinicalOpiateWithdrawalScale) — same pattern
if (st.SiteCode == "PHC")
    SrcDt = sm.GetTableData(...,
        @"Data Source=PHCSQLVM;Initial Catalog=SAMMSGLOBAL;Integrated Security=True;Encrypt=False;");
else
    SrcDt = sm.GetTableData(..., st.ConStr);
```

**Tables in this pattern:**


| Table                                    | Source DB                           | ActionKey | Notes                             |
| ---------------------------------------- | ----------------------------------- | --------- | --------------------------------- |
| `pats.tbl_BriefAddictionMonitor`         | SAMMSGLOBAL                         | 2         | Shared BAM data — not per-clinic  |
| `pats.tbl_ClinicalOpiateWithdrawalScale` | SAMMSGLOBAL (PHC) / own SAMMS (BHG) | 2         | PHC = hardcoded; BHG = own ConStr |
| `pats.tbl_FormsSAMMSClient`              | SAMMSGLOBAL                         | 2         | Form-to-client linkage            |
| `pats.tbl_LiquidLog`                     | SAMMSGLOBAL                         | 2         | Dispensing log                    |


**Fabric implication:** These tables get a **separate pipeline activity** — one JDBC connection to SAMMSGLOBAL, no ForEach loop over sites. PHC SAMMSGLOBAL is a different server (`PHCSQLVM`) from BHG SAMMSGLOBAL.

---

### Pattern C — LAB Site ONLY (2 tables)

The `Samms-LAB` pipeline (`arg=5`) is exclusively for the **LAB site**. The task queue only creates tasks for `SiteCode = "Lab"`.

**Evidence from code:**

```csharp
case "5":
    pTasks = db.VwTaskList.Where(x =>
        x.SiteCode != "PHC"
        && x.Status == 17
        && x.TaskName == "Samms-LAB"
        && x.RunAt < DateTime.Now).ToList();
```

**Tables in this pattern:**


| Table                  | Pipeline          | Notes                      |
| ---------------------- | ----------------- | -------------------------- |
| `pats.tbl_ClientDemo1` | Samms-LAB (arg=5) | LAB site demographics only |
| `pats.tbl_ClientDemo2` | Samms-LAB (arg=5) | LAB site demographics only |


> **Note:** These same two tables also run for all BHG sites under P1 (arg=2). The LAB pipeline (arg=5) is an **additional** run for LAB specifically.

**Fabric implication:** Two separate pipeline activities for ClientDemo1/2 — one for LAB site, one for all BHG sites (P1).

---

### Pattern D — PHC Sites ONLY (separate runner entirely)

PHC clinics run through `PHC/PHC.exe`, not `BHGTaskRunner.exe`.  
The main `BHGTaskRunner` filter excludes PHC at the top level:

```csharp
// Line 22, 26, 33, 36, etc. — PHC excluded from ALL pTasks queries
db.VwTaskList.Where(x => x.SiteCode != "PHC" && ...)
```

**PHC has its own versions of these files:**


| File                    | BHG-DR-LIB Version     | PHC/ Version      | Difference                 |
| ----------------------- | ---------------------- | ----------------- | -------------------------- |
| `BulkDartsSvc.cs`       | Standard bulk loader   | PHC variant       | PHC-specific staging merge |
| `SaveBills.cs`          | Standard billing       | PHC billing rules | PHC billing logic          |
| `SaveFormQAData.cs`     | Standard forms         | PHC forms         | PHC form handling          |
| `SavePreAdmissionV6.cs` | Standard pre-admission | PHC pre-admission | PHC field handling         |
| `SaveUAResults.cs`      | Standard UA            | PHC UA results    | PHC-specific UA logic      |


**Column-level PHC filtering:**  
For tables that do run in PHC context, column mappings are filtered differently:

```csharp
if (st.SiteCode == "PHC")
    tdwork = tdwork.Where(x => x.PHC_Enabled).ToList();
```

The `dms.tbl_MapSrc2Dsn` table has a `PHC_Enabled` column per field — PHC gets a subset of columns.

**Fabric implication:** PHC gets its **own Fabric pipeline branch** with `runner_type = 'PHC'` parameter. Same notebook logic, different parameters, PHC-specific column map filtered by `phc_enabled = true` in `meta.dim_column_map`.

---

### Pattern E — Specific Sites SKIPPED for a Table

Some tables run for ALL sites in the task loop but certain sites are **silently skipped** inside the switch case. The task is marked complete (no error) but no data is processed.


| Table                         | Site(s) Skipped               | Why Skipped                                                      | Code                                                                                              |
| ----------------------------- | ----------------------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `pats.tbl_enrollment`         | `Lab`                         | Lab site doesn't use enrollment                                  | `if (st.SiteCode != "Lab") { ... } else { rCodes.IsResult = true; }`                              |
| `ayx.tbl_preadmission_v6`     | Any V5 schema site            | Old SAMMS version — `SF_PatientPreAdmission` table doesn't exist | `if (SrcDt.Rows.Count == 1 && st.SchemaVersion != "V5") { ... } else { rCodes.IsResult = true; }` |
| `pats.tbl_cows_v6`            | Sites without `SF_COWS` table | Older SAMMS version                                              | `if (SrcDt.Rows.Count == 1) { ... }` — checks `sys.tables` for `SF_COWS` first                    |
| `pats.tbl_clientdemo1/2` (P1) | Lab (under P1)                | Lab uses separate LAB pipeline                                   | Not in P1 task queue for Lab                                                                      |


**Fabric implication:** `meta.dim_site_features` table controls which tables get skipped per site. These become **pre-flight checks** in the notebook before the JDBC SELECT runs, not code-level if/else.

---

### Pattern F — Same Table, Different Path or Columns Per Site

These tables run for **all sites** but specific sites trigger **different code paths** — either a different load method (EF vs Bulk) or a different SELECT (column stripped or added).

#### F1 — Site-Specific ROUTING (EF vs Bulk switch)


| Table             | Sites → EF Core Path           | All Other Sites → Bulk Path              |
| ----------------- | ------------------------------ | ---------------------------------------- |
| `pats.tbl_claims` | `VBRA`, `VMIN`, `VWBY`, `VBRP` | `BulkDartsSrvLoader` + `stg.ClaimsMerge` |


```csharp
case "pats.tbl_claims":
    if ((st.SiteCode == "VBRA") || (st.SiteCode == "VMIN")
        || (st.SiteCode == "VWBY") || (st.SiteCode == "VBRP"))
    {
        rCodes = sd.SaveClaims(SrcDt, st.SiteCode, ...);  // EF Core
    }
    else
    {
        rCodes = bldr.BulkDartsSrvLoader(SrcDt, "stg.tbl_claims", ...);  // Bulk
    }
```

#### F2 — Site-Specific COLUMN VARIATIONS (SELECT modified per site)


| Table                  | Site                           | Column Change                     | Direction                                  |
| ---------------------- | ------------------------------ | --------------------------------- | ------------------------------------------ |
| `pats.tbl_3parnote`    | `Lab`                          | `globalBatchId` stripped          | Removed — doesn't exist in Lab schema      |
| `pats.tbl_3pclaimnote` | `Lab`                          | `globalBatchId` stripped          | Removed                                    |
| `ctrl.tbl_clinic`      | `Lab`                          | `PullPicsFromDB` stripped         | Removed — Lab doesn't store patient photos |
| `pats.tbl_bottle`      | `Lab`                          | `ExpDate` stripped                | Removed                                    |
| `pats.tbl_dartssrv`    | All sites                      | `ServiceType` stripped if absent  | Runtime `sys.all_columns` check per site   |
| `pats.tbl_enrollment`  | `CBNC` + sites with `Modality` | `Modality` column added           | Runtime view column check                  |
| `pats.tbl_enrollment`  | Sites without `TreatmentLevel` | `TreatmentLevel` stripped         | Runtime `sys.all_columns` check            |
| `pats.tbl_dose`        | `V10A`, `CBCO`, `V21`, `V10`   | Different lookback window formula | Different year-based WHERE                 |


**Fabric implication:** `meta.dim_site_features` stores these flags. The PySpark notebook checks the flag before building the JDBC SELECT string. No hardcoded `if SiteCode == "Lab"` inside notebook logic.

---

## Complete Summary Table — All Patterns at a Glance


| Pattern                                    | # Tables (approx) | Source                           | Sites Involved                                                              |
| ------------------------------------------ | ----------------- | -------------------------------- | --------------------------------------------------------------------------- |
| **A — All BHG sites**                      | ~82               | Each site's own SAMMS SQL Server | 80–113 BHG sites                                                            |
| **B — SAMMSGLOBAL only**                   | ~4                | One shared SAMMSGLOBAL database  | 1 shared DB (BHG) + 1 PHC SAMMSGLOBAL                                       |
| **C — LAB site only**                      | 2                 | Lab site SAMMS only              | 1 site                                                                      |
| **D — PHC sites only**                     | Several           | PHC.exe separate runner          | All PHC sites                                                               |
| **E — Specific sites skipped**             | ~3                | All sites except excluded ones   | Exclusions: Lab, V5 sites, no-SF_COWS sites                                 |
| **F — Same table, different path/columns** | ~8 tables         | All sites                        | Column/path varies: Lab, VBRA, VMIN, VWBY, VBRP, CBNC, V10A, CBCO, V21, V10 |


---

## Visual Map — Source → Table → Destination

```
SOURCE DATABASES                        DESTINATION (Azure BHG_DR)
════════════════════════════            ═══════════════════════════

Pattern A: Each clinic's SAMMS
  SAMMS-B01  ──┐
  SAMMS-B02  ──┤
  SAMMS-B03  ──┤  ~82 tables each   →  pats.tbl_DartsSrv_20XX
  ...          ├──────────────────→  pats.tbl_Enrollment
  SAMMS-B80  ──┤                    →  pats.tbl_Dose
  SAMMS-V10  ──┤                    →  pats.tbl_Orders_20XX
  SAMMS-CBNC ──┘                    →  ... (all per-site tables)

Pattern B: One shared SAMMSGLOBAL
  SAMMSGLOBAL  ──────────────────→  pats.tbl_BriefAddictionMonitor
               (1 connection)     →  pats.tbl_FormsSAMMSClient
                                  →  pats.tbl_LiquidLog
                                  →  pats.tbl_ClinicalOpiateWithdrawalScale

  PHCSQLVM/SAMMSGLOBAL (hardcoded)
               ──────────────────→  Same tables for PHC sites

Pattern C: LAB site only
  SAMMS-Lab    ──────────────────→  pats.tbl_ClientDemo1 (LAB rows only)
               (1 connection)    →  pats.tbl_ClientDemo2 (LAB rows only)

Pattern D: PHC sites via PHC.exe
  PHC-Site-1  ──┐
  PHC-Site-2  ──┤  PHC.exe only   →  pats.tbl_DartsSrv_20XX (PHC rows)
  ...          ──┘                →  pats.tbl_Bills (PHC rows)
                                  →  pats.tbl_FormQuestionAnswers (PHC rows)

Pattern E: Skipped sites
  All sites loop → Lab skipped for tbl_enrollment
                 → V5 sites skipped for ayx.tbl_preadmission_v6
                 → Sites without SF_COWS skipped for pats.tbl_cows_v6

Pattern F: Same table, different path
  SAMMS-VBRA  ──┐  EF Core path  →  pats.tbl_Claims (VBRA, VMIN, VWBY, VBRP)
  All others  ──┘  Bulk path     →  pats.tbl_Claims (all other sites)
```

---

## Fabric ForEach Design — What This Means

Based on these 6 patterns, you need **4 different ForEach / activity shapes** in Fabric:

### ForEach 1 — Standard site loop (Pattern A)

```
ForEach site in meta.dim_site
    WHERE runner_type = 'BHG' AND enabled = true
    concurrency = 8–20
    │
    └── Notebook: ingest_standard_tables(site_code, run_id, work_date)
           reads meta.dim_site_features for column guards
           reads meta.dim_column_map for SELECT construction
           writes Bronze → merges Silver
```

### Single Activity — SAMMSGLOBAL (Pattern B)

```
Single Notebook Activity (no ForEach)
    ingest_global_tables(run_id, work_date)
    source = SAMMSGLOBAL connection (Key Vault secret)
    tables: BAM, FormsSAMMSClient, LiquidLog, COWS
    writes Bronze → merges Silver
```

### Single Activity — LAB site (Pattern C)

```
Single Notebook Activity
    ingest_lab_demographics(run_id, work_date)
    site_code = 'Lab'
    tables: ClientDemo1, ClientDemo2
    writes Bronze → merges Silver
```

### ForEach 2 — PHC site loop (Pattern D)

```
ForEach site in meta.dim_site
    WHERE runner_type = 'PHC' AND enabled = true
    │
    └── Notebook: ingest_standard_tables(site_code='PHC', run_id, work_date)
           phc_enabled = true → filters column map to PHC_Enabled columns only
           uses PHC-specific SAMMSGLOBAL for BAM/COWS
```

### Patterns E and F are handled by `meta.dim_site_features` — no extra ForEach needed

- Skip logic → checked inside the notebook per site per table
- Column variation → driven by feature flags in `meta.dim_site_features`
- EF vs Bulk routing → irrelevant in Fabric (Delta MERGE replaces both)

---

## The `meta.dim_site_features` Table — Must Build Before First Run

This table is the **single most important prerequisite** for Fabric migration.  
It replaces all the `if (st.SiteCode == "...")` guards in `BHGTaskRunner/Program.cs`.


| Column                 | Type    | Purpose                                         |
| ---------------------- | ------- | ----------------------------------------------- |
| `SiteCode`             | varchar | Primary key                                     |
| `RunnerType`           | varchar | `BHG` / `PHC` / `LAB`                           |
| `TimeZone`             | varchar | `EST` / `CST` / `MST` / `PST`                   |
| `SchemaVersion`        | varchar | `V5` / `V6` / `Methasoft`                       |
| `HasServiceTypeCol`    | bit     | DartsSrv SELECT guard                           |
| `HasSfPreAdmission`    | bit     | PreAdmission V6 table exists                    |
| `HasSfCows`            | bit     | COWS V6 table exists                            |
| `HasModalityCol`       | bit     | Enrollment SELECT guard                         |
| `HasTreatmentLevelCol` | bit     | Enrollment SELECT guard                         |
| `HasGlobalBatchId`     | bit     | 3pArnote / 3pClaimNote SELECT guard             |
| `HasExpDateCol`        | bit     | Bottle SELECT guard                             |
| `HasPullPicsFromDb`    | bit     | Clinic SELECT guard                             |
| `UseEfForClaims`       | bit     | Claims routing: `true` = EF, `false` = Bulk     |
| `UseLongDoseLookback`  | bit     | Dose: sites V10A, CBCO, V21, V10                |
| `IsEnabled`            | bit     | Whether site is active in ETL                   |
| `KvSecretName`         | varchar | Azure Key Vault secret name for JDBC connection |


---

*Derived from deep analysis of `BHGTaskRunner/Program.cs` switch cases (lines 144–3375),*  
*`ctrl.tbl_LocationCons` routing, and all Save* documentation in the BCAppCode workspace.*