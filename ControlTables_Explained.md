# Control Tables — Complete Explanation
**Topic:** How `ctrl.tbl_Connections` + `ctrl.tbl_LocationCons` + `ctrl.tbl_Locations` drive the ETL pipelines  
**Date:** 2026-03-25

---

## The Short Answer

Yes — the combination of these three tables is the **master control panel** for the entire ETL system. They decide:
- **Which clinics** participate in ETL
- **What database** to connect to for each clinic
- **Which pipeline** each clinic belongs to (Eastern/Central/Mountain/Pacific)
- **Which columns** to extract (schema version)

Without these tables being configured correctly, no ETL task gets created and no pipeline runs.

---

## PART 1 — The Three Pipeline-Driving Tables

### Table 1: `ctrl.tbl_Connections` — The Server Phone Book

Think of this as a **phone book of SQL servers**. It has one row per unique SQL Server instance that SAMMS clinics run on. Multiple clinics can share the same physical server.

**Columns:**

| Column | Type | Purpose |
|---|---|---|
| `ConId` | int (PK) | Unique ID for each server |
| `ConType` | int | Type of system (SAMMS, PHC, etc.) |
| `ConStr` | varchar(500) | SQL connection string — server address + auth **only** (NO database name) |
| `ConName` | varchar(50) | Friendly name for this server |
| `UserName` | varchar(50) | SQL login username |
| `Password` | varchar(50) | SQL login password (hardcoded — security risk) |
| `LastModBy` | varchar(50) | Who last changed this row |
| `LastModAt` | datetime | When it was last changed |

**Example data:**

| ConId | ConType | ConStr | ConName |
|---|---|---|---|
| 1 | SAMMS | `Data Source=BHG-SQL-East01;Persist Security Info=True;` | East Server 1 |
| 2 | SAMMS | `Data Source=BHG-SQL-East02;Persist Security Info=True;` | East Server 2 |
| 3 | PHC | `Data Source=PHC-SQL01;Persist Security Info=True;` | PHC Server |
| 4 | SAMMS | `Data Source=BHG-SQL-Central01;Persist Security Info=True;` | Central Server 1 |

> **Critical note:** `ConStr` here is INCOMPLETE — it has the server address but NOT the database name.  
> The database name comes from `ctrl.tbl_LocationCons.DbName` and gets appended at runtime.  
> Final ConStr = `tbl_Connections.ConStr` + `;Initial Catalog=` + `tbl_LocationCons.DbName`

---

### Table 2: `ctrl.tbl_LocationCons` — The Bridge / Assignment Table

This is the **most important** of the three. It links every clinic to:
- Which server it lives on (`ConnectionId` → foreign key to `tbl_Connections`)
- Which database it uses (`DbName`)
- Which ETL action group it belongs to (`ActionKey`)
- What SAMMS version it is running (`SchemaVersion`)

**Columns:**

| Column | Type | Purpose |
|---|---|---|
| `SiteCode` | varchar(25) (PK) | The clinic identifier (e.g., B01, B03, PHC) |
| `EffectiveDate` | datetime (PK) | When this connection config became active |
| `ConnectionId` | int | FK → `ctrl.tbl_Connections.ConId` (which server) |
| `DbName` | varchar(255) | The actual database name on that server (e.g., `SAMMS_B01`) |
| `ActionKey` | long (PK) | Which ETL action group this site participates in |
| `SchemaVersion` | varchar(5) | SAMMS version at this site (`V5` or `V6`) |

**Example data:**

| SiteCode | EffectiveDate | ConnectionId | DbName | ActionKey | SchemaVersion |
|---|---|---|---|---|---|
| B01 | 2021-01-01 | 1 | SAMMS_B01 | 1 | V6 |
| B01 | 2021-01-01 | 1 | SAMMS_B01 | 2 | V6 |
| B03 | 2020-06-01 | 1 | SAMMS_B03 | 1 | V5 |
| B05 | 2022-03-01 | 2 | SAMMS_B05 | 1 | V6 |
| B12 | 2019-09-01 | 4 | SAMMS_B12 | 1 | V6 |
| PHC | 2020-01-01 | 3 | SAMMS_PHC | 1 | V6 |

> **Key things to understand:**
> - A site can have **multiple rows** (one per ActionKey) — meaning one clinic can participate in multiple ETL action types
> - `EffectiveDate` allows connection changes over time — when a clinic moves to a new server, you add a new row with the new `EffectiveDate` rather than editing the old one
> - `SchemaVersion = V5` means the old SAMMS schema — some newer columns are skipped during extraction  
> - `SchemaVersion = V6` means newer SAMMS schema — all columns including newer ones are extracted

---

### Table 3: `ctrl.tbl_Locations` — The Site Registry

