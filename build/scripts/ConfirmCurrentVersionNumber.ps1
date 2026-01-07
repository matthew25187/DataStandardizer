param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string]    $PackageName,

    [Parameter()]
    [int]    $TraceLevel = 0
)

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# set environment variable for current process
$env:AZURE_DEVOPS_EXT_PAT = $env:SYSTEM_ACCESSTOKEN
                                          
az devops configure -d organization=${env:ORGANIZATION_URL} project=${env:PROJECT_NAME}
az account set -s ${env:SUBSCRIPTION_ID}

$packageInfo = $env:PACKAGEINFOS | ConvertFrom-Json | Where-Object -Property packageName -EQ -Value $PackageName
if ($null -eq $packageInfo) {
    Write-Error "Package information not found for $PackageName."
}

$variableListOutput = & az pipelines variable-group variable list --group-id $packageInfo.variableGroupId
$variableGroupVersionNumbers = ($variableListOutput | ConvertFrom-Json).PSObject.Properties |
Where-Object { $_.Name.StartsWith('current') } |
Out-String -InputObject { $_.Name + ':' + $_.Value.value } -Stream |
Sort-Object |
ConvertFrom-Csv -Delimiter ':' -Header 'Name', 'Value' |
Out-String -InputObject { $_.Value } -Stream
[version] $packageVersionNumbers = ($variableGroupVersionNumbers[0], $variableGroupVersionNumbers[1], $variableGroupVersionNumbers[2], $variableGroupVersionNumbers[3] -join '.')
Write-Information "Updated package $($packageInfo.packageName) current version to v$packageVersionNumbers." -InformationAction Continue