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
    # Every change reaches develop as a squash merge whose subject is the pull request title, a
    # one-line summary of the change, so the subject alone is the note a package consumer needs.
    # The body explains the change to a reviewer and is far more detail than a package listing wants.
    [Parameter()]
    [ValidateSet('Subject', 'Body', 'Both')]
    [string]    $NoteSource = 'Subject',

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
$packageInfo = Get-PackageInfo | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "No package information found for $PackageName."
    exit
}
$packageSourcePath = $packageInfo.packageSourcePath

# Find the most recent tag for the package that is an ancestor of the build commit.
# `--merged` (not `--no-contains`) restricts the candidates to tags reachable from the
# build commit, so the chosen baseline is genuinely the previous tag on this history.
$buildCommit = Get-BuildCommit
$escapedPackageName = [regex]::Escape($PackageName)
$packageMatchPattern = "v*$PackageName"
$tagsOutput = & git tag --list $packageMatchPattern --sort=-version:refname --merged $buildCommit
# On master the package is promoted to a 3-part release version, so notes accumulate from
# the previous release tag; otherwise (pre-release) they accumulate from the previous 4-part tag.
$tagMatchPattern = (Test-ReleaseBuild) ? "^v\d+\.\d+\.\d+-$escapedPackageName$" : "^v\d+\.\d+\.\d+\.\d+-$escapedPackageName$"
[string]$latestTagName = $tagsOutput -split '\r?\n' | Select-String -Pattern $tagMatchPattern -Raw | Select-Object -First 1

# Find the repository object preceding the source commit.
[string]$previousCommitObject
if (-not [string]::IsNullOrWhiteSpace($latestTagName)) {
    $previousCommitObject = "tags/$($latestTagName.Trim())"
}
else {
    $previousCommitObject = & git log --format="%H" --no-abbrev-commit | Select-Object -Last 1
}

$revisionRange = "$previousCommitObject..$buildCommit"

Write-Information "Using commit messages between $previousCommitObject and $buildCommit scoped to $packageSourcePath."

# Patterns to scrub from commit messages before they become release notes.
[string[]]$sanitiseTags = @('[skip ci]', '[ci skip]', 'skip-checks: true', 'skip-checks:true', '[skip azurepipelines]', '[azurepipelines skip]', '[skip azpipelines]', '[azpipelines skip]', '[skip azp]', '[azp skip]', '***NO_CI***', '[no ci]', '[skip actions]', '[actions skip]')
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

# A GitHub closing keyword followed only by the issues it closes, such as 'Closes #123' or
# 'Fixes #12, #13'. It links the pull request to its issues and says nothing about the change, but
# once its references are removed the keyword alone is left, which was published as 'Closes.'.
$script:closingKeywordPattern = '(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)'
$script:issueReferencePattern = '(?:[\w.-]+/[\w.-]+)?#\d+'
$script:closingLinePattern = "(?i)^$($script:closingKeywordPattern):?\s+$($script:issueReferencePattern)(?:\s*(?:,|\band\b)\s*(?:$($script:closingKeywordPattern):?\s+)?$($script:issueReferencePattern))*\s*[.]?$"

