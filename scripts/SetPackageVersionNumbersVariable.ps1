param (
    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $PackageList,

    [Parameter()]
    [string] $VariableNamePrefix,

    [Parameter()]
    [ValidateNotNullOrEmpty()]
    [string] $PipelineVariableName,
    
    [Parameter()]
    [switch] $WriteOutputVar,

    # Debug tracing.
    [Parameter()]
    [int]   $TraceLevel = 0
)

function ConvertTo-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )
    
    process {
        $inputBytes = [System.Text.Encoding]::UTF8.GetBytes($InputObject)
        return [System.Convert]::ToBase64String($inputBytes)
    }
}

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                      
& az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
& az account set -s ${env:SUBSCRIPTION_ID}

$packageVersionNumbers = @()
$PackageList | ConvertFrom-Json | ForEach-Object {
    $variableListOutput = & az pipelines variable-group variable list --group-id $_.variableGroupId
    $variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties 
    | Where-Object { [string]::IsNullOrEmpty($VariableNamePrefix) -or $_.Name.StartsWith($VariableNamePrefix) } 
    | Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream 
    | Sort-Object 
    | ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' 
    | Out-String -InputObject { $_.Value } -Stream
    
    $packageVersionNumbers += [PSCustomObject]@{
        PackageName    = $_.packageName
        PackageVersion = [version]($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')
    }
}

if ($WriteOutputVar) {
    Write-Host ("##vso[task.setvariable variable=$PipelineVariableName;isOutput=true]$($packageVersionNumbers | ConvertTo-Json | ConvertTo-Base64String)")
}
else {
    Write-Host ("##vso[task.setvariable variable=$PipelineVariableName]$($packageVersionNumbers | ConvertTo-Json | ConvertTo-Base64String)")
}