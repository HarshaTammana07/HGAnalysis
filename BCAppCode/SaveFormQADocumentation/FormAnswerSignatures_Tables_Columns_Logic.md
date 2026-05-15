
FormAnswerSignatures ETL — Tables, Columns & Logic Reference
BHGTaskRunner | SaveFormQAData.cs | pats.tbl_dbo_FormAnswerSignatures
________________________________________

1. ENTRY GATE
________________________________________

Before any SQL is built:

  Probe: SELECT name FROM sys.tables WHERE name = 'answersignature'
  If Rows.Count != 1 → skip entirely (ExceptMsg = "No AnswerSignature table.")

Lookback:
  DaysBack     = -15 (global)
  formDaysBack = DaysBack - 15  →  -30 days total
  wrkdt        = WorkDate.AddDays(-30).Date

  Special one-off override:
    if WorkDate.Date == 2/2/2024 → wrkdt = 1/1/2010  (full historical reload)
    [No Reload flag for this table — only this hard-coded date triggers full reload]

  NOTE: The WHERE clause on wrkdt is COMMENTED OUT in the base query.
        The base Form query pulls ALL forms regardless of date.
        Only Forms2Process custom table UNIONs respect DateFilterEnabled.


________________________________________
2. BASE QUERY — always runs (UNION 1 + UNION 2)
________________________________________

Source tables:
  dbo.Form (f)
  dbo.FormTemplate (ft)            LEFT JOIN  on f.FormTemplateId = ft.Id
  dbo.SF_PatientPreAdmission (pa)  INNER JOIN on f.PreAdmissionId = pa.ID
  dbo.SF_DataForms (d)             LEFT JOIN  on pa.DataFormId = d.Id
  dbo.AnswerSignature              Correlated subquery per signature column

UNION 1 — All active forms (no date filter — WHERE clause commented out):
  FROM Form f LEFT JOIN FormTemplate ft ... INNER JOIN SF_PatientPreAdmission pa ...
  LEFT JOIN SF_DataForms d ...

UNION 2 — Deleted forms:
  Same joins + WHERE f.Isdeleted = 1
  (Date filter in UNION 2 is also commented out)

Columns produced by the base query:

  Column            Source
  ────────────────  ──────────────────────────────────────────────────────────
  SiteCode          Hard-coded '{st.SiteCode}'
  FormName          ft.FormName  (from FormTemplate)
  FormId            f.id  (convert to varchar(100))
  ClientId          f.ClientId
  CreatedOn         f.CreatedOn
  UpdatedOn         f.UpdatedOn
  IsDeleted         CASE WHEN ISNULL(f.IsDeleted,0)=0
                         AND pa.IsDeleted <> 1
                         AND ISNULL(pa.DataFormId,0) >= 0
                         AND ISNULL(d.IsDeleted,0) = 0
                    THEN 0 ELSE 1 END

  ── 9 Signature Date columns (all via correlated subquery to AnswerSignature) ──

  Each column pattern:
    (SELECT TOP 1
       CASE WHEN Sign IS NULL THEN '1/1/1900' ELSE [DateTime] END
     FROM AnswerSignature
     WHERE FormId = x.FormId AND DateField = '{field}'
     ORDER BY [DateTime] DESC)

  Column                               DateField value
  ──────────────────────────────────── ─────────────────────────────────────
  CompletedBySignatureSignatureDate    'CompletedBySignatureSignatureDate'
  CounselorSignatureSignatureDate      'CounselorSignatureSignatureDate'
                                        OR 'CounselorSignatureDate'
  DoctorSignatureSignatureDate         'DoctorSignatureSignatureDate'
  MedicalProviderSignatureSignatureDate 'MedicalProviderSignatureSignatureDate'
  PatientSignatureDate                 'PatientSignatureDate'
  ProviderSignatureSignatureDate       'ProviderSignatureSignatureDate'
  RequestorSignatureDate               'RequestorSignatureDate'
  StaffSignatureDate                   'StaffSignatureDate'
  SupervisorSignatureSignatureDate     'SupervisorSignatureSignatureDate'

  Rule: if Sign IS NULL → return '1/1/1900' (sentinel for "form exists, not signed")
        if no row found → returns NULL


