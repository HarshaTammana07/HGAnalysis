# Forms ETL — Complete Simulation with Dummy Data

**Pipeline:** `Samms-Forms` (BHGTaskRunner.exe arg=6)  
**Site used in this simulation:** `B01` (Virginia Beach — EST)  
**Run date:** 2026-04-22  
**Lookback window:** -30 days → pulls data from 2026-03-23 onwards

---

## PART 1 — What is in the task queue before BHGTaskRunner runs?

The Scheduler already created these rows in `tsk.tbl_Tasks2`:

```
tsk.tbl_Tasks2
──────────────────────────────────────────────────────────────────────────────
TaskId | ParentTaskId | TaskName                           | SiteCode | Status
──────────────────────────────────────────────────────────────────────────────
 5000  | NULL         | Samms-Forms                        | All      | 17 ← PARENT
 5001  | 5000         | pats.tbl_dbo_FormQuestionAnswers   | B01      | 17 ← child
 5002  | 5000         | pats.tbl_dbo_FormAnswerSignatures  | B01      | 17 ← child
 5003  | 5000         | pats.tbl_dbo_FormQuestionAnswers   | B24      | 17 ← child
 5004  | 5000         | pats.tbl_dbo_FormAnswerSignatures  | B24      | 17 ← child
 ...   | 5000         | ... (115 sites × 2 = 230 rows)
```

**Why only 2 table names?** Those are the 2 "container" tasks. The other 3 destination tables (`tbl_EandMFormMDM`, `tbl_EandMFormPregnancy`, `tbl_ComprehensiveAssessmentForm`) run inline inside the same process — they are not separate task rows.

---

## PART 2 — What is in `ctrl.tbl_Forms2Process` (Azure BHG_DR)?

This config table tells the ETL about **extra form tables** in SAMMS that don't use the standard `Form / Question / Answer` structure.

```
ctrl.tbl_Forms2Process
──────────────────────────────────────────────────────────────────────────────────
Id | FormName          | TableName      | Prefix | DateFilterEnabled | Enabled
──────────────────────────────────────────────────────────────────────────────────
 1 | Treatment Plan    | tblTP17REVIEW  | 8      | true              | true
 2 | Level Justif.     | tblORDERREQ    | 9      | true              | true
 3 | Counseling Note   | NULL           | NULL   | true              | true
 4 | Intake Assessment | NULL           | NULL   | false             | true
```

- Rows with `TableName = NULL` → standard Form/Answer structure, no extra UNION needed
- Rows with `TableName = tblTP17REVIEW` → this form lives in its own SAMMS table → needs a UNION

---

## PART 3 — What is in the SAMMS database for site B01?

### 3a. `dbo.Form` — form instances

```
dbo.Form  (on B01's SAMMS SQL Server)
────────────────────────────────────────────────────────────────────────────────
Id  | FormTemplateId | ClientId | PreAdmissionId | CreatedOn   | UpdatedOn   | IsDeleted
────────────────────────────────────────────────────────────────────────────────
101 | 10             | 5001     | 9001           | 2026-04-01  | NULL        | 0
102 | 10             | 5002     | 9002           | 2026-04-05  | 2026-04-10  | 0
103 | 11             | 5003     | 9003           | 2026-03-10  | NULL        | 0  ← outside 30-day window
104 | 12             | 5001     | 9001           | 2026-04-15  | NULL        | 1  ← IsDeleted=1
```

### 3b. `dbo.FormTemplate` — what type of form each is

```
dbo.FormTemplate
──────────────────────────────
Id  | FormName
──────────────────────────────
10  | Counseling Note
11  | Intake Assessment
12  | Counseling Note
```

### 3c. `dbo.Question` — questions per form type

```
dbo.Question
──────────────────────────────────────────────────────────────────
Id  | FormTemplateId | QuestionOrderId | QuestionText
──────────────────────────────────────────────────────────────────
201 | 10             | 1               | How are you feeling today?
202 | 10             | 2               | Any substance use this week?
203 | 12             | 1               | How are you feeling today?
```

### 3d. `dbo.Answer` — patient answers

```
dbo.Answer
──────────────────────────────────────────────────────────────────────
Id  | FormId | QuestionId | OptionId | Value
──────────────────────────────────────────────────────────────────────
301 | 101    | 201        | NULL     | I feel much better
302 | 101    | 202        | NULL     | No substance use
303 | 102    | 201        | NULL     | Struggling a bit
304 | 102    | 202        | NULL     | One relapse this week
```

