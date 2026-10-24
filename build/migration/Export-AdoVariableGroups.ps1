#Requires -Version 7.4

<#
.SYNOPSIS
Exports each package's version variables from its Azure DevOps variable group, for Import-VersionVariables.ps1.

.DESCRIPTION
A one-off step of the move to GitHub Actions (#365).  Reads the package-to-group mapping from
build/build-pipeline-vars.yml and writes { "<PackageId>": { "<variable>": "<value>", ... }, ... } to -Path.  The values
are version numbers and labels, not secrets, but keep the file out of the repository: the organisation and project are
deliberately not recorded here.

Needs the Azure CLI with the azure-devops extension, signed in to the organisation.

.EXAMPLE
./build/migration/Export-AdoVariableGroups.ps1 -Organization https://dev.azure.com/<org> -Project <project> -Path ado-versions.json
#>
param (
    [Parameter(Mandatory)]
    [string]    $Organization,

    [Parameter(Mandatory)]
    [string]    $Project,

    [Parameter(Mandatory)]
    [string]    $Path
)

$ErrorActionPreference = 'Stop'

$pipelineVars = Get-Content (Join-Path $PSScriptRoot '..' 'build-pipeline-vars.yml') -Raw
$entries = [regex]::Matches($pipelineVars, '(?m)^  - packageName: (?<name>\S+)\s*\r?\n    variableGroupId: (?<id>\d+)')
if ($entries.Count -eq 0) {
    throw 'No packages found in build/build-pipeline-vars.yml.'
}

$export = [ordered]@{}
foreach ($entry in $entries) {
    $packageName = $entry.Groups['name'].Value
    $groupId = $entry.Groups['id'].Value
    $listed = & az pipelines variable-group variable list --group-id $groupId --organization $Organization --project $Project --output json
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read the variable group of $packageName."
    }

    $variables = [ordered]@{}
    foreach ($property in ($listed -join "`n" | ConvertFrom-Json).PSObject.Properties) {
        $variables[$property.Name] = [string]$property.Value.value
    }
    $export[$packageName] = $variables
    Write-Host "$packageName`: $($variables.Count) variables."
}

ConvertTo-Json -InputObject $export -Depth 4 | Set-Content -LiteralPath $Path
Write-Host "Exported $($export.Count) packages to $Path."
