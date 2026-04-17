select m.FormTemplateId, m.FormName, m.TableName, m.PatientSignatureDateColumn, m.StaffSignatureDateColumn, m.MedProviderSignatureDateColumn
     , m.SupervisorSignatureDateColumn, m.CreatedOnColumn, m.NurseSignatureDateColumn, m.SignatureTableName, m.SignatureTableJoinColumn
  from tblTreatmentFormAlertMappings m
 --where m.FormName = 'MN - Mental Health Informed Consent' --m.IsEnabled = 1
order by m.FormName

select * from tblDataForms where IsDeleted = 0
select * from [dbo].[tblFORMS]
--select * from [dbo].[tblConfigForms]