### 3e. `dbo.SF_PatientPreAdmission` — encounter/episode context

```
dbo.SF_PatientPreAdmission
────────────────────────────────────────────────────────
Id   | PatientID | DataFormId | IsDeleted | CreatedOn
────────────────────────────────────────────────────────
9001 | 5001      | 1          | 0         | 2025-01-10
9002 | 5002      | 2          | 0         | 2025-02-15
9003 | 5003      | 3          | 0         | 2024-11-01
```

### 3f. `dbo.SF_DataForms` — data form definitions

```
dbo.SF_DataForms
──────────────────────────────
Id | IsDeleted
──────────────────────────────
1  | 0
2  | 0
3  | 0
```

### 3g. `dbo.tblTP17REVIEW` — Treatment Plan Review (its OWN table, not Form/Answer)

```
dbo.tblTP17REVIEW
──────────────────────────────────────────────────────────────────────────
tpRID | tprTPID | tprCLTID | tprType         | DateAdded   | tprDRSIGDate
──────────────────────────────────────────────────────────────────────────
 801  | 1       | 5001     | Treatment Plan  | 2026-04-12  | 2026-04-13
 802  | 2       | 5002     | Treatment Plan  | 2026-04-18  | NULL
```

---

## PART 4 — Step-by-step execution for site B01

### Step 1 — Check: does dbo.Form exist at this site?

```sql
SELECT name FROM sys.tables WHERE name = 'Form'
-- Result: 1 row returned → YES, Forms are deployed at B01 → continue
```

### Step 2 — Compute the date window

```
DaysBack     = -15  (global default)
formDaysBack = -15 - 15 = -30
wrkdt        = 2026-04-22 + (-30 days) = 2026-03-23
```

**Only forms created or updated on or after 2026-03-23 are pulled.**

---

### Step 3 — Build the main Form/Q/A query

```sql
-- PART A: Forms WITH answers WHERE question is mapped
SELECT SiteCode = 'B01',
       ft.FormName,
       CONVERT(VARCHAR(100), f.Id) AS FormId,
       f.PreAdmissionId,
       f.ClientId,
       QuestionId       = ISNULL(q.Id, 0),
       QuestionOrderId  = q.QuestionOrderId,
       q.QuestionText,
       a.OptionId,
       AnswerValue      = a.Value,
       f.CreatedBy,
       f.CreatedOn,
       f.UpdatedBy,
       f.UpdatedOn,
       IsDeleted        = CASE WHEN ISNULL(f.IsDeleted,0)=0 AND pa.IsDeleted<>1
                               AND ISNULL(pa.DataFormId,0)>=0
                               AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END
FROM dbo.Form f
LEFT JOIN FormTemplate         ft ON f.FormTemplateId = ft.Id
LEFT JOIN Question             q  ON ft.Id = q.FormTemplateId
LEFT JOIN Answer               a  ON f.Id = a.FormId AND q.Id = a.QuestionId
INNER JOIN SF_PatientPreAdmission pa ON f.PreAdmissionId = pa.ID
LEFT JOIN  dbo.SF_DataForms    d  ON pa.DataFormId = d.Id
WHERE a.Value IS NOT NULL
  AND (f.CreatedOn >= '2026-03-23' OR ISNULL(f.UpdatedOn, f.CreatedOn) >= '2026-03-23')

UNION

-- PART B: Forms WHERE question is NOT mapped (QuestionId = NULL — header-only rows)
SELECT SiteCode = 'B01', ft.FormName, CONVERT(VARCHAR(100), f.Id), ...
FROM dbo.Form f ... (same joins)
WHERE q.Id IS NULL
  AND (f.CreatedOn >= '2026-03-23' OR ...)
```

**Result of main query (before TblForms2Process UNIONs):**

