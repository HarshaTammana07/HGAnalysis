# EF Core Table Runtime — Single Day Results

**Run date:** 2026-04-22  
**Source:** `tsk.tbl_Tasks2` (Status = 19, child tasks only)  
**Load Type:** EF Core Upsert (all tables)  
**Scope:** One ETL night — 115–116 site runs per table (where applicable)

---

## Runtime Results by Category

### 3p Eligibility


| Table Name             | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ---------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_3pClaimNote` | 116   | 385,144    | **01:07:51**   | 00:00:35   | 00:00:00 | 00:11:29 | VWBY         | 3,320        | 17,057       |
| `pats.tbl_3pArnote`    | 116   | 873,631    | **00:16:26**   | 00:00:08   | 00:00:00 | 00:02:54 | DRD-KVC      | 7,531        | 59,790       |
| `pats.tbl_3pElig`      | 116   | 11,292     | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 97           | 1,475        |


---

### Activity


| Table Name                   | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ---------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.Tbl_Appointments`      | 115   | 2,141,293  | **01:00:01**   | 00:00:31   | 00:00:02 | 00:03:39 | D07          | 18,620       | 93,832       |
| `pats.tbl_CheckIn`           | 116   | 186,167    | 00:00:24       | 00:00:00   | 00:00:00 | 00:00:02 | B35          | 1,605        | 6,676        |
| `pats.tbl_AppointmentAttend` | 115   | 23,965     | 00:00:23       | 00:00:00   | 00:00:00 | 00:00:08 | B33          | 208          | 6,642        |
| `pats.tbl_TreatmentLevel`    | 115   | 70,651     | 00:01:03       | 00:00:00   | 00:00:00 | 00:00:03 | B33          | 614          | 1,892        |


---

### Assessment (Admission)


| Table Name                                              | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ------------------------------------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.Tbl_AdmissionAssessmentSummary`                   | 115   | 48,422     | **00:23:59**   | 00:00:12   | 00:00:00 | 00:00:52 | D09          | 421          | 1,579        |
| `pats.tbl_AdmissionAssessmentDimensionFour`             | 115   | 45,848     | 00:00:29       | 00:00:00   | 00:00:00 | 00:00:03 | DRD-KVC      | 399          | 1,483        |
| `pats.tbl_AdmissionAssessmentDimensionSix`              | 115   | 45,723     | 00:00:27       | 00:00:00   | 00:00:00 | 00:00:02 | B26          | 398          | 1,477        |
| `pats.Tbl_AdmissionAssessmentDimensionThree`            | 115   | 46,143     | 00:00:27       | 00:00:00   | 00:00:00 | 00:00:02 | D07          | 401          | 1,490        |
| `pats.Tbl_AdmissionAssessmentDimensionFiveSubstanceUse` | 115   | 45,754     | 00:00:26       | 00:00:00   | 00:00:00 | 00:00:02 | D07          | 398          | 1,482        |
| `pats.Tbl_AdmissionAssessmentDimensionTwo`              | 115   | 46,367     | 00:00:24       | 00:00:00   | 00:00:00 | 00:00:02 | D07          | 403          | 1,510        |
| `pats.tbl_Admissionassessmentsubstanceusehistory`       | 115   | 47,178     | 00:00:39       | 00:00:00   | 00:00:00 | 00:00:03 | DRD-NOLA     | 410          | 2,484        |
| `pats.Tbl_AdmissionAssessment`                          | 115   | 1,052      | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 9            | 56           |


---

### ReAssessment


| Table Name                            | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ------------------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_ReAssessmentPhysicalHealth` | 115   | 78,871     | 00:01:36       | 00:00:00   | 00:00:00 | 00:00:10 | B57          | 686          | 3,205        |
| `pats.tbl_ReAssessmentSubstanceUse`   | 115   | 79,713     | 00:01:27       | 00:00:00   | 00:00:00 | 00:00:04 | D07          | 693          | 3,216        |
| `pats.tbl_ReAssessmentLegal`          | 115   | 78,449     | 00:01:23       | 00:00:00   | 00:00:00 | 00:00:05 | V11          | 682          | 3,202        |
| `pats.tbl_ReAssessmentMentalHealth`   | 115   | 78,733     | 00:01:22       | 00:00:00   | 00:00:00 | 00:00:04 | V20          | 685          | 3,203        |
| `pats.tbl_ReAssessmentSocial`         | 115   | 78,434     | 00:01:16       | 00:00:00   | 00:00:00 | 00:00:05 | VBRP         | 682          | 3,201        |
| `pats.tbl_ReAssessmentOccupational`   | 115   | 78,554     | 00:01:15       | 00:00:00   | 00:00:00 | 00:00:04 | DRD-NOLA     | 683          | 3,204        |
| `pats.tbl_ReAssessmentFamily`         | 115   | 78,632     | 00:01:12       | 00:00:00   | 00:00:00 | 00:00:04 | MP           | 684          | 3,205        |
| `pats.tbl_ReAssessmentTreatment`      | 115   | 78,203     | 00:01:12       | 00:00:00   | 00:00:00 | 00:00:04 | DRD-NOLA     | 680          | 3,195        |
| `pats.Tbl_ReAssessment`               | 115   | 3          | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 0            | 3            |


