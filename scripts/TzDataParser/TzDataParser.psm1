#############################################################################
# Title: TZ Database Source Parser                                          #
# Copyright: Copyright © 2026, Matthew25187. All rights reserved.           #
#                                                                           #
# Purpose: Parse the Rule, Zone and Link lines of the TZ Database source.   #
# Source: Time Zone Database, IANA.                                         #
# https://www.iana.org/time-zones                                           #
#############################################################################
#Requires -Version 7.4

# The main-format region files, which hold every Zone named in zone1970.tab.
$script:RegionFileNames = @('africa', 'antarctica', 'asia', 'australasia', 'europe', 'northamerica', 'southamerica')

$script:LineKeywords = @('Rule', 'Zone', 'Link')
$script:MonthNames = @('January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December')
# Ordered to match the values of System.DayOfWeek.
$script:WeekdayNames = @('Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday')
$script:FromYearKeywords = @('minimum')
$script:ToYearKeywords = @('maximum', 'only')

$script:TimePattern = [regex]::new('^(?<sign>-)?(?<hours>\d+)(?::(?<minutes>\d{1,2})(?::(?<seconds>\d{1,2})(?:\.(?<fraction>\d+))?)?)?(?<suffix>[a-z])?$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:WeekdayRelativeDayPattern = [regex]::new('^(?<weekday>[a-z]+)(?<operator>>=|<=)(?<day>\d+)$', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
$script:YearPattern = [regex]::new('^-?\d+$')

# The leap year against which a day of the month is validated, as zic does, so that Feb 29 is accepted.
$script:LeapYear = 2000

function Find-TzDataKeyword {
    <#
        .SYNOPSIS
        Finds the index of the keyword in a table that a word names, as zic does: case-insensitively, and by
        exact match or else by any unambiguous abbreviation.  Returns -1 where the word names no keyword or is
        ambiguous.
    #>
    [OutputType([int])]
    param (
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Word,

        [Parameter(Mandatory)]
        [string[]]  $Keywords
    )

    if ($Word.Length -eq 0) {
        return -1
    }

    for ($index = 0; $index -lt $Keywords.Length; $index++) {
        if ([string]::Equals($Word, $Keywords[$index], [System.StringComparison]::OrdinalIgnoreCase)) {
            return $index
        }
    }

    $foundIndex = -1
    for ($index = 0; $index -lt $Keywords.Length; $index++) {
        if ($Keywords[$index].StartsWith($Word, [System.StringComparison]::OrdinalIgnoreCase)) {
            if ($foundIndex -ge 0) {
                return -1
            }
            $foundIndex = $index
        }
    }

    return $foundIndex
}

function Split-TzDataLine {
    <#
        .SYNOPSIS
        Splits a line of TZ Database source into its fields, discarding any comment.  A field may be enclosed
        in double quotes, in which case it may contain white space or '#'.
    #>
    [OutputType([string[]])]
    param (
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Line
    )

    # Most lines contain no quotes, and are split directly.
    if ($Line.IndexOf('"') -lt 0) {
        $commentIndex = $Line.IndexOf('#')
        if ($commentIndex -ge 0) {
            $Line = $Line.Substring(0, $commentIndex)
        }
        return , [string[]]($Line.Split([char[]]@(' ', "`t", "`f", "`v", "`r", "`n"), [System.StringSplitOptions]::RemoveEmptyEntries))
    }

    $fields = [System.Collections.Generic.List[string]]::new()
    $field = $null
    $inQuotes = $false
    foreach ($character in $Line.ToCharArray()) {
        if ($inQuotes) {
            if ($character -eq '"') {
                $inQuotes = $false
            }
            else {
                $field += $character
            }
        }
        elseif ($character -eq '#') {
            break
        }
        elseif ($character -eq '"') {
            $inQuotes = $true
            if ($null -eq $field) {
                $field = [string]::Empty
            }
        }
        elseif ([char]::IsWhiteSpace($character)) {
            if ($null -ne $field) {
                $fields.Add($field)
                $field = $null
            }
        }
        else {
            $field += $character
        }
    }
    if ($inQuotes) {
        throw 'Unterminated quoted string.'
    }
    if ($null -ne $field) {
        $fields.Add($field)
    }

    return , $fields.ToArray()
}

function Read-TzDataSource {
    <#
        .SYNOPSIS
        Reads the Rule, Zone and Link lines of the main-format TZ Database region files.

        .DESCRIPTION
        Tokenises each file, discarding comments and blank lines, and gathers the continuation lines of each Zone
        with the Zone line itself.  Returns an object with three ordinal, case-sensitive ordered dictionaries:

        - Rules: the name of each rule set to the Rule lines that belong to it;
        - Zones: the name of each zone to its zone lines, the first taken from the Zone line itself;
        - Links: the name of each link to the name of its target.

        Each line is an object with the properties Fields, the fields following the name (or, for a
        continuation line, all of its fields), and Location, the file name and line number for diagnostics.

        .PARAMETER SourceFolderPath
        Path to the folder containing the extracted TZ Database source.

        .PARAMETER FileName
        Names of the files to read.  Defaults to the region files: africa, antarctica, asia, australasia, europe,
        northamerica and southamerica.
    #>
    [CmdletBinding()]
    [OutputType([pscustomobject])]
    param (
        [Parameter(Mandatory)]
        [string]    $SourceFolderPath,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string[]]  $FileName = $script:RegionFileNames
    )

    $rules = [System.Collections.Specialized.OrderedDictionary]::new([System.StringComparer]::Ordinal)
    $zones = [System.Collections.Specialized.OrderedDictionary]::new([System.StringComparer]::Ordinal)
    $links = [System.Collections.Specialized.OrderedDictionary]::new([System.StringComparer]::Ordinal)
    $zoneLocations = @{}

    foreach ($file in $FileName) {
        $filePath = $SourceFolderPath | Join-Path -ChildPath $file
        if (-not (Test-Path -Path $filePath -PathType Leaf)) {
            throw "Source file '$filePath' not found."
        }

        Write-Verbose "Reading TZ Database source file '$filePath'."

        $lineNumber = 0
        $continuedZoneLines = $null
        $continuedZoneName = $null
        foreach ($line in [System.IO.File]::ReadLines($filePath)) {
            $lineNumber++
            $location = "${file}:$lineNumber"

            try {
                $fields = Split-TzDataLine -Line $line
            }
            catch {
                throw "${location}: $($_.Exception.Message)"
            }
            if ($fields.Length -eq 0) {
                continue
            }

            # A zone line with an UNTIL column is followed by a continuation line.
            if ($null -ne $continuedZoneLines) {
                if ($fields.Length -lt 3 -or $fields.Length -gt 7) {
                    throw "${location}: Zone $continuedZoneName continuation line has $($fields.Length) fields, where 3 to 7 are expected."
                }
                $continuedZoneLines.Add([pscustomobject]@{ Fields = $fields; Location = $location })
                if ($fields.Length -eq 3) {
                    $continuedZoneLines = $null
                    $continuedZoneName = $null
                }
                continue
            }

            switch (Find-TzDataKeyword -Word $fields[0] -Keywords $script:LineKeywords) {
                0 {
                    if ($fields.Length -ne 10) {
                        throw "${location}: Rule line has $($fields.Length) fields, where 10 are expected."
                    }
                    $name = $fields[1]
                    if (-not $rules.Contains($name)) {
                        $rules[$name] = [System.Collections.Generic.List[pscustomobject]]::new()
                    }
                    $rules[$name].Add([pscustomobject]@{ Fields = [string[]]$fields[2..9]; Location = $location })
                }
                1 {
                    if ($fields.Length -lt 5 -or $fields.Length -gt 9) {
                        throw "${location}: Zone line has $($fields.Length) fields, where 5 to 9 are expected."
                    }
                    $name = $fields[1]
                    if ($zones.Contains($name)) {
                        throw "${location}: Zone $name is already defined at $($zoneLocations[$name])."
                    }
                    if ($links.Contains($name)) {
                        throw "${location}: Zone $name is already defined as a Link."
                    }
                    $zoneLines = [System.Collections.Generic.List[pscustomobject]]::new()
                    $zoneLines.Add([pscustomobject]@{ Fields = [string[]]$fields[2..($fields.Length - 1)]; Location = $location })
                    $zones[$name] = $zoneLines
                    $zoneLocations[$name] = $location
                    if ($fields.Length -gt 5) {
                        $continuedZoneLines = $zoneLines
                        $continuedZoneName = $name
                    }
                }
                2 {
                    if ($fields.Length -ne 3) {
                        throw "${location}: Link line has $($fields.Length) fields, where 3 are expected."
                    }
                    $name = $fields[2]
                    if ($links.Contains($name) -or $zones.Contains($name)) {
                        throw "${location}: Link $name is already defined."
                    }
                    $links[$name] = $fields[1]
                }
                default {
                    throw "${location}: Line begins with '$($fields[0])', where Rule, Zone or Link is expected."
                }
            }
        }

        if ($null -ne $continuedZoneLines) {
            throw "${file}: Zone $continuedZoneName ends with an UNTIL column, but no continuation line follows."
        }
    }

    return [pscustomobject]@{
        Rules = $rules
        Zones = $zones
        Links = $links
    }
}