This is the **clinic master list**. One row per clinic with all its metadata.

**Columns:**

| Column | Type | Purpose |
|---|---|---|
| `SiteCode` | varchar(25) | The clinic identifier |
| `ClinicName` | varchar | Full name of the clinic |
| `RegionCode` | varchar | **Timezone code: EST, CST, MST, PST** — determines which pipeline |
| `IsActive` | bool | **THE ON/OFF SWITCH** — if false, clinic is excluded from ALL ETL |
| `SiteClinic` | varchar | Site/clinic grouping |
| `Location` | varchar | Physical location description |
| `StateCode` | varchar | US state code |
| `ZipCode` | varchar | Zip code |
| `Latitude` / `Longitude` | decimal | Geographic coordinates |
| `ContractDate` | date | When BHG contracted with this clinic |
| `EnrollCutoff` | date | Date threshold for enrollment data extraction |
| `SammstrxDate` | date | SAMMS transaction start date |
| `IsNewSchema` | bool | Whether this site uses the new SAMMS schema |
| `AcctCmpyId` | varchar | Accounting company ID |
| `VpregionCode` | varchar | VP region grouping code |

**Example data:**

| SiteCode | ClinicName | IsActive | RegionCode | IsNewSchema | EnrollCutoff |
|---|---|---|---|---|---|
| B01 | BHG Baltimore | **true** | EST | true | 2019-01-01 |
| B03 | BHG Richmond | **true** | EST | false | 2018-06-01 |
| B05 | BHG Nashville | **true** | CST | true | 2020-03-01 |
| B12 | BHG Dallas | **true** | CST | true | 2019-09-01 |
| B99 | BHG Old Clinic | **FALSE** | EST | false | 2017-01-01 |

> **`IsActive = false`** = that clinic is DEAD to the ETL. The Scheduler creates ZERO tasks for it.  
> **`RegionCode`** = this is what determines whether a clinic goes to Eastern ETL, Central ETL, Mountain ETL, or Pacific ETL.

---

## PART 2 — How They Combine: The `dms.vw_MapAction` View

The Scheduler and BHGTaskRunner **never query these three tables directly**. They query a database view called `dms.vw_MapAction` which JOINs all three together (plus the ETL mapping definition table). Here is the conceptual SQL behind that view:

```sql
SELECT
    lc.SiteCode,
    lc.ActionKey,
    lc.SchemaVersion,
    lc.DbName,

    -- Full connection string is BUILT here by combining both tables:
    c.ConStr + ';Initial Catalog=' + lc.DbName   AS ConStr,

    c.ConnectionId,
    l.IsActive,
    l.IsNewSchema,
    l.RegionCode            AS TimeZone,   -- EST/CST/MST/PST → pipeline assignment
    l.EnrollCutoff,
    l.ContractDate,
    l.ClinicName,

    -- ETL mapping fields (what source table maps to what destination table)
    ma.SrcSchema,           -- e.g. 'dbo'
    ma.FromTblVw,           -- e.g. 'tblEnrollment'  (source table/view name)
    ma.DsnSchema,           -- e.g. 'pats'
    ma.DsnTbl,              -- e.g. 'tbl_Enrollment'  (destination table name)
    ma.WhereCondition,      -- e.g. 'LastModAt >= @WorkDate'
    ma.Enabled,
    ma.IsActive

FROM ctrl.tbl_LocationCons  lc
JOIN ctrl.tbl_Connections   c   ON lc.ConnectionId = c.ConId
JOIN ctrl.tbl_Locations     l   ON lc.SiteCode     = l.SiteCode
JOIN dms.tbl_MapAction      ma  ON lc.ActionKey    = ma.ActionKey
WHERE l.IsActive = 1       -- only active clinics
  AND ma.Enabled = 1       -- only enabled table mappings
```

This single view produces **every (clinic × table) combination** that will become a child task. It is the heartbeat of the entire system.

---

## PART 3 — The Scheduler's Key Filter: `ConnectionID <> 3`

In `Scheduler/Program.cs`, the child task INSERT has this condition:

```sql
WHERE ma.Enabled = 1
  AND ma.IsActive = 1
  AND ConnectionID <> 3    -- PHC server is excluded from SAMMS pipelines
```

`ConnectionId = 3` is the **PHC server**. PHC has its own separate pipeline (`PHC ETL`). This filter prevents PHC sites from getting regular SAMMS child tasks. Even if PHC appears in `vw_MapAction`, it is blocked here.

---

## PART 4 — The Full Picture (Combined Flow Diagram)

