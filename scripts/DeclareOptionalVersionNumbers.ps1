[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $SourceBranch,

    [Parameter()]
    [int]   $TraceLevel = 0
)

function ConvertFrom-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )
    
    process {
        $inputBytes = [System.Convert]::FromBase64String($InputObject)
        return [System.Text.Encoding]::UTF8.GetString($inputBytes)
    }
}

function ConvertTo-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )
    
    process {
        $inputBytes = [System.Text.Encoding]::UTF8.GetBytes($InputObject)
        return [System.Convert]::ToBase64String($inputBytes)
    }
}

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

# Calculate production package version number.
$productionMajorNumber = $packageVersions.PackageNextVersion.Major
$productionMinorNumber = $packageVersions.PackageNextVersion.Minor
$productionBuildNumber = $packageVersions.PackageNextVersion.Build
$productionRevisionNumber = [System.Math]::Max($packageVersions.PackageNextVersion.Revision, 1)
if ($SourceBranch -eq 'refs/heads/master') {
    $productionRevisionNumber = 0
}
[version]$productionPackageVersion = ($productionMajorNumber, $productionMinorNumber, $productionBuildNumber, $productionRevisionNumber -join '.')
$packageVersions.PackageProductionVersion = $productionPackageVersion.ToString()
Write-Information "Production package version will be $productionPackageVersion."

# Calculate post-production package version number.
$postProductionNumbers = $productionPackageVersion.Major, $productionPackageVersion.Minor, $productionPackageVersion.Build, $productionPackageVersion.Revision
$incrementFromIndex = $postProductionNumbers.Count - 1 # default to incrementing the preview number
if ($SourceBranch -eq 'refs/heads/master') {
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
Write-Information "Post-production package version will be $postProductionPackageVersion."

# Calculate production assembly version numbers.
[version]$productionAssemblyFileVersion = $productionPackageVersion
$packageVersions | Add-Member -MemberType NoteProperty -Name 'AssemblyProductionFileVersion' -Value $productionAssemblyFileVersion.ToString()
Write-Information "Production assembly file version will be $productionAssemblyFileVersion."

$productionAssemblyVersionRevision = [System.Convert]::ToInt32([datetime]::UtcNow.TimeOfDay.TotalSeconds / 2)
[version]$productionAssemblyVersion = "$($productionPackageVersion.Major).$($productionPackageVersion.Minor).$($productionPackageVersion.Build).$productionAssemblyVersionRevision"
$packageVersions | Add-Member -MemberType NoteProperty -Name 'AssemblyProductionVersion' -Value $productionAssemblyVersion.ToString()
Write-Information "Production assembly version will be $productionAssemblyVersion."

[version]$productionAssemblyInformationalVersion = "$($productionPackageVersion.Major).$($productionPackageVersion.Minor)"
$packageVersions | Add-Member -MemberType NoteProperty -Name 'AssemblyProductionInformationalVersion' -Value $productionAssemblyInformationalVersion.ToString()
Write-Information "Production assembly informational version will be $productionAssemblyInformationalVersion."

# Serialize updated package versions.
$serializedPackageVersionsList = $packageVersionsList | ConvertTo-Json | ConvertTo-Base64String
Write-Host "##vso[task.setvariable variable=versionNumberList;]$serializedPackageVersionsList"