  WITH _AS_agg AS (
    SELECT FormId,
      MAX(CASE WHEN DateField = 'CompletedBySignatureSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS CompletedBySignatureSignatureDate,
      MAX(CASE WHEN DateField IN ('CounselorSignatureSignatureDate','CounselorSignatureDate')
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS CounselorSignatureSignatureDate,
      MAX(CASE WHEN DateField = 'DoctorSignatureSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS DoctorSignatureSignatureDate,
      MAX(CASE WHEN DateField = 'MedicalProviderSignatureSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS MedicalProviderSignatureSignatureDate,
      MAX(CASE WHEN DateField = 'PatientSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS PatientSignatureDate,
      MAX(CASE WHEN DateField = 'ProviderSignatureSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS ProviderSignatureSignatureDate,
      MAX(CASE WHEN DateField = 'RequestorSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS RequestorSignatureDate,
      MAX(CASE WHEN DateField = 'StaffSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS StaffSignatureDate,
      MAX(CASE WHEN DateField = 'SupervisorSignatureSignatureDate'
          THEN CASE WHEN Sign IS NULL THEN '1900-01-01'
              ELSE CONVERT(varchar(20),[DateTime],23) END END) AS SupervisorSignatureSignatureDate
    FROM [SAMMS-GadsdenV5].dbo.[AnswerSignature] WITH (NOLOCK)
    WHERE DateField IN (
      'CompletedBySignatureSignatureDate','CounselorSignatureSignatureDate',
      'CounselorSignatureDate','DoctorSignatureSignatureDate',
      'MedicalProviderSignatureSignatureDate','PatientSignatureDate',
      'ProviderSignatureSignatureDate','RequestorSignatureDate',
      'StaffSignatureDate','SupervisorSignatureSignatureDate'
    )
    GROUP BY FormId
  ),
  src AS (
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    x.SiteCode, x.FormName,
    CONVERT(varchar(100), x.FormId) AS FormId,
    x.ClientId, x.CreatedOn, x.UpdatedOn, x.IsDeleted,
    ag.CompletedBySignatureSignatureDate AS CompletedBySignatureSignatureDate,
    ag.CounselorSignatureSignatureDate AS CounselorSignatureSignatureDate,
    ag.DoctorSignatureSignatureDate AS DoctorSignatureSignatureDate,
    ag.MedicalProviderSignatureSignatureDate AS MedicalProviderSignatureSignatureDate,
    ag.PatientSignatureDate AS PatientSignatureDate,
    ag.ProviderSignatureSignatureDate AS ProviderSignatureSignatureDate,
    ag.RequestorSignatureDate AS RequestorSignatureDate,
    ag.StaffSignatureDate AS StaffSignatureDate,
    ag.SupervisorSignatureSignatureDate AS SupervisorSignatureSignatureDate
  FROM (
    SELECT SiteCode='B54', ft.FormName, f.id AS FormId, f.ClientId,
          f.CreatedOn, f.UpdatedOn,
          IsDeleted = CASE WHEN ISNULL(f.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END
    FROM [SAMMS-GadsdenV5].dbo.[Form] f WITH (NOLOCK)
      LEFT JOIN [SAMMS-GadsdenV5].dbo.[FormTemplate] ft WITH (NOLOCK) ON f.FormTemplateId = ft.Id
      INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa WITH (NOLOCK) ON f.PreAdmissionId = pa.ID
      LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d WITH (NOLOCK) ON pa.DataFormId = d.Id

    UNION

    SELECT SiteCode='B54', ft.FormName, f.id AS FormId, f.ClientId,
          f.CreatedOn, f.UpdatedOn,
          IsDeleted = CASE WHEN ISNULL(f.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END
    FROM [SAMMS-GadsdenV5].dbo.[Form] f WITH (NOLOCK)
      LEFT JOIN [SAMMS-GadsdenV5].dbo.[FormTemplate] ft WITH (NOLOCK) ON f.FormTemplateId = ft.Id
      INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa WITH (NOLOCK) ON f.PreAdmissionId = pa.ID
      LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d WITH (NOLOCK) ON pa.DataFormId = d.Id
    WHERE f.Isdeleted = 1
  ) x
  LEFT JOIN _AS_agg ag ON ag.FormId = x.FormId

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Periodic Reassessment' AS [FormName],
    '99-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, ISNULL(b.PreAdmissionId, 0)) + '-' + CONVERT(varchar, a.Id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn] = CONVERT(date, a.CreatedOn),
    [UpdatedOn] = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = CONVERT(date, b.CounselorSignatureDate),
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = CONVERT(date, b.PatientSignatureDate),
    ProviderSignatureSignatureDate = CONVERT(date, b.ProviderSignatureDate),
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = CONVERT(date, b.SupervisorSignatureDate)
  FROM [SAMMS-GadsdenV5].dbo.[NewPeriodicReassessment] a
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[NewPeriodicReassessmentCounselorReview] b
      ON a.Id = b.NewPeriodicReassessmentId
    AND a.PreAdmissionId = b.PreAdmissionId

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - BHOLD' AS [FormName],
    '24-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIBHOLD] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home Care Plan Review Form' AS [FormName],
    '25-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomeCareReview] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home Consent to Receive' AS [FormName],
    '26-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomeConsentToReceive] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home Eligibility and Follow up Checklist' AS [FormName],
    '27-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomeEligibilityFollUpChecklist] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home History' AS [FormName],
    '28-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomeHistory] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home Note' AS [FormName],
    '29-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomeNote] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home Triage Assessment' AS [FormName],
    '30-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomeTriageAssessment] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Health Home Patient Centered Plan of Care' AS [FormName],
    '31-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIHealthHomePatientCenteredPlan] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - Overdose Prevention Education' AS [FormName],
    '32-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIOverdosePreventionEducation] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'RI - PHQ-9 Form' AS [FormName],
    '33-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RIPHQ9] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'GA - Patient Rights and Responsibilities' AS [FormName],
    '34-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[PatientRightsandResponsibilities] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'GA - Order for Services' AS [FormName],
    '35-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[OrderforServices] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'GA - Consent to Treatment with an Approved Narcotic' AS [FormName],
    '36-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[GAConsenttoTreatmentwithanApprovedNarcotic] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'GA - Consent Central Registry Georgia' AS [FormName],
    '37-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[GAConsentCentralRegistryGeorgia] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'GA - Transition and Discharge Plan' AS [FormName],
    '38-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[TransitionandDischargePlan] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'NC - Consent to Central Registry' AS [FormName],
    '39-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NCConsenttoCentralRegistry] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'NC - Initial Transition and Discharge Plan' AS [FormName],
    '40-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NCInitialTransitionDischargePlan] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'NC - Crisis Prevention and Intervention Plan' AS [FormName],
    '41-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[CrisisPrevention] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'NC - Consent and Auth Disclosure of Sub Disorder' AS [FormName],
    '42-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NCConsentAuthDisclosureSubDisorder] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'NC - Person Centered Profile' AS [FormName],
    '43-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NCPersonCenteredProfile] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'NC - PIE' AS [FormName],
    '44-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NCPIE] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', '90 Day Review' AS [FormName],
    '45-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NinetyDayReview] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'AR-State Fact form' AS [FormName],
    '46-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.Createdon),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[StateFactForm] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.Createdon >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.Createdon) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MN - Consent to Treatment Via Telehealth' AS [FormName],
    '23-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsenttoTreatmentViaTelehealth] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MN - DAANES Notification Form' AS [FormName],
    '22-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[tblDAANESNotification] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MN - Authorization for Release of Information to the MAARC' AS [FormName],
    '21-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedDate),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[tblMAARC] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedDate, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Initial Services Plan and Vulnerable Adult Determination' AS [FormName],
    '20-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[InitialServicesPlanandVAD] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'State Fact Form' AS [FormName],
    '19-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[StateFactForm] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MN - Mental Health Informed Consent' AS [FormName],
    '18-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[MentalHealthInformedConsent] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Insurance Benefit Verification' AS [FormName],
    '17-' + CONVERT(varchar, ISNULL(pa.PatientID, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(pa.PatientID, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[InsuranceBenefitVerification] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Patient Information sheet' AS [FormName],
    '16-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[PatientInformationsheet] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Adverse Childhood Experiences' AS [FormName],
    '15-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedDate),
    [UpdatedOn]  = CONVERT(date, a.ModifiedDate),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[AdverseChildhood] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedDate >= '2026-01-01'
    OR ISNULL(a.ModifiedDate, a.CreatedDate) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MN - Consent to Central Registry' AS [FormName],
    '14-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[GeneralConsentAuthforReleaseInfo] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'CO - Consent Central Registry Colorado' AS [FormName],
    '13-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentCentralRegistryColorado] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'KS Patient Rights and Responsibilities' AS [FormName],
    '12-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[KSPatientRightsResponsibilities] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'BHG Notice Of Privacy Practices' AS [FormName],
    '10-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[tblBHGNoticeOfPrivacyPractices] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Suicide Severity Rating Scale' AS [FormName],
    '1-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[SuicideSeverityRatingScale] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Suicide Severity Rating Scale 2.0' AS [FormName],
    '11-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[SAFETProtocolwithCSSRS] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Health Questionnaire' AS [FormName],
    '2-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.Createddate),
    [UpdatedOn]  = CONVERT(date, a.Modifieddate),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[HealthQuestionnaire] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.Createddate >= '2026-01-01'
    OR ISNULL(a.Modifieddate, a.Createddate) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Infectious Disease And Behavioral Screen' AS [FormName],
    '3-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = CASE WHEN CONVERT(date, a.MedicalStaffSignatureDate) IS NULL THEN '1900-01-01' ELSE CONVERT(date, a.MedicalStaffSignatureDate) END,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[InfectiousDiseaseAndBehavioralScreen] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent to Treatment with an Approved Narcotic' AS [FormName],
    '4-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentToTreatmentWithAnApprovedNarcotic] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Financial Hardship Application' AS [FormName],
    '5-' + CONVERT(varchar, ISNULL(a.CltID, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.CltID, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[FinancialHardshipApplication] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Comprehensive Assessment Form' AS [FormName],
    '6-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ComprehensiveAssessmentForm] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Admission Assessment' AS [FormName],
    '7-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[AdmissionAssessment] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  INNER JOIN [SAMMS-GadsdenV5].dbo.[AdmissionAssessmentSummary] aas
      ON a.Id = aas.AdmissionAssessmentId
    AND a.PreAdmissionId = aas.PreAdmissionId
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode, FormName, FormID, ClientId, CreatedOn, UpdatedOn, IsDeleted,
    CompletedBySignatureSignatureDate,
    CounselorSignatureSignatureDate,
    DoctorSignatureSignatureDate,
    MedicalProviderSignatureSignatureDate,
    PatientSignatureDate,
    ProviderSignatureSignatureDate,
    RequestorSignatureDate,
    StaffSignatureDate,
    SupervisorSignatureSignatureDate
  FROM (
    SELECT SiteCode='B54', 'TP-' + tprType AS [FormName],
      '8-1-' + CONVERT(varchar, ABS(tprCLTID)) + '-' + CONVERT(varchar, tpRID) + '-' + CONVERT(varchar, tprTPID) AS [FormID],
      tprCLTID AS ClientId,
      CONVERT(date, tprDT) AS [CreatedOn],
      null AS [UpdatedOn],
      IsDeleted = CASE WHEN tprCLTID < 0 THEN 1 ELSE 0 END,
      CompletedBySignatureSignatureDate     = null,
      CounselorSignatureSignatureDate       = null,
      DoctorSignatureSignatureDate          = null,
      MedicalProviderSignatureSignatureDate = null,
      PatientSignatureDate = CASE
          WHEN CONVERT(date, tprCLIRNTSIGDate) IS NULL THEN '1900-01-01'
          ELSE CONVERT(date, tprCLIRNTSIGDate) END,
      ProviderSignatureSignatureDate = CASE
          WHEN CONVERT(date, tprDRSIGDate) IS NULL THEN '1900-01-01'
          ELSE CONVERT(date, tprDRSIGDate) END,
      RequestorSignatureDate = null,
      StaffSignatureDate = CASE
          WHEN CONVERT(date, tprCOUNSSIGDate) IS NULL
          AND CONVERT(date, tprSUPERSIGDate) IS NULL THEN '1900-01-01'
          ELSE CONVERT(date, tprCOUNSSIGDate) END,
      SupervisorSignatureSignatureDate = CONVERT(date, tprSUPERSIGDate)
    FROM [SAMMS-GadsdenV5].dbo.[tblTP17REVIEW]
  ) tp
  WHERE (CreatedOn >= '2026-01-01'
      OR ISNULL(UpdatedOn, CreatedOn) >= '2026-01-01'
      OR ProviderSignatureSignatureDate >= '2026-01-01'
      OR CompletedBySignatureSignatureDate >= '2026-01-01'
      OR PatientSignatureDate >= '2026-01-01'
      OR StaffSignatureDate >= '2026-01-01'
      OR SupervisorSignatureSignatureDate >= '2026-01-01')

  UNION
  SELECT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Level Justification' AS [FormName],
    '9-1-' + CONVERT(varchar, ABS(cltID)) + '-' + CONVERT(varchar, ReqNum) + '-1' AS FormId,
    ClientId = cltID,
    [CreatedOn]  = CONVERT(date, DateAdded),
    [UpdatedOn]  = CONVERT(date, statusDate),
    IsDeleted    = CASE WHEN cltID < 0 THEN 1 ELSE 0 END,
    CompletedBySignatureSignatureDate      = null,
    CounselorSignatureSignatureDate        = null,
    DoctorSignatureSignatureDate           = null,
    MedicalProviderSignatureSignatureDate  = null,
    PatientSignatureDate                   = null,
    ProviderSignatureSignatureDate = CASE
        WHEN ISNULL(CONVERT(date, DrSigDt), CONVERT(date, SigNurseDt)) IS NULL
        AND status = 'Approved' THEN '1900-01-01'
        ELSE ISNULL(CONVERT(date, DrSigDt), CONVERT(date, SigNurseDt)) END,
    RequestorSignatureDate                 = null,
    StaffSignatureDate                     = null,
    SupervisorSignatureSignatureDate = CASE
        WHEN CONVERT(date, sigCoordinatorDt) IS NULL AND status = 'Approved'
        THEN '1900-01-01'
        ELSE CONVERT(date, sigCoordinatorDt) END
  FROM [SAMMS-GadsdenV5].dbo.[tblORDERREQ]
  WHERE status = 'Approved' AND Notes NOT LIKE 'Test %' AND Notes <> 'TEST' AND DrNote <> 'HEllo test' AND DrNote <> 'TEST'
    AND (DateAdded >= '2026-01-01' OR ISNULL(statusDate, DateAdded) >= '2026-01-01')

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent for Release of Confidential Info V0' AS [FormName],
    '48-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentforReleaseConInfoRevised] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Guest Dosing/Permanent Transfer' AS [FormName],
    '52-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[GuestDosingPermanentTransfer] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Pre Admission' AS [FormName],
    '58-' + CONVERT(varchar, ISNULL(pa.PatientID, 0)) + '-' + CONVERT(varchar, ISNULL(a.ParentPreAdmissionId, 0)) + '-' + CONVERT(varchar, ISNULL(a.id, 0)) AS [FormID],
    ClientId = ISNULL(pa.PatientID, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.LastUpdateOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.ID = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.LastUpdateOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Referral' AS [FormName],
    '60-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.createdon),
    [UpdatedOn]  = CONVERT(date, a.updatedon),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ReferralForm] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Understanding of Treatment' AS [FormName],
    '61-' + CONVERT(varchar, ISNULL(pa.PatientID, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(pa.PatientID, 0),
    [CreatedOn]  = CONVERT(date, a.Createddate),
    [UpdatedOn]  = CONVERT(date, a.UpdatedDate),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[SF_UnderstandingOfTreatment] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.Createddate >= '2026-01-01'
    OR ISNULL(a.UpdatedDate, a.Createddate) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MO - Consent Central Registry Missouri' AS [FormName],
    '62-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[MOConsentCentralRegistryMissouri] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'SC - Consent Central Registry' AS [FormName],
    '63-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[SCConsentReleaseCentralRegistry] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent to Disclose Assignment of Benefits Ver 1' AS [FormName],
    '64-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentToDiscloseAssignmentofBenefits] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent to Treatment for IOP Or EOP Or OP' AS [FormName],
    '65-' + CONVERT(varchar, ISNULL(d.PatientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(d.PatientId, 0),
    [CreatedOn]  = CONVERT(date, a.Createdon),
    [UpdatedOn]  = CONVERT(date, a.Modifiedon),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsenttoTreatmentforIOPOrEOPOrOP] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.Createdon >= '2026-01-01'
    OR ISNULL(a.Modifiedon, a.Createdon) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'GPRA' AS [FormName],
    '66-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.Createdon),
    [UpdatedOn]  = CONVERT(date, a.Modifiedon),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = CASE WHEN CONVERT(date, a.StaffSignatureDate) IS NULL THEN '1900-01-01' ELSE CONVERT(date, a.StaffSignatureDate) END,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[GPRA] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.Createdon >= '2026-01-01'
    OR ISNULL(a.Modifiedon, a.Createdon) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MAT and Driving' AS [FormName],
    '67-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[MATandDriving] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Patient Rights And Responsibilities V2' AS [FormName],
    '68-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[PatientRightsAndResponsibilitiesV2] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Request Release of Medical Records V2' AS [FormName],
    '69-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[RequestReleaseofMedicalRecordsV2] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Take Home Risk Assessment' AS [FormName],
    '70-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[TakeHomeRiskAssessment] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Adult Nutritional Screen' AS [FormName],
    '71-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[AdultNutritionalScreen] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent For Follow-Up Contact' AS [FormName],
    '72-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentForFollowUpContact] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent for Release of Confidential Information to Emergency Contact Ver 1' AS [FormName],
    '73-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentReleaseEmergencyContact] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'AL - Consent to Central Registry' AS [FormName],
    '74-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentCentralRegistryAlabama] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'LA - Consent to Central Registry' AS [FormName],
    '75-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentCentralRegistryLouisiana] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Overdose Education' AS [FormName],
    '76-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[OpioidOverdoseRisks] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Notice of Privacy Practice DC' AS [FormName],
    '77-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NoticeofPrivacyPracticesDC] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Admission Assessment V1' AS [FormName],
    '78-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[NewAdmissionAssessment] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  INNER JOIN [SAMMS-GadsdenV5].dbo.[NewAdmissionAssessmentASAMDimension6] b
      ON a.preadmissionID = b.preadmissionID
    AND a.ID = b.NewAdmissionAssessmentFormId
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent and Screen for Tuberculosis Skin Test' AS [FormName],
    '79-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsentandScreenFTST] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Consent to Release Information to the Health Department' AS [FormName],
    '80-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[ConsenttoReleaseInformationtotheHealthDepartmentRevised] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Take Home Agreement and Diversion Control Plan' AS [FormName],
    '81-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[TakeHomeAgreementandDiversionControl] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'Take Home Guidelines' AS [FormName],
    '82-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[TakeHomeGuidelinesForm] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'KY - Patient Rights and Responsibilities' AS [FormName],
    '83-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[KYPatientRightsandResp] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'PPD Test' AS [FormName],
    '84-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[PPDTest] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'

  UNION
  SELECT DISTINCT
    'SAMMS-GadsdenV5' AS SourceDatabase,
    '3f1dd88e-1554-4d12-b836-22ed6c3d7008' AS IngestRunId,
    GETDATE() AS ExtractedAt,
    '2026-01-01' AS LookbackDate,
    SiteCode='B54', 'MN - Comprehensive Assessment' AS [FormName],
    '85-' + CONVERT(varchar, ISNULL(a.ClientId, 0)) + '-' + CONVERT(varchar, a.PreAdmissionId) + '-' + CONVERT(varchar, a.id) AS [FormID],
    ClientId = ISNULL(a.ClientId, 0),
    [CreatedOn]  = CONVERT(date, a.CreatedOn),
    [UpdatedOn]  = CONVERT(date, a.ModifiedOn),
    IsDeleted = CASE WHEN ISNULL(a.IsDeleted,0)=0 AND pa.IsDeleted<>1 AND ISNULL(pa.DataFormId,0)>=0 AND ISNULL(d.IsDeleted,0)=0 THEN 0 ELSE 1 END,
    CompletedBySignatureSignatureDate = null,
    CounselorSignatureSignatureDate = null,
    DoctorSignatureSignatureDate = null,
    MedicalProviderSignatureSignatureDate = null,
    PatientSignatureDate = null,
    ProviderSignatureSignatureDate = null,
    RequestorSignatureDate = null,
    StaffSignatureDate = null,
    SupervisorSignatureSignatureDate = null
  FROM [SAMMS-GadsdenV5].dbo.[MNComprehensiveAssessment] a
  INNER JOIN [SAMMS-GadsdenV5].dbo.[SF_PatientPreAdmission] pa ON a.PreAdmissionId = pa.ID
  LEFT JOIN [SAMMS-GadsdenV5].dbo.[SF_DataForms] d ON pa.DataFormId = d.Id
  INNER JOIN [SAMMS-GadsdenV5].dbo.[MNComprehensiveAssessmentSocialHistory] b
      ON a.preadmissionID = b.preadmissionID
    AND a.Id = b.MNComprehensiveAssessmentFormId
  WHERE a.CreatedOn >= '2026-01-01'
    OR ISNULL(a.ModifiedOn, a.CreatedOn) >= '2026-01-01'
  )
  SELECT
      SiteCode,
      EOMONTH(CreatedOn) AS MonthEnd,
      COUNT(FormId) AS RowCnt
  FROM src
  WHERE CreatedOn >= '2026-01-01'
  GROUP BY SiteCode, EOMONTH(CreatedOn)
  ORDER BY SiteCode, EOMONTH(CreatedOn);
