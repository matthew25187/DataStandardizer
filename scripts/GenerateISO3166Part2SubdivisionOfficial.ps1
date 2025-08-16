#########################################################################
# Title: ISO 3166 Part 2 Source Code Generator                          #
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

    [Parameter(HelpMessage = 'Include subdivision fields for the nominated country code.')]
    [string]    $IncludeFieldsCountryCode,

    [Parameter(Mandatory, HelpMessage = 'Name of the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeLanguage = 'CSharp',

    [Parameter()]
    [ValidateRange(0, 2)]
    [int]   $TraceLevel = 0
)

function Get-MemberFieldDeclaredFieldsPredicateMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    
    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = 'MemberFieldDeclaredFieldsPredicate'
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Private
    [void]$method.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new([System.CodeDom.CodeTypeReference]::new([System.Reflection.TypeInfo]), 'type'))
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([System.Collections.Generic.IEnumerable[System.Reflection.FieldInfo]])

    # Define method statements.
    [void]$method.Statements.Add([System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeArgumentReferenceExpression]::new('type'), 'DeclaredFields')))

    return $method
}

function Get-SpecialToStringMethodDefinition {
    [OutputType([System.CodeDom.CodeMemberMethod])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )
    
    $methodName = 'ToString'
    $memberFieldVariableName = 'memberField'
    $resultVariableName = 'result'

    # Declare method.
    $method = [System.CodeDom.CodeMemberMethod]::new()
    $method.Name = $methodName
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
    $method.Attributes = ($method.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Override
    $method.ReturnType = [System.CodeDom.CodeTypeReference]::new([string])

    # Define method statements.
    $nestedTypesVariableDeclarationStatement = [System.CodeDom.CodeVariableDeclarationStatement]::new([System.Collections.Generic.IEnumerable[System.Reflection.TypeInfo]], 'nestedTypes', [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType', @()), 'GetTypeInfo', @()), 'DeclaredNestedTypes'))
    $memberFieldVariableDeclarationStatement = [System.CodeDom.CodeVariableDeclarationStatement]::new('var', 'memberField', [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeVariableReferenceExpression]::new('nestedTypes'), 'SelectMany', @([System.CodeDom.CodeMethodReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'MemberFieldDeclaredFieldsPredicate'))), 'FirstOrDefault', @([System.CodeDom.CodeMethodReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'MemberFieldPredicate'))))
    $result1Statement = [System.CodeDom.CodeVariableDeclarationStatement]::new(($PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? [System.CodeDom.CodeTypeReference]::new('string?'):[System.CodeDom.CodeTypeReference]::new([string])), $resultVariableName, [System.CodeDom.CodePrimitiveExpression]::new($null))
    $result2Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($memberFieldVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::IdentityInequality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeVariableReferenceExpression]::new($memberFieldVariableName), 'Name'))))
    $result3Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_value'))))
    $result4Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeBaseReferenceExpression]::new(), $methodName, @()))))
    $result5Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType', @()), 'FullName'))))
    $result6Statement = [System.CodeDom.CodeConditionStatement]::new(
        [System.CodeDom.CodeBinaryOperatorExpression]::new(
            [System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName),
            [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
            [System.CodeDom.CodePrimitiveExpression]::new($null)),
        @([System.CodeDom.CodeAssignStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName), [System.CodeDom.CodePropertyReferenceExpression]::new([System.CodeDom.CodeMethodInvokeExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), 'GetType'), 'Name'))))
    $returnStatement = [System.CodeDom.CodeMethodReturnStatement]::new([System.CodeDom.CodeVariableReferenceExpression]::new($resultVariableName))
    [System.CodeDom.CodeStatement[]]$methodStatements = @($nestedTypesVariableDeclarationStatement, $memberFieldVariableDeclarationStatement, $result1Statement, $result2Statement, $result3Statement, $result4Statement, $result5Statement, $result6Statement, $returnStatement)
    $method.Statements.AddRange($methodStatements)

    return $method
}

