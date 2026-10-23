#Requires -Version 7.4

param (
    [Parameter(Mandatory)]
    [string]$ResultsFolder,
    [Parameter()]
    [int]   $TraceLevel = 0
)

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# vstest.console only warns when a matched test assembly yields no tests, and the
# TRX logger records each warning as a run-level RunInfo.  Fail on those, because
# failOnMinTestsNotRun only checks the total across all assemblies.
$noTestsMessagePatterns = @(
    '^No test is available in ',
    '^No test matches the given testcase filter '
)

$resultFiles = @(Get-ChildItem -Path $ResultsFolder -Filter '*.trx' -File -Recurse -ErrorAction SilentlyContinue)
if ($resultFiles.Count -eq 0) {
    Write-Error "No test result files found in $ResultsFolder."
    exit 1
}

$emptyAssemblyMessages = foreach ($resultFile in $resultFiles) {
    Write-Verbose "Checking $($resultFile.FullName)."
    [xml]$testRun = Get-Content -LiteralPath $resultFile.FullName -Raw
    $testRun.TestRun.ResultSummary.RunInfos.RunInfo |
        Where-Object { $null -ne $_ } |
        ForEach-Object { $_.Text } |
        Where-Object { $message = $_; $noTestsMessagePatterns | Where-Object { $message -match $_ } }
}

if ($emptyAssemblyMessages) {
    $emptyAssemblyMessages | ForEach-Object {
        Write-Host "##vso[task.logissue type=error]$($_)"
    }
    Write-Error "$(@($emptyAssemblyMessages).Count) matched test assembl$((@($emptyAssemblyMessages).Count -eq 1) ? 'y' : 'ies') ran no tests."
    exit 1
}

Write-Information "Every matched test assembly ran tests ($($resultFiles.Count) result file$(($resultFiles.Count -eq 1) ? [string]::Empty : 's') checked)."
