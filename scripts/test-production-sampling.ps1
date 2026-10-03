$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightStageTools.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightProductionSampling.ps1')
$script:checks=0
function Assert-Sampling([bool]$Condition,[string]$Name){if(-not $Condition){throw "Sampling regression: $Name"};$script:checks++}
function Reset-Sampling([long[]]$Ends,[string]$Fault='',[int[]]$ItemIds=@(1109)){
    $script:ends=$Ends;$script:sampleIndex=0;$script:fault=$Fault;$script:itemIds=@($ItemIds);$script:entryBoundaryPending=$false;$script:calls=[Collections.Generic.List[string]]::new();$script:payloads=[Collections.Generic.List[object]]::new();$script:records=[Collections.Generic.List[object]]::new();$script:now=[datetime]'2026-09-30T00:00:00Z';$script:startTime=$script:now
}
function Get-Date{$script:now}
function Start-Sleep([int]$Milliseconds){$script:now=$script:now.AddMilliseconds($Milliseconds)}
function Invoke-SpherewrightBridgeRequest([string]$Method,[string]$SessionId,[hashtable]$Payload){
    $script:calls.Add($Method)
    $script:payloads.Add([pscustomobject]@{method=$Method;payload=$Payload})
    if($Method -like 'prepare_*' -or $Method -like 'commit_*'){throw 'A read-only sampler attempted a write'}
    $index=[math]::Min($script:sampleIndex,$script:ends.Count-1);$tick=$script:ends[$index]
    if($script:fault -ceq '15fps'){$tick=[long](($script:now-$script:startTime).TotalSeconds*15)}
    $result=switch($Method){
        get_session_state {
            if($script:entryBoundaryPending){if($script:fault -cne '15fps'){$tick=if($script:sampleIndex -eq 0){0}else{$script:ends[$script:sampleIndex-1]}};$script:entryBoundaryPending=$false}
            if($script:fault -ceq 'paused' -and $index -gt 0){$tick=$script:ends[0]}
            [pscustomobject]@{sessionId=$SessionId;localPlanetId=104;gameVersion=$(if($script:fault -ceq 'version'){'other'}else{'fixture'});gameLoaded=$true;ownedBySpherewright=$true;accessRestricted=$false;writeHealth=$(if($script:fault -ceq 'quarantine'){'quarantined'}else{'healthy'});writeBlockers=@();gameTick=$tick;revision=22}
        }
        inspect_factory_entity {[pscustomobject]@{sessionId=$SessionId;planetId=104;objectId=$Payload.objectId;recipeId=17;buffers=@([pscustomobject]@{role='input';itemId=1006;count=4})}}
        get_power_summary {[pscustomobject]@{sessionId=$SessionId;planetId=104;networks=@([pscustomobject]@{consumerRatio=$(if($script:fault -ceq 'power' -and $index -eq 1){0.5}else{1.0})})}}
        get_overseer_production {
            if($script:fault -ceq 'deadline'){$script:now=$script:now.AddSeconds(200)}
            $script:sampleIndex++
            $state=if($script:fault -ceq 'warming' -and $index -eq 0){'warming_up'}else{'ready'}
            $returnedItemIds=@($script:itemIds)
            if($script:fault -ceq 'incompleteProduction'){$returnedItemIds=@($returnedItemIds|Select-Object -First ($returnedItemIds.Count-1))}
            $productionRows=@(foreach($itemId in $returnedItemIds){[pscustomobject]@{itemId=$itemId;itemName="fixture item $itemId";producedCount=5;consumedCount=4;actualProductionPerMinute=30.0;actualConsumptionPerMinute=24.0}})
            [pscustomobject]@{sessionId=$SessionId;capturedAtGameTick=$tick;totalFactoryCount=1;returnedFactoryCount=1;requestedItemIds=@($script:itemIds);nextCursor=$(if($script:fault -ceq 'cursor'){'unread-page'}else{$null});window=[pscustomobject]@{state=$state;startGameTick=$tick-599;endGameTick=$tick;elapsedGameTicks=600;crossedSessionBoundary=$false};planets=@(if($script:fault -cne 'localCoverage'){[pscustomobject]@{planetId=104;production=$productionRows}})}
        }
        default{throw "Unexpected read $Method"}
    }
    [pscustomobject]@{success=$true;result=$result}
}
$arguments=@{SessionId='fixture-session';PlanetId=104;GameVersion='fixture';EntityIds=@(3404);ItemIds=@(1109);ValidateObservation={param($s) $s.entities[0].recipeId -eq 17 -and $s.power.networks[0].consumerRatio -eq 1 -and $s.production.planets[0].production[0].actualProductionPerMinute -gt 0};ReceiptMarker={[pscustomobject]@{runId='aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa';ordinal=$script:calls.Count}};RecordEvidence={param($row) $script:records.Add($row);if($row.event -ceq 'production-experiment-intent'){$script:entryBoundaryPending=$true}}}
Reset-Sampling @(600,1206,1812)
$result=Invoke-SpherewrightProductionExperiment @arguments
Assert-Sampling ($result.result -ceq 'sampling_completed' -and $result.qualifyingWindows -eq 3 -and $result.requests -eq 14) 'three fixed independent windows run in one bounded entry'
Assert-Sampling ($result.gameWrites -eq 0 -and $result.modelDecisionsInsideLoop -eq 0 -and -not $result.governorAcceptance) 'no writes or in-loop model decisions, no Governor sign-off'
Assert-Sampling (@($script:records|Where-Object event -EQ 'production-sample').Count -eq 3 -and $script:records[1].firstOrdinal -eq 2 -and $script:records[1].lastOrdinal -eq 5) 'all samples link to original read ranges'
$batchedWindows=@($script:records|Where-Object event -EQ 'production-sample'|ForEach-Object { $_.window|ConvertTo-Json -Compress })

