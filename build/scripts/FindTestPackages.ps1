#Requires -Version 7.4

param (
    [Parameter()]
    [int]   $TraceLevel = 0
)

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$testPackageNames = @($env:PACKAGEINFOS |
    ConvertFrom-Json |
    Where-Object -Property enableTests -NE -Value 0 |
    Select-Object -ExpandProperty packageName)

Write-Host "##vso[task.setvariable variable=testPackageNames;isOutput=true]$($testPackageNames -join ',')"

if ($testPackageNames.Count -gt 0) {
    Write-Information "Detected $($testPackageNames.Count) test package$(($testPackageNames.Count -eq 1) ? [string]::Empty : 's')."
}
else {
    Write-Information 'No test packages were detected.'
}

$testPackageNames | ForEach-Object {
    Write-Verbose "-`t$($_)"
}

# Find the packages whose tests, tests/<Package>.Tests, this build changes.  Their tests are built and run even when the package
# itself is unchanged, but a test change never makes a package changed, so it has no effect on versions or publication.
# The changes are those since the first parent: for a PR, the PR's changes, from its merge commit; for develop, which takes
# squash merges and builds every push, the commit's own.  The package's version tag isn't used, as a test change never moves it.
$changedTestPackageNames = @()
& git rev-parse --verify --quiet "$($env:BUILD_SOURCEVERSION)^1" | Out-Null
if ($LASTEXITCODE -eq 0) {
    $changedFiles = @(& git diff --name-only "$($env:BUILD_SOURCEVERSION)^1" $env:BUILD_SOURCEVERSION)
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to get the files changed by $env:BUILD_SOURCEVERSION."
    }

    $changedTestPackageNames = @($testPackageNames |
        Where-Object {
            $testFolderPath = "tests/$_.Tests/"
            $changedFiles | Where-Object { $_.StartsWith($testFolderPath, [System.StringComparison]::Ordinal) } | Select-Object -First 1
        })
}
else {
    Write-Debug "$env:BUILD_SOURCEVERSION has no parent; no tests are listed as changed."
}

Write-Host "##vso[task.setvariable variable=changedTestPackageNames;isOutput=true]$($changedTestPackageNames -join ',')"

if ($changedTestPackageNames.Count -gt 0) {
    Write-Information "Detected changed tests for $($changedTestPackageNames.Count) package$(($changedTestPackageNames.Count -eq 1) ? [string]::Empty : 's')."
}
else {
    Write-Information 'No test changes were detected.'
}

$changedTestPackageNames | ForEach-Object {
    Write-Verbose "-`t$($_)"
}