```
Main Form/Q/A result
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
SiteCode | FormName       | FormId | PreAdmissionId | ClientId | QuestionId | QuestionText              | AnswerValue          | CreatedOn   | IsDeleted
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
B01      | Counseling Note | 101   | 9001           | 5001     | 201        | How are you feeling today?| I feel much better   | 2026-04-01  | 0
B01      | Counseling Note | 101   | 9001           | 5001     | 202        | Any substance use?        | No substance use     | 2026-04-01  | 0
B01      | Counseling Note | 102   | 9002           | 5002     | 201        | How are you feeling today?| Struggling a bit     | 2026-04-05  | 0
B01      | Counseling Note | 102   | 9002           | 5002     | 202        | Any substance use?        | One relapse          | 2026-04-05  | 0
B01      | Counseling Note | 104   | 9001           | 5001     | 203        | How are you feeling today?| NULL                 | 2026-04-15  | 1  ← IsDeleted=1 (form deleted in SAMMS)

NOTE: Form 103 (Intake Assessment, CreatedOn=2026-03-10) is EXCLUDED — outside the 30-day window
```

---

### Step 4 — Loop through `ctrl.tbl_Forms2Process` and append UNIONs

```
BHGTaskRunner loads ctrl.tbl_Forms2Process:
  Row 1: FormName=Treatment Plan, TableName=tblTP17REVIEW, Prefix=8
  Row 2: FormName=Level Justification, TableName=tblORDERREQ, Prefix=9
  Row 3: FormName=Counseling Note, TableName=NULL → skip (standard form, already in main query)
  Row 4: FormName=Intake Assessment, TableName=NULL → skip
```

**For Row 1 — does `tblTP17REVIEW` exist at B01?**

```sql
SELECT name FROM sys.tables WHERE name = 'tblTP17REVIEW'
-- Result: 1 row → YES it exists → UNION it in
```

**Append UNION for tblTP17REVIEW:**

```sql
UNION
SELECT SiteCode         = 'B01',
       FormName         = 'TP-' + tprType,                          -- 'TP-Treatment Plan'
       FormId           = '8-1-' + ABS(tprCLTID) + '-' + tpRID + '-' + tprTPID,
       PreAdmissionId   = NULL,
       ClientId         = tprCLTID,
       QuestionId       = 0,
       QuestionOrderId  = 1,
       QuestionText     = NULL,
       OptionId         = NULL,
       AnswerValue      = NULL,
       CreatedBy        = NULL,
       CreatedOn        = DateAdded,
       UpdatedBy        = NULL,
       UpdatedOn        = NULL,
       IsDeleted        = 0
FROM dbo.tblTP17REVIEW
WHERE DateAdded >= '2026-03-23'
```

**For Row 2 — does `tblORDERREQ` exist at B01?**

```sql
SELECT name FROM sys.tables WHERE name = 'tblORDERREQ'
-- Result: 0 rows → NO, this clinic doesn't have it → SKIP silently
```

---

### Step 5 — Execute the full combined SELECT

```sql
SELECT DISTINCT * FROM (
    <main Form/Q/A query>
    UNION
    <tblTP17REVIEW query>
) z
```

**Final combined result sent to Azure:**

```
Final SrcDt (DataTable passed to loader)
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
SiteCode | FormName          | FormId        | PreAdmId | ClientId | QuestionId | QuestionText               | AnswerValue        | CreatedOn   | IsDeleted
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
B01      | Counseling Note   | 101           | 9001     | 5001     | 201        | How are you feeling today? | I feel much better | 2026-04-01  | 0
B01      | Counseling Note   | 101           | 9001     | 5001     | 202        | Any substance use?         | No substance use   | 2026-04-01  | 0
B01      | Counseling Note   | 102           | 9002     | 5002     | 201        | How are you feeling today? | Struggling a bit   | 2026-04-05  | 0
B01      | Counseling Note   | 102           | 9002     | 5002     | 202        | Any substance use?         | One relapse        | 2026-04-05  | 0
B01      | Counseling Note   | 104           | 9001     | 5001     | 203        | How are you feeling today? | NULL               | 2026-04-15  | 1   ← soft-deleted
B01      | TP-Treatment Plan | 8-1-5001-801-1| NULL     | 5001     | 0          | NULL                       | NULL               | 2026-04-12  | 0   ← from tblTP17REVIEW
B01      | TP-Treatment Plan | 8-1-5002-802-2| NULL     | 5002     | 0          | NULL                       | NULL               | 2026-04-18  | 0   ← from tblTP17REVIEW
```

---

### Step 6 — Route to Bulk or EF Core

**B01 is NOT in the 18 high-volume sites list → EF Core path.**

