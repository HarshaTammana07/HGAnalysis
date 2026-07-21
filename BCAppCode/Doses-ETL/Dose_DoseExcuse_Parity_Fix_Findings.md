# Dose / DoseExcuse Parity Fix Findings

This document captures parity checks between the Fabric Dose ETL and the legacy C# implementation, plus the current implementation status in `dosedefinistion.txt`.

## Files Reviewed

- `BCAppCode/BHGTaskRunner/Program.cs`
- `BCAppCode/BHG-DR-LIB/SaveDoses.cs`
- `BCAppCode/BHG-DR-LIB/SelectConstructor.cs`
- `BCAppCode/Framework/vw_mapAction.csv`
- `BCAppCode/Framework/vw_MapSrc2Dsn.csv`
- `BCAppCode/Doses-ETL/dosedefinistion.txt`

## High Level Conclusion

Dose and DoseExcuse are now aligned for the normal incremental path in the updated Fabric definition.

The main items fixed in `dosedefinistion.txt` are:

- Dose Bronze source `WHERE` condition now follows the hardcoded legacy override in `Program.cs`.
- Dose Silver `RowState` reset is limited to the successful sites and the active work-date/lookback window.
- Dose Silver applies the legacy void/client `RowState` false rules.
- Silver tables are normalized to clean business/final columns only, similar to Notes.
- Bronze site success is detected from marker rows in the normal Bronze tables; no separate success-marker tables are required.
- Control/audit now follows the Notes pattern with BR and SL as the active audited layers. Gold copy activities are retained but inactive for now.

Still open / intentionally not included:

- Legacy reload mode behavior is not implemented in this Fabric flow.
- Bronze source table names are still hardcoded in the child copy queries; `SourceTable` exists in taskconfig and can be used later if we want that extra dynamic behavior.

## Legacy Dose Source Logic

Legacy `Program.cs` does not simply use the `vw_mapAction.WhereCondition` for `pats.tbl_dose`. It overrides the source filter with special logic.

### Special EF Sites

Special sites:

```text
V10A, CBCO, V21, V10
```

Legacy normal-run source filter:

```sql
WHERE
    (
        YEAR(dtDate) >= YEAR(WorkDate + DaysBack - 1 year)
        OR YEAR(dtMedDate) >= YEAR(WorkDate + DaysBack - 1 year)
    )
    AND dtDate <= WorkDate + 2 days
    AND CltId IS NOT NULL
    AND dtDate >= WorkDate - 1 month
```

Legacy reload source filter:

```sql
WHERE CltID IS NOT NULL
  AND dtMedDate IS NOT NULL
```

### All Other Dose Sites

Legacy normal-run source filter:

```sql
WHERE
    (
        YEAR(dtDate) >= YEAR(WorkDate + DaysBack - 1 year)
        OR YEAR(dtMedDate) >= YEAR(WorkDate + DaysBack - 1 year)
    )
    AND dtDate <= WorkDate + 2 days
    AND CltId IS NOT NULL
    AND dtDate >= WorkDate - 6 months
```

Legacy reload source filter:

```sql
WHERE CltID IS NOT NULL
  AND dtMedDate IS NOT NULL
```

For non-special sites in reload mode, legacy also deletes existing target rows for that site before reloading:

```sql
DELETE FROM pats.tbl_dose
WHERE SiteCode = @SiteCode
```

## Previous Fabric Dose Source Logic

The previous Fabric Dose Bronze copy query used:

```sql
WHERE
    d.dtMedDate >= '1/1/2020'
    AND (
        d.dtMedDate <= CONVERT(date, @WorkDate)
        OR d.dtDate <= CONVERT(date, @WorkDate)
    )
```

That was not equivalent to legacy.

### Issues

- Missing `CltId IS NOT NULL`.
- Missing `dtDate <= WorkDate + 2 days`.
- Missing special-site rule for `V10A`, `CBCO`, `V21`, `V10`.
- Missing `dtDate >= WorkDate - 1 month` for special sites.
- Missing `dtDate >= WorkDate - 6 months` for other sites.
- Missing legacy year guard based on `WorkDate + DaysBack - 1 year`.
- `p_lookback_days` is used only for metadata `SourceQueryStartDate`, not for filtering source rows.
- Reload behavior is still not implemented.

## Current Fabric Dose Source Logic

The updated Dose Bronze copy query uses the legacy normal-run extraction rules:

