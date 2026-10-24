#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.5.0' }

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionVariables/VersionVariables.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot '../psmodules/VersionNumbers/VersionNumbers.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot 'TestHelpers.psm1') -Force

    $script:ScriptsFolder = Join-Path $PSScriptRoot '..' 'scripts'
    $script:Core = New-TestPackageInfo -PackageName 'DataStandardizer.Core'
    $script:Csv = New-TestPackageInfo -PackageName 'DataStandardizer.File.CSV'

    $script:CiVariables = 'TF_BUILD', 'GITHUB_ACTIONS', 'GITHUB_OUTPUT', 'GITHUB_SHA', 'GITHUB_REF', 'DS_RELEASE_BUILD', 'PACKAGEINFOS'
    $script:SavedVariables = @{}
    foreach ($name in $script:CiVariables) {
        $script:SavedVariables[$name] = [System.Environment]::GetEnvironmentVariable($name)
    }

    function Read-Outputs {
        $outputs = @{}
        Get-Content $env:GITHUB_OUTPUT | Where-Object { $_ } | ForEach-Object { $name, $value = $_ -split '=', 2; $outputs[$name] = $value }
        return $outputs
    }
}

AfterAll {
    foreach ($name in $script:CiVariables) {
        [System.Environment]::SetEnvironmentVariable($name, $script:SavedVariables[$name])
    }
}