```
EF Core: sd.SaveFormQuestionAnswers(SrcDt, "B01", wrkdt=2026-03-23, xForms, null)
```

#### Step 6a — Pre-pass: load all existing Azure rows for B01 into memory

```
Existing Azure rows for B01 in pats.tbl_dbo_FormQuestionAnswers (before today's run):
─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
SiteCode | FormName        | FormId | ClientId | QuestionId | AnswerValue        | CreatedOn   | RowState
─────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
B01      | Counseling Note | 101    | 5001     | 201        | I feel okay        | 2026-04-01  | 1  ← old value
B01      | Counseling Note | 101    | 5001     | 202        | No substance use   | 2026-04-01  | 1
B01      | Counseling Note | 102    | 5002     | 201        | Struggling a bit   | 2026-04-05  | 1
B01      | Counseling Note | 99     | 5001     | 201        | I was doing well   | 2026-03-01  | 1  ← old form from March
```

#### Step 6b — Pre-pass: soft-reset RowState = 0 for rows within the date window

For each Azure row, check `ctrl.tbl_Forms2Process` to decide:

```
Row FormId=101, FormName='Counseling Note':
  → In tbl_Forms2Process? YES (Row 3 — DateFilterEnabled=true)
  → CreatedOn=2026-04-01 >= wrkdt=2026-03-23? YES
  → SET RowState = 0  ← temporarily deactivated, will be re-activated if SAMMS still has it

Row FormId=102, FormName='Counseling Note':
  → DateFilterEnabled=true, CreatedOn=2026-04-05 >= 2026-03-23 → SET RowState = 0

Row FormId=99, FormName='Counseling Note':
  → DateFilterEnabled=true, CreatedOn=2026-03-01 < wrkdt=2026-03-23 → SKIP (outside window)
  → RowState stays 1 ← old record left untouched

After pre-pass, Azure table looks like:
──────────────────────────────────────────────────────────────────────────────────────────────────────
B01 | Counseling Note | 101 | 5001 | 201 | I feel okay       | 2026-04-01 | RowState=0  ← reset
B01 | Counseling Note | 101 | 5001 | 202 | No substance use  | 2026-04-01 | RowState=0  ← reset
B01 | Counseling Note | 102 | 5002 | 201 | Struggling a bit  | 2026-04-05 | RowState=0  ← reset
B01 | Counseling Note |  99 | 5001 | 201 | I was doing well  | 2026-03-01 | RowState=1  ← untouched
```

#### Step 6c — Main upsert loop (for each row in SrcDt)

```
SrcDt Row 1: SiteCode=B01, FormId=101, ClientId=5001, QuestionId=201, AnswerValue='I feel much better'
  → Lookup in Azure: FormId=101 + QuestionId=201 exists? YES
  → AnswerValue changed: 'I feel okay' → 'I feel much better'  ← UPDATED
  → SET AnswerValue = 'I feel much better', RowState = 1  ← re-activated

SrcDt Row 2: SiteCode=B01, FormId=101, ClientId=5001, QuestionId=202, AnswerValue='No substance use'
  → Lookup in Azure: exists? YES
  → No change → SET RowState = 1 only (re-activated, data unchanged)

SrcDt Row 3: SiteCode=B01, FormId=102, ClientId=5002, QuestionId=201, AnswerValue='Struggling a bit'
  → Lookup: exists? YES → No change → RowState = 1

SrcDt Row 4: SiteCode=B01, FormId=102, ClientId=5002, QuestionId=202, AnswerValue='One relapse'
  → Lookup: exists? YES → No change → RowState = 1

SrcDt Row 5: SiteCode=B01, FormId=104, ClientId=5001, QuestionId=203, IsDeleted=1
  → Lookup: exists? NO → INSERT new row with RowState=0 (IsDeleted=1 → inactive)

SrcDt Row 6: SiteCode=B01, FormId=8-1-5001-801-1 (Treatment Plan from tblTP17REVIEW)
  → Lookup: exists? NO → INSERT new row, RowState=1

SrcDt Row 7: SiteCode=B01, FormId=8-1-5002-802-2 (Treatment Plan)
  → Lookup: exists? NO → INSERT new row, RowState=1
```

#### Step 6d — SaveChanges() — writes all updates + inserts to Azure in one batch

---

### Step 7 — Final state of `pats.tbl_dbo_FormQuestionAnswers` for B01

