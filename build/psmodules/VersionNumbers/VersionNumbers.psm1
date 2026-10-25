#Requires -Version 7.4

# Package version arithmetic.  Pure functions: no I/O, so every rule can be tested directly.
#
# A package version is four numbers, major.minor.patch.prerelease.  Prerelease 0 is a release (1.4.0); anything else is a
# preview (1.4.0.3), published as SemVer 1.4.0-<label>.3 and tagged v1.4.0.3-<Package>.

function ConvertTo-DisplayVersion {
    <#
    .SYNOPSIS
    The version as written in tags and messages: three parts for a release, four for a preview.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [version]   $Version
    )

    if ($Version.Revision -le 0) {
        return ([version]::new($Version.Major, $Version.Minor, [System.Math]::Max($Version.Build, 0))).ToString()
    }

    return $Version.ToString()
}

function ConvertTo-SemanticVersion {
    <#
    .SYNOPSIS
    The NuGet package version: M.m.p for a release, M.m.p-<label>.<prerelease> for a preview.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [version]   $Version,

        [Parameter()]
        [AllowEmptyString()]
        [AllowNull()]
        [string]    $PrereleaseLabel
    )

    $semanticVersion = $Version.Major, $Version.Minor, [System.Math]::Max($Version.Build, 0) -join '.'
    if ($Version.Revision -gt 0) {
        $semanticVersion += "-$PrereleaseLabel.$($Version.Revision)"
    }

    return $semanticVersion
}

function ConvertTo-PackageTagName {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [version]   $Version,

        [Parameter(Mandatory)]
        [string]    $PackageName
    )

    return "v$(ConvertTo-DisplayVersion -Version $Version)-$PackageName"
}

function ConvertFrom-SemanticVersion {
    <#
    .SYNOPSIS
    Parses a package version published by CI (1.4.0 or 1.4.0-preview.3) into its four-part version and label.  Returns $null
    for any other form.
    #>
    param (
        [Parameter(Mandatory)]
        [string]    $SemanticVersion
    )

    if ($SemanticVersion -match '^(?<core>\d+\.\d+\.\d+)$') {
        return [pscustomobject]@{ Version = [version]"$($Matches.core).0"; PrereleaseLabel = $null; IsPrerelease = $false }
    }
    if ($SemanticVersion -match '^(?<core>\d+\.\d+\.\d+)-(?<label>[A-Za-z][A-Za-z0-9]*)\.(?<number>[1-9]\d*)$') {
        return [pscustomobject]@{ Version = [version]"$($Matches.core).$($Matches.number)"; PrereleaseLabel = $Matches.label; IsPrerelease = $true }
    }

    return $null
}

function New-PackageVersions {
    <#
    .SYNOPSIS
    The version record of a package before this run produces anything: its stored versions, with the production version
    equal to the current one.  The property names are those the Azure DevOps pipelines used, so existing tooling still reads
    them.

    .PARAMETER State
    The stored versions, as returned by Get-PackageVersionState.

    .PARAMETER AssemblyVersions
    The assembly, file and informational versions read from the current package, or $null if there is none.
    #>
    param (
        [Parameter(Mandatory)]
        [string]    $PackageName,

        [Parameter(Mandatory)]
        [pscustomobject]    $State,

        [Parameter()]
        [AllowNull()]
        [pscustomobject]    $AssemblyVersions
    )

    [version]$current = $State.CurrentVersion
    return [pscustomobject]@{
        PackageName                            = $PackageName
        PackageStableVersion                   = ([version]$State.StableVersion).ToString()
        PackageCurrentVersion                  = $current.ToString()
        PackageCurrentPrereleaseLabel          = $State.CurrentPrereleaseLabel
        PackageNextVersion                     = ([version]$State.NextVersion).ToString()
        PackageNextPrereleaseLabel             = $State.NextPrereleaseLabel
        PackageProductionVersion               = $current.ToString()
        PackageProductionVersionDisplay        = ConvertTo-DisplayVersion -Version $current
        PackageProductionVersionSemVer         = ConvertTo-SemanticVersion -Version $current -PrereleaseLabel $State.CurrentPrereleaseLabel
        PackageProductionPrereleaseLabel       = $State.CurrentPrereleaseLabel
        AssemblyProductionVersion              = ${AssemblyVersions}?.AssemblyVersion
        AssemblyProductionFileVersion          = ${AssemblyVersions}?.FileVersion
        AssemblyProductionInformationalVersion = ${AssemblyVersions}?.InformationalVersion
    }
}

