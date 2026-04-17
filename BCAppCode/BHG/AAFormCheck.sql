select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Comprehensive Assessment Form' as [FormName]
, '6-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] 
     , c.ClientId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn]
     , isnull((select df.Isdeleted from SF_PatientPreAdmission pa left join SF_DataForms df on c.DataFormId = df.Id and c.ClientId = df.PatientId 
      where df.FormName in ('Comprehensive Assessment', 'Comprehensive Assessment Form') and c.DataFormId = abs(pa.DataFormId) and 
       c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID), c.IsDeleted) Isdeleted 
            , CompletedBySignatureSignatureDate = null
            , CounselorSignatureSignatureDate = null
            , DoctorSignatureSignatureDate = null 
            , MedicalProviderSignatureSignatureDate = null 
         , PatientSignatureDate = case when convert(date,c.CAPatientSignatureDate) is null then '1900-01-01' else convert(date,c.CAPatientSignatureDate) end 
            , ProviderSignatureSignatureDate = case when convert(date,c.CAProviderSignatureDate) is null then null else convert(date,c.CAProviderSignatureDate) end 
            , RequestorSignatureDate = null
            , StaffSignatureDate = case when convert(date, c.CAStaffSignatureDate) is null then '1900-01-01' else convert(date,c.CAStaffSignatureDate) end 
           , SupervisorSignatureSignatureDate = case when convert(date,c.CASupervisorSignatureDate) is null then null else convert(date,c.CASupervisorSignatureDate) end 
       from [dbo].[ComprehensiveAssessmentForm] c

select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Financial Hardship Application' as [FormName] 
        , '5-' + convert(varchar, c.CltId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] 
       , c.CltId, convert(date, c.CreatedOn) as [CreatedOn], convert(date, c.ModifiedOn) as [UpdatedOn], isnull((select df.Isdeleted 
   from SF_DataForms df left join SF_PatientPreAdmission pa  on df.Id = abs(pa.DataFormId) and df.Id = pa.ID and df.PatientID = pa.PatientId 
   where df.FormName = 'Financial Hardship Application' and c.DataFormId = df.Id and c.CltId = df.PatientId), c.Isdeleted) Isdeleted 
        , CompletedBySignatureSignatureDate = null
        , CounselorSignatureSignatureDate = null 
        , DoctorSignatureSignatureDate = null 
        , MedicalProviderSignatureSignatureDate = null 
        , PatientSignatureDate = case when convert(date,c.FHAPatientSignatureDate) is null then '1900-01-01' else convert(date,c.FHAPatientSignatureDate) end 
        , ProviderSignatureSignatureDate = null 
        , RequestorSignatureDate = null
        , StaffSignatureDate = null 
        , SupervisorSignatureSignatureDate = null 
   from [dbo].[FinancialHardshipApplication]  c

   select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID 
      , QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) 
      , QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (
       Select SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName] 
      , '7-' + convert(varchar, c.ClientId) + '-' + convert(varchar, c.PreAdmissionId) + '-' + convert(varchar, c.id) as [FormID] 
            , c.PreAdmissionId, c.ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue 
            , c.Createdby, convert(date, c.CreatedOn) as [CreatedOn], c.ModifiedBy as [UpdatedBy], convert(date, c.ModifiedOn) as [UpdatedOn] 
      , isnull((select df.Isdeleted from SF_PatientPreAdmission pa inner join SF_DataForms df on abs(pa.DataFormId) = df.Id and pa.PatientID = df.PatientId 
      where df.FormName = 'Admission Assessment' and c.DataFormId = abs(pa.DataFormId) and c.PreAdmissionId = pa.ID and c.ClientId = pa.PatientID), c.IsDeleted) IsDeleted 
        from [dbo].[AdmissionAssessment] c) v 



select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName]
      , '7-' + convert(varchar, aa.ClientId) + '-' + convert(varchar, aa.PreAdmissionId) + '-' + convert(varchar, aa.id) as [FormID] 
      , aa.ClientId, convert(date, aa.CreatedOn) as [CreatedOn], convert(date, aa.ModifiedOn) as [UpdatedOn]
      , isnull((select df.Isdeleted from SF_PatientPreAdmission pa left join SF_DataForms df on aa.DataFormId = df.Id and aa.ClientId = df.PatientId 
  where df.FormName = 'Admission Assessment' and aa.DataFormId = abs(pa.DataFormId) and aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID ), aa.IsDeleted) IsDeleted
      , CompletedBySignatureSignatureDate = null
      , CounselorSignatureSignatureDate = null
      , DoctorSignatureSignatureDate = null 
      , MedicalProviderSignatureSignatureDate = null 
      , PatientSignatureDate = case when convert(date,aas.AdmissionAssessmentPatientSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentPatientSignatureDate) end 
      , ProviderSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentProviderSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentProviderSignatureDate) end 
      , RequestorSignatureDate = null
      , StaffSignatureDate = case when convert(date,aas.AdmissionAssessmentStaffSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentStaffSignatureDate) end 
      , SupervisorSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) end 
   from [dbo].[AdmissionAssessment] aa inner join [dbo].[AdmissionAssessmentSummary] aas on aa.Id = aas.AdmissionAssessmentId and aa.PreAdmissionId = aas.PreAdmissionId 
