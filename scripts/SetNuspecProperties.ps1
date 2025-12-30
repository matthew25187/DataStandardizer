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
    [string]    $SourceRootFolderPath,

    [Parameter()]
    [ValidateNotNull()]
    [string]    $EncodedReleaseNotes,

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

function Remove-MetadataProperty {
    param ($document, $name)
    
    $nuspecNode = $document.SelectSingleNode("/package/metadata/$name")
    if ($null -ne $nuspecNode) {
        $metadataNode = $document.SelectSingleNode('/package/metadata')
        $metadataNode.RemoveChild($nuspecNode) | Out-Null
    }
}

function Set-MetadataProperty {
    [OutputType([System.Xml.XmlNode])]
    param ($document, $name, $value)
    
    $nuspecNode = $document.SelectSingleNode("/package/metadata/$name")
    if ($null -eq $nuspecNode) {
        $metadataNode = $document.SelectSingleNode('/package/metadata')

        $nuspecNode = $document.CreateElement($name)
        $metadataNode.AppendChild($nuspecNode) | Out-Null
    }

    $nuspecNode.InnerText = $value
    return $nuspecNode
}

function Set-XmlAttribute {
    param($node, $name, $value)

    $attr = ${node.Attributes}?[$name]
    if (-not $attr) {
        $attr = $node.OwnerDocument.CreateAttribute($name)
        $node.Attributes.Append($attr) | Out-Null
    }
    $attr.Value = $value
}

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName"
}

$projectRootFolderPath = $SourceRootFolderPath | Join-Path -ChildPath $packageInfo.packageSourcePath

$nuspecDocumentFileName = "$PackageName.nuspec"
$nuspecDocumentFilePath = $projectRootFolderPath | Join-Path -ChildPath $nuspecDocumentFileName
$nuspecDocument = [xml](Get-Content $nuspecDocumentFilePath)

Write-Information "Loaded .nuspec document $nuspecDocumentFilePath."

$projectDocumentFileName = "$PackageName.csproj"
$projectDocumentFilePath = $projectRootFolderPath | Join-Path -ChildPath $projectDocumentFileName
$projectDocument = [xml](Get-Content $projectDocumentFilePath)

Write-Information "Loaded project document $projectDocumentFilePath."

# Set "description" property.
$projectDocumentNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/Description')
Set-MetadataProperty $nuspecDocument 'description' $projectDocumentNode.InnerText | Out-Null

Write-Information 'Patched "description" property.'

# Set "authors" property.
$projectAuthorsNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/Authors')
Set-MetadataProperty $nuspecDocument 'authors' $projectAuthorsNode.InnerText | Out-Null

Write-Information 'Patched "authors" property.'

# Set "projectUrl" property.
$projectPackageProjectUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageProjectUrl')
if (-not [string]::IsNullOrEmpty(${projectPackageProjectUrlNode}?.InnerText)) {
    Set-MetadataProperty $nuspecDocument 'projectUrl' $projectPackageProjectUrlNode.InnerText | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'projectUrl'
}

Write-Information 'Patched "projectUrl" property.'

# Set "license" property.
$projectPackageLicenseExpressionNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageLicenseExpression')
if (-not [string]::IsNullOrEmpty(${projectPackageLicenseExpressionNode}?.InnerText)) {
    $nuspecLicenseNode = Set-MetadataProperty $nuspecDocument 'license' $projectPackageLicenseExpressionNode.InnerText
    Set-XmlAttribute $nuspecLicenseNode 'type' 'expression'
}
else {
    Remove-MetadataProperty $nuspecDocument 'license'
}

Write-Information 'Patched "license" property.'

# Set "icon" property.
$projectPackageIconNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageIcon')
$iconFileName = ${projectPackageIconNode}?.InnerText
if (-not [string]::IsNullOrEmpty($iconFileName)) {
    Set-MetadataProperty $nuspecDocument 'icon' $iconFileName | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'icon'
}

Write-Information 'Patched "icon" property.'

# Set "readme" property.
$projectPackageReadmeFileNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageReadmeFile')
$readmeFileName = ${projectPackageReadmeFileNode}?.InnerText
if (-not [string]::IsNullOrEmpty($readmeFileName)) {
    Set-MetadataProperty $nuspecDocument 'readme' $readmeFileName | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'readme'
}

