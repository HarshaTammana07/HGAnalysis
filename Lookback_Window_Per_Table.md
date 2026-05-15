# BHG ETL — Lookback Window Patterns Per Table

**Source:** `BHGTaskRunner/Program.cs` (all switch cases) + Save* documentation files  
**Date:** April 2026

---

## What Does "Lookback Window" Mean?

The ETL does **NOT** pull all historical data from SAMMS on every daily run.  
That would be millions of rows per site, per table, every night — way too slow.

Instead, for each table it asks:  

> **"Give me only the records that were created or changed within the last X days."**

That X is the **lookback window**. It creates a date boundary:

```
WorkDate (today)  minus  X days  =  LookbackDate
                                          │
WHERE ModifiedDate >= LookbackDate   ←────┘
```

So **-15 days** means: pull only rows touched in the last 15 days.  
If a row has not changed in 30 days, it is NOT pulled. It stays untouched in Azure.

---

## What the Numbers Mean


| Value                                     | What It Means                                         | Why That Number                                                                                                                                 |
| ----------------------------------------- | ----------------------------------------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------- |
| **-15 days**                              | Pull records modified in the last 15 days             | Standard daily incremental — safe overlap for updates arriving slightly late                                                                    |
| **-90 days**                              | Pull records modified in the last 90 days             | End-of-month catch-up — billing/sign/update dates can lag by weeks, so a wider scan is done monthly                                             |
| **-200 days**                             | Pull records modified in the last ~6.5 months         | Special one-time correction window — used on specific override dates (e.g. `1/24/2025`) when a data quality issue was found going back that far |
| **-30 days**                              | Pull records modified in the last 30 days             | BAM assessments — monthly monitoring instrument, wider window needed                                                                            |
| **-515 days (~17 months)**                | Pull referral source records going ~17 months back    | Referral source can be added/changed long after the pre-admission form was filled                                                               |
| **-12 months**                            | Pull claim status records from the last 12 months     | Claims can take months to adjudicate — need full year window                                                                                    |
| **Year-based**                            | Pull records from start of year                       | Bills / Dose — year boundary is more meaningful than day count for financial data                                                               |
| **Full reload (-728250 days = all time)** | Pull ALL records — no date limit                      | Emergency/manual full reload flag — effectively 2000 years back                                                                                 |
| **No lookback / Static date**             | Fixed date hardcoded (e.g. `'12/31/2019'`)            | Data only relevant after a known cutover date                                                                                                   |
| **WhereCondition from DB**                | Lookback stored in `dms.tbl_MapAction.WhereCondition` | Dynamic — each table in metadata defines its own filter                                                                                         |


---

## How the Default -15 Gets Set

At the very start of the child task loop in `BHGTaskRunner/Program.cs`:

```csharp
int DaysBack = -15;  // line 136 — default for EVERY table

// This gets applied to strWhere via SelectConstructor:
.Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(DaysBack).ToShortDateString() + "'")
```

The `strWhere` coming from `dms.tbl_MapAction.WhereCondition` typically looks like:

```sql
LastModAt >= '@WorkDate'
```

Which becomes:

```sql
LastModAt >= '04/06/2026'    -- (today minus 15 days)
```

Only tables that **override** this default get a different window.

---

## Complete Table — Every Table and Its Lookback

### GROUP 1 — Standard -15 Days (the majority)

These tables use the default `DaysBack = -15` through `strWhere` which applies `@WorkDate` substitution.


