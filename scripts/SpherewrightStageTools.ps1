# Dot-source AFTER SpherewrightActionClient and the existing protected transport.
# Importing this file makes zero requests. These helpers grant no write authority.
Set-StrictMode -Version Latest

function Get-SpherewrightCommitChecks {
    [CmdletBinding()]
    param([Parameter(Mandatory)][ValidatePattern('^[0-9a-f]{40}$')][string]$CommitSha)
    # Reuse gh, fetch once, exact full SHA. An absent run is unknown, not green.
    $json = & gh run list --repo AvaloNero/Spherewright --commit $CommitSha --limit 5 --json databaseId,headSha,status,conclusion,url
    if ($LASTEXITCODE -ne 0) { throw 'CI status collection failed; do not infer success.' }
    $decoded = ($json -join "`n") | ConvertFrom-Json
    $runs = @()
    # Windows PowerShell can emit JSON [] as one empty-array pipeline object.
    # Normalize enumeration before StrictMode property access; absent is unknown.
    foreach ($run in $decoded) { if ($null -ne $run) { $runs += ,$run } }
    if (@($runs | Where-Object { $_.headSha -cne $CommitSha }).Count) { throw 'CI returned a different source commit.' }
    [pscustomobject]@{commit=$CommitSha;observed=($runs.Count -gt 0);runs=$runs;signOff=$false}
}

function Get-SpherewrightStageField($Value, [string]$Name) {
    if ($null -eq $Value -or $null -eq $Value.PSObject.Properties[$Name]) {
        throw "Required response field missing: $Name"
    }
    return $Value.$Name
}

function Get-SpherewrightStorageItemCount {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]$Snapshot,
        [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$ItemId
    )
    if ((Get-SpherewrightStageField $Snapshot 'objectKind') -cne 'entity' -or
        (Get-SpherewrightStageField $Snapshot 'componentKind') -cne 'storage') {
        throw 'An observed built storage entity is required; research points are not items.'
    }
    $null = Get-SpherewrightStageField $Snapshot 'buffers'
    if ($null -eq $Snapshot.buffers) { throw 'Storage buffers were not observed.' }
    [long]$total = 0
    # Storage emits one row per occupied grid, not one row per item type.
    foreach ($buffer in @($Snapshot.buffers)) {
        $observedItemId = Get-SpherewrightStageField $buffer 'itemId'
        if (($observedItemId -isnot [int] -and $observedItemId -isnot [long]) -or
            $observedItemId -le 0 -or $observedItemId -gt [int]::MaxValue) { throw 'Storage item identity is not a positive supported integer.' }
        if ($observedItemId -ne $ItemId) { continue }
        if ((Get-SpherewrightStageField $buffer 'role') -cne 'storage' -or
            (Get-SpherewrightStageField $buffer 'countUnit') -cne 'items' -or
            (Get-SpherewrightStageField $buffer 'unitsPerItem') -ne 1) {
            throw 'Storage item count has an incompatible role or unit.'
        }
        $count = Get-SpherewrightStageField $buffer 'count'
        if (($count -isnot [int] -and $count -isnot [long]) -or $count -lt 0 -or
            $total -gt [long]::MaxValue - $count) { throw 'Storage item count is not a non-negative bounded integer.' }
        $total += $count
    }
    return $total
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
    # Research itemBudget is native future research-consumption, not an empty
    # inventory transaction. Validate it against the approved fresh tech requirements.
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
        $actions.Add([pscustomobject]@{actionId=$research.committed.actionId;kind='select-research';timingMs=$research.timingMs;observedExecutionGameTicks=$research.observedExecutionGameTicks})
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
        $actions.Add([pscustomobject]@{actionId=$save.committed.actionId;kind='save';timingMs=$save.timingMs;observedExecutionGameTicks=$save.observedExecutionGameTicks})
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

