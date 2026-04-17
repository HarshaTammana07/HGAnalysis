BHG-DR-LIB — Complete Save Services Documentation
All Save*.cs Files, Methods, and Purposes
________________________________________

1. Document Purpose

This document lists every Save service file in BCAppCode/BHG-DR-LIB.
For each file it covers:
- The file name and full path
- Every public method it contains
- The method signature
- What that method does
- What destination table it writes to

Total: 30 Save*.cs files containing approximately 110 public methods.

________________________________________

2. Summary Table — All Files at a Glance

#    File                        Methods    Category
--   ----                        -------    --------
1    SaveDartsSrvs.cs            10         Counseling Sessions (year-split)
2    SaveOrders.cs               14         Prescription Orders (year-split)
3    SaveBills.cs                3          Billing Records
4    SaveClaims.cs               4          Insurance Claims
5    SaveAuths.cs                2          Authorizations
6    Save3pElig.cs               4          3rd Party Eligibility & Notes
7    SaveCleints.cs              4          Client Demographics
8    SaveDoses.cs                2          Medication / Dose Records
9    SaveUAResults.cs            4          UA and Lab Results
10   SaveAssessments.cs          19         Admission & Reassessment Dimensions
11   SavePreAdmissionV6.cs       2          Pre-Admission (V6 schema)
12   SavePAData.cs               10         Pre-Admission Detailed Dimensions
13   SaveGlobal.cs               13         Global / Reference Data
14   SaveGlobalorg.cs            6          Global Org Data (alternate version)
15   SaveGlobal-old.cs           5          Global Data (legacy/reference only)
16   SaveCodes.cs                2          Service Codes
17   SaveClinic.cs               1          Clinic Master Data
18   SaveInventory.cs            4          Inventory (Bottles, Liquid, Types)
19   SaveFormQAData.cs           5          Form Question Answers & E&M Forms
20   SaveCustomQA.cs             2          Custom Questions & Answers
21   SaveCows.cs                 1          COWS Clinical Scale (V6)
22   SaveCA.cs                   8          Clinical Assessments (MN/VA)
23   SaveBAM.cs                  2          Brief Addiction Monitor
24   SaveEnrollment.cs           1          Patient Enrollment
25   SaveCheckIn.cs              1          Daily Check-In Records
26   SaveTreatmentLevel.cs       1          Treatment Level / LOC
27   SaveAppointments.cs         2          Appointments & Attendance
28   SavePayorClient.cs          3          Payer / Insurance Client Records
29   SaveRowTrax.cs              1          Row Count Audit Trail
30   SaveFmp.cs                  1          Financial Management Platform Data

________________________________________

3. Detailed Method Reference — File by File

________________________________________
3.1  SaveDartsSrvs.cs
Path: BCAppCode/BHG-DR-LIB/SaveDartsSrvs.cs
Lines: 1,691
Category: Counseling Sessions (DartsSrv) — year-partitioned EF Core upsert path
Schedule: Used by BHGTaskRunner.exe arg=9 (SAMMS-ETL-DartSvc) via SelectConstructor

What this file does:
Contains 10 public methods, one per destination year table. Each method performs an EF Core
row-by-row upsert into its year-specific Azure table. Change detection uses RowChkSum.
Used for historical backfills and year-specific loads. The daily incremental path uses
BulkDartsSvc instead.

