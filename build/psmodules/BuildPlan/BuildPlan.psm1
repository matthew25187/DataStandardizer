#Requires -Version 7.4

# Decides which packages a build covers.  Used by the Azure DevOps scripts FindChangedPackages.ps1, FindDependentPackages.ps1
# and FindTestPackages.ps1, and by ResolveBuildPlan.ps1 for the GitHub workflows.

function Test-PathInFolder {
    <#
    .SYNOPSIS
    True when $Path is $FolderPath or lies under it, comparing whole path segments (src/A.File doesn't contain src/A.File.CSV).
    #>
    [OutputType([bool])]
    param (
        [Parameter(Mandatory)]
        [string]    $FolderPath,

        [Parameter(Mandatory)]
        [AllowEmptyString()]
        [string]    $Path
    )

    if ([string]::IsNullOrEmpty($Path)) {
        return $false
    }

    $folderSegments = $FolderPath -split '[/\\]'
    $pathSegments = $Path -split '[/\\]'
    if ($folderSegments.Count -gt $pathSegments.Count) {
        return $false
    }

    for ($index = 0; $index -lt $folderSegments.Count; $index++) {
        if ($pathSegments[$index] -ne $folderSegments[$index]) {
            return $false
        }
    }

    return $true
}

function Get-PackageBaselineTag {
    <#
    .SYNOPSIS
    The tag a package's changes are measured from: its latest release tag for a release build; otherwise the most recent of
    its release and preview tags, where a preview counts only if the release isn't built on top of it.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $PackageName,

        [Parameter(Mandatory)]
        [string]    $Commit,

        [Parameter(Mandatory)]
        [bool]      $IsRelease
    )

    $tags = @(& git tag --list "v*$PackageName" --sort=-version:refname --merged $Commit)
    $escapedPackageName = [regex]::Escape($PackageName)
    $latestReleaseTag = $tags | Where-Object { $_ -match "^v\d+\.\d+\.\d+-$escapedPackageName$" } | Select-Object -First 1
    $latestPreviewTag = $tags | Where-Object { $_ -match "^v\d+\.\d+\.\d+\.\d+-$escapedPackageName$" } | Select-Object -First 1

    if ($IsRelease) {
        # Release builds compare against the previous release only.  Preview tags are ignored, so a package that has never
        # been released compares against the whole history and its first release includes everything.
        return $latestReleaseTag
    }

    # Other builds compare against the most recent build, which is a preview made since the last release when that preview
    # is not an ancestor of the release (develop includes releases through back-merges from master).
    $baselineTag = $latestReleaseTag ?? $latestPreviewTag
    if ($latestReleaseTag -and $latestPreviewTag) {
        & git merge-base --is-ancestor "tags/$latestPreviewTag" "tags/$latestReleaseTag"
        switch ($LASTEXITCODE) {
            0 { }
            1 { $baselineTag = $latestPreviewTag }
            default { throw "Failed to compare tags $latestPreviewTag and $latestReleaseTag." }
        }
    }

    return $baselineTag
}

function Get-ChangedPackageName {
    <#
    .SYNOPSIS
    The packages with a change under their source folder (or to a shared build file) since their baseline tag.
    #>
    [OutputType([string[]])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo,

        [Parameter(Mandatory)]
        [string]    $Commit,

        [Parameter(Mandatory)]
        [bool]      $IsRelease,

        # Whole repository-relative paths whose change marks every package as changed.
        [Parameter()]
        [string[]]  $SharedBuildFile = @()
    )

    # Without version tags every package would be compared against the first commit and reported as changed, so a checkout
    # that didn't fetch tags must fail here instead.
    if (-not (& git tag --list 'v*')) {
        throw 'No version tags found in the checkout.  The checkout must fetch tags.'
    }

    $changedPackages = @()
    foreach ($package in $PackageInfo) {
        Write-Verbose "Checking for changes made to package $($package.packageName)"

        $baselineTag = Get-PackageBaselineTag -PackageName $package.packageName -Commit $Commit -IsRelease $IsRelease
        if (-not [string]::IsNullOrWhiteSpace($baselineTag)) {
            Write-Debug "Getting file differences between tags/$baselineTag and $Commit."
            $changedFiles = @(& git diff --name-only "tags/$baselineTag" $Commit)
        }
        else {
            Write-Debug 'Found no baseline tag; using all differences instead.'
            $firstCommit = & git log --format='%H' --no-abbrev-commit $Commit | Select-Object -Last 1
            $changedFiles = @(& git diff --name-only $firstCommit $Commit)
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to get the files changed for $($package.packageName)."
        }

        $changedFile = $changedFiles | Where-Object {
            $path = $_.Trim()
            (Test-PathInFolder -FolderPath $package.packageSourcePath -Path ($path | Split-Path -Parent)) -or
            ($SharedBuildFile -contains $path)
        } | Select-Object -First 1
        if ($changedFile) {
            Write-Debug "File $changedFile marks package $($package.packageName) as changed."
            $changedPackages += $package.packageName
        }
    }

    return [string[]]$changedPackages
}

