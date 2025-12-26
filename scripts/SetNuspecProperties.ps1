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

$nuspecDocumentFileName = "$PackageName.nuspec"
$nuspecDocument = [xml](Get-Content $nuspecDocumentFileName)
$nuspecMetadataNode = $nuspecDocument.SelectSingleNode('/package/metadata')

$projectDocumentFileName = "$PackageName.csproj"
$projectDocument = [xml](Get-Content $projectDocumentFileName)

# Set "requireLicenseAcceptance" property.
$nuspecRequireLicenseAcceptanceNode = $nuspecDocument.SelectSingleNode('/package/metadata/requireLicenseAcceptance')
if ($nuspecRequireLicenseAcceptanceNode -eq $null) {
    $nuspecRequireLicenseAcceptanceNode = $nuspecDocument.CreateElement('requireLicenseAcceptance')
    $nuspecMetadataNode.AppendChild($nuspecRequireLicenseAcceptanceNode)
}

$projectPackageRequireLicenseAcceptanceNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageRequireLicenseAcceptance')
$nuspecRequireLicenseAcceptanceNode.InnerText = $projectPackageRequireLicenseAcceptanceNode.InnerText

# Set "license" property.
$nuspecLicenseNode = $nuspecDocument.SelectSingleNode('/package/metadata/license')
if ($nuspecLicenseNode -eq $null) {
    $nuspecLicenseNode = $nuspecDocument.CreateElement('license')
    $nuspecMetadataNode.AppendChild($nuspecLicenseNode)
}

$projectPackageLicenseExpressionNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageLicenseExpression')
$nuspecLicenseNode.Attributes['type'] = 'expression'
$nuspecLicenseNode.InnerText = $projectPackageLicenseExpressionNode.InnerText

# Set "projectUrl" property.
$nuspecProjectUrlNode = $nuspecDocument.SelectSingleNode('/package/metadata/projectUrl')
if ($nuspecProjectUrlNode -eq $null) {
    $nuspecProjectUrlNode = $nuspecDocument.CreateElement('projectUrl')
    $nuspecMetadataNode.AppendChild($nuspecProjectUrlNode)
}

$projectPackageProjectUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageProjectUrl')
$nuspecProjectUrlNode.InnerText = $projectPackageProjectUrlNode.InnerText

# Set "tags" property.
$nuspecTagsNode = $nuspecDocument.SelectSingleNode('/package/metadata/tags')
if ($nuspecTagsNode -eq $null) {
    $nuspecTagsNode = $nuspecDocument.CreateElement('tags')
    $nuspecMetadataNode.AppendChild($nuspecTagsNode)
}

$projectPackageTagsNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageTags')
$nuspecTagsNode.InnerText = $projectPackageTagsNode.InnerText

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

# Save changes to .nuspec file.
$nuspecDocument.Save($nuspecDocumentFileName)

Write-Debug "Patched file $($nuspecDocumentFileName):"
Write-Debug (Get-Content $nuspecDocumentFileName -Raw)