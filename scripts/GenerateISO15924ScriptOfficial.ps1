#############################################################################
# Title: ISO 15924 Source Code Generator                                    #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.           #
#                                                                           #
# Purpose: Generate source code for implementing the ISO 15924 standard.    #
# Source: Codes for the representation of names of scripts, Unicode, Inc.   #
# https://www.unicode.org/iso15924/codelists.html                           #
#############################################################################
#Requires -Version 7.4

param (
    [Parameter(Mandatory, HelpMessage = 'Path to the file containing the official list of script codes.')]
    [string]    $SourceFilePath,

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

function Out-SourceCode {
    param (
        [Parameter(ValueFromPipeline)]
        [psobject[]]    $InputObject,
    
        [Parameter()]
        [int]   $CodeCount,

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
        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Language')
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
        $status = "Evaluating script code $($_.Code)"

        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding script code field' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)
        $enumField = [System.CodeDom.CodeMemberField]::new([ushort], $_.Code)
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new($_.'Nº' -as [ushort])

        $codeAttributeArguments = @(
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.Name)),
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.Nom)),
            [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new($_.Date)))
        $age = [decimal]::Zero
        if ([decimal]::TryParse($_.Age, [ref]$age)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodeSnippetExpression]::new($_.Age))
        }
        if (-not [string]::IsNullOrWhiteSpace($_.PVA)) {
            $codeAttributeArguments += [System.CodeDom.CodeAttributeArgument]::new('Alias', [System.CodeDom.CodePrimitiveExpression]::new($_.PVA))
        }
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.Language.Iso15924ScriptCodeAttribute', $codeAttributeArguments)
        [void]$enumField.CustomAttributes.Add($codeAttribute)

        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($_.Name, $true)
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
if (-not (Test-Path -Path $SourceFilePath)) {
    Write-Error "Source file $SourceFilePath not found."
    exit;
}

# Process language codes to produce source code.
Set-PSDebug -Trace 0    # activate tracing here for debugging
try {
    $sourceFileLines = Get-Content -Path $SourceFilePath
    $codeSetLines = $sourceFileLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) -and -not $_.StartsWith('#') }
    $codeCount = $codeSetLines | Measure-Object | Select-Object -ExpandProperty Count
    $codeSetLines | ConvertFrom-Csv -Delimiter ';' -Header 'Code', 'Nº', 'Name', 'Nom', 'PVA', 'Age', 'Date' | Out-SourceCode -CodeCount $codeCount -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage
}
finally {
    Set-PSDebug -Off
}