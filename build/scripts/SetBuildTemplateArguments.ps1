param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageInfos,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$EncodedPackageVersions,

    [Parameter()]
    [int]   $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Set package Source Folder Path argument.
$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ $PackageName
if ($null -eq $packageInfo) {
    Write-Error "No package information found for $PackageName."
    exit
}

Write-Host "##vso[task.setvariable variable=sourceFolderPath]$($packageInfo.packageSourcePath)"
Write-Information "Package Source Path argument is $($packageInfo.packageSourcePath)"

# Get package versions.
$packageVersions = $EncodedPackageVersions | ConvertFrom-Base64String | ConvertFrom-Json | Where-Object -Property PackageName -EQ -Value $PackageName

# Set assembly Build File Version argument.
if (-not [string]::IsNullOrEmpty($packageVersions.AssemblyProductionFileVersion)) {
    [version]$fileVersion = $packageVersions.AssemblyProductionFileVersion
    Write-Host "##vso[task.setvariable variable=buildFileVersion]$fileVersion"
    Write-Information "File Version will be $fileVersion."
}
else {
    Write-Warning "No file version found for $PackageName."
}

# Set assembly Build Assembly Version argument.
if (-not [string]::IsNullOrEmpty($packageVersions.AssemblyProductionVersion)) {
    [version]$assemblyVersion = $packageVersions.AssemblyProductionVersion
    Write-Host "##vso[task.setvariable variable=buildAssemblyVersion]$assemblyVersion"
    Write-Information "Assembly Version will be $assemblyVersion."
}
else {
    Write-Warning "No assembly version found for $PackageName."
}

# Set assembly Build Informational Version argument.
if (-not [string]::IsNullOrEmpty($packageVersions.AssemblyProductionInformationalVersion)) {
    [version]$informationalVersion = $packageVersions.AssemblyProductionInformationalVersion
    Write-Host "##vso[task.setvariable variable=buildInformationalVersion]$informationalVersion"
    Write-Information "Informational Version will be $informationalVersion."
}
else {
    Write-Warning "No informational version found for $PackageName."
}