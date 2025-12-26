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

$projectDocumentFileName = "$PackageName.csproj"
$projectDocument = [xml](Get-Content $projectDocumentFileName)
$projectPropertyGroupNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup')

$nuspecDocumentFileName = "$PackageName.nuspec"

# Set "IsPackable" property.
$projectIsPackableNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/IsPackable')
if ($projectIsPackableNode -eq $null) {
    $projectIsPackableNode = $projectDocument.CreateElement('IsPackable')
    $projectPropertyGroupNode.AppendChild($projectIsPackableNode)
}

$projectIsPackableNode.InnerText = [bool]::TrueString

# Set "NuspecFile" property.
$projectNuspecFileNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/NuspecFile')
if ($projectNuspecFileNode -eq $null) {
    $projectNuspecFileNode = $projectDocument.CreateElement('NuspecFile')
    $projectPropertyGroupNode.AppendChild($projectNuspecFileNode)
}

$projectNuspecFileNode.InnerText = $nuspecDocumentFileName

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

# Save changes to project file.
$projectDocument.Save($projectDocumentFileName)

Write-Debug "Patched project $($projectDocumentFileName):"
Write-Debug (Get-Content $projectDocumentFileName -Raw)