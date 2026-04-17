
Custom QA ETL — Complete Integration Documentation
BHG-DR-LIB | BHGTaskRunner | Scheduler
Schedule 6 — Samms-Forms (primary) / Schedule 3 — Catch-all
________________________________________

1. Document Purpose

This document explains the complete end-to-end process used to extract custom question and
custom answer records from local SAMMS SQL Server databases at each clinic and load them into
the central Azure SQL data warehouse (BHG_DR).

The goal of this document is to explain:
- What custom question and answer data are and why they exist
- What systems and files are involved from start to finish
- How BHGTaskRunner dispatches these tasks
- How both methods in SaveCustomQA.cs work
- The partial pre-pass RowSate reset used by both methods
- How RowChkSum (named RowCheckSum in this file) guards field updates
- The RowSate typo preserved throughout both methods and the EF model
- All known anomalies, bugs, and defects

There are two methods in SaveCustomQA.cs spanning two Azure destination tables:

pats.tbl_CustomQuestions:  SaveCustomQuestions  (custom form questions)
pats.tbl_CustomAnswers:    SaveCustomAnswers     (patient answers to custom questions)
________________________________________

2. High-Level Business Summary

What is custom QA data?

SAMMS allows clinics to define custom questions that are added to patient forms — beyond
the standard clinical fields. These are clinic-specific questions relevant to their
programs or regulatory requirements. `pats.tbl_CustomQuestions` stores the question
definitions (CId, question text), while `pats.tbl_CustomAnswers` stores each patient's
response to each question (CaId, linked question CaQid, linked patient CaCltid, answer text).

RowSate (note: missing 't' — typo preserved from the EF model) tracks whether a question
or answer is currently active. A negative CId or CaCltid marks the record as inactive.
The partial pre-pass reset marks all currently-active Azure records as inactive before the
loop, then only records returned from the source are re-activated.

Load type
Both methods use an EF Core two-phase upsert with a partial pre-pass RowSate reset and an
effective RowChkSum guard (named RowCheckSum in this file). The checksum property accepts
both "rowchksum" and "rowchecksum" source column names via dual case labels.
________________________________________

3. Systems Involved

System / File                               Role
-----------                                 ----
tsk.tbl_Schedule (Azure DB)                 Defines schedules and run times
Scheduler.exe                               Creates child tasks in tsk.tbl_Tasks daily
BHGTaskRunner.exe arg=6 / arg=3             ETL orchestrator — Samms-Forms / catch-all
ctrl.tbl_LocationCons (Azure DB)            Connection strings per clinic SAMMS SQL Server
dms.tbl_MapSrc2Dsn (Azure DB)              Column list + RowChkSum expression per task
SQLSvrManager.cs                            Fires SELECT against the clinic SAMMS SQL Server
SaveCustomQA.cs (BHG-DR-LIB)               2 methods — SaveCustomQuestions, SaveCustomAnswers
Models/TblCustomQuestions.cs               EF entity → pats.tbl_CustomQuestions
Models/TblCustomAnswers.cs                 EF entity → pats.tbl_CustomAnswers
pats.tbl_CustomQuestions (Azure BHG_DR)   Final destination for custom question definitions
pats.tbl_CustomAnswers (Azure BHG_DR)     Final destination for patient custom answers
________________________________________

4. BHGTaskRunner — Dispatch

File: BCAppCode/BHGTaskRunner/Program.cs

CASE: pats.tbl_customquestions   (~line 843)
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveCustomQuestions(SrcDt, st.SiteCode, null)
    RowTrax block: EMPTY — no audit logged

CASE: pats.tbl_customanswers     (~line 854)
    strCmd += " Where " + strWhere + " " + st.SortOrder
    SrcDt = sm.GetTableData(st.FromTblVw, strCmd, st.ConStr)
    rCodes = sd.SaveCustomAnswers(SrcDt, st.SiteCode, null)
    RowTrax block: EMPTY — no audit logged

