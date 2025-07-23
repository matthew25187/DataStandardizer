#################################################################################
# Title: ISO 639 Part 2: Alpha-3 Source Code Generator                          #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.               #
#                                                                               #
# Purpose: Generate source code for implementing the ISO 639: Part 2 standard.  #
# Source: ISO 639 Set 2, Library of Congress.                                   #
# https://www.loc.gov/standards/iso639-2/ascii_8bits.html                       #
#################################################################################

param (
    [Parameter(Mandatory, HelpMessage = 'Path to the file containing the official list of language codes.')]
    [string]    $SourceFilePath,

    [Parameter(Mandatory, HelpMessage = 'Name of the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.  WARNING: Use of this parameter to specify a source code language other than C# is not fully supported.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeLanguage = 'CSharp',

    [Parameter(HelpMessage = 'Type of Part 2 language codes to process.')]
    [ValidateSet('Bibliographic', 'Terminologic')]
    [string]  $LanguageCodeType = [CodeType]::Terminologic
)

enum CodeType {
    Bibliographic
    Terminologic
}

function Out-SourceCode {
    [CmdletBinding()]
    param (
        [Parameter(Mandatory, ValueFromPipeline)]
        [System.Object] $InputObject,

        [Parameter()]
        [int]   $CodeProcessCount,

        [Parameter()]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter()]
        [string]    $GenerateLanguage,

        [Parameter()]
        [CodeType]  $CodeType
    )
    
    begin {
        $activity = "Generating $GenerateTypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        $codesProcessed = 0

        $preprocessorDirectives = '#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif'
        $preprocessorDirectivesCompileUnit = [System.CodeDom.CodeSnippetCompileUnit]::new($preprocessorDirectives)
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
        $compileUnits = @($preprocessorDirectivesCompileUnit, $compileUnit)
        
        $namespace = [System.CodeDom.CodeNamespace]::new((Get-Variable -Name SourceCodeNamespace -ValueOnly))
        [void]$compileUnit.Namespaces.Add($namespace)
        $namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Reflection'))
    
        Write-Progress -Activity $activity -CurrentOperation 'Declaring enum type' -PercentComplete -1

        $structType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
        [void]$structType.BaseTypes.Add('DataStandardizer.Core.IStringEnum')
        $equatableTypeReference = [System.CodeDom.CodeTypeReference]::new('System.IEquatable')
        $equatableTypeReference.TypeArguments.Add("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName")
        [void]$structType.BaseTypes.Add($equatableTypeReference)
        $structType.IsStruct = $true
        $structType.TypeAttributes = [System.Reflection.TypeAttributes]::Public

        if (-not [string]::IsNullOrEmpty($TypeComment)) {
            $structTypeOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
            $structTypeSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($TypeComment, $true)
            $structTypeCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
            $structType.Comments.AddRange(@($structTypeOpenSummaryComment, $structTypeSummaryContentComment, $structTypeCloseSummaryComment))
        }
    
        Write-Progress -Activity $activity -CurrentOperation 'Declaring fields' -PercentComplete -1

        [System.CodeDom.CodeTypeMember[]]$declarationMembers = @(
            [System.CodeDom.CodeSnippetTypeMember]::new('#if NETCOREAPP3_0_OR_GREATER'),
            (Get-ValueFieldDeclaration -UseNullableReferenceTypes),
            [System.CodeDom.CodeSnippetTypeMember]::new('#else'),
            (Get-ValueFieldDeclaration),
            [System.CodeDom.CodeSnippetTypeMember]::new('#endif'))
        $declarationMembers | Select-Object -First 1 | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Declarations')) }
        $declarationMembers | Select-Object -Last 1 | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }
        $structType.Members.AddRange($declarationMembers)
        
        Write-Progress -Activity $activity -CurrentOperation 'Declaring constructor' -PercentComplete -1

        $structConstructor = [System.CodeDom.CodeConstructor]::new()
        $structConstructor.Attributes = ($structConstructor.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Private
        [void]$structConstructor.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new([string], 'value'))
        $argumentCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
            [System.CodeDom.CodeBinaryOperatorExpression]::new(
                [System.CodeDom.CodeArgumentReferenceExpression]::new('value'),
                [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
                [System.CodeDom.CodePrimitiveExpression]::new($null)),
            @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.ArgumentNullException]), @([System.CodeDom.CodeSnippetExpression]::new('nameof(value)'))))))
        $valueAssignmentStatement = [System.CodeDom.CodeAssignStatement]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'), 
            [System.CodeDom.CodeArgumentReferenceExpression]::new('value'))
        $structConstructor.Statements.AddRange(@($argumentCheckStatement, $valueAssignmentStatement))
        [void]$structConstructor.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Constructors'))
        [void]$structConstructor.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        [void]$structType.Members.Add($structConstructor)

        Write-Progress -Activity $activity -CurrentOperation 'Declaring operators' -PercentComplete -1

        $stringToStructConversionOperatorSnippet = "#if NETCOREAPP3_0_OR_GREATER
        public static explicit operator $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName(string value)
