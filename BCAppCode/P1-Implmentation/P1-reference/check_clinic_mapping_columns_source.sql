/*
Run this in the SOURCE SQL Server where the SAMMS-* databases exist.
It compares the clinic Copy activity source needs against dbo.tblClinic for all test sites.

Output:
- MISSING_SOURCE_COLUMN: source base column is absent from that site's dbo.tblClinic.
- CASE_MISMATCH: same source base column exists with different casing. This matters for direct c.* mappings,
  but alias mappings are safe because the source query emits a fixed alias.

Generated from pl_p1_reference.txt cp_clinic_to_bronze mappings.
*/

DECLARE @Sites TABLE (
    SiteCode varchar(25) NOT NULL,
    DatabaseName sysname NOT NULL
);

INSERT INTO @Sites (SiteCode, DatabaseName)
VALUES
    ('B12B', 'SAMMS-ColoradoSpringsV5'),
    ('B24',  'SAMMS-PaintsvilleV5'),
    ('B25',  'SAMMS-PikevilleV5'),
    ('B26',  'SAMMS-HazardV5'),
    ('B28',  'SAMMS-WestPlainesV5');

DECLARE @Expected TABLE (
    MappingSourceColumn sysname NOT NULL,
    BaseSourceColumn sysname NOT NULL,
    SinkColumn sysname NOT NULL
);

