#########################################################################
# Title: ISO 3166 Part 1 Alpha-3 Source Code Generator                  #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.       #
#                                                                       #
# Purpose: Generate source code for implementing the ISO 3166 standard. #
# Source: Country Codes Collection, ISO.                                #
# https://www.iso.org/obp/ui#iso:pub:PUB500001:en                       #
#########################################################################
#Requires -Version 7.0

param (
    [Parameter(Mandatory, HelpMessage = 'Path to the folder containing the official files for script codes.')]
    [string]    $SourceFolderPath,

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
        [psobject[]]    $CountryNamesSet,

        [Parameter()]
        [psobject[]]    $TerritoriesSet,

        [Parameter()]
        [psobject[]]    $LanguagesSet,

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
        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.ISO3166')
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
        $status = "Evaluating script code $($_.alpha_3_code)"

        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding script code field' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        # Declare field.
        $enumField = [System.CodeDom.CodeMemberField]::new([ushort], $_.alpha_3_code)
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new($_.numeric_code -as [ushort])

        # Add code attribute.
        $codeAttributeArguments = @(
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.short_name_en)),
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.short_name_uppercase_en)))
        if (-not [string]::IsNullOrWhiteSpace($_.full_name_en)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.full_name_en))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.independent)) {
            switch ($_.independent) {
                'NO' { $isIndependent = $false }
                'YES' { $isIndependent = $true }
            }
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('IsIndependent', [System.CodeDom.CodePrimitiveExpression]::new($isIndependent))
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166CountryCodeAttribute', $codeAttributeArguments)
        [void]$enumField.CustomAttributes.Add($codeAttribute)

        # Add name attributes.
        $countryNumericCode = $_.numeric_code -as [ushort]
        $countryNamesCurrentSet = $CountryNamesSet | Where-Object { ($_.numeric_code -as [ushort]) -eq $countryNumericCode }
        foreach ($countryName in $countryNamesCurrentSet) {
            $countryNameLanguage = (-not [string]::IsNullOrWhiteSpace($countryName.language))?$countryName.language:$null
            $nameAttributeArguments = @(
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($countryNameLanguage)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($countryName.language_alpha_3_code)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($countryName.short_name)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($countryName.short_name_uppercase)))
            if (-not [string]::IsNullOrWhiteSpace($countryName.full_name)) {
                $nameAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($countryName.full_name))
            }

            $nameAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166CountryNameAttribute', $nameAttributeArguments)
            [void]$enumField.CustomAttributes.Add($nameAttribute)
        }

        # Add territory attributes.
        $territoriesCurrentSet = $TerritoriesSet | Where-Object { ($_.numeric_code -as [ushort]) -eq $countryNumericCode }
        foreach ($territory in $territoriesCurrentSet) {
            $territoryNameLanguage = (-not [string]::IsNullOrWhiteSpace($territory.language))?$territory.language:$null
            $territoryAttributeArguments = @(
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($territoryNameLanguage)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($territory.language_alpha_3_code)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($territory.territory_id -as [ushort])),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($territory.territory_name.Trim())))
            $territoryAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166CountryTerritoryAttribute', $territoryAttributeArguments)
            [void]$enumField.CustomAttributes.Add($territoryAttribute)
        }
        
        # Add language attributes.
        $languagesCurrentSet = $LanguagesSet | Where-Object { ($_.numeric_code -as [ushort]) -eq $countryNumericCode }
        foreach ($language in $languagesCurrentSet) {
            $languageAlpha2Code = (-not [string]::IsNullOrWhiteSpace($language.language_alpha_2_code))?$language.language_alpha_2_code:$null
            $languageAttributeArguments = @(
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($languageAlpha2Code)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($language.language_alpha_3_code)))
            if (-not [string]::IsNullOrWhiteSpace($language.is_administrative)) {
                switch ($language.is_administrative) {
                    'NO' { $isAdministrative = $false }
                    'YES' { $isAdministrative = $true }
                }
                $languageAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('IsAdministrative', [System.CodeDom.CodePrimitiveExpression]::new($isAdministrative))
            }
            $languageAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166LanguageAttribute', $languageAttributeArguments)
            [void]$enumField.CustomAttributes.Add($languageAttribute)
        }
        
        # Add summary comment.
        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($_.short_name_en, $true)
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

# Validate parameters.
if (-not (Test-Path $SourceFolderPath -PathType Container)) {
    Write-Error "Source folder $SourceFolderPath not found."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    $countryCodesFilePath = Join-Path $SourceFolderPath -ChildPath 'country-codes.csv'
    if (Test-Path $countryCodesFilePath -PathType Leaf) {
        $countryCodesSet = Import-Csv -Path $countryCodesFilePath | Where-Object -Property status -EQ 'officially-assigned'
    }
    else {
        Write-Error "Source file '$countryCodesFilePath' not found."
        exit;
    }

    $countryNamesFilePath = Join-Path $SourceFolderPath -ChildPath 'country-names.csv'
    if (Test-Path $countryNamesFilePath -PathType Leaf) {
        $countryNamesSet = Import-Csv -Path $countryNamesFilePath
    }
    else {
        Write-Error "Source file '$countryNamesFilePath' not found."
        exit;
    }

    $territoriesFilePath = Join-Path $SourceFolderPath -ChildPath 'territories.csv'
    if (Test-Path $territoriesFilePath -PathType Leaf) {
        $territoriesSet = Import-Csv -Path $territoriesFilePath
    }
    else {
        Write-Error "Source file '$territoriesFilePath' not found."
        exit;
    }

    $languagesFilePath = Join-Path $SourceFolderPath -ChildPath 'languages.csv'
    if (Test-Path $languagesFilePath -PathType Leaf) {
        $languagesSet = Import-Csv -Path $languagesFilePath
    }
    else {
        Write-Error "Source file '$languagesFilePath' not found."
        exit;
    }

    $codeCount = $countryCodesSet | Measure-Object | Select-Object -ExpandProperty Count
    $countryCodesSet | Out-SourceCode -CodeCount $codeCount -CountryNamesSet $countryNamesSet -TerritoriesSet $territoriesSet -LanguagesSet $languagesSet -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage
}
finally {
    Set-PSDebug -Off
}