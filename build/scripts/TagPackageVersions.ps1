#Requires -Version 7.4

<#
.SYNOPSIS
Tags the commit built with each published package version, v<version>-<Package>, and pushes the tags.

.DESCRIPTION
The tags are what change detection and release notes measure from, so they must be annotated and on the exact commit
built.  An existing tag is an error: it means the stored versions are out of step with the repository.
#>
param (
    # Packages to tag, as a JSON array or free-text list.
    [Parameter(Mandatory)]
    [string]    $PackageSet,

    # The 'versions' output of ResolveVersionNumbers.ps1.
    [Parameter(Mandatory)]
    [string]    $EncodedPackageVersions,

    # Create the tags locally but don't push them.
    [Parameter()]
    [switch]    $NoPush,

    [Parameter()]
    [int]   $TraceLevel = 0
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$packageInfo = Get-PackageInfo
$packageNames = ($PackageSet.TrimStart().StartsWith('[')) ? @($PackageSet | ConvertFrom-Json) : @(Resolve-PackageName -Name $PackageSet -PackageInfo $packageInfo)
$versionsList = @(ConvertFrom-EncodedVersionList -EncodedValue $EncodedPackageVersions)
$commit = Get-BuildCommit
$buildKind = (Test-ReleaseBuild) ? 'Release build' : 'CI preview build'

$tagNames = foreach ($packageName in $packageNames) {
    $versions = $versionsList | Where-Object -Property PackageName -EQ -Value $packageName | Select-Object -First 1
    if ($null -eq $versions) {
        throw "Versions for package $packageName not found."
    }

    [version]$version = $versions.PackageProductionVersion
    $tagName = ConvertTo-PackageTagName -Version $version -PackageName $packageName
    & git rev-parse --verify --quiet "refs/tags/$tagName" | Out-Null
    if ($LASTEXITCODE -eq 0) {
        throw "Tag $tagName already exists.  The stored versions of $packageName are out of step with the repository."
    }

    & git tag -a -m "$buildKind $($versions.PackageProductionVersionDisplay) for package $packageName" $tagName $commit
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create tag $tagName."
    }
    Write-Information "Tagged $commit as $tagName."
    $tagName
}

if (-not $NoPush -and @($tagNames).Count -gt 0) {
    & git push origin @($tagNames | ForEach-Object { "refs/tags/$_" })
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to push the tags.'
    }
    Write-Information "Pushed $(@($tagNames).Count) tag$((@($tagNames).Count -eq 1) ? [string]::Empty : 's')."
}
