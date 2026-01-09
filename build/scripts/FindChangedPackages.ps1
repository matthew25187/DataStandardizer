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

    $isMatch = $true
    for ($folderIndex = 0; $folderIndex -lt $packageSourcePathFolders.Count -and $isMatch; $folderIndex++) {
        if ($filePathFolders[$folderIndex] -ne $packageSourcePathFolders[$folderIndex]) {
            $isMatch = $false
        }
    }

    return $isMatch
}

if (0 -lt $TraceLevel) {
    Set-PSDebug -Trace $TraceLevel
}

# Get list of changed files since most recent build on current branch.
$changedPackages = @()
$env:PACKAGEINFOS |
    ConvertFrom-Json |
    ForEach-Object {
        Write-Verbose "Checking for changes made to package $($_.packageName)"

        [string] $diffFilesOutput
        $packageMatchPattern = "v*$($_.packageName)"
        $tagsOutput = & git tag --list $packageMatchPattern --sort=-version:refname --merged $env:BUILD_SOURCEVERSION
        # $latestTagName = & git for-each-ref refs/tags --sort=-taggerdate --format='%(refname:short)' --count=1 --points-at=HEAD
        # $latestTagName = & git describe --tags --abbrev=0 --match $packageMatchPattern $SourceCommitHash
        $latestReleaseTagName = $tagsOutput -split '\r?\n' | Select-String -Pattern "^v\d+\.\d+\.\d+-$($_.packageName)$" | Select-Object -First 1 -ExpandProperty Matches | Select-Object -First 1 -ExpandProperty Value
        $latestPreviewTagName = $tagsOutput -split '\r?\n' | Select-String -Pattern "^v\d+\.\d+\.\d+\.\d+-$($_.packageName)$" | Select-Object -First 1 -ExpandProperty Matches | Select-Object -First 1 -ExpandProperty Value
        $latestTagName = $latestReleaseTagName ?? $latestPreviewTagName
        if (-not [string]::IsNullOrWhiteSpace($latestTagName)) {
            Write-Debug "Found most recent project tag $latestTagName."

            $tagRefName = "tags/$latestTagName"
            Write-Debug "Getting file differences between $tagRefName and $env:BUILD_SOURCEVERSION."
            $diffFilesOutput = & git diff --name-only $tagRefName $env:BUILD_SOURCEVERSION
        }
        else {
            Write-Debug 'Failed to find standard differences; using all differences instead.'

            $firstCommitHash = & git log --format="%H" --no-abbrev-commit | tail -1
            Write-Debug "Getting file differences between $firstCommitHash and $env:BUILD_SOURCEVERSION."
            $diffFilesOutput = & git diff --name-only $firstCommitHash $env:BUILD_SOURCEVERSION
        }
        
        $changedFiles = (-not [string]::IsNullOrEmpty($diffFilesOutput))? $diffFilesOutput.Trim() -split '\r?\n':@()

        $isFileFromProject = $false
        for ($fileIndex = 0; $fileIndex -lt $changedFiles.Count -and (-not $isFileFromProject); $fileIndex++) {
            $changedFilePath = $changedFiles[$fileIndex]
            Write-Debug "Checking if file $changedFilePath is a member of project $($_.packageName)."

            $changedFileFolderPath = $changedFilePath | Split-Path -Parent
            $isFileFromProject = (-not [string]::IsNullOrEmpty($changedFileFolderPath))? (Get-FileMatchToPackage $_.packageSourcePath $changedFileFolderPath):$false
            if ($isFileFromProject) {
                Write-Debug "File $changedFilePath found in project; listing project $($_.packageName) as changed."
                $changedPackages += $_.packageName
            }
        }
    }

Write-Host "##vso[task.setvariable variable=changedPackageNamesList]$($changedPackages -join ',')"

if ($changedPackages.Count -gt 0) {
    Write-Information "Detected $($changedPackages.Count) changed package$(($changedPackages.Count -eq 1) ? '' : 's')."
}
else {
    Write-Information "No package changes were detected."
}

$changedPackages | ForEach-Object {
    Write-Verbose "-`t$($_)"
}