| Table                                                   | Domain            | What the WHERE looks like                           |
| ------------------------------------------------------- | ----------------- | --------------------------------------------------- |
| `pats.tbl_3pArnote`                                     | Billing Notes     | `strWhere` with DaysBack=-15                        |
| `pats.tbl_3pClaimNote`                                  | Billing Notes     | `strWhere` with DaysBack=-15                        |
| `ctrl.tbl_3pSetup`                                      | 3P Billing Config | `strWhere` with DaysBack=-15                        |
| `pats.tbl_3pElig`                                       | 3P Eligibility    | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessment`                          | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentSummary`                   | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentDimensionFour`             | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentDimensionOneDisorder`      | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentDimensionFiveSubstanceUse` | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentDimensionTwo`              | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentDimensionThree`            | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AdmissionAssessmentDimensionSix`              | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessment`                                 | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentOccupational`                     | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentFamily`                           | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentLegal`                            | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentMentalHealth`                     | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentSubstanceUse`                     | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentPhysicalHealth`                   | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentSocial`                           | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ReAssessmentTreatment`                        | Assessment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_Appointments`                                 | Appointments      | `strWhere` with DaysBack=-15                        |
| `pats.tbl_AppointmentAttend`                            | Appointments      | `strWhere` with DaysBack=-15                        |
| `pats.tbl_OrientationChecklistNew`                      | Inventory         | `strWhere` with DaysBack=-15                        |
| `ctrl.tbl_InvType`                                      | Inventory         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_LiquidLog`                                    | Inventory         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_Bottle`                                       | Inventory         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_CheckIn`                                      | Daily Activity    | `strWhere` with DaysBack=-15                        |
| `pats.tbl_Claims`                                       | Billing           | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ClaimLineItem`                                | Billing           | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ClaimLineItemActivity`                        | Billing           | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ClientDemo1`                                  | Demographics      | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ClientDemo2`                                  | Demographics      | `strWhere` with DaysBack=-15                        |
| `pats.tbl_ClinicalOpiateWithdrawalScale`                | Clinical Scale    | `strWhere` with DaysBack=-15                        |
| `pats.tbl_Enrollment`                                   | Enrollment        | `strWhere` with DaysBack=-15                        |
| `pats.tbl_FeeSched`                                     | Reference         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_FMP`                                          | Financial         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_GlobalPayor`                                  | Reference         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_EandMFormMDM`                                 | Forms             | `strWhere` with DaysBack=-15                        |
| `pats.tbl_EandMFormPregnancy`                           | Forms             | `strWhere` with DaysBack=-15                        |
| `pats.tbl_TreatmentLevel`                               | Clinical          | `strWhere` with DaysBack=-15                        |
| `pats.tbl_BAMForm`                                      | BAM               | `strWhere` with DaysBack=-15                        |
| `pats.tbl_BAMScore`                                     | BAM               | `strWhere` with DaysBack=-15                        |
| `pats.tbl_TblDiag10`                                    | Diagnosis         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_PayerClient`                                  | Insurance         | `WhereCondition` with `@WorkDate` = DaysBack=-15    |
| `pats.tbl_PayerCltHistory`                              | Insurance         | `WhereCondition` with `@WorkDate` = DaysBack=-15    |
| `pats.tbl_pbi3payauth`                                  | Authorizations    | `strWhere` with DaysBack=-15                        |
| `pats.tbl_UAResult`                                     | UA / Lab          | `strWhere` with DaysBack=-15                        |
| `pats.tbl_UAResultDetail`                               | UA / Lab          | `strWhere` with DaysBack=-15                        |
| `pats.tbl_UASched`                                      | UA / Lab          | `strWhere` with DaysBack=-15                        |
| `pats.tbl_LabResult`                                    | Lab               | `strWhere` with DaysBack=-15                        |
| `pats.tbl_LabResultDetail`                              | Lab               | `strWhere` with DaysBack=-15                        |
| `pats.tbl_CustomQuestions`                              | Custom Forms      | `strWhere` with DaysBack=-15                        |
| `pats.tbl_CustomAnswers`                                | Custom Forms      | `strWhere` with DaysBack=-15                        |
| `ctrl.tbl_Clinic`                                       | Reference         | `strWhere` with DaysBack=-15                        |
| `ctrl.tbl_User`                                         | Reference         | `strWhere` with DaysBack=-15                        |
| `ctrl.tbl_UserSites`                                    | Reference         | `strWhere` with DaysBack=-15                        |
| `ctrl.tbl_Consents`                                     | Reference         | `strWhere` with DaysBack=-15                        |
| `pats.tbl_Codes`                                        | Reference         | `strWhere` with DaysBack=-15                        |
| `ayx.tbl_PreAdmission_V6`                               | Pre-Admission     | `strWhere` with DaysBack=-15                        |
| `pats.tbl_COWS_V6`                                      | Clinical Scale    | Per-site schema check; uses `strWhere` DaysBack=-15 |
| `pats.tbl_vw3pbill`                                     | Billing           | `strWhere` with DaysBack=-15                        |


---

### GROUP 2 — DartsSrv: THREE-TIER Dynamic Lookback (unique in the system)

`pats.tbl_DartsSrv_2014B4` through `pats.tbl_DartsSrv_2028`

**This is the only table in the system with a lookback that changes depending on WHEN the run happens.**

```csharp
// From BHGTaskRunner/Program.cs line 866–878
int offsetvalue = -15;   // default

