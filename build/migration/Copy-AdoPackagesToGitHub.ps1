#Requires -Version 7.4

<#
.SYNOPSIS
Copies each package's current version (and its stable version, where different) from the Azure Artifacts feed to
GitHub Packages.

.DESCRIPTION
A one-off step of the move to GitHub Actions (#365).  The versions come from the export of the Azure DevOps
version variables made during the migration.  CI reads each package's current version from GitHub Packages to learn its assembly
versions, so the copy must be done before the real version variables are imported at cut-over.  Older versions stay on
nuget.org where they were published.

Each package is downloaded with an Azure DevOps token (Packaging, read) and pushed with a GitHub personal access token
(classic) with write:packages; fine-grained tokens can't publish packages.  Versions already in GitHub Packages are skipped,
so the script can be re-run.  Use -WhatIf to list what would be copied.

After copying, check each package on GitHub: it should show "Connected repository: DataStandardizer" and be private.

.EXAMPLE
./build/migration/Copy-AdoPackagesToGitHub.ps1 -ExportPath ado-versions.json -AdoFeedUrl https://pkgs.dev.azure.com/<org>/<project>/_packaging/<feed>/nuget/v3/index.json -AdoToken $adoPat -GitHubToken $ghPat -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    # The export of the Azure DevOps version variables made during the migration.
    [Parameter(Mandatory)]
    [string]    $ExportPath,

    # The Azure Artifacts feed's NuGet v3 service index URL.
    [Parameter(Mandatory)]
    [string]    $AdoFeedUrl,

    [Parameter(Mandatory)]
    [string]    $AdoToken,

    [Parameter(Mandatory)]
    [string]    $GitHubToken,

    [Parameter()]
    [string]    $Owner = 'matthew25187'
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionVariables/VersionVariables.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/PackageRegistry/PackageRegistry.psm1') -Scope Local

$gitHubFeedUrl = "https://nuget.pkg.github.com/$Owner/index.json"
$export = Get-Content -LiteralPath $ExportPath -Raw | ConvertFrom-Json -AsHashtable
$folder = Join-Path ([System.IO.Path]::GetTempPath()) "ado-packages-$([guid]::NewGuid().ToString('N'))"
$copied = 0

try {
    foreach ($package in Get-PackageInfo) {
        if (-not $export.ContainsKey($package.packageName)) {
            Write-Warning "$($package.packageName) is not in the export; skipped."
            continue
        }

        $variables = [hashtable](ConvertFrom-AdoVersionVariable -PackageInfo $package -AdoVariable $export[$package.packageName])
        $state = Get-PackageVersionState -PackageInfo $package -Variables $variables
        $versions = [System.Collections.Generic.List[string]]::new()
        if ($state.CurrentVersion -ne [version]'0.0.0.0') {
            $versions.Add((ConvertTo-SemanticVersion -Version $state.CurrentVersion -PrereleaseLabel $state.CurrentPrereleaseLabel))
        }
        if ($state.StableVersion -ne [version]'0.0.0') {
            $stable = ConvertTo-SemanticVersion -Version ([version]"$($state.StableVersion).0")
            if (-not $versions.Contains($stable)) {
                $versions.Add($stable)
            }
        }
        if ($versions.Count -eq 0) {
            Write-Host "$($package.packageName): never released; nothing to copy."
            continue
        }

        foreach ($version in $versions) {
            $existing = Save-NuGetPackage -PackageId $package.packageName -Version $version -DestinationFolder (Join-Path $folder 'existing') `
                -Source @{ ServiceIndexUrl = $gitHubFeedUrl; Token = $GitHubToken }
            if ($existing) {
                Write-Host "$($package.packageName) $version is already in GitHub Packages; skipped."
                continue
            }

            if (-not $PSCmdlet.ShouldProcess("$($package.packageName) $version", 'Copy from Azure Artifacts to GitHub Packages')) {
                continue
            }

            $packagePath = Save-NuGetPackage -PackageId $package.packageName -Version $version -DestinationFolder $folder `
                -Source @{ ServiceIndexUrl = $AdoFeedUrl; Token = $AdoToken }
            if ($null -eq $packagePath) {
                throw "$($package.packageName) $version not found in the Azure Artifacts feed."
            }

            dotnet nuget push $packagePath --source $gitHubFeedUrl --api-key $GitHubToken
            if ($LASTEXITCODE -ne 0) {
                throw "Failed to push $($package.packageName) $version to GitHub Packages."
            }
            Write-Host "$($package.packageName) $version copied."
            $copied++
        }
    }
}
finally {
    Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Copied $copied package version$(($copied -eq 1) ? [string]::Empty : 's')."
