#Requires -Version 7.4

<#
.SYNOPSIS
Runs a package's built test project of one kind, once for each target framework it builds for.

.DESCRIPTION
The unit tests are tests/<Package>.Tests, and every package with tests enabled has them.  The integration tests are
tests/<Package>.IntegrationTests, and are optional: a package without that project has nothing to run, and passes.  A test
project that exists but has no built assemblies fails, as the build didn't build it (is it in <Package>.slnf?).

Each target framework's assembly, bin/<Configuration>/<framework>/<Project>.dll, runs with dotnet test and writes a TRX
file, <Project>.<framework>.trx, to the results folder.  .NET Framework assemblies (net4*) run on Windows only.
#>
param (
    [Parameter(Mandatory)]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [string]    $Configuration,

    [Parameter(Mandatory)]
    [ValidateSet('Unit', 'Integration')]
    [string]    $Kind,

    [Parameter(Mandatory)]
    [string]    $ResultsFolder,

    # The folder that holds tests/; the repository root by default.
    [Parameter()]
    [string]    $RepositoryRoot = '',

    [Parameter()]
    [int]   $TraceLevel = 0
)

$ErrorActionPreference = 'Stop'
# A target framework whose tests fail is counted rather than fatal, so that the others still run.
$PSNativeCommandUseErrorActionPreference = $false

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/BuildPlan/BuildPlan.psm1') -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

if ([string]::IsNullOrEmpty($RepositoryRoot)) {
    $RepositoryRoot = Get-RepositoryRoot
}

$projectName = Get-TestProjectName -PackageName $PackageName -Kind $Kind
$projectFolderPath = Join-Path $RepositoryRoot 'tests' $projectName
$kindDescription = ($Kind -eq 'Unit') ? 'unit tests' : 'integration tests'

if (-not (Test-Path -LiteralPath (Join-Path $projectFolderPath "$projectName.csproj") -PathType Leaf)) {
    if ($Kind -eq 'Integration') {
        Write-Information "$PackageName has no integration tests (tests/$projectName); nothing to run."
        exit 0
    }

    Write-Error "$PackageName has no unit test project, tests/$projectName/$projectName.csproj."
}

$assemblies = @(Get-TestAssemblyPath -ProjectFolderPath $projectFolderPath -Configuration $Configuration)
if ($assemblies.Count -eq 0) {
    Write-Error "No $Configuration assemblies found for tests/$projectName.  Is the project in $PackageName.slnf?"
}

$failed = 0
foreach ($assembly in $assemblies) {
    $framework = Split-Path -Path (Split-Path -Path $assembly -Parent) -Leaf
    Write-Host "::group::$PackageName $kindDescription on $framework"
    & dotnet test $assembly --logger "trx;LogFileName=$projectName.$framework.trx" --results-directory $ResultsFolder
    if ($LASTEXITCODE) {
        $failed++
    }
    Write-Host '::endgroup::'
}

if ($failed) {
    Write-Error "$PackageName $kindDescription failed on $failed target framework$(($failed -eq 1) ? '' : 's')."
}

Write-Information "$PackageName $kindDescription passed on $($assemblies.Count) target framework$(($assemblies.Count -eq 1) ? '' : 's')."
