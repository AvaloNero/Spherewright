# Load StageTools and the existing protected transport first. Import is inert.
# Scheduling of existing read-only native windows, not a Governor/sign-off engine.
Set-StrictMode -Version Latest

function Invoke-SpherewrightProductionExperiment {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][string]$SessionId,
        [Parameter(Mandatory)][ValidateRange(1,[int]::MaxValue)][int]$PlanetId,
        [Parameter(Mandatory)][string]$GameVersion,
        [Parameter(Mandatory)][ValidateCount(1,32)][int[]]$EntityIds,
        # One bounded native query can cover a complete supply chain; the bridge
        # already permits64 IDs. Keep this caller deliberately smaller.
        [Parameter(Mandatory)][ValidateCount(1,12)][int[]]$ItemIds,
        [Parameter(Mandatory)][scriptblock]$ValidateObservation,
        [Parameter(Mandatory)][scriptblock]$ReceiptMarker,
        [Parameter(Mandatory)][scriptblock]$RecordEvidence,
        [ValidateSet('independent','continuous')][string]$Mode='independent',
        [ValidateRange(1,120)][int]$RequiredWindows=3,
        [ValidateRange(1,120)][int]$MaximumSamples=3,
        [ValidateRange(1,3600)][int]$IntervalGameTicks=606,
        [ValidateSet(600)][int]$WindowGameTicks=600,
        [ValidateRange(36000,360000)][int]$RequiredContinuousGameTicks=36000,
        # At15 game ticks/second,36000 ticks require2400 wall seconds.
        # Only the initial finite declaration may use this larger bound; a
        # running experiment's original deadline is never extended or replayed.
        [ValidateRange(1,3600)][int]$TimeoutSeconds=180,
        [ValidateRange(1,4096)][int]$MaximumRequests=90,
        [ValidateRange(1,10)][int]$PollSeconds=5
    )
    if (@($EntityIds|Where-Object {$_ -le 0}).Count -or @($ItemIds|Where-Object {$_ -le 0}).Count -or
        @($EntityIds|Sort-Object -Unique).Count -ne $EntityIds.Count -or @($ItemIds|Sort-Object -Unique).Count -ne $ItemIds.Count -or
        ($Mode -ceq 'independent' -and $MaximumSamples -lt $RequiredWindows) -or
        ($Mode -ceq 'continuous' -and $IntervalGameTicks -gt $WindowGameTicks)) { throw 'Invalid fixed scope/cadence/budget; no request sent.' }
    $started=Get-Date; $deadline=$started.AddSeconds($TimeoutSeconds)
    $experiment=[pscustomobject]@{requests=0;lastMethod=$null;readWallMs=0.0;pollWaitMs=0.0;samples=0;qualifying=0;resets=0;covered=0;startTick=$null;endTick=$null;lastWindowEnd=$null;lastSessionTick=$null;nextTick=0;firstTick=$null;lastTick=$null;lastRates=@()}
    $read = {
        param([string]$Method,[hashtable]$Payload)
        $experiment.lastMethod=$Method
        if ((Get-Date) -ge $deadline -or $experiment.requests -ge $MaximumRequests) {
            $failure=[InvalidOperationException]::new('Finite sampling deadline/request budget reached; do not extend or restart.')
            $failure.Data['spherewrightSamplingFailureKind']=$(if ((Get-Date) -ge $deadline) {'deadline_exhausted'} else {'request_budget_exhausted'})
            throw $failure
        }
        if ($Method -notin @('get_session_state','inspect_factory_entity','get_power_summary','get_overseer_production')) { throw 'Sampling is read-only.' }
        $experiment.requests++
        $clock=[Diagnostics.Stopwatch]::StartNew()
        try { $value=Read-SpherewrightStageResult $Method $SessionId $Payload }
        finally { $clock.Stop(); $experiment.readWallMs+=$clock.Elapsed.TotalMilliseconds }
        if ((Get-Date) -ge $deadline) {
            $failure=[InvalidOperationException]::new('Read crossed the original sampling deadline; evidence retained, no new request.')
            $failure.Data['spherewrightSamplingFailureKind']='read_crossed_deadline'
            throw $failure
        }
        return $value
    }
    $null = & $RecordEvidence ([pscustomobject]@{event='production-experiment-intent';mode=$Mode;entityIds=$EntityIds;itemIds=$ItemIds;intervalGameTicks=$IntervalGameTicks;windowGameTicks=$WindowGameTicks;pollSeconds=$PollSeconds;requiredWindows=$RequiredWindows;requiredContinuousGameTicks=$RequiredContinuousGameTicks;maximumSamples=$MaximumSamples;maximumRequests=$MaximumRequests;deadlineUtc=$deadline.ToUniversalTime().ToString('o');gameWrites=0})
    try {
        $initial=& $read get_session_state @{}
        Assert-SpherewrightStageSession $initial $SessionId $PlanetId $GameVersion
        # Exclude the rolling window that predates the declared experiment. Wait in
        # game ticks, within the original wall/request budget, not via a model loop.
        $experiment.nextTick=[long]$initial.gameTick+$WindowGameTicks
        while ($experiment.samples -lt $MaximumSamples) {
            $beginMarker=& $ReceiptMarker
            $state=& $read get_session_state @{}
            Assert-SpherewrightStageSession $state $SessionId $PlanetId $GameVersion
            if ($state.revision -ne $initial.revision -or ($null -ne $experiment.lastSessionTick -and $state.gameTick -lt $experiment.lastSessionTick)) { throw 'Read-only experiment revision/tick changed; stop for review.' }
            $experiment.lastSessionTick=[long]$state.gameTick
            if ($state.gameTick -lt $experiment.nextTick) {
                $remaining=($deadline-(Get-Date)).TotalMilliseconds
                if ($remaining -le 0) { throw 'Finite sampling deadline reached.' }
                $sleepMs=[int][math]::Min($PollSeconds*1000,$remaining)
                Start-Sleep -Milliseconds $sleepMs; $experiment.pollWaitMs+=$sleepMs; continue
            }
            $details=@(foreach($id in $EntityIds){
                $detail=& $read inspect_factory_entity @{planetId=$PlanetId;objectId=$id}
                if ($detail.sessionId -cne $SessionId -or $detail.planetId -ne $PlanetId -or $detail.objectId -ne $id) { throw 'Selected entity identity changed.' }
                $detail
            })
            $power=& $read get_power_summary @{planetId=$PlanetId}
            if ($power.sessionId -cne $SessionId -or $power.planetId -ne $PlanetId) { throw 'Power observation identity changed.' }
            $production=& $read get_overseer_production @{itemIds=$ItemIds;limit=8}
            if ($production.sessionId -cne $SessionId -or $production.nextCursor -or
                $production.totalFactoryCount -ne $production.returnedFactoryCount -or
                (@($production.requestedItemIds|Sort-Object) -join ',') -cne (@($ItemIds|Sort-Object) -join ',')) { throw 'Production identity/item/complete-page coverage unproved.' }
            $window=Get-SpherewrightStageField $production 'window'
            foreach($field in @('state','startGameTick','endGameTick','elapsedGameTicks','crossedSessionBoundary')){$null=Get-SpherewrightStageField $window $field}
            $endMarker=& $ReceiptMarker
            if ($beginMarker.runId -notmatch '^[0-9a-f]{32}$' -or $endMarker.runId -cne $beginMarker.runId -or $endMarker.ordinal -le $beginMarker.ordinal) { throw 'Original sample receipt range unproved.' }
            $sample=[pscustomobject]@{state=$state;entities=$details;power=$power;production=$production}
            $approval=@(& $ValidateObservation $sample)
            if ($approval.Count -ne 1 -or $approval[0] -isnot [bool]) { throw 'Observation validator must return exactly one Boolean; retain evidence.' }
            $valid=$approval[0] -and $window.state -ceq 'ready' -and $window.crossedSessionBoundary -eq $false
            $resetReason=$null
            if ($window.state -ceq 'ready' -and ($null -eq $window.startGameTick -or $window.elapsedGameTicks -ne $WindowGameTicks -or
                $window.endGameTick-$window.startGameTick+1 -ne $WindowGameTicks -or $production.capturedAtGameTick -lt $window.endGameTick)) { throw 'Native window fields do not match the declared period.' }
            if ($window.state -ceq 'ready' -and $window.startGameTick -le $initial.gameTick) { throw 'Native window predates the declared experiment; do not count it.' }
            if ($window.state -ceq 'ready' -and $null -ne $experiment.lastWindowEnd -and $window.endGameTick -le $experiment.lastWindowEnd) { throw 'Stale/regressed native window; do not count or replay it.' }
            $local=@($production.planets|Where-Object planetId -EQ $PlanetId)
            if ($local.Count -ne 1 -or @($local[0].production).Count -ne $ItemIds.Count -or
                (@($local[0].production.itemId|Sort-Object) -join ',') -cne (@($ItemIds|Sort-Object) -join ',')) { throw 'Local item-rate coverage unproved.' }
            $experiment.lastRates=@($local[0].production|Select-Object itemId,itemName,producedCount,consumedCount,actualProductionPerMinute,actualConsumptionPerMinute)
            if ($Mode -ceq 'independent') {
                if ($valid -and $null -ne $experiment.lastWindowEnd -and $window.startGameTick -le $experiment.lastWindowEnd) { throw 'Independent windows overlap; evidence cannot be combined.' }
                if ($valid) { $experiment.qualifying++ }
            } elseif ($valid) {
                if ($null -eq $experiment.endTick -or $window.startGameTick -gt $experiment.endTick+1) {
                    if ($null -ne $experiment.endTick) { $experiment.resets++;$resetReason='sample_gap' }
                    $experiment.startTick=[long]$window.startGameTick
                }
                $experiment.endTick=[long]$window.endGameTick
                $experiment.covered=$experiment.endTick-$experiment.startTick+1
            }
            if (-not $valid) {
                $experiment.resets++;$resetReason='observation_not_qualifying'
                $experiment.covered=0;$experiment.startTick=$null;$experiment.endTick=$null
            }
            $experiment.samples++
            $experiment.lastWindowEnd=[long]$window.endGameTick
            $experiment.nextTick=[long]$window.endGameTick+$IntervalGameTicks
            if ($null -eq $experiment.firstTick) { $experiment.firstTick=$production.capturedAtGameTick }
            $experiment.lastTick=$production.capturedAtGameTick
            $null=& $RecordEvidence ([pscustomobject]@{event='production-sample';index=$experiment.samples;runId=$beginMarker.runId;firstOrdinal=$beginMarker.ordinal+1;lastOrdinal=$endMarker.ordinal;window=$window;valid=$valid;resetReason=$resetReason;coveredGameTicks=$experiment.covered;rates=$experiment.lastRates;healthScope='sampled_only'})
            if (($Mode -ceq 'independent' -and $experiment.qualifying -ge $RequiredWindows) -or ($Mode -ceq 'continuous' -and $experiment.covered -ge $RequiredContinuousGameTicks)) { break }
        }
        $final=& $read get_session_state @{}
        Assert-SpherewrightStageSession $final $SessionId $PlanetId $GameVersion
        if ($final.revision -ne $initial.revision -or $final.gameTick -lt $experiment.lastTick) { throw 'Final experiment identity/revision/tick boundary changed.' }
        $completed=($Mode -ceq 'independent' -and $experiment.qualifying -ge $RequiredWindows) -or ($Mode -ceq 'continuous' -and $experiment.covered -ge $RequiredContinuousGameTicks)
        $summary=[pscustomobject]@{event='production-experiment-result';result=$(if($completed){'sampling_completed'}else{'not_proven'});samples=$experiment.samples;qualifyingWindows=$experiment.qualifying;coveredGameTicks=$experiment.covered;resetCount=$experiment.resets;requests=$experiment.requests;firstObservedTick=$experiment.firstTick;lastObservedTick=$experiment.lastTick;lastRates=$experiment.lastRates;gameWrites=0;modelDecisionsInsideLoop=0;governorAcceptance=$false;healthScope='sampled_only';timingMs=@{reads=[math]::Round($experiment.readWallMs,3);scheduledWait=$experiment.pollWaitMs;total=((Get-Date)-$started).TotalMilliseconds}}
        $null=& $RecordEvidence $summary
        return $summary
    } catch {
        $samplingError=$_
        $failureKind=$samplingError.Exception.Data['spherewrightSamplingFailureKind']
        if (-not $failureKind) { $failureKind='observation_failed' }
        $samplingError.Exception.Data['spherewrightSamplingFailureKind']=$failureKind
        $samplingError.Exception.Data['spherewrightSamplingRequests']=$experiment.requests
        $samplingError.Exception.Data['spherewrightSamplingSamples']=$experiment.samples
        $samplingError.Exception.Data['spherewrightSamplingDeadlineUtc']=$deadline.ToUniversalTime().ToString('o')
        $samplingError.Exception.Data['spherewrightGameWrites']=0
        # Keep completed sample receipts even when a later read fails. Failure is
        # not a production verdict, and never starts another experiment/worker.
        try {
            $null=& $RecordEvidence ([pscustomobject]@{event='production-experiment-failed';result='not_proven';failureKind=$failureKind;method=$experiment.lastMethod;message=$samplingError.Exception.Message;samples=$experiment.samples;qualifyingWindows=$experiment.qualifying;requests=$experiment.requests;maximumRequests=$MaximumRequests;lastObservedTick=$experiment.lastTick;coveredGameTicks=$experiment.covered;deadlineUtc=$deadline.ToUniversalTime().ToString('o');gameWrites=0;governorAcceptance=$false;automaticRestart=$false;timingMs=@{reads=[math]::Round($experiment.readWallMs,3);scheduledWait=$experiment.pollWaitMs;total=((Get-Date)-$started).TotalMilliseconds}})
        } catch {
            $samplingError.Exception.Data['spherewrightFailureEvidenceError']=$_.Exception.Message
        }
        throw $samplingError
    }
}
