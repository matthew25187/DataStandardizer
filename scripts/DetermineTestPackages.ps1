#Requires -Version 7.4

param (
    [Parameter(Mandatory)]
    [string]    $PackageInfos,

    [Parameter()]
    [int]   $TraceLevel = 0
)

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$testPackageNames = $PackageInfos
| ConvertFrom-Json
| Where-Object -Property enableTests -NE 0
| Select-Object -Property packageName
| ConvertTo-Csv -NoHeader -NoTypeInformation -UseQuotes Never
| Out-String -Stream

Write-Host "##vso[task.setvariable variable=testPackageNames;isOutput=true]$($testPackageNames -join ',')"