```
ctrl.tbl_Connections          ctrl.tbl_LocationCons           ctrl.tbl_Locations
─────────────────────         ──────────────────────────       ──────────────────────
ConId | ConStr                SiteCode | ConId | DbName        SiteCode | IsActive | TimeZone
  1   | Data Source=          | ActionKey | SchemaVer            B01    |   true   |   EST
      | East01;...    ──────► B01 |  1  | SAMMS_B01  ◄────      B03    |   true   |   EST
  2   | Data Source=          |    |    | V6                     B99    |  FALSE   |   EST  ← EXCLUDED
      | East02;...            B03 |  1  | SAMMS_B03              B05    |   true   |   CST
  3   | Data Source=          B99 |  1  | SAMMS_B99  ← IsActive=false, skipped!
      | PHC01;...             PHC |  3  | SAMMS_PHC  ← ConnectionId=3, skipped!
                                        │
                                        ▼
                              dms.vw_MapAction  (the combined view)
                         ────────────────────────────────────────────
                          SiteCode | TimeZone | FullConStr          | Source → Destination
                          B01      | EST      | East01+SAMMS_B01   | dbo.tblEnrollment → pats.tbl_Enrollment
                          B01      | EST      | East01+SAMMS_B01   | dbo.tblClientDemo1 → pats.tbl_ClientDemo1
                          B03      | EST      | East01+SAMMS_B03   | dbo.tblEnrollment → pats.tbl_Enrollment
                          B05      | CST      | East02+SAMMS_B05   | dbo.tblEnrollment → pats.tbl_Enrollment
                         (B99 not here — IsActive = false)
                         (PHC not here — ConnectionId = 3)
                                        │
                                        ▼
                              Scheduler.exe reads vw_MapAction
                              and inserts child tasks:
                         ────────────────────────────────────────────
                          B01 × pats.tbl_Enrollment  → assigned to → Eastern ETL P1
                          B01 × pats.tbl_ClientDemo1 → assigned to → Eastern ETL P1
                          B03 × pats.tbl_Enrollment  → assigned to → Eastern ETL P1
                          B05 × pats.tbl_Enrollment  → assigned to → Central ETL P1
                         (B99 = 0 tasks created)
                         (PHC = 0 SAMMS tasks, goes to PHC ETL only)
                                        │
                                        ▼
                              BHGTaskRunner.exe 2
                              picks up Eastern ETL P1 parent task
                              loops through all child tasks
                              for each child: connects to SAMMS source
                              via the ConStr built from these tables
                              extracts data → writes to Azure BHG_DR
```

---

## PART 5 — Real-World Scenarios

| What you want to do | What you change in the control tables |
|---|---|
| **Add a brand new clinic to ETL** | 1. Add row to `ctrl.tbl_Locations` (IsActive=true, RegionCode=EST/CST/etc.) 2. Add row(s) to `ctrl.tbl_LocationCons` (ConId pointing to right server, DbName, ActionKey) |
| **Temporarily stop a clinic's ETL** | Set `ctrl.tbl_Locations.IsActive = false` — next morning Scheduler creates zero tasks for it |
| **Permanently retire a clinic** | Set `ctrl.tbl_Locations.IsActive = false` (or delete the `tbl_LocationCons` rows) |
| **Clinic moves to a different SQL server** | Add new row to `ctrl.tbl_LocationCons` with new `ConnectionId` and new `EffectiveDate` |
| **Clinic upgrades SAMMS to V6** | Update `ctrl.tbl_LocationCons.SchemaVersion = 'V6'` — SelectConstructor now includes V6 columns |
| **Clinic changes timezone (moves state)** | Update `ctrl.tbl_Locations.RegionCode` to new timezone — Scheduler CASE assigns it to different pipeline |
| **Add a new SQL server** | Add one row to `ctrl.tbl_Connections` with the new server's ConStr, get a new ConId |
| **Exclude a specific site from a specific table** | Handled by Scheduler skip rule UPDATEs (those SET RowState=26 statements) |

---

## PART 6 — The Full `ctrl` Schema (All Control Tables)

The `ctrl` schema has more than just the three pipeline-driving tables. Here is every table and its role:

### Sub-Group A: Pipeline Drivers (what we discussed above)

| Table | Role |
|---|---|
| `ctrl.tbl_Connections` | Server connection strings library |
| `ctrl.tbl_LocationCons` | Site → server + DB + ActionKey + SchemaVersion mapping |
| `ctrl.tbl_Locations` | Clinic registry with IsActive and timezone |
| `ctrl.tbl_LocationCmds` | Site-specific SQL commands to run during initialization (custom per site) |
| `ctrl.tbl_SiteTableInit` | Tracks whether each table has been fully initialized per site |
| `ctrl.tbl_SiteTableInitLog` | Audit log of every init run (site, table, row count, when) |

### Sub-Group B: Clinic Configuration (ETL destination tables)

These are **copied from SAMMS** into Azure during each ETL run. They are destination tables — written to by BHGTaskRunner.

