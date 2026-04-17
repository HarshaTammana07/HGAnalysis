select * from dms.tbl_MapAction where ActionKey = 1
select * from dms.tbl_MapSrc2Dsn where ActionKey = 1 and ActionStepKey = 9
insert into dms.tbl_MapSrc2Dsn (ActionKey, ActionStepKey, FieldKey, FieldName, Enabled, PHC_Enabled, FieldType, FieldLength, FieldPrecision, FieldScale, LastModAt, LastModBy, DsnFieldName) values (1, 9, 33, 'UAEval', 1, 0, 'varchar', 50, 0, 0, GetDate(), 'Brian.Catellier', 'UAEval')
--update dms.tbl_MapSrc2Dsn set Enabled = 0 where ActionKey = 1 and ActionStepKey = 62 and FieldKey in (6, 23)
update dms.tbl_MapAction set SortOrder = 'Order by SiteCode, dgid' where ActionKey = 1 and StepKey = 83
FromTblVw = 'tblTreatmentLevel', DsnTbl = 'tbl_TreatmentLevel' where StepKey = 61 and ActionKey = 1
update dms.tbl_MapAction set FromTblVw = 'admissionassessmentsubstanceusehistory', DsnTbl = 'tbl_Admissionassessmentsubstanceusehistory' where StepKey = 62 and ActionKey = 1
update dms.tbl_MapAction set DsnTbl = 'tbl_NewAdmissionAssessmentASAMDimension6' where ActionKey = 1 and StepKey = 79
update dms.tbl_MapAction set FromTblVw = 'ReAssessment', DsnTbl = 'Tbl_ReAssessment' where ActionKey = 1 and StepKey = 44
update dms.tbl_MapAction set WhereCondition = '(tpcnDtmAddedarnDATE >= \''1/1/2023\'' and tpcnDtmAdded >= @WorkDate) or tpcnDtTickler >= @WorkDate' where ActionKey = 1 and StepKey = 34
update dms.tbl_MapAction set WhereCondition = '(arnDATE >= \''1/1/2023\'' and arnDATE >= @WorkDate) or arnDtRemoved >= @WorkDate' where ActionKey = 1 and StepKey = 35

