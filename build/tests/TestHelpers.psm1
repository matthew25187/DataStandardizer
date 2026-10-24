#Requires -Version 7.4

# Fixtures for the build script tests.

function New-TestFolder {
    [OutputType([string])]
    param ()

    $path = Join-Path ([System.IO.Path]::GetTempPath()) "ds-build-tests-$([guid]::NewGuid().ToString('N'))"
    $null = New-Item -ItemType Directory -Path $path
    return $path
}

function Invoke-Git {
    # No parameter block, so git's own switches pass through unchanged.
    $output = & git @args 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "git $($args -join ' ') failed: $output"
    }
    return $output
}

function New-TestRepository {
    <#
    .SYNOPSIS
    Creates an empty git repository with a committer identity and returns its path.
    #>
    [OutputType([string])]
    param ()

    $path = New-TestFolder
    Push-Location $path
    try {
        Invoke-Git init --quiet --initial-branch=develop | Out-Null
        Invoke-Git config user.name 'Build Tests' | Out-Null
        Invoke-Git config user.email 'build-tests@example.com' | Out-Null
        Invoke-Git config commit.gpgsign false | Out-Null
        Invoke-Git config tag.gpgsign false | Out-Null
        Invoke-Git config core.autocrlf false | Out-Null
    }
    finally {
        Pop-Location
    }
    return $path
}

function Add-TestCommit {
    <#
    .SYNOPSIS
    Writes files (repository-relative path => content) in the current repository and commits them.  Returns the commit id.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [hashtable] $File,

        [Parameter()]
        [string]    $Message = 'Change files'
    )

    foreach ($relativePath in $File.Keys) {
        $fullPath = Join-Path (Get-Location) $relativePath
        $null = New-Item -ItemType Directory -Path (Split-Path $fullPath -Parent) -Force
        [System.IO.File]::WriteAllText($fullPath, [string]$File[$relativePath])
        Invoke-Git add -- $relativePath | Out-Null
    }

    $messageFile = Join-Path ([System.IO.Path]::GetTempPath()) "msg-$([guid]::NewGuid().ToString('N')).txt"
    [System.IO.File]::WriteAllText($messageFile, $Message)
    try {
        Invoke-Git commit --quiet --file $messageFile | Out-Null
    }
    finally {
        Remove-Item -LiteralPath $messageFile -Force
    }
    return (Invoke-Git rev-parse HEAD) | Select-Object -First 1
}

function Add-TestTag {
    param (
        [Parameter(Mandatory)]
        [string]    $Name,

        [Parameter()]
        [string]    $Commit = 'HEAD'
    )

    Invoke-Git tag -a -m "Tag $Name" $Name $Commit | Out-Null
}

function New-TestPackageInfo {
    <#
    .SYNOPSIS
    A package list in the build/packages.json shape, for the given package IDs.
    #>
    param (
        [Parameter(Mandatory)]
        [string[]]  $PackageName
    )

    foreach ($name in $PackageName) {
        [pscustomobject]@{
            packageName       = $name
            packageSourcePath = "src/$name"
            variableSuffix    = ($name -replace '^DataStandardizer\.', '').ToUpperInvariant().Replace('.', '_')
            enableTests       = $true
        }
    }
}

function New-TestNuGetPackage {
    <#
    .SYNOPSIS
    Writes a minimal .nupkg (a zip with a .nuspec and optional extra files) and returns its path.

    .PARAMETER Dependency
    Dependency id => version.

    .PARAMETER ExtraFile
    Archive path => source file path.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $Folder,

        [Parameter(Mandatory)]
        [string]    $Id,

        [Parameter(Mandatory)]
        [string]    $Version,

        [Parameter()]
        [System.Collections.IDictionary]    $Dependency = @{},

        [Parameter()]
        [string]    $RepositoryUrl = 'https://github.com/matthew25187/DataStandardizer.git',

        [Parameter()]
        [hashtable] $ExtraFile = @{}
    )

    $dependencies = ($Dependency.Keys | ForEach-Object { "        <dependency id=`"$_`" version=`"$($Dependency[$_])`" exclude=`"Build,Analyzers`" />" }) -join "`n"
    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    <id>$Id</id>
    <version>$Version</version>
    <authors>matthew25187</authors>
    <description>Test package.</description>
    <releaseNotes>Initial release.</releaseNotes>
    <repository type="git" url="$RepositoryUrl" />
    <dependencies>
      <group targetFramework=".NETStandard2.0">
$dependencies
      </group>
    </dependencies>
  </metadata>
</package>
"@
    $staging = Join-Path $Folder "staging-$([guid]::NewGuid().ToString('N'))"
    $null = New-Item -ItemType Directory -Path $staging
    [System.IO.File]::WriteAllText((Join-Path $staging "$Id.nuspec"), $nuspec)

    $packagePath = Join-Path $Folder "$Id.$Version.nupkg"
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::Open($packagePath, [System.IO.Compression.ZipArchiveMode]::Create)
    try {
        $null = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, (Join-Path $staging "$Id.nuspec"), "$Id.nuspec")
        foreach ($entryName in $ExtraFile.Keys) {
            $null = [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $ExtraFile[$entryName], $entryName)
        }
    }
    finally {
        $archive.Dispose()
    }

    Remove-Item -LiteralPath $staging -Recurse -Force
    return $packagePath
}

