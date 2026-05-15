
FormQuestionAnswers ETL — Tables, Columns & Logic Reference
BHGTaskRunner | SaveFormQAData.cs | pats.tbl_dbo_FormQuestionAnswers
________________________________________

1. ENTRY GATE
________________________________________

Before any SQL is built:

  Probe: SELECT name FROM sys.tables WHERE name = 'Form'
  If Rows.Count != 1 → skip entirely (ExceptMsg = "No Form table.")

Lookback:
  DaysBack     = -15 (global)
  formDaysBack = DaysBack - 15  →  -30 days total
  wrkdt        = WorkDate.AddDays(-30).Date

  if Reload == true → wrkdt = 1/1/2010  (full historical reload)


________________________________________
2. BASE QUERY — always runs (UNION 1 + UNION 2)
________________________________________

Source tables:
  dbo.Form (f)
  dbo.FormTemplate (ft)          LEFT JOIN  on f.FormTemplateId = ft.Id
  dbo.Question (q)               LEFT JOIN  on ft.Id = q.FormTemplateId
  dbo.Answer (a)                 LEFT JOIN  on f.Id = a.FormId AND q.Id = a.QuestionId
  dbo.SF_PatientPreAdmission (pa) INNER JOIN on f.PreAdmissionId = pa.ID
  dbo.SF_DataForms (d)           LEFT JOIN  on pa.DataFormId = d.Id

Outer wrapper adds QuestionOrderId via ROW_NUMBER() when null:
  QuestionOrderId = ISNULL(x.QuestionOrderId,
    ROW_NUMBER() OVER (PARTITION BY x.FormName, x.FormId, x.ClientId, x.QuestionId
                       ORDER BY x.QuestionId, x.AnswerSeq))

UNION 1 — Forms with answered questions:
  WHERE a.Value IS NOT NULL
  AND (f.CreatedOn >= wrkdt OR ISNULL(f.UpdatedOn, f.CreatedOn) >= wrkdt)

UNION 2 — Forms with no questions (q.Id is null):
  WHERE q.Id IS NULL
  AND (f.CreatedOn >= wrkdt OR ISNULL(f.UpdatedOn, f.CreatedOn) >= wrkdt)

Columns produced by the base query:

  Column            Source
  ─────────────     ──────────────────────────────────────────────────────
  SiteCode          Hard-coded '{st.SiteCode}'
  FormName          ft.FormName  (from FormTemplate)
  FormId            f.id  (convert to varchar(100))
  ClientId          f.ClientId
  PreAdmissionId    f.PreAdmissionId
  QuestionId        ISNULL(q.Id, 0)
  QuestionOrderId   q.QuestionOrderId  (wrapped in ROW_NUMBER outer query)
  QuestionText      q.QuestionText
  OptionId          a.OptionId
  AnswerValue       a.Value
  AnswerSeq         a.Id  (used only for ROW_NUMBER ordering, not saved)
  CreatedBy         f.CreatedBy
  CreatedOn         f.CreatedOn
  UpdatedBy         f.UpdatedBy
  UpdatedOn         f.UpdatedOn
  IsDeleted         CASE WHEN ISNULL(f.IsDeleted,0)=0
                         AND pa.IsDeleted <> 1
                         AND ISNULL(pa.DataFormId,0) >= 0
                         AND ISNULL(d.IsDeleted,0) = 0
                    THEN 0 ELSE 1 END


________________________________________
3. FORMS2PROCESS LOOP — Custom table UNIONs
________________________________________

  xForms = ctrl.tbl_Forms2Process WHERE Enabled = true AND RowState = true
           ORDER BY Prefix

  For each xf WHERE xf.TableName != null:
    Probe: SELECT name FROM sys.tables WHERE name = xf.TableName
    If found → UNION a block into strCmd via switch(xf.TableName.ToLower())

  After all UNIONs:
    SrcDt = GetTableData("SELECT DISTINCT * FROM (" + strCmd + ") z")


________________________________________
4. CUSTOM TABLE SWITCH CASES
________________________________________

All custom cases produce the same outer column list as the base query:
  SiteCode, FormName, FormID, PreAdmissionId, ClientId,
  QuestionID, QuestionOrderID, QuestionText, OptionID, AnswerValue,
  CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted

What differs per case is: FormID formula, ClientId source, PreAdmissionId,
QuestionID/Text/AnswerValue content, UpdatedBy source, and join strategy.

