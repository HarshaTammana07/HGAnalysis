select SiteCode, FormName, FormId, ClientId, CreatedOn, UpdatedOn 
     , CompletedBySignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'CompletedBySignatureSignatureDate' order by [DateTime] desc)
     , CounselorSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'CounselorSignatureSignatureDate' order by [DateTime] desc)
     , DoctorSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'DoctorSignatureSignatureDate' order by [DateTime] desc)
     , MedicalProviderSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'MedicalProviderSignatureSignatureDate' order by [DateTime] desc)
     , PatientSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'PatientSignatureDate' order by [DateTime] desc)
     , ProviderSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'ProviderSignatureSignatureDate' order by [DateTime] desc)
     , RequestorSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'RequestorSignatureDate' order by [DateTime] desc)
     , StaffSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'StaffSignatureDate' order by [DateTime] desc)
     , SupervisorSignatureSignatureDate = (select top 1 case when Sign is null then '1/1/1900' else [DateTime] end from AnswerSignature where FormId = x.FormId and DateField = 'SupervisorSignatureSignatureDate' order by [DateTime] desc)
from (select SiteCode = 'D09', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.UpdatedOn
        from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) 
where (f.CreatedOn >= '1/1/2000' or isnull(f.UpdatedOn, f.CreatedOn) >= '1/1/2000')
union select SiteCode = 'D09', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.UpdatedOn
 from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) 
where (f.CreatedOn >= '1/1/2000' or isnull(f.UpdatedOn, f.CreatedOn) >= '1/1/2000')) x where FormName = 'TP-Plan Review'
order by SiteCode, FormName, FormId, ClientId


select * from tblTP17REVIEW where tprCLTID = 8483

select distinct SiteCode, FormName, FormID, ClientId, CreatedOn, UpdatedOn, IsDeleted 
     , CompletedBySignatureSignatureDate, CounselorSignatureSignatureDate, DoctorSignatureSignatureDate, MedicalProviderSignatureSignatureDate 
     , PatientSignatureDate, ProviderSignatureSignatureDate, RequestorSignatureDate, StaffSignatureDate, SupervisorSignatureSignatureDate from (
      Select SiteCode = '" + st.SiteCode.ToString() + "', 'TP-' + tprType as [FormName] 
           , '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID]
           , null PreAdmissionId, tprCLTID ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID], TprTYPE as AnswerValue
           , tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID < 0 then 1 else 0 end
           , CompletedBySignatureSignatureDate = null, CounselorSignatureSignatureDate = null, DoctorSignatureSignatureDate = null, MedicalProviderSignatureSignatureDate = null
           , PatientSignatureDate = case when convert(date, tprCLIRNTSIGDate) is null then '1900-01-01' else convert(date, tprCLIRNTSIGDate) end
           , ProviderSignatureSignatureDate = case when convert(date, tprDRSIGDate) is null then '1900-01-01' else convert(date, tprDRSIGDate) end
           , RequestorSignatureDate = null
           , StaffSignatureDate = case when(convert(date, tprCOUNSSIGDate) is null and convert(date, tprSUPERSIGDate) is null) then '1900-01-01' else convert(date, tprCOUNSSIGDate) end 
           , SupervisorSignatureSignatureDate = convert(date, tprSUPERSIGDate) from [dbo].tblTP17REVIEW) tp where FormID = '8-1-8483-3926-1480'