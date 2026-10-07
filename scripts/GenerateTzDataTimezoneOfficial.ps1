#############################################################################
# Title: Tz Database Enum Source Code Generator                             #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.           #
#                                                                           #
# Purpose: Generate source code for a TZ Database enum type.                #
# Source: Time Zone Database, IANA.                                         #
# https://www.iana.org/time-zones                                           #
#############################################################################
#Requires -Version 7.4

<#
    .SYNOPSIS
    Generates the source code of the TzDataTimezone type from the TZ Database.

    .DESCRIPTION
    Generates a field for each timezone listed in zone1970.tab, carrying the timezone's coordinates, countries and
    comment, and its full history of zone lines.  The zone lines, and the daylight saving rule sets they reference,
    are parsed from the main-format region files and generated into two further partial source files.

    The following files of the TZ Database source (tzdata) are required in the source folder:

    - zone1970.tab, which lists the timezones to generate;
    - iso3166.tab, which names the countries each timezone is used in;
    - africa, antarctica, asia, australasia, europe, northamerica and southamerica, the main-format region files,
      which hold the Rule and Zone lines of every timezone listed in zone1970.tab.

    The file version, which identifies the release, is optional.

    Generation fails, rather than emitting bad data, where a timezone listed in zone1970.tab has no Zone, a zone
    line references a rule set that does not exist, any zone line but the last has no UNTIL, the UNTIL values of a
    zone do not strictly increase, or any line of the source cannot be parsed.

    The main source file is written to the output stream, and needs no further editing.  The script must be run
    from the root of the repository.

    .PARAMETER SourceFolderPath
    Path to the folder containing the extracted TZ Database source.

    .PARAMETER PartialFileFolderPath
    Path to the folder to which the partial source files <SourceCodeTypeName>.ZoneLineData.cs and
    <SourceCodeTypeName>.RuleSets.cs are written, replacing any existing files.

    .PARAMETER SourceCodeTypeName
    Name of the type in the generated source code.

    .PARAMETER SourceCodeTypeComment
    Inline comment to be applied to the type in the generated source code.

    .PARAMETER SourceCodeLanguage
    Language of the source code to be generated.

    .EXAMPLE
    ./scripts/GenerateTzDataTimezoneOfficial.ps1 -SourceFolderPath ~/tzdata -PartialFileFolderPath src/DataStandardizer.Chronology -SourceCodeTypeName TzDataTimezone -SourceCodeTypeComment 'Time Zone Database' > src/DataStandardizer.Chronology/TzDataTimezone.cs
#>
[CmdletBinding()]
param (
    [Parameter(Mandatory, HelpMessage = 'Path to the folder containing the extracted TZ Database source.')]
    [string]    $SourceFolderPath,

    [Parameter(Mandatory, HelpMessage = 'Path to the folder to which the zone line data and rule set partial source files are written.')]
    [string]    $PartialFileFolderPath,

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

function Get-ZoneLinesFieldDeclaration {
    [OutputType([System.CodeDom.CodeMemberField])]
    param (
        [Parameter()]
        [switch]    $UseNullableReferenceTypes
    )

    $typeExpression = $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')? 'readonly DataStandardizer.Chronology.TzDataZoneLine[]?':'readonly DataStandardizer.Chronology.TzDataZoneLine[]'
    $fieldDeclaration = [System.CodeDom.CodeMemberField]::new($typeExpression, '_zoneLines')
    $fieldDeclaration.Attributes = ($fieldDeclaration.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Private

    if (-not $PSBoundParameters.ContainsKey('UseNullableReferenceTypes')) {
        [void]$fieldDeclaration.CustomAttributes.Add([System.CodeDom.CodeAttributeDeclaration]::new('JetBrains.Annotations.CanBeNullAttribute'))
    }

    return $fieldDeclaration
}

function ConvertTo-FieldName {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $Name,

        [Parameter(Mandatory)]
        [System.CodeDom.Compiler.CodeDomProvider]   $Provider
    )

    $fieldName = $Name -replace '[/-]', '_'
    if (-not $Provider.IsValidIdentifier($fieldName)) {
        throw "'$Name' does not convert to a valid field name."
    }

    return $fieldName
}

function Format-TimeSpanExpression {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [timespan]  $Value
    )

    if ($Value -eq [timespan]::Zero) {
        return 'System.TimeSpan.Zero'
    }

    # Every component takes the sign of the value, so that -4:56:02 is written as (-4, -56, -2).
    $sign = $Value -lt [timespan]::Zero ? -1 : 1
    $magnitude = $Value.Duration()
    return "new System.TimeSpan($($sign * [math]::Floor($magnitude.TotalHours)), $($sign * $magnitude.Minutes), $($sign * $magnitude.Seconds))"
}