function Out-SourceCode {
    param (
        [Parameter(ValueFromPipeline)]
        [psobject[]]    $InputObject,
    
        [Parameter()]
        [int]   $CodeCount,

        [Parameter()]
        [bool]  $IncludeMainBody,

        [Parameter()]
        [string]    $IncludeFieldsForCountryCode,

        [Parameter()]
        [psobject[]]    $SubdivisionNamesSet,

        [Parameter()]
        [psobject[]]    $SubdivisionCategoriesSet,

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
        $subdivisionCategoryIdentifierKey = 'category_id'

        $activity = "Generating $TypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        $codesProcessed = 0

        $preprocessorDirectives = '#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif'
        $preprocessorDirectivesCompileUnit = [System.CodeDom.CodeSnippetCompileUnit]::new($preprocessorDirectives)
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
        $compileUnits = @($compileUnit)
        if ($IncludeMainBody) {
            $compileUnits = @($preprocessorDirectivesCompileUnit, $compileUnit)
        }

        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.ISO3166')
        [void]$compileUnit.Namespaces.Add($namespace)
        if ($IncludeMainBody) {
            [void]$namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Linq'))
            [void]$namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Reflection'))
        }

        Write-Progress -Activity $activity -CurrentOperation 'Declaring enum type' -PercentComplete -1
    
        $structType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
        if ($IncludeMainBody) {
            [void]$structType.BaseTypes.Add([System.CodeDom.CodeTypeReference]::new([System.IComparable]))
            [void]$structType.BaseTypes.Add([System.CodeDom.CodeTypeReference]::new('System.IEquatable', [System.CodeDom.CodeTypeReference[]]@([System.CodeDom.CodeTypeReference]::new("DataStandardizer.ISO3166.$TypeName"))))
        }
        $structType.IsStruct = $true
        $structType.IsPartial = $true
        $structType.TypeAttributes = [System.Reflection.TypeAttributes]::Public

        if (-not [string]::IsNullOrEmpty($TypeComment)) {
            $enumTypeOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
            $enumTypeSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($TypeComment, $true)
            $enumTypeCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
            $structType.Comments.AddRange(@($enumTypeOpenSummaryComment, $enumTypeSummaryContentComment, $enumTypeCloseSummaryComment))
        }

        if ($IncludeMainBody) {
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
            public static explicit operator DataStandardizer.ISO3166.$TypeName(string value)
    #else
            public static explicit operator DataStandardizer.ISO3166.$TypeName([JetBrains.Annotations.NotNullAttribute] string value)
    #endif
            {
                return new DataStandardizer.ISO3166.$TypeName(value);
            }"
            $stringToStructConversionOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($stringToStructConversionOperatorSnippet)
            [void]$stringToStructConversionOperatorSnippetMember.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Operators'))
            [void]$structType.Members.Add($stringToStructConversionOperatorSnippetMember)
    
            $structToStringConversionOperatorSnippet = "#if NETCOREAPP3_0_OR_GREATER
            public static implicit operator string?(DataStandardizer.ISO3166.$TypeName value)
    #else
            [JetBrains.Annotations.CanBeNullAttribute]
            public static implicit operator string(DataStandardizer.ISO3166.$TypeName value)
    #endif
            {
                return value._value;
            }"
            [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($structToStringConversionOperatorSnippet))
    
            $equalityOperatorSnippet = "        public static bool operator ==(DataStandardizer.ISO3166.$TypeName left, DataStandardizer.ISO3166.$TypeName right)
            {
                return left.Equals(right);
            }"
            [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($equalityOperatorSnippet))
    
            $inequalityOperatorSnippet = "        public static bool operator !=(DataStandardizer.ISO3166.$TypeName left, DataStandardizer.ISO3166.$TypeName right)
            {
                return !left.Equals(right);
            }"
            $inequalityOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($inequalityOperatorSnippet)
            [void]$inequalityOperatorSnippetMember.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
            [void]$structType.Members.Add($inequalityOperatorSnippetMember)
        }

        [void]$namespace.Types.Add($structType)

        if (-not [string]::IsNullOrEmpty($IncludeFieldsForCountryCode)) {
            # Declare nested subdivision codes type.
            $subdivisionCodesNestedClassType = [System.CodeDom.CodeTypeDeclaration]::new($IncludeFieldsForCountryCode)
            $subdivisionCodesNestedClassType.Attributes = ($subdivisionCodesNestedClassType.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Public
            $subdivisionCodesNestedClassType.Attributes = ($subdivisionCodesNestedClassType.Attributes -band -bnot [System.CodeDom.MemberAttributes]::ScopeMask) -bor [System.CodeDom.MemberAttributes]::Static

            # Add subdivision category attributes.
            $subdivisionCategoriesCurrentSet = $SubdivisionCategoriesSet | Where-Object -Property alpha_2_code -EQ $IncludeFieldsForCountryCode
            foreach ($subdivisionCategory in $subdivisionCategoriesCurrentSet) {
                $subdivisionCategoryLanguage = (-not [string]::IsNullOrWhiteSpace($subdivisionCategory.language))?$subdivisionCategory.language:$null
                $subdivisionCategoryAttributeArguments = @(
                    [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionCategoryLanguage)),
                    [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionCategory.language_alpha_3_code)),
                    [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionCategory.category_id -as [ushort])),
                    [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionCategory.category_name.Trim())))
                if (-not [string]::IsNullOrWhiteSpace($subdivisionCategory.category_name_plural)) {
                    $subdivisionCategoryAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionCategory.category_name_plural.Trim()))
                }
                $subdivisionCategoryAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166SubdivisionCategoryNameAttribute', $subdivisionCategoryAttributeArguments)
                [void]$subdivisionCodesNestedClassType.CustomAttributes.Add($subdivisionCategoryAttribute)
            }

            # Add language attributes.
            $languagesCurrentSet = $LanguagesSet | Where-Object -Property alpha_2_code -EQ $IncludeFieldsForCountryCode
            foreach ($language in $languagesCurrentSet) {
                $languageAlpha2Code = (-not [string]::IsNullOrWhiteSpace($language.language_alpha_2_code))?$language.language_alpha_2_code:$null
                $languageAttributeArguments = @(
                    [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($languageAlpha2Code)),
                    [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($language.language_alpha_3_code)))

                switch ($language.is_administrative) {
                    'NO' { $isAdministrative = $false }
                    'YES' { $isAdministrative = $true }
                }
                $languageAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('IsAdministrative', [System.CodeDom.CodePrimitiveExpression]::new($isAdministrative))

                $languageAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166LanguageAttribute', $languageAttributeArguments)
                [void]$subdivisionCodesNestedClassType.CustomAttributes.Add($languageAttribute)
            }

            [void]$structType.Members.Add($subdivisionCodesNestedClassType)
        }
    }

    process {
        if ($_.alpha_2_code -ne $IncludeFieldsForCountryCode) {
            return;
        }

        $status = "Evaluating country code $($_.alpha_2_code)"

        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding subdivision codes' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        # Declare field.
        $enumFieldName = $_.subdivision_code -split '-' | Select-Object -Last 1 | ForEach-Object { "_$_" }
        $enumField = [System.CodeDom.CodeMemberField]::new("readonly DataStandardizer.ISO3166.$TypeName", $enumFieldName)
        $enumField.Attributes = [System.CodeDom.MemberAttributes]::Public -bor [System.CodeDom.MemberAttributes]::Static
        $enumField.InitExpression = [System.CodeDom.CodeObjectCreateExpression]::new("DataStandardizer.ISO3166.$TypeName", @([System.CodeDom.CodePrimitiveExpression]::new($_.subdivision_code)))
        [void]$enumField.UserData.Add($subdivisionCategoryIdentifierKey, $_.subdivision_category_id -as [ushort])

        # Add code attribute.
        $codeAttributeArguments = @(
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.subdivision_category_id -as [ushort])),
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.subdivision_code)))
        if (-not [string]::IsNullOrWhiteSpace($_.subdivision_parent)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.subdivision_parent))
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166SubdivisionCodeAttribute', $codeAttributeArguments)
        [void]$enumField.CustomAttributes.Add($codeAttribute)

        # Add name attributes.
        $subdivisionCode = $_.subdivision_code
        $subdivisionNamesCurrentSet = $SubdivisionNamesSet | Where-Object { $_.alpha_2_code -eq $IncludeFieldsForCountryCode -and $_.subdivision_code -eq $subdivisionCode }
        foreach ($subdivisionName in $subdivisionNamesCurrentSet) {
            $subdivisionNameLanguage = (-not [string]::IsNullOrWhiteSpace($subdivisionName.language))?$subdivisionName.language:$null
            $subdivisionNameAttributeArguments = @(
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionNameLanguage)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionName.language_alpha_3_code)),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionName.subdivision_category_id -as [ushort])),
                [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($subdivisionName.subdivision_name)))
            if (-not [string]::IsNullOrWhiteSpace($subdivisionName.subdivision_name_local_variation)) {
                $subdivisionNameAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('SubdivisionNameLocalVariant', [System.CodeDom.CodePrimitiveExpression]::new($subdivisionName.subdivision_name_local_variation))
            }
            if (-not [string]::IsNullOrWhiteSpace($subdivisionName.romanization_system)) {
                $subdivisionNameAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('RomanizationSystem', [System.CodeDom.CodePrimitiveExpression]::new($subdivisionName.romanization_system))
            }
            $subdivisionNameAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.ISO3166.Iso3166SubdivisionNameAttribute', $subdivisionNameAttributeArguments)
            [void]$enumField.CustomAttributes.Add($subdivisionNameAttribute)
        }
        
        # Add summary comment.
        $englishName = $subdivisionNamesCurrentSet | Where-Object -Property language -EQ 'en' | Select-Object -ExpandProperty subdivision_name | ForEach-Object { (-not [string]::IsNullOrWhiteSpace($_))?($_):$null }
        $preferredLanguage = $languagesCurrentSet | Where-Object -Property is_administrative -EQ $true | Select-Object -First 1 -ExpandProperty language_alpha_3_code
        $preferredName = $subdivisionNamesCurrentSet | Where-Object -Property language_alpha_3_code -EQ $preferredLanguage | Select-Object -ExpandProperty subdivision_name | ForEach-Object { (-not [string]::IsNullOrWhiteSpace($_))?($_):$null }
        $fallbackName = $subdivisionNamesCurrentSet | Select-Object -First 1 -ExpandProperty subdivision_name | ForEach-Object { (-not [string]::IsNullOrWhiteSpace($_))?($_):$null }
        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($englishName ?? $preferredName ?? $fallbackName, $true)
        $enumFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $enumField.Comments.AddRange(@($enumFieldOpenSummaryComment, $enumFieldSummaryContentComment, $enumFieldCloseSummaryComment))

        [void]$subdivisionCodesNestedClassType.Members.Add($enumField)

        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeCount) * 100)
    }

    end {
        $subdivisionCategoryIdentifiers = $subdivisionCategoriesCurrentSet | Select-Object -ExpandProperty category_id -Unique
        foreach ($subdivisionCategoryIdentifier in $subdivisionCategoryIdentifiers) {
            $englishName = $subdivisionCategoriesCurrentSet | Where-Object { $_.category_id -eq $subdivisionCategoryIdentifier -and $_.language -eq 'en' } | Select-Object -ExpandProperty category_name | ForEach-Object { -not [string]::IsNullOrWhiteSpace($_)?($_):$null }
            $englishNamePlural = $subdivisionCategoriesCurrentSet | Where-Object { $_.category_id -eq $subdivisionCategoryIdentifier -and $_.language -eq 'en' } | Select-Object -ExpandProperty category_name_plural | ForEach-Object { -not [string]::IsNullOrWhiteSpace($_)?($_):$null }
            $preferredLanguage = $languagesCurrentSet | Where-Object { $_.language_alpha_3_code -in ($subdivisionCategoriesCurrentSet | Select-Object -ExpandProperty language_alpha_3_code) -and $_.is_administrative -eq 'YES' } | Select-Object -First 1 -ExpandProperty language_alpha_3_code
            $preferredName = $subdivisionCategoriesCurrentSet | Where-Object { $_.category_id -eq $subdivisionCategoryIdentifier -and $_.language_alpha_3_code -eq $preferredLanguage } | Select-Object -ExpandProperty category_name | ForEach-Object { -not [string]::IsNullOrWhiteSpace($_)?($_):$null }
            $preferredNamePlural = $subdivisionCategoriesCurrentSet | Where-Object { $_.category_id -eq $subdivisionCategoryIdentifier -and $_.language_alpha_3_code -eq $preferredLanguage } | Select-Object -ExpandProperty category_name_plural | ForEach-Object { -not [string]::IsNullOrWhiteSpace($_)?($_):$null }
            $fallbackName = $subdivisionCategoriesCurrentSet | Where-Object -Property category_id -EQ $subdivisionCategoryIdentifier | Select-Object -First 1 -ExpandProperty category_name | ForEach-Object { -not [string]::IsNullOrWhiteSpace($_)?($_):$null }
            $fallbackNamePlural = $subdivisionCategoriesCurrentSet | Where-Object -Property category_id -EQ $subdivisionCategoryIdentifier | Select-Object -First 1 -ExpandProperty category_name_plural | ForEach-Object { -not [string]::IsNullOrWhiteSpace($_)?($_):$null }
            $subdivisionCategoryName = $englishNamePlural ?? $englishName ?? $preferredNamePlural ?? $preferredName ?? $fallbackNamePlural ?? $fallbackName
            
            $subdivisionCodesNestedClassType.Members
            | Where-Object { $_ -is [System.CodeDom.CodeMemberField] -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::AccessMask) -eq [System.CodeDom.MemberAttributes]::Public) -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::ScopeMask) -eq [System.CodeDom.MemberAttributes]::Static) -and $_.UserData[$subdivisionCategoryIdentifierKey] -eq $subdivisionCategoryIdentifier }
            | Select-Object -First 1
            | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, $subdivisionCategoryName)) }

            $subdivisionCodesNestedClassType.Members
            | Where-Object { $_ -is [System.CodeDom.CodeMemberField] -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::AccessMask) -eq [System.CodeDom.MemberAttributes]::Public) -and (($_.Attributes -band [System.CodeDom.MemberAttributes]::ScopeMask) -eq [System.CodeDom.MemberAttributes]::Static) -and $_.UserData[$subdivisionCategoryIdentifierKey] -eq $subdivisionCategoryIdentifier }
            | Select-Object -Last 1
            | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }
        }
        
        if ($IncludeMainBody) {
            # Declare public methods.
            Write-Progress -Activity $activity -CurrentOperation 'Declaring public methods' -PercentComplete -1

            [System.CodeDom.CodeTypeMember[]]$publicMethods = @(
                    (Get-EqualsMethodDefinition -TypeNamespace 'DataStandardizer.ISO3166' -TypeName $TypeName),
                    (Get-GetHashCodeMethodDefinition),
                [System.CodeDom.CodeSnippetTypeMember]::new('#if NETCOREAPP3_0_OR_GREATER'),
                    (Get-CompareToMethodDefinition -UseNullableReferenceTypes),
                    (Get-InheritedEqualsMethodDefinition -TypeNamespace 'DataStandardizer.ISO3166' -TypeName $TypeName -UseNullableReferenceTypes),
                    (Get-SpecialToStringMethodDefinition -UseNullableReferenceTypes),
                [System.CodeDom.CodeSnippetTypeMember]::new('#else'),
                    (Get-CompareToMethodDefinition),
                    (Get-InheritedEqualsMethodDefinition -TypeNamespace 'DataStandardizer.ISO3166' -TypeName $TypeName),
                    (Get-SpecialToStringMethodDefinition),
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
    
            [System.CodeDom.CodeTypeMember[]]$privateMethods = @(
                (Get-MemberFieldPredicateMethodDefinition -TypeNamespace 'DataStandardizer.ISO3166' -TypeName $TypeName -GenerateLanguage $GenerateLanguage),
                (Get-MemberFieldDeclaredFieldsPredicateMethodDefinition)
            )
            $privateMethods | Select-Object -First 1 | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Private Methods')) }
            $privateMethods | Select-Object -Last 1 | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }

            $structType.Members.AddRange($privateMethods)
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
            $sourceCodeBuilder = [System.Text.StringBuilder]::new()
            $includeConvertibleInterface = $false
            $writer = [System.IO.StringWriter]::new($sourceCodeCuBuilder)
            try {
                $provider.GenerateCodeFromCompileUnit($cu, $writer, $options)
                $sourceCode = $sourceCodeCuBuilder.ToString()
                
                # Add conditional logic for applying IConvertible interface.
                if ($IncludeMainBody) {
                    try {
                        $reader = [System.IO.StringReader]::new($sourceCode)

                        $sourceCodeLine = $reader.ReadLine()
                        while ($null -ne $sourceCodeLine) {
                            if ($includeConvertibleInterface) {
                                [void]$sourceCodeBuilder.AppendLine('#if NETSTANDARD1_3_OR_GREATER||NET')
                                [void]$sourceCodeBuilder.AppendLine(', System.IConvertible')
                                [void]$sourceCodeBuilder.AppendLine('#endif')
                                $includeConvertibleInterface = $false
                                continue;
                            }
                            elseif ($sourceCodeLine.TrimStart().StartsWith("public partial struct $TypeName")) {
                                $includeConvertibleInterface = $true
                            }
                            [void]$sourceCodeBuilder.AppendLine($sourceCodeLine)

                            $sourceCodeLine = $reader.ReadLine()
                        }

                        $sourceCode = $sourceCodeBuilder.ToString()
                    }
                    finally {
                        $reader.Close()
                    }
                }

                Write-Output $sourceCode
            }
            finally {
                $writer.Close()
            }
        }
    }
}

