# `SaveComprehensiveAssessmentForm` — Transformation & Logic

**File:** `BCAppCode/BHG-DR-LIB/SaveFormQAData.cs`  
**Class:** `SaveData` (partial)  
**Lines:** 883–1984  
**What it does:** Reads Comprehensive Assessment Form data from SAMMS and upserts it into the Azure destination table `pats.tbl_comprehensiveassessmentform`. One row per assessment — captures the full clinical, social, demographic, trauma, legal, and recovery intake picture for a patient.

---

## Inputs

| Parameter | Type | Purpose |
|---|---|---|
| `tbl` | DataTable | Rows from SAMMS — one row per assessment form for this clinic |
| `sc` | string | SiteCode — identifies which clinic this run is for |
| `wrkdt` | DateTime | Passed in but **not used** inside this method |
| `db` | DbContext | EF Core database context — created internally if not passed in |

> No `f2p` (Forms2Process config) parameter. No form-type configuration. All records processed unconditionally.

---

## Output — RCodes

| Field | Meaning |
|---|---|
| `IsResult` | `true` = success, `false` = exception occurred |
| `RowsProcessed` | Total rows in the incoming DataTable |
| `RowsIns` | How many new rows were inserted |
| `RowsUpd` | How many existing rows were updated |
| `ExceptMsg` | Exception message if the run failed |
| `ExceptInnerMsg` | Inner exception detail if available |

---

## How This Differs From All Other Form Save Methods

| Aspect | Other Form Methods | SaveComprehensiveAssessmentForm |
|---|---|---|
| Pre-pass soft-reset | Yes (FormQA, Signatures) or No (EMForms) | **No pre-pass** |
| Per-column error handling | No — one exception kills the whole row | **Yes** — every column has its own try/catch |
| RowChkSum | Not used or commented out | **Read from source and stored** — but not used as an update gate |
| RowState type | int (0/1) or absent | **bool (`true`/`false`)** |
| `IsDeleted` and `RowState` | Set separately | **Set together in one line** — both assigned the same value simultaneously |
| `RowState` default | Set per row | **Set to `true` in the `sitecode` case** — default is always active |
| Column count | Up to 35 | **100+ columns** across 7 clinical domains |
| Match key | Single or 4–7 columns | **2 columns: `SiteCode + Id`** |

---

## Step-by-Step Logic

### Step 1 — Capture Run Timestamp

```
runDate = DateTime.Now
```

Captured once at method start. Set on every row via `LastModAt = runDate` when the `sitecode` column is processed. All rows written in the same run share the exact same timestamp.

---

### Step 2 — Load All Existing Azure Rows for This Clinic

All rows currently in `pats.tbl_comprehensiveassessmentform` for this `SiteCode` are loaded into memory. Two lists are maintained:

- `forms` — existing Azure rows for lookups
- `xforms` — staging list for new rows to batch-insert at the end

---

### Step 3 — No Pre-Pass

No soft-reset step. `RowState` is not pre-reset on existing rows before the upsert. The current `RowState` of each row is simply overwritten during the update based on what comes from the source.

---

### Step 4 — Map Each Source Row — Per-Column Try/Catch

This is the most important structural difference in this method. Every column's mapping is wrapped in its own individual `try/catch`:

```
for each column c in the row:
    try
    {
        switch (c.ColumnName.ToLower()) { ... map the field ... }
    }
    catch (Exception e)
    {
        Console.WriteLine(c.ColumnName)  ← logs the bad column name, then continues
    }
```

**What this means in practice:**
- If a single column fails to parse (e.g. a bool field arrives with an unexpected value), only that one column on that one row is skipped
- The rest of the row's columns continue to be mapped
- The row is still written to Azure with all successfully mapped fields
- The failed column name is printed to the console for debugging
- The entire run does NOT fail because of one bad field

This is the most fault-tolerant column mapping in the entire `SaveFormQAData.cs` file.

---

### Step 5 — Column Mapping: The `sitecode` Case (Sets System Fields)

When the `sitecode` column is encountered, three system fields are set in one shot:

