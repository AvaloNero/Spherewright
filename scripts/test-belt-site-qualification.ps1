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
function Fixture-Plan([int]$Count=4,[switch]$VirtualEndpoints,[switch]$Ground) {
    $a=Fixture-Vector 0 200 0; $b=Fixture-Vector 5 204 0
    $c=Fixture-Vector 25 204 0; $d=Fixture-Vector 30 200 0; $m=Fixture-Vector 15 204 0
    $spans=@(
        [pscustomobject]@{label='D';preferredPosition=$c;pathEnd=$d;beltStartAltitudeLevel=3;beltEndAltitudeLevel=0;endpointPreviewRole='destination'},
        [pscustomobject]@{label='A';preferredPosition=$a;pathEnd=$b;beltStartAltitudeLevel=0;beltEndAltitudeLevel=3;endpointPreviewRole='source'})
    if ($Count -eq 3) {
        $spans += [pscustomobject]@{label='H';preferredPosition=$b;pathEnd=$c;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
    } elseif ($Count -eq 4) {
        $spans += [pscustomobject]@{label='H1';preferredPosition=$b;pathEnd=$m;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
        $spans += [pscustomobject]@{label='H2';preferredPosition=$m;pathEnd=$c;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
    } else {
        $h1=Fixture-Vector 11.6666666667 204 0; $h2=Fixture-Vector 18.3333333333 204 0
        $spans += [pscustomobject]@{label='H1';preferredPosition=$b;pathEnd=$h1;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
        $spans += [pscustomobject]@{label='H2';preferredPosition=$h1;pathEnd=$h2;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
        $spans += [pscustomobject]@{label='H3';preferredPosition=$h2;pathEnd=$c;beltStartAltitudeLevel=3;beltEndAltitudeLevel=3;endpointPreviewRole=$null}
    }
    if ($Ground) {
        foreach ($span in $spans) {
            $span.PSObject.Properties.Remove('beltStartAltitudeLevel')
            $span.PSObject.Properties.Remove('beltEndAltitudeLevel')
        }
    }
    $sourceEndpoint=[pscustomobject]@{existingObjectId=870;existingSlot=9;existingBeltQuarterTurns=0;plannedBeltQuarterTurns=1;expectedSlotPosition=(Fixture-Vector -1 200 0)}
    $destinationEndpoint=[pscustomobject]@{existingObjectId=5334;existingSlot=7;existingBeltQuarterTurns=0;plannedBeltQuarterTurns=0;expectedSlotPosition=(Fixture-Vector 31 200 0)}
    if($VirtualEndpoints) {
        $sourceEndpoint.existingSlot=-1;$sourceEndpoint.existingBeltQuarterTurns=1;$sourceEndpoint.plannedBeltQuarterTurns=2
        $destinationEndpoint.existingSlot=-1;$destinationEndpoint.existingBeltQuarterTurns=3;$destinationEndpoint.plannedBeltQuarterTurns=1
    }
    [pscustomobject]@{gameCommitAllowed=$false;candidateCountPerInterface=1;maximumRequests=(6+2*$Count);maximumWallSeconds=180;
        expectedSessionId='fixture-session';expectedGameVersion='fixture-version';expectedSaveGameTick=1000;sorterItemId=2011;filterItemId=1109;
        commonPayload=[pscustomobject]@{planetId=104;buildingItemId=2001;stateHashVersion=1;beltPathMode=$(if($Ground){'native_grid'}else{'native_elevated_grid'});expectedPlayerStateHash='placeholder'};
        sourceEndpoint=$sourceEndpoint;destinationEndpoint=$destinationEndpoint;
        spans=$spans}
}
function Reset-Qualification([string]$Mode='',[int]$Count=4,[switch]$VirtualEndpoints,[switch]$Ground) {
    $script:methods.Clear();$script:mode=$Mode;$script:prepareCount=0;$script:playerCount=0;$script:sessionCount=0
    $script:plan=Fixture-Plan -Count $Count -VirtualEndpoints:$VirtualEndpoints -Ground:$Ground;$script:evidence=[Collections.Generic.List[object]]::new()
    $script:virtualPreviewBindings=[Collections.Generic.List[object]]::new();$script:virtualPreviewEchoes=[Collections.Generic.List[object]]::new()
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
            if($binding.existingSlot -eq -1) {
                $position=$binding.expectedSlotPosition
                $points=@(0..3|ForEach-Object{[pscustomobject]@{index=$_;slot=-1;occupied=$null;otherObjectId=$null;otherSlot=$null;position=$position}})
                switch($script:mode) {
                    virtual_missing_direction {$points=@($points|Select-Object -First 3)}
                    virtual_duplicate_direction {$points[3]=$points[2]}
                    virtual_fake_free {$points[$binding.existingBeltQuarterTurns].occupied=$false;$points[$binding.existingBeltQuarterTurns].otherObjectId=0;$points[$binding.existingBeltQuarterTurns].otherSlot=0}
                    virtual_pose_change {$points[$binding.existingBeltQuarterTurns].position=Fixture-Vector ($position.x+.1) $position.y $position.z}
                }
                $observationKind=if($script:mode-eq'virtual_wrong_kind'){'sorter_slots'}else{'belt_virtual'}
                $observedItemId=if($script:mode-eq'virtual_wrong_item'){2011}else{2001}
                $sorterEndpoints=[pscustomobject]@{state='observed';kind=$observationKind;endpoints=$points}
            } else {
                $position=$binding.expectedSlotPosition;$observedItemId=2011
                $sorterEndpoints=[pscustomobject]@{endpoints=@([pscustomobject]@{slot=$binding.existingSlot;occupied=($script:mode-eq'occupied');otherObjectId=0;position=$binding.expectedSlotPosition})}
            }
            $observedObjectId=if($script:mode-eq'wrong_id'-or$script:mode-eq'virtual_wrong_id'){999}else{$binding.existingObjectId}
            [pscustomobject]@{sessionId=$SessionId;objectKind='entity';planetId=104;objectId=$observedObjectId;itemId=$observedItemId;position=$position;
                endpointStateHash=('endpoint-'+$role);sorterEndpoints=$sorterEndpoints}
        }
        prepare_build {
            $script:prepareCount++;$span=$script:plan.spans[$script:prepareCount-1]
            if ($Payload.beltPathMode -cne $script:plan.commonPayload.beltPathMode) { throw 'Approved routing mode changed.' }
            if ($Payload.beltPathMode -ceq 'native_grid') {
                if ($Payload.ContainsKey('beltStartAltitudeLevel') -or $Payload.ContainsKey('beltEndAltitudeLevel')) { throw 'Ground request serialized altitude fields.' }
            } elseif ($Payload.beltStartAltitudeLevel -ne $span.beltStartAltitudeLevel -or $Payload.beltEndAltitudeLevel -ne $span.beltEndAltitudeLevel) {
                throw 'Approved elevated layers changed.'
            }
            if ($script:methods[$script:methods.Count-2]-cne'get_player_state' -or $Payload.expectedPlayerStateHash-cne'fresh-player') { throw 'Not a fresh per-span player binding.' }
            if ($script:mode-eq'bridge_error' -or ($script:mode-eq'late_bridge_error' -and $script:prepareCount-eq3)) { return [pscustomobject]@{success=$false;error=[pscustomobject]@{code='BUILD_LOCATION_INVALID';message='fixture planned point9 overlaps2459';retryable=$false;recovery='Do not retry same site.'}} }
            $path=@($Payload.preferredPosition,(Fixture-Vector 8 204 0),(Fixture-Vector 20 204 0),$Payload.pathEnd)
            if ($script:mode-eq'polyline' -and $span.endpointPreviewRole-eq$null) { $path[1]=Fixture-Vector 5 224 0;$path[2]=Fixture-Vector 25 224 0 }
            if ($script:mode-eq'join' -and $span.label-eq'H2') { $path[0]=Fixture-Vector 15.03 204 0 }
            $budget=@([pscustomobject]@{itemId=2001;count=$(if($script:mode-eq'budget'){5}else{4})})
            $preview=$null;$prepared=$true;$token='PRIVATE-FIXTURE-TOKEN'
            $role=$span.endpointPreviewRole
            if($null-ne$role) {
                $binding=$script:plan.($role+'Endpoint');$requestBinding=$Payload.beltEndpointPreview[$role]
                if($requestBinding.existingObjectId-ne$binding.existingObjectId -or $requestBinding.existingSlot-ne$binding.existingSlot -or
                    $requestBinding.existingBeltQuarterTurns-ne$binding.existingBeltQuarterTurns -or $requestBinding.plannedBeltQuarterTurns-ne$binding.plannedBeltQuarterTurns -or
                    $requestBinding.expectedEndpointStateHash-cne('endpoint-'+$role)) { throw 'Wrong role/ID/slot/direction/hash binding.' }
                if($binding.existingSlot-eq-1) {$script:virtualPreviewBindings.Add([pscustomobject]@{role=$role;existingObjectId=$requestBinding.existingObjectId;existingSlot=$requestBinding.existingSlot;existingBeltQuarterTurns=$requestBinding.existingBeltQuarterTurns;plannedBeltQuarterTurns=$requestBinding.plannedBeltQuarterTurns})}
                $echoExistingItemId=if($binding.existingSlot-eq-1 -and $script:mode-eq'virtual_echo_item_mismatch'){2011}elseif($binding.existingSlot-eq-1){2001}else{$null}
                $echoExistingTurn=if($binding.existingSlot-eq-1 -and $script:mode-eq'virtual_echo_turn_mismatch'){($binding.existingBeltQuarterTurns+1)%4}elseif($binding.existingSlot-eq-1){$binding.existingBeltQuarterTurns}else{$null}
                $echoHash=if($script:mode-eq'endpoint_hash'-or$script:mode-eq'virtual_echo_hash_mismatch'){'wrong'}else{'endpoint-'+$role}
                $attachment=[pscustomobject]@{role=$role;existingObjectId=$binding.existingObjectId;existingItemId=$echoExistingItemId;existingBeltQuarterTurns=$echoExistingTurn;endpointStateHash=$echoHash;
                    filterItemId=1109;sorterItemId=2011;plannedBeltQuarterTurns=$binding.plannedBeltQuarterTurns;nativeCondition='Ok';nativeSpan=2;
                    attachment=[pscustomobject]@{sourceSlot=$(if($role-eq'source'){$binding.existingSlot}else{-1});destinationSlot=$(if($role-eq'destination'){$binding.existingSlot}else{-1})}}
                if($binding.existingSlot-eq-1) {$script:virtualPreviewEchoes.Add($attachment)}
                $blocked=$script:mode-eq'endpoint_blocker';$nativeNotRun=$script:mode-eq'virtual_native_not_run';$nativePerformed=(-not($blocked-or$nativeNotRun))
                $preview=[pscustomobject]@{nativeCheckPerformed=$nativePerformed;nativeCheckPassed=$nativePerformed;executable=$false;
                    blockers=$(if($blocked){@('planned_endpoint_collision_or_prototype_unavailable')}elseif($nativeNotRun){@('native_check_not_performed')}else{@()});attachments=@($attachment)}
                $prepared=$false;$token='';$budget+=[pscustomobject]@{itemId=2011;count=1}
            }
            [pscustomobject]@{prepared=$prepared;commitAllowedNow=$prepared;planToken=$token;commitBlockers=@();plannedPath=$path;itemBudget=$budget;beltEndpointPreview=$preview;
                plannedBeltPath=[pscustomobject]@{newObjectCount=4;nativeValidationMode='full_path_stage1';routingMode=$(if($script:mode-eq'routing_echo'){'geodesic'}else{$Payload.beltPathMode});
                    startAltitudeLevel=$(if($script:mode-eq'ground_layer_echo'){0}else{$Payload['beltStartAltitudeLevel']});endAltitudeLevel=$Payload['beltEndAltitudeLevel']}}
        }
        get_gameplay_journal {
            [pscustomobject]@{sessionId=$SessionId;entries=@([pscustomobject]@{sequence=1});durableThroughSequence=1;persistencePending=($script:mode-eq'journal');persistenceError=$null}
        }
    }
    [pscustomobject]@{success=$true;result=$result}
}
function Run-Qualification([int]$Accepted=10) {
    Invoke-SpherewrightBeltSiteQualification -ApprovedPlan $script:plan -ExpectedRevision 7 -AcceptedBefore $Accepted -AuditWindowLimit 10 -MinimumDurableSequence 1 -RecordEvidence {param($row)$script:evidence.Add($row)}
}
foreach($count in @(3,4,5)) {
    Reset-Qualification -Count $count
    $summary=Run-Qualification
    Assert-Qualification ($summary.result-ceq'qualified_sites_only' -and $summary.spans.Count-eq$count) "$count fixed spans work without rewriting a caller"
    Assert-Qualification ($summary.bridgeRequests-eq(6+2*$count) -and $script:methods.Count-eq$summary.bridgeRequests) "$count bounded requests including closure"
    Assert-Qualification ($script:prepareCount-eq$count -and $script:playerCount-eq($count+1)) "$count one prepare and fresh player per span"
    Assert-Qualification ($summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq10 -and $summary.closure.revision-eq7 -and $summary.closure.savedTick-eq1000) 'preserves ten-write freeze, revision and save'
    Assert-Qualification (-not$summary.futureActualIdJoinProven -and -not$summary.wholePlanExecutable -and $summary.doNotReplay) 'site previews never mean construction approval'
    Assert-Qualification (($summary|ConvertTo-Json -Depth 30)-notmatch'PRIVATE-FIXTURE-TOKEN' -and ($script:evidence|ConvertTo-Json -Depth 30)-notmatch'PRIVATE-FIXTURE-TOKEN') 'ordinary prepare tokens never enter returned or caller evidence'
}
Reset-Qualification -Count 3
$summary = Invoke-SpherewrightBeltSiteQualification -ApprovedPlan $script:plan -ExpectedRevision 7 -AcceptedBefore 20 -MinimumDurableSequence 1 -RecordEvidence {param($row)$script:evidence.Add($row)}
Assert-Qualification ($summary.result-ceq'qualified_sites_only' -and $summary.acceptedAfter-eq20 -and $summary.acceptedDelta-eq0 -and
    $summary.auditWindowLimit-eq20 -and @($script:methods|Where-Object{$_ -like 'commit_*'}).Count-eq0) 'omitted read-only limit defaults to 20 at acceptedBefore 20 without consuming a write slot'
foreach($count in @(3,4,5)) {
    Reset-Qualification -Count $count -Ground
    $summary=Run-Qualification 9
    Assert-Qualification ($summary.result-ceq'qualified_sites_only' -and $summary.spans.Count-eq$count -and $summary.bridgeRequests-eq(6+2*$count)) "$count ground spans use the same bounded caller with no altitude fields"
    Assert-Qualification ($summary.acceptedAfter-eq9 -and $summary.acceptedDelta-eq0 -and -not$summary.wholePlanExecutable -and -not$summary.futureActualIdJoinProven -and $summary.doNotReplay) 'ground qualification neither grants construction nor consumes a write slot'
    Assert-Qualification (($summary|ConvertTo-Json -Depth 30)-notmatch'PRIVATE-FIXTURE-TOKEN' -and ($script:evidence|ConvertTo-Json -Depth 30)-notmatch'PRIVATE-FIXTURE-TOKEN') 'ground ordinary tokens remain private and unused'
}
Reset-Qualification -Count 3 -Ground -VirtualEndpoints
$summary=Run-Qualification 9
Assert-Qualification ($summary.result-ceq'qualified_sites_only' -and $script:virtualPreviewBindings.Count-eq2 -and $summary.acceptedDelta-eq0) 'ground virtual endpoints retain exact native preview binding'
Reset-Qualification -Count 3 -Ground
foreach($span in $script:plan.spans) {
    $span|Add-Member -NotePropertyName beltStartAltitudeLevel -NotePropertyValue $null
    $span|Add-Member -NotePropertyName beltEndAltitudeLevel -NotePropertyValue $null
}
$summary=Run-Qualification 9
Assert-Qualification ($summary.result-ceq'qualified_sites_only') 'explicit null ground layers are omitted rather than coerced to zero'
foreach($mode in @('routing_echo','ground_layer_echo','bridge_error','endpoint_blocker')) {
    Reset-Qualification -Mode $mode -Count 3 -Ground
    $summary=Run-Qualification 9
    Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq1 -and $summary.bridgeRequests-eq8 -and $null-ne$summary.closure -and $summary.acceptedDelta-eq0 -and $summary.doNotReplay) "$mode ground failure stops without retry and retains read-only closure"
}
Reset-Qualification -Mode late_bridge_error -Count 5
$summary=Run-Qualification 3
Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq3 -and $summary.spans.Count-eq2 -and $summary.bridgeRequests-eq12) 'five-span failure stops the remaining two prepares and includes bounded closure'
Assert-Qualification ($summary.failure.code-ceq'BUILD_LOCATION_INVALID' -and $summary.failure.stage-ceq'H1' -and $summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq3 -and $null-ne$summary.closure -and $summary.doNotReplay) 'five-span partial qualification retains failure identity, accepted and non-replay semantics'
Reset-Qualification -Mode join -Count 5
$summary=Run-Qualification
Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq5 -and $summary.bridgeRequests-eq16 -and $summary.failure.stage-ceq'contiguous_endpoints' -and $summary.acceptedDelta-eq0) 'five-span returned seam gap is not a qualification pass or retry'
Reset-Qualification -Mode polyline -Count 3
$summary=Run-Qualification
Assert-Qualification ($summary.result-ceq'qualified_sites_only') 'native endpoint chord, not longer fixture polyline, is the30m bound'
Reset-Qualification -Count 4 -VirtualEndpoints
$summary=Run-Qualification
$virtualSource=@($script:virtualPreviewBindings|Where-Object{$_.role-ceq'source'})[0];$virtualDestination=@($script:virtualPreviewBindings|Where-Object{$_.role-ceq'destination'})[0]
$virtualSourceEcho=@($script:virtualPreviewEchoes|Where-Object{$_.role-ceq'source'})[0];$virtualDestinationEcho=@($script:virtualPreviewEchoes|Where-Object{$_.role-ceq'destination'})[0]
Assert-Qualification ($summary.result-ceq'qualified_sites_only' -and $summary.spans.Count-eq4 -and $summary.bridgeRequests-eq14 -and $script:prepareCount-eq4) 'source and destination virtual belts pass all four bounded spans'
Assert-Qualification ($virtualSource.existingObjectId-eq870 -and $virtualSource.existingSlot-eq-1 -and $virtualSource.existingBeltQuarterTurns-eq1 -and $virtualSource.plannedBeltQuarterTurns-eq2 -and
    $virtualDestination.existingObjectId-eq5334 -and $virtualDestination.existingSlot-eq-1 -and $virtualDestination.existingBeltQuarterTurns-eq3 -and $virtualDestination.plannedBeltQuarterTurns-eq1) 'requests bind exact source/destination IDs and nonzero observed directions'