Both cases use standard rolling lookback (DaysBack=-15) via strWhere.
Neither method receives a wrkdt parameter — date scoping is source-side only.
________________________________________

5. Source Tables — SAMMS SQL Server (dbo)

5a. dbo.tblCustomQuestions — Custom Question Definitions
    CId           int        Unique question ID (primary key; negative = inactive)
    CQuestion     varchar    Question text
    RowCheckSum   int        CHECKSUM() — note: may also appear as RowChkSum in some schemas

5b. dbo.tblCustomAnswers — Patient Custom Answers
    CaId          int        Unique answer record ID
    CaQid         int        Linked question ID
    CaCltid       int        Patient/client ID (negative = inactive)
    CaAns         varchar    Answer text
    RowCheckSum   int        CHECKSUM()
________________________________________

6. SaveCustomQuestions — Custom Question Definitions Load (pats.tbl_CustomQuestions)

Source: dbo.tblCustomQuestions (or clinic view via st.FromTblVw)
Destination: pats.tbl_CustomQuestions
Key: SiteCode (via pre-load) + CId
Parameters: tbl, sc, db
No wrkdt parameter.

Azure pre-load:
    CQs = db.TblCustomQuestions.Where(x => x.SiteCode == sc).OrderBy(o => o.CId).ToList()
Full site slice ordered by CId.

Pre-pass partial RowSate reset:
    foreach (TblCustomQuestions c in CQs)
    {
        if (c.RowSate == 1) { c.RowSate = 0; }
    }
Only records where RowSate is currently 1 (active) are reset to 0. Records already at 0
are left unchanged. No SaveChanges called here — reset committed at end.

Per-row fresh object construction via 4-field switch:
    sitecode:              cq.SiteCode = ...; cq.RowSate = 1; cq.LastModAt = DateTime.Now
    rowchksum/rowchecksum: cq.RowCheckSum = int.Parse(...)  ← dual case labels
    cid:                   cq.CId = int.Parse(...); if (CId < 0) { cq.RowSate = 0; }
    cquestion:             cq.CQuestion = ...

Note: RowSate (int, not bool) is set to 1 in the sitecode case, then may be overridden to 0
by the cid case if CId is negative. Column order matters — if "cid" processes before
"sitecode", RowSate=1 from sitecode will override the negative-CId RowSate=0. In practice
"sitecode" typically appears first in the SELECT.

Lookup: CId only
    dcq = CQs.Where(x => x.CId == cq.CId).FirstOrDefault()

RowChkSum guard:
    if (dcq.RowCheckSum == cq.RowCheckSum)
    {
        dcq.RowSate = cq.RowSate       ← re-activates (or keeps inactive if CId<0)
        dcq.LastModAt = DateTime.Now
    }
    else
    {
        dcq.CQuestion = cq.CQuestion
        dcq.LastModAt = DateTime.Now
        dcq.RowCheckSum = cq.RowCheckSum
        dcq.RowSate = cq.RowSate
    }
On checksum match: only RowSate and LastModAt refreshed. On mismatch: CQuestion also updated.

Commit: db.SaveChanges() → AddRange(NewCQs) → db.SaveChanges()
No RowsIns or RowsUpd counters incremented.
________________________________________

7. SaveCustomAnswers — Patient Custom Answers Load (pats.tbl_CustomAnswers)

Source: dbo.tblCustomAnswers (or clinic view via st.FromTblVw)
Destination: pats.tbl_CustomAnswers
Composite key: SiteCode (via pre-load) + CaId + CaQid + CaCltid
Parameters: tbl, sc, db
No wrkdt parameter.

Azure pre-load:
    CAs = db.TblCustomAnswers.Where(x => x.SiteCode == sc).OrderBy(o => o.CaId).ThenBy(p => p.CaCltid).ToList()
Full site slice ordered by CaId then CaCltid.

Pre-pass partial RowSate reset: Same pattern as SaveCustomQuestions.

