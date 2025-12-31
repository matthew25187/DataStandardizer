#Requires -Version 7.1

param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageInfos,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $TempPath,

    [Parameter()]
    [string]    $OutputInformationPreference = 'SilentlyContinue',

    [Parameter()]
    [string]    $OutputVerbosePreference = 'SilentlyContinue',

    [Parameter()]
    [string]    $OutputDebugPreference = 'SilentlyContinue',

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

$InformationPreference = $OutputInformationPreference
$VerbosePreference = $OutputVerbosePreference
$DebugPreference = $OutputDebugPreference

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                      
az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
az account set -s ${env:SUBSCRIPTION_ID}

# Extract package metadata.
$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ "$PackageName"
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName."
}

Write-Information "Found information for package $PackageName."

# Fetch current package version number.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties |
Where-Object { $_.Name.StartsWith('current') } |
Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream |
Sort-Object |
ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' |
Out-String -InputObject { $_.Value } -Stream
[version] $currentPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')

$currentPackageVersionString = $currentPackageVersion.Major, $currentPackageVersion.Minor, $currentPackageVersion.Build -join '.'
if ($currentPackageVersion.Revision -gt 0) {
    $currentPackageVersionString += "-preview.$($currentPackageVersion.Revision)"
}

# Fetch next package version number.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties |
Where-Object { $_.Name.StartsWith('next') } |
Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream |
Sort-Object |
ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' |
Out-String -InputObject { $_.Value } -Stream
[version] $nextPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')

# Fetch current assembly number.
[version]$currentAssemblyVersion = $null

nuget install $PackageName -DependencyVersion Ignore -DirectDownload -ExcludeVersion -NoHttpCache -NonInteractive -OutputDirectory $TempPath -PackageSaveMode nupkg -PreRelease -Source 'https://pkgs.dev.azure.com/solobyte/DataStandardizer/_packaging/DataStandardizer/nuget/v3/index.json' -Source 'https://api.nuget.org/v3/index.json' -Verbosity detailed -Version $currentPackageVersionString 2>&1 | Write-Host

[string]$currentPackageFolderPath
$currentPackageArchivePath = Get-ChildItem -Path $TempPath -Filter "$PackageName.nupkg" -Recurse | Select-Object -ExpandProperty FullName
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
    $asm = [System.Reflection.AssemblyName]::GetAssemblyName($currentPackageAssemblyPath)
    if ($null -eq $currentAssemblyVersion) {
        $currentAssemblyVersion = $asm.Version

        Write-Verbose "Found $PackageName assembly at $currentPackageAssemblyPath."
        Write-Verbose $asm.ToString()
    }

    if ($asm.Version -ne $currentAssemblyVersion) {
        Write-Warning "Detected multiple assembly versions in package $PackageName.  Package will not resolve correctly if referenced."
    }
}

if ($currentPackageAssemblyPaths.Count -eq 0) {
    Write-Warning "Package $PackageName is not expanded.  Unable to determine current package assembly version."
}

# Check for package/assembly version match.
if ($null -ne $currentAssemblyVersion -and ($currentAssemblyVersion.Major -ne $currentPackageVersion.Major -or $currentAssemblyVersion.Minor -ne $currentPackageVersion.Minor)) {
    Write-Warning "$PackageName package/assembly version mismatch; $currentPackageVersion != $currentAssemblyVersion."
}

# Add package version numbers to list.
$packageVersions = [PSCustomObject]@{
    PackageName                    = $PackageName
    PackageCurrentVersion          = $currentPackageVersion.ToString()
    PackageNextVersion             = $nextPackageVersion.ToString()
    PackageProductionVersion       = $currentPackageVersion.ToString()
    PackageProductionVersionString = $currentPackageVersionString
    AssemblyProductionVersion      = ${currentAssemblyVersion}?.ToString() ?? "$($currentPackageVersion.Major).$($currentPackageVersion.Minor).$($currentPackageVersion.Build).*"
}

Write-Information "Current package version number is $currentPackageVersion."
Write-Information "Current package version is $currentPackageVersionString."
Write-Information "Next package version number will be $nextPackageVersion"
Write-Information "Current assembly version number is $($currentAssemblyVersion ?? 'unknown')."

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