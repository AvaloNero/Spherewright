# Offline DTO/transport fixtures only. They do not simulate DSP placement.
$ErrorActionPreference = 'Stop'
$script:methods = [Collections.Generic.List[string]]::new()
$script:allMethods = [Collections.Generic.List[string]]::new()
. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightStageTools.ps1')
if ($script:methods.Count) { throw 'Import dispatched a request.' }
$script:checks = 1
function Assert-Qualification([bool]$Condition,[string]$Name) {
    if (-not $Condition) { throw "Belt qualification regression: $Name" }
    $script:checks++
}
function Fixture-Vector([double]$x,[double]$y,[double]$z) { [pscustomobject]@{x=$x;y=$y;z=$z} }
function Fixture-Plan([int]$Count=4) {
    $a=Fixture-Vector 0 200 0; $b=Fixture-Vector 5 204 0
    $c=Fixture-Vector 25 204 0; $d=Fixture-Vector 30 200 0; $m=Fixture-Vector 15 204 0
    $spans=@(
        [pscustomobject]@{label='D';preferredPosition=$c;pathEnd=$d;beltStartAltitudeLevel=3;beltEndAltitudeLevel=0;endpointPreviewRole='destination'},
        [pscustomobject]@{label='A';preferredPosition=$a;pathEnd=$b;beltStartAltitudeLevel=0;beltEndAltitudeLevel=3;endpointPreviewRole='source'})
    if ($Count -eq 3) {
        $spans += [pscustomobject]@{label='H';preferredPosition=$b;pathEnd=$c;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
    } else {
        $spans += [pscustomobject]@{label='H1';preferredPosition=$b;pathEnd=$m;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
        $spans += [pscustomobject]@{label='H2';preferredPosition=$m;pathEnd=$c;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
    }
    [pscustomobject]@{gameCommitAllowed=$false;candidateCountPerInterface=1;maximumRequests=(6+2*$Count);maximumWallSeconds=180;
        expectedSessionId='fixture-session';expectedGameVersion='fixture-version';expectedSaveGameTick=1000;sorterItemId=2011;filterItemId=1109;
        commonPayload=[pscustomobject]@{planetId=104;buildingItemId=2001;stateHashVersion=1;beltPathMode='native_elevated_grid';expectedPlayerStateHash='placeholder'};
        sourceEndpoint=[pscustomobject]@{existingObjectId=870;existingSlot=9;existingBeltQuarterTurns=0;plannedBeltQuarterTurns=1;expectedSlotPosition=(Fixture-Vector -1 200 0)};
        destinationEndpoint=[pscustomobject]@{existingObjectId=5334;existingSlot=7;existingBeltQuarterTurns=0;plannedBeltQuarterTurns=0;expectedSlotPosition=(Fixture-Vector 31 200 0)};
        spans=$spans}
}
function Reset-Qualification([string]$Mode='',[int]$Count=4) {
    $script:methods.Clear();$script:mode=$Mode;$script:prepareCount=0;$script:playerCount=0;$script:sessionCount=0
    $script:plan=Fixture-Plan $Count;$script:evidence=[Collections.Generic.List[object]]::new()
}
function Get-LiveSpherewrightDescriptor { throw 'Offline fixture must never discover a live descriptor.' }
function Invoke-SpherewrightBridgeRequest([string]$Method,[string]$SessionId,[hashtable]$Payload) {
    if ($Method -cnotin @('get_session_state','get_player_state','inspect_factory_entity','prepare_build','get_gameplay_journal')) { throw 'Unexpected write/lifecycle method.' }
    if ($SessionId -cne 'fixture-session') { throw 'Incorrect session.' }
    $script:methods.Add($Method)
    $script:allMethods.Add($Method)
    $result=switch($Method) {
        get_session_state {
            $script:sessionCount++
            [pscustomobject]@{sessionId=$SessionId;localPlanetId=104;gameVersion='fixture-version';gameLoaded=$true;ownedBySpherewright=$true;
                accessRestricted=($script:mode-eq'restricted');writeHealth=$(if($script:mode-eq'quarantine'){'quarantined'}else{'healthy'});
                writeBlockers=@();gameTick=1100;revision=$(if($script:mode-eq'revision' -and $script:sessionCount-gt1){8}else{7});lastOwnedSaveGameTick=1000}
        }
        get_player_state {
            $script:playerCount++
            if ($Payload.planetId-ne104) { throw 'Missing explicit player planet.' }
            [pscustomobject]@{sessionId=$SessionId;planetId=104;stateHash='fresh-player';movementState='Walk';speed=0.0;coreEnergy=1600000000;
                inventory=@([pscustomobject]@{itemId=2001;count=$(if($script:mode-eq'player_change' -and $script:playerCount-gt$script:plan.spans.Count){99}else{100})})}
        }
        inspect_factory_entity {
            $role=if($Payload.objectId-eq870){'source'}else{'destination'};$binding=$script:plan.($role+'Endpoint')
            [pscustomobject]@{sessionId=$SessionId;planetId=104;objectId=$(if($script:mode-eq'wrong_id'){999}else{$binding.existingObjectId});
                endpointStateHash=('endpoint-'+$role);sorterEndpoints=[pscustomobject]@{endpoints=@([pscustomobject]@{slot=$binding.existingSlot;occupied=($script:mode-eq'occupied');otherObjectId=0;position=$binding.expectedSlotPosition})}}
        }
        prepare_build {
            $script:prepareCount++;$span=$script:plan.spans[$script:prepareCount-1]
            if ($script:methods[$script:methods.Count-2]-cne'get_player_state' -or $Payload.expectedPlayerStateHash-cne'fresh-player') { throw 'Not a fresh per-span player binding.' }
            if ($script:mode-eq'bridge_error') { return [pscustomobject]@{success=$false;error=[pscustomobject]@{code='BUILD_LOCATION_INVALID';message='fixture planned point9 overlaps2459';retryable=$false;recovery='Do not retry same site.'}} }
            $path=@($Payload.preferredPosition,(Fixture-Vector 8 204 0),(Fixture-Vector 20 204 0),$Payload.pathEnd)
            if ($script:mode-eq'polyline' -and $span.endpointPreviewRole-eq$null) { $path[1]=Fixture-Vector 5 224 0;$path[2]=Fixture-Vector 25 224 0 }
            if ($script:mode-eq'join' -and $span.label-eq'H2') { $path[0]=Fixture-Vector 15.03 204 0 }
            $budget=@([pscustomobject]@{itemId=2001;count=$(if($script:mode-eq'budget'){5}else{4})})
            $preview=$null;$prepared=$true;$token='PRIVATE-FIXTURE-TOKEN'
            $role=$span.endpointPreviewRole
            if($null-ne$role) {
                $binding=$script:plan.($role+'Endpoint');$requestBinding=$Payload.beltEndpointPreview[$role]
                if($requestBinding.existingObjectId-ne$binding.existingObjectId -or $requestBinding.expectedEndpointStateHash-cne('endpoint-'+$role)) { throw 'Wrong role/hash binding.' }
                $attachment=[pscustomobject]@{role=$role;existingObjectId=$binding.existingObjectId;endpointStateHash=$(if($script:mode-eq'endpoint_hash'){'wrong'}else{'endpoint-'+$role});
                    filterItemId=1109;sorterItemId=2011;plannedBeltQuarterTurns=$binding.plannedBeltQuarterTurns;nativeCondition='Ok';nativeSpan=2;
                    attachment=[pscustomobject]@{sourceSlot=$(if($role-eq'source'){$binding.existingSlot}else{-1});destinationSlot=$(if($role-eq'destination'){$binding.existingSlot}else{-1})}}
                $blocked=$script:mode-eq'endpoint_blocker'
                $preview=[pscustomobject]@{nativeCheckPerformed=(-not$blocked);nativeCheckPassed=(-not$blocked);executable=$false;
                    blockers=$(if($blocked){@('planned_endpoint_collision_or_prototype_unavailable')}else{@()});attachments=@($attachment)}
                $prepared=$false;$token='';$budget+=[pscustomobject]@{itemId=2011;count=1}
            }
            [pscustomobject]@{prepared=$prepared;commitAllowedNow=$prepared;planToken=$token;commitBlockers=@();plannedPath=$path;itemBudget=$budget;beltEndpointPreview=$preview;
                plannedBeltPath=[pscustomobject]@{newObjectCount=4;nativeValidationMode='full_path_stage1';routingMode='native_elevated_grid';startAltitudeLevel=$span.beltStartAltitudeLevel;endAltitudeLevel=$span.beltEndAltitudeLevel}}
        }
        get_gameplay_journal {
            [pscustomobject]@{sessionId=$SessionId;entries=@([pscustomobject]@{sequence=1});durableThroughSequence=1;persistencePending=($script:mode-eq'journal');persistenceError=$null}
        }
    }
    [pscustomobject]@{success=$true;result=$result}
}
function Run-Qualification([int]$Accepted=10) {
    Invoke-SpherewrightBeltSiteQualification -ApprovedPlan $script:plan -ExpectedRevision 7 -AcceptedBefore $Accepted -MinimumDurableSequence 1 -RecordEvidence {param($row)$script:evidence.Add($row)}
}
foreach($count in @(3,4)) {
    Reset-Qualification -Count $count
    $summary=Run-Qualification
    Assert-Qualification ($summary.result-ceq'qualified_sites_only' -and $summary.spans.Count-eq$count) "$count fixed spans work without rewriting a caller"
    Assert-Qualification ($summary.bridgeRequests-eq(6+2*$count) -and $script:methods.Count-eq$summary.bridgeRequests) "$count bounded requests including closure"
    Assert-Qualification ($script:prepareCount-eq$count -and $script:playerCount-eq($count+1)) "$count one prepare and fresh player per span"
    Assert-Qualification ($summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq10 -and $summary.closure.revision-eq7 -and $summary.closure.savedTick-eq1000) 'preserves ten-write freeze, revision and save'
    Assert-Qualification (-not$summary.futureActualIdJoinProven -and -not$summary.wholePlanExecutable -and $summary.doNotReplay) 'site previews never mean construction approval'
    Assert-Qualification (($summary|ConvertTo-Json -Depth 30)-notmatch'PRIVATE-FIXTURE-TOKEN' -and ($script:evidence|ConvertTo-Json -Depth 30)-notmatch'PRIVATE-FIXTURE-TOKEN') 'ordinary prepare tokens never enter returned or caller evidence'
}
Reset-Qualification -Mode polyline -Count 3
$summary=Run-Qualification
Assert-Qualification ($summary.result-ceq'qualified_sites_only') 'native endpoint chord, not longer fixture polyline, is the30m bound'
foreach($mode in @('endpoint_blocker','bridge_error','endpoint_hash','budget')) {
    Reset-Qualification -Mode $mode
    $summary=Run-Qualification 3
    Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq1 -and $script:methods.Count-eq8) "$mode stops remaining spans and closes without replay"
    Assert-Qualification ($summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq3 -and $null-ne$summary.closure) "$mode retains counts and proved closure"
    if($mode-eq'bridge_error'){Assert-Qualification ($summary.failure.code-ceq'BUILD_LOCATION_INVALID') 'preserves concrete bridge error code'}
    if($mode-eq'endpoint_blocker'){Assert-Qualification ($summary.failure.blockers-contains'planned_endpoint_collision_or_prototype_unavailable' -and -not$summary.failure.nativeCheckPerformed) 'pre-native blocker is not a native pass'}
}
foreach($mode in @('wrong_id','occupied','restricted','quarantine')) {
    Reset-Qualification -Mode $mode
    $summary=Run-Qualification
    Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq0) "$mode prevents any prepare"
    if($mode-in@('restricted','quarantine')){Assert-Qualification ($script:methods.Count-eq1) "$mode reads no user world entities"}
}
foreach($mode in @('join','journal','revision','player_change')) {
    Reset-Qualification -Mode $mode
    $summary=Run-Qualification
    Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq4 -and $summary.acceptedDelta-eq0) "$mode does not turn readback problems into retries/writes"
}
foreach($caseName in @('too_many','write_allowed','unsupported_common','bad_vector','request_budget')) {
    Reset-Qualification
    switch($caseName) {
        too_many {$script:plan.spans+= $script:plan.spans[-1]}
        write_allowed {$script:plan.gameCommitAllowed=$true}
        unsupported_common {$script:plan.commonPayload|Add-Member -NotePropertyName forbiddenSourceObjectId -NotePropertyValue 123}
        bad_vector {$script:plan.spans[0].preferredPosition.x=$true}
        request_budget {$script:plan.maximumRequests=13}
    }
    $caught=$false;try{Run-Qualification|Out-Null}catch{$caught=$true}
    Assert-Qualification ($caught -and $script:methods.Count-eq0) "$caseName rejects before runtime discovery or transport"
}
Assert-Qualification (@($script:allMethods|Where-Object {$_-like'commit_*'-or$_-match'save|resume|close|exit'}).Count-eq0) 'no write/lifecycle dispatch across all fixtures'
Write-Output "Belt site qualification: $script:checks checks passed; zero real game calls."
