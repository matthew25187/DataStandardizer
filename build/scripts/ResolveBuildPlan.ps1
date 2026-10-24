#Requires -Version 7.4

<#
.SYNOPSIS
Decides which packages a GitHub Actions run builds, tests and publishes, and writes the lists as step outputs.

.DESCRIPTION
Outputs, each a JSON array of package IDs: changed, tests-changed, dependent, build-set, test-set, publish-set; and
has-build / has-publish ('true' or 'false').  See Get-BuildPlan for how the sets combine.  Also build-matrix and test-matrix,
the build and test legs as matrix 'include' lists of package, configuration and os (see Get-BuildMatrix and
Get-TestMatrix).

The package-list parameters take free text (commas or spaces; full IDs or short names such as File.CSV; '*' for all).
#>
param (
    # The run publishes packages (a push to develop or master, or a manual run that forces publication).
    [Parameter()]
    [switch]    $Publish,

    # Packages to publish whether or not they changed.
    [Parameter()]
    [AllowEmptyString()]
    [string]    $ForcePublication = '',

    # Replaces change detection with this list of changed packages.
    [Parameter()]
    [AllowEmptyString()]
    [string]    $OverrideChangedPackages = '',

    [Parameter()]
    [int]   $TraceLevel = 0
)

$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/BuildPlan/BuildPlan.psm1') -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$manifest = Get-PackageManifest
$packageInfo = Get-PackageInfo
$commit = Get-BuildCommit
$isRelease = Test-ReleaseBuild

$forcePublicationNames = @(Resolve-PackageName -Name $ForcePublication -PackageInfo $packageInfo)
$override = @(Resolve-PackageName -Name $OverrideChangedPackages -PackageInfo $packageInfo)

if ($override.Count -gt 0) {
    Write-Information "Using the changed-package override: $($override -join ', ')."
    $changed = $override
}
else {
    $changed = @(Get-ChangedPackageName -PackageInfo $packageInfo -Commit $commit -IsRelease $isRelease -SharedBuildFile @($manifest.sharedBuildFiles))
}

$changedTests = @(Get-ChangedTestPackageName -TestPackageName (Get-TestPackageName -PackageInfo $packageInfo) -Commit $commit)
$dependent = @(Get-DependentPackageName -PackageInfo $packageInfo -ChangedPackageName $changed -RepositoryRoot (Get-RepositoryRoot))

$plan = Get-BuildPlan -PackageInfo $packageInfo -ChangedPackageName $changed -ChangedTestPackageName $changedTests `
    -DependentPackageName $dependent -ForcePublicationPackageName $forcePublicationNames -Publish $Publish.IsPresent

$toJson = { param([string[]]$Names) ConvertTo-Json -InputObject ([string[]]@($Names | Where-Object { $_ })) -Compress }
Set-CiOutput -Name 'changed' -Value (& $toJson $plan.Changed)
Set-CiOutput -Name 'tests-changed' -Value (& $toJson $plan.TestsChanged)
Set-CiOutput -Name 'dependent' -Value (& $toJson $plan.Dependent)
Set-CiOutput -Name 'build-set' -Value (& $toJson $plan.Build)
Set-CiOutput -Name 'test-set' -Value (& $toJson $plan.Test)
Set-CiOutput -Name 'publish-set' -Value (& $toJson $plan.Publish)
$defaultBuildRunner = $manifest.PSObject.Properties['defaultBuildRunner'] ? $manifest.defaultBuildRunner : 'ubuntu-24.04'
$buildMatrix = @(Get-BuildMatrix -PackageInfo $packageInfo -BuildPackageName $plan.Build -DefaultRunner $defaultBuildRunner)
Set-CiOutput -Name 'build-matrix' -Value (ConvertTo-Json -InputObject $buildMatrix -Compress -Depth 3)
$defaultRunners = @($manifest.PSObject.Properties['defaultTestRunners'] ? $manifest.defaultTestRunners : @('ubuntu-24.04', 'windows-2025'))
$testMatrix = @(Get-TestMatrix -PackageInfo $packageInfo -TestPackageName $plan.Test -DefaultRunner $defaultRunners)
Set-CiOutput -Name 'test-matrix' -Value (ConvertTo-Json -InputObject $testMatrix -Compress -Depth 3)
Set-CiOutput -Name 'has-build' -Value ($plan.Build.Count -gt 0 ? 'true' : 'false')
Set-CiOutput -Name 'has-publish' -Value ($plan.Publish.Count -gt 0 ? 'true' : 'false')

Write-Information "Build plan for $commit ($($isRelease ? 'release' : 'preview') build, $($Publish ? 'publishing' : 'not publishing')):"
foreach ($property in 'Changed', 'TestsChanged', 'Dependent', 'Publish', 'Build', 'Test') {
    $names = @($plan.$property)
    Write-Information ("  {0,-13} {1}" -f "${property}:", (($names.Count -gt 0) ? ($names -join ', ') : '(none)'))
}