function ConvertFrom-TzDataTime {
    <#
        .SYNOPSIS
        Converts a time of day or offset, such as 2:00, -4:56:02, 24:00 or 1:00s, to an object with the properties
        Time, a TimeSpan, and Reference, the clock it is measured against: Wall, Standard or Universal.

        .DESCRIPTION
        Accepts an optional sign, then hours, optionally followed by minutes and seconds, the seconds optionally
        with a fraction, which is rounded to the nearest second, ties to even, as zic does.  Hours may exceed 23.
        A value of '-' denotes zero.  A suffix of w denotes wall clock time, s standard time, and u, g or z
        universal time; with none, wall clock time applies.

        .PARAMETER Value
        The value to convert.

        .PARAMETER NoSuffix
        Rejects a value with a suffix, as for a STDOFF column.
    #>
    [OutputType([pscustomobject])]
    param (
        [Parameter(Mandatory)]
        [string]    $Value,

        [Parameter()]
        [switch]    $NoSuffix
    )

    if ($Value -eq '-') {
        return [pscustomobject]@{ Time = [timespan]::Zero; Reference = 'Wall' }
    }

    $match = $script:TimePattern.Match($Value)
    if (-not $match.Success) {
        throw "'$Value' is not a valid time."
    }

    [long]$hours = $match.Groups['hours'].Value
    [int]$minutes = $match.Groups['minutes'].Success ? $match.Groups['minutes'].Value : 0
    [int]$seconds = $match.Groups['seconds'].Success ? $match.Groups['seconds'].Value : 0
    if ($minutes -ge 60 -or $seconds -ge 60) {
        throw "'$Value' is not a valid time; minutes and seconds must be less than 60."
    }

    [long]$totalSeconds = ($hours * 3600) + ($minutes * 60) + $seconds
    if ($match.Groups['fraction'].Success) {
        $fraction = [decimal]::Parse("0.$($match.Groups['fraction'].Value)", [System.Globalization.CultureInfo]::InvariantCulture)
        if ($fraction -gt 0.5 -or ($fraction -eq 0.5 -and $totalSeconds % 2 -ne 0)) {
            $totalSeconds++
        }
    }
    if ($match.Groups['sign'].Success) {
        $totalSeconds = -$totalSeconds
    }

    $reference = 'Wall'
    if ($match.Groups['suffix'].Success) {
        if ($NoSuffix) {
            throw "'$Value' is not a valid time; a suffix is not permitted."
        }
        $reference = switch ($match.Groups['suffix'].Value.ToLowerInvariant()) {
            'w' { 'Wall' }
            's' { 'Standard' }
            { $_ -in 'u', 'g', 'z' } { 'Universal' }
            default { throw "'$Value' is not a valid time; the suffix '$_' is not recognised." }
        }
    }

    return [pscustomobject]@{ Time = [timespan]::FromSeconds($totalSeconds); Reference = $reference }
}

