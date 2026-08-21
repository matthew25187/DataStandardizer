#Requires -Version 7.0

param (
    # Package name.
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    # Split an enumerated commit message paragraph into separate release notes on ';' boundaries.
    [Parameter()]
    [bool]  $IsReleaseNoteSeparationRequired = $false,

    # Where release notes are taken from: the commit subject, the commit body paragraphs, or both.
    [Parameter()]
    [ValidateSet('Subject', 'Body', 'Both')]
    [string]    $NoteSource = 'Both',

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

$revisionRange = "$previousCommitObject..$env:BUILD_SOURCEVERSION"

Write-Information "Using commit messages between $previousCommitObject and $env:BUILD_SOURCEVERSION scoped to $packageSourcePath."

# Patterns to scrub from commit messages before they become release notes.
[string[]]$sanitiseTags = @('[skip ci]', '[ci skip]', 'skip-checks: true', 'skip-checks:true', '[skip azurepipelines]', '[azurepipelines skip]', '[skip azpipelines]', '[azpipelines skip]', '[skip azp]', '[azp skip]', '***NO_CI***')
$sanitisePattern = [string]::Join('|', [System.Linq.Enumerable]::Select[string, string]($sanitiseTags, [System.Func[string, string]] { param($tag)[regex]::Escape($tag) }))

# Read the commits in the range as structured records. A commit body spans many lines, so a record
# separator delimits the commits and a unit separator the fields; both are control characters which
# cannot occur in a commit message, so bodies containing blank lines survive intact.
function Get-CommitRecord {
    param (
        [string]    $RevisionRange,
        [string]    $PackageSourcePath
    )

    $recordSeparator = [char]0x1e
    $unitSeparator = [char]0x1f
    $commitFormat = "%H$unitSeparator%s$unitSeparator%b$recordSeparator"
    $output = & git rev-list $RevisionRange --no-merges --reverse --no-commit-header --format=$commitFormat -- $PackageSourcePath

    # Git emits each record over several lines, which PowerShell surfaces as an array; rejoin them
    # before splitting so that the record separator is found regardless of the line boundaries.
    $joinedOutput = ($output -join "`n") -replace "`r", [string]::Empty
    foreach ($record in ($joinedOutput -split $recordSeparator)) {
        if ([string]::IsNullOrWhiteSpace($record)) {
            continue
        }

        $fields = $record.Trim("`n") -split $unitSeparator
        if ($fields.Count -lt 3) {
            Write-Warning "Skipped a commit record which could not be read."
            continue
        }

        [PSCustomObject]@{
            CommitId = $fields[0].Trim()
            Subject  = $fields[1].Trim()
            Body     = $fields[2]
        }
    }
}

# Recognise a git trailer line, such as 'Co-Authored-By:' or 'Signed-off-by:', or an Azure Boards
# work item reference. Matching the form rather than a fixed list of names covers any trailer.
function Test-TrailerLine {
    param (
        [string]    $Line
    )

    return ($Line -match '^[A-Za-z][A-Za-z-]*:\s') -or ($Line -match '^AB#\d+\b')
}

# Read the values of any breaking change trailer. These are stated by the author rather than
# inferred, so they are the most reliable signal a release note can carry.
function Get-BreakingChangeTrailer {
    param (
        [string]    $Body
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return
    }

    foreach ($line in (($Body -replace "`r", [string]::Empty) -split "`n")) {
        $trailerMatch = [regex]::Match($line.Trim(), '^BREAKING[- ]CHANGE:\s*(?<value>.+)$')
        if ($trailerMatch.Success) {
            $trailerMatch.Groups['value'].Value.Trim()
        }
    }
}

# A list item marker: a bullet or a number. Held in one place because both recognising the line
# which opens an item and stripping the marker from it must agree on what a marker is.
$script:listMarkerPattern = '^\s*(?:[-*+]\s+|\d+[.)]\s+)'

# A squash merge separates the squashed commits with a rule of hyphens, which is not prose.
$script:squashRulePattern = '^\s*-{3,}\s*$'

# Read a body into blocks, each a run of non-blank lines, keeping the indentation of every line.
# Indentation is what distinguishes a subordinate list item from a top level one, so unlike the
# rest of the parsing this cannot trim first and decide afterwards.
function Get-BodyBlock {
    param (
        [string]    $Body
    )

    foreach ($block in (($Body -replace "`r", [string]::Empty) -split "`n[ `t]*`n")) {
        $lines = @(
            $block -split "`n" |
                Where-Object { -not [string]::IsNullOrWhiteSpace($_) } |
                Where-Object { $_ -notmatch $script:squashRulePattern } |
                ForEach-Object { $_ -replace '\s+$', [string]::Empty }
        )
        if (0 -lt $lines.Count) {
            , $lines
        }
    }
}

# The indentation of a line, in columns, with a tab counted as four. Only the relative depth of one
# item against another matters, so the exact width is unimportant provided it is applied uniformly.
function Get-LineIndent {
    param (
        [string]    $Line
    )

    $leading = [regex]::Match($Line, '^[ \t]*').Value
    return ($leading -replace "`t", '    ').Length
}

# Split a block into its list items, each rejoined from the lines which wrap it, and each carrying
# the indent of the line which opened it. A new item begins only at a marker line; every other line
# continues the item currently open, which is what keeps a hard-wrapped item a single note.
function Get-ListItem {
    param (
        [string[]]  $Lines
    )

    $text = [System.Text.StringBuilder]::new()
    $indent = 0

    foreach ($line in $Lines) {
        if ($line -match $script:listMarkerPattern) {
            if (0 -lt $text.Length) {
                [PSCustomObject]@{ Indent = $indent; Text = $text.ToString().Trim() }
            }
            [void]$text.Clear()
            $indent = Get-LineIndent -Line $line
            [void]$text.Append(($line -replace $script:listMarkerPattern, [string]::Empty).Trim())
            continue
        }

        if (0 -lt $text.Length) {
            [void]$text.Append(' ')
        }
        [void]$text.Append($line.Trim())
    }

    if (0 -lt $text.Length) {
        [PSCustomObject]@{ Indent = $indent; Text = $text.ToString().Trim() }
    }
}

# Keep only the outermost items of a list. A more deeply indented item qualifies the item above it,
# in the same way a list qualifies the paragraph above it, and a qualification is detail rather than
# a change: it belongs in the commit history, not in a package listing.
function Select-TopLevelItem {
    param (
        [object[]]  $Items
    )

    if ($null -eq $Items -or 0 -eq $Items.Count) {
        return
    }

    $outermost = ($Items | Measure-Object -Property Indent -Minimum).Minimum
    foreach ($item in $Items) {
        if ($item.Indent -eq $outermost) {
            $item.Text
        }
    }
}

# Verbs which open a sentence announcing a change in its own right.
$script:announcingVerbs = 'Add|Remove|Fix|Cache|Consolidate|Rename|Deprecate|Replace|Drop|Introduce|Correct|Restore|Gate|Route|Report|Implement|Declare|Document|Cover|Confirm|Update|Extend|Expose|Enable|Disable|Move|Split|Merge|Delete'

# Decide whether a paragraph introducing a list is substantive prose or merely a title for it.
# A substantive lead states the change itself and the list below it only qualifies that statement,
# so the lead is published and the list dropped. A title carries no statement of its own, so the
# list carries the content instead. The test is whether the lead reads as a sentence: a finite verb
# and enough length to say something. A heading such as the bold one a generated commit opens with
# has neither, whereas 'Each directive now states that file's actual minimum, ...' has both.
$script:finiteVerbPattern = '\b(?:is|are|was|were|be|been|being|has|have|had|does|do|did|can|could|will|would|shall|should|may|might|must|states|stated|becomes|became|returns|returned|carries|carried|uses|used|takes|took|makes|made|gives|gave|keeps|kept|leaves|left|remains|remained|ensures|ensured|avoids|avoided|produces|produced|requires|required|allows|allowed|prevents|prevented|reads|holds|held|spans|covers|adds|added)\b'
$script:leadSentenceMinimumLength = 60

function Test-SubstantiveLead {
    param (
        [string]    $Paragraph
    )

    if ([string]::IsNullOrWhiteSpace($Paragraph)) {
        return $false
    }

    # A heading emphasised in its entirety is a title however it is worded.
    if ($Paragraph -match '^\*\*[^*]+\*\*[\p{P}]*$') {
        return $false
    }

    # A lead which opens with a verb announcing a change states that change itself, however
    # briefly, so it is substantive whatever its length: 'Extend UnM49Extensions to cover the
    # new enumeration:' says what changed just as 'Each directive now states ...' does.
    if ($Paragraph -cmatch "^(?:$($script:announcingVerbs))\b") {
        return $true
    }

    if ($Paragraph.Length -lt $script:leadSentenceMinimumLength) {
        return $false
    }

    return ($Paragraph -match $script:finiteVerbPattern)
}

# Restore hard-wrapped paragraphs to single logical lines and resolve the relationship between a
# paragraph and the list which follows it. Paragraphs are separated by blank lines and trailer
# blocks are dropped. A list elaborates on the paragraph above it, so exactly one of the two is
# published: the paragraph where it states the change itself, otherwise the list items.
function Expand-WrappedParagraph {
    param (
        [string]    $Body
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return
    }

    $blocks = @(Get-BodyBlock -Body $Body)
    $pendingLead = $null

    foreach ($lines in $blocks) {
        # A block whose every line is a trailer is metadata rather than prose. Testing the whole
        # block leaves prose which merely opens 'Note: ...' alone, because such a block also
        # contains lines which are not trailers.
        $trailerLines = @($lines | Where-Object { Test-TrailerLine -Line $_.Trim() })
        if ($trailerLines.Count -eq $lines.Count) {
            continue
        }

        if ($lines[0] -match $script:listMarkerPattern) {
            $items = @(Get-ListItem -Lines $lines)

            # A squash merge opens each squashed commit with its subject as a lone bullet, which
            # the paragraphs following it then describe in full. It is a title for what follows,
            # so it is held as a pending lead rather than published in its own right.
            if (1 -eq $items.Count -and $null -eq $pendingLead) {
                $pendingLead = $items[0].Text
                continue
            }

            # The list qualifies the paragraph above it. Where that paragraph stated the change,
            # it is the note and the list is detail; otherwise the paragraph was only a title and
            # the list carries the content.
            if ($null -ne $pendingLead -and (Test-SubstantiveLead -Paragraph $pendingLead)) {
                $pendingLead
                $pendingLead = $null
                continue
            }

            $pendingLead = $null
            Select-TopLevelItem -Items $items
            continue
        }

        # A paragraph is held back until the following block is known, because whether it is
        # published depends on whether a list follows it and on what that paragraph says.
        if ($null -ne $pendingLead) {
            $pendingLead
        }
        $pendingLead = ((($lines | ForEach-Object { $_.Trim() }) -join ' ') -replace '\s{2,}', ' ').Trim()
    }

    if ($null -ne $pendingLead) {
        $pendingLead
    }
}

# Sentence boundaries in this repository's prose. A boundary is terminating punctuation followed by
# whitespace and a capital: requiring the whitespace leaves 'MoneyFormatter.Format', '.Net's' and
# 'netstandard1.0' whole, requiring the capital leaves '2.5' and 'U+2212' whole, and the look-behinds
# exclude common abbreviations and single letter initials.
[string[]]$script:sentenceAbbreviations = @('e.g', 'i.e', 'etc', 'vs', 'cf', 'al', 'Mr', 'Mrs', 'Ms', 'Dr', 'St', 'approx', 'No', 'Inc', 'Ltd')
$script:sentenceGuard = [string]::Join('|', [System.Linq.Enumerable]::Select[string, string]($script:sentenceAbbreviations, [System.Func[string, string]] { param($abbreviation)[regex]::Escape($abbreviation) }))
$script:sentenceOpeningChars = [regex]::Escape('"' + "'" + '(' + [char]0x2018 + [char]0x201C)
$script:sentenceBoundaryPattern = '(?<!\b(?:' + $script:sentenceGuard + '))(?<![A-Z])([.!?])\s+(?=[' + $script:sentenceOpeningChars + ']?[A-Z])'

# Sentences which describe the commit rather than the software. These read as an aside to a reviewer
# reading the history and as noise to somebody reading a package listing, so they are not published.
# Matching this narrow form rather than requiring an announcing verb keeps the many legitimate notes
# which open with neither a verb nor a subject, such as 'Behaviour is unchanged.'
$script:commentaryPattern = '^(?:\w+\s+){0,3}(?:tests?|changes?|commits?|paragraphs?|notes?)\s+(?:is|are|was|were)\s+worth\b|\bworth\s+calling\s+out\b|\bis\s+worth\s+(?:noting|mentioning)\b|^Th(?:is|ese)\s+(?:commit|change|update|edit|revision)s?\b'

# Split a paragraph into its sentences.
function Split-Sentence {
    param (
        [string]    $Paragraph
    )

    [regex]::Split($Paragraph, $script:sentenceBoundaryPattern) |
        Where-Object { $_ -notmatch '^[.!?]$' -and -not [string]::IsNullOrWhiteSpace($_) } |
        ForEach-Object { $_.Trim() }
}

# Take the lead sentence of a paragraph, which by this repository's convention states the change.
# The sentence is taken whole: an explanatory clause usually carries the substance of the change,
# so cutting at one produced notes such as 'Implement MoneyFormatter.' which say nothing.
function Split-LeadSentence {
    param (
        [string]    $Paragraph
    )

    $sentences = @(Split-Sentence -Paragraph $Paragraph)
    if ($sentences.Count -eq 0) {
        return $null
    }

    return $sentences[0]
}

# Recover any further sentence of a paragraph which announces a change in its own right. Without
# this a removal stated after the lead sentence is lost, which is precisely the kind of note a
# package consumer must not miss.
function Select-AnnouncingSentence {
    param (
        [string]    $Paragraph
    )

    $sentences = @(Split-Sentence -Paragraph $Paragraph)
    for ($sentenceIndex = 1; $sentenceIndex -lt $sentences.Count; $sentenceIndex++) {
        if ($sentences[$sentenceIndex] -match "^(?:$script:announcingVerbs)\b") {
            $sentences[$sentenceIndex]
        }
    }
}

# A paragraph which introduces a list with a colon and separates its items with semicolons carries
# several changes. Reduced to its lead where separation was not asked for, so that the note stays
# short, and expanded to one note per item where it was. Returns $null when not such a paragraph.
function Split-EnumeratedParagraph {
    param (
        [string]    $Paragraph,
        [bool]      $IsSeparationRequired
    )

    $enumerationMatch = [regex]::Match($Paragraph, '^(?<lead>[^:;]{10,}?):\s+(?<tail>.+)$')
    if (-not $enumerationMatch.Success) {
        return $null
    }

    $tail = $enumerationMatch.Groups['tail'].Value
    if (0 -eq ([regex]::Matches($tail, ';')).Count) {
        return $null
    }

    $notes = [System.Collections.Generic.List[string]]::new()
    $notes.Add($enumerationMatch.Groups['lead'].Value.Trim())
    if ($IsSeparationRequired) {
        foreach ($item in ($tail -split ';')) {
            $itemText = ($item -replace '^\s*(?:and|or)\s+', [string]::Empty).Trim()
            if (-not [string]::IsNullOrWhiteSpace($itemText)) {
                $notes.Add($itemText)
            }
        }
    }

    return , $notes.ToArray()
}

# Tidy a raw commit subject into a release note: drop CI directives and ticket/PR references,
# normalise whitespace, and ensure terminating punctuation. Returns $null when nothing remains.
function Format-ReleaseNote {
    param (
        [string]    $Message,
        [string]    $SanitisePattern
    )

    # A fragment left by rewrapping carries no information worth publishing.
    if ($Message -notmatch '\w') {
        return $null
    }

    # Commentary on the commit itself is not a release note.
    if ($Message -match $script:commentaryPattern) {
        return $null
    }

    $note = $Message -replace $SanitisePattern, [string]::Empty
    # Remove an Azure Boards work item reference (e.g. "AB#4374") whole, before the bare reference
    # removal below can reduce it to a stray "AB".
    $note = $note -replace '\bAB#\d+\b', [string]::Empty
    # Remove a trailing parenthesised PR reference (e.g. " (#87)") added by squash merges.
    $note = $note -replace '\s*\(#\d+\)', [string]::Empty
    # Remove any remaining bare ticket/PR references (e.g. "#123").
    $note = $note -replace '#\d+', [string]::Empty
    # Drop punctuation left stranded at the start once a leading reference has been removed.
    $note = $note -replace '^\s*[:;,.\-]+\s*', [string]::Empty
    # Collapse whitespace left behind by the removals.
    $note = ($note -replace '\s{2,}', ' ').Trim()

    # Nothing but punctuation may remain once the references have been removed.
    if ($note -notmatch '\w') {
        return $null
    }
    # A note which introduced a list is published without that list, the items having been dropped
    # as detail, so a colon or a semicolon left at the end now introduces nothing. Replace it with
    # a full stop rather than appending one, which would read as a mistake.
    $note = $note -replace '\s*[:;]$', '.'

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

# Compose the release notes. The note count and total length are bounded so that a long accumulation
# cannot overrun the 32,766 character limit which applies to the encoded pipeline variable on
# Windows; that limit, not the larger .nuspec one, is the binding constraint.
$noteLimit = 60
$totalLengthLimit = 8000

$releaseNotesBuilder = [System.Text.StringBuilder]::new()
$seenNotes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$noteCount = 0
$droppedCount = 0

# Breaking changes are collected across every commit and published first, so that they are read
# before the notes describing ordinary changes.
$noteTexts = [System.Collections.Generic.List[string]]::new()
$breakingNoteTexts = [System.Collections.Generic.List[string]]::new()

foreach ($commit in (Get-CommitRecord -RevisionRange $revisionRange -PackageSourcePath $packageSourcePath)) {
    foreach ($breakingChange in (Get-BreakingChangeTrailer -Body $commit.Body)) {
        $breakingNoteTexts.Add("BREAKING: $breakingChange")
    }

    if ($NoteSource -in @('Subject', 'Both')) {
        $noteTexts.Add($commit.Subject)
    }

    if ($NoteSource -in @('Body', 'Both')) {
        foreach ($paragraph in (Expand-WrappedParagraph -Body $commit.Body)) {
            $enumeratedNotes = Split-EnumeratedParagraph -Paragraph $paragraph -IsSeparationRequired $IsReleaseNoteSeparationRequired
            if ($null -ne $enumeratedNotes) {
                $noteTexts.AddRange([string[]]$enumeratedNotes)
                continue
            }

            $leadSentence = Split-LeadSentence -Paragraph $paragraph
            if (-not [string]::IsNullOrWhiteSpace($leadSentence)) {
                $noteTexts.Add($leadSentence)
            }

            foreach ($announcingSentence in (Select-AnnouncingSentence -Paragraph $paragraph)) {
                $noteTexts.Add($announcingSentence)
            }
        }
    }
}

foreach ($noteText in ($breakingNoteTexts + $noteTexts)) {
    $releaseNote = Format-ReleaseNote -Message $noteText -SanitisePattern $sanitisePattern
    if ($null -eq $releaseNote) {
        continue
    }

    # The same paragraph recurs where a change is amended or cherry-picked; publish it once.
    if (-not $seenNotes.Add($releaseNote)) {
        continue
    }

    if ($noteCount -ge $noteLimit -or ($releaseNotesBuilder.Length + $releaseNote.Length + 2) -gt $totalLengthLimit) {
        $droppedCount++
        continue
    }

    # A hyphen and a space so that the notes render as a list wherever they are read as Markdown.
    [void]$releaseNotesBuilder.AppendLine("- $releaseNote")
    $noteCount++
}

if (0 -lt $droppedCount) {
    Write-Warning "Kept $noteCount release notes for $PackageName; $droppedCount were dropped by the note count limit of $noteLimit or the length limit of $totalLengthLimit characters."
}

Write-Verbose "Composed $noteCount release notes for $PackageName."

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
