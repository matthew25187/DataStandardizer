#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.5.0' }

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../psmodules/BuildPlan/BuildPlan.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot 'TestHelpers.psm1') -Force

    $script:Packages = @(New-TestPackageInfo -PackageName 'DataStandardizer.Core', 'DataStandardizer.File', 'DataStandardizer.File.CSV')
    $script:ScriptsFolder = Join-Path $PSScriptRoot '..' 'scripts'

    $script:CiVariables = 'GITHUB_ACTIONS', 'GITHUB_OUTPUT', 'GITHUB_SHA', 'GITHUB_REF', 'DS_RELEASE_BUILD', 'PACKAGEINFOS'
    $script:SavedVariables = @{}
    foreach ($name in $script:CiVariables) {
        $script:SavedVariables[$name] = [System.Environment]::GetEnvironmentVariable($name)
    }

    function Initialize-Repository {
        # Core, File and File.CSV, each with a source file, released once and then previewed once.
        $path = New-TestRepository
        Push-Location $path
        Add-TestCommit -File @{
            'src/DataStandardizer.Core/Core.cs'         = 'core'
            'src/DataStandardizer.File/File.cs'         = 'file'
            'src/DataStandardizer.File.CSV/Csv.cs'      = 'csv'
            'tests/DataStandardizer.Core.Tests/T.cs'    = 'test'
            'README.md'                                 = 'readme'
        } -Message 'Initial commit' | Out-Null
        foreach ($package in $script:Packages) {
            Add-TestTag -Name "v1.0.0-$($package.packageName)"
        }
        Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core 2'; 'src/DataStandardizer.File/File.cs' = 'file 2'; 'src/DataStandardizer.File.CSV/Csv.cs' = 'csv 2' } | Out-Null
        foreach ($package in $script:Packages) {
            Add-TestTag -Name "v1.1.0.1-$($package.packageName)"
        }
        return $path
    }
}

AfterAll {
    foreach ($name in $script:CiVariables) {
        [System.Environment]::SetEnvironmentVariable($name, $script:SavedVariables[$name])
    }
}

