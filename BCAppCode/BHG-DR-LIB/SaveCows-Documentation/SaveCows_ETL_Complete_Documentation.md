
COWS ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 6 — Samms-Forms
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract Clinical Opiate
Withdrawal Scale (COWS) assessment records from local SAMMS SQL Server databases at each
clinic and load them into the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What COWS assessments are and why they exist in the warehouse
- What systems and files are involved from start to finish
- How BHGTaskRunner dispatches and pre-validates this task — two-layer pre-check and
  a hardcoded schema-adaptive SELECT (not metadata-driven)
- How the SELECT adapts to old and new SAMMS column names at runtime
- How BHGTaskRunner resolves integer scores to descriptive text via DroDownListItems joins
- How SaveCows_v6 uses TblCowxref as a secondary fallback for description resolution
- The lookup key, explicit field-copy update pattern, and two-phase commit
- How RowChkSum is stored but never used as a change detection guard
- All known anomalies, bugs, and design notes

There is one method in SaveCows.cs:

pats.tbl_CowsV6:   SaveCows_v6   (COWS withdrawal assessments)
________________________________________

2. High-Level Business Summary

What is a COWS assessment?

The Clinical Opiate Withdrawal Scale (COWS) is a standardised clinical tool used in opioid
treatment programs to objectively measure the severity of opiate withdrawal symptoms in a
patient at a specific point in time. Each COWS assessment captures eleven withdrawal
indicators — resting pulse rate, GI upset, sweating, tremor, restlessness, yawning, pupil
size, anxiety/irritability, bone or joint aches, gooseflesh skin, and runny nose/tearing.
Each indicator is scored on a numeric scale, and the SAMMS `SF_COWS` table stores both the
raw integer score and a description from a dropdown lookup. The COWS is linked to the
patient via a PreAdmission record (`preadmissionid + PatientID`), with an assessment date
(`Dttime`) and optional signature fields.

The warehouse holds COWS data in `pats.tbl_CowsV6` to support clinical reporting, treatment
outcome analysis, withdrawal severity trending, and program quality reviews.

Why two column name sets?

Older SAMMS installations used different column names for some withdrawal indicators
(e.g., `PulseRate` instead of `RestingPulseRate`, `UpsetGI` instead of `GIUpset`, `Sweat`
instead of `Sweating`, etc.). BHGTaskRunner detects which column names exist via
`sys.all_columns` at runtime and uses `ISNULL(new_column, old_column)` to handle both
schema versions in a single SELECT — making this task backward-compatible across all clinic
SAMMS versions.

Load type
SaveCows_v6 uses an EF Core two-phase upsert with a full site slice pre-load. Lookup is by
`Cowid + Preadmissionid`. No RowChkSum guard — all fields of found records are always
overwritten. New records are batched into `ncows` and inserted with `AddRange`.
________________________________________

3. Systems Involved

System / File                              Role
-----------                                ----
tsk.tbl_Schedule (Azure DB)               Defines schedules and run times
Scheduler.exe                             Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=6                   ETL orchestrator — Samms-Forms
ctrl.tbl_LocationCons (Azure DB)          Connection strings per clinic SAMMS SQL Server
SQLSvrManager.cs                          Fires system probes and data SELECT against SAMMS
SaveCows.cs (BHG-DR-LIB)                 1 method — SaveCows_v6
Models/TblCowsV6.cs                       EF entity → pats.tbl_CowsV6
Models/TblCowxref.cs                      EF entity → pats.tbl_Cowxref (xref lookup)
pats.tbl_CowsV6 (Azure BHG_DR)          Final destination for COWS assessments
pats.tbl_Cowxref (Azure BHG_DR)          Reference lookup — integer code → descriptive text
tsk.tbl_RowTrax (Azure DB)               Audit log — present but empty for this task
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs — ~line 738

CASE: pats.tbl_cows_v6

Unlike most other cases, this task does NOT use the standard metadata-driven SELECT from
`dms.tbl_MapSrc2Dsn` via SelectConstructor. Instead, BHGTaskRunner performs a two-layer
pre-check, then builds a fully hardcoded schema-adaptive SELECT.

LAYER 1 — Table existence check:
    SrcDt = sm.GetTableData(st.FromTblVw,
        "select name from sys.tables t where upper(name) = 'SF_COWS'",
        st.ConStr)
    if (SrcDt.Rows.Count != 1) → skip: rCodes.IsResult = true; rCodes.RowsProcessed = 0
