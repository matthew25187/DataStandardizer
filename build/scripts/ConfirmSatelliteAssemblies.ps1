<#
    .SYNOPSIS
    Confirms that a package contains the satellite assemblies for its culture-specific resources.

    .DESCRIPTION
    A package whose culture-specific resources are missing does not fail to load: every culture silently
    falls back to the neutral resources, so monetary values are formatted with the invariant separators and
    patterns wherever the package is consumed. That is a difficult failure to attribute after the fact, so
    the presence of the satellite assemblies is confirmed here rather than assumed.

    The package is expected to carry one satellite assembly per culture for each target framework.

    .PARAMETER PackageFolder
    Path to the folder containing the packages to confirm.

    .PARAMETER PackageName
    Name of the package to confirm.

    .PARAMETER MinimumCultureCount
    Least number of cultures the package is expected to carry for each target framework.
#>
#Requires -Version 7.4

param (
    [Parameter(Mandatory, HelpMessage = 'Path to the folder containing the packages to confirm.')]
    [string]    $PackageFolder,

    [Parameter(Mandatory, HelpMessage = 'Name of the package to confirm.')]
    [string]    $PackageName,

    [Parameter(HelpMessage = 'Least number of cultures the package is expected to carry for each target framework.')]
    [int]       $MinimumCultureCount = 1
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -Path $PackageFolder)) {
    Write-Error "Package folder $PackageFolder does not exist."
    exit 1
}

$package = Get-ChildItem -Path $PackageFolder -Filter "$PackageName.*.nupkg" -Recurse |
    Where-Object { $_.Name -notlike '*.symbols.nupkg' } |
    Select-Object -First 1

if ($null -eq $package) {
    Write-Error "No package matching $PackageName.*.nupkg was found in $PackageFolder."
    exit 1
}

Write-Host "Confirming satellite assemblies in $($package.Name)."

Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
    $satelliteEntries = $archive.Entries |
        Where-Object { $_.FullName -match '^lib/(?<framework>[^/]+)/(?<culture>[^/]+)/.+\.resources\.dll$' }

    if ($satelliteEntries.Count -eq 0) {
        Write-Error "$($package.Name) contains no satellite assemblies. Culture-specific resources would silently fall back to the neutral resources."
        exit 1
    }

    $frameworks = $archive.Entries |
        Where-Object { $_.FullName -match '^lib/(?<framework>[^/]+)/[^/]+\.dll$' } |
        ForEach-Object { [regex]::Match($_.FullName, '^lib/(?<framework>[^/]+)/').Groups['framework'].Value } |
        Sort-Object -Unique

    $failed = $false
    foreach ($framework in $frameworks) {
        $cultures = $satelliteEntries |
            Where-Object { $_.FullName -like "lib/$framework/*" } |
            ForEach-Object { [regex]::Match($_.FullName, '^lib/[^/]+/(?<culture>[^/]+)/').Groups['culture'].Value } |
            Sort-Object -Unique

        if ($cultures.Count -lt $MinimumCultureCount) {
            Write-Host "##vso[task.logissue type=error]$framework carries $($cultures.Count) cultures; at least $MinimumCultureCount were expected."
            $failed = $true
        }
        else {
            Write-Host "  $framework : $($cultures.Count) cultures"
        }
    }

    if ($failed) {
        Write-Error "$($package.Name) is missing satellite assemblies."
        exit 1
    }

    Write-Host "Confirmed $($satelliteEntries.Count) satellite assemblies across $($frameworks.Count) target frameworks."
}
finally {
    $archive.Dispose()
}
