#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.5.0' }

# The scripts that stamp, pack and tag packages.

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot 'TestHelpers.psm1') -Force

    $script:ScriptsFolder = Join-Path $PSScriptRoot '..' 'scripts'
    $script:Packages = @(New-TestPackageInfo -PackageName 'DataStandardizer.Core', 'DataStandardizer.Geography', 'DataStandardizer.LanguageTag')

    $script:CiVariables = 'GITHUB_ACTIONS', 'GITHUB_OUTPUT', 'GITHUB_SHA', 'GITHUB_REF', 'DS_RELEASE_BUILD', 'PACKAGEINFOS', 'RUNNER_TEMP', 'VERSIONS'
    $script:SavedVariables = @{}
    foreach ($name in $script:CiVariables) {
        $script:SavedVariables[$name] = [System.Environment]::GetEnvironmentVariable($name)
    }

    # Build the encoded version list the scripts take.  Each record needs only the properties the scripts read.
    function ConvertTo-TestVersionList {
        param ([object[]]$Record)
        $json = ConvertTo-Json -InputObject @($Record) -Depth 4 -Compress
        return [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes($json))
    }

    function New-Record {
        param ($Name, $Production, $SemVer, $Stable = '0.0.0', $Assembly = $null, $File = $null, $Informational = $null)
        [pscustomobject]@{
            PackageName                            = $Name
            PackageStableVersion                   = $Stable
            PackageProductionVersion               = $Production
            PackageProductionVersionDisplay        = ([version]$Production).Revision -eq 0 ? ($Production -replace '\.0$', '') : $Production
            PackageProductionVersionSemVer         = $SemVer
            AssemblyProductionVersion              = $Assembly
            AssemblyProductionFileVersion          = $File
            AssemblyProductionInformationalVersion = $Informational
        }
    }
}

AfterAll {
    foreach ($name in $script:CiVariables) {
        [System.Environment]::SetEnvironmentVariable($name, $script:SavedVariables[$name])
    }
}

