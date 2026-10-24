#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.5.0' }

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../psmodules/CommonHelpers/CommonHelpers.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot 'TestHelpers.psm1') -Force

    $script:CiVariables = 'TF_BUILD', 'GITHUB_ACTIONS', 'GITHUB_OUTPUT', 'GITHUB_ENV', 'BUILD_SOURCEVERSION', 'BUILD_SOURCEBRANCH', 'GITHUB_SHA', 'GITHUB_REF', 'DS_RELEASE_BUILD', 'PACKAGEINFOS'
    $script:SavedVariables = @{}
    foreach ($name in $script:CiVariables) {
        $script:SavedVariables[$name] = [System.Environment]::GetEnvironmentVariable($name)
    }
}

AfterAll {
    foreach ($name in $script:CiVariables) {
        [System.Environment]::SetEnvironmentVariable($name, $script:SavedVariables[$name])
    }
}

Describe 'CommonHelpers' {
    BeforeEach {
        foreach ($name in $script:CiVariables) {
            [System.Environment]::SetEnvironmentVariable($name, $null)
        }
    }

    Context 'Get-PackageInfo' {
        It 'reads build/packages.json' {
            $packages = @(Get-PackageInfo)
            $packages.Count | Should -Be 16
            $packages[0].packageName | Should -Be 'DataStandardizer.Core'
            $packages | Where-Object packageName -EQ 'DataStandardizer.File.iCal' | Select-Object -ExpandProperty variableSuffix | Should -Be 'FILE_ICAL'
        }

        It 'gives every package a distinct variable suffix that is a valid variable-name fragment' {
            $suffixes = @(Get-PackageInfo | Select-Object -ExpandProperty variableSuffix)
            ($suffixes | Sort-Object -Unique).Count | Should -Be $suffixes.Count
            $suffixes | ForEach-Object { $_ | Should -Match '^[A-Z0-9_]+$' }
        }

        It 'matches the Azure DevOps package list' {
            $pipelineVars = Get-Content (Join-Path (Get-RepositoryRoot) 'build' 'build-pipeline-vars.yml') -Raw
            $pipelineNames = [regex]::Matches($pipelineVars, '(?m)^  - packageName: (\S+)') | ForEach-Object { $_.Groups[1].Value }
            @(Get-PackageInfo | Select-Object -ExpandProperty packageName) | Should -Be @($pipelineNames)
        }

        It 'prefers PACKAGEINFOS when set' {
            $env:PACKAGEINFOS = '[{"packageName":"A","packageSourcePath":"src/A","enableTests":1}]'
            @(Get-PackageInfo).packageName | Should -Be 'A'
        }
    }

    Context 'Resolve-PackageName' {
        BeforeAll {
            $script:Packages = @(New-TestPackageInfo -PackageName 'DataStandardizer.Core', 'DataStandardizer.File', 'DataStandardizer.File.CSV')
        }

        It 'accepts full IDs and short names, case-insensitively, separated by commas or spaces' {
            Resolve-PackageName -Name 'file.csv, DataStandardizer.Core file' -PackageInfo $script:Packages |
                Should -Be @('DataStandardizer.Core', 'DataStandardizer.File', 'DataStandardizer.File.CSV')
        }

        It 'returns package-list order without duplicates' {
            Resolve-PackageName -Name 'File.CSV,Core,Core' -PackageInfo $script:Packages | Should -Be @('DataStandardizer.Core', 'DataStandardizer.File.CSV')
        }

        It 'selects every package for *' {
            @(Resolve-PackageName -Name '*' -PackageInfo $script:Packages).Count | Should -Be 3
        }

        It 'returns nothing for an empty list' {
            @(Resolve-PackageName -Name '' -PackageInfo $script:Packages).Count | Should -Be 0
            @(Resolve-PackageName -Name '  ' -PackageInfo $script:Packages).Count | Should -Be 0
        }

        It 'rejects an unknown name and lists the valid ones' {
            { Resolve-PackageName -Name 'Core, Moneys' -PackageInfo $script:Packages } | Should -Throw "*Unknown package 'Moneys'*File.CSV*"
        }
    }

    Context 'Build commit and release detection' {
        It 'prefers BUILD_SOURCEVERSION to GITHUB_SHA' {
            $env:GITHUB_SHA = 'github'
            Get-BuildCommit | Should -Be 'github'
            $env:BUILD_SOURCEVERSION = 'azure'
            Get-BuildCommit | Should -Be 'azure'
        }

        It 'fails when no commit is known' {
            { Get-BuildCommit } | Should -Throw
        }

        It 'treats master as a release build' {
            $env:GITHUB_REF = 'refs/heads/master'
            Test-ReleaseBuild | Should -BeTrue
            $env:GITHUB_REF = 'refs/heads/develop'
            Test-ReleaseBuild | Should -BeFalse
            $env:BUILD_SOURCEBRANCH = 'refs/heads/master'
            Test-ReleaseBuild | Should -BeTrue
        }

        It 'lets DS_RELEASE_BUILD override the branch' {
            $env:GITHUB_REF = 'refs/heads/master'
            $env:DS_RELEASE_BUILD = 'false'
            Test-ReleaseBuild | Should -BeFalse
            $env:GITHUB_REF = 'refs/pull/1/merge'
            $env:DS_RELEASE_BUILD = 'true'
            Test-ReleaseBuild | Should -BeTrue
        }
    }

    Context 'Set-CiOutput' {
        It 'writes the Azure Pipelines command under TF_BUILD' {
            $env:TF_BUILD = 'True'
            $output = Set-CiOutput -Name 'testPackageNames' -Value 'A,B' -IsOutput 6>&1
            "$output" | Should -BeExactly '##vso[task.setvariable variable=testPackageNames;isOutput=true]A,B'
            $output = Set-CiOutput -Name 'changedPackageNamesList' -Value '' 6>&1
            "$output" | Should -BeExactly '##vso[task.setvariable variable=changedPackageNamesList]'
        }

        It 'appends to GITHUB_OUTPUT under GitHub Actions' {
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            Set-CiOutput -Name 'build-set' -Value '["A"]'
            Set-CiOutput -Name 'empty' -Value ''
            Get-Content $env:GITHUB_OUTPUT | Should -Be @('build-set=["A"]', 'empty=')
        }

        It 'uses a delimiter for a multi-line value' {
            $env:GITHUB_OUTPUT = Join-Path (New-TestFolder) 'output'
            Set-CiOutput -Name 'notes' -Value "one`ntwo"
            $lines = @(Get-Content $env:GITHUB_OUTPUT)
            $lines[0] | Should -Match '^notes<<ghadelimiter_[0-9a-f]{32}$'
            $lines[1..2] | Should -Be @('one', 'two')
            $lines[3] | Should -Be ($lines[0] -replace '^notes<<', '')
        }
    }

    Context 'Write-CiIssue' {
        It 'escapes a GitHub annotation' {
            $env:GITHUB_ACTIONS = 'true'
            "$(Write-CiIssue -Type error -Message "50% done`nnext" 6>&1)" | Should -BeExactly '::error::50%25 done%0Anext'
        }
    }
}