function Get-ProjectPackageName {
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo,

        [Parameter(Mandatory)]
        [string]    $ProjectFolderPath
    )

    # A production project maps to the package whose source path contains it; prefer the longest match.
    $packageName = $PackageInfo |
        Where-Object { Test-PathInFolder -FolderPath $_.packageSourcePath -Path $ProjectFolderPath } |
        Sort-Object -Property { ($_.packageSourcePath -split '[/\\]').Count } -Descending |
        Select-Object -First 1 -ExpandProperty packageName

    # A test project tests/<Package>.Tests maps to <Package>.
    if ([string]::IsNullOrEmpty($packageName)) {
        $projectFolders = $ProjectFolderPath -split '[/\\]'
        if ($projectFolders.Count -eq 2 -and $projectFolders[0] -eq 'tests' -and $projectFolders[1].EndsWith('.Tests', [System.StringComparison]::OrdinalIgnoreCase)) {
            $testedPackageName = $projectFolders[1].Substring(0, $projectFolders[1].Length - '.Tests'.Length)
            $packageName = $PackageInfo |
                Where-Object -Property packageName -EQ -Value $testedPackageName |
                Select-Object -First 1 -ExpandProperty packageName
        }
    }

    return $packageName
}

function Get-DependentPackageName {
    <#
    .SYNOPSIS
    The packages that depend, directly or through other packages, on any of the changed packages.  The changed packages
    themselves are excluded.  Project references of src/*/*.csproj and tests/*/*.csproj form the dependency graph.
    #>
    [OutputType([string[]])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $ChangedPackageName = @(),

        [Parameter(Mandatory)]
        [string]    $RepositoryRoot
    )

    $RepositoryRoot = [System.IO.Path]::GetFullPath($RepositoryRoot)
    $toRelativePath = { param($Path) [System.IO.Path]::GetRelativePath($RepositoryRoot, $Path) -replace '\\', '/' }

    # Build the reverse dependency graph (dependency -> dependents) from the project references.
    $dependentsByPackageName = @{}
    Get-ChildItem -Path (Join-Path $RepositoryRoot 'src' '*' '*.csproj'), (Join-Path $RepositoryRoot 'tests' '*' '*.csproj') -File |
        ForEach-Object {
            $projectFolderPath = & $toRelativePath $_.DirectoryName
            $dependentPackageName = Get-ProjectPackageName -PackageInfo $PackageInfo -ProjectFolderPath $projectFolderPath
            if ([string]::IsNullOrEmpty($dependentPackageName)) {
                Write-Debug "Project $projectFolderPath doesn't belong to a package; skipping."
                return
            }

            $projectXml = [xml](Get-Content -Path $_.FullName -Raw)
            $projectXml.SelectNodes('//*[local-name()="ProjectReference"]/@Include') |
                ForEach-Object {
                    $referencePath = [System.IO.Path]::GetFullPath((Join-Path $projectFolderPath.Replace('/', [System.IO.Path]::DirectorySeparatorChar) ($_.Value -replace '[/\\]', [System.IO.Path]::DirectorySeparatorChar)), $RepositoryRoot)
                    $referenceFolderPath = & $toRelativePath ([System.IO.Path]::GetDirectoryName($referencePath))
                    $dependencyPackageName = Get-ProjectPackageName -PackageInfo $PackageInfo -ProjectFolderPath $referenceFolderPath
                    if ([string]::IsNullOrEmpty($dependencyPackageName)) {
                        Write-Debug "Reference from $projectFolderPath to $referenceFolderPath doesn't belong to a package; skipping."
                    }
                    elseif ($dependencyPackageName -ne $dependentPackageName) {
                        Write-Debug "Package $dependentPackageName depends on package $dependencyPackageName."
                        if (-not $dependentsByPackageName.ContainsKey($dependencyPackageName)) {
                            $dependentsByPackageName[$dependencyPackageName] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
                        }
                        [void] $dependentsByPackageName[$dependencyPackageName].Add($dependentPackageName)
                    }
                }
        }

    # Walk the reverse graph transitively from each changed package.
    $reachedPackageNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    $pendingPackageNames = [System.Collections.Generic.Queue[string]]::new()
    $ChangedPackageName | Where-Object { $_ } | ForEach-Object {
        [void] $reachedPackageNames.Add($_)
        $pendingPackageNames.Enqueue($_)
    }
    while ($pendingPackageNames.Count -gt 0) {
        $packageName = $pendingPackageNames.Dequeue()
        if ($dependentsByPackageName.ContainsKey($packageName)) {
            $dependentsByPackageName[$packageName] |
                ForEach-Object {
                    if ($reachedPackageNames.Add($_)) {
                        Write-Debug "Package $_ depends on changed package $packageName, directly or indirectly."
                        $pendingPackageNames.Enqueue($_)
                    }
                }
        }
    }

    # Exclude the changed packages themselves, and list the rest in package order.
    return [string[]]@($PackageInfo |
        Select-Object -ExpandProperty packageName |
        Where-Object { $reachedPackageNames.Contains($_) -and $ChangedPackageName -notcontains $_ })
}

