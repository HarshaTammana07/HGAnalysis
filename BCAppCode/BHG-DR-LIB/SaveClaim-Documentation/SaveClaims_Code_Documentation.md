
SaveClaims.cs — Code-Level Documentation
BHG-DR-LIB | SaveData (partial class)
________________________________________

INTEGRATION METADATA
________________________________________

Name            : Claims EF Core Upsert Load (SaveClaims / SaveClaimLineItem / SaveClaimLineItemActivity)
Module          : Insurance / Billing / Revenue Cycle
Layer           : Target Load / Incremental Upsert Integration
Source System   : SAMMS
Source DB       : SQL Server (clinic local)
Source Tables   : dbo.tbl3pClaim
                  dbo.tbl3pClaimLineItem
                  dbo.tbl3pClaimLineItemActivity
Target DB       : BHG_DR (Azure SQL)
Target Tables   : pats.tbl_Claims
                  pats.tbl_ClaimLineItem
                  pats.tbl_ClaimLineItemActivity
Load Type       : Incremental Upsert via EF Core
Load Key Columns: SiteCode + tpcID (Claims)
                  SiteCode + tpcliID (ClaimLineItem)
                  SiteCode + liaID (ClaimLineItemActivity)
Change Detection: RowChkSum = CHECKSUM() across all mapped columns
Frequency       : Daily (incremental) or As-Needed (yearly backfill / reconciliation)
Schedule        : BHGTaskRunner.exe 8 → SAMMS-ETL-INV (EF path for VBRA/VMIN/VWBY/VBRP)
                  SelectConstructor backfill path / manual run for other scenarios
Parent          : BHGTaskRunner Program.cs (case "pats.tbl_claims")
                  SelectConstructor.cs (case "tbl_claims")
                  PHC/Program.cs (site VBRA special handling)
Downstream      : Billing reconciliation, denial analysis, revenue cycle reporting
Connection      : C# + EF Core (BHG_DRContext)
Server/DB/API   : Clinic SAMMS SQL Server (source), Azure SQL BHG_DR (target)
Owner           : BHG / Data Integration Team
Status          : Active / Existing
Folder Path     : BCAppCode/BHG-DR-LIB/SaveClaims.cs
Analysis Doc    : Claims_ETL_Complete_Documentation.md
________________________________________

FLOW DIAGRAMS — PER METHOD
________________________________________

SaveClaims() — Claim Header Upsert
------------------------------------

START
  │
  ├─ Create EF context (if null)
  │
  ├─ Load existing Azure rows for SiteCode
  │   │
  │   ├─ yearly=true  → Load ALL claims for site
  │   │                  Reset RowState=false for current year rows
  │   │
  │   └─ yearly=false → Load ALL claims for site
  │                      (no RowState reset)
  │
  ├─ No rows in Azure? → Set AllNewRows = true
  │
  ├─ For each row in DataTable (SAMMS source):
  │   │
  │   ├─ Extract tpcID + RowChkSum from DataRow
  │   │
  │   ├─ AllNewRows = true? ───YES──► Create new TblClaims object
  │   │                               RowsIns += 1
  │   │                               NewRow = true
  │   └─ NO
  │       │
  │       ├─ Find by tpcID in in-memory list
  │       │
  │       ├─ Not found? ───────YES──► Create new TblClaims object
  │       │                           RowsIns += 1
  │       │                           NewRow = true
  │       └─ Found
  │           │
  │           └─ RowsUpd += 1
  │
  ├─ NewRow = true OR RowChkSum changed?
  │   │
  │   ├─ YES ──► Map ALL columns from DataRow to EF object
  │   │           Apply type transformations:
  │   │             string  → .ToString()           (F1–F33 CMS fields, status, payer)
  │   │             int?    → int.Parse() if Length > 0   (ClaimBatchID, ClaimType, SiteID)
  │   │             int     → int.Parse()            (tpccltID)
  │   │             datetime? → DateTime.Parse() if Length > 7  (DtmAdded, CreatedDate)
  │   │             date    → DateTime.Parse().Date   (tpcWKSTART)
  │   │           Set RowState = true
  │   │           Set LastModAt = DateTime.Now
  │   │           Set RowChkSum = new value
  │   │           If NewRow → db.TblClaims.Add(clm)
  │   │
  │   └─ NO (checksum unchanged, row exists)
  │       │
  │       └─ Set RowState = true   (keep active — not soft-deleted)
  │           Set LastModAt = DateTime.Now
  │           (no data columns written)
  │
  └─ db.SaveChanges()
      │
      ├─ Commits UPDATEs for modified existing rows (EF change tracking)
      └─ Commits INSERTs for all new rows added via Add()


SaveClaimLineItem() — Line Item Upsert
----------------------------------------

