[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]    $PackageName,

    [Parameter(Mandatory)]
    [string]    $PackageVersionList,

    [Parameter()]
    [string]    $PackageSearchRootPath,

    [Parameter()]
    [string]    $BuildConfiguration,

    [Parameter()]
    [string]    $TempPath,

    [Parameter()]
    [int]    $TraceLevel = 0
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

# Retrieve the collection of package versions.
$packageVersions = $PackageVersionList | ConvertFrom-Base64String | ConvertFrom-Json
Write-Verbose -Message ($packageVersions | Out-String)

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

# Update <dependency> nodes in the .nuspec document with pre-calculated dependency package version numbers.
$namespaceManager = New-Object System.Xml.XmlNamespaceManager($packageNuspecDocument.NameTable)
$namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd")

$dependencyNodes = $packageNuspecDocument.SelectNodes('//ns:dependency', $namespaceManager)
if ($dependencyNodes.Count -eq 0) {
    Write-Warning "Found $($dependencyNodes.Count) dependencies to process."
    exit;
}

$totalDependenciesUpdatedCount = 0
foreach ($packageVersion in $packageVersions) {
    $dependenciesUpdatedCount = 0

    foreach ($dependencyNode in $dependencyNodes) {
        if ($dependencyNode.Attributes['id'].Value -ne $packageVersion.PackageName) {
            continue;
        }
                                                                                                                    
        $dependencyNode.Attributes['version'].Value = $packageVersion.PackageVersion
        $dependenciesUpdatedCount++
    }

    if ($dependenciesUpdatedCount -gt 0) {
        Write-Information "Updated $dependenciesUpdatedCount dependencies on package $($packageVersion.PackageName)."
    }
    else {
        Write-Information "Found no dependencies on package $($packageVersion.PackageName)."
    }

    $totalDependenciesUpdatedCount += $dependenciesUpdatedCount
}

# Replace the .nuspec file in the package with the copy having updated dependency version numbers.
if ($totalDependenciesUpdatedCount -gt 0) {
    $packageNuspecDocument.Save($packageNuspecFilePath)
    Compress-Archive $packageNuspecFilePath -DestinationPath $packageFilePath -Update -PassThru
}