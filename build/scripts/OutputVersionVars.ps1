#Requires -Version 3.0

param (
    [Parameter()]
    [int]    $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Make the package versions list available to the pipeline.
Write-Host "Promoting versionNumberList to output variable"
Write-Host "##vso[task.setvariable variable=versionNumberList;isOutput=true]$env:VERSIONNUMBERLIST"            

#   Create a separate list to flag if assembly versioning should be applied.
$packageVersioningList = $env:VERSIONNUMBERLIST | 
ConvertFrom-Base64String | 
ConvertFrom-Json | 
ForEach-Object { @{
        PackageName                     = $_.PackageName
        HasAssemblyVersion              = (-not [string]::IsNullOrEmpty($_.AssemblyProductionVersion))
        HasAssemblyFileVersion          = (-not [string]::IsNullOrEmpty($_.AssemblyProductionFileVersion))
        HasAssemblyInformationalVersion = (-not [string]::IsNullOrEmpty($_.AssemblyProductionInformationalVersion))
    } }

$packagesHavingAssemblyVersionList = @($packageVersioningList |
    Where-Object -Property HasAssemblyVersion -NE -Value $false |
    Select-Object -ExpandProperty PackageName) -join ','
Write-Information "Found $(($packagesHavingAssemblyVersionList -split ',').Count) package assemblies having an assembly version."
($packagesHavingAssemblyVersionList -split ',') | ForEach-Object { Write-Verbose "-`t$($_)" }
Write-Host "##vso[task.setvariable variable=hasAssemblyVersionPackageNames;isOutput=true]$packagesHavingAssemblyVersionList"

$packagesHavingAssemblyFileVersionList = @($packageVersioningList |
    Where-Object -Property HasAssemblyFileVersion -NE -Value $false |
    Select-Object -ExpandProperty PackageName) -join ','
Write-Information "Found $(($packagesHavingAssemblyFileVersionList -split ',').Count) package assemblies having an assembly file version."
($packagesHavingAssemblyFileVersionList -split ',') | ForEach-Object { Write-Verbose "-`t$($_)" }
Write-Host "##vso[task.setvariable variable=hasAssemblyFileVersionPackageNames;isOutput=true]$packagesHavingAssemblyFileVersionList"

$packagesHavingAssemblyInformationalVersionList = @($packageVersioningList |
    Where-Object -Property HasAssemblyInformationalVersion -NE -Value $false |
    Select-Object -ExpandProperty PackageName) -join ','
Write-Information "Found $(($packagesHavingAssemblyInformationalVersionList -split ',').Count) package assemblies having an assembly informational version."
($packagesHavingAssemblyInformationalVersionList -split ',') | ForEach-Object { Write-Verbose "-`t$($_)" }
Write-Host "##vso[task.setvariable variable=hasAssemblyInformationalVersionPackageNames;isOutput=true]$packagesHavingAssemblyInformationalVersionList"