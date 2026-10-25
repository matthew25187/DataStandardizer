#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.5.0' }

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Force

    function New-State {
        param ([string]$Stable = '1.3.0', [string]$Current = '1.4.0.2', [string]$CurrentLabel = 'preview', [string]$Next = '1.4.0.3', [string]$NextLabel = 'preview')
        [pscustomobject]@{
            StableVersion          = [version]$Stable
            CurrentVersion         = [version]$Current
            CurrentPrereleaseLabel = $CurrentLabel
            NextVersion            = [version]$Next
            NextPrereleaseLabel    = $NextLabel
        }
    }
}

Describe 'VersionNumbers' {
    Context 'Formatting' {
        It 'shows a release in three parts and a preview in four' {
            ConvertTo-DisplayVersion -Version '1.4.0.0' | Should -Be '1.4.0'
            ConvertTo-DisplayVersion -Version '1.4.0.3' | Should -Be '1.4.0.3'
        }

        It 'formats the package version' {
            ConvertTo-SemanticVersion -Version '1.4.0.0' -PrereleaseLabel 'preview' | Should -Be '1.4.0'
            ConvertTo-SemanticVersion -Version '1.4.0.3' -PrereleaseLabel 'preview' | Should -Be '1.4.0-preview.3'
        }

        It 'names tags v<version>-<Package>' {
            ConvertTo-PackageTagName -Version '1.4.0.0' -PackageName 'DataStandardizer.Core' | Should -Be 'v1.4.0-DataStandardizer.Core'
            ConvertTo-PackageTagName -Version '0.2.0.10' -PackageName 'DataStandardizer.File.iCal' | Should -Be 'v0.2.0.10-DataStandardizer.File.iCal'
        }

        It 'parses only the package versions CI publishes' {
            (ConvertFrom-SemanticVersion '1.4.0').Version | Should -Be ([version]'1.4.0.0')
            $preview = ConvertFrom-SemanticVersion '1.4.0-preview.3'
            $preview.Version | Should -Be ([version]'1.4.0.3')
            $preview.PrereleaseLabel | Should -Be 'preview'
            $preview.IsPrerelease | Should -BeTrue
            ConvertFrom-SemanticVersion '1.4.0.3' | Should -BeNullOrEmpty
            ConvertFrom-SemanticVersion '1.4.0-preview' | Should -BeNullOrEmpty
            ConvertFrom-SemanticVersion '1.4.0-preview.0' | Should -BeNullOrEmpty
        }
    }

    Context 'New-PackageVersions' {
        It 'makes the production version the current one, with the assembly versions of the current package' {
            $assemblies = [pscustomobject]@{ AssemblyVersion = '1.4.0.12345'; FileVersion = '1.4.0.2'; InformationalVersion = '1.4' }
            $versions = New-PackageVersions -PackageName 'P' -State (New-State) -AssemblyVersions $assemblies
            $versions.PackageStableVersion | Should -Be '1.3.0'
            $versions.PackageProductionVersion | Should -Be '1.4.0.2'
            $versions.PackageProductionVersionDisplay | Should -Be '1.4.0.2'
            $versions.PackageProductionVersionSemVer | Should -Be '1.4.0-preview.2'
            $versions.AssemblyProductionVersion | Should -Be '1.4.0.12345'
            $versions.AssemblyProductionFileVersion | Should -Be '1.4.0.2'
            $versions.AssemblyProductionInformationalVersion | Should -Be '1.4'
        }

        It 'leaves assembly versions empty when there is no current package' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Current '0.0.0.0') -AssemblyVersions $null
            $versions.AssemblyProductionVersion | Should -BeNullOrEmpty
            $versions.PackageProductionVersionSemVer | Should -Be '0.0.0'
        }

        It 'has the properties the Azure DevOps version record has' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State) -AssemblyVersions $null
            $versions.PSObject.Properties.Name | Should -Be @(
                'PackageName', 'PackageStableVersion', 'PackageCurrentVersion', 'PackageCurrentPrereleaseLabel', 'PackageNextVersion',
                'PackageNextPrereleaseLabel', 'PackageProductionVersion', 'PackageProductionVersionDisplay', 'PackageProductionVersionSemVer',
                'PackageProductionPrereleaseLabel', 'AssemblyProductionVersion', 'AssemblyProductionFileVersion', 'AssemblyProductionInformationalVersion')
        }
    }

    Context 'Add-ProductionVersion' {
        BeforeAll {
            $script:Noon = [datetime]::new(2026, 10, 24, 12, 0, 0, [System.DateTimeKind]::Utc)
        }

        It 'produces the next preview and reserves the one after it' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Next '1.4.0.3') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $false -UtcNow $script:Noon
            $versions.PackageProductionVersion | Should -Be '1.4.0.3'
            $versions.PackageProductionVersionDisplay | Should -Be '1.4.0.3'
            $versions.PackageProductionVersionSemVer | Should -Be '1.4.0-preview.3'
            $versions.PackagePostProductionVersion | Should -Be '1.4.0.4'
        }

        It 'numbers the first preview after a release 1' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Next '1.5.0.0') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $false -UtcNow $script:Noon
            $versions.PackageProductionVersion | Should -Be '1.5.0.1'
            $versions.PackagePostProductionVersion | Should -Be '1.5.0.2'
        }

        It 'produces a release and reserves the next minor version' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Next '1.4.0.3') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $true -UtcNow $script:Noon
            $versions.PackageProductionVersion | Should -Be '1.4.0.0'
            $versions.PackageProductionVersionDisplay | Should -Be '1.4.0'
            $versions.PackageProductionVersionSemVer | Should -Be '1.4.0'
            $versions.PackagePostProductionVersion | Should -Be '1.5.0.0'
        }

        It 'produces a patch release and reserves the next patch, to continue the series' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Next '1.4.1.0') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $true -UtcNow $script:Noon
            $versions.PackageProductionVersionSemVer | Should -Be '1.4.1'
            $versions.PackagePostProductionVersion | Should -Be '1.4.2.0'
        }

        It 'numbers previews of a patch from the prerelease number' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Next '1.4.1.0') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $false -UtcNow $script:Noon
            $versions.PackageProductionVersionSemVer | Should -Be '1.4.1-preview.1'
            $versions.PackagePostProductionVersion | Should -Be '1.4.1.2'
        }

        It 'takes the prerelease label from the next version' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -CurrentLabel 'alpha' -NextLabel 'beta') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $false -UtcNow $script:Noon
            $versions.PackageProductionVersionSemVer | Should -Be '1.4.0-beta.3'
            $versions.PackageProductionPrereleaseLabel | Should -Be 'beta'
        }

        It 'sets the assembly versions from the production version and the time of day' {
            $versions = New-PackageVersions -PackageName 'P' -State (New-State -Next '1.4.0.3') -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $false -UtcNow $script:Noon
            $versions.AssemblyProductionFileVersion | Should -Be '1.4.0.3'
            $versions.AssemblyProductionVersion | Should -Be '1.4.0.21600'
            $versions.AssemblyProductionInformationalVersion | Should -Be '1.4'
        }

        It 'keeps the assembly revision within the 16-bit limit at the end of the day' {
            $endOfDay = [datetime]::new(2026, 10, 24, 23, 59, 59, [System.DateTimeKind]::Utc)
            $versions = New-PackageVersions -PackageName 'P' -State (New-State) -AssemblyVersions $null |
                Add-ProductionVersion -IsRelease $false -UtcNow $endOfDay
            ([version]$versions.AssemblyProductionVersion).Revision | Should -BeLessOrEqual 65534
        }
    }

    Context 'Get-MajorStartVersion' {
        It 'increments the major number and resets the rest' {
            Get-MajorStartVersion -StableVersion '1.3.0' -NextVersion '1.4.0.3' | Should -Be ([version]'2.0.0.0')
            Get-MajorStartVersion -StableVersion '0.2.1' -NextVersion '0.3.2.7' | Should -Be ([version]'1.0.0.0')
        }

        It 'refuses when a major version has already been started' {
            { Get-MajorStartVersion -StableVersion '1.3.0' -NextVersion '2.0.0.4' } | Should -Throw '*already ahead*'
            { Get-MajorStartVersion -StableVersion '0.0.0' -NextVersion '1.0.0.0' } | Should -Throw '*already ahead*'
        }
    }

    Context 'Get-MajorStartVersion -Major' {
        It 'jumps a pre-1.0 package to the chosen major version' {
            Get-MajorStartVersion -StableVersion '0.4.0' -NextVersion '0.5.0.2' -Major 2 | Should -Be ([version]'2.0.0.0')
            Get-MajorStartVersion -StableVersion '0.0.0' -NextVersion '0.1.0.0' -Major 1 | Should -Be ([version]'1.0.0.0')
        }

        It 'must be above the next version''s major number' {
            { Get-MajorStartVersion -StableVersion '1.3.0' -NextVersion '1.4.0.3' -Major 1 } | Should -Throw '*not above*'
            { Get-MajorStartVersion -StableVersion '1.3.0' -NextVersion '2.0.0.4' -Major 2 } | Should -Throw '*not above*'
        }
    }

    Context 'Get-PatchStartVersion' {
        It 'is the first patch of the last release' {
            Get-PatchStartVersion -StableVersion '1.1.0' -PackageName 'P' | Should -Be ([version]'1.1.1.0')
        }

        It 'follows the highest patch tagged for the last release, whatever the order' {
            $tags = 'v1.1.0-P', 'v1.1.2-P', 'v1.1.1-P', 'v1.2.0.4-P', 'v1.2.0-P'
            Get-PatchStartVersion -StableVersion '1.1.0' -PackageName 'P' -TagName $tags | Should -Be ([version]'1.1.3.0')
        }

        It 'ignores other majors, previews and packages' {
            $tags = 'v2.1.5-P', 'v1.1.7.2-P', 'v1.1.9-Q', 'v1.1.8-XP', 'v1.1.4-P'
            Get-PatchStartVersion -StableVersion '1.1.0' -PackageName 'P' -TagName $tags | Should -Be ([version]'1.1.5.0')
        }

        It 'does not go below the last release when its tag is missing' {
            Get-PatchStartVersion -StableVersion '1.1.3' -PackageName 'P' -TagName @() | Should -Be ([version]'1.1.4.0')
        }

        It 'refuses a package with no release' {
            { Get-PatchStartVersion -StableVersion '0.0.0' -PackageName 'P' } | Should -Throw '*no released version*'
        }
    }

    Context 'Encoding' {
        It 'round-trips version records' {
            $list = @(New-PackageVersions -PackageName 'P' -State (New-State) -AssemblyVersions $null)
            $encoded = ConvertTo-EncodedVersionList -InputObject $list
            $encoded | Should -Not -Match '\s'
            $decoded = @(ConvertFrom-EncodedVersionList -EncodedValue $encoded)
            $decoded.Count | Should -Be 1
            $decoded[0].PackageProductionVersionSemVer | Should -Be '1.4.0-preview.2'
        }

        It 'keeps a single record a JSON array' {
            $encoded = ConvertTo-EncodedVersionList -InputObject @([pscustomobject]@{ PackageName = 'P' })
            [System.Text.Encoding]::UTF8.GetString([System.Convert]::FromBase64String($encoded)) | Should -BeLike '`[*'
        }
    }

    Context 'Get-FinalizeAction' {
        BeforeAll {
            $script:Preview = [pscustomobject]@{ PackageName = 'P'; PackageProductionVersion = '1.4.0.3'; PackageProductionPrereleaseLabel = 'preview' }
            $script:Release = [pscustomobject]@{ PackageName = 'R'; PackageProductionVersion = '1.4.0.0'; PackageProductionPrereleaseLabel = 'preview' }
            $script:Reserved = @{
                P = [pscustomobject]@{ PreviousNext = '1.4.0.3'; WrittenNext = '1.4.0.4' }
                R = [pscustomobject]@{ PreviousNext = '1.4.0.3'; WrittenNext = '1.5.0.0' }
            }
        }

        It 'records a published preview as current only' {
            $actions = @(Get-FinalizeAction -PublishSucceeded $true -PackageVersions $script:Preview, $script:Release -Reserved @{ P = $script:Reserved.P } -StoredNext @{ P = [version]'1.4.0.4' })
            $actions.Count | Should -Be 1
            $actions[0].Group | Should -Be 'Current'
            $actions[0].Version | Should -Be ([version]'1.4.0.3')
            $actions[0].PrereleaseLabel | Should -Be 'preview'
        }

        It 'records a published release as current and stable' {
            $actions = @(Get-FinalizeAction -PublishSucceeded $true -PackageVersions $script:Preview, $script:Release -Reserved @{ R = $script:Reserved.R } -StoredNext @{ R = [version]'1.5.0.0' })
            $actions.Group | Should -Be @('Current', 'Stable')
            $actions | ForEach-Object { $_.Version | Should -Be ([version]'1.4.0.0') }
        }

        It 'records a publication as current even when a later run has reserved since' {
            $actions = @(Get-FinalizeAction -PublishSucceeded $true -PackageVersions $script:Preview -Reserved @{ P = $script:Reserved.P } -StoredNext @{ P = [version]'1.4.0.5' })
            $actions[0].Group | Should -Be 'Current'
        }

        It 'restores the next version after a failure when it is still the one reserved' {
            $actions = @(Get-FinalizeAction -PublishSucceeded $false -PackageVersions $script:Preview -Reserved @{ P = $script:Reserved.P } -StoredNext @{ P = [version]'1.4.0.4' })
            $actions[0].Group | Should -Be 'Next'
            $actions[0].Version | Should -Be ([version]'1.4.0.3')
        }

        It 'restores every package''s next version after a failure' {
            $reserved = @{ P = $script:Reserved.P; R = $script:Reserved.R }
            $actions = @(Get-FinalizeAction -PublishSucceeded $false -PackageVersions $script:Preview, $script:Release -Reserved $reserved `
                -StoredNext @{ P = [version]'1.4.0.4'; R = [version]'1.5.0.0' })
            $actions | ForEach-Object { "$($_.PackageName) $($_.Group) $($_.Version)" } | Should -Be @('P Next 1.4.0.3', 'R Next 1.4.0.3')
        }

        It 'leaves the next version alone after a failure when it has changed since' {
            $actions = @(Get-FinalizeAction -PublishSucceeded $false -PackageVersions $script:Release -Reserved @{ R = $script:Reserved.R } -StoredNext @{ R = [version]'1.4.1.0' })
            $actions[0].Group | Should -BeNullOrEmpty
            $actions[0].Reason | Should -BeLike 'left alone*1.4.1.0*1.5.0.0*'
        }

        It 'fails for a reserved package with no version record' {
            { Get-FinalizeAction -PublishSucceeded $true -PackageVersions @() -Reserved @{ P = $script:Reserved.P } -StoredNext @{} } | Should -Throw
        }
    }
}
