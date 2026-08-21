############################################################################################
# Title: ISO 4217 Historic Denominations (Currencies & Funds) Source Code Generator        #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.                          #
#                                                                                          #
# Purpose: Generate source code for implementing the ISO 4217 standard.                    #
# Source: Historic Denominations (Currencies & Funds) (XML), SIX Group.                    #
# https://www.six-group.com/en/products-services/financial-information/data-standards.html #
############################################################################################
#Requires -Version 7.1

param (
    [Parameter(HelpMessage = 'URL from which the ISO 4217 codes list can be downloaded.')]
    [string]    $SourceUrl,
 
    [Parameter(HelpMessage = 'Path to the file containing the ISO 4217 codes.')]
    [string]    $SourceFilePath,

    [Parameter(Mandatory, HelpMessage = 'Name of the enum type in the generated source code.')]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the enum type in the generated source code.')]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.')]
    [string]    $SourceCodeLanguage = 'CSharp'
)

function Import-List {
    param (
        [Parameter(Mandatory)]
        [string]    $FileLocation,
    
        [Parameter()]
        [ref] [System.Nullable[System.DateOnly]]  $ListPublishedDate,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [ref] [array] $ListItems
    )
        
    Write-Debug '================================================================================'
    Write-Debug 'LOADING LIST FROM SOURCE'

    # Load file from source location.
    $filePath = $FileLocation
    if ([uri]::IsWellFormedUriString($FileLocation, [System.UriKind]::Absolute)) {
        Write-Verbose "Downloading list from $FileLocation."

        $fileFolder = [System.IO.Path]::GetTempPath()
        Invoke-WebRequest -Uri $FileLocation -OutFile $fileFolder

        $filePath = Join-Path -Path $fileFolder -ChildPath (Split-Path $FileLocation -Leaf)
        Write-Verbose "Downloaded list to $filePath."
    }

    [xml]$fileContent = Get-Content -Path $filePath

    # Get published date from root node.
    $rootNode = Select-Xml -Xml $fileContent -XPath '/ISO_4217'
    $publishedDate = [System.DateOnly]::MinValue
    if ([System.DateOnly]::TryParseExact($rootNode.Node.Attributes['Pblshd'].Value, 'yyyy-MM-dd', [ref]$publishedDate)) {
        Write-Verbose "List was published on $publishedDate."

        $ListPublishedDate.Value = $publishedDate
    }

    # Get currency country items.
    $historicCurrencyCountries = Select-Xml -Xml $fileContent -XPath '//HstrcCcyNtry'
    | Select-Object -ExpandProperty Node
    | ForEach-Object {
        $countryName = $_.CtryNm
        $currencyName = ($_.CcyNm -is [System.Xml.XmlElement])?$_.CcyNm.InnerText:$_.CcyNm
        $currencyCode = $_.Ccy
        $currencyNumber = (-not [string]::IsNullOrEmpty($_.CcyNbr))? ($_.CcyNbr -as [short]):$null
        $withdrawalDate = $_.WthdrwlDt
        $fundsCode = (($_.CcyNm.Attributes?.Count ?? 0) -gt 0)?$_.CcyNm.Attributes['IsFund']?.Value:$null

        Write-Verbose "List contains currency $currencyCode ($currencyName) for $countryName."

        [PSCustomObject]@{
            CtryNm    = $countryName
            CcyNm     = $currencyName
            Ccy       = $currencyCode
            CcyNbr    = $currencyNumber
            WthdrwlDt = $withdrawalDate
            IsFund    = $fundsCode
        }
    }
    Write-Information "Found $($historicCurrencyCountries.Count) currency countries."

    $ListItems.Value = $historicCurrencyCountries
}