function Get-TestPackageName {
    <#
    .SYNOPSIS
    The packages that have tests to run.
    #>
    [OutputType([string[]])]
    param (
        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo
    )

    return [string[]]@($PackageInfo |
        Where-Object -Property enableTests -NE -Value 0 |
        Select-Object -ExpandProperty packageName)
}

function Get-ChangedTestPackageName {
    <#
    .SYNOPSIS
    The packages whose tests, tests/<Package>.Tests, the commit being built changes.

    .DESCRIPTION
    Their tests are built and run even when the package itself is unchanged, but a test change never makes a package changed,
    so it has no effect on versions or publication.  The changes are those since the first parent: for a PR, the PR's
    changes, from its merge commit; for develop, which takes squash merges and builds every push, the commit's own.  The
    package's version tag isn't used, as a test change never moves it.
    #>
    [OutputType([string[]])]
    param (
        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $TestPackageName = @(),

        [Parameter(Mandatory)]
        [string]    $Commit
    )

    & git rev-parse --verify --quiet "$Commit^1" | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Debug "$Commit has no parent; no tests are listed as changed."
        return [string[]]@()
    }

    $changedFiles = @(& git diff --name-only "$Commit^1" $Commit)
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to get the files changed by $Commit."
    }

    return [string[]]@($TestPackageName |
        Where-Object {
            $testFolderPath = "tests/$_.Tests/"
            $changedFiles | Where-Object { $_.StartsWith($testFolderPath, [System.StringComparison]::Ordinal) } | Select-Object -First 1
        })
}

function Get-BuildPlan {
    <#
    .SYNOPSIS
    Combines change detection, manual selections and the dependency graph into the package sets a run works on.

    .DESCRIPTION
    - Changed: changed since the baseline tag, or the override list when one is given.
    - TestsChanged: only their tests changed in this commit.
    - Dependent: depend on a changed package.
    - Publish: forced publication, plus the changed packages when the run publishes.  Never dependent or tests-only.
    - Build: publish, changed, dependent and tests-changed packages.
    - Test: the build packages that have tests.
    #>
    param (
        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [string[]]  $ChangedPackageName,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $ChangedTestPackageName = @(),

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $DependentPackageName = @(),

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $ForcePublicationPackageName = @(),

        [Parameter(Mandatory)]
        [bool]      $Publish
    )

    $inOrder = { param([string[]]$Names) [string[]]@($PackageInfo | Select-Object -ExpandProperty packageName | Where-Object { $Names -contains $_ }) }

    $publishSet = & $inOrder (@($ForcePublicationPackageName) + @($Publish ? $ChangedPackageName : @()))
    $testsChanged = & $inOrder @($ChangedTestPackageName | Where-Object { $ChangedPackageName -notcontains $_ })
    $buildSet = & $inOrder (@($publishSet) + @($ChangedPackageName) + @($DependentPackageName) + @($testsChanged))
    $testSet = & $inOrder @(Get-TestPackageName -PackageInfo $PackageInfo | Where-Object { $buildSet -contains $_ })

    return [pscustomobject]@{
        Changed      = & $inOrder $ChangedPackageName
        TestsChanged = $testsChanged
        Dependent    = & $inOrder $DependentPackageName
        Publish      = $publishSet
        Build        = $buildSet
        Test         = $testSet
    }
}

function Get-TestMatrix {
    <#
    .SYNOPSIS
    One test leg per package, configuration and runner, as a GitHub Actions matrix 'include' list.  A package's runners are
    its testRunners, or the default runners when it has none.
    #>
    param (
        [Parameter(Mandatory)]
        [pscustomobject[]]  $PackageInfo,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $TestPackageName = @(),

        [Parameter(Mandatory)]
        [string[]]  $DefaultRunner,

        [Parameter()]
        [string[]]  $Configuration = @('Debug', 'Release')
    )

    foreach ($package in $PackageInfo | Where-Object { $TestPackageName -contains $_.packageName }) {
        $runners = @($package.PSObject.Properties['testRunners'] ? $package.testRunners : $DefaultRunner)
        foreach ($runner in $runners) {
            foreach ($buildConfiguration in $Configuration) {
                [ordered]@{ package = $package.packageName; configuration = $buildConfiguration; os = $runner }
            }
        }
    }
}

Export-ModuleMember -Function Test-PathInFolder, Get-PackageBaselineTag, Get-ChangedPackageName, Get-DependentPackageName, `
    Get-TestPackageName, Get-ChangedTestPackageName, Get-BuildPlan, Get-TestMatrix
