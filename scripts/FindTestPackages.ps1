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

if ($testPackageNames.Count -eq 0) {
    Write-Information 'No test packages were detected.'
}
else {
    Write-Information "Detected $($testPackageNames.Count) test package$(($testPackageNames.Count -eq 1)? [string]::Empty :'s')."
}