function Format-StringExpression {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Value
    )

    return '"' + ($Value -replace '\\', '\\' -replace '"', '\"') + '"'
}

function Format-DayRuleArgumentExpressions {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $DayKind,

        [Parameter()]
        [System.Nullable[int]]  $Day,

        [Parameter()]
        [System.Nullable[System.DayOfWeek]] $DayOfWeek
    )

    $dayExpression = $null -ne $Day ? [string]$Day : 'null'
    $dayOfWeekExpression = $null -ne $DayOfWeek ? "System.DayOfWeek.$DayOfWeek" : 'null'
    return "TzDataDayKind.$DayKind, $dayExpression, $dayOfWeekExpression"
}

function Format-ZoneLineExpression {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject]    $ZoneLine,

        [Parameter()]
        [string]    $RuleSetFieldName
    )

    $fixedSaveExpression = $null -ne $ZoneLine.FixedSave ? (Format-TimeSpanExpression -Value $ZoneLine.FixedSave) : 'null'
    $rulesExpression = $ZoneLine.RuleKind -eq 'RuleSet' ? "RuleSets.$RuleSetFieldName" : 'null'
    $untilExpression = 'null'
    if ($null -ne $ZoneLine.Until) {
        $until = $ZoneLine.Until
        $untilExpression = "new TzDataUntil($($until.Year), $($until.Month), $(Format-DayRuleArgumentExpressions -DayKind $until.DayKind -Day $until.Day -DayOfWeek $until.DayOfWeek), $(Format-TimeSpanExpression -Value $until.Time), TzDataTimeReference.$($until.TimeReference))"
    }

    return "new TzDataZoneLine($(Format-TimeSpanExpression -Value $ZoneLine.StandardOffset), TzDataZoneRuleKind.$($ZoneLine.RuleKind), $fixedSaveExpression, $rulesExpression, $(Format-StringExpression -Value $ZoneLine.Format), $untilExpression)"
}

function Format-RuleExpression {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject]    $Rule
    )

    $isDaylightExpression = $Rule.IsDaylight ? 'true' : 'false'
    return "new TzDataRule($(Format-StringExpression -Value $Rule.Name), $($Rule.FromYear), $($Rule.ToYear), $($Rule.Month), $(Format-DayRuleArgumentExpressions -DayKind $Rule.DayKind -Day $Rule.Day -DayOfWeek $Rule.DayOfWeek), $(Format-TimeSpanExpression -Value $Rule.AtTime), TzDataTimeReference.$($Rule.AtTimeReference), $(Format-TimeSpanExpression -Value $Rule.Save), $isDaylightExpression, $(Format-StringExpression -Value $Rule.Letter))"
}

function New-DataHostTypeDeclaration {
    <#
        .SYNOPSIS
        Declares a private static class to hold generated data, nested in a partial declaration of the struct.
        Returns the compile unit and the class.
    #>
    [OutputType([pscustomobject])]
    param (
        [Parameter(Mandatory)]
        [string]    $TypeName,

        [Parameter(Mandatory)]
        [string]    $HostTypeName,

        [Parameter(Mandatory)]
        [string]    $HostTypeComment
    )

    $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
    $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Chronology')
    [void]$compileUnit.Namespaces.Add($namespace)

    $structType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
    $structType.IsStruct = $true
    $structType.IsPartial = $true
    $structType.TypeAttributes = [System.Reflection.TypeAttributes]::Public
    [void]$namespace.Types.Add($structType)

    $hostType = [System.CodeDom.CodeTypeDeclaration]::new($HostTypeName)
    $hostType.IsClass = $true
    $hostType.TypeAttributes = [System.Reflection.TypeAttributes]::NestedPrivate -bor [System.Reflection.TypeAttributes]::Sealed -bor [System.Reflection.TypeAttributes]::Abstract
    @('<summary>', $HostTypeComment, '</summary>') | ForEach-Object { [void]$hostType.Comments.Add([System.CodeDom.CodeCommentStatement]::new($_, $true)) }
    [void]$structType.Members.Add($hostType)

    return [pscustomobject]@{ CompileUnit = $compileUnit; HostType = $hostType }
}

