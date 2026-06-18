/*
    Lists SAMMS source tables/views that do NOT have a Modified Date-like column.

    Run inside a SAMMS database.

    This checks broad date-column naming patterns:
      ModifiedOn, ModifiedDate, ModifiedDt, LastModifiedOn,
      LastUpdateOn, LastUpdatedDate, UpdatedOn, UpdateDate,
      dtModified, DateModified, dtUpdated, DateUpdated,
      plus common misspelling "modifed".
*/

WITH SourceObjects AS (
    SELECT *
    FROM (VALUES
        ('dbo', 'aaBriefAddictionMonitor'),
        ('dbo', 'aaClinicalOpiateWithdrawalScale'),
        ('dbo', 'tblClaimStatus'),
        ('dbo', 'tblCONSENTS'),
        ('dbo', 'tblDEVICE'),
        ('dbo', 'tblFEESCHED'),
        ('dbo', 'tblFORMSSAMMSCLIENT'),
        ('dbo', 'tblPAYER'),
        ('dbo', 'tbluser'),
        ('dbo', 'tblUserSites'),
        ('dbo', 'admissionassessmentsubstanceusehistory'),
        ('dbo', 'AppointmentAttend'),
        ('dbo', 'BAMForm'),
        ('dbo', 'BAMScore'),
        ('dbo', 'ComprehensiveAssessmentForm'),
        ('dbo', 'consenttomarketing'),
        ('dbo', 'DroDownListItems'),
        ('dbo', 'EandMFormPregnancy'),
        ('dbo', 'FinancialHardshipApplication'),
        ('dbo', 'MNComprehensiveAssessment'),
        ('dbo', 'MNComprehensiveAssessmentLevelOfCare'),
        ('dbo', 'mntreatmentservicereview'),
        ('dbo', 'NewAdmissionAssessment'),
        ('dbo', 'NewAdmissionAssessmentASAMDimension2'),
        ('dbo', 'NewAdmissionAssessmentASAMDimension4'),
        ('dbo', 'NewAdmissionAssessmentASAMDimension5'),
        ('dbo', 'NewAdmissionAssessmentASAMDimension6'),
        ('dbo', 'newdischargetransferplanform'),
        ('dbo', 'NewPeriodicReassessment'),
        ('dbo', 'NewPeriodicReassessmentCounselorReview'),
        ('dbo', 'newperiodicreassessmentd2'),
        ('dbo', 'newperiodicreassessmentd3'),
        ('dbo', 'newperiodicreassessmentd4'),
        ('dbo', 'newperiodicreassessmentd5'),
        ('dbo', 'newperiodicreassessmentd6'),
        ('dbo', 'PACounselorReview'),
        ('dbo', 'PADimension1'),
        ('dbo', 'PADimension2'),
        ('dbo', 'PADimension3'),
        ('dbo', 'PADimension4'),
        ('dbo', 'PADimension5'),
        ('dbo', 'PADimension6'),
        ('dbo', 'PeriodicReassessment'),
        ('dbo', 'SF_COWS'),
        ('dbo', 'SF_DataForms'),
        ('dbo', 'SF_PatientPreAdmission'),
        ('dbo', 'SF_PatientPreadmissionReferralSource'),
        ('dbo', 'SMSTextConsentForm'),
        ('dbo', 'takehomeagreementanddiversioncontrol'),
        ('dbo', 'TakeHomeRiskAssessment'),
        ('dbo', 'tbl3PAYauth'),
        ('dbo', 'Tbl3pElig'),
        ('dbo', 'tbl3PSETUP'),
        ('dbo', 'tblBill'),
        ('dbo', 'tblCHECKIN'),
        ('dbo', 'tblclient'),
        ('dbo', 'tblClinic'),
        ('dbo', 'tblCodes'),
        ('dbo', 'tblCUSTOMANSWERS'),
        ('dbo', 'tblCUSTOMQUESTIONS'),
        ('dbo', 'Tbldiag10'),
        ('dbo', 'tblENROLL'),
        ('dbo', 'tblFMP'),
        ('dbo', 'tblPayerCltHistory'),
        ('dbo', 'tblSERVICES'),
        ('dbo', 'tblTreatmentLevel'),
        ('dbo', 'tblUAResult'),
        ('dbo', 'tblUASched'),
        ('dbo', 'vw3pBillSub'),
        ('dbo', 'EandMForm'),
        ('dbo', 'tbl3pClaim'),
        ('dbo', 'tbl3pClaimLineItem'),
        ('dbo', 'tbl3pClaimLineItemActivity'),
        ('dbo', 'tblPayerClt'),
        ('dbo', 'tblUAResultDetail'),
        ('dbo', 'Form'),
        ('dbo', 'AnswerSignature'),
        ('dbo', 'tbl3pArnote'),
        ('dbo', 'tbl3pClaimNote'),
        ('dbo', 'AdmissionAssessment'),
        ('dbo', 'AdmissionAssessmentDimensionFiveSubstanceUse'),
        ('dbo', 'AdmissionAssessmentDimensionFour'),
        ('dbo', 'AdmissionAssessmentDimensionOneDisorder'),
        ('dbo', 'AdmissionAssessmentDimensionSix'),
        ('dbo', 'AdmissionAssessmentDimensionThree'),
        ('dbo', 'AdmissionAssessmentDimensionTwo'),
        ('dbo', 'AdmissionAssessmentSummary'),
        ('dbo', 'Appointments'),
        ('dbo', 'OrientationChecklistNew'),
        ('dbo', 'ReAssessment'),
        ('dbo', 'ReAssessmentFamily'),
        ('dbo', 'ReAssessmentLegal'),
        ('dbo', 'ReAssessmentMentalHealth'),
        ('dbo', 'ReAssessmentOccupational'),
        ('dbo', 'ReAssessmentPhysicalHealth'),
        ('dbo', 'ReAssessmentSocial'),
        ('dbo', 'ReAssessmentSubstanceUse'),
        ('dbo', 'ReAssessmentTreatment'),
        ('dbo', 'tblBottle'),
        ('dbo', 'tblINVTYPE'),
        ('dbo', 'tblLABRESULT'),
        ('dbo', 'tblLABRESULTDETAIL'),
        ('dbo', 'tblLiquidLog'),
        ('dbo', 'tblDartsSrv'),
        ('dbo', 'tblDose'),
        ('dbo', 'tblDOSE_Excuse'),
        ('dbo', 'tblOrder'),
        ('dbo', 'tblClient'),
        ('dbo', 'VAComprehensiveAssessment'),
        ('dbo', 'vacomprehensiveassessmentsummary')
    ) v(SchemaName, ObjectName)
),
MatchedObjects AS (
    SELECT
        so.SchemaName,
        so.ObjectName,
        ObjectFullName = so.SchemaName + '.' + so.ObjectName,
        o.object_id,
        ObjectType =
            CASE o.type
                WHEN 'U' THEN 'TABLE'
                WHEN 'V' THEN 'VIEW'
                ELSE o.type_desc
            END
    FROM SourceObjects so
    LEFT JOIN sys.schemas s
        ON s.name = so.SchemaName
    LEFT JOIN sys.objects o
        ON o.schema_id = s.schema_id
       AND o.name = so.ObjectName
       AND o.type IN ('U', 'V')
),
DateColumnMatches AS (
    SELECT
        mo.ObjectFullName,
        ModifiedDateColumns =
            STUFF((
                SELECT ', ' + c.name
                FROM sys.columns c
                CROSS APPLY (
                    SELECT NormalizedColumnName = LOWER(
                        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(c.name, '_', ''), '-', ''), ' ', ''), '.', ''), '[', ''), ']', '')
                    )
                ) n
                WHERE c.object_id = mo.object_id
                  AND (
                      n.NormalizedColumnName IN (
                          'modifieddate', 'modifieddatetime', 'modifieddt', 'modifiedon',
                          'lastmodifieddate', 'lastmodifieddatetime', 'lastmodifieddt', 'lastmodifiedon',
                          'updateddate', 'updateddatetime', 'updateddt', 'updatedon',
                          'lastupdateddate', 'lastupdateddatetime', 'lastupdateddt', 'lastupdatedon',
                          'lastupdate', 'lastupdateon', 'lastupdated',
                          'modifydate', 'modifydatetime', 'modifydt', 'modifyon',
                          'moddate', 'moddatetime', 'moddt',
                          'dtmodified', 'datemodified', 'datemodifiedon',
                          'dtupdated', 'dateupdated'
                      )
                      OR n.NormalizedColumnName LIKE '%modified%date%'
                      OR n.NormalizedColumnName LIKE '%modified%dt%'
                      OR n.NormalizedColumnName LIKE '%modified%on%'
                      OR n.NormalizedColumnName LIKE '%modify%date%'
                      OR n.NormalizedColumnName LIKE '%modify%dt%'
                      OR n.NormalizedColumnName LIKE '%modify%on%'
                      OR n.NormalizedColumnName LIKE '%modifed%date%'
                      OR n.NormalizedColumnName LIKE '%modifed%dt%'
                      OR n.NormalizedColumnName LIKE '%modifed%on%'
                      OR n.NormalizedColumnName LIKE '%lastupdate%date%'
                      OR n.NormalizedColumnName LIKE '%lastupdate%dt%'
                      OR n.NormalizedColumnName LIKE '%lastupdate%on%'
                      OR n.NormalizedColumnName LIKE '%lastupdated%'
                      OR n.NormalizedColumnName LIKE '%updated%date%'
                      OR n.NormalizedColumnName LIKE '%updated%dt%'
                      OR n.NormalizedColumnName LIKE '%updated%on%'
                      OR n.NormalizedColumnName LIKE '%update%date%'
                      OR n.NormalizedColumnName LIKE '%update%dt%'
                      OR n.NormalizedColumnName LIKE '%update%on%'
                  )
                ORDER BY c.name
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, '')
    FROM MatchedObjects mo
    WHERE mo.object_id IS NOT NULL
)
SELECT
    mo.ObjectFullName
FROM MatchedObjects mo
LEFT JOIN DateColumnMatches dcm
    ON dcm.ObjectFullName = mo.ObjectFullName
WHERE mo.object_id IS NOT NULL
  AND ISNULL(dcm.ModifiedDateColumns, '') = ''
ORDER BY
    mo.ObjectFullName;