#else
        public static explicit operator $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName([JetBrains.Annotations.NotNullAttribute] string value)
#endif
        {
            return new $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName(value);
        }"
        $stringToStructConversionOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($stringToStructConversionOperatorSnippet)
        [void]$stringToStructConversionOperatorSnippetMember.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Operators'))
        [void]$structType.Members.Add($stringToStructConversionOperatorSnippetMember)

        $structToStringConversionOperatorSnippet = "#if NETCOREAPP3_0_OR_GREATER
        public static implicit operator string?($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName value)
#else
        [JetBrains.Annotations.CanBeNullAttribute]
        public static implicit operator string($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName value)
#endif
        {
            return value._value;
        }"
        [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($structToStringConversionOperatorSnippet))

        $equalityOperatorSnippet = "        public static bool operator ==($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName left, $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName right)
        {
            return left.Equals(right);
        }"
        [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($equalityOperatorSnippet))

        $inequalityOperatorSnippet = "        public static bool operator !=($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName left, $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName right)
        {
            return !left.Equals(right);
        }"
        $inequalityOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($inequalityOperatorSnippet)
        [void]$inequalityOperatorSnippetMember.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        [void]$structType.Members.Add($inequalityOperatorSnippetMember)

        [void]$namespace.Types.Add($structType)
    }

    process {
        $item = $_

        [string]$languageCode = $null
        switch ($CodeType) {
            ([CodeType]::Bibliographic) { $languageCode = $item.Part2bCode ?? $item.Part2tCode }
            ([CodeType]::Terminologic) { $languageCode = $item.Part2tCode ?? $item.Part2bCode }

            default {
                Write-Error "Unrecognised code type $CodeType."
            }
        }
        if ([string]::IsNullOrEmpty($languageCode) -or $languageCode.Length -ne 3) {
            return;
        }

        $status = "Evaluating language code $languageCode"

        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding language code field' -PercentComplete (($codesProcessed -gt 0? ($codesProcessed / $CodeProcessCount):0) * 100)

        $languageCodeField = [System.CodeDom.CodeMemberField]::new("readonly $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName", "@$languageCode")
        $languageCodeField.Attributes = ($languageCodeField.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
        $languageCodeField.Attributes = ($languageCodeField.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Static
        $languageCodeField.InitExpression = [System.CodeDom.CodeObjectCreateExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).$TypeName", @([System.CodeDom.CodePrimitiveExpression]::new($languageCode)))

        $languageCodeFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $languageCodeFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($item.EnglishName, $true)
        $languageCodeFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $languageCodeField.Comments.AddRange(@($languageCodeFieldOpenSummaryComment, $languageCodeFieldSummaryContentComment, $languageCodeFieldCloseSummaryComment))

        $englishNames = $_.EnglishName -split ';' 
        $frenchNames = $_.FrenchName -split ';' 
        [System.CodeDom.CodeAttributeArgument[]]$codeAttributeArguments = @()
        if ($englishNames.Count -eq 1 -and $frenchNames.Count -eq 1) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($englishNames[0].Trim()))
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($frenchNames[0].Trim()))
        }
        else {
            [System.CodeDom.CodeExpression[]]$englishNameExpressions = $englishNames | ForEach-Object { [System.CodeDom.CodePrimitiveExpression]::new($_.Trim()) }
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodeArrayCreateExpression]::new([string], $englishNameExpressions))

            [System.CodeDom.CodeExpression[]]$frenchNameExpressions = $frenchNames | ForEach-Object { [System.CodeDom.CodePrimitiveExpression]::new($_.Trim()) }
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodeArrayCreateExpression]::new([string], $frenchNameExpressions))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.Part1Code)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Part1Code', [System.CodeDom.CodePrimitiveExpression]::new($_.Part1Code))
        }
        switch ($CodeType) {
            ([CodeType]::Bibliographic) {
                $part2TCode = $item.Part2tCode ?? $item.Part2bCode
                $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Part2TCode', [System.CodeDom.CodePrimitiveExpression]::new($part2TCode))
            }
            ([CodeType]::Terminologic) {
                $part2BCode = $item.Part2bCode ?? $item.Part2tCode
                $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Part2BCode', [System.CodeDom.CodePrimitiveExpression]::new($part2BCode))
            }
            Default {}
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639CodeAttribute", $codeAttributeArguments)
        [void]$languageCodeField.CustomAttributes.Add($codeAttribute)

        [void]$structType.Members.Add($languageCodeField)
        
        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeProcessCount) * 100)
    }

    end {
        $structType.Members
        | Where-Object { $_ -is [System.CodeDom.CodeMemberField] -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::AccessMask) -eq [System.CodeDom.MemberAttributes]::Public) -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::ScopeMask) -eq [System.CodeDom.MemberAttributes]::Static) }
        | Select-Object -First 1
        | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields')) }

        $structType.Members
        | Where-Object { $_ -is [System.CodeDom.CodeMemberField] -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::AccessMask) -eq [System.CodeDom.MemberAttributes]::Public) -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::ScopeMask) -eq [System.CodeDom.MemberAttributes]::Static) }
        | Select-Object -Last 1
        | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }
        
        Write-Progress -Activity $activity -CurrentOperation 'Declaring public methods' -PercentComplete -1

        [System.CodeDom.CodeTypeMember[]]$publicMethods = @(
                (Get-EqualsMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $TypeName),
                (Get-GetHashCodeMethodDefinition),
            [System.CodeDom.CodeSnippetTypeMember]::new('#if NETCOREAPP3_0_OR_GREATER'),
                (Get-CompareToMethodDefinition -UseNullableReferenceTypes),
                (Get-InheritedEqualsMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $TypeName -UseNullableReferenceTypes),
                (Get-InheritedToStringMethodDefinition -UseNullableReferenceTypes),
            [System.CodeDom.CodeSnippetTypeMember]::new('#else'),
                (Get-CompareToMethodDefinition),
                (Get-InheritedEqualsMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $TypeName),
                (Get-InheritedToStringMethodDefinition),
            [System.CodeDom.CodeSnippetTypeMember]::new('#endif')
            [System.CodeDom.CodeSnippetTypeMember]::new('#if NETCOREAPP3_0_OR_GREATER'),
                (Get-GetTypeCodeMethodDefinition),
                (Get-ToBooleanMethodDefinition -UseNullableReferenceTypes),
                (Get-ToByteMethodDefinition -UseNullableReferenceTypes),
                (Get-ToCharMethodDefinition -UseNullableReferenceTypes),
                (Get-ToDateTimeMethodDefinition -UseNullableReferenceTypes),
                (Get-ToDecimalMethodDefinition -UseNullableReferenceTypes),
                (Get-ToDoubleMethodDefinition -UseNullableReferenceTypes),
                (Get-ToInt16MethodDefinition -UseNullableReferenceTypes),
                (Get-ToInt32MethodDefinition -UseNullableReferenceTypes),
                (Get-ToInt64MethodDefinition -UseNullableReferenceTypes),
                (Get-ToSByteMethodDefinition -UseNullableReferenceTypes),
                (Get-ToSingleMethodDefinition -UseNullableReferenceTypes),
                (Get-ToStringMethodDefinition -UseNullableReferenceTypes),
                (Get-ToTypeMethodDefinition -UseNullableReferenceTypes),
                (Get-ToUInt16MethodDefinition -UseNullableReferenceTypes),
                (Get-ToUInt32MethodDefinition -UseNullableReferenceTypes),
                (Get-ToUInt64MethodDefinition -UseNullableReferenceTypes),
            [System.CodeDom.CodeSnippetTypeMember]::new('#elif NETSTANDARD1_3_OR_GREATER||NET'),
                (Get-GetTypeCodeMethodDefinition),
                (Get-ToBooleanMethodDefinition),
                (Get-ToByteMethodDefinition),
                (Get-ToCharMethodDefinition),
                (Get-ToDateTimeMethodDefinition),
                (Get-ToDecimalMethodDefinition),
                (Get-ToDoubleMethodDefinition),
                (Get-ToInt16MethodDefinition),
                (Get-ToInt32MethodDefinition),
                (Get-ToInt64MethodDefinition),
                (Get-ToSByteMethodDefinition),
                (Get-ToSingleMethodDefinition),
                (Get-ToStringMethodDefinition),
                (Get-ToTypeMethodDefinition),
                (Get-ToUInt16MethodDefinition),
                (Get-ToUInt32MethodDefinition),
                (Get-ToUInt64MethodDefinition),
            [System.CodeDom.CodeSnippetTypeMember]::new('#endif')
        )
        $publicMethods | Select-Object -First 1 | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Methods')) }
        $publicMethods | Select-Object -Last 1 | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }

        $structType.Members.AddRange($publicMethods)

        # Declare private methods.
        Write-Progress -Activity $activity -CurrentOperation 'Declaring private methods' -PercentComplete -1

        $memberFieldPredicateMethod = Get-MemberFieldPredicateMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $TypeName -GenerateLanguage $GenerateLanguage
        [void]$memberFieldPredicateMethod.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Private Methods'))
        [void]$memberFieldPredicateMethod.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        [void]$structType.Members.Add($memberFieldPredicateMethod)

        Write-Progress -Completed

        # Output source code.
        $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($GenerateLanguage)

        $options = [System.CodeDom.Compiler.CodeGeneratorOptions]::new()
        $options.BlankLinesBetweenMembers = $true
        $options.BracingStyle = 'C'
        $options.VerbatimOrder = $true

        foreach ($cu in $compileUnits) {
            $sourceCodeCuBuilder = [System.Text.StringBuilder]::new()
            $writer = [System.IO.StringWriter]::new($sourceCodeCuBuilder)
            try {
                $provider.GenerateCodeFromCompileUnit($cu, $writer, $options)
                Write-Output $sourceCodeCuBuilder.ToString()
            }
            finally {
                $writer.Close()
            }
        }
    }
}

