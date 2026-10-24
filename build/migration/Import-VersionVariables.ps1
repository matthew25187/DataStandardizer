#Requires -Version 7.4

<#
.SYNOPSIS
Creates the GitHub repository version variables from an export made by Export-AdoVariableGroups.ps1.

.DESCRIPTION
A one-off step of the move to GitHub Actions (#365).  For each package in build/packages.json, maps its Azure DevOps
version variables to DATASTANDARDIZER_<suffix>__* (see build/psmodules/VersionVariables) and sets them with gh, creating
them as needed.  Run it with -Prefix SHADOW_ for the trial copy, and again without a prefix at cut-over.

Needs gh signed in with permission to manage the repository's variables.  Use -WhatIf to list what would be set.

.EXAMPLE
./build/migration/Import-VersionVariables.ps1 -Path ado-versions.json -Prefix SHADOW_ -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter(Mandatory)]
    [string]    $Path,

    [Parameter()]
    [ValidatePattern('^([A-Z0-9_]*)$')]
    [string]    $Prefix = ''
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionVariables/VersionVariables.psm1') -Scope Local

$export = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
$count = 0
foreach ($package in Get-PackageInfo) {
    if (-not $export.ContainsKey($package.packageName)) {
        Write-Warning "$($package.packageName) is not in the export; skipped."
        continue
    }

    $variables = ConvertFrom-AdoVersionVariable -PackageInfo $package -AdoVariable $export[$package.packageName] -Prefix $Prefix
    if ($variables.Count -eq 0) {
        Write-Warning "$($package.packageName) has no version variables in the export; skipped."
        continue
    }

    foreach ($name in $variables.Keys) {
        if ($PSCmdlet.ShouldProcess($name, "Set to '$($variables[$name])'")) {
            [void](Invoke-Gh variable set $name --body $variables[$name])
            $count++
        }
    }
    Write-Host "$($package.packageName): $($variables.Count) variables."
}

Write-Host "Set $count variables."