Describe 'Package scripts' {
    BeforeEach {
        foreach ($name in $script:CiVariables) {
            [System.Environment]::SetEnvironmentVariable($name, $null)
        }
        $env:PACKAGEINFOS = ConvertTo-Json -InputObject $script:Packages -Compress
    }

    AfterEach {
        while ((Get-Location).Path -like '*ds-build-tests-*') {
            Pop-Location
        }
    }

    Context 'WriteVersionProps.ps1' {
        BeforeEach {
            $script:Root = New-TestFolder
            foreach ($package in $script:Packages) {
                [void](New-Item -ItemType Directory -Path (Join-Path $script:Root $package.packageSourcePath))
            }
            $script:PropsPath = { param($Name) Join-Path $script:Root "src/$Name/Version.g.props" }
        }

        It 'stamps all three versions on the package and the assembly version on the others' {
            $versions = ConvertTo-TestVersionList @(
                (New-Record 'DataStandardizer.Core' '1.2.0.0' '1.2.0' -Assembly '1.2.0.100' -File '1.2.0.0' -Informational '1.2')
                (New-Record 'DataStandardizer.LanguageTag' '1.3.0.4' '1.3.0-preview.4' -Assembly '1.3.0.200' -File '1.3.0.4' -Informational '1.3')
                (New-Record 'DataStandardizer.Geography' '0.0.0.0' '0.0.0')
            )
            & (Join-Path $script:ScriptsFolder 'WriteVersionProps.ps1') -PackageName 'DataStandardizer.LanguageTag' -EncodedPackageVersions $versions -RepositoryRoot $script:Root 6>$null

            [xml]$own = Get-Content (& $script:PropsPath 'DataStandardizer.LanguageTag') -Raw
            $own.Project.PropertyGroup.AssemblyVersion | Should -Be '1.3.0.200'
            $own.Project.PropertyGroup.FileVersion | Should -Be '1.3.0.4'
            $own.Project.PropertyGroup.InformationalVersion | Should -Be '1.3'

            [xml]$dependency = Get-Content (& $script:PropsPath 'DataStandardizer.Core') -Raw
            $dependency.Project.PropertyGroup.AssemblyVersion | Should -Be '1.2.0.100'
            $dependency.Project.PropertyGroup.FileVersion | Should -BeNullOrEmpty

            Test-Path (& $script:PropsPath 'DataStandardizer.Geography') | Should -BeFalse
        }

        It 'removes a stale file from a package with no versions' {
            Set-Content -LiteralPath (& $script:PropsPath 'DataStandardizer.Geography') -Value '<Project />'
            $versions = ConvertTo-TestVersionList @((New-Record 'DataStandardizer.Geography' '0.0.0.0' '0.0.0'))
            & (Join-Path $script:ScriptsFolder 'WriteVersionProps.ps1') -PackageName 'DataStandardizer.Core' -EncodedPackageVersions $versions -RepositoryRoot $script:Root 6>$null 3>$null
            Test-Path (& $script:PropsPath 'DataStandardizer.Geography') | Should -BeFalse
        }

        It 'rejects a value that is not a version' {
            $versions = ConvertTo-TestVersionList @((New-Record 'DataStandardizer.Core' '1.2.0.0' '1.2.0' -Assembly '1.2.0.x'))
            { & (Join-Path $script:ScriptsFolder 'WriteVersionProps.ps1') -PackageName 'DataStandardizer.Core' -EncodedPackageVersions $versions -RepositoryRoot $script:Root 6>$null } |
                Should -Throw
        }
    }

    Context 'UpdatePackageMetadata.ps1' {
        BeforeEach {
            $env:RUNNER_TEMP = New-TestFolder
            $script:Folder = New-TestFolder
            $script:Notes = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("### Fixed`n- Something"))
            $script:Run = {
                param ($Package, $Versions)
                $ErrorActionPreference = 'Stop'
                & (Join-Path $script:ScriptsFolder 'UpdatePackageMetadata.ps1') -PackageName 'DataStandardizer.LanguageTag' -EncodedPackageVersions $Versions `
                    -PackageFilePath $Package -EncodedReleaseNotes $script:Notes *>$null
            }
            $script:Dependencies = { param($Package)
                $nuspec = Get-TestNuspec -PackagePath $Package -Id 'DataStandardizer.LanguageTag'
                $result = [ordered]@{}
                $nuspec.package.metadata.dependencies.group.dependency | ForEach-Object { $result[$_.id] = $_.version }
                return $result
            }
        }

        It 'gives a preview package the versions this run produces for its dependencies' {
            $package = New-TestNuGetPackage -Folder $script:Folder -Id 'DataStandardizer.LanguageTag' -Version '1.3.0-preview.4' -Dependency ([ordered]@{ 'DataStandardizer.Core' = '1.0.0'; 'DataStandardizer.Geography' = '1.0.0' })
            $versions = ConvertTo-TestVersionList @(
                (New-Record 'DataStandardizer.LanguageTag' '1.3.0.4' '1.3.0-preview.4' -Stable '1.2.0')
                (New-Record 'DataStandardizer.Core' '1.2.0.0' '1.2.0' -Stable '1.2.0')
                (New-Record 'DataStandardizer.Geography' '1.5.0.2' '1.5.0-preview.2' -Stable '1.4.0')
            )
            & $script:Run $package $versions
            $dependencies = & $script:Dependencies $package
            $dependencies['DataStandardizer.Core'] | Should -Be '1.2.0'
            $dependencies['DataStandardizer.Geography'] | Should -Be '1.5.0-preview.2'
            (Get-TestNuspec -PackagePath $package -Id 'DataStandardizer.LanguageTag').package.metadata.releaseNotes | Should -Be "### Fixed`n- Something"
        }

        It 'gives a release package the stable version of a dependency that is in preview' {
            $package = New-TestNuGetPackage -Folder $script:Folder -Id 'DataStandardizer.LanguageTag' -Version '1.3.0' -Dependency ([ordered]@{ 'DataStandardizer.Core' = '1.0.0'; 'DataStandardizer.Geography' = '1.0.0' })
            $versions = ConvertTo-TestVersionList @(
                (New-Record 'DataStandardizer.LanguageTag' '1.3.0.0' '1.3.0' -Stable '1.2.0')
                (New-Record 'DataStandardizer.Core' '1.2.0.0' '1.2.0' -Stable '1.2.0')
                (New-Record 'DataStandardizer.Geography' '1.5.0.2' '1.5.0-preview.2' -Stable '1.4.0')
            )
            & $script:Run $package $versions
            $dependencies = & $script:Dependencies $package
            $dependencies['DataStandardizer.Core'] | Should -Be '1.2.0'
            $dependencies['DataStandardizer.Geography'] | Should -Be '1.4.0'
        }

        It 'refuses a release package whose dependency has no stable version (#373)' {
            $package = New-TestNuGetPackage -Folder $script:Folder -Id 'DataStandardizer.LanguageTag' -Version '1.3.0' -Dependency ([ordered]@{ 'DataStandardizer.Geography' = '1.0.0' })
            $versions = ConvertTo-TestVersionList @(
                (New-Record 'DataStandardizer.LanguageTag' '1.3.0.0' '1.3.0' -Stable '1.2.0')
                (New-Record 'DataStandardizer.Geography' '0.1.0.2' '0.1.0-preview.2' -Stable '0.0.0')
            )
            { & $script:Run $package $versions } | Should -Throw '*prerelease dependencies*DataStandardizer.Geography 0.1.0-preview.2*'
        }

        It 'refuses a release package with a prerelease third-party dependency (#373)' {
            $package = New-TestNuGetPackage -Folder $script:Folder -Id 'DataStandardizer.LanguageTag' -Version '1.3.0' -Dependency ([ordered]@{ 'Some.Library' = '2.0.0-beta.1' })
            $versions = ConvertTo-TestVersionList @((New-Record 'DataStandardizer.LanguageTag' '1.3.0.0' '1.3.0' -Stable '1.2.0'))
            { & $script:Run $package $versions } | Should -Throw '*Some.Library 2.0.0-beta.1*'
        }

        It 'lets a preview package keep a prerelease third-party dependency' {
            $package = New-TestNuGetPackage -Folder $script:Folder -Id 'DataStandardizer.LanguageTag' -Version '1.3.0-preview.4' -Dependency ([ordered]@{ 'Some.Library' = '2.0.0-beta.1' })
            $versions = ConvertTo-TestVersionList @((New-Record 'DataStandardizer.LanguageTag' '1.3.0.4' '1.3.0-preview.4'))
            { & $script:Run $package $versions } | Should -Not -Throw
        }

        It 'checks the length of the repository URL (#372)' {
            $package = New-TestNuGetPackage -Folder $script:Folder -Id 'DataStandardizer.LanguageTag' -Version '1.3.0' -RepositoryUrl ('https://example.com/' + ('x' * 4000))
            $versions = ConvertTo-TestVersionList @((New-Record 'DataStandardizer.LanguageTag' '1.3.0.0' '1.3.0'))
            { & $script:Run $package $versions } | Should -Throw "*'url' exceeds*"
        }
    }

    Context 'TagPackageVersions.ps1' {
        BeforeEach {
            Push-Location (New-TestRepository)
            $env:GITHUB_SHA = Add-TestCommit -File @{ 'README.md' = 'readme' }
            $script:Versions = ConvertTo-TestVersionList @(
                (New-Record 'DataStandardizer.Core' '1.2.0.3' '1.2.0-preview.3')
                (New-Record 'DataStandardizer.Geography' '1.5.0.0' '1.5.0')
            )
        }

        It 'creates annotated tags on the built commit' {
            $env:GITHUB_REF = 'refs/heads/develop'
            Add-TestCommit -File @{ 'README.md' = 'later' } | Out-Null
            & (Join-Path $script:ScriptsFolder 'TagPackageVersions.ps1') -PackageSet '["DataStandardizer.Core","DataStandardizer.Geography"]' -EncodedPackageVersions $script:Versions -NoPush 6>$null
            (Invoke-Git rev-list -n 1 'v1.2.0.3-DataStandardizer.Core') | Should -Be $env:GITHUB_SHA
            (Invoke-Git cat-file -t 'v1.5.0-DataStandardizer.Geography') | Should -Be 'tag'
            (Invoke-Git tag -l --format='%(contents:subject)' 'v1.2.0.3-DataStandardizer.Core') | Should -Be 'CI preview build 1.2.0.3 for package DataStandardizer.Core'
        }

        It 'describes a release build' {
            $env:GITHUB_REF = 'refs/heads/master'
            & (Join-Path $script:ScriptsFolder 'TagPackageVersions.ps1') -PackageSet 'Geography' -EncodedPackageVersions $script:Versions -NoPush 6>$null
            (Invoke-Git tag -l --format='%(contents:subject)' 'v1.5.0-DataStandardizer.Geography') | Should -Be 'Release build 1.5.0 for package DataStandardizer.Geography'
        }

        It 'fails when the tag already exists' {
            Add-TestTag -Name 'v1.2.0.3-DataStandardizer.Core'
            { & (Join-Path $script:ScriptsFolder 'TagPackageVersions.ps1') -PackageSet 'Core' -EncodedPackageVersions $script:Versions -NoPush 6>$null } | Should -Throw '*already exists*'
        }
    }

    Context 'SetReleaseNotesArgument.ps1' {
        BeforeAll {
            # A repository holding a package tag, for the commits a test adds after it.
            function New-TaggedTestRepository {
                Push-Location (New-TestRepository)
                Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core' } -Message 'Initial commit' | Out-Null
                Add-TestTag -Name 'v1.0.0.1-DataStandardizer.Core'
                Add-TestTag -Name 'v1.0.0.1-DataStandardizer.Geography'
            }

            # Runs the script on the current commit and returns the notes it wrote as a step output.
            function Get-TestReleaseNote {
                param ([string]$PackageName = 'DataStandardizer.Core', [hashtable]$Option = @{})
                $env:GITHUB_SHA = Invoke-Git rev-parse HEAD
                $env:GITHUB_REF = 'refs/heads/develop'
                $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
                & (Join-Path $script:ScriptsFolder 'SetReleaseNotesArgument.ps1') -PackageName $PackageName @Option *>$null
                $line = Get-Content $env:GITHUB_OUTPUT | Where-Object { $_ -like 'releaseNotesArg=*' }
                return [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(($line -split '=', 2)[1]))
            }

            # Writes a release note overrides file and returns its path.
            function New-TestOverrideFile {
                param ([string]$Commit, [string]$Subject, [object[]]$Note)
                $path = Join-Path (New-TestFolder) 'overrides.json'
                $commits = @{ $Commit = @{ subject = $Subject; notes = $Note } }
                [System.IO.File]::WriteAllText($path, (ConvertTo-Json -InputObject @{ commits = $commits } -Depth 6))
                return $path
            }
        }

        It 'publishes the trailer of a commit that changes only <File>' -ForEach @(
            @{ File = 'Directory.Build.props' }
            @{ File = 'Directory.Build.targets' }
            @{ File = 'Directory.Packages.props' }
            @{ File = 'src/Directory.Build.props' }
            @{ File = 'src/Directory.Build.targets' }
        ) {
            New-TaggedTestRepository
            Add-TestCommit -File @{ $File = '<Project />' } -Message "Embed the symbols`n`nRelease-note: fixed: Debuggers can step into the source" | Out-Null

            Get-TestReleaseNote | Should -Be "Fixed:`n- Debuggers can step into the source."
        }

        It 'publishes a commit that changes only a shared build file for every package' {
            New-TaggedTestRepository
            Add-TestCommit -File @{ 'src/Directory.Build.props' = '<Project />' } -Message "Embed the symbols`n`nRelease-note: fixed" | Out-Null

            Get-TestReleaseNote -PackageName 'DataStandardizer.Geography' | Should -Be "Fixed:`n- Embed the symbols."
        }

        It 'infers the note of a commit that changes only a shared build file and declares nothing' {
            New-TaggedTestRepository
            Add-TestCommit -File @{ 'Directory.Packages.props' = '<Project />' } -Message 'Bump System.Memory from 4.6.2 to 4.6.3' | Out-Null

            Get-TestReleaseNote | Should -Be "Changed:`n- Bump System.Memory from 4.6.2 to 4.6.3."
        }

        It 'leaves out a commit that changes only <File>' -ForEach @(
            @{ File = 'src/DataStandardizer.Geography/Geography.cs' }
            @{ File = 'tests/Directory.Build.props' }
            @{ File = 'global.json' }
            @{ File = 'README.md' }
        ) {
            New-TaggedTestRepository
            Add-TestCommit -File @{ $File = 'changed' } -Message "Change something else`n`nRelease-note: fixed" | Out-Null

            Get-TestReleaseNote | Should -Be 'No changes affect the use of this package.'
        }

        It 'publishes one note for a commit that changes both the package and a shared build file' {
            New-TaggedTestRepository
            Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core 2'; 'src/Directory.Build.props' = '<Project />' } -Message "Change both`n`nRelease-note: fixed" | Out-Null

            Get-TestReleaseNote | Should -Be "Fixed:`n- Change both."
        }

        It 'publishes an override of a commit that changes only a shared build file' {
            New-TaggedTestRepository
            $commit = Add-TestCommit -File @{ 'Directory.Packages.props' = '<Project />' } -Message 'Update the packages'
            $overridePath = New-TestOverrideFile -Commit $commit -Subject 'Update the packages' -Note @(@{ category = 'fixed'; text = 'The stated change' })

            Get-TestReleaseNote -Option @{ OverridePath = $overridePath } | Should -Be "Fixed:`n- The stated change."
        }

        It 'publishes an override note limited to a package only for that package, for <PackageName>' -ForEach @(
            @{ PackageName = 'DataStandardizer.Core'; Expected = "Fixed:`n- Only for Core.`n- For every package." }
            @{ PackageName = 'DataStandardizer.Geography'; Expected = "Fixed:`n- For every package." }
        ) {
            New-TaggedTestRepository
            $commit = Add-TestCommit -File @{ 'Directory.Packages.props' = '<Project />' } -Message 'Update the packages'
            $overridePath = New-TestOverrideFile -Commit $commit -Subject 'Update the packages' -Note @(
                @{ category = 'fixed'; text = 'Only for Core'; packages = @('DataStandardizer.Core') }
                @{ category = 'fixed'; text = 'For every package' }
            )

            Get-TestReleaseNote -PackageName $PackageName -Option @{ OverridePath = $overridePath } | Should -Be $Expected
        }

        It 'refuses an override note limited to an unknown package' {
            New-TaggedTestRepository
            $commit = Add-TestCommit -File @{ 'Directory.Packages.props' = '<Project />' } -Message 'Update the packages'
            $overridePath = New-TestOverrideFile -Commit $commit -Subject 'Update the packages' -Note @(@{ category = 'fixed'; text = 'A note'; packages = @('DataStandardizer.Nothing') })
            $env:GITHUB_SHA = $commit
            $env:GITHUB_REF = 'refs/heads/develop'

            {
                $ErrorActionPreference = 'Stop'
                & (Join-Path $script:ScriptsFolder 'SetReleaseNotesArgument.ps1') -PackageName 'DataStandardizer.Core' -OverridePath $overridePath *>$null
            } | Should -Throw '*DataStandardizer.Nothing*'
        }

        It 'writes the encoded notes as a GitHub step output' {
            Push-Location (New-TestRepository)
            Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core' } -Message 'Initial commit' | Out-Null
            Add-TestTag -Name 'v1.0.0.1-DataStandardizer.Core'
            $env:GITHUB_SHA = Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core 2' } -Message "Fix the core thing`n`nRelease-note: fixed"
            $env:GITHUB_REF = 'refs/heads/develop'
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            & (Join-Path $script:ScriptsFolder 'SetReleaseNotesArgument.ps1') -PackageName 'DataStandardizer.Core' *>$null
            $line = Get-Content $env:GITHUB_OUTPUT | Where-Object { $_ -like 'releaseNotesArg=*' }
            $notes = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(($line -split '=', 2)[1]))
            $notes | Should -Match 'Fixed'
            $notes | Should -Match 'Fix the core thing'
        }

        It 'publishes <Subject> as <Expected>' -ForEach @(
            # An apostrophe within a word is not a quote, so it doesn't keep the reference (#353).
            @{ Subject = "Give the right offset before a VTIMEZONE's first onset (#294)"; Expected = "Give the right offset before a VTIMEZONE's first onset" }
            @{ Subject = "Keep the TRIGGER's parameters (#264, #282)"; Expected = "Keep the TRIGGER's parameters" }
            @{ Subject = "Don't drop the users' data (#12)"; Expected = "Don't drop the users' data" }
            # A reference between a matching pair of quotes is the subject matter, and is kept.
            @{ Subject = "Report '(#12)' as text"; Expected = "Report '(#12)' as text" }
            @{ Subject = 'Report "it''s (#12)" as text'; Expected = 'Report "it''s (#12)" as text' }
            # Curly quotes reach the notes intact whatever the console encoding is (#438).
            @{ Subject = "Don$([char]0x2019)t drop the users' data (#12)"; Expected = "Don$([char]0x2019)t drop the users' data"; ConsoleEncoding = [System.Text.Encoding]::Latin1 }
            @{ Subject = "Report $([char]0x201C)it's (#12)$([char]0x201D) as text"; Expected = "Report $([char]0x201C)it's (#12)$([char]0x201D) as text"; ConsoleEncoding = [System.Text.Encoding]::Latin1 }
        ) {
            if ($null -ne $ConsoleEncoding) {
                $savedEncoding = [Console]::OutputEncoding
                [Console]::OutputEncoding = $ConsoleEncoding
            }
            Push-Location (New-TestRepository)
            Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core' } -Message 'Initial commit' | Out-Null
            Add-TestTag -Name 'v1.0.0.1-DataStandardizer.Core'
            $env:GITHUB_SHA = Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core 2' } -Message "$Subject`n`nRelease-note: fixed"
            $env:GITHUB_REF = 'refs/heads/develop'
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            try {
                & (Join-Path $script:ScriptsFolder 'SetReleaseNotesArgument.ps1') -PackageName 'DataStandardizer.Core' *>$null
            }
            finally {
                if ($null -ne $ConsoleEncoding) {
                    [Console]::OutputEncoding = $savedEncoding
                }
            }
            $line = Get-Content $env:GITHUB_OUTPUT | Where-Object { $_ -like 'releaseNotesArg=*' }
            $notes = [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String(($line -split '=', 2)[1]))
            $notes | Should -Match "(?m)^- $([regex]::Escape($Expected))\.?$"
        }
    }

    Context 'ConfirmSatelliteAssemblies.ps1' {
        BeforeAll {
            # A package whose lib/<framework> folders carry the given cultures' satellite assemblies.
            function New-SatellitePackage {
                param ([string]$Folder, [string[]]$Framework, [string[]]$Culture)
                $content = Join-Path $Folder 'content.dll'
                [System.IO.File]::WriteAllText($content, 'not really an assembly')
                $files = @{}
                foreach ($tfm in $Framework) {
                    $files["lib/$tfm/DataStandardizer.Money.dll"] = $content
                    foreach ($name in $Culture) {
                        $files["lib/$tfm/$name/DataStandardizer.Money.resources.dll"] = $content
                    }
                }
                [void](New-TestNuGetPackage -Folder $Folder -Id 'DataStandardizer.Money' -Version '1.0.0-preview.1' -ExtraFile $files)
            }
        }

        It 'passes when every target framework carries enough cultures' {
            $folder = New-TestFolder
            New-SatellitePackage -Folder $folder -Framework 'netstandard2.0', 'net10.0' -Culture 'de', 'fr', 'ja'
            { & (Join-Path $script:ScriptsFolder 'ConfirmSatelliteAssemblies.ps1') -PackageFolder $folder -PackageName 'DataStandardizer.Money' -MinimumCultureCount 3 6>$null } |
                Should -Not -Throw
        }

        It 'fails when a target framework carries too few cultures' {
            $folder = New-TestFolder
            New-SatellitePackage -Folder $folder -Framework 'netstandard2.0' -Culture 'de', 'fr'
            { & (Join-Path $script:ScriptsFolder 'ConfirmSatelliteAssemblies.ps1') -PackageFolder $folder -PackageName 'DataStandardizer.Money' -MinimumCultureCount 3 6>$null } |
                Should -Throw '*missing satellite assemblies*'
        }

        It 'fails when the package carries no satellite assemblies' {
            $folder = New-TestFolder
            New-SatellitePackage -Folder $folder -Framework 'netstandard2.0' -Culture @()
            { & (Join-Path $script:ScriptsFolder 'ConfirmSatelliteAssemblies.ps1') -PackageFolder $folder -PackageName 'DataStandardizer.Money' 6>$null } |
                Should -Throw '*contains no satellite assemblies*'
        }
    }
}
