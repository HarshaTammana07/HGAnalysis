--select IsEnabled, DataFormId, FormName, TableName, PatientSignatureDateColumn, MedProviderSignatureDateColumn, StaffSignatureDateColumn, CreatedOnColumn, SignatureTableName
--  from tblTreatmentFormAlertMappings order by FormName

select *
from INFORMATION_SCHEMA.COLUMNS
where TABLE_NAME='RNP_AuthorizationToObtainOrRelease'