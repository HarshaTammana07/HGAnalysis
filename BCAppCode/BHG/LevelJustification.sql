
Select-- (For ETL’ing to pats.tbl_dbo_FormAnswerSignatures)

'Level Justification' as [FormName],
--Logic that was discussed to created unique ID--  as [FormID], Form Addition ID, and cltID and ReqNum 
ABS(cltID) as ClientId, 
convert(date, DateAdded) as [CreatedOn],
convert(date, statusDate) as [UpdatedOn],
Case when cltID < 0 then 1 else 0 end as Isdeleted,
case when (ISNull(convert(date,DrSigDt),convert(date,SigNurseDt))  is null  and Status = 'Approved') then '1900-01-01' else ISNull(convert(date,DrSigDt),convert(date,SigNurseDt)) end as ProviderSignatureDate, ---Brian will review architecture of AnswerSig to determin correct name of column
case when convert(date,sigCoordinatorDt) is null and Status = 'Approved' then '1900-01-01' else convert(date,sigCoordinatorDt) end as SupervisorSignatureDate
from [dbo].[tblORDERREQ] where  status = 'Approved' and (Notes not like 'Test %' and Notes <>'TEST' and  DrNote  <>'HEllo test' and DrNote <>'TEST')



select SiteCode, FormName, FormID = FormId + '-' + convert(varchar,Row_Number() over(Partition by tp.FormName, tp.FormId, tp.ClientId, tp.QuestionId order by tp.AnswerValue))
     , PreadmissionId, ClientId, QuestionID 
     , QuestionOrderID = Row_Number() over(Partition by tp.FormName, tp.FormId, tp.ClientId, tp.QuestionId order by tp.AnswerValue) 
     , QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (
select SiteCode = 'SC', FormName = 'Level Justification'
     , FormID = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' --+ convert(varchar, tprTPID)
     , PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 1
     , QuestionText = 'Effective Date', OptionID = 0, AnswerValue = EffectiveDate
     , Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate)
     , IsDeleted = Case when cltID < 0 then 1 else 0 end
from [dbo].[tblORDERREQ] where  status = 'Approved' and (Notes not like 'Test %' and Notes <>'TEST' and  DrNote  <>'HEllo test' and DrNote <>'TEST')
union select SiteCode = 'SC', FormName = 'Level Justification'
     , FormID = '9-2-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar,ReqNum ) + '-' --+ convert(varchar, tprTPID)
     , PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 2
     , QuestionText = 'Expiration Date', OptionID = 0, AnswerValue = expirationdate
     , Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate)
     , IsDeleted = Case when cltID < 0 then 1 else 0 end
from [dbo].[tblORDERREQ] where  status = 'Approved' and (Notes not like 'Test %' and Notes <>'TEST' and  DrNote  <>'HEllo test' and DrNote <>'TEST')

) tp
order by 3



Select SiteCode = 'st.SiteCode.ToString()', 'Level Justification' as [FormName] 
     , [FormID] = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-1' --+ convert(varchar, tprTPID)
     /*, Createdby = staff*/, [CreatedOn] = convert(date, DateAdded)
     /*, [UpdatedBy] = StatusUser*/, [UpdatedOn] = convert(date, statusDate)
     --, Isdeleted = Case when cltID < 0 then 1 else 0 end 
     , CompletedBySignatureSignatureDate = null
     , CounselorSignatureSignatureDate = null
     , DoctorSignatureSignatureDate = null
     , MedicalProviderSignatureSignatureDate = null
     , PatientSignatureDate = null
     , ProviderSignatureSignatureDate = case when (ISNull(convert(date,DrSigDt),convert(date,SigNurseDt))  is null  and Status = 'Approved') then '1900-01-01' else ISNull(convert(date,DrSigDt),convert(date,SigNurseDt)) end
     , RequestorSignatureDate = null 
     , StaffSignatureDate = null
     , SupervisorSignatureSignatureDate = case when convert(date,sigCoordinatorDt) is null and Status = 'Approved' then '1900-01-01' else convert(date,sigCoordinatorDt) end
  from [dbo].[tblORDERREQ] 
order by 3

select * from sf_Cows 