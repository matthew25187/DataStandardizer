############################################################################################
# Title: CLDR Currency Formats Source Code Generator                                       #
# Copyright: Copyright © 2026, Matthew25187. All rights reserved.                          #
#                                                                                          #
# Purpose: Generate source code for the culture-specific currency formatting information   #
#          used when formatting and parsing monetary values.                               #
# Source: Number symbols and currency patterns, and supplemental currency, likely-subtag   #
#         and parent-locale data (JSON), Unicode Common Locale Data Repository.            #
# https://github.com/unicode-org/cldr-json                                                 #
#                                                                                          #
# ISO 4217 does not define how amounts are presented in a locale, so the separators,       #
# grouping, sign and placement of the currency are sourced from CLDR, which is the         #
# reference data used by .NET, ICU, Java and browsers. CLDR is published under the Unicode #
# licence, which permits redistribution with attribution.                                  #
#                                                                                          #
# Constraints applied to the CLDR data:                                                    #
# - Only locales at or above the requested coverage level (modern by default) are emitted. #
#   Other locales resolve through CLDR's parent locales, likely subtags and truncation.    #
# - The Latin (latn) numbering system is used for every locale, because amounts are        #
#   always formatted with ASCII digits, whatever the locale's default numbering system.    #
# - Currency patterns are mapped onto the pattern indices of                               #
#   NumberFormatInfo.CurrencyPositivePattern and CurrencyNegativePattern. Bidirectional    #
#   marks in a pattern are dropped, and any white space in a pattern becomes the single    #
#   space those indices place. A pattern no index expresses stops the generator.           #
# - minimumGroupingDigits and currencySpacing are not represented.                         #
# - The root locale (und) is not emitted; a culture that resolves to root, or to nothing,  #
#   is given the package's invariant information instead.                                  #
# - A region's currency is the first current legal tender CLDR lists for it, and only     #
#   where that currency is a member of Iso4217CurrencyCurrent.                             #
############################################################################################
#Requires -Version 7.4
param (
    [Parameter(Mandatory, HelpMessage = 'Version of CLDR from which the data is taken, such as 47.0.0.')]
    [string]    $CldrVersion,
    [Parameter(HelpMessage = 'Folder holding the extracted cldr-core and cldr-numbers-full packages; downloaded from npm when omitted.')]
    [string]    $SourceFolderPath,
    [Parameter(Mandatory, HelpMessage = 'Name of the type in the generated source code.')]
    [string]    $SourceCodeTypeName,
    [Parameter(HelpMessage = 'Inline comment to be applied to the type in the generated source code.')]
    [string]    $SourceCodeTypeComment,
    [Parameter(HelpMessage = 'Language of the source code to be generated.')]
    [string]    $SourceCodeLanguage = 'CSharp',
    [Parameter(HelpMessage = 'Least CLDR coverage level of the locales to be emitted.')]
    [ValidateSet('modern', 'moderate', 'basic')]
    [string]    $CoverageLevel = 'modern'
)

$ErrorActionPreference = 'Stop'

$CldrPackageNames = @('cldr-core', 'cldr-numbers-full')

# The currency sign in CLDR patterns, and the characters a pattern may hold that a NumberFormatInfo
# pattern index cannot: bidirectional marks, and the forms of space between the number and the sign.
$CurrencySign = [string][char]0x00A4
$BidiMarks = @([char]0x200E, [char]0x200F, [char]0x061C)
$PatternSpaces = @([char]0x0020, [char]0x00A0, [char]0x202F)

# The layouts of NumberFormatInfo.CurrencyPositivePattern and CurrencyNegativePattern, written with '$'
# for the currency, 'n' for the number and '-' for the negative sign.
$PositivePatternIndexes = @{ '$n' = 0; 'n$' = 1; '$ n' = 2; 'n $' = 3 }
$NegativePatternIndexes = @{
    '($n)' = 0; '-$n' = 1; '$-n' = 2; '$n-' = 3; '(n$)' = 4; '-n$' = 5; 'n-$' = 6; 'n$-' = 7
    '-n $' = 8; '-$ n' = 9; 'n $-' = 10; '$ n-' = 11; '$ -n' = 12; 'n- $' = 13; '($ n)' = 14; '(n $)' = 15
    '$- n' = 16
}

