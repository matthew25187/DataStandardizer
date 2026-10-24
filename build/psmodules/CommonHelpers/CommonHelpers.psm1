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

function ConvertTo-Base64String {
    param (
        # Input.
        [Parameter(Mandatory, ValueFromPipeline)]
        [string]    $InputObject
    )

    process {
        $inputBytes = [System.Text.Encoding]::UTF8.GetBytes($InputObject)
        return [System.Convert]::ToBase64String($inputBytes)
    }
}

# The helpers below let a build script run unchanged under Azure Pipelines (TF_BUILD) and GitHub Actions (GITHUB_ACTIONS).
# Under Azure Pipelines they produce exactly the logging commands the scripts used to write themselves.

function Get-RepositoryRoot {
    [OutputType([string])]
    param ()

    return [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' '..' '..'))
}

function Get-PackageManifest {
    <#
    .SYNOPSIS
    Reads build/packages.json, the list of packages and shared build files used by the build scripts and workflows.
    #>
    param (
        [Parameter()]
        [string]    $Path = (Join-Path (Get-RepositoryRoot) 'build' 'packages.json')
    )

    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Get-PackageInfo {
    <#
    .SYNOPSIS
    Returns the package list, in build order.  The PACKAGEINFOS environment variable, set by the Azure DevOps pipelines and
    by tests, takes precedence over build/packages.json.
    #>
    [OutputType([pscustomobject[]])]
    param (
        [Parameter()]
        [string]    $Path
    )

    if (-not [string]::IsNullOrEmpty($env:PACKAGEINFOS)) {
        return @($env:PACKAGEINFOS | ConvertFrom-Json)
    }

    $manifest = [string]::IsNullOrEmpty($Path) ? (Get-PackageManifest) : (Get-PackageManifest -Path $Path)
    return @($manifest.packages)
}

function Resolve-PackageName {
    <#
    .SYNOPSIS
    Turns a free-text package list into package IDs.

    .DESCRIPTION
    Names are separated by commas or white space and matched case-insensitively against either the package ID
    (DataStandardizer.File.CSV) or its short name (File.CSV).  '*' selects every package.  An unknown name is an error.
    The result is in package-list order with duplicates removed.
    #>
    [OutputType([string[]])]
    param (
        [Parameter()]
        [AllowEmptyString()]
        [AllowNull()]
        [string[]]  $Name,

        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo
    )

    $tokens = @($Name | Where-Object { $_ } | ForEach-Object { $_ -split '[,\s]+' } | Where-Object { $_ })
    if ($tokens.Count -eq 0) {
        return [string[]]@()
    }

    $selected = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($token in $tokens) {
        if ($token -eq '*') {
            $PackageInfo | ForEach-Object { $null = $selected.Add($_.packageName) }
            continue
        }

        $match = $PackageInfo | Where-Object {
            $_.packageName -eq $token -or ($_.packageName -replace '^DataStandardizer\.', '') -eq $token
        } | Select-Object -First 1
        if ($null -eq $match) {
            $validNames = ($PackageInfo | ForEach-Object { $_.packageName -replace '^DataStandardizer\.', '' }) -join ', '
            throw "Unknown package '$token'.  Valid names are: $validNames (or '*' for all)."
        }

        $null = $selected.Add($match.packageName)
    }

    return [string[]]@($PackageInfo | Where-Object { $selected.Contains($_.packageName) } | Select-Object -ExpandProperty packageName)
}

function Get-FirstValue {
    # The first argument that is neither null nor empty.  An empty environment variable counts as unset.
    param ([Parameter(ValueFromRemainingArguments)] [AllowNull()] [AllowEmptyString()] [string[]] $Value)

    return $Value | Where-Object { -not [string]::IsNullOrEmpty($_) } | Select-Object -First 1
}

function Get-BuildCommit {
    <#
    .SYNOPSIS
    The commit being built: BUILD_SOURCEVERSION under Azure Pipelines, GITHUB_SHA under GitHub Actions.  For a pull request
    both are the merge commit of the PR into its target.
    #>
    [OutputType([string])]
    param ()

    $commit = Get-FirstValue $env:BUILD_SOURCEVERSION $env:GITHUB_SHA
    if ([string]::IsNullOrEmpty($commit)) {
        throw 'The commit being built is unknown: set BUILD_SOURCEVERSION or GITHUB_SHA.'
    }

    return $commit
}

function Get-BuildRef {
    [OutputType([string])]
    param ()

    return Get-FirstValue $env:BUILD_SOURCEBRANCH $env:GITHUB_REF
}

function Test-ReleaseBuild {
    <#
    .SYNOPSIS
    True for a release build.  DS_RELEASE_BUILD, set by the GitHub workflows, takes precedence; otherwise a build of master
    is a release build.
    #>
    [OutputType([bool])]
    param ()

    if (-not [string]::IsNullOrEmpty($env:DS_RELEASE_BUILD)) {
        return $env:DS_RELEASE_BUILD -eq 'true'
    }

    return (Get-BuildRef) -eq 'refs/heads/master'
}

function Set-CiOutput {
    <#
    .SYNOPSIS
    Publishes a value to later steps: an Azure Pipelines variable (an output variable with -IsOutput), or a GitHub Actions
    step output.  Outside CI it only writes the value to the verbose stream.
    #>
    param (
        [Parameter(Mandatory)]
        [ValidatePattern('^[A-Za-z_][A-Za-z0-9_-]*$')]
        [string]    $Name,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [AllowNull()]
        [string]    $Value,

        [Parameter()]
        [switch]    $IsOutput
    )

    $Value = $Value ?? [string]::Empty
    if (-not [string]::IsNullOrEmpty($env:TF_BUILD)) {
        $outputArgument = $IsOutput ? ';isOutput=true' : [string]::Empty
        Write-Host "##vso[task.setvariable variable=$Name$outputArgument]$Value"
    }
    elseif (-not [string]::IsNullOrEmpty($env:GITHUB_OUTPUT)) {
        Add-CiFileEntry -Path $env:GITHUB_OUTPUT -Name $Name -Value $Value
    }
    else {
        Write-Verbose "Output $Name = $Value"
    }
}

function Set-CiVariable {
    <#
    .SYNOPSIS
    Sets an environment variable for later steps of the same job.
    #>
    param (
        [Parameter(Mandatory)]
        [ValidatePattern('^[A-Za-z_][A-Za-z0-9_]*$')]
        [string]    $Name,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [AllowNull()]
        [string]    $Value
    )

    $Value = $Value ?? [string]::Empty
    if (-not [string]::IsNullOrEmpty($env:TF_BUILD)) {
        Write-Host "##vso[task.setvariable variable=$Name]$Value"
    }
    elseif (-not [string]::IsNullOrEmpty($env:GITHUB_ENV)) {
        Add-CiFileEntry -Path $env:GITHUB_ENV -Name $Name -Value $Value
    }
    else {
        Write-Verbose "Variable $Name = $Value"
    }
}

function Add-CiFileEntry {
    param (
        [Parameter(Mandatory)]
        [string]    $Path,

        [Parameter(Mandatory)]
        [string]    $Name,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Value
    )

    if ($Value -match '[\r\n]') {
        $delimiter = "ghadelimiter_$([guid]::NewGuid().ToString('N'))"
        $entry = "$Name<<$delimiter`n$Value`n$delimiter`n"
    }
    else {
        $entry = "$Name=$Value`n"
    }

    [System.IO.File]::AppendAllText($Path, $entry, [System.Text.UTF8Encoding]::new($false))
}

function Write-CiIssue {
    <#
    .SYNOPSIS
    Writes an error or warning that the CI system shows as an annotation.
    #>
    param (
        [Parameter(Mandatory)]
        [ValidateSet('error', 'warning')]
        [string]    $Type,

        [Parameter(Mandatory)]
        [string]    $Message
    )

    if (-not [string]::IsNullOrEmpty($env:TF_BUILD)) {
        Write-Host "##vso[task.logissue type=$Type]$Message"
    }
    elseif (-not [string]::IsNullOrEmpty($env:GITHUB_ACTIONS)) {
        $escaped = $Message.Replace('%', '%25').Replace("`r", '%0D').Replace("`n", '%0A')
        Write-Host "::${Type}::$escaped"
    }
    else {
        Write-Host "${Type}: $Message"
    }
}

function Invoke-Gh {
    <#
    .SYNOPSIS
    Runs the GitHub CLI and fails on a non-zero exit code.  Kept as a function so tests can mock it.

    No parameter block, so every argument, including gh's own switches such as -X, is passed through unchanged.
    #>

    $output = & gh @args
    if ($LASTEXITCODE -ne 0) {
        throw "gh $(@($args)[0..1] -join ' ') failed with exit code $LASTEXITCODE."
    }

    return $output
}

Export-ModuleMember -Function ConvertFrom-Base64String, ConvertTo-Base64String, Get-RepositoryRoot, Get-PackageManifest, Get-PackageInfo, `
    Resolve-PackageName, Get-BuildCommit, Get-BuildRef, Test-ReleaseBuild, Set-CiOutput, Set-CiVariable, Write-CiIssue, Invoke-Gh