```
pats.tbl_dbo_FormQuestionAnswers  (Azure BHG_DR — after today's run)
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
SiteCode | FormName          | FormId          | ClientId | QuestionId | AnswerValue         | CreatedOn   | RowState | Notes
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
B01      | Counseling Note   | 101             | 5001     | 201        | I feel much better  | 2026-04-01  | 1        ← UPDATED (was 'I feel okay')
B01      | Counseling Note   | 101             | 5001     | 202        | No substance use    | 2026-04-01  | 1        ← RE-ACTIVATED (unchanged)
B01      | Counseling Note   | 102             | 5002     | 201        | Struggling a bit    | 2026-04-05  | 1        ← RE-ACTIVATED
B01      | Counseling Note   | 102             | 5002     | 202        | One relapse         | 2026-04-05  | 1        ← RE-ACTIVATED
B01      | Counseling Note   |  99             | 5001     | 201        | I was doing well    | 2026-03-01  | 1        ← UNTOUCHED (outside window)
B01      | Counseling Note   | 104             | 5001     | 203        | NULL                | 2026-04-15  | 0        ← NEW INSERT (IsDeleted=1 → RowState=0)
B01      | TP-Treatment Plan | 8-1-5001-801-1  | 5001     | 0          | NULL                | 2026-04-12  | 1        ← NEW INSERT (from tblTP17REVIEW)
B01      | TP-Treatment Plan | 8-1-5002-802-2  | 5002     | 0          | NULL                | 2026-04-18  | 1        ← NEW INSERT (from tblTP17REVIEW)
```

---

### Step 8 — Run `pats.BAMMerge` for B01

```sql
EXEC pats.BAMMerge @sitecode = 'B01'
-- Reads pats.tbl_dbo_FormQuestionAnswers for B01 where FormName = 'Brief Addiction Monitor'
-- Calculates BAM scores and writes to pats.tbl_BAMForm + pats.tbl_BAMScore
-- This runs after EVERY site's FormQuestionAnswers load
```

---

### Step 9 — Move to AnswerSignatures task (Task 5002 — same site B01)

**Source: `dbo.AnswerSignature` in B01's SAMMS**

```
dbo.AnswerSignature
──────────────────────────────────────────────────────────────────────────────────────────────
Id  | FormId | DateField                             | DateTime    | Sign
──────────────────────────────────────────────────────────────────────────────────────────────
401 | 101    | CounselorSignatureSignatureDate        | 2026-04-01  | John Smith
402 | 101    | PatientSignatureDate                   | 2026-04-01  | Jane Doe
403 | 102    | CounselorSignatureSignatureDate        | 2026-04-06  | John Smith
404 | 102    | DoctorSignatureSignatureDate           | 2026-04-07  | Dr. Brown
```

**Built pivot query → one row per FormId, each signature type as a column:**

```sql
SELECT DISTINCT
    SiteCode       = 'B01',
    ft.FormName,
    CONVERT(VARCHAR(100), f.Id) AS FormId,
    f.ClientId,
    f.CreatedOn, f.UpdatedOn, IsDeleted = ...,
    CounselorSignatureSignatureDate = (
        SELECT TOP 1 [DateTime] FROM AnswerSignature
        WHERE FormId = x.FormId AND DateField = 'CounselorSignatureSignatureDate'
        ORDER BY [DateTime] DESC
    ),
    PatientSignatureDate = (SELECT TOP 1 [DateTime] FROM AnswerSignature WHERE ...),
    DoctorSignatureSignatureDate = (SELECT TOP 1 [DateTime] FROM AnswerSignature WHERE ...),
    MedicalProviderSignatureSignatureDate = ...,
    CompletedBySignatureSignatureDate = ...,
    ProviderSignatureSignatureDate = ...,
    SupervisorSignatureSignatureDate = ...,
    StaffSignatureDate = ...,
    RequestorSignatureDate = ...
FROM (... same Form join ...) x
WHERE f.CreatedOn >= '2026-03-23' OR ...
```

**Result:**

```
Signature SrcDt for B01
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
SiteCode | FormName       | FormId | ClientId | CounselorSigDate | PatientSigDate | DoctorSigDate | CreatedOn   | IsDeleted
──────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────────
B01      | Counseling Note | 101   | 5001     | 2026-04-01       | 2026-04-01     | NULL          | 2026-04-01  | 0
B01      | Counseling Note | 102   | 5002     | 2026-04-06       | NULL           | 2026-04-07    | 2026-04-05  | 0
```