# Validate parameters.
if (-not (Test-Path -Path $SourceFilePath)) {
    Write-Error "Source file $SourceFilePath not found."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    $modulePath = Resolve-Path scripts\StringEnumCodeGen\StringEnumCodeGen.psm1
    Import-Module (Split-Path $modulePath -Parent)

    if (-not (Test-Path Variable:\SourceCodeNamespace)) {
        Set-Variable -Name SourceCodeNamespace -Value 'DataStandardizer.ISO639' -Option Constant
    }

    $codeSet = Import-Csv -Path $SourceFilePath -Header Part2bCode, Part2tCode, Part1Code, EnglishName, FrenchName -Delimiter '|'
    foreach ($codeItem in $codeSet) {
        $codeItem.Part2bCode = (-not [string]::IsNullOrWhiteSpace($codeItem.Part2bCode))?($codeItem.Part2bCode):$null
        $codeItem.Part2tCode = (-not [string]::IsNullOrWhiteSpace($codeItem.Part2tCode))?($codeItem.Part2tCode):$null
    }
    $codeCount = $codeSet | Measure-Object | Select-Object -ExpandProperty Count
    $codeSet 
    | Out-SourceCode -CodeProcessCount $codeCount -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage -CodeType ([CodeType]$LanguageCodeType)
}
finally {
    Remove-Module StringEnumCodeGen
    Set-PSDebug -Off
}

# Follow-up instructions.
Write-Information 'Next steps:'
Write-Information "*`tMake the $SourceCodeTypeName type readonly."
Write-Information "*`tMove the preprocessor directives at the top of the file to below the headline comment block.  This may help to minimise or eliminate spurious warnings."