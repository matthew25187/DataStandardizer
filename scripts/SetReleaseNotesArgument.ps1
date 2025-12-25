param (
    # Package name.
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    # Source commit hash.
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $SourceCommitHash,

    # Metadata for packages
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageInfos,

    # Path to source code root folder
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $SourceRootFolderPath,

    # Is build happening on the master branch?
    [Parameter()]
    [bool]  $IsBuildMasterBranch,

    # Release Note separation.
    [Parameter()]
    [bool]  $IsReleaseNoteSeparationRequired = $false,

    [Parameter()]
    [int]   $TraceLevel = 0
)

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Find the most recent tag for the package.
$packageMatchPattern = "v*$PackageName"
$tagsOutput = & git tag --list $packageMatchPattern --sort=-version:refname --no-contains $SourceCommitHash
# $isMasterBranch = "$(Build.SourceBranch)" -eq 'refs/heads/master'
$tagMatchPattern = $IsBuildMasterBranch ? "^v\d+\.\d+\.\d+-$PackageName$" : "^v\d+\.\d+\.\d+\.\d+-$PackageName$"
[string]$latestTagName = $tagsOutput -split '\r?\n' | Select-String -Pattern $tagMatchPattern -Raw | Select-Object -First 1 | Out-String -Stream

# Find the repository object preceding the source commit.
[string]$previousCommitObject
if (-not [string]::IsNullOrWhiteSpace($latestTagName)) {
    $previousCommitObject = "tags/$latestTagName"
}
else {
    $previousCommitObject = & git log --format="%H" --no-abbrev-commit | tail -1
}
$revisionListOutput = Invoke-Expression "git rev-list $previousCommitObject..$SourceCommitHash --no-commit-header --format=%s"

Write-Information "Using commit messages between $previousCommitObject and $SourceCommitHash."

# Compose the release notes.
[string[]]$sanitiseTags = @('[skip ci]', '[ci skip]', 'skip-checks: true', 'skip-checks:true', '[skip azurepipelines]', '[azurepipelines skip]', '[skip azpipelines]', '[azpipelines skip]', '[skip azp]', '[azp skip]', '***NO_CI***')
$sanitisePattern = [string]::Join('|', [System.Linq.Enumerable]::Select[string, string]($sanitiseTags, [System.Func[string, string]] { param($tag)[regex]::Escape($tag) }))

$releaseNotesBuilder = [System.Text.StringBuilder]::new()
$revisionListOutput -split '\r?\n' | Select-String -Pattern '#\d+' -Raw | Out-String -Stream | ForEach-Object {
    $sanitisedCommitMessage = $_ -replace $sanitisePattern, [string]::Empty
    if ($IsReleaseNoteSeparationRequired) {
        $ticketReference = $sanitisedCommitMessage | Select-String -Pattern '(#\d+.*?)(?=\s)' | Select-Object -ExpandProperty Matches | Select-Object -Property Value -First 1 | Format-Table -HideTableHeaders | Out-String -NoNewline
        $sanitisedCommitMessage -split ';' | ForEach-Object { 
            $releaseNote = $_.Trim()
            if (-not $releaseNote.EndsWith('.')) {
                $releaseNote += '.'
            }
            if ($releaseNote -notlike "$ticketReference*") {
                $releaseNote = ($ticketReference + ' ' + $releaseNote)
            }
            [void]$releaseNotesBuilder.AppendLine("*`t$releaseNote")
        }
    }
    else {
        [void]$releaseNotesBuilder.AppendLine("*`t$sanitisedCommitMessage")
    }
}
$releaseNotes = $releaseNotesBuilder.ToString().TrimEnd()
$releaseNotes

# Apply release notes to project file.
$packageInfo = $PackageInfos | ConvertFrom-Json | Where-Object -Property packageName -EQ -Value $PackageName
$projectRootPath = $SourceRootFolderPath | Join-Path -ChildPath $packageInfo.packageSourcePath
$projectFilePath = Get-ChildItem -Path $projectRootPath -Filter "$PackageName.csproj" -Recurse | Select-Object -First 1 -ExpandProperty FullName

$projectDocument = New-Object System.Xml.XmlDocument
$projectDocument.Load($projectFilePath)

# Try to find an existing PackageReleaseNotes node
$packageReleaseNotesNode = $projectDocument.SelectSingleNode('/Project/PropertyGroup/PackageReleaseNotes')

if ($null -eq $packageReleaseNotesNode) {
    # No PackageReleaseNotes node exists — we need to create one

    # Try to find an existing PropertyGroup
    $propertyGroup = $projectDocument.SelectSingleNode('/Project/PropertyGroup')

    if ($null -eq $propertyGroup) {
        # No PropertyGroup exists — create one
        $propertyGroup = $projectDocument.CreateElement('PropertyGroup')
        $projectDocument.DocumentElement.AppendChild($propertyGroup) | Out-Null
    }

    # Create the PackageReleaseNotes element
    $packageReleaseNotesNode = $projectDocument.CreateElement('PackageReleaseNotes')
    $propertyGroup.AppendChild($packageReleaseNotesNode) | Out-Null
}

# Normalize Unicode to ensure ASCII punctuation
$releaseNotes = $releaseNotes.Normalize([Text.NormalizationForm]::FormKC)

# At this point, the node definitely exists — set its value
$packageReleaseNotesNode.InnerText = '<![CDATA[' + $releaseNotes + ']]>'

# Save the updated project file
$projectDocument.Save($projectFilePath)

Write-Debug "Patched project file $(Split-Path $projectFilePath -Leaf) as follows:"
Write-Debug (Get-Content -Path $projectFilePath -Raw)