function ConvertFrom-TzDataSave {
    <#
        .SYNOPSIS
        Converts a SAVE column, such as 1:00, 0, -1:00 or 0:30d, to an object with the properties Save, a
        TimeSpan, and IsDaylight.

        .DESCRIPTION
        A suffix of d denotes daylight saving time and s standard time.  With no suffix, as zic does, the time
        is daylight saving time where the amount saved is not zero.

        .PARAMETER Value
        The value to convert.
    #>
    [OutputType([pscustomobject])]
    param (
        [Parameter(Mandatory)]
        [string]    $Value
    )

    $isDaylight = $null
    $amount = $Value
    if ($Value.Length -gt 1) {
        switch ($Value.Substring($Value.Length - 1).ToLowerInvariant()) {
            'd' { $isDaylight = $true }
            's' { $isDaylight = $false }
        }
        if ($null -ne $isDaylight) {
            $amount = $Value.Substring(0, $Value.Length - 1)
        }
    }

    try {
        $save = (ConvertFrom-TzDataTime -Value $amount -NoSuffix).Time
    }
    catch {
        throw "'$Value' is not a valid amount of saved time."
    }
    if ($null -eq $isDaylight) {
        $isDaylight = $save -ne [timespan]::Zero
    }

    return [pscustomobject]@{ Save = $save; IsDaylight = $isDaylight }
}

