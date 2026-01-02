#Requires -Version 7.0

param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $EncodedPackageVersions,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageSearchRootPath,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $BuildConfiguration,

    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $TempPath,

    [Parameter()]
    [ValidateNotNull()]
    [string]    $EncodedReleaseNotes,

    [Parameter()]
    [int]    $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Retrieve the collection of package versions.
$packageVersionsList = $EncodedPackageVersions | ConvertFrom-Base64String | ConvertFrom-Json

#   Extract the package file to temporary location.
$packageSearchPath = $PackageSearchRootPath | Join-Path -ChildPath 'packages' | Join-Path -ChildPath $BuildConfiguration
$packageFilePath = Get-ChildItem $packageSearchPath -Recurse -Filter "$PackageName*.nupkg" | Select-Object -First 1 -ExpandProperty FullName
Write-Information "Found package file at $packageFilePath."

$tempPackagePath = $TempPath | Join-Path -ChildPath $PackageName
if (-not (Test-Path $tempPackagePath -PathType Container)) {
    New-Item $tempPackagePath -ItemType Directory
}

Expand-Archive -Path $packageFilePath -DestinationPath $tempPackagePath -PassThru

# Load .nuspec file extracted from package.
$packageNuspecFilePath = Get-ChildItem $tempPackagePath -Filter "$PackageName.nuspec" | Select-Object -First 1 -ExpandProperty FullName
$packageNuspecDocument = [xml](Get-Content $packageNuspecFilePath)
$metadataNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata', $namespaceManager)

$namespaceUri = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($packageNuspecDocument.NameTable)
$namespaceManager.AddNamespace("ns", $namespaceUri)

$isChangedPackageNuspecDocument = $false

# Validate "id" property.
$idLengthLimit = 128
$idNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:id', $namespaceManager)
if ($idNode -ne $null -and $idNode.InnerText.Length -gt $idLengthLimit) {
    Write-Error "Property 'id' exceeds the limit of $idLengthLimit characters for NuGet."
}

Write-Verbose "Validated 'id' property - OK."

# Validate "version" property.
$versionLengthLimit = 64
$versionNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:version', $namespaceManager)
if ($versionNode -ne $null -and $versionNode.InnerText.Length -gt $versionLengthLimit) {
    Write-Error "Property 'version' exceeds the limit of $versionLengthLimit characters for NuGet."
}

Write-Verbose "Validated 'version' property - OK."

# Validate "description" property.
$descriptionLengthLimit = 4000
$descriptionNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:description', $namespaceManager)
if ((-not [string]::IsNullOrEmpty(${descriptionNode}?.InnerText)) -and $descriptionNode.InnerText.Length -gt $descriptionLengthLimit) {
    $descriptionNode.InnerText = $descriptionNode.InnerText.Substring(0, $descriptionLengthLimit)
    $isChangedPackageNuspecDocument = $true

    Write-Warning "Property 'description' truncated to $descriptionLengthLimit characters."
}

Write-Verbose "Validated 'description' property - OK."

# Validate "projectUrl" property.
$projectUrlLengthLimit = 4000
$projectUrlNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:projectUrl', $namespaceManager)
if ($projectUrlNode -ne $null -and $projectUrlNode.InnerText.Length -gt $projectUrlLengthLimit) {
    Write-Error "Property 'projectUrl' exceeds the limit of $projectUrlLengthLimit characters for NuGet."
}

Write-Verbose "Validated 'projectUrl' property - OK."

# Update "releaseNotes" property.
$releaseNotesLengthLimit = 35000
$releaseNotes = (-not [string]::IsNullOrEmpty($EncodedReleaseNotes)) ? ($EncodedReleaseNotes | ConvertFrom-Base64String) : [string]::Empty
$releaseNotesNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:releaseNotes', $namespaceManager)
if (-not [string]::IsNullOrWhiteSpace($releaseNotes)) {
    if ($releaseNotesNode -eq $null) {
        $releaseNotesNode = $packageNuspecDocument.CreateElement('ns', 'releaseNotes', $namespaceUri)
        $metadataNode.AppendChild($releaseNotesNode)
    }

    $releaseNotesNode.InnerText = $releaseNotes
    $isChangedPackageNuspecDocument = $true
}
elseif ($releaseNotesNode -ne $null) {
    $metadataNode.RemoveChild($releaseNotesNode)
    $isChangedPackageNuspecDocument = $true
}
if ((-not [string]::IsNullOrEmpty(${releaseNotesNode}?.InnerText)) -and $releaseNotesNode.InnerText.Length -gt $releaseNotesLengthLimit) {
    $releaseNotesNode.InnerText = $releaseNotesNode.InnerText.Substring(0, $releaseNotesLengthLimit)
    $isChangedPackageNuspecDocument = $true

    Write-Warning "Property 'releaseNotes' truncated to $releaseNotesLengthLimit characters."
}