function New-DataArrayFieldDeclaration {
    [OutputType([System.CodeDom.CodeMemberField])]
    param (
        [Parameter(Mandatory)]
        [string]    $ElementTypeName,

        [Parameter(Mandatory)]
        [string]    $FieldName,

        [Parameter(Mandatory)]
        [string[]]  $ElementExpressions
    )

    $field = [System.CodeDom.CodeMemberField]::new("readonly $ElementTypeName[]", $FieldName)
    $field.Attributes = [System.CodeDom.MemberAttributes]::Assembly -bor [System.CodeDom.MemberAttributes]::Static
    $field.InitExpression = [System.CodeDom.CodeArrayCreateExpression]::new($ElementTypeName, [System.CodeDom.CodeExpression[]]@($ElementExpressions | ForEach-Object { [System.CodeDom.CodeSnippetExpression]::new($_) }))

    return $field
}

function ConvertTo-FinalSourceCode {
    <#
        .SYNOPSIS
        Generates the source code of a compile unit, and makes the changes that CodeDom cannot express: the
        nullable context follows the auto-generated header, the struct is readonly, nested classes are static, and
        the IConvertible interface is applied conditionally.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [System.CodeDom.CodeCompileUnit]    $CompileUnit,

        [Parameter(Mandatory)]
        [System.CodeDom.Compiler.CodeDomProvider]   $Provider,

        [Parameter(Mandatory)]
        [System.CodeDom.Compiler.CodeGeneratorOptions]  $Options,

        [Parameter(Mandatory)]
        [string]    $TypeName,

        [Parameter()]
        [switch]    $IncludeConvertibleInterface
    )

    $newLine = [System.Environment]::NewLine
    $nullableDirectives = "#if NETCOREAPP3_0_OR_GREATER$newLine#nullable enable$newLine#endif"

    $writer = [System.IO.StringWriter]::new()
    try {
        $Provider.GenerateCodeFromCompileUnit($CompileUnit, $writer, $Options)
        $sourceCode = $writer.ToString()
    }
    finally {
        $writer.Close()
    }

    $headerPattern = [regex]::new('\A//-+\r?\n(?://.*\r?\n)*?//-+\r?\n\r?\n')
    if (-not $headerPattern.IsMatch($sourceCode)) {
        throw 'The auto-generated header was not found in the generated source code.'
    }
    $sourceCode = $headerPattern.Replace($sourceCode, { param($match) $match.Value + $nullableDirectives + $newLine + $newLine }, 1)

    $sourceCodeBuilder = [System.Text.StringBuilder]::new()
    $reader = [System.IO.StringReader]::new($sourceCode)
    try {
        $sourceCodeLine = $reader.ReadLine()
        while ($null -ne $sourceCodeLine) {
            $trimmedSourceCodeLine = $sourceCodeLine.TrimStart()
            if ($trimmedSourceCodeLine.StartsWith("public partial struct $TypeName")) {
                $sourceCodeLine = $sourceCodeLine.Replace("public partial struct $TypeName", "public readonly partial struct $TypeName")
            }
            elseif ($trimmedSourceCodeLine -match '^(public|private) sealed abstract class ') {
                $sourceCodeLine = $sourceCodeLine.Replace("$($Matches[1]) sealed abstract class ", "$($Matches[1]) static class ")
            }
            [void]$sourceCodeBuilder.AppendLine($sourceCodeLine)

            # Add conditional logic for applying IConvertible interface.
            if ($IncludeConvertibleInterface -and $trimmedSourceCodeLine.StartsWith("public partial struct $TypeName")) {
                [void]$sourceCodeBuilder.AppendLine('#if NETSTANDARD1_3_OR_GREATER||NET')
                [void]$sourceCodeBuilder.AppendLine(', System.IConvertible')
                [void]$sourceCodeBuilder.AppendLine('#endif')
            }

            $sourceCodeLine = $reader.ReadLine()
        }
    }
    finally {
        $reader.Close()
    }

    return $sourceCodeBuilder.ToString()
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
        [string]        $TzDataVersion,

        [Parameter()]
        [pscustomobject]    $TzDataSource,

        [Parameter()]
        [string]        $PartialFileFolderPath
    )

    begin {
        $activity = "Generating $TypeName code DOM"
        Write-Progress -Activity $activity -PercentComplete -1

        $codesProcessed = 0

        $provider = [System.CodeDom.Compiler.CodeDomProvider]::CreateProvider($GenerateLanguage)

        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()

        $zoneLineDataDeclaration = New-DataHostTypeDeclaration -TypeName $TypeName -HostTypeName 'ZoneLineData' -HostTypeComment 'The zone lines of each canonical timezone, referenced by the timezone fields.'
        $ruleSetsDeclaration = New-DataHostTypeDeclaration -TypeName $TypeName -HostTypeName 'RuleSets' -HostTypeComment 'The daylight saving rule sets referenced by the zone lines.'

        # The names of the rule sets referenced by the zone lines, mapped to the names of their fields.
        $referencedRuleSets = [System.Collections.Generic.SortedDictionary[string, string]]::new([System.StringComparer]::Ordinal)
        $zoneLineDataFieldNames = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
        $ruleSetFieldNames = [System.Collections.Generic.Dictionary[string, string]]::new([System.StringComparer]::Ordinal)
        $ruleSetNames = [string[]]@($TzDataSource.Rules.Keys)

        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Chronology')
        [void]$compileUnit.Namespaces.Add($namespace)
        [void]$namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Linq'))
        [void]$namespace.Imports.Add([System.CodeDom.CodeNamespaceImport]::new('System.Reflection'))

        Write-Progress -Activity $activity -CurrentOperation 'Declaring enum type' -PercentComplete -1
    
        $structType = [System.CodeDom.CodeTypeDeclaration]::new($TypeName)
        [void]$structType.BaseTypes.Add([System.CodeDom.CodeTypeReference]::new([System.IComparable]))
        [void]$structType.BaseTypes.Add([System.CodeDom.CodeTypeReference]::new('System.IEquatable', [System.CodeDom.CodeTypeReference[]]@([System.CodeDom.CodeTypeReference]::new("DataStandardizer.Chronology.$TypeName"))))
        $structType.IsStruct = $true
        $structType.IsPartial = $true
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
            (Get-ZoneLinesFieldDeclaration -UseNullableReferenceTypes),
            [System.CodeDom.CodeSnippetTypeMember]::new('#else'),
            (Get-ValueFieldDeclaration),
            (Get-ZoneLinesFieldDeclaration),
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
        # Every field of a struct must be assigned by its constructors, and the zone lines of a timezone created by
        # explicit cast are found by its identifier instead.
        $noZoneLinesAssignmentStatement = [System.CodeDom.CodeAssignStatement]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_zoneLines'),
            [System.CodeDom.CodePrimitiveExpression]::new($null))
        $structConstructor.Statements.AddRange(@($argumentCheckStatement, $valueAssignmentStatement, $noZoneLinesAssignmentStatement))
        [void]$structConstructor.StartDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::Start, 'Constructors'))
        [void]$structType.Members.Add($structConstructor)

        $zoneLinesStructConstructor = [System.CodeDom.CodeConstructor]::new()
        $zoneLinesStructConstructor.Attributes = ($zoneLinesStructConstructor.Attributes -band -bnot [System.CodeDom.MemberAttributes]::AccessMask) -bor [System.CodeDom.MemberAttributes]::Private
        [void]$zoneLinesStructConstructor.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new([string], 'value'))
        [void]$zoneLinesStructConstructor.Parameters.Add([System.CodeDom.CodeParameterDeclarationExpression]::new('DataStandardizer.Chronology.TzDataZoneLine[]', 'zoneLines'))
        $zoneLinesArgumentCheckStatement = [System.CodeDom.CodeConditionStatement]::new(
            [System.CodeDom.CodeBinaryOperatorExpression]::new(
                [System.CodeDom.CodeArgumentReferenceExpression]::new('zoneLines'),
                [System.CodeDom.CodeBinaryOperatorType]::ValueEquality,
                [System.CodeDom.CodePrimitiveExpression]::new($null)),
            @([System.CodeDom.CodeThrowExceptionStatement]::new([System.CodeDom.CodeObjectCreateExpression]::new([System.CodeDom.CodeTypeReference]::new([System.ArgumentNullException]), @([System.CodeDom.CodeSnippetExpression]::new('nameof(zoneLines)'))))))
        $zoneLinesAssignmentStatement = [System.CodeDom.CodeAssignStatement]::new(
            [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeThisReferenceExpression]::new(), '_zoneLines'),
            [System.CodeDom.CodeArgumentReferenceExpression]::new('zoneLines'))
        $zoneLinesStructConstructor.Statements.AddRange(@($argumentCheckStatement, $zoneLinesArgumentCheckStatement, $valueAssignmentStatement, $zoneLinesAssignmentStatement))
        [void]$zoneLinesStructConstructor.EndDirectives.Add([System.CodeDom.CodeRegionDirective]::new([System.CodeDom.CodeRegionMode]::End, [string]::Empty))
        [void]$structType.Members.Add($zoneLinesStructConstructor)
    
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

        # Add the zone lines of the timezone, referenced by the member.
        if (-not $TzDataSource.Zones.Contains($_.TZ)) {
            throw "Timezone $($_.TZ) is listed in zone1970.tab, but no Zone is defined for it."
        }
        $zoneLines = ConvertFrom-TzDataZone -Name $_.TZ -Lines $TzDataSource.Zones[$_.TZ] -RuleSetNames $ruleSetNames
        $zoneLineDataFieldName = ConvertTo-FieldName -Name $_.TZ -Provider $provider
        if ($zoneLineDataFieldNames.ContainsKey($zoneLineDataFieldName)) {
            throw "Timezones $($zoneLineDataFieldNames[$zoneLineDataFieldName]) and $($_.TZ) convert to the same field name."
        }
        $zoneLineDataFieldNames[$zoneLineDataFieldName] = $_.TZ
        [string[]]$zoneLineExpressions = foreach ($zoneLine in $zoneLines) {
            $ruleSetFieldName = $null
            if ($zoneLine.RuleKind -eq 'RuleSet') {
                if (-not $referencedRuleSets.ContainsKey($zoneLine.RuleSetName)) {
                    $ruleSetFieldName = ConvertTo-FieldName -Name $zoneLine.RuleSetName -Provider $provider
                    if ($ruleSetFieldNames.ContainsKey($ruleSetFieldName)) {
                        throw "Rule sets $($ruleSetFieldNames[$ruleSetFieldName]) and $($zoneLine.RuleSetName) convert to the same field name."
                    }
                    $ruleSetFieldNames[$ruleSetFieldName] = $zoneLine.RuleSetName
                    $referencedRuleSets[$zoneLine.RuleSetName] = $ruleSetFieldName
                }
                $ruleSetFieldName = $referencedRuleSets[$zoneLine.RuleSetName]
            }
            Format-ZoneLineExpression -ZoneLine $zoneLine -RuleSetFieldName $ruleSetFieldName
        }
        $zoneLineDataField = New-DataArrayFieldDeclaration -ElementTypeName 'TzDataZoneLine' -FieldName $zoneLineDataFieldName -ElementExpressions $zoneLineExpressions
        @('<summary>', $_.TZ, '</summary>') | ForEach-Object { [void]$zoneLineDataField.Comments.Add([System.CodeDom.CodeCommentStatement]::new($_, $true)) }
        [void]$zoneLineDataDeclaration.HostType.Members.Add($zoneLineDataField)

        $enumField.InitExpression = [System.CodeDom.CodeObjectCreateExpression]::new("DataStandardizer.Chronology.$TypeName", @(
                [System.CodeDom.CodePrimitiveExpression]::new($_.TZ),
                [System.CodeDom.CodeFieldReferenceExpression]::new([System.CodeDom.CodeTypeReferenceExpression]::new("$TypeName.ZoneLineData"), $zoneLineDataFieldName)))
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

        # Declare the rule sets referenced by the zone lines.
        Write-Progress -Activity $activity -CurrentOperation 'Declaring rule sets' -PercentComplete -1

        foreach ($referencedRuleSet in $referencedRuleSets.GetEnumerator()) {
            $rules = ConvertFrom-TzDataRule -Name $referencedRuleSet.Key -Lines $TzDataSource.Rules[$referencedRuleSet.Key]
            [string[]]$ruleExpressions = foreach ($rule in $rules) { Format-RuleExpression -Rule $rule }
            $ruleSetField = New-DataArrayFieldDeclaration -ElementTypeName 'TzDataRule' -FieldName $referencedRuleSet.Value -ElementExpressions $ruleExpressions
            @('<summary>', "Rule set $($referencedRuleSet.Key)", '</summary>') | ForEach-Object { [void]$ruleSetField.Comments.Add([System.CodeDom.CodeCommentStatement]::new($_, $true)) }
            [void]$ruleSetsDeclaration.HostType.Members.Add($ruleSetField)
        }

        Write-Progress -Completed

        # Output source code.
        $options = [System.CodeDom.Compiler.CodeGeneratorOptions]::new()
        $options.BlankLinesBetweenMembers = $true
        $options.BracingStyle = 'C'
        $options.VerbatimOrder = $true

        $partialFileFolderFullPath = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($PartialFileFolderPath)
        $partialFiles = [ordered]@{
            "$TypeName.ZoneLineData.cs" = $zoneLineDataDeclaration.CompileUnit
            "$TypeName.RuleSets.cs"     = $ruleSetsDeclaration.CompileUnit
        }
        foreach ($partialFile in $partialFiles.GetEnumerator()) {
            $partialSourceCode = ConvertTo-FinalSourceCode -CompileUnit $partialFile.Value -Provider $provider -Options $options -TypeName $TypeName
            [System.IO.File]::WriteAllText((Join-Path -Path $partialFileFolderFullPath -ChildPath $partialFile.Key), $partialSourceCode, [System.Text.UTF8Encoding]::new($false))
        }

        Write-Output (ConvertTo-FinalSourceCode -CompileUnit $compileUnit -Provider $provider -Options $options -TypeName $TypeName -IncludeConvertibleInterface)
    }
}

