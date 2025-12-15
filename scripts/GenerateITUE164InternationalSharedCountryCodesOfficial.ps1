#############################################################################
# Title: ITU-T E.164 International Shared Country Codes Generator           #
# Copyright: Copyright © 2025, Matthew25187. All rights reserved.           #
#                                                                           #
# Purpose: Generate shared codes for ITU-T E.164.                           #
# Source: ITU-T E.164 International Shared Country Codes Database.          #
# https://www.itu.int/net/ITU-T/inrdb/e164_intlsharedcc.aspx?cc=881,882,883 #
#############################################################################

[CmdletBinding()]
param (
    [Parameter(Mandatory, HelpMessage = 'Path to the file containing the official list of script codes.')]
    [string]    $SourceFilePath,

    [Parameter()]
    [ushort[]]    $IncludeSharedCodes,

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
        [pscustomobject[]]        $InputObject,
    
        [Parameter()]
        [int]   $CodeCount,

        [Parameter()]
        [System.DateOnly]        $PublishDate,

        [Parameter()]
        [Int16[]]        $SharedCodeFilterBy,

        [Parameter()]
        [bool]        $IsSharedCodeFiltered,

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

        # Declare namespace.
        $compileUnit = [System.CodeDom.CodeCompileUnit]::new()
        $namespace = [System.CodeDom.CodeNamespace]::new('DataStandardizer.Communication.E164')
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

        if ($null -ne $PublishDate) {
            $enumTypeOpenRemarksComment = [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
            $enumTypeRemarksContentComment = [System.CodeDom.CodeCommentStatement]::new("Up to date as at $($PublishDate.ToString('yyyy-MM-dd')).", $true)
            $enumTypeCloseRemarksComment = [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
            $enumType.Comments.AddRange(@($enumTypeOpenRemarksComment, $enumTypeRemarksContentComment, $enumTypeCloseRemarksComment))
        }

        [void]$namespace.Types.Add($enumType)

        $codesProcessed = 0
    }

    process {
        if ($_.Status -ne 'A') {
            return;
        }

        $codeParts = $_.'Shared Code and Identification Code' -split ' '
        $sharedCode, $identificationCode = $codeParts[0], $codeParts[1]
        if ($IsSharedCodeFiltered -and $sharedCode -notin $SharedCodeFilterBy) {
            return;
        }

        $status = "Evaluating shared code $($_.'Shared Code and Identification Code')"
        Write-Progress -Activity $activity -Status $status -CurrentOperation 'Adding shared code' -PercentComplete (($codesProcessed -gt 0?($codesProcessed / $CodeCount):0) * 100)

        # Declare field.
        $enumFieldName = "IC$identificationCode"
        $enumField = [System.CodeDom.CodeMemberField]::new([ushort], $enumFieldName)
        $enumField.InitExpression = [System.CodeDom.CodePrimitiveExpression]::new([ushort]::Parse($identificationCode))
        [void]$enumField.UserData.Add('Shared Code', $sharedCode)
    
        $codeAttributeArguments = @([System.CodeDom.CodeAttributeArgument]::new([System.CodeDom.CodePrimitiveExpression]::new([ushort]::Parse($sharedCode))))
        $codeAttribute = [System.CodeDom.CodeAttributeDeclaration]::new('DataStandardizer.Communication.E164.ItuE164SharedCodeAttribute', $codeAttributeArguments)
        [void]$enumField.CustomAttributes.Add($codeAttribute)
        
        $enumFieldOpenSummaryComment = [System.CodeDom.CodeCommentStatement]::new('<summary>', $true)
        $enumFieldSummaryContentComment = [System.CodeDom.CodeCommentStatement]::new($_.Network, $true)
        $enumFieldCloseSummaryComment = [System.CodeDom.CodeCommentStatement]::new('</summary>', $true)
        $enumField.Comments.AddRange(@($enumFieldOpenSummaryComment, $enumFieldSummaryContentComment, $enumFieldCloseSummaryComment))

        $enumFieldOpenRemarksComment = [System.CodeDom.CodeCommentStatement]::new('<remarks>', $true)
        $enumFieldRemarksContentComment = [System.CodeDom.CodeCommentStatement]::new("Shared code: $sharedCode", $true)
        $enumFieldCloseRemarksComment = [System.CodeDom.CodeCommentStatement]::new('</remarks>', $true)
        $enumField.Comments.AddRange(@($enumFieldOpenRemarksComment, $enumFieldRemarksContentComment, $enumFieldCloseRemarksComment))
        
        [void]$enumType.Members.Add($enumField)

        Write-Progress -Activity $activity -Status $status -PercentComplete ((++$codesProcessed / $CodeCount) * 100)
    }

    end {
        Write-Progress -Completed

        # Generate source code.
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
if (-not (Test-Path -Path $SourceFilePath -PathType Leaf)) {
    Write-Error "Source file $SourceFilePath not found."
    exit;
}

# Process shared codes to produce source code.
Set-PSDebug -Trace 0
try {
    $sharedCodeRecords = Import-Csv -Path $SourceFilePath
    $sharedCodeRecordCount = $sharedCodeRecords | Measure-Object | Select-Object -ExpandProperty Count
    $sharedCodeUpdatedDate = $sharedCodeRecords | Where-Object -Property 'Date of Assignment' -NE -Value '' | ForEach-Object { [System.DateOnly]::ParseExact($_.'Date of Assignment', 'd/MM/yyyy') } | Measure-Object -Maximum | Select-Object -ExpandProperty Maximum
    $isSharedCodeFiltered = $PSBoundParameters.ContainsKey('IncludeSharedCodes')
    $sharedCodeRecords | Out-SourceCode -CodeCount $sharedCodeRecordCount -PublishDate $sharedCodeUpdatedDate -SharedCodeFilterBy $IncludeSharedCodes -IsSharedCodeFiltered $isSharedCodeFiltered -TypeName $SourceCodeTypeName -TypeComment $SourceCodeTypeComment -GenerateLanguage $SourceCodeLanguage
}
finally {
    Set-PSDebug -Off
}