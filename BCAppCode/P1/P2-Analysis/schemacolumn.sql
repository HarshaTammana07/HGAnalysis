SELECT

    c.TABLE_SCHEMA                          AS SchemaName,

    c.TABLE_NAME                            AS TableName,

    c.COLUMN_NAME                           AS ColumnName,

    c.ORDINAL_POSITION                      AS OrdinalPosition,

    c.DATA_TYPE                             AS DataType,

    c.CHARACTER_MAXIMUM_LENGTH              AS MaxLength,

    c.NUMERIC_PRECISION                     AS NumericPrecision,

    c.NUMERIC_SCALE                         AS NumericScale,

    c.IS_NULLABLE                           AS IsNullable,

    c.COLUMN_DEFAULT                        AS ColumnDefault

FROM INFORMATION_SCHEMA.COLUMNS c

WHERE

    (c.TABLE_SCHEMA + '.' + c.TABLE_NAME) IN (

        -- ctrl schema

        'ctrl.tbl_3PSETUP',

        'ctrl.tbl_Clinic',

        'ctrl.tbl_DroDownListItems',

        -- ayx schema

        'ayx.tbl_PreAdmission_V6',

        -- stg schema

        'stg.ClientDemo',

        -- pats schema (53 tables)

        'pats.tbl_3pElig',

        'pats.tbl_Admissionassessmentsubstanceusehistory',

        'pats.tbl_AppointmentAttend',

        'pats.tbl_BAMForm',

        'pats.tbl_BAMScore',

        'pats.tbl_Bills',

        'pats.tbl_CheckIn',

        'pats.tbl_Codes',

        'pats.tbl_ComprehensiveAssessmentForm',

        'pats.tbl_consenttomarketing',

        'pats.tbl_Cows_V6',

        'pats.tbl_CustomAnswers',

        'pats.tbl_CustomQuestions',

        'pats.tbl_EandMFormPregnancy',

        'pats.tbl_Enrollment',

        'pats.tbl_FinancialHardshipApplication',

        'pats.tbl_Fmp',

        'pats.tbl_MNComprehensiveAssessment',

        'pats.tbl_MNComprehensiveAssessmentLevelOfCare',

        'pats.tbl_mntreatmentservicereview',

        'pats.tbl_NewAdmissionassessment',

        'pats.tbl_NewAdmissionAssessmentASAMDimension6',

        'pats.tbl_newdischargetransferplanform',

        'pats.tbl_NewPeriodicReassessment',

        'pats.tbl_NewPeriodicReassessmentCounselorReview',

        'pats.tbl_newperiodicreassessmentd2',

        'pats.tbl_newperiodicreassessmentd3',

        'pats.tbl_newperiodicreassessmentd4',

        'pats.tbl_newperiodicreassessmentd5',

        'pats.tbl_newperiodicreassessmentd6',

        'pats.tbl_PA',

        'pats.tbl_PACounselorReview',

        'pats.tbl_PADimension1',

        'pats.tbl_PADimension2',

        'pats.tbl_PADimension3',

        'pats.tbl_PADimension4',

        'pats.tbl_PADimension5',

        'pats.tbl_PADimension6',

        'pats.tbl_PayerCltHistory',

        'pats.tbl_pbi3PayAuth',

        'pats.tbl_PreadmissionReferralSource',

        'pats.tbl_SERVICES',

        'pats.tbl_SF_DataForms',

        'pats.tbl_SMSTextConsentForm',

        'pats.tbl_takehomeagreementanddiversioncontrol',

        'pats.tbl_TakeHomeRiskAssessment',

        'pats.tbl_TblDiag10',

        'pats.tbl_UAResults',

        'pats.tbl_UASched',

        'pats.tbl_VAComprehensiveAssessment',

        'pats.tbl_vacomprehensiveassessmentsummary',

        'pats.tbl_vw3pBillSub'

    )

ORDER BY

    c.TABLE_SCHEMA,

    c.TABLE_NAME,

    c.ORDINAL_POSITION;
 
 
SELECT

    c.TABLE_SCHEMA                          AS SchemaName,

    c.TABLE_NAME                            AS TableName,

    c.COLUMN_NAME                           AS ColumnName,

    c.ORDINAL_POSITION                      AS OrdinalPosition,

    c.DATA_TYPE                             AS DataType,

    c.CHARACTER_MAXIMUM_LENGTH              AS MaxLength,

    c.NUMERIC_PRECISION                     AS NumericPrecision,

    c.NUMERIC_SCALE                         AS NumericScale,

    c.IS_NULLABLE                           AS IsNullable,

    c.COLUMN_DEFAULT                        AS ColumnDefault

FROM INFORMATION_SCHEMA.COLUMNS c

WHERE

    (c.TABLE_SCHEMA + '.' + c.TABLE_NAME) IN (

        'pats.tbl_Bills',

        'pats.tbl_CheckIn',

        'pats.tbl_ClaimLineItem',

        'pats.tbl_ClaimLineItemActivity',

        'pats.tbl_Claims',

        'pats.tbl_EandMFormMDM',

        'pats.tbl_EandMFormPregnancy',

        'pats.tbl_Enrollment',

        'pats.tbl_FeeSched',

        'pats.tbl_GlobalPayor',

        'pats.tbl_NewAdmissionAssessmentASAMDimension2',

        'pats.tbl_NewAdmissionAssessmentASAMDimension4',

        'pats.tbl_NewAdmissionAssessmentASAMDimension5',

        'pats.tbl_PayerClient',

        'pats.tbl_PayerCltHistory',

        'pats.tbl_TreatmentLevel',

        'pats.tbl_UAResultDetail'

    )

ORDER BY

    c.TABLE_SCHEMA,

    c.TABLE_NAME,

    c.ORDINAL_POSITION;
 