insert into dms.tbl_MapAction (ActionKey, StepKey, Enabled, ValidateStructure, DsnConID, SrcSchema, DsnSchema, CtrlMethod, FromTblVw, DsnTbl, WhereCondition, ReInitialize, LastModBy, LastModAt, SortOrder)
  values 
  (1, 84, 1, 0, 1, 'dbo', 'pats', 12, 'NewPeriodicReassessment', 'tbl_NewPeriodicReassessment', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by 1, 2'),
  (1, 85, 1, 0, 1, 'dbo', 'pats', 12, 'NewPeriodicReassessmentCounselorReview', 'tbl_NewPeriodicReassessmentCounselorReview', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by 1, 2'),
  (1, 83, 1, 0, 1, 'dbo', 'pats', 12, 'Tbldiag10', 'tbl_TblDiag10', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, id')
-- (1, 65, 1, 0, 1, 'dbo', 'pats', 12, 'PACounselorReview', 'tbl_PACounselorReview', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')

--(1, 66, 1, 0, 1, 'dbo', 'pats', 12, 'PADimension1', 'tbl_PADimension1', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')
--(1, 67, 1, 0, 1, 'dbo', 'pats', 12, 'PADimension2', 'tbl_PADimension2', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')
--(1, 68, 1, 0, 1, 'dbo', 'pats', 12, 'PADimension3', 'tbl_PADimension3', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')
(1, 69, 1, 0, 1, 'dbo', 'pats', 12, 'PADimension4', 'tbl_PADimension4', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')
, (1, 70, 1, 0, 1, 'dbo', 'pats', 12, 'PADimension5', 'tbl_PADimension5', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')
, (1, 71, 1, 0, 1, 'dbo', 'pats', 12, 'PADimension6', 'tbl_PADimension6', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, PeriodicReassessmentId')

, (1, 56, 1, 0, 1, 'dbo', 'pats', 12, 'ReAssessmentSocial', 'tbl_ReAssessmentSocial', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, Id')
, (1, 57, 1, 0, 1, 'dbo', 'pats', 12, 'ReAssessmentTreatment', 'tbl_ReAssessmentTreatment', '1 = 1', 0, 'Brian.Catellier', GetDate(), 'Order by SiteCode, Id')

insert into dms.tbl_MapSrc2Dsn (ActionKey, ActionStepKey, FieldKey, FieldName, Enabled, PrimaryKey, FieldType, FieldLength, FieldPrecision, FieldScale, LastModAt, LastModBy, DsnFieldName, Nullable, PHC_Enabled)
select 1, 85, c.column_id, case when c.name = 'SiteCode' then '@SiteCode' else c.name end, 1 [Enabled]
      , (select key_ordinal from sys.index_columns ic inner join sys.indexes i on (ic.object_id = i.object_id and ic.index_id = i.index_id) 
	      where i.is_primary_key = 1 and i.object_id = o.object_id and ic.column_id = c.column_id) [PrimKey]
	  , t.name coltype, c.max_length, c.precision, c.scale 
	  , GetDate(), 'Brian.Catellier', c.name, c.is_nullable, 0
	  --, c.is_nullable, c.is_identity, c.is_replicated, cc.name checkcon, cc.definition checkdef, dc.name defcon, dc.definition defcondef
  from sys.all_columns c inner join sys.all_objects o on (c.object_id = o.object_id and o.type in ('U', 'V')) 
    inner join sys.systypes t on (c.user_type_id = t.xusertype) 
    inner join sys.schemas sc on (o.schema_id = sc.schema_id)
    left join sys.default_constraints dc on (dc.parent_object_id = o.object_id and dc.parent_column_id = c.column_id)
	left join sys.check_constraints cc on (cc.parent_object_id = o.object_id and cc.parent_column_id = c.column_id)
  where o.name = 'tbl_NewPeriodicReassessmentCounselorReview' and c.name not in ('LastModETL', 'LastModAt', 'RowState', 'RowChkSum', 'upsize_ts')
  order by o.Name, c.column_id;

insert into dms.tbl_MapSrc2Dsn (ActionKey, ActionStepKey, FieldKey, FieldName, Enabled, PrimaryKey, FieldType, FieldLength, FieldPrecision, FieldScale, LastModAt, LastModBy, DsnFieldName, Nullable, PHC_Enabled)
select 1, 65, c.column_id, case when c.name = 'SiteCode' then '@SiteCode' else c.name end, 1 [Enabled]
      , (select key_ordinal from sys.index_columns ic inner join sys.indexes i on (ic.object_id = i.object_id and ic.index_id = i.index_id) 
	      where i.is_primary_key = 1 and i.object_id = o.object_id and ic.column_id = c.column_id) [PrimKey]
	  , t.name coltype, c.max_length, c.precision, c.scale 
	  , GetDate(), 'Brian.Catellier', c.name, c.is_nullable, 0
	  --, c.is_nullable, c.is_identity, c.is_replicated, cc.name checkcon, cc.definition checkdef, dc.name defcon, dc.definition defcondef
  from sys.all_columns c inner join sys.all_objects o on (c.object_id = o.object_id and o.type in ('U', 'V')) 
    inner join sys.systypes t on (c.user_type_id = t.xusertype) 
    inner join sys.schemas sc on (o.schema_id = sc.schema_id)
    left join sys.default_constraints dc on (dc.parent_object_id = o.object_id and dc.parent_column_id = c.column_id)
	left join sys.check_constraints cc on (cc.parent_object_id = o.object_id and cc.parent_column_id = c.column_id)
  where o.name = 'tbl_PACounselorReview' and c.name not in ('LastModETL', 'LastModAt', 'RowState', 'RowChkSum')
  order by o.Name, c.column_id
  --select * from dms.tbl_MapSrc2Dsn where ActionKey = 1 and ActionStepKey >= 28

insert into dms.tbl_MapSrc2Dsn (ActionKey, ActionStepKey, FieldKey, FieldName, Enabled, PrimaryKey, FieldType, FieldLength, FieldPrecision, FieldScale, LastModAt, LastModBy, DsnFieldName, Nullable, PHC_Enabled)
select 2, 13, c.column_id, case when c.name = 'SiteCode' then '@SiteCode' else c.name end, 1 [Enabled]
      , (select key_ordinal from sys.index_columns ic inner join sys.indexes i on (ic.object_id = i.object_id and ic.index_id = i.index_id) 
	      where i.is_primary_key = 1 and i.object_id = o.object_id and ic.column_id = c.column_id) [PrimKey]
	  , t.name coltype, c.max_length, c.precision, c.scale 
	  , GetDate(), 'Brian.Catellier', c.name, c.is_nullable, 0
	  --, c.is_nullable, c.is_identity, c.is_replicated, cc.name checkcon, cc.definition checkdef, dc.name defcon, dc.definition defcondef
  from sys.all_columns c inner join sys.all_objects o on (c.object_id = o.object_id and o.type in ('U', 'V')) 
    inner join sys.systypes t on (c.user_type_id = t.xusertype) 
    inner join sys.schemas sc on (o.schema_id = sc.schema_id)
    left join sys.default_constraints dc on (dc.parent_object_id = o.object_id and dc.parent_column_id = c.column_id)
	left join sys.check_constraints cc on (cc.parent_object_id = o.object_id and cc.parent_column_id = c.column_id)
  where o.name = 'tbl_LiquidLog' and c.name not in ('LastModETL', 'LastModAt', 'RowState', 'RowChkSum')
  order by o.Name, c.column_id
 
update dms.tbl_MapSrc2Dsn set enabled = 0 where ActionKey = 1 and ActionStepKey = 35 and FieldKey in (2,4)

select * from tsk.tbl_Schedule
insert into tsk.tbl_Schedule (ScheduleId, [Enabled], RowState, LastModBy, LastModAt, [Name], TriggerKey, ActionKey, NextRunTime, LastRunTime)
Values (24, 1, 24, 'Brian.Catellier', GetDate(), 'SAMMS-ETL-INV', 1, 2, '2024-11-21 20:06:00.000', '2024-11-20 20:06:00.000')
----
