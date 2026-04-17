select ft.FormName, f.id, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId, f.IsDeleted, f.IsChildForm
     , QuestionId = q.Id, q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value
  from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id)
    left join Question q on (ft.Id = q.FormTemplateId)
    left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId)
where a.Value is not null --and isnull(f.IsDeleted, 0) = 0

select top 100 * from Question
select top 100 * from Answer
select top 100 * from QuestionAnswerOption
select * from AdverseChildhood order by id

select *
  from (select QuestionId, Text, 'Ans_' + convert(varchar(4),OrderId) codeval from QuestionAnswerOption) qa PIVOT
  (
    max([Text])
     For Codeval in ([Ans_10], [Ans_20], [Ans_30], [Ans_40], [Ans_50], [Ans_60], [Ans_70], [Ans_80], [Ans_90], [Ans_100], [Ans_110], [Ans_120]
       , [Ans_130], [Ans_140], [Ans_150], [Ans_160], [Ans_170], [Ans_180], [Ans_190], [Ans_200], [Ans_210], [Ans_220], [Ans_230], [Ans_240], 
       [Ans_250], [Ans_260], [Ans_270], [Ans_280], [Ans_290], [Ans_300], [Ans_310], [Ans_320], [Ans_330], [Ans_340], [Ans_350], [Ans_360], 
       [Ans_370], [Ans_380], [Ans_390], [Ans_400], [Ans_410], [Ans_420], [Ans_430], [Ans_440])
  ) pvt

select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID 
     , QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId) 
     , QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (
      Select SiteCode = '" + st.SiteCode.ToString() + "', 'Adverse Childhood Experiences' as [FormName] 
            , '19-' + convert(varchar, isnull(pa.PatientID, 0)) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id) as [FormID] 
            , a.PreAdmissionId, ClientId = isnull(pa.PatientID, 0), 0 as QuestionID, 1 as QuestionOrderID, null as QuestionText, null as [OptionID], null as AnswerValue 
            , a.Createdby, convert(date, a.CreatedOn) as [CreatedOn], a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn], a.Isdeleted, a.ClientId
        from [dbo].StateFactForm a left join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID) v
