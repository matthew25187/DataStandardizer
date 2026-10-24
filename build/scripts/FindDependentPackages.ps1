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

$changedPackageNames = @(($env:CHANGEDPACKAGENAMESLIST ?? '') -split ',' | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrEmpty($_) })
$dependentPackages = @(Get-DependentPackageName -PackageInfo (Get-PackageInfo) -ChangedPackageName $changedPackageNames -RepositoryRoot (Get-RepositoryRoot))

Set-CiOutput -Name 'dependentPackageNames' -Value ($dependentPackages -join ',') -IsOutput

if ($dependentPackages.Count -gt 0) {
    Write-Information "Detected $($dependentPackages.Count) dependent package$(($dependentPackages.Count -eq 1) ? [string]::Empty : 's')."
}
else {
    Write-Information "No dependent packages were detected."
}

$dependentPackages | ForEach-Object {
    Write-Verbose "-`t$($_)"
}