START
  │
  ├─ Create EF context (if null)
  │
  ├─ Auto-detect load scope from wrkdt:
  │   │
  │   ├─ wrkdt = Jan 1 of year? ──YES──► YEARLY MODE
  │   │                                   Load items where SiteCode=sc AND year=wrkdt.Year
  │   │                                   Reset RowState=false for all loaded rows
  │   │
  │   └─ NO ──────────────────────────► DATE MODE
  │                                      Load items where SiteCode=sc AND date=wrkdt.Date
  │
  ├─ No rows? → AllNewRows = true, NewRow = true
  │
  ├─ For each row in DataTable:
  │   │
  │   ├─ Extract tpcliID + RowChkSum
  │   │
  │   ├─ AllNewRows? ──YES──► New TblClaimLineItem, RowsIns += 1, NewRow = true
  │   └─ NO → Find by tpcliID in memory
  │               Not found → New object, RowsIns += 1, NewRow = true
  │               Found     → RowsUpd += 1
  │
  ├─ NewRow OR RowChkSum changed?
  │   │
  │   ├─ YES ──► Map ALL columns:
  │   │           int?    → int.Parse()    if Length > 0  (tpcliTPCID, units, dx1–4, dsid, mg)
  │   │           datetime? → DateTime.Parse() if Length > 7  (DtmService, DtmAdded, DtmServiceTo)
  │   │           date    → DateTime.Parse().Date if Length > 3  (VoidDT)
  │   │           decimal → decimal.Parse() if Length > 0  (AmtCharge, UnitFee)
  │   │           bool?   → bool.Parse()   if Length > 0  (Void)
  │   │           string  → .ToString()    (CPT, modifier, NDC, POS, diagnosis, etc.)
  │   │           If NewRow → db.TblClaimLineItem.Add(li)
  │   │
  │   └─ NO ──► RowState = true, LastModAt = Now
  │
  └─ db.SaveChanges()


SaveClaimLineItemActivity() — Payment/Adjustment Activity Upsert
------------------------------------------------------------------

START
  │
  ├─ Create EF context (if null)
  │
  ├─ Auto-detect load scope from wrkdt:
  │   │
  │   ├─ wrkdt = Jan 1 of year? ──YES──► YEARLY MODE
  │   │                                   Load activity where SiteCode=sc AND year=wrkdt.Year
  │   │                                   Reset RowState=false for all loaded rows
  │   │
  │   └─ NO ──────────────────────────► DATE MODE
  │                                      Load activity where SiteCode=sc AND date=wrkdt.Date
  │
  ├─ No rows? → AllNewRows = true
  │
  ├─ For each row in DataTable:
  │   │
  │   ├─ Extract liaID + RowChkSum
  │   │
  │   ├─ AllNewRows? ──YES──► New TblClaimLineItemActivity, RowsIns += 1, NewRow = true
  │   └─ NO → Find by liaID in memory
  │               Not found → New object, RowsIns += 1, NewRow = true
  │               Found     → RowsUpd += 1
  │
  ├─ NewRow OR RowChkSum changed?
  │   │
  │   ├─ YES ──► Map ALL columns:
  │   │           int?    → int.Parse()    if Length > 0  (liaTPCLIID, tprbID, billID)
  │   │           datetime? → DateTime.Parse() if Length > 7  (liaDtm)
  │   │           decimal → decimal.Parse() if Length > 0  (paidIns, contAdj, genAdj,
  │   │                                                      copay, deduc, client,
  │   │                                                      coins, liaAmt)
  │   │           bool?   → bool.Parse()   if Length > 0  (bitNoteOnly, pending)
  │   │           string  → .ToString()    (user, desc, text, adjReason, action1/2,
  │   │                                     adjContract, adjGeneral, ANSI1/2, ANSIMod1/2,
  │   │                                     dbnotes)
  │   │           If NewRow → db.TblClaimLineItemActivity.Add(lia)
  │   │
  │   └─ NO ──► RowState = true, LastModAt = Now
  │
  └─ db.SaveChanges()


CleanupDeletedData() — Soft Delete Reconciliation
---------------------------------------------------

START
  │
  ├─ Create EF context (if null)
  │
  ├─ Switch on tblName:
  │
  ├─ "claims"
  │   │
  │   ├─ Load ALL Azure claims for SiteCode into memory
  │   ├─ Set RowState = false for EVERY row  (assume all deleted)
  │   ├─ For each ID in source DataTable:
  │   │       Find Azure row by tpcID
  │   │       Found? → Set RowState = true  (still active in source)
  │   │       Not found in Azure? → skip (will be inserted by SaveClaims later)
  │   └─ (continue to SaveChanges)
  │
  ├─ "claimlineitem"
  │   │
  │   ├─ Load ALL Azure line items for SiteCode into memory
  │   ├─ Set RowState = false for EVERY row
  │   ├─ For each row in source DataTable:
  │   │       Find Azure row by TpcliId + TpcliDsid + TpcliTpcid  (3-column match)
  │   │       Found? → Set RowState = true
  │   └─ (continue to SaveChanges)
  │
  ├─ "claimlineitemactivity"
  │   │
  │   ├─ Load ALL Azure activity for SiteCode into memory (ordered by LiaId)
  │   ├─ Set RowState = false for EVERY row
  │   ├─ For each row in source DataTable:
  │   │       Find Azure row by LiaId + LiaTpcliid  (2-column match)
  │   │       Note: liaDtm is extracted but NOT used in the lookup
  │   │       Found? → Set RowState = true
  │   └─ (continue to SaveChanges)
  │
  └─ db.SaveChanges()
      │
      ├─ Rows found in source  → remain RowState = true
      └─ Rows NOT in source    → remain RowState = false  (soft-deleted)
________________________________________

FILE OVERVIEW

File     : BCAppCode/BHG-DR-LIB/SaveClaims.cs
Namespace: BHG_DR_LIB
Class    : SaveData (partial)
Lines    : 650
Usings   : BHG_DR_LIB.Models, System, System.Collections.Generic, System.Data, System.Linq

This file is part of the SaveData partial class and contains four public methods that handle
the EF Core upsert and reconciliation operations for the three insurance claim tables:

    Method                      Destination Table               Lines
    ------                      -----------------               -----
    SaveClaims()                pats.tbl_Claims                 11–217
    SaveClaimLineItem()         pats.tbl_ClaimLineItem          218–388
    SaveClaimLineItemActivity() pats.tbl_ClaimLineItemActivity  389–553
    CleanupDeletedData()        pats.tbl_Claims / LineItem /    555–647
                                       LineItemActivity