________________________________________
3. FORMS2PROCESS LOOP — Custom table UNIONs
________________________________________

  xForms = ctrl.tbl_Forms2Process WHERE Enabled = true AND RowState = true
           (no ORDER BY Prefix — unlike FormQA)

  For each xf WHERE xf.TableName != null:
    Probe: SELECT name FROM sys.tables WHERE name = xf.TableName
    If found → UNION a block via switch(xf.TableName)

  Final execute:
    SrcDt = GetTableData(strCmd)
    [No "SELECT DISTINCT * FROM (...) z" wrapper — unlike FormQA]


________________________________________
4. TOP-LEVEL SWITCH — 3 cases
________________________________________

──────────────────────────────────────────────────────────────────────────────
CASE: tblORDERREQ
──────────────────────────────────────────────────────────────────────────────

  Columns:
  Column                               Value
  ──────────────────────────────────── ────────────────────────────────────────────────
  SiteCode                             '{st.SiteCode}'
  FormName                             'Level Justification'  (hard-coded)
  FormID                               '9-1-' + ABS(cltID) + '-' + ReqNum + '-1'
  ClientId                             cltID
  CreatedOn                            convert(date, DateAdded)
  UpdatedOn                            convert(date, statusDate)
  IsDeleted                            CASE WHEN cltID < 0 THEN 1 ELSE 0 END
  CompletedBySignatureSignatureDate    null
  CounselorSignatureSignatureDate      null
  DoctorSignatureSignatureDate         null
  MedicalProviderSignatureSignatureDate null
  PatientSignatureDate                 null
  ProviderSignatureSignatureDate       ISNULL(DrSigDt, SigNurseDt)
                                         — if null AND Status='Approved' → '1900-01-01'
  RequestorSignatureDate               null
  StaffSignatureDate                   null
  SupervisorSignatureSignatureDate     sigCoordinatorDt
                                         — if null AND Status='Approved' → '1900-01-01'

  WHERE: status = 'Approved'
         AND Notes NOT LIKE 'Test %' AND Notes <> 'TEST'
         AND DrNote <> 'HEllo test' AND DrNote <> 'TEST'

  DateFilterEnabled (optional):
    AND (DateAdded >= wrkdt OR ISNULL(statusDate, DateAdded) >= wrkdt)
    [SupervisorSig / ProviderSig date filters are commented out]

  Join: Direct FROM tblORDERREQ — NO SF_PatientPreAdmission join
  No PreAdmissionId column in this case

──────────────────────────────────────────────────────────────────────────────
CASE: tblTP17REVIEW
──────────────────────────────────────────────────────────────────────────────

  Columns:
  Column                               Value
  ──────────────────────────────────── ────────────────────────────────────────────────
  SiteCode                             '{st.SiteCode}'
  FormName                             'TP-' + tprType
  FormID                               '8-1-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID
  ClientId                             tprCLTID  (raw, not ABS — saved as-is)
  PreAdmissionId                       null
  CreatedOn                            convert(date, tprDT)
  UpdatedOn                            null
  IsDeleted                            CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END
  CompletedBySignatureSignatureDate    null
  CounselorSignatureSignatureDate      null
  DoctorSignatureSignatureDate         null
  MedicalProviderSignatureSignatureDate null
  PatientSignatureDate                 CASE WHEN convert(date, tprCLIRNTSIGDate) IS NULL
                                            THEN '1900-01-01'
                                            ELSE convert(date, tprCLIRNTSIGDate) END
  ProviderSignatureSignatureDate       CASE WHEN convert(date, tprDRSIGDate) IS NULL
                                            THEN '1900-01-01'
                                            ELSE convert(date, tprDRSIGDate) END
  RequestorSignatureDate               null
  StaffSignatureDate                   CASE WHEN tprCOUNSSIGDate IS NULL
                                             AND tprSUPERSIGDate IS NULL
                                            THEN '1900-01-01'
                                            ELSE convert(date, tprCOUNSSIGDate) END
  SupervisorSignatureSignatureDate     convert(date, tprSUPERSIGDate)  ← raw, no null guard

  Join: Direct FROM tblTP17REVIEW — NO SF_PatientPreAdmission join

  DateFilterEnabled (optional — checks 7 columns):
    WHERE (CreatedOn >= wrkdt
        OR ISNULL(UpdatedOn, CreatedOn) >= wrkdt
        OR ProviderSignatureSignatureDate >= wrkdt
        OR CompletedBySignatureSignatureDate >= wrkdt
        OR PatientSignatureDate >= wrkdt
        OR StaffSignatureDate >= wrkdt
        OR SupervisorSignatureSignatureDate >= wrkdt)