| Field | Value Set | Notes |
|---|---|---|
| `SiteCode` | From source row | Source value used directly |
| `LastModAt` | `runDate` | Shared timestamp for this run |
| `RowChkSum` | `int.Parse(dr["rowchksum"])` | Read directly by name — not from the current column loop variable |
| `RowState` | `true` | **Default set here** — active unless `isdeleted` overrides it later |

> **Key point:** `RowChkSum` is read by directly accessing the `"rowchksum"` column in the row — regardless of which column the loop is currently on. This means `RowChkSum` is always set when `sitecode` is processed, before the `rowchksum` column case is even reached in the loop.

---

### Step 6 — Column Mapping: `isdeleted` (Sets Both RowState and IsDeleted Simultaneously)

```
ca.RowState = ca.IsDeleted = bool.Parse(dr[c.ColumnName].ToString())
```

This is a **dual assignment** — both fields receive the exact same bool value in a single statement:

| Source value | `IsDeleted` | `RowState` |
|---|---|---|
| `"True"` or `"true"` | `true` | `true` — **overrides the default set in `sitecode` case** |
| `"False"` or `"false"` | `false` | `false` |
| Empty string | Skipped entirely | Stays as `true` (default from `sitecode` case) |

> **This is unique** — `RowState` here is `true` = **deleted** and `false` = **active**. This is the opposite convention from `SaveFormQuestionAnswers` where `RowState = 1` means active. In this method, `IsDeleted = true` means deleted, so `RowState = true` also means deleted.

---

### Step 7 — All 100+ Columns Grouped by Domain

All columns follow this rule unless noted otherwise:
- **Bool fields:** `bool.Parse()` applied. Skipped if empty.
- **Int fields:** `int.Parse()` applied. Skipped if empty.
- **String fields:** Stored directly. Skipped if empty.
- **DateTime fields:** `DateTime.Parse()` applied. Skipped if empty.

#### Group A — Identity & System Fields

| Source Column | Destination Field | Type | Notes |
|---|---|---|---|
| `sitecode` | `SiteCode`, `LastModAt`, `RowChkSum`, `RowState` | — | Sets all 4 system fields; `RowState = true` default |
| `id` | `Id` | int | Primary key — part of composite match key |
| `preadmissionid` | `PreAdmissionId` | int | Skipped if empty |
| `clientid` | `ClientId` | int | Skipped if empty |
| `clientm4id` | `ClientM4Id` | string | Skipped if empty |
| `clientname` | `ClientName` | string | Skipped if empty |
| `dataformid` | `DataFormId` | int | Skipped if empty |
| `createdon` | `CreatedOn` | DateTime | Skipped if empty |
| `createdby` | `CreatedBy` | string | Skipped if empty |
| `modifiedon` | `ModifiedOn` | DateTime | Skipped if empty |
| `modifiedby` | `ModifiedBy` | string | Skipped if empty |
| `version` | `Version` | string | Skipped if empty |
| `isdeleted` | `IsDeleted` + `RowState` | bool | Dual assignment — both set to same value |

#### Group B — Demographics