$wideIds=@(1101..1112)
$wide=@{};foreach($key in $arguments.Keys){$wide[$key]=$arguments[$key]};$wide.ItemIds=$wideIds
Reset-Sampling @(600,1206,1812) '' $wideIds
$wideResult=Invoke-SpherewrightProductionExperiment @wide
$wideProductionCalls=@($script:payloads|Where-Object method -CEQ 'get_overseer_production')
$wideQueriesValid=$wideProductionCalls.Count -eq 3 -and @($wideProductionCalls|Where-Object { $_.payload.limit -ne 8 -or (@($_.payload.itemIds|Sort-Object) -join ',') -cne (@($wideIds|Sort-Object) -join ',') }).Count -eq 0
Assert-Sampling ($wideResult.result -ceq 'sampling_completed' -and $wideResult.samples -eq 3 -and $wideResult.qualifyingWindows -eq 3 -and $wideResult.requests -eq 14) 'twelve source items qualify across three native windows with the unchanged request budget'
Assert-Sampling ($wideQueriesValid -and @($script:records|Where-Object event -EQ 'production-sample'|Where-Object { $_.rates.Count -eq 12 }).Count -eq 3) 'all three reads request the complete twelve-item set and retain every returned rate'
Assert-Sampling ($wideResult.gameWrites -eq 0 -and $script:calls.Count -eq 14 -and @($script:calls|Where-Object {$_ -like 'prepare_*' -or $_ -like 'commit_*'}).Count -eq 0) 'twelve-item observation stays read-only within fourteen requests'

