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

$projectDocumentFileName = "$PackageName.csproj"
$projectDocumentFilePath = $SourceRootFolderPath | Join-Path -ChildPath $packageInfo.packageSourcePath
$projectDocument = [xml](Get-Content $projectDocumentFilePath)
$projectPropertyGroupNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup')

Write-Information "Loaded project document $projectDocumentFilePath"

$nuspecDocumentFileName = "$PackageName.nuspec"

# Set "IsPackable" property.
$projectIsPackableNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/IsPackable')
if ($projectIsPackableNode -eq $null) {
    $projectIsPackableNode = $projectDocument.CreateElement('IsPackable')
    $projectPropertyGroupNode.AppendChild($projectIsPackableNode)
}

$projectIsPackableNode.InnerText = [bool]::TrueString

Write-Information 'Patched "IsPackable" property.'

# Set "NuspecFile" property.
$projectNuspecFileNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/NuspecFile')
if ($projectNuspecFileNode -eq $null) {
    $projectNuspecFileNode = $projectDocument.CreateElement('NuspecFile')
    $projectPropertyGroupNode.AppendChild($projectNuspecFileNode)
}

$projectNuspecFileNode.InnerText = $nuspecDocumentFileName

Write-Information 'Patched "NuspecFile" property.'

# Set "NuspecProperties" property.
$projectNuspecPropertiesNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/NuspecProperties')
if ($projectNuspecPropertiesNode -eq $null) {
    $projectNuspecPropertiesNode = $projectDocument.CreateElement('NuspecProperties')
    $projectPropertyGroupNode.AppendChild($projectNuspecPropertiesNode)
}

$nuspecProperties = @{
    'version'     = '$(PackageVersion)'
    'title'       = '$(Title)'
    'authors'     = '$(Authors)'
    'description' = '$(Description)'
    'icon'        = '$(PackageIcon)'
    'readme'      = '$(PackageReadmeFile)'
    'copyright'   = '$(Copyright)'
}

$projectPackageIdNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageId')
if ($projectPackageIdNode -ne $null) {
    $nuspecProperties.Add('id', '$(PackageId)')
}
else {
    $nuspecProperties.Add('id', '$(AssemblyName)')
}

$projectNuspecPropertiesNode.InnerText = ($nuspecProperties.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ';'

Write-Information 'Patched "NuspecProperties" property.'

# Save changes to project file.
$projectDocument.Save($projectDocumentFilePath)

Write-Information "Saved changes to project document $projectDocumentFilePath."

Write-Debug "Patched project file $($projectDocumentFileName):"
Write-Debug (Get-Content $projectDocumentFilePath -Raw)