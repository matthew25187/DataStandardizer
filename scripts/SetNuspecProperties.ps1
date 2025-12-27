param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageInfos,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $SourceRootFolderPath,

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

$projectRootFolderPath = $SourceRootFolderPath | Join-Path -ChildPath $packageInfo.packageSourcePath

$nuspecDocumentFileName = "$PackageName.nuspec"
$nuspecDocumentFilePath = $projectRootFolderPath | Join-Path -ChildPath $nuspecDocumentFileName
$nuspecDocument = [xml](Get-Content $nuspecDocumentFilePath)
$nuspecMetadataNode = $nuspecDocument.SelectSingleNode('/package/metadata')

Write-Information "Loaded .nuspec document $nuspecDocumentFilePath."

$projectDocumentFileName = "$PackageName.csproj"
$projectDocumentFilePath = $projectRootFolderPath | Join-Path -ChildPath $projectDocumentFileName
$projectDocument = [xml](Get-Content $projectDocumentFilePath)

Write-Information "Loaded project document $projectDocumentFilePath."

# Set "requireLicenseAcceptance" property.
$nuspecRequireLicenseAcceptanceNode = $nuspecDocument.SelectSingleNode('/package/metadata/requireLicenseAcceptance')
if ($nuspecRequireLicenseAcceptanceNode -eq $null) {
    $nuspecRequireLicenseAcceptanceNode = $nuspecDocument.CreateElement('requireLicenseAcceptance')
    $nuspecMetadataNode.AppendChild($nuspecRequireLicenseAcceptanceNode)
}

$projectPackageRequireLicenseAcceptanceNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageRequireLicenseAcceptance')
$nuspecRequireLicenseAcceptanceNode.InnerText = $projectPackageRequireLicenseAcceptanceNode.InnerText

Write-Information 'Patched "requireLicenseAcceptance" property.'

# Set "license" property.
$nuspecLicenseNode = $nuspecDocument.SelectSingleNode('/package/metadata/license')
if ($nuspecLicenseNode -eq $null) {
    $nuspecLicenseNode = $nuspecDocument.CreateElement('license')
    $nuspecMetadataNode.AppendChild($nuspecLicenseNode)
}

$projectPackageLicenseExpressionNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageLicenseExpression')
$nuspecLicenseNode.Attributes['type'] = 'expression'
$nuspecLicenseNode.InnerText = $projectPackageLicenseExpressionNode.InnerText

Write-Information 'Patched "license" property.'

# Set "projectUrl" property.
$nuspecProjectUrlNode = $nuspecDocument.SelectSingleNode('/package/metadata/projectUrl')
if ($nuspecProjectUrlNode -eq $null) {
    $nuspecProjectUrlNode = $nuspecDocument.CreateElement('projectUrl')
    $nuspecMetadataNode.AppendChild($nuspecProjectUrlNode)
}

$projectPackageProjectUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageProjectUrl')
$nuspecProjectUrlNode.InnerText = $projectPackageProjectUrlNode.InnerText

Write-Information 'Patched "projectUrl" property.'

# Set "tags" property.
$nuspecTagsNode = $nuspecDocument.SelectSingleNode('/package/metadata/tags')
if ($nuspecTagsNode -eq $null) {
    $nuspecTagsNode = $nuspecDocument.CreateElement('tags')
    $nuspecMetadataNode.AppendChild($nuspecTagsNode)
}

$projectPackageTagsNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageTags')
$nuspecTagsNode.InnerText = $projectPackageTagsNode.InnerText

Write-Information 'Patched "tags" property.'

# Set "repository" property.
$nuspecRepositoryNode = $nuspecDocument.SelectSingleNode('/package/metadata/repository')
if ($nuspecRepositoryNode -eq $null) {
    $nuspecRepositoryNode = $nuspecDocument.CreateElement('repository')
    $nuspecMetadataNode.AppendChild($nuspecRepositoryNode)
}

$projectRepositoryTypeNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/RepositoryType')
$projectRepositoryUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/RepositoryUrl')
$nuspecRepositoryNode.Attributes['type'] = $projectRepositoryTypeNode.InnerText
$nuspecRepositoryNode.Attributes['url'] = $projectRepositoryUrlNode.InnerText

Write-Information 'Patched "repository" property.'

# Save changes to .nuspec file.
$nuspecDocument.Save($nuspecDocumentFilePath)

Write-Information "Saved changes to .nuspec document $nuspecDocumentFilePath."

Write-Debug "Patched file $($nuspecDocumentFileName):"
Write-Debug (Get-Content $nuspecDocumentFilePath -Raw)