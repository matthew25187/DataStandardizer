param (
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $PackageName,

    # Package information list.
    [Parameter()]
    [string]    $PackageInfos,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $PackageCurrentVersionNumbersList,

    [Parameter()]
    [ValidateNotNull()]
    [string] $PackageNewVersionNumbersList,

    # Debug tracing.
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

function Get-VersionNumbers {
    param (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $PackageName,

        [Parameter()]
        [string] $List
    )
    
    $packageInfo = $List | ConvertFrom-Json | Where-Object -Property PackageName -EQ $PackageName
    if ($null -eq $packageInfo) {
        return
    }
    
    [version] $packageVersion = $packageInfo.PackageVersion.Major, $packageInfo.PackageVersion.Minor, $packageInfo.PackageVersion.Build, $packageInfo.PackageVersion.Revision -join '.'
    return $packageVersion
}

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Set Package Source Path argument.
$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ $PackageName
if ($null -eq $packageInfo) {
    Write-Error "No package information found for $PackageName."
    exit
}

Write-Host "##vso[task.setvariable variable=sourceFolderPath]$($packageInfo.packageSourcePath)"
Write-Information "Package Source Path argument is $($packageInfo.packageSourcePath)"

# Assume package has changes; use package's new version number.
Write-Information "Searching new version numbers for $PackageName version..."
$versionNumber = Get-VersionNumbers -PackageName $PackageName -List ($PackageNewVersionNumbersList | ConvertFrom-Base64String)
if ($null -eq $versionNumber) {
    # Package is not a changed package; use package's current version number instead.
    Write-Information "Searching current version numbers for $PackageName version..."
    $versionNumber = Get-VersionNumbers -PackageName $PackageName -List ($PackageCurrentVersionNumbersList | ConvertFrom-Base64String)
}

if ($null -ne $versionNumber) {
    Write-Information "Found version $versionNumber for package $PackageName."
}
else {
    Write-Error "No version found for package $PackageName."
}

# Compose file version.
$fileVersion = "$($versionNumber.Major).$($versionNumber.Minor).$($versionNumber.Build).$($versionNumber.Revision)"
Write-Host "##vso[task.setvariable variable=buildFileVersion]$fileVersion"
Write-Information "File Version will be $fileVersion."

# Compose assembly version.
$assemblyVersionRevision = [System.Convert]::ToInt32([datetime]::Now.TimeOfDay.TotalSeconds / 2)
$assemblyVersion = "$($versionNumber.Major).$($versionNumber.Minor).$($versionNumber.Build).$assemblyVersionRevision"
Write-Host "##vso[task.setvariable variable=buildAssemblyVersion]$assemblyVersion"
Write-Information "Assembly Version will be $assemblyVersion."

# Compose informational version.
$informationalVersion = "$($versionNumber.Major).$($versionNumber.Minor)"
Write-Host "##vso[task.setvariable variable=buildInformationalVersion]$informationalVersion"
Write-Information "Informational Version will be $informationalVersion."