$chainIds=@(1101..1115)
$chain=@{};foreach($key in $arguments.Keys){$chain[$key]=$arguments[$key]};$chain.EntityIds=@(1..32);$chain.ItemIds=$chainIds;$chain.RequiredWindows=1;$chain.MaximumSamples=1;$chain.MaximumRequests=40
Reset-Sampling @(600) '' $chainIds
$chainResult=Invoke-SpherewrightProductionExperiment @chain
$chainProductionCalls=@($script:payloads|Where-Object method -CEQ 'get_overseer_production')
$chainEntityCalls=@($script:payloads|Where-Object method -CEQ 'inspect_factory_entity')
$chainIntent=@($script:records|Where-Object event -EQ 'production-experiment-intent')
Assert-Sampling ($chainResult.result -ceq 'sampling_completed' -and $chainResult.samples -eq 1 -and $chainResult.qualifyingWindows -eq 1 -and $chainResult.requests -eq 37 -and $chainIntent.Count -eq 1 -and $chainIntent[0].maximumRequests -eq 40) 'fifteen items and thirty-two entities fit one window within the explicit forty-request budget'
Assert-Sampling ($chainEntityCalls.Count -eq 32 -and $chainProductionCalls.Count -eq 1 -and (@($chainProductionCalls[0].payload.itemIds|Sort-Object) -join ',') -ceq (@($chainIds|Sort-Object) -join ',') -and @($script:records|Where-Object event -EQ 'production-sample'|Where-Object { $_.rates.Count -eq 15 }).Count -eq 1) 'full fixed entity/item scope is covered in one complete fifteen-item response'
Assert-Sampling ($chainResult.gameWrites -eq 0 -and $chainResult.modelDecisionsInsideLoop -eq 0 -and @($script:calls|Where-Object {$_ -like 'prepare_*' -or $_ -like 'commit_*'}).Count -eq 0) 'bounded thirty-seven-request fixture remains read-only with no model loop'

$boundaryIds=@(1101..1116)
$boundary=@{};foreach($key in $arguments.Keys){$boundary[$key]=$arguments[$key]};$boundary.ItemIds=$boundaryIds;$boundary.RequiredWindows=1;$boundary.MaximumSamples=1
Reset-Sampling @(600) '' $boundaryIds
$boundaryResult=Invoke-SpherewrightProductionExperiment @boundary
$boundaryProduction=@($script:payloads|Where-Object method -CEQ 'get_overseer_production')
Assert-Sampling ($boundaryResult.result -ceq 'sampling_completed' -and $boundaryResult.samples -eq 1 -and $boundaryResult.requests -eq 6 -and $boundaryProduction.Count -eq 1 -and @($script:records|Where-Object event -EQ 'production-sample'|Where-Object { $_.rates.Count -eq 16 }).Count -eq 1) 'sixteen-item caller boundary succeeds as one complete source-item response'

foreach($scopeCase in @(
    [pscustomobject]@{name='seventeen items';ids=[int[]](1101..1117)},
    [pscustomobject]@{name='duplicate item id';ids=[int[]]@(1109,1109)},
    [pscustomobject]@{name='nonpositive item id';ids=[int[]]@(0)}
)){
    Reset-Sampling @(600,1206,1812) '' $scopeCase.ids
    $badScope=@{};foreach($key in $arguments.Keys){$badScope[$key]=$arguments[$key]};$badScope.ItemIds=$scopeCase.ids
    $scopeError=$null;try{Invoke-SpherewrightProductionExperiment @badScope|Out-Null}catch{$scopeError=$_}
    Assert-Sampling ($null -ne $scopeError -and $script:calls.Count -eq 0) "$($scopeCase.name) rejects before any request"
}

Reset-Sampling @(600) '' @(1109)
$tooManyEntities=@{};foreach($key in $arguments.Keys){$tooManyEntities[$key]=$arguments[$key]};$tooManyEntities.EntityIds=@(1..33)
$entityScopeError=$null;try{Invoke-SpherewrightProductionExperiment @tooManyEntities|Out-Null}catch{$entityScopeError=$_}
Assert-Sampling ($null -ne $entityScopeError -and $script:calls.Count -eq 0) 'thirty-three entity scope rejects before any request'

$incomplete=@{};foreach($key in $arguments.Keys){$incomplete[$key]=$arguments[$key]};$incomplete.ItemIds=$chainIds;$incomplete.RequiredWindows=1;$incomplete.MaximumSamples=1;$incomplete.MaximumRequests=40;$incomplete.ValidateObservation={param($sample) $true}
Reset-Sampling @(600) 'incompleteProduction' $chainIds
$incompleteError=$null;try{Invoke-SpherewrightProductionExperiment @incomplete|Out-Null}catch{$incompleteError=$_}
$incompleteFailures=@($script:records|Where-Object event -EQ 'production-experiment-failed')
Assert-Sampling ($null -ne $incompleteError -and $incompleteError.Exception.Data['spherewrightSamplingSamples'] -eq 0 -and $incompleteError.Exception.Data['spherewrightSamplingRequests'] -eq 5) 'incomplete fifteen-item response fails without sample credit'
Assert-Sampling (@($script:records|Where-Object event -EQ 'production-sample').Count -eq 0 -and $incompleteFailures.Count -eq 1 -and $incompleteFailures[0].samples -eq 0 -and $incompleteFailures[0].qualifyingWindows -eq 0) 'incomplete source-item coverage cannot advance samples or qualification'