Write-Verbose "Validated 'releaseNotes' property - OK."

# Validate "copyright" property.
$copyrightLengthLimit = 4000
$copyrightNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:copyright', $namespaceManager)
if ($copyrightNode -ne $null -and $copyrightNode.InnerText.Length -gt $copyrightLengthLimit) {
    Write-Error "Property 'copyright' exceeds the limit of $copyrightLengthLimit characters for NuGet."
}

Write-Verbose "Validated 'copyright' property - OK."

# Validate "tags" property.
$tagsLengthLimit = 4000
$tagsNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:tags', $namespaceManager)
if ((-not [string]::IsNullOrEmpty(${tagsNode}?.InnerText)) -and $tagsNode.InnerText.Length -gt $tagsLengthLimit) {
    $tagsNode.InnerText = $tagsNode.InnerText.Substring(0, $tagsLengthLimit)
    $isChangedPackageNuspecDocument = $true

    Write-Warning "Property 'tags' truncated to $tagsLengthLimit characters."
}

Write-Verbose "Validated 'tags' property - OK."

# Validate "repository" property.
$repositoryNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:repository', $namespaceManager)

$repositoryTypeLengthLimit = 100
if ($repositoryNode.Attributes['type'].Value.Length -gt $repositoryTypeLengthLimit) {
    Write-Error "Attribute 'type' exceeds the limit of $repositoryTypeLengthLimit characters for NuGet."
}

$repositoryUrlLengthLimit = 4000
if ($repositoryNode.Attributes['repository'].Value.Length -gt $repositoryUrlLengthLimit) {
    Write-Error "Attribute 'repository' exceeds the limit of $repositoryUrlLengthLimit characters for NuGet."
}

Write-Verbose "Validated 'repository' property - OK."

# Validate "title" property.
$titleLengthLimit = 256
$titleNode = $packageNuspecDocument.SelectSingleNode('/ns:package/ns:metadata/ns:title', $namespaceManager)
if ((-not [string]::IsNullOrEmpty(${titleNode}?.InnerText)) -and $titleNode.InnerText.Length -gt $titleLengthLimit) {
    $titleNode.InnerText = $titleNode.InnerText.Substring(0, $titleLengthLimit)
    $isChangedPackageNuspecDocument = $true

    Write-Warning "Property 'title' truncated to $titleLengthLimit characters."
}

Write-Verbose "Validated 'title' property - OK."

# Update <dependency> nodes in the .nuspec document with pre-calculated dependency package version numbers.
$dependencyNodes = $packageNuspecDocument.SelectNodes('//ns:dependency', $namespaceManager)
if ($dependencyNodes.Count -eq 0) {
    Write-Warning "Found $($dependencyNodes.Count) dependencies to process."
}

$dependenciesUpdatedCount = 0
foreach ($dependencyNode in $dependencyNodes) {
    $dependencyName = $dependencyNode.Attributes['id'].Value
    $packageVersions = $packageVersionsList | Where-Object -Property PackageName -EQ -Value $dependencyName
    if ($null -eq $packageVersions) {
        Write-Verbose "Found no versions for $dependencyName; skipped."
        continue
    }

    $dependencyNode.Attributes['version'].Value = $packageVersions.PackageProductionVersion
    Write-Verbose "Updated dependency $dependencyName to v$($packageVersions.PackageProductionVersion)."

    $dependenciesUpdatedCount++
    $isChangedPackageNuspecDocument = $true
}

Write-Information "Updated $dependenciesUpdatedCount dependencies."

# Replace the .nuspec file in the package with the copy having updated dependency version numbers.
if ($isChangedPackageNuspecDocument) {
    $packageNuspecDocument.Save($packageNuspecFilePath)
    Compress-Archive $packageNuspecFilePath -DestinationPath $packageFilePath -Update -PassThru

    Write-Debug "Patched NuSpec file $(Split-Path $packageNuspecFilePath -Leaf):"
    Write-Debug (Get-Content -Path $packageNuspecFilePath -Raw)
}