```sql
WHERE
    (
        YEAR(d.dtDate) >= YEAR(DATEADD(year, -1, DATEADD(day, -@LookbackDays, @WorkDate)))
        OR YEAR(d.dtMedDate) >= YEAR(DATEADD(year, -1, DATEADD(day, -@LookbackDays, @WorkDate)))
    )
    AND d.dtDate <= DATEADD(day, 2, @WorkDate)
    AND d.CltId IS NOT NULL
    AND (
        (
            @SiteCode IN ('V10A', 'CBCO', 'V21', 'V10')
            AND d.dtDate >= DATEADD(month, -1, @WorkDate)
        )
        OR
        (
            @SiteCode NOT IN ('V10A', 'CBCO', 'V21', 'V10')
            AND d.dtDate >= DATEADD(month, -6, @WorkDate)
        )
    )
```

`SourceQueryStartDate` metadata is also based on `p_work_date - p_lookback_days`, not local run time.

## Dose SourceTable / Metadata Concern

`vw_mapAction` has this for ActionKey 1 / StepKey 13:

```text
FromTblVw      = tblDose
DsnTbl         = tbl_Dose
WhereCondition = dtMedDate >= '1/1/2020' and (dtMedDate <= @WorkDate or dtDate <= @WorkDate)
```

But legacy `Program.cs` overrides this specifically for `pats.tbl_dose`.

So for Dose, `vw_mapAction.WhereCondition` alone is not enough to reproduce legacy behavior.

## Legacy Dose Silver Logic

Legacy `SaveDoses.cs` behavior:

1. Load existing target rows for the current `SiteCode`.
2. Reset `RowState = false` only where:

```csharp
d.DtDate >= dtWrk.Date
```

3. Match records by:

```text
SiteCode + DoseId
```

4. Update business fields only if:

```text
new row OR RowChkSum changed
```

5. If checksum is unchanged, refresh only operational status fields.
6. Apply RowState business rules:

```text
if BlVoid == true and DtVoid == true -> RowState = false
if CltId < 0 and CltId != -111     -> RowState = false
```

## Previous Fabric Dose Silver Logic

The previous Fabric Dose Silver notebook reset all rows for the successful sites:

```python
condition = SiteCode IN (...)
RowState = false
```

That was too broad for Dose.

## Current Fabric Dose Silver Logic

The updated Dose Silver notebook resets only the active processing window:

```text
SiteCode in successful Bronze sites
AND DtDate >= WorkDate - LookbackDays
```

It then applies the legacy `RowState` false rules during source preparation and again after merge:

```text
BlVoid = true and DtVoid = true -> RowState = false
CltId < 0 and CltId != -111     -> RowState = false
```

The Silver table is also normalized to final business columns only:

```text
SiteCode, RowState, RowChkSum, LastModAt, DoseId, CltId, DtMedDate,
GuestId, DtDate, Dose, StrUser, BlVoid, StrVoidReason, BlException,
Bottletype, Ordernum, ExceptionReason, BlBulk, BlPrepack, Dtgiven,
Dtprep, DtVoid, Ppstaff, Exceptiontype, Manualauthdtm, Manualauthuser,
Dosenote, Dosesig, InventoryGroup, SiteId, DoseSigImg
```

## DoseExcuse Source Logic

`vw_mapAction` has this for ActionKey 1 / StepKey 14:

```text
FromTblVw      = tblDOSE_Excuse
DsnTbl         = tbl_Dose_Excuse
WhereCondition = 1 = 1
```

Current Fabric copy effectively pulls the full table for the site. That is aligned with the map action.

## Legacy DoseExcuse Silver Logic

Legacy `SaveDoseExcuse.cs` behavior:

1. Load all existing target rows for current `SiteCode`.
2. Reset all existing rows for that site:

```text
RowState = false
```

3. Match records by:

```text
SiteCode + ExId
```

4. Update business fields only if:

```text
new row OR RowChkSum changed
```

5. If checksum is unchanged, refresh:

```text
RowState = true
LastModAt = RunDT
```

Current Fabric DoseExcuse Silver reset by site is aligned with this behavior. It also normalizes Silver to final business columns only:

```text
SiteCode, RowChkSum, RowState, ExId, CltID, DtEx, StrExcused,
Dtstamp, StrUser, LastModAt
```

## Checksum Review

