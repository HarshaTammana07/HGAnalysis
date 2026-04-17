--db:Data Source=BHGDALLSQL05\SQL2016SAMMS;Connection Timeout=60;Integrated Security=True;Encrypt=False;Initial Catalog=SAMMS-ClydeV5;
select * from (
select SiteCode, FormName, convert(varchar(100),FormId) FormId, PreAdmissionId, ClientId, QuestionId , QuestionOrderId = isnull(x.QuestionOrderId, Row_Number() over(Partition by x.FormName, x.FormId, x.ClientId, x.QuestionId order by x.QuestionId, x.AnswerSeq)) , QuestionText, OptionId, AnswerValue, CreatedBy, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted from (select SiteCode = 'B37', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId
, IsDeleted = case when isnull(f.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end, QuestionId = isnull(q.Id, 0), QuestionOrderId = q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value, AnswerSeq = a.Id 
 from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) left join Question q on (ft.Id = q.FormTemplateId)
 left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId) inner join [dbo].[SF_PatientPreAdmission] pa on (f.PreAdmissionId = pa.ID)
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)
 where a.Value is not null and (f.CreatedOn >= '2/10/2025' or isnull(f.UpdatedOn, f.CreatedOn) >= '2/10/2025') 
 union select SiteCode = 'B37', ft.FormName, f.id as FormId, f.ClientId, f.CreatedOn, f.CreatedBy, f.UpdatedOn, f.UpdatedBy, f.PreAdmissionId
, IsDeleted = case when isnull(f.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
 , QuestionId = isnull(q.Id, 0), QuestionOrderId = q.QuestionOrderId, q.QuestionText, a.OptionId, AnswerValue = a.Value, AnswerSeq = a.Id 
 from dbo.Form f left join FormTemplate ft on (f.FormTemplateId = ft.Id) left join Question q on (ft.Id = q.FormTemplateId)
 left join Answer a on (f.Id = a.FormId and q.id = a.QuestionId) inner join [dbo].[SF_PatientPreAdmission] pa on (f.PreAdmissionId = pa.ID)
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)
 where q.Id is null and (f.CreatedOn >= '2/10/2025' or isnull(f.UpdatedOn, f.CreatedOn) >= '2/10/2025')) x 
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Suicide Severity Rating Scale'
, FormID = '1-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from SuicideSeverityRatingScale a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Health Questionnaire'
, FormID = '2-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.Createddate), a.ModifiedBy as [UpdatedBy], convert(date, a.Modifieddate) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from HealthQuestionnaire a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Infectious Disease And Behavioral Screen'
, FormID = '3-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from InfectiousDiseaseAndBehavioralScreen a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Consent to Treatment with an Approved Narcotic'
, FormID = '4-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentToTreatmentWithAnApprovedNarcotic a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Financial Hardship Application'
, FormID = '5-' + convert(varchar, a.cltId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.cltId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null
, a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from FinancialHardshipApplication a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Comprehensive Assessment Form'
, FormID = '6-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ComprehensiveAssessmentForm a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Admission Assessment'
, FormID = '7-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from AdmissionAssessment a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Treatment Plan', [FormID] = '8-1-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID), null PreAdmissionId, ABS(tprCLTID) ClientId, 1 as QuestionID, 1 as QuestionOrderID, 'Treatment Plan Type' as QuestionText, null as [OptionID] , TprTYPE as AnswerValue, tprCreatedby Createdby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] , Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW 
 union Select SiteCode = 'B37', 'TP-' + TprTYPE as [FormName] , '8-2-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] , null PreAdmissionId, ABS(tprCLTID) ClientId, 2 as QuestionID, 1 as QuestionOrderID, 'Treatment Phase Type' as QuestionText, null as [OptionID] , tpTreatmentPhase as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] , Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW 
 union Select SiteCode = 'B37', 'TP-' + TprTYPE as [FormName] , '8-3-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] , null PreAdmissionId, ABS(tprCLTID) ClientId, 3 as QuestionID, 1 as QuestionOrderID, 'Next Due' as QuestionText, null as [OptionID] , convert(varchar, tprNEXT) as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn], null as [UpdatedBy], null as [UpdatedOn] , Isdeleted = Case when tprCLTID < 0 then 1 else 0 end from [dbo].tblTP17REVIEW 
union Select SiteCode = 'B37', 'TP-' + TprTYPE as [FormName] , '8-4-' + convert(varchar, ABS(tprCLTID)) + '-' + convert(varchar, tpRID) + '-' + convert(varchar, tprTPID) as [FormID] , null PreAdmissionId, ABS(tprCLTID) ClientId, 4 as QuestionID, 1 as QuestionOrderID, 'Review Frequency' as QuestionText, null as [OptionID] , case when Len(tprReviewFrequency) > 6 then rtrim(substring(tprReviewFrequency, 6, LEN(tprReviewFrequency) - 5)) else rtrim(tprReviewFrequency) end as AnswerValue, tprCreatedby, convert(date, tprDT) as [CreatedOn] , null as [UpdatedBy], null as [UpdatedOn], Isdeleted = Case when tprCLTID< 0 then 1 else 0 end from[dbo].tblTP17REVIEW  ) v  Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Level Justification'
, FormID = '9-1-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ 
, PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 1, QuestionText = 'Effective Date', OptionID = 0, AnswerValue = convert(varchar,EffectiveDate, 101) 
, Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) 
, IsDeleted = Case when cltID < 0 then 1 else 0 end from [dbo].[tblORDERREQ] 
 where status = 'Approved' and(Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST') 
 union select SiteCode = 'B37', FormName = 'Level Justification' 
, FormID = '9-2-' + convert(varchar, ABS(cltID)) + '-' + convert(varchar, ReqNum) + '-' /* + convert(varchar, tprTPID) */ 
, PreadmissionId = ReqNum, ClientId = cltID, QuestionID = 2, QuestionText = 'Expiration Date', OptionID = 0, AnswerValue = convert(varchar,expirationdate, 101) 
, Createdby = Staff, CreatedOn = Convert(date, DateAdded), UpdatedBy = StatusUser, UpdatedOn = convert(date, statusDate) 
, IsDeleted = Case when cltID < 0 then 1 else 0 end from [dbo].[tblORDERREQ] 
 where status = 'Approved' and (Notes not like 'Test %' and Notes <> 'TEST' and  DrNote <> 'HEllo test' and DrNote <> 'TEST')) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'BHG Notice Of Privacy Practices'