Only clinics with an SF_COWS table in their SAMMS database proceed.

LAYER 2 — Column introspection:
    tblCols = sm.GetTableData("Cols",
        "select name, column_id from sys.all_columns c where c.object_id = (select object_id
        from sys.tables where upper(name) = 'SF_COWS')",
        st.ConStr)
    → builds lstCols: List<TblCols> (ColName, ColID)
The column list is used to conditionally add fields to the SELECT.

Hardcoded schema-adaptive SELECT:

The core SELECT is built as a string, with each section conditionally included:

Core fields (always present):
    SiteCode = '{SiteCode}',
    COWID = c.id,
    CltID = p.PatientID,
    c.preadmissionid,
    dttime

Dual-name score fields (ISNULL handles old column name where it exists):
    RestingPulseRate:  isnull(c.RestingPulseRate, c.PulseRate) if "pulserate" col exists, else c.RestingPulseRate
    GIUpset:           isnull(c.giupset, c.upsetGI) if "upsetgi" col exists, else c.giupset
    Sweating:          isnull(c.sweating, c.Sweat) if "sweat" col exists, else c.sweating
    Tremor:            isnull(c.tremor, c.TremorHand) if "tremorhand" col exists, else c.tremor
    Restlessness:      isnull(c.Restlessness, c.Restless) if "restless" col exists, else c.Restlessness
    Yawning:           isnull(c.Yawning, c.Yawn) if "yawn" col exists, else c.Yawning
    PupilSize:         isnull(c.PupilSize, c.Pupil) if "pupil" col exists, else c.PupilSize
    AnxietyOrIrritability:  isnull(c.AnxietyOrIrritability, c.Anxiety) if "anxiety" col exists, else c.AnxietyOrIrritability
    BoneOrJointAches:  isnull(c.BoneOrJointAches, c.BoneJointAche) if "bonejointache" col exists, else c.BoneOrJointAches
    GoosefleshSkin:    isnull(c.GoosefleshSkin, c.GooseFlesh) if "gooseflesh" col exists, else c.GoosefleshSkin
    RunnyNoseOrTearing: isnull(c.RunnyNoseOrTearing, c.RunnyNose) if "runnynose" col exists, else c.RunnyNoseOrTearing

DroDownListItems description joins (always present for each score):
    drp.dropdownlistitem as RestingPulseRatedesc
    dgi.dropdownlistitem as GIUpsetdesc
    dsw.dropdownlistitem as Sweatingdesc
    dt.dropdownlistitem as Tremordesc
    dr.dropdownlistitem as Restlessnessdesc
    dy.dropdownlistitem as Yawningdec
    dps.dropdownlistitem as PupilSizedesc
    doi.dropdownlistitem as AnxietyOrIrritabilitydesc
    dbj.dropdownlistitem as BoneOrJointAchesdesc
    dgf.dropdownlistitem as GoosefleshSkindesc
    drnt.dropdownlistitem as RunnyNoseOrTearingdesc

Optional columns (added only if col exists in SF_COWS):
    c.CompletedBy         (fetched but NEVER mapped — see Anomaly 3)
    c.CreatedOn, c.CreatedBy, c.UpdatedBy, c.UpdatedOn   (always appended with drnt join)
    c.IsActive            (if "isactive" col exists)
    c.PatientSignature    (if "patientsignature" col exists)
    c.IsDeleted           (if "isdeleted" col exists)
    c.ClientSignatureDate (if "clientsignaturedate" col exists)
    c.Version             (if "version" col exists)
    c.staffNameSignature  (if "staffnamesignature" col exists)
    c.staffsignatureby    (if "staffsignatureby" col exists)
    c.staffsignaturedate  (if "staffsignaturedate" col exists)

RowChkSum expression (always included):
    CHECKSUM(c.id, p.PatientID, c.preadmissionid, dttime, c.RestingPulseRate, c.giupset,
    c.sweating, c.tremor, c.Restlessness, c.Yawning, c.PupilSize, c.AnxietyOrIrritability,
    c.BoneOrJointAches, c.GoosefleshSkin, c.RunnyNoseOrTearing, c.CreatedOn, c.UpdatedOn)

