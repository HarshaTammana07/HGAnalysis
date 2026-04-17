select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID 
     , QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) 
     , QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (
  Select SiteCode = ' + st.SiteCode.ToString() + ', 'RI - BHOLD' as [FormName] 
     , '24-' + convert(varchar, ClientId) + '-' + convert(varchar, PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] 
     , PreAdmissionId, ClientId, 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue 
     , a.Createdby, convert(date, a.CreatedOn) as [CreatedOn], a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
     , IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from [dbo].NinetyDayReview a inner join [dbo].[SF_PatientPreAdmission] pa on (a.PreAdmissionId = pa.ID)
    left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v

select distinct SiteCode = '" + st.SiteCode.ToString() + "', 'RI - Health Home Care Plan Review Form' as [FormName]
     , '24-' + convert(varchar, isnull(a.ClientId, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID]
     , ClientId = isnull(a.ClientId, 0), convert(date, a.CreatedOn) as [CreatedOn], convert(date, a.ModifiedOn) as [UpdatedOn]
     , isnull((select IsDeleted from dbo.SF_DataForms df where df.FormName = 'RI - Health Home Care Plan Review Form' and pa.DataFormId = df.Id), a.Isdeleted) IsDeleted
     , CompletedBySignatureSignatureDate = null 
     , CounselorSignatureSignatureDate = null 
  --//case when convert(date, CounselorsignatureDate) is null then '1900-01-01' else convert(date, CounselorsignatureDate) end  
    , DoctorSignatureSignatureDate = null
    , MedicalProviderSignatureSignatureDate = null
    , PatientSignatureDate = case when convert(date, a.PatientSignatureDate) is null then '1900-01-01' else convert(date, a.PatientSignatureDate) end 
    , ProviderSignatureSignatureDate = null
    , RequestorSignatureDate = null
    , [StaffSignatureDate] = case when convert(date, a.StaffSignatureDate) is null then '1900-01-01' else convert(date, a.StaffSignatureDate) end 
    , SupervisorSignatureSignatureDate = null
 from [dbo].[RIHealthHomeCareReview] a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID
