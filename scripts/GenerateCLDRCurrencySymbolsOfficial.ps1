############################################################################################
# Title: CLDR Currency Symbols Source Code Generator                                       #
# Copyright: Copyright © 2026, Matthew25187. All rights reserved.                          #
#                                                                                          #
# Purpose: Generate source code for currency symbol lookups used when formatting and       #
#          parsing monetary values.                                                        #
# Source: Currency display names and symbols (JSON), Unicode Common Locale Data Repository.#
# https://github.com/unicode-org/cldr-json                                                 #
#                                                                                          #
# ISO 4217 does not define currency symbols; it defines alphabetic codes, numeric codes    #
# and minor units only. CLDR is the reference data used by .NET, ICU, Java and browsers,   #
# and is published under the Unicode licence, which permits redistribution with            #
# attribution.                                                                             #
############################################################################################
#Requires -Version 7.4

param (
    [Parameter(HelpMessage = 'URL from which the CLDR currency data can be downloaded.')]
    [string]    $SourceUrl,

    [Parameter(HelpMessage = 'Path to the file containing the CLDR currency data.')]
    [string]    $SourceFilePath,

    [Parameter(Mandatory, HelpMessage = 'Name of the type in the generated source code.')]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the type in the generated source code.')]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.')]
    [string]    $SourceCodeLanguage = 'CSharp',

    [Parameter(HelpMessage = 'CLDR locale from which the currency symbols are taken.')]
    [string]    $CldrLocale = 'en',

    [Parameter(HelpMessage = 'Version of CLDR from which the currency symbols are taken; recorded in the generated source code.')]
    [string]    $CldrVersion
)

function Import-List {
    param (
        [Parameter(Mandatory)]
        [string]    $FileLocation,

        [Parameter(Mandatory)]
        [string]    $Locale,

        [Parameter(Mandatory)]
        [ref]   $ListItems
    )

    Write-Debug '================================================================================'
    Write-Debug 'LOADING LIST FROM SOURCE'

    # Load file from source location.
    $filePath = $FileLocation
    if ([uri]::IsWellFormedUriString($FileLocation, [System.UriKind]::Absolute)) {
        Write-Verbose "Downloading list from $FileLocation."

        $fileFolder = [System.IO.Path]::GetTempPath()
        $filePath = Join-Path -Path $fileFolder -ChildPath (Split-Path $FileLocation -Leaf)
        Invoke-WebRequest -Uri $FileLocation -OutFile $filePath

        Write-Verbose "Downloaded list to $filePath."
    }

    $fileContent = Get-Content -Path $filePath -Raw -Encoding utf8 | ConvertFrom-Json

    $currencies = $fileContent.main.$Locale.numbers.currencies
    if ($null -eq $currencies) {
        Write-Error "No currency data found for locale $Locale."
        exit;
    }

    # Project the CLDR entries into a flat list of currency code, standard symbol and narrow symbol.
    [array]$currencySymbols = $currencies.PSObject.Properties | ForEach-Object {
        $standardSymbol = $_.Value.symbol
        $narrowSymbol = $_.Value.'symbol-alt-narrow'

        # CLDR falls back to the ISO 4217 code where no distinct symbol exists. Storing that would
        # duplicate data the currency code already carries, so it is discarded here; consumers fall
        # back to the code themselves.
        if ($standardSymbol -eq $_.Name) {
            $standardSymbol = $null
        }
        if ($narrowSymbol -eq $_.Name) {
            $narrowSymbol = $null
        }

        # A narrow symbol identical to the standard symbol carries no additional information.
        if ($narrowSymbol -eq $standardSymbol) {
            $narrowSymbol = $null
        }

        if ([string]::IsNullOrEmpty($standardSymbol) -and [string]::IsNullOrEmpty($narrowSymbol)) {
            return;
        }

        [pscustomobject]@{
            CurrencyCode   = $_.Name
            StandardSymbol = $standardSymbol
            NarrowSymbol   = $narrowSymbol
        }
    }

    $currencySymbolCount = $currencySymbols | Measure-Object | Select-Object -ExpandProperty Count
    Write-Verbose "Loaded $currencySymbolCount currency symbols for locale $Locale."

    $ListItems.Value = $currencySymbols
}

function Get-DictionaryFieldDeclaration {
    [OutputType([System.CodeDom.CodeSnippetTypeMember])]
    param (
        [Parameter(Mandatory)]
        [string]    $FieldName,

        [Parameter(Mandatory)]
        [string]    $KeyTypeName,

        [Parameter(Mandatory)]
        [string]    $ValueTypeName,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [array]     $Entries,

        [Parameter(Mandatory)]
        [string]    $Comment
    )

    # CodeDOM can express neither a collection initialiser nor the readonly modifier, and it escapes
    # 'string' as a type reference, so the whole declaration is emitted as a snippet member.
    $declarationBuilder = [System.Text.StringBuilder]::new()
    [void]$declarationBuilder.AppendLine("        // $Comment")
    [void]$declarationBuilder.AppendLine("        private static readonly System.Collections.Generic.Dictionary<$KeyTypeName, string> $FieldName = new System.Collections.Generic.Dictionary<$KeyTypeName, string>")
    [void]$declarationBuilder.AppendLine('        {')
    foreach ($entry in $Entries) {
        [void]$declarationBuilder.AppendLine("            { $($entry.Key), $($entry.Value) },")
    }
    [void]$declarationBuilder.Append('        };')

    return [System.CodeDom.CodeSnippetTypeMember]::new($declarationBuilder.ToString())
}

