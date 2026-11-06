#Requires -Modules @{ ModuleName = 'Pester'; ModuleVersion = '5.5.0' }

BeforeAll {
    Import-Module (Join-Path $PSScriptRoot '../psmodules/PackageRegistry/PackageRegistry.psm1') -Force
    Import-Module (Join-Path $PSScriptRoot 'TestHelpers.psm1') -Force

    # A real assembly with file and informational version attributes, present wherever PowerShell runs.
    $script:Assembly = [System.Management.Automation.PSObject].Assembly
    $script:AssemblyPath = $script:Assembly.Location
    $script:AssemblyName = [System.IO.Path]::GetFileNameWithoutExtension($script:AssemblyPath)
}

Describe 'PackageRegistry' {
    Context 'Get-AssemblyAttributeValue' {
        It 'reads the file and informational versions from metadata' {
            $values = Get-AssemblyAttributeValue -AssemblyPath $script:AssemblyPath -AttributeName 'AssemblyFileVersionAttribute', 'AssemblyInformationalVersionAttribute'
            $values['AssemblyFileVersionAttribute'] | Should -Be ([System.Reflection.CustomAttributeExtensions]::GetCustomAttribute[System.Reflection.AssemblyFileVersionAttribute]($script:Assembly)).Version
            $values['AssemblyInformationalVersionAttribute'] | Should -Be ([System.Reflection.CustomAttributeExtensions]::GetCustomAttribute[System.Reflection.AssemblyInformationalVersionAttribute]($script:Assembly)).InformationalVersion
        }
    }

    Context 'Get-PackageAssemblyVersion' {
        It 'reads the versions of the package''s main assembly' {
            $folder = New-TestFolder
            $package = New-TestNuGetPackage -Folder $folder -Id $script:AssemblyName -Version '1.0.0' -ExtraFile @{
                "lib/netstandard2.0/$script:AssemblyName.dll" = $script:AssemblyPath
                "lib/net10.0/$script:AssemblyName.dll"        = $script:AssemblyPath
            }
            $versions = Get-PackageAssemblyVersion -PackagePath $package -PackageId $script:AssemblyName 3>$null
            $versions.AssemblyVersion | Should -Be $script:Assembly.GetName().Version.ToString()
            $versions.FileVersion | Should -Not -BeNullOrEmpty
        }

        It 'fails for a package without its assembly' {
            $package = New-TestNuGetPackage -Folder (New-TestFolder) -Id 'Empty.Package' -Version '1.0.0'
            { Get-PackageAssemblyVersion -PackagePath $package -PackageId 'Empty.Package' } | Should -Throw '*no Empty.Package.dll*'
        }
    }

    Context 'Save-NuGetPackage' {
        It 'tries each feed in turn, sending the token only where given' {
            Mock -ModuleName PackageRegistry Invoke-RestMethod {
                [pscustomobject]@{ resources = @([pscustomobject]@{ '@type' = 'PackageBaseAddress/3.0.0'; '@id' = "$($Uri -replace 'index\.json$', '')flat/" }) }
            }
            Mock -ModuleName PackageRegistry Invoke-WebRequest {
                if ($Uri -like 'https://first.example/*') { throw 'Not found' }
                Set-Content -LiteralPath $OutFile -Value 'package'
            }
            $path = Save-NuGetPackage -PackageId 'DataStandardizer.Core' -Version '1.4.0-Preview.3' -DestinationFolder (New-TestFolder) -Source @(
                @{ ServiceIndexUrl = 'https://first.example/index.json'; Token = 'secret' }
                @{ ServiceIndexUrl = 'https://second.example/index.json' }
            )
            $path | Should -BeLike '*datastandardizer.core.1.4.0-preview.3.nupkg'
            Should -Invoke -ModuleName PackageRegistry Invoke-WebRequest -ParameterFilter { $Uri -eq 'https://second.example/flat/datastandardizer.core/1.4.0-preview.3/datastandardizer.core.1.4.0-preview.3.nupkg' -and -not $Headers.ContainsKey('Authorization') } -Times 1
            Should -Invoke -ModuleName PackageRegistry Invoke-WebRequest -ParameterFilter { $Uri -like 'https://first.example/*' -and $Headers['Authorization'] -like 'Basic *' } -Times 1
        }

        It 'returns nothing when no feed has the package' {
            Mock -ModuleName PackageRegistry Invoke-RestMethod { throw 'Unavailable' }
            Save-NuGetPackage -PackageId 'P' -Version '1.0.0' -DestinationFolder (New-TestFolder) -Source @(@{ ServiceIndexUrl = 'https://x.example/index.json' }) |
                Should -BeNullOrEmpty
        }
    }

    Context 'Save-BuildProvenance' {
        BeforeEach {
            $script:PackagePath = Join-Path (New-TestFolder) 'P.1.0.0.nupkg'
        }

        It 'saves the verified bundle beside the package' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 0; '{"mediaType":"application/vnd.dev.sigstore.bundle.v0.3+json"}' }
            $path = Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo'
            $path | Should -Be "$script:PackagePath.sigstore.json"
            (Get-Content -LiteralPath $path -Raw).Trim() | Should -Be '{"mediaType":"application/vnd.dev.sigstore.bundle.v0.3+json"}'
        }

        It 'requires SLSA provenance signed by the build workflow on a hosted runner' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 0; '{}' }
            Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo' | Out-Null
            Should -Invoke -ModuleName PackageRegistry gh -Times 1 -ParameterFilter {
                "$args" -like '*--repo owner/repo *' -and
                "$args" -like '*--signer-workflow owner/repo/.github/workflows/build-and-publish.yml *' -and
                "$args" -like '*--predicate-type https://slsa.dev/provenance/v1 *' -and
                "$args" -like '*--deny-self-hosted-runners *'
            }
        }

        It 'throws for a package with no attestation' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 1; 'Error: HTTP 404: Not Found' }
            { Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo' } | Should -Throw '*P.1.0.0.nupkg did not verify*'
        }

        It 'returns nothing for a package with no attestation when that is allowed' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 1; 'Error: HTTP 404: Not Found' }
            Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo' -AllowMissing | Should -BeNullOrEmpty
            Test-Path -LiteralPath "$script:PackagePath.sigstore.json" | Should -BeFalse
        }

        It 'returns nothing for a package with attestations but no provenance when that is allowed' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 1; 'Error: no attestations found' }
            Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo' -AllowMissing | Should -BeNullOrEmpty
        }

        It 'leaves no failed exit code behind for a missing attestation that is allowed' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 1; 'Error: HTTP 404: Not Found' }
            Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo' -AllowMissing | Out-Null
            $global:LASTEXITCODE | Should -Be 0
        }

        It 'throws for an attestation that fails to verify, even when a missing one is allowed' {
            Mock -ModuleName PackageRegistry gh { $global:LASTEXITCODE = 1; 'Error: verifying with issuer "sigstore.dev": certificate identity mismatch' }
            { Save-BuildProvenance -PackagePath $script:PackagePath -Repository 'owner/repo' -AllowMissing } | Should -Throw '*certificate identity mismatch*'
        }
    }

    Context 'Get-PrunablePreviewVersion' {
        BeforeAll {
            $script:Now = [datetime]::new(2026, 10, 24, 0, 0, 0, [System.DateTimeKind]::Utc)
            $script:Recent = $script:Now.AddDays(-10)
            function New-Stored { param($Name, $CreatedAt = $script:Recent) [pscustomobject]@{ Id = [math]::Abs($Name.GetHashCode()); Name = $Name; CreatedAt = $CreatedAt } }
        }

        It 'deletes previews whose release exists' {
            $stored = @((New-Stored '1.4.0-preview.1'), (New-Stored '1.4.0-preview.2'), (New-Stored '1.4.0'), (New-Stored '1.5.0-preview.1'))
            $prunable = @(Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion $stored -UtcNow $script:Now)
            $prunable.Name | Should -Be @('1.4.0-preview.1', '1.4.0-preview.2')
            $prunable[0].Tag | Should -Be 'v1.4.0.1-P'
            $prunable[0].Reason | Should -Be 'release 1.4.0 exists'
        }

        It 'counts a release tag as the release existing' {
            $prunable = @(Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion @(New-Stored '1.4.0-preview.1') -ReleaseTag 'v1.4.0-P', 'v1.4.0-Q' -UtcNow $script:Now)
            $prunable.Name | Should -Be '1.4.0-preview.1'
            @(Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion @(New-Stored '1.4.0-preview.1') -ReleaseTag 'v1.4.0-Q' -UtcNow $script:Now).Count | Should -Be 0
        }

        It 'keeps promoted previews, previews on nuget.org and the current version' {
            $stored = @((New-Stored '1.4.0-preview.1'), (New-Stored '1.4.0-preview.2'), (New-Stored '1.4.0-preview.3'), (New-Stored '1.4.0-preview.4'), (New-Stored '1.4.0'))
            $prunable = @(Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion $stored -PromotedTag 'v1.4.0.1-P' -NuGetOrgVersion '1.4.0-PREVIEW.2' -CurrentVersion '1.4.0-preview.3' -UtcNow $script:Now)
            $prunable.Name | Should -Be '1.4.0-preview.4'
        }

        It 'deletes previews past the age limit only when one is set' {
            $stored = @((New-Stored '1.5.0-preview.1' $script:Now.AddDays(-200)), (New-Stored '1.5.0-preview.2'))
            @(Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion $stored -UtcNow $script:Now).Count | Should -Be 0
            (Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion $stored -MaxAgeDays 180 -UtcNow $script:Now).Name | Should -Be '1.5.0-preview.1'
        }

        It 'never deletes releases or versions in another form' {
            $stored = @((New-Stored '1.4.0' $script:Now.AddDays(-999)), (New-Stored '1.4.0.3' $script:Now.AddDays(-999)), (New-Stored '1.4.0-rc' $script:Now.AddDays(-999)))
            @(Get-PrunablePreviewVersion -PackageName 'P' -StoredVersion $stored -MaxAgeDays 1 -UtcNow $script:Now).Count | Should -Be 0
        }
    }
}
