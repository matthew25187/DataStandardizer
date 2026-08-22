#########################################################################################################
# Title: UN M49 Source Code Generator                                                                   #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.                                       #
#                                                                                                       #
# Purpose: Generate source code for implementing the UN M49 standard.                                   #
# Source: Standard country or area codes for statistical use (M49), United Nations Statistics Division. #
# https://unstats.un.org/unsd/methodology/m49/overview/                                                 #
#########################################################################################################
#Requires -Version 7.4

param (
    [Parameter(Mandatory, HelpMessage = 'Path to the file containing official English-language codes.')]
    [string]    $EnglishCodesFilePath,

    [Parameter(HelpMessage = 'Path to the file containing official Chinese-language codes.')]
    [string]    $ChineseCodesFilePath,

    [Parameter(HelpMessage = 'Path to the file containing official Russian-language codes.')]
    [string]    $RussianCodesFilePath,

    [Parameter(HelpMessage = 'Path to the file containing official French-language codes.')]
    [string]    $FrenchCodesFilePath,

    [Parameter(HelpMessage = 'Path to the file containing official Spanish-language codes.')]
    [string]    $SpanishCodesFilePath,

    [Parameter(HelpMessage = 'Path to the file containing official Arabic-language codes.')]
    [string]    $ArabicCodesFilePath,

    [Parameter(Mandatory, HelpMessage = 'Name of the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeLanguage = 'CSharp'
)

function Out-SourceCode {
    param (
        [Parameter(ValueFromPipeline)]
        [psobject[]]    $InputObject,

        [Parameter()]
        [int]   $CodeCount,

        [Parameter()]
        [psobject[]]    $ChineseCodesSet,

        [Parameter()]
        [psobject[]]    $RussianCodesSet,

        [Parameter()]
        [psobject[]]    $FrenchCodesSet,

        [Parameter()]
        [psobject[]]    $SpanishCodesSet,

        [Parameter()]
        [psobject[]]    $ArabicCodesSet,

        [Parameter()]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter()]
        [string]    $GenerateLanguage
    )
    
    begin {
        $activity = "Generating $TypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        $codesProcessed = 0
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()

        # Declare namespace.
        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Geography')
        [void]$compileUnit.Namespaces.Add($namespace)

        # Declare type.
        Write-Progress -Activity $activity -CurrentOperation 'Declaring enum type' -PercentComplete -1
    
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

        [void]$namespace.Types.Add($enumType)
    }

    process {
        $status = "Evaluating code $($_.'ISO-alpha2 Code')"

        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding code field' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        $m49Code = $_.'M49 Code'
        $chineseCodeItem = $ChineseCodesSet | Where-Object { ($_.'M49 Code' -as [ushort]) -eq ($m49Code -as [ushort]) }
        $russianCodeItem = $RussianCodesSet | Where-Object { ($_.'M49 Code' -as [ushort]) -eq ($m49Code -as [ushort]) }
        $frenchCodeItem = $FrenchCodesSet | Where-Object { ($_.'M49 Code' -as [ushort]) -eq ($m49Code -as [ushort]) }
        $spanishCodeItem = $SpanishCodesSet | Where-Object { ($_.'M49 Code' -as [ushort]) -eq ($m49Code -as [ushort]) }
        $arabicCodeItem = $ArabicCodesSet | Where-Object { ($_.'M49 Code' -as [ushort]) -eq ($m49Code -as [ushort]) }

        if ((($ChineseCodesSet | Measure-Object | Select-Object -ExpandProperty Count) -gt 0) -and $null -eq $chineseCodeItem) {
            Write-Warning "Chinese variant for M49 code $m49Code not found."
        }
        if ((($RussianCodesSet | Measure-Object | Select-Object -ExpandProperty Count) -gt 0) -and $null -eq $russianCodeItem) {
            Write-Warning "Russian variant for M49 code $m49Code not found."
        }
        if ((($FrenchCodesSet | Measure-Object | Select-Object -ExpandProperty Count) -gt 0) -and $null -eq $frenchCodeItem) {
            Write-Warning "French variant for M49 code $m49Code not found."
        }
        if ((($SpanishCodesSet | Measure-Object | Select-Object -ExpandProperty Count) -gt 0) -and $null -eq $spanishCodeItem) {
            Write-Warning "Spanish variant for M49 code $m49Code not found."
        }
        if ((($ArabicCodesSet | Measure-Object | Select-Object -ExpandProperty Count) -gt 0) -and $null -eq $arabicCodeItem) {
            Write-Warning "Arabic variant for M49 code $m49Code not found."
        }

        # Declare field.
        $enumField = [System.CodeDom.CodeMemberField]::new([ushort], $_.'ISO-alpha2 Code')
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new($m49Code -as [ushort])

        # Add code attribute.
        $codeAttributeArguments = @([System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.'Global Code' -as [ushort])))
        if (-not [string]::IsNullOrWhiteSpace($_.'Region Code')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.'Region Code' -as [ushort]))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Sub-region Code')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.'Sub-region Code' -as [ushort]))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Intermediate Region Code')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.'Intermediate Region Code' -as [ushort]))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Global Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('EnglishGlobalName', [System.CodeDom.CodePrimitiveExpression]::new($_.'Global Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($chineseCodeItem.'Global Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ChineseGlobalName', [System.CodeDom.CodePrimitiveExpression]::new($chineseCodeItem.'Global Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($russianCodeItem.'Global Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('RussianGlobalName', [System.CodeDom.CodePrimitiveExpression]::new($russianCodeItem.'Global Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($frenchCodeItem.'Global Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('FrenchGlobalName', [System.CodeDom.CodePrimitiveExpression]::new($frenchCodeItem.'Global Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($spanishCodeItem.'Global Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('SpanishGlobalName', [System.CodeDom.CodePrimitiveExpression]::new($spanishCodeItem.'Global Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($arabicCodeItem.'Global Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ArabicGlobalName', [System.CodeDom.CodePrimitiveExpression]::new($arabicCodeItem.'Global Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('EnglishRegionName', [System.CodeDom.CodePrimitiveExpression]::new($_.'Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($chineseCodeItem.'Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ChineseRegionName', [System.CodeDom.CodePrimitiveExpression]::new($chineseCodeItem.'Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($russianCodeItem.'Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('RussianRegionName', [System.CodeDom.CodePrimitiveExpression]::new($russianCodeItem.'Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($frenchCodeItem.'Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('FrenchRegionName', [System.CodeDom.CodePrimitiveExpression]::new($frenchCodeItem.'Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($spanishCodeItem.'Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('SpanishRegionName', [System.CodeDom.CodePrimitiveExpression]::new($spanishCodeItem.'Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($arabicCodeItem.'Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ArabicRegionName', [System.CodeDom.CodePrimitiveExpression]::new($arabicCodeItem.'Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Sub-region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('EnglishSubRegionName', [System.CodeDom.CodePrimitiveExpression]::new($_.'Sub-region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($chineseCodeItem.'Sub-region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ChineseSubRegionName', [System.CodeDom.CodePrimitiveExpression]::new($chineseCodeItem.'Sub-region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($russianCodeItem.'Sub-region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('RussianSubRegionName', [System.CodeDom.CodePrimitiveExpression]::new($russianCodeItem.'Sub-region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($frenchCodeItem.'Sub-region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('FrenchSubRegionName', [System.CodeDom.CodePrimitiveExpression]::new($frenchCodeItem.'Sub-region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($spanishCodeItem.'Sub-region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('SpanishSubRegionName', [System.CodeDom.CodePrimitiveExpression]::new($spanishCodeItem.'Sub-region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($arabicCodeItem.'Sub-region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ArabicSubRegionName', [System.CodeDom.CodePrimitiveExpression]::new($arabicCodeItem.'Sub-region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Intermediate Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('EnglishIntermediateRegionName', [System.CodeDom.CodePrimitiveExpression]::new($_.'Intermediate Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($chineseCodeItem.'Intermediate Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ChineseIntermediateRegionName', [System.CodeDom.CodePrimitiveExpression]::new($chineseCodeItem.'Intermediate Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($russianCodeItem.'Intermediate Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('RussianIntermediateRegionName', [System.CodeDom.CodePrimitiveExpression]::new($russianCodeItem.'Intermediate Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($frenchCodeItem.'Intermediate Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('FrenchIntermediateRegionName', [System.CodeDom.CodePrimitiveExpression]::new($frenchCodeItem.'Intermediate Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($spanishCodeItem.'Intermediate Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('SpanishIntermediateRegionName', [System.CodeDom.CodePrimitiveExpression]::new($spanishCodeItem.'Intermediate Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($arabicCodeItem.'Intermediate Region Name')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ArabicIntermediateRegionName', [System.CodeDom.CodePrimitiveExpression]::new($arabicCodeItem.'Intermediate Region Name'))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'Country or Area')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('EnglishCountryOrAreaName', [System.CodeDom.CodePrimitiveExpression]::new($_.'Country or Area'))
        }
        if (-not [string]::IsNullOrWhiteSpace($chineseCodeItem.'Country or Area')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ChineseCountryOrAreaName', [System.CodeDom.CodePrimitiveExpression]::new($chineseCodeItem.'Country or Area'))
        }
        if (-not [string]::IsNullOrWhiteSpace($russianCodeItem.'Country or Area')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('RussianCountryOrAreaName', [System.CodeDom.CodePrimitiveExpression]::new($russianCodeItem.'Country or Area'))
        }
        if (-not [string]::IsNullOrWhiteSpace($frenchCodeItem.'Country or Area')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('FrenchCountryOrAreaName', [System.CodeDom.CodePrimitiveExpression]::new($frenchCodeItem.'Country or Area'))
        }
        if (-not [string]::IsNullOrWhiteSpace($spanishCodeItem.'Country or Area')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('SpanishCountryOrAreaName', [System.CodeDom.CodePrimitiveExpression]::new($spanishCodeItem.'Country or Area'))
        }
        if (-not [string]::IsNullOrWhiteSpace($arabicCodeItem.'Country or Area')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('ArabicCountryOrAreaName', [System.CodeDom.CodePrimitiveExpression]::new($arabicCodeItem.'Country or Area'))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'ISO-alpha2 Code')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Iso3166Part1Alpha2Code', [System.CodeDom.CodePrimitiveExpression]::new($_.'ISO-alpha2 Code'))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.'ISO-alpha3 Code')) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Iso3166Part1Alpha3Code', [System.CodeDom.CodePrimitiveExpression]::new($_.'ISO-alpha3 Code'))
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.Geography.UnM49AreaCodeAttribute', $codeAttributeArguments)
        [void]$enumField.CustomAttributes.Add($codeAttribute)
        
        # Add summary comment.
        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($_.'Country or Area', $true)
        $enumFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $enumField.Comments.AddRange(@($enumFieldOpenSummaryComment, $enumFieldSummaryContentComment, $enumFieldCloseSummaryComment))

        [void]$enumType.Members.Add($enumField)

        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeCount) * 100)
    }

    end {
        Write-Progress -Completed

        # Output source code.
        $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($GenerateLanguage)

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

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    if ($PSBoundParameters.ContainsKey('EnglishCodesFilePath')) {
        if (Test-Path $EnglishCodesFilePath -PathType Leaf) {
            $englishCodesSet = Import-Csv $EnglishCodesFilePath -Delimiter ';'
        }
        else {
            Write-Error "Source file '$EnglishCodesFilePath' not found."
            exit;
        }
    }

    if ($PSBoundParameters.ContainsKey('ChineseCodesFilePath')) {
        if (Test-Path $ChineseCodesFilePath -PathType Leaf) {
            $chineseCodesSet = Import-Csv $ChineseCodesFilePath
        }
        else {
            Write-Error "Source file '$ChineseCodesFilePath' not found."
            exit;
        }
    }

    if ($PSBoundParameters.ContainsKey('RussianCodesFilePath')) {
        if (Test-Path $RussianCodesFilePath -PathType Leaf) {
            $russianCodesSet = Import-Csv $RussianCodesFilePath
        }
        else {
            Write-Error "Source file '$RussianCodesFilePath' not found."
            exit;
        }
    }

    if ($PSBoundParameters.ContainsKey('FrenchCodesFilePath')) {
        if (Test-Path $FrenchCodesFilePath -PathType Leaf) {
            $frenchCodesSet = Import-Csv $FrenchCodesFilePath
        }
        else {
            Write-Error "Source file '$FrenchCodesFilePath' not found."
            exit;
        }
    }

    if ($PSBoundParameters.ContainsKey('SpanishCodesFilePath')) {
        if (Test-Path $SpanishCodesFilePath -PathType Leaf) {
            $spanishCodesSet = Import-Csv $SpanishCodesFilePath
        }
        else {
            Write-Error "Source file '$SpanishCodesFilePath' not found."
            exit;
        }
    }

    if ($PSBoundParameters.ContainsKey('ArabicCodesFilePath')) {
        if (Test-Path $ArabicCodesFilePath -PathType Leaf) {
            $arabicCodesSet = Import-Csv $ArabicCodesFilePath
        }
        else {
            Write-Error "Source file '$ArabicCodesFilePath' not found."
            exit;
        }
    }

    $codeCount = $englishCodesSet | Measure-Object | Select-Object -ExpandProperty Count
    $englishCodesSet | Out-SourceCode -CodeCount $codeCount -ChineseCodesSet ($chineseCodesSet ?? @()) -RussianCodesSet ($russianCodesSet ?? @()) -FrenchCodesSet ($frenchCodesSet ?? @()) -SpanishCodesSet ($spanishCodesSet ?? @()) -ArabicCodesSet ($arabicCodesSet ?? @()) -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage
}
finally {
    Set-PSDebug -Off
}