# Same three native-window fixtures, split into three scheduler entries. This is
# an offline caller comparison, NOT three measured model calls or token savings.
Reset-Sampling @(600,1206,1812)
$single=@{};foreach($key in $arguments.Keys){$single[$key]=$arguments[$key]};$single.RequiredWindows=1;$single.MaximumSamples=1
for($i=0;$i -lt 3;$i++){ $null=Invoke-SpherewrightProductionExperiment @single }
$splitRequests=$script:calls.Count
$splitWindows=@($script:records|Where-Object event -EQ 'production-sample'|ForEach-Object { $_.window|ConvertTo-Json -Compress })
Assert-Sampling ($splitRequests -eq 18 -and ($splitWindows -join '|') -ceq ($batchedWindows -join '|')) 'three entries versus one retain identical native windows, avoid four duplicate boundary reads'

# Same ten entities, 15 ticks/second, 180 seconds and 90-read cap. A fast
# polling caller burns its cap; changing cadence, not caps, completes offline.
$slow=@{};foreach($key in $arguments.Keys){$slow[$key]=$arguments[$key]};$slow.EntityIds=@(1..10)
Reset-Sampling @(600,1206,1812) '15fps'
$slow.PollSeconds=1
$slowError=$null;try{Invoke-SpherewrightProductionExperiment @slow|Out-Null}catch{$slowError=$_}
Assert-Sampling ($null -ne $slowError -and $slowError.Exception.Data['spherewrightSamplingFailureKind'] -ceq 'request_budget_exhausted' -and $script:calls.Count -eq 90) '15fps one-second polling hits the original hard request cap'
$slowFailure=@($script:records|Where-Object event -EQ 'production-experiment-failed')
Assert-Sampling ($slowFailure.Count -eq 1 -and $slowFailure[0].samples -eq 1 -and $slowFailure[0].requests -eq 90 -and $slowFailure[0].result -ceq 'not_proven' -and -not $slowFailure[0].automaticRestart) 'failure persists the actual cap and one completed sample, not a game verdict or restart'
Assert-Sampling (@($script:records|Where-Object event -EQ 'production-sample').Count -eq 1 -and $slowError.Exception.Data['spherewrightSamplingSamples'] -eq 1) 'a later budget failure cannot erase a completed original window'
$slow.Remove('PollSeconds')
Reset-Sampling @(600,1206,1812) '15fps'
$slowResult=Invoke-SpherewrightProductionExperiment @slow
Assert-Sampling ($slowResult.result -ceq 'sampling_completed' -and $slowResult.samples -eq 3 -and $slowResult.requests -lt 90 -and $slowResult.timingMs.total -le 180000) 'five-second default completes at 15fps within unchanged time/read caps'
Assert-Sampling ($script:records[0].pollSeconds -eq 5 -and $slowResult.gameWrites -eq 0 -and $slowResult.modelDecisionsInsideLoop -eq 0) 'cadence is declared before reads with no model loop or writes'

