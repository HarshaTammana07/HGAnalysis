# ETL site to region mapping (BHG_DR)

**Purpose:** List every active clinic and its ETL group (Eastern / Central / Mountain / Pacific) used by `BHGTaskRunner.exe 2` (P1) and P2.

**Source of truth:** The live mapping is `ctrl.tbl_Locations` / `ctrl.vw_LocationCons` in Azure BHG_DR (`TimeZone` = `EST` | `CST` | `MST` | `PST`).

**Snapshot below:** Exported 2026 — use the SQL to refresh from the database at any time.

---

## 1. SQL: all active sites with region and clinic name

Run this in **BHG_DR** to get the current list (same structure as the tables below).

```sql
-- All active sites mapped to their ETL pipeline group
SELECT
    [Region]        = CASE l.TimeZone
                          WHEN 'EST' THEN 'Eastern'
                          WHEN 'CST' THEN 'Central'
                          WHEN 'MST' THEN 'Mountain'
                          WHEN 'PST' THEN 'Pacific'
                          ELSE 'Unknown / SAMMSGlobal'
                      END,
    [TimeZone]      = l.TimeZone,
    [SiteCode]      = l.SiteCode,
    [ClinicName]    = l.ClinicName,
    [SchemaVersion] = l.SchemaVersion,
    [IsActive]      = l.IsActive
FROM ctrl.vw_LocationCons l
WHERE l.IsActive = 1
  AND l.SiteCode NOT IN ('PHC', 'LAB', 'Global')   -- exclude special sites
ORDER BY
    CASE l.TimeZone
        WHEN 'EST' THEN 1
        WHEN 'CST' THEN 2
        WHEN 'MST' THEN 3
        WHEN 'PST' THEN 4
        ELSE 5
    END,
    l.SiteCode;
```

**Optional: counts only**

```sql
SELECT
    [Region]    = CASE TimeZone
                      WHEN 'EST' THEN 'Eastern ETL'
                      WHEN 'CST' THEN 'Central ETL'
                      WHEN 'MST' THEN 'Mountain ETL'
                      WHEN 'PST' THEN 'Pacific ETL'
                      ELSE 'SAMMSGlobal / No TZ'
                  END,
    [TimeZone]  = TimeZone,
    [SiteCount] = COUNT(1)
FROM ctrl.vw_LocationCons
WHERE IsActive = 1
  AND SiteCode NOT IN ('PHC', 'LAB', 'Global')
GROUP BY TimeZone
ORDER BY SiteCount DESC;
```

---

## 2. Summary (this snapshot)


| Region   | TimeZone | Site count |
| -------- | -------- | ---------- |
| Eastern  | EST      | 43         |
| Central  | CST      | 56         |
| Mountain | MST      | 12         |
| Pacific  | PST      | 4          |
| **All**  |          | **115**    |


---

## 3. Eastern (EST) — ETL P1 / P2 sites

*Scheduler assigns child tasks to `Eastern ETL P1` or `Eastern ETL P2` based on `TimeZone = 'EST'` and the destination table (P1 vs P2 list in `Scheduler/Program.cs`).*