function Invoke-SpherewrightMaterialHandcraftAndSave {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$PlanetId,
        [Parameter(Mandatory)][string]$GameVersion,
        [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$StorageEntityId,
        [Parameter(Mandatory)][ValidateRange(1, 6000)][int]$MaterialItemId,
        [Parameter(Mandatory)][ValidateRange(1, 1000)][int]$MaterialCount,
        [Parameter(Mandatory)][ValidateRange(1, 2147483647)][int]$RecipeId,
        [Parameter(Mandatory)][ValidateRange(1, 100)][int]$CraftCount,
        [Parameter(Mandatory)][ValidateRange(0, 7)][int]$AcceptedBefore,
        [Parameter(Mandatory)][scriptblock]$ValidateCraftPlan,
        [Parameter(Mandatory)][scriptblock]$ValidateCraftReadback,
        [Parameter(Mandatory)][scriptblock]$RecordEvidence,
        [ValidateRange(1, 1800)][int]$TimeoutSeconds = 180
    )
    # One explicitly approved ordinary-material transfer, one recipe, one save.
    # Caller supplies the single-writer lease and THREE external audit slots.
    # Matrix/cache transfers are excluded; this does not select goals or recipes.
    $watch = [Diagnostics.Stopwatch]::StartNew()
    $phase = 'fresh_reads'; $acceptedDelta = 0
    $actions = [Collections.Generic.List[object]]::new()
    try {
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        $journal = Read-SpherewrightStageResult get_gameplay_journal $SessionId @{}
        $initialDurable = Get-SpherewrightDurableJournalBoundary $journal $SessionId
        $player = Read-SpherewrightStageResult get_player_state $SessionId @{planetId=$PlanetId}
        if ($player.sessionId -cne $SessionId -or $player.planetId -ne $PlanetId -or
            (Get-SpherewrightStageField $player 'movementState') -cne 'Walk' -or
            (Get-SpherewrightStageField $player 'speed') -gt .1 -or
            (Get-SpherewrightStageField $player 'coreEnergy') -le 0 -or
            @((Get-SpherewrightStageField $player 'handcraftQueue')).Count) { throw 'Settled powered player and empty forge queue required.' }
        $source = Read-SpherewrightStageResult inspect_factory_entity $SessionId @{planetId=$PlanetId;objectId=$StorageEntityId}
        if ($source.sessionId -cne $SessionId -or $source.planetId -ne $PlanetId -or $source.objectId -ne $StorageEntityId -or
            (Get-SpherewrightStorageItemCount $source $MaterialItemId) -lt $MaterialCount) { throw 'Approved ordinary material source is unavailable.' }
        $beforeCount = Get-SpherewrightInventoryCount $player $MaterialItemId
        $firstPrepareMs = $watch.Elapsed.TotalMilliseconds
        $phase = 'transfer'
        $transfer = Invoke-SpherewrightNormalAction -PrepareMethod prepare_transfer -CommitMethod commit_transfer -SessionId $SessionId -PlanetId $PlanetId -TimeoutSeconds $TimeoutSeconds -PreparePayload @{
            planetId=$PlanetId;direction='storage-to-player';storageEntityId=$StorageEntityId;itemId=$MaterialItemId;count=$MaterialCount
            expectedPlayerStateHash=$player.stateHash;expectedStorageStateHash=$source.stateHash;stateHashVersion=1
        } -ValidatePrepared {
            param($plan)
            $budget = @($plan.itemBudget)
            [bool]($plan.actionKind -ceq 'transfer' -and $plan.sourceObjectId -eq $StorageEntityId -and
                $null -eq $plan.destinationObjectId -and $budget.Count -eq 1 -and
                $budget[0].itemId -eq $MaterialItemId -and $budget[0].count -eq $MaterialCount -and
                $budget[0].direction -ceq 'storage-to-player')
        }
        if ($transfer.committed.idempotentReplay) { throw 'Unexpected replay; reconcile original stage.' }
        $acceptedDelta++
        $actions.Add([pscustomobject]@{actionId=$transfer.committed.actionId;kind='transfer';timingMs=$transfer.timingMs})
        $phase = 'transfer_readback'
        $player = Read-SpherewrightStageResult get_player_state $SessionId @{planetId=$PlanetId}
        $afterSource = Read-SpherewrightStageResult inspect_factory_entity $SessionId @{planetId=$PlanetId;objectId=$StorageEntityId}
        if ($player.sessionId -cne $SessionId -or $player.planetId -ne $PlanetId -or
            (Get-SpherewrightInventoryCount $player $MaterialItemId) -ne $beforeCount+$MaterialCount -or
            $transfer.result.beforeTargetAmount -lt $MaterialCount -or
            $transfer.result.afterTargetAmount -ne $transfer.result.beforeTargetAmount-$MaterialCount) { throw 'Accepted ordinary material transfer is not conserved; no replay.' }
        foreach ($field in @('sessionId','planetId','objectId','objectKind','itemId','componentKind','position','rotation','recipeId','connections','storageConfiguration')) {
            if ((ConvertTo-Json -InputObject $source.$field -Depth 12 -Compress) -cne
                (ConvertTo-Json -InputObject $afterSource.$field -Depth 12 -Compress)) { throw "Accepted transfer source changed $field; no replay." }
        }
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        $phase = 'handcraft'
        $craft = Invoke-SpherewrightNormalAction -PrepareMethod prepare_handcraft -CommitMethod commit_handcraft -SessionId $SessionId -PlanetId $PlanetId -TimeoutSeconds $TimeoutSeconds -PreparePayload @{
            planetId=$PlanetId;recipeId=$RecipeId;count=$CraftCount;expectedPlayerStateHash=$player.stateHash;stateHashVersion=1
        } -ValidatePrepared $ValidateCraftPlan
        if ($craft.committed.idempotentReplay) { throw 'Unexpected replay; reconcile original stage.' }
        $acceptedDelta++
        $actions.Add([pscustomobject]@{actionId=$craft.committed.actionId;kind='handcraft';timingMs=$craft.timingMs})
        $phase = 'handcraft_readback'
        $craftedPlayer = Read-SpherewrightStageResult get_player_state $SessionId @{planetId=$PlanetId}
        if ($craftedPlayer.sessionId -cne $SessionId -or $craftedPlayer.planetId -ne $PlanetId -or @($craftedPlayer.handcraftQueue).Count) { throw 'Accepted handcraft identity/terminal queue readback failed; no replay.' }
        $readback = @(& $ValidateCraftReadback $player $craftedPlayer $craft.result)
        if ($readback.Count -ne 1 -or $readback[0] -isnot [bool] -or -not $readback[0]) { throw 'Accepted handcraft exact readback not approved; no replay.' }
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        $phase = 'save'
        $save = Invoke-SpherewrightNormalAction -PrepareMethod prepare_save -CommitMethod commit_save -SessionId $SessionId -PlanetId $PlanetId -TimeoutSeconds $TimeoutSeconds -PreparePayload @{
            planetId=$PlanetId;expectedRevision=$state.revision;stateHashVersion=1
        } -ValidatePrepared {param($plan) $plan.actionKind -ceq 'save' -and @($plan.itemBudget).Count -eq 0}
        if ($save.committed.idempotentReplay) { throw 'Unexpected replay; reconcile original stage.' }
        $acceptedDelta++
        $actions.Add([pscustomobject]@{actionId=$save.committed.actionId;kind='save';timingMs=$save.timingMs})
        $phase = 'save_readback'
        $state = Read-SpherewrightStageResult get_session_state $SessionId @{}
        Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion -RequireWrites
        if ($state.ownedSaveState -cne 'saved' -or $state.lastOwnedSaveGameTick -ne $save.result.completedAtGameTick -or $state.restartResumeAvailable -ne $true) { throw 'Accepted save readback unproved; no replay.' }
        $journal = Read-SpherewrightStageResult get_gameplay_journal $SessionId @{}
        $durable = Get-SpherewrightDurableJournalBoundary $journal $SessionId
        if ($durable -lt $initialDurable) { throw 'Durable Journal regressed; no replay.' }
        $summary = [pscustomobject]@{result='completed';acceptedDelta=$acceptedDelta;acceptedAfter=$AcceptedBefore+$acceptedDelta;frozen=($AcceptedBefore+$acceptedDelta -ge 10);inFlightActionIds=@();actions=$actions.ToArray();observedTick=$state.gameTick;savedTick=$state.lastOwnedSaveGameTick;revision=$state.revision;durableThroughSequence=$durable;timingMs=[pscustomobject]@{entryToFirstPrepare=$firstPrepareMs;total=$watch.Elapsed.TotalMilliseconds};unproved=@('save_restart','production_throughput')}
        $null = & $RecordEvidence $summary
        return $summary
    } catch {
        if ($phase -in @('transfer','handcraft','save') -and $_.Exception.Data['spherewrightCommitAccepted'] -eq $true -and $_.Exception.Data['spherewrightIdempotentReplay'] -ne $true) { $acceptedDelta++ }
        $_.Exception.Data['spherewrightStagePhase']=$phase
        $_.Exception.Data['spherewrightStageAcceptedDelta']=$acceptedDelta
        $_.Exception.Data['spherewrightPriorActionIds']=@($actions|ForEach-Object actionId)
        $_.Exception.Data['spherewrightDoNotReplayStage']=$true
        throw
    }
}
