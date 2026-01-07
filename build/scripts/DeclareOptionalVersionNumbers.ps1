param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter()]
    [int]   $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

[PSCustomObject[]]$packageVersionsList = @()

$raw = $env:VERSIONNUMBERLIST

if (-not [string]::IsNullOrEmpty($raw)) {
    $packageVersionsList = $raw | ConvertFrom-Base64String | ConvertFrom-Json
    Write-Debug "Extracted $($packageVersionsList.Count) existing package versions."
}

$packageVersions = $packageVersionsList | Where-Object -Property PackageName -EQ -Value $PackageName
if ($null -eq $packageVersions) {
    Write-Error "Versions for package $PackageName not found."
}

# Calculate production package version number.
[version]$nextPackageVersion = $packageVersions.PackageNextVersion
$productionMajorNumber = $nextPackageVersion.Major
$productionMinorNumber = $nextPackageVersion.Minor
$productionBuildNumber = $nextPackageVersion.Build
$productionRevisionNumber = [System.Math]::Max($nextPackageVersion.Revision, 1)
if ($env:BUILD_SOURCEBRANCH -eq 'refs/heads/master') {
    $productionRevisionNumber = 0
}
[version]$productionPackageVersion = ($productionMajorNumber, $productionMinorNumber, $productionBuildNumber, $productionRevisionNumber -join '.')
$packageVersions.PackageProductionVersion = $productionPackageVersion.ToString()

Write-Information "Production package version number will be $productionPackageVersion."

$productionPackageVersionString = $productionPackageVersion.Major, $productionPackageVersion.Minor, $productionPackageVersion.Build -join '.'
if ($productionPackageVersion.Revision -gt 0) {
    $productionPackageVersionString += "-preview.$($productionPackageVersion.Revision)"
}
$packageVersions.PackageProductionVersionString = $productionPackageVersionString

Write-Information "Production package version will be $productionPackageVersionString."

# Calculate post-production package version number.
$postProductionNumbers = $productionPackageVersion.Major, $productionPackageVersion.Minor, $productionPackageVersion.Build, $productionPackageVersion.Revision
$incrementFromIndex = $postProductionNumbers.Count - 1 # default to incrementing the preview number
if ($env:BUILD_SOURCEBRANCH -eq 'refs/heads/master') {
    $incrementFromIndex = 1 # increment minor number
}
for ($versionPartIndex = $incrementFromIndex; $versionPartIndex -lt $postProductionNumbers.Count; $versionPartIndex++) {
    if ($versionPartIndex -eq $incrementFromIndex) {
        $postProductionNumbers[$versionPartIndex]++
    }
    elseif ($versionPartIndex -gt $incrementFromIndex) {
        $postProductionNumbers[$versionPartIndex] = 0
    }
}

[version]$postProductionPackageVersion = ($postProductionNumbers[0], $postProductionNumbers[1], $postProductionNumbers[2], $postProductionNumbers[3] -join '.')
$packageVersions | Add-Member -MemberType NoteProperty -Name 'PackagePostProductionVersion' -Value $postProductionPackageVersion.ToString()
Write-Information "Post-production package version number will be $postProductionPackageVersion."

# Calculate production assembly version numbers.
[version]$productionAssemblyFileVersion = $productionPackageVersion
$packageVersions.AssemblyProductionFileVersion = $productionAssemblyFileVersion.ToString()
Write-Information "Production assembly file version number will be $productionAssemblyFileVersion."

$productionAssemblyVersionRevision = [System.Convert]::ToInt32([datetime]::UtcNow.TimeOfDay.TotalSeconds / 2)
[version]$productionAssemblyVersion = "$($productionPackageVersion.Major).$($productionPackageVersion.Minor).$($productionPackageVersion.Build).$productionAssemblyVersionRevision"
$packageVersions.AssemblyProductionVersion = $productionAssemblyVersion.ToString()
Write-Information "Production assembly version number will be $productionAssemblyVersion."

[version]$productionAssemblyInformationalVersion = "$($productionPackageVersion.Major).$($productionPackageVersion.Minor)"
$packageVersions.AssemblyProductionInformationalVersion = $productionAssemblyInformationalVersion.ToString()
Write-Information "Production assembly informational version number will be $productionAssemblyInformationalVersion."

# Serialize updated package versions.
$serializedPackageVersionsList = $packageVersionsList | ConvertTo-Json | ConvertTo-Base64String
Write-Host "##vso[task.setvariable variable=versionNumberList;]$serializedPackageVersionsList"