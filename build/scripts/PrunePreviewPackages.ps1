#Requires -Version 7.4

<#
.SYNOPSIS
Deletes preview versions from GitHub Packages once they have served their purpose.

.DESCRIPTION
A preview is deleted once its release exists, or when it is older than -MaxAgeDays, unless it was promoted (a published
GitHub Release on its tag), is on nuget.org, or is the package's current version.  Releases are never deleted.  Tags are
never deleted: change detection and release notes depend on them.  See Get-PrunablePreviewVersion.

Needs a token that can read and delete the packages (GH_TOKEN), the version variables (for current versions), and the
repository's tags and releases.  Run with -WhatIf to list what would be deleted.
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter()]
    [string]    $Owner = (@($env:GITHUB_REPOSITORY_OWNER, 'matthew25187') | Where-Object { $_ } | Select-Object -First 1),

    [Parameter()]
    [int]       $MaxAgeDays = 180,

    [Parameter()]
    [ValidatePattern('^([A-Z0-9_]*)$')]
    [string]    $VariablePrefix = '',

    # Read variables from this JSON file (toJSON(vars)) instead of the GitHub API.
    [Parameter()]
    [string]    $VariablesFile,

    [Parameter()]
    [int]   $TraceLevel = 0
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionVariables/VersionVariables.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/PackageRegistry/PackageRegistry.psm1') -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$packageInfo = Get-PackageInfo
$variables = [string]::IsNullOrEmpty($VariablesFile) ? (Read-VersionVariableSet) : (Read-VersionVariableSet -SnapshotPath $VariablesFile)
$tags = @(& git tag --list 'v*')
$publishedReleaseTags = @((Invoke-Gh release list --limit 1000 --exclude-drafts --json 'tagName') -join "`n" | ConvertFrom-Json | Select-Object -ExpandProperty tagName)

$deleted = 0
foreach ($package in $packageInfo) {
    $packageName = $package.packageName
    try {
        $listed = @(Invoke-Gh api --paginate "/users/$Owner/packages/nuget/$packageName/versions" --jq '.[] | {id, name, created_at}')
    }
    catch {
        # Most often the package has never been published to GitHub Packages.
        Write-Verbose "No stored versions of ${packageName}: $($_.Exception.Message)"
        continue
    }
    $stored = @($listed | Where-Object { $_ } | ConvertFrom-Json | ForEach-Object {
        [pscustomobject]@{ Id = $_.id; Name = $_.name; CreatedAt = $_.created_at }
    })

    $nugetOrgVersions = @()
    try {
        $nugetOrgVersions = @((Invoke-RestMethod -Uri "https://api.nuget.org/v3-flatcontainer/$($packageName.ToLowerInvariant())/index.json" -ErrorAction Stop).versions)
    }
    catch {
        Write-Verbose "No nuget.org versions of ${packageName}: $($_.Exception.Message)"
    }

    $state = Get-PackageVersionState -PackageInfo $package -Variables $variables -Prefix $VariablePrefix
    $currentVersion = ConvertTo-SemanticVersion -Version $state.CurrentVersion -PrereleaseLabel $state.CurrentPrereleaseLabel

    $prunable = @(Get-PrunablePreviewVersion -PackageName $packageName -StoredVersion $stored -ReleaseTag $tags `
        -PromotedTag $publishedReleaseTags -NuGetOrgVersion $nugetOrgVersions -CurrentVersion $currentVersion -MaxAgeDays $MaxAgeDays)
    foreach ($candidate in $prunable) {
        if ($PSCmdlet.ShouldProcess("$packageName $($candidate.Name)", "Delete ($($candidate.Reason))")) {
            [void](Invoke-Gh api -X DELETE "/users/$Owner/packages/nuget/$packageName/versions/$($candidate.Id)")
            Write-Information "Deleted $packageName $($candidate.Name): $($candidate.Reason)."
            $deleted++
        }
    }
}

Write-Information "Deleted $deleted preview version$(($deleted -eq 1) ? [string]::Empty : 's')."