function Add-ProductionVersion {
    <#
    .SYNOPSIS
    Sets the version this run produces, and the next version to reserve, on a package's version record.

    .DESCRIPTION
    - Production version: the stored next version.  A preview uses its prerelease number (at least 1); a release uses 0.
    - Post-production version, reserved as the new next version: a preview increments the prerelease number; a release
      increments the minor number and resets patch and prerelease, unless it is a patch release (patch above 0), when it
      increments the patch number instead and so continues the patch series.  Start-PatchVersion.ps1 sets up a patch
      release and ends the series.
    - Assembly versions: FileVersion is the production version.  AssemblyVersion is M.m.p plus a time-of-day revision.
      InformationalVersion is M.m.
    #>
    param (
        [Parameter(Mandatory, ValueFromPipeline)]
        [pscustomobject]    $PackageVersions,

        [Parameter(Mandatory)]
        [bool]      $IsRelease,

        [Parameter()]
        [datetime]  $UtcNow = [datetime]::UtcNow
    )

    process {
        [version]$next = $PackageVersions.PackageNextVersion
        $productionRevision = $IsRelease ? 0 : [System.Math]::Max($next.Revision, 1)
        $production = [version]::new($next.Major, $next.Minor, $next.Build, $productionRevision)

        $PackageVersions.PackageProductionVersion = $production.ToString()
        $PackageVersions.PackageProductionVersionDisplay = ConvertTo-DisplayVersion -Version $production
        $PackageVersions.PackageProductionVersionSemVer = ConvertTo-SemanticVersion -Version $production -PrereleaseLabel $PackageVersions.PackageNextPrereleaseLabel
        $PackageVersions.PackageProductionPrereleaseLabel = $PackageVersions.PackageNextPrereleaseLabel

        $postProduction = if (-not $IsRelease) {
            [version]::new($production.Major, $production.Minor, $production.Build, $production.Revision + 1)
        }
        elseif ($production.Build -gt 0) {
            [version]::new($production.Major, $production.Minor, $production.Build + 1, 0)
        }
        else {
            [version]::new($production.Major, $production.Minor + 1, 0, 0)
        }
        $PackageVersions | Add-Member -MemberType NoteProperty -Name 'PackagePostProductionVersion' -Value $postProduction.ToString() -Force

        $assemblyRevision = [System.Convert]::ToInt32($UtcNow.TimeOfDay.TotalSeconds / 2)
        $PackageVersions.AssemblyProductionFileVersion = $production.ToString()
        $PackageVersions.AssemblyProductionVersion = ([version]::new($production.Major, $production.Minor, $production.Build, $assemblyRevision)).ToString()
        $PackageVersions.AssemblyProductionInformationalVersion = ([version]::new($production.Major, $production.Minor)).ToString()

        return $PackageVersions
    }
}

function Get-MajorStartVersion {
    <#
    .SYNOPSIS
    The next version that starts a new major version: the next major number, with minor, patch and prerelease reset to 0.

    .DESCRIPTION
    A next version already ahead of the last release's major number means a new major version has been started, so asking
    again is an error.
    #>
    [OutputType([version])]
    param (
        [Parameter(Mandatory)]
        [version]   $StableVersion,

        [Parameter(Mandatory)]
        [version]   $NextVersion
    )

    if ($NextVersion.Major -gt $StableVersion.Major) {
        throw "The next version $(ConvertTo-DisplayVersion -Version $NextVersion) is already ahead of the last release ${StableVersion}: a new major version has been started."
    }

    return [version]::new($NextVersion.Major + 1, 0, 0, 0)
}