---

### BAM (Brief Addiction Monitor)


| Table Name          | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_BAMForm`  | 115   | 0          | 00:07:16       | 00:00:03   | 00:00:00 | 00:01:34 | B26          | 0            | 0            |
| `pats.tbl_BAMScore` | 115   | 0          | 00:06:11       | 00:00:03   | 00:00:00 | 00:01:06 | LO           | 0            | 0            |


---

### Billing


| Table Name       | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ---------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_Bills` | 116   | 493,994    | **00:26:40**   | 00:00:13   | 00:00:00 | 00:00:47 | DRD-KVC      | 4,259        | 15,370       |


---

### Clinical Scales


| Table Name         | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ------------------ | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_Cows_V6` | 116   | 643,416    | **00:15:55**   | 00:00:08   | 00:00:00 | 00:00:48 | VWBY         | 5,547        | 19,288       |


---

### Enrollment


| Table Name            | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| --------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_Enrollment` | 116   | 702,955    | **00:47:21**   | 00:00:24   | 00:00:00 | 00:01:50 | V9           | 6,060        | 25,453       |


---

### Financial


| Table Name     | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| -------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_Fmp` | 116   | 174,083    | 00:03:47       | 00:00:01   | 00:00:00 | 00:00:27 | DRD-NOLA     | 1,501        | 18,596       |


---

### Forms


| Table Name                             | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| -------------------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_dbo_FormAnswerSignatures`    | 116   | 1,544,350  | **09:16:01**   | 00:04:47   | 00:00:01 | 01:11:03 | D07          | 13,313       | 79,200       |
| `pats.tbl_CustomAnswers`               | 116   | 186,943    | 00:03:17       | 00:00:01   | 00:00:00 | 00:00:27 | DRD-KVB      | 1,612        | 22,012       |
| `pats.tbl_EandMFormPregnancy`          | 115   | 55,036     | 00:00:52       | 00:00:00   | 00:00:00 | 00:00:03 | V12          | 479          | 1,731        |
| `pats.tbl_EandMFormMDM`                | 115   | 474,706    | 00:00:46       | 00:00:00   | 00:00:00 | 00:00:19 | D07          | 4,128        | 56,202       |
| `pats.tbl_ComprehensiveAssessmentForm` | 115   | 44,288     | 00:00:33       | 00:00:00   | 00:00:00 | 00:00:02 | LV2          | 385          | 1,176        |
| `pats.tbl_CustomQuestions`             | 116   | 1,941      | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 17           | 48           |


---