─────────────────────────────────────────────────────────────────────────────
CASE: adversechildhood
─────────────────────────────────────────────────────────────────────────────
  FormName        xf.FormName (from Forms2Process)
  FormID          '{Prefix}-{a.PreAdmissionId}-{a.PreAdmissionId}-{a.id}'
  PreAdmissionId  a.PreAdmissionId
  ClientId        a.ClientId
  QuestionID      0
  QuestionOrderID 1
  QuestionText    null
  OptionID        null
  AnswerValue     null
  CreatedBy       a.Createdby
  CreatedOn       convert(date, a.{xf.CreatedOn})
  UpdatedBy       a.ModifiedBy
  UpdatedOn       convert(date, a.{xf.ModifiedOn})  or null if ModifiedOn is null
  IsDeleted       Standard 4-condition CASE (f/pa/d)
  Join            adversechildhood a
                  INNER JOIN SF_PatientPreAdmission pa ON a.PreAdmissionId = pa.ID
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: financialhardshipapplication
─────────────────────────────────────────────────────────────────────────────
  FormName        xf.FormName
  FormID          '{Prefix}-{a.cltId}-{a.PreAdmissionId}-{a.id}'
  PreAdmissionId  a.PreAdmissionId
  ClientId        a.cltId                ← different column name (cltId not ClientId)
  QuestionID      0
  QuestionOrderID 1
  QuestionText    null
  OptionID        null
  AnswerValue     null
  CreatedBy       a.Createdby
  CreatedOn       convert(date, a.{xf.CreatedOn})
  UpdatedBy       a.ModifiedBy
  UpdatedOn       convert(date, a.{xf.ModifiedOn})  or null
  IsDeleted       Standard 4-condition CASE
  Join            financialhardshipapplication a
                  INNER JOIN SF_PatientPreAdmission pa ON a.PreAdmissionId = pa.ID
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: tbltp17review  — REAL question data, 3–4 UNIONs, NO pa join
─────────────────────────────────────────────────────────────────────────────
  Each UNION is a separate question row from the same tblTP17REVIEW record.

  UNION A — QuestionID 1
    FormName        'TP-' + tprType
    FormID          '8-1-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID
    PreAdmissionId  null
    ClientId        ABS(tprCLTID)
    QuestionID      1
    QuestionOrderID 1
    QuestionText    'Treatment Plan Type'
    AnswerValue     TprTYPE
    CreatedBy       tprCreatedby
    CreatedOn       convert(date, tprDT)
    UpdatedBy       null
    UpdatedOn       null
    IsDeleted       CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END

  UNION B — QuestionID 2
    QuestionText    'Treatment Phase Type'
    AnswerValue     tpTreatmentPhase
    FormID          '8-2-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID

  UNION C — QuestionID 3
    QuestionText    'Next Due'
    AnswerValue     convert(varchar, tprNEXT)
    FormID          '8-3-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID

  UNION D — QuestionID 4  (only if column tprReviewFrequency exists in sys.all_columns)
    Probe: SELECT * FROM sys.all_columns WHERE object_id = OBJECT_ID('tblTP17REVIEW')
                                          AND name = 'tprReviewFrequency'
    QuestionText    'Review Frequency'
    AnswerValue     RTRIM(SUBSTRING(tprReviewFrequency, 6, LEN-5))
                    (strips leading 'Every' prefix if > 6 chars, else raw value)
    FormID          '8-4-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID

  Join: Direct FROM tblTP17REVIEW — NO SF_PatientPreAdmission join

─────────────────────────────────────────────────────────────────────────────
CASE: tblorderreq  — REAL question data, 2 UNIONs, NO pa join
─────────────────────────────────────────────────────────────────────────────
  WHERE status = 'Approved'
  AND Notes NOT LIKE 'Test %' AND Notes <> 'TEST'
  AND DrNote <> 'HEllo test' AND DrNote <> 'TEST'

  UNION A — QuestionID 1
    FormName        'Level Justification' (hard-coded)
    FormID          '9-1-' + ABS(cltID) + '-' + ReqNum + '-'
    PreAdmissionId  ReqNum                ← uses ReqNum as PreAdmissionId
    ClientId        cltID
    QuestionID      1
    QuestionText    'Effective Date'
    OptionID        0
    AnswerValue     convert(varchar, EffectiveDate, 101)
    CreatedBy       Staff
    CreatedOn       convert(date, DateAdded)
    UpdatedBy       StatusUser
    UpdatedOn       convert(date, statusDate)
    IsDeleted       CASE WHEN cltID < 0 THEN 1 ELSE 0 END

  UNION B — QuestionID 2
    FormID          '9-2-' + ABS(cltID) + '-' + ReqNum + '-'
    QuestionID      2
    QuestionText    'Expiration Date'
    AnswerValue     convert(varchar, expirationdate, 101)

  Join: Direct FROM tblORDERREQ — NO SF_PatientPreAdmission join

