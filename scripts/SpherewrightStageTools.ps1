# Dot-source AFTER SpherewrightActionClient and the existing protected transport.
# Importing this file makes zero requests. These helpers grant no write authority.
Set-StrictMode -Version Latest

function Get-SpherewrightStageField($Value, [string]$Name) {
    if ($null -eq $Value -or $null -eq $Value.PSObject.Properties[$Name]) {
        throw "Required response field missing: $Name"
    }
    return $Value.$Name
}

function Read-SpherewrightStageResult([string]$Method, [string]$SessionId, [hashtable]$Payload) {
    Get-SpherewrightBridgeResult -Response (Invoke-SpherewrightBridgeRequest -Method $Method -SessionId $SessionId -Payload $Payload) -Operation $Method
}

function Assert-SpherewrightStageSession($State, [string]$SessionId, [int]$PlanetId, [string]$GameVersion, [switch]$RequireWrites) {
    foreach ($field in @('sessionId','localPlanetId','gameVersion','gameLoaded','ownedBySpherewright','accessRestricted','writeHealth','gameTick','revision','writeBlockers')) {
        $null = Get-SpherewrightStageField $State $field
    }
    if ($State.sessionId -cne $SessionId -or $State.localPlanetId -ne $PlanetId -or $State.gameVersion -cne $GameVersion -or
        $State.gameLoaded -ne $true -or $State.ownedBySpherewright -ne $true -or $State.accessRestricted -ne $false -or
        $State.writeHealth -cne 'healthy' -or @($State.writeBlockers).Count -or $null -eq $State.gameTick -or $State.gameTick -lt 0) {
        throw 'Session identity/version/health boundary changed; stop, do not load or replay.'
    }
    if ($RequireWrites -and ((Get-SpherewrightStageField $State 'writesAllowed') -ne $true -or
        (Get-SpherewrightStageField $State 'peacefulMode') -cne 'confirmed_peaceful')) {
        throw 'Ordinary write boundary is not satisfied.'
    }
}

function Get-SpherewrightDurableJournalBoundary($Journal, [string]$SessionId) {
    foreach ($field in @('sessionId','entries','durableThroughSequence','persistencePending','persistenceError')) {
        $null = Get-SpherewrightStageField $Journal $field
    }
    if ($Journal.sessionId -cne $SessionId -or $null -eq $Journal.entries -or $Journal.persistencePending -ne $false -or $Journal.persistenceError) {
        throw 'Journal identity or durable persistence boundary is unproved.'
    }
    [long]$highest = 0
    foreach ($entry in @($Journal.entries)) {
        $sequence = Get-SpherewrightStageField $entry 'sequence'
        if ($sequence -ne $highest + 1) { throw 'Journal sequence coverage is not contiguous.' }
        $highest = [long]$sequence
    }
    if ($Journal.durableThroughSequence -ne $highest) { throw 'Journal contains entries not proved durable.' }
    return $highest
}