function ConvertFrom-TzDataMonth {
    <#
        .SYNOPSIS
        Converts a month name, or any unambiguous abbreviation of one, to its number from 1 to 12.

        .PARAMETER Value
        The value to convert.
    #>
    [OutputType([int])]
    param (
        [Parameter(Mandatory)]
        [string]    $Value
    )

    $index = Find-TzDataKeyword -Word $Value -Keywords $script:MonthNames
    if ($index -lt 0) {
        throw "'$Value' is not a valid month."
    }

    return $index + 1
}

function ConvertFrom-TzDataWeekday {
    [OutputType([System.DayOfWeek])]
    param (
        [Parameter(Mandatory)]
        [string]    $Value
    )

    $index = Find-TzDataKeyword -Word $Value -Keywords $script:WeekdayNames
    if ($index -lt 0) {
        throw "'$Value' is not a valid weekday."
    }

    return [System.DayOfWeek]$index
}

function ConvertFrom-TzDataDayRule {
    <#
        .SYNOPSIS
        Converts a day specification, such as 5, lastSun, Sun>=8 or Sun<=25, to an object with the properties
        DayKind, Day and DayOfWeek, which correspond to those of TzDataRule and TzDataUntil.

        .PARAMETER Value
        The value to convert.

        .PARAMETER Month
        The month, from 1 to 12, in which the day is specified.  A day of the month is validated against the
        length of the month in a leap year, as zic does.
    #>
    [OutputType([pscustomobject])]
    param (
        [Parameter(Mandatory)]
        [string]    $Value,

        [Parameter(Mandatory)]
        [ValidateRange(1, 12)]
        [int]       $Month
    )

    $dayMaximum = [datetime]::DaysInMonth($script:LeapYear, $Month)

    if ($Value -match '^\d+$') {
        $dayKind = 'DayOfMonth'
        [int]$day = $Value
        $dayOfWeek = $null
    }
    elseif ($Value.Length -gt 4 -and $Value.StartsWith('last', [System.StringComparison]::OrdinalIgnoreCase)) {
        $dayKind = 'LastWeekday'
        $day = $null
        $dayOfWeek = ConvertFrom-TzDataWeekday -Value $Value.Substring(4)
    }
    else {
        $match = $script:WeekdayRelativeDayPattern.Match($Value)
        if (-not $match.Success) {
            throw "'$Value' is not a valid day."
        }
        $dayKind = $match.Groups['operator'].Value -eq '>=' ? 'WeekdayOnOrAfter' : 'WeekdayOnOrBefore'
        [int]$day = $match.Groups['day'].Value
        $dayOfWeek = ConvertFrom-TzDataWeekday -Value $match.Groups['weekday'].Value
    }

    if ($null -ne $day -and ($day -lt 1 -or $day -gt $dayMaximum)) {
        throw "'$Value' is not a valid day of month $Month."
    }

    return [pscustomobject]@{ DayKind = $dayKind; Day = $day; DayOfWeek = $dayOfWeek }
}