function Get-PatchStartVersion {
    <#
    .SYNOPSIS
    The next version that starts a patch release of the last release: its major and minor numbers, and the patch number
    after the highest one already released for them.

    .PARAMETER StableVersion
    The last release.

    .PARAMETER TagName
    The repository's tag names.  Those of the form v<major>.<minor>.<patch>-<Package> are the package's releases.
    #>
    [OutputType([version])]
    param (
        [Parameter(Mandatory)]
        [version]   $StableVersion,

        [Parameter(Mandatory)]
        [string]    $PackageName,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $TagName = @()
    )

    if ($StableVersion -le [version]'0.0.0') {
        throw "$PackageName has no released version to patch."
    }

    $highest = [System.Math]::Max($StableVersion.Build, 0)
    foreach ($tag in $TagName) {
        if ($tag -match '^v(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)-(?<package>.+)$' -and
            $Matches.package -eq $PackageName -and
            [int]$Matches.major -eq $StableVersion.Major -and [int]$Matches.minor -eq $StableVersion.Minor) {
            $highest = [System.Math]::Max($highest, [int]$Matches.patch)
        }
    }

    return [version]::new($StableVersion.Major, $StableVersion.Minor, $highest + 1, 0)
}

function ConvertTo-EncodedVersionList {
    <#
    .SYNOPSIS
    Encodes version records (or any JSON-able value) as single-line Base64 UTF-8 JSON, the form passed between jobs.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object]    $InputObject
    )

    $json = ConvertTo-Json -InputObject $InputObject -Depth 8 -Compress
    return [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($json))
}

function ConvertFrom-EncodedVersionList {
    param (
        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $EncodedValue
    )

    if ([string]::IsNullOrEmpty($EncodedValue)) {
        return @()
    }

    $json = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($EncodedValue))
    return $json | ConvertFrom-Json
}

function Get-FinalizeAction {
    <#
    .SYNOPSIS
    Decides how the end of a run updates each package's stored versions.  Matches the Version_rollback and
    Update_package_version jobs of the Azure DevOps pipelines.

    .DESCRIPTION
    For each package this run reserved a version for:
    - Publication succeeded: current := production version (and label); stable := production version when it is a release.
      The next version stays as reserved.
    - Publication failed or was cancelled: if the stored next version is still the one this run reserved, restore the next
      version it replaced.  If not, a later run reserved after this one, or it was edited by hand, so it is left alone.

    .PARAMETER Reserved
    Per package: PreviousNext (the next version before this run) and WrittenNext (the next version this run reserved).

    .PARAMETER StoredNext
    Per package: the next version stored now.
    #>
    param (
        [Parameter(Mandatory)]
        [bool]      $PublishSucceeded,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]  $PackageVersions,

        [Parameter(Mandatory)]
        [hashtable] $Reserved,

        [Parameter(Mandatory)]
        [hashtable] $StoredNext
    )

    foreach ($packageName in @($Reserved.Keys | Sort-Object)) {
        $versions = $PackageVersions | Where-Object -Property PackageName -EQ -Value $packageName | Select-Object -First 1
        if ($null -eq $versions) {
            throw "Versions for reserved package $packageName not found."
        }

        if ($PublishSucceeded) {
            [version]$production = $versions.PackageProductionVersion
            [pscustomobject]@{ PackageName = $packageName; Group = 'Current'; Version = $production; PrereleaseLabel = $versions.PackageProductionPrereleaseLabel; Reason = 'published' }
            if ($production.Revision -eq 0) {
                [pscustomobject]@{ PackageName = $packageName; Group = 'Stable'; Version = $production; PrereleaseLabel = $null; Reason = 'released' }
            }
            continue
        }

        # Not $storedNext: PowerShell names are case-insensitive, so that would overwrite the $StoredNext parameter.
        [version]$writtenNext = $Reserved[$packageName].WrittenNext
        [version]$packageStoredNext = $StoredNext[$packageName]
        if ($packageStoredNext -eq $writtenNext) {
            [pscustomobject]@{ PackageName = $packageName; Group = 'Next'; Version = [version]$Reserved[$packageName].PreviousNext; PrereleaseLabel = $null; Reason = 'rolled back' }
        }
        else {
            [pscustomobject]@{ PackageName = $packageName; Group = $null; Version = $packageStoredNext; PrereleaseLabel = $null; Reason = "left alone: next version is $packageStoredNext, not the $writtenNext this run reserved" }
        }
    }
}

Export-ModuleMember -Function ConvertTo-DisplayVersion, ConvertTo-SemanticVersion, ConvertTo-PackageTagName, ConvertFrom-SemanticVersion, `
    New-PackageVersions, Add-ProductionVersion, Get-MajorStartVersion, Get-PatchStartVersion, ConvertTo-EncodedVersionList, ConvertFrom-EncodedVersionList, Get-FinalizeAction
