# BHG - Fabric Migration Defect Trend

Period: May 1, 2026 through June 30, 2026  
Azure DevOps project: Behavioral Health Group  
Board/team scope: BHG - Fabric Migration  
Work item type: Bug

## Executive Summary

From May 1 through June 30, the BHG - Fabric Migration board had 91 new defects and 81 closed defects. The period ended with 10 open defects.

Overall closure rate for the period was 89.0 percent, calculated as closed defects divided by new defects during the same window.

The biggest defect inflow happened in Week 3, with 23 new defects and only 3 closures. The strongest closure week was Week 9, with 22 closures against 4 new defects, reducing the open defect count from 28 to 10.

## Defect Trend By Week

| Week | Date Range | New Defects | Closed Defects | Net Change | Open Defects At Week End |
|---|---:|---:|---:|---:|---:|
| Week 1 | May 1 - May 7 | 0 | 0 | 0 | 0 |
| Week 2 | May 8 - May 14 | 2 | 0 | +2 | 2 |
| Week 3 | May 15 - May 21 | 23 | 3 | +20 | 22 |
| Week 4 | May 22 - May 28 | 7 | 0 | +7 | 29 |
| Week 5 | May 29 - Jun 4 | 8 | 14 | -6 | 23 |
| Week 6 | Jun 5 - Jun 11 | 13 | 17 | -4 | 19 |
| Week 7 | Jun 12 - Jun 18 | 13 | 5 | +8 | 27 |
| Week 8 | Jun 19 - Jun 25 | 21 | 20 | +1 | 28 |
| Week 9 | Jun 26 - Jun 30 | 4 | 22 | -18 | 10 |

## Key Observations

- Defects ramped up sharply in Week 3, likely reflecting deeper validation activity after initial migration work began.
- Weeks 5 and 6 show strong burn-down, with closures exceeding new defects.
- Week 7 added another backlog increase, with 13 new defects and 5 closures.
- Week 8 had high throughput on both sides: 21 new and 20 closed.
- Week 9 was the strongest stabilization week, closing 22 defects while only 4 new defects were opened.
- The final open backlog was 10 defects: 9 Active and 1 Resolved.

## Defect Backlog Movement

| Metric | Count |
|---|---:|
| Open defects at start of May 1 | 0 |
| New defects opened during period | 91 |
| Defects closed during period | 81 |
| Open defects at end of June 30 | 10 |
| Closure rate | 89.0% |

## Open Defects At Period End

| State | Count |
|---|---:|
| Active | 9 |
| Resolved | 1 |

## Top Parent Items By New Defects

| Parent ID | Parent Title | New Defects | Open Defects | Closed Defects |
|---:|---|---:|---:|---:|
| 2095 | Development: SAMMS-Forms | 16 | 0 | 16 |
| 2000 | Development: SAMMS-ETL-Orders | 13 | 1 | 12 |
| 2048 | Development: SAMMS-ETL-Dose | 12 | 0 | 12 |
| 985 | Development: CredentialStreamDemographicPayerContractsRefresh | 12 | 0 | 12 |
| 1089 | Development: 8x8CallInteractions_Python | 9 | 1 | 8 |
| 2592 | Development: SAMMS-ETL-PPA | 4 | 1 | 3 |
| 2543 | Development: SAMMS-ETL-Notes | 4 | 2 | 2 |
| 2591 | Development: SAMMS-ETL-DartSrv | 4 | 0 | 4 |
| 2006 | SaveDoseExcuse | 3 | 2 | 1 |
| 1760 | Development: UKG Clock Transactions | 3 | 1 | 2 |

## New Defects By ETL / Category

| Category | New Defects |
|---|---:|
| Other | 17 |
| ETL-006 | 9 |
| ETL-020 | 6 |
| ETL-005 | 6 |
| ETL-003 | 6 |
| ETL-002 | 6 |
| ETL-004 | 5 |
| ETL-023 | 4 |
| PY_ETL_004 | 4 |
| ETL_001 | 4 |
| ETL_005 | 3 |
| ETL-021 | 2 |

## Interpretation

The trend shows a typical migration validation curve. Defects increased heavily once table-by-table validation began, especially around SAMMS Forms, Orders, Dose, and external/reference ETLs. After that, the team started closing defects at a much faster pace, particularly in the final week of June.

The remaining backlog is small compared with the total defect volume found during the period. With 81 closures against 91 new defects, the project appears to be in a stabilization phase rather than an uncontrolled defect growth phase.

## Notes On Scope

This report uses Azure DevOps work items from the Behavioral Health Group project, scoped to the BHG - Fabric Migration team area path. It includes bugs created or closed between May 1, 2026 and June 30, 2026.
