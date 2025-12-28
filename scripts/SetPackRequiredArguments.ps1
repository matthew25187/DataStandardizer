#Requires -Version 7.1

param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter()]
    [int]   $TraceLevel = 0
)

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$packageInfo = ($PackageInfos | ConvertFrom-Json) | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName"
}

$projectDocumentFileName = "$PackageName.csproj"
$projectDocumentFilePath = $SourceRootFolderPath | Join-Path -ChildPath $packageInfo.packageSourcePath | Join-Path -ChildPath $projectDocumentFileName
$projectDocument = [xml](Get-Content $projectDocumentFilePath)
$propertyGroupNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup')

# Get Description value.
$description = $propertyGroupNode.SelectSingleNode('Description')?.InnerText
if ($null -eq $description) {
    Write-Error "Description not found in $PackageName"
}

# Get Authors value.
$authors = $propertyGroupNode.SelectSingleNode('Authors')?.InnerText
if ($null -eq $authors) {
    Write-Error "Authors not found in $PackageName"
}

# Save variables to pipeline.
Write-Host "##vso[task.setvariable variable=descriptionArg]$description"
Write-Host "##vso[task.setvariable variable=authorsArg]$authors"