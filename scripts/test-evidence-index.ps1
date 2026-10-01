# Offline fixtures exercise action/resume receipt indexing; no Bridge or game calls.
$ErrorActionPreference = 'Stop'
$fixtureDir = Join-Path ([IO.Path]::GetTempPath()) ('spherewright-evidence-index-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $fixtureDir
$run = 'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa'
$otherRun = 'bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb'
$missingRun = 'cccccccccccccccccccccccccccccccc'
$lostResumeRun = 'dddddddddddddddddddddddddddddddd'
$unknownResumeRun = 'eeeeeeeeeeeeeeeeeeeeeeeeeeeeeeee'
$mixedFamilyRun = 'ffffffffffffffffffffffffffffffff'
$wrongIdentityRun = '11111111111111111111111111111111'
$wrongOrdinalRun = '22222222222222222222222222222222'
$conflictingIntentRun = '33333333333333333333333333333333'
$files = [Collections.Generic.List[string]]::new()
$passed = 0
$indexScript = Join-Path $PSScriptRoot 'Get-SpherewrightEvidenceIndex.ps1'

function Add-Fixture([int]$Ordinal, [string]$Type, [object]$Result, [string]$AtUtc, [bool]$Success = $true, [string]$Family = 'action', [string]$RunId = $run) {
    $name = '{0}-{1}-{2:D4}-{3}.json' -f $Family, $RunId, $Ordinal, $Type
    $path = Join-Path $fixtureDir $name
    [pscustomobject]@{
        recordType = $Type
        runId = $RunId
        ordinal = $Ordinal
        recordedAtUtc = $AtUtc
        payload = @{ response = @{ success = $Success; result = $Result } }
    } | ConvertTo-Json -Depth 10 -Compress | Set-Content -LiteralPath $path
    $files.Add($path)
}

function Add-Intent([int]$Ordinal, [string]$Method, [string]$Family = 'action', [string]$RunId = $run, [string]$ConflictingMethod = '') {
    $path = Join-Path $fixtureDir ('{0}-{1}-{2:D4}-commit-intent.json' -f $Family, $RunId, $Ordinal)
    if ($Family -ceq 'resume') {
        $payload = @{ operation = $Method; actionKind = 'resume-owned-world'; expectedPlanetId = 104; minimumJournalSequence = 1; idempotencyKey = 'fixture-idempotency-key'; minimumGameTick = 1; planTokenLoadedInMemoryOnly = $true }
        if ($ConflictingMethod) { $payload.method = $ConflictingMethod }
    } else {
        $payload = @{ method = $Method; request = @{ planToken = 'fixture-secret-must-not-appear' } }
    }
    [pscustomobject]@{
        recordType = 'commit-intent'
        runId = $RunId
        ordinal = $Ordinal
        recordedAtUtc = '2026-09-30T00:00:05Z'
        payload = $payload
    } | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $path
    $files.Add($path)
}

function Add-MalformedFixture([string]$FileRunId, [int]$FileOrdinal, [string]$RecordRunId, [int]$RecordOrdinal) {
    $type = 'bridge-response-commit_save'
    $path = Join-Path $fixtureDir ('action-{0}-{1:D4}-{2}.json' -f $FileRunId, $FileOrdinal, $type)
    [pscustomobject]@{
        recordType = $type
        runId = $RecordRunId
        ordinal = $RecordOrdinal
        recordedAtUtc = '2026-09-30T00:00:00Z'
        payload = @{ response = @{ success = $true; result = @{ accepted = $false } } }
    } | ConvertTo-Json -Depth 8 -Compress | Set-Content -LiteralPath $path
    $files.Add($path)
}

function Assert-IndexThrows([string[]]$RunIds, [string]$ExpectedMessage) {
    try {
        & $indexScript -EvidenceDirectory $fixtureDir -RunIds $RunIds | Out-Null
    } catch {
        if ($_.Exception.Message -notlike ('*' + $ExpectedMessage + '*')) {
            throw "Expected index failure containing '$ExpectedMessage'."
        }
        return
    }
    throw "Index unexpectedly accepted the fixture; expected '$ExpectedMessage'."
}

try {
    Add-Fixture 1 'bridge-response-commit_build' @{accepted=$true;actionId='action-one';idempotentReplay=$false;planToken='fixture-secret-must-not-appear'} '2026-09-30T00:00:00Z'
    Add-Fixture 2 'bridge-response-get_action_result' @{actionId='action-one';terminal=$false;state='waiting_for_game'} '2026-09-30T00:00:01Z'
    Add-Fixture 3 'bridge-response-get_action_result' @{actionId='action-one';terminal=$true;succeeded=$true;state='completed';completedAtGameTick=123} '2026-09-30T00:00:02.125Z'
    Add-Fixture 4 'bridge-response-commit_build' @{accepted=$true;actionId='action-one';idempotentReplay=$true} '2026-09-30T00:00:03Z'
    Add-Fixture 5 'bridge-response-commit_save' @{accepted=$true;actionId='action-two';idempotentReplay=$false} '2026-09-30T00:00:04Z'
    $indexJson = & $indexScript -EvidenceDirectory $fixtureDir -RunIds @($run)
    if ($indexJson -match 'fixture-secret-must-not-appear' -or $indexJson -match [regex]::Escape($fixtureDir)) {
        throw 'Evidence index leaked a token or private evidence path.'
    }
    $summary = $indexJson | ConvertFrom-Json
    if ($summary.acceptedUnique -ne 2 -or $summary.replayResponses -ne 1 -or
        @($summary.unresolvedActionIds).Count -ne 1 -or $summary.unresolvedActionIds[0] -cne 'action-two' -or
        $summary.actions[0].receiptWallMs -ne 2125 -or $summary.actions[0].completedAtGameTick -ne 123 -or
        $summary.auditCoverage -cne 'action_receipts_only') {
        throw 'Evidence index incorrectly counted a replay, missing terminal, or receipt time.'
    }
    $passed++

    Assert-IndexThrows -RunIds @($missingRun) -ExpectedMessage 'No commit/action receipts found'
    $passed++

    Add-Intent -Ordinal 1 -Method 'commit_resume_owned_game' -Family 'resume' -RunId $otherRun
    Add-Fixture -Ordinal 2 -Type 'bridge-response-commit_resume_owned_game' -Result @{accepted=$true;actionId='resume-action-one';idempotentReplay=$false} -AtUtc '2026-09-30T00:00:01Z' -Family 'resume' -RunId $otherRun
    Add-Fixture -Ordinal 3 -Type 'bridge-response-get_action_result' -Result @{actionId='resume-action-one';terminal=$true;succeeded=$true;state='completed';completedAtGameTick=456} -AtUtc '2026-09-30T00:00:02Z' -Family 'resume' -RunId $otherRun
    $mixedSummary = (& $indexScript -EvidenceDirectory $fixtureDir -RunIds @($run, $otherRun)) | ConvertFrom-Json
    $resumeAction = @($mixedSummary.actions | Where-Object { $_.runId -ceq $otherRun }) | Select-Object -First 1
    if ($mixedSummary.acceptedUnique -ne 3 -or $mixedSummary.replayResponses -ne 1 -or
        @($mixedSummary.unresolvedActionIds).Count -ne 1 -or $null -eq $resumeAction -or
        $resumeAction.terminalState -cne 'completed' -or $resumeAction.succeeded -ne $true -or
        $resumeAction.commitKind -cne 'commit_resume_owned_game') {
        throw 'Mixed distinct action/resume runs or successful resume counting failed.'
    }
    $passed++

    Add-Intent 6 'commit_build'
    $indexJson = & $indexScript -EvidenceDirectory $fixtureDir -RunIds @($run)
    $summary = $indexJson | ConvertFrom-Json
    if ($summary.acceptedUnique -ne 2 -or -not $summary.newWriteBlocked -or @($summary.unresolvedCommitIntents).Count -ne 1 -or
        $summary.unresolvedCommitIntents[0].intentOrdinal -ne 6 -or $indexJson -match 'fixture-secret-must-not-appear') {
        throw 'Lost commit response incorrectly became safe to replay.'
    }
    $passed++

    Add-Fixture 7 'bridge-response-commit_build' @{} '2026-09-30T00:00:06Z' $false
    $summary = (& $indexScript -EvidenceDirectory $fixtureDir -RunIds @($run)) | ConvertFrom-Json
    if (@($summary.unclassifiedCommitResponses).Count -ne 1 -or @($summary.unresolvedCommitIntents).Count -ne 1 -or
        $summary.rejectedCommitResponses -ne 0) { throw 'Error envelope inferred a trustworthy rejection.' }
    $passed++

    Add-Intent -Ordinal 1 -Method 'commit_resume_owned_game' -Family 'resume' -RunId $lostResumeRun
    $lostResumeSummary = (& $indexScript -EvidenceDirectory $fixtureDir -RunIds @($lostResumeRun)) | ConvertFrom-Json
    if ($lostResumeSummary.acceptedUnique -ne 0 -or -not $lostResumeSummary.newWriteBlocked -or
        @($lostResumeSummary.unresolvedCommitIntents).Count -ne 1 -or $lostResumeSummary.replayResponses -ne 0) {
        throw 'Lost resume commit intent was not blocked from replay.'
    }
    $passed++

    Add-Intent -Ordinal 1 -Method 'commit_resume_owned_game' -Family 'resume' -RunId $unknownResumeRun
    Add-Fixture -Ordinal 2 -Type 'bridge-response-commit_resume_owned_game' -Result @{accepted=$true;actionId='resume-action-unknown';idempotentReplay=$false} -AtUtc '2026-09-30T00:00:01Z' -Family 'resume' -RunId $unknownResumeRun
    Add-Fixture -Ordinal 3 -Type 'bridge-response-get_action_result' -Result @{actionId='resume-action-unknown';terminal=$true;succeeded=$false;state='outcome_unknown';completedAtGameTick=$null} -AtUtc '2026-09-30T00:00:02Z' -Family 'resume' -RunId $unknownResumeRun
    $unknownSummary = (& $indexScript -EvidenceDirectory $fixtureDir -RunIds @($unknownResumeRun)) | ConvertFrom-Json
    if ($unknownSummary.acceptedUnique -ne 1 -or -not $unknownSummary.newWriteBlocked -or
        @($unknownSummary.uncertainTerminalActionIds).Count -ne 1 -or $unknownSummary.replayResponses -ne 0 -or
        @($unknownSummary.unresolvedCommitIntents).Count -ne 0) {
        throw 'Outcome-unknown resume action was not retained and blocked from replay.'
    }
    $passed++

    Add-Fixture -Ordinal 1 -Type 'bridge-response-commit_save' -Result @{accepted=$false} -AtUtc '2026-09-30T00:00:00Z' -Family 'action' -RunId $mixedFamilyRun
    Add-Fixture -Ordinal 2 -Type 'bridge-response-get_action_result' -Result @{actionId='cross-family-terminal';terminal=$true;succeeded=$true;state='completed'} -AtUtc '2026-09-30T00:00:01Z' -Family 'resume' -RunId $mixedFamilyRun
    Assert-IndexThrows -RunIds @($mixedFamilyRun) -ExpectedMessage 'Ambiguous receipt families'
    $passed++

    Add-MalformedFixture -FileRunId $wrongIdentityRun -FileOrdinal 1 -RecordRunId $otherRun -RecordOrdinal 1
    Assert-IndexThrows -RunIds @($wrongIdentityRun) -ExpectedMessage 'Receipt identity or response is malformed'
    $passed++

    Add-MalformedFixture -FileRunId $wrongOrdinalRun -FileOrdinal 1 -RecordRunId $wrongOrdinalRun -RecordOrdinal 2
    Assert-IndexThrows -RunIds @($wrongOrdinalRun) -ExpectedMessage 'Receipt identity or response is malformed'
    $passed++

    Add-Intent -Ordinal 1 -Method 'commit_resume_owned_game' -Family 'resume' -RunId $conflictingIntentRun -ConflictingMethod 'commit_save'
    Assert-IndexThrows -RunIds @($conflictingIntentRun) -ExpectedMessage 'Malformed commit intent'
    $passed++

    [pscustomobject]@{passed=$passed;gameCalls=0} | ConvertTo-Json -Compress
} finally {
    foreach ($file in $files) { Remove-Item -LiteralPath $file -Force }
    Remove-Item -LiteralPath $fixtureDir -Force
}