### Global Reference


| Table Name                               | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ---------------------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_clinicalopiatewithdrawalscale` | 1     | 425,775    | **00:33:57**   | 00:33:57   | 00:33:57 | 00:33:57 | Global       | 425,775      | 425,775      |
| `pats.tbl_FeeSched`                      | 1     | 15,206     | 00:00:39       | 00:00:39   | 00:00:39 | 00:00:39 | Global       | 15,206       | 15,206       |
| `pats.tbl_BriefAddictionMonitor`         | 1     | 0          | 00:00:05       | 00:00:05   | 00:00:05 | 00:00:05 | Global       | 0            | 0            |
| `ctrl.tbl_Clinic`                        | 116   | 116        | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 1            | 1            |
| `pats.tbl_SERVICES`                      | 116   | 27,696     | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 239          | 250          |
| `pats.tbl_GlobalPayor`                   | 1     | 883        | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | Global       | 883          | 883          |
| `pats.tbl_Codes`                         | 116   | 173,404    | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 1,495        | 4,158        |


---

### Inventory


| Table Name                         | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ---------------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_Bottle`                  | 116   | 1,756,233  | **00:07:12**   | 00:00:03   | 00:00:00 | 00:01:02 | DRD-KVB      | 15,140       | 112,171      |
| `ctrl.tbl_InvType`                 | 116   | 753        | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 6            | 9            |
| `pats.Tbl_OrientationChecklistNew` | 112   | 726        | 00:00:00       | 00:00:00   | 00:00:00 | 00:00:00 | AHK          | 6            | 24           |


---

### Orders (Year-Partitioned — all routed through C# year split)


| Table Name        | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ----------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_Orders` | 116   | 3,515,280  | **06:14:06**   | 00:03:13   | 00:00:00 | 00:22:10 | VBRP         | 30,304       | 202,350      |


---

### PA Data (Pre-Admission)


| Table Name                              | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| --------------------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_PACounselorReview`            | 115   | 101,456    | **00:48:11**   | 00:00:25   | 00:00:00 | 00:04:06 | DRD-NOLA     | 882          | 3,349        |
| `pats.tbl_PA`                           | 115   | 102,265    | **00:23:10**   | 00:00:12   | 00:00:00 | 00:01:00 | B42          | 889          | 3,359        |
| `pats.tbl_FinancialHardshipApplication` | 115   | 64,448     | 00:10:32       | 00:00:05   | 00:00:00 | 00:00:36 | D07          | 560          | 2,498        |
| `pats.tbl_PADimension4`                 | 115   | 101,165    | 00:02:15       | 00:00:01   | 00:00:00 | 00:00:05 | B48          | 880          | 3,339        |
| `pats.tbl_PADimension5`                 | 115   | 101,102    | 00:01:55       | 00:00:01   | 00:00:00 | 00:00:05 | DRD-NOLA     | 879          | 3,340        |
| `pats.tbl_PADimension6`                 | 115   | 101,081    | 00:01:47       | 00:00:00   | 00:00:00 | 00:00:04 | DRD-KVC      | 879          | 3,338        |
| `pats.tbl_PADimension3`                 | 115   | 101,232    | 00:01:46       | 00:00:00   | 00:00:00 | 00:00:05 | LO           | 880          | 3,342        |
| `pats.tbl_PADimension2`                 | 115   | 101,353    | 00:01:45       | 00:00:00   | 00:00:00 | 00:00:05 | HNT          | 881          | 3,345        |
| `pats.tbl_PADimension1`                 | 115   | 102,265    | 00:01:39       | 00:00:00   | 00:00:00 | 00:00:04 | DRD-KVC      | 889          | 3,359        |


---

### Payer / Insurance


