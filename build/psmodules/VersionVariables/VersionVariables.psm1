#Requires -Version 7.4

# Reads and writes the per-package version numbers kept as GitHub repository variables.  They replace the Azure DevOps
# variable groups, one set per package:
#
#   DATASTANDARDIZER_<suffix>__STABLE_{MAJOR,MINOR,PATCH}_NUMBER                 the last release
#   DATASTANDARDIZER_<suffix>__CURRENT_{MAJOR,MINOR,PATCH,PRERELEASE}_NUMBER    the last published version
#   DATASTANDARDIZER_<suffix>__CURRENT_PRERELEASE_LABEL
#   DATASTANDARDIZER_<suffix>__NEXT_{MAJOR,MINOR,PATCH,PRERELEASE}_NUMBER       the version the next build produces
#   DATASTANDARDIZER_<suffix>__NEXT_PRERELEASE_LABEL
#
# <suffix> is the package's variableSuffix in build/packages.json.  A prefix such as SHADOW_ selects a separate copy, used to
# trial the workflows without touching the real numbers.  Edit the NEXT_* variables by hand to set up a patch release.

Import-Module (Join-Path $PSScriptRoot '../CommonHelpers/CommonHelpers.psm1') -Scope Local

$script:NumberParts = 'Major', 'Minor', 'Patch', 'Prerelease'

function Get-VersionVariableName {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [ValidatePattern('^[A-Z0-9_]+$')]
        [string]    $VariableSuffix,

        [Parameter(Mandatory)]
        [ValidateSet('Stable', 'Current', 'Next')]
        [string]    $Group,

        [Parameter(Mandatory)]
        [ValidateSet('Major', 'Minor', 'Patch', 'Prerelease', 'Label')]
        [string]    $Part,

        [Parameter()]
        [ValidatePattern('^([A-Z0-9_]*)$')]
        [string]    $Prefix = ''
    )

    $partName = ($Part -eq 'Label') ? 'PRERELEASE_LABEL' : "$($Part.ToUpperInvariant())_NUMBER"
    return "${Prefix}DATASTANDARDIZER_${VariableSuffix}__$($Group.ToUpperInvariant())_$partName"
}

function Read-VersionVariableSet {
    <#
    .SYNOPSIS
    Returns the repository's variables as a case-insensitive name/value table.

    .PARAMETER SnapshotPath
    A JSON object of name/value pairs, such as the workflow's toJSON(vars), to read instead of calling the GitHub API.
    #>
    [OutputType([hashtable])]
    param (
        [Parameter()]
        [string]    $SnapshotPath
    )

    $variables = [hashtable]::new([System.StringComparer]::OrdinalIgnoreCase)
    if (-not [string]::IsNullOrEmpty($SnapshotPath)) {
        $snapshot = Get-Content -LiteralPath $SnapshotPath -Raw | ConvertFrom-Json -AsHashtable
        foreach ($name in $snapshot.Keys) {
            $variables[$name] = [string]$snapshot[$name]
        }
        return $variables
    }

    $listed = (Invoke-Gh variable list --json 'name,value' --limit 1000) -join "`n" | ConvertFrom-Json
    foreach ($variable in $listed) {
        $variables[$variable.name] = [string]$variable.value
    }

    return $variables
}

function Get-PackageVersionState {
    <#
    .SYNOPSIS
    A package's stored versions.  A missing number counts as 0, as for a package that has never been built.
    #>
    param (
        [Parameter(Mandatory)]
        [pscustomobject]    $PackageInfo,

        [Parameter(Mandatory)]
        [hashtable]     $Variables,

        [Parameter()]
        [string]    $Prefix = ''
    )

    $read = {
        param($Group, $Part)
        $name = Get-VersionVariableName -VariableSuffix $PackageInfo.variableSuffix -Group $Group -Part $Part -Prefix $Prefix
        $value = $Variables[$name]
        if ($Part -eq 'Label') {
            return $value
        }
        if ([string]::IsNullOrWhiteSpace($value)) {
            return 0
        }
        $number = 0
        if (-not [int]::TryParse($value.Trim(), [ref]$number) -or $number -lt 0) {
            throw "Variable $name has the value '$value', which isn't a version number part."
        }
        return $number
    }

    $readVersion = {
        param($Group, [int]$PartCount)
        $parts = $script:NumberParts[0..($PartCount - 1)] | ForEach-Object { & $read $Group $_ }
        return [version]($parts -join '.')
    }

    return [pscustomobject]@{
        PackageName            = $PackageInfo.packageName
        StableVersion          = & $readVersion 'Stable' 3
        CurrentVersion         = & $readVersion 'Current' 4
        CurrentPrereleaseLabel = & $read 'Current' 'Label'
        NextVersion            = & $readVersion 'Next' 4
        NextPrereleaseLabel    = & $read 'Next' 'Label'
    }
}

function Set-PackageVersionState {
    <#
    .SYNOPSIS
    Writes one of a package's stored versions, part by part from major to prerelease, then the label if one is given.  Only
    variables that already exist are written, as with the Azure DevOps variable groups, so the stable group's prerelease
    number need not exist.  Returns the names written.
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [OutputType([string[]])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject]    $PackageInfo,

        [Parameter(Mandatory)]
        [ValidateSet('Stable', 'Current', 'Next')]
        [string]    $Group,

        [Parameter(Mandatory)]
        [version]   $Version,

        [Parameter()]
        [AllowNull()]
        [AllowEmptyString()]
        [string]    $PrereleaseLabel,

        [Parameter()]
        [switch]    $SetPrereleaseLabel,

        # The current variables, to know which exist.
        [Parameter(Mandatory)]
        [hashtable]     $Variables,

        [Parameter()]
        [string]    $Prefix = ''
    )

    $values = [ordered]@{
        Major      = $Version.Major
        Minor      = $Version.Minor
        Patch      = [System.Math]::Max($Version.Build, 0)
        Prerelease = [System.Math]::Max($Version.Revision, 0)
    }
    if ($SetPrereleaseLabel) {
        $values['Label'] = $PrereleaseLabel ?? ''
    }

    $written = @()
    foreach ($part in $values.Keys) {
        $name = Get-VersionVariableName -VariableSuffix $PackageInfo.variableSuffix -Group $Group -Part $part -Prefix $Prefix
        if (-not $Variables.ContainsKey($name)) {
            Write-Verbose "Variable $name doesn't exist; not written."
            continue
        }

        $value = [string]$values[$part]
        if ($PSCmdlet.ShouldProcess($name, "Set to '$value'")) {
            $null = Invoke-Gh variable set $name --body $value
            $Variables[$name] = $value
            $written += $name
            Write-Verbose "Set $name to '$value'."
        }
    }

    return [string[]]$written
}

Export-ModuleMember -Function Get-VersionVariableName, Read-VersionVariableSet, Get-PackageVersionState, Set-PackageVersionState