──────────────────────────────────────────────────────────────────────────────
CASE: default  (all other Forms2Process TableNames — two nested sub-levels)
──────────────────────────────────────────────────────────────────────────────

  Has two levels of inner switches:
    Level A — FormID / ClientId (inner switch on xf.TableName)
    Level B — Each of the 9 signature date columns (inner switch on xf.TableName per column)


________________________________________
5. DEFAULT CASE — LEVEL A: FormID / ClientId inner switch
________________________________________

  TableName                  FormID formula                                          ClientId
  ─────────────────────────  ──────────────────────────────────────────────────────  ────────────────────
  SF_PatientPreAdmission     '{Prefix}-{ISNULL(pa.PatientID,0)}-{a.ParentPreAdmId}-{a.id}'  pa.PatientID
  SF_DataForm                '{Prefix}-{ISNULL(pa.PatientID,0)}-{ISNULL(a.PreAdmId,0)}-{a.id}' pa.PatientID
  SF_UnderstandingOfTreatment '{Prefix}-{ISNULL(pa.PatientID,0)}-{a.PreAdmId}-{a.id}'  pa.PatientID
  InsuranceBenefitVerification '{Prefix}-{ISNULL(pa.PatientID,0)}-{a.PreAdmId}-{a.id}' pa.PatientID
  FinancialHardshipApplication '{Prefix}-{ISNULL(a.CltID,0)}-{a.PreAdmId}-{a.id}'     a.CltID  ← different
  xNewAdmissionAssessment     '{Prefix}-{ISNULL(b.ClientId,0)}-{b.PreAdmId}-{b.id}'   b.ClientId ← from join alias
  default                     '{Prefix}-{ISNULL(a.ClientId,0)}-{a.PreAdmId}-{a.id}'   a.ClientId

  Notes:
  - SF_PatientPreAdmission, SF_DataForm, SF_UnderstandingOfTreatment,
    InsuranceBenefitVerification → ClientId comes from pa.PatientID (the joined PA table)
  - FinancialHardshipApplication → uses a.CltID (different column name)
  - xNewAdmissionAssessment → uses b. alias (from NewAdmissionAssessmentASAMDimension6 join)


________________________________________
6. DEFAULT CASE — LEVEL B: Shared columns (same for all default sub-cases)
________________________________________

  Column            Source
  ────────────────  ────────────────────────────────────────────────
  CreatedOn         convert(date, a.{xf.CreatedOn})
  UpdatedOn         convert(date, a.{xf.ModifiedOn})  or null if ModifiedOn is null
  IsDeleted         CASE WHEN ISNULL(a.IsDeleted,0)=0
                         AND pa.IsDeleted <> 1
                         AND ISNULL(pa.DataFormId,0) >= 0
                         AND ISNULL(d.IsDeleted,0) = 0
                    THEN 0 ELSE 1 END


________________________________________
7. DEFAULT CASE — LEVEL B: 9 Signature date columns
________________________________________

  Each signature column checks: if xf.{Field} != null → generate CASE expression
                                 if xf.{Field} == null → column = null

  Expression when set:
    CASE WHEN convert(date, {alias}.{xf.Field}) IS NULL
         THEN '1900-01-01'
         ELSE convert(date, {alias}.{xf.Field})
    END

  The alias used (a. / b. / aas.) depends on xf.TableName per column:

  ┌──────────────────────────────────────┬──────────────────────┬─────────────────────┬──────────────┐
  │ Signature Column                     │ AdmissionAssessment  │ NewAdmissionAssmt   │ All others   │
  ├──────────────────────────────────────┼──────────────────────┼─────────────────────┼──────────────┤
  │ CompletedBySignatureSignatureDate    │ a.                   │ b.                  │ a.           │
  │ CounselorSignatureSignatureDate      │ a.                   │ b.                  │ a.           │
  │ DoctorSignatureSignatureDate         │ a.                   │ b.                  │ a.           │
  │ MedicalProviderSignatureSignatureDate│ a.                   │ b.                  │ a.           │
  │ PatientSignatureDate                 │ aas. ⚠               │ b.                  │ a.           │
  │ ProviderSignatureSignatureDate       │ aas. ⚠               │ b.                  │ a.           │
  │ RequestorSignatureDate               │ a. (no table switch) │ a. (no table switch)│ a.           │
  │ StaffSignatureDate                   │ aas. ⚠               │ b.                  │ a. *         │
  │ SupervisorSignatureSignatureDate     │ aas. ⚠               │ b.                  │ a.           │
  └──────────────────────────────────────┴──────────────────────┴─────────────────────┴──────────────┘

  ⚠  AdmissionAssessment reads Patient / Provider / Staff / Supervisor from
     AdmissionAssessmentSummary (alias aas), not the main table row (a).

  *  SF_PatientPreAdmission special rule on StaffSignatureDate:
       if SiteCode.ToUpper() == "LAB" → StaffSignatureDate = null  (hardcoded skip)
       else → normal a.{xf.Staff} expression


