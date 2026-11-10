#Requires -Version 7.4

<#
.SYNOPSIS
Starts a patch series of a package's last release, or ends it by returning to the development line.

.DESCRIPTION
Run by the maintainer, through the Start patch release workflow, just before merging a release into master.

start: saves the package's next version, then sets it to the first unreleased patch of the last release, which is one
above the highest patch already tagged for that major and minor number.  The release build that publishes the patch then
reserves the following patch as the next version (see Add-ProductionVersion), so a series of patches needs no further runs.

resume-development: sets the next version back to the one start saved, and forgets it.  Run it once the patch series is
finished, so the next preview build does not number a patch.

Use -WhatIf to see the change without writing it.
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    # The package, as an ID or short name such as File.CSV.
    [Parameter(Mandatory)]
    [string]    $Package,

    [Parameter()]
    [ValidateSet('start', 'resume-development')]
    [string]    $Action = 'start',

    # Selects a separate copy of the version variables, such as SHADOW_.
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
$packageName = $info.packageName
$variables = Read-VersionVariableSet
$state = Get-PackageVersionState -PackageInfo $info -Variables $variables -Prefix $VariablePrefix
$resume = Get-ResumeNextVersion -PackageInfo $info -Variables $variables -Prefix $VariablePrefix

if ($Action -eq 'start') {
    if ($null -ne $resume) {
        throw "$packageName already has a patch series open, to return to $resume.  Run this workflow with resume-development to end it."
    }

    $tags = @(& git tag --list 'v*')
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to list the repository tags.'
    }

    $next = Get-PatchStartVersion -StableVersion $state.StableVersion -PackageName $packageName -TagName $tags
    $nextToSave = $state.NextVersion
}
else {
    if ($null -eq $resume) {
        throw "$packageName has no patch series open."
    }
    if ($state.NextVersion.Revision -ne 0) {
        Write-CiIssue -Type warning -Message "$packageName next version is $($state.NextVersion), not a release: a preview build has numbered a patch."
    }

    $next = $resume
}

Write-Information "$packageName ($Action): next version $(ConvertTo-DisplayVersion -Version $state.NextVersion) becomes $(ConvertTo-DisplayVersion -Version $next)."

if ($Action -eq 'start') {
    Set-ResumeNextVersion -PackageInfo $info -Version $nextToSave -Variables $variables -Prefix $VariablePrefix -WhatIf:$WhatIfPreference
}

try {
    $written = @(Set-PackageVersionState -PackageInfo $info -Group 'Next' -Version $next -Variables $variables -Prefix $VariablePrefix -WhatIf:$WhatIfPreference)
    if (-not $WhatIfPreference -and $written.Count -eq 0) {
        throw "$packageName has no ${VariablePrefix}NEXT_* variables to set the next version in."
    }
}
catch {
    # Leave no series open for a patch that was not set up.
    if ($Action -eq 'start' -and -not $WhatIfPreference) {
        Set-ResumeNextVersion -PackageInfo $info -Remove -Variables $variables -Prefix $VariablePrefix
    }
    throw
}

if ($Action -eq 'resume-development') {
    Set-ResumeNextVersion -PackageInfo $info -Remove -Variables $variables -Prefix $VariablePrefix -WhatIf:$WhatIfPreference
}

if ($WhatIfPreference) {
    return
}

$reread = Read-VersionVariableSet
$stored = (Get-PackageVersionState -PackageInfo $info -Variables $reread -Prefix $VariablePrefix).NextVersion
if ($stored -ne $next) {
    throw "Failed to set $packageName next version to $next; the stored next version is $stored."
}
Write-Information "Set $packageName next version to $stored."