| Region  | TimeZone | SiteCode | Clinic name              | Schema | Active |
| ------- | -------- | -------- | ------------------------ | ------ | ------ |
| Eastern | EST      | AHK      | Ahoskie                  | V6     | 1      |
| Eastern | EST      | B27      | Savannah                 | V6     | 1      |
| Eastern | EST      | B31      | Dyersburg                | V6     | 1      |
| Eastern | EST      | B36      | Asheville                | V6     | 1      |
| Eastern | EST      | B37      | Clyde                    | V6     | 1      |
| Eastern | EST      | B38      | Spartanburg              | V6     | 1      |
| Eastern | EST      | B39      | Aiken                    | V6     | 1      |
| Eastern | EST      | B41      | Chesapeake               | V6     | 1      |
| Eastern | EST      | B42      | Virginia Beach           | V6     | 1      |
| Eastern | EST      | B42A     | Newport News             | V6     | 1      |
| Eastern | EST      | B42B     | Franklin                 | V6     | 1      |
| Eastern | EST      | B42C     | Glen Allen               | V6     | 1      |
| Eastern | EST      | B42D     | Chesapeake South         | V6     | 1      |
| Eastern | EST      | B44      | Albany                   | V6     | 1      |
| Eastern | EST      | B45      | Tifton                   | V6     | 1      |
| Eastern | EST      | B52      | Jackson GA               | V6     | 1      |
| Eastern | EST      | B57      | Pawtucket                | V6     | 1      |
| Eastern | EST      | B57A     | Johnston                 | V6     | 1      |
| Eastern | EST      | B57B     | Middletown               | V6     | 1      |
| Eastern | EST      | B57C     | Providence               | V6     | 1      |
| Eastern | EST      | B57D     | Westerly                 | V6     | 1      |
| Eastern | EST      | B66A     | Bremen                   | V6     | 1      |
| Eastern | EST      | D07      | Medical Services Knox TN | V6     | 1      |
| Eastern | EST      | D08      | Madison                  | V6     | 1      |
| Eastern | EST      | D09      | Murfreesboro             | V6     | 1      |
| Eastern | EST      | DRD-KVB  | KVB                      | V6     | 1      |
| Eastern | EST      | DRD-KVC  | KVC                      | V6     | 1      |
| Eastern | EST      | ELC      | Elizabeth City           | V6     | 1      |
| Eastern | EST      | FW       | Fort Wayne               | V6     | 1      |
| Eastern | EST      | GAL      | Gaylord                  | V6     | 1      |
| Eastern | EST      | HGT      | Hagerstown               | V6     | 1      |
| Eastern | EST      | LAN      | Lansing                  | V6     | 1      |
| Eastern | EST      | MP       | Mt. Pleasant             | V6     | 1      |
| Eastern | EST      | NC       | North Charleston         | V6     | 1      |
| Eastern | EST      | STN      | Staunton                 | V6     | 1      |
| Eastern | EST      | V1       | Memphis South            | V6     | 1      |
| Eastern | EST      | V17      | Columbia TN              | V6     | 1      |
| Eastern | EST      | V19      | Jackson                  | V6     | 1      |
| Eastern | EST      | V20      | Paris                    | V6     | 1      |
| Eastern | EST      | V21      | Memphis North            | V6     | 1      |
| Eastern | EST      | V8       | Memphis Midtown          | V6     | 1      |
| Eastern | EST      | V9       | Nashville                | V6     | 1      |
| Eastern | EST      | WIL      | Wilson                   | V6     | 1      |


---

## 4. Central (CST) — ETL P1 / P2 sites


| Region  | TimeZone | SiteCode | Clinic name          | Schema | Active |
| ------- | -------- | -------- | -------------------- | ------ | ------ |
| Central | CST      | B24      | Paintsville          | V6     | 1      |
| Central | CST      | B25      | Pikeville            | V6     | 1      |
| Central | CST      | B26      | Hazard               | V6     | 1      |
| Central | CST      | B28      | West Plains          | V6     | 1      |
| Central | CST      | B29      | Poplar Bluff         | V6     | 1      |
| Central | CST      | B30      | KC North             | V6     | 1      |
| Central | CST      | B33      | Paducah              | V6     | 1      |
| Central | CST      | B34      | Corbin               | V6     | 1      |
| Central | CST      | B35      | Lexington            | V6     | 1      |
| Central | CST      | B35A     | Berea                | V6     | 1      |
| Central | CST      | B46      | Washington DC        | V6     | 1      |
| Central | CST      | B47      | Mobile               | V6     | 1      |
| Central | CST      | B48      | Tuscaloosa           | V6     | 1      |
| Central | CST      | B51      | N. Little Rock (OTP) | V6     | 1      |
| Central | CST      | B54      | Gadsden              | V6     | 1      |
| Central | CST      | B55      | Shoals               | V6     | 1      |
| Central | CST      | B72      | Mobile (OBOT)        | V6     | 1      |
| Central | CST      | B73      | Montgomery           | V6     | 1      |
| Central | CST      | B75      | Lawrence             | V6     | 1      |
| Central | CST      | B76      | Huntsville OBOT      | V6     | 1      |
| Central | CST      | BAT      | Batesville           | V6     | 1      |
| Central | CST      | BG       | Bowling Green        | V6     | 1      |
| Central | CST      | CON      | Conway               | V6     | 1      |
| Central | CST      | DA       | Davenport            | V6     | 1      |
| Central | CST      | DM       | Des Moines           | V6     | 1      |
| Central | CST      | DRD-CO   | Columbia MO          | V6     | 1      |
| Central | CST      | DRD-KC   | KC                   | V6     | 1      |
| Central | CST      | DRD-NOLA | NOLA                 | V6     | 1      |
| Central | CST      | DRD-SF   | Springfield          | V6     | 1      |
| Central | CST      | ET       | Elizabethtown        | V6     | 1      |
| Central | CST      | FAY      | Fayetteville         | V6     | 1      |
| Central | CST      | FR       | Frankfort            | V6     | 1      |
| Central | CST      | FS       | Fort Smith           | V6     | 1      |
| Central | CST      | HNT      | Huntsville           | V6     | 1      |
| Central | CST      | HS       | Hot Springs          | V6     | 1      |
| Central | CST      | JON      | Jonesboro            | V6     | 1      |
| Central | CST      | LO       | Louisville           | V6     | 1      |
| Central | CST      | MNRE     | Monroe               | V6     | 1      |
| Central | CST      | NLR      | N. Little Rock       | V6     | 1      |
| Central | CST      | RMD      | Richmond             | V6     | 1      |
| Central | CST      | SFN      | Springfield North    | V6     | 1      |
| Central | CST      | SHP      | Shreveport           | V6     | 1      |
| Central | CST      | STVN     | Stevenson            | V6     | 1      |
| Central | CST      | TEX      | Texarkana            | V6     | 1      |
| Central | CST      | TTCA     | Bessemer             | V6     | 1      |
| Central | CST      | TTCB     | Cullman              | V6     | 1      |
| Central | CST      | TTCC     | Grand Bay            | V6     | 1      |
| Central | CST      | V14      | Overland Park        | V6     | 1      |
| Central | CST      | V15      | Joplin               | V6     | 1      |
| Central | CST      | V5       | West Bank            | V6     | 1      |
| Central | CST      | V5B      | Houma                | V6     | 1      |
| Central | CST      | V6       | Lake Charles         | V6     | 1      |
| Central | CST      | VBRA     | Brainerd             | V6     | 1      |
| Central | CST      | VBRP     | Brooklyn Park        | V6     | 1      |
| Central | CST      | VMIN     | Minneapolis          | V6     | 1      |
| Central | CST      | VWBY     | Woodbury             | V6     | 1      |