Assert-Qualification ($virtualSourceEcho.existingItemId-eq2001 -and $virtualSourceEcho.existingBeltQuarterTurns-eq1 -and $virtualSourceEcho.attachment.sourceSlot-eq-1 -and
    $virtualDestinationEcho.existingItemId-eq2001 -and $virtualDestinationEcho.existingBeltQuarterTurns-eq3 -and $virtualDestinationEcho.attachment.destinationSlot-eq-1) 'native echoes exact virtual item, direction and slot=-1'
Assert-Qualification ($summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq10 -and $summary.closure.revision-eq7 -and $summary.closure.savedTick-eq1000 -and
    -not$summary.futureActualIdJoinProven -and -not$summary.wholePlanExecutable -and $summary.doNotReplay) 'virtual site qualification preserves write freeze, closure and non-executable result'
foreach($caseName in @('virtual_wrong_item','virtual_wrong_kind','virtual_missing_direction','virtual_duplicate_direction','virtual_fake_free','virtual_pose_change','virtual_wrong_id')) {
    Reset-Qualification -Mode $caseName -Count 4 -VirtualEndpoints
    $summary=Run-Qualification 6
    Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq0 -and $summary.spans.Count-eq0) "$caseName rejects before every prepare"
    Assert-Qualification ($summary.failure.stage-ceq'endpoint_source' -and $summary.bridgeRequests-eq5 -and $null-ne$summary.closure -and $summary.doNotReplay -and $summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq6) "$caseName stops and closes read-only without replay"
}
Reset-Qualification -Count 4 -VirtualEndpoints
$script:plan.sourceEndpoint.existingBeltQuarterTurns=4
$summary=Run-Qualification 6
Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq0 -and $summary.failure.stage-ceq'endpoint_source' -and $summary.bridgeRequests-eq4 -and $null-ne$summary.closure -and $summary.doNotReplay) 'out-of-range virtual direction rejects before entity inspection or prepare and closes read-only'
Reset-Qualification -Count 4
$script:plan.sourceEndpoint.existingBeltQuarterTurns=1
$summary=Run-Qualification 6
Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq0 -and $summary.failure.stage-ceq'endpoint_source' -and $summary.bridgeRequests-eq4 -and $null-ne$summary.closure -and $summary.doNotReplay) 'built device with nonzero existing quarter-turn rejects before entity inspection or prepare and closes read-only'
foreach($caseName in @('virtual_native_not_run','virtual_echo_item_mismatch','virtual_echo_turn_mismatch','virtual_echo_hash_mismatch')) {
    Reset-Qualification -Mode $caseName -Count 4 -VirtualEndpoints
    $summary=Run-Qualification 6
    Assert-Qualification ($summary.result-ceq'stopped' -and $script:prepareCount-eq1 -and $summary.bridgeRequests-eq8 -and $summary.spans.Count-eq0) "$caseName fails first preview and does not prepare later spans"
    Assert-Qualification ($summary.failure.stage-ceq'D' -and $null-ne$summary.closure -and $summary.doNotReplay -and $summary.acceptedDelta-eq0 -and $summary.acceptedAfter-eq6) "$caseName performs only proved read-only closure and never replays"
    if($caseName-eq'virtual_native_not_run'){Assert-Qualification ($summary.failure.kind-ceq'endpoint_rejection' -and -not$summary.failure.nativeCheckPerformed) 'unperformed native check is not a pass'}
}
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
foreach($caseName in @('too_many','write_allowed','unsupported_common','bad_vector','request_budget','five_request_budget','excess_request_budget','unsupported_mode','ground_zero','ground_elevated','missing_layer','flat_elevated')) {
    Reset-Qualification
    switch($caseName) {
        too_many {$script:plan=Fixture-Plan -Count 5; $script:plan.spans+= $script:plan.spans[-1]}
        write_allowed {$script:plan.gameCommitAllowed=$true}
        unsupported_common {$script:plan.commonPayload|Add-Member -NotePropertyName forbiddenSourceObjectId -NotePropertyValue 123}
        bad_vector {$script:plan.spans[0].preferredPosition.x=$true}
        request_budget {$script:plan.maximumRequests=13}
        five_request_budget {$script:plan=Fixture-Plan -Count 5; $script:plan.maximumRequests=15}
        excess_request_budget {$script:plan.maximumRequests=17}
        unsupported_mode {$script:plan.commonPayload.beltPathMode='geodesic'}
        ground_zero {$script:plan=Fixture-Plan -Count 3 -Ground; $script:plan.spans[0]|Add-Member -NotePropertyName beltStartAltitudeLevel -NotePropertyValue 0}
        ground_elevated {$script:plan.commonPayload.beltPathMode='native_grid'}
        missing_layer {$script:plan.spans[0].beltStartAltitudeLevel=$null}
        flat_elevated {$script:plan.spans[0].beltStartAltitudeLevel=0; $script:plan.spans[0].beltEndAltitudeLevel=0}
    }
    $caught=$false;try{Run-Qualification|Out-Null}catch{$caught=$true}
    Assert-Qualification ($caught -and $script:methods.Count-eq0) "$caseName rejects before runtime discovery or transport"
}
Assert-Qualification (@($script:allMethods|Where-Object {$_-like'commit_*'-or$_-match'save|resume|close|exit'}).Count-eq0) 'no write/lifecycle dispatch across all fixtures'
Write-Output "Belt site qualification: $script:checks checks passed; zero real game calls."