Per-row fresh object construction via 6-field switch:
    sitecode:              ca.SiteCode = ...; ca.RowSate = 1; ca.LastModAt = DateTime.Now
    rowchksum/rowchecksum: ca.RowCheckSum = int.Parse(...)
    caid:                  ca.CaId = int.Parse(...)
    caqid:                 ca.CaQid = int.Parse(...)
    cacltid:               ca.CaCltid = int.Parse(...); if (CaCltid < 0) { ca.RowSate = 0; } else { ca.RowSate = 1; }
    caans:                 ca.CaAns = ...

Note: CaCltid case explicitly sets RowSate to 1 (active) for non-negative values, overriding
any previous RowSate value. RowSate in SaveCustomAnswers is driven by CaCltid sign, not CaId.

Lookup: CaId + CaQid + CaCltid (triple composite key)
    dca = CAs.Where(x => x.CaId == ca.CaId && x.CaQid == ca.CaQid && x.CaCltid == ca.CaCltid).FirstOrDefault()

RowChkSum guard:
    if (dca.RowCheckSum == ca.RowCheckSum)
    {
        dca.RowSate = ca.RowSate
        dca.LastModAt = DateTime.Now
    }
    else
    {
        dca.LastModAt = DateTime.Now
        dca.RowCheckSum = ca.RowCheckSum
        dca.RowSate = ca.RowSate
        dca.CaAns = ca.CaAns
    }
On match: only RowSate and LastModAt refreshed. On mismatch: CaAns also updated.

Commit: db.SaveChanges() → AddRange(NewCAs) → db.SaveChanges()
No RowsIns or RowsUpd counters incremented.
________________________________________

8. Change Detection — RowChkSum Behaviour

Method                  RowChkSum present    Guard used    Effective behaviour
------                  -----------------    ----------    -------------------
SaveCustomQuestions     Yes (RowCheckSum)     Yes           Effective. Match: RowSate + LastModAt only. Mismatch: CQuestion + LastModAt + RowCheckSum + RowSate.
SaveCustomAnswers        Yes (RowCheckSum)     Yes           Effective. Match: RowSate + LastModAt only. Mismatch: CaAns + LastModAt + RowCheckSum + RowSate.

Note: The checksum property is named RowCheckSum (not RowChkSum as in most other methods).
Both source column names "rowchksum" and "rowchecksum" are handled via dual case labels.
________________________________________

9. Anomalies, Bugs, and Known Defects

ANOMALY 1 — Both methods: NullReferenceException risk in catch block.

File: SaveCustomQA.cs, lines 86–89 and 174–177
    if (e.InnerException.Message != null)
    {
        rcodes.ExceptInnerMsg = e.InnerException.Message;
    }

The check is on e.InnerException.Message (the property), NOT on e.InnerException (the object).
If e.InnerException is null, accessing .Message throws a NullReferenceException inside the
catch block, masking the original exception entirely. The correct pattern used in all other
BHG-DR-LIB methods is: if (e.InnerException != null). This defect is present in BOTH methods.

ANOMALY 2 — Both methods: RowSate typo preserved throughout.

File: SaveCustomQA.cs (all references) / EF model
The property is named RowSate (missing 't') instead of RowState. This typo exists in the
source column, the EF model, and every reference in both methods. It is frozen in the schema
and would require a database migration + model regeneration to correct. Documented as a
known naming inconsistency.

ANOMALY 3 — Both methods: RowTrax blocks present but empty.

File: BHGTaskRunner/Program.cs, lines 847–851 and 858–862
    if ((st.RowTrax.HasValue) && (st.SiteCode != "PHC"))
    {
        if (st.RowTrax.Value) { }    ← EMPTY
    }
No source-vs-destination row count audit is ever logged for these tasks.

ANOMALY 4 — Both methods: RowsIns and RowsUpd never incremented.

File: SaveCustomQA.cs — both methods
rcodes.RowsIns and rcodes.RowsUpd are never set inside either method. Only
rcodes.RowsProcessed is populated (from tbl.Rows.Count on construction). All new and updated
row counts will appear as 0 in the RCodes returned to BHGTaskRunner.

