############################################################################################
# Title: ISO 4217 Current Currency & Funds Source Code Generator                           #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.                          #
#                                                                                          #
# Purpose: Generate source code for implementing the ISO 4217 standard.                    #
# Source: Current Currency & Funds list (XML), SIX Group.                                  #
# https://www.six-group.com/en/products-services/financial-information/data-standards.html #
############################################################################################
#Requires -Version 7.4

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
    [string]    $SourceCodeLanguage = 'CSharp',

    [Parameter(HelpMessage = 'Include currencies that are marked as a funds code.')]
    [switch]    $IncludeFundsCodes
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
    $currencyCountries = Select-Xml -Xml $fileContent -XPath '//CcyNtry' 
    | Select-Object -ExpandProperty Node 
    | Where-Object { -not [string]::IsNullOrEmpty($_.Ccy) } 
    | ForEach-Object { 
        $countryName = $_.CtryNm
        $currencyName = ($_.CcyNm -is [System.Xml.XmlElement])?$_.CcyNm.InnerText:$_.CcyNm
        $currencyCode = $_.Ccy
        $currencyNumber = $_.CcyNbr -as [ushort]
        $currencyMinorUnits = $_.CcyMnrUnts

        Write-Verbose "List contains currency $currencyCode ($currencyName) for $countryName."

        [PSCustomObject]@{
            CtryNm           = $countryName
            CcyNm            = $currencyName
            Ccy              = $currencyCode
            CcyNbr           = $currencyNumber
            CcyMnrUnts       = $currencyMinorUnits
            IsFundsCode      = ((($_.CcyNm.Attributes?.Count ?? 0) -gt 0) -and $_.CcyNm.Attributes['IsFund']?.Value -eq [bool]::TrueString)
            IsSpecialPurpose = $currencyCode.StartsWith('X')
        } 
    }
    Write-Information "Found $($currencyCountries.Count) currency countries."

    $ListItems.Value = $currencyCountries
}