Describe 'BuildPlan' {
    BeforeEach {
        foreach ($name in $script:CiVariables) {
            [System.Environment]::SetEnvironmentVariable($name, $null)
        }
    }

    AfterEach {
        while ((Get-Location).Path -like '*ds-build-tests-*') {
            Pop-Location
        }
    }

    Context 'Test-PathInFolder' {
        It 'matches whole path segments' {
            Test-PathInFolder -FolderPath 'src/DataStandardizer.File' -Path 'src/DataStandardizer.File' | Should -BeTrue
            Test-PathInFolder -FolderPath 'src/DataStandardizer.File' -Path 'src/DataStandardizer.File/Sub' | Should -BeTrue
            Test-PathInFolder -FolderPath 'src/DataStandardizer.File' -Path 'src\DataStandardizer.File\Sub' | Should -BeTrue
            Test-PathInFolder -FolderPath 'src/DataStandardizer.File' -Path 'src/DataStandardizer.File.CSV' | Should -BeFalse
            Test-PathInFolder -FolderPath 'src/DataStandardizer.File' -Path 'src' | Should -BeFalse
            Test-PathInFolder -FolderPath 'src/DataStandardizer.File' -Path '' | Should -BeFalse
        }
    }

    Context 'Get-ChangedPackageName' {
        It 'lists only the packages changed since their latest preview' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'src/DataStandardizer.File.CSV/Csv.cs' = 'csv 3' }
            Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false | Should -Be @('DataStandardizer.File.CSV')
        }

        It 'does not count a change to a package whose name extends another package''s' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'src/DataStandardizer.File.CSV/Csv.cs' = 'csv 3' }
            Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false | Should -Not -Contain 'DataStandardizer.File'
        }

        It 'ignores changes outside package folders' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'README.md' = 'readme 2'; 'tests/DataStandardizer.Core.Tests/T.cs' = 'test 2' }
            @(Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false).Count | Should -Be 0
        }

        It 'measures a release build from the previous release, ignoring previews' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'README.md' = 'readme 2' }
            # Every package changed between its 1.0.0 release and its 1.1.0.1 preview.
            Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $true | Should -Be @($script:Packages.packageName)
        }

        It 'uses the release when the latest preview is an ancestor of it' {
            Push-Location (Initialize-Repository)
            # A release built on top of the preview, as after a back-merge from master.
            Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core 3' } | Out-Null
            Add-TestTag -Name 'v1.1.0-DataStandardizer.Core'
            $commit = Add-TestCommit -File @{ 'README.md' = 'readme 2' }
            Get-PackageBaselineTag -PackageName 'DataStandardizer.Core' -Commit $commit -IsRelease $false | Should -Be 'v1.1.0-DataStandardizer.Core'
            Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false | Should -Not -Contain 'DataStandardizer.Core'
        }

        It 'uses a preview made after the release on another line of history' {
            Push-Location (Initialize-Repository)
            $base = (Invoke-Git rev-parse HEAD) | Select-Object -First 1
            Invoke-Git switch --quiet -c master | Out-Null
            Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'released' } | Out-Null
            Add-TestTag -Name 'v1.1.0-DataStandardizer.Core'
            Invoke-Git switch --quiet develop | Out-Null
            Add-TestCommit -File @{ 'src/DataStandardizer.Core/Other.cs' = 'later' } | Out-Null
            Add-TestTag -Name 'v1.2.0.1-DataStandardizer.Core'
            Invoke-Git merge --quiet --no-edit master | Out-Null
            $commit = (Invoke-Git rev-parse HEAD) | Select-Object -First 1
            $base | Should -Not -BeNullOrEmpty
            Get-PackageBaselineTag -PackageName 'DataStandardizer.Core' -Commit $commit -IsRelease $false | Should -Be 'v1.2.0.1-DataStandardizer.Core'
        }

        It 'treats a package with no tag as changed since the first commit' {
            Push-Location (Initialize-Repository)
            $packages = @($script:Packages) + @(New-TestPackageInfo -PackageName 'DataStandardizer.New')
            Add-TestCommit -File @{ 'src/DataStandardizer.New/New.cs' = 'new' } | Out-Null
            $commit = Add-TestCommit -File @{ 'README.md' = 'readme 2' }
            Get-ChangedPackageName -PackageInfo $packages -Commit $commit -IsRelease $false | Should -Be @('DataStandardizer.New')
        }

        It 'marks every package changed when a shared build file changes' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'Directory.Packages.props' = '<Project />' }
            Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false -SharedBuildFile 'Directory.Packages.props' |
                Should -Be @($script:Packages.packageName)
        }

        It 'ignores a build file that is not on the shared list' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'tests/Directory.Packages.props' = '<Project />' }
            @(Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false -SharedBuildFile 'Directory.Packages.props', 'src/Directory.Build.props').Count |
                Should -Be 0
        }

        It 'fails when the checkout has no version tags' {
            Push-Location (New-TestRepository)
            $commit = Add-TestCommit -File @{ 'src/DataStandardizer.Core/Core.cs' = 'core' }
            { Get-ChangedPackageName -PackageInfo $script:Packages -Commit $commit -IsRelease $false } | Should -Throw '*No version tags*'
        }
    }

    Context 'Get-DependentPackageName' {
        BeforeAll {
            $script:GraphRoot = New-TestFolder
            $project = { param($Path, [string[]]$References)
                $items = ($References | ForEach-Object { "<ProjectReference Include=`"$_`" />" }) -join ''
                $full = Join-Path $script:GraphRoot $Path
                [void](New-Item -ItemType Directory -Path (Split-Path $full -Parent) -Force)
                [System.IO.File]::WriteAllText($full, "<Project><ItemGroup>$items</ItemGroup></Project>")
            }
            & $project 'src/DataStandardizer.Core/DataStandardizer.Core.csproj' @()
            & $project 'src/DataStandardizer.File/DataStandardizer.File.csproj' @('..\DataStandardizer.Core\DataStandardizer.Core.csproj')
            & $project 'src/DataStandardizer.File.CSV/DataStandardizer.File.CSV.csproj' @('..\DataStandardizer.File\DataStandardizer.File.csproj')
            & $project 'tests/DataStandardizer.Core.Tests/DataStandardizer.Core.Tests.csproj' @('..\..\src\DataStandardizer.Core\DataStandardizer.Core.csproj')
        }

        It 'follows references transitively and excludes the changed packages' {
            Get-DependentPackageName -PackageInfo $script:Packages -ChangedPackageName 'DataStandardizer.Core' -RepositoryRoot $script:GraphRoot |
                Should -Be @('DataStandardizer.File', 'DataStandardizer.File.CSV')
        }

        It 'lists nothing for a package nothing depends on' {
            @(Get-DependentPackageName -PackageInfo $script:Packages -ChangedPackageName 'DataStandardizer.File.CSV' -RepositoryRoot $script:GraphRoot).Count | Should -Be 0
        }

        It 'lists nothing when nothing changed' {
            @(Get-DependentPackageName -PackageInfo $script:Packages -ChangedPackageName @() -RepositoryRoot $script:GraphRoot).Count | Should -Be 0
        }
    }

    Context 'Get-ChangedTestPackageName' {
        It 'lists the packages whose tests the commit changes, compared with its first parent' {
            Push-Location (Initialize-Repository)
            $commit = Add-TestCommit -File @{ 'tests/DataStandardizer.Core.Tests/T.cs' = 'test 2'; 'tests/DataStandardizer.File.Tests.Extra/X.cs' = 'x' }
            Get-ChangedTestPackageName -TestPackageName $script:Packages.packageName -Commit $commit | Should -Be @('DataStandardizer.Core')
        }

        It 'ignores test changes in earlier commits' {
            Push-Location (Initialize-Repository)
            Add-TestCommit -File @{ 'tests/DataStandardizer.Core.Tests/T.cs' = 'test 2' } | Out-Null
            $commit = Add-TestCommit -File @{ 'README.md' = 'readme 2' }
            @(Get-ChangedTestPackageName -TestPackageName $script:Packages.packageName -Commit $commit).Count | Should -Be 0
        }

        It 'lists nothing for a root commit' {
            Push-Location (New-TestRepository)
            $commit = Add-TestCommit -File @{ 'tests/DataStandardizer.Core.Tests/T.cs' = 'test' }
            @(Get-ChangedTestPackageName -TestPackageName $script:Packages.packageName -Commit $commit).Count | Should -Be 0
        }
    }

    Context 'Get-BuildPlan' {
        It 'builds changed, dependent and tests-only packages, and publishes only changed ones' {
            $plan = Get-BuildPlan -PackageInfo $script:Packages -ChangedPackageName 'DataStandardizer.File' -ChangedTestPackageName 'DataStandardizer.Core' `
                -DependentPackageName 'DataStandardizer.File.CSV' -Publish $true
            $plan.Build | Should -Be @('DataStandardizer.Core', 'DataStandardizer.File', 'DataStandardizer.File.CSV')
            $plan.Test | Should -Be @('DataStandardizer.Core', 'DataStandardizer.File', 'DataStandardizer.File.CSV')
            $plan.Publish | Should -Be @('DataStandardizer.File')
            $plan.TestsChanged | Should -Be @('DataStandardizer.Core')
        }

        It 'publishes nothing unless forced when the run does not publish' {
            $plan = Get-BuildPlan -PackageInfo $script:Packages -ChangedPackageName 'DataStandardizer.File' -Publish $false
            @($plan.Publish).Count | Should -Be 0
            $plan = Get-BuildPlan -PackageInfo $script:Packages -ChangedPackageName 'DataStandardizer.File' -ForcePublicationPackageName 'DataStandardizer.Core' -Publish $false
            $plan.Publish | Should -Be @('DataStandardizer.Core')
            $plan.Build | Should -Be @('DataStandardizer.Core', 'DataStandardizer.File')
        }

        It 'does not count a changed package as tests-only' {
            $plan = Get-BuildPlan -PackageInfo $script:Packages -ChangedPackageName 'DataStandardizer.Core' -ChangedTestPackageName 'DataStandardizer.Core' -Publish $true
            @($plan.TestsChanged).Count | Should -Be 0
            $plan.Publish | Should -Be @('DataStandardizer.Core')
        }

        It 'tests only packages with tests enabled' {
            $packages = @(New-TestPackageInfo -PackageName 'DataStandardizer.Core', 'DataStandardizer.File')
            $packages[1].enableTests = $false
            $plan = Get-BuildPlan -PackageInfo $packages -ChangedPackageName 'DataStandardizer.Core', 'DataStandardizer.File' -Publish $false
            $plan.Test | Should -Be @('DataStandardizer.Core')
        }
    }

    Context 'Get-TestMatrix' {
        It 'gives each package a leg per configuration on its own runners, or the default ones' {
            $packages = @(New-TestPackageInfo -PackageName 'DataStandardizer.Core', 'DataStandardizer.File', 'DataStandardizer.File.CSV')
            $packages[1] | Add-Member -NotePropertyName testRunners -NotePropertyValue @('windows-2025')
            $legs = @(Get-TestMatrix -PackageInfo $packages -TestPackageName 'DataStandardizer.File', 'DataStandardizer.File.CSV' -DefaultRunner 'ubuntu-24.04', 'windows-2025')
            $legs | ForEach-Object { "$($_.package)|$($_.configuration)|$($_.os)" } | Should -Be @(
                'DataStandardizer.File|Debug|windows-2025', 'DataStandardizer.File|Release|windows-2025'
                'DataStandardizer.File.CSV|Debug|ubuntu-24.04', 'DataStandardizer.File.CSV|Release|ubuntu-24.04'
                'DataStandardizer.File.CSV|Debug|windows-2025', 'DataStandardizer.File.CSV|Release|windows-2025')
        }

        It 'builds each package on its own runner, or the default one' {
            $packages = @(New-TestPackageInfo -PackageName 'DataStandardizer.Core', 'DataStandardizer.File')
            $packages[1] | Add-Member -NotePropertyName buildRunner -NotePropertyValue 'windows-2025'
            $legs = @(Get-BuildMatrix -PackageInfo $packages -BuildPackageName 'DataStandardizer.Core', 'DataStandardizer.File' -DefaultRunner 'ubuntu-24.04')
            $legs | ForEach-Object { "$($_.package)|$($_.configuration)|$($_.os)" } | Should -Be @(
                'DataStandardizer.Core|Debug|ubuntu-24.04', 'DataStandardizer.Core|Release|ubuntu-24.04'
                'DataStandardizer.File|Debug|windows-2025', 'DataStandardizer.File|Release|windows-2025')
        }

        It 'builds and tests every package on the default runners' {
            Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Force
            $manifest = Get-PackageManifest
            @($manifest.packages | Where-Object { $_.PSObject.Properties['buildRunner'] -or $_.PSObject.Properties['testRunners'] }) | Should -BeNullOrEmpty
            $manifest.defaultTestRunners | Should -Be @('ubuntu-24.04', 'windows-2025')
            $manifest.defaultBuildRunner | Should -Be 'ubuntu-24.04'
        }
    }

    Context 'ResolveBuildPlan.ps1' {
        It 'writes the package sets as JSON step outputs' {
            Push-Location (Initialize-Repository)
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            $env:GITHUB_SHA = Add-TestCommit -File @{ 'src/DataStandardizer.File/File.cs' = 'file 3'; 'tests/DataStandardizer.Core.Tests/T.cs' = 'test 2' }
            $env:GITHUB_REF = 'refs/heads/develop'
            $env:PACKAGEINFOS = ConvertTo-Json -InputObject $script:Packages -Compress
            & (Join-Path $script:ScriptsFolder 'ResolveBuildPlan.ps1') -Publish -ForcePublication 'File.CSV' 6>$null
            $outputs = @{}
            Get-Content $env:GITHUB_OUTPUT | ForEach-Object { $name, $value = $_ -split '=', 2; $outputs[$name] = $value }
            $outputs['changed'] | Should -Be '["DataStandardizer.File"]'
            $outputs['tests-changed'] | Should -Be '["DataStandardizer.Core"]'
            $outputs['publish-set'] | Should -Be '["DataStandardizer.File","DataStandardizer.File.CSV"]'
            $outputs['build-set'] | Should -Be '["DataStandardizer.Core","DataStandardizer.File","DataStandardizer.File.CSV"]'
            $outputs['has-build'] | Should -Be 'true'
            $outputs['has-publish'] | Should -Be 'true'
            $legs = @($outputs['test-matrix'] | ConvertFrom-Json)
            $legs.Count | Should -Be 12
            @($outputs['build-matrix'] | ConvertFrom-Json).Count | Should -Be 6
            ($legs | Where-Object package -EQ 'DataStandardizer.Core').os | Sort-Object -Unique | Should -Be @('ubuntu-24.04', 'windows-2025')
        }

        It 'uses the override instead of change detection, and writes empty arrays' {
            Push-Location (Initialize-Repository)
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            $env:GITHUB_SHA = Add-TestCommit -File @{ 'README.md' = 'readme 2' }
            $env:PACKAGEINFOS = ConvertTo-Json -InputObject $script:Packages -Compress
            & (Join-Path $script:ScriptsFolder 'ResolveBuildPlan.ps1') 6>$null
            $outputs = @{}
            Get-Content $env:GITHUB_OUTPUT | ForEach-Object { $name, $value = $_ -split '=', 2; $outputs[$name] = $value }
            $outputs['build-set'] | Should -Be '[]'
            $outputs['has-build'] | Should -Be 'false'

            Set-Content -LiteralPath $env:GITHUB_OUTPUT -Value $null
            & (Join-Path $script:ScriptsFolder 'ResolveBuildPlan.ps1') -OverrideChangedPackages 'core' 6>$null
            $outputs = @{}
            Get-Content $env:GITHUB_OUTPUT | Where-Object { $_ } | ForEach-Object { $name, $value = $_ -split '=', 2; $outputs[$name] = $value }
            $outputs['changed'] | Should -Be '["DataStandardizer.Core"]'
            $outputs['publish-set'] | Should -Be '[]'
        }
    }
}
