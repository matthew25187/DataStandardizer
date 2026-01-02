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

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force

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

# Output release notes to pipeline variable.
$encodedReleaseNotes = $releaseNotes | ConvertTo-Base64String
Write-Host "##vso[task.setvariable variable=releaseNotesArg]$encodedReleaseNotes"