ANOMALY 5 — SaveCustomQuestions: RowSate column-order dependency (cid vs sitecode).

File: SaveCustomQA.cs, lines 34–46
If a source row has a negative CId and the DataTable column order places "sitecode" AFTER
"cid", the sitecode case will fire last and set RowSate=1, overriding the negative-CId
RowSate=0 set by the cid case. In practice sitecode appears first in the SELECT, making this
non-impactful — but the design is fragile.
________________________________________

10. End-to-End Flow Diagram

Windows Task Scheduler
        |
        V
Scheduler.exe
        |
        |-- insert parent task (Samms-Forms or equivalent)
        |-- insert child tasks per clinic:
        |       pats.tbl_customquestions    SiteCode='B01A'
        |       pats.tbl_customanswers      SiteCode='B01A'
        |
        V
tsk.tbl_Tasks (Azure BHG_DR)
        |
        V
BHGTaskRunner.exe 6 (or 3)
        |
        |======================================================
        |  BRANCH A: pats.tbl_customquestions
        |======================================================
        |   strCmd += " Where " + strWhere (DaysBack=-15)
        |   SrcDt = sm.GetTableData(...)
        |   → sd.SaveCustomQuestions(SrcDt, SiteCode, null)
        |       Pre-load full site slice from pats.tbl_CustomQuestions (ordered by CId)
        |       Pre-pass: RowSate=1 → 0 for all active records
        |       Loop rows → build TblCustomQuestions via 4-field switch
        |       Lookup by CId
        |       Found + checksum match: RowSate + LastModAt only
        |       Found + checksum mismatch: CQuestion + LastModAt + RowCheckSum + RowSate
        |       Not found: NewCQs.Add(cq)
        |       db.SaveChanges() → AddRange(NewCQs) → db.SaveChanges()
        |       → pats.tbl_CustomQuestions (Azure BHG_DR)
        |       RowTrax EMPTY — no audit
        |
        |======================================================
        |  BRANCH B: pats.tbl_customanswers
        |======================================================
        |   strCmd += " Where " + strWhere (DaysBack=-15)
        |   SrcDt = sm.GetTableData(...)
        |   → sd.SaveCustomAnswers(SrcDt, SiteCode, null)
        |       Pre-load full site slice (ordered by CaId, CaCltid)
        |       Pre-pass: RowSate=1 → 0 for all active records
        |       Loop rows → build TblCustomAnswers via 6-field switch
        |       Lookup by CaId + CaQid + CaCltid
        |       Found + checksum match: RowSate + LastModAt only
        |       Found + checksum mismatch: CaAns + LastModAt + RowCheckSum + RowSate
        |       Not found: NewCAs.Add(ca)
        |       db.SaveChanges() → AddRange(NewCAs) → db.SaveChanges()
        |       → pats.tbl_CustomAnswers (Azure BHG_DR)
        |       RowTrax EMPTY — no audit
        |
        V
BHGTaskRunner marks task Status=20 (complete)
________________________________________

11. File Reference Map

File Path                                                     Purpose
---------                                                     -------
BCAppCode/BHG-DR-LIB/SaveCustomQA.cs                         Both methods (184 lines)
BCAppCode/BHGTaskRunner/Program.cs                            Dispatch
                                                              pats.tbl_customquestions  ~line 843
                                                              pats.tbl_customanswers    ~line 854
BCAppCode/BHG-DR-LIB/SelectConstructor.cs                    Builds SELECT and RowCheckSum expression
BCAppCode/BHG-DR-LIB/SQLSvrManager.cs                        Executes SELECT against SAMMS
BCAppCode/BHG-DR-LIB/Models/TblCustomQuestions.cs            EF Model → pats.tbl_CustomQuestions
BCAppCode/BHG-DR-LIB/Models/TblCustomAnswers.cs              EF Model → pats.tbl_CustomAnswers
BCAppCode/BHG-DR-LIB/Models/BHG_DRContext.cs                 EF DbContext — DbSet registrations
________________________________________