function Out-SourceCode {
    param (
        [Parameter(ValueFromPipeline)]
        [psobject[]]    $InputObject,

        [Parameter()]
        [System.Nullable[System.DateOnly]]   $ListPublishedDate,

        [Parameter()]
        [int]   $CodeCount,

        [Parameter()]
        [System.Collections.Generic.IDictionary[string, int]]    $CodeCountryCounts,

        [Parameter()]
        [bool]  $IncludeFundsCodes,

        [Parameter()]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter()]
        [string]    $SourceCodeLanguage
    )

    begin {
        $activity = "Generating $TypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        Write-Debug '================================================================================'
        Write-Debug 'GENERATING SOURCE CODE FOR OUTPUT'

        # Declare namespace.
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.ISO4217')
        [void]$compileUnit.Namespaces.Add($namespace)

        # Declare type.
        $enumType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
        $enumType.BaseTypes.Add([UInt16])
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

        $codesProcessed = 0
        [string[]]$currencyCountryNames = @()
    }

    process {
        if (-not $IncludeFundsCodes -and $_.IsFundsCode) {
            return;
        }

        $status = "Evaluating currency code $($_.Ccy)"
        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding currency field' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        $currencyCountryNames += $_.CtryNm
        $currencyCountryNameCount = $currencyCountryNames | Measure-Object | Select-Object -ExpandProperty Count
        Write-Debug "There are now $currencyCountryNameCount of $($CodeCountryCounts[$_.Ccy]) country names pending for currency $($_.Ccy)."
        if ($currencyCountryNameCount -lt $CodeCountryCounts[$_.Ccy]) {
            return;
        }

        # Declare field.
        $enumField = [System.CodeDom.CodeMemberField]::new([ushort], $_.Ccy)
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new($_.CcyNbr)
        [void]$enumField.UserData.Add('Ccy', $_.Ccy)

        $codeAttributeArguments = @([System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.CcyNm)))
        if (-not [string]::IsNullOrWhiteSpace($_.CcyMnrUnts) -and $_.CcyMnrUnts -ne 'N.A.') {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.CcyMnrUnts -as [byte]))
        }
        if ($_.IsFundsCode) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('IsFundsCode', [System.CodeDom.CodePrimitiveExpression]::new($_.IsFundsCode))
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO4217.Iso4217CurrencyCodeAttribute', $codeAttributeArguments)
        [void]$enumField.CustomAttributes.Add($codeAttribute)

        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($_.CcyNm, $true)
        $enumFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $enumField.Comments.AddRange(@($enumFieldOpenSummaryComment, $enumFieldSummaryContentComment, $enumFieldCloseSummaryComment))

        if ($currencyCountryNameCount -gt 0 -and $null -ne $_.CcyMnrUnts) {
            [System.CodeDom.CodeCommentStatement[]]$enumFieldRemarksComments = @()
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('Used by:', $true)
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('<list type="bullet">', $true)
            foreach ($currencyCountryName in $currencyCountryNames) {
                $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t<item>", $true)
                $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t`t<description>$currencyCountryName</description>", $true)
                $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new("`t</item>", $true)
            }
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('</list>', $true)
            $enumFieldRemarksComments += [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
            $enumField.Comments.AddRange($enumFieldRemarksComments)
        }

        [void]$enumType.Members.Add($enumField)

        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeCount) * 100)

        $currencyCountryNames = @()
    }

    end {
        $enumType.Members | Where-Object { -not $_.UserData['Ccy'].StartsWith('X') } | Select-Object -First 1 | ForEach-Object {
            [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields: Active currency codes'))
        }
        $enumType.Members | Where-Object { -not $_.UserData['Ccy'].StartsWith('X') } | Select-Object -Last 1 | ForEach-Object {
            [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        }

        $enumType.Members | Where-Object { $_.UserData['Ccy'].StartsWith('X') } | Select-Object -First 1 | ForEach-Object {
            [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields: Special purpose currency codes'))
        }
        $enumType.Members | Where-Object { $_.UserData['Ccy'].StartsWith('X') } | Select-Object -Last 1 | ForEach-Object {
            [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        }

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
Write-Debug "Include funds codes: $($PSBoundParameters.ContainsKey('IncludeFundsCodes'))"

# Process input.
try {
    Set-PSDebug -Trace 0
    
    [System.Nullable[System.DateOnly]]$listPublishedDate = $null
    [array]$listItems = @()
    Import-List $SourceFileLocation ([ref]$listPublishedDate) ([ref]$listItems)

    $currencyCodeCountryCount = [System.Collections.Generic.Dictionary[string, int]]::new()
    foreach ($listItem in $listItems) {
        if (-not $PSBoundParameters.ContainsKey('IncludeFundsCodes') -and $listItem.IsFundsCode) {
            Write-Debug "Skipping count for funds code $($listItem.Ccy)."
            continue;
        }
    
        if ($currencyCodeCountryCount.ContainsKey($listItem.Ccy)) {
            $currencyCodeCountryCount[$listItem.Ccy] = $currencyCodeCountryCount[$listItem.Ccy] + 1
        }
        else {
            $currencyCodeCountryCount.Add($listItem.Ccy, 1)
        }
        Write-Debug "Found $($currencyCodeCountryCount[$listItem.Ccy]) countries for currency code $($listItem.Ccy)."
    }

    $codeCount = $listItems | Where-Object { $IncludeFundsCodes -or -not $_.IsFundsCode } | Select-Object -Property Ccy -Unique | Measure-Object | Select-Object -ExpandProperty Count
    $listItems | Sort-Object IsSpecialPurpose, Ccy, CtryNm | Out-SourceCode -ListPublishedDate $listPublishedDate -CodeCount $codeCount -CodeCountryCounts $currencyCodeCountryCount -IncludeFundsCodes $PSBoundParameters.ContainsKey('IncludeFundsCodes') -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -SourceCodeLanguage $SourceCodeLanguage
}
finally {
    Set-PSDebug -Off
}