All four methods follow the same structural pattern:
    1. Initialize result object (RCodes)
    2. Create EF context if not provided
    3. Load existing Azure rows into memory (scoped by site/date)
    4. Loop through source DataTable rows, compare RowChkSum, map columns
    5. Commit with db.SaveChanges()
    6. Return RCodes with success/failure and row counts
________________________________________

RETURN TYPE — RCodes

All methods return RCodes (except CleanupDeletedData which returns bool).

RCodes properties used in this file:

    Property            Type        Set in SaveClaims.cs        Meaning
    --------            ----        --------------------        -------
    IsResult            bool        true on success             Overall pass/fail flag
    RowsProcessed       int         = tbl.Rows.Count            Rows received from source
    RowsIns             int         incremented per INSERT      Count of new rows added
    RowsUpd             int         incremented per UPDATE      Count of rows updated
    ExceptMsg           string      = e.Message                 Exception message text
    ExceptInnerMsg      string      = e.InnerException.Message  Inner exception message
________________________________________

METHOD 1 — SaveClaims()
Lines 11–217

SIGNATURE
    public RCodes SaveClaims(
        DataTable tbl,                  // Source rows from SAMMS dbo.tbl3pClaim
        string sc,                      // SiteCode (e.g. "VBRA", "VMIN")
        DateTime wrkdt,                 // Work date — used to scope yearly reset
        bool yearly,                    // true = full-year mode, false = date mode
        Models.BHG_DRContext db)        // EF Core context (created internally if null)

PURPOSE
Upserts CMS-1500 claim header records from a SAMMS DataTable into pats.tbl_Claims in Azure.
Performs RowChkSum-based change detection — only writes columns that have actually changed.
Also manages RowState to track which claims are still active vs soft-deleted in source.

INITIALIZATION (lines 13–17)
    RCodes res = new RCodes { IsResult = true, RowsProcessed = tbl.Rows.Count };
    if (db == null) { db = new Models.BHG_DRContext(); }

RowsProcessed is set immediately to the incoming DataTable row count.
EF context is created lazily if the caller passes null — allows both standalone use
and shared-context use (when the caller is managing a long-lived DbContext).

LOADING EXISTING AZURE ROWS (lines 19–44)

Two modes based on the yearly parameter:

--- YEARLY MODE (yearly == true) ---
    claims = db.TblClaims
        .Where(x => x.SiteCode == sc)
        .ToList();

    foreach (TblClaims c in claims)
    {
        if (c.TpcCreatedDate.Value.Year == wrkdt.Year)
            c.RowState = false;
    }

ALL claims for this site are loaded into memory (no year filter in the LINQ query).
Then a foreach loop sets RowState=false ONLY for records whose TpcCreatedDate is in
the same year as wrkdt.

Why this matters:
- The entire site's claims list is in memory for fast lookup (AllNewRows shortcut and
  per-row LINQ search both work against this in-memory list, not the database).
- The RowState reset for the current year prepares for soft-delete detection:
  after the loop, any current-year claim that was NOT seen in SAMMS will remain false.

--- NON-YEARLY MODE (yearly == false) ---
    claims = db.TblClaims
        .Where(x => x.SiteCode == sc)
        .ToList();

Same query — loads all claims for the site. The RowState reset is skipped entirely.
This mode is used when loading claims by a specific date rather than a full year sweep.

ALL-NEW DETECTION (line 45)
    if (claims.Count == 0) { AllNewRows = true; }

If no Azure rows exist for this site, the AllNewRows flag is set.
When AllNewRows=true, all per-row lookups are skipped — every row from source
is treated as a new INSERT. This is a significant performance optimization for
first-time loads of a site.

MAIN LOOP (lines 46–204)
    foreach (DataRow r in tbl.Rows)

Iterates every row in the SAMMS DataTable.

STEP A — Extract key values and new checksum
    int inttpcID = int.Parse(r["tpcid"].ToString())
    int rcs      = int.Parse(r["rowchksum"].ToString())

Both values use int.Parse(ToString()) — the DataTable stores all values as objects,
so ToString() is called first, then parsed. No null check is done here because tpcID
and rowchksum are guaranteed non-null from the source SELECT.

STEP B — Find or create EF object

    IF AllNewRows:
        clm = new Models.TblClaims { SiteCode = sc, TpcId = inttpcID, RowChkSum = rcs }
        NewRow = true
        res.RowsIns += 1

    ELSE:
        clm = claims.Where(x => x.TpcId == inttpcID).FirstOrDefault()
        IF clm == null:
            clm = new Models.TblClaims { SiteCode = sc, TpcId = inttpcID, RowChkSum = rcs }
            NewRow = true
            res.RowsIns += 1
        ELSE:
            res.RowsUpd += 1  // existing row — may or may not actually write columns

Note: RowsUpd is incremented for ALL existing rows, including those whose checksum
has not changed (no-write rows). It is a count of "rows seen that existed in Azure",
not a count of "rows that were actually updated."

STEP C — Column mapping block (lines 78–198)
Entered ONLY when: NewRow == true OR rcs != clm.RowChkSum

    if (NewRow || (rcs != clm.RowChkSum))

If the checksum matches and the row is not new, this entire block is skipped.
The row is still "touched" (RowState and LastModAt are set in the ELSE block below),
but no data columns are written.