function Get-TestNuspec {
    <#
    .SYNOPSIS
    Reads the .nuspec from a .nupkg as XML.
    #>
    [OutputType([xml])]
    param (
        [Parameter(Mandatory)]
        [string]    $PackagePath,

        [Parameter(Mandatory)]
        [string]    $Id
    )

    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $reader = [System.IO.StreamReader]::new($archive.GetEntry("$Id.nuspec").Open())
        try {
            return [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $archive.Dispose()
    }
}


function Install-FakeGh {
    <#
    .SYNOPSIS
    Puts a fake gh first on PATH, keeping repository variables in a JSON file.  Supports 'variable list --json name,value'
    and 'variable set NAME --body VALUE'.  Returns the folder; restore PATH with Uninstall-FakeGh.
    #>
    [OutputType([string])]
    param (
        [Parameter()]
        [hashtable] $Variables = @{}
    )

    $folder = New-TestFolder
    $statePath = Join-Path $folder 'variables.json'
    ConvertTo-Json -InputObject $Variables | Set-Content -LiteralPath $statePath
    $script = @'
$statePath = $env:FAKE_GH_STATE
$state = Get-Content -LiteralPath $statePath -Raw | ConvertFrom-Json -AsHashtable
if ($args[0] -eq 'variable' -and $args[1] -eq 'list') {
    ConvertTo-Json -InputObject @($state.Keys | Sort-Object | ForEach-Object { [pscustomobject]@{ name = $_; value = $state[$_] } }) -Compress
    exit 0
}
if ($args[0] -eq 'variable' -and $args[1] -eq 'set') {
    $name = $args[2]
    $value = $args[[array]::IndexOf($args, '--body') + 1]
    $state[$name] = $value
    ConvertTo-Json -InputObject $state | Set-Content -LiteralPath $statePath
    Add-Content -LiteralPath "$statePath.log" -Value "set $name=$value"
    exit 0
}
[Console]::Error.WriteLine("fake gh: unsupported command: $args")
exit 1
'@
    Set-Content -LiteralPath (Join-Path $folder 'gh.ps1') -Value $script
    $script:SavedPath = $env:PATH
    $env:PATH = "$folder$([System.IO.Path]::PathSeparator)$env:PATH"
    $env:FAKE_GH_STATE = $statePath
    return $folder
}

function Uninstall-FakeGh {
    if ($null -ne $script:SavedPath) {
        $env:PATH = $script:SavedPath
        $script:SavedPath = $null
    }
    $env:FAKE_GH_STATE = $null
}

function Get-FakeGhVariable {
    [OutputType([hashtable])]
    param ()

    return Get-Content -LiteralPath $env:FAKE_GH_STATE -Raw | ConvertFrom-Json -AsHashtable
}

function Get-FakeGhLog {
    [OutputType([string[]])]
    param ()

    $logPath = "$($env:FAKE_GH_STATE).log"
    return (Test-Path -LiteralPath $logPath) ? @(Get-Content -LiteralPath $logPath) : @()
}

function New-TestVersionVariables {
    <#
    .SYNOPSIS
    The 13 version variables of one package, as a name/value table.
    #>
    [OutputType([hashtable])]
    param (
        [Parameter(Mandatory)]
        [string]    $Suffix,

        [Parameter()]
        [string]    $Stable = '1.3.0',

        [Parameter()]
        [string]    $Current = '1.4.0.2',

        [Parameter()]
        [string]    $Next = '1.4.0.3',

        [Parameter()]
        [string]    $Label = 'preview',

        [Parameter()]
        [string]    $Prefix = ''
    )

    $variables = @{}
    $parts = 'MAJOR', 'MINOR', 'PATCH', 'PRERELEASE'
    $groups = @{ STABLE = [version]$Stable; CURRENT = [version]$Current; NEXT = [version]$Next }
    foreach ($group in $groups.Keys) {
        $version = $groups[$group]
        $numbers = $version.Major, $version.Minor, $version.Build, $version.Revision
        $count = ($group -eq 'STABLE') ? 3 : 4
        for ($index = 0; $index -lt $count; $index++) {
            $variables["${Prefix}DATASTANDARDIZER_${Suffix}__${group}_$($parts[$index])_NUMBER"] = [string]$numbers[$index]
        }
        if ($group -ne 'STABLE') {
            $variables["${Prefix}DATASTANDARDIZER_${Suffix}__${group}_PRERELEASE_LABEL"] = $Label
        }
    }
    return $variables
}

Export-ModuleMember -Function New-TestFolder, Invoke-Git, New-TestRepository, Add-TestCommit, Add-TestTag, New-TestPackageInfo, `
    New-TestNuGetPackage, Get-TestNuspec, Install-FakeGh, Uninstall-FakeGh, Get-FakeGhVariable, Get-FakeGhLog, New-TestVersionVariables