# Validate parameters.
if (-not (Test-Path $SourceFolderPath -PathType Container)) {
    Write-Error "Source folder $SourceFolderPath not found."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace $TraceLevel    # activate tracing here for debugging
try {
    $modulePath = Resolve-Path scripts\StringEnumCodeGen\StringEnumCodeGen.psm1
    Import-Module (Split-Path $modulePath -Parent)

    $subdivisionCodesFilePath = Join-Path $SourceFolderPath -ChildPath 'subdivisions.csv'
    if (Test-Path $subdivisionCodesFilePath -PathType Leaf) {
        $subdivisionCodesSet = Import-Csv $subdivisionCodesFilePath
    }
    else {
        Write-Error "Source file '$countryCodesFilePath' not found."
        exit;
    }

    $subdivisionNamesFilePath = Join-Path $SourceFolderPath -ChildPath 'subdivision-names.csv'
    if (Test-Path $subdivisionNamesFilePath -PathType Leaf) {
        $subdivisionNamesSet = Import-Csv $subdivisionNamesFilePath
    }
    else {
        Write-Error "Source file '$subdivisionNamesFilePath' not found."
        exit;
    }

    $subdivisionCategoriesFilePath = Join-Path $SourceFolderPath -ChildPath 'subdivision-categories.csv'
    if (Test-Path $subdivisionCategoriesFilePath -PathType Leaf) {
        $subdivisionCategoriesSet = Import-Csv $subdivisionCategoriesFilePath
    }
    else {
        Write-Error "Source file '$subdivisionCategoriesFilePath' not found."
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

    $codeCount = $subdivisionCodesSet | Where-Object { $_.alpha_2_code -eq $IncludeFieldsCountryCode -or $_.alpha_3_code -eq $IncludeFieldsCountryCode } | Measure-Object | Select-Object -ExpandProperty Count
    $subdivisionCodesSet | Sort-Object -Property subdivision_category_id | Out-SourceCode -CodeCount $codeCount -IncludeMainBody (-not $PSBoundParameters.ContainsKey('IncludeFieldsCountryCode')) -IncludeFieldsForCountryCode $IncludeFieldsCountryCode -SubdivisionNamesSet $subdivisionNamesSet -SubdivisionCategoriesSet $subdivisionCategoriesSet -LanguagesSet $languagesSet -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage
}
finally {
    Remove-Module StringEnumCodeGen
    Set-PSDebug -Off
}

Write-Information 'Next steps:'
Write-Information "*`tMove preprocessor directives (if any) to below the headline comment at the top of the file."
Write-Information "*`tMake the struct readonly."
Write-Information "*`tMake the nested class (if any) static."