#Requires -Version 7.0

<#
.SYNOPSIS
Creates or updates this repository's GitHub branch rulesets from the JSON files beside this script.

.DESCRIPTION
Each <name>.json file is a ruleset in the GitHub REST API format.  A ruleset whose name already exists on the repository
is replaced; otherwise it is created.  Requires the GitHub CLI (gh), signed in as a repository administrator.

.PARAMETER Name
The rulesets to apply, named by JSON file (without extension), e.g. develop, master.

.PARAMETER Repository
The repository as owner/name.  Defaults to the repository the GitHub CLI resolves from the current directory.

.EXAMPLE
./.github/rulesets/UpdateRepositoryRulesets.ps1 -Name develop -WhatIf
#>
[CmdletBinding(SupportsShouldProcess)]
param (
    [Parameter(Mandatory)]
    [ValidateNotNullOrEmpty()]
    [string[]] $Name,

    [Parameter()]
    [string] $Repository
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrEmpty($Repository)) {
    $Repository = & gh repo view --json nameWithOwner --jq .nameWithOwner
    if ($LASTEXITCODE -ne 0) {
        Write-Error 'Failed to resolve the repository.  Pass -Repository owner/name.'
    }
}

$rulesetsOutput = & gh api "repos/$Repository/rulesets"
if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to list the rulesets of $Repository."
}
$existingRulesets = $rulesetsOutput | ConvertFrom-Json

foreach ($rulesetName in $Name) {
    $rulesetPath = Join-Path $PSScriptRoot "$rulesetName.json"
    if (-not (Test-Path $rulesetPath -PathType Leaf)) {
        Write-Error "Ruleset file $rulesetPath not found."
    }

    $ruleset = Get-Content $rulesetPath -Raw | ConvertFrom-Json
    $existingRuleset = $existingRulesets | Where-Object -Property name -EQ -Value $ruleset.name
    if ($null -ne $existingRuleset) {
        if ($PSCmdlet.ShouldProcess("$Repository ruleset '$($ruleset.name)' (id $($existingRuleset.id))", 'Replace')) {
            & gh api --method PUT "repos/$Repository/rulesets/$($existingRuleset.id)" --input $rulesetPath --jq '"Updated ruleset \(.name) (id \(.id))."'
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to update ruleset $($ruleset.name)."
            }
        }
    }
    else {
        if ($PSCmdlet.ShouldProcess("$Repository ruleset '$($ruleset.name)'", 'Create')) {
            & gh api --method POST "repos/$Repository/rulesets" --input $rulesetPath --jq '"Created ruleset \(.name) (id \(.id))."'
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to create ruleset $($ruleset.name)."
            }
        }
    }
}