JOINs:
    SF_Cows c
    left join DroDownListItems drp on c.RestingPulseRate = drp.Id
    left join DroDownListItems dgi on c.GIUpset = dgi.Id
    left join DroDownListItems dsw on c.Sweating = dsw.Id
    left join DroDownListItems dt on c.Tremor = dt.Id
    left join DroDownListItems dr on c.restlessness = dr.Id
    left join DroDownListItems dy on c.Yawning = dy.Id
    left join DroDownListItems dps on c.PupilSize = dps.Id
    left join DroDownListItems doi on c.AnxietyOrIrritability = doi.Id
    left join DroDownListItems dbj on c.BoneOrJointAches = dbj.Id
    left join DroDownListItems dgf on c.GoosefleshSkin = dgf.Id
    left join DroDownListItems drnt on c.RunnyNoseOrTearing = drnt.Id
    left join SF_PatientPreAdmission p on c.PreAdmissionId = p.ID
    -- left join dbo.tblClient clt on (p.PatientID = clt.cltID)    ← commented out

ORDER BY: order by 3, 2    (CltID, COWID)

IMPORTANT — No WHERE clause:
Unlike all other standard BHGTaskRunner cases, this hardcoded SELECT has NO date-based
WHERE clause. The full SF_COWS table is fetched on every ETL run. For clinics with a large
historical COWS dataset this can be a significant volume load.

Call:
    rCodes = sd.SaveCows_v6(SrcDt, st.SiteCode, null)

RowTrax block: EMPTY — no audit logged.
________________________________________

5. Source Table — SAMMS SQL Server (dbo)

dbo.SF_COWS — COWS Withdrawal Assessment Records

Field                    Type      Notes
-----                    ----      -----
id                       int       COWS record ID (mapped as COWID)
PatientID                int       Patient ID — from joined SF_PatientPreAdmission
preadmissionid           int       Linked pre-admission ID
dttime                   datetime  Assessment date/time
reasonforthisassessment  varchar   Free text or resolved DDL reason (optional col)
ddlreasonforthisassessment int     Dropdown integer code for reason (optional col)
RestingPulseRate/PulseRate   int   Raw pulse rate score (dual-name handling)
GIUpset/upsetGI          int       GI upset score
Sweating/Sweat           int       Sweating score
Tremor/TremorHand        int       Hand tremor score
Restlessness/Restless    int       Restlessness score
Yawning/Yawn             int       Yawning score
PupilSize/Pupil          int       Pupil size score
AnxietyOrIrritability/Anxiety int  Anxiety score
BoneOrJointAches/BoneJointAche int Bone/joint ache score
GoosefleshSkin/GooseFlesh  int     Gooseflesh skin score
RunnyNoseOrTearing/RunnyNose int   Runny nose/tearing score
CompletedBy              varchar   Completed by user (fetched but unmapped)
CreatedOn                datetime  Record creation date
CreatedBy                varchar   Created by user
UpdatedBy                varchar   Last updated by user
UpdatedOn                datetime  Last update timestamp
IsActive                 bit       Active flag (optional col — see Anomaly 4)
PatientSignature         varchar   Patient signature (optional col)
IsDeleted                bit       Soft-delete flag (optional col)
ClientSignatureDate      datetime  Client signature date (optional col)
Version                  varchar   Form version (optional col)
staffNameSignature       varchar   Staff name or signature (optional col, dual-name)
staffsignatureby         varchar   Staff signed-by name (optional col, dual-name)
staffsignaturedate       datetime  Staff signature date (optional col)

Reference table: dbo.DroDownListItems
    Used in JOIN to resolve integer scores to descriptive text for all 11 indicators.

Reference table: pats.tbl_Cowxref (Azure — pre-loaded globally)
    Used as secondary fallback inside SaveCows_v6 if description fields arrive empty.
________________________________________

6. SaveCows_v6 — COWS Assessment Load (pats.tbl_CowsV6)

Source: dbo.SF_COWS (via hardcoded BHGTaskRunner SELECT with DroDownListItems joins)
Destination: pats.tbl_CowsV6
Composite key: Cowid + Preadmissionid (CltId commented out of lookup)
Parameters: tbl, sc, db
No wrkdt parameter — no date filter applied; full table fetched.

