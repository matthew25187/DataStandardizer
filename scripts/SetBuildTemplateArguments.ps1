param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string] $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageInfos,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]$VersionNumbers,

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
$packageVersions = $VersionNumbers | ConvertFrom-Base64String | ConvertFrom-Json | Where-Object -Property PackageName -EQ -Value $PackageName

# Set assembly Build File Version argument.
$fileVersion = $packageVersions.AssemblyProductionFileVersion
Write-Host "##vso[task.setvariable variable=buildFileVersion]$fileVersion"
Write-Information "File Version will be $fileVersion."

# Set assembly Build Assembly Version argument.
$assemblyVersion = $packageVersions.AssemblyProductionVersion
Write-Host "##vso[task.setvariable variable=buildAssemblyVersion]$assemblyVersion"
Write-Information "Assembly Version will be $assemblyVersion."

# Set assembly Build Informational Version argument.
$informationalVersion = $packageVersions.AssemblyProductionInformationalVersion
Write-Host "##vso[task.setvariable variable=buildInformationalVersion]$informationalVersion"
Write-Information "Informational Version will be $informationalVersion."