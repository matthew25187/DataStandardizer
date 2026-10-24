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

$changedPackages = @(Get-ChangedPackageName -PackageInfo (Get-PackageInfo) -Commit (Get-BuildCommit) -IsRelease (Test-ReleaseBuild))

Set-CiOutput -Name 'changedPackageNamesList' -Value ($changedPackages -join ',')

if ($changedPackages.Count -gt 0) {
    Write-Information "Detected $($changedPackages.Count) changed package$(($changedPackages.Count -eq 1) ? [string]::Empty : 's')."
}
else {
    Write-Information "No package changes were detected."
}

$changedPackages | ForEach-Object {
    Write-Verbose "-`t$($_)"
}
