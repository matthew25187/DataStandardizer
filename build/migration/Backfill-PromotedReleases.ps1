#Requires -Version 7.4

<#
.SYNOPSIS
Creates a GitHub Release for each package's promoted version in the Azure Artifacts feed's Release view, so the README
badges show the same versions as before the move to GitHub.

.DESCRIPTION
A one-off step of the move to GitHub Actions (#365).  On GitHub a published GitHub Release on a package version's tag marks
the version as promoted (see .github/workflows/publish-package.yml).  This script reads which versions are in the feed's
Release view and, for each package, creates the release for its latest promoted version (or for every promoted version with
-All).  The release gets the version's .nupkg, downloaded from the feed, and its release notes.

Needs an Azure DevOps token with Packaging (read), and gh signed in with permission to create releases.  The checkout
must have the package tags (git fetch --tags).  Versions without a tag, or that already have a release, are skipped.  Use
-WhatIf to list what would be created.

.EXAMPLE
./build/migration/Backfill-PromotedReleases.ps1 -Organization <org> -Project <project> -Feed <feed> -AdoToken $adoPat -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter(Mandatory)]
    [string]    $Organization,

    [Parameter(Mandatory)]
    [string]    $Project,

    [Parameter(Mandatory)]
    [string]    $Feed,

    [Parameter(Mandatory)]
    [string]    $AdoToken,

    # The feed view that holds the promoted versions.
    [Parameter()]
    [string]    $View = 'Release',

    # Create a release for every promoted version, not only the latest.
    [Parameter()]
    [switch]    $All
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/PackageRegistry/PackageRegistry.psm1') -Scope Local

$headers = @{ Authorization = 'Basic ' + [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes(":$AdoToken")) }
$feedIndexUrl = "https://pkgs.dev.azure.com/$Organization/$Project/_packaging/$Feed/nuget/v3/index.json"
$packagesUrl = "https://feeds.dev.azure.com/$Organization/$Project/_apis/packaging/Feeds/$Feed/packages?protocolType=NuGet&includeAllVersions=true&api-version=7.1"
$feedPackages = (Invoke-RestMethod -Uri $packagesUrl -Headers $headers).value

$folder = Join-Path ([System.IO.Path]::GetTempPath()) "promoted-$([guid]::NewGuid().ToString('N'))"
$created = 0
try {
    foreach ($package in Get-PackageInfo) {
        $feedPackage = $feedPackages | Where-Object { $_.name -eq $package.packageName } | Select-Object -First 1
        $promoted = @($feedPackage.versions |
            Where-Object { $_.views.name -contains $View } |
            ForEach-Object { [pscustomobject]@{ SemVer = $_.version; Parsed = ConvertFrom-SemanticVersion -SemanticVersion $_.version } } |
            Where-Object { $null -ne $_.Parsed } |
            Sort-Object { $_.Parsed.Version })
        if ($promoted.Count -eq 0) {
            Write-Host "$($package.packageName): no version in the $View view."
            continue
        }
        if (-not $All) {
            $promoted = @($promoted[-1])
        }

        foreach ($version in $promoted) {
            $tag = ConvertTo-PackageTagName -Version $version.Parsed.Version -PackageName $package.packageName
            & git rev-parse --verify --quiet "refs/tags/$tag" | Out-Null
            if ($LASTEXITCODE -ne 0) {
                Write-Warning "$($package.packageName) $($version.SemVer): tag $tag not found; skipped."
                continue
            }
            & gh release view $tag --json tagName 2>$null | Out-Null
            if ($LASTEXITCODE -eq 0) {
                Write-Host "$($package.packageName) $($version.SemVer): release $tag already exists; skipped."
                continue
            }

            if (-not $PSCmdlet.ShouldProcess("$($package.packageName) $($version.SemVer)", "Create GitHub Release on $tag")) {
                continue
            }

            $packagePath = Save-NuGetPackage -PackageId $package.packageName -Version $version.SemVer -DestinationFolder $folder `
                -Source @{ ServiceIndexUrl = $feedIndexUrl; Token = $AdoToken }
            if ($null -eq $packagePath) {
                throw "$($package.packageName) $($version.SemVer) not found in the feed."
            }
            $assetPath = Join-Path $folder "$($package.packageName).$($version.SemVer).nupkg"
            if ($packagePath -cne $assetPath) {
                Move-Item -LiteralPath $packagePath -Destination $assetPath -Force
            }

            $extracted = Join-Path $folder "extract-$([guid]::NewGuid().ToString('N'))"
            Expand-Archive -LiteralPath $assetPath -DestinationPath $extracted
            [xml]$nuspec = Get-Content -LiteralPath (Get-ChildItem -Path $extracted -Filter '*.nuspec' -File | Select-Object -First 1).FullName -Raw
            $notesPath = Join-Path $folder "$tag.md"
            $notes = $nuspec.package.metadata.releaseNotes
            Set-Content -LiteralPath $notesPath -Value ($notes ? $notes : "$($package.packageName) $($version.SemVer)")

            $arguments = @('release', 'create', $tag, $assetPath, '--verify-tag', '--title', "$($package.packageName) $($version.SemVer)", '--notes-file', $notesPath)
            if ($version.Parsed.IsPrerelease) {
                $arguments += '--prerelease'
            }
            [void](Invoke-Gh @arguments)
            Write-Host "$($package.packageName) $($version.SemVer): release $tag created."
            $created++
        }
    }
}
finally {
    # A scratch folder, so it is removed even under -WhatIf.
    Remove-Item -LiteralPath $folder -Recurse -Force -ErrorAction SilentlyContinue -WhatIf:$false
}

Write-Host "Created $created release$(($created -eq 1) ? [string]::Empty : 's')."