function Invoke-SpherewrightResearchAndSave {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][ValidateRange(1, [int]::MaxValue)][int]$PlanetId,
        [Parameter(Mandatory)][string]$GameVersion,
        [Parameter(Mandatory)][ValidateRange(1, [int]::MaxValue)][int]$TechId,
        [Parameter(Mandatory)][ValidateRange(0, 8)][int]$AcceptedBefore,
        [Parameter(Mandatory)][scriptblock]$ValidateResearchPlan,
        [Parameter(Mandatory)][scriptblock]$RecordEvidence,
        [bool]$PrioritizeQueued = $false,
        [ValidateRange(1, 1800)][int]$TimeoutSeconds = 180,
        [Nullable[datetimeoffset]]$DispatchedAtUtc
    )

    # Caller must already hold the verified single-writer handoff and two write
    # slots. Do not import a historical executor or infer permissions from this count.
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $phase = 'fresh_reads'
    $actions = [Collections.Generic.List[object]]::new()
    $acceptedDelta = 0
    try {
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        $player = Read-SpherewrightStageResult get_player_state $SessionId @{planetId=$PlanetId}
        if ((Get-SpherewrightStageField $player 'sessionId') -cne $SessionId -or
            (Get-SpherewrightStageField $player 'planetId') -ne $PlanetId) { throw 'Fresh player identity changed.' }
        $progress = Read-SpherewrightStageResult get_progression_state $SessionId @{planetId=$PlanetId}
        if ((Get-SpherewrightStageField $progress 'sessionId') -cne $SessionId -or
            (Get-SpherewrightStageField $progress 'planetId') -ne $PlanetId) { throw 'Fresh progression identity changed.' }
        $hash = Get-SpherewrightStageField $progress 'selectionStateHash'
        if ([string]::IsNullOrWhiteSpace($hash)) { throw 'selectionStateHash is unavailable.' }
        $hashVersion = Get-SpherewrightStageField $progress 'selectionStateHashVersion'
        $initialJournal = Read-SpherewrightStageResult get_gameplay_journal $SessionId @{}
        $initialDurable = Get-SpherewrightDurableJournalBoundary $initialJournal $SessionId
        $firstPrepareMs = $watch.Elapsed.TotalMilliseconds
        $firstPrepareUtc = [datetimeoffset]::UtcNow
        $phase = 'research'
        $research = Invoke-SpherewrightNormalAction -PrepareMethod prepare_select_research -CommitMethod commit_select_research -SessionId $SessionId -PlanetId $PlanetId -TimeoutSeconds $TimeoutSeconds -PreparePayload @{
            planetId=$PlanetId;techId=$TechId;expectedSelectionStateHash=$hash;stateHashVersion=$hashVersion;prioritizeQueued=$PrioritizeQueued
        } -ValidatePrepared $ValidateResearchPlan
        if (-not $research.committed.idempotentReplay) { $acceptedDelta++ }
        $actions.Add([pscustomobject]@{actionId=$research.committed.actionId;kind='select-research';timingMs=$research.timingMs})
        $phase = 'research_readback'
        $progress = Read-SpherewrightStageResult get_progression_state $SessionId @{planetId=$PlanetId}
        if ($progress.sessionId -cne $SessionId -or $progress.planetId -ne $PlanetId -or
            (@($progress.techQueue) -notcontains $TechId -and @($progress.technologies | Where-Object { $_.techId -eq $TechId -and $_.unlocked -eq $true }).Count -ne 1)) {
            throw 'Selected technology is neither queued nor proved unlocked; retain the successful action.'
        }
        $phase = 'save'
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        $save = Invoke-SpherewrightNormalAction -PrepareMethod prepare_save -CommitMethod commit_save -SessionId $SessionId -PlanetId $PlanetId -TimeoutSeconds $TimeoutSeconds -PreparePayload @{
            planetId=$PlanetId;expectedRevision=$state.revision;stateHashVersion=1
        } -ValidatePrepared { param($plan) $plan.actionKind -ceq 'save' -and @($plan.itemBudget).Count -eq 0 }
        if (-not $save.committed.idempotentReplay) { $acceptedDelta++ }
        $actions.Add([pscustomobject]@{actionId=$save.committed.actionId;kind='save';timingMs=$save.timingMs})
        $phase = 'save_readback'
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        if ((Get-SpherewrightStageField $state 'ownedSaveState') -cne 'saved' -or
            (Get-SpherewrightStageField $state 'lastOwnedSaveGameTick') -ne $save.result.completedAtGameTick -or
            (Get-SpherewrightStageField $state 'restartResumeAvailable') -ne $true) { throw 'Normal save/resume readback is unproved.' }
        $journal = Read-SpherewrightStageResult get_gameplay_journal $SessionId @{}
        $durable = Get-SpherewrightDurableJournalBoundary $journal $SessionId
        if ($durable -lt $initialDurable) { throw 'Journal durable sequence regressed; no replay.' }
        $summary = [pscustomobject]@{
            result='completed';acceptedDelta=$acceptedDelta;acceptedAfter=$AcceptedBefore+$acceptedDelta
            frozen=($AcceptedBefore+$acceptedDelta -ge 10);inFlightActionIds=@();actions=$actions.ToArray()
            observedTick=$state.gameTick;savedTick=$state.lastOwnedSaveGameTick;revision=$state.revision;durableThroughSequence=$durable
            timingMs=[pscustomobject]@{entryToFirstPrepare=[math]::Round($firstPrepareMs,3);dispatchToFirstPrepare=$(if ($null -eq $DispatchedAtUtc) { $null } else { [math]::Round(($firstPrepareUtc-$DispatchedAtUtc).TotalMilliseconds,3) });total=[math]::Round($watch.Elapsed.TotalMilliseconds,3)}
            unproved=@('save_restart','production_throughput');nextBlocker=$null
        }
        $null = & $RecordEvidence $summary
        return $summary
    } catch {
        # Count a known accepted commit even if terminal/readback failed. An
        # unknown commit remains unknown; a local summary cannot clear the lease.
        $currentAccepted = $_.Exception.Data['spherewrightCommitAccepted']
        if ($phase -in @('research','save') -and $currentAccepted -eq $true -and $_.Exception.Data['spherewrightIdempotentReplay'] -ne $true) { $acceptedDelta++ }
        $_.Exception.Data['spherewrightStagePhase'] = $phase
        $_.Exception.Data['spherewrightStageAcceptedDelta'] = $acceptedDelta
        $_.Exception.Data['spherewrightPriorActionIds'] = @($actions | ForEach-Object actionId)
        $_.Exception.Data['spherewrightDoNotReplayStage'] = $true
        throw
    }
}
