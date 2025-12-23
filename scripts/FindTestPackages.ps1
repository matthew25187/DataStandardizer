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
| Where-Object -Property enableTests -NE -Value 0
| Select-Object -ExpandProperty packageName

Write-Host "##vso[task.setvariable variable=testPackageNames;isOutput=true]$($testPackageNames -join ',')"