| Table Name                 | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| -------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_PayerClient`     | 115   | 105,905    | 00:05:34       | 00:00:02   | 00:00:00 | 00:00:10 | B35          | 921          | 3,666        |
| `pats.tbl_PayerCltHistory` | 116   | 7,246      | 00:00:03       | 00:00:00   | 00:00:00 | 00:00:01 | B24          | 62           | 429          |


---

### Pre-Admission


| Table Name                | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| ------------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `ayx.tbl_PreAdmission_V6` | 115   | 23,966     | 00:00:23       | 00:00:00   | 00:00:00 | 00:00:02 | CBCO         | 208          | 902          |


---

### UA / Lab Results


| Table Name           | Sites | Total Rows | Total Duration | Avg / Site | Min      | Max      | Slowest Site | Avg Rows/Run | Max Rows/Run |
| -------------------- | ----- | ---------- | -------------- | ---------- | -------- | -------- | ------------ | ------------ | ------------ |
| `pats.tbl_UAResults` | 116   | 47,883     | **02:46:48**   | 00:01:26   | 00:00:00 | 00:11:51 | B35          | 413          | 1,370        |
| `pats.tbl_UASched`   | 116   | 647,291    | **01:18:37**   | 00:00:40   | 00:00:00 | 00:10:04 | B26          | 5,580        | 27,240       |
| `pats.Tbl_LabResult` | 116   | 384,569    | 00:12:11       | 00:00:06   | 00:00:00 | 00:00:53 | LO           | 3,315        | 27,736       |


---

## Top 10 Heaviest Tables (by Total Sequential Duration)


| Rank | Table Name                            | Category   | Total Duration | Max (Fabric wall-clock) | Slowest Site |
| ---- | ------------------------------------- | ---------- | -------------- | ----------------------- | ------------ |
| 1    | `pats.tbl_dbo_FormAnswerSignatures`   | Forms      | **09:16:01**   | 01:11:03                | D07          |
| 2    | `pats.tbl_Orders`                     | Orders     | **06:14:06**   | 00:22:10                | VBRP         |
| 3    | `pats.tbl_UAResults`                  | UA/Lab     | **02:46:48**   | 00:11:51                | B35          |
| 4    | `pats.tbl_3pClaimNote`                | 3p Elig    | **01:07:51**   | 00:11:29                | VWBY         |
| 5    | `pats.Tbl_Appointments`               | Activity   | **01:00:01**   | 00:03:39                | D07          |
| 6    | `pats.tbl_PACounselorReview`          | PA Data    | **00:48:11**   | 00:04:06                | DRD-NOLA     |
| 7    | `pats.tbl_Enrollment`                 | Enrollment | **00:47:21**   | 00:01:50                | V9           |
| 8    | `pats.tbl_UASched`                    | UA/Lab     | **01:18:37**   | 00:10:04                | B26          |
| 9    | `pats.tbl_Bills`                      | Billing    | **00:26:40**   | 00:00:47                | DRD-KVC      |
| 10   | `pats.Tbl_AdmissionAssessmentSummary` | Assessment | **00:23:59**   | 00:00:52                | D09          |


---

## Key Observations


| Observation                               | Detail                                                                                                                                                                                                                                     |
| ----------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| **Biggest outlier**                       | `pats.tbl_dbo_FormAnswerSignatures` — 9+ hours total, one site (**D07**) alone took **1:11:03**. In current sequential system this single site holds up the pipeline for over an hour. In Fabric parallel, the wall-clock = 1:11 not 9:16. |
| **Orders via C# year routing**            | All years are loaded through one parent `pats.tbl_Orders` task which routes to year tables in C#. Total = 6:14; slowest site **VBRP** = 22:10.                                                                                             |
| **BAM tables — 0 rows but non-zero time** | `tbl_BAMForm` and `tbl_BAMScore` ran 115 sites, fetched 0 rows but still spent 7+ minutes total. These sites are connected to and queried but returned no data on this date.                                                               |
| **Global Ref tables — 1 site**            | `tbl_clinicalopiatewithdrawalscale`, `tbl_FeeSched`, `tbl_GlobalPayor`, `tbl_BriefAddictionMonitor` all show `Sites = 1` — they run once against SAMMSGLOBAL, not per clinic.                                                              |
| **Near-instant tables**                   | `ctrl.tbl_Clinic`, `ctrl.tbl_InvType`, `pats.tbl_Codes`, `pats.tbl_SERVICES`, `pats.tbl_CustomQuestions`, `pats.Tbl_OrientationChecklistNew`, `pats.tbl_GlobalPayor` all show 00:00:00 total. Either sub-second or zero rows.              |
| **Sites = 115 vs 116**                    | Some tables ran for 115 sites not 116 — one site either skipped, errored, or is not in scope for that table's schedule.                                                                                                                    |


---

## The Query Used

```sql
DECLARE @RunDate DATE = '2026-04-22';
-- DECLARE @RunDate DATE = CAST(GETDATE() - 1 AS DATE);

