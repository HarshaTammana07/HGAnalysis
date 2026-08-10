# P1 Reference Bronze Site Success Optimization Review

## Context

P1 Reference has 9 source methods/tables. The current site-level failure handling uses one success-marker Copy activity per method and writes into a marker table such as:

```text
bhg_bronze.P1Reference.br_p1_reference_site_success
```

This works, but it adds:

- 9 extra marker Copy activities.
- 1 extra marker table for the ETL.
- Extra pipeline visual complexity and maintenance.

The lead question is valid: can we avoid these extra marker activities and still support site-level Bronze success/failure handling?

## Option 1: Infer Success From Bronze Rows

Use each Bronze table itself to determine which sites succeeded.

For each method/table, Silver reads the Bronze table for the current run:

```sql
SELECT DISTINCT SiteCode
FROM <bronze_table>
WHERE IngestRunId = '<current_ingest_run_id>';
```

Then Silver processes only those sites.

### Benefits

- No separate marker table.
- No 9 marker Copy activities.
- Simpler parent and child pipeline layout.
- Faster and easier to maintain.
- Works well when a successful site normally writes at least one Bronze row.

### Audit Behavior

Control/audit can still work with this approach:

- Site has Bronze rows for the current run: mark as `SUCCESS`.
- Site has no Bronze rows for the current run: mark as `FAILED` or `SKIPPED`.
- Silver processes only the successful Bronze sites.
- If one site fails but others succeed, the successful sites can still proceed to Silver.

### Limitation

The limitation is zero-row success.

If a site copy succeeds but legitimately returns 0 source rows for the current run/lookback, Bronze will have no rows. With this approach, audit cannot distinguish:

```text
Site succeeded with 0 rows
```

from:

```text
Site failed before writing rows
```

So that site may be treated as `FAILED` or `SKIPPED`, even though the copy technically succeeded.

## Option 2: Marker Rows Inside the Same Bronze Table

Instead of a separate marker table, write a lightweight marker row into the same Bronze table when a site succeeds.

Example design:

```text
IsMarker = 0 for business rows
IsMarker = 1 for success marker rows
```

Silver then filters:

```sql
WHERE IsMarker = 0
```

Audit reads:

```sql
WHERE IsMarker = 1
```

### Benefits

- No separate marker table.
- No 9 extra marker Copy activities.
- Correctly captures successful zero-row sites.

### Tradeoff

- Bronze table contains non-business marker rows.
- Every downstream process must consistently filter marker rows.
- DQ and count logic must exclude marker rows.

## Option 3: One Generic Post-Bronze Notebook

Run one notebook after all Bronze ForEach blocks. That notebook reads the Bronze tables and builds site-level success/failure JSON for all 9 methods.

### Benefits

- Avoids 9 marker Copy activities.
- Centralizes site success detection logic.
- Keeps pipeline layout cleaner.

### Limitation

Like Option 1, it still cannot perfectly identify successful zero-row sites unless it has another source of copy activity status or marker metadata.

## Recommendation

Use **Option 1** if zero-row successful sites can be treated as `SKIPPED` or `NO_DATA`.

Use **Option 2** if the business/audit requirement is:

```text
A site that succeeds with 0 rows must still be audited as SUCCESS.
```

For P1 Reference, the cleanest optimization is to remove the 9 marker Copy activities and infer successful sites from Bronze rows, provided the team accepts the zero-row caveat.

If exact zero-row success tracking is required, keep a marker mechanism, but avoid a separate marker table by using marker rows inside the Bronze table with strong filtering rules.

## Required Changes If Marker Table Is Removed

If the `br_p1_reference_site_success` marker table is removed, the change is not limited to Silver processing. The audit notebook must also be updated.

Required changes:

1. Remove or disable the 9 `mk_*_site_success` Copy activities in the child Bronze pipeline.
2. Remove dependency on `bhg_bronze.P1Reference.br_p1_reference_site_success`.
3. Update Silver notebooks to infer successful Bronze sites from the Bronze business rows for the current run.
4. Update the audit notebook to infer successful Bronze sites from the configured Bronze table instead of reading the success-marker table.
5. Use taskconfig `RequestBody` metadata to drive this dynamically:

```json
{
  "full_table": "bhg_bronze.<schema>.<bronze_table>",
  "ingest_column": "IngestRunId",
  "site_column": "SiteCode"
}
```

Audit/Silver success inference should follow this pattern:

```sql
SELECT DISTINCT SiteCode
FROM <bronze_table>
WHERE <ingest_column> = '<current_ingest_run_id>';
```

## Accepted Behavior With 0-Row Sites

With the marker table removed, a site that successfully runs but returns 0 source rows will not write anything to Bronze.

That means the audit notebook has no Bronze-row proof that the site succeeded. Under this optimized design, that site may be shown as:

```text
FAILED, SKIPPED, or NO_DATA
```

depending on the final status wording we choose.

This is acceptable only if the team agrees that Bronze-row presence is the source of truth for site success.

In other words:

```text
Site has current-run Bronze rows    -> SUCCESS
Site has no current-run Bronze rows -> FAILED/SKIPPED/NO_DATA
```

If this accepted behavior is approved, the optimized approach remains aligned and avoids the extra marker table and 9 marker Copy activities.

## Change Impact

This is a medium-sized change, not a full redesign.

The Reference framework already has most of the required dynamic metadata in taskconfig, and the Silver notebooks already use Bronze-row data as part of processing. The main work is removing the marker dependency cleanly.

### Areas That Need Changes

| Area | Change Size | Required Change |
|---|---:|---|
| Child Bronze pipeline | Medium | Remove or disable the 9 `mk_*_site_success` Copy activities. |
| Audit notebook | Medium | Replace marker-table lookup with Bronze-table success inference using taskconfig `RequestBody`. |
| Silver notebooks | Low/Medium | Remove marker-table fallback and use current-run Bronze rows to identify successful sites. |
| Taskconfig | Low | Confirm all Bronze rows have correct `RequestBody.full_table`, `ingest_column`, and `site_column`. |
| Pipeline JSON dependencies | Medium | Clean up dependencies connected to marker activities. Disable first if safer than deleting. |

### Main Audit Notebook Change

Current audit logic reads the marker table:

```python
reference_site_success_table = "bhg_bronze.P1Reference.br_p1_reference_site_success"
```

New audit logic should read the Bronze table for each method dynamically:

```python
full_table = request_body["full_table"]
ingest_column = request_body["ingest_column"]
site_column = request_body["site_column"]
```

Then infer successful sites:

```sql
SELECT DISTINCT <site_column>
FROM <full_table>
WHERE <ingest_column> = '<current_ingest_run_id>'
```

### Risk Level

The largest risk is not code volume. The largest risk is the business decision for 0-row successful sites.

Once the team accepts that a site with no current-run Bronze rows can be reported as `FAILED`, `SKIPPED`, or `NO_DATA`, the implementation is straightforward.

## Review Decision Needed

The team should confirm this requirement before implementation:

```text
Should a site that returns 0 rows but copies successfully be audited as SUCCESS, SKIPPED, or NO_DATA?
```

That answer determines whether Option 1 is enough or whether a marker-based pattern is still required.
