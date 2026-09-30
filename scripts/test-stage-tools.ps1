# Offline transport fixture, never discovers a runtime descriptor.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'SpherewrightActionClient.ps1')
. (Join-Path $PSScriptRoot 'SpherewrightStageTools.ps1')
$script:checks = 0
$script:storageChecks = 0
function Assert-Stage([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Stage regression: $Name" }
    $script:checks++
}
function Assert-Storage([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "Storage count regression: $Name" }
    $script:storageChecks++
}
function Reset-Stage([string]$Failure = '') {
    $script:methods = [Collections.Generic.List[string]]::new()
    $script:sessionReads = 0; $script:progressReads = 0; $script:journalReads = 0
    $script:actionId = ''; $script:failure = $Failure; $script:researchPayload = $null; $script:savePayload = $null; $script:researchBudget = @()
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
            [pscustomobject]@{sessionId=$SessionId;planetId=104;selectionStateHash='fresh-selection';selectionStateHashVersion=1;techQueue=$(if($script:progressReads -gt 1 -and $script:failure -cne 'readback'){@(2104)}else{@()});technologies=@()}
        }
        get_gameplay_journal {
            $script:journalReads++
            $last = if($script:journalReads -eq 1){91}else{95}
            [pscustomobject]@{sessionId=$SessionId;entries=@(1..$last | ForEach-Object { [pscustomobject]@{sequence=$_} });durableThroughSequence=$last;persistencePending=($script:failure -ceq 'journal' -and $script:journalReads -gt 1);persistenceError=$null}
        }
        prepare_select_research {
            $script:researchPayload=$Payload
            if ($Payload.expectedSelectionStateHash -cne 'fresh-selection' -or $Payload.ContainsKey('expectedProgressionStateHash')) { throw 'Wrong research hash field' }
            # Match PrepareSelectResearchOnMainThread: these are the technology's
            # future research costs, not immediate backpack deductions.
            $script:researchBudget = @(6001..6004 | ForEach-Object {
                [pscustomobject]@{itemId=$_;name='fixture-matrix';count=500;direction='research-consumption'}
            })
            switch ($script:failure) {
                budget_missing { $script:researchBudget = @() }
                budget_direction { $script:researchBudget[0].direction = 'consume' }
                budget_count { $script:researchBudget[0].count = 501 }
                budget_duplicate { $script:researchBudget[0].itemId = 6002 }
            }
            [pscustomobject]@{prepared=$true;commitAllowedNow=$true;planToken='private-fixture-token';actionKind='select-research';itemBudget=$script:researchBudget;completionCondition="DSP's normal technology queue contains the requested technology and currentTech reflects the queue head."}
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
$arguments = @{SessionId='fixture-session';PlanetId=104;GameVersion='fixture-version';TechId=2104;AcceptedBefore=8;ValidateResearchPlan={
    param($p)
    if ($p.actionKind -cne 'select-research' -or @($p.itemBudget).Count -ne 4 -or
        $p.completionCondition -cne "DSP's normal technology queue contains the requested technology and currentTech reflects the queue head.") { return $false }
    foreach ($itemId in 6001..6004) {
        $rows = @($p.itemBudget | Where-Object itemId -eq $itemId)
        if ($rows.Count -ne 1 -or $rows[0].count -ne 500 -or $rows[0].direction -cne 'research-consumption') { return $false }
    }
    return $true
};RecordEvidence={param($row) $script:recorded=$row}}
Reset-Stage
$result = Invoke-SpherewrightResearchAndSave @arguments
Assert-Stage ($result.acceptedDelta -eq 2 -and $result.acceptedAfter -eq 10 -and $result.frozen) 'counts actual accepted and freezes at ten'
Assert-Stage ($script:savePayload.expectedRevision -eq 7 -and $result.revision -eq 11 -and $result.durableThroughSequence -eq 95) 'uses actual revision and durable Journal, no plus-one or old J'
Assert-Stage ($script:methods.Count -eq 14 -and @($script:methods | Where-Object {$_ -like 'commit_*'}).Count -eq 2) 'one research then one save, no replay'
Assert-Stage ($script:researchPayload.techId -eq 2104 -and $script:researchBudget.Count -eq 4) 'accepts approved nonempty native future research budget'
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
foreach ($failure in @('budget_missing','budget_direction','budget_count','budget_duplicate')) {
    Reset-Stage $failure
    $errorRecord = $null
    try { Invoke-SpherewrightResearchAndSave @arguments | Out-Null } catch { $errorRecord=$_ }
    Assert-Stage ($null -ne $errorRecord -and $errorRecord.Exception.Data['spherewrightStageAcceptedDelta'] -eq 0) "$failure fails exact budget validation before acceptance"
    Assert-Stage (@($script:methods | Where-Object {$_ -like 'commit_*'}).Count -eq 0) "$failure never commits research or save"
}
Reset-Stage
$invalid = @{}; foreach($key in $arguments.Keys){$invalid[$key]=$arguments[$key]}; $invalid.AcceptedBefore=9
try { Invoke-SpherewrightResearchAndSave @invalid | Out-Null } catch { }
Assert-Stage ($script:methods.Count -eq 0) 'insufficient external audit budget rejected before any request'
function gh { $global:LASTEXITCODE=0; '[{"databaseId":1,"headSha":"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa","status":"completed","conclusion":"success","url":"https://example.invalid/ci"}]' }
$ciFixture=Get-SpherewrightCommitChecks -CommitSha ('a'*40)
Assert-Stage ($ciFixture.observed -and $ciFixture.runs[0].conclusion -ceq 'success' -and -not $ciFixture.signOff) 'exact SHA CI snapshot is not independent sign-off'
function gh { $global:LASTEXITCODE=0; '[]' }
$ciFixture=Get-SpherewrightCommitChecks -CommitSha ('a'*40)
Assert-Stage (-not $ciFixture.observed) 'absent workflow run stays unknown'

# Direct offline fixtures for observed built-storage snapshots. These call no
# transport; keep their count separate from the existing 26 stage assertions.
function New-StorageTestBuffer($ItemId, $Count, $Role='storage', $CountUnit='items', $UnitsPerItem=1) {
    [pscustomobject]@{itemId=$ItemId;role=$Role;countUnit=$CountUnit;unitsPerItem=$UnitsPerItem;count=$Count}
}
function New-StorageTestSnapshot($Buffers) {
    [pscustomobject]@{objectKind='entity';componentKind='storage';buffers=$Buffers}
}
function Assert-StorageReject($Snapshot, [string]$Name) {
    $caught = $false
    try { Get-SpherewrightStorageItemCount -Snapshot $Snapshot -ItemId 6004 | Out-Null } catch { $caught = $true }
    Assert-Storage $caught $Name
}

$storageBuffers = [Collections.Generic.List[object]]::new()
for ($index = 0; $index -lt 11; $index++) {
    $storageBuffers.Add((New-StorageTestBuffer -ItemId 6004 -Count 200))
}
$storageBuffers.Add((New-StorageTestBuffer -ItemId 6004 -Count 124))
$storageBuffers.Add((New-StorageTestBuffer -ItemId 1120 -Count 5000))
$storageBuffers.Add((New-StorageTestBuffer -ItemId 1802 -Count 9))
$storageSnapshot = New-StorageTestSnapshot -Buffers $storageBuffers.ToArray()
$storageTotal = Get-SpherewrightStorageItemCount -Snapshot $storageSnapshot -ItemId 6004
Assert-Storage (@($storageSnapshot.buffers | Where-Object itemId -eq 6004).Count -eq 12) 'fixture covers twelve occupied grids for the same item'
Assert-Storage ($storageTotal -eq [long]2324) 'sums 11 x 200 plus 124 and ignores other item types'

$zeroSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count 0))
Assert-Storage ((Get-SpherewrightStorageItemCount -Snapshot $zeroSnapshot -ItemId 6004) -eq 0) 'observed zero count is valid'
$emptySnapshot = [pscustomobject]@{objectKind='entity';componentKind='storage';buffers=[object[]]@()}
Assert-Storage ((Get-SpherewrightStorageItemCount -Snapshot $emptySnapshot -ItemId 6004) -eq 0) 'observed empty buffer list is valid zero'