# Recognise a git trailer line, such as 'Co-Authored-By:' or 'Signed-off-by:', an Azure Boards
# work item reference, or a GitHub closing keyword. Matching the form rather than a fixed list of
# names covers any trailer.
function Test-TrailerLine {
    param (
        [string]    $Line
    )

    # 'BREAKING CHANGE' is the one trailer token which may contain a space.
    return ($Line -match '^[A-Za-z][A-Za-z-]*:\s') -or ($Line -match '^BREAKING CHANGE:\s') -or ($Line -match '^AB#\d+\b') -or ($Line -match $script:closingLinePattern)
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

# A tracker reference: an Azure Boards work item such as 'AB#4374', or a ticket or pull request
# such as '#123'. Held in one place because recognising one and deciding where it may be removed
# from must agree on what a reference is.
$script:trackerReferencePattern = '(?:\bAB)?#\d+'

# The characters which quote prose in this repository's commit messages: straight and curly quotes,
# and the backtick which sets code and file names apart.
$script:quotingChars = [char[]]@('"', "'", '`', [char]0x2018, [char]0x2019, [char]0x201C, [char]0x201D)

# The character which closes a run each quoting character opens. Curly quotes close with their
# counterpart; every other quoting character closes with itself.
$script:closingQuoteChars = @{
    ([char]0x2018) = [char]0x2019
    ([char]0x201C) = [char]0x201D
}

# The characters which also serve as an apostrophe, and are one rather than a quote where they
# stand between two letters or digits, as in "VTIMEZONE's" or "doesn't".
$script:apostropheChars = [char[]]@("'", [char]0x2019)

# Test whether the character at an index of a note is a quoting character, rather than another
# character or an apostrophe within a word.
function Test-QuotingCharacter {
    param (
        [string]    $Note,
        [int]       $Index
    )

    $character = $Note[$Index]
    if ($script:quotingChars -notcontains $character) {
        return $false
    }

    if ($script:apostropheChars -contains $character -and
        $Index -gt 0 -and $Index -lt ($Note.Length - 1) -and
        [char]::IsLetterOrDigit($Note[$Index - 1]) -and [char]::IsLetterOrDigit($Note[$Index + 1])) {
        return $false
    }

    return $true
}

# Test whether the character at an index falls inside a quoted or backticked run: after a quoting
# character that is still open, and before the character that closes it. This is what
# distinguishes a reference the prose is discussing from one a tool appended. Quotes of another
# kind inside an open run don't close it, and a quote that is never closed quotes nothing.
function Test-QuotedPosition {
    param (
        [string]    $Note,
        [int]       $Index
    )

    $closingChar = $null
    for ($characterIndex = 0; $characterIndex -lt $Index; $characterIndex++) {
        if (-not (Test-QuotingCharacter -Note $Note -Index $characterIndex)) {
            continue
        }

        $character = $Note[$characterIndex]
        if ($null -eq $closingChar) {
            $closingChar = $script:closingQuoteChars.ContainsKey($character) ? $script:closingQuoteChars[$character] : $character
        }
        elseif ($character -ceq $closingChar) {
            $closingChar = $null
        }
    }

    if ($null -eq $closingChar) {
        return $false
    }

    for ($characterIndex = $Index; $characterIndex -lt $Note.Length; $characterIndex++) {
        if ($Note[$characterIndex] -ceq $closingChar -and (Test-QuotingCharacter -Note $Note -Index $characterIndex)) {
            return $true
        }
    }

    return $false
}

# Remove the tracker references a tool appended to a commit subject, leaving those the prose is
# about. A reference is removed only where it stands apart from the sentence rather than within it:
# parenthesised, as a squash merge writes it, or trailing the note. A reference inside quotes or
# backticks is the subject matter and is always kept, as is one embedded mid-sentence, whose
# removal would leave the sentence ungrammatical.
function Remove-TrackerReference {
    param (
        [string]    $Note
    )

    if ([string]::IsNullOrWhiteSpace($Note)) {
        return $Note
    }

    # A parenthesised reference, or list of references such as '(#263, #280)', is punctuation around
    # metadata wherever it sits, provided the parenthesis itself is not quoted.
    $parenthesised = "\s*\(\s*$script:trackerReferencePattern(?:\s*,\s*$script:trackerReferencePattern)*\s*\)"
    foreach ($match in ([regex]::Matches($Note, $parenthesised) | Sort-Object -Property Index -Descending)) {
        if (-not (Test-QuotedPosition -Note $Note -Index $match.Index)) {
            $Note = $Note.Remove($match.Index, $match.Length)
        }
    }

    # A bare reference is metadata only where it trails the prose, allowing for the punctuation a
    # sentence may end with. Anything after it means it is part of the sentence rather than an
    # addition to it.
    $trailing = [regex]::Match($Note, "\s*$script:trackerReferencePattern\s*[.!?:;,]*\s*$")
    if ($trailing.Success -and -not (Test-QuotedPosition -Note $Note -Index $trailing.Index)) {
        # Keep any terminating punctuation which followed the reference, so that a note reading
        # 'Fix the parser #123.' is left as a properly terminated sentence.
        $punctuation = ($trailing.Value -replace '[^.!?]', [string]::Empty)
        $Note = $Note.Remove($trailing.Index) + $punctuation
    }

    return $Note
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
    # Remove the tracker references a tool appended, but not one the prose is itself discussing.
    # A reference is metadata where it stands apart from the sentence: trailing, or parenthesised.
    # Where it is quoted or set in backticks it is the subject matter, as in a note reporting that
    # 'AB#4374' was reduced to a stray 'AB', and removing it empties the quotation it explains.
    # Removing one from mid-sentence is likewise wrong, leaving 'Correct handling of reported by
    # the team', so a reference is taken only from the end of the note where it trails the prose.
    $note = Remove-TrackerReference -Note $note
    # Drop punctuation left stranded at the start once a leading reference has been removed.
    $note = $note -replace '^\s*[:;,.\-]+\s*', [string]::Empty
    # Collapse whitespace left behind by the removals.
    $note = ($note -replace '\s{2,}', ' ').Trim()

    # Nothing but punctuation may remain once the references have been removed, or nothing but the
    # closing keyword which introduced them.
    if ($note -notmatch '\w' -or $note -match "(?i)^$($script:closingKeywordPattern)\W*$") {
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

# The sections a release note is published under, in the order a consumer needs to read them: what
# may break their code first, then what they can now use, then what was wrong and is now right.
$script:categoryHeadings = [ordered]@{
    Breaking   = 'Breaking changes'
    Added      = 'New'
    Fixed      = 'Fixed'
    Deprecated = 'Deprecated and removed'
    Changed    = 'Changed'
}

# The categories a 'Release-note:' trailer or an override may declare, mapped to the section each
# is published under. 'none' keeps a commit out of the notes altogether, which is how a change that
# touches the package's folder without affecting its consumers, such as a build or test change, is
# left out.
$script:declaredCategories = @{
    none       = 'None'
    breaking   = 'Breaking'
    added      = 'Added'
    new        = 'Added'
    fixed      = 'Fixed'
    deprecated = 'Deprecated'
    removed    = 'Deprecated'
    changed    = 'Changed'
}

# The verbs a subject opens with when it states a change of each kind. The test is deliberately
# narrow: a subject opening with any other verb is published as 'Changed', which is never wrong,
# whereas a wide test would publish some changes under a heading which misdescribes them. A change
# that this does not place correctly, such as a fix whose subject says 'Keep' or 'Throw', declares
# its category with a 'Release-note:' trailer instead.
$script:subjectCategoryPatterns = [ordered]@{
    Fixed      = '^(?:Fix|Fixes|Fixed|Correct|Corrects|Corrected)\b'
    Deprecated = '^(?:Deprecate|Deprecates|Deprecated|Remove|Removes|Removed|Drop|Drops|Dropped)\b'
    Added      = '^(?:Add|Adds|Added|Support|Supports|Introduce|Introduces|Implement|Implements|Expose|Exposes)\b'
}

# The types of a Conventional Commits subject, such as 'feat: ...', mapped to the category each
# one declares. The repository's early history used this form.
$script:conventionalCategories = @{
    feat = 'Added'
    fix  = 'Fixed'
}
$script:conventionalPrefixPattern = '^(?<type>[a-z]+)(?:\([^)]*\))?(?<breaking>!)?:\s+'

# A declared release note: the category it is published under and, where the author wrote one, its
# text. Without text the commit subject is the note.
function New-DeclaredNote {
    param (
        [string]    $Category,
        [string]    $Text
    )

    [PSCustomObject]@{
        Category = $Category
        Text     = [string]::IsNullOrWhiteSpace($Text) ? $null : $Text.Trim()
    }
}

# Read the release notes the author of a change declared with 'Release-note:' trailers. Each trailer
# is one note, written as 'Release-note: <category>' to publish the subject under that category, or
# 'Release-note: <category>: <text>' to publish the text instead, so a pull request which fixes two
# problems can say so in words a consumer recognises. The trailers belong in a commit message rather
# than the pull request description, because the squash merge builds its body from the former.
# Returns nothing when the commit declares nothing, so that its notes are inferred instead.
function Get-ReleaseNoteTrailer {
    param (
        [string]    $Subject,
        [string]    $Body
    )

    if ([string]::IsNullOrWhiteSpace($Body)) {
        return
    }

    foreach ($line in (($Body -replace "`r", [string]::Empty) -split "`n")) {
        $trailerMatch = [regex]::Match($line.Trim(), '^(?i)Release-note:\s*(?<category>[A-Za-z]+)\s*(?::\s*(?<text>\S.*))?$')
        if (-not $trailerMatch.Success) {
            continue
        }

        $declared = $trailerMatch.Groups['category'].Value.ToLowerInvariant()
        if (-not $script:declaredCategories.ContainsKey($declared)) {
            Write-Warning "Ignored the unrecognised release note category '$declared' declared on '$Subject'."
            continue
        }

        New-DeclaredNote -Category $script:declaredCategories[$declared] -Text $trailerMatch.Groups['text'].Value
    }
}

# Read the release notes declared for commits which were merged before they could declare them
# themselves. A commit's message cannot be changed once it is on develop, so its notes are stated in
# this file instead, keyed by the full commit id; an empty list of notes leaves the commit out.
function Get-ReleaseNoteOverride {
    param (
        [string]    $Path
    )

    $overrides = @{}
    if (-not (Test-Path -LiteralPath $Path)) {
        return $overrides
    }

    $document = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -AsHashtable
    foreach ($commitId in $document['commits'].Keys) {
        if ($commitId -notmatch '^[0-9a-f]{40}$') {
            Write-Error "Release note override key '$commitId' is not a full commit id."
            exit 1
        }

        $notes = [System.Collections.Generic.List[object]]::new()
        foreach ($entry in $document['commits'][$commitId]['notes']) {
            $declared = ([string]$entry['category']).ToLowerInvariant()
            if (-not $script:declaredCategories.ContainsKey($declared)) {
                Write-Error "Release note override for $commitId has the unrecognised category '$declared'."
                exit 1
            }
            $notes.Add((New-DeclaredNote -Category $script:declaredCategories[$declared] -Text $entry['text']))
        }

        $overrides[$commitId] = [PSCustomObject]@{
            Subject = [string]$document['commits'][$commitId]['subject']
            Notes   = $notes.ToArray()
        }
    }

    return $overrides
}

# Decide which section a commit's subject is published under when the commit declares nothing: from
# a Conventional Commits type, or else from the verb the subject opens with, falling back to 'Changed'.
function Get-CommitCategory {
    param (
        [string]    $Subject
    )

    $conventionalMatch = [regex]::Match($Subject, $script:conventionalPrefixPattern)
    if ($conventionalMatch.Success) {
        if ($conventionalMatch.Groups['breaking'].Success) {
            return 'Breaking'
        }
        $type = $conventionalMatch.Groups['type'].Value
        if ($script:conventionalCategories.ContainsKey($type)) {
            return $script:conventionalCategories[$type]
        }
    }

    $note = Format-SubjectNote -Subject $Subject
    foreach ($category in $script:subjectCategoryPatterns.Keys) {
        if ($note -cmatch $script:subjectCategoryPatterns[$category]) {
            return $category
        }
    }

    return 'Changed'
}

# Reduce a subject to the change it states: drop a Conventional Commits prefix, whose type is
# carried by the section heading instead, and bold emphasis around the whole subject.
function Format-SubjectNote {
    param (
        [string]    $Subject
    )

    $note = [regex]::Replace($Subject, $script:conventionalPrefixPattern, [string]::Empty)
    $note = $note -replace '^\*\*(?<text>[^*]+)\*\*', '${text}'
    if (0 -lt $note.Length) {
        $note = $note.Substring(0, 1).ToUpperInvariant() + $note.Substring(1)
    }

    return $note
}

# Compose the release notes. The note count and total length are bounded so that a long accumulation
# cannot overrun the 32,766 character limit which applies to the encoded pipeline variable on
# Windows; that limit, not the larger .nuspec one, is the binding constraint.
$noteLimit = 60
$totalLengthLimit = 8000

$seenNotes = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$noteCount = 0
$noteLength = 0
$droppedCount = 0

# Notes are gathered by section across every commit, so that each section lists every change of its
# kind however the commits were ordered.
$noteTextsByCategory = [ordered]@{}
foreach ($category in $script:categoryHeadings.Keys) {
    $noteTextsByCategory[$category] = [System.Collections.Generic.List[string]]::new()
}

$overrides = Get-ReleaseNoteOverride -Path (Join-Path $PSScriptRoot '../release-notes-overrides.json')

foreach ($commit in (Get-CommitRecord -RevisionRange $revisionRange -PackageSourcePath $packageSourcePath)) {
    # An override states every note of the commit, breaking changes included, because it exists to
    # correct what the commit message says.
    $override = $overrides[$commit.CommitId]
    if ($null -ne $override) {
        if ($override.Subject -cne $commit.Subject) {
            Write-Warning "The release note override for $($commit.CommitId) names the subject '$($override.Subject)', but the commit's subject is '$($commit.Subject)'."
        }
        $declaredNotes = $override.Notes
    }
    else {
        # A breaking change trailer is published whatever else the commit declares, because a
        # consumer must not miss it.
        foreach ($breakingChange in (Get-BreakingChangeTrailer -Body $commit.Body)) {
            $noteTextsByCategory['Breaking'].Add($breakingChange)
        }
        $declaredNotes = @(Get-ReleaseNoteTrailer -Subject $commit.Subject -Body $commit.Body)
    }

    # Declared notes are the commit's complete notes; nothing more is inferred from its message.
    if ($null -ne $override -or 0 -lt $declaredNotes.Count) {
        foreach ($declaredNote in $declaredNotes) {
            if ('None' -ne $declaredNote.Category) {
                $noteTextsByCategory[$declaredNote.Category].Add($declaredNote.Text ?? (Format-SubjectNote -Subject $commit.Subject))
            }
        }
        continue
    }

    $noteTexts = $noteTextsByCategory[(Get-CommitCategory -Subject $commit.Subject)]

    if ($NoteSource -in @('Subject', 'Both')) {
        $noteTexts.Add((Format-SubjectNote -Subject $commit.Subject))
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

$sections = [System.Collections.Generic.List[string]]::new()
foreach ($category in $noteTextsByCategory.Keys) {
    $heading = "$($script:categoryHeadings[$category]):"
    $sectionBuilder = [System.Text.StringBuilder]::new()

    foreach ($noteText in $noteTextsByCategory[$category]) {
        $releaseNote = Format-ReleaseNote -Message $noteText -SanitisePattern $sanitisePattern
        if ($null -eq $releaseNote) {
            continue
        }

        # The same paragraph recurs where a change is amended or cherry-picked; publish it once.
        if (-not $seenNotes.Add($releaseNote)) {
            continue
        }

        # A section's heading counts towards the length only once it has a note to head.
        $addedLength = $releaseNote.Length + 3 + ((0 -eq $sectionBuilder.Length) ? $heading.Length + 3 : 0)
        if ($noteCount -ge $noteLimit -or ($noteLength + $addedLength) -gt $totalLengthLimit) {
            $droppedCount++
            continue
        }

        # A hyphen and a space so that the notes render as a list wherever they are read as Markdown.
        [void]$sectionBuilder.AppendLine("- $releaseNote")
        $noteCount++
        $noteLength += $addedLength
    }

    if (0 -lt $sectionBuilder.Length) {
        $sections.Add("$heading`n$($sectionBuilder.ToString().TrimEnd())")
    }
}

if (0 -lt $droppedCount) {
    Write-Warning "Kept $noteCount release notes for $PackageName; $droppedCount were dropped by the note count limit of $noteLimit or the length limit of $totalLengthLimit characters."
}

Write-Verbose "Composed $noteCount release notes for $PackageName."

$releaseNotes = (($sections -join "`n`n") -replace "`r", [string]::Empty).TrimEnd()

# A package is published without notes of its own where its publication was required by hand, or
# every change since the last release was left out as not affecting its consumers. Saying so tells a
# reader that nothing was missed, which an empty field does not.
if ([string]::IsNullOrWhiteSpace($releaseNotes)) {
    Write-Information "No commit since the last release of $PackageName affects its consumers."
    $releaseNotes = 'No changes affect the use of this package.'
}
$releaseNotes

# Output release notes to a pipeline variable or step output.
$encodedReleaseNotes = $releaseNotes | ConvertTo-Base64String
Set-CiOutput -Name 'releaseNotesArg' -Value $encodedReleaseNotes