--        left join SF_PatientPreAdmission pa  on aa.DataFormId = abs(pa.DataFormId) and aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID 
--        left join SF_DataForms df on aa.DataFormId = df.Id and aa.ClientId = df.PatientId 
--  where df.FormName = 'Admission Assessment' and aa.ClientId = 5224
where aa.ClientId = 5224

union
select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'Admission Assessment' as [FormName]
      , '7-' + convert(varchar, aa.ClientId) + '-' + convert(varchar, aa.PreAdmissionId) + '-' + convert(varchar, aa.id) as [FormID] 
      , aa.ClientId, convert(date, aa.CreatedOn) as [CreatedOn], convert(date, aa.ModifiedOn) as [UpdatedOn], aa.Isdeleted 
      , CompletedBySignatureSignatureDate = null
      , CounselorSignatureSignatureDate = null
      , DoctorSignatureSignatureDate = null 
      , MedicalProviderSignatureSignatureDate = null 
      , PatientSignatureDate = case when convert(date,aas.AdmissionAssessmentPatientSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentPatientSignatureDate) end 
      , ProviderSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentProviderSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentProviderSignatureDate) end 
      , RequestorSignatureDate = null
      , StaffSignatureDate = case when convert(date,aas.AdmissionAssessmentStaffSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentStaffSignatureDate) end 
      , SupervisorSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) end 
   from [dbo].[AdmissionAssessment] aa inner join [dbo].[AdmissionAssessmentSummary] aas on aa.Id = aas.AdmissionAssessmentId and aa.PreAdmissionId = aas.PreAdmissionId 
        left join SF_PatientPreAdmission pa  on aa.DataFormId = abs(pa.DataFormId) and aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID 
        --left join SF_DataForms df on aa.DataFormId = df.Id and aa.ClientId = df.PatientId 
  where aa.ClientId = 5224

select pa.PatientID, pa.Id PreAddmitID, abs(pa.DataFormId) DataFormID, df.IsDeleted
from SF_PatientPreAdmission pa left join SF_DataForms df on (pa.PatientID = df.PatientId)
where FormName = 'Admission Assessment' and pa.PatientId = 5224

select * from SF_DataForms where id in (38, 41)  
  
select * from AdmissionAssessment where ClientId = 5224
select * from AdmissionAssessmentSummary where Id in (38, 41) and PreAdmissionId in (3171, 3333)


select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'RI - BHOLD' as [FormName]
      , '7-' + convert(varchar, aa.ClientId) + '-' + convert(varchar, aa.PreAdmissionId) + '-' + convert(varchar, aa.id) as [FormID] 
      , aa.ClientId, convert(date, aa.CreatedOn) as [CreatedOn], convert(date, aa.ModifiedOn) as [UpdatedOn], aa.Isdeleted 
      , CompletedBySignatureSignatureDate = null
      , CounselorSignatureSignatureDate = null
      , DoctorSignatureSignatureDate = null 
      , MedicalProviderSignatureSignatureDate = null 
      , PatientSignatureDate = case when convert(date,aas.AdmissionAssessmentPatientSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentPatientSignatureDate) end 
      , ProviderSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentProviderSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentProviderSignatureDate) end 
      , RequestorSignatureDate = null
      , StaffSignatureDate = case when convert(date,aas.AdmissionAssessmentStaffSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentStaffSignatureDate) end 
      , SupervisorSignatureSignatureDate = case when convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) is null then '1900-01-01' else convert(date,aas.AdmissionAssessmentSupervisorSignatureDate) end 
   from [dbo].[RIBHOLD] a
        left join SF_PatientPreAdmission pa  on aa.DataFormId = abs(pa.DataFormId) and aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID 
        --left join SF_DataForms df on aa.DataFormId = df.Id and aa.ClientId = df.PatientId 
  where aa.ClientId = 5224

select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'RI - BHOLD' as [FormName]
      , '17-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] 
      , a.ClientId, convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn]
      , isnull((select df.Isdeleted from SF_PatientPreAdmission pa left join SF_DataForms df on a.DataFormId = df.Id and a.ClientId = df.PatientId 
  where df.FormName = 'RI - BHOLD' and a.DataFormId = abs(pa.DataFormId) and a.PreAdmissionId = pa.ID and a.ClientId = pa.PatientID ), a.IsDeleted) IsDeleted
      , CompletedBySignatureSignatureDate = null
      , CounselorSignatureSignatureDate = null
      , DoctorSignatureSignatureDate = null 
      , MedicalProviderSignatureSignatureDate = null 
      , PatientSignatureDate = null
      , ProviderSignatureSignatureDate =null
      , RequestorSignatureDate = null
      , StaffSignatureDate = case when convert(date,a.MedicalStaffSignatureDate) is null then '1900-01-01' else convert(date,a.MedicalStaffSignatureDate) end 
      , SupervisorSignatureSignatureDate = null
   from [dbo].[RIBHOLD] a
--        left join SF_PatientPreAdmission pa  on aa.DataFormId = abs(pa.DataFormId) and aa.PreAdmissionId = pa.ID and aa.ClientId = pa.PatientID 
--        left join SF_DataForms df on aa.DataFormId = df.Id and aa.ClientId = df.PatientId 
--  where df.FormName = 'Admission Assessment' and aa.ClientId = 5224
where aa.ClientId = 5224