if (WorkDate.DayOfWeek == DayOfWeek.Friday)                    // Is today a Friday?
{
    if (WorkDate.Month == WorkDate.AddDays(1).Month)           // Is tomorrow still the same month?
    {                                                          // → meaning: it's NOT the last Friday
        offsetvalue = -90;                                     // → extended scan = -90 days
        if (WorkDate.Date == DateTime.Parse("1/24/2025"))      // Special override date?
        {
            offsetvalue = -200;                                // → deep scan = -200 days
        }
    }
}

DateTime DartsDate = WorkDate.AddDays(offsetvalue);
```

> **Note the logic:** `-90` applies when today is a **Friday that is NOT the last Friday** of the month. On the **last Friday** of the month, the condition `WorkDate.Month == WorkDate.AddDays(1).Month` is `false` (tomorrow is a different month), so the -90 block is skipped and it falls back to **-15**. The comment in docs saying "-90 on month-end Friday" is actually the **non-last Friday**. Verify against DB for exact business intent.

**Five-column WHERE clause — unique to DartsSrv:**

```sql
WHERE dsClt IS NOT NULL
  AND (
    convert(date, dsDtStart)  >= DartsDate   OR
    convert(date, dsDtAdded)  >= DartsDate   OR
    convert(date, dsUpdate)   >= DartsDate   OR
    convert(date, dsBilled)   >= DartsDate   OR
    convert(date, dsSigDate)  >= DartsDate   OR
    dsClt <= 0
  )
```

Why 5 columns? A counseling session can be:

- **Created** on day 1 (`dsDtStart`, `dsDtAdded`)
- **Updated** later (`dsUpdate`)
- **Billed** weeks later (`dsBilled`)
- **Signed** days later (`dsSigDate`)

If you only checked one column, you'd miss rows that were updated (billed/signed) after the last run.


| Lookback Value | When It Applies                                         |
| -------------- | ------------------------------------------------------- |
| **-15 days**   | Any day that is NOT a Friday                            |
| **-90 days**   | On a Friday where tomorrow is the same month            |
| **-200 days**  | On specific hardcoded override dates (e.g. `1/24/2025`) |


---

### GROUP 3 — BAM (Brief Addiction Monitor): Fixed -30 Days

`pats.tbl_BriefAddictionMonitor`

```csharp
// Line 552–563
strCmd += " Where fCltID > 0 and convert(date, ...) >= '"
    + st.WorkDate.Value.AddDays(-30).ToShortDateString()
    + "' and fClinic not in (25, 100)";

rCodes = sd.SaveBAM(SrcDt, task.WorkDate.Value.AddDays(-30).Date, null);
```

**Hardcoded to -30 days** — not from `DaysBack` variable. This is a monthly monitoring instrument so 30 days is the natural assessment cycle.

---

### GROUP 4 — ClaimStatus: Fixed -12 Months

`ctrl.tbl_ClaimStatus`

```csharp
// Line 169
strCmd += " Where tpcbdtcreated >= '"
    + st.WorkDate.Value.AddMonths(-12).ToShortDateString()
    + "'";
```

**Always pulls last 12 months** of claim status records. Claims take months to adjudicate through payers, so a rolling 12-month window is required.

---

### GROUP 5 — PreAdmission Referral Source: -515 Days (~17 months)

`pats.tbl_PreAdmissionReferralSource`

```csharp
// Line 1348–1349
int mydaysback = DaysBack - 500;    // = -15 - 500 = -515 days
strCmd += " Where " + st.WhereCondition
    .Replace("@WorkDate", "'" + st.WorkDate.Value.AddDays(mydaysback).ToShortDateString() + "'");
```

**-515 days = approximately 17 months back.** Referral source information is often added or corrected well after the pre-admission form is first created, so a much wider window is required.


| Table                                 | Lookback                       |
| ------------------------------------- | ------------------------------ |
| `ayx.tbl_PreAdmission_V6`             | -15 days (standard)            |
| `pats.tbl_PreAdmissionReferralSource` | **-515 days** (DaysBack - 500) |


These are two separate child tasks for the same site — one gets a narrow window, the linked child table gets a very wide window.

---

### GROUP 6 — Bills: Year-Based + Optional Full Reload

`pats.tbl_Bills`

```csharp
// Line 519–533
int BillDaysBack = DaysBack;     // default -15