Inside the column mapping block, every field from the DataTable is assigned to the
EF object. The transformations applied are:

    clm.LastModAt = RunDT;      // DateTime.Now captured once at method start
    clm.RowState  = true;       // Mark as active (seen in this run)
    clm.RowChkSum = int.Parse(r["RowChkSum"].ToString());  // Note: re-read here (matches rcs)

DATA TYPE TRANSFORMATIONS — SaveClaims():

Most fields are plain string copies:
    clm.TpcStrStatus = r["tpcstrstatus"].ToString()
    clm.TpcStrPayer  = r["tpcstrpayer"].ToString()
    clm.TpcStrAdded  = r["tpcstrAdded"].ToString()
    clm.F10oth       = r["f10oth"].ToString()
    ... (all F# CMS-1500 fields follow the same pattern)

Integers with no null guard:
    clm.TpccltId         = int.Parse(r["tpccltid"].ToString())
    clm.TpcClaimBatchId  = int.Parse(r["tpcclaimbatchid"].ToString())   ← guarded (see below)

Integer fields WITH null/empty guard:
    if (r["tpcclaimbatchid"].ToString().Length > 0)
    {
        clm.TpcClaimBatchId = int.Parse(r["tpcclaimbatchid"].ToString());
    }
    // If empty string → field left at whatever it was (null for new rows)

    if (r["tpcclaimtype"].ToString().Length > 0)
    {
        clm.TpcClaimType = int.Parse(r["tpcClaimtype"].ToString());
    }

    if (r["SiteId"].ToString().Length > 0)
    {
        clm.SiteId = int.Parse(r["SiteID"].ToString());
    }

Why Length > 0?
These are nullable int columns (int?) in the EF model. The DataTable may return an
empty string if the source SQL Server value is NULL. int.Parse("") throws a FormatException,
so the guard prevents that. Note: the guard is Length > 0 not Length > 7 (no minimum
length requirement unlike DateTime fields).

DateTime fields WITH length guard (> 7):
    if (r["tpcDtmAdded"].ToString().Length > 7)
    {
        clm.TpcDtmAdded = DateTime.Parse(r["tpcDtmAdded"].ToString());
    }

    if (r["tpcCreatedDate"].ToString().Length > 7)
    {
        clm.TpcCreatedDate = DateTime.Parse(r["tpcCreatedDate"].ToString());
    }

Why Length > 7?
A valid datetime string ("1/1/2020") has at minimum 8 characters. Using Length > 7
ensures the string is at least plausibly a date before attempting DateTime.Parse().
An empty string (""), NULL-converted string (""), or obviously short junk value is
skipped rather than causing a parse exception. If the guard fails, the nullable
DateTime? field remains null (or retains its previous value for existing rows).

Date field WITH .Date stripping:
    if (r["tpcwkstart"].ToString().Length > 0)
    {
        clm.TpcWkstart = DateTime.Parse(r["tpcwkstart"].ToString()).Date;
    }

tpcWKSTART is mapped as a date (not datetime) in the EF model. Calling .Date strips
the time component (sets time to 00:00:00) to ensure the value stored matches the
SQL Server date type exactly.

All CMS-1500 F# fields (f1id through f33phone) are varchar in both source and destination.
No type transformation is needed — they are all plain ToString() copies:
    clm.F1id          = r["f1id"].ToString()
    clm.F2name        = r["f2name"].ToString()
    clm.F3dob         = r["f3dob"].ToString()
    clm.F3sex         = r["f3sex"].ToString()
    ... (same for all ~60 F# fields)

These fields store the raw text values from the CMS-1500 paper form as printed —
amounts like "150.00" are stored as strings, not as decimal. No numeric parsing.

ADD vs SKIP for new rows:
    if (NewRow || AllNewRows)
    {
        NewRow = false;
        db.TblClaims.Add(clm);
    }
    // If not new (existing row with changed checksum), EF tracks the change automatically
    // because the object was loaded from db.TblClaims — no explicit .Update() needed.

STEP D — Rows with unchanged checksum (ELSE block, lines 199–203)
    else
    {
        clm.RowState  = true;
        clm.LastModAt = RunDT;
    }

Even when the checksum matches (no data change), RowState is set to true and
LastModAt is updated. This ensures:
- The row is not marked as soft-deleted (RowState stays true)
- The LastModAt timestamp reflects the most recent ETL run that confirmed the record

COMMIT (line 205)
    db.SaveChanges();

A single SaveChanges() call commits all changes accumulated during the loop:
- INSERTs for all new rows added via db.TblClaims.Add()
- UPDATEs for all modified existing rows (EF change tracking detects the property changes)

EF Core generates individual INSERT/UPDATE statements per row — not a bulk operation.

ERROR HANDLING (lines 207–214)
    catch (Exception e)
    {
        res.IsResult = false;
        res.ExceptMsg = e.Message;
        res.ExceptInnerMsg = e.InnerException.Message;
        Console.WriteLine(e.Message);
        Console.WriteLine(e.InnerException.Message);
    }

If any exception occurs (parse error, EF validation, SQL timeout, etc.):
- res.IsResult is set to false
- Both message levels are captured (message + inner exception)
- Both are printed to Console (visible in BHGTaskRunner output log)
- The entire batch is rolled back — SaveChanges() was not called, so no partial writes

RETURN
    return res;
________________________________________

METHOD 2 — SaveClaimLineItem()
Lines 218–388

SIGNATURE
    public RCodes SaveClaimLineItem(
        DataTable tbl,                  // Source rows from SAMMS dbo.tbl3pClaimLineItem
        string sc,                      // SiteCode
        DateTime wrkdt,                 // Work date — controls yearly vs date scoping
        Models.BHG_DRContext db)        // EF Core context (created if null)

PURPOSE
Upserts claim line item records into pats.tbl_ClaimLineItem. Each line item represents
one CPT-coded service billed under a parent claim. Multiple line items can exist per claim.

KEY DIFFERENCE FROM SaveClaims():
No "yearly" boolean parameter. Instead, the method auto-detects yearly vs daily mode
by comparing wrkdt to the first day of its own year:

LOADING EXISTING AZURE ROWS (lines 236–246)

    if (wrkdt.Date == DateTime.Parse("1/1/" + wrkdt.Year.ToString()))
    {
        // YEARLY MODE — work date is exactly January 1st of the year
        items = db.TblClaimLineItem
            .Where(x => x.SiteCode == sc && x.TpcliDtmAdded.Value.Year == wrkdt.Year)
            .ToList();
        foreach (TblClaimLineItem t in items) { t.RowState = false; }
    }
    else
    {
        // DATE MODE — work date is any other day
        items = db.TblClaimLineItem
            .Where(x => x.SiteCode == sc
                     && x.TpcliDtmAdded.Value.Date == wrkdt.Date)
            .ToList();
    }

Important contrast with SaveClaims:
- SaveClaims loads ALL rows for the site (no year filter in the LINQ), then resets in a foreach
- SaveClaimLineItem scopes the LINQ query itself by year or by date — fewer rows loaded

Date comparison uses .Date (time-stripped) for reliable matching when TpcliDtmAdded
may contain a time component.

ALL-NEW DETECTION (lines 247–252)
    if (items.Count == 0)
    {
        AllNewRows = true;
        NewRow = true;    // ← differs from SaveClaims: NewRow is also pre-set true here
    }

Note: Both AllNewRows and NewRow are set to true here, whereas in SaveClaims only
AllNewRows is set. The effect is the same since the loop sets NewRow=true for AllNewRows,
but this is a minor code difference.

MAIN LOOP — Key identification:
    int intClt = int.Parse(r["tpcliid"].ToString())    // PK: line item ID
    int rcs    = int.Parse(r["RowChkSum"].ToString())

Variable is named intClt but it holds the line item ID (tpcliid) — the name is a
copy-paste artifact from client-related code.

DATA TYPE TRANSFORMATIONS — SaveClaimLineItem():

Integer fields WITH null/empty guard (Length > 0):
    if (r["tpclitpcid"].ToString().Length > 0)
        li.TpcliTpcid = int.Parse(r["tpclitpcid"].ToString())      // parent claim ID
    if (r["tpcliintunits"].ToString().Length > 0)
        li.TpcliIntUnits = int.Parse(r["tpcliintunits"].ToString())  // units billed
    if (r["tpcliintdx1"].ToString().Length > 0)
        li.TpcliIntDx1 = int.Parse(r["tpcliintdx1"].ToString())     // dx pointer 1
    if (r["tpcliintdx2"].ToString().Length > 0)
        li.TpcliIntDx2 = int.Parse(r["tpcliintdx2"].ToString())     // dx pointer 2
    if (r["tpcliintdx3"].ToString().Length > 0)
        li.TpcliIntDx3 = int.Parse(r["tpcliintdx3"].ToString())     // dx pointer 3
    if (r["tpcliintdx4"].ToString().Length > 0)
        li.TpcliIntDx4 = int.Parse(r["tpcliintdx4"].ToString())     // dx pointer 4
    if (r["tpclidsid"].ToString().Length > 0)
        li.TpcliDsid = int.Parse(r["tpclidsid"].ToString())         // linked DartsSrv session
    if (r["tpcliintmg"].ToString().Length > 0)
        li.TpcliIntMg = int.Parse(r["tpcliintmg"].ToString())       // milligrams (MAT)

DateTime fields WITH length guard (> 7):
    if (r["tpcliDtmService"].ToString().Length > 7)
        li.TpcliDtmService = DateTime.Parse(r["tpcliDtmService"].ToString())
    if (r["tpcliDtmAdded"].ToString().Length > 7)
        li.TpcliDtmAdded = DateTime.Parse(r["tpcliDtmAdded"].ToString())
    if (r["tpcliDtmServiceTo"].ToString().Length > 7)
        li.TpcliDtmServiceTo = DateTime.Parse(r["tpcliDtmServiceTo"].ToString())

Date field with .Date stripping:
    if (r["tpclivoiddt"].ToString().Length > 3)
        li.TpclivoidDt = DateTime.Parse(r["tpclivoiddt"].ToString()).Date

Note: Length > 3 is used here (not > 7) for the void date. This is a looser guard —
any string longer than 3 characters is attempted as a date parse. TpcliVoidDt is a
date-only column, so .Date strips the time component.

Decimal fields WITH null/empty guard:
    if (r["tpcliAmtcharge"].ToString().Length > 0)
        li.TpcliAmtCharge = decimal.Parse(r["tpcliAmtcharge"].ToString())
    if (r["tpcliunitfee"].ToString().Length > 0)
        li.TpcliUnitfee = decimal.Parse(r["tpcliunitfee"].ToString())

decimal.Parse is used instead of int.Parse for monetary/fee values.
The Length > 0 guard prevents FormatException on null/empty source values.

Boolean (bit) field WITH null/empty guard:
    if (r["tpclivoid"].ToString().Length > 0)
        li.TpcliVoid = bool.Parse(r["tpclivoid"].ToString())

bool.Parse expects "True" or "False" (case-insensitive). SQL Server bit columns
come through as "True"/"False" when converted via ToString(). If the value is DBNull,
ToString() returns "" — the Length > 0 guard prevents parse failure.

Plain string copies (no transformation):
    li.TpcliTxtService   = r["tpclitxtservice"].ToString()
    li.TpcliStrAdded     = r["tpclistradded"].ToString()
    li.TpcliStrCpt       = r["tpclistrcpt"].ToString()
    li.TpcliStrModifier  = r["tpclistrModifier"].ToString()
    li.TpcliStrNdc       = r["tpclistrndc"].ToString()
    li.TpcliStrPos       = r["tpclistrpos"].ToString()
    li.TpcliDiagnosis    = r["tpclidiagnosis"].ToString()
    li.TpcliPayerClaimId = r["tpclipayerclaimid"].ToString()
    li.TpcliProviderId   = r["tpcliproviderid"].ToString()
    li.TpclivoidUser     = r["tpclivoiduser"].ToString()
    li.TpcliDbnotes      = r["tpclidbnotes"].ToString()

COMMIT AND ERROR HANDLING — same pattern as SaveClaims():
    db.SaveChanges();
    // One SaveChanges() call for all rows processed in the loop

    catch (Exception e)
    {
        res.IsResult = false;
        res.ExceptMsg = e.Message;
        res.ExceptInnerMsg = e.InnerException.Message;
        Console.WriteLine(e.Message);
        if (e.InnerException != null)
            Console.WriteLine(e.InnerException.Message);
        Console.WriteLine(li.SiteCode + ", " + li.TpcliId.ToString());
        // ← Prints the last line item being processed when the error occurred
    }

Extra diagnostics: Unlike SaveClaims, this method prints the SiteCode + TpcliId
of the row being processed when an exception occurs. This helps identify which
specific line item caused the failure during debugging.

Also note: InnerException is null-checked before printing (unlike SaveClaims which
calls .Message directly without null guard — a potential NullReferenceException risk).
________________________________________

METHOD 3 — SaveClaimLineItemActivity()
Lines 389–553

SIGNATURE
    public RCodes SaveClaimLineItemActivity(
        DataTable tbl,                  // Source rows from SAMMS dbo.tbl3pClaimLineItemActivity
        string sc,                      // SiteCode
        DateTime wrkdt,                 // Work date — controls yearly vs date scoping
        Models.BHG_DRContext db)        // EF Core context (created if null)

PURPOSE
Upserts payment and adjustment activity records into pats.tbl_ClaimLineItemActivity.
Each row represents one financial event (payment, denial, adjustment, copay posting)
against a line item.

LOADING EXISTING AZURE ROWS (lines 405–418) — same auto-detect pattern as SaveClaimLineItem:

    if (wrkdt.Date == DateTime.Parse("1/1/" + wrkdt.Year))   // Note: no .ToString() on Year
    {
        // YEARLY MODE
        clia = db.TblClaimLineItemActivity
            .Where(x => x.SiteCode == sc && x.LiaDtm.Value.Year == wrkdt.Year)
            .ToList();
        foreach (TblClaimLineItemActivity c in clia) { c.RowState = false; }
    }
    else
    {
        // DATE MODE
        clia = db.TblClaimLineItemActivity
            .Where(x => x.SiteCode == sc && x.LiaDtm.Value.Date == wrkdt.Date)
            .ToList();
    }

Yearly detection scopes by LiaDtm (the activity date) — analogous to TpcliDtmAdded
for line items and TpcCreatedDate for claim headers.

MAIN LOOP — Key identification:
    int liaid = int.Parse(r["liaid"].ToString())     // PK: activity ID
    int rcs   = int.Parse(r["rowchksum"].ToString())

DATA TYPE TRANSFORMATIONS — SaveClaimLineItemActivity():

Integer fields WITH guard:
    if (r["liatpcliid"].ToString().Length > 0)
        lia.LiaTpcliid = int.Parse(r["liatpcliid"].ToString())    // parent line item ID
    if (r["tprbid"].ToString().Length > 0)
        lia.TprbId = int.Parse(r["tprbid"].ToString())             // remittance batch ID
    if (r["billid"].ToString().Length > 0)
        lia.BillId = int.Parse(r["billid"].ToString())             // billing run ID

DateTime field WITH guard:
    if (r["liadtm"].ToString().Length > 7)
        lia.LiaDtm = DateTime.Parse(r["liadtm"].ToString())        // activity date/time

Boolean fields WITH guard:
    if (r["liabitnoteonly"].ToString().Length > 0)
        lia.LiaBitNoteOnly = bool.Parse(r["liabitnoteonly"].ToString())  // note-only flag
    if (r["liapending"].ToString().Length > 0)
        lia.LiaPending = bool.Parse(r["liapending"].ToString())          // pending flag

Decimal fields WITH guard (all financial amounts):
    if (r["laipaidins"].ToString().Length > 0)
        lia.LaiPaidins = decimal.Parse(r["laipaidins"].ToString())    // insurance paid amount
    if (r["laicontadj"].ToString().Length > 0)
        lia.LaiContAdj = decimal.Parse(r["laicontadj"].ToString())   // contractual adjustment
    if (r["laigenadj"].ToString().Length > 0)
        lia.LaiGenadj = decimal.Parse(r["laigenadj"].ToString())     // general adjustment
    if (r["laicopay"].ToString().Length > 0)
        lia.LaiCopay = decimal.Parse(r["laicopay"].ToString())       // copay amount
    if (r["laideduc"].ToString().Length > 0)
        lia.LaiDeduc = decimal.Parse(r["laideduc"].ToString())       // deductible amount
    if (r["laiclient"].ToString().Length > 0)
        lia.LaiClient = decimal.Parse(r["laiclient"].ToString())     // patient responsibility
    if (r["laicoins"].ToString().Length > 0)
        lia.LaiCoins = decimal.Parse(r["laicoins"].ToString())       // coinsurance amount
    if (r["liaamt"].ToString().Length > 0)
        lia.Liaamt = decimal.Parse(r["liaamt"].ToString())           // total activity amount

These are the financial EOB (Explanation of Benefits) posting amounts. All are stored
as SQL decimal in the destination — unlike the claim header F# fields which store dollar
amounts as varchar strings.

Plain string copies:
    lia.LiaStrUser     = r["liastruser"].ToString()
    lia.LiaStrDesc     = r["liastrdesc"].ToString()
    lia.Liastrtext     = r["liastrtext"].ToString()
    lia.LiaAdjreason   = r["liaadjreason"].ToString()
    lia.LiaAction1     = r["liaaction1"].ToString()
    lia.LiaAction2     = r["liaaction2"].ToString()
    lia.LiaAdjcontract = r["liaadjcontract"].ToString()
    lia.LiaAdjgeneral  = r["liaadjgeneral"].ToString()
    lia.LiaAnsi1       = r["liaansi1"].ToString()        // ANSI/CARC reason code 1
    lia.LiaAnsi2       = r["liaansi2"].ToString()        // ANSI/CARC reason code 2
    lia.LiaAnsimod1    = r["liaansimod1"].ToString()
    lia.LiaAnsimod2    = r["liaansimod2"].ToString()
    lia.LiaDbnotes     = r["liadbnotes"].ToString()

COMMIT AND ERROR HANDLING — same pattern as SaveClaims():
    db.SaveChanges();

    catch (Exception e)
    {
        res.IsResult = false;
        res.ExceptMsg = e.Message;
        res.ExceptInnerMsg = e.InnerException.Message;
        Console.WriteLine(e.Message);
        Console.WriteLine(e.InnerException.Message);
        // No null guard on InnerException here (same risk as SaveClaims)
    }
________________________________________

METHOD 4 — CleanupDeletedData()
Lines 555–647

SIGNATURE
    public bool CleanupDeletedData(
        DataTable tbl,                  // ALL currently active IDs from SAMMS (full set)
        string sc,                      // SiteCode
        string tblName,                 // "claims", "claimlineitem", or "claimlineitemactivity"
        Models.BHG_DRContext db)        // EF Core context (created if null)

RETURN TYPE: bool (true = success, false = exception caught)

PURPOSE
This method performs full RowState reconciliation — a soft-delete sweep.
Unlike SaveClaims which only sees the incremental subset of recently changed rows,
this method receives a DataTable of ALL currently active IDs from SAMMS (the full
list, not filtered by date) and uses it to determine which Azure rows no longer
exist in the source.

ALGORITHM (same for all three table types):
    1. Load all Azure rows for sc into memory
    2. Set EVERY row's RowState = false ("assume all deleted")
    3. For each source ID in tbl:
           find the matching Azure row and set RowState = true ("this one still exists")
    4. db.SaveChanges() — rows not found in step 3 remain false permanently

This is the definitive "mark deleted" pass. SaveClaims's RowState management only
covers the current-year window; CleanupDeletedData covers the full history.

SWITCH ON tblName (lines 561–636):

--- CASE "claims" (lines 563–584) ---
    List<TblClaims> claims = db.TblClaims.Where(x => x.SiteCode == sc).ToList();
    Console.WriteLine("Claims - " + sc);

    foreach (TblClaims c in claims) { c.RowState = false; }  // Mark all as deleted

    foreach (DataRow r in tbl.Rows)
    {
        int id = int.Parse(r["tpcid"].ToString());
        TblClaims c = claims.Where(x => x.TpcId == id).FirstOrDefault();
        if (c != null) { c.RowState = true; }  // Still in source → keep active
        // c == null → record is in source but not yet in Azure → not touched here
    }

PK used for matching: TpcId (single column). SiteCode is already scoped by the
initial WHERE clause on the load.

--- CASE "claimlineitem" (lines 585–612) ---
    List<TblClaimLineItem> clis = db.TblClaimLineItem.Where(x => x.SiteCode == sc).ToList();
    Console.WriteLine("ClaimLineItems - " + sc);

    foreach (TblClaimLineItem scli in clis) { scli.RowState = false; }

    foreach (DataRow r in tbl.Rows)
    {
        int id  = int.Parse(r["tpcliID"].ToString());
        int sid = int.Parse(r["tpcliDSID"].ToString());   // linked DartsSrv session
        int cid = int.Parse(r["tpcliTPCID"].ToString());  // parent claim ID

        TblClaimLineItem cli = clis.Where(x =>
            x.TpcliId    == id  &&
            x.TpcliDsid  == sid &&
            x.TpcliTpcid == cid).FirstOrDefault();

        if (cli != null) { cli.RowState = true; }
    }

PK used for matching: THREE columns (TpcliId + TpcliDsid + TpcliTpcid).
This is more than just the primary key — it uses a business-key match combining
the line item ID, its linked DartsSrv session, and its parent claim. This ensures
a row is only marked active when all three relationship links match exactly.

--- CASE "claimlineitemactivity" (lines 613–635) ---
    List<TblClaimLineItemActivity> clias = db.TblClaimLineItemActivity
        .Where(x => x.SiteCode == sc)
        .OrderBy(o => o.LiaId)
        .ToList();
    Console.WriteLine("ClaimLineItemActivity - " + sc);

    foreach (TblClaimLineItemActivity s in clias) { s.RowState = false; }

    foreach (DataRow r in tbl.Rows)
    {
        int id    = int.Parse(r["liaID"].ToString());
        int tpcid = int.Parse(r["liaTPCLIID"].ToString());  // parent line item ID
        string dtm = r["liaDtm"].ToString();                // captured but not used in lookup

        TblClaimLineItemActivity clia = clias
            .Where(x => x.LiaId == id && x.LiaTpcliid == tpcid)
            .FirstOrDefault();

        if (clia != null) { clia.RowState = true; }
    }

PK used for matching: LiaId + LiaTpcliid (two columns).
liaDtm is extracted into the dtm variable but is NOT used in the LINQ lookup —
it may have been included for debugging purposes or future use.
The OrderBy(o => o.LiaId) on the initial load has no functional effect on the
reconciliation but may help with debugging output ordering.

COMMIT (line 637)
    db.SaveChanges();
    // Single commit after all three table cases — commits RowState changes for all rows

ERROR HANDLING (lines 638–646)
    catch (Exception e)
    {
        res = false;
        Console.WriteLine(e.Message);
        if (e.InnerException.Message != null)
            Console.WriteLine(e.InnerException.Message);
    }

Note: e.InnerException.Message != null is checked but e.InnerException itself is not
null-checked first. If InnerException is null, this check throws a NullReferenceException
masking the original exception. This is a bug in the error handler.

RETURN
    return res;   // true = success, false = exception caught
________________________________________

CROSS-METHOD PATTERNS SUMMARY

Pattern                     SaveClaims  SaveClaimLineItem  SaveClaimLineItemActivity
---------                   ----------  -----------------  -------------------------
EF context lazy creation    YES         YES                YES
AllNewRows optimization     YES         YES                YES
RowState=false reset        YES (yearly mode only)         YES (yearly mode only)
RowState=true on no-change  YES         YES                YES
LastModAt=Now on no-change  YES         YES                YES
Single db.SaveChanges()     YES         YES                YES
int.Parse for PK            YES         YES                YES
Length > 7 for DateTime     YES         YES                YES
.Date for date columns      YES (wkstart) YES (voiddt)    NO
Length > 0 for int?         YES         YES                YES
Length > 0 for decimal?     NO          YES                YES
Length > 0 for bool?        NO          YES                YES
InnerException null check   NO          YES                NO
Diagnostic row print        NO          YES (SiteCode+ID)  NO

________________________________________

TRANSACTION BEHAVIOR

None of these methods explicitly open a transaction. EF Core's db.SaveChanges()
wraps all pending changes in an implicit transaction internally. This means:

- All INSERTs and UPDATEs within one SaveClaims() call either all commit or all roll back
- There is no partial write for a single clinic's run
- Across multiple clinic runs (different calls to SaveClaims), each call is its own transaction

If SaveChanges() throws (e.g. a unique constraint violation, timeout, or validation error),
none of the changes from that call are persisted, and the catch block captures the error.
________________________________________

MEMORY USAGE NOTES

All three Save methods load the entire site's Azure dataset into memory before processing:
- For SaveClaims: all claims for SiteCode sc (potentially thousands of rows)
- For SaveClaimLineItem: all line items for sc in the year, or on a date
- For SaveClaimLineItemActivity: all activity for sc in the year, or on a date

This in-memory list approach enables the fast AllNewRows shortcut and O(1) per-row
lookup (LINQ FirstOrDefault on an already-loaded List<T>). However it means memory
consumption scales with the size of the site's claim history.

For large sites with many years of claims, the yearly SaveClaims load (all claims
for the site, no year filter) can pull tens of thousands of rows into memory.
________________________________________

COLUMN CASE SENSITIVITY IN DataRow LOOKUP

All DataRow column lookups use r["columnname"].ToString(). The column names used
are in lowercase in most cases (e.g. r["tpcid"], r["rowchksum"]) matching the
convention from the source SELECT in SelectConstructor.

Some columns use mixed case as they appear in SAMMS:
    r["tpcDtmAdded"]       — mixed case D
    r["tpcCreatedDate"]    — mixed case C and D
    r["tpcEncounter"]      — mixed case E
    r["tpcliDtmService"]   — mixed case D and S
    r["tpcliDtmAdded"]     — mixed case D and A
    r["tpcliDtmServiceTo"] — mixed case D, S, T
    r["liaTPCLIID"]        — uppercase (used in CleanupDeletedData source query)

DataTable column lookup is case-insensitive by default in .NET, so mixed case
does not cause runtime errors. The inconsistency is a source code style artifact.
________________________________________

QUICK REFERENCE

                        SaveClaims      SaveClaimLineItem   SaveClaimLineItemActivity   CleanupDeletedData
                        ----------      -----------------   -------------------------   ------------------
Source Table            tbl3pClaim      tbl3pClaimLineItem  tbl3pClaimLineItemActivity  (all three)
Destination Table       pats.tbl_Claims pats.tbl_ClaimLI    pats.tbl_ClaimLIA           (all three)
EF Model Class          TblClaims       TblClaimLineItem     TblClaimLineItemActivity    (all three)
PK Columns              SiteCode+tpcID  SiteCode+tpcliID     SiteCode+liaID             (varies)
Return Type             RCodes          RCodes               RCodes                     bool
yearly parameter        YES             NO (auto-detected)   NO (auto-detected)         N/A
Yearly scope field      TpcCreatedDate  TpcliDtmAdded        LiaDtm                     N/A
RowState managed        YES             YES                  YES                        YES (full sweep)
Decimal fields          NO              YES (charge, fee)    YES (all EOB amounts)       NO
Financial amounts type  varchar (F#)    decimal              decimal                     N/A
