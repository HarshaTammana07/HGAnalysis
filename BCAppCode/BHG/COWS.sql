select COWID = c.id, CltID = p.PatientID, c.preadmissionid, dttime, reasonforthisAssessment, c.RestingPulseRate
     , drp.dropdownlistitem as RestingPulseRatedesc, c.giupset, dgi.dropdownlistitem as GIUpsetdesc
     , c.sweating,dsw.dropdownlistitem as Sweatingdesc,  c.tremor, dt.dropdownlistitem as Tremordesc
     , c.Restlessness  ,dr.dropdownlistitem as Restlessnessdesc, c.Yawning, dy.dropdownlistitem as Yawningdec
     , c.PupilSize, dps.dropdownlistitem as PupilSizedesc, c.AnxietyOrIrritability, doi.dropdownlistitem as AnxietyOrIrritabilitydesc
     , c.BoneOrJointAches,dbj.dropdownlistitem as BoneOrJointAchesdesc,  c.GoosefleshSkin, dgf.dropdownlistitem as GoosefleshSkindesc
     , c.RunnyNoseOrTearing  ,drnt.dropdownlistitem as RunnyNoseOrTearingdesc
     , c.CompletedBy, c.CreatedOn, c.CreatedBy, c.UpdatedBy, c.UpdatedOn, c.IsActive, c.PatientSignature, ClientSignatureDate, c.IsDeleted, staffNameSignature, c.Version
     , RowChkSum = CHECKSUM(c.id, p.PatientID, c.preadmissionid, dttime, c.RestingPulseRate, c.giupset, dgi.dropdownlistitem, c.sweating, c.tremor
     , c.Restlessness, c.Yawning, dy.dropdownlistitem, c.PupilSize, c.AnxietyOrIrritability
     , c.BoneOrJointAches, c.GoosefleshSkin, c.RunnyNoseOrTearing, c.CreatedOn, c.UpdatedOn, c.IsActive, c.IsDeleted, c.Version)
  from SF_Cows c
left join DroDownListItems drp on c.RestingPulseRate = drp.Id
left join DroDownListItems dgi on c.GIUpset = dgi.Id
left join DroDownListItems dsw on c.Sweating = dsw.Id
left join DroDownListItems dt on c.Tremor = dt.Id
left join DroDownListItems dr on c.restlessness = dr.Id
left join DroDownListItems dy on c.Yawning = dy.Id
left join DroDownListItems dps on c.PupilSize = dps.Id
left join DroDownListItems doi on c.AnxietyOrIrritability = doi.Id
left join DroDownListItems dbj on c.BoneOrJointAches = dbj.Id
left join DroDownListItems dgf on c.GoosefleshSkin = dgf.Id
left join DroDownListItems drnt on c.RunnyNoseOrTearing = drnt.Id
left join SF_PatientPreAdmission p on c.PreAdmissionId = p.ID
--left join dbo.tblClient clt on (p.PatientID = clt.cltID)
order by 2

select name from sys.tables t where upper(name) = 'SF_COWS'
select * from sys.all_columns c where c.object_id = (select object_id from sys.tables where upper(name) = 'SF_COWS') --and name = 'tprReviewFrequency'