________________________________________
8. DEFAULT CASE — Join strategy
________________________________________

  Base join (all default sub-cases):
    FROM {xf.TableName} a
    INNER JOIN SF_PatientPreAdmission pa ON a.PreAdmissionId = pa.ID
    LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

  Exception — SF_PatientPreAdmission:
    FROM SF_PatientPreAdmission a
    INNER JOIN SF_PatientPreAdmission pa ON a.ID = pa.ID   ← self-join on ID not PreAdmissionId

  Additional join — AdmissionAssessment:
    INNER JOIN AdmissionAssessmentSummary aas
      ON a.Id = aas.AdmissionAssessmentId
      AND a.PreAdmissionId = aas.PreAdmissionId

  Additional join — NewAdmissionAssessment:
    INNER JOIN NewAdmissionAssessmentASAMDimension6 b
      ON a.preadmissionID = b.preadmissionID
      AND a.ID = b.NewAdmissionAssessmentFormId

  DateFilterEnabled (optional, default sub-case only):
    WHERE a.{xf.CreatedOn} >= wrkdt
       OR ISNULL(a.{xf.ModifiedOn}, a.{xf.CreatedOn}) >= wrkdt


________________________________________
9. SIGNATURE SOURCE QUICK REFERENCE
________________________________________

  TableName                    Sig source   Patient    Provider   Staff      Supervisor
  ───────────────────────────  ──────────   ────────── ────────── ────────── ──────────
  Base Form query (Form tbl)   AnswerSig    subquery   subquery   subquery   subquery
  tblORDERREQ                  Direct col   null       DrSigDt/   null       sigCoordinatorDt
                                                       SigNurseDt
  tblTP17REVIEW                Direct col   tprCLIRNT  tprDRSIG   tprCOUNS  tprSUPER
                                            SIGDate    Date       SIGDate    SIGDate
  AdmissionAssessment (def.)   Direct col   aas.col    aas.col    aas.col    aas.col
  NewAdmissionAssessment (def.)Direct col   b.col      b.col      b.col      b.col
  All other default tables     Direct col   a.col      a.col      a.col *    a.col


________________________________________
10. SaveAnswerSignatures EF LOGIC
________________________________________

Step 1 — Load existing Azure rows:
  Ans = db.TblDboFormAnswerSignatures WHERE SiteCode = sc

Step 2 — PRE-PASS (soft RowState reset):
  For each existing row d:
    formname = d.FormName
    if d.FormName.StartsWith("TP-") → formname = "Treatment Plan"
    xf = f2p.FirstOrDefault(x => x.FormName == formname)

    if xf found:
      if xf.DateFilterEnabled:
        if (CreatedOn >= wrkdt OR UpdatedOn >= wrkdt) AND RowState == 1
          → d.RowState = 0
      else → d.RowState = 0  (unconditional reset)

    if xf NOT found:
      if (CreatedOn >= wrkdt OR UpdatedOn >= wrkdt) AND RowState == 1
        → d.RowState = 0

  db.SaveChanges()   ← pre-pass IS committed here (unlike FormQA where it was deferred)

