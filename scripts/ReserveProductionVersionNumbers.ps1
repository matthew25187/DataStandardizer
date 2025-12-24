param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [string]    $PackageInfos,

    [Parameter(Mandatory)]
    [string]    $PackageVersionNumbers,

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

# Set environment variable for current process.
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN

# Get information about the current package.
$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "Package information for $PackageName not found."
}

# Update version numbers in pipeline.
$packageVersions = $PackageVersionNumbers | ConvertFrom-Base64String | ConvertFrom-Json | Where-Object -Property PackageName -EQ -Value $PackageName
[version]$postProductionPackageVersion = $packageVersions.PackagePostProductionVersion

az devops configure -d organization=${env:ORGANIZATION_URL}
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name next-major-number --project ${env:PROJECT_NAME} --value $postProductionPackageVersion.Major --verbose
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name next-minor-number --project ${env:PROJECT_NAME} --value $postProductionPackageVersion.Minor --verbose
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name next-patch-number --project ${env:PROJECT_NAME} --value $postProductionPackageVersion.Build --verbose
az pipelines variable-group variable update --group-id $packageInfo.variableGroupId --name next-preview-number --project ${env:PROJECT_NAME} --value $postProductionPackageVersion.Revision --verbose

Write-Debug "Reserved package version $postProductionPackageVersion."