function ConvertFrom-TzDataYear {
    <#
        .SYNOPSIS
        Converts the FROM or TO column of a Rule line to a year.

        .DESCRIPTION
        A FROM column may be a year, or minimum, which converts to [int]::MinValue.  A TO column may be a year,
        maximum, which converts to [int]::MaxValue, or only, which converts to the FROM year.  As zic does,
        minimum, maximum and only may be given as any unambiguous abbreviation, such as min, max or o.

        .PARAMETER Value
        The value to convert.

        .PARAMETER FromYear
        The year of the FROM column, which identifies the value as a TO column.
    #>
    [OutputType([int])]
    param (
        [Parameter(Mandatory)]
        [string]    $Value,

        [Parameter()]
        [System.Nullable[int]]  $FromYear
    )

    $isTo = $PSBoundParameters.ContainsKey('FromYear')

    if ($script:YearPattern.IsMatch($Value)) {
        [int]$year = 0
        if (-not [int]::TryParse($Value, [System.Globalization.NumberStyles]::AllowLeadingSign, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$year)) {
            throw "'$Value' is not a valid year."
        }
    }
    elseif ($isTo) {
        switch (Find-TzDataKeyword -Word $Value -Keywords $script:ToYearKeywords) {
            0 { $year = [int]::MaxValue }
            1 { $year = $FromYear }
            default { throw "'$Value' is not a valid TO year." }
        }
    }
    else {
        switch (Find-TzDataKeyword -Word $Value -Keywords $script:FromYearKeywords) {
            0 { $year = [int]::MinValue }
            default { throw "'$Value' is not a valid FROM year." }
        }
    }

    if ($isTo -and $year -lt $FromYear) {
        throw "TO year '$Value' precedes FROM year $FromYear."
    }

    return $year
}

function ConvertFrom-TzDataUntil {
    <#
        .SYNOPSIS
        Converts the UNTIL columns of a zone line to an object with the properties Year, Month, DayKind, Day,
        DayOfWeek, Time and TimeReference, which correspond to those of TzDataUntil.

        .DESCRIPTION
        Columns omitted take their earliest value: month January, day 1, and time 0:00 wall clock time.

        .PARAMETER Fields
        The one to four UNTIL columns: year, and optionally month, day and time.
    #>
    [OutputType([pscustomobject])]
    param (
        [Parameter(Mandatory)]
        [ValidateCount(1, 4)]
        [string[]]  $Fields
    )

    if (-not $script:YearPattern.IsMatch($Fields[0])) {
        throw "'$($Fields[0])' is not a valid UNTIL year."
    }
    [int]$year = $Fields[0]
    $month = $Fields.Length -gt 1 ? (ConvertFrom-TzDataMonth -Value $Fields[1]) : 1
    $dayRule = ConvertFrom-TzDataDayRule -Value ($Fields.Length -gt 2 ? $Fields[2] : '1') -Month $month
    $time = $Fields.Length -gt 3 ? (ConvertFrom-TzDataTime -Value $Fields[3]) : (ConvertFrom-TzDataTime -Value '0')

    return [pscustomobject]@{
        Year          = $year
        Month         = $month
        DayKind       = $dayRule.DayKind
        Day           = $dayRule.Day
        DayOfWeek     = $dayRule.DayOfWeek
        Time          = $time.Time
        TimeReference = $time.Reference
    }
}