| Source Column | Destination Field | What it captures |
|---|---|---|
| `ddlgender` | `DDLGender` | Gender identity dropdown selection |
| `ddltermsofgender` | `DDLTermsofGender` | Preferred terms of gender |
| `ddlsexualorientation` | `DDLSexualOrientation` | Sexual orientation |
| `ddlrelationshipstatus` | `DDLRelationshipStatus` | Relationship status |
| `ddlpreferredlanguage` | `DDLPreferredLanguage` | Preferred language |
| `hispanic` | `Hispanic` | Patient identifies as Hispanic |
| `nonhispanic` | `NonHispanic` | Patient identifies as non-Hispanic |
| `raceamericanindian` | `RaceAmericanIndian` | Race: American Indian / Alaska Native |
| `raceasian` | `RaceAsian` | Race: Asian |
| `raceblack` | `RaceBlack` | Race: Black / African American |
| `racenativehawaiian` | `RaceNativeHawaiian` | Race: Native Hawaiian / Pacific Islander |
| `raceother` | `RaceOther` | Race: Other |
| `raceothertxt` | `RaceOtherTxt` | Free-text description if race = Other |
| `racetwoormore` | `RaceTwoorMore` | Race: Two or more races |
| `racewhite` | `RaceWhite` | Race: White |
| `islgbt` | `IsLGBT` | Patient identifies as LGBTQ+ |
| `ispertainingbeinglgbt` | `IsPertainingBeingLGBT` | Has experienced trauma pertaining to being LGBTQ+ |
| `thosewhoarenotcisgender` | `ThoseWhoAreNotcisgender` | Free-text — notes on non-cisgender identity |
| `supportivesexualorientaion` | `SupportiveSexualOrientaion` | Free-text — who is supportive of orientation |
| `notsupportivesexualorientaion` | `NotSupportiveSexualOrientaion` | Free-text — who is not supportive |
| `ismakeyouuncomfortable` | `IsMakeYouUncomfortable` | Does sexual orientation make others uncomfortable |
| `culturalpreferencesforyourtreatment` | `CulturalPreferencesForYourTreatment` | Has cultural preferences for their treatment |

#### Group C — Employment & Education

| Source Column | Destination Field | What it captures |
|---|---|---|
| `ddlemploymentstatus` | `DDLEmploymentStatus` | Employment status dropdown |
| `ddlcurrentjob` | `DDLCurrentJob` | Current job type |
| `howlonghadcurrentjob` | `HowLongHadCurrentJob` | How long in current job (int — likely coded) |
| `affectedyouremployment` | `AffectedYourEmployment` | Substance use affected employment |
| `istrainingactivities` | `IsTrainingActivities` | In vocational training or activities |
| `isemploymentsituation` | `IsEmploymentSituation` | Employment situation is a concern |
| `ddlhighestgradecompleted` | `DDLHighestGradeCompleted` | Highest grade completed |
| `ddlwhatkindofschoolattend` | `DDLWhatKindOfSchoolAttend` | Type of school currently attending |
| `havehighschooldiploma` | `HaveHighSchoolDiploma` | Has high school diploma |
| `ishighschooldiplomaged` | `IsHighSchoolDiplomaGED` | Has GED instead of diploma |
| `isheldbackschool` | `IsHeldBackSchool` | Was held back a grade in school |
| `ismainstreamclasses` | `IsMainstreamClasses` | Attended mainstream classes |
| `isunderstandenglish` | `IsUnderstandEnglish` | Can understand English |
| `isreadwriteeffectively` | `IsReadWriteEffectively` | Can read and write effectively |
| `isfulltimestudent` | `IsFullTimeStudent` | Currently a full-time student |
| `isparttimestudent` | `IsPartTimeStudent` | Currently a part-time student |

#### Group D — Family History (Substance Use)

The family history section uses a parallel pattern — for each family member there is both a **checkbox** (did this person have substance use issues) and a **DDL dropdown** (what substance):