if (st.Reload.HasValue && st.Reload.Value)
{
    BillDaysBack = -728250;      // ← 2000 years = effectively ALL TIME
}

strCmd += " where year(billDate) >= " + WorkDate.AddDays(BillDaysBack).Year.ToString()
        + " and billdate <= '" + WorkDate.AddDays(12).ToShortDateString() + "'";
```


| Run Mode                            | What Happens                                                                                                    |
| ----------------------------------- | --------------------------------------------------------------------------------------------------------------- |
| **Normal daily** (`Reload = false`) | Pulls bills from start of **current year** (because `-15 days` keeps you in the same year for most of the year) |
| **Reload flag** (`Reload = true`)   | `BillDaysBack = -728250` → year becomes year 24 → effectively **all bills ever**                                |


Also: WHERE includes `billdate <= WorkDate + 12 days` — picks up **future-dated bills** entered up to 12 days ahead.

---

### GROUP 7 — Dose: Year-Based with Site-Specific Override

`pats.tbl_Dose`

**Standard sites (most sites):**

```csharp
strWhere = "(Year(dtDate) >= " + WorkDate.AddDays(DaysBack).AddYears(-1).Year
    + " or Year(dtMedDate) >= " + WorkDate.AddDays(DaysBack).AddYears(-1).Year + ")"
    + " and dtDate <= '" + WorkDate.AddDays(2).ToShortDateString()
    + "' and CltId is not null and dtDate >= '" + WorkDate.AddMonths(-1).ToShortDateString() + "'";
```

Effectively: **last 1 month** (`AddMonths(-1)`) with a year guard cross-check. Also includes 2 days into the future.

**Special sites (V10A, CBCO, V21, V10):**

```csharp
// Line 912–915
strWhere = "(Year(dtDate) >= " + WorkDate.AddDays(DaysBack).AddYears(-1).Year + "..."
    + "' and dtDate >= '" + WorkDate.AddMonths(-6).ToShortDateString() + "'";
```

Extended to **last 6 months** for these 4 specific sites — they have more historical corrections.


| Mode               | Lookback               | Sites                         |
| ------------------ | ---------------------- | ----------------------------- |
| Normal incremental | ~1 month rolling       | All standard sites            |
| Special sites      | ~6 months rolling      | V10A, CBCO, V21, V10          |
| Reload flag        | Full delete + reinsert | Any site when `Reload = true` |


---

### GROUP 8 — Orders: No Date Lookback — Uses WhereCondition from Database

`pats.tbl_Orders` (all year-partitioned tables)

```csharp
// Line 1171
strCmd += " Where " + st.WhereCondition;   // ← taken directly from dms.tbl_MapAction.WhereCondition
SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr);
// Then split by year in C# memory
```

**No DaysBack applied.** The WHERE clause is stored in `dms.tbl_MapAction.WhereCondition` in Azure. The C# code takes it verbatim. After the full pull, C# splits the DataTable by `OrderDate.Year` and sends each year's subset to its year-specific method (`SaveOrders2016`, `SaveOrders2017` … `SaveOrders2028`).

---

### GROUP 9 — Services: No Date Lookback — Uses WhereCondition

`pats.tbl_Services`

```csharp
// Line 1361
strCmd += " Where " + st.WhereCondition;   // verbatim from dms.tbl_MapAction
```

No date window — the filter is whatever is stored in the metadata table. Typically a simple `1=1` or a status-based filter (only active services).

---

### GROUP 10 — FormsSAMMSClient: Fixed Historical Cutoff Date

`pats.tbl_FormsSAMMSClient`

```csharp
// Line 1129/1138 — NOT a rolling window — fixed date
strWhere = " Where fscDATE > '12/31/2019'...";   // BHG
strWhere = " Where fscDATE > '12/31/2019' and fscsite = 1 ";   // PHC
```

**Static cutoff date `'12/31/2019'`** — always pulls everything since 2020, regardless of today's date. This is a global forms linkage table and requires all history since the system was standardized.

---

## Summary: All Lookback Groups at a Glance


| Group                     | Lookback                                                     | Tables                                            |
| ------------------------- | ------------------------------------------------------------ | ------------------------------------------------- |
| **1 — Standard**          | **-15 days**                                                 | ~55 tables — the vast majority                    |
| **2 — DartsSrv**          | **-15 / -90 / -200 days**                                    | All DartsSrv year tables (dynamic by day of week) |
| **3 — BAM**               | **-30 days**                                                 | `pats.tbl_BriefAddictionMonitor`                  |
| **4 — ClaimStatus**       | **-12 months**                                               | `ctrl.tbl_ClaimStatus`                            |
| **5 — PreAdminReferrals** | **-515 days (~17 months)**                                   | `pats.tbl_PreAdmissionReferralSource`             |
| **6 — Bills**             | **Year-based** (normal) / **All-time** (reload)              | `pats.tbl_Bills`                                  |
| **7 — Dose**              | **~1 month** (normal) / **~6 months** (V10A, CBCO, V21, V10) | `pats.tbl_Dose`                                   |
| **8 — Orders**            | **WhereCondition from DB** (no DaysBack)                     | All `pats.tbl_Orders_20XX`                        |
| **9 — Services**          | **WhereCondition from DB**                                   | `pats.tbl_Services`                               |
| **10 — FormsSAMMSClient** | **Static date '12/31/2019'**                                 | `pats.tbl_FormsSAMMSClient`                       |


---

## Visual Timeline — What Gets Pulled on Each Daily Run

```
Using today = April 21, 2026 as example

