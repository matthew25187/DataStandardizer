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

function Set-XmlAttribute {
    param($node, $name, $value)

    $attr = $node.Attributes[$name]
    if (-not $attr) {
        $attr = $node.OwnerDocument.CreateAttribute($name)
        $node.Attributes.Append($attr) | Out-Null
    }
    $attr.Value = $value
}

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
    $nuspecMetadataNode.AppendChild($nuspecRequireLicenseAcceptanceNode) | Out-Null
}

$projectPackageRequireLicenseAcceptanceNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageRequireLicenseAcceptance')
$nuspecRequireLicenseAcceptanceNode.InnerText = $projectPackageRequireLicenseAcceptanceNode.InnerText

Write-Information 'Patched "requireLicenseAcceptance" property.'

# Set "license" property.
$nuspecLicenseNode = $nuspecDocument.SelectSingleNode('/package/metadata/license')
if ($nuspecLicenseNode -eq $null) {
    $nuspecLicenseNode = $nuspecDocument.CreateElement('license')
    $nuspecMetadataNode.AppendChild($nuspecLicenseNode) | Out-Null
}

$projectPackageLicenseExpressionNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageLicenseExpression')
Set-XmlAttribute $nuspecLicenseNode 'type' 'expression'
$nuspecLicenseNode.InnerText = $projectPackageLicenseExpressionNode.InnerText

Write-Information 'Patched "license" property.'

# Set "projectUrl" property.
$nuspecProjectUrlNode = $nuspecDocument.SelectSingleNode('/package/metadata/projectUrl')
if ($nuspecProjectUrlNode -eq $null) {
    $nuspecProjectUrlNode = $nuspecDocument.CreateElement('projectUrl')
    $nuspecMetadataNode.AppendChild($nuspecProjectUrlNode) | Out-Null
}

$projectPackageProjectUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageProjectUrl')
$nuspecProjectUrlNode.InnerText = $projectPackageProjectUrlNode.InnerText

Write-Information 'Patched "projectUrl" property.'

# Set "tags" property.
$nuspecTagsNode = $nuspecDocument.SelectSingleNode('/package/metadata/tags')
if ($nuspecTagsNode -eq $null) {
    $nuspecTagsNode = $nuspecDocument.CreateElement('tags')
    $nuspecMetadataNode.AppendChild($nuspecTagsNode) | Out-Null
}

$projectPackageTagsNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageTags')
$nuspecTagsNode.InnerText = $projectPackageTagsNode.InnerText

Write-Information 'Patched "tags" property.'

# Set "repository" property.
$nuspecRepositoryNode = $nuspecDocument.SelectSingleNode('/package/metadata/repository')
if ($nuspecRepositoryNode -eq $null) {
    $nuspecRepositoryNode = $nuspecDocument.CreateElement('repository')
    $nuspecMetadataNode.AppendChild($nuspecRepositoryNode) | Out-Null
}

$projectRepositoryTypeNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/RepositoryType')
Set-XmlAttribute $nuspecRepositoryNode 'type' $projectRepositoryTypeNode.InnerText

$projectRepositoryUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/RepositoryUrl')
Set-XmlAttribute $nuspecRepositoryNode 'url' $projectRepositoryUrlNode.InnerText

Write-Information 'Patched "repository" property.'

# Save changes to .nuspec file.
$nuspecDocument.Save($nuspecDocumentFilePath)

Write-Information "Saved changes to .nuspec document $nuspecDocumentFilePath."

Write-Debug "Patched .nuspec file $($nuspecDocumentFileName):"
Write-Debug (Get-Content $nuspecDocumentFilePath -Raw)