# Identical15fps virtual-clock fixtures: a30-minute cap cannot cover ten
# game minutes. Declare a finite45-minute task BEFORE it starts, not a retry
# or extension of any live experiment. Existing sample/read caps stay bounded.
$slowContinuous=@{};foreach($key in $arguments.Keys){$slowContinuous[$key]=$arguments[$key]}
$slowContinuous.EntityIds=@(1..15);$slowContinuous.Mode='continuous';$slowContinuous.MaximumSamples=120
$slowContinuous.RequiredContinuousGameTicks=36000;$slowContinuous.IntervalGameTicks=330
$slowContinuous.PollSeconds=2;$slowContinuous.MaximumRequests=4096;$slowContinuous.TimeoutSeconds=1800
Reset-Sampling @(600) '15fps'
$slowContinuousError=$null;try{Invoke-SpherewrightProductionExperiment @slowContinuous|Out-Null}catch{$slowContinuousError=$_}
$slowContinuousFailure=@($script:records|Where-Object event -EQ 'production-experiment-failed')
Assert-Sampling ($null -ne $slowContinuousError -and $slowContinuousError.Exception.Data['spherewrightSamplingFailureKind'] -in @('deadline_exhausted','read_crossed_deadline')) '15fps36000-tick task stops at its original1800-second deadline'
Assert-Sampling ($slowContinuousFailure.Count -eq 1 -and -not $slowContinuousFailure[0].automaticRestart) 'slow continuous deadline failure is retained without extending or replaying it'
$slowContinuous.TimeoutSeconds=2700
Reset-Sampling @(600) '15fps'
$slowContinuousResult=Invoke-SpherewrightProductionExperiment @slowContinuous
Assert-Sampling ($slowContinuousResult.result -ceq 'sampling_completed' -and $slowContinuousResult.coveredGameTicks -ge 36000 -and $slowContinuousResult.resetCount -eq 0) 'predeclared2700-second finite task covers36000 contiguous ticks at15fps'
Assert-Sampling ($slowContinuousResult.timingMs.total -ge 2400000 -and $slowContinuousResult.timingMs.total -le 2700000 -and $slowContinuousResult.requests -le 4096 -and $slowContinuousResult.samples -le 120) 'slow continuous fixture obeys initial wall/read/sample bounds'
Assert-Sampling ($slowContinuousResult.gameWrites -eq 0 -and $slowContinuousResult.modelDecisionsInsideLoop -eq 0 -and -not $slowContinuousResult.governorAcceptance) 'longer initial budget grants no mutations,model loop or production sign-off'
Reset-Sampling @(600)
$tooLong=@{};foreach($key in $arguments.Keys){$tooLong[$key]=$arguments[$key]};$tooLong.TimeoutSeconds=3601
$tooLongError=$null;try{Invoke-SpherewrightProductionExperiment @tooLong|Out-Null}catch{$tooLongError=$_}
Assert-Sampling ($null -ne $tooLongError -and $script:calls.Count -eq 0) 'over3600-second declaration rejects before any request'

$continuous=@{};foreach($key in $arguments.Keys){$continuous[$key]=$arguments[$key]};$continuous.Mode='continuous';$continuous.IntervalGameTicks=600;$continuous.MaximumSamples=60;$continuous.MaximumRequests=300
Reset-Sampling @(1..60|ForEach-Object {$_*600})
$result=Invoke-SpherewrightProductionExperiment @continuous
Assert-Sampling ($result.coveredGameTicks -eq 36000 -and $result.samples -eq 60 -and $result.requests -eq 242 -and $result.resetCount -eq 0) 'only a contiguous 36000-tick fixture completes continuous sampling'
Assert-Sampling ($result.healthScope -ceq 'sampled_only' -and -not $result.governorAcceptance) 'does not overclaim health/attribution/declaration validation'
$continuous.MaximumSamples=3
Reset-Sampling @(600,1206,1806)
$result=Invoke-SpherewrightProductionExperiment @continuous
Assert-Sampling ($result.result -ceq 'not_proven' -and $result.resetCount -eq 1 -and $result.coveredGameTicks -eq 1200) 'gap resets credit instead of joining separated windows'
Reset-Sampling @(600,1200,1800) power
$result=Invoke-SpherewrightProductionExperiment @continuous
Assert-Sampling ($result.coveredGameTicks -eq 600 -and $result.resetCount -eq 1 -and $result.result -ceq 'not_proven') 'failed predeclared power condition resets continuity'
Reset-Sampling @(600,1206,1812) warming
$result=Invoke-SpherewrightProductionExperiment @arguments
Assert-Sampling ($result.result -ceq 'not_proven' -and $result.qualifyingWindows -eq 2) 'warming window is not an independent production proof'