function ConvertTo-SourceCodeStringLiteral {
    param (
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Value
    )

    # Emit non-ASCII characters as escape sequences so the generated file is unambiguous regardless of
    # the encoding it is saved or viewed in. Several currency symbols rely on characters that are easy
    # to corrupt in transit, such as U+00A0, U+202F and the fullwidth yen sign U+FFE5.
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

function Out-SourceCode {
    param (
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [array]     $ListItems,

        [Parameter()]
        [string]    $CldrDataVersion,

        [Parameter()]
        [string]    $Locale,

        [Parameter()]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter()]
        [string]    $SourceCodeLanguage
    )

    $activity = "Generating $TypeName code DOM"
    Write-Progress -Activity $activity -PercentComplete -1

    Write-Debug '================================================================================'
    Write-Debug 'GENERATING SOURCE CODE FOR OUTPUT'

    # Determine which currency codes are defined by the generated ISO 4217 enums. The symbol data is
    # joined onto the enums rather than the other way around, so the generated source can never refer
    # to an enum member that does not exist.
    $currentCurrencyNames = [System.Enum]::GetNames([DataStandardizer.Money.Iso4217CurrencyCurrent])
    $historicCurrencyNames = [System.Enum]::GetNames([DataStandardizer.Money.Iso4217CurrencyHistoric])

    $currentStandardEntries = @()
    $currentNarrowEntries = @()
    $historicStandardEntries = @()
    $historicNarrowEntries = @()
    $skippedCurrencyCodes = @()

    foreach ($listItem in $ListItems | Sort-Object -Property CurrencyCode) {
        $isCurrent = $currentCurrencyNames -contains $listItem.CurrencyCode
        $isHistoric = $historicCurrencyNames -contains $listItem.CurrencyCode

        if (-not $isCurrent -and -not $isHistoric) {
            $skippedCurrencyCodes += $listItem.CurrencyCode
            continue
        }

        if ($isCurrent) {
            $enumMember = "DataStandardizer.Money.Iso4217CurrencyCurrent.$($listItem.CurrencyCode)"
            if (-not [string]::IsNullOrEmpty($listItem.StandardSymbol)) {
                $currentStandardEntries += [pscustomobject]@{ Key = $enumMember; Value = (ConvertTo-SourceCodeStringLiteral $listItem.StandardSymbol) }
            }
            if (-not [string]::IsNullOrEmpty($listItem.NarrowSymbol)) {
                $currentNarrowEntries += [pscustomobject]@{ Key = $enumMember; Value = (ConvertTo-SourceCodeStringLiteral $listItem.NarrowSymbol) }
            }
        }

        if ($isHistoric) {
            $enumMember = "DataStandardizer.Money.Iso4217CurrencyHistoric.$($listItem.CurrencyCode)"
            if (-not [string]::IsNullOrEmpty($listItem.StandardSymbol)) {
                $historicStandardEntries += [pscustomobject]@{ Key = $enumMember; Value = (ConvertTo-SourceCodeStringLiteral $listItem.StandardSymbol) }
            }
            if (-not [string]::IsNullOrEmpty($listItem.NarrowSymbol)) {
                $historicNarrowEntries += [pscustomobject]@{ Key = $enumMember; Value = (ConvertTo-SourceCodeStringLiteral $listItem.NarrowSymbol) }
            }
        }
    }

    if ($skippedCurrencyCodes.Count -gt 0) {
        Write-Verbose "Skipped $($skippedCurrencyCodes.Count) CLDR currency codes with no matching ISO 4217 enum member: $($skippedCurrencyCodes -join ', ')."
    }

    Write-Verbose "Current currencies: $($currentStandardEntries.Count) standard symbols, $($currentNarrowEntries.Count) narrow symbols."
    Write-Verbose "Historic currencies: $($historicStandardEntries.Count) standard symbols, $($historicNarrowEntries.Count) narrow symbols."

    # Declare namespace.
    $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
    $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Money')
    [void]$compileUnit.Namespaces.Add($namespace)

    # Declare type.
    $classType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
    $classType.IsClass = $true
    $classType.TypeAttributes = [System.Reflection.TypeAttributes]::NotPublic
    # Declared partial and static so the hand-written lookup logic can live alongside the generated data
    # without being overwritten when this generator is re-run.
    $classType.IsPartial = $true
    [void]$classType.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Currency Symbol Data'))
    [void]$classType.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))

    if (-not [string]::IsNullOrEmpty($TypeComment)) {
        $classType.Comments.AddRange(@(
                [System.CodeDom.CodeCommentStatement]::new('<summary>', $true),
                [System.CodeDom.CodeCommentStatement]::new($TypeComment, $true),
                [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)))
    }

    $remarks = @('<remarks>')
    if (-not [string]::IsNullOrEmpty($CldrDataVersion)) {
        $remarks += "Based on Unicode CLDR $CldrDataVersion currency data for the $Locale locale."
    }
    else {
        $remarks += "Based on Unicode CLDR currency data for the $Locale locale."
    }
    $remarks += 'ISO 4217 does not define currency symbols, so this data is sourced from CLDR rather than'
    $remarks += 'from the currency code standard itself.'
    $remarks += '</remarks>'
    foreach ($remark in $remarks) {
        [void]$classType.Comments.Add([System.CodeDom.CodeCommentStatement]::new($remark, $true))
    }

    [void]$namespace.Types.Add($classType)

    # Declare symbol lookups.
    [void]$classType.Members.Add((Get-DictionaryFieldDeclaration `
                -FieldName 'CurrentStandardSymbols' `
                -KeyTypeName 'DataStandardizer.Money.Iso4217CurrencyCurrent' `
                -ValueTypeName 'string' `
                -Entries $currentStandardEntries `
                -Comment 'Standard currency symbols; unambiguous in context, and the form used by default.'))

    [void]$classType.Members.Add((Get-DictionaryFieldDeclaration `
                -FieldName 'CurrentNarrowSymbols' `
                -KeyTypeName 'DataStandardizer.Money.Iso4217CurrencyCurrent' `
                -ValueTypeName 'string' `
                -Entries $currentNarrowEntries `
                -Comment 'Narrow currency symbols; the shortest recognisable form, which may be shared by several currencies.'))

    [void]$classType.Members.Add((Get-DictionaryFieldDeclaration `
                -FieldName 'HistoricStandardSymbols' `
                -KeyTypeName 'DataStandardizer.Money.Iso4217CurrencyHistoric' `
                -ValueTypeName 'string' `
                -Entries $historicStandardEntries `
                -Comment 'Standard currency symbols for historic currencies.'))

    [void]$classType.Members.Add((Get-DictionaryFieldDeclaration `
                -FieldName 'HistoricNarrowSymbols' `
                -KeyTypeName 'DataStandardizer.Money.Iso4217CurrencyHistoric' `
                -ValueTypeName 'string' `
                -Entries $historicNarrowEntries `
                -Comment 'Narrow currency symbols for historic currencies.'))

    Write-Progress -Completed

    # Generate source code.
    $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($SourceCodeLanguage)

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

