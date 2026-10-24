#Requires -Version 7.4

# Downloads packages from NuGet v3 feeds, reads the versions stamped on a package's assemblies, and decides which preview
# versions may be pruned from the package store.

Import-Module (Join-Path $PSScriptRoot '../VersionNumbers/VersionNumbers.psm1') -Scope Local

function Get-NuGetBasicAuthHeader {
    [OutputType([hashtable])]
    param (
        [Parameter()]
        [AllowEmptyString()]
        [AllowNull()]
        [string]    $Token
    )

    if ([string]::IsNullOrEmpty($Token)) {
        return @{}
    }

    # GitHub Packages accepts any user name with a token as the password.
    $credential = [System.Convert]::ToBase64String([System.Text.Encoding]::UTF8.GetBytes("token:$Token"))
    return @{ Authorization = "Basic $credential" }
}

function Get-NuGetPackageBaseAddress {
    <#
    .SYNOPSIS
    The flat-container (PackageBaseAddress/3.0.0) address of a NuGet v3 feed, from its service index.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $ServiceIndexUrl,

        [Parameter()]
        [hashtable] $Headers = @{}
    )

    $index = Invoke-RestMethod -Uri $ServiceIndexUrl -Headers $Headers -ErrorAction Stop
    $resource = $index.resources | Where-Object { $_.'@type' -like 'PackageBaseAddress/3.0.0*' } | Select-Object -First 1
    if ($null -eq $resource) {
        throw "Feed $ServiceIndexUrl has no PackageBaseAddress resource."
    }

    return $resource.'@id'.TrimEnd('/') + '/'
}

function Save-NuGetPackage {
    <#
    .SYNOPSIS
    Downloads one package version, trying each feed in turn, and returns the path of the .nupkg, or $null if no feed has it.

    .PARAMETER Source
    Feeds to try, in order: hashtables with ServiceIndexUrl and, for an authenticated feed, Token.
    #>
    [OutputType([string])]
    param (
        [Parameter(Mandatory)]
        [string]    $PackageId,

        [Parameter(Mandatory)]
        [string]    $Version,

        [Parameter(Mandatory)]
        [string]    $DestinationFolder,

        [Parameter(Mandatory)]
        [hashtable[]]   $Source
    )

    $null = New-Item -ItemType Directory -Path $DestinationFolder -Force
    $lowerId = $PackageId.ToLowerInvariant()
    $lowerVersion = $Version.ToLowerInvariant()
    $destination = Join-Path $DestinationFolder "$lowerId.$lowerVersion.nupkg"

    foreach ($feed in $Source) {
        $headers = Get-NuGetBasicAuthHeader -Token $feed.Token
        try {
            $baseAddress = Get-NuGetPackageBaseAddress -ServiceIndexUrl $feed.ServiceIndexUrl -Headers $headers
            $uri = "$baseAddress$lowerId/$lowerVersion/$lowerId.$lowerVersion.nupkg"
            Invoke-WebRequest -Uri $uri -Headers $headers -OutFile $destination -ErrorAction Stop | Out-Null
            Write-Verbose "Downloaded $PackageId $Version from $($feed.ServiceIndexUrl)."
            return $destination
        }
        catch {
            Write-Verbose "$PackageId $Version not downloaded from $($feed.ServiceIndexUrl): $($_.Exception.Message)"
        }
    }

    return $null
}

function Get-AssemblyAttributeValue {
    <#
    .SYNOPSIS
    The string argument of each assembly-level attribute whose type is in $AttributeName, read from the assembly metadata.
    Works the same on every platform, unlike FileVersionInfo.
    #>
    [OutputType([hashtable])]
    param (
        [Parameter(Mandatory)]
        [string]    $AssemblyPath,

        [Parameter(Mandatory)]
        [string[]]  $AttributeName
    )

    $values = @{}
    $stream = [System.IO.File]::OpenRead($AssemblyPath)
    try {
        $peReader = [System.Reflection.PortableExecutable.PEReader]::new($stream)
        try {
            $reader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($peReader)
            foreach ($handle in $reader.GetAssemblyDefinition().GetCustomAttributes()) {
                $attribute = $reader.GetCustomAttribute($handle)
                $typeName = $null
                if ($attribute.Constructor.Kind -eq [System.Reflection.Metadata.HandleKind]::MemberReference) {
                    $member = $reader.GetMemberReference([System.Reflection.Metadata.MemberReferenceHandle]$attribute.Constructor)
                    if ($member.Parent.Kind -eq [System.Reflection.Metadata.HandleKind]::TypeReference) {
                        $typeName = $reader.GetString($reader.GetTypeReference([System.Reflection.Metadata.TypeReferenceHandle]$member.Parent).Name)
                    }
                }
                elseif ($attribute.Constructor.Kind -eq [System.Reflection.Metadata.HandleKind]::MethodDefinition) {
                    $method = $reader.GetMethodDefinition([System.Reflection.Metadata.MethodDefinitionHandle]$attribute.Constructor)
                    $typeName = $reader.GetString($reader.GetTypeDefinition($method.GetDeclaringType()).Name)
                }

                if ($AttributeName -notcontains $typeName) {
                    continue
                }

                $blob = $reader.GetBlobReader($attribute.Value)
                if ($blob.ReadUInt16() -eq 1) {
                    $values[$typeName] = $blob.ReadSerializedString()
                }
            }
        }
        finally {
            $peReader.Dispose()
        }
    }
    finally {
        $stream.Dispose()
    }

    return $values
}