─────────────────────────────────────────────────────────────────────────────
CASE: insurancebenefitverification
─────────────────────────────────────────────────────────────────────────────
  FormID          '{Prefix}-{a.PreAdmissionId}-{a.PreAdmissionId}-{a.id}'
  ClientId        a.PreAdmissionId        ← QUIRK: PreAdmissionId used as ClientId
  PreAdmissionId  a.PreAdmissionId
  QuestionID      0 / QuestionText null / AnswerValue null
  UpdatedBy       a.ModifiedBy
  Join            INNER JOIN SF_PatientPreAdmission pa ON (a.PreAdmissionId = pa.ID)
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: referralform
─────────────────────────────────────────────────────────────────────────────
  FormID          '{Prefix}-{a.ClientId}-{a.PreAdmissionId}-{a.id}'
  ClientId        a.ClientId
  PreAdmissionId  a.PreAdmissionId
  QuestionID      0 / QuestionText null / AnswerValue null
  UpdatedBy       a.updatedby             ← different column name vs others
  Join            INNER JOIN SF_PatientPreAdmission pa ON a.PreAdmissionId = pa.ID
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: sf_understandingoftreatment
─────────────────────────────────────────────────────────────────────────────
  FormID          '{Prefix}-{pa.PatientId}-{ISNULL(a.PreAdmissionId,0)}-{ISNULL(a.id,0)}'
  ClientId        pa.PatientId            ← from the joined PA table, not a.
  PreAdmissionId  a.PreAdmissionId
  QuestionID      0 / QuestionText null / AnswerValue null
  UpdatedBy       a.LastUpdatedBy         ← different column name
  Join            INNER JOIN SF_PatientPreAdmission pa ON a.PreAdmissionId = pa.ID
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: sf_patientpreadmission
─────────────────────────────────────────────────────────────────────────────
  FormID          '{Prefix}-{a.PatientId}-{ISNULL(a.ParentPreAdmissionId,0)}-{ISNULL(a.id,0)}'
  ClientId        a.PatientId
  PreAdmissionId  a.Id                    ← the record itself IS the pre-admission row
  QuestionID      0 / QuestionText null / AnswerValue null
  UpdatedBy       a.LastUpdatedBy
  Join            INNER JOIN SF_PatientPreAdmission pa ON a.ID = pa.ID  ← self-join on ID
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: newperiodicreassessment
─────────────────────────────────────────────────────────────────────────────
  FormID          '{Prefix}-{a.PatientId}-{ISNULL(a.ParentPreAdmissionId,0)}-{ISNULL(a.id,0)}'
  ClientId        a.PatientId
  PreAdmissionId  a.Id                    ← same quirk as sf_patientpreadmission
  QuestionID      0 / QuestionText null / AnswerValue null
  UpdatedBy       a.LastUpdatedBy
  Join            INNER JOIN SF_PatientPreAdmission pa ON a.ID = pa.ID  ← self-join
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

─────────────────────────────────────────────────────────────────────────────
CASE: default  (catches all other Forms2Process TableNames)
─────────────────────────────────────────────────────────────────────────────
  FormID          '{Prefix}-{ISNULL(a.ClientId,'0')}-{a.PreAdmissionId}-{a.id}'
  ClientId        a.ClientId
  PreAdmissionId  a.PreAdmissionId
  QuestionID      0 / QuestionText null / AnswerValue null
  UpdatedBy       a.ModifiedBy
  Join            INNER JOIN SF_PatientPreAdmission pa ON a.PreAdmissionId = pa.ID
                  LEFT JOIN SF_DataForms d ON pa.DataFormId = d.Id

  Optional DateFilterEnabled:
    WHERE a.{xf.CreatedOn} >= wrkdt
       OR ISNULL(a.{xf.ModifiedOn}, a.{xf.CreatedOn}) >= wrkdt


________________________________________
5. QUICK REFERENCE — WHAT DIFFERS PER CASE
________________________________________

  Case                         FormID key parts                   ClientId from      UpdatedBy col      pa join key
  ───────────────────────────  ─────────────────────────────────  ─────────────────  ─────────────────  ──────────────────
  adversechildhood             Prefix-PreAdmId-PreAdmId-id        a.ClientId         a.ModifiedBy       a.PreAdmissionId
  financialhardshipapplication Prefix-cltId-PreAdmId-id           a.cltId            a.ModifiedBy       a.PreAdmissionId
  tbltp17review (8-x)          8-{1-4}-ABS(tprCLTID)-tpRID-tprTPID ABS(tprCLTID)   null               no pa join
  tblorderreq (9-x)            9-{1-2}-ABS(cltID)-ReqNum          cltID              StatusUser         no pa join
  insurancebenefitverification Prefix-PreAdmId-PreAdmId-id        a.PreAdmissionId   a.ModifiedBy       a.PreAdmissionId
  referralform                 Prefix-ClientId-PreAdmId-id        a.ClientId         a.updatedby        a.PreAdmissionId
  sf_understandingoftreatment  Prefix-pa.PatientId-PreAdmId-id    pa.PatientId       a.LastUpdatedBy    a.PreAdmissionId
  sf_patientpreadmission       Prefix-PatientId-ParentPAId-id     a.PatientId        a.LastUpdatedBy    a.ID = pa.ID
  newperiodicreassessment      Prefix-PatientId-ParentPAId-id     a.PatientId        a.LastUpdatedBy    a.ID = pa.ID
  default                      Prefix-ClientId-PreAdmId-id        a.ClientId         a.ModifiedBy       a.PreAdmissionId

  Columns always null (all cases except tbltp17review/tblorderreq):
    QuestionText = null
    OptionID     = null
    AnswerValue  = null

  Only tbltp17review and tblorderreq carry real QuestionText + AnswerValue.