# Validate parameters.
if ($PSBoundParameters.ContainsKey('SourceUrl') -and $PSBoundParameters.ContainsKey('SourceFilePath')) {
    Write-Error 'Parameters -SourceUrl and -SourceFilePath are mutually exclusive; specify either -SourceUrl or -SourceFilePath.'
    exit;
}

if ((-not $PSBoundParameters.ContainsKey('SourceUrl')) -and (-not $PSBoundParameters.ContainsKey('SourceFilePath'))) {
    Write-Error 'Either -SourceUrl or -SourceFilePath are required.'
    exit;
}

[string]    $SourceFileLocation

if (-not [string]::IsNullOrEmpty($SourceUrl)) {
    if ([uri]::IsWellFormedUriString($SourceUrl, [System.UriKind]::Absolute)) {
        $SourceFileLocation = $SourceUrl
    }
    else {
        Write-Error 'Expected source URL.'
        exit;
    }
}

if (-not [string]::IsNullOrEmpty($SourceFilePath)) {
    if (Test-Path -IsValid $SourceFilePath) {
        $SourceFileLocation = $SourceFilePath
    }
    else {
        Write-Error 'Expected source file path.'
        exit;
    }
}

if ([string]::IsNullOrEmpty($SourceFileLocation)) {
    Write-Error 'Unknown source.'
    exit;
}

Write-Debug '================================================================================'
Write-Debug 'PARAMETERS'
Write-Debug "Source: $SourceFileLocation"
Write-Debug "CLDR locale: $CldrLocale"
Write-Debug "CLDR version: $CldrVersion"
Write-Debug "Generated type name: $SourceCodeTypeName"
Write-Debug "Generated type comment: $SourceCodeTypeComment"
Write-Debug "Generated language: $SourceCodeLanguage"

# The ISO 4217 enums must be loaded so the CLDR data can be joined onto them.
$moneyAssemblyPath = Join-Path -Path $PSScriptRoot -ChildPath '..\src\DataStandardizer.Money\bin\Debug\net10.0\DataStandardizer.Money.dll' -Resolve -ErrorAction SilentlyContinue
if ([string]::IsNullOrEmpty($moneyAssemblyPath)) {
    Write-Error 'Unable to locate DataStandardizer.Money.dll. Build the project before running this generator.'
    exit;
}
Add-Type -Path $moneyAssemblyPath

[array]$listItems = @()
Import-List -FileLocation $SourceFileLocation -Locale $CldrLocale -ListItems ([ref]$listItems)

Out-SourceCode `
    -ListItems $listItems `
    -CldrDataVersion $CldrVersion `
    -Locale $CldrLocale `
    -TypeName $SourceCodeTypeName `
    -TypeComment $SourceCodeTypeComment `
    -SourceCodeLanguage $SourceCodeLanguage
