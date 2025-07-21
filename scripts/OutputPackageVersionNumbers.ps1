param (
    [Parameter()]
    [string]    $PackageVersionNumbersList,

    # Debug tracing.
    [Parameter()]
    [int]   $TraceLevel = 0
)

function ConvertFrom-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )
    
    process {
        $inputBytes = [System.Convert]::FromBase64String($InputObject)
        return [System.Text.Encoding]::UTF8.GetString($inputBytes)
    }
}

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

Write-Verbose "The following package versions were detected:"

$PackageVersionNumbersList
| ConvertFrom-Base64String
| ConvertFrom-Json
| ForEach-Object {
    [version] $packageVersionNumber = $_.PackageVersion.Major, $_.PackageVersion.Minor, $_.PackageVersion.Build, $_.PackageVersion.Revision -join '.'
    Write-Verbose ([string]::Concat($_.PackageName, ': ', $packageVersionNumber))
}