# Validate parameters.
if (-not (Test-Path -Path $SourceFolderPath -PathType Container)) {
    Write-Error "Source folder $SourceFolderPath not found."
    exit;
}
if (-not (Test-Path -Path $PartialFileFolderPath -PathType Container)) {
    Write-Error "Partial file folder $PartialFileFolderPath not found."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    $modulePath = Resolve-Path scripts\StringEnumCodeGen\StringEnumCodeGen.psm1
    Import-Module (Split-Path $modulePath -Parent)
    $parserModulePath = Resolve-Path scripts\TzDataParser\TzDataParser.psm1
    Import-Module (Split-Path $parserModulePath -Parent)

    # Read the zone and rule lines of the region files.
    $tzDataSource = Read-TzDataSource -SourceFolderPath $SourceFolderPath

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
    $timezoneLines | ConvertFrom-Csv -Delimiter "`t" -Header $zonesFileHeaderFieldNames | Out-SourceCode -CodeCount $timezoneCount -CountryCodeTable $countryCodeTable -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage -TzDataVersion $tzDataVersion -TzDataSource $tzDataSource -PartialFileFolderPath $PartialFileFolderPath
}
finally {
    Remove-Module TzDataParser
    Remove-Module StringEnumCodeGen
    Set-PSDebug -Off
}