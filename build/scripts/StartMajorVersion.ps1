#Requires -Version 7.4

<#
.SYNOPSIS
Starts a new major version of a package: increments the major number of its next version, or sets it to -Major, and resets
the rest to zero.

.DESCRIPTION
Run by the maintainer, through the Start major version workflow, just before merging a release into master.  Preview
builds then number the new major version's previews, and the release build publishes it.  Use -WhatIf to see the change
without writing it.
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    # The package, as an ID or short name such as File.CSV.
    [Parameter(Mandatory)]
    [string]    $Package,

    # Selects a separate copy of the version variables, such as SHADOW_.
    # The major number to start, above the next version's.  Without it, the next version's major number plus one.  For a new
    # package's first public release, to match the major version the other packages are at.
    [Parameter()]
    [ValidateRange(1, [int]::MaxValue)]
    [int]   $Major,

    [Parameter()]
    [ValidatePattern('^([A-Z0-9_]*)$')]
    [string]    $VariablePrefix = '',

    [Parameter()]
    [int]   $TraceLevel = 0
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionVariables/VersionVariables.psm1') -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$packageInfo = Get-PackageInfo
$names = @(Resolve-PackageName -Name $Package -PackageInfo $packageInfo)
if ($names.Count -ne 1) {
    throw "Name exactly one package; '$Package' names $($names.Count)."
}

$info = $packageInfo | Where-Object -Property packageName -EQ -Value $names[0]
$variables = Read-VersionVariableSet
$state = Get-PackageVersionState -PackageInfo $info -Variables $variables -Prefix $VariablePrefix

if ($null -ne (Get-ResumeNextVersion -PackageInfo $info -Variables $variables -Prefix $VariablePrefix)) {
    throw "$($info.packageName) has a patch series open.  Run the Start patch release workflow with resume-development first."
}

$majorArgument = $PSBoundParameters.ContainsKey('Major') ? @{ Major = $Major } : @{}
$next = Get-MajorStartVersion -StableVersion $state.StableVersion -NextVersion $state.NextVersion @majorArgument
Write-Information "$($info.packageName): next version $(ConvertTo-DisplayVersion -Version $state.NextVersion) becomes $(ConvertTo-DisplayVersion -Version $next)."

$written = @(Set-PackageVersionState -PackageInfo $info -Group 'Next' -Version $next -Variables $variables -Prefix $VariablePrefix -WhatIf:$WhatIfPreference)
if ($WhatIfPreference) {
    return
}
if ($written.Count -eq 0) {
    throw "$($info.packageName) has no ${VariablePrefix}NEXT_* variables to set the next version in."
}

$stored = (Get-PackageVersionState -PackageInfo $info -Variables (Read-VersionVariableSet) -Prefix $VariablePrefix).NextVersion
if ($stored -ne $next) {
    throw "Failed to set $($info.packageName) next version to $next; the stored next version is $stored."
}
Write-Information "Set $($info.packageName) next version to $stored."
