#############################################################################
# Title: Tz Database Enum Source Code Generator                             #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.           #
#                                                                           #
# Purpose: Generate source code for a TZ Database enum type.                #
# Source: Time Zone Database, IANA.                                         #
# https://www.iana.org/time-zones                                           #
#############################################################################
#Requires -Version 7.4

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]    $SourceFolderPath,

    [Parameter(Mandatory, HelpMessage = 'Name of the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeName,

    [Parameter(HelpMessage = 'Inline comment to be applied to the enum type in the generated source code.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeTypeComment,

    [Parameter(HelpMessage = 'Language of the source code to be generated.  WARNING: Use of this parameter to specify a source code language other than C# is not fully supported.')]
    [ValidateNotNullOrWhiteSpace()]
    [string]    $SourceCodeLanguage = 'CSharp'
)

function Get-HeaderFieldNames {
    [OutputType([string[]])]
    param (
        [Parameter()]
        [string[]]        $FileLines
    )

    $headerBlankCommentLineNumber = -1
    $headerCommentLineNumber = 0
    $headerLineNumber = 0
    $headerLine = $FileLines[$headerLineNumber]
    while ($headerLine.StartsWith('#')) {
        if ($headerLine.StartsWith('#')) {
            $headerCommentLineNumber = $headerLineNumber
        }
        if ([string]::IsNullOrWhiteSpace($headerLine.TrimStart('#'))) {
            $headerBlankCommentLineNumber = $headerLineNumber
        }

        $headerLine = $FileLines[++$headerLineNumber]
    }
    [string[]]$headerFieldNames = @()
    $headerLineCount = 0
    $FileLines | Select-Object -Skip ($headerBlankCommentLineNumber + 1) | ForEach-Object {
        if (++$headerLineCount -gt ($headerCommentLineNumber - $headerBlankCommentLineNumber)) {
            return;
        }

        $headerFieldValues = $_ -split "`t"
        while ($headerFieldNames.Length -lt $headerFieldValues.Length) {
            $headerFieldNames += [string]::Empty
        }
        for ($headerFieldIndex = 0; $headerFieldIndex -lt $headerFieldValues.Count; $headerFieldIndex++) {
            $headerFieldNames[$headerFieldIndex] += $headerFieldValues[$headerFieldIndex].TrimStart('#')
        }
    }

    return $headerFieldNames
}

function Get-HeaderLineCount {
    param (
        [Parameter()]
        [string[]]        $FileLines
    )
    
    $fileHeaderLineCount = 0
    $fileLineIndex = 0
    while ($FileLines[$fileLineIndex++].StartsWith('#')) {
        $fileHeaderLineCount++;
    }

    return $fileHeaderLineCount
}

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
        [int]        $CodeCount,

        [Parameter()]
        [hashtable]        $CountryCodeTable,

        [Parameter()]
        [string]    $TypeName,

        [Parameter()]
        [string]    $TypeComment,

        [Parameter()]
        [string]    $GenerateLanguage,

        [Parameter()]
        [string]        $TzDataVersion
    )
    
    begin {
        $activity = "Generating $TypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        $codesProcessed = 0

        $preprocessorDirectives = '#if NETCOREAPP3_0_OR_GREATER
#nullable enable
#endif'
        $preprocessorDirectivesCompileUnit = [System.CodeDom.CodeSnippetCompileUnit]::new($preprocessorDirectives)
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
        $compileUnits = @($preprocessorDirectivesCompileUnit, $compileUnit)

        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Chronology')
        [void]$compileUnit.Namespaces.Add($namespace)
        [void]$namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Linq'))
        [void]$namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Reflection'))

        Write-Progress -Activity $activity -CurrentOperation 'Declaring enum type' -PercentComplete -1
    
        $structType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
        [void]$structType.BaseTypes.Add([System.CodeDom.CodeTypeReference]::new([System.IComparable]))
        [void]$structType.BaseTypes.Add([System.CodeDom.CodeTypeReference]::new('System.IEquatable', [System.CodeDom.CodeTypeReference[]]@([System.CodeDom.CodeTypeReference]::new("DataStandardizer.Chronology.$TypeName"))))
        $structType.IsStruct = $true
        $structType.TypeAttributes = [System.Reflection.TypeAttributes]::Public

        if (-not [string]::IsNullOrEmpty($TypeComment)) {
            $enumTypeOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
            $enumTypeSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($TypeComment, $true)
            $enumTypeCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
            $structType.Comments.AddRange(@($enumTypeOpenSummaryComment, $enumTypeSummaryContentComment, $enumTypeCloseSummaryComment))
        }

        if (-not [string]::IsNullOrEmpty($TzDataVersion)) {
            $enumTypeOpenRemarksComment = [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
            $enumTypeRemarksContentComment = [System.CodeDom.CodeCommentStatement]::new("Based on TZ Database version $TzDataVersion.", $true)
            $enumTypeCloseRemarksComment = [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
            $structType.Comments.AddRange(@($enumTypeOpenRemarksComment, $enumTypeRemarksContentComment, $enumTypeCloseRemarksComment))
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
            public static explicit operator DataStandardizer.Chronology.$TypeName(string value)
    #else
            public static explicit operator DataStandardizer.Chronology.$TypeName([JetBrains.Annotations.NotNullAttribute] string value)
    #endif
            {
                return new DataStandardizer.Chronology.$TypeName(value);
            }"
        $stringToStructConversionOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($stringToStructConversionOperatorSnippet)
        [void]$stringToStructConversionOperatorSnippetMember.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Operators'))
        [void]$structType.Members.Add($stringToStructConversionOperatorSnippetMember)
    
        $structToStringConversionOperatorSnippet = "#if NETCOREAPP3_0_OR_GREATER
            public static implicit operator string?(DataStandardizer.Chronology.$TypeName value)
    #else
            [JetBrains.Annotations.CanBeNullAttribute]
            public static implicit operator string(DataStandardizer.Chronology.$TypeName value)
    #endif
            {
                return value._value;
            }"
        [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($structToStringConversionOperatorSnippet))
    
        $equalityOperatorSnippet = "        public static bool operator ==(DataStandardizer.Chronology.$TypeName left, DataStandardizer.Chronology.$TypeName right)
            {
                return left.Equals(right);
            }"
        [void]$structType.Members.Add([System.CodeDom.CodeSnippetTypeMember]::new($equalityOperatorSnippet))
    
        $inequalityOperatorSnippet = "        public static bool operator !=(DataStandardizer.Chronology.$TypeName left, DataStandardizer.Chronology.$TypeName right)
            {
                return !left.Equals(right);
            }"
        $inequalityOperatorSnippetMember = [System.CodeDom.CodeSnippetTypeMember]::new($inequalityOperatorSnippet)
        [void]$inequalityOperatorSnippetMember.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        [void]$structType.Members.Add($inequalityOperatorSnippetMember)

        [void]$namespace.Types.Add($structType)

        $memberHostTypes = [ordered]@{}
    }

    process {
        $status = "Evaluating timezone $($_.TZ)"
        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding timezones' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        [string[]]$timezoneIdentifierParts = @($_.TZ -split '/')
        [string[]]$hostIdentifierParts = [System.Linq.Enumerable]::ToArray([System.Linq.Enumerable]::Take($timezoneIdentifierParts, $timezoneIdentifierParts.Length - 1))
        $hostIdentifier = $hostIdentifierParts -join '/'

        # Get a host type for the member to be added to.
        [System.CodeDom.CodeTypeDeclaration]$memberHostType = $null
        if ($memberHostTypes.Contains($hostIdentifier)) {
            $memberHostType = $memberHostTypes[$hostIdentifier]
        }
        else {
            $hostTypeName = $hostIdentifierParts | Select-Object -Last 1
            $memberHostType = [System.CodeDom.CodeTypeDeclaration]::new($hostTypeName)
            $memberHostType.IsClass = $true
            $memberHostType.TypeAttributes = [System.Reflection.TypeAttributes]::NestedPublic -bor [System.Reflection.TypeAttributes]::Sealed -bor [System.Reflection.TypeAttributes]::Abstract
            $memberHostTypes[$hostIdentifier] = $memberHostType
        }

        # Add a member for the timezone.
        [string]$enumFieldName = $timezoneIdentifierParts | Select-Object -Last 1
        $enumFieldName = $enumFieldName -replace '-', '_'
        $enumField = [System.CodeDom.CodeMemberField]::new("readonly DataStandardizer.Chronology.$TypeName", $enumFieldName)
        $enumField.Attributes = [System.CodeDom.MemberAttributes]::Public -bor [System.CodeDom.MemberAttributes]::Static
        $enumField.InitExpression = [System.CodeDom.CodeObjectCreateExpression]::new("DataStandardizer.Chronology.$TypeName", @([System.CodeDom.CodePrimitiveExpression]::new($_.TZ)))
        [void]$memberHostType.Members.Add($enumField)

        $coordinateMatch = $_.coordinates | Select-String -Pattern '^(?:(?<latitude>[-\+](?<latitudeDegrees>\d{2})(?<latitudeMinutes>\d{2}))(?<longitude>[-\+](?<longitudeDegrees>\d{3})(?<longitudeMinutes>\d{2}))|(?<latitude>[-\+](?<latitudeDegrees>\d{2})(?<latitudeMinutes>\d{2})(?<latitudeSeconds>\d{2}))(?<longitude>[-\+](?<longitudeDegrees>\d{3})(?<longitudeMinutes>\d{2})(?<longitudeSeconds>\d{2})))$'
        $coordinateGroups = $coordinateMatch | Select-Object -ExpandProperty Matches | Select-Object -ExpandProperty Groups
        $coordinateLatitudeDMS = $coordinateGroups | Where-Object { $_.Name -eq 'latitude' -and $_.Success } | Select-Object -ExpandProperty Value
        [int]$coordinateLatitudeDegrees = $coordinateGroups | Where-Object { $_.Name -eq 'latitudeDegrees' -and $_.Success } | Select-Object -ExpandProperty Value
        [int]$coordinateLatitudeMinutes = $coordinateGroups | Where-Object { $_.Name -eq 'latitudeMinutes' -and $_.Success } | Select-Object -ExpandProperty Value
        [int]$coordinateLatitudeSeconds = $coordinateGroups | Where-Object { $_.Name -eq 'latitudeSeconds' -and $_.Success } | Select-Object -ExpandProperty Value
        $coordinateLongitudeDMS = $coordinateGroups | Where-Object { $_.Name -eq 'longitude' } | Select-Object -ExpandProperty Value
        [int]$coordinateLongitudeDegrees = $coordinateGroups | Where-Object { $_.Name -eq 'longitudeDegrees' -and $_.Success } | Select-Object -ExpandProperty Value
        [int]$coordinateLongitudeMinutes = $coordinateGroups | Where-Object { $_.Name -eq 'longitudeMinutes' -and $_.Success } | Select-Object -ExpandProperty Value
        [int]$coordinateLongitudeSeconds = $coordinateGroups | Where-Object { $_.Name -eq 'longitudeSeconds' -and $_.Success } | Select-Object -ExpandProperty Value

        [double]$coordinateLatitude = ($coordinateLatitudeDegrees + ($coordinateLatitudeMinutes / 60) + ($coordinateLatitudeSeconds / 3600))
        if ($coordinateLatitudeDMS.StartsWith('-')) {
            $coordinateLatitude *= -1
        }
        [double]$coordinateLongitude = ($coordinateLongitudeDegrees + ($coordinateLongitudeMinutes / 60) + ($coordinateLongitudeSeconds / 3600))
        if ($coordinateLongitudeDMS.StartsWith('-')) {
            $coordinateLongitude *= -1
        }
        [System.CodeDom.CodeAttributeArgument[]]$enumFieldAttributeArguments = @([System.CodeDom.CodePrimitiveExpression]::new($coordinateLatitude), [System.CodeDom.CodePrimitiveExpression]::new($coordinateLongitude))
        $countryCodes = $_.'country-codes' -split ','
        foreach ($countryCode in $countryCodes) {
            $enumFieldAttributeArguments += [System.CodeDom.CodePrimitiveExpression]::new($countryCode)
        }
        if (-not [string]::IsNullOrWhiteSpace($_.comments)) {
            $enumFieldAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Comment', [System.CodeDom.CodePrimitiveExpression]::new($_.comments))
        }
        $enumFieldAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.Chronology.TzDataTimezoneAttribute', $enumFieldAttributeArguments)
        [void]$enumField.CustomAttributes.Add($enumFieldAttribute)
        
        $summaryOpenComment = [System.CodeDom.CodeComment]::new('<summary>', $true)
        $summaryContentComment = [System.CodeDom.CodeComment]::new($_.TZ, $true)
        $summaryCloseComment = [System.CodeDom.CodeComment]::new('</summary>', $true)
        @($summaryOpenComment, $summaryContentComment, $summaryCloseComment) | ForEach-Object { [void]$enumField.Comments.Add([System.CodeDom.CodeCommentStatement]::new($_)) }

        [System.CodeDom.CodeComment[]]$remarksComments = @()
        $remarksComments += [System.CodeDom.CodeComment]::new('<remarks>', $true)
        $remarksComments += [System.CodeDom.CodeComment]::new('Used in the following countries:', $true)
        $remarksComments += [System.CodeDom.CodeComment]::new("`t<list type=""bullet"">", $true)
        $remarksComments += [System.CodeDom.CodeComment]::new("`t`t<listheader>", $true)
        $remarksComments += [System.CodeDom.CodeComment]::new("`t`t`t<term>Code</term>", $true)
        $remarksComments += [System.CodeDom.CodeComment]::new("`t`t`t<description>Country Name</description>", $true)
        $remarksComments += [System.CodeDom.CodeComment]::new("`t`t</listheader>", $true)
        $countryCodes | ForEach-Object {
            $countryName = $CountryCodeTable[$_]

            $remarksComments += [System.CodeDom.CodeComment]::new("`t`t<item>", $true)
            $remarksComments += [System.CodeDom.CodeComment]::new("`t`t`t<term>$_</term>", $true)
            $remarksComments += [System.CodeDom.CodeComment]::new("`t`t`t<description>$countryName</description>", $true)
            $remarksComments += [System.CodeDom.CodeComment]::new("`t`t</item>", $true)
        }
        $remarksComments += [System.CodeDom.CodeComment]::new("`t</list>", $true)
        $remarksComments += [System.CodeDom.CodeComment]::new('</remarks>', $true)
        $remarksComments | ForEach-Object { [void]$enumField.Comments.Add([System.CodeDom.CodeCommentStatement]::new($_)) }

        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeCount) * 100)
    }

    end {
        # Add member host types to struct.
        $topLevelHostTypes = @()
        foreach ($hostType in $memberHostTypes.GetEnumerator()) {
            $hostIdentifierParts = $hostType.Key -split '/'
            if ($hostIdentifierParts.Length -lt 2) {
                [void]$structType.Members.Add($hostType.Value)
                $topLevelHostTypes += $hostType.Value
            }
            else {
                [string[]]$parentIdentifierParts = [System.Linq.Enumerable]::ToArray([System.Linq.Enumerable]::Take($hostIdentifierParts, $hostIdentifierParts.Length - 1))
                $parentIdentifier = $parentIdentifierParts -join '/'

                if ($memberHostTypes.Contains($parentIdentifier)) {
                    $parentType = $memberHostTypes[$parentIdentifier]
                    [void]$parentType.Members.Add($hostType.Value)
                }
            }
        }
        $topLevelHostTypes | Select-Object -First 1 | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Public Fields')) }
        $topLevelHostTypes | Select-Object -Last 1 | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }

        # Declare public methods.
        Write-Progress -Activity $activity -CurrentOperation 'Declaring public methods' -PercentComplete -1

        [System.CodeDom.CodeTypeMember[]]$publicMethods = @(
            (Get-EqualsMethodDefinition -TypeNamespace 'DataStandardizer.Chronology' -TypeName $TypeName),
            (Get-GetHashCodeMethodDefinition),
            [System.CodeDom.CodeSnippetTypeMember]::new('#if NETCOREAPP3_0_OR_GREATER'),
            (Get-CompareToMethodDefinition -UseNullableReferenceTypes),
            (Get-InheritedEqualsMethodDefinition -TypeNamespace 'DataStandardizer.Chronology' -TypeName $TypeName -UseNullableReferenceTypes),
            (Get-SpecialToStringMethodDefinition -UseNullableReferenceTypes),
            [System.CodeDom.CodeSnippetTypeMember]::new('#else'),
            (Get-CompareToMethodDefinition),
            (Get-InheritedEqualsMethodDefinition -TypeNamespace 'DataStandardizer.Chronology' -TypeName $TypeName),
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
            (Get-MemberFieldPredicateMethodDefinition -TypeNamespace 'DataStandardizer.Chronology' -TypeName $TypeName -GenerateLanguage $GenerateLanguage),
            (Get-MemberFieldDeclaredFieldsPredicateMethodDefinition)
        )
        $privateMethods | Select-Object -First 1 | ForEach-Object { [void]$_.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Private Methods')) }
        $privateMethods | Select-Object -Last 1 | ForEach-Object { [void]$_.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty)) }

        $structType.Members.AddRange($privateMethods)

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
                        elseif ($sourceCodeLine.TrimStart().StartsWith("public struct $TypeName")) {
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

                Write-Output $sourceCode
            }
            finally {
                $writer.Close()
            }
        }
    }
}

