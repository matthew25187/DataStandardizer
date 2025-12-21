param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageInfos,

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

# Fetch current package version numbers.
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

# Fetch next package version numbers.
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

# Fetch current assembly numbers.
[version]$currentAssemblyVersion = $null
& nuget install $PackageName -DirectDownload -ExcludeVersion -NoHttpCache -NonInteractive -OutputDirectory "$(Agent.TempDirectory)" -PackageSaveMode nupkg -PreRelease -Source 'https://pkgs.dev.azure.com/solobyte/DataStandardizer/_packaging/DataStandardizer/nuget/v3/index.json' -Source 'https://api.nuget.org/v3/index.json' -Version $currentPackageVersionString
$currentPackageFolderPath = Join-Path -Path "$(Agent.TempDirectory)" -ChildPath $PackageName
$currentPackageArchivePath = $currentPackageFolderPath | Join-Path -ChildPath "$PackageName.nupkg"
if (Test-Path $currentPackageArchivePath -PathType Leaf) {
    Expand-Archive -Path $currentPackageArchivePath -DestinationPath $currentPackageFolderPath

    $currentPackageAssemblyPath = Get-ChildItem -Path $currentPackageFolderPath -Filter "$PackageName.dll" -Recurse
    | Sort-Object -Property FullName
    | Select-Object -First -ExpandProperty FullName
    $asm = [System.Reflection.AssemblyName]::GetAssemblyName($currentPackageAssemblyPath)
    $currentAssemblyVersion = $asm.Version

    Write-Information "Current assembly version is $currentAssemblyVersion." -InformationAction Continue
}
else {
    Write-Warning "Failed to acquire package $PackageName.  Unable to determine current package assembly version."
}

# Calculate next assembly numbers.
[version]$nextAssemblyFileVersion = $nextPackageVersion
$nextAssemblyVersionRevision = [System.Convert]::ToInt32([datetime]::Now.TimeOfDay.TotalSeconds / 2)
[version]$nextAssemblyVersion = "$($nextPackageVersion.Major).$($nextPackageVersion.Minor).$($nextPackageVersion.Build).$nextAssemblyVersionRevision"
[version]$nextAssemblyInformationalVersion = "$($nextPackageVersion.Major).$($nextPackageVersion.Minor)"

Write-Information "Next assembly file version will be $nextAssemblyFileVersion." -InformationAction Continue
Write-Information "Next assembly version will be $nextAssemblyVersion." -InformationAction Continue
Write-Information "Next assembly informational version will be $nextAssemblyInformationalVersion." -InformationAction Continue

# Add package version numbers to list.
$packageVersions = [PSCustomObject]@{
    PackageName                      = $PackageName
    PackageCurrentVersion            = $currentPackageVersion
    PackageNextVersion               = $nextPackageVersion
    AssemblyCurrentVersion           = $currentAssemblyVersion
    AssemblyNextVersion              = $nextAssemblyVersion
    AssemblyNextFileVersion          = $nextAssemblyFileVersion
    AssemblyNextInformationalVersion = $nextAssemblyInformationalVersion
}

[PSCustomObject[]]$packageVersionsList = @()
if (-not [string]::IsNullOrEmpty("$(versionNumberList)")) {
    $packageVersionsList = "$(versionNumberList)" | ConvertFrom-Base64String | ConvertFrom-Json
    Write-Debug "Extracted $($packageVersionsList.Count) existing package versions."
}

$packageVersionsList += $packageVersions
Write-Host "##vso[task.setvariable variable=packageVersionsList;]$packageVersionsList"

Write-Information "Set package versions list for $($packageVersionsList.Count) packages." -InformationAction Continue