function Get-CldrPackageFolder {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $PackageName
    )

    $packageFolder = Join-Path -Path $script:SourceFolder -ChildPath $PackageName
    # A package extracted from its npm tarball keeps its files in a 'package' folder.
    if (Test-Path -Path (Join-Path -Path $packageFolder -ChildPath 'package/package.json')) {
        $packageFolder = Join-Path -Path $packageFolder -ChildPath 'package'
    }

    $manifestPath = Join-Path -Path $packageFolder -ChildPath 'package.json'
    if (-not (Test-Path -Path $manifestPath)) {
        throw "The $PackageName package was not found in $script:SourceFolder."
    }

    $packageVersion = (Get-Content -Path $manifestPath -Raw -Encoding utf8 | ConvertFrom-Json).version
    if ($packageVersion -ne $CldrVersion) {
        throw "The $PackageName package is CLDR $packageVersion, not $CldrVersion."
    }

    return $packageFolder
}

function Read-CldrJson {
    param (
        [Parameter(Mandatory)]
        [string]    $Path
    )

    return Get-Content -Path $Path -Raw -Encoding utf8 | ConvertFrom-Json -AsHashtable
}

function Save-CldrPackage {
    param (
        [Parameter(Mandatory)]
        [string]    $DestinationFolder
    )

    # Each package is extracted into a fresh folder of its own, so nothing else in the temporary folder
    # can be mistaken for CLDR data.
    foreach ($packageName in $CldrPackageNames) {
        $packageUrl = "https://registry.npmjs.org/$packageName/-/$packageName-$CldrVersion.tgz"
        $packageFolder = Join-Path -Path $DestinationFolder -ChildPath $packageName
        [void](New-Item -ItemType Directory -Path $packageFolder)
        $archivePath = Join-Path -Path $DestinationFolder -ChildPath "$packageName.tgz"

        Write-Verbose "Downloading $packageUrl."
        Invoke-WebRequest -Uri $packageUrl -OutFile $archivePath
        tar -xzf $archivePath -C $packageFolder
        if ($LASTEXITCODE) {
            throw "Unable to extract $archivePath."
        }
    }
}

function Get-CoverageLocales {
    [OutputType([string[]])]
    param (
        [Parameter(Mandatory)]
        [string]    $CorePackageFolder
    )

    $levelOrder = @{ 'basic' = 0; 'moderate' = 1; 'modern' = 2 }
    $leastLevel = $levelOrder[$CoverageLevel]
    $coverage = (Read-CldrJson -Path (Join-Path -Path $CorePackageFolder -ChildPath 'coverageLevels.json')).effectiveCoverageLevels

    # The root locale (und) is not emitted: a culture that resolves to root is given the package's
    # invariant information, which matches the invariant culture of .NET.
    return [string[]]($coverage.GetEnumerator() |
            Where-Object { $_.Key -cne 'und' -and $levelOrder.ContainsKey($_.Value) -and $levelOrder[$_.Value] -ge $leastLevel } |
            ForEach-Object { $_.Key } |
            Sort-Object -CaseSensitive)
}

function ConvertFrom-CurrencyPattern {
    param (
        [Parameter(Mandatory)]
        [string]    $Pattern,

        [Parameter(Mandatory)]
        [string]    $Locale
    )

    $cleanPattern = -join ($Pattern.ToCharArray() | Where-Object { $BidiMarks -notcontains $_ } | ForEach-Object {
            if ($PatternSpaces -contains $_) { ' ' } else { $_ }
        })

    if ($cleanPattern.Contains("'")) {
        throw "The currency pattern of $Locale contains quoted text, which no pattern index expresses: $Pattern"
    }

    $subpatterns = $cleanPattern.Split(';')
    $positivePattern = $subpatterns[0]
    # Without an explicit negative subpattern, CLDR prefixes the minus sign to the positive subpattern.
    $negativePattern = if ($subpatterns.Count -gt 1) { $subpatterns[1] } else { "-$positivePattern" }

    $numberMatch = [regex]::Match($positivePattern, '[#0,.]+')
    $numberPart = $numberMatch.Value
    $integerPart, $fractionPart = $numberPart.Split('.')
    $integerGroups = $integerPart.Split(',')

    # The primary group is the run of digits nearest the decimal separator, and the secondary group the
    # run before it. A pattern with no grouping separator groups nothing.
    $groupSizes = switch ($integerGroups.Count) {
        1 { , @(0) }
        2 { , @($integerGroups[1].Length) }
        default { , @($integerGroups[-1].Length, $integerGroups[-2].Length) }
    }

    $fractionDigits = if ($null -eq $fractionPart) { 0 } else { $fractionPart.Length }

    $layouts = foreach ($subpattern in $positivePattern, $negativePattern) {
        $subpattern -replace '[#0,.]+', 'n' -replace [regex]::Escape($CurrencySign), '$' -replace ' +', ' '
    }

    if (-not $PositivePatternIndexes.ContainsKey($layouts[0])) {
        throw "The positive currency pattern of $Locale has no pattern index: $Pattern"
    }

    if (-not $NegativePatternIndexes.ContainsKey($layouts[1])) {
        throw "The negative currency pattern of $Locale has no pattern index: $Pattern"
    }

    return [pscustomobject]@{
        GroupSizes      = $groupSizes
        FractionDigits  = $fractionDigits
        PositivePattern = $PositivePatternIndexes[$layouts[0]]
        NegativePattern = $NegativePatternIndexes[$layouts[1]]
    }
}

