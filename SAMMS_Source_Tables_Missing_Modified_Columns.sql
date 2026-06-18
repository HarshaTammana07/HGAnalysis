/*
    SAMMS source tables/views missing Modified Date / Modified By columns

    Run this inside a SAMMS database.

    Output:
      - First result set: count summary
      - Second result set: detail rows
      - One row per source object from the BHGTaskRunner/SAMMS source list
      - Shows whether the object exists
      - Shows detected Modified Date-like columns
      - Shows detected Modified By-like columns
      - Defaults to returning only objects missing either Modified Date or Modified By

    Notes:
      - This checks tables and views: sys.objects type U and V.
      - Matching is intentionally broad to catch spelling/style variations:
        ModifiedOn, ModifiedDate, LastModifiedDate, LastUpdateOn, UpdatedOn,
        UpdateDate, dtModified, modified_by, LastUpdatedBy, UpdatedBy, etc.
      - Set @OnlyMissing = 0 to return every object with detection details.
*/

DECLARE @OnlyMissing bit = 1;

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
ColumnScan AS (
    SELECT
        mo.SchemaName,
        mo.ObjectName,
        mo.ObjectType,
        mo.object_id,
        c.name AS ColumnName,
        NormalizedColumnName = LOWER(
            REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(c.name, '_', ''), '-', ''), ' ', ''), '.', ''), '[', ''), ']', '')
        )
    FROM MatchedObjects mo
    LEFT JOIN sys.columns c
        ON c.object_id = mo.object_id
),
ColumnFlags AS (
    SELECT
        SchemaName,
        ObjectName,
        ObjectType,
        object_id,
        ColumnName,
        IsModifiedDateColumn =
            CASE
                WHEN ColumnName IS NULL THEN 0
                WHEN NormalizedColumnName IN (
                    'modifieddate', 'modifieddatetime', 'modifieddt', 'modifiedon',
                    'lastmodifieddate', 'lastmodifieddatetime', 'lastmodifieddt', 'lastmodifiedon',
                    'updateddate', 'updateddatetime', 'updateddt', 'updatedon',
                    'lastupdateddate', 'lastupdateddatetime', 'lastupdateddt', 'lastupdatedon',
                    'lastupdate', 'lastupdateon', 'lastupdated',
                    'modifydate', 'modifydatetime', 'modifydt', 'modifyon',
                    'moddate', 'moddatetime', 'moddt',
                    'dtmodified', 'datemodified', 'datemodifiedon',
                    'dtupdated', 'dateupdated'
                ) THEN 1
                WHEN NormalizedColumnName LIKE '%modified%date%' THEN 1
                WHEN NormalizedColumnName LIKE '%modified%dt%' THEN 1
                WHEN NormalizedColumnName LIKE '%modified%on%' THEN 1
                WHEN NormalizedColumnName LIKE '%modify%date%' THEN 1
                WHEN NormalizedColumnName LIKE '%modify%dt%' THEN 1
                WHEN NormalizedColumnName LIKE '%modify%on%' THEN 1
                WHEN NormalizedColumnName LIKE '%modifed%date%' THEN 1 -- common misspelling
                WHEN NormalizedColumnName LIKE '%modifed%dt%' THEN 1
                WHEN NormalizedColumnName LIKE '%modifed%on%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastupdate%date%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastupdate%dt%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastupdate%on%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastupdated%' THEN 1
                WHEN NormalizedColumnName LIKE '%updated%date%' THEN 1
                WHEN NormalizedColumnName LIKE '%updated%dt%' THEN 1
                WHEN NormalizedColumnName LIKE '%updated%on%' THEN 1
                WHEN NormalizedColumnName LIKE '%update%date%' THEN 1
                WHEN NormalizedColumnName LIKE '%update%dt%' THEN 1
                WHEN NormalizedColumnName LIKE '%update%on%' THEN 1
                ELSE 0
            END,
        IsModifiedByColumn =
            CASE
                WHEN ColumnName IS NULL THEN 0
                WHEN NormalizedColumnName IN (
                    'modifiedby', 'lastmodifiedby',
                    'updatedby', 'lastupdatedby',
                    'modifyby', 'modby',
                    'userupdated', 'updateduser',
                    'usermodified', 'modifieduser',
                    'lastmodby', 'lastmoduser'
                ) THEN 1
                WHEN NormalizedColumnName LIKE '%modified%by%' THEN 1
                WHEN NormalizedColumnName LIKE '%modify%by%' THEN 1
                WHEN NormalizedColumnName LIKE '%modifed%by%' THEN 1 -- common misspelling
                WHEN NormalizedColumnName LIKE '%updated%by%' THEN 1
                WHEN NormalizedColumnName LIKE '%update%by%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastupdate%by%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastmodified%user%' THEN 1
                WHEN NormalizedColumnName LIKE '%modified%user%' THEN 1
                WHEN NormalizedColumnName LIKE '%lastupdated%user%' THEN 1
                WHEN NormalizedColumnName LIKE '%updated%user%' THEN 1
                ELSE 0
            END
    FROM ColumnScan
),
ObjectSummary AS (
    SELECT
        SchemaName,
        ObjectName,
        ObjectFullName = SchemaName + '.' + ObjectName,
        ObjectType = MAX(ObjectType),
        ExistsInDatabase = CASE WHEN MAX(object_id) IS NULL THEN 0 ELSE 1 END,
        HasModifiedDateColumn = MAX(IsModifiedDateColumn),
        HasModifiedByColumn = MAX(IsModifiedByColumn),
        ModifiedDateColumns =
            STUFF((
                SELECT ', ' + cf2.ColumnName
                FROM ColumnFlags cf2
                WHERE cf2.SchemaName = cf.SchemaName
                  AND cf2.ObjectName = cf.ObjectName
                  AND cf2.IsModifiedDateColumn = 1
                ORDER BY cf2.ColumnName
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, ''),
        ModifiedByColumns =
            STUFF((
                SELECT ', ' + cf2.ColumnName
                FROM ColumnFlags cf2
                WHERE cf2.SchemaName = cf.SchemaName
                  AND cf2.ObjectName = cf.ObjectName
                  AND cf2.IsModifiedByColumn = 1
                ORDER BY cf2.ColumnName
                FOR XML PATH(''), TYPE
            ).value('.', 'nvarchar(max)'), 1, 2, '')
    FROM ColumnFlags cf
    GROUP BY
        SchemaName,
        ObjectName
)
SELECT
    TotalSourceObjects = COUNT(1),
    ObjectsFoundInDatabase = SUM(CASE WHEN ExistsInDatabase = 1 THEN 1 ELSE 0 END),
    ObjectsNotFoundInDatabase = SUM(CASE WHEN ExistsInDatabase = 0 THEN 1 ELSE 0 END),
    TablesWithModifiedDateKindColumn = SUM(CASE WHEN ExistsInDatabase = 1 AND HasModifiedDateColumn = 1 THEN 1 ELSE 0 END),
    TablesWithoutModifiedDateKindColumn = SUM(CASE WHEN ExistsInDatabase = 1 AND HasModifiedDateColumn = 0 THEN 1 ELSE 0 END),
    TablesWithModifiedByKindColumn = SUM(CASE WHEN ExistsInDatabase = 1 AND HasModifiedByColumn = 1 THEN 1 ELSE 0 END),
    TablesWithoutModifiedByKindColumn = SUM(CASE WHEN ExistsInDatabase = 1 AND HasModifiedByColumn = 0 THEN 1 ELSE 0 END),
    TablesWithBothModifiedDateAndModifiedByKindColumns =
        SUM(CASE WHEN ExistsInDatabase = 1 AND HasModifiedDateColumn = 1 AND HasModifiedByColumn = 1 THEN 1 ELSE 0 END),
    TablesMissingEitherModifiedDateOrModifiedByKindColumn =
        SUM(CASE WHEN ExistsInDatabase = 1 AND (HasModifiedDateColumn = 0 OR HasModifiedByColumn = 0) THEN 1 ELSE 0 END)
FROM ObjectSummary;

SELECT
    ObjectFullName,
    ObjectType,
    ExistsInDatabase,
    HasModifiedDateColumn,
    ModifiedDateColumns = ISNULL(ModifiedDateColumns, ''),
    HasModifiedByColumn,
    ModifiedByColumns = ISNULL(ModifiedByColumns, ''),
    MissingReason =
        CASE
            WHEN ExistsInDatabase = 0 THEN 'Object not found in this SAMMS database'
            WHEN HasModifiedDateColumn = 0 AND HasModifiedByColumn = 0 THEN 'Missing Modified Date and Modified By'
            WHEN HasModifiedDateColumn = 0 THEN 'Missing Modified Date'
            WHEN HasModifiedByColumn = 0 THEN 'Missing Modified By'
            ELSE ''
        END
FROM ObjectSummary
WHERE
    @OnlyMissing = 0
    OR ExistsInDatabase = 0
    OR HasModifiedDateColumn = 0
    OR HasModifiedByColumn = 0
ORDER BY
    ExistsInDatabase,
    ObjectFullName;
