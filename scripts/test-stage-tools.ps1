# Offline transport fixture, never discovers a runtime descriptor.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightStageTools.ps1')
$script:checks = 0
function Assert-Stage([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Stage regression: $Name" }
    $script:checks++
}
function Reset-Stage([string]$Failure = '') {
    $script:methods = [Collections.Generic.List[string]]::new()
    $script:sessionReads = 0; $script:progressReads = 0; $script:journalReads = 0
    $script:actionId = ''; $script:failure = $Failure; $script:researchPayload = $null; $script:savePayload = $null
}
function Invoke-SpherewrightBridgeRequest([string]$Method, [string]$SessionId, [hashtable]$Payload) {
    $script:methods.Add($Method)
    if ($SessionId -cne 'fixture-session') { throw 'Unexpected session' }
    $result = switch ($Method) {
        get_session_state {
            $script:sessionReads++
            [pscustomobject]@{sessionId=$SessionId;localPlanetId=104;gameVersion='fixture-version';gameLoaded=$true;ownedBySpherewright=$true;accessRestricted=$false;writeHealth='healthy';writeBlockers=@();writesAllowed=$true;peacefulMode='confirmed_peaceful';gameTick=1100;revision=$(switch($script:sessionReads){1{2}2{7}default{11}});ownedSaveState='saved';lastOwnedSaveGameTick=1000;restartResumeAvailable=$true}
        }
        get_player_state { [pscustomobject]@{sessionId=$SessionId;planetId=104} }
        get_progression_state {
            $script:progressReads++
            [pscustomobject]@{sessionId=$SessionId;planetId=104;selectionStateHash='fresh-selection';selectionStateHashVersion=1;techQueue=$(if($script:progressReads -gt 1 -and $script:failure -cne 'readback'){@(1607)}else{@()});technologies=@()}
        }
        get_gameplay_journal {
            $script:journalReads++
            $last = if($script:journalReads -eq 1){91}else{95}
            [pscustomobject]@{sessionId=$SessionId;entries=@(1..$last | ForEach-Object { [pscustomobject]@{sequence=$_} });durableThroughSequence=$last;persistencePending=($script:failure -ceq 'journal' -and $script:journalReads -gt 1);persistenceError=$null}
        }
        prepare_select_research {
            $script:researchPayload=$Payload
            if ($Payload.expectedSelectionStateHash -cne 'fresh-selection' -or $Payload.ContainsKey('expectedProgressionStateHash')) { throw 'Wrong research hash field' }
            [pscustomobject]@{prepared=$true;commitAllowedNow=$true;planToken='private-fixture-token';actionKind='select-research';itemBudget=@()}
        }
        prepare_save {
            $script:savePayload=$Payload
            [pscustomobject]@{prepared=$true;commitAllowedNow=$true;planToken='private-fixture-token';actionKind='save';itemBudget=@()}
        }
        commit_select_research {
            $script:actionId='research-action'
            [pscustomobject]@{accepted=$true;idempotentReplay=$false;actionId=$script:actionId}
        }
        commit_save {
            if($script:failure -ceq 'lost_commit'){throw 'fixture lost response'}
            $script:actionId='save-action'
            [pscustomobject]@{accepted=$true;idempotentReplay=$false;actionId=$script:actionId}
        }
        get_action_result {
            if ($Payload.actionId -cne $script:actionId) { throw 'Different action observed' }
            [pscustomobject]@{actionId=$script:actionId;terminal=$true;succeeded=$true;completedAtGameTick=1000}
        }
        default { throw "Unexpected method $Method" }
    }
    [pscustomobject]@{success=$true;result=$result}
}
$arguments = @{SessionId='fixture-session';PlanetId=104;GameVersion='fixture-version';TechId=1607;AcceptedBefore=8;ValidateResearchPlan={param($p) $p.actionKind -ceq 'select-research' -and @($p.itemBudget).Count -eq 0};RecordEvidence={param($row) $script:recorded=$row}}
Reset-Stage
$result = Invoke-SpherewrightResearchAndSave @arguments
Assert-Stage ($result.acceptedDelta -eq 2 -and $result.acceptedAfter -eq 10 -and $result.frozen) 'counts actual accepted and freezes at ten'
Assert-Stage ($script:savePayload.expectedRevision -eq 7 -and $result.revision -eq 11 -and $result.durableThroughSequence -eq 95) 'uses actual revision and durable Journal, no plus-one or old J'
Assert-Stage ($script:methods.Count -eq 14 -and @($script:methods | Where-Object {$_ -like 'commit_*'}).Count -eq 2) 'one research then one save, no replay'
Assert-Stage (($result | ConvertTo-Json -Depth 8) -notmatch 'private-fixture-token' -and $result.timingMs.entryToFirstPrepare -ge 0 -and $null -eq $result.timingMs.dispatchToFirstPrepare) 'compact credential-free timing, unknown dispatch stays null'
Assert-Stage ($script:recorded.acceptedAfter -eq 10 -and $result.unproved -contains 'save_restart') 'summary is not a restart or throughput proof'
foreach ($failure in @('readback','journal','lost_commit')) {
    Reset-Stage $failure
    $errorRecord = $null
    try { Invoke-SpherewrightResearchAndSave @arguments | Out-Null } catch { $errorRecord=$_ }
    Assert-Stage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightDoNotReplayStage'] -eq $true) "$failure stops, never replays a prefix"
    Assert-Stage (@($script:methods | Where-Object {$_ -ceq 'commit_select_research'}).Count -eq 1 -and @($script:methods | Where-Object {$_ -ceq 'commit_save'}).Count -le 1) "$failure keeps unique commits"
    if ($failure -ceq 'readback') { Assert-Stage ($errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 1 -and $script:methods -notcontains 'prepare_save') 'unproved selection prevents save without reclassifying research' }
    if ($failure -ceq 'journal') { Assert-Stage ($errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 2) 'summary failure retains both accepted actions' }
    if ($failure -ceq 'lost_commit') { Assert-Stage ($errorRecord.Exception.Data['spherewrightCommitMayHaveBeenAccepted'] -eq $true -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 1) 'second commit stays uncertain, not falsely rejected or counted known' }
}
Reset-Stage
$invalid = @{}; foreach($key in $arguments.Keys){$invalid[$key]=$arguments[$key]}; $invalid.AcceptedBefore=9
try { Invoke-SpherewrightResearchAndSave @invalid | Out-Null } catch { }
Assert-Stage ($script:methods.Count -eq 0) 'insufficient external audit budget rejected before any request'
[pscustomobject]@{passed=$script:checks;gameCalls=0;successfulFixtureRequests=14} | ConvertTo-Json -Compress