function ConvertFrom-TzDataRule {
    <#
        .SYNOPSIS
        Converts the Rule lines of a rule set to objects with the properties Name, FromYear, ToYear, Month,
        DayKind, Day, DayOfWeek, AtTime, AtTimeReference, Save, IsDaylight and Letter, which correspond to the
        parameters of the TzDataRule constructor.

        .PARAMETER Name
        The name of the rule set.

        .PARAMETER Lines
        The Rule lines of the rule set, as returned by Read-TzDataSource.
    #>
    [OutputType([pscustomobject[]])]
    param (
        [Parameter(Mandatory)]
        [string]    $Name,

        [Parameter(Mandatory)]
        [pscustomobject[]]  $Lines
    )

    foreach ($line in $Lines) {
        try {
            # Columns: FROM TO - IN ON AT SAVE LETTER/S
            $fields = $line.Fields
            if ($fields[2] -ne '-') {
                throw "The reserved column contains '$($fields[2])', where '-' is expected."
            }
            $fromYear = ConvertFrom-TzDataYear -Value $fields[0]
            $toYear = ConvertFrom-TzDataYear -Value $fields[1] -FromYear $fromYear
            $month = ConvertFrom-TzDataMonth -Value $fields[3]
            $dayRule = ConvertFrom-TzDataDayRule -Value $fields[4] -Month $month
            $at = ConvertFrom-TzDataTime -Value $fields[5]
            $save = ConvertFrom-TzDataSave -Value $fields[6]

            [pscustomobject]@{
                Name            = $Name
                FromYear        = $fromYear
                ToYear          = $toYear
                Month           = $month
                DayKind         = $dayRule.DayKind
                Day             = $dayRule.Day
                DayOfWeek       = $dayRule.DayOfWeek
                AtTime          = $at.Time
                AtTimeReference = $at.Reference
                Save            = $save.Save
                IsDaylight      = $save.IsDaylight
                Letter          = $fields[7] -eq '-' ? [string]::Empty : $fields[7]
            }
        }
        catch {
            throw "$($line.Location): Rule ${Name}: $($_.Exception.Message)"
        }
    }
}

function Get-TzDataUntilInstant {
    <#
        .SYNOPSIS
        Approximates the universal time at which a zone line ceases to apply, for ordering zone lines.  The
        standard offset of the zone line applies, and any fixed save, but not the save of any rule set.
    #>
    [OutputType([datetime])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject]    $ZoneLine
    )

    $until = $ZoneLine.Until
    if ($until.Year -lt 1 -or $until.Year -gt 9999) {
        throw "UNTIL year $($until.Year) is out of range."
    }

    $date = switch ($until.DayKind) {
        'DayOfMonth' {
            if ($until.Day -gt [datetime]::DaysInMonth($until.Year, $until.Month)) {
                throw "UNTIL day $($until.Day) of month $($until.Month) does not occur in year $($until.Year)."
            }
            [datetime]::new($until.Year, $until.Month, $until.Day)
        }
        'LastWeekday' {
            $anchor = [datetime]::new($until.Year, $until.Month, [datetime]::DaysInMonth($until.Year, $until.Month))
            $anchor.AddDays( - ((([int]$anchor.DayOfWeek - [int]$until.DayOfWeek) + 7) % 7))
        }
        'WeekdayOnOrAfter' {
            $anchor = [datetime]::new($until.Year, $until.Month, 1).AddDays($until.Day - 1)
            $anchor.AddDays(((([int]$until.DayOfWeek - [int]$anchor.DayOfWeek) + 7) % 7))
        }
        'WeekdayOnOrBefore' {
            $anchor = [datetime]::new($until.Year, $until.Month, 1).AddDays($until.Day - 1)
            $anchor.AddDays( - ((([int]$anchor.DayOfWeek - [int]$until.DayOfWeek) + 7) % 7))
        }
    }

    $offset = switch ($until.TimeReference) {
        'Universal' { [timespan]::Zero }
        'Standard' { $ZoneLine.StandardOffset }
        'Wall' { $ZoneLine.StandardOffset + ($null -ne $ZoneLine.FixedSave ? $ZoneLine.FixedSave : [timespan]::Zero) }
    }

    return $date + $until.Time - $offset
}