function Get-LocaleFormat {
    param (
        [Parameter(Mandatory)]
        [string]    $NumbersPackageFolder,

        [Parameter(Mandatory)]
        [string]    $Locale
    )

    $numbersPath = Join-Path -Path $NumbersPackageFolder -ChildPath "main/$Locale/numbers.json"
    if (-not (Test-Path -Path $numbersPath)) {
        throw "No number data was found for $Locale."
    }

    # The CLDR JSON is resolved: each locale carries the values it inherits from its parents.
    $numbers = (Read-CldrJson -Path $numbersPath).main[$Locale].numbers
    $symbols = $numbers['symbols-numberSystem-latn']
    $currencyFormats = $numbers['currencyFormats-numberSystem-latn']
    if ($null -eq $symbols -or $null -eq $currencyFormats -or [string]::IsNullOrEmpty($currencyFormats.standard)) {
        throw "The Latin number data of $Locale is incomplete."
    }

    $pattern = ConvertFrom-CurrencyPattern -Pattern $currencyFormats.standard -Locale $Locale

    # A locale may separate currency amounts differently from other numbers.
    $decimalSeparator = if ($symbols.ContainsKey('currencyDecimal')) { $symbols.currencyDecimal } else { $symbols.decimal }
    $groupSeparator = if ($symbols.ContainsKey('currencyGroup')) { $symbols.currencyGroup } else { $symbols.group }

    return [pscustomobject]@{
        DecimalSeparator = $decimalSeparator
        GroupSeparator   = $groupSeparator
        GroupSizes       = $pattern.GroupSizes
        PositivePattern  = $pattern.PositivePattern
        NegativePattern  = $pattern.NegativePattern
        NegativeSign     = $symbols.minusSign
        FractionDigits   = $pattern.FractionDigits
    }
}

function Get-RegionCurrencies {
    param (
        [Parameter(Mandatory)]
        [string]    $CorePackageFolder,

        [Parameter(Mandatory)]
        [string[]]  $CurrencyNames
    )

    $regions = (Read-CldrJson -Path (Join-Path -Path $CorePackageFolder -ChildPath 'supplemental/currencyData.json')).supplemental.currencyData.region
    $regionCurrencies = [ordered]@{}
    $skippedRegions = @()

    foreach ($region in $regions.Keys | Sort-Object -CaseSensitive) {
        # The list is in CLDR's order of preference; the first current legal tender is the region's currency.
        $currency = $regions[$region] | ForEach-Object { $_.GetEnumerator() } | Where-Object {
            -not $_.Value.ContainsKey('_to') -and $_.Value['_tender'] -ne 'false'
        } | Select-Object -First 1

        if ($null -eq $currency) {
            continue
        }

        if ($CurrencyNames -cnotcontains $currency.Key) {
            $skippedRegions += "$region=$($currency.Key)"
            continue
        }

        $regionCurrencies[$region] = $currency.Key
    }

    if ($skippedRegions.Count -gt 0) {
        Write-Verbose "Skipped $($skippedRegions.Count) regions whose currency is not a member of Iso4217CurrencyCurrent: $($skippedRegions -join ', ')."
    }

    return $regionCurrencies
}