| Source Column | Destination Field | Type | Family Member |
|---|---|---|---|
| `checkfather` | `CheckFather` | bool | Father |
| `ddlcheckfather` | `DDLCheckFather` | int | Father (substance type) |
| `checkmother` | `CheckMother` | bool | Mother |
| `ddlcheckmother` | `DDLCheckMother` | int | Mother (substance type) |
| `checksibling` | `CheckSibling` | bool | Sibling |
| `ddlchecksibling` | `DDLCheckSibling` | int | Sibling (substance type) |
| `checkmaternalaunt` | `CheckMaternalAunt` | bool | Maternal Aunt |
| `ddlcheckmaternalaunt` | `DDLCheckMaternalAunt` | int | Maternal Aunt (substance type) |
| `checkmaternaluncle` | `CheckMaternalUncle` | bool | Maternal Uncle |
| `ddlcheckmaternaluncle` | `DDLCheckMaternalUncle` | int | Maternal Uncle (substance type) |
| `checkmaternalgrandfather` | `CheckMaternalGrandfather` | bool | Maternal Grandfather |
| `ddlcheckmaternalgrandfather` | `DDLCheckMaternalGrandfather` | int | Maternal Grandfather (substance type) |
| `checkmaternalgrandmother` | `CheckMaternalGrandmother` | bool | Maternal Grandmother |
| `ddlcheckmaternalgrandmother` | `DDLCheckMaternalGrandmother` | int | Maternal Grandmother (substance type) |
| `checkmaternalcousins` | `CheckMaternalCousins` | bool | Maternal Cousins |
| `ddlcheckmaternalcousins` | `DDLCheckMaternalCousins` | int | Maternal Cousins (substance type) |
| `checkpaternalaunt` | `CheckPaternalAunt` | bool | Paternal Aunt |
| `ddlcheckpaternalaunt` | `DDLCheckPaternalAunt` | int | Paternal Aunt (substance type) |
| `checkpaternaluncle` | `CheckPaternalUncle` | bool | Paternal Uncle |
| `ddlcheckpaternaluncle` | `DDLCheckPaternalUncle` | int | Paternal Uncle (substance type) |
| `checkpaternalgrandfather` | `CheckPaternalGrandfather` | bool | Paternal Grandfather |
| `ddlcheckpaternalgrandfather` | `DDLCheckPaternalGrandfather` | int | Paternal Grandfather (substance type) |
| `checkpaternalgrandmother` | `CheckPaternalGrandmother` | bool | Paternal Grandmother |
| `ddlcheckpaternalgrandmother` | `DDLCheckPaternalGrandmother` | int | Paternal Grandmother (substance type) |
| `checkpaternalcousins` | `CheckPaternalCousins` | bool | Paternal Cousins |
| `ddlcheckpaternalcousins` | `DDLCheckPaternalCousins` | int | Paternal Cousins (substance type) |
| `checkfamilydisorder` | `CheckFamilyDisorder` | bool | Family member had a disorder |
| `ddlactivesubstanceusers` | `DDLActiveSubstanceUsers` | int | Active substance users in household |
| `ddllivewithyou` | `DDLLiveWithYou` | int | Who lives with you |
| `ddlinfluencedrugs` | `DDLInfluenceDrugs` | int | Who influenced drug use |
| `familystruggledwithdrugalcoholproblems` | `FamilyStruggledWithDrugAlcoholProblems` | bool | Family has drug/alcohol history |

#### Group E — Social History & Recovery Support

| Source Column | Destination Field | What it captures |
|---|---|---|
| `ishaveanychildren` | `IsHaveAnyChildren` | Has children |
| `iscloserelationship` | `IsCloseRelationship` | Has a close relationship |
| `iscounttosupportyou` | `IsCountToSupportYou` | Has someone they can count on for support |
| `isfriendsrecovery` | `IsFriendsRecovery` | Has friends in recovery |
| `ispeersupportmeetings` | `IsPeerSupportMeetings` | Attends peer support meetings |
| `checkmeetings` | `CheckMeetings` | Recovery meetings as support source |
| `checkeveryone` | `CheckEveryone` | Gets support from everyone |
| `checkimmediatefamily` | `CheckImmediateFamily` | Gets support from immediate family |
| `checkextendedfamily` | `CheckExtendedFamily` | Gets support from extended family |
| `checkfriends` | `CheckFriends` | Gets support from friends |
| `checkclosefriendsonly` | `CheckCloseFriendsOnly` | Support from close friends only |
| `checkcoworkers` | `CheckCoworkers` | Support from coworkers |
| `checkpeoplework` | `CheckPeopleWork` | Support from people at work |
| `checkonline` | `CheckOnline` | Finds support online |
| `checknoone` | `CheckNoOne` | Has no support network |
| `checkfriendsyourselfrecovery` | `CheckFriendsYourselfRecovery` | Friends who are themselves in recovery |
| `findsupportyourselfinrecoveryother` | `FindSupportYourselfInRecoveryOther` | Other sources of recovery support |
| `socialhistoryproblemswithother` | `SocialHistoryProblemsWithOther` | Has social history problems with others |
| `substancesaffectedyourlife` | `SubstancesAffectedYourLife` | Free-text — how substances affected their life |
| `checkpersonalexperience` | `CheckPersonalExperience` | Learns best from personal experience |
| `checktalkitthrough` | `CheckTalkItThrough` | Learns best by talking it through |
| `checkverballyexplainittome` | `CheckVerballyExplainItToMe` | Learns best when verbally explained |
| `checkvisuallyshowme` | `CheckVisuallyShowMe` | Learns best when visually shown |
| `checktactilelyhandson` | `CheckTactilelyHandsOn` | Learns best hands-on / tactile |
| `haveyoueverreceivedservices` | `HaveYouEverReceivedServices` | int — has previously received treatment services |