> **Note on naming:** *Central* in this ETL view means `TimeZone = 'CST'`, not “central United States” as geography. Several northern sites (e.g. Minneapolis, Woodbury) are grouped with Central for ETL load balancing, not IANA time zone alone.

---

## 5. Mountain (MST) — ETL P1 / P2 sites


| Region   | TimeZone | SiteCode | Clinic name      | Schema | Active |
| -------- | -------- | -------- | ---------------- | ------ | ------ |
| Mountain | MST      | B12B     | Colorado Springs | V6     | 1      |
| Mountain | MST      | BOI      | Boise            | V6     | 1      |
| Mountain | MST      | CBCO     | Coeur d'Alene    | V6     | 1      |
| Mountain | MST      | MRD      | Meridian         | V6     | 1      |
| Mountain | MST      | PH       | Phoenix          | V6     | 1      |
| Mountain | MST      | TE       | Tempe            | V6     | 1      |
| Mountain | MST      | TU       | Tucson           | V6     | 1      |
| Mountain | MST      | V10      | Longmont         | V6     | 1      |
| Mountain | MST      | V10A     | Fort Collins     | V6     | 1      |
| Mountain | MST      | V11      | Westminster      | V6     | 1      |
| Mountain | MST      | V12      | Downtown Denver  | V6     | 1      |
| Mountain | MST      | V12A     | Centennial       | V6     | 1      |


---

## 6. Pacific (PST) — ETL P1 / P2 sites


| Region  | TimeZone | SiteCode | Clinic name | Schema | Active |
| ------- | -------- | -------- | ----------- | ------ | ------ |
| Pacific | PST      | LV1      | Cheyenne    | V6     | 1      |
| Pacific | PST      | LV2      | Desert Inn  | V6     | 1      |
| Pacific | PST      | LV3      | McDaniel    | V6     | 1      |
| Pacific | PST      | RE       | Reno        | V6     | 1      |


---

## 7. How this ties to BHGTaskRunner


| Arg | `BHGTaskRunner.exe` | Parent `TaskName` in `tsk.tbl_Tasks2`                                   |
| --- | ------------------- | ----------------------------------------------------------------------- |
| 2   | Regional **P1**     | `Eastern ETL P1`, `Central ETL P1`, `Mountain ETL P1`, `Pacific ETL P1` |
| 4   | Regional **P2**     | `Eastern ETL P2`, `Central ETL P2`, `Mountain ETL P2`, `Pacific ETL P2` |


All sites in the tables above with `TimeZone = EST` receive **Eastern** P1/P2 child tasks; `CST` → **Central**; `MST` → **Mountain**; `PST` → **Pacific**. Exact P1 vs P2 per *table* is still from `dms.tbl_MapAction` + the Scheduler `CASE` (see `Tables_By_Pipeline.md`).