function Get-LocaleRelations {
    param (
        [Parameter(Mandatory)]
        [string]    $CorePackageFolder,

        [Parameter(Mandatory)]
        [string[]]  $Locales
    )

    $emitted = [System.Collections.Generic.HashSet[string]]::new([string[]]$Locales, [System.StringComparer]::Ordinal)
    $languages = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
    foreach ($locale in $Locales) {
        [void]$languages.Add($locale.Split('-')[0])
    }

    $likelySubtags = (Read-CldrJson -Path (Join-Path -Path $CorePackageFolder -ChildPath 'supplemental/likelySubtags.json')).supplemental.likelySubtags
    $parentLocales = (Read-CldrJson -Path (Join-Path -Path $CorePackageFolder -ChildPath 'supplemental/parentLocales.json')).supplemental.parentLocales.parentLocale

    # The likely script of each language decides CLDR's nonlikelyScript rule: a locale written in another
    # script inherits from root rather than from the language.
    $likelyScripts = [ordered]@{}
    foreach ($language in $languages | Sort-Object -CaseSensitive) {
        $likely = $likelySubtags[$language]
        if ($null -ne $likely) {
            $likelyScripts[$language] = $likely.Split('-')[1]
        }
    }

    # Explicit parents, for locales that are not emitted themselves but inherit from another locale than
    # the one truncation would give, such as en-150 from en-001.
    $parents = [ordered]@{}
    foreach ($child in $parentLocales.Keys | Sort-Object -CaseSensitive) {
        if (-not $emitted.Contains($child) -and $languages.Contains($child.Split('-')[0])) {
            $parents[$child] = $parentLocales[$child]
        }
    }

    # A language and region without a script, such as zh-TW, names the locale its likely subtags give
    # (zh-Hant-TW) where that script is not the language's own. Truncation would reach the wrong script.
    $aliases = [ordered]@{}
    foreach ($key in $likelySubtags.Keys | Sort-Object -CaseSensitive) {
        $subtags = $key.Split('-')
        if ($subtags.Count -ne 2 -or $subtags[1] -notmatch '^([A-Z]{2}|[0-9]{3})$' -or $emitted.Contains($key) -or -not $likelyScripts.Contains($subtags[0])) {
            continue
        }

        $likelyScript = $likelySubtags[$key].Split('-')[1]
        if ($likelyScript -cne $likelyScripts[$subtags[0]]) {
            $aliases[$key] = "$($subtags[0])-$likelyScript-$($subtags[1])"
        }
    }

    return [pscustomobject]@{
        LikelyScripts = $likelyScripts
        Parents       = $parents
        Aliases       = $aliases
    }
}

function ConvertTo-SourceCodeStringLiteral {
    param (
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Value
    )

    # Emit non-ASCII characters as escape sequences so the generated file is unambiguous regardless of
    # the encoding it is saved or viewed in. Separators and signs rely on characters that are easy to
    # corrupt in transit, such as U+00A0, U+202F, U+200E and U+2212.
    $literalBuilder = [System.Text.StringBuilder]::new()
    [void]$literalBuilder.Append('"')
    foreach ($character in $Value.ToCharArray()) {
        $codePoint = [int]$character
        if ($character -eq '"') {
            [void]$literalBuilder.Append('\"')
        }
        elseif ($character -eq '\') {
            [void]$literalBuilder.Append('\\')
        }
        elseif ($codePoint -lt 0x20 -or $codePoint -gt 0x7E) {
            [void]$literalBuilder.Append("\u$($codePoint.ToString('x4'))")
        }
        else {
            [void]$literalBuilder.Append($character)
        }
    }
    [void]$literalBuilder.Append('"')

    return $literalBuilder.ToString()
}

function Get-FieldDeclaration {
    [OutputType([System.CodeDom.CodeSnippetTypeMember])]
    param (
        [Parameter(Mandatory)]
        [string]    $Declaration,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]  $Entries,

        [Parameter(Mandatory)]
        [string]    $Comment
    )

    # CodeDOM can express neither a collection initialiser nor the readonly modifier, so the whole
    # declaration is emitted as a snippet member.
    $declarationBuilder = [System.Text.StringBuilder]::new()
    [void]$declarationBuilder.AppendLine("        // $Comment")
    [void]$declarationBuilder.AppendLine("        $Declaration")
    [void]$declarationBuilder.AppendLine('        {')
    foreach ($entry in $Entries) {
        [void]$declarationBuilder.AppendLine("            $entry,")
    }
    [void]$declarationBuilder.Append('        };')

    return [System.CodeDom.CodeSnippetTypeMember]::new($declarationBuilder.ToString())
}

