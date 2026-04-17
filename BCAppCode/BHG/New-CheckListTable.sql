select * from pats.Tbl_OrientationChecklistNew
select * from ctrl.tbl_Forms2Process
select * from dms.tbl_MapAction where ActionKey = 1
update dms.tbl_MapAction set WhereCondition = 'dtm >= @WorkDate or dtRTI >= @WorkDate' where ActionKey = 1 and StepKey = 38
select * from dms.tbl_MapSrc2Dsn where ActionKey = 1 and ActionStepKey = 54
update dms.tbl_MapSrc2Dsn set DsnFieldName = 'Versionx' where ActionKey = 1 and ActionStepKey = 39 and FieldKey = 33
--delete from dms.tbl_MapSrc2Dsn where ActionKey = 1 and ActionStepKey = 39 and FieldKey = 38
select top 100 a.*, case when b.IsDeleted = 1 then 1 when a.IsDeleted = 1 then 1 Else 0 end IsDelete
from OrientationChecklistNew a, SF_PatientPreAdmission b where a.PreAdmissionId = b.ID
update dms.tbl_MapAction set WhereCondition = 'ModifiedOn >= @WorkDate or CreatedOn >= @WorkDate' where ActionKey = 1 and StepKey = 39
drop table [pats].[Tbl_OrientationChecklistNew];
CREATE TABLE [pats].[Tbl_OrientationChecklistNew] (
    [SiteCode]                         VARCHAR(25)    NOT NULL,
    [CheckListId]                      INT            NOT NULL,
    [PreAdmissionId]                   INT            NULL,
    [ClientId]                         INT            NULL,
    [DataFormId]                       INT            NULL,
    [PatientComplaints]                BIT            NULL,
    [AccesstoEmergency]                BIT            NULL,
    [CodeofEthics]                     BIT            NULL,
    [ConfidentialityPolicy]            BIT            NULL,
    [Methods]                          BIT            NULL,
    [ExplanationofFiancialObligations] BIT            NULL,
    [RulesforInvoluntaryDetox]         BIT            NULL,
    [FireSafety]                       BIT            NULL,
    [ProgramRulesonPatientParking]     BIT            NULL,
    [PolicyonRestraint]                BIT            NULL,
    [PolicyonTobaccoProducts]          BIT            NULL,
    [PolicyonIllicit]                  BIT            NULL,
    [PolicyonWeapons]                  BIT            NULL,
    [KnowledgeofNames]                 BIT            NULL,
    [ProgramRules]                     BIT            NULL,
    [AIDSHIVPrevention]                BIT            NULL,
    [HepatitisPrevention]              BIT            NULL,
    [PurposeandProcess]                BIT            NULL,
    [IndividualTreatmentPlan]          BIT            NULL,
    [PolicyRegardingUrineDrug]         BIT            NULL,
    [DischargeTransitionCriteria]      BIT            NULL,
    [NaturalProgression]               BIT            NULL,
    [CreatedBy]                        NVARCHAR (100) NULL,
    [CreatedOn]                        DATETIME       NULL,
    [ModifiedBy]                       NVARCHAR (100) NULL,
    [ModifiedOn]                       DATETIME       NULL,
    [IsDeleted]                        BIT            NULL,
    [Version]                          NVARCHAR (100) NULL,
    --[StaffSignature]                   NVARCHAR (MAX) NULL,
    [StaffSignatureDate]               DATETIME       NULL,
    [StaffSignatureBy]                 NVARCHAR (200) NULL,
    --[PatientSignature]                 NVARCHAR (MAX) NULL,
    [PatientSignatureDate]             DATETIME       NULL,
    [PatientSignatureBy]               NVARCHAR (200) NULL,
    [LastModETL]                       DATETIME       NULL,
    CONSTRAINT [PK_Tbl_OrientationChecklistNew] PRIMARY KEY CLUSTERED ([SiteCode] ASC, [CheckListId] ASC)
);