________________________________________
6. LOAD PATH — BULK vs EF
________________________________________

After SrcDt = GetTableData("SELECT DISTINCT * FROM (" + strCmd + ") z"):

  BULK PATH — SiteCode IN:
    B37, DM, GAL, HGT, LV1, NC, PH, D07, B26, B24, DRD-SF,
    V12, B35, B25, V9, FW, LO, B42
    1. Truncate stg.tbl_FormQA
    2. SqlBulkCopy → stg.tbl_FormQA
    3. exec stg.sp_FormQA_Merge @sitecode
    4. exec pats.BAMMerge        @sitecode

  EF PATH — all other sites:
    sd.SaveFormQuestionAnswers(SrcDt, SiteCode, wrkdt, xForms, null)
    exec pats.BAMMerge @sitecode   ← called after EF path too


________________________________________
7. SaveFormQuestionAnswers EF LOGIC
________________________________________

Step 1 — Load existing Azure rows:
  fqas = db.TblDboFormQuestionAnswers WHERE SiteCode = sc  (full site slice)

Step 2 — PRE-PASS (soft RowState reset):
  For each existing row d:
    formname = d.FormName
    if d.FormName.StartsWith("TP-") → formname = "Treatment Plan"
    xf = f2p.FirstOrDefault(x => x.FormName == formname)

    if xf found:
      if xf.DateFilterEnabled:
        if (CreatedOn >= wrkdt OR UpdatedOn >= wrkdt) AND RowState == 1
          → d.RowState = 0
      else (no date filter):
        → d.RowState = 0  (unconditional reset)

    if xf NOT found:
      if (CreatedOn >= wrkdt OR UpdatedOn >= wrkdt) AND RowState == 1
        → d.RowState = 0

  [db.SaveChanges() — COMMENTED OUT here; deferred to end]

Step 3 — UPSERT loop (foreach source row):
  Build fqa via column switch:

    Column          Mapping
    ──────────────  ─────────────────────────────────────────────────────
    sitecode        fqa.SiteCode = sc; fqa.LastModAt = DateTime.Now
    formname        fqa.FormName
    formid          fqa.FormId.ToUpper()
    clientid        fqa.ClientId = int.Parse(value)
                    if ClientId < 0 → fqa.RowState = 0
                    else            → fqa.RowState = 1
    createdon       fqa.CreatedOn
    createdby       fqa.CreatedBy
    updatedon       fqa.UpdatedOn
    updatedby       fqa.UpdatedBy
    preadmissionid  fqa.PreAdmissionId  (default -1 if empty)
    isdeleted       if "1" → fqa.IsDeleted = true; fqa.RowState = 0
                    else   → fqa.RowState = 1
    ischildform     fqa.IsChildForm (true/false)
    questionid      fqa.QuestionId  (0 if empty)
    questionorderid fqa.QuestionOrderId
    questiontext    fqa.QuestionText
    optionid        fqa.OptionId
    answervalue     fqa.AnswerValue

  NOTE: No RowChkSum on TblDboFormQuestionAnswers — no checksum logic here.

  PK lookup (in-memory on fqas list):
    SiteCode + FormName + FormId(toUpper) + ClientId + PreAdmissionId
    + QuestionId + QuestionOrderId

    Found     → update PreAdmissionId, CreatedBy/On, UpdatedBy/On,
                        AnswerValue, OptionId, LastModAt, RowState
                rCodes.RowsUpd++
    Not found → newfqas.Add(fqa)
                rCodes.RowsIns++

Step 4 — Commit:
  db.SaveChanges()                            ← flushes all updates
  if newfqas.Count > 0:
    db.TblDboFormQuestionAnswers.AddRange(newfqas)
    db.SaveChanges()                          ← inserts new rows


________________________________________
8. ROWTRAX
________________________________________

  st.RowTrax check is present in BHGTaskRunner after SaveFormQuestionAnswers.
  The body is EMPTY — no SaveRowTrax call is made for this table.


________________________________________
9. DEAD CODE (disabled)
________________________________________

  The following tables were coded but commented out and never run:
    - SuicideSeverityRatingScale
    - SAFETProtocolwithCSSRS  (Suicide Severity Rating Scale 2.0)