function Out-SourceCode {
    param (
        [Parameter(Mandatory)]
        [array]     $LocaleFormats,

        [Parameter(Mandatory)]
        [System.Collections.IDictionary]    $RegionCurrencies,

        [Parameter(Mandatory)]
        [pscustomobject]    $Relations,

        [Parameter(Mandatory)]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter(Mandatory)]
        [string]    $Language
    )

    # Locales which format alike share one record, numbered in the order the sorted locales first use it.
    $formatEntries = [System.Collections.Generic.List[string]]::new()
    $formatIndexes = @{}
    $localeEntries = foreach ($localeFormat in $LocaleFormats) {
        $format = $localeFormat.Format
        $entry = 'new Format({0}, {1}, new int[] {{ {2} }}, {3}, {4}, {5}, {6})' -f `
        (ConvertTo-SourceCodeStringLiteral $format.DecimalSeparator), `
        (ConvertTo-SourceCodeStringLiteral $format.GroupSeparator), `
        ($format.GroupSizes -join ', '), `
            $format.PositivePattern, `
            $format.NegativePattern, `
        (ConvertTo-SourceCodeStringLiteral $format.NegativeSign), `
            $format.FractionDigits

        if (-not $formatIndexes.ContainsKey($entry)) {
            $formatIndexes[$entry] = $formatEntries.Count
            $formatEntries.Add($entry)
        }

        '{{ {0}, {1} }}' -f (ConvertTo-SourceCodeStringLiteral $localeFormat.Locale), $formatIndexes[$entry]
    }

    Write-Verbose "Emitting $($LocaleFormats.Count) locales sharing $($formatEntries.Count) formats, $($RegionCurrencies.Count) region currencies, $($Relations.Parents.Count) parent locales and $($Relations.Aliases.Count) aliases."

    $toStringPairs = {
        param($Dictionary, [scriptblock]$Value)
        foreach ($key in $Dictionary.Keys) {
            '{{ {0}, {1} }}' -f (ConvertTo-SourceCodeStringLiteral $key), (& $Value $Dictionary[$key])
        }
    }

    $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
    $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Money')
    [void]$compileUnit.Namespaces.Add($namespace)

    $classType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
    $classType.IsClass = $true
    $classType.TypeAttributes = [System.Reflection.TypeAttributes]::NotPublic
    # Declared partial so the hand-written lookup logic can live alongside the generated data without
    # being overwritten when this generator is re-run.
    $classType.IsPartial = $true
    [void]$classType.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Currency Format Data'))
    [void]$classType.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))

    if (-not [string]::IsNullOrEmpty($TypeComment)) {
        $classType.Comments.AddRange(@(
                [System.CodeDom.CodeCommentStatement]::new('<summary>', $true),
                [System.CodeDom.CodeCommentStatement]::new($TypeComment, $true),
                [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)))
    }

    $remarks = @(
        '<remarks>'
        "Based on Unicode CLDR $CldrVersion number and supplemental data for the locales at $CoverageLevel coverage."
        'ISO 4217 does not define how amounts are presented in a locale, so this data is sourced from CLDR'
        'rather than from the currency code standard itself.'
        '<para>'
        'Copyright &#169; 1991-Present Unicode, Inc. Distributed under the Unicode licence;'
        'see THIRD-PARTY-NOTICES.md and https://www.unicode.org/license.txt.'
        '</para>'
        '</remarks>'
    )
    foreach ($remark in $remarks) {
        [void]$classType.Comments.Add([System.CodeDom.CodeCommentStatement]::new($remark, $true))
    }

    [void]$namespace.Types.Add($classType)

    [void]$classType.Members.Add((Get-FieldDeclaration `
                -Declaration 'private static readonly Format[] Formats = new Format[]' `
                -Entries $formatEntries `
                -Comment 'Currency formats: decimal separator, group separator, group sizes, positive pattern, negative pattern, negative sign and fraction digits.'))

    [void]$classType.Members.Add((Get-FieldDeclaration `
                -Declaration 'private static readonly System.Collections.Generic.Dictionary<string, int> LocaleFormats = new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase)' `
                -Entries $localeEntries `
                -Comment 'The index into Formats of each locale emitted.'))

    [void]$classType.Members.Add((Get-FieldDeclaration `
                -Declaration 'private static readonly System.Collections.Generic.Dictionary<string, string> LocaleAliases = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)' `
                -Entries ([string[]](& $toStringPairs $Relations.Aliases { param($v) ConvertTo-SourceCodeStringLiteral $v })) `
                -Comment 'Language and region pairs whose likely script is not the language''s own, and the locale they name.'))

    [void]$classType.Members.Add((Get-FieldDeclaration `
                -Declaration 'private static readonly System.Collections.Generic.Dictionary<string, string> ParentLocales = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)' `
                -Entries ([string[]](& $toStringPairs $Relations.Parents { param($v) ConvertTo-SourceCodeStringLiteral $v })) `
                -Comment 'Locales whose parent is not the locale truncation would give.'))

    [void]$classType.Members.Add((Get-FieldDeclaration `
                -Declaration 'private static readonly System.Collections.Generic.Dictionary<string, string> LikelyScripts = new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)' `
                -Entries ([string[]](& $toStringPairs $Relations.LikelyScripts { param($v) ConvertTo-SourceCodeStringLiteral $v })) `
                -Comment 'The likely script of each language; a locale in another script inherits from root.'))

    [void]$classType.Members.Add((Get-FieldDeclaration `
                -Declaration 'private static readonly System.Collections.Generic.Dictionary<string, DataStandardizer.Money.Iso4217CurrencyCurrent> RegionCurrencies = new System.Collections.Generic.Dictionary<string, DataStandardizer.Money.Iso4217CurrencyCurrent>(System.StringComparer.OrdinalIgnoreCase)' `
                -Entries ([string[]](& $toStringPairs $RegionCurrencies { param($v) "DataStandardizer.Money.Iso4217CurrencyCurrent.$v" })) `
                -Comment 'The currency of each region.'))

    $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($Language)

    $options = [System.CodeDom.Compiler.CodeGeneratorOptions]::new()
    $options.BlankLinesBetweenMembers = $true
    $options.BracingStyle = 'C'

    $sourceCodeBuilder = [System.Text.StringBuilder]::new()
    $writer = [System.IO.StringWriter]::new($sourceCodeBuilder)
    try {
        $provider.GenerateCodeFromCompileUnit($compileUnit, $writer, $options)
        Write-Output $sourceCodeBuilder.ToString()
    }
    finally {
        $writer.Close()
    }
}

Write-Debug '================================================================================'
Write-Debug 'PARAMETERS'
Write-Debug "CLDR version: $CldrVersion"
Write-Debug "Source folder: $SourceFolderPath"
Write-Debug "Coverage level: $CoverageLevel"
Write-Debug "Generated type name: $SourceCodeTypeName"
Write-Debug "Generated type comment: $SourceCodeTypeComment"
Write-Debug "Generated language: $SourceCodeLanguage"

# The ISO 4217 enum must be loaded so that regions are only given currencies it defines.
$moneyAssemblyPath = Join-Path -Path $PSScriptRoot -ChildPath '..\src\DataStandardizer.Money\bin\Debug\net10.0\DataStandardizer.Money.dll' -Resolve -ErrorAction SilentlyContinue
if ([string]::IsNullOrEmpty($moneyAssemblyPath)) {
    throw 'Unable to locate DataStandardizer.Money.dll. Build the project before running this generator.'
}
Add-Type -Path $moneyAssemblyPath
$currentCurrencyNames = [string[]][System.Enum]::GetNames([DataStandardizer.Money.Iso4217CurrencyCurrent])

if ([string]::IsNullOrEmpty($SourceFolderPath)) {
    $script:SourceFolder = Join-Path -Path ([System.IO.Path]::GetTempPath()) -ChildPath "cldr-$CldrVersion-$([guid]::NewGuid().ToString('N'))"
    [void](New-Item -ItemType Directory -Path $script:SourceFolder)
    Save-CldrPackage -DestinationFolder $script:SourceFolder
}
else {
    $script:SourceFolder = $SourceFolderPath
}

$corePackageFolder = Get-CldrPackageFolder -PackageName 'cldr-core'
$numbersPackageFolder = Get-CldrPackageFolder -PackageName 'cldr-numbers-full'

$locales = Get-CoverageLocales -CorePackageFolder $corePackageFolder
Write-Verbose "Found $($locales.Count) locales at $CoverageLevel coverage or above."

$localeFormats = foreach ($locale in $locales) {
    [pscustomobject]@{
        Locale = $locale
        Format = Get-LocaleFormat -NumbersPackageFolder $numbersPackageFolder -Locale $locale
    }
}

$regionCurrencies = Get-RegionCurrencies -CorePackageFolder $corePackageFolder -CurrencyNames $currentCurrencyNames
$relations = Get-LocaleRelations -CorePackageFolder $corePackageFolder -Locales $locales

Out-SourceCode -LocaleFormats $localeFormats -RegionCurrencies $regionCurrencies -Relations $relations `
    -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -Language $SourceCodeLanguage
