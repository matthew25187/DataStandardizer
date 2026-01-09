#Requires -Version 3.0

param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,
    
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $EncodedPackageVersions,

    [Parameter()]
    [int]    $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                                                      
az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
az account set -s ${env:SUBSCRIPTION_ID}

# Get package information.
$packageInfo = $env:PACKAGEINFOS | ConvertFrom-Json | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName."
}

$packageVersions = $EncodedPackageVersions | ConvertFrom-Base64String | ConvertFrom-Json | Where-Object -Property PackageName -EQ -Value '${{packageName}}'
if ($null -eq $packageVersions) {
    Write-Error "Package versions not found for $PackageName."
}
[version]$nextPackageVersion = $packageVersions.PackageNextVersion

# Fetch next package version from pipeline.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties |
Where-Object { $_.Name.StartsWith('next') -and $_.Name.EndsWith('number') } |
Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream |
Sort-Object |
ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' |
Out-String -InputObject { $_.Value } -Stream
[version] $currentPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')

# Verify rollback was successful.
if ($currentPackageVersion -ne $nextPackageVersion) {
    Write-Error "Failed to rollback package version for $PackageName to $nextPackageVersion; actual version is $currentPackageVersion."
}
                                
[version]$oldPackageVersion = '$(currentNextMajor).$(currentNextMinor).$(currentNextPatch).$(currentNextPreview)'
Write-Information "Rolled back package $($packageInfo.packageName) next version from v$oldPackageVersion to v$currentPackageVersion." -InformationAction Continue