**Written to `pats.tbl_dbo_FormAnswerSignatures` via EF Core SaveAnswerSignatures()**

---

### Step 10 — Move to next site (B24, then B25… all 115 sites sequentially)

After B01 is fully complete (FormQA + Signatures), BHGTaskRunner moves to B24, then B25... one by one.

---

## PART 5 — The other 3 destination tables (inline, same Samms-Forms run)

These run as **separate child task rows** under the same `Samms-Forms` parent (TaskId=5000):

### `pats.tbl_eandmformmdm` — Evaluation & Management forms

```
Source: dbo.EandMForm + dbo.EandMFormMDM (B01's SAMMS)

dbo.EandMForm
───────────────────────────────────────────────────────────────────────
Id   | PreAdmissionId | ClientId | CreatedBy | CreatedOn   | IsDeleted
───────────────────────────────────────────────────────────────────────
1001 | 9001           | 5001     | Dr. Brown | 2026-04-10  | 0

dbo.EandMFormMDM
──────────────────────────────────────────────────────────────────────────────────────────
EandMFormID | MedicalDecisionMakingLevel | MedicalProviderSignatureDate | MedicalProviderSignatureBy
──────────────────────────────────────────────────────────────────────────────────────────
1001        | Moderate                   | 2026-04-10                  | Dr. Brown

→ Written to pats.tbl_EandMFormMDM
   SiteCode=B01, Id=1001, ClientId=5001, MedicalDecisionMakingLevel='Moderate'
   MedicalProviderSignatureDate=2026-04-10
```

### `pats.tbl_eandmformpregnancy` — Pregnancy-related E&M forms

```
Source: dbo.EandMForm + dbo.EandMFormPregnancy

→ Only runs if a patient has a pregnancy record linked to an E&M form
→ If no rows: SrcDt is empty, nothing written
```

### `pats.tbl_comprehensiveassessmentform` — Comprehensive intake assessment

```
Source: dbo.ComprehensiveAssessmentForm (B01's SAMMS)
→ Uses standard SelectConstructor path (column map from dms.tbl_MapSrc2Dsn)
→ WHERE clause based on DaysBack (-15 days, not -30)
→ Written to pats.tbl_ComprehensiveAssessmentForm via EF Core SaveComprehensiveAssessmentForm()
```

---

## PART 6 — Full summary diagram

```
BHGTaskRunner.exe 6 (Samms-Forms)
│
├── Parent task: Samms-Forms (Status=17→18→19)
│
├── SITE B01 (sequential):
│   │
│   ├── Task: pats.tbl_dbo_FormQuestionAnswers
│   │   ├── 1. Check dbo.Form exists at B01 → YES
│   │   ├── 2. Compute wrkdt = today - 30 days
│   │   ├── 3. Build main Form/Q/A query (Form+FormTemplate+Question+Answer JOINs)
│   │   ├── 4. Loop ctrl.tbl_Forms2Process:
│   │   │       tblTP17REVIEW exists at B01? YES → UNION it in
│   │   │       tblORDERREQ exists at B01?   NO  → skip
│   │   ├── 5. Execute: SELECT DISTINCT * FROM (<all unions>) z
│   │   ├── 6. B01 is EF path:
│   │   │       Pre-pass: load all Azure rows for B01 into memory
│   │   │                 reset RowState=0 for rows in window
│   │   │       Upsert:   for each SAMMS row → UPDATE or INSERT
│   │   │       Commit:   SaveChanges()
│   │   └── 7. Run pats.BAMMerge @sitecode='B01'
│   │
│   ├── Task: pats.tbl_dbo_FormAnswerSignatures
│   │   ├── 1. Check dbo.answersignature exists → YES
│   │   ├── 2. Build pivot query (one row per FormId, 9 signature date columns)
│   │   ├── 3. Same TblForms2Process UNIONs appended
│   │   └── 4. EF Core SaveAnswerSignatures()
│   │
│   ├── Task: pats.tbl_eandmformmdm    (separate child task row)
│   ├── Task: pats.tbl_eandmformpregnancy  (separate child task row)
│   └── Task: pats.tbl_comprehensiveassessmentform (separate child task row)
│
├── SITE B24 (sequential — starts after B01 fully done)
├── SITE B25 ...
└── ... (115 sites total)

Destination tables written by Samms-Forms pipeline:
──────────────────────────────────────────────────────────────────────────
pats.tbl_dbo_FormQuestionAnswers    ← 20.7M rows (all sites)
pats.tbl_dbo_FormAnswerSignatures   ← 7.4M rows
pats.tbl_EandMFormMDM               ← 473K rows
pats.tbl_EandMFormPregnancy         ← 55K rows
pats.tbl_ComprehensiveAssessmentForm← 44K rows
```