WITH SingleNight AS (
    SELECT
        TaskName,
        SiteCode,
        Duration,
        [RowCnt]      = [RowCount],
        [DurationSec] = DATEDIFF(SECOND, '00:00:00', TRY_CAST([Duration] AS TIME))
    FROM tsk.tbl_Tasks2
    WHERE
        [Status]           = 19
        AND [ParentTaskId] IS NOT NULL
        AND [WorkDate]     = @RunDate
        AND [Duration]     IS NOT NULL
        AND [Duration]     <> ''
        AND [Duration]     <> '0'
        AND LOWER([TaskName]) IN (
            'pats.tbl_orders',
            'pats.tbl_orders_2016', 'pats.tbl_orders_2017', 'pats.tbl_orders_2018',
            'pats.tbl_orders_2019', 'pats.tbl_orders_2020', 'pats.tbl_orders_2021',
            'pats.tbl_orders_2022', 'pats.tbl_orders_2023', 'pats.tbl_orders_2024',
            'pats.tbl_orders_2025', 'pats.tbl_orders_2026', 'pats.tbl_orders_2027',
            'pats.tbl_orders_2028',
            'pats.tbl_bills', 'pats.tbl_authbills',
            'pats.tbl_auths',
            'pats.tbl_3pelig', 'pats.tbl_3psetup', 'pats.tbl_3pclaimnote', 'pats.tbl_3parnote',
            'pats.tbl_clientdemo1', 'pats.tbl_clientdemo2', 'pats.tbl_clientdemo3',
            'pats.tbl_uaresults', 'pats.tbl_uasched', 'pats.tbl_labresult',
            'pats.tbl_admissionassessment', 'pats.tbl_admissionassessmentsummary',
            'pats.tbl_admissionassessmentdimensionfour',
            'pats.tbl_admissionassessmentdimensiononeisorder',
            'pats.tbl_admissionassessmentdimensiontwo',
            'pats.tbl_admissionassessmentdimensionthree',
            'pats.tbl_admissionassessmentdimensionfivesubstanceuse',
            'pats.tbl_admissionassessmentdimensionsix',
            'pats.tbl_admissionassessmentsubstanceusehistory',
            'pats.tbl_reassessment', 'pats.tbl_reassessmentoccupational',
            'pats.tbl_reassessmentfamily', 'pats.tbl_reassessmentlegal',
            'pats.tbl_reassessmentmentalhealth', 'pats.tbl_reassessmentphysicalhealth',
            'pats.tbl_reassessmentsubstanceuse', 'pats.tbl_reassessmentsocial',
            'pats.tbl_reassessmenttreatment', 'pats.tbl_assessmentsubstanceusehistory',
            'ayx.tbl_preadmission_v6', 'pats.tbl_preadmission_referrals',
            'pats.tbl_pa', 'pats.tbl_pacounselorreview', 'pats.tbl_financialhardshipapplication',
            'pats.tbl_dropdownlistitems',
            'pats.tbl_padimension1', 'pats.tbl_padimension2', 'pats.tbl_padimension3',
            'pats.tbl_padimension4', 'pats.tbl_padimension5', 'pats.tbl_padimension6',
            'pats.tbl_feesched', 'pats.tbl_globalpayor', 'pats.tbl_globaluser',
            'pats.tbl_globalusersite', 'pats.tbl_clinicalopiatewithdrawalscale',
            'pats.tbl_consents', 'pats.tbl_devices', 'pats.tbl_briefaddictionmonitor',
            'pats.tbl_services', 'pats.tbl_claimstatus', 'pats.tbl_codes', 'ctrl.tbl_clinic',
            'pats.tbl_bottle', 'ctrl.tbl_invtype', 'pats.tbl_orientationchecklistnew',
            'pats.tbl_dbo_formanswersignatures', 'pats.tbl_eandmformmdm',
            'pats.tbl_eandmformpregnancy', 'pats.tbl_comprehensiveassessmentform',
            'pats.tbl_customquestions', 'pats.tbl_customanswers',
            'pats.tbl_cows_v6', 'pats.tbl_mnca', 'pats.tbl_mncaloc',
            'pats.tbl_vaca', 'pats.tbl_vacasummary',
            'pats.tbl_bamform', 'pats.tbl_bamscore',
            'pats.tbl_enrollment', 'pats.tbl_checkin', 'pats.tbl_treatmentlevel',
            'pats.tbl_appointmentattend', 'pats.tbl_appointments',
            'pats.tbl_payerclient', 'pats.tbl_payerclthistory',
            'tsk.tbl_rowtrax', 'pats.tbl_fmp'
        )
),

