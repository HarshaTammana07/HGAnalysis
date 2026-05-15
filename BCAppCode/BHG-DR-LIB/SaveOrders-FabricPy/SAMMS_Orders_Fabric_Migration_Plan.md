# SAMMS Orders ETL — Microsoft Fabric Migration Plan
## Theory & Workflow (Pre-Code Review Document)

**Purpose:** This document describes the complete end-to-end plan to migrate the
SAMMS Orders ETL pipeline from the existing C# / Azure SQL system into Microsoft
Fabric. No code is included. This document is intended for senior review and
sign-off before any development begins.

**Prepared:** April 30, 2026
**Pipeline:** SAMMS-ETL-Orders (BHGTaskRunner.exe arg=11)
**C# Source:** BCAppCode/BHG-DR-LIB/SaveOrders.cs

---

## 1. Current State — What Exists Today

### 1.1 What the Pipeline Does

The Orders ETL extracts prescription order records from 80+ clinic SQL Server
databases (SAMMS) and loads them into a central Azure SQL data warehouse (BHG_DR).
It runs daily and covers orders dating back to 2016, partitioned by year into 13
separate destination tables.

### 1.2 Current Technology Stack

| Component | Technology | Role |
|-----------|-----------|------|
| Source databases | SQL Server (on-premises per clinic) | Raw prescription order data |
| Destination | Azure SQL — BHG_DR | Central data warehouse |
| Runner | C# Windows EXE — BHGTaskRunner.exe | Orchestrates the ETL |
| Scheduling | Windows Task Scheduler | Triggers BHGTaskRunner daily |
| Task queue | tsk.tbl_Tasks2 (Azure SQL) | Tracks which clinics to run |
| Clinic registry | ctrl.tbl_LocationCons (Azure SQL) | Maps SiteCode to connection string |
| Column mapping | dms.tbl_MapSrc2Dsn (Azure SQL) | Defines SELECT columns per ActionKey |
| Query builder | SelectConstructor.cs (C#) | Builds SELECT dynamically from metadata |

### 1.3 Current Destination Tables

| Table | Content |
|-------|---------|
| pats.tbl_Orders2016 | Orders with OrderDate in 2016 |
| pats.tbl_Orders2017 | Orders with OrderDate in 2017 |
| pats.tbl_Orders2018 | Orders with OrderDate in 2018 |
| pats.tbl_Orders2019 | Orders with OrderDate in 2019 |
| pats.tbl_Orders2020 | Orders with OrderDate in 2020 |
| pats.tbl_Orders2021 | Orders with OrderDate in 2021 |
| pats.tbl_Orders2022 | Orders with OrderDate in 2022 |
| pats.tbl_Orders2023 | Orders with OrderDate in 2023 |
| pats.tbl_Orders2024 | Orders with OrderDate in 2024 |
| pats.tbl_Orders2025 | Orders with OrderDate in 2025 |
| pats.tbl_Orders2026 | Orders with OrderDate in 2026 |
| pats.tbl_Orders2027 | Orders with OrderDate in 2027 |
| pats.tbl_Orders2028 | Orders with OrderDate in 2028 |

### 1.4 Current Upsert Logic (Per Clinic, Per Year)

The C# SaveOrders20XX methods follow this logic for each year:

1. Load all existing rows for the clinic from the destination table
2. Mark every existing row: RowState = 0, Active = 0 (pre-emptive soft-delete)
3. For each row coming from SAMMS:
   - If the row does not exist in the destination → INSERT it
   - If the row exists and the RowChkSum has changed → UPDATE all columns
   - If the row exists and the RowChkSum is unchanged → touch RowState=1, LastModAt only
4. Any row that was marked in step 2 and never touched in step 3 stays
   RowState=0, Active=0 — this is the soft-delete for orders that disappeared
   from the source

### 1.5 What Is Being Removed in the Migration

The following components exist only because the C# system needed them. They are
not needed in Fabric and will not be migrated.

| Removed Component | Why It Is No Longer Needed |
|-------------------|---------------------------|
| tsk.tbl_Tasks2 | Fabric Pipeline handles scheduling and run tracking natively |
| tsk.tbl_Schedule | Fabric Pipeline schedule replaces this |
| Parent / child task rows | Direct clinic loop in the notebook replaces this |
| Status codes 17 / 18 / 20 | Fabric Pipeline run status replaces this |
| ActionKey | One notebook = one pipeline, no generic routing needed |
| dms.tbl_MapSrc2Dsn | SELECT is written directly in the notebook — no dynamic metadata needed |
| dms.vw_MapAction | No longer needed — no dynamic column mapping |
| SelectConstructor.cs | No longer needed — fixed SELECT in the notebook |
| Scheduler.exe | Fabric Pipeline schedule replaces this |

---

## 2. Target State — What Fabric Looks Like

### 2.1 Guiding Principle

One workspace. One source of truth. Everything self-contained inside Fabric.
No task queues. No dynamic column mapping. No action keys. The notebook reads
one control table, loops through clinics, extracts data, and writes Delta tables.
Fabric tracks run history, status, and scheduling natively.

### 2.2 Target Technology Stack

| Component | Technology | Role |
|-----------|-----------|------|
| Source databases | SQL Server (on-premises per clinic) — unchanged | Raw prescription order data |
| Destination | Fabric Lakehouse (Delta tables in OneLake) | Replaces Azure SQL BHG_DR |
| Runner | Fabric Notebook (Python / PySpark) | Replaces BHGTaskRunner.exe |
| Scheduling | Fabric Pipeline (scheduled trigger) | Replaces Windows Task Scheduler |
| Clinic registry | ctrl_location_cons (Lakehouse Delta table) | Replaces ctrl.tbl_LocationCons |
| Column mapping | None — SELECT hard-coded in notebook | Replaces dms.tbl_MapSrc2Dsn |
| Query builder | None — fixed SELECT in notebook | Replaces SelectConstructor.cs |

### 2.3 Target Workspace Structure

```
Fabric Workspace: "SAMMS-Orders-ETL"
│
├── Lakehouse: "orders_lakehouse"
│   │
│   ├── ctrl_location_cons        ← only config table needed
│   │
│   ├── orders_2016               ← destination Delta tables (13 total)
│   ├── orders_2017
│   ├── orders_2018
│   ├── orders_2019
│   ├── orders_2020
│   ├── orders_2021
│   ├── orders_2022
│   ├── orders_2023
│   ├── orders_2024
│   ├── orders_2025
│   ├── orders_2026
│   ├── orders_2027
│   └── orders_2028
│
├── Notebook: "orders_etl"        ← all ETL logic lives here
│
└── Pipeline: "orders_daily"      ← runs on a daily schedule
```

### 2.4 Target ctrl_location_cons Table

This is the only configuration table in the entire system.

| Column | Type | Description |
|--------|------|-------------|
| SiteCode | string | Clinic identifier e.g. 'B01' |
| ConStr | string | Full ODBC connection string to the clinic's SAMMS SQL Server |
| Active | boolean | True = include in daily run, False = skip |

That is all. No status columns. No action keys. No run dates.

### 2.5 Target Data Flow

```
Fabric Pipeline fires (daily schedule)
        │
        ▼
Fabric Notebook starts
        │
        ▼
Step 1: Read ctrl_location_cons from Lakehouse
        → get list of all clinics where Active = true
        │
        ▼
Step 2: For each clinic in the list:
        │
        ├── Connect to clinic SAMMS SQL Server (via connection string)
        │
        ├── Run fixed SELECT for orders within the lookback window
        │   (all years in one query — same as current C# approach)
        │
        ├── Split the results by year of OrderDate
        │   (2016 rows → year 2016 bucket, 2017 rows → year 2017 bucket, etc.)
        │
        └── For each year bucket that has rows:
                Delta MERGE into orders_YYYY Lakehouse table
                (INSERT new, UPDATE changed, soft-delete missing)
        │
        ▼
Step 3: Fabric Pipeline records run as Succeeded or Failed
        (no status updates needed — Fabric handles this)
```

---

## 3. The Upsert Logic in Fabric (Delta MERGE)

The business logic does not change. Only the execution engine changes.

### What stays exactly the same:
- Match key: SiteCode + OrderNum + CltId
- Change detection: RowChkSum comparison
- New row behavior: INSERT
- Changed row behavior: UPDATE all columns, set RowState=1, LastModAt=now
- Unchanged row behavior: touch RowState=1, LastModAt=now (checksum same)
- Missing row behavior: RowState=0, Active=0 (soft-delete)
- Year partitioning: rows routed to correct year table by OrderDate year
- Failure gate: if any year fails, stop processing remaining years for that clinic

### What changes:
- T-SQL MERGE statement → PySpark Delta MERGE operation
- Destination is a Lakehouse Delta table, not an Azure SQL table
- The logic expressed in PySpark, not C#

---

## 4. The Lookback Window

Currently the lookback window (how far back the SELECT reaches) is stored in
`tsk.tbl_Tasks2.WhereCondition` per task row. In Fabric it becomes a single
notebook parameter:

| Run type | Lookback |
|----------|---------|
| Normal daily | 15 days back from today |
| Month-end Friday | 90 days back |
| Manual backfill | Custom date range — passed as a parameter when triggering the pipeline |

The Fabric Pipeline passes this as a parameter to the notebook. No database row
needs to be updated. It is set in the Fabric UI when scheduling or triggering
a run.

---

## 5. Connectivity — The Critical Decision

This must be resolved before any development starts. It is the only thing
that can block the migration entirely.

### The Problem

Fabric runs in Microsoft's cloud. The clinic SAMMS SQL Servers are on-premises
inside clinic networks. Fabric cannot reach them directly over the internet.

### The Three Options

**Option A — On-premises Data Gateway (Recommended for existing infrastructure)**

Microsoft provides a gateway agent that you install on any machine that already
has network access to the clinic SQL Servers. The machine where BHGTaskRunner.exe
currently runs is the ideal candidate — it already has all the required network
access and SQL Server drivers installed.

- Install the On-premises Data Gateway on that machine
- Register it in your Fabric workspace
- Fabric routes all SAMMS connection requests through that gateway
- The clinic SQL Servers never need to be exposed to the internet
- This is the lowest-disruption path

**Option B — VNet Data Gateway**

If the clinic SQL Servers are already reachable from Azure via VPN or
ExpressRoute, Fabric can use a VNet-injected gateway to connect directly.
This is the enterprise-grade option but requires existing Azure networking
infrastructure.

**Option C — Push Architecture (Invert the flow)**

Keep a lightweight Python agent running on the existing on-premises server.
That agent reads from SAMMS and pushes data up to the Fabric Lakehouse via
the Fabric REST API or Azure Event Hub. The Fabric notebook only handles the
destination write side. This avoids the connectivity problem entirely but is
a more significant architectural change.

**Recommendation:** Option A. The machine running BHGTaskRunner.exe already
solves the connectivity problem. Install the gateway on it and Fabric inherits
all existing SAMMS access with no network changes required.

---

## 6. Connection String Security

The clinic SAMMS connection strings contain usernames and passwords. A decision
is needed on how to store them in the Lakehouse.

**Option A — Store in ctrl_location_cons as-is (simplest)**
- Full connection strings stored in the Delta table
- Security relies on Fabric workspace access controls
- Acceptable if workspace access is tightly controlled to a small team
- Matches the current approach in Azure SQL exactly

**Option B — Azure Key Vault (most secure)**
- ctrl_location_cons stores only server name and database name (no password)
- Passwords stored as Key Vault secrets (one per clinic or one shared secret
  if all clinics use the same credential)
- Notebook reads the base connection info from the Lakehouse and injects the
  password from Key Vault at runtime
- More setup work but follows security best practices

**Option C — Single shared credential**
- If all 80+ clinic SAMMS databases use the same SQL login username and password
  (just different server addresses), store only one secret in Key Vault
- ctrl_location_cons stores server + database only
- Simplest secure option if the infrastructure supports it

**Recommendation:** Confirm with the team whether all clinics share the same
SQL credential. If yes, Option C is the cleanest. If not, Option B.

---

## 7. Migration Phases

### Phase 1 — Infrastructure Setup (No code, no data movement)

**Goal:** Get the plumbing right before touching any ETL logic.

Steps:
1. Create the Fabric Workspace named "SAMMS-Orders-ETL"
2. Create the Lakehouse named "orders_lakehouse" inside that workspace
3. Resolve the connectivity decision (Section 5) and test it
   - Install On-premises Data Gateway on the BHGTaskRunner server if going with Option A
   - Confirm Fabric can reach at least one clinic SAMMS SQL Server through the gateway
4. Resolve the connection string security decision (Section 6)
5. Create the ctrl_location_cons Delta table in the Lakehouse
6. Populate ctrl_location_cons with the existing data from ctrl.tbl_LocationCons
   in Azure SQL — this is a one-time export/import

**Exit criteria:** Fabric can read ctrl_location_cons and connect to at least
one clinic SAMMS SQL Server.

---

### Phase 2 — Destination Table Setup (No code, no data movement)

**Goal:** Create all 13 year-partitioned Delta tables in the Lakehouse with the
correct schema before any data is written.

Steps:
1. Define the schema for orders_YYYY Delta tables
   (same columns as pats.tbl_Orders20XX in Azure SQL)
2. Create all 13 tables: orders_2016 through orders_2028
3. Confirm the schema matches the SAMMS source columns exactly
   (every column that the C# SaveOrders20XX methods read must be present)

Key columns to confirm exist in the schema:
- SiteCode, OrderNum, CltId (composite match key)
- RowChkSum (change detection)
- RowState, Active (soft-delete flags)
- LastModAt (timestamp of last ETL write)
- All 60+ order data columns (MedType, DateAdded, OrderDate, Doctor,
  EffectiveDate, ExpirationDate, Dose, Dose2, all day-of-week bits, Notes, etc.)

**Exit criteria:** All 13 Delta tables exist in the Lakehouse with the correct
schema and zero rows.

---

### Phase 3 — Single Clinic, Single Year Proof of Concept

**Goal:** Prove the end-to-end flow works for one clinic and one year before
scaling to all clinics and all years.

Steps:
1. Write the notebook to read ctrl_location_cons for one specific test clinic
2. Connect to that clinic's SAMMS SQL Server through the gateway
3. Run the fixed SELECT query for that clinic, filtering to one year only
   (e.g. 2024) using a 15-day lookback window
4. Inspect the returned data — confirm columns match, row counts look right
5. Perform the Delta MERGE into orders_2024
6. Validate the result:
   - Row count in orders_2024 matches what BHG_DR currently has for that clinic
   - A changed row (modify a value in SAMMS test data if available) triggers
     an UPDATE with the new values
   - A row not in the source extract gets RowState=0, Active=0

**Exit criteria:** One clinic, one year, data in the Lakehouse matches Azure SQL.

---

### Phase 4 — Single Clinic, All 13 Years

**Goal:** Add the year-split loop and failure gate for one clinic.

Steps:
1. Expand the notebook to split the source extract by OrderDate year
2. Route each year's rows to the correct orders_YYYY table
3. Implement the failure gate — if any year's MERGE fails, log the error
   and stop processing remaining years for that clinic
4. Run for the test clinic across all 13 years
5. Validate row counts for all 13 year tables against Azure SQL

**Exit criteria:** One clinic, all 13 years, all row counts match.

---

### Phase 5 — All Clinics, All Years

**Goal:** Scale the loop to all active clinics.

Steps:
1. Expand the notebook to loop over all rows in ctrl_location_cons
   where Active = true
2. Handle per-clinic errors without stopping the entire run
   (one clinic failing should not stop other clinics from processing)
3. Run the full loop — all clinics, all 13 years
4. Validate total row counts across all sites and all year tables
   against the current Azure SQL totals

**Exit criteria:** Full run completes. Total row counts match Azure SQL.
   
---

### Phase 6 — Pipeline Setup and Scheduling

**Goal:** Replace Windows Task Scheduler with a Fabric Pipeline.

Steps:
1. Create the Fabric Pipeline named "orders_daily"
2. Add the notebook as the pipeline activity
3. Configure the lookback window as a pipeline parameter:
   - Default: 15 days
   - The notebook reads this parameter at runtime
4. Set the daily schedule (match the current BHGTaskRunner schedule time)
5. Configure pipeline failure notifications (email or Teams alert)
6. Run the pipeline manually once to confirm it triggers the notebook correctly

**Exit criteria:** Pipeline runs successfully on demand and on schedule.

---

### Phase 7 — Parallel Validation Run

**Goal:** Run both systems simultaneously for a period and compare outputs.

Steps:
1. Continue running BHGTaskRunner.exe on its normal schedule (do not stop it)
2. Run the Fabric pipeline on the same schedule
3. After each run, compare:
   - Total row counts per year table between Azure SQL and Lakehouse
   - Specific clinic row counts for a sample of clinics
   - Any rows that differ in checksum between the two systems
4. Investigate and resolve any discrepancies
5. Run in parallel for a minimum of 5 business days (recommendation: 2 weeks)

**Exit criteria:** Zero discrepancies between Azure SQL totals and Lakehouse
totals across all clinics and all year tables over the validation period.

---

### Phase 8 — Cutover

**Goal:** Decommission the C# system and run Fabric exclusively.

Steps:
1. Get sign-off from senior team that parallel validation passed
2. Disable the Windows Task Scheduler job for BHGTaskRunner.exe arg=11
3. Monitor the first 3 Fabric-only runs closely
4. Confirm row counts are stable
5. After 2 weeks of stable Fabric-only runs, archive the C# code
   (do not delete — keep as reference for 90 days)

**Exit criteria:** Fabric has been the sole Orders ETL system for 2 weeks
with no incidents.

---

## 8. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Connectivity to on-prem SAMMS fails | Medium | High — blocks entire project | Resolve in Phase 1 before any other work |
| Delta MERGE performance slower than SQL MERGE | Low | Medium | Test in Phase 3 with a large clinic; optimize batch size if needed |
| Row count discrepancies during parallel run | Medium | Medium | Trace to specific clinic/year; likely a date filter or NULL handling difference |
| Connection string security gap in Lakehouse | Low | High | Decide on Key Vault approach in Phase 1 |
| SAMMS schema differs between clinics | Low | Low | Column-existence guards already in the current C# code — replicate in notebook |
| Fabric workspace access granted too broadly | Low | High | Lock down workspace permissions before Phase 3 |

---

## 9. What Is Not in Scope for This Migration

The following pipelines are separate and not affected by this migration:
- DartsSrv ETL (BHGTaskRunner arg=9)
- Dose ETL (BHGTaskRunner arg=10)
- Regional ETL P1/P2 (args 2 and 4)
- All other pipelines (args 1, 3, 5, 6, 7, 8)
- PHC ETL (separate runner entirely)

This migration covers only the Orders pipeline (arg=11,
SaveOrders2016 through SaveOrders2028). All other pipelines continue running
in C# on BHGTaskRunner unchanged until their own migration is planned.

---

## 10. Open Questions for Senior Review

The following decisions need sign-off before development begins:

1. **Connectivity approach** — On-premises Data Gateway on the existing
   BHGTaskRunner server (Option A)? Or a different connectivity path?

2. **Connection string security** — Full strings in Lakehouse (Option A),
   Key Vault (Option B), or shared credential (Option C)?

3. **Fabric workspace name and Lakehouse name** — Confirm naming conventions
   for the workspace and Lakehouse before creation.

4. **Parallel validation period** — Is 5 business days sufficient or do we
   need a longer parallel run before cutover?

5. **Who owns cutover sign-off** — Which role or person gives final approval
   to stop BHGTaskRunner.exe for Orders?

6. **Historical data migration** — The existing rows in pats.tbl_Orders2016
   through pats.tbl_Orders2028 in Azure SQL need to be copied into the
   Lakehouse Delta tables as a one-time seed load before Phase 3. This is
   a bulk copy of all historical records — confirm this is in scope and
   who is responsible for it.

---

## 11. Summary

| What changes | What stays the same |
|---|---|
| Runner: C# EXE → Fabric Notebook | Source databases: SAMMS SQL Server unchanged |
| Destination: Azure SQL → Fabric Lakehouse Delta | Year partitioning: 2016–2028 unchanged |
| Scheduling: Windows Task Scheduler → Fabric Pipeline | RowChkSum change detection unchanged |
| Config: task queue + column mapping → single ctrl table | RowState / Active soft-delete logic unchanged |
| Connectivity: direct → via Data Gateway | Failure gate behavior unchanged |
| Complexity: 6 moving parts → 4 things total | All 60+ order columns unchanged |
