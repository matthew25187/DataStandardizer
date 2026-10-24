#Requires -Version 7.4

<#
.SYNOPSIS
Updates the stored versions at the end of a GitHub Actions run that reserved versions.

.DESCRIPTION
After a successful publication, records each published package's version as current, and as stable when it is a release.
After a failed or cancelled publication, restores each next version this run reserved, unless something has changed it
since.  See Get-FinalizeAction.  Every write is read back.
#>
param (
    # The result of the publication: 'success', or anything else ('failure', 'cancelled', 'skipped') for a failure.
    [Parameter(Mandatory)]
    [string]    $PublishResult,

    # The 'versions' output of ResolveVersionNumbers.ps1.
    [Parameter(Mandatory)]
    [string]    $EncodedPackageVersions,

    # The 'reserved' output of ResolveVersionNumbers.ps1.
    [Parameter(Mandatory)]
    [AllowEmptyString()]
    [string]    $EncodedReserved,

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
$versionsList = @(ConvertFrom-EncodedVersionList -EncodedValue $EncodedPackageVersions)
$reserved = @{}
$decodedReserved = ConvertFrom-EncodedVersionList -EncodedValue $EncodedReserved
if ($null -ne $decodedReserved) {
    foreach ($property in $decodedReserved.PSObject.Properties) {
        $reserved[$property.Name] = $property.Value
    }
}
if ($reserved.Count -eq 0) {
    Write-Information 'This run reserved no versions; nothing to finalize.'
    return
}

$publishSucceeded = $PublishResult -eq 'success'
Write-Information "Publication result: $PublishResult."

$variables = Read-VersionVariableSet
$storedNext = @{}
foreach ($packageName in $reserved.Keys) {
    $package = $packageInfo | Where-Object -Property packageName -EQ -Value $packageName
    $storedNext[$packageName] = (Get-PackageVersionState -PackageInfo $package -Variables $variables -Prefix $VariablePrefix).NextVersion
}

$actions = @(Get-FinalizeAction -PublishSucceeded $publishSucceeded -PackageVersions $versionsList -Reserved $reserved -StoredNext $storedNext)
foreach ($action in $actions) {
    if ($null -eq $action.Group) {
        Write-CiIssue -Type warning -Message "$($action.PackageName): $($action.Reason)."
        continue
    }

    $package = $packageInfo | Where-Object -Property packageName -EQ -Value $action.PackageName
    $setLabel = $action.Group -eq 'Current'
    $null = Set-PackageVersionState -PackageInfo $package -Group $action.Group -Version $action.Version `
        -PrereleaseLabel $action.PrereleaseLabel -SetPrereleaseLabel:$setLabel -Variables $variables -Prefix $VariablePrefix
}

# Read back every write.
$reread = Read-VersionVariableSet
$failures = @()
foreach ($action in $actions | Where-Object { $null -ne $_.Group }) {
    $package = $packageInfo | Where-Object -Property packageName -EQ -Value $action.PackageName
    $state = Get-PackageVersionState -PackageInfo $package -Variables $reread -Prefix $VariablePrefix
    $stored = $state."$($action.Group)Version"
    $expected = ($action.Group -eq 'Stable') ? [version]::new($action.Version.Major, $action.Version.Minor, $action.Version.Build) : $action.Version
    if ($stored -ne $expected) {
        $failures += "$($action.PackageName) $($action.Group.ToLowerInvariant()) version is $stored, expected $expected"
    }
    else {
        Write-Information "$($action.PackageName) $($action.Group.ToLowerInvariant()) version $($action.Reason): $stored."
    }
}

if ($failures.Count -gt 0) {
    throw "Failed to update stored versions: $($failures -join '; ')."
}