Azure pre-load:
    cowxrefs = db.TblCowxref.ToList()    ← ALL xref records — no SiteCode filter
    cows = db.TblCowsV6.Where(x => x.SiteCode == sc).ToList()
`TblCowxref` is loaded globally (not per site). `TblCowsV6` is full site slice.

Per-row fresh object construction via 35-field column switch:

cowid / id (dual labels):
    cow.Cowid = int.Parse(...)
    cow.RowState = true
    cow.LastModAt = DateTime.Now
    Note: RowState is activated here; it may be overridden by the isdeleted case.

cltid / patientid (dual labels):
    cow.CltId = int.Parse(...)

preadmissionid:
    cow.Preadmissionid = int.Parse(...)

dttime:
    DateTime.Parse guarded by length > 6

reasonforthisassessment:
    Complex two-path resolution:
    Path A (source value empty): if column "ddlreasonforthisassessment" exists in tbl,
    parse the DDL integer and look up in cowxrefs (ColumnName="AssessmentReason").
    If xref resolves to empty string, fall back to raw integer as string.
    Path B (source value non-empty): use source text directly.
    Note: This case only handles the fallback. The "ddlreasonforthisassessment" case fires
    independently with its own xref lookup (see below).

ddlreasonforthisassessment:
    Independent xref lookup: parse integer, find in cowxrefs. If no match, use raw string.
    This means when BOTH columns exist, both cases fire and the last one to execute
    (column order dependent) writes the final value.

Score fields (11 indicators — all follow same pattern):
    Integer value: guarded by length > 0, then int.Parse
    Description field: assigned directly from source. If empty AND integer value is set,
    fallback lookup in cowxrefs by (ColumnName, PermissibleValue).

    Field → CowxrefColumnName
    RestingPulseRatedesc → "PulseRate"
    GIUpsetdesc → "UpsetGI"
    Sweatingdesc → "Sweat"
    Tremordesc → "Tremorhand"
    Restlessnessdesc → "Restless"
    Yawningdec → "Yawn"          ← note: property is Yawningdec (not Yawningdesc)
    PupilSizedesc → "Pupil"
    AnxietyOrIrritabilitydesc → "Anxiety"
    BoneOrJointAchesdesc → "BoneJointAche"
    GoosefleshSkindesc → "Gooseflesh"
    RunnyNoseOrTearingdesc → "RunnyNose"

    Since BHGTaskRunner already resolves all descriptions via DroDownListItems JOINs in the
    SELECT, the xref fallback inside SaveCows_v6 is a safety net for cases where the JOIN
    returns NULL (no matching DroDownListItems row).

completedby:
    COMMENTED OUT — `//cow.CompletedBy = r[c.ColumnName].ToString();`
    Column is fetched in the SELECT but mapping is disabled (see Anomaly 3).

createdon / updatedon:
    DateTime.Parse guarded by length > 6.

createdby / updatedby:
    Direct string assignment.

isactive — DEAD CODE BRANCH (see Anomaly 4):
    if (length > 0) { if (value == "") { cow.IsActive = true; } }
    The outer guard ensures length > 0; the inner check for == "" can never be true.
    IsActive is never set from this case.

isdeleted:
    if (value == "1") { cow.IsDeleted = true; cow.RowState = false; }
    Column-order dependency: if "isdeleted" processes before "cowid/id", RowState will be
    overridden back to true by the cowid case (see Anomaly 5).

staffnamesignature / staffsignatureby (dual labels):
    Both map to cow.StaffNameSignature. Logic: try r["staffnamesignature"] first; if empty,
    use the current column's value. This gracefully handles clinics providing either column.

staffsignaturedate:
    DateTime.Parse guarded by Trim().Length > 6.

rowchksum:
    cow.RowChkSum = int.Parse(...) — stored but NOT used as a guard (see Anomaly 2).

Lookup:
    dbc = cows.Where(x => x.Cowid == cow.Cowid && x.Preadmissionid == cow.Preadmissionid).FirstOrDefault()
    Note: CltId is commented out of the lookup: //&& x.CltId == cow.CltId