$missingBuffersSnapshot = [pscustomobject]@{objectKind='entity';componentKind='storage'}
Assert-StorageReject $missingBuffersSnapshot 'missing buffers are unknown, not zero'
$nullBuffersSnapshot = [pscustomobject]@{objectKind='entity';componentKind='storage';buffers=$null}
Assert-StorageReject $nullBuffersSnapshot 'null buffers are unknown, not zero'
$researchSnapshot = [pscustomobject]@{objectKind='entity';componentKind='research-matrix';buffers=@();points=36000}
Assert-StorageReject $researchSnapshot 'research-matrix points are not storage items'
$unknownComponentSnapshot = [pscustomobject]@{objectKind='entity';componentKind='unrecognized';buffers=@()}
Assert-StorageReject $unknownComponentSnapshot 'unknown component is rejected'

foreach ($invalidItemId in @(
    [pscustomobject]@{value=$null;name='null'},
    [pscustomobject]@{value='6004';name='string'},
    [pscustomobject]@{value=0;name='zero'},
    [pscustomobject]@{value=-1;name='negative'},
    [pscustomobject]@{value=([long]2147483648);name='above Int32 range'}
)) {
    $invalidIdBuffer = New-StorageTestBuffer -ItemId $invalidItemId.value -Count 1
    Assert-StorageReject (New-StorageTestSnapshot -Buffers @($invalidIdBuffer)) "observed $($invalidItemId.name) itemId is rejected"
}
$nullRows = [Collections.Generic.List[object]]::new()
$nullRows.Add($null)
Assert-StorageReject (New-StorageTestSnapshot -Buffers $nullRows.ToArray()) 'null buffer rows are rejected'

$badRoleSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count 1 -Role 'research-matrix'))
Assert-StorageReject $badRoleSnapshot 'wrong storage role is rejected'
$badUnitSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count 1 -CountUnit 'points'))
Assert-StorageReject $badUnitSnapshot 'wrong count unit is rejected'
$badUnitsPerItemSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count 1 -UnitsPerItem 2))
Assert-StorageReject $badUnitsPerItemSnapshot 'non-unit item multiplier is rejected'

$negativeCountSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count -1))
Assert-StorageReject $negativeCountSnapshot 'negative count is rejected'
$nullCountSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count $null))
Assert-StorageReject $nullCountSnapshot 'null count is rejected'
$fractionalCountSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count 1.5))
Assert-StorageReject $fractionalCountSnapshot 'fractional count is rejected'
$booleanCountSnapshot = New-StorageTestSnapshot -Buffers @((New-StorageTestBuffer -ItemId 6004 -Count $true))
Assert-StorageReject $booleanCountSnapshot 'boolean count is rejected'
$missingCountBuffer = [pscustomobject]@{itemId=6004;role='storage';countUnit='items';unitsPerItem=1}
$missingCountSnapshot = New-StorageTestSnapshot -Buffers @($missingCountBuffer)
Assert-StorageReject $missingCountSnapshot 'missing count is rejected'
$overflowSnapshot = New-StorageTestSnapshot -Buffers @(
    (New-StorageTestBuffer -ItemId 6004 -Count ([long]::MaxValue)),
    (New-StorageTestBuffer -ItemId 6004 -Count 1)
)
Assert-StorageReject $overflowSnapshot 'Int64 sum overflow is rejected'
foreach ($invalidRequestedItemId in @(
    [pscustomobject]@{value=$null;name='null'},
    [pscustomobject]@{value=-1;name='negative'},
    [pscustomobject]@{value=([long]2147483648);name='above Int32 range'},
    [pscustomobject]@{value='not-an-id';name='non-numeric string'}
)) {
    $caught = $false
    try { Get-SpherewrightStorageItemCount -Snapshot $storageSnapshot -ItemId $invalidRequestedItemId.value | Out-Null } catch { $caught = $true }
    Assert-Storage $caught "requested $($invalidRequestedItemId.name) itemId is rejected"
}
Assert-Storage ($script:methods.Count -eq 0) 'storage helper fixtures make no Bridge or game requests'

[pscustomobject]@{passed=$script:checks;storageChecks=$script:storageChecks;gameCalls=0;successfulFixtureRequests=14} | ConvertTo-Json -Compress