foreach($fault in @('cursor','version','quarantine','deadline','paused','budget')){
    Reset-Sampling @(600,1206,1812) $fault
    $limited=@{};foreach($key in $arguments.Keys){$limited[$key]=$arguments[$key]}
    if($fault -ceq 'paused'){$limited.TimeoutSeconds=3}
    if($fault -ceq 'budget'){$limited.MaximumRequests=3}
    $errorRecord=$null;try{Invoke-SpherewrightProductionExperiment @limited|Out-Null}catch{$errorRecord=$_}
    Assert-Sampling ($null -ne $errorRecord -and @($script:calls|Where-Object {$_ -like 'prepare_*' -or $_ -like 'commit_*'}).Count -eq 0) "$fault stops without mutations or replay"
    if($fault -ceq 'deadline'){Assert-Sampling ($script:calls.Count -eq 5 -and $errorRecord.Exception.Data['spherewrightSamplingRequests'] -eq 5) 'read crossing original deadline cannot extend observation'}
    if($fault -ceq 'budget'){Assert-Sampling ($script:calls.Count -eq 3 -and $errorRecord.Exception.Data['spherewrightSamplingFailureKind'] -ceq 'request_budget_exhausted') 'hard request cap with exact failure kind'}
    if($fault -in @('version','quarantine')){Assert-Sampling (@($script:records|Where-Object event -EQ 'production-experiment-failed').Count -eq 1) 'initial identity/health failure is retained too'}
}
Reset-Sampling @(600,1206,1812)
$evidenceFailure=@{};foreach($key in $arguments.Keys){$evidenceFailure[$key]=$arguments[$key]};$evidenceFailure.MaximumRequests=3
$evidenceFailure.RecordEvidence={param($row) if($row.event -ceq 'production-experiment-failed'){throw 'fixture evidence write failed'};$script:records.Add($row);if($row.event -ceq 'production-experiment-intent'){$script:entryBoundaryPending=$true}}
$evidenceError=$null;try{Invoke-SpherewrightProductionExperiment @evidenceFailure|Out-Null}catch{$evidenceError=$_}
Assert-Sampling ($evidenceError.Exception.Data['spherewrightSamplingFailureKind'] -ceq 'request_budget_exhausted' -and $evidenceError.Exception.Data['spherewrightFailureEvidenceError'] -ceq 'fixture evidence write failed' -and $script:calls.Count -eq 3) 'failure-record persistence error cannot replace the original sampling failure or trigger new reads'
Reset-Sampling @(600,1206,1812) 'localCoverage'
$coverage=@{};foreach($key in $arguments.Keys){$coverage[$key]=$arguments[$key]};$coverage.ValidateObservation={param($sample) $true}
$coverageError=$null;try{Invoke-SpherewrightProductionExperiment @coverage|Out-Null}catch{$coverageError=$_}
Assert-Sampling ($coverageError.Exception.Message -ceq 'Local item-rate coverage unproved.' -and $coverageError.Exception.Data['spherewrightSamplingSamples'] -eq 0 -and @($script:records|Where-Object event -EQ 'production-sample').Count -eq 0) 'incomplete local rates cannot advance completed-sample or qualification counts'
Reset-Sampling @(600,1206,1812)
$bad=@{};foreach($key in $arguments.Keys){$bad[$key]=$arguments[$key]};$bad.EntityIds=@(3404,3404)
try{Invoke-SpherewrightProductionExperiment @bad|Out-Null}catch{}
Assert-Sampling ($script:calls.Count -eq 0) 'invalid scope rejects before the first request'
[pscustomobject]@{passed=$script:checks;gameCalls=0;independentFixtureRequestsBefore=$splitRequests;independentFixtureRequestsAfter=14;entryInvocationsBefore=3;entryInvocationsAfter=1;slowFixtureRequestsBefore=90;slowFixtureCompletedBefore=$false;slowFixtureRequestsAfter=$slowResult.requests;slowFixtureWallMsAfter=$slowResult.timingMs.total;slowFixtureCompletedAfter=$true;continuousFixtureRequests=242;continuousFixtureTicks=36000;providerUsage=$null}|ConvertTo-Json -Compress