function Out-SourceCode {
    param (
        [Parameter()]
        [System.Nullable[System.DateOnly]]  $ListPublishedDate,
    
        [Parameter(Mandatory)]
        [pscustomobject[]]  $ListItems,

        [Parameter()]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter()]
        [string]    $SourceCodeLanguage
    )
        
    $compileUnit = [System.CodeDom.CodeCompileUnit]::new()

    Write-Debug '================================================================================'
    Write-Debug 'GENERATING SOURCE CODE FOR OUTPUT'

    # Declare namespace.
    $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Money')
    [void]$compileUnit.Namespaces.Add($namespace)

    # Declare type.
    $enumType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
    $enumType.BaseTypes.Add([short])
    $enumType.IsEnum = $true
    $enumType.TypeAttributes = [System.Reflection.TypeAttributes]::Public

    if (-not [string]::IsNullOrEmpty($TypeComment)) {
        $enumTypeOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumTypeSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($TypeComment, $true)
        $enumTypeCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $enumType.Comments.AddRange(@($enumTypeOpenSummaryComment, $enumTypeSummaryContentComment, $enumTypeCloseSummaryComment))
    }

    if ($null -ne $ListPublishedDate) {
        $enumTypeOpenRemarksComment = [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
        $enumTypeRemarksContentComment = [System.CodeDom.CodeCommentStatement]::new("Based on official ISO 4217 currency codes as at $($ListPublishedDate.ToString('yyyy-MM-dd')).", $true)
        $enumTypeCloseRemarksComment = [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
        $enumType.Comments.AddRange(@($enumTypeOpenRemarksComment, $enumTypeRemarksContentComment, $enumTypeCloseRemarksComment))
    }

    [void]$namespace.Types.Add($enumType)

    # Declare fields.
    $enumFieldFunc = {
        $currencyAlphabeticCode = $_
        $currencyCountryItems = $ListItems | Where-Object -Property Ccy -EQ $currencyAlphabeticCode
        $currencyNumericCode = $currencyCountryItems | Select-Object -First 1 -ExpandProperty CcyNbr
        $currencyName = $currencyCountryItems | Select-Object -First 1 -ExpandProperty CcyNm
        $currencyCountryNames = $currencyCountryItems | Select-Object -ExpandProperty CtryNm
        $currencyWithdrawalDate = $currencyCountryItems | Select-Object -First 1 -ExpandProperty WthdrwlDt

        Write-Verbose "Generating source code for currency code $currencyAlphabeticCode."
        Write-Debug "Numeric code ($currencyAlphabeticCode): $currencyNumericCode"
        Write-Debug "Name ($currencyAlphabeticCode): $currencyName"
        Write-Debug "Countries ($currencyAlphabeticCode): $($currencyCountryNames -join '; ')"

        # Add currency code to source code generator.
        $enumField = [System.CodeDom.CodeMemberField]::new([short], $currencyAlphabeticCode)
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new($currencyNumericCode ?? -1)

        $codeAttributeArgument = [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($currencyName))
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.Money.Iso4217CurrencyCodeAttribute', @($codeAttributeArgument))
        [void]$enumField.CustomAttributes.Add($codeAttribute)
    
        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($currencyName, $true)
        $enumFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $enumField.Comments.AddRange(@($enumFieldOpenSummaryComment, $enumFieldSummaryContentComment, $enumFieldCloseSummaryComment))

        [System.CodeDom.CodeCommentStatement[]]$enumFieldRemarksComments = @()
        $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
        if ($currencyCountryNames.Count -gt 0 -and $null -ne $currencyNumericCode) {
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t<para>", $true)
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`tUsed by:", $true)
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`t<list type=""bullet"">", $true)
            foreach ($currencyCountryName in $currencyCountryNames) {
                $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`t`t<item>", $true)
                $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`t`t`t<description>$currencyCountryName</description>", $true)
                $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`t`t</item>", $true)
            }
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`t</list>", $true)
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t</para>", $true)
        }
        $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t<para>", $true)
        $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`tWithdrawn: $currencyWithdrawalDate", $true)
        $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t</para>", $true)
        $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
        $enumField.Comments.AddRange($enumFieldRemarksComments)

        [void]$enumType.Members.Add($enumField)
    }

    # Process historical currency codes.
    $historicalCurrencyCodes = $ListItems | Where-Object -Property CcyNbr -NE $null | Select-Object -ExpandProperty Ccy -Unique | Sort-Object
    Write-Information "Found $($historicalCurrencyCodes.Count) currency codes."
    $historicalCurrencyCodes | ForEach-Object $enumFieldFunc

    [string]$firstHistoricalCurrencyCode = $historicalCurrencyCodes | Select-Object -First 1
    $firstHistoricalEnumField = $enumType.Members | Where-Object -Property Name -EQ $firstHistoricalCurrencyCode
    if ($null -ne $firstHistoricalEnumField) {
        [void]$firstHistoricalEnumField.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields: Historical currency codes'))
    }
    [string]$lastHistoricalCurrencyCode = $historicalCurrencyCodes | Select-Object -Last 1
    $lastHistoricalEnumField = $enumType.Members | Where-Object -Property Name -EQ $lastHistoricalCurrencyCode
    if ($null -ne $lastHistoricalEnumField) {
        [void]$lastHistoricalEnumField.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
    }

    # Process special purpose currency codes.
    $specialPurposeCurrencyCodes = $ListItems | Where-Object -Property CcyNbr -EQ $null | Select-Object -ExpandProperty Ccy -Unique | Sort-Object
    Write-Information "Found $($specialPurposeCurrencyCodes.Count) special purpose currency codes."
    $specialPurposeCurrencyCodes | ForEach-Object $enumFieldFunc

    [string]$firstSpecialPurposeCurrencyCode = $specialPurposeCurrencyCodes | Select-Object -First 1
    $firstSpecialPurposeEnumField = $enumType.Members | Where-Object -Property Name -EQ $firstSpecialPurposeCurrencyCode
    if ($null -ne $firstSpecialPurposeEnumField) {
        [void]$firstSpecialPurposeEnumField.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields: Special purpose currency codes'))
    }
    [string]$lastSpecialPurposeCurrencyCode = $specialPurposeCurrencyCodes | Select-Object -Last 1
    $lastSpecialPurposeEnumField = $enumType.Members | Where-Object -Property Name -EQ $lastSpecialPurposeCurrencyCode
    if ($null -ne $lastSpecialPurposeEnumField) {
        [void]$lastSpecialPurposeEnumField.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
    }

    # Generate source code.
    $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($SourceCodeLanguage)

    $options = [System.CodeDom.Compiler.CodeGeneratorOptions]::new()
    $options.BlankLinesBetweenMembers = $true
    $options.BracingStyle = 'C'

    $sourceCodeBuilder = [System.Text.StringBuilder]::new()
    $writer = [System.IO.StringWriter]::new($sourceCodeBuilder)
    try {
        $provider.GenerateCodeFromCompileUnit($compileUnit, $writer, $options)
    }
    finally {
        $writer.Close()
    }

    Write-Output $sourceCodeBuilder.ToString()
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
Write-Debug "Generated type name: $SourceCodeTypeName"
Write-Debug "Generated type comment: $SourceCodeTypeComment"
Write-Debug "Generated language: $SourceCodeLanguage"

# Process input.
[System.Nullable[System.DateOnly]]$listPublishedDate = $null
[array]$listItems = @()
Import-List $SourceFileLocation ([ref]$listPublishedDate) ([ref]$listItems)

Out-SourceCode $listPublishedDate $listItems $SourceCodeTypeName $SourceCodeTypeComment $SourceCodeLanguage