Explicit field-copy update (found path):
Unlike some other Save methods that rely on EF change tracking, this method explicitly
copies each field from the source `cow` to the existing `dbc`. Fields updated:
    CltId, all 11 score integers, all 11 score descriptions, ClientSignatureDate,
    PatientSignature, StaffNameSignature, StaffSignatureDate, CreatedBy, CreatedOn,
    Dttime, IsActive, IsDeleted, LastModAt (TWICE — see Anomaly 6), Preadmissionid,
    UpdatedBy, UpdatedOn, Version, RowChkSum, RowState.

No RowChkSum guard — all fields are overwritten unconditionally.

Two-phase commit:
    db.SaveChanges()                      ← commit all updates
    if (ncows.Count > 0)
    {
        db.TblCowsV6.AddRange(ncows)      ← insert new records
        db.SaveChanges()
    }

Error handling:
    Correct pattern: `if (e.InnerException != null)` — null check on InnerException before
    accessing .Message.
________________________________________

7. Change Detection — RowChkSum Behaviour

RowChkSum is calculated in the BHGTaskRunner SELECT across 16 source fields (c.id, PatientID,
preadmissionid, dttime, all 11 score integers, CreatedOn, UpdatedOn) and is stored in
`cow.RowChkSum`. It is mapped in the switch (`cow.RowChkSum = int.Parse(...)`) and copied to
`dbc.RowChkSum` in the update block.

However, there is no conditional guard:
    // if (dbc.RowChkSum != cow.RowChkSum) — MISSING
    {
        // all field copy code — runs unconditionally
    }

Result: every COWS record that exists in both source and Azure is fully overwritten on every
ETL run, even if nothing changed. The RowChkSum is stored correctly but is never read to
suppress unnecessary updates.
________________________________________

8. TblCowxref — Secondary Description Resolution

`pats.tbl_Cowxref` is a cross-reference lookup table pre-loaded globally at the start of
SaveCows_v6 (no SiteCode filter). It maps (ColumnName, PermissibleValue) → DescripiveText
(note: typo preserved in the property name).

It is used in two contexts:

A. Score description fallback: When a description field arrives empty from the SELECT
   (DroDownListItems JOIN returned NULL), the method falls back to cowxrefs.
   Example: `cowxrefs.Where(x => x.ColumnName == "PulseRate" && x.PermissibleValue == cow.RestingPulseRate).FirstOrDefault().DescripiveText`

B. Reason resolution: For `reasonforthisassessment` / `ddlreasonforthisassessment`,
   cowxrefs.FirstOrDefault(...) is used as the primary lookup for DDL integer codes.

RISK: The cowxref `.FirstOrDefault().DescripiveText` calls do NOT null-check the
`FirstOrDefault()` result before accessing `.DescripiveText`. If no matching xref row
exists, this will throw a `NullReferenceException`, aborting the full site load (see Anomaly 7).
________________________________________

9. Anomalies, Bugs, and Known Defects

ANOMALY 1 — No WHERE clause — full table fetch on every run.

File: BHGTaskRunner/Program.cs, ~line 803
The hardcoded SELECT does not append `strWhere` (the standard rolling date filter). The entire
SF_COWS table is fetched every run. For high-volume clinics, this is a significant and
unnecessary data transfer. All other comparable form-data tasks use a rolling lookback window.

ANOMALY 2 — RowChkSum stored but never used as a guard.

File: SaveCows.cs, lines 317–365
RowChkSum is populated in `cow.RowChkSum` and copied to `dbc.RowChkSum` in the update block,
but there is no comparison gate around the update code. All 30+ fields are unconditionally
overwritten on every row for every found record. The RowChkSum is effectively decorative.

ANOMALY 3 — CompletedBy permanently commented out.

File: SaveCows.cs, lines 239–240 and 350
    //cow.CompletedBy = r[c.ColumnName].ToString();   ← mapping disabled
    //dbc.CompletedBy = cow.CompletedBy;              ← update copy disabled
The column is fetched in the BHGTaskRunner SELECT but the mapping in the switch and the copy
in the update block are both commented out. CompletedBy is never populated in Azure.

ANOMALY 4 — IsActive case is dead code.

File: SaveCows.cs, lines 259–267
    if (r[c.ColumnName].ToString().Length > 0)    ← ensures value is non-empty
    {
        if (r[c.ColumnName].ToString() == "")     ← can never be true given the outer guard
        {
            cow.IsActive = true;
        }
    }
The inner condition `== ""` is mutually exclusive with the outer guard `Length > 0`. The
`IsActive` property is never assigned from this case. Any record with an IsActive column in
the source will always have `cow.IsActive` remain as null/default.

