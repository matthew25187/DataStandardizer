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

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                      
& az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
& az account set -s ${env:SUBSCRIPTION_ID}

# Extract package metadata.
$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ "$PackageName"
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName."
    exit;
}

Write-Information "Found information for package $PackageName." -InformationAction Continue

# Fetch current package version number.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties 
| Where-Object { $_.Name.StartsWith('current') } 
| Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream 
| Sort-Object 
| ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' 
| Out-String -InputObject { $_.Value } -Stream
[version] $currentPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')
$currentPackageVersionString = "$($currentPackageVersion.Major).$($currentPackageVersion.Minor).$($currentPackageVersion.Build)"
if ($variableGroupVersionNumbers[3] -gt 0) {
    $currentPackageVersionString += "-preview.$($currentPackageVersion.Revision)"
}

Write-Information "Current package version is $currentPackageVersion." -InformationAction Continue

# Fetch next package version number.
$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties 
| Where-Object { $_.Name.StartsWith('next') } 
| Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream 
| Sort-Object 
| ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' 
| Out-String -InputObject { $_.Value } -Stream
[version] $nextPackageVersion = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')
$nextPackageVersionString = "$($nextPackageVersion.Major).$($nextPackageVersion.Minor).$($nextPackageVersion.Build)"
if ($variableGroupVersionNumbers[3] -gt 0) {
    $nextPackageVersionString += "-preview.$($nextPackageVersion.Revision)"
}

Write-Information "Next package version will be $nextPackageVersion" -InformationAction Continue

# Fetch current assembly number.
[version]$currentAssemblyVersion = $null

nuget install $PackageName -DirectDownload -ExcludeVersion -NoHttpCache -NonInteractive -OutputDirectory $TempPath -PackageSaveMode nupkg -PreRelease -Source 'https://pkgs.dev.azure.com/solobyte/DataStandardizer/_packaging/DataStandardizer/nuget/v3/index.json' -Source 'https://api.nuget.org/v3/index.json' -Verbosity detailed -Version $currentPackageVersionString 2>&1 | Write-Host

[string]$currentPackageFolderPath
$currentPackageArchivePath = Get-ChildItem -Path $TempPath -Filter "$PackageName.nupkg" -Recurse 
| Select-Object -ExpandProperty FullName
if ((-not [string]::IsNullOrEmpty($currentPackageArchivePath)) -and (Test-Path $currentPackageArchivePath -PathType Leaf)) {
    $currentPackageFolderPath = $currentPackageArchivePath | Split-Path -Parent
    # Expand-Archive -Path $currentPackageArchivePath -DestinationPath $currentPackageFolderPath -PassThru  # not needed; already expanded by nuget install?
}
else {
    Write-Warning "Failed to acquire package $PackageName.  Unable to determine current package assembly version."
}

$currentPackageAssemblyPath = Get-ChildItem -Path $currentPackageFolderPath -Filter "$PackageName.dll" -Recurse
| Sort-Object -Property FullName
| Select-Object -First 1 -ExpandProperty FullName
if (Test-Path $currentPackageAssemblyPath -PathType Leaf) {
    $asm = [System.Reflection.AssemblyName]::GetAssemblyName($currentPackageAssemblyPath)
    $currentAssemblyVersion = $asm.Version

    Write-Information "Current assembly version is $currentAssemblyVersion." -InformationAction Continue
}
else {
    Write-Error "Package $PackageName is not expanded.  Unable to determine current package assembly version."
}

# Check for package/assembly version match.
if ($currentAssemblyVersion.Major -ne $currentPackageVersion.Major -or $currentAssemblyVersion.Minor -ne $currentPackageVersion.Minor) {
    Write-Warning "$PackageName package/assembly version mismatch; $currentPackageVersion != $currentAssemblyVersion."
}

# Add package version numbers to list.
$packageVersions = [PSCustomObject]@{
    PackageName               = $PackageName
    PackageCurrentVersion     = $currentPackageVersion
    PackageNextVersion        = $nextPackageVersion
    PackageProductionVersion  = $currentPackageVersion
    AssemblyCurrentVersion    = $currentAssemblyVersion
    AssemblyProductionVersion = $currentAssemblyVersion
}

[PSCustomObject[]]$packageVersionsList = @()

$raw = $env:VERSIONNUMBERLIST

if (-not [string]::IsNullOrEmpty($raw)) {
    $packageVersionsList = $raw | ConvertFrom-Base64String | ConvertFrom-Json
    Write-Debug "Extracted $($packageVersionsList.Count) existing package versions."
}

$packageVersionsList += $packageVersions
$serializedPackageVersionsList = $packageVersionsList | ConvertTo-Json | ConvertTo-Base64String
Write-Host "##vso[task.setvariable variable=versionNumberList;]$serializedPackageVersionsList"

Write-Information "Set package versions for $($packageVersionsList.Count) packages." -InformationAction Continue