Method 1:  SaveDartSrv2014
Signature: public bool SaveDartSrv2014(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2014B4
Purpose:   EF Core upsert for counseling sessions dated 2008–2014

Method 2:  SaveDartSrv2015
Signature: public bool SaveDartSrv2015(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2015
Purpose:   EF Core upsert for 2015 counseling sessions

Method 3:  SaveDartSrv2016
Signature: public bool SaveDartSrv2016(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2016
Purpose:   EF Core upsert for 2016 counseling sessions

Method 4:  SaveDartSrv2017
Signature: public bool SaveDartSrv2017(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2017
Purpose:   EF Core upsert for 2017 counseling sessions

Method 5:  SaveDartSrv2018
Signature: public bool SaveDartSrv2018(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2018
Purpose:   EF Core upsert for 2018 counseling sessions

Method 6:  SaveDartSrv2019
Signature: public bool SaveDartSrv2019(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2019
Purpose:   EF Core upsert for 2019 counseling sessions

Method 7:  SaveDartSrv2020
Signature: public bool SaveDartSrv2020(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2020
Purpose:   EF Core upsert for 2020 counseling sessions

Method 8:  SaveDartSrv2021
Signature: public bool SaveDartSrv2021(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2021
Purpose:   EF Core upsert for 2021 counseling sessions

Method 9:  SaveDartSrv2022
Signature: public bool SaveDartSrv2022(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2022
Purpose:   EF Core upsert for 2022 counseling sessions

Method 10: SaveDartSrv2023
Signature: public bool SaveDartSrv2023(DataTable tbl, string sc, long akey, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_DartsSrv_2023
Purpose:   EF Core upsert for 2023 counseling sessions

________________________________________
3.2  SaveOrders.cs
Path: BCAppCode/BHG-DR-LIB/SaveOrders.cs
Category: Prescription Orders — year-partitioned EF Core upsert path
Schedule: BHGTaskRunner.exe arg=11 (SAMMS-ETL-Orders)

What this file does:
Same year-split pattern as SaveDartsSrvs. One method per year. Used for backfill only.
Daily path uses BulkDartsSvc (BulkDartsSrvLoader routes through generic bulk path).

Method 1:  SaveOrders
Signature: public bool SaveOrders(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders (base table — pre-2016 records)
Purpose:   EF Core upsert for orders with no specific year filter

Method 2:  SaveOrders2016
Signature: public bool SaveOrders2016(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2016
Purpose:   EF Core upsert for 2016 prescription orders

Method 3:  SaveOrders2017
Signature: public bool SaveOrders2017(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2017
Purpose:   EF Core upsert for 2017 prescription orders

Method 4:  SaveOrders2018
Signature: public bool SaveOrders2018(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2018
Purpose:   EF Core upsert for 2018 prescription orders

Method 5:  SaveOrders2019
Signature: public bool SaveOrders2019(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2019
Purpose:   EF Core upsert for 2019 prescription orders

Method 6:  SaveOrders2020
Signature: public bool SaveOrders2020(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2020
Purpose:   EF Core upsert for 2020 prescription orders

Method 7:  SaveOrders2021
Signature: public bool SaveOrders2021(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2021
Purpose:   EF Core upsert for 2021 prescription orders

Method 8:  SaveOrders2022
Signature: public bool SaveOrders2022(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2022
Purpose:   EF Core upsert for 2022 prescription orders

Method 9:  SaveOrders2023
Signature: public bool SaveOrders2023(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2023
Purpose:   EF Core upsert for 2023 prescription orders

Method 10: SaveOrders2024
Signature: public bool SaveOrders2024(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2024
Purpose:   EF Core upsert for 2024 prescription orders

Method 11: SaveOrders2025
Signature: public bool SaveOrders2025(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2025
Purpose:   EF Core upsert for 2025 prescription orders

Method 12: SaveOrders2026
Signature: public bool SaveOrders2026(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2026
Purpose:   EF Core upsert for 2026 prescription orders

Method 13: SaveOrders2027
Signature: public bool SaveOrders2027(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2027
Purpose:   EF Core upsert for 2027 prescription orders

Method 14: SaveOrders2028
Signature: public bool SaveOrders2028(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_Orders_2028
Purpose:   EF Core upsert for 2028 prescription orders (future-proofed)

________________________________________
3.3  SaveBills.cs
Path: BCAppCode/BHG-DR-LIB/SaveBills.cs
Category: Billing Records
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Handles billing records from SAMMS. Two overloads of SaveBills for different calling patterns.
SaveAuthBills handles authorization-linked billing separately.

Method 1:  SaveBills (DataTable overload)
Signature: public RCodes SaveBills(DataTable tbl, string sc, DateTime wrkdt, int DaysBack, BHG_DRContext db)
Writes to: pats.tbl_Bills
Purpose:   EF Core upsert of billing records from a pre-loaded DataTable.
           DaysBack controls how far back to look for existing Azure rows.

Method 2:  SaveBills (Source command overload)
Signature: public RCodes SaveBills(string SrcCmd, string SrcCon, string sc, DateTime wrkdt, bool yearly)
Writes to: pats.tbl_Bills
Purpose:   Self-contained overload — takes a source SQL command and connection string,
           queries the source itself, then saves. Used when data is not pre-fetched.

Method 3:  SaveAuthBills
Signature: public RCodes SaveAuthBills(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_AuthBills (authorization-linked bill records)
Purpose:   Saves billing records that are specifically tied to insurance authorizations.
           Separate from standard bills because they carry auth reference IDs.

________________________________________
3.4  SaveClaims.cs
Path: BCAppCode/BHG-DR-LIB/SaveClaims.cs
Category: Insurance Claims
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Handles the 3-tier claims structure: claim header → line items → line item activity.
Each level has its own method and destination table.

Method 1:  SaveClaims
Signature: public RCodes SaveClaims(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_Claims
Purpose:   Saves the claim header records — one row per submitted insurance claim.

Method 2:  SaveClaimLineItem
Signature: public RCodes SaveClaimLineItem(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_ClaimLineItem
Purpose:   Saves claim line items — one row per service line within a claim.

Method 3:  SaveClaimLineItemActivity
Signature: public RCodes SaveClaimLineItemActivity(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_ClaimLineItemActivity
Purpose:   Saves claim line item activity — payment, adjustment, and denial events
           for each claim line item.

Method 4:  CleanupDeletedData
Signature: public bool CleanupDeletedData(DataTable tbl, string sc, string tblName, BHG_DRContext db)
Writes to: Varies (tblName is passed as parameter)
Purpose:   Removes rows from the destination table that were deleted at the source.
           Used to keep destination in sync when records are voided or deleted in SAMMS.

________________________________________
3.5  SaveAuths.cs
Path: BCAppCode/BHG-DR-LIB/SaveAuths.cs
Category: Prior Authorizations
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Handles insurance prior authorization records and their associated billing submissions.

Method 1:  SaveAuths
Signature: public RCodes SaveAuths(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_Auths
Purpose:   Saves prior authorization records — payer-issued approval numbers,
           service limits, effective dates.

Method 2:  SaveAuthBillsub
Signature: public RCodes SaveAuthBillsub(DataTable tbl, string sc, DateTime WrkDate, bool Reload, BHG_DRContext db)
Writes to: pats.tbl_vw3pBillSub (3rd party bill submission records)
Purpose:   Saves authorization bill submission records linking auth approvals
           to actual bill submissions. Reload flag triggers full reload for this site.

________________________________________
3.6  Save3pElig.cs
Path: BCAppCode/BHG-DR-LIB/Save3pElig.cs
Category: 3rd Party Eligibility and Notes
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Handles 3rd party (insurance) eligibility setup, claim notes, and AR notes.

Method 1:  Save3pElig
Signature: public RCodes Save3pElig(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_3pElig
Purpose:   Saves 3rd party eligibility records — insurance coverage verification results
           per patient per payer.

Method 2:  Save3pSetup
Signature: public RCodes Save3pSetup(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_3pSetup
Purpose:   Saves 3rd party payer setup configuration records for each site.

Method 3:  Save3pClaimNote
Signature: public RCodes Save3pClaimNote(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_3pClaimNote
Purpose:   Saves notes attached to specific claims in the 3rd party billing system.

Method 4:  Save3pArnote
Signature: public RCodes Save3pArnote(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_3pArnote
Purpose:   Saves Accounts Receivable (AR) notes — follow-up activity on unpaid claims.

________________________________________
3.7  SaveCleints.cs
Path: BCAppCode/BHG-DR-LIB/SaveCleints.cs
Category: Client / Patient Demographics
Schedule: Regional ETL P1 (BHGTaskRunner.exe arg=2)

What this file does:
Handles client demographic data split into multiple destination tables. The split exists
because demographics data is very wide — splitting improves query performance.
Note: The filename "Cleints" is a typo in the original codebase (should be "Clients").

Method 1:  SaveClientDemo1var
Signature: public RCodes SaveClientDemo1var(DataTable tbl, string sc, int actionkey, BHG_DRContext db)
Writes to: pats.tbl_ClientDemo1
Purpose:   Saves core client demographic data — name, DOB, gender, race, address,
           contact info. The "var" variant handles variable schema versions.

Method 2:  SaveClientDemo1
Signature: public RCodes SaveClientDemo1(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_ClientDemo1
Purpose:   Standard version of client demographics save — core identity and contact fields.

Method 3:  SaveClientDemo2
Signature: public RCodes SaveClientDemo2(DataTable tbl, string sc, int actionkey, BHG_DRContext db)
Writes to: pats.tbl_ClientDemo2
Purpose:   Saves extended demographics — insurance details, employment, legal status,
           substance use history, referral source.

Method 4:  SaveClientDemo3
Signature: public bool SaveClientDemo3(DataTable tbl, string sc)
Writes to: pats.tbl_ClientDemo3
Purpose:   Saves additional client detail fields. Returns bool (simpler version,
           no EF context parameter — uses its own internal connection).

________________________________________
3.8  SaveDoses.cs
Path: BCAppCode/BHG-DR-LIB/SaveDoses.cs
Category: Medication / Dose Administration
Schedule: BHGTaskRunner.exe arg=10 (SAMMS-ETL-Dose)

What this file does:
Handles medication administration records and dose excuse records.

Method 1:  SaveDoses
Signature: public RCodes SaveDoses(DataTable tbl, string sc, DateTime dtWrk, bool reload, BHG_DRContext db)
Writes to: pats.tbl_Dose
Purpose:   Saves methadone/buprenorphine dose administration records — one row per
           patient per dose event. reload=true triggers full site reload.

Method 2:  SaveDoseExcuse
Signature: public bool SaveDoseExcuse(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_Dose_Excuse
Purpose:   Saves dose excuse records — when a patient was excused from a required dose
           (e.g., hospitalization, travel).

________________________________________
3.9  SaveUAResults.cs
Path: BCAppCode/BHG-DR-LIB/SaveUAResults.cs
Category: UA (Urinalysis) and Lab Results
Schedule: BHGTaskRunner.exe arg=5 (Samms-LAB) and arg=8 (SAMMS-ETL-INV)

What this file does:
Handles urine drug screen results, lab panel results, result detail, and UA schedule.

Method 1:  SaveLABResults
Signature: public RCodes SaveLABResults(DataTable tbl, string sc, DateTime wrkdt, bool reInit, BHG_DRContext db)
Writes to: pats.tbl_LabResult
Purpose:   Saves external lab result panel header records (LabCorp, Quest results).
           reInit=true clears and reloads all records for this site.

Method 2:  SaveUAResults
Signature: public RCodes SaveUAResults(DataTable tbl, string sc, DateTime wrkdt, bool reInit, BHG_DRContext db)
Writes to: pats.tbl_UAResult (in-house urinalysis results)
Purpose:   Saves in-clinic urine drug screen results — pass/fail/dilute per substance
           per patient per test date.

Method 3:  SaveUAResultDetail
Signature: public RCodes SaveUAResultDetail(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_UAResultDetail
Purpose:   Saves per-substance detail for each UA result — individual panel results
           for each drug category tested (opiates, benzos, cocaine, etc.).

Method 4:  SaveUASched
Signature: public RCodes SaveUASched(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_UASched
Purpose:   Saves UA schedule records — the expected testing schedule assigned to
           each patient (random, weekly, etc.).

________________________________________
3.10  SaveAssessments.cs
Path: BCAppCode/BHG-DR-LIB/SaveAssessments.cs
Category: Clinical Assessments — Admission and Periodic Reassessment
Schedule: BHGTaskRunner.exe arg=8 (SAMMS-ETL-INV)

What this file does:
Contains 19 methods covering the complete ASAM (American Society of Addiction Medicine)
assessment framework. Admission assessments are split across 6 ASAM dimensions.
Reassessments are also split by ASAM dimension.

Method 1:  SaveAdmissionAssessment
Signature: public RCodes SaveAdmissionAssessment(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessment
Purpose:   Saves the master admission assessment record — the top-level header
           for a patient's intake assessment.

Method 2:  SaveAdmissionAssessmentSummary
Signature: public RCodes SaveAdmissionAssessmentSummary(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessmentSummary
Purpose:   Saves the summary scores and conclusions from the admission assessment.

Method 3:  SaveAdmissionAssessmentDimensionfour
Signature: public RCodes SaveAdmissionAssessmentDimensionfour(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessmentDimensionFour (ASAM Dimension 4 — Readiness to Change)
Purpose:   Saves dimension 4 scores and narrative for the admission assessment.

Method 4:  SaveAdmissionAssessmentDimensionOneDisorder
Signature: public RCodes SaveAdmissionAssessmentDimensionOneDisorder(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessmentDimensionOneDisorder (ASAM Dimension 1 — Acute Intoxication)
Purpose:   Saves Dimension 1 disorder detail for the admission assessment.

Method 5:  SaveAdmissionAssessmentDimensionTwo
Signature: public RCodes SaveAdmissionAssessmentDimensionTwo(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessmentDimensionTwo (ASAM Dimension 2 — Biomedical)
Purpose:   Saves Dimension 2 biomedical condition data for the admission assessment.

Method 6:  SaveAdmissionAssessmentDimensionThree
Signature: public RCodes SaveAdmissionAssessmentDimensionThree(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessmentDimensionThree (ASAM Dimension 3 — Emotional/Behavioral)
Purpose:   Saves Dimension 3 emotional and behavioral health data.

Method 7:  SaveAdmissionAssessmentDimensionFiveSubstanceUse
Signature: public RCodes SaveAdmissionAssessmentDimensionFiveSubstanceUse(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse (ASAM Dimension 5 — Relapse Potential)
Purpose:   Saves Dimension 5 substance use relapse potential data.

Method 8:  SaveAdmissionAssessmentDimensionSix
Signature: public RCodes SaveAdmissionAssessmentDimensionSix(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_AdmissionAssessmentDimensionSix (ASAM Dimension 6 — Recovery Environment)
Purpose:   Saves Dimension 6 recovery environment / living situation data.

Method 9:  SaveAdmissionAssessmentSubstanceuseHistory
Signature: public RCodes SaveAdmissionAssessmentSubstanceuseHistory(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_AdmissionAssessmentSubstanceuseHistory
Purpose:   Saves per-substance historical use details linked to the admission assessment.

Method 10: SaveReAssessment
Signature: public RCodes SaveReAssessment(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessment
Purpose:   Saves the periodic reassessment header record.

Method 11: SaveReAssessmentOccupational
Signature: public RCodes SaveReAssessmentOccupational(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentOccupational
Purpose:   Saves reassessment occupational/employment status section.

Method 12: SaveReAssessmentFamily
Signature: public RCodes SaveReAssessmentFamily(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentFamily
Purpose:   Saves reassessment family/social relationships section.

Method 13: SaveReAssessmentLegal
Signature: public RCodes SaveReAssessmentLegal(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentLegal
Purpose:   Saves reassessment legal status section (pending charges, probation, etc.).

Method 14: SaveReAssessmentMentalHealth
Signature: public RCodes SaveReAssessmentMentalHealth(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentMentalHealth
Purpose:   Saves reassessment mental health and psychiatric status section.

Method 15: SaveReAssessmentPhysicalHealth
Signature: public RCodes SaveReAssessmentPhysicalHealth(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentPhysicalHealth
Purpose:   Saves reassessment physical health section.

Method 16: SaveReAssessmentSubstanceUse
Signature: public RCodes SaveReAssessmentSubstanceUse(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentSubstanceUse
Purpose:   Saves reassessment substance use status at time of reassessment.

Method 17: SaveReAssessmentSocial
Signature: public RCodes SaveReAssessmentSocial(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentSocial
Purpose:   Saves reassessment social support and community integration section.

Method 18: SaveReAssessmentTreatment
Signature: public RCodes SaveReAssessmentTreatment(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessmentTreatment
Purpose:   Saves reassessment treatment plan and goals section.

Method 19: SaveAssessmentSubstanceuseHistory
Signature: public RCodes SaveAssessmentSubstanceuseHistory(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_AssessmentSubstanceuseHistory
Purpose:   Saves substance use history detail for periodic reassessments.

________________________________________
3.11  SavePreAdmissionV6.cs
Path: BCAppCode/BHG-DR-LIB/SavePreAdmissionV6.cs
Category: Pre-Admission (V6 SAMMS Schema)
Schedule: Regional ETL P1 (BHGTaskRunner.exe arg=2)

What this file does:
Handles pre-admission intake records for clinics running SAMMS V6 or later schema.
Pre-admission captures patient information before formal enrollment.

Method 1:  SavePreAdmissionV6
Signature: public RCodes SavePreAdmissionV6(DataTable tbl, string sc, BHG_DRContext db)
Writes to: ayx.tbl_PreAdmission_V6
Purpose:   Saves new V6-schema pre-admission records — patient demographics, referral
           source, program of interest, consent status prior to full enrollment.

Method 2:  SavePreAdminReferrals
Signature: public RCodes SavePreAdminReferrals(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PreAdmission_Referrals
Purpose:   Saves referral source detail linked to pre-admission records.

________________________________________
3.12  SavePAData.cs
Path: BCAppCode/BHG-DR-LIB/SavePAData.cs
Category: Pre-Admission Detailed Dimensions and Financial Data
Schedule: BHGTaskRunner.exe arg=8 (SAMMS-ETL-INV)

What this file does:
Contains 10 methods for the detailed pre-admission data — financial hardship,
counselor review, six ASAM dimension assessments at pre-admission stage, full PA record,
and dropdown reference data.

Method 1:  SaveFinancialHardshipApplication
Signature: public RCodes SaveFinancialHardshipApplication(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_FinancialHardshipApplication
Purpose:   Saves patient financial hardship applications for sliding-scale fee determination.

Method 2:  SavePACounselorReview
Signature: public RCodes SavePACounselorReview(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PACounselorReview
Purpose:   Saves counselor review notes attached to the pre-admission record.

Method 3:  SavePADimension1
Signature: public RCodes SavePADimension1(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PADimension1
Purpose:   Saves PA ASAM Dimension 1 (Acute Intoxication/Withdrawal) data.

Method 4:  SavePADimension2
Signature: public RCodes SavePADimension2(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PADimension2
Purpose:   Saves PA ASAM Dimension 2 (Biomedical Conditions) data.

Method 5:  SavePADimension3
Signature: public RCodes SavePADimension3(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PADimension3
Purpose:   Saves PA ASAM Dimension 3 (Emotional/Behavioral/Cognitive) data.

Method 6:  SavePADimension4
Signature: public RCodes SavePADimension4(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PADimension4
Purpose:   Saves PA ASAM Dimension 4 (Readiness to Change) data.

Method 7:  SavePADimension5
Signature: public RCodes SavePADimension5(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PADimension5
Purpose:   Saves PA ASAM Dimension 5 (Relapse/Continued Use Potential) data.

Method 8:  SavePADimension6
Signature: public RCodes SavePADimension6(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PADimension6
Purpose:   Saves PA ASAM Dimension 6 (Recovery/Living Environment) data.

Method 9:  SavePA
Signature: public RCodes SavePA(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PA
Purpose:   Saves the master pre-admission header record that all dimensions link to.

Method 10: SavedropDownListItems
Signature: public RCodes SavedropDownListItems(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_dropDownListItems
Purpose:   Saves drop-down list item reference values used in the pre-admission form.

________________________________________
3.13  SaveGlobal.cs
Path: BCAppCode/BHG-DR-LIB/SaveGlobal.cs
Category: Global Reference / Configuration Data
Schedule: BHGTaskRunner.exe arg=1 (SAMMSGlobal)

What this file does:
The primary global data file. Contains 13 methods covering fee schedules, payers, users,
site configuration, BAM forms, and SAMMS client consent/device data. These are records
that apply across all clinics or are managed centrally.

Method 1:  SaveFeeSchedules
Signature: public RCodes SaveFeeSchedules(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_FeeSched
Purpose:   Saves fee schedule rates for each clinic — the billable amounts per service code.

Method 2:  SaveGlobalPayer
Signature: public RCodes SaveGlobalPayer(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_GlobalPayor
Purpose:   Saves payer (insurance) master records — payer name, payer ID, contract info.

Method 3:  SaveGlobalUser
Signature: public RCodes SaveGlobalUser(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_GlobalUser
Purpose:   Saves SAMMS staff/user records from each clinic — staff names, roles, credentials.

Method 4:  SaveGlobalUserSite
Signature: public RCodes SaveGlobalUserSite(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_GlobalUserSite
Purpose:   Saves staff-to-site assignment records — which users are active at which clinic.

Method 5:  SaveGlobalClinicalOpiateWithdrawalScale
Signature: public RCodes SaveGlobalClinicalOpiateWithdrawalScale(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_ClinicalOpiateWithdrawalScale
Purpose:   Saves COWS (Clinical Opiate Withdrawal Scale) assessment scores.

Method 6:  SaveGlobalFormsSAMMSClients
Signature: public RCodes SaveGlobalFormsSAMMSClients(DataTable tbl, string sc, DateTime FltrDate, bool firsthalf, BHG_DRContext db)
Writes to: pats.tbl_FormsSAMMSClient
Purpose:   Saves form-to-client linkage data from SAMMS — which forms are associated
           with which patients. firsthalf flag splits large loads into two batches.

Method 7:  SaveGlobalConsents
Signature: public RCodes SaveGlobalConsents(DataTable tbl, BHG_DRContext db)
Writes to: pats.tbl_Consents (or similar consent table)
Purpose:   Saves patient consent records from standard BHG clinics.

Method 8:  SaveGlobalConsentsPhc
Signature: public RCodes SaveGlobalConsentsPhc(DataTable tbl, BHG_DRContext db)
Writes to: pats.tbl_Consents (PHC-specific consent handling)
Purpose:   Saves patient consent records specifically from PHC — separate method
           because PHC may have different consent form structures.

Method 9:  SaveGlobalDevices
Signature: public RCodes SaveGlobalDevices(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_Devices
Purpose:   Saves device registration records — tablets or kiosks assigned to each clinic.

Method 10: SaveBAM
Signature: public RCodes SaveBAM(DataTable tbl, DateTime FltrDate, BHG_DRContext db)
Writes to: pats.tbl_BriefAddictionMonitor (BAM forms — note: also exists as separate SaveBAM.cs)
Purpose:   Saves Brief Addiction Monitor assessment form data.

Method 11: SaveServices
Signature: public RCodes SaveServices(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_Services
Purpose:   Saves service code lookup table data — the master list of billable service types.

Method 12: SaveFormCounts
Signature: public RCodes SaveFormCounts(DataTable tbl, BHG_DRContext db)
Writes to: stg.tbl_FormsCounts
Purpose:   Saves aggregated form count records into the staging schema for reporting.

Method 13: SaveClaimStatus
Signature: public RCodes SaveClaimStatus(DataTable tbl, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_ClaimStatus
Purpose:   Saves payer-returned claim status records — accepted, rejected, pending.

________________________________________
3.14  SaveGlobalorg.cs
Path: BCAppCode/BHG-DR-LIB/SaveGlobalorg.cs
Category: Global Org Data (active alternate version)

What this file does:
An alternate version of SaveGlobal.cs with 6 methods. Adds SaveGlobalFormsSAMMSClients.
Used in some execution paths. Overlaps with SaveGlobal.cs for some methods.

Method 1:  SaveFeeSchedules         → pats.tbl_FeeSched
Method 2:  SaveGlobalPayer          → pats.tbl_GlobalPayor
Method 3:  SaveGlobalUser           → pats.tbl_GlobalUser
Method 4:  SaveGlobalUserSite       → pats.tbl_GlobalUserSite
Method 5:  SaveGlobalClinicalOpiateWithdrawalScale  → pats.tbl_ClinicalOpiateWithdrawalScale
Method 6:  SaveGlobalFormsSAMMSClients → pats.tbl_FormsSAMMSClient

________________________________________
3.15  SaveGlobal-old.cs
Path: BCAppCode/BHG-DR-LIB/SaveGlobal-old.cs
Category: Legacy reference file — NOT used in production

What this file does:
Previous version of SaveGlobal.cs. Kept for historical reference only. Has 5 methods.
Should not be called in any active code path.

Method 1:  SaveFeeSchedules         → pats.tbl_FeeSched
Method 2:  SaveGlobalPayer          → pats.tbl_GlobalPayor
Method 3:  SaveGlobalUser           → pats.tbl_GlobalUser
Method 4:  SaveGlobalUserSite       → pats.tbl_GlobalUserSite
Method 5:  SaveGlobalClinicalOpiateWithdrawalScale → pats.tbl_ClinicalOpiateWithdrawalScale

________________________________________
3.16  SaveCodes.cs
Path: BCAppCode/BHG-DR-LIB/SaveCodes.cs
Category: Service / Reference Codes
Schedule: SAMMSGlobal (BHGTaskRunner.exe arg=1)

What this file does:
Two overloaded versions of SaveCodes. The bool version is an older pattern; the RCodes
version is the modern pattern. Both write to the same destination.

Method 1:  SaveCodes (bool version)
Signature: public bool SaveCodes(DataTable tbl, string sc, bool PYear, BHG_DRContext db)
Writes to: pats.tbl_Codes
Purpose:   Saves SAMMS service/diagnosis code lookup values. PYear flag indicates
           whether to filter by year.

Method 2:  SaveCodes (RCodes version)
Signature: public RCodes SaveCodes(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_Codes
Purpose:   Modern version of SaveCodes returning structured RCodes result.

________________________________________
3.17  SaveClinic.cs
Path: BCAppCode/BHG-DR-LIB/SaveClinic.cs
Category: Clinic Master Data
Schedule: SAMMSGlobal (BHGTaskRunner.exe arg=1)

What this file does:
Single method that saves clinic/site configuration records from SAMMS.

Method 1:  SaveClinic
Signature: public RCodes SaveClinic(DataTable tbl, string sc, BHG_DRContext db)
Writes to: ctrl.tbl_Clinic
Purpose:   Saves clinic profile data — site name, address, NPI numbers, program types,
           operating hours, and SAMMS version information.

________________________________________
3.18  SaveInventory.cs
Path: BCAppCode/BHG-DR-LIB/SaveInventory.cs
Category: Inventory Management
Schedule: BHGTaskRunner.exe arg=8 (SAMMS-ETL-INV)

What this file does:
Handles medication inventory records — bottles dispensed, liquid methadone logs,
inventory type setup, and patient orientation checklists.

Method 1:  SaveBottles
Signature: public RCodes SaveBottles(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_Bottle
Purpose:   Saves bottled take-home medication dispensing records — one row per
           bottle issued per patient.

Method 2:  SaveLiquidlog
Signature: public RCodes SaveLiquidlog(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_LiquidLog
Purpose:   Saves liquid methadone dispensing log records — daily volume dispensed
           per patient per date.

Method 3:  SaveInvTypes
Signature: public RCodes SaveInvTypes(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: ctrl.tbl_InvType
Purpose:   Saves inventory type reference data — medication types and container types
           configured at each clinic.

Method 4:  SaveOrientationCheckList
Signature: public RCodes SaveOrientationCheckList(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_OrientationChecklistNew
Purpose:   Saves patient orientation checklist completion records — tracks which
           orientation items each patient has completed.

________________________________________
3.19  SaveFormQAData.cs
Path: BCAppCode/BHG-DR-LIB/SaveFormQAData.cs
Category: Form Question Answers and E&M Forms
Schedule: BHGTaskRunner.exe arg=6 (Samms-Forms)

What this file does:
Handles digital form submission data from SAMMS — question/answer pairs, signatures,
E&M evaluation forms, and comprehensive assessments.

Method 1:  SaveFormQuestionAnswers
Signature: public RCodes SaveFormQuestionAnswers(DataTable tbl, string sc, DateTime wrkdt, List<TblForms2Process> f2p, BHG_DRContext db)
Writes to: pats.tbl_dbo_FormQuestionAnswers
Purpose:   Saves SAMMS digital form answers — one row per question per form submission.
           f2p is a filter list of form types to process (e.g. specific assessment forms).

Method 2:  SaveAnswerSignatures
Signature: public RCodes SaveAnswerSignatures(DataTable tbl, string sc, DateTime wrkdt, List<TblForms2Process> f2p, BHG_DRContext db)
Writes to: pats.tbl_dbo_FormAnswerSignatures
Purpose:   Saves digital signature data linked to completed form answers.

Method 3:  SaveEMFormMDM
Signature: public RCodes SaveEMFormMDM(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_EandMFormMDM
Purpose:   Saves Evaluation & Management (E&M) Medical Decision Making (MDM) form data.
           Used for medical billing complexity scoring.

Method 4:  SaveEMFormPregnancy
Signature: public RCodes SaveEMFormPregnancy(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_EandMFormPregnancy
Purpose:   Saves E&M pregnancy-related form data — obstetric risk assessment records.

Method 5:  SaveComprehensiveAssessmentForm
Signature: public RCodes SaveComprehensiveAssessmentForm(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_ComprehensiveAssessmentForm
Purpose:   Saves comprehensive clinical assessment form submissions.

________________________________________
3.20  SaveCustomQA.cs
Path: BCAppCode/BHG-DR-LIB/SaveCustomQA.cs
Category: Custom Questions and Answers
Schedule: Regional ETL P1 (BHGTaskRunner.exe arg=2)

What this file does:
Handles custom (clinic-defined) question and answer records that are not part of standard forms.

Method 1:  SaveCustomQuestions
Signature: public RCodes SaveCustomQuestions(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_CustomQuestions
Purpose:   Saves custom question definitions created by individual clinics.

Method 2:  SaveCustomAnswers
Signature: public RCodes SaveCustomAnswers(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_CustomAnswers
Purpose:   Saves answers to custom questions — linked to patient records.

________________________________________
3.21  SaveCows.cs
Path: BCAppCode/BHG-DR-LIB/SaveCows.cs
Category: COWS Clinical Scale (V6 schema)
Schedule: BHGTaskRunner.exe arg=8 (SAMMS-ETL-INV)

What this file does:
Single method for the COWS V6 (Clinical Opiate Withdrawal Scale version 6) assessments.

Method 1:  SaveCows_v6
Signature: public RCodes SaveCows_v6(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_Cows_V6
Purpose:   Saves COWS V6 assessment records — structured scoring of opiate withdrawal
           symptoms using the standardized 11-item clinical scale.
           Note: PHC is excluded from this method via task RowState=26 in Scheduler.

________________________________________
3.22  SaveCA.cs
Path: BCAppCode/BHG-DR-LIB/SaveCA.cs
Category: Clinical Assessments (MN and VA specific)
Schedule: BHGTaskRunner.exe arg=8 (SAMMS-ETL-INV)

What this file does:
Contains clinical assessment methods for state-specific assessment forms.
MN = Minnesota LOC (Level of Care), VA = Virginia ASAM assessments.

Method 1:  SaveMNCA
Signature: public RCodes SaveMNCA(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_MNCA
Purpose:   Saves Minnesota-specific clinical assessment records.

Method 2:  SaveMNCALOC
Signature: public RCodes SaveMNCALOC(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_MNCALOC
Purpose:   Saves Minnesota Level of Care (LOC) determination records.

Method 3:  SaveVACA
Signature: public RCodes SaveVACA(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_VACA
Purpose:   Saves Virginia-specific ASAM clinical assessment records.

Method 4:  SaveVACASummary
Signature: public RCodes SaveVACASummary(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_VACASummary
Purpose:   Saves summary scores from the Virginia ASAM assessment.

Method 5:  SaveNewAdmissionAssessment
Signature: public RCodes SaveNewAdmissionAssessment(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_AdmissionAssessment (new schema version)
Purpose:   Saves admission assessments using a newer SAMMS schema structure.

Method 6:  SaveNewAdmissionAssessmentASAMDimension6
Signature: public RCodes SaveNewAdmissionAssessmentASAMDimension6(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_AdmissionAssessmentDimensionSix (new schema version)
Purpose:   Saves ASAM Dimension 6 data from the newer admission assessment structure.

Method 7:  SaveNewPeriodicReassessment
Signature: public RCodes SaveNewPeriodicReassessment(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_ReAssessment (new schema version)
Purpose:   Saves periodic reassessment records using the newer SAMMS schema.

Method 8:  Savenewperiodicreassessmentcounselorreview
Signature: public RCodes Savenewperiodicreassessmentcounselorreview(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_PACounselorReview (reassessment counselor review)
Purpose:   Saves counselor review data attached to new periodic reassessment records.

________________________________________
3.23  SaveBAM.cs
Path: BCAppCode/BHG-DR-LIB/SaveBAM.cs
Category: Brief Addiction Monitor
Schedule: SAMMSGlobal (BHGTaskRunner.exe arg=1)

What this file does:
Handles the BAM (Brief Addiction Monitor) standardized addiction severity assessment —
a validated 17-item scale measuring substance use, risk factors, and protective factors.

Method 1:  SaveBamForm
Signature: public RCodes SaveBamForm(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_BAMForm
Purpose:   Saves the BAM form header record — one row per assessment per patient.

Method 2:  SaveBamScore
Signature: public RCodes SaveBamScore(DataTable tbl, string sc, BHG_DRContext db)
Writes to: pats.tbl_BAMScore
Purpose:   Saves the BAM domain scores — risk factor score, protective factor score,
           and substance use score computed from the 17-item questionnaire.

________________________________________
3.24  SaveEnrollment.cs
Path: BCAppCode/BHG-DR-LIB/SaveEnrollment.cs
Category: Patient Enrollment
Schedule: Regional ETL P1 and P2 (args 2 and 4)

What this file does:
Single method that saves patient enrollment records — the official admission
of a patient into a BHG treatment program.

Method 1:  SaveEnrollment
Signature: public RCodes SaveEnrollment(DataTable tbl, string sc, long akey, BHG_DRContext db)
Writes to: pats.tbl_Enrollment
Purpose:   Saves enrollment records — admission date, program type, discharge date,
           treatment level, discharge reason. One row per program admission per patient.

________________________________________
3.25  SaveCheckIn.cs
Path: BCAppCode/BHG-DR-LIB/SaveCheckIn.cs
Category: Daily Check-In Records
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Single method that saves daily clinic check-in records.

Method 1:  SaveCheckIn
Signature: public RCodes SaveCheckIn(DataTable tbl, string sc, DateTime workdate, BHG_DRContext db)
Writes to: pats.tbl_CheckIn
Purpose:   Saves daily patient check-in events — when a patient arrives at the clinic
           for their dose or counseling appointment.

________________________________________
3.26  SaveTreatmentLevel.cs
Path: BCAppCode/BHG-DR-LIB/SaveTreatmentLevel.cs
Category: Treatment Level / Level of Care
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Single method for treatment level of care determination records.

Method 1:  SaveTreatmentLevel
Signature: public RCodes SaveTreatmentLevel(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_TreatmentLevel
Purpose:   Saves treatment level assignments — the ASAM level of care (e.g. 1.0, 2.1, 3.5)
           assigned to each patient at each review date.

________________________________________
3.27  SaveAppointments.cs
Path: BCAppCode/BHG-DR-LIB/SaveAppointments.cs
Category: Appointments and Attendance
Schedule: BHGTaskRunner.exe arg=8 (SAMMS-ETL-INV)

What this file does:
Handles appointment records and appointment attendance/show-no-show records.

Method 1:  SaveAppointmentAttend
Signature: public RCodes SaveAppointmentAttend(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.tbl_AppointmentAttend
Purpose:   Saves appointment attendance outcomes — attended, no-show, cancelled,
           rescheduled. One row per scheduled appointment.

Method 2:  SaveAppointments
Signature: public RCodes SaveAppointments(DataTable tbl, string sc, DateTime wrkdt, BHG_DRContext db)
Writes to: pats.Tbl_Appointments
Purpose:   Saves appointment master records — scheduled appointment date/time,
           type, assigned staff, linked service.

________________________________________
3.28  SavePayorClient.cs
Path: BCAppCode/BHG-DR-LIB/SavePayorClient.cs
Category: Payer / Insurance Client Records
Schedule: Regional ETL P2 (BHGTaskRunner.exe arg=4)

What this file does:
Handles payer-to-client insurance linkage records and payer client history.
Also handles removal of lapsed insurance records.

Method 1:  SavePayerClient
Signature: public RCodes SavePayerClient(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_PayerClient
Purpose:   Saves active insurance coverage records per patient — payer, policy number,
           group number, effective dates, copay amounts.

Method 2:  RemovePayerClients
Signature: public RCodes RemovePayerClients(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_PayerClient (removal/deactivation)
Purpose:   Marks insurance records as inactive when the patient's coverage has ended.
           Used to sync coverage terminations from SAMMS into Azure.

Method 3:  SavePayerCltHistory
Signature: public RCodes SavePayerCltHistory(DataTable tbl, string sc, DateTime wrkdt, bool yearly, BHG_DRContext db)
Writes to: pats.tbl_PayerCltHistory
Purpose:   Saves historical insurance coverage records — tracks all payer relationships
           a patient has had over time, including terminated policies.

________________________________________
3.29  SaveRowTrax.cs
Path: BCAppCode/BHG-DR-LIB/SaveRowTrax.cs
Category: Row Count Audit Trail
Called by: All other Save methods that have RowTrax enabled

What this file does:
Single utility method that records source vs destination row counts after each load.
Used for data quality monitoring.

Method 1:  SaveRowTrax
Signature: public RCodes SaveRowTrax(string sc, DateTime rcdate, string tblname, int sammscnt, int azurecnt, BHG_DRContext db)
Writes to: tsk.tbl_RowTrax
Purpose:   Logs a row count audit record:
           - sc = SiteCode (e.g. "B01")
           - rcdate = the WorkDate of the run
           - tblname = the destination table (e.g. "pats.tbl_dartssrv")
           - sammscnt = count of rows in source SAMMS table
           - azurecnt = count of rows in Azure destination table
           This lets analysts compare source vs destination over time and flag
           clinics where data is falling behind or drifting.

________________________________________
3.30  SaveFmp.cs
Path: BCAppCode/BHG-DR-LIB/SaveFmp.cs
Category: Financial Management Platform Data
Schedule: Regional ETL (specific schedule — FMP-related)

What this file does:
Single method that saves FMP (Financial Management Platform) data.

Method 1:  SaveFmp
Signature: public RCodes SaveFmp(DataTable tbl, string sc, DateTime dtWrk, BHG_DRContext db)
Writes to: pats.tbl_FMP (or similar FMP destination)
Purpose:   Saves financial management records from FMP-integrated clinics —
           payment plans, sliding fee scale calculations, financial class assignments.

________________________________________

4. Method Count by File — Quick Reference

File                       Methods    Pattern
----                       -------    -------
SaveDartsSrvs.cs           10         Year-split (2014–2023)
SaveOrders.cs              14         Year-split (base + 2016–2028)
SaveAssessments.cs         19         One per ASAM dimension/section
SavePAData.cs              10         One per PA dimension/section
SaveGlobal.cs              13         Mixed reference data
SaveGlobalorg.cs           6          Reference data (alt version)
SaveGlobal-old.cs          5          Legacy (not in use)
SaveCA.cs                  8          State-specific assessments
SaveFormQAData.cs          5          Form QA data
SaveClaims.cs              4          Claims 3-tier
Save3pElig.cs              4          3rd party billing
SaveUAResults.cs           4          UA/Lab
SaveInventory.cs           4          Inventory
SaveCleints.cs             4          Client demographics
SaveBills.cs               3          Billing
SavePayorClient.cs         3          Insurance
SavePreAdmissionV6.cs      2          Pre-admission V6
SaveDoses.cs               2          Medications
SaveAuths.cs               2          Authorizations
SaveCustomQA.cs            2          Custom forms
SaveBAM.cs                 2          BAM scale
SaveAppointments.cs        2          Appointments
SaveCodes.cs               2          Service codes (2 overloads)
SaveEnrollment.cs          1          Enrollment
SaveCheckIn.cs             1          Check-in
SaveTreatmentLevel.cs      1          Treatment level
SaveCows.cs                1          COWS V6 scale
SaveClinic.cs              1          Clinic master
SaveRowTrax.cs             1          Audit counts
SaveFmp.cs                 1          FMP financial data

TOTAL (excluding old/legacy): ~105 active methods across 29 files

________________________________________

5. Destination Table Quick Map

Method                                  → Destination Table
------                                  -------------------
SaveDartSrv2014–2023                    → pats.tbl_DartsSrv_2014B4 through 2023
SaveOrders / SaveOrders2016–2028        → pats.tbl_Orders / tbl_Orders_2016 through 2028
SaveBills                               → pats.tbl_Bills
SaveAuthBills                           → pats.tbl_AuthBills
SaveClaims                              → pats.tbl_Claims
SaveClaimLineItem                       → pats.tbl_ClaimLineItem
SaveClaimLineItemActivity               → pats.tbl_ClaimLineItemActivity
SaveAuths                               → pats.tbl_Auths
SaveAuthBillsub                         → pats.tbl_vw3pBillSub
Save3pElig                              → pats.tbl_3pElig
Save3pSetup                             → pats.tbl_3pSetup
Save3pClaimNote                         → pats.tbl_3pClaimNote
Save3pArnote                            → pats.tbl_3pArnote
SaveClientDemo1 / Demo1var              → pats.tbl_ClientDemo1
SaveClientDemo2                         → pats.tbl_ClientDemo2
SaveClientDemo3                         → pats.tbl_ClientDemo3
SaveDoses                               → pats.tbl_Dose
SaveDoseExcuse                          → pats.tbl_Dose_Excuse
SaveLABResults                          → pats.tbl_LabResult
SaveUAResults                           → pats.tbl_UAResult
SaveUAResultDetail                      → pats.tbl_UAResultDetail
SaveUASched                             → pats.tbl_UASched
SaveAdmissionAssessment                 → pats.Tbl_AdmissionAssessment
SaveAdmissionAssessmentSummary          → pats.Tbl_AdmissionAssessmentSummary
SaveAdmissionAssessmentDimension1       → pats.Tbl_AdmissionAssessmentDimensionOneDisorder
SaveAdmissionAssessmentDimension2       → pats.Tbl_AdmissionAssessmentDimensionTwo
SaveAdmissionAssessmentDimension3       → pats.Tbl_AdmissionAssessmentDimensionThree
SaveAdmissionAssessmentDimension4       → pats.Tbl_AdmissionAssessmentDimensionFour
SaveAdmissionAssessmentDimension5       → pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse
SaveAdmissionAssessmentDimension6       → pats.tbl_AdmissionAssessmentDimensionSix
SaveAdmissionAssessmentSubstanceHist    → pats.tbl_AdmissionAssessmentSubstanceuseHistory
SaveReAssessment                        → pats.Tbl_ReAssessment
SaveReAssessmentOccupational            → pats.Tbl_ReAssessmentOccupational
SaveReAssessmentFamily                  → pats.Tbl_ReAssessmentFamily
SaveReAssessmentLegal                   → pats.Tbl_ReAssessmentLegal
SaveReAssessmentMentalHealth            → pats.Tbl_ReAssessmentMentalHealth
SaveReAssessmentPhysicalHealth          → pats.Tbl_ReAssessmentPhysicalHealth
SaveReAssessmentSubstanceUse            → pats.Tbl_ReAssessmentSubstanceUse
SaveReAssessmentSocial                  → pats.Tbl_ReAssessmentSocial
SaveReAssessmentTreatment               → pats.Tbl_ReAssessmentTreatment
SaveAssessmentSubstanceuseHistory       → pats.tbl_AssessmentSubstanceuseHistory
SavePreAdmissionV6                      → ayx.tbl_PreAdmission_V6
SavePreAdminReferrals                   → pats.tbl_PreAdmission_Referrals
SaveFinancialHardshipApplication        → pats.tbl_FinancialHardshipApplication
SavePACounselorReview                   → pats.tbl_PACounselorReview
SavePADimension1–6                      → pats.tbl_PADimension1 through 6
SavePA                                  → pats.tbl_PA
SavedropDownListItems                   → pats.tbl_dropDownListItems
SaveFeeSchedules                        → pats.tbl_FeeSched
SaveGlobalPayer                         → pats.tbl_GlobalPayor
SaveGlobalUser                          → pats.tbl_GlobalUser
SaveGlobalUserSite                      → pats.tbl_GlobalUserSite
SaveGlobalClinicalOpiateWithdrawalScale → pats.tbl_ClinicalOpiateWithdrawalScale
SaveGlobalFormsSAMMSClients             → pats.tbl_FormsSAMMSClient
SaveGlobalConsents                      → pats.tbl_Consents
SaveGlobalDevices                       → pats.tbl_Devices
SaveBAM / SaveBamForm                   → pats.tbl_BAMForm
SaveBamScore                            → pats.tbl_BAMScore
SaveServices                            → pats.tbl_Services
SaveFormCounts                          → stg.tbl_FormsCounts
SaveClaimStatus                         → pats.tbl_ClaimStatus
SaveCodes                               → pats.tbl_Codes
SaveClinic                              → ctrl.tbl_Clinic
SaveBottles                             → pats.tbl_Bottle
SaveLiquidlog                           → pats.tbl_LiquidLog
SaveInvTypes                            → ctrl.tbl_InvType
SaveOrientationCheckList                → pats.tbl_OrientationChecklistNew
SaveFormQuestionAnswers                 → pats.tbl_dbo_FormQuestionAnswers
SaveAnswerSignatures                    → pats.tbl_dbo_FormAnswerSignatures
SaveEMFormMDM                           → pats.tbl_EandMFormMDM
SaveEMFormPregnancy                     → pats.tbl_EandMFormPregnancy
SaveComprehensiveAssessmentForm         → pats.tbl_ComprehensiveAssessmentForm
SaveCustomQuestions                     → pats.tbl_CustomQuestions
SaveCustomAnswers                       → pats.tbl_CustomAnswers
SaveCows_v6                             → pats.tbl_Cows_V6
SaveMNCA                                → pats.tbl_MNCA
SaveMNCALOC                             → pats.tbl_MNCALOC
SaveVACA                                → pats.tbl_VACA
SaveVACASummary                         → pats.tbl_VACASummary
SaveEnrollment                          → pats.tbl_Enrollment
SaveCheckIn                             → pats.tbl_CheckIn
SaveTreatmentLevel                      → pats.tbl_TreatmentLevel
SaveAppointmentAttend                   → pats.tbl_AppointmentAttend
SaveAppointments                        → pats.Tbl_Appointments
SavePayerClient                         → pats.tbl_PayerClient
RemovePayerClients                      → pats.tbl_PayerClient (deactivation)
SavePayerCltHistory                     → pats.tbl_PayerCltHistory
SaveRowTrax                             → tsk.tbl_RowTrax
SaveFmp                                 → pats.tbl_FMP
