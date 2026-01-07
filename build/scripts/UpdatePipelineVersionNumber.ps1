param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $VersionNumberName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $VariableNamePrefix,

    [Parameter()]
    [int]    $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                      
az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
az account set -s ${env:SUBSCRIPTION_ID}

# Extract package metadata.
$packageInfo = ($env:PACKAGEINFOS | ConvertFrom-Json) | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName."
}

$packageVersions = ($env:VERSIONNUMBERS | ConvertFrom-Base64String | ConvertFrom-Json) | Where-Object -Property PackageName -EQ -Value $PackageName
if ($null -eq $packageVersions) {
    Write-Error "Package versions not found for $PackageName."
}

# Update version numbers in pipeline.
[version]$updateVersionNumber = $packageVersions.PSObject.Properties[$VersionNumberName].Value
$updateMajorNumber = $updateVersionNumber.Major
$updateMinorNumber = $updateVersionNumber.Minor
$updateBuildNumber = [System.Math]::Max($updateVersionNumber.Build, 0)
$updateRevisionNumber = [System.Math]::Max($updateVersionNumber.Revision, 0)
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name "$VariableNamePrefix-major-number" --value $updateMajorNumber --verbose
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name "$VariableNamePrefix-minor-number" --value $updateMinorNumber --verbose
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name "$VariableNamePrefix-patch-number" --value $updateBuildNumber --verbose
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name "$VariableNamePrefix-preview-number" --value $updateRevisionNumber --verbose

Write-Information "Updated $PackageName $VariableNamePrefix version number to $updateVersionNumber." -InformationAction Continue