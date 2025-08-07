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

if (${{parameters.traceLevel}} -gt 0) {
    Set-PSDebug -Trace ${{parameters.traceLevel}}
}

# Retrieve the collection of package versions.
$packageVersions = "$(packageVersions)" | ConvertFrom-Base64String | ConvertFrom-Json
$packageVersions

#   Extract the package file to temporary location.
$packageFilePath = Get-ChildItem "$(Build.ArtifactStagingDirectory)/packages/$(BuildConfiguration)" -Recurse -Filter '${{packageName}}*.nupkg' | Select-Object -First 1 -ExpandProperty FullName
Write-Information "Found package file at $packageFilePath." -InformationAction SilentlyContinue

$tempPackagePath = "$(Agent.TempDirectory)/${{packageName}}"
if (-not (Test-Path $tempPackagePath -PathType Container)) {
    New-Item $tempPackagePath -ItemType Directory
}

Expand-Archive -Path $packageFilePath -DestinationPath $tempPackagePath -PassThru

# Load .nuspec file extracted from package.
$packageNuspecFilePath = Get-ChildItem $tempPackagePath -Filter '${{packageName}}.nuspec' | Select-Object -First 1 -ExpandProperty FullName
$packageNuspecDocument = [xml](Get-Content $packageNuspecFilePath)

# Update <dependency> nodes in the .nuspec document with pre-calculated dependency package version numbers.
$dependencyNodes = $packageNuspecDocument.SelectNodes('//dependency')
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
        Write-Information "Updated $dependenciesUpdatedCount dependencies on package $($packageVersion.PackageName)." -InformationAction SilentlyContinue
    }
    else {
        Write-Information "Found no dependencies on package $($packageVersion.PackageName)." -InformationAction SilentlyContinue
    }
}

$packageNuspecDocument.Save($packageNuspecFilePath)
Get-Content $packageNuspecFilePath
Compress-Archive $packageNuspecFilePath -DestinationPath $packageFilePath -Update -Force
