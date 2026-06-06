#Requires -Version 7.0

param (
    # Package name.
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    # Split a single commit message into separate release notes on ';' boundaries.
    [Parameter()]
    [bool]  $IsReleaseNoteSeparationRequired = $false,

    [Parameter()]
    [int]   $TraceLevel = 0
)

$modulePath = Join-Path $PSScriptRoot "../psmodules/CommonHelpers/CommonHelpers.psm1"
Import-Module $modulePath -Force -Scope Local

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Resolve the package's source folder so release notes can be scoped to commits that
# actually touched this package (rather than every change across the repository).
$packageInfo = $env:PACKAGEINFOS | ConvertFrom-Json | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "No package information found for $PackageName."
    exit
}
$packageSourcePath = $packageInfo.packageSourcePath

# Find the most recent tag for the package that is an ancestor of the build commit.
# `--merged` (not `--no-contains`) restricts the candidates to tags reachable from the
# build commit, so the chosen baseline is genuinely the previous tag on this history.
$escapedPackageName = [regex]::Escape($PackageName)
$packageMatchPattern = "v*$PackageName"
$tagsOutput = & git tag --list $packageMatchPattern --sort=-version:refname --merged $env:BUILD_SOURCEVERSION
# On master the package is promoted to a 3-part release version, so notes accumulate from
# the previous release tag; otherwise (pre-release) they accumulate from the previous 4-part tag.
$tagMatchPattern = ($env:BUILD_SOURCEBRANCH -eq 'refs/heads/master') ? "^v\d+\.\d+\.\d+-$escapedPackageName$" : "^v\d+\.\d+\.\d+\.\d+-$escapedPackageName$"
[string]$latestTagName = $tagsOutput -split '\r?\n' | Select-String -Pattern $tagMatchPattern -Raw | Select-Object -First 1

# Find the repository object preceding the source commit.
[string]$previousCommitObject
if (-not [string]::IsNullOrWhiteSpace($latestTagName)) {
    $previousCommitObject = "tags/$($latestTagName.Trim())"
}
else {
    $previousCommitObject = & git log --format="%H" --no-abbrev-commit | Select-Object -Last 1
}

# Collect the commit subjects between the baseline and the build commit, excluding merge
# commits and scoping to the package's source folder.
$revisionRange = "$previousCommitObject..$env:BUILD_SOURCEVERSION"
$revisionListOutput = & git rev-list $revisionRange --no-merges --reverse --no-commit-header --format=%s -- $packageSourcePath

Write-Information "Using commit messages between $previousCommitObject and $env:BUILD_SOURCEVERSION scoped to $packageSourcePath."

# Patterns to scrub from commit messages before they become release notes.
[string[]]$sanitiseTags = @('[skip ci]', '[ci skip]', 'skip-checks: true', 'skip-checks:true', '[skip azurepipelines]', '[azurepipelines skip]', '[skip azpipelines]', '[azpipelines skip]', '[skip azp]', '[azp skip]', '***NO_CI***')
$sanitisePattern = [string]::Join('|', [System.Linq.Enumerable]::Select[string, string]($sanitiseTags, [System.Func[string, string]] { param($tag)[regex]::Escape($tag) }))

# Tidy a raw commit subject into a release note: drop CI directives and ticket/PR references,
# normalise whitespace, and ensure terminating punctuation. Returns $null when nothing remains.
function Format-ReleaseNote {
    param (
        [string]    $Message,
        [string]    $SanitisePattern
    )

    $note = $Message -replace $SanitisePattern, [string]::Empty
    # Remove a trailing parenthesised PR reference (e.g. " (#87)") added by squash merges.
    $note = $note -replace '\s*\(#\d+\)', [string]::Empty
    # Remove any remaining bare ticket/PR references (e.g. "#123").
    $note = $note -replace '#\d+', [string]::Empty
    # Collapse whitespace left behind by the removals.
    $note = ($note -replace '\s{2,}', ' ').Trim()

    if ([string]::IsNullOrWhiteSpace($note)) {
        return $null
    }
    # Append a full stop unless the note already ends in terminating punctuation
    # (full stop, exclamation/question mark, or an ellipsis), ignoring any trailing
    # closing quote (straight or curly) or bracket.
    $closingChars = [char[]]@('"', "'", ')', ']', [char]0x2019, [char]0x201D)
    $terminators = [char[]]@('.', '!', '?', [char]0x2026)
    $trimmed = $note.TrimEnd($closingChars)
    if ($trimmed.Length -eq 0 -or $terminators -notcontains $trimmed[-1]) {
        $note += '.'
    }
    return $note
}

# Compose the release notes.
$releaseNotesBuilder = [System.Text.StringBuilder]::new()
$revisionListOutput -split '\r?\n' |
    Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
    ForEach-Object {
        $commitMessage = $_
        $noteSources = $IsReleaseNoteSeparationRequired ? ($commitMessage -split ';') : @($commitMessage)
        foreach ($noteSource in $noteSources) {
            $releaseNote = Format-ReleaseNote -Message $noteSource -SanitisePattern $sanitisePattern
            if ($null -ne $releaseNote) {
                [void]$releaseNotesBuilder.AppendLine("*`t$releaseNote")
            }
        }
    }
$releaseNotes = $releaseNotesBuilder.ToString().TrimEnd()
$releaseNotes

# Output release notes to pipeline variable. When no relevant notes were produced, clear the
# variable so UpdatePackageMetadata removes any stale releaseNotes element from the .nuspec.
if ([string]::IsNullOrWhiteSpace($releaseNotes)) {
    Write-Information "No relevant commit messages found for $PackageName; release notes will be cleared."
    Write-Host "##vso[task.setvariable variable=releaseNotesArg]"
}
else {
    $encodedReleaseNotes = $releaseNotes | ConvertTo-Base64String
    Write-Host "##vso[task.setvariable variable=releaseNotesArg]$encodedReleaseNotes"
}
