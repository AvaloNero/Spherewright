[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpherewrightPackageSurface.ps1')

$playbook = 'maximumObjectsToSubmit resumeBuildId never replay the whole blueprint spherewright_get_governor_plan nonzero measured baseline ten game minutes researchQueueReadback validationBaselineProposalHash inputEndpointFacingDot/outputEndpointFacingDot foundryIntent expectedFoundryPlanHash boundaryPorts 2011/2012 transportBudget'
$readNames = @('spherewright_get_overseer_production', 'spherewright_get_overseer_summary',
    'spherewright_get_overseer_diagnostic_bundle', 'spherewright_get_foundry_plan',
    'spherewright_inspect_blueprint', 'spherewright_export_blueprint',
    'spherewright_get_blueprint_builds', 'spherewright_get_governor_plan')
$writeNames = @('spherewright_prepare_upgrade', 'spherewright_commit_upgrade',
    'spherewright_prepare_blueprint_build', 'spherewright_commit_blueprint_build',
    'spherewright_prepare_cancel_blueprint', 'spherewright_commit_cancel_blueprint',
    'spherewright_prepare_select_research', 'spherewright_prepare_dismantle', 'spherewright_commit_dismantle')
function New-TestSurface {
    foreach ($name in ($readNames + $writeNames)) {
        [pscustomobject]@{ name=$name; description='ordinary2011/2012 reciprocal recovery transportBudget'; annotations=[pscustomobject]@{readOnlyHint=($readNames -contains $name)}
            inputSchema=[pscustomobject]@{properties=[pscustomobject]@{prioritizeQueued=[pscustomobject]@{type='boolean'}; validationBaselineProposalHash=[pscustomobject]@{type='string'}; blueprint=[pscustomobject]@{type='object'}; foundryIntent=[pscustomobject]@{type='object'}; expectedFoundryPlanHash=[pscustomobject]@{type='string'}}} }
    }
}
function Assert-Rejects([scriptblock]$Action, [string]$ExpectedMessage) {
    try { & $Action } catch {
        if ($_.Exception.Message.Contains($ExpectedMessage)) { return }
        throw
    }
    throw "Expected rejection containing: $ExpectedMessage"
}
function Test-Surface([object[]]$Surface, [string]$Text=$playbook) {
    Assert-SpherewrightPackageSurface -Version '0.4.0.0' -Tools $Surface -PackagedPlaybook $Text -ResourcePlaybook $Text
}

$cases = 0
Test-Surface @(New-TestSurface); $cases++
Assert-SpherewrightPackageSurface -Version '0.3.3.0' -Tools @() -PackagedPlaybook 'legacy' -ResourcePlaybook 'legacy'; $cases++
Assert-SpherewrightPackageSurface -Version '0.3.3.0' -Tools @() -PackagedPlaybook "a`r`nb" -ResourcePlaybook "a`nb"; $cases++
Assert-Rejects { Assert-SpherewrightPackageSurface -Version '0.4.0' -Tools @(New-TestSurface) -PackagedPlaybook $playbook -ResourcePlaybook 'old' } 'differs'; $cases++
foreach ($name in ($readNames + $writeNames)) {
    $caseTools = @(New-TestSurface | Where-Object name -ne $name)
    Assert-Rejects { Test-Surface $caseTools } 'requires exactly one tool'; $cases++
}
$caseTools = @(New-TestSurface); $caseTools += $caseTools[0]
Assert-Rejects { Test-Surface $caseTools } 'requires exactly one tool'; $cases++
$caseTools = @(New-TestSurface); $caseTools[0].annotations.readOnlyHint = $false
Assert-Rejects { Test-Surface $caseTools } 'read-only metadata'; $cases++
$caseTools = @(New-TestSurface); $caseTools[-1].annotations.readOnlyHint = $true
Assert-Rejects { Test-Surface $caseTools } 'read-only metadata'; $cases++
$caseTools = @(New-TestSurface); $caseTools[0].PSObject.Properties.Remove('annotations')
Assert-Rejects { Test-Surface $caseTools } 'read-only metadata'; $cases++
$caseTools = @(New-TestSurface); ($caseTools | Where-Object name -eq 'spherewright_prepare_select_research').inputSchema.properties.prioritizeQueued.type = 'string'
Assert-Rejects { Test-Surface $caseTools } 'research-priority option'; $cases++
$caseTools = @(New-TestSurface); ($caseTools | Where-Object name -eq 'spherewright_prepare_select_research').inputSchema.properties.PSObject.Properties.Remove('prioritizeQueued')
Assert-Rejects { Test-Surface $caseTools } 'research-priority option'; $cases++
Assert-Rejects { Test-Surface @(New-TestSurface) 'old playbook' } 'lacks required'; $cases++
$caseTools = @(New-TestSurface)
($caseTools | Where-Object name -eq 'spherewright_get_governor_plan').inputSchema.properties.PSObject.Properties.Remove('validationBaselineProposalHash')
Assert-Rejects { Test-Surface $caseTools } 'Governor declaration option'; $cases++
$caseTools = @(New-TestSurface)
($caseTools | Where-Object name -eq 'spherewright_get_governor_plan').inputSchema.properties.validationBaselineProposalHash.type = @('string', 'null')
Test-Surface $caseTools; $cases++
$caseTools = @(New-TestSurface)
($caseTools | Where-Object name -eq 'spherewright_get_governor_plan').inputSchema.properties.validationBaselineProposalHash.type = 'number'
Assert-Rejects { Test-Surface $caseTools } 'Governor declaration option'; $cases++
foreach ($requirement in @(@('spherewright_get_foundry_plan','blueprint'), @('spherewright_prepare_blueprint_build','foundryIntent'), @('spherewright_prepare_blueprint_build','expectedFoundryPlanHash'))) {
    $caseTools = @(New-TestSurface)
    ($caseTools | Where-Object name -eq $requirement[0]).inputSchema.properties.PSObject.Properties.Remove($requirement[1])
    Assert-Rejects { Test-Surface $caseTools } 'Foundry finite-composition option'; $cases++
}
$caseTools = @(New-TestSurface)
($caseTools | Where-Object name -eq 'spherewright_prepare_dismantle').description = 'miners only'
Assert-Rejects { Test-Surface $caseTools } 'basic-sorter dismantle contract'; $cases++
$caseTools = @(New-TestSurface)
($caseTools | Where-Object name -eq 'spherewright_get_foundry_plan').description = 'old unconstrained graph'
Assert-Rejects { Test-Surface $caseTools } 'Foundry transport budget contract'; $cases++
Assert-Rejects { Test-Surface @(New-TestSurface) ($playbook.Replace('transportBudget', '')) } 'lacks required'; $cases++
[pscustomobject]@{passed=$cases;failed=0;scope='offline package surface policy only; no game or ZIP validation'} | ConvertTo-Json