Step 3 — UPSERT loop (foreach source row):
  Build a via column switch:

    Column                                Mapping
    ─────────────────────────────────── ──────────────────────────────────────────────
    sitecode                              a.SiteCode = sc; a.RowState = 1; a.LastModAt = Now
    formname                              a.FormName
    formid                                a.FormId.ToUpper()
    clientid                              a.ClientId = Math.Abs(int.Parse(value))
                                          if original value < 0 → a.RowState = 0
    createdon                             a.CreatedOn  (skip if length <= 6)
    updatedon                             a.UpdatedOn  (skip if length <= 6)
    completedbysignaturesignaturedate     a.CompletedBySignatureSignatureDate
    counselorsignaturesignaturedate       a.CounselorSignatureSignatureDate
    doctorsignaturesignaturedate          a.DoctorSignatureSignatureDate
    medicalprovidersignaturesignaturedate a.MedicalProviderSignatureSignatureDate
    patientsignaturedate                  a.PatientSignatureDate
    providersignaturesignaturedate        a.ProviderSignatureSignatureDate
    requestorsignaturedate                a.RequestorSignatureDate
    staffsignaturedate                    a.StaffSignatureDate
    supervisorsignaturesignaturedate      a.SupervisorSignatureSignatureDate
    rowchksum                             a.RowChkSum = int.Parse(value)
    isdeleted                             if "1" → a.RowState = 0
                                          else if ClientId < 0 → a.RowState = 0
                                          else → a.RowState = 1

  NOTE: ClientId is always stored as Math.Abs (positive) — RowState carries the negative signal.
  NOTE: RowChkSum IS present on TblDboFormAnswerSignatures (unlike FormQuestionAnswers).

  PK lookup (in-memory on Ans list):
    SiteCode + FormName + FormId(toUpper) + ClientId

    Found → update ALL fields (RowChkSum guard is COMMENTED OUT — always updates):
              RowChkSum, CreatedOn, UpdatedOn, all 9 signature date columns,
              LastModAt, RowState
            rc.RowsUpd++

    Not found → newAns.Add(a)
                rc.RowsIns++

Step 4 — Commit:
  db.SaveChanges()                        ← flushes all updates
  if newAns.Count > 0:
    db.TblDboFormAnswerSignatures.AddRange(newAns)
    db.SaveChanges()                      ← inserts new rows


________________________________________
11. DIFFERENCES vs FormQuestionAnswers
________________________________________

  Aspect                       FormQuestionAnswers              FormAnswerSignatures
  ──────────────────────────── ──────────────────────────────── ─────────────────────────────────
  Table probed                 sys.tables: 'Form'               sys.tables: 'answersignature'
  Full reload trigger          Reload flag = true               WorkDate == 2/2/2024 (hard-coded)
  Base query date filter       Active (WHERE on CreatedOn)      Commented out — pulls ALL forms
  SELECT DISTINCT wrapper      Yes — "SELECT DISTINCT * FROM z" No wrapper
  Forms2Process ORDER BY       ORDER BY Prefix                  No ORDER BY
  Top-level switch cases       9 cases + default                tblORDERREQ, tblTP17REVIEW, default
  Signature columns            None — not applicable            9 columns from AnswerSignature subquery
                                                                 or direct table columns
  RowChkSum                    NOT present on entity            Present — mapped and stored
  RowChkSum update guard       Not applicable                   Guard COMMENTED OUT (always updates)
  ClientId handling            Raw value; if < 0 → RowState=0  Math.Abs stored; if orig < 0 → RowState=0
  Pre-pass SaveChanges         Deferred (commented out)         Committed immediately
  Bulk load path               Yes (18 site allowlist)          No — always EF path
  BAMMerge call                Yes (both bulk and EF)           No BAMMerge call
  PK key columns               6 fields incl QuestionId+OrderId 4 fields: SiteCode+FormName+FormId+ClientId
  RowTrax                      Check present, body empty        Check present, body empty


________________________________________
12. ROWTRAX
________________________________________

  st.RowTrax check is present in BHGTaskRunner after SaveAnswerSignatures.
  The body is EMPTY — no SaveRowTrax call is made for this table.


________________________________________
13. DEAD CODE (disabled)
________________________________________

  The following were coded but commented out and never run:
    - SuicideSeverityRatingScale  (for both FormQA and AnswerSignatures)
    - SAFETProtocolwithCSSRS  (Suicide Severity Rating Scale 2.0)