function Get-PackageAssemblyVersion {
    <#
    .SYNOPSIS
    The assembly, file and informational versions of a package's main assembly, <PackageId>.dll, from its first target
    framework.  Warns when target frameworks disagree, or when the assembly's major.minor differs from the package's.
    #>
    param (
        [Parameter(Mandatory)]
        [string]    $PackagePath,

        [Parameter(Mandatory)]
        [string]    $PackageId,

        [Parameter()]
        [version]   $PackageVersion
    )

    $extractFolder = Join-Path ([System.IO.Path]::GetTempPath()) "nupkg-$([guid]::NewGuid().ToString('N'))"
    try {
        Expand-Archive -LiteralPath $PackagePath -DestinationPath $extractFolder -Force
        $assemblyPaths = @(Get-ChildItem -Path $extractFolder -Filter "$PackageId.dll" -Recurse -File |
            Sort-Object -Property FullName |
            Select-Object -ExpandProperty FullName)
        if ($assemblyPaths.Count -eq 0) {
            throw "Package $PackageId has no $PackageId.dll.  Unable to determine its assembly versions."
        }

        $result = $null
        foreach ($assemblyPath in $assemblyPaths) {
            $assemblyVersion = [System.Reflection.AssemblyName]::GetAssemblyName($assemblyPath).Version
            if ($null -eq $result) {
                $attributes = Get-AssemblyAttributeValue -AssemblyPath $assemblyPath -AttributeName 'AssemblyFileVersionAttribute', 'AssemblyInformationalVersionAttribute'
                $result = [pscustomobject]@{
                    AssemblyVersion      = $assemblyVersion.ToString()
                    FileVersion          = $attributes['AssemblyFileVersionAttribute']
                    InformationalVersion = $attributes['AssemblyInformationalVersionAttribute']
                }
            }
            elseif ($assemblyVersion.ToString() -ne $result.AssemblyVersion) {
                Write-Warning "Detected multiple assembly versions in package $PackageId.  Package will not resolve correctly if referenced."
            }
        }

        if ($null -ne $PackageVersion) {
            [version]$first = $result.AssemblyVersion
            if ($first.Major -ne $PackageVersion.Major -or $first.Minor -ne $PackageVersion.Minor) {
                Write-Warning "$PackageId package/assembly version mismatch; $PackageVersion != $first."
            }
        }

        return $result
    }
    finally {
        Remove-Item -LiteralPath $extractFolder -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Get-PrunablePreviewVersion {
    <#
    .SYNOPSIS
    Decides which stored versions of a package may be deleted.  Pure: the caller supplies everything it needs.

    .DESCRIPTION
    A preview (M.m.p-<label>.r) may be deleted when release M.m.p of the package exists, or when it is older than
    MaxAgeDays.  It is kept regardless when it was promoted (a published GitHub Release exists on its tag), when it is on
    nuget.org, or when it is the package's current version.  Releases (M.m.p) and versions in any other form are never
    deleted.

    .PARAMETER StoredVersion
    The versions in the store: objects with Id, Name (the package version) and CreatedAt.
    #>
    param (
        [Parameter(Mandatory)]
        [string]    $PackageName,

        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [object[]]  $StoredVersion,

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $ReleaseTag = @(),

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $PromotedTag = @(),

        [Parameter()]
        [AllowEmptyCollection()]
        [string[]]  $NuGetOrgVersion = @(),

        [Parameter()]
        [AllowEmptyString()]
        [AllowNull()]
        [string]    $CurrentVersion,

        [Parameter()]
        [int]       $MaxAgeDays = 0,

        [Parameter()]
        [datetime]  $UtcNow = [datetime]::UtcNow
    )

    $storedReleases = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($stored in $StoredVersion) {
        $parsed = ConvertFrom-SemanticVersion -SemanticVersion $stored.Name
        if ($null -ne $parsed -and -not $parsed.IsPrerelease) {
            $null = $storedReleases.Add($stored.Name)
        }
    }

    foreach ($stored in $StoredVersion) {
        $parsed = ConvertFrom-SemanticVersion -SemanticVersion $stored.Name
        if ($null -eq $parsed -or -not $parsed.IsPrerelease) {
            continue
        }

        $core = $parsed.Version.Major, $parsed.Version.Minor, $parsed.Version.Build -join '.'
        $tag = ConvertTo-PackageTagName -Version $parsed.Version -PackageName $PackageName
        $reason = $null
        if ($storedReleases.Contains($core) -or $ReleaseTag -contains "v$core-$PackageName") {
            $reason = "release $core exists"
        }
        elseif ($MaxAgeDays -gt 0 -and ($UtcNow - [datetime]$stored.CreatedAt).TotalDays -gt $MaxAgeDays) {
            $reason = "older than $MaxAgeDays days"
        }
        if ($null -eq $reason) {
            continue
        }

        if ($PromotedTag -contains $tag) {
            Write-Verbose "Keeping $PackageName $($stored.Name): promoted."
            continue
        }
        if ($NuGetOrgVersion -contains $stored.Name) {
            Write-Verbose "Keeping $PackageName $($stored.Name): on nuget.org."
            continue
        }
        if ($CurrentVersion -eq $stored.Name) {
            Write-Verbose "Keeping $PackageName $($stored.Name): current version."
            continue
        }

        [pscustomobject]@{ PackageName = $PackageName; Id = $stored.Id; Name = $stored.Name; Tag = $tag; Reason = $reason }
    }
}

Export-ModuleMember -Function Save-NuGetPackage, Get-AssemblyAttributeValue, Get-PackageAssemblyVersion, Get-PrunablePreviewVersion