INSERT INTO @Expected (MappingSourceColumn, BaseSourceColumn, SinkColumn)
VALUES
    ('PKEY', 'PKEY', 'PKEY'),
    ('DoseWarn', 'DoseWarn', 'DoseWarn'),
    ('DoseStop', 'DoseStop', 'DoseStop'),
    ('Photos', 'Photos', 'Photos'),
    ('Bottles', 'Bottles', 'Bottles'),
    ('Overdue', 'Overdue', 'Overdue'),
    ('BillHold', 'BillHold', 'BillHold'),
    ('Test', 'Test', 'Test'),
    ('TBTest', 'TBTest', 'TBTest'),
    ('Force', 'Force', 'Force'),
    ('LastUpdated', 'LastUpdated', 'LastUpdated'),
    ('Note', 'Note', 'Note'),
    ('Provider', 'Provider', 'Provider'),
    ('Site', 'Site', 'Site'),
    ('CLINICCODE', 'CLINICCODE', 'CLINICCODE'),
    ('DischargeGuest', 'DischargeGuest', 'DischargeGuest'),
    ('NumInventory', 'NumInventory', 'NumInventory'),
    ('SCHEDUA', 'SCHEDUA', 'SCHEDUA'),
    ('UAMONTHLY', 'UAMONTHLY', 'UAMONTHLY'),
    ('ClinicNAME', 'ClinicNAME', 'ClinicNAME'),
    ('TPAUTOMATION', 'TPAUTOMATION', 'TPAUTOMATION'),
    ('RequireDarts', 'RequireDarts', 'RequireDarts'),
    ('PhysicalTestDays', 'PhysicalTestDays', 'PhysicalTestDays'),
    ('ASPhysicalTest', 'ASPhysicalTest', 'ASPhysicalTest'),
    ('ServiceOverlapPopup', 'ServiceOverlapPopup', 'ServiceOverlapPopup'),
    ('OrangeandWhite', 'OrangeandWhite', 'OrangeandWhite'),
    ('ToxProvider', 'ToxProvider', 'ToxProvider'),
    ('NumberofReceipts', 'NumberofReceipts', 'NumberofReceipts'),
    ('PasswordEnforce', 'PasswordEnforce', 'PasswordEnforce'),
    ('PasswordLength', 'PasswordLength', 'PasswordLength'),
    ('helpfile', 'helpfile', 'helpfile'),
    ('ScanPath', 'ScanPath', 'ScanPath'),
    ('DateSigStart', 'DateSigStart', 'DateSigStart'),
    ('ElecSigs', 'ElecSigs', 'ElecSigs'),
    ('CreditPriorWeek', 'CreditPriorWeek', 'CreditPriorWeek'),
    ('DefaultTabOrange', 'DefaultTabOrange', 'DefaultTabOrange'),
    ('BottleWeight', 'BottleWeight', 'BottleWeight'),
    ('spGRAVITY', 'spGRAVITY', 'spGRAVITY'),
    ('DoseCharge', 'DoseCharge', 'DoseCharge'),
    ('TimeOffset', 'TimeOffset', 'TimeOffset'),
    ('CoSign', 'CoSign', 'CoSign'),
    ('DefaultProgram', 'DefaultProgram', 'DefaultProgram'),
    ('ClientSecurity', 'ClientSecurity', 'ClientSecurity'),
    ('AutoCheckin', 'AutoCheckin', 'AutoCheckin'),
    ('CheckinCheck', 'CheckinCheck', 'CheckinCheck'),
    ('OrderService', 'OrderService', 'OrderService'),
    ('Residential', 'Residential', 'Residential'),
    ('BillDirection', 'BillDirection', 'BillDirection'),
    ('SmallReceipts', 'SmallReceipts', 'SmallReceipts'),
    ('DuplicateCheckinCheck', 'DuplicateCheckinCheck', 'DuplicateCheckinCheck'),
    ('NoPrintCheckinLabel', 'NoPrintCheckinLabel', 'NoPrintCheckinLabel'),
    ('AdDomain', 'AdDomain', 'AdDomain'),
    ('AutoSetLABELPrinter', 'AutoSetLABELPrinter', 'AutoSetLABELPrinter'),
    ('AutoSetReceiptPrinter', 'AutoSetReceiptPrinter', 'AutoSetReceiptPrinter'),
    ('ClinicLetter', 'ClinicLetter', 'ClinicLetter'),
    ('ClinicState', 'ClinicState', 'ClinicState'),
    ('Liquid', 'Liquid', 'Liquid'),
    ('OtherInvType', 'OtherInvType', 'OtherInvType'),
    ('PrintDoseAmt', 'PrintDoseAmt', 'PrintDoseAmt'),
    ('Tabs', 'Tabs', 'Tabs'),
    ('DailyServices', 'DailyServices', 'DailyServices'),
    ('clientsearchRIN', 'clientsearchRIN', 'clientsearchRIN'),
    ('ClientServiceBilling', 'ClientServiceBilling', 'ClientServiceBilling'),
    ('DischargeClearsHolds', 'DischargeClearsHolds', 'DischargeClearsHolds'),
    ('DrugFreeOnly', 'DrugFreeOnly', 'DrugFreeOnly'),
    ('halfweekcredit', 'halfweekcredit', 'halfweekcredit'),
    ('AllowRinZero', 'AllowRinZero', 'AllowRinZero'),
    ('AllowAnyRin', 'AllowAnyRin', 'AllowAnyRin'),
    ('DefaultShowHoldAtNursing', 'DefaultShowHoldAtNursing', 'DefaultShowHoldAtNursing'),
    ('HideElecSigDates', 'HideElecSigDates', 'HideElecSigDates'),
    ('QueSearch', 'QueSearch', 'QueSearch'),
    ('EducFieldIsEmpStatus', 'EducFieldIsEmpStatus', 'EducFieldIsEmpStatus'),
    ('AutoImportUA', 'AutoImportUA', 'AutoImportUA'),
    ('FastDose', 'FastDose', 'FastDose'),
    ('RecIDPrint', 'RecIDPrint', 'RecIDPrint'),
    ('NurseSig', 'NurseSig', 'NurseSig'),
    ('ORDER2CONFIRM', 'ORDER2CONFIRM', 'ORDER2CONFIRM'),
    ('ORDERCONFIRM', 'ORDERCONFIRM', 'ORDERCONFIRM'),
    ('ToxACCT', 'ToxACCT', 'ToxACCT'),
    ('spGravityClear', 'spGravityClear', 'spGravityClear'),
    ('toxtixnum', 'toxtixnum', 'toxtixnum'),
    ('toxTixspecial', 'toxTixspecial', 'toxTixspecial'),
    ('AutoOrderExpirationHolds', 'AutoOrderExpirationHolds', 'AutoOrderExpirationHolds'),
    ('reqallintake', 'reqallintake', 'reqallintake'),
    ('NumberOfBulkLabels', 'NumberOfBulkLabels', 'NumberOfBulkLabels'),
    ('UAonVISIT', 'UAonVISIT', 'UAonVISIT'),
    ('Diversion_Padding', 'Diversion_Padding', 'Diversion_Padding'),
    ('WORDPATH', 'WORDPATH', 'WORDPATH'),
    ('ScanDeleteOriginal', 'ScanDeleteOriginal', 'ScanDeleteOriginal'),
    ('UDSpanelRequired', 'UDSpanelRequired', 'UDSpanelRequired'),
    ('AutoDischargeCredit', 'AutoDischargeCredit', 'AutoDischargeCredit'),
    ('BeakerColors', 'BeakerColors', 'BeakerColors'),
    ('NumberPriorTransactionsOnReceipt', 'NumberPriorTransactionsOnReceipt', 'NumberPriorTransactionsOnReceipt'),
    ('AlwaysAllowUseSavedSignature', 'AlwaysAllowUseSavedSignature', 'AlwaysAllowUseSavedSignature'),
    ('NewBottleLabels', 'NewBottleLabels', 'NewBottleLabels'),
    ('DocTemplatePath', 'DocTemplatePath', 'DocTemplatePath'),
    ('ReportDir', 'ReportDir', 'ReportDir'),
    ('reportServer', 'reportServer', 'reportServer'),
    ('DonotallowCASCADE', 'DonotallowCASCADE', 'DonotallowCASCADE'),
    ('isBHG', 'isBHG', 'isBHG'),
    ('MultipleQueues', 'MultipleQueues', 'MultipleQueues'),
    ('LabAcct', 'LabAcct', 'LabAcct'),
    ('AlwaysAskBagLabel', 'AlwaysAskBagLabel', 'AlwaysAskBagLabel'),
    ('PrepackBagLabelDefault', 'PrepackBagLabelDefault', 'PrepackBagLabelDefault'),
    ('DefaultShowHoldFront', 'DefaultShowHoldFront', 'DefaultShowHoldFront'),
    ('ShowFutureUAholds', 'ShowFutureUAholds', 'ShowFutureUAholds'),
    ('OpenOnSunday', 'OpenOnSunday', 'OpenOnSunday'),
    ('ChargeBeforeDose', 'ChargeBeforeDose', 'ChargeBeforeDose'),
    ('LandscapeLabel', 'LandscapeLabel', 'LandscapeLabel'),
    ('sigIMGPATH', 'sigIMGPATH', 'sigIMGPATH'),
    ('sigIMGURI', 'sigIMGURI', 'sigIMGURI'),
    ('SignBeforeDose', 'SignBeforeDose', 'SignBeforeDose'),
    ('SortClientSearchbyID', 'SortClientSearchbyID', 'SortClientSearchbyID'),
    ('UApath', 'UApath', 'UApath'),
    ('AdjustmentEmail', 'AdjustmentEmail', 'AdjustmentEmail'),
    ('Fifo_Bottle', 'Fifo_Bottle', 'Fifo_Bottle'),
    ('UseCostCenter', 'UseCostCenter', 'UseCostCenter'),
    ('ForceCheckin', 'ForceCheckin', 'ForceCheckin'),
    ('VerifyMedAdjustment', 'VerifyMedAdjustment', 'VerifyMedAdjustment'),
    ('PinSigs', 'PinSigs', 'PinSigs'),
    ('COMBINE3PAYFEES', 'COMBINE3PAYFEES', 'COMBINE3PAYFEES'),
    ('_pinbeforesig_mapped', 'pinbeforesig', 'PinBeforeSig'),
    ('siglcd', 'siglcd', 'siglcd'),
    ('DictionaryPath', 'DictionaryPath', 'DictionaryPath'),
    ('grammerpath', 'grammerpath', 'grammerpath'),
    ('DiversionType', 'DiversionType', 'DiversionType'),
    ('ismedmark', 'ismedmark', 'ismedmark'),
    ('ServiceDimsLinkToTP', 'ServiceDimsLinkToTP', 'ServiceDimsLinkToTP'),
    ('advancedtesting', 'advancedtesting', 'advancedtesting'),
    ('AllowActOldOrder', 'AllowActOldOrder', 'AllowActOldOrder'),
    ('FirstInitialonTOXlabel', 'FirstInitialonTOXlabel', 'FirstInitialonTOXlabel'),
    ('Over100check', 'Over100check', 'Over100check'),
    ('NoQuePop', 'NoQuePop', 'NoQuePop'),
    ('offsetdoseconfirm', 'offsetdoseconfirm', 'offsetdoseconfirm'),
    ('OrderRequestsNeedBothSigs', 'OrderRequestsNeedBothSigs', 'OrderRequestsNeedBothSigs'),
    ('SmallTox', 'SmallTox', 'SmallTox'),
    ('toxservice', 'toxservice', 'toxservice'),
    ('Zebra', 'Zebra', 'Zebra'),
    ('FingerPrintSig', 'FingerPrintSig', 'FingerPrintSig'),
    ('VOregistrationpath', 'VOregistrationpath', 'VOregistrationpath'),
    ('blockaptcalhold', 'blockaptcalhold', 'blockaptcalhold'),
    ('CalendarStartTime', 'CalendarStartTime', 'CalendarStartTime'),
    ('EligPW', 'EligPW', 'EligPW'),
    ('EligUN', 'EligUN', 'EligUN'),
    ('FiveDayCalendarWeek', 'FiveDayCalendarWeek', 'FiveDayCalendarWeek'),
    ('multitenant', 'multitenant', 'multitenant'),
    ('pumpwindow', 'pumpwindow', 'pumpwindow'),
    ('RequireEmergencyContact', 'RequireEmergencyContact', 'RequireEmergencyContact'),
    ('QueueTwice', 'QueueTwice', 'QueueTwice'),
    ('CheckUAIsPrescription', 'CheckUAIsPrescription', 'CheckUAIsPrescription'),
    ('enableBusPass', 'enableBusPass', 'enableBusPass'),
    ('ClaimDir', 'ClaimDir', 'ClaimDir'),
    ('isIHC', 'isIHC', 'isIHC'),
    ('Phase', 'Phase', 'Phase'),
    ('setEvalsOtherFocus', 'setEvalsOtherFocus', 'setEvalsOtherFocus'),
    ('EnableHoldayPickupCalifornia', 'EnableHoldayPickupCalifornia', 'EnableHoldayPickupCalifornia'),
    ('ZeroSSNs', 'ZeroSSNs', 'ZeroSSNs'),
    ('EnableTouchSig', 'EnableTouchSig', 'EnableTouchSig'),
    ('CreditDosesDischarge', 'CreditDosesDischarge', 'CreditDosesDischarge'),
    ('AllowBulkDrSigs', 'AllowBulkDrSigs', 'AllowBulkDrSigs'),
    ('EnableAlertsMedChanges', 'EnableAlertsMedChanges', 'EnableAlertsMedChanges'),
    ('EnableOrderAlerts', 'EnableOrderAlerts', 'EnableOrderAlerts'),
    ('EnableTestingAlerts', 'EnableTestingAlerts', 'EnableTestingAlerts'),
    ('EnableAtRiskAlerts', 'EnableAtRiskAlerts', 'EnableAtRiskAlerts'),
    ('EnableAdministeringClientMeds', 'EnableAdministeringClientMeds', 'EnableAdministeringClientMeds'),
    ('DisableServiceUnits', 'DisableServiceUnits', 'DisableServiceUnits'),
    ('isMultiProgram', 'isMultiProgram', 'isMultiProgram'),
    ('versionNbr', 'versionNbr', 'versionNbr'),
    ('LabelPrintMedTypeInsteadOfMedClass', 'LabelPrintMedTypeInsteadOfMedClass', 'LabelPrintMedTypeInsteadOfMedClass'),
    ('EnableEnrollDischargeDateInSearchGrid', 'EnableEnrollDischargeDateInSearchGrid', 'EnableEnrollDischargeDateInSearchGrid'),
    ('EnableServiceRevisions', 'EnableServiceRevisions', 'EnableServiceRevisions'),
    ('enableBAC', 'enableBAC', 'enableBAC'),
    ('SammsFormsDefaultIndexNumber', 'SammsFormsDefaultIndexNumber', 'SammsFormsDefaultIndexNumber'),
    ('enableDriveMapping', 'enableDriveMapping', 'enableDriveMapping'),
    ('destructbottle', 'destructbottle', 'destructbottle'),
    ('dontprintorders', 'dontprintorders', 'dontprintorders'),
    ('DisablePrintServiceMessageAfterSavePrompt', 'DisablePrintServiceMessageAfterSavePrompt', 'DisablePrintServiceMessageAfterSavePrompt'),
    ('DisableOtherAsReferralSource', 'DisableOtherAsReferralSource', 'DisableOtherAsReferralSource'),
    ('EnableInventory4and5', 'EnableInventory4and5', 'EnableInventory4and5'),
    ('SigPadTest', 'SigPadTest', 'SigPadTest'),
    ('EnableRssAlerts', 'EnableRssAlerts', 'EnableRssAlerts'),
    ('iispath', 'iispath', 'iispath'),
    ('PrintAlternativeZebraAndDymoLabelVersion1', 'PrintAlternativeZebraAndDymoLabelVersion1', 'PrintAlternativeZebraAndDymoLabelVersion1'),
    ('isRNP', 'isRNP', 'isRNP'),
    ('EnableAutoPopulateCity', 'EnableAutoPopulateCity', 'EnableAutoPopulateCity'),
    ('IntakePacketURL', 'IntakePacketURL', 'IntakePacketURL'),
    ('NoCheckinatPay', 'NoCheckinatPay', 'NoCheckinatPay'),
    ('EnableAutoHoldOnAbnormalLab', 'EnableAutoHoldOnAbnormalLab', 'EnableAutoHoldOnAbnormalLab'),
    ('MultiQueueRefreshIntervalTimeSet', 'MultiQueueRefreshIntervalTimeSet', 'MultiQueueRefreshIntervalTimeSet'),
    ('EnableSuffixMiddleInitialInFirstNameOfSearch', 'EnableSuffixMiddleInitialInFirstNameOfSearch', 'EnableSuffixMiddleInitialInFirstNameOfSearch'),
    ('enableUserLoginAtBACqueueModeElseInitials', 'enableUserLoginAtBACqueueModeElseInitials', 'enableUserLoginAtBACqueueModeElseInitials'),
    ('SiteID', 'SiteID', 'SiteID'),
    ('PrintSitesAddressDependingOnSites', 'PrintSitesAddressDependingOnSites', 'PrintSitesAddressDependingOnSites'),
    ('DoNotPrintDOE', 'DoNotPrintDOE', 'DoNotPrintDOE'),
    ('enableAutoSpSAMMSBilling', 'enableAutoSpSAMMSBilling', 'enableAutoSpSAMMSBilling'),
    ('enableCounselorSelectionInMultiProgramSectionOnly', 'enableCounselorSelectionInMultiProgramSectionOnly', 'enableCounselorSelectionInMultiProgramSectionOnly'),
    ('URLassessment', 'URLassessment', 'URLassessment'),
    ('DisableTPproblemAndInOwnWords', 'DisableTPproblemAndInOwnWords', 'DisableTPproblemAndInOwnWords'),
    ('enableCustomizableRequirementsForClientInfo', 'enableCustomizableRequirementsForClientInfo', 'enableCustomizableRequirementsForClientInfo'),
    ('enableIntakeDischargeIncomeInputs', 'enableIntakeDischargeIncomeInputs', 'enableIntakeDischargeIncomeInputs'),
    ('enableAutoBillingDuringEachToxPrint', 'enableAutoBillingDuringEachToxPrint', 'enableAutoBillingDuringEachToxPrint'),
    ('isSH', 'isSH', 'isSH'),
    ('enableBACstopDose', 'enableBACstopDose', 'enableBACstopDose'),
    ('enableBACnurseHoldEvenBlowZero', 'enableBACnurseHoldEvenBlowZero', 'enableBACnurseHoldEvenBlowZero'),
    ('EnableSignaturesDuringPillCount', 'EnableSignaturesDuringPillCount', 'EnableSignaturesDuringPillCount'),
    ('EnableSignatureWhenAdministeringMeds', 'EnableSignatureWhenAdministeringMeds', 'EnableSignatureWhenAdministeringMeds'),
    ('CHSAMSID', 'CHSAMSID', 'CHSAMSID'),
    ('FTS', 'FTS', 'FTS'),
    ('Over20', 'Over20', 'Over20'),
    ('_forcefindtype_mapped', 'forcefindtype', 'forcefindtype'),
    ('HnPUrl', 'HnPUrl', 'HnPUrl'),
    ('EnablePrintMedTypeColorOnIDCard', 'EnablePrintMedTypeColorOnIDCard', 'EnablePrintMedTypeColorOnIDCard'),
    ('EnablePortraitLabelDoubleSide', 'EnablePortraitLabelDoubleSide', 'EnablePortraitLabelDoubleSide'),
    ('enableCommentsOnMultiCheckin', 'enableCommentsOnMultiCheckin', 'enableCommentsOnMultiCheckin'),
    ('PullPicsFromDB', 'PullPicsFromDB', 'PullPicsFromDB'),
    ('EnableCompetentCheckBoxAtDosing', 'EnableCompetentCheckBoxAtDosing', 'EnableCompetentCheckBoxAtDosing'),
    ('EnableActivateOrderWhenNotInSuboxoneProg', 'EnableActivateOrderWhenNotInSuboxoneProg', 'EnableActivateOrderWhenNotInSuboxoneProg'),
    ('EnablePrintToxLandscape', 'EnablePrintToxLandscape', 'EnablePrintToxLandscape'),
    ('EnableFlagNurseForBAC', 'EnableFlagNurseForBAC', 'EnableFlagNurseForBAC'),
    ('BottleReturnNote', 'BottleReturnNote', 'BottleReturnNote'),
    ('BHGMarginTH', 'BHGMarginTH', 'BHGMarginTH'),
    ('BHGMarginTox', 'BHGMarginTox', 'BHGMarginTox'),
    ('NoCheckinService', 'NoCheckinService', 'NoCheckinService'),
    ('NoSAMMSFormHeader', 'NoSAMMSFormHeader', 'NoSAMMSFormHeader'),
    ('PrintUnitDoseLabel', 'PrintUnitDoseLabel', 'PrintUnitDoseLabel'),
    ('AuthBasedOnProgram', 'AuthBasedOnProgram', 'AuthBasedOnProgram'),
    ('LandscapeZebra', 'LandscapeZebra', 'LandscapeZebra'),
    ('ShowBalanceAtDispense', 'ShowBalanceAtDispense', 'ShowBalanceAtDispense'),
    ('SingleQueueRefreshIntervalTimeSet', 'SingleQueueRefreshIntervalTimeSet', 'SingleQueueRefreshIntervalTimeSet'),
    ('MultiDosingClinic', 'MultiDosingClinic', 'MultiDosingClinic'),
    ('BlasterWide', 'BlasterWide', 'BlasterWide'),
    ('PumpCalibrate', 'PumpCalibrate', 'PumpCalibrate'),
    ('CheckVisitingPatient', 'CheckVisitingPatient', 'CheckVisitingPatient'),
    ('RequireClientSignatureOrderRequest', 'RequireClientSignatureOrderRequest', 'RequireClientSignatureOrderRequest'),
    ('DischargedAllowAddPayer', 'DischargedAllowAddPayer', 'DischargedAllowAddPayer'),
    ('DymoDetailed', 'DymoDetailed', 'DymoDetailed');

