param(
    [Parameter(Mandatory = $true)]
    [string]$Title,

    [Parameter(Mandatory = $true)]
    [string]$Description,

    [string]$Parent,

    [ValidateSet("Task", "Bug", "Issue")]
    [string]$Type = "Task",

    [string]$AssignedTo = "harsha.t@seanergy.ai",

    [string]$OrgUrl = "https://dev.azure.com/SeanergyDigital",

    [string]$Project = "Behavioral Health Group",

    [string]$AreaPath,

    [string]$IterationPath,

    [int]$Priority,

    [string]$Tags,

    [switch]$WhatIf
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function ConvertTo-Base64AuthHeader {
    param([Parameter(Mandatory = $true)][string]$Token)
    $bytes = [System.Text.Encoding]::ASCII.GetBytes(":$Token")
    return "Basic " + [Convert]::ToBase64String($bytes)
}

function ConvertTo-HtmlText {
    param([Parameter(Mandatory = $true)][string]$Text)
    $encoded = [System.Net.WebUtility]::HtmlEncode($Text)
    return ($encoded -replace "(`r`n|`n|`r)", "<br/>")
}

function Invoke-AzureDevOpsJson {
    param(
        [Parameter(Mandatory = $true)][string]$Method,
        [Parameter(Mandatory = $true)][string]$Uri,
        [Parameter(Mandatory = $true)][hashtable]$Headers,
        [object]$Body,
        [string]$ContentType = "application/json"
    )

    $params = @{
        Method      = $Method
        Uri         = $Uri
        Headers     = $Headers
        ContentType = $ContentType
    }

    if ($null -ne $Body) {
        $params.Body = $Body
    }

    return Invoke-RestMethod @params
}

function Get-WorkItemById {
    param(
        [Parameter(Mandatory = $true)][int]$Id,
        [Parameter(Mandatory = $true)][string]$OrgUrl,
        [Parameter(Mandatory = $true)][hashtable]$Headers
    )

    $uri = "$OrgUrl/_apis/wit/workitems/$Id`?api-version=7.1"
    return Invoke-AzureDevOpsJson -Method "GET" -Uri $uri -Headers $Headers
}

function Find-ParentWorkItem {
    param(
        [Parameter(Mandatory = $true)][string]$Parent,
        [Parameter(Mandatory = $true)][string]$OrgUrl,
        [Parameter(Mandatory = $true)][string]$Project,
        [Parameter(Mandatory = $true)][hashtable]$Headers
    )

    if ($Parent -match "^\d+$") {
        return Get-WorkItemById -Id ([int]$Parent) -OrgUrl $OrgUrl -Headers $Headers
    }

    if ($Parent -match "/workitems/edit/(\d+)") {
        return Get-WorkItemById -Id ([int]$Matches[1]) -OrgUrl $OrgUrl -Headers $Headers
    }

    $escapedProject = $Project.Replace("'", "''")
    $escapedParent = $Parent.Replace("'", "''")
    $wiql = @{
        query = @"
SELECT [System.Id], [System.Title], [System.WorkItemType]
FROM WorkItems
WHERE [System.TeamProject] = '$escapedProject'
  AND [System.WorkItemType] IN ('User Story', 'Feature', 'Epic')
  AND [System.Title] CONTAINS '$escapedParent'
ORDER BY [System.ChangedDate] DESC
"@
    } | ConvertTo-Json

    $projectSegment = [System.Uri]::EscapeDataString($Project)
    $uri = "$OrgUrl/$projectSegment/_apis/wit/wiql?api-version=7.1"
    $result = Invoke-AzureDevOpsJson -Method "POST" -Uri $uri -Headers $Headers -Body $wiql

    if (-not $result.workItems -or $result.workItems.Count -eq 0) {
        throw "No parent User Story/Feature/Epic found matching '$Parent'. Pass the exact work item ID or URL if the title search is ambiguous."
    }

    if ($result.workItems.Count -gt 1) {
        Write-Warning "Multiple parent work items matched '$Parent'. Using the most recently changed match: #$($result.workItems[0].id)."
    }

    return Get-WorkItemById -Id ([int]$result.workItems[0].id) -OrgUrl $OrgUrl -Headers $Headers
}

$token = $env:AZURE_DEVOPS_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    $token = [Environment]::GetEnvironmentVariable("AZURE_DEVOPS_TOKEN", "User")
}
if ([string]::IsNullOrWhiteSpace($token)) {
    $token = [Environment]::GetEnvironmentVariable("AZURE_DEVOPS_TOKEN", "Machine")
}
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "Set AZURE_DEVOPS_TOKEN to an Azure DevOps PAT with Work Items read/write permission before running this script."
}

$headers = @{
    Authorization = ConvertTo-Base64AuthHeader -Token $token
}

$patch = @(
    @{ op = "add"; path = "/fields/System.Title"; value = $Title },
    @{ op = "add"; path = "/fields/System.Description"; value = (ConvertTo-HtmlText -Text $Description) },
    @{ op = "add"; path = "/fields/System.AssignedTo"; value = $AssignedTo }
)

if (-not [string]::IsNullOrWhiteSpace($AreaPath)) {
    $patch += @{ op = "add"; path = "/fields/System.AreaPath"; value = $AreaPath }
}

if (-not [string]::IsNullOrWhiteSpace($IterationPath)) {
    $patch += @{ op = "add"; path = "/fields/System.IterationPath"; value = $IterationPath }
}

if ($Priority -gt 0) {
    $patch += @{ op = "add"; path = "/fields/Microsoft.VSTS.Common.Priority"; value = $Priority }
}

if (-not [string]::IsNullOrWhiteSpace($Tags)) {
    $patch += @{ op = "add"; path = "/fields/System.Tags"; value = $Tags }
}

if (-not [string]::IsNullOrWhiteSpace($Parent)) {
    $parentItem = Find-ParentWorkItem -Parent $Parent -OrgUrl $OrgUrl -Project $Project -Headers $headers
    $parentUrl = "$OrgUrl/_apis/wit/workItems/$($parentItem.id)"
    $patch += @{
        op    = "add"
        path  = "/relations/-"
        value = @{
            rel        = "System.LinkTypes.Hierarchy-Reverse"
            url        = $parentUrl
            attributes = @{ comment = "Linked to parent by New-AzureBoardTicket.ps1" }
        }
    }
}

$jsonPatch = $patch | ConvertTo-Json -Depth 10
$projectPath = [System.Uri]::EscapeDataString($Project)
$typePath = [System.Uri]::EscapeDataString($Type)
$createUri = "$OrgUrl/$projectPath/_apis/wit/workitems/`$$typePath`?api-version=7.1"

if ($WhatIf) {
    Write-Host "Would create Azure Boards $Type in project '$Project' assigned to '$AssignedTo'."
    if (-not [string]::IsNullOrWhiteSpace($Parent)) {
        Write-Host "Would link to parent #$($parentItem.id): $($parentItem.fields.'System.Title')"
    }
    $jsonPatch
    exit 0
}

$created = Invoke-AzureDevOpsJson `
    -Method "PATCH" `
    -Uri $createUri `
    -Headers $headers `
    -Body $jsonPatch `
    -ContentType "application/json-patch+json"

Write-Host "Created $Type #$($created.id): $($created.fields.'System.Title')"
Write-Host $created._links.html.href



#[Environment]::SetEnvironmentVariable("AZURE_DEVOPS_TOKEN", "NEW_TOKEN_HERE", "User")