Describe 'VersionVariables' {
    BeforeEach {
        foreach ($name in $script:CiVariables) {
            [System.Environment]::SetEnvironmentVariable($name, $null)
        }
    }

    AfterEach {
        Uninstall-FakeGh
    }

    Context 'Get-VersionVariableName' {
        It 'names numbers and labels' {
            Get-VersionVariableName -VariableSuffix 'FILE_CSV' -Group Next -Part Major | Should -Be 'DATASTANDARDIZER_FILE_CSV__NEXT_MAJOR_NUMBER'
            Get-VersionVariableName -VariableSuffix 'CORE' -Group Current -Part Label | Should -Be 'DATASTANDARDIZER_CORE__CURRENT_PRERELEASE_LABEL'
            Get-VersionVariableName -VariableSuffix 'CORE' -Group Stable -Part Patch -Prefix 'SHADOW_' | Should -Be 'SHADOW_DATASTANDARDIZER_CORE__STABLE_PATCH_NUMBER'
        }

        It 'gives no two packages a common variable' {
            $names = Get-PackageInfo | ForEach-Object {
                $suffix = $_.variableSuffix
                foreach ($group in 'Stable', 'Current', 'Next') {
                    foreach ($part in 'Major', 'Minor', 'Patch', 'Prerelease', 'Label') {
                        Get-VersionVariableName -VariableSuffix $suffix -Group $group -Part $part
                    }
                }
            }
            ($names | Sort-Object -Unique).Count | Should -Be $names.Count
        }
    }

    Context 'Get-PackageVersionState' {
        It 'reads the stored versions' {
            $variables = [hashtable]::new((New-TestVersionVariables -Suffix 'CORE'), [System.StringComparer]::OrdinalIgnoreCase)
            $state = Get-PackageVersionState -PackageInfo $script:Core -Variables $variables
            $state.StableVersion | Should -Be ([version]'1.3.0')
            $state.CurrentVersion | Should -Be ([version]'1.4.0.2')
            $state.CurrentPrereleaseLabel | Should -Be 'preview'
            $state.NextVersion | Should -Be ([version]'1.4.0.3')
        }

        It 'counts missing numbers as 0' {
            $state = Get-PackageVersionState -PackageInfo $script:Core -Variables @{}
            $state.StableVersion | Should -Be ([version]'0.0.0')
            $state.CurrentVersion | Should -Be ([version]'0.0.0.0')
        }

        It 'reads the prefixed copy' {
            $variables = (New-TestVersionVariables -Suffix 'CORE') + (New-TestVersionVariables -Suffix 'CORE' -Next '9.0.0.1' -Prefix 'SHADOW_')
            (Get-PackageVersionState -PackageInfo $script:Core -Variables $variables -Prefix 'SHADOW_').NextVersion | Should -Be ([version]'9.0.0.1')
        }

        It 'rejects a value that is not a number' {
            $variables = New-TestVersionVariables -Suffix 'CORE'
            $variables['DATASTANDARDIZER_CORE__NEXT_MINOR_NUMBER'] = 'four'
            { Get-PackageVersionState -PackageInfo $script:Core -Variables $variables } | Should -Throw '*NEXT_MINOR_NUMBER*four*'
        }
    }

    Context 'Set-PackageVersionState' {
        It 'writes each existing part from major to prerelease, then the label' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE') | Out-Null
            $variables = Read-VersionVariableSet
            $written = Set-PackageVersionState -PackageInfo $script:Core -Group Current -Version '1.4.0.3' -PrereleaseLabel 'beta' -SetPrereleaseLabel -Variables $variables
            $written | Should -Be @(
                'DATASTANDARDIZER_CORE__CURRENT_MAJOR_NUMBER', 'DATASTANDARDIZER_CORE__CURRENT_MINOR_NUMBER', 'DATASTANDARDIZER_CORE__CURRENT_PATCH_NUMBER',
                'DATASTANDARDIZER_CORE__CURRENT_PRERELEASE_NUMBER', 'DATASTANDARDIZER_CORE__CURRENT_PRERELEASE_LABEL')
            $stored = Get-PackageVersionState -PackageInfo $script:Core -Variables (Read-VersionVariableSet)
            $stored.CurrentVersion | Should -Be ([version]'1.4.0.3')
            $stored.CurrentPrereleaseLabel | Should -Be 'beta'
        }

        It 'skips variables that do not exist, such as a stable prerelease number' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE') | Out-Null
            $written = Set-PackageVersionState -PackageInfo $script:Core -Group Stable -Version '1.4.0.0' -Variables (Read-VersionVariableSet)
            $written.Count | Should -Be 3
            (Get-FakeGhVariable)['DATASTANDARDIZER_CORE__STABLE_MINOR_NUMBER'] | Should -Be '4'
            (Get-FakeGhVariable).ContainsKey('DATASTANDARDIZER_CORE__STABLE_PRERELEASE_NUMBER') | Should -BeFalse
        }

        It 'writes nothing with -WhatIf' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE') | Out-Null
            Set-PackageVersionState -PackageInfo $script:Core -Group Next -Version '2.0.0.0' -Variables (Read-VersionVariableSet) -WhatIf | Out-Null
            Get-FakeGhLog | Should -BeNullOrEmpty
        }
    }

    Context 'ResolveVersionNumbers.ps1' {
        BeforeEach {
            # Never-released packages, so nothing is downloaded.
            $script:Packages = @($script:Core, $script:Csv)
            $env:PACKAGEINFOS = ConvertTo-Json -InputObject $script:Packages -Compress
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            $env:GITHUB_REF = 'refs/heads/develop'
            $script:Variables = (New-TestVersionVariables -Suffix 'CORE' -Stable '0.0.0' -Current '0.0.0.0' -Next '1.0.0.0') +
                (New-TestVersionVariables -Suffix 'FILE_CSV' -Stable '0.0.0' -Current '0.0.0.0' -Next '1.2.0.5')
        }

        It 'reserves the next versions of the packages it publishes and reports every package' {
            Install-FakeGh -Variables $script:Variables | Out-Null
            & (Join-Path $script:ScriptsFolder 'ResolveVersionNumbers.ps1') -PublishSet '["DataStandardizer.File.CSV"]' -Reserve 6>$null
            $outputs = Read-Outputs
            $versions = @(ConvertFrom-EncodedVersionList -EncodedValue $outputs['versions'])
            $versions.PackageName | Should -Be @('DataStandardizer.Core', 'DataStandardizer.File.CSV')
            ($versions | Where-Object PackageName -EQ 'DataStandardizer.File.CSV').PackageProductionVersionSemVer | Should -Be '1.2.0-preview.5'
            ($versions | Where-Object PackageName -EQ 'DataStandardizer.Core').PackageProductionVersionSemVer | Should -Be '0.0.0'
            $reserved = ConvertFrom-EncodedVersionList -EncodedValue $outputs['reserved']
            $reserved.'DataStandardizer.File.CSV'.PreviousNext | Should -Be '1.2.0.5'
            $reserved.'DataStandardizer.File.CSV'.WrittenNext | Should -Be '1.2.0.6'
            $reserved.PSObject.Properties.Name | Should -Not -Contain 'DataStandardizer.Core'
            (Get-FakeGhVariable)['DATASTANDARDIZER_FILE_CSV__NEXT_PRERELEASE_NUMBER'] | Should -Be '6'
            (Get-FakeGhVariable)['DATASTANDARDIZER_CORE__NEXT_PRERELEASE_NUMBER'] | Should -Be '0'
        }

        It 'reserves the next minor version on a release build' {
            $env:GITHUB_REF = 'refs/heads/master'
            Install-FakeGh -Variables $script:Variables | Out-Null
            & (Join-Path $script:ScriptsFolder 'ResolveVersionNumbers.ps1') -PublishSet 'File.CSV' -Reserve 6>$null
            $versions = @(ConvertFrom-EncodedVersionList -EncodedValue (Read-Outputs)['versions'])
            ($versions | Where-Object PackageName -EQ 'DataStandardizer.File.CSV').PackageProductionVersionSemVer | Should -Be '1.2.0'
            $stored = Get-FakeGhVariable
            $stored['DATASTANDARDIZER_FILE_CSV__NEXT_MINOR_NUMBER'] | Should -Be '3'
            $stored['DATASTANDARDIZER_FILE_CSV__NEXT_PRERELEASE_NUMBER'] | Should -Be '0'
        }

        It 'writes nothing without -Reserve, reading a snapshot file' {
            $snapshot = Join-Path (New-TestFolder) 'vars.json'
            ConvertTo-Json -InputObject $script:Variables | Set-Content -LiteralPath $snapshot
            Install-FakeGh -Variables $script:Variables | Out-Null
            & (Join-Path $script:ScriptsFolder 'ResolveVersionNumbers.ps1') -PublishSet 'File.CSV' -VariablesFile $snapshot 6>$null
            Get-FakeGhLog | Should -BeNullOrEmpty
            (Read-Outputs)['versions'] | Should -Not -BeNullOrEmpty
        }

        It 'refuses to reserve from a snapshot' {
            { & (Join-Path $script:ScriptsFolder 'ResolveVersionNumbers.ps1') -PublishSet 'File.CSV' -Reserve -VariablesFile 'x.json' } | Should -Throw '*-Reserve*'
        }

        It 'works on a prefixed copy only' {
            Install-FakeGh -Variables ($script:Variables + (New-TestVersionVariables -Suffix 'FILE_CSV' -Current '0.0.0.0' -Next '7.0.0.1' -Prefix 'SHADOW_')) | Out-Null
            & (Join-Path $script:ScriptsFolder 'ResolveVersionNumbers.ps1') -PublishSet 'File.CSV' -Reserve -VariablePrefix 'SHADOW_' 6>$null
            $stored = Get-FakeGhVariable
            $stored['SHADOW_DATASTANDARDIZER_FILE_CSV__NEXT_PRERELEASE_NUMBER'] | Should -Be '2'
            $stored['DATASTANDARDIZER_FILE_CSV__NEXT_PRERELEASE_NUMBER'] | Should -Be '5'
        }
    }

    Context 'FinalizeVersionNumbers.ps1' {
        BeforeEach {
            $env:PACKAGEINFOS = ConvertTo-Json -InputObject @($script:Core) -Compress
            $script:Versions = ConvertTo-EncodedVersionList -InputObject @([pscustomobject]@{
                PackageName = 'DataStandardizer.Core'; PackageProductionVersion = '1.4.0.3'; PackageProductionPrereleaseLabel = 'preview'
            })
            $script:ReleaseVersions = ConvertTo-EncodedVersionList -InputObject @([pscustomobject]@{
                PackageName = 'DataStandardizer.Core'; PackageProductionVersion = '1.4.0.0'; PackageProductionPrereleaseLabel = 'preview'
            })
            $script:Reserved = ConvertTo-EncodedVersionList -InputObject @{ 'DataStandardizer.Core' = @{ PreviousNext = '1.4.0.3'; WrittenNext = '1.4.0.4' } }
            $script:ReleaseReserved = ConvertTo-EncodedVersionList -InputObject @{ 'DataStandardizer.Core' = @{ PreviousNext = '1.4.0.3'; WrittenNext = '1.5.0.0' } }
        }

        It 'records a published preview as current' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE' -Next '1.4.0.4') | Out-Null
            & (Join-Path $script:ScriptsFolder 'FinalizeVersionNumbers.ps1') -PublishResult success -EncodedPackageVersions $script:Versions -EncodedReserved $script:Reserved 6>$null
            $state = Get-PackageVersionState -PackageInfo $script:Core -Variables (Get-FakeGhVariable)
            $state.CurrentVersion | Should -Be ([version]'1.4.0.3')
            $state.StableVersion | Should -Be ([version]'1.3.0')
            $state.NextVersion | Should -Be ([version]'1.4.0.4')
        }

        It 'records a published release as current and stable' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE' -Next '1.5.0.0') | Out-Null
            & (Join-Path $script:ScriptsFolder 'FinalizeVersionNumbers.ps1') -PublishResult success -EncodedPackageVersions $script:ReleaseVersions -EncodedReserved $script:ReleaseReserved 6>$null
            $state = Get-PackageVersionState -PackageInfo $script:Core -Variables (Get-FakeGhVariable)
            $state.CurrentVersion | Should -Be ([version]'1.4.0.0')
            $state.StableVersion | Should -Be ([version]'1.4.0')
        }

        It 'restores the reserved next version after a failure' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE' -Next '1.4.0.4') | Out-Null
            & (Join-Path $script:ScriptsFolder 'FinalizeVersionNumbers.ps1') -PublishResult failure -EncodedPackageVersions $script:Versions -EncodedReserved $script:Reserved 6>$null
            $state = Get-PackageVersionState -PackageInfo $script:Core -Variables (Get-FakeGhVariable)
            $state.NextVersion | Should -Be ([version]'1.4.0.3')
            $state.CurrentVersion | Should -Be ([version]'1.4.0.2')
        }

        It 'leaves a next version changed since the reservation alone after a cancellation' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE' -Next '1.4.1.0') | Out-Null
            & (Join-Path $script:ScriptsFolder 'FinalizeVersionNumbers.ps1') -PublishResult cancelled -EncodedPackageVersions $script:Versions -EncodedReserved $script:Reserved 6>$null 3>$null
            (Get-PackageVersionState -PackageInfo $script:Core -Variables (Get-FakeGhVariable)).NextVersion | Should -Be ([version]'1.4.1.0')
            Get-FakeGhLog | Should -BeNullOrEmpty
        }

        It 'does nothing when nothing was reserved' {
            Install-FakeGh -Variables (New-TestVersionVariables -Suffix 'CORE') | Out-Null
            & (Join-Path $script:ScriptsFolder 'FinalizeVersionNumbers.ps1') -PublishResult success -EncodedPackageVersions $script:Versions -EncodedReserved '' 6>$null
            Get-FakeGhLog | Should -BeNullOrEmpty
        }
    }
}
