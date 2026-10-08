#########################################################################################################
# Title: UN M49 Source Code Generator                                                                   #
# Copyright: Copyright © 2026, Matthew25187. All rights reserved.                                       #
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

function Expand-AreaCode {
    <#
        .SYNOPSIS
        Expands the country or area rows of an M49 code set into one record per distinct M49 code,
        covering every level of the M49 hierarchy: global, region, sub-region, intermediate region and
        country or area.  Each record carries the codes and the names of every level down to and
        including the level the code itself occupies.
    #>
    param (
        [Parameter(ValueFromPipeline)]
        [psobject[]]    $InputObject
    )

    begin {
        $areas = [ordered]@{}

        function Add-Area {
            param (
                [string]    $Code,
                [string]    $GlobalCode,
                [string]    $RegionCode,
                [string]    $SubRegionCode,
                [string]    $IntermediateRegionCode,
                [string[]]  $NameArguments,
                [string[]]  $Names,
                [string]    $Iso3166Part1Alpha2Code,
                [string]    $Iso3166Part1Alpha3Code
            )

            if ([string]::IsNullOrWhiteSpace($Code)) {
                return
            }

            $key = $Code -as [ushort]
            if ($areas.Contains($key)) {
                return
            }

            # Retain every level down to and including this code's own level, keyed by the attribute
            # argument suffix for that level.  The code of each level is retained alongside its English
            # name so that the equivalent name can be looked up in the other language code sets.
            $levelCodes = @($GlobalCode, $RegionCode, $SubRegionCode, $IntermediateRegionCode, $Code)
            $levels = [ordered]@{}
            for ($index = 0; $index -lt $NameArguments.Length; $index++) {
                if (-not [string]::IsNullOrWhiteSpace($Names[$index])) {
                    $levels[$NameArguments[$index]] = [pscustomobject]@{
                        Code = $levelCodes[$index]
                        Name = $Names[$index]
                    }
                }
            }

            $areas[$key] = [pscustomobject]@{
                'M49 Code'                 = $Code
                'Global Code'              = $GlobalCode
                'Region Code'              = $RegionCode
                'Sub-region Code'          = $SubRegionCode
                'Intermediate Region Code' = $IntermediateRegionCode
                'ISO-alpha2 Code'          = $Iso3166Part1Alpha2Code
                'ISO-alpha3 Code'          = $Iso3166Part1Alpha3Code
                'Levels'                   = $levels
            }
        }
    }

    process {
        # Global level.
        Add-Area -Code $_.'Global Code' `
            -GlobalCode $_.'Global Code' `
            -NameArguments @('GlobalName') `
            -Names @($_.'Global Name')

        # Region level.
        Add-Area -Code $_.'Region Code' `
            -GlobalCode $_.'Global Code' -RegionCode $_.'Region Code' `
            -NameArguments @('GlobalName', 'RegionName') `
            -Names @($_.'Global Name', $_.'Region Name')

        # Sub-region level.
        Add-Area -Code $_.'Sub-region Code' `
            -GlobalCode $_.'Global Code' -RegionCode $_.'Region Code' -SubRegionCode $_.'Sub-region Code' `
            -NameArguments @('GlobalName', 'RegionName', 'SubRegionName') `
            -Names @($_.'Global Name', $_.'Region Name', $_.'Sub-region Name')

        # Intermediate region level.
        Add-Area -Code $_.'Intermediate Region Code' `
            -GlobalCode $_.'Global Code' -RegionCode $_.'Region Code' -SubRegionCode $_.'Sub-region Code' -IntermediateRegionCode $_.'Intermediate Region Code' `
            -NameArguments @('GlobalName', 'RegionName', 'SubRegionName', 'IntermediateRegionName') `
            -Names @($_.'Global Name', $_.'Region Name', $_.'Sub-region Name', $_.'Intermediate Region Name')

        # Country or area level.
        Add-Area -Code $_.'M49 Code' `
            -GlobalCode $_.'Global Code' -RegionCode $_.'Region Code' -SubRegionCode $_.'Sub-region Code' -IntermediateRegionCode $_.'Intermediate Region Code' `
            -NameArguments @('GlobalName', 'RegionName', 'SubRegionName', 'IntermediateRegionName', 'CountryOrAreaName') `
            -Names @($_.'Global Name', $_.'Region Name', $_.'Sub-region Name', $_.'Intermediate Region Name', $_.'Country or Area') `
            -Iso3166Part1Alpha2Code $_.'ISO-alpha2 Code' -Iso3166Part1Alpha3Code $_.'ISO-alpha3 Code'
    }

    end {
        Write-Output ($areas.Values | Sort-Object { $_.'M49 Code' -as [ushort] })
    }
}

