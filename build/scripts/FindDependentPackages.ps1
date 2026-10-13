#Requires -Version 7.0

param (
    [Parameter()]
    [int]   $TraceLevel = 0
)

function Get-FileMatchToPackage {
    [OutputType([bool])]
    param (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $PackageSourcePath,

        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $FilePath
    )

    $packageSourcePathFolders = $PackageSourcePath -split '[/\\]'
    $filePathFolders = $FilePath -split '[/\\]'

    $isMatch = $packageSourcePathFolders.Count -le $filePathFolders.Count
    for ($folderIndex = 0; $folderIndex -lt $packageSourcePathFolders.Count -and $isMatch; $folderIndex++) {
        if ($filePathFolders[$folderIndex] -ne $packageSourcePathFolders[$folderIndex]) {
            $isMatch = $false
        }
    }

    return $isMatch
}

function Get-ProjectPackageName {
    [OutputType([string])]
    param (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $ProjectFolderPath
    )

    # A production project maps to the package whose source path contains it; prefer the longest match.
    $packageName = $packageInfos |
        Where-Object { Get-FileMatchToPackage $_.packageSourcePath $ProjectFolderPath } |
        Sort-Object -Property { ($_.packageSourcePath -split '[/\\]').Count } -Descending |
        Select-Object -First 1 -ExpandProperty packageName

    # A test project tests/<Package>.Tests maps to <Package>.
    if ([string]::IsNullOrEmpty($packageName)) {
        $projectFolders = $ProjectFolderPath -split '[/\\]'
        if ($projectFolders.Count -eq 2 -and $projectFolders[0] -eq 'tests' -and $projectFolders[1].EndsWith('.Tests', [System.StringComparison]::OrdinalIgnoreCase)) {
            $testedPackageName = $projectFolders[1].Substring(0, $projectFolders[1].Length - '.Tests'.Length)
            $packageName = $packageInfos |
                Where-Object -Property packageName -EQ -Value $testedPackageName |
                Select-Object -First 1 -ExpandProperty packageName
        }
    }

    return $packageName
}

function Get-RepositoryRelativePath {
    [OutputType([string])]
    param (
        [Parameter()]
        [ValidateNotNullOrEmpty()]
        [string] $Path
    )

    return [System.IO.Path]::GetRelativePath($repositoryRootPath, $Path) -replace '\\', '/'
}

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

$repositoryRootPath = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..' '..'))
$packageInfos = @($env:PACKAGEINFOS | ConvertFrom-Json)
$changedPackageNames = @(($env:CHANGEDPACKAGENAMESLIST ?? '') -split ',' | ForEach-Object { $_.Trim() } | Where-Object { -not [string]::IsNullOrEmpty($_) })

# Build the reverse dependency graph (dependency -> dependents) from the project references.
$dependentsByPackageName = @{}
Get-ChildItem -Path (Join-Path $repositoryRootPath 'src' '*' '*.csproj'), (Join-Path $repositoryRootPath 'tests' '*' '*.csproj') -File |
    ForEach-Object {
        $projectFolderPath = Get-RepositoryRelativePath $_.DirectoryName
        $dependentPackageName = Get-ProjectPackageName $projectFolderPath
        if ([string]::IsNullOrEmpty($dependentPackageName)) {
            Write-Debug "Project $projectFolderPath doesn't belong to a package; skipping."
            return
        }

        $projectXml = [xml](Get-Content -Path $_.FullName -Raw)
        $projectXml.SelectNodes('//*[local-name()="ProjectReference"]/@Include') |
            ForEach-Object {
                $referencePath = [System.IO.Path]::GetFullPath((Join-Path $projectFolderPath.Replace('/', [System.IO.Path]::DirectorySeparatorChar) ($_.Value -replace '[/\\]', [System.IO.Path]::DirectorySeparatorChar)), $repositoryRootPath)
                $referenceFolderPath = Get-RepositoryRelativePath ([System.IO.Path]::GetDirectoryName($referencePath))
                $dependencyPackageName = Get-ProjectPackageName $referenceFolderPath
                if ([string]::IsNullOrEmpty($dependencyPackageName)) {
                    Write-Debug "Reference from $projectFolderPath to $referenceFolderPath doesn't belong to a package; skipping."
                }
                elseif ($dependencyPackageName -ne $dependentPackageName) {
                    Write-Debug "Package $dependentPackageName depends on package $dependencyPackageName."
                    if (-not $dependentsByPackageName.ContainsKey($dependencyPackageName)) {
                        $dependentsByPackageName[$dependencyPackageName] = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
                    }
                    $null = $dependentsByPackageName[$dependencyPackageName].Add($dependentPackageName)
                }
            }
    }

# Walk the reverse graph transitively from each changed package.
$reachedPackageNames = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$pendingPackageNames = [System.Collections.Generic.Queue[string]]::new()
$changedPackageNames | ForEach-Object {
    $null = $reachedPackageNames.Add($_)
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
$dependentPackages = @($packageInfos |
    Select-Object -ExpandProperty packageName |
    Where-Object { $reachedPackageNames.Contains($_) -and $changedPackageNames -notcontains $_ })

Write-Host "##vso[task.setvariable variable=dependentPackageNames;isOutput=true]$($dependentPackages -join ',')"

if ($dependentPackages.Count -gt 0) {
    Write-Information "Detected $($dependentPackages.Count) dependent package$(($dependentPackages.Count -eq 1) ? '' : 's')."
}
else {
    Write-Information "No dependent packages were detected."
}

$dependentPackages | ForEach-Object {
    Write-Verbose "-`t$($_)"
}