Write-Information 'Patched "readme" property.'

# Set "requireLicenseAcceptance" property.
$projectPackageRequireLicenseAcceptanceNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageRequireLicenseAcceptance')
if (-not [string]::IsNullOrEmpty(${projectPackageRequireLicenseAcceptanceNode}?.InnerText)) {
    Set-MetadataProperty $nuspecDocument 'requireLicenseAcceptance' $projectPackageRequireLicenseAcceptanceNode.InnerText.ToLowerInvariant() | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'requireLicenseAcceptance'
}

Write-Information 'Patched "requireLicenseAcceptance" property.'

# Set "releaseNotes" property.
$releaseNotes = $EncodedReleaseNotes | ConvertFrom-Base64String
if (-not [string]::IsNullOrWhiteSpace($releaseNotes)) {
    Set-MetadataProperty $nuspecDocument 'releaseNotes' $releaseNotes | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'releaseNotes'
}

Write-Information 'Patched "releaseNotes" property.'

# Set "copyright" property.
$projectCopyrightNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/Copyright')
if (-not [string]::IsNullOrEmpty(${projectCopyrightNode}?.InnerText)) {
    Set-MetadataProperty $nuspecDocument 'copyright' $projectCopyrightNode.InnerText | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'copyright'
}

Write-Information 'Patched "copyright" property.'

# Set "tags" property.
$projectPackageTagsNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageTags')
if (-not [string]::IsNullOrEmpty(${projectPackageTagsNode}?.InnerText)) {
    Set-MetadataProperty $nuspecDocument 'tags' $projectPackageTagsNode.InnerText | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'tags'
}

Write-Information 'Patched "tags" property.'

# Set "repository" property.
$nuspecRepositoryNode = Set-MetadataProperty $nuspecDocument 'repository' ''

$projectRepositoryTypeNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/RepositoryType')
Set-XmlAttribute $nuspecRepositoryNode 'type' $projectRepositoryTypeNode.InnerText

$projectRepositoryUrlNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/RepositoryUrl')
Set-XmlAttribute $nuspecRepositoryNode 'url' $projectRepositoryUrlNode.InnerText

Write-Information 'Patched "repository" property.'

# Set "title" property.
$projectAssemblyTitleNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/AssemblyTitle')
if (-not [string]::IsNullOrEmpty(${projectAssemblyTitleNode}?.InnerText)) {
    Set-MetadataProperty $nuspecDocument 'title' $projectAssemblyTitleNode.InnerText | Out-Null
}
else {
    Remove-MetadataProperty $nuspecDocument 'title'
}

Write-Information 'Patched "title" property.'

# Set "files" section.
$nuspecFilesNode = $nuspecDocument.SelectSingleNode('/package/files')
if ($nuspecFilesNode -eq $null) {
    $nuspecPackageNode = $nuspecDocument.SelectSingleNode('/package')
    $nuspecFilesNode = $nuspecDocument.CreateElement('files')
    $nuspecPackageNode.AppendChild($nuspecFilesNode) | Out-Null
}

if (-not [string]::IsNullOrEmpty($iconFileName)) {
    $nuspecIconFileNode = $nuspecDocument.CreateElement('file')
    Set-XmlAttribute $nuspecIconFileNode 'src' "images/$(Split-Path -Path $iconFileName -Leaf)"
    Set-XmlAttribute $nuspecIconFileNode 'target' '.'
    $nuspecFilesNode.AppendChild($nuspecIconFileNode) | Out-Null
}

if (-not [string]::IsNullOrEmpty($readmeFileName)) {
    $nuspecReadmeFileNode = $nuspecDocument.CreateElement('file')
    Set-XmlAttribute $nuspecReadmeFileNode 'src' (Split-Path -Path $readmeFileName -Leaf)
    Set-XmlAttribute $nuspecReadmeFileNode 'target' '.'
    $nuspecFilesNode.AppendChild($nuspecReadmeFileNode) | Out-Null
}

Write-Information 'Added "files" section.'

# Save changes to .nuspec file.
$nuspecDocument.Save($nuspecDocumentFilePath)

Write-Information "Saved changes to .nuspec document $nuspecDocumentFilePath."

Write-Debug "Patched .nuspec file $($nuspecDocumentFileName):"
Write-Debug (Get-Content $nuspecDocumentFilePath -Raw)