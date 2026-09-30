# Offline fixture: exact named runs only, no Bridge or game calls.
$ErrorActionPreference = 'Stop'
$fixtureDir = Join-Path ([IO.Path]::GetTempPath()) ('spherewright-evidence-index-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureDir
$run = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$otherRun = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
$files = [Collections.Generic.List[string]]::new()

function Add-Fixture([int]$Ordinal, [string]$Type, [object]$Result, [string]$AtUtc, [bool]$Success = $true) {
    $name = 'action-{0}-{1:D4}-{2}.json' -f $run, $Ordinal, $Type
    $path = Join-Path $fixtureDir $name
    [pscustomobject]@{
        recordType = $Type
        runId = $run
        ordinal = $Ordinal
        recordedAtUtc = $AtUtc
        payload = @{ response = @{ success = $Success; result = $Result } }
    } | ConvertTo-Json -Depth 10 -Compress | Set-Content -LiteralPath $path
    $files.Add($path)
}

try {
    Add-Fixture 1 'bridge-response-commit_build' @{accepted=$true;actionId='action-one';idempotentReplay=$false;planToken='fixture-secret-must-not-appear'} '2026-09-30T00:00:00Z'
    Add-Fixture 2 'bridge-response-get_action_result' @{actionId='action-one';terminal=$false;state='waiting_for_game'} '2026-09-30T00:00:01Z'
    Add-Fixture 3 'bridge-response-get_action_result' @{actionId='action-one';terminal=$true;succeeded=$true;state='completed';completedAtGameTick=123} '2026-09-30T00:00:02Z'
    Add-Fixture 4 'bridge-response-commit_build' @{accepted=$true;actionId='action-one';idempotentReplay=$true} '2026-09-30T00:00:03Z'
    Add-Fixture 5 'bridge-response-commit_save' @{accepted=$true;actionId='action-two';idempotentReplay=$false} '2026-09-30T00:00:04Z'
    $indexJson = & (Join-Path $PSScriptRoot 'Get-SpherewrightEvidenceIndex.ps1') -EvidenceDirectory $fixtureDir -RunIds @($run)
    if ($indexJson -match 'fixture-secret-must-not-appear' -or $indexJson -match [regex]::Escape($fixtureDir)) {
        throw 'Evidence index leaked a token or private evidence path.'
    }
    $summary = $indexJson | ConvertFrom-Json
    if ($summary.acceptedUnique -ne 2 -or $summary.replayResponses -ne 1 -or
        @($summary.unresolvedActionIds).Count -ne 1 -or $summary.unresolvedActionIds[0] -cne 'action-two' -or
        $summary.actions[0].receiptWallMs -ne 2000 -or $summary.actions[0].completedAtGameTick -ne 123 -or
        $summary.auditCoverage -cne 'action_receipts_only') {
        throw 'Evidence index incorrectly counted a replay, missing terminal, or receipt time.'
    }
    $missingRejected = $false
    try {
        & (Join-Path $PSScriptRoot 'Get-SpherewrightEvidenceIndex.ps1') -EvidenceDirectory $fixtureDir -RunIds @($otherRun) | Out-Null
    } catch { $missingRejected = $true }
    if (-not $missingRejected) { throw 'Index silently accepted a missing run.' }
    [pscustomobject]@{passed=2;gameCalls=0} | ConvertTo-Json -Compress
} finally {
    foreach ($file in $files) { Remove-Item -LiteralPath $file -Force }
    Remove-Item -LiteralPath $fixtureDir -Force
}
