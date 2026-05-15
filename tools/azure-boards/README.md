# Azure Boards Ticket Helper

This folder contains a small PowerShell helper for creating Azure Boards work items in:

- Organization: `https://dev.azure.com/SeanergyDigital`
- Project: `Behavioral Health Group`
- Default assignee: `harsha.t@seanergy.ai`

The script does not store the Azure DevOps token in the repo. Set it in your PowerShell session:

```powershell
$env:AZURE_DEVOPS_TOKEN = "DGViuc6hRJATTrKbD6bjD5ePUbxBHfMgm8caYg32Bm0I7e9QFHjTJQQJ99CEACAAAAAabJWuAAASAZDO2OzO"
```

Use a PAT scoped only to Work Items read/write.

## Create A Task Under A User Story

Pass the parent story ID, work item URL, or title text:

```powershell
.\tools\azure-boards\New-AzureBoardTicket.ps1 `
  -Parent "SaveFormQA" `
  -Type Task `
  -Title "Validate Form QA incremental load" `
  -Description "Check source-to-target row counts and document any mismatches."
```

## Create A Bug

```powershell
.\tools\azure-boards\New-AzureBoardTicket.ps1 `
  -Parent "SaveFormAnswerSignature" `
  -Type Bug `
  -Title "Fix missing signature date mapping" `
  -Description @"
Issue:
Signature date is not landing correctly for selected forms.

Expected:
Signature date should match SAMMS source data.

Notes:
Add test evidence after validating affected site/date range.
"@
```

## Useful Options

- `-AssignedTo "name@company.com"` overrides the default assignee.
- `-Priority 2` sets Azure DevOps priority when that field exists for the work item type.
- `-Tags "Fabric Migration; SaveFormQA"` adds tags.
- `-AreaPath "Behavioral Health Group\Some Area"` sets the area path.
- `-IterationPath "Behavioral Health Group\Sprint Name"` sets the iteration.
- `-WhatIf` prints the JSON patch without creating the ticket.

## How Codex Can Use This Later

You can tell Codex:

```text
Create a bug under SaveFormQA for the null FormLineID issue, assign to me, priority 2.
```

Codex can turn your notes into a clean Azure Boards title/description and run this helper.
