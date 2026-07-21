# BHG Fabric Defect Trend - Shared Queries

These Azure DevOps queries are intended to be created under:

`Shared Queries > BHG Fabric Defect Trend`

The date filter starts from `2026-05-01` and does not have an end date. This means the queries will continue to sync as new defects are created, updated, or closed after May 1.

## Open Defects

Shows all defects that are currently not closed or removed.

```sql
SELECT
    [System.Id],
    [System.Title],
    [System.State],
    [System.AssignedTo],
    [Microsoft.VSTS.Common.Priority],
    [System.Parent]
FROM WorkItems
WHERE
    [System.TeamProject] = @project
    AND [System.WorkItemType] = 'Bug'
    AND [System.State] <> 'Closed'
    AND [System.State] <> 'Removed'
ORDER BY
    [Microsoft.VSTS.Common.Priority] ASC,
    [System.Id] ASC
```

## New Defects From May 1

Shows all defects created from May 1, 2026 onward.

```sql
SELECT
    [System.Id],
    [System.Title],
    [System.State],
    [System.AssignedTo],
    [System.CreatedDate],
    [Microsoft.VSTS.Common.Priority],
    [System.Parent]
FROM WorkItems
WHERE
    [System.TeamProject] = @project
    AND [System.WorkItemType] = 'Bug'
    AND [System.CreatedDate] >= '2026-05-01'
ORDER BY
    [System.CreatedDate] ASC
```

## Closed Defects From May 1

Shows all defects closed from May 1, 2026 onward.

```sql
SELECT
    [System.Id],
    [System.Title],
    [System.State],
    [System.AssignedTo],
    [Microsoft.VSTS.Common.ClosedDate],
    [Microsoft.VSTS.Common.Priority],
    [System.Parent]
FROM WorkItems
WHERE
    [System.TeamProject] = @project
    AND [System.WorkItemType] = 'Bug'
    AND [System.State] = 'Closed'
    AND [Microsoft.VSTS.Common.ClosedDate] >= '2026-05-01'
ORDER BY
    [Microsoft.VSTS.Common.ClosedDate] ASC
```

## All Defects From May 1

Shows all defects created from May 1, 2026 onward, whether they are open, active, resolved, or closed.

```sql
SELECT
    [System.Id],
    [System.Title],
    [System.State],
    [System.AssignedTo],
    [System.CreatedDate],
    [Microsoft.VSTS.Common.ClosedDate],
    [Microsoft.VSTS.Common.Priority],
    [System.Parent]
FROM WorkItems
WHERE
    [System.TeamProject] = @project
    AND [System.WorkItemType] = 'Bug'
    AND [System.CreatedDate] >= '2026-05-01'
ORDER BY
    [System.CreatedDate] ASC
```

## Active Defects By Parent

Shows currently open defects grouped by parent work item.

```sql
SELECT
    [System.Id],
    [System.Title],
    [System.State],
    [System.AssignedTo],
    [Microsoft.VSTS.Common.Priority],
    [System.Parent]
FROM WorkItems
WHERE
    [System.TeamProject] = @project
    AND [System.WorkItemType] = 'Bug'
    AND [System.State] <> 'Closed'
    AND [System.State] <> 'Removed'
    AND [System.Parent] <> ''
ORDER BY
    [System.Parent] ASC,
    [Microsoft.VSTS.Common.Priority] ASC
```

## Dashboard Widget Notes

After these queries are saved under Shared Queries, the dashboard tiles can point to the shared query IDs. Then the team can open and run the same queries, and the dashboard counts will stay synced from May 1 onward.