Checksum logic appears mostly aligned.

`SelectConstructor.cs` excludes the following field types from `CHECKSUM(...)`:

```text
ntext
varbinary
timestamp
```

Therefore:

- `DoseSigImg` is selected but should not be in `RowChkSum`.
- `StrExcused` is selected but should not be in `RowChkSum`.

Current Fabric excluding these from checksum is correct.

## Implementation Checklist

### Bronze Copy - Dose

- [x] Use legacy `Program.cs` Dose `WHERE` logic.
- [x] Add special-site branch for:

```text
V10A, CBCO, V21, V10
```

- [x] Add `CltId IS NOT NULL`.
- [x] Add `dtDate <= WorkDate + 2 days`.
- [x] Add `dtDate >= WorkDate - 1 month` for special sites.
- [x] Add `dtDate >= WorkDate - 6 months` for all other sites.
- [x] Add legacy year guard using `WorkDate + DaysBack - 1 year`.
- [ ] Decide whether reload behavior is required later; currently unsupported.

### Bronze Copy - DoseExcuse

- [x] No major WHERE change required; aligned to `vw_mapAction` (`1 = 1`).
- [ ] Optional cleanup: source table/existence check can come from taskconfig `SourceTable` instead of hardcoded table name.

### Silver - Dose

- [x] Change RowState reset from all site rows to only the legacy processing window.
- [x] Silver target is now final named and resolved from taskconfig: `bhg_silver.pats.tbl_dose`.
- [x] Apply void/client RowState logic:

```text
BlVoid = true and DtVoid = true -> RowState = false
CltId < 0 and CltId != -111     -> RowState = false
```

- [x] Continue matching by:

```text
SiteCode + DoseId
```

### Silver - DoseExcuse

- [x] Keep site-level RowState reset.
- [x] Silver target is now final named and resolved from taskconfig: `bhg_silver.pats.tbl_dose_excuse`.
- [x] Continue matching by:

```text
SiteCode + ExId
```

### Control Table / TaskConfig

- [x] Make sure Bronze request body has DQ keys:

```json
{
  "full_table": "bhg_bronze.Dose.br_tblDose",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["SiteCode", "DoseId"]
}
```

The helper script [update_dose_taskconfig_pyspark.py](./update_dose_taskconfig_pyspark.py) now updates:

- all Bronze site-level Dose/DoseExcuse rows for ConfigId `7`
- Silver non-site task rows for ConfigId `8` to final table names: `tbl_dose`, `tbl_dose_excuse`
- Gold non-site task rows for ConfigId `9`
- Gold `SourceTable` values to read from final Silver names if Gold is enabled later
- active and inactive matching rows, so toggled-off rows do not retain stale metadata

Use exact key casing:

```text
Dose       -> SiteCode + DoseId
DoseExcuse -> SiteCode + ExId
```

```json
{
  "full_table": "bhg_bronze.Dose.br_tblDoseExcuse",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode",
  "database_column": "SourceDatabase",
  "dq_keys": ["SiteCode", "ExId"]
}
```

### Control / Audit Notebook

- [x] `controlaudtdose.txt` is aligned to the Notes audit pattern.
- [x] Audit start creates BR/SL pipeline/task records only.
- [x] Audit finalize reads Bronze and Silver method result JSON, including site-level failures.
- [x] Bronze DQ filters out marker rows by using the configured business `dq_keys`.
- [x] Gold is not part of Dose audit finalization currently.

## Validation After Fix

Validate Dose and DoseExcuse separately.

### Dose Validation

For each active test site, compare source count using the corrected legacy filter against Fabric Bronze/Silver affected rows.

Also validate:

- `COUNT(*)`
- `COUNT(DISTINCT SiteCode + DoseId)`
- RowState active/inactive counts
- sample `RowChkSum` values
- rows where `CltId IS NULL`
- rows where `dtDate > WorkDate + 2`
- rows older than the 1-month/6-month source window

### DoseExcuse Validation

For each active test site, compare:

- source full table count
- Fabric Bronze count for current `IngestRunId`
- Fabric Silver count by `SiteCode`
- `COUNT(DISTINCT SiteCode + ExId)`
- sample `RowChkSum` values
- RowState counts

## Important Reminder

For Dose, do not trust only `vw_mapAction.WhereCondition`. The old code has a hardcoded override in `Program.cs`, and that override is the real legacy behavior to match.