---

## PART 7 — Why it takes 11+ hours (from the simulation)


| Step                                          | Cost for B01               | × 115 sites                                 |
| --------------------------------------------- | -------------------------- | ------------------------------------------- |
| Existence checks per form type                | ~2 DB round trips per site | ~230 round trips                            |
| Build + execute UNION SELECT                  | 1 large query to SAMMS     | 115 queries                                 |
| Load all Azure rows into EF memory (pre-pass) | Thousands of rows per site | Millions of rows total                      |
| Reset RowState per row in memory              | Per row loop               | ~79,200 rows for FormAnswerSignatures alone |
| Upsert each SAMMS row                         | Per row loop               | 1.5M individual EF operations               |
| pats.BAMMerge                                 | 1 stored proc per site     | 115 stored proc calls                       |
| All sequential — site 2 waits for site 1      | No parallelism             | Full 115-site chain                         |


**In Fabric: run all 115 sites in parallel → estimated 20–30 min instead of 11 hours.**

What pats.BAMMerge is
BAM = Behavioral Analysis Metrics (or Behavioral Analysis Monitor — the abbreviation used throughout the codebase).

After the Forms pipeline loads raw question/answer rows into pats.tbl_dbo_FormQuestionAnswers, that table contains raw per-question answer strings for every form submitted by every patient. The BAM scoring logic (risk score, protective factor score, recovery score) cannot be stored in that raw row format — it needs to be calculated and aggregated from those individual Q&A rows.

That is what pats.BAMMerge @sitecode = 'B01' does:

pats.tbl_dbo_FormQuestionAnswers  (raw Q&A rows — just loaded)
            │
            ▼
  pats.BAMMerge @sitecode='B01'  ← Stored Procedure
            │
            ▼
  pats.tbl_BamForm / pats.tbl_BamScore  (calculated BAM scores per patient per visit)
Two different BAMMerge procedures — do not confuse them
Procedure	Called when	Scope	What it produces
pats.BAMMerge	After every site's FormQuestionAnswers load	Single site (@sitecode = 'B01')	Per-site BAM scores from the Q&A rows just loaded
pats.BAMMergeGbl	After SaveBAM in the SAMMSGlobal pipeline (Schedule 1)	@sitecode = 'Global'	Cross-clinic global BAM aggregation
These are two separate stored procedures with two separate purposes.

Why it runs per-site (not at the end of everything)
Because the Forms loop is sequential:

Site B01 → Load QA → BAMMerge('B01')
Site B02 → Load QA → BAMMerge('B02')
Site B03 → Load QA → BAMMerge('B03')
...
Site B42 → Load QA → BAMMerge('B42')
The SP runs immediately after each site's QA load, so BAM scores are always in sync with whatever just got written. If it ran at the end for all sites at once, a slow site in the middle could fail and leave BAM scores stale for all sites after it.

Impact on pipeline runtime
Since pats.BAMMerge is:

Called 115 times (once per site)
Runs synchronously (next site cannot start until it finishes)
Likely does JOIN + aggregation + MERGE across pats.tbl_dbo_FormQuestionAnswers for that site
...it is one of the main contributors to the 11+ hour Forms pipeline runtime, alongside the sequential site loop and the 30-day lookback window.

SP For BAM