DECLARE @Actual TABLE (
    SiteCode varchar(25) NOT NULL,
    DatabaseName sysname NOT NULL,
    ActualColumn sysname NOT NULL,
    DataType sysname NULL,
    MaxLength smallint NULL,
    NumericPrecision tinyint NULL,
    NumericScale tinyint NULL,
    IsNullable bit NULL
);

DECLARE @sql nvarchar(max) = N'';
DECLARE @SiteCode varchar(25);
DECLARE @DatabaseName sysname;

DECLARE site_cursor CURSOR LOCAL FAST_FORWARD FOR
SELECT SiteCode, DatabaseName
FROM @Sites;

OPEN site_cursor;
FETCH NEXT FROM site_cursor INTO @SiteCode, @DatabaseName;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sql = @sql
        + CASE WHEN LEN(@sql) = 0 THEN N'' ELSE N' UNION ALL ' END
        + N'SELECT '
        + QUOTENAME(@SiteCode, '''') + N' AS SiteCode, '
        + QUOTENAME(@DatabaseName, '''') + N' AS DatabaseName, '
        + N'c.name AS ActualColumn, ty.name AS DataType, c.max_length AS MaxLength, '
        + N'c.precision AS NumericPrecision, c.scale AS NumericScale, c.is_nullable AS IsNullable '
        + N'FROM ' + QUOTENAME(@DatabaseName) + N'.sys.tables t '
        + N'JOIN ' + QUOTENAME(@DatabaseName) + N'.sys.schemas s ON t.schema_id = s.schema_id '
        + N'JOIN ' + QUOTENAME(@DatabaseName) + N'.sys.columns c ON t.object_id = c.object_id '
        + N'JOIN ' + QUOTENAME(@DatabaseName) + N'.sys.types ty ON c.user_type_id = ty.user_type_id '
        + N'WHERE s.name = ''dbo'' AND t.name = ''tblClinic''';

    FETCH NEXT FROM site_cursor INTO @SiteCode, @DatabaseName;
END

CLOSE site_cursor;
DEALLOCATE site_cursor;

INSERT INTO @Actual (SiteCode, DatabaseName, ActualColumn, DataType, MaxLength, NumericPrecision, NumericScale, IsNullable)
EXEC sys.sp_executesql @sql;

;WITH CheckRows AS (
    SELECT
        s.SiteCode,
        s.DatabaseName,
        e.MappingSourceColumn,
        e.BaseSourceColumn,
        e.SinkColumn,
        exact_col.ActualColumn AS ExactColumn,
        ci_col.ActualColumn AS CaseInsensitiveColumn,
        ci_col.DataType,
        ci_col.MaxLength,
        ci_col.NumericPrecision,
        ci_col.NumericScale,
        ci_col.IsNullable
    FROM @Sites s
    CROSS JOIN @Expected e
    OUTER APPLY (
        SELECT TOP (1) a.ActualColumn
        FROM @Actual a
        WHERE a.SiteCode = s.SiteCode
          AND a.ActualColumn COLLATE Latin1_General_BIN2 = e.BaseSourceColumn COLLATE Latin1_General_BIN2
    ) exact_col
    OUTER APPLY (
        SELECT TOP (1) a.ActualColumn, a.DataType, a.MaxLength, a.NumericPrecision, a.NumericScale, a.IsNullable
        FROM @Actual a
        WHERE a.SiteCode = s.SiteCode
          AND LOWER(a.ActualColumn) = LOWER(e.BaseSourceColumn)
        ORDER BY a.ActualColumn
    ) ci_col
)
SELECT
    SiteCode,
    DatabaseName,
    CASE
        WHEN CaseInsensitiveColumn IS NULL THEN 'MISSING_SOURCE_COLUMN'
        WHEN ExactColumn IS NULL THEN 'CASE_MISMATCH'
    END AS IssueType,
    MappingSourceColumn,
    BaseSourceColumn,
    SinkColumn AS MappingSinkColumn,
    CaseInsensitiveColumn AS ActualSourceColumn,
    DataType,
    MaxLength,
    NumericPrecision,
    NumericScale,
    IsNullable
FROM CheckRows
WHERE CaseInsensitiveColumn IS NULL
   OR (ExactColumn IS NULL AND MappingSourceColumn = BaseSourceColumn)
ORDER BY SiteCode, IssueType, BaseSourceColumn;