#### Group F — Trauma History

| Source Column | Destination Field | What it captures |
|---|---|---|
| `experiencedanytraumaabuseneglect` | `ExperiencedAnytraumaAbuseNeglect` | Has experienced any trauma/abuse/neglect |
| `currentlyexperiencingabusenglectexploitation` | `CurrentlyExperiencingAbuseNglectExploitation` | Currently experiencing abuse/neglect/exploitation |
| `isabuseneglectgrowingup` | `IsAbuseNeglectGrowingUp` | Experienced abuse or neglect growing up |
| `isfeelingtraumatized` | `IsFeelingTraumatized` | Currently feeling traumatized |
| `anydifficultycopingwithtrauma` | `AnyDifficultyCopingWithTrauma` | int — difficulty level coping with trauma |
| `physicalabuse` | `PhysicalAbuse` | Experienced physical abuse |
| `verbalabuse` | `VerbalAbuse` | Experienced verbal abuse |
| `sexualabuse` | `SexualAbuse` | Experienced sexual abuse |
| `neglect` | `Neglect` | Experienced neglect |
| `captivity` | `Captivity` | Experienced captivity |
| `laborexploitation` | `LaborExploitation` | Experienced labor exploitation |
| `sexualexploitation` | `SexualExploitation` | Experienced sexual exploitation |
| `traumaother` | `TraumaOther` | Experienced other type of trauma |
| `traumarelatedtorace` | `TraumaRelatedtoRace` | Experienced trauma related to their race |
| `neglecttraumarelatedyourrace` | `NeglectTraumaRelatedYourRace` | Neglect/trauma related to race |
| `physicalabuseviolencecaptivityother` | `PhysicalAbuseViolenceCaptivityOther` | Physical abuse / violence / captivity / other combined |
| `sexualabuseassaultsexualexploitation` | `SexualAbuseAssaultSexualExploitation` | Sexual abuse / assault / exploitation combined |
| `verbalemotionalfinancialabuse` | `VerbalEmotionalFinancialAbuse` | Verbal / emotional / financial abuse combined |
| `obsevationofothers` | `ObsevationofOthers` | Trauma by observation of others being harmed |
| `alwaysfollowssafersexpracices` | `AlwaysFollowsSaferSexPracices` | Always follows safer sex practices |
| `issafersexpractices` | `IsSaferSexPractices` | Practices safer sex |

#### Group G — Legal & Military History

| Source Column | Destination Field | What it captures |
|---|---|---|
| `isincarcerated` | `IsIncarcerated` | Currently or previously incarcerated |
| `isarrested` | `IsArrested` | Has been arrested |
| `isopencourtcases` | `IsOpenCourtCases` | Has open court cases |
| `isopenwarrants` | `IsOpenWarrants` | Has open warrants |
| `probationorparole` | `ProbationorParole` | On probation or parole |
| `isdrugtreatmentcourt` | `IsDrugTreatmentCourt` | Involved in drug treatment court |
| `iscourtfines` | `IsCourtFines` | Has outstanding court fines |
| `ischildsupportpayments` | `IsChildSupportPayments` | Has child support payment obligations |
| `iscourtorderedchildsupportpayments` | `IsCourtOrderedChildSupportPayments` | Court-ordered child support specifically |
| `isarmedforces` | `IsArmedForces` | Has served in the armed forces |
| `isdeployoverseas` | `IsDeployOverseas` | Has been deployed overseas |
| `isveteransadministration` | `IsVeteransAdministration` | Receives VA services |
| `ddlwhatbranch` | `DDLWhatBranch` | Which branch of military |
| `ddlwhatbranchtype` | `DDLWhatBranchType` | Active / reserve / national guard |
| `ddltypedischarge` | `DDLTypeDischarge` | Type of military discharge |
| `checkeatingdisorders` | `CheckEatingDisorders` | Co-occurring eating disorder |
| `checkgamblingdisorder` | `CheckGamblingDisorder` | Co-occurring gambling disorder |
| `checkinternetaddiction` | `CheckInternetAddiction` | Co-occurring internet addiction |
| `checksocialmediaaddiction` | `CheckSocialMediaAddiction` | Co-occurring social media addiction |
| `checkloveintimacydependence` | `CheckLoveIntimacyDependence` | Co-occurring love/intimacy dependence |
| `checkfoodovereating` | `CheckFoodOvereating` | Co-occurring food/overeating issue |
| `iscareoffamilymembers` | `IsCareOfFamilyMembers` | Responsible for care of family members |