function Get-AreaName {
    <#
        .SYNOPSIS
        Resolves the name of a nominated level of the M49 hierarchy, for the row of a language-specific
        code set that carries the given M49 code at that level.
    #>
    param (
        [Parameter()]
        [psobject[]]    $CodesSet,

        [Parameter()]
        [string]    $Code,

        [Parameter()]
        [string]    $NameArgument
    )

    if (($CodesSet | Measure-Object | Select-Object -ExpandProperty Count) -eq 0) {
        return $null
    }

    # Locate the row carrying this code, then read the column holding the name of the requested level.
    $columns = switch ($NameArgument) {
        'GlobalName' { @('Global Code', 'Global Name') }
        'RegionName' { @('Region Code', 'Region Name') }
        'SubRegionName' { @('Sub-region Code', 'Sub-region Name') }
        'IntermediateRegionName' { @('Intermediate Region Code', 'Intermediate Region Name') }
        'CountryOrAreaName' { @('M49 Code', 'Country or Area') }
        default { return $null }
    }

    $codeValue = $Code -as [ushort]
    $match = $CodesSet | Where-Object { ($_.($columns[0]) -as [ushort]) -eq $codeValue } | Select-Object -First 1
    if ($null -ne $match -and -not [string]::IsNullOrWhiteSpace($match.($columns[1]))) {
        return $match.($columns[1])
    }

    return $null
}

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
        $m49Code = $_.'M49 Code'
        $status = "Evaluating code $m49Code"

        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding code field' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        # Declare field.  Member names are an underscore followed by the 3-digit M49 code.
        $enumField = [System.CodeDom.CodeMemberField]::new([ushort], "_$(($m49Code -as [ushort]).ToString('D3'))")
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new($m49Code -as [ushort])

        # Add code attribute.  Positional arguments describe the code's place in the M49 hierarchy, from
        # the global level down to the level the code itself occupies.
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

        # Named arguments carry the names of every level down to and including the level the code itself
        # occupies, in each available language, mirroring the positional codes above.
        foreach ($nameArgument in $_.'Levels'.Keys) {
            $level = $_.'Levels'[$nameArgument]
            $levelCode = $level.Code

            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new("English$nameArgument", [System.CodeDom.CodePrimitiveExpression]::new($level.Name))

            foreach ($languageSet in @(
                    @{ Language = 'Chinese'; CodesSet = $ChineseCodesSet },
                    @{ Language = 'Russian'; CodesSet = $RussianCodesSet },
                    @{ Language = 'French'; CodesSet = $FrenchCodesSet },
                    @{ Language = 'Spanish'; CodesSet = $SpanishCodesSet },
                    @{ Language = 'Arabic'; CodesSet = $ArabicCodesSet })) {
                $languageName = Get-AreaName -CodesSet $languageSet.CodesSet -Code $levelCode -NameArgument $nameArgument
                if (-not [string]::IsNullOrWhiteSpace($languageName)) {
                    $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new("$($languageSet.Language)$nameArgument", [System.CodeDom.CodePrimitiveExpression]::new($languageName))
                }
                elseif (($languageSet.CodesSet | Measure-Object | Select-Object -ExpandProperty Count) -gt 0) {
                    Write-Warning "$($languageSet.Language) variant of the $nameArgument for M49 code $m49Code not found."
                }
            }
        }

        # The ISO 3166 Part 1 country codes correlate the M49 code with the ISO 3166 standard.  Only
        # codes occupying the country or area level of the hierarchy bear them; aggregate levels such as
        # the global, region, sub-region and intermediate region levels have no country code.
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
        # The summary comment bears the English name of the level the code itself occupies, which is the
        # last of the levels retained for the code.
        $ownLevelName = $_.'Levels'[@($_.'Levels'.Keys)[-1]].Name
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($ownLevelName, $true)
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

    # Expand the country or area rows into one record per distinct M49 code at every hierarchy level.
    $areaCodesSet = $englishCodesSet | Expand-AreaCode

    $codeCount = $areaCodesSet | Measure-Object | Select-Object -ExpandProperty Count
    $areaCodesSet | Out-SourceCode -CodeCount $codeCount -ChineseCodesSet ($chineseCodesSet ?? @()) -RussianCodesSet ($russianCodesSet ?? @()) -FrenchCodesSet ($frenchCodesSet ?? @()) -SpanishCodesSet ($spanishCodesSet ?? @()) -ArabicCodesSet ($arabicCodesSet ?? @()) -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage
}
finally {
    Set-PSDebug -Off
}
