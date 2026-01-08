#Requires -Version 3.0

param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $VersionNumberName,

    [Parameter()]
    [string]    $VersionPrereleaseLabelName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $VariableNamePrefix,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]    $OutputInformationPreference = 'SilentlyContinue',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]    $OutputVerbosePreference = 'SilentlyContinue',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]    $OutputDebugPreference = 'SilentlyContinue',

    [Parameter()]
    [int]    $TraceLevel = 0
)

$InformationPreference = $OutputInformationPreference
$VerbosePreference = $OutputVerbosePreference
$DebugPreference = $OutputDebugPreference

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force -Scope Local

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

# Get version numbers for updating.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$pipelinePackageVersions = $variableListOutput | ConvertFrom-Json

[version]$updateVersionNumber = $packageVersions.PSObject.Properties[$VersionNumberName].Value

# Update version Major component.
$variableName = "$VariableNamePrefix-major-number"
if ($null -ne $pipelinePackageVersions.PSObject.Properties[$variableName]) {
    $updateMajorNumber = $updateVersionNumber.Major
    az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name $variableName --value $updateMajorNumber --verbose

    Write-Verbose "Set pipeline $variableName variable to $updateMajorNumber."
}

# Update version Minor component.
$variableName = "$VariableNamePrefix-minor-number"
if ($null -ne $pipelinePackageVersions.PSObject.Properties[$variableName]) {
    $updateMinorNumber = $updateVersionNumber.Minor
    az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name $variableName --value $updateMinorNumber --verbose
    
    Write-Verbose "Set pipeline $variableName variable to $updateMinorNumber."
}

# Update version Patch component.
$variableName = "$VariableNamePrefix-patch-number"
if ($null -ne $pipelinePackageVersions.PSObject.Properties[$variableName]) {
    $updateBuildNumber = [System.Math]::Max($updateVersionNumber.Build, 0)
    az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name $variableName --value $updateBuildNumber --verbose

    Write-Verbose "Set pipeline $variableName variable to $updateBuildNumber."
}

# Update version Prerelease component.
$variableName = "$VariableNamePrefix-prerelease-number"
if ($null -ne $pipelinePackageVersions.PSObject.Properties[$variableName]) {
    $updateRevisionNumber = [System.Math]::Max($updateVersionNumber.Revision, 0)
    az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name $variableName --value $updateRevisionNumber --verbose

    Write-Verbose "Set pipeline $variableName variable to $updateRevisionNumber."
}

# Update prerelease label.
$variableName = "$VariableNamePrefix-prerelease-label"
if ((-not [string]::IsNullOrEmpty($VersionPrereleaseLabelName)) -and ($null -ne $pipelinePackageVersions.PSObject.Properties[$variableName])) {
    $prereleaseLabel = $packageVersions.PSObject.Properties[$VersionPrereleaseLabelName].Value
    az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name $variableName --value $prereleaseLabel --verbose

    Write-Verbose "Set pipeline $variableName variable to $prereleaseLabel."
}

Write-Information "Updated $PackageName $VariableNamePrefix version number to $updateVersionNumber."