Summary AS (
    SELECT
        TaskName,
        [Category]     = CASE LOWER(TaskName)
            WHEN 'pats.tbl_3parnote'      THEN '3p Elig'
            WHEN 'pats.tbl_3pclaimnote'   THEN '3p Elig'
            WHEN 'pats.tbl_3pelig'        THEN '3p Elig'
            WHEN 'pats.tbl_3psetup'       THEN '3p Elig'
            WHEN 'pats.tbl_appointments'  THEN 'Activity'
            WHEN 'pats.tbl_checkin'       THEN 'Activity'
            WHEN 'pats.tbl_appointmentattend' THEN 'Activity'
            WHEN 'pats.tbl_treatmentlevel'    THEN 'Activity'
            WHEN 'pats.tbl_admissionassessment'                          THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentsummary'                   THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentdimensionfour'             THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentdimensiononeisorder'       THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentdimensiontwo'              THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentdimensionthree'            THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentdimensionfivesubstanceuse' THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentdimensionsix'              THEN 'Assessment'
            WHEN 'pats.tbl_admissionassessmentsubstanceusehistory'       THEN 'Assessment'
            WHEN 'pats.tbl_reassessment'              THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentoccupational'  THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentfamily'        THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentlegal'         THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentmentalhealth'  THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentphysicalhealth' THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentsubstanceuse'  THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmentsocial'        THEN 'ReAssessment'
            WHEN 'pats.tbl_reassessmenttreatment'     THEN 'ReAssessment'
            WHEN 'pats.tbl_assessmentsubstanceusehistory' THEN 'ReAssessment'
            WHEN 'pats.tbl_bamform'  THEN 'BAM'
            WHEN 'pats.tbl_bamscore' THEN 'BAM'
            WHEN 'pats.tbl_bills'    THEN 'Billing'
            WHEN 'pats.tbl_authbills' THEN 'Billing'
            WHEN 'pats.tbl_cows_v6'  THEN 'Clinical'
            WHEN 'pats.tbl_mnca'     THEN 'Clinical'
            WHEN 'pats.tbl_mncaloc'  THEN 'Clinical'
            WHEN 'pats.tbl_vaca'     THEN 'Clinical'
            WHEN 'pats.tbl_vacasummary' THEN 'Clinical'
            WHEN 'pats.tbl_enrollment'  THEN 'Enrollment'
            WHEN 'pats.tbl_fmp'         THEN 'Financial'
            WHEN 'pats.tbl_dbo_formanswersignatures'    THEN 'Forms'
            WHEN 'pats.tbl_eandmformmdm'                THEN 'Forms'
            WHEN 'pats.tbl_eandmformpregnancy'          THEN 'Forms'
            WHEN 'pats.tbl_comprehensiveassessmentform' THEN 'Forms'
            WHEN 'pats.tbl_customquestions'             THEN 'Forms'
            WHEN 'pats.tbl_customanswers'               THEN 'Forms'
            WHEN 'pats.tbl_feesched'                         THEN 'Global Ref'
            WHEN 'pats.tbl_globalpayor'                      THEN 'Global Ref'
            WHEN 'pats.tbl_globaluser'                       THEN 'Global Ref'
            WHEN 'pats.tbl_globalusersite'                   THEN 'Global Ref'
            WHEN 'pats.tbl_clinicalopiatewithdrawalscale'    THEN 'Global Ref'
            WHEN 'pats.tbl_consents'                         THEN 'Global Ref'
            WHEN 'pats.tbl_devices'                          THEN 'Global Ref'
            WHEN 'pats.tbl_briefaddictionmonitor'            THEN 'Global Ref'
            WHEN 'pats.tbl_services'                         THEN 'Global Ref'
            WHEN 'pats.tbl_claimstatus'                      THEN 'Global Ref'
            WHEN 'pats.tbl_codes'                            THEN 'Global Ref'
            WHEN 'ctrl.tbl_clinic'                           THEN 'Global Ref'
            WHEN 'pats.tbl_bottle'               THEN 'Inventory'
            WHEN 'ctrl.tbl_invtype'              THEN 'Inventory'
            WHEN 'pats.tbl_orientationchecklistnew' THEN 'Inventory'
            WHEN 'pats.tbl_orders'      THEN 'Orders'
            WHEN 'pats.tbl_orders_2016' THEN 'Orders'
            WHEN 'pats.tbl_orders_2017' THEN 'Orders'
            WHEN 'pats.tbl_orders_2018' THEN 'Orders'
            WHEN 'pats.tbl_orders_2019' THEN 'Orders'
            WHEN 'pats.tbl_orders_2020' THEN 'Orders'
            WHEN 'pats.tbl_orders_2021' THEN 'Orders'
            WHEN 'pats.tbl_orders_2022' THEN 'Orders'
            WHEN 'pats.tbl_orders_2023' THEN 'Orders'
            WHEN 'pats.tbl_orders_2024' THEN 'Orders'
            WHEN 'pats.tbl_orders_2025' THEN 'Orders'
            WHEN 'pats.tbl_orders_2026' THEN 'Orders'
            WHEN 'pats.tbl_orders_2027' THEN 'Orders'
            WHEN 'pats.tbl_orders_2028' THEN 'Orders'
            WHEN 'pats.tbl_pa'                          THEN 'PA Data'
            WHEN 'pats.tbl_pacounselorreview'           THEN 'PA Data'
            WHEN 'pats.tbl_financialhardshipapplication' THEN 'PA Data'
            WHEN 'pats.tbl_dropdownlistitems'           THEN 'PA Data'
            WHEN 'pats.tbl_padimension1'                THEN 'PA Data'
            WHEN 'pats.tbl_padimension2'                THEN 'PA Data'
            WHEN 'pats.tbl_padimension3'                THEN 'PA Data'
            WHEN 'pats.tbl_padimension4'                THEN 'PA Data'
            WHEN 'pats.tbl_padimension5'                THEN 'PA Data'
            WHEN 'pats.tbl_padimension6'                THEN 'PA Data'
            WHEN 'pats.tbl_payerclient'     THEN 'Payer'
            WHEN 'pats.tbl_payerclthistory' THEN 'Payer'
            WHEN 'ayx.tbl_preadmission_v6'          THEN 'Pre-Admission'
            WHEN 'pats.tbl_preadmission_referrals'  THEN 'Pre-Admission'
            WHEN 'pats.tbl_uaresults'   THEN 'UA/Lab'
            WHEN 'pats.tbl_uasched'     THEN 'UA/Lab'
            WHEN 'pats.tbl_labresult'   THEN 'UA/Lab'
            WHEN 'tsk.tbl_rowtrax'      THEN 'Audit'
            ELSE 'Other'
        END,
        [SitesRan]    = COUNT(1),
        [TotalDurSec] = SUM([DurationSec]),
        [AvgDurSec]   = AVG(CAST([DurationSec] AS FLOAT)),
        [MinDurSec]   = MIN([DurationSec]),
        [MaxDurSec]   = MAX([DurationSec]),
        [TotalRowCnt] = SUM(CAST([RowCnt] AS BIGINT)),
        [AvgRowCnt]   = AVG(CAST([RowCnt] AS FLOAT)),
        [MaxRowCnt]   = MAX(CAST([RowCnt] AS BIGINT))
    FROM SingleNight
    GROUP BY TaskName
),

