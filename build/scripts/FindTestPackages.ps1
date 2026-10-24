#Requires -Version 7.4

param (
    [Parameter()]
    [int]   $TraceLevel = 0
)

Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Scope Local
Import-Module (Join-Path $PSScriptRoot '../psmodules/BuildPlan/BuildPlan.psm1') -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$testPackageNames = @(Get-TestPackageName -PackageInfo (Get-PackageInfo))

Set-CiOutput -Name 'testPackageNames' -Value ($testPackageNames -join ',') -IsOutput

if ($testPackageNames.Count -gt 0) {
    Write-Information "Detected $($testPackageNames.Count) test package$(($testPackageNames.Count -eq 1) ? [string]::Empty : 's')."
}
else {
    Write-Information 'No test packages were detected.'
}

$testPackageNames | ForEach-Object {
    Write-Verbose "-`t$($_)"
}

# A package whose tests, tests/<Package>.Tests, this build changes has its tests built and run even when the package itself
# is unchanged.  See Get-ChangedTestPackageName.
$changedTestPackageNames = @(Get-ChangedTestPackageName -TestPackageName $testPackageNames -Commit (Get-BuildCommit))

Set-CiOutput -Name 'changedTestPackageNames' -Value ($changedTestPackageNames -join ',') -IsOutput

if ($changedTestPackageNames.Count -gt 0) {
    Write-Information "Detected changed tests for $($changedTestPackageNames.Count) package$(($changedTestPackageNames.Count -eq 1) ? [string]::Empty : 's')."
}
else {
    Write-Information 'No test changes were detected.'
}

$changedTestPackageNames | ForEach-Object {
    Write-Verbose "-`t$($_)"
}