# Validate parameters.
if (-not (Test-Path -Path $SourceFolderPath -PathType Container)) {
    Write-Error "Source folder $SourceFolderPath not found."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    $modulePath = Resolve-Path scripts\StringEnumCodeGen\StringEnumCodeGen.psm1
    Import-Module (Split-Path $modulePath -Parent)

    $zonesFilePath = $SourceFolderPath | Join-Path -ChildPath 'zone1970.tab'
    if (Test-Path $zonesFilePath -PathType Leaf) {
        $zonesFileLines = Get-Content -Path $zonesFilePath
    }
    else {
        Write-Error "Source file '$zonesFilePath' not found."
    }

    $iso3166FilePath = $SourceFolderPath | Join-Path -ChildPath 'iso3166.tab'
    if (Test-Path $iso3166FilePath -PathType Leaf) {
        $iso3166FileLines = Get-Content -Path $iso3166FilePath
    }
    else {
        Write-Error "Source file '$iso3166FilePath' not found."
    }

    [string]$tzDataVersion = $null
    $versionFilePath = $SourceFolderPath | Join-Path -ChildPath 'version'
    if (Test-Path $versionFilePath -PathType Leaf) {
        $versionFileLines = Get-Content -Path $versionFilePath
        $tzDataVersion = $versionFileLines | Select-Object -First 1
    }
    else {
        Write-Warning "Source file '$versionFilePath' not found."
    }

    # Convert country codes to hash table.
    $iso3166FileHeaderLineCount = Get-HeaderLineCount $iso3166FileLines
    $iso3166FileHeaderFieldNames = Get-HeaderFieldNames $iso3166FileLines
    $countryCodes = $iso3166FileLines | Select-Object -Skip $iso3166FileHeaderLineCount | ConvertFrom-Csv -Header $iso3166FileHeaderFieldNames -Delimiter "`t"
    $countryCodeTable = @{}
    foreach ($item in $countryCodes) {
        $key = $item.'country-code'
        $value = $item.'name of country, territory, area, or subdivision'
        if ($key) {
            $countryCodeTable[$key] = $value
        }
        else {
            Write-Warning "Skipped item with null country-code: $($item | Out-String)"
        }
    }

    # Process timezone lines.
    $zonesFileHeaderLineCount = Get-HeaderLineCount $zonesFileLines
    $zonesFileHeaderFieldNames = Get-HeaderFieldNames $zonesFileLines
    $timezoneLines = $zonesFileLines | Select-Object -Skip $zonesFileHeaderLineCount | Where-Object { -not $_.StartsWith('#') }
    $timezoneCount = $timezoneLines | Measure-Object | Select-Object -ExpandProperty Count
    $timezoneLines | ConvertFrom-Csv -Delimiter "`t" -Header $zonesFileHeaderFieldNames | Out-SourceCode -CodeCount $timezoneCount -CountryCodeTable $countryCodeTable -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage -TzDataVersion $tzDataVersion
}
finally {
    Remove-Module StringEnumCodeGen
    Set-PSDebug -Off
}

Write-Information 'Next steps:'
Write-Information "*`tMake the $SourceCodeTypeName type readonly."
Write-Information "*`tReplace all 'public sealed abstract class' declarations with 'public static class'."