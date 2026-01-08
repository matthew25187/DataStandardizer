#Requires -Version 7.1

param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter()]
    [string]    $OutputInformationPreference = 'SilentlyContinue',

    [Parameter()]
    [string]    $OutputVerbosePreference = 'SilentlyContinue',

    [Parameter()]
    [string]    $OutputDebugPreference = 'SilentlyContinue',

    [Parameter()]
    [int]   $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$InformationPreference = $OutputInformationPreference
$VerbosePreference = $OutputVerbosePreference
$DebugPreference = $OutputDebugPreference

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                      
az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
az account set -s ${env:SUBSCRIPTION_ID}

# Extract package metadata.
$packageInfo = $env:PACKAGEINFOS | ConvertFrom-Json | Where-Object -Property packageName -EQ "$PackageName"
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName."
}

Write-Information "Found information for package $PackageName."

# Download variables from pipeline.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$packageVersions = $variableListOutput | ConvertFrom-Json

# Fetch stable package version number.
$variableGroupVersionNumbers = $packageVersions.PSObject.Properties |
Where-Object { $_.Name.StartsWith('stable') -and $_.Name.EndsWith('number') } |
Sort-Object -Property Name |
Select-Object -ExpandProperty Value
[version]$stablePackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2] -join '.')

# Fetch current package version number.
$variableGroupVersionNumbers = $packageVersions.PSObject.Properties |
Where-Object { $_.Name.StartsWith('current') -and $_.Name.EndsWith('number') } |
Sort-Object -Property Name |
Select-Object -ExpandProperty Value
[version] $currentPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')
$currentPackagePrereleaseLabel = $packageVersions.PSObject.Properties['current-prerelease-label'].Value

$currentPackageVersionString = $currentPackageVersion.Major, $currentPackageVersion.Minor, $currentPackageVersion.Build -join '.'
if ($currentPackageVersion.Revision -gt 0) {
    $currentPackageVersionString += "-$currentPackagePrereleaseLabel.$($currentPackageVersion.Revision)"
}

# Fetch next package version number.
$variableGroupVersionNumbers = $packageVersions.PSObject.Properties |
Where-Object { $_.Name.StartsWith('next') -and $_.Name.EndsWith('number') } |
Sort-Object -Property Name |
Select-Object -ExpandProperty Value
[version] $nextPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')
$nextPackagePrereleaseLabel = $packageVersions.PSObject.Properties['next-prerelease-label'].Value

# Fetch current version numbers from assembly.
[version]$currentAssemblyVersion = $null; [version]$currentFileVersion = $null; [version]$currentInformationalVersion = $null

nuget install $PackageName -DependencyVersion Ignore -DirectDownload -ExcludeVersion -NoHttpCache -NonInteractive -OutputDirectory $env:AGENT_TEMPDIRECTORY -PackageSaveMode nupkg -PreRelease -Source 'https://pkgs.dev.azure.com/solobyte/DataStandardizer/_packaging/DataStandardizer/nuget/v3/index.json' -Source 'https://api.nuget.org/v3/index.json' -Verbosity detailed -Version $currentPackageVersionString 2>&1 | Write-Host

[string]$currentPackageFolderPath
$currentPackageArchivePath = Get-ChildItem -Path $env:AGENT_TEMPDIRECTORY -Filter "$PackageName.nupkg" -Recurse | Select-Object -ExpandProperty FullName
if ((-not [string]::IsNullOrEmpty($currentPackageArchivePath)) -and (Test-Path $currentPackageArchivePath -PathType Leaf)) {
    Write-Verbose "Found $PackageName package file at $currentPackageArchivePath."

    $currentPackageFolderPath = $currentPackageArchivePath | Split-Path -Parent
    # Expand-Archive -Path $currentPackageArchivePath -DestinationPath $currentPackageFolderPath -PassThru  # not needed; already expanded by nuget install?
}
else {
    Write-Error "Failed to acquire package $PackageName.  Unable to determine current package assembly version."
}

