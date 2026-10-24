#Requires -Version 7.4

<#
.SYNOPSIS
Works out every package's versions for a GitHub Actions run and, with -Reserve, reserves the next versions.

.DESCRIPTION
For each package, reads its stored versions and the assembly versions of its current package.  For the packages this run
publishes it then sets the production version and the next version to reserve (see Add-ProductionVersion).  With -Reserve,
it writes those next versions and reads them back.

Outputs:
- versions: Base64 JSON array of version records for all packages (the form UpdatePackageMetadata.ps1 and
  WriteVersionProps.ps1 take).
- reserved: Base64 JSON object, per reserved package, of PreviousNext and WrittenNext, for FinalizeVersionNumbers.ps1.

Every package is covered, not only those built: a released package's dependencies on unchanged packages are set from
their stored versions.
#>
param (
    # Packages this run publishes, as a JSON array or free-text list.
    [Parameter()]
    [AllowEmptyString()]
    [string]    $PublishSet = '',

    # Write the reserved next versions.  Without it nothing is written.
    [Parameter()]
    [switch]    $Reserve,

    # Read variables from this JSON file (toJSON(vars)) instead of the GitHub API.  Not allowed with -Reserve.
    [Parameter()]
    [string]    $VariablesFile,

    # Selects a separate copy of the version variables, such as SHADOW_.
    [Parameter()]
    [ValidatePattern('^([A-Z0-9_]*)$')]
    [string]    $VariablePrefix = '',

    # Feeds holding the current packages, as NuGet v3 service index URLs, tried in order.  The token is sent to all but
    # nuget.org.
    [Parameter()]
    [string[]]  $PackageSource = @("https://nuget.pkg.github.com/$(@($env:GITHUB_REPOSITORY_OWNER, 'matthew25187') | Where-Object { $_ } | Select-Object -First 1)/index.json", 'https://api.nuget.org/v3/index.json'),

    [Parameter()]
    [string]    $PackageSourceToken = $env:GITHUB_TOKEN,

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

if ($Reserve -and -not [string]::IsNullOrEmpty($VariablesFile)) {
    throw 'A reservation must read the variables fresh: -Reserve cannot be used with -VariablesFile.'
}

$packageInfo = Get-PackageInfo
$publishNames = ($PublishSet.TrimStart().StartsWith('[')) ? @($PublishSet | ConvertFrom-Json) : @(Resolve-PackageName -Name $PublishSet -PackageInfo $packageInfo)
$isRelease = Test-ReleaseBuild
$variables = [string]::IsNullOrEmpty($VariablesFile) ? (Read-VersionVariableSet) : (Read-VersionVariableSet -SnapshotPath $VariablesFile)
$sources = @($PackageSource | ForEach-Object {
    @{ ServiceIndexUrl = $_; Token = ($_ -like 'https://api.nuget.org/*') ? $null : $PackageSourceToken }
})
$downloadFolder = Join-Path (@($env:RUNNER_TEMP, [System.IO.Path]::GetTempPath()) | Where-Object { $_ } | Select-Object -First 1) "current-packages-$([guid]::NewGuid().ToString('N'))"

$versionsList = @()
$reserved = [ordered]@{}
foreach ($package in $packageInfo) {
    $state = Get-PackageVersionState -PackageInfo $package -Variables $variables -Prefix $VariablePrefix

    # The assemblies of the current package give the versions stamped on a build that produces no new version.
    $assemblyVersions = $null
    if ($state.CurrentVersion -eq [version]'0.0.0.0') {
        Write-Information "Package $($package.packageName) has no released version.  Current assembly version numbers are unknown."
    }
    else {
        $currentSemVer = ConvertTo-SemanticVersion -Version $state.CurrentVersion -PrereleaseLabel $state.CurrentPrereleaseLabel
        $packagePath = Save-NuGetPackage -PackageId $package.packageName -Version $currentSemVer -DestinationFolder $downloadFolder -Source $sources
        if ($null -eq $packagePath) {
            throw "Failed to acquire package $($package.packageName) $currentSemVer from any feed.  Unable to determine current package assembly version."
        }
        $assemblyVersions = Get-PackageAssemblyVersion -PackagePath $packagePath -PackageId $package.packageName -PackageVersion $state.CurrentVersion
    }

    $versions = New-PackageVersions -PackageName $package.packageName -State $state -AssemblyVersions $assemblyVersions
    if ($publishNames -contains $package.packageName) {
        $versions = $versions | Add-ProductionVersion -IsRelease $isRelease
        $reserved[$package.packageName] = [ordered]@{ PreviousNext = $versions.PackageNextVersion; WrittenNext = $versions.PackagePostProductionVersion }
        Write-Information "Package $($package.packageName) will be $($versions.PackageProductionVersionSemVer); next version $($versions.PackagePostProductionVersion)."
    }
    else {
        Write-Information "Package $($package.packageName) is at $($versions.PackageProductionVersionSemVer)."
    }

    $versionsList += $versions
}

if ($Reserve) {
    foreach ($packageName in $reserved.Keys) {
        $package = $packageInfo | Where-Object -Property packageName -EQ -Value $packageName
        $written = @(Set-PackageVersionState -PackageInfo $package -Group 'Next' -Version $reserved[$packageName].WrittenNext -Variables $variables -Prefix $VariablePrefix)
        if ($written.Count -eq 0) {
            throw "Package $packageName has no ${VariablePrefix}NEXT_* variables to reserve a version in."
        }
    }

    # Read back: a mismatch means the variables can't be trusted for this run.
    $reread = Read-VersionVariableSet
    foreach ($packageName in $reserved.Keys) {
        $package = $packageInfo | Where-Object -Property packageName -EQ -Value $packageName
        $stored = (Get-PackageVersionState -PackageInfo $package -Variables $reread -Prefix $VariablePrefix).NextVersion
        if ($stored -ne [version]$reserved[$packageName].WrittenNext) {
            throw "Failed to reserve $packageName next version $($reserved[$packageName].WrittenNext); the stored next version is $stored."
        }
        Write-Information "Reserved $packageName next version $stored."
    }
}

Remove-Item -LiteralPath $downloadFolder -Recurse -Force -ErrorAction SilentlyContinue

Set-CiOutput -Name 'versions' -Value (ConvertTo-EncodedVersionList -InputObject @($versionsList))
Set-CiOutput -Name 'reserved' -Value (ConvertTo-EncodedVersionList -InputObject $reserved)