/****** Object:  StoredProcedure [pats].[BAMMerge]    Script Date: 4/27/2026 1:47:47 PM ******/SET ANSI_NULLS ONGOSET QUOTED_IDENTIFIER ONGOALTER procedure [pats].[BAMMerge] (  @sitecode varchar(25)) asDECLARE @SummaryOfChanges TABLE(Change VARCHAR(20));delete from pats.tbl_vw_BAM where SiteCode = @sitecode;Merge into pats.tbl_vw_BAM as tUsing (SELECT [Sitecode], [fcltid], [Date], Idx = Row_Number() over(Partition by SiteCode, fcltid, [Date] order by SiteCode, fcltid, [Date], ClinicianTEXT, AdminList, PreAdmissionId, fid), [ClinicianTEXT], [AdminList], [PreAdmissionId], [fid]

```
  , [Q1], [Q2], [Q3], [Q4], [Q5], [Q6], [Q7a], [Q7b], [Q7c], [Q7d], [Q7e], [Q7f], [Q7g], [Q8]

  , [Q9], [Q10], [Q11], [Q12], [Q13], [Q14], [Q15], [Q16], [Q17], [UseCalc], [RiskCalc]

  , [ProtectiveCalc]
```

  FROM [pats].[vw_BAM] 

  where SiteCode = @sitecode 

  ) as s

```
on (t.SiteCode = s.SiteCode and t.cltid = s.fcltid and t.[Date] = s.[Date] and t.Idx = s.Idx)When Matched Thenupdate set t.[ClinicianTEXT] = s.ClinicianTEXT

  , t.[AdminList] = s.[AdminList]

  , t.[PreAdmissionId] = s.[PreAdmissionId], t.fid = s.fid

  , t.[Q1] = s.[Q1]

  , t.[Q2] = s.[Q2]

  , t.[Q3] = s.[Q3]

  , t.[Q4] = s.[Q4]

  , t.[Q5] = s.[Q5]

  , t.[Q6] = s.[Q6]

  , t.[Q7a] = s.[Q7a]

  , t.[Q7b] = s.[Q7b]

  , t.[Q7c] = s.[Q7c]

  , t.[Q7d] = s.[Q7d]

  , t.[Q7e] = s.[Q7e]

  , t.[Q7f] = s.[Q7f]

  , t.[Q7g] = s.[Q7g]

  , t.[Q8] = s.[Q8]

  , t.[Q9] = s.[Q9]

  , t.[Q10] = s.[Q10]

  , t.[Q11] = s.[Q11]

  , t.[Q12] = s.[Q12]

  , t.[Q13] = s.[Q13]

  , t.[Q14] = s.[Q14]

  , t.[Q15] = s.[Q15]

  , t.[Q16] = s.[Q16]

  , t.[Q17] = s.[Q17]

  , t.[UseCalc] = s.[UseCalc]

  , t.[RiskCalc] = s.[RiskCalc]

  , t.[ProtectiveCalc] = s.[ProtectiveCalc]
```

when not Matched by TARGET and s.[fcltID] > 0 thenInsert ([Sitecode], [cltid], [Date], Idx, [ClinicianTEXT], [AdminList], [PreAdmissionId], [fid]

```
      , [Q1], [Q2], [Q3], [Q4], [Q5], [Q6], [Q7a], [Q7b], [Q7c], [Q7d], [Q7e], [Q7f], [Q7g]

      , [Q8], [Q9], [Q10], [Q11], [Q12], [Q13], [Q14], [Q15], [Q16], [Q17], [UseCalc]

      , [RiskCalc], [ProtectiveCalc])values (s.[Sitecode], s.[fcltid], s.[Date], s.Idx, s.[ClinicianTEXT], s.[AdminList], s.[PreAdmissionId], s.[fid]

        , s.[Q1], s.[Q2], s.[Q3], s.[Q4], s.[Q5], s.[Q6], s.[Q7a], s.[Q7b], s.[Q7c], s.[Q7d], s.[Q7e], s.[Q7f]
```

      , s.[Q7g], s.[Q8], s.[Q9], s.[Q10], s.[Q11], s.[Q12], s.[Q13], s.[Q14], s.[Q15], s.[Q16], s.[Q17]

```
        , s.[UseCalc], s.[RiskCalc], s.[ProtectiveCalc])--When not Matched by SOURCE and t.SiteCode = s.SiteCode then --  update set t.RowState = 0output $action into @SummaryOfChanges;--if(@sitecode = 'V9')--begin--delete FROM [pats].[tbl_vw_BAM] where siteCode = 'V9' and cltid = 43147 and [date] = '2023-1-2';--endselect */*  RowsUpd = count(case when Change = 'UPDATE' then 1 else 0 end)     , RowsIns = count(case when Change = 'INSERT' then 1 else 0 end) */from @SummaryOfChanges;
```