---

### Step 8 — Lookup: Does This Row Already Exist in Azure?

The method searches the in-memory snapshot using a **2-column composite key**:

```
SiteCode + Id
```

`Id` is unique per form within a clinic. `SiteCode` is needed because multiple clinics share the same Azure table.

---

### Step 9a — Row Found → UPDATE

All 100+ columns are updated unconditionally. There is no checksum gate — even though `RowChkSum` is stored, it is not compared before deciding whether to update. Every matched row is always fully refreshed.

Fields **never changed** on a matched row:

| Field NOT Updated | Why |
|---|---|
| `SiteCode` | Part of composite key |
| `Id` | Primary key |

---

### Step 9b — Row Not Found → INSERT (batched)

Added to `xforms` staging list. All new rows inserted together at the end via `AddRange`.

---

### Step 10 — Write Everything to the Database (Two Commits)

**Commit 1:** Saves all field updates.

**Commit 2:** Inserts all new rows via `AddRange`.

---

## Business Rules at a Glance

| Rule | Detail |
|---|---|
| **Per-column try/catch** | One bad column on one row logs to console and continues — the row is not abandoned |
| **RowChkSum read in `sitecode` case** | `dr["rowchksum"]` is read directly by name regardless of which column the loop is on — always set early |
| **`RowState = true` default** | Set when `sitecode` is processed — means active by default |
| **`IsDeleted` and `RowState` set together** | `ca.RowState = ca.IsDeleted = bool.Parse(...)` — one line, both fields |
| **`RowState = true` means deleted** | In this table, `RowState` mirrors `IsDeleted` — `true` = deleted. Opposite convention from `SaveFormQuestionAnswers` |
| **RowChkSum stored but not used as gate** | Every matched row is always fully updated regardless of whether data changed |
| **No pre-pass** | No rows are soft-reset before the upsert |
| **`SiteCode` from source row** | Not overridden by `sc` parameter |
| **All non-bool/int/date fields skipped if empty** | Consistent across all 100+ columns |
| **`runDate` shared across all rows** | Consistent `LastModAt` across the entire batch |
| **Match key is `SiteCode + Id`** | Simple 2-column key — `Id` is per-form unique within the clinic |
| **Inserts always batched** | `AddRange` + single `SaveChanges` |
| **Family member fields follow a paired pattern** | Each family member has a bool checkbox (did they use substances) and an int DDL (which substance) |

---

## What Is the Comprehensive Assessment Form?

This is the full clinical intake assessment completed at the start of treatment. It captures everything a clinician needs to understand the patient's background:

- **Who they are** — demographics, race, gender, language, relationship status
- **Their family** — which family members had substance use issues and what substances
- **Their support network** — who they can lean on, how they learn, recovery community
- **Their trauma history** — types of abuse, neglect, exploitation, trauma related to race or identity
- **Their legal situation** — arrests, warrants, court orders, probation, military service
- **Their education and employment** — current status, history, challenges
- **Their identity** — LGBTQ+ status, cultural preferences, self-identification

This data is used for treatment planning, compliance reporting, and outcomes research across all BHG clinics.