ANOMALY 5 — RowState column-order dependency (cowid vs isdeleted).

File: SaveCows.cs, lines 35–40 and 277–285
RowState is set to `true` in the `cowid`/`id` case and may be overridden to `false` by the
`isdeleted` case when the value is "1". If the DataTable column order places "isdeleted"
BEFORE "cowid" in the iteration, the `cowid` case fires last and always resets RowState=true,
making the isdeleted-based deactivation ineffective. In practice the SELECT orders cowid
first (order by 3, 2 on CltID, COWID), but column order in the DataTable is determined by
the SELECT list and is fragile.

ANOMALY 6 — LastModAt assigned twice in the update block.

File: SaveCows.cs, lines 356 and 361
    dbc.LastModAt = cow.LastModAt;    ← line 356
    ...
    dbc.LastModAt = cow.LastModAt;    ← line 361 (duplicate)
The same assignment appears twice — the second is redundant. No functional impact.

ANOMALY 7 — NullReferenceException risk on cowxref lookups.

File: SaveCows.cs, lines 63–66, 87–92, 103–106, 116–119, 130–132, 143–145, etc.
Pattern: `cowxrefs.FirstOrDefault(x => ...).DescripiveText`
If no matching row exists in `TblCowxref`, `FirstOrDefault()` returns null. Accessing
`.DescripiveText` on null throws a `NullReferenceException`, aborting the entire site's load.
The safe pattern would be:
    var dtref = cowxrefs.FirstOrDefault(x => ...)
    if (dtref != null) { cow.Field = dtref.DescripiveText; }

ANOMALY 8 — CltId commented out of lookup key.

File: SaveCows.cs, line 310
    cows.Where(x => x.Cowid == cow.Cowid
        //&& x.CltId == cow.CltId        ← COMMENTED OUT
        && x.Preadmissionid == cow.Preadmissionid)
CltId is excluded from the composite lookup. If two different patients (different CltId) had
COWS records with the same Cowid and Preadmissionid (unlikely but theoretically possible),
they would match the same Azure record and one would incorrectly overwrite the other.

ANOMALY 9 — RowTrax block present but empty.

File: BHGTaskRunner/Program.cs, lines 824–829
No source-vs-destination row count audit is ever logged for this task.

ANOMALY 10 — DescripiveText typo frozen in EF model.

File: Models/TblCowxref.cs (referenced throughout SaveCows.cs)
The EF model property is `DescripiveText` (missing 'r' in "Descriptive"). This typo is frozen
in both the database schema and the EF model. All references in SaveCows.cs use the typo
name correctly and consistently.
________________________________________

10. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe
        |
        |-- insert parent task (Samms-Forms)
        |-- insert child tasks per clinic:
        |       pats.tbl_cows_v6    SiteCode='B01A'
        |       ...
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        V
BHGTaskRunner.exe 6 (Samms-Forms)
        |
        |  LAYER 1: probe sys.tables for 'SF_COWS'
        |      Not found → skip (rCodes.IsResult=true, RowsProcessed=0)
        |
        |  LAYER 2: probe sys.all_columns for SF_COWS → build lstCols
        |
        |  Build hardcoded schema-adaptive SELECT:
        |      Core: SiteCode, COWID, CltID, preadmissionid, dttime
        |      11 score columns (ISNULL old/new name where applicable)
        |      11 DroDownListItems desc joins
        |      Optional columns (IsActive, PatientSignature, IsDeleted, etc.)
        |      RowChkSum = CHECKSUM(16 fields)
        |      FROM SF_Cows c LEFT JOIN DroDownListItems x11 LEFT JOIN SF_PatientPreAdmission
        |      ORDER BY CltID, COWID
        |      NOTE: No WHERE clause — full table fetch
        |
        |  SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
        |
        V