, FormID = '10-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from tblBHGNoticeOfPrivacyPractices a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Suicide Severity Rating Scale 2.0'
, FormID = '11-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from SAFETProtocolwithCSSRS a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'KS Patient Rights and Responsibilities'
, FormID = '12-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from KSPatientRightsResponsibilities a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'CO - Consent Central Registry Colorado'
, FormID = '13-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentCentralRegistryColorado a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MN - Consent to Central Registry'
, FormID = '14-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from GeneralConsentAuthforReleaseInfo a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Adverse Childhood Experiences'
, FormID = '15-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedDate), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedDate) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from AdverseChildhood a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Patient Information sheet'
, FormID = '16-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from PatientInformationsheet a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Insurance Benefit Verification'
, FormID = '17-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.PreAdmissionId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null
, a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from InsuranceBenefitVerification a inner join [dbo].[SF_PatientPreAdmission] pa on (a.PreAdmissionId = pa.ID) 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MN - Mental Health Informed Consent'
, FormID = '18-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from MentalHealthInformedConsent a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'State Fact Form'
, FormID = '19-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from StateFactForm a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Initial Services Plan and Vulnerable Adult Determination'
, FormID = '20-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from InitialServicesPlanandVAD a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MN - Authorization for Release of Information to the MAARC'
, FormID = '21-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedDate) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from tblMAARC a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MN - DAANES Notification Form'
, FormID = '22-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from tblDAANESNotification a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MN - Consent to Treatment Via Telehealth'
, FormID = '23-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsenttoTreatmentViaTelehealth a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - BHOLD'
, FormID = '24-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIBHOLD a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home Care Plan Review Form'
, FormID = '25-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomeCareReview a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home Consent to Receive'
, FormID = '26-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomeConsentToReceive a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home Eligibility and Follow up Checklist'
, FormID = '27-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomeEligibilityFollUpChecklist a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home History'
, FormID = '28-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomeHistory a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home Note'
, FormID = '29-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomeNote a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home Triage Assessment'
, FormID = '30-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomeTriageAssessment a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Health Home Patient Centered Plan of Care'
, FormID = '31-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIHealthHomePatientCenteredPlan a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - Overdose Prevention Education'
, FormID = '32-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIOverdosePreventionEducation a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'RI - PHQ-9 Form'
, FormID = '33-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RIPHQ9 a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'GA - Patient Rights and Responsibilities'
, FormID = '34-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from PatientRightsandResponsibilities a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'GA - Order for Services'
, FormID = '35-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from OrderforServices a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'GA - Consent to Treatment with an Approved Narcotic'
, FormID = '36-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from GAConsenttoTreatmentwithanApprovedNarcotic a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'GA - Consent Central Registry Georgia'
, FormID = '37-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from GAConsentCentralRegistryGeorgia a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'GA - Transition and Discharge Plan'
, FormID = '38-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from TransitionandDischargePlan a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'NC - Consent to Central Registry'
, FormID = '39-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from NCConsenttoCentralRegistry a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'NC - Initial Transition and Discharge Plan'
, FormID = '40-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from NCInitialTransitionDischargePlan a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'NC - Crisis Prevention and Intervention Plan'
, FormID = '41-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from CrisisPrevention a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'NC - Consent and Auth Disclosure of Sub Disorder'
, FormID = '42-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from NCConsentAuthDisclosureSubDisorder a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'NC - Person Centered Profile'
, FormID = '43-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from NCPersonCenteredProfile a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'NC - PIE'
, FormID = '44-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from NCPIE a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = '90 Day Review'
, FormID = '45-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from NinetyDayReview a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'AR-State Fact form'
, FormID = '46-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.Createdon), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from StateFactForm a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Consent for Release of Confidential Info V0'
, FormID = '48-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentforReleaseConInfoRevised a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Guest Dosing/Permanent Transfer'
, FormID = '52-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from GuestDosingPermanentTransfer a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Pre Admission'
, FormID = '58-' + convert(varchar, a.PatientId) + '-' + convert(varchar, isnull(a.ParentPreAdmissionId, 0)) + '-' + convert(varchar, isnull(a.id,0))
, PreAdmissionId = a.Id, ClientId = a.PatientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.LastUpdatedBy as [UpdatedBy], convert(date, a.LastUpdateOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from SF_PatientPreAdmission a inner join [dbo].[SF_PatientPreAdmission] pa on a.ID = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Referral'
, FormID = '60-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.createdon), a.updatedby as [UpdatedBy], convert(date, a.updatedon) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ReferralForm a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Understanding of Treatment'
, FormID = '61-' + convert(varchar, pa.PatientId) + '-' + convert(varchar, isnull(a.PreAdmissionId, 0)) + '-' + convert(varchar, isnull(a.id,0))
, PreAdmissionId = a.PreAdmissionId, ClientId = pa.PatientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.Createddate), a.LastUpdatedBy as [UpdatedBy], convert(date, a.UpdatedDate) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from SF_UnderstandingOfTreatment a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MO - Consent Central Registry Missouri'
, FormID = '62-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from MOConsentCentralRegistryMissouri a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'SC - Consent Central Registry'
, FormID = '63-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from SCConsentReleaseCentralRegistry a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Consent to Disclose Assignment of Benefits Ver 1'
, FormID = '64-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentToDiscloseAssignmentofBenefits a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Consent to Treatment for IOP Or EOP Or OP'
, FormID = '65-' + isnull(convert(varchar, a.ClientId), '0') + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.Createdon), a.ModifiedBy as [UpdatedBy], convert(date, a.Modifiedon) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsenttoTreatmentforIOPOrEOPOrOP a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'GPRA'
, FormID = '66-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.Createdon), a.ModifiedBy as [UpdatedBy], convert(date, a.Modifiedon) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from GPRA a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'MAT and Driving'
, FormID = '67-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from MATandDriving a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Patient Rights And Responsibilities V2'
, FormID = '68-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from PatientRightsAndResponsibilitiesV2 a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Request Release of Medical Records V2'
, FormID = '69-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from RequestReleaseofMedicalRecordsV2 a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Take Home Risk Assessment'
, FormID = '70-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from TakeHomeRiskAssessment a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Adult Nutritional Screen'
, FormID = '71-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from AdultNutritionalScreen a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Consent For Follow-Up Contact'
, FormID = '72-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentForFollowUpContact a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'Consent for Release of Confidential Information to Emergency Contact Ver 1'
, FormID = '73-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentReleaseEmergencyContact a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'AL - Consent to Central Registry'
, FormID = '74-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentCentralRegistryAlabama a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
Union Select SiteCode, FormName, FormID, PreadmissionId, ClientId, QuestionID, QuestionOrderID = Row_Number() over(Partition by v.FormName, v.FormId, v.ClientId, v.QuestionId order by v.QuestionId), QuestionText, OptionID, AnswerValue, Createdby, CreatedOn, UpdatedBy, UpdatedOn, IsDeleted 
from (Select SiteCode = 'B37', [FormName] = 'LA - Consent to Central Registry'
, FormID = '75-' + convert(varchar, a.ClientId) + '-' + convert(varchar, a.PreAdmissionId) + '-' + convert(varchar, a.id)
, PreAdmissionId = a.PreAdmissionId, ClientId = a.ClientId, QuestionID = 0, QuestionOrderID = 1, QuestionText = null, [OptionID] = null, AnswerValue = null , a.Createdby, [CreatedOn] = convert(date, a.CreatedOn), a.ModifiedBy as [UpdatedBy], convert(date, a.ModifiedOn) as [UpdatedOn]
, IsDeleted = case when isnull(a.IsDeleted,0) = 0 and pa.IsDeleted <> 1 and isnull(pa.DataFormId,0) >= 0 and isnull(d.IsDeleted,0) = 0 then 0 else 1 end
  from ConsentCentralRegistryLouisiana a inner join [dbo].[SF_PatientPreAdmission] pa on a.PreAdmissionId = pa.ID 
 left join dbo.SF_DataForms d on (pa.DataFormId = d.Id)) v Where (v.CreatedOn >= '2/10/2025' or isnull(v.UpdatedOn, v.CreatedOn) >= '2/10/2025')
) z