| Table | Source in SAMMS | What it holds |
|---|---|---|
| `ctrl.tbl_CLINIC` | `dbo.tblClinic` | All 200+ feature flags and settings for each clinic |
| `ctrl.tbl_CODES` | `dbo.tblCodes` | Service and billing codes per clinic |
| `ctrl.tbl_CONSENTS` | `dbo.tblConsents` | Consent form definitions (regular sites) |
| `ctrl.tbl_CONSENTS_PHC` | `dbo.tblConsents` (PHC) | Consent form definitions (PHC sites) |
| `ctrl.tbl_INVTYPE` | `dbo.tblInvtype` | Medication and inventory types per clinic |
| `ctrl.tbl_DroDownListItems` | `dbo.tblDropDownListItems` | All dropdown options from SAMMS UI |
| `ctrl.tbl_GlobalDevices` | `dbo.tblGlobalDevices` | Dispensing pumps, printers, sig pads per clinic |
| `ctrl.tbl_3PSETUP` | `dbo.tbl3pSetup` | Third-party billing configuration per clinic |
| `ctrl.tbl_claimstatus` | `dbo.tblClaimStatus` | Claim batch submission statuses |

### Sub-Group C: User and Access Tables

| Table | Source in SAMMS | What it holds |
|---|---|---|
| `ctrl.tbl_USER` | `dbo.tblUser` | All staff/user accounts across all clinics |
| `ctrl.tbl_USERSITES` | `dbo.tblUserSites` | Which users are assigned to which sites |

### Sub-Group D: Reference / Lookup Tables

| Table | What it holds |
|---|---|
| `ctrl.tbl_XREF` | Shared cross-reference lookup codes and descriptions |
| `ctrl.tbl_COWXREF` | COWS (Clinical Opiate Withdrawal Scale) valid values and descriptions |
| `ctrl.tbl_Forms2Process` | Configuration — which form names map to which destination tables |
| `ctrl.tbl_COLS` | Column name registry used by SelectConstructor for column validation |

---

## PART 7 — One-Line Summary of Every `ctrl` Table

| Table | One-Line Role |
|---|---|
| `ctrl.tbl_Connections` | **"Which SQL servers exist and how to connect to them"** |
| `ctrl.tbl_LocationCons` | **"Which clinic uses which server + database + action group"** |
| `ctrl.tbl_Locations` | **"Which clinics are active and what timezone are they in"** |
| `ctrl.tbl_LocationCmds` | **"Custom SQL commands to run for each site during init"** |
| `ctrl.tbl_SiteTableInit` | **"Has this table been fully loaded for this site yet?"** |
| `ctrl.tbl_SiteTableInitLog` | **"Log of every full-load initialization run"** |
| `ctrl.tbl_CLINIC` | **"Full SAMMS settings and feature flags for each clinic"** |
| `ctrl.tbl_CODES` | **"Service and billing code definitions per clinic"** |
| `ctrl.tbl_CONSENTS` | **"Consent form type definitions"** |
| `ctrl.tbl_CONSENTS_PHC` | **"PHC-specific consent form definitions"** |
| `ctrl.tbl_INVTYPE` | **"Medication and inventory type definitions per clinic"** |
| `ctrl.tbl_DroDownListItems` | **"All dropdown list options from SAMMS"** |
| `ctrl.tbl_GlobalDevices` | **"Dispensing pumps, printers, and devices at each clinic"** |
| `ctrl.tbl_3PSETUP` | **"Third-party billing setup per clinic"** |
| `ctrl.tbl_claimstatus` | **"Claim batch submission status tracking"** |
| `ctrl.tbl_USER` | **"All staff user accounts across all clinics"** |
| `ctrl.tbl_USERSITES` | **"Which staff members are assigned to which sites"** |
| `ctrl.tbl_XREF` | **"Shared lookup codes and their descriptions"** |
| `ctrl.tbl_COWXREF` | **"COWS assessment valid value descriptions"** |
| `ctrl.tbl_Forms2Process` | **"Config: which form names map to which ETL destination tables"** |
| `ctrl.tbl_COLS` | **"Column name registry for extraction validation"** |

---

## The Bottom Line

When people say **"the control table drives the pipelines"** in meetings, they mean:

> `ctrl.tbl_LocationCons` + `ctrl.tbl_Connections` + `ctrl.tbl_Locations`
> → **JOIN into `dms.vw_MapAction`**
> → **Scheduler reads that view every morning**
> → **Creates child tasks for every active clinic × every enabled table**
> → **BHGTaskRunner processes those tasks using the connection string built from these tables**

Change one row in these three tables = change which clinics run, which server they connect to, which pipeline they belong to. That is the power of the control table design.