────────────────────────────────────────────────────────────────────────────────
  Jan 2020          Jan 2025    Jan 2026      Apr 6  Apr 21
     │                  │           │            │     │ TODAY
─────┼──────────────────┼───────────┼────────────┼─────┼───────────────────────
     │                  │           │            │     │
     │◄────────────────────────────────────────── FormsSAMMSClient (since 1/1/2020)
     │                  │           │            │     │
     │                  │◄──────────────────── ClaimStatus (-12 months)
     │                  │           │            │     │
     │                  │           │◄──────── PreAdminReferrals (-515 days, ~17mo)
     │                  │           │            │     │
     │                  │           │       ◄──────── DartsSrv normal (-15 days)
     │                  │           │  ◄──────────── DartsSrv Friday (-90 days)
     │                  │◄──────────────────────────── DartsSrv override (-200 days)
     │                  │           │            │     │
     │                  │           │        ◄──────── BAM (-30 days)
     │                  │           │            │     │
     │                  │           │            ◄──── All standard tables (-15 days)
     │                  │           │            │     │
     │                  │           │        ◄──────── Dose (~1 month)
     │                  │ ◄─────────────────────────── Dose special sites (~6 months)
────────────────────────────────────────────────────────────────────────────────
```

---

## Fabric Migration Implication

In Fabric, these lookback values must come from a **config table** — not from hardcoded C# integers.

**Proposed `meta.tbl_lookback_config` table:**


| column           | value (example)                                   |
| ---------------- | ------------------------------------------------- |
| `table_name`     | `pats.tbl_dartssrv`                               |
| `lookback_type`  | `DYNAMIC`                                         |
| `standard_days`  | `-15`                                             |
| `extended_days`  | `-90`                                             |
| `override_days`  | `-200`                                            |
| `override_dates` | `2025-01-24`                                      |
| `date_columns`   | `dsDtStart,dsDtAdded,dsUpdate,dsBilled,dsSigDate` |
| `notes`          | `5 column OR lookback`                            |



| `table_name`                          | `lookback_type`   | `standard_days` | `notes`                            |
| ------------------------------------- | ----------------- | --------------- | ---------------------------------- |
| `pats.tbl_briefaddictionmonitor`      | `FIXED`           | `-30`           | Always -30, no override            |
| `ctrl.tbl_claimstatus`                | `FIXED`           | `-365`          | Always -12 months                  |
| `pats.tbl_preadmissionreferralsource` | `EXTENDED`        | `-515`          | DaysBack - 500                     |
| `pats.tbl_bills`                      | `YEAR_BASED`      | `-15`           | year(billDate) logic + reload flag |
| `pats.tbl_dose`                       | `MONTH_BASED`     | `-30`           | Special sites get -180             |
| `pats.tbl_orders`                     | `WHERE_CONDITION` | `null`          | Stored in dms.tbl_MapAction        |
| `pats.tbl_services`                   | `WHERE_CONDITION` | `null`          | Stored in dms.tbl_MapAction        |
| `pats.tbl_formssammsclient`           | `STATIC_DATE`     | `null`          | Fixed: `'12/31/2019'`              |
| *all others*                          | `STANDARD`        | `-15`           | Default DaysBack                   |


---

*Derived from `BHGTaskRunner/Program.cs` all 131 switch cases and all Save*-Documentation files.*