function ConvertFrom-TzDataZone {
    <#
        .SYNOPSIS
        Converts the zone lines of a zone to objects with the properties StandardOffset, RuleKind, FixedSave,
        RuleSetName, Format and Until, which correspond to the parameters of the TzDataZoneLine constructor.

        .DESCRIPTION
        RuleKind is None, FixedSave or RuleSet, and Until is $null for the last line, or else an object as returned
        by ConvertFrom-TzDataUntil.  Fails where a zone line references a rule set that does not exist, where any
        line but the last has no UNTIL or the last has one, or where the UNTIL values do not strictly increase.

        .PARAMETER Name
        The name of the zone.

        .PARAMETER Lines
        The zone lines of the zone, as returned by Read-TzDataSource.

        .PARAMETER RuleSetNames
        The names of the rule sets defined in the source.
    #>
    [OutputType([pscustomobject[]])]
    param (
        [Parameter(Mandatory)]
        [string]    $Name,

        [Parameter(Mandatory)]
        [pscustomobject[]]  $Lines,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [System.Collections.ICollection]    $RuleSetNames
    )

    $ruleSetNameSet = [System.Collections.Generic.HashSet[string]]::new([string[]]@($RuleSetNames), [System.StringComparer]::Ordinal)
    $zoneLines = [System.Collections.Generic.List[pscustomobject]]::new()
    $previousLine = $null
    $previousInstant = $null

    for ($index = 0; $index -lt $Lines.Length; $index++) {
        $line = $Lines[$index]
        $isLast = $index -eq $Lines.Length - 1
        try {
            # Columns: STDOFF RULES FORMAT [UNTIL]
            $fields = $line.Fields
            $standardOffset = (ConvertFrom-TzDataTime -Value $fields[0] -NoSuffix).Time

            $fixedSave = $null
            $ruleSetName = $null
            if ($fields[1] -eq '-') {
                $ruleKind = 'None'
            }
            elseif ($fields[1] -match '^-?\d') {
                $ruleKind = 'FixedSave'
                $fixedSave = (ConvertFrom-TzDataSave -Value $fields[1]).Save
            }
            else {
                $ruleKind = 'RuleSet'
                $ruleSetName = $fields[1]
                if (-not $ruleSetNameSet.Contains($ruleSetName)) {
                    throw "Rule set $ruleSetName does not exist."
                }
            }

            $format = $fields[2]
            if ([string]::IsNullOrEmpty($format)) {
                throw 'FORMAT is empty.'
            }

            $until = $null
            if ($fields.Length -gt 3) {
                if ($isLast) {
                    throw 'The last zone line has an UNTIL column.'
                }
                $until = ConvertFrom-TzDataUntil -Fields ([string[]]$fields[3..($fields.Length - 1)])
            }
            elseif (-not $isLast) {
                throw 'A zone line other than the last has no UNTIL column.'
            }

            $zoneLine = [pscustomobject]@{
                StandardOffset = $standardOffset
                RuleKind       = $ruleKind
                FixedSave      = $fixedSave
                RuleSetName    = $ruleSetName
                Format         = $format
                Until          = $until
            }

            if ($null -ne $until) {
                $instant = Get-TzDataUntilInstant -ZoneLine $zoneLine
                if ($null -ne $previousInstant -and $instant -le $previousInstant) {
                    throw "UNTIL $($fields[3..($fields.Length - 1)] -join ' ') does not follow UNTIL $($previousLine.Fields[3..($previousLine.Fields.Length - 1)] -join ' ') of the preceding line."
                }
                $previousInstant = $instant
            }

            $zoneLines.Add($zoneLine)
            $previousLine = $line
        }
        catch {
            throw "$($line.Location): Zone ${Name}: $($_.Exception.Message)"
        }
    }

    return , $zoneLines.ToArray()
}

Export-ModuleMember -Function Read-TzDataSource
Export-ModuleMember -Function ConvertFrom-TzDataTime
Export-ModuleMember -Function ConvertFrom-TzDataSave
Export-ModuleMember -Function ConvertFrom-TzDataMonth
Export-ModuleMember -Function ConvertFrom-TzDataDayRule
Export-ModuleMember -Function ConvertFrom-TzDataYear
Export-ModuleMember -Function ConvertFrom-TzDataUntil
Export-ModuleMember -Function ConvertFrom-TzDataRule
Export-ModuleMember -Function ConvertFrom-TzDataZone