$currentPackageAssemblyPaths = @(Get-ChildItem -Path $currentPackageFolderPath -Filter "$PackageName.dll" -Recurse |
    Sort-Object -Property FullName |
    Select-Object -ExpandProperty FullName)
foreach ($currentPackageAssemblyPath in $currentPackageAssemblyPaths) {
    $asmName = [System.Reflection.AssemblyName]::GetAssemblyName($currentPackageAssemblyPath)
    if ($null -eq $currentAssemblyVersion) {
        $currentAssemblyVersion = $asmName.Version

        Write-Verbose "Found $PackageName assembly at $currentPackageAssemblyPath."
        Write-Verbose $asmName.ToString()

        $versionInfo = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($currentPackageAssemblyPath)
        $currentFileVersion = $versionInfo.FileVersion
        $currentInformationalVersion = $versionInfo.ProductVersion
    }

    if ($asmName.Version -ne $currentAssemblyVersion) {
        Write-Warning "Detected multiple assembly versions in package $PackageName.  Package will not resolve correctly if referenced."
    }
}

if ($currentPackageAssemblyPaths.Count -eq 0) {
    Write-Error "Package $PackageName is not expanded.  Unable to determine current package assembly version."
}

# Check for package/assembly version match.
if ($null -ne $currentAssemblyVersion -and ($currentAssemblyVersion.Major -ne $currentPackageVersion.Major -or $currentAssemblyVersion.Minor -ne $currentPackageVersion.Minor)) {
    Write-Warning "$PackageName package/assembly version mismatch; $currentPackageVersion != $currentAssemblyVersion."
}

# Add package version numbers to list.
$packageVersions = [PSCustomObject]@{
    PackageName                            = $PackageName
    PackageStableVersion                   = $stablePackageVersion.ToString()
    PackageCurrentVersion                  = $currentPackageVersion.ToString()
    PackageCurrentPrereleaseLabel          = $currentPackagePrereleaseLabel
    PackageNextVersion                     = $nextPackageVersion.ToString()
    PackageNextPrereleaseLabel             = $nextPackagePrereleaseLabel
    PackageProductionVersion               = $currentPackageVersion.ToString()
    PackageProductionVersionString         = $currentPackageVersionString
    PackageProductionPrereleaseLabel       = $currentPackagePrereleaseLabel
    AssemblyProductionVersion              = ${currentAssemblyVersion}?.ToString()
    AssemblyProductionFileVersion          = ${currentFileVersion}?.ToString()
    AssemblyProductionInformationalVersion = ${currentInformationalVersion}?.ToString()
}

Write-Information "Stable package version number is $stablePackageVersion."
Write-Information "Current package version number is $currentPackageVersion."
Write-Information "Current package version is $currentPackageVersionString."
Write-Information "Current package pre-release label is $currentPackagePrereleaseLabel."
Write-Information "Next package version number will be $nextPackageVersion."
Write-Information "Next package pre-release label is $nextPackagePrereleaseLabel."
Write-Information "Current assembly version number is $($currentAssemblyVersion ?? 'unknown')."
Write-Information "Current assembly file version number is $($currentFileVersion ?? 'unknown')."
Write-Information "Current assembly informational version is $($currentInformationalVersion ?? 'unknown')."

[PSCustomObject[]]$packageVersionsList = @()

$raw = $env:VERSIONNUMBERLIST

if (-not [string]::IsNullOrEmpty($raw)) {
    $packageVersionsList = $raw | ConvertFrom-Base64String | ConvertFrom-Json
    Write-Debug "Extracted $($packageVersionsList.Count) existing package versions."
}

$packageVersionsList += $packageVersions
$serializedPackageVersionsList = $packageVersionsList | ConvertTo-Json | ConvertTo-Base64String
Write-Host "##vso[task.setvariable variable=versionNumberList;]$serializedPackageVersionsList"

Write-Debug "Set package versions for $($packageVersionsList.Count) packages."