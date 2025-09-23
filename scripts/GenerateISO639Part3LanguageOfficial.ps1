#################################################################################
# Title: ISO 639 Part 3: Alpha-3 Source Code Generator                          #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.               #
#                                                                               #
# Purpose: Generate source code for implementing the ISO 639: Part 3 standard.  #
# Source: ISO 639 Code Tables, SIL Global.                                      #
# https://iso639-3.sil.org/code_tables/download_tables                          #
#################################################################################
#Requires -Version 7.0

param (
    [Parameter(HelpMessage = 'Path to the folder containing the source files.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceFolder,

    [Parameter(HelpMessage = 'Date on which the source files were published or last changed.  Use format yyyyMMdd.')]
    [ValidatePattern('^\d{4}(?:0[1-9]|1[0-2])(?:0[1-9]|[1-2]\d|3[0-1])$')]
    [string]    $PublishedDate,

    [Parameter(Mandatory, HelpMessage = 'Name of the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.  WARNING: Use of this parameter to specify a source code language other than C# is not fully supported.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeLanguage = 'CSharp',

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]    $ExcludeFieldsStartingWith = $null,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string]    $IncludeFieldsStartingWith = $null,

    [Parameter()]
    [switch]    $ExcludeTypeMainBody,

    [Parameter()]
    [switch]    $ExcludeFields,

    [Parameter()]
    [switch]    $MakeSourceCodeTypePartial
)

function Out-SourceCode {
    [CmdletBinding()]
    param (
        [Parameter(ValueFromPipeline)]
        [System.Object] $InputObject,

        [Parameter()]
        [int]   $CodeProcessCount,

        [Parameter()]
        [psobject[]]    $LanguageNamesIndex,

        [Parameter()]
        [psobject[]]    $MacrolanguageMappings,

        [Parameter()]
        [System.Nullable[System.DateOnly]]  $PublishedDate,

        [Parameter()]
        [string]    $GenerateTypeName,

        [Parameter()]
        [string]    $GenerateTypeComment,

        [Parameter()]
        [string]    $GenerateLanguage,

        [Parameter()]
        [string]    $ExcludeFieldNamePrefix,

        [Parameter()]
        [string]    $IncludeFieldNamePrefix,

        [Parameter()]
        [bool]  $GenerateMainBody,

        [Parameter()]
        [bool]  $GeneratePartialType
    )
    
    begin {
        Write-Debug '================================================================================'
        Write-Debug 'GENERATING SOURCE CODE FOR OUTPUT'
        
        $activity = "Generating $GenerateTypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        $codesProcessed = 0
        $preprocessorDirectives = '#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif'
        $preprocessorDirectivesCompileUnit = [System.CodeDom.CodeSnippetCompileUnit]::new($preprocessorDirectives)
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
        $compileUnits = @($preprocessorDirectivesCompileUnit, $compileUnit)
    
        # Declare namespace.
        $namespace = [System.CodeDom.CodeNamespace]::new((Get-Variable -Name SourceCodeNamespace -ValueOnly))
        [void]$compileUnit.Namespaces.Add($namespace)
        if ($GenerateMainBody) {
            $namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Reflection'))
        }
    
        # Declare type.
        Write-Progress -Activity $activity -CurrentOperation 'Declaring enum type' -PercentComplete -1

        $structType = [System.CodeDom.CodeTypeDeclaration]::new($GenerateTypeName)
        [void]$structType.BaseTypes.Add('DataStandardizer.Core.IStringEnum')
        $equatableTypeReference = [System.CodeDom.CodeTypeReference]::new('System.IEquatable')
        $equatableTypeReference.TypeArguments.Add("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName")
        [void]$structType.BaseTypes.Add($equatableTypeReference)
        $structType.IsStruct = $true
        $structType.IsPartial = $GeneratePartialType
        $structType.TypeAttributes = [System.Reflection.TypeAttributes]::Public
    
        if ($GenerateMainBody) {
            if (-not [string]::IsNullOrEmpty($GenerateTypeComment)) {
                $structTypeOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
                $structTypeSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($GenerateTypeComment, $true)
                $structTypeCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
                $structType.Comments.AddRange(@($structTypeOpenSummaryComment, $structTypeSummaryContentComment, $structTypeCloseSummaryComment))
            }
        
            if ($null -ne $PublishedDate) {
                $structTypeOpenRemarksComment = [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
                $structTypeRemarksContentComment = [System.CodeDom.CodeCommentStatement]::new("Based on official ISO 639 Part 3 language codes as at $($PublishedDate.ToString('yyyy-MM-dd')).", $true)
                $structTypeCloseRemarksComment = [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
                $structType.Comments.AddRange(@($structTypeOpenRemarksComment, $structTypeRemarksContentComment, $structTypeCloseRemarksComment))
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
            public static explicit operator $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName(string value)
#else
            public static explicit operator $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName([JetBrains.Annotations.NotNullAttribute] string value)
#endif
            {
                return new $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName(value);
            }"
            $stringToStructConversionOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($stringToStructConversionOperatorSnippet)
            [void]$stringToStructConversionOperatorSnippetMember.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Operators'))
            [void]$structType.Members.Add($stringToStructConversionOperatorSnippetMember)
    
            $structToStringConversionOperatorSnippet = "#if NETCOREAPP3_0_OR_GREATER
            public static implicit operator string?($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName value)
#else
            [JetBrains.Annotations.CanBeNullAttribute]
            public static implicit operator string($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName value)
#endif
            {
                return value._value;
            }"
            [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($structToStringConversionOperatorSnippet))
    
            $equalityOperatorSnippet = "        public static bool operator ==($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName left, $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName right)
            {
                return left.Equals(right);
            }"
            [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($equalityOperatorSnippet))
    
            $inequalityOperatorSnippet = "        public static bool operator !=($(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName left, $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName right)
            {
                return !left.Equals(right);
            }"
            $inequalityOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($inequalityOperatorSnippet)
            [void]$inequalityOperatorSnippetMember.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
            [void]$structType.Members.Add($inequalityOperatorSnippetMember)
        }

        [void]$namespace.Types.Add($structType)
    }

    process {
        if ($CodeProcessCount -eq 0) {
            return; # fields have been excluded so nothing to do here
        }

        $codeIdentifier = $_.Id
        $codePart2b = $_.Part2b
        $codePart2t = $_.Part2t
        $codePart1 = $_.Part1
        $codeScope = $_.Scope
        $codeLanguageType = $_.Language_Type
        $codeReferenceName = $_.Ref_Name
        $codeComment = $_.Comment

        if ((-not [string]::IsNullOrEmpty($ExcludeFieldNamePrefix)) -and $codeIdentifier -like "$ExcludeFieldNamePrefix*") {
            return;
        }

        if ((-not [string]::IsNullOrEmpty($IncludeFieldNamePrefix)) -and $codeIdentifier -notlike "$IncludeFieldNamePrefix*") {
            return;
        }

        $status = "Evaluating language code $codeIdentifier"

        # Find related records.
        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Finding related language names and macrolanguage' -PercentComplete (($codesProcessed -gt 0? ($codesProcessed / $CodeProcessCount):0) * 100)

        $languageNameItem = $LanguageNamesIndex | Where-Object -Property Id -EQ $codeIdentifier
        $macrolanguageMappingItem = $MacrolanguageMappings | Where-Object -Property I_Id -EQ $codeIdentifier

        # Declare public fields.
        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding language code field' -PercentComplete (($codesProcessed -gt 0? ($codesProcessed / $CodeProcessCount):0) * 100)

        $languageCodeField = [System.CodeDom.CodeMemberField]::new("readonly $(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName", "@$codeIdentifier")
        $languageCodeField.Attributes = ($languageCodeField.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
        $languageCodeField.Attributes = ($languageCodeField.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Static
        $languageCodeField.InitExpression = [System.CodeDom.CodeObjectCreateExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).$GenerateTypeName", @([System.CodeDom.CodePrimitiveExpression]::new($codeIdentifier)))

        $languageCodeFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $languageCodeFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($codeReferenceName, $true)
        $languageCodeFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $languageCodeField.Comments.AddRange(@($languageCodeFieldOpenSummaryComment, $languageCodeFieldSummaryContentComment, $languageCodeFieldCloseSummaryComment))

        if (-not [string]::IsNullOrWhiteSpace($codeComment)) {
            $languageCodeField.Comments.AddRange(@(
                    [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true),
                    [System.CodeDom.CodeCommentStatement]::new("`t<para>", $true),
                    [System.CodeDom.CodeCommentStatement]::new("`t`t$codeComment", $true),
                    [System.CodeDom.CodeCommentStatement]::new("`t</para>", $true),
                    [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)))
        }

        [string]$languagePrintName = ${languageNameItem}?.Print_Name
        [string]$languageInvertedName = ${languageNameItem}?.Inverted_Name
        [System.CodeDom.CodeFieldReferenceExpression]$languageCodeScope = $null
        switch ($codeScope) {
            'I' { $languageCodeScope = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageScope"), 'Individual') }
            'M' { $languageCodeScope = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageScope"), 'Macrolanguage') }
            'S' { $languageCodeScope = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageScope"), 'Special') }
        }
        [System.CodeDom.CodeFieldReferenceExpression]$languageCodeType = $null
        switch ($codeLanguageType) {
            'A' { $languageCodeType = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageType"), 'Ancient') }
            'C' { $languageCodeType = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageType"), 'Constructed') }
            'E' { $languageCodeType = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageType"), 'Extinct') }
            'H' { $languageCodeType = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageType"), 'Historical') }
            'L' { $languageCodeType = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageType"), 'Living') }
            'S' { $languageCodeType = [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageType"), 'Special') }
        }
        [System.CodeDom.CodeAttributeArgument[]]$codeAttributeArguments = @([System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($codeReferenceName)))
        if ($null -ne $languageCodeScope -and $null -ne $languageCodeType) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new($languageCodeScope)
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new($languageCodeType)
        }
        if (-not [string]::IsNullOrEmpty($languagePrintName)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('PrintName', [System.CodeDom.CodePrimitiveExpression]::new($languagePrintName))
        }
        if (-not [string]::IsNullOrEmpty($languageInvertedName)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('InvertedName', [System.CodeDom.CodePrimitiveExpression]::new($languageInvertedName))
        }
        if (-not [string]::IsNullOrEmpty($codePart1)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Part1Code', [System.CodeDom.CodePrimitiveExpression]::new($codePart1))
        }
        if (-not [string]::IsNullOrEmpty($codePart2b)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Part2BCode', [System.CodeDom.CodePrimitiveExpression]::new($codePart2b))
        }
        if (-not [string]::IsNullOrEmpty($codePart2t)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Part2TCode', [System.CodeDom.CodePrimitiveExpression]::new($codePart2t))
        }
        if ($null -ne $macrolanguageMappingItem) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('MacrolanguageCode', [System.CodeDom.CodePrimitiveExpression]::new($macrolanguageMappingItem.M_Id))
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new("$(Get-Variable -Name SourceCodeNamespace -ValueOnly).Iso639LanguageCodeAttribute", $codeAttributeArguments)
        [void]$languageCodeField.CustomAttributes.Add($codeAttribute)

        [void]$structType.Members.Add($languageCodeField)
        
        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeProcessCount) * 100)
    }

    end {
        # Define region for public fields.
        $firstPublicField = $structType.Members
        | Where-Object { $_ -is [System.CodeDom.CodeMemberField] -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::AccessMask) -eq [System.CodeDom.MemberAttributes]::Public) -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::ScopeMask) -eq [System.CodeDom.MemberAttributes]::Static) }
        | Select-Object -First 1
        if ($null -ne $firstPublicField) {
            [void]$firstPublicField.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields'))
        }

        $lastPublicField = $structType.Members
        | Where-Object { $_ -is [System.CodeDom.CodeMemberField] -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::AccessMask) -eq [System.CodeDom.MemberAttributes]::Public) -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::ScopeMask) -eq [System.CodeDom.MemberAttributes]::Static) }
        | Select-Object -Last 1
        if ($null -ne $lastPublicField) {
            [void]$lastPublicField.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        }
        
        if ($GenerateMainBody) {
            # Declare public methods.
            Write-Progress -Activity $activity -CurrentOperation 'Declaring public methods' -PercentComplete -1

            [System.CodeDom.CodeTypeMember[]]$publicMethods = @(
                (Get-EqualsMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $GenerateTypeName),
                (Get-GetHashCodeMethodDefinition),
                [System.CodeDom.CodeSnippetTypeMember]::new('#if NETCOREAPP3_0_OR_GREATER'),
                (Get-CompareToMethodDefinition -UseNullableReferenceTypes),
                (Get-InheritedEqualsMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $GenerateTypeName -UseNullableReferenceTypes),
                (Get-InheritedToStringMethodDefinition -UseNullableReferenceTypes),
                [System.CodeDom.CodeSnippetTypeMember]::new('#else'),
                (Get-CompareToMethodDefinition),
                (Get-InheritedEqualsMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $GenerateTypeName),
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
        
            $firstPublicMethod = $publicMethods | Select-Object -First 1
            [void]$firstPublicMethod.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Methods'))
        
            $lastPublicMethod = $publicMethods | Select-Object -Last 1
            [void]$lastPublicMethod.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))

            $structType.Members.AddRange($publicMethods)

            # Declare private methods.
            Write-Progress -Activity $activity -CurrentOperation 'Declaring private methods' -PercentComplete -1

            $memberFieldPredicateMethod = Get-MemberFieldPredicateMethodDefinition -TypeNamespace (Get-Variable -Name SourceCodeNamespace -ValueOnly) -TypeName $GenerateTypeName -GenerateLanguage $GenerateLanguage
            [void]$memberFieldPredicateMethod.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Private Methods'))
            [void]$memberFieldPredicateMethod.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
            [void]$structType.Members.Add($memberFieldPredicateMethod)
        }

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

Write-Debug '================================================================================'
Write-Debug 'PARAMETERS'
Write-Debug "Source folder: $SourceFolder"
Write-Debug "Published date: $PublishedDate"
Write-Debug "Source code type name: $SourceCodeTypeName"
Write-Debug "Source code type comment: $SourceCodeTypeComment"
Write-Debug "Source code language: $SourceCodeLanguage"
Write-Debug "Exclude fields starting with: $ExcludeFieldsStartingWith"
Write-Debug "Include fields starting with: $IncludeFieldsStartingWith"
Write-Debug "Exclude type main body: $($PSBoundParameters.ContainsKey('ExcludeTypeMainBody'))"
Write-Debug "Make partial type: $($PSBoundParameters.ContainsKey('MakeSourceCodeTypePartial'))"
Write-Debug ''

# Validate parameters.
if ($PSBoundParameters.ContainsKey('SourceFolder') -and -not (Test-Path -Path $SourceFolder -PathType Container)) {
    Write-Error "Source folder '$SourceFolder' not found."
    exit;
}

$tempPublishedDate = [System.DateOnly]::MinValue
if ($PSBoundParameters.ContainsKey('PublishedDate') -and -not [System.DateOnly]::TryParseExact($PublishedDate, 'yyyyMMdd', [ref]$tempPublishedDate)) {
    Write-Error "Published date $PublishedDate not recognised."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    $modulePath = Resolve-Path scripts\StringEnumCodeGen\StringEnumCodeGen.psm1
    Import-Module (Split-Path $modulePath -Parent)

    if (-not (Test-Path Variable:\SourceCodeNamespace)) {
        Set-Variable -Name SourceCodeNamespace -Value 'DataStandardizer.Language' -Option Constant
    }

    # Load source files.
    $useFolder = $SourceFolder ?? '.'

    $languageNamesIndexFilePath = Join-Path $useFolder -ChildPath 'iso-639-3_Name_Index.tab'
    $languageNamesIndex = Import-Csv -Path $languageNamesIndexFilePath -Delimiter "`t"
    
    $macrolanguageMappingsFilePath = Join-Path $useFolder -ChildPath 'iso-639-3-macrolanguages.tab'
    $macrolanguageMappings = Import-Csv $macrolanguageMappingsFilePath -Delimiter "`t"
    
    $mainTableFilePath = Join-Path -Path $useFolder -ChildPath 'iso-639-3.tab'
    [System.Nullable[System.DateOnly]]$usePublishedDate = $PSBoundParameters.ContainsKey('PublishedDate')? [System.DateOnly]::ParseExact($PublishedDate, 'yyyyMMdd'):$null
    Write-Debug "Using published date $usePublishedDate"

    $codeSet = Import-Csv -Path $mainTableFilePath -Delimiter "`t"

    # Build code DOM and output source code.
    $codeTotalCount = (-not $PSBoundParameters.ContainsKey('ExcludeFields'))? ($codeSet | Measure-Object | Select-Object -ExpandProperty Count):0
    if ($codeTotalCount -gt 0) {
        Write-Information "Found $codeTotalCount language codes in source file."
    }
    $codeProcessCount = $codeTotalCount
    if ((-not $PSBoundParameters.ContainsKey('ExcludeFields')) -and (-not [string]::IsNullOrWhiteSpace($IncludeFieldsStartingWith))) {
        $codeProcessCount = $codeSet | Where-Object -Property Id -Like "$IncludeFieldsStartingWith*" | Measure-Object | Select-Object -ExpandProperty Count
        Write-Information "Include Fields filtering applied; $codeProcessCount language codes found."
    }
    if ((-not $PSBoundParameters.ContainsKey('ExcludeFields')) -and (-not [string]::IsNullOrWhiteSpace($ExcludeFieldsStartingWith))) {
        $codeProcessCount -= $codeSet | Where-Object -Property Id -NotLike "$ExcludeFieldsStartingWith*" | Measure-Object | Select-Object -ExpandProperty Count
        Write-Information "Exclude fields filtering applied; $codeProcessCount language codes found."
    }
    $codeSet | Out-SourceCode -CodeProcessCount $codeProcessCount -LanguageNamesIndex $languageNamesIndex -MacrolanguageMappings $macrolanguageMappings -PublishedDate $usePublishedDate -GenerateTypeName $SourceCodeTypeName -GenerateTypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage -ExcludeFieldNamePrefix $ExcludeFieldsStartingWith -IncludeFieldNamePrefix $IncludeFieldsStartingWith -GenerateMainBody (-not $PSBoundParameters.ContainsKey('ExcludeTypeMainBody')) -GeneratePartialType $PSBoundParameters.ContainsKey('MakeSourceCodeTypePartial')
}
finally {
    Remove-Module StringEnumCodeGen
    Set-PSDebug -Off
}

# Follow-up instructions.
Write-Information 'Next steps:'
Write-Information "*`tMake the $SourceCodeTypeName type readonly."
Write-Information "*`tMove the preprocessor directives at the top of the file to below the headline comment block.  This may help to minimise or eliminate spurious warnings."