12. Quick Reference Summary

Method                  Load path    Key                            RowCheckSum guard   RowSate        Schedule
------                  ---------    ---                            -----------------   -------        --------
SaveCustomQuestions     EF Core      SiteCode + CId                 Yes (effective)     int (0/1)      6 / 3
SaveCustomAnswers       EF Core      SiteCode + CaId + CaQid + CaCltid  Yes (effective) int (0/1)     6 / 3

Fields updated on checksum mismatch:
    SaveCustomQuestions: CQuestion + LastModAt + RowCheckSum + RowSate
    SaveCustomAnswers:   CaAns + LastModAt + RowCheckSum + RowSate

Critical bugs:
1. Both methods — e.InnerException.Message checked without null-guarding e.InnerException —
   NullReferenceException in catch block masks the original exception
2. Both methods — RowsIns and RowsUpd never incremented — all counts reported as 0
3. Both methods — RowTrax blocks empty — no audit logging
4. RowSate typo (missing 't') frozen in EF model and both methods





SaveCustomQA.cs — Method Metadata Tables
Method: SaveCustomQuestions
Field	Value
Name	SaveCustomQuestions
Module	Custom form question definitions
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode
Source table	dbo.tblCustomQuestions; column list + RowCheckSum from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_CustomQuestions
Load type	EF Core upsert — partial pre-pass RowSate reset (active records only); lookup by CId; effective RowCheckSum guard; new rows in NewCQs list; two-phase commit
Load type column	RowCheckSum (dual case labels: rowchksum / rowchecksum); RowSate (int, not bool — note typo); LastModAt stamped on every write
Frequency	Daily
Schedule	Schedule 6 — BHGTaskRunner.exe 6 (Samms-Forms) / Schedule 3 (catch-all)
Parent	Samms-Forms
Downstream	pats.tbl_CustomQuestions → clinic reporting and custom form display
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveCustomQuestions(SrcDt, st.SiteCode, null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveCustomQA.cs
Status	Active
Folder	BHG-DR-LIB\SaveCustomQA.cs; detail in SaveCustomQA-Documentation\SaveCustomQA_ETL_Complete_Documentation.md
Method: SaveCustomAnswers
Field	Value
Name	SaveCustomAnswers
Module	Patient answers to custom form questions
Layer	Target load / EF Core upsert
Source system	SAMMS (SQL Server — per clinic)
Source DB	ctrl.tbl_LocationCons.DbName where ActionKey = 6 + SiteCode
Source table	dbo.tblCustomAnswers; column list + RowCheckSum from dms.tbl_MapSrc2Dsn
Target DB	Azure SQL — BHG_DR
Target table	pats.tbl_CustomAnswers
Load type	EF Core upsert — partial pre-pass RowSate reset (active records only); triple composite key lookup (CaId + CaQid + CaCltid); effective RowCheckSum guard; new rows in NewCAs list; two-phase commit
Load type column	RowCheckSum (dual case labels: rowchksum / rowchecksum); RowSate driven by CaCltid sign (negative = inactive); LastModAt stamped on every write
Frequency	Daily
Schedule	Schedule 6 — BHGTaskRunner.exe 6 (Samms-Forms) / Schedule 3 (catch-all)
Parent	Samms-Forms
Downstream	pats.tbl_CustomAnswers → clinic reporting and custom form display
Connection / method	Source: sm.GetTableData(st.FromTblVw, strCmd, st.ConStr). Target: sd.SaveCustomAnswers(SrcDt, st.SiteCode, null)
Server / DB / API	Source: st.ConStr. Target: Azure BHG_DR via BHG_DRContext
Owner	BHGTaskRunner / BHG-DR-LIB\SaveCustomQA.cs
Status	Active
Folder	BHG-DR-LIB\SaveCustomQA.cs; detail in SaveCustomQA-Documentation\SaveCustomQA_ETL_Complete_Documentation.md