-- ROW_NUMBER ensures one row per table even when multiple sites tie for MaxDurSec
SlowestSitePerTable AS (
    SELECT TaskName,
           [SlowestSite] = SiteCode
    FROM (
        SELECT
            s.TaskName,
            s.SiteCode,
            [rn] = ROW_NUMBER() OVER (
                       PARTITION BY s.TaskName
                       ORDER BY s.SiteCode    -- alphabetical pick on tie
                   )
        FROM SingleNight s
        INNER JOIN Summary agg
            ON  s.TaskName      = agg.TaskName
            AND s.[DurationSec] = agg.[MaxDurSec]
    ) ranked
    WHERE rn = 1
)

SELECT
    [RunDate]       = @RunDate,
    [TaskName]      = s.TaskName,
    [Category]      = s.[Category],
    [LoadType]      = 'EF Core Upsert',
    s.[SitesRan],
    s.[TotalRowCnt],

    [TotalDuration] = RIGHT('0' + CAST(s.[TotalDurSec] / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((s.[TotalDurSec] % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(s.[TotalDurSec] % 60 AS VARCHAR), 2),

    [AvgDuration]   = RIGHT('0' + CAST(CAST(s.[AvgDurSec] AS INT) / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((CAST(s.[AvgDurSec] AS INT) % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(CAST(s.[AvgDurSec] AS INT) % 60 AS VARCHAR), 2),

    [MinDuration]   = RIGHT('0' + CAST(s.[MinDurSec] / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((s.[MinDurSec] % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(s.[MinDurSec] % 60 AS VARCHAR), 2),

    [MaxDuration]   = RIGHT('0' + CAST(s.[MaxDurSec] / 3600 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST((s.[MaxDurSec] % 3600) / 60 AS VARCHAR), 2) + ':' +
                      RIGHT('0' + CAST(s.[MaxDurSec] % 60 AS VARCHAR), 2),

    [SlowestSite]   = ss.[SlowestSite],
    [AvgRowCnt]     = s.[AvgRowCnt],
    [MaxRowCnt]     = s.[MaxRowCnt]

FROM Summary s
LEFT JOIN SlowestSitePerTable ss ON s.TaskName = ss.TaskName
ORDER BY s.[Category], s.[TotalDurSec] DESC;
```

---

*Generated for BHG_DR ETL runtime analysis and Microsoft Fabric migration planning.*  
*`ROW_NUMBER()` on `SlowestSitePerTable` guarantees one row per table even when sites tie on MaxDurSec.*