sd.SaveCows_v6(SrcDt, SiteCode, null)
        |
        |  Pre-load:
        |      cowxrefs = db.TblCowxref.ToList()           (global, no SiteCode filter)
        |      cows = db.TblCowsV6.Where(SiteCode==sc).ToList()   (full site slice)
        |
        |  For each source row:
        |      Build fresh TblCowsV6 via 35-field column switch
        |      Resolve descriptions via cowxrefs fallback if DroDownListItems returned NULL
        |      Lookup: Cowid + Preadmissionid
        |      Found:
        |          Explicit field copy of all 30+ fields → dbc (no RowChkSum guard)
        |          rCodes.RowsUpd++
        |      Not found:
        |          ncows.Add(cow)
        |          rCodes.RowsIns++
        |
        |  db.SaveChanges()
        |  if (ncows.Count > 0) → db.TblCowsV6.AddRange(ncows) → db.SaveChanges()
        |
        V
pats.tbl_CowsV6 (Azure BHG_DR)
        |
        V
RowTrax EMPTY — no audit logged
BHGTaskRunner marks task Status=20 (complete)
________________________________________

11. File Reference Map

File Path                                              Purpose
---------                                              -------
BCAppCode/BHG-DR-LIB/SaveCows.cs                      Single method SaveCows_v6 (388 lines)
BCAppCode/BHGTaskRunner/Program.cs                     Dispatch — case "pats.tbl_cows_v6" ~line 738
                                                       2-layer pre-check + hardcoded SELECT
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                 Executes sys.tables probe, column probe, data SELECT
BCAppCode/BHG-DR-LIB/Models/TblCowsV6.cs              EF Model → pats.tbl_CowsV6
BCAppCode/BHG-DR-LIB/Models/TblCowxref.cs             EF Model → pats.tbl_Cowxref (xref lookup)
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs          EF DbContext — DbSet registrations
________________________________________

12. Quick Reference Summary

Method          Load path    Key                          RowChkSum guard    RowState    Schedule
------          ---------    ---                          ---------------    --------    --------
SaveCows_v6     EF Core      Cowid + Preadmissionid       Stored, not used   bool        6 (Samms-Forms)

Source: dbo.SF_COWS (hardcoded JOIN SELECT — not metadata-driven)
Score descriptions: resolved by DroDownListItems JOINs in SELECT; fallback via pats.tbl_Cowxref
Update path: explicit field-by-field copy (not EF change tracking)
No WHERE clause — full SF_COWS table fetched on every run

Critical bugs / anomalies:
1. No WHERE clause — entire SF_COWS table loaded every run (performance risk)
2. RowChkSum not used as guard — all fields unconditionally overwritten
3. CompletedBy mapping and update copy permanently commented out
4. IsActive case is dead code — can never assign true
5. cowxref .FirstOrDefault().DescripiveText without null check — NullReferenceException risk
6. RowState column-order dependency (isdeleted vs cowid/id case sequence)
7. LastModAt assigned twice redundantly in update block
8. CltId removed from composite lookup key



SaveCows.cs
Method: SaveCows_v6
Field	Value
Name	SaveCows_v6
Module	COWS (Clinical Opiate Withdrawal Scale) assessments
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode
Source table	dbo.SF_COWS joined to dbo.DroDownListItems (×11) and dbo.SF_PatientPreAdmission; SELECT is hardcoded in BHGTaskRunner with schema-adaptive ISNULL(new_col, old_col) for 11 score fields — not metadata-driven
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_CowsV6
Load type	EF Core upsert — full site slice pre-load; 2-layer pre-check (SF_COWS table existence + sys.all_columns introspection); composite key lookup (Cowid + Preadmissionid); no RowChkSum guard — explicit field copy overwrites all fields unconditionally; description resolution via DroDownListItems JOINs in SELECT with pats.tbl_Cowxref fallback inside method; two-phase commit
Load type column	RowChkSum stored but not used as guard; RowState (bool) set in cowid case, may be overridden by isdeleted case; LastModAt stamped in cowid case; no WHERE clause — full SF_COWS table fetched every run
Frequency	Daily
Schedule	Schedule 6 — BHGTaskRunner.exe 6 (Samms-Forms)
Parent	Samms-Forms
Downstream	pats.tbl_CowsV6 → withdrawal severity reporting; treatment outcome analysis
Connection / method	Source: hardcoded schema-adaptive JOIN SELECT in BHGTaskRunner. Target: sd.SaveCows_v6(SrcDt, st.SiteCode, null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveCows.cs
Status	Active — gated by 2-layer pre-check
Folder	BHG-DR-LIB\SaveCows.cs; detail in SaveCows-Documentation\SaveCows_ETL_Complete_Documentation.md